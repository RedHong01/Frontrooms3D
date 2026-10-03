using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Two checks that need no player build (Verification/glass/compile_check.txt):
///  1. The FrontRoomsRenderSetup hook: its old URP Lit glass pass, then FrontRoomsGlassSetup,
///     must leave Prop_Glass / Prop_BottleBlue on FrontRooms/Glass.
///  2. FrontRooms/Glass and Hidden/FrontRooms/ReflectionBlend compiled offline for WebGL 2
///     (GLES3x compiler, BuildTarget.WebGL) and Metal, for representative keyword sets.
/// Batch: -executeMethod FrontRoomsGlassCompileCheck.RunBatch -quit
/// </summary>
public static class FrontRoomsGlassCompileCheck
{
    public static void RunBatch()
    {
        var log = new StringBuilder();
        var old = typeof(FrontRoomsRenderSetup).GetMethod("EnsureGlassMaterials", BindingFlags.Static | BindingFlags.NonPublic);
        if (old != null) old.Invoke(null, null);
        log.AppendLine("after the old URP Lit pass: Prop_Glass = " + ShaderOf("Prop_Glass"));
        FrontRoomsGlassSetup.EnsureAll();
        AssetDatabase.SaveAssets();
        foreach (var n in new[] { "Prop_Glass", "Prop_BottleBlue", "Glass_Window", "Glass_Edge", "Glass_Shard" })
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(FrontRoomsGlassSetup.SurfaceDir + "/" + n + ".mat");
            log.AppendLine("after the hook: " + n + " = " + ShaderOf(n) + ", keywords [" + (m != null ? string.Join(" ", m.shaderKeywords) : "") + "], queue " + (m != null ? m.renderQueue : 0));
        }

        var sets = new[]
        {
            new string[0],
            new[] { "_FR_GLASS_GRIME" },
            new[] { "_FR_GLASS_GRIME", "_MAIN_LIGHT_SHADOWS", "_ADDITIONAL_LIGHTS", "_ADDITIONAL_LIGHT_SHADOWS", "_SHADOWS_SOFT", "_CLUSTER_LIGHT_LOOP", "_LIGHT_COOKIES", "FOG_EXP2" },
            new[] { "_FR_GLASS_GRIME", "_ADDITIONAL_LIGHTS", "_SHADOWS_SOFT_MEDIUM", "_CLUSTER_LIGHT_LOOP", "FOG_EXP2", "INSTANCING_ON" },
            // Fix pass: the G14 hook (desktop only; stripped from WebGL by FrontRoomsGlassRTStripper) and G7's probe set.
            new[] { "_FR_GLASS_GRIME", "_FR_GLASS_RT", "_ADDITIONAL_LIGHTS", "_CLUSTER_LIGHT_LOOP", "FOG_EXP2" },
            new[] { "_FR_GLASS_GRIME", "_REFLECTION_PROBE_BLENDING", "_REFLECTION_PROBE_BOX_PROJECTION", "_ADDITIONAL_LIGHTS", "_CLUSTER_LIGHT_LOOP", "FOG_EXP2" },
        };
        foreach (var shaderName in new[] { FrontRoomsGlassSetup.GlassShaderName, "Hidden/FrontRooms/ReflectionBlend" })
        {
            var shader = Shader.Find(shaderName);
            if (shader == null) { log.AppendLine(shaderName + ": NOT FOUND"); continue; }
            // Pick our own subshader explicitly: ActiveSubshader can be the FallBack in batch mode.
            var data = ShaderUtil.GetShaderData(shader);
            ShaderData.Pass pass = null;
            for (var si = 0; si < data.SubshaderCount && pass == null; si++)
            {
                var sub = data.GetSubshader(si);
                for (var pi = 0; pi < sub.PassCount; pi++)
                    if (sub.GetPass(pi).Name == "ForwardLit" || shaderName.StartsWith("Hidden/FrontRooms")) { pass = sub.GetPass(pi); log.AppendLine(shaderName + ": subshader " + si + " of " + data.SubshaderCount + " (active " + data.ActiveSubshaderIndex + "), pass '" + pass.Name + "'"); break; }
            }
            if (pass == null) { log.AppendLine(shaderName + ": no ForwardLit pass"); continue; }
            foreach (var platform in new[] { UnityEditor.Rendering.ShaderCompilerPlatform.GLES3x, UnityEditor.Rendering.ShaderCompilerPlatform.Metal })
            {
                var target = platform == UnityEditor.Rendering.ShaderCompilerPlatform.GLES3x ? BuildTarget.WebGL : BuildTarget.StandaloneOSX;
                foreach (var keywords in shaderName.StartsWith("Hidden") ? new[] { new string[0] } : sets)
                foreach (var stage in new[] { UnityEditor.Rendering.ShaderType.Vertex, UnityEditor.Rendering.ShaderType.Fragment })
                {
                    var info = pass.CompileVariant(stage, keywords, platform, target);
                    var errors = info.Messages.Where(x => x.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).Select(x => x.message + " (line " + x.line + ")").ToArray();
                    log.AppendLine(shaderName + " " + platform + " " + stage + " [" + string.Join(" ", keywords) + "]: " + (info.Success ? "OK" : "FAILED") + ", "
                        + (info.ShaderData != null ? info.ShaderData.Length : 0) + " bytes, textures [" + string.Join(" ", info.TextureBindings.Select(t => t.Name)) + "], cbuffers ["
                        + string.Join(" ", info.ConstantBuffers.Select(c => c.Name + ":" + c.Size)) + "], messages " + info.Messages.Length
                        + (errors.Length > 0 ? " ERRORS: " + string.Join(" | ", errors) : ""));
                }
            }
        }
        var dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "glass");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "compile_check.txt"), log.ToString());
        Debug.Log("[GlassCompileCheck]\n" + log);
    }

    static string ShaderOf(string name)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(FrontRoomsGlassSetup.SurfaceDir + "/" + name + ".mat");
        return m != null && m.shader != null ? m.shader.name : "missing";
    }
}
