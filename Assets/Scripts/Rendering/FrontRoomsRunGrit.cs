using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The Run! T2 re-stack's mortar grit (71_run_room.md s.5.3 and s.5.8; research/door_break/54_dust_grit.md 4.1, the door break's
/// grain look): as each course of the receding end lands, mortar grit sifts from its bed and head joints, lit by the goal lamp,
/// gone within 3 s. Desktop only (iPad and phones none, WebGL never builds a re-stack).
///
/// No per-frame C# particles (71 s.5.8: the door break's CPU dust cost 4.7 ms in the Editor): a landing writes its grains ONCE
/// (start point, start time, start velocity, colour and size class: 32 bytes each) into a ring buffer, and FrontRooms/RunGrit
/// moves every grain analytically in its vertex stage (gravity with linear drag to each size's settling speed, resting on the
/// floor), streaks it along its screen motion and lights it per vertex from the Forward+ lights (the carried goal lamp among
/// them). One procedural draw per camera (Graphics.RenderPrimitives in beginCameraRendering, as the door break's grit, so tools
/// rendering several cameras a frame see it) while any grain is alive; nothing at all otherwise.
/// </summary>
[DisallowMultipleComponent]
public sealed class FrontRoomsRunGrit : MonoBehaviour
{
    public const int Capacity = 4096;
    /// <summary>A grain's life (s): 71 s.5.8 "gone within 3 s".</summary>
    public const float Life = 2.6f;
    /// <summary>Grains per metre of joint (bed and head joints, the face toward the player; a third as many on the back).</summary>
    public const float PerMetre = 70f;

    struct Grain
    {
        public Vector3 p0;
        public float t0;
        public Vector3 v0;
        public uint packed;     // r | g << 8 | b << 16 | size class << 24
    }

    static readonly int IdGrit = Shader.PropertyToID("_FR_RunGrit");
    static readonly int IdParams = Shader.PropertyToID("_FR_GritParams");
    static readonly int IdScale = Shader.PropertyToID("_FR_GritScale");
    static readonly int IdClock = Shader.PropertyToID("_FR_RunGritClock");

    // sRGB: mortar (lighter, warmer), the block's raw concrete, and a few paint flecks of the two tones (Run_BlockCore_A,
    // RunB_BlockUpper, RunB_BlockLower measured as built)
    static readonly Color32[] Palette =
    {
        new Color32(178, 172, 158, 0), new Color32(166, 160, 147, 0), new Color32(152, 148, 139, 0), new Color32(140, 137, 129, 0),
        new Color32(126, 123, 116, 0), new Color32(190, 186, 176, 0), new Color32(204, 200, 186, 0), new Color32(118, 126, 114, 0),
    };

    GraphicsBuffer buffer;
    Material material;
    MaterialPropertyBlock props;
    readonly List<Grain> staging = new List<Grain>(512);
    int ring, live;
    float clock, lastEmit = -99f, floorY;
    uint serial;

    /// <summary>Grains written so far (tests, the cost row).</summary>
    public int Emitted { get; private set; }
    /// <summary>Grains that would be drawn now (0 when the last landing is over 2.6 s old).</summary>
    public int Alive => clock - lastEmit <= Life + .05f ? live : 0;

    public static FrontRoomsRunGrit Create(Transform parent)
    {
        var shader = Shader.Find("FrontRooms/RunGrit");
        if (shader == null || !shader.isSupported || !SystemInfo.supportsComputeShaders) return null;
        var go = new GameObject("run b restack grit");
        go.transform.SetParent(parent, false);
        var g = go.AddComponent<FrontRoomsRunGrit>();
        g.material = new Material(shader) { name = "Run restack grit", hideFlags = HideFlags.DontSave };
        var grain = Resources.Load<Texture2D>("Surfaces/Textures/Door_Grain_D");
        if (grain != null) g.material.SetTexture("_GrainTex", grain);
        g.buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, Capacity, 32) { name = "Run restack grit" };
        g.props = new MaterialPropertyBlock();
        return g;
    }

    /// <summary>The recede's clock (s since the trip), once a frame: the shader's time base. Not a simulation step.</summary>
    public void SetClock(float seconds) => clock = seconds;

    /// <summary>
    /// One course has landed: grains along its bed joint (<paramref name="bedY"/>, across <paramref name="spans"/> in the end's
    /// frame) and its head joints (each span's ends, up the course's <paramref name="height"/>), most on the face toward the
    /// player. They leave with the end's velocity and a small scatter, at the recede's clock. One buffer write.
    /// </summary>
    public void EmitCourse(Transform end, List<Vector2> spans, float bedY, float height, float now, Vector3 velocity)
    {
        if (buffer == null || end == null || spans == null) return;
        staging.Clear();
        floorY = end.position.y;
        foreach (var s in spans)
        {
            var len = Mathf.Max(0f, s.y - s.x);
            var n = Mathf.Max(2, Mathf.RoundToInt(len * PerMetre));
            for (var i = 0; i < n; i++)
            {
                var u = (i + Hash01(serial + (uint)i * 3u)) / n;
                Add(end, new Vector3(Mathf.Lerp(s.x, s.y, u), bedY, i % 3 == 2 ? .082f : -.082f), now, velocity);
            }
            // the head joint at each end of the span: a short vertical line
            var m = Mathf.Max(1, Mathf.RoundToInt(height * PerMetre * .6f));
            for (var e = 0; e < 2; e++)
                for (var i = 0; i < m; i++)
                    Add(end, new Vector3(e == 0 ? s.x : s.y, bedY + height * (i + .5f) / m, -.082f), now, velocity);
        }
        if (staging.Count == 0) return;
        var count = Mathf.Min(staging.Count, Capacity);
        var first = ring;
        var a = Mathf.Min(count, Capacity - first);
        buffer.SetData(staging, 0, first, a);
        if (count > a) buffer.SetData(staging, a, 0, count - a);
        ring = (first + count) % Capacity;
        live = Mathf.Min(Capacity, live + count);
        lastEmit = now;
        Emitted += count;
    }

    void Add(Transform end, Vector3 local, float now, Vector3 velocity)
    {
        var id = serial++;
        var jitter = new Vector3(Hash01(id * 7u + 1u) - .5f, Hash01(id * 7u + 2u) * .3f, -Hash01(id * 7u + 3u) * .5f) * .35f;
        var c = Palette[(int)(Hash01(id * 7u + 4u) * Palette.Length) % Palette.Length];
        // size class: 45 % fine (0.1-0.3 mm), the rest log-uniform to 2 mm (the shader maps class 0..255 to 0.1..2 mm)
        var r = Hash01(id * 7u + 5u);
        var size = r < .45f ? (uint)(r / .45f * 92f) : (uint)(92f + (r - .45f) / .55f * 163f);
        staging.Add(new Grain
        {
            p0 = end.TransformPoint(local + new Vector3(0f, -.004f, 0f)),
            t0 = now + Hash01(id * 7u + 6u) * .12f,
            v0 = velocity * .5f + end.TransformDirection(jitter),
            packed = c.r | ((uint)c.g << 8) | ((uint)c.b << 16) | (size << 24),
        });
    }

    static float Hash01(uint x)
    {
        x ^= x >> 16; x *= 0x7feb352du; x ^= x >> 15; x *= 0x846ca68bu; x ^= x >> 16;
        return (x & 0xffffffu) / 16777216f;
    }

    void OnEnable() { RenderPipelineManager.beginCameraRendering += OnCamera; }
    void OnDisable() { RenderPipelineManager.beginCameraRendering -= OnCamera; }

    void OnDestroy()
    {
        buffer?.Release();
        buffer = null;
        if (material != null) { if (Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
    }

    void OnCamera(ScriptableRenderContext context, Camera cam)
    {
        if (cam == null || material == null || buffer == null || Alive == 0) return;
        if (cam.cameraType != CameraType.Game && cam.cameraType != CameraType.SceneView) return;
        var msaa = 1;
        if (cam.targetTexture != null) msaa = cam.targetTexture.antiAliasing;
        else if (cam.allowMSAA && GraphicsSettings.currentRenderPipeline is UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset urp) msaa = urp.msaaSampleCount;
        props.SetBuffer(IdGrit, buffer);
        props.SetVector(IdParams, new Vector4(1f / 120f, 24f, msaa <= 1 ? 1f : 0f, floorY));
        props.SetVector(IdScale, new Vector4(1f, 1.5f, clock, live));
        props.SetFloat(IdClock, clock);
        // the grains stay where they fell while the end moves on: the bounds reach back along the corridor
        var at = transform.position - transform.forward * 3f;
        var rp = new RenderParams(material)
        {
            camera = cam, worldBounds = new Bounds(at + new Vector3(0f, 1.3f, 0f), new Vector3(14f, 3.2f, 14f)), matProps = props,
            shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false, layer = gameObject.layer,
        };
        Graphics.RenderPrimitives(rp, MeshTopology.Triangles, live * 6, 1);
    }
}
