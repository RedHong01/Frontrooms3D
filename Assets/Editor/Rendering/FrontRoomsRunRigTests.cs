using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Run! B (EMERGENCY POWER) rig checks, gate T2 of research/room_visuals/run_b_production/10_plan.md.
/// Edit mode, headless, no menu item and no InitializeOnLoad hook:
///   Unity -batchmode -projectPath <project> -buildTarget OSXUniversal -executeMethod FrontRoomsRunRigTests.RunBatch -quit [-runRigTestOut dir]
/// Default output: <project>/Verification/run_b/tests (tests.json, log.txt).
///   R1  Evaluate(t) is deterministic (two rigs, any order of calls).
///   R2  photosafety per lamp (WCAG 2.3.1 general flash: a change of 0.1 from the last extreme, darker side
///       below 0.8, a flash is a pair): trip, animated reset, stutter tube, stage-1 warning; at most 3 a second.
///   R3  REDUCE FLASHING: envelopes of at least 0.5 s, no black gap (the brighter state never under 15 %), no flash
///       pair (each lamp changes once, monotone), a monotone re-strike, a steady stutter.
///   R4  the trip is one-shot; R5  the reset is idempotent; R6  Tick allocates nothing.
///   R7  corridor light budget (plan section 3, C41 + C42, F7): armed 10 (9 wraps + the goal window light; every wrap
///       shadowed: 9), blackout 1 (the goal window light), tripped 9 (8 heads shadowed + the goal window light).
///   R8  C41: no EXIT sign anywhere in the room (no FrontRoomsExitSign, no Kit_ExitSign*); the red fire gear stays.
///   R9  C42: the goal window light: unshadowed, lit from the trip line though past the 16 m cull, off as the far lamp comes
///       on (FarRoomLamp 1), half at 0.5, its no-lamp fade near the door, off once the viewer is past the goal wall,
///       untouched by the trip.
///   R10 side doors on the RN-K DA kits: 3 frames + 3 leaves, every lite DEAD, no closer, the frame inside the wall.
///   R11 CR-4 warning burst on the wraps: at most 3 flashes a second, ends armed and whole; REDUCE FLASHING one sag >= 0.69.
///   R12 the map skin (skinShell, the binder's layout): the soffit skin, the arch reveal on the room's half of the wall
///       (2 jambs x lower tone, cove, upper tone + the head), none at the goal door, and the lower tone running on behind
///       the extinguisher cabinet (the map's CR-3 finish shows nowhere).
///   R13 F1 (stage C): nothing culls while the viewer is in the room (trip line: 8 heads + the goal window light); 25 m
///       outside the arch only the goal head runs; farther than the box fade, each light follows the map's 16 m / 3 m fade.
///   R14 F3 (stage C): the reset seen as the frame sees it: the whole corridor and every run of 3 neighbouring wraps, the
///       window's mean against its own maximum, at most 3 flashes a second (the design gives 2); one flick per wrap.
///   R15 step 10 + C39 (stage C): every gateable material on the rig's and the dress's renderers is a gated twin with the
///       room's box in its block; the NOTICE plate outside is not; the goal opening is the lite while shut, the door open.
///   R16 F7 + C39 (stage C): three Run_B volumes (main blend 0.3, two mouths blend 1); a camera in a side neighbour
///       (1.88 m off the axis) is beyond every volume's reach; the Run_B profile overrides every parameter the Office grade
///       does, and SMH / film grain / chromatic aberration with the global look's values (a Level 0 host is unchanged).
///   R17 F8 + F9 + F2 + F6 (stage C): the NOTICE plate under the room root on the host face, the overhead services cast no
///       shadow, the chair and cart albedo well under the floor's, the five LOD groups switching at the new distances.
///   R18 card 1 (71 s.9, the rig in cells): today's room unchanged: Corridor(9) and Corridor1x9 equal the preset as it landed
///       (frozen below) field for field, bit for bit, in three width pairs; the dress (look-dev shell and the map skin) and the
///       rig built from each place the same pieces and lights; the binder's CellsOf reads 9 from today's record and by default.
///   R19 card 1, 7 .. 14 cells: a strip every cell, units every 6 m from z 3 (one 9 m interval, the last, at an even count), the
///       goal unit at L - 6, the goal at L, probes by segment covering 0 .. L on the 9-cell bake, an obstacle in every pool but
///       the goal's (lane >= 1.4 m), the side doors and cabinets by rule, frames only when asked (every third cell line), the
///       lights (armed n + 1 / n shadowed, tripped 2U + 1 / 2U), the trip and the reset photosafe, the reset n x 0.15 s of sweep
///       from the entrance (ticked); 6 and 15 cells refused at 9. The COST line per length goes to log.txt.
///   R20 card 2 (71 s.4-s.5, the recede, head first): the schedule's seats (T2 R 9: 1.55 / 3.35 s, R 15: 1.55 / 2.90 / 4.25 / 6.05 s;
///       T1 R 15: 1.15 / 2.15 / 3.15 / 4.85 s) and peaks, the glide (6 m/s); Receding(9, 12 | 14) by rule (FD1 / FD2 only, no side door,
///       units clear of the frames, the carried goal unit, cabinets in the fixed length, refusals); a built 9 -> 12 room: the hidden strips
///       and cells, the trip and the stepped recede (seats, events in order, the strips striking at their seats, one-way), the keep-away
///       glide (a walker 5 m from the end at the gate), the gaze gate (shut facing away, open facing the goal), the 8-byte record and
///       Restore; a plain 9-cell room has no goal mount, no hidden strip and its gate on z 27.
///   (S1 / S2 / S3, the title-stream checks, were dropped with the RoomStream hunk: call 6 / C40, 2026-10-31.)
///   R21 card 2, the T2 body (71 s.5.3): the four clips (69 blocks / 28 strips, T2's phases, rest at both ends, courses landing
///       0 -> 12 in order); a built 9 -> 12 room re-stacks at every step (the body shown from the tell to the end of the seat, its
///       materials in the room's gate, the static wall whole in every dwell and after the final seat, 26 landings bottom up, the
///       grit on desktop); the glide and a restore ride whole and quiet; iPad blocks without grit, phones the strips; a plain room
///       builds none of it.
///   W1  the WebGL tier is off in the editor (the FRONTROOMS_WEBGL_PREVIEW compile is checked outside Unity).
///   W2  step 9 (stage C): the handheld tier (ForceHandheldForTools) builds 5 head + 5 wrap Lights, hard shadows, no beams,
///       the pools and the goal kept, photosafe; the desktop build before and after it is the same, light for light.
///   R22 card 4 (71 s.7.2, decision 8c: the Run door's tell): every Corridor(n) keeps today's arch; UseEntranceDoor (both hands) makes
///       the entrance the door's opening (1.0 x 2.1), the goal face and everything else untouched, the NOTICE plate on the hinge side;
///       the map skin builds no arch reveal at a Run door and closes the wall over it from 2.1 m; the gate's entrance portal is none
///       while the leaf is shut and the whole door while it is open (an arch ignores it); the entrance tell (DressEntranceTell): the
///       card on the latch side clear of the frame, the detector at the door head's sight-rule depth, past the 2.5 m trip, clear of
///       the strips (the arch's as landed); the light under the door: one draw, zero lights, on the approach side only, cool white
///       armed, black in the trip's gap, back with the reset, off as the leaf opens, no allocation per Tick.
///   R23 card 3 (71 s.6.3-6.7, decision 6: the red looks): the default look is B (it passed the project's own WCAG 2.3.1 check,
///       Tools/lookdev/flash_check.py, Red 11-04 in 60_production_66.md C3.10; A is the fallback); armed is the same in Today, A and B; A's front (each strip's white out as the front passes at 20 m/s, its red struck
///       0.35 s later at 0.35 rising to 1 over 0.4 s, the pool heads in its wake, the goal head as built; all red at about 2 s; the strip
///       Light at 3.5 in the typed red, the red tube at redTubeLinear, the halo in its red, 18 lights on and 17 shadowed); B's pulses
///       (every 2.5 s from 3 s, a dip to 0.35, 12 events, held until ReleasePulses); Reduce Flashing (a 0.5 s cross-fade, no band, no
///       pulse); photosafety per lamp; the reset white again; TripImmediate quiet; the handheld tier's scale; the body's lamp registry
///       gets each strip's Light; no allocation per Tick. The tests above run with DefaultRedLook = Today (the trip as landed).
///   R24 RB (the level-design line's card 4 in the binder, 71 s.4-s.5): on a real map (the Level Designer's preview, the game's
///       builder): with runRooms off nothing binds; a plain room builds no end, holds no pulse and keeps no record; a 9 -> 12 room
///       is built receding (the end before the rig, B's pulses held, the goal tell on the deepest line, its detector off in the last
///       hidden cell); the map's trip trips the end; each SeatReached seats the map on its line in cells (the map's RunRoomSeated,
///       its open cells, its goal point); the final seat frees B's pulses (none before it, the first a second after it); the record
///       lives in the map's room; a room dropped before its end moved comes back unmoved and waiting, one dropped mid-recede or after
///       it comes back seated with no event (the map seated once by the binder, or not at all when it already was); a 9 -> 14
///       glide seats the map once, on its last line.
/// The rig needs the B kits (Kit_EmergencyTwinHead*, Kit_WrapFixture_4ft, the RN-K DA door kits); without them it builds
/// named stand-ins and the light counts still hold.
/// </summary>
public static class FrontRoomsRunRigTests
{
    const float Dt = 1f / 60f;
    static readonly StringBuilder Log = new StringBuilder();
    sealed class Result { public string name; public bool pass; public string detail; }
    static readonly List<Result> Results = new List<Result>();
    static string outDir;

    public static void RunBatch()
    {
        var args = Environment.GetCommandLineArgs();
        outDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "run_b", "tests");
        for (var i = 0; i < args.Length - 1; i++) if (args[i] == "-runRigTestOut") outDir = args[i + 1];
        Directory.CreateDirectory(outDir);
        Log.Length = 0;
        Results.Clear();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        FrontRoomsKitLibrary.ClearCache();
        var reducedBefore = FrontRoomsSettings.ReduceFlashing;
        // Card 3: R1-R22 check the trip as landed (Today); R23 checks the red looks and the shipped default.
        var lookBefore = FrontRoomsRunRig.DefaultRedLook;
        shippedLook = lookBefore;
        try
        {
            FrontRoomsRunRig.DefaultRedLook = RunRedLook.Today;
            SetReduced(false);
            Run("R1 Evaluate(t) is deterministic", R1Deterministic);
            Run("R2 photosafety per lamp: trip, reset, stutter, warning", R2Photosafety);
            Run("R3 REDUCE FLASHING: no black gap, no flash", R3Reduced);
            Run("R4 the trip is one-shot", R4OneShot);
            Run("R5 the reset is idempotent", R5Reset);
            Run("R6 Tick allocates nothing", R6Alloc);
            Run("R7 corridor light budget (armed 10/9, blackout 1, tripped 9/8)", R7Budget);
            Run("R8 C41: no EXIT sign in the room, the fire gear stays", R8NoExitSign);
            Run("R9 C42: the goal window light", R9GoalLight);
            Run("R10 side doors: RN-K DA kits, dead lites, no closer", R10SideDoors);
            Run("R11 CR-4 warning burst on the wraps", R11WarnBurst);
            Run("R12 the map skin: soffit, arch reveal, lower tone behind the cabinet", R12MapSkin);
            Run("R13 F1: the cull keeps the room whole, the goal head never culls", R13Cull);
            Run("R14 F3: the reset as the frame sees it (one flick per wrap)", R14ResetWindows);
            Run("R15 step 10 + C39: the light gate on every Run! renderer", R15Gate);
            Run("R16 F7 + C39: the Run_B grade stays in the room, host-neutral", R16Post);
            Run("R17 F8 F9 F2 F6: plate outside, overhead unshadowed, dark obstacles, LOD distances", R17Dress);
            Run("R18 card 1: Corridor(9) is today's room, field for field and piece for piece", R18TodayUnchanged);
            Run("R19 card 1: 7 .. 14 cells by rule, photosafe, reset n x 0.15 s", R19Cells);
            Run("R20 card 2: the receding end (schedule, layout, keep-away, gate, record), today untouched", R20Recede);
            Run("R21 card 2 T2: the re-stack (kits, clips, a step flies and seats, tiers, glide and restore whole)", R21Restack);
            Run("W1 WebGL tier off in the editor", W1Gate);
            Run("W2 step 9: the handheld tier, desktop unchanged", W2Handheld);
            Run("R22 card 4: the Run door entrance (opening, skin, gate, tell, the light under the door), today's arch kept", R22DoorTell);
            Run("R23 card 3: the red looks (B default, A the fallback; front, pulse, Reduce Flashing, photosafe, registry)", R23RedLooks);
            Run("R24 RB: the receding room on the map (build, trip, seats -> RunSeated, pulses, record, drop and rebuild), plain and off untouched", R24RecedeBinder);
        }
        finally
        {
            SetReduced(reducedBefore);
            FrontRoomsRunRig.DefaultRedLook = lookBefore;
            FrontRoomsRunRig.ForceHandheldForTools = false;
        }
        var passed = 0;
        foreach (var r in Results) if (r.pass) passed++;
        var json = new StringBuilder("{\n \"results\": [\n");
        for (var i = 0; i < Results.Count; i++)
            json.Append("  {\"name\": ").Append(Q(Results[i].name)).Append(", \"pass\": ").Append(Results[i].pass ? "true" : "false")
                .Append(", \"detail\": ").Append(Q(Results[i].detail)).Append(i < Results.Count - 1 ? "},\n" : "}\n");
        json.Append(" ],\n \"passed\": ").Append(passed).Append(",\n \"failed\": ").Append(Results.Count - passed).Append("\n}\n");
        File.WriteAllText(Path.Combine(outDir, "tests.json"), json.ToString());
        File.WriteAllText(Path.Combine(outDir, "log.txt"), Log.ToString());
        Debug.Log("[RunRigTests] " + passed + " passed, " + (Results.Count - passed) + " failed | " + outDir);
    }

    static string Q(string s) { return "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ") + "\""; }
    static string F(float f) { return f.ToString("0.###", CultureInfo.InvariantCulture); }

    static void Run(string name, Func<string> test)
    {
        string detail; var pass = true;
        try { detail = test(); }
        catch (Exception e) { pass = false; detail = e.GetType().Name + ": " + e.Message; Log.AppendLine(e.ToString()); }
        Results.Add(new Result { name = name, pass = pass, detail = detail });
        Log.AppendLine((pass ? "PASS " : "FAIL ") + name + " | " + detail);
        Debug.Log("[RunRigTests] " + (pass ? "PASS " : "FAIL ") + name + " | " + detail);
    }

    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }

    // ------------------------------------------------------------------ card 4 (71 s.7.2 and s.9, decision 8c): the Run door's tell
    static bool SameFace(RunWallFace a, RunWallFace b)
    {
        if (a.start != b.start || a.along != b.along || a.normal != b.normal || !Mathf.Approximately(a.length, b.length)) return false;
        var ga = a.gaps ?? new Vector2[0];
        var gb = b.gaps ?? new Vector2[0];
        if (ga.Length != gb.Length) return false;
        for (var i = 0; i < ga.Length; i++) if (ga[i] != gb[i] || !Mathf.Approximately(a.GapTop(i), b.GapTop(i))) return false;
        return true;
    }

    static Vector3 NoticeOf(RunRigLayout l)
    {
        for (var i = 0; i < l.wallItems.Length; i++)
            if (l.wallItems[i].kit == RunRigLayout.NoticePlateKit && l.wallItems[i].point.z < 0f) return l.wallItems[i].point;
        throw new Exception("no NOTICE plate on the host face");
    }

    static Vector4 GateD(Transform dress)
    {
        var mpb = new MaterialPropertyBlock();
        foreach (var r in dress.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (var k = 0; k < mats.Length; k++)
                if (mats[k] != null && FrontRoomsRunGate.IsTwin(mats[k]))
                {
                    r.GetPropertyBlock(mpb, k);
                    return mpb.GetVector("_FR_RunGateD");
                }
        }
        throw new Exception("no gated dress renderer");
    }

    static string R22DoorTell()
    {
        var sb = new StringBuilder();
        // 1. Today: every length keeps the arch (the map builds arches only).
        for (var n = RunRigLayout.MinCells; n <= RunRigLayout.MaxCells; n++)
        {
            var c = RunRigLayout.Corridor(n, 1.4f, 1f);
            Require(!c.entranceDoor && Mathf.Approximately(c.archHalf, .7f) && Mathf.Approximately(c.archTop, 2.2f), n + " cells: the entrance is not today's arch");
        }
        var arch = RunRigLayout.Corridor1x9(1.4f, 1f);
        var archNotice = NoticeOf(arch);
        Require(Mathf.Abs(archNotice.x - 1.1f) < 1e-4f, "today's NOTICE plate moved: x " + F(archNotice.x));
        // 2. UseEntranceDoor, both hands: the door's opening, the goal face and the rest untouched, the plate on the hinge side.
        for (var hand = -1; hand <= 1; hand += 2)
        {
            var l = RunRigLayout.Corridor1x9(1.4f, 1f);
            l.UseEntranceDoor(1f, hand);
            Require(l.entranceDoor && l.entranceHinge == hand && Mathf.Approximately(l.archHalf, .5f) && Mathf.Approximately(l.archTop, 2.1f),
                "UseEntranceDoor(" + hand + "): opening " + F(l.archHalf) + " / " + F(l.archTop));
            Require(l.walls.Length == arch.walls.Length, "faces changed");
            var entry = 0;
            for (var i = 0; i < l.walls.Length; i++)
            {
                var shortFace = Mathf.Abs(l.walls[i].along.x) > .5f;
                var isEntry = shortFace && l.walls[i].start.z < 1f;
                if (!isEntry) { Require(SameFace(l.walls[i], arch.walls[i]), "face " + i + " changed"); continue; }
                entry++;
                var g = l.walls[i].gaps;
                Require(g != null && g.Length == 1 && Mathf.Abs(g[0].x - (l.halfWidth - .5f)) < 1e-5f && Mathf.Abs(g[0].y - (l.halfWidth + .5f)) < 1e-5f
                    && Mathf.Abs(l.walls[i].GapTop(0) - 2.1f) < 1e-5f, "the entrance face's opening is not the door's");
            }
            Require(entry == 1, entry + " entrance faces");
            var p = NoticeOf(l);
            Require(Mathf.Abs(p.x - hand * .9f) < 1e-4f && Mathf.Abs(p.y - archNotice.y) < 1e-5f && Mathf.Abs(p.z - archNotice.z) < 1e-5f, "NOTICE plate at " + p);
            Require(l.length == arch.length && l.units.Length == arch.units.Length && l.ownLamps.Length == arch.ownLamps.Length && l.sideDoors.Length == arch.sideDoors.Length,
                "the door changed the corridor");
        }
        sb.Append("Corridor(7..14) arch kept; door 1.0 x 2.1 both hands, plate +-0.9");
        // 3. The map skin: no arch reveal at a Run door, the wall closed over it from 2.1 m.
        var skinRoot = new GameObject("TEST run door skin").transform;
        try
        {
            skinRoot.position = new Vector3(4992f, 0f, 4992f);
            var l = RunRigLayout.Corridor1x9(1.4f, 1f);
            l.buildShell = false;
            l.skinShell = true;
            l.UseEntranceDoor(1f, -1);
            FrontRoomsRunDress.Build(skinRoot, l);
            int reveal = 0, header = 0;
            foreach (var t in skinRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("run b arch reveal", StringComparison.Ordinal)) reveal++;
                if (t.name == "run b upper tone header")
                {
                    var b = t.GetComponent<Renderer>().bounds;
                    var lo = skinRoot.InverseTransformPoint(b.min);
                    var hi = skinRoot.InverseTransformPoint(b.max);
                    if (Mathf.Max(lo.z, hi.z) < 1f && Mathf.Abs(Mathf.Min(lo.y, hi.y) - 2.1f) < .002f && Mathf.Abs(hi.x - lo.x) > .99f) header++;
                }
            }
            Require(reveal == 0 && header == 1, "Run door skin: " + reveal + " arch reveal slabs, " + header + " headers from 2.1 m over the door");
            sb.Append("; skin: 0 reveal, header from 2.1");
        }
        finally { UnityEngine.Object.DestroyImmediate(skinRoot.gameObject); }
        // 4. The gate, the tell and the light under the door on a built Run door room (look-dev shell).
        var root = new GameObject("TEST run door room").transform;
        var archRoot = new GameObject("TEST run arch room").transform;
        try
        {
            root.position = new Vector3(4992f, 0f, 4992f);
            archRoot.position = new Vector3(4892f, 0f, 4992f);
            var l = RunRigLayout.Corridor1x9(1.4f, 1f);
            l.UseEntranceDoor(1f, 1);
            var dress = FrontRoomsRunDress.Build(root, l);
            var entranceDetector = FrontRoomsRunRigBinder.DressEntranceTell(root, dress, l);
            var rig = FrontRoomsRunRig.Create(root, l, RunRigHost.OwnLamps, null, dress);
            var al = RunRigLayout.Corridor1x9(1.4f, 1f);
            var adress = FrontRoomsRunDress.Build(archRoot, al);
            var archDetector = FrontRoomsRunRigBinder.DressEntranceTell(archRoot, adress, al);
            var arig = FrontRoomsRunRig.Create(archRoot, al, RunRigHost.OwnLamps, null, adress);
            // gate
            var d = GateD(dress);
            Require(!rig.EntranceOpen && d == Vector4.zero, "shut Run door: D " + d);
            rig.SetEntranceOpen(true);
            d = GateD(dress);
            Require(rig.EntranceOpen && Mathf.Abs(d.y - .5f) < 1e-5f && Mathf.Abs(d.z - 2.1f) < 1e-5f && Mathf.Abs(d.x) < 1e-5f, "open Run door: D " + d);
            rig.SetEntranceOpen(false);
            Require(GateD(dress) == Vector4.zero, "shut again: D " + GateD(dress));
            var ad = GateD(adress);
            arig.SetEntranceOpen(true);
            arig.SetEntranceOpen(false);
            var ad2 = GateD(adress);
            Require(Mathf.Abs(ad.y - .7f) < 1e-5f && Mathf.Abs(ad.z - 2.2f) < 1e-5f && ad == ad2, "the arch's portal moved: " + ad + " / " + ad2);
            sb.Append("; gate shut 0 / open (0, .5, 2.1) / arch (0, .7, 2.2) kept");
            // tell
            Transform card = null, acard = null;
            foreach (Transform t in root) if (t.name == FrontRoomsAlarmTell.CardName) card = t;
            foreach (Transform t in archRoot) if (t.name == FrontRoomsAlarmTell.CardName) acard = t;
            Require(card != null && entranceDetector != null && acard != null && archDetector != null, "the entrance tell is missing");
            var cx = .5f + FrontRoomsAlarmTell.FrameFace + FrontRoomsAlarmTell.CardGap + FrontRoomsAlarmTell.CardHalfWidth;
            var cp = card.localPosition;
            Require(Mathf.Abs(cp.x + cx) < 1e-4f && Mathf.Abs(cp.y - FrontRoomsAlarmTell.CardCentreHeight) < 1e-4f && Mathf.Abs(cp.z + l.WallHalf) < 1e-4f,
                "the card is not on the latch side (hinge right, latch left): " + cp);
            Require(Mathf.Abs(cp.x) - FrontRoomsAlarmTell.CardHalfWidth >= .5f + .0488f + .05f, "the card is not clear of the frame's casing");
            Require(Mathf.Sign(cp.x) != Mathf.Sign(NoticeOf(l).x), "the card and the NOTICE plate are on the same side");
            var dz = entranceDetector.transform.localPosition.z;
            var stripHalf = FrontRoomsRunRig.OwnLampHalfLength(l) + FrontRoomsRunRigBinder.DetectorClear;
            for (var k = 0; k < l.ownLamps.Length; k++) Require(Mathf.Abs(dz - l.ownLamps[k].z) >= stripHalf - 1e-4f, "the detector touches strip " + k);
            Require(FrontRoomsAlarmTell.SeenFromOutside(dz, l.wallThickness, l.height, FrontRooms.Map.ModuleUnits.DoorHeight) && dz > 2.5f && Mathf.Abs(entranceDetector.transform.localPosition.x) < 1e-4f,
                "the Run door's detector at z " + F(dz));
            var acp = acard.localPosition;
            var adz = archDetector.transform.localPosition.z;
            Require(Mathf.Abs(acp.x + (.7f + FrontRoomsAlarmTell.FrameFace + FrontRoomsAlarmTell.CardGap + FrontRoomsAlarmTell.CardHalfWidth)) < 1e-4f && Mathf.Abs(adz - 2.71f) < .02f,
                "today's arch tell moved: card " + acp + ", detector z " + F(adz));
            sb.Append("; tell: card x ").Append(F(cp.x)).Append(", detector z ").Append(F(dz)).Append(" (arch ").Append(F(acp.x)).Append(", ").Append(F(adz)).Append(")");
            // the light under the door
            int total0, on0, sh0;
            rig.CountLights(out total0, out on0, out sh0);
            var gap = FrontRoomsRunDoorGap.Build(rig, root);
            int total1, on1, sh1;
            rig.CountLights(out total1, out on1, out sh1);
            Require(gap != null && total1 == total0 && root.GetComponentsInChildren<Light>(true).Length == Lights(rig).Length, "the light under the door added a Light");
            var gr = gap.Renderer;
            Require(gr != null, "FrontRooms/RunB/DoorGap is missing (Resources/Rendering/RunDoorGap.shader)");
            Require(gr.shadowCastingMode == ShadowCastingMode.Off && gr.sharedMaterials.Length == 1, "the strip is not one unshadowed draw");
            var mesh = gr.GetComponent<MeshFilter>().sharedMesh;
            foreach (var v in mesh.vertices)
                Require(v.z < -.0215f && v.z > -.31f && Mathf.Abs(v.x) <= .536f && v.y >= 0f && v.y < .0135f, "a strip vertex off the approach side's saddle and floor: " + v);
            rig.Tick(Dt);
            gap.Tick(1f);
            var armed = gap.Slot;
            var armedSum = armed.r + armed.g + armed.b;
            Require(armedSum > .05f && armed.b >= armed.r && gr.enabled, "armed: the slot " + armed);
            gap.Tick(0f);
            Require(!gr.enabled, "open: the strip still draws");
            gap.Tick(1f);
            Require(gr.enabled, "shut again: the strip is off");
            // the trip: dark in the black gap (mains down, heads not yet up), then the tripped light (warm or dark)
            rig.Trip();
            for (var k = 0; k < 12; k++) { rig.Tick(Dt); gap.Tick(1f); }
            var gapLevel = gap.Slot.r + gap.Slot.g + gap.Slot.b;
            Require(gapLevel < .05f * armedSum, "the trip's black gap: the slot " + gap.Slot + " against armed " + armed);
            for (var k = 0; k < 90; k++) { rig.Tick(Dt); gap.Tick(1f); }
            var tripped = gap.Slot;
            Require(tripped.r >= tripped.b - 1e-5f, "tripped: the slot is not warm " + tripped);
            // the reset re-strike brings the armed slot back
            rig.StartReset();
            var steps = Mathf.CeilToInt((rig.ResetDuration + .2f) / Dt);
            for (var k = 0; k < steps; k++) { rig.Tick(Dt); gap.Tick(1f); }
            var back = gap.Slot.r + gap.Slot.g + gap.Slot.b;
            Require(rig.State == RunRigState.Armed && Mathf.Abs(back - armedSum) <= .02f * armedSum, "after the reset: " + F(back) + " against " + F(armedSum));
            // no allocation
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var k = 0; k < 200; k++) gap.Tick(1f - (k % 2) * .5f);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Require(allocated == 0, "Tick allocated " + allocated + " bytes");
            sb.Append("; gap: armed ").Append(F(armed.r)).Append("/").Append(F(armed.g)).Append("/").Append(F(armed.b))
                .Append(", black gap ").Append(F(gapLevel)).Append(", tripped ").Append(F(tripped.r)).Append("/").Append(F(tripped.g)).Append("/").Append(F(tripped.b))
                .Append(", ").Append(gap.LightCount).Append(" lights read, ").Append(mesh.vertexCount).Append(" verts, 0 alloc");
            return sb.ToString();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            UnityEngine.Object.DestroyImmediate(archRoot.gameObject);
        }
    }

    static void SetReduced(bool on)
    {
        // The private setter only: never FrontRoomsSettings.SetReduceFlashing, which writes PlayerPrefs.
        typeof(FrontRoomsSettings).GetProperty("ReduceFlashing", BindingFlags.Static | BindingFlags.Public).GetSetMethod(true).Invoke(null, new object[] { on });
    }

    // ------------------------------------------------------------------ helpers
    sealed class Corridor : IDisposable
    {
        public readonly Transform root;
        public readonly FrontRoomsRunRig rig;
        public Corridor()
        {
            root = new GameObject("TEST run b corridor").transform;
            root.position = new Vector3(4992f, 0f, 4992f);
            var layout = RunRigLayout.Corridor1x9();
            var dress = FrontRoomsRunDress.Build(root, layout);
            rig = FrontRoomsRunRig.Create(root, layout, RunRigHost.OwnLamps, null, dress);
        }
        public void Dispose() { UnityEngine.Object.DestroyImmediate(root.gameObject); }
    }

    static Light[] Lights(FrontRoomsRunRig rig) { return rig.GetComponentsInChildren<Light>(true); }

    static string Fingerprint(FrontRoomsRunRig rig)
    {
        var sb = new StringBuilder();
        foreach (var l in Lights(rig)) sb.Append(l.enabled ? '1' : '0').Append(F(l.intensity)).Append(';');
        foreach (var p in rig.GetComponentsInChildren<ReflectionProbe>(true)) sb.Append(p.enabled ? 'P' : 'p').Append(F(p.intensity)).Append(';');
        return sb.ToString();
    }

    /// <summary>The same flash rule as FrontRoomsExitSignTests: transitions of 0.1 from the last extreme with the
    /// darker side below 0.8; a flash is a transition pair; the most flashes in any 1 s window.</summary>
    sealed class FlashCounter
    {
        float extreme; int dir;
        public readonly List<float> transitions = new List<float>();
        public FlashCounter(float start) { extreme = start; }
        public void Add(float t, float v)
        {
            if (dir >= 0)
            {
                if (v > extreme) extreme = v;
                else if (extreme - v >= .1f && v < .8f) { transitions.Add(t); dir = -1; extreme = v; }
            }
            else
            {
                if (v < extreme) extreme = v;
                else if (v - extreme >= .1f && extreme < .8f) { transitions.Add(t); dir = 1; extreme = v; }
            }
        }
        public float MaxFlashesPerSecond()
        {
            var best = 0;
            for (int i = 0, j = 0; i < transitions.Count; i++)
            {
                while (transitions[i] - transitions[j] >= 1f) j++;
                best = Mathf.Max(best, i - j + 1);
            }
            return best / 2f;
        }
    }

    /// <summary>Record every rig light's intensity, normalised by its own maximum over the run, and count flashes.</summary>
    sealed class LampTrace
    {
        readonly Light[] lights;
        readonly List<float>[] values;
        readonly List<float> times = new List<float>();
        public LampTrace(Light[] l)
        {
            lights = l;
            values = new List<float>[l.Length];
            for (var i = 0; i < l.Length; i++) values[i] = new List<float>();
        }
        public void Sample(float t)
        {
            times.Add(t);
            for (var i = 0; i < lights.Length; i++) values[i].Add(lights[i].enabled ? lights[i].intensity : 0f);
        }
        public float WorstFlashes(out string worstName)
        {
            var worst = 0f; worstName = "-";
            for (var i = 0; i < lights.Length; i++)
            {
                var max = 0f;
                foreach (var v in values[i]) max = Mathf.Max(max, v);
                if (max <= 0f) continue;
                var fc = new FlashCounter(values[i][0] / max);
                for (var k = 0; k < values[i].Count; k++) fc.Add(times[k], values[i][k] / max);
                var f = fc.MaxFlashesPerSecond();
                if (f > worst) { worst = f; worstName = lights[i].name; }
            }
            return worst;
        }
        public bool Monotone(int i, float tolerance)
        {
            for (var k = 1; k < values[i].Count; k++) if (values[i][k] < values[i][k - 1] - tolerance) return false;
            return true;
        }
        public int Count { get { return lights.Length; } }
        public string Name(int i) { return lights[i].name; }
    }

    static Light[] Named(FrontRoomsRunRig rig, string prefix)
    {
        var list = new List<Light>();
        foreach (var l in Lights(rig)) if (l.name.StartsWith(prefix, StringComparison.Ordinal)) list.Add(l);
        return list.ToArray();
    }

    // ------------------------------------------------------------------ rig
    static string R1Deterministic()
    {
        var times = new[] { -1f, 0f, .03f, .06f, .2f, .35f, .37f, .45f, .6f, 1f, 3f };
        using (var a = new Corridor())
        using (var b = new Corridor())
        {
            var first = new string[times.Length];
            for (var i = 0; i < times.Length; i++) { a.rig.Evaluate(times[i]); first[i] = Fingerprint(a.rig); }
            for (var i = times.Length - 1; i >= 0; i--)
            {
                a.rig.Evaluate(times[i]);
                Require(Fingerprint(a.rig) == first[i], "rig A at t " + F(times[i]) + " differs when evaluated in reverse order");
                b.rig.Evaluate(times[i]);
                Require(Fingerprint(b.rig) == first[i], "rig B at t " + F(times[i]) + " differs from rig A");
            }
            return times.Length + " times x 2 orders x 2 rigs identical; " + Lights(a.rig).Length + " lights";
        }
    }

    static string R2Photosafety()
    {
        var sb = new StringBuilder();
        using (var c = new Corridor())
        {
            var rig = c.rig;
            // Trip: evaluated at 1 kHz over 3 s.
            var trip = new LampTrace(Lights(rig));
            for (var ms = -100; ms <= 3000; ms++) { rig.Evaluate(ms / 1000f); trip.Sample(ms / 1000f); }
            string n; var f = trip.WorstFlashes(out n);
            sb.Append("trip ").Append(F(f)).Append("/s (").Append(n).Append("); ");
            Require(f <= 3f, "trip: " + F(f) + " flashes/s on " + n);
            // Animated reset (map RoomReset): Trip, settle, StartReset, ticked at 240 Hz.
            rig.ResetImmediate();
            rig.Trip();
            for (var k = 0; k < 240; k++) rig.Tick(Dt);
            var reset = new LampTrace(Lights(rig));
            rig.StartReset();
            var t = 0f;
            reset.Sample(t);
            for (var k = 0; k < 240 * 12 && rig.State == RunRigState.Resetting; k++) { rig.Tick(1f / 240f); t += 1f / 240f; reset.Sample(t); }
            Require(rig.State == RunRigState.Armed, "reset did not end Armed after " + F(t) + " s");
            f = reset.WorstFlashes(out n);
            sb.Append("reset ").Append(F(f)).Append("/s (").Append(n).Append(", ").Append(F(t)).Append(" s); ");
            Require(f <= 3f, "reset: " + F(f) + " flashes/s on " + n);
            // Stutter tube (armed, own lamps): 60 s at 60 Hz.
            var stutter = new LampTrace(Lights(rig));
            t = 0f;
            for (var k = 0; k < 3600; k++) { rig.Tick(Dt); t += Dt; stutter.Sample(t); }
            f = stutter.WorstFlashes(out n);
            sb.Append("stutter ").Append(F(f)).Append("/s (").Append(n).Append("); ");
            Require(f <= 3f, "stutter: " + F(f) + " flashes/s on " + n);
            // Stage-1 warning (CR-4): the map's warning lamps drive ApplyWarn; here a 2 Hz square between 1 and .55,
            // the fastest the warning plan allows, must not exceed the bar on any wrap.
            var warnTrace = new LampTrace(Lights(rig));
            t = 0f;
            for (var k = 0; k < 600; k++)
            {
                rig.ApplyWarn(Mathf.Repeat(t, .5f) < .25f ? 1f : .55f);
                rig.Tick(Dt); t += Dt; warnTrace.Sample(t);
            }
            rig.ApplyWarn(1f);
            f = warnTrace.WorstFlashes(out n);
            sb.Append("warning 2 Hz ").Append(F(f)).Append("/s (").Append(n).Append(")");
            Require(f <= 3f, "warning: " + F(f) + " flashes/s on " + n);
        }
        return sb.ToString();
    }

    static string R3Reduced()
    {
        var sb = new StringBuilder();
        SetReduced(true);
        try
        {
            using (var c = new Corridor())
            {
                var rig = c.rig;
                var wraps = Named(rig, "run b wrap light");
                var heads = Named(rig, "head light");
                Require(wraps.Length == 9 && heads.Length == 8, "corridor: " + wraps.Length + " wraps, " + heads.Length + " heads");
                rig.Trip();
                var trace = new LampTrace(Lights(rig));
                var minRoom = 1f; var minAt = 0f; var t = 0f;
                var wrapMax = RunRigLayout.Corridor1x9().wrapIntensity;
                var headMax = RunRigLayout.Corridor1x9().floodIntensity;
                trace.Sample(0f);
                for (var k = 0; k < 240 * 3; k++)
                {
                    rig.Tick(1f / 240f); t += 1f / 240f; trace.Sample(t);
                    var w = 0f; foreach (var l in wraps) w += l.enabled ? l.intensity / wrapMax : 0f;
                    var h = 0f; foreach (var l in heads) h += l.enabled ? Mathf.Min(1f, l.intensity / headMax) : 0f;
                    var room = Mathf.Max(w / wraps.Length, h / heads.Length);
                    if (room < minRoom) { minRoom = room; minAt = t; }
                }
                string n; var f = trace.WorstFlashes(out n);
                sb.Append("trip: ").Append(F(f)).Append(" flashes/s, darkest room level ").Append(F(minRoom)).Append(" at ").Append(F(minAt)).Append(" s; ");
                Require(f <= .5f, "reduced trip flashes on " + n);   // one monotone change per lamp, never a pair
                Require(minRoom >= .15f, "reduced trip has a black gap (" + F(minRoom) + " at " + F(minAt) + " s)");
                Require(rig.WrapLevel(.25f) > .4f && rig.WrapLevel(.49f) > 0f, "reduced wrap fade is shorter than 0.5 s");
                Require(rig.HeadLevel(.5f, 0f) < 1f && rig.HeadLevel(.74f, 0f) > .9f, "reduced head rise is not 0.5 s from 0.25 s");
                // Re-strike: a rise, no flicks.
                var reset = new LampTrace(wraps);
                rig.StartReset();
                t = 0f; reset.Sample(0f);
                for (var k = 0; k < 240 * 12 && rig.State == RunRigState.Resetting; k++) { rig.Tick(1f / 240f); t += 1f / 240f; reset.Sample(t); }
                f = reset.WorstFlashes(out n);
                var mono = true;
                for (var i = 0; i < reset.Count; i++) if (!reset.Monotone(i, 1e-4f)) mono = false;
                sb.Append("reset: ").Append(F(f)).Append(" flashes/s, monotone ").Append(mono).Append("; ");
                Require(f <= .5f && mono, "reduced re-strike flickers");
                // Stutter: steady.
                var stutter = new LampTrace(wraps);
                for (var k = 0; k < 600; k++) { rig.Tick(Dt); stutter.Sample(k * Dt); }
                f = stutter.WorstFlashes(out n);
                sb.Append("stutter: ").Append(F(f)).Append(" flashes/s");
                Require(f == 0f, "reduced stutter flickers on " + n);
            }
        }
        finally { SetReduced(false); }
        return sb.ToString();
    }

    static string R4OneShot()
    {
        using (var c = new Corridor())
        {
            var rig = c.rig;
            int drops = 0, relays = 0;
            rig.MainsDropped += p => drops++;
            rig.TransferRelay += (i, p) => relays++;
            rig.Trip();
            rig.Tick(.05f);
            var clock = rig.Clock;
            rig.Trip();
            Require(Mathf.Approximately(rig.Clock, clock), "a second Trip restarted the clock");
            for (var k = 0; k < 120; k++) rig.Tick(Dt);
            Require(rig.State == RunRigState.Tripped, "state " + rig.State + " after 2 s");
            rig.Trip();
            for (var k = 0; k < 60; k++) rig.Tick(Dt);
            Require(drops == 1, drops + " mains drops");
            Require(relays == rig.HeadCount, relays + " transfer relays for " + rig.HeadCount + " heads");
            return "1 mains drop, " + relays + " relays, Trip ignored while Tripping and Tripped";
        }
    }

    static string R5Reset()
    {
        using (var fresh = new Corridor())
        using (var c = new Corridor())
        {
            var armed = Fingerprint(fresh.rig);
            var rig = c.rig;
            var changes = 0;
            rig.StateChanged += (s, p) => changes++;
            rig.Trip();
            for (var k = 0; k < 180; k++) rig.Tick(Dt);
            var before = changes;
            rig.ResetImmediate();
            var once = Fingerprint(rig);
            rig.ResetImmediate();
            var twice = Fingerprint(rig);
            Require(changes == before + 1, "ResetImmediate raised " + (changes - before) + " state changes");
            Require(once == twice, "a second ResetImmediate changed the rig");
            Require(once == armed, "reset rig differs from a freshly built armed rig");
            Require(rig.State == RunRigState.Armed && rig.Clock < 0f, "not armed after reset");
            return "armed after one reset, unchanged by a second, identical to a fresh rig";
        }
    }

    static string R6Alloc()
    {
        using (var c = new Corridor())
        {
            var rig = c.rig;
            for (var k = 0; k < 60; k++) rig.Tick(Dt);              // armed warm-up (stutter)
            rig.Warn(1);                                             // the CR-4 burst path inside the armed measure
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var k = 0; k < 600; k++) rig.Tick(Dt);
            var armed = GC.GetAllocatedBytesForCurrentThread() - before;
            rig.Trip();
            for (var k = 0; k < 5; k++) rig.Tick(Dt);
            before = GC.GetAllocatedBytesForCurrentThread();
            for (var k = 0; k < 600; k++) rig.Tick(Dt);
            var tripped = GC.GetAllocatedBytesForCurrentThread() - before;
            rig.StartReset();
            before = GC.GetAllocatedBytesForCurrentThread();
            for (var k = 0; k < 600; k++) rig.Tick(Dt);
            var reset = GC.GetAllocatedBytesForCurrentThread() - before;
            var detail = "armed " + armed + " B, tripping/tripped " + tripped + " B, resetting " + reset + " B over 600 ticks each";
            Require(armed == 0 && tripped == 0 && reset == 0, detail);
            return detail;
        }
    }

    static string R7Budget()
    {
        using (var c = new Corridor())
        {
            var rig = c.rig;
            int total, on, sh;
            rig.Evaluate(-1f); rig.CountLights(out total, out on, out sh);
            var detail = "armed " + on + "/" + total + " (" + sh + " shadowed)";
            Require(on == 10 && sh == 9, detail);
            rig.Evaluate(.2f); rig.CountLights(out total, out on, out sh);
            detail += ", blackout " + on;
            Require(on == 1, detail);
            rig.Evaluate(3f); rig.CountLights(out total, out on, out sh);
            detail += ", tripped " + on + " (" + sh + " shadowed)";
            Require(on == 9 && sh == 8, detail);
            return detail;
        }
    }

    static string R8NoExitSign()
    {
        using (var c = new Corridor())
        {
            var signs = c.root.GetComponentsInChildren<FrontRoomsExitSign>(true).Length;
            var named = 0;
            foreach (var t in c.root.GetComponentsInChildren<Transform>(true))
                if (t.name.IndexOf("exit sign", StringComparison.OrdinalIgnoreCase) >= 0 || t.name.StartsWith("Kit_ExitSign", StringComparison.Ordinal)) named++;
            Require(signs == 0 && named == 0, signs + " FrontRoomsExitSign, " + named + " objects named as EXIT signs");
            var gear = new[] { "run b Kit_PullStation", "run b Kit_FireBell", "run b Kit_ExtinguisherCabinet", "run b sprinkler main" };
            var detail = "0 EXIT signs; fire gear:";
            foreach (var g in gear)
            {
                var n = 0;
                foreach (var t in c.root.GetComponentsInChildren<Transform>(true)) if (t.name == g) n++;
                Require(n > 0, "missing " + g);
                detail += " " + g.Substring(6) + " " + n;
            }
            return detail;
        }
    }

    static string R9GoalLight()
    {
        using (var c = new Corridor())
        {
            var rig = c.rig;
            var gl = rig.GoalWindowLight;
            Require(gl != null, "no goal window light");
            Require(gl.shadows == LightShadows.None, "the goal window light casts shadows");
            var local = c.root.InverseTransformPoint(gl.transform.position);
            Require(local.z > 27.08f, "the goal window light is not past the goal wall (z " + F(local.z) + ")");
            Require(Vector3.Dot(gl.transform.forward, c.root.forward) > .5f, "the goal window light is not aimed away from the door");
            var eye = new GameObject("TEST viewer").transform;
            try
            {
                eye.SetParent(c.root, false);
                rig.SetViewer(eye);
                eye.localPosition = new Vector3(0f, 1.62f, 1.5f);      // the trip line
                rig.Tick(Dt);
                var detail = "trip line: on " + gl.enabled + " w " + F(rig.GoalLightWeight);
                Require(gl.enabled && rig.GoalLightWeight > .99f, detail + " (estimate: the far lamp is 27 m away, unlit)");
                rig.FarRoomLamp = () => 1f; rig.Tick(Dt);
                detail += "; far lamp 1: w " + F(rig.GoalLightWeight);
                Require(!gl.enabled, detail);
                rig.FarRoomLamp = () => .5f; rig.Tick(Dt);
                detail += "; far lamp .5: " + F(gl.intensity);
                Require(Mathf.Abs(rig.GoalLightWeight - .5f) < 1e-4f, detail);
                rig.FarRoomLamp = () => -1f;
                eye.localPosition = new Vector3(0f, 1.62f, 26.0f);     // 1 m from the door, no far lamp
                rig.Tick(Dt);
                detail += "; no lamp at 1 m: w " + F(rig.GoalLightWeight);
                Require(rig.GoalLightWeight < 1e-4f, detail);
                eye.localPosition = new Vector3(0f, 1.62f, 16.0f);
                rig.Tick(Dt);
                detail += ", at 11 m " + F(rig.GoalLightWeight);
                Require(rig.GoalLightWeight > .99f, detail);
                rig.Trip();
                for (var k = 0; k < 120; k++) rig.Tick(Dt);
                detail += ", tripped " + F(rig.GoalLightWeight);
                Require(rig.GoalLightWeight > .99f && gl.enabled, detail);
                eye.localPosition = new Vector3(0f, 1.62f, 28.0f);     // in the far room
                rig.Tick(Dt);
                detail += "; past the wall: on " + gl.enabled;
                Require(!gl.enabled, detail);
                return detail;
            }
            finally { UnityEngine.Object.DestroyImmediate(eye.gameObject); }
        }
    }

    static string R10SideDoors()
    {
        using (var c = new Corridor())
        {
            int frames = 0, leaves = 0, closers = 0;
            foreach (var t in c.root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "run b side door frame") frames++;
                if (t.name == "run b side door leaf") leaves++;
                if (t.name.IndexOf("closer", StringComparison.OrdinalIgnoreCase) >= 0) closers++;
            }
            var lites = c.root.GetComponentsInChildren<FrontRoomsDoorLite>(true);
            var dead = 0;
            foreach (var l in lites) if (l.Dead) dead++;
            var detail = frames + " frames, " + leaves + " leaves, " + dead + "/" + lites.Length + " dead lites, " + closers + " closers";
            Require(frames == 3 && leaves == 3 && dead == 3 && lites.Length == 3 && closers == 0, detail);
            // The frame never shows on the neighbour's face: every frame renderer stays inside the wall's far face.
            var layout = RunRigLayout.Corridor1x9();
            var limit = layout.halfWidth + layout.wallThickness - .001f;
            foreach (var t in c.root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != "run b side door frame") continue;
                foreach (var r in t.GetComponentsInChildren<Renderer>(true))
                {
                    var b = r.bounds;
                    var a = c.root.InverseTransformPoint(b.min);
                    var z = c.root.InverseTransformPoint(b.max);
                    Require(Mathf.Max(Mathf.Abs(a.x), Mathf.Abs(z.x)) <= limit, "a side door frame reaches |x| " + F(Mathf.Max(Mathf.Abs(a.x), Mathf.Abs(z.x))) + " > " + F(limit));
                }
            }
            return detail + "; frames inside |x| <= " + F(limit);
        }
    }

    static string R11WarnBurst()
    {
        var sb = new StringBuilder();
        using (var c = new Corridor())
        {
            var rig = c.rig;
            var wraps = Named(rig, "run b wrap light");
            var trace = new LampTrace(wraps);
            var t = 0f;
            var minRatio = 1f;
            var armed = new float[wraps.Length];
            for (var i = 0; i < wraps.Length; i++) armed[i] = wraps[i].intensity;
            rig.Warn(1);
            Require(rig.Bursting, "Warn(1) started no burst");
            trace.Sample(0f);
            for (var k = 0; k < 240 * 3; k++)
            {
                rig.Tick(1f / 240f); t += 1f / 240f; trace.Sample(t);
                for (var i = 0; i < wraps.Length; i++)
                    if (i != RunRigLayout.Corridor1x9().stutterLamp && armed[i] > 0f) minRatio = Mathf.Min(minRatio, wraps[i].intensity / armed[i]);
            }
            string n; var f = trace.WorstFlashes(out n);
            sb.Append("stage 1: ").Append(F(f)).Append(" flashes/s (").Append(n).Append("), deepest ").Append(F(minRatio));
            Require(f <= 3f, sb.ToString());
            Require(minRatio > .5f && minRatio < .8f, sb + ": the dip is not the map's 0.55-0.75");
            Require(!rig.Bursting && rig.State == RunRigState.Armed, sb + ": the burst did not end armed");
        }
        SetReduced(true);
        try
        {
            using (var c = new Corridor())
            {
                var rig = c.rig;
                var wraps = Named(rig, "run b wrap light");
                var k0 = RunRigLayout.Corridor1x9().stutterLamp == 0 ? 1 : 0;
                var armed = wraps[k0].intensity;
                var min = 1f;
                rig.Warn(1);
                for (var k = 0; k < 240 * 3; k++) { rig.Tick(1f / 240f); min = Mathf.Min(min, wraps[k0].intensity / armed); }
                sb.Append("; reduced: one sag to ").Append(F(min));
                Require(min >= .69f && min < .75f, sb.ToString());
            }
        }
        finally { SetReduced(false); }
        return sb.ToString();
    }

    static string R12MapSkin()
    {
        var root = new GameObject("TEST run b map skin").transform;
        try
        {
            root.position = new Vector3(4992f, 0f, 4992f);
            var l = RunRigLayout.Corridor1x9();
            l.buildShell = false;                     // as the binder (FrontRoomsRunRigBinder.Layout): the map builds the shell
            l.skinShell = true;
            FrontRoomsRunDress.Build(root, l);
            int soffit = 0, reveal = 0, revealAtGoal = 0, coverCabinet = 0;
            var revealDepth = 0f;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "run b soffit skin") soffit++;
                if (t.name.StartsWith("run b arch reveal", StringComparison.Ordinal))
                {
                    reveal++;
                    var r = t.GetComponent<Renderer>();
                    var a = root.InverseTransformPoint(r.bounds.min);
                    var b = root.InverseTransformPoint(r.bounds.max);
                    if (Mathf.Max(a.z, b.z) > l.length * .5f) revealAtGoal++;
                    revealDepth = Mathf.Max(revealDepth, Mathf.Max(a.z, b.z));
                    Require(Mathf.Min(a.z, b.z) > -.001f, t.name + " reaches the host's half (z " + F(Mathf.Min(a.z, b.z)) + ")");
                }
                if (t.name == "run b lower tone")
                {
                    var r = t.GetComponent<Renderer>();
                    var a = root.InverseTransformPoint(r.bounds.min);
                    var b = root.InverseTransformPoint(r.bounds.max);
                    if (Mathf.Min(a.x, b.x) < -l.halfWidth + .01f && Mathf.Min(a.z, b.z) < 10.04f && Mathf.Max(a.z, b.z) > 10.36f) coverCabinet++;
                }
            }
            var detail = soffit + " soffit, " + reveal + " arch reveal slabs to z " + F(revealDepth) + " (" + revealAtGoal + " at the goal), lower tone behind the cabinet " + coverCabinet;
            Require(soffit == 1 && reveal == 7 && revealAtGoal == 0 && coverCabinet == 1, detail);
            Require(revealDepth > l.WallHalf && revealDepth < l.WallHalf + .005f, detail + ": the reveal does not meet the room face");
            return detail;
        }
        finally { UnityEngine.Object.DestroyImmediate(root.gameObject); }
    }

    // ------------------------------------------------------------------ stage C (60_production_66.md)
    static float XZ(Vector3 a, Vector3 b) { var dx = a.x - b.x; var dz = a.z - b.z; return Mathf.Sqrt(dx * dx + dz * dz); }

    /// <summary>A direct child by name (Transform.Find splits on '/', and the rig's names carry one).</summary>
    static Transform Child(Transform t, string name)
    {
        if (t == null) return null;
        for (var i = 0; i < t.childCount; i++) if (t.GetChild(i).name == name) return t.GetChild(i);
        return null;
    }

    static string R13Cull()
    {
        using (var c = new Corridor())
        {
            var rig = c.rig;
            var layout = RunRigLayout.Corridor1x9();
            var eye = new GameObject("TEST viewer").transform;
            try
            {
                eye.SetParent(c.root, false);
                rig.SetViewer(eye);
                eye.localPosition = new Vector3(0f, 1.62f, 1.5f);          // the trip line
                rig.Trip();
                for (var k = 0; k < 120; k++) rig.Tick(Dt);
                int total, on, sh;
                rig.CountLights(out total, out on, out sh);
                var detail = "trip line, tripped: " + on + " on";
                Require(on == 9, detail + " (want 8 heads + the goal window light)");
                var full = new float[rig.HeadCount];
                for (var i = 0; i < rig.HeadCount; i++) full[i] = rig.HeadLight(i) != null ? rig.HeadLight(i).intensity : 0f;
                eye.localPosition = new Vector3(0f, 1.62f, -25f);          // 25 m outside the arch
                rig.Tick(Dt);
                var goalOn = false; var othersOn = 0;
                for (var i = 0; i < rig.HeadCount; i++)
                {
                    var l = rig.HeadLight(i);
                    if (l == null) continue;
                    if (rig.HeadIsGoal(i)) goalOn = l.enabled && Mathf.Abs(l.intensity - full[i]) < 1e-3f;
                    else if (l.enabled) othersOn++;
                }
                detail += "; 25 m out: goal head at full " + goalOn + ", other heads on " + othersOn;
                Require(goalOn && othersOn == 0, detail);
                eye.localPosition = new Vector3(0f, 1.62f, -10f);          // 9.9 m outside: the box weight is 0
                rig.Tick(Dt);
                var worst = 0f;
                for (var i = 0; i < rig.HeadCount; i++)
                {
                    var l = rig.HeadLight(i);
                    if (l == null || rig.HeadIsGoal(i)) continue;
                    var want = full[i] * Mathf.Clamp01((layout.cullDistance - XZ(l.transform.position, eye.position)) / layout.cullFade);
                    worst = Mathf.Max(worst, Mathf.Abs((l.enabled ? l.intensity : 0f) - want));
                }
                detail += "; 10 m out: worst error against the 16 m / 3 m fade " + F(worst);
                Require(worst < 1e-3f, detail);
                return detail;
            }
            finally { UnityEngine.Object.DestroyImmediate(eye.gameObject); }
        }
    }

    static string R14ResetWindows()
    {
        using (var c = new Corridor())
        {
            var rig = c.rig;
            var wraps = Named(rig, "run b wrap light");
            var heads = Named(rig, "head light");
            Require(wraps.Length == 9, wraps.Length + " wraps");
            rig.Trip();
            for (var k = 0; k < 240; k++) rig.Tick(Dt);
            var series = new List<float[]>();
            var times = new List<float>();
            rig.StartReset();
            var t = 0f;
            for (var k = 0; k < 240 * 12; k++)
            {
                var row = new float[wraps.Length + 1];
                for (var i = 0; i < wraps.Length; i++) row[i] = wraps[i].enabled ? wraps[i].intensity : 0f;
                var h = 0f;
                foreach (var l in heads) h += l.enabled ? l.intensity : 0f;
                row[wraps.Length] = h;
                series.Add(row); times.Add(t);
                if (rig.State != RunRigState.Resetting) break;
                rig.Tick(1f / 240f); t += 1f / 240f;
            }
            // Flicks per wrap: rising edges before its sweep.
            var flicks = 0; var worstWrap = 0;
            for (var i = 0; i < wraps.Length; i++)
            {
                var edges = 0; var prev = 0f;
                for (var k = 0; k < series.Count && times[k] < .44f; k++) { if (series[k][i] > 0f && prev <= 0f) edges++; prev = series[k][i]; }
                flicks += edges; worstWrap = Mathf.Max(worstWrap, edges);
            }
            Require(worstWrap == 1, "a wrap flicks " + worstWrap + " times before the sweep");
            // Windows: everything (the corridor seen whole) and every 3 neighbouring wraps with the heads.
            var worst = 0f; var worstName = "-";
            for (var w = -1; w + 2 < wraps.Length; w++)
            {
                var a = w < 0 ? 0 : w; var b = w < 0 ? wraps.Length - 1 : w + 2;
                var mean = new float[series.Count];
                var max = 0f;
                for (var k = 0; k < series.Count; k++)
                {
                    var v = series[k][wraps.Length];
                    for (var i = a; i <= b; i++) v += series[k][i];
                    mean[k] = v; max = Mathf.Max(max, v);
                }
                if (max <= 0f) continue;
                var fc = new FlashCounter(mean[0] / max);
                for (var k = 0; k < series.Count; k++) fc.Add(times[k], mean[k] / max);
                var f = fc.MaxFlashesPerSecond();
                if (f > worst) { worst = f; worstName = w < 0 ? "whole corridor" : "wraps " + a + "-" + b; }
            }
            var detail = flicks + " flicks over " + wraps.Length + " wraps, worst window " + F(worst) + " flashes/s (" + worstName + "), " + F(t) + " s";
            Require(worst <= 3f, detail);
            return detail;
        }
    }

    static string R15Gate()
    {
        using (var c = new Corridor())
        {
            var rig = c.rig;
            Require(rig.GatedRenderers > 0, "no renderer took a gated twin");
            var mpb = new MaterialPropertyBlock();
            int twins = 0, ungateable = 0, missed = 0, blocks = 0;
            var dress = Child(c.root, "Run B / dress");
            var roots = new[] { rig.transform, dress };
            foreach (var root in roots)
            {
                if (root == null) continue;
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = r.sharedMaterials;
                    for (var k = 0; k < mats.Length; k++)
                    {
                        if (mats[k] == null) continue;
                        if (FrontRoomsRunGate.IsTwin(mats[k]))
                        {
                            twins++;
                            r.GetPropertyBlock(mpb, k);
                            if (mpb.GetVector("_FR_RunGateB").w > .5f) blocks++;
                        }
                        else if (FrontRoomsRunGate.CanGate(mats[k])) missed++;
                        else ungateable++;
                    }
                }
            }
            var detail = twins + " gated slots (" + blocks + " with the box), " + ungateable + " not gateable (glass, emissive, other shaders), " + missed + " missed";
            Require(twins > 0 && blocks == twins && missed == 0, detail);
            var notice = c.root.Find("run b Kit_SafetyPlate_10x7");
            Require(notice != null, "the NOTICE plate is not under the room root");
            foreach (var r in notice.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials) Require(!FrontRoomsRunGate.IsTwin(m), "the NOTICE plate outside took a gated twin");
            // The goal opening: the lite while shut, the whole door when open.
            Renderer probe = null; var slot = -1;
            foreach (var r in dress.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (var k = 0; k < mats.Length && slot < 0; k++) if (FrontRoomsRunGate.IsTwin(mats[k])) { probe = r; slot = k; }
                if (slot >= 0) break;
            }
            Require(probe != null, "no gated dress renderer");
            var l = RunRigLayout.Corridor1x9();
            probe.GetPropertyBlock(mpb, slot);
            var e = mpb.GetVector("_FR_RunGateE");
            Require(!rig.GoalOpen && Mathf.Abs(e.y - l.goalLiteHalf) < 1e-4f && Mathf.Abs(e.z - l.goalLiteBottom) < 1e-4f, "shut: E " + e);
            rig.SetGoalOpen(true);
            probe.GetPropertyBlock(mpb, slot);
            e = mpb.GetVector("_FR_RunGateE");
            Require(rig.GoalOpen && Mathf.Abs(e.y - l.goalHalf) < 1e-4f && e.z < 0f && Mathf.Abs(e.w - l.goalTop) < 1e-4f, "open: E " + e);
            var cvec = mpb.GetVector("_FR_RunGateC");
            Require(Mathf.Abs(cvec.x - (l.halfWidth + l.WallHalf)) < 1e-4f && Mathf.Abs(cvec.y - l.length) < 1e-4f, "box C " + cvec);
            return detail + "; NOTICE plate ungated; goal portal lite / door";
        }
    }

    static float VolumeWeight(Volume v, Vector3 world)
    {
        var box = v.GetComponent<BoxCollider>();
        var p = box.transform.InverseTransformPoint(world) - box.center;
        var h = box.size * .5f;
        var q = new Vector3(Mathf.Clamp(p.x, -h.x, h.x), Mathf.Clamp(p.y, -h.y, h.y), Mathf.Clamp(p.z, -h.z, h.z));
        var d = (box.transform.TransformPoint(q + box.center) - world).magnitude;
        return v.blendDistance > 0f ? Mathf.Clamp01(1f - d / v.blendDistance) : (d <= 0f ? 1f : 0f);
    }

    static string R16Post()
    {
        using (var c = new Corridor())
        {
            var vols = c.rig.GetComponentsInChildren<Volume>(true);
            Require(vols.Length == 3, vols.Length + " Run_B volumes (want main + 2 mouths)");
            var main = 0; var mouths = 0;
            foreach (var v in vols) { if (Mathf.Abs(v.blendDistance - .3f) < 1e-4f) main++; else if (Mathf.Abs(v.blendDistance - 1f) < 1e-4f) mouths++; }
            Require(main == 1 && mouths == 2, main + " main, " + mouths + " mouth volumes");
            var r = PlayerRadius();
            var worstSide = 0f; var worstIn = 1f;
            for (var z = -2f; z <= 29f; z += .25f)
            {
                foreach (var sx in new[] { -1f, 1f })
                {
                    var side = c.root.TransformPoint(new Vector3(sx * (1.42f + .16f + r), 1.62f, z));
                    foreach (var v in vols) worstSide = Mathf.Max(worstSide, VolumeWeight(v, side));
                }
                if (z > .5f && z < 26.5f)
                {
                    var inside = c.root.TransformPoint(new Vector3(1.42f - r, 1.62f, z));
                    var w = 0f;
                    foreach (var v in vols) w = Mathf.Max(w, VolumeWeight(v, inside));
                    worstIn = Mathf.Min(worstIn, w);
                }
            }
            var detail = "side neighbour max weight " + F(worstSide) + ", corridor (hugging a wall) min weight " + F(worstIn);
            Require(worstSide <= 1e-4f && worstIn >= .999f, detail);
            // Host-neutral: everything the Office grade overrides, Run_B overrides; the three it took are the global's values.
            var b = Resources.Load<VolumeProfile>("Rendering/FrontRoomsPost_Run_B");
            var o = Resources.Load<VolumeProfile>("Rendering/FrontRoomsPost_Office");
            var g = Resources.Load<VolumeProfile>("Rendering/FrontRoomsPost");
            Require(b != null && o != null && g != null, "a post profile is missing");
            var missing = 0;
            foreach (var oc in o.components)
            {
                VolumeComponent bc = null;
                foreach (var x in b.components) if (x.GetType() == oc.GetType()) bc = x;
                for (var i = 0; i < oc.parameters.Count; i++)
                    if (oc.parameters[i].overrideState && (bc == null || !bc.parameters[i].overrideState)) { missing++; Log.AppendLine("R16 not overridden by Run_B: " + oc.GetType().Name + " #" + i); }
            }
            Require(missing == 0, detail + "; " + missing + " Office parameters Run_B leaves to the host");
            ShadowsMidtonesHighlights bs, gs; FilmGrain bf, gf; ChromaticAberration bca, gca;
            Require(b.TryGet(out bs) && g.TryGet(out gs) && bs.shadows.value == gs.shadows.value && bs.highlights.value == gs.highlights.value, "SMH differs from the global look");
            Require(b.TryGet(out bf) && g.TryGet(out gf) && bf.intensity.value == gf.intensity.value && bf.response.value == gf.response.value && bf.type.value == gf.type.value, "film grain differs from the global look");
            Require(b.TryGet(out bca) && g.TryGet(out gca) && bca.intensity.value == gca.intensity.value, "chromatic aberration differs from the global look");
            return detail + "; Run_B overrides all Office parameters; SMH, grain, CA = the global look";
        }
    }

    static float PlayerRadius() { return FrontRooms.Map.ModuleUnits.PlayerRadius; }

    static float LinearY(Color srgb) { var l = srgb.linear; return .2126f * l.r + .7152f * l.g + .0722f * l.b; }

    static string R17Dress()
    {
        using (var c = new Corridor())
        {
            var layout = RunRigLayout.Corridor1x9();
            var notice = c.root.Find("run b Kit_SafetyPlate_10x7");
            Require(notice != null && notice.parent == c.root, "the NOTICE plate is not on the room root");
            var lp = c.root.InverseTransformPoint(notice.position);
            Require(Mathf.Abs(lp.z + layout.WallHalf) < 1e-3f && Vector3.Dot(notice.forward, -c.root.forward) > .99f, "the NOTICE plate is not on the host face (z " + F(lp.z) + ")");
            var overhead = Child(Child(c.root, "Run B / dress"), "run b overhead services");
            Require(overhead != null, "no overhead services group");
            var casting = 0; var n = 0;
            foreach (var r in overhead.GetComponentsInChildren<Renderer>(true)) { n++; if (r.shadowCastingMode != ShadowCastingMode.Off) casting++; }
            Require(n > 0 && casting == 0, casting + " of " + n + " overhead renderers cast shadows");
            var floorY = LinearY(new Color(.725f, .706f, .651f));   // the VCT field #B9B4A6
            var chair = FrontRoomsSurfaces.Get("RunB_ChairGrey");
            var cart = FrontRoomsSurfaces.Get("RunB_CartPlastic");
            var cy = LinearY(chair.GetColor("_BaseColor"));
            var ky = LinearY(cart.GetColor("_BaseColor"));
            var detail = "plate on the host face, " + n + " overhead renderers unshadowed, albedo floor/chair " + F(floorY / cy) + ", floor/cart " + F(floorY / ky);
            Require(floorY / cy >= 4f && floorY / ky >= 4f, detail);
            var kits = new[] { "Kit_CableTray_3m", "Kit_ChairDolly8", "Kit_HandTruck_Cartons", "Kit_ExtinguisherCabinet", "Kit_EmergencyTwinHead" };
            var d01 = new[] { 4.5f, 4f, 5.8f, 3.5f, 3.6f };
            var d12 = new[] { 9f, 20f, 12f, 8f, 10f };
            var tan = Mathf.Tan(38f * Mathf.Deg2Rad);
            for (var i = 0; i < kits.Length; i++)
            {
                var go = Resources.Load<GameObject>("Props/Models/" + kits[i]);
                var g = go != null ? go.GetComponent<LODGroup>() : null;
                Require(g != null, kits[i] + ": no LOD group");
                var lods = g.GetLODs();
                Require(lods.Length >= 3, kits[i] + ": " + lods.Length + " LODs");
                var want0 = g.size / (2f * d01[i] * tan);
                var want1 = g.size / (2f * d12[i] * tan);
                Require(Mathf.Abs(lods[0].screenRelativeTransitionHeight - want0) < 1e-3f && Mathf.Abs(lods[1].screenRelativeTransitionHeight - want1) < 1e-3f,
                    kits[i] + ": switches at " + F(lods[0].screenRelativeTransitionHeight) + " / " + F(lods[1].screenRelativeTransitionHeight) + ", want " + F(want0) + " / " + F(want1) + " (re-imported?)");
            }
            return detail + "; 5 LOD groups at the new distances";
        }
    }

    /// <summary>Every light the rig owns, in order: its name, kind, range, cone, intensity, shadows and cookie; and the counts
    /// of beams, halos and renderers. Two builds with the same signature build the same lights.</summary>
    static string Signature(FrontRoomsRunRig rig)
    {
        var sb = new StringBuilder();
        foreach (var l in Lights(rig))
            sb.Append(l.name).Append('|').Append(l.type).Append('|').Append(F(l.range)).Append('|').Append(F(l.spotAngle)).Append('|').Append(F(l.innerSpotAngle))
              .Append('|').Append(F(l.intensity)).Append('|').Append(l.enabled ? 1 : 0).Append('|').Append(l.shadows).Append('|').Append(l.cookie != null ? l.cookie.name : "-").Append(';');
        int beams = 0, halos = 0, renderers = 0;
        foreach (var r in rig.GetComponentsInChildren<Renderer>(true))
        {
            renderers++;
            if (r.name == "beam cone") beams++;
            if (r.name == "run b soffit halo") halos++;
        }
        sb.Append("beams ").Append(beams).Append(" halos ").Append(halos).Append(" renderers ").Append(renderers);
        return sb.ToString();
    }

    static string W2Handheld()
    {
        var before = FrontRoomsRunRig.ForceHandheldForTools;
        string desktopA, desktopB, detail;
        try
        {
            FrontRoomsRunRig.ForceHandheldForTools = false;
            using (var a = new Corridor())
            {
                Require(!a.rig.Handheld, "a desktop build reports the handheld tier");
                a.rig.Evaluate(3f);
                desktopA = Signature(a.rig);
                int total, on, sh;
                a.rig.CountLights(out total, out on, out sh);
                Require(on == 9 && sh == 8 && Named(a.rig, "head light").Length == 8 && Named(a.rig, "run b wrap light").Length == 9, "desktop tripped " + on + "/" + sh);
            }
            FrontRoomsRunRig.ForceHandheldForTools = true;
            using (var h = new Corridor())
            {
                var rig = h.rig;
                Require(rig.Handheld, "the forced handheld build is not the handheld tier");
                var heads = Named(rig, "head light");
                var wraps = Named(rig, "run b wrap light");
                var beams = 0;
                foreach (var r in rig.GetComponentsInChildren<Renderer>(true)) if (r.name == "beam cone") beams++;
                var soft = 0;
                foreach (var l in Lights(rig)) if (l.shadows == LightShadows.Soft) soft++;
                detail = "handheld: " + heads.Length + " head + " + wraps.Length + " wrap Lights, " + beams + " beams, " + soft + " soft shadows";
                Require(heads.Length == 5 && wraps.Length == 5 && beams == 0 && soft == 0, detail);
                var goal = -1;
                for (var i = 0; i < rig.HeadCount; i++) if (rig.HeadIsGoal(i)) goal = i;
                Require(goal >= 0 && rig.HeadLight(goal) != null, "the handheld tier lost the goal head");
                int total, on, sh;
                rig.Evaluate(3f); rig.CountLights(out total, out on, out sh);
                detail += "; tripped " + on + " (" + sh + " shadowed)";
                Require(on == 6 && sh == 5, detail);
                rig.Evaluate(-1f); rig.CountLights(out total, out on, out sh);
                detail += ", armed " + on + " (" + sh + " shadowed)";
                Require(on == 6 && sh == 5, detail);
                var layout = RunRigLayout.Corridor1x9();
                foreach (var l in wraps)
                    if (l.name != "run b wrap light " + layout.stutterLamp)
                        Require(Mathf.Abs(l.intensity - layout.wrapIntensity * 9f / 5f) < 1e-3f, l.name + " at " + F(l.intensity));
                var trip = new LampTrace(Lights(rig));
                for (var ms = -100; ms <= 3000; ms++) { rig.Evaluate(ms / 1000f); trip.Sample(ms / 1000f); }
                string n; var f = trip.WorstFlashes(out n);
                detail += ", trip " + F(f) + " flashes/s";
                Require(f <= 3f, detail);
            }
            FrontRoomsRunRig.ForceHandheldForTools = false;
            using (var b = new Corridor())
            {
                b.rig.Evaluate(3f);
                desktopB = Signature(b.rig);
            }
        }
        finally { FrontRoomsRunRig.ForceHandheldForTools = before; }
        Require(desktopA == desktopB, "the desktop build differs before and after a handheld build");
        return detail + "; desktop signature identical before and after (" + desktopA.Length + " chars)";
    }

    // ------------------------------------------------------------------ card 1: the rig in cells (71 s.9, decisions 3 and 7)
    /// <summary>Today's preset as it landed in main (FrontRoomsRunRig.cs 9e964b3b, 2026-11-03), frozen verbatim: the reference R18
    /// holds Corridor(9) to, field for field. Never edit it to follow the factory.</summary>
    static RunRigLayout TodayCorridor1x9(float archWidth = 1.4f, float doorWidth = 1f)
    {
        var w = 1.42f;
        const float wh = .08f;
        var ah = archWidth * .5f;
        var dh = doorWidth * .5f;
        var l = new RunRigLayout
        {
            name = "Corridor1x9",
            halfWidth = w, length = 27f, height = 2.9f,
            units = new[]
            {
                // F12 (stage C): the near pool (z 6) read .220 x armed against r8's .169 (+30 %): its two heads x 0.77.
                // Zone lamps (Red 2026-10-31, T-A + T-B): the self-contained units stay at the entry and the goal ends; the two
                // between are remote heads on brackets, fed from the entry unit along both walls. Heads, aims and pools unchanged.
                new RunUnitDef { z = 3f, side = 1f, autoAim = true, trimPlus = -.23f },
                new RunUnitDef { z = 9f, side = -1f, autoAim = true, trimMinus = -.23f, remote = true },
                new RunUnitDef { z = 15f, side = 1f, autoAim = true, remote = true },
                new RunUnitDef { z = 21f, side = -1f, autoAim = true, goalPlus = true },
            },
            goalPoint = new Vector3(0f, 1.05f, 27f),
            stutterLamp = 4,
            // F7 (stage C): every wrap shadowed, so none lights the neighbours through the long walls (6 of 9 did, up to
            // 143 levels on the Office wall in the preview). Armed now carries 9 shadowed spots, tripped 8: the peak is
            // the tripped set's, plus one. WebGL keeps its one (ownLampShadows[0]).
            ownLampShadows = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 },
            archHalf = ah, archTop = 2.2f, goalHalf = dh, goalTop = 2.1f, goalLiteX = 0f,
            probes = new[]
            {
                new RunProbeBox { centre = new Vector3(0f, 1.45f, 4.5f), size = new Vector3(3.16f, 3.22f, 9.6f) },
                new RunProbeBox { centre = new Vector3(0f, 1.45f, 13.5f), size = new Vector3(3.16f, 3.22f, 9.6f) },
                new RunProbeBox { centre = new Vector3(0f, 1.45f, 22.46f), size = new Vector3(3.16f, 3.22f, 9.6f) },
            },
            postCentre = new Vector3(0f, 1.45f, 13.5f), postSize = new Vector3(3f, 2.9f, 27f),
            buildShell = true,
            floorX0 = -w, floorX1 = w, floorZ0 = 0f, floorZ1 = 27f,
            laneHalf = .45f, accentRowsX = new[] { -.6f, .6f },
            mainX = .55f, trayX = -.60f, emtWallX = -w, runZ0 = 0f, runZ1 = 27f,
        };
        l.ownLamps = new Vector3[9];
        for (var k = 0; k < 9; k++) l.ownLamps[k] = new Vector3(0f, 2.9f, 1.8f + 3f * k);
        // Long faces run the full 27 m (their first and last 0.08 m lie inside the end walls); the side-door gaps are the
        // leaf (+-0.5) plus 45 mm under the F4 casing (+-0.549), so the lower tone tucks under the casing and the rail stops clear.
        const float dg = .545f;
        var goal = TodayFace(new Vector3(-w, 0f, 27f - wh), Vector3.right, w * 2f, Vector3.back, new Vector2(w - dh, w + dh));
        goal.gapTops = new[] { 2.1f };
        var entry = TodayFace(new Vector3(-w, 0f, wh), Vector3.right, w * 2f, Vector3.forward, new Vector2(w - ah, w + ah));
        entry.gapTops = new[] { 2.2f };
        l.walls = new[]
        {
            TodayFace(new Vector3(-w, 0f, 0f), Vector3.forward, 27f, Vector3.right, new Vector2(10.04f, 10.36f), new Vector2(17f - dg, 17f + dg)),
            TodayFace(new Vector3(w, 0f, 0f), Vector3.forward, 27f, Vector3.left, new Vector2(5f - dg, 5f + dg), new Vector2(23f - dg, 23f + dg)),
            entry,
            goal,
        };
        l.wallItems = new[]
        {
            new RunWallItem { kit = "Kit_PullStation", point = new Vector3(w, 1.2f - .09f, 1f), normal = Vector3.left },
            new RunWallItem { kit = "Kit_FireBell", point = new Vector3(w, 2.3f, 1f), normal = Vector3.left },
            new RunWallItem { kit = "Kit_ExtinguisherCabinet", point = new Vector3(-w, 1.1f - .305f, 10.2f), normal = Vector3.right },
            // The NOTICE plate on the host side of the entrance wall (its outside face, z -0.08: F8, it was buried in the
            // wall), right of the arch seen from outside; the ALARM ZONE card goes on the left (FrontRoomsRunRigBinder).
            new RunWallItem { kit = "Kit_SafetyPlate_10x7", point = new Vector3(Mathf.Min(ah + .40f, w - .15f), 1.5f, -wh), normal = Vector3.back },
        };
        l.sideDoors = new[]
        {
            new RunSideDoor { z = 5f, side = 1f, face = "electrical" },
            new RunSideDoor { z = 17f, side = -1f, face = "staff" },
            new RunSideDoor { z = 23f, side = 1f, face = "stencil" },
        };
        l.obstacles = new[]
        {
            new RunObstacle { kit = "Kit_ChairDolly8", position = new Vector3(1.0f, 0f, 6.4f), yaw = 90f },
            new RunObstacle { kit = "Kit_JanitorCart", position = new Vector3(-.63f, 0f, 12.0f), yaw = 90f },
            new RunObstacle { kit = "Kit_HandTruck_Cartons", position = new Vector3(1.05f, 0f, 18.2f), yaw = -90f },
        };
        return l;
    }

    static RunWallFace TodayFace(Vector3 start, Vector3 along, float length, Vector3 normal, params Vector2[] gaps)
    {
        return new RunWallFace { start = start, along = along, length = length, normal = normal, gaps = gaps };
    }

    static string Bits(float f) { return f.ToString("R", CultureInfo.InvariantCulture); }
    static string Bits(Vector3 v) { return Bits(v.x) + "," + Bits(v.y) + "," + Bits(v.z); }
    static string Bits(Quaternion q) { return Bits(q.x) + "," + Bits(q.y) + "," + Bits(q.z) + "," + Bits(q.w); }

    /// <summary>Every field of a and b, recursively: arrays element by element, structs and classes field by field, floats bit for
    /// bit. Returns the number of leaf values compared; every difference goes to diffs.</summary>
    static int SameFields(object a, object b, string path, List<string> diffs)
    {
        if (a == null || b == null)
        {
            if (a != null || b != null) diffs.Add(path + ": " + (a == null ? "null" : "set") + " / " + (b == null ? "null" : "set"));
            return 1;
        }
        var t = a.GetType();
        if (t != b.GetType()) { diffs.Add(path + ": " + t.Name + " / " + b.GetType().Name); return 1; }
        if (t == typeof(float))
        {
            if (BitConverter.ToInt32(BitConverter.GetBytes((float)a), 0) != BitConverter.ToInt32(BitConverter.GetBytes((float)b), 0))
                diffs.Add(path + ": " + Bits((float)a) + " / " + Bits((float)b));
            return 1;
        }
        if (t.IsPrimitive || t.IsEnum || t == typeof(string))
        {
            if (!a.Equals(b)) diffs.Add(path + ": " + a + " / " + b);
            return 1;
        }
        if (t.IsArray)
        {
            var x = (Array)a;
            var y = (Array)b;
            if (x.Length != y.Length) { diffs.Add(path + ": length " + x.Length + " / " + y.Length); return 1; }
            var n = 0;
            for (var i = 0; i < x.Length; i++) n += SameFields(x.GetValue(i), y.GetValue(i), path + "[" + i + "]", diffs);
            return n;
        }
        var count = 0;
        foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            count += SameFields(f.GetValue(a), f.GetValue(b), path + "." + f.Name, diffs);
        return count;
    }

    /// <summary>Every piece under root, in hierarchy order: name, local transform (to the bit), mesh and its vertex count, materials
    /// and shadow mode, box colliders, lights. Two builds with the same signature placed the same pieces.</summary>
    static string PieceSignature(Transform root, out int pieces)
    {
        var sb = new StringBuilder();
        pieces = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t == root) continue;
            pieces++;
            sb.Append(t.name).Append('|').Append(Bits(t.localPosition)).Append('|').Append(Bits(t.localRotation)).Append('|').Append(Bits(t.localScale));
            var mf = t.GetComponent<MeshFilter>();
            if (mf != null) sb.Append("|m ").Append(mf.sharedMesh != null ? mf.sharedMesh.name + " " + mf.sharedMesh.vertexCount : "-");
            var r = t.GetComponent<Renderer>();
            if (r != null)
            {
                sb.Append("|r ").Append(r.shadowCastingMode).Append(' ').Append(r.enabled ? 1 : 0);
                foreach (var m in r.sharedMaterials) sb.Append(' ').Append(m != null ? m.name : "-");
            }
            foreach (var bc in t.GetComponents<BoxCollider>()) sb.Append("|c ").Append(Bits(bc.center)).Append(' ').Append(Bits(bc.size));
            var lt = t.GetComponent<Light>();
            if (lt != null)
                sb.Append("|l ").Append(lt.type).Append(' ').Append(Bits(lt.range)).Append(' ').Append(Bits(lt.spotAngle)).Append(' ').Append(Bits(lt.innerSpotAngle))
                  .Append(' ').Append(Bits(lt.intensity)).Append(' ').Append(lt.shadows).Append(' ').Append(lt.enabled ? 1 : 0).Append(' ').Append(lt.cookie != null ? lt.cookie.name : "-");
            sb.Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>A room (dress + rig) from a layout under a root at the tests' place; the caller destroys the root.</summary>
    static Transform BuildRoom(string label, RunRigLayout layout, out FrontRoomsRunRig rig)
    {
        var root = new GameObject(label).transform;
        root.position = new Vector3(4992f, 0f, 4992f);
        var dress = FrontRoomsRunDress.Build(root, layout);
        rig = FrontRoomsRunRig.Create(root, layout, RunRigHost.OwnLamps, null, dress);
        return root;
    }

    static string R18TodayUnchanged()
    {
        // 1. The layout, field for field, bit for bit: the factory's defaults, the binder's arguments (the map's arch and door) and
        //    a third pair (the arch and door widths are the factory's only inputs besides the cells).
        var diffs = new List<string>();
        var leaves = 0;
        var widths = new[] { new[] { 1.4f, 1f }, new[] { FrontRooms.Map.FrontRoomsRunRooms.ArchWidth, FrontRooms.Map.ModuleUnits.DoorWidth }, new[] { 1.2f, .9f } };
        foreach (var p in widths)
        {
            leaves += SameFields(TodayCorridor1x9(p[0], p[1]), RunRigLayout.Corridor(9, p[0], p[1]), "Corridor(9, " + F(p[0]) + ", " + F(p[1]) + ")", diffs);
            leaves += SameFields(TodayCorridor1x9(p[0], p[1]), RunRigLayout.Corridor1x9(p[0], p[1]), "Corridor1x9(" + F(p[0]) + ", " + F(p[1]) + ")", diffs);
        }
        leaves += SameFields(TodayCorridor1x9(), RunRigLayout.Corridor1x9(), "Corridor1x9()", diffs);
        Require(diffs.Count == 0, diffs.Count + " differences against today's preset, first: " + (diffs.Count > 0 ? diffs[0] : ""));
        var detail = "Corridor(9) = today's Corridor1x9: " + leaves + " values in 7 builds, 0 differences";
        // 2. The pieces: the dress and the rig from today's preset and from Corridor(9), look-dev shell and the map skin (the binder's
        //    layout), piece by piece and light by light.
        for (var skin = 0; skin < 2; skin++)
        {
            var a = TodayCorridor1x9();
            var b = RunRigLayout.Corridor(9);
            if (skin == 1)
            {
                a.buildShell = false; a.skinShell = true;
                b.buildShell = false; b.skinShell = true;
            }
            FrontRoomsRunRig ra, rb;
            var rootA = BuildRoom("TEST run b today", a, out ra);
            var rootB = BuildRoom("TEST run b cells 9", b, out rb);
            try
            {
                int pa, pb;
                var sa = PieceSignature(rootA, out pa);
                var sb = PieceSignature(rootB, out pb);
                if (sa != sb)
                {
                    var la = sa.Split('\n');
                    var lb = sb.Split('\n');
                    var k = 0;
                    while (k < la.Length && k < lb.Length && la[k] == lb[k]) k++;
                    throw new Exception((skin == 1 ? "map skin" : "shell") + ": piece " + k + " differs: " + (k < la.Length ? la[k] : "-") + " / " + (k < lb.Length ? lb[k] : "-"));
                }
                ra.Evaluate(3f); rb.Evaluate(3f);
                Require(Signature(ra) == Signature(rb), "the rigs' lights differ tripped");
                ra.Evaluate(-1f); rb.Evaluate(-1f);
                Require(Signature(ra) == Signature(rb), "the rigs' lights differ armed");
                Require(Mathf.Abs(ra.ResetDuration - rb.ResetDuration) < 1e-6f, "reset " + F(ra.ResetDuration) + " / " + F(rb.ResetDuration));
                detail += "; " + (skin == 1 ? "map skin" : "shell") + " " + pa + " pieces identical";
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootA.gameObject);
                UnityEngine.Object.DestroyImmediate(rootB.gameObject);
            }
        }
        // 3. The binder reads the room's cells from its record, 9 by default.
        var room = new FrontRooms.Map.FrontRoomsRunRoom { Cells = new FrontRooms.Map.GridCoord[9] };
        Require(FrontRoomsRunRigBinder.CellsOf(room) == 9 && FrontRoomsRunRigBinder.CellsOf(null) == 9 && FrontRoomsRunRigBinder.CellsOf(new FrontRooms.Map.FrontRoomsRunRoom()) == 9, "CellsOf: not 9 for today's record or none");
        return detail + "; the binder's CellsOf 9 (record), 9 (none)";
    }

    /// <summary>R14's frame windows over one animated reset (the whole corridor, every 3 neighbouring wraps with the heads): the worst
    /// flashes a second, the most flicks a wrap makes before its sweep, and the reset's length as ticked.</summary>
    static float ResetWindows(FrontRoomsRunRig rig, out int worstFlicks, out string worstName, out float seconds)
    {
        var wraps = Named(rig, "run b wrap light");
        var heads = Named(rig, "head light");
        rig.Trip();
        for (var k = 0; k < 240; k++) rig.Tick(Dt);
        var series = new List<float[]>();
        var times = new List<float>();
        rig.StartReset();
        var t = 0f;
        for (var k = 0; k < 240 * 15; k++)
        {
            var row = new float[wraps.Length + 1];
            for (var i = 0; i < wraps.Length; i++) row[i] = wraps[i].enabled ? wraps[i].intensity : 0f;
            var h = 0f;
            foreach (var l in heads) h += l.enabled ? l.intensity : 0f;
            row[wraps.Length] = h;
            series.Add(row); times.Add(t);
            if (rig.State != RunRigState.Resetting) break;
            rig.Tick(1f / 240f); t += 1f / 240f;
        }
        seconds = t;
        worstFlicks = 0;
        for (var i = 0; i < wraps.Length; i++)
        {
            var edges = 0; var prev = 0f;
            for (var k = 0; k < series.Count && times[k] < rig.Layout.resetRiseStart - .01f; k++) { if (series[k][i] > 0f && prev <= 0f) edges++; prev = series[k][i]; }
            worstFlicks = Mathf.Max(worstFlicks, edges);
        }
        var worst = 0f; worstName = "-";
        for (var w = -1; w + 2 < wraps.Length; w++)
        {
            var a = w < 0 ? 0 : w; var b = w < 0 ? wraps.Length - 1 : w + 2;
            var mean = new float[series.Count];
            var max = 0f;
            for (var k = 0; k < series.Count; k++)
            {
                var v = series[k][wraps.Length];
                for (var i = a; i <= b; i++) v += series[k][i];
                mean[k] = v; max = Mathf.Max(max, v);
            }
            if (max <= 0f) continue;
            var fc = new FlashCounter(mean[0] / max);
            for (var k = 0; k < series.Count; k++) fc.Add(times[k], mean[k] / max);
            var f = fc.MaxFlashesPerSecond();
            if (f > worst) { worst = f; worstName = w < 0 ? "whole corridor" : "wraps " + a + "-" + b; }
        }
        return worst;
    }

    static int Tris(Mesh m)
    {
        if (m == null) return 0;
        long n = 0;
        for (var s = 0; s < m.subMeshCount; s++) n += m.GetIndexCount(s);
        return (int)(n / 3);
    }

    /// <summary>COST: triangles at LOD0 / LOD1 / LOD2 (a renderer outside any LOD group counts at every level; a group with fewer
    /// levels shows its last), renderer slots (renderers x materials) at LOD0, and lights.</summary>
    static string Cost(Transform root)
    {
        var inGroup = new HashSet<Renderer>();
        var tris = new long[3];
        long slots = 0;
        foreach (var g in root.GetComponentsInChildren<LODGroup>(true))
        {
            var lods = g.GetLODs();
            for (var level = 0; level < 3; level++)
            {
                var lod = lods[Mathf.Min(level, lods.Length - 1)];
                foreach (var r in lod.renderers)
                {
                    if (r == null) continue;
                    var mf = r.GetComponent<MeshFilter>();
                    tris[level] += Tris(mf != null ? mf.sharedMesh : null);
                    if (level == 0) slots += r.sharedMaterials.Length;
                }
            }
            foreach (var lod in lods) foreach (var r in lod.renderers) if (r != null) inGroup.Add(r);
        }
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (inGroup.Contains(r) || !r.enabled) continue;
            var mf = r.GetComponent<MeshFilter>();
            var n = Tris(mf != null ? mf.sharedMesh : null);
            for (var level = 0; level < 3; level++) tris[level] += n;
            slots += r.sharedMaterials.Length;
        }
        return "tris " + tris[0] + "/" + tris[1] + "/" + tris[2] + ", " + slots + " renderer slots";
    }

    static string R19Cells()
    {
        var sb = new StringBuilder();
        var reset9 = 0f;
        var hw = 1.42f;
        for (var n = RunRigLayout.MinCells; n <= RunRigLayout.MaxCells; n++)
        {
            var l = RunRigLayout.Corridor(n);
            var len = 3f * n;
            var tag = n + " cells: ";
            Require(l.Cells == n && l.length == len && l.name == "Corridor1x" + n, tag + "length " + F(l.length) + ", " + l.Cells + " cells, " + l.name);
            // A strip every cell, every one shadowed; the stutter tube in cell 5.
            Require(l.ownLamps.Length == n && l.ownLampShadows.Length == n && l.stutterLamp == 4, tag + l.ownLamps.Length + " strips");
            for (var k = 0; k < n; k++)
                Require(l.ownLamps[k] == new Vector3(0f, 2.9f, 1.8f + 3f * k) && l.ownLampShadows[k] == k, tag + "strip " + k + " at z " + F(l.ownLamps[k].z));
            // Heads every two cells; the goal unit two cells before the goal wall; at most one 9 m interval (even counts), the last.
            var u = l.units;
            var count = (n - 1) / 2;
            Require(u.Length == count, tag + u.Length + " units, want " + count);
            Require(u[0].z == 3f && u[0].side > 0f && !u[0].remote && !u[0].goalPlus && u[0].trimPlus == -.23f && u[1].trimMinus == -.23f, tag + "the entry unit");
            Require(u[count - 1].z == len - 6f && u[count - 1].goalPlus && !u[count - 1].remote, tag + "the goal unit at z " + F(u[count - 1].z));
            var nine = 0;
            for (var i = 1; i < count; i++)
            {
                var gap = u[i].z - u[i - 1].z;
                if (gap == 9f) nine++;
                Require(gap == 6f || (gap == 9f && i == count - 1), tag + "units " + (i - 1) + "-" + i + " " + F(gap) + " m apart");
                Require(u[i].side == -u[i - 1].side && u[i].autoAim, tag + "unit " + i + " not on the other wall");
                Require(u[i].remote == (i < count - 1) && u[i].goalPlus == (i == count - 1), tag + "unit " + i + " remote / goal");
                if (i > 1) Require(u[i].trimMinus == 0f && u[i].trimPlus == 0f, tag + "unit " + i + " trimmed");
            }
            Require(nine == (n % 2 == 0 ? 1 : 0), tag + nine + " intervals of 9 m");
            // The goal assembly at the end; the faces, floor, services, post over 0 .. L.
            Require(l.goalPoint == new Vector3(0f, 1.05f, len) && l.goalNormal == Vector3.back, tag + "goal point " + l.goalPoint);
            Require(l.walls.Length == 4 && l.walls[0].length == len && l.walls[1].length == len && l.walls[2].start.z == .08f && Mathf.Abs(l.walls[3].start.z - (len - .08f)) < 1e-5f, tag + "faces");
            Require(l.floorZ1 == len && l.runZ1 == len && l.postCentre.z == len * .5f && l.postSize.z == len, tag + "floor / runs / post");
            // Probes by segment: (n + 2) / 3 boxes covering the corridor, the goal box at L - 4.54, the 9-cell bake's cubes.
            var segs = (n + 2) / 3;
            Require(l.probes.Length == segs, tag + l.probes.Length + " probe boxes, want " + segs);
            Require(l.probes[0].centre.z == 4.5f && Mathf.Abs(l.probes[segs - 1].centre.z - (len - 4.54f)) < 1e-4f, tag + "end probes");
            var reach = -1f;
            for (var i = 0; i < segs; i++)
            {
                var c = l.probes[i].centre.z; var h = l.probes[i].size.z * .5f;
                Require(l.probes[i].size == new Vector3(3.16f, 3.22f, 9.6f) && c - h <= Mathf.Max(0f, reach) + 1e-4f, tag + "probe " + i + " leaves a gap");
                if (i > 0 && i < segs - 1) Require(c == 4.5f + 9f * i, tag + "probe " + i + " at z " + F(c));
                reach = Mathf.Max(reach, c + h);
            }
            Require(reach >= len, tag + "the probes stop at z " + F(reach));
            if (n == RunRigLayout.TodayCells) Require(string.IsNullOrEmpty(l.probeBake) && l.probeCubes.Length == 0, tag + "probe bake set");
            else
            {
                Require(l.probeBake == RunRigLayout.ProbeBakeName && l.probeCubes.Length == segs && l.probeCubes[0] == 0 && l.probeCubes[segs - 1] == 2, tag + "probe cubes");
                for (var i = 1; i < segs - 1; i++) Require(l.probeCubes[i] == 1, tag + "probe cube " + i);
            }
            // An obstacle in every pool but the goal's, on alternate walls.
            Require(l.obstacles.Length == count - 1, tag + l.obstacles.Length + " obstacles");
            for (var p = 0; p < l.obstacles.Length; p++)
            {
                var o = l.obstacles[p];
                var pool = (u[p].z + u[p + 1].z) * .5f;
                Require(Mathf.Abs(o.position.z - pool) <= .5f && Mathf.Sign(o.position.x) == (p % 2 == 0 ? 1f : -1f), tag + "obstacle " + p + " at " + o.position + ", pool " + F(pool));
            }
            // Side doors: ELECTRICAL in cell 1 (right), NOT AN EXIT two cells before the goal (right), STAFF ONLY in cell 5 (left) when
            // a cell short of it; each 1.2 m or more from a unit on its wall and clear of every other door and cabinet there; gapped.
            var doors = l.sideDoors;
            var staff = 17f <= len - 7f;
            Require(doors.Length == (staff ? 3 : 2) && doors[0].z == 5f && doors[0].side > 0f && doors[0].face == "electrical", tag + doors.Length + " side doors");
            Require(doors[doors.Length - 1].z == len - 4f && doors[doors.Length - 1].side > 0f && doors[doors.Length - 1].face == "stencil", tag + "NOT AN EXIT at z " + F(doors[doors.Length - 1].z));
            if (staff) Require(doors[1].z == 17f && doors[1].side < 0f && doors[1].face == "staff", tag + "STAFF ONLY");
            var cabinets = new List<float>();
            foreach (var it in l.wallItems) if (it.kit == "Kit_ExtinguisherCabinet") cabinets.Add(it.point.z);
            foreach (var d in doors)
            {
                foreach (var unit in u) if (unit.side == d.side) Require(Mathf.Abs(d.z - unit.z) >= 1.2f, tag + "door at z " + F(d.z) + " on the unit at z " + F(unit.z));
                foreach (var e in doors) if (e.z != d.z && e.side == d.side) Require(Mathf.Abs(e.z - d.z) >= 3f, tag + "doors at z " + F(d.z) + " and " + F(e.z));
                if (d.side < 0f) foreach (var c in cabinets) Require(Mathf.Abs(c - d.z) >= .7f, tag + "door at z " + F(d.z) + " on the cabinet at z " + F(c));
                var face = l.walls[d.side > 0f ? 1 : 0];
                var gapped = false;
                foreach (var g in face.gaps) if (Mathf.Abs(g.x - (d.z - .545f)) < 1e-4f && Mathf.Abs(g.y - (d.z + .545f)) < 1e-4f) gapped = true;
                Require(gapped, tag + "no face gap for the door at z " + F(d.z));
            }
            // Extinguisher cabinets on the left: every point of the corridor within 75 ft (22.86 m) of one; each gapped.
            Require(cabinets.Count == (len > 10.2f + 22.86f ? 2 : 1) && cabinets[0] == 10.2f, tag + cabinets.Count + " cabinets");
            for (var z = 0f; z <= len; z += .5f)
            {
                var near = float.MaxValue;
                foreach (var c in cabinets) near = Mathf.Min(near, Mathf.Abs(z - c));
                Require(near <= 22.86f, tag + "z " + F(z) + " is " + F(near) + " m from a cabinet");
            }
            // Compartment frames: none unless asked; asked, FD1 on z 9 and FD2 on z 18 (71 s.10, card 2), FD2 only 3 m or more from the
            // goal unit and 6 m from the goal wall (so not at 7 or 8 cells); every unit but the goal unit half a cell past its line.
            Require(l.compartmentFrames.Length == 0, tag + "frames without being asked");
            var framed = RunRigLayout.Corridor(n, 1.4f, 1f, true);
            Require(framed.compartmentFrames.Length == (n >= 9 ? 2 : 1), tag + framed.compartmentFrames.Length + " frames");
            for (var i = 0; i < framed.compartmentFrames.Length; i++) Require(framed.compartmentFrames[i] == 9f * (i + 1) && framed.compartmentFrames[i] < len, tag + "frame " + i);
            foreach (var fu in framed.units)
                foreach (var fz in framed.compartmentFrames)
                    Require(Mathf.Abs(fu.z - fz) >= 1f, tag + "framed: a unit at z " + F(fu.z) + " stands in the frame on z " + F(fz));
            // The binder reads the length from the room's record.
            Require(FrontRoomsRunRigBinder.CellsOf(new FrontRooms.Map.FrontRoomsRunRoom { Cells = new FrontRooms.Map.GridCoord[n] }) == n, tag + "CellsOf");
            // Built: lights, the lanes, the trip and the reset over the current length.
            FrontRoomsRunRig rig;
            var root = BuildRoom("TEST run b cells " + n, l, out rig);
            try
            {
                var wraps = Named(rig, "run b wrap light");
                var heads = Named(rig, "head light");
                Require(wraps.Length == n && heads.Length == 2 * count, tag + wraps.Length + " wraps, " + heads.Length + " heads");
                int total, on, sh;
                rig.Evaluate(-1f); rig.CountLights(out total, out on, out sh);
                var armed = on + "/" + sh;
                Require(on == n + 1 && sh == n, tag + "armed " + armed);
                rig.Evaluate(3f); rig.CountLights(out total, out on, out sh);
                var tripped = on + "/" + sh;
                Require(on == 2 * count + 1 && sh == 2 * count, tag + "tripped " + tripped);
                var lane = float.MaxValue;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!t.name.StartsWith("run b obstacle ", StringComparison.Ordinal)) continue;
                    float x0 = float.MaxValue, x1 = float.MinValue;
                    foreach (var bc in t.GetComponentsInChildren<BoxCollider>(true))
                        for (var c = 0; c < 8; c++)
                        {
                            var corner = bc.center + Vector3.Scale(bc.size * .5f, new Vector3((c & 1) == 0 ? -1f : 1f, (c & 2) == 0 ? -1f : 1f, (c & 4) == 0 ? -1f : 1f));
                            var x = root.InverseTransformPoint(bc.transform.TransformPoint(corner)).x;
                            x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x);
                        }
                    if (x0 <= x1) lane = Mathf.Min(lane, Mathf.Max(x0 + hw, hw - x1));
                }
                Require(l.obstacles.Length == 0 || lane >= 1.4f, tag + "lane " + F(lane) + " m beside an obstacle");
                var trip = new LampTrace(Lights(rig));
                for (var ms = -100; ms <= 3000; ms += 2) { rig.Evaluate(ms / 1000f); trip.Sample(ms / 1000f); }
                string worstLamp; var tf = trip.WorstFlashes(out worstLamp);
                Require(tf <= 3f, tag + "trip " + F(tf) + " flashes/s on " + worstLamp);
                // The reset: n x resetCellSeconds of sweep from the entrance; the last strip rises once, from its cell's start.
                var want = l.resetRiseStart + n * l.resetCellSeconds + l.resetRise + .2f;
                Require(Mathf.Abs(rig.ResetDuration - want) < 1e-4f, tag + "reset " + F(rig.ResetDuration) + " s, want " + F(want));
                if (n == RunRigLayout.TodayCells) reset9 = rig.ResetDuration;
                var lastZ = l.ownLamps[n - 1].z;
                var lastStart = l.resetRiseStart + (n - 1) * l.resetCellSeconds;
                rig.ResetImmediate();
                Require(rig.RestrikeLevel(lastStart - .01f, lastZ) == 0f && rig.RestrikeLevel(lastStart + .2f, lastZ) > .35f, tag + "the last strip does not rise at " + F(lastStart) + " s");
                int flicks; string window; float seconds;
                var rf = ResetWindows(rig, out flicks, out window, out seconds);
                Require(rig.State == RunRigState.Armed && Mathf.Abs(seconds - want) <= 2f / 240f, tag + "the reset ticked " + F(seconds) + " s, want " + F(want));
                Require(flicks == 1 && rf <= 3f, tag + "reset " + F(rf) + " flashes/s (" + window + "), " + flicks + " flicks a strip");
                sb.Append(n).Append(" cells ").Append(F(len)).Append(" m: units ");
                for (var i = 0; i < count; i++) sb.Append(i > 0 ? "," : "").Append(F(u[i].z)).Append(u[i].side > 0f ? "R" : "L");
                sb.Append("; doors ").Append(doors.Length).Append(", obstacles ").Append(l.obstacles.Length).Append(", cabinets ").Append(cabinets.Count)
                  .Append(", probes ").Append(segs).Append("; lights armed ").Append(armed).Append(", tripped ").Append(tripped)
                  .Append("; reset ").Append(F(rig.ResetDuration)).Append(" s, ").Append(F(rf)).Append(" flashes/s; lane ").Append(F(lane)).Append(" m; ")
                  .Append(Cost(root)).Append(" | ");
            }
            finally { UnityEngine.Object.DestroyImmediate(root.gameObject); }
        }
        // A corridor outside 7 .. 14 cells is refused (a warning) and built at 9.
        Require(RunRigLayout.Corridor(6).Cells == 9 && RunRigLayout.Corridor(15).Cells == 9 && !RunRigLayout.ValidCells(6) && !RunRigLayout.ValidCells(15), "6 or 15 cells not refused");
        Require(Mathf.Abs(reset9 - 2.4f) < 1e-4f, "today's reset " + F(reset9) + " s, not 2.4");
        Log.AppendLine("R19 COST " + sb);
        return sb.Append("6 and 15 cells refused (built at 9)").ToString();
    }

    // ------------------------------------------------------------------ card 2: the receding end
    static string R20Recede()
    {
        var sb = new StringBuilder();
        // 1. The schedule (71 s.4.5, motion.txt R5).
        var t2r9 = new RunRecedeSchedule(27f, 36f, RunRecedeBody.T2Restack);
        var t2r15 = new RunRecedeSchedule(27f, 42f, RunRecedeBody.T2Restack);
        var t1r15 = new RunRecedeSchedule(27f, 42f, RunRecedeBody.T1Roll);
        Require(t2r9.Steps == 2 && Near(t2r9.SeatedAt(0), 1.55f) && Near(t2r9.SeatedAt(1), 3.35f), "T2 R 9 seats " + F(t2r9.SeatedAt(0)) + " / " + F(t2r9.SeatedAt(1)));
        Require(t2r15.Steps == 4 && Near(t2r15.SeatedAt(0), 1.55f) && Near(t2r15.SeatedAt(1), 2.90f) && Near(t2r15.SeatedAt(2), 4.25f) && Near(t2r15.SeatedAt(3), 6.05f),
            "T2 R 15 seats " + F(t2r15.SeatedAt(0)) + " / " + F(t2r15.SeatedAt(1)) + " / " + F(t2r15.SeatedAt(2)) + " / " + F(t2r15.SeatedAt(3)));
        Require(t1r15.Steps == 4 && Near(t1r15.SeatedAt(0), 1.15f) && Near(t1r15.SeatedAt(1), 2.15f) && Near(t1r15.SeatedAt(2), 3.15f) && Near(t1r15.SeatedAt(3), 4.85f),
            "T1 R 15 seats " + F(t1r15.SeatedAt(0)) + " / " + F(t1r15.SeatedAt(3)));
        Require(t2r9.StepTo(0) == 33f && t2r9.StepTo(1) == 36f && t2r15.StepTo(1) == 36f && t2r15.StepTo(2) == 39f, "seat lines");
        // the travel's peak (s / (t - 0.175)) and the glide's 6 m/s
        var peak = 0f;
        for (var u = 0f; u < 1.1f; u += .001f) peak = Mathf.Max(peak, (RunRecedeSchedule.TravelAt(u + .001f, 1.1f, 6f) - RunRecedeSchedule.TravelAt(u, 1.1f, 6f)) / .001f);
        Require(Mathf.Abs(peak - 6f / (1.1f - .175f)) < .05f, "the flinch peaks at " + F(peak));
        var gl = RunRecedeSchedule.GlideTravel(9f);
        var gpeak = 0f;
        for (var u = 0f; u < gl; u += .001f) gpeak = Mathf.Max(gpeak, (RunRecedeSchedule.TravelAt(u + .001f, gl, 9f) - RunRecedeSchedule.TravelAt(u, gl, 9f)) / .001f);
        Require(Mathf.Abs(gpeak - RunRecedeSchedule.GlideSpeed) < .05f, "the glide peaks at " + F(gpeak));
        // the end never moves toward the player but in the tell's 15 mm jolt; it ends on its line
        float line, off, local; RunRecedePhase ph; int st;
        var last = 27f;
        for (var t = 0f; t < 3.6f; t += .01f)
        {
            t2r9.Sample(t, out line, out off, out ph, out st, out local);
            Require(line >= last - 1e-4f, "the end came back at t " + F(t));
            Require(off >= -RunRecedeSchedule.Jolt - 1e-5f && off <= RunRecedeSchedule.FinalOvershoot + 1e-5f, "offset " + F(off) + " at t " + F(t));
            last = line;
        }
        t2r9.Sample(4f, out line, out off, out ph, out st, out local);
        Require(ph == RunRecedePhase.Seated && line == 36f && off == 0f, "not seated on 36 at 4 s");
        sb.Append("seats T2 R9 1.55/3.35, R15 1.55/2.9/4.25/6.05, T1 R15 1.15/4.85; flinch peak ").Append(F(peak)).Append(", glide ").Append(F(gpeak));

        // 2. The layout (Receding).
        foreach (var to in new[] { 12, 14 })
        {
            var r = RunRigLayout.Receding(9, to);
            var tag = "9 -> " + to + ": ";
            Require(r.length == 3f * to && r.recedeLine == 27f && r.goalPoint.z == 27f, tag + "lines");
            Require(r.compartmentFrames.Length == 2 && r.compartmentFrames[0] == 9f && r.compartmentFrames[1] == 18f, tag + "frames");
            Require(r.sideDoors.Length == 0, tag + r.sideDoors.Length + " side doors");
            var carried = 0;
            foreach (var u in r.units)
            {
                if (u.carried) { carried++; Require(Near(u.z, 27f + RunRigLayout.CarriedUnitZ) && u.goalPlus, tag + "carried unit"); continue; }
                Require(!u.goalPlus && u.z < 24f, tag + "a fixed unit at z " + F(u.z));
                foreach (var fz in r.compartmentFrames) Require(Mathf.Abs(u.z - fz) >= 1f, tag + "unit in a frame");
            }
            Require(carried == 1 && r.units.Length == 5 && r.units[r.units.Length - 1].carried, tag + "units");
            foreach (var it in r.wallItems) Require(it.point.z <= 27f, tag + it.kit + " at z " + F(it.point.z) + " in the length the end travels");
            Require(r.obstacles.Length == 3, tag + r.obstacles.Length + " obstacles");
            foreach (var o in r.obstacles) Require(o.position.z < 27f - 1.68f, tag + "an obstacle in the goal pool");
            Require(r.ownLamps.Length == to, tag + "strips");
        }
        var refusedWarn = false;
        Application.LogCallback lc = (m, st2, ty) => { if (m.Contains("receding Run!")) refusedWarn = true; };
        Application.logMessageReceived += lc;
        var refused = RunRigLayout.Receding(9, 13);
        Application.logMessageReceived -= lc;
        Require(refusedWarn && refused.recedeLine < 0f && refused.Cells == 9, "9 -> 13 not refused");
        sb.Append("; Receding 9->12/14 by rule, 9->13 refused");

        // 3. A built 9 -> 12 room (look-dev shell).
        var lay = RunRigLayout.Receding(9, 12);
        var root = new GameObject("TEST run b recede").transform;
        root.position = new Vector3(5600f, 0f, 4992f);
        var viewer = new GameObject("TEST recede viewer").transform;
        try
        {
            var dress = FrontRoomsRunDress.Build(root, lay);
            var rec = FrontRoomsRunRecede.Build(dress, lay);
            Require(rec != null, "no recede built");
            var rig = FrontRoomsRunRig.Create(root, lay, RunRigHost.OwnLamps, null, dress);
            rec.Bind(rig);
            Require(rig.GoalMount != null && rig.HiddenWraps == 3 && Near(rig.GoalLine, 27f) && rig.GoalBeyondHidden, "built: mount " + (rig.GoalMount != null) + ", hidden " + rig.HiddenWraps + ", line " + F(rig.GoalLine));
            var cellsOff = 0;
            foreach (Transform t in dress.Find("run b recede hidden cells")) if (!t.gameObject.activeSelf && t.childCount > 0) cellsOff++;
            Require(cellsOff == 3, cellsOff + " hidden cells off");
            var heads = Named(rig, "head light");
            Require(heads.Length == 10, heads.Length + " head lights (4 fixed units + the carried one)");
            var cues = new List<string>();
            var seats = new List<string>();
            rec.Cue += (c, i, p) => cues.Add(c + ":" + i);
            rec.SeatReached += (i, z, fin) => seats.Add(i + "@" + F(z) + (fin ? "F" : ""));
            // walk the viewer down the axis facing the goal, 3.2 m/s from z 9 (the trip), 60 Hz
            viewer.position = root.TransformPoint(new Vector3(0f, 1.62f, 9f));
            viewer.rotation = root.rotation;
            rec.SetViewer(viewer);
            rig.SetViewer(viewer);
            rig.Trip();
            rec.Trip();
            var strikeSeen = 0f;
            for (var k = 0; k < 60 * 6; k++)
            {
                viewer.position = root.TransformPoint(new Vector3(0f, 1.62f, 9f + 3.2f * k / 60f));
                rig.Tick(Dt);
                rec.Tick(Dt);
                if (rec.Seated && strikeSeen <= 0f) strikeSeen = k / 60f;
            }
            Require(rec.Seated && Near(rec.Line, 36f) && rec.StepsDone == 2 && !rec.Gliding, "walker: seated " + rec.Seated + " line " + F(rec.Line) + " steps " + rec.StepsDone + " glide " + rec.Gliding);
            Require(seats.Count == 2 && seats[0] == "0@33" && seats[1] == "1@36F", "seats " + string.Join(",", seats.ToArray()));
            Require(rig.HiddenWraps == 0 && !rig.GoalBeyondHidden && Near(rig.GoalLine, 36f), "after the seat: hidden " + rig.HiddenWraps + " line " + F(rig.GoalLine));
            Require(cues.IndexOf("MagnetRelease:0") == 0 && cues.Contains("MagnetRelease:1") && cues.Contains("TrolleyRoll:0") && cues.Contains("Seat:0") && cues.Contains("FinalSeat:1")
                && cues.Contains("StripStrike:0") && cues.Contains("StripStrike:2"), "cues " + string.Join(",", cues.ToArray()));
            Require(Near(rig.GoalMount.localPosition.z, 9f) && Near(rec.Head.localPosition.z, 36f), "the mount rides the end");
            var record = rec.Record;
            Require(record.Started && record.Done && record.steps == 2 && record.glideFrom == 255, "record " + record.flags + "/" + record.steps);
            sb.Append("; walker: seats ").Append(string.Join(",", seats.ToArray())).Append(", ").Append(cues.Count).Append(" cues, strips struck, record 8 B");

            // the keep-away: a viewer 5 m from the end when the gate opens glides the whole way
            var root2 = new GameObject("TEST run b recede glide").transform;
            root2.position = new Vector3(5700f, 0f, 4992f);
            try
            {
                var d2 = FrontRoomsRunDress.Build(root2, lay);
                var r2 = FrontRoomsRunRecede.Build(d2, lay);
                var g2 = FrontRoomsRunRig.Create(root2, lay, RunRigHost.OwnLamps, null, d2);
                r2.Bind(g2);
                viewer.position = root2.TransformPoint(new Vector3(0f, 1.62f, 22f));
                viewer.rotation = root2.rotation;
                r2.SetViewer(viewer);
                r2.Trip();
                var seatedAt = -1f;
                for (var k = 0; k < 60 * 4 && seatedAt < 0f; k++) { r2.Tick(Dt); if (r2.Seated) seatedAt = (k + 1) / 60f; }
                var expect = RunRecedeSchedule.GlideTell + RunRecedeSchedule.GlideTravel(9f) + .35f;
                Require(r2.Gliding && r2.Seated && Mathf.Abs(seatedAt - expect) < .05f, "glide: seated at " + F(seatedAt) + " (expected " + F(expect) + ")");
                // Restore from the first room's record
                var root3 = new GameObject("TEST run b recede restore").transform;
                root3.position = new Vector3(5800f, 0f, 4992f);
                try
                {
                    var d3 = FrontRoomsRunDress.Build(root3, lay);
                    var r3 = FrontRoomsRunRecede.Build(d3, lay);
                    var g3 = FrontRoomsRunRig.Create(root3, lay, RunRigHost.OwnLamps, null, d3);
                    r3.Bind(g3);
                    var quietCues = 0;
                    r3.Cue += (c, i, p) => quietCues++;
                    r3.Restore(record);
                    Require(r3.Seated && Near(r3.Line, 36f) && g3.HiddenWraps == 0 && quietCues == 0, "restore: seated " + r3.Seated + ", cues " + quietCues);
                }
                finally { UnityEngine.Object.DestroyImmediate(root3.gameObject); }
                sb.Append("; glide seated at ").Append(F(seatedAt)).Append(" s, restore seated quietly");
            }
            finally { UnityEngine.Object.DestroyImmediate(root2.gameObject); }

            // the gaze gate: facing the entrance at 9.5 m it stays shut; facing the goal it opens
            var root4 = new GameObject("TEST run b recede gate").transform;
            root4.position = new Vector3(5900f, 0f, 4992f);
            try
            {
                var d4 = FrontRoomsRunDress.Build(root4, lay);
                var r4 = FrontRoomsRunRecede.Build(d4, lay);
                var g4 = FrontRoomsRunRig.Create(root4, lay, RunRigHost.OwnLamps, null, d4);
                r4.Bind(g4);
                viewer.position = root4.TransformPoint(new Vector3(0f, 1.62f, 9.5f));
                viewer.rotation = root4.rotation * Quaternion.Euler(0f, 180f, 0f);
                r4.SetViewer(viewer);
                r4.Trip();
                for (var k = 0; k < 60; k++) r4.Tick(Dt);
                Require(r4.Phase == RunRecedePhase.Waiting && Near(r4.Line, 27f), "gate open facing away: " + r4.Phase);
                viewer.rotation = root4.rotation * Quaternion.Euler(0f, 30f, 0f);
                r4.Tick(Dt);
                Require(r4.Phase == RunRecedePhase.Tell, "gate shut facing 30 deg off the goal: " + r4.Phase);
            }
            finally { UnityEngine.Object.DestroyImmediate(root4.gameObject); }
            sb.Append("; gaze gate shut facing away, open at 30 deg");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            UnityEngine.Object.DestroyImmediate(viewer.gameObject);
        }

        // 4. Today's room is untouched: no goal mount, no hidden strip, the gate on z 27.
        using (var today = new Corridor())
        {
            Require(today.rig.GoalMount == null && today.rig.HiddenWraps == 0 && today.rig.GoalLine == 27f && !today.rig.GoalBeyondHidden, "today's room changed");
            Require(today.rig.Layout.recedeLine < 0f, "today's layout recedes");
        }
        return sb.Append("; today's room untouched").ToString();
    }

    static bool Near(float a, float b) { return Mathf.Abs(a - b) < .006f; }

    /// <summary>R21 (card 2, the T2 body, 71 s.5.3): the pieces and their clips; a built 9 -> 12 room re-stacks at every step and
    /// is whole again at each seat; the cues come in order (bottom-up landings); the glide and a restore keep the wall whole;
    /// desktop blocks with grit, iPad blocks without, phones the strips; a plain room builds none of it.</summary>
    static string R21Restack()
    {
        var sb = new StringBuilder();
        // 1. The clips: T2's phases, exact rest at both ends, landings from the bottom up.
        foreach (var kit in new[] { FrontRoomsRunRestack.BlocksKit, FrontRoomsRunRestack.StripsKit })
        {
            var n = kit == FrontRoomsRunRestack.BlocksKit ? 69 : 28;
            foreach (var suffix in new[] { FrontRoomsRunRestack.FlinchSuffix, FrontRoomsRunRestack.StepSuffix })
            {
                var c = FrontRoomsRunRestack.LoadClip(kit + suffix);
                var tag = kit + suffix + ": ";
                Require(c != null && c.hz == 30 && c.pieces.Length == n, tag + "missing or " + (c != null ? c.pieces.Length : -1) + " pieces");
                var flinch = suffix == FrontRoomsRunRestack.FlinchSuffix;
                Require(Mathf.Abs(c.phases[0] - (flinch ? .25f : .15f)) < 1e-4f && Mathf.Abs(c.phases[1] - (flinch ? 1.1f : .8f)) < 1e-4f && Mathf.Abs(c.phases[2] - .2f) < 1e-4f
                    && Mathf.Abs(c.distance - (flinch ? 6f : 3f)) < 1e-4f, tag + "phases");
                Require(Mathf.Abs(c.frames - 1 - c.duration * 30f) <= 1f, tag + c.frames + " frames for " + F(c.duration) + " s");
                foreach (var p in c.pieces)
                {
                    Require(p.keys.Length == c.frames * 7, tag + p.name + " keys");
                    for (var e = 0; e < 2; e++)
                    {
                        var o = e == 0 ? 0 : (c.frames - 1) * 7;
                        Require(Mathf.Abs(p.keys[o] - p.rest[0]) < 1e-4f && Mathf.Abs(p.keys[o + 1] - p.rest[1]) < 1e-4f && Mathf.Abs(p.keys[o + 2]) < 1e-4f
                            && Mathf.Abs(p.keys[o + 6] - 1f) < 1e-4f, tag + p.name + (e == 0 ? " does not start" : " does not end") + " at rest");
                    }
                }
                var lastCourse = -1; var lastT = -1f; var courses = 0;
                foreach (var e in c.contacts)
                {
                    Require(e.t >= lastT - 1e-5f, tag + "contacts out of order");
                    lastT = e.t;
                    if (e.kind != "course") continue;
                    Require(e.course == lastCourse + 1, tag + "course " + e.course + " landed after " + lastCourse);
                    lastCourse = e.course; courses++;
                }
                Require(courses == 13, tag + courses + " course landings");
            }
        }
        sb.Append("clips: blocks 69 / strips 28 x flinch (.25/1.1/.2, 6 m) and step (.15/.8/.2, 3 m), rest at both ends, courses land 0..12");

        // 2. A built 9 -> 12 room, desktop: the wall comes apart at each tell and is whole at each seat.
        var lay = RunRigLayout.Receding(9, 12);
        var root = new GameObject("TEST run b restack").transform;
        root.position = new Vector3(6000f, 0f, 4992f);
        var viewer = new GameObject("TEST restack viewer").transform;
        try
        {
            var dress = FrontRoomsRunDress.Build(root, lay);
            var rec = FrontRoomsRunRecede.Build(dress, lay);
            var rig = FrontRoomsRunRig.Create(root, lay, RunRigHost.OwnLamps, null, dress);
            rec.Bind(rig);
            var body = rec.Restack;
            Require(body != null && body.BodyTier == FrontRoomsRunRestack.Tier.Desktop && body.PieceCount == 69 && body.Levels == 3, "desktop body: " + (body != null ? body.BodyTier + " " + body.PieceCount + " pieces " + body.Levels + " levels" : "none"));
            var tris = body.LevelTriangles;
            Require(tris[0] > tris[1] && tris[1] > tris[2] && tris[2] >= 69 * 12, "level triangles " + tris[0] + "/" + tris[1] + "/" + tris[2]);
            Require(rec.WallSkins != null && rec.WallSkins.activeSelf && !body.gameObject.activeSelf && !body.Active, "at rest: the static wall shows, the body is hidden");
            var gated = 0;
            foreach (var r in body.Renderers) foreach (var m in r.sharedMaterials) if (FrontRoomsRunGate.IsTwin(m)) gated++;
            Require(gated > 0, "the body's materials are not in the room's light gate");
            var cues = new List<string>();
            rec.Cue += (cue, i, p) => cues.Add(cue + ":" + i);
            viewer.position = root.TransformPoint(new Vector3(0f, 1.62f, 9f));
            viewer.rotation = root.rotation;
            rec.SetViewer(viewer);
            rig.SetViewer(viewer);
            rig.Trip();
            rec.Trip();
            var flew = 0; var wholeInDwell = 0; var apartInDwell = 0; var maxOut = 0f;
            for (var k = 0; k < 60 * 6; k++)
            {
                viewer.position = root.TransformPoint(new Vector3(0f, 1.62f, 9f + 3.2f * k / 60f));
                rig.Tick(Dt);
                rec.Tick(Dt);
                if (body.Active) flew++;
                if (rec.Phase == RunRecedePhase.Dwell) { if (rec.WallSkins.activeSelf && !body.gameObject.activeSelf) wholeInDwell++; else apartInDwell++; }
                if (body.Active && (rec.Phase == RunRecedePhase.Travel))
                    foreach (Transform b in body.transform)
                        if (b.name.StartsWith("p", StringComparison.Ordinal)) maxOut = Mathf.Max(maxOut, -b.localPosition.z);
            }
            Require(rec.Seated && rec.StepsDone == 2 && !body.Active && rec.WallSkins.activeSelf && !body.gameObject.activeSelf, "after the final seat the wall is whole: active " + body.Active);
            Require(flew > 60 && wholeInDwell > 0 && apartInDwell == 0, "flew " + flew + " frames, dwell whole " + wholeInDwell + " apart " + apartInDwell);
            Require(maxOut > .3f && maxOut < 4f, "the pieces hung back at most " + F(maxOut) + " m");
            var iCrack = cues.IndexOf("MortarCrack:0"); var iSlide = cues.IndexOf("SubframeSlide:0");
            var iLand0 = cues.IndexOf("CourseLand:0"); var iLand12 = cues.IndexOf("CourseLand:12");
            Require(iCrack > 0 && iSlide > iCrack && cues.Contains("BlockLift:12") && cues.Contains("StreamClatter:0") && iLand0 > iSlide && iLand12 > iLand0 && cues.Contains("MortarSift:12"),
                "T2 cues: crack " + iCrack + " slide " + iSlide + " land0 " + iLand0 + " land12 " + iLand12);
            var landings = 0; foreach (var cu in cues) if (cu.StartsWith("CourseLand:", StringComparison.Ordinal)) landings++;
            Require(landings == 26, landings + " course landings in two steps");
            Require(body.HasGrit && body.Grit.Emitted > 0, "desktop grit: " + (body.HasGrit ? body.Grit.Emitted + " grains" : "none"));
            sb.Append("; desktop: tris ").Append(tris[0]).Append("/").Append(tris[1]).Append("/").Append(tris[2]).Append(", flew ").Append(flew).Append(" frames, hung back ")
                .Append(F(maxOut)).Append(" m, whole in every dwell, 26 landings bottom-up, grit ").Append(body.Grit.Emitted);

            // 3. The keep-away glide: the wall rides whole (T1 under T2).
            var root2 = new GameObject("TEST run b restack glide").transform;
            root2.position = new Vector3(6100f, 0f, 4992f);
            try
            {
                var d2 = FrontRoomsRunDress.Build(root2, lay);
                var r2 = FrontRoomsRunRecede.Build(d2, lay);
                var g2 = FrontRoomsRunRig.Create(root2, lay, RunRigHost.OwnLamps, null, d2);
                r2.Bind(g2);
                viewer.position = root2.TransformPoint(new Vector3(0f, 1.62f, 22f));
                viewer.rotation = root2.rotation;
                r2.SetViewer(viewer);
                r2.Trip();
                var apart = 0;
                for (var k = 0; k < 60 * 4; k++) { r2.Tick(Dt); if (r2.Restack != null && r2.Restack.Active) apart++; }
                Require(r2.Gliding && r2.Seated && apart == 0 && r2.WallSkins.activeSelf, "glide: apart " + apart + " frames");
                // a restore: seated whole, no event
                var root3 = new GameObject("TEST run b restack restore").transform;
                root3.position = new Vector3(6200f, 0f, 4992f);
                try
                {
                    var d3 = FrontRoomsRunDress.Build(root3, lay);
                    var r3 = FrontRoomsRunRecede.Build(d3, lay);
                    var g3 = FrontRoomsRunRig.Create(root3, lay, RunRigHost.OwnLamps, null, d3);
                    r3.Bind(g3);
                    var quiet = 0;
                    r3.Cue += (cue, i, p) => quiet++;
                    r3.Restore(rec.Record);
                    Require(r3.Seated && quiet == 0 && r3.WallSkins.activeSelf && !r3.Restack.Active, "restore: whole and quiet");
                }
                finally { UnityEngine.Object.DestroyImmediate(root3.gameObject); }
            }
            finally { UnityEngine.Object.DestroyImmediate(root2.gameObject); }
            sb.Append("; glide and restore ride whole");

            // 4. The tiers: iPad blocks without grit, phones the strips without grit.
            foreach (var phone in new[] { false, true })
            {
                var root5 = new GameObject("TEST run b restack tier").transform;
                root5.position = new Vector3(phone ? 6400f : 6300f, 0f, 4992f);
                FrontRoomsRunRestack.ForceTabletForTools = !phone;
                FrontRoomsRunRestack.ForcePhoneForTools = phone;
                try
                {
                    var d5 = FrontRoomsRunDress.Build(root5, lay);
                    var r5 = FrontRoomsRunRecede.Build(d5, lay);
                    var b5 = r5.Restack;
                    if (phone) Require(b5 != null && b5.BodyTier == FrontRoomsRunRestack.Tier.Phone && b5.PieceCount == 28 && b5.Levels == 1 && !b5.HasGrit, "phone: strips, no grit");
                    else Require(b5 != null && b5.BodyTier == FrontRoomsRunRestack.Tier.Tablet && b5.PieceCount == 69 && b5.Levels == 3 && !b5.HasGrit, "iPad: blocks, no grit");
                }
                finally
                {
                    FrontRoomsRunRestack.ForceTabletForTools = false;
                    FrontRoomsRunRestack.ForcePhoneForTools = false;
                    UnityEngine.Object.DestroyImmediate(root5.gameObject);
                }
            }
            sb.Append("; iPad blocks no grit, phone 28 strips no grit");

            // 5. Today: a plain 9-cell room builds no end, no wall kit, no body.
            var root6 = new GameObject("TEST run b plain").transform;
            root6.position = new Vector3(6500f, 0f, 4992f);
            try
            {
                var d6 = FrontRoomsRunDress.Build(root6, RunRigLayout.Corridor(9));
                Require(FrontRoomsRunRecede.Build(d6, RunRigLayout.Corridor(9)) == null && d6.GetComponentInChildren<FrontRoomsRunRestack>(true) == null, "a plain room built a re-stack");
            }
            finally { UnityEngine.Object.DestroyImmediate(root6.gameObject); }
            sb.Append("; a plain 9-cell room builds none");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            UnityEngine.Object.DestroyImmediate(viewer.gameObject);
        }
        return sb.ToString();
    }

    static string W1Gate()
    {
        Require(!FrontRoomsRunRig.WebGLTier, "WebGLTier is on in the editor");
        return "WebGLTier false (desktop path); the preview-define compile is roslyn_check with FRONTROOMS_WEBGL_PREVIEW";
    }

    // ------------------------------------------------------------------ card 3 (71 s.6.3-6.7, decision 6): the red looks
    /// <summary>DefaultRedLook as RunBatch found it: the look the build ships.</summary>
    static RunRedLook shippedLook;

    static Corridor LookCorridor(RunRedLook look)
    {
        var c = new Corridor();
        c.rig.RedLook = look;
        return c;
    }

    static float TickTo(FrontRoomsRunRig rig, float t, float until)
    {
        while (t < until - 1e-4f) { rig.Tick(Dt); t += Dt; }
        return t;
    }

    static bool Near(float a, float b, float tol) { return Mathf.Abs(a - b) <= tol; }

    static bool NearColor(Color a, Color b, float tol) { return Near(a.r, b.r, tol) && Near(a.g, b.g, tol) && Near(a.b, b.b, tol); }

    static Renderer Named(Transform root, string name)
    {
        foreach (var r in root.GetComponentsInChildren<Renderer>(true)) if (r.name == name) return r;
        return null;
    }

    static string R23RedLooks()
    {
        var sb = new StringBuilder();
        Require(shippedLook == RunRedLook.AlarmPulse, "the build ships " + shippedLook + ", not B (B passed the WCAG 2.3.1 check, flash_check.py)");
        sb.Append("ships B (A the fallback)");

        // 1. Armed is the same in all three looks.
        using (var today = LookCorridor(RunRedLook.Today))
        using (var a = LookCorridor(RunRedLook.AlarmCircuit))
        using (var b = LookCorridor(RunRedLook.AlarmPulse))
        {
            today.rig.Evaluate(-1f); a.rig.Evaluate(-1f); b.rig.Evaluate(-1f);
            Require(Fingerprint(today.rig) == Fingerprint(a.rig) && Fingerprint(a.rig) == Fingerprint(b.rig), "armed differs between the looks");
            for (var k = 0; k < a.rig.OwnLampCount; k++)
                Require(a.rig.WrapLight(k).color == today.rig.WrapLight(k).color, "armed strip " + k + " colour differs");
        }
        sb.Append("; armed identical in Today / A / B");

        // 2. A's front, read through Evaluate(t) (deterministic; an armed rig previews its RedLook's trip).
        using (var c = LookCorridor(RunRedLook.AlarmCircuit))
        {
            var rig = c.rig;
            var l = rig.Layout;
            var n = rig.OwnLampCount;
            for (var k = 0; k < n; k++)
            {
                var f = rig.StripFrontOf(k);
                Require(Near(f, (l.ownLamps[k].z - l.ownLamps[0].z) / l.frontSpeed, 1e-4f), "strip " + k + " front " + F(f));
                if (k > 0) Require(Near(rig.StripWhiteOf(k, f - .005f), 1f, 1e-4f), "strip " + k + " not white before the front");
                Require(rig.StripWhiteOf(k, f + .07f) <= 0f && rig.StripRedOf(k, f + .07f) <= 0f, "strip " + k + " not dark in the band");
                var struck = rig.StripRedOf(k, f + .36f);
                Require(struck >= l.redStrikeLevel - .001f && struck <= .42f, "strip " + k + " red at its strike " + F(struck));
                Require(Near(rig.StripRedOf(k, f + l.headDelay + l.redRise + .01f), 1f, 1e-4f), "strip " + k + " red not full");
            }
            rig.Evaluate(2.0f);
            for (var k = 0; k < n; k++)
            {
                var lt = rig.WrapLight(k);
                Require(lt.enabled && Near(lt.intensity, l.redLightIntensity, .01f) && NearColor(lt.color, l.redLightColor, .01f),
                    "strip " + k + " at 2.0 s: " + F(lt.intensity) + " " + lt.color);
            }
            // The pool heads rise in the band's wake; the goal head (the door's light) at once, as built.
            for (var i = 0; i < rig.HeadCount; i++)
            {
                var z = l.units[i / 2].z;
                var start = rig.HeadIsGoal(i) ? l.headDelay + l.headStagger * z : l.headDelay + Mathf.Max(0f, z - l.ownLamps[0].z) / l.frontSpeed;
                Require(rig.HeadLevelOf(i, start - .01f) <= 0f && Near(rig.HeadLevelOf(i, start + l.headRise + .01f), 1f, 1e-4f),
                    "head " + i + (rig.HeadIsGoal(i) ? " (goal)" : "") + " does not start at " + F(start));
            }
            int total, on, sh;
            rig.Evaluate(3f); rig.CountLights(out total, out on, out sh);
            Require(on == 18 && sh == 17, "A tripped " + on + " on, " + sh + " shadowed");
            sb.Append("; A: strips out at 0.15 s a cell, red struck 0.35 s later, all red by ").Append(F(l.headDelay + rig.StripFrontOf(n - 1) + l.redRise))
              .Append(" s, Light ").Append(F(l.redLightIntensity)).Append(" typed (1, .06, .07), tripped ").Append(on).Append(" on / ").Append(sh).Append(" shadowed");
        }

        // 3. A ticked: the halo and the red tube in their red, one RedStrike per strip in front order, photosafe; the reset white again.
        using (var c = LookCorridor(RunRedLook.AlarmCircuit))
        {
            var rig = c.rig;
            var l = rig.Layout;
            var strikes = new List<int>();
            var strikeTimes = new List<float>();
            var t = 0f;
            rig.RedStrike += (k, p) => { strikes.Add(k); strikeTimes.Add(rig.Clock); };
            var trace = new LampTrace(Lights(rig));
            rig.Trip();
            while (t < 4f) { rig.Tick(Dt); t += Dt; trace.Sample(t); }
            Require(strikes.Count == rig.OwnLampCount, strikes.Count + " RedStrike events");
            for (var k = 0; k < strikes.Count; k++)
                Require(strikes[k] == k && Near(strikeTimes[k], rig.StripFrontOf(k) + l.headDelay, Dt + .001f), "RedStrike " + k + " at " + F(strikeTimes[k]));
            string worst; var flashes = trace.WorstFlashes(out worst);
            Require(flashes <= 3f, "A trip " + F(flashes) + " flashes/s (" + worst + ")");
            var block = new MaterialPropertyBlock();
            var halo = Named(rig.transform, "run b soffit halo");
            if (halo != null)
            {
                halo.GetPropertyBlock(block);
                var hc = (Vector4)block.GetVector("_Color");
                var want = l.redHaloColor;
                Require(halo.enabled && Near(hc.x, want.r, 1e-3f) && Near(hc.y, want.g, 1e-3f) && Near(hc.z, want.b, 1e-3f), "red halo " + hc);
            }
            var tube = Named(rig.transform, "run b strip tubes red");
            if (tube != null)
            {
                tube.GetPropertyBlock(block, 0);
                var got = block.GetColor("_EmissionColor");
                var probe = new MaterialPropertyBlock();
                probe.SetColor("_EmissionColor", l.redTubeLinear.gamma);
                Require(NearColor(got, probe.GetColor("_EmissionColor"), 1e-3f), "red tube " + got);
            }
            rig.StartReset();
            var r = 0f;
            var resetTrace = new LampTrace(Lights(rig));
            while (rig.State != RunRigState.Armed && r < 10f) { rig.Tick(Dt); r += Dt; resetTrace.Sample(r); }
            Require(rig.State == RunRigState.Armed, "the reset did not end");
            flashes = resetTrace.WorstFlashes(out worst);
            Require(flashes <= 3f, "red reset " + F(flashes) + " flashes/s (" + worst + ")");
            for (var k = 0; k < rig.OwnLampCount; k++)
            {
                var lt = rig.WrapLight(k);
                Require(lt.color == l.wrapColor && lt.shadows != LightShadows.None, "strip " + k + " not white again: " + lt.color + " " + lt.shadows);
            }
            if (halo != null)
            {
                halo.GetPropertyBlock(block);
                Require(((Vector4)block.GetVector("_Color")).sqrMagnitude < 1e-8f, "the halo kept its red after the reset");
            }
            sb.Append("; A ticked: ").Append(strikes.Count).Append(" RedStrike in front order, trip ").Append(F(trace.WorstFlashes(out worst)))
              .Append(" flashes/s, halo and tube red, reset white again (").Append(F(flashes)).Append(" flashes/s)");
        }

        // 4. B: 12 pulses from 3 s every 2.5 s, a dip to 0.35, photosafe per lamp, steady red after; held until ReleasePulses.
        using (var c = LookCorridor(RunRedLook.AlarmPulse))
        {
            var rig = c.rig;
            var l = rig.Layout;
            var pulses = new List<float>();
            var t = 0f;
            rig.PulseStarted += (p, at) => pulses.Add(rig.Clock);
            var trace = new LampTrace(Lights(rig));
            rig.Trip();
            var mid = 4;
            var dipAt = l.pulseFirst + rig.StripFrontOf(mid) + l.pulseFall + l.pulseHold * .5f;
            var dip = -1f;
            while (t < 36f)
            {
                rig.Tick(Dt); t += Dt; trace.Sample(t);
                if (dip < 0f && t >= dipAt) dip = rig.WrapLight(mid).intensity;
            }
            Require(pulses.Count == l.pulseCount, pulses.Count + " pulses");
            for (var p = 0; p < pulses.Count; p++)
                Require(Near(pulses[p], l.pulseFirst + p * l.pulsePeriod, Dt + .001f), "pulse " + p + " at " + F(pulses[p]));
            Require(Near(dip, l.redLightIntensity * l.pulseDepth, .08f), "strip " + mid + " mid-dip " + F(dip));
            for (var k = 0; k < rig.OwnLampCount; k++)
                Require(Near(rig.WrapLight(k).intensity, l.redLightIntensity, .01f), "strip " + k + " after the train " + F(rig.WrapLight(k).intensity));
            string worst; var flashes = trace.WorstFlashes(out worst);
            Require(flashes <= 3f, "B " + F(flashes) + " flashes/s (" + worst + ")");
            sb.Append("; B: ").Append(pulses.Count).Append(" pulses from ").Append(F(pulses[0])).Append(" s every ").Append(F(l.pulsePeriod))
              .Append(" s, dip ").Append(F(dip)).Append(", per lamp ").Append(F(flashes)).Append(" flashes/s");
        }
        using (var c = LookCorridor(RunRedLook.AlarmPulse))
        {
            var rig = c.rig;
            var pulses = new List<float>();
            var t = 0f;
            rig.PulseStarted += (p, at) => pulses.Add(rig.Clock);
            rig.PulseHeld = true;
            rig.Trip();
            t = TickTo(rig, t, 5f);
            Require(pulses.Count == 0, "a held train pulsed");
            rig.ReleasePulses(1f);
            t = TickTo(rig, t, 6.6f);
            Require(pulses.Count == 1 && Near(pulses[0], 6f, Dt + .001f), "released train: " + pulses.Count + " pulses, first " + (pulses.Count > 0 ? F(pulses[0]) : "-"));
            sb.Append("; held to 5 s, released: first pulse at ").Append(F(pulses[0])).Append(" s");
        }

        // 5. Reduce Flashing: a 0.5 s cross-fade on every strip, no band, no pulse, each Light monotone after the trip.
        SetReduced(true);
        try
        {
            using (var c = LookCorridor(RunRedLook.AlarmPulse))
            {
                var rig = c.rig;
                var pulses = 0;
                rig.PulseStarted += (p, at) => pulses++;
                rig.Trip();
                var t = TickTo(rig, 0f, .25f);
                for (var k = 0; k < rig.OwnLampCount; k++)
                    Require(Near(rig.StripWhiteOf(k, t), .5f, .04f) && Near(rig.StripRedOf(k, t), .5f, .04f), "reduced strip " + k + " at 0.25 s");
                // Each strip Light only falls from its white to its red (one change, never a dip below it).
                var last = new float[rig.OwnLampCount];
                for (var k = 0; k < last.Length; k++) last[k] = rig.WrapLight(k).intensity;
                while (t < 8f)
                {
                    rig.Tick(Dt); t += Dt;
                    for (var k = 0; k < last.Length; k++)
                    {
                        var v = rig.WrapLight(k).enabled ? rig.WrapLight(k).intensity : 0f;
                        Require(v <= last[k] + 1e-4f && v >= rig.Layout.redLightIntensity - .01f, "reduced strip " + k + " at " + F(t) + ": " + F(v) + " after " + F(last[k]));
                        last[k] = v;
                    }
                }
                Require(pulses == 0, "a pulse under Reduce Flashing");
            }
        }
        finally { SetReduced(false); }
        sb.Append("; Reduce Flashing: 0.5 s cross-fade, no pulse, monotone");

        // 6. TripImmediate: quiet (no strike, no pulse), steady red.
        using (var c = LookCorridor(RunRedLook.AlarmPulse))
        {
            var rig = c.rig;
            var events = 0;
            rig.RedStrike += (k, p) => events++;
            rig.PulseStarted += (k, p) => events++;
            rig.TripImmediate();
            TickTo(rig, 0f, 12f);
            Require(events == 0, events + " events after TripImmediate");
            Require(Near(rig.WrapLight(0).intensity, rig.Layout.redLightIntensity, .01f), "TripImmediate not red");
        }
        sb.Append("; TripImmediate quiet and red");

        // 7. No allocation per Tick through A's front and B's pulses.
        using (var c = LookCorridor(RunRedLook.AlarmPulse))
        {
            var rig = c.rig;
            rig.Trip();
            rig.Tick(Dt);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var k = 0; k < 600; k++) rig.Tick(Dt);
            var alloc = GC.GetAllocatedBytesForCurrentThread() - before;
            Require(alloc == 0, "B ticks allocated " + alloc + " B");
        }
        sb.Append("; Tick 0 B");

        // 8. The body's lamp registry gets each strip's Light (the contact under a strip: white armed, red under A and B).
        using (var c = LookCorridor(RunRedLook.AlarmCircuit))
        {
            var rig = c.rig;
            var lamps = new FrontRoomsTrofferKit.LiveLamp[8];
            var given = 0;
            var strips = FrontRoomsLampKit.Available(FrontRoomsLampKit.Family.Strip) && rig.Layout.bareStrips;
            for (var k = 0; k < rig.OwnLampCount && strips; k++)
            {
                var at = rig.WrapLight(k).transform.position;
                var found = FrontRoomsTrofferKit.LampsNear(at, .3f, lamps);
                for (var i = 0; i < found; i++) if (lamps[i].light == rig.WrapLight(k)) { given++; break; }
            }
            Require(!strips || given == rig.OwnLampCount, given + " of " + rig.OwnLampCount + " strips hand their Light to the registry");
            sb.Append("; registry: ").Append(strips ? given + " strip Lights handed over" : "no strip kit (skipped)");
        }

        // 9. The handheld tier: the kept strips' red at 3.5 x the tier's scale, hard shadows.
        FrontRoomsRunRig.ForceHandheldForTools = true;
        try
        {
            using (var c = LookCorridor(RunRedLook.AlarmCircuit))
            {
                var rig = c.rig;
                rig.Evaluate(3f);
                var kept = 0;
                for (var k = 0; k < rig.OwnLampCount; k++) if (rig.WrapLight(k) != null) kept++;
                var scale = rig.OwnLampCount / (float)Mathf.Max(1, kept);
                for (var k = 0; k < rig.OwnLampCount; k++)
                {
                    var lt = rig.WrapLight(k);
                    if (lt == null) continue;
                    Require(Near(lt.intensity, rig.Layout.redLightIntensity * scale, .02f) && lt.shadows == LightShadows.Hard, "handheld strip " + k + ": " + F(lt.intensity) + " " + lt.shadows);
                }
                sb.Append("; handheld ").Append(kept).Append(" red Lights at ").Append(F(rig.Layout.redLightIntensity * scale)).Append(", hard");
            }
        }
        finally { FrontRoomsRunRig.ForceHandheldForTools = false; }
        return sb.ToString();
    }

    // ------------------------------------------------------------------ RB: the receding room in the binder (the level-design line's card 4)
    /// <summary>
    /// A map holding built Run! rooms of one card, made by the Level Designer's preview (the game's own builder, placement forced, as the
    /// door harness builds it; no kit dressing of the map's doors), bound by the binder as the game binds it (the map's player its viewer
    /// and the end's gaze). room is null when the card was not placed. Counts the map's RunRoomSeated for the room (its open cells).
    /// </summary>
    sealed class RbMap : IDisposable
    {
        /// <summary>The room the last RbMap watches (set before the binder binds, so the build's own RecedeBuilt is counted).</summary>
        public static FrontRooms.Map.FrontRoomsRunRoom Watch;
        public readonly FrontRoomsModulePreview preview;
        public readonly FrontRoomsMapWorld world;
        public readonly FrontRooms.Map.FrontRoomsRunRoom room;
        public readonly FrontRoomsRunRigBinder binder;
        public readonly string status;
        public readonly List<int> mapSeats = new List<int>();
        readonly Action<FrontRooms.Map.FrontRoomsRunRoom> onSeated;

        public RbMap(FrontRoomsModulePreview.RunCardView card, int seed)
        {
            Watch = null;
            var go = new GameObject("TEST RB preview " + card);
            preview = go.AddComponent<FrontRoomsModulePreview>();
            preview.view = FrontRoomsModulePreview.RoomView.RunRoom;
            preview.runCard = card;
            preview.seed = seed;
            preview.gameDressing = false;
            preview.runInOffice = false;
            preview.mixedSurroundings = false;
            preview.Rebuild();
            world = preview.World;
            status = preview.Status;
            if (world == null || world.RunRooms == null || world.Player == null) return;
            // The preview's own room: built, and the one whose entrance the preview stood the player in (its chunks streamed round it).
            var rooms = world.RunRooms.Rooms;
            var best = float.MaxValue;
            for (var i = 0; i < rooms.Count; i++)
            {
                if (!world.IsRunRoomBuilt(rooms[i])) continue;
                var d = (world.RunRoomPoint(rooms[i], new Vector3(0f, 0f, 1.2f)) - world.Player.position).sqrMagnitude;
                if (d < best) { best = d; room = rooms[i]; }
            }
            if (room == null) return;
            Watch = room;
            var mine = room;
            onSeated = r => { if (r == mine) mapSeats.Add(r.OpenCells); };
            world.RunRoomSeated += onSeated;
            binder = FrontRoomsRunRigBinder.Bind(world, world.Player);
        }

        public FrontRoomsRunRig Rig { get { return binder != null ? binder.RigOf(room) : null; } }
        public FrontRoomsRunRecede Recede { get { return binder != null ? binder.RecedeOf(room) : null; } }

        /// <summary>The map's player (the binder's viewer, the end's gaze) on the axis at rig z: yaw 0 faces the goal, 180 the entrance.</summary>
        public void Stand(float z, float yaw)
        {
            world.Player.SetPositionAndRotation(world.RunRoomPoint(room, new Vector3(0f, 0f, z)), world.RunRoomRotation(room) * Quaternion.Euler(0f, yaw, 0f));
        }

        /// <summary>The binder's ticks (LateUpdate's) while the player walks from <paramref name="z0"/> at <paramref name="speed"/> facing
        /// the goal, until <paramref name="until"/> holds or <paramref name="seconds"/> pass; returns the time taken.</summary>
        public float Walk(float z0, float speed, float seconds, Func<bool> until)
        {
            var t = 0f;
            while (t < seconds - 1e-4f && (until == null || !until()))
            {
                Stand(z0 + speed * t, 0f);
                binder.TickForTools(Dt);
                t += Dt;
            }
            return t;
        }

        /// <summary>The room's entrance chunk dropped as streaming drops it (the binder lets the room go).</summary>
        public void Drop()
        {
            world.DropChunkForTools(room.ChunkA);
            Require(binder.RigOf(room) == null, "the binder kept the rig of a dropped room");
        }

        /// <summary>The chunk streamed back round the room (a revisit, no shift): the binder builds the room again.</summary>
        public void Rebuild()
        {
            world.StreamForTools(world.RunRoomPoint(room, new Vector3(0f, 0f, room.LengthMetres * .5f)), 0f);
            Require(binder.RigOf(room) != null, "the room did not come back");
        }

        public void Dispose()
        {
            if (world != null && onSeated != null) world.RunRoomSeated -= onSeated;
            if (world != null) world.Release();
            if (preview != null) UnityEngine.Object.DestroyImmediate(preview.gameObject);
        }
    }

    /// <summary>The goal end's ALARM ZONE detector and card under a room's root (those past rig z <paramref name="split"/>).</summary>
    static void RbGoalTell(FrontRoomsRunRig rig, float split, out Transform detector, out Transform card)
    {
        detector = null;
        card = null;
        var root = rig.transform.parent;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            var isDetector = t.name.StartsWith(FrontRoomsAlarmTell.DetectorName, StringComparison.Ordinal);
            var isCard = t.name.StartsWith(FrontRoomsAlarmTell.CardName, StringComparison.Ordinal);
            if (!isDetector && !isCard) continue;
            if (root.InverseTransformPoint(t.position).z < split) continue;
            if (isDetector) detector = t; else card = t;
        }
    }

    static float RbZ(FrontRoomsRunRig rig, Transform t) { return rig.transform.parent.InverseTransformPoint(t.position).z; }

    static string RbJoin(List<int> list)
    {
        var s = new StringBuilder();
        for (var i = 0; i < list.Count; i++) s.Append(i > 0 ? "," : "").Append(list[i]);
        return s.ToString();
    }

    static string RbRecord(ulong bits)
    {
        var r = FrontRoomsRunRigBinder.UnpackRecord(bits);
        return "v" + r.version + " flags " + r.flags + " steps " + r.steps;
    }

    /// <summary>
    /// R24 (RB, the level-design line's card 4 in the binder; 71 s.4-s.5; contract W/apply/run_rethink_contract/README.md "Final"):
    /// the record and the seat lines; runRooms off binds nothing; today's plain room builds no end, holds no pulse and keeps no
    /// record; a 9 -> 12 room on the map is built receding, tripped by the map, seated on the map seat by seat (RunSeated on each
    /// SeatReached, its RunRoomSeated, open cells, hidden cells and goal point following), B's pulses held until the final seat; the
    /// record in the map's room; drops and rebuilds before, during and after the recede; a 9 -> 14 glide seats the map once.
    /// </summary>
    static string R24RecedeBinder()
    {
        var sb = new StringBuilder();
        // 1. The record (8 bytes in the map's room) and the seat lines in cells.
        var full = new RunRecedeRecord
        {
            version = RunRecedeRecord.Version1, flags = (byte)(RunRecedeRecord.Tripped | RunRecedeRecord.GateOpen | RunRecedeRecord.Seated),
            steps = 4, glideFrom = 255, gateTime = Mathf.FloatToHalf(1.25f), glideTime = Mathf.FloatToHalf(0f),
        };
        var back = FrontRoomsRunRigBinder.UnpackRecord(FrontRoomsRunRigBinder.PackRecord(full));
        Require(back.version == full.version && back.flags == full.flags && back.steps == full.steps && back.glideFrom == full.glideFrom
            && back.gateTime == full.gateTime && back.glideTime == full.glideTime && back.Started && back.Done, "the record does not round-trip");
        var none = FrontRoomsRunRigBinder.UnpackRecord(0UL);
        Require(FrontRoomsRunRigBinder.PackRecord(full) != 0UL && none.version == 0 && !none.Started, "0 reads as a record");
        var lines = new[] { 27f, 33f, 36f, 39f, 42f };
        var cellsAt = new[] { 9, 11, 12, 13, 14 };
        for (var i = 0; i < lines.Length; i++)
            Require(FrontRoomsRunRigBinder.LineCells(lines[i]) == cellsAt[i] && FrontRoomsRunRigBinder.LineCells(lines[i] + .004f) == cellsAt[i], "LineCells(" + F(lines[i]) + ")");
        sb.Append("record 8 B round trip (0 = none); seat lines 27..42 m = cells 9, 11..14");

        var lookBefore = FrontRoomsRunRig.DefaultRedLook;
        // RecedeBuilt / RecedeDropped for the watched room (the map may hold more rooms of the card; each is bound too).
        var built = 0;
        var dropped = 0;
        var builtAny = 0;
        RbMap.Watch = null;
        Action<FrontRooms.Map.FrontRoomsRunRoom, FrontRoomsRunRecede> onBuilt = (r, x) => { builtAny++; if (r != null && r == RbMap.Watch) built++; };
        Action<FrontRooms.Map.FrontRoomsRunRoom, FrontRoomsRunRecede> onDropped = (r, x) => { if (r != null && r == RbMap.Watch) dropped++; };
        FrontRoomsRunRigBinder.RecedeBuilt += onBuilt;
        FrontRoomsRunRigBinder.RecedeDropped += onDropped;
        SetReduced(false);
        try
        {
            // 2. runRooms off (the shipped profile, the map made as the preview makes one): no planner, and the game's hook binds nothing.
            var offGo = new GameObject("TEST RB map, runRooms off");
            offGo.SetActive(false);
            var prof = UnityEngine.Object.Instantiate(FrontRoomsLevelProfile.Default);
            prof.hideFlags = HideFlags.DontSave;
            prof.buildRadius = 1;
            var off = offGo.AddComponent<FrontRoomsMapWorld>();
            try
            {
                off.Profile = prof;
                offGo.SetActive(true);
                off.BuildForCapture();
                var bindIfOn = typeof(FrontRoomsRunRigBinder).GetMethod("BindIfOn", BindingFlags.NonPublic | BindingFlags.Static);
                Require(bindIfOn != null, "FrontRoomsRunRigBinder.BindIfOn not found");
                bindIfOn.Invoke(null, new object[] { off });
                Require(!prof.runRooms && !off.RunRoomsOn && off.RunRooms == null && off.GetComponent<FrontRoomsRunRigBinder>() == null && builtAny == 0,
                    "runRooms off: planner " + (off.RunRooms != null) + ", binder " + (off.GetComponent<FrontRoomsRunRigBinder>() != null));
            }
            finally
            {
                off.Release();
                UnityEngine.Object.DestroyImmediate(offGo);
                UnityEngine.Object.DestroyImmediate(prof);
            }
            sb.Append("; runRooms off: no planner, nothing bound");

            // 3. Today's plain room (the preview's Tools card: a plain 9): no end, no hold, no record, the goal tell as landed.
            FrontRoomsRunRig.DefaultRedLook = RunRedLook.Today;
            using (var m = new RbMap(FrontRoomsModulePreview.RunCardView.Tools, 1))
            {
                Require(m.room != null, "plain: no room (" + m.status + ")");
                var rig = m.Rig;
                Require(!m.room.Receding && m.Recede == null && rig != null && rig.Layout.recedeLine < 0f && !rig.PulseHeld && rig.GoalMount == null && builtAny == 0,
                    "plain: an end " + (m.Recede != null) + ", held " + rig.PulseHeld);
                Transform det, card;
                RbGoalTell(rig, rig.Layout.length * .5f, out det, out card);
                Require(det != null && card != null && det.gameObject.activeInHierarchy && RbZ(rig, det) < rig.Layout.length && RbZ(rig, card) > rig.Layout.length,
                    "plain: the goal tell moved");
                var plainTellZ = RbZ(rig, det);
                m.Stand(1.6f, 0f);
                m.world.TripRunRoom(m.room);
                m.Walk(1.6f, 3.2f, 2f, null);
                Require(rig.State != RunRigState.Armed && m.room.RecedeRecord == 0UL && m.mapSeats.Count == 0, "plain tripped: " + rig.State + ", record " + m.room.RecedeRecord);
                m.Drop();
                m.Rebuild();
                Require(m.Recede == null && m.room.RecedeRecord == 0UL && builtAny == 0 && dropped == 0 && m.Rig.State == RunRigState.Tripped, "plain rebuilt: " + m.Rig.State);
                sb.Append("; plain 9 (").Append(m.room.VariantText).Append("): no end, no hold, no record, its goal tell at z ").Append(F(plainTellZ)).Append(" as landed, rebuilt tripped");
            }

            // 4. A 9 -> 12 room in B (the pulse): built receding, tripped by the map, seated on the map seat by seat, pulses after the final seat.
            FrontRoomsRunRig.DefaultRedLook = RunRedLook.AlarmPulse;
            using (var m = new RbMap(FrontRoomsModulePreview.RunCardView.Recede9To12, 1))
            {
                var room = m.room;
                Require(room != null && room.Receding, "9 -> 12: no receding room (" + m.status + ")");
                var rig = m.Rig;
                var rec = m.Recede;
                Require(rec != null && built == 1, "9 -> 12: no end built (" + built + " RecedeBuilt)");
                Require(room.LengthCells == 12 && room.SeatCells.Length == 3 && room.SeatCells[0] == 9 && room.SeatCells[1] == 11 && room.SeatCells[2] == 12
                    && room.SeatIndex == 0 && room.OpenCells == 9 && room.RecedeRecord == 0UL, "the map's record: " + room.VariantText);
                Require(Near(rig.Layout.recedeLine, 27f) && Near(rig.Layout.length, 36f) && rig.PulseHeld && rig.GoalBeyondHidden && Near(rig.GoalLine, 27f)
                    && rig.HiddenWraps == 3 && rig.GoalMount != null && rec.Rig == rig && Near(rec.Line, 27f) && rec.Phase == RunRecedePhase.Armed
                    && rec.transform.IsChildOf(rig.transform.parent), "the receding build: line " + F(rig.Layout.recedeLine) + ", length " + F(rig.Layout.length)
                    + ", held " + rig.PulseHeld + ", hidden " + rig.HiddenWraps);
                Transform det, card;
                RbGoalTell(rig, rig.Layout.recedeLine + .5f, out det, out card);
                Require(det != null && card != null, "no goal tell past the end's first line");
                // The detector stands at the sight rule's depth from the deepest line, pushed clear of the strips (about 31 m for 9 -> 12):
                // in the hidden length, off with its cell until the end uncovers it; the card outside the L_max frame.
                var dz = RbZ(rig, det);
                var cz = RbZ(rig, card);
                Require(dz > rig.Layout.recedeLine + .5f && dz < rig.Layout.length && !det.gameObject.activeInHierarchy && cz > rig.Layout.length && card.gameObject.activeInHierarchy,
                    "the goal tell: detector z " + F(dz) + (det.gameObject.activeInHierarchy ? " on" : " off") + ", card z " + F(cz));
                // The map arms this room at FD1 (Order T); the map's trip trips the end with the alarm.
                float mx, mz;
                room.ToMap(0f, 8.5f, out mx, out mz);
                var short9 = room.InArmedZone(mx, mz);
                room.ToMap(0f, 9.5f, out mx, out mz);
                Require(!short9 && room.InArmedZone(mx, mz), "the map does not arm the receding room at FD1");
                m.Stand(9.5f, 0f);
                m.world.TripRunRoom(room);
                Require(rig.State == RunRigState.Tripping && rec.Phase == RunRecedePhase.Waiting && rig.PulseHeld, "the map's trip: rig " + rig.State + ", end " + rec.Phase);
                var seatLog = new List<string>();
                rec.SeatReached += (i, z, fin) => seatLog.Add(FrontRoomsRunRigBinder.LineCells(z) + "/" + room.OpenCells + (fin ? "F" : ""));
                var finalAt = m.Walk(9.5f, 3.2f, 6f, () => room.Seated);
                var pulsesAtFinal = rig.PulsesStarted;
                m.Walk(9.5f + 3.2f * finalAt, 3.2f, 6f - finalAt, null);
                Require(seatLog.Count == 2 && seatLog[0] == "11/11" && seatLog[1] == "12/12F", "SeatReached -> the map: " + string.Join(",", seatLog.ToArray()));
                Require(m.mapSeats.Count == 2 && m.mapSeats[0] == 11 && m.mapSeats[1] == 12 && room.Seated && room.SeatIndex == 2 && room.OpenCells == 12,
                    "the map's seats: " + RbJoin(m.mapSeats) + ", index " + room.SeatIndex);
                Require(!m.world.RunRooms.IsHiddenCell(room.Cells[11]) && !m.world.RunRooms.HeldShut(room.Cells[10], room.Cells[11]), "the map still hides the last cell");
                Vector3 gp, gn;
                m.world.RunEntrance(room, true, out gp, out gn);
                var gz = rig.transform.parent.InverseTransformPoint(gp).z;
                Require(Near(gz, 36f, .01f), "the map's goal point at z " + F(gz));
                Require(pulsesAtFinal == 0 && rig.PulsesStarted >= 1 && !rig.PulseHeld, "B's pulses: " + pulsesAtFinal + " at the final seat (" + F(finalAt) + " s), " + rig.PulsesStarted + " by 6 s");
                Require(det.gameObject.activeInHierarchy && !rig.GoalBeyondHidden && Near(rig.GoalLine, 36f) && rig.HiddenWraps == 0, "after the final seat: detector "
                    + det.gameObject.activeInHierarchy + ", hidden " + rig.HiddenWraps);
                var rcd = FrontRoomsRunRigBinder.UnpackRecord(room.RecedeRecord);
                Require(rcd.version == RunRecedeRecord.Version1 && rcd.Started && rcd.steps == 2, "the map's record at the final seat: " + RbRecord(room.RecedeRecord));
                sb.Append("; 9 -> 12 (").Append(room.VariantText).Append("): built receding (line 27, 36 m, 3 strips hidden, held), goal tell at z ").Append(F(dz)).Append(" / ").Append(F(cz))
                    .Append(" (detector off in its cell), armed at FD1, seats ").Append(string.Join(",", seatLog.ToArray())).Append(" = map ").Append(RbJoin(m.mapSeats))
                    .Append(", final at ").Append(F(finalAt)).Append(" s, pulses 0 then ").Append(rig.PulsesStarted).Append(" by 6 s");
                // Dropped after the recede: the record stays with the room; rebuilt seated, quiet; the map already seated (no RunRoomSeated).
                var droppedBefore = dropped;
                var builtBefore = built;
                m.Drop();
                rcd = FrontRoomsRunRigBinder.UnpackRecord(room.RecedeRecord);
                Require(dropped == droppedBefore + 1 && rcd.Started && rcd.Done && rcd.steps == 2, "dropped: " + RbRecord(room.RecedeRecord) + ", RecedeDropped " + (dropped - droppedBefore));
                m.Rebuild();
                var rig2 = m.Rig;
                var rec2 = m.Recede;
                Require(built == builtBefore + 1 && rec2 != null && rec2.Seated && Near(rec2.Line, 36f) && rig2.HiddenWraps == 0 && !rig2.GoalBeyondHidden && !rig2.PulseHeld
                    && rig2.State == RunRigState.Tripped && m.mapSeats.Count == 2 && room.Seated, "rebuilt after the recede: seated " + (rec2 != null && rec2.Seated) + ", rig " + rig2.State
                    + ", map seats " + RbJoin(m.mapSeats));
                var cues = 0;
                rec2.Cue += (c, i, p) => cues++;
                m.Walk(20f, 0f, 2f, null);
                Require(cues == 0 && rig2.PulsesStarted == 0 && m.mapSeats.Count == 2 && Near(rec2.Line, 36f), "the rebuilt room moved or spoke: " + cues + " cues, " + rig2.PulsesStarted + " pulses");
                sb.Append("; dropped after it (").Append(RbRecord(room.RecedeRecord)).Append("), rebuilt seated at 36, no cue, no pulse, the map not seated again");
            }

            // 5. Drops before and during the recede (Today's look): waiting comes back waiting; mid-recede comes back seated, the map seated once by the binder.
            FrontRoomsRunRig.DefaultRedLook = RunRedLook.Today;
            using (var m = new RbMap(FrontRoomsModulePreview.RunCardView.Recede9To12, 1))
            {
                var room = m.room;
                Require(room != null && room.Receding, "9 -> 12 (2): no receding room (" + m.status + ")");
                var rec = m.Recede;
                m.Stand(9.5f, 180f);
                m.world.TripRunRoom(room);
                for (var k = 0; k < 60; k++) m.binder.TickForTools(Dt);
                Require(rec.Phase == RunRecedePhase.Waiting && Near(rec.Line, 27f) && room.SeatIndex == 0, "facing the entrance: " + rec.Phase + " at " + F(rec.Line));
                var builtBefore = built;
                m.Drop();
                var rcd = FrontRoomsRunRigBinder.UnpackRecord(room.RecedeRecord);
                Require(rcd.version == RunRecedeRecord.Version1 && (rcd.flags & RunRecedeRecord.Tripped) != 0 && !rcd.Started, "dropped waiting: " + RbRecord(room.RecedeRecord));
                m.Rebuild();
                rec = m.Recede;
                var rig = m.Rig;
                Require(built == builtBefore + 1 && rec.Phase == RunRecedePhase.Waiting && Near(rec.Line, 27f) && rig.PulseHeld && rig.State == RunRigState.Tripped
                    && rig.HiddenWraps == 3 && room.SeatIndex == 0 && m.mapSeats.Count == 0, "rebuilt waiting: " + rec.Phase + ", held " + rig.PulseHeld + ", rig " + rig.State);
                var firstAt = m.Walk(9.5f, 3.2f, 3f, () => room.SeatIndex >= 1);
                Require(room.SeatIndex == 1 && room.OpenCells == 11 && m.mapSeats.Count == 1 && m.mapSeats[0] == 11 && !rec.Seated, "the first seat after the rebuild: index "
                    + room.SeatIndex + ", map seats " + RbJoin(m.mapSeats));
                m.Drop();
                rcd = FrontRoomsRunRigBinder.UnpackRecord(room.RecedeRecord);
                Require(rcd.Started && !rcd.Done && rcd.steps == 1, "dropped mid-recede: " + RbRecord(room.RecedeRecord));
                m.Rebuild();
                rec = m.Recede;
                rig = m.Rig;
                Require(rec.Seated && Near(rec.Line, 36f) && rig.HiddenWraps == 0 && !rig.PulseHeld && room.Seated && room.SeatIndex == 2 && m.mapSeats.Count == 2 && m.mapSeats[1] == 12,
                    "rebuilt mid-recede: seated " + rec.Seated + ", map seats " + RbJoin(m.mapSeats) + ", index " + room.SeatIndex);
                sb.Append("; dropped waiting, rebuilt waiting (re-tripped, held), first seat ").Append(F(firstAt)).Append(" s later on the map at 11; dropped mid-recede (")
                    .Append(RbRecord(room.RecedeRecord)).Append("), rebuilt seated at 36 and the map seated once on 12");
            }

            // 6. A 9 -> 14 room: a walker 5 m from the end at the gate glides it, one seat on the map, on its last line. A 14-cell straight
            //    has one place per chunk pair, so the preview's eight seeds may not hold one: seeds 1, 9, 17 in turn.
            var m14 = new RbMap(FrontRoomsModulePreview.RunCardView.Recede9To14, 1);
            for (var s = 9; s <= 17 && m14.room == null; s += 8)
            {
                m14.Dispose();
                m14 = new RbMap(FrontRoomsModulePreview.RunCardView.Recede9To14, s);
            }
            using (var m = m14)
            {
                var room = m.room;
                Require(room != null && room.Receding, "9 -> 14: no receding room in seeds 1-24 (" + m.status + ")");
                var rig = m.Rig;
                var rec = m.Recede;
                Require(room.LengthCells == 14 && room.SeatCells.Length == 5 && Near(rig.Layout.length, 42f) && rig.HiddenWraps == 5, "9 -> 14 built: " + room.VariantText + ", hidden " + rig.HiddenWraps);
                var seatLog = new List<string>();
                rec.SeatReached += (i, z, fin) => seatLog.Add(FrontRoomsRunRigBinder.LineCells(z) + "/" + room.OpenCells + (fin ? "F" : ""));
                m.Stand(22f, 0f);
                m.world.TripRunRoom(room);
                var seatedAt = m.Walk(22f, 0f, 6f, () => rec.Seated);
                Require(rec.Gliding && rec.Seated && Near(rec.Line, 42f), "the glide: gliding " + rec.Gliding + ", seated " + rec.Seated + " at " + F(rec.Line));
                Require(seatLog.Count == 1 && seatLog[0] == "14/14F" && m.mapSeats.Count == 1 && m.mapSeats[0] == 14 && room.SeatIndex == 4 && room.Seated,
                    "the glide on the map: " + string.Join(",", seatLog.ToArray()) + ", map " + RbJoin(m.mapSeats) + ", index " + room.SeatIndex);
                sb.Append("; 9 -> 14 (").Append(room.VariantText).Append("): glided from 5 m, seated at ").Append(F(seatedAt)).Append(" s, one seat on the map (index 0 -> 4)");
            }
        }
        finally
        {
            FrontRoomsRunRig.DefaultRedLook = lookBefore;
            FrontRoomsRunRigBinder.RecedeBuilt -= onBuilt;
            FrontRoomsRunRigBinder.RecedeDropped -= onDropped;
        }
        return sb.ToString();
    }
}
