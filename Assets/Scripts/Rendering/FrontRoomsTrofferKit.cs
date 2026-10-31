using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The ceiling troffer (visual line, lamp options; Red 2026-10-31 picked L03 with the lens at 1.8): an air-handling 2'x4'
/// lay-in troffer, a matte black return channel along each long side, a 20 mm lit return wall and a prismatic lens over four
/// tubes. Kit: Resources/Props/Troffer (Troffer_L03.fbx, its lens and trim sheets; built by Tools/lookdev/troffer).
///
/// No ceiling hole: the fixture is lowered by Hang so the lens face sits 1 mm below the ceiling plane and the T-bar's outer
/// skirt is the outer wall (the render stage's no-hole route, as Red saw it). Under the fixture root, on the ceiling plane:
///   Lens     the ONE renderer the lamp logic drives (MapWorld.TickFixtures / RoomStream.ApplyFixtureVisual): lens, lit
///            return walls and tubes are all on it, masked by its emission map, so flicker, dim, dead and the warning
///            bursts reach all of it through the same property block as today. One mesh, no LOD (a LOD copy would never
///            get the lamp's level). Shadows off. No collider.
///   Housing  desktop only: the unlit exterior (T-bar, door frame, return channels, hinges, latches) on the shared trim
///            sheet; no glow, shadows off, no collider; LODGroup 4 m / 12 m, culled at 46 m.
///   Soil     desktop only: the air-handling stain on the tile beside the lamp (URP Unlit multiply, never hung, culled
///            past 12 m). Transparent, so GlassRT never traces it.
/// Phones (PhoneTier: FrontRoomsHandheld.Active, or the lab's DebugForcePhoneTier; WebGL players always): the lens is the
/// one phone mesh (66 tris; the frame sits in emission-free swatches of the lens atlas), no housing, no soil: 0 extra draws.
/// The behaviour is the same on every platform; only the mesh differs.
///
/// Brightness: LensPeak (1.8) is the lens as seen, as a material colour is passed (gamma), in the start room, the title
/// corridor and the map alike. The stream's lens tops out at it (RoomStream: lerp(.02, LensPeak, level) x the profile
/// colour); the map drives LensDrive(zone lens) = the zone lens colour scaled so the shared Troffer_Lens (2.2, 2.1, 1.8)
/// reads LensPeak (Level 0's lens is that colour x MapWorld.Level0LensEmissionScale, 1 once the map contract lands).
/// The Relay's lens (FrontRoomsRelayLens.FullEmission, 0.80 / 0.76 / 0.65 as passed) is NOT changed: it was set on screen
/// (band peaks near white, gaps and face-shadow darker) and must stay under the troffers' p99; at 1.8 the troffer is 2.25x
/// the Relay as passed (it ties near 0.8), same tint, so the "cut strip of the same lens" kinship holds.
///
/// Names (GlassRT, tests, tools): the map's lens is "Lens", a direct child of "Fixture (x, y)" beside its "Light" (GlassRT
/// pairs them); the stream's keeps "fluorescent diffuser N" (CacheRoomLightVisuals parses it). Nothing here is named
/// "Light", nothing starts with "Key", "Lock ", "Door " or "fluorescent light", and no name holds floor, carpet, ceiling,
/// wall, seal or partition (RoomStream.RefreshRoomMaterials reassigns those).
/// ASCII only.
/// </summary>
public static class FrontRoomsTrofferKit
{
    /// <summary>The lens as seen (the band peak's drive, gamma as passed), everywhere. Red 2026-10-31.</summary>
    public const float LensPeak = 1.8f;
    /// <summary>Troffer_Lens.mat's authored peak, the reference LensDrive scales from when the asset cannot be read.</summary>
    public const float SharedLensPeak = 2.2f;
    public const string LensName = "Lens", HousingName = "Housing", SoilName = "Soil";
    public const string ResourceRoot = "Props/Troffer/";
    public const string Model = "Models/Troffer_L03";
    public const string DesktopPrefab = "Prefabs/Troffer_L03", PhonePrefab = "Prefabs/Troffer_L03_Phone", SoilPrefab = "Prefabs/Troffer_L03_Soil";

    /// <summary>Tools: build the phone tier on desktop too.</summary>
    public static bool DebugForcePhoneTier;
    public static bool PhoneTierOnHandheld = true;
    public static bool PhoneTier
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return true;
#else
            return DebugForcePhoneTier || (PhoneTierOnHandheld && FrontRoomsHandheld.Active);
#endif
        }
    }

    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    static bool loaded, ok, desktopLoaded, desktopOk, warned;
    static Mesh lensMesh, phoneMesh;
    static Material lensMaterial;
    static GameObject housingSource, soilSource;
    static float hang;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        loaded = ok = desktopLoaded = desktopOk = warned = false;
        lensMesh = phoneMesh = null;
        lensMaterial = null;
        housingSource = soilSource = null;
        hang = 0f;
    }

    /// <summary>The kit's assets are in the build (Resources/Props/Troffer). False: callers build what they built before.</summary>
    public static bool Available => Load();
    /// <summary>The lens material (FrontRooms/Surface, _EMISSION, the L03 lens atlas); null when the kit is missing.</summary>
    public static Material LensMaterial => Load() ? lensMaterial : null;
    /// <summary>How far the fixture is lowered below the ceiling plane: the lens face 1 mm under the tile (0.0215 m).</summary>
    public static float Hang => Load() ? hang : 0f;

    // Every tier: the lens material and its maps (through the phone prefab, which holds nothing else) and the model's meshes
    // (the FBX imports no material). The desktop prefab and the soil prefab, which pull in the trim sheet and the stain, are
    // loaded only by the first desktop Build, so a phone never holds their textures.
    static bool Load()
    {
        if (loaded) return ok;
        loaded = true;
        var phone = Resources.Load<GameObject>(ResourceRoot + PhonePrefab);
        var phoneLens = phone != null ? phone.transform.Find(LensName) : null;
        var phoneRenderer = phoneLens != null ? phoneLens.GetComponent<MeshRenderer>() : null;
        var phoneFilter = phoneLens != null ? phoneLens.GetComponent<MeshFilter>() : null;
        lensMaterial = phoneRenderer != null ? phoneRenderer.sharedMaterial : null;
        phoneMesh = phoneFilter != null ? phoneFilter.sharedMesh : null;
        foreach (var m in Resources.LoadAll<Mesh>(ResourceRoot + Model))
            if (m != null && m.name == LensName) { lensMesh = m; break; }
        ok = lensMesh != null && phoneMesh != null && lensMaterial != null;
        // The render stage's no-hole hang: the deepest part of the lens (its face, kit +Y = up into the ceiling) 1 mm below
        // the ceiling plane, so it never z-fights the tile. The phone mesh hangs by the same amount.
        if (ok) hang = lensMesh.bounds.max.y + .001f;
        else Warn(PhonePrefab + ", " + Model);
        return ok;
    }

    static bool LoadDesktop()
    {
        if (desktopLoaded) return desktopOk;
        desktopLoaded = true;
        var desktop = Resources.Load<GameObject>(ResourceRoot + DesktopPrefab);
        var housing = desktop != null ? desktop.transform.Find(HousingName) : null;
        housingSource = housing != null ? housing.gameObject : null;
        soilSource = Resources.Load<GameObject>(ResourceRoot + SoilPrefab);
        desktopOk = housingSource != null && soilSource != null;
        if (!desktopOk) Warn(DesktopPrefab + ", " + SoilPrefab);
        return desktopOk;
    }

    static void Warn(string what)
    {
        if (warned) return;
        warned = true;
        Debug.LogWarning("[FrontRoomsTrofferKit] Resources/" + ResourceRoot + " is incomplete (" + what + "): the lamps keep the flat lens.");
    }

    /// <summary>The map's lamp: the kit centred on the fixture root, which sits on the ceiling plane (MapWorld.BuildFixture).
    /// Returns the lens renderer named "Lens", or null (nothing built) when the kit is missing.</summary>
    public static MeshRenderer Build(Transform fixtureRoot)
    {
        return Build(fixtureRoot, Vector3.zero, Quaternion.identity, LensName);
    }

    /// <summary>
    /// Builds the troffer under <paramref name="fixtureRoot"/>: the lens (returned, named <paramref name="lensName"/>), and on
    /// desktop the housing and the soil stain. <paramref name="ceilingCentre"/> is the opening's centre ON the ceiling plane,
    /// in the root's space; <paramref name="rotation"/> turns the kit about it (identity: the long side along the root's Z).
    /// The tier is PhoneTier at the call. Nothing is lit here: the caller drives the lens's _EmissionColor (property block)
    /// exactly as it drove its old lens. Returns null and builds nothing when the kit is missing.
    /// </summary>
    public static MeshRenderer Build(Transform fixtureRoot, Vector3 ceilingCentre, Quaternion rotation, string lensName)
    {
        if (fixtureRoot == null || !Load()) return null;
        var phone = PhoneTier;
        if (!phone && !LoadDesktop()) return null;
        var hung = ceilingCentre + rotation * new Vector3(0f, -hang, 0f);

        var lensGo = new GameObject(string.IsNullOrEmpty(lensName) ? LensName : lensName);
        var lt = lensGo.transform;
        lt.SetParent(fixtureRoot, false);
        lt.localPosition = hung;
        lt.localRotation = rotation;
        lensGo.AddComponent<MeshFilter>().sharedMesh = phone ? phoneMesh : lensMesh;
        var lens = lensGo.AddComponent<MeshRenderer>();
        lens.sharedMaterial = lensMaterial;
        lens.shadowCastingMode = ShadowCastingMode.Off;
        lens.receiveShadows = false;
        if (phone) return lens;

        var housing = Object.Instantiate(housingSource, fixtureRoot, false);
        housing.name = HousingName;
        housing.transform.localPosition = hung;
        housing.transform.localRotation = rotation;
        housing.transform.localScale = Vector3.one;

        var soil = Object.Instantiate(soilSource, fixtureRoot, false);
        soil.name = SoilName;
        soil.transform.localPosition = ceilingCentre;
        soil.transform.localRotation = rotation;
        soil.transform.localScale = Vector3.one;
        return lens;
    }

    /// <summary>True for a renderer that carries the kit's lens or phone mesh (a lens this kit built).</summary>
    public static bool IsKitLens(Renderer r)
    {
        if (r == null || !Load()) return false;
        var mf = r.GetComponent<MeshFilter>();
        if (mf == null) return false;
        var m = mf.sharedMesh;
        return m != null && (m == lensMesh || m == phoneMesh);
    }

    /// <summary>
    /// The map's lens drive for a zone lens material (Fixture.emission): its _EmissionColor scaled by LensPeak over the shared
    /// Troffer_Lens's peak, so the shared lens's colour reads exactly LensPeak and every zone keeps its own tint and its
    /// authored ratio to it (Level 0: x Level0LensEmissionScale; the Office's cool lens: 2.21 / 2.2, 1.81).
    /// </summary>
    public static Color LensDrive(Material zoneLens)
    {
        var shared = FrontRoomsSurfaces.TrofferLens;
        var reference = shared != null && shared.HasProperty(EmissionColorId) ? shared.GetColor(EmissionColorId).maxColorComponent : SharedLensPeak;
        if (reference <= 1e-4f) reference = SharedLensPeak;
        Color c;
        if (zoneLens != null && zoneLens.HasProperty(EmissionColorId)) c = zoneLens.GetColor(EmissionColorId);
        else if (shared != null && shared.HasProperty(EmissionColorId)) c = shared.GetColor(EmissionColorId);
        else c = new Color(2.2f, 2.1f, 1.8f, 1f);
        var k = LensPeak / reference;
        return new Color(c.r * k, c.g * k, c.b * k, 1f);
    }
}
