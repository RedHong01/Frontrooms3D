// FrontRoomsGlassFractureTools.cs -- GD3 step 3: editor tools around the C# crack-graph generator.
//   FrontRooms/Glass/Write Profile Assets        -> Assets/Resources/Rendering/GlassProfiles/<profile>.asset (5 built-ins)
//   FrontRooms/Glass/Bake Fracture Variants      -> Verification/glass_fracture/bake/<profile>/*.json (plan sec 4.1: the
//                                                   18-variant fallback per profile = 3 x 3 bake centres x 2 seeds; written
//                                                   OUTSIDE Assets: ship them only if the WebGL build-time measurement fails)
// Headless: -executeMethod FrontRoomsGlassFractureTools.WriteProfileAssetsBatch -quit
//           -executeMethod FrontRoomsGlassFractureTools.BakeVariantsBatch -quit
//           -executeMethod FrontRoomsGlassFractureTools.AllBatch -quit   (assets + bake + FrontRoomsGlassFractureTests)
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class FrontRoomsGlassFractureTools
{
    const string AssetFolder = "Assets/Resources/" + FrontRoomsGlassTypeProfile.ResourceFolder;

    [MenuItem("FrontRooms/Glass/Write Profile Assets")]
    public static void WriteProfileAssets()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Rendering"))
            AssetDatabase.CreateFolder("Assets/Resources", "Rendering");
        if (!AssetDatabase.IsValidFolder(AssetFolder))
            AssetDatabase.CreateFolder("Assets/Resources/Rendering", "GlassProfiles");
        var made = new List<string>();
        foreach (var name in FrontRoomsGlassProfileData.BuiltinNames)
        {
            var path = AssetFolder + "/" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<FrontRoomsGlassTypeProfile>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<FrontRoomsGlassTypeProfile>();
                asset.data = FrontRoomsGlassProfileData.Builtin(name);
                AssetDatabase.CreateAsset(asset, path);
                made.Add(path + " (new)");
            }
            else
            {
                asset.data = FrontRoomsGlassProfileData.Builtin(name);
                EditorUtility.SetDirty(asset);
                made.Add(path + " (updated)");
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[GlassFractureTools] profile assets:\n" + string.Join("\n", made.ToArray()));
    }

    public static void WriteProfileAssetsBatch() => WriteProfileAssets();

    /// <summary>Plan sec 4.1 fallback: 3 x 3 bake centres (06w sec 4.4) x 2 seeds per profile, with the map's default rotation.</summary>
    public static uint BakeSeed(int centre, int k) => (uint)(GlassRng.MixKey(19900101 + k, centre) & 0xFFFFFFFFUL);

    [MenuItem("FrontRooms/Glass/Bake Fracture Variants")]
    public static void BakeVariants()
    {
        var root = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "glass_fracture", "bake");
        var job = new FrontRoomsGlassFractureJob();
        var res = new FrontRoomsGlassFracture();
        var log = new StringBuilder("profile\tcentre\tk\tseed\tx\ty\tpieces\tbodies\tteeth\ttris_stage2\tvalid\tfile\n");
        var invalid = 0;
        foreach (var prof in new[] { "Annealed6", "Annealed6_Cinematic", "Annealed6_WebGL", "Wired6" })
        {
            var dir = Path.Combine(root, prof);
            Directory.CreateDirectory(dir);
            var P = FrontRoomsGlassProfileData.BuiltinShared(prof);
            var centre = 0;
            foreach (var y in FrontRoomsGlassFracturePattern.BakeY)
                foreach (var x in FrontRoomsGlassFracturePattern.BakeX)
                {
                    for (var k = 0; k < 2; k++)
                    {
                        var seed = BakeSeed(centre, k);
                        FrontRoomsGlassFracturePattern.BeginRoot(job, P, seed, x, y, FrontRoomsGlassFracturePattern.SlabW, FrontRoomsGlassFracturePattern.SlabH,
                            double.NaN, 1, "band", res);
                        while (!job.Step())
                        {
                        }
                        var file = string.Format(System.Globalization.CultureInfo.InvariantCulture, "bake_c{0}_k{1}_s{2}.json", centre, k, seed);
                        File.WriteAllText(Path.Combine(dir, file), FrontRoomsGlassFracturePattern.ToJson(res));
                        if (!job.Validation.AllOk)
                            invalid++;
                        log.Append(prof).Append('\t').Append(centre).Append('\t').Append(k).Append('\t').Append(seed).Append('\t').Append(x).Append('\t').Append(y)
                            .Append('\t').Append(res.stats.piecesTotal).Append('\t').Append(res.stats.bodies).Append('\t').Append(res.stats.tooth).Append('\t')
                            .Append(res.stats.trisStage2).Append('\t').Append(job.Validation.AllOk).Append('\t').Append(file).Append('\n');
                    }
                    centre++;
                }
        }
        File.WriteAllText(Path.Combine(root, "bake_index.tsv"), log.ToString());
        Debug.Log("[GlassFractureTools] baked 4 x 18 variants into " + root + ", invalid " + invalid);
    }

    public static void BakeVariantsBatch() => BakeVariants();

    /// <summary>One headless run for step 3: write the profile assets, bake the fallback variants, run the suite.</summary>
    public static void AllBatch()
    {
        WriteProfileAssets();
        BakeVariants();
        FrontRoomsGlassFractureTests.RunBatch();
    }
}
