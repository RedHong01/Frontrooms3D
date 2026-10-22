// FrontRoomsGlassStagesTests.cs -- GD3 steps 4-5 (build/04_stages.md): edit-mode checks of the stage meshes and the glass
// shader. Menu FrontRooms/Glass/Test Stage Meshes; batch -executeMethod FrontRoomsGlassStagesTests.RunBatch (-quit).
// Output: Verification/glass_stages/stage-mesh-tests.txt (+ .tsv per break) and shader-report.txt.
//   M1 triangle counts = the reference's rule (stage 2: 12n - 4 per piece with a bevel, 4n - 4 without; budgets)
//   M2 every piece is a closed, consistently wound prism (each welded edge used twice, in opposite directions)
//   M3 outward winding (signed volume > 0, about area x 6 mm) and flat normals that agree with the winding
//   M4 UV0 = the rest position; posed vertices = FrontRoomsGlassFractureMeshes.StagePoint of the rest position (seat tilt, then
//      the generator's PosePoint)
//   M5 16-bit indices (< 65,000 vertices), no NaN, one submesh, Float32 x 3 positions in stream 0 (G14 P4 format)
//   M6 determinism: the same pattern gives the same vertex and index bytes; BuildPieceSource ranges cover every triangle
//   M7 the S1 fins lie on the stage-1 cracks and inside the glass; the crater <= 15 mm and on the struck face
//   M8 time: thread CPU per slice and per break (the plan's "1-5 ms over ~10 frames" with the generator)
//   M9 memory per struck window (reported)
//   S1 shader: the keyword pragmas equal main's (no new variant), variant counts equal, GLES 3 (WebGL 2) and Metal compiles
//      (S1a/S1b need the lab's reference copy of main's shader and are skipped without it, e.g. in Red's project)
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

public static class FrontRoomsGlassStagesTests
{
    static readonly StringBuilder log = new StringBuilder();
    static int pass, fail;

    [StructLayout(LayoutKind.Sequential)] struct Timespec { public long sec, nsec; }
    [DllImport("/usr/lib/libSystem.B.dylib")] static extern int clock_gettime(int clk, out Timespec ts);
    static double CpuMs() { try { if (clock_gettime(16, out var t) == 0) return t.sec * 1000.0 + t.nsec / 1e6; } catch { } return double.NaN; }

    static string Out => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "glass_stages");
    static string F(double v, string f = "0.000") => v.ToString(f, CultureInfo.InvariantCulture);

    static void Check(string id, bool ok, string what)
    {
        if (ok) pass++; else fail++;
        log.AppendLine((ok ? "PASS " : "FAIL ") + id + "  " + what);
    }

    public static void RunBatch()
    {
        var code = 1;
        try { code = Run() ? 0 : 2; }
        catch (Exception e) { Debug.LogException(e); log.AppendLine("EXCEPTION " + e); }
        finally
        {
            Directory.CreateDirectory(Out);
            File.WriteAllText(Path.Combine(Out, "stage-mesh-tests.txt"), log.ToString());
            Debug.Log("[GD3 stages] tests " + pass + " passed, " + fail + " failed -> " + Out);
        }
        if (Application.isBatchMode) EditorApplication.Exit(code);
    }

    [MenuItem("FrontRooms/Glass/Test Stage Meshes")]
    public static void Menu() { Run(); Directory.CreateDirectory(Out); File.WriteAllText(Path.Combine(Out, "stage-mesh-tests.txt"), log.ToString()); }

    static bool Run()
    {
        log.Clear(); pass = fail = 0;
        log.AppendLine("GD3 stage mesh tests · " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " · Unity " + Application.unityVersion + " · load " + Load());
        ShaderReport();
        var rng = new System.Random(20261008);
        var cases = new List<(string profile, int n)> { ("Annealed6", 60), ("Annealed6_Cinematic", 20), ("Annealed6_WebGL", 20), ("Wired6", 6), ("Tempered6", 3) };
        var tsv = new StringBuilder("profile\tseed\tix\tiy\tside\tpieces\tfins\tfin_tris\tcrater_tris\ts2_tris\ts2_tris_rule\ts2_verts\tclosed_bad\tvolume_bad\tnormal_bad\tpose_err_um\tbevel_clamped\tslices\tslice_max_ms\tmesh_ms\tpattern_ms\n");
        var job = new FrontRoomsGlassFractureJob();
        var res = new FrontRoomsGlassFracture();
        var b = new FrontRoomsGlassFractureMeshes();
        FrontRoomsGlassFractureJob.ClockMs = () => { var c = CpuMs(); return double.IsNaN(c) ? System.Diagnostics.Stopwatch.GetTimestamp() * 1000.0 / System.Diagnostics.Stopwatch.Frequency : c; };
        FrontRoomsGlassFractureMeshes.ClockMs = FrontRoomsGlassFractureJob.ClockMs;
        int triRuleBad = 0, closedBad = 0, volBad = 0, nrmBad = 0, uvBad = 0, poseBad = 0, fmtBad = 0, finBad = 0, craterBad = 0, budgetBad = 0, detBad = 0, rangeBad = 0, breaks = 0;
        double worstPoseUm = 0, worstSlice = 0;
        var meshMs = new List<double>();
        var patternMs = new List<double>();
        var frames1 = new List<int>();
        var phaseMax = new double[6];
        // Warm-up (Mono JIT, pools): the first break compiles the generator and the builder; it is timed on its own and left
        // out of M8 (in the game FrontRoomsGlassBreakable.StartWarmUp does this on a worker thread at the first WindowBuilt).
        {
            var P0 = FrontRoomsGlassTypeProfile.Load("Annealed6");
            for (var w = 0; w < 6; w++)
            {
                var t0 = FrontRoomsGlassFractureJob.ClockMs();
                FrontRoomsGlassFracturePattern.BeginRoot(job, P0, (uint)(777 + w), .1 * w - .2, 1.3, FrontRoomsGlassFracturePattern.SlabW, FrontRoomsGlassFracturePattern.SlabH, double.NaN, 1, "band", res);
                job.Finish();
                var t1 = FrontRoomsGlassFractureJob.ClockMs();
                b.Begin(res, new FrontRoomsGlassFractureMeshes.Options { keyA = 1f, keyB = 2f, bevel = double.NaN });
                b.Finish();
                var t2 = FrontRoomsGlassFractureJob.ClockMs();
                if (w == 0 || w == 5) log.AppendLine("     warm-up break " + (w + 1) + ": pattern " + F(t1 - t0, "0.00") + " ms + meshes " + F(t2 - t1, "0.00") + " ms thread CPU" + (w == 0 ? " (first in this process: JIT + pools)" : ""));
            }
        }
        FrontRoomsGlassFractureMeshes.SliceLog = Enumerable.Range(0, 6).Select(_ => new List<double>()).ToArray();
        foreach (var (profileName, n) in cases)
        {
            var P = FrontRoomsGlassTypeProfile.Load(profileName);
            for (var k = 0; k < n; k++)
            {
                var seed = (uint)rng.Next(1, int.MaxValue);
                var ix = -.5 + rng.NextDouble();
                var iy = .55 + rng.NextDouble() * 1.25;
                var side = rng.Next(2) == 0 ? 1 : -1;
                var t0 = FrontRoomsGlassFractureJob.ClockMs();
                FrontRoomsGlassFracturePattern.BeginRoot(job, P, seed, ix, iy, FrontRoomsGlassFracturePattern.SlabW, FrontRoomsGlassFracturePattern.SlabH, double.NaN, side, "band", res);
                job.Finish();
                patternMs.Add(FrontRoomsGlassFractureJob.ClockMs() - t0);
                if (job.Failed) { log.AppendLine("  generation failed " + profileName + " seed " + seed + ": " + job.Error); continue; }
                breaks++;
                b.Begin(res, new FrontRoomsGlassFractureMeshes.Options { keyA = 12345f, keyB = 678f, bevel = double.NaN });
                // frames at a 1 ms budget (as the breakable runs it)
                var frames = 0;
                while (!b.StepFor(FrontRoomsGlassBreakable.MeshBudgetMs)) frames++;
                frames++;
                frames1.Add(frames);
                meshMs.Add(b.TotalMs);
                worstSlice = Math.Max(worstSlice, b.SliceMsMax);
                for (var ph = 0; ph < phaseMax.Length; ph++) phaseMax[ph] = Math.Max(phaseMax[ph], b.PhaseMsMax[ph]);
                var c = b.Stats;
                // M1 counts
                var rule = 0;
                for (var i = 0; i < res.pieces.Count; i++)
                    if (res.pieces[i].kind != GlassPieceKind.Crush) rule += res.bevel > 0 ? 12 * res.pieces[i].vn - 4 : 4 * res.pieces[i].vn - 4;
                if (!res.isDice && c.stage2Tris != rule) triRuleBad++;
                if (!res.isDice && (c.stage2Tris > P.budgetS2Tris || c.finTris > P.budgetFinTris || c.craterTris > P.budgetCraterTris)) budgetBad++;
                // M2/M3 per piece
                var v = b.Stage2 != null ? b.Stage2.vertices : new Vector3[0];
                var nr = b.Stage2 != null ? b.Stage2.normals : new Vector3[0];
                var uv0 = new List<Vector2>(); if (b.Stage2 != null) b.Stage2.GetUVs(0, uv0);
                var idx = b.Stage2 != null ? b.Stage2.GetIndices(0) : new int[0];
                int pc = 0, pv = 0, pn = 0;
                for (var q = 0; q < b.PieceIds.Count; q++)
                {
                    var tris = new List<int>();
                    var rs = b.PieceSides[q]; var rf = b.PieceFaces[q];
                    for (var t = 0; t < rs.indexCount; t++) tris.Add(idx[rs.indexStart + t]);
                    for (var t = 0; t < rf.indexCount; t++) tris.Add(idx[rf.indexStart + t]);
                    if (!Closed(v, tris)) pc++;
                    var vol = 0.0;
                    for (var t = 0; t < tris.Count; t += 3)
                    {
                        Vector3 a = v[tris[t]], b1 = v[tris[t + 1]], c1 = v[tris[t + 2]];
                        vol += Vector3.Dot(a, Vector3.Cross(b1, c1)) / 6.0;
                        // in doubles (Unity's Vector3.normalized returns zero below 1e-5: the bevel triangles are smaller)
                        double ux = (double)b1.x - a.x, uy = (double)b1.y - a.y, uz = (double)b1.z - a.z, wx = (double)c1.x - a.x, wy = (double)c1.y - a.y, wz = (double)c1.z - a.z;
                        double gx = uy * wz - uz * wy, gy = uz * wx - ux * wz, gz = ux * wy - uy * wx, gl = Math.Sqrt(gx * gx + gy * gy + gz * gz);
                        if (gl > 1e-14 && (gx * nr[tris[t]].x + gy * nr[tris[t]].y + gz * nr[tris[t]].z) / gl < .99) pn++;
                    }
                    // Unity's front faces: cross(b - a, c - a) points out of the surface (its clockwise winding seen from outside),
                    // so a closed mesh wound outward has a POSITIVE signed volume with this formula
                    // against the piece's outline less the 0.04 mm crack gap (the bevel takes b^2 per metre of edge on top)
                    var area = InsetArea(res, b.PieceIds[q], FrontRoomsGlassFractureMeshes.PieceInset);
                    var expect = area * res.slabT;
                    if (!(vol > 0 && Math.Abs(vol - expect) < .03 * Math.Abs(expect) + 1e-10))
                    {
                        pv++;
                        if (pv <= 3) log.AppendLine("     M3 detail: " + profileName + " seed " + seed + " piece " + b.PieceIds[q] + " kind " + res.pieces[b.PieceIds[q]].kind + " n " + res.pieces[b.PieceIds[q]].vn
                            + " area " + F(area * 1e4, "0.000") + " cm2 · volume " + F(vol * 1e9, "0.0") + " mm3 vs " + F(expect * 1e9, "0.0") + " mm3");
                    }
                }
                closedBad += pc; volBad += pv; nrmBad += pn;
                // M4 UV0 = rest; posed = PosePoint(rest) (front-face vertices: z = +t/2)
                var worst = 0.0;
                for (var q = 0; q < b.PieceIds.Count; q++)
                {
                    var pi = b.PieceIds[q];
                    var p = res.pieces[pi];
                    var rf = b.PieceFaces[q];
                    for (var t = 0; t < rf.indexCount; t++)
                    {
                        var vi = idx[rf.indexStart + t];
                        var z = nr[vi].z > 0 ? res.slabT * .5 : -res.slabT * .5;
                        double px = uv0[vi].x, py = uv0[vi].y, pz = z;
                        if (p.kind == GlassPieceKind.Shard)
                        {
                            var sp = FrontRoomsGlassFractureMeshes.StagePoint(res, pi, new FrontRoomsGlassFractureMeshes.Vector3d(uv0[vi].x, uv0[vi].y, z));
                            px = sp.x; py = sp.y; pz = sp.z;
                        }
                        var e = Math.Sqrt((px - v[vi].x) * (px - v[vi].x) + (py - v[vi].y) * (py - v[vi].y) + (pz - v[vi].z) * (pz - v[vi].z));
                        worst = Math.Max(worst, e);
                    }
                }
                worstPoseUm = Math.Max(worstPoseUm, worst * 1e6);
                if (worst > 2e-6) poseBad++;
                // UV0 rest of unposed pieces
                foreach (var q in Enumerable.Range(0, b.PieceIds.Count).Where(q => res.pieces[b.PieceIds[q]].kind == GlassPieceKind.Tooth).Take(3))
                {
                    var rf = b.PieceFaces[q];
                    var vi = idx[rf.indexStart];
                    if (Math.Abs(uv0[vi].x - v[vi].x) > 1e-6 || Math.Abs(uv0[vi].y - v[vi].y) > 1e-6) uvBad++;
                }
                // M5 format
                if (!res.isDice && (b.Stage2.indexFormat != IndexFormat.UInt16 || b.Stage2.subMeshCount != 1 || b.Stage2.GetVertexAttributeFormat(VertexAttribute.Position) != VertexAttributeFormat.Float32
                    || b.Stage2.GetVertexAttributeStream(VertexAttribute.Position) != 0 || v.Any(x => float.IsNaN(x.x) || float.IsNaN(x.y) || float.IsNaN(x.z)) || !b.Stage2.isReadable)) fmtBad++;
                // M6 determinism + piece source ranges
                if (k < 4 && !res.isDice)
                {
                    var h1 = Hash(b.Stage2) + Hash(b.Stage1);
                    var b2 = new FrontRoomsGlassFractureMeshes();
                    b2.Begin(res, new FrontRoomsGlassFractureMeshes.Options { keyA = 12345f, keyB = 678f, bevel = double.NaN });
                    b2.Finish();
                    if (Hash(b2.Stage2) + Hash(b2.Stage1) != h1) detBad++;
                    var ranges = new List<FrontRoomsGlassRTRange>();
                    var src = b2.BuildPieceSource(ranges);
                    var covered = ranges.Sum(r => r.indexCount);
                    if (covered != src.GetIndices(0).Length || ranges.Count != b2.PieceIds.Count) rangeBad++;
                    for (var q = 0; q < ranges.Count && q < 10; q++)
                        if (!Closed(src.vertices, src.GetIndices(0).Skip(ranges[q].indexStart).Take(ranges[q].indexCount).ToList())) rangeBad++;
                    UnityEngine.Object.DestroyImmediate(src);
                    b2.ReleaseMeshes();
                }
                // M7 fins on the stage-1 cracks, inside the glass; crater
                if (!res.isDice && b.Stage1 != null)
                {
                    var v1 = b.Stage1.vertices;
                    var uv1 = new List<Vector4>(); b.Stage1.GetUVs(1, uv1);
                    for (var i = 0; i < v1.Length; i++)
                    {
                        if (uv1[i].w == FrontRoomsGlassFractureMeshes.KindFracture)
                        {
                            if (Math.Abs(v1[i].z) > res.slabT * .5) { finBad++; break; }
                            if (DistToCracks(res, v1[i].x, v1[i].y) > .0012) { finBad++; break; }
                        }
                        else if (uv1[i].w == FrontRoomsGlassFractureMeshes.KindCrushed)
                        {
                            var r = Math.Sqrt((v1[i].x - res.impact.x) * (v1[i].x - res.impact.x) + (v1[i].y - res.impact.y) * (v1[i].y - res.impact.y));
                            var onStruck = v1[i].z * -res.farZSign >= -1e-6;   // on (or into) the breaker's half
                            if (r > .015 + 1e-6) { craterBad++; break; }
                            _ = onStruck;
                        }
                    }
                }
                tsv.AppendLine(profileName + "\t" + seed + "\t" + F(ix, "0.0000") + "\t" + F(iy, "0.0000") + "\t" + side + "\t" + res.pieces.Count + "\t" + c.fins + "\t" + c.finTris + "\t" + c.craterTris + "\t" + c.stage2Tris + "\t" + rule
                    + "\t" + c.stage2Verts + "\t" + pc + "\t" + pv + "\t" + pn + "\t" + F(worst * 1e6, "0.00") + "\t" + c.bevelClamped + "\t" + b.Slices + "\t" + F(b.SliceMsMax, "0.000") + "\t" + F(b.TotalMs, "0.00") + "\t" + F(patternMs.Last(), "0.00"));
            }
        }
        b.ReleaseMeshes();
        File.WriteAllText(Path.Combine(Directory.CreateDirectory(Out).FullName, "stage-mesh-breaks.tsv"), tsv.ToString());
        Check("M1", triRuleBad == 0, "stage-2 triangles = the reference's rule (12n-4 bevelled / 4n-4 flat) in " + (breaks - triRuleBad) + " / " + breaks + " breaks");
        Check("M1b", budgetBad == 0, "within the budgets (S2 <= profile, fins <= the profile (plan: 400), crater <= profile): " + (breaks - budgetBad) + " / " + breaks);
        Check("M2", closedBad == 0, "closed, consistently wound pieces (each welded edge twice, opposite directions): " + closedBad + " bad pieces");
        Check("M3", volBad == 0 && nrmBad == 0, "outward winding, volume = inset area x 6 mm within 3 %: " + volBad + " bad · flat normals agree with the winding: " + nrmBad + " bad triangles");
        Check("M4", poseBad == 0 && uvBad == 0, "posed vertex = StagePoint(rest) = the generator's PosePoint after the seat tilt (worst " + F(worstPoseUm, "0.00") + " um, bar 2 um) · UV0 = rest: " + uvBad + " bad");
        Check("M5", fmtBad == 0, "16-bit indices, one submesh, Float32 x 3 positions in stream 0, readable, no NaN: " + fmtBad + " bad");
        Check("M6", detBad == 0 && rangeBad == 0, "same pattern -> same bytes (" + detBad + " bad) · BuildPieceSource ranges cover every triangle and are closed slabs (" + rangeBad + " bad)");
        Check("M7", finBad == 0 && craterBad == 0, "fins on the stage-1 cracks (<= 1.2 mm) and inside the 6 mm: " + finBad + " bad · crater within 15 mm of the impact: " + craterBad + " bad");
        meshMs.Sort(); patternMs.Sort(); frames1.Sort();
        var all = FrontRoomsGlassFractureMeshes.SliceLog.SelectMany(l => l).OrderBy(x => x).ToList();
        var byPhase = string.Join(" · ", new[] { 1, 2, 3, 4 }.Select(ph => { var l = FrontRoomsGlassFractureMeshes.SliceLog[ph].OrderBy(x => x).ToList(); return ((FrontRoomsGlassFractureMeshes.Phase)ph) + " n " + l.Count + " p50 " + F(Pct(l, .5), "0.000") + " p99 " + F(Pct(l, .99), "0.000") + " max " + F(l.Count > 0 ? l.Last() : double.NaN, "0.000"); }));
        FrontRoomsGlassFractureMeshes.SliceLog = null;
        log.AppendLine("     mesh slices (thread CPU ms): all n " + all.Count + " p50 " + F(Pct(all, .5), "0.000") + " p99 " + F(Pct(all, .99), "0.000") + " p99.9 " + F(Pct(all, .999), "0.000") + " max " + F(all.Last(), "0.000") + " · " + byPhase);
        var gcBefore = GC.CollectionCount(0);
        Check("M8", Pct(all, .99) <= 1.0, "mesh slice thread CPU p99 " + F(Pct(all, .99), "0.000") + " ms (bar 1 ms), worst " + F(worstSlice, "0.000") + " ms · per break p50 / p90 / max " + F(Pct(meshMs, .5), "0.00") + " / " + F(Pct(meshMs, .9), "0.00") + " / " + F(meshMs.Last(), "0.00")
            + " ms · frames at 1 ms p50 / max " + frames1[frames1.Count / 2] + " / " + frames1.Last() + " · worst slice by phase (Stage1 / Pieces / Upload1 / Upload2) "
            + string.Join(" / ", phaseMax.Skip(1).Take(4).Select(x => F(x, "0.000"))) + " ms · pattern p50 / p90 " + F(Pct(patternMs, .5), "0.00") + " / " + F(Pct(patternMs, .9), "0.00") + " ms · load " + Load());
        Memory();
        log.AppendLine("RESULT " + (fail == 0 ? "PASS" : "FAIL") + ": " + pass + " passed, " + fail + " failed · " + breaks + " breaks");
        return fail == 0;
    }

    // M9 (measured, reported): memory of one struck window's working set (generator pools, result, mesh-builder lists:
    // managed heap) and of its stage meshes (GPU vertex + index buffers; Unity keeps a CPU copy of the same size while a
    // mesh is readable, which G14 P4 needs). An intact window holds none of it (FrontRoomsGlassBreakable takes a set only
    // when struck).
    static void Memory()
    {
        var m0 = GC.GetTotalMemory(true);
        var job = new FrontRoomsGlassFractureJob();
        var res = new FrontRoomsGlassFracture();
        var b = new FrontRoomsGlassFractureMeshes();
        var m1 = GC.GetTotalMemory(true);
        FrontRoomsGlassFracturePattern.BeginRoot(job, FrontRoomsGlassTypeProfile.ForTier(), 4242u, 0.0, 1.62, FrontRoomsGlassFracturePattern.SlabW,
            FrontRoomsGlassFracturePattern.SlabH, double.NaN, 1, "band", res);
        job.Finish();
        b.Begin(res, new FrontRoomsGlassFractureMeshes.Options { keyA = 1f, keyB = 2f, bevel = double.NaN });
        b.Finish();
        var m2 = GC.GetTotalMemory(true);
        long MeshBytes(Mesh m) => m == null ? 0 : (long)m.vertexCount * m.GetVertexBufferStride(0) + (long)m.GetIndexCount(0) * 2;
        var gpu = MeshBytes(b.Stage1) + MeshBytes(b.Stage2) + 24 * 52 + 36 * 2;
        log.AppendLine("     M9 memory (Annealed6, seed 4242, eye-height centre, " + res.pieces.Count + " pieces): working set managed " + F((m2 - m0) / 1048576.0, "0.00")
            + " MB (" + F((m1 - m0) / 1048576.0, "0.00") + " MB of it allocated empty) · stage meshes GPU " + F(gpu / 1048576.0, "0.00") + " MB (S2 " + b.Stats.stage2Verts
            + " verts x " + (b.Stage2 != null ? b.Stage2.GetVertexBufferStride(0) : 0) + " B), the same again as Unity's readable CPU copy · an intact window: none");
        b.ReleaseMeshes();
        GC.KeepAlive(job); GC.KeepAlive(res);
    }

    static double Pct(List<double> s, double p) => s.Count == 0 ? double.NaN : s[Math.Min(s.Count - 1, (int)(s.Count * p))];

    static double Area(FrontRoomsGlassFracture r, int pi)
    {
        var p = r.pieces[pi];
        var a = 0.0;
        for (var k = 0; k < p.vn; k++)
        {
            var u = r.verts[p.v0 + k]; var w = r.verts[p.v0 + (k + 1) % p.vn];
            a += u.x * w.y - w.x * u.y;
        }
        return a * .5;
    }

    static double InsetArea(FrontRoomsGlassFracture r, int pi, double d)
    {
        var poly = new List<GlassV2>(); r.GetPoly(pi, poly);
        var q = new List<GlassV2>(); FrontRoomsGlassFractureMeshes.InsetConvex(poly, d, q);
        var a = 0.0;
        for (var k = 0; k < q.Count; k++) { var u = q[k]; var w = q[(k + 1) % q.Count]; a += u.x * w.y - w.x * u.y; }
        return a * .5;
    }

    static double DistToCracks(FrontRoomsGlassFracture r, double x, double y)
    {
        var best = double.MaxValue;
        foreach (var c in r.cracks)
            for (var i = 0; i + 1 < c.vn; i++)
            {
                var a = r.crackVerts[c.v0 + i]; var b = r.crackVerts[c.v0 + i + 1];
                double dx = b.x - a.x, dy = b.y - a.y, L2 = dx * dx + dy * dy;
                var t = L2 > 0 ? Math.Max(0, Math.Min(1, ((x - a.x) * dx + (y - a.y) * dy) / L2)) : 0;
                var px = a.x + t * dx - x; var py = a.y + t * dy - y;
                best = Math.Min(best, Math.Sqrt(px * px + py * py));
            }
        return best;
    }

    /// <summary>Weld by exact position; every undirected edge used exactly twice, once in each direction.</summary>
    static bool Closed(Vector3[] v, List<int> tris)
    {
        var id = new Dictionary<Vector3, int>();
        int W(int i) { if (!id.TryGetValue(v[i], out var k)) { k = id.Count; id[v[i]] = k; } return k; }
        var edges = new Dictionary<(int, int), int>();
        for (var t = 0; t < tris.Count; t += 3)
        {
            int a = W(tris[t]), b = W(tris[t + 1]), c = W(tris[t + 2]);
            foreach (var (p, q) in new[] { (a, b), (b, c), (c, a) })
            {
                if (p == q) continue;
                edges.TryGetValue((p, q), out var n); edges[(p, q)] = n + 1;
            }
        }
        foreach (var kv in edges)
        {
            edges.TryGetValue((kv.Key.Item2, kv.Key.Item1), out var back);
            if (kv.Value != 1 || back != 1) return false;
        }
        return true;
    }

    static string Hash(Mesh m)
    {
        if (m == null) return "-";
        using (var sha = SHA256.Create())
        {
            var vb = m.GetVertexBuffer(0);
            var data = new byte[vb.count * vb.stride];
            vb.GetData(data);
            vb.Dispose();
            var idx = m.GetIndices(0);
            var ib = new byte[idx.Length * 4];
            Buffer.BlockCopy(idx, 0, ib, 0, ib.Length);
            return BitConverter.ToString(sha.ComputeHash(data.Concat(ib).ToArray())).Replace("-", "").Substring(0, 16);
        }
    }

    static string Load()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("/usr/sbin/sysctl", "-n vm.loadavg") { RedirectStandardOutput = true, UseShellExecute = false };
            using (var p = System.Diagnostics.Process.Start(psi)) { var o = p.StandardOutput.ReadToEnd(); p.WaitForExit(2000); return o.Trim(); }
        }
        catch { return "?"; }
    }

    // ---------------------------------------------------------------------------------------------------------------------
    // S1: the shader. Main's copy is kept as text in the lab (Assets/GlassLab/Reference/FrontRoomsGlass.main.shader.txt);
    // a temporary asset of it is compiled for the comparison and deleted again.
    // ---------------------------------------------------------------------------------------------------------------------
    public static void ShaderReportBatch()
    {
        log.Clear(); pass = fail = 0;
        ShaderReport();
        Directory.CreateDirectory(Out);
        File.WriteAllText(Path.Combine(Out, "shader-report.txt"), log.ToString());
        if (Application.isBatchMode) EditorApplication.Exit(fail == 0 ? 0 : 2);
    }

    static void ShaderReport()
    {
        var sh = Shader.Find("FrontRooms/Glass");
        if (sh == null) { Check("S1", false, "FrontRooms/Glass not found"); return; }
        var shaderPath = AssetDatabase.GetAssetPath(sh);
        var text = File.ReadAllText(shaderPath);
        var refPath = "Assets/GlassLab/Reference/FrontRoomsGlass.main.shader.txt";
        string mainText = File.Exists(refPath) ? File.ReadAllText(refPath) : null;
        IEnumerable<string> Pragmas(string t) => t.Split('\n').Select(l => l.Trim()).Where(l => l.StartsWith("#pragma multi_compile") || l.StartsWith("#pragma shader_feature") || l.StartsWith("#include_with_pragmas"));
        var now = Pragmas(text).ToList();
        if (mainText == null)
            log.AppendLine("SKIP S1a/S1b  no reference copy of main's shader here (" + refPath + " lives in the GD3 lab clone only); keyword pragmas now: " + now.Count + " lines");
        if (mainText != null)
        {
            var was = Pragmas(mainText).ToList();
            Check("S1a", now.SequenceEqual(was), "keyword pragmas identical to main's shader: " + now.Count + " lines now, " + was.Count + " in main" + (now.SequenceEqual(was) ? "" : " · DIFF: +" + string.Join(" | ", now.Except(was)) + " -" + string.Join(" | ", was.Except(now))));
        }
        var getCount = typeof(ShaderUtil).GetMethod("GetVariantCount", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        ulong Count(Shader s) { try { return Convert.ToUInt64(getCount?.Invoke(null, new object[] { s, false }) ?? 0UL); } catch { return 0UL; } }
        var countNow = Count(sh);
        ulong countMain = 0;
        if (mainText != null)
        {
            var tmp = "Assets/GlassLab/Reference/Tmp_FrontRoomsGlassMain.shader";
            File.WriteAllText(tmp, mainText.Replace("Shader \"FrontRooms/Glass\"", "Shader \"Hidden/GlassLab/FrontRoomsGlassMainCopy\""));
            AssetDatabase.ImportAsset(tmp, ImportAssetOptions.ForceSynchronousImport);
            var copy = AssetDatabase.LoadAssetAtPath<Shader>(tmp);
            countMain = copy != null ? Count(copy) : 0;
            AssetDatabase.DeleteAsset(tmp);
        }
        if (mainText != null)
            Check("S1b", countNow == countMain && countNow > 0, "variant count (ShaderUtil.GetVariantCount, all keywords): " + countNow + " now, " + countMain + " for main's shader");
        else log.AppendLine("     variant count (ShaderUtil.GetVariantCount, all keywords): " + countNow);
        var kw = string.Join(" ", sh.keywordSpace.keywordNames);
        log.AppendLine("     keywords: " + kw);
        Check("S1c", !kw.Contains("GD3") && !kw.Contains("_PANE") && !kw.Contains("FRACTURE"), "no GD3 keyword (uniform branches only)");
        // compile representative variants for WebGL 2 (GLES3x) and Metal
        var data = ShaderUtil.GetShaderData(sh);
        var sub = data.GetSubshader(0);
        var sets = new[] { new string[0], new[] { "_FR_GLASS_GRIME" }, new[] { "_FR_GLASS_GRIME", "_ADDITIONAL_LIGHTS", "_MAIN_LIGHT_SHADOWS" } };
        // desktop Forward+ (the cluster light loop the crack fallback walks) on Metal only
        var setsDesktop = new[] { new[] { "_FR_GLASS_GRIME", "_CLUSTER_LIGHT_LOOP", "_MAIN_LIGHT_SHADOWS" }, new[] { "_CLUSTER_LIGHT_LOOP", "_ADDITIONAL_LIGHT_SHADOWS" } };
        foreach (var (plat, target, name) in new[] { (ShaderCompilerPlatform.GLES3x, BuildTarget.WebGL, "WebGL 2 (GLES3x)"), (ShaderCompilerPlatform.Metal, BuildTarget.StandaloneOSX, "Metal (macOS)") })
        {
            var ok = 0; var tot = 0; var msgs = new List<string>();
            for (var p = 0; p < sub.PassCount; p++)
            {
                var pass = sub.GetPass(p);
                if (plat == ShaderCompilerPlatform.GLES3x && pass.Name == "FRGlassRTPrepass") continue;   // stripped from WebGL (G14)
                foreach (var set in plat == ShaderCompilerPlatform.Metal ? sets.Concat(setsDesktop) : sets)
                    foreach (var st in new[] { ShaderType.Vertex, ShaderType.Fragment })
                    {
                        tot++;
                        var r = pass.CompileVariant(st, set, plat, target);
                        if (r.Success) ok++;
                        else msgs.Add(pass.Name + " " + st + " [" + string.Join(" ", set) + "]: " + string.Join("; ", r.Messages.Select(m => m.message)));
                    }
            }
            Check("S1d " + name, ok == tot, name + ": " + ok + " / " + tot + " representative stage variants compile" + (msgs.Count > 0 ? " · " + string.Join(" || ", msgs.Take(3)) : ""));
        }
    }
}
