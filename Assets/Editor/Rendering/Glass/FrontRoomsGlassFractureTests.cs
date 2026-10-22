// FrontRoomsGlassFractureTests.cs -- GD3 step 3 (plan 10 sec 5.2 step 3): the edit-mode suite of the C# crack-graph
// generator. The project's suite convention (FrontRoomsGlassShotTests): a menu item plus a headless RunBatch that writes
// a JSON report under Verification/ and throws on FAIL.
//
//   Unity -batchmode -projectPath <clone> -buildTarget OSXUniversal -executeMethod FrontRoomsGlassFractureTests.RunBatch
//         -quit -logFile <log>  [-glassTestN 120] [-glassTestPerf 200] [-glassTestXcheck 1015] [-glassTestOut <dir>]
//         [-glassTestOnly env,golden,sweep,determinism,negatives,band,contract,profiles,xcheck,perf]
//
// Checks (each mirrors Tools/Blender/glass/test_crack_graph_ref.py T1-T7, plus the port's own):
//   T0 env      runtime facts: Unity, code optimisation, math fingerprint (libm), load average.
//   T3 golden   the 20 golden cases (Tools/Blender/glass/golden): bytes and sha256 equal to the Python reference;
//               every number within 0.1 mm (1e-4) of it at the least (the plan's bar).
//   T1 sweep    the reference's own T1 sweep inputs (same Mersenne Twister draws, 215 breaks over 5 profiles):
//               every validate() check passes; the per-profile counts.
//   T2 determ.  same inputs -> same bytes (same job, a fresh job, the static API); a different seed differs.
//   T4 negative the validator catches the planted defects (hole, non-convex, crossing, sliver, tooth in the clear
//               zone, '+' junction).
//   T5 fixes    the four prototype fixes over the sweep.
//   T6 band     band_mode "stop" leaves no visible tooth.
//   T7 contract impactUV == impact in metres; side only sets far_z_sign; the 0.2 m clamp; the map's rotation; float
//               record inputs (GlassBreakRecord holds floats).
//   T8 profiles the profile assets (if present) equal the built-ins (= crack_graph_ref.py PROFILES).
//   T9 xcheck   1,015 breaks vs Tools/Blender/glass/xcheck (sha256 of the JSON + verdict per break; -glassTestXcheck N).
//   P  perf     time per slice (thread CPU and wall) and allocation per slice after warm-up; GC collections.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class FrontRoomsGlassFractureTests
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    [Serializable]
    sealed class Report
    {
        public string verdict;
        public int passed, failed;
        public string started, finished;
        public double load1Start, load1End;
        public List<string> checks = new List<string>();
        public List<string> notes = new List<string>();
    }

    static Report report;
    static string outDir;

    static void Check(bool ok, string what)
    {
        if (ok) report.passed++; else report.failed++;
        report.checks.Add((ok ? "ok   " : "FAIL ") + what);
    }

    static void Note(string s) => report.notes.Add(s);

    [MenuItem("FrontRooms/Glass/Test Fracture Generator")]
    public static void Run() => Execute(false);

    public static void RunBatch() => Execute(true);

    static string ArgValue(string name, string fallback)
    {
        var a = Environment.GetCommandLineArgs();
        for (var i = 0; i < a.Length - 1; i++)
            if (a[i] == name)
                return a[i + 1];
        return fallback;
    }

    static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

    static void Execute(bool throwOnFail)
    {
        report = new Report { started = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", Inv), load1Start = Load1() };
        outDir = ArgValue("-glassTestOut", Path.Combine(ProjectRoot, "Verification", "glass_fracture"));
        Directory.CreateDirectory(outDir);
        var n = int.Parse(ArgValue("-glassTestN", "120"), Inv);
        var perfN = int.Parse(ArgValue("-glassTestPerf", "200"), Inv);
        var only = ArgValue("-glassTestOnly", "");
        Func<string, bool> want = k => only == "" || ("," + only + ",").Contains("," + k + ",");
        try
        {
            if (want("env")) Env();
            if (want("golden")) Golden();
            if (want("sweep")) Sweep(n);
            if (want("determinism")) Determinism();
            if (want("negatives")) Negatives();
            if (want("band")) BandStop();
            if (want("contract")) Contract();
            if (want("profiles")) Profiles();
            if (want("xcheck")) CrossCheck(int.Parse(ArgValue("-glassTestXcheck", "1015"), Inv));
            if (want("perf")) PerfRun(perfN);
        }
        catch (Exception e)
        {
            Check(false, "exception: " + e.GetType().Name + " " + e.Message + "\n" + e.StackTrace);
        }
        report.finished = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", Inv);
        report.load1End = Load1();
        var pass = report.failed == 0 && report.passed > 0;
        report.verdict = (pass ? "PASS" : "FAIL") + ": " + report.passed + " passed, " + report.failed + " failed";
        File.WriteAllText(Path.Combine(outDir, "glass-fracture-tests.json"), JsonUtility.ToJson(report, true));
        var text = "[GlassFractureTests] " + report.verdict + "\n" + string.Join("\n", report.checks.ToArray()) + "\n-- notes --\n" + string.Join("\n", report.notes.ToArray());
        File.WriteAllText(Path.Combine(outDir, "glass-fracture-tests.txt"), text);
        if (pass) Debug.Log(text); else Debug.LogError(text);
        if (throwOnFail && !pass)
            throw new Exception(report.verdict);
    }

    // ---------------------------------------------------------------------------------------------------------
    // machine facts
    // ---------------------------------------------------------------------------------------------------------
    [StructLayout(LayoutKind.Sequential)]
    struct Timespec
    {
        public long sec, nsec;
    }

#if UNITY_EDITOR_OSX
    [DllImport("/usr/lib/libSystem.B.dylib")] static extern int clock_gettime(int clk, out Timespec ts);
    [DllImport("/usr/lib/libSystem.B.dylib")] static extern int getloadavg(double[] l, int n);
#endif
    static bool cpuClockOk = true;
    static readonly Func<double> DefaultClock = FrontRoomsGlassFractureJob.ClockMs;

    /// <summary>This thread's CPU time in ms (macOS CLOCK_THREAD_CPUTIME_ID); NaN where unavailable.</summary>
    static double ThreadCpuMs()
    {
#if UNITY_EDITOR_OSX
        if (cpuClockOk)
        {
            try
            {
                Timespec t;
                if (clock_gettime(16, out t) == 0)
                    return t.sec * 1000.0 + t.nsec / 1e6;
            }
            catch (Exception)
            {
                cpuClockOk = false;
            }
        }
#endif
        return double.NaN;
    }

    static double Load1()
    {
#if UNITY_EDITOR_OSX
        try
        {
            var l = new double[3];
            if (getloadavg(l, 3) > 0)
                return Math.Round(l[0], 1);
        }
        catch (Exception)
        {
        }
#endif
        return double.NaN;
    }

    static string LoadTag(double load) => double.IsNaN(load) ? "load ?" : load > 32 ? "load " + load.ToString("0", Inv) + " (measured under load, re-measure)" : "load " + load.ToString("0.0", Inv);

    static string Sha(string s)
    {
        using (var h = SHA256.Create())
            return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(s))).Replace("-", "").ToLowerInvariant();
    }

    // The math fingerprint: sha256 over the bit patterns of sin, cos, log, exp, atan2 and the port's hypot on 4,000
    // fixed inputs. MathPrintRef is the value on Apple Silicon: standalone Mono 6000.3.10f1 == Python 3.11.9 arm64 (Blender),
    // build/03_generator.md sec 4.1. A different value means a different libm; the goldens may then differ by 1e-7.
    const string MathPrintRef = "b7cf3f2f12c25bc0a4fd12b9c8b9243f4d1077be9bfa24fe632df1671abf9d85";

    public static string MathFingerprint()
    {
        var sb = new StringBuilder();
        var r = GlassRng.Make(19900101);
        for (var i = 0; i < 4000; i++)
        {
            var x = (r.U01() * 2.0 - 1.0) * 20.0;
            var y = r.U01() * 2.0 - 1.0;
            sb.Append(BitConverter.DoubleToInt64Bits(GlassMath.Sin(x)).ToString("x16"));
            sb.Append(BitConverter.DoubleToInt64Bits(GlassMath.Cos(x)).ToString("x16"));
            sb.Append(BitConverter.DoubleToInt64Bits(GlassMath.Log(Math.Abs(x) + 1e-3)).ToString("x16"));
            sb.Append(BitConverter.DoubleToInt64Bits(GlassMath.Exp(x / 10.0)).ToString("x16"));
            sb.Append(BitConverter.DoubleToInt64Bits(GlassMath.Atan2(y, x)).ToString("x16"));
            sb.Append(BitConverter.DoubleToInt64Bits(GlassMath.Hypot(x, y)).ToString("x16"));
            sb.Append(BitConverter.DoubleToInt64Bits(Math.Pow(x, 2.0)).ToString("x16"));
        }
        return Sha(sb.ToString());
    }

    static void Env()
    {
        Note("Unity " + Application.unityVersion + ", " + SystemInfo.processorType + " x" + SystemInfo.processorCount + ", " + SystemInfo.operatingSystem);
        Note("code optimization: " + UnityEditor.Compilation.CompilationPipeline.codeOptimization + "; scripting runtime " + Environment.Version);
        var fp = MathFingerprint();
        Note("math fingerprint " + fp + (MathPrintRef == "SEE_REPORT" ? "" : fp == MathPrintRef ? " (= arm64 reference)" : " (DIFFERS from the arm64 reference)"));
        Note("load1 at start " + report.load1Start.ToString("0.0", Inv));
        var alloc0 = GC.GetAllocatedBytesForCurrentThread();
        var probe = new byte[1 << 20];
        var alloc = GC.GetAllocatedBytesForCurrentThread() - alloc0;
        allocCounterWorks = alloc >= (1 << 20) && probe.Length > 0;
        Note("allocation counter probe: 1 MiB array -> " + alloc + " B counted (" + (allocCounterWorks ? "works" : "NOT usable") + ")");
        Check(true, "T0 env recorded (" + LoadTag(report.load1Start) + ")");
    }

    static bool allocCounterWorks;

    // ---------------------------------------------------------------------------------------------------------
    // T3 golden
    // ---------------------------------------------------------------------------------------------------------
    static void Golden()
    {
        var dir = Path.Combine(ProjectRoot, "Tools", "Blender", "glass", "golden");
        if (!Directory.Exists(dir))
        {
            Check(false, "T3 golden: no folder " + dir);
            return;
        }
        var index = File.ReadAllText(Path.Combine(dir, "index.json"));
        var idxTree = MiniJson.Parse(index) as Dictionary<string, object>;
        var idxCases = idxTree != null ? idxTree["cases"] as List<object> : null;
        var job = new FrontRoomsGlassFractureJob();
        var res = new FrontRoomsGlassFracture();
        var bytesEqual = 0;
        var shaEqual = 0;
        var worstAll = 0.0;
        var structOk = 0;
        var tsv = new StringBuilder("case\tfile\tprofile\tpieces\tattempts\tsteps\tvalid\tbytes_equal\tsha_equal\tmax_abs_diff_m\tstructure\n");
        foreach (var c in FrontRoomsGlassFractureGoldenCases.All())
        {
            var name = FrontRoomsGlassFractureGoldenCases.FileName(c);
            var path = Path.Combine(dir, name);
            if (!File.Exists(path))
            {
                Check(false, "T3 golden " + name + ": file missing");
                continue;
            }
            FrontRoomsGlassFractureGoldenCases.Run(c, job, res);
            if (job.Failed)
            {
                Check(false, "T3 golden " + name + ": generation failed " + job.Error);
                continue;
            }
            var json = FrontRoomsGlassFracturePattern.ToJson(res);
            var gold = File.ReadAllText(path);
            var be = json == gold;
            var sha = Sha(json);
            string idxSha = null;
            if (idxCases != null)
                foreach (var o in idxCases)
                {
                    var d = o as Dictionary<string, object>;
                    if (d != null && (string)d["file"] == name)
                        idxSha = (string)d["sha256"];
                }
            var se = sha == idxSha;
            // tolerant comparison (the plan's bar: every vertex within 0.1 mm)
            var worst = 0.0;
            string why = null;
            var same = CompareTrees(MiniJson.Parse(gold), MiniJson.Parse(json), "", ref worst, ref why);
            worstAll = Math.Max(worstAll, worst);
            if (be) bytesEqual++;
            if (se) shaEqual++;
            if (same && worst <= 1e-4) structOk++;
            Check(same && worst <= 1e-4 && job.Validation.AllOk,
                "T3 golden " + name + " (" + c.what + "): " + (be ? "bytes equal" : "bytes differ") + ", sha256 " + (se ? "= index" : "!= index") +
                ", max |diff| " + (worst * 1000.0).ToString("0.000000", Inv) + " mm" + (same ? "" : ", STRUCTURE: " + why) + ", " + res.pieces.Count + " pieces, valid " + job.Validation.AllOk);
            tsv.Append(c.index).Append('\t').Append(name).Append('\t').Append(c.profile).Append('\t').Append(res.pieces.Count).Append('\t').Append(res.attempts)
                .Append('\t').Append(job.StepsTaken).Append('\t').Append(job.Validation.AllOk).Append('\t').Append(be).Append('\t').Append(se)
                .Append('\t').Append(worst.ToString("R", Inv)).Append('\t').Append(same ? "same" : why).Append('\n');
        }
        File.WriteAllText(Path.Combine(outDir, "golden.tsv"), tsv.ToString());
        Check(bytesEqual == 20 && shaEqual == 20, "T3 golden: " + bytesEqual + " / 20 byte-identical, " + shaEqual + " / 20 sha256 = index.json, " + structOk +
            " / 20 within 0.1 mm (worst " + (worstAll * 1000.0).ToString("0.000000", Inv) + " mm)");
    }

    // ---------------------------------------------------------------------------------------------------------
    // T9 cross-check: 1,015 breaks against the Python reference (Tools/Blender/glass/xcheck, made by make_xcheck.py):
    // the T1 sweep inputs plus 800 wider random breaks (clamped impacts, both band modes, explicit rotations), each
    // compared by the sha256 of the full JSON and the validate() verdict.
    // ---------------------------------------------------------------------------------------------------------
    static double HexDouble(string h) => BitConverter.Int64BitsToDouble((long)Convert.ToUInt64(h, 16));

    static void CrossCheck(int limit)
    {
        var dir = Path.Combine(ProjectRoot, "Tools", "Blender", "glass", "xcheck");
        var casesPath = Path.Combine(dir, "cases.txt");
        var shaPath = Path.Combine(dir, "py_sha.txt");
        if (!File.Exists(casesPath) || !File.Exists(shaPath))
        {
            Check(false, "T9 cross-check: missing " + casesPath + " or " + shaPath);
            return;
        }
        var cases = File.ReadAllLines(casesPath);
        var py = File.ReadAllLines(shaPath);
        var job = new FrontRoomsGlassFractureJob();
        var res = new FrontRoomsGlassFracture();
        int run = 0, same = 0, verdictSame = 0, invalid = 0, invalidButBudgetOk = 0;
        var mismatches = new List<string>();
        var invalidList = new List<string>();
        var tsv = new StringBuilder("i\tprofile\tseed\tx\ty\tside\tband\trotation\tset\tsha_cs\tsha_py\tvalid_cs\tvalid_py\tok_but_budget\tpieces\ttris_teeth\n");
        for (var i = 0; i < cases.Length && run < limit; i++)
        {
            var p = cases[i].Split(' ');
            if (p.Length < 8)
                continue;
            var q = i < py.Length ? py[i].Split(' ') : new string[0];
            run++;
            var x = HexDouble(p[2]);
            var y = HexDouble(p[3]);
            var rot = p[6] == "nan" ? double.NaN : HexDouble(p[6]);
            string sha, verdict;
            FrontRoomsGlassFracturePattern.BeginRoot(job, FrontRoomsGlassProfileData.BuiltinShared(p[0]), uint.Parse(p[1], Inv), x, y,
                FrontRoomsGlassFracturePattern.SlabW, FrontRoomsGlassFracturePattern.SlabH, rot, int.Parse(p[4], Inv), p[5], res);
            while (!job.Step())
            {
            }
            if (job.Failed)
            {
                sha = "FAILED";
                verdict = "FAILED";
            }
            else
            {
                sha = Sha(FrontRoomsGlassFracturePattern.ToJson(res));
                verdict = job.Validation.AllOk ? "valid" : "INVALID";
            }
            var pySha = q.Length > 1 ? q[1] : "?";
            var pyVerdict = q.Length > 2 ? q[2] : "?";
            if (sha == pySha) same++;
            else if (mismatches.Count < 10) mismatches.Add(i + " " + p[0] + " seed " + p[1]);
            if (verdict == pyVerdict) verdictSame++;
            if (verdict == "INVALID")
            {
                invalid++;
                if (job.Validation.AllOkButBudget) invalidButBudgetOk++;
                invalidList.Add(i + " " + p[0] + " s" + p[1] + " root (" + x.ToString("0.000", Inv) + ", " + y.ToString("0.000", Inv) + ") teeth tris " +
                                res.stats.trisTeeth + " / " + FrontRoomsGlassProfileData.BuiltinShared(p[0]).budgetTeethTris + " (" + job.Validation.Summary() + ")");
            }
            tsv.Append(i).Append('\t').Append(p[0]).Append('\t').Append(p[1]).Append('\t').Append(x.ToString("R", Inv)).Append('\t').Append(y.ToString("R", Inv))
                .Append('\t').Append(p[4]).Append('\t').Append(p[5]).Append('\t').Append(double.IsNaN(rot) ? "nan" : rot.ToString("R", Inv)).Append('\t').Append(p[7])
                .Append('\t').Append(sha).Append('\t').Append(pySha).Append('\t').Append(verdict).Append('\t').Append(pyVerdict)
                .Append('\t').Append(!job.Failed && job.Validation.AllOkButBudget).Append('\t').Append(job.Failed ? 0 : res.pieces.Count)
                .Append('\t').Append(job.Failed ? 0 : res.stats.trisTeeth).Append('\n');
        }
        File.WriteAllText(Path.Combine(outDir, "xcheck.tsv"), tsv.ToString());
        if (invalidList.Count > 0)
            Note("T9 invalid in both C# and Python (budget only = " + invalidButBudgetOk + " of " + invalid + "):\n  " + string.Join("\n  ", invalidList.ToArray()));
        Check(run > 0 && same == run && verdictSame == run && invalidButBudgetOk == invalid,
            "T9 cross-check vs Python: " + same + " / " + run + " sha256 equal, " + verdictSame + " / " + run + " same verdict; " + invalid +
            " invalid in both, " + invalidButBudgetOk + " of them on the budget alone" + (mismatches.Count > 0 ? "; first mismatches: " + string.Join(", ", mismatches.ToArray()) : ""));
    }

    /// <summary>Same structure, same strings and ints; numbers within tolerance (worst |diff| returned).</summary>
    static bool CompareTrees(object a, object b, string path, ref double worst, ref string why)
    {
        if (a == null || b == null)
        {
            if (a == null && b == null) return true;
            why = path + ": null vs value";
            return false;
        }
        var da = a as Dictionary<string, object>;
        if (da != null)
        {
            var db = b as Dictionary<string, object>;
            if (db == null || db.Count != da.Count)
            {
                why = path + ": object keys differ";
                return false;
            }
            foreach (var kv in da)
            {
                object vb;
                if (!db.TryGetValue(kv.Key, out vb))
                {
                    why = path + "." + kv.Key + ": missing";
                    return false;
                }
                if (!CompareTrees(kv.Value, vb, path + "." + kv.Key, ref worst, ref why))
                    return false;
            }
            return true;
        }
        var la = a as List<object>;
        if (la != null)
        {
            var lb = b as List<object>;
            if (lb == null || lb.Count != la.Count)
            {
                why = path + ": list length " + la.Count + " vs " + (lb == null ? -1 : lb.Count);
                return false;
            }
            for (var i = 0; i < la.Count; i++)
                if (!CompareTrees(la[i], lb[i], path + "[" + i + "]", ref worst, ref why))
                    return false;
            return true;
        }
        if (a is double && b is double)
        {
            var d = Math.Abs((double)a - (double)b);
            worst = Math.Max(worst, d);
            return true;
        }
        if (!a.Equals(b))
        {
            why = path + ": " + a + " vs " + b;
            return false;
        }
        return true;
    }

    // ---------------------------------------------------------------------------------------------------------
    // T1 sweep: the reference's own inputs (Python random.Random = MT19937, init_by_array; 3.9 randrange)
    // ---------------------------------------------------------------------------------------------------------
    sealed class PyRandom
    {
        readonly uint[] mt = new uint[624];
        int mti = 625;

        public PyRandom(uint seed)
        {
            InitByArray(new[] { seed });
        }

        void InitGenrand(uint s)
        {
            mt[0] = s;
            for (mti = 1; mti < 624; mti++)
                mt[mti] = unchecked(1812433253U * (mt[mti - 1] ^ (mt[mti - 1] >> 30)) + (uint)mti);
        }

        void InitByArray(uint[] key)
        {
            InitGenrand(19650218U);
            int i = 1, j = 0;
            var k = 624 > key.Length ? 624 : key.Length;
            for (; k > 0; k--)
            {
                mt[i] = unchecked((mt[i] ^ ((mt[i - 1] ^ (mt[i - 1] >> 30)) * 1664525U)) + key[j] + (uint)j);
                i++;
                j++;
                if (i >= 624) { mt[0] = mt[623]; i = 1; }
                if (j >= key.Length) j = 0;
            }
            for (k = 623; k > 0; k--)
            {
                mt[i] = unchecked((mt[i] ^ ((mt[i - 1] ^ (mt[i - 1] >> 30)) * 1566083941U)) - (uint)i);
                i++;
                if (i >= 624) { mt[0] = mt[623]; i = 1; }
            }
            mt[0] = 0x80000000U;
        }

        public uint Genrand()
        {
            uint y;
            if (mti >= 624)
            {
                int kk;
                for (kk = 0; kk < 624 - 397; kk++)
                {
                    y = (mt[kk] & 0x80000000U) | (mt[kk + 1] & 0x7fffffffU);
                    mt[kk] = mt[kk + 397] ^ (y >> 1) ^ ((y & 1U) != 0 ? 0x9908b0dfU : 0U);
                }
                for (; kk < 623; kk++)
                {
                    y = (mt[kk] & 0x80000000U) | (mt[kk + 1] & 0x7fffffffU);
                    mt[kk] = mt[kk + (397 - 624)] ^ (y >> 1) ^ ((y & 1U) != 0 ? 0x9908b0dfU : 0U);
                }
                y = (mt[623] & 0x80000000U) | (mt[0] & 0x7fffffffU);
                mt[623] = mt[396] ^ (y >> 1) ^ ((y & 1U) != 0 ? 0x9908b0dfU : 0U);
                mti = 0;
            }
            y = mt[mti++];
            y ^= y >> 11;
            y ^= (y << 7) & 0x9d2c5680U;
            y ^= (y << 15) & 0xefc60000U;
            y ^= y >> 18;
            return y;
        }

        public double Random()
        {
            var a = Genrand() >> 5;
            var b = Genrand() >> 6;
            return (a * 67108864.0 + b) * (1.0 / 9007199254740992.0);
        }

        public double Uniform(double a, double b) => a + (b - a) * Random();

        /// <summary>randrange(1 &lt;&lt; 32) in Python 3.9: getrandbits(33) until &lt; 2^32.</summary>
        public uint RandRange32()
        {
            while (true)
            {
                var lo = Genrand();
                var hi = Genrand() >> 31;
                if (hi == 0)
                    return lo;
            }
        }
    }

    struct SweepCase
    {
        public string profile;
        public uint seed;
        public double x, y;
        public int side;
    }

    public static List<string> SweepInputsHex(int n)
    {
        var o = new List<string>();
        foreach (var c in SweepCases(n))
            o.Add(c.profile + " " + c.seed + " " + BitConverter.DoubleToInt64Bits(c.x).ToString("x16") + " " + BitConverter.DoubleToInt64Bits(c.y).ToString("x16") + " " + c.side);
        return o;
    }

    static List<SweepCase> SweepCases(int n)
    {
        var o = new List<SweepCase>();
        var spec = new[]
        {
            new KeyValuePair<string, int>("Annealed6", n), new KeyValuePair<string, int>("Annealed6_Cinematic", Math.Max(10, n / 3)),
            new KeyValuePair<string, int>("Annealed6_WebGL", Math.Max(10, n / 3)), new KeyValuePair<string, int>("Wired6", Math.Max(5, n / 12)),
            new KeyValuePair<string, int>("Tempered6", 5),
        };
        foreach (var s in spec)
        {
            var sum = 0;
            foreach (var ch in s.Key)
                sum += ch;
            var rnd = new PyRandom((uint)(20261003 + sum));
            for (var i = 0; i < s.Value; i++)
            {
                var seed = rnd.RandRange32();
                var x = rnd.Uniform(-0.4835, 0.4835);
                var y = rnd.Uniform(0.5665, 1.7835);
                var side = rnd.Random() < 0.8 ? 1 : -1;
                o.Add(new SweepCase { profile = s.Key, seed = seed, x = x, y = y, side = side });
            }
        }
        return o;
    }

    static double Pct(List<double> a, double f)
    {
        if (a.Count == 0) return double.NaN;
        var c = new List<double>(a);
        c.Sort();
        return c[Math.Min(c.Count - 1, (int)(c.Count * f))];
    }

    static void Sweep(int n)
    {
        var job = new FrontRoomsGlassFractureJob();
        var res = new FrontRoomsGlassFracture();
        var cases = SweepCases(n);
        var byProfile = new Dictionary<string, List<int>>();
        for (var i = 0; i < cases.Count; i++)
        {
            if (!byProfile.ContainsKey(cases[i].profile))
                byProfile[cases[i].profile] = new List<int>();
            byProfile[cases[i].profile].Add(i);
        }
        var tsv = new StringBuilder("i\tprofile\tseed\tx\ty\tside\tpieces\tbodies\tteeth\ttracks\tattempts\ttris_stage2\tlongest_free_m\ttooth_share\tvalid\tsummary\n");
        var poly = new List<GlassV2>();
        var totalFails = 0;
        foreach (var kv in byProfile)
        {
            var P = FrontRoomsGlassProfileData.BuiltinShared(kv.Key);
            List<double> pieces = new List<double>(), bodies = new List<double>(), teeth = new List<double>(), tracks = new List<double>(),
                attempts = new List<double>(), tris = new List<double>(), longest = new List<double>(), share = new List<double>();
            double worstArea = 0, worstOverlap = 0, minPiece = double.PositiveInfinity, crater = 0;
            int fails = 0, crossings = 0, nonConvex = 0, plus = 0, teethBad = 0;
            foreach (var i in kv.Value)
            {
                var c = cases[i];
                FrontRoomsGlassFracturePattern.BeginRoot(job, P, c.seed, c.x, c.y, FrontRoomsGlassFracturePattern.SlabW, FrontRoomsGlassFracturePattern.SlabH, double.NaN, c.side, "band", res);
                while (!job.Step())
                {
                }
                var v = job.Validation;
                var ok = !job.Failed && v.AllOk;
                if (!ok) fails++;
                worstArea = Math.Max(worstArea, Math.Abs(v.areaRelErrorPct));
                worstOverlap = Math.Max(worstOverlap, v.worstOverlapMm2);
                minPiece = Math.Min(minPiece, v.minCm2);
                crossings += v.crossings;
                nonConvex += v.nonConvexIds.Count;
                plus += v.plusJunctions;
                teethBad += v.teethBad.Count;
                crater = Math.Max(crater, v.craterMm);
                pieces.Add(res.stats.piecesTotal);
                double lf = 0.0, sh = double.NaN;
                if (!res.isDice)
                {
                    bodies.Add(res.stats.bodies);
                    teeth.Add(res.stats.tooth);
                    tracks.Add(res.stats.tracksTotal);
                    attempts.Add(res.attempts);
                    tris.Add(res.stats.trisStage2);
                    // fix 2: the longest free shard; fix 3: teeth are a subset of the pieces touching the frame
                    var touching = 0;
                    for (var k = 0; k < res.pieces.Count; k++)
                    {
                        var p = res.pieces[k];
                        res.GetPoly(k, poly);
                        var touches = MaxContact(poly, res.frame) >= 0.005;
                        if (p.kind == GlassPieceKind.Shard && !touches)
                        {
                            int di, dj;
                            lf = Math.Max(lf, GlassPoly.Diameter(poly, out di, out dj));
                        }
                        if (p.kind != GlassPieceKind.Crush && touches)
                            touching++;
                    }
                    longest.Add(lf);
                    sh = (double)res.stats.tooth / Math.Max(1, touching);
                    share.Add(sh);
                }
                tsv.Append(i).Append('\t').Append(c.profile).Append('\t').Append(c.seed).Append('\t').Append(c.x.ToString("R", Inv)).Append('\t').Append(c.y.ToString("R", Inv))
                    .Append('\t').Append(c.side).Append('\t').Append(res.stats.piecesTotal).Append('\t').Append(res.stats.bodies).Append('\t').Append(res.stats.tooth)
                    .Append('\t').Append(res.stats.tracksTotal).Append('\t').Append(res.attempts).Append('\t').Append(res.stats.trisStage2).Append('\t')
                    .Append(lf.ToString("0.0000", Inv)).Append('\t').Append(double.IsNaN(sh) ? "" : sh.ToString("0.000", Inv)).Append('\t').Append(ok).Append('\t').Append(v.Summary()).Append('\n');
            }
            totalFails += fails;
            Func<List<double>, string> mpm = l => l.Count == 0 ? "-" : Pct(l, 0.0).ToString("0", Inv) + " / " + Pct(l, 0.5).ToString("0", Inv) + " / " + Pct(l, 1.0 - 1e-9).ToString("0", Inv);
            Check(fails == 0, "T1 sweep " + kv.Key + ": " + (kv.Value.Count - fails) + " / " + kv.Value.Count + " valid; pieces " + mpm(pieces) + ", bodies " + mpm(bodies) +
                ", teeth " + mpm(teeth) + ", tracks " + mpm(tracks) + ", stage-2 tris " + mpm(tris) + ", attempts p50 " + Pct(attempts, 0.5).ToString("0", Inv) + " max " + Pct(attempts, 1 - 1e-9).ToString("0", Inv) +
                "; worst area error " + worstArea.ToString("0.0000000", Inv) + " %, worst overlap " + worstOverlap.ToString("0.000", Inv) + " mm2, smallest piece " + minPiece.ToString("0.000", Inv) +
                " cm2, crossings " + crossings + ", non-convex " + nonConvex + ", '+' " + plus + ", teeth outside band " + teethBad + ", crater max " + crater.ToString("0.0", Inv) + " mm" +
                (longest.Count > 0 ? ", longest free shard " + Pct(longest, 1 - 1e-9).ToString("0.000", Inv) + " m" : "") +
                (share.Count > 0 ? ", tooth share p50 " + Pct(share, 0.5).ToString("0.000", Inv) : ""));
            if (kv.Key == "Annealed6")
            {
                // T5: the four prototype fixes
                Check(crossings == 0 && Pct(longest, 1 - 1e-9) <= P.daggerMax + 1e-9 && teethBad == 0 && Pct(share, 0.5) < 1.0 && fails == 0,
                    "T5 prototype fixes (Annealed6 sweep): crossings " + crossings + "; longest free shard " + Pct(longest, 1 - 1e-9).ToString("0.000", Inv) + " m <= " + P.daggerMax.ToString("0.00", Inv) +
                    "; teeth a subset (share p50 " + Pct(share, 0.5).ToString("0.000", Inv) + "), 0 outside the band; no holes (area within 0.05 %)");
            }
        }
        File.WriteAllText(Path.Combine(outDir, "sweep.tsv"), tsv.ToString());
        Note("T1 sweep: " + cases.Count + " breaks, " + totalFails + " invalid (inputs = test_crack_graph_ref.py T1, Python 3.9 Mersenne Twister draws)");
    }

    static double MaxContact(List<GlassV2> poly, GlassFrame fr)
    {
        double cL = 0.0, cR = 0.0, cT = 0.0, cB = 0.0;
        var n = poly.Count;
        for (var i = 0; i < n; i++)
        {
            var p = poly[i];
            var q = poly[(i + 1) % n];
            var L = GlassMath.Hypot(q.x - p.x, q.y - p.y);
            if (Math.Abs(p.x + fr.hx) < 1e-9 && Math.Abs(q.x + fr.hx) < 1e-9) cL += L;
            if (Math.Abs(p.x - fr.hx) < 1e-9 && Math.Abs(q.x - fr.hx) < 1e-9) cR += L;
            if (Math.Abs(p.y - fr.hy) < 1e-9 && Math.Abs(q.y - fr.hy) < 1e-9) cT += L;
            if (Math.Abs(p.y + fr.hy) < 1e-9 && Math.Abs(q.y + fr.hy) < 1e-9) cB += L;
        }
        return Math.Max(Math.Max(cL, cR), Math.Max(cT, cB));
    }

    // ---------------------------------------------------------------------------------------------------------
    // T2 determinism
    // ---------------------------------------------------------------------------------------------------------
    static string Gen(FrontRoomsGlassFractureJob job, string profile, uint seed, double x, double y, int side = 1, string band = "band",
        double rot = double.NaN, FrontRoomsGlassFracture res = null)
    {
        res = res ?? new FrontRoomsGlassFracture();
        FrontRoomsGlassFracturePattern.BeginRoot(job, FrontRoomsGlassProfileData.BuiltinShared(profile), seed, x, y, FrontRoomsGlassFracturePattern.SlabW,
            FrontRoomsGlassFracturePattern.SlabH, rot, side, band, res);
        while (!job.Step())
        {
        }
        return FrontRoomsGlassFracturePattern.ToJson(res);
    }

    static void Determinism()
    {
        var job = new FrontRoomsGlassFractureJob();
        var a = Gen(job, "Annealed6", 4242, 0.0, 1.62);
        var b = Gen(job, "Annealed6", 4242, 0.0, 1.62);
        var other = Gen(new FrontRoomsGlassFractureJob(), "Annealed6", 4242, 0.0, 1.62);
        var viaApi = FrontRoomsGlassFracturePattern.ToJson(FrontRoomsGlassFracturePattern.GenerateAtRoot(FrontRoomsGlassProfileData.BuiltinShared("Annealed6"), 4242, 0.0, 1.62));
        // after other breaks on the same job (pools full of old data)
        Gen(job, "Annealed6_WebGL", 99, 0.3, 1.0);
        Gen(job, "Tempered6", 5, -0.2, 1.4);
        var c = Gen(job, "Annealed6", 4242, 0.0, 1.62);
        var d = Gen(job, "Annealed6", 4243, 0.0, 1.62);
        // the job is plain C#: on desktop it can run off the main thread (WebGL has no threads and slices it)
        string onWorker = null;
        var t = new System.Threading.Thread(() => onWorker = Gen(new FrontRoomsGlassFractureJob(), "Annealed6", 4242, 0.0, 1.62));
        t.Start();
        t.Join();
        Check(onWorker == a, "T2 worker thread: the same break generated on a worker thread gives the same bytes " + (onWorker == a));
        Check(a == b && a == other && a == viaApi && a == c && d != a,
            "T2 determinism: same inputs -> same bytes (same job twice, fresh job, static API, after other breaks on the reused job); seed 4243 differs; sha " + Sha(a).Substring(0, 16));
    }

    // ---------------------------------------------------------------------------------------------------------
    // T4 negatives: each planted defect flips exactly the check that guards it
    // ---------------------------------------------------------------------------------------------------------
    static FrontRoomsGlassFracture Base()
    {
        var job = new FrontRoomsGlassFractureJob();
        var r = new FrontRoomsGlassFracture();
        FrontRoomsGlassFracturePattern.BeginRoot(job, FrontRoomsGlassProfileData.BuiltinShared("Annealed6"), 4242, 0.0, 1.62, FrontRoomsGlassFracturePattern.SlabW,
            FrontRoomsGlassFracturePattern.SlabH, double.NaN, 1, "band", r);
        while (!job.Step())
        {
        }
        return r;
    }

    static void Negatives()
    {
        var P = FrontRoomsGlassProfileData.BuiltinShared("Annealed6");
        var v = new FrontRoomsGlassFractureValidation();
        var r = Base();
        v.Run(r, P);
        Check(v.AllOk, "T4 base break valid (" + v.Summary() + ")");
        var poly = new List<GlassV2>();
        // (a) a dropped piece leaves a hole (the 04 prototype's 39 cm2 there)
        r = Base();
        var target = -1;
        for (var i = 0; i < r.pieces.Count; i++)
            if (r.pieces[i].kind == GlassPieceKind.Shard && r.pieces[i].area > 30e-4 &&
                (target < 0 || Math.Abs(r.pieces[i].area - 39e-4) < Math.Abs(r.pieces[target].area - 39e-4)))
                target = i;
        var holeCm2 = r.pieces[target].area * 1e4;
        r.pieces.RemoveAt(target);
        v.Run(r, P);
        var hole = !v.areaSumOk;
        // (b) a non-convex piece (dent one vertex inward past its neighbours)
        r = Base();
        var big = -1;
        for (var i = 0; i < r.pieces.Count; i++)
            if (r.pieces[i].kind == GlassPieceKind.Shard && r.pieces[i].vn >= 4 && (big < 0 || r.pieces[i].area > r.pieces[big].area))
                big = i;
        r.GetPoly(big, poly);
        var cen = GlassPoly.Centroid(poly);
        var v0 = r.verts[r.pieces[big].v0];
        r.verts[r.pieces[big].v0] = new GlassV2(cen.x + 0.05 * (v0.x - cen.x), cen.y + 0.05 * (v0.y - cen.y));
        v.Run(r, P);
        var nonConvex = !v.convexOk;
        // (c) a crossing: bend one radial track across its neighbour
        r = Base();
        var ti = -1;
        for (var i = 0; i < r.tracks.Count; i++)
            if (r.tracks[i].initial && r.tracks[i].vn > 4)
            {
                ti = i;
                break;
            }
        var tp = r.graphVerts[r.tracks[ti].v0 + 3];
        var ix = r.impact.x;
        var iy = r.impact.y;
        var ang = Math.Atan2(tp.y - iy, tp.x - ix) + 0.9;
        var rad = GlassMath.Hypot(tp.x - ix, tp.y - iy);
        r.graphVerts[r.tracks[ti].v0 + 3] = new GlassV2(ix + rad * Math.Cos(ang), iy + rad * Math.Sin(ang));
        v.Run(r, P);
        var crossing = !v.noCrossingsOk;
        // (d) a sliver under 1 cm2 outside the crush core
        r = Base();
        var sm = -1;
        for (var i = 0; i < r.pieces.Count; i++)
            if (r.pieces[i].kind == GlassPieceKind.Shard && (sm < 0 || r.pieces[i].area < r.pieces[sm].area))
                sm = i;
        var sp = r.pieces[sm];
        sp.area = 0.5e-4;
        r.pieces[sm] = sp;
        v.Run(r, P);
        var sliver = !v.minAreaOk;
        // (e) a tooth reaching into the clear zone
        r = Base();
        var tt = -1;
        for (var i = 0; i < r.pieces.Count; i++)
            if (r.pieces[i].kind == GlassPieceKind.Tooth)
            {
                tt = i;
                break;
            }
        r.GetPoly(tt, poly);
        var tc = GlassPoly.Centroid(poly);
        for (var k = 0; k < r.pieces[tt].vn; k++)
        {
            var q = r.verts[r.pieces[tt].v0 + k];
            r.verts[r.pieces[tt].v0 + k] = new GlassV2(0.0 + (q.x - tc.x), 0.0 + (q.y - tc.y));
        }
        v.Run(r, P);
        var toothClear = !v.teethOk;
        // (f) a '+' junction: move one ring crack's end onto the other side's T on the same radial
        r = Base();
        var done = false;
        for (var i = 0; i < r.chords.Count && !done; i++)
            for (var j = 0; j < r.chords.Count; j++)
                if (i != j && r.chords[i].ring == r.chords[j].ring && r.chords[i].b == r.chords[j].a)
                {
                    var cj = r.chords[j];
                    cj.p0 = r.chords[i].p1;
                    r.chords[j] = cj;
                    done = true;
                    break;
                }
        v.Run(r, P);
        var plusJ = done && !v.v2Ok;
        Check(hole && nonConvex && crossing && sliver && toothClear && plusJ,
            "T4 negatives caught: hole " + holeCm2.ToString("0", Inv) + " cm2 " + hole + ", non-convex " + nonConvex + ", crossing " + crossing + ", sliver < 1 cm2 " + sliver +
            ", tooth in the clear zone " + toothClear + ", '+' junction " + plusJ);
    }

    // ---------------------------------------------------------------------------------------------------------
    // T6, T7
    // ---------------------------------------------------------------------------------------------------------
    static void BandStop()
    {
        var job = new FrontRoomsGlassFractureJob();
        var r = new FrontRoomsGlassFracture();
        Gen(job, "Annealed6", 4242, 0.40, 0.90, 1, "stop", double.NaN, r);
        Check(job.Validation.AllOk && r.stats.tooth == 0, "T6 band_mode stop: valid " + job.Validation.AllOk + ", " + r.stats.tooth + " teeth (no visible tooth)");
    }

    static void Contract()
    {
        var job = new FrontRoomsGlassFractureJob();
        var P = FrontRoomsGlassProfileData.BuiltinShared("Annealed6");
        double x = 0.40, y = 0.90;
        var u = (x + FrontRoomsGlassFracturePattern.StopX) / (2 * FrontRoomsGlassFracturePattern.StopX);
        var vv = (y - FrontRoomsGlassFracturePattern.StopBot) / (FrontRoomsGlassFracturePattern.StopTop - FrontRoomsGlassFracturePattern.StopBot);
        var a = new FrontRoomsGlassFracture();
        Gen(job, "Annealed6", 4242, x, y, 1, "band", double.NaN, a);
        var aj = FrontRoomsGlassFracturePattern.ToTree(a) as IDictionary<string, object>;
        var b = new FrontRoomsGlassFracture();
        FrontRoomsGlassFracturePattern.BeginUV(job, P, 4242, new GlassV2(u, vv), new GlassV2(FrontRoomsGlassFracturePattern.SlabW, FrontRoomsGlassFracturePattern.SlabH), double.NaN, 1, "band", b);
        while (!job.Step())
        {
        }
        var bj = FrontRoomsGlassFracturePattern.ToTree(b) as IDictionary<string, object>;
        var uvEqualsRoot = Json(aj["pieces"]) == Json(bj["pieces"]);
        var uvRoundTrip = Math.Abs(a.impactUV.x - u) < 1e-6 && Math.Abs(a.impactUV.y - vv) < 1e-6;
        var c = new FrontRoomsGlassFracture();
        Gen(job, "Annealed6", 4242, x, y, -1, "band", double.NaN, c);
        var cj = FrontRoomsGlassFracturePattern.ToTree(c) as IDictionary<string, object>;
        var sideOnly = Json(cj["pieces"]) == Json(aj["pieces"]) && Json(cj["graph"]) == Json(aj["graph"]) && a.farZSign == 1 && c.farZSign == -1;
        var d = new FrontRoomsGlassFracture();
        Gen(job, "Annealed6", 4242, 0.68, 0.37, 1, "band", double.NaN, d);
        var clamp = Math.Abs(d.impactRoot.x - (FrontRoomsGlassFracturePattern.StopX - 0.2)) < 1e-6 && Math.Abs(d.impactRoot.y - (FrontRoomsGlassFracturePattern.StopBot + 0.2)) < 1e-6;
        var e = new FrontRoomsGlassFracture();
        Gen(job, "Annealed6", 0x9E3779B9, x, y, 1, "band", double.NaN, e);
        var mapRot = MapUnit(0x9E3779B9) * 360f;   // the map's float32 arithmetic (FrontRoomsMapWorld.GlassBreakOf)
        var rotIsMap = Math.Abs(e.rotationDeg - mapRot) < 1e-4;
        // GlassBreakRecord carries floats: widened float inputs still give a valid, deterministic break
        var f1 = Gen(job, "Annealed6", unchecked((uint)-123456789), (float)0.1234f, (float)1.3456f, 1, "band", (double)(MapUnit(unchecked((uint)-123456789)) * 360f));
        var fv = job.Validation.AllOk;
        var f2 = Gen(job, "Annealed6", unchecked((uint)-123456789), (float)0.1234f, (float)1.3456f, 1, "band", (double)(MapUnit(unchecked((uint)-123456789)) * 360f));
        Check(uvEqualsRoot && uvRoundTrip && sideOnly && clamp && rotIsMap && fv && f1 == f2,
            "T7 contract: impactUV == impact in metres " + uvEqualsRoot + "; impact_uv round trip " + uvRoundTrip + "; side changes only far_z_sign " + sideOnly +
            "; clamp like the map " + clamp + "; default rotation = MapHash.Unit(seed) * 360f " + rotIsMap + " (" + e.rotationDeg.ToString("R", Inv) + "); float record inputs valid " + fv + " and repeatable " + (f1 == f2));
    }

    static float MapUnit(uint h) => (h >> 8) * (1f / 16777216f);

    static string Json(object tree)
    {
        var sb = new StringBuilder();
        MiniJson.Write(sb, tree);
        return sb.ToString();
    }

    // ---------------------------------------------------------------------------------------------------------
    // T8 profiles
    // ---------------------------------------------------------------------------------------------------------
    static void Profiles()
    {
        var found = 0;
        var diffs = new List<string>();
        foreach (var name in FrontRoomsGlassProfileData.BuiltinNames)
        {
            var asset = Resources.Load<FrontRoomsGlassTypeProfile>(FrontRoomsGlassTypeProfile.ResourceFolder + "/" + name);
            if (asset == null)
                continue;
            found++;
            var d = new List<string>();
            if (!FrontRoomsGlassProfileData.SameNumbers(asset.data, FrontRoomsGlassProfileData.BuiltinShared(name), d))
                diffs.Add(name + ": " + string.Join("; ", d.ToArray()));
        }
        Check(diffs.Count == 0, "T8 profiles: " + found + " / " + FrontRoomsGlassProfileData.BuiltinNames.Length + " assets in Resources/" + FrontRoomsGlassTypeProfile.ResourceFolder +
            (diffs.Count == 0 ? ", all equal to the built-ins (= crack_graph_ref.py PROFILES)" : ", DRIFT: " + string.Join(" | ", diffs.ToArray())));
        var tier = FrontRoomsGlassTypeProfile.ForTier();
        Check(tier != null && tier.name == FrontRoomsGlassProfileData.Annealed6, "T8 editor/desktop tier resolves to " + (tier == null ? "null" : tier.name) + " (WebGL players get Annealed6_WebGL only behind UNITY_WEBGL)");
    }

    // ---------------------------------------------------------------------------------------------------------
    // P: time and allocation per slice
    // ---------------------------------------------------------------------------------------------------------
    static void PerfRun(int n)
    {
        var job = new FrontRoomsGlassFractureJob();
        var res = new FrontRoomsGlassFracture();
        var tsv = new StringBuilder("profile\tbreak\tstep\tphase\tattempt\tcpu_ms\twall_ms\talloc_b\n");
        var summary = new StringBuilder();
        var loadStart = Load1();
        var gc0 = GC.CollectionCount(0);
        var maxCpuAll = 0.0;
        var maxWallAll = 0.0;
        var allocAfterWarm = 0L;
        long allocMaxSlice = 0;
        var slicesTotal = 0;
        var cold = new StringBuilder();
        var frameMaxCpuAll = 0.0;
        foreach (var prof in new[] { "Annealed6", "Annealed6_Cinematic", "Annealed6_WebGL" })
        {
            var P = FrontRoomsGlassProfileData.BuiltinShared(prof);
            var rng = GlassRng.Make(777, prof.Length);
            var cpu = new Dictionary<string, List<double>>();
            var wall = new Dictionary<string, List<double>>();
            var alloc = new Dictionary<string, List<double>>();
            var perBreakCpu = new List<double>();
            var perBreakWall = new List<double>();
            var perBreakSteps = new List<double>();
            var perBreakAlloc = new List<double>();
            const int warm = 20;
            for (var bi = -warm; bi < n; bi++)
            {
                var seed = rng.NextU32();
                var x = rng.Uniform(-0.4835, 0.4835);
                var y = rng.Uniform(0.5665, 1.7835);
                FrontRoomsGlassFracturePattern.BeginRoot(job, P, seed, x, y, FrontRoomsGlassFracturePattern.SlabW, FrontRoomsGlassFracturePattern.SlabH, double.NaN, 1, "band", res);
                double bc = 0, bw = 0;
                long ba = 0;
                var steps = 0;
                while (true)
                {
                    var phase = job.Next.ToString();
                    var attempt = job.Attempt;
                    var a0 = GC.GetAllocatedBytesForCurrentThread();
                    var c0 = ThreadCpuMs();
                    var t0 = Stopwatch.GetTimestamp();
                    var done = job.Step();
                    var w = (Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency;
                    var c = ThreadCpuMs() - c0;
                    var al = GC.GetAllocatedBytesForCurrentThread() - a0;
                    steps++;
                    bc += c;
                    bw += w;
                    ba += al;
                    if (bi < 0 && bi == -warm)
                        cold.Append(prof).Append(" first break (JIT) step ").Append(phase).Append(": cpu ").Append(c.ToString("0.000", Inv)).Append(" ms, alloc ").Append(al).Append(" B; ");
                    if (bi >= 0)
                    {
                        if (!cpu.ContainsKey(phase))
                        {
                            cpu[phase] = new List<double>();
                            wall[phase] = new List<double>();
                            alloc[phase] = new List<double>();
                        }
                        cpu[phase].Add(c);
                        wall[phase].Add(w);
                        alloc[phase].Add(al);
                        maxCpuAll = Math.Max(maxCpuAll, c);
                        maxWallAll = Math.Max(maxWallAll, w);
                        allocAfterWarm += al;
                        allocMaxSlice = Math.Max(allocMaxSlice, al);
                        slicesTotal++;
                        tsv.Append(prof).Append('\t').Append(bi).Append('\t').Append(steps).Append('\t').Append(phase).Append('\t').Append(attempt).Append('\t')
                            .Append(c.ToString("0.0000", Inv)).Append('\t').Append(w.ToString("0.0000", Inv)).Append('\t').Append(al).Append('\n');
                    }
                    if (done)
                        break;
                }
                if (job.Failed || !job.Validation.AllOk)
                    Check(false, "P " + prof + " break " + bi + " invalid: " + job.Error + " " + job.Validation.Summary());
                if (bi >= 0)
                {
                    perBreakCpu.Add(bc);
                    perBreakWall.Add(bw);
                    perBreakSteps.Add(steps);
                    perBreakAlloc.Add(ba);
                }
            }
            // frames: StepFor(budget) once per frame, budget in thread CPU (load-robust) and in wall time (as the game runs it)
            var rngF = GlassRng.Make(778, prof.Length);
            var framesCpu = new List<double>();
            var framesWall = new List<double>();
            var frameMaxCpu = 0.0;
            var frameCpuAll = new List<double>();
            for (var bi = 0; bi < Math.Min(n, 40); bi++)
            {
                var seed = rngF.NextU32();
                var x = rngF.Uniform(-0.4835, 0.4835);
                var y = rngF.Uniform(0.5665, 1.7835);
                FrontRoomsGlassFracturePattern.BeginRoot(job, P, seed, x, y, FrontRoomsGlassFracturePattern.SlabW, FrontRoomsGlassFracturePattern.SlabH, double.NaN, 1, "band", res);
                // StepFor itself, on this thread's CPU clock: the packing a quiet machine would see (load-independent)
                var frames = 0;
                var cpuClock = !double.IsNaN(ThreadCpuMs());
                if (cpuClock)
                    FrontRoomsGlassFractureJob.ClockMs = ThreadCpuMs;
                try
                {
                    while (!job.IsDone)
                    {
                        frames++;
                        var c0 = ThreadCpuMs();
                        job.StepFor(1.0);
                        var fc = ThreadCpuMs() - c0;
                        frameMaxCpu = Math.Max(frameMaxCpu, fc);
                        frameCpuAll.Add(fc);
                    }
                }
                finally
                {
                    FrontRoomsGlassFractureJob.ClockMs = DefaultClock;
                }
                framesCpu.Add(frames);
                FrontRoomsGlassFracturePattern.BeginRoot(job, P, seed, x, y, FrontRoomsGlassFracturePattern.SlabW, FrontRoomsGlassFracturePattern.SlabH, double.NaN, 1, "band", res);
                frames = 0;
                while (!job.IsDone)
                {
                    frames++;
                    job.StepFor(1.0);
                }
                framesWall.Add(frames);
            }
            frameMaxCpuAll = Math.Max(frameMaxCpuAll, frameMaxCpu);
            var over = 0;
            foreach (var v in frameCpuAll)
                if (v > 1.0)
                    over++;
            summary.Append(prof).Append(" frames per break, StepFor(1 ms) on the thread-CPU clock: p50 ").Append(Pct(framesCpu, 0.5).ToString("0", Inv)).Append(" max ").Append(Pct(framesCpu, 1 - 1e-9).ToString("0", Inv))
                .Append(" (frame CPU p50 ").Append(Pct(frameCpuAll, 0.5).ToString("0.000", Inv)).Append(" p99 ").Append(Pct(frameCpuAll, 0.99).ToString("0.000", Inv))
                .Append(" max ").Append(frameMaxCpu.ToString("0.000", Inv)).Append(" ms; ").Append(over).Append(" of ").Append(frameCpuAll.Count).Append(" frames over 1 ms)")
                .Append("; on the wall clock (as the game runs, this machine's load included) p50 ").Append(Pct(framesWall, 0.5).ToString("0", Inv)).Append(" max ")
                .Append(Pct(framesWall, 1 - 1e-9).ToString("0", Inv)).Append('\n');
            summary.Append(prof).Append(": ").Append(n).Append(" breaks after ").Append(warm).Append(" warm-up; per break CPU p50 ").Append(Pct(perBreakCpu, 0.5).ToString("0.00", Inv))
                .Append(" / p90 ").Append(Pct(perBreakCpu, 0.9).ToString("0.00", Inv)).Append(" / max ").Append(Pct(perBreakCpu, 1 - 1e-9).ToString("0.00", Inv)).Append(" ms, wall p50 ")
                .Append(Pct(perBreakWall, 0.5).ToString("0.00", Inv)).Append(" ms, steps p50 ").Append(Pct(perBreakSteps, 0.5).ToString("0", Inv)).Append(" max ").Append(Pct(perBreakSteps, 1 - 1e-9).ToString("0", Inv))
                .Append(", alloc per break p50 ").Append(Pct(perBreakAlloc, 0.5).ToString("0", Inv)).Append(" B max ").Append(Pct(perBreakAlloc, 1 - 1e-9).ToString("0", Inv)).Append(" B\n");
            foreach (var kv in cpu)
                summary.Append("    ").Append(kv.Key.PadRight(10)).Append(" n ").Append(kv.Value.Count.ToString().PadLeft(5)).Append("  CPU p50 ").Append(Pct(kv.Value, 0.5).ToString("0.000", Inv))
                    .Append(" p99 ").Append(Pct(kv.Value, 0.99).ToString("0.000", Inv)).Append(" max ").Append(Pct(kv.Value, 1 - 1e-9).ToString("0.000", Inv)).Append(" ms | wall p50 ")
                    .Append(Pct(wall[kv.Key], 0.5).ToString("0.000", Inv)).Append(" p99 ").Append(Pct(wall[kv.Key], 0.99).ToString("0.000", Inv)).Append(" max ")
                    .Append(Pct(wall[kv.Key], 1 - 1e-9).ToString("0.000", Inv)).Append(" ms | alloc p50 ").Append(Pct(alloc[kv.Key], 0.5).ToString("0", Inv)).Append(" max ")
                    .Append(Pct(alloc[kv.Key], 1 - 1e-9).ToString("0", Inv)).Append(" B\n");
        }
        var gc1 = GC.CollectionCount(0);
        var loadEnd = Load1();
        File.WriteAllText(Path.Combine(outDir, "perf_slices.tsv"), tsv.ToString());
        File.WriteAllText(Path.Combine(outDir, "perf_summary.txt"), "load1 " + loadStart + " -> " + loadEnd + "\n" + summary + "\ncold: " + cold);
        Note("perf (" + LoadTag(Math.Max(loadStart, loadEnd)) + "):\n" + summary + "cold: " + cold);
        var cpuOk = !double.IsNaN(maxCpuAll) && maxCpuAll <= 1.0;
        Check(cpuOk, "P time per slice: max thread CPU of one Step() " + maxCpuAll.ToString("0.000", Inv) + " ms over " + slicesTotal + " slices (bar 1 ms); max wall " + maxWallAll.ToString("0.000", Inv) +
            " ms; a frame running StepFor(1 ms) on the CPU clock peaks at " + frameMaxCpuAll.ToString("0.000", Inv) + " ms CPU (" + LoadTag(Math.Max(loadStart, loadEnd)) + ")");
        if (allocCounterWorks)
            Check(allocMaxSlice <= 64 * 1024, "P allocation after warm-up: " + allocAfterWarm + " B over " + slicesTotal + " slices, max " + allocMaxSlice +
                " B in one slice; gen-0 collections during the run " + (gc1 - gc0));
        else
            HeapRun(n);
    }

    /// <summary>
    /// The Editor's Boehm GC does not count per-thread allocation, so here the managed heap's used size
    /// (GC.GetTotalMemory(false)) is read around each break: between collections it only grows by what was allocated.
    /// Breaks during which a collection ran are left out of the numbers.
    /// </summary>
    static void HeapRun(int n)
    {
        var job = new FrontRoomsGlassFractureJob();
        var res = new FrontRoomsGlassFracture();
        var lines = new StringBuilder();
        var worstBreak = 0L;
        var total = 0L;
        var counted = 0;
        var skipped = 0;
        // probe: does the used size see SMALL allocations? (10,000 x object[4] = ~400 KB)
        var keep = new object[10000];
        var p0 = GC.GetTotalMemory(false);
        var pg = GC.CollectionCount(0);
        for (var i = 0; i < keep.Length; i++)
            keep[i] = new object[4];
        var probe = GC.GetTotalMemory(false) - p0;
        var probeOk = GC.CollectionCount(0) == pg && probe >= 200 * 1024;
        Note("heap probe: 10,000 small arrays (~400 KB) -> used size +" + probe + " B (" + (probeOk ? "small allocations are seen" : "NOT seen: the heap numbers below are a lower bound") + ")");
        keep = null;
        var gcStart = GC.CollectionCount(0);
        foreach (var prof in new[] { "Annealed6", "Annealed6_Cinematic", "Annealed6_WebGL", "Wired6", "Tempered6" })
        {
            var P = FrontRoomsGlassProfileData.BuiltinShared(prof);
            var rng = GlassRng.Make(999, prof.Length);
            var deltas = new List<double>();
            for (var bi = -20; bi < n; bi++)
            {
                var seed = rng.NextU32();
                var x = rng.Uniform(-0.4835, 0.4835);
                var y = rng.Uniform(0.5665, 1.7835);
                var g0 = GC.CollectionCount(0);
                var m0 = GC.GetTotalMemory(false);
                FrontRoomsGlassFracturePattern.BeginRoot(job, P, seed, x, y, FrontRoomsGlassFracturePattern.SlabW, FrontRoomsGlassFracturePattern.SlabH, double.NaN, 1, "band", res);
                while (!job.Step())
                {
                }
                var m1 = GC.GetTotalMemory(false);
                if (bi < 0)
                    continue;
                if (GC.CollectionCount(0) != g0)
                {
                    skipped++;
                    continue;
                }
                var d = m1 - m0;
                deltas.Add(d);
                total += d;
                counted++;
                worstBreak = Math.Max(worstBreak, d);
            }
            lines.Append(prof).Append(": heap growth per break p50 ").Append(Pct(deltas, 0.5).ToString("0", Inv)).Append(" B, max ").Append(Pct(deltas, 1 - 1e-9).ToString("0", Inv))
                .Append(" B over ").Append(deltas.Count).Append(" breaks; ");
        }
        var gcs = GC.CollectionCount(0) - gcStart;
        Note("heap: " + lines + "collections during the run " + gcs + ", breaks left out (a collection ran) " + skipped);
        Check(counted > 0 && worstBreak <= 64 * 1024, "P allocation after warm-up (Editor, heap used size" + (probeOk ? "" : ", probe failed: lower bound") + "): " + total + " B over " + counted +
            " breaks, worst break " + worstBreak + " B; GC collections during the run " + gcs + " (" + skipped + " breaks left out)");
    }

    // ---------------------------------------------------------------------------------------------------------
    // A minimal JSON reader/writer (golden files; Unity's JsonUtility cannot read nested lists)
    // ---------------------------------------------------------------------------------------------------------
    public static class MiniJson
    {
        public static object Parse(string s)
        {
            var i = 0;
            var v = Value(s, ref i);
            return v;
        }

        static void Ws(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\n' || s[i] == '\r' || s[i] == '\t'))
                i++;
        }

        static object Value(string s, ref int i)
        {
            Ws(s, ref i);
            var c = s[i];
            if (c == '{')
            {
                var d = new Dictionary<string, object>(StringComparer.Ordinal);
                i++;
                Ws(s, ref i);
                if (s[i] == '}')
                {
                    i++;
                    return d;
                }
                while (true)
                {
                    Ws(s, ref i);
                    var k = Str(s, ref i);
                    Ws(s, ref i);
                    i++; // ':'
                    d[k] = Value(s, ref i);
                    Ws(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    i++; // '}'
                    return d;
                }
            }
            if (c == '[')
            {
                var l = new List<object>();
                i++;
                Ws(s, ref i);
                if (s[i] == ']')
                {
                    i++;
                    return l;
                }
                while (true)
                {
                    l.Add(Value(s, ref i));
                    Ws(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    i++; // ']'
                    return l;
                }
            }
            if (c == '"')
                return Str(s, ref i);
            if (s.Substring(i).StartsWith("true", StringComparison.Ordinal)) { i += 4; return true; }
            if (s.Substring(i, Math.Min(5, s.Length - i)).StartsWith("false", StringComparison.Ordinal)) { i += 5; return false; }
            if (s.Substring(i, Math.Min(4, s.Length - i)).StartsWith("null", StringComparison.Ordinal)) { i += 4; return null; }
            var st = i;
            var isFloat = false;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0)
            {
                if (s[i] == '.' || s[i] == 'e' || s[i] == 'E') isFloat = true;
                i++;
            }
            var num = s.Substring(st, i - st);
            if (isFloat)
                return double.Parse(num, NumberStyles.Float, Inv);
            return long.Parse(num, Inv);
        }

        static string Str(string s, ref int i)
        {
            var sb = new StringBuilder();
            i++; // '"'
            while (s[i] != '"')
            {
                if (s[i] == '\\')
                {
                    i++;
                    var e = s[i];
                    if (e == 'n') sb.Append('\n');
                    else if (e == 't') sb.Append('\t');
                    else if (e == 'r') sb.Append('\r');
                    else if (e == 'u') { sb.Append((char)Convert.ToInt32(s.Substring(i + 1, 4), 16)); i += 4; }
                    else sb.Append(e);
                    i++;
                    continue;
                }
                sb.Append(s[i]);
                i++;
            }
            i++;
            return sb.ToString();
        }

        public static void Write(StringBuilder sb, object v)
        {
            if (v == null) { sb.Append("null"); return; }
            var d = v as IDictionary<string, object>;
            if (d != null)
            {
                var keys = new List<string>(d.Keys);
                keys.Sort(StringComparer.Ordinal);
                sb.Append('{');
                for (var i = 0; i < keys.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append('"').Append(keys[i]).Append("\":");
                    Write(sb, d[keys[i]]);
                }
                sb.Append('}');
                return;
            }
            var l = v as List<object>;
            if (l != null)
            {
                sb.Append('[');
                for (var i = 0; i < l.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    Write(sb, l[i]);
                }
                sb.Append(']');
                return;
            }
            if (v is double) { sb.Append(((double)v).ToString("R", Inv)); return; }
            if (v is bool) { sb.Append((bool)v ? "true" : "false"); return; }
            if (v is long) { sb.Append(((long)v).ToString(Inv)); return; }
            sb.Append('"').Append(v).Append('"');
        }
    }
}
