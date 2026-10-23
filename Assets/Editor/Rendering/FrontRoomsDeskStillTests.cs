using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

/// <summary>
/// V4 still desk screen checks (tutorial T3; Documentation/research/tutorial/PREBUILD_v4desk.md). Edit mode,
/// headless, no menu item and no InitializeOnLoad hook:
///   Unity -batchmode -projectPath <project> -buildTarget OSXUniversal -executeMethod FrontRoomsDeskStillTests.RunBatch -quit [-dsTestOut dir]
/// Default output: <project>/Verification/desk_still/tests (tests.json, log.txt). Takes seconds.
///   S1  the reader: FrontRoomsMapWorld.FloorHasStation bound as property / field / absent; absent reads false;
///       FrontRooms3DGame.FloorChanged subscribed directly, and raising it moves the reader's floor (S1f).
///   S2  the pick: deterministic per desk; shares over 1,000 desks within 0.04 of 55 / 17 / 13 / 15 %; every
///       frame present in 100 desks; another map seed or floor re-deals.
///   S3  the art in Resources/Rendering/StillDesk: six frames, 640 x 480 and 1280 x 960, uncompressed, sRGB,
///       mipmapped, clamped, no NPOT scaling; the @2x set (never loaded there) and the flat desk_blank import at 32 px
///       on iOS, Android and WebGL.
///   S4  the fit (4:3 frame on the 0.28 x 0.19 m face, centred) and the @2x rule (1x up to ~2,370 px windows at 76 deg).
/// </summary>
public static class FrontRoomsDeskStillTests
{
    static readonly StringBuilder Log = new StringBuilder();
    sealed class Result { public string name; public bool pass; public string detail; }
    static readonly List<Result> Results = new List<Result>();
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static string outDir;

    public static void RunBatch()
    {
        var args = Environment.GetCommandLineArgs();
        outDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "desk_still", "tests");
        for (var i = 0; i < args.Length - 1; i++) if (args[i] == "-dsTestOut") outDir = args[i + 1];
        Directory.CreateDirectory(outDir);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Run("S1 reader", S1Reader);
        Run("S2 pick", S2Pick);
        Run("S3 art", S3Art);
        Run("S4 fit and @2x rule", S4Fit);
        var failed = 0;
        var json = new StringBuilder("{\n  \"tests\": [\n");
        for (var i = 0; i < Results.Count; i++)
        {
            var r = Results[i];
            if (!r.pass) failed++;
            json.Append("    {\"name\": \"" + Esc(r.name) + "\", \"pass\": " + (r.pass ? "true" : "false") + ", \"detail\": \"" + Esc(r.detail) + "\"}" + (i + 1 < Results.Count ? "," : "") + "\n");
        }
        json.Append("  ],\n  \"verdict\": \"" + (failed == 0 ? "PASS" : "FAIL") + " " + (Results.Count - failed) + "/" + Results.Count + "\"\n}\n");
        File.WriteAllText(Path.Combine(outDir, "tests.json"), json.ToString());
        Say("VERDICT " + (failed == 0 ? "PASS" : "FAIL") + " " + (Results.Count - failed) + "/" + Results.Count);
        File.WriteAllText(Path.Combine(outDir, "log.txt"), Log.ToString());
    }

    static string Esc(string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\""); }

    static void Say(string s)
    {
        Log.AppendLine(s);
        Debug.Log("[DeskStillTests] " + s);
    }

    static void Check(bool ok, string name, string detail)
    {
        Results.Add(new Result { name = name, pass = ok, detail = detail });
        Say((ok ? "ok   " : "FAIL ") + name + " :: " + detail);
    }

    static void Run(string title, Action body)
    {
        Say("== " + title);
        try { body(); }
        catch (Exception e) { Check(false, title + " threw", e.GetType().Name + ": " + e.Message); }
    }

    static void S1Reader()
    {
        var binding = FrontRoomsStationPresence.Binding;
        var member = typeof(FrontRoomsMapWorld).GetProperty(FrontRoomsStationPresence.MemberName, BindingFlags.Public | BindingFlags.Instance) != null ? "property"
            : typeof(FrontRoomsMapWorld).GetField(FrontRoomsStationPresence.MemberName, BindingFlags.Public | BindingFlags.Instance) != null ? "field" : "absent";
        Check(binding == member, "S1a binding matches the map", "binding " + binding + ", map member " + member);
        var go = new GameObject("DeskStillTests map");
        try
        {
            var map = go.AddComponent<FrontRoomsMapWorld>();
            var read = FrontRoomsStationPresence.Read(map);
            if (member == "absent") Check(!read, "S1b absent member reads false", "Read = " + read);
            else Check(true, "S1b member present", "Read = " + read + " (the map's default)");
            Check(!FrontRoomsStationPresence.Read(null), "S1c null map reads false", "Read(null) = false");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
        var hook = typeof(FrontRoomsStationPresence).GetMethod("Hook", BindingFlags.NonPublic | BindingFlags.Static);
        hook.Invoke(null, null);
        var hasEvent = typeof(FrontRooms3DGame).GetEvent(FrontRoomsStationPresence.FloorChangedEventName, BindingFlags.Public | BindingFlags.Static) != null;
        var state = FrontRoomsStationPresence.FloorChangedHook;
        Check(hasEvent && state == "subscribed", "S1d FloorChanged hook", "event " + (hasEvent ? "present" : "absent") + ", hook: " + state);
        Check(!FrontRoomsStationPresence.FloorHasStation && FrontRoomsStationPresence.Floor == 1, "S1e defaults after the hook", "FloorHasStation false, Floor 1");
        // S1f: the subscription is live. Raise FloorChanged through the event's backing delegate (no map: reads false).
        var backing = typeof(FrontRooms3DGame).GetField(FrontRoomsStationPresence.FloorChangedEventName, BindingFlags.NonPublic | BindingFlags.Static);
        var raised = backing != null ? backing.GetValue(null) as Delegate : null;
        var listeners = raised != null ? raised.GetInvocationList().Length : 0;
        if (raised != null) raised.DynamicInvoke(null, null, 3);
        var followed = FrontRoomsStationPresence.Floor == 3 && !FrontRoomsStationPresence.FloorHasStation;
        hook.Invoke(null, null);
        var single = backing != null && backing.GetValue(null) is Delegate again && again.GetInvocationList().Length == listeners;
        Check(followed && single && FrontRoomsStationPresence.Floor == 1, "S1f FloorChanged reaches the reader",
            "raised floor 3 with " + listeners + " listener(s): Floor " + (followed ? "3" : "not 3") + "; re-hook keeps " + listeners + " (" + single + "), Floor back to " + FrontRoomsStationPresence.Floor);
    }

    static void S2Pick()
    {
        var rng = new System.Random(7);
        var counts = new int[4];
        var counts100 = new int[4];
        var stable = true;
        for (var i = 0; i < 1000; i++)
        {
            var p = new Vector3((float)(rng.NextDouble() * 400 - 200), .74f + (i % 2) * .13f, (float)(rng.NextDouble() * 400 - 200));
            var f = FrontRoomsDeskStill.Pick(516574485, 1, p);
            counts[(int)f]++;
            if (i < 100) counts100[(int)f]++;
            if (FrontRoomsDeskStill.Pick(516574485, 1, p) != f) stable = false;
        }
        Check(stable, "S2a deterministic", "1,000 desks picked twice: same frames");
        var target = new[] { FrontRoomsDeskStill.CursorShare, FrontRoomsDeskStill.CursorLowShare, FrontRoomsDeskStill.BlankShare,
            1f - FrontRoomsDeskStill.CursorShare - FrontRoomsDeskStill.CursorLowShare - FrontRoomsDeskStill.BlankShare };
        var ok = true;
        var detail = new StringBuilder();
        for (var k = 0; k < 4; k++)
        {
            var share = counts[k] / 1000f;
            if (Mathf.Abs(share - target[k]) > .04f) ok = false;
            detail.Append(((FrontRoomsDeskStill.Frame)k) + " " + counts[k] + " (" + (share * 100f).ToString("0.0", Inv) + " % vs " + (target[k] * 100f).ToString("0", Inv) + " %); ");
        }
        Check(ok, "S2b shares over 1,000 desks", detail.ToString());
        Check(counts100[0] > counts100[1] && counts100[0] > counts100[2] && counts100[0] > counts100[3] && counts100[1] > 0 && counts100[2] > 0 && counts100[3] > 0,
            "S2c 100 desks: cursor most, every frame present", "cursor " + counts100[0] + ", cursor_low " + counts100[1] + ", blank " + counts100[2] + ", off " + counts100[3]);
        var differSeed = 0;
        var differFloor = 0;
        for (var i = 0; i < 200; i++)
        {
            var p = new Vector3(i * 3.1f, .74f, -i * 1.7f);
            if (FrontRoomsDeskStill.Pick(1, 1, p) != FrontRoomsDeskStill.Pick(2, 1, p)) differSeed++;
            if (FrontRoomsDeskStill.Pick(1, 1, p) != FrontRoomsDeskStill.Pick(1, 2, p)) differFloor++;
        }
        Check(differSeed > 60 && differFloor > 60, "S2d another seed or floor re-deals", differSeed + "/200 differ by seed, " + differFloor + "/200 by floor");
    }

    static void S3Art()
    {
        var names = new[] { "desk_cursor", "desk_cursor_low", "desk_blank" };
        foreach (var n in names)
            foreach (var hi in new[] { false, true })
            {
                var res = FrontRoomsDeskStill.Folder + n + (hi ? "@2x" : "");
                var tex = Resources.Load<Texture2D>(res);
                if (tex == null) { Check(false, "S3 " + res, "missing"); continue; }
                var w = hi ? 1280 : 640;
                var h = hi ? 960 : 480;
                var compressed = GraphicsFormatUtility.IsCompressedFormat(tex.graphicsFormat);
                var path = AssetDatabase.GetAssetPath(tex);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                var small = true;
                foreach (var plat in new[] { "iOS", "Android", "WebGL" })
                {
                    var ps = imp != null ? imp.GetPlatformTextureSettings(plat) : null;
                    var expectSmall = hi || n == "desk_blank";   // a flat field: 32 px is the same picture
                    var isSmall = ps != null && ps.overridden && ps.maxTextureSize == 32;
                    if (expectSmall != isSmall) small = false;
                }
                var ok = tex.width == w && tex.height == h && !compressed && tex.mipmapCount > 1 && tex.wrapMode == TextureWrapMode.Clamp
                    && tex.filterMode == FilterMode.Trilinear && imp != null && imp.sRGBTexture && imp.npotScale == TextureImporterNPOTScale.None && small;
                Check(ok, "S3 " + res, tex.width + "x" + tex.height + " " + tex.graphicsFormat + " mips " + tex.mipmapCount + " wrap " + tex.wrapMode + " filter " + tex.filterMode
                    + " sRGB " + (imp != null && imp.sRGBTexture) + " npot " + (imp != null ? imp.npotScale.ToString() : "?") + " phone/WebGL import " + (hi || n == "desk_blank" ? "32 px" : "full") + (small ? "" : " (WRONG)"));
            }
    }

    static void S4Fit()
    {
        var fit = FrontRoomsDeskStill.Fit(new Vector2(.28f, .19f), 640, 480);
        var s = (.28f / .19f) / (640f / 480f);
        Check(Mathf.Abs(fit.x - s) < 1e-4f && Mathf.Abs(fit.y - 1f) < 1e-6f && Mathf.Abs(fit.z - (1f - s) * .5f) < 1e-4f && Mathf.Abs(fit.w) < 1e-6f,
            "S4a 4:3 frame on the 0.28 x 0.19 face", "scale " + fit.x.ToString("0.00000", Inv) + " x " + fit.y.ToString("0.###", Inv) + ", offset " + fit.z.ToString("0.00000", Inv) + ", " + fit.w.ToString("0.###", Inv)
            + " (frame width " + (.19f * 640f / 480f).ToString("0.0000", Inv) + " m of 0.28 m, centred)");
        var r1080 = FrontRoomsDeskStill.ShownRows(76f, 1080, .19f, FrontRoomsDeskStill.ClosestViewMetres);
        var r2160 = FrontRoomsDeskStill.ShownRows(76f, 2160, .19f, FrontRoomsDeskStill.ClosestViewMetres);
        var r2880 = FrontRoomsDeskStill.ShownRows(76f, 2880, .19f, FrontRoomsDeskStill.ClosestViewMetres);
        Check(!FrontRoomsDeskStill.UseHiRes(76f, 1080, .19f) && !FrontRoomsDeskStill.UseHiRes(76f, 2160, .19f) && FrontRoomsDeskStill.UseHiRes(76f, 2880, .19f),
            "S4b @2x only where 1x is magnified", "rows shown at 0.6 m, 76 deg: 1080p " + r1080.ToString("0", Inv) + ", 2160p " + r2160.ToString("0", Inv) + ", 2880p " + r2880.ToString("0", Inv) + " (of 480)");
    }
}
