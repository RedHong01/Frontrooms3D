// FrontRoomsGlassProfileData.cs -- GD3 step 3: the generator numbers of one glass type and tier (plan 10 sec 2.1, 4.3).
// Pure C#. The built-in profiles are a literal copy of crack_graph_ref.py PROFILES (version 1.3); the golden test
// (FrontRoomsGlassFractureTests) fails if any of these numbers drifts from the reference. The ScriptableObject wrapper
// (FrontRoomsGlassTypeProfile) lets a designer hold a copy as an asset; the tests check those assets too.
using System;
using System.Collections.Generic;

public enum GlassFractureMode
{
    /// <summary>Annealed and wired glass: radial + ring crack graph, stages S1/S2, teeth.</summary>
    Graph = 0,
    /// <summary>Tempered glass: no crack stages; the whole pane dices at the shatter (clumps of granules).</summary>
    Dice = 1,
}

[Serializable]
public sealed class FrontRoomsGlassProfileData
{
    public string name = "Annealed6";
    public string glass = "annealed";
    public string tier = "High";
    public GlassFractureMode mode = GlassFractureMode.Graph;
    public double thickness = 0.006;
    public double bevel = 0.0003;                       // 0.3 mm (build/02_pattern.md sec 4.4); WebGL 0

    public int radialsMin = 8, radialsMax = 12;
    public double radialAngleJitter = 0.30;              // fraction of the even spacing
    public double coreRadius = 0.010, coreJitter = 0.30, coreMax = 0.0145;
    public double ring1 = 0.030, ringGrowth = 1.55, ringGrowthJitter = 0.10, ringNodeJitter = 0.12;
    public double kinkDeg = 2.0, laneFrac = 0.40;
    public double kinkStrongP = 0.15, kinkStrongDegMin = 4.0, kinkStrongDegMax = 7.0;
    public int forkFromRing = 2;
    public double forkMinGap = 0.30, forkP = 0.30, forkChildFracMin = 0.35, forkChildFracMax = 0.60;
    public double forkParentKinkFracMin = 0.04, forkParentKinkFracMax = 0.12;
    public double forkMaxFracToStop = 0.80;
    public double forkWide = 0.16, forkWideP = 0.85;
    public double chordPInner = 0.97, chordPOuter = 0.45;
    public double tSep = 0.004, tSepFrac = 0.05;
    public double chordMinWedge = 0.012;
    public int stage1CountMin = 5, stage1CountMax = 8;
    public double stage1ReachMin = 0.25, stage1ReachMax = 0.35;
    public double daggerMax = 0.90, daggerSplitMin = 0.40, daggerSplitMax = 0.60, daggerSplitTiltDeg = 20.0;
    public double toothKeepL = 0.80, toothKeepR = 0.80, toothKeepT = 0.75, toothKeepB = 0.65;
    public double toothTriP = 0.65, toothMaxLen = 0.30;
    public double toothVisMin = 0.006, toothSplitTiltDeg = 25.0;
    public double cornerToothP = 0.85, cornerLegMin = 0.9, cornerLegMax = 1.9;
    public double minPieceArea = 1.0e-4, level2Area = 60.0e-4;
    public int level2MaxDepth = 2;
    public double tiltDegMin = 0.2, tiltDegMax = 0.8, pushMin = 0.0005, pushMax = 0.003, poseFalloff = 0.35, tiltAxisJitterDeg = 35.0;
    public double releaseInnerR = 0.15, releaseMidMsMin = 30.0, releaseMidMsMax = 120.0, releaseOuterMsMin = 120.0, releaseOuterMsMax = 300.0;
    public double releaseSpeedMin = 2.0, releaseSpeedMax = 4.0, releaseSpeedFalloff = 0.25;
    public int maxPieces = 150, maxBodies = 150;
    public double capMergeArea = 400.0e-4;
    public int chipsS1Min = 6, chipsS1Max = 12, chipsS2Min = 10, chipsS2Max = 20;
    public int budgetS2Tris = 10000, budgetTeethTris = 2000, budgetFinTris = 400, budgetCraterTris = 200;
    public int craterTrisPerVertex = 8;
    public double wireGrid = 0.0;
    public bool hangsOnWire = false;
    // dice mode (Tempered6)
    public double granule = 0.009, clump = 0.16, clumpJitter = 0.30;

    public double ToothKeep(char side)
    {
        switch (side)
        {
            case 'L': return toothKeepL;
            case 'R': return toothKeepR;
            case 'T': return toothKeepT;
            default: return toothKeepB;
        }
    }

    public FrontRoomsGlassProfileData Clone() => (FrontRoomsGlassProfileData)MemberwiseClone();

    // ---------------------------------------------------------------------------------------------------------
    // Built-in profiles = crack_graph_ref.py PROFILES (one per glass type and tier). Annealed6 = BUILD.
    // ---------------------------------------------------------------------------------------------------------
    public const string Annealed6 = "Annealed6";
    public const string Annealed6Cinematic = "Annealed6_Cinematic";
    public const string Annealed6WebGL = "Annealed6_WebGL";
    public const string Wired6 = "Wired6";
    public const string Tempered6 = "Tempered6";

    public static readonly string[] BuiltinNames = { Annealed6, Annealed6Cinematic, Annealed6WebGL, Wired6, Tempered6 };

    static Dictionary<string, FrontRoomsGlassProfileData> builtins;

    /// <summary>A fresh copy of a built-in profile, or null for an unknown name.</summary>
    public static FrontRoomsGlassProfileData Builtin(string name)
    {
        if (builtins == null)
        {
            var d = new Dictionary<string, FrontRoomsGlassProfileData>();
            foreach (var n in BuiltinNames)
                d[n] = MakeBuiltin(n);
            builtins = d;
        }
        FrontRoomsGlassProfileData p;
        return builtins.TryGetValue(name, out p) ? p.Clone() : null;
    }

    /// <summary>The shared read-only instance of a built-in profile (do not modify).</summary>
    public static FrontRoomsGlassProfileData BuiltinShared(string name)
    {
        if (builtins == null)
            Builtin(name);
        FrontRoomsGlassProfileData p;
        return builtins.TryGetValue(name, out p) ? p : null;
    }

    static FrontRoomsGlassProfileData MakeBuiltin(string name)
    {
        var p = new FrontRoomsGlassProfileData();   // _ANNEALED defaults
        p.name = name;
        switch (name)
        {
            case Annealed6:
                break;
            case Annealed6Cinematic:
                // Desktop Cinematic: same pattern, a little denser, <= 170 pieces
                p.tier = "Cinematic";
                p.forkP = 0.34;
                p.chordPOuter = 0.50;
                p.maxPieces = 170;
                p.maxBodies = 170;
                break;
            case Annealed6WebGL:
                // WebGL tier: about 60 pieces, no bevel, no level-2 re-break (plan sec 4.8)
                p.tier = "WebGL";
                p.bevel = 0.0;
                p.radialsMin = 6;
                p.radialsMax = 8;
                p.ring1 = 0.035;
                p.ringGrowth = 1.85;
                p.forkP = 0.22;
                p.forkMinGap = 0.40;
                p.chordPInner = 0.92;
                p.chordPOuter = 0.30;
                p.daggerMax = 0.95;
                p.toothKeepL = 0.60; p.toothKeepR = 0.60; p.toothKeepT = 0.55; p.toothKeepB = 0.45;
                p.toothMaxLen = 0.40;
                p.cornerToothP = 0.60;
                p.level2Area = 0.0;
                p.maxPieces = 64;
                p.maxBodies = 40;
                p.chipsS1Min = 4; p.chipsS1Max = 6; p.chipsS2Min = 6; p.chipsS2Max = 10;
                p.budgetS2Tris = 1500; p.budgetTeethTris = 400; p.budgetFinTris = 150; p.budgetCraterTris = 60;
                p.craterTrisPerVertex = 6;
                break;
            case Wired6:
                // PARKED, data only: cracks like annealed and hangs on its 12.5 mm wire
                p.glass = "wired";
                p.wireGrid = 0.0125;
                p.hangsOnWire = true;
                p.toothKeepL = 1.0; p.toothKeepR = 1.0; p.toothKeepT = 1.0; p.toothKeepB = 1.0;
                p.budgetTeethTris = 2600;
                break;
            case Tempered6:
                // PARKED, data only: no crack stages; the whole pane dices at 1.0 s
                p.glass = "tempered";
                p.mode = GlassFractureMode.Dice;
                p.granule = 0.009;
                p.clump = 0.16;
                p.clumpJitter = 0.30;
                p.toothKeepL = 0.0; p.toothKeepR = 0.0; p.toothKeepT = 0.0; p.toothKeepB = 0.0;
                p.cornerToothP = 0.0;
                break;
            default:
                return null;
        }
        return p;
    }

    /// <summary>Field-by-field comparison (tests: an asset copy must equal its built-in, or the goldens go stale).</summary>
    public static bool SameNumbers(FrontRoomsGlassProfileData a, FrontRoomsGlassProfileData b, List<string> diffs)
    {
        var ok = true;
        foreach (var f in typeof(FrontRoomsGlassProfileData).GetFields())
        {
            if (f.IsStatic || f.IsLiteral)
                continue;
            var va = f.GetValue(a);
            var vb = f.GetValue(b);
            if (!Equals(va, vb))
            {
                ok = false;
                if (diffs != null)
                    diffs.Add(f.Name + ": " + va + " != " + vb);
            }
        }
        return ok;
    }
}
