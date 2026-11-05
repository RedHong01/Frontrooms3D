using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Dust in lamp light (技术美术调研 row 9, research/tech_art/20_ranked_changes.md 3.9; Red 2026-11-04 12:49 in its chat,
/// "全部按推荐": the corridor's 60 additive beams go, and depth- and view-angle-faded shafts come back only in the room
/// families that want dust). A shaft is a box below a lamp's lens that the lamp's light fills: its shader
/// (Resources/Rendering/FrontRoomsDustShaft) marches the view ray through the box in a few jittered steps and stops at the
/// scene depth, so the dust ends softly where the floor, a desk or a wall begins; it is brightest just under the lens,
/// spreads from the lens's rectangle with a smooth tail (no flat side, so nothing shows edge-on), glows most when the eye
/// looks toward the lamp, drifts slowly in thin layers, and fades to nothing inside the box. 1990 office air: faint; dust
/// in lamp light seen from eye height, never a cone of fog.
/// One material for every shaft and no MaterialPropertyBlock: the lamp's colour, level and a seed ride in the renderer's
/// shader user value (MeshRenderer.SetShaderUserValue), so shafts stay in the SRP Batcher, one SetPass for all of them. The
/// box sizes ride in the shared mesh (one per lamp shape). Nothing is built until something asks for dust: the stream's
/// rooms through FrontRoomsRoomStream.SetRoomDust (no room family asks today); other lamps (a vignette's own fixtures) can
/// use Build and Drive directly. WebGL builds none (its own track). Handhelds march fewer steps.
/// </summary>
public static class FrontRoomsDustShaft
{
    public const string ShaderResource = "Rendering/FrontRoomsDustShaft";
    /// <summary>The 2'x4' troffer's lens (L03, FrontRoomsTrofferKit), half sizes across (x) and along (z), metres.</summary>
    public const float TrofferLensHalfX = .27f, TrofferLensHalfZ = .56f;
    /// <summary>How far below a troffer's lens its shaft's box reaches: 1.7 m, to about 1.2 m above the floor (ceiling
    /// 2.9 m; the light has fallen to a tenth at eye height).</summary>
    public const float TrofferShaftLength = 1.7f;
    // Sideways the lit dust spreads from the lens's rectangle with a tail whose width is 5 cm at the lens and grows 24 cm a
    // metre down; the box holds the tail to 2.2 widths, where it is down to 4 % (the shader fades it to 0 at the box).
    const float Spread = .24f, BlurAtLens = .05f, BoxReach = 2.2f;
    const int NoiseSize = 32;

    static Material material;
    static Texture3D noise;
    static bool missing;
    static readonly Dictionary<long, Mesh> meshes = new Dictionary<long, Mesh>();

    /// <summary>False on WebGL players: no shaft is built there.</summary>
    public static bool Supported
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return false;
#else
            return SystemInfo.supports3DTextures;
#endif
        }
    }

    /// <summary>The one material every shaft shares (null when the shader is missing or on WebGL).</summary>
    public static Material Material
    {
        get
        {
            if (material != null || missing || !Supported) return material;
            var shader = Resources.Load<Shader>(ShaderResource);
            if (shader == null || !shader.isSupported)
            {
                missing = true;
                Debug.LogWarning("[FrontRooms3D] FrontRooms/DustShaft is unavailable; rooms stay without dust shafts.");
                return null;
            }
            // Queue 3100: after the other transparents, so the shafts draw back to back (one SRP batch, one SetPass) instead
            // of being sorted among the glass and signs (each break a SetPass); the dust only adds light, so order does not
            // change what it adds.
            material = new Material(shader) { name = "FrontRooms / dust shaft", renderQueue = 3100 };
            material.SetTexture("_DustNoise", Noise());
            // Handhelds: fewer steps (the dust is smooth along the ray and the shafts are faint; the jitter hides the steps).
            if (FrontRoomsHandheld.Active) material.SetFloat("_Steps", 5f);
            return material;
        }
    }

    /// <summary>
    /// A shaft below a lens: a child of <paramref name="parent"/> at <paramref name="lensCentre"/> (the lens face's centre,
    /// parent space), running <paramref name="length"/> metres down the parent's -Y. Built switched off: Drive lights it.
    /// Returns null (nothing built) on WebGL or without the shader.
    /// </summary>
    public static MeshRenderer Build(Transform parent, Vector3 lensCentre, float lensHalfX, float lensHalfZ, float length, string name)
    {
        var shared = Material;
        if (shared == null || parent == null) return null;
        var go = new GameObject(string.IsNullOrEmpty(name) ? "dust shaft" : name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = lensCentre;
        go.AddComponent<MeshFilter>().sharedMesh = ShaftMesh(lensHalfX, lensHalfZ, length);
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = shared;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        renderer.SetShaderUserValue(0u);
        renderer.enabled = false;
        return renderer;
    }

    /// <summary>
    /// The shader user value for a shaft: its lamp's colour (a tint up to 1, RGB565 in linear), its level (0..1: the
    /// lamp's output times the room's dust, 8 bit) and a per-shaft seed (0..255) that keeps neighbouring shafts' dust apart.
    /// A level under half a step packs as 0 (off).
    /// </summary>
    public static uint Pack(Color color, float level, int seed)
    {
        var step = Mathf.RoundToInt(Mathf.Clamp01(level) * 255f);
        if (step <= 0) return 0u;
        var c = color.linear;
        var r = (uint)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 31f);
        var g = (uint)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 63f);
        var b = (uint)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 31f);
        return (r << 27) | (g << 21) | (b << 16) | ((uint)step << 8) | ((uint)seed & 255u);
    }

    /// <summary>Writes a packed value (Pack) to a shaft; drawn only while its level is above 0.</summary>
    public static void Drive(MeshRenderer shaft, uint packed)
    {
        if (shaft == null) return;
        shaft.SetShaderUserValue(packed);
        shaft.enabled = ((packed >> 8) & 255u) != 0u;
    }

    // The dust: a 32^3 tiling value noise (two octaves, 8 and 16 cells a tile), made once on the CPU (about 0.3 M hash
    // steps, a few ms) the first time a room asks for dust; 32 KB, R8, repeat, bilinear. No asset, no download.
    static Texture3D Noise()
    {
        if (noise != null) return noise;
        var n = NoiseSize;
        var data = new byte[n * n * n];
        for (var z = 0; z < n; z++)
            for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                {
                    var v = .62f * Octave(x, y, z, 4, 17) + .38f * Octave(x, y, z, 2, 101);
                    data[(z * n + y) * n + x] = (byte)Mathf.RoundToInt(Mathf.Clamp01(v) * 255f);
                }
        noise = new Texture3D(n, n, n, TextureFormat.R8, false)
        {
            name = "FrontRooms / dust noise",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            anisoLevel = 0,
            hideFlags = HideFlags.DontSave
        };
        noise.SetPixelData(data, 0);
        noise.Apply(false, true);
        return noise;
    }

    // Value noise on a lattice of NoiseSize / period cells that wraps, smoothstepped between cells.
    static float Octave(int x, int y, int z, int period, int salt)
    {
        var cells = NoiseSize / period;
        int ix = x / period, iy = y / period, iz = z / period;
        float fx = (x % period) / (float)period, fy = (y % period) / (float)period, fz = (z % period) / (float)period;
        fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy); fz = fz * fz * (3f - 2f * fz);
        int x1 = (ix + 1) % cells, y1 = (iy + 1) % cells, z1 = (iz + 1) % cells;
        var a = Mathf.Lerp(Mathf.Lerp(Lattice(ix, iy, iz, salt), Lattice(x1, iy, iz, salt), fx), Mathf.Lerp(Lattice(ix, y1, iz, salt), Lattice(x1, y1, iz, salt), fx), fy);
        var b = Mathf.Lerp(Mathf.Lerp(Lattice(ix, iy, z1, salt), Lattice(x1, iy, z1, salt), fx), Mathf.Lerp(Lattice(ix, y1, z1, salt), Lattice(x1, y1, z1, salt), fx), fy);
        return Mathf.Lerp(a, b, fz);
    }

    static float Lattice(int x, int y, int z, int salt)
    {
        unchecked
        {
            var h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(z * 83492791) ^ (uint)(salt * 668265263);
            h ^= h >> 13;
            h *= 0x5bd1e995u;
            h ^= h >> 15;
            return (h & 0xffffu) / 65535f;
        }
    }

    // One closed box per lamp shape, its far faces drawn by the shader (Cull Front). The shape rides in every vertex:
    // uv0 = (lens half x, lens half z, length, spread), uv1 = (box half x, box half z, blur width at the lens, 0).
    static Mesh ShaftMesh(float lensHalfX, float lensHalfZ, float length)
    {
        var key = ((long)Mathf.RoundToInt(lensHalfX * 1000f) << 40) ^ ((long)Mathf.RoundToInt(lensHalfZ * 1000f) << 20) ^ Mathf.RoundToInt(length * 1000f);
        Mesh mesh;
        if (meshes.TryGetValue(key, out mesh) && mesh != null) return mesh;
        var reach = BoxReach * (BlurAtLens + Spread * length);
        var bx = lensHalfX + reach;
        var bz = lensHalfZ + reach;
        var corners = new Vector3[8];
        for (var i = 0; i < 8; i++)
            corners[i] = new Vector3((i & 1) == 0 ? -bx : bx, (i & 2) == 0 ? -length : 0f, (i & 4) == 0 ? -bz : bz);
        // Faces as corner quads; each triangle wound clockwise seen from outside (Unity's front face), checked below.
        int[][] faces =
        {
            new[] { 0, 2, 3, 1 }, // -Z
            new[] { 5, 7, 6, 4 }, // +Z
            new[] { 4, 6, 2, 0 }, // -X
            new[] { 1, 3, 7, 5 }, // +X
            new[] { 0, 1, 5, 4 }, // -Y (bottom)
            new[] { 2, 6, 7, 3 }, // +Y (lens plane)
        };
        var tris = new List<int>(36);
        var centre = new Vector3(0f, -length * .5f, 0f);
        for (var f = 0; f < faces.Length; f++)
        {
            var q = faces[f];
            var a = corners[q[0]];
            var n = Vector3.Cross(corners[q[1]] - a, corners[q[2]] - a);
            var outward = (a + corners[q[2]]) * .5f - centre;
            if (Vector3.Dot(n, outward) >= 0f) tris.AddRange(new[] { q[0], q[1], q[2], q[0], q[2], q[3] });
            else tris.AddRange(new[] { q[0], q[2], q[1], q[0], q[3], q[2] });
        }
        var shape = new Vector4(lensHalfX, lensHalfZ, length, Spread);
        var shape2 = new Vector4(bx, bz, BlurAtLens, 0f);
        var uv0 = new List<Vector4>(8);
        var uv1 = new List<Vector4>(8);
        for (var i = 0; i < 8; i++) { uv0.Add(shape); uv1.Add(shape2); }
        mesh = new Mesh { name = "Dust shaft box " + lensHalfX.ToString("0.00") + " x " + lensHalfZ.ToString("0.00") + " x " + length.ToString("0.00") };
        mesh.SetVertices(corners);
        mesh.SetUVs(0, uv0);
        mesh.SetUVs(1, uv1);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mesh.UploadMeshData(true);
        meshes[key] = mesh;
        return mesh;
    }
}
