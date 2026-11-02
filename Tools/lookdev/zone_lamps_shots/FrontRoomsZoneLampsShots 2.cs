// ZONE LAMPS SHOTS: one zone option's renders in the real game (zone_lamps workflow, design + build stage, 2026-10-31).
// CLONE ONLY (W/proj_zonelamps); never lands in main. ASCII only, no C# tuples, no GetInstanceID.
// Cut down from lamp_options' FrontRoomsLampOptionsShots. Random.InitState(4242) before the run, the Relay held.
// Map zones (office, tall, low): the option replaces the troffer on every lamp of that zone near the shots by the swap rule of
// lamp_options 10c s.12 (the lens object keeps its name, renderer and property block; only its mesh, material and position
// change; the housing is a new unlit sibling; L03's own housing and soil are hidden; no map Light is written).
// Stand-ins (pool, run): built far from the map in the same scene (same ambient, haze, post); every option of a zone shares
// the room, the cameras and the exposure. The pool room is a STAND-IN ROOM (no pool zone exists in the map).
// Run: Unity -batchmode -projectPath W/proj_zonelamps -buildTarget OSXUniversal -executeMethod FrontRoomsZoneLampsShots.RunBatch
//      -zlZone office|tall|low|pool|run -zlOption <id> [-zlPhone 1] -frGlassRT off -logFile <f>   (exits by itself)
// Output: zone_lamps/renders/<zone>/<option>/<pose>.jpg (q95) + _work/<zone>_<option>_<tier>/ log, shots.tsv.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using FrontRooms.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

[InitializeOnLoad]
public static partial class FrontRoomsZoneLampsShots
{
    const string ScenePath = "Assets/Scenes/FrontRooms3D.unity";
    const string ActiveKey = "FrontRooms.ZoneLamps.Active", DeadlineKey = "FrontRooms.ZoneLamps.Deadline", DoneKey = "FrontRooms.ZoneLamps.Done";
    const string ZoneKey = "FrontRooms.ZoneLamps.Zone", OptKey = "FrontRooms.ZoneLamps.Option", PhoneKey = "FrontRooms.ZoneLamps.Phone";
    const string RenderRoot = "/Users/redwang/FrontRoomsVisualWork/level_space/renders/";
    const string ZL = "Assets/Resources/Props/ZoneLamps/";
    const string LO = "Assets/Resources/Props/LampOptions/";
    const BindingFlags BF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    const uint BasinLayer = 1u << 7;

    static int W = 1920, H = 1080;
    static int waitUntilFrame = -1, baseFrame;
    static bool finished;
    static string outDir, workDir;
    static readonly StringBuilder log = new StringBuilder();
    static readonly StringBuilder rows = new StringBuilder();
    static readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
    static int fail;

    static FrontRooms3DGame game;
    static FrontRoomsMapWorld map;
    static Camera cam;
    static RenderTexture rt;
    static Texture2D readback;
    static Vector3 shotPos;
    static Quaternion shotRot;
    static float shotFov = -1f, gameFov = 60f;
    static Action preRender;

    static string Zone { get { return SessionState.GetString(ZoneKey, "office"); } }
    static string Opt { get { return SessionState.GetString(OptKey, "base"); } }
    static bool Phone { get { return SessionState.GetBool(PhoneKey, false); } }

    static FrontRoomsZoneLampsShots()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (Phone)
        {
            var p = FrontRoomsHandheld.IPhonePro;
            W = p.Pixels.x; H = p.Pixels.y;
            FrontRoomsHandheld.Force(p);
        }
        EditorApplication.update += Tick;
    }

    public static void RunBatch()
    {
        var args = Environment.GetCommandLineArgs();
        string zone = "office", opt = "base";
        var phone = false;
        for (var i = 0; i + 1 < args.Length; i++)
        {
            if (args[i] == "-zlZone") zone = args[i + 1];
            if (args[i] == "-zlOption") opt = args[i + 1];
            if (args[i] == "-zlPhone") phone = args[i + 1] == "1";
        }
        SessionState.SetString(ZoneKey, zone);
        SessionState.SetString(OptKey, opt);
        SessionState.SetBool(PhoneKey, phone);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetFloat(DeadlineKey, (float)EditorApplication.timeSinceStartup + 1500f);
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    static string OutDir
    {
        get { if (outDir == null) { outDir = RenderRoot + Zone + "/" + Opt + "/"; Directory.CreateDirectory(outDir); } return outDir; }
    }

    static string WorkDir
    {
        get { if (workDir == null) { workDir = RenderRoot + "_work/" + Zone + "_" + Opt + (Phone ? "_phone" : "_desk") + "/"; Directory.CreateDirectory(workDir); } return workDir; }
    }

    static string F3(float f) { return f.ToString("0.000", CultureInfo.InvariantCulture); }
    static string V3(Vector3 v) { return F3(v.x) + "," + F3(v.y) + "," + F3(v.z); }

    static void Log(string s)
    {
        var line = "[" + (Time.frameCount - baseFrame) + "] " + s;
        log.AppendLine(line);
        Debug.Log("[ZoneLamps] " + line);
        try { File.WriteAllText(WorkDir + "log.txt", log.ToString()); } catch (Exception) { }
    }

    static void Check(bool ok, string what) { if (!ok) fail++; Log((ok ? "PASS " : "FAIL ") + what); }

    static void Finish()
    {
        RestoreUrp();
        finished = true;
        Time.captureDeltaTime = 0f;
        SessionState.SetBool(DoneKey, true);
        Log("FINISHED fail=" + fail);
    }

    static void Tick()
    {
        var timedOut = EditorApplication.timeSinceStartup > SessionState.GetFloat(DeadlineKey, float.MaxValue);
        if (finished || timedOut || SessionState.GetBool(DoneKey, false))
        {
            if (timedOut && !finished && !SessionState.GetBool(DoneKey, false)) { Log("TIMEOUT"); fail++; Finish(); }
            if (EditorApplication.isPlaying) { EditorApplication.ExitPlaymode(); return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorApplication.update -= Tick;
            SessionState.EraseBool(ActiveKey);
            SessionState.EraseBool(DoneKey);
            if (Application.isBatchMode) EditorApplication.Exit(fail == 0 ? 0 : 3);
            return;
        }
        if (!EditorApplication.isPlaying) return;
        if (stack.Count == 0) stack.Push(Main());
        if (Time.frameCount < waitUntilFrame) return;
        try
        {
            for (var guard = 0; guard < 1000; guard++)
            {
                var top = stack.Peek();
                if (!top.MoveNext())
                {
                    stack.Pop();
                    if (stack.Count == 0) { Finish(); return; }
                    continue;
                }
                var cur = top.Current;
                var sub = cur as IEnumerator;
                if (sub != null) { stack.Push(sub); continue; }
                var n = cur is int ? (int)cur : 1;
                waitUntilFrame = Time.frameCount + Mathf.Max(1, n);
                return;
            }
        }
        catch (Exception e) { Log("EXCEPTION " + e); fail++; Finish(); }
    }

    // ------------------------------------------------------------------------------------------- reflection helpers
    static FieldInfo FI(Type t, string n)
    {
        for (; t != null; t = t.BaseType) { var f = t.GetField(n, BF); if (f != null) return f; }
        return null;
    }
    static object Get(object o, string n) { var f = FI(o.GetType(), n); return f != null ? f.GetValue(o) : null; }
    static T1 Get<T1>(object o, string n) { var v = Get(o, n); return v is T1 ? (T1)v : default(T1); }
    static void Set(object o, string n, object v) { var f = FI(o.GetType(), n); if (f != null) f.SetValue(o, v); }
    static object Call(object o, string n, params object[] a) { return o.GetType().GetMethod(n, BF).Invoke(o, a); }
    static string GamePhase { get { return Get(game, "phase").ToString(); } }
    static Transform PlayerRoot { get { return Get<Transform>(game, "playerRoot"); } }

    static void Resume()
    {
        if (GamePhase != "Paused") return;
        var phaseType = typeof(FrontRooms3DGame).GetNestedType("Phase", BindingFlags.NonPublic);
        Call(game, "SetPhase", Enum.Parse(phaseType, "Playing"));
    }

    static void Place(Vector3 feet, Vector3 lookAt)
    {
        var r = PlayerRoot;
        var body = Get<CharacterController>(game, "playerBody");
        body.enabled = false;
        r.position = feet;
        body.enabled = true;
        Physics.SyncTransforms();
        var e = r.position + Vector3.up * ModuleUnits.PlayerEye;
        var v = lookAt - e;
        var yaw = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
        var pitch = -Mathf.Asin(Mathf.Clamp(v.y / Mathf.Max(1e-4f, v.magnitude), -1f, 1f)) * Mathf.Rad2Deg;
        Set(game, "yaw", yaw);
        Set(game, "pitch", pitch);
        r.rotation = Quaternion.Euler(0f, yaw, 0f);
        Get<FrontRoomsCameraRig>(game, "rig").SetBase(pitch);
    }

    static IEnumerator Settle(int maxFrames)
    {
        var start = Time.frameCount;
        while (!map.Settled && Time.frameCount - start < maxFrames) { Resume(); yield return 1; }
    }

    static IEnumerator Frames(int n) { for (var i = 0; i < n; i++) { Resume(); yield return 1; } }

    static void SetPreset(FrontRoomsQuality.Preset p)
    {
        FrontRoomsQuality.Apply();
        var m = typeof(FrontRoomsQuality).GetMethod("SetDetailsFrom", SF);
        var prop = typeof(FrontRoomsQuality).GetProperty("CurrentPreset", SF);
        if (m != null) m.Invoke(null, new object[] { p });
        if (prop != null) prop.SetValue(null, p, null);
        FrontRoomsQuality.Apply();
    }

    static string QualityLine()
    {
        return QualitySettings.names[QualitySettings.GetQualityLevel()] + "/" + FrontRoomsQuality.CurrentPreset + " | handheld " + FrontRoomsHandheld.Active + " | fov " + F3(cam.fieldOfView);
    }

    // ------------------------------------------------------------------------------------------- kit variants
    sealed class Variant
    {
        public string name;
        public Mesh lens, phone;
        public Material[] mats;
        public GameObject housing;
        public string cut;
        public float hang;
        public Vector3 offset;
        public Quaternion rot = Quaternion.identity;
        public bool canShift, hideLens;
        public float drive = 1.8f;
    }

    static GameObject LoadPrefab(string path)
    {
        var g = AssetDatabase.LoadAssetAtPath<GameObject>(path + ".prefab");
        if (g == null) throw new Exception("missing prefab " + path);
        return g;
    }

    static Variant FromPrefab(string prefab, string phonePrefab)
    {
        var p = LoadPrefab(prefab);
        var lens = p.transform.Find("Lens");
        var v = new Variant { name = Path.GetFileName(prefab) };
        v.lens = lens.GetComponent<MeshFilter>().sharedMesh;
        v.mats = lens.GetComponent<MeshRenderer>().sharedMaterials;
        var h = p.transform.Find("Housing");
        v.housing = h != null ? h.gameObject : null;
        v.phone = phonePrefab != null ? LoadPrefab(phonePrefab).transform.Find("Lens").GetComponent<MeshFilter>().sharedMesh : v.lens;
        return v;
    }

    static readonly Dictionary<string, Mesh> cutMeshes = new Dictionary<string, Mesh>();

    static void LoadCuts()
    {
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(LO + "Models/LampOpt_CeilingCuts.fbx")) { var m = o as Mesh; if (m != null) cutMeshes[m.name] = m; }
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(ZL + "Models/ZL_MHDL.fbx")) { var m = o as Mesh; if (m != null && m.name.StartsWith("CeilingCut")) cutMeshes[m.name] = m; }
    }

    static Variant MapVariant()
    {
        var o = Opt;
        Variant v = null;
        if (o == "base") return null;
        if (o == "L07" || o == "L06" || o == "L09")
        {
            v = FromPrefab(LO + "Prefabs/LampOpt_" + o, LO + "Prefabs/LampOpt_" + o + "_Phone");
            v.cut = o == "L07" ? "CeilingCut_2x2" : o == "L06" ? "CeilingCut_2x4" : null;
            v.offset = o == "L07" ? new Vector3(0f, 0f, -.3f) : o == "L09" ? new Vector3(-.15f, 0f, 0f) : Vector3.zero;
            v.hang = v.cut != null ? 0f : v.lens.bounds.max.y + .001f;
        }
        else if (o == "PEND")
        {
            v = FromPrefab(ZL + "Prefabs/ZL_PEND", ZL + "Prefabs/ZL_PEND_Phone");
            v.drive = 1.4f;
        }
        else if (o == "MHDL")
        {
            v = FromPrefab(ZL + "Prefabs/ZL_MHDL", ZL + "Prefabs/ZL_MHDL_Phone");
            v.cut = "CeilingCut_MH";
            v.canShift = true;
            v.drive = 1.8f;
        }
        else if (o == "HIDUP")
        {
            v = new Variant { name = "HIDUP (cell lens removed)", hideLens = true };
        }
        else if (o == "WRAP")
        {
            // the Run! B / stairwell kit (Kit_WrapFixture_4ft) surface-mounted on the lay-in grid, long axis along the cell
            var kit = AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/Props/Models/Kit_WrapFixture_4ft.fbx");
            Mesh l0 = null, l2 = null;
            foreach (var a in kit)
            {
                var m = a as Mesh;
                if (m == null) continue;
                if (l0 == null || m.vertexCount > l0.vertexCount) l0 = m;
                if (l2 == null || m.vertexCount < l2.vertexCount) l2 = m;
            }
            var lensMat = AssetDatabase.LoadAssetAtPath<Material>(ZL + "Materials/ZL_WrapLens_R1.mat");
            var pm = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Surfaces/Painted_Metal.mat");
            var al = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Surfaces/Prop_SteelAlmond.mat");
            v = new Variant { name = "Kit_WrapFixture_4ft", lens = l0, phone = l2, mats = new[] { pm, lensMat, al }, rot = Quaternion.Euler(0f, 90f, 0f), drive = 1.8f };
            Log("WRAP meshes: lod0 " + l0.name + " " + l0.triangles.Length / 3 + " tris, " + l0.subMeshCount + " submeshes; phone " + l2.name + " " + l2.triangles.Length / 3);
        }
        else throw new Exception("unknown map option " + o);
        Log("variant " + v.name + " | lens " + (v.lens != null ? v.lens.name + " " + v.lens.triangles.Length / 3 + " tris" : "none") + " | cut " + (v.cut ?? "none") + " | hang " + F3(v.hang) + " | drive " + F3(v.drive));
        return v;
    }

    // ------------------------------------------------------------------------------------------- map swap
    static IEnumerable<object> MapFixtures()
    {
        var built = Get(map, "built") as IDictionary;
        if (built == null) yield break;
        foreach (var chunk in built.Values)
        {
            var fixtures = Get(chunk, "fixtures") as IList;
            if (fixtures == null) continue;
            foreach (var f in fixtures) yield return f;
        }
    }

    static bool ZoneMatch(GridCoord c)
    {
        if (map.IsReserved(c) || map.InStation(c)) return false;
        var z = map.ZoneOf(c);
        switch (Zone)
        {
            case "office": return z.theme == ZoneTheme.Office;
            case "tall": return z.height == ZoneHeight.Tall;
            case "low": return z.height == ZoneHeight.Low && z.theme == ZoneTheme.Level0;
        }
        return false;
    }

    static Color Normalised(Color c)
    {
        var m = Mathf.Max(1e-4f, Mathf.Max(c.r, Mathf.Max(c.g, c.b)));
        return new Color(c.r / m, c.g / m, c.b / m, 1f);
    }

    static Vector3 OffsetFor(Variant v, Vector3 worldCentre)
    {
        if (!v.canShift) return v.offset;
        var d = Mathf.Repeat(worldCentre.z, 1.2f);
        return Mathf.Abs(d - .6f) < .15f ? Vector3.zero : new Vector3(0f, 0f, -.15f);
    }

    static readonly Dictionary<int, Color> origEmission = new Dictionary<int, Color>();
    static readonly HashSet<int> copiedMeshes = new HashSet<int>();
    static int swapped, cutsDone, cutsMissed;

    static void ApplyLamp(Transform root, Renderer lensR, Variant v, bool phoneTier, Vector3 off)
    {
        // L03's own housing and soil go; the lens renderer stays (the lamp logic's renderer)
        var l3h = root.Find("Housing");
        if (l3h != null && l3h.gameObject.activeSelf) l3h.gameObject.SetActive(false);
        var l3s = root.Find("Soil");
        if (l3s != null && l3s.gameObject.activeSelf) l3s.gameObject.SetActive(false);
        if (v.hideLens) { lensR.enabled = false; return; }
        var mf = lensR.GetComponent<MeshFilter>();
        var mesh = phoneTier ? v.phone : v.lens;
        if (mf.sharedMesh != mesh) mf.sharedMesh = mesh;
        var use = new Material[mesh.subMeshCount];
        for (var i = 0; i < use.Length; i++) use[i] = v.mats[Mathf.Min(i, v.mats.Length - 1)];
        lensR.sharedMaterials = use;
        lensR.shadowCastingMode = ShadowCastingMode.Off;
        var p = new Vector3(off.x, -v.hang, off.z);
        lensR.transform.localPosition = p;
        lensR.transform.localRotation = v.rot;
        lensR.transform.localScale = Vector3.one;
        var h = root.Find("ZL Housing");
        if (!phoneTier && v.housing != null)
        {
            if (h == null)
            {
                var go = UnityEngine.Object.Instantiate(v.housing, root, false);
                go.name = "ZL Housing";
                h = go.transform;
            }
            h.localPosition = p;
            h.localRotation = v.rot;
            if (!h.gameObject.activeSelf) h.gameObject.SetActive(true);
        }
        else if (h != null && h.gameObject.activeSelf) h.gameObject.SetActive(false);
    }

    sealed class CutReq { public Transform root; public Rect cell; public float y; public Vector3 centre; public string cut; public Renderer src; public int removed; }

    static void AddFill(List<Vector3> vs, List<int> ts, Rect r, float y)
    {
        var i = vs.Count;
        vs.Add(new Vector3(r.xMin, y, r.yMin)); vs.Add(new Vector3(r.xMax, y, r.yMin));
        vs.Add(new Vector3(r.xMax, y, r.yMax)); vs.Add(new Vector3(r.xMin, y, r.yMax));
        ts.Add(i); ts.Add(i + 1); ts.Add(i + 2);
        ts.Add(i); ts.Add(i + 2); ts.Add(i + 3);
    }

    static Mesh FillMesh(Rect r, List<Rect> holes, float y)
    {
        var xs = new List<float> { r.xMin, r.xMax };
        var zs = new List<float> { r.yMin, r.yMax };
        foreach (var h in holes)
        {
            xs.Add(Mathf.Clamp(h.xMin, r.xMin, r.xMax)); xs.Add(Mathf.Clamp(h.xMax, r.xMin, r.xMax));
            zs.Add(Mathf.Clamp(h.yMin, r.yMin, r.yMax)); zs.Add(Mathf.Clamp(h.yMax, r.yMin, r.yMax));
        }
        xs.Sort(); zs.Sort();
        var vs = new List<Vector3>();
        var ts = new List<int>();
        for (var i = 0; i + 1 < xs.Count; i++)
        for (var j = 0; j + 1 < zs.Count; j++)
        {
            if (xs[i + 1] - xs[i] < 1e-4f || zs[j + 1] - zs[j] < 1e-4f) continue;
            var c = new Vector2((xs[i] + xs[i + 1]) * .5f, (zs[j] + zs[j + 1]) * .5f);
            var inHole = false;
            foreach (var h in holes) if (h.Contains(c)) { inHole = true; break; }
            if (inHole) continue;
            AddFill(vs, ts, Rect.MinMaxRect(xs[i], zs[j], xs[i + 1], zs[j + 1]), y);
        }
        var m = new Mesh { name = "ZL ceiling fill" };
        m.SetVertices(vs);
        m.SetTriangles(ts, 0);
        var n = new Vector3[vs.Count];
        var t = new Vector4[vs.Count];
        var uv = new Vector2[vs.Count];
        for (var i = 0; i < vs.Count; i++) { n[i] = Vector3.down; t[i] = new Vector4(1f, 0f, 0f, 1f); uv[i] = new Vector2(vs[i].x, vs[i].z); }
        m.normals = n; m.tangents = t; m.uv = uv;
        m.RecalculateBounds();
        return m;
    }

    static GameObject MakeCeilingPiece(string name, Mesh mesh, Vector3 worldPos, Renderer src, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.position = worldPos;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = src.sharedMaterial;
        r.shadowCastingMode = src.shadowCastingMode;
        r.receiveShadows = src.receiveShadows;
        r.renderingLayerMask = src.renderingLayerMask;
        if (src.HasPropertyBlock()) { var b = new MaterialPropertyBlock(); src.GetPropertyBlock(b); r.SetPropertyBlock(b); }
        go.transform.SetParent(parent, true);
        return go;
    }

    static Rect PanelRect(Vector3 c) { return new Rect(c.x - .6f, c.z - .9f, 1.2f, 1.8f); }

    static void CutMap(List<CutReq> reqs)
    {
        if (reqs.Count == 0) return;
        var byChunk = new Dictionary<Transform, List<CutReq>>();
        foreach (var q in reqs)
        {
            List<CutReq> l;
            if (!byChunk.TryGetValue(q.root.parent, out l)) byChunk[q.root.parent] = l = new List<CutReq>();
            l.Add(q);
        }
        foreach (var kv in byChunk)
        {
            var chunk = kv.Key;
            for (var ci = 0; ci < chunk.childCount; ci++)
            {
                var block = chunk.GetChild(ci);
                if (!block.name.StartsWith("Block ")) continue;
                foreach (var mr in block.GetComponentsInChildren<MeshRenderer>())
                {
                    var bb = mr.bounds;
                    var any = false;
                    foreach (var q in kv.Value)
                        if (bb.min.x <= q.cell.xMax && bb.max.x >= q.cell.xMin && bb.min.z <= q.cell.yMax && bb.max.z >= q.cell.yMin && bb.min.y <= q.y + .01f && bb.max.y >= q.y - .01f) { any = true; break; }
                    if (!any) continue;
                    var mf = mr.GetComponent<MeshFilter>();
                    var mesh = mf.sharedMesh;
                    if (mesh == null || !mesh.isReadable) continue;
                    var verts = mesh.vertices;
                    var l2w = mr.transform.localToWorldMatrix;
                    var wv = new Vector3[verts.Length];
                    for (var i = 0; i < verts.Length; i++) wv[i] = l2w.MultiplyPoint3x4(verts[i]);
                    var changed = false;
                    var newTris = new List<int[]>();
                    for (var s = 0; s < mesh.subMeshCount; s++)
                    {
                        var tris = mesh.GetTriangles(s);
                        var keep = new List<int>(tris.Length);
                        for (var t = 0; t + 2 < tris.Length; t += 3)
                        {
                            Vector3 a = wv[tris[t]], b2 = wv[tris[t + 1]], c = wv[tris[t + 2]];
                            var n = Vector3.Cross(b2 - a, c - a);
                            var drop = false;
                            if (n.sqrMagnitude > 1e-10f && n.normalized.y < -.9f)
                            {
                                var cen = (a + b2 + c) / 3f;
                                foreach (var q in kv.Value)
                                {
                                    if (Mathf.Abs(cen.y - q.y) > .003f) continue;
                                    if (cen.x <= q.cell.xMin + .01f || cen.x >= q.cell.xMax - .01f || cen.z <= q.cell.yMin + .01f || cen.z >= q.cell.yMax - .01f) continue;
                                    drop = true; q.removed++; q.src = mr;
                                    break;
                                }
                            }
                            if (drop) { changed = true; continue; }
                            keep.Add(tris[t]); keep.Add(tris[t + 1]); keep.Add(tris[t + 2]);
                        }
                        newTris.Add(keep.ToArray());
                    }
                    if (!changed) continue;
                    if (!copiedMeshes.Contains(FrontRoomsObjectId.Of(mesh)))
                    {
                        mesh = UnityEngine.Object.Instantiate(mesh);
                        mesh.name = mf.sharedMesh.name + " (zl cut)";
                        copiedMeshes.Add(FrontRoomsObjectId.Of(mesh));
                        mf.sharedMesh = mesh;
                    }
                    for (var s = 0; s < newTris.Count; s++) mesh.SetTriangles(newTris[s], s, false);
                }
            }
        }
        foreach (var q in reqs)
        {
            var marker = new GameObject("ZL cut");
            marker.transform.SetParent(q.root, false);
            if (q.src == null || q.removed == 0) { cutsMissed++; continue; }
            var holes = new List<Rect> { PanelRect(q.centre) };
            MakeCeilingPiece("ZL ceiling fill", FillMesh(q.cell, holes, q.y), Vector3.zero, q.src, marker.transform);
            MakeCeilingPiece("ZL cut panel", cutMeshes[q.cut], new Vector3(q.centre.x, q.y, q.centre.z), q.src, marker.transform);
            cutsDone++;
        }
    }

    static Variant mapV;

    static int SweepMap(bool phoneTier, Vector3 around, float radius)
    {
        if (mapV == null) return 0;
        var n = 0;
        var reqs = new List<CutReq>();
        foreach (var f in MapFixtures())
        {
            var panel = Get<Renderer>(f, "panel");
            if (panel == null) continue;
            var root = panel.transform.parent;
            if ((root.position - around).sqrMagnitude > radius * radius) continue;
            var cell = (GridCoord)Get(f, "cell");
            if (!ZoneMatch(cell)) continue;
            var key = FrontRoomsObjectId.Of(panel);
            Color orig;
            if (!origEmission.TryGetValue(key, out orig)) { orig = (Color)Get(f, "emission"); origEmission[key] = orig; }
            var off = OffsetFor(mapV, root.position);
            ApplyLamp(root, panel, mapV, phoneTier, off);
            var e = Normalised(orig) * mapV.drive;
            e.a = 1f;
            Set(f, "emission", e);
            if (mapV.cut != null && root.Find("ZL cut") == null)
            {
                var cc = root.position - new Vector3(0f, 0f, ModuleUnits.TrofferOffsetZ);
                reqs.Add(new CutReq { root = root, cell = new Rect(cc.x - 1.5f, cc.z - 1.5f, 3f, 3f), y = root.position.y, centre = root.TransformPoint(off), cut = mapV.cut });
            }
            n++;
        }
        CutMap(reqs);
        swapped = Mathf.Max(swapped, n);
        return n;
    }

    static int PinClocks()
    {
        var n = 0;
        foreach (var f in MapFixtures()) { if (Get<int>(f, "mode") != 0) continue; Set(f, "clock", 40f); n++; }
        return n;
    }

    // ------------------------------------------------------------------------------------------- render
    static void EnsureRT()
    {
        if (rt != null) return;
        var msaa = 1;
        var urp = GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
        if (urp != null) msaa = Mathf.Max(1, urp.msaaSampleCount);
        rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = msaa, name = "ZL capture" };
        rt.Create();
        readback = new Texture2D(W, H, TextureFormat.RGB24, false);
    }

    static void Aim(Vector3 eye, Vector3 target, float fov)
    {
        shotPos = eye;
        var d = target - eye;
        var up = Mathf.Abs(Vector3.Dot(d.normalized, Vector3.up)) > .98f ? Vector3.forward : Vector3.up;
        shotRot = Quaternion.LookRotation(d, up);
        shotFov = fov;
    }

    static void AimRot(Vector3 eye, Quaternion rot, float fov) { shotPos = eye; shotRot = rot; shotFov = fov; }

    static void RenderNow()
    {
        if (preRender != null) preRender();
        cam.transform.SetPositionAndRotation(shotPos, shotRot);
        cam.targetTexture = rt;
        cam.fieldOfView = shotFov > 0f ? shotFov : gameFov;
        var s = UnityEngine.Random.state;
        UnityEngine.Random.InitState(9001);
        cam.Render();
        UnityEngine.Random.InitState(9001);
        cam.Render();
        UnityEngine.Random.state = s;
    }

    static void Shot(string pose, string note)
    {
        RenderNow();
        var active = RenderTexture.active;
        RenderTexture.active = rt;
        readback.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        readback.Apply(false);
        RenderTexture.active = active;
        var file = pose + (Phone ? "_phone" : "") + ".jpg";
        File.WriteAllBytes(OutDir + file, readback.EncodeToJPG(95));
        var lights = 0;
        foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l != null && l.isActiveAndEnabled && (l.transform.position - shotPos).sqrMagnitude < 30f * 30f) lights++;
        rows.Append(Zone).Append('\t').Append(Opt).Append('\t').Append(Phone ? "phone" : "desktop").Append('\t').Append(file).Append('\t').Append(W + "x" + H).Append('\t')
            .Append(QualityLine()).Append('\t').Append(V3(shotPos)).Append('\t').Append(V3(shotRot.eulerAngles)).Append('\t').Append(lights).Append('\t').Append(note).Append('\n');
        File.WriteAllText(WorkDir + "shots.tsv", "zone\toption\ttier\tfile\tres\tquality\tcam_pos\tcam_euler\tlights_30m\tnote\n" + rows);
        Log("saved " + file + " | " + QualityLine() + " | lights " + lights + " | " + note);
    }

    // ------------------------------------------------------------------------------------------- main
    static IEnumerator Main()
    {
        ShaderUtil.allowAsyncCompilation = false;
        var q = Array.IndexOf(QualitySettings.names, Phone ? "Medium" : "High");
        if (q >= 0) QualitySettings.SetQualityLevel(q, true);
        while ((game = UnityEngine.Object.FindAnyObjectByType<FrontRooms3DGame>()) == null) yield return 1;
        baseFrame = Time.frameCount;
        cam = Get<Camera>(game, "cam");
        SetPreset(Phone ? FrontRoomsQuality.Preset.Medium : FrontRoomsQuality.Preset.High);
        EnsureRT();
        Time.captureDeltaTime = 1f / 60f;
        Log("start | zone " + Zone + " | option " + Opt + " | phone " + Phone + " | " + W + "x" + H + " | " + QualityLine() + " | gpu " + SystemInfo.graphicsDeviceName);
        LoadCuts();
        yield return 60;
        UnityEngine.Random.InitState(4242);
        for (var tries = 0; tries < 60 && !Get<bool>(game, "mapPlay"); tries++)
        {
            Call(game, "RequestTitleStart");
            if (!Get<bool>(game, "mapPlay")) yield return 15;
        }
        Check(Get<bool>(game, "mapPlay"), "a run started");
        if (!Get<bool>(game, "mapPlay")) yield break;
        map = Get<FrontRoomsMapWorld>(game, "map");
        Get<FrontRoomsHunterTuning>(game, "hunterTuning").releaseDelaySeconds = 1e6f;
        Log("run started | seed " + Get<int>(game, "runSeed"));
        yield return Settle(1500);
        gameFov = cam.fieldOfView;
        SetPreset(Phone ? FrontRoomsQuality.Preset.Medium : FrontRoomsQuality.Preset.High);
        if (Zone == "levelspace") { yield return LevelSpaceMain(); yield break; }
        if (Zone == "pooldir") { yield return PoolDirMain(); yield break; }
        if (Zone == "pool") { yield return PoolMain(); yield break; }
        if (Zone == "run") { yield return RunMain(); yield break; }
        yield return MapMain();
    }

    // =========================================================================================== map zones
    sealed class Lamp { public object f; public Transform root; public GridCoord cell; }

    static bool Free(Vector3 feet) { return !Physics.CheckCapsule(feet + Vector3.up * .4f, feet + Vector3.up * 1.6f, .32f, ~0, QueryTriggerInteraction.Ignore); }
    static bool Clear(Vector3 a, Vector3 b) { return !Physics.Linecast(a, b, ~0, QueryTriggerInteraction.Ignore); }

    static GridCoord FindZoneCell(Vector3 sd)
    {
        var c0 = map.CellOf(sd);
        var best = new GridCoord(int.MinValue, 0);
        var bestD = float.MaxValue;
        for (var r = 4; r <= 60; r++)
        for (var dx = -r; dx <= r; dx++)
        for (var dy = -r; dy <= r; dy++)
        {
            if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
            var c = new GridCoord(c0.x + dx, c0.y + dy);
            if (!ZoneMatch(c)) continue;
            var inner = 0;
            for (var ex = -2; ex <= 2; ex++)
            for (var ey = -2; ey <= 2; ey++)
                if (ZoneMatch(new GridCoord(c.x + ex, c.y + ey))) inner++;
            if (inner < 20) continue;
            var d = dx * dx + dy * dy;
            if (d < bestD) { bestD = d; best = c; }
            if (r > 8 && best.x != int.MinValue) return best;
        }
        return best;
    }

    static Lamp lampSpot;
    static Vector3 eyePos, eyeDir, eye5;
    static int eyeLamps;
    static float eyeDepth;

    static float Height { get { return Zone == "tall" ? 5.4f : Zone == "low" ? 2.4f : 2.9f; } }

    static bool SelectZoneSpots(float feetY)
    {
        var body = Get<CharacterController>(game, "playerBody");
        body.enabled = false;
        Physics.SyncTransforms();
        var cand = new List<Lamp>();
        foreach (var f in MapFixtures())
        {
            var panel = Get<Renderer>(f, "panel");
            if (panel == null) continue;
            var root = panel.transform.parent;
            var cell = (GridCoord)Get(f, "cell");
            if (!ZoneMatch(cell)) continue;
            if (Mathf.Abs(root.position.y - feetY - Height) > .08f) continue;
            if ((root.position - PlayerRoot.position).sqrMagnitude > 40f * 40f) continue;
            cand.Add(new Lamp { f = f, root = root, cell = cell });
        }
        var eyeUp = Vector3.up * ModuleUnits.PlayerEye;
        var tanV = Mathf.Tan(gameFov * .5f * Mathf.Deg2Rad);
        var tanH = tanV * (float)W / H;
        var axes = new[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
        var best = -1f;
        foreach (var s in cand)
        {
            var cc = s.root.position - new Vector3(0f, 0f, ModuleUnits.TrofferOffsetZ);
            var feet = new Vector3(cc.x, feetY, cc.z);
            if (!Free(feet)) continue;
            var eye = feet + eyeUp;
            foreach (var ax in axes)
            {
                RaycastHit h;
                var depth = Physics.Raycast(eye, ax, out h, 30f, ~0, QueryTriggerInteraction.Ignore) ? h.distance : 30f;
                if (depth < 7f) continue;
                var lamps = 0;
                foreach (var o in cand)
                {
                    var lp = o.root.position + Vector3.down * .05f;
                    var v = lp - eye;
                    var fwd = Vector3.Dot(v, ax);
                    if (fwd < .5f || fwd > depth + .3f) continue;
                    var side = Vector3.Cross(Vector3.up, ax);
                    if (Mathf.Abs(Vector3.Dot(v, side)) / fwd > tanH * .92f || Mathf.Abs(v.y) / fwd > tanV * .92f) continue;
                    if (!Clear(eye, lp)) continue;
                    lamps++;
                }
                var score = lamps - Mathf.Abs(Mathf.Min(depth, 18f) - 12f) * .15f;
                if (lamps >= 3 && score > best) { best = score; eyePos = eye; eyeDir = ax; eyeLamps = lamps; eyeDepth = depth; }
            }
        }
        // the close-up lamp: free under it and free at a three-quarter spot 1.9 m (2.6 m in a tall hall) off on both axes
        var off = Zone == "tall" ? 2.6f : 1.9f;
        var dirs = new[] { new Vector2(1f, -1f), new Vector2(-1f, -1f), new Vector2(1f, 1f), new Vector2(-1f, 1f) };
        cand.Sort((a, b) => (a.root.position - eyePos).sqrMagnitude.CompareTo((b.root.position - eyePos).sqrMagnitude));
        foreach (var s in cand)
        {
            var p = s.root.position;
            var under = new Vector3(p.x, feetY, p.z);
            if (!Free(under)) continue;
            foreach (var d in dirs)
            {
                var feet = new Vector3(p.x + d.x * off, feetY, p.z + d.y * off);
                if (!ZoneMatch(map.CellOf(feet)) || !Free(feet)) continue;
                if (!Clear(feet + eyeUp, p + Vector3.down * .3f) || !Clear(under + eyeUp, feet + eyeUp)) continue;
                eye5 = feet + eyeUp;
                lampSpot = s;
                break;
            }
            if (lampSpot != null) break;
        }
        body.enabled = true;
        Physics.SyncTransforms();
        Log("spots: " + cand.Count + " zone lamps | eye " + (best > -1f ? V3(eyePos) + " dir " + V3(eyeDir) + " depth " + F3(eyeDepth) + " lamps " + eyeLamps : "NONE")
            + " | lamp " + (lampSpot != null ? lampSpot.cell + " at " + V3(lampSpot.root.position) : "NONE"));
        var steady = 0;
        foreach (var f in MapFixtures())
        {
            var panel = Get<Renderer>(f, "panel");
            if (panel == null) continue;
            var c = (GridCoord)Get(f, "cell");
            if (map.IsReserved(c)) continue;
            if ((panel.transform.position - eyePos).sqrMagnitude > 35f * 35f) continue;
            map.SetLampMode(c, ModuleLamp.Steady);
            steady++;
        }
        Log("set " + steady + " lamps Steady");
        return best > -1f && lampSpot != null;
    }

    // Tall T-B: uplight boxes on the hall's columns and walls (3.6 m), found by rays from the hall's lamps
    static readonly List<GameObject> uplights = new List<GameObject>();

    static void PlaceUplights(float feetY, bool phoneTier)
    {
        var prefab = LoadPrefab(ZL + (phoneTier ? "Prefabs/ZL_HIDUP_Tall_Phone" : "Prefabs/ZL_HIDUP_Tall"));
        var mounts = new List<Vector3>();
        var normals = new List<Vector3>();
        var axes = new[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
        foreach (var f in MapFixtures())
        {
            var panel = Get<Renderer>(f, "panel");
            if (panel == null) continue;
            var cell = (GridCoord)Get(f, "cell");
            if (!ZoneMatch(cell)) continue;
            var p = panel.transform.parent.position;
            if ((p - eyePos).sqrMagnitude > 30f * 30f) continue;
            var o = new Vector3(p.x, p.y - 1.8f, p.z - ModuleUnits.TrofferOffsetZ);
            foreach (var ax in axes)
            {
                RaycastHit h;
                if (!Physics.Raycast(o, ax, out h, 4.6f, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (Mathf.Abs(h.normal.y) > .2f) continue;
                var ok = true;
                foreach (var m in mounts) if ((m - h.point).sqrMagnitude < 2.6f * 2.6f) { ok = false; break; }
                if (!ok) continue;
                mounts.Add(h.point);
                normals.Add(h.normal);
            }
        }
        var col = new Color(1f, .95f, .86f, 1f) * 1.6f;
        for (var i = 0; i < mounts.Count; i++)
        {
            var go = UnityEngine.Object.Instantiate(prefab);
            go.name = "ZL uplight " + i;
            go.transform.SetPositionAndRotation(mounts[i], Quaternion.LookRotation(normals[i], Vector3.up));
            var r = go.transform.Find("Lens").GetComponent<Renderer>();
            var b = new MaterialPropertyBlock();
            b.SetColor("_EmissionColor", col);
            r.SetPropertyBlock(b);
            uplights.Add(go);
        }
        Log("uplights placed: " + mounts.Count + " (columns and walls at 3.6 m; glow patches at 5.4 m; static drive " + F3(col.r) + ")");
    }

    static IEnumerator MapMain()
    {
        mapV = MapVariant();
        var sd = (Vector3)Get(game, "startDoorPoint");
        var feetY = PlayerRoot.position.y;
        Place(new Vector3(sd.x, feetY, sd.z - 1.6f), sd + Vector3.forward * 3f + Vector3.up * 1.6f);
        var ts = Time.time;
        while (!Get<bool>(game, "startDoorOpened") && Time.time - ts < 15f) { Resume(); yield return 1; }
        yield return 20;
        var target = FindZoneCell(sd);
        Check(target.x != int.MinValue, "found a " + Zone + " zone cell near the start: " + target);
        if (target.x == int.MinValue) yield break;
        var tc = map.CellCenter(target);
        // stand at the start door's floor height, inside the zone (any wall here is fine: only the streaming follows the player)
        Place(new Vector3(tc.x, feetY, tc.z), tc + Vector3.forward * 5f + Vector3.up * 1.6f);
        yield return Settle(1500);
        yield return 60;
        yield return Settle(600);
        Check(SelectZoneSpots(feetY), "found the zone spots");
        if (lampSpot == null) yield break;
        // move the player to the eye spot so every lamp near the shots ticks (16 m light radius)
        Place(new Vector3(eyePos.x, feetY, eyePos.z), eyePos + eyeDir * 6f);
        yield return Settle(900);
        yield return 30;
        Log("swept " + SweepMap(Phone, eyePos, 34f) + " lamps | clocks " + PinClocks());
        if (Opt == "HIDUP") PlaceUplights(feetY, Phone);
        yield return Frames(30);
        Log("swept again " + SweepMap(Phone, eyePos, 34f) + " | cuts " + cutsDone + " missed " + cutsMissed);
        var lamp = lampSpot.root.position;
        var tag = Zone + " " + Opt;
        preRender = () => { SweepMap(Phone, eyePos, 34f); };
        // eye: level, down the zone, the game's FOV
        AimRot(eyePos, Quaternion.LookRotation(eyeDir, Vector3.up), -1f);
        Shot("p1_eye", tag + " | eye 1.62 level, " + eyeLamps + " zone lamps in view, depth " + F3(eyeDepth));
        if (Phone)
        {
            Aim(eye5, lamp + Vector3.down * .2f, -1f);
            Shot("p3_three_quarter", tag + " | phone tier, three-quarter");
            Stat();
            yield break;
        }
        // under: eye height straight up
        var under = new Vector3(lamp.x, feetY + ModuleUnits.PlayerEye, lamp.z);
        AimRot(under, Quaternion.LookRotation(Vector3.up, Vector3.right), Zone == "tall" ? 40f : -1f);
        Shot("p2_under", tag + " | under lamp " + lampSpot.cell + ", straight up");
        if (Opt == "HIDUP" && uplights.Count > 0)
        {
            GameObject nu = uplights[0];
            foreach (var u in uplights) if ((u.transform.position - eyePos).sqrMagnitude < (nu.transform.position - eyePos).sqrMagnitude) nu = u;
            var ut = nu.transform;
            var q3 = ut.position + ut.forward * 3.2f + ut.right * 1.6f;
            q3.y = feetY + ModuleUnits.PlayerEye;
            Aim(q3, ut.position + Vector3.up * .9f, 50f);
            Shot("p3_three_quarter", tag + " | an uplight and its ceiling patch from " + F3(Vector3.Distance(q3, ut.position)) + " m");
            var he = ut.position + ut.forward * .42f + ut.right * .16f + Vector3.down * .30f;
            Aim(he, ut.position + ut.forward * .14f, 60f);
            Shot("p4_hero_035m", tag + " | about 0.5 m from an uplight, LOD0");
            Aim(q3, ut.position + Vector3.up * .9f, 50f);
            Shot("p5_off", tag + " | (the uplight's own lamp state is not driven by the map in this study: static)");
            Stat();
            yield break;
        }
        // three-quarter
        Aim(eye5, lamp + Vector3.down * (Opt == "PEND" ? .55f : .1f), Zone == "tall" ? 30f : 45f);
        Shot("p3_three_quarter", tag + " | lamp " + lampSpot.cell + " from " + F3(Vector3.Distance(eye5, lamp)) + " m");
        // hero: 0.35 m from the fixture (LOD0 must hold)
        var focus = HeroFocus(lampSpot.root);
        var heroEye = focus + new Vector3(.42f, -.62f, -.48f).normalized * .35f;
        Aim(heroEye, focus, 60f);
        Shot("p4_hero_035m", tag + " | 0.35 m from the fixture, LOD0");
        // the same lamp Off (the game's own state): the dead read
        map.SetLampMode(lampSpot.cell, ModuleLamp.Off);
        yield return Frames(12);
        Aim(eye5, lamp + Vector3.down * (Opt == "PEND" ? .55f : .1f), Zone == "tall" ? 30f : 45f);
        Shot("p5_off", tag + " | lamp " + lampSpot.cell + " Off (ModuleLamp.Off)");
        Stat();
    }

    static Vector3 HeroFocus(Transform root)
    {
        if (Opt == "HIDUP" && uplights.Count > 0)
        {
            GameObject nearest = uplights[0];
            foreach (var u in uplights) if ((u.transform.position - eye5).sqrMagnitude < (nearest.transform.position - eye5).sqrMagnitude) nearest = u;
            return nearest.transform.position + Vector3.down * .05f;
        }
        var lens = root.Find("Lens");
        var b = new Bounds(root.position, Vector3.zero);
        foreach (var r in root.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || r is LineRenderer) continue;
            if (r.name == "Light" || r.GetComponent<Light>() != null) continue;
            if (r.sharedMaterial != null && r.sharedMaterial.shader != null && r.sharedMaterial.shader.name.Contains("GlowAdd")) continue;
            b.Encapsulate(r.bounds);
        }
        if (Opt == "PEND") return root.position + new Vector3(0f, -.56f, -1.15f);
        return new Vector3(b.center.x, b.min.y + .02f, b.center.z) + (Opt == "base" || Opt == "L06" || Opt == "L09" ? new Vector3(0f, 0f, -.45f) : Vector3.zero);
    }

    static void Stat()
    {
        File.WriteAllText(WorkDir + "stats.json", "{\"zone\": \"" + Zone + "\", \"option\": \"" + Opt + "\", \"phone\": " + (Phone ? "true" : "false") + ", \"swapped\": " + swapped
            + ", \"cuts\": " + cutsDone + ", \"cuts_missed\": " + cutsMissed + ", \"uplights\": " + uplights.Count + ", \"fail\": " + fail + "}\n");
    }

    // =========================================================================================== shared stand-in helpers
    static Transform standRoot;

    static Mesh QuadMesh(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float uvScale, bool worldUV)
    {
        var m = new Mesh { name = "ZL quad" };
        var vs = new[] { a, b, c, d };
        m.vertices = vs;
        m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        var n = Vector3.Cross(b - a, c - a).normalized;
        var t = (b - a).normalized;
        m.normals = new[] { n, n, n, n };
        m.tangents = new[] { new Vector4(t.x, t.y, t.z, 1f), new Vector4(t.x, t.y, t.z, 1f), new Vector4(t.x, t.y, t.z, 1f), new Vector4(t.x, t.y, t.z, 1f) };
        var uvs = new Vector2[4];
        var bt = Vector3.Cross(n, t);
        for (var i = 0; i < 4; i++) uvs[i] = new Vector2(Vector3.Dot(vs[i], t), Vector3.Dot(vs[i], bt)) / uvScale;
        m.uv = uvs;
        m.RecalculateBounds();
        return m;
    }

    static Renderer Quad(string name, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Material mat, float uvScale, uint layers)
    {
        var go = new GameObject(name);
        go.transform.SetParent(standRoot, false);
        go.AddComponent<MeshFilter>().sharedMesh = QuadMesh(a, b, c, d, uvScale, true);
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.On;
        r.receiveShadows = true;
        r.renderingLayerMask = layers;
        return r;
    }

    // A box as six quads (faces listed in skip are not built), each facing out.
    static void Box(string name, Vector3 min, Vector3 max, Material mat, float uvScale, uint layers, string skip)
    {
        Vector3 a = min, b = max;
        if (!skip.Contains("-y")) Quad(name + " -y", new Vector3(a.x, a.y, a.z), new Vector3(b.x, a.y, a.z), new Vector3(b.x, a.y, b.z), new Vector3(a.x, a.y, b.z), mat, uvScale, layers);
        if (!skip.Contains("+y")) Quad(name + " +y", new Vector3(a.x, b.y, a.z), new Vector3(a.x, b.y, b.z), new Vector3(b.x, b.y, b.z), new Vector3(b.x, b.y, a.z), mat, uvScale, layers);
        if (!skip.Contains("-x")) Quad(name + " -x", new Vector3(a.x, a.y, a.z), new Vector3(a.x, a.y, b.z), new Vector3(a.x, b.y, b.z), new Vector3(a.x, b.y, a.z), mat, uvScale, layers);
        if (!skip.Contains("+x")) Quad(name + " +x", new Vector3(b.x, a.y, a.z), new Vector3(b.x, b.y, a.z), new Vector3(b.x, b.y, b.z), new Vector3(b.x, a.y, b.z), mat, uvScale, layers);
        if (!skip.Contains("-z")) Quad(name + " -z", new Vector3(a.x, a.y, a.z), new Vector3(a.x, b.y, a.z), new Vector3(b.x, b.y, a.z), new Vector3(b.x, a.y, a.z), mat, uvScale, layers);
        if (!skip.Contains("+z")) Quad(name + " +z", new Vector3(a.x, a.y, b.z), new Vector3(b.x, a.y, b.z), new Vector3(b.x, b.y, b.z), new Vector3(a.x, b.y, b.z), mat, uvScale, layers);
    }

    static Material SurfMat(string name) { var m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Surfaces/" + name + ".mat"); if (m == null) throw new Exception("missing " + name); return m; }
    static Material ZLMat(string name) { var m = AssetDatabase.LoadAssetAtPath<Material>(ZL + "Materials/" + name + ".mat"); if (m == null) throw new Exception("missing " + name); return m; }

    static Light AddSpot(string name, Vector3 pos, Vector3 dir, float outer, float inner, float intensity, float range, Color col, bool shadows, uint layers)
    {
        var go = new GameObject(name);
        go.transform.SetParent(standRoot, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.LookRotation(dir, Mathf.Abs(dir.normalized.y) > .95f ? Vector3.forward : Vector3.up);
        var l = go.AddComponent<Light>();
        l.type = LightType.Spot;
        l.spotAngle = outer;
        l.innerSpotAngle = inner;
        l.intensity = intensity;
        l.range = range;
        l.color = col;
        l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        l.shadowStrength = .9f;
        l.shadowNearPlane = .1f;
        l.renderingLayerMask = (int)layers;
        return l;
    }

    static GameObject AddKit(string prefabPath, Vector3 pos, Quaternion rot, Color emission)
    {
        var go = UnityEngine.Object.Instantiate(LoadPrefab(prefabPath));
        go.transform.SetParent(standRoot, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        var l = go.transform.Find("Lens");
        if (l != null)
        {
            var b = new MaterialPropertyBlock();
            b.SetColor("_EmissionColor", emission);
            l.GetComponent<Renderer>().SetPropertyBlock(b);
        }
        return go;
    }

    static void Cyl(string name, Vector3 a, Vector3 b, float radius, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = name;
        go.transform.SetParent(standRoot, false);
        go.transform.localPosition = (a + b) * .5f;
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
        go.transform.localScale = new Vector3(radius * 2f, (b - a).magnitude * .5f, radius * 2f);
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // URP box projection for the pool's probe (in memory; restored at the end)
    static object urpAsset;
    static object urpBoxOld;

    static void UrpBoxProjection(bool on)
    {
        urpAsset = GraphicsSettings.currentRenderPipeline;
        var f = urpAsset != null ? FI(urpAsset.GetType(), "m_ReflectionProbeBoxProjection") : null;
        if (f == null) { Log("WARN no URP box projection field"); return; }
        urpBoxOld = f.GetValue(urpAsset);
        f.SetValue(urpAsset, on);
        Log("URP reflection probe box projection " + urpBoxOld + " -> " + on + " (in memory)");
    }

    static void RestoreUrp()
    {
        if (urpAsset == null || urpBoxOld == null) return;
        var f = FI(urpAsset.GetType(), "m_ReflectionProbeBoxProjection");
        if (f != null) f.SetValue(urpAsset, urpBoxOld);
    }

    // =========================================================================================== pool (STAND-IN ROOM)
    static readonly Vector3 P0 = new Vector3(3000f, 0f, 3000f);
    const float HallX = 7.42f, HallZ = 23.84f, HallH = 5.4f, PoolX = 4.5f, PoolZ0 = 2.92f, PoolZ1 = 20.92f, WaterY = -.12f;
    static readonly float[] Bays = { 2.98f, 8.94f, 14.90f, 20.86f };
    static readonly List<Light> overLights = new List<Light>();
    static readonly List<Light> waterLights = new List<Light>();
    static readonly List<Renderer> overLens = new List<Renderer>();
    static readonly List<Renderer> waterLens = new List<Renderer>();
    static Material causticFloor, causticCeil;
    static ReflectionProbe probe;

    static float FloorY(float z)
    {
        if (z < 9f) return poolShallowY;
        if (z < 15f) return Mathf.Lerp(poolShallowY, poolDeepY, (z - 9f) / 6f);
        return poolDeepY;
    }

    static void BuildPoolRoom()
    {
        standRoot = new GameObject("ZL STAND-IN ROOM pool").transform;
        standRoot.position = P0;
        var deck = ZLMat("ZL_TileDeck");
        var wall = ZLMat("ZL_TileWall");
        var basin = ZLMat("ZL_TileBasin");
        var lane = ZLMat("ZL_TileLane");
        var coping = ZLMat("ZL_TileCoping");
        var block = SurfMat("RunB_BlockUpper");
        var paint = SurfMat("Office_Wall_Fresh");
        const float T = .9144f;
        uint L = 1u;
        uint LB = 1u | BasinLayer;
        // deck (four rectangles round the pool), facing up
        Quad("deck s", new Vector3(-HallX, 0, 0), new Vector3(-HallX, 0, PoolZ0), new Vector3(HallX, 0, PoolZ0), new Vector3(HallX, 0, 0), deck, T, L);
        Quad("deck n", new Vector3(-HallX, 0, PoolZ1), new Vector3(-HallX, 0, HallZ), new Vector3(HallX, 0, HallZ), new Vector3(HallX, 0, PoolZ1), deck, T, L);
        Quad("deck w", new Vector3(-HallX, 0, PoolZ0), new Vector3(-HallX, 0, PoolZ1), new Vector3(-PoolX - .25f, 0, PoolZ1), new Vector3(-PoolX - .25f, 0, PoolZ0), deck, T, L);
        Quad("deck e", new Vector3(PoolX + .25f, 0, PoolZ0), new Vector3(PoolX + .25f, 0, PoolZ1), new Vector3(HallX, 0, PoolZ1), new Vector3(HallX, 0, PoolZ0), deck, T, L);
        // coping: a 0.25 m glazed bullnose band round the pool, 30 mm proud, its inner lip down to the water
        Box("coping w", new Vector3(-PoolX - .25f, 0f, PoolZ0 - .25f), new Vector3(-PoolX, .03f, PoolZ1 + .25f), coping, T, L, "-y");
        Box("coping e", new Vector3(PoolX, 0f, PoolZ0 - .25f), new Vector3(PoolX + .25f, .03f, PoolZ1 + .25f), coping, T, L, "-y");
        Box("coping s", new Vector3(-PoolX, 0f, PoolZ0 - .25f), new Vector3(PoolX, .03f, PoolZ0), coping, T, L, "-y");
        Box("coping n", new Vector3(-PoolX, 0f, PoolZ1), new Vector3(PoolX, .03f, PoolZ1 + .25f), coping, T, L, "-y");
        // basin walls and floor (the floor follows the slope in 1.5 m strips), facing into the pool
        for (var z = PoolZ0; z < PoolZ1 - .01f; z += 1.5f)
        {
            var z1 = Mathf.Min(PoolZ1, z + 1.5f);
            Quad("basin floor", new Vector3(-PoolX, FloorY(z), z), new Vector3(-PoolX, FloorY(z1), z1), new Vector3(PoolX, FloorY(z1), z1), new Vector3(PoolX, FloorY(z), z), basin, T, LB);
            Quad("basin wall w", new Vector3(-PoolX, FloorY(z), z), new Vector3(-PoolX, 0f, z), new Vector3(-PoolX, 0f, z1), new Vector3(-PoolX, FloorY(z1), z1), basin, T, LB);
            Quad("basin wall e", new Vector3(PoolX, FloorY(z), z), new Vector3(PoolX, FloorY(z1), z1), new Vector3(PoolX, 0f, z1), new Vector3(PoolX, 0f, z), basin, T, LB);
        }
        Quad("basin wall s", new Vector3(-PoolX, FloorY(PoolZ0), PoolZ0), new Vector3(PoolX, FloorY(PoolZ0), PoolZ0), new Vector3(PoolX, 0f, PoolZ0), new Vector3(-PoolX, 0f, PoolZ0), basin, T, LB);
        Quad("basin wall n", new Vector3(-PoolX, FloorY(PoolZ1), PoolZ1), new Vector3(-PoolX, 0f, PoolZ1), new Vector3(PoolX, 0f, PoolZ1), new Vector3(PoolX, FloorY(PoolZ1), PoolZ1), basin, T, LB);
        // lane lines: dark tile, 0.25 m wide, T-ends 1 m wide 1.5 m from the end walls (no text anywhere)
        foreach (var x in new[] { -3.375f, -1.125f, 1.125f, 3.375f })
        {
            for (var z = PoolZ0 + 1.5f; z < PoolZ1 - 1.5f - .01f; z += 1.5f)
            {
                var z1 = Mathf.Min(PoolZ1 - 1.5f, z + 1.5f);
                Quad("lane", new Vector3(x - .125f, FloorY(z) + .004f, z), new Vector3(x - .125f, FloorY(z1) + .004f, z1), new Vector3(x + .125f, FloorY(z1) + .004f, z1), new Vector3(x + .125f, FloorY(z) + .004f, z), lane, T, LB);
            }
            foreach (var zt in new[] { PoolZ0 + 1.5f, PoolZ1 - 1.5f })
                Quad("lane T", new Vector3(x - .5f, FloorY(zt) + .005f, zt - .125f), new Vector3(x - .5f, FloorY(zt) + .005f, zt + .125f), new Vector3(x + .5f, FloorY(zt) + .005f, zt + .125f), new Vector3(x + .5f, FloorY(zt) + .005f, zt - .125f), lane, T, LB);
        }
        // hall walls: glazed tile to 2.4 m, painted block above, facing in
        foreach (var side in new[] { -1f, 1f })
        {
            var x = side * HallX;
            if (side < 0)
            {
                Quad("wall w tile", new Vector3(x, 0, 0), new Vector3(x, 2.4f, 0), new Vector3(x, 2.4f, HallZ), new Vector3(x, 0, HallZ), wall, T, L);
                Quad("wall w block", new Vector3(x, 2.4f, 0), new Vector3(x, HallH, 0), new Vector3(x, HallH, HallZ), new Vector3(x, 2.4f, HallZ), block, 1f, L);
            }
            else
            {
                Quad("wall e tile", new Vector3(x, 0, 0), new Vector3(x, 0, HallZ), new Vector3(x, 2.4f, HallZ), new Vector3(x, 2.4f, 0), wall, T, L);
                Quad("wall e block", new Vector3(x, 2.4f, 0), new Vector3(x, 2.4f, HallZ), new Vector3(x, HallH, HallZ), new Vector3(x, HallH, 0), block, 1f, L);
            }
        }
        Quad("wall s tile", new Vector3(-HallX, 0, 0), new Vector3(HallX, 0, 0), new Vector3(HallX, 2.4f, 0), new Vector3(-HallX, 2.4f, 0), wall, T, L);
        Quad("wall s block", new Vector3(-HallX, 2.4f, 0), new Vector3(HallX, 2.4f, 0), new Vector3(HallX, HallH, 0), new Vector3(-HallX, HallH, 0), block, 1f, L);
        Quad("wall n tile", new Vector3(-HallX, 0, HallZ), new Vector3(-HallX, 2.4f, HallZ), new Vector3(HallX, 2.4f, HallZ), new Vector3(HallX, 0, HallZ), wall, T, L);
        Quad("wall n block", new Vector3(-HallX, 2.4f, HallZ), new Vector3(-HallX, HallH, HallZ), new Vector3(HallX, HallH, HallZ), new Vector3(HallX, 2.4f, HallZ), block, 1f, L);
        // ceiling: painted structure, white, with three downstand beams across the hall
        Quad("ceiling", new Vector3(-HallX, HallH, 0), new Vector3(HallX, HallH, 0), new Vector3(HallX, HallH, HallZ), new Vector3(-HallX, HallH, HallZ), paint, 1f, L);
        foreach (var zb in new[] { 5.96f, 11.92f, 17.88f })
            Box("beam", new Vector3(-HallX, HallH - .45f, zb - .175f), new Vector3(HallX, HallH, zb + .175f), paint, 1f, L, "+y");
        // water: one transparent sheet over the pool
        var wgo = new GameObject("water");
        wgo.transform.SetParent(standRoot, false);
        var wm = QuadMesh(new Vector3(-PoolX, WaterY, PoolZ0), new Vector3(-PoolX, WaterY, PoolZ1), new Vector3(PoolX, WaterY, PoolZ1), new Vector3(PoolX, WaterY, PoolZ0), 1f, false);
        wm.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
        wgo.AddComponent<MeshFilter>().sharedMesh = wm;
        var wr = wgo.AddComponent<MeshRenderer>();
        wr.sharedMaterial = ZLMat("ZL_Water");
        wr.shadowCastingMode = ShadowCastingMode.Off;
        // caustics: on the basin floor (from the overheads) and on the ceiling over the pool (from the underwater lamps)
        causticFloor = new Material(ZLMat("ZL_Caustic"));
        causticFloor.SetVector("_Rect", new Vector4(P0.x - PoolX, P0.z + PoolZ0, P0.x + PoolX, P0.z + PoolZ1));
        causticCeil = new Material(ZLMat("ZL_Caustic"));
        causticCeil.SetVector("_Rect", new Vector4(P0.x - PoolX - 1.5f, P0.z + PoolZ0 - 1.5f, P0.x + PoolX + 1.5f, P0.z + PoolZ1 + 1.5f));
        causticCeil.SetFloat("_Scale", 2.4f);
        causticCeil.SetFloat("_Fade", 1.6f);
        for (var z = PoolZ0; z < PoolZ1 - .01f; z += 1.5f)
        {
            var z1 = Mathf.Min(PoolZ1, z + 1.5f);
            var r = Quad("caustic floor", new Vector3(-PoolX, FloorY(z) + .01f, z), new Vector3(-PoolX, FloorY(z1) + .01f, z1), new Vector3(PoolX, FloorY(z1) + .01f, z1), new Vector3(PoolX, FloorY(z) + .01f, z), causticFloor, 1f, L);
            r.shadowCastingMode = ShadowCastingMode.Off;
        }
        var rc = Quad("caustic ceiling", new Vector3(-PoolX - 1.5f, HallH - .004f, PoolZ0 - 1.5f), new Vector3(PoolX + 1.5f, HallH - .004f, PoolZ0 - 1.5f), new Vector3(PoolX + 1.5f, HallH - .004f, PoolZ1 + 1.5f), new Vector3(-PoolX - 1.5f, HallH - .004f, PoolZ1 + 1.5f), causticCeil, 1f, L);
        rc.shadowCastingMode = ShadowCastingMode.Off;
        // two stainless ladders at the deep end (scale), handrails over the coping into the water
        var steel = SurfMat("Prop_StainlessSatin");
        foreach (var lx in new[] { -3.3f, 3.3f })
            foreach (var rx in new[] { -.25f, .25f })
            {
                var x = lx + rx;
                var pts = new[] { new Vector3(x, 0f, PoolZ1 + .45f), new Vector3(x, .78f, PoolZ1 + .45f), new Vector3(x, .9f, PoolZ1 + .25f), new Vector3(x, .78f, PoolZ1 + .02f), new Vector3(x, .25f, PoolZ1 - .12f), new Vector3(x, -1.3f, PoolZ1 - .12f) };
                for (var i = 0; i + 1 < pts.Length; i++) Cyl("ladder rail", pts[i], pts[i + 1], .021f, steel);
            }
        foreach (var lx in new[] { -3.3f, 3.3f })
            foreach (var ty in new[] { -.35f, -.65f, -.95f })
                Box("ladder tread", new Vector3(lx - .24f, ty - .02f, PoolZ1 - .16f), new Vector3(lx + .24f, ty, PoolZ1 - .06f), steel, 1f, LB, "");
        // battery twin-head units on both end walls (mains on: heads dark, pilot red) - reused Run! kit, no lights
        foreach (var zx in new[] { new Vector3(-4.6f, 2.55f, .02f), new Vector3(4.6f, 2.55f, .02f), new Vector3(-4.6f, 2.55f, HallZ - .02f), new Vector3(4.6f, 2.55f, HallZ - .02f) })
        {
            var rot = zx.z < 1f ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
            var unit = FrontRoomsKitLibrary.Spawn("Kit_EmergencyTwinHead", standRoot, zx, rot, null, false, "pool twin-head", false);
            if (unit == null) continue;
            foreach (var s in new[] { -1f, 1f })
            {
                var yoke = FrontRoomsKitLibrary.Spawn("Kit_EmergencyTwinHead_Yoke", unit.transform, new Vector3(s * .112f, .201f, .059f), Quaternion.Euler(0f, s * 25f, 0f), null, false, "yoke", false);
                if (yoke != null) FrontRoomsKitLibrary.Spawn("Kit_EmergencyTwinHead_Lamp", yoke.transform, new Vector3(0f, .085f, 0f), Quaternion.Euler(22f, 0f, 0f), null, false, "head", false);
            }
            foreach (var r in unit.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (var k = 0; k < mats.Length; k++)
                    if (mats[k] != null && mats[k].name.StartsWith("Run_NeonPilot")) { var b = new MaterialPropertyBlock(); r.GetPropertyBlock(b, k); b.SetColor("_EmissionColor", new Color(2.6f, .2f, .1f)); r.SetPropertyBlock(b, k); }
            }
        }
        // the hall's reflection probe (box projected), rendered once the lights are set
        var pgo = new GameObject("ZL pool probe");
        pgo.transform.SetParent(standRoot, false);
        pgo.transform.localPosition = new Vector3(0f, 1.8f, HallZ * .5f);
        probe = pgo.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        probe.boxProjection = true;
        probe.size = new Vector3(HallX * 2f, HallH + 2.8f, HallZ);
        probe.center = new Vector3(0f, (HallH - 2.8f) * .5f - 1.8f, 0f);
        probe.resolution = 512;
        probe.importance = 10;
        probe.hdr = true;
        probe.nearClipPlane = .05f;
        probe.farClipPlane = 40f;
        Log("pool STAND-IN ROOM built at " + V3(P0) + " (hall 14.84 x 23.84 x 5.4 m, pool 9 x 18 m)");
    }

    static readonly Color MH = new Color(1f, .95f, .87f);
    static readonly Color[] MHScatter = { new Color(1f, .93f, .90f), new Color(.95f, 1f, .90f), new Color(1f, .96f, .86f), new Color(1f, .95f, .92f), new Color(.96f, .99f, .88f), new Color(1f, .94f, .86f), new Color(1f, .97f, .88f), new Color(.97f, 1f, .91f) };
    static readonly Color CW = new Color(1f, .96f, .88f);
    static readonly Color Incand = new Color(1f, .80f, .58f);

    static void BuildOverhead(string kind, bool phoneTier)
    {
        var i = 0;
        if (kind == "P1A")
        {
            foreach (var side in new[] { -1f, 1f })
                foreach (var z in Bays)
                {
                    var n = new Vector3(-side, 0f, 0f);
                    var mount = new Vector3(side * HallX, 3.4f, z);
                    var col = MHScatter[i % MHScatter.Length];
                    var go = AddKit(ZL + (phoneTier ? "Prefabs/ZL_HIDUP_Phone" : "Prefabs/ZL_HIDUP"), mount, Quaternion.LookRotation(n, Vector3.up), col * 1.0f);
                    overLens.Add(go.transform.Find("Lens").GetComponent<Renderer>());
                    overLights.Add(AddSpot("uplight " + i, mount + n * .16f + Vector3.up * .22f, Vector3.up + n * .30f, 130f, 80f, 9f, 7f, col, false, 1u));
                    overLights.Add(AddSpot("ceiling bounce " + i, mount + n * 1.2f + Vector3.up * (HallH - 3.45f), Vector3.down, 170f, 120f, 5.5f, 14f, col * new Color(.98f, .98f, .96f), false, 1u));
                    i++;
                }
        }
        else if (kind == "P1B")
        {
            foreach (var side in new[] { -1f, 1f })
                foreach (var z in Bays)
                {
                    var p = new Vector3(side * 5.96f, HallH, z);
                    var col = MHScatter[i % MHScatter.Length];
                    var go = AddKit(ZL + (phoneTier ? "Prefabs/ZL_MHLB_Phone" : "Prefabs/ZL_MHLB"), p, Quaternion.Euler(0f, 15f * i, 0f), col * 1.8f);
                    overLens.Add(go.transform.Find("Lens").GetComponent<Renderer>());
                    overLights.Add(AddSpot("low-bay " + i, p + Vector3.down * .62f, Vector3.down, 124f, 64f, 11f, 14f, col, true, 1u));
                    i++;
                }
        }
        else if (kind == "P1C")
        {
            foreach (var side in new[] { -1f, 1f })
                foreach (var z in Bays)
                    foreach (var dz in new[] { -1.3f, 1.3f })
                    {
                        var p = new Vector3(side * 5.96f, HallH, z + dz);
                        var go = AddKit(ZL + (phoneTier ? "Prefabs/ZL_VT8_Phone" : "Prefabs/ZL_VT8"), p, Quaternion.identity, CW * 1.8f);
                        overLens.Add(go.transform.Find("Lens").GetComponent<Renderer>());
                        overLights.Add(AddSpot("vapour-tight " + i, p + Vector3.down * .84f, Vector3.down, 165f, 100f, 4.2f, 12f, CW, i % 3 == 0, 1u));
                        i++;
                    }
        }
        Log("overhead " + kind + ": " + overLens.Count + " fixtures, " + overLights.Count + " lights");
    }

    static void BuildWater(string kind, bool phoneTier)
    {
        var zs = new List<Vector3>();
        var ns = new List<Vector3>();
        float inten = 2.4f, outer = 110f;
        string prefab = "ZL_NICHE";
        if (kind == "P2A" || kind == "P2C")
        {
            for (var k = 0; k < 6; k++)
                foreach (var side in new[] { -1f, 1f }) { zs.Add(new Vector3(side * PoolX, -.70f, PoolZ0 + 1.5f + 3f * k)); ns.Add(new Vector3(-side, 0f, 0f)); }
            if (kind == "P2C") { prefab = "ZL_PORT"; inten = 2.1f; outer = 90f; }
        }
        else
        {
            for (var k = 0; k < 3; k++)
                foreach (var side in new[] { -1f, 1f }) { zs.Add(new Vector3(side * PoolX, -.75f, PoolZ0 + 3f + 6f * k)); ns.Add(new Vector3(-side, 0f, 0f)); }
            zs.Add(new Vector3(-2.25f, -1.6f, PoolZ1)); ns.Add(Vector3.back);
            zs.Add(new Vector3(2.25f, -1.6f, PoolZ1)); ns.Add(Vector3.back);
            zs.Add(new Vector3(0f, -.65f, PoolZ0)); ns.Add(Vector3.forward);
            inten = 4f;
        }
        for (var i = 0; i < zs.Count; i++)
        {
            var rot = Quaternion.FromToRotation(Vector3.down, ns[i]);
            // the lens sits 3 mm (niche) / 30 mm (port) behind the ring face: stand the kit off the tile plane so it shows
            var go = AddKit(ZL + "Prefabs/" + prefab + (phoneTier ? "_Phone" : ""), zs[i] + ns[i] * (prefab == "ZL_PORT" ? .034f : .006f), rot, Incand * 1.4f);
            var lr = go.transform.Find("Lens").GetComponent<Renderer>();
            lr.renderingLayerMask = 1u;
            waterLens.Add(lr);
            if (!phoneTier) { var h = go.transform.Find("Housing"); if (h != null) foreach (var r in h.GetComponentsInChildren<Renderer>()) r.renderingLayerMask = 1u | BasinLayer; }
            waterLights.Add(AddSpot("underwater " + i, zs[i] + ns[i] * .12f, (ns[i] + Vector3.down * .22f).normalized, outer, 40f, inten, 10f, Incand, false, BasinLayer));
        }
        Log("water " + kind + ": " + waterLens.Count + " lamps (" + prefab + "), " + waterLights.Count + " lights, basin layer only");
    }

    static void SetPoolState(bool overOn, bool waterOn)
    {
        foreach (var l in overLights) l.enabled = overOn;
        foreach (var l in waterLights) l.enabled = waterOn;
        foreach (var r in overLens) { var b = new MaterialPropertyBlock(); r.GetPropertyBlock(b); var c = b.GetColor("_EmissionColor"); if (c.maxColorComponent > 0f) Set2(r, "zl_on", c); b.SetColor("_EmissionColor", overOn ? Get2(r) : new Color(.02f, .02f, .02f)); r.SetPropertyBlock(b); }
        foreach (var r in waterLens) { var b = new MaterialPropertyBlock(); r.GetPropertyBlock(b); var c = b.GetColor("_EmissionColor"); if (c.maxColorComponent > 0f) Set2(r, "zl_on", c); b.SetColor("_EmissionColor", waterOn ? Get2(r) : new Color(.02f, .02f, .02f)); r.SetPropertyBlock(b); }
        causticFloor.SetFloat("_Level", overOn ? .9f : 0f);
        causticCeil.SetFloat("_Level", waterOn ? (overOn ? .30f : 1.0f) : 0f);
        Log("pool state: overheads " + (overOn ? "on" : "off") + ", underwater " + (waterOn ? "on" : "off"));
    }

    static readonly Dictionary<int, Color> onColor = new Dictionary<int, Color>();
    static void Set2(Renderer r, string k, Color c) { var id = FrontRoomsObjectId.Of(r); if (!onColor.ContainsKey(id)) onColor[id] = c; }
    static Color Get2(Renderer r) { Color c; return onColor.TryGetValue(FrontRoomsObjectId.Of(r), out c) ? c : Color.white; }

    static IEnumerator RenderProbe()
    {
        // the realtime probe has no roughness convolution here: only the water (smoothness .95) samples it; every other
        // stand-in surface keeps the game's environment reflection, as the map's surfaces do
        foreach (var r in standRoot.GetComponentsInChildren<Renderer>(true))
            r.reflectionProbeUsage = r.name == "water" ? ReflectionProbeUsage.BlendProbes : ReflectionProbeUsage.Off;
        probe.RenderProbe();
        yield return Frames(6);
    }

    static IEnumerator PoolMain()
    {
        UrpBoxProjection(true);
        BuildPoolRoom();
        var o = Opt;
        var over = o.StartsWith("P1") ? o : "P1A";
        var water = o.StartsWith("P2") ? o : "P2A";
        BuildOverhead(over, Phone);
        BuildWater(water, Phone);
        // the player's camera far away: lock the map's own lamps out of the frame (they are 3 km away anyway)
        SetPoolState(true, true);
        yield return Frames(10);
        yield return RenderProbe();
        var tag = "pool STAND-IN ROOM " + over + " + " + water;
        var eye = P0 + new Vector3(-6.4f, ModuleUnits.PlayerEye, 1.2f);
        Aim(eye, P0 + new Vector3(1.0f, 1.9f, 16f), -1f);
        Shot("p1_eye", tag + " | deck corner, eye 1.62, the game's FOV");
        if (Phone)
        {
            if (o.StartsWith("P2")) { SetPoolState(false, true); yield return RenderProbe(); Aim(eye, P0 + new Vector3(1.0f, 1.9f, 16f), -1f); Shot("p6_night", tag + " | overheads off, underwater on (phone)"); }
            Stat();
            yield break;
        }
        if (o.StartsWith("P1"))
        {
            Vector3 fx, under, q3;
            if (over == "P1A") { fx = P0 + new Vector3(-HallX, 3.4f, Bays[2]); under = P0 + new Vector3(-HallX + .9f, ModuleUnits.PlayerEye, Bays[2]); q3 = P0 + new Vector3(-4.9f, ModuleUnits.PlayerEye, Bays[2] - 2.4f); }
            else if (over == "P1B") { fx = P0 + new Vector3(-5.96f, 4.8f, Bays[2]); under = P0 + new Vector3(-5.96f, ModuleUnits.PlayerEye, Bays[2]); q3 = P0 + new Vector3(-7.0f, ModuleUnits.PlayerEye, Bays[2] - 3.0f); }
            else { fx = P0 + new Vector3(-5.96f, 4.6f, Bays[2] - 1.3f); under = P0 + new Vector3(-5.96f, ModuleUnits.PlayerEye, Bays[2] - 1.3f); q3 = P0 + new Vector3(-7.0f, ModuleUnits.PlayerEye, Bays[2] - 4.3f); }
            AimRot(under, Quaternion.LookRotation(Vector3.up, Vector3.right), 70f);
            Shot("p2_under", tag + " | under the fixture, straight up, vFOV 70");
            Aim(q3, fx, 34f);
            Shot("p3_three_quarter", tag + " | the fixture from " + F3(Vector3.Distance(q3, fx)) + " m, vFOV 34");
            var hero = fx + new Vector3(.40f, -.55f, -.50f).normalized * .45f;
            if (over == "P1A") hero = fx + new Vector3(.55f, -.45f, -.40f).normalized * .55f;
            Aim(hero, fx + (over == "P1A" ? new Vector3(.12f, 0f, 0f) : Vector3.zero), 60f);
            Shot("p4_hero_035m", tag + " | 0.35 to 0.55 m from the fixture, LOD0");
            // the water mirror: across the pool at grazing, overhead images in the water
            var wEye = P0 + new Vector3(-4.75f, ModuleUnits.PlayerEye, 4.5f);
            Aim(wEye, P0 + new Vector3(2.0f, -.2f, 15f), -1f);
            Shot("p5_water_mirror", tag + " | across the water from the deck edge");
        }
        else
        {
            var niche = P0 + new Vector3(PoolX, -.70f, PoolZ0 + 1.5f + 3f * 2f);
            if (water == "P2B") niche = P0 + new Vector3(PoolX, -.75f, PoolZ0 + 3f + 6f);
            var wEye = P0 + new Vector3(-4.75f, ModuleUnits.PlayerEye, niche.z - 3.5f - P0.z + P0.z);
            Aim(wEye, niche, 40f);
            Shot("p2_niche_across", tag + " | a niche lamp across the pool, from the deck edge, vFOV 40");
            var close = niche + new Vector3(-1.6f, 1.25f, -.6f);
            Aim(close, niche, 40f);
            Shot("p3_niche_close", tag + " | over the water near the far wall (a wader), vFOV 40");
            Aim(niche + new Vector3(-.30f, .12f, -.12f), niche, 60f);
            Shot("p4_hero_035m", tag + " | 0.33 m from the niche ring (no water between: fixture check)");
            SetPoolState(false, true);
            yield return Frames(4);
            yield return RenderProbe();
            Aim(eye, P0 + new Vector3(1.0f, 1.9f, 16f), -1f);
            Shot("p6_night", tag + " | overheads off (mains dip / night), underwater lamps on, ceiling caustics");
            Aim(P0 + new Vector3(-4.75f, ModuleUnits.PlayerEye, 3.2f), P0 + new Vector3(0f, 4.2f, 13f), -1f);
            Shot("p7_night_ceiling", tag + " | overheads off: the ceiling over the pool");
        }
        Stat();
    }

    // =========================================================================================== Run! B (stand-in rig)
    static readonly Vector3 R0 = new Vector3(-3000f, 0f, 3000f);
    static FrontRoomsRunRig rig;
    static readonly List<Renderer> optLens = new List<Renderer>();
    static readonly List<Renderer> srcLens = new List<Renderer>();
    static readonly List<int> srcIndex = new List<int>();
    static float optGain = 1f;

    static IEnumerator RunMain()
    {
        var o = Opt;
        var layout = RunRigLayout.Corridor1x9();
        var face = o == "RA" || o == "RB" || o == "RC" || o == "TC" ? 1.8f : 1.1f;
        layout.wrapLensEmission = new Color(.90f, .95f, 1f) * face;
        var root = new GameObject("ZL Run stand-in").transform;
        root.position = R0;
        standRoot = root;
        var dress = FrontRoomsRunDress.Build(root, layout);
        rig = FrontRoomsRunRig.Create(root, layout, RunRigHost.OwnLamps, null, dress);
        var viewer = new GameObject("ZL run viewer").transform;
        viewer.position = R0 + new Vector3(0f, 1.6f, 6f);
        rig.SetViewer(viewer);
        Log("Run! B rig built at " + V3(R0) + " | wraps " + rig.OwnLampCount + " heads " + rig.HeadCount + " handheld " + rig.Handheld + " | wrap face " + F3(face));
        var wraps = Get(rig, "wraps") as Array;
        if (o == "RA") ApplyWrapVariants(wraps);
        if (o == "RB" || o == "RC") ReplaceWraps(wraps, o == "RB" ? "ZL_STRIP" : "ZL_INDR", o == "RB" ? 2.6f / 1.8f : 1f, o == "RC");
        if (o == "TB") RemoteHeads();
        var tripped = o.StartsWith("T");
        if (tripped) rig.TripImmediate(); else rig.ResetImmediate();
        for (var i = 0; i < 90; i++) { rig.Tick(1f / 60f); yield return 1; }
        if (o == "TC") EmergencyBallast(wraps);
        preRender = CopyLens;
        var tag = "run " + o + (tripped ? " (tripped)" : " (armed)");
        var eye = R0 + new Vector3(0f, ModuleUnits.PlayerEye, .7f);
        Aim(eye, eye + new Vector3(0f, -.05f, 10f), -1f);
        Shot("p1_eye", tag + " | entrance, eye 1.62, down the corridor");
        if (Phone) { Stat(); yield break; }
        if (!tripped)
        {
            AimRot(R0 + new Vector3(0f, ModuleUnits.PlayerEye, 7.8f), Quaternion.LookRotation(Vector3.up, Vector3.right), -1f);
            Shot("p2_under", tag + " | under wrap 2 (z 7.8), straight up");
            var fx = R0 + new Vector3(0f, 2.82f, 10.8f);
            Aim(R0 + new Vector3(1.05f, ModuleUnits.PlayerEye, 8.4f), fx, 40f);
            Shot("p3_three_quarter", tag + " | wrap 3 from 2.8 m, vFOV 40");
            Aim(fx + new Vector3(.22f, -.24f, -.12f).normalized * .35f, fx + new Vector3(0f, -.04f, 0f), 60f);
            Shot("p4_hero_035m", tag + " | 0.35 m from wrap 3, LOD0");
        }
        else
        {
            var unit = R0 + new Vector3(-1.42f, 2.45f, 9f);
            Aim(R0 + new Vector3(.95f, ModuleUnits.PlayerEye, 11.6f), unit, 40f);
            Shot("p3_three_quarter", tag + " | unit 1 (left wall, z 9) from 3.4 m, vFOV 40");
            Aim(R0 + new Vector3(0f, ModuleUnits.PlayerEye, 4.2f), R0 + new Vector3(0f, .6f, 14f), -1f);
            Shot("p2_mid", tag + " | mid corridor, looking at the floor pools");
            Aim(unit + new Vector3(.40f, -.10f, .25f), unit + new Vector3(0f, .05f, 0f), 60f);
            Shot("p4_hero_035m", tag + " | about 0.45 m from unit 1, LOD0");
        }
        Stat();
    }

    static void ApplyWrapVariants(Array wraps)
    {
        // R1 on all; spot-relamp variants by instance (a: one tube with black ends, b: one tube warmer, c: specks + yellowed lens)
        var names = new[] { "R1", "R1a", "R1", "R1c", "R1", "R1b", "R1", "R1a", "R1c" };
        for (var k = 0; k < wraps.Length; k++)
        {
            var w = wraps.GetValue(k);
            var lens = Get<Renderer[]>(w, "lens");
            var idx = Get<int[]>(w, "lensIndex");
            var mat = ZLMat("ZL_WrapLens_" + names[k % names.Length]);
            for (var i = 0; lens != null && i < lens.Length; i++)
            {
                var ms = lens[i].sharedMaterials;
                ms[idx[i]] = mat;
                lens[i].sharedMaterials = ms;
            }
        }
        Log("wrap variants applied: " + string.Join(",", names));
    }

    static void ReplaceWraps(Array wraps, string kit, float gain, bool hideHalo)
    {
        optGain = gain;
        for (var k = 0; k < wraps.Length; k++)
        {
            var w = wraps.GetValue(k);
            var light = Get<Light>(w, "light");
            var lens = Get<Renderer[]>(w, "lens");
            var idx = Get<int[]>(w, "lensIndex");
            // the handheld tier keeps fewer wrap lights: find the wrap's frame from its lens when it has none
            Transform frame = light != null ? light.transform.parent : null;
            if (frame == null && lens != null && lens.Length > 0)
                for (var t = lens[0].transform; t != null; t = t.parent) if (t.name.StartsWith("run b wraparound")) { frame = t; break; }
            if (frame == null) continue;
            for (var c = 0; c < frame.childCount; c++)
            {
                var ch = frame.GetChild(c);
                if ((light != null && ch == light.transform) || ch.name.Contains("halo") || ch.GetComponent<Light>() != null) continue;
                foreach (var r in ch.GetComponentsInChildren<Renderer>()) r.enabled = false;
            }
            if (hideHalo) { var h = Get<Renderer>(w, "halo"); if (h != null) h.enabled = false; }
            var go = UnityEngine.Object.Instantiate(LoadPrefab(ZL + "Prefabs/" + kit + (Phone ? "_Phone" : "")));
            go.transform.SetParent(frame, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            optLens.Add(go.transform.Find("Lens").GetComponent<Renderer>());
            srcLens.Add(lens != null && lens.Length > 0 ? lens[0] : null);
            srcIndex.Add(idx != null && idx.Length > 0 ? idx[0] : 0);
        }
        Log("wraps replaced by " + kit + " x" + optLens.Count + " (lens drive x " + F3(gain) + " of the rig's wrap lens" + (hideHalo ? ", soffit halos off: the reflector stops the up-light" : "") + ")");
    }

    static void CopyLens()
    {
        for (var i = 0; i < optLens.Count; i++)
        {
            var s = srcLens[i];
            if (s == null) continue;
            var b = new MaterialPropertyBlock();
            s.GetPropertyBlock(b, srcIndex[i]);
            var c = b.GetColor("_EmissionColor") * optGain;
            c.a = 1f;
            var o = new MaterialPropertyBlock();
            o.SetColor("_EmissionColor", c);
            optLens[i].SetPropertyBlock(o);
        }
    }

    static void RemoteHeads()
    {
        // every unit's cabinet goes (heads, yokes and EMT feed stay); a remote mount (junction box + bracket) holds the heads;
        // one full cabinet stands at the entrance, an EMT run joins them along the walls at 2.55 m
        var plate = LoadPrefab(ZL + "Prefabs/ZL_RPLATE_Housing");
        var hidden = 0;
        foreach (Transform unit in rig.transform)
        {
            if (!unit.name.StartsWith("Run B / twin-head unit")) continue;
            for (var c = 0; c < unit.childCount; c++)
            {
                var ch = unit.GetChild(c);
                var n = ch.name.ToLowerInvariant();
                if (n.Contains("yoke") || n.Contains("lamp") || n.Contains("head") || n.Contains("emt") || n.Contains("feed")) continue;
                foreach (var r in ch.GetComponentsInChildren<Renderer>()) { r.enabled = false; hidden++; }
            }
            if (!Phone)
            {
                var p = UnityEngine.Object.Instantiate(plate);
                p.transform.SetParent(unit, false);
                p.transform.localPosition = Vector3.zero;
                p.transform.localRotation = Quaternion.identity;
            }
        }
        var cab = FrontRoomsKitLibrary.Spawn("Kit_EmergencyTwinHead", standRoot, new Vector3(-1.42f, 2.35f, 1.2f), Quaternion.Euler(0f, 90f, 0f), null, false, "entrance battery cabinet", false);
        var emt = 0;
        foreach (var side in new[] { -1f, 1f })
        {
            var x = side * 1.40f;
            var seg = FrontRoomsKitLibrary.Spawn("Kit_ConduitEMT", standRoot, new Vector3(x, 2.62f, 1.2f), Quaternion.Euler(90f, 0f, 0f), new Vector3(1f, side < 0 ? 20.0f : 14.0f, 1f), false, "EMT run", false);
            if (seg != null) emt++;
        }
        Log("remote heads: cabinet renderers hidden " + hidden + ", entrance cabinet " + (cab != null) + ", EMT runs " + emt);
    }

    static void EmergencyBallast(Array wraps)
    {
        // heads stay dark (the units' batteries are not wired here); one tube in every third wrap runs on its emergency ballast
        rig.Evaluate(.2f);
        var mi = typeof(FrontRoomsRunRig).GetMethod("ApplyAmbient", BF);
        if (mi != null) mi.Invoke(rig, new object[] { true });
        var heads = Get(rig, "heads") as Array;
        var off = 0;
        for (var i = 0; heads != null && i < heads.Length; i++)
        {
            var h = heads.GetValue(i);
            var l = Get<Light>(h, "light");
            if (l != null) { l.enabled = false; off++; }
            var lens = Get<Renderer[]>(h, "lens");
            var idx = Get<int[]>(h, "lensIndex");
            for (var k = 0; lens != null && k < lens.Length; k++) { var b = new MaterialPropertyBlock(); lens[k].GetPropertyBlock(b, idx[k]); b.SetColor("_EmissionColor", Color.black); lens[k].SetPropertyBlock(b, idx[k]); }
        }
        foreach (var r in rig.GetComponentsInChildren<Renderer>()) if (r.name.Contains("beam cone")) r.enabled = false;
        var one = ZLMat("ZL_WrapLens_OneTube");
        var layout = rig.Layout;
        for (var k = 0; k < wraps.Length; k += 3)
        {
            var w = wraps.GetValue(k);
            var l = Get<Light>(w, "light");
            if (l != null) { l.enabled = true; l.intensity = layout.wrapIntensity * .12f; }
            var lens = Get<Renderer[]>(w, "lens");
            var idx = Get<int[]>(w, "lensIndex");
            for (var i = 0; lens != null && i < lens.Length; i++)
            {
                var ms = lens[i].sharedMaterials;
                ms[idx[i]] = one;
                lens[i].sharedMaterials = ms;
                var b = new MaterialPropertyBlock();
                lens[i].GetPropertyBlock(b, idx[i]);
                b.SetColor("_EmissionColor", new Color(.90f, .95f, 1f) * .55f);
                lens[i].SetPropertyBlock(b, idx[i]);
            }
        }
        Log("emergency ballast: heads off " + off + ", one tube lit in wraps 0, 3, 6 at 0.12 of a wrap's light, lens 0.55");
    }
}
