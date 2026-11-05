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
        live.Clear();
        pending.Clear();
        liveCount = 0;
        queryStamp = 0;
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
        var plane = fixtureRoot.TransformPoint(ceilingCentre).y;
        // the Relay's body reads the lamps it presses under (40_ceiling_lamps.md); the phone mesh carries the frame
        if (phone) { RegisterLamp(lens, plane, LampShape.Troffer, null, fixtureRoot); return lens; }

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
        RegisterLamp(lens, plane, LampShape.Troffer, housing.GetComponentsInChildren<Renderer>(true), fixtureRoot);
        return lens;
    }

    // ------------------------------------------------------------------ the live lamps (research/hunter/production_b/40_ceiling_lamps.md)
    /// <summary>What a lamp is to a body pressed under the ceiling (FrontRoomsRelayBody): recessed (L03: its frame 24 mm under
    /// the plane, the lens up inside it), a surface wrap (its lens is its underside) or a pendant (its body 0.60 m down).</summary>
    public enum LampShape : byte { Troffer, Wrap, Pendant }

    /// <summary>
    /// A lamp a kit built, as the Relay's body reads it (read only; the lamp logic never sees this): its lens and the whole
    /// fixture as world XZ rectangles, the world height of the ceiling plane it hangs from and of its lowest point, its shape,
    /// and its Light (the map's "Light" beside the lens, or the one its builder handed over, SetLampLight: the Run! rig's strips; null
    /// where the builder has none, as in the stream rooms).
    /// </summary>
    public struct LiveLamp
    {
        public Rect lens, outer;
        public float ceiling, bottom;
        public LampShape shape;
        public Light light;
    }

    sealed class LiveEntry
    {
        public Renderer lens;
        public Transform root;
        public Renderer[] parts;
        public Vector3 at;
        public float plane;
        public LiveLamp lamp;
        public bool lightSought, retired;
        /// <summary>The builder handed the Light over (SetLampLight): it is not sought by name, and a moved lamp keeps it.</summary>
        public bool lightGiven;
        public int stamp;
    }

    /// <summary>The live lamps' hash cell (the map's 3 m cell).</summary>
    public const float LiveCell = 3f;
    static readonly System.Collections.Generic.Dictionary<long, System.Collections.Generic.List<LiveEntry>> live =
        new System.Collections.Generic.Dictionary<long, System.Collections.Generic.List<LiveEntry>>();
    static int liveCount, queryStamp;
    static readonly System.Collections.Generic.List<LiveEntry> pending = new System.Collections.Generic.List<LiveEntry>(2);

    /// <summary>Lamps registered and not yet seen gone (tools, tests).</summary>
    public static int LiveCount => liveCount;

    static long CellKey(int x, int z) => ((long)x << 32) ^ (uint)z;

    /// <summary>
    /// Registers a lamp a kit built (every tier): its lens renderer, the ceiling plane it hangs from (world y), its shape and the
    /// renderers that hang (the lens counts; never a glow, a stain or a light). Kits call it from Build; a lamp leaves when the
    /// hash meets it destroyed (in a query, or where a lamp is added); SubsystemRegistration clears it all.
    /// </summary>
    public static void RegisterLamp(Renderer lens, float plane, LampShape shape, Renderer[] parts, Transform fixtureRoot)
    {
        if (lens == null) return;
        var e = new LiveEntry { lens = lens, root = fixtureRoot, parts = parts, plane = plane - lens.transform.position.y };
        e.lamp.shape = shape;
        Measure(e);
        Insert(e);
        liveCount++;
    }

    /// <summary>
    /// The Light of a lamp whose builder does not name it "Light" beside the lens (the game visual line's Run! rig, card 3, 2026-11-04: its strip
    /// Light keeps the name the sound line reads, "run b wrap light k"), handed over after the kit registered the lens: the body's contact
    /// reads it as it reads the map's Light (white armed, red under the alarm's A and B, nothing while it is dark). Every other lamp is
    /// found by name as before. True when the lamp was registered.
    /// </summary>
    public static bool SetLampLight(Renderer lens, Light light)
    {
        if (lens == null) return false;
        var found = false;
        foreach (var list in live.Values)
            for (var i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e.lens != lens || e.retired) continue;
                e.lamp.light = light;
                e.lightSought = true;
                e.lightGiven = true;
                found = true;
            }
        return found;
    }

    static void Insert(LiveEntry e)
    {
        int x0 = Mathf.FloorToInt(e.lamp.outer.xMin / LiveCell), x1 = Mathf.FloorToInt(e.lamp.outer.xMax / LiveCell);
        int z0 = Mathf.FloorToInt(e.lamp.outer.yMin / LiveCell), z1 = Mathf.FloorToInt(e.lamp.outer.yMax / LiveCell);
        for (var x = x0; x <= x1; x++)
        for (var z = z0; z <= z1; z++)
        {
            var key = CellKey(x, z);
            if (!live.TryGetValue(key, out var list)) { list = new System.Collections.Generic.List<LiveEntry>(2); live[key] = list; }
            Purge(list);
            list.Add(e);
        }
    }

    static void Purge(System.Collections.Generic.List<LiveEntry> list)
    {
        for (var i = list.Count - 1; i >= 0; i--)
        {
            var e = list[i];
            if (e.retired) { list.RemoveAt(i); continue; }
            if (e.lens != null) continue;
            if (e.stamp != -1) { e.stamp = -1; liveCount--; }
            list.RemoveAt(i);
        }
    }

    // The lens rectangle is the lens mesh's footprint, the outer one and the bottom every hanging part's, all in world space (any
    // turn); the plane is kept as an offset from the lens, so a moved lamp keeps it.
    static void Measure(LiveEntry e)
    {
        var t = e.lens.transform;
        e.at = t.position;
        e.lamp.ceiling = t.position.y + e.plane;
        float bottom;
        e.lamp.lens = Footprint(e.lens, out bottom);
        var outer = e.lamp.lens;
        var low = bottom;
        if (e.parts != null)
            for (var i = 0; i < e.parts.Length; i++)
            {
                var p = e.parts[i];
                if (p == null || p == e.lens) continue;
                float b;
                var r = Footprint(p, out b);
                if (r.width <= 0f) continue;
                outer = Rect.MinMaxRect(Mathf.Min(outer.xMin, r.xMin), Mathf.Min(outer.yMin, r.yMin), Mathf.Max(outer.xMax, r.xMax), Mathf.Max(outer.yMax, r.yMax));
                low = Mathf.Min(low, b);
            }
        e.lamp.outer = outer;
        e.lamp.bottom = low;
    }

    static Rect Footprint(Renderer r, out float bottom)
    {
        bottom = float.MaxValue;
        var mf = r.GetComponent<MeshFilter>();
        var m = mf != null ? mf.sharedMesh : null;
        if (m == null) { bottom = r.transform.position.y; return Rect.MinMaxRect(r.transform.position.x, r.transform.position.z, r.transform.position.x, r.transform.position.z); }
        var b = m.bounds;
        var t = r.transform;
        float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
        for (var i = 0; i < 8; i++)
        {
            var p = t.TransformPoint(new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z));
            x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x);
            z0 = Mathf.Min(z0, p.z); z1 = Mathf.Max(z1, p.z);
            bottom = Mathf.Min(bottom, p.y);
        }
        return Rect.MinMaxRect(x0, z0, x1, z1);
    }

    /// <summary>
    /// The live lamps whose fixture comes within <paramref name="radius"/> of <paramref name="world"/> (XZ), written into
    /// <paramref name="into"/> (no allocation); returns how many. A lamp its builder switched off (inactive) is skipped, a
    /// destroyed one leaves the hash, a moved one is measured again and re-hashed where it is.
    /// </summary>
    public static int LampsNear(Vector3 world, float radius, LiveLamp[] into)
    {
        if (into == null || into.Length == 0 || liveCount <= 0) return 0;
        queryStamp = queryStamp == int.MaxValue ? 1 : queryStamp + 1;
        var n = 0;
        int x0 = Mathf.FloorToInt((world.x - radius) / LiveCell), x1 = Mathf.FloorToInt((world.x + radius) / LiveCell);
        int z0 = Mathf.FloorToInt((world.z - radius) / LiveCell), z1 = Mathf.FloorToInt((world.z + radius) / LiveCell);
        for (var x = x0; x <= x1; x++)
        for (var z = z0; z <= z1; z++)
        {
            if (!live.TryGetValue(CellKey(x, z), out var list)) continue;
            Purge(list);
            for (var i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e.stamp == queryStamp || e.retired) continue;
                e.stamp = queryStamp;
                if (!e.lens.gameObject.activeInHierarchy) continue;
                if ((e.lens.transform.position - e.at).sqrMagnitude > 1e-4f)
                {
                    // moved (a chunk rebuilt or shifted): a fresh entry where it is now, this one retired
                    var moved = new LiveEntry { lens = e.lens, root = e.root, parts = e.parts, plane = e.plane, stamp = queryStamp };
                    moved.lamp.shape = e.lamp.shape;
                    if (e.lightGiven)
                    {
                        moved.lamp.light = e.lamp.light;
                        moved.lightSought = moved.lightGiven = true;
                    }
                    e.retired = true;
                    Measure(moved);
                    pending.Add(moved);   // hashed after the walk (the lists are being read)
                    e = moved;
                }
                var dx = Mathf.Max(e.lamp.outer.xMin - world.x, 0f, world.x - e.lamp.outer.xMax);
                var dz = Mathf.Max(e.lamp.outer.yMin - world.z, 0f, world.z - e.lamp.outer.yMax);
                if (dx * dx + dz * dz > radius * radius) continue;
                if (!e.lightSought)
                {
                    e.lightSought = true;
                    var lt = e.root != null ? e.root.Find("Light") : null;
                    e.lamp.light = lt != null ? lt.GetComponent<Light>() : null;
                }
                into[n++] = e.lamp;
                if (n >= into.Length) { FlushPending(); return n; }
            }
        }
        FlushPending();
        return n;
    }

    static void FlushPending()
    {
        for (var i = 0; i < pending.Count; i++) Insert(pending[i]);
        pending.Clear();
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
