using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The light under a shut Run door (research/level_space/71_run_room.md s.7.2 D1 and s.7.5 S1; game visual card 4, decision 8c:
/// "the corridor's light in the window and under the door", the door readable from at least 5 m; as measured in the clone this
/// line reads from about 3 m, on the saddle, and at 5 m the ALARM ZONE card and the square lite carry the read). Built by
/// FrontRoomsRunRigBinder for a Run door entrance only (RunRigLayout.entranceDoor): every room the map builds today has the arch,
/// so none is built.
///
/// What it is: the map's RN-F leaf (Kit_DoorLeaf_Ward_DA, faces at +-22 mm, its bottom at 15 mm) hangs 3 mm over the Steel DA
/// frame's fluted saddle (152 mm wide, a 104 mm flat at 12 mm, 1:2 bevels). Light from the lit corridor floor leaves that slot
/// almost level (a 3 mm slot under a 44 mm leaf passes about +-4 degrees), so it lands on the saddle's outer flat, brightest at
/// the leaf, catches the outer bevel, and dies out over a hand's width of the floor beyond. A 3 mm slot is far under a shadow
/// texel, so no Light can draw it; as the rig's soffit halos do (RunSoffitHalo), it is one additive strip draped 0.4 mm over the
/// saddle's outer half and the floor in front of it (FrontRooms/RunB/DoorGap, vertex alpha = the profile), on the approach side
/// only, the leaf's 0.99 m wide with soft ends at the jambs' brush seals.
///
/// Its level is read, never typed: each Tick sums the rig's own Lights at the corridor floor just inside the door (each light's
/// colour, its intensity after the rig's state and cull, its range and cone, attenuated as URP attenuates them; no shadows, no
/// cookies) times the floor's albedo. So armed it is the strips' cool white; at the trip it drops with the mains and comes back
/// as the threshold head's warm pool; in the reset it rises as the re-strike reaches the door; on phones it follows the kept
/// strips. It fades out as the leaf leaves its latch (the open door lets the real light through). Zero lights, one draw, 17 x 13
/// vertices, no texture, every tier (the tell is gameplay information, not decor). Tick allocates nothing.
/// </summary>
public sealed class FrontRoomsRunDoorGap
{
    public const string ObjectName = "Run B / Run door gap light";
    /// <summary>The corridor floor's diffuse albedo the inside light is multiplied by (RunB_VCT field, about half).</summary>
    public static float FloorAlbedo = .5f;
    /// <summary>The added radiance at the slot over the inside floor's (1: the line reads as the lit floor seen through the gap).</summary>
    public static float SlotGain = 1f;
    /// <summary>Where the inside floor is sampled (rig space): on the axis, this far past the entrance line.</summary>
    public const float SampleZ = .35f;

    // The section (rig space, outward = -z): the leaf's outer face, the saddle's flat, its bevel, the floor.
    const float LeafFace = .022f, SaddleFlat = .052f, SaddleFoot = .076f, SaddleTop = .012f, Lift = .0004f;
    const float LeafHalf = .495f, Spread = .535f;
    // Rows along the outward distance: z (positive = outward), height, the profile there.
    static readonly float[] RowZ = { .0225f, .030f, .038f, .046f, .0515f, .064f, .0765f, .090f, .110f, .140f, .180f, .230f, .300f };
    static readonly float[] RowY = { SaddleTop, SaddleTop, SaddleTop, SaddleTop, SaddleTop, .0063f, .0008f, 0f, 0f, 0f, 0f, 0f, 0f };
    static readonly float[] RowLevel = { 1f, .78f, .58f, .44f, .36f, .40f, .26f, .12f, .075f, .045f, .025f, .010f, 0f };
    // Columns across (x / LeafHalf on the saddle; the floor rows spread a little wider).
    static readonly float[] ColX = { -Spread, -LeafHalf, -.47f, -.40f, -.30f, -.20f, -.10f, -.04f, 0f, .04f, .10f, .20f, .30f, .40f, .47f, LeafHalf, Spread };

    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int DensityId = Shader.PropertyToID("_Density");
    static Mesh mesh;
    static Material material;

    readonly FrontRoomsRunRig rig;
    readonly Light[] lights;
    readonly float[] cosOuter, invRange;
    readonly MeshRenderer renderer;
    readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
    readonly Transform sample;
    Color lastColor = new Color(-1f, -1f, -1f, 1f);
    bool lastOn = true;

    /// <summary>The added radiance at the slot last Tick (linear, before the door's fade; tests and tools).</summary>
    public Color Slot { get; private set; }
    /// <summary>The door's fade last Tick (1 shut .. 0 open).</summary>
    public float Shut { get; private set; }
    /// <summary>The strip's renderer (one draw), or null when the shader is missing.</summary>
    public MeshRenderer Renderer => renderer;
    /// <summary>How many of the rig's Lights it reads.</summary>
    public int LightCount => lights.Length;

    FrontRoomsRunDoorGap(FrontRoomsRunRig rig, Transform parent)
    {
        this.rig = rig;
        var all = rig.GetComponentsInChildren<Light>(true);
        var n = 0;
        for (var i = 0; i < all.Length; i++) if (all[i] != rig.GoalWindowLight) n++;
        lights = new Light[n];
        cosOuter = new float[n];
        invRange = new float[n];
        n = 0;
        for (var i = 0; i < all.Length; i++)
        {
            var l = all[i];
            if (l == rig.GoalWindowLight) continue;
            lights[n] = l;
            var co = Mathf.Cos(Mathf.Deg2Rad * l.spotAngle * .5f);
            var ci = Mathf.Cos(Mathf.Deg2Rad * Mathf.Min(l.innerSpotAngle, l.spotAngle) * .5f);
            cosOuter[n] = co;
            invRange[n] = 1f / Mathf.Max(.001f, ci - co);
            n++;
        }
        var go = new GameObject(ObjectName);
        go.transform.SetParent(parent, false);
        sample = new GameObject("Run door gap sample").transform;
        sample.SetParent(go.transform, false);
        sample.localPosition = new Vector3(0f, 0f, SampleZ);
        if (Material == null) return;
        go.AddComponent<MeshFilter>().sharedMesh = Strip();
        renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        renderer.allowOcclusionWhenDynamic = false;
    }

    /// <summary>The light under the Run door at the entrance of <paramref name="rig"/>'s room, under <paramref name="parent"/> (the
    /// room's root: the rig's frame, the entrance wall's centre line at z 0). Null without a rig.</summary>
    public static FrontRoomsRunDoorGap Build(FrontRoomsRunRig rig, Transform parent)
    {
        if (rig == null || parent == null) return null;
        var gap = new FrontRoomsRunDoorGap(rig, parent);
        gap.Tick(1f);
        return gap;
    }

    static Material Material
    {
        get
        {
            if (material != null) return material;
            var shader = Resources.Load<Shader>("Rendering/RunDoorGap");
            if (shader == null) return null;
            material = new Material(shader) { name = "Run B / Run door gap light", hideFlags = HideFlags.DontSave };
            material.SetVector(ColorId, new Vector4(0f, 0f, 0f, 1f));
            material.SetFloat(DensityId, 1f);
            return material;
        }
    }

    /// <summary>
    /// Read the rig's Lights at the floor inside the door and write the strip's level; <paramref name="shut"/> is the door's fade
    /// (1 on its latch, 0 open or broken: FrontRoomsRunRigBinder.ShutWeight). Call after the rig's Tick. No allocation.
    /// </summary>
    public void Tick(float shut)
    {
        Shut = Mathf.Clamp01(shut);
        var p = sample.position;
        var up = sample.up;
        float r = 0f, g = 0f, b = 0f;
        for (var i = 0; i < lights.Length; i++)
        {
            var l = lights[i];
            if (l == null || !l.isActiveAndEnabled || l.intensity <= 0f) continue;
            var type = l.type;
            if (type != LightType.Spot && type != LightType.Point) continue;
            var d = l.transform.position - p;
            var d2 = Mathf.Max(d.sqrMagnitude, 6.1035e-5f);
            var dist = Mathf.Sqrt(d2);
            var ndl = (d.x * up.x + d.y * up.y + d.z * up.z) / dist;
            if (ndl <= 0f) continue;
            // URP 17 DistanceAttenuation: 1 / d^2 times the smooth range window (1 - (d^2 / r^2)^2)^2.
            var f = d2 / Mathf.Max(1e-4f, l.range * l.range);
            var smooth = Mathf.Clamp01(1f - f * f);
            var atten = smooth * smooth / d2;
            if (type == LightType.Spot)
            {
                var fwd = l.transform.forward;
                var sdl = -(d.x * fwd.x + d.y * fwd.y + d.z * fwd.z) / dist;
                var a = Mathf.Clamp01((sdl - cosOuter[i]) * invRange[i]);
                atten *= a * a;
            }
            var c = l.color.linear;
            var k = l.intensity * atten * ndl;
            r += c.r * k; g += c.g * k; b += c.b * k;
        }
        var gain = FloorAlbedo * SlotGain;
        Slot = new Color(r * gain, g * gain, b * gain, 1f);
        if (renderer == null) return;
        var on = Shut > .001f && (r + g + b) > 1e-5f;
        if (on != lastOn)
        {
            lastOn = on;
            renderer.enabled = on;
        }
        if (!on) return;
        var col = new Color(Slot.r * Shut, Slot.g * Shut, Slot.b * Shut, 1f);
        if (Mathf.Abs(col.r - lastColor.r) + Mathf.Abs(col.g - lastColor.g) + Mathf.Abs(col.b - lastColor.b) < 1e-5f) return;
        lastColor = col;
        block.Clear();
        // The level is linear radiance: SetVector passes it as it is (SetColor would treat it as sRGB and darken it about 5 times
        // in the project's linear colour space; the first build did).
        block.SetVector(ColorId, new Vector4(col.r, col.g, col.b, 1f));
        block.SetFloat(DensityId, 1f);
        renderer.SetPropertyBlock(block);
    }

    /// <summary>The strip (shared by every Run door): rows along the outward distance, columns across the leaf; vertex alpha is the
    /// profile (the slot's line at the leaf, the flat, the brighter bevel, the floor's short tail) times the soft ends at the jambs.</summary>
    static Mesh Strip()
    {
        if (mesh != null) return mesh;
        var rows = RowZ.Length;
        var cols = ColX.Length;
        var v = new Vector3[rows * cols];
        var n = new Vector3[rows * cols];
        var c = new Color32[rows * cols];
        for (var j = 0; j < rows; j++)
        {
            // The bevel's normal leans outward (1:2); the flat and the floor face up.
            var bevel = RowZ[j] > SaddleFlat + .001f && RowZ[j] < SaddleFoot + .001f;
            var normal = bevel ? new Vector3(0f, .894f, -.447f) : Vector3.up;
            var floor = RowZ[j] > SaddleFoot + .001f;
            for (var i = 0; i < cols; i++)
            {
                var x = ColX[i];
                var ax = Mathf.Abs(x);
                // The slot ends at the jambs' brush seals (+-0.495); light on the floor spreads a little past them.
                float side;
                if (floor) side = ax <= .46f ? 1f : ax <= LeafHalf ? Mathf.Lerp(1f, .35f, (ax - .46f) / (LeafHalf - .46f)) : Mathf.Lerp(.35f, 0f, (ax - LeafHalf) / (Spread - LeafHalf));
                else side = ax <= .47f ? 1f : ax <= LeafHalf ? Mathf.Lerp(1f, .15f, (ax - .47f) / (LeafHalf - .47f)) : 0f;
                var k = j * cols + i;
                v[k] = new Vector3(x, RowY[j], -RowZ[j]) + normal * Lift;
                n[k] = normal;
                c[k] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(RowLevel[j] * side)));
            }
        }
        var t = new int[(rows - 1) * (cols - 1) * 6];
        var q = 0;
        for (var j = 0; j < rows - 1; j++)
        for (var i = 0; i < cols - 1; i++)
        {
            var a = j * cols + i;
            var bq = a + 1;
            var cq = a + cols;
            var dq = cq + 1;
            // Facing up: clockwise seen from above (a, b on the row nearer the leaf; c, d one row out, at lower z).
            t[q++] = a; t[q++] = bq; t[q++] = cq;
            t[q++] = bq; t[q++] = dq; t[q++] = cq;
        }
        mesh = new Mesh { name = "Run B Run door gap light" };
        mesh.vertices = v;
        mesh.normals = n;
        mesh.colors32 = c;
        mesh.triangles = t;
        mesh.RecalculateBounds();
        mesh.hideFlags = HideFlags.DontSave;
        return mesh;
    }
}
