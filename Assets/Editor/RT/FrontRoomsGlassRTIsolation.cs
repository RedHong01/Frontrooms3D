// P0-A10 isolation check (design 10 sec. 2.5): compiles the PLAYER scripts for WebGL, Windows 64 and macOS (no build, no
// target switch) and reports which compiled assemblies contain FRGlassRT_ P/Invoke imports or the RT system types.
// Also reads back the plugin importer and asks the WebGL stripper what it would remove.
// Batch: Unity -batchmode -projectPath <proj> -executeMethod FrontRoomsGlassRTIsolation.RunBatch -logFile <f>
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;

public static class FrontRoomsGlassRTIsolation
{
    public static void RunBatch()
    {
        var log = new StringBuilder();
        var root = Directory.GetParent(Application.dataPath).FullName;
        // fix round 32 (review 2 engineering #3): parallel arrays instead of a tuple list (clone editor scripts stay tuple-free)
        var targets = new[] { BuildTarget.WebGL, BuildTarget.iOS, BuildTarget.StandaloneWindows64, BuildTarget.StandaloneOSX };
        var groups = new[] { BuildTargetGroup.WebGL, BuildTargetGroup.iOS, BuildTargetGroup.Standalone, BuildTargetGroup.Standalone };
        for (var ti = 0; ti < targets.Length; ti++)
        {
            var target = targets[ti]; var group = groups[ti];
            var outDir = Path.Combine(root, "Temp", "rt_isolation_" + target);
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            Directory.CreateDirectory(outDir);
            var settings = new ScriptCompilationSettings { target = target, group = group, options = ScriptCompilationOptions.None };
            var result = PlayerBuildInterface.CompilePlayerScripts(settings, outDir);
            var dlls = Directory.GetFiles(outDir, "*.dll");
            var main = dlls.FirstOrDefault(d => Path.GetFileName(d) == "Assembly-CSharp.dll");
            string Find(string needle)
            {
                if (main == null) return "no Assembly-CSharp.dll";
                var bytes = File.ReadAllBytes(main);
                var text = Encoding.ASCII.GetString(bytes);
                var n = 0; var at = 0;
                while ((at = text.IndexOf(needle, at, System.StringComparison.Ordinal)) >= 0) { n++; at += needle.Length; }
                return n.ToString();
            }
            log.AppendLine(target + ": compiled " + dlls.Length + " assemblies (" + (result.assemblies != null ? result.assemblies.Count : 0) + " reported) | Assembly-CSharp.dll "
                + (main != null ? new FileInfo(main).Length + " bytes" : "missing")
                + " | occurrences of \"FRGlassRT_\" " + Find("FRGlassRT_") + " | \"FrontRoomsMetalGlassRT\" (plugin name) " + Find("FrontRoomsMetalGlassRT\0")
                + " | \"FrontRoomsGlassRTSystem\" " + Find("FrontRoomsGlassRTSystem") + " | \"FrontRoomsGlassRTCamera\" " + Find("FrontRoomsGlassRTCamera")
                + " | \"FrontRoomsMetalGlassRTRendererFeature\" (class kept, body empty off-Mac) " + Find("FrontRoomsMetalGlassRTRendererFeature"));
        }
        var p = AssetImporter.GetAtPath(FrontRoomsGlassRTImporter.DylibPath) as PluginImporter;
        log.AppendLine("PLUGIN importer: " + (p != null ? FrontRoomsGlassRTImporter.Describe(p) : "missing"));
        log.AppendLine("STRIPPER on WebGL: Hidden/FrontRooms/GlassRTPrepass " + FrontRoomsGlassRTWebGLStripper.WouldStrip(BuildTarget.WebGL, FrontRoomsGlassRTWebGLStripper.PrepassShader, "FRGlassRTPrepass")
            + " | FrontRooms/Glass pass FRGlassRTPrepass " + FrontRoomsGlassRTWebGLStripper.WouldStrip(BuildTarget.WebGL, "FrontRooms/Glass", "FRGlassRTPrepass")
            + " | FrontRooms/Glass pass ForwardLit " + FrontRoomsGlassRTWebGLStripper.WouldStrip(BuildTarget.WebGL, "FrontRooms/Glass", "ForwardLit")
            + " | on StandaloneOSX: prepass shader " + FrontRoomsGlassRTWebGLStripper.WouldStrip(BuildTarget.StandaloneOSX, FrontRoomsGlassRTWebGLStripper.PrepassShader, "FRGlassRTPrepass")
            + " | on iOS: prepass shader " + FrontRoomsGlassRTWebGLStripper.WouldStrip(BuildTarget.iOS, FrontRoomsGlassRTWebGLStripper.PrepassShader, "FRGlassRTPrepass")
            + ", glass FRGlassRTPrepass " + FrontRoomsGlassRTWebGLStripper.WouldStrip(BuildTarget.iOS, "FrontRooms/Glass", "FRGlassRTPrepass")
            + ", _FR_GLASS_RT variants " + FrontRoomsGlassRTWebGLStripper.WouldStripKeyword(BuildTarget.iOS)
            + " | on StandaloneWindows64: prepass shader " + FrontRoomsGlassRTWebGLStripper.WouldStrip(BuildTarget.StandaloneWindows64, FrontRoomsGlassRTWebGLStripper.PrepassShader, "FRGlassRTPrepass"));
        // The stripping step itself (review 1 engineering #10), on stand-in variant lists through the same Strip() the build
        // callback runs: a variant is a string, "RT" marks one with _FR_GLASS_RT.
        string Run(BuildTarget t, string shaderName, string pass)
        {
            var v = new System.Collections.Generic.List<string> { "base", "RT", "fog", "fog RT" };
            var removed = FrontRoomsGlassRTWebGLStripper.Strip(t, shaderName, pass, v, x => x.EndsWith("RT", System.StringComparison.Ordinal));
            return removed + " of 4 removed (" + v.Count + " left)";
        }
        log.AppendLine("STRIP STEP: WebGL prepass shader " + Run(BuildTarget.WebGL, FrontRoomsGlassRTWebGLStripper.PrepassShader, "FRGlassRTPrepass")
            + " | WebGL glass FRGlassRTPrepass " + Run(BuildTarget.WebGL, FrontRoomsGlassRTWebGLStripper.GlassShader, "FRGlassRTPrepass")
            + " | WebGL glass ForwardLit " + Run(BuildTarget.WebGL, FrontRoomsGlassRTWebGLStripper.GlassShader, "ForwardLit")
            + " | iOS glass FRGlassRTPrepass " + Run(BuildTarget.iOS, FrontRoomsGlassRTWebGLStripper.GlassShader, "FRGlassRTPrepass")
            + " | iOS glass ForwardLit " + Run(BuildTarget.iOS, FrontRoomsGlassRTWebGLStripper.GlassShader, "ForwardLit")
            + " | macOS glass FRGlassRTPrepass " + Run(BuildTarget.StandaloneOSX, FrontRoomsGlassRTWebGLStripper.GlassShader, "FRGlassRTPrepass")
            + " | macOS glass ForwardLit " + Run(BuildTarget.StandaloneOSX, FrontRoomsGlassRTWebGLStripper.GlassShader, "ForwardLit")
            + " | Win64 prepass shader " + Run(BuildTarget.StandaloneWindows64, FrontRoomsGlassRTWebGLStripper.PrepassShader, "FRGlassRTPrepass"));
        Debug.Log("[FrontRoomsGlassRTIsolation]\n" + log);
        var outPath = Path.Combine(root, "Verification", "rt_p0p1");
        Directory.CreateDirectory(outPath);
        File.WriteAllText(Path.Combine(outPath, "isolation.txt"), log.ToString());
        EditorApplication.Exit(0);
    }
}
