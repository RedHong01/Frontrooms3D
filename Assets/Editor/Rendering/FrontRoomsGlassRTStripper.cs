using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// WebGL builds only: removes FrontRooms/Glass variants with the global keyword _FR_GLASS_RT
/// (the G14 ray-traced / planar reflection input), which the Web tier never enables (no RT, no
/// planar). Halves the glass shader's fragment variants in the WebGL build; desktop builds and
/// the editor are unchanged (WebGL-only rule, VISUAL_CHAT_TASKS §1b).
/// </summary>
public sealed class FrontRoomsGlassRTStripper : IPreprocessShaders
{
    public const string Keyword = "_FR_GLASS_RT";
    public int callbackOrder => 0;

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL) return;
        if (shader == null || shader.name != FrontRoomsGlassSetup.GlassShaderName) return;
        var keyword = new ShaderKeyword(Keyword);
        for (var i = data.Count - 1; i >= 0; i--)
            if (data[i].shaderKeywordSet.IsEnabled(keyword)) data.RemoveAt(i);
    }
}
