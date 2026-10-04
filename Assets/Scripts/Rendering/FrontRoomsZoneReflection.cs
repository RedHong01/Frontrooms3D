using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// The world's default specular reflection, one baked HDR cube per zone type
/// (Resources/Rendering/Reflections, captured by FrontRoomsReflectionCapture in
/// the editor). FrontRoomsLook.SetZoneReflection is the public entry point.
///
/// How it reaches URP 17: RenderSettings.defaultReflectionMode = Custom and
/// RenderSettings.customReflectionTexture = a cube. With probe blending off
/// (FrontRooms_URP.asset) every renderer without a probe samples that cube as
/// unity_SpecCube0; with blending on (G7) it is the fallback
/// _GlossyEnvironmentCubeMap that URP binds per camera from
/// ReflectionProbe.defaultTexture. Proven in play mode by
/// FrontRoomsGlassVerification.RunReflectionTestBatch.
///
/// Crossfade, two modes:
///  - Blend (desktop): one HDR cube render texture is the custom reflection; a
///    zone change redraws it every frame as lerp(from, to) per face and per mip
///    (Hidden/FrontRooms/ReflectionBlend; 6 faces x 9 mips = 54 tiny draws per
///    frame, only while a fade runs). The intensity is lerped with it.
///  - Dip (WebGL, or when the HDR render texture is unsupported): the intensity
///    fades to 0, the cube asset is swapped at the midpoint, and it fades back.
///    Uses only RenderSettings, so nothing depends on render-to-cube support.
///
/// Intensities are LINEAR fractions of the captured light (the cubes were captured
/// under the game's own lamps, so 1.0 is physical). Unity applies the Lighting
/// window's slider in gamma (decode = GammaToLinear(slider); measured in
/// FrontRoomsGlassVerification), so this writes RenderSettings.reflectionIntensity =
/// LinearToGamma(linear). The scene's own .3 is 0.073 linear. Kept at or below
/// MaxLinear (0.5) until per-room box-projected probes land (G7: dead-lamp corridors,
/// zone borders), and because a baked cube shows a frozen copy of the wallpaper print,
/// which will animate later (wallpaper chat's caveat).
///
/// Also publishes the global _FR_ZoneReflNominal: the steady (non-dipping) linear
/// intensity. FrontRooms/Glass divides its reflection floor by it, so glass dips with
/// everything else in the dip mode instead of cancelling the dip.
/// </summary>
public static class FrontRoomsZoneReflection
{
    public const string ResourceFolder = "Rendering/Reflections/";
    public static readonly string[] CubeNames = { "Refl_Level0", "Refl_Office", "Refl_Tall", "Refl_DeadLamp" };
    /// <summary>Upper bound for the linear zone intensity until per-room probes land (G7).</summary>
    public const float MaxLinear = .5f;

    /// <summary>Linear reflection intensity per zone (index = ReflectionZone), each clamped to MaxLinear.</summary>
    public static readonly float[] ZoneLinear = { .5f, .5f, .45f, .5f };

    public static readonly int NominalId = Shader.PropertyToID("_FR_ZoneReflNominal");

    /// <summary>Set false to force the dip mode everywhere (for comparisons).</summary>
    public static bool AllowBlendTexture = true;

    static Cubemap[] cubes;
    static RenderTexture mix, snapshot;
    static Material blendMaterial;
    static Mesh quad;
    static bool hasZone;
    static FrontRoomsLook.ReflectionZone zone, target;
    static Texture fromTexture, toTexture;
    static float fromLinear, toLinear, progress, duration;
    static bool fading, swappedAtMidpoint;
    static FrontRoomsZoneReflectionDriver driver;
    static readonly int CubeAId = Shader.PropertyToID("_CubeA");
    static readonly int CubeBId = Shader.PropertyToID("_CubeB");
    static readonly int BlendId = Shader.PropertyToID("_Blend");
    static readonly int MipId = Shader.PropertyToID("_Mip");
    static readonly int FaceId = Shader.PropertyToID("_Face");
    static readonly int SizeId = Shader.PropertyToID("_Size");
    static readonly int FlipId = Shader.PropertyToID("_FlipY");

    /// <summary>True once a zone has been set in this session.</summary>
    public static bool Active => hasZone;
    /// <summary>The zone being shown (the target while a fade runs).</summary>
    public static FrontRoomsLook.ReflectionZone Zone => target;
    public static bool Fading => fading;
    /// <summary>"blend", "dip" or "none": how the last change was applied (tests and logs).</summary>
    public static string Mode { get; private set; } = "none";

    /// <summary>
    /// Row-order of the render target relative to the cube face (see the blend shader).
    /// Measured on Metal by the play-mode test (false there); set from
    /// SystemInfo.graphicsUVStartsAtTop at start-up (GL-family targets are the opposite
    /// case). D3D12 / Vulkan: run RunReflectionTestBatch there before the High tier ships.
    /// </summary>
    public static bool FlipY = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Release();
        hasZone = false;
        fading = false;
        Mode = "none";
        FlipY = !SystemInfo.graphicsUVStartsAtTop;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Application.quitting -= Release;
        Application.quitting += Release;   // in the editor this fires when Play mode stops
    }

    // A Single load (R restarts the run by reloading the scene) brings the scene's own
    // RenderSettings back: forget the zone so the next SetZoneReflection snaps.
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        fading = false;
        hasZone = false;
        Shader.SetGlobalFloat(NominalId, 0f);
    }

    public static Cubemap Cube(FrontRoomsLook.ReflectionZone z)
    {
        if (cubes == null) cubes = new Cubemap[CubeNames.Length];
        var i = (int)z;
        if (i < 0 || i >= cubes.Length) return null;
        if (cubes[i] == null) cubes[i] = Resources.Load<Cubemap>(ResourceFolder + CubeNames[i]);
        return cubes[i];
    }

    /// <summary>Linear intensity of a zone (fraction of the captured light).</summary>
    public static float LinearOf(FrontRoomsLook.ReflectionZone z)
    {
        var i = (int)z;
        var v = i >= 0 && i < ZoneLinear.Length ? ZoneLinear[i] : MaxLinear;
        return Mathf.Clamp(v, 0f, MaxLinear);
    }

    /// <summary>Write a linear intensity to RenderSettings (the slider is applied in gamma).</summary>
    static void WriteLinear(float linear) => RenderSettings.reflectionIntensity = Mathf.LinearToGammaSpace(Mathf.Max(0f, linear));

    static float CurrentLinear() => Mathf.GammaToLinearSpace(Mathf.Max(0f, RenderSettings.reflectionIntensity));

    static void PublishNominal(float linear) => Shader.SetGlobalFloat(NominalId, Mathf.Max(0f, linear));

    /// <summary>See FrontRoomsLook.SetZoneReflection.</summary>
    public static void Set(FrontRoomsLook.ReflectionZone z, float blendSeconds)
    {
        if (hasZone && z == target)
        {
            // Same request every frame: nothing to do, unless something rewrote RenderSettings.
            if (RenderSettings.defaultReflectionMode != DefaultReflectionMode.Custom || RenderSettings.customReflectionTexture == null) Reapply();
            return;
        }
        var cube = Cube(z);
        if (cube == null)
        {
            Debug.LogWarning("[FrontRoomsLook] No reflection cube Resources/" + ResourceFolder + CubeNames[(int)z] + "; run FrontRooms > Rendering > Capture zone reflection cubemaps.");
            return;
        }
        if (!hasZone || blendSeconds <= 0f || !Application.isPlaying)
        {
            Snap(z, cube);
            return;
        }

        var useBlend = UseBlendTexture(cube);
        if (useBlend)
        {
            EnsureTargets(cube);
            if (fading && Mode == "blend")
            {
                // Retarget mid-fade: freeze what is on screen now and fade from that.
                EnsureSnapshot();
                Draw(snapshot, mix, mix, 0f);
                fromTexture = snapshot;
            }
            else
            {
                fromTexture = CurrentTexture();
            }
            RenderSettings.customReflectionTexture = mix;
            Mode = "blend";
        }
        else
        {
            fromTexture = RenderSettings.customReflectionTexture;
            Mode = "dip";
        }
        toTexture = cube;
        fromLinear = CurrentLinear();
        toLinear = LinearOf(z);
        target = z;
        progress = 0f;
        duration = blendSeconds;
        fading = true;
        swappedAtMidpoint = false;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        EnsureDriver();
        Step(0f);
    }

    /// <summary>Switch at once, even to the zone already shown (captures, tests, edit mode).</summary>
    public static void SetImmediate(FrontRoomsLook.ReflectionZone z)
    {
        var cube = Cube(z);
        if (cube == null)
        {
            Debug.LogWarning("[FrontRoomsLook] No reflection cube Resources/" + ResourceFolder + CubeNames[(int)z] + ".");
            return;
        }
        Snap(z, cube);
    }

    static void Snap(FrontRoomsLook.ReflectionZone z, Cubemap cube)
    {
        fading = false;
        zone = target = z;
        hasZone = true;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        if (Application.isPlaying && UseBlendTexture(cube))
        {
            // Keep one render texture bound for the whole run, so later fades never rebind.
            EnsureTargets(cube);
            Draw(mix, cube, cube, 1f);
            RenderSettings.customReflectionTexture = mix;
            Mode = "blend";
        }
        else
        {
            RenderSettings.customReflectionTexture = cube;
            Mode = "dip";
        }
        WriteLinear(LinearOf(z));
        PublishNominal(LinearOf(z));
    }

    /// <summary>Advance a running fade. Called by the hidden driver every frame.</summary>
    public static void Step(float dt)
    {
        if (!fading) return;
        progress = duration <= 0f ? 1f : Mathf.Clamp01(progress + dt / duration);
        var s = Mathf.SmoothStep(0f, 1f, progress);
        if (Mode == "blend")
        {
            Draw(mix, fromTexture, toTexture, s);
            var lin = Mathf.Lerp(fromLinear, toLinear, s);
            WriteLinear(lin);
            PublishNominal(lin);   // no dip: the steady value is the current one
        }
        else
        {
            if (!swappedAtMidpoint && progress >= .5f)
            {
                RenderSettings.customReflectionTexture = toTexture;
                swappedAtMidpoint = true;
            }
            WriteLinear(progress < .5f
                ? Mathf.Lerp(fromLinear, 0f, Mathf.SmoothStep(0f, 1f, progress * 2f))
                : Mathf.Lerp(0f, toLinear, Mathf.SmoothStep(0f, 1f, progress * 2f - 1f)));
            PublishNominal(progress < .5f ? fromLinear : toLinear);   // the dip itself is not nominal
        }
        if (progress >= 1f)
        {
            fading = false;
            zone = target;
            hasZone = true;
        }
    }

    static Texture CurrentTexture()
    {
        if (fading) return toTexture;
        return Cube(zone);
    }

    static bool UseBlendTexture(Cubemap cube)
    {
        if (!AllowBlendTexture || cube == null) return false;
        if (Application.platform == RuntimePlatform.WebGLPlayer) return false;
        if (Format() == RenderTextureFormat.Default) return false;
        if (BlendMaterial() == null) return false;
        return true;
    }

    static RenderTextureFormat Format()
    {
        if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float)) return RenderTextureFormat.RGB111110Float;
        if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf)) return RenderTextureFormat.ARGBHalf;
        return RenderTextureFormat.Default;
    }

    static Material BlendMaterial()
    {
        if (blendMaterial != null) return blendMaterial;
        var shader = Resources.Load<Shader>("Rendering/FrontRoomsReflectionBlend");
        if (shader == null || !shader.isSupported) return null;
        blendMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave, name = "FrontRooms reflection blend" };
        return blendMaterial;
    }

    static RenderTexture NewCube(int size, string name)
    {
        var mips = Mathf.FloorToInt(Mathf.Log(size, 2f)) + 1;
        var desc = new RenderTextureDescriptor(size, size, Format(), 0, mips)
        {
            dimension = TextureDimension.Cube,
            useMipMap = true,
            autoGenerateMips = false,
            sRGB = false,
            msaaSamples = 1,
        };
        var rt = new RenderTexture(desc) { name = name, hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp };
        rt.Create();
        return rt;
    }

    static void EnsureTargets(Cubemap like)
    {
        var size = Mathf.Max(8, like.width);
        if (mix != null && mix.width == size && mix.IsCreated()) return;
        if (mix != null) { mix.Release(); Object.DestroyImmediate(mix); }
        mix = NewCube(size, "FrontRooms zone reflection");
        if (snapshot != null) { snapshot.Release(); Object.DestroyImmediate(snapshot); snapshot = null; }
    }

    static void EnsureSnapshot()
    {
        if (snapshot != null && snapshot.width == mix.width && snapshot.IsCreated()) return;
        snapshot = NewCube(mix.width, "FrontRooms zone reflection (fade start)");
    }

    static Mesh Quad()
    {
        if (quad != null) return quad;
        quad = new Mesh { name = "FrontRooms reflection blend quad", hideFlags = HideFlags.HideAndDontSave };
        quad.vertices = new[] { new Vector3(-1f, -1f, 0f), new Vector3(1f, -1f, 0f), new Vector3(1f, 1f, 0f), new Vector3(-1f, 1f, 0f) };
        quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        quad.bounds = new Bounds(Vector3.zero, Vector3.one * 1e4f);
        return quad;
    }

    /// <summary>Write lerp(a, b, w) into every face and mip of dst.</summary>
    public static void Draw(RenderTexture dst, Texture a, Texture b, float w)
    {
        var material = BlendMaterial();
        if (material == null || dst == null || a == null || b == null) return;
        var previous = RenderTexture.active;
        material.SetTexture(CubeAId, a);
        material.SetTexture(CubeBId, b);
        material.SetFloat(BlendId, w);
        material.SetFloat(FlipId, FlipY ? 1f : 0f);
        var mesh = Quad();
        var sourceMips = Mathf.Min(a.mipmapCount, b.mipmapCount);
        for (var mip = 0; mip < dst.mipmapCount; mip++)
        {
            var size = Mathf.Max(1, dst.width >> mip);
            material.SetFloat(SizeId, size);
            material.SetFloat(MipId, Mathf.Min(mip, sourceMips - 1));
            for (var face = 0; face < 6; face++)
            {
                Graphics.SetRenderTarget(dst, mip, (CubemapFace)face);
                material.SetFloat(FaceId, face);
                material.SetPass(0);
                Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
            }
        }
        RenderTexture.active = previous;
    }

    static void EnsureDriver()
    {
        if (driver != null || !Application.isPlaying) return;
        var go = new GameObject("FrontRooms zone reflection") { hideFlags = HideFlags.HideAndDontSave };
        Object.DontDestroyOnLoad(go);
        driver = go.AddComponent<FrontRoomsZoneReflectionDriver>();
    }

    /// <summary>Re-assert the zone's cube and intensity (FrontRoomsLook.ApplyAmbient calls this).</summary>
    public static void Reapply()
    {
        if (!hasZone) return;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        if (Mode == "blend" && mix != null) RenderSettings.customReflectionTexture = mix;
        else if (!fading) RenderSettings.customReflectionTexture = Cube(zone);
        if (!fading)
        {
            WriteLinear(LinearOf(zone));
            PublishNominal(LinearOf(zone));
        }
    }

    /// <summary>Free the blend render textures and the driver (Play mode stop, quit).</summary>
    public static void Release()
    {
        if (mix != null) { mix.Release(); Object.DestroyImmediate(mix); mix = null; }
        if (snapshot != null) { snapshot.Release(); Object.DestroyImmediate(snapshot); snapshot = null; }
        if (driver != null) { Object.DestroyImmediate(driver.gameObject); driver = null; }
    }
}
