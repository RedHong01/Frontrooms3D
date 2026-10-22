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

    /// <summary>A close-up or the pause card holds the world (C26): no focus, no E, no playback.</summary>
    internal static bool WorldHeld => pausedByGame || FrontRooms3DGame.InspectingNow;

    // Hooked once per play session, before any scene loads, so a screen spawned
    // under the pause card still sees it (FrontRoomsInspect hooks the same way).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void HookWorldHold()
    {
        pausedByGame = false;
        heldFrame = -10;
        FrontRooms3DGame.Paused -= OnGamePaused;
        FrontRooms3DGame.Paused += OnGamePaused;
    }

    static void OnGamePaused(bool paused) => pausedByGame = paused;

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

    public bool IsPoweredOn => poweredOn;
    public bool IsFocused => focusedScreen == this;
    public string InteractionPrompt => poweredOn ? "E  TURN OFF" : "E  TURN ON";
    public float InteractionDistance => interactionDistance;

    /// <summary>Attach the runtime surface to an authored or procedurally spawned CRT.</summary>
    public static FrontRoomsScreenVideo Attach(GameObject monitor, string adFile = null)
    {
        if (monitor == null) return null;
        var screen = monitor.GetComponent<FrontRoomsScreenVideo>();
        if (screen == null) screen = monitor.AddComponent<FrontRoomsScreenVideo>();
        if (!string.IsNullOrEmpty(adFile)) screen.streamFile = adFile;
        return screen;
    }

    void Awake()
    {
        poweredOn = startsPowered;
        if (!Application.isPlaying) return;
        bank = FrontRoomsScreenAdBank.Acquire(streamFile, desktopClip);
        CreateSurface();
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
        var visible = poweredOn && camera != null && surfaceRenderer.isVisible &&
            Vector3.Distance(camera.transform.position, surfaceRenderer.bounds.center) <= visibleDistance;
        if (visible != countedVisible)
        {
            countedVisible = visible;
            bank.SetVisible(this, visible);
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
        }

        focusedScreen = next;
        if (focusedScreen != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            focusedScreen.TogglePower();
    }

    void TogglePower()
    {
        poweredOn = !poweredOn;
        if (surfaceRenderer != null) surfaceRenderer.enabled = poweredOn;
        if (!poweredOn)
        {
            countedVisible = false;
            bank?.SetVisible(this, false);
        }
    }

    internal void BindTexture(Texture texture)
    {
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

    public RenderTexture TargetTexture => targetTexture;

    public static FrontRoomsScreenAdBank Acquire(string file, VideoClip clip)
    {
        file = string.IsNullOrEmpty(file) ? FrontRoomsScreenVideo.DefaultStreamFile : file;
        var key = file + "|" + (clip != null ? clip.GetInstanceID().ToString() : "url");
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
            // C26: never Play under a close-up or the pause card.
            if (player != null && player.isPrepared && !player.isPlaying && !FrontRoomsScreenVideo.WorldHeld) player.Play();
            return;
        }
        preparing = true;
        player.Prepare();
    }

    void OnPrepared(VideoPlayer source)
    {
        preparing = false;
        if (visibleScreens.Count > 0 && !FrontRoomsScreenVideo.WorldHeld) source.Play();
    }

    void OnError(VideoPlayer source, string message)
    {
        preparing = false;
        retryAt = Time.unscaledTime + 2f;
        Debug.LogWarning("[FrontRoomsScreenVideo] Could not prepare '" + streamFile + "': " + message);
    }

    void Update()
    {
        if (player == null) return;
        // C26: the hold pauses the decoder; on release PrepareIfNeeded plays it again.
        if (FrontRoomsScreenVideo.WorldHeld) { if (player.isPlaying) player.Pause(); }
        else if (visibleScreens.Count > 0) PrepareIfNeeded();
        else if (player.isPlaying) player.Pause();
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
}
