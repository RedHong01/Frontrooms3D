using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The terminal's picture (41 section 3.1): the 80 x 25 grid as VGA text mode, 720 x 400 at 2x = 1440 x 800, whole
/// pixels, with mips. Rendered only when the grid changes (a key, the clock's minute, a face swap): never per frame.
///   GPU (default): the cells (80 x 25 RGBA8, 8 KB) are uploaded and one offscreen procedural draw of
///     Hidden/FrontRooms/TerminalRaster fills a 1440 x 800 sRGB RenderTexture, then its mips are generated on the GPU.
///     5.9 MiB per picture, GPU only (no CPU copy).
///   CPU (fallback: no graphics device, or the raster shader unsupported): <see cref="RasterCpu"/> into a readable
///     Texture2D with CPU mips. About twice the memory (CPU and GPU copies) and slower; the harness also uses it as the
///     reference the GPU picture is checked against, pixel for pixel.
/// The picture is a plain texture for the CRT tube (FrontRoomsScreenVideo.ShowTerminal), which stretches it to the
/// desk monitor's 276 x 207 mm underscan picture as the hardware did (pixels taller than wide).
/// </summary>
public sealed class FrontRoomsTerminalRaster : IDisposable
{
    public const int Scale = 2, DotW = 9, DotH = 16;
    public const int Width = FrontRoomsTerminalGrid.Cols * DotW * Scale;    // 1440
    public const int Height = FrontRoomsTerminalGrid.Rows * DotH * Scale;   // 800
    public const string ShaderPath = "Rendering/FrontRoomsTerminalRaster";

    static Material material;
    static bool shaderTried;
    static readonly int CellsId = Shader.PropertyToID("_TermCells");
    static readonly int FontId = Shader.PropertyToID("_TermFont");
    static readonly int PaletteId = Shader.PropertyToID("_TermPalette");

    readonly bool gpu;
    RenderTexture rt;
    Texture2D cells;
    Color32[] cellPixels;
    MaterialPropertyBlock props;
    CommandBuffer cmd;
    readonly Vector4[] palette = new Vector4[16];
    Texture2D cpuTexture;
    Color32[] cpuPixels;

    /// <summary>The picture to bind on the tube (a RenderTexture on the GPU path, a Texture2D on the CPU path).</summary>
    public Texture Picture => gpu ? (Texture)rt : cpuTexture;
    public bool Gpu => gpu;
    /// <summary>Renders since creation (the harness counts them: one per screen change).</summary>
    public int Renders { get; private set; }
    /// <summary>CPU time of the last and of the slowest render (ms), the GPU path's upload and command recording included.</summary>
    public float LastCpuMs { get; private set; }
    public float MaxCpuMs { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        if (material != null) UnityEngine.Object.Destroy(material);
        material = null;
        shaderTried = false;
    }

    /// <summary>Whether the GPU path runs here (a graphics device and a supported raster shader).</summary>
    public static bool GpuSupported
    {
        get
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return false;
            if (!shaderTried)
            {
                shaderTried = true;
                var shader = Resources.Load<Shader>(ShaderPath);
                if (shader != null && shader.isSupported) material = new Material(shader) { name = "FrontRooms terminal raster", hideFlags = HideFlags.DontSave };
                else UnityEngine.Debug.LogWarning("[FrontRoomsTerminalRaster] raster shader " + (shader == null ? "missing" : "unsupported") + "; terminals use the CPU raster.");
            }
            return material != null && SystemInfo.graphicsShaderLevel >= 35 && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32);
        }
    }

    /// <summary>Bytes of one picture with its mips (1440 x 800 x 4 B, 11 levels).</summary>
    public static long PictureBytes
    {
        get
        {
            long sum = 0;
            int w = Width, h = Height;
            while (true)
            {
                sum += (long)w * h * 4;
                if (w == 1 && h == 1) break;
                w = Mathf.Max(1, w / 2);
                h = Mathf.Max(1, h / 2);
            }
            return sum;
        }
    }

    /// <summary>Bytes this raster holds: GPU = the picture + the cells texture; CPU = the picture twice (CPU and GPU copies) + its buffer.</summary>
    public long Bytes => gpu ? PictureBytes + 80 * 25 * 4 * 2 : PictureBytes * 2 + (long)Width * Height * 4;

    public FrontRoomsTerminalRaster(bool forceCpu = false)
    {
        gpu = !forceCpu && GpuSupported;
        if (gpu)
        {
            var desc = new RenderTextureDescriptor(Width, Height, RenderTextureFormat.ARGB32, 0)
            {
                sRGB = true, useMipMap = true, autoGenerateMips = false, msaaSamples = 1,
            };
            rt = new RenderTexture(desc)
            {
                name = "FrontRooms terminal picture", filterMode = FilterMode.Trilinear, anisoLevel = 4,
                wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave,
            };
            rt.Create();
            cells = new Texture2D(FrontRoomsTerminalGrid.Cols, FrontRoomsTerminalGrid.Rows, TextureFormat.RGBA32, false, true)
            {
                name = "FrontRooms terminal cells", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave,
            };
            cellPixels = new Color32[FrontRoomsTerminalGrid.Cols * FrontRoomsTerminalGrid.Rows];
            props = new MaterialPropertyBlock();
            cmd = new CommandBuffer { name = "FrontRooms terminal raster" };
        }
        else
        {
            cpuTexture = new Texture2D(Width, Height, TextureFormat.RGBA32, true, false)
            {
                name = "FrontRooms terminal picture (CPU)", filterMode = FilterMode.Trilinear, anisoLevel = 4,
                wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave,
            };
            cpuPixels = new Color32[Width * Height];
        }
    }

    /// <summary>The GPU picture's content was lost (a device reset on some platforms): the owner renders it again.</summary>
    public bool Lost => gpu && rt != null && !rt.IsCreated();

    public void Render(FrontRoomsTerminalGrid grid, FrontRoomsTerminalFace face)
    {
        if (grid == null || face == null) return;
        var sw = Stopwatch.StartNew();
        if (gpu)
        {
            if (rt == null) return;
            if (!rt.IsCreated()) rt.Create();
            grid.CopyTo(cellPixels);
            cells.SetPixelData(cellPixels, 0);
            cells.Apply(false, false);
            for (var i = 0; i < 16; i++)
            {
                var c = ((Color)FrontRoomsTerminalPalette.Vga[i]).linear;
                palette[i] = new Vector4(c.r, c.g, c.b, 1f);
            }
            props.SetTexture(CellsId, cells);
            props.SetTexture(FontId, face.GlyphTexture);
            props.SetVectorArray(PaletteId, palette);
            cmd.Clear();
            cmd.SetRenderTarget(rt, 0);
            cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3, 1, props);
            cmd.GenerateMips(rt);
            Graphics.ExecuteCommandBuffer(cmd);
        }
        else
        {
            if (cpuTexture == null) return;
            RasterCpu(grid, face, cpuPixels);
            cpuTexture.SetPixelData(cpuPixels, 0);
            cpuTexture.Apply(true, false);
        }
        Renders++;
        LastCpuMs = (float)sw.Elapsed.TotalMilliseconds;
        MaxCpuMs = Mathf.Max(MaxCpuMs, LastCpuMs);
    }

    /// <summary>
    /// The text-mode raster on the CPU (the shader's twin): dst = Width x Height in Unity texture order (row 0 = the
    /// picture's bottom). Each dot is 2 x 2 pixels; the 9th dot column follows the VGA rule for CP437 0xC0-0xDF.
    /// </summary>
    public static void RasterCpu(FrontRoomsTerminalGrid grid, FrontRoomsTerminalFace face, Color32[] dst)
    {
        var line = new Color32[Width];
        for (var row = 0; row < FrontRoomsTerminalGrid.Rows; row++)
        {
            for (var fy = 0; fy < DotH; fy++)
            {
                for (var col = 0; col < FrontRoomsTerminalGrid.Cols; col++)
                {
                    var code = grid.Glyph(col, row);
                    var a = grid.Attr(col, row);
                    var fg = FrontRoomsTerminalPalette.Fg(a);
                    var bg = FrontRoomsTerminalPalette.Bg(a);
                    var bits = face.Row(code, fy);
                    var nine = FrontRoomsTerminalFace.NinthColumn(code) && (bits & 1) != 0;
                    var x0 = col * DotW * Scale;
                    for (var x = 0; x < DotW; x++)
                    {
                        var on = x < 8 ? (bits >> (7 - x) & 1) != 0 : nine;
                        var c = on ? fg : bg;
                        line[x0 + x * 2] = c;
                        line[x0 + x * 2 + 1] = c;
                    }
                }
                var yTop = row * DotH * Scale + fy * Scale;
                Array.Copy(line, 0, dst, (Height - 1 - yTop) * Width, Width);
                Array.Copy(line, 0, dst, (Height - 2 - yTop) * Width, Width);
            }
        }
    }

    public void Dispose()
    {
        Kill(rt); rt = null;
        Kill(cells); cells = null;
        Kill(cpuTexture); cpuTexture = null;
        cpuPixels = null;
        cmd?.Release(); cmd = null;
    }

    static void Kill(UnityEngine.Object o)
    {
        if (o == null) return;
        if (o is RenderTexture r) r.Release();
        if (Application.isPlaying) UnityEngine.Object.Destroy(o); else UnityEngine.Object.DestroyImmediate(o);
    }
}
