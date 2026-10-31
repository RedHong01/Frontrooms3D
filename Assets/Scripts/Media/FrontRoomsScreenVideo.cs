using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

/// <summary>
/// A diegetic CRT screen surface. The video decoder is shared by screens using
/// the same ad bank, while each screen keeps its own power and focus state.
/// This is deliberately a surface component, not a full-screen UI: a later
/// interactive overlay can sit on the same screen collider without baking
/// controls into the advertisement video.
/// </summary>
[DisallowMultipleComponent]
public sealed class FrontRoomsScreenVideo : MonoBehaviour
{
    public const string DefaultStreamFile = "FrontRooms_Ad_01.mp4";
    public const float DefaultVisibleDistance = 18f;
    public const float DefaultInteractionDistance = 2.4f;

    // Player-safe surface material (codex audit F6). A material asset under Resources keeps
    // its shader (the CRT tube, FrontRooms/ScreenVideo, since 2026-10-22) in every player build;
    // a Shader.Find by name alone does not.
    const string AdSurfaceTemplatePath = "Media/CRT_AdSurface";
    const string AdSurfaceShaderPath = "Rendering/FrontRoomsScreenVideo";

    static readonly List<FrontRoomsScreenVideo> ActiveScreens = new List<FrontRoomsScreenVideo>();
    static FrontRoomsScreenVideo focusedScreen;
    static Camera fallbackCamera;
    static Material adSurfaceTemplate;
    static bool warnedNoSurfaceMaterial;

    // The world hold (research/inspect/contracts/map_inspect.md C26). A close-up
    // (FrontRooms3DGame.InspectingNow) or the pause card holds the world: no screen
    // takes focus or answers E, and no bank plays. A43 (2026-10-30): a looking close-up
    // that runs live holds focus and E only (InputHeld), never playback (WorldHeld).
    // Paused is an event, so its last value is kept here; the game sends false on
    // resume and on teardown.
    static bool pausedByGame;
    static int heldFrame = -10;

    // R3 (Red 10-29; research/inspect/NOTE_1029_R3_station_tv.md): the close-up of the playing station TV is the hold's
    // one exemption. The screen whose READ target is FrontRoomsInspect.Current, cached per target.
    static FrontRoomsInspectable inspectedTarget;
    static FrontRoomsScreenVideo inspectedScreen;

    /// <summary>
    /// The pause card or a close-up that pauses the world holds it (C26): no bank plays and a power change waits. A43 (Red
    /// 2026-10-30): a looking close-up the game keeps live (FrontRoomsInspect.WorldRuns, the map's A43 contract) holds
    /// nothing, so the screens around it keep playing; without that contract every close-up pauses and holds, as before.
    /// </summary>
    internal static bool WorldHeld => pausedByGame || (FrontRooms3DGame.InspectingNow && !FrontRoomsInspect.WorldRuns);

    /// <summary>The pause card or any close-up, paused or live: no screen takes focus or answers E (the close-up owns E), and
    /// the station TV's close-up keeps the distance it was opened with.</summary>
    internal static bool InputHeld => pausedByGame || FrontRooms3DGame.InspectingNow;

    /// <summary>
    /// R3: the screen in the close-up, or null. It is the playing station TV whose own READ target the game opened
    /// (InspectingNow, FrontRoomsInspect.Current). Null during the return, which runs after InspectingNow goes false.
    /// </summary>
    public static FrontRoomsScreenVideo InspectedScreen
    {
        get
        {
            if (!FrontRooms3DGame.InspectingNow) return null;
            var t = FrontRoomsInspect.Current;
            if (t == null) return null;
            if (t != inspectedTarget)
            {
                inspectedTarget = t;
                var s = t.GetComponentInParent<FrontRoomsScreenVideo>();
                inspectedScreen = s != null && s.inspectTarget == t ? s : null;
            }
            return inspectedScreen;
        }
    }

    /// <summary>R3: this bank feeds the screen in the close-up.</summary>
    internal static bool IsCloseUpBank(FrontRoomsScreenAdBank bank)
    {
        if (bank == null) return false;
        var s = InspectedScreen;
        return s != null && s.bank == bank;
    }

    /// <summary>
    /// R3: whether the hold pauses this bank. <see cref="WorldHeld"/> for every bank, except the close-up's own bank,
    /// which keeps playing through the close-up (the pause card over the close-up still holds it).
    /// </summary>
    internal static bool HeldFor(FrontRoomsScreenAdBank bank) => WorldHeld && (pausedByGame || !IsCloseUpBank(bank));

    // Hooked once per play session, before any scene loads, so a screen spawned
    // under the pause card still sees it (FrontRoomsInspect hooks the same way).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void HookWorldHold()
    {
        pausedByGame = false;
        heldFrame = -10;
        inspectedTarget = null;
        inspectedScreen = null;
        FrontRooms3DGame.Paused -= OnGamePaused;
        FrontRooms3DGame.Paused += OnGamePaused;
        // R3: the close-up's bank changes clock in the frame the close-up opens and in the frame it ends.
        FrontRooms3DGame.Inspecting -= OnGameInspecting;
        FrontRooms3DGame.Inspecting += OnGameInspecting;
    }

    static void OnGamePaused(bool paused)
    {
        pausedByGame = paused;
        // V5: the pause card's release puts each sounding picture in view back on the broadcast (once; none while still held).
        if (!paused) FrontRoomsScreenAdBank.SyncToSoundOnRelease();
    }

    static void OnGameInspecting(bool inspecting)
    {
        // V5: the close-up's release likewise, before its own bank leaves the close-up clock (that bank is never sought).
        if (!inspecting) FrontRoomsScreenAdBank.SyncToSoundOnRelease();
        FrontRoomsScreenAdBank.SyncCloseUpClocks();
    }

    [SerializeField] string streamFile = DefaultStreamFile;
    [SerializeField] VideoClip desktopClip;
    [SerializeField] Vector3 screenAnchor = new Vector3(0f, .248f, .194f);
    [SerializeField] Vector2 screenSize = new Vector2(.28f, .19f);
    [SerializeField] float visibleDistance = DefaultVisibleDistance;
    [SerializeField] float interactionDistance = DefaultInteractionDistance;
    [SerializeField] bool startsPowered = true;

    GameObject surfaceObject;
    MeshRenderer surfaceRenderer;
    BoxCollider surfaceCollider;
    Material surfaceMaterial;
    FrontRoomsScreenAdBank bank;
    bool poweredOn;
    bool countedVisible;

    // V4 still desk screens (tutorial T3; FrontRoomsDeskStill, FrontRoomsStationPresence). Only an Office desk's CRT
    // inside the maze (AttachDesk under a FrontRoomsMapWorld) ever goes still, and only on a floor with a station;
    // the title stream's rooms, the demo rig and any other screen (the station TV) keep the ad.
    bool deskScreen, deskInMaze, stillMode, stillForcedOff;
    int deskSeed;
    Vector3 deskMapLocal;
    FrontRoomsDeskStill.Frame stillFrame;
    Texture stillTexture;

    // R3: the READ target on the surface (made the first time this is the playing station TV), and whether the screen
    // sits under a FrontRoomsMapWorld (resolved once, again after a reparent).
    FrontRoomsInspectable inspectTarget;
    InspectDef inspectDef;
    FrontRoomsMapWorld mapWorld;
    bool mapResolved, inMap;

    public bool IsPoweredOn => poweredOn;
    public bool IsFocused => focusedScreen == this;
    public string InteractionPrompt => poweredOn ? "E  TURN OFF" : "E  TURN ON";
    public float InteractionDistance => interactionDistance;
    /// <summary>V4: this desk CRT shows a still frame instead of the ad (a station floor).</summary>
    public bool IsStill => stillMode;
    /// <summary>V4: the still frame this desk shows while <see cref="IsStill"/> (Off = a set left off).</summary>
    public FrontRoomsDeskStill.Frame StillFrame => stillFrame;
    /// <summary>V4: an Office desk's CRT in the maze, which goes still on a floor with a station.</summary>
    public bool IsMazeDesk => deskScreen && deskInMaze;

    /// <summary>
    /// R3 (Red 10-29): this screen is the floor's playing station TV: powered on and playing the ad (not a still, and its
    /// bank has put the ad's first frame on the screen), not an Office desk's CRT, inside the map (under a
    /// FrontRoomsMapWorld), on a floor with a station whose key is not taken yet (FrontRoomsStationPresence.FloorHasStation,
    /// StationKeyIsTaken). Only then does it carry a READ target (the look-eye, E opens a paused close-up of its screen).
    /// Every other screen keeps its own E.
    /// </summary>
    public bool IsPlayingStationTv => IsStationTv && bank.HasPicture;

    /// <summary>
    /// R3: <see cref="IsPlayingStationTv"/> without the first-frame term. While this holds the screen's E is the close-up's,
    /// never TURN OFF, so a TV still waiting for its first frame (no eye yet) never falls through to its power. A stream
    /// that failed to prepare is not playing the ad: that TV keeps its own E until the bank's retry prepares it.
    /// </summary>
    bool IsStationTv => !deskScreen && poweredOn && !stillMode && surfaceRenderer != null && bank != null && !bank.Failed
        && FrontRoomsStationPresence.FloorHasStation && !FrontRoomsStationPresence.StationKeyIsTaken && InMap;

    /// <summary>R3: the screen's READ target while it is the playing station TV (enabled = registered), else null or disabled.</summary>
    public FrontRoomsInspectable InspectTarget => inspectTarget;

    /// <summary>Attach the runtime surface to an authored or procedurally spawned CRT.</summary>
    public static FrontRoomsScreenVideo Attach(GameObject monitor, string adFile = null, FrontRoomsScreenSoundRole soundRole = FrontRoomsScreenSoundRole.None)
    {
        if (monitor == null) return null;
        var screen = monitor.GetComponent<FrontRoomsScreenVideo>();
        if (screen == null) screen = monitor.AddComponent<FrontRoomsScreenVideo>();
        if (!string.IsNullOrEmpty(adFile)) screen.streamFile = adFile;
        // V5: set at Attach (None leaves a role given earlier, as an empty adFile leaves the file).
        if (soundRole != FrontRoomsScreenSoundRole.None) screen.SoundRole = soundRole;
        return screen;
    }

    /// <summary>
    /// An Office desk's CRT (FrontRoomsOfficeKit). Inside the maze it shows a still frame on a floor with a station
    /// (V4); a desk built on such a floor starts still, before it is ever drawn.
    /// </summary>
    public static FrontRoomsScreenVideo AttachDesk(GameObject monitor)
    {
        var screen = Attach(monitor);
        if (screen != null) screen.MarkDesk();
        return screen;
    }

    void Awake()
    {
        poweredOn = startsPowered;
        if (!Application.isPlaying) return;
        bank = FrontRoomsScreenAdBank.Acquire(streamFile, desktopClip);
        CreateSurface();
        // A desk marked while its monitor was inactive (Awake deferred) takes its mode before its first frame.
        if (deskScreen && !stillMode && WantsStill()) EnterStill(ResolveCamera());
    }

    void OnEnable()
    {
        if (!Application.isPlaying) return;
        if (!ActiveScreens.Contains(this)) ActiveScreens.Add(this);
        if (bank != null) bank.Register(this);
    }

    void Start()
    {
        if (!Application.isPlaying) return;
        if (bank == null) bank = FrontRoomsScreenAdBank.Acquire(streamFile, desktopClip);
        if (surfaceObject == null) CreateSurface();
        bank.Register(this);
    }

    void OnDisable()
    {
        if (bank != null)
        {
            bank.SetVisible(this, false);
            bank.Unregister(this);
        }
        if (ActiveScreens.Contains(this)) ActiveScreens.Remove(this);
        if (focusedScreen == this) focusedScreen = null;
    }

    void OnDestroy()
    {
        if (surfaceMaterial != null)
        {
            if (Application.isPlaying) Destroy(surfaceMaterial);
            else DestroyImmediate(surfaceMaterial);
        }
    }

    void Update()
    {
        if (!Application.isPlaying || bank == null || surfaceRenderer == null) return;
        var camera = ResolveCamera();
        if (deskScreen) UpdateDeskStill(camera);
        else UpdateInspectTarget();
        // A still desk never asks the shared decoder to play.
        var visible = !stillMode && terminalPicture == null && poweredOn && camera != null && surfaceRenderer.isVisible &&
            Vector3.Distance(camera.transform.position, surfaceRenderer.bounds.center) <= visibleDistance;
        if (visible != countedVisible)
        {
            countedVisible = visible;
            bank.SetVisible(this, visible);
            // V5: a sounding screen coming into view puts the shared picture on the broadcast's clock (once per edge).
            if (visible && SoundRole != FrontRoomsScreenSoundRole.None) bank.SyncToSound(this);
        }
    }

    void LateUpdate()
    {
        if (!Application.isPlaying || bank == null || surfaceRenderer == null) return;
        // One screen owns the raycast for the whole family. This gives every
        // monitor a shared focus/interaction path instead of N identical rays.
        // LateUpdate (C26): after every Update, so the E press that opens a
        // close-up is already in InspectingNow whatever the script order.
        if (ActiveScreens.Count > 0 && ActiveScreens[0] == this) UpdateFocusedScreen(ResolveCamera());
    }

    static Camera ResolveCamera()
    {
        var camera = Camera.main;
        if (camera != null) return camera;
        if (fallbackCamera == null) fallbackCamera = FindFirstObjectByType<Camera>();
        return fallbackCamera;
    }

    static void UpdateFocusedScreen(Camera camera)
    {
        // C26: no ray while the world is held, and none on the frame it is
        // released, so the E that leaves a close-up never switches a CRT too.
        if (InputHeld) heldFrame = Time.frameCount;
        if (Time.frameCount - heldFrame <= 1)
        {
            focusedScreen = null;
            return;
        }
        FrontRoomsScreenVideo next = null;
        var maxDistance = DefaultInteractionDistance;
        for (var i = 0; i < ActiveScreens.Count; i++)
        {
            if (ActiveScreens[i] != null)
                maxDistance = Mathf.Max(maxDistance, ActiveScreens[i].interactionDistance);
        }
        if (camera != null && Physics.Raycast(camera.transform.position, camera.transform.forward,
            out var hit, maxDistance, ~0, QueryTriggerInteraction.Collide))
        {
            next = hit.collider.GetComponentInParent<FrontRoomsScreenVideo>();
            if (next != null && hit.distance > next.interactionDistance) next = null;
            // R3: the station TV's E is its READ close-up, never TURN OFF, so a press the game refuses (cooldown, a Relay
            // nearby, the first frame not shown yet) never falls through to the power switch (EXAMINE C5).
            if (next != null && next.IsStationTv) next = null;
            // station-look-v1: a set powered off for good (the station TV at the take) offers no E at all.
            if (next != null && next.offForGood) next = null;
            // terminal: a terminal's E is its READ close-up (the EXAMINE extension), never TURN OFF.
            if (next != null && next.terminalPicture != null) next = null;
        }

        focusedScreen = next;
        if (focusedScreen != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            focusedScreen.TogglePower();
    }

    void TogglePower()
    {
        if (offForGood) return;
        // The player's choice: a set the still mix left off is now the player's to keep on or off.
        stillForcedOff = false;
        SetPower(!poweredOn);
    }

    // station-look-v1 (A26; README_V4 "After the key is taken the station TV goes off for good (V1's job)"; plan 70 §8
    // take row, plan check C1-1): the one public power call. The station dresser calls it on map.StationKeyTaken. It goes
    // through SetPower, so a power-off effect hooked there (crt-tube's collapse) plays; afterwards neither E nor the still
    // mix turns the set back on.
    bool offForGood;

    /// <summary>Power the set off for the rest of its life (the station TV at the key take). Idempotent.</summary>
    public void PowerOffForGood()
    {
        offForGood = true;
        stillForcedOff = false;
        if (poweredOn) SetPower(false);
        if (focusedScreen == this) focusedScreen = null;
    }

    /// <summary>True once <see cref="PowerOffForGood"/> was called.</summary>
    public bool IsOffForGood => offForGood;

    void SetPower(bool on)
    {
        poweredOn = on;
        if (surfaceRenderer != null) surfaceRenderer.enabled = poweredOn;
        if (!poweredOn)
        {
            countedVisible = false;
            bank?.SetVisible(this, false);
        }
    }

    // ---------------------------------------------------------------- R3 the playing station TV's READ target

    /// <summary>Whether a FrontRoomsMapWorld is above this screen (the station is a map room; the title rooms are not).</summary>
    bool InMap
    {
        get
        {
            if (!mapResolved)
            {
                mapResolved = true;
                mapWorld = GetComponentInParent<FrontRoomsMapWorld>(true);
                inMap = mapWorld != null;
            }
            return inMap;
        }
    }

    void OnTransformParentChanged() => mapResolved = false;

    /// <summary>The READ target exists only while this is the playing station TV: power off, a still, no station, the key taken or no picture removes it.</summary>
    void UpdateInspectTarget()
    {
        var want = IsPlayingStationTv;
        if (inspectTarget == null)
        {
            if (!want) return;
            inspectTarget = MakeInspectTarget();
            if (inspectTarget == null) return;
        }
        if (inspectTarget.enabled != want) inspectTarget.enabled = want;
        // Never under a close-up (paused or live) or the pause card: the close-up's own solve stays as it was opened.
        if (want && !InputHeld) FitCloseUpDistance();
    }

    /// <summary>Added to the body-band distance (m), so the solve lands inside the band, not on its edge.</summary>
    const float CloseUpSlack = .05f;

    /// <summary>
    /// The close-up camera stays at most FrontRoomsShotTimings.Inspect.MaxRise above the player's eye and turns at most the
    /// face's maxAngle from its normal. A screen hung above that (A26's station TV: bottom at least 2.15 m, tilted 20 deg
    /// down) is therefore framed from far enough back for the camera to look up at it: the base distance (InspectDef.dist)
    /// = the face's height over the band / sin(maxAngle - the normal's pitch), plus CloseUpSlack, within minDist..maxDist.
    /// A screen at or below the band keeps the default solve (no base distance). The eye: the map's floor + PlayerEye.
    /// </summary>
    void FitCloseUpDistance()
    {
        if (inspectTarget == null || inspectDef == null || inspectDef.dist == null || inspectDef.dist.Length == 0) return;
        var face = inspectTarget.Face;
        var floorY = mapWorld != null ? mapWorld.transform.position.y : 0f;
        var rise = face.position.y - (floorY + FrontRooms.Map.ModuleUnits.PlayerEye + FrontRoomsShotTimings.Inspect.MaxRise);
        var d = 0f;
        if (rise > .02f)
        {
            var normalY = (face.rotation * Vector3.forward).y;
            var pitch0 = Mathf.Asin(Mathf.Clamp(normalY, -1f, 1f)) * Mathf.Rad2Deg;   // < 0 for a screen tilted down
            var down = Mathf.Clamp(inspectDef.MaxAngleFor(face.position.y) - pitch0, 5f, 85f);
            d = Mathf.Clamp(rise / Mathf.Sin(down * Mathf.Deg2Rad) + CloseUpSlack, inspectDef.MinDist, inspectDef.MaxDist);
        }
        inspectDef.dist[0] = d;
    }

    /// <summary>The aim box's depth behind the screen face (m): the glass, not the cabinet.</summary>
    const float StationTvBoxDepth = .02f;

    /// <summary>
    /// R3: the station TV's READ block (a fresh one per screen). dist[0] (the base distance) is FitCloseUpDistance's: 0 = the
    /// default solve. A43: a "read" target with no transcript, so its close-up runs live (the ad is watched, not read
    /// carefully; research/inspect/NOTE_1030_a43_pause_sort.md).
    /// </summary>
    public static InspectDef StationTvDef() => new InspectDef
    {
        kind = "flat", verb = "read", ship = true, sound = "plastic", dist = new float[1],
        // The one target that keeps the traditional angle (Red 2026-10-30): you look up at it from where you stand.
        maxYawOff = FrontRoomsShotTimings.Inspect.MaxYawOff, maxRise = FrontRoomsShotTimings.Inspect.MaxRise,
    };

    /// <summary>
    /// The READ target on the screen surface (a child of this screen, so FrontRoomsInspect.Current.GetComponentInParent
    /// finds the screen): the face is the surface quad at the screen anchor, screenSize wide and high, facing the
    /// monitor's +Z, text-up = the monitor's up. A flat EXAMINE target with the default numbers (reach 1.6 m, 60°), and a
    /// base distance only for a screen hung above the eye (FitCloseUpDistance).
    /// </summary>
    FrontRoomsInspectable MakeInspectTarget()
    {
        if (surfaceObject == null) return null;
        var target = surfaceObject.GetComponent<FrontRoomsInspectable>();
        if (target == null) target = surfaceObject.AddComponent<FrontRoomsInspectable>();
        var w = screenSize.x * .5f;
        var h = screenSize.y * .5f;
        var face = new FrontRoomsKitLibrary.Info
        {
            boundsMin = new[] { -w, -h, -StationTvBoxDepth },
            boundsMax = new[] { w, h, 0f },
        };
        inspectDef = StationTvDef();
        target.Configure(FrontRoomsInspectCatalog.StationTvKit, inspectDef, face);
        inspectTarget = target;
        FitCloseUpDistance();
        target.EnsureRegistered();   // OnEnable ran inside AddComponent, before the data was bound
        return target;
    }

    // ---------------------------------------------------------------- V4 still desk screens

    void MarkDesk()
    {
        deskScreen = true;
        var map = GetComponentInParent<FrontRoomsMapWorld>(true);
        deskInMaze = map != null;
        if (!deskInMaze) return;
        deskSeed = map.Seed;
        deskMapLocal = map.transform.InverseTransformPoint(transform.position);
        // Built on a station floor: still from the start (never drawn yet). If Awake has not run yet, it does this.
        if (surfaceObject != null && !stillMode && WantsStill()) EnterStill(ResolveCamera());
    }

    bool WantsStill() => deskInMaze && FrontRoomsStationPresence.FloorHasStation && terminalPicture == null;

    /// <summary>Switches between the ad and the still frame when the floor's answer changes, only while nobody can see the screen.</summary>
    void UpdateDeskStill(Camera camera)
    {
        if (WantsStill() == stillMode) return;
        if (SeenBy(camera)) return;
        if (stillMode) ExitStill(); else EnterStill(camera);
    }

    /// <summary>
    /// Whether the player could see this screen now: inside the game camera's frustum and drawn last frame (a lit
    /// surface that no camera drew, or one outside the frustum, is not seen). An unlit (off) surface counts as seen
    /// whenever it is in the frustum, since turning it on would show.
    /// </summary>
    bool SeenBy(Camera camera)
    {
        if (camera == null || surfaceObject == null) return false;
        if (surfaceRenderer != null && surfaceRenderer.enabled && !surfaceRenderer.isVisible) return false;
        var size = Mathf.Max(screenSize.x, screenSize.y) + .05f;
        return FrontRoomsDeskStill.InView(camera, new Bounds(surfaceObject.transform.position, new Vector3(size, size, size)));
    }

    void EnterStill(Camera camera)
    {
        if (surfaceMaterial == null) return;
        var frame = FrontRoomsDeskStill.Pick(deskSeed, FrontRoomsStationPresence.Floor, deskMapLocal);
        var texture = FrontRoomsDeskStill.Texture(frame, FrontRoomsDeskStill.UseHiResNow(camera, screenSize.y));
        if (texture == null) return;   // no art in this build: keep the ad
        stillMode = true;
        stillFrame = frame;
        stillTexture = texture;
        if (countedVisible)
        {
            countedVisible = false;
            bank?.SetVisible(this, false);
        }
        var fit = FrontRoomsDeskStill.Fit(screenSize, texture.width, texture.height);
        SetSurfaceScaleOffset(new Vector2(fit.x, fit.y), new Vector2(fit.z, fit.w));
        BindTexture(texture);
        if (frame == FrontRoomsDeskStill.Frame.Off && poweredOn)
        {
            SetPower(false);
            stillForcedOff = true;
        }
    }

    void ExitStill()
    {
        stillMode = false;
        stillTexture = null;
        SetSurfaceScaleOffset(Vector2.one, Vector2.zero);
        BindTexture(bank != null ? bank.TargetTexture : null);
        if (stillForcedOff && !poweredOn && !offForGood) SetPower(true);
        stillForcedOff = false;
    }

    void SetSurfaceScaleOffset(Vector2 scale, Vector2 offset)
    {
        if (surfaceMaterial == null) return;
        if (surfaceMaterial.HasProperty("_BaseMap"))
        {
            surfaceMaterial.SetTextureScale("_BaseMap", scale);
            surfaceMaterial.SetTextureOffset("_BaseMap", offset);
        }
        if (surfaceMaterial.HasProperty("_MainTex"))
        {
            surfaceMaterial.SetTextureScale("_MainTex", scale);
            surfaceMaterial.SetTextureOffset("_MainTex", offset);
        }
    }

    internal void BindTexture(Texture texture)
    {
        // V4: a still desk keeps its still frame; the bank's RenderTexture (Register, OnEnable) never reaches it.
        // terminal: a terminal keeps its own picture likewise.
        if (terminalPicture != null) texture = terminalPicture;
        else if (stillMode) texture = stillTexture;
        if (surfaceMaterial == null || texture == null) return;
        if (surfaceMaterial.HasProperty("_BaseMap")) surfaceMaterial.SetTexture("_BaseMap", texture);
        if (surfaceMaterial.HasProperty("_MainTex")) surfaceMaterial.SetTexture("_MainTex", texture);
    }

    void CreateSurface()
    {
        if (surfaceObject != null) return;
        // Material first. Without one, build nothing: no quad with an empty material
        // slot and no trigger collider. Update, BindTexture and TogglePower already
        // skip a screen that has no surface.
        var material = MakeMaterial(bank != null ? bank.TargetTexture : null);
        if (material == null)
        {
            if (!warnedNoSurfaceMaterial)
            {
                warnedNoSurfaceMaterial = true;
                Debug.LogWarning("[FrontRoomsScreenVideo] No screen surface material or shader in this player; CRT ad screens stay dark.");
            }
            return;
        }
        surfaceMaterial = material;
        surfaceObject = new GameObject("screen surface / shared ad video");
        surfaceObject.transform.SetParent(transform, false);
        surfaceObject.layer = gameObject.layer;
        surfaceObject.transform.localPosition = screenAnchor + Vector3.forward * .004f;

        var filter = surfaceObject.AddComponent<MeshFilter>();
        filter.sharedMesh = MakeQuad(screenSize.x, screenSize.y);
        surfaceRenderer = surfaceObject.AddComponent<MeshRenderer>();
        surfaceRenderer.sharedMaterial = surfaceMaterial;
        surfaceRenderer.enabled = poweredOn;

        surfaceCollider = surfaceObject.AddComponent<BoxCollider>();
        surfaceCollider.isTrigger = true;
        surfaceCollider.size = new Vector3(screenSize.x, screenSize.y, .012f);
        surfaceCollider.center = new Vector3(0f, 0f, .002f);

        TubeAfterCreate();
        AddToMonitorLods(surfaceRenderer);
    }

    /// <summary>
    /// Put the ad quad into every LOD of the monitor's own LODGroup, so it switches
    /// and culls with the monitor (codex audit F7). Outside every LOD it kept drawing
    /// after the monitor culled (11.2 m at High), floating where the monitor stood.
    /// </summary>
    void AddToMonitorLods(Renderer quad)
    {
        // The kit importer puts the group on the monitor root. A group further up
        // could belong to something else; a monitor without one (the ScreenAds demo
        // rig) is left alone.
        var group = GetComponent<LODGroup>();
        if (group == null || quad == null) return;
        var lods = group.GetLODs();
        if (lods.Length == 0) return;
        for (var i = 0; i < lods.Length; i++)
        {
            var renderers = new List<Renderer>(lods[i].renderers ?? new Renderer[0]);
            if (!renderers.Contains(quad)) renderers.Add(quad);
            lods[i].renderers = renderers.ToArray();
        }
        // No RecalculateBounds: it would change the imported size (.524 m) and move
        // the import-time switch and cull distances.
        group.SetLODs(lods);
    }

    // Every monitor of one screen size shares one quad (codex audit COV-4).
    // A new Mesh per monitor was never destroyed, so each Office chunk
    // teardown leaked one per CRT. Never destroy the shared quad per screen.
    // If something else destroys it, the Unity null check builds a new one.
    static readonly Dictionary<Vector2, Mesh> SharedQuads = new Dictionary<Vector2, Mesh>();

    static Mesh MakeQuad(float width, float height)
    {
        var key = new Vector2(width, height);
        if (SharedQuads.TryGetValue(key, out var shared) && shared != null) return shared;
        var mesh = new Mesh { name = "FrontRooms CRT screen quad" };
        SharedQuads[key] = mesh;
        var x = width * .5f;
        var y = height * .5f;
        mesh.vertices = new[]
        {
            new Vector3(-x, -y, 0f), new Vector3(x, -y, 0f),
            new Vector3(x, y, 0f), new Vector3(-x, y, 0f)
        };
        mesh.uv = new[]
        {
            // VideoPlayer's RenderTexture is authored from the opposite
            // horizontal convention to this CRT face, so flip U here rather
            // than mirroring the source creative itself.
            new Vector2(1f, 0f), new Vector2(0f, 0f),
            new Vector2(0f, 1f), new Vector2(1f, 1f)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    /// <summary>
    /// The screen's material, or null when this player has no usable shader. Order:
    /// 1. Resources/Media/CRT_AdSurface.mat (the CRT tube, FrontRooms/ScreenVideo, opaque,
    ///    white; 2026-10-22). It keeps the editor look in players: fog, the depth and normals
    ///    passes, and the glass RT traces it as emissive.
    /// 2. Resources/Rendering/FrontRoomsScreenVideo.shader (FrontRooms/ScreenVideo, the same
    ///    tube at its property defaults), which Resources also keeps in every build.
    /// 3. URP Unlit by name (editor only, when neither asset is present).
    /// The built-in Unlit/Texture is not tried: no player build includes it.
    /// Explicit null checks: '??' skips Unity's destroyed-object test.
    /// </summary>
    static Material MakeMaterial(Texture texture)
    {
        if (adSurfaceTemplate == null) adSurfaceTemplate = Resources.Load<Material>(AdSurfaceTemplatePath);
        Material material = null;
        if (adSurfaceTemplate != null)
        {
            material = new Material(adSurfaceTemplate);
        }
        else
        {
            var shader = Resources.Load<Shader>(AdSurfaceShaderPath);
            if (shader == null) shader = Shader.Find("FrontRooms/ScreenVideo");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null) material = new Material(shader);
        }
        if (material == null) return null;
        material.name = "FrontRooms / CRT ad surface";
        if (texture != null)
        {
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        }
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        return material;
    }

    // [CRT-TUBE-BEGIN] The tube layer (SCREENS_VISUAL_SYSTEM.md section 3, layer 3; Red approved it on 2026-10-22) and the
    // VL245 first-sight warm-up (in FrontRoomsScreenAdBank). One block; outside it the tube only adds the TubeAfterCreate
    // call in CreateSurface. V5 (SoundRole, the picture sync) is main's own; the tube reads none of it. Rebased on
    // 2026-10-29 onto main 7cef8417 (R3 close-up, V5 picture sync + re-seek on release, station-look-v1).
    // Report: Documentation/research/screens/40_tube_layer.md.
    //
    // Two curved surfaces follow the kit's own screen face (measured from the FBX: W/crt_tube/tools/face_profile.py):
    //   picture  "screen surface / shared ad video", 2 mm in front of the face, FrontRooms/ScreenVideo (the tube shader:
    //            overscan, barrel, scan lines, aperture grille, halation, vignette, the power collapse, the phosphor body);
    //   glass    "screen glass / faceplate", 3.5 mm in front, the glass system's own FrontRooms/Glass with _RTReceive = 1: the
    //            desktop RT glass traces its reflections and the raster fallback is that shader's own, so the two match by
    //            construction. Desktop only: WebGL (draw-bound) and phones draw no faceplate layer.
    // A screen with no measured kit face (the station set's picture on its scaled mount, an authored anchor or size)
    // keeps the flat quad, drawn by the same tube shader with TV overscan, and no faceplate.
    // Power and the warning language stay apart (section 3): nothing here reads LampLevel, the Warn overrides or WarnBurst
    // (the WarnBurst sag waits on Red's go for section 4 and is not built). The only change a screen ever makes is a power
    // change; its brightness never rises faster than a 1.5 s attack and it never goes above the steady picture's white.
    // A set that is off is dark glass: the picture surface swaps to one shared material with _BaseColor black (the RT trace
    // reads it as no emission) and the phosphor body shows under the faceplate; a flat screen shows the kit's own dark face.

    // ---- the screen-effects level (C26 "Screen effects setting: Full, Reduced, Off?" is open; no menu yet)
    static FrontRoomsScreenEffects screenEffects = FrontRoomsScreenEffects.Full;

    /// <summary>The requested screen-effects level (default Full). A future setting calls <see cref="SetScreenEffects"/>.</summary>
    public static FrontRoomsScreenEffects ScreenEffects => screenEffects;

    /// <summary>Sets the screen-effects level of every screen; the next rendered frame uses it.</summary>
    public static void SetScreenEffects(FrontRoomsScreenEffects level) => screenEffects = level;

    /// <summary>The level this platform draws: the request, capped at Reduced on WebGL and on phones (separate tracks).</summary>
    public static FrontRoomsScreenEffects EffectiveScreenEffects
    {
        get
        {
            var level = screenEffects;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (level == FrontRoomsScreenEffects.Full) level = FrontRoomsScreenEffects.Reduced;
#endif
            if (level == FrontRoomsScreenEffects.Full && FrontRoomsHandheld.Active) level = FrontRoomsScreenEffects.Reduced;
            return level;
        }
    }

    /// <summary>The faceplate glass layer: desktop (editor, macOS, Windows) only.</summary>
    static bool GlassLayerAllowed
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return false;
#else
            return !FrontRoomsHandheld.Active;
#endif
        }
    }

    // ---- the kit faces: z = faceZ - bulge * ((x / halfW)^2 + ((y - centreY) / halfH)^2), monitor-local metres (LOD0 = LOD1)
    struct TubeFace
    {
        public int key;
        public float centreY, faceZ, halfW, halfH, bulge;
        public float maskPitch;   // aperture grille pitch on the glass, metres
        public TubeFace(int key, float centreY, float faceZ, float halfW, float halfH, float bulge, float maskPitch)
        {
            this.key = key; this.centreY = centreY; this.faceZ = faceZ; this.halfW = halfW; this.halfH = halfH; this.bulge = bulge; this.maskPitch = maskPitch;
        }
    }

    // Kit_CRTMonitor: a 14-inch-class office monitor (0.25 mm grille); Kit_CRTTV: a 20-inch-class set (0.72 mm grille).
    static readonly TubeFace MonitorFace = new TubeFace(0, .248f, .195f, .149f, .113f, .006f, .00025f);
    static readonly TubeFace TvFace = new TubeFace(1, .284f, .224f, .2255f, .171f, .010f, .00072f);
    const float PictureLift = .002f, GlassLift = .0035f, FaceInset = .003f;
    /// <summary>Overscan of the broadcast picture per edge, top and bottom (the sides follow at 4:3: ~4.7 %).</summary>
    const float Overscan = .045f;
    /// <summary>Electron-optics barrel of the live picture (face units at the edge middle).</summary>
    const float Barrel = .05f;
    const float LiveCorner = .06f, StillCorner = .03f, BroadcastLines = 480f;
    /// <summary>Red's desk-monitor picture (2026-10-22 21:23): the whole 4:3 frame lit at 276 x 207 mm (underscan).</summary>
    static readonly Vector2 MonitorPicture = new Vector2(.276f, .207f);
    /// <summary>The desk monitor's barrel: its raster edges are in view, so a little less than the TV's.</summary>
    const float MonitorBarrel = .02f;
    static readonly Vector3 LegacyAnchor = new Vector3(0f, .248f, .194f);
    static readonly Vector2 LegacySize = new Vector2(.28f, .19f);

    static readonly int TubeLevelId = Shader.PropertyToID("_TubeLevel"), PowerHId = Shader.PropertyToID("_PowerH");
    static readonly int PowerVId = Shader.PropertyToID("_PowerV"), BeamId = Shader.PropertyToID("_Beam"), WarmId = Shader.PropertyToID("_Warm");
    static readonly int SignalId = Shader.PropertyToID("_Signal"), StillId = Shader.PropertyToID("_Still"), LinesId = Shader.PropertyToID("_Lines");
    static readonly int RasterId = Shader.PropertyToID("_Raster"), FaceId = Shader.PropertyToID("_Face"), StillRectId = Shader.PropertyToID("_StillRect");
    static readonly int TubeBaseMapId = Shader.PropertyToID("_BaseMap"), TubeBaseColorId = Shader.PropertyToID("_BaseColor");

    TubeFace tubeFace = MonitorFace;
    bool tubeShaped;
    GameObject glassObject;
    MeshRenderer glassRenderer;
    // A reading close-up holds the faceplate's reflections under the text (平面视觉, 2026-10-30: a lamp's image hid "FROM:" on
    // the desktop). This one renderer's F0 through a block: the glass keeps its tint and alpha, the shared material is untouched.
    const float FaceplateF0 = .045f;
    static readonly int PaneF0Id = Shader.PropertyToID("_PaneF0");
    static MaterialPropertyBlock faceBlock;
    float faceReflection = 1f;

    /// <summary>
    /// The measured kit face this screen sits on, found by the kit's own mesh names under the screen: Kit_CRTTV
    /// (Kit_CRTTV_LOD0/1), else Kit_CRTMonitor (Kit_CRTMonitor_LOD0/1). False for anything else (2026-10-29): the station
    /// set's picture, which FrontRoomsStationDress attaches to a scaled "screen mount" on Kit_CRTTV_Station (a kit with its
    /// own curved glass and no measured face here), and any other mount keep the flat quad, still drawn by the tube.
    /// </summary>
    static bool TryFaceOf(GameObject monitor, out TubeFace face)
    {
        var monitorFound = false;
        foreach (var mf in monitor.GetComponentsInChildren<MeshFilter>(true))
        {
            var mesh = mf.sharedMesh;
            if (mesh == null) continue;
            var n = mesh.name;
            if (n.IndexOf("CRTTV", System.StringComparison.OrdinalIgnoreCase) >= 0 && n.IndexOf("Station", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                face = TvFace;
                return true;
            }
            if (n.IndexOf("CRTMonitor", System.StringComparison.OrdinalIgnoreCase) >= 0) monitorFound = true;
        }
        face = MonitorFace;
        return monitorFound;
    }

    /// <summary>A flat screen on a TV (the station set's mount under Kit_CRTTV_Station): the TV's grille pitch.</summary>
    static bool UnderTv(Transform t)
    {
        for (; t != null; t = t.parent)
            if (t.name.IndexOf("CRTTV", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }

    // One mesh per face and layer, shared by every screen and never destroyed per screen (as COV-4's shared quad).
    static readonly Dictionary<int, Mesh> TubeMeshes = new Dictionary<int, Mesh>();

    static Mesh TubeMesh(TubeFace f, float lift, int layer)
    {
        var key = f.key * 2 + layer;
        if (TubeMeshes.TryGetValue(key, out var shared) && shared != null) return shared;
        const int nx = 16, ny = 12;
        var hw = f.halfW - FaceInset;
        var hh = f.halfH - FaceInset;
        var verts = new Vector3[(nx + 1) * (ny + 1)];
        var normals = new Vector3[verts.Length];
        var uvs = new Vector2[verts.Length];
        for (var j = 0; j <= ny; j++)
        {
            for (var i = 0; i <= nx; i++)
            {
                var u = i / (float)nx;
                var v = j / (float)ny;
                // u = 0 at the viewer's left, which is the monitor's +X: the video RT's convention (see MakeQuad)
                var x = Mathf.Lerp(hw, -hw, u);
                var y = Mathf.Lerp(-hh, hh, v);
                var gx = x / (f.halfW * f.halfW);
                var gy = y / (f.halfH * f.halfH);
                var k = j * (nx + 1) + i;
                verts[k] = new Vector3(x, y, lift - f.bulge * (x * gx + y * gy));
                normals[k] = new Vector3(2f * f.bulge * gx, 2f * f.bulge * gy, 1f).normalized;
                uvs[k] = new Vector2(u, v);
            }
        }
        var tris = new int[nx * ny * 6];
        var n = 0;
        for (var j = 0; j < ny; j++)
        {
            for (var i = 0; i < nx; i++)
            {
                var a = j * (nx + 1) + i;
                var b = a + 1;
                var c = a + nx + 2;
                var d = a + nx + 1;
                // front face toward +Z (clockwise as the viewer sees it, like MakeQuad's)
                tris[n++] = b; tris[n++] = a; tris[n++] = d;
                tris[n++] = b; tris[n++] = d; tris[n++] = c;
            }
        }
        var mesh = new Mesh { name = layer == 0 ? "FrontRooms CRT tube picture" : "FrontRooms CRT tube faceplate" };
        mesh.vertices = verts;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        TubeMeshes[key] = mesh;
        return mesh;
    }

    // The faceplate: the glass system's own shader, one shared material (no per-screen property).
    static Material faceGlass;
    static bool faceGlassTried;

    static Material FaceGlassMaterial()
    {
        if (faceGlass != null) return faceGlass;
        if (faceGlassTried) return null;
        faceGlassTried = true;
        var shader = Resources.Load<Shader>("Rendering/FrontRoomsGlass");
        if (shader == null || !shader.isSupported) return null;
        var m = new Material(shader) { name = "FrontRooms / CRT faceplate glass" };
        // A pressed, tinted faceplate: one front surface (F0 .045 against a window's two, .08), no float-glass roll, a
        // light dust film; the reflection tops up to the full captured light like the windows (_ReflectionMin 1).
        m.SetColor("_BaseColor", new Color(.10f, .12f, .11f, 1f));
        m.SetFloat("_AlphaFace", .10f);
        m.SetFloat("_AlphaFresnel", .60f);
        m.SetFloat("_Smoothness", .93f);
        m.SetFloat("_SmudgeSmoothness", .70f);
        m.SetFloat("_PaneF0", FaceplateF0);
        m.SetFloat("_RollStrength", 0f);
        m.SetFloat("_ReflectionMin", 1f);
        m.SetFloat("_DustFilm", .08f);
        m.SetFloat("_AutoEdge", 0f);
        m.SetVector("_PaneSize", new Vector4(.30f, .23f, .012f, 0f));   // the thin axis is Z (the curved sheet's normal)
        m.SetFloat("_RTReceive", 1f);
        faceGlass = m;
        return m;
    }

    // A set that is off: one shared material, no picture, _BaseColor black (no emission in the RT trace).
    static Material offMaterial;

    static Material OffMaterial(Material from)
    {
        if (offMaterial != null || from == null) return offMaterial;
        offMaterial = new Material(from) { name = "FrontRooms / CRT tube, off" };
        if (offMaterial.HasProperty(TubeBaseMapId)) offMaterial.SetTexture(TubeBaseMapId, Texture2D.blackTexture);
        if (offMaterial.HasProperty(TubeBaseColorId)) offMaterial.SetColor(TubeBaseColorId, Color.black);
        if (offMaterial.HasProperty(PowerHId)) offMaterial.SetFloat(PowerHId, 0f);
        if (offMaterial.HasProperty(PowerVId)) offMaterial.SetFloat(PowerVId, 0f);
        if (offMaterial.HasProperty(WarmId)) offMaterial.SetFloat(WarmId, 0f);
        if (offMaterial.HasProperty(SignalId)) offMaterial.SetFloat(SignalId, 0f);
        if (offMaterial.HasProperty(BeamId)) offMaterial.SetFloat(BeamId, 0f);
        return offMaterial;
    }

    /// <summary>Scales this screen's faceplate reflection (1 = the glass as built). No glass layer (phones, WebGL): nothing to do.</summary>
    public void SetFaceplateReflection(float scale)
    {
        if (Mathf.Approximately(scale, faceReflection)) return;
        faceReflection = scale;
        if (glassRenderer == null) return;
        if (scale >= 1f) { glassRenderer.SetPropertyBlock(null); return; }
        if (faceBlock == null) faceBlock = new MaterialPropertyBlock();
        faceBlock.SetFloat(PaneF0Id, FaceplateF0 * scale);
        glassRenderer.SetPropertyBlock(faceBlock);
    }

    /// <summary>CreateSurface's last step: the curved picture surface and the faceplate glass on the kit's face.</summary>
    void TubeAfterCreate()
    {
        if (surfaceObject == null || surfaceRenderer == null) return;
        surfaceRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        surfaceRenderer.receiveShadows = false;
        // A screen authored with its own anchor or size, or one on no measured kit face (the station set's scaled mount),
        // has no face to follow: it keeps the flat quad (the tube shader still draws on it, with no faceplate layer) and
        // turns its surface off with the power, as before: off, the kit's own dark screen face shows.
        if (screenAnchor != LegacyAnchor || screenSize != LegacySize || !TryFaceOf(gameObject, out var face))
        {
            var pitch = UnderTv(transform) ? TvFace.maskPitch : MonitorFace.maskPitch;
            tubeFace = new TubeFace(-1, screenAnchor.y, screenAnchor.z, screenSize.x * .5f + FaceInset, screenSize.y * .5f + FaceInset, 0f, pitch);
            return;
        }
        tubeFace = face;
        tubeShaped = true;
        var anchor = new Vector3(0f, tubeFace.centreY, tubeFace.faceZ);
        surfaceObject.transform.localPosition = anchor;
        var filter = surfaceObject.GetComponent<MeshFilter>();
        if (filter != null) filter.sharedMesh = TubeMesh(tubeFace, PictureLift, 0);
        if (surfaceCollider != null)
        {
            surfaceCollider.size = new Vector3(2f * (tubeFace.halfW - FaceInset), 2f * (tubeFace.halfH - FaceInset), .012f);
            surfaceCollider.center = new Vector3(0f, 0f, PictureLift);
        }
        if (!GlassLayerAllowed) return;
        var glass = FaceGlassMaterial();
        if (glass == null) return;
        glassObject = new GameObject("screen glass / faceplate");
        glassObject.transform.SetParent(transform, false);
        glassObject.layer = gameObject.layer;
        glassObject.transform.localPosition = anchor;
        glassObject.AddComponent<MeshFilter>().sharedMesh = TubeMesh(tubeFace, GlassLift, 1);
        glassRenderer = glassObject.AddComponent<MeshRenderer>();
        glassRenderer.sharedMaterial = glass;
        glassRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        AddToMonitorLods(glassRenderer);   // culls with the monitor like the picture (codex audit F7)
    }

    /// <summary>
    /// The raster this screen lights, pushed when the picture changes between the broadcast and a still (V4):
    /// - Kit_CRTMonitor (Red, 2026-10-22 21:23, VL429 "1990 OFFICE MONITOR"; research/screens/NOTE_1022_crt_border_READ_FIRST.md):
    ///   UNDERSCAN. The whole 4:3 frame, no crop and no pillarbox, lit at <see cref="MonitorPicture"/> (276 x 207 mm) in the
    ///   centre of the 300 x 228 mm opening; the 12 / 10 mm round it is the tube's unlit dark glass (the phosphor body),
    ///   never flat black. The ad goes through a small barrel (its edge middles land on the 276 x 207 mm rectangle); a V4
    ///   still lands on the same area, 1:1, with no barrel and none of the tube's image effects ("no tube effects");
    /// - Kit_CRTTV: the broadcast overscanned 4.5 % top and bottom (about 4.7 % at the sides) through the barrel, so the
    ///   edges of the frame run under the bezel like any 1990 set (SCREENS_VISUAL_SYSTEM.md section 3); never pillarboxed;
    /// - a flat screen authored with its own size (no measured face): as the TV, and a still keeps V4's own Fit size.
    /// </summary>
    void PushTubeGeometry(bool still)
    {
        var fw = tubeFace.halfW - FaceInset;
        var fh = tubeFace.halfH - FaceInset;
        // The face in world metres: a screen on a scaled mount (the station set's, 1.46 x 1.63) keeps a 4:3 broadcast, a
        // round power dot and its grille pitch. Unit scale (every kit screen) changes nothing.
        var ls = surfaceObject != null ? surfaceObject.transform.lossyScale : Vector3.one;
        var wx = Mathf.Max(1e-4f, Mathf.Abs(ls.x));
        var wy = Mathf.Max(1e-4f, Mathf.Abs(ls.y));
        tubeDotAspect = tubeFace.halfH * wy / (tubeFace.halfW * wx);
        Vector4 raster, stillRect = new Vector4(1f, 1f, 0f, 0f);
        if (tubeShaped && tubeFace.key == MonitorFace.key)
        {
            var b = still ? 0f : MonitorBarrel;
            var tx = MonitorPicture.x * .5f / fw;
            var ty = MonitorPicture.y * .5f / fh;
            raster = new Vector4(tx * (1f + b * tx * tx), ty * (1f + b * ty * ty), b, still ? StillCorner : LiveCorner);
            if (still)
            {
                var st = surfaceMaterial.HasProperty(TubeBaseMapId) ? surfaceMaterial.GetTextureScale(TubeBaseMapId) : Vector2.one;
                stillRect = new Vector4(1f / Mathf.Max(1e-3f, Mathf.Abs(st.x)), 1f / Mathf.Max(1e-3f, Mathf.Abs(st.y)), 0f, 0f);
            }
        }
        else if (still)
        {
            var st = surfaceMaterial.HasProperty(TubeBaseMapId) ? surfaceMaterial.GetTextureScale(TubeBaseMapId) : Vector2.one;
            var sx = Mathf.Max(1e-3f, Mathf.Abs(st.x));
            var sy = Mathf.Max(1e-3f, Mathf.Abs(st.y));
            raster = new Vector4(screenSize.x * .5f / sx / fw, screenSize.y * .5f / sy / fh, 0f, StillCorner);
            stillRect = new Vector4(1f / sx, 1f / sy, 0f, 0f);
        }
        else
        {
            var ry = (1f + Barrel) / (1f - 2f * Overscan);
            var rx = ry * (4f / 3f) * (fh * wy) / (fw * wx);
            raster = new Vector4(rx, ry, Barrel, LiveCorner);
        }
        surfaceMaterial.SetVector(RasterId, raster);
        surfaceMaterial.SetVector(StillRectId, stillRect);
        surfaceMaterial.SetVector(FaceId, new Vector4(fw * wx, fh * wy, tubeFace.maskPitch, 0f));
        surfaceMaterial.SetFloat(StillId, still ? 1f : 0f);
        surfaceMaterial.SetFloat(LinesId, terminalPicture != null ? TerminalLines : BroadcastLines);
    }

    // ---- power: a power change animates; ticked once per frame before any camera renders, after every LateUpdate
    // (TogglePower runs in LateUpdate), so the toggle's own frame already draws the first step of the animation.
    // The tube follows main's power state (poweredOn) whoever changes it: E (TogglePower), V4's still mix, and
    // PowerOffForGood (station-look-v1: the station TV at the key take) collapses like E's off, then stays off.
    const float LineV = .008f;
    bool tubeInit, tubeOn, tubeAnimating, tubeStill;
    float tubeT, tubePowerH = 1f, tubePowerV = 1f, tubeBeam, tubeWarm = 1f, tubeDotAspect = .758f;
    float lastLevel = float.NaN, lastPowerH = float.NaN, lastPowerV = float.NaN, lastBeam = float.NaN, lastWarm = float.NaN, lastSignal = float.NaN;
    static int tubeTickFrame = -1;

    /// <summary>Harness: is this screen in a power animation, and is the picture (not the off material) drawn.</summary>
    internal bool TubeAnimating => tubeAnimating;
    internal bool TubeShowsPicture => tubeOn || tubeAnimating;
    internal GameObject TubeGlassObject => glassObject;
    internal bool TubeShaped => tubeShaped;
    internal float TubeClock => tubeT;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void HookTube()
    {
        tubeTickFrame = -1;
        if (faceGlass == null) faceGlassTried = false;
        UnityEngine.Rendering.RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
        UnityEngine.Rendering.RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
    }

    static void OnBeginContextRendering(UnityEngine.Rendering.ScriptableRenderContext context, List<Camera> cameras)
    {
        if (!Application.isPlaying || Time.frameCount == tubeTickFrame) return;
        tubeTickFrame = Time.frameCount;
        // game time: a held world (C26) and a paused clock freeze a power change; a fixed-step capture records it at speed
        var dt = WorldHeld ? 0f : Time.deltaTime;
        var camera = ActiveScreens.Count > 0 ? ResolveCamera() : null;
        for (var i = 0; i < ActiveScreens.Count; i++)
            if (ActiveScreens[i] != null) ActiveScreens[i].TickTube(dt, camera);
    }

    /// <summary>Total length of a power change: on 1.8 s, off 1.9 s, a plain 1.5 s fade at Off.</summary>
    static float TubeDuration(bool on, FrontRoomsScreenEffects level) => level == FrontRoomsScreenEffects.Off ? 1.5f : (on ? 1.8f : 1.9f);

    static float EaseIn(float x) { x = Mathf.Clamp01(x); return x * x; }
    static float EaseOut(float x) { x = Mathf.Clamp01(x); return 1f - (1f - x) * (1f - x); }

    void SetTubeSteady(bool on)
    {
        tubePowerH = on ? 1f : 0f;
        tubePowerV = on ? 1f : 0f;
        tubeWarm = on ? 1f : 0f;
        tubeBeam = 0f;
    }

    /// <summary>
    /// Off (1.9 s): the picture folds to a line (0.18 s), the line shrinks to a dot (0.32 s), the dot fades (1.4 s).
    /// On (1.8 s): a dot opens to a line (0.3 s) and the line to the picture (0.3 s) while the cold tube warms up: the
    /// brightness only ever rises along one smoothstep over the whole 1.8 s (attack >= 1.5 s).
    /// The collapsed beam concentrates its energy, but never above the steady picture's white (the shader caps it).
    /// </summary>
    void EvaluateTube(bool on, float t, FrontRoomsScreenEffects level)
    {
        if (level == FrontRoomsScreenEffects.Off)
        {
            var k = Mathf.SmoothStep(0f, 1f, t / 1.5f);
            tubePowerH = 1f; tubePowerV = 1f; tubeBeam = 0f;
            tubeWarm = on ? k : 1f - k;
            return;
        }
        var dotH = LineV * tubeDotAspect;   // a round dot on the face (world metres)
        if (!on)
        {
            if (t < .18f) { tubePowerV = Mathf.Lerp(1f, LineV, EaseIn(t / .18f)); tubePowerH = 1f; tubeWarm = 1f; tubeBeam = 0f; }
            else if (t < .50f) { tubePowerV = LineV; tubePowerH = Mathf.Lerp(1f, dotH, EaseIn((t - .18f) / .32f)); tubeWarm = 1f; tubeBeam = 1f; }
            else { var k = Mathf.Exp(-(t - .50f) * 3.2f); tubePowerV = LineV; tubePowerH = dotH; tubeWarm = k; tubeBeam = k; }
        }
        else
        {
            tubeWarm = Mathf.SmoothStep(0f, 1f, t / 1.8f);
            if (t < .30f) { tubePowerV = LineV; tubePowerH = Mathf.Lerp(dotH, 1f, EaseOut(t / .30f)); tubeBeam = .5f; }
            else if (t < .60f) { tubePowerV = Mathf.Lerp(LineV, 1f, EaseOut((t - .30f) / .30f)); tubePowerH = 1f; tubeBeam = 0f; }
            else { tubePowerV = 1f; tubePowerH = 1f; tubeBeam = 0f; }
        }
    }

    void TickTube(float dt, Camera camera)
    {
        if (surfaceRenderer == null || surfaceMaterial == null) return;
        var level = EffectiveScreenEffects;
        if (!tubeInit)
        {
            // a screen starts where its power is: no animation at spawn
            tubeInit = true;
            tubeOn = poweredOn;
            SetTubeSteady(tubeOn);
            tubeStill = stillMode;
            tubeTerminal = terminalPicture != null;
            PushTubeGeometry(tubeStill);
        }
        if (stillMode != tubeStill || (terminalPicture != null) != tubeTerminal)
        {
            tubeStill = stillMode;
            tubeTerminal = terminalPicture != null;
            PushTubeGeometry(tubeStill);
        }
        if (poweredOn != tubeOn)
        {
            if (!tubeAnimating && !SeenBy(camera))
            {
                // a change nobody can see (V4's still mix leaving a set off while it is out of view) just happens
                tubeOn = poweredOn;
                SetTubeSteady(tubeOn);
            }
            else
            {
                // a toggle during a change reverses it from where the picture is
                var progress = tubeAnimating ? Mathf.Clamp01(tubeT / TubeDuration(tubeOn, level)) : 1f;
                tubeOn = poweredOn;
                tubeT = tubeAnimating ? (1f - progress) * TubeDuration(tubeOn, level) : 0f;
                tubeAnimating = true;
            }
        }
        if (tubeAnimating)
        {
            tubeT += dt;
            if (tubeT >= TubeDuration(tubeOn, level)) { tubeAnimating = false; SetTubeSteady(tubeOn); }
            else EvaluateTube(tubeOn, tubeT, level);
        }
        var showPicture = tubeOn || tubeAnimating;
        // Off: the desktop keeps the surface as dark glass under the faceplate; WebGL, phones and flat screens show the
        // kit's own face again, as before the tube.
        var keep = showPicture || glassRenderer != null;
        if (surfaceRenderer.enabled != keep) surfaceRenderer.enabled = keep;
        var want = showPicture ? surfaceMaterial : OffMaterial(adSurfaceTemplate != null ? adSurfaceTemplate : surfaceMaterial);
        if (want != null && surfaceRenderer.sharedMaterial != want) surfaceRenderer.sharedMaterial = want;
        if (!showPicture) return;
        // VL245 belt and braces: a live picture with no decoded frame yet draws the tube's own raster glow, never a hole
        var signal = 1f;
        if (!tubeStill && terminalPicture == null && bank != null && !bank.HasFrame) signal = 0f;
        Push(TubeLevelId, (float)level, ref lastLevel);
        Push(PowerHId, tubePowerH, ref lastPowerH);
        Push(PowerVId, tubePowerV, ref lastPowerV);
        Push(BeamId, tubeBeam, ref lastBeam);
        Push(WarmId, tubeWarm, ref lastWarm);
        Push(SignalId, signal, ref lastSignal);
    }

    void Push(int id, float value, ref float last)
    {
        if (value == last) return;
        last = value;
        surfaceMaterial.SetFloat(id, value);
    }
    // [CRT-TUBE-END]

    // [TERMINAL-BEGIN] Office terminals (game visual, 2026-10-30; research/computer_ui/41_c19_period_interaction.md and
    // 50_build.md). FrontRoomsTerminalScreen puts its text-mode picture on this desk CRT's own tube. INERT until something
    // calls ShowTerminal (no desk in main does yet): with no terminal picture every terminal guard in this file is false and
    // every screen (desk ads, V4 stills, the station TV, the title TV) behaves exactly as before. While a terminal shows:
    // the picture wins over the bank's RT and the still frame (BindTexture), the still mix leaves it alone (WantsStill),
    // the shared decoder is never asked to play for it (Update), E never toggles its power (UpdateFocusedScreen: its E is
    // the READ close-up), and the tube draws the live raster path (underscan 276 x 207 mm, its barrel and effects) with
    // VGA text mode's 400 scan lines and never the no-frame glow (TickTube, PushTubeGeometry).
    Texture terminalPicture;
    bool tubeTerminal;
    /// <summary>VGA text mode's visible lines (720 x 400 at 70 Hz): the tube's scan lines on a terminal.</summary>
    const float TerminalLines = 400f;

    /// <summary>This screen shows an office terminal's picture.</summary>
    public bool ShowsTerminal => terminalPicture != null;

    /// <summary>The picture surface's centre in world space (the monitor's face before the surface exists).</summary>
    public Vector3 PictureCentre => surfaceObject != null ? surfaceObject.transform.position : transform.TransformPoint(screenAnchor);

    /// <summary>The picture surface (null before it exists): a terminal's READ target is its child (FrontRoomsTerminalScreen).</summary>
    public Transform PictureSurface => surfaceObject != null ? surfaceObject.transform : null;

    /// <summary>
    /// Show a terminal's picture on this CRT (FrontRoomsTerminalScreen; a TELL texture or its live text-mode picture). The set
    /// leaves V4's still mix and powers on (the tube's own power-on if it is seen).
    /// </summary>
    public void ShowTerminal(Texture picture)
    {
        if (picture == null) { ClearTerminal(); return; }
        terminalPicture = picture;
        if (stillMode) ExitStill();
        stillForcedOff = false;
        if (!poweredOn && !offForGood) SetPower(true);
        if (countedVisible)
        {
            countedVisible = false;
            bank?.SetVisible(this, false);
        }
        SetSurfaceScaleOffset(Vector2.one, Vector2.zero);
        BindTexture(picture);
    }

    /// <summary>Back to the ad (and, on a station floor, V4's still mix the next time nobody sees the set).</summary>
    public void ClearTerminal()
    {
        if (terminalPicture == null) return;
        terminalPicture = null;
        BindTexture(bank != null ? bank.TargetTexture : null);
    }
    // [TERMINAL-END]

    // ---------------------------------------------------------------- V5 (agreed with 声音设计, 2026-10-22 15:2x)

    /// <summary>V5: which sound this screen carries (声音设计's FrontRoomsScreenSound reads it). Set at Attach; None = silent.</summary>
    public FrontRoomsScreenSoundRole SoundRole;
}

/// <summary>
/// Shared decoder and RenderTexture for one in-world ad source. Banks are
/// keyed by source so different channels do not retarget each other.
/// WebGL uses a StreamingAssets URL; desktop can use the same URL or a
/// VideoClip supplied by a prefab/scene author.
/// </summary>
public sealed class FrontRoomsScreenAdBank : MonoBehaviour
{
    static readonly Dictionary<string, FrontRoomsScreenAdBank> Banks = new Dictionary<string, FrontRoomsScreenAdBank>();
    readonly HashSet<FrontRoomsScreenVideo> screens = new HashSet<FrontRoomsScreenVideo>();
    readonly HashSet<FrontRoomsScreenVideo> visibleScreens = new HashSet<FrontRoomsScreenVideo>();
    VideoPlayer player;
    RenderTexture targetTexture;
    bool preparing;
    float retryAt;
    string streamFile;
    VideoClip desktopClip;
    string bankKey;
    // R3: the clock this bank's player was given when it was made (the decided clock: Unity's default, which this file
    // never set), and whether the player is on the close-up clock now.
    VideoTimeUpdateMode decidedClock;
    bool onCloseUpClock;
    // R3: the player has shown a frame since it last prepared (latched, so a pause or the hold never drops it), and the
    // last prepare failed (OnError) with no prepare since.
    bool pictured;
    bool failed;
    // VL245 (first sight): the decoder warms when the first screen registers; hasFrame once a decoded frame is in the RT
    // (reported by the player: frameReady while it is hooked, or a frame index / presentation time on two Updates in a row)
    bool hasFrame, warming, frameSeen, frameHooked, frameArrived;
    // The tube's two RTs (2026-10-29, the exit-crash fix): the player decodes into decodeTexture, a plain 640 x 480 RT
    // without mips (as main's); each new frame is copied into targetTexture, the mipmapped trilinear RT the screens sample.
    RenderTexture decodeTexture;
    bool copyByTexture, copyAgain, copiedPicture;
    long copiedFrame = long.MinValue;
    double copiedTime = -1d;

    /// <summary>R3: the clock the close-up's bank plays on; it runs at timeScale 0.</summary>
    public const VideoTimeUpdateMode CloseUpClock = VideoTimeUpdateMode.UnscaledGameTime;

    public RenderTexture TargetTexture => targetTexture;

    /// <summary>VL245: a decoded frame is in <see cref="TargetTexture"/>. The bank warms on the first Register and holds
    /// that frame paused until a screen is in view, so the first visible frame of any screen shows the picture.</summary>
    public bool HasFrame => hasFrame;
    /// <summary>R3: the player's decided clock, set back when the close-up ends.</summary>
    public VideoTimeUpdateMode DecidedClock => decidedClock;
    /// <summary>R3: the player is on <see cref="CloseUpClock"/> because its screen is in the close-up.</summary>
    public bool OnCloseUpClock => onCloseUpClock;
    /// <summary>The shared player (tests and the close-up proof read it).</summary>
    public VideoPlayer Player => player;

    /// <summary>
    /// R3: the ad is on the screen: the player is prepared and has shown its first frame (frame index set, or the playhead
    /// has moved). Latched until the next error, so a pause, the hold or a screen out of view keeps it.
    /// </summary>
    public bool HasPicture
    {
        get
        {
            if (player == null || !player.isPrepared) return false;
            if (!pictured && (player.frame >= 0 || (player.isPlaying && player.time > 0d))) pictured = true;
            return pictured;
        }
    }

    /// <summary>R3: the stream failed to prepare and has not prepared since (PrepareIfNeeded retries it every 2 s).</summary>
    public bool Failed => failed && (player == null || !player.isPrepared);

    public static FrontRoomsScreenAdBank Acquire(string file, VideoClip clip)
    {
        file = string.IsNullOrEmpty(file) ? FrontRoomsScreenVideo.DefaultStreamFile : file;
        var key = file + "|" + (clip != null ? FrontRoomsObjectId.Of(clip).ToString() : "url");
        if (!Banks.TryGetValue(key, out var bank) || bank == null)
        {
            var go = new GameObject("FrontRooms / shared screen ad bank / " + Path.GetFileNameWithoutExtension(file));
            go.hideFlags = HideFlags.DontSave;
            bank = go.AddComponent<FrontRoomsScreenAdBank>();
            bank.bankKey = key;
            bank.streamFile = file;
            bank.desktopClip = clip;
            Banks[key] = bank;
            bank.EnsurePlayer();
        }
        return bank;
    }

    void EnsurePlayer()
    {
        if (player == null)
        {
            player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = true;
            player.waitForFirstFrame = true;
            player.skipOnDrop = true;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.aspectRatio = VideoAspectRatio.FitInside;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.prepareCompleted += OnPrepared;
            player.errorReceived += OnError;
            decidedClock = player.timeUpdateMode;
        }
        if (targetTexture == null)
        {
            // The tube (2026-10-22): the creative is 4:3 (Ad_03, 640 x 480) and so is the kit's face; the tube shader crops
            // the overscan itself (4.5 % per edge). Other aspect ratios are still fitted inside, never stretched.
            // Two RTs (2026-10-29): the player decodes into a plain RT without mips, as main's (a mipmapped video target
            // crashed Unity's video teardown at exit: AVFoundation PixelBufferBlitter); CopyNewFrame copies each new frame into
            // the screens' RT, trilinear with mips (no shimmer at a distance; the tube's bloom reads two mip levels).
            decodeTexture = new RenderTexture(640, 480, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                name = "FrontRooms / shared CRT ad decode RT",
                useMipMap = false,
                autoGenerateMips = false,
                antiAliasing = 1,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            decodeTexture.Create();
            copyByTexture = (SystemInfo.copyTextureSupport & UnityEngine.Rendering.CopyTextureSupport.Basic) != 0;
            targetTexture = new RenderTexture(640, 480, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                name = "FrontRooms / shared CRT ad RT",
                useMipMap = true,
                autoGenerateMips = !copyByTexture,   // CopyTexture + GenerateMips; the Blit fallback regenerates them itself
                antiAliasing = 1,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4,
                wrapMode = TextureWrapMode.Clamp
            };
            targetTexture.Create();
            player.targetTexture = decodeTexture;
        }

        player.source = VideoSource.Url;
        player.url = ResolveStreamUrl(streamFile);
#if !UNITY_WEBGL || UNITY_EDITOR
        if (desktopClip != null)
        {
            player.source = VideoSource.VideoClip;
            player.clip = desktopClip;
        }
        else
        {
            var resourceClip = Resources.Load<VideoClip>("Videos/" + Path.GetFileNameWithoutExtension(streamFile));
            if (resourceClip != null)
            {
                player.source = VideoSource.VideoClip;
                player.clip = resourceClip;
            }
        }
#endif
        if (visibleScreens.Count > 0) PrepareIfNeeded();
    }

    static string ResolveStreamUrl(string file)
    {
        if (string.IsNullOrEmpty(file)) file = FrontRoomsScreenVideo.DefaultStreamFile;
        if (file.Contains("://")) return file;
        return (Application.streamingAssetsPath + "/" + file).Replace("\\", "/");
    }

    public void Register(FrontRoomsScreenVideo screen)
    {
        if (screen == null) return;
        screens.Add(screen);
        if (targetTexture == null) EnsurePlayer();
        screen.BindTexture(targetTexture);
        Warm();
    }

    /// <summary>
    /// VL245. Codex audit F7 culls the ad quad with its monitor (11.2 m at High), so the bank no longer prepared before
    /// first sight and its RT stayed black for about 6 frames when a CRT first came into view. F7 stays: the decoder now
    /// prepares when the first screen registers (a chunk builds its desks before the player can see them), plays to its
    /// first decoded frame and pauses there until a screen is seen. Never while the hold holds this bank (C26 + R3:
    /// <see cref="FrontRoomsScreenVideo.HeldFor"/>: the pause card holds every bank, a close-up every bank but its own,
    /// which plays through the close-up anyway); Update starts it again once the hold releases.
    /// </summary>
    void Warm()
    {
        if (player == null || hasFrame) return;
        HookFirstFrame();
        if (warming || FrontRoomsScreenVideo.HeldFor(this) || Time.unscaledTime < retryAt) return;
        warming = true;
        if (player.isPrepared) { if (!player.isPlaying) player.Play(); }
        else if (!preparing) { preparing = true; player.Prepare(); }
    }

    /// <summary>VL245: until the first decoded frame is in the RT the player reports each new frame (frameReady); off again
    /// once hasFrame latches, so no screen pays for the events afterwards.</summary>
    void HookFirstFrame()
    {
        if (frameHooked) return;
        frameHooked = true;
        player.frameReady += OnFrameReady;
        player.sendFrameReadyEvents = true;
    }

    void OnFrameReady(VideoPlayer source, long frameIdx) => frameArrived = true;

    /// <summary>
    /// The player's newest frame -> <see cref="TargetTexture"/> (the screens' mipmapped RT), only when the player shows a
    /// new frame (its frame index or presentation time changed; frameReady while hooked), and once more on the next Update
    /// so a frame still landing in the decode RT is never missed. Paused or held: nothing is copied. GPU only, no allocation.
    /// </summary>
    void CopyNewFrame()
    {
        if (player == null || decodeTexture == null || targetTexture == null || !player.isPrepared) return;
        var f = player.frame;
        var t = player.time;
        if (f < 0 && t <= 0d && !frameArrived) return;   // nothing decoded yet
        var changed = f != copiedFrame || t != copiedTime;
        if (!changed && !copyAgain) return;
        copyAgain = changed;
        copiedFrame = f;
        copiedTime = t;
        if (copyByTexture)
        {
            Graphics.CopyTexture(decodeTexture, 0, 0, targetTexture, 0, 0);
            targetTexture.GenerateMips();
        }
        else Graphics.Blit(decodeTexture, targetTexture);
        copiedPicture = true;
    }

    public void SetVisible(FrontRoomsScreenVideo screen, bool visible)
    {
        if (screen == null) return;
        if (visible) visibleScreens.Add(screen);
        else visibleScreens.Remove(screen);
        if (visible) PrepareIfNeeded();
        else if (visibleScreens.Count == 0 && player != null && player.isPlaying && !warming) player.Pause();
    }

    public void Unregister(FrontRoomsScreenVideo screen)
    {
        if (screen == null) return;
        screens.Remove(screen);
        visibleScreens.Remove(screen);
        if (visibleScreens.Count == 0 && player != null && player.isPlaying && !warming) player.Pause();
    }

    void PrepareIfNeeded()
    {
        if (player == null || preparing || player.isPrepared || Time.unscaledTime < retryAt)
        {
            // C26: never Play under a close-up or the pause card (R3: except the close-up's own bank, which plays on
            // from where it is: Play after Pause resumes, nothing seeks).
            if (player != null && player.isPrepared && !player.isPlaying && !FrontRoomsScreenVideo.HeldFor(this)) player.Play();
            return;
        }
        preparing = true;
        player.Prepare();
    }

    void OnPrepared(VideoPlayer source)
    {
        preparing = false;
        failed = false;
        ServeSoundSeek();   // V5: a sounding screen's first view seeks before the first Play
        // VL245: a warm-up plays to its first decoded frame too (Update pauses it there while nothing is in view)
        if ((visibleScreens.Count > 0 || warming) && !FrontRoomsScreenVideo.HeldFor(this)) source.Play();
    }

    /// <summary>
    /// R3: the close-up's bank plays on <see cref="CloseUpClock"/> (it runs at timeScale 0) from the frame the close-up
    /// opens; every bank is on its decided clock otherwise. Called on the game's Inspecting event and every Update.
    /// </summary>
    internal void SyncCloseUpClock()
    {
        if (player == null) return;
        var want = FrontRoomsScreenVideo.IsCloseUpBank(this);
        if (want == onCloseUpClock) return;
        onCloseUpClock = want;
        var clock = want ? CloseUpClock : decidedClock;
        if (player.timeUpdateMode != clock) player.timeUpdateMode = clock;
    }

    internal static void SyncCloseUpClocks()
    {
        foreach (var bank in Banks.Values)
            if (bank != null) bank.SyncCloseUpClock();
    }

    void OnError(VideoPlayer source, string message)
    {
        preparing = false;
        warming = false;   // VL245: Update warms again after the 2 s retry
        // R3: an error while the player is still prepared (it plays on) changes nothing; one that leaves it unprepared
        // means no ad on the screen until a retry prepares it again.
        if (player == null || !player.isPrepared) { failed = true; pictured = false; }
        retryAt = Time.unscaledTime + 2f;
        Debug.LogWarning("[FrontRoomsScreenVideo] Could not prepare '" + streamFile + "': " + message);
    }

    void Update()
    {
        if (player == null) return;
        SyncCloseUpClock();
        // The decoded frame -> the screens' RT (only on a new frame).
        CopyNewFrame();
        // VL245: a decoded frame (frameReady, a frame index or a presentation time: a platform may report only one of them)
        // copied into the screens' RT and copied again on the next Update = it is in the RT; a warm-up with nothing in view
        // pauses on it
        if (!hasFrame && copiedPicture)
        {
            if (frameSeen)
            {
                hasFrame = true;
                if (player.sendFrameReadyEvents) player.sendFrameReadyEvents = false;
            }
            frameSeen = true;
        }
        if (warming && hasFrame)
        {
            warming = false;
            if (visibleScreens.Count == 0 && player.isPlaying) player.Pause();
        }
        // C26: the hold pauses the decoder; on release PrepareIfNeeded plays it again. R3: the close-up's own bank is
        // not held (HeldFor), so the inspected screen keeps playing; every other bank is held exactly as before.
        if (FrontRoomsScreenVideo.HeldFor(this)) { if (player.isPlaying) player.Pause(); }
        else if (visibleScreens.Count > 0) PrepareIfNeeded();
        else if (warming) { if (player.isPrepared && !player.isPlaying) player.Play(); }
        else if (player.isPlaying) player.Pause();
        // VL245: a warm-up the hold or a decoder error interrupted starts again
        if (!hasFrame && !warming && screens.Count > 0) Warm();
        ServeSoundSeek();   // V5: a request still waiting (player not prepared, world held, no broadcast yet)
    }

    // Quit: Unity 6.6's video shutdown (VideoManager::CleanupClass) crashed in AVFoundation's PixelBufferBlitter when it
    // met a player still bound to its target (an editor batch EditorApplication.Exit from Play, 2026-10-29; a player's
    // quit takes the same engine path). Every bank lets go of its player as quitting starts, before the engine tears down.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void HookQuit()
    {
        Application.quitting -= ReleaseAllForQuit;
        Application.quitting += ReleaseAllForQuit;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.quitting -= ReleaseAllForQuit;
        UnityEditor.EditorApplication.quitting += ReleaseAllForQuit;
#endif
    }

    static void ReleaseAllForQuit()
    {
        foreach (var bank in Banks.Values)
            if (bank != null) bank.ReleasePlayer();
    }

    /// <summary>Stops the player and unbinds it from its target and events (idempotent): OnDestroy's first half.</summary>
    void ReleasePlayer()
    {
        if (player == null) return;
        player.Stop();
        player.prepareCompleted -= OnPrepared;
        player.errorReceived -= OnError;
        player.frameReady -= OnFrameReady;
        player.sendFrameReadyEvents = false;
        // The player lets go of its target before either RT is released: Unity's video teardown never meets a freed RT.
        player.targetTexture = null;
    }

    void OnDestroy()
    {
        if (player != null)
        {
            player.Stop();
            player.prepareCompleted -= OnPrepared;
            player.errorReceived -= OnError;
            player.frameReady -= OnFrameReady;
            player.sendFrameReadyEvents = false;
            // The player lets go of its target before either RT is released: Unity's video teardown never meets a freed RT.
            player.targetTexture = null;
        }
        if (decodeTexture != null)
        {
            decodeTexture.Release();
            Destroy(decodeTexture);
        }
        if (targetTexture != null)
        {
            targetTexture.Release();
            Destroy(targetTexture);
        }
        if (!string.IsNullOrEmpty(bankKey) && Banks.TryGetValue(bankKey, out var current) && current == this)
            Banks.Remove(bankKey);
    }

    // ---------------------------------------------------------------- V5 picture sync (agreed with 声音设计, 2026-10-22 15:2x)
    // Audio is the clock. When a sounding screen (SoundRole != None) comes into view, this shared player seeks once to
    // FrontRooms.Audio.FrontRoomsScreenSound.LoopTime (声音设计's: 0-16 s into the playing loop, -1 while none plays), modulo
    // the clip's length. The request waits for a prepared player, a world that is not held (C26) and a running broadcast
    // (the broadcast can start after the picture: the director finds a sounding screen within a second, a power-on starts
    // the loop again, and FMOD reads 0 from start() until the loop really runs). It is dropped when its screen leaves view.
    // The close-up's bank never takes one (R3: the inspected TV plays on from where it is). A None screen never asks,
    // nothing seeks every frame, and a picture already within one frame of the broadcast is left alone.
    // Re-seek on release (声音设计 10-29): when the world hold (the pause card or a C26 close-up) releases, each bank with a
    // sounding screen in view takes one such request too, since the picture and the broadcast resume 40-60 ms apart.
    FrontRoomsScreenVideo soundSeekFor;
    static System.Func<float> loopTime;
    static bool loopTimeLooked;

    /// <summary>V5: FrontRooms.Audio.FrontRoomsScreenSound.LoopTime, read by reflection (resolved once); -1 when the build lacks it.</summary>
    internal static float SoundLoopTime
    {
        get
        {
            if (!loopTimeLooked)
            {
                loopTimeLooked = true;
                var type = typeof(FrontRoomsScreenAdBank).Assembly.GetType("FrontRooms.Audio.FrontRoomsScreenSound");
                var prop = type != null ? type.GetProperty("LoopTime", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static) : null;
                var get = prop != null && prop.PropertyType == typeof(float) ? prop.GetGetMethod() : null;
                if (get != null) loopTime = (System.Func<float>)System.Delegate.CreateDelegate(typeof(System.Func<float>), get);
            }
            return loopTime != null ? loopTime() : -1f;
        }
    }

    /// <summary>V5: <paramref name="screen"/>, a sounding screen, came into view on this bank (its visibility edge).</summary>
    internal void SyncToSound(FrontRoomsScreenVideo screen)
    {
        if (screen == null || FrontRoomsScreenVideo.IsCloseUpBank(this)) return;
        soundSeekFor = screen;
        ServeSoundSeek();
    }

    /// <summary>
    /// V5: the world hold released (FrontRooms3DGame.Paused false, or Inspecting false). One request per bank that has a
    /// sounding screen in view, served by Update like an edge's (after the Play). Not while the world is still held (a
    /// pause card over a close-up); never the close-up's own bank (still on the close-up clock here); never a bank whose
    /// screens in view are all None. A request already waiting is the same one.
    /// </summary>
    internal static void SyncToSoundOnRelease()
    {
        if (FrontRoomsScreenVideo.WorldHeld) return;
        foreach (var bank in Banks.Values)
        {
            if (bank == null || bank.onCloseUpClock) continue;
            foreach (var s in bank.visibleScreens)
                if (s != null && s.SoundRole != FrontRoomsScreenSoundRole.None) { bank.soundSeekFor = s; break; }
        }
    }

    void ServeSoundSeek()
    {
        if (soundSeekFor == null) return;
        if (player == null || !visibleScreens.Contains(soundSeekFor) || FrontRoomsScreenVideo.IsCloseUpBank(this)) { soundSeekFor = null; return; }
        if (!player.isPrepared || FrontRoomsScreenVideo.WorldHeld) return;
        var loop = SoundLoopTime;
        if (loop <= 0f) return;   // -1: no broadcast; 0: started, not running yet
        soundSeekFor = null;
        if (!player.canSetTime) return;
        var length = player.length;
        var frame = player.frameRate > 0f ? 1d / player.frameRate : 1d / 30d;
        // The decoder shows a seek from the next frame on: aim where the broadcast will be then (at most two clip frames on).
        var at = loop + System.Math.Min(Time.unscaledDeltaTime, 2d * frame);
        var target = length > 0d ? at % length : at;
        var off = player.time - target;
        if (length > 0d) off -= length * System.Math.Round(off / length);   // the shorter way round the loop
        if (System.Math.Abs(off) > frame) player.time = target;
    }
}

/// <summary>V5 (声音设计, 2026-10-22): the sound role of a screen (FrontRoomsScreenVideo.SoundRole). None = no sound; TitleTV = the title
/// corridor TV (T6); Beacon = the station TV heard through walls (S2).</summary>
public enum FrontRoomsScreenSoundRole { None, TitleTV, Beacon }

/// <summary>The CRT tube's screen-effects level (C26 "Screen effects setting: Full, Reduced, Off?", open). Full by default;
/// WebGL and phones draw at most Reduced (<see cref="FrontRoomsScreenVideo.EffectiveScreenEffects"/>).</summary>
public enum FrontRoomsScreenEffects { Full, Reduced, Off }
