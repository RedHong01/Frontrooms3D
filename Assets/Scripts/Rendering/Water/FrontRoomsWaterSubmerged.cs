// The pool family's water: what lies under it and the light it throws (research/bathroom_rooms/70_look_build.md,
// "Submerged"; 17_water.md 4.1-4.4, 7 items 2, 8 and 9). Game visual line.
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Feeds FrontRoomsWaterSubmerged.hlsl (included by FrontRooms/Surface): the bodies of water in the scene (their
/// rectangle, height, room and absorption scale), the wet-niche lamps that throw light out of them, the compact lamps
/// whose light carries the ripple's caustics onto the floor (at most two per body), the trapping LUT and the tier's
/// caustic loops (Resources/Rendering/Water/FR_Caustic_*, written by Tools/lookdev/pool_water/gen_pool_caustics.py from
/// the same surface as the ripple loops).
///
/// Every FrontRooms/Surface material (tile, ladders, props, the Relay's cloth) takes the submerged look by WHERE it is:
/// a point under a registered body's water gets the water's colour, a point above it in its room the niches' thrown
/// light. No material changes; nothing runs with no body registered (the shader's count is 0: Level 0 and every other
/// zone render exactly as before).
///
/// Tiers (17 6.1): desktop = the fine loop's two layers and the long loop (FR_Caustic_Fine, FR_Caustic_Long); phones =
/// the phone loop (FR_Caustic_FinePhone) and the long loop: the flipbook; WebGL = two scrolled layers of one tile (the
/// phone loop's first slice). The caustics follow the ripple's own clock (FrontRoomsWaterRipples), so Reduce Motion
/// freezes them with the ripple; the WebGL scroll uses the same clock.
///
/// FrontRoomsWaterSurface registers its body; the lamp kit (or a tool) registers wet niches with RegisterNiche and the
/// compact lamps with the water's causticSourceA/B. Publish runs once a frame from FrontRoomsWaterPlanar (game) or from
/// a tool before it renders.
/// </summary>
public static class FrontRoomsWaterSubmerged
{
    public const int MaxBodies = 12, MaxLamps = 16;
    public const float N = 1.333f;
    public const int LutN = 64;
    public const string Folder = "Rendering/Water/";
    public const string FineName = "FR_Caustic_Fine", FinePhoneName = "FR_Caustic_FinePhone", LongName = "FR_Caustic_Long";
    public const string StatsName = "FR_Caustic_Stats";

    /// <summary>17 4.1: the effective sRGB absorption of clear water per metre (Pope and Fry; metal halide to daylight).</summary>
    public static readonly Vector3 Absorption = new Vector3(.29f, .048f, .006f);
    /// <summary>A glaze (n 1.52) against water instead of air: F0 0.0043 against 0.0426, about a tenth.</summary>
    public static float GlazeUnderWater = .10f;
    /// <summary>1 = the projected caustics as baked (energy-true); 0 = flat.</summary>
    public static float CausticStrength = 1f;
    /// <summary>Thrown light of the wet niches: the share of a niche's light that comes back up out of the basin as a
    /// glow about the vertical (17 4.3: three bounces in white tile, 1 - p = 0.525 leaves each time).</summary>
    public static float NicheGlow = .25f;
    /// <summary>The glow's radius (m): the niche's lens and the lit tile around it; it blurs the thrown caustics.</summary>
    public static float GlowRadius = .15f;
    /// <summary>Tools: -1 follows the platform and tier; 0 off, 1 desktop, 2 phone, 3 WebGL.</summary>
    public static int ForceMode = -1;
    /// <summary>Tools: false publishes no body (the term off, as outside a pool zone), whatever is registered.</summary>
    public static bool Enabled = true;
    /// <summary>The waterline on top of each material's wet band (FRWetBand), for shallow water that would otherwise read
    /// as a clean wet floor: x raises the band (m, water wicked up what can soak), y adds darkening, z the meniscus gain
    /// (1 = twice as bright), w the tide mark at the band's dry edge (0..1). Zero (the default) changes nothing.</summary>
    public static Vector4 Waterline;
    /// <summary>Where the viewer is (FrontRoomsWaterPlanar sets it each frame; tools before they render): with more bodies
    /// than the shader holds (MaxBodies), the nearest are published.</summary>
    public static Vector3 ViewerPosition;
    /// <summary>Soft room faces (FrontRoomsWaterSubmerged.hlsl, _FR_SubBoxSoft; structures G2-a, scene 4's SP6 spring line): the
    /// width (m) over which the thrown niche light, the mirror and the bounce ramp out across each face of a body's room box
    /// instead of stopping at it. 0 (the default: the game, the map's pools, the title rooms) is the hard test bit for bit; scene
    /// 4 sets 0.6 in NEW (FrontRoomsStructuresWater). Never tied to SetWaterLight: the title rooms call that too.</summary>
    public static float BoxSoft;

    public enum Mode { Off = 0, Desktop = 1, Phone = 2, WebGL = 3 }

    public static Mode CurrentMode
    {
        get
        {
            if (ForceMode >= 0) return (Mode)Mathf.Clamp(ForceMode, 0, 3);
#if UNITY_WEBGL && !UNITY_EDITOR
            return Mode.WebGL;
#else
            return FrontRoomsWaterRipples.PhoneTier ? Mode.Phone : Mode.Desktop;
#endif
        }
    }

    sealed class Body
    {
        public int id;
        public Rect water, room;
        public float y, top, scale, albedo, thrown;
        public Light srcA, srcB;
        public float radius;
        public float depth = -1f;                     // the water's mean depth (m) for its bounce (SetDepth; below 0: 1.2)
        public Vector3 rho;                           // its return albedo, cached for rhoKey
        public Vector3 rhoKey = new Vector3(-1f, -1f, -1f);
    }

    sealed class Lamp
    {
        public Light light;
        public float baseIntensity;
    }

    [Serializable] sealed class StatsDoc { public float enc_max; public Loop[] loops; }
    [Serializable] sealed class Loop { public string name; public int size, slices; public float[] lens_D_m; }

    static readonly List<Body> bodies = new List<Body>();
    static readonly List<Lamp> lamps = new List<Lamp>();
    static Texture2D lut;
    static Texture2DArray fine, longArr;
    static Vector4 fineD, longD, texels;
    static bool loaded, loadedPhone;
    static int lastFrame = -1;

    static readonly Vector4[] rects = new Vector4[MaxBodies], rooms = new Vector4[MaxBodies], pars = new Vector4[MaxBodies];
    static readonly Vector4[] src0 = new Vector4[MaxBodies], src1 = new Vector4[MaxBodies];
    static readonly Vector4[] lampPos = new Vector4[MaxLamps], lampCol = new Vector4[MaxLamps];

    static readonly int CountId = Shader.PropertyToID("_FR_SubCount");
    static readonly int BoxSoftId = Shader.PropertyToID("_FR_SubBoxSoft");
    static readonly int RectId = Shader.PropertyToID("_FR_SubRect");
    static readonly int RoomId = Shader.PropertyToID("_FR_SubRoom");
    static readonly int ParamId = Shader.PropertyToID("_FR_SubParam");
    static readonly int Src0Id = Shader.PropertyToID("_FR_SubSrc0");
    static readonly int Src1Id = Shader.PropertyToID("_FR_SubSrc1");
    static readonly int AbsorbId = Shader.PropertyToID("_FR_SubAbsorb");
    static readonly int ModeId = Shader.PropertyToID("_FR_SubMode");
    static readonly int LampPosId = Shader.PropertyToID("_FR_SubLampPos");
    static readonly int LampColId = Shader.PropertyToID("_FR_SubLampCol");
    static readonly int LampCountId = Shader.PropertyToID("_FR_SubLampCount");
    static readonly int ThrownId = Shader.PropertyToID("_FR_SubThrown");
    static readonly int WaterlineId = Shader.PropertyToID("_FR_SubWaterline");
    static readonly int LutId = Shader.PropertyToID("_FR_SubLut");
    static readonly int FineId = Shader.PropertyToID("_FR_CausticFine");
    static readonly int LongId = Shader.PropertyToID("_FR_CausticLong");
    static readonly int FineDId = Shader.PropertyToID("_FR_CausticFineD");
    static readonly int LongDId = Shader.PropertyToID("_FR_CausticLongD");
    static readonly int TexId = Shader.PropertyToID("_FR_CausticTex");

    public static int BodyCount => bodies.Count;

    /// <summary>The i-th registered body's rectangle (world xz) and water height (the zone look's height fog over terraced
    /// water, tools); false past the end.</summary>
    public static bool GetBody(int i, out Rect water, out float y)
    {
        water = default(Rect);
        y = 0f;
        if (i < 0 || i >= bodies.Count || bodies[i] == null) return false;
        water = bodies[i].water;
        y = bodies[i].y;
        return true;
    }
    public static int LampCount => lamps.Count;
    public static string Problem { get; private set; }
    public static Mode LastMode { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        bodies.Clear();
        lamps.Clear();
        nearest.Clear();
        BoxSoft = 0f;
        Shader.SetGlobalFloat(BoxSoftId, 0f);
        loaded = false;
        lastFrame = -1;
        ForceMode = -1;
        Enabled = true;
        Waterline = Vector4.zero;
        Problem = null;
        Shader.SetGlobalFloat(CountId, 0f);
        Shader.SetGlobalVector(WaterlineId, Vector4.zero);
        Shader.SetGlobalFloat(LampCountId, 0f);
    }

    // ------------------------------------------------------------------------------------------------ registry
    /// <summary>A body of water: its rectangle (world xz), the room above it (xz, up to roomTop), its height, the
    /// absorption scale (Q1: the club 1, the level 3-5, a room or two 9; below 0 = the zone's) and the basin's albedo.</summary>
    public static void Register(int id, Rect water, Rect room, float waterY, float roomTop, float absorbScale, float basinAlbedo, float thrownGain)
    {
        // a body registered again keeps its place in the list (FrontRoomsWaterSurface.RefreshSubmerged), as a new one
        var at = -1;
        for (var i = 0; i < bodies.Count; i++) if (bodies[i] != null && bodies[i].id == id) { at = i; break; }
        Unregister(id, false);
        var body = new Body { id = id, water = water, room = room, y = waterY, top = roomTop, scale = absorbScale, albedo = basinAlbedo, thrown = thrownGain };
        if (at >= 0 && at <= bodies.Count) bodies.Insert(at, body);
        else bodies.Add(body);
        Publish(true);
    }

    /// <summary>The compact lamps over a body whose light carries the caustics onto its floor (17 4.4: at most two per room).</summary>
    public static void SetSources(int id, Light a, Light b, float radius)
    {
        foreach (var x in bodies)
            if (x.id == id) { x.srcA = a; x.srcB = b; x.radius = Mathf.Max(radius, .001f); }
    }

    public static void Unregister(int id) { Unregister(id, true); }

    static void Unregister(int id, bool publish)
    {
        for (var i = bodies.Count - 1; i >= 0; i--) if (bodies[i].id == id) bodies.RemoveAt(i);
        if (publish) Publish(true);
    }

    /// <summary>A wet niche (a light under a body's water): its light comes back up out of the basin as a glow that
    /// lights the room above, with the ripple's caustics. It dims and dies with the lamp (its intensity is read every
    /// frame against baseIntensity).</summary>
    public static void RegisterNiche(Light light, float baseIntensity)
    {
        if (light == null) return;
        UnregisterNiche(light);
        lamps.Add(new Lamp { light = light, baseIntensity = Mathf.Max(baseIntensity, 1e-4f) });
    }

    public static void UnregisterNiche(Light light)
    {
        for (var i = lamps.Count - 1; i >= 0; i--) if (lamps[i].light == light || lamps[i].light == null) lamps.RemoveAt(i);
    }

    public static void ClearNiches() { lamps.Clear(); }

    // ------------------------------------------------------------------------------------------------ assets
    static bool EnsureAssets()
    {
        var phone = CurrentMode != Mode.Desktop;
        if (loaded && phone == loadedPhone && lut != null) return fine != null && longArr != null;
        loaded = true;
        loadedPhone = phone;
        Problem = null;
        if (lut == null) BuildLut();
        var stats = Resources.Load<TextAsset>(Folder + StatsName);
        StatsDoc doc = null;
        if (stats != null)
        {
            try { doc = JsonUtility.FromJson<StatsDoc>(stats.text); }
            catch (Exception e) { Problem = "cannot read " + StatsName + ": " + e.Message; }
        }
        else Problem = "missing Resources/" + Folder + StatsName;
        var fineName = phone ? FinePhoneName : FineName;
        fine = Resources.Load<Texture2DArray>(Folder + fineName);
        longArr = Resources.Load<Texture2DArray>(Folder + LongName);
        if (fine == null || longArr == null) Problem = "missing Texture2DArray " + (fine == null ? fineName : LongName);
        var lf = FindLoop(doc, phone ? "FinePhone" : "Fine");
        var ll = FindLoop(doc, "Long");
        fineD = D4(lf, new Vector4(.01f, .03f, .1f, .32f));
        longD = D4(ll, new Vector4(.06f, .2f, .6f, 1.8f));
        texels = new Vector4(lf != null ? lf.size : (phone ? 128 : 256), ll != null ? ll.size : 128, 0f, 0f);
        if (Problem != null) Debug.LogWarning("[FrontRoomsWaterSubmerged] " + Problem + "; the caustics stay flat.");
        return fine != null && longArr != null;
    }

    static Loop FindLoop(StatsDoc d, string name)
    {
        if (d == null || d.loops == null) return null;
        foreach (var l in d.loops) if (l != null && l.name == name) return l;
        return null;
    }

    static Vector4 D4(Loop l, Vector4 fallback)
    {
        if (l == null || l.lens_D_m == null || l.lens_D_m.Length < 4) return fallback;
        return new Vector4(l.lens_D_m[0], l.lens_D_m[1], l.lens_D_m[2], l.lens_D_m[3]);
    }

    /// <summary>The LUT, u = tau / (tau + 1) over 64 texels: R the diffuse light from above that reaches depth tau
    /// (the mean over a uniform sky of (1 - F) exp(-tau / cos tw)), G the share of the upwelling diffuse light the
    /// surface sends back down, P(tau) = the mean over a Lambertian of R(tw) exp(-2 tau / cos tw) (R = 1 past the
    /// critical angle; 0.475 at tau 0, W26's p).</summary>
    static void BuildLut()
    {
        lut = new Texture2D(LutN, 1, TextureFormat.RGBAHalf, false, true)
        {
            name = "FR_SubLut", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave
        };
        var data = new ushort[LutN * 4];
        for (var k = 0; k < LutN; k++)
        {
            var u = k / (double)(LutN - 1);
            var tau = u >= .9999 ? 1e4 : u / (1.0 - u);
            var dIn = 0.0;
            var pTrap = 0.0;
            const int steps = 512;
            for (var j = 0; j < steps; j++)
            {
                var th = (j + .5) / steps * Math.PI * .5;
                var c = Math.Cos(th);
                var sn = Math.Sin(th);
                var w = 2.0 * c * sn * (Math.PI * .5 / steps);              // Lambertian: 2 cos sin d(theta), sums to 1
                // down: from the air at th, into the water at tw
                var cw = Math.Sqrt(Math.Max(0.0, 1.0 - sn * sn / (N * N)));
                dIn += w * (1.0 - Fresnel(c, N)) * Math.Exp(-tau / Math.Max(cw, 1e-4));
                // up: from the floor at th in the water, back down by the surface
                pTrap += w * Fresnel(c, 1.0 / N) * Math.Exp(-2.0 * tau / Math.Max(c, 1e-4));
            }
            data[k * 4] = Mathf.FloatToHalf((float)dIn);
            data[k * 4 + 1] = Mathf.FloatToHalf((float)pTrap);
            data[k * 4 + 2] = 0;
            data[k * 4 + 3] = Mathf.FloatToHalf(1f);
        }
        lut.SetPixelData(data, 0);
        lut.Apply(false, true);
    }

    /// <summary>Exact unpolarised Fresnel reflectance: ci the cosine in the medium the light comes from, nrel the index
    /// it goes into over the one it comes from (1 past the critical angle).</summary>
    public static double Fresnel(double ci, double nrel)
    {
        ci = Math.Min(Math.Max(ci, 0.0), 1.0);
        var g2 = nrel * nrel - 1.0 + ci * ci;
        if (g2 <= 0.0) return 1.0;
        var g = Math.Sqrt(g2);
        var a = (g - ci) / (g + ci);
        var b = (ci * (g + ci) - 1.0) / (ci * (g - ci) + 1.0);
        return Math.Min(1.0, .5 * a * a * (1.0 + b * b));
    }

    /// <summary>The LUT's two values at an optical depth (tools and logs: the doc's numbers).</summary>
    public static Vector2 LutAt(double tau)
    {
        var dIn = 0.0;
        var pTrap = 0.0;
        const int steps = 512;
        for (var j = 0; j < steps; j++)
        {
            var th = (j + .5) / steps * Math.PI * .5;
            var c = Math.Cos(th);
            var sn = Math.Sin(th);
            var w = 2.0 * c * sn * (Math.PI * .5 / steps);
            var cw = Math.Sqrt(Math.Max(0.0, 1.0 - sn * sn / (N * N)));
            dIn += w * (1.0 - Fresnel(c, N)) * Math.Exp(-tau / Math.Max(cw, 1e-4));
            pTrap += w * Fresnel(c, 1.0 / N) * Math.Exp(-2.0 * tau / Math.Max(c, 1e-4));
        }
        return new Vector2((float)dIn, (float)pTrap);
    }

    // ------------------------------------------------------------------------------------------------ per frame
    /// <summary>Sets the shader's globals for this frame (FrontRoomsWaterPlanar calls it in the game; tools call it
    /// before they render). A second call in the same frame (Play mode) does nothing unless forced.</summary>
    public static void Publish(bool force = false)
    {
        if (Application.isPlaying && !force)
        {
            if (Time.frameCount == lastFrame) return;
            lastFrame = Time.frameCount;
        }
        Shader.SetGlobalVector(WaterlineId, Waterline);
        Shader.SetGlobalFloat(BoxSoftId, Mathf.Max(BoxSoft, 0f));
        for (var i = bodies.Count - 1; i >= 0; i--) if (bodies[i] == null) bodies.RemoveAt(i);
        if (bodies.Count > MaxBodies) bodies.Sort(ByViewerDistance);
        var n = Enabled ? Mathf.Min(bodies.Count, MaxBodies) : 0;
        Shader.SetGlobalFloat(CountId, n);
        if (n == 0)
        {
            Shader.SetGlobalFloat(LampCountId, 0f);
            return;
        }
        var mode = CurrentMode;
        LastMode = mode;
        var haveLoops = EnsureAssets();
        for (var i = 0; i < MaxBodies; i++)
        {
            if (i >= n)
            {
                rects[i] = rooms[i] = pars[i] = src0[i] = src1[i] = Vector4.zero;
                continue;
            }
            var b = bodies[i];
            rects[i] = new Vector4(b.water.xMin, b.water.yMin, b.water.xMax, b.water.yMax);
            rooms[i] = new Vector4(b.room.xMin, b.room.yMin, b.room.xMax, b.room.yMax);
            pars[i] = new Vector4(b.y, ScaleOf(b), Mathf.Clamp(b.albedo, 0f, .95f), b.top);
            src0[i] = Source(b.srcA, b.radius);
            src1[i] = Source(b.srcB, b.radius);
        }
        // the wet niches: each under one body's water, its live share read now (flicker, dim, dead). With more than the shader
        // holds (scene 4's SP6 registers 24), the ones nearest the viewer go first, so the walker's own room keeps its glow and
        // caustics (structures G2-a); up to MaxLamps the registration order stands, as before (the map, the title rooms)
        var m = 0;
        var order = lamps;
        if (lamps.Count > MaxLamps)
        {
            nearest.Clear();
            nearest.AddRange(lamps);
            nearest.Sort(ByNearest);
            order = nearest;
        }
        foreach (var l in order)
        {
            if (m >= MaxLamps) break;
            if (l == null || l.light == null) continue;
            var p = l.light.transform.position;
            var bi = BodyUnder(p, n);
            if (bi < 0) continue;
            var live = l.light.isActiveAndEnabled ? Mathf.Clamp(l.light.intensity / l.baseIntensity, 0f, 4f) : 0f;
            // the glow has crossed the water on its way to the basin's tile and back up (about twice the lamp's depth and
            // a metre across): it leaves the water in the water's colour (the zone's absorption)
            var path = 2f * Mathf.Max(bodies[bi].y - p.y, .3f) + 1f;
            var a = Absorption * ScaleOf(bodies[bi]);
            var tr = new Color(Mathf.Exp(-a.x * path), Mathf.Exp(-a.y * path), Mathf.Exp(-a.z * path));
            var c = l.light.color.linear * tr * (l.baseIntensity * NicheGlow * ThrownOf(bodies[bi]));
            lampPos[m] = new Vector4(p.x, p.y, p.z, bi);
            lampCol[m] = new Vector4(c.r, c.g, c.b, live);
            m++;
        }
        for (var i = m; i < MaxLamps; i++) lampPos[i] = lampCol[i] = Vector4.zero;
        Shader.SetGlobalVectorArray(RectId, rects);
        Shader.SetGlobalVectorArray(RoomId, rooms);
        Shader.SetGlobalVectorArray(ParamId, pars);
        Shader.SetGlobalVectorArray(Src0Id, src0);
        Shader.SetGlobalVectorArray(Src1Id, src1);
        Shader.SetGlobalVectorArray(LampPosId, lampPos);
        Shader.SetGlobalVectorArray(LampColId, lampCol);
        Shader.SetGlobalFloat(LampCountId, m);
        Shader.SetGlobalVector(AbsorbId, new Vector4(Absorption.x, Absorption.y, Absorption.z, 1f / (N * N)));
        var scroll = (float)(FrontRoomsWaterRipples.Clock % 3600.0);                // frozen with the ripple (Reduce Motion)
        Shader.SetGlobalVector(ModeId, new Vector4(haveLoops ? (float)mode : 0f, GlazeUnderWater, Mathf.Max(CausticStrength, 0f), scroll));
        Shader.SetGlobalVector(ThrownId, new Vector4(1f, GlowRadius, 0f, 0f));
        Shader.SetGlobalTexture(LutId, lut);
        if (haveLoops)
        {
            Shader.SetGlobalTexture(FineId, fine);
            Shader.SetGlobalTexture(LongId, longArr);
        }
        Shader.SetGlobalVector(FineDId, fineD);
        Shader.SetGlobalVector(LongDId, longD);
        Shader.SetGlobalVector(TexId, texels);
        PublishWaterLight(n, mode);
    }

    // The niches nearest the viewer first (the walker's room), nulls last.
    static readonly List<Lamp> nearest = new List<Lamp>();
    static readonly Comparison<Lamp> ByNearest = (a, b) => NicheDistance(a).CompareTo(NicheDistance(b));

    static float NicheDistance(Lamp l)
    {
        if (l == null || l.light == null) return float.MaxValue;
        return (l.light.transform.position - ViewerPosition).sqrMagnitude;
    }

    static int ByViewerDistance(Body a, Body b)
    {
        return DistanceTo(a).CompareTo(DistanceTo(b));
    }

    static float DistanceTo(Body b)
    {
        var p = new Vector2(ViewerPosition.x, ViewerPosition.z);
        var dx = Mathf.Max(0f, Mathf.Max(b.room.xMin - p.x, p.x - b.room.xMax));
        var dz = Mathf.Max(0f, Mathf.Max(b.room.yMin - p.y, p.y - b.room.yMax));
        return dx * dx + dz * dz + (ViewerPosition.y - b.y) * (ViewerPosition.y - b.y) * .01f;
    }

    static Vector4 Source(Light l, float radius)
    {
        if (l == null || !l.isActiveAndEnabled || l.type == LightType.Directional) return Vector4.zero;
        var p = l.transform.position;
        return new Vector4(p.x, p.y, p.z, radius);
    }

    /// <summary>The body whose water a lamp is in: the lowest surface above it (equal heights keep the first, as
    /// FRSubFind does), so a niche of a catch pool beside a higher terrace stays the catch pool's.</summary>
    static int BodyUnder(Vector3 p, int n)
    {
        var best = -1;
        for (var i = 0; i < n; i++)
        {
            var b = bodies[i];
            if (p.y >= b.y) continue;
            var r = b.water;
            if (p.x >= r.xMin - .4f && p.x <= r.xMax + .4f && p.z >= r.yMin - .4f && p.z <= r.yMax + .4f && (best < 0 || b.y < bodies[best].y)) best = i;
        }
        return best;
    }

    static float ScaleOf(Body b)
    {
        if (b.scale > 0f) return b.scale;
        var p = ZoneProfileAt(b);
        return p != null ? Mathf.Max(p.waterAbsorbScale, 0f) : 1f;
    }

    static float ThrownOf(Body b)
    {
        if (b.thrown >= 0f) return b.thrown;
        var p = ZoneProfileAt(b);
        return p != null ? Mathf.Max(p.thrownLight, 0f) : 1f;
    }

    static FrontRoomsZoneLookProfile ZoneProfileAt(Body b)
    {
        float w;
        var zone = FrontRoomsZoneLook.ZoneAt(new Vector3(b.water.center.x, b.y + .5f, b.water.center.y), out w);
        return string.IsNullOrEmpty(zone) || w <= 0f ? null : FrontRoomsZoneLook.Profile(zone);
    }

    /// <summary>Tools and logs.</summary>
    public static string Describe()
    {
        var s = "bodies " + bodies.Count + ", niches " + lamps.Count + (lamps.Count > MaxLamps ? " (the " + MaxLamps + " nearest published)" : "")
            + (BoxSoft > 0f ? ", soft room faces " + BoxSoft.ToString("0.00") + " m" : "") + ", mode " + LastMode + ", caustics "
            + (fine != null ? fine.name + " " + fine.width + "^2 x " + fine.depth + " " + fine.format : "none") + " / "
            + (longArr != null ? longArr.name + " " + longArr.width + "^2 x " + longArr.depth + " " + longArr.format : "none");
        foreach (var b in bodies)
            s += " | body " + b.id + " y " + b.y.ToString("0.000") + " absorb x" + ScaleOf(b).ToString("0.0") + " thrown x" + ThrownOf(b).ToString("0.00")
                + (b.srcA != null ? " srcA " + b.srcA.name : "") + (b.srcB != null ? " srcB " + b.srcB.name : "");
        if (Problem != null) s += " | PROBLEM " + Problem;
        return s;
    }

    // ------------------------------------------------------------------------------------------------ the room lit by its water
    // 70_look_build.md section 12 (Red, 2026-11-04, on scene 4: the room takes neither light nor colour from its water).
    // FrontRoomsWaterSubmerged.hlsl FRSubWaterLight: the mirror (the overhead lamps the water reflects onto the ceiling and
    // walls, with the long ripple's net) and the bounce (the light that went into the water and came back out in the water's
    // colour, a Lambertian rectangle seen from each surface). Every static's default is today's frame: nothing runs.

    /// <summary>The overhead lamps the water mirrors onto its room (Fresnel R, the ripple's net): 0 off (today), 1 as physics
    /// gives it. Desktop only (ForceMirror): phones and WebGL keep the bounce.</summary>
    public static float MirrorGain;
    /// <summary>The light the water sends back into its room in its own colour (absorption by depth and the zone's scale, the
    /// basin's albedo, the trapping), from the lamps above it and the ambient: 0 off (today), 1 as physics gives it.</summary>
    public static float BounceGain;
    /// <summary>The share of the ambient's light from the water's directions that the bounce replaces: 1 the water's own light
    /// instead of the zone's grey there (energy kept), 0 the bounce added on top.</summary>
    public static float BounceReplace = 1f;
    /// <summary>The share of each wet niche's glow that comes up over the whole water (the basin's white tile carries the
    /// light across before it leaves) instead of at the niche (FRSubThrown): 0 today; the niche light's total is kept.</summary>
    public static float NicheSpread;
    /// <summary>The radius (m) of a lamp the water mirrors when it is not one of a body's compact caustic sources: the blur of
    /// its net on the ceiling (a troffer's lens about 0.3, a down-fill 0.2, a 50 mm low-voltage spot 0.025).</summary>
    public static float MirrorLampRadius = .25f;
    /// <summary>Tools: -1 the tier decides (the mirror on desktop only); 0 off, 1 on.</summary>
    public static int ForceMirror = -1;

    /// <summary>The looks Red picks from in the structures scene (each path alone, and the recommendation).</summary>
    public enum WaterLightLook { Today = 0, Mirror = 1, Bounce = 2, Spread = 3, Recommended = 4 }

    /// <summary>The recommended values (70 section 12): the mirror and the bounce as physics gives them, half of the niches'
    /// glow spread over the basin.</summary>
    public const float RecMirror = 1f, RecBounce = 1f, RecSpread = .5f;

    public static WaterLightLook CurrentWaterLight { get; private set; }

    /// <summary>Sets the statics for one look and publishes (callers re-render their realtime reflection probes after it,
    /// so the tile and the probe tiers reflect the lit room).</summary>
    public static void SetWaterLight(WaterLightLook look)
    {
        CurrentWaterLight = look;
        var rec = look == WaterLightLook.Recommended;
        MirrorGain = look == WaterLightLook.Mirror || rec ? RecMirror : 0f;
        BounceGain = look == WaterLightLook.Bounce || rec ? RecBounce : 0f;
        NicheSpread = look == WaterLightLook.Spread || rec ? RecSpread : 0f;
        BounceReplace = 1f;
        MirrorLampRadius = .25f;
        lightsScanned = -1f;
        Publish(true);
    }

    /// <summary>The bounce reads the water's mean depth (m); FrontRoomsWaterSurface sets it (its depth field, else by kind).</summary>
    public static void SetDepth(int id, float depth)
    {
        foreach (var x in bodies)
            if (x.id == id) x.depth = depth;
    }

    /// <summary>The share of the light falling on the water that comes back up out of it, per colour channel: down through
    /// the column (the LUT's D), the basin's albedo, back up and out (X: 1 - R inside the critical cone), the trapping
    /// (1 / (1 - albedo P)); a = (0.29, 0.048, 0.006) per metre x the scale. The dry tile's own 0.8 for comparison: clear water
    /// 0.12 m (0.53, 0.61, 0.63), 0.9 m (0.24, 0.51, 0.61); the level's x4 at 0.12 m (0.35, 0.56, 0.62).</summary>
    public static Vector3 ReturnAlbedo(float depth, float scale, float albedo)
    {
        var r = Vector3.zero;
        for (var ch = 0; ch < 3; ch++)
        {
            var tau = Absorption[ch] * Mathf.Max(scale, 0f) * Mathf.Max(depth, 0f);
            double dIn = 0.0, xOut = 0.0, pTrap = 0.0;
            const int steps = 256;
            for (var j = 0; j < steps; j++)
            {
                var th = (j + .5) / steps * Math.PI * .5;
                var c = Math.Cos(th);
                var sn = Math.Sin(th);
                var w = 2.0 * c * sn * (Math.PI * .5 / steps);
                var cw = Math.Sqrt(Math.Max(0.0, 1.0 - sn * sn / (N * N)));
                dIn += w * (1.0 - Fresnel(c, N)) * Math.Exp(-tau / Math.Max(cw, 1e-4));
                var up = Fresnel(c, 1.0 / N);
                xOut += w * (1.0 - up) * Math.Exp(-tau / Math.Max(c, 1e-4));
                pTrap += w * up * Math.Exp(-2.0 * tau / Math.Max(c, 1e-4));
            }
            r[ch] = (float)(dIn * albedo * xOut / Math.Max(1.0 - albedo * pTrap, 1e-3));
        }
        return r;
    }

    /// <summary>The water's reflectance for the room's diffuse light (1 - the LUT's D(0)): 0.066.</summary>
    public static readonly float DiffuseReflectance = 1f - LutAt(0.0).x;

    static readonly Vector4[] bounceA = new Vector4[MaxBodies], bounceB = new Vector4[MaxBodies];
    static readonly List<Light> roomLamps = new List<Light>();
    static float lightsScanned = -1f;
    static readonly int WLId = Shader.PropertyToID("_FR_SubWL");
    static readonly int WL2Id = Shader.PropertyToID("_FR_SubWL2");
    static readonly int BounceAId = Shader.PropertyToID("_FR_SubBounceA");
    static readonly int BounceBId = Shader.PropertyToID("_FR_SubBounceB");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetWaterLight()
    {
        MirrorGain = BounceGain = NicheSpread = 0f;
        BounceReplace = 1f;
        MirrorLampRadius = .25f;
        ForceMirror = -1;
        CurrentWaterLight = WaterLightLook.Today;
        roomLamps.Clear();
        lightsScanned = -1f;
        Shader.SetGlobalVector(WLId, Vector4.zero);
    }

    static void PublishWaterLight(int n, Mode mode)
    {
        var spread = Mathf.Clamp01(NicheSpread);
        var gain = Mathf.Max(BounceGain, 0f);
        var mirror = MirrorGain > 0f && (ForceMirror >= 0 ? ForceMirror > 0 : mode == Mode.Desktop);
        var bounce = gain > 0f || spread > 0f;
        if (!mirror && !bounce)
        {
            Shader.SetGlobalVector(WLId, Vector4.zero);
            return;
        }
        if (bounce)
        {
            if (gain > 0f) ScanLamps();
            for (var i = 0; i < MaxBodies; i++)
            {
                if (i >= n) { bounceA[i] = bounceB[i] = Vector4.zero; continue; }
                var b = bodies[i];
                var rho = ReturnAlbedoOf(b);
                var e = gain > 0f ? LampsOnWater(b) : Vector3.zero;
                bounceA[i] = new Vector4(rho.x * gain, rho.y * gain, rho.z * gain, DiffuseReflectance * gain);
                bounceB[i] = new Vector4(rho.x * e.x * gain, rho.y * e.y * gain, rho.z * e.z * gain, 0f);
            }
            if (spread > 0f)
            {
                // a Lambertian glow I0 leaves the surface with flux 0.525 pi I0 (1 - P(0)); spread over the water it is a
                // radiance of that flux over the water's area (URP units)
                for (var k = 0; k < MaxLamps; k++)
                {
                    var c = lampCol[k];
                    if (c.w <= 0f) continue;
                    var bi = (int)(lampPos[k].w + .5f);
                    if (bi < 0 || bi >= n) continue;
                    var area = Mathf.Max(bodies[bi].water.width * bodies[bi].water.height, .25f);
                    var s = spread * c.w * .525f * Mathf.PI / area;
                    bounceB[bi] += new Vector4(c.x * s, c.y * s, c.z * s, 0f);
                }
                Shader.SetGlobalVector(ThrownId, new Vector4(1f - spread, GlowRadius, 0f, 0f));
            }
            Shader.SetGlobalVectorArray(BounceAId, bounceA);
            Shader.SetGlobalVectorArray(BounceBId, bounceB);
        }
        Shader.SetGlobalVector(WLId, new Vector4(mirror ? MirrorGain : 0f, gain, Mathf.Clamp01(BounceReplace), 1f));
        Shader.SetGlobalVector(WL2Id, new Vector4(mirror ? 1f : 0f, Mathf.Max(MirrorLampRadius, .001f), bounce ? 1f : 0f, 0f));
    }

    static Vector3 ReturnAlbedoOf(Body b)
    {
        var key = new Vector3(b.depth > 0f ? b.depth : 1.2f, ScaleOf(b), Mathf.Clamp(b.albedo, 0f, .95f));
        if (key != b.rhoKey)
        {
            b.rhoKey = key;
            b.rho = ReturnAlbedo(key.x, key.y, key.z);
        }
        return b.rho;
    }

    /// <summary>The scene's point and spot lights, found again once a second in Play mode (every publish in edit mode).</summary>
    static void ScanLamps()
    {
        var now = Application.isPlaying ? Time.unscaledTime : -1f;
        if (Application.isPlaying && lightsScanned >= 0f && now - lightsScanned < 1f) return;
        lightsScanned = Application.isPlaying ? now : -1f;
        roomLamps.Clear();
        foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (l != null && (l.type == LightType.Point || l.type == LightType.Spot)) roomLamps.Add(l);
    }

    /// <summary>The mean irradiance (URP units) the lamps of the body's room above the water give the water, over a grid of
    /// points about 1.5 m apart, as URP lights a floor: colour x intensity, the range fade, the cone, the cosine.</summary>
    static Vector3 LampsOnWater(Body b)
    {
        var w = b.water;
        int nx = Mathf.Clamp(Mathf.CeilToInt(w.width / 1.5f), 2, 8), nz = Mathf.Clamp(Mathf.CeilToInt(w.height / 1.5f), 2, 8);
        var sum = Vector3.zero;
        foreach (var l in roomLamps)
        {
            if (l == null || !l.isActiveAndEnabled || l.intensity <= 0f) continue;
            var lp = l.transform.position;
            if (lp.y <= b.y + .05f || lp.y > b.top + .5f || lp.x < b.room.xMin - .5f || lp.x > b.room.xMax + .5f || lp.z < b.room.yMin - .5f || lp.z > b.room.yMax + .5f) continue;
            var col = l.color.linear * l.intensity;
            if (UnityEngine.Rendering.GraphicsSettings.lightsUseColorTemperature && l.useColorTemperature) col *= Mathf.CorrelatedColorTemperatureToRGB(l.colorTemperature);
            var range2 = l.range * l.range;
            float cosOuter = 1f, inv = 0f;
            var spot = l.type == LightType.Spot;
            if (spot)
            {
                cosOuter = Mathf.Cos(l.spotAngle * .5f * Mathf.Deg2Rad);
                var cosInner = Mathf.Cos(l.innerSpotAngle * .5f * Mathf.Deg2Rad);
                inv = 1f / Mathf.Max(cosInner - cosOuter, .001f);
            }
            var fwd = l.transform.forward;
            var e = 0f;
            for (var i = 0; i < nx; i++)
                for (var j = 0; j < nz; j++)
                {
                    var q = new Vector3(w.xMin + (i + .5f) * w.width / nx, b.y, w.yMin + (j + .5f) * w.height / nz);
                    var v = lp - q;
                    var d2 = Mathf.Max(v.sqrMagnitude, 1e-4f);
                    if (d2 >= range2) continue;
                    var f = d2 / range2;
                    var smooth = Mathf.Clamp01(1f - f * f);
                    var dir = v / Mathf.Sqrt(d2);
                    var a = smooth * smooth / d2 * dir.y;
                    if (spot)
                    {
                        var s = Mathf.Clamp01((Vector3.Dot(fwd, -dir) - cosOuter) * inv);
                        a *= s * s;
                    }
                    e += a;
                }
            e /= nx * nz;
            sum += new Vector3(col.r, col.g, col.b) * e;
        }
        return sum;
    }

    /// <summary>Tools and logs: the water-light statics and each body's bounce.</summary>
    public static string DescribeWaterLight()
    {
        var s = "water light " + CurrentWaterLight + ": mirror " + MirrorGain.ToString("0.00") + " bounce " + BounceGain.ToString("0.00")
            + " replace " + BounceReplace.ToString("0.00") + " spread " + NicheSpread.ToString("0.00") + " lamp radius " + MirrorLampRadius.ToString("0.000")
            + " | lamps scanned " + roomLamps.Count;
        for (var i = 0; i < Mathf.Min(bodies.Count, MaxBodies); i++)
        {
            var b = bodies[i];
            s += " | body " + b.id + " depth " + (b.depth > 0f ? b.depth : 1.2f).ToString("0.00") + " rho " + b.rho.ToString("F3")
                + " A " + bounceA[i].ToString("F4") + " B " + bounceB[i].ToString("F4");
        }
        return s;
    }
}
