// The desktop Metal ray-traced glass reflections, C# side (G14 P0 + P1; design 10 §1.3-§1.8, §2.2, §2.6, §2.7).
// macOS editor / macOS player only. Everything here runs on the main thread and never touches Metal:
//   - scene registry: one native mesh per Unity Mesh (copied, never Unity's buffers), a material table, a
//     texture table, one instance record per renderer (flags + mask per instance, R15);
//   - registration without MapWorld edits (P0): the map is found once, then a cheap per-frame diff of its chunk
//     roots and their child counts; new or changed chunks are rescanned, registration is sliced to ~1 ms/frame;
//   - per traced camera: same-frame liveness of receivers / dynamic instances, a TLAS candidate list around the
//     camera (priority: receivers, glass, dynamic, Relay; then nearest, cap 2,048 High / 4,096 Ultra), the lamps
//     that are on this frame, ambient SH, fog, the zone cube, and one frame packet pushed to the plugin.
// The render thread (plugin event) does all Metal work: BLAS builds, TLAS, trace, resolve.
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
using System;
using System.Collections.Generic;
using System.Diagnostics;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Native = FrontRoomsGlassRTNative;
using Object = UnityEngine.Object;

// The verify harness (Assets/Editor/RT/FrontRoomsGlassRTVerify.cs) reads the per-camera targets and stats.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Assembly-CSharp-Editor")]

static class FrontRoomsGlassRTSystem
{
    // ------------------------------------------------------------------ constants
    /// <summary>Rendering-layer bit of receivers drawn with their own FRGlassRTPrepass pass (FrontRooms/Glass).</summary>
    public const int PrepassBit = 30;
    /// <summary>Rendering-layer bit of receivers drawn with Hidden/FrontRooms/GlassRTPrepass (other materials).</summary>
    public const int OverrideBit = 29;
    const int MaxMaterials = 4096, MaxTextures = 4096, MaxLamps = 128;
    const float LampRadius = 30f;
    const int StaticRefreshPerFrame = 32;
    const float NearDynamic2 = 15f * 15f;    // dynamic transforms read every frame within 15 m, every 8th frame beyond
    const float NearLens2 = 20f * 20f;       // lens emission read every frame within 20 m, round-robin beyond
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    static readonly int CeilingHeightId = Shader.PropertyToID("_CeilingHeight");
    static readonly int RTReceiveId = Shader.PropertyToID("_RTReceive");

    // ------------------------------------------------------------------ settings (internal knobs for the harness)
    internal static float RegisterBudgetMs = 1.0f;
    internal static uint DebugMode;               // Native.DebugNone / DebugParity / DebugHitId
    internal static bool AllowAccumulation = true;
    internal static int AccumulationCap = 32;
    internal static bool BackSurfaceImage = true;
    internal static float EdgeFilterHigh = .6f, EdgeFilterUltra = .45f;
    internal static int HighShadowLamps = 4, UltraShadowLamps = 16;

    // ------------------------------------------------------------------ capability
    static bool started, nativeChecked, nativeOk;
    static string nativeError = "";
    static int caps;
    static IntPtr eventFunc;
    static FrontRoomsGlassRTQuality? qualityOverride;
    static bool loggedOn;

    public static IntPtr EventFunc => eventFunc;

    public static FrontRoomsGlassRTQuality Quality
    {
        get
        {
            if (qualityOverride.HasValue) return qualityOverride.Value;
            var q = DefaultQuality();
            qualityOverride = q;
            return q;
        }
        set => qualityOverride = value;
    }

    static FrontRoomsGlassRTQuality DefaultQuality()
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i + 1 < args.Length; i++)
            if (args[i] == "-frGlassRT" && TryParse(args[i + 1], out var q)) return q;
        try
        {
            var pref = PlayerPrefs.GetString("FrontRooms.GlassRT", "");
            if (TryParse(pref, out var p)) return p;
        }
        catch { }
        var level = QualitySettings.GetQualityLevel();
        return level >= 5 ? FrontRoomsGlassRTQuality.Ultra : level >= 3 ? FrontRoomsGlassRTQuality.High : FrontRoomsGlassRTQuality.Off;
    }

    static bool TryParse(string s, out FrontRoomsGlassRTQuality q)
    {
        q = FrontRoomsGlassRTQuality.Off;
        switch ((s ?? "").Trim().ToLowerInvariant())
        {
            case "off": q = FrontRoomsGlassRTQuality.Off; return true;
            case "high": q = FrontRoomsGlassRTQuality.High; return true;
            case "ultra": q = FrontRoomsGlassRTQuality.Ultra; return true;
        }
        return false;
    }

    static void CheckNative()
    {
        if (nativeChecked) return;
        nativeChecked = true;
        if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Metal) { nativeError = "graphics API is " + SystemInfo.graphicsDeviceType + ", not Metal"; return; }
        try
        {
            if (!Native.SizeCheck(out var sizeError)) { nativeError = "struct layout: " + sizeError; return; }
            caps = Native.FRGlassRT_QueryCaps();
            eventFunc = Native.FRGlassRT_GetRenderEventFunc();
            nativeOk = eventFunc != IntPtr.Zero;
            if (!nativeOk) nativeError = "plugin returned no render event";
        }
        catch (DllNotFoundException e) { nativeError = "plugin not loaded: " + e.Message; }
        catch (EntryPointNotFoundException e) { nativeError = "plugin is an old build: " + e.Message; }
        catch (Exception e) { nativeError = "plugin error: " + e.GetType().Name + " " + e.Message; }
    }

    static int Caps
    {
        get
        {
            if (!nativeOk) return 0;
            caps = Native.FRGlassRT_QueryCaps();
            return caps;
        }
    }

    static bool WebGLTargetInEditor
    {
        get
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WebGL;
#else
            return false;
#endif
        }
    }

    public static bool Supported
    {
        get
        {
            CheckNative();
            if (!nativeOk || WebGLTargetInEditor) return false;
            var c = Caps;
            const int need = Native.CapDevice | Native.CapRaytracing | Native.CapApple9 | Native.CapMetal3 | Native.CapOS14 | Native.CapKernelReady | Native.CapLayoutOK;
            const int bad = Native.CapGpuError | Native.CapLayoutFailed | Native.CapCompileFailed;
            return (c & need) == need && (c & bad) == 0;
        }
    }

    public static string Status
    {
        get
        {
            CheckNative();
            if (!nativeOk) return "off: " + nativeError;
            if (WebGLTargetInEditor) return "off: the editor's active build target is WebGL (the WebGL tier has no RT)";
            var c = Caps;
            if ((c & Native.CapDevice) == 0) return "off: no Metal device reached the plugin (UnityPluginLoad / device init missing)";
            if ((c & Native.CapRaytracing) == 0) return "off: this GPU has no Metal ray tracing";
            if ((c & Native.CapApple9) == 0) return "off: not an Apple9 GPU (M1/M2 trace in software: P2 tier, off by default)";
            if ((c & Native.CapOS14) == 0) return "off: macOS 14 or newer is required";
            if ((c & Native.CapCompileFailed) != 0) return "off: kernel compile failed: " + Native.LastError();
            if ((c & Native.CapLayoutFailed) != 0) return "off: GPU layout self-test failed: " + Native.LastError();
            if ((c & Native.CapGpuError) != 0) return "off: a command buffer failed this session: " + Native.LastError();
            if ((c & Native.CapKernelReady) == 0) return "waiting: kernel compiling";
            if ((c & Native.CapLayoutOK) == 0) return "waiting: layout self-test";
            var residency = Native.Get(Native.Stat.ResidencyMode) == 1 ? "residency set" : "useResource";
            return "on: Apple9, " + Quality + ", kernel " + Native.Get(Native.Stat.CompileMs).ToString("0") + " ms (async), layout OK, " + residency
                + ", hazard depth " + Native.Get(Native.Stat.HazardDepth) + " / out " + Native.Get(Native.Stat.HazardOut)
                + ", timers " + ((c & Native.CapCounters) != 0 ? "stage-boundary counters" : "command buffer only");
        }
    }

    public static bool Active => Supported && Quality != FrontRoomsGlassRTQuality.Off;

    /// <summary>Hardware or a failure rules RT out for this session (no scanning, no native calls per frame).</summary>
    static bool PermanentlyOff
    {
        get
        {
            var c = Caps;
            const int need = Native.CapDevice | Native.CapRaytracing | Native.CapApple9 | Native.CapMetal3 | Native.CapOS14;
            const int bad = Native.CapGpuError | Native.CapLayoutFailed | Native.CapCompileFailed;
            return (c & need) != need || (c & bad) != 0;
        }
    }

    // ------------------------------------------------------------------ cameras
    static int nextCameraId = 1;
    static readonly List<FrontRoomsGlassRTCamera> cameras = new List<FrontRoomsGlassRTCamera>();

    public static void OptIn(Camera camera)
    {
        if (camera == null) return;
        EnsureStarted();
        if (!camera.TryGetComponent<FrontRoomsGlassRTCamera>(out var state)) state = camera.gameObject.AddComponent<FrontRoomsGlassRTCamera>();
        if (state.id < 0) { state.id = nextCameraId++; cameras.Add(state); }
    }

    public static void ReleaseCamera(FrontRoomsGlassRTCamera state)
    {
        if (state == null || state.id < 0) return;
        cameras.Remove(state);
        if (nativeOk) Native.FRGlassRT_FreeCamera(state.id);
        state.id = -1;
    }

    public static void EnsureStarted()
    {
        if (started) return;
        started = true;
        CheckNative();
        if (nativeOk) Native.FRGlassRT_ResetScene();   // a new session: the render thread drops the previous scene (generation-safe)
        if (nativeOk) Debug.Log("[FrontRoomsGlassRT] plugin loaded · caps 0x" + Caps.ToString("x") + " · " + Status);
        else Debug.Log("[FrontRoomsGlassRT] " + Status);
    }

    // ------------------------------------------------------------------ registry: meshes
    sealed class MeshEntry
    {
        public Mesh mesh;
        public int key;
        public int refs;
        public int triangles;
        public int[] geometrySubmesh;   // geometry index -> Unity submesh index
        public bool deforming, failed, gpuPath;
    }

    static readonly Dictionary<int, MeshEntry> meshById = new Dictionary<int, MeshEntry>();
    static readonly Dictionary<int, MeshEntry> meshByKey = new Dictionary<int, MeshEntry>();
    static int nextMeshKey;
    static int meshesUploadedCPU, meshesUploadedGPU, meshUploadFailures;

    static MeshEntry AcquireMesh(Mesh mesh, bool deforming)
    {
        if (mesh == null) return null;
        var id = mesh.GetInstanceID();
        if (meshById.TryGetValue(id, out var e))
        {
            if (e.failed) return null;
            e.refs++;
            return e;
        }
        e = new MeshEntry { mesh = mesh, key = nextMeshKey++, deforming = deforming };
        if (!Upload(e)) { e.failed = true; meshById[id] = e; meshUploadFailures++; return null; }
        e.refs = 1;
        meshById[id] = e;
        meshByKey[e.key] = e;
        return e;
    }

    static void ReleaseMeshRef(MeshEntry e)
    {
        if (e == null || --e.refs > 0) return;
        if (nativeOk) Native.FRGlassRT_ReleaseMesh(e.key);
        meshByKey.Remove(e.key);
        if (e.mesh != null) meshById.Remove(e.mesh.GetInstanceID());
        else
        {
            // the Mesh object is already destroyed: find the entry by reference
            int dead = int.MinValue;
            foreach (var kv in meshById) if (kv.Value == e) { dead = kv.Key; break; }
            if (dead != int.MinValue) meshById.Remove(dead);
        }
    }

    static bool Upload(MeshEntry e)
    {
        var mesh = e.mesh;
        var subs = new List<int>();
        for (var s = 0; s < mesh.subMeshCount; s++)
            if (mesh.GetTopology(s) == MeshTopology.Triangles && mesh.GetIndexCount(s) >= 3) subs.Add(s);
        if (subs.Count == 0 || mesh.vertexCount == 0) return false;
        e.geometrySubmesh = subs.ToArray();
        if (mesh.isReadable) return UploadCPU(e, subs);
        return UploadGPU(e, subs);
    }

    static bool UploadCPU(MeshEntry e, List<int> subs)
    {
        var mesh = e.mesh;
        var verts = mesh.vertices;
        var normals = mesh.normals;
        var uvs = mesh.uv;
        var colors = mesh.colors;
        var n = verts.Length;
        var attrs = new Native.VertexAttr[n];
        var hasN = normals != null && normals.Length == n;
        var hasUV = uvs != null && uvs.Length == n;
        var hasC = colors != null && colors.Length == n;
        for (var i = 0; i < n; i++)
        {
            var a = new Native.VertexAttr();
            if (hasN) { a.nx = normals[i].x; a.ny = normals[i].y; a.nz = normals[i].z; } else a.ny = 1f;
            if (hasUV) { a.u = uvs[i].x; a.v = uvs[i].y; }
            if (hasC) a.colorR = colors[i].r;
            attrs[i] = a;
        }
        var all = new List<int>();
        var starts = new int[subs.Count];
        var counts = new int[subs.Count];
        for (var k = 0; k < subs.Count; k++)
        {
            var idx = mesh.GetIndices(subs[k], true);
            starts[k] = all.Count;
            counts[k] = idx.Length - idx.Length % 3;
            for (var i = 0; i < counts[k]; i++) all.Add(idx[i]);
        }
        var indices = all.ToArray();
        e.triangles = indices.Length / 3;
        var r = Native.FRGlassRT_UploadMeshCPU(e.key, verts, attrs, n, indices, indices.Length, starts, counts, subs.Count, e.deforming ? 1 : 0, 0);
        if (r != 0) { Warn("mesh " + mesh.name + " upload failed (" + r + ")"); return false; }
        meshesUploadedCPU++;
        return true;
    }

    static uint FormatCode(VertexAttributeFormat f)
    {
        switch (f)
        {
            case VertexAttributeFormat.Float32: return 1;
            case VertexAttributeFormat.Float16: return 2;
            case VertexAttributeFormat.UNorm8: return 3;
            case VertexAttributeFormat.SNorm8: return 4;
            case VertexAttributeFormat.UNorm16: return 5;
            case VertexAttributeFormat.SNorm16: return 6;
        }
        return 0;
    }

    // Non-readable meshes (the kit FBX): Unity's native buffers, fetched ONCE per Mesh per session, gathered into the
    // plugin's own copy on the render thread (fr_gather_*). Unity's buffers are released when that command buffer completes.
    static bool UploadGPU(MeshEntry e, List<int> subs)
    {
        var mesh = e.mesh;
        if (!mesh.HasVertexAttribute(VertexAttribute.Position) || mesh.GetVertexAttributeFormat(VertexAttribute.Position) != VertexAttributeFormat.Float32
            || mesh.GetVertexAttributeDimension(VertexAttribute.Position) < 3)
        { Warn("mesh " + mesh.name + ": positions are not Float32x3; skipped"); return false; }
        var streams = new IntPtr[4];
        var args = new Native.GatherVertexArgs { vertexCount = (uint)mesh.vertexCount };
        void Attr(VertexAttribute a, ref uint stride, ref uint offset, ref uint format, int slot)
        {
            if (!mesh.HasVertexAttribute(a)) { format = 0; return; }
            var s = mesh.GetVertexAttributeStream(a);
            if (s < 0 || s > 3) { format = 0; return; }
            if (streams[slot] == IntPtr.Zero) streams[slot] = mesh.GetNativeVertexBufferPtr(s);
            stride = (uint)mesh.GetVertexBufferStride(s);
            offset = (uint)mesh.GetVertexAttributeOffset(a);
            format = FormatCode(mesh.GetVertexAttributeFormat(a));
        }
        uint posFormat = 0;
        Attr(VertexAttribute.Position, ref args.posStride, ref args.posOffset, ref posFormat, 0);
        Attr(VertexAttribute.Normal, ref args.nrmStride, ref args.nrmOffset, ref args.nrmFormat, 1);
        Attr(VertexAttribute.TexCoord0, ref args.uvStride, ref args.uvOffset, ref args.uvFormat, 2);
        Attr(VertexAttribute.Color, ref args.colStride, ref args.colOffset, ref args.colFormat, 3);
        if (streams[0] == IntPtr.Zero) { Warn("mesh " + mesh.name + ": no native vertex buffer"); return false; }
        var ib = mesh.GetNativeIndexBufferPtr();
        if (ib == IntPtr.Zero) { Warn("mesh " + mesh.name + ": no native index buffer"); return false; }
        var indexSize = mesh.indexFormat == IndexFormat.UInt16 ? 2u : 4u;
        var ia = new Native.GatherIndexArgs[subs.Count];
        uint dst = 0;
        for (var k = 0; k < subs.Count; k++)
        {
            var count = (uint)mesh.GetIndexCount(subs[k]);
            count -= count % 3;
            ia[k] = new Native.GatherIndexArgs { count = count, indexSize = indexSize, srcStart = (uint)mesh.GetIndexStart(subs[k]), dstStart = dst, baseVertex = (uint)mesh.GetBaseVertex(subs[k]) };
            dst += count;
        }
        e.triangles = (int)(dst / 3);
        e.gpuPath = true;
        var r = Native.FRGlassRT_UploadMeshGPU(e.key, streams[0], streams[1], streams[2], streams[3], ref args, ib, ia, subs.Count, (int)dst);
        if (r != 0) { Warn("mesh " + mesh.name + " GPU upload failed (" + r + ")"); return false; }
        meshesUploadedGPU++;
        return true;
    }

    // ------------------------------------------------------------------ registry: materials and textures
    static readonly Dictionary<int, int> materialBlockSingle = new Dictionary<int, int>();
    static readonly Dictionary<string, int> materialBlockMulti = new Dictionary<string, int>();
    static readonly Dictionary<int, int> textureSlot = new Dictionary<int, int>();
    static int nextMaterial, nextTexture = 4;
    static readonly System.Text.StringBuilder keyBuilder = new System.Text.StringBuilder();

    static int MaterialBlock(Material[] materials, int[] geometrySubmesh)
    {
        if (geometrySubmesh.Length == 1)
        {
            var m = Pick(materials, geometrySubmesh[0]);
            var id = m != null ? m.GetInstanceID() : 0;
            if (materialBlockSingle.TryGetValue(id, out var b)) return b;
            if (nextMaterial + 1 > MaxMaterials) return 0;
            b = nextMaterial++;
            UploadMaterials(b, new[] { m });
            materialBlockSingle[id] = b;
            return b;
        }
        keyBuilder.Clear();
        foreach (var s in geometrySubmesh) { var m = Pick(materials, s); keyBuilder.Append(m != null ? m.GetInstanceID() : 0).Append(','); }
        var key = keyBuilder.ToString();
        if (materialBlockMulti.TryGetValue(key, out var blk)) return blk;
        if (nextMaterial + geometrySubmesh.Length > MaxMaterials) return 0;
        blk = nextMaterial;
        nextMaterial += geometrySubmesh.Length;
        var list = new Material[geometrySubmesh.Length];
        for (var k = 0; k < list.Length; k++) list[k] = Pick(materials, geometrySubmesh[k]);
        UploadMaterials(blk, list);
        materialBlockMulti[key] = blk;
        return blk;
    }

    static Material Pick(Material[] materials, int submesh)
    {
        if (materials == null || materials.Length == 0) return null;
        return materials[Mathf.Min(submesh, materials.Length - 1)];
    }

    static void UploadMaterials(int first, Material[] list)
    {
        var recs = new Native.Material[list.Length];
        for (var i = 0; i < list.Length; i++) recs[i] = Describe(list[i]);
        Native.FRGlassRT_SetMaterials(first, recs, recs.Length);
    }

    /// <summary>Re-read a material (e.g. after LiveApplied or a harness change).</summary>
    internal static void RefreshMaterial(Material m)
    {
        if (m == null) return;
        if (materialBlockSingle.TryGetValue(m.GetInstanceID(), out var b)) UploadMaterials(b, new[] { m });
    }

    static int Slot(Material m, string prop, int fallback)
    {
        if (m == null || !m.HasProperty(prop)) return fallback;
        var t = m.GetTexture(prop);
        return TextureSlotOf(t, fallback);
    }

    static int TextureSlotOf(Texture t, int fallback)
    {
        if (t == null) return fallback;
        if (t.dimension != TextureDimension.Tex2D) return fallback;
        var id = t.GetInstanceID();
        if (textureSlot.TryGetValue(id, out var s)) return s;
        if (nextTexture >= MaxTextures) return fallback;
        var ptr = t.GetNativeTexturePtr();   // synchronises with the render thread: once per texture per session
        if (ptr == IntPtr.Zero) return fallback;
        s = nextTexture++;
        Native.FRGlassRT_SetTexture(s, ptr);
        textureSlot[id] = s;
        return s;
    }

    static Vector4 Lin(Material m, string prop, Color fallback)
    {
        var c = m != null && m.HasProperty(prop) ? m.GetColor(prop) : fallback;
        var l = c.linear;
        return new Vector4(l.r, l.g, l.b, c.a);
    }

    static float Fl(Material m, string prop, float fallback) => m != null && m.HasProperty(prop) ? m.GetFloat(prop) : fallback;

    internal static bool IsGlassMaterial(Material m)
    {
        if (m == null) return false;
        if (m.shader != null && m.shader.name == "FrontRooms/Glass") return true;
        return m.renderQueue >= 2450 && m.name.IndexOf("glass", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static bool IsReceiverMaterial(Material m) => m != null && m.shader != null && m.shader.name == "FrontRooms/Glass" && m.HasProperty(RTReceiveId) && m.GetFloat(RTReceiveId) > .5f;

    static Native.Material Describe(Material m)
    {
        var r = new Native.Material
        {
            baseColor = new Vector4(.5f, .5f, .5f, 1f), tile = Vector4.one, baseST = new Vector4(1, 1, 0, 0), p0 = new Vector4(.5f, 0f, 1f, 1f),
            kind = Native.KindFlat, texBase = Native.TexWhite, texMask = Native.TexWhite, texBump = Native.TexBump, texMacro = Native.TexGrey,
            texEmission = Native.TexWhite, texOcclusion = Native.TexWhite,
        };
        if (m == null) return r;
        var shader = m.shader != null ? m.shader.name : "";
        Vector4 St(string prop)
        {
            if (!m.HasProperty(prop)) return new Vector4(1, 1, 0, 0);
            var s = m.GetTextureScale(prop); var o = m.GetTextureOffset(prop);
            return new Vector4(s.x, s.y, o.x, o.y);
        }
        if (shader == "FrontRooms/Surface")
        {
            r.kind = Native.KindSurface;
            r.baseColor = Lin(m, "_BaseColor", Color.white);
            var tile = m.HasProperty("_TileSize") ? m.GetVector("_TileSize") : new Vector4(1, 1, 0, 0);
            r.tile = new Vector4(tile.x, tile.y, 0, 0);
            r.baseST = St("_BaseMap");
            r.stain = Lin(m, "_StainColor", new Color(.42f, .31f, .16f));
            r.stain.w = Fl(m, "_StainStrength", 0f);
            r.p0 = new Vector4(Fl(m, "_Smoothness", 1f), Fl(m, "_Metallic", 0f), Fl(m, "_OcclusionStrength", .8f), Fl(m, "_BumpScale", 1f));
            r.p1 = new Vector4(Fl(m, "_MacroTone", .35f), Fl(m, "_MacroDirt", .25f), Fl(m, "_WetStrength", 0f), Fl(m, "_FloorGrime", 0f));
            r.p2 = new Vector4(Fl(m, "_FloorGrimeHeight", .35f), Fl(m, "_CeilingGrime", 0f), Fl(m, "_CeilingHeight", 2.9f), 0f);
            r.texBase = (uint)Slot(m, "_BaseMap", Native.TexWhite);
            r.texMask = (uint)Slot(m, "_MaskMap", Native.TexWhite);
            r.texBump = (uint)Slot(m, "_BumpMap", Native.TexBump);
            r.texMacro = (uint)Slot(m, "_MacroMap", Native.TexGrey);
            if (m.IsKeywordEnabled("_FR_MESH_UV")) r.flags |= Native.MatMeshUV;
            if (m.IsKeywordEnabled("_EMISSION"))
            {
                r.flags |= Native.MatEmission;
                r.emission = Lin(m, "_EmissionColor", Color.black);
                r.texEmission = (uint)Slot(m, "_EmissionMap", Native.TexWhite);
            }
            return r;
        }
        if (IsGlassMaterial(m))
        {
            r.kind = Native.KindGlass;
            r.baseColor = Lin(m, "_BaseColor", new Color(.15f, .17f, .16f));
            r.p0 = new Vector4(Fl(m, "_Smoothness", .96f), 0f, 1f, 1f);
            return r;
        }
        if (m.HasProperty("_BaseMap") || m.HasProperty("_MainTex") || shader.StartsWith("Universal Render Pipeline/"))
        {
            // URP Lit / Simple Lit / Unlit-like: metallic workflow (LitInput.hlsl InitializeStandardLitSurfaceData)
            r.kind = Native.KindLit;
            var baseProp = m.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            r.baseColor = m.HasProperty("_BaseColor") ? Lin(m, "_BaseColor", Color.white) : Lin(m, "_Color", Color.white);
            r.baseST = St(baseProp);
            r.texBase = (uint)Slot(m, baseProp, Native.TexWhite);
            var smooth = m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness") : Fl(m, "_Glossiness", .5f);
            r.p0 = new Vector4(smooth, Fl(m, "_Metallic", 0f), Fl(m, "_OcclusionStrength", 1f), Fl(m, "_BumpScale", 1f));
            if (m.IsKeywordEnabled("_METALLICSPECGLOSSMAP")) { r.flags |= Native.MatMetallicMap; r.texMask = (uint)Slot(m, "_MetallicGlossMap", Native.TexWhite); }
            if (m.IsKeywordEnabled("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A")) r.flags |= Native.MatSmoothAlbedo;
            if (m.IsKeywordEnabled("_NORMALMAP")) { r.flags |= Native.MatNormalMap; r.texBump = (uint)Slot(m, "_BumpMap", Native.TexBump); }
            if (m.IsKeywordEnabled("_OCCLUSIONMAP")) { r.flags |= Native.MatOcclusionMap; r.texOcclusion = (uint)Slot(m, "_OcclusionMap", Native.TexWhite); }
            if (m.IsKeywordEnabled("_EMISSION") || (m.HasProperty("_EmissionColor") && m.GetColor("_EmissionColor").maxColorComponent > 0f))
            {
                r.flags |= Native.MatEmission;
                r.emission = Lin(m, "_EmissionColor", Color.black);
                r.texEmission = (uint)Slot(m, "_EmissionMap", Native.TexWhite);
            }
            if (shader.Contains("Unlit")) { r.flags |= Native.MatEmission; r.emission = r.baseColor; r.texEmission = r.texBase; r.baseColor = Vector4.zero; }
            return r;
        }
        r.baseColor = m.HasProperty("_BaseColor") ? Lin(m, "_BaseColor", Color.grey) : Lin(m, "_Color", Color.grey);
        return r;
    }

    // ------------------------------------------------------------------ registry: instances
    sealed class Inst
    {
        public int handle, rendererId;
        public Renderer r;
        public Transform t;
        public GameObject go;
        public MeshEntry mesh;
        public int meshKeyOverride = -1;   // fracture pieces: a PrepareMeshes key
        public uint materialBase, flags, mask;
        public int windowId = -1;
        public Vector4 r0, r1, r2;
        public Bounds bounds;
        public int materialId;              // receivers / glass: watched for a material swap
        public bool enabled = true, alive = true, manual, dynamic, priority, lens, receiver;
        public int layer;
        public ChunkRec chunk;
        public Vector4 emission;           // xyz emission (lens, linear), w ceiling height
        public int prepassBit = -1;
        public float d2;
    }

    sealed class ChunkRec
    {
        public Transform root;
        public int id, childCount = -1, stamp;
        public bool dirty = true, queued;
        public float lateRescanAt;
        public readonly Dictionary<int, Inst> byRenderer = new Dictionary<int, Inst>();
        public readonly List<LampRec> lights = new List<LampRec>();
        public Vector3 lightCenter;
        public float lightRadius;
    }

    // Static data of a chunk's light, cached at (re)scan: map lights never move relative to their chunk and only
    // their enabled state, intensity and shadows change per frame (MapWorld.TickFixtures).
    sealed class LampRec
    {
        public Light light;
        public Vector3 pos, fwd;
        public LightType type;
        public float invRange2, invAngleRange, angleAdd, shadowStrength;
        public Color colorLinear;
    }

    static LampRec MakeLamp(Light l)
    {
        var rec = new LampRec { light = l, pos = l.transform.position, fwd = l.transform.forward, type = l.type, shadowStrength = l.shadowStrength };
        var range = Mathf.Max(l.range, 1e-3f);
        rec.invRange2 = 1f / (range * range);
        var c = l.color.linear;
        if (GraphicsSettings.lightsUseColorTemperature && l.useColorTemperature) c *= Mathf.CorrelatedColorTemperatureToRGB(l.colorTemperature);
        rec.colorLinear = c;
        if (l.type == LightType.Spot)
        {
            var cosOuter = Mathf.Cos(Mathf.Deg2Rad * l.spotAngle * .5f);
            var cosInner = Mathf.Cos(Mathf.Deg2Rad * l.innerSpotAngle * .5f);
            rec.invAngleRange = 1f / Mathf.Max(.001f, cosInner - cosOuter);
            rec.angleAdd = -cosOuter * rec.invAngleRange;
        }
        return rec;
    }

    static float SqrDist(Bounds b, Vector3 p)
    {
        var c = b.center; var e = b.extents;
        var dx = Mathf.Max(Mathf.Abs(p.x - c.x) - e.x, 0f);
        var dy = Mathf.Max(Mathf.Abs(p.y - c.y) - e.y, 0f);
        var dz = Mathf.Max(Mathf.Abs(p.z - c.z) - e.z, 0f);
        return dx * dx + dy * dy + dz * dz;
    }

    static readonly List<Inst> insts = new List<Inst>();
    static readonly Stack<int> freeHandles = new Stack<int>();
    static readonly List<Inst> watch = new List<Inst>();          // receivers, glass, dynamic, manual: checked every frame
    static readonly Dictionary<int, ChunkRec> chunks = new Dictionary<int, ChunkRec>();
    static readonly Queue<ChunkRec> rescanQueue = new Queue<ChunkRec>();
    static int liveCount, chunkStamp, staticCursor;
    static FrontRoomsMapWorld map;
    static Transform mapRoot;
    static Vector3 mapRootPos;
    static float nextMapSearch, nextDirSearch;
    static Light dirLight;
    static Transform relayRoot;
    static bool warnedMarker;
    static int lastSceneFrame = -1;
    static readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
    static readonly List<MeshRenderer> scratchRenderers = new List<MeshRenderer>();
    static readonly List<Light> scratchLights = new List<Light>();
    static readonly HashSet<int> scratchSeen = new HashSet<int>();

    static void Warn(string s) => Debug.LogWarning("[FrontRoomsGlassRT] " + s);

    static Inst NewInst()
    {
        var h = freeHandles.Count > 0 ? freeHandles.Pop() : insts.Count;
        var i = new Inst { handle = h };
        if (h == insts.Count) insts.Add(i); else insts[h] = i;
        liveCount++;
        return i;
    }

    static void RowsFrom(Inst i, Matrix4x4 m)
    {
        i.r0 = m.GetRow(0); i.r1 = m.GetRow(1); i.r2 = m.GetRow(2);
    }

    static Inst RegisterRenderer(Renderer r, FrontRoomsGlassRTFlags flags, int windowId, ChunkRec chunk, bool manual, int meshKeyOverride = -1)
    {
        if (r == null) return null;
        if (!(r is MeshRenderer)) return null;   // particles, trails, lines and skinned meshes are not traced (P0)
        if (r.isPartOfStaticBatch) return null;  // R32
        if (r.shadowCastingMode == ShadowCastingMode.ShadowsOnly) return null;
        var mats = r.sharedMaterials;
        var first = mats != null && mats.Length > 0 ? mats[0] : null;
        var glass = IsGlassMaterial(first) || (flags & FrontRoomsGlassRTFlags.Glass) != 0;
        if (!glass && first != null && first.renderQueue >= 2450) return null;   // other transparents (beams, decals): not traced
        var lod = r.GetComponentInParent<LODGroup>();
        if (lod != null && !InLod0(lod, r)) return null;

        MeshEntry mesh = null;
        if (meshKeyOverride < 0)
        {
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return null;
            mesh = AcquireMesh(mf.sharedMesh, (flags & FrontRoomsGlassRTFlags.Deforming) != 0);
            if (mesh == null) return null;
        }
        var inst = NewInst();
        inst.r = r; inst.rendererId = r.GetInstanceID(); inst.t = r.transform; inst.go = r.gameObject; inst.mesh = mesh; inst.meshKeyOverride = meshKeyOverride;
        inst.windowId = windowId; inst.chunk = chunk; inst.manual = manual; inst.layer = inst.go.layer;
        var geometry = mesh != null ? mesh.geometrySubmesh : new[] { 0 };
        inst.materialBase = (uint)MaterialBlock(mats, geometry);
        inst.materialId = first != null ? first.GetInstanceID() : 0;

        if (glass) flags |= FrontRoomsGlassRTFlags.Glass;
        if (IsReceiverMaterial(first)) flags |= FrontRoomsGlassRTFlags.Receiver;
        else if (r.GetComponent<FrontRoomsMetalGlassTarget>() != null && !warnedMarker && glass)
        {
            warnedMarker = true;
            Debug.Log("[FrontRoomsGlassRT] a FrontRoomsMetalGlassTarget pane uses " + (first != null ? first.name + " (" + first.shader.name + ")" : "no material")
                      + ", which has no RT input: it is traced as glass but receives no RT reflection until it uses Glass_Window (_RTReceive = 1).");
        }
        inst.flags = (uint)flags;
        inst.receiver = (flags & FrontRoomsGlassRTFlags.Receiver) != 0;
        inst.lens = (flags & FrontRoomsGlassRTFlags.EmissiveLens) != 0;
        inst.dynamic = (flags & (FrontRoomsGlassRTFlags.Dynamic | FrontRoomsGlassRTFlags.Relay | FrontRoomsGlassRTFlags.Door)) != 0 || manual;
        inst.priority = inst.receiver || glass || inst.dynamic || manual;
        inst.mask = (flags & FrontRoomsGlassRTFlags.Shard) != 0 ? Native.MaskShard
                  : glass ? Native.MaskGlass
                  : (flags & FrontRoomsGlassRTFlags.ReflectionOnly) != 0 ? Native.MaskReflectionOnly
                  : inst.dynamic ? Native.MaskDynamic : Native.MaskOpaque;
        RowsFrom(inst, r.localToWorldMatrix);
        inst.bounds = r.bounds;
        if (first != null && first.HasProperty(CeilingHeightId) && r.HasPropertyBlock())
        {
            r.GetPropertyBlock(block);
            inst.emission.w = block.GetFloat(CeilingHeightId);
        }
        if (inst.lens) ReadLensEmission(inst);
        if (inst.receiver) SetPrepassBit(inst, IsReceiverMaterial(first) ? PrepassBit : OverrideBit);
        if (inst.priority) watch.Add(inst);
        return inst;
    }

    static bool InLod0(LODGroup group, Renderer r)
    {
        var lods = group.GetLODs();
        if (lods.Length == 0) return true;
        foreach (var x in lods[0].renderers) if (x == r) return true;
        for (var k = 1; k < lods.Length; k++) foreach (var x in lods[k].renderers) if (x == r) return false;
        return true;
    }

    static void SetPrepassBit(Inst i, int bit)
    {
        ClearPrepassBit(i);
        if (i.r == null) return;
        i.r.renderingLayerMask |= 1u << bit;
        i.prepassBit = bit;
    }

    static void ClearPrepassBit(Inst i)
    {
        if (i.prepassBit < 0) return;
        if (i.r != null) i.r.renderingLayerMask &= ~(1u << i.prepassBit);
        i.prepassBit = -1;
    }

    static void ReadLensEmission(Inst i)
    {
        if (i.r == null) return;
        Color c;
        if (i.r.HasPropertyBlock())
        {
            i.r.GetPropertyBlock(block);
            c = block.GetColor(EmissionColorId);   // the value as passed: the GPU sees its sRGB->linear conversion
        }
        else
        {
            var m = i.r.sharedMaterial;
            c = m != null && m.HasProperty(EmissionColorId) ? m.GetColor(EmissionColorId) : Color.black;
        }
        var l = c.linear;
        i.emission.x = l.r; i.emission.y = l.g; i.emission.z = l.b;
    }

    static void RemoveInst(Inst i)
    {
        if (i == null || !i.alive) return;
        i.alive = false;
        if (i.chunk != null && i.chunk.byRenderer.TryGetValue(i.rendererId, out var same) && same == i) i.chunk.byRenderer.Remove(i.rendererId);
        ClearPrepassBit(i);
        ReleaseMeshRef(i.mesh);
        i.mesh = null;
        if (i.priority) watch.Remove(i);
        insts[i.handle] = null;
        freeHandles.Push(i.handle);
        liveCount--;
    }

    // ------------------------------------------------------------------ public API (manual registration)
    public static int Register(Renderer renderer, FrontRoomsGlassRTFlags flags, int windowId)
    {
        EnsureStarted();
        if (!nativeOk) return -1;
        var i = RegisterRenderer(renderer, flags, windowId, null, true);
        return i != null ? i.handle : -1;
    }

    public static void Unregister(int handle)
    {
        if (handle < 0 || handle >= insts.Count) return;
        RemoveInst(insts[handle]);
    }

    public static void SetEnabled(int handle, bool enabled)
    {
        if (handle < 0 || handle >= insts.Count || insts[handle] == null) return;
        var i = insts[handle];
        i.enabled = enabled;
        if (i.receiver)
        {
            if (enabled && i.prepassBit < 0) SetPrepassBit(i, IsReceiverMaterial(i.r != null ? i.r.sharedMaterial : null) ? PrepassBit : OverrideBit);
            else if (!enabled) ClearPrepassBit(i);
        }
    }

    sealed class Ticket { public int firstKey, count; public bool failed; }
    static readonly List<Ticket> tickets = new List<Ticket>();

    public static int PrepareMeshes(Mesh source, FrontRoomsGlassRTRange[] pieces, int notBeforeFrame)
    {
        EnsureStarted();
        if (!nativeOk || source == null || pieces == null || pieces.Length == 0 || !source.isReadable) return -1;
        var verts = source.vertices;
        var normals = source.normals;
        var uvs = source.uv;
        var colors = source.colors;
        var n = verts.Length;
        var attrs = new Native.VertexAttr[n];
        for (var v = 0; v < n; v++)
        {
            var a = new Native.VertexAttr { ny = 1f };
            if (normals.Length == n) { a.nx = normals[v].x; a.ny = normals[v].y; a.nz = normals[v].z; }
            if (uvs.Length == n) { a.u = uvs[v].x; a.v = uvs[v].y; }
            if (colors.Length == n) a.colorR = colors[v].r;
            attrs[v] = a;
        }
        // indexStart / indexCount address submesh 0's index list; baseVertex is added to each index of the range
        var idx = source.GetIndices(0, false);
        var outIdx = new int[idx.Length];
        var starts = new int[pieces.Length];
        var counts = new int[pieces.Length];
        for (var k = 0; k < pieces.Length; k++)
        {
            var p = pieces[k];
            starts[k] = p.indexStart;
            counts[k] = p.indexCount - p.indexCount % 3;
            for (var j = 0; j < counts[k] && p.indexStart + j < idx.Length; j++) outIdx[p.indexStart + j] = idx[p.indexStart + j] + p.baseVertex;
        }
        var t = new Ticket { firstKey = nextMeshKey, count = pieces.Length };
        nextMeshKey += pieces.Length;
        var r = Native.FRGlassRT_UploadMeshRanges(t.firstKey, verts, attrs, n, outIdx, outIdx.Length, starts, counts, pieces.Length, notBeforeFrame);
        t.failed = r != 0;
        tickets.Add(t);
        return tickets.Count - 1;
    }

    public static bool IsPrepared(int ticket)
    {
        if (ticket < 0 || ticket >= tickets.Count || tickets[ticket].failed || !nativeOk) return false;
        var t = tickets[ticket];
        var keys = new int[t.count];
        var states = new int[t.count];
        for (var k = 0; k < t.count; k++) keys[k] = t.firstKey + k;
        Native.FRGlassRT_MeshStates(keys, keys.Length, states);
        foreach (var s in states) if (s < 2) return false;
        return true;
    }

    public static int RegisterPiece(int ticket, int piece, Renderer renderer, FrontRoomsGlassRTFlags flags, int windowId)
    {
        if (ticket < 0 || ticket >= tickets.Count || piece < 0 || piece >= tickets[ticket].count) return -1;
        var i = RegisterRenderer(renderer, flags | FrontRoomsGlassRTFlags.Dynamic, windowId, null, true, tickets[ticket].firstKey + piece);
        return i != null ? i.handle : -1;
    }

    public static void MeshChanged(Mesh mesh)
    {
        if (mesh == null || !meshById.TryGetValue(mesh.GetInstanceID(), out var e) || e.failed) return;
        Upload(e);   // same key: the plugin replaces the old BLAS (generation-safe) and builds the new one this frame
    }

    public static void RefitMesh(Mesh mesh)
    {
        if (mesh == null || !meshById.TryGetValue(mesh.GetInstanceID(), out var e) || !e.deforming || !mesh.isReadable) return;
        var v = mesh.vertices;
        Native.FRGlassRT_RefitMesh(e.key, v, v.Length);
    }

    public static void GlassBroken(Vector3 at)
    {
        foreach (var i in watch.ToArray())
            if (i.receiver && i.alive && (i.bounds.SqrDistance(at) < 1f || (i.bounds.center - at).sqrMagnitude < 1f)) RemoveInst(i);
    }

    // ------------------------------------------------------------------ scene update (once per frame)
    static readonly Stopwatch mainWatch = new Stopwatch();
    static float mainMs, mainMsMax;
    static int lastCandidates, lastLamps;

    /// <summary>Once per frame: find the map, diff its chunks, slice registrations, watch materials.</summary>
    internal static void UpdateScene(bool force = false)
    {
        if (!force && lastSceneFrame == Time.frameCount) return;
        lastSceneFrame = Time.frameCount;
        var now = Time.realtimeSinceStartup;
        if (map == null)
        {
            if (mapRoot != null || chunks.Count > 0) OnMapLost();
            if (now >= nextMapSearch)
            {
                nextMapSearch = now + 1f;
                map = Object.FindAnyObjectByType<FrontRoomsMapWorld>();
                if (map != null) OnMapFound();
            }
        }
        if (map != null)
        {
            if (mapRoot.position != mapRootPos) Rebase(mapRoot.position - mapRootPos);
            DiffChunks(now);
            var budget = Stopwatch.StartNew();
            while (rescanQueue.Count > 0 && budget.Elapsed.TotalMilliseconds < RegisterBudgetMs)
            {
                var c = rescanQueue.Dequeue();
                c.queued = false;
                if (c.root == null) continue;
                Rescan(c, budget);
            }
        }
        if (dirLight == null && now >= nextDirSearch)
        {
            nextDirSearch = now + 5f;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional && l.isActiveAndEnabled) { dirLight = l; break; }
        }
        RefreshStatics();
    }

    static void OnMapFound()
    {
        mapRoot = map.transform;
        mapRootPos = mapRoot.position;
        map.GlassBroken += GlassBroken;
        // the Relay: 16 rigid MeshRenderers (02 §7), registered as Dynamic | Relay; liveness is checked every frame
        var rig = Object.FindAnyObjectByType<FrontRoomsRelayRig>(FindObjectsInactive.Include);
        if (rig != null && relayRoot == null)
        {
            relayRoot = rig.transform;
            foreach (var r in rig.GetComponentsInChildren<MeshRenderer>(true))
                RegisterRenderer(r, FrontRoomsGlassRTFlags.Dynamic | FrontRoomsGlassRTFlags.Relay, -1, null, false);
        }
        Debug.Log("[FrontRoomsGlassRT] map found: " + map.name + " · Relay parts " + (relayRoot != null ? relayRoot.GetComponentsInChildren<MeshRenderer>(true).Length : 0));
    }

    static void OnMapLost()
    {
        foreach (var c in chunks.Values) DropChunk(c);
        chunks.Clear();
        rescanQueue.Clear();
        mapRoot = null;
        map = null;
    }

    static void Rebase(Vector3 delta)
    {
        mapRootPos = mapRoot.position;
        foreach (var i in insts)
        {
            if (i == null || i.dynamic || i.chunk == null) continue;
            i.r0.w += delta.x; i.r1.w += delta.y; i.r2.w += delta.z;
            i.bounds.center += delta;
        }
        foreach (var c in chunks.Values)
        {
            foreach (var l in c.lights) l.pos += delta;
            c.lightCenter += delta;
        }
    }

    static void DiffChunks(float now)
    {
        chunkStamp++;
        var n = mapRoot.childCount;
        for (var k = 0; k < n; k++)
        {
            var root = mapRoot.GetChild(k);
            var id = root.GetInstanceID();
            if (!chunks.TryGetValue(id, out var c))
            {
                c = new ChunkRec { root = root, id = id, lateRescanAt = now + 1f };
                chunks[id] = c;
            }
            c.stamp = chunkStamp;
            var cc = root.childCount;
            if (cc != c.childCount) { c.childCount = cc; c.dirty = true; }
            if (c.lateRescanAt > 0f && now >= c.lateRescanAt) { c.lateRescanAt = 0f; c.dirty = true; }   // catch the room dressing
            if (c.dirty && !c.queued) { c.queued = true; c.dirty = false; rescanQueue.Enqueue(c); }
        }
        if (chunks.Count > n)
        {
            List<int> gone = null;
            foreach (var kv in chunks) if (kv.Value.stamp != chunkStamp) (gone ??= new List<int>()).Add(kv.Key);
            if (gone != null) foreach (var id in gone) { DropChunk(chunks[id]); chunks.Remove(id); }
        }
    }

    static void DropChunk(ChunkRec c)
    {
        foreach (var i in c.byRenderer.Values) RemoveInst(i);
        c.byRenderer.Clear();
        c.lights.Clear();
        c.root = null;
    }

    static void Rescan(ChunkRec c, Stopwatch budget)
    {
        scratchRenderers.Clear();
        c.root.GetComponentsInChildren(true, scratchRenderers);
        scratchSeen.Clear();
        foreach (var r in scratchRenderers) scratchSeen.Add(r.GetInstanceID());
        // removed renderers
        List<int> gone = null;
        foreach (var kv in c.byRenderer) if (!scratchSeen.Contains(kv.Key) || kv.Value.r == null) (gone ??= new List<int>()).Add(kv.Key);
        if (gone != null) foreach (var id in gone) { RemoveInst(c.byRenderer[id]); c.byRenderer.Remove(id); }
        // new renderers
        var requeue = false;
        foreach (var r in scratchRenderers)
        {
            var id = r.GetInstanceID();
            if (c.byRenderer.ContainsKey(id)) continue;
            if (!r.enabled || !r.gameObject.activeInHierarchy || r.forceRenderingOff) continue;
            if (budget.Elapsed.TotalMilliseconds > RegisterBudgetMs * 4f) { requeue = true; break; }
            var inst = RegisterRenderer(r, Classify(r, c.root), -1, c, false);
            if (inst != null) c.byRenderer[id] = inst;
        }
        // lights of this chunk (positions cached: chunk roots only translate)
        scratchLights.Clear();
        c.root.GetComponentsInChildren(true, scratchLights);
        c.lights.Clear();
        var lc = Vector3.zero;
        foreach (var l in scratchLights) if (l.type != LightType.Directional) { var rec = MakeLamp(l); c.lights.Add(rec); lc += rec.pos; }
        c.lightCenter = c.lights.Count > 0 ? lc / c.lights.Count : c.root.position;
        c.lightRadius = 0f;
        foreach (var rec in c.lights) c.lightRadius = Mathf.Max(c.lightRadius, (rec.pos - c.lightCenter).magnitude);
        if (requeue && !c.queued) { c.queued = true; rescanQueue.Enqueue(c); }
    }

    // P0 classification without MapWorld edits (P2 replaces it with the ChunkHandles payload, design §2.4 C5)
    static FrontRoomsGlassRTFlags Classify(Renderer r, Transform chunkRoot)
    {
        var f = FrontRoomsGlassRTFlags.None;
        var name = r.gameObject.name;
        if (name == "Lens") f |= FrontRoomsGlassRTFlags.EmissiveLens;
        if (name.StartsWith("Key", StringComparison.Ordinal)) f |= FrontRoomsGlassRTFlags.Dynamic;
        for (var t = r.transform.parent; t != null && t != chunkRoot; t = t.parent)
            if (t.name.StartsWith("Door hinge", StringComparison.Ordinal)) { f |= FrontRoomsGlassRTFlags.Dynamic | FrontRoomsGlassRTFlags.Door; break; }
        if (r.GetComponent<FrontRoomsMetalGlassTarget>() != null) f |= FrontRoomsGlassRTFlags.Glass;
        return f;
    }

    // Static instances: a slow round-robin re-read (props pushed or moved by other code), plus liveness.
    static void RefreshStatics()
    {
        if (insts.Count == 0) return;
        for (var k = 0; k < StaticRefreshPerFrame; k++)
        {
            staticCursor = (staticCursor + 1) % insts.Count;
            var i = insts[staticCursor];
            if (i == null || i.priority) continue;
            if (i.r == null) { RemoveInst(i); continue; }
            RowsFrom(i, i.r.localToWorldMatrix);
            i.bounds = i.r.bounds;
        }
    }

    // ------------------------------------------------------------------ per camera: frame packet
    static Native.InstanceIn[] instBuffer = new Native.InstanceIn[4096];
    static readonly Native.Lamp[] lampBuffer = new Native.Lamp[MaxLamps + 1];
    static readonly List<Inst> candidates = new List<Inst>(8192);
    static readonly List<Inst> others = new List<Inst>(8192);
    static readonly List<(float d2, LampRec rec)> lampCandidates = new List<(float, LampRec)>(512);
    static readonly int[] histogram = new int[64];
    static readonly Plane[] frustum = new Plane[6];
    static readonly Dictionary<int, IntPtr> cubePtr = new Dictionary<int, IntPtr>();
    static int lastReceiversVisible;
    static uint frameCounter;

    internal struct FrameResult
    {
        public ulong seq;
        public RectInt rect;
        public int visibleReceivers;
        public bool traced;
    }

    /// <summary>Build and submit one camera's frame. Main thread, inside URP's record of that camera.</summary>
    internal static FrameResult BuildFrame(Camera cam, FrontRoomsGlassRTCamera state, int width, int height, Matrix4x4 view, Matrix4x4 gpuProjFlipped, bool forceScene = false)
    {
        mainWatch.Restart();
        var result = new FrameResult();
        var quality = Quality;
        CheckNative();
        if (!nativeOk || quality == FrontRoomsGlassRTQuality.Off || WebGLTargetInEditor || PermanentlyOff) { Finish(); return result; }
        UpdateScene(forceScene);   // also while the kernel compiles, so the scene is warm when RT turns on
        Mark(0);
        if (!Supported) { Finish(); return result; }

        var camPos = cam.transform.position;
        var far = cam.farClipPlane;
        var far2 = far * far;
        GeometryUtility.CalculateFrustumPlanes(cam, frustum);
        var viewProjUnflipped = cam.projectionMatrix * view;

        // ---- same-frame liveness of watched instances (receivers, glass, dynamic, manual) and visible receivers
        int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
        var visible = 0;
        var fullScreen = false;
        var stagger = (int)(frameCounter & 7);
        for (var k = watch.Count - 1; k >= 0; k--)
        {
            var i = watch[k];
            var glassOrReceiver = i.receiver || (i.flags & (uint)FrontRoomsGlassRTFlags.Glass) != 0;
            // far dynamic instances (door leaves, keys) are re-read every 8th frame; receivers, glass, manual and
            // near dynamic instances every frame (a broken pane or a moving piece must be right on this frame)
            if (!glassOrReceiver && !i.manual && (i.handle & 7) != stagger && i.d2 >= 0f && SqrDist(i.bounds, camPos) > NearDynamic2) continue;
            if (i.r == null) { RemoveInst(i); continue; }
            var on = i.enabled && i.r.enabled && i.go.activeInHierarchy && !i.r.forceRenderingOff;
            i.d2 = on ? 0f : -1f;
            if (!on) continue;
            if (i.dynamic) { RowsFrom(i, i.r.localToWorldMatrix); i.bounds = i.r.bounds; }
            if (glassOrReceiver)
            {
                var m = i.r.sharedMaterial;
                var mid = m != null ? m.GetInstanceID() : 0;
                if (mid != i.materialId) Rematerial(i, m);
            }
            if (!i.receiver || (cam.cullingMask & (1 << i.layer)) == 0) continue;
            if (!GeometryUtility.TestPlanesAABB(frustum, i.bounds)) continue;
            visible++;
            if (!fullScreen && !ProjectBounds(i.bounds, viewProjUnflipped, width, height, ref x0, ref y0, ref x1, ref y1)) fullScreen = true;
        }
        result.visibleReceivers = visible;
        lastReceiversVisible = visible;
        if (DebugMode == Native.DebugParity) { fullScreen = true; visible = Mathf.Max(visible, 1); }   // parity traces the whole screen
        if (visible == 0) { Finish(); return result; }
        if (fullScreen) { x0 = 0; y0 = 0; x1 = width; y1 = height; }
        x0 = Mathf.Clamp(x0 - 2, 0, width); y0 = Mathf.Clamp(y0 - 2, 0, height);
        x1 = Mathf.Clamp(x1 + 2, 0, width); y1 = Mathf.Clamp(y1 + 2, 0, height);
        if (x1 <= x0 || y1 <= y0) { Finish(); return result; }
        result.rect = new RectInt(x0, y0, x1 - x0, y1 - y0);

        Mark(1);
        // ---- TLAS candidates around the camera: priority class first, then nearest (bounds distance, never the centre)
        var cap = quality == FrontRoomsGlassRTQuality.Ultra ? 4096 : 2048;
        candidates.Clear();
        others.Clear();
        foreach (var i in insts)
        {
            if (i == null || !i.enabled) continue;
            if ((cam.cullingMask & (1 << i.layer)) == 0) continue;
            if (i.priority)
            {
                if (i.d2 < 0f) continue;   // watched and dead / hidden this frame
                if (SqrDist(i.bounds, camPos) > far2) continue;
                candidates.Add(i);
                continue;
            }
            var d2 = SqrDist(i.bounds, camPos);
            if (d2 > far2) continue;
            i.d2 = d2;
            others.Add(i);
        }
        var priorityCount = candidates.Count;
        candidates.AddRange(others);
        var selected = SelectNearest(priorityCount, cap, far2);
        if (instBuffer.Length < selected) instBuffer = new Native.InstanceIn[Mathf.NextPowerOfTwo(selected)];
        uint signature = 2166136261u;
        var farLensReads = 0;
        var n = 0;
        var recordOrder = DebugMode == Native.DebugHitId;
        if (recordOrder) SubmittedRenderers.Clear();
        for (var k = 0; k < candidates.Count && n < selected; k++)
        {
            var i = candidates[k];
            if (k >= priorityCount && i.d2 < 0f) continue;   // dropped by SelectNearest
            if (i.lens && (i.d2 < NearLens2 || (farLensReads < 48 && ((i.handle + (int)frameCounter) & 15) == 0 && ++farLensReads > 0))) ReadLensEmission(i);
            var key = i.meshKeyOverride >= 0 ? i.meshKeyOverride : i.mesh != null ? i.mesh.key : -1;
            if (key < 0) continue;
            if (recordOrder) SubmittedRenderers.Add(i.r);
            instBuffer[n++] = new Native.InstanceIn
            {
                o2w0 = i.r0, o2w1 = i.r1, o2w2 = i.r2, meshKey = key, materialBase = i.materialBase, flags = i.flags,
                windowId = (uint)i.windowId, emission = i.emission, mask = i.mask,
            };
            if (i.dynamic) signature = Hash(signature, i.r0, i.r1, i.r2);
            if (i.lens && i.d2 < NearLens2) signature = Hash(signature, i.emission);
        }
        lastCandidates = n;

        Mark(2);
        // ---- lamps on this frame (the raster's lights), nearest first, cap 128; the directional fill last
        var lampCount = BuildLamps(camPos, quality, ref signature);
        lastLamps = lampCount;

        Mark(3);
        // ---- temporal accumulation while nothing moves
        var viewProj = gpuProjFlipped * view;
        var still = AllowAccumulation && state.frameIndex > 0 && viewProj == state.lastViewProj && signature == state.lastSignature && DebugMode == Native.DebugNone;
        state.accumulation = still ? Mathf.Min(state.accumulation + 1, AccumulationCap) : 0;
        state.lastViewProj = viewProj;
        state.lastSignature = signature;
        state.frameIndex++;

        // ---- frame constants
        var f = new Native.Frame();
        f.invViewProj = viewProj.inverse;
        f.viewProj = viewProj;
        f.camPos = new Vector4(camPos.x, camPos.y, camPos.z, cam.nearClipPlane);
        var fwd = cam.transform.forward;
        f.camFwd = new Vector4(fwd.x, fwd.y, fwd.z, far);
        f.screen = new Vector4(width, height, 1f / width, 1f / height);
        f.rectX0 = (uint)x0; f.rectY0 = (uint)y0; f.rectX1 = (uint)x1; f.rectY1 = (uint)y1;
        var fogOn = RenderSettings.fog && RenderSettings.fogMode == FogMode.ExponentialSquared;
        var spread = 2f * Mathf.Tan(.5f * cam.fieldOfView * Mathf.Deg2Rad) / height;
        f.fog = new Vector4(RenderSettings.fogDensity * 1.2011224f, fogOn ? 1f : 0f, spread, Time.time);
        var fc = RenderSettings.fogColor.linear;
        f.fogColor = new Vector4(fc.r, fc.g, fc.b, 1f);
        AmbientSH(ref f);
        var env = EnvCube(out var envValid);
        f.envDecode = ReflectionProbe.defaultTextureHDRDecodeValues;
        f.envInfo = new Vector4(6f, envValid ? 1f : 0f, 1f, 0f);
        f.lampCount = (uint)lampCount;
        f.quality = (uint)quality;
        f.debugMode = DebugMode;
        f.instanceCount = (uint)n;
        f.maxShadowLamps = (uint)(quality == FrontRoomsGlassRTQuality.Ultra ? UltraShadowLamps : HighShadowLamps);
        f.glassLayers = quality == FrontRoomsGlassRTQuality.Ultra ? 2u : 1u;
        f.frameIndex = frameCounter++;
        f.accumulationIndex = (uint)state.accumulation;
        var acc = state.accumulation;
        f.jitter = acc > 0 ? new Vector4(Halton(acc, 2) - .5f, Halton(acc, 3) - .5f, 1f / (acc + 1), 1f) : Vector4.zero;
        f.thin = new Vector4(.006f, 1.52f, .002f, BackSurfaceImage ? 1f : 0f);
        f.blur = new Vector4(24f * height / 1440f, quality == FrontRoomsGlassRTQuality.Ultra ? EdgeFilterUltra : EdgeFilterHigh, 4f,
                             quality == FrontRoomsGlassRTQuality.Ultra ? .15f : 0f);
        f.fill = new Vector4(0f, 0f, 0f, -1f);

        result.seq = Native.FRGlassRT_SubmitFrame(state.id, Time.frameCount, ref f, instBuffer, n, lampBuffer, lampCount, env);
        Mark(4);
        result.traced = result.seq != 0;
        Finish();
        return result;
    }

    /// <summary>No trace this frame: the plugin event still applies queued ops and builds BLASes (warm-up packet).</summary>
    internal static ulong Warmup()
    {
        CheckNative();
        if (!nativeOk || PermanentlyOff) return 0;
        return Native.FRGlassRT_SubmitWarmup(Time.frameCount);
    }

    /// <summary>Main-thread ms of the last traced frame: scene diff/registration, liveness+receivers, TLAS candidates,
    /// lamps, constants+submit (harness / report).</summary>
    internal static readonly float[] Breakdown = new float[5];
    static double markLast;
    static void Mark(int k)
    {
        var now = mainWatch.Elapsed.TotalMilliseconds;
        Breakdown[k] = (float)(now - (k == 0 ? 0 : markLast));
        markLast = now;
    }

    static void Finish()
    {
        mainWatch.Stop();
        mainMs = (float)mainWatch.Elapsed.TotalMilliseconds;
        mainMsMax = Mathf.Max(mainMsMax, mainMs);
    }

    internal static void ResetMaxima() { mainMsMax = 0f; if (nativeOk) Native.FRGlassRT_ResetStatMax(); }

    static void Rematerial(Inst i, Material m)
    {
        i.materialId = m != null ? m.GetInstanceID() : 0;
        var geometry = i.mesh != null ? i.mesh.geometrySubmesh : new[] { 0 };
        i.materialBase = (uint)MaterialBlock(i.r.sharedMaterials, geometry);
        var receiver = IsReceiverMaterial(m);
        var flags = (FrontRoomsGlassRTFlags)i.flags;
        if (receiver) flags |= FrontRoomsGlassRTFlags.Receiver | FrontRoomsGlassRTFlags.Glass; else flags &= ~FrontRoomsGlassRTFlags.Receiver;
        i.flags = (uint)flags;
        i.receiver = receiver;
        if (receiver) SetPrepassBit(i, PrepassBit); else ClearPrepassBit(i);
    }

    // Selects the nearest non-priority candidates so that the total stays within cap. Marks dropped ones with d2 = -1.
    static int SelectNearest(int priorityCount, int cap, float far2)
    {
        var rest = candidates.Count - priorityCount;
        LastPriority = priorityCount;
        LastDroppedNear = 0;
        var room = Mathf.Max(0, cap - priorityCount);
        if (rest <= room) return Mathf.Min(candidates.Count, cap);
        Array.Clear(histogram, 0, histogram.Length);
        var scale = histogram.Length / Mathf.Max(far2, 1f);
        for (var k = priorityCount; k < candidates.Count; k++) histogram[Mathf.Min(histogram.Length - 1, (int)(candidates[k].d2 * scale))]++;
        var acc = 0; var cut = histogram.Length;
        for (var b = 0; b < histogram.Length; b++) { if (acc + histogram[b] > room) { cut = b; break; } acc += histogram[b]; }
        var left = room - acc;   // how many from the cut bucket
        LastDroppedNear = 0;
        for (var k = priorityCount; k < candidates.Count; k++)
        {
            var b = Mathf.Min(histogram.Length - 1, (int)(candidates[k].d2 * scale));
            if (b < cut) continue;
            if (b == cut && left > 0) { left--; continue; }
            if (candidates[k].d2 < 400f) LastDroppedNear++;
            candidates[k].d2 = -1f;
        }
        return Mathf.Min(candidates.Count, cap);
    }

    static bool ProjectBounds(Bounds b, Matrix4x4 vp, int w, int h, ref int x0, ref int y0, ref int x1, ref int y1)
    {
        var c = b.center; var e = b.extents;
        for (var k = 0; k < 8; k++)
        {
            var p = new Vector3(c.x + ((k & 1) != 0 ? e.x : -e.x), c.y + ((k & 2) != 0 ? e.y : -e.y), c.z + ((k & 4) != 0 ? e.z : -e.z));
            var clip = vp * new Vector4(p.x, p.y, p.z, 1f);
            if (clip.w <= 1e-3f) return false;
            var vx = (clip.x / clip.w * .5f + .5f) * w;
            // the targets are rendered with the flipped (render-into-texture) projection: texture row = viewport y * height
            var vy = (clip.y / clip.w * .5f + .5f) * h;
            x0 = Mathf.Min(x0, Mathf.FloorToInt(vx)); x1 = Mathf.Max(x1, Mathf.CeilToInt(vx));
            y0 = Mathf.Min(y0, Mathf.FloorToInt(vy)); y1 = Mathf.Max(y1, Mathf.CeilToInt(vy));
        }
        return true;
    }

    static int BuildLamps(Vector3 camPos, FrontRoomsGlassRTQuality quality, ref uint signature)
    {
        lampCandidates.Clear();
        var r2 = LampRadius * LampRadius;
        foreach (var c in chunks.Values)
        {
            if (c.lights.Count == 0) continue;
            var cd = (c.lightCenter - camPos).magnitude - c.lightRadius;
            if (cd > LampRadius) continue;
            foreach (var rec in c.lights)
            {
                var d2 = (rec.pos - camPos).sqrMagnitude;
                if (d2 > r2) continue;
                var l = rec.light;
                if (l == null || !l.isActiveAndEnabled) continue;
                lampCandidates.Add((d2, rec));
            }
        }
        lampCandidates.Sort((a, b) => a.d2.CompareTo(b.d2));
        var n = 0;
        var linear = GraphicsSettings.lightsUseLinearIntensity;
        foreach (var (d2, rec) in lampCandidates)
        {
            if (n >= MaxLamps) break;
            var l = rec.light;
            var intensity = l.intensity;
            if (intensity <= 0f) continue;
            var col = rec.colorLinear * intensity;   // URP: lightsUseLinearIntensity -> color.linear * intensity
            var shadow = l.shadows != LightShadows.None ? rec.shadowStrength : 0f;
            var lamp = new Native.Lamp
            {
                posInvRange2 = new Vector4(rec.pos.x, rec.pos.y, rec.pos.z, rec.invRange2),
                color = new Vector4(col.r, col.g, col.b, shadow),
            };
            if (rec.type == LightType.Spot)
            {
                lamp.atten = new Vector4(rec.invRange2, 0f, rec.invAngleRange, rec.angleAdd);
                lamp.spotDir = new Vector4(-rec.fwd.x, -rec.fwd.y, -rec.fwd.z, Native.LampSpot);
            }
            else
            {
                lamp.atten = new Vector4(rec.invRange2, 0f, 0f, 1f);
                lamp.spotDir = new Vector4(0f, -1f, 0f, Native.LampPoint);
            }
            lampBuffer[n++] = lamp;
            if (d2 < 400f) signature = Hash(signature, lamp.color);
        }
        if (dirLight != null && dirLight.isActiveAndEnabled && dirLight.intensity > 0f && n < lampBuffer.Length)
        {
            var col = FinalColor(dirLight, linear);
            var shadowed = dirLight.shadows != LightShadows.None;
            var w = 0f;
            if (shadowed)
            {
                if (quality == FrontRoomsGlassRTQuality.Ultra) w = dirLight.shadowStrength;   // a fill shadow ray
                else col *= 1f - dirLight.shadowStrength;                                        // High: the constant 0.82 of design §1.5
            }
            var d = -dirLight.transform.forward;
            lampBuffer[n++] = new Native.Lamp
            {
                posInvRange2 = new Vector4(d.x, d.y, d.z, 0f),
                color = new Vector4(col.r, col.g, col.b, w),
                spotDir = new Vector4(0f, 0f, 0f, Native.LampDirectional),
                atten = new Vector4(0f, 0f, 0f, 1f),
            };
        }
        return n;
    }

    static Color FinalColor(Light l, bool linearIntensity)
    {
        var c = linearIntensity ? l.color.linear * l.intensity : (l.color * l.intensity).linear;
        if (GraphicsSettings.lightsUseColorTemperature && l.useColorTemperature) c *= Mathf.CorrelatedColorTemperatureToRGB(l.colorTemperature);
        return c;
    }

    // URP SampleSH from RenderSettings.ambientProbe, rewritten in the kernel's basis {1, x, y, z, xy, yz, xz, x², y²}
    // (z² = 1 - x² - y²). Same constants Unity uses for unity_SHAr..unity_SHC.
    static void AmbientSH(ref Native.Frame f)
    {
        var sh = RenderSettings.ambientProbe;
        var o = new Vector4[9];
        for (var ch = 0; ch < 3; ch++)
        {
            var shaX = sh[ch, 3]; var shaY = sh[ch, 1]; var shaZ = sh[ch, 2]; var shaW = sh[ch, 0] - sh[ch, 6];
            var shbX = sh[ch, 4]; var shbY = sh[ch, 5]; var shbZ = sh[ch, 6] * 3f; var shbW = sh[ch, 7];
            var shc = sh[ch, 8];
            float[] v = { shaW + shbZ, shaX, shaY, shaZ, shbX, shbY, shbW, shc - shbZ, -shc - shbZ };
            for (var k = 0; k < 9; k++) o[k][ch] = v[k];
        }
        f.sh0 = o[0]; f.sh1 = o[1]; f.sh2 = o[2]; f.sh3 = o[3]; f.sh4 = o[4]; f.sh5 = o[5]; f.sh6 = o[6]; f.sh7 = o[7]; f.sh8 = o[8];
    }

    static IntPtr EnvCube(out bool valid)
    {
        valid = false;
        Texture t = null;
        if (RenderSettings.defaultReflectionMode == DefaultReflectionMode.Custom && RenderSettings.customReflectionTexture != null) t = RenderSettings.customReflectionTexture;
        else t = ReflectionProbe.defaultTexture;
        if (t == null || t.dimension != TextureDimension.Cube) return IntPtr.Zero;
        var id = t.GetInstanceID();
        if (!cubePtr.TryGetValue(id, out var p))
        {
            if (t is RenderTexture rt && !rt.IsCreated()) return IntPtr.Zero;
            p = t.GetNativeTexturePtr();   // once per cube object
            cubePtr[id] = p;
        }
        valid = p != IntPtr.Zero;
        return p;
    }

    static float Halton(int index, int b)
    {
        float f = 1f, r = 0f;
        var i = index;
        while (i > 0) { f /= b; r += f * (i % b); i /= b; }
        return r;
    }

    static uint Hash(uint h, Vector4 a)
    {
        unchecked
        {
            h = (h ^ (uint)a.x.GetHashCode()) * 16777619u;
            h = (h ^ (uint)a.y.GetHashCode()) * 16777619u;
            h = (h ^ (uint)a.z.GetHashCode()) * 16777619u;
            h = (h ^ (uint)a.w.GetHashCode()) * 16777619u;
        }
        return h;
    }

    static uint Hash(uint h, Vector4 a, Vector4 b, Vector4 c) => Hash(Hash(Hash(h, a), b), c);

    // ------------------------------------------------------------------ camera targets -> native
    internal static void SendTargets(FrontRoomsGlassRTCamera s)
    {
        if (!nativeOk) return;
        IntPtr P(RTHandle h) => h != null && h.rt != null ? h.rt.GetNativeTexturePtr() : IntPtr.Zero;   // once per (re)allocation (R23)
        Native.FRGlassRT_SetCameraTargets(s.id, P(s.depth), P(s.normal), P(s.raw), P(s.aux), P(s.rawAA), P(s.outTex), P(s.ids), s.width, s.height);
    }

    // ------------------------------------------------------------------ stats
    public static bool TryGetStats(out FrontRoomsGlassRTStats s)
    {
        s = default;
        CheckNative();
        if (!nativeOk) return false;
        float G(Native.Stat x) => (float)Native.Get(x);
        int I(Native.Stat x) => (int)Native.Get(x);
        s.gpuMsAccel = G(Native.Stat.GpuMsAS); s.gpuMsTrace = G(Native.Stat.GpuMsTrace); s.gpuMsResolve = G(Native.Stat.GpuMsResolve);
        s.gpuMsTotal = G(Native.Stat.GpuMsTotal); s.gpuMsCommandBuffer = G(Native.Stat.GpuMsCB); s.gpuMsFrame = G(Native.Stat.TimerFrameGpuMs);
        s.encodeMs = G(Native.Stat.EncodeMs); s.encodeMsMax = G(Native.Stat.EncodeMsMax); s.compileMs = G(Native.Stat.CompileMs);
        s.mainThreadMs = mainMs; s.mainThreadMsMax = mainMsMax;
        s.tlasInstances = I(Native.Stat.TlasInstances); s.candidates = lastCandidates; s.meshes = I(Native.Stat.Meshes); s.meshesPending = I(Native.Stat.MeshesPending);
        s.blasBuiltTotal = I(Native.Stat.BlasBuiltTotal); s.compacted = I(Native.Stat.Compacted); s.retired = I(Native.Stat.Retired);
        s.lamps = lastLamps; s.receiversVisible = lastReceiversVisible;
        s.glassPixels = I(Native.Stat.GlassPx); s.missPixels = I(Native.Stat.MissPx); s.hitPixels = I(Native.Stat.HitPx); s.layerPixels = I(Native.Stat.LayerPx);
        s.aaPixels = I(Native.Stat.AAPx); s.shadowRays = I(Native.Stat.ShadowRays);
        s.events = I(Native.Stat.Events); s.traceEvents = I(Native.Stat.TraceEvents); s.skippedNotReady = I(Native.Stat.SkippedNotReady);
        s.ringBusy = I(Native.Stat.RingBusy); s.commandBufferErrors = I(Native.Stat.CbErrors);
        s.blasBytes = (long)Native.Get(Native.Stat.BlasBytes); s.geometryBytes = (long)Native.Get(Native.Stat.GeometryBytes);
        foreach (var c in cameras) if (c != null) { s.targetBytes += c.targetBytes; s.accumulation = Mathf.Max(s.accumulation, c.accumulation); }
        s.residency = Native.Get(Native.Stat.ResidencyMode) == 1 ? "residency set" : Native.Get(Native.Stat.ResidencyMode) == 2 ? "useResource" : "none";
        s.hazardModes = "depth " + Native.Get(Native.Stat.HazardDepth) + ", out " + Native.Get(Native.Stat.HazardOut);
        return true;
    }

    // ------------------------------------------------------------------ harness helpers
    internal static int RegisteredInstances => liveCount;
    /// <summary>Hit-id debug only: the renderer of each TLAS instance index of the last frame (all meshes built).</summary>
    internal static readonly List<Renderer> SubmittedRenderers = new List<Renderer>();
    internal static int LastDroppedNear, LastPriority;
    internal static int KnownChunks => chunks.Count;
    internal static int PendingRescans => rescanQueue.Count;
    internal static int MeshUploadsCPU => meshesUploadedCPU;
    internal static int MeshUploadsGPU => meshesUploadedGPU;
    internal static int MeshUploadFailures => meshUploadFailures;
    internal static int MaterialsUsed => nextMaterial;
    internal static int TexturesUsed => nextTexture;
    internal static int Lens => CountWhere(i => i.lens);
    internal static int Receivers => CountWhere(i => i.receiver);
    internal static int Dynamics => CountWhere(i => i.dynamic);
    internal static List<FrontRoomsGlassRTCamera> Cameras => cameras;

    static int CountWhere(Func<Inst, bool> p)
    {
        var n = 0;
        foreach (var i in insts) if (i != null && p(i)) n++;
        return n;
    }

    /// <summary>Harness: register every pending chunk now (no 1 ms slicing).</summary>
    internal static void Flush()
    {
        var keep = RegisterBudgetMs;
        RegisterBudgetMs = 10000f;
        UpdateScene(true);
        RegisterBudgetMs = keep;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        // Domain reload off or a new Play session: forget every registration (EnsureStarted resets the plugin's scene).
        foreach (var i in insts) if (i != null) ClearPrepassBit(i);
        insts.Clear(); freeHandles.Clear(); watch.Clear(); chunks.Clear(); rescanQueue.Clear();
        meshById.Clear(); meshByKey.Clear(); materialBlockSingle.Clear(); materialBlockMulti.Clear(); textureSlot.Clear(); cubePtr.Clear();
        tickets.Clear(); cameras.Clear();
        nextMaterial = 0; nextTexture = 4; liveCount = 0; map = null; mapRoot = null; relayRoot = null; dirLight = null;
        lastSceneFrame = -1; nextMapSearch = 0f; nextDirSearch = 0f; started = false; loggedOn = false; warnedMarker = false;
        nextMeshKey = 0; meshesUploadedCPU = meshesUploadedGPU = meshUploadFailures = 0;
    }
}
#endif
