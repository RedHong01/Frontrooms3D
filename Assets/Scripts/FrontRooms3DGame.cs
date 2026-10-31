using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UiDocument = UnityEngine.UIElements.UIDocument;
using UiImage = UnityEngine.UIElements.Image;
using UiVectorImage = UnityEngine.UIElements.VectorImage;
using UiVisualElement = UnityEngine.UIElements.VisualElement;
using UiPanelSettings = UnityEngine.UIElements.PanelSettings;
using UiPanelScaleMode = UnityEngine.UIElements.PanelScaleMode;
using UiPanelScreenMatchMode = UnityEngine.UIElements.PanelScreenMatchMode;
using UiDisplayStyle = UnityEngine.UIElements.DisplayStyle;
using UiPosition = UnityEngine.UIElements.Position;
using UiOverflow = UnityEngine.UIElements.Overflow;
using UiPickingMode = UnityEngine.UIElements.PickingMode;
using UiLength = UnityEngine.UIElements.Length;
using UiLengthUnit = UnityEngine.UIElements.LengthUnit;
using UiEventSystem = UnityEngine.EventSystems.EventSystem;
using UiEventTrigger = UnityEngine.EventSystems.EventTrigger;
using UiEventTriggerType = UnityEngine.EventSystems.EventTriggerType;
using UiInputModule = UnityEngine.InputSystem.UI.InputSystemUIInputModule;

[ExecuteAlways]
// First-person FrontRooms. The title is the looping room stream; when the
// player presses Space they take over where the camera is, the stream stops
// at its next shut door, and the generated Level 0 maze (FrontRoomsMapWorld)
// lies behind that door. There the serialized Relay hunts them
// (FrontRoomsMapHunter).
public sealed partial class FrontRooms3DGame : MonoBehaviour
{
    enum Phase { Title, Playing, Paused, Caught, Inspect }   // append only (the touch playtest reads phases by name)
    Phase phase;
    Camera cam;
    [SerializeField, Tooltip("The Relay rig serialized in this scene. It stays hidden until the hunter is released.")]
    Transform hunter;
    [SerializeField, Tooltip("When the Relay is released, how it listens, hunts, searches, chases and breaks doors.")]
    FrontRoomsHunterTuning hunterTuning = new FrontRoomsHunterTuning();
    /// <summary>A map run began (the player took over in the title's stream room): the map and the Relay exist. For listeners such as the sound layer.</summary>
    public static event Action<FrontRoomsMapWorld, FrontRoomsMapHunter> MapRunStarted;
    /// <summary>The map run is torn down (restart reloads the scene, or Play stops).</summary>
    public static event Action MapRunEnded;
    /// <summary>
    /// C22 (plan v2 D15): the run went one floor deeper. Raised once the map has been reseeded in place for the
    /// new floor (FrontRoomsMapWorld.NextFloor: the same map object, so a listener bound to it at MapRunStarted
    /// stays bound) and has begun streaming round the stairwell the player is shut in. Carries the map, the Relay
    /// (the same hunter, parked Dormant for the change: FrontRoomsMapHunter.Park) and the new floor: 2 for the
    /// first floor below the title's, then 3, ... (FloorLabel names it). MapRunStarted stays the run's start
    /// only, so nothing that restarts on it (the sound layer's breath and room tone) restarts on a floor change.
    /// </summary>
    public static event Action<FrontRoomsMapWorld, FrontRoomsMapHunter, int> FloorChanged;
    /// <summary>
    /// The name of a floor of the run on the player's screens (C22 D13; 叙事设计's signed words,
    /// Documentation/research/narrative/40_floors_exit_tutorial_strings.md §2 and §6): the title's floor (1) is
    /// LEVEL 0 and every floor below it a basement, LEVEL B1, LEVEL B2, ... The one place the token is built:
    /// the HUD's room text, the CAUGHT line and the touch caught card read it. Red may switch to counting up
    /// (LEVEL 1, 2, ...); then only this function changes.
    /// </summary>
    public static string FloorLabel(int floor) => floor <= 1 ? FrontRoomsLanguage.T("LEVEL 0") : FrontRoomsLanguage.F("LEVEL B{0}", (floor - 1).ToString());
    /// <summary>The player started climbing through a broken window, at its opening.</summary>
    public static event Action<Vector3> PlayerClimbed;
    /// <summary>The game paused (true: Esc or lost focus) or resumed (false). Also false when a paused run is torn down.</summary>
    public static event Action<bool> Paused;
    /// <summary>The run's difficulty tier: 1 when a run starts, then each rise (level profile Tiers: every 4 new zones, or 2 minutes without one).</summary>
    public static event Action<int> TierChanged;
    /// <summary>W1: a warning burst began (the room's centre, how many lamps dip; 0 = audio only), re-raised from the run's FrontRoomsWarnLamps for the sound layer's ballast groan.</summary>
    public static event Action<Vector3, int> WarnBurst;
    /// <summary>W1: true while a warning burst's dips play (FrontRoomsWarnLamps.Bursting); false outside a run, once caught, and when the run is torn down.</summary>
    public static bool WarnBursting { get; private set; }
    /// <summary>The player's stamina, 0..1 (5 s of sprint), for the sound layer's breath and run gait: read it, don't mirror the rule.</summary>
    public static float PlayerStamina01 { get; private set; } = 1f;
    /// <summary>Run dry and not yet one segment (1 s) back: no sprint until then.</summary>
    public static bool PlayerWinded { get; private set; }
    /// <summary>The sprint rule's answer this frame (Shift held, moving, stamina left, not winded).</summary>
    public static bool PlayerSprinting { get; private set; }
    /// <summary>EXAMINE (research/inspect/10_design.md): a close-up began (true, at the E press) or gave control back (false, as the camera starts home). False on teardown.</summary>
    public static event Action<bool> Inspecting;
    /// <summary>A close-up holds the world (from the E press until the camera starts home).</summary>
    public static bool InspectingNow { get; private set; }
    // A43 (Red 2026-10-30, 系统设计: only careful reading pauses; 游戏视觉's inspect_a43): the open close-up runs live, as
    // FrontRoomsInspect.CurrentPauses said at BeginInspect: timeScale stays 1, the doors and the Relay keep ticking
    // (UpdateMapPlay), a perceivable threat or the rig letting the shot go ends it (FrontRoomsInspect.WantsEnd), a catch ends it.
    bool inspectLive;
    FrontRoomsInspectable inspectAimed, pendingInspect;
    ShotHandle inspectShot;
    Phase pausedFrom = Phase.Playing;
    bool useLatch, backLatch, timeFrozenByInspect;
    // C25b (computer_ui 41 §4.2, terminals): MOVE keys held as a terminal close-up ends are ignored by walking until released.
    bool moveLatch;
    float inspectCooldown;
    // C11: in a close-up the hint slot sits in the bottom band, clear of the framed subject (PROPOSED; 平面视觉 places it).
    // 83: the 26-unit prompt rect's centre, so Bayon 20's glyph bottom lands on UI_SYSTEM's 72 px outer margin at 1080
    // (v1's 64 put it at 53 px, inside the margin).
    Vector2 promptHome;
    bool promptHomeSet;
    const float InspectHintBottom = 83f;

    [SerializeField, Tooltip("The Level 0 maze's numbers: generation, run seed, streaming, light budget, dressing (Assets/Levels/FrontRoomsLevel0.asset). Empty: the code defaults.")]
    FrontRoomsLevelProfile levelProfile;
    FrontRoomsRelayRig hunterRig;
    FrontRoomsMapWorld map;
    FrontRoomsMapHunter relay;
    // W1 (RELAY §9.2, C10): the warning's lamp bursts in the player's room, read from relay.WarnStage.
    FrontRoomsWarnLamps warnLamps;
    Transform playerRoot;
    CharacterController playerBody;
    Vector2 playerPos;
    readonly List<string> events = new List<string>();
    // The events CSV (PT1): one file a run, rewritten whole at each save, so a quit or a floor change keeps every row so far.
    // A session is one launch of the game (a PlayerPrefs counter), on every row.
    const string EventsSessionKey = "FrontRooms.EventsSession";
    static int eventsSession;
    string eventsFile;
    Material wallMat, floorMat, darkMat, ceilingMat;
    readonly Dictionary<RoomRule, Material> wallMats = new Dictionary<RoomRule, Material>();
    readonly Dictionary<RoomRule, Material> floorMats = new Dictionary<RoomRule, Material>();
    readonly Dictionary<RoomRule, Material> ceilingMats = new Dictionary<RoomRule, Material>();
    Material trimMat, fixtureMat;
    AudioSource hum;
    AudioClip playerStepClip, playerRunStepClip, hunterStepClip, doorClip, bangClip, caughtClip;
    FrontRoomsFoley foley;
    Text roomMetaText, roomText, threatStateText, distanceText, contextText, overlayText, keyText, displaySettingsText;
    Text mobilePauseTitle, mobilePauseRows, mobileCaughtTitle, mobileCaughtStats;
    Image crosshairImage, keyImage;
    Text promptText;
    // Captions (audit 2.11): lines above the hint card, outside the shot fade, only with Captions on.
    GameObject captionPanel;
    Text captionText;
    CanvasGroup captionHudGroup;
    Image holdBarFill;
    GameObject holdBar;
    readonly Image[] staminaSegments = new Image[5];
    GameObject keyPanel;
    // HK: the visual chat's key HUD (FrontRoomsKeyHud) and what the crosshair is on for it (the map's Kind, grammar H1).
    FrontRoomsKeyHud keyHud;
    FrontRoomsMapWorld.AimKind aimKind;
    GridCoord aimKeyZone;
    bool aimKeyHeld;
    Image logoImage;
    Image logoLeftImage, logoSlideImage;
    Transform logoMotionRoot;
    UiDocument vectorLogoDocument;
    UiVisualElement vectorLogoRoot;
    UiImage vectorLogoLeftImage, vectorLogoS1Image, vectorLogoS2Image;
    UiVectorImage vectorLogoLeftAsset, vectorLogoS1Asset, vectorLogoS2Asset;
    readonly List<UiVisualElement> vectorLogoLetterMasks = new List<UiVisualElement>();
    readonly List<float> vectorLogoLetterWidths = new List<float>();
    bool vectorLogoActive;
    // Handheld cold start (Red, 2026-10-24): the logo shows on black from the first frame, the same picture as the iOS
    // launch screen; the title room is revealed behind it once it has drawn, and the rest loads while both are held.
    UiVisualElement startupCurtain;
    float startupCurtainAlpha, startupCurtainTarget;
    bool startupReady = true, logoHeldOn;
    const float StartupCurtainSeconds = .6f;
    Material whiteLogoMaterial;
    Sprite brandLogo;
    enum LogoMotionVariation { SlideThenFade, FullLockup }
    [SerializeField, Tooltip("Title logo test: SlideThenFade isolates the SS mark; FullLockup keeps the complete wordmark.")]
    LogoMotionVariation logoMotionVariation = LogoMotionVariation.SlideThenFade;
    [SerializeField, Tooltip("Initial display mode. The player can switch HDR on or off from Display Settings while paused.")]
    bool defaultHdr = true;
    const string HdrPreferenceKey = "FrontRooms.Display.HDR";
    // The settings panel (pause → O): a cursor over rows of comfort, assist and input settings
    // (FrontRoomsSettings, remembered across runs) plus HDR.
    int settingsIndex;
    bool hdrEnabled;
    bool displaySettingsOpen;
    // ---------- The desktop menus (平面视觉's FIX_CARD_VL217 A1–A3 and A7; the T5 legend, 50_tutorial_placard.md §11) ----------
    // The pause and caught cards keep their title (and CAUGHT's subtitle) in overlayText. Every other line is its own
    // Text, laid on a slot that overlayText's own layout leaves for it (a blank line of the line's size), so the card
    // keeps the single block's rhythm: the controls legend and the stats in IBM Plex Mono, the action lines in Bayon
    // 20 ink. Each action line has a transparent hit Image for the mouse (an EventSystem and the menu canvas's
    // GraphicRaycaster, desktop only); A1's yellow marker bar marks the line under the mouse, else the open restart
    // question, else the first line. None of it is built on handhelds (the touch layer draws those cards).
    Text menuLegend, menuStats, menuSubtitle, menuTitleProbe, titleLegend;
    readonly Text[] menuLines = new Text[3];
    readonly Image[] menuHits = new Image[3];
    // Each shown action line's cap middle (canvas y) and width, for the marker.
    readonly float[] menuLineCapY = new float[3], menuLineWidth = new float[3];
    Image menuMarker;
    int menuLineCount, menuHover = -1;
    // The screen size the menus were last laid out for (UpdateDesktopMenu lays them out again on a change) and the HUD
    // canvas's scale factor then (CanvasScaler applies a new size's scale after Update, in preWillRenderCanvases).
    Vector2Int menuLayoutScreen;
    float menuLayoutScale;
    // A3: the settings rows' hit Images over displaySettingsText (hover chooses the row, a click is Enter), and the
    // line of that text each row is on.
    readonly List<Image> settingsHits = new List<Image>();
    readonly List<int> settingsRowLines = new List<int>();
    // T5 on the title: steady at 100 % from the title's first frame (no fade-in), dismissed by the first key or mouse
    // button, then gone with the wordmark over LogoExitSeconds.
    float titleLegendAlpha;
    bool titleLegendDismissed;
    // Lays out the menus' text off screen, as the Text components will (slots, ink bands, widths). Made on first use:
    // a TextGenerator is a native object, which a MonoBehaviour's field initializer may not create.
    TextGenerator menuGeneratorCache;
    TextGenerator MenuGenerator => menuGeneratorCache ?? (menuGeneratorCache = new TextGenerator());
    readonly List<Vector2> menuSlots = new List<Vector2>();
    // overlayText's line numbers of the desktop cards' own lines (PauseText; the caught text in SetPhase).
    const int PauseLegendLine = 2, PauseFirstActionLine = 4, CaughtStatsLine = 3, CaughtActionLine = 5;
    // FIX_CARD §E1: CAUGHT's caps foot to the subtitle's caps top: 0.3 × the title's 64 px caps, as on touch.
    const float CaughtSubtitleGap = 20f;
    // T5's one line (平面视觉): two spaces between a key and its verb, four between pairs. SPRINT names a key, so the
    // never-RUN rule (advice strings) does not apply. Red 2026-10-30: hold Ctrl to run ("按住 Ctrl 奔跑"), hold Shift to walk
    // slowly ("在电脑版慢走的逻辑是按下shift触发"), so the slow walk has its own pair.
    const string ControlsLegend = "WASD  MOVE    MOUSE  LOOK    CTRL  SPRINT    SHIFT  SLOW    E  USE";
    // The title legend's line centre, below the screen centre. Desktop has no title prompt (D01, no "press" text), so
    // it sits under the wordmark's 144-unit box (965 x 192 at LogoScale) with 96 units between: PROPOSED, 平面视觉 places it.
    const float TitleLegendY = -168f;
    // C11 on desktop (FIX_CARD_VL217 §B, the map's one-line prompt until KeyHud B): a broken door is BROKEN, and
    // WON'T SHUT for BrokenDoor.Duration after a try. The broken door tried last, and until when (Time.time).
    const string BrokenPrompt = "BROKEN", WontShutPrompt = "WON'T SHUT";
    FrontRoomsMapWorld.Door brokenTried;
    float brokenTriedUntil;
    Font monoFont, bayonFont, serifFont;
    GameObject overlay, roomPanel, threatPanel, contextPanel, displaySettingsPanel;
    // The desktop settings sheet (tabs, a control per row, explanation, live preview); 设置菜单与画质预设, 2026-10-24.
    FrontRoomsDesktopSettingsView desktopSettings;
    Canvas hudCanvas;
    Rect lastMobileHudSafeArea;
    // The phone HUD's top band (zone, key, threat): where each is laid out, and how far it has given way to a camera or a
    // rounded corner, eased (Red, 2026-10-24).
    Vector2 mobileZoneAt, mobileKeyAt, mobileThreatAt, mobileFlashAt;
    // Their boxes in points (bottom-left origin): what each gives way from, cut to the text it draws.
    Rect mobileZoneBox, mobileKeyBox, mobileThreatBox, mobileFlashBox;
    readonly FrontRoomsTouchMotion.Value mobileZoneGive = new FrontRoomsTouchMotion.Value(), mobileKeyGive = new FrontRoomsTouchMotion.Value(),
        mobileThreatGive = new FrontRoomsTouchMotion.Value(), mobileFlashGive = new FrontRoomsTouchMotion.Value();
    // Down (negative) from a rounded top corner: the zone and the key line under it move together; the threat on its own.
    readonly FrontRoomsTouchMotion.Value mobileZoneDrop = new FrontRoomsTouchMotion.Value(), mobileThreatDrop = new FrontRoomsTouchMotion.Value();
    Vector4 mobileGiveShown;
    Vector2 mobileDropShown;
    bool mobileGiveLaidOut;
    Vector2Int lastMobileHudScreenSize;
    float lastMobileHudPointScale;
    CanvasGroup roomHudGroup, threatHudGroup, contextHudGroup, crosshairHudGroup, keyHudGroup;
    Image overlayImage;
    Outline logoOutline;
    Transform titleWorld;
    FrontRoomsRoomStream roomStream;
    [SerializeField, Tooltip("Optional room prefab/template copied into each streamed title room.")]
    GameObject streamedRoomTemplate;
    float titleLogoAlpha;
    // The trailing-S relay's clock, 0..1 (AdvanceLogoRelay).
    float logoRelayProgress;
    // The wordmark fades out over the first moments of play instead of vanishing.
    bool logoFading;
    const float LogoExitSeconds = .55f;
    bool mapPlay;
    // The run starts in the title's stream rooms (the map's start area). The
    // door out of them opens once the map behind it is built, and shuts for
    // good once the player is well into the map; then the rooms go dark and,
    // when the map has dropped every chunk round them, they are removed.
    bool inStartRooms, leftStartRooms, startDoorOpened;
    GridCoord startDoorCell;
    Vector3 startDoorPoint;
    Light[] streamLights;
    float[] streamLightLevels;
    float streamFade = -1f, startRearZ, startDoorHeldFor;
    // The door opens once the map in sight through it is built and
    // furnished, and never later than this after Space.
    const float StreamFadeSeconds = 1.2f, StartDoorShutDistance = 4f, StartDoorHoldLimit = 3f, StartLampsRiseSeconds = .6f;
    // The stream rooms take a 5-cell (15 m) strip of the map: the 11.76 m room plus the gaps to the map's walls.
    const int StartAreaHalfCells = 2;
    // ---------- C22: floors (Red 2026-10-22; plan v2, ~/FrontRoomsMapWork/notes/c22_stairwell_plan_v2.md) ----------
    // Each floor has one EXIT: a door from N, a map cell, into X, the stairwell's cell, which the map leaves out
    // (SetExitArea). Stepping well into X seals the EXIT behind the player (D14), the stairwell goes dark, the map
    // is reseeded in place for the floor below (D1, D9) with X as its start area, and X's north door opens onto it
    // once the map round it is built (D10). A run ends only when the Relay catches the player.
    // The run's floor (1 = the title's, FloorLabel names it), the floor's map seed (the run seed on floor 1), and
    // its EXIT as FrontRoomsExitPlanner chose it.
    int floor = 1, floorSeed;
    ExitPlan exitPlan;
    // The stairwell in this floor's X, and the one this floor started in (floors 2 and up; floor 1 starts in the
    // title's stream rooms). Both are the game's objects, parented to its root, never the map's (D8).
    FrontRoomsStairwell exitStairwell, startStairwell;

    /// <summary>
    /// The stairwell the current floor began in (C22), null on floor 1. Its NorthDoorCell / NorthDoorPoint are the
    /// floor's start door: the placard mount hangs the B1+ sheet beside it (P1 west, which SeedFor keeps true, G1).
    /// </summary>
    public FrontRoomsStairwell StartStairwell => startStairwell;
    // ---------- A26: the floor-1 start group (Red 2026-10-29; ~/FrontRoomsMapWork/notes/floor1_start_group_plan.md 4.5) ----------
    // With a station module (FrontRoomsLevelProfile.stationModule, empty in the landing: today's start), floor 1 starts in
    // the station, the first map room behind the start door: FrontRoomsStartGroup.Plan picks the map's root and carves the
    // room, its corridor and the EXIT 4 cells straight north of E1 into the floor's map data before Begin (PlanStation).
    // The plan (null: no station on this floor) and the module its template came from.
    StartGroupPlan stationPlan;
    RoomModuleData stationModuleData;
    // Q1 B (Red 2026-10-29 17:21): this run's floor 1 held the station (set at the start, kept down the floors): floors 2
    // and deeper then build no zone keys (ChangeFloor sets the map's ZoneKeysOff after NextFloor). False: today's keys.
    bool stationRun;
    // The station counts as no zone (plan 4.5, 7.5): ZONE 01 is the first back room past E1 or E2. Its cells never count
    // toward the tier either (C04, V1), whatever this says.
    const bool StationCountsAsZone = false;
    // M7 (plan 4.10): floor 1's first send with a station. The gate once due (it latches), why, when (elapsed), the
    // player's feet, path cells out of the station and floor cells explored then; when the send was done.
    bool stationSendDue;
    FrontRoomsMapHunter.StationGate stationSendWhy;
    float stationSendAt, stationSentAt;
    Vector3 stationSendFeet;
    int stationSendPath, stationSendExplored;
    // The onboarding (Red 2026-10-30, "按探索量延后"): the unique cells of this floor the player has stood in, counted as the
    // tier counts them (tierCells: never the title's rooms, a stairwell, or floor 1's station); floor 1's gate reads it
    // (FrontRoomsMapHunter.StationExploreCells). A new floor and a new run start it at 0.
    int floorExplored;
    // The first send's grace (Red 2026-10-30: the run's first send wherever it comes): the W1 bursts already begun when it
    // was placed; the grace holds while no burst has begun since (FirstSendGraceNow).
    int firstSendBursts;
    // C32's W fix: the first send's first warning sample is still to log (FirstSendSample); when the send was placed (elapsed).
    bool firstSendSampleDue;
    float firstSendPlacedAt;
    // M7's fallback clock (A15, 61 9 as 叙事 clarified 10-29): when the start door shut behind the player on floor 1
    // (elapsed; -1 before it). The 150 s count from there, never from Space.
    float startDoorShutAt = -1f;
    // M8 (M3 / M6): when the floor key was taken and when the EXIT's key turn committed (elapsed; -1 before), and how
    // often the locked EXIT was tried before the take. Only a floor-key floor (map.FloorKeyRule) moves them.
    float stationTakeAt = -1f, exitUnlockAt = -1f;
    int exitTriesBeforeTake;
#if UNITY_EDITOR
    // The whole of StartRunInPlace's time, for the autopilot's station rows (the START line prints the same).
    float startRunMs;
#endif
    // The seal and the change of floor: None; Sealing (SealExit asked, the EXIT door shutting, until it latches and
    // locks: the map's ExitDoorSealed); Blackout (the stairwell lamp and the map's exit lamp group going to 0 over
    // BlackoutSeconds, then the floor changes in the dark, D9). The player stands still from the seal to the change.
    enum FloorSwap { None, Sealing, Blackout }
    FloorSwap floorSwap;
    float floorSwapClock;
    // The floor start on floors 2 and up (D10, the stairwell's counterpart of UpdateStartRooms): its own clock from
    // the change, the north door opened, the player out of the stairwell, the north door shut and locked behind
    // them (from then on the Relay's release clock runs), how long the door was held, and the change's own cost.
    float floorClock, floorHeldFor, floorSwapMs;
    bool floorDoorOpened, floorLeftStart, floorStartShut, floorOpenedLate;
    // D9: the blackout's fade, each way.
    const float BlackoutSeconds = .3f;
    // D14: how far past the EXIT door's line, into X, the player must stand for the seal. The shutting leaf (hinged
    // on that line, 0.99 m long) sweeps everything nearer than its length plus the map's player clearance (0.34 m)
    // and half the leaf (0.025 m), 1.355 m: at 1.40 m nothing of the player is in its way, so the shut is never held
    // and the player is never stepped (no DoorPulled). The Relay never enters X (it is no map to it, plan v2 D3) and
    // waits at most at its stand in N, 0.45 m short of the line, so a player this deep is far beyond its catch
    // distance (0.7 m) whatever it does: this replaces D14's "Relay not within 2 m of the EXIT door", which would
    // never let the seal come while a Relay that sees the player in X stands at that stand (step 2's finding).
    const float ExitSealDepth = ModuleUnits.DoorWidth + ModuleUnits.PlayerRadius + .1f;
    // The title camera's drift, eased out when the player takes over, so control does not start with a jolt.
    Vector3 glide;
    const float GlideSeconds = .6f;
    // Cursor lock can report one large mouse delta: ignore the first frames of play.
    int mouseSettleFrames;
    // The runtime stream runs on its own centerline, clear of the edit-mode
    // profile preview at X = 0. It sits on a map cell centre (3 m cells from
    // world 0, so x ≡ 1.5 mod 3): the stream door then opens onto one cell of
    // the map the run continues in.
    public const float TitleCenterX = 256.5f;
    const string EditorPreviewName = "EDITOR_PREVIEW / Room profiles (generated)";
    const float LogoScale = .75f;
    // The trailing S forms are deliberately sequenced instead of sharing the
    // same reveal clock: the near afterimage settles first, then the far one
    // pushes out to create the depth trail in the wordmark.
    const float LogoS1SettleAt = .58f;
    // Start the far afterimage while the near S is in its final approach.
    // The relay is intentionally earlier than the near S settle point so the
    // two forms overlap in motion instead of waiting for a hard hand-off.
    const float LogoS2StartAt = .50f;
    const float GameplayHudFadeSeconds = .9f;
    float gameplayHudAlpha;
    float yaw, pitch, elapsed, stepTime, hunterStepTime, flashTime;
    // The player camera's layers: BaseEye for gameplay, shots and shakes for the picture (audit §6.4).
    FrontRoomsCameraRig rig;
    // The glass break's shot ("Brace, strike, flinch") on the rig.
    FrontRoomsGlassShot glassShot;
    // The door shots on the rig: the unlock (the head dip) when a key turns in a locked door. The map's key-turn owner.
    FrontRoomsDoorShot doorShot;
    // The pane the glass shot belongs to (E on it is the shot's), and this frame's wish to move (before any lock).
    Collider glassPane;
    bool moveIntent;
    // Glass: a hold starts only on a fresh E-down on the pane; tap mode banks progress that drains in real time.
    bool glassArmed;
    float tapCredit, tapReadyAt;
    // The reflection zone last asked for, and the cell it was worked out in (it changes only with the cell).
    FrontRoomsLook.ReflectionZone reflectionZone;
    GridCoord reflectionCell;
    bool reflectionSet;
    string flash = "";
    const float Walk = 3.2f, Run = 5.5f;
    // Sprint stamina: about 5 s of running, refilling after a 1 s breather.
    const float StaminaSeconds = 5f, StaminaRecoverDelay = 1f, StaminaRecoverRate = 1f;
    const float EyeHeight = ModuleUnits.PlayerEye, Reach = 2.4f;
    float stamina = StaminaSeconds, sinceSprint, fallSpeed;
    // Run dry, the player is winded until one stamina segment (1 s) is back: holding Shift on an
    // empty bar would otherwise flicker into one-frame sprints, each able to land a sprint step's noise.
    bool winded;
    // Climbing through a broken window: the sill (0.35 m) is above the step
    // height and the opening (1.65 m) is lower than the player, so walking
    // into the frame vaults through it, ducking under the head.
    const float ClimbSeconds = .6f, ClimbLift = .35f, ClimbDuck = .55f;
    float climbTime = -1f;
    Vector3 climbFrom, climbTo;
    Collider aimed;
    bool aimedHold;
    float holdProgress;
    string prompt;
    // Zones (C22 D13): every new zone of the run in the order it was entered, across floors (its Count is the
    // run's zone total, which the tier, the HUD's ZONE number and the CAUGHT lines read), and the zones of this
    // floor. Zone ids are cells of the floor's map, which is reseeded in place on every floor, so the same id on
    // two floors is two zones: only the floor's own set tells a new zone from one already entered.
    readonly List<GridCoord> zonesVisited = new List<GridCoord>();
    readonly HashSet<GridCoord> floorZones = new HashSet<GridCoord>();
    GridCoord currentZone;
    // Difficulty: the run's tier. The Relay runs on relayTuning, rebuilt every frame from
    // hunterTuning (the Inspector's base) and the tier.
    int tier = 1;
    // C04 a + b (Red, 2026-10-21; V1, 系统设计 P1 2026-10-29): the tier is 1 + the unique cells the player has stood in
    // / 150 (FrontRoomsTierRules.TierForCells), up to 5, and never falls; the 120 s stall rise is gone (until V3's
    // dry-spell sweep nothing replaces it on v1). A cell counts out of the title's rooms, the floor's stairwells (the
    // start one and X) and floor 1's station. Keyed by floor, since every floor reseeds the same grid: a regenerated
    // chunk or a walk back never counts a cell twice, and a new floor's cells are new. The run's totals carry down
    // the floors (C22); a new run clears them. Catches never count (a catch ends the run).
    readonly HashSet<RunCell> tierCells = new HashSet<RunCell>();
    readonly FrontRoomsHunterTuning relayTuning = new FrontRoomsHunterTuning();
    FrontRoomsTierRules TierRules
    {
        get
        {
            var profile = levelProfile != null ? levelProfile : FrontRoomsLevelProfile.Default;
            if (profile.tiers == null) profile.tiers = new FrontRoomsTierRules();
            return profile.tiers;
        }
    }
    int keysTaken, runSeed;
    static bool restart;

    void OnEnable()
    {
        // Edit mode shows the generator itself: one room per profile, built by
        // the same code as the runtime pool. Play Mode builds the live stream
        // instead, so the preview is never saved into the scene or a build.
        if (!Application.isPlaying) BuildEditorPreview();
    }

    void OnDisable()
    {
        if (!Application.isPlaying) DestroyEditorPreview();
    }

    void BuildEditorPreview()
    {
        DestroyEditorPreview();
        BuildMaterials();
        var preview = new GameObject(EditorPreviewName);
        preview.transform.SetParent(transform, false);
        var stream = preview.AddComponent<FrontRoomsRoomStream>();
        stream.roomTemplate = streamedRoomTemplate;
        stream.BuildEditorPreview(WallMaterial(RoomRule.Lobby), FloorMaterial(RoomRule.Lobby), CeilingMaterial(RoomRule.Lobby), trimMat, fixtureMat, darkMat,
            ProfileMaterials(wallMats), ProfileMaterials(floorMats), ProfileMaterials(ceilingMats));
        foreach (var child in preview.GetComponentsInChildren<Transform>(true))
            child.gameObject.hideFlags = HideFlags.DontSave;
    }

    void DestroyEditorPreview()
    {
        for (var i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            if (child.name != EditorPreviewName) continue;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

    static Material[] ProfileMaterials(Dictionary<RoomRule, Material> materials)
    {
        var profiles = (RoomRule[])Enum.GetValues(typeof(RoomRule));
        var result = new Material[profiles.Length];
        for (var i = 0; i < profiles.Length; i++) materials.TryGetValue(profiles[i], out result[i]);
        return result;
    }

    void InitializeFonts()
    {
        var fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        monoFont = Resources.Load<Font>("Fonts/IBMPlexMono-Regular") ?? fallback;
        bayonFont = Resources.Load<Font>("Fonts/Bayon-Regular") ?? fallback;
        serifFont = Resources.Load<Font>("Fonts/SourceSerif4-Variable") ?? fallback;
    }

    /// <summary>The scene's first-person camera, used by Create Scene and as a runtime fallback.</summary>
    public static Camera CreateCamera(Transform parent)
    {
        var camera = new GameObject("First-person camera").AddComponent<Camera>();
        camera.transform.SetParent(parent, false);
        camera.transform.localPosition = new Vector3(TitleCenterX, 1.62f, 0f);
        camera.fieldOfView = 76f; camera.nearClipPlane = .06f; camera.farClipPlane = 80f;
        camera.allowHDR = true;
        camera.allowMSAA = true;
        camera.useOcclusionCulling = true;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = C("22231C"); camera.gameObject.AddComponent<AudioListener>();
        FrontRoomsPostStack.ConfigureCamera(camera);
        return camera;
    }

    // A standalone player can restore a serialized camera's enabled/viewport
    // state from an earlier editor session or a display-mode change. Keep the
    // runtime output deterministic: one active camera, full screen viewport,
    // and no off-screen target texture. This is intentionally a no-op for the
    // normal scene camera and does not alter its lens, clipping, or rendering
    // quality settings.
    static void EnsureRuntimeCamera(Camera camera)
    {
        if (camera == null) return;
        if (!camera.gameObject.activeSelf) camera.gameObject.SetActive(true);
        camera.enabled = true;
        camera.rect = new Rect(0f, 0f, 1f, 1f);
        camera.targetTexture = null;
    }

    /// <summary>The serialized Relay: an editable rig that the hunter brain drives at runtime.</summary>
    public static Transform CreateHunter(Transform parent)
    {
        var relay = new GameObject("Hunter").transform;
        relay.SetParent(parent, false);
        relay.gameObject.AddComponent<FrontRoomsRelayRig>().Configure(Mat("Relay / body", C("2B2928")), Mat("Relay / blank head", C("D8D4C8")), Mat("Relay / detail", C("A99E78")));
        return relay;
    }

    /// <summary>
    /// Scene lighting for a fresh scene. Kept out of Awake so lighting tuned in
    /// the editor's Lighting window survives into Play Mode and builds.
    /// </summary>
    public static void ApplySceneLighting(Transform parent)
    {
        // Trilight bounce and haze shared with the maze and the shipped scene;
        // it also rebuilds URP's ambient probe, without which they do nothing.
        FrontRoomsLook.ApplyAmbient();
        var fill = new GameObject("Soft ambient direction").AddComponent<Light>();
        fill.transform.SetParent(parent);
        fill.type = LightType.Directional; fill.intensity = .22f; fill.color = C("D6D3B4");
        fill.shadows = LightShadows.Soft;
        fill.shadowStrength = .18f;
        fill.shadowBias = .045f;
        fill.shadowNormalBias = .28f;
        fill.shadowNearPlane = .1f;
        fill.transform.rotation = Quaternion.Euler(70f, -30f, 0f);
    }

    void BindHunter()
    {
        if (hunter == null) hunter = transform.Find("Hunter");
        if (hunter == null) hunter = CreateHunter(transform);
        hunterRig = hunter.GetComponent<FrontRoomsRelayRig>();
        hunter.gameObject.SetActive(false);
    }

    void Awake()
    {
        if (!Application.isPlaying) return;

        Application.targetFrameRate = 60;
        // C12c: a close-up's target went away (chunk unload, Level Designer rebuild): end it at once.
        FrontRoomsInspect.AbortRequested -= OnInspectAbortRequested;
        FrontRoomsInspect.AbortRequested += OnInspectAbortRequested;
        // C7 (交互设计's T3): the zoom has an input now (desktop wheel / right button, touch pinch / double tap).
        FrontRoomsInspect.ZoomInputWired = true;
        // Full-resolution textures; MSAA, HDR and shadows come from the URP
        // pipeline asset (Assets/Settings/FrontRooms_URP).
        QualitySettings.globalTextureMipmapLimit = 0;
        Time.timeScale = 1f;
        DestroyEditorPreview();
#if UNITY_EDITOR
        AutopilotStart();
#endif
        // The shared look's ambient and fog (FrontRoomsLook holds the scene's tuned values), which
        // also rebuilds URP's ambient probe: it is only rebuilt from the ambient colours on a bake or here.
        var startupWatch = System.Diagnostics.Stopwatch.StartNew();
        var startupSteps = new System.Text.StringBuilder();
        void Step(string name)
        {
            startupSteps.Append(" · ").Append(name).Append(' ').Append(startupWatch.ElapsedMilliseconds).Append(" ms");
            startupWatch.Restart();
        }
        FrontRoomsLook.ApplyAmbient();
        InitializeFonts();
        Step("look + fonts");
        cam = GetComponentInChildren<Camera>(true);
        if (cam == null) cam = CreateCamera(transform);
        EnsureRuntimeCamera(cam);
        FrontRoomsPostStack.ConfigureCamera(cam);
        FrontRoomsPostStack.Ensure(transform);
        Step("camera");
        BindHunter();
        Step("hunter");
        BuildMaterials();
        Step("materials");
        hdrEnabled = PlayerPrefs.GetInt(HdrPreferenceKey, defaultHdr ? 1 : 0) != 0;
        FrontRoomsSettings.Load();
        ApplyHdrMode(hdrEnabled, false);
        // Render-quality presets and details (Settings › GRAPHICS). The desktop's default (ULTRA) writes nothing.
        FrontRoomsSettingsMenu.HdrGet = () => hdrEnabled;
        FrontRoomsSettingsMenu.HdrSet = on => ApplyHdrMode(on, true);
        FrontRoomsQuality.Load();
        FrontRoomsQuality.Apply();
        BuildHud();
        Step("hud");
        BuildSound();
        Step("sound");
        // A26: with a station module, the map's Prewarm below (or a handheld's, behind its title) also plans one start
        // group on a throwaway cache, so the planner's first-use costs are not paid in the frame of Space.
        FrontRoomsMapWorld.StationWarmUp = StationWarmUp();
        // The map's first-use loads happen before the first frame, not in the
        // frames after Space while the player is watching. A handheld's cold
        // start draws the title first and loads them behind it, with
        // everything held (FinishHandheldStartup).
        var deferLoads = FrontRoomsHandheld.Active && !restart;
        if (!deferLoads)
        {
            FrontRoomsMapWorld.Prewarm();
            Step("map prewarm");
            FrontRoomsHandheld.StartupReady = true;
        }
        BuildTitleCorridor();
        Step("title room");
        SetPhase(Phase.Title);
        if (restart)
        {
            restart = false;
            StartCanonicalStreamedRestart();
        }
        if (deferLoads)
        {
            startupReady = false;
            logoHeldOn = true;
            startupCurtainAlpha = startupCurtainTarget = startupCurtain != null ? 1f : 0f;
            FrontRoomsHandheld.StartupReady = false;
            UpdateStartupCurtain(0f);
            StartCoroutine(FinishHandheldStartup());
        }
        Log("READY · manual title, first-person · display " + Screen.width + "x" + Screen.height
            + " " + Screen.fullScreenMode + " · camera " + (cam != null && cam.enabled ? "active" : "missing")
            + " · startup" + startupSteps + (deferLoads ? " · the rest loads behind the title" : string.Empty));
    }

    /// <summary>
    /// The rest of a handheld's cold start (Red, 2026-10-24: the logo first, the game once its graphics are in, TAP TO
    /// START once everything is). The app comes on screen with the logo on black; the held title room draws behind the
    /// curtain for a few frames (its shaders are compiled by then) and is revealed; then the map's first-use loads run
    /// while nothing moves, and the title starts.
    /// </summary>
    System.Collections.IEnumerator FinishHandheldStartup()
    {
        var waitFrom = Time.realtimeSinceStartup;
        while (!Application.isFocused && !Application.isBatchMode && Time.realtimeSinceStartup - waitFrom < 20f) yield return null;
        for (var i = 0; i < 3; i++) yield return null;
        var roomAt = Time.realtimeSinceStartup;
        startupCurtainTarget = 0f;
        if (startupCurtain != null) yield return new WaitForSecondsRealtime(StartupCurtainSeconds + .05f);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        FrontRoomsMapWorld.Prewarm();
        var prewarm = watch.ElapsedMilliseconds;
        startupReady = true;
        FrontRoomsHandheld.StartupReadyAt = FrontRoomsTouchMotion.Now;
        FrontRoomsHandheld.StartupReady = true;
        Log("READY · handheld start · room shown at " + roomAt.ToString("0.00") + " s · map prewarm " + prewarm + " ms · title from "
            + Time.realtimeSinceStartup.ToString("0.00") + " s (engine clock)");
    }

    /// <summary>The cold start's black curtain under the logo: eased out once the room has drawn.</summary>
    void UpdateStartupCurtain(float dt)
    {
        if (startupCurtain == null) return;
        startupCurtainAlpha = Mathf.MoveTowards(startupCurtainAlpha, startupCurtainTarget, dt / StartupCurtainSeconds);
        var shown = startupCurtainAlpha > .001f;
        startupCurtain.style.display = shown ? UiDisplayStyle.Flex : UiDisplayStyle.None;
        if (shown) startupCurtain.style.opacity = startupCurtainAlpha * startupCurtainAlpha * (3f - 2f * startupCurtainAlpha);
    }

    static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
    static Vector3 V(Vector2 p, float y = 0f) => new Vector3(p.x, y, p.y);
    static Material Mat(string name, Color color, bool emission = false)
        => FrontRoomsSurfaces.Lit(name, color, .12f, 0f, emission ? color * .8f : (Color?)null);

    Material WallMaterial(RoomRule rule) => wallMats.TryGetValue(rule, out var m) ? m : wallMat;
    Material FloorMaterial(RoomRule rule) => floorMats.TryGetValue(rule, out var m) ? m : floorMat;
    Material CeilingMaterial(RoomRule rule) => ceilingMats.TryGetValue(rule, out var m) ? m : ceilingMat;

    /// <summary>
    /// The room profile palette, read from the editable surface materials in
    /// Resources/Surfaces: Level 0 chevron paper, loop-pile carpet and 2'x4'
    /// tiles for Lobby/Shift/Exit; drywall, carpet tiles and 2'x2' tiles for
    /// the Office; the white hospital corridor for Run.
    /// </summary>
    void BuildMaterials()
    {
        wallMats.Clear(); floorMats.Clear(); ceilingMats.Clear();
        foreach (RoomRule rule in Enum.GetValues(typeof(RoomRule)))
        {
            wallMats[rule] = FrontRoomsSurfaces.Room(rule, FrontRoomsSurfaces.Slot.Wall);
            floorMats[rule] = FrontRoomsSurfaces.Room(rule, FrontRoomsSurfaces.Slot.Floor);
            ceilingMats[rule] = FrontRoomsSurfaces.Room(rule, FrontRoomsSurfaces.Slot.Ceiling);
        }
        wallMat = wallMats[RoomRule.Lobby];
        floorMat = floorMats[RoomRule.Lobby];
        ceilingMat = ceilingMats[RoomRule.Lobby];
        trimMat = FrontRoomsSurfaces.CoveBase;
        fixtureMat = FrontRoomsSurfaces.TrofferLens;
        darkMat = FrontRoomsSurfaces.DoorVeneer;
    }

    void BuildTitleCorridor()
    {
        // Fast Enter Play Mode can preserve the previous generated stream.
        // Rebuild it so the title always starts with a fresh wipe/relay clock.
        if (titleWorld != null) StopTitleCorridor();
        titleWorld = new GameObject("Title sequence / recycled corridor").transform;
        // The camera stays in the same generated room through the handoff;
        // the room it is in when Space is pressed is where play starts. Z 0
        // puts every room boundary on a multiple of 3 m (rooms are 12 m,
        // starting 6 m behind the camera), so the door the map is attached
        // behind always lies on a map cell line.
        if (cam != null)
        {
            cam.transform.position = new Vector3(TitleCenterX, EyeHeight, 0f);
            cam.transform.rotation = Quaternion.identity;
        }
        roomStream = titleWorld.gameObject.AddComponent<FrontRoomsRoomStream>();
        roomStream.roomTemplate = streamedRoomTemplate;
        roomStream.Initialize(cam, WallMaterial(RoomRule.Lobby), FloorMaterial(RoomRule.Lobby), CeilingMaterial(RoomRule.Lobby), trimMat, fixtureMat, darkMat,
            foley == null ? doorClip : foley.Doors.hinge,
            foley == null ? null : foley.Doors.latch,
            foley == null ? null : foley.Doors.travel,
            ProfileMaterials(wallMats), ProfileMaterials(floorMats), ProfileMaterials(ceilingMats));
        titleLogoAlpha = 0f;
        logoRelayProgress = 0f;
        logoFading = false;
        mapPlay = false;
        if (cam != null) cam.transform.rotation = Quaternion.identity;
    }

    void UpdateTitleSequence(float dt)
    {
        if (roomStream == null || cam == null) return;
        if (!startupReady)
        {
            // Cold start on a handheld: the room and the logo hold until everything is loaded.
            titleLogoAlpha = 1f;
            UpdateLogoMotion();
            return;
        }
        roomStream.Tick(dt);
        // Historical title clock: the complete wordmark stays present while
        // the first streamed door drives the two trailing-S relays. After a
        // handheld cold start it has been on since the launch screen.
        titleLogoAlpha = logoHeldOn ? 1f : roomStream.LogoVisibility;
        AdvanceLogoRelay(dt);
        UpdateLogoMotion();
    }

    // The wordmark shows on the title and while it fades out as play starts.
    bool LogoShown => phase == Phase.Title || logoFading;

    void UpdateLogoFade(float dt)
    {
        titleLogoAlpha = Mathf.MoveTowards(titleLogoAlpha, 0f, dt / LogoExitSeconds);
        if (titleLogoAlpha <= 0f)
        {
            logoFading = false;
            // On mobile the title logo shares the transparent overlay with
            // the title prompt. Keep that overlay alive for the 0.55 s fade,
            // then release it so the playing HUD is the only owner of the
            // screen. Without this, SetPhase(Playing) disables the parent
            // immediately and the title appears to pop out instead of fading.
            if (FrontRoomsHandheld.Active && phase == Phase.Playing && overlay != null)
                overlay.SetActive(false);
        }
        AdvanceLogoRelay(dt);
        UpdateLogoMotion();
    }

    /// <summary>
    /// The relay follows the first streamed door, but never over a partly
    /// revealed mark. UI Toolkit applies opacity to each element, not to the
    /// lockup as a group, so while the wordmark fades in its solid S cannot
    /// hide the two S stacked under it: they show through it, and move at a
    /// fraction of their own gradient (2993e80 tied the relay to the door, which
    /// opens at about 26–49% of the 4 s fade). The clock holds until the
    /// reveal is complete, then catches up with the door at the door's own
    /// pace. Once started it finishes, even while play fades the mark out.
    /// </summary>
    void AdvanceLogoRelay(float dt)
    {
        if (roomStream == null) return;
        if (titleLogoAlpha < 1f && logoRelayProgress <= 0f) return;
        logoRelayProgress = Mathf.MoveTowards(logoRelayProgress, roomStream.FirstDoorProgress, dt / FrontRoomsRoomStream.DoorOpenSeconds);
    }

    void UpdateLogoMotion()
    {
        if (vectorLogoActive)
        {
            var vectorVisible = LogoShown;
            if (vectorLogoRoot != null) vectorLogoRoot.style.display = vectorVisible ? UiDisplayStyle.Flex : UiDisplayStyle.None;
            if (!vectorVisible || vectorLogoLeftImage == null || vectorLogoS1Image == null || vectorLogoS2Image == null) return;

            // Historical 11:53 title motion: the complete wordmark is
            // stationary and the two afterimage S forms relay from the final
            // S as the first streamed door opens (on the guarded clock of
            // AdvanceLogoRelay). The later per-letter wipe is intentionally
            // disabled; room/gameplay systems are unchanged.
            var relayClock = logoRelayProgress;
            var s1End = logoMotionVariation == LogoMotionVariation.FullLockup ? .66f : LogoS1SettleAt;
            var s2Start = logoMotionVariation == LogoMotionVariation.FullLockup ? .70f : LogoS2StartAt;
            var vectorS1T = Mathf.Clamp01(relayClock / s1End);
            vectorS1T = vectorS1T * vectorS1T * (3f - 2f * vectorS1T);
            var vectorS2T = Mathf.Clamp01((relayClock - s2Start) / (1f - s2Start));
            vectorS2T = vectorS2T * vectorS2T * (3f - 2f * vectorS2T);
            // VectorImage assets are imported at their painted bounds (about
            // 78px wide), so their CSS left value is already the visible
            // glyph position. Do not subtract the original SVG viewBox x
            // coordinates here; that would move both afterimages to the
            // far-left edge of the wordmark.
            var vectorS1X = Mathf.Lerp(798f, 842f, vectorS1T);
            // The far S follows the first afterimage until the relay point,
            // then continues from the first S's actual position at that point.
            // This keeps the earlier hand-off continuous and preserves the
            // intended left-to-right depth relationship.
            var s2StartS1T = Mathf.Clamp01(s2Start / s1End);
            s2StartS1T = s2StartS1T * s2StartS1T * (3f - 2f * s2StartS1T);
            // Historical relay baseline: the far S starts from the current
            // first-S position at the handoff, not from a second, tighter
            // offset. This keeps the two trailing glyphs evenly tracked.
            var vectorS2StartX = Mathf.Lerp(798f, 842f, s2StartS1T);
            var vectorS2X = relayClock < s2Start
                ? vectorS1X
                : Mathf.Lerp(vectorS2StartX, 880f, vectorS2T);
            vectorLogoS1Image.style.left = new UiLength(vectorS1X, UiLengthUnit.Pixel);
            vectorLogoS2Image.style.left = new UiLength(vectorS2X, UiLengthUnit.Pixel);
            // Keep the vector mark on the same fade-in clock as the title
            // corridor. It remains at full opacity after the reveal; the
            // player handoff fades it out (UpdateLogoFade).
            var vectorAlpha = Mathf.Clamp01(titleLogoAlpha);
            vectorLogoLeftImage.style.opacity = vectorAlpha;
            // Relay visibility of the last good relay (2ffb0d8): the two S
            // stay hidden while they sit under the solid S, the near S shows
            // once it starts to peel away, and the far S fades in as it leaves
            // the near S's moving position. Each keeps its own SVG gradient.
            var relayNearAlpha = relayClock > 0f ? 1f : 0f;
            var relayFarAlpha = Mathf.Clamp01((relayClock - s2Start) / .12f);
            vectorLogoS1Image.style.opacity = vectorAlpha * relayNearAlpha;
            vectorLogoS2Image.style.opacity = vectorAlpha * relayFarAlpha;
            return;
        }
        if (logoMotionRoot == null || logoLeftImage == null || logoSlideImage == null) return;
        var visible = LogoShown;
        logoLeftImage.enabled = visible;
        logoSlideImage.enabled = visible;
        if (!visible) return;
        var doorProgressFallback = roomStream == null ? 0f : roomStream.FirstDoorProgress;
        var slideT = doorProgressFallback;
        slideT = slideT * slideT * (3f - 2f * slideT);
        var slideRect = logoSlideImage.rectTransform;
        var restX = -75f;
        var startX = restX - 118f;
        slideRect.anchoredPosition = new Vector2(Mathf.Lerp(startX, restX, slideT), 0f);
        logoLeftImage.color = new Color(1f, 1f, 1f, titleLogoAlpha);
        logoSlideImage.color = new Color(1f, 1f, 1f, titleLogoAlpha);
    }

    void StopTitleCorridor()
    {
        if (titleWorld != null)
        {
            if (Application.isPlaying) Destroy(titleWorld.gameObject);
            else DestroyImmediate(titleWorld.gameObject);
        }
        titleWorld = null;
        roomStream = null;
    }

    void RequestTitleStart()
    {
        if (mapPlay || roomStream == null || cam == null || !startupReady) return;
        FrontRoomsMobileInteractionEvents.MenuConfirmed();
        StartRunInPlace();
    }

    void StartCanonicalStreamedRestart()
    {
        if (roomStream == null) return;
        SetPhase(Phase.Title);
        // A retry starts in the title's first room with a fresh maze (or the
        // same one when Map Seed is set) behind its door, like a first run.
        RequestTitleStart();
        // The title never crawled here, so there is no drift to ease out.
        glide = Vector3.zero;
        Log("RESTART · in the first stream room, maze behind its door");
    }

    /// <summary>
    /// Space on the title. The player takes over where the camera is, in the
    /// stream room they are watching: no glide to an anchor and no white-out.
    /// The stream ends at its first door that is still shut (nothing past it
    /// has been seen), and the Level 0 map is attached behind that door:
    /// placed so the door opens onto one of its cells, with the stream rooms
    /// left out of it as its start area. The map fills in at the normal
    /// streaming budget, round the door first, and the door stays shut until
    /// it is built and furnished, so nothing is ever built in view.
    /// </summary>
    void StartRunInPlace()
    {
        var terminal = roomStream.FirstClosedDoorSequence();
        if (terminal < 0)
        {
            Log("START · no shut stream door is loaded yet");
            return;
        }
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var profile = levelProfile != null ? levelProfile : FrontRoomsLevelProfile.Default;
        runSeed = profile.runSeed != 0 ? profile.runSeed : UnityEngine.Random.Range(1, int.MaxValue);
#if UNITY_EDITOR
        if (autopilot && autopilotSeed != 0) runSeed = autopilotSeed;
#endif
        map = FrontRoomsMapWorld.CreateEmbedded(transform, profile, runSeed);
        // C14 (Red, 2026-10-21): every key is taken by looking at it and pressing E ("E · TAKE KEY"). Phones too
        // (交互设计, 2026-10-22): USE says TAKE on a key under the crosshair, and a tap on a key takes it (HandleMobileLookTap).
        map.KeyPickupNeedsE = true;
        var createMs = watch.Elapsed.TotalMilliseconds;

        // The door line is a map cell line and the stream centreline a cell
        // centre (BuildTitleCorridor, TitleCenterX), so the door opens onto
        // one cell; the stream rooms take the 5-cell strip behind it.
        var centerX = roomStream.CenterX;
        var doorZ = roomStream.RoomStartZ(terminal) + FrontRoomsRoomStream.RoomLength;
        var rearZ = roomStream.RoomStartZ(roomStream.OldestSequence);
        var rows = Mathf.RoundToInt((doorZ - rearZ) / MapGrid.CellSize);
        // A26: with a station module the start group's planner places the map and carves the station into its data;
        // without one, or with no plan, MapRootFor as before (G1, the placard's P1 west, then ChooseExit in PlanExit).
        stationPlan = PlanStation(profile, centerX, doorZ);
        map.transform.SetPositionAndRotation(stationPlan != null ? StationRoot(stationPlan) : MapRootFor(map, centerX, doorZ, rows), Quaternion.identity);
        var placeMs = watch.Elapsed.TotalMilliseconds - createMs;
        startDoorCell = map.CellOf(new Vector3(centerX, 0f, doorZ + MapGrid.CellSize * .5f));
        startDoorPoint = new Vector3(centerX, 0f, doorZ);
        startRearZ = rearZ;
        startDoorHeldFor = 0f;
        var rearCell = map.CellOf(new Vector3(centerX, 0f, rearZ + MapGrid.CellSize * .5f));
        var startArea = new RectInt(startDoorCell.x - StartAreaHalfCells, rearCell.y, StartAreaHalfCells * 2 + 1, startDoorCell.y - rearCell.y);
        map.SetStartArea(startArea, startDoorCell);
        // A26: the station's room, before Begin (FloorHasStation is read at MapRunStarted).
        if (stationPlan != null) HoldStation();
        // Q1 B: HoldStation clears the plan when the map refuses it, so this is "floor 1 holds the station".
        stationRun = stationPlan != null;
        // The facade ends inside the start area's side walls (centred on the cell lines), not on their faces.
        roomStream.EndStreamAt(terminal, (StartAreaHalfCells + .5f) * MapGrid.CellSize + ModuleUnits.WallHalf - .01f);
        roomStream.TerminalDoorHeld = true;

        // The player is a capsule the camera rides on, standing where the
        // camera is. Walls and doors are colliders: the stream rooms' boxes,
        // then the map's.
        var eye = cam.transform.position;
        playerRoot = new GameObject("Player").transform;
        playerRoot.SetParent(transform, false);
        playerRoot.position = ClearStandingSpot(new Vector3(eye.x, eye.y - EyeHeight, eye.z));
        playerBody = playerRoot.gameObject.AddComponent<CharacterController>();
        playerBody.height = ModuleUnits.PlayerHeight;
        playerBody.radius = ModuleUnits.PlayerRadius;
        playerBody.center = new Vector3(0f, ModuleUnits.PlayerHeight * .5f, 0f);
        playerBody.stepOffset = .3f;
        playerBody.skinWidth = .03f;
        rig = FrontRoomsCameraRig.Attach(playerRoot, cam, EyeHeight);
        rig.ResetLayers();
        glassShot = new FrontRoomsGlassShot(rig);
        // The unlock shot owns the map's key turns: E on a keyable door holds it for the shot (keys are off in shipped play).
        doorShot = new FrontRoomsDoorShot(rig, map, cam, playerRoot);
        map.KeyTurnOwner = doorShot;
        // It also presents picked-up keys, only when there is a model to show (none in shipped play: pickups stay instant).
        map.KeyPresenter = doorShot.BeginKeyPickup;
        // HK M-5 (KP-1): with the key HUD attached the pickup hands over to it at HoldUntil (its P4 flight).
        FrontRoomsDoorShot.KeyHandoff -= OnKeyHandoff;
        if (keyHud != null) FrontRoomsDoorShot.KeyHandoff += OnKeyHandoff;
#if UNITY_EDITOR
        // FrontRooms/Map/Play with keys on: this run's map needs keys (the profile asset is untouched). Never for the autopilot.
        if (!autopilot && UnityEditor.SessionState.GetBool(KeysOnSessionKey, false))
        {
            map.DoorsNeedKeysForTools = true;
            Log("KEYS ON · doors need keys this run (FrontRooms/Map/Play with keys on)");
        }
        // FrontRooms/Map/Show placeholder key: the cube key is presented on pickup (timing captures). Never for the autopilot.
        FrontRoomsMapWorld.ShowPlaceholderKey = !autopilot && UnityEditor.SessionState.GetBool(PlaceholderKeySessionKey, false);
        if (FrontRoomsMapWorld.ShowPlaceholderKey) Log("PLACEHOLDER KEY · pickups present the cube key (FrontRooms/Map/Show placeholder key)");
        if (autopilot) map.KeyPickupStarted += AutoKeyPickupStarted;
#endif
        yaw = 0f;
        pitch = 0f;
        fallSpeed = 0f;
        climbTime = -1f;
        stamina = StaminaSeconds;
        FrontRoomsCaptions.Clear();
        winded = false;
        PlayerStamina01 = 1f;
        PlayerWinded = PlayerSprinting = false;
        WarnBursting = false;
        PlayerGait = FrontRoomsGait.Still;
        sinceSprint = 0f;
        glide = Vector3.forward * FrontRoomsRoomStream.TitleSpeed;
        mouseSettleFrames = 2;
        inStartRooms = true;
        leftStartRooms = false;
        startDoorOpened = false;
        streamFade = -1f;
        streamLights = null;
        map.DoorMoved += OnDoorMoved;
        map.GlassBroken += OnGlassBroken;
        map.KeyTaken += OnKeyTaken;
        map.StationKeyTaken += OnStationKeyTaken;
        map.DoorUnlocked += OnStationDoorUnlocked;
        stationSendDue = false;
        startDoorShutAt = stationTakeAt = exitUnlockAt = -1f;
        exitTriesBeforeTake = 0;
        floorExplored = 0;
        firstSendBursts = 0;
        map.DoorPulled += OnDoorPulled;
        // The camera feels the door (audit §3.3, §3.4, §3.7): the lever's push, the latch landing, a locked
        // door's two rattle jolts on their beats, and the Relay's break when it is close.
        map.DoorHandleTurned += (d, _) => Jolt(d.position, DoorJoltRange, FrontRoomsShotTimings.Open.PushImpulseDeg, FrontRoomsShotTimings.Open.PushImpulseDecay);
        map.DoorLatched += (d, p) => Jolt(p, DoorJoltRange, FrontRoomsShotTimings.Open.ShutLatchImpulseDeg, FrontRoomsShotTimings.Open.PushImpulseDecay);
        rattleJolts.Clear();
        map.DoorLocked += p =>
        {
            rattleJolts.Add((elapsed + FrontRoomsShotTimings.Rattle.Jolt1, p));
            rattleJolts.Add((elapsed + FrontRoomsShotTimings.Rattle.Jolt2, p));
        };
        map.DoorRattled += OnDoorRattled;
        map.DoorBroken += p => Jolt(p, FrontRoomsShotTimings.DoorBreak.CameraRange, FrontRoomsShotTimings.DoorBreak.BreakShakeDeg, FrontRoomsShotTimings.DoorBreak.BreakShakeDecay);
        // A live build radius change moves the far plane (in the start rooms UpdateStartRooms sets it every frame).
        map.LiveApplied += () => { if (cam != null && roomStream == null) cam.farClipPlane = map.SightDistance; };
        // C22 (D4, D8): floor 1's EXIT, chosen from the map's data before Begin, so X's chunk is built with X left
        // out the first time and never rebuilt in view; its stairwell stands in X from now on (hidden until X's
        // chunk is built). The seal is the map's (D7): its latch moves the floor change on (OnExitDoorSealed).
        floor = 1;
        floorSeed = runSeed;
        floorSwap = FloorSwap.None;
        floorStartShut = floorDoorOpened = floorLeftStart = false;
        floorClock = floorHeldFor = floorSwapMs = 0f;
        PlanExit(new CellRect(startArea.x, startArea.y, startArea.width, startArea.height), startDoorCell);
        map.ExitDoorSealed += OnExitDoorSealed;
        map.StreamFocus = map.CellCenter(startDoorCell);
        map.StartLampsNorth = 0f;
        map.StartLampsSouth = 0f;
        map.Begin(playerRoot, false);
        // The start rooms take Level 0's reflection at once (no blend from the title's).
        reflectionSet = false;
        UpdateReflectionZone(true);

        tier = 1;
        tierCells.Clear();
        UpdateRelayTuning();
        relay = new FrontRoomsMapHunter(map, relayTuning, playerBody, hunter, runSeed);
        warnLamps = new FrontRoomsWarnLamps(map, runSeed);
        warnLamps.WarnBurst += (centre, lamps) => { WarnBurst?.Invoke(centre, lamps); Event("w1 burst", lamps + " lamps"); };
        FrontRoomsWayCueDriver.ArrowsTurned -= OnArrowsTurned;
        FrontRoomsWayCueDriver.ArrowsTurned += OnArrowsTurned;
        relay.StateChanged += state => Event("hunter", state.ToString());
        // M1 (door break): the visual's stage clock, with the door itself (it falls back on DoorBlow's position without this line).
        relay.DoorBlowStruck += FrontRoomsDoorBreakDriver.BlowStruck;
        relay.DoorBlow += p =>
        {
            if (hunterRig != null) hunterRig.DoorBlow(relay.BlowIndex, relay.BlowCount);
            // Each blow shakes the camera a little harder (the first at the minimum, the last at the maximum), within 8 m of the door.
            var t = relay.BlowCount > 1 ? relay.BlowIndex / (relay.BlowCount - 1f) : 1f;
            Jolt(p, FrontRoomsShotTimings.DoorBreak.CameraRange,
                Mathf.Lerp(FrontRoomsShotTimings.DoorBreak.BlowShakeMinDeg, FrontRoomsShotTimings.DoorBreak.BlowShakeMaxDeg, t),
                FrontRoomsShotTimings.DoorBreak.BlowShake);
            // Under FMOD the sound layer plays the blows (AUDIO_CONTRACT.md).
            if (!FrontRooms.Audio.FrontRoomsFmod.Ready) FoleyDoorBreak(Flat(p));
        };
        relay.Caught += End;
#if FRONTROOMS_RELAY_V2
        // Relay v2 (V3a): the pacing director, ticked before the hunter (FrontRooms3DGame.RelayV2.cs).
        StartRelayDirector();
#endif

        zonesVisited.Clear();
        floorZones.Clear();
        keysTaken = 0;
        playerPos = Flat(playerRoot.position);
        elapsed = 0f;
        mapPlay = true;
        logoFading = true;
        SetPhase(Phase.Playing);
        Event("start", "map seed " + runSeed + ", stream room " + terminal);
        if (exitPlan.found) Event("exit", ExitDetail());
        MapRunStarted?.Invoke(map, relay);
        map.GenerationTier = tier;
        TierChanged?.Invoke(tier);
#if UNITY_EDITOR
        AutopilotRunStarted();
        AutoBotAfterExitRunStarted();
#endif
#if UNITY_EDITOR
        startRunMs = (float)watch.Elapsed.TotalMilliseconds;
#endif
        Log("START · in place in stream room " + terminal + " · maze seed " + runSeed + " behind its door, map root " + map.transform.position + ", door cell " + startDoorCell
            + " · " + watch.Elapsed.TotalMilliseconds.ToString("0.0", CultureInfo.InvariantCulture) + " ms (map " + createMs.ToString("0.0", CultureInfo.InvariantCulture) + ", placing " + placeMs.ToString("0.0", CultureInfo.InvariantCulture) + ")");
        LogStation();
        LogExit();
    }

    // ---------- A26: the floor-1 start group (plan 4.5, 4.11) ----------

#if UNITY_EDITOR
    /// <summary>
    /// Tools (A26): a station module for runs while the profile has none, so the start group can be played before the
    /// visual chat's Front Desk asset exists. FrontRoomsMainScenePlaytest sets it for -autopilotStation (the map's
    /// stand-in, made in memory: never an asset, never in the profile's pool) after each domain reload of that run.
    /// Never in a player.
    /// </summary>
    public static Func<RoomModuleData> StationModuleForTools;

    /// <summary>
    /// Tools (the onboarding, Red 2026-10-30): true turns floor 1's onboarding gate off for captures and smoke runs that
    /// must meet the Relay early, so the gate is M7's old one (the station's key and StationLeaveCells path cells out, or
    /// OnboardOffSendSeconds: FrontRoomsMapHunter's 5-argument StationSendDue). FrontRoomsMainScenePlaytest sets it for
    /// -autopilotOnboardOff after each domain reload of that run, as StationModuleForTools. Never in a player: a build
    /// always plays the onboarding gate.
    /// </summary>
    public static bool OnboardOffForTools;
#endif

    // The module floor 1's station is made from: the profile's (FrontRoomsLevelProfile.stationModule) or, in the editor
    // while it has none, the tools' stand-in (StationModuleForTools). Null: no station, today's start.
    RoomModuleData StationModuleFor(FrontRoomsLevelProfile profile)
    {
#if UNITY_EDITOR
        // -autopilotDoors tests zone keys and locked inner doors on floor 1, which a station floor no longer has (M3,
        // A13): that scenario keeps today's start, as the Relay baselines do.
        if (autopilot && autoDoorWanted) return null;
#endif
        var module = profile.StationData();
#if UNITY_EDITOR
        if (module == null && StationModuleForTools != null) module = StationModuleForTools();
#endif
        return module;
    }

    /// <summary>
    /// A26 (plan 4.5, 4.11): floor 1's start group, planned on the new map's cache before the map is placed. No station
    /// module (the landing), or a Relay baseline that plays without the EXIT: null at once, today's start. Else the
    /// module's template (StartGroupTemplate.From, the station contract checked, plan 4.9) and FrontRoomsStartGroup.Plan
    /// at the stream door's world cell: the zone root, the carve set on the cache (c and c' pinned) and the EXIT
    /// straight north of E1. A refused module or a failed assert is logged as an error, no zone root as a warning, and
    /// the start falls back to today's (MapRootFor, G1 and P1 west, ChooseExit; FloorHasStation false). A plan accepted
    /// with a warning (no root in the budget leads on, or c and c' join only through glass) is logged and kept.
    /// </summary>
    StartGroupPlan PlanStation(FrontRoomsLevelProfile profile, float centerX, float doorZ)
    {
        stationModuleData = null;
        var module = StationModuleFor(profile);
        if (module == null) return null;
#if UNITY_EDITOR
        if (AutoBaselineWithoutExit) return null;
#endif
        var template = StartGroupTemplate.From(module, FrontRoomsMapWorld.KitFootprint, out var why);
        if (template == null)
        {
            Debug.LogError("[FrontRooms3D] STATION · the station module is refused (" + why + "); today's start instead");
            return null;
        }
        // The stream door's map cell with the root at the origin (MapRootFor's probe): the planner's door world cell.
        var door = FrontRoomsMapWorld.CellAt(new Vector3(centerX, 0f, doorZ + MapGrid.CellSize * .5f));
        var plan = FrontRoomsStartGroup.Plan(map.Cache, template, door);
        if (!plan.Ready)
        {
            var line = "[FrontRooms3D] STATION · no start group (" + (plan.error ?? "no EXIT") + "); today's start instead";
            if (plan.found) Debug.LogError(line);
            else Debug.LogWarning(line);
            return null;
        }
        if (plan.warning != null) Debug.LogWarning("[FrontRooms3D] STATION · " + plan.warning);
        stationModuleData = module;
        return plan;
    }

    // Once per domain: the planner's code is compiled by then (a restart's Awake does not warm it again).
    static bool stationWarm;

    /// <summary>
    /// A26: the start group's warm-up for FrontRoomsMapWorld.Prewarm (null: no station module, or done already): the
    /// module's template and one FrontRoomsStartGroup.Plan on a throwaway cache of seed 1 at the title's door cell, so
    /// the planner's code and first-use loads are paid while nothing is on screen, not in the frame of Space.
    /// </summary>
    Action StationWarmUp()
    {
        if (stationWarm) return null;
        var profile = levelProfile != null ? levelProfile : FrontRoomsLevelProfile.Default;
        var module = StationModuleFor(profile);
        if (module == null) return null;
        return () =>
        {
            stationWarm = true;
            var template = StartGroupTemplate.From(module, FrontRoomsMapWorld.KitFootprint, out _);
            if (template == null) return;
            var door = FrontRoomsMapWorld.CellAt(new Vector3(TitleCenterX, 0f, MapGrid.CellSize * 6.5f));
            FrontRoomsStartGroup.Plan(new FrontRoomsMapCache(profile.Generation(1), profile.ModuleData()), template, door);
        };
    }

    // A station plan's map root: its root cell, a multiple of the world period (as MapRootFor's), in metres.
    static Vector3 StationRoot(StartGroupPlan plan) => new Vector3(plan.root.x * MapGrid.CellSize, 0f, plan.root.y * MapGrid.CellSize);

    /// <summary>
    /// A26: the planned station held by the map (SetStation: after SetStartArea, before Begin, so FloorHasStation is
    /// true by MapRunStarted). The start door must open onto the plan's D. A mismatch or a refusal is a bug: it is
    /// logged, the station cleared (its carve off the cache), and the run goes on as today's on this root.
    /// </summary>
    void HoldStation()
    {
        var onD = startDoorCell == stationPlan.layout.door;
        if (onD && map.SetStation(stationPlan, stationModuleData)) return;
        Debug.LogError("[FrontRooms3D] STATION · " + (onD ? "the map refused the plan" : "the start door opens onto " + startDoorCell + ", not the plan's D " + stationPlan.layout.door)
            + "; no station");
        map.ClearStation();
        stationPlan = null;
        stationModuleData = null;
    }

    // The STATION console line, after START (plan 4.5): the template, the root, the door's chunk-local cell, the EXIT,
    // the planner's time and counts, the plan hash. Nothing without a station, so today's console is unchanged.
    void LogStation()
    {
        if (stationPlan == null) return;
        var p = stationPlan;
        var l = p.layout;
        var c = p.mapCarve;
        var o = MapGrid.ChunkOrigin(l.DoorChunk);
        Log("STATION · H" + l.station.h + " E2r" + (l.e2In.y - l.station.y) + " k" + l.corridor + " · root ring " + p.ring + " (" + p.zoneRoots + (p.zoneRoots == 1 ? " zone root" : " zone roots")
            + " tried), D " + l.door + " chunk-local (" + (l.door.x - o.x) + ", " + (l.door.y - o.y) + "), station " + l.station.x + ".." + (l.station.x + l.station.w - 1) + " x "
            + l.station.y + ".." + (l.station.y + l.station.h - 1) + ", EXIT X " + l.exit + " from N " + l.front + ", " + p.exit.distance + " walking cells · plan "
            + p.planMs.ToString("0.0", CultureInfo.InvariantCulture) + " ms · repairs A/B/C " + c.RepairA + "/" + c.RepairB + "/" + c.RepairC + ", " + c.WindowJoins + " window joins, "
            + c.RoomDrops + " rooms dropped, " + c.KeyMoves + " keys moved, " + c.BorderOverrides + " border overrides · hash " + c.Hash);
    }

    /// <summary>
    /// A26 M7 (plan 4.10): floor 1's first send with a station, each frame until it is done. The start door shuts with the
    /// player 4 m into the station, so the release delay after the shut is not used. The gate is the onboarding's (Red
    /// 2026-10-30, StationGateNow): due once the player has explored StationExploreCells (150) unique cells of the floor
    /// (floorExplored), or StationSendSeconds (180) after the start door shut behind the player (never from Space, so a
    /// player still in the title's rooms is never sent at); the station's key plays no part. Once due it latches. The send
    /// is at once (ReleaseNow by FirstSendPolicy's ladder from the station, no delay; asked again next frame while no cell
    /// is found): the run's first send, with its grace. One RELAY line.
    /// </summary>
    void UpdateStationSend()
    {
        var feet = playerRoot.position;
        var cell = map.CellOf(feet);
        if (!stationSendDue)
        {
            var why = StationGateNow(cell);
            if (why == FrontRoomsMapHunter.StationGate.Waiting) return;
            LatchStationGate(why, feet, cell);
        }
        UpdateRelayTuning();
        var eye = rig != null ? rig.BaseEye.position : cam.transform.position;
        if (!relay.ReleaseNow(FrontRoomsMapHunter.FirstSendPolicy.ForStation(map), feet, eye, playerRoot.forward)) return;
        stationSentAt = elapsed;
        FirstSendPlaced();
#if UNITY_EDITOR
        autoStationLeg = relay.State;
        autoStationLegPoint = relay.ListenPoint;
#endif
        var s = relay.LastFirstSend;
        var l = stationPlan.layout;
        var north = map.CellCenter(l.exit) - map.CellCenter(l.front);
        north.y = 0f;
        north.Normalize();
        Log("RELAY · first send (M7) · gate " + StationGateName(stationSendWhy) + " at "
            + stationSendAt.ToString("0.0", CultureInfo.InvariantCulture) + " s, sent " + (stationSentAt - stationSendAt).ToString("0.00", CultureInfo.InvariantCulture) + " s later; the player "
            + stationSendPath + " path cells out, " + Vector3.Dot(stationSendFeet - map.CrossingPoint(l.e1In, l.e1Out), north).ToString("0.0", CultureInfo.InvariantCulture) + " m past E1, "
            + stationSendExplored + " cells explored, key " + (map.StationKeyIsTaken ? "taken" : "not taken")
            + " · rung " + s.rung + " (" + s.rung1 + "/" + s.rung2 + "/" + s.rung3 + " cells), " + s.cell + ", " + s.cells + " walking cells, "
            + s.straight.ToString("0.0", CultureInfo.InvariantCulture) + " m straight, " + FirstSendWText(s) + ", " + FirstSendSide(s) + (s.unseen ? ", unseen" : ", SEEN") + " · first leg " + (s.hunt ? "Hunt to the key" : "Listen"));
    }

    /// <summary>
    /// Floor 1's gate now (the onboarding, Red 2026-10-30, "按探索量延后"): FrontRoomsMapHunter.StationSendDue on the floor's
    /// explored cells (floorExplored) and the fallback clock: Explored, Fallback or Waiting. In the editor under the tools'
    /// -autopilotOnboardOff (OnboardOffForTools): M7's old gate instead, the key and StationLeaveCells path cells out of
    /// <paramref name="cell"/> (Left), or OnboardOffSendSeconds.
    /// </summary>
    FrontRoomsMapHunter.StationGate StationGateNow(GridCoord cell)
    {
#if UNITY_EDITOR
        if (OnboardOffForTools) return FrontRoomsMapHunter.StationSendDue(map, cell, StationFallbackClock, map.StationKeyIsTaken, true);
#endif
        return FrontRoomsMapHunter.StationSendDue(map, StationFallbackClock, floorExplored);
    }

    // The gate is due (it latches): why, when, where the player stood, path cells out of the station and cells explored then.
    void LatchStationGate(FrontRoomsMapHunter.StationGate why, Vector3 feet, GridCoord cell)
    {
        stationSendDue = true;
        stationSendWhy = why;
        stationSendAt = elapsed;
        stationSendFeet = feet;
        stationSendPath = map.StationPathDistance(cell, FrontRoomsStartGroup.Rung1Max);
        stationSendExplored = floorExplored;
    }

    static string StationGateName(FrontRoomsMapHunter.StationGate why) =>
        why == FrontRoomsMapHunter.StationGate.Explored ? FrontRoomsMapHunter.StationExploreCells + " cells of the floor explored"
            : why == FrontRoomsMapHunter.StationGate.Fallback ? FallbackSecondsNow.ToString("0", CultureInfo.InvariantCulture) + " s after the start door shut"
            : why == FrontRoomsMapHunter.StationGate.Left ? "left the station (onboarding off, tools)"
            : why == FrontRoomsMapHunter.StationGate.ThirdZone ? "a third zone (retired)" : "not due";

    // The gate's fallback seconds in force: the onboarding's, or the tools' old 150 s under -autopilotOnboardOff.
    static float FallbackSecondsNow
    {
        get
        {
#if UNITY_EDITOR
            if (OnboardOffForTools) return FrontRoomsMapHunter.OnboardOffSendSeconds;
#endif
            return FrontRoomsMapHunter.StationSendSeconds;
        }
    }

    // A first send's side, for the RELAY lines and the autopilot's rows: the station's far side by its exit field, or behind the player.
    static string FirstSendSide(FrontRoomsMapHunter.FirstSendReport s) =>
        (s.farSide ? (float.IsNaN(s.margin) ? "behind the player" : "far side") : (float.IsNaN(s.margin) ? "not behind the player" : "near side"))
        + (s.station ? "" : " (generic ladder)");

    // C32's W fix (systems design 2026-10-30): a first send's W at the placement for the RELAY lines and the autopilot's rows, and the
    // cells rungs 1-2 left out for W over FirstSendMaxW (30 m).
    static string FirstSendWText(FrontRoomsMapHunter.FirstSendReport s) =>
        "W " + (float.IsPositiveInfinity(s.w) ? "past the field" : s.w.ToString("0.0", CultureInfo.InvariantCulture) + " m") + " at the placement"
        + (s.rung1WOut + s.rung2WOut > 0 ? " (rungs 1/2 left out for W over " + FrontRoomsMapHunter.FirstSendMaxW.ToString("0", CultureInfo.InvariantCulture) + " m: "
            + s.rung1WOut + "/" + s.rung2WOut + " cells)" : "");

    // C32's W fix: rungs 1-2 stand at W at most FirstSendMaxW (rungs 3-4 are the fallbacks), for the autopilot's rows.
    static bool FirstSendWOk(FrontRoomsMapHunter.FirstSendReport s) => s.rung > 2 || s.w <= FrontRoomsMapHunter.FirstSendMaxW;

    // The warning stage and W the hunter has now, for the autopilot's first-send rows (no sample before its first tick on the map).
    string RelayStageNowText() =>
        relay == null || float.IsPositiveInfinity(relay.WarningDistance) ? "no warning sample yet"
            : "stage " + relay.WarnStage + " now (W " + relay.WarningDistance.ToString("0.0", CultureInfo.InvariantCulture) + " m)";

    /// <summary>
    /// The onboarding (Red 2026-10-30): the run's first send, wherever it comes, is gentle while nothing has been sent this
    /// run. Only a run that started on a station floor (stationRun) has the onboarding; a run without one (the tools'
    /// -autopilotDoors, a Relay baseline without -autopilotExit) keeps today's release and no grace.
    /// </summary>
    bool GentleFirstSendDue => stationRun && relay != null && !relay.FirstSendDone;

    /// <summary>
    /// The first send's grace (A26 M7's, moved with the send by Red 2026-10-30): in a station run, while the Relay on the map
    /// came by the run's first send (OnFirstSend), or no send has come yet (a Relay placed by tools before it keeps the grace
    /// floor 1 always gave), until the first W1 burst after it. False everywhere else.
    /// </summary>
    bool FirstSendGraceNow() =>
        stationRun && relay != null && warnLamps != null && (relay.OnFirstSend || !relay.FirstSendDone) && warnLamps.BurstCount <= firstSendBursts;

    // Holds the hunter's ChaseGrace to the rule (each frame before its tick, and again in a frame that sent it).
    void UpdateFirstSendGrace()
    {
        if (relay != null) relay.ChaseGrace = FirstSendGraceNow();
    }

    // The run's first send was just placed (floor 1's gate, the release below it, or v2's FirstSend dispatch): its grace
    // counts W1 bursts from here.
    void FirstSendPlaced()
    {
        if (warnLamps != null) firstSendBursts = warnLamps.BurstCount;
        UpdateFirstSendGrace();
        firstSendSampleDue = true;
        firstSendPlacedAt = elapsed;
    }

    // C32's W fix (systems design 2026-10-30): after each relay tick, the run's first send's first warning sample once it is on the
    // map (v1: the tick after the placement; v2: the step-in's), one RELAY line: stage 1 should start on arrival, since
    // rungs 1-2 stand at W at most FirstSendMaxW (30 m). A send that left the map before a sample logs nothing.
    void FirstSendSample()
    {
        if (!firstSendSampleDue || relay == null) return;
        if (!relay.OnFirstSend)
        {
            firstSendSampleDue = false;
            return;
        }
        if (!relay.Released || float.IsPositiveInfinity(relay.WarningDistance)) return;
        firstSendSampleDue = false;
        var s = relay.LastFirstSend;
        Log("RELAY · the run's first send's first warning sample: stage " + relay.WarnStage + ", W " + relay.WarningDistance.ToString("0.0", CultureInfo.InvariantCulture) + " m, "
            + (elapsed - firstSendPlacedAt).ToString("0.00", CultureInfo.InvariantCulture) + " s after the placement (rung " + s.rung + ", " + FirstSendWText(s) + ")");
    }

    // v1 (the onboarding, Red 2026-10-30): the run's first send came below floor 1, released releaseDelaySeconds after the
    // floor's start door shut and placed by the generic ladder (ReleasePolicy). Its grace from here; one RELAY line.
    void LogFirstSendBelow()
    {
        FirstSendPlaced();
        var s = relay.LastFirstSend;
        Log("RELAY · the run's first send on floor " + floor + " (" + FloorLabel(floor) + "; none on floor 1: the player left it before the onboarding's gate) at "
            + elapsed.ToString("0.0", CultureInfo.InvariantCulture) + " s · rung " + s.rung + " (" + s.rung1 + "/" + s.rung2 + "/" + s.rung3 + " cells), " + s.cell + ", " + s.cells
            + " walking cells, " + s.straight.ToString("0.0", CultureInfo.InvariantCulture) + " m straight, " + FirstSendWText(s) + ", " + FirstSendSide(s) + (s.unseen ? ", unseen" : ", SEEN")
            + " · grace " + (relay.ChaseGrace ? "on" : "off") + " · first leg " + (s.hunt ? "Hunt" : "Listen"));
    }

    // M7's fallback clock: seconds since the start door shut behind the player on floor 1 (0 before it).
    float StationFallbackClock => startDoorShutAt >= 0f ? elapsed - startDoorShutAt : 0f;

    // M6 (FrontRoomsMapWorld.StationKeyTaken, raised by the floor key's take, M3): a sent Relay hunts to the take point
    // (v1: a noise it cannot miss, FrontRoomsMapHunter.HuntStation); one not yet sent starts with that hunt
    // (FirstSendPolicy), and M7's gate counts it. M8: the take's time and the EXIT's tries before it.
    void OnStationKeyTaken(Vector3 at)
    {
        stationTakeAt = elapsed;
        Event("station", "key taken");
        Log("STATION · key taken at " + elapsed.ToString("0.0", CultureInfo.InvariantCulture) + " s after Space (EXIT tried " + exitTriesBeforeTake + " time"
            + (exitTriesBeforeTake == 1 ? "" : "s") + " before)");
#if FRONTROOMS_RELAY_V2
        // A14 (V3a): the station take is a device call; on floor 1 it waits for the gate (HoldFirstCall). V3b: with the
        // Relay on the map it re-dispatches there (RELAY 6: a walk or the handoff while the budget lasts, else queued; V3c:
        // in a chase or a break heard and queued), so v1's HuntStation is not called as well. V4a (RELAY 7): the device is
        // a noise too, as glass is: that is how a chase without sight hears it.
        RelayNoise(RelayNoiseKind.Device, at);
        director?.DeviceTrigger(at, "station");
#else
        relay?.HuntStation(at);
#endif
    }

    // M8: the EXIT's key turn committed on a floor-key floor (DoorUnlocked comes after ExitLockChanged at the commit).
    void OnStationDoorUnlocked(FrontRoomsMapWorld.Door door, GridCoord zone, Vector3 keyhole)
    {
        if (map == null || !map.FloorKeyRule || !map.IsExitDoor(door)) return;
        exitUnlockAt = elapsed;
        Log("STATION · the EXIT unlocked by the floor key at " + elapsed.ToString("0.0", CultureInfo.InvariantCulture) + " s after Space"
            + (stationTakeAt >= 0f ? ", " + (elapsed - stationTakeAt).ToString("0.0", CultureInfo.InvariantCulture) + " s after the take" : ""));
    }

    // ---------- C22: the EXIT, its seal, the change of floor, and the floor start (plan v2 D4, D8-D16) ----------

    /// <summary>
    /// C22 (plan v2 D4, D8): this floor's EXIT, chosen by FrontRoomsExitPlanner from the map's cache alone before the
    /// map builds the chunk X lies in (floor 1: in StartRunInPlace, before Begin; later floors: in ChangeFloor,
    /// right after NextFloor), so X's chunk is built with X left out the first time and nothing is ever rebuilt in
    /// view. The planner's band is the floor's (14-18 walking cells from the start door on floor 1, 14-22 below),
    /// its pick a hash of the floor's map seed, and it generates the chunks its walk touches at the cache's tier,
    /// which is already the run's (NextFloor sets it first; on floor 1 it is 1). The map leaves X out
    /// (SetExitArea: no floor, ceiling, lamp or furniture there, a Relay-free cell, the EXIT door from N swinging
    /// into X) and the stairwell is made at once under the game's root: it outlives the floor, since the next one
    /// starts in it. It stays hidden until X's chunk is built (UpdateStairwells). No EXIT found (none in the 5,400
    /// floors the planner was checked on): the floor has none, and the run goes on there until it is caught.
    /// A Relay baseline (-autopilotBot, editor only) plans none unless the run also asks for -autopilotExit: no X
    /// is left out, no stairwell is made and the planner generates no chunk ahead of the map (at the start tier,
    /// before the baseline locks its own), so the bots play the map their baselines were recorded on.
    /// </summary>
    void PlanExit(CellRect startArea, GridCoord startDoor)
    {
#if UNITY_EDITOR
        if (AutoBaselineWithoutExit)
        {
            exitPlan = default;
            exitPlanMs = 0f;
            exitStairwell = null;
            exitRefused = false;
            return;
        }
#endif
        if (floor == 1 && stationPlan != null)
        {
            // A26: floor 1's EXIT is the start group's, k = 4 cells straight north of E1 (FromStartGroup, asserted on the
            // carved cache by FrontRoomsStartGroup.Plan: one candidate, the band its own distance H + k). Its time is the
            // plan's. Its floor-key lock follows below (M3).
            exitPlan = stationPlan.exit;
            exitPlanMs = stationPlan.planMs;
        }
        else
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            FrontRoomsExitPlanner.BandFor(floor, out var bandMin, out var bandMax);
            exitPlan = FrontRoomsExitPlanner.ChooseExit(map.Cache, startArea, startDoor, floorSeed, bandMin, bandMax);
            exitPlanMs = (float)watch.Elapsed.TotalMilliseconds;
        }
        exitStairwell = null;
        exitRefused = false;
        if (!exitPlan.found) return;
        map.SetExitArea(exitPlan.exitCell);
        // M3 (A13): a floor whose station holds the floor key starts with its EXIT locked, before Begin, so X's first
        // ExitDoorBuilt reads it (ExitLockChanged(null, true): no door built yet). Floor 1 only for now: NextFloor drops
        // the station, so floors 2+ keep today's unlocked EXIT and zone keys until their stations exist (plan 3, Q1 (a)).
        if (map.FloorKeyRule) map.SetExitLocked(true);
        exitStairwell = FrontRoomsStairwell.Create(map, exitPlan.exitCell, transform);
    }

    // The planner's time for this floor's EXIT (it runs in the frame of Space on floor 1, in the change on later ones),
    // and whether the map refused to seal it (SealExit: only a broken door, a guard).
    float exitPlanMs;
    bool exitRefused;

    // What the floor's EXIT is, for the event log and the console.
    string ExitDetail() => "X " + exitPlan.exitCell + " from N " + exitPlan.frontCell + ", " + exitPlan.distance + " walking cells (band "
        + exitPlan.bandMin + "-" + exitPlan.bandMax + (exitPlan.widened ? ", widened, " + exitPlan.nearestGap + " outside the band asked" : "") + (exitPlan.throughGlass ? ", through glass" : "")
        + (exitPlan.deadEnd ? ", dead end" : "") + ", " + exitPlan.candidates + " candidates" + (exitPlan.corridor ? ", a corridor runs past: end-on sign" : "") + ")";

    void LogExit()
    {
#if UNITY_EDITOR
        if (AutoBaselineWithoutExit)
        {
            Log("EXIT · none planned: a Relay baseline plays the map without one (no X, no stairwell; -autopilotExit plans it)");
            return;
        }
#endif
        if (exitPlan.found)
            Log("EXIT · " + FloorLabel(floor) + " (floor " + floor + ", seed " + floorSeed + ") · " + ExitDetail() + " · "
                + exitPlanMs.ToString("0.0", CultureInfo.InvariantCulture) + " ms");
        else
            Debug.LogWarning("[FrontRooms3D] EXIT · floor " + floor + " (seed " + floorSeed + ") has no EXIT: FrontRoomsExitPlanner found no cell in any band");
    }

    /// <summary>
    /// The seal's gate (plan v2 D14), asked every frame after relay.Tick, so a catch in this tick wins (End has
    /// set Caught). The player plays on (phase Playing: not paused, not in a close-up), no shot holds the camera
    /// (a door or key shot, the glass shot or its blend back), no pull or step-in is moving the body, no climb, no
    /// key is being presented, and they stand in X, at least ExitSealDepth past the EXIT door's line (out of the
    /// shutting leaf's way and of any Relay's reach). Harness runs keep the EXIT closed unless the run asked for the
    /// EXIT scenario (-autopilotExit): the autopilot's plays never route into X anyway (it is no map to them), this
    /// only makes sure. A Relay baseline has no EXIT at all (PlanExit), so its map stays the one its baselines were
    /// recorded on; one that asked for -autopilotExit's map has the EXIT, and it stays closed here too.
    /// </summary>
    bool ExitSealAllowed()
    {
        if (floorSwap != FloorSwap.None || exitStairwell == null || exitRefused || !map.HasExitArea || map.ExitSealing) return false;
#if UNITY_EDITOR
        // A bot-after-exit run goes one floor down, by its walker: once the bot plays (B1's shut on) the EXIT stays closed.
        if (HarnessRunning && (!autoExitWanted || autoBotHandedOff)) return false;
#endif
        if (phase != Phase.Playing || FrontRoomsInspect.Active || InspectingNow) return false;
        if (rig != null && rig.InShot) return false;
        if (glassShot != null && (glassShot.Active || glassShot.Blending)) return false;
        if (pullClear.HasValue || climbTime >= 0f) return false;
        if (doorShot != null && (doorShot.UnlockActive || doorShot.KeyClock >= 0f)) return false;
        var feet = playerRoot.position;
        if (!map.InExitArea(map.CellOf(feet))) return false;
        // L11: a Relay that chased them in stops at the door and walks off first, so its steps recede before it parks
        // (its beat ends by itself within FrontRoomsMapHunter's SafeDoorBeatMaxSeconds).
        if (relay != null && relay.SafeDoorBeat) return false;
        return ExitDepth(feet) >= Mathf.Max(ExitSealDepth, relayTuning.catchDistance + .1f);
    }

    // How far a point stands past the EXIT door's line, into X (negative on N's side).
    float ExitDepth(Vector3 p) => exitStairwell == null ? float.NegativeInfinity
        : Vector3.Dot(p - exitStairwell.SouthDoorPoint, exitStairwell.transform.forward);

    /// <summary>
    /// The seal and the change of floor, each frame of play after relay.Tick (D14, D9). At the gate the map seals
    /// the EXIT (SealExit, D7: it shuts the door if it is open and locks it as it latches; nothing opens it again
    /// from the call on) and the player stands still. The latch (OnExitDoorSealed) shows the stairwell's own shut
    /// leaf and starts the blackout: the stairwell's lamp and every map lamp within light range of X (the exit
    /// group: shadowless lamps light straight through the stairwell's walls) go to 0 over BlackoutSeconds, and in
    /// the dark the floor changes (ChangeFloor), so no skin, paper or lamp pop is ever seen.
    /// </summary>
    void UpdateExitSeal(float dt)
    {
        if (floorSwap == FloorSwap.None)
        {
            if (!ExitSealAllowed()) return;
            // Sealing first: an EXIT door already shut and latched locks inside SealExit, and its ExitDoorSealed
            // must find the seal under way.
            floorSwap = FloorSwap.Sealing;
            floorSwapClock = 0f;
            if (!map.SealExit())
            {
                // Only a broken EXIT door refuses (X is no map to the Relay, so it never breaks it): no seal, ever.
                floorSwap = FloorSwap.None;
                exitRefused = true;
                Debug.LogWarning("[FrontRooms3D] EXIT · the EXIT door at " + map.ExitCell + " cannot be sealed (broken); the floor keeps it open");
                return;
            }
            Event("exit", "sealing at " + ExitDepth(playerRoot.position).ToString("0.00", CultureInfo.InvariantCulture) + " m into X");
            return;
        }
        floorSwapClock += dt;
        if (floorSwap == FloorSwap.Sealing)
        {
            // The leaf has 0.55 s to shut and nothing can hold it (the player is out of its sweep, the Relay never in X).
            if (floorSwapClock > 3f && floorSwapClock - dt <= 3f) Debug.LogWarning("[FrontRooms3D] EXIT · the EXIT door has not latched 3 s after the seal");
            return;
        }
        var level = Mathf.Clamp01(1f - floorSwapClock / BlackoutSeconds);
        if (exitStairwell != null) exitStairwell.LampLevel = level;
        map.ExitLamps = level;
        if (floorSwapClock >= BlackoutSeconds) ChangeFloor();
    }

    /// <summary>
    /// The map locked the EXIT door at its latch (D7): the stairwell's own leaf stands shut in its south doorway,
    /// the title's stream rooms go if they still stand (D11; the player, shut in X, cannot see them), and the
    /// blackout begins.
    /// </summary>
    void OnExitDoorSealed(FrontRoomsMapWorld.Door door, Vector3 at)
    {
        if (floorSwap != FloorSwap.Sealing) return;
        if (exitStairwell != null) exitStairwell.Seal();
        EndTitleRooms();
        floorSwap = FloorSwap.Blackout;
        floorSwapClock = 0f;
        Event("exit", "sealed");
    }

    /// <summary>
    /// D11: on floor 1 the EXIT is 14-18 walking cells from the stream rooms' door, so the map round them is usually
    /// still built and they still stand when the player is sealed in; they must never meet the next floor (and
    /// UpdateStartRooms must never run on its map). The rooms, their lamps and the start-room state end here, and
    /// the far plane is the map's. Nothing to do once they have gone (every floor after the first).
    /// </summary>
    void EndTitleRooms()
    {
        StopTitleCorridor();
        streamLights = null;
        streamLightLevels = null;
        streamFade = -1f;
        inStartRooms = false;
        if (cam != null && map != null) cam.farClipPlane = map.SightDistance;
    }

    /// <summary>
    /// One floor deeper, in one frame, in the dark (plan v2 D1, D2, D9-D13, D15), with the player shut in the
    /// stairwell (the title's stream rooms have gone at the latch, D11). In order:
    /// - the Relay is parked (D12: Dormant, hidden, its release clock at 0, everything it knew of the old floor
    ///   forgotten) and the warning lamps let go of the old floor's lamps while those still stand;
    /// - the run's totals stay (the zone list, keys taken, the Relay's doors broken, the tier, the run clock); the
    ///   floor's own zone set and the key label go;
    /// - the next floor's seed is searched (D2: the root cannot move under a sealed player, so the seed is the one
    ///   whose cell X + (0, 1) is Standard Level 0 and leads into the maze), and the map reseeds in place
    ///   (NextFloor: every chunk and record of the old floor gone, the cache made at the run's tier, X the start
    ///   area and X + (0, 1) its door cell);
    /// - the stairwell the old floor started in goes; this one becomes the floor's start stairwell and builds its
    ///   north end wall onto the new map (JoinNextFloor); the new floor's EXIT is chosen (PlanExit);
    /// - the floor start (D10): its clock at 0, the stream focus on the door cell, the start lamps held dark like
    ///   the title's, Begin without building (the held north door covers the streaming, as the title's door does);
    /// - FloorChanged, and the autopilot's floor rows.
    /// </summary>
    void ChangeFloor()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var x = map.ExitCell;
        var feet = playerRoot.position;
        if (!map.InExitArea(map.CellOf(feet))) Debug.LogWarning("[FrontRooms3D] FLOOR · the player is not in X " + x + " at the change (" + feet + ")");
        // D11 ended the title's stream rooms at the latch (OnExitDoorSealed); this only makes sure.
        EndTitleRooms();
        // D12: before NextFloor, while the old floor's lamps still stand (what the warning lamps release is theirs).
#if FRONTROOMS_RELAY_V2
        // RELAY 4 (V3a): Park becomes Disarm; the next floor arms at its start door's shut (系统设计 P2).
        director.Disarm();
#else
        relay.Park();
#endif
        warnLamps?.Reset();
        // What the player aimed at, held or was about to open belongs to the old floor.
        aimed = null;
        prompt = null;
        aimedHold = false;
        holdProgress = 0f;
        glassPane = null;
        glassArmed = false;
        tapCredit = 0f;
        inspectAimed = pendingInspect = null;
        rattleJolts.Clear();
        // D13: the run's totals stay; this floor's zone set, explored cells and key label start again.
#if UNITY_EDITOR
        if (autopilot) AutopilotFloorLeaving();
#endif
        floor++;
        floorZones.Clear();
        floorExplored = 0;
        keyLabelSet = false;
        var profile = map.Profile;
        var seedPlan = FrontRoomsExitPlanner.SeedFor(runSeed, floor, x, profile.Generation(runSeed), profile.ModuleData(), tier, out _);
        floorSeed = seedPlan.seed;
        var door = new GridCoord(x.x, x.y + 1);
        // A26: floor 1's station goes with its floor before NextFloor (FloorHasStation false, the set piece gone, the
        // carve off the old cache). Nothing is rebuilt: NextFloor drops every chunk next.
        if (stationPlan != null || map.HasStation)
        {
            map.ClearStation(false);
            stationPlan = null;
            stationModuleData = null;
        }
        map.NextFloor(floorSeed, tier, new RectInt(x.x, x.y, 1, 1), door);
        // Q1 B (Red 2026-10-29 17:21): below a station floor 1 no zone keys are built (NextFloor cleared the flag; set
        // before Begin, so no chunk of this floor is built yet). A run with no station keeps today's zone keys.
        map.ZoneKeysOff = stationRun;
        // The stairwell the old floor began in is two floors back by now: gone. This one is the new floor's start.
        if (startStairwell != null) Destroy(startStairwell.gameObject);
        startStairwell = exitStairwell;
        startStairwell.JoinNextFloor();
        startStairwell.SetVisible(true);
        PlanExit(new CellRect(x.x, x.y, 1, 1), door);
        // D10: the floor start. The stream rooms' fields keep naming the start door (the autopilot's walk out reads them).
        startDoorCell = door;
        startDoorPoint = startStairwell.NorthDoorPoint;
        floorClock = 0f;
        floorHeldFor = 0f;
        floorDoorOpened = floorLeftStart = floorStartShut = floorOpenedLate = false;
        map.StreamFocus = map.CellCenter(door);
        map.StartLampsNorth = 0f;
        map.StartLampsSouth = 0f;
        map.Begin(playerRoot, false);
        reflectionSet = false;
        UpdateReflectionZone(true);
        floorSwap = FloorSwap.None;
        floorSwapMs = (float)watch.Elapsed.TotalMilliseconds;
        Event("floor", FloorLabel(floor) + " (floor " + floor + "), seed " + floorSeed + " (" + seedPlan.tries + (seedPlan.tries == 1 ? " try" : " tries")
            + (seedPlan.standard ? "" : ", not Standard") + "), start door " + door + ", " + floorSwapMs.ToString("0.0", CultureInfo.InvariantCulture) + " ms");
        if (exitPlan.found) Event("exit", ExitDetail());
        SaveEvents();
        Log("FLOOR " + floor + " · " + FloorLabel(floor) + " · seed " + floorSeed + " (" + seedPlan.tries + " of " + FrontRoomsExitPlanner.MaxSeedTries + " tried"
            + (seedPlan.standard ? "" : ", no Standard start found") + "), start door " + door + ", tier " + tier + ", " + zonesVisited.Count + " zones, "
            + elapsed.ToString("0.0", CultureInfo.InvariantCulture) + " s · change " + floorSwapMs.ToString("0.0", CultureInfo.InvariantCulture) + " ms (EXIT "
            + exitPlanMs.ToString("0.0", CultureInfo.InvariantCulture) + ")");
        LogExit();
        FloorChanged?.Invoke(map, relay, floor);
#if UNITY_EDITOR
        AutopilotFloorStarted();
#endif
    }

    /// <summary>
    /// The floor start on floors 2 and up (plan v2 D10): UpdateStartRooms for the stairwell the floor began in.
    /// Its own clock (the run clock is long past 3 s); the lamp back after the blackout; the north door held shut
    /// until everything in sight through it is built and furnished (ReadyAround), never longer than
    /// StartDoorHoldLimit, then opened away from the player; the stream focus dropped and the lamps south of the
    /// door line raised once the player is out; the lamps past the line raised with the swing; the door shut and
    /// locked for good once the player is StartDoorShutDistance out (from then on the Relay's release clock runs:
    /// the gate in UpdateMapPlay); and when the map has dropped every chunk round the stairwell, the stairwell goes
    /// and the map takes the cell back, as with the title's rooms.
    /// </summary>
    void UpdateFloorStart(float dt)
    {
        if (floor <= 1 || startStairwell == null) return;
        floorClock += dt;
        var feet = playerRoot.position;
        var inside = map.InStartArea(map.CellOf(feet));
        if (startStairwell.LampLevel < 1f) startStairwell.LampLevel = Mathf.MoveTowards(startStairwell.LampLevel, 1f, dt / BlackoutSeconds);
        if (!inside && !floorLeftStart)
        {
            floorLeftStart = true;
            map.StreamFocus = null;
            Event("stairwell", "left");
        }
        if (floorLeftStart) map.StartLampsSouth = Mathf.MoveTowards(map.StartLampsSouth, 1f, dt / StreamFadeSeconds);
        if (!floorDoorOpened)
        {
            // The lamp back first; then everything in sight through the door: every chunk in range built and furnished.
            if (floorClock < BlackoutSeconds) return;
            var ready = map.ReadyAround(map.BuildRadius);
            if (!ready && floorClock <= StartDoorHoldLimit) return;
            if (!ready) Log("FLOOR " + floor + " · map round the stairwell door not ready after " + StartDoorHoldLimit + " s; opening anyway");
            startStairwell.Open(feet);
            floorDoorOpened = true;
            floorOpenedLate = !ready;
            floorHeldFor = floorClock;
            Event("start door", "opens");
            return;
        }
        if (!floorStartShut)
        {
            // The maze's lamps by the door come up with the swing.
            map.StartLampsNorth = Mathf.MoveTowards(map.StartLampsNorth, 1f, dt / StartLampsRiseSeconds);
            if (!inside && Vector2.Distance(Flat(feet), Flat(startStairwell.NorthDoorPoint)) >= StartDoorShutDistance) startStairwell.Close();
            if (!startStairwell.NorthDoorLocked) return;
            floorStartShut = true;
            map.StartLampsNorth = 1f;
            Event("start door", "shut");
            return;
        }
        // Behind its locked door; when no chunk round it is built (far out of sight), it goes and X is map again.
        if (map.StartAreaBuilt) return;
        Destroy(startStairwell.gameObject);
        startStairwell = null;
        map.ClearStartArea();
        Event("stairwell", "removed");
    }

    // The player stands in the stairwell the floor started in (floors 2 and up), where, as in the title's rooms, no zone counts yet.
    bool InFloorStairwell => floor >= 2 && map.InStartArea(map.CellOf(playerRoot.position));

    /// <summary>
    /// The player is in the front rooms (A26, plan 4.5): the title's stream rooms, or, on floor 1 with a station
    /// (FloorHasStation), the station's room. The HUD names both LEVEL 0 / FRONT ROOMS (Standard Level 0), neither
    /// counts as a zone (StationCountsAsZone), and both take Level 0's reflection. With no station it is inStartRooms.
    /// </summary>
    bool InFrontRooms => inStartRooms || (floor == 1 && map != null && map.FloorHasStation && playerRoot != null && map.InStation(map.CellOf(playerRoot.position)));

    /// <summary>
    /// D8: a stairwell's renderers and lamp are on only while X's chunk is built (nothing of it shows where the
    /// map round it is not there); the one the floor started in also while the player is in it (the change and
    /// the hold run before its chunk is built again). Colliders stay on throughout (the stairwell's own rule).
    /// Every frame of a run, paused or not: the map streams under the pause card too.
    /// </summary>
    void UpdateStairwells()
    {
        if (map == null) return;
        if (exitStairwell != null)
        {
            var show = map.IsChunkBuiltAt(exitStairwell.Cell) || floorSwap != FloorSwap.None;
            if (show != exitStairwell.Visible) exitStairwell.SetVisible(show);
        }
        if (startStairwell != null)
        {
            var show = map.IsChunkBuiltAt(startStairwell.Cell) || (playerRoot != null && map.InStartArea(map.CellOf(playerRoot.position)));
            if (show != startStairwell.Visible) startStairwell.SetVisible(show);
        }
    }

    /// <summary>
    /// Where the map's root goes: a multiple of 192 m (ModuleUnits.WorldPeriod,
    /// so the printed ceiling grid stays on the troffers), chosen so that the
    /// row of cells the stream door opens onto, all five across the start
    /// area, is Standard-height Level 0 (the stream room's 2.9 m ceiling and
    /// Lobby paper carry straight on), and so that the way on is straight
    /// ahead: the door's open leaves stand 1.12 m into its cell, too close to
    /// the cell's side walls to pass, so the cell's far edge must be open and
    /// lead into the maze (the stream rooms' strip of <paramref name="rows"/>
    /// cells behind the door can cut cells off from the rest of their chunk).
    /// The map is infinite and fixed by its seed; this only picks which part
    /// of it the door leads into. G1 (tutorial phase 0): the evacuation plan
    /// hangs on the start wall west of the door (P1 west), so a root whose door
    /// cell's west edge is Open comes first; a Standard row without it is the
    /// fallback. FrontRoomsExitPlanner.RootFor is the same search in cells.
    /// </summary>
    static Vector3 MapRootFor(FrontRoomsMapWorld map, float centerX, float doorZ, int rows)
    {
        var period = ModuleUnits.WorldPeriod;
        var probe = new Vector3(centerX, 0f, doorZ + MapGrid.CellSize * .5f);
        Vector3? reachable = null, standardOnly = null;
        for (var ring = 0; ring <= 24; ring++)
        for (var b = -ring; b <= ring; b++)
        for (var a = -ring; a <= ring; a++)
        {
            if (Mathf.Max(Mathf.Abs(a), Mathf.Abs(b)) != ring) continue;
            var root = new Vector3(a * period, 0f, b * period);
            var door = FrontRoomsMapWorld.CellAt(probe - root);
            var area = new RectInt(door.x - StartAreaHalfCells, door.y - rows, StartAreaHalfCells * 2 + 1, rows);
            var standard = true;
            for (var dx = -StartAreaHalfCells; dx <= StartAreaHalfCells && standard; dx++)
            {
                var zone = map.ZoneOf(new GridCoord(door.x + dx, door.y));
                standard = zone.height == ZoneHeight.Standard && zone.theme == ZoneTheme.Level0;
            }
            if (!standard && (reachable.HasValue || standardOnly.HasValue)) continue;
            if (standard && standardOnly.HasValue && !FrontRoomsExitPlanner.StartDoorWestOpen(map.Cache, door)) continue;
            var ahead = new GridCoord(door.x, door.y + 1);
            var onward = map.Cache.Edge(door, ahead);
            if (onward == EdgeKind.Wall || onward == EdgeKind.Window || !LeadsIntoMaze(map.Cache, ahead, area, door)) continue;
            if (standard && FrontRoomsExitPlanner.StartDoorWestOpen(map.Cache, door)) return root;
            if (standard) standardOnly = root;
            else reachable = root;
        }
        if (standardOnly.HasValue)
        {
            Log("START · no Standard Level 0 row with an open west edge at the door (G1, the placard's P1 west); using one without");
            return standardOnly.Value;
        }
        Log("START · no Standard Level 0 row found for the stream door" + (reachable.HasValue ? "; using one that only leads on" : "; the map stays at the origin"));
        return reachable ?? Vector3.zero;
    }

    static readonly GridCoord[] Steps4 = { new GridCoord(1, 0), new GridCoord(-1, 0), new GridCoord(0, 1), new GridCoord(0, -1) };

    /// <summary>
    /// From <paramref name="start"/>, through anything but a wall and never
    /// back into the door cell or the start area, the walk reaches a good
    /// part of the maze (150 cells; a pocket cut off by the strip is far smaller).
    /// </summary>
    static bool LeadsIntoMaze(FrontRoomsMapCache cache, GridCoord start, RectInt area, GridCoord door)
    {
        var seen = new HashSet<GridCoord> { start, door };
        var queue = new Queue<GridCoord>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            foreach (var step in Steps4)
            {
                var next = cell + step;
                if (seen.Contains(next) || area.Contains(new Vector2Int(next.x, next.y))) continue;
                if (cache.Edge(cell, next) == EdgeKind.Wall) continue;
                seen.Add(next);
                if (seen.Count >= 150) return true;
                queue.Enqueue(next);
            }
        }
        return false;
    }

    /// <summary>
    /// Feet under the camera, unless the body would stand in a door leaf or
    /// return there; then the nearest clear spot back along the corridor.
    /// </summary>
    static Vector3 ClearStandingSpot(Vector3 feet)
    {
        Physics.SyncTransforms();
        var r = ModuleUnits.PlayerRadius;
        for (var step = 0; step <= 30; step++)
        {
            var p = feet + Vector3.back * (step * .1f);
            // Above the floor and below the ceiling: only walls and doors count.
            if (!Physics.CheckCapsule(p + Vector3.up * (r + .2f), p + Vector3.up * (ModuleUnits.PlayerHeight - r), r, ~0, QueryTriggerInteraction.Ignore))
                return p;
        }
        return feet;
    }

    /// <summary>
    /// The stream rooms the run starts in. Their door into the map opens once
    /// the map behind it is ready; leaving them hands the camera the map's
    /// sight distance. Once the player is well into the map the door swings
    /// shut for good (the Relay is let loose from then on), the rooms' lamps
    /// fade out, and when the map has dropped every chunk round them they are
    /// removed and the map takes the ground back.
    /// </summary>
    void UpdateStartRooms(float dt)
    {
        if (roomStream == null) return;
        var feet = playerRoot.position;
        var inside = map.InStartArea(map.CellOf(feet));
        if (!inside && !leftStartRooms)
        {
            leftStartRooms = true;
            map.StreamFocus = null;
            Event("start rooms", "left");
        }
        // Out of the rooms, the player sees into them only through the door,
        // and walks straight into the cells beside them: bring those lamps up.
        if (leftStartRooms) map.StartLampsSouth = Mathf.MoveTowards(map.StartLampsSouth, 1f, dt / StreamFadeSeconds);
        inStartRooms = inside;
        // Far enough to see down the stream rooms to their rear wall, never
        // further past the door than the map is built (SightDistance of it).
        cam.farClipPlane = inside
            ? Mathf.Clamp(feet.z - startRearZ + 2f, map.SightDistance, map.SightDistance + Mathf.Max(0f, startDoorPoint.z - feet.z))
            : map.SightDistance;
        if (streamFade < 0f)
        {
            roomStream.Tick(dt);
            if (!startDoorOpened)
            {
                // Everything in sight through the door: every chunk in range built and furnished.
                var ready = map.ReadyAround(map.BuildRadius);
                if (!ready && elapsed > StartDoorHoldLimit && roomStream.TerminalDoorHeld) Log("START · map round the door not ready after " + StartDoorHoldLimit + " s; opening anyway");
                roomStream.TerminalDoorHeld = !ready && elapsed <= StartDoorHoldLimit;
                if (roomStream.TerminalDoorHeld && inside && Vector2.Distance(Flat(feet), Flat(startDoorPoint)) < 4f) startDoorHeldFor += dt;
                if (roomStream.TerminalDoorOpen)
                {
                    startDoorOpened = true;
                    roomStream.TerminalDoorHeld = false;
                    Event("start door", "opens");
                }
            }
            else
            {
                // The maze's lamps by the door come up with the swing, as the stream's rooms do.
                map.StartLampsNorth = Mathf.MoveTowards(map.StartLampsNorth, 1f, dt / StartLampsRiseSeconds);
                if (!inside && Vector2.Distance(Flat(feet), Flat(startDoorPoint)) >= StartDoorShutDistance)
                    roomStream.CloseTerminalDoor();
            }
            if (!roomStream.TerminalDoorShut) return;
            // Nothing of the stream can be seen any more: fade its lamps out,
            // gently, for their light on the map's side and the hum that reads them.
            streamFade = 0f;
            streamLights = titleWorld.GetComponentsInChildren<Light>();
            streamLightLevels = new float[streamLights.Length];
            for (var i = 0; i < streamLights.Length; i++) streamLightLevels[i] = streamLights[i].enabled ? streamLights[i].intensity : 0f;
            startDoorShutAt = elapsed;
            Event("start door", "shut");
            return;
        }
        if (streamFade < 1f)
        {
            streamFade = Mathf.Min(1f, streamFade + dt / StreamFadeSeconds);
            map.StartLampsNorth = 1f;
            for (var i = 0; i < streamLights.Length; i++)
            {
                if (streamLights[i] == null) continue;
                streamLights[i].intensity = streamLightLevels[i] * (1f - streamFade);
                if (streamFade >= 1f) Destroy(streamLights[i]);
            }
            return;
        }
        // One room a frame, behind the shut door; the last one keeps facing the maze.
        if (roomStream.DisposeOneRoom() || map.StartAreaBuilt) return;
        // Every chunk round the rooms is gone, far out of sight: so is the last of them.
        StopTitleCorridor();
        map.ClearStartArea();
        inStartRooms = false;
        Event("start rooms", "removed");
    }

    void OnDoorMoved(Vector3 p)
    {
        // Under FMOD the sound layer plays the door (AUDIO_CONTRACT.md).
        if (!FrontRooms.Audio.FrontRoomsFmod.Ready) Sound(doorClip, Flat(p), .8f);
#if FRONTROOMS_RELAY_V2
        // V4a: the door noise, one loudness until V5 splits it (RelayDoorLoudness, FrontRooms3DGame.RelayV2.cs).
        RelayNoise(RelayNoiseKind.Door, p);
#elif UNITY_EDITOR
        AutopilotNoise(AutoCauseDoor, p, hunterTuning.doorNoiseRadius);
#else
        relay?.Noise(p, hunterTuning.doorNoiseRadius);
#endif
#if FRONTROOMS_RELAY_V2
        RelayDoorMoved(p);
#endif
        Event("door", Flat(p).ToString());
    }

    void OnGlassBroken(Vector3 p)
    {
        FrontRoomsMobileInteractionEvents.GlassShattered();
        FoleyDoorBreak(Flat(p));
#if FRONTROOMS_RELAY_V2
        // V4a (RELAY 7): the on-map ears hear it at the v2 loudness.
        RelayNoise(RelayNoiseKind.Glass, p);
#elif UNITY_EDITOR
        AutopilotNoise(AutoCauseGlass, p, hunterTuning.glassNoiseRadius);
#else
        relay?.Noise(p, hunterTuning.glassNoiseRadius);
#endif
#if FRONTROOMS_RELAY_V2
        // RELAY 6 (V3a): a broken window is a device: it calls at once (RELAX included) or waits in the queued slot.
        director?.DeviceTrigger(p, "glass");
#endif
        // Phones break the line before TO, so the wrap does not strand it (平面视觉, 2026-10-22); the words are unchanged.
        Flash(FrontRoomsLanguage.T(FrontRoomsHandheld.Active ? "GLASS BROKEN  /  WALK INTO THE FRAME\nTO CLIMB THROUGH" : "GLASS BROKEN  /  WALK INTO THE FRAME TO CLIMB THROUGH"));
        Event("glass", Flat(p).ToString());
    }

    void OnKeyTaken(GridCoord zone)
    {
        keysTaken++;
        // HK M-8 (KP-1b, DECISIONS #37, Red 2026-10-29): on the INSTANT pickup (nothing presents the key: shipped play
        // until D1.6's real key) the key HUD's B glyph plays the full lab P4 by itself: 0.5 s held at the hand position
        // (0.65, 0.30), then into the key panel's slot. A presented key hands over at HoldUntil instead (KP-1, M-5).
        if (keyHud != null && (doorShot == null || !doorShot.Presenting(zone)))
            keyHud.PickupInstant(HudIdentity(zone), rig != null ? rig.BaseFov : cam.fieldOfView);
        // The key panel fading in is the pickup cue until keys open doors (audit F1, 1.8).
        Event("key", zone == FrontRoomsMapWorld.FloorKeyZone ? "floor key" : zone.ToString());
#if UNITY_EDITOR
        if (autopilot) AutoKeyTaken(zone);
#endif
    }

    static Vector2 Flat(Vector3 p) => new Vector2(p.x, p.z);

    /// <summary>A stick tilt under this is the slow gait (a pad's small tilt, as the touch stick's inner circle).</summary>
    const float SlowTilt = .5f;

    /// <summary>
    /// The run goes ahead only (Red, 2026-10-30: "奔跑时目前可以只能向前奔跑"), as the touch stick's run line does: it starts
    /// within FrontRoomsTouchControls.RunHalfAngle (70°) of straight ahead and, once running, holds until the move turns
    /// sideways or back. Keys, pads and touch all pass through it.
    /// </summary>
    static bool RunAhead(Vector2 move, bool running)
    {
        if (move.sqrMagnitude <= .01f) return false;
        return running ? move.y > .01f : Vector2.Angle(Vector2.up, move) <= FrontRoomsTouchControls.RunHalfAngle;
    }

    /// <summary>
    /// The player's move at its gait's size, the touch stick's on every input (Red, 2026-10-30): SLOW held (hold Shift)
    /// or a tilt under SlowTilt is the slow gait at FrontRoomsTouchControls.SlowSpeedShare (1.44 m/s), anything else the
    /// full walk (3.2 m/s; the run's own speed is applied by the caller).
    /// </summary>
    static Vector2 GaitMove(Vector2 move, bool slow)
    {
        var size = move.magnitude;
        if (size <= .001f) return Vector2.zero;
        return move / size * (slow || size < SlowTilt ? FrontRoomsTouchControls.SlowSpeedShare : 1f);
    }

    void UpdateMapPlay(float dt)
    {
        if (map == null || playerBody == null) return;
        Vector2 local;
        bool sprintHeld;
        // SLOW held (Shift) and whether the move is the player's (the editor bots' steer is not sized by GaitMove).
        var slowHeld = false;
        var playerMove = true;
        var lookDegrees = 0f;
        var input = FrontRoomsInput.ReadFrame();
#if UNITY_EDITOR
        if (autopilot) { AutopilotSteer(dt, out local, out sprintHeld); playerMove = false; }
        else
#endif
        {
            // A43: in a live close-up FrontRoomsInspect.Tick takes the look (above); the base yaw and pitch stay.
            if (phase == Phase.Inspect) lookDegrees = 0f;
            else if (mouseSettleFrames > 0) mouseSettleFrames--;
            // While a shot holds the look, mouse movement is dropped, not saved for later.
            else if ((rig == null || !rig.LookLocked) && (glassShot == null || !glassShot.LookLocked))
            {
                // The unlock holds the look (scale 0, then back over 0.2 s): the dropped share is lost, not queued.
                var lookScale = doorShot != null ? doorShot.LookScale : 1f;
                var dx = input.Look.x * lookScale;
                var dy = input.Look.y * lookScale;
                yaw += dx;
                pitch = Mathf.Clamp(pitch - dy, -75f, 75f);
                lookDegrees = Mathf.Abs(dx) + Mathf.Abs(dy);
                // A presented key in view draws the base pitch toward it (≤ 6°, 0.2 s) while the mouse is idle; any look ends it.
                if (doorShot != null) pitch = Mathf.Clamp(pitch + doorShot.AimAssistStep(dt, lookDegrees <= FrontRoomsShotTimings.KeyPickup.MouseIdleDeg), -75f, 75f);
            }
            local = input.Move;
            sprintHeld = input.SprintHeld;
            slowHeld = input.SlowHeld;
        }
        // C25: E and S that ended a close-up are latched until released (no door on the exit E, no walking backward on the exit S).
        if (useLatch && !EHeld()) useLatch = false;
        if (backLatch) { if (input.Move.y < -.1f) local.y = Mathf.Max(local.y, 0f); else backLatch = false; }
        if (moveLatch) { if (input.Move.sqrMagnitude > .01f) local = Vector2.zero; else moveLatch = false; }
        moveIntent = local.sqrMagnitude > .01f;
        if (rig != null)
        {
            rig.ClampLook(ref yaw, ref pitch);
            if (rig.MoveLocked) { local = Vector2.zero; sprintHeld = false; }
            // S cancels a shot that allows it (Esc is pause and never cancels).
            if (rig.InShot && input.ShotBackDown) rig.Consume(ShotInput.Back);
        }
        if (glassShot != null)
        {
            // The glass shot: a demote (not a cancel) when the Relay sees or chases; asking to move (a key held,
            // not only pressed) or a look ends its soft lock. The step-in moves the body on its own velocity.
#if FRONTROOMS_RELAY_V2
            // V4b (RELAY 8): camera shots read TargetAcquired or Spotting 0.5, not any glimpse.
            var threat = relay != null && relay.Threat.Any;
#else
            var threat = relay != null && (relay.SeesPlayer || relay.State == HunterState.Chase);
#endif
            glassShot.Tick(dt, threat, moveIntent, lookDegrees);
            glassShot.ClampLook(ref yaw, ref pitch);
            if (glassShot.MoveLocked) { local = Vector2.zero; sprintHeld = false; }
        }
        if (doorShot != null)
        {
            // The unlock: its own clock, the Relay's cancel rule (UnlockThreat), and its move lock (also on the autopilot's steer).
            doorShot.Tick(dt, UnlockThreat());
            if (doorShot.MoveLocked) { local = Vector2.zero; sprintHeld = false; }
        }
        // C22 (D14, D9): from the seal until the floor has changed, under a second, the player stands where the
        // seal found them, deep in X: the EXIT door shuts behind them and the stairwell goes dark. The look stays free.
        if (floorSwap != FloorSwap.None) { local = Vector2.zero; sprintHeld = false; }
        playerRoot.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (rig != null) rig.SetBase(pitch);
        else cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        // A pull (a shut from the leaf's side, A12): the player steps clear onto the latch side while the leaf waits.
        var stepVelocity = Vector3.zero;
        if (pullClear.HasValue)
        {
            pullClearTime += dt;
            // A door pull steps at PullStepSpeed; the glass and key-turn step-ins follow a smoothstep from where they began.
            var smooth = pullIsGlass || pullIsUnlock;
            var stepIn = pullIsUnlock ? FrontRoomsShotTimings.Unlock.StepInSeconds : FrontRoomsShotTimings.GlassBreak.StepInSeconds;
            var target = smooth ? Vector3.Lerp(glassStepFrom, pullClear.Value, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(pullClearTime / stepIn))) : pullClear.Value;
            var toClear = target - playerRoot.position;
            toClear.y = 0f;
            // Arrive exactly (the last step lands on the spot): the spot sits right at the leaf's reach, so stopping short leaves the body in its way.
            var arrived = toClear.magnitude < 1e-3f && (!smooth || pullClearTime >= stepIn);
            if (arrived || pullClearTime > PullStepSeconds || (relay != null && (relay.SeesPlayer || relay.State == HunterState.Chase))) pullClear = null;
            else
            {
                local = Vector2.zero;
                sprintHeld = false;
                stepVelocity = Vector3.ClampMagnitude(toClear / Mathf.Max(dt, 1e-4f), smooth ? GlassStepMaxSpeed : PullStepSpeed);
            }
        }
        // One gait rule on every platform (Red, 2026-10-30: "三端保持一致"): the run goes only ahead, as on the touch stick
        // ("奔跑时目前可以只能向前奔跑"), and never from a stick's small tilt; then the move takes its gait's size.
        var sprinting = sprintHeld && RunAhead(local, PlayerSprinting) && (!playerMove || local.sqrMagnitude >= SlowTilt * SlowTilt)
            && stamina > 0f && !winded;
        if (playerMove) local = GaitMove(local, slowHeld && !sprinting);
        var wish = playerRoot.right * local.x + playerRoot.forward * local.y;
        if (wish.sqrMagnitude > 1f) wish.Normalize();
        sprinting &= wish.sqrMagnitude > .01f;
        if (sprinting)
        {
            stamina = Mathf.Max(0f, stamina - dt);
            sinceSprint = 0f;
            if (stamina <= 0f) winded = true;
        }
        else
        {
            sinceSprint += dt;
            if (sinceSprint > StaminaRecoverDelay) stamina = Mathf.Min(StaminaSeconds, stamina + StaminaRecoverRate * dt);
            if (winded && stamina >= StaminaSeconds / staminaSegments.Length) winded = false;
        }
        PlayerStamina01 = stamina / StaminaSeconds;
        PlayerWinded = winded;
        PlayerSprinting = sprinting;
        PlayerGait = GaitOf(sprinting, wish.sqrMagnitude);
        fallSpeed = playerBody.isGrounded ? -1f : fallSpeed - 9.81f * dt;
        var before = playerPos;
        if (climbTime >= 0f || TryStartClimb(wish)) Climb(dt);
        else playerBody.Move((wish * (sprinting ? Run : Walk) + glide + stepVelocity + Vector3.up * fallSpeed) * dt);
        glide = Vector3.MoveTowards(glide, Vector3.zero, FrontRoomsRoomStream.TitleSpeed / GlideSeconds * dt);
        playerPos = Flat(playerRoot.position);
        if (Vector2.Distance(before, playerPos) > .001f)
        {
            stepTime += dt;
            if (stepTime > (sprinting ? .3f : .5f))
            {
                stepTime = 0f;
                FoleyFootstep(playerPos, FrontRoomsFoleyActor.Player, FrontRoomsFoleySurface.Carpet, sprinting, sprinting ? .48f : .15f);
#if FRONTROOMS_RELAY_V2
                // V4a (RELAY 7): a sprint step at the v2 loudness.
                if (sprinting) RelayNoise(RelayNoiseKind.Sprint, playerRoot.position);
#elif UNITY_EDITOR
                if (sprinting) AutopilotNoise(AutoCauseSprint, playerRoot.position, hunterTuning.sprintNoiseRadius);
#else
                if (sprinting) relay?.Noise(playerRoot.position, hunterTuning.sprintNoiseRadius);
#endif
#if FRONTROOMS_RELAY_V2
                // RELAY 6 (V3a): a sprint step is +3 Attention (V5 adds the 0.3 s cadence and the 0.5 m test).
                if (sprinting) RelayDisturb(FrontRoomsRelayDisturbanceKind.Sprint, playerRoot.position);
#endif
                // A walk step is heard nearby on every platform; the slow gait stays silent (Mobile.cs).
                if (!sprinting) WalkStepNoise();
            }
        }
        UpdateStartRooms(dt);
        // C22 (D10): floors 2 and up start in the stairwell instead.
        UpdateFloorStart(dt);
        if (!inStartRooms && !InFloorStairwell)
        {
            // C04 a (V1): the tier counts unique cells (tierCells), never the station's (whatever StationCountsAsZone
            // says) nor X's stairwell.
            var cell = map.CellOf(playerRoot.position);
            if (!(floor == 1 && map.InStation(cell)) && !map.InExitArea(cell) && tierCells.Add(new RunCell(floor, cell)))
            {
                // The onboarding (Red 2026-10-30): the same cells are this floor's explored count (floor 1's gate).
                floorExplored++;
                RaiseTier(TierRules.TierForCells(tierCells.Count), "cells");
            }
            // Zones count from the first step into the map (out of the title's rooms, or, below, out of the stairwell; on
            // floor 1 with a station, out of the station too: A26, StationCountsAsZone). They name the HUD's ZONE nn and
            // the logs; since C04 they raise no tier.
            if (StationCountsAsZone || !InFrontRooms)
            {
                var zone = map.ZoneOf(cell).id;
                if (zone != currentZone || floorZones.Count == 0)
                {
                    currentZone = zone;
                    if (floorZones.Add(zone))
                    {
                        zonesVisited.Add(zone);
                        Event("zone", zone.ToString());
                    }
                }
            }
        }
        if (phase == Phase.Playing) UpdateAim(dt);   // A43: a live close-up aims at nothing (E, S and Esc are the close-up's)
#if UNITY_EDITOR
        if (autoBaseline) AutoBaseBeforeRelayTick();
        var tickWatch = autopilot ? System.Diagnostics.Stopwatch.StartNew() : null;
#endif
#if FRONTROOMS_RELAY_V2
        // Relay v2 (V3a): armed at the floor's start-door shut, the director ticked before the hunter, floor 1's first call
        // held for its gate (UpdateRelayV2, FrontRooms3DGame.RelayV2.cs). No timed release.
        UpdateRelayV2(dt);
#else
        // A26 M7: on floor 1 with a station the start door shuts with the player inside it, so the first send has its own
        // gate (UpdateStationSend: the onboarding's, Red 2026-10-30) instead of the release delay after the shut.
        var stationSend = floor == 1 && stationPlan != null && !relay.Released;
        if (stationSend) UpdateStationSend();
        // Dormant until the door back to the stream rooms has shut behind the
        // player: its release clock only starts once they are in the maze for good.
        // C22 (D10): below the title's floor the door is the stairwell's north door.
        if (relay.Released || (!stationSend && (floor <= 1 ? streamFade >= 0f || roomStream == null : floorStartShut)))
        {
            UpdateRelayTuning();
            // The onboarding (Red 2026-10-30): a station run whose first send has not come (the player left floor 1 before its
            // gate) is released here as on any floor, releaseDelaySeconds after the start door shut, but placed gently.
            var wasReleased = relay.Released;
            if (!wasReleased) relay.ReleasePolicy = GentleFirstSendDue ? relay.ReleasePolicy ?? FrontRoomsMapHunter.FirstSendPolicy.Generic() : null;
            // The first send's grace: no chase before the first W1 burst after it unless the player is within 6 m.
            UpdateFirstSendGrace();
            // The Relay sees the player's base eye, never a shot or shake pose; PlayerSeesRelay's reference frustum takes
            // that eye's yaw and pitch (RELAY §8, V2).
            relay.Tick(dt, playerRoot.position, rig != null ? rig.BaseEye.position : cam.transform.position,
                rig != null ? rig.BaseEye.rotation * Vector3.forward : cam.transform.forward);
            if (!wasReleased && relay.Released && relay.OnFirstSend) LogFirstSendBelow();
            FirstSendSample();
            // The hunter's Threat reading frees the player from any shot that allows it, at once (audit §3.2; RELAY §8,
            // V2: TargetAcquired recently or Spotting, never a glimpse; v1: a chase, or its sight within 6 m).
            if (rig != null && relay.Threat.Any) rig.CancelForRelay();
        }
#endif
        // W1: stage 1 and 2 dip the lamps of the player's room; a chase (v1's lock-on) starts no new burst.
#if FRONTROOMS_RELAY_V2
        // V4b (RELAY 9.4): from the lock-on, not the chase: no new bursts from TargetAcquired on (the current one finishes).
        warnLamps?.Tick(dt, relay.Released ? relay.WarnStage : 0, relay.LockedOn, playerRoot.position, relay.Position);
#else
        warnLamps?.Tick(dt, relay.Released ? relay.WarnStage : 0, relay.State == HunterState.Chase, playerRoot.position, relay.Position);
#endif
        WarnBursting = warnLamps != null && warnLamps.Bursting;
#if UNITY_EDITOR
        if (tickWatch != null && tickWatch.Elapsed.TotalMilliseconds > autoRelayTickMs)
        {
            autoRelayTickMs = (float)tickWatch.Elapsed.TotalMilliseconds;
            autoRelayTickAt = autoPlayClock;
        }
#endif
        // C22 (D14): the EXIT's seal and the change of floor, after relay.Tick (a catch this tick wins).
        UpdateExitSeal(dt);
        UpdateRelayRig(dt);
        UpdateReflectionZone(false);
        // C5b: an E press on a close-up target commits with this frame's Relay state.
        if (pendingInspect != null)
        {
            var t = pendingInspect;
            pendingInspect = null;
            if (CanShowInspect(t) && CanPressInspect()) BeginInspect(t);
        }
        // Picture layers last, after gameplay has read BaseEye this frame.
        rig?.Tick(dt);
        // A presented key rides the camera as rendered (after the rig), so it never lags a frame.
        doorShot?.PlaceKey();
    }

    // ---------- EXAMINE (C4, C5): the gate, entry and exit ----------

    bool HarnessRunning
    {
        get
        {
#if UNITY_EDITOR
            return autopilot || autoBaseline;      // the game's own fields; no static the autopilot must set
#else
            return false;
#endif
        }
    }

    /// <summary>Hard checks (C4): false hides the prompt.</summary>
    bool CanShowInspect(FrontRoomsInspectable t) =>
        t != null && !HarnessRunning && mapPlay && phase == Phase.Playing
        && (glassShot == null || !(glassShot.Active || glassShot.Blending))
        && climbTime < 0f && !pullClear.HasValue && (!ThreatPerceivable() || !FrontRoomsInspect.ThreatGated(t))   // C36: no threat gate on the door peek
        && FrontRoomsInspect.CanOpen(t, rig);

    /// <summary>Transient checks (C4): false keeps the prompt but swallows E.</summary>
    bool CanPressInspect() => !useLatch && inspectCooldown <= 0f && (rig == null || !rig.InShot);

    /// <summary>
    /// The prompt gate (RELAY §8 amended 2026-10-14; computer_ui 20_design §6.1, CONTRACT L4; V2, 2026-10-29): a close-up
    /// prompt hides only for threats the player perceives, and returns only on a change they perceive
    /// (FrontRoomsMapHunter.ThreatPerceivable: a chase and 3 s of its fade, a heard door break, Spotting while its steps
    /// are audible, RelayOnScreen on this device's render camera). Replaces RelayBlocksInspect (a chase, a door break at
    /// any range, or the flat ±60° PlayerSeesRelay with no light term). Never SeesPlayer alone.
    /// </summary>
    bool ThreatPerceivable() => relay != null && relay.ThreatPerceivable(cam);

    void BeginInspect(FrontRoomsInspectable t)
    {
        ReleaseGlass();
        holdProgress = 0f;
        inspectShot = FrontRoomsInspect.Open(t, rig);
        if (inspectShot == null) return;
        InspectingNow = true;
        // A43: a reading close-up pauses (InspectDef.Pauses); a looking one (look kits, keys, the station TV ad) runs live.
        inspectLive = !FrontRoomsInspect.CurrentPauses;
        SetPhase(Phase.Inspect);                            // timeScale 0 (C8) when it pauses; 1 when it runs live (A43)
        Inspecting?.Invoke(true);
        Event("inspect", t.Id);
    }

    void EndInspect(bool byBack)
    {
        if (!InspectingNow) return;
        FrontRoomsInspect.Close(rig);                       // rig.EndShot: rotation 0.15 s, position 0.40 s
        InspectingNow = false;                              // before SetPhase, so C8's guard does not recurse
        // A43: backing out of a live close-up is one move, so a held S (or the pull-back's finger) walks on; a reading one latches it (C25).
        var wasLive = inspectLive;
        inspectLive = false;
        SetPhase(Phase.Playing);                            // timeScale 1, settle frames
        Inspecting?.Invoke(false);
        useLatch = true;
        backLatch = byBack && !wasLive;
        inspectCooldown = FrontRoomsShotTimings.Inspect.Cooldown;
    }

    void OnInspectAbortRequested()
    {
        var shot = inspectShot;
        EndInspect(false);
        if (shot != null) rig?.CancelShot(shot, 0f);
    }

    void ResumeFromPause() => SetPhase(pausedFrom);

    /// <summary>Desktop (Red, 2026-10-24): Esc opens the settings page itself, with no PAUSED card in between, and leaving it resumes.</summary>
    bool DesktopPauseIsSettings => !FrontRoomsHandheld.Active && desktopSettings != null;

    /// <summary>Phones too (Red, 2026-10-24: "手机页直接进设置"): on every device a pause is the settings sheet.</summary>
    bool PauseIsSettings => DesktopPauseIsSettings || (FrontRoomsHandheld.Active && mobileTouch != null);

    /// <summary>Esc, Back or the sheet's RESUME: the settings sheet is the pause, so closing it resumes the run.</summary>
    void CloseSettings()
    {
        if (PauseIsSettings && phase == Phase.Paused) ResumeFromPause();
        else ToggleDisplaySettings();
    }

    /// <summary>
    /// The reflection cube for where the player stands (FrontRoomsLook, the visual chat's): the
    /// zone's kind, or the dead-lamp cube under a dead, dim or missing lamp, keyed by the lamp's
    /// temperament so a flicker never pumps it. Worked out again only when the player's cell
    /// changes; Look crossfades over its own 0.5 s.
    /// </summary>
    void UpdateReflectionZone(bool force)
    {
        if (map == null || playerRoot == null) return;
        var cell = map.CellOf(playerRoot.position);
        if (!force && reflectionSet && cell == reflectionCell) return;
        reflectionCell = cell;
        FrontRoomsLook.ReflectionZone zone;
        // A cell the map leaves out (the title's rooms, a stairwell: C22) has no zone or lamp of its own to read.
        // A26's hook: floor 1's station (InFrontRooms) takes Level 0's too, until the visual chat supplies its own cube
        // (61 7); its Dim and Off lamps are the room's design, not dead lamps.
        if (inStartRooms || map.IsReserved(cell) || InFrontRooms) zone = FrontRoomsLook.ReflectionZone.Level0;
        else
        {
            var info = map.ZoneOf(cell);
            var lamp = map.LampModeOf(cell);
            if (lamp == ModuleLamp.Dead || lamp == ModuleLamp.Dim || lamp == ModuleLamp.Off) zone = FrontRoomsLook.ReflectionZone.DeadLamp;
            else if (info.theme == ZoneTheme.Office) zone = FrontRoomsLook.ReflectionZone.Office;
            else if (info.height == ZoneHeight.Tall) zone = FrontRoomsLook.ReflectionZone.Tall;
            else zone = FrontRoomsLook.ReflectionZone.Level0;
        }
        if (!force && reflectionSet && zone == reflectionZone) return;
        reflectionZone = zone;
        FrontRoomsLook.SetZoneReflection(zone, force || !reflectionSet ? 0f : .5f);
        reflectionSet = true;
    }

    // A pull's step: where to, and for how long the leaf waits for it (MapWorld.DoorPullBeatSeconds).
    Vector3? pullClear;
    float pullClearTime;
    const float PullStepSeconds = FrontRoomsMapWorld.PullStepSeconds, PullStepSpeed = FrontRoomsMapWorld.PullStepSpeed;

    // Flat reach from the body to any point of a leaf, open or shut: the use ray plus the open leaf's tip.
    const float DoorJoltRange = Reach + ModuleUnits.DoorWidth * 1.2f;
    // A locked door's rattle jolts, due at Rattle.Jolt1 and Jolt2 after the try (run time, so a pause holds them).
    readonly List<(float at, Vector3 p)> rattleJolts = new List<(float, Vector3)>();

    /// <summary>
    /// A shut from the side the leaf is open to (the one pull left with A12's double-acting doors; opens never
    /// pull): step the player to the clear spot beside the latch, with the controller (so walls still hold),
    /// during the pull beat. Not while the Relay chases or sees them: they keep control, and the leaf rests on
    /// them until they move.
    /// </summary>
    void OnDoorPulled(FrontRoomsMapWorld.Door door, Vector3 clear)
    {
        if (playerRoot == null || (relay != null && (relay.SeesPlayer || relay.State == HunterState.Chase))) return;
        if (Flat(clear - playerRoot.position).sqrMagnitude < 1e-4f) return;
        // Never a step that cannot finish in the pull beat (an open from across the cell): they keep control instead.
        if (Flat(clear - playerRoot.position).magnitude > PullStepSpeed * PullStepSeconds) return;
        pullClear = clear;
        pullClearTime = 0f;
        pullIsGlass = false;
        pullIsUnlock = false;
    }

    // The pull step's mover also carries the glass and key-turn step-ins; these say whose step it is, and where the step-in began.
    bool pullIsGlass, pullIsUnlock;
    Vector3 glassStepFrom;
    // The step-in's peak speed: its smoothstep over 0.2 s peaks at 1.5× the average of a 0.7 m step.
    const float GlassStepMaxSpeed = 6f;

    /// <summary>Let go of the glass: the shot blends back, and a step-in still under way stops where it is.</summary>
    void ReleaseGlass()
    {
        if (glassShot == null || !glassShot.Active || glassShot.Shattered) return;
        glassShot.Release();
        if (pullIsGlass) pullClear = null;
    }

    /// <summary>Walking into a broken window's frame from up to 0.95 m away starts a climb through it.</summary>
    bool TryStartClimb(Vector3 wish)
    {
        if (wish.sqrMagnitude < .1f) return false;
        var feet = playerRoot.position;
        var here = map.CellOf(feet);
        foreach (var step in new[] { new GridCoord(1, 0), new GridCoord(-1, 0), new GridCoord(0, 1), new GridCoord(0, -1) })
        {
            var next = here + step;
            if (!map.IsBrokenWindow(here, next)) continue;
            var center = map.CrossingPoint(here, next);
            var dir = map.CellCenter(next) - map.CellCenter(here);
            dir.y = 0f;
            dir.Normalize();
            var rel = feet - center;
            rel.y = 0f;
            var along = Vector3.Dot(rel, dir);
            var lateral = (rel - dir * along).magnitude;
            if (along < -.95f || along > 0f || lateral > .45f || Vector3.Dot(wish.normalized, dir) < .5f) continue;
            climbFrom = feet;
            climbTo = center + dir * .75f;
            climbTo.y = feet.y;
            climbTime = 0f;
            FoleyFootstep(Flat(feet), FrontRoomsFoleyActor.Player, FrontRoomsFoleySurface.Carpet, false, .3f);
            glassShot?.ClimbStarted();
            PlayerClimbed?.Invoke(center);
            return true;
        }
        return false;
    }

    void Climb(float dt)
    {
        climbTime += dt;
        var t = Mathf.Clamp01(climbTime / ClimbSeconds);
        var arc = Mathf.Sin(t * Mathf.PI);
        playerBody.enabled = false;
        playerRoot.position = Vector3.Lerp(climbFrom, climbTo, t * t * (3f - 2f * t)) + Vector3.up * (arc * ClimbLift);
        playerBody.enabled = true;
        // The duck under the window head is the body's stance, so the aim and the Relay see it too.
        if (rig != null) rig.StanceDrop = arc * ClimbDuck;
        else cam.transform.localPosition = new Vector3(0f, EyeHeight - arc * ClimbDuck, 0f);
        fallSpeed = 0f;
        if (t < 1f) return;
        climbTime = -1f;
        if (rig != null) rig.StanceDrop = 0f;
        else cam.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
    }

    // E from the keyboard, or from the autopilot's glass scenario (editor only).
    bool EDown()
    {
#if UNITY_EDITOR
        // M3: the autopilot's own press (the floor key's take, the EXIT's unlock), one frame.
        if (autopilot && Time.frameCount == autoKeyEFrame) return true;
        if (autoGlassActive || autoDoorActive || autoBaseline) return autoEDown;
#endif
        return FrontRoomsInput.UseDown;
    }

    bool EHeld()
    {
#if UNITY_EDITOR
        if (autopilot && Time.frameCount == autoKeyEFrame) return true;
        if (autoGlassActive || autoDoorActive || autoBaseline) return autoEHeld;
#endif
        return FrontRoomsInput.UseHeld;
    }

    /// <summary>
    /// E went down on a pane: the map begins the hold at the base-eye hit, the
    /// glass shot starts (or resumes at the stage reached), and the body steps
    /// in to stand 0.55 m from the pane (not at Camera motion off).
    /// </summary>
    void BeginGlass(Collider pane, Vector3 hitPoint, Pose eye)
    {
        var window = map.BeginGlassHold(pane, hitPoint, eye.rotation * Vector3.forward);
        if (window == null || glassShot == null) return;
        mobileLastGlassBeat = 0f;
        FrontRoomsMobileInteractionEvents.GlassHoldStarted();
        glassPane = pane;
        var impact = map.GlassImpact(window);
        var feet = playerRoot.position;
        var inward = window.root.forward * Mathf.Sign(Vector3.Dot(window.root.forward, impact - feet));
        glassShot.Begin(impact, inward, window.hold / FrontRoomsShotTimings.GlassBreak.HoldSeconds);
        if (FrontRoomsSettings.CameraMotionScale <= 0f) return;
        // A door pull's step clearing the leaf keeps the mover: the body stays put for this hold.
        if (pullClear.HasValue && !pullIsGlass) return;
        var stand = FrontRoomsGlassShot.StandPoint(feet, impact, inward);
        if (Flat(stand - feet).sqrMagnitude < 1e-4f) return;
        // The step-in rides the pull step's mover (swept by the controller), eased over GlassBreak.StepInSeconds.
        pullClear = stand;
        pullClearTime = 0f;
        pullIsGlass = true;
        pullIsUnlock = false;
        glassStepFrom = feet;
    }

    /// <summary>
    /// E on a door (what the crosshair is on, or the touch layer's look tap: HandleMobileLookTap): the map's Use,
    /// and the unlock shot when it answers with a key turn. Every input path reaches doors through here. With
    /// look+E keys (map.KeyPickupNeedsE, off) E on a key comes here too: Use takes it and hands it to the presenter.
    /// </summary>
    FrontRoomsMapWorld.UseResult UseDoor(Collider c)
    {
        if (map == null) return FrontRoomsMapWorld.UseResult.None;
        var result = map.Use(c);
        if (result == FrontRoomsMapWorld.UseResult.KeyTurn) BeginUnlock(map.DoorOf(c));
        // HK M-4: only for the door under the crosshair (a touch look-tap can use another door).
        if (keyHud != null && c == aimed)
        {
            if (result == FrontRoomsMapWorld.UseResult.Rattled) keyHud.NoKey();
            else if (result == FrontRoomsMapWorld.UseResult.KeyTurn) keyHud.Unlocking();
            else if (result == FrontRoomsMapWorld.UseResult.Broken) keyHud.BrokenTry();
        }
        return result;
    }

    /// <summary>
    /// A key turns in a locked door (plan §3.2.1): the unlock shot starts on the keyhole of the face the
    /// player stands on, in place while chunks are still building or during a climb; then the body steps
    /// in toward the lock (capped like the glass step-in, never while a door pull's step runs; not at Camera
    /// motion Off or in place). With A12 every key turn first turns the shut leaf away from the player, so the
    /// unlock is always a push and the leaf never comes toward the camera.
    /// </summary>
    void BeginUnlock(FrontRoomsMapWorld.Door door)
    {
        if (door == null || doorShot == null || playerRoot == null) return;
        var feet = playerRoot.position;
        var face = FrontRoomsMapWorld.FaceOf(door, feet);
        var lockPose = map.LockPose(door, feet);
        var normal = FrontRoomsMapWorld.LockNormal(door, feet);
        // A12: the map turned the shut leaf away from the key's side (Use's key turn), so this is a push from either face.
        var pull = FrontRoomsMapWorld.OnSwingSide(door, feet);
        var inPlace = !map.Settled || climbTime >= 0f;
        doorShot.BeginUnlock(door, face, lockPose, normal, pull, inPlace);
        Event("unlock", door.a + "-" + door.b + (pull ? " pull" : " push") + (inPlace ? " in place" : ""));
        if (inPlace || FrontRoomsSettings.CameraMotionScale <= 0f) return;
        if (!FrontRoomsDoorShot.UnlockStepIn(door, feet, lockPose.position, normal, pull, pullClear.HasValue && !pullIsGlass && !pullIsUnlock, out var stand)) return;
        // The step-in rides the pull step's mover (swept by the controller; it stops when the Relay sees or chases).
        pullClear = stand;
        pullClearTime = 0f;
        pullIsGlass = false;
        pullIsUnlock = true;
        glassStepFrom = feet;
    }

    /// <summary>
    /// What the Relay is doing, for the unlock shot's cancel rule (FrontRoomsDoorShot.Threatened): the hunter's Threat
    /// reading (RELAY §8, V2 2026-10-29: TargetAcquired recently or Spotting, before or after the commit). Its v1
    /// stand-ins are a chase and sight within 6 m; V4b swaps them inside the hunter, not here.
    /// </summary>
    FrontRoomsRelayThreat UnlockThreat() => relay != null ? relay.Threat : default;

    // A locked door's try: a Relay noise at the lock, off until Red decides (the audit's 6 m, plan §3.4). Not const: a const 0 would make the call unreachable code.
    static readonly float RattleNoiseRadius = 0f;

    void OnDoorRattled(FrontRoomsMapWorld.Door door, Vector3 lockPoint)
    {
        if (RattleNoiseRadius > 0f) relay?.Noise(lockPoint, RattleNoiseRadius);
        // M8: the locked EXIT tried before the floor key was taken.
        if (map != null && map.FloorKeyRule && map.IsExitDoor(door) && !map.StationKeyIsTaken)
        {
            exitTriesBeforeTake++;
            Event("exit", "tried locked");
        }
    }

    readonly RaycastHit[] aimHits = new RaycastHit[16];

    /// <summary>
    /// The aim's hit: the nearest solid collider within Reach that is not the player's own body (A10, Red 2026-10-21:
    /// from about 33° down the base-eye ray met the player's CharacterController first, so E reached nothing).
    /// </summary>
    bool AimCast(Ray ray, out RaycastHit hit)
    {
        hit = default;
        var count = Physics.RaycastNonAlloc(ray, aimHits, Reach, ~0, QueryTriggerInteraction.Ignore);
        var best = float.PositiveInfinity;
        for (var i = 0; i < count; i++)
        {
            var c = aimHits[i].collider;
            if (c == null || (playerRoot != null && c.transform.IsChildOf(playerRoot))) continue;
            if (aimHits[i].distance < best) { best = aimHits[i].distance; hit = aimHits[i]; }
        }
        return best < float.PositiveInfinity;
    }

    /// <summary>What the crosshair is on: E opens or shuts a door, holding E breaks glass.</summary>
    void UpdateAim(float dt)
    {
        var previous = aimed;
        aimed = null;
        prompt = null;
        aimedHold = false;
        aimKind = FrontRoomsMapWorld.AimKind.None;
        // Gameplay aims from the base eye: shots, shakes and leans are picture only.
        var eye = rig != null ? rig.BaseEye : new Pose(cam.transform.position, cam.transform.rotation);
        var hitPoint = Vector3.zero;
        var ray = new Ray(eye.position, eye.rotation * Vector3.forward);
        // A10: the aim's hit never stops on the player's own body.
        var hitAny = AimCast(ray, out var hit);
        // EXAMINE (C3): the registry's aim boxes are asked first; a box on a wall face wins up to 2 cm behind the
        // physics hit, a door or furniture in front of it still wins.
        inspectAimed = null;
        if (FrontRoomsInspect.Find(ray, hitAny ? hit.distance + FrontRoomsInspect.SurfaceSlack : Reach, out var inspectTarget) && CanShowInspect(inspectTarget))
        {
            inspectAimed = inspectTarget;
            prompt = inspectTarget.Prompt;
            // HK (grammar H1): the aim registry supplies Read / Examine (an object close-up = Examine, a face = Read).
            aimKind = inspectTarget.Kind == InspectKind.Object || inspectTarget.Kind == InspectKind.Peek ? FrontRoomsMapWorld.AimKind.Examine : FrontRoomsMapWorld.AimKind.Read;
        }
        else if (hitAny)
        {
            prompt = map.Describe(hit.collider, out aimedHold);
            // Panes break from 1.2 m (the glass shot's stand needs it); everything else keeps the 2.4 m reach.
            if (aimedHold && hit.distance > FrontRoomsShotTimings.GlassBreak.Reach) { prompt = null; aimedHold = false; }
            // Tap mode: the pane breaks on repeated taps, so the prompt says so (the map's text is the hold prompt).
            if (aimedHold && FrontRoomsSettings.TapToBreak) prompt = FrontRoomsLanguage.T("TAP E  ·  BREAK GLASS");
            // C11, desktop only: a broken door has no Describe prompt (the phone's USE keys on that, UpdateMobileTouchPrompt),
            // so the desktop names it here and aims it, and E on it is the won't-shut try (UseDoor answers Broken).
            if (prompt == null && !FrontRoomsHandheld.Active && map.Kind(hit.collider, playerRoot.position, out _, out _) == FrontRoomsMapWorld.AimKind.Broken)
                prompt = FrontRoomsLanguage.T(Time.time < brokenTriedUntil && map.DoorOf(hit.collider) == brokenTried ? WontShutPrompt : BrokenPrompt);
            if (prompt != null) aimed = hit.collider;
            // HK (grammar H1): the crosshair's kind, consistent with the prompt (a pane beyond 1.2 m has none); a broken
            // door is aimed so E reaches Use (Broken), with no map prompt.
            aimKind = map.Kind(hit.collider, playerRoot.position, out aimKeyZone, out aimKeyHeld);
            if (prompt == null && aimKind != FrontRoomsMapWorld.AimKind.Broken) aimKind = FrontRoomsMapWorld.AimKind.None;
            if (aimKind == FrontRoomsMapWorld.AimKind.Broken && aimed == null) aimed = hit.collider;
            hitPoint = hit.point;
        }
        // Look+E keys (map.KeyPickupNeedsE, off by default): a key's trigger on the same ray, nearer than anything
        // solid (the body's own controller aside), takes the aim and the prompt ("E  ·  TAKE KEY").
        if (map.KeyPickupNeedsE)
        {
            var key = map.KeyOnRay(new Ray(eye.position, eye.rotation * Vector3.forward), Reach, playerRoot);
            if (key != null)
            {
                prompt = map.Describe(key, out aimedHold);
                aimed = key;
                aimKind = FrontRoomsMapWorld.AimKind.TakeKey;
                // R3 / M3: on a floor-key floor the key under the crosshair wins E too, not only the prompt: a close-up's aim
                // box on the same ray (the station TV, a rack plate) never opens instead of the take.
                if (map.FloorKeyRule) inspectAimed = null;
            }
        }
        if (previous != null && previous != aimed)
        {
            map.ReleaseHold(previous);
            ReleaseGlass();
            glassArmed = false;
            tapCredit = 0f;
            // The bar shows only while E holds a pane. A pane left keeps its progress in the map (GD3c, Red 2026-10-22),
            // and the bar picks it up from there on the next hold.
            holdProgress = 0f;
        }
        // During a shot E belongs to the shot (cancel, or a buffered push), never to what is aimed at; the
        // glass shot's own pane keeps its E (the hold, or tap mode's next strike).
        var eDown = EDown();
        var glassOwnsE = glassShot != null && aimedHold && aimed == glassPane && !glassShot.Shattered && (glassShot.Active || glassShot.Blending);
        var pressed = eDown && (glassOwnsE || rig == null || !rig.Consume(ShotInput.Use));
        // C5: E on a close-up target is committed after relay.Tick (C5b). A transient refusal (latch, cooldown, a
        // shot running) keeps the prompt and swallows E: it never falls through to the door or wall behind.
        if (inspectAimed != null)
        {
            holdProgress = 0f;
            if (pressed && CanPressInspect()) pendingInspect = inspectAimed;
            return;
        }
        if (aimed == null)
        {
            holdProgress = 0f;
            return;
        }
        if (!aimedHold)
        {
            holdProgress = 0f;
            // A try on a broken door (desktop: only it aims one) says WON'T SHUT for the map's won't-shut window.
            if (pressed && UseDoor(aimed) == FrontRoomsMapWorld.UseResult.Broken)
            {
                brokenTried = map.DoorOf(aimed);
                brokenTriedUntil = Time.time + FrontRoomsShotTimings.BrokenDoor.Duration;
            }
            return;
        }
        float step;
        if (FrontRoomsSettings.TapToBreak)
        {
            // Tap mode: each tap banks a third of the hold, spent in real time, so taps are never faster than holding.
            if (pressed && Time.time >= tapReadyAt)
            {
                if (glassShot == null || !glassShot.Active || glassShot.Shattered || aimed != glassPane) BeginGlass(aimed, hitPoint, eye);
                tapCredit += TapProgress;
                tapReadyAt = Time.time + TapCooldown;
            }
            step = Mathf.Min(dt, tapCredit);
            tapCredit -= step;
            // Between strikes a move key lets go of the pane (the shot holds the body otherwise).
            if (step <= 0f && moveIntent) ReleaseGlass();
        }
        else
        {
            // A hold starts only on a fresh press on the pane, not an E still held from a door.
            if (pressed && !glassArmed) BeginGlass(aimed, hitPoint, eye);
            if (pressed) glassArmed = true;
            if (!EHeld()) glassArmed = false;
            step = glassArmed ? dt : 0f;
        }
        if (step > 0f)
        {
            var broke = map.Hold(aimed, hitPoint, step, out holdProgress);
            glassShot?.Hold(holdProgress);
            EmitMobileGlassBeat(holdProgress);
            if (broke)
            {
                glassShot?.Shatter();
                aimed = null;
                holdProgress = 0f;
                glassArmed = false;
                tapCredit = 0f;
            }
        }
        else if (FrontRoomsSettings.TapToBreak) map.PauseHold(aimed);   // the credit is spent: the stress stops, the progress stays
        else
        {
            map.ReleaseHold(aimed);
            ReleaseGlass();
            holdProgress = 0f;
        }
    }

    // Tap mode for breaking glass: progress per tap (seconds of hold) and the time between counted taps.
    const float TapProgress = .35f, TapCooldown = .3f;

    // The Relay rig's turn rate (picture only: the brain uses relay.Heading and SeesPlayer).
    const float RelayTurnDegPerSecond = 360f;
    int relayRelaysSeen = -1;

    void UpdateRelayRig(float dt)
    {
        if (relay == null || hunter == null) return;
        if (hunter.gameObject.activeSelf != relay.Released) hunter.gameObject.SetActive(relay.Released);
        if (!relay.Released) return;
        var position = relay.Position;
        hunter.position = position;
#if FRONTROOMS_RELAY_V2
        // V4b (RELAY 8): the body follows Heading (the hold's square-up is in it), never the player on a glimpse; the head follows LookYaw.
        var facing = Flat(relay.Heading);
#else
        var facing = relay.SeesPlayer ? Flat(playerRoot.position - position) : Flat(relay.Heading);
#endif
        if (facing.sqrMagnitude > .0001f)
        {
            // It turns at a capped rate instead of snapping between its heading and the player (audit 1.5);
            // the first frame it is out (release, relay) it simply faces the way it goes.
            var target = Quaternion.Euler(0f, Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg, 0f);
            var appeared = relay.Relays != relayRelaysSeen;
            relayRelaysSeen = relay.Relays;
            hunter.rotation = appeared ? target : Quaternion.RotateTowards(hunter.rotation, target, RelayTurnDegPerSecond * dt);
        }
        var state = relay.State;
        if (hunterRig != null)
        {
            var motion = state == HunterState.Chase ? FrontRoomsRelayRig.MotionState.Run
                : state == HunterState.BreakDoor ? FrontRoomsRelayRig.MotionState.BreakDoor
                : state == HunterState.Hunt || state == HunterState.Wander || state == HunterState.Withdraw || (state == HunterState.Search && relay.Moving) ? FrontRoomsRelayRig.MotionState.Walk
                : state == HunterState.Search ? FrontRoomsRelayRig.MotionState.Search
                : FrontRoomsRelayRig.MotionState.IdleListen;
#if FRONTROOMS_RELAY_V2
            // V4b (RELAY 8): the lock-on's hold and the "almost" stop stand still, the head on LookYaw (the rig turns it where it stands; while it walks it is the rig owner's to draw).
            if (relay.LockingOn || relay.AlmostStopped) motion = FrontRoomsRelayRig.MotionState.IdleListen;
            hunterRig.SetListenTarget(relay.LookPoint);
#else
            // The head turns toward the last noise while it listens or stands searching.
            hunterRig.SetListenTarget(state == HunterState.Listen || state == HunterState.Search ? relay.ListenPoint : null);
#endif
            // The room it stands in and how deep it is in a doorway, for the rig's crammed poses.
            hunterRig.CeilingHeight = MapGrid.CeilingHeight(map.ZoneOf(map.CellOf(position)).height);
            hunterRig.DoorSqueeze = relay.DoorSqueeze;
            hunterRig.TickAnimation(dt, motion, relay.Moving, state == HunterState.Chase ? 1.15f * ChasePace : 1f);
        }
        // Under FMOD the sound layer plays its steps from the rig's foot plants (AUDIO_CONTRACT.md).
        if (!relay.Moving || (FrontRooms.Audio.FrontRoomsFmod.Ready && hunterRig != null && hunterRig.RaisesSteps)) return;
        hunterStepTime += dt;
        if (hunterStepTime < (state == HunterState.Chase ? .29f / ChasePace : .44f)) return;
        hunterStepTime = 0f;
        var p = Flat(position);
        var distance = Vector2.Distance(playerPos, p);
        FoleyFootstep(p, FrontRoomsFoleyActor.Hunter, FrontRoomsFoleySurface.Carpet, state == HunterState.Chase, .36f + Mathf.Clamp01(1f - distance / 24f) * (state == HunterState.Chase ? .64f : .40f));
    }

    /// <summary>
    /// The HUD's room text for a zone of this floor: the floor's token (FloorLabel: LEVEL 0 on the title's floor,
    /// LEVEL B1, B2, ... below it, C22 D13) and the zone's kind. The Office keeps its own level name on every floor
    /// (叙事设计's floor strings name the maze, low rooms and tall halls only).
    /// </summary>
    string ZoneName(ZoneInfo zone)
    {
        if (zone.theme == ZoneTheme.Office) return FrontRoomsLanguage.T("LEVEL 4 / OFFICE");
        var level = FloorLabel(floor);
        switch (zone.height)
        {
            case ZoneHeight.Low: return FrontRoomsLanguage.F("{0} / LOW ROOMS", level);
            case ZoneHeight.Tall: return FrontRoomsLanguage.F("{0} / TALL HALLS", level);
            default: return FrontRoomsLanguage.F("{0} / THE MAZE", level);
        }
    }

    void BuildSound()
    {
        // Mobile builds use FMOD for all sound and the Unity AudioManager is
        // intentionally disabled. Creating procedural AudioClips here makes
        // every SetData call fail on iOS before FMOD has finished booting.
        // Leave the legacy fallback unallocated; FMOD owns the mobile path.
        if (FrontRoomsHandheld.Active)
        {
            Debug.Log("[FrontRoomsAudio] Handheld FMOD path active; skipped legacy Unity clips.");
            return;
        }
        var recordedDoorCreak = Resources.Load<AudioClip>("Audio/door-creak");
        foley = new FrontRoomsFoley(recordedDoorCreak);
        playerStepClip = FrontRoomsAudio.PlayerStep(); playerRunStepClip = FrontRoomsAudio.PlayerRunStep(); hunterStepClip = FrontRoomsAudio.HunterStep();
        // The Foley door set layers a latch, hinge recording, movement bed and
        // break impact. The plain clips are fallbacks when Foley is unavailable.
        doorClip = foley.Doors.hinge;
        bangClip = foley.Doors.breakImpact;
        caughtClip = FrontRoomsAudio.Caught();
        hum = cam.gameObject.AddComponent<AudioSource>(); hum.clip = FrontRoomsAudio.Hum(); hum.loop = true; hum.volume = .18f; hum.Play();
    }
    void Sound(AudioClip clip, Vector2 p, float volume = .7f)
    {
        if (clip == null) return;
        var g = new GameObject("Spatial sound"); g.transform.position = V(p, 1f);
        var source = g.AddComponent<AudioSource>(); source.clip = clip; source.spatialBlend = 1f; source.minDistance = 2f; source.maxDistance = 26f; source.volume = volume; source.Play();
        Destroy(g, clip.length + .1f);
    }

    void FoleyFootstep(Vector2 p, FrontRoomsFoleyActor actor, FrontRoomsFoleySurface surface, bool running, float volume)
    {
        if (foley == null)
        {
            var fallback = actor == FrontRoomsFoleyActor.Hunter ? hunterStepClip : (running ? playerRunStepClip : playerStepClip);
            Sound(fallback, p, volume);
            return;
        }
        var selection = foley.PickStep(actor, surface, running);
        var g = new GameObject("Foley / " + actor + " / " + surface);
        g.transform.position = V(p, actor == FrontRoomsFoleyActor.Hunter ? .45f : .08f);
        var source = g.AddComponent<AudioSource>();
        source.spatialBlend = 1f;
        source.minDistance = actor == FrontRoomsFoleyActor.Hunter ? 1.25f : 1.1f;
        source.maxDistance = actor == FrontRoomsFoleyActor.Hunter ? 32f : 18f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.dopplerLevel = actor == FrontRoomsFoleyActor.Hunter ? .15f : .04f;
        source.spread = actor == FrontRoomsFoleyActor.Hunter ? 28f : 12f;
        source.priority = actor == FrontRoomsFoleyActor.Hunter ? 64 : 90;
        source.pitch = selection.pitch;
        source.PlayOneShot(selection.impact, volume);
        source.PlayOneShot(selection.texture, volume * (actor == FrontRoomsFoleyActor.Hunter ? .72f : .62f));
        source.PlayOneShot(selection.cloth, volume * .34f);
        if (actor == FrontRoomsFoleyActor.Hunter)
        {
            var low = g.AddComponent<AudioLowPassFilter>();
            low.cutoffFrequency = 1450f;
            low.lowpassResonanceQ = 1.1f;
        }
        var lifetime = Mathf.Max(selection.impact.length, selection.texture.length, selection.cloth.length) + .06f;
        Destroy(g, lifetime);
    }

    void FoleyDoorBreak(Vector2 p)
    {
        if (foley == null)
        {
            Sound(bangClip, p, 1f);
            return;
        }
        var g = new GameObject("Foley / door / break");
        g.transform.position = V(p, 1f);
        var source = g.AddComponent<AudioSource>();
        source.spatialBlend = 1f;
        source.minDistance = 2f;
        source.maxDistance = 26f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.dopplerLevel = .06f;
        source.priority = 48;
        source.pitch = .97f + UnityEngine.Random.Range(-.025f, .025f);
        source.PlayOneShot(foley.Doors.breakImpact, 1f);
        Destroy(g, foley.Doors.breakImpact.length + .1f);
    }

    Font UiFont(string name, int fontSize)
    {
        if (name.Contains("Room meta") || name.Contains("Distance") || name.Contains("Aim")) return monoFont;
        if (name.Contains("Threat") || name.Contains("Key") || name.Contains("Menu text")) return bayonFont;
        return serifFont;
    }
    Text Text(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, TextAnchor alignment)
    {
        var g = new GameObject(name); g.transform.SetParent(parent, false); var t = g.AddComponent<Text>();
        t.font = UiFont(name, fontSize); t.fontSize = fontSize; t.color = C("ECEAE0"); t.alignment = alignment; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = t.rectTransform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor; rt.anchoredPosition = pos; rt.sizeDelta = size;
        t.lineSpacing = 1f;
        return t;
    }
    GameObject Panel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
    {
        var g = new GameObject(name); g.transform.SetParent(parent, false); var image = g.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
        var rt = image.rectTransform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor; rt.anchoredPosition = pos; rt.sizeDelta = size;
        return g;
    }
    Sprite LoadHudSprite(string resourceName)
    {
        var texture = Resources.Load<Texture2D>(resourceName);
        if (texture == null) return null;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
    }
    void LoadBrandLogo()
    {
        vectorLogoLeftAsset = Resources.Load<UiVectorImage>("Brand/FrontRoomsLogo_Left");
        vectorLogoS1Asset = Resources.Load<UiVectorImage>("Brand/FrontRoomsLogo_S1");
        vectorLogoS2Asset = Resources.Load<UiVectorImage>("Brand/FrontRoomsLogo_S2");
        if (vectorLogoLeftAsset != null && vectorLogoS1Asset != null && vectorLogoS2Asset != null
            && BuildVectorLogo(vectorLogoLeftAsset, vectorLogoS1Asset, vectorLogoS2Asset))
        {
            vectorLogoActive = true;
            if (logoImage != null) logoImage.enabled = false;
            return;
        }
        var texture = Resources.Load<Texture2D>("Brand/FrontRoomsLogo");
        if (texture == null) return;
        brandLogo = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f);
        brandLogo.name = "FrontRooms brand logo (runtime)";
        if (logoImage != null)
        {
            logoImage.sprite = brandLogo;
            logoImage.preserveAspect = true;
            logoImage.color = Color.white;
            var whiteLogoShader = Resources.Load<Shader>("Brand/WhiteLogoUI") ?? Shader.Find("UI/FrontRooms White Logo");
            if (whiteLogoShader != null)
            {
                whiteLogoMaterial = new Material(whiteLogoShader);
                whiteLogoMaterial.name = "FrontRooms logo white (runtime)";
                logoImage.material = whiteLogoMaterial;
            }
            ConfigureLogoMotion(texture);
        }
    }

    bool BuildVectorLogo(UiVectorImage leftAsset, UiVectorImage s1Asset, UiVectorImage s2Asset)
    {
        if (leftAsset == null || s1Asset == null || s2Asset == null) return false;
        var host = new GameObject("FrontRooms vector logo");
        vectorLogoDocument = host.AddComponent<UiDocument>();
        var settings = ScriptableObject.CreateInstance<UiPanelSettings>();
        settings.clearColor = false;
        settings.clearDepthStencil = false;
        settings.scaleMode = FrontRoomsHandheld.Active ? UiPanelScaleMode.ConstantPixelSize : UiPanelScaleMode.ScaleWithScreenSize;
        if (FrontRoomsHandheld.Active) settings.scale = FrontRoomsHandheld.PointScale;
        settings.referenceResolution = FrontRoomsHandheld.Active
            ? new Vector2Int(874, 402)
            : new Vector2Int(1920, 1080);
        settings.screenMatchMode = UiPanelScreenMatchMode.MatchWidthOrHeight;
        settings.match = .5f;
        settings.sortingOrder = 100;
        vectorLogoDocument.panelSettings = settings;
        var panel = vectorLogoDocument.rootVisualElement;
        if (panel == null) return false;
        panel.pickingMode = UiPickingMode.Ignore;
        panel.style.position = UiPosition.Absolute;
        panel.style.left = 0f; panel.style.top = 0f;
        panel.style.width = UiLength.Percent(100); panel.style.height = UiLength.Percent(100);

        // A handheld's cold start: black under the logo until the title room has drawn (UpdateStartupCurtain).
        startupCurtain = new UiVisualElement { name = "Startup curtain" };
        startupCurtain.style.position = UiPosition.Absolute;
        startupCurtain.style.left = 0f; startupCurtain.style.top = 0f;
        startupCurtain.style.width = UiLength.Percent(100); startupCurtain.style.height = UiLength.Percent(100);
        startupCurtain.style.backgroundColor = Color.black;
        startupCurtain.pickingMode = UiPickingMode.Ignore;
        startupCurtain.style.display = UiDisplayStyle.None;
        panel.Add(startupCurtain);

        vectorLogoRoot = new UiVisualElement { name = "FrontRooms SVG lockup" };
        vectorLogoRoot.style.position = UiPosition.Absolute;
        vectorLogoRoot.style.left = UiLength.Percent(50);
        vectorLogoRoot.style.top = UiLength.Percent(50);
        vectorLogoRoot.style.width = 965f; vectorLogoRoot.style.height = 192f;
        vectorLogoRoot.style.marginLeft = -482.5f; vectorLogoRoot.style.marginTop = -96f;
        var logoScale = FrontRoomsHandheld.Active ? 540f / 965f : LogoScale;
        vectorLogoRoot.style.scale = new UnityEngine.UIElements.Scale(new Vector3(logoScale, logoScale, 1f));
        vectorLogoRoot.style.overflow = UiOverflow.Hidden;
        vectorLogoRoot.pickingMode = UiPickingMode.Ignore;
        panel.Add(vectorLogoRoot);

        // Restore the 11:53 lockup: one complete FRONTROOMS vector wordmark
        // plus the two trailing S assets. The source white lockup paints the
        // two translucent afterimages first and the solid wordmark last; keep
        // that painter order so the gray SS never washes over the final solid
        // S when their paths overlap.
        vectorLogoLetterMasks.Clear();
        vectorLogoLetterWidths.Clear();
        // Unity imports each S at its painted bounds. Place both visible glyphs
        // on the final solid-S baseline; do not apply the source SVG viewBox
        // x coordinates a second time.
        vectorLogoS1Image = VectorLogoImage("SVG trailing S 1", s1Asset, 798f, 17.8f);
        vectorLogoS2Image = VectorLogoImage("SVG trailing S 2", s2Asset, 798f, 17.8f);
        vectorLogoRoot.Add(vectorLogoS2Image);
        vectorLogoRoot.Add(vectorLogoS1Image);
        vectorLogoLeftImage = VectorLogoImage("SVG complete wordmark", leftAsset, 8f, 17.6f);
        vectorLogoRoot.Add(vectorLogoLeftImage);
        vectorLogoRoot.style.display = UiDisplayStyle.None;
        return vectorLogoLeftImage != null && vectorLogoS1Image != null && vectorLogoS2Image != null;
    }

    UiVisualElement MaskedVectorGlyph(string name, UiVectorImage vectorImage, float glyphStart, float glyphEnd, out UiImage innerImage)
    {
        var mask = new UiVisualElement { name = name + " / hard wipe mask" };
        mask.style.position = UiPosition.Absolute;
        mask.style.left = glyphStart;
        mask.style.top = 0f;
        mask.style.width = 0f;
        mask.style.height = 192f;
        mask.style.overflow = UiOverflow.Hidden;
        mask.pickingMode = UiPickingMode.Ignore;
        // The VectorImage is already cropped to this glyph's local viewBox.
        // Keep it stationary at the mask origin; only the mask width changes.
        innerImage = VectorLogoImage(name + " / stationary glyph source", vectorImage, 0f, 0f);
        // The imported VectorImage reports painted bounds, which are slightly
        // narrower than the original local viewBox. Use the source lockup box
        // so the final M and solid S are never trimmed by their masks.
        innerImage.style.width = glyphEnd - glyphStart;
        innerImage.style.height = 192f;
        innerImage.style.opacity = 1f;
        mask.Add(innerImage);
        return mask;
    }

    UiImage VectorLogoImage(string name, UiVectorImage vectorImage, float left, float top)
    {
        var image = new UiImage { name = name, vectorImage = vectorImage };
        image.scaleMode = UnityEngine.ScaleMode.StretchToFill;
        image.tintColor = Color.white;
        image.style.position = UiPosition.Absolute;
        image.style.left = left; image.style.top = top;
        // VectorImage bounds are the painted path bounds. Stretching the small
        // S asset to the full 965px viewBox turns it into a ribbon, so preserve
        // each imported asset's native size and place it in the shared lockup.
        image.style.width = vectorImage.width; image.style.height = vectorImage.height;
        image.style.opacity = 0f;
        image.pickingMode = UiPickingMode.Ignore;
        return image;
    }

    void ApplyHdrMode(bool enabled, bool persist)
    {
        var supported = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.DefaultHDR);
        if (cam != null) cam.allowHDR = enabled && supported;
        // Unity 6 no longer exposes a readable Camera.hdr property. The
        // support check plus the value applied to allowHDR is the portable
        // runtime state for the built-in renderer and WebGL fallback.
        hdrEnabled = enabled && supported;
        if (persist)
        {
            PlayerPrefs.SetInt(HdrPreferenceKey, hdrEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }
        UpdateDisplaySettingsText();
    }

    /// <summary>The settings panel's rows: section, name, value, and a step for ← → (or Enter).</summary>
    (string section, string label, Func<string> value, Action<int> step)[] SettingsRows() => new (string, string, Func<string>, Action<int>)[]
    {
        ("OUTPUT", "HDR RENDER", () => hdrEnabled ? "ON" : "OFF  (SDR)", d => ApplyHdrMode(!hdrEnabled, true)),
        ("COMFORT", "CAMERA MOTION", () => FrontRoomsSettings.CameraMotionPercent == 0 ? "OFF" : FrontRoomsSettings.CameraMotionPercent + "%", d => FrontRoomsSettings.StepCameraMotion(d)),
        ("COMFORT", "REDUCE FLASHING", () => FrontRoomsSettings.ReduceFlashing ? "ON" : "OFF", d => FrontRoomsSettings.SetReduceFlashing(!FrontRoomsSettings.ReduceFlashing)),
        ("INPUT", "BREAK GLASS", () => FrontRoomsSettings.TapToBreak ? "TAP E" : "HOLD E", d => FrontRoomsSettings.SetTapToBreak(!FrontRoomsSettings.TapToBreak)),
        ("ASSIST", "CAPTIONS", () => FrontRoomsSettings.Captions ? "ON" : "OFF", d => FrontRoomsSettings.SetCaptions(!FrontRoomsSettings.Captions)),
        ("ASSIST", "THREAT READOUT", () => FrontRoomsSettings.RelayReadout ? "ON" : "OFF", d => FrontRoomsSettings.SetRelayReadout(!FrontRoomsSettings.RelayReadout)),
    };

    void UpdateDisplaySettingsText()
    {
        if (desktopSettings != null && !FrontRoomsHandheld.Active) { desktopSettings.Refresh(); return; }
        if (displaySettingsText == null) return;
        // The mobile canvas owns the eight larger touch preference rows. Keep
        // the legacy desktop text panel as a quiet title/instruction layer so
        // its six keyboard rows do not sit underneath those touch targets.
        if (FrontRoomsHandheld.Active)
        {
            displaySettingsText.text = "<size=30><b>TOUCH SETTINGS</b></size>\n\n<size=16>TAP A ROW TO CHANGE    BACK TO CLOSE</size>";
            return;
        }
        var rows = SettingsRows();
        settingsIndex = (settingsIndex % rows.Length + rows.Length) % rows.Length;
        var text = new System.Text.StringBuilder("<size=30><b>SETTINGS</b></size>\n");
        string section = null;
        // The text's line each row lands on (the title is line 0; a section adds a blank line and its own), for the hits.
        var at = 1;
        settingsRowLines.Clear();
        for (var i = 0; i < rows.Length; i++)
        {
            if (rows[i].section != section)
            {
                section = rows[i].section;
                text.Append("\n<size=14>").Append(section).Append("</size>\n");
                at += 2;
            }
            settingsRowLines.Add(at++);
            var line = rows[i].label + "  /  " + rows[i].value();
            text.Append(i == settingsIndex ? "<color=#F4DF3B>›  " + line + "</color>\n" : line + "\n");
        }
        text.Append("\n<size=16>↑ ↓  CHOOSE    ← →  CHANGE    ESC  CLOSE</size>");
        displaySettingsText.text = text.ToString();
        PlaceSettingsHits(rows.Length);
    }

    /// <summary>
    /// A3: one transparent hit Image per settings row, over the row's line of displaySettingsText (where the game's own
    /// text layout puts it), as wide as the widest row (with its "›  ") plus 8 each side and as tall as the line, so the
    /// rows tile without overlapping. Hover chooses the row (settingsIndex); a click is Enter (a step of 2: cycle).
    /// </summary>
    void PlaceSettingsHits(int count)
    {
        if (FrontRoomsHandheld.Active || displaySettingsPanel == null) return;
        while (settingsHits.Count < count)
        {
            var row = settingsHits.Count;
            var hit = MenuHit(displaySettingsPanel.transform, "Settings hit " + (row + 1));
            AddTrigger(hit, UiEventTriggerType.PointerEnter, () =>
            {
                if (!displaySettingsOpen || settingsIndex == row) return;
                settingsIndex = row;
                UpdateDisplaySettingsText();
            });
            AddTrigger(hit, UiEventTriggerType.PointerClick, () =>
            {
                if (!displaySettingsOpen) return;
                settingsIndex = row;
                StepGameSetting(row, 2);
            });
            settingsHits.Add(hit);
        }
        MenuLineSlots(displaySettingsText, displaySettingsText.text, menuSlots);
        var rows = SettingsRows();
        var settings = displaySettingsText.GetGenerationSettings(Vector2.zero);
        var width = 0f;
        for (var i = 0; i < rows.Length; i++)
            width = Mathf.Max(width, MenuGenerator.GetPreferredWidth("›  " + rows[i].label + "  /  " + rows[i].value(), settings));
        width /= Mathf.Max(.01f, displaySettingsText.pixelsPerUnit);
        var origin = displaySettingsText.rectTransform.anchoredPosition;
        for (var i = 0; i < settingsHits.Count; i++)
        {
            var on = i < count && i < settingsRowLines.Count && settingsRowLines[i] < menuSlots.Count;
            settingsHits[i].gameObject.SetActive(on);
            if (!on) continue;
            var slot = menuSlots[settingsRowLines[i]];
            settingsHits[i].rectTransform.anchoredPosition = origin + new Vector2(0f, slot.x);
            settingsHits[i].rectTransform.sizeDelta = new Vector2(width + 16f, slot.y);
        }
    }

    /// <summary>The settings panel's keys: ↑ ↓ (W S) choose a row, ← → (A D), Enter or Space change it; H and T stay as shortcuts.</summary>
    void HandleSettingsKeys()
    {
        if (desktopSettings != null && !FrontRoomsHandheld.Active)
        {
            if (Input.GetKeyDown(KeyCode.H)) { ApplyHdrMode(!hdrEnabled, true); return; }
            if (Input.GetKeyDown(KeyCode.T)) { FrontRoomsSettings.SetRelayReadout(!FrontRoomsSettings.RelayReadout); desktopSettings.Refresh(); return; }
            desktopSettings.HandleKeys();
            return;
        }
        var rows = SettingsRows();
        var moved = 0;
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) moved = -1;
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) moved = 1;
        if (moved != 0)
        {
            settingsIndex = (settingsIndex + moved + rows.Length) % rows.Length;
            UpdateDisplaySettingsText();
            return;
        }
        // ← → step (camera motion stops at its ends); Enter and Space cycle (2). On/off rows ignore the value.
        var change = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) ? -1
            : Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) ? 1
            : Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) ? 2 : 0;
        if (Input.GetKeyDown(KeyCode.H)) { ApplyHdrMode(!hdrEnabled, true); return; }
        if (Input.GetKeyDown(KeyCode.T)) { FrontRoomsSettings.SetRelayReadout(!FrontRoomsSettings.RelayReadout); UpdateDisplaySettingsText(); return; }
        if (change == 0) return;
        rows[settingsIndex].step(change);
        UpdateDisplaySettingsText();
    }

    /// <summary>
    /// The pause card's overlayText. Desktop (A3 + T5, 平面视觉's FIX_CARD_VL217): the title, then a slot for the
    /// controls legend and one for each action line, which LayoutDesktopMenu fills with their own Texts. The handheld
    /// copy stays hidden: the touch layer draws that card.
    /// </summary>
    string PauseText()
    {
        if (FrontRoomsHandheld.Active)
            return "<size=48>PAUSED</size>\n\n<size=11>MOVE       DRAG · LOOK\nUSE · DOOR       HOLD USE · BREAK GLASS</size>";
        return "<size=88>" + FrontRoomsLanguage.T("PAUSED") + "</size>\n\n" + MenuSlot(13) + "\n\n" + MenuSlot(20) + "\n" + MenuSlot(20) + "\n" + MenuSlot(20);
    }

    // A blank line of a menu card at this size: the slot one of the card's own lines is laid on.
    static string MenuSlot(int size) => "<size=" + size + "> </size>";

    void ToggleDisplaySettings()
    {
        if (phase != Phase.Paused || displaySettingsPanel == null) return;
        displaySettingsOpen = !displaySettingsOpen;
        if (!FrontRoomsHandheld.Active)
        {
            if (desktopSettings != null) { displaySettingsPanel.SetActive(false); desktopSettings.SetOpen(displaySettingsOpen); }
            else displaySettingsPanel.SetActive(displaySettingsOpen);
        }
        if (overlayText != null)
        {
            overlayText.enabled = !FrontRoomsHandheld.Active && !displaySettingsOpen;
            if (!displaySettingsOpen) overlayText.text = PauseText();
        }
        menuHover = -1;
        LayoutDesktopMenu();
        if (FrontRoomsHandheld.Active)
        {
            var showPauseCopy = !displaySettingsOpen;
            if (mobilePauseTitle != null) mobilePauseTitle.gameObject.SetActive(showPauseCopy);
            if (mobilePauseRows != null) mobilePauseRows.gameObject.SetActive(showPauseCopy);
        }
        UpdateDisplaySettingsText();
        UpdateMobileTouchMenu();
    }

    /// <summary>
    /// The desktop menus' own pieces (never on handhelds): an EventSystem (the project's Input System UI module) and a
    /// GraphicRaycaster on the menu canvas, for the mouse (A3); on the card (overlay), above its text, the pause legend,
    /// the caught stats and the three action lines, A1's marker, and the lines' hit Images on top; and the T5 title legend.
    /// </summary>
    void BuildDesktopMenu(GameObject canvas, Color paper, Color accent)
    {
        canvas.AddComponent<GraphicRaycaster>();
        if (UiEventSystem.current == null) new GameObject("EventSystem", typeof(UiEventSystem), typeof(UiInputModule));
        var ink = C("0A0A0A");
        // T5 on the pause card: the legend in ink on the light wash (no keyline there), above the action lines.
        menuLegend = MenuText("Menu legend", monoFont, 13, ink);
        menuLegend.text = FrontRoomsLanguage.T(ControlsLegend);
        FrontRoomsLanguage.Style(menuLegend, ControlsLegend);
        // A7: the stats out of Bayon (it draws 1 and 0 as I and O), in IBM Plex Mono 20 caps.
        menuStats = MenuText("Menu stats", monoFont, 20, ink);
        // FIX_CARD §E1: the subtitle is its own Text, so it sits with the title; the probe only measures CAUGHT's caps.
        menuSubtitle = MenuText("Menu subtitle", bayonFont, 28, ink);
        menuSubtitle.text = FrontRoomsLanguage.T("OCCUPANT ACCOUNTED FOR");
        FrontRoomsLanguage.Style(menuSubtitle, "OCCUPANT ACCOUNTED FOR");
        menuTitleProbe = MenuText("Menu title probe", bayonFont, 88, ink);
        menuTitleProbe.text = FrontRoomsLanguage.T("CAUGHT");
        FrontRoomsLanguage.Style(menuTitleProbe, "CAUGHT");
        // A1 + A3: each action its own line, Bayon 20 ink (ESC  RESUME, R  RESTART, O  SETTINGS; caught: R  TRY AGAIN).
        for (var i = 0; i < menuLines.Length; i++) menuLines[i] = MenuText(i == 0 ? "Menu CTA" : "Menu line " + (i + 1), bayonFont, 20, ink);
        // A1: the yellow is a marker now, 4 x 14 (Bayon 20's caps), its right edge 10 left of the marked line's text.
        menuMarker = Panel(overlay.transform, "Menu CTA marker", new Vector2(.5f, .5f), Vector2.zero, new Vector2(4f, 14f), accent).GetComponent<Image>();
        menuMarker.rectTransform.pivot = new Vector2(0f, .5f);
        menuMarker.gameObject.SetActive(false);
        // A3: hover moves the marker to the line, a click does what its key does.
        for (var i = 0; i < menuHits.Length; i++)
        {
            var line = i;
            menuHits[i] = MenuHit(overlay.transform, "Menu hit " + (i + 1));
            AddTrigger(menuHits[i], UiEventTriggerType.PointerEnter, () => { menuHover = line; MarkMenuLine(); });
            AddTrigger(menuHits[i], UiEventTriggerType.PointerExit, () => { if (menuHover != line) return; menuHover = -1; MarkMenuLine(); });
            AddTrigger(menuHits[i], UiEventTriggerType.PointerClick, () => MenuLineClicked(line));
            menuHits[i].gameObject.SetActive(false);
        }
        // T5 on the title: IBM Plex Mono 13 caps, 100 % paper with the keyline, under the wordmark. On the canvas itself,
        // not the card: the card goes the moment play starts, and the legend leaves with the wordmark (UpdateDesktopMenu).
        titleLegend = Text(canvas.transform, "Title legend", new Vector2(.5f, .5f), new Vector2(0f, TitleLegendY), new Vector2(1440f, 24f), 13, TextAnchor.MiddleCenter);
        titleLegend.font = monoFont;
        titleLegend.color = paper;
        titleLegend.horizontalOverflow = HorizontalWrapMode.Overflow;
        titleLegend.text = FrontRoomsLanguage.T(ControlsLegend);
        FrontRoomsLanguage.Style(titleLegend, ControlsLegend);
        AddKeyline(titleLegend);
        titleLegend.gameObject.SetActive(false);
    }

    // One of a desktop card's own lines: centred on the card, one line, laid on its slot by LayoutDesktopMenu.
    Text MenuText(string name, Font font, int size, Color color)
    {
        var t = Text(overlay.transform, name, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1440f, 36f), size, TextAnchor.MiddleCenter);
        t.font = font;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.gameObject.SetActive(false);
        return t;
    }

    // A line's mouse area (A3): a transparent raycast target with an EventTrigger. Text stays off the raycast.
    Image MenuHit(Transform parent, string name)
    {
        var hit = Panel(parent, name, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f)).GetComponent<Image>();
        hit.raycastTarget = true;
        // A culled (fully transparent) mesh gets no depth, and GraphicRaycaster skips a graphic with none.
        hit.canvasRenderer.cullTransparentMesh = false;
        hit.gameObject.AddComponent<UiEventTrigger>();
        return hit;
    }

    static void AddTrigger(Image hit, UiEventTriggerType type, Action action)
    {
        var entry = new UiEventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => action());
        hit.GetComponent<UiEventTrigger>().triggers.Add(entry);
    }

    // The keyline round a text with no card behind it (平面视觉, 50_tutorial_placard.md §11: 1 px #141414 at 85 %, Unity
    // Outline, never the view's ink #0A0A0A): the T5 title legend, the flash line, the captions, the zone block and the
    // prompt under the crosshair. Every value comes from FrontRoomsHudStyle, the one constant shared with HUD key B
    // (游戏视觉's, calibrated in linear light). Outline draws 4 diagonal copies and two overlap on the ring, so each copy
    // takes OutlineTapAlpha and the ring comes to KeylineAlpha. A distance other than KeylineDistance is for the prompt only
    // (G6, 平面视觉 2026-10-29: 2 px, since at Bayon 20 a 1 px ring lies over the 2 px stems).
    static void AddKeyline(Graphic graphic, float distance = FrontRoomsHudStyle.KeylineDistance)
    {
        var outline = graphic.gameObject.AddComponent<Outline>();
        var k = FrontRoomsHudStyle.KeylineColor;
        outline.effectColor = new Color(k.r, k.g, k.b, FrontRoomsHudStyle.OutlineTapAlpha);
        outline.effectDistance = new Vector2(distance, -distance);
    }

    /// <summary>
    /// Lays the desktop card's own lines on overlayText's slots for the phase: pause, the legend and three action lines;
    /// caught, the stats and TRY AGAIN; otherwise none. Each action line's hit Image is its line's width plus 8 each side
    /// and the line's full pitch, so the hits tile, and the marker goes to the marked line. Called when the card, a line's
    /// words, the screen size or the HUD's scale change.
    /// </summary>
    void LayoutDesktopMenu()
    {
        if (menuMarker == null || overlayText == null) return;
        var pause = phase == Phase.Paused && !displaySettingsOpen;
        var caught = phase == Phase.Caught;
        menuLineCount = pause ? 3 : caught ? 1 : 0;
        menuLegend.gameObject.SetActive(pause);
        menuStats.gameObject.SetActive(caught);
        menuSubtitle.gameObject.SetActive(caught);
        for (var i = 0; i < menuLines.Length; i++)
        {
            menuLines[i].gameObject.SetActive(i < menuLineCount);
            menuHits[i].gameObject.SetActive(i < menuLineCount);
        }
        menuMarker.gameObject.SetActive(menuLineCount > 0);
        if (menuLineCount == 0) return;
        if (pause)
        {
            menuLines[0].text = FrontRoomsLanguage.T("ESC  RESUME");
            menuLines[1].text = FrontRoomsLanguage.T("R  END RUN");
            menuLines[2].text = FrontRoomsLanguage.T("O  SETTINGS");
        }
        else menuLines[0].text = FrontRoomsLanguage.T("R  TRY AGAIN");
        MenuLineSlots(overlayText, overlayText.text, menuSlots);
        var origin = overlayText.rectTransform.anchoredPosition;
        PlaceOnSlot(pause ? menuLegend : menuStats, origin, pause ? PauseLegendLine : CaughtStatsLine);
        if (caught)
        {
            PlaceOnSlot(menuTitleProbe, origin, 0);
            PlaceOnSlot(menuSubtitle, origin, 1);
            var titleCaps = InkBand(menuTitleProbe);
            var subtitleCaps = InkBand(menuSubtitle);
            var capsFoot = menuTitleProbe.rectTransform.anchoredPosition.y + titleCaps.x - titleCaps.y * .5f;
            menuSubtitle.rectTransform.anchoredPosition = new Vector2(origin.x, capsFoot - CaughtSubtitleGap - subtitleCaps.x - subtitleCaps.y * .5f);
        }
        var first = pause ? PauseFirstActionLine : CaughtActionLine;
        // The hits tile: each spans its line's whole pitch (no dead band between lines, where the marker would drop to RESUME).
        var pitch = first + 1 < menuSlots.Count && menuLineCount > 1 ? Mathf.Abs(menuSlots[first].x - menuSlots[first + 1].x)
            : first < menuSlots.Count ? menuSlots[first].y : 36f;
        for (var i = 0; i < menuLineCount; i++)
        {
            var line = menuLines[i];
            PlaceOnSlot(line, origin, first + i);
            var ink = InkBand(line);
            var at = line.rectTransform.anchoredPosition;
            menuLineCapY[i] = at.y + ink.x;
            menuLineWidth[i] = line.preferredWidth;
            menuHits[i].rectTransform.anchoredPosition = at;
            menuHits[i].rectTransform.sizeDelta = new Vector2(menuLineWidth[i] + 16f, pitch);
        }
        MarkMenuLine();
    }

    // A1 + A3: the marker is on the line under the mouse (hover always wins, A3), else on the restart question while it
    // is open (the keyboard's R), else on the first line (RESUME, TRY AGAIN).
    // Centred on the caps; x = -preferredWidth / 2 - 14 for the centred text.
    void MarkMenuLine()
    {
        if (menuMarker == null || menuLineCount == 0) return;
        var line = menuHover >= 0 && menuHover < menuLineCount ? menuHover : 0;
        var x = menuLines[line].rectTransform.anchoredPosition.x;
        menuMarker.rectTransform.anchoredPosition = new Vector2(x - menuLineWidth[line] * .5f - 14f, menuLineCapY[line]);
    }

    /// <summary>
    /// Where <paramref name="t"/> lays out <paramref name="text"/>: per line, the y of its middle from t's pivot (x) and
    /// its height (y), in t's local units. It is the game's own text layout (a TextGenerator with t's settings), so a
    /// line laid on a slot sits where the single block put that line.
    /// </summary>
    void MenuLineSlots(Text t, string text, List<Vector2> slots)
    {
        slots.Clear();
        var ppu = Mathf.Max(.01f, t.pixelsPerUnit);
        MenuGenerator.Populate(text, t.GetGenerationSettings(t.rectTransform.rect.size));
        var lines = MenuGenerator.lines;
        for (var i = 0; i < lines.Count; i++)
            slots.Add(new Vector2((lines[i].topY - lines[i].height * .5f) / ppu, lines[i].height / ppu));
    }

    // A card line's Text on a slot of overlayText: its middle on the slot's middle, as tall as the slot.
    void PlaceOnSlot(Text t, Vector2 origin, int slot)
    {
        if (slot >= menuSlots.Count) return;
        t.rectTransform.anchoredPosition = origin + new Vector2(0f, menuSlots[slot].x);
        t.rectTransform.sizeDelta = new Vector2(t.rectTransform.sizeDelta.x, menuSlots[slot].y);
    }

    /// <summary>
    /// The ink of a one-line Text (menu lines are capitals, so: their caps): its middle's y from the Text's pivot (x) and
    /// its height (y), in local units, from the glyph quads the game's text layout makes.
    /// </summary>
    Vector2 InkBand(Text t)
    {
        MenuGenerator.Populate(t.text, t.GetGenerationSettings(t.rectTransform.rect.size));
        var verts = MenuGenerator.verts;
        float low = float.MaxValue, high = float.MinValue;
        for (var i = 0; i < verts.Count; i++)
        {
            low = Mathf.Min(low, verts[i].position.y);
            high = Mathf.Max(high, verts[i].position.y);
        }
        if (low > high) return new Vector2(0f, t.fontSize * .7f);
        var ppu = Mathf.Max(.01f, t.pixelsPerUnit);
        return new Vector2((low + high) * .5f / ppu, (high - low) / ppu);
    }

    // A3: a click on a card's line does what its key does.
    void MenuLineClicked(int line)
    {
        if (phase == Phase.Caught)
        {
            if (line == 0) RestartRun();
            return;
        }
        if (phase != Phase.Paused || displaySettingsOpen) return;
        if (line == 0) ResumeFromPause();
        else if (line == 1) ReturnToTitle();
        else ToggleDisplaySettings();
    }

    // A new run: the scene reloads and starts in the title's first room (StartCanonicalStreamedRestart).
    void RestartRun()
    {
        restart = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// The desktop menus' per-frame part: the T5 title legend (100 % from the title's first frame, dismissed by the
    /// first key or mouse button, then out over LogoExitSeconds, as the wordmark leaves), the restart question's
    /// time-out, and a new layout when the screen size or the HUD's scale changes.
    /// </summary>
    void UpdateDesktopMenu()
    {
        if (FrontRoomsHandheld.Active) return;
        if (titleLegend != null)
        {
            if (phase == Phase.Title && !titleLegendDismissed && Input.anyKeyDown) titleLegendDismissed = true;
            var shown = phase == Phase.Title && !titleLegendDismissed;
            titleLegendAlpha = shown ? 1f : Mathf.MoveTowards(titleLegendAlpha, 0f, Time.unscaledDeltaTime / LogoExitSeconds);
            // Never over a card: a pause in the first moments of play drops it at once.
            if (phase != Phase.Title && phase != Phase.Playing) titleLegendAlpha = 0f;
            var visible = titleLegendAlpha > 0f;
            if (titleLegend.gameObject.activeSelf != visible) titleLegend.gameObject.SetActive(visible);
            if (visible)
            {
                var c = titleLegend.color;
                c.a = titleLegendAlpha;
                titleLegend.color = c;
            }
        }
        var screen = new Vector2Int(Screen.width, Screen.height);
        var scale = hudCanvas != null ? hudCanvas.scaleFactor : 1f;
        if (screen != menuLayoutScreen || scale != menuLayoutScale)
        {
            menuLayoutScreen = screen;
            menuLayoutScale = scale;
            LayoutDesktopMenu();
            if (displaySettingsOpen) UpdateDisplaySettingsText();
        }
    }

    void ConfigureLogoMotion(Texture2D texture)
    {
        if (texture == null || logoImage == null || logoImage.transform.parent == null) return;
        var parent = logoImage.transform.parent;
        var rootObject = new GameObject("FrontRooms logo motion");
        if (rootObject == null) return;
        logoMotionRoot = rootObject.transform;
        if (logoMotionRoot == null) return;
        logoMotionRoot.SetParent(parent, false);
        var rootRect = rootObject.GetComponent<RectTransform>();
        if (rootRect == null) rootRect = rootObject.AddComponent<RectTransform>();
        if (rootRect == null) return;
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(.5f, .5f);
        rootRect.pivot = new Vector2(.5f, .5f);
        rootRect.anchoredPosition = FrontRoomsHandheld.Active ? new Vector2(0f, 14f) : Vector2.zero;
        rootRect.sizeDelta = new Vector2(texture.width, texture.height);
        logoMotionRoot.localScale = Vector3.one * LogoScale;

        // The supplied lockup reserves the final 150 px for the two offset S
        // forms. Keeping those pixels as a separate sprite lets the title use
        // the requested slide-and-fade motion without redrawing the artwork.
        const int splitX = 815;
        var leftWidth = Mathf.Clamp(splitX, 1, texture.width - 1);
        var rightWidth = texture.width - leftWidth;
        var leftSprite = Sprite.Create(texture, new Rect(0f, 0f, leftWidth, texture.height), new Vector2(.5f, .5f), 100f);
        var rightSprite = Sprite.Create(texture, new Rect(leftWidth, 0f, rightWidth, texture.height), new Vector2(.5f, .5f), 100f);
        leftSprite.name = "FrontRooms logo left lockup";
        rightSprite.name = "FrontRooms logo sliding SS";
        logoLeftImage = LogoPart(logoMotionRoot, "Logo left lockup", leftSprite, leftWidth, -((texture.width - leftWidth) * .5f));
        logoSlideImage = LogoPart(logoMotionRoot, "Logo sliding SS", rightSprite, rightWidth, -((texture.width - leftWidth) * .5f));
        if (logoLeftImage == null || logoSlideImage == null) return;
        if (whiteLogoMaterial != null)
        {
            logoLeftImage.material = whiteLogoMaterial;
            logoSlideImage.material = whiteLogoMaterial;
        }
        if (logoImage != null) logoImage.enabled = false;
        logoLeftImage.enabled = false;
        logoSlideImage.enabled = false;
    }

    Image LogoPart(Transform parent, string name, Sprite sprite, float width, float x)
    {
        if (parent == null || sprite == null) return null;
        var objectForImage = new GameObject(name);
        if (objectForImage == null) return null;
        objectForImage.transform.SetParent(parent, false);
        var image = objectForImage.AddComponent<Image>();
        if (image == null) return null;
        image.sprite = sprite;
        image.preserveAspect = false;
        image.raycastTarget = false;
        var rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(x, 0f);
        rect.sizeDelta = new Vector2(width, sprite.rect.height);
        return image;
    }
    GameObject TypographyGroup(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        // A RectTransform-only group keeps the HUD typography positioned without
        // introducing a visible card behind information that belongs to the world.
        var g = new GameObject(name); g.transform.SetParent(parent, false);
        var rt = g.AddComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor; rt.anchoredPosition = pos; rt.sizeDelta = size;
        return g;
    }
    GameObject Rule(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
    {
        return Panel(parent, name, anchor, pos, size, color);
    }
    void BuildHud()
    {
        var mobileHud = FrontRoomsHandheld.Active;
        var g = new GameObject("Minimal HUD"); var c = g.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.pixelPerfect = true;
        hudCanvas = c;
        var scale = g.AddComponent<CanvasScaler>();
        scale.uiScaleMode = mobileHud ? CanvasScaler.ScaleMode.ConstantPixelSize : CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scale.referenceResolution = new Vector2(1920, 1080);
        scale.matchWidthOrHeight = .5f;
        if (mobileHud) scale.scaleFactor = FrontRoomsHandheld.PointScale;
        var media = new Color(.078f, .078f, .078f, .9f);
        var paper = C("F4F1E8");
        var accent = C("F4DF3B");

        // The play HUD follows the 72px outer margin and 24px internal rhythm from UI_SYSTEM.
        // Room and threat are direct typography overlays. Their cards obscured the
        // environment and allowed long room names to bleed past the top-left edge.
        roomPanel = TypographyGroup(g.transform, "Room typography", new Vector2(0, 1), mobileHud ? new Vector2(78, -16) : new Vector2(72, -72), mobileHud ? new Vector2(360, 40) : new Vector2(505, 144));
        roomHudGroup = roomPanel.AddComponent<CanvasGroup>();
        if (mobileHud) Rule(roomPanel.transform, "Mobile zone rule", new Vector2(0, 1), new Vector2(1.5f, 0f), new Vector2(3f, 40f), accent);
        roomMetaText = Text(roomPanel.transform, "Room meta", new Vector2(0, 1), mobileHud ? new Vector2(13, 0) : new Vector2(24, -18), mobileHud ? new Vector2(347, 16) : new Vector2(660, 22), mobileHud ? 11 : 13, TextAnchor.UpperLeft);
        // Paper, not BDBAB0 (平面视觉 2026-10-29, as the T5 legend): the meta row is quieter by face and size only, so
        // over a lamp panel its keyline still clears 3:1.
        roomMetaText.color = paper;
        // HUD-NOCARD (平面视觉 2026-10-29): the zone block has no card either, so it keeps its keyline over a lamp panel.
        AddKeyline(roomMetaText);
        roomText = Text(roomPanel.transform, "Room", new Vector2(0, 1), mobileHud ? new Vector2(13, -18) : new Vector2(24, -44), mobileHud ? new Vector2(347, 28) : new Vector2(505, 72), mobileHud ? 26 : 50, TextAnchor.UpperLeft);
        roomText.color = paper;
        roomText.horizontalOverflow = HorizontalWrapMode.Overflow;
        AddKeyline(roomText);
        roomText.verticalOverflow = VerticalWrapMode.Overflow;

        threatPanel = TypographyGroup(g.transform, "Threat typography", Vector2.one, mobileHud ? new Vector2(-78, -16) : new Vector2(-72, -72), mobileHud ? new Vector2(360, 40) : new Vector2(720, 144));
        threatHudGroup = threatPanel.AddComponent<CanvasGroup>();
        // A5 (平面视觉, FIX_CARD_VL217): Bayon 20's line is 36 px, so the desktop box is 40 tall (34 truncated it away).
        threatStateText = Text(threatPanel.transform, "Threat state", new Vector2(1, 1), mobileHud ? new Vector2(0, 0) : new Vector2(-24, -18), mobileHud ? new Vector2(360, 18) : new Vector2(660, 40), mobileHud ? 11 : 20, TextAnchor.UpperRight);
        threatStateText.color = accent; threatStateText.fontStyle = FontStyle.Bold;
        threatStateText.horizontalOverflow = HorizontalWrapMode.Overflow;
        threatStateText.verticalOverflow = VerticalWrapMode.Truncate;
        distanceText = Text(threatPanel.transform, "Distance", new Vector2(1, 1), mobileHud ? new Vector2(0, -18) : new Vector2(-24, -57), mobileHud ? new Vector2(360, 18) : new Vector2(660, 24), mobileHud ? 11 : 13, TextAnchor.UpperRight);
        distanceText.color = C("BDBAB0");
        distanceText.horizontalOverflow = HorizontalWrapMode.Overflow;
        distanceText.verticalOverflow = VerticalWrapMode.Truncate;

        contextPanel = Panel(g.transform, "Context panel", new Vector2(.5f, 0), mobileHud ? new Vector2(0, 31) : new Vector2(0, 72), mobileHud ? new Vector2(452, 60) : new Vector2(920, 120), media);
        contextHudGroup = contextPanel.AddComponent<CanvasGroup>();
        // Phones: the flash line sits on the slot's bottom edge (29 pt up), so a second line grows upward only as far as
        // it must and stays under a two-line caption card (平面视觉, 2026-10-22); centred, since the yellow rule is gone.
        contextText = Text(contextPanel.transform, "Context", new Vector2(.5f, .5f), mobileHud ? new Vector2(0, -4) : new Vector2(10, 0), mobileHud ? new Vector2(410, 52) : new Vector2(820, 72), mobileHud ? 17 : 24, mobileHud ? TextAnchor.LowerCenter : TextAnchor.MiddleCenter);
        contextText.color = paper;
        // A6 (平面视觉): the flash line has no dark card: paper with the keyline, in the same slot. Phones too since
        // 交互设计's X2 (step 2): with the calm card gone, nothing dark is left at the bottom of the phone HUD.
        contextPanel.GetComponent<Image>().enabled = false;
        AddKeyline(contextText);

        captionPanel = Panel(g.transform, "HUD / Captions", new Vector2(.5f, 0), mobileHud ? new Vector2(0, 33) : new Vector2(0, 208), mobileHud ? new Vector2(234, 36) : new Vector2(920, 50), new Color(.078f, .078f, .078f, .75f));
        captionHudGroup = captionPanel.AddComponent<CanvasGroup>();
        captionText = Text(captionPanel.transform, "Captions", new Vector2(.5f, .5f), Vector2.zero, mobileHud ? new Vector2(220, 32) : new Vector2(880, 110), mobileHud ? 17 : 20, TextAnchor.MiddleCenter);
        captionText.color = paper;
        captionText.supportRichText = true;
        captionText.lineSpacing = 1.15f;
        // R4 (Red 2026-10-29, 平面视觉's lit Level 0 test): captions lose the dark card too, paper with the keyline like
        // the flash line. The panel stays as the layout rect: its anchor, the phone's top-hung -66 and UpdateCaptions' sizing.
        captionPanel.GetComponent<Image>().enabled = false;
        AddKeyline(captionText);
        captionPanel.SetActive(false);

        var crosshairObject = Panel(g.transform, "HUD / Crosshair", new Vector2(.5f, .5f), Vector2.zero, mobileHud ? new Vector2(24, 24) : new Vector2(32, 32), Color.white);
        crosshairImage = crosshairObject.GetComponent<Image>();
        crosshairImage.sprite = LoadHudSprite("UI/HUD_Crosshair");
        crosshairImage.preserveAspect = true;
        crosshairHudGroup = crosshairObject.AddComponent<CanvasGroup>();
        // Prompt, hold ring and stamina sit under the crosshair and share its fade.
        // O-36 (游戏视觉 2026-10-29): desktop y -45, 1 px lower than before, so the line sits on the key HUD's line 1
        // baseline (591 at 1080) and does not hop when the aim moves between our prompts and the key HUD's. Phone unchanged.
        promptText = Text(crosshairObject.transform, "Key prompt", new Vector2(.5f, .5f), new Vector2(0f, mobileHud ? -26f : -45f), mobileHud ? new Vector2(360f, 26f) : new Vector2(720f, 26f), mobileHud ? 17 : 20, TextAnchor.MiddleCenter);
        promptText.color = paper;
        promptText.horizontalOverflow = HorizontalWrapMode.Overflow;
        // No dark card under the prompt (hud_build DECISIONS #4): paper with the keyline, phones too (#23, O-5), at 2 px
        // (G6, 平面视觉 2026-10-29). The Outline follows the text's alpha, so the close-up hint (HintAlpha) keeps it in step.
        // HK M-9: the key HUD's line 1 reads the same value (G6: 2 px desktop; phone the 1 pt keyline of #23).
        AddKeyline(promptText, FrontRoomsHudStyle.PromptKeyline(mobileHud));
        holdBar = Panel(crosshairObject.transform, "Hold bar", new Vector2(.5f, .5f), new Vector2(0f, mobileHud ? -48.5f : -68f), mobileHud ? new Vector2(100f, 3f) : new Vector2(120f, 4f), new Color(1f, 1f, 1f, .25f));
        holdBarFill = Panel(holdBar.transform, "Hold bar fill", new Vector2(0f, .5f), Vector2.zero, new Vector2(0f, mobileHud ? 3f : 4f), accent).GetComponent<Image>();
        holdBar.SetActive(false);
        for (var i = 0; i < staminaSegments.Length; i++)
        {
            staminaSegments[i] = Panel(crosshairObject.transform, "Stamina " + (i + 1), new Vector2(.5f, .5f), new Vector2(mobileHud ? -50f + i * 25f : -60f + i * 30f, mobileHud ? -58f : -92f), mobileHud ? new Vector2(20f, 5f) : new Vector2(24f, 6f), accent).GetComponent<Image>();
            staminaSegments[i].enabled = false;
        }

        // Desktop keeps the key row bottom-left (72, 118 up from the corner); the phone HUD puts it top-left under the zone.
        keyPanel = TypographyGroup(g.transform, "HUD / Key", mobileHud ? new Vector2(0, 1) : new Vector2(0, 0), mobileHud ? new Vector2(78, -72) : new Vector2(72, 118), mobileHud ? new Vector2(150, 22) : new Vector2(147, 22));
        keyHudGroup = keyPanel.AddComponent<CanvasGroup>();
        keyImage = Panel(keyPanel.transform, "Key glyph", new Vector2(0, 0), Vector2.zero, new Vector2(40, 22), Color.white).GetComponent<Image>();
        keyImage.sprite = LoadHudSprite("UI/HUD_KeyGlyph");
        keyImage.preserveAspect = true;
        keyText = Text(keyPanel.transform, "Key label", new Vector2(0, 0), new Vector2(54, 0), new Vector2(110, 22), mobileHud ? 11 : 20, TextAnchor.MiddleLeft);
        keyText.color = paper;
        keyText.text = FrontRoomsLanguage.T(KeyPanelLabel);
        FrontRoomsLanguage.Style(keyText, KeyPanelLabel);
        keyText.horizontalOverflow = HorizontalWrapMode.Overflow;
        keyText.verticalOverflow = VerticalWrapMode.Overflow;
        // HK M-1: the key HUD draws the dot, the crosshair states and the key panel.
        keyHud = FrontRoomsKeyHud.Attach(hudCanvas, crosshairObject.GetComponent<RectTransform>(), keyPanel.GetComponent<RectTransform>(), mobileHud);
        gameplayHudAlpha = 0f;
        ApplyGameplayHudAlpha();

        displaySettingsPanel = Panel(g.transform, "Display settings", new Vector2(.5f, .5f), Vector2.zero, mobileHud ? new Vector2(742, 369) : new Vector2(920, 720), new Color(.055f, .055f, .05f, .97f));
        Rule(displaySettingsPanel.transform, "Display settings accent", new Vector2(0, .5f), new Vector2(28, 0), new Vector2(4, 320), accent);
        displaySettingsText = Text(displaySettingsPanel.transform, "Display settings text", new Vector2(.5f, .5f), new Vector2(18, 0), mobileHud ? new Vector2(690, 330) : new Vector2(760, 660), mobileHud ? 17 : 24, TextAnchor.MiddleCenter);
        displaySettingsText.color = paper;
        displaySettingsPanel.SetActive(false);
        overlay = new GameObject("Menu"); overlay.transform.SetParent(g.transform, false); var image = overlay.AddComponent<Image>(); overlayImage = image;
        // The title uses the supplied brand asset with a white runtime shader;
        // the room remains visible behind it while the mark fades in.
        image.color = new Color(.93f, .92f, .88f, .98f);
        var rt = image.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        overlayText = Text(overlay.transform, "Menu text", new Vector2(.5f, .5f), Vector2.zero, mobileHud ? new Vector2(742, 320) : new Vector2(1440, 760), mobileHud ? 16 : 20, TextAnchor.MiddleCenter);
        overlayText.color = C("0A0A0A");
        overlayText.rectTransform.anchoredPosition = mobileHud ? Vector2.zero : new Vector2(0f, 30f);
        if (!mobileHud) BuildDesktopMenu(g, paper, accent);
        if (mobileHud)
        {
            mobilePauseTitle = Text(overlay.transform, "Mobile pause title", new Vector2(.5f, .5f), new Vector2(0f, 127f), new Vector2(600f, 44f), 48, TextAnchor.MiddleCenter);
            mobilePauseTitle.font = bayonFont;
            mobilePauseTitle.color = C("0A0A0A");
            mobilePauseRows = Text(overlay.transform, "Mobile pause rows", new Vector2(.5f, .5f), new Vector2(0f, 21f), new Vector2(600f, 60f), 11, TextAnchor.UpperCenter);
            mobilePauseRows.font = monoFont;
            mobilePauseRows.color = C("0A0A0A");
            mobilePauseRows.lineSpacing = 1.25f;
            mobileCaughtTitle = Text(overlay.transform, "Mobile caught title", new Vector2(.5f, .5f), new Vector2(0f, 127f), new Vector2(600f, 44f), 48, TextAnchor.MiddleCenter);
            mobileCaughtTitle.font = bayonFont;
            mobileCaughtTitle.color = C("0A0A0A");
            mobileCaughtStats = Text(overlay.transform, "Mobile caught stats", new Vector2(.5f, .5f), new Vector2(0f, 36f), new Vector2(720f, 40f), 17, TextAnchor.MiddleCenter);
            mobileCaughtStats.font = monoFont;
            mobileCaughtStats.color = C("0A0A0A");
            mobilePauseTitle.gameObject.SetActive(false);
            mobilePauseRows.gameObject.SetActive(false);
            mobileCaughtTitle.gameObject.SetActive(false);
            mobileCaughtStats.gameObject.SetActive(false);
        }
        if (!mobileHud)
        {
            desktopSettings = FrontRoomsDesktopSettingsView.Create(overlay.transform, g.transform, bayonFont, monoFont, serifFont, cam);
            FrontRoomsDesktopSettingsHost.RequestClose = () => { if (displaySettingsOpen) CloseSettings(); };
            // END RUN on the page (the pause): back to the title at once (Red, 2026-10-30).
            FrontRoomsDesktopSettingsHost.RequestRestart = ReturnToTitle;
        }
        // Keep the settings card inside the pause overlay so its dark surface
        // renders above the light pause wash.  It is still created with the
        // same canvas-scale coordinates, then normalized after reparenting.
        if (displaySettingsPanel != null)
        {
            displaySettingsPanel.transform.SetParent(overlay.transform, false);
            var settingsRect = displaySettingsPanel.GetComponent<RectTransform>();
            settingsRect.anchorMin = settingsRect.anchorMax = new Vector2(.5f, .5f);
            settingsRect.pivot = new Vector2(.5f, .5f);
            settingsRect.anchoredPosition = Vector2.zero;
            settingsRect.sizeDelta = mobileHud ? new Vector2(742f, 369f) : new Vector2(920f, 720f);
        }
        logoImage = Panel(overlay.transform, "FrontRooms brand logo", new Vector2(.5f, .5f), Vector2.zero, mobileHud ? new Vector2(540f, 107.44f) : new Vector2(965f, 192f), Color.white).GetComponent<Image>();
        logoImage.raycastTarget = false;
        if (mobileHud) logoImage.rectTransform.anchoredPosition = new Vector2(0f, 14f);
        logoOutline = logoImage.gameObject.AddComponent<Outline>();
        logoOutline.effectDistance = new Vector2(2f, -2f);
        logoOutline.effectColor = new Color(1f, .86f, .34f, 0f);
        LoadBrandLogo();
        UpdateDisplaySettingsText();
        EnsureMobileTouchLayer();
        ApplyMobileSafeAreaLayout(true);
        // Localization L1: last, so every Text has its final font and size (several set them after Text()). Fixed words were
        // styled with their English where they were set.
        foreach (var t in g.GetComponentsInChildren<Text>(true)) FrontRoomsLanguage.Style(t);
    }

    void ApplyMobileSafeAreaLayout(bool force = false)
    {
        if (!FrontRoomsHandheld.Active || hudCanvas == null)
            return;
        // Squarer views keep 16:9's horizontal field (Red, 2026-10-24); the title and pause frames too, where the rig rests.
        rig?.ApplyAspectFov();
        TickMobileGiveWay();

        // The bleed safe area: iOS's, less a camera band it folded in (the Duo's outer display). What touches that camera
        // gives way on its own; everything else keeps to the edge.
        var area = FrontRoomsHandheld.BleedSafeAreaPixels;
        var screenPixels = FrontRoomsHandheld.ScreenPixels;
        var screen = new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(screenPixels.x)), Mathf.Max(1, Mathf.RoundToInt(screenPixels.y)));
        if (area.width <= 0f || area.height <= 0f)
            area = new Rect(0f, 0f, screen.x, screen.y);
        var pointScale = Mathf.Max(.01f, FrontRoomsHandheld.PointScale);
        if (!force && area == lastMobileHudSafeArea && screen == lastMobileHudScreenSize && Mathf.Approximately(lastMobileHudPointScale, pointScale))
            return;

        var scaler = hudCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = pointScale;
        hudCanvas.scaleFactor = pointScale;
        if (vectorLogoDocument != null && vectorLogoDocument.panelSettings != null)
        {
            vectorLogoDocument.panelSettings.scaleMode = UiPanelScaleMode.ConstantPixelSize;
            vectorLogoDocument.panelSettings.scale = pointScale;
        }
        Canvas.ForceUpdateCanvases();
        area = FrontRoomsHandheld.BleedSafeAreaPixels;
        if (area.width <= 0f || area.height <= 0f)
            area = new Rect(0f, 0f, screen.x, screen.y);
        lastMobileHudSafeArea = area;
        lastMobileHudScreenSize = screen;
        lastMobileHudPointScale = pointScale;

        // One HUD unit is a UIKit point. Expand the world and reflow foreground content
        // from the current Unity view; the 874x402 phone master remains the spacing baseline.
        var pixelsPerPoint = pointScale;
        var safeMinX = area.xMin / pixelsPerPoint;
        var safeMaxX = area.xMax / pixelsPerPoint;
        var safeMinY = area.yMin / pixelsPerPoint;
        var safeMaxY = area.yMax / pixelsPerPoint;
        var frameWidth = screen.x / pixelsPerPoint;
        var frameHeight = screen.y / pixelsPerPoint;
        var safeWidth = Mathf.Max(1f, safeMaxX - safeMinX);
        var topInset = Mathf.Max(0f, frameHeight - safeMaxY);
        var leftX = safeMinX + 16f;
        var keyTop = topInset + 72f;
        var rightEdge = safeMaxX - 16f;
        var bottomHint = safeMinY + 8f;
        var centerX = Mathf.Clamp(frameWidth * .5f, safeMinX, safeMaxX);
        var centerY = Mathf.Clamp(frameHeight * .5f, safeMinY, safeMaxY);
        var logoCenterY = Mathf.Clamp(frameHeight * .5f + 14f, safeMinY + 54f, safeMaxY - 54f);
        var compact = safeWidth < 600f;
        var zoneWidth = Mathf.Max(1f, compact ? safeWidth - 88f : Mathf.Min(347f, safeWidth * .52f));
        var threatWidth = Mathf.Max(1f, compact ? safeWidth - 32f : Mathf.Min(360f, safeWidth * .4f - 44f));
        var threatOffset = compact ? 110f : 16f;
        // The top band gives way sideways to a camera or a rounded corner (Red, 2026-10-24: the outer camera at the top
        // left pushes the zone right), eased in TickMobileGiveWay; on one row the zone then stops short of the threat.
        mobileZoneBox = new Rect(leftX, frameHeight - topInset - 56f, zoneWidth + 13f, 40f);
        mobileThreatBox = new Rect(rightEdge - threatWidth, frameHeight - topInset - threatOffset - 40f, threatWidth, 40f);
        mobileKeyBox = new Rect(leftX, frameHeight - keyTop - 22f, 150f, 22f);
        // The flash line (the hint slot, bottom centre) as well: on the outer display upright, the camera is at its corner.
        var flashWidth = Mathf.Min(410f, safeWidth - 32f);
        mobileFlashBox = new Rect(centerX - flashWidth * .5f, bottomHint, flashWidth, 52f);
        // On one row, a zone pushed right by a corner or the outer camera stops short of the threat readout.
        if (!compact)
        {
            var zoneShift = FrontRoomsHandheld.ClearCornerBlock(mobileZoneBox).x - mobileZoneBox.x;
            zoneWidth = Mathf.Max(88f, Mathf.Min(zoneWidth, mobileThreatBox.xMin - 12f - (mobileZoneBox.xMin + zoneShift) - 13f));
        }
        foreach (var text in new[] { roomMetaText, roomText })
        {
            if (text == null) continue;
            text.rectTransform.sizeDelta = new Vector2(zoneWidth, text.rectTransform.sizeDelta.y);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = text == roomText ? 20 : 9;
            text.resizeTextMaxSize = text == roomText ? 26 : 11;
        }
        if (roomPanel != null) ((RectTransform)roomPanel.transform).sizeDelta = new Vector2(zoneWidth + 13f, 40f);
        foreach (var text in new[] { threatStateText, distanceText })
        {
            if (text == null) continue;
            text.rectTransform.sizeDelta = new Vector2(threatWidth, text.rectTransform.sizeDelta.y);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
        }
        if (threatPanel != null) ((RectTransform)threatPanel.transform).sizeDelta = new Vector2(threatWidth, 40f);
        contextText.rectTransform.sizeDelta = new Vector2(Mathf.Min(410f, safeWidth - 32f), 52f);
        promptText.rectTransform.sizeDelta = new Vector2(Mathf.Min(360f, safeWidth - 32f), 26f);
        promptText.horizontalOverflow = HorizontalWrapMode.Wrap;
        var logoWidth = Mathf.Min(540f, Mathf.Max(1f, safeWidth - 32f));
        if (logoImage != null) logoImage.rectTransform.sizeDelta = new Vector2(logoWidth, logoWidth * 192f / 965f);
        if (logoMotionRoot != null) logoMotionRoot.localScale = Vector3.one * (logoWidth / 965f);

        // The top band hangs from the top edge (Unity's y is up): anchored at the bottom, these sat below the screen.
        mobileZoneAt = new Vector2(leftX, -topInset - 16f);
        mobileKeyAt = new Vector2(leftX, -keyTop);
        mobileThreatAt = new Vector2(rightEdge - frameWidth, -topInset - threatOffset);
        // Laid out off screen (the title, a menu), the band is already where it gives way; in play it eases there.
        var firstLayout = !mobileGiveLaidOut;
        mobileGiveLaidOut = true;
        UpdateMobileGiveWay(firstLayout);
        mobileGiveShown = new Vector4(mobileZoneGive.Current, mobileKeyGive.Current, mobileThreatGive.Current, mobileFlashGive.Current);
        mobileDropShown = new Vector2(mobileZoneDrop.Current, mobileThreatDrop.Current);
        SetMobileAnchor(roomPanel != null ? roomPanel.transform : null, Vector2.up, new Vector2(0f, 1f), mobileZoneAt + new Vector2(mobileZoneGive.Current, mobileZoneDrop.Current));
        SetMobileAnchor(keyPanel != null ? keyPanel.transform : null, Vector2.up, new Vector2(0f, 1f), mobileKeyAt + new Vector2(mobileKeyGive.Current, mobileZoneDrop.Current));
        SetMobileAnchor(threatPanel != null ? threatPanel.transform : null, Vector2.one, new Vector2(1f, 1f), mobileThreatAt + new Vector2(mobileThreatGive.Current, mobileThreatDrop.Current));
        mobileFlashAt = new Vector2(0f, bottomHint);
        SetMobileAnchor(contextPanel != null ? contextPanel.transform : null, new Vector2(.5f, 0f), new Vector2(.5f, 0f), mobileFlashAt + Vector2.right * mobileFlashGive.Current);
        // Captions hang from a fixed top 66 pt under the dot and grow downward (平面视觉, 2026-10-22): the first line never
        // jumps when a second arrives, the card stays 5.5 pt under the stamina dashes (dot −58) on every aspect, and two
        // lines still end above a two-line flash in the hint slot (bottom 31). Pinned at bottom 99 and growing upward,
        // one line touched the dashes and two covered them.
        SetMobileAnchor(captionPanel != null ? captionPanel.transform : null, new Vector2(centerX / frameWidth, centerY / frameHeight), new Vector2(.5f, 1f), new Vector2(0f, -66f));
        SetMobileAnchor(crosshairImage != null ? crosshairImage.transform as RectTransform : null, new Vector2(centerX / frameWidth, centerY / frameHeight), new Vector2(.5f, .5f), Vector2.zero);
        SetMobileAnchor(displaySettingsPanel != null ? displaySettingsPanel.transform : null, new Vector2(centerX / frameWidth, centerY / frameHeight), new Vector2(.5f, .5f), Vector2.zero);
        SetMobileAnchor(overlayText, new Vector2(centerX / frameWidth, centerY / frameHeight), new Vector2(.5f, .5f), Vector2.zero);
        SetMobileAnchor(logoImage, new Vector2(centerX / frameWidth, logoCenterY / frameHeight), new Vector2(.5f, .5f), Vector2.zero);
        SetMobileAnchor(logoMotionRoot, new Vector2(centerX / frameWidth, logoCenterY / frameHeight), new Vector2(.5f, .5f), Vector2.zero);
        SetMobileAnchor(mobilePauseTitle, new Vector2(centerX / frameWidth, centerY / frameHeight), new Vector2(.5f, .5f), new Vector2(0f, 127f));
        SetMobileAnchor(mobilePauseRows, new Vector2(centerX / frameWidth, centerY / frameHeight), new Vector2(.5f, .5f), new Vector2(0f, 21f));
        SetMobileAnchor(mobileCaughtTitle, new Vector2(centerX / frameWidth, centerY / frameHeight), new Vector2(.5f, .5f), new Vector2(0f, 127f));
        SetMobileAnchor(mobileCaughtStats, new Vector2(centerX / frameWidth, centerY / frameHeight), new Vector2(.5f, .5f), new Vector2(0f, 41f));
        if (vectorLogoRoot != null)
        {
            vectorLogoRoot.style.left = UiLength.Percent(centerX / frameWidth * 100f);
            vectorLogoRoot.style.top = UiLength.Percent((1f - logoCenterY / frameHeight) * 100f);
            vectorLogoRoot.style.marginLeft = -482.5f;
            vectorLogoRoot.style.marginTop = -96f;
            var logoScale = logoWidth / 965f;
            vectorLogoRoot.style.scale = new UnityEngine.UIElements.Scale(new Vector3(logoScale, logoScale, 1f));
        }
    }

    /// <summary>A give-way to a camera or a rounded corner: eased over 0.28 s (OutCubic), the touch layer's transition;
    /// snapped off screen and under Reduce Motion.</summary>
    static void MobileGiveWay(FrontRoomsTouchMotion.Value value, float target, bool snap)
    {
        if (snap || FrontRoomsHandheld.ReduceMotion) value.Snap(target);
        else value.To(target, .28f, FrontRoomsTouchMotion.Ease.OutCubic);
    }

    /// <summary>
    /// Where the top band and the flash line give way now: each clears what it draws (the zone and key are left-aligned,
    /// the threat right-aligned, the flash centred) of every camera and rounded corner, so a radius iOS reports a frame
    /// late, the inner camera turning on, or a longer zone name all move them. Off screen they snap.
    /// </summary>
    void UpdateMobileGiveWay(bool snapAll)
    {
        var offScreen = snapAll || roomPanel == null || !roomPanel.activeInHierarchy;
        var zone = mobileZoneBox;
        zone.width = Mathf.Min(zone.width, 13f + Mathf.Max(PreferredWidth(roomText), PreferredWidth(roomMetaText)));
        var key = mobileKeyBox;
        key.width = Mathf.Min(key.width, 54f + PreferredWidth(keyText));
        var threat = mobileThreatBox;
        var threatText = Mathf.Max(PreferredWidth(threatStateText), PreferredWidth(distanceText));
        threat.xMin = Mathf.Max(threat.xMin, threat.xMax - threatText);
        var flash = mobileFlashBox;
        var flashText = Mathf.Min(flash.width, PreferredWidth(contextText));
        flash = new Rect(flash.center.x - flashText * .5f, flash.y, flashText, flash.height);
        // The zone and the threat are corner blocks: a rounded top corner moves them down and in, 16 pt from the curve
        // (Red, 2026-10-24); a camera still pushes them sideways. The key line keeps its place under the zone.
        var zoneClear = FrontRoomsHandheld.ClearCornerBlock(zone);
        var zoneDrop = zoneClear.y - zone.y;
        MobileGiveWay(mobileZoneGive, zoneClear.x - zone.x, offScreen);
        MobileGiveWay(mobileZoneDrop, zoneDrop, offScreen);
        key.y += zoneDrop;
        var keyOff = offScreen || keyPanel == null || !keyPanel.activeInHierarchy;
        MobileGiveWay(mobileKeyGive, Mathf.Max(FrontRoomsHandheld.ClearRect(key).x - key.x, zoneClear.x - zone.x), keyOff);
        var threatClear = FrontRoomsHandheld.ClearCornerBlock(threat);
        MobileGiveWay(mobileThreatGive, threatClear.x - threat.x, offScreen);
        MobileGiveWay(mobileThreatDrop, threatClear.y - threat.y, offScreen);
        MobileGiveWay(mobileFlashGive, flashText > 0f ? FrontRoomsHandheld.ClearRect(flash).x - flash.x : 0f, offScreen || contextPanel == null || !contextPanel.activeInHierarchy);
    }

    static float PreferredWidth(Text text) => text != null && !string.IsNullOrEmpty(text.text) ? text.preferredWidth : 0f;

    /// <summary>The top band follows its eased give-way every frame (the layout itself only runs when the view changes).</summary>
    void TickMobileGiveWay()
    {
        if (!mobileGiveLaidOut) return;
        UpdateMobileGiveWay(false);
        var now = new Vector4(mobileZoneGive.Current, mobileKeyGive.Current, mobileThreatGive.Current, mobileFlashGive.Current);
        var drop = new Vector2(mobileZoneDrop.Current, mobileThreatDrop.Current);
        if ((now - mobileGiveShown).sqrMagnitude < 1e-4f && (drop - mobileDropShown).sqrMagnitude < 1e-4f) return;
        mobileGiveShown = now;
        mobileDropShown = drop;
        if (roomPanel != null) ((RectTransform)roomPanel.transform).anchoredPosition = mobileZoneAt + new Vector2(now.x, drop.x);
        if (keyPanel != null) ((RectTransform)keyPanel.transform).anchoredPosition = mobileKeyAt + new Vector2(now.y, drop.x);
        if (threatPanel != null) ((RectTransform)threatPanel.transform).anchoredPosition = mobileThreatAt + new Vector2(now.z, drop.y);
        if (contextPanel != null) ((RectTransform)contextPanel.transform).anchoredPosition = mobileFlashAt + Vector2.right * now.w;
    }

    static void SetMobileAnchor(Component target, Vector2 anchor, Vector2 pivot, Vector2 position)
    {
        if (target == null) return;
        var rect = target as RectTransform ?? target.GetComponent<RectTransform>();
        SetMobileAnchor(rect, anchor, pivot, position);
    }

    static void SetMobileAnchor(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
    }
    void SetPhase(Phase p)
    {
        // C8: a stray resume never strands a close-up; the phase a pause came from is recorded here, for every caller.
        if (p == Phase.Playing && InspectingNow) { EndInspect(false); return; }
        if (p == Phase.Paused && phase != Phase.Paused) pausedFrom = phase;
        var wasPlaying = phase == Phase.Playing;
        var wasPaused = phase == Phase.Paused;
        var wasHud = wasPlaying || phase == Phase.Inspect;
        phase = p; bool playing = p == Phase.Playing;
        // Doors freeze with the game (a swing, a jolt, a key turn's watchdog), so nothing moves or sounds under the pause card.
        // A43: a live close-up keeps them going (the Relay comes through a door while the player looks).
        if (map != null) map.DoorsPaused = !playing && !(p == Phase.Inspect && inspectLive);
        // Inspect is a HUD phase: the panels stay and rig.HudFade fades them; the crosshair goes at once.
        var hudPhase = playing || p == Phase.Inspect;
        if ((p == Phase.Paused) != wasPaused) Paused?.Invoke(p == Phase.Paused);
        if (hudPhase && !wasHud) gameplayHudAlpha = 0f;
        if (!hudPhase) gameplayHudAlpha = 0f;
        // A reading close-up pauses the world (Phase.Inspect + timeScale 0; A43: a live one keeps 1); 1 is restored only where
        // the close-up set 0. The pause card over any close-up freezes it.
        var freeze = (p == Phase.Inspect && !inspectLive) || (p == Phase.Paused && pausedFrom == Phase.Inspect);
        if (freeze) Time.timeScale = 0f;
        else if (timeFrozenByInspect) Time.timeScale = 1f;
        timeFrozenByInspect = freeze;
        if (p != Phase.Paused) pausedFrom = Phase.Playing;
        if (p != Phase.Paused)
        {
            displaySettingsOpen = false;
            if (displaySettingsPanel != null) displaySettingsPanel.SetActive(false);
            if (desktopSettings != null) desktopSettings.SetOpen(false);
        }
        // Mobile keeps the transparent overlay alive while the authored logo
        // is leaving. This is the only frame in which the title layer and the
        // playing HUD overlap; all title children are non-raycast targets.
        var keepMobileTitleFade = FrontRoomsHandheld.Active && playing && logoFading;
        overlay.SetActive(!hudPhase || keepMobileTitleFade); if (crosshairImage != null) crosshairImage.enabled = playing && (keyHud == null || !keyHud.OwnsDot);
        if (overlayImage != null) overlayImage.color = p == Phase.Title || FrontRoomsHandheld.Active
            ? new Color(0f, 0f, 0f, 0f)
            : new Color(.93f, .92f, .88f, .98f);
        if (logoImage != null)
        {
            logoImage.enabled = p == Phase.Title && logoMotionRoot == null && !vectorLogoActive;
            if (p == Phase.Title && logoMotionRoot == null) logoImage.color = new Color(1f, 1f, 1f, titleLogoAlpha);
        }
        // The vector wordmark is its own panel: it keeps fading out over the start of play.
        if (vectorLogoRoot != null) vectorLogoRoot.style.display = LogoShown ? UiDisplayStyle.Flex : UiDisplayStyle.None;
        if (logoMotionRoot != null)
        {
            if (logoLeftImage != null) logoLeftImage.enabled = LogoShown;
            if (logoSlideImage != null) logoSlideImage.enabled = LogoShown;
        }
        if (roomPanel != null) roomPanel.SetActive(hudPhase);
        if (threatPanel != null) threatPanel.SetActive(hudPhase);
        if (keyPanel != null) keyPanel.SetActive(hudPhase && keyHud == null);
        if (contextPanel != null) contextPanel.SetActive(false);
        ApplyGameplayHudAlpha();
        Cursor.lockState = hudPhase ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !hudPhase;
        if (overlayText != null)
        {
            overlayText.enabled = !FrontRoomsHandheld.Active && p != Phase.Title && !displaySettingsOpen;
            if (p == Phase.Title) overlayText.text = "";
            else if (p == Phase.Paused) overlayText.text = PauseText();
        }
        if (FrontRoomsHandheld.Active)
        {
            var showPauseCopy = p == Phase.Paused && !displaySettingsOpen;
            if (mobilePauseTitle != null) mobilePauseTitle.gameObject.SetActive(showPauseCopy);
            if (mobilePauseRows != null) mobilePauseRows.gameObject.SetActive(showPauseCopy);
            if (mobileCaughtTitle != null) mobileCaughtTitle.gameObject.SetActive(p == Phase.Caught);
            if (mobileCaughtStats != null) mobileCaughtStats.gameObject.SetActive(p == Phase.Caught);
            if (mobilePauseTitle != null) mobilePauseTitle.text = FrontRoomsLanguage.T("PAUSED");
            if (mobilePauseRows != null) mobilePauseRows.text = FrontRoomsLanguage.T("MOVE        DRAG · LOOK\nUSE · DOOR        HOLD USE · BREAK GLASS");
        }
        // Re-locking the cursor can report one large mouse jump: ignore the first frames back (also Paused → Inspect).
        if (hudPhase && wasPaused) mouseSettleFrames = 2;
        if (p == Phase.Caught)
        {
            // C22 (D13; 叙事设计's line): the floor first, the run's totals (the Relay keeps its doors broken across
            // floors, Park), and a count of one in the singular; no TIER (C04 c, the readout has it). Under the title the
            // subtitle OCCUPANT ACCOUNTED FOR (叙事 strings v1.1, Red 10-22; 平面视觉's A7 and FIX_CARD §E1: its own Text, on this slot).
            var caughtStats = FloorLabel(floor) + "  /  " + FrontRoomsLanguage.F("{0} S", Mathf.RoundToInt(elapsed).ToString()) + "  /  " + Counted(zonesVisited.Count, "{0} ZONE", "{0} ZONES")
                + "  /  " + Counted(keysTaken, "{0} KEY", "{0} KEYS") + "  /  " + Counted(relay == null ? 0 : relay.DoorsBroken, "{0} DOOR BROKEN", "{0} DOORS BROKEN");
            if (FrontRoomsHandheld.Active)
            {
                if (mobileCaughtTitle != null) mobileCaughtTitle.text = FrontRoomsLanguage.T("CAUGHT");
                if (mobileCaughtStats != null) mobileCaughtStats.text = caughtStats;
            }
            else if (overlayText != null)
            {
                // A7 + A1: the stats line (IBM Plex Mono 20) and R  TRY AGAIN (ink, with the marker) are their own
                // Texts, laid on this text's slots by LayoutDesktopMenu.
                overlayText.text = "<size=88>" + FrontRoomsLanguage.T("CAUGHT") + "</size>\n" + MenuSlot(28) + "\n\n" + MenuSlot(24) + "\n\n" + MenuSlot(20);
                if (menuStats != null) menuStats.text = caughtStats;
            }
        }
        // The desktop card's own lines (A1–A3, A7): a new card starts with the first line marked.
        menuHover = -1;
        LayoutDesktopMenu();
        UpdateMobileTouchMenu();
        // A pause opens the settings sheet straight away, desktop and phones (Red, 2026-10-24: no PAUSED card in between).
        if (p == Phase.Paused && !wasPaused && PauseIsSettings && !displaySettingsOpen) ToggleDisplaySettings();
    }
    void OnApplicationFocus(bool focused)
    {
#if UNITY_EDITOR
        if (autopilot) return;
#endif
        if (!focused && (phase == Phase.Playing || phase == Phase.Inspect)) SetPhase(Phase.Paused);
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) SaveEvents();
#if UNITY_EDITOR
        if (autopilot) return;
#endif
        if (paused && (phase == Phase.Playing || phase == Phase.Inspect)) SetPhase(Phase.Paused);
    }
    void Update()
    {
        if (!Application.isPlaying) return;
        ApplyMobileSafeAreaLayout();
        var input = FrontRoomsInput.ReadFrame();
        UpdateDesktopMenu();
        if (phase == Phase.Title && input.StartDown) RequestTitleStart();
        else if (phase == Phase.Paused && input.SettingsDown && !(DesktopPauseIsSettings && displaySettingsOpen))
        {
            // The touch sheet's RESUME (its old BACK) closes the pause; the desktop's O is ignored while the sheet is open.
            if (displaySettingsOpen && PauseIsSettings) CloseSettings();
            else ToggleDisplaySettings();
        }
        else if (input.PauseDown && displaySettingsOpen) { if (desktopSettings == null || !desktopSettings.ConsumeEscape()) CloseSettings(); }
        else if (displaySettingsOpen) HandleSettingsKeys();
        else if (phase == Phase.Inspect)
        {
            // L3' (computer_ui 41 §5.3, 游戏视觉 2026-10-30): on an Office terminal E, S and Esc are the terminal's own keys (EXIT,
            // down, CANCEL; O-3) and the terminal says when to leave (EXIT, CANCEL on MAIN, pad MENU; on touch BACK and the
            // pull-back), after CancelAfter as every exit. Any MOVE key held then is latched until released (C25b).
            if (FrontRoomsInspectTerminal.Owns)
            {
                if (FrontRoomsInspectTerminal.Tick(FrontRoomsInspect.Dt, input) && FrontRoomsInspect.Age >= FrontRoomsShotTimings.Inspect.CancelAfter)
                {
                    EndInspect(false);
                    moveLatch = input.Move.sqrMagnitude > .01f;
                }
            }
            // C6: E, S or Esc leaves the close-up once CancelAfter has passed (Esc exits: no pause-card loop).
            else if ((input.UseDown || input.ShotBackDown || input.PauseDown) && FrontRoomsInspect.Age >= FrontRoomsShotTimings.Inspect.CancelAfter)
                EndInspect(input.ShotBackDown);
        }
        else if (input.PauseDown && (phase == Phase.Playing || phase == Phase.Paused))
        {
            if (phase == Phase.Playing) SetPhase(Phase.Paused);
            else ResumeFromPause();
        }
        // R (and the touch sheet's END RUN chip) in the pause ends the run back to the title, at once and on every
        // platform (Red, 2026-10-30: "在回退标题前不用确认一下", "三端保持一致"); on the caught card R is TRY AGAIN, a retry.
        if ((!displaySettingsOpen || PauseIsSettings) && input.RestartDown && phase != Phase.Playing && phase != Phase.Title && phase != Phase.Inspect)
        {
            if (phase == Phase.Paused) ReturnToTitle();
            else RestartRun();
        }
        var dt = Mathf.Min(Time.deltaTime, .1f);
#if UNITY_EDITOR
        if (autopilot) AutopilotTick(dt);
#endif
        if (phase == Phase.Title) UpdateTitleSequence(dt);
        if (logoFading) UpdateLogoFade(dt);
        if (startupCurtainAlpha > 0f || startupCurtainTarget > 0f) UpdateStartupCurtain(Time.unscaledDeltaTime);
        // C7: Inspect first, then Playing, so the rig ticks exactly once per frame (a live close-up's in UpdateMapPlay, A43).
        if (inspectCooldown > 0f) inspectCooldown -= FrontRoomsInspect.Dt;
        // A43: a close-up that asks to end (a live one's perceivable threat, or its shot let go by the rig) ends now, with no
        // CancelAfter wait.
        if (phase == Phase.Inspect && FrontRoomsInspect.WantsEnd) EndInspect(false);
        var liveInspect = phase == Phase.Inspect && inspectLive && mapPlay;
        if (phase == Phase.Inspect)
        {
            var u = FrontRoomsInspect.Dt;
            var look = Vector2.zero;
            if (mouseSettleFrames > 0) mouseSettleFrames--; else look = input.Look;
            // C7: the zoom input (T3): the wheel steps it and the right button toggles it; touch pinches and double-taps.
            // The second tap (Red 2026-10-30), or RMB, or RT: the content alone, full screen, and back.
            var fullView = input.ZoomToggleDown && FrontRoomsInspect.FullViewOffered;   // a flat close-up's tap is never the zoom's
            if (fullView) FrontRoomsInspect.ToggleFullView();
            FrontRoomsInspect.Tick(u, look, input.Zoom, input.ZoomToggleDown && !fullView);
            // A43: a live close-up's rig ticks with the world, in UpdateMapPlay below.
            if (!liveInspect) rig?.Tick(u);
        }
        if ((phase == Phase.Playing && mapPlay) || liveInspect)
        {
            elapsed += dt;
            flashTime -= dt;
            FrontRoomsCaptions.Tick(dt);
            for (var i = rattleJolts.Count - 1; i >= 0; i--)
            {
                if (elapsed < rattleJolts[i].at) continue;
                Kick(rattleJolts[i].p, DoorJoltRange, FrontRoomsShotTimings.Rattle.CameraImpulseDeg, FrontRoomsShotTimings.Open.PushImpulseDecay);
                rattleJolts.RemoveAt(i);
            }
            UpdateMapPlay(dt);
        }
        // C22 (D8): the stairwells show with X's chunk, in every phase of a run.
        if (mapPlay) UpdateStairwells();
        UpdateHud();
    }
    void UpdateHud()
    {
        bool play = phase == Phase.Playing;
        var inspecting = phase == Phase.Inspect;
        // C11: Inspect keeps the gameplay HUD alpha (rig.HudFade fades the panels); the prompt slot carries the hint.
        if (play || inspecting)
            gameplayHudAlpha = Mathf.MoveTowards(gameplayHudAlpha, 1f, Time.unscaledDeltaTime / GameplayHudFadeSeconds);
        else
            gameplayHudAlpha = 0f;
        ApplyGameplayHudAlpha();
        if (keyHud != null && (!play || !mapPlay || map == null || playerRoot == null))
        {
            keyHud.SetAim(FrontRoomsKeyHud.Aim.None, default, false);
            keyHud.SetHeldKey(false, default);
        }
        if (!mapPlay || map == null || playerRoot == null) return;
        // In the stream rooms the run starts in: Level 0's lobby, before zone 01.
        var zone = inStartRooms ? new ZoneInfo { height = ZoneHeight.Standard, theme = ZoneTheme.Level0 } : map.ZoneOf(map.CellOf(playerRoot.position));
        // A26: floor 1's station reads as the front rooms too (InFrontRooms). The key panel below shows the key that opens
        // locked doors where the player stands: that zone's key, or on a floor-key floor the floor key (M3).
        var frontRooms = InFrontRooms;
        var shown = frontRooms ? new ZoneInfo { height = ZoneHeight.Standard, theme = ZoneTheme.Level0 } : zone;
        var released = relay != null && relay.Released;
        var threat = !released ? "" : relay.State == HunterState.BreakDoor ? FrontRoomsLanguage.T("IT  /  BREAKING DOOR") : FrontRoomsLanguage.F("IT  /  {0}", FrontRoomsLanguage.T(relay.State.ToString().ToUpperInvariant()));
        var hudOn = play || inspecting;
        // C04 (Red, 2026-10-21, "all three"): no TIER on the HUD; the tier still runs and is logged (TierChanged).
        roomMetaText.text = hudOn ? FrontRoomsLanguage.F("ZONE {0}", zonesVisited.Count.ToString("00")) + "  /  " + FrontRoomsLanguage.T(shown.height.ToString().ToUpperInvariant()) + "  " + FrontRoomsLanguage.F("{0} M", MapGrid.CeilingHeight(shown.height).ToString("0.0")) : "";
        roomText.text = hudOn ? (frontRooms ? FrontRoomsLanguage.F("{0} / FRONT ROOMS", FloorLabel(1)) : ZoneName(zone)) : "";
        // The Relay's state and distance are an assist, off by default. C04 (Q5c; V1): the run's tier shows here only,
        // on the distance line ("IT  23 M  /  TIER 2"), never on the always-on HUD.
        var showThreat = hudOn && released && FrontRoomsSettings.RelayReadout;
        threatStateText.text = showThreat ? threat : "";
        distanceText.text = showThreat ? FrontRoomsLanguage.F("IT  {0} M", Mathf.RoundToInt(RelayDistance()).ToString()) + "  /  " + FrontRoomsLanguage.F("TIER {0}", tier.ToString()) : "";
        if (crosshairImage != null) crosshairImage.enabled = play && (keyHud == null || !keyHud.OwnsDot);
        // The hint line shows flashes only. M1 (Red's T5, 2026-10-22): the first-run CalmHint card is gone; the
        // controls are on the title and the pause card (the T5 legend; on phones 交互设计's X2 hints).
        var hint = flashTime > 0f && !string.IsNullOrEmpty(flash) ? flash : "";
        contextText.text = play ? hint : "";
        // HK M-2: what the crosshair is on, its zone identity, whether that key is held, and for READ / EXAMINE the
        // words as shown (the eye's line; one predicate with the prompt, grammar sec. 1). No string parsing. In a
        // close-up (Phase.Inspect) the aim is None: the eye is latched in its react / close-up state until
        // FrontRoomsInspect.Closed (the key HUD hears it itself), and the crosshair is hidden by the shot's fade.
        // Before the prompt line: OwnsPrompt reads this frame's aim, so the map never shows a state's words itself.
        if (keyHud != null && play)
            keyHud.SetAim(aimKind, HudIdentity(aimKeyZone), aimKeyHeld, FrontRoomsKeyHud.WantsWords(aimKind) && prompt != null ? DisplayPrompt(prompt) : null);
        if (promptText != null)
        {
            if (inspecting) promptText.text = DisplayPrompt(FrontRoomsInspect.HintText);
            else promptText.text = play && prompt != null && (keyHud == null || !keyHud.OwnsPrompt) ? DisplayPrompt(prompt) : "";
            var c = promptText.color;
            c.a = inspecting ? FrontRoomsInspect.HintAlpha : 1f;   // the hint's own alpha, not the HUD fade
            promptText.color = c;
            if (!promptHomeSet) { promptHome = promptText.rectTransform.anchoredPosition; promptHomeSet = true; }
            var canvasHeight = hudCanvas != null ? ((RectTransform)hudCanvas.transform).rect.height : 1080f;
            promptText.rectTransform.anchoredPosition = inspecting ? new Vector2(promptHome.x, -(canvasHeight * .5f - InspectHintBottom)) : promptHome;
        }
        if (holdBar != null)
        {
            holdBar.SetActive(play && holdProgress > 0f);
            // The fill follows the bar's own size (phones 100 × 3, desktop 120 × 4).
            var hb = ((RectTransform)holdBar.transform).sizeDelta;
            holdBarFill.rectTransform.sizeDelta = new Vector2(hb.x * holdProgress, hb.y);
        }
        UpdateMobileTouchPrompt();
        var tired = play && stamina < StaminaSeconds - .01f;
        for (var i = 0; i < staminaSegments.Length; i++)
        {
            if (staminaSegments[i] == null) continue;
            staminaSegments[i].enabled = tired;
            staminaSegments[i].color = stamina >= (i + .5f) * (StaminaSeconds / staminaSegments.Length) ? new Color(.957f, .875f, .231f) : new Color(1f, 1f, 1f, .25f);
        }
        if (roomPanel != null) roomPanel.SetActive(hudOn);
        if (threatPanel != null) threatPanel.SetActive(showThreat);
        // A presented key's panel waits for the pocket (KeyPickup.PocketEnd), then fades in over PanelFadeIn
        // (keyPanelFade, in ApplyGameplayHudAlpha). On the instant path (no model) it is up at once, as before.
        var keyZone = map.FloorKeyRule ? FrontRoomsMapWorld.FloorKeyZone : zone.id;
        var presentingKey = doorShot != null && doorShot.Presenting(keyZone);
        if (presentingKey) keyPanelFade = 0f;
        else if (keyPanelFade < 1f) keyPanelFade = Mathf.MoveTowards(keyPanelFade, 1f, Mathf.Min(Time.deltaTime, .1f) / FrontRoomsShotTimings.KeyPickup.PanelFadeIn);
        var showKey = hudOn && !inStartRooms && map.HasKeyFor(keyZone) && !presentingKey;
        if (showKey && keyText != null && (!keyLabelSet || !keyZone.Equals(keyLabelZone)))
        {
            keyLabelZone = keyZone;
            keyLabelSet = true;
            keyText.text = KeyLabel(zone);
        }
        // HK M-2: the key HUD owns the panel (and the P4 flight); the map's own panel stays off.
        if (keyHud != null)
        {
            if (keyPanel != null && keyPanel.activeSelf) keyPanel.SetActive(false);
            // KP-1: from the hand-off the key HUD shows the key (its P4 flight), not only after PocketEnd
            var handedOff = presentingKey && hudOn && !inStartRooms && map.HasKeyFor(keyZone) && doorShot.HandedOff(keyZone);
            keyHud.SetHeldKey(showKey || handedOff, HudIdentity(keyZone));
        }
        else if (keyPanel != null) keyPanel.SetActive(showKey);
        if (contextPanel != null) contextPanel.SetActive(play && hint.Length > 0);
        UpdateCaptions(play);
    }

    // Captions: one line per sound, with its direction from where the player looks, fading out at the end.
    readonly System.Text.StringBuilder captionLines = new System.Text.StringBuilder();
    void UpdateCaptions(bool play)
    {
        if (captionPanel == null) return;
        var lines = FrontRoomsCaptions.Lines;
        var show = play && FrontRoomsSettings.Captions && lines.Count > 0;
        if (captionPanel.activeSelf != show) captionPanel.SetActive(show);
        if (!show) return;
        var eye = rig != null ? rig.BaseEye : new Pose(cam.transform.position, cam.transform.rotation);
        var forward = eye.rotation * Vector3.forward;
        captionLines.Clear();
        for (var i = 0; i < lines.Count; i++)
        {
            if (i > 0) captionLines.Append('\n');
            var alpha = Mathf.RoundToInt(FrontRoomsCaptions.Alpha(lines[i]) * 255f);
            captionLines.Append("<color=#F4F1E8").Append(alpha.ToString("X2")).Append('>')
                .Append(FrontRoomsCaptions.Format(lines[i], eye.position, forward)).Append("</color>");
        }
        captionText.text = captionLines.ToString();
        // Phones: 220 pt of text (平面视觉 2026-10-29), so "[HEAVY STEPS · BEHIND]" (205.7 pt) stays one line and two captions
        // stay two, while the rect stays clear of X2's SLOW · UNHEARD word above a left thumb.
        var captionWidth = FrontRoomsHandheld.Active ? 234f : 920f;
        // Phones: 5 pt above and below the lines (平面视觉, 2026-10-22), so one line is 30 tall and two 50. The card
        // counts the lines as drawn: up to three captions (FrontRoomsCaptions.MaxLines), and a long one wraps in 220 pt.
        // Counting captions instead let three, or one wrapped, spill above and below the card.
        var captionHeight = 20f + 28f * lines.Count;
        if (FrontRoomsHandheld.Active)
        {
            var generator = captionText.cachedTextGeneratorForLayout;
            generator.Populate(captionText.text, captionText.GetGenerationSettings(new Vector2(captionWidth - 14f, 0f)));
            captionHeight = 10f + 20f * Mathf.Max(1, generator.lineCount);
        }
        ((RectTransform)captionPanel.transform).sizeDelta = new Vector2(captionWidth, captionHeight);
        captionText.rectTransform.sizeDelta = FrontRoomsHandheld.Active
            ? new Vector2(captionWidth - 14f, captionHeight - 4f)
            : new Vector2(880f, captionHeight + 60f);
    }

    // The zone the key panel's label was written for (written again only when it changes).
    GridCoord keyLabelZone;
    bool keyLabelSet;
    // The key panel's fade-in after a presented pickup (0..1; 1 whenever no key is being presented).
    float keyPanelFade = 1f;

    // HK1: a zone's key identity for the key HUD. DECISIONS #20: FrontRoomsKeyHud.StubIdentity (shape and colour from a
    // hash) until D1.6 brings the map's rule; then this line calls it instead. The map is not touched for it. The tag's
    // token is the run's floor (DECISIONS #28/#32/#33: C22's floor 1 = LEVEL 0 = "00", floor 2 = "B1", ...).
    FrontRoomsKeyHud.Identity HudIdentity(GridCoord zone) => FrontRoomsKeyHud.StubIdentity(zone, floor - 1);

    // KP-1 (M-5): the presented key hands over to the HUD key (DECISIONS #18: the first key's hand-off comes at its
    // EXAMINE hold end; call PickupFlight(..., clock, takeOffClock) then).
    void OnKeyHandoff(GridCoord zone, Vector2 viewport, float depth, float clock)
    {
        if (keyHud != null) keyHud.PickupFlight(HudIdentity(zone), viewport, depth, rig != null ? rig.BaseFov : cam.fieldOfView, clock);
    }

    /// <summary>
    /// What the key panel calls the key it shows (main's A panel, until KeyHud B): the stair key, on every floor
    /// and for every zone (C22 D13; 叙事设计's string, approved by Red 2026-10-22: "LEVEL 0 KEY" read as "LEVEL O
    /// KEY" in Bayon). The zone's floor token goes on KeyHud B's tag, which is 平面视觉's.
    /// </summary>
    const string KeyPanelLabel = "STAIR KEY";
    static string KeyLabel(ZoneInfo zone) => FrontRoomsLanguage.T(KeyPanelLabel);

    float RelayDistance() => relay == null || !relay.Released ? -1f : Vector2.Distance(playerPos, Flat(relay.Position));
    void ApplyGameplayHudAlpha()
    {
        // A takeover fades the HUD, but never the hint card (it carries captions).
        var hud = gameplayHudAlpha * (rig != null ? 1f - rig.HudFade : 1f);
        if (roomHudGroup != null) roomHudGroup.alpha = hud;
        if (threatHudGroup != null) threatHudGroup.alpha = hud;
        if (contextHudGroup != null) contextHudGroup.alpha = gameplayHudAlpha;
        if (captionHudGroup != null) captionHudGroup.alpha = gameplayHudAlpha;
        if (crosshairHudGroup != null) crosshairHudGroup.alpha = phase == Phase.Inspect ? gameplayHudAlpha : hud;   // the hint under it keeps its own alpha (C11)
        // The key panel also fades in after a presented pickup (keyPanelFade stays 1 on the instant path).
        // HK1 (DECISIONS #14): in mode (d) the key panel is exempt from the unlock shot's fade. (Under this seam the
        // map's own panel is off while the key HUD is attached, so this line matters only without it.)
        if (keyHudGroup != null) keyHudGroup.alpha = (keyHud != null && keyHud.PanelShotExempt ? gameplayHudAlpha : hud) * keyPanelFade;
        // HK M-3: the key HUD applies the takeover fade itself (DECISIONS #13 (b)/(c), #14 (d)).
        if (keyHud != null) keyHud.SetHudAlpha(gameplayHudAlpha, rig != null ? rig.HudFade : 0f);
    }
    void Flash(string message, float duration = 3f) { flash = message; flashTime = duration; }

    // A count and its unit, the unit in the singular for one ("1 KEY", "0 KEYS"; 叙事设计's CAUGHT line, C22 D13).
    static string Counted(int n, string one, string many) => FrontRoomsLanguage.F(n == 1 ? one : many, n.ToString());

    /// <summary>A camera shake (picture only) when something happens within <paramref name="range"/> metres of the player.</summary>
    void Jolt(Vector3 at, float range, float degrees, float decaySeconds)
    {
        if (rig == null || playerRoot == null || phase != Phase.Playing) return;
        if (Flat(at - playerRoot.position).sqrMagnitude > range * range) return;
        rig.AddTrauma(degrees, decaySeconds);
    }

    /// <summary>
    /// A forward pitch impulse (picture only, the audit's §3.3/§3.4 impulse rather than a shake) when something
    /// happens within <paramref name="range"/> metres of the player. The locked rattle uses it; normal opens keep
    /// Jolt until Red answers the door-shot landing question.
    /// </summary>
    void Kick(Vector3 at, float range, float degrees, float decaySeconds)
    {
        if (rig == null || playerRoot == null || phase != Phase.Playing) return;
        if (Flat(at - playerRoot.position).sqrMagnitude > range * range) return;
        rig.Kick(degrees, decaySeconds);
    }

    /// <summary>How much faster than the base the tier makes the chase: the run cycle and its step clock follow, so the feet keep up (1 at tier 1).</summary>
#if FRONTROOMS_RELAY_V2
    // V4b: the tier table's chase speed (the hunter's), not v1's scaled tuning.
    float ChasePace => (relay != null ? relay.ChaseSpeed : hunterTuning.chaseSpeed) / Mathf.Max(.01f, hunterTuning.chaseSpeed);
#else
    float ChasePace => relayTuning.chaseSpeed / Mathf.Max(.01f, hunterTuning.chaseSpeed);
#endif

    /// <summary>The Relay's tuning this frame: the Inspector's base, scaled by the tier's row. Base edits and tier changes both apply on the next frame and never compound.</summary>
    void UpdateRelayTuning()
    {
        relayTuning.CopyFrom(hunterTuning);
        TierRules.At(tier).ApplyTo(relayTuning);
    }

    /// <summary>
    /// Raise the run's tier to <paramref name="next"/> (never lower, capped at the table): chunks generated from now on
    /// take it too. Why: "cells" (C04, the unique-cell count) or "baseline" (a bot's fixed tier).
    /// </summary>
    void RaiseTier(int next, string why)
    {
#if UNITY_EDITOR
        // A baseline run keeps the tier it asked for (Baseline.cs: autoTierLock, off with -autopilotTierFree).
        if (autoBaseline && autoTierLock && why != "baseline") return;
#endif
        next = Mathf.Min(next, TierRules.MaxTier);
        if (next <= tier) return;
        tier = next;
        if (map != null) map.GenerationTier = tier;
        Event("tier", tier + " (" + why + ")");
        Log("TIER " + tier + " · " + why + " · " + tierCells.Count + " unique cells, " + zonesVisited.Count + " zones, " + elapsed.ToString("0.0", CultureInfo.InvariantCulture) + " s");
#if UNITY_EDITOR
        if (autopilot) autoTiers.Add(autoPlayClock.ToString("0.0", CultureInfo.InvariantCulture) + " s tier " + tier + " (" + why + ", " + tierCells.Count + " cells, " + zonesVisited.Count + " zones)");
#endif
        TierChanged?.Invoke(tier);
    }
    /// <summary>A cell of one floor's map (C04's count): the same GridCoord on two floors is two cells.</summary>
    readonly struct RunCell : IEquatable<RunCell>
    {
        public readonly int floor;
        public readonly GridCoord cell;
        public RunCell(int floor, GridCoord cell) { this.floor = floor; this.cell = cell; }
        public bool Equals(RunCell other) => floor == other.floor && cell == other.cell;
        public override bool Equals(object obj) => obj is RunCell other && Equals(other);
        public override int GetHashCode() => unchecked(cell.GetHashCode() * 31 + floor);
    }

    // One row of the events CSV (End writes it): run time, kind, detail, the Relay's distance, and the floor (C22: 1 is the title's).
    void Event(string kind, string detail) { events.Add(elapsed.ToString("0.000", CultureInfo.InvariantCulture) + "," + kind + ",\"" + detail.Replace("\"", "\"\"") + "\"," + RelayDistance().ToString("0.00", CultureInfo.InvariantCulture) + "," + floor + "," + EventsSession); }
    static int EventsSession
    {
        get
        {
            if (eventsSession > 0) return eventsSession;
            eventsSession = PlayerPrefs.GetInt(EventsSessionKey, 0) + 1;
            PlayerPrefs.SetInt(EventsSessionKey, eventsSession);
            PlayerPrefs.Save();
            return eventsSession;
        }
    }
    void SaveEvents()
    {
        if (events.Count == 0) return;
        try
        {
            if (eventsFile == null)
            {
                var dir = Application.persistentDataPath; Directory.CreateDirectory(dir);
                eventsFile = Path.Combine(dir, "events-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv");
            }
            File.WriteAllText(eventsFile, "time_s,event,detail,relay_m,floor,session\n" + string.Join("\n", events));
        }
        catch (Exception e) { Debug.LogWarning("[FrontRooms3D] events CSV not saved: " + e.Message); }
    }
    void OnArrowsTurned(string why) => Event("arrows", why);
    void OnApplicationQuit() => SaveEvents();
    void OnDestroy()
    {
        SaveEvents();
        FrontRoomsWayCueDriver.ArrowsTurned -= OnArrowsTurned;
#if UNITY_EDITOR
        AutoBaseRestoreTime();
        FrontRoomsCameraRig.ShotStarted -= AutoShotStarted;
#endif
        if (mobileTouch != null)
        {
            mobileTouch.SettingsRowRequested -= HandleMobileSettingsRow;
            mobileTouch.LookTapped -= HandleMobileLookTap;
        }
        if (mobileBackBridge != null)
            FrontRoomsMobileBackBridge.BackPressed -= HandleMobileBack;
        PlayerStamina01 = 1f;
        PlayerWinded = PlayerSprinting = false;
        WarnBursting = false;
        PlayerGait = FrontRoomsGait.Still;
        // C12: a close-up torn down (R restart, Play stopped) gives the world back.
        FrontRoomsInspect.AbortRequested -= OnInspectAbortRequested;
        if (timeFrozenByInspect) Time.timeScale = 1f;
        timeFrozenByInspect = false;
        if (InspectingNow) { InspectingNow = false; Inspecting?.Invoke(false); }
        FrontRoomsInspect.Abort();
        if (phase == Phase.Paused) Paused?.Invoke(false);
        if (map != null && map.KeyTurnOwner == doorShot) map.KeyTurnOwner = null;
        if (map != null && map.KeyPresenter != null && map.KeyPresenter.Target == doorShot) map.KeyPresenter = null;
        FrontRoomsDoorShot.KeyHandoff -= OnKeyHandoff;
        if (map != null) MapRunEnded?.Invoke();
        OnDestroyMobile();
    }

    void End()
    {
        // A43: the world runs under a live close-up, so the Relay can catch the player in one; it ends at once (C12b below).
        if (phase != Phase.Playing && !(phase == Phase.Inspect && inspectLive)) return;
        if (InspectingNow) { InspectingNow = false; inspectLive = false; Inspecting?.Invoke(false); }
        // Caught > cancel > shot: a key turn resolves now (locked before its commit, unlocked and shut after), the camera stays.
        doorShot?.Abort();
        // C12b: caught during a close-up's return: nothing freezes mid-blend (other shots behave as before).
        if (FrontRoomsInspect.Active) { FrontRoomsInspect.Abort(); rig?.ResetLayers(); }
        WarnBursting = false;
        SetPhase(Phase.Caught); FrontRoomsMobileInteractionEvents.Caught(); Sound(caughtClip, playerPos); Event("outcome", "caught");
        SaveEvents();
        Log(phase + " · " + elapsed.ToString("0.0") + " s");
    }
    static void Log(string message) => Debug.Log("[FrontRooms3D] " + message);

#if UNITY_EDITOR
    // ---------- Autopilot: a scripted play-through for batch verification ----------
    // FrontRoomsMainScenePlaytest sets the session flag and enters Play. The
    // autopilot presses Space, takes over in the stream room, walks out
    // through its door into the maze, walks the maze along breadth-first
    // routes (opening doors on the way), sprints once, captures frames to
    // Verification/main-autopilot, writes report.json and a done file. It
    // never runs in a build or in a normal Play session.
    public const string AutopilotKey = "FrontRooms.Autopilot";
    /// <summary>Session key set by FrontRooms/Map/Play with keys on: the next runs' maps need keys (the autopilot ignores it).</summary>
    public const string KeysOnSessionKey = "FrontRooms.Map.KeysOn";
    /// <summary>Session key set by FrontRooms/Map/Show placeholder key: the next runs present the cube key on pickup (the autopilot ignores it).</summary>
    public const string PlaceholderKeySessionKey = "FrontRooms.Map.PlaceholderKey";
    /// <summary>Session key for a fixed maze seed on the autopilot (0 = the profile's).</summary>
    public const string AutopilotSeedKey = "FrontRooms.Autopilot.Seed";
    int autopilotSeed, autoSkipFrames, autoGhostsSeen;
    float autoWorstFrameMs, autoRelayTickMs, autoRelayTickAt, autoHandoffWorstMs;
    // When (play seconds) the start door opened, the player left the stream rooms, the door shut behind them, the rooms were removed.
    float autoStartDoorAt = -1f, autoLeftStartAt = -1f, autoStartShutAt = -1f, autoStreamRemovedAt = -1f, autoSettledAt = -1f;
    bool autoDoorShot, autoLookBackShot;
    // When the autopilot presses Space (title seconds). -autopilotSpaceAt 1.0 starts with the
    // first stream door still shut 4.85 m ahead, so the door is reached while the map is building.
    float autoSpaceAt = 1.8f;
    readonly List<(float ms, string text)> autoHandoffFrames = new List<(float, string)>();
    const float AutoHandoffSeconds = 4f;
    readonly List<string> autoGhostLog = new List<string>();
    readonly List<float> autoFrameMs = new List<float>();
    readonly List<string> autoSpikes = new List<string>();
    public const string AutopilotDoneFile = "Temp/frontrooms-autopilot-done.txt";
    const float AutopilotPlaySeconds = 75f;
    static readonly GridCoord[] AutoSteps = { new GridCoord(1, 0), new GridCoord(-1, 0), new GridCoord(0, 1), new GridCoord(0, -1) };
    bool autopilot, autoFinished, autoRelayShot, autoOfficeShot;
    float autoOfficeCheckAt;
    float autoClock, autoPlayClock, autoNextShot, autoStuckClock, autoDistance, autoReleaseTime = -1f, autoMinRelay = float.MaxValue;
    int autoShots, autoFrames, autoErrors, autoRoutes, autoDoorsOpened, autoMaxChunks;
    Vector3 autoLastPosition;
    Vector2 autoLastFlat;
    string autoOutDir;
    readonly List<GridCoord> autoRoute = new List<GridCoord>();
    int autoRouteIndex;
    readonly HashSet<GridCoord> autoCells = new HashSet<GridCoord>();
    readonly List<string> autoErrorLog = new List<string>();
    readonly List<string> autoStates = new List<string>();
    readonly List<string> autoTiers = new List<string>();
    // The cells visited on floors already left (C22: the map is reseeded in place, so a new floor's cells reuse the
    // old GridCoords): cellsVisited is this plus the current floor's set.
    int autoCellsTotal;

    [Serializable]
    sealed class AutopilotReport
    {
        public string verdict;
        public int seed;
        public float playSeconds;
        public float distanceWalked;
        public int cellsVisited;
        public int zonesVisited;
        public int tier;
        // C04 (V1): the unique cells the tier counts (out of the start rooms, stairwells and station; per floor).
        public int tierCells;
        public List<string> tierLog = new List<string>();
        public int maxChunksBuilt;
        public int routes;
        public int doorsOpened;
        public int keysTaken;
        public float relayReleasedAt = -1f;
        public float closestRelayMetres;
        public string relayFinalState;
        public int relayDoorsBroken;
#if FRONTROOMS_RELAY_V2
        // V3b: the door outcomes (OPENed, pushed through Ajar, WALLed) and the on-map device calls (re-dispatches, handoffs, sweeps).
        public int relayDoorsOpened;
        public int relayDoorsPushed;
        public int relayDoorsWalled;
        public int relayRedispatches;
        public int relayHandoffs;
        public int relaySweeps;
#endif
        public int relayRelays;
        public int relayGhosts;
        public bool caught;
        public float averageFps;
        public float worstFrameMs;
        public float p95FrameMs;
        public float p99FrameMs;
        public float relayTickMaxMs;
        public float relayTickMaxAt;
        // The handoff from the title: the worst frame in the first 4 s after Space (nothing is hidden any more),
        // and when the start door opened, the player left the stream rooms, the door shut, the rooms were removed (-1: never).
        public float handoffWorstFrameMs;
        public List<string> handoffSlowestFrames = new List<string>();
        public float spacePressedAt;
        public float mapReadyAt = -1f;
        public float startDoorHeldSeconds;
        public float startDoorOpenedAt = -1f;
        public float leftStartRoomsAt = -1f;
        public float startDoorShutAt = -1f;
        public float streamRemovedAt = -1f;
        public List<string> frameSpikes = new List<string>();
        public int officeRoomsDressed;
        public int errors;
        public List<string> errorLog = new List<string>();
        public List<string> relayStates = new List<string>();
        public List<string> relayGhostLog = new List<string>();
        public List<string> frames = new List<string>();
        // The glass scenario (-autopilotGlass): what it checked, ok or FAIL.
        public bool glassScenario;
        public List<string> glassChecks = new List<string>();
        // The door shots: Unlock shots started (none with keys off: shipped play never shows one), and what was checked, ok or FAIL.
        public int unlockShotsStarted;
        public List<string> doorChecks = new List<string>();
        // Key pickups (door_shots_plan.md §5.4): how many, the worst KeyTaken lag after KeyPickupStarted (s; −1: no
        // pickup), and how many were presented. Without a model (shipped play) none is, and every lag is 0, same frame.
        public int keyPickups;
        public float keyTakenLagMax = -1f;
        public int keyPresentations;
        // HUD panels laid out outside the screen (any one fails the run).
        public List<string> hudOffscreen = new List<string>();
        // C22: the floor the run ended on (1 = the title's), and the EXIT scenario (-autopilotExit): what it checked
        // (ok or FAIL), one row per change of floor (the EXIT, the route, the seal, the change's cost and its frame,
        // the north door's hold, the shut, the Relay's release), and the worst change and the longest hold.
        public int floor = 1;
        public bool exitScenario;
        public List<string> exitChecks = new List<string>();
        public List<string> floorRows = new List<string>();
        public float floorChangeWorstMs;
        public float floorChangeFrameWorstMs;
        public float floorHoldMaxSeconds;
        // A26: floor 1's station (empty without one): the plan, the start door's hold and shut, the EXIT sign in view,
        // the TV and the placard in the opening's frustum (AutopilotStation), each ok or FAIL.
        public List<string> stationChecks = new List<string>();
    }

    readonly List<string> autoFrameNames = new List<string>();

    // ---------- Key pickups (§5.4): KeyTaken after each KeyPickupStarted ----------
    // On the instant path (no model, the placeholder off: shipped play) KeyTaken comes in the same frame as
    // KeyPickupStarted; a presented key's comes at its arrival, KeyPickup.Arrive later and never after it plus
    // a frame. A pickup never announced, or a KeyTaken with no pickup, breaks the rule too. Any break fails the run.
    readonly Dictionary<GridCoord, int> autoKeyStartFrame = new Dictionary<GridCoord, int>();
    readonly Dictionary<GridCoord, float> autoKeyStartAt = new Dictionary<GridCoord, float>();
    readonly HashSet<GridCoord> autoKeyAnnounced = new HashSet<GridCoord>();
    readonly List<string> autoKeyBreaks = new List<string>();
    int autoKeyPickups, autoKeyPresentations;
    float autoKeyLagMax = -1f, autoKeyLastLag = -1f;
    bool autoKeyLastPresented;

    void AutoKeyPickupStarted(GridCoord zone, Vector3 at)
    {
        autoKeyPickups++;
        autoKeyStartFrame[zone] = Time.frameCount;
        autoKeyStartAt[zone] = elapsed;
    }

    void AutoKeyTaken(GridCoord zone)
    {
        if (!autoKeyStartFrame.TryGetValue(zone, out var frame))
        {
            autoKeyBreaks.Add("KeyTaken " + zone + " with no KeyPickupStarted");
            return;
        }
        autoKeyAnnounced.Add(zone);
        var presented = doorShot != null && doorShot.Presenting(zone);
        var lag = elapsed - autoKeyStartAt[zone];
        autoKeyLastLag = lag;
        autoKeyLastPresented = presented;
        autoKeyLagMax = Mathf.Max(autoKeyLagMax, lag);
        if (presented) autoKeyPresentations++;
        if (!presented && frame != Time.frameCount)
            autoKeyBreaks.Add("key " + zone + ": not presented, but KeyTaken " + (Time.frameCount - frame) + " frames (" + lag.ToString("0.000") + " s) after KeyPickupStarted");
        if (presented && lag > FrontRoomsShotTimings.KeyPickup.Arrive + .1f + 1e-3f)
            autoKeyBreaks.Add("key " + zone + ": presented, KeyTaken " + lag.ToString("0.000") + " s after KeyPickupStarted (arrival " + FrontRoomsShotTimings.KeyPickup.Arrive + ")");
        Log("AUTOPILOT KEY " + zone + (presented ? " presented" : " instant") + ", KeyTaken " + (Time.frameCount - frame) + " frames / " + lag.ToString("0.000") + " s after KeyPickupStarted");
    }

    // The key rule's report line, at the finish: a pickup still being presented is not yet due.
    string AutoKeyRuleLine()
    {
        foreach (var zone in autoKeyStartFrame.Keys)
            if (!autoKeyAnnounced.Contains(zone) && (doorShot == null || !doorShot.Presenting(zone)))
                autoKeyBreaks.Add("key " + zone + ": KeyPickupStarted, never KeyTaken");
        var ok = autoKeyBreaks.Count == 0;
        return (ok ? "ok   " : "FAIL ") + "keys: " + autoKeyPickups + " pickups, " + autoKeyPresentations + " presented; every one not presented took the key in its contact frame"
            + " (KeyTaken lag max " + (autoKeyLagMax < 0f ? "none" : autoKeyLagMax.ToString("0.000") + " s") + ")" + (ok ? "" : ": " + string.Join("; ", autoKeyBreaks));
    }

    void AutopilotStart()
    {
        autopilot = UnityEditor.SessionState.GetBool(AutopilotKey, false);
        autopilotSeed = UnityEditor.SessionState.GetInt(AutopilotSeedKey, 0);
        if (!autopilot) return;
        var args = Environment.GetCommandLineArgs();
        AutoBaseStart(args);
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "-autopilotSpaceAt" && float.TryParse(args[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var at)) autoSpaceAt = Mathf.Max(.2f, at);
        autoGlassWanted = Array.IndexOf(args, "-autopilotGlass") >= 0;
        autoDoorWanted = Array.IndexOf(args, "-autopilotDoors") >= 0;
        // C32's W fix (10-30), tools: -autopilotFirstSendNoW (any letter case) runs the first send's ladder without the W
        // rule, as before the fix, for a before/after reference run (lean: play:<seed>:...+autopilotFirstSendNoW).
        autoFirstSendNoW = false;
        foreach (var arg in args) autoFirstSendNoW |= string.Equals(arg, "-autopilotFirstSendNoW", StringComparison.OrdinalIgnoreCase);
        if (autoFirstSendNoW) Log("AUTOPILOT FIRST SEND W RULE OFF · rungs 1-2 as before C32's W fix (a reference run, tools only)");
        // -autopilotBotAfterExit (any letter case; 关卡设计调研, 10-30): a baseline whose floor 1 the EXIT walker plays
        // (AutoBotAfterExit*). Only with -autopilotBot <mode>; without one it is ignored and the run is as before.
        autoBotAfterExit = false;
        foreach (var arg in args) autoBotAfterExit |= string.Equals(arg, "-autopilotBotAfterExit", StringComparison.OrdinalIgnoreCase);
        if (autoBotAfterExit && !autoBaseline)
        {
            autoBotAfterExit = false;
            Log("AUTOPILOT BOT AFTER EXIT ignored: it needs -autopilotBot <mode>");
        }
        // C22: never in a Relay baseline (its runs keep the EXIT closed, so they stay comparable), but for the walk of
        // a bot-after-exit run's floor 1.
        autoExitWanted = (Array.IndexOf(args, "-autopilotExit") >= 0 && !autoBaseline) || autoBotAfterExit;
        // Nor is one planned for a baseline (PlanExit), unless it asks for -autopilotExit: then its map has the EXIT,
        // still never sealed (ExitSealAllowed), for baselines to be recorded on the C22 map. A bot-after-exit run's
        // floors plan their EXIT as in play.
        autoBaselineExit = autoBaseline && (Array.IndexOf(args, "-autopilotExit") >= 0 || autoBotAfterExit);
        if (autoBotAfterExit) Log("AUTOPILOT BOT AFTER EXIT on · floor 1 by the EXIT walker (the Relay never held), then from B1's start-door shut the bot "
            + AutoBotNames[(int)autoBot] + " for " + AutoF(autoBotBudget / 60f, "0.##") + " min from the run's first send");
        else if (autoExitWanted) Log("AUTOPILOT EXIT scenario on · " + AutoExitFloors + " floors down");
        // The onboarding (Red 2026-10-30): FrontRoomsMainScenePlaytest sets OnboardOffForTools for -autopilotOnboardOff.
        if (OnboardOffForTools) Log("AUTOPILOT ONBOARDING OFF · floor 1's gate is M7's old one (the key and " + FrontRoomsMapHunter.StationLeaveCells + " path cells out, or "
            + AutoF(FrontRoomsMapHunter.OnboardOffSendSeconds, "0") + " s)");
        FrontRoomsCameraRig.ShotStarted += AutoShotStarted;
        autoOutDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "main-autopilot");
        Directory.CreateDirectory(autoOutDir);
        foreach (var old in Directory.GetFiles(autoOutDir, "*.png")) File.Delete(old);
        Application.logMessageReceived += AutopilotLog;
        Log("AUTOPILOT on · frames to " + autoOutDir);
    }

    // Every Unlock shot the run starts (keys off: there must be none).
    int autoUnlockShots;
    void AutoShotStarted(ShotKind kind, Vector3 focus)
    {
        if (this == null) { FrontRoomsCameraRig.ShotStarted -= AutoShotStarted; return; }
        if (kind == ShotKind.Unlock) autoUnlockShots++;
        if (kind == ShotKind.Unlock && autoExitWanted && !autoExitDone && autoExitStep == ExitUnlock) autoStationUnlockShots++;
    }

    void AutopilotLog(string condition, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        // A fresh batch clone can emit Unity's own empty SearchDatabase index exception before Play.
        // It is editor infrastructure, not a level or gameplay failure, so keep it out of the baseline verdict.
        if (stack != null && stack.Contains("UnityEditor.Search.SearchDatabase")) return;
        autoErrors++;
        if (autoErrorLog.Count < 20) autoErrorLog.Add(type + ": " + condition);
    }

    void AutopilotTick(float dt)
    {
        if (autoFinished) return;
        autoClock += dt;
        autoFrames++;
        // C32's W fix: -autopilotFirstSendNoW's reference run (the hunter of every floor and run).
        if (relay != null) relay.FirstSendNoWForTools = autoFirstSendNoW;
        if (autoBaseline) AutoBaseFrame();
        // Streaming and dressing hitches, from the frame Space is pressed: the
        // map now builds while the player is in the stream room, in view.
        // Frames right after an autopilot capture carry its PNG encode, not the game's cost.
        if (autoSkipFrames > 0) autoSkipFrames--;
        else if (mapPlay)
        {
            var ms = Time.unscaledDeltaTime * 1000f;
            autoWorstFrameMs = Mathf.Max(autoWorstFrameMs, ms);
            if (autoPlayClock < AutoHandoffSeconds)
            {
                autoHandoffWorstMs = Mathf.Max(autoHandoffWorstMs, ms);
                // The six slowest handoff frames and what the map did in them.
                autoHandoffFrames.Add((ms, autoPlayClock.ToString("0.00", CultureInfo.InvariantCulture) + " s: " + ms.ToString("0.0") + " ms" + (map.WorkInFrame(Time.frameCount - 1).Length > 0 ? ", map: " + map.WorkInFrame(Time.frameCount - 1) : "")));
                autoHandoffFrames.Sort((x, y) => y.ms.CompareTo(x.ms));
                if (autoHandoffFrames.Count > 6) autoHandoffFrames.RemoveAt(6);
            }
            autoFrameMs.Add(ms);
            // Spikes with their time, to tell streaming or dressing hitches from one-off editor shader compiles.
            if (ms > 50f && autoSpikes.Count < 12)
                autoSpikes.Add(autoPlayClock.ToString("0.0", CultureInfo.InvariantCulture) + " s: " + ms.ToString("0") + " ms, zone " + ZoneName(map.ZoneOf(map.CellOf(playerRoot.position))) + ", chunks " + map.BuiltChunkCount
                    + (map.WorkInFrame(Time.frameCount - 1).Length > 0 ? ", map: " + map.WorkInFrame(Time.frameCount - 1) : ""));
        }
        if (relay != null && relay.Ghosts != autoGhostsSeen)
        {
            autoGhostsSeen = relay.Ghosts;
            if (autoGhostLog.Count < 20)
                autoGhostLog.Add(autoPlayClock.ToString("0.0", CultureInfo.InvariantCulture) + " s " + relay.State + " at " + relay.Position.ToString("F2") + " cell " + map.CellOf(relay.Position) + " · " + relay.DebugSteering + " · " + relay.DebugBlocker);
        }
        if (!mapPlay)
        {
            if (phase == Phase.Title)
            {
                if (autoShots == 0 && autoClock > Mathf.Min(1.2f, autoSpaceAt - .3f)) AutopilotCapture("00_title");
                if (autoClock > autoSpaceAt) RequestTitleStart();
            }
            if (autoClock > 40f) AutopilotFinish("never reached the maze");
            return;
        }
        autoPlayClock += dt;
        if (autoBaseline) AutoBaseTick(dt);
        if (playerRoot != null)
        {
            var flat = Flat(playerRoot.position);
            if (autoPlayClock > dt) autoDistance += Vector2.Distance(flat, autoLastFlat);
            autoLastFlat = flat;
            if (!inStartRooms) autoCells.Add(map.CellOf(playerRoot.position));
        }
        if (autoStartDoorAt < 0f && startDoorOpened) autoStartDoorAt = autoPlayClock;
        if (autoLeftStartAt < 0f && leftStartRooms) autoLeftStartAt = autoPlayClock;
        if (autoStartShutAt < 0f && streamFade >= 0f) autoStartShutAt = autoPlayClock;
        if (autoStreamRemovedAt < 0f && roomStream == null) autoStreamRemovedAt = autoPlayClock;
        // A26: floor 1's station, as the walk meets it.
        if (stationPlan != null && floor == 1) AutopilotStation();
        // The seam, from both sides: the map through the open stream door, and the shut door from the maze.
        if (!autoDoorShot && autoStartDoorAt >= 0f && autoPlayClock - autoStartDoorAt > .6f && inStartRooms)
        {
            autoDoorShot = true;
            AutopilotCapture((autoShots < 10 ? "0" : "") + autoShots + "_start_door");
        }
        if (!autoLookBackShot && autoStartShutAt >= 0f && autoPlayClock - autoStartShutAt > .3f)
        {
            // From the door cell, eye height, looking back at the shut door and either side of it.
            autoLookBackShot = true;
            var from = startDoorPoint + Vector3.forward * 2.7f + Vector3.up * EyeHeight;
            foreach (var side in new[] { 0f, -2.4f, 2.4f })
                AutopilotLookFrom(from, startDoorPoint + new Vector3(side, 1.3f, 0f), "start_door_from_maze" + (side < 0f ? "_left" : side > 0f ? "_right" : ""));
        }
        if (autoSettledAt < 0f && map.Settled) autoSettledAt = autoPlayClock;
        autoMaxChunks = Mathf.Max(autoMaxChunks, map.BuiltChunkCount);
        if (relay != null)
        {
            if (autoStates.Count == 0 || autoStates[autoStates.Count - 1].EndsWith(relay.State.ToString()) == false)
                autoStates.Add(autoPlayClock.ToString("0.0", CultureInfo.InvariantCulture) + " s " + relay.State);
            if (relay.Released)
            {
                if (autoReleaseTime < 0f) autoReleaseTime = autoPlayClock;
                autoMinRelay = Mathf.Min(autoMinRelay, RelayDistance());
                if (!autoRelayShot && autoPlayClock - autoReleaseTime > 1.5f)
                {
                    autoRelayShot = true;
                    AutopilotCaptureRelay();
                }
            }
#if FRONTROOMS_RELAY_V2
            // V3a: the debug Summon AutoSummonSeconds after Armed, and PASS = on the map at least once.
            AutoRelayV2Tick();
#endif
        }
        // The first time the walk is in a dressed Office room: look round it (four views), for the furniture.
        if (!autoOfficeShot && playerRoot != null && autoPlayClock >= autoOfficeCheckAt && map.ZoneOf(map.CellOf(playerRoot.position)).theme == ZoneTheme.Office)
        {
            autoOfficeCheckAt = autoPlayClock + .5f;
            if (OfficeDressingNear(playerRoot.position, 4.5f))
            {
                autoOfficeShot = true;
                AutopilotLookAround("office");
            }
        }
        if (autoGlassWanted && !autoGlassDone) AutopilotGlass(dt);
        if (autoDoorWanted && !autoDoorDone) AutopilotDoors(dt);
        if (autoExitWanted && !autoExitDone) AutopilotExit(dt);
        if (autoBotAfterExit) AutoBotAfterExitTick();
        if (!autoHudChecked && autoPlayClock > 1f) AutopilotHudCheck();
        if (autoPlayClock > .6f && autoPlayClock >= autoNextShot && !autoGlassActive && !autoDoorActive && !AutoExitQuiet)
        {
            autoNextShot = autoPlayClock + 9f;
            AutopilotCapture((autoShots < 10 ? "0" : "") + autoShots + "_play_" + Mathf.RoundToInt(autoPlayClock) + "s");
        }
        if (phase == Phase.Caught) AutopilotFinish("caught");
        else if (autoExitWanted && autoExitDone && !autoBotAfterExit) AutopilotFinish("exits");
        // A bot-after-exit run follows the bot (caught, or its budget below); it ends early only when the walk failed.
        else if (autoBotAfterExit && autoExitDone && !autoBotHandedOff) AutopilotFinish("the exit walker failed (floor " + floor + ", step " + AutoExitStepNames[autoExitStep] + ")");
        else if (autoPlayClock >= (autoBaseline ? autoBotSeconds : autoExitWanted ? AutoExitPlayLimit : AutopilotPlaySeconds)) AutopilotFinish("time");
    }

    /// <summary>Walk the current route; plan a new far route when it ends or the walk stalls.</summary>
    void AutopilotSteer(float dt, out Vector2 local, out bool sprint)
    {
        local = Vector2.zero;
        sprint = false;
        // The glass scenario stands still at the pane, then walks into the broken frame to climb.
        if (autoGlassActive)
        {
            if (autoGlassStep == 4) local = new Vector2(0f, 1f);
            return;
        }
        // The door scenario stands where it put the player.
        if (autoDoorActive) return;
        // M3: the floor key's take walks to its stand and faces the key (AutopilotStation presses E).
        if (autoStationTake >= 1 && autoStationTake <= 3)
        {
            AutoStationTakeSteer(dt, out local);
            return;
        }
        // The EXIT scenario walks on its own from the first step past the stream rooms' door (C22).
        if (autoExitWanted && !autoExitDone && AutoExitSteer(dt, out local, out sprint)) return;
        // Out of the stream rooms first: down the centreline, through the door
        // and on past its open leaves into the cell ahead (always open, MapRootFor).
        // The edge-runner needs to clear the door cell before choosing a lateral edge route;
        // seed 7 otherwise starts the route against the south wall and never releases the Relay.
        var startExitDepth = autoBaseline && autoBot == AutoBot.EdgeRunner ? 3.8f : 2.2f;
        if (inStartRooms || (map.CellOf(playerRoot.position) == startDoorCell && playerRoot.position.z < startDoorPoint.z + startExitDepth))
        {
            var aim = map.CellCenter(startDoorCell) + Vector3.forward * 1.2f - playerRoot.position;
            aim.y = 0f;
            var aimYaw = Mathf.Atan2(aim.x, aim.z) * Mathf.Rad2Deg;
            yaw = Mathf.MoveTowardsAngle(yaw, aimYaw, 300f * dt);
            pitch = Mathf.MoveTowards(pitch, 0f, 60f * dt);
            local = new Vector2(0f, 1f);
            if (autoBaseline && Flat(startDoorPoint - playerRoot.position).magnitude < AutoStartStandOff && !AutoBotMayLeave()) local = Vector2.zero;
            return;
        }
        var here = map.CellOf(playerRoot.position);
        autoStuckClock += dt;
        if (autoStuckClock > 2.5f)
        {
            if ((playerRoot.position - autoLastPosition).magnitude < .4f) autoRouteIndex = autoRoute.Count;
            autoLastPosition = playerRoot.position;
            autoStuckClock = 0f;
        }
        if (autoBaseline)
        {
            AutoBotSteer(dt, here, out local, out sprint);
            return;
        }
        if (autoRouteIndex >= autoRoute.Count && !AutoStationKeyRoute(here)) AutopilotPlan(here);
        if (autoRouteIndex >= autoRoute.Count) return;
        var next = autoRoute[autoRouteIndex];
        if (here == next)
        {
            autoRouteIndex++;
            return;
        }
        if (Mathf.Abs(here.x - next.x) + Mathf.Abs(here.y - next.y) != 1)
        {
            autoRouteIndex = autoRoute.Count;
            return;
        }
        var passage = map.PassageBetween(here, next);
        if (passage == FrontRoomsMapWorld.Passage.ClosedDoor)
        {
            if (map.TryOpenDoor(here, next)) autoDoorsOpened++;
            // The leaf is on its way (after a pull, the game has stepped the walker clear): wait for it, facing the door.
            var opening = map.DoorBetween(here, next);
            if (opening != null && opening.open)
            {
                var face = map.CrossingPoint(here, next) - playerRoot.position;
                face.y = 0f;
                yaw = Mathf.MoveTowardsAngle(yaw, Mathf.Atan2(face.x, face.z) * Mathf.Rad2Deg, 300f * dt);
                local = Vector2.zero;
                return;
            }
        }
        else if (passage != FrontRoomsMapWorld.Passage.Open)
        {
            autoRouteIndex = autoRoute.Count;
            return;
        }
        var target = map.CrossingPoint(here, next) + (map.CellCenter(next) - map.CellCenter(here)).normalized * .45f;
        var to = target - playerRoot.position;
        to.y = 0f;
        var desiredYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
        yaw = Mathf.MoveTowardsAngle(yaw, desiredYaw, 300f * dt);
        pitch = Mathf.MoveTowards(pitch, 0f, 60f * dt);
        local = new Vector2(0f, Mathf.Abs(Mathf.DeltaAngle(yaw, desiredYaw)) < 45f ? 1f : .15f);
        sprint = autoPlayClock > 16f && autoPlayClock < 22f;
    }

    /// <summary>
    /// A26 (the station on, the plain walk): the floor key first, as a player who reads the room takes it. Until the take
    /// starts, a new route heads up the station's centre aisle to E1's inside (the walk the EXIT run takes past the key;
    /// the take spot's own cell holds the desk); AutopilotStation starts the take within AutoStationKeyReach of the
    /// KeySpot. Without it the 75 s walk never took the key (M3's rows need the take; since the onboarding, Red 2026-10-30,
    /// the key no longer gates the send, except under -autopilotOnboardOff). False (plan as before) off floor 1, with no station, once the take has
    /// begun, for the bots, at E1's inside already, or with it out of reach.
    /// </summary>
    bool AutoStationKeyRoute(GridCoord here)
    {
        if (autoBaseline || floor != 1 || stationPlan == null || autoStationTake != 0 || map.StationKeyIsTaken) return false;
        var stand = stationPlan.layout.e1In;
        if (stand == here) return false;
        AutoBfs(here, 90, null);
        if (!autoDepth.ContainsKey(stand)) return false;
        AutoRouteTo(here, stand);
        return autoRouteIndex < autoRoute.Count;
    }

    /// <summary>Breadth-first route to a far cell (depth 18–30) through open edges and doors.</summary>
    void AutopilotPlan(GridCoord from)
    {
        autoRoute.Clear();
        autoRouteIndex = 0;
        var cameFrom = new Dictionary<GridCoord, GridCoord>();
        var depth = new Dictionary<GridCoord, int> { [from] = 0 };
        var queue = new Queue<GridCoord>();
        queue.Enqueue(from);
        var far = new List<GridCoord>();
        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            var d = depth[cell];
            if (d >= 18) far.Add(cell);
            if (d >= 30) continue;
            foreach (var step in AutoSteps)
            {
                var n = cell + step;
                if (depth.ContainsKey(n) || !map.IsBuilt(n)) continue;
                var p = map.PassageBetween(cell, n);
                if (p == FrontRoomsMapWorld.Passage.Wall || p == FrontRoomsMapWorld.Passage.Glass) continue;
                depth[n] = d + 1;
                cameFrom[n] = cell;
                queue.Enqueue(n);
            }
        }
        if (far.Count == 0)
            foreach (var pair in depth) if (pair.Value > 0) far.Add(pair.Key);
        if (far.Count == 0) return;
        var goal = far[UnityEngine.Random.Range(0, far.Count)];
        for (var c = goal; c != from; c = cameFrom[c]) autoRoute.Add(c);
        autoRoute.Reverse();
        autoRoutes++;
    }

    void AutopilotCapture(string name)
    {
        // Baseline runs take no pictures: a capture renders twice and skips frames, which would skew their timings.
        if (cam == null || autoBaseline) return;
        AutopilotRender(cam, name);
    }

    /// <summary>One view from a point towards another, with the player's lens.</summary>
    void AutopilotLookFrom(Vector3 from, Vector3 target, string label)
    {
        if (cam == null || autoBaseline) return;
        var go = new GameObject("AUTOPILOT / look-at camera");
        var shot = go.AddComponent<Camera>();
        shot.CopyFrom(cam);
        // A capture mid-shot keeps the player's own field of view.
        if (rig != null) shot.fieldOfView = rig.BaseFov;
        shot.enabled = false;
        go.transform.position = from;
        go.transform.LookAt(target);
        AutopilotRender(shot, (autoShots < 10 ? "0" : "") + autoShots + "_" + label);
        Destroy(go);
    }

    bool OfficeDressingNear(Vector3 point, float radius)
    {
        // Dressing roots sit directly under the chunk roots, which sit directly under the map.
        foreach (Transform chunk in map.transform)
            foreach (Transform t in chunk)
                if (t.name == "office dressing")
                    foreach (var r in t.GetComponentsInChildren<Renderer>())
                        if ((r.bounds.center - point).sqrMagnitude < radius * radius) return true;
        return false;
    }

    /// <summary>Four views from the player's eye, a quarter turn apart, level.</summary>
    void AutopilotLookAround(string label)
    {
        if (cam == null || autoBaseline) return;
        var go = new GameObject("AUTOPILOT / look-around camera");
        var shot = go.AddComponent<Camera>();
        shot.CopyFrom(cam);
        // A capture mid-shot keeps the player's own field of view.
        if (rig != null) shot.fieldOfView = rig.BaseFov;
        shot.enabled = false;
        go.transform.position = rig != null ? rig.BaseEye.position : cam.transform.position;
        for (var i = 0; i < 4; i++)
        {
            go.transform.rotation = Quaternion.Euler(6f, yaw + i * 90f, 0f);
            AutopilotRender(shot, (autoShots < 10 ? "0" : "") + autoShots + "_" + label + "_" + i * 90);
        }
        Destroy(go);
    }

    /// <summary>A third-person look at the Relay from behind the player's side of it, to check the rig is placed and animating.</summary>
    void AutopilotCaptureRelay()
    {
        if (relay == null || cam == null || autoBaseline) return;
        var go = new GameObject("AUTOPILOT / relay camera");
        var shot = go.AddComponent<Camera>();
        shot.CopyFrom(cam);
        // A capture mid-shot keeps the player's own field of view.
        if (rig != null) shot.fieldOfView = rig.BaseFov;
        shot.enabled = false;
        // Stand in the most open of eight directions around the Relay, so the
        // shot is not taken from inside a wall.
        var target = relay.Position + Vector3.up * 1.3f;
        var bestDir = Vector3.back;
        var bestClear = -1f;
        for (var i = 0; i < 8; i++)
        {
            var dir = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
            var clear = Physics.Raycast(target, dir, out var hit, 3.2f, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(hunter) ? hit.distance : 3.2f;
            if (clear > bestClear) { bestClear = clear; bestDir = dir; }
        }
        go.transform.position = target + bestDir * Mathf.Max(.6f, bestClear - .35f) + Vector3.up * .3f;
        go.transform.LookAt(target);
        AutopilotRender(shot, (autoShots < 10 ? "0" : "") + autoShots + "_relay");
        Destroy(go);
    }

    void AutopilotRender(Camera source, string name)
    {
        autoSkipFrames = 2;
        var rt = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32);
        var previous = source.targetTexture;
        source.targetTexture = rt;
        source.Render();
        source.Render();
        source.targetTexture = previous;
        var active = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = active;
        RenderTexture.ReleaseTemporary(rt);
        File.WriteAllBytes(Path.Combine(autoOutDir, name + ".png"), tex.EncodeToPNG());
        Destroy(tex);
        autoFrameNames.Add(name + ".png");
        autoShots++;
    }

    static float Percentile(List<float> values, float q)
    {
        if (values.Count == 0) return 0f;
        var sorted = new List<float>(values);
        sorted.Sort();
        return sorted[Mathf.Clamp(Mathf.CeilToInt(q * sorted.Count) - 1, 0, sorted.Count - 1)];
    }

    static int CountNamed(Transform root, string name)
    {
        var count = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) count++;
        return count;
    }

    // ---------- The glass scenario (-autopilotGlass) ----------
    // Once the map has settled, the autopilot stands 0.9 m in front of the
    // nearest intact pane (inside its keep-clear strip), aims at it and holds
    // E through the same input path as a player. It checks that the glass
    // shot starts, lands its three beats, steps the body in and moves the
    // camera; that the window shatters (WindowShattered, the way open); that
    // the camera is back on the eye after the shot; then walks into the frame
    // and climbs through to the far cell. Frames at the beats go to
    // Verification/main-autopilot. A failed check fails the run.
    bool autoGlassWanted, autoGlassActive, autoGlassDone, autoEDown, autoEHeld;
    // C32's W fix, tools: -autopilotFirstSendNoW (the first send's ladder as before the fix, for a reference run).
    bool autoFirstSendNoW;
    int autoGlassStep, autoGlassBeats, autoGlassShatters, autoGlassClimbs, autoGlassCapture;
    float autoGlassClock, autoGlassMaxOffset;
    float[] autoGlassBeatStrength = new float[3];
    FrontRoomsMapWorld.Window autoGlassWindow;
    Vector3 autoGlassFeet;
    readonly List<string> autoGlassChecks = new List<string>();
    static readonly float[] AutoGlassCaptureAt = { .30f, .36f, .72f, 1.02f, 1.12f, 1.45f };

    void AutoGlassCheck(bool ok, string what)
    {
        autoGlassChecks.Add((ok ? "ok   " : "FAIL ") + what);
        Log("AUTOPILOT GLASS " + (ok ? "ok   " : "FAIL ") + what);
    }

    void AutoGlassBeat(int beat, float intensity)
    {
        if (!autoGlassActive) return;
        autoGlassBeats++;
        if (beat >= 1 && beat <= 3) autoGlassBeatStrength[beat - 1] = intensity;
    }

    void AutoGlassShattered(FrontRoomsMapWorld.Window w, Vector3 at, Vector3 impulse) { if (w == autoGlassWindow) autoGlassShatters++; }
    void AutoGlassClimbed(Vector3 at) { if (autoGlassActive) autoGlassClimbs++; }

    void AutoGlassEnd()
    {
        autoGlassActive = false;
        autoGlassDone = true;
        autoEDown = autoEHeld = false;
        FrontRoomsGlassShot.Beat -= AutoGlassBeat;
        map.WindowShattered -= AutoGlassShattered;
        PlayerClimbed -= AutoGlassClimbed;
        // Back to the walk: plan a fresh route from wherever the climb left it.
        autoRouteIndex = autoRoute.Count;
    }

    void AutopilotGlass(float dt)
    {
        if (phase != Phase.Playing) return;
        if (!autoGlassActive)
        {
            if (inStartRooms || autoSettledAt < 0f || autoPlayClock < 6f || climbTime >= 0f || pullClear.HasValue) return;
            autoGlassWindow = map.NearestIntactWindowForTools(playerRoot.position);
            if (autoGlassWindow == null)
            {
                if (autoPlayClock > 30f) { AutoGlassCheck(false, "glass: no intact window built within the map"); autoGlassDone = true; }
                return;
            }
            // Stand on cell a's side, 0.9 m from the pane, facing it, aimed 1.3 m up.
            var root = autoGlassWindow.root;
            var feet = root.position - root.forward * .9f;
            feet.y = playerRoot.position.y;
            playerBody.enabled = false;
            playerRoot.position = feet;
            playerBody.enabled = true;
            Physics.SyncTransforms();
            yaw = Quaternion.LookRotation(root.forward, Vector3.up).eulerAngles.y;
            pitch = Mathf.Atan2(EyeHeight - 1.3f, .9f) * Mathf.Rad2Deg;
            FrontRoomsGlassShot.Beat += AutoGlassBeat;
            map.WindowShattered += AutoGlassShattered;
            PlayerClimbed += AutoGlassClimbed;
            autoGlassActive = true;
            autoGlassStep = 1;
            autoGlassClock = 0f;
            autoGlassCapture = 0;
            Log("AUTOPILOT GLASS window " + autoGlassWindow.a + "-" + autoGlassWindow.b);
            return;
        }
        autoGlassClock += dt;
        switch (autoGlassStep)
        {
            case 1:
                // One settling frame after the move, then E goes down on the pane.
                if (autoGlassClock < .05f) return;
                autoGlassFeet = playerRoot.position;
                autoEDown = autoEHeld = true;
                autoGlassStep = 2;
                autoGlassClock = 0f;
                return;
            case 2:
                autoEDown = false;
                if (autoGlassClock < dt * 1.5f)
                {
                    AutoGlassCheck(glassShot != null && glassShot.Active && aimedHold, "glass: E on the pane from 0.9 m starts the shot");
                    if (glassShot == null || !glassShot.Active) { AutoGlassEnd(); return; }
                }
                autoGlassMaxOffset = Mathf.Max(autoGlassMaxOffset, cam.transform.localPosition.magnitude);
                while (autoGlassCapture < AutoGlassCaptureAt.Length && glassShot != null && glassShot.Clock >= AutoGlassCaptureAt[autoGlassCapture])
                    AutopilotCapture((autoShots < 10 ? "0" : "") + autoShots + "_glass_t" + Mathf.RoundToInt(AutoGlassCaptureAt[autoGlassCapture++] * 100f).ToString("000"));
                if (autoGlassShatters > 0)
                {
                    var stepped = Flat(playerRoot.position - autoGlassFeet).magnitude;
                    var moving = FrontRoomsSettings.CameraMotionScale > 0f;
                    AutoGlassCheck(autoGlassBeats == 3, "glass: three beats landed (" + autoGlassBeats + "; strengths " + string.Join("/", Array.ConvertAll(autoGlassBeatStrength, x => x.ToString("0.0"))) + ")");
                    AutoGlassCheck(!moving || stepped > .25f, "glass: the body stepped in " + stepped.ToString("0.00") + " m toward the pane");
                    AutoGlassCheck(!moving || autoGlassMaxOffset > .02f, "glass: the camera left the eye during the hold (" + autoGlassMaxOffset.ToString("0.000") + " m)");
                    AutoGlassCheck(map.IsBrokenWindow(autoGlassWindow.a, autoGlassWindow.b) && map.PassageBetween(autoGlassWindow.a, autoGlassWindow.b) == FrontRoomsMapWorld.Passage.Open
                        && autoGlassWindow.pane == null && autoGlassWindow.root != null, "glass: WindowShattered; the pane gone, the root kept, the way open");
                    autoEHeld = false;
                    autoGlassStep = 3;
                    autoGlassClock = 0f;
                }
                else if (autoGlassClock > 3f)
                {
                    AutoGlassCheck(false, "glass: the pane never shattered (hold " + holdProgress.ToString("0.00") + ", aimed " + (aimed != null) + ")");
                    AutoGlassEnd();
                }
                return;
            case 3:
                while (autoGlassCapture < AutoGlassCaptureAt.Length && glassShot != null && glassShot.Active && glassShot.Clock >= AutoGlassCaptureAt[autoGlassCapture])
                    AutopilotCapture((autoShots < 10 ? "0" : "") + autoShots + "_glass_t" + Mathf.RoundToInt(AutoGlassCaptureAt[autoGlassCapture++] * 100f).ToString("000"));
                if (autoGlassClock < .9f) return;
                AutoGlassCheck(glassShot != null && !glassShot.Active && !glassShot.MoveLocked && (rig == null || !rig.InShot) && cam.transform.localPosition.magnitude < 1e-3f,
                    "glass: the shot is over by 1.9 s and the camera is back on the eye (" + cam.transform.localPosition.magnitude.ToString("0.0000") + " m)");
                autoGlassStep = 4;
                autoGlassClock = 0f;
                return;
            case 4:
                if (autoGlassClimbs > 0 && climbTime < 0f)
                {
                    var cell = map.CellOf(playerRoot.position);
                    AutoGlassCheck(cell == autoGlassWindow.b, "glass: walked into the frame and climbed through to " + cell + " (far cell " + autoGlassWindow.b + ")");
                    AutoGlassEnd();
                }
                else if (autoGlassClock > 3f)
                {
                    AutoGlassCheck(false, "glass: no climb through the broken frame within 3 s");
                    AutoGlassEnd();
                }
                return;
        }
    }

    // ---------- The door scenario (-autopilotDoors, editor only; door_shots_plan.md §3.11, §5.4) ----------
    // Keys go on for this run's map (the profile asset is untouched) and the Relay is held where it stands, so it
    // cannot cancel a shot under test. Four cases on the nearest shut doors, each pressed through the player's E
    // (EDown): a locked try and a second try 0.2 s later (the rattle and its debounce); the unlock from the push
    // side, with the key (its timings, the camera, frames at the beats); a second E 0.4 s into an unlock on another
    // door (the cancel); and the unlock from the side the leaf last went to (A12: the key turn turns the shut leaf
    // away, so it opens at 1.00 s like the push and rests beyond the player). Times are on the
    // shot's own clock (from E-down); a long frame (a capture renders twice) widens the tolerance to that frame.
    // Then two key pickups (plan §3.3) on the nearest keys still lying, walked onto by a teleport from 1.4–2 m off:
    // without a model (the instant path: KeyTaken and the panel at contact, the cube gone), and with the placeholder
    // (presented: KeyTaken at 0.25 s, the pocket beat at 0.85, the key gone and the panel fading in from 0.95, frames
    // at the beats). Keys and the placeholder go back off at the end. A failed check fails the run.
    bool autoDoorWanted, autoDoorActive, autoDoorDone, autoDoorKeysWere;
    int autoDoorCase, autoDoorStep, autoDoorCapture, autoDoorLocked, autoDoorRattled, autoDoorUnlocked, autoDoorShots, autoDoorKickRises;
    float autoDoorClock, autoDoorUnlockedAt, autoDoorOpenAt, autoDoorShotEndAt, autoDoorFreeAt, autoDoorCamBackAt, autoDoorFovBackAt;
    float autoDoorMaxOffset, autoDoorMinFov, autoDoorMaxDt, autoDoorPrevKick, autoDoorPressAt;
    bool autoDoorLatchDrawn, autoDoorHingeMoved;
    FrontRoomsMapWorld.DoorOpenSource autoDoorOpenSource;
    FrontRoomsMapWorld.Door autoDoor;
    Quaternion autoDoorHingeAt;
    readonly List<string> autoDoorChecks = new List<string>();
    static readonly float[] AutoDoorCaptureAt = { .30f, .55f, .70f, .86f, 1.00f, 1.30f, 1.55f };
    static readonly string[] AutoDoorCaseNames = { "rattle", "unlock (push)", "cancel", "unlock (leaf side)", "key (instant)", "key (presented)" };
    // The key cases: the key, its zone, the spot to walk onto it from; the counts before contact; what the
    // presentation did (on the play clock from contact: KeyTaken, the pocket beat, the end; the panel's alpha).
    Transform autoKey;
    GridCoord autoKeyZone;
    Vector3 autoKeyContact;
    int autoKeyPickupsBefore, autoKeyTakenBefore, autoKeyBreaksBefore;
    float autoKeyStartedAt, autoKeyPocketAt, autoKeyGoneAt, autoKeyPanelFirstAlpha, autoKeyMaxHudFade;
    bool autoKeyPanelEarly, autoKeyParentedToMap, autoKeyPanelSeen;
    // HK M-6: the key panel the player sees: the key HUD's when it is attached (the map's own panel is off then), else
    // the map's; and how far it has come in, 0..1 (the key HUD: its P4 flight, below 1 until it lands; the map: its fade).
    bool AutoKeyPanelUp => keyHud != null ? keyHud.PanelShown : keyPanel != null && keyPanel.activeSelf;
    float AutoKeyPanelIn => keyHud != null ? keyHud.PanelArrival : keyPanelFade;
    static readonly float[] AutoKeyCaptureAt = { .10f, .25f, .50f, .85f };
    void AutoKeyBeat(ShotKind kind, DoorBeat beat, Vector3 p)
    {
        if (autoDoorActive && kind == ShotKind.KeyPickup && beat == DoorBeat.Pocket && autoKeyPocketAt < 0f) autoKeyPocketAt = elapsed - autoKeyStartedAt;
    }

    void AutoDoorCheck(bool ok, string what)
    {
        autoDoorChecks.Add((ok ? "ok   " : "FAIL ") + what);
        Log("AUTOPILOT DOORS " + (ok ? "ok   " : "FAIL ") + what);
    }

    void AutoDoorLockedAt(Vector3 p) { if (autoDoorActive) autoDoorLocked++; }
    void AutoDoorRattledAt(FrontRoomsMapWorld.Door d, Vector3 p) { if (autoDoorActive && d == autoDoor) autoDoorRattled++; }
    void AutoDoorUnlockedAt(FrontRoomsMapWorld.Door d, GridCoord zone, Vector3 p)
    {
        if (!autoDoorActive || d != autoDoor) return;
        autoDoorUnlocked++;
        autoDoorUnlockedAt = doorShot != null ? doorShot.Clock : -1f;
    }
    void AutoDoorOpenBegan(FrontRoomsMapWorld.Door d, FrontRoomsMapWorld.DoorOpenSource source, bool latchDrawn)
    {
        if (!autoDoorActive || d != autoDoor || autoDoorOpenAt >= 0f) return;
        autoDoorOpenAt = doorShot != null ? doorShot.Clock : -1f;
        autoDoorOpenSource = source;
        autoDoorLatchDrawn = latchDrawn;
    }
    void AutoDoorShotStarted(ShotKind kind, Vector3 focus) { if (autoDoorActive && kind == ShotKind.Unlock) autoDoorShots++; }
    void AutoDoorShotEnded(ShotKind kind, Vector3 focus)
    {
        if (autoDoorActive && kind == ShotKind.Unlock && autoDoorShotEndAt < 0f) autoDoorShotEndAt = doorShot != null ? doorShot.Clock : -1f;
    }

    void AutoDoorEnd()
    {
        autoDoorActive = false;
        autoDoorDone = true;
        autoEDown = autoEHeld = false;
        map.DoorsNeedKeysForTools = autoDoorKeysWere;
        if (relay != null) relay.HeldForTools = false;
        map.DoorLocked -= AutoDoorLockedAt;
        map.DoorRattled -= AutoDoorRattledAt;
        map.DoorUnlocked -= AutoDoorUnlockedAt;
        map.DoorOpenBegan -= AutoDoorOpenBegan;
        FrontRoomsCameraRig.ShotStarted -= AutoDoorShotStarted;
        FrontRoomsCameraRig.ShotEnded -= AutoDoorShotEnded;
        FrontRoomsDoorShot.Beat -= AutoKeyBeat;
        // The autopilot runs with the placeholder off (set at run start); only the presented key case turns it on.
        FrontRoomsMapWorld.ShowPlaceholderKey = false;
        // Back to the walk: plan a fresh route from wherever the last door left it.
        autoRouteIndex = autoRoute.Count;
    }

    // Stand 0.9 m out from the lock on the face `side` is on, facing the door, the crosshair 1.3 m up the leaf (as the
    // glass scenario): the shot finds the keyhole itself (a raw ray 36° down, at the lock, would meet the player's own
    // controller; the aim skips it since A10, see AutoDoorSelfHitNote).
    void AutoDoorStand(FrontRoomsMapWorld.Door door, Vector3 side)
    {
        var lockPose = map.LockPose(door, side);
        var normal = FrontRoomsMapWorld.LockNormal(door, side);
        var feet = lockPose.position + normal * .9f;
        feet.y = playerRoot.position.y;
        playerBody.enabled = false;
        playerRoot.position = feet;
        playerBody.enabled = true;
        Physics.SyncTransforms();
        yaw = Quaternion.LookRotation(-normal, Vector3.up).eulerAngles.y;
        pitch = Mathf.Atan2(EyeHeight - 1.3f, .9f) * Mathf.Rad2Deg;
    }

    // Once per run, 0-75° down: the aim (AimCast) never stops on the player's own body (A10, a check); where a raw ray
    // would first meet the player's CharacterController is kept as a note.
    void AutoDoorSelfHitNote()
    {
        var eye = rig != null ? rig.BaseEye.position : cam.transform.position;
        var flat = Quaternion.Euler(0f, yaw, 0f);
        var raw = -1;
        var aimSelf = -1;
        for (var deg = 0; deg <= 75; deg++)
        {
            var ray = new Ray(eye, flat * Quaternion.Euler(deg, 0f, 0f) * Vector3.forward);
            if (raw < 0 && Physics.Raycast(ray, out var hit, Reach, ~0, QueryTriggerInteraction.Ignore) && hit.collider == playerBody) raw = deg;
            if (aimSelf < 0 && AimCast(ray, out var aim) && aim.collider.transform.IsChildOf(playerRoot)) aimSelf = deg;
        }
        autoDoorChecks.Add(raw >= 0 ? "note a raw ray meets the player's own controller from " + raw + "° down (the aim skips it)" : "note a raw ray never meets the player's own controller (0–75° down)");
        AutoDoorCheck(aimSelf < 0, "doors: the aim never stops on the player's own body, 0-75° down" + (aimSelf >= 0 ? " (it does at " + aimSelf + "°)" : ""));
    }

    // The next case: its door (the first unlock reuses the rattled one), the player put in front of it, the counters cleared.
    void AutoDoorNext(int next)
    {
        autoDoorCase = next;
        autoDoorStep = 0;
        autoDoorClock = 0f;
        autoDoorCapture = 0;
        autoDoorLocked = autoDoorRattled = autoDoorUnlocked = autoDoorShots = autoDoorKickRises = 0;
        autoDoorUnlockedAt = autoDoorOpenAt = autoDoorShotEndAt = autoDoorFreeAt = autoDoorCamBackAt = autoDoorFovBackAt = -1f;
        autoDoorMaxOffset = autoDoorMaxDt = 0f;
        autoDoorMinFov = cam.fieldOfView;
        autoDoorHingeMoved = autoDoorLatchDrawn = false;
        autoDoorPrevKick = rig != null ? Mathf.Abs(rig.KickPitch) : 0f;
        if (next == 1) return;
        if (next >= 4)
        {
            AutoKeyNext(next);
            return;
        }
        var pullSide = next == 3;
        autoDoor = map.NearestClosedDoorForTools(playerRoot.position, pullSide);
        if (autoDoor == null)
        {
            AutoDoorCheck(false, "doors: no shut door built near the player with them on its " + (pullSide ? "swing" : "stop") + " side (" + AutoDoorCaseNames[next] + ")");
            AutoDoorEnd();
            return;
        }
        AutoDoorStand(autoDoor, playerRoot.position);
        Log("AUTOPILOT DOORS " + AutoDoorCaseNames[next] + " · door " + autoDoor.a + "-" + autoDoor.b);
    }

    // A key case: the nearest key still lying, the player put 1.4–2 m from it (a clear walk onto it), facing it.
    void AutoKeyNext(int next)
    {
        autoKey = map.NearestKeyForTools(playerRoot.position, out autoKeyZone, out _);
        if (autoKey == null || !AutoKeyStand(autoKey.position))
        {
            AutoDoorCheck(false, "doors: " + (autoKey == null ? "no key lying in the built map" : "no clear spot to walk onto the key of zone " + autoKeyZone) + " (" + AutoDoorCaseNames[next] + ")");
            AutoDoorEnd();
            return;
        }
        autoKeyPocketAt = autoKeyGoneAt = autoKeyPanelFirstAlpha = -1f;
        autoKeyMaxHudFade = 0f;
        autoKeyPanelEarly = autoKeyParentedToMap = autoKeyPanelSeen = false;
        Log("AUTOPILOT DOORS " + AutoDoorCaseNames[next] + " · key of zone " + autoKeyZone + " at " + autoKey.position.ToString("F2"));
    }

    // Where to stand for a key: a clear spot within 0.6 m (flat) of it to walk onto (contact is 0.9 m), and one
    // 1.4–2 m off with a clear line to it at knee height (the same room). The player goes to the second, facing the key.
    bool AutoKeyStand(Vector3 keyPosition)
    {
        var floor = playerRoot.position.y;
        var keyFlat = new Vector3(keyPosition.x, floor, keyPosition.z);
        var found = false;
        foreach (var r in new[] { 0f, .3f, .6f })
        {
            for (var i = 0; i < 16 && !found; i++)
            {
                var spot = keyFlat + Quaternion.Euler(0f, i * 22.5f, 0f) * Vector3.forward * r;
                if (AutoBodyClear(spot)) { autoKeyContact = spot; found = true; }
            }
            if (found) break;
        }
        if (!found) return false;
        foreach (var r in new[] { 1.6f, 1.4f, 2f })
            for (var i = 0; i < 16; i++)
            {
                var spot = autoKeyContact + Quaternion.Euler(0f, i * 22.5f, 0f) * Vector3.forward * r;
                if (Flat(spot - keyFlat).magnitude < 1.2f || !AutoBodyClear(spot)) continue;
                if (Physics.Linecast(spot + Vector3.up * .5f, autoKeyContact + Vector3.up * .5f, ~0, QueryTriggerInteraction.Ignore)) continue;
                playerBody.enabled = false;
                playerRoot.position = spot;
                playerBody.enabled = true;
                Physics.SyncTransforms();
                yaw = Quaternion.LookRotation(keyFlat - spot, Vector3.up).eulerAngles.y;
                pitch = 15f;
                return true;
            }
        return false;
    }

    // A body (the player's capsule) fits here: only walls, doors and furniture count, as ClearStandingSpot.
    bool AutoBodyClear(Vector3 feet)
    {
        var r = ModuleUnits.PlayerRadius;
        return !Physics.CheckCapsule(feet + Vector3.up * (r + .2f), feet + Vector3.up * (ModuleUnits.PlayerHeight - r), r, ~0, QueryTriggerInteraction.Ignore);
    }

    // After a teleport the map streams round the new spot: wait until it has settled and frames are short again, or
    // a long frame (the map's door clock takes raw deltaTime up to 0.33 s; the game's is capped at 0.1 s) skews the
    // debounce, and an unsettled map makes the unlock play in place. Gives up (a FAIL) after 8 s.
    bool AutoDoorReady()
    {
        if (map.Settled && Time.deltaTime < .1f) return true;
        if (autoDoorClock > 8f)
        {
            AutoDoorCheck(false, "doors: the map never settled round " + AutoDoorCaseNames[autoDoorCase] + "'s door (8 s)");
            AutoDoorEnd();
        }
        return false;
    }

    // One frame of E, as a player's press.
    void AutoDoorPress()
    {
        Log("AUTOPILOT DOORS press E at " + autoDoorClock.ToString("0.000") + " (aimed " + (aimed != null ? aimed.name : "nothing") + ", door clock " + map.DoorClockForTools.ToString("0.000") + ")");
        autoEDown = autoEHeld = true;
        autoDoorPressAt = autoDoorClock;
    }

    void AutoDoorCaptures(string label)
    {
        if (doorShot == null) return;
        while (autoDoorCapture < AutoDoorCaptureAt.Length && (doorShot.Clock >= AutoDoorCaptureAt[autoDoorCapture] || !doorShot.UnlockActive))
            AutopilotCapture((autoShots < 10 ? "0" : "") + autoShots + "_door_" + label + "_t" + Mathf.RoundToInt(AutoDoorCaptureAt[autoDoorCapture++] * 100f).ToString("000"));
    }

    void AutopilotDoors(float dt)
    {
        if (phase != Phase.Playing) return;
        if (!autoDoorActive)
        {
            if (inStartRooms || autoSettledAt < 0f || autoPlayClock < 6f || climbTime >= 0f || pullClear.HasValue) return;
            if (autoGlassWanted && !autoGlassDone) return;
            if (relay != null && (relay.State == HunterState.Chase || relay.State == HunterState.BreakDoor || relay.SeesPlayer)) return;
            autoDoorKeysWere = map.DoorsNeedKeysForTools;
            map.DoorsNeedKeysForTools = true;
            if (relay != null) relay.HeldForTools = true;
            map.DoorLocked += AutoDoorLockedAt;
            map.DoorRattled += AutoDoorRattledAt;
            map.DoorUnlocked += AutoDoorUnlockedAt;
            map.DoorOpenBegan += AutoDoorOpenBegan;
            FrontRoomsCameraRig.ShotStarted += AutoDoorShotStarted;
            FrontRoomsCameraRig.ShotEnded += AutoDoorShotEnded;
            autoDoorActive = true;
            AutoDoorSelfHitNote();
            Log("AUTOPILOT DOORS keys on for this run's map; the Relay held (" + (relay != null ? relay.State.ToString() : "none") + ")");
            AutoDoorNext(0);
            return;
        }
        autoDoorClock += dt;
        autoDoorMaxDt = Mathf.Max(autoDoorMaxDt, dt);
        // A press lasts one frame.
        if (autoEDown && autoDoorClock > autoDoorPressAt) autoEDown = autoEHeld = false;
        var moving = FrontRoomsSettings.CameraMotionScale > 0f;
        var full = FrontRoomsSettings.CameraMotionScale >= .999f;
        var slack = Mathf.Max(.03f, autoDoorMaxDt);
        var offset = cam.transform.localPosition.magnitude;
        autoDoorMaxOffset = Mathf.Max(autoDoorMaxOffset, offset);
        autoDoorMinFov = Mathf.Min(autoDoorMinFov, cam.fieldOfView);
        var baseFov = rig != null ? rig.BaseFov : cam.fieldOfView;
        switch (autoDoorCase)
        {
            case 0:
            {
                // A locked try, and a second one 0.2 s later inside Rattle.Duration: one rattle, two pitch kicks. No frame is
                // captured in that window: a capture's long frame runs the map's door clock (raw deltaTime) past the game's
                // (capped at 0.1 s), which a player's two presses never see. Then a third try after the window, for the frame.
                var kick = rig != null ? Mathf.Abs(rig.KickPitch) : 0f;
                if (kick > autoDoorPrevKick + .05f) autoDoorKickRises++;
                autoDoorPrevKick = kick;
                if (autoDoorStep == 0 && autoDoorClock >= .25f && AutoDoorReady()) { autoDoorClock = 0f; AutoDoorPress(); autoDoorStep = 1; return; }
                if (autoDoorStep == 1 && autoDoorClock >= .2f) { AutoDoorPress(); autoDoorStep = 2; return; }
                if (autoDoorStep == 2 && autoDoorClock >= .55f)
                {
                    AutoDoorCheck(autoDoorLocked == 1 && autoDoorRattled == 1, "doors: two tries 0.2 s apart on a locked door raise DoorLocked and DoorRattled once each (" + autoDoorLocked + ", " + autoDoorRattled + ")");
                    AutoDoorCheck(!moving || autoDoorKickRises == 2, "doors: the rattle kicks the camera's pitch twice (" + autoDoorKickRises + ")");
                    autoDoorClock = 0f;
                    AutoDoorPress();
                    autoDoorStep = 3;
                    return;
                }
                if (autoDoorStep == 3 && autoDoorClock >= .05f && autoDoorCapture == 0)
                {
                    AutopilotCapture((autoShots < 10 ? "0" : "") + autoShots + "_door_rattle_t005");
                    autoDoorCapture = 1;
                }
                if (autoDoorStep == 3 && autoDoorClock >= .5f)
                {
                    AutoDoorCheck(autoDoorLocked == 2 && autoDoorRattled == 2, "doors: a try after Rattle.Duration rattles again (" + autoDoorLocked + ", " + autoDoorRattled + ")");
                    AutoDoorCheck(autoDoorShots == 0 && (rig == null || !rig.InShot) && !map.IsUnlockedForTools(autoDoor), "doors: no shot and no unlock without the key");
                    AutoDoorNext(1);
                }
                return;
            }
            case 1:
            case 3:
            {
                // The unlock with the key: from the side away from the leaf (case 1, the rattled door) or from the side
                // the leaf last went to (case 3; A12: a push all the same).
                if (autoDoorStep == 0)
                {
                    if (autoDoorClock < .45f || !AutoDoorReady()) return;
                    map.GiveKeyForTools(map.ZoneOf(map.CellOf(playerRoot.position)).id);
                    autoDoorHingeAt = autoDoor.hinge != null ? autoDoor.hinge.localRotation : Quaternion.identity;
                    autoDoorMinFov = cam.fieldOfView;
                    autoDoorMaxOffset = 0f;
                    autoDoorMaxDt = 0f;
                    autoDoorClock = 0f;
                    AutoDoorPress();
                    autoDoorStep = 1;
                    return;
                }
                var label = autoDoorCase == 1 ? "unlock" : "unlock_leafside";
                if (autoDoorStep == 1 && autoDoorClock > autoDoorPressAt && !autoEDown)
                {
                    AutoDoorCheck(doorShot != null && doorShot.UnlockActive && autoDoorShots == 1, "doors: E with the key starts the Unlock shot (" + AutoDoorCaseNames[autoDoorCase] + ", door " + autoDoor.a + "-" + autoDoor.b + ")");
                    if (doorShot == null || !doorShot.UnlockActive) { AutoDoorEnd(); return; }
                    autoDoorStep = 2;
                }
                if (autoDoorStep < 2) return;
                AutoDoorCaptures(label);
                if (autoDoorFreeAt < 0f && doorShot.Clock > .5f && !doorShot.MoveLocked) autoDoorFreeAt = doorShot.Clock;
                if (autoDoorCase == 3 && doorShot.UnlockActive && doorShot.Clock < .99f && autoDoor.hinge != null && Quaternion.Angle(autoDoor.hinge.localRotation, autoDoorHingeAt) > .1f) autoDoorHingeMoved = true;
                if (autoDoorClock < 1.8f) return;
                var unlockOk = autoDoorUnlocked == 1 && Mathf.Abs(autoDoorUnlockedAt - FrontRoomsShotTimings.Unlock.Commit) <= slack;
                AutoDoorCheck(unlockOk && map.IsUnlockedForTools(autoDoor), "doors: DoorUnlocked once, at " + autoDoorUnlockedAt.ToString("0.000") + " s (commit 0.86 ± " + slack.ToString("0.000") + ") (" + label + ")");
                if (autoDoorCase == 1)
                {
                    AutoDoorCheck(!full || autoDoorMaxOffset > .2f, "doors: the camera dipped toward the lock (max " + autoDoorMaxOffset.ToString("0.000") + " m from the eye)");
                    AutoDoorCheck(!full || autoDoorMinFov <= baseFov - 13.5f, "doors: the FOV pushed in (min " + autoDoorMinFov.ToString("0.0") + " of " + baseFov.ToString("0.0") + ")");
                    AutoDoorCheck(autoDoorOpenAt >= 0f && autoDoorOpenAt <= 1.03f + slack && autoDoorOpenSource == FrontRoomsMapWorld.DoorOpenSource.Unlock && autoDoorLatchDrawn,
                        "doors: push side: the leaf opens at " + autoDoorOpenAt.ToString("0.000") + " s with the latch drawn (by 1.03; source " + autoDoorOpenSource + ")");
                    AutoDoorCheck(autoDoorFreeAt >= 0f && autoDoorFreeAt <= FrontRoomsShotTimings.Unlock.MoveLockEnd + .02f + slack, "doors: move is free at " + autoDoorFreeAt.ToString("0.000") + " s (by 1.47)");
                }
                else
                {
                    AutoDoorCheck(!autoDoorHingeMoved, "doors: leaf side: the leaf stays shut until the knob beat (no hinge turn before 1.00 s)");
                    AutoDoorCheck(autoDoorOpenAt >= 0f && autoDoorOpenAt <= 1.03f + slack && autoDoorOpenSource == FrontRoomsMapWorld.DoorOpenSource.Unlock && autoDoorLatchDrawn,
                        "doors: leaf side (A12): the leaf opens at " + autoDoorOpenAt.ToString("0.000") + " s with the latch drawn, a push (by 1.03; source " + autoDoorOpenSource + ")");
                    AutoDoorCheck(!FrontRoomsMapWorld.OnSwingSide(autoDoor, playerRoot.position) && map.CellOf(autoDoor.leaf.transform.position) != map.CellOf(playerRoot.position),
                        "doors: leaf side (A12): the leaf went away from the player, into the cell beyond the door");
                }
                AutoDoorCheck(autoDoorShotEndAt >= 0f && autoDoorShotEndAt <= 1.60f + slack && offset < 1e-3f && Mathf.Abs(cam.fieldOfView - baseFov) < .05f && (rig == null || !rig.InShot),
                    "doors: ShotEnded at " + autoDoorShotEndAt.ToString("0.000") + " s (by 1.60), the camera back on the eye (" + offset.ToString("0.0000") + " m, FOV " + cam.fieldOfView.ToString("0.0") + ") (" + label + ")");
                AutoDoorCheck(autoDoor.open, "doors: the door is open after the shot (" + label + ")");
                AutoDoorNext(autoDoorCase == 1 ? 2 : 4);
                return;
            }
            case 2:
            {
                // A second E 0.4 s into the unlock (after Unlock.CancelAfter): the key comes out, the door stays locked and shut.
                if (autoDoorStep == 0)
                {
                    if (autoDoorClock < .25f || !AutoDoorReady()) return;
                    map.GiveKeyForTools(map.ZoneOf(map.CellOf(playerRoot.position)).id);
                    autoDoorClock = 0f;
                    AutoDoorPress();
                    autoDoorStep = 1;
                    return;
                }
                if (autoDoorStep == 1)
                {
                    if (doorShot == null || doorShot.Clock < .4f) return;
                    AutoDoorPress();
                    autoDoorStep = 2;
                    return;
                }
                var since = doorShot.Clock;
                if (autoDoorStep == 2 && !autoEDown)
                {
                    if (autoDoorCamBackAt < 0f && offset < 1e-3f) autoDoorCamBackAt = autoDoorClock;
                    if (autoDoorFovBackAt < 0f && Mathf.Abs(cam.fieldOfView - baseFov) < .05f) autoDoorFovBackAt = autoDoorClock;
                }
                if (autoDoorClock < 1.4f) return;
                AutoDoorCheck(autoDoorUnlocked == 0 && !map.IsUnlockedForTools(autoDoor) && !autoDoor.open, "doors: cancel at 0.4 s: no DoorUnlocked, the door locked and shut");
                AutoDoorCheck(autoDoorCamBackAt >= 0f && autoDoorCamBackAt <= .70f + slack, "doors: cancel: the camera back on the eye at " + autoDoorCamBackAt.ToString("0.000") + " s (by 0.70)");
                AutoDoorCheck(autoDoorFovBackAt >= 0f && autoDoorFovBackAt <= .85f + slack, "doors: cancel: the FOV back at " + autoDoorFovBackAt.ToString("0.000") + " s (by 0.85)");
                AutoDoorCheck(!doorShot.UnlockActive && (rig == null || !rig.InShot), "doors: cancel: the shot is over (stopped at " + since.ToString("0.000") + " s)");
                AutoDoorNext(3);
                return;
            }
            case 4:
            case 5:
            {
                // A key pickup: the walk onto the key (a teleport from the stand spot) once the map has settled round it.
                var presentedCase = autoDoorCase == 5;
                var caseName = AutoDoorCaseNames[autoDoorCase];
                if (autoDoorStep == 0)
                {
                    if (autoDoorClock < .25f || !AutoDoorReady()) return;
                    if (autoKey == null)
                    {
                        AutoDoorCheck(false, "doors: the key of zone " + autoKeyZone + " went before the walk onto it (" + caseName + ")");
                        AutoDoorEnd();
                        return;
                    }
                    FrontRoomsMapWorld.ShowPlaceholderKey = presentedCase;
                    if (presentedCase) FrontRoomsDoorShot.Beat += AutoKeyBeat;
                    autoKeyPickupsBefore = autoKeyPickups;
                    autoKeyTakenBefore = keysTaken;
                    autoKeyBreaksBefore = autoKeyBreaks.Count;
                    if (map.KeyPickupNeedsE)
                    {
                        // C14: as a desktop player takes it, from 1.1 m (flat), looking at the key, with one E.
                        var keyAt = autoKey.position;
                        var keyFlat = new Vector3(keyAt.x, playerRoot.position.y, keyAt.z);
                        var away = Flat(playerRoot.position - keyFlat);
                        var spot = away.sqrMagnitude > 1e-4f ? keyFlat + new Vector3(away.x, 0f, away.y).normalized * 1.1f : playerRoot.position;
                        if (AutoBodyClear(spot))
                        {
                            playerBody.enabled = false;
                            playerRoot.position = spot;
                            playerBody.enabled = true;
                            Physics.SyncTransforms();
                        }
                        yaw = Quaternion.LookRotation(keyFlat - playerRoot.position, Vector3.up).eulerAngles.y;
                        pitch = Mathf.Atan2(playerRoot.position.y + EyeHeight - keyAt.y, Flat(keyFlat - playerRoot.position).magnitude) * Mathf.Rad2Deg;
                        AutoDoorPress();
                    }
                    else
                    {
                        playerBody.enabled = false;
                        playerRoot.position = autoKeyContact;
                        playerBody.enabled = true;
                        Physics.SyncTransforms();
                    }
                    autoDoorMaxDt = 0f;
                    autoDoorClock = 0f;
                    autoDoorStep = 1;
                    return;
                }
                var inZone = map.ZoneOf(map.CellOf(playerRoot.position)).id == autoKeyZone;
                if (autoDoorStep == 1)
                {
                    // The map takes it in its Update (contact: 0.9 m, flat).
                    if (autoKeyPickups == autoKeyPickupsBefore || !autoKeyStartAt.ContainsKey(autoKeyZone))
                    {
                        if (autoDoorClock <= 1f) return;
                        AutoDoorCheck(false, "doors: " + (map.KeyPickupNeedsE ? "E on" : "walked onto") + " the key of zone " + autoKeyZone + " and nothing was picked up in 1 s (" + caseName + ")");
                        AutoDoorEnd();
                        return;
                    }
                    autoKeyStartedAt = autoKeyStartAt[autoKeyZone];
                    if (!presentedCase)
                        AutoDoorCheck(keysTaken == autoKeyTakenBefore + 1 && !autoKeyLastPresented && autoKeyLastLag == 0f && autoKeyBreaks.Count == autoKeyBreaksBefore
                                && map.HasKeyFor(autoKeyZone) && !doorShot.Presenting(autoKeyZone),
                            "doors: key (instant): KeyPickupStarted and KeyTaken in the same frame, not presented (no model, the placeholder off), the key held");
                    else
                    {
                        autoKeyParentedToMap = autoKey != null && autoKey.parent == map.transform;
                        AutoDoorCheck(doorShot.Presenting(autoKeyZone) && map.HasKeyFor(autoKeyZone) && keysTaken == autoKeyTakenBefore && autoKeyParentedToMap,
                            "doors: key (presented): at contact the key is held and presented, KeyTaken not yet, the object on the map's root");
                    }
                    autoDoorStep = 2;
                    return;
                }
                var since = elapsed - autoKeyStartedAt;
                if (!presentedCase)
                {
                    // The frame after contact: the cube gone, the panel up at full alpha (as before step 5).
                    // HK M-6 / M-8: the key HUD's panel is up on the frame after contact too, but it starts its P4 there (KP-1b)
                    AutoDoorCheck(autoKey == null && (!inZone || (AutoKeyPanelUp && (keyHud != null || AutoKeyPanelIn >= 1f))),
                        "doors: key (instant): the cube is gone at once and the key panel is up " + (keyHud != null ? "(the key HUD: its P4 from the hand position, KP-1b, " + AutoKeyPanelIn.ToString("0.00") + ")" : "at full alpha") + (inZone ? "" : " (the player stands in another zone: panel not checked)"));
                    AutoDoorNext(5);
                    return;
                }
                var presenting = doorShot.Presenting(autoKeyZone);
                // HK M-6: with the key HUD the 3D key hides at the hand-off (KP-1), so its panel waits for that, not for PocketEnd
                var keyInView = presenting && autoKey != null && autoKey.gameObject.activeSelf;
                if (keyInView && AutoKeyPanelUp) autoKeyPanelEarly = true;
                if (rig != null) autoKeyMaxHudFade = Mathf.Max(autoKeyMaxHudFade, rig.HudFade + (rig.InShot ? 1f : 0f));
                while (autoDoorCapture < AutoKeyCaptureAt.Length && (doorShot.KeyClock >= AutoKeyCaptureAt[autoDoorCapture] || !presenting))
                    AutopilotCapture((autoShots < 10 ? "0" : "") + autoShots + "_key_pickup_t" + Mathf.RoundToInt(AutoKeyCaptureAt[autoDoorCapture++] * 100f).ToString("000"));
                if (!presenting && autoKeyGoneAt < 0f) autoKeyGoneAt = since;
                if (!keyInView && !autoKeyPanelSeen && AutoKeyPanelUp)
                {
                    autoKeyPanelSeen = true;
                    autoKeyPanelFirstAlpha = AutoKeyPanelIn;
                }
                if (since < 1.6f) return;
                var keySlack = Mathf.Max(.03f, autoDoorMaxDt);
                var arrive = FrontRoomsShotTimings.KeyPickup.Arrive;
                AutoDoorCheck(autoKeyLastPresented && Mathf.Abs(autoKeyLastLag - arrive) <= keySlack && keysTaken == autoKeyTakenBefore + 1,
                    "doors: key (presented): KeyTaken " + autoKeyLastLag.ToString("0.000") + " s after contact (" + arrive.ToString("0.00") + " ± " + keySlack.ToString("0.000") + ")");
                AutoDoorCheck(autoKeyPocketAt >= 0f && Mathf.Abs(autoKeyPocketAt - FrontRoomsShotTimings.KeyPickup.PocketSound) <= keySlack,
                    "doors: key (presented): the pocket beat at " + autoKeyPocketAt.ToString("0.000") + " s (" + FrontRoomsShotTimings.KeyPickup.PocketSound.ToString("0.00") + ")");
                AutoDoorCheck(autoKeyGoneAt >= 0f && Mathf.Abs(autoKeyGoneAt - FrontRoomsShotTimings.KeyPickup.PocketEnd) <= keySlack + autoDoorMaxDt && autoKey == null,
                    "doors: key (presented): gone at " + autoKeyGoneAt.ToString("0.000") + " s (" + FrontRoomsShotTimings.KeyPickup.PocketEnd.ToString("0.00") + "), the object destroyed");
                AutoDoorCheck(!autoKeyPanelEarly && (!inZone || (autoKeyPanelSeen && autoKeyPanelFirstAlpha < 1f - 1e-3f && AutoKeyPanelIn >= 1f)),
                    "doors: key (presented): the key panel waited for the pocket (the key HUD: for the hand-off) and came in (faded; the key HUD: its P4 flight) (first frame × " + autoKeyPanelFirstAlpha.ToString("0.00") + ", not at once; " + AutoKeyPanelIn.ToString("0.00") + " at the end)"
                    + (inZone ? "" : " (the player stands in another zone: fade not checked)"));
                AutoDoorCheck(autoKeyMaxHudFade <= 0f, "doors: key (presented): no rig shot and no HUD fade (" + autoKeyMaxHudFade.ToString("0.00") + ")");
                AutoDoorEnd();
                return;
            }
        }
    }

    // ---------- The EXIT scenario (-autopilotExit, editor only; C22 plan v2 step 4) ----------
    // The autopilot walks out of the stream rooms as always, then goes AutoExitFloors floors down. On each floor it
    // routes by world cell to N, the cell in front of the floor's EXIT door (breadth-first over built cells and
    // doors to N, or toward the built cell nearest N while N's chunk is still to be built), opens the EXIT door
    // with TryOpenDoor(N, X) as a tool does (the leaf must swing into X), lines up in front of the doorway and walks
    // through it toward X's middle; the game's own gate seals the EXIT behind it (D14), blacks the stairwell out and
    // changes the floor (D9, D11-D13), which AutopilotFloorStarted checks in the change's own frame. On the new
    // floor it waits in the stairwell for the north door (held until the map in sight is built, at most
    // StartDoorHoldLimit, D10), walks out north past StartDoorShutDistance, and checks that the door shut and
    // locked behind it and that the Relay, parked at the seal (D12), stayed dormant until then and was released
    // after it. From each release the Relay is held (HeldForTools) until the next change, so a catch cannot end
    // the walk: what this checks is the floors, not the chase. Each change gets a row in report.json (the EXIT, the
    // route, the seal's depth, the change's cost and its frame, the hold, the shut, the release; D16's numbers).
    // A failed check ends the scenario and fails the run, as any error in the log does.
    bool autoExitWanted, autoExitDone, autoExitLined, autoExitPlanChecked, autoExitOutShot;
    // A Relay baseline that asked for -autopilotExit: its floor plans the EXIT (the map as in play). Without it a
    // baseline plans none (AutoBaselineWithoutExit), so its map is the one the baselines were recorded on.
    bool autoBaselineExit;
    bool AutoBaselineWithoutExit => autoBaseline && !autoBaselineExit;
    int autoExitStep, autoExitCycles, autoExitSwapFrame = -1, autoExitFloorFrom, autoExitDistance;
    float autoExitClock, autoExitNextPlanAt, autoExitShutAt = -1f, autoExitBackOff;
    // The walk has reached the line-up point of the door to the route's next cell (AutoExitWalk).
    bool autoExitAtWaypoint;
    // Diagnostics in the log, a few lines a floor: each new plan's goal and length, and each time the walk was stuck.
    int autoExitNotes;
    // A26 (the station's floor-2 maps): edges the walk was stuck at twice running on this floor, such as an Office
    // kit's cubicle panels across an Open edge; its plans route round them. Cleared with each new floor.
    readonly HashSet<long> autoExitBlocked = new HashSet<long>();
    long autoExitStuckEdge = -1;
    GridCoord autoExitLastGoal;
    float autoExitRouteSeconds, autoExitSealDepthSeen, autoExitSwapMs, autoExitSwapFrameMs = -1f, autoExitShutSeconds;
    float autoExitSwapWorstMs, autoExitSwapFrameWorstMs, autoExitHoldMax;
    Vector3 autoExitRootBefore;
    GridCoord autoExitX;
    readonly List<string> autoExitChecks = new List<string>();
    readonly List<string> autoExitRows = new List<string>();
    const int AutoExitFloors = 3;
    // Game seconds the scenario may take in all, and for one floor's route to N.
    const float AutoExitPlaySeconds = 240f, AutoExitRouteLimit = 90f;
#if FRONTROOMS_RELAY_V2
    // V3a: each floor below the first waits for its debug Summon and the walk-in before routing on.
    float AutoExitPlayLimit => AutoExitPlaySeconds + AutoExitFloors * AutoOnMapLimitSeconds;
#else
    float AutoExitPlayLimit => AutoExitPlaySeconds;
#endif
    // Its steps, one floor at a time.
    const int ExitRoute = 0, ExitOpen = 1, ExitEnter = 2, ExitSeal = 3, ExitHold = 4, ExitOut = 5, ExitRelease = 6, ExitUnlock = 7;
    static readonly string[] AutoExitStepNames = { "route to N", "open the EXIT door", "into X", "seal and change", "north door hold", "walk out", "Relay release", "unlock the EXIT" };

    // From the step into X to the north door's opening no frame is captured: a capture's long frame would land in the change's numbers.
    bool AutoExitQuiet => autoExitWanted && !autoExitDone && autoExitStep >= ExitEnter && autoExitStep <= ExitHold;

    static string AutoF(float v, string format = "0.00") => v.ToString(format, CultureInfo.InvariantCulture);

    /// <summary>
    /// C23 step 1 as the player meets it in N: the map's EXIT sign stands over the door, LIT-locked and Lit, its
    /// bottom at ExitSignBottom (+- 0.005 m; 2.18 m on the steel frame's anchor since round 2), EmissionGain 2.0,
    /// no renderer of it casting a shadow, the end-on blade exactly when the plan saw a corridor; the EXIT leaf
    /// carries its crossbar (when the kit is there); and every lamp within ExitLampHoldCells of N (but X and cells
    /// with no lamp, and the start area while it stands) reads Steady.
    /// </summary>
    bool AutoExitSignReady(out string note)
    {
        var sign = map.ExitSign;
        var bottom = float.PositiveInfinity;
        var casters = 0;
        if (sign != null)
            foreach (var r in sign.GetComponentsInChildren<Renderer>(true))
            {
                bottom = Mathf.Min(bottom, r.bounds.min.y - map.transform.position.y);
                if (r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off) casters++;
            }
        var crossbar = map.ExitCrossbar != null || !FrontRoomsKitLibrary.Has(FrontRoomsMapWorld.ExitCrossbarKit);
        int lamps = 0, steady = 0;
        var n = map.ExitFrontCell;
        for (var dy = -FrontRoomsMapWorld.ExitLampHoldCells; dy <= FrontRoomsMapWorld.ExitLampHoldCells; dy++)
        for (var dx = -FrontRoomsMapWorld.ExitLampHoldCells; dx <= FrontRoomsMapWorld.ExitLampHoldCells; dx++)
        {
            var cell = new GridCoord(n.x + dx, n.y + dy);
            var mode = map.LampModeOf(cell);
            if (map.IsReserved(cell) || mode == ModuleLamp.Off) continue;
            lamps++;
            if (mode == ModuleLamp.Steady) steady++;
        }
        var end = map.ExitSignIsEnd;
        note = "the EXIT sign over the door: " + (sign == null ? "none" : sign.name + ", LockLit " + sign.LockLit + ", " + sign.State + ", bottom " + AutoF(bottom, "0.000") + " m, gain "
            + AutoF(sign.EmissionGain, "0.0") + ", " + casters + " shadow casters") + ", " + (end ? "end-on" : "flat") + " (the plan's corridor " + exitPlan.corridor + "); crossbar "
            + (map.ExitCrossbar != null ? "on the leaf" : "none") + "; the exit lamps Steady: " + steady + " of " + lamps;
        return sign != null && sign.LockLit && sign.State == FrontRoomsExitSign.Temperament.Lit && Mathf.Abs(bottom - FrontRoomsMapWorld.ExitSignBottom) <= .005f
            && Mathf.Approximately(sign.EmissionGain, FrontRoomsMapWorld.ExitSignEmissionGain) && casters == 0 && crossbar
            && end == exitPlan.corridor && lamps > 0 && steady == lamps;
    }

    void AutoExitCheck(bool ok, string what)
    {
        autoExitChecks.Add((ok ? "ok   " : "FAIL ") + what);
        Log("AUTOPILOT EXIT " + (ok ? "ok   " : "FAIL ") + what);
    }

    // A failed step ends the scenario (and the run, AutopilotTick).
    void AutoExitFail(string what)
    {
        AutoExitCheck(false, what);
        autoExitDone = true;
    }

    void AutoExitNext(int step)
    {
        autoExitStep = step;
        autoExitClock = 0f;
        autoExitLined = false;
        autoExitBackOff = 0f;
        autoExitAtWaypoint = false;
        if (step == ExitRoute)
        {
            autoExitPlanChecked = false;
            autoRoute.Clear();
            autoRouteIndex = 0;
            autoExitNextPlanAt = 0f;
        }
    }

    // The way into X: north from N, the cell in front of the EXIT door.
    Vector3 AutoExitNorth()
    {
        var north = map.CellCenter(map.ExitCell) - map.CellCenter(map.ExitFrontCell);
        north.y = 0f;
        return north.normalized;
    }

    /// <summary>The scenario's clock and checks, once a frame from AutopilotTick (before the game's play update).</summary>
    void AutopilotExit(float dt)
    {
        if (phase != Phase.Playing || playerRoot == null || map == null) return;
        autoExitClock += dt;
        var feet = playerRoot.position;
        var here = map.CellOf(feet);
        switch (autoExitStep)
        {
            case ExitRoute:
                // Floor 1: the usual walk out of the stream rooms first.
                if (floor <= 1 && inStartRooms) { autoExitClock = 0f; break; }
                if (!autoExitPlanChecked)
                {
                    autoExitPlanChecked = true;
                    AutoExitCheck(exitPlan.found && map.HasExitArea && map.ExitCell == exitPlan.exitCell && exitStairwell != null && exitStairwell.Cell == exitPlan.exitCell
                            && exitPlan.distance >= exitPlan.bandMin && exitPlan.distance <= exitPlan.bandMax,
                        "floor " + floor + " (" + FloorLabel(floor) + "): the EXIT is planned and reserved, its stairwell made: " + (exitPlan.found ? ExitDetail() : "none"));
                    if (!map.HasExitArea || exitStairwell == null) { autoExitDone = true; break; }
                }
                if (here == map.ExitFrontCell)
                {
                    autoExitRouteSeconds = autoExitClock;
                    AutoExitCheck(AutoExitSignReady(out var signNote), "floor " + floor + ": " + signNote);
                    var door = map.ExitDoor;
                    // M3: a floor-key floor's EXIT is locked from the start: E with the floor key turns it (the unlock shot).
                    if (map.FloorKeyRule && map.ExitLocked && door != null && !door.open)
                    {
                        AutoExitNext(ExitUnlock);
                        AutoExitUnlockBegin();
                        break;
                    }
                    var opened = door != null && map.TryOpenDoor(map.ExitFrontCell, map.ExitCell);
                    AutoExitCheck(door != null && (opened || door.open), "floor " + floor + ": routed by world cell to N " + map.ExitFrontCell + " in " + AutoF(autoExitClock, "0.0")
                        + " s; TryOpenDoor(N, X) opens the EXIT door (" + (door == null ? "not built" : opened ? "opening" : door.open ? "already open" : "refused") + ")");
                    if (door == null || !(opened || door.open)) { autoExitDone = true; break; }
                    AutoExitNext(ExitOpen);
                    break;
                }
                if (autoExitClock > AutoExitRouteLimit) AutoExitFail("floor " + floor + ": no way to N " + map.ExitFrontCell + " in " + AutoExitRouteLimit + " s (the walk stands in " + here + ")");
                break;
            case ExitOpen:
            {
                var door = map.ExitDoor;
                if (door != null && door.angle >= FrontRoomsMapWorld.DoorPassableDegrees)
                {
                    var leafDepth = door.leaf != null ? ExitDepth(door.leaf.bounds.center) : float.NegativeInfinity;
                    AutoExitCheck(leafDepth > .2f, "floor " + floor + ": the EXIT leaf swung into X, away from the opener in N (" + AutoF(door.angle, "0") + " deg, the leaf's middle "
                        + AutoF(leafDepth) + " m past the line)");
                    AutopilotCapture((autoShots < 10 ? "0" : "") + autoShots + "_exit_floor" + floor + "_door_open");
                    AutoExitNext(ExitEnter);
                }
                else if (autoExitClock > 3f) AutoExitFail("floor " + floor + ": the EXIT door did not open within 3 s of TryOpenDoor (" + (door == null ? "not built" : AutoF(door.angle, "0") + " deg") + ")");
                break;
            }
            case ExitEnter:
                if (floorSwap != FloorSwap.None)
                {
                    autoExitSealDepthSeen = ExitDepth(feet);
                    AutoExitCheck(map.ExitSealing && autoExitSealDepthSeen >= ExitSealDepth - 1e-3f && map.InExitArea(here),
                        "floor " + floor + ": a step into X sealed the EXIT behind the player at " + AutoF(autoExitSealDepthSeen) + " m past its line (the gate: " + AutoF(ExitSealDepth) + " m)");
                    autoExitRootBefore = map.transform.position;
                    autoExitX = map.ExitCell;
                    autoExitFloorFrom = floor;
                    autoExitDistance = exitPlan.distance;
                    AutoExitNext(ExitSeal);
                }
                else if (autoExitClock > 8f)
                    AutoExitFail("floor " + floor + ": no seal 8 s after the EXIT door opened (the player " + AutoF(ExitDepth(feet)) + " m past its line, in " + here + ")");
                break;
            case ExitSeal:
                // AutopilotFloorStarted moves it on in the change's frame.
                if (autoExitClock > 4f) AutoExitFail("floor " + floor + ": no change of floor within 4 s of the seal (ExitSealed " + map.ExitSealed + ", " + floorSwap + ")");
                break;
            case ExitHold:
                // The change's frame is the one before this: its length is this frame's delta.
                if (autoExitSwapFrameMs < 0f && Time.frameCount > autoExitSwapFrame)
                {
                    autoExitSwapFrameMs = Time.unscaledDeltaTime * 1000f;
                    autoExitSwapFrameWorstMs = Mathf.Max(autoExitSwapFrameWorstMs, autoExitSwapFrameMs);
                }
                if (floorDoorOpened)
                {
                    autoExitHoldMax = Mathf.Max(autoExitHoldMax, floorHeldFor);
                    AutoExitCheck(floorHeldFor <= StartDoorHoldLimit + .2f && startStairwell != null && startStairwell.NorthDoorOpen && startStairwell.NorthDoorSwing > 0f,
                        "floor " + floor + ": the north door held " + AutoF(floorHeldFor) + " s (" + (floorOpenedLate ? "the " + StartDoorHoldLimit + " s limit" : "the map in sight built") + "), then swung out of X, away from the player");
                    AutoExitNext(ExitOut);
                }
                else if (autoExitClock > StartDoorHoldLimit + 2f) AutoExitFail("floor " + floor + ": the north door did not open within " + (StartDoorHoldLimit + 2f) + " s of the change");
                break;
            case ExitOut:
                if (relay != null && relay.Released) { AutoExitFail("floor " + floor + ": the Relay was released before the start door had shut (" + relay.State + ")"); break; }
                if (!autoExitOutShot && startStairwell != null && startStairwell.NorthDoorAngle >= 60f && map.InStartArea(here))
                {
                    autoExitOutShot = true;
                    AutopilotCapture((autoShots < 10 ? "0" : "") + autoShots + "_exit_floor" + floor + "_north_door");
                }
                if (floorStartShut)
                {
                    autoExitShutAt = autoPlayClock;
                    autoExitShutSeconds = autoExitClock;
                    var outMetres = Vector2.Distance(Flat(feet), Flat(startDoorPoint));
                    AutoExitCheck((startStairwell == null || startStairwell.NorthDoorLocked) && outMetres >= StartDoorShutDistance - .05f,
                        "floor " + floor + ": walked out; the north door shut and locked " + AutoF(autoExitClock) + " s after it opened, the player " + AutoF(outMetres) + " m out");
                    // A bot-after-exit run: the walk ends at this shut (B1's), and the bot plays from here.
                    if (autoBotAfterExit) AutoBotAfterExitHandoff();
                    else AutoExitNext(ExitRelease);
                }
                else if (autoExitClock > 15f) AutoExitFail("floor " + floor + ": the north door did not shut within 15 s of opening (the player in " + here + ", " + AutoF(Vector2.Distance(Flat(feet), Flat(startDoorPoint))) + " m out)");
                break;
            case ExitUnlock:
                AutoExitUnlockTick();
                break;
            case ExitRelease:
#if FRONTROOMS_RELAY_V2
                // V3a: armed at the shut, on the map within the first dispatch (the debug Summon's).
                AutoExitReleaseV2();
                break;
#else
                if (relay != null && relay.Released)
                {
                    var after = autoPlayClock - autoExitShutAt;
                    AutoExitCheck(after >= hunterTuning.releaseDelaySeconds - .25f, "floor " + floor + ": the Relay, parked since the seal, stayed dormant until the start door shut and was released "
                        + AutoF(after) + " s after it (its delay " + AutoF(hunterTuning.releaseDelaySeconds, "0.0") + " s)");
                    // M3, plan 3, Q1 B: below a station floor, the floor's rules once the map round the start is built (every
                    // floor of the scenario, the last one too).
                    if (autoStationPlanned && floor >= 2) AutoStationBelowCheck();
                    // The onboarding (Red 2026-10-30): the run's first send, when it came on this floor, is the gentle one.
                    if (autoStationPlanned && floor >= 2 && relay.OnFirstSend) AutoFirstSendBelowRow();
                    autoExitRows.Add("floor " + autoExitFloorFrom + " -> " + floor + " (" + FloorLabel(floor) + ", seed " + floorSeed + "): EXIT X " + autoExitX + ", " + autoExitDistance
                        + " walking cells; route to N " + AutoF(autoExitRouteSeconds, "0.0") + " s; sealed at " + AutoF(autoExitSealDepthSeen) + " m into X; change " + AutoF(autoExitSwapMs, "0.0")
                        + " ms (its frame " + AutoF(autoExitSwapFrameMs, "0.0") + " ms); north door held " + AutoF(floorHeldFor) + " s" + (floorOpenedLate ? " (limit)" : "") + "; shut "
                        + AutoF(autoExitShutSeconds) + " s after opening; Relay released " + AutoF(after) + " s after the shut; tier " + tier + ", " + tierCells.Count + " cells, " + zonesVisited.Count + " zones");
                    // Looking back from the floor at the shut north door.
                    AutopilotLookFrom(startDoorPoint + AutoExitNorthOf(startDoorPoint) * 2.7f + Vector3.up * EyeHeight, startDoorPoint + Vector3.up * 1.3f,
                        "exit_floor" + floor + "_start_door_from_floor");
                    autoExitCycles++;
                    if (autoExitCycles >= AutoExitFloors) { autoExitDone = true; break; }
                    AutoExitNext(ExitRoute);
                }
                else if (autoExitClock > hunterTuning.releaseDelaySeconds + 8f) AutoExitFail("floor " + floor + ": the Relay was not released within " + (hunterTuning.releaseDelaySeconds + 8f) + " s of the start door's shut");
                break;
#endif
        }
        // From each release on, the Relay is held where it arrived (AutopilotFloorStarted lets the parked one go again).
        // Never in a bot-after-exit run: its Relay hunts and catches as in play.
        if (!autoBotAfterExit && relay != null && relay.Released && !relay.HeldForTools && autoExitStep != ExitRelease) relay.HeldForTools = true;
    }

    // North on the map at a point: the map is never rotated, but read it from the map all the same.
    Vector3 AutoExitNorthOf(Vector3 point)
    {
        var north = map.transform.forward;
        north.y = 0f;
        return north.normalized;
    }

    /// <summary>The scenario's walk (from AutopilotSteer): true when it steers this frame, false to leave it to the usual walk.</summary>
    bool AutoExitSteer(float dt, out Vector2 local, out bool sprint)
    {
        local = Vector2.zero;
        sprint = false;
        var feet = playerRoot.position;
        var here = map.CellOf(feet);
        switch (autoExitStep)
        {
            case ExitRoute:
            {
                // Floor 1: out of the stream rooms and past their open door's leaves, as every autopilot run walks.
                if (floor <= 1 && (inStartRooms || (here == startDoorCell && feet.z < startDoorPoint.z + 2.2f))) return false;
                if (!map.HasExitArea || here == map.ExitFrontCell) return true;
                autoStuckClock += dt;
                if (autoStuckClock > 2.5f)
                {
                    if ((feet - autoLastPosition).magnitude < .4f)
                    {
                        // Stuck (a leaf resting on the body, a corner of furniture): a step back, then a new plan.
                        AutoExitStuckNote(here);
                        AutoExitStuckEdge(here);
                        autoRouteIndex = autoRoute.Count;
                        autoExitBackOff = .6f;
                    }
                    autoLastPosition = feet;
                    autoStuckClock = 0f;
                }
                if (autoExitBackOff > 0f)
                {
                    autoExitBackOff -= dt;
                    local = new Vector2(0f, -1f);
                    return true;
                }
                if (autoRouteIndex >= autoRoute.Count && autoPlayClock >= autoExitNextPlanAt)
                {
                    AutoExitPlan(here, map.ExitFrontCell);
                    autoExitNextPlanAt = autoPlayClock + .5f;
                }
                AutoExitWalk(dt, here, out local);
                return true;
            }
            case ExitUnlock:
                AutoExitUnlockSteer(dt, feet, out local);
                return true;
            case ExitOpen:
            {
                // Facing the door while the leaf swings away, into X.
                var face = map.CrossingPoint(map.ExitFrontCell, map.ExitCell) - feet;
                face.y = 0f;
                yaw = Mathf.MoveTowardsAngle(yaw, Mathf.Atan2(face.x, face.z) * Mathf.Rad2Deg, 300f * dt);
                pitch = Mathf.MoveTowards(pitch, 0f, 60f * dt);
                return true;
            }
            case ExitEnter:
            {
                // Lined up 0.9 m in front of the doorway first, then straight through it toward X's middle and past
                // it (the seal comes at ExitSealDepth, 1.40 m in; X's middle is 1.5 m in).
                var north = AutoExitNorth();
                var cross = map.CrossingPoint(map.ExitFrontCell, map.ExitCell);
                var lineUp = cross - north * .9f;
                if (!autoExitLined && Flat(lineUp - feet).magnitude < .12f) autoExitLined = true;
                AutoSteerAt(dt, autoExitLined ? map.CellCenter(map.ExitCell) + north * .35f : lineUp, true, out local);
                return true;
            }
            case ExitOut:
            {
                if (startStairwell == null) return true;
                // The leaf first; then north out of the stairwell, across the door cell and into the cell beyond it,
                // to its middle (StartDoorShutDistance is 4 m; that middle is 4.5 m out).
                if (startStairwell.NorthDoorAngle < FrontRoomsMapWorld.DoorPassableDegrees && !floorStartShut) return true;
                var north = AutoExitNorthOf(feet);
                var door = startDoorCell;
                var ahead = new GridCoord(door.x, door.y + 1);
                // A map door on the door cell's far edge: only to the cell's middle, then AutoExitWalk lines up 1.3 m
                // out, opens it and goes through (1.2 m on would press the body against the shut leaf for good).
                var reach = map.DoorBetween(door, ahead) != null ? 0f : 1.2f;
                if (map.InStartArea(here) || (here == door && Vector3.Dot(feet - map.CellCenter(door), north) < reach - .2f))
                {
                    AutoSteerAt(dt, map.CellCenter(door) + north * reach, false, out local);
                    return true;
                }
                if (here == door)
                {
                    if (autoRouteIndex >= autoRoute.Count || autoRoute[autoRouteIndex] != ahead)
                    {
                        autoRoute.Clear();
                        autoRoute.Add(ahead);
                        autoRouteIndex = 0;
                        autoExitAtWaypoint = false;
                    }
                    if (!AutoExitWalk(dt, here, out local)) AutoSteerAt(dt, map.CellCenter(ahead), false, out local);
                    return true;
                }
                if (here == ahead) AutoSteerAt(dt, map.CellCenter(ahead) + north * .5f, true, out local);
                return true;
            }
            default:
                // The seal, the change, the hold and the release: standing.
                return true;
        }
    }

    /// <summary>
    /// The EXIT scenario's own walk along autoRoute (the bots' AutoWalkRoute goes straight at each crossing, and a
    /// leaf standing open into the walker's cell can stand between it and the doorway, pinning it in the corner of
    /// the wall and the leaf). Before any door, opened or shut, it lines up on the door's axis 1.3 m out, where no
    /// leaf of that door can reach (the leaf is 0.99 m from a hinge 0.5 m off the axis: the map's player clearance
    /// ends 1.355 m from the hinge); a shut door is opened from there (TryOpenDoor, as a tool), and the walk waits,
    /// facing it, until it is a way through; then straight through the middle into the next cell. False when the
    /// route is done or no longer leads anywhere (a plan is made again).
    /// </summary>
    bool AutoExitWalk(float dt, GridCoord here, out Vector2 local)
    {
        local = Vector2.zero;
        if (autoRouteIndex >= autoRoute.Count) return false;
        var next = autoRoute[autoRouteIndex];
        if (here == next)
        {
            autoRouteIndex++;
            autoExitAtWaypoint = false;
            return autoRouteIndex < autoRoute.Count;
        }
        if (Mathf.Abs(here.x - next.x) + Mathf.Abs(here.y - next.y) != 1 || !map.IsBuilt(here) || !map.IsBuilt(next))
        {
            autoRouteIndex = autoRoute.Count;
            return false;
        }
        var passage = map.PassageBetween(here, next);
        if (passage == FrontRoomsMapWorld.Passage.Wall || passage == FrontRoomsMapWorld.Passage.Glass)
        {
            autoRouteIndex = autoRoute.Count;
            return false;
        }
        var feet = playerRoot.position;
        var dir = map.CellCenter(next) - map.CellCenter(here);
        dir.y = 0f;
        dir.Normalize();
        var cross = map.CrossingPoint(here, next);
        if (map.DoorBetween(here, next) != null && !autoExitAtWaypoint)
        {
            var lineUp = cross - dir * 1.3f;
            if (Flat(lineUp - feet).magnitude > .25f)
            {
                AutoSteerAt(dt, lineUp, true, out local);
                return true;
            }
            autoExitAtWaypoint = true;
        }
        if (passage == FrontRoomsMapWorld.Passage.ClosedDoor)
        {
            if (map.TryOpenDoor(here, next)) autoDoorsOpened++;
            var face = cross - feet;
            face.y = 0f;
            yaw = Mathf.MoveTowardsAngle(yaw, Mathf.Atan2(face.x, face.z) * Mathf.Rad2Deg, 300f * dt);
            pitch = Mathf.MoveTowards(pitch, 0f, 60f * dt);
            return true;
        }
        AutoSteerAt(dt, cross + dir * .45f, false, out local);
        return true;
    }

    /// <summary>
    /// A route by world cell to N: breadth-first over built cells and doors (AutoBfs, never walls or glass, never
    /// into X, which is no map), to N when it is reached, else to the reached cell nearest N (N's chunk may not be
    /// built yet: the walk goes on as the map streams round it and plans again).
    /// </summary>
    void AutoExitPlan(GridCoord here, GridCoord front)
    {
        AutoBfs(here, 90, null, autoExitBlocked.Count == 0 ? null : (Func<GridCoord, GridCoord, bool>)((a, b) => !autoExitBlocked.Contains(MapGrid.EdgeId(a, b))));
        var goal = front;
        if (!autoDepth.ContainsKey(front))
        {
            goal = here;
            var best = int.MaxValue;
            foreach (var c in autoReached)
            {
                var d = Mathf.Abs(c.x - front.x) + Mathf.Abs(c.y - front.y);
                if (d < best) { best = d; goal = c; }
            }
        }
        if (goal != here) AutoRouteTo(here, goal);
        else { autoRoute.Clear(); autoRouteIndex = 0; }
        autoExitAtWaypoint = false;
        if (goal != autoExitLastGoal && autoExitNotes < 80)
        {
            autoExitNotes++;
            Log("AUTOPILOT EXIT plan floor " + floor + ": " + here + " -> " + goal + (goal == front ? " (N)" : " (nearest built cell to N " + front + ")") + ", " + autoRoute.Count + " steps, "
                + autoReached.Count + " cells reached");
        }
        autoExitLastGoal = goal;
    }

    // Stuck before the same edge twice running (furniture across it; the step back and a new plan did not help):
    // later plans on this floor go round it.
    void AutoExitStuckEdge(GridCoord here)
    {
        var next = autoRouteIndex < autoRoute.Count ? autoRoute[autoRouteIndex] : here;
        if (Mathf.Abs(next.x - here.x) + Mathf.Abs(next.y - here.y) != 1)
        {
            autoExitStuckEdge = -1;
            return;
        }
        var edge = MapGrid.EdgeId(here, next);
        if (edge == autoExitStuckEdge && autoExitBlocked.Add(edge))
            Log("AUTOPILOT EXIT floor " + floor + ": stuck twice before " + here + " -> " + next + " (" + map.PassageBetween(here, next) + "); later plans go round it");
        autoExitStuckEdge = edge;
    }

    // What held the walk up: where it stands, the next cell and what lies between, and that door's state.
    void AutoExitStuckNote(GridCoord here)
    {
        if (autoExitNotes >= 80) return;
        autoExitNotes++;
        var next = autoRouteIndex < autoRoute.Count ? autoRoute[autoRouteIndex] : here;
        var door = next != here ? map.DoorBetween(here, next) : null;
        Log("AUTOPILOT EXIT stuck floor " + floor + " at " + playerRoot.position.ToString("F2") + " in " + here + ", next " + next + " ("
            + (next != here ? map.PassageBetween(here, next).ToString() : "none") + (door != null ? ", door open " + door.open + " angle " + AutoF(door.angle, "0") + " pull " + door.pull : "")
            + "), route " + autoRouteIndex + "/" + autoRoute.Count + ", pull step " + pullClear.HasValue + ", Relay " + (relay != null ? relay.State + (relay.SeesPlayer ? " sees" : "") : "none"));
    }

    /// <summary>
    /// The run went one floor down (ChangeFloor, in the change's frame, C22): the walk's cells and route belong to
    /// the old floor (the same GridCoords name other cells now). In the EXIT scenario, the change is checked here:
    /// one floor down, the map root where it was, the old X now the start area with the player in it, the Relay
    /// parked (Dormant and hidden), the stairwell sealed and joined to the new map, the next EXIT planned; then the
    /// Relay is let go of, so its release on this floor can be seen.
    /// </summary>
    void AutopilotFloorStarted()
    {
        autoRoute.Clear();
        autoRouteIndex = 0;
        autoCellsTotal += autoCells.Count;
        autoCells.Clear();
        autoStuckClock = 0f;
        autoExitBlocked.Clear();
        autoExitStuckEdge = -1;
        if (playerRoot != null) autoLastPosition = playerRoot.position;
        if (!autoExitWanted || autoExitDone) return;
        if (relay != null) relay.HeldForTools = false;
        autoExitSwapFrame = Time.frameCount;
        autoExitSwapFrameMs = -1f;
        autoExitSwapMs = floorSwapMs;
        autoExitSwapWorstMs = Mathf.Max(autoExitSwapWorstMs, floorSwapMs);
        autoExitOutShot = false;
        var feetCell = map.CellOf(playerRoot.position);
        var rootKept = (map.transform.position - autoExitRootBefore).sqrMagnitude < 1e-6f;
        var parked = relay != null && relay.State == HunterState.Dormant && (hunter == null || !hunter.gameObject.activeSelf);
        var ok = autoExitStep == ExitSeal && floor == autoExitFloorFrom + 1 && rootKept && map.HasStartArea && map.InStartArea(autoExitX) && map.InStartArea(feetCell)
            && parked && startStairwell != null && startStairwell.Cell == autoExitX && startStairwell.Sealed && startStairwell.Joined
            && map.HasExitArea && map.ExitCell != autoExitX && exitStairwell != null && map.StreamFocus.HasValue;
        AutoExitCheck(ok, "floor " + autoExitFloorFrom + " -> " + floor + " (" + FloorLabel(floor) + ", seed " + floorSeed + "): changed in " + AutoF(floorSwapMs, "0.0") + " ms; root "
            + (rootKept ? "kept" : "MOVED") + "; old X " + autoExitX + " is the start area" + (map.InStartArea(feetCell) ? " with the player in it" : ", the player OUT of it (" + feetCell + ")")
            + "; Relay " + (relay != null ? relay.State.ToString() : "none") + (parked ? ", parked" : ", NOT parked") + "; stairwell " + (startStairwell != null && startStairwell.Sealed ? "sealed" : "NOT sealed")
            + (startStairwell != null && startStairwell.Joined ? " and joined" : ", NOT joined") + "; next EXIT " + (map.HasExitArea ? map.ExitCell.ToString() : "none"));
        if (autoExitStep == ExitSeal) AutoExitNext(ExitHold);
        else AutoExitFail("floor " + floor + ": the floor changed during the step '" + AutoExitStepNames[autoExitStep] + "'");
    }

    // ---------- A bot after the EXIT (-autopilotBotAfterExit with -autopilotBot <mode>; 关卡设计调研, 2026-10-30) ----------
    // Since A40 (Red 10-30: no chase during floor 1's onboarding) a quick player meets the Relay first on B1, where a
    // baseline's bot never gets (it never takes the EXIT on purpose). In this mode floor 1 is played by the EXIT walker
    // (the scenario's own steps: the station's floor key, the floor-1 EXIT's unlock, into X, the change, B1's north door
    // held, the walk out) with the Relay never held: when the onboarding's gate comes on floor 1 first, the walk goes on
    // and the Relay may hunt or catch. At B1's start-door shut the walk ends (autoExitDone) and AutoBotSteer plays the
    // mode; from then on the EXIT stays closed (ExitSealAllowed). The run ends as the bot's does: caught, or its budget,
    // which counts from the run's first send (AutoBaseTick, from the Relay's first Arrived) wherever that lands; never by
    // "exits". The walker's rows that still hold here (the EXIT planned, its sign, the unlock, the door, the seal, the
    // change, the north door's hold, the shut) fail the run when one fails (as an error, AutoBotAfterExitFinish); the
    // Relay's release row and the scenario's floors 2 and 3 are not walked; its "did not finish" is no failure here.
    // Recorded (log, the baseline's bot notes, exitChecks, floorRows): the handoff, and the floor, time and metres of the
    // run's first send.
    bool autoBotAfterExit, autoBotHandedOff;
    int autoBotHandoffFloor = -1, autoBotSendFloor = -1;
    float autoBotHandoffAt = -1f, autoBotSendAt = -1f, autoBotSendStraight = -1f, autoBotSendWalk = -1f;
    string autoBotSendWhen = "", autoBotSendGate = "";

    // The run has started (StartRunInPlace, after the baseline's own hooks): the first Arrived is recorded after the baseline's.
    void AutoBotAfterExitRunStarted()
    {
        if (autoBotAfterExit && relay != null) relay.Arrived += AutoBotAfterExitArrived;
    }

    // The Relay's first Arrived is the run's first send (AutoBaseArrived has just taken it as the release).
    void AutoBotAfterExitArrived(Vector3 feet, string tag)
    {
        if (autoBotSendFloor >= 0 || autoFinished || playerRoot == null) return;
        autoBotSendFloor = floor;
        autoBotSendAt = autoPlayClock;
        autoBotSendStraight = Vector3.Distance(feet, playerRoot.position);
        autoBotSendWalk = autoReleaseWalk;
        autoBotSendWhen = autoBotHandedOff ? AutoF(autoPlayClock - autoBotHandoffAt, "0.0") + " s after the handoff, the bot playing"
            : "before the handoff, the walker at '" + AutoExitStepNames[autoExitStep] + "'" + (autoExitDone ? " (done)" : "");
        autoBotSendGate = floor == 1 ? (stationSendDue ? "floor 1's gate (" + StationGateName(stationSendWhy) + ")" : "floor 1's release") : "the floor's release after its shut";
        var line = "botAfterExit: true · the run's first send (the Relay's first Arrived) on floor " + floor + " (" + FloorLabel(floor) + ") at " + AutoF(autoPlayClock, "0.0")
            + " s play, " + AutoF(autoBotSendStraight, "0.0") + " m straight, " + AutoF(autoBotSendWalk, "0.0") + " m walk, " + autoBotSendWhen + "; by " + autoBotSendGate
            + (floor == 1 && !autoBotHandedOff ? "; it came before the walker left floor 1: the walk goes on, the Relay not held" : "");
        Log("AUTOPILOT BOT AFTER EXIT · " + line);
        AutoBotNote(line);
    }

    /// <summary>B1's start-door shut (ExitOut): the walk's floor row, the floor below the station's row, and the bot's turn.</summary>
    void AutoBotAfterExitHandoff()
    {
        autoExitCycles++;
        // Plan 3, Q1 B: the floor below the station floor, at the shut (the EXIT run writes it at the release; no floor or
        // zone key is built on it either way).
        if (autoStationPlanned) AutoStationBelowCheck();
        autoExitRows.Add("floor " + autoExitFloorFrom + " -> " + floor + " (" + FloorLabel(floor) + ", seed " + floorSeed + "): EXIT X " + autoExitX + ", " + autoExitDistance
            + " walking cells; route to N " + AutoF(autoExitRouteSeconds, "0.0") + " s; sealed at " + AutoF(autoExitSealDepthSeen) + " m into X; change " + AutoF(autoExitSwapMs, "0.0")
            + " ms (its frame " + AutoF(autoExitSwapFrameMs, "0.0") + " ms); north door held " + AutoF(floorHeldFor) + " s" + (floorOpenedLate ? " (limit)" : "") + "; shut "
            + AutoF(autoExitShutSeconds) + " s after opening; bot-after-exit: the bot " + AutoBotNames[(int)autoBot] + " plays from this shut (the Relay never held; its release here is the bot run's)"
            + "; tier " + tier + ", " + tierCells.Count + " cells, " + zonesVisited.Count + " zones");
        autoExitDone = true;
        autoBotHandedOff = true;
        autoBotHandoffFloor = floor;
        autoBotHandoffAt = autoPlayClock;
        // The bot plans its own routes from here.
        autoRoute.Clear();
        autoRouteIndex = 0;
        autoStuckClock = 0f;
        if (playerRoot != null) autoLastPosition = playerRoot.position;
        var line = "botAfterExit: true · handoff on floor " + floor + " (" + FloorLabel(floor) + ") at " + AutoF(autoPlayClock, "0.0") + " s play, at the start door's shut: the bot "
            + AutoBotNames[(int)autoBot] + " plays from here; the Relay " + (relay == null ? "none" : relay.State + (relay.Released ? ", on the map" : ", not sent on this floor yet"))
            + "; the run's first send " + (autoBotSendFloor >= 0 ? "came on floor " + autoBotSendFloor + " at " + AutoF(autoBotSendAt, "0.0") + " s" : "not yet");
        Log("AUTOPILOT BOT AFTER EXIT · " + line);
        AutoBotNote(line);
    }

    /// <summary>Once a frame (AutopilotTick): the run's first send below floor 1, as the EXIT run's release step writes its row.</summary>
    void AutoBotAfterExitTick()
    {
        if (autoBotHandedOff && !autoFirstSendBelow && autoStationPlanned && autoBotSendFloor >= 2 && floor >= 2 && relay != null && relay.Released && relay.OnFirstSend)
            AutoFirstSendBelowRow();
    }

    /// <summary>
    /// The finish (AutopilotFinish, before the report takes the error count): the scenario's "did not finish" is the
    /// run's end here (a catch or the bot's budget, before B1's shut); the mode's summary row; and when one of the
    /// walker's rows failed, an error, so the baseline (AutoBaseFinish) and the run fail with it.
    /// </summary>
    void AutoBotAfterExitFinish(string reason)
    {
        if (!autoBotHandedOff && !autoExitDone)
            AutoExitCheck(true, "exit (bot-after-exit): the run ended by " + reason + " on floor " + floor + " (" + FloorLabel(floor) + ") at the walker's step '" + AutoExitStepNames[autoExitStep]
                + "', before its handoff at B1's shut; not a failure in this mode (a catch or the bot's budget ends it as a bot run)");
        var failed = autoExitChecks.FindAll(c => !c.StartsWith("ok", StringComparison.Ordinal));
        var sent = relay != null && relay.FirstSendDone;
        var s = sent ? relay.LastFirstSend : default;
        var line = "botAfterExit: true, bot " + AutoBotNames[(int)autoBot] + "; handoff " + (autoBotHandedOff ? "on floor " + autoBotHandoffFloor + " (" + FloorLabel(autoBotHandoffFloor) + ") at "
                + AutoF(autoBotHandoffAt, "0.0") + " s play" : "never (the walk " + (autoExitDone ? "failed" : "was still on") + ")") + "; the run's first send "
            + (autoBotSendFloor >= 0 ? "on floor " + autoBotSendFloor + " (" + FloorLabel(autoBotSendFloor) + ") at " + AutoF(autoBotSendAt, "0.0") + " s play by " + autoBotSendGate + ", " + autoBotSendWhen
                + ", " + AutoF(autoBotSendStraight, "0.0") + " m straight, " + AutoF(autoBotSendWalk, "0.0") + " m walk" + (sent ? ", rung " + s.rung + " (" + s.cells + " walking cells, " + FirstSendWText(s) + ")" : "")
                : "never came") + "; ended by " + reason + " on floor " + floor + " at " + AutoF(autoPlayClock, "0.0") + " s play; the walker's rows: " + (failed.Count == 0 ? "all ok" : failed.Count + " FAILED");
        AutoExitCheck(failed.Count == 0, "bot-after-exit: " + line);
        Log("AUTOPILOT BOT AFTER EXIT · " + line);
        if (failed.Count > 0)
            Debug.LogError("[FrontRooms3D] AUTOPILOT BOT AFTER EXIT FAIL · " + failed.Count + " of the EXIT walker's rows failed, so the bot did not play the run this mode claims: "
                + string.Join(" | ", failed));
    }

    // ---------- A26: floor 1's station (plan 6 step 4) ----------
    // With a station on floor 1 (a profile module, or -autopilotStation's stand-in) the walk checks it as the player
    // meets it, one row each, ok or FAIL (report.json's stationChecks; one FAIL fails the run): the plan, the start
    // door's hold, the EXIT sign in view once the door's leaves are open, the TV and the placard in the opening's
    // frustum, and the shut. Nothing here changes the run.
    bool autoStationPlanned, autoStationOpened, autoStationSight, autoStationShut;
    // Step 5 (M7, M8): the first send's row written; the Relay's state and listen point as ReleaseNow left them
    // (UpdateStationSend), before any tick.
    bool autoStationSent;
    HunterState autoStationLeg;
    Vector3? autoStationLegPoint;
    readonly List<string> autoStationChecks = new List<string>();
    // The sight rays' stream-room points, metres behind the door line, and the frustum's half-angle (a 60 degree frustum).
    static readonly float[] AutoStationBehind = { 2f, 6f, 16f };
    const float AutoStationFrustumHalfDeg = 30f;
    // M3 / M6: the walk takes the floor key by E once its feet are this close to the KeySpot (metres, flat), standing
    // AutoStationTakeStand off the key, facing it.
    const float AutoStationKeyReach = 3f, AutoStationTakeStand = 1.1f;
    // The take's stage: 0 not yet; 1 walking to the stand; 2 facing the key; 3 E pressed (the row the next frame); 4 done.
    int autoStationTake;
    Transform autoStationKeyGo;
    Vector3 autoStationStand;
    float autoStationTakeStartedAt, autoStationFacingSince, autoStationTakePressAt;
    int autoStationTakeFrame = -1, autoStationTakeHeard, autoStationTakeHeardFrame = -1;
    Vector3 autoStationTakeHeardAt;
    HunterState autoStationTakeRelay;
    bool autoStationPanelRow;
    // The autopilot's E press for one frame (EDown, EHeld): the floor key's take and the EXIT's unlock.
    int autoKeyEFrame = -1;
    // The floor-1 EXIT's unlock by the floor key (ExitUnlock): 0 walking to the stand, 1 facing the leaf, 2 E pressed;
    // the stand, the clocks, what the aim read at the press, the shot seen, ExitLockChanged (door, false) and DoorUnlocked
    // heard (the shot's clock at the first, the key and whether the lock had lifted at the second), and the Unlock shots
    // the floor key started at the EXIT (AutopilotFinish's keys-off line allows exactly those).
    int autoExitUnlockStage, autoExitLockLifts, autoExitLockSets, autoExitUnlocks, autoStationUnlockShots;
    float autoExitUnlockFacingSince, autoExitUnlockPressAt, autoExitLockClock = -1f;
    bool autoExitUnlockShotSeen, autoExitUnlockAfterLift, autoExitUnlockHooked;
    GridCoord autoExitUnlockZone;
    string autoExitUnlockAim = "";
    // M5 (AutoStationRoute): 0 waiting for the field's first build, 1 the start's row written, 2 the take seen (the
    // route before it kept), 3 the flip's row written; the field's revision and the frame at the take, and the row's
    // first half.
    int autoRouteStage, autoRouteTakeRevision, autoRouteTakeFrame;
    string autoRouteBefore = "";

    void AutoStationCheck(bool ok, string what)
    {
        autoStationChecks.Add((ok ? "ok   " : "FAIL ") + what);
        Log("AUTOPILOT STATION " + (ok ? "ok   " : "FAIL ") + what);
    }

    /// <summary>
    /// M5 (MAP_ROUTE_SPEC 1, 5; A20), once a frame on floor 1 with a station: the route at the start, once the field has
    /// built with the TV's cell built and reached (its target the TV's cell, the map's StationBeaconCell on StationBeaconEdge, taken from the map's station;
    /// BeaconPath from D reaches it, its metres logged); and the flip at the floor key's take, once the field has rebuilt
    /// after it (the key taken, the target N, BeaconPath off; the metres before and after, from the player's cell).
    /// </summary>
    void AutoStationRoute(StartGroupLayout l)
    {
        var driver = FrontRoomsWayCueDriver.Current;
        var way = driver != null && driver.World == map ? driver.Way : null;
        var here = map.CellOf(playerRoot.position);
        if (autoRouteStage == 0)
        {
            // The start: the first build that holds the TV's cell built and reached by the target pass (the map builds round
            // the start door over the first frames; until the TV's chunk is in, the field routes by the frontier rule).
            if (way == null || way.Builds == 0 || !map.IsBuilt(map.StationBeaconCell) || way.RegionAt(map.StationBeaconCell) != 1)
            {
                if (autoPlayClock < 5f) return;
                autoRouteStage = 1;
                AutoStationCheck(false, "M5 route at the start: no wall-cue field " + (way == null ? "bound to this map" : way.Builds == 0 ? "built"
                    : "with the TV's cell " + map.StationBeaconCell + " built and reached (built " + map.IsBuilt(map.StationBeaconCell) + ", region " + way.RegionAt(map.StationBeaconCell) + ")") + " 5 s after Space");
                return;
            }
            autoRouteStage = 1;
            var tv = map.StationBeaconCell;
            var edge = map.StationBeaconEdge;
            var beacon = way.BeaconPath(l.door, out var metres, out var shut, out var portal);
            var tvDirOk = way.Route(tv, out var tvDir) ? tvDir == edge : edge < 0;
            var ok = driver.StationFromMap && way.Station == tv && !way.StationKeyTaken && way.TargetIsStation && way.Target == tv && tvDirOk
                && beacon && metres > 0f && !float.IsInfinity(metres) && way.Route(l.door, out _);
            AutoStationCheck(ok, "M5 route at the start (" + AutoF(autoPlayClock) + " s after Space, field build " + way.Builds + "): the target is the station TV's cell " + tv + " ("
                + (way.TargetIsStation ? "station" : "NOT the station") + ", the map's station " + driver.StationFromMap + ", key taken " + way.StationKeyTaken + "), its dir " + tvDir
                + " at the TV's wall edge " + edge + "; BeaconPath from D " + l.door + " " + (beacon ? AutoF(metres, "0.0") + " m, " + shut + " shut openings, portal "
                + map.transform.InverseTransformPoint(portal).ToString("F1") : "NONE") + "; the field's metres from D " + AutoF(way.MetresAt(l.door), "0.0") + ", to N "
                + l.front + " once the key is taken");
        }
        if (autoRouteStage == 1 && autoStationTake >= 3)
        {
            autoRouteStage = 2;
            autoRouteTakeRevision = way != null ? way.Revision : -1;
            autoRouteTakeFrame = Time.frameCount;
            var metres = float.PositiveInfinity;
            var beacon = way != null && way.BeaconPath(here, out metres, out _, out _);
            autoRouteBefore = "before the take (" + (autoStationTake == 3 ? "the press frame" : "the take already in") + ") the target " + (way == null ? "none" : (way.TargetIsStation ? "the TV " : "N ")
                + way.Target) + ", BeaconPath from the player's " + here + " " + (beacon ? AutoF(metres, "0.0") + " m" : "none");
        }
        if (autoRouteStage == 2 && autoStationTake == 4 && (way == null || way.Revision != autoRouteTakeRevision || Time.frameCount > autoRouteTakeFrame + 30))
        {
            autoRouteStage = 3;
            var beacon = way != null && way.BeaconPath(here, out _, out _, out _);
            var ok = way != null && way.StationKeyTaken && !way.TargetIsStation && way.HasTarget && way.Target == l.front && !beacon && way.Revision != autoRouteTakeRevision
                && way.Route(here, out _);
            AutoStationCheck(ok, "M5 route flip at the take: " + autoRouteBefore + "; after it (" + (Time.frameCount - autoRouteTakeFrame) + " frames, field build "
                + (way != null ? way.Builds.ToString() : "-") + ") key taken " + (way != null && way.StationKeyTaken) + ", the target " + (way == null ? "none" : (way.TargetIsStation ? "the TV " : "N ")
                + way.Target) + " (N " + l.front + "), BeaconPath " + (beacon ? "STILL ON" : "off") + ", the field's metres from the player's " + here + " to N "
                + (way != null ? AutoF(way.MetresAt(here), "0.0") : "-"));
        }
    }

    /// <summary>
    /// Once a frame on floor 1 with a station (from AutopilotTick): the plan, once (the map holds it, FloorHasStation,
    /// the start door opens onto D at chunk-local (5, 2) or (5, 6), k = 4, the EXIT is the plan's X, H + k walking
    /// cells away); the start door's hold when it opens (at most StartDoorHoldLimit, the station's props in); one
    /// DoorOpenSeconds later, with the leaves open, the EXIT sign in view and the TV and the placard in the opening's
    /// frustum; and the shut (the player in the station when the leaves are back in the frame).
    /// </summary>
    void AutopilotStation()
    {
        if (playerRoot == null || map == null) return;
        var l = stationPlan.layout;
        if (!autoStationPlanned)
        {
            autoStationPlanned = true;
            var o = MapGrid.ChunkOrigin(l.DoorChunk);
            var local = new GridCoord(l.door.x - o.x, l.door.y - o.y);
            var walk = l.station.h + l.corridor;
            var ok = map.HasStation && map.FloorHasStation && startDoorCell == l.door && local.x == 5 && (local.y == 2 || local.y == 6)
                && l.corridor == StartGroupTemplate.CorridorCells && l.ExitChunk.x == l.DoorChunk.x && l.ExitChunk.y == l.DoorChunk.y + 1
                && exitPlan.found && exitPlan.exitCell == l.exit && map.HasExitArea && map.ExitCell == l.exit && exitPlan.distance == walk;
            AutoStationCheck(ok, "planned: H" + l.station.h + " k" + l.corridor + ", root ring " + stationPlan.ring + " (" + stationPlan.zoneRoots + " zone roots tried), D " + l.door
                + " chunk-local (" + local.x + ", " + local.y + ") = the start door " + startDoorCell + "; EXIT X " + exitPlan.exitCell + " from N " + exitPlan.frontCell + ", "
                + exitPlan.distance + " walking cells (H + k = " + walk + "); FloorHasStation " + map.FloorHasStation + "; plan " + AutoF(stationPlan.planMs, "0.0") + " ms, START "
                + AutoF(startRunMs, "0.0") + " ms; hash " + stationPlan.mapCarve.Hash);
        }
        if (!autoStationOpened && startDoorOpened)
        {
            autoStationOpened = true;
            var set = map.Station;
            AutoStationCheck(startDoorHeldFor <= StartDoorHoldLimit + .05f && map.StationReady, "the start door opened " + AutoF(autoPlayClock) + " s after Space, held "
                + AutoF(startDoorHeldFor) + " s (at most " + AutoF(StartDoorHoldLimit, "0.0") + "), the map round it ready " + map.ReadyAround(map.BuildRadius) + ", the station's props in "
                + map.StationReady + (set != null ? " (" + set.PropsPlaced + " placed, " + set.PropsLeftOutCount + " left out)" : ""));
        }
        if (!autoStationShut && streamFade >= 0f)
        {
            autoStationShut = true;
            var feet = playerRoot.position;
            var cell = map.CellOf(feet);
            AutoStationCheck(map.InStation(cell), "the start door shut " + AutoF(autoPlayClock - autoStartDoorAt) + " s after it opened, the player in the station: cell " + cell + " ("
                + (map.InStation(cell) ? "in" : "OUT of") + " " + l.station.x + ".." + (l.station.x + l.station.w - 1) + " x " + l.station.y + ".." + (l.station.y + l.station.h - 1) + "), "
                + AutoF(Vector3.Dot(feet - startDoorPoint, AutoExitNorthOf(feet))) + " m past the door line");
        }
        // The leaves stand open DoorOpenSeconds after the door starts to open; X's chunk (the sign) may come a moment later.
        if (!autoStationSight && autoStartDoorAt >= 0f && autoPlayClock - autoStartDoorAt >= FrontRoomsRoomStream.DoorOpenSeconds + .1f
            && (map.ExitSign != null || autoPlayClock - autoStartDoorAt > 4f))
        {
            autoStationSight = true;
            AutoStationSight(map.ExitSign);
            AutoStationFrustum();
        }
        // M3 / M6: the floor key taken by E as the walk passes the KeySpot on its way to E1, before the first send, as a
        // player who reads the room would (AutoStationTakeBegin, then AutoStationTakeStep each frame; the stand and the
        // facing are AutoStationTakeSteer's).
        if (autoStationTake == 0 && relay != null && !relay.Released && map.StationAnchor(ModuleMarkerKind.KeySpot, out var keyAt, out var keyTurn, out _)
            && Vector2.Distance(Flat(playerRoot.position), Flat(keyAt)) <= AutoStationKeyReach)
            AutoStationTakeBegin(keyAt, keyTurn);
        if (autoStationTake >= 1 && autoStationTake <= 3) AutoStationTakeStep(l);
        if (autoStationTake == 4 && !autoStationPanelRow) AutoStationPanelStep();
        // M5: the route at the start and its flip at the take.
        AutoStationRoute(l);
        // M7 / M8: the first send, the frame after it (the game sends in its update, after this).
        if (!autoStationSent && relay != null && relay.Released) AutoStationSend(l);
    }

    /// <summary>
    /// M7 / M8 (plan 6 step 5; the onboarding, Red 2026-10-30): floor 1's first send's row. The gate (Explored: at least
    /// StationExploreCells cells of the floor explored; or StationSendSeconds after the start door shut; under the tools'
    /// -autopilotOnboardOff M7's old Left, at least StationLeaveCells path cells out, or its 150 s), sent in the gate's
    /// frame; the rung, the arrival's walking cells and metres, its straight metres and side, unseen; the arrival outside
    /// the station and its ring, and (rungs 1-3) outside the strip and the band 2 cells round the walk D..N and X; rung 1
    /// within 8-10 cells on rung 1's side; the first leg a Hunt to the take point exactly when the key was taken, else
    /// Listen; the grace on (the run's first send, no W1 burst since).
    /// </summary>
    void AutoStationSend(StartGroupLayout l)
    {
        autoStationSent = true;
        var s = relay.LastFirstSend;
        var north = AutoExitNorthOf(stationSendFeet);
        var pastE1 = Vector3.Dot(stationSendFeet - map.CrossingPoint(l.e1In, l.e1Out), north);
        var beforeExit = Vector3.Dot(map.CrossingPoint(l.front, l.exit) - stationSendFeet, north);
        var gateCell = map.CellOf(stationSendFeet);
        var why = stationSendWhy;
        var left = why == FrontRoomsMapHunter.StationGate.Left;
        var explored = why == FrontRoomsMapHunter.StationGate.Explored;
        var shutFor = stationSendAt - startDoorShutAt;
        var gateOk = explored ? !OnboardOffForTools && stationSendExplored >= FrontRoomsMapHunter.StationExploreCells
            : why == FrontRoomsMapHunter.StationGate.Fallback ? startDoorShutAt >= 0f && shutFor >= FallbackSecondsNow - .05f
                && (OnboardOffForTools || stationSendExplored < FrontRoomsMapHunter.StationExploreCells)
            : left && OnboardOffForTools && stationSendPath >= FrontRoomsMapHunter.StationLeaveCells;
        var ringed = FrontRoomsExitPlanner.InAreaOrRing(l.station, s.cell);
        var banded = Mathf.Abs(s.cell.x - l.exit.x) <= 2 && Mathf.Abs(s.cell.y - l.exit.y) <= 2;
        foreach (var w in map.StationExitWalk) banded |= Mathf.Abs(s.cell.x - w.x) <= 2 && Mathf.Abs(s.cell.y - w.y) <= 2;
        var placeOk = !ringed && (s.rung == 4 || (!banded && !l.InStrip(s.cell))) && s.station && s.unseen;
        // C32's W fix (systems design 2026-10-30): rungs 1-2 stand at W at most 30 m (FirstSendWOk).
        var rungOk = s.rung >= 1 && s.rung <= 4 && (s.rung != 1 || (s.cells >= FrontRoomsStartGroup.Rung1Min && s.cells <= FrontRoomsStartGroup.Rung1Max && s.farSide)) && FirstSendWOk(s);
#if FRONTROOMS_RELAY_V2
        // V3a: the send is backstage (Arrive); after the step-in's listen it hunts to the take point once the key is taken,
        // else to the gate's estimate.
        var legOk = s.hunt == map.StationKeyIsTaken && autoStationLeg == HunterState.Arrive
            && (!s.hunt || (autoStationLegPoint.HasValue && (autoStationLegPoint.Value - map.StationTakePoint).sqrMagnitude < 1e-6f));
#else
        var leg = s.hunt ? HunterState.Hunt : HunterState.Listen;
        var legOk = s.hunt == map.StationKeyIsTaken && autoStationLeg == leg
            && (!s.hunt || (autoStationLegPoint.HasValue && (autoStationLegPoint.Value - map.StationTakePoint).sqrMagnitude < 1e-6f));
#endif
        // The run's first send carries the grace (FirstSendGraceNow), and the hunter holds it.
        var graceOk = relay.OnFirstSend && relay.FirstSendDone && relay.ChaseGrace && FirstSendGraceNow();
        var side = FirstSendSide(s) + " (" + (float.IsNaN(s.margin) ? "the behind test" : "exit field margin "
            + (float.IsPositiveInfinity(s.margin) ? "past its reach" : AutoF(s.margin, "0.0") + " m")) + ")";
        var gateText = left ? "left the station (onboarding off, tools), the player " + stationSendPath + " path cells out in " + gateCell + (gateCell == l.Corridor(2) ? " (C2)" : "") + ", "
                + AutoF(pastE1) + " m past E1, " + AutoF(beforeExit) + " m before the EXIT door"
            : explored ? stationSendExplored + " cells of floor 1 explored (at least " + FrontRoomsMapHunter.StationExploreCells + "), the player in " + gateCell + ", " + stationSendPath + " path cells out"
            : AutoF(FallbackSecondsNow, "0") + " s after the start door shut (" + stationSendExplored + " cells explored)";
        AutoStationCheck(gateOk && stationSentAt - stationSendAt < .05f && placeOk && rungOk && legOk && graceOk,
            "first send (M7, onboarding" + (OnboardOffForTools ? " OFF" : "") + "): gate " + gateText + ", " + AutoF(stationSendAt, "0.0") + " s after Space ("
            + AutoF(shutFor, "0.0") + " s after the shut), key " + (map.StationKeyIsTaken ? "taken" : "not taken") + " (the key gates: " + OnboardOffForTools + "); sent "
            + AutoF(stationSentAt - stationSendAt) + " s after the gate; rung " + s.rung + " of 4 (rungs 1/2/3 offered " + s.rung1 + "/" + s.rung2 + "/" + s.rung3 + " cells), arrival "
            + s.cell + ", " + s.cells + " walking cells (" + AutoF(s.cells * MapGrid.CellSize, "0") + " m), " + AutoF(s.straight, "0.0") + " m straight, " + FirstSendWText(s) + "; "
            + RelayStageNowText() + "; " + side + ", "
            + (s.unseen ? "unseen" : "SEEN") + "; " + (placeOk ? "outside" : "INSIDE") + " the station, its ring" + (s.rung == 4 ? "" : " and the exit-walk band") + "; first leg "
            + (s.hunt ? "Hunt to the take point" : "Listen") + " (" + autoStationLeg + "); grace " + (relay.ChaseGrace ? "on" : "off") + " (" + (warnLamps != null ? warnLamps.BurstCount : 0)
            + " W1 bursts, " + firstSendBursts + " at the send)");
        // Plan 7 K3b: out of the station at the gate, the HUD names the zone (not LEVEL 0 / FRONT ROOMS) and it counted. The
        // Explored gate comes in a cell it counted, so out of the station too.
        if (left || explored)
            AutoStationCheck(!InFrontRooms && !roomText.text.Contains("FRONT ROOMS") && zonesVisited.Count >= 1, "the HUD at the send in " + gateCell + ": \"" + roomText.text + "\", \""
                + roomMetaText.text + "\" (out of the front rooms, " + zonesVisited.Count + " zones counted)");
    }

    // ---------- The onboarding (Red 2026-10-30): no send on floor 1 before its gate; the run's first send is gentle ----------
    // The EXIT run walks floor 1 in about 8 cells, so its first send comes on floor 2 by the generic ladder. These rows say so:
    // floor 1 left with no send (AutopilotFloorLeaving), and the run's first send below it (AutoFirstSendBelowRow).
    bool autoOnboardFloorRow, autoFirstSendBelow;

    /// <summary>
    /// The floor is about to change (ChangeFloor, before the floor's counts start again). Leaving floor 1 of a station run
    /// under the onboarding's rules: its row, ok when floor 1's gate never came and nothing was sent (explored under
    /// StationExploreCells, under StationSendSeconds since the start door shut), or when the gate came and the first send
    /// went out on floor 1.
    /// </summary>
    void AutopilotFloorLeaving()
    {
        if (floor != 1 || !autoStationPlanned || autoOnboardFloorRow || OnboardOffForTools || relay == null) return;
        autoOnboardFloorRow = true;
        var clock = StationFallbackClock;
        var held = !stationSendDue && !relay.FirstSendDone && floorExplored < FrontRoomsMapHunter.StationExploreCells && clock < FrontRoomsMapHunter.StationSendSeconds;
        var sent = stationSendDue && relay.FirstSendDone && autoStationSent;
        AutoStationCheck(held || sent, held
            ? "onboarding: no send on floor 1 (explored " + floorExplored + " < " + FrontRoomsMapHunter.StationExploreCells + " cells, " + AutoF(clock, "0.0") + " < "
                + AutoF(FrontRoomsMapHunter.StationSendSeconds, "0") + " s since the start door shut; key " + (map.StationKeyIsTaken ? "taken" : "not taken") + "); floor 1 left by the EXIT "
                + AutoF(elapsed, "0.0") + " s after Space, the Relay " + relay.State
            : "onboarding: floor 1's gate " + (stationSendDue ? "came (" + StationGateName(stationSendWhy) + ", " + stationSendExplored + " cells explored)" : "NEVER came") + " and the first send "
                + (relay.FirstSendDone ? "went out" : "did NOT go out") + " on floor 1 (explored " + floorExplored + ", " + AutoF(clock, "0.0") + " s since the shut)");
    }

    /// <summary>
    /// The run's first send below floor 1 (the player left it before the onboarding's gate; v1: released at the floor's
    /// delay, v2: the floor's first dispatch): the generic ladder's rung, its walking cells and metres, straight metres,
    /// behind the player for rung 1, unseen, kept out of a stairwell and the 2 cells round it; the grace on; nothing sent on
    /// floor 1.
    /// </summary>
    void AutoFirstSendBelowRow()
    {
        if (autoFirstSendBelow || relay == null) return;
        autoFirstSendBelow = true;
        var s = relay.LastFirstSend;
        var nearStair = false;
        for (var dy = -2; dy <= 2; dy++)
        for (var dx = -2; dx <= 2; dx++)
            nearStair |= map.InStairwell(new GridCoord(s.cell.x + dx, s.cell.y + dy));
        // C32's W fix (systems design 2026-10-30): rungs 1-2 also stand at W at most 30 m (FirstSendWOk).
        var rungOk = (s.rung == 1 ? s.cells >= FrontRoomsStartGroup.Rung1Min && s.cells <= FrontRoomsStartGroup.Rung1Max && s.farSide
            : s.rung == 2 ? s.cells >= FrontRoomsStartGroup.Rung2Min && s.cells <= FrontRoomsStartGroup.Rung2Max
            : s.rung == 3 ? s.cells >= FrontRoomsStartGroup.Rung3Min && s.cells <= FrontRoomsStartGroup.Rung3Max
            : s.rung == 4) && FirstSendWOk(s);
        var keptOut = !map.InStartArea(s.cell) && !map.InStairwell(s.cell) && (s.rung == 4 || !nearStair);
        var graceOk = relay.OnFirstSend && relay.ChaseGrace && FirstSendGraceNow();
        var ok = floor >= 2 && !autoStationSent && relay.FirstSendDone && !s.station && s.unseen && rungOk && keptOut && graceOk;
        AutoStationCheck(ok, "the run's first send on floor " + floor + " (" + FloorLabel(floor) + "; none on floor 1, onboarding): the generic ladder, rung " + s.rung
            + " of 4 (rungs 1/2/3 offered " + s.rung1 + "/" + s.rung2 + "/" + s.rung3 + " cells), arrival " + s.cell + ", " + s.cells + " walking cells (" + AutoF(s.cells * MapGrid.CellSize, "0")
            + " m), " + AutoF(s.straight, "0.0") + " m straight, " + FirstSendWText(s) + "; " + RelayStageNowText() + "; " + FirstSendSide(s) + ", " + (s.unseen ? "unseen" : "SEEN") + ", " + (keptOut ? "kept out of" : "IN or beside")
            + " the stairwells; grace " + (relay.ChaseGrace ? "on" : "off") + " (" + (warnLamps != null ? warnLamps.BurstCount : 0) + " W1 bursts, " + firstSendBursts + " at the send); first leg "
            + (s.hunt ? "Hunt" : "Listen") + "; " + AutoF(elapsed, "0.0") + " s after Space");
    }

    // ---------- M3 / M6 / R3: the floor key's take by E, and the floor-1 EXIT's unlock (notes/a26_group_m3_plan.md 6.2) ----------

    /// <summary>
    /// The floor key's take begins: the "Key · floor" lying at the KeySpot (NearestKeyForTools, FloorKeyZone) and a stand
    /// AutoStationTakeStand off it, in front of the KeySpot marker (its turn's forward), else toward the walk. No floor key
    /// there: a FAIL row, and the take is given as a tool would (AutoStationTakeFail).
    /// </summary>
    void AutoStationTakeBegin(Vector3 keyAt, Quaternion keyTurn)
    {
        autoStationTakeStartedAt = autoPlayClock;
        autoStationTake = 1;
        var key = map.NearestKeyForTools(keyAt, out var zone, out _);
        if (key == null || zone != FrontRoomsMapWorld.FloorKeyZone || key.name != FrontRoomsMapWorld.FloorKeyName || (key.position - keyAt).magnitude > .1f)
        {
            AutoStationTakeFail("no \"" + FrontRoomsMapWorld.FloorKeyName + "\" at the KeySpot " + keyAt.ToString("F2") + " (nearest key: "
                + (key == null ? "none" : key.name + " at " + key.position.ToString("F2")) + ")");
            return;
        }
        autoStationKeyGo = key;
        var feet = playerRoot.position;
        var flatKey = new Vector3(key.position.x, feet.y, key.position.z);
        var outward = keyTurn * Vector3.forward;
        outward.y = 0f;
        var stand = flatKey + outward.normalized * AutoStationTakeStand;
        if (outward.sqrMagnitude < 1e-4f || !AutoBodyClear(stand))
        {
            var toward = feet - flatKey;
            toward.y = 0f;
            stand = flatKey + (toward.sqrMagnitude > 1e-4f ? toward.normalized : Vector3.back) * AutoStationTakeStand;
        }
        autoStationStand = stand;
    }

    // The take's walk (from AutopilotSteer): to the stand, then standing, facing the key from the base eye.
    void AutoStationTakeSteer(float dt, out Vector2 local)
    {
        local = Vector2.zero;
        var feet = playerRoot.position;
        if (autoStationTake == 1)
        {
            if (Flat(autoStationStand - feet).magnitude > .12f && autoPlayClock - autoStationTakeStartedAt < 6f)
            {
                AutoSteerAt(dt, autoStationStand, true, out local);
                return;
            }
            autoStationTake = 2;
            autoStationFacingSince = autoPlayClock;
        }
        if (autoStationKeyGo == null) return;
        var eye = rig != null ? rig.BaseEye.position : cam.transform.position;
        var to = autoStationKeyGo.position - eye;
        var flat = new Vector2(to.x, to.z).magnitude;
        yaw = Mathf.MoveTowardsAngle(yaw, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, 300f * dt);
        pitch = Mathf.MoveTowards(pitch, Mathf.Atan2(-to.y, Mathf.Max(flat, .01f)) * Mathf.Rad2Deg, 120f * dt);
    }

    /// <summary>
    /// Once a frame while the take runs (AutopilotStation, before the play update): with the aim on the floor key for
    /// 0.25 s, E for one frame through UpdateAim (EXAMINE never opens under the autopilot, HarnessRunning, so the key's
    /// priority over a close-up box on the same ray, R3, is not exercised here); the frame after, the
    /// row: KeyPickupStarted and KeyTaken (FloorKeyZone) in the press frame, StationKeyTaken once in it at the KeySpot on
    /// the floor, FloorKeyHeld, StationKeyIsTaken (FloorHasStation still true), still Playing (no close-up), the Relay still
    /// Dormant, no zone key built on floor 1, the EXIT locked since the start.
    /// </summary>
    void AutoStationTakeStep(StartGroupLayout l)
    {
        if (autoStationTake == 1)
        {
            if (autoPlayClock - autoStationTakeStartedAt > 8f) AutoStationTakeFail("the walk never reached the stand " + autoStationStand.ToString("F2"));
            return;
        }
        if (autoStationTake == 2)
        {
            // A rebuild of the key's chunk while the walk stands there makes a new key object: the one lying now.
            if (autoStationKeyGo == null && map.StationAnchor(ModuleMarkerKind.KeySpot, out var spot, out _, out _))
            {
                var again = map.NearestKeyForTools(spot, out var againZone, out _);
                if (again != null && againZone == FrontRoomsMapWorld.FloorKeyZone) autoStationKeyGo = again;
            }
            var onKey = aimKind == FrontRoomsMapWorld.AimKind.TakeKey && aimed != null && autoStationKeyGo != null && aimed.gameObject == autoStationKeyGo.gameObject;
            if (onKey && autoPlayClock - autoStationFacingSince >= .25f)
            {
                autoStationTakeHeard = 0;
                autoStationTakeHeardFrame = -1;
                autoStationTakeRelay = relay != null ? relay.State : HunterState.Dormant;
                map.StationKeyTaken += AutoStationKeyHeard;
                autoKeyEFrame = autoStationTakeFrame = Time.frameCount;
                autoStationTakePressAt = autoPlayClock;
                autoStationTake = 3;
            }
            else if (autoPlayClock - autoStationFacingSince > 3f)
                AutoStationTakeFail("facing the key for 3 s, the aim never read TakeKey on it (aim " + aimKind + " on " + (aimed != null ? aimed.name : "nothing") + ")");
            return;
        }
        // 3: E was pressed the frame before; UpdateAim answered it (UseDoor -> Use -> TakeKey). D1.6 (contract v9 sec. 4): a
        // dressed key is presented first (KP-1), so KeyTaken comes at KeyPickup.Arrive and the object leaves at the hand-off
        // (HoldUntil); the row waits for both, at most HoldUntil + 0.25 s after E. The cube key still does both in the press frame.
        if (Time.frameCount <= autoStationTakeFrame) return;
        var fk = FrontRoomsMapWorld.FloorKeyZone;
        var goneNow = autoStationKeyGo == null || !autoStationKeyGo.gameObject.activeInHierarchy;
        if ((!autoKeyAnnounced.Contains(fk) || !goneNow) && autoPlayClock - autoStationTakePressAt <= FrontRoomsShotTimings.KeyPickup.HoldUntil + .25f) return;
        map.StationKeyTaken -= AutoStationKeyHeard;
        autoStationTake = 4;
        var startedInPress = autoKeyStartFrame.TryGetValue(fk, out var startFrame) && startFrame == autoStationTakeFrame;
        var announced = autoKeyAnnounced.Contains(fk) && autoKeyLastLag >= 0f && autoKeyLastLag <= FrontRoomsShotTimings.KeyPickup.HoldUntil;
        map.StationAnchor(ModuleMarkerKind.KeySpot, out var keyAt, out _, out var keyCell);
        var onFloor = new Vector3(keyAt.x, map.CellCenter(keyCell).y, keyAt.z);
        // C-1: the take is heard at StationTakeSpot, 0.5 m in front of the key along its facing, on the floor.
        var spotOk = map.StationTakeSpot(out var takeSpot);
        var heardOk = spotOk && autoStationTakeHeard == 1 && autoStationTakeHeardFrame == autoStationTakeFrame && (autoStationTakeHeardAt - takeSpot).sqrMagnitude < 1e-6f;
        var keyGone = autoStationKeyGo == null || !autoStationKeyGo.gameObject.activeInHierarchy;
#if FRONTROOMS_RELAY_V2
        // V3a: armed at the start door's shut, the Relay is Away at the take (off the map; the take is a device call held for
        // the gate), never placed by it: off the map before and after (Away, or Dormant if the take came before the shut).
        var dormant = (autoStationTakeRelay == HunterState.Dormant || autoStationTakeRelay == HunterState.Away) && relay != null && !relay.OnMap
            && (relay.State == HunterState.Dormant || relay.State == HunterState.Away);
#else
        var dormant = autoStationTakeRelay == HunterState.Dormant && relay != null && !relay.Released && relay.State == HunterState.Dormant;
#endif
        AutoCountKeys(out var zoneKeys, out var floorKeys);
        var take = map.transform.InverseTransformPoint(takeSpot) - new Vector3(l.station.x * MapGrid.CellSize, 0f, l.station.y * MapGrid.CellSize);
        var ok = startedInPress && announced && heardOk && keyGone && map.FloorKeyHeld && map.StationKeyIsTaken && map.FloorHasStation
            && phase == Phase.Playing && !FrontRoomsInspect.Active && dormant && zoneKeys == 0 && floorKeys == 0 && map.ExitLocked && stationTakeAt >= 0f;
        AutoStationCheck(ok, "the floor key taken by E (M3, M6): the aim read TakeKey on \"" + FrontRoomsMapWorld.FloorKeyName + "\" from the stand "
            + AutoF(Flat(autoStationStand - onFloor).magnitude) + " m off it; KeyPickupStarted in the press frame " + startedInPress + ", KeyTaken (the floor key) " + AutoF(autoKeyLastLag)
            + " s after it (at most " + AutoF(FrontRoomsShotTimings.KeyPickup.HoldUntil) + ") " + announced + ", StationKeyTaken " + autoStationTakeHeard + "x in it at the take spot 0.5 m in front of the key, on the floor (" + AutoF(take.x) + ", " + AutoF(take.z) + ") m in the room; FloorKeyHeld " + map.FloorKeyHeld
            + ", StationKeyIsTaken " + map.StationKeyIsTaken + ", FloorHasStation " + map.FloorHasStation + "; the key gone " + keyGone + "; phase " + phase + " (no close-up); the Relay " + autoStationTakeRelay + " -> "
            + (relay != null ? relay.State.ToString() : "none") + "; built on floor 1: " + zoneKeys + " zone keys, " + floorKeys + " floor keys left; ExitLocked " + map.ExitLocked + " since the start");
    }

    void AutoStationKeyHeard(Vector3 at)
    {
        autoStationTakeHeard++;
        autoStationTakeHeardFrame = Time.frameCount;
        autoStationTakeHeardAt = at;
    }

    // The take failed: a FAIL row; the floor key is then given as a tool would (held, and TakeStationKey's event), so the
    // rest of the walk (the send, the EXIT's unlock) still runs and is checked.
    void AutoStationTakeFail(string why)
    {
        map.StationKeyTaken -= AutoStationKeyHeard;
        AutoStationCheck(false, "the floor key's take by E (M3): " + why);
        map.GiveKeyForTools(FrontRoomsMapWorld.FloorKeyZone);
        map.TakeStationKey();
        autoStationTake = 4;
        autoStationPanelRow = true;
    }

    // KP-1b (R1): the key panel shows within 1.3 s of the take (the glyph's 0.5 s hold at the hand, then its 0.72 s flight).
    void AutoStationPanelStep()
    {
        var shown = keyHud != null ? keyHud.PanelShown : keyPanel != null && keyPanel.activeSelf;
        var after = autoPlayClock - autoStationTakePressAt;
        if (!shown && after <= 1.5f) return;
        autoStationPanelRow = true;
        AutoStationCheck(shown && after <= 1.4f && map.FloorKeyHeld, "the key panel after the take (KP-1b, STAIR KEY / OPENS THE STAIR DOOR, tag "
            + HudIdentity(FrontRoomsMapWorld.FloorKeyZone).number.ToString("00") + "): " + (keyHud != null ? "the key HUD's B panel" : "the map's panel")
            + (shown ? " shown " + AutoF(after) + " s after E" : " NOT shown 1.5 s after E") + " (at most 1.3 s)");
    }

    // The keys built under the map now: zone keys ("Key · zone ...") and floor keys ("Key · floor").
    void AutoCountKeys(out int zoneKeys, out int floorKeys)
    {
        zoneKeys = floorKeys = 0;
        foreach (var t in map.GetComponentsInChildren<Transform>(true))
        {
            // D1.6: a dressed key handed to the key HUD goes inactive at HoldUntil and is destroyed at PocketEnd; it is not lying.
            if (!t.gameObject.activeInHierarchy) continue;
            if (t.name.StartsWith("Key · zone", StringComparison.Ordinal)) zoneKeys++;
            else if (t.name == FrontRoomsMapWorld.FloorKeyName) floorKeys++;
        }
    }

    // Plan 3 (Q1 (a)) as Red's Q1 B (10-29 17:21) changed it: a floor below the station floor has the EXIT unlocked, no
    // floor key rule and no floor key, FloorHasStation false, and no zone keys (ZoneKeysOff on, 0 built). One row a floor.
    int autoStationBelowRows;
    void AutoStationBelowCheck()
    {
        autoStationBelowRows++;
        AutoCountKeys(out var zoneKeys, out var floorKeys);
        AutoStationCheck(!map.ExitLocked && !map.FloorKeyRule && !map.FloorHasStation && floorKeys == 0 && !map.HasKeyFor(FrontRoomsMapWorld.FloorKeyZone)
                && map.ZoneKeysOff && zoneKeys == 0,
            "floor " + floor + " (" + FloorLabel(floor) + "): below the station floor (plan 3, Q1 B): ExitLocked " + map.ExitLocked + ", FloorKeyRule " + map.FloorKeyRule
            + ", FloorHasStation " + map.FloorHasStation + ", floor keys built " + floorKeys + ", held " + map.HasKeyFor(FrontRoomsMapWorld.FloorKeyZone)
            + "; ZoneKeysOff " + map.ZoneKeysOff + ", " + zoneKeys + " zone keys built (0 wanted)");
    }

    // The stand for the EXIT's key turn: on the doorway's axis, this far south of its line, and the pitch at the leaf.
    const float AutoExitUnlockStand = 1.05f, AutoExitUnlockPitch = 12f;
    bool autoExitUnlockOk;

    // At N on a floor-key floor (the EXIT locked): the step's state, and the map's lock and unlock events heard from here
    // (hooked once a run: the map is the run's on every floor).
    void AutoExitUnlockBegin()
    {
        autoExitUnlockStage = 0;
        autoExitLockLifts = autoExitLockSets = autoExitUnlocks = 0;
        autoExitLockClock = -1f;
        autoExitUnlockShotSeen = autoExitUnlockAfterLift = autoExitUnlockOk = false;
        autoExitUnlockAim = "";
        if (autoExitUnlockHooked) return;
        autoExitUnlockHooked = true;
        map.ExitLockChanged += AutoExitLockChanged;
        map.DoorUnlocked += AutoExitDoorUnlocked;
    }

    void AutoExitLockChanged(FrontRoomsMapWorld.Door door, bool locked)
    {
        if (locked)
        {
            autoExitLockSets++;
            return;
        }
        autoExitLockLifts++;
        if (autoExitLockClock < 0f) autoExitLockClock = doorShot != null ? doorShot.Clock : -1f;
    }

    void AutoExitDoorUnlocked(FrontRoomsMapWorld.Door door, GridCoord zone, Vector3 keyhole)
    {
        if (!map.IsExitDoor(door)) return;
        autoExitUnlocks++;
        autoExitUnlockZone = zone;
        autoExitUnlockAfterLift = autoExitLockLifts > 0 && !map.ExitLocked;
    }

    // The unlock's walk (AutoExitSteer): to the stand on the doorway's axis, then facing the leaf; standing through the shot.
    void AutoExitUnlockSteer(float dt, Vector3 feet, out Vector2 local)
    {
        local = Vector2.zero;
        var north = AutoExitNorth();
        var cross = map.CrossingPoint(map.ExitFrontCell, map.ExitCell);
        if (autoExitUnlockStage == 0)
        {
            var stand = cross - north * AutoExitUnlockStand;
            if (Flat(stand - feet).magnitude > .12f && autoExitClock < 6f)
            {
                AutoSteerAt(dt, stand, true, out local);
                return;
            }
            autoExitUnlockStage = 1;
            autoExitUnlockFacingSince = autoPlayClock;
        }
        if (autoExitUnlockStage != 1) return;
        var face = cross - feet;
        face.y = 0f;
        yaw = Mathf.MoveTowardsAngle(yaw, Mathf.Atan2(face.x, face.z) * Mathf.Rad2Deg, 300f * dt);
        pitch = Mathf.MoveTowards(pitch, AutoExitUnlockPitch, 60f * dt);
    }

    /// <summary>
    /// ExitUnlock (M3, floor 1 with the floor key): with the aim on the EXIT's leaf reading LockedHaveKey for 0.25 s, the
    /// row's aim (the floor key's identity, held, "E  ·  UNLOCK", the key HUD's Locked state) and E for one frame; then
    /// the unlock shot (UseDoor -> KeyTurn -> BeginUnlock): ExitLockChanged(door, false) once at the commit (0.86 s on the
    /// shot's clock), DoorUnlocked once with the floor key after it, the leaf passable within 4 s of E; then ExitOpen.
    /// </summary>
    void AutoExitUnlockTick()
    {
        var door = map.ExitDoor;
        if (door == null)
        {
            AutoExitFail("floor " + floor + ": the locked EXIT door is not built at N");
            return;
        }
        if (autoExitUnlockStage == 0)
        {
            if (autoExitClock > 8f) AutoExitFail("floor " + floor + ": the walk never stood in front of the locked EXIT (8 s)");
            return;
        }
        if (autoExitUnlockStage == 1)
        {
            var onLeaf = aimed != null && map.DoorOf(aimed) == door;
            if (onLeaf && aimKind == FrontRoomsMapWorld.AimKind.LockedHaveKey && autoPlayClock - autoExitUnlockFacingSince >= .25f)
            {
                var words = map.Describe(aimed, out _);
                autoExitUnlockOk = aimKeyZone == FrontRoomsMapWorld.FloorKeyZone && aimKeyHeld && words == FrontRoomsMapWorld.UnlockPrompt && map.ExitLocked && map.FloorKeyHeld
                    && FrontRoomsKeyHud.AimOf(aimKind) == FrontRoomsKeyHud.Aim.Locked;
                autoExitUnlockAim = "Kind " + aimKind + ", key " + (aimKeyZone == FrontRoomsMapWorld.FloorKeyZone ? "the floor key" : aimKeyZone.ToString()) + " held " + aimKeyHeld
                    + ", \"" + words + "\", the key HUD's padlock state (" + (keyHud != null ? "showing " + keyHud.State + (keyHud.OwnsPrompt ? ", drawing its lines" : "") : "no key HUD") + ")";
                autoKeyEFrame = Time.frameCount;
                autoExitUnlockPressAt = autoPlayClock;
                autoExitUnlockStage = 2;
            }
            else if (autoPlayClock - autoExitUnlockFacingSince > 3f)
                AutoExitFail("floor " + floor + ": facing the locked EXIT for 3 s, the aim never read LockedHaveKey (aim " + aimKind + " on " + (aimed != null ? aimed.name : "nothing") + ")");
            return;
        }
        if (rig != null && rig.InShot) autoExitUnlockShotSeen = true;
        var after = autoPlayClock - autoExitUnlockPressAt;
        if (door.angle >= FrontRoomsMapWorld.DoorPassableDegrees)
        {
            var commitOk = autoExitLockLifts == 1 && Mathf.Abs(autoExitLockClock - FrontRoomsShotTimings.Unlock.Commit) <= .1f;
            AutoExitCheck(autoExitUnlockOk && autoExitUnlockShotSeen && commitOk && autoExitLockSets == 0 && autoExitUnlocks == 1 && autoExitUnlockZone == FrontRoomsMapWorld.FloorKeyZone
                    && autoExitUnlockAfterLift && !map.ExitLocked && map.IsUnlockedForTools(door) && after <= 4f && autoStationUnlockShots == 1,
                "floor " + floor + ": at N the locked EXIT read " + autoExitUnlockAim + "; E turned the floor key: " + autoStationUnlockShots + " unlock shot (seen " + autoExitUnlockShotSeen
                + "), ExitLockChanged(door, false) " + autoExitLockLifts + "x at the shot's " + AutoF(autoExitLockClock) + " s (commit " + AutoF(FrontRoomsShotTimings.Unlock.Commit) + "), DoorUnlocked "
                + autoExitUnlocks + "x with " + (autoExitUnlockZone == FrontRoomsMapWorld.FloorKeyZone ? "the floor key" : autoExitUnlockZone.ToString())
                + (autoExitUnlockAfterLift ? " after the lock lifted" : " BEFORE the lock lifted") + "; ExitLocked " + map.ExitLocked + "; the leaf passable " + AutoF(after) + " s after E");
            AutoStationM8Row();
            AutoExitNext(ExitOpen);
        }
        else if (after > 4f)
            AutoExitFail("floor " + floor + ": the EXIT not open 4 s after E with the floor key (shot seen " + autoExitUnlockShotSeen + ", lock lifts " + autoExitLockLifts + ", unlocks " + autoExitUnlocks
                + ", held for the key " + map.IsHeldForKey(door) + ", " + AutoF(door.angle, "0") + " deg)");
    }

    // M8 (20_requests): floor 1's timings as a station row: Space -> take, take -> the send's gate, take -> the EXIT's
    // commit, and the locked EXIT's tries before the take (this walk takes the key first: 0). The onboarding (Red
    // 2026-10-30): the key no longer gates, so the gate is usually not due by the EXIT's commit (that is ok while floor 1's
    // explored cells and clock are under the gate's); under -autopilotOnboardOff M7's old gate comes after the take.
    void AutoStationM8Row()
    {
        var notDueOk = !stationSendDue && floorExplored < FrontRoomsMapHunter.StationExploreCells && StationFallbackClock < FrontRoomsMapHunter.StationSendSeconds;
        var gateOk = OnboardOffForTools ? stationSendDue && stationSendAt >= stationTakeAt : stationSendDue || notDueOk;
        var ok = stationTakeAt >= 0f && exitUnlockAt >= stationTakeAt && gateOk && exitTriesBeforeTake == 0;
        AutoStationCheck(ok, "M8: Space -> take " + AutoF(stationTakeAt, "0.0") + " s; take -> the send's gate " + (stationSendDue ? AutoF(stationSendAt - stationTakeAt, "0.0") + " s (" + stationSendWhy + ")"
                : "not due (onboarding: explored " + floorExplored + " < " + FrontRoomsMapHunter.StationExploreCells + ", " + AutoF(StationFallbackClock, "0.0") + " < "
                + AutoF(FrontRoomsMapHunter.StationSendSeconds, "0") + " s)")
            + "; take -> the EXIT's commit " + AutoF(exitUnlockAt - stationTakeAt, "0.0") + " s; the EXIT tried " + exitTriesBeforeTake + " times before the take");
    }

    /// <summary>
    /// The EXIT in view (plan 6 step 4): physics rays at eye height from the stream rooms, 2, 6 and 16 m behind the door
    /// line on its axis (a point past the stream rooms' rear wall is moved to 0.5 m inside it), from D's middle and
    /// from E1, to the middle of the EXIT sign's renderers. Nothing may be hit on the way but the sign itself and the
    /// player's own body; the stream rooms' door must still stand open.
    /// </summary>
    void AutoStationSight(FrontRoomsExitSign sign)
    {
        var l = stationPlan.layout;
        if (sign == null)
        {
            AutoStationCheck(false, "the EXIT in view: no sign over the EXIT door " + l.front + " -> " + l.exit + " 4 s after the start door opened");
            return;
        }
        var any = false;
        var bounds = new Bounds(sign.transform.position, Vector3.zero);
        foreach (var r in sign.GetComponentsInChildren<Renderer>(true))
        {
            if (!any) bounds = r.bounds;
            else bounds.Encapsulate(r.bounds);
            any = true;
        }
        var target = bounds.center;
        var north = AutoExitNorthOf(startDoorPoint);
        var eyeY = map.transform.position.y + EyeHeight;
        var streamDepth = Vector3.Dot(startDoorPoint - new Vector3(startDoorPoint.x, startDoorPoint.y, startRearZ), north);
        var labels = new List<string>();
        var points = new List<Vector3>();
        foreach (var behind in AutoStationBehind)
        {
            var at = Mathf.Min(behind, streamDepth - .5f);
            var p = startDoorPoint - north * at;
            points.Add(new Vector3(p.x, eyeY, p.z));
            labels.Add(AutoF(behind, "0") + " m behind" + (at < behind ? " (at " + AutoF(at, "0.0") + " m, inside the rear wall)" : ""));
        }
        var d = map.CellCenter(l.door);
        points.Add(new Vector3(d.x, eyeY, d.z));
        labels.Add("D");
        var e1 = map.CrossingPoint(l.e1In, l.e1Out);
        points.Add(new Vector3(e1.x, eyeY, e1.z));
        labels.Add("E1");
        Physics.SyncTransforms();
        var ok = roomStream != null && roomStream.TerminalDoorOpen;
        var parts = new List<string>();
        for (var i = 0; i < points.Count; i++)
        {
            var ray = target - points[i];
            var blockers = new List<string>();
            foreach (var hit in Physics.RaycastAll(points[i], ray.normalized, ray.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == playerBody || hit.collider.transform.IsChildOf(sign.transform)) continue;
                blockers.Add(hit.collider.name + " at " + AutoF(hit.distance, "0.0") + " m");
            }
            ok &= blockers.Count == 0;
            parts.Add(labels[i] + " (" + AutoF(ray.magnitude, "0.0") + " m): " + (blockers.Count == 0 ? "clear" : "HIT " + string.Join(", ", blockers)));
        }
        AutoStationCheck(ok, "the EXIT in view: rays at eye height (" + AutoF(EyeHeight) + " m) to the sign's middle, " + AutoF(target.y - map.transform.position.y) + " m up over "
            + l.front + " -> " + l.exit + ", the stream door " + (roomStream != null && roomStream.TerminalDoorOpen ? "open" : "NOT open") + ": " + string.Join("; ", parts));
    }

    /// <summary>
    /// The TV (the StationBeacon anchor) and the placard (the Placard anchor) inside a 60 degree frustum (30 degrees
    /// each way) from the eye at the door opening, looking north along the axis (plan 6 step 4).
    /// </summary>
    void AutoStationFrustum()
    {
        var eye = startDoorPoint + Vector3.up * EyeHeight;
        var north = AutoExitNorthOf(eye);
        var parts = new List<string>();
        var ok = true;
        foreach (var kind in new[] { ModuleMarkerKind.StationBeacon, ModuleMarkerKind.Placard })
        {
            var what = kind == ModuleMarkerKind.StationBeacon ? "TV" : "placard";
            if (!map.StationAnchor(kind, out var at, out _, out _))
            {
                ok = false;
                parts.Add(what + ": no anchor");
                continue;
            }
            var v = at - eye;
            var flat = new Vector3(v.x, 0f, v.z);
            var across = Vector3.SignedAngle(north, flat, Vector3.up);
            var rise = Mathf.Atan2(v.y, flat.magnitude) * Mathf.Rad2Deg;
            ok &= Vector3.Dot(flat, north) > 0f && Mathf.Abs(across) <= AutoStationFrustumHalfDeg && Mathf.Abs(rise) <= AutoStationFrustumHalfDeg;
            parts.Add(what + " " + AutoF(flat.magnitude, "0.0") + " m ahead, " + AutoF(across, "0.0") + " deg across, " + AutoF(rise, "0.0") + " deg up");
        }
        AutoStationCheck(ok, "the TV and the placard inside a 60 deg frustum from the door opening (eye " + AutoF(EyeHeight) + " m, looking north): " + string.Join("; ", parts));
    }

    // ---------- HUD layout: every panel inside the screen ----------
    // Laid out by hand per platform, the HUD can drift off the canvas without any error
    // (the desktop key row once sat 96-118 px above the top edge). Each panel's rect is
    // checked against the canvas once, active or not; one outside fails the run.
    bool autoHudChecked;
    readonly List<string> autoHudOffscreen = new List<string>();

    void AutopilotHudCheck()
    {
        autoHudChecked = true;
        var canvas = keyPanel != null ? keyPanel.GetComponentInParent<Canvas>(true) : null;
        if (canvas == null)
        {
            autoHudOffscreen.Add("no HUD canvas");
            return;
        }
        Canvas.ForceUpdateCanvases();
        var root = (RectTransform)canvas.transform;
        var screen = root.rect;
        var corners = new Vector3[4];
        var panels = new (string name, GameObject go)[]
        {
            ("room", roomPanel), ("threat", threatPanel), ("hint card", contextPanel), ("captions", captionPanel),
            ("crosshair", crosshairImage != null ? crosshairImage.gameObject : null), ("key", keyPanel),
        };
        foreach (var (name, go) in panels)
        {
            if (go == null) continue;
            ((RectTransform)go.transform).GetWorldCorners(corners);
            var min = (Vector2)root.InverseTransformPoint(corners[0]);
            var max = (Vector2)root.InverseTransformPoint(corners[2]);
            if (min.x < screen.xMin - 1f || min.y < screen.yMin - 1f || max.x > screen.xMax + 1f || max.y > screen.yMax + 1f)
                autoHudOffscreen.Add(name + " panel " + min.ToString("F0") + " to " + max.ToString("F0") + " on a screen " + screen.min.ToString("F0") + " to " + screen.max.ToString("F0"));
        }
        Log("AUTOPILOT HUD " + (autoHudOffscreen.Count == 0 ? "all panels on screen" : "OFF SCREEN: " + string.Join("; ", autoHudOffscreen)));
    }

    void AutopilotFinish(string reason)
    {
        if (autoFinished) return;
        autoFinished = true;
        // Before the report takes the error count: a bot-after-exit run's walker rows and summary (AutoBotAfterExitFinish).
        if (autoBotAfterExit) AutoBotAfterExitFinish(reason);
        var report = new AutopilotReport
        {
            seed = runSeed,
            playSeconds = autoPlayClock,
            distanceWalked = autoDistance,
            cellsVisited = autoCellsTotal + autoCells.Count,
            zonesVisited = zonesVisited.Count,
            tier = tier,
            tierCells = tierCells.Count,
            tierLog = new List<string>(autoTiers),
            maxChunksBuilt = autoMaxChunks,
            routes = autoRoutes,
            doorsOpened = autoDoorsOpened,
            keysTaken = keysTaken,
            relayReleasedAt = autoReleaseTime,
            closestRelayMetres = autoMinRelay == float.MaxValue ? -1f : autoMinRelay,
            relayFinalState = relay == null ? "none" : relay.State.ToString(),
            relayDoorsBroken = relay == null ? 0 : relay.DoorsBroken,
#if FRONTROOMS_RELAY_V2
            relayDoorsOpened = relay == null ? 0 : relay.DoorsOpened,
            relayDoorsPushed = relay == null ? 0 : relay.DoorsPushed,
            relayDoorsWalled = relay == null ? 0 : relay.DoorsWalled,
            relayRedispatches = director == null ? 0 : director.Redispatches,
            relayHandoffs = director == null ? 0 : director.Handoffs,
            relaySweeps = director == null ? 0 : director.Sweeps,
#endif
            relayRelays = relay == null ? 0 : relay.Relays,
            relayGhosts = relay == null ? 0 : relay.Ghosts,
            caught = phase == Phase.Caught,
            averageFps = autoClock > 0f ? autoFrames / autoClock : 0f,
            worstFrameMs = autoWorstFrameMs,
            p95FrameMs = Percentile(autoFrameMs, .95f),
            p99FrameMs = Percentile(autoFrameMs, .99f),
            relayTickMaxMs = autoRelayTickMs,
            relayTickMaxAt = autoRelayTickAt,
            handoffWorstFrameMs = autoHandoffWorstMs,
            handoffSlowestFrames = autoHandoffFrames.ConvertAll(f => f.text),
            spacePressedAt = autoSpaceAt,
            mapReadyAt = autoSettledAt,
            startDoorHeldSeconds = startDoorHeldFor,
            startDoorOpenedAt = autoStartDoorAt,
            leftStartRoomsAt = autoLeftStartAt,
            startDoorShutAt = autoStartShutAt,
            streamRemovedAt = autoStreamRemovedAt,
            frameSpikes = autoSpikes,
            officeRoomsDressed = map == null ? 0 : CountNamed(map.transform, "office dressing"),
            errors = autoErrors,
            errorLog = autoErrorLog,
            relayStates = autoStates,
            relayGhostLog = autoGhostLog,
            frames = autoFrameNames,
            glassScenario = autoGlassWanted,
            glassChecks = autoGlassChecks,
            hudOffscreen = autoHudOffscreen,
            unlockShotsStarted = autoUnlockShots,
            keyPickups = autoKeyPickups,
            keyTakenLagMax = autoKeyLagMax,
            keyPresentations = autoKeyPresentations,
            floor = floor,
            exitScenario = autoExitWanted,
            floorRows = autoExitRows,
            floorChangeWorstMs = autoExitSwapWorstMs,
            floorChangeFrameWorstMs = autoExitSwapFrameWorstMs,
            floorHoldMaxSeconds = autoExitHoldMax,
        };
        FrontRoomsCameraRig.ShotStarted -= AutoShotStarted;
        // Keys off (shipped play): a key turn never happens, so no Unlock shot may have started. The door scenario turns keys on for its cases.
        if (map != null && !autoDoorWanted && !map.DoorsNeedKeysForTools)
        {
            // M3: a station run's floor key turns the floor-1 EXIT (ExitUnlock): those Unlock shots, and no other.
            if (autoStationPlanned)
                report.doorChecks.Add((autoUnlockShots == autoStationUnlockShots ? "ok   " : "FAIL ") + "keys off: no Unlock shot started but the floor key's at the EXIT (" + autoUnlockShots + ", "
                    + autoStationUnlockShots + " of them the floor key's)");
            else report.doorChecks.Add((autoUnlockShots == 0 ? "ok   " : "FAIL ") + "keys off: no Unlock shot started (" + autoUnlockShots + ")");
        }
        // Every pickup, the door scenario's key cases too: KeyTaken in its contact frame unless presented (§5.4).
        if (map != null) report.doorChecks.Add(AutoKeyRuleLine());
        if (autoDoorWanted)
        {
            if (!autoDoorDone) AutoDoorCheck(false, "doors: the scenario did not finish (case " + autoDoorCase + ", step " + autoDoorStep + ")");
            if (autoDoorActive) AutoDoorEnd();
            report.doorChecks.AddRange(autoDoorChecks);
        }
        var doorsOk = report.doorChecks.TrueForAll(c => !c.StartsWith("FAIL"));
        if (!autoHudChecked && mapPlay) AutopilotHudCheck();
        var reached = mapPlay && autoDistance > 20f && autoCellsTotal + autoCells.Count > 8;
        if (autoGlassWanted && !autoGlassDone) AutoGlassCheck(false, "glass: the scenario did not finish (step " + autoGlassStep + ")");
        var glassOk = !autoGlassWanted || autoGlassChecks.TrueForAll(c => c.StartsWith("ok"));
        // C22: the EXIT scenario passes only when it went down all its floors with every check ok.
        if (autoExitWanted && !autoExitDone && !autoBotAfterExit) AutoExitCheck(false, "exit: the scenario did not finish (floor " + floor + ", step " + AutoExitStepNames[autoExitStep] + ")");
        var exitOk = !autoExitWanted || (autoExitCycles >= AutoExitFloors && autoExitChecks.TrueForAll(c => c.StartsWith("ok")));
        report.exitChecks = autoExitChecks;
        // A26: a run with a station passes only with all its rows in and ok; one that asked for the stand-in must have had it.
        if (autoStationPlanned)
        {
            if (!autoStationOpened) AutoStationCheck(false, "the start door never opened");
            if (!autoStationSight) AutoStationCheck(false, "the EXIT was never checked in view");
            if (!autoStationShut) AutoStationCheck(false, "the start door never shut");
            if (autoStationTake < 4) AutoStationCheck(false, "the floor key was never taken (M3; the take's stage " + autoStationTake + ")");
            else if (!autoStationPanelRow) AutoStationCheck(false, "the key panel after the take was never checked (left floor 1 within 1.5 s of the take)");
            if (autoRouteStage == 0) AutoStationCheck(false, "M5: the route at the start was never checked");
            else if (autoStationTake >= 4 && autoRouteStage < 3) AutoStationCheck(false, "M5: the route's flip at the take was never checked (stage " + autoRouteStage + ")");
            if (OnboardOffForTools)
            {
                // The tools' old gate: as before the onboarding, the first send comes on floor 1.
                if (!autoStationSent && (autoExitWanted || elapsed > FrontRoomsMapHunter.OnboardOffSendSeconds + 1f)) AutoStationCheck(false, "the Relay's first send never came on floor 1 (M7, onboarding off)");
            }
            else if (!autoStationSent && !autoFirstSendBelow)
            {
                // The onboarding (Red 2026-10-30): no send on floor 1 before its gate. The EXIT run must meet the run's first
                // send below it; a run that stayed on floor 1 passes without one while the gate never came, and says why.
                if (stationSendDue && floor == 1)
                {
#if !FRONTROOMS_RELAY_V2
                    AutoStationCheck(false, "floor 1's gate came (" + StationGateName(stationSendWhy) + " at " + AutoF(stationSendAt, "0.0") + " s) but the first send never did");
#endif
                }
                else if (autoExitWanted) AutoStationCheck(false, "the run's first send never came (none on floor 1 by the onboarding, none below it; floor " + floor + ")");
                else if (floor == 1)
                {
                    var clock = StationFallbackClock;
                    AutoStationCheck(floorExplored < FrontRoomsMapHunter.StationExploreCells && clock < FrontRoomsMapHunter.StationSendSeconds,
                        "onboarding: no send on floor 1 in this run (explored " + floorExplored + " < " + FrontRoomsMapHunter.StationExploreCells + " cells, " + AutoF(clock, "0.0") + " < "
                        + AutoF(FrontRoomsMapHunter.StationSendSeconds, "0") + " s since the start door shut); the run passes without a release on floor 1");
                }
            }
            if (autoExitWanted && autoStationBelowRows < autoExitCycles)
                AutoStationCheck(false, "below the station floor (Q1 B): " + autoStationBelowRows + " floor rows for " + autoExitCycles + " floors gone down");
        }
        else if (StationModuleForTools != null && mapPlay) AutoStationCheck(false, "a station was asked for (-autopilotStation), but floor 1 had none");
        var stationOk = autoStationChecks.TrueForAll(c => c.StartsWith("ok"));
        report.stationChecks = autoStationChecks;
        var hudOk = autoHudOffscreen.Count == 0;
        // The onboarding (Red 2026-10-30): a station run still on floor 1 whose gate never came has no Relay to meet; that is
        // the rule, not a failure (its row says why). -autopilotOnboardOff runs keep today's need.
        var onboardingHeld = autoStationPlanned && !OnboardOffForTools && floor == 1 && !stationSendDue && relay != null && !relay.FirstSendDone;
#if FRONTROOMS_RELAY_V2
        // V3a: PASS needs the Relay on the map at least once (it may be Away again at the end).
        var relayOk = relay != null && (autoV2OnMapAt >= 0f || onboardingHeld);
#else
        var relayOk = relay != null && (relay.Released || onboardingHeld);
#endif
        report.verdict = (reached && autoErrors == 0 && relayOk && glassOk && hudOk && doorsOk && exitOk && stationOk ? "PASS" : "FAIL") + " · ended by " + reason
            + (hudOk ? "" : " · HUD off screen: " + string.Join("; ", autoHudOffscreen));
        if (autoBaseline) report.verdict = AutoBaseFinish(reason);
        File.WriteAllText(Path.Combine(autoOutDir, "report.json"), JsonUtility.ToJson(report, true));
        var done = Path.Combine(Directory.GetParent(Application.dataPath).FullName, AutopilotDoneFile);
        Directory.CreateDirectory(Path.GetDirectoryName(done));
        File.WriteAllText(done, report.verdict);
        Log("AUTOPILOT " + report.verdict);
    }
#endif
}
