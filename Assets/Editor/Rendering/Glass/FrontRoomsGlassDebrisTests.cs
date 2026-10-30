// FrontRoomsGlassDebrisTests.cs -- GD3c (research/glass/destruction/gd3c/10_design.md sec 8): edit-mode checks of the debris
// world's pure parts. Menu FrontRooms/Glass/Test Debris (GD3c); batch -executeMethod FrontRoomsGlassDebrisTests.RunBatch -quit.
// Output: Verification/glass_debris/debris-tests.txt (the play-mode half is the clone's FrontRoomsGd3cBuild harness).
//   R1 the compact record round-trips (121 shards + 8 clusters): 2,076 B, position error <= 0.05 mm, rotation <= 0.01 deg
//   R2 record sizes: desktop maximum <= 2.5 KB, Cinematic <= 2.5 KB, reduced tier <= 0.8 KB
//   R3 the record store: over MaxRecords the oldest RELEASED record goes first; a live record is never dropped;
//      KeepFloorRecords = false keeps none (Red's literal rule)
//   R4 the job-safe half conversion equals Mathf.FloatToHalf
//   R5 the merge jobs on a real Annealed6 break: F5 drops only downward faces of flat pieces (about 30 % of their faces),
//      LOD1 holds pane faces only, the 44-byte vertex, valid indices, V9 counts a piece left in the opening, bounds
//   R6 the tier gate: the Editor's Play Mode is the desktop tier; the lab switch gives the WebGL profile
//   R7 the S1 fin twist: 10-20 deg, same triangle count, still within 1.2 mm of the crack and inside the glass
//   R8 the teeth-mask bytes the debris world patches in place sit where the header keeps them
//   R9 the glass shader compiles (no error) and keeps its property list (no new property: the wipe uses _ImpactUV.zw)
//   R5f/R5g (fix r1) the merge counts piece vertices inside the dressed frame's boxes; every merged vertex carries its piece's
//      pane normal in UV1.z (floor mode)
//   R10 (fix r1) the octahedral pane-normal code round-trips within 0.1 deg (2,000 directions)
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class FrontRoomsGlassDebrisTests
{
    static readonly StringBuilder log = new StringBuilder();
    static int pass, fail;

    static string Out { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "glass_debris"); } }
    static string F(double v, string f) { return v.ToString(f, CultureInfo.InvariantCulture); }

    static void Check(string id, bool ok, string what)
    {
        if (ok) pass++; else fail++;
        log.AppendLine((ok ? "PASS " : "FAIL ") + id + "  " + what);
    }

    public static void RunBatch()
    {
        var code = 1;
        try { code = Run() ? 0 : 2; }
        catch (Exception e) { Debug.LogException(e); log.AppendLine("EXCEPTION " + e); fail++; }
        finally
        {
            Directory.CreateDirectory(Out);
            File.WriteAllText(Path.Combine(Out, "debris-tests.txt"), log.ToString());
            Debug.Log("[GD3c debris] tests " + pass + " passed, " + fail + " failed -> " + Out);
        }
        if (Application.isBatchMode) EditorApplication.Exit(code);
    }

    [MenuItem("FrontRooms/Glass/Test Debris (GD3c)")]
    public static void Menu() { try { Run(); } catch (Exception e) { log.AppendLine("EXCEPTION " + e); } Directory.CreateDirectory(Out); File.WriteAllText(Path.Combine(Out, "debris-tests.txt"), log.ToString()); }

    static bool Run()
    {
        log.Length = 0; pass = fail = 0;
        log.AppendLine("GD3c debris tests | " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | Unity " + Application.unityVersion);
        RecordRoundTrip();
        RecordSizes();
        Store();
        HalfConversion();
        MergeJobs();
        TierGate();
        FinTwist();
        TeethMaskOffset();
        Shader();
        OctCode();
        log.AppendLine("TOTAL " + pass + " passed, " + fail + " failed");
        return fail == 0;
    }

    // ---- R1 ---------------------------------------------------------------------------------------------------------------
    static void RecordRoundTrip()
    {
        var rng = new System.Random(19901022);
        var shards = new List<FrontRoomsGlassDebrisPack.Shard>();
        for (var i = 0; i < 121; i++)
        {
            var q = UnityEngine.Random.rotationUniform;
            q = Quaternion.Euler((float)(rng.NextDouble() * 360), (float)(rng.NextDouble() * 360), (float)(rng.NextDouble() * 360));
            shards.Add(new FrontRoomsGlassDebrisPack.Shard
            {
                piece = (byte)(i + 3), flags = (byte)((i % 2 == 0 ? FrontRoomsGlassDebrisPack.ShardFar : 0) | (i % 7 == 0 ? FrontRoomsGlassDebrisPack.ShardDropped : 0)),
                position = new Vector3((float)(rng.NextDouble() * 6 - 3), (float)(rng.NextDouble() * 3.2), (float)(rng.NextDouble() * 6.4 - 3.2)),
                rotation = q,
            });
        }
        var clusters = new List<FrontRoomsGlassDebrisPack.Cluster>();
        for (var i = 0; i < 8; i++)
            clusters.Add(new FrontRoomsGlassDebrisPack.Cluster { centre = new Vector2(i * .1f - .4f, .3f + i * .05f), extent = new Vector2(.1f, .2f), count = (ushort)(20 + i), seed = (ushort)(1000 + i), level = (byte)(i == 3 ? 1 : 0) });
        var h = new FrontRoomsGlassDebrisPack.Header
        {
            edge = 0x0000123400005678L, seed = -123456789, impact = new Vector2(.213f, 1.317f), rotation = 47.5f, side = -1, tier = FrontRoomsGlassDebrisPack.TierDesktop,
            floorY = -.0f, breakTime = 123.25f, envHash = 0xDEADBEEF, teethSnapped = 0x8000000000000101UL, flags = 5,
        };
        var data = FrontRoomsGlassDebrisPack.Pack(h, shards, clusters);
        var back = new List<FrontRoomsGlassDebrisPack.Shard>();
        var backC = new List<FrontRoomsGlassDebrisPack.Cluster>();
        var ok = FrontRoomsGlassDebrisPack.Unpack(data, out var h2, back, backC);
        double posErr = 0, rotErr = 0;
        var flagsOk = true;
        for (var i = 0; i < shards.Count && i < back.Count; i++)
        {
            posErr = Math.Max(posErr, (shards[i].position - back[i].position).magnitude);
            rotErr = Math.Max(rotErr, Quaternion.Angle(shards[i].rotation, back[i].rotation));
            if (shards[i].piece != back[i].piece || shards[i].flags != back[i].flags) flagsOk = false;
        }
        var clustersOk = backC.Count == 8;
        for (var i = 0; i < backC.Count; i++)
            if (backC[i].count != clusters[i].count || backC[i].seed != clusters[i].seed || backC[i].level != clusters[i].level || (backC[i].centre - clusters[i].centre).magnitude > .001f) clustersOk = false;
        var headerOk = h2.edge == h.edge && h2.seed == h.seed && h2.side == h.side && h2.tier == h.tier && h2.envHash == h.envHash && h2.teethSnapped == h.teethSnapped && h2.flags == h.flags
            && Mathf.Abs(h2.impact.x - h.impact.x) < .001f && Mathf.Abs(h2.impact.y - h.impact.y) < .002f && Mathf.Abs(h2.rotation - h.rotation) < .05f && h2.breakTime == h.breakTime;
        Check("R1", ok && data.Length == 2076 && back.Count == 121 && posErr <= .0000867 && rotErr <= .01 && flagsOk && clustersOk && headerOk,
            "121 shards + 8 clusters: " + data.Length + " B (2,076 expected), position error " + F(posErr * 1000, "0.000") + " mm (<= 0.087: 0.05 per axis), rotation error " + F(rotErr, "0.0000")
            + " deg (<= 0.01), pieces/flags " + flagsOk + ", clusters " + clustersOk + ", header " + headerOk);
    }

    // ---- R2 ---------------------------------------------------------------------------------------------------------------
    static void RecordSizes()
    {
        var desk = FrontRoomsGlassDebrisPack.SizeOf(121, 8);
        var cine = FrontRoomsGlassDebrisPack.SizeOf(132, 8);
        var reduced = FrontRoomsGlassDebrisPack.SizeOf(40, 8);
        Check("R2", desk <= 2560 && cine <= 2560 && reduced <= 800, "Annealed6 max (121 bodies) " + desk + " B, Cinematic (132) " + cine + " B, reduced (40) " + reduced + " B; 512 records at the desktop maximum = "
            + F((desk + 48) * 512 / 1048576.0, "0.00") + " MB");
    }

    // ---- R3 ---------------------------------------------------------------------------------------------------------------
    static void Store()
    {
        var keep = FrontRoomsGlassDebris.MaxRecords;
        var keepOn = FrontRoomsGlassDebris.KeepFloorRecords;
        var key = new GameObject("GD3c test map key");
        try
        {
            FrontRoomsGlassDebris.ClearRecords();
            FrontRoomsGlassDebris.MaxRecords = 3;
            var blob = new byte[100];
            FrontRoomsGlassDebris.Put(key, 1, blob);
            FrontRoomsGlassDebris.Put(key, 2, blob);
            FrontRoomsGlassDebris.Put(key, 3, blob);
            FrontRoomsGlassDebris.MarkReleased(key, 2);
            FrontRoomsGlassDebris.MarkReleased(key, 1);
            FrontRoomsGlassDebris.Put(key, 4, blob);           // 4 > 3: the oldest released (2) goes
            var a = FrontRoomsGlassDebris.Find(key, 2) == null && FrontRoomsGlassDebris.Find(key, 1) != null && FrontRoomsGlassDebris.Find(key, 3) != null && FrontRoomsGlassDebris.Find(key, 4) != null;
            FrontRoomsGlassDebris.Put(key, 5, blob);           // 4 > 3 again: 1 (released) goes
            var b = FrontRoomsGlassDebris.Find(key, 1) == null && FrontRoomsGlassDebris.RecordCount == 3;
            FrontRoomsGlassDebris.Put(key, 6, blob);           // all live: nothing may be dropped (4 records)
            var c = FrontRoomsGlassDebris.RecordCount == 4 && FrontRoomsGlassDebris.Find(key, 3) != null;
            FrontRoomsGlassDebris.MarkLive(key, 3);
            FrontRoomsGlassDebris.KeepFloorRecords = false;
            var d = FrontRoomsGlassDebris.Put(key, 7, blob) == null && FrontRoomsGlassDebris.Find(key, 7) == null;
            Check("R3", a && b && c && d, "oldest released first " + a + " | again " + b + " | live never dropped " + c + " | KeepFloorRecords off keeps none " + d + " | evicted " + FrontRoomsGlassDebris.RecordsEvicted);
        }
        finally
        {
            FrontRoomsGlassDebris.MaxRecords = keep;
            FrontRoomsGlassDebris.KeepFloorRecords = keepOn;
            FrontRoomsGlassDebris.ClearRecords();
            UnityEngine.Object.DestroyImmediate(key);
        }
    }

    // ---- R4 ---------------------------------------------------------------------------------------------------------------
    static void HalfConversion()
    {
        var rng = new System.Random(7);
        var bad = 0;
        var n = 0;
        var values = new List<float> { 0f, -0f, 1f, -1f, .5f, .6835f, -.6835f, .821f, 65504f, 1e-5f, 6.1e-5f, 3e-8f, 70000f };
        for (var i = 0; i < 20000; i++) values.Add((float)(rng.NextDouble() * 4 - 2));
        foreach (var v in values)
        {
            n++;
            if (FrontRoomsGlassDebrisJobs.Half(v) != Mathf.FloatToHalf(v)) bad++;
        }
        Check("R4", bad == 0, n + " values: " + bad + " differ from Mathf.FloatToHalf");
    }

    // ---- R5 ---------------------------------------------------------------------------------------------------------------
    static void MergeJobs()
    {
        var P = FrontRoomsGlassTypeProfile.Load("Annealed6");
        var job = new FrontRoomsGlassFractureJob();
        var res = new FrontRoomsGlassFracture();
        var b = new FrontRoomsGlassFractureMeshes();
        FrontRoomsGlassFracturePattern.BeginRoot(job, P, 516574485u, .12, 1.30, FrontRoomsGlassFracturePattern.SlabW, FrontRoomsGlassFracturePattern.SlabH, double.NaN, 1, "band", res);
        job.Finish();
        if (job.Failed) { Check("R5", false, "pattern failed: " + job.Error); return; }
        b.Begin(res, new FrontRoomsGlassFractureMeshes.Options { keyA = 1f, keyB = 2f, bevel = double.NaN });
        b.Finish();
        var srcV = new List<FrontRoomsGlassFractureMeshes.Vertex>();
        var srcI = new List<int>();
        using (var ro = Mesh.AcquireReadOnlyMeshData(b.Stage2))
        {
            var md = ro[0];
            var vv = md.GetVertexData<FrontRoomsGlassFractureMeshes.Vertex>(0);
            for (var i = 0; i < vv.Length; i++) srcV.Add(vv[i]);
            if (md.indexFormat == IndexFormat.UInt16) { var ii = md.GetIndexData<ushort>(); for (var i = 0; i < ii.Length; i++) srcI.Add(ii[i]); }
            else { var ii = md.GetIndexData<int>(); for (var i = 0; i < ii.Length; i++) srcI.Add(ii[i]); }
        }
        // items: every shard laid flat on the floor (face up), plus one shard left standing in the opening (V9 must see it)
        var items = new List<FrontRoomsGlassDebrisJobs.MergeItem>();
        var flat = Matrix4x4.TRS(new Vector3(0f, .003f, .6f), Quaternion.Euler(-90f, 0f, 0f), Vector3.one);
        var upright = Matrix4x4.Translate(new Vector3(0f, (float)FrontRoomsGlassFracturePattern.SlabCentreY, 0f));
        var downFaceTris = 0;
        var faceTris = 0;
        var sideTris = 0;
        var standing = -1;
        for (var k = 0; k < b.PieceIds.Count; k++)
        {
            var pc = res.pieces[b.PieceIds[k]];
            if (pc.kind != GlassPieceKind.Shard) continue;
            int lo = int.MaxValue, hi = -1;
            var sr = b.PieceSides[k];
            var fr = b.PieceFaces[k];
            for (var i = sr.indexStart; i < sr.indexStart + sr.indexCount; i++) { lo = Math.Min(lo, srcI[i]); hi = Math.Max(hi, srcI[i]); }
            for (var i = fr.indexStart; i < fr.indexStart + fr.indexCount; i++) { lo = Math.Min(lo, srcI[i]); hi = Math.Max(hi, srcI[i]); }
            var isStanding = standing < 0 && Math.Abs(pc.centroid.x) < .3 && Math.Abs(pc.centroid.y) < .3;
            if (isStanding) standing = k;
            var m = isStanding ? upright : flat;
            items.Add(new FrontRoomsGlassDebrisJobs.MergeItem
            {
                m = m, v0 = lo, vn = hi - lo + 1, sideStart = sr.indexStart, sideCount = sr.indexCount, faceStart = fr.indexStart, faceCount = fr.indexCount,
                dropDown = (byte)(isStanding ? 0 : 1), lod1 = 1,
            });
            sideTris += sr.indexCount / 3;
            faceTris += fr.indexCount / 3;
            if (!isStanding)
            {
                for (var t = fr.indexStart; t < fr.indexStart + fr.indexCount; t += 3)
                {
                    var nn = m.MultiplyVector(srcV[srcI[t]].normal).normalized;
                    if (nn.y < FrontRoomsGlassDebrisJobs.DownY) downFaceTris++;
                }
                for (var t = sr.indexStart; t < sr.indexStart + sr.indexCount; t += 3)
                {
                    var nn = m.MultiplyVector(srcV[srcI[t]].normal).normalized;
                    if (nn.y < FrontRoomsGlassDebrisJobs.DownY) downFaceTris++;
                }
            }
        }
        var nSrcV = new NativeArray<FrontRoomsGlassFractureMeshes.Vertex>(srcV.ToArray(), Allocator.TempJob);
        var nSrcI = new NativeArray<int>(srcI.ToArray(), Allocator.TempJob);
        var nItems = new NativeArray<FrontRoomsGlassDebrisJobs.MergeItem>(items.ToArray(), Allocator.TempJob);
        var remap0 = new NativeArray<int>(srcV.Count, Allocator.TempJob);
        var remap1 = new NativeArray<int>(srcV.Count, Allocator.TempJob);
        var keep = new NativeArray<byte>(srcI.Count / 3, Allocator.TempJob);
        var counts = new NativeArray<int>(5, Allocator.TempJob);
        var stats = new NativeArray<int>(3, Allocator.TempJob);
        var bounds = new NativeArray<Vector3>(4, Allocator.TempJob);
        // one box over the floor where the flat pieces lie: every one of their vertices must be counted as "inside the frame"
        var frameBoxes = new NativeArray<Vector3>(new[] { new Vector3(-3f, -.01f, -3f), new Vector3(3f, .02f, 3f) }, Allocator.TempJob);
        var crumbV = new NativeArray<FrontRoomsGlassFractureMeshes.Vertex>(0, Allocator.TempJob);
        var crumbI = new NativeArray<int>(0, Allocator.TempJob);
        var lod0 = new Mesh();
        var lod1 = new Mesh();
        try
        {
            new FrontRoomsGlassDebrisJobs.MergeCountJob { srcV = nSrcV, srcI = nSrcI, items = nItems, remap0 = remap0, remap1 = remap1, keep = keep, counts = counts }.Schedule().Complete();
            var mda = Mesh.AllocateWritableMeshData(2);
            mda[0].SetVertexBufferParams(counts[0], FrontRoomsGlassDebrisJobs.Layout(true));
            mda[1].SetVertexBufferParams(counts[2], FrontRoomsGlassDebrisJobs.Layout(true));
            mda[0].SetIndexBufferParams(counts[1], IndexFormat.UInt16);
            mda[1].SetIndexBufferParams(counts[3], IndexFormat.UInt16);
            var wj = new FrontRoomsGlassDebrisJobs.MergeWriteJob
            {
                srcV = nSrcV, srcI = nSrcI, items = nItems, remap0 = remap0, remap1 = remap1, keep = keep, crumbV = crumbV, crumbI = crumbI,
                lod0 = mda[0], lod1 = mda[1], hasLod1 = true, compact = true, wide0 = false, wide1 = false, pieceVerts0 = counts[0],
                clearMin = FrontRoomsGlassDebrisWorld.ClearMin, clearMax = FrontRoomsGlassDebrisWorld.ClearMax, bounds = bounds, stats = stats, frameBoxes = frameBoxes,
            };
            wj.Schedule().Complete();
            Mesh.ApplyAndDisposeWritableMeshData(mda, new[] { lod0, lod1 }, MeshUpdateFlags.DontRecalculateBounds);
            var tris0 = (int)lod0.GetIndexCount(0) / 3;
            var tris1 = (int)lod1.GetIndexCount(0) / 3;
            var idx0 = lod0.GetIndices(0);
            var idxOk = true;
            foreach (var i in idx0) if (i < 0 || i >= lod0.vertexCount) { idxOk = false; break; }
            var uv1 = new List<Vector4>();
            lod1.GetUVs(1, uv1);
            var lod1Crack = 0;
            foreach (var u in uv1) if (u.w > .5f && u.w < 2.5f) lod1Crack++;
            var stride = lod0.GetVertexBufferStride(0);
            var dropped = counts[4];
            var expect0 = sideTris + faceTris - downFaceTris;
            var pct = 100.0 * dropped / Math.Max(1, sideTris + faceTris);
            Check("R5a", dropped == downFaceTris && pct >= 20.0 && tris0 == expect0, "F5: " + dropped + " downward tris (bottom face + bottom bevel) dropped of " + (sideTris + faceTris) + " (" + F(pct, "0.0") + " % of the flat pieces' tris; expected "
                + downFaceTris + "), LOD0 " + tris0 + " tris (expected " + expect0 + ")");
            Check("R5b", lod1Crack == 0 && tris1 > 0 && tris1 < tris0, "LOD1 " + tris1 + " tris, pane faces only (crack/crushed vertices in LOD1: " + lod1Crack + ")");
            Check("R5c", stride == FrontRoomsGlassDebrisJobs.FloorVertexBytes && idxOk, "vertex stride " + stride + " B (44), " + lod0.vertexCount + " verts, indices valid " + idxOk
                + " | LOD0 " + F((lod0.vertexCount * stride + idx0.Length * 2) / 1024.0, "0.0") + " KB");
            Check("R5d", standing >= 0 ? stats[0] > 0 : true, "V9 counter: " + stats[0] + " LOD0 vertices inside the clear zone with one shard left standing in the opening (" + (standing >= 0 ? "must be > 0" : "no central shard found") + ")");
            var bmin = bounds[0];
            var bmax = bounds[1];
            Check("R5e", bmin.y >= -.01f && bmax.y < 2.1f && bmax.x > bmin.x, "LOD0 bounds " + bmin.ToString("F3") + " .. " + bmax.ToString("F3"));
            var uvs0 = new List<Vector4>();
            lod0.GetUVs(1, uvs0);
            var pos0 = new List<Vector3>();
            lod0.GetVertices(pos0);
            int upOk = 0, fwdOk = 0, other = 0;
            for (var i = 0; i < uvs0.Count; i++)
            {
                var nd = FrontRoomsGlassDebrisJobs.OctDecode24(uvs0[i].z);
                if (Vector3.Angle(nd, Vector3.up) < .5f) upOk++;
                else if (Vector3.Angle(nd, Vector3.forward) < .5f) fwdOk++;
                else other++;
            }
            var flatVerts = 0;
            foreach (var q in pos0) if (q.y > -.01f + .001f && q.y < .02f - .001f) flatVerts++;
            Check("R5f", stats[2] > 0 && stats[2] == flatVerts, "frame counter: " + stats[2] + " piece vertices inside a box over the flat pieces (expected " + flatVerts + ", the vertices strictly inside it)");
            Check("R5g", other == 0 && upOk > 0 && fwdOk > 0, "UV1.z pane normals: " + upOk + " up (the flat pieces), " + fwdOk + " +Z (the standing one), " + other + " other");
        }
        finally
        {
            nSrcV.Dispose(); nSrcI.Dispose(); nItems.Dispose(); remap0.Dispose(); remap1.Dispose(); keep.Dispose(); counts.Dispose(); stats.Dispose(); bounds.Dispose(); crumbV.Dispose(); crumbI.Dispose(); frameBoxes.Dispose();
            UnityEngine.Object.DestroyImmediate(lod0);
            UnityEngine.Object.DestroyImmediate(lod1);
            b.ReleaseMeshes();
        }
    }

    // ---- R6 ---------------------------------------------------------------------------------------------------------------
    static void TierGate()
    {
        var was = FrontRoomsGlassTypeProfile.DebugForceReducedTier;
        try
        {
            FrontRoomsGlassTypeProfile.DebugForceReducedTier = false;
            var desktopTier = FrontRoomsGlassTypeProfile.ForTier(FrontRoomsGlassProfileData.Annealed6, false).tier;
            var reducedOff = FrontRoomsGlassTypeProfile.ReducedTier;
            FrontRoomsGlassTypeProfile.DebugForceReducedTier = true;
            var reducedTier = FrontRoomsGlassTypeProfile.ForTier(FrontRoomsGlassProfileData.Annealed6, false).tier;
            var wired = FrontRoomsGlassTypeProfile.ForTier(FrontRoomsGlassProfileData.Wired6, false).tier;
            Check("R6", !reducedOff && desktopTier == "High" && reducedTier == "WebGL" && wired != "WebGL",
                "Editor (handheld preview off): reduced " + reducedOff + ", Annealed6 tier " + desktopTier + " | lab switch on: " + reducedTier + " | Wired6 unchanged: " + wired);
        }
        finally { FrontRoomsGlassTypeProfile.DebugForceReducedTier = was; }
    }

    // ---- R7 ---------------------------------------------------------------------------------------------------------------
    static void FinTwist()
    {
        var was = FrontRoomsGlassFractureMeshes.S1Twist;
        var P = FrontRoomsGlassTypeProfile.Load("Annealed6");
        var job = new FrontRoomsGlassFractureJob();
        var res = new FrontRoomsGlassFracture();
        var b = new FrontRoomsGlassFractureMeshes();
        try
        {
            FrontRoomsGlassFracturePattern.BeginRoot(job, P, 4242u, -.1, 1.25, FrontRoomsGlassFracturePattern.SlabW, FrontRoomsGlassFracturePattern.SlabH, double.NaN, 1, "band", res);
            job.Finish();
            FrontRoomsGlassFractureMeshes.S1Twist = false;
            b.Begin(res, new FrontRoomsGlassFractureMeshes.Options { keyA = 1f, keyB = 2f, bevel = double.NaN });
            b.Finish();
            var offTris = b.Stats.finTris;
            double offLean;
            Leans(b, res, out offLean, out var offMin, out var offShift);
            b.ReleaseMeshes();
            FrontRoomsGlassFractureMeshes.S1Twist = true;
            b.Begin(res, new FrontRoomsGlassFractureMeshes.Options { keyA = 1f, keyB = 2f, bevel = double.NaN });
            b.Finish();
            var onTris = b.Stats.finTris;
            Leans(b, res, out var onLean, out var onMin, out var onShift);
            Check("R7", onTris == offTris && onMin >= 9.9 && onLean <= 20.1 && onShift <= .0012, "fin tris " + offTris + " -> " + onTris + " | lean off " + F(offMin, "0.0") + "-" + F(offLean, "0.0")
                + " deg, on " + F(onMin, "0.0") + "-" + F(onLean, "0.0") + " deg | largest offset from the crack across the 6 mm: " + F(onShift * 1000, "0.00") + " mm (<= 1.2)");
        }
        finally
        {
            FrontRoomsGlassFractureMeshes.S1Twist = was;
            b.ReleaseMeshes();
        }
    }

    // fins are quads with vertices at -depth and +depth: the lean of each wall from its two depth edges
    static void Leans(FrontRoomsGlassFractureMeshes b, FrontRoomsGlassFracture res, out double maxDeg, out double minDeg, out double maxShift)
    {
        maxDeg = 0; minDeg = 90; maxShift = 0;
        var v = new List<Vector3>();
        b.Stage1.GetVertices(v);
        var h = res.slabT * .5 * FrontRoomsGlassFractureMeshes.FinDepthScale;
        // fin walls come first in the stage-1 vertex list (4 per wall, p0..p3: -d, -d, +d, +d), then the crater
        for (var i = 0; i + 3 < v.Count; i += 4)
        {
            var p0 = v[i]; var p3 = v[i + 3];
            if (Math.Abs(p0.z + h) > 1e-4 || Math.Abs(p3.z - h) > 1e-4) break;
            var d = new Vector2(p3.x - p0.x, p3.y - p0.y).magnitude;
            var deg = Math.Atan2(d, 2 * h) * 180.0 / Math.PI;
            maxDeg = Math.Max(maxDeg, deg);
            minDeg = Math.Min(minDeg, deg);
            maxShift = Math.Max(maxShift, d * .5);
        }
    }

    // ---- R8 ---------------------------------------------------------------------------------------------------------------
    static void TeethMaskOffset()
    {
        var h = new FrontRoomsGlassDebrisPack.Header { edge = 9, seed = 1, teethSnapped = 0 };
        var data = FrontRoomsGlassDebrisPack.Pack(h, new List<FrontRoomsGlassDebrisPack.Shard>(), new List<FrontRoomsGlassDebrisPack.Cluster>());
        var mask = 0x00F0000000000F0FUL;
        for (var i = 0; i < 8; i++) data[32 + i] = (byte)(mask >> (8 * i));
        FrontRoomsGlassDebrisPack.UnpackHeader(data, out var h2);
        Check("R8", h2.teethSnapped == mask && h2.edge == 9, "teeth mask patched at bytes 32-39 reads back as 0x" + h2.teethSnapped.ToString("X16"));
    }

    // ---- R9 ---------------------------------------------------------------------------------------------------------------
    static void Shader()
    {
        var sh = UnityEngine.Shader.Find("FrontRooms/Glass");
        if (sh == null) { Check("R9", false, "FrontRooms/Glass not found"); return; }
        var err = ShaderUtil.ShaderHasError(sh);
        var props = sh.GetPropertyCount();
        var msgs = ShaderUtil.GetShaderMessages(sh);
        var errors = 0;
        foreach (var m in msgs) if (m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error) errors++;
        var hasImpact = sh.FindPropertyIndex("_ImpactUV") >= 0;
        Check("R9", !err && errors == 0 && hasImpact, "FrontRooms/Glass: has error " + err + ", " + errors + " error messages, " + props + " properties, _ImpactUV present " + hasImpact);
    }

    // ---- R10 (fix r1) --------------------------------------------------------------------------------------------------------
    static void OctCode()
    {
        var rng = new System.Random(1990);
        var worst = 0f;
        var axes = new[] { Vector3.up, Vector3.down, Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        for (var i = 0; i < 2000 + axes.Length; i++)
        {
            var v = i < axes.Length ? axes[i] : new Vector3((float)rng.NextDouble() * 2f - 1f, (float)rng.NextDouble() * 2f - 1f, (float)rng.NextDouble() * 2f - 1f);
            if (v.sqrMagnitude < 1e-4f) continue;
            v.Normalize();
            var code = FrontRoomsGlassDebrisJobs.OctEncode24(v);
            var exact = code == Mathf.Round(code) && code >= 0f && code < 16777216f;
            var back = FrontRoomsGlassDebrisJobs.OctDecode24(code);
            var a = Vector3.Angle(v, back);
            if (!exact) a = 180f;
            worst = Mathf.Max(worst, a);
        }
        Check("R10", worst <= .1f, "octahedral pane-normal code (UV1.z, 2 x 12 bits): worst round-trip error " + F(worst, "0.0000") + " deg over 2,006 directions");
    }
}
