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
    // URP Unlit in every player build; a Shader.Find by name alone does not.
    const string AdSurfaceTemplatePath = "Media/CRT_AdSurface";
    const string AdSurfaceShaderPath = "Rendering/FrontRoomsScreenVideo";

    static readonly List<FrontRoomsScreenVideo> ActiveScreens = new List<FrontRoomsScreenVideo>();
    static FrontRoomsScreenVideo focusedScreen;
    static Camera fallbackCamera;
    static Material adSurfaceTemplate;
    static bool warnedNoSurfaceMaterial;

    // The world hold (research/inspect/contracts/map_inspect.md C26). A close-up
    // (FrontRooms3DGame.InspectingNow) or the pause card holds the world: no screen
    // takes focus or answers E, and no bank plays. Paused is an event, so its last
    // value is kept here; the game sends false on resume and on teardown.
    static bool pausedByGame;
    static int heldFrame = -10;

    // R3 (Red 10-29; research/inspect/NOTE_1029_R3_station_tv.md): the close-up of the playing station TV is the hold's
    // one exemption. The screen whose READ target is FrontRoomsInspect.Current, cached per target.
    static FrontRoomsInspectable inspectedTarget;
    static FrontRoomsScreenVideo inspectedScreen;

    /// <summary>A close-up or the pause card holds the world (C26): no focus, no E, no playback.</summary>
    internal static bool WorldHeld => pausedByGame || FrontRooms3DGame.InspectingNow;

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
        var visible = !stillMode && poweredOn && camera != null && surfaceRenderer.isVisible &&
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
        if (WorldHeld) heldFrame = Time.frameCount;
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
        // Never while the world is held: the close-up's own solve stays as it was opened.
        if (want && !WorldHeld) FitCloseUpDistance();
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
        // dist[0] (the base distance) is FitCloseUpDistance's: 0 = the default solve.
        inspectDef = new InspectDef { kind = "flat", verb = "read", ship = true, sound = "plastic", dist = new float[1] };
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

    bool WantsStill() => deskInMaze && FrontRoomsStationPresence.FloorHasStation;

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
        if (stillMode) texture = stillTexture;
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
    /// 1. Resources/Media/CRT_AdSurface.mat (URP Unlit, opaque, white). It keeps the
    ///    editor look in players: fog, the depth and normals passes, and the glass RT
    ///    traces it as emissive.
    /// 2. Resources/Rendering/FrontRoomsScreenVideo.shader (FrontRooms/ScreenVideo),
    ///    a minimal unlit fallback that Resources also keeps in every build.
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

    /// <summary>R3: the clock the close-up's bank plays on; it runs at timeScale 0.</summary>
    public const VideoTimeUpdateMode CloseUpClock = VideoTimeUpdateMode.UnscaledGameTime;

    public RenderTexture TargetTexture => targetTexture;
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
            // The authored CRT face is about 1.47:1. The 16:9 creative is
            // letterboxed into this 640x434 target so type is never stretched.
            targetTexture = new RenderTexture(640, 434, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                name = "FrontRooms / shared CRT ad RT",
                useMipMap = false,
                autoGenerateMips = false,
                antiAliasing = 1,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            targetTexture.Create();
            player.targetTexture = targetTexture;
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
    }

    public void SetVisible(FrontRoomsScreenVideo screen, bool visible)
    {
        if (screen == null) return;
        if (visible) visibleScreens.Add(screen);
        else visibleScreens.Remove(screen);
        if (visible) PrepareIfNeeded();
        else if (visibleScreens.Count == 0 && player != null && player.isPlaying) player.Pause();
    }

    public void Unregister(FrontRoomsScreenVideo screen)
    {
        if (screen == null) return;
        screens.Remove(screen);
        visibleScreens.Remove(screen);
        if (visibleScreens.Count == 0 && player != null && player.isPlaying) player.Pause();
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
        if (visibleScreens.Count > 0 && !FrontRoomsScreenVideo.HeldFor(this)) source.Play();
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
        // C26: the hold pauses the decoder; on release PrepareIfNeeded plays it again. R3: the close-up's own bank is
        // not held (HeldFor), so the inspected screen keeps playing; every other bank is held exactly as before.
        if (FrontRoomsScreenVideo.HeldFor(this)) { if (player.isPlaying) player.Pause(); }
        else if (visibleScreens.Count > 0) PrepareIfNeeded();
        else if (player.isPlaying) player.Pause();
        ServeSoundSeek();   // V5: a request still waiting (player not prepared, world held, no broadcast yet)
    }

    void OnDestroy()
    {
        if (player != null)
        {
            player.Stop();
            player.prepareCompleted -= OnPrepared;
            player.errorReceived -= OnError;
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
