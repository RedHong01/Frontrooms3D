# Print / Form: the paper derivative of the P1 deck styles (v1, 2026-10-15)

Owner: 平面视觉 · 印刷与演示 (Prints / Presentations). Red's principle: the P1 deck styles are the base, and each medium derives its own system from it, improved for its purpose but still one design language. This is the derivation for forms: the playtest questionnaire first, then other paper forms.

The P1 base is unchanged:

| P1 style | Spec |
|---|---|
| Title | Bayon 88/80 |
| Statement | Source Serif 4 50/42 |
| Body | Source Serif 4 24/26 |
| Label | Bayon 20/20 |
| Meta | IBM Plex Mono 13/16 |

**Figma**
- The revised form is `04 · ONE-PAGE PLAYTEST / LETTER · v2 · CANARY 25 %` (3207:7280), in section 3198:7273 at x 4932, y 3880.
- There is also a white-paper version, `… · v2 · WHITE` (3227:7424), at x 5664.
- The boards are PF01 (before / after, 3210:7267) and PF02 (system, 3210:7295).
- v1 (2946:7087, content unchanged) moved into this section on 2026-10-15 at Red's request, renamed "… · v1". It came from Advertising's 2800:6096.
- Row 4 now runs old → new: v1 at x 4200, v2 canary at x 4932, v2 white at x 5664.
- **v3, the answer sheet (Scantron edition, §9):** `… · v3 · ANSWER SHEET · CANARY 25 %` (3290:7280, x 6396) and `… · WHITE` (3290:7602, x 7128), with 12 pt fill boxes. The reader is in `Tools/playtest_scan/`. Boards PF03 (anatomy, 3295:7280) and PF04 (fill shapes, 3285:7280).

**Current revision: REV. 10-22-26 (§11), built 2026-10-22.** Red, 2026-10-22: "同时在问卷里的这个during play的section在未来的版本就不需要了" ("the During play section of the questionnaire is no longer needed in future versions").
- Both editions (FR-PT 04 questionnaire and FR-PT 05 answer sheet) drop 02 During play. The later sections are renumbered and re-flowed into the freed space.
- The same revision adds the look-eye item (Red kept E·1 and wants it checked). Its wording comes from 系统设计 and goes in the After play interview.
- On the answer sheet the scanner rows move with the re-flow, so the reader template was regenerated from the new frame, the simulated tests rerun, and both InDesign files rebuilt from the new frames. The v2 / v3 frames stay as the record of the 10-15 editions.
- Figma: section 3510:7478 (questionnaire v2.1 3512:7489 / 3512:7677, answer sheet v3.1 3510:7479 / 3510:7787). Details in §11.

## 1. Medium

- **Page:** US Letter, 612 × 792 pt, on **canary paper** (Red, 2026-10-15). The brand yellow becomes the paper, as on the yellow copy of a 1990 multi-part form, and every mark stays black.
- **Output:** black only, printed on canary stock or photocopied onto it. Turn off the frame fill (#FCF7CE) when you print on yellow stock. On white paper, print the fill full-bleed.
- **Use:** read at arm's length and filled in with a pen by a playtester or a moderator.

Because of this, nothing is thinner than 0.5 pt and no tint is lighter than about 8 %.

## 2. Grid

The grid is a typewriter grid, the grid real 1970s–90s office forms were built on.

| | Value | Why |
|---|---|---|
| Margins | 36 pt on all sides | ½ in. The frame is 540 × 720 pt. |
| Horizontal unit | **6 pt = 1 character** | IBM Plex Mono 10 pt advances 6 pt, i.e. 12 cpi (elite pitch). The 540 pt measure is 90 characters. |
| Fields | **18 × 30 pt** (5 characters), grouped as 6 × 90 pt | Two paired columns of 270 pt. The rating table is 360 + 6 × 30. |
| Vertical unit | **12 pt line = 6 lines per inch** | Typewriter line spacing. Half-steps of 6 pt are allowed. |
| Writing band | **18 pt** (6.35 mm) | Was 14 pt in v1. A real SF 63 uses 24 pt (two lines) per entry; one page holds 18. |
| Text axis | x 60 | The number hangs in the first 24 pt (x 36–60); questions start at x 60. |

Every bar, rule and cell edge sits on the 6 pt grid. Small labels and the checkbox rows (14 pt pitch, as on SF 63) are centred optically inside their cells.

**Calibration.** The grid was checked against a real SF 63 Memorandum of Call (rev. Aug 1967, GPO 1969), measured from a public-domain scan; research file `~/FrontRoomsVisualWork/reference/print_reference/research/C_documents_signage.md` §4.9.
- **Single-entry fields:** 0.33 in, two typewriter lines at 6 lpi.
- **Message field:** 2.00 in, 12 lines.
- **Labels:** about 7 pt caps (cap height 5.0–5.3 pt), hanging from each field's top-left.
- **Rules:** about 0.75 and 1.5 pt.
- **Checkboxes:** 9.3 pt, on a 14 pt row pitch.
- **Footer:** the form ID on the left.

## 3. Type

These are local Figma text styles, prefixed `Print / Form /`.

| Style | Spec | Derived from P1 | Use |
|---|---|---|---|
| Title `S:54ac7c9e…` | Bayon 32/36, −1 % | Title, Bayon 88/80 | Masthead |
| Section `S:9d69e6c6…` | Bayon 12/18, +4 %, caps | Label, Bayon 20/20 | Reversed on an 18 pt black bar, so the type shows the paper. The section number is set in mono. |
| Question `S:21606a72…` | Source Serif 4 10/12 | Body, Serif 24/26 | Questions and rating statements |
| Hint `S:961f6157…` | Source Serif 4 Italic 10/12 | New | Follow-up prompts: italic instead of a smaller size |
| Number `S:b3a95cc4…` | IBM Plex Mono 8/12 | Meta, Plex Mono 13/16 | Question numbers, scale heads, checkbox labels, consent line |
| Label `S:52b46759…` | IBM Plex Mono 7/8, caps, +2 % | New | Cell labels and the revision line. 7 pt is the label size measured on SF 63. |
| Fill `S:147e7feb…` | IBM Plex Mono 10/12 | New | Typed fill; one character = 6 pt |

That is five text sizes: 32, 12, 10, 8 and 7. The logo is a separate graphic. v1 used seven: 32, 11, 10, 9.5, 8, 7.5 and 6.5, measured on 2946:7087.

Every number is set in mono. Bayon draws "1" as a bare stroke and "0" like an O, so numerals never go in Bayon.

## 4. Paper, ink, rules and boxes

- **Paper:** #FCF7CE, the brand call yellow #F4DF3B at 25 % over white: a canary copy.
  - **Red's pick, 2026-10-15:** 25 % "plus a white version without the full yellow". The white version (3227:7424) has the same ink on white paper, with reversed type in white.
  - **Why paper and not ink.** Red's call, 2026-10-15. A first try put the call yellow on the call cells (the rating boxes and the cue choice), and Red rejected it. With canary paper the brand colour carries the whole sheet while the ink stays one colour.
  - **Also tried:** 40 % #FBF2B1, which came first and was replaced by 25 %. Other options are 60 % #F8EA89 or the full #F4DF3B.
- **Ink:** black #000000 only. Nothing is white: checkboxes are unfilled, and reversed type in the bars is set in the paper colour, so on yellow stock it really is the paper showing through.
- **Rules:** the two weights measured on SF 63.
  - 1.5 pt for the footer rule; the section bars are filled black;
  - 0.75 pt for cell rules, the rating spine and the head and foot rules;
  - 0.5 pt at 45 % black for writing lines, so they read as ink on the paper.
- **Checkboxes:** 9 pt, 0.75 pt stroke, centred in each 30 pt field.
- **Zebra:** 8 % black on even rating rows, which reads as a darker yellow.
- **Logo:** Red's light-paper lockup (a clone of 2235:44), scaled to 0.1 and set on the title baseline. It is black ink, with the echo s's fading from paper to black (S1 from 0.15 → 1, S2 0 → 0.25). There is no black chip: Advertising flagged that v1's chip overrides the echo ramps, so it breaks the relay S rule.

## 5. Layout of the playtest form v2

All values are in pt.

| Band | y | Content |
|---|---|---|
| Masthead | 36–72 | Title, with the logo right-aligned on its baseline |
| Session row | 78–102 | DATE · PLATFORM (check one: MAC, WINDOWS, IPHONE) |
| 01 Before play | 114–168 | Bar, then the question and one writing band |
| 02 During play | 180–306 | Bar, then a 2 × 2 grid of 54 pt cells (question plus two writing bands, or question, hint and one band) |
| 03 Ratings | 318–498 | Bar; a head row with the scale defined once and columns 1–5 · N/E; then 8 rows of 18 pt with the spine at x 390 |
| 04 After play | 510–726 | Bar, then a 2 × 3 grid of 66 pt cells; "Most helpful cue" sits in the last cell |
| Footer | 738–756 | 1.5 pt rule; on the left "FR-PT 04" (Number) and "REV. 10-15-26" (Label); on the right the consent line "You may skip any question or stop at any time." |

## 6. What changed from v1, and why

**From the analysis of Arthur's set:**
- **Grid.** v1 had loose 265 / 10 / 265 columns, 14 pt answer lines and 22 pt rating rows. v2 uses the typewriter grid above.
- **Form grammar.** Labels sit in the cell corners, there are two rule weights, and a form number with a revision date ("FR-PT 04 · REV. 10-15-26") appears in the footer.
- **One ink on brand paper.** There is no black logo chip and no ghost numerals. The yellow is the paper.

**From Red's own revision logic ("say it once"):**
- The callout chips (SETUP / WRITE, ASK / LIVE, CIRCLE / ONE, OPEN / NOTE) moved into the section bars.
- The intro line was removed; it repeated the bar instructions.
- The ghost numerals 01–04 were removed; they repeated the section numbers and collided with the answer space.
- The scale is defined once, in the ratings header.

## 7. Content changes (confirmed by Red, 2026-10-22: "全部按推荐")

1. **Rating scale.** v1's head rows read "SD D U A SA N/E" over "1 2 3 4 5 U", so "U" meant both *undecided* and *not experienced*. v2 uses 1–5 plus N/E, with anchors "1 = strongly disagree · 5 = strongly agree".
2. **Instruction.** v1 said "Circle one response" but printed boxes. v2 says "check one box per row".
3. **New footer form ID:** "FR-PT 04 · REV. 10-15-26". This is a fictional issuer and number, not a real agency form.
4. **Fields not added.** SESSION, TESTER and BUILD would make the form a real record, but the questions and fields are Red's, so they were left out.

## 8. Other Print media

Diaries, kit sheets and other forms derive the same way:
- keep the three families and roles;
- set the unit from the medium (6 pt typewriter grid for Letter forms);
- keep every number in mono;
- use two rule weights and one ink.

The 1920 × 1080 Figma documentation boards stay on the P1 deck styles: they are presentations.

## 9. The answer sheet (v3, FR-PT 05)

Red, 2026-10-15: make the playtest form a Scantron answer sheet. First research how US Scantron sheets are designed and the simplest way to enter them by machine. Then build it in the brand's design language: "确保可扫描的同时也符合品牌设计风格，就像是information design一样" ("make sure it scans, and fits the brand style at the same time, like information design").

**Sources:**
- **Research:** track D, `~/FrontRoomsVisualWork/reference/print_reference/research/D_scantron_omr.md`, summarised in `SOURCES.md`.
- **Figma:**
  - canary 3290:7280 (x 6396, y 3880);
  - white 3290:7602 (x 7128);
  - boards PF03 · Answer sheet (3295:7280) and PF04 · Fill shapes (3285:7280).
- **Build script:** `~/FrontRoomsVisualWork/print_ref_work/scantron/build_v3.js`. It is re-runnable, builds both versions, and returns the scanner geometry for the reader template.

### 9.1 Two readers, two zones

**The 36 pt frame is for people.** It holds the v2 form: the same grid, type, order and questions.

**The margin is for the scanner.** That is where print puts its registration marks and slug lines. It holds:
- four corner squares;
- one timing mark per machine row;
- the form-number slug;
- a one-line note that the column is the scanner's.

The page tells the two apart: what a person writes on sits inside the frame, and what the machine looks for sits outside it.

### 9.2 Scanner marks

| Element | Spec | Period source (track D) |
|---|---|---|
| Corner squares | 18 × 18 pt, black, at (18, 18), (576, 18), (18, 756) and (576, 756), so each sits 18 pt (¼ in) from two edges. Nothing touches them. | Remark's minimum anchor is 0.25 in; NCS 1988 survey stock has corner QA marks |
| Timing marks | 12 × 6 pt at x 582–594, one per machine row, centred on the row. The first is 12 × 12 (start) and the last is 12 × 9 (end). | One black mark per row, with the start mark longest and the end mark mid-height: Scan-Tron patent US 3,800,439 (1974); Form 882 as used in 1990. The 1990 882's marks sit ≈ 15 pt from the edge |
| Column note | "SCANNER COLUMN · DO NOT WRITE IN THIS AREA", Label style, rotated to read downwards, y 168–344 | "DO NOT WRITE IN THIS AREA" on Scantron F-236 (1988) |

### 9.3 Boxes

- **One size:** 12 × 12 pt, with a 0.75 pt black outline and a paper fill. That is two typewriter characters by one line.
  - Image OMR asks for targets 10–14 pt (Remark).
  - Scantron's own F-236 survey form used boxes (1988).
- **On the grid:** every box's vertical edges fall on the 6 pt grid, and each box is centred in its 18 pt row (3 pt above and below), like the timing marks.
  - The rating fields are 24 pt wide (four characters), from x 432, so a box sits 6 pt in from each side.
  - The session columns are 24 pt too, from x 138.
- **Spacing:**
  - rows are 18 pt apart, leaving a 6 pt gap;
  - columns are 24 pt apart, leaving a 12 pt gap.
- **The value is printed inside,** in IBM Plex Mono 6 pt at 45 % black, centred; N/E is set at 5 pt.
  - Period precedent: letters inside the bracket on Scantron 882; numerals inside the targets on NCS sheets.
  - A pencil fill covers the value, and the reader subtracts its ink.
  - The 6 pt and 5 pt sizes are guide marks, not text styles. They join the five text sizes of §3.
- **Numerals in, words out.**
  - Rating and session boxes carry their value inside.
  - Platform and cue boxes are blank, with the word to their right, as v2's checkboxes had.
- **Behind the boxes:** the zebra stays (8 % behind ratings 2, 4, 6, 8). The boxes' paper fill keeps the band out of the part the reader measures.
- **Answer boxes are not scanner squares.**
  - Answer boxes are 12 pt and sit inside the frame, with an outline and a printed value.
  - The corner squares are 18 pt, solid, and sit in the margin.
  - The reader ignores any dark square under half a corner square's area (a filled answer box is 0.44).

**How the shape was chosen** (2026-10-15, board PF04):
1. **Ovals**, the first build (3275:7280, VL292). Red: "我不是特别喜欢椭圆形的的设计语言，我希望能够更加符合我项目的设计语言", and asked for square or wallpaper-arrow fill shapes.
2. **Five candidates**, compared at 300 %: oval, square, diamond, arrow tag and chevron.
   - Recommended: the arrow, because a filled square looks like the scanner's own squares.
   - Red picked the arrow.
3. **Arrow tags** (3288:7280). Their roof was set at the wallpaper chevron's measured 55° (`Tools/print/frames/hard_edge/K00`). Red: "目前箭头的元素有一点突兀，还是用方块，箭头没办法fit到网格里面".
4. **Boxes**, the current build (3290:7280, VL293). The scanner-square risk is handled by size, position and the reader's size window above.

### 9.4 Layout (pt)

| Band | y | v3 |
|---|---|---|
| Masthead | 36–72 | Unchanged: the title, with the logo on baseline 64 |
| Session band | 72–138 | A label line, then three 18 pt rows. **DATE** (write-in) · **SESSION NO.** (three rows of 0–9, an 18 pt write-in box per row: 000–999) · **PLATFORM** (one per row) · **HOW TO MARK** ("FILL THE BOX" with a filled example; four wrong marks ✓ ✗ • ◐ "NOT"; "PENCIL · ERASE CLEANLY") |
| 01 Before play | 150–204 | As v2 |
| 02 During play | 216–342 | As v2 |
| 03 Ratings | 354–528 | Head row 12 pt (the anchors); 8 rows of 18 pt; statements x 60–420, spine x 426, six 24 pt fields x 432–576; fill boxes replace v2's 9 pt checkboxes; the bar reads "AFTER PLAY · FILL ONE BOX PER ROW" |
| 04 After play | 540–756 | As v2; the cue becomes 2 rows of 3: DOOR, ROOM DETAIL, SOUND / LIGHT, HUD, RELAY |
| Slug line | baseline 768 | In the bottom margin, between the corner squares: "FR-PT 05 REV. 10-15-26" on the left, the consent line on the right |

**Vertical budget.** The 18 pt session rows needed 18 pt more height. It came from:
- the band rule, which moved from y 78 to 72;
- the rating head row, which went from 18 to 12;
- the 6 pt that v2 left below section 04.

No writing band lost height.

### 9.5 What the research changed, kept and left out

**Changed**, after the research checked the first build:
- **Session rows** went from 12 to 18 pt. At 12 pt, a 3 pt gap let an overfilled target reach the next digit.
- **Targets** went from 9 to 10 pt tall, inside Remark's 10–14 pt; the boxes are now 12 pt.
- **The cue** went from three rows of 12 pt to two rows of 18 pt.
- **Start and end timing marks** were added.
- **Reader:**
  - per-target blank calibration from a blank sheet;
  - the light, check and MULTI rules;
  - a timing-mark count;
  - shape-aware read areas, so any target shape reads.

**Kept:**
- US Letter;
- black ink on canary paper;
- the 1/6 in grid (the 12 pt line is Scantron's own 6 rows per inch);
- the rating table, on 24 pt fields since the boxes;
- corner squares;
- the reading approach.

**Not adopted:**
- **Drop-out colour:** only dedicated IR scanners need it, and the brand stays one black ink.
- **A name grid:** a 1990 study found students fill it in by reflex, then worry about anonymity.
- **Comments on the back:** this is a one-page form.

### 9.6 Printing and reading

- **On canary stock, print the WHITE version, black only.**
  - Print from the InDesign file, `Print/FR-PT05_Playtest_Answer_Sheet.indd` (§10.1): it is the print master.
  - The canary version is the on-screen preview, and the option for a colour printer on white paper.
  - This replaces §1's "turn the frame fill off".
- **Reader:** `Tools/playtest_scan/`. Its README is the operator's manual: print, fill, scan, run, flags.
- **Scans and results stay outside the repo.** They hold playtesters' handwriting.
- **Tested 2026-10-15 (VL293, box edition),** on simulated sheets made from the faithful 3× render:
  - 560 / 560 fields on 40 sheets: flatbed scan, photocopy, phone photo and phone under tungsten light, 8 of them upside down;
  - 224 / 224 at half resolution;
  - 168 / 168 on the white version;
  - 140 / 140 through the PDF route with blank calibration.
  - Measured on those sheets: the lightest real fill 0.38, the darkest erasure 0.17, the darkest empty row 0.05, against thresholds of 0.30 and 0.15.
  - The oval edition passed the same tests first (VL292).
- **Next:** one real sheet, printed, filled in pencil and scanned.

### 9.7 Content changes (confirmed by Red, 2026-10-22: "全部按推荐")

1. **SESSION NO. (three digits) is new.**
   - §7.4 left SESSION out of v2. A machine-read sheet needs it to join each scan to its session's notes.
   - It is a number, not a name, and the moderator fills it.
2. **Form number "FR-PT 05"** for the answer-sheet edition, at REV. 10-15-26. Like FR-PT 04, it is fictional.
3. **"Check one box" becomes "fill one box".** The cue options now run in two rows.

## 10. Production file (InDesign)

Red, 2026-10-15, to 系统设计: "把这个给我设置成正常的indesign文件，就是咱们的问卷，同时所有黑色要用rich black" ("set this up as a normal InDesign file, our questionnaire, with every black in rich black").

- **File:** `EGAM-401A-01 Individual Game Project/Print/FR-PT04_Playtest_Questionnaire.indd`, outside the repo; printable PDF `Print/FR-PT04_Playtest_Questionnaire_printable.pdf`.
  - REV. 10-22-26 (2026-10-22, VL352; rebuilt at opsz 10 13:49, VL372): rebuilt by 印刷与演示 from the v2.1 white form 3512:7677 with 系统设计's kit `~/FrontRoomsVisualWork/print_indesign/`. Its SH / TX tables are regenerated from the Figma export by `gen_tables.py`; the kit's baseline check reads 0.000 pt over 66 frames.
  - REV. 10-15-26: built by 系统设计 from 3227:7424 (VL295); archived in `Print/_archive_REV-10-15-26/`.
- **Rich black lives only in InDesign** (Red 10-15, for every chat): "所有rich black 都是在Indesign里，因为只有indesign是CMYK印刷 … figma里面不用Rich Black" ("all rich black is in InDesign, because only InDesign is CMYK print; Figma doesn't use rich black"). The Figma frames stay plain black (RGB).
- **Ink: rich black everywhere in the InDesign files** (Red 10-15, confirmed to 印刷与演示 the same day).
  - The swatch is C60 M40 Y40 K100, with no [Black] or Registration.
  - This holds for the 7–8 pt text and the hairlines too: Red accepts the registration risk.
  - It supersedes §1's "black only" for this file. The Figma frames stay as they are.
- **Tints match Figma by lightness, not by percentage.** A rich-black tint is darker than the same percentage of grey.
  - Each tint is set to the rich-black percentage whose L\*, in the document's CMYK profile (US Web Coated SWOP v2), equals Figma's grey.
  - Zebra 8 % becomes 4 % (L\* 93.2), writing lines and values 45 % become 35 % (L\* 60.1), and the logo echo 25 % becomes 18 % (L\* 77.4).
- **Line breaks match Figma.** Figma sets Source Serif 4 at its optical size (opsz 10 at 10 pt).
  - In InDesign, use the variable font at opsz 10. Or, if Adobe Fonts' static family of the same name shadows it, use the static optical-size cut whose breaks match Figma: try Caption first.
  - Adobe's default "Text" cut (opsz 20) is narrower and breaks three questions differently.
  - **Done since 2026-10-22 13:46 (VL372):** both kits set the serif in `Source Serif 4 Variable` (Adobe's GitHub release, `~/Library/Fonts/SourceSerif4Variable-{Roman,Italic}.ttf`, OFL) at wght 400, opsz = point size, through `designAxes` on the paragraph styles. Its family name differs from Adobe Fonts' static "Source Serif 4", so nothing shadows it; the game's Google build (`Assets/Resources/Fonts/SourceSerif4-Variable.ttf`) is named "Source Serif 4" and is shadowed. Static Caption (opsz 8) stays the fallback.
  - Set `designAxes` after `pointSize`. InDesign's automatic optical size keeps the opsz of the size a style had when the font was applied: the first 10-22 build came out at opsz 12 ("SmallText-12.000") from the 12 pt default.
  - Pick the font by PostScript name (the kits' `findCaption()`): an unknown style of an installed family resolves to its default instance and still reports INSTALLED. `doc.fonts` lists the variable font as "Caption ExtraLight"; read `characters[0].appliedFont` and `designAxes` instead (`id_probe_serif.jsx`).
- **Baselines match Figma, font by font** (found 10-15, VL296).
  - Figma centres each font's box (hhea ascender to descender) in the line height. A frame whose first baseline sits one leading below its top therefore goes at y + ld/2 + r × size − ld, where r = (ascender + descender) / 2 / unitsPerEm of that font.
  - From the font files: Bayon 0.3174, IBM Plex Mono 0.375, Source Serif 4 0.3505.
  - One r = 0.35 for every face sets Bayon about 0.03 em low (the 32 pt title 1 pt) and Plex Mono 0.025 em high.
  - Check baselines in the document (`lines[0].baseline`), not only on renders: Figma's export and PDFKit both snap baselines to whole pixels (0.33 pt at 3×).
- **Bayon** is packaged in `Print/Document Fonts` with its OFL licence. It has no "·", so a GREP style sets the dot in Plex Mono.
  - The Plex Mono dot is wider than Figma's fallback dot: on the 12 pt cue line it sat 1.83 pt right of Figma and the next word 3.67 pt right (measured 10-22). Since VL372, a second GREP style (`Space before dot`, " (?=·)") and the dot style each set tracking −113 (the label's +40 − 153), so the dot and the words after it land on Figma's positions within 0.34 pt.
  - Since 2026-10-16 Bayon is also installed in `~/Library/Fonts` (same file, md5 cda3375f), so it resolves however a document is opened. Keep `Print/Document Fonts` for packaging and for other machines.
  - Before that, Document Fonts loaded only for a .indd opened from `Print/`, and a crash-recovered document set Bayon in Khmer Sangam MN (10-15).

### 10.1 The answer sheet in InDesign (FR-PT 05)

Red, 2026-10-15: "答题卡我也需要你给我做成indesign文件，同时rich black" ("make the answer sheet an InDesign file too, in rich black"), from the white twin 3290:7602.

- **File:** `Print/FR-PT05_Playtest_Answer_Sheet.indd`, outside the repo. It is a separate edition and does not replace FR-PT04.
  - **Now REV. 10-22-26** (2026-10-22, VL352; rebuilt at opsz 10 13:46, VL372), built from 3510:7787. Printable PDF: `Print/FR-PT05_Playtest_Answer_Sheet_printable.pdf`. The REV. 10-15-26 file and its printable PDF are in `Print/_archive_REV-10-15-26/`, and the kit's 10-15 inputs are in `indesign/rev_10-15-26/`.
  - It is the master (Red 2026-10-22, "全部按推荐"). The three dated copies saved on 10-16 while Bayon was being fixed are in `Print/_archive_FR-PT05_2026-10-16/`, not deleted.
  - The old `FR-PT05_Playtest_Answer_Sheet.font-fixed-20261016_printable.pdf` is no longer in `Print/`; `FR-PT05_Playtest_Answer_Sheet_printable.pdf` (REV. 10-22-26, VL372) is the one to print.
- **Built natively by 印刷与演示 from the Figma export.** Kit: `~/FrontRoomsVisualWork/print_ref_work/scantron/indesign/`.
  - `fig_shapes.json`, `fig_texts.json` and `fig_values.json` are the geometry exported from 3290:7602.
  - `make_jsx.py` writes `id_build_answer_sheet.jsx` from `id_build_template.jsx`.
  - `id_close.jsx` closes FR-PT05 (a run on its own); `id_run.jsx` runs the build, which saves the file and exports `fr_pt05_proof.pdf` and a JPG preview.
  - `id_audit_black.jsx` (read only) lists the colour of every object and text run; `compare.py` overlays the proof on the Figma 3× export.
- **Structure:**
  - 6 layers, scanner marks on top ("do not move");
  - paragraph-style group "Answer sheet";
  - one object style per element;
  - swatches: Rich Black, its 4 % and 35 % tints, Rich Black 18 (mix), and the two logo gradients.
- **Scanner geometry is exact.** The build checks every corner square, timing mark and box against the reader template (`Tools/playtest_scan/FR-PT-05_template.json`) and logs the worst deviation: 0.0000 pt.
- **Print it like §9.6:** on canary stock, black only. The file has no paper colour.
- **Verified 2026-10-15 (VL296):**
  - 305 items, 0 overset;
  - 0 objects or text in [Black] or Registration;
  - scanner marks within 0.17 pt of the Figma render;
  - 280 / 280 fields read from 20 simulated fills of its PDF.
- **History: Caption, not yet in InDesign (2026-10-22 10:3x; solved 13:46 by the variable font, §10 and VL372).** Red activated it ("Caption 已经启用了"), but InDesign still reports Source Serif 4 Caption NOT_AVAILABLE, and Adobe Fonts' local entitlement list is unchanged since 10-14. Both REV. 10-22-26 files therefore use the Text cut for now.
  - The kits only take Caption when its status is INSTALLED. `itemByName()` alone is "valid" even for a missing font; the FR-PT05 template was fixed for this on 10-22.
  - At the Caption rebuild, also tune the "·" in the Bayon cue label. It is set in Plex Mono (Bayon has none), and sits 4 pt wider than Figma's fallback dot.
- **Caption (Red 2026-10-22: yes, "全部按推荐"; Red activates it in Creative Cloud):** then run `python3 make_jsx.py --caption`, close FR-PT05 (`id_close.jsx`) and run `id_run.jsx`. Until then "favorite aspects" sets on one line instead of two, and the serif lines run slightly narrower. Rebuild once, from the next revision's frames (top of this doc), together with FR-PT04 (also approved for an overwrite).
- **Rebuild rules,** after InDesign crashed on 10-15:
  - no `#targetengine "session"`;
  - the build refuses to run while FR-PT05 is open: close it in a separate run;
  - never close a referenced document inside a build.

## 11. REV. 10-22-26 (both forms)

Red, 2026-10-22: "同时在问卷里的这个during play的section在未来的版本就不需要了". The same revision adds the look-eye item; its wording is final from 系统设计.

**Figma.** Section `FRONTROOMS · PLAYTEST FORMS · REV. 10-22-26 (印刷与演示)`, 3510:7478, at (33937, 33200).
- It is a new section because 3198:7273 has no room: STAIRWELL C22 sits 800 px below it, and the 33937 column 400 px to its right.
- **Questionnaire FR-PT 04 v2.1:** canary 3512:7489, white 3512:7677. Made from a clone of v2 3207:7280, edited by layer name.
- **Answer sheet FR-PT 05 v3.1:** canary 3510:7479, white 3510:7787. Built by `~/FrontRoomsVisualWork/print_ref_work/scantron/build_v31.js` (r5).

**Content**
- **01 Before play:** the same question, now with two writing lines.
- **02 Experience ratings:** items 01–08 unchanged. Only the section number moved, from 03.
- **03 After play interview,** three rows of two:
  1. overall impression;
  2. favorite aspects;
  3. least favorite aspects;
  4. confusion, with its hint;
  5. "If you saw the crosshair turn into an eye, what did you think it meant?";
  6. the magic wand, with its hint. It is the wrap-up question, so it comes last.
- **Item 05 is the E·1 check** (`research/computer_ui/30_crosshair_grammar.md` §8).
  - It is open and asked after play, so it never primes the tester.
  - The form has no coding boxes, because a label such as BEING SEEN would hint at the meaning.
  - The interviewer codes each answer LOOK / READ, BEING SEEN, OTHER or DIDN'T NOTICE, working from the reader's `after_05` contact sheet.
  - The switch rule (BEING SEEN ≥ 2 in a round, or ≥ 25 % overall → E·3) belongs to 平面视觉.
- **Most helpful cue:** one row of six, under the interview.

**Layout (pt)**

| Band | FR-PT 04 v2.1 | FR-PT 05 v3.1 |
|---|---|---|
| Masthead and header band | 36–102, unchanged | 36–138, unchanged (scanner rows band1–3) |
| 01 Before play | bar 114; question 135; lines 168, 186 | bar 150; question 171; lines 204, 222 |
| 02 Ratings | bar 198; scale head 219; rows 234–378 (8 × 18); spine x 390 | bar 234; anchors 254; rows 264–408 (8 × 18); spine x 426; marks r01–r08 centred 273–399 |
| 03 Interview | bar 390; rows at 411 / 501 / 591 (pitch 90) | bar 420; rows at 441 / 525 / 609 (pitch 84) |
| Writing lines | at row top + 45 / 63 / 81; under a hint, + 63 / 81 (aligned across each row) | the same |
| Cue | label 690; 9 pt boxes at y 711, x 36 + 90 n | label 696; 12 pt boxes at y 717, x 36 + 90 n; one scanner row whose mark is the end mark (12 × 9, centred 723) |
| Footer | rule 737; FR-PT 04 · REV. 10-22-26 | slug 768; FR-PT 05 · REV. 10-22-26 |
| Scanner note | none | right margin, y 468–651, beside the interview and clear of every mark |

**Reader** (`Tools/playtest_scan/`)
- `FR-PT-05_template.json` is now REV. 10-22-26: 13 fields, 87 boxes, 12 marks, 9 crops, with paper probes taken from the export.
- The 10-15 template is `FR-PT-05_REV-10-15-26_template.json`. Choose by the REV. printed in the slug.
- Simulated reads: 280/280 on the white, canary, blank-calibrated and InDesign routes (VL352).

**InDesign:** both files were rebuilt on 2026-10-22 (§10, §10.1, VL352), then again at 13:46–13:49 with Source Serif 4 Variable at opsz 10 and the dot fix (VL372, PASS): 17/17 serif breaks on each form as in Figma, baselines 0.000 pt, marks within 0.167 pt, reader 280/280. These are the final REV. 10-22-26 files. The 10:24 Text-cut files are in `Print/_archive_REV-10-22-26_text-cut/`.
