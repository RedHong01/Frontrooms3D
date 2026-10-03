using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Captions (FrontRoomsCaptions), checked without playing: nothing shows
/// while captions are off; at most three lines, the oldest dropped; the same
/// text refreshes its line and moves it to the bottom; lines expire and fade
/// over their last 0.3 s; the direction from the player's view (ahead, left,
/// right, behind, none when close); turning captions off clears them.
/// Writes Verification/captions-tests.json.
/// Headless: -executeMethod FrontRoomsCaptionsTests.RunBatch -quit (throws on FAIL).
/// </summary>
public static class FrontRoomsCaptionsTests
{
    [Serializable]
    sealed class Report
    {
        public string verdict;
        public int passed, failed;
        public List<string> checks = new List<string>();
    }

    static Report report;

    static void Check(bool ok, string what)
    {
        if (ok) report.passed++; else report.failed++;
        report.checks.Add((ok ? "ok   " : "FAIL ") + what);
    }

    [MenuItem("FrontRooms/Map/Test captions")]
    public static void Run() => Execute(false);

    public static void RunBatch() => Execute(true);

    static void Execute(bool throwOnFail)
    {
        report = new Report();
        FrontRoomsSettings.Load();
        var captions = FrontRoomsSettings.Captions;
        try
        {
            Tests();
        }
        catch (Exception e)
        {
            Check(false, "exception: " + e.GetType().Name + " " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            if (FrontRoomsSettings.Captions != captions) FrontRoomsSettings.SetCaptions(captions);
            FrontRoomsCaptions.Clear();
        }
        var pass = report.failed == 0 && report.passed > 0;
        report.verdict = (pass ? "PASS" : "FAIL") + ": " + report.passed + " passed, " + report.failed + " failed";
        var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "captions-tests.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        var text = "[CaptionsTests] " + report.verdict + "\n" + string.Join("\n", report.checks);
        if (pass) Debug.Log(text); else Debug.LogError(text);
        if (throwOnFail && !pass) throw new Exception(report.verdict);
    }

    static string Texts()
    {
        var names = new List<string>();
        foreach (var line in FrontRoomsCaptions.Lines) names.Add(line.text);
        return string.Join(",", names);
    }

    static void Tests()
    {
        FrontRoomsCaptions.Clear();
        if (FrontRoomsSettings.Captions) FrontRoomsSettings.SetCaptions(false);
        FrontRoomsCaptions.Post("[A]");
        Check(FrontRoomsCaptions.Lines.Count == 0, "off: a posted caption does not show");

        FrontRoomsSettings.SetCaptions(true);
        FrontRoomsCaptions.Post("[A]", null, 2f);
        FrontRoomsCaptions.Post("[B]", null, 2f);
        FrontRoomsCaptions.Post("[C]", null, 2f);
        FrontRoomsCaptions.Post("[D]", null, 2f);
        Check(Texts() == "[B],[C],[D]", "three lines at most, the oldest dropped (" + Texts() + ")");
        FrontRoomsCaptions.Tick(1.5f);
        FrontRoomsCaptions.Post("[B]", Vector3.forward * 5f, 1f);
        var b = FrontRoomsCaptions.Lines[FrontRoomsCaptions.Lines.Count - 1];
        Check(Texts() == "[C],[D],[B]" && Mathf.Abs(b.left - 1f) < 1e-4f && b.source.HasValue,
            "the same text refreshes its line, moves it to the bottom and takes the new place (" + Texts() + ", " + b.left.ToString("0.00") + " s left)");
        FrontRoomsCaptions.Tick(.4f);
        var c = FrontRoomsCaptions.Lines[0];
        Check(Mathf.Abs(FrontRoomsCaptions.Alpha(c) - .1f / FrontRoomsCaptions.FadeSeconds) < 1e-3f && FrontRoomsCaptions.Alpha(b) == 1f,
            "a line fades over its last 0.3 s (" + FrontRoomsCaptions.Alpha(c).ToString("0.00") + " with 0.1 s left), a fresh one is opaque");
        FrontRoomsCaptions.Tick(.2f);
        Check(Texts() == "[B]", "lines expire when their time is up (" + Texts() + ")");

        var o = Vector3.zero;
        var f = Vector3.forward;
        Check(FrontRoomsCaptions.Direction(o, f, new Vector3(.3f, 0f, 6f)) == "AHEAD" && FrontRoomsCaptions.Direction(o, f, new Vector3(-6f, 0f, 1f)) == "LEFT"
              && FrontRoomsCaptions.Direction(o, f, new Vector3(6f, 0f, -1f)) == "RIGHT" && FrontRoomsCaptions.Direction(o, f, new Vector3(-1f, 0f, -6f)) == "BEHIND"
              && FrontRoomsCaptions.Direction(o, f, new Vector3(1f, 0f, .5f)) == null,
            "direction from the player's view: ahead, left, right, behind, none within 1.5 m");
        var turned = FrontRoomsCaptions.Direction(o, Vector3.right, new Vector3(0f, 0f, 6f));
        Check(turned == "LEFT", "the direction follows where the player looks (+Z is LEFT when facing +X: " + turned + ")");
        FrontRoomsCaptions.Post("[DOOR BLOWS]", new Vector3(-8f, 0f, 0f));
        var blows = FrontRoomsCaptions.Lines[FrontRoomsCaptions.Lines.Count - 1];
        FrontRoomsCaptions.Post("[HUM]");
        var hum = FrontRoomsCaptions.Lines[FrontRoomsCaptions.Lines.Count - 1];
        Check(FrontRoomsCaptions.Format(blows, o, f) == "[DOOR BLOWS  ·  LEFT]" && FrontRoomsCaptions.Format(hum, o, f) == "[HUM]",
            "drawn with the direction inside the brackets (" + FrontRoomsCaptions.Format(blows, o, f) + "); no place, no direction");

        FrontRoomsSettings.SetCaptions(false);
        Check(FrontRoomsCaptions.Lines.Count == 0, "turning captions off clears the lines");
    }
}
