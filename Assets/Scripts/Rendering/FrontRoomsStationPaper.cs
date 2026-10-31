using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// BS7: the station paper's live date (research/tutorial/20_requests.md BS7; research/station/01_research.md section 6).
/// The graphic line's atlas (Prop_StationPaper_A, Tools/print/ink/art_from_graphic/station_paper/) prints every string
/// except three dates, which follow the live clock: the DAR header, the register's first DATE cell and the sign-out
/// sheet's first DATE cell (date_slots.json). All three show the same night: the shift that began at 2300 on the day
/// before the in-game date when the in-game time is 07:00 or later, otherwise two days before (in-game date = today's
/// month and day in 1990, FrontRoomsTerminalContent's rule and clock, so a harness's ClockOverride holds here too).
/// The date is fixed for the run (worked out once, at the first station) and the DAR transcript's {DAR_DATE} is set
/// to it, so EXAMINE reads what the sheet prints.
/// How it is drawn: Resources/Surfaces/Textures/Prop_StationPaper_Dates.json (Tools/lookdev/gen_station_paper_dates.py)
/// holds, per slot, the glyphs 0-9 and '-' at build_atlas.py's drawing scale, the patch of the cell every date touches
/// (in atlas texels) and the atlas's own texels there. The date's coverage is composed at that scale, resampled to the
/// cell's texels with PIL's LANCZOS weights (build_atlas stretches each page into its cell the same way), and written as
/// bg - A * (255 - ink) into a small patch texture. The patch lies 0.3 mm over the paper on a grid mapped through the
/// kit's own UVs (so it sits exactly on those texels, on any kit that shows the cell), with a copy of Prop_StationPaper
/// (same tint, wear and lighting), in the kit's LOD0 only, no shadow, following the paper renderer's enabled state.
/// Needs readable kit meshes (DatedKits; FrontRoomsKitImporter); a mesh that is not readable keeps its blank slot.
/// Under map.Station, one per station (every floor's); never edits a map file; nothing happens without a station.
/// </summary>
[AddComponentMenu("")]
public sealed class FrontRoomsStationPaper : MonoBehaviour
{
    public const string ObjectName = "Station paper dates";
    public const string PaperMaterial = "Prop_StationPaper";
    public const string DatesResource = "Surfaces/Textures/Prop_StationPaper_Dates";
    public const string DarTranscript = "station_dar";
    public const string DarToken = "{DAR_DATE}";
    /// <summary>The kits whose paper shows a dated cell (register_left, dar, sign_out); FrontRoomsKitImporter imports them readable.</summary>
    public static readonly string[] DatedKits = { "Kit_Clipboard", "Kit_Clipboard_Wall", "Kit_RegisterBook" };
    public static bool IsDatedKit(string kit) => kit != null && Array.IndexOf(DatedKits, kit) >= 0;

    /// <summary>The patch's height over the paper (metres, along the paper's front).</summary>
    public const float Lift = .0003f;
    const int GridX = 8, GridY = 2;
    /// <summary>A triangle counts as the slot's page when its metres per atlas unit are within this share of the page's.</summary>
    const float ScaleTolerance = .2f;

    // ------------------------------------------------------------------ the data (JSON)
    [Serializable] public sealed class GlyphData { public string c; public int w, h, ox, oy; public float adv; public string a; }
    [Serializable] public sealed class KernData { public string p; public float k; }
    [Serializable]
    public sealed class SlotData
    {
        public string id, cellName, face, penRound, bg;
        public int cell, pageWpx, pageHpx, stroke, baseInt;
        public float pageW, pageH, x, baseline, cap, slotW, penX;
        public int[] ink, patch;
        public GlyphData[] glyphs;
        public KernData[] kern;
    }
    [Serializable]
    public sealed class DatesData
    {
        public int version, cellPx, grid;
        public float pxPerMm;
        public string atlas, slotsSource;
        public SlotData[] slots;
    }

    /// <summary>One slot, decoded once: glyph coverage, kerning, background, and the LANCZOS weights from page pixels to the patch's texels.</summary>
    sealed class Slot
    {
        public SlotData d;
        public int cellPx, grid;
        public int tx0, ty0, tw, th;
        public readonly Dictionary<char, GlyphData> glyphs = new Dictionary<char, GlyphData>();
        public readonly Dictionary<char, byte[]> coverage = new Dictionary<char, byte[]>();
        public readonly Dictionary<int, float> kern = new Dictionary<int, float>();
        public byte[] bg;
        public Color32 ink;
        public int inX0, inX1, inY0, inY1;
        public int[] wxStart, wyStart;
        public float[][] wx, wy;

        public float AtlasU(float tx) => (d.cell % grid) / (float)grid + tx / (cellPx * (float)grid);
        public float AtlasV(float ty) => 1f - (d.cell / grid) / (float)grid - ty / (cellPx * (float)grid);
    }

    static DatesData data;
    static bool dataTried;
    static Slot[] slots;
    static string runStamp;
    static string[] darTemplate;
    static readonly HashSet<string> warned = new HashSet<string>();

    // ------------------------------------------------------------------ the date
    /// <summary>The night the station's paper is dated with (01 section 6 live-clock rule) for the wall clock <paramref name="now"/>.</summary>
    public static DateTime NightOf(DateTime now)
    {
        var game = new DateTime(1990, now.Month, now.Month == 2 && now.Day == 29 ? 28 : now.Day);
        return game.AddDays(now.Hour >= 7 ? -1 : -2);
    }

    /// <summary>MM-DD-YY, as the paper writes it.</summary>
    public static string Stamp(DateTime d) =>
        d.Month.ToString("00", CultureInfo.InvariantCulture) + "-" + d.Day.ToString("00", CultureInfo.InvariantCulture) + "-"
        + (d.Year % 100).ToString("00", CultureInfo.InvariantCulture);

    /// <summary>The run's date stamp (worked out at the first call of a run, then fixed), and the DAR transcript set to it.</summary>
    public static string RunStamp
    {
        get
        {
            if (runStamp == null)
            {
                runStamp = Stamp(NightOf(FrontRoomsTerminalContent.Now));
                ApplyTranscript(runStamp);
            }
            return runStamp;
        }
    }

    static void ApplyTranscript(string stamp)
    {
        if (darTemplate == null)
        {
            var lines = FrontRoomsInspectCatalog.Transcript(DarTranscript);
            if (lines == null) return;
            var has = false;
            foreach (var l in lines) if (l != null && l.IndexOf(DarToken, StringComparison.Ordinal) >= 0) has = true;
            if (!has) return;
            darTemplate = (string[])lines.Clone();
        }
        var resolved = new string[darTemplate.Length];
        for (var i = 0; i < resolved.Length; i++) resolved[i] = darTemplate[i] != null ? darTemplate[i].Replace(DarToken, stamp) : null;
        FrontRoomsInspectCatalog.SetTranscript(DarTranscript, resolved);
    }

    // ------------------------------------------------------------------ hook
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Hook()
    {
        // darTemplate stays: with domain reload off the catalog keeps last session's resolved lines, and only the template has the token.
        runStamp = null;
        slots = null;
        data = null;
        dataTried = false;
        FrontRooms3DGame.MapRunStarted -= OnRunStarted;
        FrontRooms3DGame.MapRunStarted += OnRunStarted;
        FrontRooms3DGame.FloorChanged -= OnFloorChanged;
        FrontRooms3DGame.FloorChanged += OnFloorChanged;
        FrontRooms3DGame.MapRunEnded -= OnRunEnded;
        FrontRooms3DGame.MapRunEnded += OnRunEnded;
    }

    static void OnRunStarted(FrontRoomsMapWorld map, FrontRoomsMapHunter relay)
    {
        runStamp = null;
        Attach(map);
    }

    static void OnFloorChanged(FrontRoomsMapWorld map, FrontRoomsMapHunter relay, int floor) => Attach(map);
    static void OnRunEnded() => runStamp = null;

    /// <summary>Date the map's station paper once (null with no station). Public for harnesses.</summary>
    public static FrontRoomsStationPaper Attach(FrontRoomsMapWorld map)
    {
        if (map == null || !map.HasStation || map.Station == null) return null;
        var existing = map.Station.GetComponentInChildren<FrontRoomsStationPaper>(true);
        if (existing != null) return existing;
        var go = new GameObject(ObjectName);
        go.transform.SetParent(map.Station.transform, false);
        var p = go.AddComponent<FrontRoomsStationPaper>();
        p.set = map.Station;
        _ = RunStamp;   // the transcript is right from the first station on
        return p;
    }

    // ------------------------------------------------------------------ per station
    sealed class Overlay
    {
        public Renderer source;
        public MeshRenderer renderer;
        public Mesh mesh;
    }

    FrontRoomsStationSet set;
    Transform props;
    int scannedChildren = -1;
    bool scanned;
    string builtStamp;
    readonly List<Overlay> overlays = new List<Overlay>();
    readonly HashSet<Renderer> seen = new HashSet<Renderer>();
    Texture2D[] textures;
    Material[] materials;

    /// <summary>The patches laid so far (renderer, slot id), for harnesses.</summary>
    public int OverlayCount => overlays.Count;
    public string DateStamp => builtStamp;

    void Update()
    {
        if (set == null) return;
        if (props == null) props = set.transform.Find("Station props");
        if (props != null && set.PropsDone && (!scanned || props.childCount != scannedChildren))
        {
            scanned = true;
            scannedChildren = props.childCount;
            Scan();
        }
        if (textures != null && builtStamp != RunStamp) BuildTextures();
        for (var i = overlays.Count - 1; i >= 0; i--)
        {
            var o = overlays[i];
            if (o.renderer == null || o.source == null)
            {
                if (o.renderer != null) Kill(o.renderer.gameObject);
                Kill(o.mesh);
                overlays.RemoveAt(i);
                continue;
            }
            if (o.renderer.enabled != o.source.enabled) o.renderer.enabled = o.source.enabled;
        }
    }

    void OnDestroy()
    {
        foreach (var o in overlays)
        {
            if (o.renderer != null) Kill(o.renderer.gameObject);
            Kill(o.mesh);
        }
        overlays.Clear();
        if (materials != null) foreach (var m in materials) Kill(m);
        if (textures != null) foreach (var t in textures) Kill(t);
        materials = null;
        textures = null;
    }

    static void Kill(UnityEngine.Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
    }

    // Every LOD0 renderer of the station's props that shows Prop_StationPaper: a patch for each dated slot its UVs hold.
    void Scan()
    {
        if (!LoadData()) return;
        foreach (Transform kit in props)
        {
            var group = kit.GetComponentInChildren<LODGroup>(true);
            Renderer[] candidates = null;
            if (group != null)
            {
                var lods = group.GetLODs();
                if (lods.Length > 0) candidates = lods[0].renderers;
            }
            if (candidates == null) candidates = kit.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in candidates)
            {
                if (r == null || !seen.Add(r)) continue;
                try { Dress(kit.name, r, group); }
                catch (Exception e) { WarnOnce("dress " + kit.name, "[StationPaper] " + kit.name + ": " + e.Message); }
            }
        }
    }

    void Dress(string kitName, Renderer r, LODGroup group)
    {
        var mr = r as MeshRenderer;
        if (mr == null) return;
        var filter = r.GetComponent<MeshFilter>();
        var mesh = filter != null ? filter.sharedMesh : null;
        if (mesh == null) return;
        var mats = r.sharedMaterials;
        var sub = -1;
        for (var i = 0; i < mats.Length && i < mesh.subMeshCount; i++)
            if (mats[i] != null && mats[i].name.StartsWith(PaperMaterial, StringComparison.Ordinal)) { sub = i; break; }
        if (sub < 0) return;
        if (!mesh.isReadable)
        {
            if (IsDatedKit(kitName)) WarnOnce("readable " + kitName, "[StationPaper] " + kitName + "'s mesh is not readable (FrontRoomsKitImporter): its date stays blank");
            return;
        }
        var verts = mesh.vertices;
        var uvs = mesh.uv;
        var norms = mesh.normals;
        var tris = mesh.GetTriangles(sub);
        if (uvs == null || uvs.Length != verts.Length || tris.Length < 3) return;
        if (norms != null && norms.Length != verts.Length) norms = null;
        for (var s = 0; s < slots.Length; s++)
        {
            var o = Lay(slots[s], s, r, mr, verts, uvs, norms, tris);
            if (o == null) continue;
            overlays.Add(o);
            seen.Add(o.renderer);   // a rescan finds it in LOD0 too: never a patch of a patch
            if (group != null) AddToLod0(group, o.renderer);
        }
    }

    // The slot's patch through this mesh's UVs: null when the mesh does not show the slot's cell there.
    Overlay Lay(Slot slot, int index, Renderer r, MeshRenderer mr, Vector3[] verts, Vector2[] uvs, Vector3[] norms, int[] tris)
    {
        // Triangles whose UVs reach the patch and whose size per atlas unit is the page's (not a block edge or a strip).
        float u0 = slot.AtlasU(slot.tx0), u1 = slot.AtlasU(slot.tx0 + slot.tw);
        float vTop = slot.AtlasV(slot.ty0), vBottom = slot.AtlasV(slot.ty0 + slot.th);
        float wantU = slot.d.pageW * .001f * slot.grid, wantV = slot.d.pageH * .001f * slot.grid;
        var t = r.transform;
        var cand = new List<int>();
        for (var k = 0; k + 2 < tris.Length; k += 3)
        {
            Vector2 a = uvs[tris[k]], b = uvs[tris[k + 1]], c = uvs[tris[k + 2]];
            if (Mathf.Max(a.x, Mathf.Max(b.x, c.x)) < u0 || Mathf.Min(a.x, Mathf.Min(b.x, c.x)) > u1) continue;
            if (Mathf.Max(a.y, Mathf.Max(b.y, c.y)) < vBottom || Mathf.Min(a.y, Mathf.Min(b.y, c.y)) > vTop) continue;
            if (!Gradients(verts[tris[k]], verts[tris[k + 1]], verts[tris[k + 2]], a, b, c, out var dPdu, out var dPdv)) continue;
            var su = t.TransformVector(dPdu).magnitude;
            var sv = t.TransformVector(dPdv).magnitude;
            if (Mathf.Abs(su / wantU - 1f) > ScaleTolerance || Mathf.Abs(sv / wantV - 1f) > ScaleTolerance) continue;
            cand.Add(k);
        }
        if (cand.Count == 0) return null;

        var n = (GridX + 1) * (GridY + 1);
        var pos = new Vector3[n];
        var nrm = new Vector3[n];
        var uv = new Vector2[n];
        var front = new Vector3[n];
        for (var j = 0; j <= GridY; j++)
            for (var i = 0; i <= GridX; i++)
            {
                var v = i + j * (GridX + 1);
                var p = new Vector2(slot.AtlasU(slot.tx0 + slot.tw * (float)i / GridX), slot.AtlasV(slot.ty0 + slot.th * (float)j / GridY));
                if (!Locate(p, cand, verts, uvs, norms, tris, out var at, out var shade, out var face)) return null;
                // Lift toward the paper's front (its winding), whatever way the imported normal points.
                var lift = face.normalized;
                var scale = t.TransformVector(lift).magnitude;
                pos[v] = at + lift * (Lift / Mathf.Max(scale, 1e-6f));
                nrm[v] = shade;
                front[v] = lift;
                uv[v] = new Vector2((float)i / GridX, 1f - (float)j / GridY);
            }
        var flip = Vector3.Dot(Vector3.Cross(pos[1] - pos[0], pos[GridX + 2] - pos[0]), front[0]) < 0f;
        var idx = new List<int>(GridX * GridY * 6);
        for (var j = 0; j < GridY; j++)
            for (var i = 0; i < GridX; i++)
            {
                int a = i + j * (GridX + 1), b = a + 1, c = a + GridX + 2, d = a + GridX + 1;
                if (!flip) { idx.Add(a); idx.Add(b); idx.Add(c); idx.Add(a); idx.Add(c); idx.Add(d); }
                else { idx.Add(a); idx.Add(c); idx.Add(b); idx.Add(a); idx.Add(d); idx.Add(c); }
            }

        var mesh = new Mesh { name = "Station paper date (" + slot.d.id + ")" };
        mesh.SetVertices(pos);
        mesh.SetNormals(nrm);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(idx, 0);
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        mesh.UploadMeshData(true);

        EnsureMaterials(mr);
        if (materials == null || materials[index] == null) { Kill(mesh); return null; }
        var go = new GameObject("date (" + slot.d.id + ")");
        go.layer = r.gameObject.layer;
        go.transform.SetParent(t, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var pr = go.AddComponent<MeshRenderer>();
        pr.sharedMaterial = materials[index];
        pr.shadowCastingMode = ShadowCastingMode.Off;
        pr.receiveShadows = r.receiveShadows;
        pr.lightProbeUsage = r.lightProbeUsage;
        pr.reflectionProbeUsage = r.reflectionProbeUsage;
        pr.probeAnchor = r.probeAnchor;
        pr.motionVectorGenerationMode = r.motionVectorGenerationMode;
        pr.allowOcclusionWhenDynamic = r.allowOcclusionWhenDynamic;
        pr.renderingLayerMask = r.renderingLayerMask;
        pr.enabled = r.enabled;
        return new Overlay { source = r, renderer = pr, mesh = mesh };
    }

    static bool Gradients(Vector3 p0, Vector3 p1, Vector3 p2, Vector2 a, Vector2 b, Vector2 c, out Vector3 dPdu, out Vector3 dPdv)
    {
        float du1 = b.x - a.x, dv1 = b.y - a.y, du2 = c.x - a.x, dv2 = c.y - a.y;
        var det = du1 * dv2 - du2 * dv1;
        if (Mathf.Abs(det) < 1e-12f) { dPdu = dPdv = Vector3.zero; return false; }
        var r = 1f / det;
        Vector3 e1 = p1 - p0, e2 = p2 - p0;
        dPdu = (e1 * dv2 - e2 * dv1) * r;
        dPdv = (e2 * du1 - e1 * du2) * r;
        return true;
    }

    // The point of the paper whose UV is p: position, shading normal (the mesh's), and the face's front (its winding).
    static bool Locate(Vector2 p, List<int> cand, Vector3[] verts, Vector2[] uvs, Vector3[] norms, int[] tris, out Vector3 at, out Vector3 shade, out Vector3 face)
    {
        const float eps = 1e-4f;
        foreach (var k in cand)
        {
            int i0 = tris[k], i1 = tris[k + 1], i2 = tris[k + 2];
            Vector2 a = uvs[i0], b = uvs[i1], c = uvs[i2];
            var d = (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y);
            if (Mathf.Abs(d) < 1e-12f) continue;
            var l1 = ((p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y)) / d;
            var l2 = ((b.x - a.x) * (p.y - a.y) - (p.x - a.x) * (b.y - a.y)) / d;
            var l0 = 1f - l1 - l2;
            if (l0 < -eps || l1 < -eps || l2 < -eps) continue;
            at = verts[i0] * l0 + verts[i1] * l1 + verts[i2] * l2;
            face = Vector3.Cross(verts[i1] - verts[i0], verts[i2] - verts[i0]);
            if (face.sqrMagnitude < 1e-20f) continue;
            face.Normalize();
            shade = norms != null ? (norms[i0] * l0 + norms[i1] * l1 + norms[i2] * l2) : face;
            if (shade.sqrMagnitude < 1e-12f) shade = face;
            shade.Normalize();
            return true;
        }
        at = shade = face = Vector3.zero;
        return false;
    }

    static void AddToLod0(LODGroup group, Renderer extra)
    {
        var lods = group.GetLODs();
        if (lods.Length == 0) return;
        var list = new List<Renderer>(lods[0].renderers ?? new Renderer[0]);
        if (list.Contains(extra)) return;
        list.Add(extra);
        lods[0].renderers = list.ToArray();
        group.SetLODs(lods);
    }

    // One texture and one material per slot (shared by every kit that shows it), from the paper material the kit uses.
    void EnsureMaterials(MeshRenderer mr)
    {
        if (materials != null) return;
        Material paper = null;
        foreach (var m in mr.sharedMaterials)
            if (m != null && m.name.StartsWith(PaperMaterial, StringComparison.Ordinal)) { paper = m; break; }
        if (paper == null) paper = FrontRoomsSurfaces.TryGet(PaperMaterial);
        if (paper == null) return;
        textures = new Texture2D[slots.Length];
        materials = new Material[slots.Length];
        for (var s = 0; s < slots.Length; s++)
        {
            var sl = slots[s];
            textures[s] = new Texture2D(sl.tw, sl.th, TextureFormat.RGBA32, true, false)
            {
                name = "Station paper date " + sl.d.id,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 16,
            };
            materials[s] = new Material(paper) { name = PaperMaterial + " (date " + sl.d.id + ")" };
            materials[s].SetTexture("_BaseMap", textures[s]);
        }
        BuildTextures();
    }

    void BuildTextures()
    {
        builtStamp = RunStamp;
        for (var s = 0; s < slots.Length; s++)
        {
            if (textures[s] == null) continue;
            textures[s].SetPixels32(Compose(slots[s], builtStamp));
            textures[s].Apply(true, false);
        }
    }

    // ------------------------------------------------------------------ composing (build_atlas.py's text, then its LANCZOS)
    /// <summary>The patch with <paramref name="text"/> drawn in the slot: RGBA, Unity's row order (bottom row first).</summary>
    static Color32[] Compose(Slot sl, string text)
    {
        int X0 = sl.inX0, W = sl.inX1 - sl.inX0, Y0 = sl.inY0, H = sl.inY1 - sl.inY0;
        var cov = new float[W * H];
        var pen = sl.d.penX;
        var round = sl.d.penRound != "floor";
        var prev = '\0';
        foreach (var ch in text)
        {
            if (prev != '\0' && sl.kern.TryGetValue(prev * 65536 + ch, out var k)) pen += k;
            prev = ch;
            if (!sl.glyphs.TryGetValue(ch, out var g)) continue;
            var a = sl.coverage[ch];
            var gx = (round ? Mathf.FloorToInt(pen + .5f) : Mathf.FloorToInt(pen)) + g.ox;
            var gy = sl.d.baseInt + g.oy;
            for (var j = 0; j < g.h; j++)
            {
                var yy = gy + j - Y0;
                if (yy < 0 || yy >= H) continue;
                for (var i = 0; i < g.w; i++)
                {
                    var xx = gx + i - X0;
                    if (xx < 0 || xx >= W) continue;
                    var v = a[j * g.w + i] * (1f / 255f);
                    var at = yy * W + xx;
                    if (v > cov[at]) cov[at] = v;
                }
            }
            pen += g.adv;
        }
        // Horizontal pass, then vertical (PIL's order).
        var tmp = new float[H * sl.tw];
        for (var y = 0; y < H; y++)
            for (var o = 0; o < sl.tw; o++)
            {
                var w = sl.wx[o];
                var start = sl.wxStart[o] - X0;
                var sum = 0f;
                for (var q = 0; q < w.Length; q++) sum += w[q] * cov[y * W + start + q];
                tmp[y * sl.tw + o] = sum;
            }
        var px = new Color32[sl.tw * sl.th];
        for (var o = 0; o < sl.th; o++)
        {
            var w = sl.wy[o];
            var start = sl.wyStart[o] - Y0;
            for (var x = 0; x < sl.tw; x++)
            {
                var A = 0f;
                for (var q = 0; q < w.Length; q++) A += w[q] * tmp[(start + q) * sl.tw + x];
                var b = (o * sl.tw + x) * 3;
                var dst = (sl.th - 1 - o) * sl.tw + x;
                px[dst] = new Color32(Ink(sl.bg[b], sl.ink.r, A), Ink(sl.bg[b + 1], sl.ink.g, A), Ink(sl.bg[b + 2], sl.ink.b, A), 255);
            }
        }
        return px;
    }

    static byte Ink(byte bg, byte ink, float a) => (byte)Mathf.Clamp(Mathf.RoundToInt(bg - a * (255 - ink)), 0, 255);

    static double Lanczos(double x)
    {
        if (x < -3.0 || x >= 3.0) return 0.0;
        return Sinc(x) * Sinc(x / 3.0);
    }

    static double Sinc(double x)
    {
        if (x == 0.0) return 1.0;
        x *= Math.PI;
        return Math.Sin(x) / x;
    }

    // PIL's precompute_coeffs (Resample.c) for output texels [first, first + count) of an inSize -> outSize resize.
    static void Weights(int inSize, int outSize, int first, int count, out int[] starts, out float[][] weights)
    {
        starts = new int[count];
        weights = new float[count][];
        var scale = (double)inSize / outSize;
        var filterscale = Math.Max(scale, 1.0);
        var support = 3.0 * filterscale;
        var ss = 1.0 / filterscale;
        for (var o = 0; o < count; o++)
        {
            var center = (first + o + .5) * scale;
            var xmin = (int)(center - support + .5);
            if (xmin < 0) xmin = 0;
            var xmax = (int)(center + support + .5);
            if (xmax > inSize) xmax = inSize;
            xmax -= xmin;
            var w = new double[Math.Max(xmax, 0)];
            var ww = 0.0;
            for (var x = 0; x < w.Length; x++) { w[x] = Lanczos((x + xmin - center + .5) * ss); ww += w[x]; }
            var f = new float[w.Length];
            for (var x = 0; x < w.Length; x++) f[x] = (float)(ww != 0.0 ? w[x] / ww : 0.0);
            starts[o] = xmin;
            weights[o] = f;
        }
    }

    static bool LoadData()
    {
        if (slots != null) return slots.Length > 0;
        if (dataTried) return false;
        dataTried = true;
        try
        {
            var text = Resources.Load<TextAsset>(DatesResource);
            if (text == null) { Debug.LogWarning("[StationPaper] " + DatesResource + " missing: the paper's dates stay blank"); slots = new Slot[0]; return false; }
            data = JsonUtility.FromJson<DatesData>(text.text);
            var list = new List<Slot>();
            if (data != null && data.slots != null)
                foreach (var d in data.slots)
                {
                    var sl = Decode(d, data.cellPx > 0 ? data.cellPx : 1024, data.grid > 0 ? data.grid : 4);
                    if (sl != null) list.Add(sl);
                }
            slots = list.ToArray();
            Resources.UnloadAsset(text);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[StationPaper] bad " + DatesResource + ": " + e.Message);
            slots = new Slot[0];
        }
        return slots.Length > 0;
    }

    static Slot Decode(SlotData d, int cellPx, int grid)
    {
        if (d == null || d.patch == null || d.patch.Length < 4 || d.glyphs == null || d.ink == null || d.ink.Length < 3 || d.pageWpx <= 0 || d.pageHpx <= 0) return null;
        var sl = new Slot { d = d, cellPx = cellPx, grid = grid, tx0 = d.patch[0], ty0 = d.patch[1], tw = d.patch[2], th = d.patch[3] };
        if (sl.tw <= 0 || sl.th <= 0) return null;
        sl.ink = new Color32((byte)d.ink[0], (byte)d.ink[1], (byte)d.ink[2], 255);
        sl.bg = string.IsNullOrEmpty(d.bg) ? null : Convert.FromBase64String(d.bg);
        if (sl.bg == null || sl.bg.Length != sl.tw * sl.th * 3)
        {
            sl.bg = new byte[sl.tw * sl.th * 3];
            for (var i = 0; i < sl.bg.Length; i++) sl.bg[i] = 255;
        }
        foreach (var g in d.glyphs)
        {
            if (g == null || string.IsNullOrEmpty(g.c) || g.w <= 0 || g.h <= 0) continue;
            var a = Convert.FromBase64String(g.a ?? "");
            if (a.Length != g.w * g.h) continue;
            sl.glyphs[g.c[0]] = g;
            sl.coverage[g.c[0]] = a;
        }
        if (d.kern != null)
            foreach (var k in d.kern)
                if (k != null && k.p != null && k.p.Length == 2) sl.kern[k.p[0] * 65536 + k.p[1]] = k.k;
        // The page pixels the patch's texels read (LANCZOS support), and their weights.
        Weights(d.pageWpx, cellPx, sl.tx0, sl.tw, out sl.wxStart, out sl.wx);
        Weights(d.pageHpx, cellPx, sl.ty0, sl.th, out sl.wyStart, out sl.wy);
        sl.inX0 = sl.wxStart[0];
        sl.inX1 = sl.wxStart[sl.tw - 1] + sl.wx[sl.tw - 1].Length;
        sl.inY0 = sl.wyStart[0];
        sl.inY1 = sl.wyStart[sl.th - 1] + sl.wy[sl.th - 1].Length;
        for (var o = 0; o < sl.tw; o++) { sl.inX0 = Math.Min(sl.inX0, sl.wxStart[o]); sl.inX1 = Math.Max(sl.inX1, sl.wxStart[o] + sl.wx[o].Length); }
        for (var o = 0; o < sl.th; o++) { sl.inY0 = Math.Min(sl.inY0, sl.wyStart[o]); sl.inY1 = Math.Max(sl.inY1, sl.wyStart[o] + sl.wy[o].Length); }
        return sl;
    }

    static void WarnOnce(string key, string message)
    {
        if (warned.Add(key)) Debug.LogWarning(message);
    }
}
