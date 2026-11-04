// The pool family's water: the constant ripple (research/bathroom_rooms/70_look_build.md, "Ripples"; 17_water.md 3,
// 7 item 3). Game visual line.
using System;
using UnityEngine;

/// <summary>
/// Drives the still pool's constant ripple for FrontRoomsWaterRipples.hlsl: loads the tier's baked loops
/// (Resources/Rendering/Water, written by Tools/lookdev/pool_water/gen_pool_ripples.py, imported as Texture2DArrays by
/// FrontRoomsRippleArray), keeps the ripple clock, and sets the shader globals; ticks FrontRoomsWaterRings too.
///
/// Tiers: desktop = FR_Ripple_Fine (512^2 x 64 slices, loop 0.50 s); the phone tier (FrontRoomsTrofferKit.PhoneTier:
/// handheld and WebGL) = FR_Ripple_FinePhone (256^2 x 32, loop 0.32 s); every tier = FR_Ripple_Long (128^2 x 128, loop
/// 2.56 s). FR_Ripple_Stats.json carries each loop's encoding, variances and footprint table.
///
/// The caller (the water component, the structures scene, tools) calls Tick once a frame while water is visible.
/// Reduce Motion (the game's flag, FrontRoomsHandheld.ReduceMotion: the OS switch or CAMERA MOTION off) freezes the
/// ripple: the clock stops on the current frame, rings stop and fade. Large-area modulation stays under 3 Hz: the
/// loop's patch-mean slope variance is constant to 0.6 % over its frames, the frame-blend loss is returned as
/// roughness (constant total), and the calm-patch mask changes over 24 s.
/// Nothing runs until something calls Ensure or Tick; no material or global changes before that.
/// </summary>
public static class FrontRoomsWaterRipples
{
    public const string Folder = "Rendering/Water/";
    public const string FineName = "FR_Ripple_Fine", FinePhoneName = "FR_Ripple_FinePhone", LongName = "FR_Ripple_Long";
    public const string StatsName = "FR_Ripple_Stats";
    /// <summary>Layer B: scale 8/7, turned by the 3-4-5 angle, played at this rate (A + B never repeats exactly).</summary>
    public const double RateB = 0.8613;
    public const int LutN = 19;

    /// <summary>1 = the reference calm, 2.0 degrees rms slope (17: calm water 1-3 degrees, so 0.5 .. 1.5).</summary>
    public static float Gain = 1f;
    /// <summary>Depth of the slow calm-patch mask (17 3.3 item 4: slicks halve the mean square slope); desktop only.</summary>
    public static float Patchiness = .25f;
    /// <summary>Tools: -1 follows the game's flag, 0 off, 1 on.</summary>
    public static int EditorReduceMotion = -1;
    /// <summary>Tools: use the phone tier's loop on desktop.</summary>
    public static bool ForcePhoneTier;

    public static bool ReduceMotion => EditorReduceMotion >= 0 ? EditorReduceMotion > 0 : FrontRoomsHandheld.ReduceMotion;
    public static bool PhoneTier => ForcePhoneTier || FrontRoomsTrofferKit.PhoneTier;
    public static bool Loaded { get; private set; }
    public static double Clock => clock;
    public static string Problem { get; private set; }

    [Serializable] sealed class StatsDoc { public float sigma_ref_deg; public Cascade[] cascades; }

    [Serializable]
    sealed class Cascade
    {
        public string name;
        public int size, slices, cols;
        public float patch_m, loop_s, slope_enc_rad, var_axis_rad2, var_sub_axis_rad2, interp_loss;
        public float[] lut_major_flat, lut_minor_flat;
    }

    static Texture2DArray fine, longArr;
    static Texture2D lut;
    static Cascade cFine, cLong;
    static bool loadedPhone;
    static double clock;
    static int lastFrame = -1;

    static readonly int FineId = Shader.PropertyToID("_FR_RippleFine");
    static readonly int LongId = Shader.PropertyToID("_FR_RippleLong");
    static readonly int LutId = Shader.PropertyToID("_FR_RippleLut");
    static readonly int AId = Shader.PropertyToID("_FR_RippleA");
    static readonly int BId = Shader.PropertyToID("_FR_RippleB");
    static readonly int LId = Shader.PropertyToID("_FR_RippleL");
    static readonly int EncId = Shader.PropertyToID("_FR_RippleEnc");
    static readonly int VarId = Shader.PropertyToID("_FR_RippleVar");
    static readonly int LossId = Shader.PropertyToID("_FR_RippleLoss");
    static readonly int GeoId = Shader.PropertyToID("_FR_RippleGeo");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Loaded = false;
        fine = longArr = null;
        lut = null;
        cFine = cLong = null;
        clock = 0.0;
        lastFrame = -1;
        Gain = 1f;
        Patchiness = .25f;
        EditorReduceMotion = -1;
        ForcePhoneTier = false;
        Problem = null;
    }

    /// <summary>Loads the tier's loops and tables once (again if the tier changed). False, with Problem set, when an
    /// asset is missing; the shader then shows flat water.</summary>
    public static bool Ensure()
    {
        var phone = PhoneTier;
        if (Loaded && phone == loadedPhone) return true;
        Loaded = false;
        Problem = null;
        var stats = Resources.Load<TextAsset>(Folder + StatsName);
        if (stats == null) return Fail("missing Resources/" + Folder + StatsName);
        StatsDoc doc;
        try { doc = JsonUtility.FromJson<StatsDoc>(stats.text); }
        catch (Exception e) { return Fail("cannot read " + StatsName + ": " + e.Message); }
        var fineName = phone ? FinePhoneName : FineName;
        cFine = Find(doc, phone ? "FinePhone" : "Fine");
        cLong = Find(doc, "Long");
        if (cFine == null || cLong == null) return Fail(StatsName + " lacks a cascade");
        fine = Resources.Load<Texture2DArray>(Folder + fineName);
        longArr = Resources.Load<Texture2DArray>(Folder + LongName);
        if (fine == null || longArr == null) return Fail("missing Texture2DArray " + (fine == null ? fineName : LongName));
        if (fine.depth != cFine.slices || longArr.depth != cLong.slices)
            return Fail("slice count differs from " + StatsName + " (" + fine.depth + "/" + cFine.slices + ", " + longArr.depth + "/" + cLong.slices + ")");
        BuildLut();
        Shader.SetGlobalTexture(FineId, fine);
        Shader.SetGlobalTexture(LongId, longArr);
        Shader.SetGlobalTexture(LutId, lut);
        loadedPhone = phone;
        Loaded = true;
        FrontRoomsWaterRings.EnsureLut();
        Upload();
        return true;
    }

    static bool Fail(string why)
    {
        Problem = why;
        Shader.SetGlobalVector(AId, Vector4.zero);
        Debug.LogWarning("[FrontRoomsWaterRipples] " + why + "; the water stays flat.");
        return false;
    }

    static Cascade Find(StatsDoc d, string name)
    {
        if (d == null || d.cascades == null) return null;
        foreach (var c in d.cascades) if (c != null && c.name == name) return c;
        return null;
    }

    static void BuildLut()
    {
        if (lut != null)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(lut);
            else UnityEngine.Object.DestroyImmediate(lut);
        }
        lut = new Texture2D(LutN, LutN, TextureFormat.RGBAHalf, false, true)
        {
            name = "FR_RippleLut", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave
        };
        var data = new ushort[LutN * LutN * 4];
        for (var b = 0; b < LutN; b++)
        {
            for (var a = 0; a < LutN; a++)
            {
                var i = (b * LutN + a) * 4;
                var j = a * LutN + b;
                data[i] = Mathf.FloatToHalf(At(cFine.lut_major_flat, j));
                data[i + 1] = Mathf.FloatToHalf(At(cFine.lut_minor_flat, j));
                data[i + 2] = Mathf.FloatToHalf(At(cLong.lut_major_flat, j));
                data[i + 3] = Mathf.FloatToHalf(At(cLong.lut_minor_flat, j));
            }
        }
        lut.SetPixelData(data, 0);
        lut.Apply(false, true);
    }

    static float At(float[] a, int i) { return a != null && i < a.Length ? a[i] : 1f; }

    /// <summary>Once a frame while water is visible: advance by dt (frozen under Reduce Motion), tick the rings,
    /// set the globals. viewer: the camera, for choosing which rings get the slots. A second call in the same frame
    /// does nothing.</summary>
    public static void Tick(float dt, Vector3 viewer)
    {
        if (Application.isPlaying)
        {
            if (Time.frameCount == lastFrame) return;
            lastFrame = Time.frameCount;
        }
        if (!Ensure()) return;
        var rm = ReduceMotion;
        if (!rm) clock += Mathf.Max(dt, 0f);
        FrontRoomsWaterRings.Tick((float)(clock % 86400.0), Mathf.Max(dt, 0f), viewer, rm);
        Upload();
    }

    /// <summary>Tools: set the ripple clock directly (seconds) and upload; rings follow it.</summary>
    public static void SetTime(double seconds, Vector3 viewer, float realDt = 0f)
    {
        if (!Ensure()) return;
        var rm = ReduceMotion;
        if (!rm) clock = Math.Max(0.0, seconds);
        FrontRoomsWaterRings.Tick((float)(clock % 86400.0), realDt, viewer, rm);
        Upload();
    }

    static Vector4 Frames(double phase, int slices)
    {
        var f = (phase - Math.Floor(phase)) * slices;
        var s0 = (int)Math.Floor(f);
        if (s0 >= slices) s0 = slices - 1;
        var s1 = (s0 + 1) % slices;
        return new Vector4(s0, s1, (float)(f - s0), 1f);
    }

    static void Upload()
    {
        if (!Loaded) return;
        var a = Frames(clock / cFine.loop_s, cFine.slices);
        var b = Frames(clock * RateB / cFine.loop_s + .5, cFine.slices);
        b.w = loadedPhone ? 0f : 1f;                     // the phone tier samples layer A only
        var l = Frames(clock / cLong.loop_s, cLong.slices);
        l.w = 0f;
        Shader.SetGlobalVector(AId, a);
        Shader.SetGlobalVector(BId, b);
        Shader.SetGlobalVector(LId, l);
        Shader.SetGlobalVector(EncId, new Vector4(cFine.slope_enc_rad, cLong.slope_enc_rad, Mathf.Max(Gain, 0f), loadedPhone ? 0f : Mathf.Clamp01(Patchiness)));
        Shader.SetGlobalVector(VarId, new Vector4(cFine.var_axis_rad2, cLong.var_axis_rad2, cFine.var_sub_axis_rad2, cLong.var_sub_axis_rad2));
        Shader.SetGlobalVector(LossId, new Vector4(cFine.interp_loss, cLong.interp_loss, (float)(clock % (24.0 * 256.0)), 0f));
        Shader.SetGlobalVector(GeoId, new Vector4(cFine.size / cFine.patch_m, cLong.size / cLong.patch_m, 1f / cFine.patch_m, 1f / cLong.patch_m));
    }

    /// <summary>Tools and logs: what is loaded.</summary>
    public static string Describe()
    {
        if (!Loaded) return "not loaded" + (Problem != null ? ": " + Problem : "");
        return (loadedPhone ? "phone tier" : "desktop") + ": fine " + fine.name + " " + fine.width + "^2 x " + fine.depth + " " + fine.format
            + " (loop " + cFine.loop_s + " s), long " + longArr.name + " " + longArr.width + "^2 x " + longArr.depth + " " + longArr.format
            + " (loop " + cLong.loop_s + " s); clock " + clock.ToString("0.000") + " s; reduce motion " + ReduceMotion;
    }
}
