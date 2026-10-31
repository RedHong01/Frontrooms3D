using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// A26 station-look-v1: the station set's own light (30L §3.1 as amended by r7 §10.1; T20 V1/V3; 61 §7; plan 70 §8).
/// A spot at the screen, 0.02 m in front of the bezel along the tilted screen normal, facing the room (along D's axis):
/// - cone inner 100°, outer 160°, URP range 9 m; a cookie with the cosine falloff and an up-clip that fades it from 10°
///   to 30° above the horizontal (no ceiling wash);
/// - intensity I = 6 x sqrt(Y / 0.140) clamped 4.86-8.4, colour = the frame's mean linear colour with its luminance kept
///   (I_eff = I x 0.71 / Y(colour)), blended 60 % toward the set's white (0.86, 0.91, 1.00); both from a track baked
///   offline from the ad (Resources/Rendering/StationTvLightTrack.json, 10 Hz x 160 samples, Tools/lookdev/
///   bake_tv_light_track.py), indexed by the broadcast's clock (FrontRoomsScreenSound.LoopTime, else the shared ad
///   bank's VideoPlayer time): no GPU read-back on any platform;
/// - smoothed with tau 0.12 s (0.5 s with Reduce flashing), x map.StartLampsNorth (0 -> 1 with the start door's swing,
///   so nothing leaks before D opens: B10);
/// - at the key take (map.StationKeyTaken) it falls to 0 by 0.60 s and stays off for the floor (50 §6);
/// - on while the set is powered and within one chunk (24 m) of the camera; never part of W1 (the lamps dip, the TV
///   doesn't); desktop soft shadows, none on WebGL (V3); light layer Station only (bit 1), set by bit operations.
/// </summary>
[AddComponentMenu("")]
public sealed class FrontRoomsStationTvLight : MonoBehaviour
{
    public const string TrackResource = "Rendering/StationTvLightTrack";
    public const float K = 6f, YRef = .140f, IMin = 4.86f, IMax = 8.4f, LumKeep = .71f, WhiteBlend = .6f;
    public const float Inner = 100f, Outer = 160f, Range = 9f, GateDistance = 24f;
    public const float Tau = .12f, TauReduced = .5f, TakeOff = .60f;
    public const float UpClipFrom = 10f, UpClipTo = 30f;
    public const float ShadowDepthBias = 1.5f, ShadowNormalBias = 1.0f;
    public const uint StationBit = 1u << 1, DefaultBit = 1u;
    static readonly Vector3 SetWhite = new Vector3(.86f, .91f, 1f);

    [System.Serializable]
    class Track
    {
        public float hz = 10f;
        public float loopSeconds = 16f;
        public int count;
        public float[] rgby;
        public string source;
        public string sha1;
    }

    static Track track;
    static Texture2D cookie;
    static bool trackLoaded;

    FrontRoomsMapWorld map;
    FrontRoomsScreenVideo screen;
    Light lamp;
    VideoPlayer player;
    Vector3 colour = Vector3.one;
    float intensity;
    bool taken, off;
    float takenFor;

    public Light Light => lamp;
#if UNITY_EDITOR
    /// <summary>Editor tools only: a loop time (0-16 s) that replaces the clock, so a proof can put the light on each card (batch mode does not play the ad).</summary>
    public static float ToolsTime = -1f;
#endif
    public bool Taken => taken;
    /// <summary>The intensity the light shows now (after StartLampsNorth and the take).</summary>
    public float Shown => lamp != null && lamp.enabled ? lamp.intensity : 0f;

    /// <summary>Build the light at the set's screen (TV local: the bezel face plus 0.02 m) and start driving it.</summary>
    public static FrontRoomsStationTvLight Create(Transform tv, Vector3 screenLocal, FrontRoomsMapWorld map, FrontRoomsScreenVideo screen)
    {
        var go = new GameObject("TV light (station)");
        go.transform.SetParent(tv, false);
        go.transform.localPosition = screenLocal + Vector3.forward * .02f;
        go.transform.localRotation = Quaternion.identity;
        var l = go.AddComponent<FrontRoomsStationTvLight>();
        l.Bind(map, screen);
        return l;
    }

    void Bind(FrontRoomsMapWorld world, FrontRoomsScreenVideo set)
    {
        map = world;
        screen = set;
        lamp = gameObject.AddComponent<Light>();
        lamp.type = LightType.Spot;
        lamp.spotAngle = Outer;
        lamp.innerSpotAngle = Inner;
        lamp.range = Range;
        lamp.intensity = 0f;
#if (UNITY_WEBGL && !UNITY_EDITOR) || FRONTROOMS_WEBGL_PREVIEW
        lamp.shadows = LightShadows.None;
#else
        lamp.shadows = LightShadows.Soft;
        // Shadow size: URP's per-light tier (UniversalAdditionalLightData), which defaults to High. Light.shadowResolution is
        // Built-In only and logged an error every station start (Unity 报错修复, 10-31).
        // A 160 deg spot spreads its 1024 px shadow slice thin, and the riser face takes the light at a grazing angle: the
        // pipeline's bias (0.6 / 0.6 texels) left blotches (acne) on the riser face beside the key plate (captures v3, v4).
        // Its own bias, in the pipeline's units (shadow-map texels, x 2.5 for soft shadows in URP): depth ShadowDepthBias,
        // normal ShadowNormalBias. The plate stands 40 mm proud, so the larger offset costs no shadow anyone reads.
        var urp = lamp.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
        if (urp == null) urp = gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
        urp.usePipelineSettings = false;
        lamp.shadowBias = ShadowDepthBias;
        lamp.shadowNormalBias = ShadowNormalBias;
        lamp.shadowStrength = .85f;
#endif
        lamp.shadowNearPlane = .1f;
        lamp.renderingLayerMask = (int)(((uint)lamp.renderingLayerMask | StationBit) & ~DefaultBit);
        lamp.cookie = Cookie(transform);
        LoadTrack();
        if (map != null) map.StationKeyTaken += OnKeyTaken;
    }

    void OnDestroy()
    {
        if (map != null) map.StationKeyTaken -= OnKeyTaken;
    }

    void OnKeyTaken(Vector3 at) => taken = true;

    /// <summary>Tools: start the take's fall without the map's event (the proof harness).</summary>
    public void TakeForTools() => taken = true;

    static void LoadTrack()
    {
        if (trackLoaded) return;
        trackLoaded = true;
        var asset = Resources.Load<TextAsset>(TrackResource);
        if (asset == null) { Debug.LogWarning("[StationTvLight] " + TrackResource + " is missing: the set's light holds the set's white"); return; }
        track = JsonUtility.FromJson<Track>(asset.text);
        if (track == null || track.rgby == null || track.rgby.Length < 4) track = null;
    }

    // The cookie: a cosine falloff across the cone and the up-clip (10° -> 30° above the horizontal), for this light's tilt.
    static Texture2D Cookie(Transform at)
    {
        if (cookie != null) return cookie;
        const int n = 128;
        cookie = new Texture2D(n, n, TextureFormat.RGBA32, false, true) { name = "Station TV light cookie", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color32[n * n];
        var t = Mathf.Tan(Outer * .5f * Mathf.Deg2Rad);
        var rot = at.rotation;
        for (var j = 0; j < n; j++)
        for (var i = 0; i < n; i++)
        {
            var x = (i + .5f) / n * 2f - 1f;
            var y = (j + .5f) / n * 2f - 1f;
            var d = new Vector3(x * t, y * t, 1f).normalized;
            var w = rot * d;
            var elev = Mathf.Asin(Mathf.Clamp(w.y, -1f, 1f)) * Mathf.Rad2Deg;
            var clip = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(UpClipFrom, UpClipTo, elev));
            var v = Mathf.Clamp01(d.z) * clip;
            var b = (byte)Mathf.RoundToInt(v * 255f);
            px[i + j * n] = new Color32(b, b, b, b);
        }
        cookie.SetPixels32(px);
        cookie.Apply(false, true);
        return cookie;
    }

    static float Luma(Vector3 c) => .2126f * c.x + .7152f * c.y + .0722f * c.z;

    // The target colour (linear, max channel 1) and intensity for the ad's current frame (r7).
    void Target(out Vector3 col, out float inten)
    {
        var c = SetWhite;
        var y = YRef;
        if (track != null)
        {
            // The broadcast is the clock (V5, agreed with 声音设计): the WTCZ loop's FMOD timeline keeps running while the
            // picture is out of view (the bank pauses then), so the light follows what the player hears; without FMOD (a
            // clone) the shared ad player's time.
            var time = FrontRooms.Audio.FrontRoomsScreenSound.LoopTime;
#if UNITY_EDITOR
            if (ToolsTime >= 0f) time = ToolsTime;
#endif
            if (time < 0f)
            {
                if (player == null)
                {
                    var bank = FrontRoomsScreenAdBank.Acquire(FrontRoomsScreenVideo.DefaultStreamFile, null);
                    if (bank != null) player = bank.GetComponent<VideoPlayer>();
                }
                time = player != null ? (float)player.time : Time.time;
            }
            var count = track.rgby.Length / 4;
            var k = Mathf.FloorToInt(Mathf.Repeat(time, track.loopSeconds) * track.hz) % count;
            c = new Vector3(track.rgby[k * 4], track.rgby[k * 4 + 1], track.rgby[k * 4 + 2]);
            y = track.rgby[k * 4 + 3];
        }
        var i = Mathf.Clamp(K * Mathf.Sqrt(Mathf.Max(y, 0f) / YRef), IMin, IMax);
        var cn = c / Mathf.Max(Luma(c), 1e-4f);
        var wn = SetWhite / Luma(SetWhite);
        var blend = Vector3.Lerp(cn, wn, WhiteBlend);
        var max = Mathf.Max(blend.x, Mathf.Max(blend.y, blend.z));
        col = blend / Mathf.Max(max, 1e-4f);
        inten = i * LumKeep / Mathf.Max(Luma(col), 1e-3f);
    }

    void Update()
    {
        if (lamp == null || off) return;
        var dt = Time.deltaTime;
        var tau = FrontRoomsSettings.ReduceFlashing ? TauReduced : Tau;
        var a = 1f - Mathf.Exp(-dt / tau);
        var powered = screen != null && screen.IsPoweredOn;
        Target(out var col, out var inten);
        var lamps = map != null ? map.StartLampsNorth : 1f;
        var goal = taken || !powered ? 0f : inten * lamps;
        if (taken)
        {
            takenFor += dt;
            // tau 0.12 leaves 0.7 % at 0.60 s; then off for the floor.
            a = 1f - Mathf.Exp(-dt / Tau);
        }
        intensity += (goal - intensity) * a;
        colour += (col - colour) * a;
        if (taken && takenFor >= TakeOff)
        {
            intensity = 0f;
            off = true;
            lamp.enabled = false;
            return;
        }
        var cam = Camera.main;
        var near = cam == null || (cam.transform.position - transform.position).sqrMagnitude <= GateDistance * GateDistance;
        lamp.enabled = near && intensity > 1e-3f;
        lamp.intensity = intensity;
        lamp.color = new Color(colour.x, colour.y, colour.z).gamma;
    }
}
