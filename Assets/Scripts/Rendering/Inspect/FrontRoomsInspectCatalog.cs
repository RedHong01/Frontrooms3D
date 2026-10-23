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
    static readonly Dictionary<string, string> PromptByKit = new Dictionary<string, string>
    {
        { "Kit_EvacPlacard", PromptReadPlan },
        { "Kit_DoorSign", PromptReadSign },
    };
    public const string HintBack = "E  ·  BACK";
    public const string HintCloser = "SCROLL  ·  CLOSER";
    public const string HintCloserTouch = "PINCH  ·  CLOSER";
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

    /// <summary>Transcript text by id (§5.2): lines in reading order, '\u0001' marks a glow line (shown only while the ink is legible).</summary>
    static readonly Dictionary<string, string[]> Transcripts = new Dictionary<string, string[]>
    {
        // The tutorial placard, sheet A, Phase 0 (no station, no padlock): 平面视觉's tutorial_placard.swift transcript
        // (Tools/print/ink/art_from_graphic/placard/sheetA_phase0/examine.json md5 ba00f0ab; strings signed by 叙事,
        // research/narrative/40_floors_exit_tutorial_strings.md §3). It ships with the same packed sheet
        // (Prop_EvacPlan_A/E); regenerate both together, never type the lines here.
        {
            "placard_q16", new[]
            {
                "EVACUATION PLAN · LEVEL 0",
                "NO REENTRY (the door into this floor)",
                "YOU ARE HERE",
                "\u0001EXIT (over the stair door)",
                "STAIR A",
                "LEGEND: YOU ARE HERE · STAIR (EXIT), ONE FLOOR DOWN",
                "TYPICAL FLOOR · NOT TO SCALE",
                "THIS FLOOR MAY DIFFER FROM PLAN",
                "IN CASE OF ALARM",
                "ON ALARM, A FLOOR WARDEN CHECKS EVERY ROOM.",
                "1 WALK, DO NOT RUN.",
                "2 CLOSE DOORS BEHIND YOU.",
                "3 IF THE LIGHTS DIM, LEAVE THE AREA.",
                "4 EXIT BY STAIR, ONE FLOOR DOWN.",
                "LEVEL 0 · SHEET A-2 OF 4 · PRINTED 11/89",
            }
        },
    };

    public static string[] Transcript(string id) => id != null && Transcripts.TryGetValue(id, out var lines) ? lines : null;

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
