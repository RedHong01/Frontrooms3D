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

[ExecuteAlways]
// First-person FrontRooms. The title is the looping room stream; when the
// player presses Space they take over where the camera is, the stream stops
// at its next shut door, and the generated Level 0 maze (FrontRoomsMapWorld)
// lies behind that door. There the serialized Relay hunts them
// (FrontRoomsMapHunter).
public sealed partial class FrontRooms3DGame : MonoBehaviour
{
    enum Phase { Title, Playing, Paused, Caught }
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
    /// <summary>The player started climbing through a broken window, at its opening.</summary>
    public static event Action<Vector3> PlayerClimbed;
    /// <summary>The game paused (true: Esc or lost focus) or resumed (false). Also false when a paused run is torn down.</summary>
    public static event Action<bool> Paused;
    /// <summary>The run's difficulty tier: 1 when a run starts, then each rise (level profile Tiers: every 4 new zones, or 2 minutes without one).</summary>
    public static event Action<int> TierChanged;
    /// <summary>The player's stamina, 0..1 (5 s of sprint), for the sound layer's breath and run gait: read it, don't mirror the rule.</summary>
    public static float PlayerStamina01 { get; private set; } = 1f;
    /// <summary>Run dry and not yet one segment (1 s) back: no sprint until then.</summary>
    public static bool PlayerWinded { get; private set; }
    /// <summary>The sprint rule's answer this frame (Shift held, moving, stamina left, not winded).</summary>
    public static bool PlayerSprinting { get; private set; }

    [SerializeField, Tooltip("The Level 0 maze's numbers: generation, run seed, streaming, light budget, dressing (Assets/Levels/FrontRoomsLevel0.asset). Empty: the code defaults.")]
    FrontRoomsLevelProfile levelProfile;
    FrontRoomsRelayRig hunterRig;
    FrontRoomsMapWorld map;
    FrontRoomsMapHunter relay;
    Transform playerRoot;
    CharacterController playerBody;
    Vector2 playerPos;
    readonly List<string> events = new List<string>();
    Material wallMat, floorMat, darkMat, ceilingMat;
    readonly Dictionary<RoomRule, Material> wallMats = new Dictionary<RoomRule, Material>();
    readonly Dictionary<RoomRule, Material> floorMats = new Dictionary<RoomRule, Material>();
    readonly Dictionary<RoomRule, Material> ceilingMats = new Dictionary<RoomRule, Material>();
    Material trimMat, fixtureMat;
    AudioSource hum;
    AudioClip playerStepClip, playerRunStepClip, hunterStepClip, doorClip, bangClip, caughtClip;
    FrontRoomsFoley foley;
    Text roomMetaText, roomText, threatStateText, distanceText, contextText, overlayText, keyText, displaySettingsText;
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
    Font monoFont, bayonFont, serifFont;
    GameObject overlay, roomPanel, threatPanel, contextPanel, displaySettingsPanel;
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
    const string CalmHint = "Shift to sprint. About 5 seconds, and it hears every step.";
    const float CalmHintSeconds = 9f;
    float gameplayHudAlpha;
    float yaw, pitch, elapsed, stepTime, hunterStepTime, flashTime;
    // The player camera's layers: BaseEye for gameplay, shots and shakes for the picture (audit §6.4).
    FrontRoomsCameraRig rig;
    // The glass break's shot ("Brace, strike, flinch") on the rig.
    FrontRoomsGlassShot glassShot;
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
    const float EyeHeight = ModuleUnits.PlayerEye, Reach = 2.4f, GlassNoiseRadius = 40f;
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
    readonly HashSet<GridCoord> zonesVisited = new HashSet<GridCoord>();
    GridCoord currentZone;
    // Difficulty: the run's tier and the seconds since the last new zone. The Relay runs on
    // relayTuning, rebuilt every frame from hunterTuning (the Inspector's base) and the tier.
    int tier = 1;
    float tierStall;
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
        fill.shadows = FrontRoomsMobilePerformance.Active ? LightShadows.None : LightShadows.Soft;
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
        FrontRoomsMobilePerformance.Apply();
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
        FrontRoomsLook.ApplyAmbient();
        InitializeFonts();
        cam = GetComponentInChildren<Camera>(true);
        if (cam == null) cam = CreateCamera(transform);
        EnsureRuntimeCamera(cam);
        FrontRoomsPostStack.ConfigureCamera(cam);
        FrontRoomsPostStack.Ensure(transform);
        BindHunter();
        BuildMaterials();
        hdrEnabled = PlayerPrefs.GetInt(HdrPreferenceKey, defaultHdr ? 1 : 0) != 0;
        FrontRoomsSettings.Load();
        ApplyHdrMode(hdrEnabled, false);
        BuildHud();
        BuildSound();
        // The map's first-use loads happen now, before the first frame, not
        // in the frames after Space while the player is watching.
        FrontRoomsMapWorld.Prewarm();
        BuildTitleCorridor();
        SetPhase(Phase.Title);
        if (restart)
        {
            restart = false;
            StartCanonicalStreamedRestart();
        }
        Log("READY · manual title, first-person · display " + Screen.width + "x" + Screen.height
            + " " + Screen.fullScreenMode + " · camera " + (cam != null && cam.enabled ? "active" : "missing"));
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
        roomStream.Tick(dt);
        // Historical title clock: the complete wordmark stays present while
        // the first streamed door drives the two trailing-S relays.
        titleLogoAlpha = roomStream.LogoVisibility;
        AdvanceLogoRelay(dt);
        UpdateLogoMotion();
    }

    // The wordmark shows on the title and while it fades out as play starts.
    bool LogoShown => phase == Phase.Title || logoFading;

    void UpdateLogoFade(float dt)
    {
        titleLogoAlpha = Mathf.MoveTowards(titleLogoAlpha, 0f, dt / LogoExitSeconds);
        if (titleLogoAlpha <= 0f) logoFading = false;
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
        if (mapPlay || roomStream == null || cam == null) return;
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
        var createMs = watch.Elapsed.TotalMilliseconds;

        // The door line is a map cell line and the stream centreline a cell
        // centre (BuildTitleCorridor, TitleCenterX), so the door opens onto
        // one cell; the stream rooms take the 5-cell strip behind it.
        var centerX = roomStream.CenterX;
        var doorZ = roomStream.RoomStartZ(terminal) + FrontRoomsRoomStream.RoomLength;
        var rearZ = roomStream.RoomStartZ(roomStream.OldestSequence);
        var rows = Mathf.RoundToInt((doorZ - rearZ) / MapGrid.CellSize);
        map.transform.SetPositionAndRotation(MapRootFor(map, centerX, doorZ, rows), Quaternion.identity);
        var placeMs = watch.Elapsed.TotalMilliseconds - createMs;
        startDoorCell = map.CellOf(new Vector3(centerX, 0f, doorZ + MapGrid.CellSize * .5f));
        startDoorPoint = new Vector3(centerX, 0f, doorZ);
        startRearZ = rearZ;
        startDoorHeldFor = 0f;
        var rearCell = map.CellOf(new Vector3(centerX, 0f, rearZ + MapGrid.CellSize * .5f));
        map.SetStartArea(new RectInt(startDoorCell.x - StartAreaHalfCells, rearCell.y, StartAreaHalfCells * 2 + 1, startDoorCell.y - rearCell.y), startDoorCell);
        FrontRoomsMobilePerformance.ApplyMapBudget(map);
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
        yaw = 0f;
        pitch = 0f;
        fallSpeed = 0f;
        climbTime = -1f;
        stamina = StaminaSeconds;
        FrontRoomsCaptions.Clear();
        winded = false;
        PlayerStamina01 = 1f;
        PlayerWinded = PlayerSprinting = false;
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
        map.DoorBroken += p => Jolt(p, FrontRoomsShotTimings.DoorBreak.CameraRange, FrontRoomsShotTimings.DoorBreak.BreakShakeDeg, FrontRoomsShotTimings.DoorBreak.BreakShakeDecay);
        // A live build radius change moves the far plane (in the start rooms UpdateStartRooms sets it every frame).
        map.LiveApplied += () => { if (cam != null && roomStream == null) cam.farClipPlane = map.SightDistance; };
        map.StreamFocus = map.CellCenter(startDoorCell);
        map.StartLampsNorth = 0f;
        map.StartLampsSouth = 0f;
        map.Begin(playerRoot, false);
        // The start rooms take Level 0's reflection at once (no blend from the title's).
        reflectionSet = false;
        UpdateReflectionZone(true);

        tier = 1;
        tierStall = 0f;
        UpdateRelayTuning();
        relay = new FrontRoomsMapHunter(map, relayTuning, playerBody, hunter, runSeed);
        relay.StateChanged += state => Event("hunter", state.ToString());
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

        zonesVisited.Clear();
        keysTaken = 0;
        playerPos = Flat(playerRoot.position);
        elapsed = 0f;
        mapPlay = true;
        logoFading = true;
        SetPhase(Phase.Playing);
        Event("start", "map seed " + runSeed + ", stream room " + terminal);
        MapRunStarted?.Invoke(map, relay);
        map.GenerationTier = tier;
        TierChanged?.Invoke(tier);
#if UNITY_EDITOR
        AutopilotRunStarted();
#endif
        Log("START · in place in stream room " + terminal + " · maze seed " + runSeed + " behind its door, map root " + map.transform.position + ", door cell " + startDoorCell
            + " · " + watch.Elapsed.TotalMilliseconds.ToString("0.0", CultureInfo.InvariantCulture) + " ms (map " + createMs.ToString("0.0", CultureInfo.InvariantCulture) + ", placing " + placeMs.ToString("0.0", CultureInfo.InvariantCulture) + ")");
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
    /// of it the door leads into.
    /// </summary>
    static Vector3 MapRootFor(FrontRoomsMapWorld map, float centerX, float doorZ, int rows)
    {
        var period = ModuleUnits.WorldPeriod;
        var probe = new Vector3(centerX, 0f, doorZ + MapGrid.CellSize * .5f);
        Vector3? reachable = null;
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
            if (!standard && reachable.HasValue) continue;
            var ahead = new GridCoord(door.x, door.y + 1);
            var onward = map.Cache.Edge(door, ahead);
            if (onward == EdgeKind.Wall || onward == EdgeKind.Window || !LeadsIntoMaze(map.Cache, ahead, area, door)) continue;
            if (standard) return root;
            reachable = root;
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
#if UNITY_EDITOR
        AutopilotNoise(AutoCauseDoor, p, hunterTuning.doorNoiseRadius);
#else
        relay?.Noise(p, hunterTuning.doorNoiseRadius);
#endif
        Event("door", Flat(p).ToString());
    }

    void OnGlassBroken(Vector3 p)
    {
        FrontRoomsMobileInteractionEvents.GlassShattered();
        FoleyDoorBreak(Flat(p));
#if UNITY_EDITOR
        AutopilotNoise(AutoCauseGlass, p, GlassNoiseRadius);
#else
        relay?.Noise(p, GlassNoiseRadius);
#endif
        Flash("GLASS BROKEN  /  WALK INTO THE FRAME TO CLIMB THROUGH");
        Event("glass", Flat(p).ToString());
    }

    void OnKeyTaken(GridCoord zone)
    {
        keysTaken++;
        // The key panel fading in is the pickup cue until keys open doors (audit F1, 1.8).
        Event("key", zone.ToString());
    }

    static Vector2 Flat(Vector3 p) => new Vector2(p.x, p.z);

    void UpdateMapPlay(float dt)
    {
        if (map == null || playerBody == null) return;
        Vector2 local;
        bool sprintHeld;
        var lookDegrees = 0f;
        var input = FrontRoomsInput.ReadFrame();
#if UNITY_EDITOR
        if (autopilot) AutopilotSteer(dt, out local, out sprintHeld);
        else
#endif
        {
            if (mouseSettleFrames > 0) mouseSettleFrames--;
            // While a shot holds the look, mouse movement is dropped, not saved for later.
            else if ((rig == null || !rig.LookLocked) && (glassShot == null || !glassShot.LookLocked))
            {
                var dx = input.Look.x;
                var dy = input.Look.y;
                yaw += dx;
                pitch = Mathf.Clamp(pitch - dy, -75f, 75f);
                lookDegrees = Mathf.Abs(dx) + Mathf.Abs(dy);
            }
            local = input.Move;
            sprintHeld = input.SprintHeld;
        }
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
            var threat = relay != null && (relay.SeesPlayer || relay.State == HunterState.Chase);
            glassShot.Tick(dt, threat, moveIntent, lookDegrees);
            glassShot.ClampLook(ref yaw, ref pitch);
            if (glassShot.MoveLocked) { local = Vector2.zero; sprintHeld = false; }
        }
        playerRoot.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (rig != null) rig.SetBase(pitch);
        else cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        // A pull: the player steps clear onto the latch side while the leaf waits (single-acting doors).
        var stepVelocity = Vector3.zero;
        if (pullClear.HasValue)
        {
            pullClearTime += dt;
            // A door pull steps at PullStepSpeed; the glass step-in follows a smoothstep from where it began.
            var stepIn = FrontRoomsShotTimings.GlassBreak.StepInSeconds;
            var target = pullIsGlass ? Vector3.Lerp(glassStepFrom, pullClear.Value, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(pullClearTime / stepIn))) : pullClear.Value;
            var toClear = target - playerRoot.position;
            toClear.y = 0f;
            // Arrive exactly (the last step lands on the spot): the spot sits right at the leaf's reach, so stopping short leaves the body in its way.
            var arrived = toClear.magnitude < 1e-3f && (!pullIsGlass || pullClearTime >= stepIn);
            if (arrived || pullClearTime > PullStepSeconds || (relay != null && (relay.SeesPlayer || relay.State == HunterState.Chase))) pullClear = null;
            else
            {
                local = Vector2.zero;
                sprintHeld = false;
                stepVelocity = Vector3.ClampMagnitude(toClear / Mathf.Max(dt, 1e-4f), pullIsGlass ? GlassStepMaxSpeed : PullStepSpeed);
            }
        }
        var wish = playerRoot.right * local.x + playerRoot.forward * local.y;
        if (wish.sqrMagnitude > 1f) wish.Normalize();
        var sprinting = sprintHeld && wish.sqrMagnitude > .01f && stamina > 0f && !winded;
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
#if UNITY_EDITOR
                if (sprinting) AutopilotNoise(AutoCauseSprint, playerRoot.position, hunterTuning.sprintNoiseRadius);
#else
                if (sprinting) relay?.Noise(playerRoot.position, hunterTuning.sprintNoiseRadius);
#endif
            }
        }
        UpdateStartRooms(dt);
        // Zones count from the first step into the map.
        if (!inStartRooms)
        {
            var zone = map.ZoneOf(map.CellOf(playerRoot.position)).id;
            if (zone != currentZone || zonesVisited.Count == 0)
            {
                currentZone = zone;
                if (zonesVisited.Add(zone))
                {
                    Event("zone", zone.ToString());
                    tierStall = 0f;
                    RaiseTier(TierRules.TierForZones(zonesVisited.Count), "zones");
                }
            }
            // A run that stalls still gets harder.
            tierStall += dt;
            if (tierStall >= TierRules.stallSeconds)
            {
                tierStall = 0f;
                RaiseTier(tier + 1, "stall");
            }
        }
        UpdateAim(dt);
#if UNITY_EDITOR
        if (autoBaseline) AutoBaseBeforeRelayTick();
        var tickWatch = autopilot ? System.Diagnostics.Stopwatch.StartNew() : null;
#endif
        // Dormant until the door back to the stream rooms has shut behind the
        // player: its release clock only starts once they are in the maze for good.
        if (relay.Released || streamFade >= 0f || roomStream == null)
        {
            UpdateRelayTuning();
            // The Relay sees the player's base eye, never a shot or shake pose.
            relay.Tick(dt, playerRoot.position, rig != null ? rig.BaseEye.position : cam.transform.position, playerRoot.forward);
            // A chasing or seeing Relay frees the player from any shot at once (audit §3.2).
            if (rig != null && (relay.SeesPlayer || relay.State == HunterState.Chase)) rig.CancelForRelay();
        }
#if UNITY_EDITOR
        if (tickWatch != null && tickWatch.Elapsed.TotalMilliseconds > autoRelayTickMs)
        {
            autoRelayTickMs = (float)tickWatch.Elapsed.TotalMilliseconds;
            autoRelayTickAt = autoPlayClock;
        }
#endif
        UpdateRelayRig(dt);
        UpdateReflectionZone(false);
        // Picture layers last, after gameplay has read BaseEye this frame.
        rig?.Tick(dt);
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
        if (inStartRooms || map.InStartArea(cell)) zone = FrontRoomsLook.ReflectionZone.Level0;
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
    /// The door opens toward the player: step them to the clear spot beside the latch, with the
    /// controller (so walls still hold), during the pull beat. Not while the Relay chases or sees
    /// them: they keep control, and the leaf rests on them until they move.
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
    }

    // The pull step's mover also carries the glass step-in; this says whose step it is, and where the step-in began.
    bool pullIsGlass;
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
        if (autoGlassActive || autoBaseline) return autoEDown;
#endif
        return FrontRoomsInput.UseDown;
    }

    bool EHeld()
    {
#if UNITY_EDITOR
        if (autoGlassActive || autoBaseline) return autoEHeld;
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
        glassStepFrom = feet;
    }

    /// <summary>What the crosshair is on: E opens or shuts a door, holding E breaks glass.</summary>
    void UpdateAim(float dt)
    {
        var previous = aimed;
        aimed = null;
        prompt = null;
        aimedHold = false;
        // Gameplay aims from the base eye: shots, shakes and leans are picture only.
        var eye = rig != null ? rig.BaseEye : new Pose(cam.transform.position, cam.transform.rotation);
        var hitPoint = Vector3.zero;
        if (Physics.Raycast(new Ray(eye.position, eye.rotation * Vector3.forward), out var hit, Reach, ~0, QueryTriggerInteraction.Ignore))
        {
            prompt = map.Describe(hit.collider, out aimedHold);
            // Panes break from 1.2 m (the glass shot's stand needs it); everything else keeps the 2.4 m reach.
            if (aimedHold && hit.distance > FrontRoomsShotTimings.GlassBreak.Reach) { prompt = null; aimedHold = false; }
            // Tap mode: the pane breaks on repeated taps, so the prompt says so (the map's text is the hold prompt).
            if (aimedHold && FrontRoomsSettings.TapToBreak) prompt = "TAP E  ·  BREAK GLASS";
            if (prompt != null) aimed = hit.collider;
            hitPoint = hit.point;
        }
        if (previous != null && previous != aimed)
        {
            map.ReleaseHold(previous);
            ReleaseGlass();
            glassArmed = false;
            tapCredit = 0f;
            // Every pane not aimed at has no progress (leaving one releases it), so the bar starts again.
            holdProgress = 0f;
        }
        // During a shot E belongs to the shot (cancel, or a buffered push), never to what is aimed at; the
        // glass shot's own pane keeps its E (the hold, or tap mode's next strike).
        var eDown = EDown();
        var glassOwnsE = glassShot != null && aimedHold && aimed == glassPane && !glassShot.Shattered && (glassShot.Active || glassShot.Blending);
        var pressed = eDown && (glassOwnsE || rig == null || !rig.Consume(ShotInput.Use));
        if (aimed == null)
        {
            holdProgress = 0f;
            return;
        }
        if (!aimedHold)
        {
            holdProgress = 0f;
            if (pressed) map.Use(aimed);
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
        var facing = relay.SeesPlayer ? Flat(playerRoot.position - position) : Flat(relay.Heading);
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
                : state == HunterState.Hunt || state == HunterState.Wander || (state == HunterState.Search && relay.Moving) ? FrontRoomsRelayRig.MotionState.Walk
                : state == HunterState.Search ? FrontRoomsRelayRig.MotionState.Search
                : FrontRoomsRelayRig.MotionState.IdleListen;
            // The head turns toward the last noise while it listens or stands searching.
            hunterRig.SetListenTarget(state == HunterState.Listen || state == HunterState.Search ? relay.ListenPoint : null);
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

    static string ZoneName(ZoneInfo zone)
    {
        if (zone.theme == ZoneTheme.Office) return "LEVEL 4 / OFFICE";
        switch (zone.height)
        {
            case ZoneHeight.Low: return "LEVEL 0 / LOW ROOMS";
            case ZoneHeight.Tall: return "LEVEL 0 / TALL HALLS";
            default: return "LEVEL 0 / THE MAZE";
        }
    }

    void BuildSound()
    {
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
        settings.scaleMode = UiPanelScaleMode.ScaleWithScreenSize;
        settings.referenceResolution = new Vector2Int(1920, 1080);
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

        vectorLogoRoot = new UiVisualElement { name = "FrontRooms SVG lockup" };
        vectorLogoRoot.style.position = UiPosition.Absolute;
        vectorLogoRoot.style.left = UiLength.Percent(50);
        vectorLogoRoot.style.top = UiLength.Percent(50);
        vectorLogoRoot.style.width = 965f; vectorLogoRoot.style.height = 192f;
        vectorLogoRoot.style.marginLeft = -482.5f; vectorLogoRoot.style.marginTop = -96f;
        vectorLogoRoot.style.scale = new UnityEngine.UIElements.Scale(new Vector3(LogoScale, LogoScale, 1f));
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
        ("ASSIST", "RELAY READOUT", () => FrontRoomsSettings.RelayReadout ? "ON" : "OFF", d => FrontRoomsSettings.SetRelayReadout(!FrontRoomsSettings.RelayReadout)),
    };

    void UpdateDisplaySettingsText()
    {
        if (displaySettingsText == null) return;
        // The mobile canvas owns the eight larger touch preference rows. Keep
        // the legacy desktop text panel as a quiet title/instruction layer so
        // its six keyboard rows do not sit underneath those touch targets.
        if (Application.isMobilePlatform)
        {
            displaySettingsText.text = "<size=30><b>TOUCH SETTINGS</b></size>\n\n<size=16>TAP A ROW TO CHANGE    BACK TO CLOSE</size>";
            return;
        }
        var rows = SettingsRows();
        settingsIndex = (settingsIndex % rows.Length + rows.Length) % rows.Length;
        var text = new System.Text.StringBuilder("<size=30><b>SETTINGS</b></size>\n");
        string section = null;
        for (var i = 0; i < rows.Length; i++)
        {
            if (rows[i].section != section)
            {
                section = rows[i].section;
                text.Append("\n<size=14>").Append(section).Append("</size>\n");
            }
            var line = rows[i].label + "  /  " + rows[i].value();
            text.Append(i == settingsIndex ? "<color=#F4DF3B>›  " + line + "</color>\n" : line + "\n");
        }
        text.Append("\n<size=16>↑ ↓  CHOOSE    ← →  CHANGE    ESC  CLOSE</size>");
        displaySettingsText.text = text.ToString();
    }

    /// <summary>The settings panel's keys: ↑ ↓ (W S) choose a row, ← → (A D), Enter or Space change it; H and T stay as shortcuts.</summary>
    void HandleSettingsKeys()
    {
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

    /// <summary>The pause card, following the input settings.</summary>
    string PauseText() => "<size=88><b>PAUSED</b></size>\n\n<size=13>WASD  MOVE    MOUSE  LOOK    SHIFT  SPRINT    E  DOOR    "
        + (FrontRoomsSettings.TapToBreak ? "TAP E" : "HOLD E") + "  BREAK GLASS\nR  RESTART    O  SETTINGS</size>\n\n<color=#F4DF3B><size=20>ESC  RESUME</size></color>";

    void ToggleDisplaySettings()
    {
        if (phase != Phase.Paused || displaySettingsPanel == null) return;
        displaySettingsOpen = !displaySettingsOpen;
        displaySettingsPanel.SetActive(displaySettingsOpen);
        if (overlayText != null)
        {
            overlayText.enabled = !displaySettingsOpen;
            // Back on the pause card: its instructions follow the input setting just changed.
            if (!displaySettingsOpen) overlayText.text = PauseText();
        }
        UpdateDisplaySettingsText();
        UpdateMobileTouchMenu();
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
        rootRect.anchoredPosition = Vector2.zero;
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
        var g = new GameObject("Minimal HUD"); var c = g.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.pixelPerfect = true;
        var scale = g.AddComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scale.referenceResolution = new Vector2(1920, 1080); scale.matchWidthOrHeight = .5f;
        var media = new Color(.078f, .078f, .078f, .9f);
        var paper = C("F4F1E8");
        var accent = C("F4DF3B");

        // The play HUD follows the 72px outer margin and 24px internal rhythm from UI_SYSTEM.
        // Room and threat are direct typography overlays. Their cards obscured the
        // environment and allowed long room names to bleed past the top-left edge.
        roomPanel = TypographyGroup(g.transform, "Room typography", new Vector2(0, 1), new Vector2(72, -72), new Vector2(505, 144));
        roomHudGroup = roomPanel.AddComponent<CanvasGroup>();
        roomMetaText = Text(roomPanel.transform, "Room meta", new Vector2(0, 1), new Vector2(24, -18), new Vector2(660, 22), 13, TextAnchor.UpperLeft);
        roomMetaText.color = C("BDBAB0");
        roomText = Text(roomPanel.transform, "Room", new Vector2(0, 1), new Vector2(24, -44), new Vector2(505, 72), 50, TextAnchor.UpperLeft);
        roomText.color = paper;
        roomText.horizontalOverflow = HorizontalWrapMode.Overflow;
        roomText.verticalOverflow = VerticalWrapMode.Overflow;

        threatPanel = TypographyGroup(g.transform, "Threat typography", Vector2.one, new Vector2(-72, -72), new Vector2(720, 144));
        threatHudGroup = threatPanel.AddComponent<CanvasGroup>();
        threatStateText = Text(threatPanel.transform, "Threat state", new Vector2(1, 1), new Vector2(-24, -18), new Vector2(660, 34), 20, TextAnchor.UpperRight);
        threatStateText.color = accent; threatStateText.fontStyle = FontStyle.Bold;
        threatStateText.horizontalOverflow = HorizontalWrapMode.Overflow;
        threatStateText.verticalOverflow = VerticalWrapMode.Truncate;
        distanceText = Text(threatPanel.transform, "Distance", new Vector2(1, 1), new Vector2(-24, -57), new Vector2(660, 24), 13, TextAnchor.UpperRight);
        distanceText.color = C("BDBAB0");
        distanceText.horizontalOverflow = HorizontalWrapMode.Overflow;
        distanceText.verticalOverflow = VerticalWrapMode.Truncate;

        contextPanel = Panel(g.transform, "Context panel", new Vector2(.5f, 0), new Vector2(0, 72), new Vector2(920, 120), media);
        contextHudGroup = contextPanel.AddComponent<CanvasGroup>();
        contextText = Text(contextPanel.transform, "Context", new Vector2(.5f, .5f), new Vector2(10, 0), new Vector2(820, 72), 24, TextAnchor.MiddleCenter);
        contextText.color = paper;

        captionPanel = Panel(g.transform, "HUD / Captions", new Vector2(.5f, 0), new Vector2(0, 208), new Vector2(920, 50), new Color(.078f, .078f, .078f, .75f));
        captionHudGroup = captionPanel.AddComponent<CanvasGroup>();
        captionText = Text(captionPanel.transform, "Captions", new Vector2(.5f, .5f), Vector2.zero, new Vector2(880, 110), 20, TextAnchor.MiddleCenter);
        captionText.color = paper;
        captionText.supportRichText = true;
        captionText.lineSpacing = 1.15f;
        captionPanel.SetActive(false);

        var crosshairObject = Panel(g.transform, "HUD / Crosshair", new Vector2(.5f, .5f), Vector2.zero, new Vector2(32, 32), Color.white);
        crosshairImage = crosshairObject.GetComponent<Image>();
        crosshairImage.sprite = LoadHudSprite("UI/HUD_Crosshair");
        crosshairImage.preserveAspect = true;
        crosshairHudGroup = crosshairObject.AddComponent<CanvasGroup>();
        // Prompt, hold ring and stamina sit under the crosshair and share its fade.
        promptText = Text(crosshairObject.transform, "Key prompt", new Vector2(.5f, .5f), new Vector2(0f, -44f), new Vector2(720f, 26f), 20, TextAnchor.MiddleCenter);
        promptText.color = paper;
        promptText.horizontalOverflow = HorizontalWrapMode.Overflow;
        holdBar = Panel(crosshairObject.transform, "Hold bar", new Vector2(.5f, .5f), new Vector2(0f, -68f), new Vector2(120f, 4f), new Color(1f, 1f, 1f, .25f));
        holdBarFill = Panel(holdBar.transform, "Hold bar fill", new Vector2(0f, .5f), Vector2.zero, new Vector2(0f, 4f), accent).GetComponent<Image>();
        holdBar.SetActive(false);
        for (var i = 0; i < staminaSegments.Length; i++)
        {
            staminaSegments[i] = Panel(crosshairObject.transform, "Stamina " + (i + 1), new Vector2(.5f, .5f), new Vector2(-60f + i * 30f, -92f), new Vector2(24f, 6f), accent).GetComponent<Image>();
            staminaSegments[i].enabled = false;
        }

        keyPanel = TypographyGroup(g.transform, "HUD / Key", new Vector2(0, 0), new Vector2(72, 118), new Vector2(147, 22));
        keyHudGroup = keyPanel.AddComponent<CanvasGroup>();
        keyImage = Panel(keyPanel.transform, "Key glyph", new Vector2(0, 0), Vector2.zero, new Vector2(40, 22), Color.white).GetComponent<Image>();
        keyImage.sprite = LoadHudSprite("UI/HUD_KeyGlyph");
        keyImage.preserveAspect = true;
        keyText = Text(keyPanel.transform, "Key label", new Vector2(0, 0), new Vector2(54, 0), new Vector2(110, 22), 20, TextAnchor.MiddleLeft);
        keyText.color = paper;
        keyText.text = "LEVEL 0 KEY";
        keyText.horizontalOverflow = HorizontalWrapMode.Overflow;
        keyText.verticalOverflow = VerticalWrapMode.Overflow;
        gameplayHudAlpha = 0f;
        ApplyGameplayHudAlpha();

        displaySettingsPanel = Panel(g.transform, "Display settings", new Vector2(.5f, .5f), Vector2.zero, new Vector2(920, 720), new Color(.055f, .055f, .05f, .97f));
        Rule(displaySettingsPanel.transform, "Display settings accent", new Vector2(0, .5f), new Vector2(28, 0), new Vector2(4, 320), accent);
        displaySettingsText = Text(displaySettingsPanel.transform, "Display settings text", new Vector2(.5f, .5f), new Vector2(18, 0), new Vector2(760, 660), 24, TextAnchor.MiddleCenter);
        displaySettingsText.color = paper;
        displaySettingsPanel.SetActive(false);
        overlay = new GameObject("Menu"); overlay.transform.SetParent(g.transform, false); var image = overlay.AddComponent<Image>(); overlayImage = image;
        // The title uses the supplied brand asset with a white runtime shader;
        // the room remains visible behind it while the mark fades in.
        image.color = new Color(.93f, .92f, .88f, .98f);
        var rt = image.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        overlayText = Text(overlay.transform, "Menu text", new Vector2(.5f, .5f), Vector2.zero, new Vector2(1440, 760), 20, TextAnchor.MiddleCenter);
        overlayText.color = C("0A0A0A");
        overlayText.rectTransform.anchoredPosition = new Vector2(0f, -170f);
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
            settingsRect.sizeDelta = new Vector2(920f, 720f);
        }
        logoImage = Panel(overlay.transform, "FrontRooms brand logo", new Vector2(.5f, .5f), Vector2.zero, new Vector2(965f, 192f), Color.white).GetComponent<Image>();
        logoImage.raycastTarget = false;
        logoOutline = logoImage.gameObject.AddComponent<Outline>();
        logoOutline.effectDistance = new Vector2(2f, -2f);
        logoOutline.effectColor = new Color(1f, .86f, .34f, 0f);
        LoadBrandLogo();
        UpdateDisplaySettingsText();
        EnsureMobileTouchLayer();
    }
    void SetPhase(Phase p)
    {
        var wasPlaying = phase == Phase.Playing;
        var wasPaused = phase == Phase.Paused;
        phase = p; bool playing = p == Phase.Playing;
        if ((p == Phase.Paused) != wasPaused) Paused?.Invoke(p == Phase.Paused);
        if (playing && !wasPlaying) gameplayHudAlpha = 0f;
        if (!playing) gameplayHudAlpha = 0f;
        if (p != Phase.Paused)
        {
            displaySettingsOpen = false;
            if (displaySettingsPanel != null) displaySettingsPanel.SetActive(false);
        }
        overlay.SetActive(!playing); if (crosshairImage != null) crosshairImage.enabled = playing;
        if (overlayImage != null) overlayImage.color = p == Phase.Title ? new Color(0f, 0f, 0f, 0f) : new Color(.93f, .92f, .88f, .98f);
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
        if (roomPanel != null) roomPanel.SetActive(playing);
        if (threatPanel != null) threatPanel.SetActive(playing);
        if (keyPanel != null) keyPanel.SetActive(playing);
        if (contextPanel != null) contextPanel.SetActive(false);
        ApplyGameplayHudAlpha();
        Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !playing;
        if (p == Phase.Title)
        {
            overlayText.text = "";
            overlayText.enabled = false;
        }
        else overlayText.enabled = true;
        if (p == Phase.Paused) overlayText.text = PauseText();
        // Re-locking the cursor can report one large mouse jump: ignore the first frames back.
        if (playing && wasPaused) mouseSettleFrames = 2;
        if (p == Phase.Caught)
            overlayText.text = "<size=88><b>CAUGHT</b></size>\n\n<size=24>" + Mathf.RoundToInt(elapsed) + " S  /  TIER " + tier + "  /  " + zonesVisited.Count + " ZONES  /  " + keysTaken + " KEYS  /  " + (relay == null ? 0 : relay.DoorsBroken) + " DOORS BROKEN</size>\n\n<color=#F4DF3B><size=20>R  TRY AGAIN</size></color>";
        UpdateMobileTouchMenu();
    }
    void OnApplicationFocus(bool focused)
    {
#if UNITY_EDITOR
        if (autopilot) return;
#endif
        if (!focused && phase == Phase.Playing) SetPhase(Phase.Paused);
    }

    void OnApplicationPause(bool paused)
    {
#if UNITY_EDITOR
        if (autopilot) return;
#endif
        if (paused && phase == Phase.Playing) SetPhase(Phase.Paused);
    }
    void Update()
    {
        if (!Application.isPlaying) return;
        var input = FrontRoomsInput.ReadFrame();
        if (phase == Phase.Title && input.StartDown) RequestTitleStart();
        else if (phase == Phase.Paused && input.SettingsDown) ToggleDisplaySettings();
        else if (input.PauseDown && displaySettingsOpen) ToggleDisplaySettings();
        else if (displaySettingsOpen) HandleSettingsKeys();
        else if (input.PauseDown && (phase == Phase.Playing || phase == Phase.Paused)) SetPhase(phase == Phase.Playing ? Phase.Paused : Phase.Playing);
        // R restarts from the pause or caught card, never while the settings panel is open.
        // Touch restart gets one confirmation step because it is easy to hit while
        // reaching for the pause card; desktop keyboard and the caught retry chip
        // retain their immediate restart behavior.
        if (!displaySettingsOpen && input.RestartDown && phase != Phase.Playing && phase != Phase.Title)
        {
            if (phase == Phase.Paused && FrontRoomsInput.VirtualSnapshotActive)
            {
                if (mobileRestartConfirmOpen) ConfirmMobileRestart();
                else RequestMobileRestartConfirmation();
            }
            else
            {
                restart = true;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
        var dt = Mathf.Min(Time.deltaTime, .1f);
#if UNITY_EDITOR
        if (autopilot) AutopilotTick(dt);
#endif
        if (phase == Phase.Title) UpdateTitleSequence(dt);
        if (logoFading) UpdateLogoFade(dt);
        if (phase == Phase.Playing && mapPlay)
        {
            elapsed += dt;
            flashTime -= dt;
            FrontRoomsCaptions.Tick(dt);
            for (var i = rattleJolts.Count - 1; i >= 0; i--)
            {
                if (elapsed < rattleJolts[i].at) continue;
                Jolt(rattleJolts[i].p, DoorJoltRange, FrontRoomsShotTimings.Rattle.CameraImpulseDeg, FrontRoomsShotTimings.Open.PushImpulseDecay);
                rattleJolts.RemoveAt(i);
            }
            UpdateMapPlay(dt);
        }
        UpdateHud();
    }
    void UpdateHud()
    {
        bool play = phase == Phase.Playing;
        if (play)
            gameplayHudAlpha = Mathf.MoveTowards(gameplayHudAlpha, 1f, Time.unscaledDeltaTime / GameplayHudFadeSeconds);
        else
            gameplayHudAlpha = 0f;
        ApplyGameplayHudAlpha();
        if (!mapPlay || map == null || playerRoot == null) return;
        // In the stream rooms the run starts in: Level 0's lobby, before zone 01.
        var zone = inStartRooms ? new ZoneInfo { height = ZoneHeight.Standard, theme = ZoneTheme.Level0 } : map.ZoneOf(map.CellOf(playerRoot.position));
        var released = relay != null && relay.Released;
        var threat = !released ? "" : relay.State == HunterState.BreakDoor ? "RELAY  /  BREAKING DOOR" : "RELAY  /  " + relay.State.ToString().ToUpperInvariant();
        roomMetaText.text = play ? "ZONE " + zonesVisited.Count.ToString("00") + "  /  TIER " + tier + "  /  " + zone.height.ToString().ToUpperInvariant() + "  " + MapGrid.CeilingHeight(zone.height).ToString("0.0") + " M" : "";
        roomText.text = play ? (inStartRooms ? "LEVEL 0 / THE LOBBY" : ZoneName(zone)) : "";
        // The Relay's state and distance are an assist, off by default.
        var showThreat = play && released && FrontRoomsSettings.RelayReadout;
        threatStateText.text = showThreat ? threat : "";
        distanceText.text = showThreat ? "RELAY  " + Mathf.RoundToInt(RelayDistance()) + " M" : "";
        if (crosshairImage != null) crosshairImage.enabled = play;
        // The hint card is timed: the first-run hint, then only flashes.
        var hint = flashTime > 0f && !string.IsNullOrEmpty(flash) ? flash : elapsed < CalmHintSeconds ? CalmHint : "";
        contextText.text = play ? hint : "";
        if (promptText != null) promptText.text = play && prompt != null ? prompt : "";
        if (holdBar != null)
        {
            holdBar.SetActive(play && holdProgress > 0f);
            holdBarFill.rectTransform.sizeDelta = new Vector2(120f * holdProgress, 4f);
        }
        UpdateMobileTouchPrompt();
        var tired = play && stamina < StaminaSeconds - .01f;
        for (var i = 0; i < staminaSegments.Length; i++)
        {
            if (staminaSegments[i] == null) continue;
            staminaSegments[i].enabled = tired;
            staminaSegments[i].color = stamina >= (i + .5f) * (StaminaSeconds / staminaSegments.Length) ? new Color(.957f, .875f, .231f) : new Color(1f, 1f, 1f, .25f);
        }
        if (roomPanel != null) roomPanel.SetActive(play);
        if (threatPanel != null) threatPanel.SetActive(showThreat);
        var showKey = play && !inStartRooms && map.HasKeyFor(zone.id);
        if (showKey && keyText != null && (!keyLabelSet || !zone.id.Equals(keyLabelZone)))
        {
            keyLabelZone = zone.id;
            keyLabelSet = true;
            keyText.text = KeyLabel(zone);
        }
        if (keyPanel != null) keyPanel.SetActive(showKey);
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
        ((RectTransform)captionPanel.transform).sizeDelta = new Vector2(920f, 20f + 28f * lines.Count);
    }

    // The zone the key panel's label was written for (written again only when it changes).
    GridCoord keyLabelZone;
    bool keyLabelSet;

    /// <summary>What the key panel calls the key it shows: the zone it belongs to (audit 1.8).</summary>
    static string KeyLabel(ZoneInfo zone) => ZoneName(zone) + " KEY";

    float RelayDistance() => relay == null || !relay.Released ? -1f : Vector2.Distance(playerPos, Flat(relay.Position));
    void ApplyGameplayHudAlpha()
    {
        // A takeover fades the HUD, but never the hint card (it carries captions).
        var hud = gameplayHudAlpha * (rig != null ? 1f - rig.HudFade : 1f);
        if (roomHudGroup != null) roomHudGroup.alpha = hud;
        if (threatHudGroup != null) threatHudGroup.alpha = hud;
        if (contextHudGroup != null) contextHudGroup.alpha = gameplayHudAlpha;
        if (captionHudGroup != null) captionHudGroup.alpha = gameplayHudAlpha;
        if (crosshairHudGroup != null) crosshairHudGroup.alpha = hud;
        if (keyHudGroup != null) keyHudGroup.alpha = hud;
    }
    void Flash(string message, float duration = 3f) { flash = message; flashTime = duration; }

    /// <summary>A camera shake (picture only) when something happens within <paramref name="range"/> metres of the player.</summary>
    void Jolt(Vector3 at, float range, float degrees, float decaySeconds)
    {
        if (rig == null || playerRoot == null || phase != Phase.Playing) return;
        if (Flat(at - playerRoot.position).sqrMagnitude > range * range) return;
        rig.AddTrauma(degrees, decaySeconds);
    }

    /// <summary>How much faster than the base the tier makes the chase: the run cycle and its step clock follow, so the feet keep up (1 at tier 1).</summary>
    float ChasePace => relayTuning.chaseSpeed / Mathf.Max(.01f, hunterTuning.chaseSpeed);

    /// <summary>The Relay's tuning this frame: the Inspector's base, scaled by the tier's row. Base edits and tier changes both apply on the next frame and never compound.</summary>
    void UpdateRelayTuning()
    {
        relayTuning.CopyFrom(hunterTuning);
        TierRules.At(tier).ApplyTo(relayTuning);
    }

    /// <summary>Raise the run's tier to <paramref name="next"/> (never lower, capped at the table): chunks generated from now on take it too.</summary>
    void RaiseTier(int next, string why)
    {
        next = Mathf.Min(next, TierRules.MaxTier);
        if (next <= tier) return;
        tier = next;
        if (map != null) map.GenerationTier = tier;
        Event("tier", tier + " (" + why + ")");
        Log("TIER " + tier + " · " + why + " · " + zonesVisited.Count + " zones, " + elapsed.ToString("0.0", CultureInfo.InvariantCulture) + " s");
#if UNITY_EDITOR
        if (autopilot) autoTiers.Add(autoPlayClock.ToString("0.0", CultureInfo.InvariantCulture) + " s tier " + tier + " (" + why + ", " + zonesVisited.Count + " zones)");
#endif
        TierChanged?.Invoke(tier);
    }
    void Event(string kind, string detail) { events.Add(elapsed.ToString("0.000", CultureInfo.InvariantCulture) + "," + kind + ",\"" + detail.Replace("\"", "\"\"") + "\"," + RelayDistance().ToString("0.00", CultureInfo.InvariantCulture)); }
    void OnDestroy()
    {
#if UNITY_EDITOR
        AutoBaseRestoreTime();
#endif
        if (mobileTouch != null)
        {
            mobileTouch.SettingsRowRequested -= HandleMobileSettingsRow;
            mobileTouch.RestartCancelRequested -= CancelMobileRestartConfirmation;
            mobileTouch.LookTapped -= HandleMobileLookTap;
        }
        if (mobileBackBridge != null)
            FrontRoomsMobileBackBridge.BackPressed -= HandleMobileBack;
        PlayerStamina01 = 1f;
        PlayerWinded = PlayerSprinting = false;
        if (phase == Phase.Paused) Paused?.Invoke(false);
        if (map != null) MapRunEnded?.Invoke();
    }

    void End()
    {
        if (phase != Phase.Playing) return;
        SetPhase(Phase.Caught); FrontRoomsMobileInteractionEvents.Caught(); Sound(caughtClip, playerPos); Event("outcome", "caught");
        string dir = Application.persistentDataPath; Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "events-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv"), "time_s,event,detail,relay_m\n" + string.Join("\n", events));
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
        public List<string> tierLog = new List<string>();
        public int maxChunksBuilt;
        public int routes;
        public int doorsOpened;
        public int keysTaken;
        public float relayReleasedAt = -1f;
        public float closestRelayMetres;
        public string relayFinalState;
        public int relayDoorsBroken;
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
    }

    readonly List<string> autoFrameNames = new List<string>();

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
        autoOutDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "main-autopilot");
        Directory.CreateDirectory(autoOutDir);
        foreach (var old in Directory.GetFiles(autoOutDir, "*.png")) File.Delete(old);
        Application.logMessageReceived += AutopilotLog;
        Log("AUTOPILOT on · frames to " + autoOutDir);
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
        if (autoPlayClock > .6f && autoPlayClock >= autoNextShot && !autoGlassActive)
        {
            autoNextShot = autoPlayClock + 9f;
            AutopilotCapture((autoShots < 10 ? "0" : "") + autoShots + "_play_" + Mathf.RoundToInt(autoPlayClock) + "s");
        }
        if (phase == Phase.Caught) AutopilotFinish("caught");
        else if (autoPlayClock >= (autoBaseline ? autoBotSeconds : AutopilotPlaySeconds)) AutopilotFinish("time");
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
        if (autoRouteIndex >= autoRoute.Count) AutopilotPlan(here);
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
        if (cam == null) return;
        AutopilotRender(cam, name);
    }

    /// <summary>One view from a point towards another, with the player's lens.</summary>
    void AutopilotLookFrom(Vector3 from, Vector3 target, string label)
    {
        if (cam == null) return;
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
        if (cam == null) return;
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
        if (relay == null || cam == null) return;
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

    void AutopilotFinish(string reason)
    {
        if (autoFinished) return;
        autoFinished = true;
        var report = new AutopilotReport
        {
            seed = runSeed,
            playSeconds = autoPlayClock,
            distanceWalked = autoDistance,
            cellsVisited = autoCells.Count,
            zonesVisited = zonesVisited.Count,
            tier = tier,
            tierLog = new List<string>(autoTiers),
            maxChunksBuilt = autoMaxChunks,
            routes = autoRoutes,
            doorsOpened = autoDoorsOpened,
            keysTaken = keysTaken,
            relayReleasedAt = autoReleaseTime,
            closestRelayMetres = autoMinRelay == float.MaxValue ? -1f : autoMinRelay,
            relayFinalState = relay == null ? "none" : relay.State.ToString(),
            relayDoorsBroken = relay == null ? 0 : relay.DoorsBroken,
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
        };
        var reached = mapPlay && autoDistance > 20f && autoCells.Count > 8;
        if (autoGlassWanted && !autoGlassDone) AutoGlassCheck(false, "glass: the scenario did not finish (step " + autoGlassStep + ")");
        var glassOk = !autoGlassWanted || autoGlassChecks.TrueForAll(c => c.StartsWith("ok"));
        report.verdict = (reached && autoErrors == 0 && relay != null && relay.Released && glassOk ? "PASS" : "FAIL") + " · ended by " + reason;
        if (autoBaseline) report.verdict = AutoBaseFinish(reason);
        File.WriteAllText(Path.Combine(autoOutDir, "report.json"), JsonUtility.ToJson(report, true));
        var done = Path.Combine(Directory.GetParent(Application.dataPath).FullName, AutopilotDoneFile);
        Directory.CreateDirectory(Path.GetDirectoryName(done));
        File.WriteAllText(done, report.verdict);
        Log("AUTOPILOT " + report.verdict);
    }
#endif
}
