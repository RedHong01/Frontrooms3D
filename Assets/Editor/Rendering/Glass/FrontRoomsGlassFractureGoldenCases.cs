// FrontRoomsGlassFractureGoldenCases.cs -- GD3 step 3: the 20 golden cases of crack_graph_ref.py golden_cases(), in C#.
// Pure C# (no UnityEditor), so the same list drives the Unity suite and the standalone Mono harness.
// The golden FILES are the Python reference's output, committed in Tools/Blender/glass/golden/ (index.json holds their
// sha256). The port must reproduce them: byte for byte where it can, every vertex within 0.1 mm at the least.
using System.Collections.Generic;

public static class FrontRoomsGlassFractureGoldenCases
{
    public struct Case
    {
        public int index;
        public string profile;
        public uint seed;
        public double rootX, rootY;
        public int side;
        public string bandMode;
        public double rotationDeg;      // NaN = the map's default (MapHash.Unit(seed) * 360)
        public string what;
    }

    // build/01_setup.md sec 2.3 lab hits, window-root metres
    public static readonly double[] H1 = { 0.0, 1.62 }, H2 = { 0.40, 0.90 }, H3 = { -0.4835, 1.30 };
    static readonly double[] GridX = { -0.40, 0.0, 0.40 };
    static readonly double[] GridY = { 0.90, 1.30, 1.65 };   // 06w sec 4.4 bake centres

    /// <summary>lab_rotation(seed) = (seed % 3600) * 0.1 (seed 4242 -> 64.2 deg, 1990 -> 199.0 deg).</summary>
    public static double LabRotation(uint seed) => (seed % 3600) * 0.1;

    public static List<Case> All()
    {
        var cases = new List<Case>();
        var i = 0;
        foreach (var y in GridY)
            foreach (var x in GridX)
            {
                var seed = (uint)(GlassRng.MixKey(20261003, i) & 0xFFFFFFFFUL);
                cases.Add(new Case { profile = "Annealed6", seed = seed, rootX = x, rootY = y, side = 1, bandMode = "band", rotationDeg = double.NaN, what = "bake centre" });
                i++;
            }
        foreach (var hit in new[] { H1, H2, H3 })
            cases.Add(new Case { profile = "Annealed6", seed = 4242, rootX = hit[0], rootY = hit[1], side = 1, bandMode = "band", rotationDeg = LabRotation(4242), what = "lab hit" });
        cases.Add(new Case { profile = "Annealed6", seed = 1990, rootX = H1[0], rootY = H1[1], side = -1, bandMode = "band", rotationDeg = LabRotation(1990), what = "struck from cell b" });
        cases.Add(new Case { profile = "Annealed6", seed = 7, rootX = 0.60, rootY = 0.40, side = 1, bandMode = "band", rotationDeg = 123.4, what = "clamp corner" });
        cases.Add(new Case { profile = "Annealed6", seed = 8, rootX = -0.60, rootY = 1.95, side = 1, bandMode = "band", rotationDeg = 301.7, what = "clamp corner" });
        cases.Add(new Case { profile = "Annealed6_WebGL", seed = 4242, rootX = H1[0], rootY = H1[1], side = 1, bandMode = "band", rotationDeg = LabRotation(4242), what = "WebGL tier" });
        cases.Add(new Case { profile = "Annealed6_WebGL", seed = 4242, rootX = H2[0], rootY = H2[1], side = 1, bandMode = "band", rotationDeg = LabRotation(4242), what = "WebGL tier" });
        cases.Add(new Case { profile = "Annealed6_Cinematic", seed = 4242, rootX = H1[0], rootY = H1[1], side = 1, bandMode = "band", rotationDeg = LabRotation(4242), what = "Cinematic tier" });
        cases.Add(new Case { profile = "Annealed6", seed = 4242, rootX = H2[0], rootY = H2[1], side = 1, bandMode = "stop", rotationDeg = LabRotation(4242), what = "stop-line fallback" });
        cases.Add(new Case { profile = "Tempered6", seed = 4242, rootX = H1[0], rootY = H1[1], side = 1, bandMode = "band", rotationDeg = LabRotation(4242), what = "tempered (parked)" });
        for (var k = 0; k < cases.Count; k++)
        {
            var c = cases[k];
            c.index = k;
            cases[k] = c;
        }
        return cases;
    }

    /// <summary>The golden file name of case i (crack_graph_ref.py case_name).</summary>
    public static string FileName(Case c)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        return "g" + c.index.ToString("00", inv) + "_" + c.profile + "_s" + c.seed.ToString(inv) +
               "_x" + (c.rootX >= 0 ? "+" : "-") + FrontRoomsGlassFracturePattern.FormatFixed(System.Math.Abs(c.rootX), 4) +
               "_y" + FrontRoomsGlassFracturePattern.FormatFixed(c.rootY, 4) +
               "_side" + (c.side >= 0 ? "+" : "-") + System.Math.Abs(c.side).ToString(inv) + "_" + c.bandMode + ".json";
    }

    public static FrontRoomsGlassFracture Run(Case c, FrontRoomsGlassFractureJob job, FrontRoomsGlassFracture result)
    {
        var p = FrontRoomsGlassProfileData.BuiltinShared(c.profile);
        FrontRoomsGlassFracturePattern.BeginRoot(job, p, c.seed, c.rootX, c.rootY, FrontRoomsGlassFracturePattern.SlabW,
            FrontRoomsGlassFracturePattern.SlabH, c.rotationDeg, c.side, c.bandMode, result);
        while (!job.Step())
        {
        }
        return job.Result;
    }
}
