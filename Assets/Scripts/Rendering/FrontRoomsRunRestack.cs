using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The Run! receding end's T2 body, 重组 (71_run_room.md s.5.3, decision 5; card 2, the T2 body). A new structure (rule 5b): built
/// only by FrontRoomsRunRecede for a receding layout whose schedule runs T2, which nothing in today's game asks for.
///
/// At every step (never the keep-away glide, never a restore: there the wall rides whole, as T1) the end wall comes apart and
/// re-stacks round the door: at the tell the at-rest wall (Kit_RunRecede_Wall, one static mesh) is hidden and this body shows the
/// same wall as its 69 concrete masonry units (Kit_RunRestack_Blocks; phones: 28 half-course strips, Kit_RunRestack_Strips),
/// flown by one of two clips baked offline (Kit_RunRestack_*_flinch / _step.json, Tools/Blender/frontrooms_kit/run_restack_bake.py:
/// the door break's method, research/door_break/50_rethink.md 3.7, pre-built pieces and baked keys, never runtime cutting); at
/// the end of the seat the static wall is back and the body hidden. The courses come out from the top, hang back along the
/// walls and into the soffit corners while the door, its frame, the lintel and the end's steel roll back alone, and land again
/// from the bottom up, the top course last into the seat. The clips' phases are time-warped onto the schedule's own (the final
/// step and the flinch-alone use the step and flinch clips), so the body and the end never drift apart.
///
/// One skinned body: the pieces of a level merged once a session into one mesh with a bone per piece (LOD0 / LOD1 / LOD2 on
/// desktop and iPad, one level of strips on phones), so a step draws one renderer per slot whatever the piece count. The bones
/// ride the end (children of its head), so the end's jolt and seat move every seated piece with it. The body is built with the
/// end, before the room's rig, so the rig takes its renderers into the room's ambient and light gate like any of the end's parts.
/// Grit (54_dust_grit.md's GPU look, analytic, FrontRoomsRunGrit) sifts from each course's joints as it lands: desktop only.
/// Events for 声音设计 (RunRecedeCue, nothing played): MortarCrack, SubframeSlide, BlockLift (per course), StreamClatter,
/// CourseLand and MortarSift (per course), from the clips' contact lists.
/// </summary>
[DisallowMultipleComponent]
public sealed class FrontRoomsRunRestack : MonoBehaviour
{
    public const string KitPrefix = "Kit_RunRestack_";
    public const string BlocksKit = "Kit_RunRestack_Blocks", StripsKit = "Kit_RunRestack_Strips";
    public const string FlinchSuffix = "_flinch", StepSuffix = "_step";
    /// <summary>The blocks' LOD switches (m at the reference vertical FOV 76): the hero units within 10 m, the cored boxes to
    /// 28 m, plain boxes beyond (never culled while they fly: the room is 42 m at most).</summary>
    public static readonly float[] LodDistances = { 10f, 28f };

    /// <summary>Which body a tier gets: desktop the blocks with grit; iPad the blocks, no grit; phones the strips, no grit
    /// (decision 5). WebGL never builds one (the schedule runs T1 there).</summary>
    public enum Tier : byte { Desktop, Tablet, Phone }

    /// <summary>Tools: the phone's strips or the iPad's blocks in the editor. Never set by the game.</summary>
    public static bool ForcePhoneForTools, ForceTabletForTools;

    public static Tier CurrentTier
    {
        get
        {
            if (ForcePhoneForTools) return Tier.Phone;
            if (ForceTabletForTools) return Tier.Tablet;
            if (!FrontRoomsRunRig.HandheldTier) return Tier.Desktop;
            return FrontRoomsHandheld.IsTablet ? Tier.Tablet : Tier.Phone;
        }
    }

    [Serializable] public sealed class ClipPiece { public string name; public int index; public int course; public int side; public float[] rest; public float lift; public float land; public float[] keys; }
    [Serializable] public sealed class ClipContact { public float t; public string kind; public int index; public int course; public float speed; public float x, y, z; }
    [Serializable] public sealed class Clip { public string name; public string method; public int hz; public int frames; public float duration; public float[] phases; public float distance; public bool phone; public ClipPiece[] pieces; public ClipContact[] contacts; }
    [Serializable] sealed class SidecarPiece { public string name; public int index; public int course; public float x0, x1, y0, y1; public float depth; }
    [Serializable] sealed class Sidecar { public string name; public SidecarPiece[] pieces; }

    /// <summary>A kit's merged levels, shared by every room this session.</summary>
    sealed class Body
    {
        public Mesh[] levels;
        public Material[][] materials;
        public Vector3[] rests;
        public int[] courses;
        public float[] widths;
        public int[] triangles;
    }

    static readonly Dictionary<string, Body> Bodies = new Dictionary<string, Body>();
    static readonly Dictionary<string, Clip> Clips = new Dictionary<string, Clip>();

    public Tier BodyTier { get; private set; }
    /// <summary>True between a step's tell and the end of its seat (the pieces fly; the static wall is hidden).</summary>
    public bool Active { get; private set; }
    public int PieceCount => bones != null ? bones.Length : 0;
    public int Levels => renderers != null ? renderers.Length : 0;
    public bool HasGrit => grit != null;
    /// <summary>The clip under way (null at rest) and its time (s).</summary>
    public Clip Current { get; private set; }
    public float ClipTime { get; private set; }
    /// <summary>Triangles of each level (the whole body; tests and the cost row).</summary>
    public int[] LevelTriangles => body != null ? body.triangles : new int[0];
    public SkinnedMeshRenderer[] Renderers => renderers;
    public FrontRoomsRunGrit Grit => grit;

    FrontRoomsRunRecede recede;
    GameObject wall;
    Body body;
    Clip flinch, step;
    Transform[] bones;
    SkinnedMeshRenderer[] renderers;
    FrontRoomsRunGrit grit;
    float tell, travel, seat;
    float lastClipTime = -1f;
    int contactCursor;
    readonly bool[] courseLifted = new bool[16];

    // ---------------------------------------------------------------- construction
    /// <summary>
    /// Build the T2 body on the receding end's head (FrontRoomsRunRecede.BuildHead, before the rig): the bones at rest, one
    /// skinned renderer per level, hidden. <paramref name="wallSkins"/> is the end's at-rest wall, hidden while a step flies.
    /// Null when a kit or a clip is missing (the end then runs T1's look on T2's timings: nothing breaks).
    /// </summary>
    public static FrontRoomsRunRestack Build(FrontRoomsRunRecede recede, Transform head, GameObject wallSkins)
    {
        if (recede == null || head == null) return null;
        var tier = CurrentTier;
        var kit = tier == Tier.Phone ? StripsKit : BlocksKit;
        var cf = LoadClip(kit + FlinchSuffix);
        var cs = LoadClip(kit + StepSuffix);
        var b = LoadBody(kit, tier == Tier.Phone ? 1 : 3);
        if (cf == null || cs == null || b == null || cf.pieces == null || cf.pieces.Length != b.rests.Length || cs.pieces.Length != b.rests.Length)
        {
            Debug.LogWarning("[RunRestack] " + kit + ": kit or clips missing or mismatched; the end rides whole (T1 look)");
            return null;
        }
        var go = new GameObject(tier == Tier.Phone ? "run b restack body (phone strips)" : "run b restack body (blocks)");
        go.transform.SetParent(head, false);
        var r = go.AddComponent<FrontRoomsRunRestack>();
        r.recede = recede;
        r.BodyTier = tier;
        r.wall = wallSkins;
        r.flinch = cf;
        r.step = cs;
        r.body = b;
        var n = b.rests.Length;
        r.bones = new Transform[n];
        for (var i = 0; i < n; i++)
        {
            var bone = new GameObject("p" + i.ToString("000")).transform;
            bone.SetParent(go.transform, false);
            bone.localPosition = b.rests[i];
            r.bones[i] = bone;
        }
        var bounds = ClipBounds(cf, cs, b);
        r.renderers = new SkinnedMeshRenderer[b.levels.Length];
        for (var lv = 0; lv < b.levels.Length; lv++)
        {
            var lgo = new GameObject("run b restack " + (b.levels.Length > 1 ? "LOD" + lv : "strips"));
            lgo.transform.SetParent(go.transform, false);
            var smr = lgo.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = b.levels[lv];
            smr.bones = r.bones;
            smr.rootBone = go.transform;
            smr.sharedMaterials = b.materials[lv];
            smr.localBounds = bounds;
            smr.updateWhenOffscreen = false;
            smr.quality = SkinQuality.Bone1;
            smr.skinnedMotionVectors = false;
            smr.shadowCastingMode = ShadowCastingMode.On;
            r.renderers[lv] = smr;
        }
        if (r.renderers.Length > 1)
        {
            // LOD switches as the kit importer sets them from a sidecar (size / (2 d tan 38 deg)), on the wall's own size
            var group = go.AddComponent<LODGroup>();
            group.localReferencePoint = new Vector3(0f, 1.3f, 0f);
            group.size = 2.9f;
            var tan = Mathf.Tan(38f * Mathf.Deg2Rad);
            var lods = new LOD[r.renderers.Length];
            for (var lv = 0; lv < lods.Length; lv++)
            {
                var h = lv < LodDistances.Length ? group.size / (2f * LodDistances[lv] * tan) : 0f;
                lods[lv] = new LOD(h, new Renderer[] { r.renderers[lv] });
            }
            group.SetLODs(lods);
            group.fadeMode = LODFadeMode.None;
        }
        // the grit outlives a step by up to 3 s, so it hangs on the head (always on), not on the body (hidden at the seat)
        if (tier == Tier.Desktop) r.grit = FrontRoomsRunGrit.Create(head);
        go.SetActive(false);
        return r;
    }

    public static Clip LoadClip(string name)
    {
        if (Clips.TryGetValue(name, out var c) && c != null) return c;
        var text = Resources.Load<TextAsset>(FrontRoomsKitLibrary.ModelFolder + name);
        c = text != null ? JsonUtility.FromJson<Clip>(text.text) : null;
        if (c != null && (c.hz <= 0 || c.frames < 2 || c.pieces == null || c.phases == null || c.phases.Length < 3)) c = null;
        Clips[name] = c;
        return c;
    }

    /// <summary>The kit's pieces merged per level into one skinned mesh (a bone per piece, bound at the piece's rest centre in
    /// the end's frame); made once, kept for the session. The kit is imported readable (FrontRoomsKitImporter) for this.</summary>
    static Body LoadBody(string kit, int levelCount)
    {
        if (Bodies.TryGetValue(kit, out var cached) && cached != null && cached.levels != null && cached.levels[0] != null) return cached;
        var model = FrontRoomsKitLibrary.Model(kit);
        var text = Resources.Load<TextAsset>(FrontRoomsKitLibrary.ModelFolder + kit);
        var side = text != null ? JsonUtility.FromJson<Sidecar>(text.text) : null;
        if (model == null || side == null || side.pieces == null || side.pieces.Length == 0) return null;
        var n = side.pieces.Length;
        var b = new Body { levels = new Mesh[levelCount], materials = new Material[levelCount][], rests = new Vector3[n], courses = new int[n], widths = new float[n], triangles = new int[levelCount] };
        var byName = new Dictionary<string, MeshFilter>();
        foreach (var mf in model.GetComponentsInChildren<MeshFilter>(true)) byName[mf.name] = mf;
        var root = model.transform;
        for (var i = 0; i < n; i++)
        {
            var p = side.pieces[i];
            b.rests[i] = new Vector3((p.x0 + p.x1) * .5f, (p.y0 + p.y1) * .5f, 0f);
            b.courses[i] = p.course;
            b.widths[i] = p.x1 - p.x0;
        }
        var bind = new Matrix4x4[n];
        for (var i = 0; i < n; i++) bind[i] = Matrix4x4.Translate(-b.rests[i]);
        for (var lv = 0; lv < levelCount; lv++)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tangents = new List<Vector4>();
            var uvs = new List<Vector2>();
            var weights = new List<BoneWeight>();
            var slots = new List<Material>();
            var tris = new List<List<int>>();
            for (var i = 0; i < n; i++)
            {
                var name = levelCount > 1 ? side.pieces[i].name + "_L" + lv : side.pieces[i].name;
                if (!byName.TryGetValue(name, out var mf) || mf.sharedMesh == null) return null;
                var mesh = mf.sharedMesh;
                if (!mesh.isReadable) { Debug.LogWarning("[RunRestack] " + kit + " is not readable (FrontRoomsKitImporter)"); return null; }
                var mr = mf.GetComponent<MeshRenderer>();
                var mats = mr != null ? mr.sharedMaterials : new Material[0];
                var toRoot = root.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                var v0 = verts.Count;
                var mv = mesh.vertices; var mn = mesh.normals; var mt = mesh.tangents; var mu = mesh.uv;
                for (var k = 0; k < mv.Length; k++)
                {
                    verts.Add(toRoot.MultiplyPoint3x4(mv[k]));
                    normals.Add(k < mn.Length ? toRoot.MultiplyVector(mn[k]).normalized : Vector3.up);
                    if (k < mt.Length)
                    {
                        var t = toRoot.MultiplyVector(new Vector3(mt[k].x, mt[k].y, mt[k].z)).normalized;
                        tangents.Add(new Vector4(t.x, t.y, t.z, mt[k].w));
                    }
                    else tangents.Add(new Vector4(1f, 0f, 0f, 1f));
                    uvs.Add(k < mu.Length ? mu[k] : Vector2.zero);
                    weights.Add(new BoneWeight { boneIndex0 = i, weight0 = 1f });
                }
                for (var s = 0; s < mesh.subMeshCount; s++)
                {
                    var m = s < mats.Length ? mats[s] : null;
                    var slot = slots.IndexOf(m);
                    if (slot < 0) { slots.Add(m); tris.Add(new List<int>()); slot = slots.Count - 1; }
                    var idx = mesh.GetTriangles(s);
                    var list = tris[slot];
                    for (var k = 0; k < idx.Length; k++) list.Add(idx[k] + v0);
                }
            }
            var merged = new Mesh { name = kit + (levelCount > 1 ? " LOD" + lv : "") + " (skinned)" };
            if (verts.Count > 65000) merged.indexFormat = IndexFormat.UInt32;
            merged.SetVertices(verts);
            merged.SetNormals(normals);
            merged.SetTangents(tangents);
            merged.SetUVs(0, uvs);
            merged.subMeshCount = slots.Count;
            var count = 0;
            for (var s = 0; s < slots.Count; s++) { merged.SetTriangles(tris[s], s, false); count += tris[s].Count / 3; }
            merged.boneWeights = weights.ToArray();
            merged.bindposes = bind;
            merged.RecalculateBounds();
            merged.UploadMeshData(true);
            b.levels[lv] = merged;
            b.materials[lv] = slots.ToArray();
            b.triangles[lv] = count;
        }
        Bodies[kit] = b;
        return b;
    }

    /// <summary>The bounds every pose of both clips stays inside (the end's frame), so the renderer never culls a flying piece.</summary>
    static Bounds ClipBounds(Clip a, Clip b, Body body)
    {
        var min = new Vector3(-1.45f, 0f, -.2f);
        var max = new Vector3(1.45f, 2.95f, .2f);
        foreach (var c in new[] { a, b })
            foreach (var p in c.pieces)
                for (var k = 0; k + 2 < p.keys.Length; k += 7)
                {
                    var q = new Vector3(p.keys[k], p.keys[k + 1], p.keys[k + 2]);
                    min = Vector3.Min(min, q - Vector3.one * .25f);
                    max = Vector3.Max(max, q + Vector3.one * .25f);
                }
        var bounds = new Bounds();
        bounds.SetMinMax(min, max);
        return bounds;
    }

    // ---------------------------------------------------------------- a step
    /// <summary>A step's tell begins (not the glide): the static wall hides, the body shows at rest. <paramref name="cells"/> picks
    /// the clip (2: the flinch; else the one-cell step); the phase durations are the schedule's, onto which the clip is warped.</summary>
    public void Begin(int cells, float tellSeconds, float travelSeconds, float seatSeconds)
    {
        Current = cells >= 2 ? flinch : step;
        tell = Mathf.Max(1e-4f, tellSeconds);
        travel = Mathf.Max(1e-4f, travelSeconds);
        seat = Mathf.Max(1e-4f, seatSeconds);
        lastClipTime = -1f;
        contactCursor = 0;
        Array.Clear(courseLifted, 0, courseLifted.Length);
        if (wall != null) wall.SetActive(false);
        gameObject.SetActive(true);
        Active = true;
        Pose(0f);
    }

    /// <summary>The step at <paramref name="phase"/>, <paramref name="local"/> seconds into it (the recede's Advance).</summary>
    public void Sample(RunRecedePhase phase, float local)
    {
        if (!Active || Current == null) return;
        var ph = Current.phases;
        float t;
        switch (phase)
        {
            case RunRecedePhase.Tell: t = Mathf.Min(local, tell) * ph[0] / tell; break;
            case RunRecedePhase.Travel: t = ph[0] + Mathf.Min(local, travel) * ph[1] / travel; break;
            case RunRecedePhase.Seat: t = ph[0] + ph[1] + Mathf.Min(local, seat) * ph[2] / seat; break;
            default: t = Current.duration; break;
        }
        t = Mathf.Clamp(t, 0f, Current.duration);
        Pose(t);
        Fire(t);
    }

    /// <summary>The seat is over (or the end is restored, or a glide begins): the static wall is back, the body hidden.</summary>
    public void End()
    {
        if (!Active) return;
        if (Current != null)
        {
            Pose(Current.duration);
            Fire(Current.duration);
        }
        Active = false;
        Current = null;
        gameObject.SetActive(false);
        if (wall != null) wall.SetActive(true);
    }

    void Pose(float t)
    {
        ClipTime = t;
        var c = Current;
        var f = Mathf.Clamp(t * c.hz, 0f, c.frames - 1);
        var i0 = Mathf.Min((int)f, c.frames - 2);
        var a = f - i0;
        for (var i = 0; i < bones.Length && i < c.pieces.Length; i++)
        {
            var k = c.pieces[i].keys;
            var o0 = i0 * 7;
            var o1 = o0 + 7;
            var p = new Vector3(k[o0] + (k[o1] - k[o0]) * a, k[o0 + 1] + (k[o1 + 1] - k[o0 + 1]) * a, k[o0 + 2] + (k[o1 + 2] - k[o0 + 2]) * a);
            var q0 = new Quaternion(k[o0 + 3], k[o0 + 4], k[o0 + 5], k[o0 + 6]);
            var q1 = new Quaternion(k[o1 + 3], k[o1 + 4], k[o1 + 5], k[o1 + 6]);
            bones[i].SetLocalPositionAndRotation(p, Quaternion.Slerp(q0, q1, a));
        }
    }

    /// <summary>The contacts crossed since the last sample, in order: the cues for 声音设计 and the grit.</summary>
    void Fire(float t)
    {
        var c = Current;
        if (c.contacts == null) return;
        while (contactCursor < c.contacts.Length && c.contacts[contactCursor].t <= t + 1e-5f)
        {
            var e = c.contacts[contactCursor++];
            var at = transform.TransformPoint(new Vector3(e.x, e.y, e.z));
            switch (e.kind)
            {
                case "crack": recede.RaiseCue(RunRecedeCue.MortarCrack, 0, at); break;
                case "subframe": recede.RaiseCue(RunRecedeCue.SubframeSlide, 0, at); break;
                case "stream":
                    recede.RaiseCue(RunRecedeCue.StreamClatter, 0, transform.TransformPoint(new Vector3(-1.1f, 1.4f, -.6f)));
                    recede.RaiseCue(RunRecedeCue.StreamClatter, 1, transform.TransformPoint(new Vector3(1.1f, 1.4f, -.6f)));
                    break;
                case "lift":
                    if (e.course >= 0 && e.course < courseLifted.Length && !courseLifted[e.course])
                    {
                        courseLifted[e.course] = true;
                        recede.RaiseCue(RunRecedeCue.BlockLift, e.course, at);
                    }
                    break;
                case "course":
                    recede.RaiseCue(RunRecedeCue.CourseLand, e.course, at);
                    recede.RaiseCue(RunRecedeCue.MortarSift, e.course, at);
                    if (grit != null) EmitGrit(e.course);
                    break;
            }
        }
        lastClipTime = t;
    }

    /// <summary>Grit from a landed course's joints: the bed joint under it and the head joints between its pieces, on both
    /// faces, falling and lit by the goal lamp (FrontRoomsRunGrit; desktop only), gone within 3 s.</summary>
    void EmitGrit(int course)
    {
        var bed = course == 0 ? .06f : (course == 12 ? 2.40f : .2f * course);
        var spans = new List<Vector2>();
        for (var i = 0; i < body.courses.Length; i++)
            if (body.courses[i] == course)
                spans.Add(new Vector2(body.rests[i].x - body.widths[i] * .5f, body.rests[i].x + body.widths[i] * .5f));
        if (spans.Count == 0) return;
        grit.EmitCourse(transform, spans, bed, course == 12 ? .13f : .2f, recede.Clock, recede.HeadVelocity);
    }

    void OnDestroy()
    {
        if (wall != null && Active) wall.SetActive(true);
    }
}
