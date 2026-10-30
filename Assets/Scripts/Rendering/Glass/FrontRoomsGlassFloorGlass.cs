// FrontRoomsGlassFloorGlass.cs -- GD3 step 6 + GD3c (plan 10 sec 2.3 S3-S4 and "Settle", 2.4 "Teeth, floor glass: static merged
// meshes", 4.3, 4.6; build/06_debris.md; gd3c/10_design.md sec 2.3-2.4, 3, 4, 6): what a broken window keeps for good, on its
// root `Window {a}-{b}` (main scene, under the chunk, so the chunk's release takes it along: Red's "too far" rule).
//   Teeth   the tooth pieces left in the stop, one static mesh at their rest pose (from the shatter frame on). Render only:
//           no collider in the game's physics. Bottom teeth snap on the climb; the snapped mask goes into the record.
//   Floor   after the settle, every shard at its settled pose plus the resting crumbs, merged into ONE static mesh in the root's
//           frame (one draw), with an LOD1 of the pane faces only (no crack faces, no crumbs). Render only: no collider, so it
//           never blocks the player or the Relay and never enters nav. Built off the main thread (FrontRoomsGlassDebrisJobs),
//           then made non-readable (no CPU copy; F6) unless KeepReadable (a G14 P4 note).
//   Reduced tier (WebGL / iOS): teeth and floor merged into one mesh, one draw (the teeth renderer goes at the merge).
// Both use the shared Glass_Fracture material and the pane's property block (impact; no bow, crush or sleeve wipe once the pane is
// gone), and carry FrontRoomsMetalGlassTarget: G14 P4 can register exactly these two after the settle (plan sec 4.7, a note for G14).
// The floor glass draws in the shader's floor mode (_PaneSize.w = 1, art r1 F4): each piece's pane frame from its vertices (UV1.z),
// the environment read rough (the zone cube has no parallax). The teeth stay in the pane's own frame (they never moved).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class FrontRoomsGlassFloorGlass : MonoBehaviour
{
    public const string TeethName = "GD3 glass S3 teeth", FloorName = "GD3 glass S4 floor glass", Lod0Name = "LOD0", Lod1Name = "LOD1";
    /// <summary>Screen-relative heights of the floor glass's LODs (LOD0 above, LOD1 above the second, culled below).</summary>
    public static float Lod0Height = .06f, Lod1Height = .006f;
    /// <summary>G14 P4 may need the merged meshes readable (BLAS build from the CPU copy); off = no CPU copy (F6).</summary>
    public static bool KeepReadable;

    public MeshRenderer TeethRenderer { get; private set; }
    public MeshRenderer FloorRenderer { get; private set; }
    public MeshRenderer FloorLod1Renderer { get; private set; }
    public LODGroup FloorLods { get; private set; }
    public Mesh TeethMesh => teethMesh;
    public Mesh FloorMesh => floorMesh;
    public Mesh FloorLod1Mesh => floorLod1;
    public int TeethCount => teeth.Count;
    public int TeethTris { get; private set; }
    public int FloorTris { get; private set; }
    public int FloorVerts { get; private set; }
    public int FloorLod1Tris { get; private set; }
    public int DroppedTris { get; internal set; }
    public int ClearZoneVerts { get; internal set; }
    /// <summary>art r1 #2: floor-glass vertices inside the dressed frame's parts (stool, nosings, aprons, casings): must be 0.</summary>
    public int FrameVerts { get; internal set; }
    public bool BottomTeethSnapped { get { var b = BottomMask(); return b != 0 && (snapped & b) == b; } }
    /// <summary>Bit k = tooth k (in the stage-2 range order of the pattern) has snapped off.</summary>
    public ulong SnappedMask => snapped;
    /// <summary>Reduced tier: teeth and floor are one mesh (the floor mesh).</summary>
    public bool Merged { get; private set; }
    /// <summary>The floor dropped to LOD1 only (the live-memory cap, gd3c sec 6.2).</summary>
    public bool Lod1Only { get; private set; }
    public bool HasFloor => FloorTris > 0;
    /// <summary>Order of the break this floor glass came from (the memory cap drops the oldest first).</summary>
    public int BrokenOrder { get; internal set; }
    /// <summary>Glass footsteps: the 5 cm floor cells this window's settled glass covers (world XZ).</summary>
    public readonly HashSet<long> Cells = new HashSet<long>();

    Mesh teethMesh, floorMesh, floorLod1;
    MaterialPropertyBlock block;
    ulong snapped;
    int teethTail;

    // the teeth: one vertex buffer (slab-local, rest pose), per tooth a side range and a face range of the source indices
    struct Tooth
    {
        public int sideStart, sideCount, faceStart, faceCount;
        public char side;
        public Vector3 centre;       // root-local
    }
    readonly List<Tooth> teeth = new List<Tooth>();
    readonly List<int> teethSides = new List<int>(), teethFaces = new List<int>();
    readonly List<FrontRoomsGlassFractureMeshes.Vertex> teethVerts = new List<FrontRoomsGlassFractureMeshes.Vertex>();
    readonly List<int> scratch = new List<int>();

    public static FrontRoomsGlassFloorGlass On(Transform root)
    {
        if (root == null) return null;
        return root.TryGetComponent<FrontRoomsGlassFloorGlass>(out var f) ? f : root.gameObject.AddComponent<FrontRoomsGlassFloorGlass>();
    }

    void OnEnable()
    {
        if (!FrontRoomsGlassDebris.liveFloors.Contains(this)) FrontRoomsGlassDebris.liveFloors.Add(this);
    }

    void OnDisable() => FrontRoomsGlassDebris.liveFloors.Remove(this);

    // ---- teeth -------------------------------------------------------------------------------------------------------------
    /// <summary>Begin a fresh teeth set (vertices slab-local at rest); <paramref name="snappedMask"/> = teeth already gone.</summary>
    internal void BeginTeeth(ulong snappedMask = 0)
    {
        teeth.Clear();
        teethSides.Clear();
        teethFaces.Clear();
        teethVerts.Clear();
        snapped = snappedMask;
    }

    /// <summary>One tooth: its vertices and its crack-face (side) and pane-face index lists, both relative to <paramref name="verts"/>.</summary>
    internal void AddTooth(List<FrontRoomsGlassFractureMeshes.Vertex> verts, int v0, int vn, List<int> sideIdx, List<int> faceIdx, char side, Vector3 centreRoot)
    {
        var baseV = teethVerts.Count;
        for (var k = 0; k < vn; k++) teethVerts.Add(verts[v0 + k]);
        var t = new Tooth { side = side, centre = centreRoot, sideStart = teethSides.Count, faceStart = teethFaces.Count };
        foreach (var i in sideIdx) teethSides.Add(baseV + i - v0);
        foreach (var i in faceIdx) teethFaces.Add(baseV + i - v0);
        t.sideCount = teethSides.Count - t.sideStart;
        t.faceCount = teethFaces.Count - t.faceStart;
        teeth.Add(t);
    }

    bool Gone(int t) => t < 64 && (snapped & (1UL << t)) != 0;

    ulong BottomMask()
    {
        ulong m = 0;
        for (var t = 0; t < teeth.Count && t < 64; t++) if (teeth[t].side == 'B') m |= 1UL << t;
        return m;
    }

    /// <summary>Upload the teeth mesh (every crack face before every pane face, as the stage-2 mesh) and show or hide it.</summary>
    internal void UploadTeeth(bool show)
    {
        if (teethMesh == null) { teethMesh = new Mesh { name = "GD3 teeth" }; }
        scratch.Clear();
        // every crack face before every pane face; the bottom teeth last (a climb snap then only shortens the draw range)
        for (var pass = 0; pass < 2; pass++)
        {
            for (var t = 0; t < teeth.Count; t++)
            {
                if (Gone(t) || (teeth[t].side == 'B') != (pass == 1)) continue;
                for (var k = 0; k < teeth[t].sideCount; k++) scratch.Add(teethSides[teeth[t].sideStart + k]);
            }
            for (var t = 0; t < teeth.Count; t++)
            {
                if (Gone(t) || (teeth[t].side == 'B') != (pass == 1)) continue;
                for (var k = 0; k < teeth[t].faceCount; k++) scratch.Add(teethFaces[teeth[t].faceStart + k]);
            }
            if (pass == 0) teethTail = scratch.Count;
        }
        teethTail = scratch.Count - teethTail;
        Upload(teethMesh, teethVerts, scratch);
        TeethTris = scratch.Count / 3;
        if (TeethRenderer == null) TeethRenderer = MakeRenderer(TeethName, transform, new Vector3(0f, (float)FrontRoomsGlassFracturePattern.SlabCentreY, 0f), teethMesh);
        else TeethRenderer.GetComponent<MeshFilter>().sharedMesh = teethMesh;
        TeethRenderer.enabled = show && scratch.Count > 0 && !Merged;
    }

    public void ShowTeeth(bool on) { if (TeethRenderer != null) TeethRenderer.enabled = on && TeethTris > 0 && !Merged; }

    /// <summary>The teeth's managed source (about 100 KB) is only needed to build the mesh: freed once it is on the GPU.</summary>
    internal void FreeTeethSource()
    {
        teethVerts.Clear(); teethVerts.TrimExcess();
        teethSides.Clear(); teethSides.TrimExcess();
        teethFaces.Clear(); teethFaces.TrimExcess();
        scratch.Clear(); scratch.TrimExcess();
    }

    /// <summary>Reduced tier: index count of the bottom teeth at the end of the merged mesh.</summary>
    internal int MergedTail { get; set; }

    // drop the last <paramref name="tail"/> indices from the draw (works on a non-readable mesh: no vertex is touched)
    static bool HideTail(Mesh m, int tail)
    {
        if (m == null || tail <= 0 || m.subMeshCount == 0) return false;
        var total = (int)m.GetIndexCount(0);
        try
        {
            m.SetSubMesh(0, new SubMeshDescriptor(0, Math.Max(0, total - tail)) { bounds = m.bounds, firstVertex = 0, vertexCount = m.vertexCount },
                MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers);
            return true;
        }
        catch (Exception e) { Debug.LogWarning("[GD3c teeth] could not shorten the draw range: " + e.Message); return false; }
    }

    /// <summary>The climb plant: the bottom teeth snap off (crumbs + TeethSnap). Returns how many snapped.</summary>
    public int SnapBottomTeeth()
    {
        var bottom = BottomMask();
        if (teeth.Count == 0 || bottom == 0 || (snapped & bottom) == bottom) return 0;
        var n = 0;
        var centre = Vector3.zero;
        var points = new List<Vector3>();
        for (var t = 0; t < teeth.Count; t++)
        {
            if (teeth[t].side != 'B' || Gone(t)) continue;
            n++;
            centre += teeth[t].centre;
            points.Add(teeth[t].centre);
        }
        if (n == 0) return 0;
        snapped |= bottom;
        if (Merged) { if (HideTail(floorMesh, MergedTail)) { FloorTris -= MergedTail / 3; MergedTail = 0; } }
        else if (teethVerts.Count > 0) UploadTeeth(TeethRenderer != null && TeethRenderer.enabled);
        else if (HideTail(teethMesh, teethTail)) { TeethTris -= teethTail / 3; teethTail = 0; }
        centre /= n;
        FrontRoomsGlassDebrisWorld.OnTeethSnapped(this, points);
        FrontRoomsGlassDebris.RaiseTeethSnap(transform.TransformPoint(centre));
        return n;
    }

    /// <summary>Tooth centres (root-local) of one side ('L', 'R', 'T', 'B'), or all with '\0'.</summary>
    public void ToothCentres(char side, List<Vector3> dst)
    {
        for (var t = 0; t < teeth.Count; t++) if (!Gone(t) && (side == '\0' || teeth[t].side == side)) dst.Add(teeth[t].centre);
    }

    // ---- floor glass ---------------------------------------------------------------------------------------------------------
    /// <summary>The two meshes the debris world's merge job writes into (made on first use, kept for this window).</summary>
    internal void FloorMeshes(out Mesh lod0, out Mesh lod1)
    {
        if (floorMesh == null) floorMesh = new Mesh { name = "GD3 floor glass" };
        if (floorLod1 == null) floorLod1 = new Mesh { name = "GD3 floor glass LOD1" };
        lod0 = floorMesh;
        lod1 = floorLod1;
    }

    /// <summary>After the job's result was applied: counts, renderers, the LOD group (hidden until ShowFloor).</summary>
    internal void FloorApplied(bool hasLod1, bool mergedWithTeeth)
    {
        FloorTris = floorMesh != null && floorMesh.subMeshCount > 0 ? (int)floorMesh.GetIndexCount(0) / 3 : 0;
        FloorVerts = floorMesh != null ? floorMesh.vertexCount : 0;
        FloorLod1Tris = hasLod1 && floorLod1 != null && floorLod1.subMeshCount > 0 ? (int)floorLod1.GetIndexCount(0) / 3 : 0;
        Merged = mergedWithTeeth;
        Lod1Only = false;
        if (FloorLods == null)
        {
            var go = new GameObject(FloorName);
            go.transform.SetParent(transform, false);
            go.SetActive(false);
            FloorLods = go.AddComponent<LODGroup>();
            FloorRenderer = MakeRenderer(Lod0Name, go.transform, Vector3.zero, floorMesh);
            FloorLod1Renderer = MakeRenderer(Lod1Name, go.transform, Vector3.zero, floorLod1);
            ApplyFloor(FloorRenderer, block ?? new MaterialPropertyBlock());
            ApplyFloor(FloorLod1Renderer, block ?? new MaterialPropertyBlock());
        }
        else
        {
            FloorRenderer.GetComponent<MeshFilter>().sharedMesh = floorMesh;
            FloorLod1Renderer.GetComponent<MeshFilter>().sharedMesh = floorLod1;
            FloorRenderer.enabled = true;
        }
        if (hasLod1 && FloorLod1Tris > 0)
        {
            FloorLod1Renderer.enabled = true;
            FloorLods.SetLODs(new[] { new LOD(Lod0Height, new Renderer[] { FloorRenderer }), new LOD(Lod1Height, new Renderer[] { FloorLod1Renderer }) });
        }
        else
        {
            FloorLod1Renderer.enabled = false;
            FloorLods.SetLODs(new[] { new LOD(Lod1Height, new Renderer[] { FloorRenderer }) });
        }
        FloorLods.RecalculateBounds();
        if (block != null) ApplyBlock(block);
    }

    public void ShowFloor(bool on)
    {
        if (FloorLods != null) FloorLods.gameObject.SetActive(on && FloorTris > 0);
        if (on && Merged && TeethRenderer != null) TeethRenderer.enabled = false;
    }

    /// <summary>F6: free the CPU copies of the persistent meshes (they stay on the GPU).</summary>
    internal void MakeNonReadable()
    {
        if (KeepReadable) return;
        foreach (var m in new[] { floorMesh, floorLod1, teethMesh })
            if (m != null && m.isReadable && m.vertexCount > 0) m.UploadMeshData(true);
    }

    /// <summary>The live-memory cap (gd3c sec 6.2): keep LOD1 only (what draws at this distance anyway).</summary>
    internal void DropToLod1Only()
    {
        if (Lod1Only || FloorLods == null || FloorLod1Tris == 0) return;
        Lod1Only = true;
        FloorLods.SetLODs(new[] { new LOD(Lod1Height, new Renderer[] { FloorLod1Renderer }) });
        FloorRenderer.enabled = false;
        FloorRenderer.GetComponent<MeshFilter>().sharedMesh = null;
        if (floorMesh != null) { Destroy(floorMesh); floorMesh = null; }
        FloorLods.RecalculateBounds();
    }

    /// <summary>Reduced tier: the teeth mesh is no longer drawn once it is merged into the floor (its GPU copy goes).</summary>
    internal void DropTeethMesh()
    {
        if (TeethRenderer != null) { TeethRenderer.enabled = false; TeethRenderer.GetComponent<MeshFilter>().sharedMesh = null; }
        if (teethMesh != null) { Destroy(teethMesh); teethMesh = null; }
    }

    internal void ApplyBlock(MaterialPropertyBlock b)
    {
        block = b;
        if (TeethRenderer != null) TeethRenderer.SetPropertyBlock(b);
        ApplyFloor(FloorRenderer, b);
        ApplyFloor(FloorLod1Renderer, b);
    }

    static readonly int PaneSizeId = Shader.PropertyToID("_PaneSize");
    static MaterialPropertyBlock floorScratch;

    /// <summary>The pane's block plus the shader's floor mode (_PaneSize.w = 1) on a floor-glass renderer.</summary>
    static void ApplyFloor(Renderer r, MaterialPropertyBlock b)
    {
        if (r == null) return;
        floorScratch ??= new MaterialPropertyBlock();
        r.SetPropertyBlock(b);
        r.GetPropertyBlock(floorScratch);
        floorScratch.SetVector(PaneSizeId, new Vector4((float)FrontRoomsGlassFracturePattern.SlabW, (float)FrontRoomsGlassFracturePattern.SlabH, (float)FrontRoomsGlassFracturePattern.SlabT, 1f));
        r.SetPropertyBlock(floorScratch);
    }

    MeshRenderer MakeRenderer(string objectName, Transform parent, Vector3 localPos, Mesh mesh)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = FrontRoomsGlassBreakable.FractureMaterial;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.BlendProbes;
        go.AddComponent<FrontRoomsMetalGlassTarget>();
        if (block != null) r.SetPropertyBlock(block);
        return r;
    }

    static void Upload(Mesh mesh, List<FrontRoomsGlassFractureMeshes.Vertex> v, List<int> ix)
    {
        mesh.Clear();
        const MeshUpdateFlags flags = MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds;
        var compact = FrontRoomsGlassDebrisJobs.CompactFloorVertex;
        mesh.SetVertexBufferParams(v.Count, FrontRoomsGlassDebrisJobs.Layout(compact));
        if (v.Count > 0)
        {
            if (compact)
            {
                var c = new FrontRoomsGlassDebrisJobs.FloorVertex[v.Count];
                for (var i = 0; i < c.Length; i++) c[i] = FrontRoomsGlassDebrisJobs.ToFloor(v[i]);
                mesh.SetVertexBufferData(c, 0, 0, c.Length, 0, flags);
            }
            else mesh.SetVertexBufferData(v, 0, 0, v.Count, 0, flags);
        }
        var fmt = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.SetIndexBufferParams(ix.Count, fmt);
        if (ix.Count > 0)
        {
            if (fmt == IndexFormat.UInt32) mesh.SetIndexBufferData(ix, 0, 0, ix.Count, flags);
            else
            {
                var s = new ushort[ix.Count];
                for (var i = 0; i < s.Length; i++) s[i] = (ushort)ix[i];
                mesh.SetIndexBufferData(s, 0, 0, s.Length, flags);
            }
        }
        mesh.subMeshCount = 1;
        mesh.SetSubMesh(0, new SubMeshDescriptor(0, ix.Count), flags);
        // bounds from the source positions (the mesh may be non-readable later)
        var mn = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var mx = -mn;
        foreach (var i in ix) { mn = Vector3.Min(mn, v[i].position); mx = Vector3.Max(mx, v[i].position); }
        mesh.bounds = ix.Count > 0 ? new Bounds((mn + mx) * .5f, mx - mn) : new Bounds();
    }

    void OnDestroy()
    {
        FrontRoomsGlassDebris.liveFloors.Remove(this);
        if (teethMesh != null) Destroy(teethMesh);
        if (floorMesh != null) Destroy(floorMesh);
        if (floorLod1 != null) Destroy(floorLod1);
    }

    static long MeshBytes(Mesh m)
    {
        if (m == null || m.vertexCount == 0) return 0;
        long b = (long)m.vertexCount * m.GetVertexBufferStride(0);
        if (m.subMeshCount > 0) b += (long)m.GetIndexCount(0) * (m.indexFormat == IndexFormat.UInt32 ? 4 : 2);
        return b;
    }

    /// <summary>GPU memory of this window's persistent glass (bytes: vertex + index buffers of teeth, LOD0, LOD1).</summary>
    public long Bytes() => MeshBytes(teethMesh) + MeshBytes(floorMesh) + MeshBytes(floorLod1);

    /// <summary>CPU copies still held (0 once non-readable).</summary>
    public long CpuBytes()
    {
        long b = 0;
        foreach (var m in new[] { teethMesh, floorMesh, floorLod1 }) if (m != null && m.isReadable) b += MeshBytes(m);
        return b;
    }

    /// <summary>Persistent draws (teeth unless merged, the floor LOD group).</summary>
    public int Draws => (TeethRenderer != null && TeethRenderer.enabled ? 1 : 0) + (FloorLods != null && FloorLods.gameObject.activeSelf ? 1 : 0);
}
