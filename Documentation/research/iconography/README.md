# Iconography: audit, research, system, revision preview (平面视觉, 2026-10-30)

Red's ask (10-30): audit the game's iconography (today: the key and the cursor marks; the run state could have one too); research our own graphic language, AAA iconography standards (recognisable and still expressive) and how Swiss typography keeps type and icon one language; design our icon system. Then: "revise我们现有所有图形语言（包括我们已经敲定好的钥匙，壁纸pattern图形等…），让我对比预览一下".

**ON HOLD (Red, 2026-10-30 12:0x): "目前这部分线暂且搁置，我之后会跟你再提起时再做revise，目前都用现有的".** All current marks stay in use. Nothing here is built or routed until Red reopens the topic.

## Figma (file 0tCbAiVUlrPId3RWd9LRif, page 2099:76)

Section `3773:8549` "FRONTROOMS · ICONOGRAPHY · AUDIT + SYSTEM".

| Board | Node | Content |
|---|---|---|
| IC00 Brief | `3773:8553` | Red's four questions answered on one page; decisions waiting; downloads waiting |
| IC01 Our graphic language | `3773:8557` | the vernaculars (thumbnails exported from our boards), two layers of type, colour tokens, house rules quoted |
| IC02 What we draw today | `3773:8561` | inventory index: 11 families, ~120 drawings, owner, stroke today, revision board |
| IC03 Audit | `3773:8565` | evidence strip + 19 findings with severity and the rule that fixes each; asks Q4–Q6 |
| IC04 AAA iconography | `3773:8569` | rules with numbers (XAG, SF Symbols, Material, GAG, ISO/ANSI), 12 shipped games, six moves for recognisable + expressive |
| IC05 Swiss: type and icon | `3773:8573` | six H's measured from the project fonts, ten schools, eleven rules |
| IC06 The system | `3773:8577` | the shared unit (HUD + world masters), two-family table, registry, 15 rules |
| IC07 Gait + noise | `3773:8581` | the run-state glyph: construction, actual size, D04 in-context crops, options B–D, asks Q1–Q3 |
| RV01 Crosshair marks + key | `3798:8601` | NOW (live master instances) vs REVISED (detached copies, stroke = stem, paper tag) |
| RV02 Gait · stamina · hold · touch | `3798:8605` | stamina slots, hold ring, stick, RUN, USE, pause |
| RV03 Touch legend | `3798:8609` | seven 22 pt glyphs, one stroke; the legend lines |
| RV04 Placard pictograms | `3798:8613` | from tutorial_placard.swift at 8 px/mm; sign weight, 90° heads, sound arcs |
| RV05 Wallpaper | `3798:8617` | WP03 at 55° (today) vs the same generator at 45°; the wall arrow; one arrow |
| RV06 Signs, menus, what stays | `3798:8621` | FIXED, key-panel rule, threat readout; what stays and why |

## Files

- `R1_internal_inventory.md`: read-only audit of every mark, with file:line references (445 lines).
- `R2_aaa_iconography.md` + `R2_sources.tsv`: AAA / platform guidelines, case studies, 15 principles (74 sources).
- `R3_swiss_type_icon.md` + `R3_sources.tsv`: Swiss / wayfinding / standards, 15-rule consistency spec (79 sources).
- `20_system.md`: the system as a spec for builders.
- `30_revision.md`: every revision row (NOW → REVISED, owner, cost).

## Parked asks (only when Red reopens the topic)

- Q1 Run-state glyph: A footprints + 0/1/3 arcs (recommended) / B figure / C words / D stamina only (IC07).
- Q2 When shown: while Shift or Ctrl is held + 2 s (recommended) / always.
- Q3 Settings rows SHOW NOISE · AUTO / ALWAYS and HUD SCALE 100–200 % (recommended: both).
- Q4 Yellow = focus only (recommended).
- Q5 Key map says FIXED instead of the pixel padlock (recommended).
- Q6 Hold progress = one ring round the dot on every platform (recommended).
- Q7 Per row on RV01–RV06: keep NOW or take REVISED.
- Downloads: 45 reference images (R2 §6: 25, R3: 20) are listed with page and image URLs; none downloaded.

## Measurement notes

- Stems measured from outlines read with CoreText from the project's own font files (`~/FrontRoomsVisualWork/iconography/tools/text2svg.swift`, `stems.swift`, `stems_from_outlines.json`), stem through the H at 25 % of its height.
- Bayon's drawn H is 0.696 em while its cap-height metric says 0.714 em; its stem is 0.122 em = 0.174 of the visible H (0.170 of the metric).
- Source Serif 4 at wght 400 / opsz 20 measures 0.142 H. R3's table lists 0.106 for that instance; 0.142 supersedes it.
- The 45° wallpaper comes from a copy of `Tools/print/patterns/hard_edge.py` with `ARM_DEG = 45` and a private output folder (`~/FrontRoomsVisualWork/iconography/revision/wallpaper/`); the same copy at 55° reproduces `out/hard_edge/hard_edge.svg` byte for byte.
