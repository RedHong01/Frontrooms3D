using System.Collections.Generic;
using System.IO;
using UnityEngine;
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

    static readonly List<FrontRoomsScreenVideo> ActiveScreens = new List<FrontRoomsScreenVideo>();
    static FrontRoomsScreenVideo focusedScreen;
    static Camera fallbackCamera;

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

        // One screen owns the raycast for the whole family. This gives every
        // monitor a shared focus/interaction path instead of N identical rays.
        if (ActiveScreens.Count > 0 && ActiveScreens[0] == this) UpdateFocusedScreen(camera);
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
        if (focusedScreen != null && FrontRoomsInput.UseDown)
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
        surfaceObject = new GameObject("screen surface / shared ad video");
        surfaceObject.transform.SetParent(transform, false);
        surfaceObject.layer = gameObject.layer;
        surfaceObject.transform.localPosition = screenAnchor + Vector3.forward * .004f;

        var filter = surfaceObject.AddComponent<MeshFilter>();
        filter.sharedMesh = MakeQuad(screenSize.x, screenSize.y);
        surfaceRenderer = surfaceObject.AddComponent<MeshRenderer>();
        surfaceMaterial = MakeMaterial(bank != null ? bank.TargetTexture : null);
        surfaceRenderer.sharedMaterial = surfaceMaterial;
        surfaceRenderer.enabled = poweredOn;

        surfaceCollider = surfaceObject.AddComponent<BoxCollider>();
        surfaceCollider.isTrigger = true;
        surfaceCollider.size = new Vector3(screenSize.x, screenSize.y, .012f);
        surfaceCollider.center = new Vector3(0f, 0f, .002f);
    }

    static Mesh MakeQuad(float width, float height)
    {
        var mesh = new Mesh { name = "FrontRooms CRT screen quad" };
        var x = width * .5f;
        var y = height * .5f;
        mesh.vertices = new[]
        {
            new Vector3(-x, -y, 0f), new Vector3(x, -y, 0f),
            new Vector3(x, y, 0f), new Vector3(-x, y, 0f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(1f, 1f), new Vector2(0f, 1f)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    static Material MakeMaterial(Texture texture)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
        var material = new Material(shader) { name = "FrontRooms / CRT ad surface" };
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
            if (player != null && player.isPrepared && !player.isPlaying) player.Play();
            return;
        }
        preparing = true;
        player.Prepare();
    }

    void OnPrepared(VideoPlayer source)
    {
        preparing = false;
        if (visibleScreens.Count > 0) source.Play();
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
        if (visibleScreens.Count > 0) PrepareIfNeeded();
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
