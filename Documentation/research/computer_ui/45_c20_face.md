# Office terminals · C20: the face (FR816 C20)

平面视觉, 2026-10-30. Red decided C20 = 1 on 10-30: we draw our own 8 × 16 bitmap face. The envelope is `41_c19_period_interaction.md` §6, with the 11-row cap from O-5.

The face is ONE file: `Assets/Resources/Terminal/TerminalFace.png`. Its layout is the one `FrontRoomsTerminalFace.cs` reads, and its import settings (`.meta`) are unchanged. It replaced the FR816 stand-in at 14:4x; no code changed.

- **Source:** `Tools/terminal_face/face_c20.py`. Every glyph is drawn there as text art. The script writes the atlas and checks the metrics.
- **Previews:** `~/FrontRoomsVisualWork/c20_face/` holds `preview.py` and `compare.py`. They simulate the 80 × 25 raster from the panel constants in `FrontRoomsTerminal.cs`.
- **Sheet:** `images/C20_face_compare.jpg` puts the stand-in and C20 side by side.

## 1. Metrics

Rows are counted in the 16-row cell, with 0 at the top. Columns are 0–7 of the 8-px glyph; the VGA hardware adds a blank 9th.

| Part | Rows | Note |
|---|---|---|
| Line spacing | 0–1 clear | Every glyph except box and block glyphs |
| Capitals, ascenders (b d f h k l), figures | 2–12 (11 rows) | O-5: the phone passes the 18.6′ cap rule (19.9′ at 95 % framing; desktop 32.3′, iPad 25.8′; 41 §3.6) |
| x-height | 5–12 (8 rows) | x / cap = 0.73 |
| t | 3–12 | A short ascender |
| Descenders (g j p q y) | 13–15 (3 rows) | Q's tail reaches row 13 |
| i / j dots | 2–3 | 2 × 2, one clear row above the x-height |
| ( ) [ ] { } / \ | 2–13 | One row below the baseline |
| Math axis | row 8 | - + = < > * and the ← → shafts |
| _ | row 14 | All 8 columns, so a run of underscores reads as a dashed rule, as on the hardware |

- **Columns:** text glyphs use columns 0–6, and column 7 stays clear. With the 9th column, letters stand 2 px apart.
  - Narrow glyphs centre their 2 px stem on columns 2–3: I T Y i j l 1 ! | : ; . , '.
  - `_` and ← → use all 8 columns. A 7-px arrow read as "+" at the close-up size.
  - Box and block glyphs run edge to edge. CP437 0xC0–0xDF repeat column 8 into the 9th, so ─ ┌ └ ├ █ join.

## 2. Drawing rules

- **Strokes:** verticals are 2 px. Horizontals are 1 px, exactly one scanline. Diagonals are 2-px runs that step one column per row, i.e. 45° on the pixel grid. The tube shows its pixels taller than wide, so diagonals look steeper there, as they did on a 1990 monitor.
- **Rounds:** O C G Q S U J, the figures and the bowls take a one-pixel 45° chamfer. D takes two steps, so it never reads as O or 0.
- **Joins:** a stem meets an arch or bowl through a 45° notch, as in `##.###.` over `###..##` (n m h r b d p q u a g y). Terminals are cut flat.
- **Distinctions:**
  - 0 has a 45° slash; O is plain.
  - 1 has a flag and a foot. I has serifs. l has a top serif and a tail. `|` is the broken bar, as on the IBM PC; │ is the box line.
  - 2 vs Z: 2 has a curved top. 5 vs S: 5 has a flat top. 8 vs B: 8 has chamfered corners on the left. rn vs m: m's middle stem is 1 px.
- **Arrows:** ↑ ↓ have 2 px stems. ← → have 1 px shafts. Every head is two 45° 2-px runs from the tip.
- **■ (the unread mark):** 5 × 4, centred on the x-height.
- **Box lines (─ │ ┌ ┐ └ ┘ ├ ┤):** the stand-in's geometry is kept, a 1 px bar on row 8 and a 2 px stem in columns 3–4. The LIST and HELP layouts were built on it.

## 3. Glyph set

109 slots, 108 of them drawn:
- the 95 printable ASCII characters, including the space;
- the 14 CP437 layout glyphs listed in 41 §6.1.

Everything that can reach the screen is covered:
- `interoffice.json` is plain ASCII.
- Key names use ASCII plus ↑ ↓ ← →. That includes the period keys, the Mac and PC rows, the D-pad, PlayStation and touch names (`FrontRoomsInspectTerminal.cs`).

So nothing on today's panels counts as a missing glyph (AT-C7).

**Block alphabets for MAIN** (41 §3.3) are redrawn to echo the face:
- 5 × 7 for the headline and 4 × 5 for the second line;
- each covers capitals, digits and `. , : / - ' ! ?`.

The sizes are the stand-in's, so MAIN's layout is unchanged: `MEMOS` is 58 columns and `2 UNREAD` is 78.

## 4. Colour roles (41 §3.4)

FIELD, BRIGHT, BAR, TITLE and SOFTKEY stay as built. The previews show them reading as intended, so there is no change.

## 5. Checks done, and what is still open

- **Done:** `face_c20.py`'s metric and coverage check reports 0 problems (108 / 108).
- **Done:** the atlas, parsed the way `FrontRoomsTerminalFace.FromAtlas` parses it, was laid out on MAIN, DOC, LIST and HELP. It was viewed at desktop close-up size (picture 756 px tall) and at phone size (1146 px). The box lines join through the 9th column.
- **Done, 2026-10-30:** in-game capture checked at native resolution (`W/terminal/land2/LAND_terminal_a2_c20_sheet.jpg`, raw frames `W/terminal/caps_a2/`), desktop and phone, MAIN and a memo: **PASS.** Lowercase, descenders, 0 vs O vs 8 and the box rules all hold on the real desk CRT.
- **Accepted:** on MAIN, `MEMOS` and `2 UNREAD` touch since the A2 spacer row. The page is one row short: title 1, spacer 1, headline 7, second line 5, blank 1, four items at one touch hit each 8, message 1, keys 1 = 25. Any other row that gives way is worse (text on text, or the block line on the selection bar), and the white and grey inks keep the two lines apart.
- **Accepted:** `2 UNREAD` runs 78 of the 80 columns. Nothing clips under the tube's corners.
- **For 叙事设计:** lowercase is real now, so the mixed-case pass of 41 §6.4 can go ahead:
  - the program name and the memo field labels in capitals;
  - panel titles in Title Case;
  - items, messages and memo bodies in sentence case.
