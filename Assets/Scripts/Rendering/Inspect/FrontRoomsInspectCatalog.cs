using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EXAMINE strings and fallback blocks (research/inspect/10_design.md §3.8, §5, §7.1 file 6).
/// - The words the player sees are 平面视觉's. Everything here is the design's PROPOSED wording,
///   kept in one place so 平面视觉 can replace it without touching logic (§7.5 items 1-5).
/// - Fallback blocks are for kits that will not be re-exported soon (the test bed, §1.2). A sidecar
///   block always wins. Every fallback carries ship: false, so it registers only with DevTargets.
/// </summary>
public static class FrontRoomsInspectCatalog
{
    // ---- prompt and hints (§5.1; PROPOSED, 平面视觉 to confirm)
    // Prompts follow the HUD's verb + object rule ("E  ·  OPEN DOOR", "E  ·  TAKE KEY"; verifier 2026-10-08). The bare
    // verbs stay as the fallback for a kit with no object word yet.
    public const string PromptRead = "E  ·  READ";
    public const string PromptLook = "E  ·  LOOK";
    public const string PromptReadPlan = "E  ·  READ PLAN";
    public const string PromptReadSign = "E  ·  READ SIGN";
    public const string PromptExamineKey = "E  ·  EXAMINE KEY";
    /// <summary>Office terminals: a screen you read (the close-up pauses, A43). 平面视觉's approved string (FIX_CARD_VL217 §B, DECISIONS #29).</summary>
    public const string PromptReadScreen = "E  ·  READ SCREEN";
    /// <summary>The playing station TV on every station floor (R3; Red 10-30 "改成 WATCH TV"): watching runs live under A43, so it takes a live verb.</summary>
    public const string PromptWatchTv = "E  ·  WATCH TV";
    /// <summary>R3: the registry label of the playing station TV's READ target (FrontRoomsScreenVideo builds it; no kit sidecar).</summary>
    public const string StationTvKit = "Screen_StationTV";
    /// <summary>
    /// Office terminals (computer_ui 41, Q3 / A38, 2026-10-30): the registry label of a terminal's READ target
    /// (FrontRoomsTerminalScreen builds it; no kit sidecar). Prompt: PromptReadScreen; the station TV has its own, PromptWatchTv.
    /// </summary>
    public const string TerminalKit = "Screen_OfficeTerminal";
    /// <summary>The transcript id a terminal's close-up reads (its current panel's lines; one terminal is read at a time).</summary>
    public const string TerminalTranscript = "terminal_panel";
    static readonly Dictionary<string, string> PromptByKit = new Dictionary<string, string>
    {
        { "Kit_EvacPlacard", PromptReadPlan },
        { "Kit_DoorSign", PromptReadSign },
        { StationTvKit, PromptWatchTv },
        { TerminalKit, PromptReadScreen },
    };
    /// <summary>Door vision: the PEEK on a door's vision lite, signed by 叙事设计 (LOOK already names the camera control).
    /// A pad reads "A  ·  PEEK" (the game's DisplayPrompt); touch: the USE disc reads PEEK.</summary>
    public const string PromptPeek = "E  ·  PEEK";
    public const string PromptPeekTouch = "PEEK";
    public const string HintBack = "E  ·  BACK";
    public const string HintCloser = "SCROLL  ·  CLOSER";
    public const string HintCloserTouch = "TAP  ·  CLOSER";
    public const string HintTurn = "MOUSE  ·  TURN";
    public const string HintTurnTouch = "DRAG  ·  TURN";
    /// <summary>Each hint shows at most this many times per session where it applies (§5.1).</summary>
    public const int HintShowings = 2;
    public const float HintFadeIn = .3f, HintHold = 3f, HintFadeOut = .3f, HintFadeCalm = .6f;

    public static string Prompt(string verb) => verb == "look" ? PromptLook : PromptRead;

    /// <summary>The prompt for a kit: its verb + object line, else the bare verb. Allocation-free (constants only).</summary>
    public static string PromptFor(string kit, string verb)
    {
        if (kit != null)
        {
            if (PromptByKit.TryGetValue(kit, out var p)) return p;
            // Direction B (Red 10-14): the key on its tag. Every zone tag (rect, round, long; red, blue, white) is the key.
            if (kit.StartsWith("Kit_KeyTag_", System.StringComparison.Ordinal) || kit.StartsWith("Kit_Key_Zone", System.StringComparison.Ordinal)) return PromptExamineKey;
        }
        return Prompt(verb);
    }

    /// <summary>
    /// Transcript text by id (§5.2): lines in reading order (G9, research/desktop_ui/EXAMINE_TEXT_G9_G10.md).
    /// - '\u0001' starts a glow line (shown only while the ink is legible).
    /// - '\u0002' starts a region head: the region's name (PLAN, LEGEND, IN CASE OF ALARM, TITLE BLOCK; 叙事 40 §4), one per
    ///   examine.json region, in its order. The lines up to the next head belong to that region. A head never glows.
    /// - Lines before the first head (the sheet's title band, above every region) belong to no region.
    /// </summary>
    static readonly Dictionary<string, string[]> Transcripts = new Dictionary<string, string[]>
    {
        // The tutorial placard, sheet A, Phase 0 (no station, no padlock): 平面视觉's tutorial_placard.swift transcript
        // (Tools/print/ink/art_from_graphic/placard/sheetA_phase0/examine.json md5 7f0a3169; strings signed by 叙事,
        // research/narrative/40_floors_exit_tutorial_strings.md §3, region heads §4). It ships with the same packed
        // sheet (Prop_EvacPlan_A/E); regenerate both together (Tools/lookdev/inspect_transcript.py), never type the lines here.
        {
            "placard_q16", new[]
            {
                "EVACUATION PLAN · LEVEL 0",
                "\u0002PLAN",
                "NO REENTRY (the door into this floor)",
                "YOU ARE HERE",
                "\u0001EXIT (over the stair door)",
                "STAIR A",
                "\u0002LEGEND",
                "LEGEND: YOU ARE HERE · STAIR (EXIT), ONE FLOOR DOWN · WALL ARROWS, POINT THE WAY OUT",
                "TYPICAL FLOOR · NOT TO SCALE",
                "THIS FLOOR MAY DIFFER FROM PLAN",
                "\u0002IN CASE OF ALARM",
                "IN CASE OF ALARM",
                "ON ALARM, A FLOOR WARDEN CHECKS EVERY ROOM.",
                "1 WALK, DO NOT RUN.",
                "2 CLOSE DOORS BEHIND YOU.",
                "3 IF THE LIGHTS DIM, LEAVE THE AREA.",
                "4 EXIT BY STAIR, ONE FLOOR DOWN.",
                "\u0002TITLE BLOCK",
                "LEVEL 0 · SHEET A-2 OF 4 · PRINTED 11/89",
            }
        },
        // station-look-v1 (A26): the Floor 1 first room's sheet with the stair lock and the station live (平面视觉's sheetA_room,
        // examine.json md5 513f180f, Tools/lookdev/inspect_transcript.py --print; strings signed by 叙事 40 §3, §9). Packed as
        // Prop_EvacPlan_A_Room / _E_Room; the station placard (FrontRoomsPlacardMount station branch) reads it.
        {
            "placard_station_room", new[]
            {
                "EVACUATION PLAN · LEVEL 0",
                "\u0002PLAN",
                "YOU ARE HERE",
                "SECURITY DESK",
                "NO REENTRY (the door into this floor)",
                "\u0001EXIT (over the stair door, padlocked)",
                "STAIR A",
                "\u0002LEGEND",
                "LEGEND: YOU ARE HERE · STAIR (EXIT), ONE FLOOR DOWN · SECURITY DESK (KEY) · WALL ARROWS, POINT THE WAY OUT",
                "BEYOND BREAK: NOT TO SCALE",
                "THIS FLOOR MAY DIFFER FROM PLAN",
                "\u0002IN CASE OF ALARM",
                "IN CASE OF ALARM",
                "ON ALARM, A FLOOR WARDEN CHECKS EVERY ROOM.",
                "1 WALK, DO NOT RUN.",
                "2 CLOSE DOORS BEHIND YOU.",
                "3 IF THE LIGHTS DIM, LEAVE THE AREA.",
                "4 STAIR DOORS ARE LOCKED. KEY AT SECURITY DESK.",
                "\u0002TITLE BLOCK",
                "LEVEL 0 · SHEET A-2 OF 4 · PRINTED 11/89",
            }
        },
        // station-look-v1 (A26): the same room before the stair locks (sheetA_room_phase0, examine.json md5 b39cd67f;
        // Prop_EvacPlan_A_RoomPhase0 / _E_RoomPhase0).
        {
            "placard_station_room_phase0", new[]
            {
                "EVACUATION PLAN · LEVEL 0",
                "\u0002PLAN",
                "YOU ARE HERE",
                "SECURITY DESK",
                "NO REENTRY (the door into this floor)",
                "\u0001EXIT (over the stair door)",
                "STAIR A",
                "\u0002LEGEND",
                "LEGEND: YOU ARE HERE · STAIR (EXIT), ONE FLOOR DOWN · WALL ARROWS, POINT THE WAY OUT",
                "BEYOND BREAK: NOT TO SCALE",
                "THIS FLOOR MAY DIFFER FROM PLAN",
                "\u0002IN CASE OF ALARM",
                "IN CASE OF ALARM",
                "ON ALARM, A FLOOR WARDEN CHECKS EVERY ROOM.",
                "1 WALK, DO NOT RUN.",
                "2 CLOSE DOORS BEHIND YOU.",
                "3 IF THE LIGHTS DIM, LEAVE THE AREA.",
                "4 EXIT BY STAIR, ONE FLOOR DOWN.",
                "\u0002TITLE BLOCK",
                "LEVEL 0 · SHEET A-2 OF 4 · PRINTED 11/89",
            }
        },
        // station-look-v1 (A26): the station's paper, 01_research.md §6's strings (叙事's words), for EXAMINE once 平面视觉's
        // Prop_StationPaper atlas prints them (30L S15 "EXAMINE-readable"; plan check C1-10). Not registered by any kit yet: the
        // paper is blank until the atlas lands, and a transcript must match the printed sheet. "{DAR_DATE}" is the night-shift
        // date the station paper computes from the live clock (01 §6 live-clock rule).
        {
            "station_dar", new[]
            {
                "{DAR_DATE}  NIGHT  DESK",
                "2300  ON DUTY  D.K.",
                "0000  TOUR 1 COMPLETE  ALL SECURE",
                "0100  TOUR 2 COMPLETE  ALL SECURE",
                "0112  BLDG SVCS CALLED  LAMPS OUT STOCK RM 3",
                "0200  TOUR 3",
            }
        },
        {
            "station_sign_out", new[]
            {
                "DATE  KEY NO.  NAME  OUT  IN  INIT.",
            }
        },
        {
            "station_register", new[]
            {
                "DATE  NAME  COMPANY  VISITING  TIME IN  TIME OUT",
            }
        },
        {
            "station_key_plate", new[]
            {
                "STAIR KEY",
                "SILENT ALARM: REMOVAL SIGNALS PANEL",
            }
        },
    };

    public static string[] Transcript(string id) => id != null && Transcripts.TryGetValue(id, out var lines) ? lines : null;

    /// <summary>
    /// A transcript written at run time (an Office terminal's current panel, <see cref="TerminalTranscript"/>); null or empty
    /// lines remove it. Plain lines (no glow or region marks).
    /// </summary>
    public static void SetTranscript(string id, string[] lines)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (lines == null || lines.Length == 0) Transcripts.Remove(id);
        else Transcripts[id] = lines;
    }

    // ---- station-look-v1 (A26): the station placard's sheet ids and regions (the room sheets' examine.json, sheet mm)
    /// <summary>The transcript id of the station placard's sheet: with the stair lock live (sheetA_room) or before it (sheetA_room_phase0).</summary>
    public static string StationPlacardTranscript(bool exitLocked) => exitLocked ? "placard_station_room" : "placard_station_room_phase0";

    /// <summary>The room sheets' regions (both sheets: the legend grows to 178 mm with the WALL ARROWS row).</summary>
    public static InspectDef.Region[] StationPlacardRegions() => new[]
    {
        new InspectDef.Region { id = "plan", rect = new[] { 20f, 46f, 268f, 158f }, capMm = 2.8f },
        new InspectDef.Region { id = "legend", rect = new[] { 296f, 46f, 118f, 178f }, capMm = 2.8f },
        new InspectDef.Region { id = "alarm", rect = new[] { 20f, 208f, 268f, 51f }, capMm = 3.6f },
        new InspectDef.Region { id = "titleblock", rect = new[] { 296f, 241f, 118f, 20f }, capMm = 3.6f },
    };

    // ---- fallback blocks (§1.2 test bed; §3.3 numbers)
    static readonly Dictionary<string, InspectDef> Fallbacks = new Dictionary<string, InspectDef>
    {
        // T1 wall clock, hung at 2.1 m: the look-up pose (40° up, d 0.59).
        { "Kit_WallClock", new InspectDef { kind = "flat", verb = "look", size = new[] { .32f, .32f }, dist = new[] { .59f, 0f }, maxAngle = 40f, ship = false, sound = "plastic" } },
        // T2 vending machine: panel at its window anchor (ESTIMATE 0.55 × 0.95), orbit ±20°.
        { "Kit_VendingMachine", new InspectDef { kind = "panel", verb = "look", face = "window", faceDir = "window_dir", size = new[] { .55f, .95f }, dist = new[] { .9f, .5f }, ship = false, sound = "metal" } },
        // T3 copier console: a face that looks up (the text-up rule).
        { "Kit_Copier", new InspectDef { kind = "panel", verb = "look", face = "console", faceDir = "console_dir", up = "console_up", size = new[] { .4f, .2f }, dist = new[] { .4f, .25f }, maxAngle = 65f, ship = false, sound = "plastic" } },
        // T4 desk phone: the object kind before the key exists.
        { "Kit_DeskPhone", new InspectDef { kind = "object", verb = "look", dist = new[] { .5f, .35f }, fovDelta = -10f, ship = false, sound = "plastic" } },
    };

    /// <summary>The fallback block for a kit (null when none). Returns a copy.</summary>
    public static InspectDef Fallback(string kit) => kit != null && Fallbacks.TryGetValue(kit, out var def) ? def.Clone() : null;
}
