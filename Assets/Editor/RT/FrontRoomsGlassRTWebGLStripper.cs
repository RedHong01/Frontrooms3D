// WebGL builds only (design 10 §2.1 G-4, §2.5): removes every variant of Hidden/FrontRooms/GlassRTPrepass and the
// FRGlassRTPrepass pass of FrontRooms/Glass. The WebGL tier has no ray tracing, so neither is ever drawn there; with
// them stripped the WebGL shader variant count and output are exactly what they were before G14. (The glass track's
// FrontRoomsGlassRTStripper removes the _FR_GLASS_RT keyword variants of the ForwardLit pass.) Desktop builds and the
// editor are unchanged.
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class FrontRoomsGlassRTWebGLStripper : IPreprocessShaders
{
    public const string PrepassShader = "Hidden/FrontRooms/GlassRTPrepass";
    public const string GlassShader = "FrontRooms/Glass";
    public const string GlassPrepassPass = "FRGlassRTPrepass";
    public int callbackOrder => 0;
    internal static int Removed, Seen;

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL || shader == null) return;
        var strip = shader.name == PrepassShader || (shader.name == GlassShader && snippet.passName == GlassPrepassPass);
        if (!strip) return;
        Seen += data.Count;
        Removed += data.Count;
        data.Clear();
    }

    /// <summary>The decision the stripper makes for a shader / pass on a given target (verify harness, P0-A10).</summary>
    public static bool WouldStrip(BuildTarget target, string shaderName, string passName)
        => target == BuildTarget.WebGL && (shaderName == PrepassShader || (shaderName == GlassShader && passName == GlassPrepassPass));
}
