# EXAMINE text: the transcript panel (G9) and the touch words (G10)

平面视觉, 2026-10-22. This answers G9 and G10 of `research/inspect/contracts/graphic_inspect.md`. It places them as the touch contract (`contracts/touch_inspect.md` T2, T6, T7) asks.

**Figma:**
- `3619:14` SCR / Desktop / D18 · EXAMINE · transcript (G9);
- `3619:42` EX01 · Touch EXAMINE, with phone mock `3619:47`.

Both are in the desktop UI section `2911:6716`.

**Words are signed by 叙事** (2026-10-22, `research/narrative/40_floors_exit_tutorial_strings.md` §4): `TEXT`, `Text transcripts`, the region heads. The transcript describer is now `NO REENTRY (the door into this floor)`.

## G10. Touch words, not new glyphs

The USE disc already speaks in Bayon verbs (OPEN, SHUT, BROKEN), and FIX_CARD §B decided `READ` as the phone verb for plans, screens and signs. So G10 needs one new control, the TEXT chip, and no icons.

| Piece | When | Look and place |
|---|---|---|
| `READ` | a readable is aimed (C14: prompt → `UseKind.Read`) | the USE verb, Bayon 17, as OPEN / SHUT |
| `BACK` | in a close-up (`MenuState.Inspect`) | the USE verb, the disc moved to the lower right: centre (768, 337) pt on the 874 × 402 phone, hit Ø88 (y 293–381) |
| TEXT chip | in a close-up whose target has a transcript (T6) | the pause slot (pause is hidden in a close-up): centre (778, 28), hit 44. Off: the paper word with the HUD keyline, no fill. On: a paper chip with the word in ink |

The chip sits right above the panel it opens, so control and content are adjacent.

## G9. The transcript panel

**It is a reading surface, like settings, not HUD.** So it takes the settings panel's surface: #0E0E0D at 97 %, no rule. HUD elements stay card-free (keylines).

| | Desktop (1080) | Phone (874 × 402 pt) |
|---|---|---|
| Space | the right 36 %, inside the 72 px outer margin: x 1253–1848, top 72, above the hint band | the right 36 % of the safe area: x 542–812, y 64–280 |
| Padding | 24 px | 16 pt |
| Region heads (PLAN, LEGEND, IN CASE OF ALARM, TITLE BLOCK) | IBM Plex Mono 13/16 caps, paper | IBM Plex Mono 11/14 caps, paper |
| Lines | Source Serif 4 20/23, paper, one printed line per line | Source Serif 4 17/20 |
| Gaps | 16 between regions, 4 inside | 12 between regions, 4 inside |
| Contrast | ≥ 12 : 1 (bar 4.5) | same |

These are the existing ramps: Meta and Caption on desktop, `Touch/Phone/Meta` and `Touch/Phone/Caption` on the phone.

- **Glow lines** (`\u0001`, shown only below the ink gate) are set in the phosphor ink #D6DEBD with a 2 pt rule at the left in the same colour, so colour is never the only cue.
- **Overflow on the phone.** The Phase 0 transcript runs to about 620 pt in a 216 pt window. A drag that starts inside the panel rect scrolls it; a drag anywhere else is Look (T1). No scroll bar: a 12 pt fade at the cut edge says there is more.
- **Never:** a 2D stand-in for the object, or a notes screen (`10_design.md` §5.2, P9).

## Who builds what

- Desktop panel and the setting: 关卡设计 (map contract C11, C24).
- Touch words, chip and the panel's placement: 交互设计 (T2, T6, T7).
- The transcript text: from `examine.json` (`Tools/print/ink/art_from_graphic/placard/sheetA*/`), signed by 叙事.
