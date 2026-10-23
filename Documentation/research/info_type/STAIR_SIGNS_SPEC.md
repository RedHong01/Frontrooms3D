# Stairwell signs (C22 stair, look A): spec for 游戏视觉

> Repo copy of `~/FrontRoomsVisualWork/info_type/stair_signs/SPEC.md` (2026-10-22 16:4x, 平面视觉 · 信息设计). Relative paths below are relative to that W folder, where the generator `stair_signs.swift` and the art live. Edit the W file, then re-copy.

v1.3, 2026-10-22, 平面视觉 · 信息设计 (v1.3 adds sign 7, AREA OF RESCUE ASSISTANCE, after Red made the stair a safe zone, 游戏视觉 chat 15:5x; details in `out/rescue/README.md`. v1.2 adds the runtime slots, §6; v1.1 follows 叙事's final §5, 4e69ee5b: no floor plate on the leaves, NOT AN EXIT on the arrival door). Generator `stair_signs.swift` (this folder). Strings: 叙事设计, `Frontrooms3D/Documentation/research/narrative/40_floors_exit_tutorial_strings.md` §5 and `research/stairwell/NOTE_1022_narrative_strings.md`. Placements: 游戏视觉's lookdev (`research/stairwell/data/FrontRoomsStairLookdev.cs.txt`, Figma 3484:7309 SW02/SW09).

Built after the Alpha with the C22 stair. Nothing here is in the game yet.

## 0. The set at a glance

| # | Item | Where | Size | Colourway | Content |
|---|---|---|---|---|---|
| 1 | Stencil floor token | block wall, hinge side of each stair door | 15 in cap (381 mm) | black enamel through a stencil | `0`, `B1`, `B2` … |
| 2 | Stair ID sign | block wall, latch side, centre 5 ft (1.524 m) | 12 × 14 in (305 × 356 mm) | white on black, 4 screws | `STAIR A` / `NO ROOF ACCESS` / `LEVEL` + token / `LEVELS 0 TO B1` |
| 3 | Exit door plate | stair face of the EXIT door, centre 5 ft | 11 × 5 in (279 × 127 mm) | black on white | `NO REENTRY` / `NEAREST REENTRY` / `ON LEVEL B1` |
| 4 | Far door plate | stair face of the far door, centre 5 ft | 11 × 5 in | white on black | `REENTRY` / `ON THIS FLOOR` |
| 5 | NOT AN EXIT plate | floor side of the far door (the arrival door on B1), centre 5 ft, in place of the dead EXIT sign | 12 × 3 in (305 × 76 mm) | black on white | `NOT AN EXIT` |
| 6 | Stair letter plate (optional) | maze side of the EXIT door, centre 5 ft. Never on the far door: B1's own EXIT is STAIR A | 6 × 6 in (152 mm) | white on black | `STAIR` / `A` |
| 7 | Area of rescue assistance (lit, every exit landing) | stair side, latch wall, centred over the stair ID sign with a 2 in gap: box bottom 69 in (1.753 m), top 76.5 in (1.943 m), under the 80 in line of ADAAG 4.30.3 | face 18.5 × 6.5 in (470 × 165 mm) in a 19.5 × 7.5 × 2.25 in white steel box | white legend and ISA on accessibility blue #004A8C, lit steady; never red, no arrows | `AREA OF` / `RESCUE` / `ASSISTANCE` + ISA (ADAAG 1991 §4.3.11.5) |

The floor number is on the wall sign, not on the leaves (叙事 §5 after Red's 13:00 answer in 游戏视觉's chat); the stair side still shows it, as LL5 C26-608.3 wants. No EXIT sign, arrow, photoluminescent strip or running man inside the stair, and no EXIT sign on the arrival door (叙事's list; Red, in 游戏视觉's chat 13:00). The gate plates in 叙事 §5 are not made: the stair has no gates since the 10-22 fix (`10_directions.md` §1.1).

## 1. Period rules used (read 2026-10-22)

- **NYC Local Law 5 of 1973** (nyc.gov `local_laws/ll_0573.pdf`, p. 9; also the placard's `SOURCES.md` S13):
  - C26-608.3: floor-number sign inside the stair on every floor, numerals bold, **at least 3 in**, contrasting, on the stair side of the door;
  - C26-608.4: stair letter, **at least 3 in**, bold, contrasting, on the occupancy side of the stair door;
  - C26-608.5: re-entry signs on the stair door, lettering **at least ½ in**, bold, contrasting, about **5 ft** above the floor; "may be either independent or combined with" the 608.3 / 608.4 signs. The law prints **REENTRY** without a hyphen ("NO REENTRY, NEAREST REENTRY ON THE ___ AND ___ FLOORS", "REENTRY ON THIS FLOOR"). The later codified Admin Code §27-394 spells RE-ENTRY; the strings keep the 1973 form.
- **UBC 1991 §3306(p)** (`research/stairwell/SOURCES.md` S1): inside the stair at each landing, about 5 ft above the landing, readable with the door open or shut: stair ID, roof access, floor level, upper and lower terminus. Its layout standard (UBC Std 33-2) is **unread**. The layout here follows the later IBC 1023.9 form (sign at least 18 × 12 in, floor number at least 5 in in the centre, other text at least 1 in; read on the Fairfax County VA and Lenexa KS fire-department pages) [ESTIMATE for 1990]. The sign is 12 × 14 in, to fit the 0.30 × 0.36 m slot between the frame and the standpipe. Its ¾ and ⅝ in lines are under that later 1 in minimum, which was not a 1990 rule.
- **OSHA 29 CFR 1910.37(b)(5)** (since 1974; via 叙事 §5 and `wallpaper_motion/30_narrative_phosphor.md` §3.5, not re-read here): a door likely to be taken for an exit is marked "Not an Exit". It sets the words only.
- **Trade practice** [ESTIMATE, `01_research.md` §1.5]: a stencilled painted numeral 12–24 in high on the block beside the door; small engraved two-ply plastic or painted steel plates.

## 2. Stencil floor token

- **Face:** TeX Gyre Heros Bold (the placard's Helvetica), cut as a stencil. Figures are scaled to the cap height (a stencil set cuts letters and figures one height), so `B` and `1` stand equally tall.
- **Bridges:** 0.052 cap wide (20 mm at 15 in). One tie for each island of the stencil sheet: `0` top and bottom, `4` through the crossbar, `6` over the bowl, `8` top and bottom, `9` under the bowl, `A` through the bar. The `B`'s stem stands alone (a gap the full height along its inner edge), the classic stencil B. The generator finds the counters itself.
- **Size:** cap **15 in (381 mm)**: the widest two-glyph token (`B7`, 625 mm) fits the hinge-side wall. `0` is 262 mm wide, `B1` 512 mm.
  - From `B10` on: Heros Bold Condensed at 13.5 in (343 mm), one size for all of them (widest `B17`, 636 mm).
- **Placement:** baseline 100 mm above the top of the tan band (1.20 m in look A, as the lookdev). The token's door-side edge sits **120 mm from the frame's outer edge** on the hinge side; longer tokens grow toward the corner. Max width 640 mm, so the corner margin stays about 110 mm or more (the wall is about 0.87 m from the frame to the corner).
- **Paint:** black enamel, sRGB #1E1E1C, smoothness ~0.55 (the lookdev value). It is paint, not a plate: it follows the block's normal and dips into the mortar joints.
- **Files:** `out/stencil/stencil_<token>.png` (white on transparent, 2 px/mm, 24 mm padding; the baseline is 24 mm above the bottom edge), `stencil_<token>_bleed.png` (the mask dilated 1.5 mm and blurred 3 mm: brush bleed and overspray for the edge), `stencil_<token>.svg` (vector, mm). Glyphs with advances, ink boxes and SVG paths: `glyphs_bold.json`, `glyphs_cn.json`, for composing any token at runtime.

## 3. Plates: type, sizes, spacing

- **Face:** TeX Gyre Heros Bold caps throughout (the placard's face). A line that overflows the measure switches to Heros Bold Condensed at the same cap height (`LEVELS B10 TO B11`, tokens from `B10`). No Bayon, no digits in Bayon.
- **Cap heights** (inches, mm):

| Role | Cap | Rule it meets |
|---|---|---|
| Floor token, wall sign | 5 in (127) | IBC form ≥ 5 in |
| Stair letter `A` | 3 in (76.2) | LL5 608.4 ≥ 3 in |
| `STAIR A` on the wall sign | 1.5 in (38.1) | IBC form stair ID ≥ 1.5 in |
| Verdict `NO REENTRY` / `REENTRY`, `NOT AN EXIT` | 1 in (25.4) | LL5 608.5 ≥ ½ in |
| Terminus `LEVELS …`, label `STAIR` | ¾ in (19.1) | — |
| Labels `LEVEL`, `NO ROOF ACCESS` | ⅝ in (15.9) | — |
| Detail `NEAREST REENTRY` / `ON LEVEL B1` / `ON THIS FLOOR` | ½ in (12.7) | LL5 608.5 ≥ ½ in |

- **Tracking:** tokens 0; `STAIR A`, verdicts and terminus +0.04 em; detail +0.06 em; labels +0.08 em.
- **Layout:** every line centred on its ink; the block centred vertically (equal top and bottom margins, side margins ¾ in). Rows and gaps: `layout.json` (baseline of every run in mm from the plate's bottom edge, ink width, face).
- **Hierarchy:** the floor token is the biggest thing on the wall sign; on the leaves the verdict (`NO REENTRY` / `REENTRY`) leads and the ½ in lines add the detail. The two door plates set the same parts in the same order and size. The line breaks are layout; the words are 叙事's.
- **NOT AN EXIT** is one line at one size, black on white. A bigger EXIT, or red, would read as an EXIT sign at a glance, and the lit red EXIT signs are the game's goal beacons. OSHA 29 CFR 1910.37(b)(5) asks only for the words.
- **Colourways:** dark plates (white on black) tell you where you are and where you can go: the wall sign, the far door, the stair letter. Pale plates (black on white) say no: `NO REENTRY`, `NOT AN EXIT`. Red stays only on EXIT signs and the standpipe.

## 4. Plates: material (two-ply engraved plastic, 1/16 in)

- **Make-up:** a 1.6 mm sheet, a thin cap layer over a contrasting core. Letters are routed 0.3 mm deep through the cap and show the core; the 45° bevelled edge (1.0 mm wide) shows the core as a thin outline round the plate. Corner radius ⅛ in (3.2 mm).
- **Colours (sRGB):** black #1D1C1A, white #ECEAE4. Screws zinc #9A9C97.
- **Fixing:** door plates are bonded (no screws through a labelled fire door). The wall sign has four pan-head screws, ½ in in from each corner.
- **Shading:** smoothness ~0.55 on the cap face, ~0.25 on the engraved floor and the bevel (use `_mask`), not metallic. Screws metallic ~0.8.
- **Geometry:** a 1.6 mm box (or a quad 1.6 mm off the surface) with the albedo on the face; the bevel, letters and screws come from the height map.

## 5. Plate files

`out/plates/<name>_albedo.png` (sRGB, alpha = the plate shape), `<name>_height.png` (grey, **255 = 3.2 mm off the surface**: plate face 128, engraved floor 104, bevel 48 → 128, screw crowns ~221; blurred 0.3 mm), `<name>_mask.png` (engraved letters white). 4 px/mm. Sizes in px: wall sign 1220 × 1424, door plates 1120 × 508, NOT AN EXIT 1220 × 308, letter plate 612 × 612.

Names: `stair<k>_exit_id`, `stair<k>_far_id`, `stair<k>_exit_door`, `stair<k>_far_door`, `not_an_exit`, `stair_letter_A`. Stair *k* joins `token(k)` and `token(k+1)`; `token(0)` = `0`, `token(k)` = `B<k>`. Stair 0 is the first C22 stair (`LEVEL 0` → `LEVEL B1`).

## 6. Every floor

Every token moves down one per floor. 游戏视觉 chose runtime setting (10-22, message after 13:39): 关卡设计 moves one stair shell 3.5 m per floor, and the shell's SetFloor(int, label) sets the tokens.

- **Plates:** `./stair_signs <out> <n> runtime` writes `<out>/runtime/`:
  - the fixed parts as art: `rt_id` (STAIR A, NO ROOF ACCESS, LEVEL), `rt_exit_door` (NO REENTRY, NEAREST REENTRY), `far_door`, `not_an_exit`, `stair_letter_A`;
  - `runtime_slots.json`, one record per slot: the template (`{floor}`, `LEVELS {from} TO {to}`, `ON LEVEL {to}`), face, cap and em in mm, tracking in em, baseline in mm from the plate's bottom edge, the centre x, the measure, and the colour. Centre the ink box, not the advance. Switch to Heros Bold Condensed at the same cap when a run is wider than the measure.
  - `{floor}` is this landing's token; `{from}` and `{to}` are the stair's upper and lower tokens.
  - Runtime text has no engraved relief in the height map. Either accept that, or render the slot runs into the plate's height and mask once per floor.
- **Stencil:** compose from `out/stencil/glyphs_bold.json` (tokens of 1–2 glyphs, cap 381 mm) or `glyphs_cn.json` (3+ glyphs: scale its numbers by 343/381). Figures are already at cap height. Glyph ink boxes sit side by side on baseline 0. The gap between inks is 0.135 cap, minus 0.02 cap for each neighbour that is one of `0 3 6 8 9`, and minus 0.03 cap when the right glyph is `1` (`B` counts as straight). Checked: composing `0`, `B1`, `B2` and `B3` from the JSON gives the baked widths to 0.1 mm.
- **Baked option (kept):** `./stair_signs <out> 20` still bakes complete per-floor sets (stairs 0–19, about 16 MB) for checks and boards.

## 7. Reading distances (1080p, vFOV 76, flat albedo; `proof/legibility.jpg`)

The distance at which a cap height reaches 12 px (the Info glance rule) and 8 px (still legible bold caps):

| Tier | 12 px at | 8 px at |
|---|---|---|
| Stencil token 15 in | 21.9 m | 32.9 m |
| Wall-sign token 5 in | 7.3 m | 11.0 m |
| Stair letter 3 in | 4.4 m | 6.6 m |
| `STAIR A` 1.5 in | 2.2 m | 3.3 m |
| Verdicts and `NOT AN EXIT` 1 in | 1.46 m | 2.2 m |
| Terminus ¾ in | 1.1 m | 1.65 m |
| Labels ⅝ in | 0.9 m | 1.4 m |
| Detail ½ in | 0.7 m | 1.1 m |

The floor reads from anywhere in the stair (the stencil) and from across a landing (the wall sign). The re-entry fine print needs the player at the door, which is where they read it, trying the door. If 交互设计 makes the plates READ SIGN targets, the EXAMINE view shows them full size.

## 8. Review board and VL

- Figma section `3545:7368` FRONTROOMS · STAIRWELL SIGNS (C22) · 信息设计: SG01 `3545:7369` (the set at one scale), SG02 `3545:7401` (the three walls with the signs placed).
- VL371 `3543:7368` (legibility at 1 / 2 / 3.5 / 6 m), images `Frontrooms3D/Documentation/research/info_type/images/vl371_stair_signs_legibility_{1to1,zoom}.jpg`.

## 9. Not done / open

- Lighting and the in-game check are 游戏视觉's (a VL in the stair once the C22 stair lands).
- UBC Std 33-2 is unread (§1). If it turns up, check the wall sign's order and sizes against it.
- 叙事's §5 table says the stencil is on the latch side; the lookdev and `10_directions.md` §1.4 put it on the hinge side, because the latch side carries the wall sign and the standpipe. This spec follows the lookdev (asked 叙事 to fix the table).
- WebGL: a separate tier (e.g. 512 px plates on the WebGL tab). Desktop is the reference.
