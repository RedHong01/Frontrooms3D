// The structures scene's falling and running water (game visual line, structures G2-b, package structures_water_flow;
// research/structures_assets/20_research.md A.6, 30_plan.md P13, W/structures_assets/g2_review_assets.md section 9).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The water that moves in image 1's pools, on FrontRooms/WaterFlow (the staged contract K2 calls it from Slide.cs, Well.cs and
/// PoolTerrain.cs in NEW; TODAY never reaches it). Each was a flat two-sided quad; now as the period's tiled pools made them (PC24-PC26):
///   - Nappe: a free sheet over a square porcelain weir lip with a drip groove (the lip: WeirLip, P1's section in P2's tile): the
///     water leaves the nose at about 0.66 m/s and falls free, so after 0.6 m it lands about 0.23 m out (PC24, scaled by sqrt(h));
///     it aerates as it falls and lands in a foam line that drifts away on the water below;
///   - WallFilm: SP2's cascade as a film clinging to the tiled wall (the review's REC; the free sheet stays the owners' option):
///     rivulets that come and go, a bright line where each joint trips it, the foam where it meets the trough;
///   - TroughFilm: the slide's 2-3 cm film riding down the trough with the ride (about 5 m/s), its spill at the exit lip.
/// Shadows off; no collider. One renderer per fall (14 in image 1), so each takes its hall's probe.
/// </summary>
public static class FrontRoomsStructureFalls
{
    const float G = 9.81f;
    /// <summary>The water's speed leaving a lip (m/s): a 6 mm sheet over a level square edge (PC24: lands 0.23 m out after 0.6 m).</summary>
    public const float LipSpeed = .66f;

    static Material sheetMat, filmMat, foamMat, troughMat;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { sheetMat = filmMat = foamMat = troughMat = null; }

    static Material Make(string name, Vector4 sheet, Vector4 flow, float glintCap)
    {
        var sh = Shader.Find("FrontRooms/WaterFlow");
        if (sh == null)
        {
            Debug.LogWarning("[Structures] falls: FrontRooms/WaterFlow is not in this build; the falls are left out.");
            return null;
        }
        var m = new Material(sh) { name = "Structures / " + name, hideFlags = HideFlags.DontSave };
        m.SetVector("_SheetParams", sheet);
        m.SetVector("_FlowParams", flow);
        m.SetFloat("_GlintCap", glintCap);
        return m;
    }

    // _SheetParams: alpha face-on, alpha grazing, foam alpha, roughness (GGX alpha): a clear sheet lets most through and mirrors the room
    // at grazing (glassy, its striations breaking the image into streaks); foam is white water
    static Material SheetMat { get { return sheetMat != null ? sheetMat : (sheetMat = Make("flow sheet", new Vector4(.08f, .45f, .85f, .07f), new Vector4(LipSpeed, G, 6f, .6f), 2.0f)); } }
    // the wall film: 16 % face on, 68 % of the wall in rivulets 45 mm wide (job g2b_fix: at 0.10 over 55 % in 35 mm rivulets the
    // cascade read as the dry tile from across the basin, 11 m; only its glints and the trough's foam showed it)
    static Material FilmMat
    {
        get
        {
            if (filmMat != null) return filmMat;
            filmMat = Make("flow film (wall)", new Vector4(.16f, .40f, .85f, .09f), new Vector4(LipSpeed, G, 3.5f, .5f), 1.6f);
            if (filmMat != null) filmMat.SetVector("_FilmParams", new Vector4(.68f, .045f, .15f, .6f));
            return filmMat;
        }
    }
    static Material FoamMat { get { return foamMat != null ? foamMat : (foamMat = Make("flow foam", new Vector4(.06f, .25f, .65f, .30f), new Vector4(.30f, G, 2f, .4f), 1.0f)); } }
    static Material TroughMat { get { return troughMat != null ? troughMat : (troughMat = Make("flow film (trough)", new Vector4(.14f, .55f, .90f, .10f), new Vector4(LipSpeed, G, 4f, .45f), 2.0f)); } }

    // ---------------------------------------------------------------- a mesh in the making

    sealed class Lists
    {
        public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>();
        public readonly List<Vector4> tan = new List<Vector4>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<Color> c = new List<Color>();
        public readonly List<int> t = new List<int>();

        public int Vert(Vector3 p, Vector3 nrm, Vector3 across, Vector3 flow, Vector2 u, Color col)
        {
            v.Add(p);
            n.Add(nrm);
            // tangent = across the flow (u); its w so that cross(n, tangent) * w runs down the flow (v), as the shader rebuilds it
            var w = Vector3.Dot(Vector3.Cross(nrm, across), flow) >= 0f ? 1f : -1f;
            tan.Add(new Vector4(across.x, across.y, across.z, w));
            uv.Add(u);
            c.Add(col);
            return v.Count - 1;
        }

        // A quad a b c d (in order round it) whose front face (Unity: Cross(b - a, d - a)) is the side its vertices' normal faces:
        // the shader draws both sides (Cull Off) and turns the normal on the back face, so the winding must agree with it.
        public void Quad(int a, int b, int cc, int d)
        {
            var f = Vector3.Cross(v[b] - v[a], v[d] - v[a]);
            if (Vector3.Dot(f, n[a] + n[b] + n[cc] + n[d]) < 0f) { var sw = b; b = d; d = sw; }
            t.Add(a); t.Add(b); t.Add(cc);
            t.Add(a); t.Add(cc); t.Add(d);
        }

        public GameObject Finish(Transform parent, string name, Material m, List<Mesh> owned)
        {
            if (m == null || t.Count == 0 || parent == null) return null;
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetTangents(tan);
            mesh.SetUVs(0, uv);
            mesh.SetColors(c);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            owned?.Add(mesh);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }
    }

    // ---------------------------------------------------------------- a free sheet over a lip and its foam line

    /// <summary>
    /// A nappe from the lip a..b (district plan points; y ignored) whose water stands at 'top', falling free 'out' (horizontal) to the
    /// water (or floor) at 'landing'; its foam line on the water below. Returns the sheet's object (the foam is its child).
    /// </summary>
    public static GameObject Nappe(Transform parent, Vector3 a, Vector3 b, float top, float landing, Vector3 outDir, List<Mesh> owned, string name = "fall")
    {
        outDir.y = 0f;
        if (outDir.sqrMagnitude < 1e-6f || top - landing < .02f) return null;
        outDir.Normalize();
        a.y = b.y = 0f;
        var along = b - a;
        var width = along.magnitude;
        if (width < .01f) return null;
        var across = along / width;
        var h = top - landing;
        var T = Mathf.Sqrt(2f * h / G);
        const int rows = 10;
        var cols = Mathf.Max(1, Mathf.CeilToInt(width / .4f));
        var L = new Lists();
        var idx = new int[rows + 1, cols + 1];
        var arc = 0f;
        var prev = Vector2.zero;
        for (var k = 0; k <= rows; k++)
        {
            // even in time: fine near the lip where the sheet turns, coarser as it falls straight
            var tk = T * k / rows;
            var x = .004f + LipSpeed * tk;
            var y = -.5f * G * tk * tk;
            var q = new Vector2(x, y);
            if (k > 0) arc += Vector2.Distance(prev, q);
            prev = q;
            // the sheet's face: square to its path, facing out and up (toward whoever looks at the fall)
            var nrm = (outDir * (G * tk) + Vector3.up * LipSpeed).normalized;
            var flow = (outDir * LipSpeed - Vector3.up * (G * tk)).normalized;
            var f = k / (float)rows;
            for (var j = 0; j <= cols; j++)
            {
                var u = width * j / cols;
                var p = a + across * u + outDir * x + Vector3.up * (top + y);
                // coverage: the sheet's two ends fade over 3 cm; its foot breaks up into the foam; foam grows as it falls
                var edge = Mathf.Clamp01(Mathf.Min(u, width - u) / .03f);
                var cov = edge * (k == rows ? .55f : 1f);
                // a 0.6 m sheet stays glassy; it aerates only near its foot (the foam is the landing line's)
                var foam = Mathf.Clamp01((f - .8f) / .2f) * .3f;
                idx[k, j] = L.Vert(p, nrm, across, flow, new Vector2(u, arc), new Color(cov, foam, 0f, 1f));
            }
        }
        for (var k = 0; k < rows; k++)
            for (var j = 0; j < cols; j++)
                L.Quad(idx[k, j], idx[k, j + 1], idx[k + 1, j + 1], idx[k + 1, j]);
        var go = L.Finish(parent, "Fall · " + name + " (NEW: nappe)", SheetMat, owned);
        // the foam line where it lands: on the water, from a little behind the impact to 0.25 m beyond, drifting away
        var land = .004f + LipSpeed * T;
        var F = new Lists();
        const int frows = 4;
        var fidx = new int[frows + 1, cols + 1];
        for (var k = 0; k <= frows; k++)
        {
            var d = -.06f + .31f * k / frows;
            for (var j = 0; j <= cols; j++)
            {
                var u = width * j / cols;
                var edge = Mathf.Clamp01(Mathf.Min(u, width - u) / .06f);
                var cov = edge * Mathf.Clamp01(1f - Mathf.Max(0f, d) / .25f) * Mathf.Clamp01((d + .06f) / .03f);
                fidx[k, j] = F.Vert(a + across * u + outDir * (land + d) + Vector3.up * (landing + .004f), Vector3.up, across, outDir, new Vector2(u, d + .06f), new Color(cov, 1f, 0f, 1f));
            }
        }
        for (var k = 0; k < frows; k++)
            for (var j = 0; j < cols; j++)
                F.Quad(fidx[k, j], fidx[k, j + 1], fidx[k + 1, j + 1], fidx[k + 1, j]);
        var foamGo = F.Finish(go != null ? go.transform : parent, "Foam line · " + name, FoamMat, owned);
        return go != null ? go : foamGo;
    }

    // ---------------------------------------------------------------- a film on a wall, the trough's film

    /// <summary>
    /// A film clinging to the tile along the wall line a..b (district plan points on the face): its path across the flow in the plane
    /// of 'out' and up, as (d, y) points (d metres out from the face, y district height), from where the water leaves to where it
    /// meets the water below (the last point); 3 mm proud of the tile (the path's own d). Rivulets and the joint lines are the
    /// shader's (mode 1); the foam where it meets the water at the path's foot (a band 0.25 m out on that surface).
    /// </summary>
    public static GameObject WallFilm(Transform parent, Vector3 a, Vector3 b, IList<Vector2> path, Vector3 outDir, List<Mesh> owned, string name = "film", int sheetPoints = 0)
    {
        outDir.y = 0f;
        if (outDir.sqrMagnitude < 1e-6f || path == null || path.Count < 2) return null;
        outDir.Normalize();
        a.y = b.y = 0f;
        var along = b - a;
        var width = along.magnitude;
        if (width < .01f) return null;
        var across = along / width;
        var cols = Mathf.Max(1, Mathf.CeilToInt(width / .5f));
        // the path split into rows of at most 0.6 m (long straight runs) with its corners kept
        var pts = new List<Vector2>();
        for (var k = 0; k + 1 < path.Count; k++)
        {
            var seg = Vector2.Distance(path[k], path[k + 1]);
            var n = Mathf.Max(1, Mathf.CeilToInt(seg / .6f));
            for (var i = 0; i < n; i++) pts.Add(Vector2.Lerp(path[k], path[k + 1], i / (float)n));
        }
        pts.Add(path[path.Count - 1]);
        // up to path[sheetPoints] the water runs as a full sheet (over a crest: no rivulets, the steady flow), the film after it
        var sheetArc = -1f;
        if (sheetPoints > 0)
        {
            sheetArc = 0f;
            for (var k = 0; k < Mathf.Min(sheetPoints, path.Count - 1); k++) sheetArc += Vector2.Distance(path[k], path[k + 1]);
        }
        var L = new Lists();
        var idx = new int[pts.Count, cols + 1];
        var arc = 0f;
        for (var k = 0; k < pts.Count; k++)
        {
            if (k > 0) arc += Vector2.Distance(pts[k - 1], pts[k]);
            // the film's face: square to its path, on the side away from the tile (out, or down under a lip)
            var dir = k + 1 < pts.Count ? pts[k + 1] - pts[k] : pts[k] - pts[k - 1];
            dir.Normalize();
            var n2 = new Vector2(dir.y, -dir.x);
            if (n2.x < -.01f || (Mathf.Abs(n2.x) <= .01f && n2.y > 0f)) n2 = -n2;
            var nrm = (outDir * n2.x + Vector3.up * n2.y).normalized;
            var flow = (outDir * dir.x + Vector3.up * dir.y).normalized;
            for (var j = 0; j <= cols; j++)
            {
                var u = width * j / cols;
                var edge = Mathf.Clamp01(Mathf.Min(u, width - u) / .05f);
                var foam = k == pts.Count - 1 ? .5f : 0f;
                var mode = arc <= sheetArc + 1e-4f ? .5f : 1f;
                idx[k, j] = L.Vert(a + across * u + outDir * pts[k].x + Vector3.up * pts[k].y, nrm, across, flow, new Vector2(u, arc), new Color(edge, foam, mode, 1f));
            }
        }
        for (var k = 0; k + 1 < pts.Count; k++)
            for (var j = 0; j < cols; j++)
                L.Quad(idx[k, j], idx[k, j + 1], idx[k + 1, j + 1], idx[k + 1, j]);
        var go = L.Finish(parent, "Fall · " + name + " (NEW: film on the tile)", FilmMat, owned);
        var foot = path[path.Count - 1];
        var F = new Lists();
        var fidx = new int[3, cols + 1];
        for (var k = 0; k <= 2; k++)
        {
            var d = .25f * k / 2f;
            for (var j = 0; j <= cols; j++)
            {
                var u = width * j / cols;
                var edge = Mathf.Clamp01(Mathf.Min(u, width - u) / .08f);
                fidx[k, j] = F.Vert(a + across * u + outDir * (foot.x + .002f + d) + Vector3.up * (foot.y + .004f), Vector3.up, across, outDir, new Vector2(u, d), new Color(edge * (1f - d / .25f), 1f, 0f, 1f));
            }
        }
        for (var k = 0; k < 2; k++)
            for (var j = 0; j < cols; j++)
                F.Quad(fidx[k, j], fidx[k, j + 1], fidx[k + 1, j + 1], fidx[k + 1, j]);
        F.Finish(go != null ? go.transform : parent, "Foam · " + name, FoamMat, owned);
        return go;
    }

    /// <summary>
    /// The slide's film (the F key): the water riding down the trough, along the owners' riding line (p, right, up, s), piece by piece
    /// (samples i0[k]..i1[k], the trough's round bottom of radius radius[k]: 0.45 the open trough, 0.60 the tube): a film 15 mm deep
    /// on the bottom thinning to nothing up the sides (half-width 0.26 m on the trough, 0.30 m in the tube), running with the ride
    /// (mode 0.5, about 5 m/s); its ends fade over 0.3 m (the tub's spill, the exit lip). One renderer.
    /// </summary>
    public static GameObject TroughFilm(Transform parent, IList<Vector3> p, IList<Vector3> right, IList<Vector3> up, IList<float> s, IList<int> i0, IList<int> i1, IList<float> radius, List<Mesh> owned)
    {
        if (p == null || i0 == null || i0.Count == 0) return null;
        var L = new Lists();
        const int w = 9;
        var first = s[i0[0]];
        var total = s[i1[i1.Count - 1]] - first;
        for (var k = 0; k < i0.Count; k++)
        {
            var r = radius[k];
            var hw = r > .5f ? .30f : .26f;
            var sample = new List<int>();
            var lastS = -1f;
            for (var i = i0[k]; i <= i1[k]; i++)
                if (i == i0[k] || i == i1[k] || s[i] - lastS >= .3f) { sample.Add(i); lastS = s[i]; }
            var idx = new int[sample.Count, w];
            for (var q = 0; q < sample.Count; q++)
            {
                var i = sample[q];
                var along = s[i] - first;
                var ends = Mathf.Clamp01(along / .3f) * Mathf.Clamp01((total - along) / .3f);
                var t = Vector3.Cross(right[i], up[i]).normalized;
                if (i + 1 < p.Count && Vector3.Dot(t, p[i + 1] - p[i]) < 0f) t = -t;
                for (var c = 0; c < w; c++)
                {
                    var x = -hw + 2f * hw * c / (w - 1);
                    var f = x / hw;
                    var y = r - Mathf.Sqrt(Mathf.Max(0f, r * r - x * x)) + .015f * (1f - f * f);
                    var e = c == 0 || c == w - 1 ? .35f : 1f;
                    idx[q, c] = L.Vert(p[i] + right[i] * x + up[i] * y, up[i], right[i], t, new Vector2(x + hw, s[i]), new Color(ends * e, .06f, .5f, 4f));
                }
            }
            for (var q = 0; q + 1 < sample.Count; q++)
                for (var c = 0; c + 1 < w; c++)
                    L.Quad(idx[q, c], idx[q, c + 1], idx[q + 1, c + 1], idx[q + 1, c]);
        }
        return L.Finish(parent, "Flume film (NEW: running water)", TroughMat, owned);
    }

    // ---------------------------------------------------------------- the weir lip (the owners append it with P2's WeirLip tile)

    /// <summary>
    /// P1's square porcelain weir lip (FrontRoomsStructureProfiles.WeirLip: 150 mm on the crest, a 60 mm nose, the 6 x 6 mm drip
    /// groove) along a..b with its top at 'crest' and its nose on the wall's face where the water leaves 'out'; ends closed (G4).
    /// District metres; UV as P2's WeirLip map reads it (u metres along, v the section's own V). The owners append it with
    /// FrontRoomsPoolTile.Get("WeirLip") and set their wall's top 60 mm lower under it.
    /// </summary>
    public static void WeirLip(Vector3 a, Vector3 b, float crest, Vector3 outDir, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t)
    {
        outDir.y = 0f;
        a.y = b.y = crest;
        if (outDir.sqrMagnitude < 1e-6f || (b - a).sqrMagnitude < 1e-6f) return;
        // the section's +x is the sweep's plumb right (Cross(up, along)): walk the lip so that it points out
        if (Vector3.Dot(Vector3.Cross(Vector3.up, b - a), outDir) < 0f) { var sw = a; a = b; b = sw; }
        var sec = FrontRoomsStructureProfiles.WeirLip();
        var o = FrontRoomsStructureProfiles.SweepOptions.Default;
        o.plumb = true;
        o.u0 = 0f;
        var start = v.Count;
        FrontRoomsStructureProfiles.Line(v, n, uv, t, a, b, Vector3.up, sec, o);
        // the two ends: the section closed by its hidden bottom (a straight line from the last point back to the first)
        var along = (b - a).normalized;
        var right = Vector3.Cross(Vector3.up, along).normalized;
        var poly = sec.P;
        var order = FrontRoomsStructureProfiles.EarClip(poly);
        for (var e = 0; e < 2; e++)
        {
            var at = e == 0 ? a : b;
            var outward = e == 0 ? -along : along;
            var first = v.Count;
            foreach (var q in poly) { v.Add(at + right * q.x + Vector3.up * q.y); n.Add(outward); uv.Add(new Vector2(q.x, q.y)); }
            for (var i = 0; i + 2 < order.Count; i += 3)
            {
                int i0 = first + order[i], i1 = first + order[i + 1], i2 = first + order[i + 2];
                var f = Vector3.Cross(v[i1] - v[i0], v[i2] - v[i0]);
                if (Vector3.Dot(f, outward) >= 0f) { t.Add(i0); t.Add(i1); t.Add(i2); } else { t.Add(i0); t.Add(i2); t.Add(i1); }
            }
        }
    }
}
