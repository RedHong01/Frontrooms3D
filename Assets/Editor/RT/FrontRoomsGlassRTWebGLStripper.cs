// Builds for targets that can never trace (design 10 sec. 2.1 G-4, sec. 2.5): removes every variant of
// Hidden/FrontRooms/GlassRTPrepass and the FRGlassRTPrepass pass of FrontRooms/Glass. Only the desktop macOS renderer
// feature ever draws them, so with them stripped the WebGL shader variant count and output are exactly what they were
// before G14.
//   WebGL: prepass shader + prepass pass (the glass track's FrontRoomsGlassRTStripper removes the _FR_GLASS_RT
//          keyword variants of the ForwardLit pass there).
//   iOS, Android, tvOS, visionOS, Linux (2026-10-07): the same, plus the _FR_GLASS_RT keyword variants of FrontRooms/Glass.
//          The RT feature's body is compiled only for UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX, so these players never
//          enable the keyword and never draw the pass: no pixel changes, only fewer variants in the build.
//   macOS standalone: everything kept (the RT tier). Windows standalone: kept as today (inert; the P3 DXR path will
//          draw the same pass). The editor is unchanged.
// The class name is kept (the WebGL plan's pre-flight checks name it).
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
    public const string RTKeyword = "_FR_GLASS_RT";
    public int callbackOrder => 0;
    internal static int Removed, Seen;

    /// <summary>Targets with no ray-traced glass and none planned: WebGL, mobile, Linux.</summary>
    public static bool NeverTraces(BuildTarget t)
        => t == BuildTarget.WebGL || t == BuildTarget.iOS || t == BuildTarget.Android || t == BuildTarget.tvOS
        || t == BuildTarget.VisionOS || t == BuildTarget.StandaloneLinux64;

    // The build callback asks exactly the two decision functions the isolation check (P0-A10) tests: WouldStrip for whole
    // passes, WouldStripKeyword for the _FR_GLASS_RT variants (review 1 engineering #10: it used to repeat their logic).
    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        if (shader == null) return;
        var keyword = new ShaderKeyword(RTKeyword);
        Removed += Strip(EditorUserBuildSettings.activeBuildTarget, shader.name, snippet.passName, data, d => d.shaderKeywordSet.IsEnabled(keyword));
    }

    /// <summary>The whole stripping step on one snippet's variants: every variant of a stripped pass, else the variants for
    /// which hasRTKeyword is true where WouldStripKeyword holds. Returns how many it removed.</summary>
    public static int Strip<T>(BuildTarget target, string shaderName, string passName, IList<T> variants, System.Func<T, bool> hasRTKeyword)
    {
        if (variants == null) return 0;
        if (WouldStrip(target, shaderName, passName))
        {
            var n = variants.Count;
            Seen += n;
            variants.Clear();
            return n;
        }
        // WebGL: the glass track's stripper owns the keyword variants there (WouldStripKeyword is false)
        if (shaderName != GlassShader || !WouldStripKeyword(target)) return 0;
        var removed = 0;
        for (var i = variants.Count - 1; i >= 0; i--)
            if (hasRTKeyword(variants[i])) { variants.RemoveAt(i); removed++; }
        return removed;
    }

    /// <summary>The decision the stripper makes for a shader / pass on a given target (verify harness, P0-A10).</summary>
    public static bool WouldStrip(BuildTarget target, string shaderName, string passName)
        => NeverTraces(target) && (shaderName == PrepassShader || (shaderName == GlassShader && passName == GlassPrepassPass));

    /// <summary>Whether this stripper removes _FR_GLASS_RT variants of FrontRooms/Glass on a target (WebGL: the glass
    /// track's FrontRoomsGlassRTStripper does).</summary>
    public static bool WouldStripKeyword(BuildTarget target) => NeverTraces(target) && target != BuildTarget.WebGL;
}
