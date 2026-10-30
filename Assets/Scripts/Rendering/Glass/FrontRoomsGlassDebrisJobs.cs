// FrontRoomsGlassDebrisJobs.cs -- GD3c (gd3c/10_design.md sec 2.4, F1/F2/F5/F6): the debris world's heavy work off the main
// thread, so no slice costs more than the plan's 1.5 ms (build) / 1.0 ms (merge) on the main thread.
//   HullBakeJob    every shard and tooth hull is cooked by PhysX on worker threads (Physics.BakeMesh is thread-safe); the
//                  main thread then only assigns the baked meshes to colliders (v1 cooked on the main thread: 11.1 ms frames).
//   MergeCountJob  the floor-glass merge, pass 1: which source triangles stay (F5: the bottom face and bottom bevel of a
//                  shard lying within 10 deg of flat cannot be seen on carpet and are dropped; the vertical crack faces stay), and a compact vertex remap
//                  for LOD0 (every kept triangle) and LOD1 (the pane faces only).
//   MergeWriteJob  pass 2: writes both meshes straight into Mesh.MeshData (Mesh.AllocateWritableMeshData), in the window
//                  root's frame: every crack face before every pane face (the stage-2 order: the faces blend over the cracks
//                  behind them in one draw), then the resting crumbs. The main thread only applies the result (v1 merged on
//                  the main thread: 12.9 ms frames). It also counts vertices inside the clear zone of the opening (V9).
// Floor meshes use a 44-byte vertex (normal SNorm16, UV0 half; F6) unless CompactFloorVertex is off; the stage and shard
// meshes keep FrontRoomsGlassFractureMeshes.Layout (52 B). Same material, same attributes for the shader.
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

public static class FrontRoomsGlassDebrisJobs
{
    /// <summary>F6: the floor glass and teeth in the 44-byte layout (normal SNorm16 x 4, UV0 Float16 x 2).</summary>
    public static bool CompactFloorVertex = true;

    [StructLayout(LayoutKind.Sequential)]
    public struct FloorVertex
    {
        public Vector3 position;
        public short nx, ny, nz, nw;
        public Color32 color;
        public ushort u, v;
        public Vector4 uv1;
    }

    public const int FloorVertexBytes = 44, FullVertexBytes = 52;

    // Managed descriptor arrays (eng r1 #5): the earlier Persistent NativeArrays were disposed only on an assembly reload, so
    // every quit logged "Leak Detected ... Persistent allocates 2". Mesh and MeshData take the managed form as well.
    static readonly VertexAttributeDescriptor[] compactLayout =
    {
        new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.SNorm16, 4),
        new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 4),
    };

    public static VertexAttributeDescriptor[] Layout(bool compact) => compact ? compactLayout : FrontRoomsGlassFractureMeshes.Layout;

    /// <summary>A unit vector as one float (octahedral, 2 x 12 bits, an integer below 2^24: exact in fp32). The floor glass
    /// carries each piece's pane normal in UV1.z this way (FrontRoomsGlass.shader OctDecode24, _PaneSize.w floor mode).</summary>
    public static float OctEncode24(Vector3 n)
    {
        var s = Mathf.Abs(n.x) + Mathf.Abs(n.y) + Mathf.Abs(n.z);
        if (s < 1e-8f) return OctEncode24(Vector3.forward);
        n /= s;
        float fx = n.x, fy = n.y;
        if (n.z < 0f)
        {
            fx = (1f - Mathf.Abs(n.y)) * (n.x >= 0f ? 1f : -1f);
            fy = (1f - Mathf.Abs(n.x)) * (n.y >= 0f ? 1f : -1f);
        }
        var qx = Math.Max(0, Math.Min(4095, (int)Math.Round((fx * .5f + .5f) * 4095f)));
        var qy = Math.Max(0, Math.Min(4095, (int)Math.Round((fy * .5f + .5f) * 4095f)));
        return qx * 4096 + qy;
    }

    /// <summary>The C# twin of the shader's OctDecode24 (tests).</summary>
    public static Vector3 OctDecode24(float code)
    {
        var v = (int)Math.Round(code);
        var qx = v / 4096;
        var qy = v - qx * 4096;
        var fx = qx / 4095f * 2f - 1f;
        var fy = qy / 4095f * 2f - 1f;
        var n = new Vector3(fx, fy, 1f - Mathf.Abs(fx) - Mathf.Abs(fy));
        var t = Mathf.Clamp01(-n.z);
        n.x += n.x >= 0f ? -t : t;
        n.y += n.y >= 0f ? -t : t;
        return n.normalized;
    }

    // ---- F1: hull cooking ------------------------------------------------------------------------------------------------
    public struct HullBakeJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<EntityId> ids;
        public MeshColliderCookingOptions options;
        public void Execute(int i) => Physics.BakeMesh(ids[i], true, options);
    }

    // ---- F2 / F5: the merge ----------------------------------------------------------------------------------------------
    /// <summary>One piece going into a merged mesh: its slab-local vertices and index ranges in the source, and the matrix that
    /// takes them into the window root's frame.</summary>
    public struct MergeItem
    {
        public Matrix4x4 m;
        public int v0, vn, sideStart, sideCount, faceStart, faceCount;
        /// <summary>1: the piece lies within 10 deg of flat: faces whose normal points down (y below DownY) are dropped.</summary>
        public byte dropDown;
        /// <summary>1: its pane faces go into LOD1 too.</summary>
        public byte lod1;
        /// <summary>1: drawn last (after the crumbs), so dropping it later only shortens the draw range (the bottom teeth that
        /// snap on the climb; the mesh can stay non-readable).</summary>
        public byte tail;
    }

    /// <summary>Normals more downward than this are dropped on a flat-lying piece (the bottom face and its bevel).</summary>
    public const float DownY = -.5f;

    public struct MergeCountJob : IJob
    {
        [ReadOnly] public NativeArray<FrontRoomsGlassFractureMeshes.Vertex> srcV;
        [ReadOnly] public NativeArray<int> srcI;
        [ReadOnly] public NativeArray<MergeItem> items;
        public NativeArray<int> remap0, remap1;
        public NativeArray<byte> keep;          // per source triangle
        public NativeArray<int> counts;         // 0 LOD0 verts (pieces), 1 LOD0 indices (pieces), 2 LOD1 verts, 3 LOD1 indices, 4 dropped tris

        public void Execute()
        {
            for (var i = 0; i < remap0.Length; i++) { remap0[i] = -1; remap1[i] = -1; }
            for (var i = 0; i < keep.Length; i++) keep[i] = 0;
            int nv0 = 0, ni0 = 0, nv1 = 0, ni1 = 0, dropped = 0;
            for (var k = 0; k < items.Length; k++)
            {
                var it = items[k];
                for (var t = it.sideStart; t < it.sideStart + it.sideCount; t += 3)
                {
                    // the bevels sit in the crack-face range: a flat piece's bottom bevel faces the carpet too
                    if (it.dropDown != 0)
                    {
                        var n = it.m.MultiplyVector(srcV[srcI[t]].normal);
                        var len = n.magnitude;
                        if (len > 1e-6f && n.y / len < DownY) { dropped++; continue; }
                    }
                    keep[t / 3] = 1;
                    for (var q = 0; q < 3; q++) { var s = srcI[t + q]; if (remap0[s] < 0) remap0[s] = nv0++; }
                    ni0 += 3;
                }
            }
            for (var k = 0; k < items.Length; k++)
            {
                var it = items[k];
                for (var t = it.faceStart; t < it.faceStart + it.faceCount; t += 3)
                {
                    if (it.dropDown != 0)
                    {
                        var n = it.m.MultiplyVector(srcV[srcI[t]].normal);
                        var len = n.magnitude;
                        if (len > 1e-6f && n.y / len < DownY) { dropped++; continue; }
                    }
                    keep[t / 3] = 1;
                    for (var q = 0; q < 3; q++) { var s = srcI[t + q]; if (remap0[s] < 0) remap0[s] = nv0++; }
                    ni0 += 3;
                    if (it.lod1 != 0)
                    {
                        for (var q = 0; q < 3; q++) { var s = srcI[t + q]; if (remap1[s] < 0) remap1[s] = nv1++; }
                        ni1 += 3;
                    }
                }
            }
            counts[0] = nv0; counts[1] = ni0; counts[2] = nv1; counts[3] = ni1; counts[4] = dropped;
        }
    }

    public struct MergeWriteJob : IJob
    {
        [ReadOnly] public NativeArray<FrontRoomsGlassFractureMeshes.Vertex> srcV;
        [ReadOnly] public NativeArray<int> srcI;
        [ReadOnly] public NativeArray<MergeItem> items;
        [ReadOnly] public NativeArray<int> remap0, remap1;
        [ReadOnly] public NativeArray<byte> keep;
        [ReadOnly] public NativeArray<FrontRoomsGlassFractureMeshes.Vertex> crumbV;   // root frame already
        [ReadOnly] public NativeArray<int> crumbI;                                    // relative to crumbV
        public Mesh.MeshData lod0, lod1;
        public bool hasLod1, compact, wide0, wide1;
        public int pieceVerts0;
        public Vector3 clearMin, clearMax;
        public NativeArray<Vector3> bounds;    // 0/1 LOD0 min/max, 2/3 LOD1 min/max
        public NativeArray<int> stats;         // 0 vertices of LOD0 inside the clear zone, 1 LOD0 indices of the tail items, 2 piece vertices inside the frame
        /// <summary>The dressed frame's parts as boxes in the root frame (min, max pairs; art r1 #2: no floor-glass vertex may sit
        /// inside the sill nosing, stool, apron or casings). Empty = not checked.</summary>
        [ReadOnly] public NativeArray<Vector3> frameBoxes;

        // the output buffers, fetched once per job (not per vertex); locals, so the job holds no unassigned containers
        struct Out
        {
            public NativeArray<FloorVertex> c;
            public NativeArray<FrontRoomsGlassFractureMeshes.Vertex> f;
            public NativeArray<ushort> s;
            public NativeArray<int> l;
            public bool compact, wide;
            public void V(int i, FrontRoomsGlassFractureMeshes.Vertex vx) { if (compact) c[i] = ToFloor(vx); else f[i] = vx; }
            public void I(int at, int value) { if (wide) l[at] = value; else s[at] = (ushort)value; }
        }

        static Out Open(Mesh.MeshData md, bool compact, bool wide)
        {
            var o = new Out { compact = compact, wide = wide };
            if (compact) o.c = md.GetVertexData<FloorVertex>(); else o.f = md.GetVertexData<FrontRoomsGlassFractureMeshes.Vertex>();
            if (wide) o.l = md.GetIndexData<int>(); else o.s = md.GetIndexData<ushort>();
            return o;
        }

        public void Execute()
        {
            var o0 = Open(lod0, compact, wide0);
            var o1 = hasLod1 ? Open(lod1, compact, wide1) : default;
            var mn0 = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var mx0 = -mn0;
            var mn1 = mn0;
            var mx1 = mx0;
            var inClear = 0;
            var inFrame = 0;
            // vertices
            for (var k = 0; k < items.Length; k++)
            {
                var it = items[k];
                // the piece's pane normal (slab +Z) in the root frame: the floor-mode pane frame (art r1, F4)
                var paneCode = OctEncode24(it.m.MultiplyVector(Vector3.forward));
                for (var s = it.v0; s < it.v0 + it.vn; s++)
                {
                    var d0 = remap0[s];
                    var d1 = hasLod1 ? remap1[s] : -1;
                    if (d0 < 0 && d1 < 0) continue;
                    var vx = srcV[s];
                    vx.uv1.z = paneCode;
                    vx.position = it.m.MultiplyPoint3x4(vx.position);
                    var nn = it.m.MultiplyVector(vx.normal);
                    var l = nn.magnitude;
                    vx.normal = l > 1e-8f ? nn / l : Vector3.up;
                    if (d0 >= 0)
                    {
                        o0.V(d0, vx);
                        mn0 = Vector3.Min(mn0, vx.position); mx0 = Vector3.Max(mx0, vx.position);
                        var p = vx.position;
                        if (p.x > clearMin.x && p.x < clearMax.x && p.y > clearMin.y && p.y < clearMax.y && p.z > clearMin.z && p.z < clearMax.z) inClear++;
                        for (var fbx = 0; fbx + 1 < frameBoxes.Length; fbx += 2)
                        {
                            var bmn = frameBoxes[fbx];
                            var bmx = frameBoxes[fbx + 1];
                            if (p.x > bmn.x + .001f && p.x < bmx.x - .001f && p.y > bmn.y + .001f && p.y < bmx.y - .001f && p.z > bmn.z + .001f && p.z < bmx.z - .001f) { inFrame++; break; }
                        }
                    }
                    if (d1 >= 0)
                    {
                        o1.V(d1, vx);
                        mn1 = Vector3.Min(mn1, vx.position); mx1 = Vector3.Max(mx1, vx.position);
                    }
                }
            }
            for (var j = 0; j < crumbV.Length; j++)
            {
                var vx = crumbV[j];
                o0.V(pieceVerts0 + j, vx);
                mn0 = Vector3.Min(mn0, vx.position); mx0 = Vector3.Max(mx0, vx.position);
            }
            // indices: every crack face, then every pane face, then the crumbs, then the tail items (LOD1: pane faces only)
            var w0 = 0;
            var w1 = 0;
            for (var pass = 0; pass < 2; pass++)
                for (var k = 0; k < items.Length; k++)
                {
                    var it = items[k];
                    if (it.tail != 0) continue;
                    var start = pass == 0 ? it.sideStart : it.faceStart;
                    var count = pass == 0 ? it.sideCount : it.faceCount;
                    for (var t = start; t < start + count; t += 3)
                    {
                        if (keep[t / 3] == 0) continue;
                        for (var q = 0; q < 3; q++) o0.I(w0++, remap0[srcI[t + q]]);
                        if (pass == 1 && hasLod1 && it.lod1 != 0)
                            for (var q = 0; q < 3; q++) o1.I(w1++, remap1[srcI[t + q]]);
                    }
                }
            for (var j = 0; j < crumbI.Length; j++) o0.I(w0++, pieceVerts0 + crumbI[j]);
            var tail0 = w0;
            for (var k = 0; k < items.Length; k++)
            {
                var it = items[k];
                if (it.tail == 0) continue;
                for (var pass = 0; pass < 2; pass++)
                {
                    var start = pass == 0 ? it.sideStart : it.faceStart;
                    var count = pass == 0 ? it.sideCount : it.faceCount;
                    for (var t = start; t < start + count; t += 3)
                    {
                        if (keep[t / 3] == 0) continue;
                        for (var q = 0; q < 3; q++) o0.I(w0++, remap0[srcI[t + q]]);
                    }
                }
            }
            stats[1] = w0 - tail0;
            if (w0 == 0) { mn0 = Vector3.zero; mx0 = Vector3.zero; }
            if (w1 == 0) { mn1 = Vector3.zero; mx1 = Vector3.zero; }
            const MeshUpdateFlags flags = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers;
            lod0.subMeshCount = 1;
            lod0.SetSubMesh(0, new SubMeshDescriptor(0, w0) { bounds = new Bounds((mn0 + mx0) * .5f, mx0 - mn0), vertexCount = lod0.vertexCount }, flags);
            if (hasLod1)
            {
                lod1.subMeshCount = 1;
                lod1.SetSubMesh(0, new SubMeshDescriptor(0, w1) { bounds = new Bounds((mn1 + mx1) * .5f, mx1 - mn1), vertexCount = lod1.vertexCount }, flags);
            }
            bounds[0] = mn0; bounds[1] = mx0; bounds[2] = mn1; bounds[3] = mx1;
            stats[0] = inClear;
            stats[2] = inFrame;
        }

    }

    public static FloorVertex ToFloor(FrontRoomsGlassFractureMeshes.Vertex v) => new FloorVertex
    {
        position = v.position,
        nx = SNorm16(v.normal.x), ny = SNorm16(v.normal.y), nz = SNorm16(v.normal.z), nw = 0,
        color = v.color,
        u = Half(v.uv0.x), v = Half(v.uv0.y),
        uv1 = v.uv1,
    };

    static short SNorm16(float f) => (short)Math.Max(-32767, Math.Min(32767, (int)Math.Round(f * 32767.0)));

    [StructLayout(LayoutKind.Explicit)]
    struct FloatBits
    {
        [FieldOffset(0)] public float f;
        [FieldOffset(0)] public int i;
    }

    /// <summary>IEEE 754 binary16 from a float (round to nearest even), pure C# (safe on any thread).</summary>
    public static ushort Half(float f)
    {
        var bits = new FloatBits { f = f }.i;
        var sign = (bits >> 16) & 0x8000;
        var exp = ((bits >> 23) & 0xFF) - 127 + 15;
        var mant = bits & 0x7FFFFF;
        if (exp <= 0)
        {
            if (exp < -10) return (ushort)sign;
            mant |= 0x800000;
            var shift = 14 - exp;
            var half = mant >> shift;
            var rem = mant & ((1 << shift) - 1);
            var mid = 1 << (shift - 1);
            if (rem > mid || (rem == mid && (half & 1) != 0)) half++;
            return (ushort)(sign | half);
        }
        if (exp >= 31) return (ushort)(sign | 0x7C00 | (((bits >> 23) & 0xFF) == 0xFF && mant != 0 ? 0x200 : 0));
        var h = (exp << 10) | (mant >> 13);
        var r = mant & 0x1FFF;
        if (r > 0x1000 || (r == 0x1000 && (h & 1) != 0)) h++;
        return (ushort)(sign | h);
    }
}
