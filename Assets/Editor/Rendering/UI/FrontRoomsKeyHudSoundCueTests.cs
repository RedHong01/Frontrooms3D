using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>
/// HUD sound cues (DECISIONS #34, research/ui_key_icon/hud_build/DECISIONS.md): FrontRoomsKeyHud.SoundCue is raised once at
/// the START of each motion, also under Reduce Motion, never while the pause card is up or a close-up is open, and a
/// cancelled unlock raises no UnlockDrop. Edit mode, no menu item and no InitializeOnLoad hook. It needs graphics (the key
/// HUD's vector shader drives the crosshair states), so never -nographics:
///   Unity -batchmode -projectPath <project> -buildTarget OSXUniversal -executeMethod FrontRoomsKeyHudSoundCueTests.RunBatch -quit [-hscOut dir]
/// Default output: <project>/Verification/hudkey/soundcue (tests.json, cues.tsv = every cue with its frame time, log.txt).
/// Every scenario drives the key HUD on an injected 1/60 s clock (FrontRoomsKeyHud.EditorClock) through its public seam
/// (SetAim, SetHeldKey, SetHudAlpha, PickupInstant / PickupFlight, NoKey, Unlocking, BrokenTry) and the static events it
/// hears (FrontRoomsDoorShot.Beat, FrontRoomsCameraRig.ShotEnded, FrontRooms3DGame.Paused), invoked on the key HUD's own
/// handlers only. The unlock shot follows the map's timings: the takeover fade up over 0.15 s, KeyInsert 0.55 s, KeyTurn
/// 0.68 s, BoltRetract 0.86 s, KeyWithdraw 0.90 s, the fade down from 1.45 s, ShotEnded 1.55 s (default build: (b) + (d)).
///   C1  the six motions in a row: each cue once, in order, at the start of its motion (PickupFlight / PickupLand of the
///       KP-1b pickup, LockTug, UnlockDrop at the HUD's return + 0.10 s, BrokenBuild, BrokenKnock); E inside a reaction's
///       window raises nothing.
///   C2  the presented pickup (PickupFlight at HoldUntil, and a late take-off), the no-shot unlock (F.1 at 0.56 s), a new E
///       after a window (a second cue), a re-aim mid-leave (no rebuild, no cue), a flight dropped before it left (no cue).
///   C3  Reduce Motion (the OS switch): the same six cues, in order, where each cross-fade starts.
///   C4  the pause card: nothing in the whole story, nothing late after the resume, cues again after it; a flight that
///       lands under the pause card raises no PickupLand.
///   C5  a close-up (FrontRooms3DGame.InspectingNow): nothing.
///   C6  a cancelled unlock raises no UnlockDrop: cancelled by ShotEnded, by the HUD's return alone, under Reduce Motion;
///       a committed press after a cancel raises exactly one.
///   C7  no per-frame allocation with a listener: the whole story looped for 1200 frames, heap in use and gen-0
///       collections, with a positive control.
///   C8  O-42 (b): in the P4 slide-in STAIR KEY's keyline is off below LabelKeylineFrom of the word's alpha, rises after,
///       and is the shared tap alpha at rest.
///   C9  a language: the lines through the table, Bayon's face swapped and the runs measured in it, the language followed on
///       Changed and then back to English with every run where it was.
/// ASCII only, no tuples (clone editor-script rule).
/// </summary>
public static class FrontRoomsKeyHudSoundCueTests
{
    const float Dt = 1f / 60f;
    const BindingFlags NP = BindingFlags.Instance | BindingFlags.NonPublic;
    const BindingFlags SNP = BindingFlags.Static | BindingFlags.NonPublic;
    static readonly StringBuilder Log = new StringBuilder();
    static readonly StringBuilder CueLog = new StringBuilder("scenario\tframe\tt\tcue\n");
    sealed class Result { public string name; public bool pass; public string detail; }
    static readonly List<Result> Results = new List<Result>();
    static string outDir;
    static readonly FrontRoomsKeyHud.Identity Id = new FrontRoomsKeyHud.Identity(FrontRoomsKeyHud.Shape.Rect, FrontRoomsKeyHud.Tint.Red, 0);
    static readonly FrontRoomsHudSoundCue[] SixInOrder =
    {
        FrontRoomsHudSoundCue.PickupFlight, FrontRoomsHudSoundCue.PickupLand, FrontRoomsHudSoundCue.LockTug,
        FrontRoomsHudSoundCue.UnlockDrop, FrontRoomsHudSoundCue.BrokenBuild, FrontRoomsHudSoundCue.BrokenKnock
    };

    public static void RunBatch()
    {
        var args = Environment.GetCommandLineArgs();
        outDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "hudkey", "soundcue");
        for (var i = 0; i < args.Length - 1; i++) if (args[i] == "-hscOut") outDir = args[i + 1];
        Directory.CreateDirectory(outDir);
        var unlockBefore = FrontRoomsKeyHud.UnlockStart; var panelBefore = FrontRoomsKeyHud.PanelMode;
        try
        {
            Run("C0 build defaults (b) + (d), the enum and the event", C0Defaults);
            Run("C1 the six motions: each cue once, in order, at the start of its motion", C1Story);
            Run("C2a the presented pickup: PickupFlight at the hand-off, PickupLand 0.72 s later (also a late take-off)", C2Presented);
            Run("C2b the no-shot unlock: UnlockDrop as F.1 starts, 0.56 s after E", C2NoShot);
            Run("C2c once per motion: a new E after its window is a new cue, E inside it is not", C2Retrigger);
            Run("C2d a re-aim mid-leave resumes (no BrokenBuild); a rebuild after the leave raises one", C2Resume);
            Run("C2e a KP-1b flight dropped before it left raises nothing", C2Dropped);
            Run("C3 Reduce Motion: the six cues, in order, where each cross-fade starts", C3ReduceMotion);
            Run("C4a the pause card: no cue in the whole story, none late after the resume, cues again after it", C4Pause);
            Run("C4b a flight that lands under the pause card raises no PickupLand, then or after", C4PauseMidFlight);
            Run("C5 a close-up (InspectingNow): no cue", C5CloseUp);
            Run("C6a a cancelled unlock (ShotEnded, no BoltRetract) raises no UnlockDrop", () => C6Cancel(false, true));
            Run("C6b a cancelled unlock (the HUD's return alone) raises no UnlockDrop", () => C6Cancel(false, false));
            Run("C6c a cancelled unlock under Reduce Motion raises no UnlockDrop", () => C6Cancel(true, true));
            Run("C6d a committed press after a cancel raises exactly one UnlockDrop", C6CancelThenCommit);
            Run("C7 no per-frame allocation with a listener (the story looped, 1200 frames)", C7Heap);
            Run("C8 O-42 (b): STAIR KEY's keyline is off below LabelKeylineFrom of the word's alpha, at the tap alpha at rest", C8LabelKeyline);
            Run("C9 a language: the table's words, its face, the runs measured in it, followed on Changed, English back in place", C9Language);
        }
        finally
        {
            FrontRoomsKeyHud.EditorClock = null;
            FrontRoomsKeyHud.EditorReduceMotion = -1; FrontRoomsKeyHud.EditorReduceFlashing = -1;
            FrontRoomsKeyHud.UnlockStart = unlockBefore; FrontRoomsKeyHud.PanelMode = panelBefore;
            SetInspecting(false);
        }
        var passed = 0; foreach (var r in Results) if (r.pass) passed++;
        var json = new StringBuilder("{\n \"results\": [\n");
        for (var i = 0; i < Results.Count; i++)
            json.Append("  {\"name\": ").Append(Q(Results[i].name)).Append(", \"pass\": ").Append(Results[i].pass ? "true" : "false")
                .Append(", \"detail\": ").Append(Q(Results[i].detail)).Append(i < Results.Count - 1 ? "},\n" : "}\n");
        json.Append(" ],\n \"passed\": ").Append(passed).Append(",\n \"failed\": ").Append(Results.Count - passed).Append("\n}\n");
        File.WriteAllText(Path.Combine(outDir, "tests.json"), json.ToString());
        File.WriteAllText(Path.Combine(outDir, "cues.tsv"), CueLog.ToString());
        File.WriteAllText(Path.Combine(outDir, "log.txt"), Log.ToString());
        Debug.Log("[KeyHudSoundCueTests] " + passed + " passed, " + (Results.Count - passed) + " failed | " + outDir);
    }

    // ---------------------------------------------------------------- the sim (the key HUD on a canvas, edit mode)
    sealed class Sim
    {
        public FrontRoomsKeyHud hud;
        public float clock;
        public int frame;
        public readonly List<FrontRoomsHudSoundCue> cues = new List<FrontRoomsHudSoundCue>();
        public readonly List<float> at = new List<float>();
        public Action<ShotKind, DoorBeat, Vector3> beat;
        public Action<ShotKind, Vector3> ended;
        public Action<bool> paused;
        readonly string name;
        readonly bool record;
        readonly GameObject root;
        readonly Action late, enable, disable;
        readonly Action<FrontRoomsHudSoundCue> listener;
        readonly Func<float> clockFn;
        public Sim(string name, bool record)
        {
            this.name = name; this.record = record;
            root = new GameObject("HSC canvas", typeof(RectTransform), typeof(Canvas));
            root.hideFlags = HideFlags.DontSave;
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var cross = new GameObject("HUD / Crosshair", typeof(RectTransform)); cross.transform.SetParent(root.transform, false);
            var key = new GameObject("HUD / Key", typeof(RectTransform)); key.transform.SetParent(root.transform, false);
            var kr = (RectTransform)key.transform;
            kr.anchorMin = kr.anchorMax = Vector2.zero; kr.pivot = Vector2.zero; kr.anchoredPosition = new Vector2(72f, 118f); kr.sizeDelta = new Vector2(400f, 48f);
            clockFn = () => clock;
            FrontRoomsKeyHud.EditorClock = clockFn;
            hud = FrontRoomsKeyHud.Attach(canvas, (RectTransform)cross.transform, kr, false);
            var t = typeof(FrontRoomsKeyHud);
            late = (Action)Delegate.CreateDelegate(typeof(Action), hud, t.GetMethod("LateUpdate", NP));
            enable = (Action)Delegate.CreateDelegate(typeof(Action), hud, t.GetMethod("OnEnable", NP));
            disable = (Action)Delegate.CreateDelegate(typeof(Action), hud, t.GetMethod("OnDisable", NP));
            enable();   // edit mode never calls OnEnable: the key HUD subscribes to the static events here
            // the key HUD's own handlers of the static events it hears (fetched once: no reflection inside the frames)
            beat = Own<Action<ShotKind, DoorBeat, Vector3>>(typeof(FrontRoomsDoorShot), "Beat");
            ended = Own<Action<ShotKind, Vector3>>(typeof(FrontRoomsCameraRig), "ShotEnded");
            paused = Own<Action<bool>>(typeof(FrontRooms3DGame), "Paused");
            hud.SetHudAlpha(1f, 0f);
            listener = record ? (Action<FrontRoomsHudSoundCue>)OnCue : Count;
            FrontRoomsKeyHud.SoundCue += listener;
        }
        T Own<T>(Type type, string field) where T : class
        {
            var f = type.GetField(field, BindingFlags.Static | BindingFlags.NonPublic);
            var all = f != null ? f.GetValue(null) as Delegate : null;
            if (all == null) return null;
            Delegate mine = null;
            foreach (var d in all.GetInvocationList()) if (ReferenceEquals(d.Target, hud)) mine = Delegate.Combine(mine, d);
            return mine as T;
        }
        void OnCue(FrontRoomsHudSoundCue c)
        {
            cues.Add(c); at.Add(clock);
            CueLog.Append(name).Append('\t').Append(frame).Append('\t').Append(F(clock)).Append('\t').Append(c).Append('\n');
        }
        public bool Wired => beat != null && ended != null && paused != null;
        // the static editor clock is this sim's on every frame (two sims never share it)
        public void Frame(int i) { frame = i; clock = i * Dt; FrontRoomsKeyHud.EditorClock = clockFn; late(); }
        public void Beat(DoorBeat b) { if (beat != null) beat(ShotKind.Unlock, b, Vector3.zero); }
        public void ShotEnded() { if (ended != null) ended(ShotKind.Unlock, Vector3.zero); }
        public void Pause(bool on) { if (paused != null) paused(on); }
        public string Get(string key)
        {
            foreach (var f in hud.DebugLine.Split('\t')) if (f.StartsWith(key + "=", StringComparison.Ordinal)) return f.Substring(key.Length + 1);
            return "";
        }
        public float GetF(string key) { float v; return float.TryParse(Get(key), NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : float.NaN; }
        public int CountOf(FrontRoomsHudSoundCue c) { var n = 0; foreach (var x in cues) if (x == c) n++; return n; }
        public float FirstAt(FrontRoomsHudSoundCue c) { for (var i = 0; i < cues.Count; i++) if (cues[i] == c) return at[i]; return float.NaN; }
        public string Seq()
        {
            var sb = new StringBuilder();
            for (var i = 0; i < cues.Count; i++) sb.Append(i > 0 ? ", " : "").Append(cues[i]).Append(" @").Append(F(at[i]));
            return cues.Count == 0 ? "(none)" : sb.ToString();
        }
        public void Dispose()
        {
            FrontRoomsKeyHud.SoundCue -= listener;
            disable();
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static int counted;
    static readonly Action<FrontRoomsHudSoundCue> Count = c => counted++;

    static void Frames(Sim s, int i0, int i1, Action<int> before)
    {
        for (var i = i0; i < i1; i++) { if (before != null) before(i); s.Frame(i); }
    }

    // ---------------------------------------------------------------- the story: the six motions on frames 0 .. 719
    //   60   KP-1b PickupInstant: the glyph holds 0.5 s at the hand, then flies 0.72 s (PickupFlight ~1.50 s, PickupLand ~2.22 s)
    //   180  aim a locked door (no key; the padlock build is silent); 216 E (NoKey): LockTug at 3.60 s; 228 E inside its window
    //   270  the key is held (haveKey); 300 E with the key (Unlocking) and the unlock shot (ShotFrame): UnlockDrop at the
    //        HUD's return + 0.10 s ((b)); the panel's U.3 runs on the beats ((d)) and has no cue
    //   480  aim a broken door: BrokenBuild at 8.00 s; 540 E (BrokenTry): BrokenKnock at 9.00 s; 552 E inside its window
    //   630  the aim leaves
    const int StoryFrames = 720, UnlockAt = 300;
    static void StoryFrame(Sim s, int i)
    {
        s.hud.SetHeldKey(i >= 60, Id);
        if (i == 60) s.hud.PickupInstant(Id, 60f);
        if (i >= 180 && i < UnlockAt) s.hud.SetAim(FrontRoomsKeyHud.Aim.Locked, Id, i >= 270, null);
        else if (i >= 480 && i < 630) s.hud.SetAim(FrontRoomsKeyHud.Aim.Broken, Id, false, null);
        else s.hud.SetAim(FrontRoomsKeyHud.Aim.None, Id, false, null);
        if (i == 216 || i == 228) s.hud.NoKey();
        if (i == UnlockAt) s.hud.Unlocking();
        if (i >= UnlockAt && i < UnlockAt + 160) ShotFrame(s, i - UnlockAt, true, true, true);
        else s.hud.SetHudAlpha(1f, 0f);
        if (i == 540 || i == 552) s.hud.BrokenTry();
    }

    // One frame of the unlock shot, k frames after the press. commit: BoltRetract at 0.86 s and the HUD back from 1.45 s;
    // else a cancel at 0.75 s (the fade comes down from there; beats: KeyWithdraw + Return; ended: ShotEnded). After a cancel
    // the aim is on the (still locked) door again.
    static void ShotFrame(Sim s, int k, bool commit, bool beats, bool ended)
    {
        var r = k * Dt;
        var len = commit ? 1.45f : .75f;
        var up = Mathf.Clamp01(r / .15f);
        var down = r > len ? Mathf.Clamp01(1f - (r - len) / .15f) : 1f;
        s.hud.SetHudAlpha(1f, k >= 1 ? Mathf.Min(up, down) : 0f);
        if (!commit && r > len) s.hud.SetAim(FrontRoomsKeyHud.Aim.Locked, Id, true, null);
        if (k == 33) s.Beat(DoorBeat.KeyInsert);      // 0.550 s
        if (k == 41) s.Beat(DoorBeat.KeyTurn);        // 0.683 s
        if (commit)
        {
            if (k == 52) s.Beat(DoorBeat.BoltRetract);   // 0.867 s
            if (k == 54) s.Beat(DoorBeat.KeyWithdraw);   // 0.900 s
            if (k == 93 && ended) s.ShotEnded();         // 1.550 s
        }
        else if (k == 45)                                // 0.750 s
        {
            if (beats) { s.Beat(DoorBeat.KeyWithdraw); s.Beat(DoorBeat.Return); }
            if (ended) s.ShotEnded();
        }
    }

    static void Story(Sim s) { Frames(s, 0, StoryFrames, i => StoryFrame(s, i)); }

    // ---------------------------------------------------------------- checks
    static string C0Defaults()
    {
        Require(FrontRoomsKeyHud.UnlockStart == FrontRoomsKeyHud.UnlockStartMode.B_AfterShot && FrontRoomsKeyHud.PanelMode == FrontRoomsKeyHud.PanelGestureMode.D_ShotBeats,
            "the build defaults moved: " + FrontRoomsKeyHud.UnlockStart + " / " + FrontRoomsKeyHud.PanelMode);
        var names = string.Join(",", Enum.GetNames(typeof(FrontRoomsHudSoundCue)));
        Require(names == "PickupFlight,PickupLand,LockTug,UnlockDrop,BrokenBuild,BrokenKnock", "enum values " + names);
        Require(typeof(FrontRoomsHudSoundCue).DeclaringType == null && typeof(FrontRoomsHudSoundCue).Namespace == null, "FrontRoomsHudSoundCue is not top level in the global namespace");
        var ev = typeof(FrontRoomsKeyHud).GetEvent("SoundCue", BindingFlags.Static | BindingFlags.Public);
        Require(ev != null && ev.EventHandlerType == typeof(Action<FrontRoomsHudSoundCue>), "public static event System.Action<FrontRoomsHudSoundCue> FrontRoomsKeyHud.SoundCue is missing");
        var s = new Sim("C0", true);
        try
        {
            Require(s.hud.OwnsDot, "the vector shader did not load (OwnsDot false): run without -nographics");
            Require(s.Wired, "the key HUD did not subscribe to Beat / ShotEnded / Paused");
        }
        finally { s.Dispose(); }
        return FrontRoomsKeyHud.UnlockStart + " / " + FrontRoomsKeyHud.PanelMode + "; enum " + names + "; event " + ev.EventHandlerType.Name + "<FrontRoomsHudSoundCue>";
    }

    static string C1Story()
    {
        var s = new Sim("C1", true);
        try
        {
            Story(s);
            var tRet = s.GetF("tRet");
            RequireSix(s);
            Near(s.at[0], 1.5f, Dt, "PickupFlight (the KP-1b take-off after the 0.5 s hold)");
            Near(s.at[1], 1.5f + FrontRoomsKeyHudMotion.P4Flight, Dt, "PickupLand (the landing, 0.72 s after the take-off)");
            Near(s.at[2], 216 * Dt, 0f, "LockTug (the D.3 window starts on E)");
            Near(s.at[3], tRet + FrontRoomsKeyHud.ReturnDropDelay, Dt, "UnlockDrop (F.1 at the HUD's return + 0.10 s)");
            Require(tRet > (UnlockAt + 87) * Dt && tRet < (UnlockAt + 90) * Dt, "the HUD's return (tRet " + F(tRet) + ") is not where the shot's fade comes down");
            Near(s.at[4], 480 * Dt, 0f, "BrokenBuild (the B.4 build starts)");
            Near(s.at[5], 540 * Dt, 0f, "BrokenKnock (the won't-shut window starts on E)");
            return s.Seq() + "; HUD return " + F(tRet) + " s";
        }
        finally { s.Dispose(); }
    }

    static string C2Presented()
    {
        var detail = new StringBuilder();
        for (var late = 0; late < 2; late++)
        {
            var s = new Sim(late == 0 ? "C2a" : "C2a-late", true);
            try
            {
                var hold = FrontRoomsShotTimings.KeyPickup.HoldUntil;
                var lateBy = late == 0 ? 0f : .3f;
                Frames(s, 0, 180, i =>
                {
                    s.hud.SetHeldKey(i >= 60, Id);
                    s.hud.SetAim(FrontRoomsKeyHud.Aim.None, Id, false, null);
                    if (i == 60) s.hud.PickupFlight(Id, new Vector2(.65f, .3f), .35f, 76f, hold + lateBy, hold);
                });
                Require(s.cues.Count == 2 && s.cues[0] == FrontRoomsHudSoundCue.PickupFlight && s.cues[1] == FrontRoomsHudSoundCue.PickupLand, "cues " + s.Seq());
                Near(s.at[0], 1f, 0f, "PickupFlight on the hand-off frame");
                Near(s.at[1], 1f + FrontRoomsKeyHudMotion.P4Flight - lateBy, Dt, "PickupLand");
                detail.Append(late == 0 ? "" : "; late 0.3 s: ").Append(s.Seq());
            }
            finally { s.Dispose(); }
        }
        return detail.ToString();
    }

    static string C2NoShot()
    {
        var s = new Sim("C2b", true);
        try
        {
            Frames(s, 0, 240, i =>
            {
                s.hud.SetAim(i < 60 ? FrontRoomsKeyHud.Aim.Locked : FrontRoomsKeyHud.Aim.None, Id, true, null);
                s.hud.SetHudAlpha(1f, 0f);
                if (i == 60) s.hud.Unlocking();
            });
            Require(s.cues.Count == 1 && s.cues[0] == FrontRoomsHudSoundCue.UnlockDrop, "cues " + s.Seq());
            Near(s.at[0], 1f + FrontRoomsKeyHudMotion.OpenReact + FrontRoomsKeyHudMotion.OpenBeat, Dt, "UnlockDrop (swing 0.5 s + beat 0.06 s, then F.1)");
            return s.Seq() + " (E at 1.000 s, no takeover fade: (b) falls back to (a))";
        }
        finally { s.Dispose(); }
    }

    static string C2Retrigger()
    {
        var s = new Sim("C2c", true);
        try
        {
            // locked: E at 1.0, 1.2 (inside 0.5 s), 1.6 (a new window); broken: E at 3.0, 3.3 (inside 0.55 s), 3.6
            Frames(s, 0, 300, i =>
            {
                s.hud.SetAim(i < 120 ? FrontRoomsKeyHud.Aim.Locked : i >= 150 && i < 260 ? FrontRoomsKeyHud.Aim.Broken : FrontRoomsKeyHud.Aim.None, Id, false, null);
                if (i == 60 || i == 72 || i == 96) s.hud.NoKey();
                if (i == 180 || i == 198 || i == 216) s.hud.BrokenTry();
            });
            Require(s.CountOf(FrontRoomsHudSoundCue.LockTug) == 2 && s.CountOf(FrontRoomsHudSoundCue.BrokenKnock) == 2 && s.CountOf(FrontRoomsHudSoundCue.BrokenBuild) == 1 && s.cues.Count == 5,
                "cues " + s.Seq());
            Near(s.at[1], 96 * Dt, 0f, "the second LockTug (a new window)");
            Near(s.at[4], 216 * Dt, 0f, "the second BrokenKnock (a new window)");
            return s.Seq();
        }
        finally { s.Dispose(); }
    }

    static string C2Resume()
    {
        var s = new Sim("C2d", true);
        try
        {
            // broken 0 .. 1.0 s, off for 0.1 s (inside the 0.3 s leave), back to 2.0 s, off, back at 3.0 s (after the leave)
            Frames(s, 0, 240, i =>
            {
                var on = i < 60 || (i >= 66 && i < 120) || i >= 180;
                s.hud.SetAim(on ? FrontRoomsKeyHud.Aim.Broken : FrontRoomsKeyHud.Aim.None, Id, false, null);
            });
            Require(s.cues.Count == 2 && s.CountOf(FrontRoomsHudSoundCue.BrokenBuild) == 2, "cues " + s.Seq());
            Near(s.at[0], 0f, 0f, "the first build");
            Near(s.at[1], 180 * Dt, 0f, "the rebuild after the leave");
            return s.Seq() + " (the re-aim at 1.100 s resumed the leave: no cue)";
        }
        finally { s.Dispose(); }
    }

    static string C2Dropped()
    {
        var s = new Sim("C2e", true);
        try
        {
            // the key is announced but not held (the player left the zone): the flight is dropped 0.1 s into its hold
            Frames(s, 0, 180, i =>
            {
                s.hud.SetHeldKey(false, Id);
                s.hud.SetAim(FrontRoomsKeyHud.Aim.None, Id, false, null);
                if (i == 60) s.hud.PickupInstant(Id, 60f);
            });
            Require(s.cues.Count == 0 && s.Get("ps") == "Hidden", "cues " + s.Seq() + ", ps " + s.Get("ps"));
            return "no cue; the panel is " + s.Get("ps");
        }
        finally { s.Dispose(); }
    }

    static string C3ReduceMotion()
    {
        FrontRoomsKeyHud.EditorReduceMotion = 1;
        string story;
        // one sim at a time: SoundCue is static, so a second sim's cues would reach the first one's listener too
        var s = new Sim("C3", true);
        try
        {
            Story(s);
            var tRet = s.GetF("tRet");
            RequireSix(s);
            Near(s.at[0], 1f, 0f, "PickupFlight (no flight: the panel starts to fade in at its slot)");
            Near(s.at[1], 1f + FrontRoomsKeyHudMotion.FadeSeconds, Dt, "PickupLand (the fade-in is complete)");
            Near(s.at[2], 216 * Dt, 0f, "LockTug (no tug is drawn; the cue still plays)");
            Near(s.at[3], tRet, 0f, "UnlockDrop (the open padlock's cross-fade starts at the HUD's return)");
            Near(s.at[4], 480 * Dt, 0f, "BrokenBuild (the cross-fade starts)");
            Near(s.at[5], 540 * Dt, 0f, "BrokenKnock (WON'T SHUT shows)");
            story = s.Seq() + "; HUD return " + F(tRet) + " s";
        }
        finally { s.Dispose(); }
        var noShot = new Sim("C3-noshot", true);
        try
        {
            Frames(noShot, 0, 120, i =>
            {
                noShot.hud.SetAim(i < 30 ? FrontRoomsKeyHud.Aim.Locked : FrontRoomsKeyHud.Aim.None, Id, true, null);
                noShot.hud.SetHudAlpha(1f, 0f);
                if (i == 30) noShot.hud.Unlocking();
            });
            Require(noShot.cues.Count == 1 && noShot.cues[0] == FrontRoomsHudSoundCue.UnlockDrop, "no-shot cues " + noShot.Seq());
            Near(noShot.at[0], 30 * Dt + Dt, Dt, "UnlockDrop without a shot (known after 3 frames without a fade)");
            return story + "; no shot: " + noShot.Seq() + " (E at 0.500 s)";
        }
        finally { noShot.Dispose(); FrontRoomsKeyHud.EditorReduceMotion = 0; }
    }

    static string C4Pause()
    {
        var s = new Sim("C4a", true);
        try
        {
            Frames(s, 0, StoryFrames, i => { if (i == 30) s.Pause(true); StoryFrame(s, i); });
            var during = s.cues.Count;
            // the motions kept running underneath (the HUD's own clock): the unlock ended, the broken door left
            var idle = s.Get("state");
            Frames(s, StoryFrames, StoryFrames + 120, i =>
            {
                if (i == StoryFrames) s.Pause(false);
                s.hud.SetHeldKey(true, Id); s.hud.SetHudAlpha(1f, 0f);
                s.hud.SetAim(i >= StoryFrames + 60 ? FrontRoomsKeyHud.Aim.Broken : FrontRoomsKeyHud.Aim.None, Id, false, null);
            });
            Require(during == 0, "cues under the pause card: " + s.Seq());
            Require(s.cues.Count == 1 && s.cues[0] == FrontRoomsHudSoundCue.BrokenBuild, "after the resume: " + s.Seq());
            Near(s.at[0], (StoryFrames + 60) * Dt, 0f, "the first cue after the resume is a new motion's");
            Require(idle == "Idle", "the motions did not run underneath the pause card (state " + idle + ")");
            return "0 cues in the story under the pause card (state after it " + idle + "); after the resume: " + s.Seq();
        }
        finally { s.Dispose(); }
    }

    static string C4PauseMidFlight()
    {
        var s = new Sim("C4b", true);
        try
        {
            Frames(s, 0, 300, i =>
            {
                s.hud.SetHeldKey(i >= 60, Id);
                s.hud.SetAim(FrontRoomsKeyHud.Aim.None, Id, false, null);
                if (i == 60) s.hud.PickupInstant(Id, 60f);
                if (i == 110) s.Pause(true);     // 1.833 s: in flight (it left at 1.5 s, lands at 2.22 s)
                if (i == 150) s.Pause(false);    // 2.5 s: landed under the pause card
            });
            Require(s.cues.Count == 1 && s.cues[0] == FrontRoomsHudSoundCue.PickupFlight, "cues " + s.Seq());
            Require(s.Get("ps") == "Flight" || s.Get("ps") == "Rest", "ps " + s.Get("ps"));
            return s.Seq() + "; paused 1.833 .. 2.500 s, landed underneath, no PickupLand (ps " + s.Get("ps") + ", glyph at the slot)";
        }
        finally { s.Dispose(); }
    }

    static string C5CloseUp()
    {
        var s = new Sim("C5", true);
        try
        {
            SetInspecting(true);
            Story(s);
            var during = s.Seq();
            Require(s.cues.Count == 0, "cues during the close-up: " + during);
            SetInspecting(false);
            Frames(s, StoryFrames, StoryFrames + 120, i =>
            {
                s.hud.SetHeldKey(true, Id); s.hud.SetHudAlpha(1f, 0f);
                s.hud.SetAim(i >= StoryFrames + 60 ? FrontRoomsKeyHud.Aim.Broken : FrontRoomsKeyHud.Aim.None, Id, false, null);
            });
            Require(s.cues.Count == 1 && s.cues[0] == FrontRoomsHudSoundCue.BrokenBuild, "after the close-up: " + s.Seq());
            return "0 cues while InspectingNow; after it: " + s.Seq();
        }
        finally { SetInspecting(false); s.Dispose(); }
    }

    static string C6Cancel(bool reduceMotion, bool ended)
    {
        if (reduceMotion) FrontRoomsKeyHud.EditorReduceMotion = 1;
        var s = new Sim(reduceMotion ? "C6c" : ended ? "C6a" : "C6b", true);
        try
        {
            var closedAt = "";
            Frames(s, 0, 60 + 240, i =>
            {
                s.hud.SetHeldKey(true, Id);
                if (i < 60) { s.hud.SetAim(FrontRoomsKeyHud.Aim.Locked, Id, true, null); s.hud.SetHudAlpha(1f, 0f); }
                else { s.hud.SetAim(FrontRoomsKeyHud.Aim.None, Id, true, null); ShotFrame(s, i - 60, false, ended, ended); }
                if (i == 60) s.hud.Unlocking();
                if (i == 60 + 120) closedAt = s.Get("state") + " " + s.Get("cs") + " shK " + s.Get("shK");
            });
            Require(s.CountOf(FrontRoomsHudSoundCue.UnlockDrop) == 0, "cues " + s.Seq());
            Require(s.Get("bolt") == "False" && s.Get("state") == "Locked", "the unlock was not cancelled: bolt " + s.Get("bolt") + ", state " + s.Get("state"));
            return "cues " + s.Seq() + "; 1 s after the cancel: " + closedAt + " (the padlock back closed)";
        }
        finally { s.Dispose(); FrontRoomsKeyHud.EditorReduceMotion = 0; }
    }

    static string C6CancelThenCommit()
    {
        var s = new Sim("C6d", true);
        try
        {
            Frames(s, 0, 60 + 240 + 240, i =>
            {
                s.hud.SetHeldKey(true, Id);
                if (i < 60) { s.hud.SetAim(FrontRoomsKeyHud.Aim.Locked, Id, true, null); s.hud.SetHudAlpha(1f, 0f); }
                else if (i < 300) { s.hud.SetAim(FrontRoomsKeyHud.Aim.None, Id, true, null); ShotFrame(s, i - 60, false, true, true); }
                else { s.hud.SetAim(FrontRoomsKeyHud.Aim.None, Id, true, null); ShotFrame(s, i - 300, true, true, true); }
                if (i == 60 || i == 300) s.hud.Unlocking();
            });
            Require(s.cues.Count == 1 && s.cues[0] == FrontRoomsHudSoundCue.UnlockDrop, "cues " + s.Seq());
            Near(s.at[0], s.GetF("tRet") + FrontRoomsKeyHud.ReturnDropDelay, Dt, "UnlockDrop of the committed press");
            return s.Seq() + " (cancelled at 1.750 s, pressed again at 5.000 s)";
        }
        finally { s.Dispose(); }
    }

    static object gcKeep;
    static string C7Heap()
    {
        int gcControl, gc;
        var control = HeapOver(true, out gcControl);
        var cuesBefore = counted;
        var bytes = HeapOver(false, out gc);
        var raised = counted - cuesBefore;
        Require(control > 0, "the positive control (100 B per frame) did not show: +" + control + " bytes; the measure is blind");
        Require(raised >= 6, "the measured frames raised only " + raised + " cues");
        Require(bytes == 0 && gc == 0, "+" + bytes + " bytes, gen-0 " + gc + " over 1200 frames (" + raised + " cues)");
        return "control +" + control + " bytes (gen-0 " + gcControl + "); the story with a listener: +" + bytes + " bytes, gen-0 " + gc + ", " + raised + " cues in 1200 frames";
    }

    // 60 warm frames, then 1200 measured frames of the story (looped): the change of the heap in use (GC.GetTotalMemory, in
    // its block steps) and the gen-0 collections over the 1200 frames (the HUD harness's measure, fix round 2)
    static long HeapOver(bool control, out int collections)
    {
        var s = new Sim(control ? "C7-control" : "C7", false);
        try
        {
            for (var i = 0; i < 60; i++) { StoryFrame(s, i % StoryFrames); s.Frame(i); }
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            var c0 = GC.CollectionCount(0); var h0 = GC.GetTotalMemory(false);
            for (var i = 60; i < 60 + 1200; i++)
            {
                StoryFrame(s, i % StoryFrames); s.Frame(i);
                if (control) gcKeep = new byte[100];
            }
            var h1 = GC.GetTotalMemory(false);
            collections = GC.CollectionCount(0) - c0;
            return h1 - h0;
        }
        finally { s.Dispose(); gcKeep = null; }
    }

    // ---------------------------------------------------------------- helpers
    // O-42 (b), #41: the P4 slide-in of a KP-1b pickup, sampled after every frame. Below LabelKeylineFrom the label's
    // outline has alpha 0 (no dark ghost under the translucent word); at rest it is the shared tap alpha; between, it rises.
    static string C8LabelKeyline()
    {
        var s = new Sim("C8", false);
        try
        {
            var o = (UnityEngine.UI.Outline)typeof(FrontRoomsKeyHud).GetField("labelOutline", NP).GetValue(s.hud);
            Require(o != null, "no label outline");
            var tap = FrontRoomsHudStyle.OutlineTapAlpha;
            int below = 0, ramp = 0, rest = 0;
            var lastW = -1f; var lastLab = -1f;
            for (var i = 0; i < 300; i++)
            {
                s.hud.SetHeldKey(i >= 60, Id);
                s.hud.SetAim(FrontRoomsKeyHud.Aim.None, Id, false, null);
                if (i == 60) s.hud.PickupInstant(Id, 60f);
                s.hud.SetHudAlpha(1f, 0f);
                s.Frame(i);
                if (i < 60) continue;
                var lab = s.GetF("labelA");
                var a = o.effectColor.a;
                if (lab > 0f && lab < FrontRoomsKeyHud.LabelKeylineFrom - .001f) { Require(a == 0f, "keyline " + F(a) + " at label alpha " + F(lab) + " (frame " + i + ")"); below++; }
                else if (lab > 0f)
                {
                    // the debug line rounds the word's alpha to 0.001, so the expected keyline is good to about 0.002
                    var want = tap * Mathf.Clamp01((lab - FrontRoomsKeyHud.LabelKeylineFrom) / (1f - FrontRoomsKeyHud.LabelKeylineFrom));
                    Require(Mathf.Abs(a - want) < .003f, "keyline " + F(a) + " vs " + F(want) + " at label alpha " + F(lab) + " (frame " + i + ")");
                    if (lab >= .9995f) rest++; else ramp++;
                }
                if (lab >= lastLab && lastLab >= 0f) Require(a >= lastW - 1e-5f, "keyline fell while the word rose (frame " + i + ")");
                lastW = a; lastLab = lab;
            }
            Require(below >= 1 && ramp >= 1 && rest >= 1, "slide-in not seen: below " + below + ", ramp " + ramp + ", rest " + rest);
            Require(Mathf.Abs(o.effectColor.a - tap) < 1e-6f, "keyline " + F(o.effectColor.a) + " at rest after the slide-in (tap " + F(tap) + ")");
            return "P4 slide-in: " + below + " frames below " + F(FrontRoomsKeyHud.LabelKeylineFrom) + " with keyline 0, " + ramp + " ramp frames, " + rest + " rest frames at tap " + F(tap);
        }
        finally { s.Dispose(); }
    }

    // German: its table holds these rows and Oswald Bold is in Resources, so the check needs no CJK font. Changed is raised by
    // hand: FrontRoomsLanguage.Pick would save the pick to this machine's PlayerPrefs.
    static string C9Language()
    {
        var english = FrontRoomsLanguage.All[0];
        var german = FrontRoomsLanguage.Find("de");
        var s = new Sim("C9", false);
        try
        {
            Func<string, UnityEngine.UI.Text> run = n => (UnityEngine.UI.Text)typeof(FrontRoomsKeyHud).GetField(n, NP).GetValue(s.hud);
            var a = run("l1a"); var dot = run("l1dot"); var b = run("l1b"); var two = run("l2");
            Action<int, bool> aim = (start, have) => Frames(s, start, start + 30, i => s.hud.SetAim(FrontRoomsKeyHud.Aim.Locked, Id, have, null));
            Action<FrontRoomsLanguage.Language> change = l =>
            {
                FrontRoomsLanguage.PreviewForTest(l);
                var raised = typeof(FrontRoomsLanguage).GetField("Changed", SNP).GetValue(null) as Action;
                if (raised != null) raised();
            };
            aim(0, true);
            var font = a.font; var size = a.fontSize; var ax = a.rectTransform.anchoredPosition.x; var bx = b.rectTransform.anchoredPosition.x;
            Require(a.text == "E  " && dot.text == "\u00B7" && b.text == "  UNLOCK" && two.text == "STAIR KEY", "English: \"" + a.text + dot.text + b.text + "\" / " + two.text);

            change(german);
            aim(30, true);
            var unlock = FrontRoomsLanguage.T("UNLOCK");
            Require(unlock != "UNLOCK", "the German table has no UNLOCK");
            Require(a.text == "E  " && dot.text == "\u00B7" && b.text == "  " + unlock, "the control stays and the verb is the table's: \"" + a.text + dot.text + b.text + "\"");
            Require(a.font != font && a.font == b.font && a.font.name.StartsWith("Oswald", StringComparison.Ordinal), "the label face: " + a.font.name);
            Require(a.fontSize == Mathf.RoundToInt(size * german.LabelScale) && dot.font.name.StartsWith("IBMPlexMono", StringComparison.Ordinal), "size " + a.fontSize + ", dot in " + dot.font.name);
            var gx = b.rectTransform.anchoredPosition.x;
            Require(a.rectTransform.anchoredPosition.x < ax && Mathf.Abs(gx - bx) > 1f, "the runs were not measured again: a " + F(a.rectTransform.anchoredPosition.x) + ", b " + F(gx));
            Frames(s, 60, 120, i => s.hud.SetAim(FrontRoomsKeyHud.Aim.None, Id, false, null));
            aim(120, false);
            Require(a.text == FrontRoomsLanguage.T("LOCKED") && a.text != "LOCKED", "LOCKED: " + a.text);
            Require(run("meta").text == FrontRoomsLanguage.T("OPENS THE STAIR DOOR"), "the panel's fixed line follows the language");

            change(english);
            Frames(s, 150, 210, i => s.hud.SetAim(FrontRoomsKeyHud.Aim.None, Id, false, null));
            aim(210, true);
            Require(a.text == "E  " && b.text == "  UNLOCK" && a.font == font && a.fontSize == size, "English back: \"" + a.text + b.text + "\" in " + a.font.name + " " + a.fontSize);
            Require(a.rectTransform.anchoredPosition.x == ax && b.rectTransform.anchoredPosition.x == bx, "English back: a " + F(a.rectTransform.anchoredPosition.x) + " b " + F(b.rectTransform.anchoredPosition.x) + ", was " + F(ax) + " " + F(bx));
            return "de: \"E . " + unlock + "\" in " + "Oswald, b at " + F(gx) + " (English " + F(bx) + "); English back in Bayon at the same x";
        }
        finally { FrontRoomsLanguage.PreviewForTest(english); s.Dispose(); }
    }

    static void RequireSix(Sim s)
    {
        var ok = s.cues.Count == SixInOrder.Length;
        for (var i = 0; ok && i < SixInOrder.Length; i++) ok = s.cues[i] == SixInOrder[i];
        Require(ok, "want PickupFlight, PickupLand, LockTug, UnlockDrop, BrokenBuild, BrokenKnock once each; got " + s.Seq());
    }

    // at = want, within [want - 1e-4, want + slack + 1e-4] (slack: the frame the motion's own predicate turns true on)
    static void Near(float at, float want, float slack, string what)
    {
        Require(!float.IsNaN(at) && at >= want - 1e-4f && at <= want + slack + 1e-4f, what + ": at " + F(at) + " s, want " + F(want) + (slack > 0f ? " .. " + F(want + slack) : "") + " s");
    }

    static void SetInspecting(bool on)
    {
        // the private setter only (as the game sets it): FrontRooms3DGame.InspectingNow, read by the cue gate
        var p = typeof(FrontRooms3DGame).GetProperty("InspectingNow", BindingFlags.Static | BindingFlags.Public);
        if (p != null && p.GetSetMethod(true) != null) p.GetSetMethod(true).Invoke(null, new object[] { on });
    }

    static void Run(string name, Func<string> test)
    {
        string detail; var pass = true;
        // the device's settings never decide a check: Reduce Motion off (C3 / C6c turn it on), Reduce Flashing off
        FrontRoomsKeyHud.EditorReduceMotion = 0; FrontRoomsKeyHud.EditorReduceFlashing = 0;
        try { detail = test(); }
        catch (Exception e) { pass = false; detail = e.GetType().Name + ": " + e.Message; Log.AppendLine(e.ToString()); }
        finally { FrontRoomsKeyHud.EditorReduceMotion = -1; FrontRoomsKeyHud.EditorReduceFlashing = -1; }
        Results.Add(new Result { name = name, pass = pass, detail = detail });
        Log.AppendLine((pass ? "PASS " : "FAIL ") + name + " | " + detail);
        Debug.Log("[KeyHudSoundCueTests] " + (pass ? "PASS " : "FAIL ") + name + " | " + detail);
    }

    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    static string Q(string s) { return "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ") + "\""; }
    static string F(float f) { return f.ToString("0.000", CultureInfo.InvariantCulture); }
}
