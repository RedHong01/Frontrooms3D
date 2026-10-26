// D1 (c) gate test (Red 2026-10-22 12:36, Documentation/research/glass/rt/NOTE_1022_red_decisions.md), EditMode: no Play
// Mode, no scene, no PlayerPrefs write. Checks FrontRoomsGlassRTSystem's default rule:
//   - forced core counts 8 / 10 / 14 / 16 / 40 / unknown at quality levels 0-5: High only at level >= 3 with >= 16 cores;
//   - the explicit switches (-frGlassRT, the FrontRooms.GlassRT setting) win at any core count; Ultra is never a default;
//   - the -frGlassRTCores argument parser; the real IORegistry read against /usr/sbin/ioreg; the fail-closed paths.
// Batch: Unity -batchmode -nographics -projectPath <proj> -executeMethod FrontRoomsGlassRTCoreGateTest.RunBatch
//        [-frGateOut <file>] -logFile <f>     (exit 0 = all PASS, 1 = a FAIL)
#if UNITY_EDITOR_OSX
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Q = FrontRoomsGlassRTQuality;

public static class FrontRoomsGlassRTCoreGateTest
{
    public static void RunBatch()
    {
        var ok = false;
        var report = "";
        try { ok = Run(out report); }
        catch (Exception e) { report += "\nEXCEPTION " + e; }
        var args = Environment.GetCommandLineArgs();
        string output = null;
        for (var i = 0; i + 1 < args.Length; i++) if (args[i] == "-frGateOut") output = args[i + 1];
        if (string.IsNullOrEmpty(output))
            output = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "rt_gate", "gate_test.txt");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, report.Replace("\r\n", "\n"));
        }
        catch (Exception e) { Debug.LogError("[GlassRTGateTest] could not write " + output + ": " + e.Message); }
        Debug.Log("[GlassRTGateTest] " + (ok ? "ALL PASS" : "FAIL") + " -> " + output + "\n" + report);
        EditorApplication.Exit(ok ? 0 : 1);
    }

    static int pass, fail;
    static StringBuilder log;

    static void Check(string what, string got, string want)
    {
        var ok = got == want;
        if (ok) pass++; else fail++;
        log.AppendLine((ok ? "PASS  " : "FAIL  ") + what + " -> " + got + (ok ? "" : " (want " + want + ")"));
    }

    public static bool Run(out string report)
    {
        pass = fail = 0;
        log = new StringBuilder();
        log.AppendLine("D1 (c) gate test, " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ", min cores " + FrontRoomsGlassRTSystem.DefaultHighMinCores);
        var none = new string[0];
        var counts = new[] { 8, 10, 14, 16, 40, -1 };

        // 1. the pure rule: forced counts x quality levels 0-5, no explicit switch
        log.AppendLine("== 1. DefaultFor(no -frGlassRT, no setting, level, cores)");
        for (var level = 0; level <= 5; level++)
            foreach (var c in counts)
            {
                var want = level >= 3 && c >= 16 ? Q.High : Q.Off;
                Check("level " + level + " cores " + Name(c), FrontRoomsGlassRTSystem.DefaultFor(none, "", level, c).ToString(), want.ToString());
            }

        // 2. the explicit switches win at any core count; Ultra only when asked for
        log.AppendLine("== 2. explicit switches");
        Check("-frGlassRT high, level 5, cores 8", FrontRoomsGlassRTSystem.DefaultFor(new[] { "x", "-frGlassRT", "high" }, "", 5, 8).ToString(), "High");
        Check("-frGlassRT high, level 0, cores unknown", FrontRoomsGlassRTSystem.DefaultFor(new[] { "-frGlassRT", "high" }, "", 0, -1).ToString(), "High");
        Check("-frGlassRT ultra, level 3, cores 10", FrontRoomsGlassRTSystem.DefaultFor(new[] { "-frGlassRT", "ultra" }, "", 3, 10).ToString(), "Ultra");
        Check("-frGlassRT off, level 5, cores 40", FrontRoomsGlassRTSystem.DefaultFor(new[] { "-frGlassRT", "off" }, "", 5, 40).ToString(), "Off");
        Check("setting high, level 5, cores 8", FrontRoomsGlassRTSystem.DefaultFor(none, "high", 5, 8).ToString(), "High");
        Check("setting ultra, level 5, cores 14", FrontRoomsGlassRTSystem.DefaultFor(none, "ultra", 5, 14).ToString(), "Ultra");
        Check("setting off, level 5, cores 40", FrontRoomsGlassRTSystem.DefaultFor(none, "off", 5, 40).ToString(), "Off");
        Check("-frGlassRT high beats setting off, cores 8", FrontRoomsGlassRTSystem.DefaultFor(new[] { "-frGlassRT", "high" }, "off", 5, 8).ToString(), "High");
        Check("bad -frGlassRT value, level 5, cores 8", FrontRoomsGlassRTSystem.DefaultFor(new[] { "-frGlassRT", "max" }, "", 5, 8).ToString(), "Off");
        Check("no switch, level 5, cores 40 (never Ultra)", FrontRoomsGlassRTSystem.DefaultFor(none, "", 5, 40).ToString(), "High");

        // 3. the -frGlassRTCores parser
        log.AppendLine("== 3. -frGlassRTCores");
        Check("-frGlassRTCores 8", Parse(new[] { "-frGlassRTCores", "8" }), "given 8");
        Check("-frGlassRTCores 40", Parse(new[] { "a", "-frGlassRTCores", "40", "b" }), "given 40");
        Check("-frGlassRTCores unknown", Parse(new[] { "-frGlassRTCores", "unknown" }), "given unknown");
        Check("-frGlassRTCores 0", Parse(new[] { "-frGlassRTCores", "0" }), "given unknown");
        Check("-frGlassRTCores -3", Parse(new[] { "-frGlassRTCores", "-3" }), "given unknown");
        Check("-frGlassRTCores (no value)", Parse(new[] { "-frGlassRTCores" }), "absent");
        Check("no argument", Parse(none), "absent");
        Check("null args", Parse(null), "absent");

        // 4. the full path: DefaultQuality() with the static override (reads this editor's real command line and setting)
        log.AppendLine("== 4. DefaultQuality() with HarnessCoreCount, QualitySettings level 0-5");
        var editorArgs = Environment.GetCommandLineArgs();
        var pref = "";
        try { pref = PlayerPrefs.GetString("FrontRooms.GlassRT", ""); } catch { }
        var explicitHere = Array.IndexOf(editorArgs, "-frGlassRT") >= 0 || pref.Trim().Length > 0;
        var levels = QualitySettings.names.Length;
        var savedLevel = QualitySettings.GetQualityLevel();
        var savedHarness = FrontRoomsGlassRTSystem.HarnessCoreCount;
        log.AppendLine("quality levels " + levels + " (" + string.Join(", ", QualitySettings.names) + "), current " + savedLevel
                       + ", setting FrontRooms.GlassRT='" + pref + "', -frGlassRT on this command line: " + (Array.IndexOf(editorArgs, "-frGlassRT") >= 0));
        if (explicitHere) log.AppendLine("SKIP  section 4: an explicit switch is set in this editor (section 1 covers the rule)");
        else
        {
            try
            {
                for (var level = 0; level <= 5 && level < levels; level++)
                {
                    QualitySettings.SetQualityLevel(level, false);
                    foreach (var c in counts)
                    {
                        FrontRoomsGlassRTSystem.HarnessCoreCount = c;
                        var want = level >= 3 && c >= 16 ? Q.High : Q.Off;
                        var got = FrontRoomsGlassRTSystem.DefaultQuality();
                        FrontRoomsGlassRTSystem.GpuCoreCount(out var src);
                        Check("level " + level + " forced " + Name(c) + " (source " + src + ")", got.ToString(), want.ToString());
                    }
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(savedLevel, false);
                FrontRoomsGlassRTSystem.HarnessCoreCount = savedHarness;
            }
        }

        // 5. the real IORegistry read on this Mac vs /usr/sbin/ioreg
        log.AppendLine("== 5. IORegistry read on this Mac");
        var sw = Stopwatch.StartNew();
        var real = FrontRoomsGlassRTSystem.ReadIORegistryCores(FrontRoomsGlassRTSystem.AgxClass, FrontRoomsGlassRTSystem.CoreCountKey, out var note);
        var us = sw.Elapsed.TotalMilliseconds * 1000.0;
        var ioreg = IoregCores();
        log.AppendLine("ReadIORegistryCores = " + Name(real) + (note.Length > 0 ? " (" + note + ")" : "") + ", " + us.ToString("0") + " us (UNDER LOAD); ioreg says " + Name(ioreg));
        Check("C# read matches ioreg", Name(real), Name(ioreg));
        var hasArg = FrontRoomsGlassRTSystem.TryCoresArgument(editorArgs, out _);
        if (!hasArg)
        {
            FrontRoomsGlassRTSystem.HarnessCoreCount = null;
            var gc = FrontRoomsGlassRTSystem.GpuCoreCount(out var src);
            Check("GpuCoreCount() without override", Name(gc) + " source=" + src, Name(ioreg) + " source=" + (ioreg > 0 ? "IORegistry" : "none"));
            FrontRoomsGlassRTSystem.HarnessCoreCount = savedHarness;
        }
        log.AppendLine("this Mac's default at level 3-5: " + FrontRoomsGlassRTSystem.GatedDefault(5, real) + " (" + SystemInfo.graphicsDeviceName + ", " + SystemInfo.processorType + ")");

        // 6. fail-closed: a missing service, a missing property, a property that is not a number
        log.AppendLine("== 6. fail-closed reads (must be unknown, never throw)");
        Check("class FRNoSuchAccelerator", Name(FrontRoomsGlassRTSystem.ReadIORegistryCores("FRNoSuchAccelerator", FrontRoomsGlassRTSystem.CoreCountKey, out var n1)) + " (" + n1 + ")", "unknown (no FRNoSuchAccelerator service (not an Apple GPU))");
        Check("key fr-no-such-key", Name(FrontRoomsGlassRTSystem.ReadIORegistryCores(FrontRoomsGlassRTSystem.AgxClass, "fr-no-such-key", out var n2)) + " (" + n2 + ")", "unknown (no fr-no-such-key property)");
        Check("key IOClass (a string)", Name(FrontRoomsGlassRTSystem.ReadIORegistryCores(FrontRoomsGlassRTSystem.AgxClass, "IOClass", out var n3)) + " (" + n3 + ")", "unknown (IOClass is not a number)");

        log.AppendLine("== RESULT " + (fail == 0 ? "ALL PASS" : "FAIL") + ": " + pass + " pass, " + fail + " fail");
        report = log.ToString();
        return fail == 0;
    }

    static string Name(int c) { return c > 0 ? c.ToString() : "unknown"; }

    static string Parse(string[] a)
    {
        return FrontRoomsGlassRTSystem.TryCoresArgument(a, out var c) ? "given " + Name(c) : "absent";
    }

    /// <summary>gpu-core-count as /usr/sbin/ioreg prints it (the reference for the C# read); -1 if none.</summary>
    static int IoregCores()
    {
        try
        {
            var psi = new ProcessStartInfo("/usr/sbin/ioreg", "-rc AGXAccelerator -d1 -l") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            using (var p = Process.Start(psi))
            {
                var text = p.StandardOutput.ReadToEnd();
                p.WaitForExit(10000);
                var m = Regex.Match(text, "\"gpu-core-count\" = (\\d+)");
                return m.Success ? int.Parse(m.Groups[1].Value) : -1;
            }
        }
        catch { return -1; }
    }
}
#endif
