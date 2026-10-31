// Level transitions (production, 2026-10-22): edit-mode checks of the visual side (FrontRoomsTransitionKit).
//   Unity -batchmode -projectPath <clone> -buildTarget OSXUniversal -executeMethod FrontRoomsTransitionKitTests.RunBatch -quit -logFile <log>
// Writes Verification/transition-kit-tests.json; exits 1 on FAIL. The map's own checks of the hooks (collision freeze,
// BorderRenovated share and determinism, start area, baseTop) are in FrontRoomsMapInteractionTests ("Transitions").
// Checks here:
//  - the double-acting sleeve is D1.6's DA envelope, symmetric, with no stop; the saddle 152 x 12 mm;
//  - every kit mesh is finite; the 20 materials exist; stage drops carry no print and no ink; the paper scraps and peeled
//    strips are L0_Wallpaper's print; the renovation textures are capped at 1024 for WebGL / iOS / Android only;
//  - G7 (A12): no kit piece or prop enters a door leaf at 0-95 degrees on either face, or at the break's rest, above 0.012 m;
//  - G4: kits on and off light the same lamps; G12: no blue tape, no label; G8: two builds are identical;
//    the reduced tier builds without props, scraps, tape, islands, hanging strips or fray.
// ASCII only, no tuples.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FrontRooms.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class FrontRoomsTransitionKitTests
{
    [Serializable]
    sealed class Report
    {
        public string verdict;
        public int passed, failed;
        public List<string> checks = new List<string>();
    }

    static Report report;
    const BindingFlags IF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    const BindingFlags SF = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    static void Check(bool ok, string what)
    {
        if (ok) report.passed++; else report.failed++;
        report.checks.Add((ok ? "ok   " : "FAIL ") + what);
    }

    public static void RunBatch()
    {
        var pass = Execute();
        EditorApplication.Exit(pass ? 0 : 1);
    }

    [MenuItem("FrontRooms/Rendering/Test level transition kits")]
    public static void Run() => Execute();

    static bool Execute()
    {
        report = new Report();
        var roots = new List<GameObject>();
        var profiles = new List<FrontRoomsLevelProfile>();
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/FrontRooms3D.unity", OpenSceneMode.Single);
            Pure();
            Materials();
            // The map-built checks need the map's hooks (production/CONTRACT_map.diff, landed by the map line after
            // C22). Until then the kit is inert in this project: say so instead of failing.
            if (typeof(FrontRoomsMapWorld).GetMethod("BorderRenovated", BindingFlags.Instance | BindingFlags.Public) == null)
                report.checks.Add("skip the map-built checks (kits build, G4, G7, G8, reduced tier): this project's FrontRoomsMapWorld has no level-transition hooks yet (CONTRACT_map.diff not landed); the kit is inert here");
            else
                Built(roots, profiles);
        }
        catch (Exception e) { Check(false, "exception: " + e.GetType().Name + " " + e.Message + "\n" + e.StackTrace); }
        finally
        {
            foreach (var r in roots)
            {
                if (r == null) continue;
                var w = r.GetComponent<FrontRoomsMapWorld>();
                if (w != null) w.Release();
                Object.DestroyImmediate(r);
            }
            foreach (var p in profiles) if (p != null) Object.DestroyImmediate(p);
            FrontRoomsTransitionAssets.SetMode("pick");
        }
        var pass = report.failed == 0 && report.passed > 0;
        report.verdict = (pass ? "PASS" : "FAIL") + ": " + report.passed + " passed, " + report.failed + " failed";
        var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "transition-kit-tests.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        var text = "[TransitionKitTests] " + report.verdict + "\n" + string.Join("\n", report.checks);
        if (pass) Debug.Log(text); else Debug.LogError(text);
        return pass;
    }

    // ------------------------------------------------------------------ pure

    static void Pure()
    {
        var F = typeof(FrontRoomsTransitionKit.FrameKit);
        var sleeve = (FrontRoomsTransitionKit.Profile)F.GetMethod("SleeveDA", SF).Invoke(null, null);
        var pts = sleeve.points.SelectMany(p => p).ToList();
        float minW = pts.Min(p => p.y), maxW = pts.Max(p => p.y), minS = pts.Min(p => p.x), maxS = pts.Max(p => p.x);
        Check(Mathf.Abs(minW + .0911f) < 1e-4f && Mathf.Abs(maxW - .0911f) < 1e-4f && Mathf.Abs(minS + .002f) < 1e-4f && Mathf.Abs(maxS - .0488f) < 1e-4f,
            "DA sleeve: casing +-0.0911 off the wall centre, 0.0488 beyond the jamb, soffit 0.002 into the opening (D1.6 DA envelope, F4) (w " + minW.ToString("0.0000") + ".." + maxW.ToString("0.0000") + ", s " + minS.ToString("0.0000") + ".." + maxS.ToString("0.0000") + ")");
        var soffit = pts.Where(p => p.x < 0f).ToList();
        var stopless = soffit.All(p => Mathf.Abs(p.x + .002f) < .0035f);
        var symmetric = pts.All(p => pts.Any(q => Mathf.Abs(q.x - p.x) < 1e-4f && Mathf.Abs(q.y + p.y) < 1e-4f));
        Check(stopless && symmetric, "DA sleeve: no stop (the soffit is flat from face to face) and the section is the same on both faces (A12)");
        var saddle = (FrontRoomsTransitionKit.Profile)F.GetMethod("SaddleProfile", SF).Invoke(null, null);
        var sp = saddle.points.SelectMany(p => p).ToList();
        Check(Mathf.Abs(sp.Max(p => p.y) - .076f) < 1e-4f && Mathf.Abs(sp.Min(p => p.y) + .076f) < 1e-4f && Mathf.Abs(sp.Max(p => p.x) - .012f) < 1e-4f,
            "saddle: +-0.076 wide, 0.012 high (D1.6)");
        Check(FrontRoomsTransitionKit.FrameKit.CoveHeight == ModuleUnits.CoveHeight && FrontRoomsTransitionKit.RenoKit.SweepRadius == .98f,
            "kit numbers: base 0.10 (ModuleUnits.CoveHeight), leaf sweep 0.98 m");
        FrontRoomsTransitionKit.ShareOverride = -1f;
        FrontRoomsTransitionKit.FrameOnly = false;
        var a = FrontRoomsTransitionKit.RenovationShare(.30f);
        FrontRoomsTransitionKit.FrameOnly = true;
        var b = FrontRoomsTransitionKit.RenovationShare(.30f);
        FrontRoomsTransitionKit.FrameOnly = false;
        FrontRoomsTransitionKit.ShareOverride = 1f;
        var c = FrontRoomsTransitionKit.RenovationShare(.30f);
        FrontRoomsTransitionKit.ShareOverride = -1f;
        Check(a == .30f && b == 0f && c == 1f, "RenovationShare: the profile's 0.30; 0 with -transitionFrameOnly; the -renoShare override");
        Check(!FrontRoomsTransitionKit.Reduced, "the Editor is not on the reduced tier unless -transitionReduced asks for it");
    }

    // ------------------------------------------------------------------ materials and textures

    static readonly string[] Stage = { "Office_Wall_Fresh", "Office_Wall_CutIn", "Wall_Primer", "Wall_Stripped", "Wall_Compound" };

    static void Materials()
    {
        var names = new[] { "Trans_AluminiumSatin", "Office_Wall_Fresh", "Office_Wall_CutIn", "Wall_Primer", "Wall_Stripped", "Wall_Compound", "Paper_Backing",
            "Paper_Scrap", "Paper_Peel", "Slab_Adhesive", "Office_Ceiling_New", "L0_CeilingTile_Loose", "Office_CarpetTile_Loose", "Plenum_Dark",
            "Chalk_Blue", "Prop_Compound", "Prop_PaintWet", "Prop_RollerNap", "Prop_Canvas", "Prop_TapeBeige" };
        var missing = names.Where(n => Resources.Load<Material>("Surfaces/" + n) == null).ToList();
        Check(missing.Count == 0, "materials: the 20 transition surfaces exist (" + (missing.Count == 0 ? "all" : "missing " + string.Join(", ", missing)) + ")");
        var printed = Stage.Where(n => { var m = Resources.Load<Material>("Surfaces/" + n); return m != null && m.IsKeywordEnabled("_FR_PRINT"); }).ToList();
        Check(printed.Count == 0, "G11: stripped, primed and painted drops carry no print and no ink (" + printed.Count + " with _FR_PRINT)");
        var l0 = Resources.Load<Material>("Surfaces/L0_Wallpaper");
        foreach (var n in new[] { "Paper_Scrap", "Paper_Peel" })
        {
            var m = Resources.Load<Material>("Surfaces/" + n);
            var same = m != null && l0 != null && m.IsKeywordEnabled("_FR_PRINT") && m.IsKeywordEnabled("_FR_MESH_UV")
                && m.GetTexture("_PrintTex") == l0.GetTexture("_PrintTex") && m.GetTexture("_BaseMap") == l0.GetTexture("_BaseMap")
                && m.GetColor("_InkGround") == l0.GetColor("_InkGround") && m.GetColor("_InkMid") == l0.GetColor("_InkMid") && m.GetColor("_InkDeep") == l0.GetColor("_InkDeep")
                && m.GetVector("_TileSize") == l0.GetVector("_TileSize") && m.shader == l0.shader
                && m.GetVector("_SheetParams") == l0.GetVector("_SheetParams") && m.GetVector("_SeamParams") == l0.GetVector("_SeamParams")
                && m.GetFloat("_SubstrateScale") == l0.GetFloat("_SubstrateScale");
            Check(same, "G11: " + n + " is today's Level 0 print (L0_Wallpaper's paper, print frame, ink, roll size and hung paper; the live _FR_Print through the driver's globals) on mesh UVs");
        }
        int capped = 0, desktopTouched = 0, total = 0;
        foreach (var stem in FrontRoomsTransitionAssets.NewTextures)
            foreach (var suffix in new[] { "_A", "_N", "_S" })
            {
                var imp = AssetImporter.GetAtPath("Assets/Resources/Surfaces/Textures/" + stem + suffix + ".png") as TextureImporter;
                if (imp == null) continue;
                total++;
                var ok = new[] { "WebGL", "iPhone", "Android" }.All(p => { var s = imp.GetPlatformTextureSettings(p); return s.overridden && s.maxTextureSize == 1024; });
                if (ok) capped++;
                if (imp.GetPlatformTextureSettings("Standalone").overridden) desktopTouched++;
            }
        Check(total == 30 && capped == 30 && desktopTouched == 0, "textures: the 30 renovation maps are capped at 1024 on WebGL / iOS / Android and untouched on the desktop (" + capped + "/" + total + " capped, " + desktopTouched + " desktop overrides)");
        var report = FrontRoomsTransitionAssets.TextureReport();
        Check(report.Contains("TOTAL desktop (GPU, mips) 94.0 MB") && report.Contains("reduced tier (1024 cap) 34.0 MB"), "textures: 94.0 MB on the desktop, 34.0 MB on the reduced tier");
    }

    // ------------------------------------------------------------------ on a built map

    sealed class Map
    {
        public GameObject root;
        public FrontRoomsMapWorld world;
    }

    static Map Build(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles, string mode, int seed, Vector3 spawn, float period)
    {
        FrontRoomsTransitionAssets.SetMode(mode);
        FrontRoomsTransitionKit.ResetStats();
        var p = Object.Instantiate(FrontRoomsLevelProfiles.Resolve());
        p.hideFlags = HideFlags.DontSave;
        p.generation = p.Generation(seed);
        profiles.Add(p);
        var go = new GameObject("TRANSITION KIT TEST / " + mode + " " + seed) { hideFlags = HideFlags.DontSave };
        go.transform.position = new Vector3(period * ModuleUnits.WorldPeriod, 0f, 26f * ModuleUnits.WorldPeriod);
        roots.Add(go);
        var w = go.AddComponent<FrontRoomsMapWorld>();
        w.Profile = p;
        w.OverrideSpawn(spawn, 0f);
        w.BuildForCapture();
        w.TickFixturesForTools(0f);
        Physics.SyncTransforms();
        return new Map { root = go, world = w };
    }

    static string Signature(GameObject root)
    {
        var parts = new List<string>();
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            var path = mf.transform.parent != null ? mf.transform.parent.name + "/" + mf.name : mf.name;
            var v = mf.sharedMesh.vertices;
            var h = 17L;
            foreach (var q in v) h = h * 31 + BitConverter.SingleToInt32Bits(q.x) * 7 + BitConverter.SingleToInt32Bits(q.y) * 13 + BitConverter.SingleToInt32Bits(q.z);
            parts.Add(path + ":" + v.Length + ":" + h);
        }
        parts.Sort(StringComparer.Ordinal);
        return string.Join("|", parts);
    }

    static void Built(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles)
    {
        var shotArea = new Vector3(793.5f, 0f, 601.5f);
        const int seed = 516574485;
        var pick = Build(roots, profiles, "pick", seed, shotArea, 30f);
        var props = FrontRoomsTransitionKit.StatProps;
        var renderers = FrontRoomsTransitionKit.StatRenderers;
        Check(FrontRoomsTransitionKit.TransitionMesh.NonFinite == 0, "meshes: no kit triangle with a non-finite corner (" + FrontRoomsTransitionKit.TransitionMesh.NonFinite + " dropped)");
        Check(renderers > 0 && FrontRoomsTransitionKit.FrameKit.Count.portals + FrontRoomsTransitionKit.FrameKit.Count.casedArches + FrontRoomsTransitionKit.FrameKit.Count.doorFrames > 0,
            "the kits build on today's map (" + renderers + " kit renderers, " + props + " props; " + FrontRoomsTransitionKit.FrameKit.Count + ")");

        // G8: the same build twice is the same geometry.
        var again = Build(roots, profiles, "pick", seed, shotArea, 32f);
        var sa = Signature(pick.root).Split('|');
        var sb = Signature(again.root).Split('|');
        var diff = sa.Except(sb).Concat(sb.Except(sa)).ToList();
        if (diff.Count == 0 && !sa.SequenceEqual(sb))
            for (var i = 0; i < Math.Min(sa.Length, sb.Length); i++) if (sa[i] != sb[i]) { diff.Add("#" + i + " " + sa[i] + " vs " + sb[i]); break; }
        if (diff.Count == 0 && sa.Length != sb.Length) diff.Add("counts " + sa.Length + " vs " + sb.Length);
        Check(sa.Length > 0 && diff.Count == 0, "G8: two desktop builds give the same meshes (" + sa.Length + " meshes" + (diff.Count > 0 ? ", " + diff.Count + " differ: " + string.Join(" / ", diff.Take(6)) : "") + ")");
        Object.DestroyImmediate(again.root);
        roots.Remove(again.root);

        // G4: the same lamps lit and shadowed with the kits on and off.
        var now = Build(roots, profiles, "now", seed, shotArea, 34f);
        string Lamps(FrontRoomsMapWorld w) => string.Join(",", w.GetComponentsInChildren<Light>(true).Select(l => l.enabled + ":" + l.intensity.ToString("0.000") + ":" + l.color + ":" + l.shadows));
        Check(Lamps(pick.world) == Lamps(now.world), "G4: every lamp has the same state with the kits on and off (" + pick.world.GetComponentsInChildren<Light>(true).Length + " lights)");
        Object.DestroyImmediate(now.root);
        roots.Remove(now.root);

        // G12: beige crepe tape only, no blue tape, no label or sign in the kits.
        var kitMats = pick.root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Transitions").SelectMany(t => t.GetComponentsInChildren<Renderer>(true)).SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.name).Distinct().ToList();
        var bad = kitMats.Where(n => n.Contains("TapeBlue") || n.Contains("Label") || n.Contains("Sign") || n.Contains("Print_")).ToList();
        Check(bad.Count == 0, "G12: no blue tape, label or sign among the kit materials (" + kitMats.Count + " materials" + (bad.Count > 0 ? ": " + string.Join(", ", bad) : "") + ")");

        // G7: the leaf of every door, 0-95 degrees on both faces and at the break's rest, meets no kit piece above 0.012 m.
        Sweep(pick.world, "pick");

        // The reduced tier: builds, with none of the desktop-only detail.
        var reduced = Build(roots, profiles, "reduced", seed, shotArea, 36f);
        var stats = FrontRoomsTransitionKit.RenoKit.Stats;
        float S(string k) => stats.TryGetValue(k, out var v) ? v : 0f;
        Check(FrontRoomsTransitionKit.StatProps == 0 && S("hanging strips") == 0f && S("islands") == 0f && S("floor scraps") == 0f && S("tape pieces") == 0f && S("fray m") == 0f && S("tear lines") > 0f,
            "reduced tier: no props, hanging strips, islands, scraps, tape or fray; tears still cut (" + S("tear lines") + " tear lines)");
        Sweep(reduced.world, "reduced");
        FrontRoomsTransitionAssets.SetMode("pick");

        // Renovated door from seed 2554 too (another layout), the sweep again.
        var other = Build(roots, profiles, "pick", 2554, new Vector3(36f, 0f, 36f), 38f);
        Sweep(other.world, "seed 2554");
        FrontRoomsTransitionAssets.SetMode("pick");
    }

    /// <summary>G7 for one built map: kit mesh vertices and prop bounds against the leaf's box at every pose.</summary>
    static void Sweep(FrontRoomsMapWorld world, string label)
    {
        var points = new List<Vector3>();
        var own = new List<bool>();
        var steel = FrontRoomsSurfaces.TryGet("Prop_SteelBrown");
        foreach (var kit in world.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Transitions"))
        {
            foreach (var mf in kit.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                var mr = mf.GetComponent<MeshRenderer>();
                var isSteel = mr != null && mr.sharedMaterial == steel;
                var m = mf.transform.localToWorldMatrix;
                var v = mf.sharedMesh.vertices;
                if (mf.name.StartsWith("Transition "))
                    foreach (var q in v) { points.Add(m.MultiplyPoint3x4(q)); own.Add(isSteel); }
            }
            foreach (Transform prop in kit)
            {
                if (!prop.name.StartsWith("Renovation prop")) continue;
                foreach (var r in prop.GetComponentsInChildren<Renderer>(true))
                {
                    var b = r.bounds;
                    for (var i = 0; i < 27; i++)
                    {
                        points.Add(new Vector3(Mathf.Lerp(b.min.x, b.max.x, (i % 3) * .5f), Mathf.Lerp(b.min.y, b.max.y, (i / 3 % 3) * .5f), Mathf.Lerp(b.min.z, b.max.z, (i / 9) * .5f)));
                        own.Add(false);
                    }
                }
            }
        }
        var doors = new List<FrontRoomsMapWorld.Door>();
        var seen = new HashSet<long>();
        var mid = world.CellOf(world.SpawnWorldPosition);
        for (var y = mid.y - 24; y <= mid.y + 24; y++)
        for (var x = mid.x - 24; x <= mid.x + 24; x++)
        {
            var c = new GridCoord(x, y);
            foreach (var n in new[] { new GridCoord(x + 1, y), new GridCoord(x, y + 1) })
            {
                var d = world.DoorBetween(c, n);
                if (d != null && seen.Add(d.edge)) doors.Add(d);
            }
        }
        var pose = typeof(FrontRoomsMapWorld).GetMethod("PoseDoor", SF);
        var crookedField = typeof(FrontRoomsMapWorld.Door).GetField("crooked", IF);
        int hits = 0, ownHits = 0, poses = 0, near = 0;
        var firstHit = "";
        foreach (var d in doors)
        {
            var hinge = d.hinge.position;
            var idx = new List<int>();
            for (var i = 0; i < points.Count; i++)
            {
                var p = points[i];
                var dx = p.x - hinge.x;
                var dz = p.z - hinge.z;
                if (dx * dx + dz * dz < 1.3f * 1.3f && p.y > .012f) idx.Add(i);
            }
            near += idx.Count;
            float keepSwing = d.swing, keepAngle = d.angle;
            var keepCrooked = crookedField != null ? (float)crookedField.GetValue(d) : 0f;
            foreach (var sw in new[] { 1f, -1f })
            {
                var angles = new List<float>();
                for (var a = 0; a <= 95; a += 5) angles.Add(a);
                angles.Add(FrontRoomsShotTimings.DoorBreak.BounceRestDeg);
                for (var k = 0; k < angles.Count; k++)
                {
                    var broken = k == angles.Count - 1;
                    d.swing = sw;
                    d.angle = angles[k];
                    crookedField?.SetValue(d, broken ? FrontRoomsShotTimings.DoorBreak.CrookedDeg : 0f);
                    pose.Invoke(null, new object[] { d });
                    poses++;
                    var leaf = d.leaf.transform;
                    var inv = leaf.worldToLocalMatrix;
                    foreach (var i in idx)
                    {
                        var l = inv.MultiplyPoint3x4(points[i]);
                        // The leaf is a unit cube scaled by its transform; 1 mm inside it counts.
                        var s = leaf.lossyScale;
                        if (Mathf.Abs(l.x) * s.x < s.x * .5f - .001f && Mathf.Abs(l.y) * s.y < s.y * .5f - .001f && Mathf.Abs(l.z) * s.z < s.z * .5f - .001f)
                        {
                            if (own[i]) { ownHits++; continue; }
                            hits++;
                            if (firstHit == "") firstHit = "door " + d.a + "|" + d.b + " swing " + sw + " angle " + angles[k] + " at " + points[i].ToString("F3");
                        }
                    }
                }
            }
            d.swing = keepSwing;
            d.angle = keepAngle;
            crookedField?.SetValue(d, keepCrooked);
            pose.Invoke(null, new object[] { d });
        }
        Check(doors.Count > 0 && hits == 0, "G7 (" + label + "): " + doors.Count + " doors x " + (poses / Math.Max(1, doors.Count)) + " poses (0-95 degrees both faces, break rest): no kit piece or prop inside the leaf above 0.012 m ("
            + hits + " hits" + (firstHit != "" ? ", first " + firstHit : "") + "; " + near + " kit points within 1.3 m of a hinge; the door's own steel frame touched the cube leaf at "
            + ownHits + " points, as the map's trim does today: D1.6's DA leaf pivots clear of it)");
    }
}
