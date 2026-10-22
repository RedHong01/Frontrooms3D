# EXAMINE: requests to 平面视觉 (type and UI)

From the visual chat (游戏视觉), fix stage, 2026-10-14. Verifier issues V2-UI and V1-8. The hand-off stage (same day) added the two sections at the end: hint typography as built, and the transcript panel.
These are requests, not decisions. Nothing here touches the PROP KIT section (2324:852) or any 平面视觉 file.
Please file them in `GRAPHIC_VISUAL_TASKS.md` (the visual chat cannot write there from this stage).

Measured on overlay captures. The method is yours (`research/desktop_ui/README.md`): overlay canvases on a post-free camera, composited in linear light. Images: `images/f_03_hud_overlay_prompts.jpg`.

## Strings and placement (needed before v1 ships)

| # | Item | Now in the build (PROPOSED) | What we need from you |
|---|---|---|---|
| G1 | Prompt words, verb + object, as the HUD's `E · OPEN DOOR` / `E · TAKE KEY` | `E  ·  READ PLAN` (placard), `E  ·  EXAMINE KEY` (every zone tag: the key on its tag, direction B), `E  ·  READ SIGN` (door sign test bed). Bare `E · READ` / `E · LOOK` only as a fallback | Confirm or replace the words. They are strings only, in `Assets/Scripts/Rendering/Inspect/FrontRoomsInspectCatalog.cs` |
| G2 | Close-up hints, at most 2 showings each | `E · BACK` (every close-up); `MOUSE · TURN` / `DRAG · TURN` (object). `SCROLL · CLOSER` / `PINCH · CLOSER` show only once the zoom input exists (touch T3); v1 never shows them | Words per platform, and whether each one shows |
| G3 | Where the hint sits | The prompt slot moved to the bottom band (map contract C11, `InspectHintBottom` 83). The glyph bottom is 77 px from the bottom edge at 1080 (≥ the 72 px outer margin). Contrast 9.9:1 over the base close-up, 3.8:1 over the zoomed sheet | Your system puts timed control hints on the hint card (D02, D11), not in the Bayon prompt slot. Pick card or slot, and the position. If card: its rect, so the zoom can keep the legend 40 px clear of it |
| G4 | `·` (U+00B7) | Bayon has no `·`, so it falls back to the system font (VL217 #3). Same as every prompt in the game | One fix for all prompts |
| G5 | Prompt over the thing it names | At 1.5 m the prompt line (crosshair −44 px) draws over the bottom of the placard (verifier v_01, frame 2; here `01_placard_prompt_1p5m`: 8.7:1, readable, but on the frame) | Accept, or move the prompt for near targets (e.g. above the crosshair, or on the card) |
| G6 | Prompt contrast on lit surfaces (all prompts, not only EXAMINE) | `E · EXAMINE KEY` on lit wallpaper: 3.3:1 median, 2.2:1 at the brightest 10 %. `E · READ SIGN` over a light sign: 2.2:1 | ≥ 3:1 everywhere: a card, a shadow, or a darker weight. The locked-door prompt of HUD key B already uses the card (`#141414` at 90 %) |
| G7 | Pause card line | none yet | e.g. `E  READ  (PAUSES)`, in your words |

## Content

| # | Item | Status |
|---|---|---|
| G8 | **Placard regions** from `placard.swift`'s layout constants (rect in sheet mm from the top-left, smallest cap per region) | The build uses one region MEASURED on main's v3 sheet (`Prop_EvacPlan_A` md5 7d589d81): **legend 299.6, 49.7, 111.0 × 174.3 mm, cap 2.4 mm**. The zoom frames it. Please replace it with the exact constants (and add the plan and title-block regions if they should be zoom targets) |
| G9 | Transcript panel (§5.2) | Face, 2 sizes, contrast, desktop and phone placement; the placard transcript in reading order (strings from A.12, with narrative), glow lines flagged |
| G10 | Touch glyphs: READ, BACK, TEXT chip | In the HUD key B language (set `3113:6999`, section `2532:4038`) |
| G11 | Optional: raise the 4.2–4.4 mm lines | At desktop base (composition A, 0.368 m) they are 10.5–11.0 px at 1080p (YOU ARE HERE contrast 4.2 lit, 5.2 with the own lamp off). The 2.4–3.6 mm lines (6.0–9.0 px) need the zoom, which waits for touch T3 |
| G12 | `Prop_KeyTagNo` digits at 4096² | The tag face is blank without it. We build the texture; you own the type. The key close-up already sits at the 2.0× texel cap of today's 3,300 px/m |
| G13 | With narrative: lit legend stays faint | Measured lit: glow words 1.14 (spec A3: 1.15–1.16). Own lamp off: 1.61; dead area: 1.54 (both ≥ 1.30, AT-01j) |

## Hint typography: what the build does now (added by the hand-off stage, 2026-10-14)

Everything below is a placeholder that copies today's prompt line. Replace any of it.

| Item | Now | Where it lives |
|---|---|---|
| Face and size | The game's prompt text: Bayon 20 px at 1080 (desktop), 17 pt on handheld; the touch layer strips the `E  ·  ` key | `FrontRooms3DGame` prompt slot (map chat) |
| Position | Centred, moved to the bottom band in a close-up: the 26-unit prompt rect's centre 83 units above the canvas bottom, so the glyph bottom sits 77 px from the edge at 1080 (outer margin 72) | map contract C11, `InspectHintBottom` |
| Alpha | Its own: fades in 0.3 s, holds 3 s, fades out 0.3 s (0.6 s with Reduce flashing). Not multiplied by the HUD fade. No blinking | `FrontRoomsInspectCatalog.HintFadeIn / HintHold / HintFadeOut / HintFadeCalm` (visual) |
| How often | Each hint counts on its own, at most 2 showings per session (`HintShowings`) | visual |
| Separator | `·` U+00B7 with two spaces each side, as every prompt; falls back to the system font (G4) | strings in `FrontRoomsInspectCatalog.cs` |
| Contrast measured (overlay method) | `E · BACK` over the base close-up 9.9:1; over the zoomed sheet 3.8:1 | `images/f_03_hud_overlay_prompts.jpg` |

What we need: the hint's face, size, colour, any card or shadow, and its slot (G3). If it moves to the hint card, send the card rect: the zoom keeps the legend's 2.4 mm line 40 px clear of whatever sits in the band (82 px today).

## Transcript panel (G9, accessibility; off by default)

The visual side has the hooks; nothing is drawn yet. Please design the panel; the map chat builds it (C11 and C24) and the touch session places it on handheld (T7).

| Item | Fixed by the system | Yours to decide |
|---|---|---|
| When it shows | Setting `Text transcripts: On` (C24, default Off), or the TEXT chip on handheld for that close-up (T6) | the setting's words; the chip glyph (G10) |
| Space | Desktop: the right 36 % of the screen (`FrontRoomsInspect.FreeScreenRect` gives the left 64 % to the framing; composition A already leaves the right side to the wall). Phone: the right 36 % of the safe area, y 64–280 pt, above the moved BACK button | margins inside that space, the panel fill (or none) |
| Text | `FrontRoomsInspect.TranscriptText`: the printed text in reading order by region, one line per printed line. Swatches are named, not explained | face, 2 size steps, leading, colour, contrast bar (we suggest ≥ 4.5:1 against what is behind) |
| Glow lines | Listed only while legible: shown when the own lamp is below 0.15 (the ink gate), hidden when lit (lit glow words measure 1.14:1; own lamp off 1.61; dead area 1.54) | how a glow line looks in the panel (a mark, a colour, or nothing) |
| Content | Empty until narrative writes the placard text (A.12). `\u0001` marks a glow line | the placard transcript, with narrative |
| Never | a 2D replacement for the object; a journal or notes screen (`10_design.md` §5.2, P9) | — |
