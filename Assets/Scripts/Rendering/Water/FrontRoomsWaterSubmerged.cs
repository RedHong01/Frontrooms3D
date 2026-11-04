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
    /// <summary>Where the viewer is (FrontRoomsWaterPlanar sets it each frame; tools before they render): with more bodies
    /// than the shader holds (MaxBodies), the nearest are published.</summary>
    public static Vector3 ViewerPosition;

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
    static readonly int LutId = Shader.PropertyToID("_FR_SubLut");
    static readonly int FineId = Shader.PropertyToID("_FR_CausticFine");
    static readonly int LongId = Shader.PropertyToID("_FR_CausticLong");
    static readonly int FineDId = Shader.PropertyToID("_FR_CausticFineD");
    static readonly int LongDId = Shader.PropertyToID("_FR_CausticLongD");
    static readonly int TexId = Shader.PropertyToID("_FR_CausticTex");

    public static int BodyCount => bodies.Count;
    public static int LampCount => lamps.Count;
    public static string Problem { get; private set; }
    public static Mode LastMode { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        bodies.Clear();
        lamps.Clear();
        loaded = false;
        lastFrame = -1;
        ForceMode = -1;
        Enabled = true;
        Problem = null;
        Shader.SetGlobalFloat(CountId, 0f);
        Shader.SetGlobalFloat(LampCountId, 0f);
    }

    // ------------------------------------------------------------------------------------------------ registry
    /// <summary>A body of water: its rectangle (world xz), the room above it (xz, up to roomTop), its height, the
    /// absorption scale (Q1: the club 1, the level 3-5, a room or two 9; below 0 = the zone's) and the basin's albedo.</summary>
    public static void Register(int id, Rect water, Rect room, float waterY, float roomTop, float absorbScale, float basinAlbedo, float thrownGain)
    {
        Unregister(id, false);
        bodies.Add(new Body { id = id, water = water, room = room, y = waterY, top = roomTop, scale = absorbScale, albedo = basinAlbedo, thrown = thrownGain });
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
        // the wet niches: each under one body's water, its live share read now (flicker, dim, dead)
        var m = 0;
        foreach (var l in lamps)
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

    static int BodyUnder(Vector3 p, int n)
    {
        for (var i = 0; i < n; i++)
        {
            var b = bodies[i];
            if (p.y >= b.y) continue;
            var r = b.water;
            if (p.x >= r.xMin - .4f && p.x <= r.xMax + .4f && p.z >= r.yMin - .4f && p.z <= r.yMax + .4f) return i;
        }
        return -1;
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
        var s = "bodies " + bodies.Count + ", niches " + lamps.Count + ", mode " + LastMode + ", caustics "
            + (fine != null ? fine.name + " " + fine.width + "^2 x " + fine.depth + " " + fine.format : "none") + " / "
            + (longArr != null ? longArr.name + " " + longArr.width + "^2 x " + longArr.depth + " " + longArr.format : "none");
        foreach (var b in bodies)
            s += " | body " + b.id + " y " + b.y.ToString("0.000") + " absorb x" + ScaleOf(b).ToString("0.0") + " thrown x" + ThrownOf(b).ToString("0.00")
                + (b.srcA != null ? " srcA " + b.srcA.name : "") + (b.srcB != null ? " srcB " + b.srcB.name : "");
        if (Problem != null) s += " | PROBLEM " + Problem;
        return s;
    }
}
