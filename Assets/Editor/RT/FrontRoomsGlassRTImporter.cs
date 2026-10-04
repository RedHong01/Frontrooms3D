// Pins the import settings of the G14 plugin and the feature's serialized shader (design 10 §2.5, R21, R22).
//   libFrontRoomsMetalGlassRT.dylib: Editor (OS OSX, CPU ARM64) + Standalone OSX (ARM64), every other platform off,
//   isPreloaded = true (UnityPluginLoad runs at start-up and sees the device-init event).
//   FrontRooms_URP_Renderer.asset: the FrontRoomsMetalGlassRTRendererFeature entry references
//   Hidden/FrontRooms/GlassRTPrepass, so player builds keep the shader.
// Batch: Unity -batchmode -projectPath <proj> -executeMethod FrontRoomsGlassRTImporter.ApplyBatch -quit -logFile <f>
// An AssetPostprocessor re-applies the plugin settings whenever the dylib is (re)imported, so a rebuilt dylib keeps them.
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class FrontRoomsGlassRTImporter : AssetPostprocessor
{
    public const string DylibPath = "Assets/Plugins/macOS/libFrontRoomsMetalGlassRT.dylib";
    public const string RendererPath = "Assets/Settings/FrontRooms_URP_Renderer.asset";
    public const string PrepassShaderPath = "Assets/Shaders/GlassRT/FrontRoomsGlassRTPrepass.shader";

    void OnPreprocessAsset()
    {
        if (assetPath != DylibPath) return;
        if (assetImporter is PluginImporter p) Configure(p);
    }

    static bool Configure(PluginImporter p)
    {
        var changed = false;
        void Set(bool cond, System.Action act) { if (!cond) { act(); changed = true; } }
        Set(!p.GetCompatibleWithAnyPlatform(), () => p.SetCompatibleWithAnyPlatform(false));
        Set(p.GetCompatibleWithEditor(), () => p.SetCompatibleWithEditor(true));
        Set(p.GetEditorData("OS") == "OSX", () => p.SetEditorData("OS", "OSX"));
        Set(p.GetEditorData("CPU") == "ARM64", () => p.SetEditorData("CPU", "ARM64"));
        Set(p.GetCompatibleWithPlatform(BuildTarget.StandaloneOSX), () => p.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, true));
        Set(p.GetPlatformData(BuildTarget.StandaloneOSX, "CPU") == "ARM64", () => p.SetPlatformData(BuildTarget.StandaloneOSX, "CPU", "ARM64"));
        foreach (var t in new[] { BuildTarget.StandaloneWindows, BuildTarget.StandaloneWindows64, BuildTarget.StandaloneLinux64, BuildTarget.WebGL, BuildTarget.iOS, BuildTarget.Android })
            Set(!p.GetCompatibleWithPlatform(t), () => p.SetCompatibleWithPlatform(t, false));
        Set(p.isPreloaded, () => p.isPreloaded = true);
        return changed;
    }

    public static string Describe(PluginImporter p)
    {
        var sb = new StringBuilder();
        sb.Append("any " + p.GetCompatibleWithAnyPlatform());
        sb.Append(" · editor " + p.GetCompatibleWithEditor() + " (OS " + p.GetEditorData("OS") + ", CPU " + p.GetEditorData("CPU") + ")");
        sb.Append(" · StandaloneOSX " + p.GetCompatibleWithPlatform(BuildTarget.StandaloneOSX) + " (CPU " + p.GetPlatformData(BuildTarget.StandaloneOSX, "CPU") + ")");
        foreach (var t in new[] { BuildTarget.StandaloneWindows64, BuildTarget.StandaloneLinux64, BuildTarget.WebGL, BuildTarget.iOS })
            sb.Append(" · " + t + " " + p.GetCompatibleWithPlatform(t));
        sb.Append(" · preloaded " + p.isPreloaded);
        return sb.ToString();
    }

    [MenuItem("FrontRooms/Rendering/G14 RT: pin plugin import + prepass shader")]
    public static void Apply()
    {
        var log = new StringBuilder();
        var p = AssetImporter.GetAtPath(DylibPath) as PluginImporter;
        if (p == null) log.AppendLine("PLUGIN missing at " + DylibPath);
        else
        {
            var changed = Configure(p);
            if (changed) { p.SaveAndReimport(); p = AssetImporter.GetAtPath(DylibPath) as PluginImporter; }
            log.AppendLine("PLUGIN " + (changed ? "updated" : "already pinned") + ": " + Describe(p));
        }
        var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(PrepassShaderPath);
        if (data == null || shader == null) log.AppendLine("RENDERER or SHADER missing: " + (data == null ? RendererPath : PrepassShaderPath));
        else
        {
            var feature = data.rendererFeatures.OfType<FrontRoomsMetalGlassRTRendererFeature>().FirstOrDefault();
            if (feature == null) log.AppendLine("RENDERER has no FrontRoomsMetalGlassRTRendererFeature");
            else
            {
                var so = new SerializedObject(feature);
                var prop = so.FindProperty("prepassShader");
                if (prop.objectReferenceValue != shader)
                {
                    prop.objectReferenceValue = shader;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(feature);
                    EditorUtility.SetDirty(data);
                    AssetDatabase.SaveAssets();
                    log.AppendLine("RENDERER feature prepassShader set to " + shader.name);
                }
                else log.AppendLine("RENDERER feature prepassShader already " + shader.name);
            }
        }
        Debug.Log("[FrontRoomsGlassRTImporter]\n" + log);
        var outDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "rt_p0p1");
        Directory.CreateDirectory(outDir);
        File.WriteAllText(Path.Combine(outDir, "importer.txt"), log.ToString());
    }

    public static void ApplyBatch()
    {
        Apply();
        EditorApplication.Exit(0);
    }
}
