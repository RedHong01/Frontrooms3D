# Playtest answer sheet: print, scan, read (FR-PT 05)

Owner: 平面视觉 · 印刷与演示.
- **Form design:** `Documentation/research/print_reference/PRINT_FORM_SYSTEM.md` §9.
- **Research:** track D, `~/FrontRoomsVisualWork/reference/print_reference/research/D_scantron_omr.md`.
- **Figma:** section 3198:7273. The canary version is **3290:7280**; the white version is **3290:7602**.

The playtest form has two readers:
- **People** read the v2 form inside the 36 pt frame, and fill 12 pt boxes.
- **The scanner** reads the margin: four corner squares, plus one timing mark beside every row of boxes. The first mark is tall (start) and the last is mid-height (end), as on a 1990 Scantron.

This folder turns a stack of filled sheets into:
- one CSV row per sheet;
- one straightened image per write-in answer;
- one image per question with every sheet's answer stacked, for reading;
- one proof sheet per page, so a person can check what the machine read.

> **Privacy: keep scans and results out of this repository.** The repo is pushed to GitHub, and filled sheets hold playtesters' handwriting and opinions. Work in a folder outside it, for example `~/FrontRoomsPlaytest/2026-10-20/`. As a safety net, `.gitignore` here ignores `out/`, `scans/`, `*.csv` and the built `render_pdf`.

## 1. Print

| Paper | Print this | Why |
|---|---|---|
| **Canary stock** (recommended) | the **WHITE** version, black only, at 100 % ("actual size"): the InDesign print master `Print/FR-PT05_Playtest_Answer_Sheet.indd` (course folder), built from Figma 3290:7602 | It holds no paper-coloured ink, so the stock shows through the boxes and the reversed type |
| White paper, black-only printer | the WHITE version | Same file; the sheet is simply white |
| White paper, colour printer, to preview canary | the CANARY version (3290:7280) | It prints #FCF7CE as ink; the printer's own margin stays white |

- **Print from InDesign** at 100 %, or from a PDF exported from it. Open the .indd from the `Print/` folder (double-click it in Finder) so Bayon loads from `Print/Document Fonts`. "Fit to page" also works: the reader handles scale.
  - Every black in that file is rich black (C60 M40 Y40 K100). On an office colour printer, choose black & white (grayscale) so small type doesn't pick up colour fringes.
  - Fallback: export the WHITE frame from Figma as PDF.
- **One printer, one batch.** Print one extra sheet and leave it blank: it calibrates the reader (§4).
- **Keep the outer 18 pt (¼ in) clear.** The corner squares and the timing marks sit 18 pt from the edges, inside every laser printer's printable area. If a printer clips them, the reader reports `unread: corner square not found`.

## 2. Fill in

- **Fill each box completely,** with a No. 2 (HB) pencil.
  - A dark pen reads too, but it can't be erased.
  - To change an answer, erase cleanly.
- **The moderator fills SESSION NO.:** write one digit in each tall box on the left, top to bottom, then fill that digit's small box on the same row. For example, 042 is 0 / 4 / 2.
- **Write-in answers go anywhere inside their cell;** they come back as images.
- **Keep the right margin clear:** it holds the timing marks.

## 3. Scan

- **Scanner or copier** (recommended): "scan to PDF" at **300 dpi, greyscale**, with auto-brightness off. Several sheets can go in one PDF, either way up.
- **Phone:** the Notes app's *Scan Documents*, or a flat, square-on photo with all four corner squares in view. The reader corrects perspective and colour casts.
- **Never use the blue channel** if a tool offers one: on canary stock it's the weak one. The reader uses the red and green channels.

## 4. Read

Run from the folder that holds the scans, so the outputs land there and not in the repo:

```bash
python3 <repo>/Tools/playtest_scan/omr_read.py <repo>/Tools/playtest_scan/FR-PT-05_template.json results.csv out/ --blank scans/blank.pdf scans/*.pdf
```

- **`--blank`** (optional, recommended): the blank sheet from the same print run, scanned the same way. Without it, each row's lightest box stands in for "empty".
- **Requirements:** Python 3 with numpy and Pillow.
- **PDF input:** the script builds `render_pdf` from `render_pdf.swift` the first time. This needs macOS with the Xcode command-line tools. On other systems, export the pages as PNG.
- **Speed:** about 1 s per page.

| Output | What it holds |
|---|---|
| `results.csv` | One row per page: `file`, `turned` (0 or 180), `session` (three digits), `session_1–3`, `platform`, `rating_01–08` (1–5 or NE), `cue`, `flags` |
| `out/check/<page>__check.png` | Proof sheet: the straightened page; read boxes outlined blue, problems red; each row's values in a column on the right; the page's flags at the top |
| `out/crops/<page>__<field>.png` | Each write-in cell, straightened, at 216 dpi: `date`, `session_written`, `before_01`, `during_01–04`, `after_01–05` |
| `out/by_question/<field>.png` | The same cell from every page, stacked and labelled, for reading and coding the open answers |

**Flags.** Every flagged page gets a look at its proof sheet.

| Flag | Meaning | What to do |
|---|---|---|
| `blank` | Nothing filled in the row | Usually a skipped question |
| `light` | A mark lighter than a fill, often an erasure; the value is left blank | Decide from the proof |
| `multi` | Two filled boxes in one row; the value is `MULTI` | Decide from the proof, or leave it blank |
| `check` | Read, but a second box is nearly as dark | Glance at the proof |
| `incomplete` | A SESSION NO. row is blank or MULTI, so `session` is empty | Read the digits from the `session_written` crop |
| `marks:k/N` | Only k of the N timing marks were found | Check the proof for a fold, a tear or a stray mark in the margin |
| `unread: …` | The page could not be straightened | Rescan with all four corners in view |

## 5. How it reads

1. **Corner squares.** In each corner window of the image, find the most square, solid dark shape of corner-square size.
2. **Homography.** Map template points (pt) to image pixels.
3. **Orientation.** Try the page both ways up and keep the way whose timing marks are dark.
4. **Row lock.** Lock each row onto its timing mark, then snap the row's boxes onto their printed outlines (±3 pt).
5. **Fill.** Measure the darkness inside each box, inset 1.75 pt from its outline. Then subtract the box's blank level:
   - with `--blank`, the same box on the blank sheet, plus the page's overall shift;
   - otherwise, the row's lightest box.
6. **Decide each row** (thresholds in the template):
   - fill below 0.15 → blank;
   - below 0.30 → light;
   - two boxes at 0.30 or more within 0.15 of each other → MULTI;
   - a rival within 0.15 → check;
   - otherwise → the darkest box.

**Answer boxes vs scanner squares.** A filled answer box is a dark square, like the scanner's own corner squares. Three things keep them apart:
- answer boxes are 12 pt and sit inside the frame, with an outline and a printed value;
- corner squares are 18 pt, solid, and sit in the margin;
- the reader ignores any dark square under half a corner square's area.

**Tested on 2026-10-15 (VL293).** Test sheets were made from the faithful Figma render of 3290:7280 with simulated pencil fills: over and under the box, partly filled, erased answers, real double marks and handwriting. Each was put through a flatbed scan, a photocopy, a phone photo or a phone under tungsten light, and 8 were turned upside down.

| Test | Fields correct |
|---|---|
| 40 sheets, as above | **560 of 560** |
| With `--blank` | 560 of 560 |
| The same pages at half resolution (about 100 dpi) | 224 of 224 |
| The white version | 168 of 168 |
| The PDF route with `--blank` | 140 of 140 |

Every flag was a real blank or a real double mark. On those sheets the lightest real fill measured 0.38, the darkest erasure 0.17 and the darkest empty row 0.05. The oval edition (3275:7280, replaced) passed the same tests first (VL292).

**Next:** a real printed, pencil-filled and scanned test sheet.

## 6. When the form changes

The template comes from the Figma layer names, so the form and the reader can't drift apart. Every scanner element is named `scan/…`:
- `scan/corner/TL|TR|BL|BR`
- `scan/mark/<id>`
- `scan/target/<field>/<value>`
- `scan/crop/<field>`

The build script (`~/FrontRoomsVisualWork/print_ref_work/scantron/build_v3.js`) returns these as JSON, as in `figma_export_3290-7280.json`. Then run:

```bash
python3 make_template.py figma_export_3290-7280.json FR-PT-05_template.json
```

Then rebuild the InDesign print master from the new Figma geometry (`PRINT_FORM_SYSTEM.md` §10.1). Its build checks every scanner element against this template and logs the worst deviation (0.0000 pt on 2026-10-15).

**Rules that keep a sheet readable:**
- every row of boxes has its own timing mark at the row's centre, and rows are at least 18 pt apart;
- all boxes are one size (12 × 12 pt), with their vertical edges on the 6 pt grid;
- nothing else is printed in the right margin, and nothing touches a corner square;
- only the box's own value is printed inside it (45 % grey in Figma, 35 % rich black in InDesign: the same lightness; calibrated out); no fill or rule crosses a box.
