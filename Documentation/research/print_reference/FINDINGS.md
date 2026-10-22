# "Press Any Key To Wake": deconstruction (Prints / Presentations, 2026-10-15)

Red's reference case: **"Press Any Key To Wake"** by Arthur, series NO.14, posted on rednote / 小红书 (watermark "rednote ID: 8933017175"). It is a fan-made collateral set for the Backrooms "level-0". Red asked for four things:
- deconstruct its grid and type system per paper format;
- research immersive game advertising materials;
- revise the playtest form.

This file covers the deconstruction. The other files are:
- [`PRINT_FORM_SYSTEM.md`](PRINT_FORM_SYSTEM.md): the derived Print / Form system and the revised playtest form;
- [`SOURCES.md`](SOURCES.md): sources.
- The immersive-materials research lands in `IMMERSIVE_MATERIALS.md`.

**Figma.** Section `FRONTROOMS · PRINT REFERENCE · PRESS ANY KEY TO WAKE (印刷与演示 · 15 Oct)` (3198:7273) on page 2099:76, at x 25257, y 26000. It holds 14 boards:

| Board | Node | Board | Node |
|---|---|---|---|
| PR01 the case | 3198:7274 | PR08 strips + tickets | 3198:7330 |
| PR02 formats | 3198:7282 | PR09 forms | 3198:7338 |
| PR03 lockup | 3198:7290 | PR10 image | 3198:7346 |
| PR04 colour | 3198:7298 | PR11 type | 3198:7354 |
| PR05 posters | 3198:7306 | PR12 for FrontRooms | 3198:7362 |
| PR06 spreads | 3198:7314 | PF01 playtest form | 3210:7267 |
| PR07 folds | 3198:7322 | PF02 Print / Form | 3210:7295 |

The revised form itself is 3207:7280.

**Method.**
- The only source is Red's six boards, at 768 × 1024 px. They are saved outside the repo at `~/FrontRoomsVisualWork/reference/press_any_key_to_wake/board_1..6.jpg`.
- Pieces were segmented, then each edge was snapped to the strongest luminance step within ±6 px. A few edges were corrected by hand where the snap locked onto an inner element: ticket T2's top had snapped to its wordmark. Type was measured from ink profiles. Tools are in `~/FrontRoomsVisualWork/print_ref_work/tools/`.
- One board pixel is about 0.6 % of a poster's width, so ratios are good to about ±1 %. Sizes are relative; the boards show mock-ups, not a common scale.
- A task is looking for the high-resolution originals and identifying the faces (`TYPE_USE.md`, pending).

## 1. What the set is

Arthur dresses one Backrooms level as about 37 printed pieces, as if an institution had issued them:
- posters, spreads, folded leaflets, strips, tickets and stickers;
- forms: a transmittal slip and memo, a wayfinding table, a trade ledger;
- a safety sign, a VHS tape and a booklet.

Three moves carry it:
1. **The title is a stamp, not a headline.** One small lockup, identical on every piece.
2. **One flat yellow**, used like a spot ink.
3. **Real paperwork.** Forms, signage and tickets borrow real document grammar, so the fiction reads as found rather than designed.

## 2. Formats (PR02)

Ratios are long side : short side, measured on the boards.

| Family | Pieces | Measured ratio | Reading |
|---|---|---|---|
| Poster | P1 P2 P3 G1 V2 V3 L7 | 1.394 · 1.374 · 1.400 · 1.402 · 1.382 · 1.350 · 1.374 | 1 : 1.40, e.g. 50 × 70 cm. Slightly squatter than A (1.414). |
| Tall | D1 D2 D3 | 1.661 · 1.680 · 1.661 | 1 : 1.66 (≈ 3 : 5) |
| Strip | D4 D5 | 2.994 · 3.006 | 1 : 3.0, used for paperwork and data |
| Fold + flap | L1–L4 | 1.845 – 1.864 | 1 : 1.85 with a 22.2 % header flap |
| Fold face | L5 L6 | 1.251 · 1.241 | 5 : 4, faces shown flat |
| Spread | S1 S2 | 1.416 · 1.418 | Two A-series pages, each 1 : 1.413 |
| Landscape | F1 F2 N1 | 1.413 · 1.594 · 1.580 | A landscape, 1.6, and card ratio 1.586 |
| Ticket | T1 T2 K1 | 3.9 · 3.9 · 3.6 | Name and stats tickets, a sign strip |
| Square | Y1 A1 | 1.000 · 1.000 | |
| Table | W1 | 1.494 | 2 : 3 |
| Die-cut | 4 stickers | none | Kiss border about 2–3 % of the sticker |
| Object | VHS, booklet | none | Photographed objects |

Each family has a job:
- **Posters and spreads** carry the image.
- **1 : 3 strips** carry paperwork and data.
- **Tickets and stickers** carry the name.

## 3. Grid (PR05–PR08)

There is **no column grid.** Everything hangs from an edge, a corner or a centre axis. Two margin tiers repeat:
- **Type and rules: 2–4 % of the width.** P3's title sits 3.9 % in; its side-column frame is 1.8 % from the edge.
- **Photos: about 9 % of the width.** P3's inset photo is 8.8 % in from the left and 13.8 % down. P2's text box sits 8.2 % / 7.2 % in and is 48 % wide.

**Anchors** that recur:

| Anchor | Where | Measured |
|---|---|---|
| Lockup centred on the tall posters | D1, D3 | Title baseline at 84.9 % of the height |
| Grey base band | D2 | Starts at 81.7 % (band = 18.3 %) |
| Horizon / yellow floor | P1 | 50.2 % |
| | S1 | Floor = 39.3 % of the page |
| | S2 | Floor = 17.5 % |
| Title band | L2, L4 | 37 % of the width × 51 % of the height, from 36 % down |
| | V2 | 37 % of the width × 71 % of the height, from 12 % down |
| Fold flap | L1–L4 | 22.2 % of the height |
| Leaflet body | L1–L4 | White panel 53 %, photo 47 % |
| Ticket stub | T1 | 1/3 of the width |
| Lockup on spreads | S1, S2 | About 26 % of the page width, bottom right, next to a column or on a hairline |

The **title band is a constant**: always 37 % of the format width, title turned 90°, the walking-man box left upright at its foot.

The **floor line moves** (50 % → 39 % → 18 %). It is the one variable that changes the mood from piece to piece.

## 4. The lockup (PR03)

Measured on the largest instance, board 1, where the cap height C = 12 px. It was checked on D1 (C = 6 px) and agrees within 1 px.

| Part | Rule |
|---|---|
| Title | One line, never broken, mixed case, semibold humanist sans. Width 12.1 C, x-height ≈ 0.7 C. |
| Box | A square of 1.8 C (21.5 px). It starts 0.25 C above the cap line and ends 0.5 C below the baseline, so it covers the descenders. Gap after the title: 0.6 C. The walking man is white or yellow on black. |
| Subline | "level-0" at ≈ 0.55 × the title size, baseline 1.0 C below. |
| Tagline | ≈ 0.5 × the title size, two lines; first baseline 1.67 C below (D1). |
| Stats row | ≈ 0.35 × the title size, spread across the full lockup width: "survival difficulty \| class 1 · safe · secure · Unconfined Entities". |

The lockup never scales up for emphasis. It covers 26 % of a spread page and 53 % of a tall poster's width.

**The level-0 wordmark** is a separate mark: a heavy lowercase grotesque with a yellow box behind the "0". The box is the ascender height + 10 %. It appears only on tickets, the VHS label and the leaflet flaps.

## 5. Colour (PR04)

Each colour is the median of a flat area.

| Role | Hex | Note |
|---|---|---|
| Yellow | **#D6BB0A** (214, 187, 10) | σ = 0.1 across 8 pieces. One spot ink, used as field, floor strip, inset strip, frame, wordmark box and map route. |
| Paper grey | #DFE0E2 | Signs, tickets, bands |
| White | #EEECED | Panels, labels |
| Black | #060203 | Night pages, CCTV, pictogram bands |
| Pictogram grey | #A2A0A1 | Warning-triangle strips |
| Alarm red | #BD4136 | The WARNING pill only. The red EXIT sign inside a photo is #B73F3D. |

The red header bar (#AD2417) is rednote presentation chrome, not part of the set.

Photos are pushed toward the yellow by tinting or duotone (D4, Y1, A1, S2).

## 6. Type (PR11; faces pending the type-ID task)

| Role | Class seen | Relative size |
|---|---|---|
| Title, tagline, stats, labels, body | One humanist sans, Myriad-class (semibold / regular) | 1 : 0.55 : 0.5 : 0.35 |
| Wordmark | Heavy neo-grotesque, lowercase | Logo only |
| Warning captions | The same humanist sans, bold caps, tight | |
| Form titles | Bold grotesque caps (TRANSMITTAL SLIP, MEMORANDUM OF CALL) | |
| Form labels | Tiny caps sans | Cap height ≈ 1 % of the slip width |
| Typed fill | Typewriter mono | ≈ 3.05 px per character. That is 12 cpi if the strip is 4¼ in wide. |
| Hand fill | Handwriting caps and script | |
| CCTV | Pixel / LCD mono | |
| Ledger | Condensed grotesque monogram "B.N.T.G."; bilingual labels with CJK sans | |

## 7. Image treatments (PR10)

Each photo is damaged on purpose, and the damage is what reads as "found":
- mosaic: the giant pixel chair;
- RGB signal noise: the cone;
- a surveillance grid: CCTV, with camera labels;
- cut-out silhouettes with numbered words. P1 spells "(1) The (2) meeting (3) is (4) cancelled" across the figures.
- a light leak / film burn: S1, the booklet;
- spotlight vignettes: N1;
- a glowing figure: A1;
- a yellow tint;
- an evidence network: photos joined by lines over a grey city map;
- piles of chairs.

## 8. Found-document strategy (PR09)

- **D5, slip + memo.**
  - A 1 : 3 strip joining an OF-41-style transmittal slip to an SF-63-style memorandum of call. It reproduces the real fine print: "63-109", "STANDARD FORM 63 (Rev. 8-76) Prescribed by GSA FPMR (41 CFR) 101-11.6", and a 1977 GPO imprint.
  - Cell grammar: labels in the top-left corner of each cell, a heavy outer frame and light inner rules, checkboxes in two columns.
  - Fills: a typed report in typewriter mono, handwritten caps and script ("14 Apr 28", "K. HARRIS"), and a marker redaction over FROM.
  - Its issuer is fictional: "MEG / EXPLORATION UNIT 7". The form numbers, though, are real US government numbers.
- **W1, wayfinding table.** Five columns: total km, segment km, direction pictogram, information, time ("???"). It has a heavy spine and black ink on yellow only. It reads like a transit timetable for "LEVEL 0 · THE LOBBY".
- **L7, B.N.T.G. trade record ticket.**
  - A condensed monogram and bilingual EN / 中文 labels.
  - Mirrored "items offered / received" tables, currency checkboxes ("almond water", "smile", "scrap"), a notes box and a notice box.
  - A form number in the footer.
- **Signage as world rules.** G1 and K1 use pictograms: AVOID VISUAL CONTACT, EXITS MAY NOT LEAD TO SAFE LEVELS, POSSIBLE MEMORY LOSS, NO LOUD NOISES. Behind G1 sits a ghost of the ISO running man.
- **Data as diegesis.** D4 overlays an MEG chart on a photo: "Dipole Moment (nAm)", "POST-EXPLORATION · BACKROOM RECORDING (MEG)". This is a pun: M.E.G. is the Backrooms explorer group, and MEG is magnetoencephalography.
- **Measured ratio.** Labels are tiny, typed fill is about 3 × the label, and the hand gets the rest. The form is mostly room to write.

## 9. What FrontRooms' Print system takes (PR12)

**Keep:**
- **A stamp, not a headline:** the logo plus one Bayon line, at fixed proportions.
- **One ink:** black only. The brand yellow can be the paper itself, a 1990 canary copy, but never a second ink. Red, 2026-10-15, chose canary paper over yellow call cells. Red is for alarm only.
- **Form grammar:** labels in cell corners, two rule weights, a form number with a revision date.
- **The typewriter grid.**
- **Fold-aware layouts.**
- **Paperwork that tells the story.**

**Leave:**
- **Arthur's yellow (#D6BB0A).** FrontRooms keeps one yellow per world.
- **Digital damage.** RGB noise reads as a modern sensor. Use 1990 analog damage instead: toner, fax banding, CRT scanlines, VHS tracking.
- **ISO-style pictograms.** ISO 7010 dates from 2003. A 1990 US exit is red EXIT lettering (NFPA 101).
- **Real agencies, seals and form numbers.** Use a fictional issuer only.
- **His faces.** Print stays in Bayon / Source Serif 4 / IBM Plex Mono.

The revision that applies all of this is in [`PRINT_FORM_SYSTEM.md`](PRINT_FORM_SYSTEM.md).
