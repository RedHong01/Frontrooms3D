# Info type system (平面视觉 · 信息设计)

Owner: 平面视觉 · 信息设计, since the split of 2026-10-15 (`GRAPHIC_VISUAL_SPLIT.md`).

Red's principle: the P1 deck styles are the base, and each medium derives its own system from them. A derived system is improved for its purpose but stays in one design language. Prints / Presentations owns the base. This file is the Info derivative: HUD, game UI, terminal, signage, and on-screen tables and forms.

Figma: file `0tCbAiVUlrPId3RWd9LRif`, page `2099:76`, section **FRONTROOMS · INFO TYPE SYSTEM** `3189:8187` (x 11009, y 31660).
- IT00 `3195:7137`: the style specimen.
- IT01 `3190:7117`: SD04 re-set in the Info styles, the reference sheet.
- Component set **Info / Status tag** `3189:8204`.

## 1. Sheets and tables (Letter landscape review sheets, on-screen tables, forms)

Derived from the playtest form (`2946:7087`), which derived it from the deck. Units are pt; 1 pt = 1 px in Figma.

### 1.1 Styles (local text styles in the file)

| Style | Setting | Use · colour |
|---|---|---|
| Info / Sheet / Running head | IBM Plex Mono 8/10, caps | Header meta only. Ink |
| Info / Sheet / Label | Bayon 11/12, +3 %, caps | Group label over the title. Ink |
| Info / Sheet / Title | Bayon 32/32, −1 % | One line per sheet. Ink |
| Info / Sheet / Ghost numeral | Bayon 120/104, −1 % | The group number, under the logo. #F0F0F0 |
| Info / Sheet / Intro | Source Serif 4 10/14, −1 % | Two lines at most, measure 432 (x 36–468). #4A4A4A |
| Info / Sheet / Footer | IBM Plex Mono 7.5/10 | Key definitions, sentence case. #4A4A4A |
| Info / Table / Column head | IBM Plex Mono 7/9, caps | Over a 0.75 pt ink head rule. Ink |
| Info / Table / ID | IBM Plex Mono 8/14 | Row IDs and figures. #4A4A4A |
| Info / Table / Row head | Source Serif 4 SemiBold 11/14, −1 % | The name column. Ink |
| Info / Table / Cell | Source Serif 4 11/14, −1 % | Cells. A closed decision (Decided, None, Later) is #8A8A8A |
| Info / Table / Cell small | Source Serif 4 9.5/14, −1 % | Secondary cells (Owner · Needs). #4A4A4A |
| Info / Table / Tag | IBM Plex Mono 7/8, caps | Status tags, 56 × 12, text inset 5 pt |
| Info / Wall / Title | Bayon 120/104, −1 % | A0 wall sheets only (§1.4). Ink |

Colours: ink #0A0A0A, grey #4A4A4A, closed #8A8A8A, zebra #F4F4F3, soft rule #C4C4C4, ghost #F0F0F0, decision yellow #F4DF3B.

**Black (Red, 2026-10-15, all chats):** Figma and screen work use plain RGB black only (ink #0A0A0A). No rich-black or CMYK swatches, and no converting frames. Rich black (C60 M40 Y40 K100) exists only in the InDesign CMYK print files. See `research/print_reference/PRINT_FORM_SYSTEM.md` §10.

### 1.2 Layout rules

- **Page:** 792 × 612, side margins 36, text measure 720. The header band, the logo badge and the ghost numeral stay as the playtest form has them (the base belongs to Prints).
- **Columns (SD tables):** starts at x 36 / 64 / 188 / 256 / 474 / 592. Every gutter is 12 pt: text widths are ID 20, System 112, Status 56, What it does 206, Owner · Needs 100, Your decision 164.
- **Rows:** one 14 pt grid. Every column's first baseline sits at row top + 16:
  - text top = row + 5 for Cell and Row head;
  - row + 5.5 for Cell small;
  - row + 6 for ID;
  - the tag box sits at row + 6, centred on the cap band.
  - Row height is 38 for up to two lines and 10 + 14 × lines above that, so three lines are always 52.
- **Zebra:** every second row #F4F4F3. The bands, the head rule and the end rule hang 6 pt outside the text (x 30, w 732); the text stays on the 36 margin.
- **Status tags: three classes, one look each.** The word gives the exact state.

| Class | States | Look |
|---|---|---|
| In the game | LIVE, PART | ink fill, paper text |
| Built, not in play | BUILD, CLONE, OFF | white, 1 pt ink outline, ink text |
| Not built | DESIGN, OPTION, NONE | no fill, 0.75 pt dashed #4A4A4A outline (2/2), grey text |

- **Your decision:** the decision cell is filled with the yellow (x 586–762, from the text top − 1 to the text bottom + 1), with ink text. It is the same yellow as the "N DECISIONS FOR YOU" tags. A closed decision has no fill and grey text. There is no thin yellow bar.
- **Footer key:** drawn, never written out. A 0.75 pt ink rule at y 566, then two rows at y 573 and 589: the real tags, each followed by its definition in Info / Sheet / Footer; then the yellow swatch "your decision" and a grey "Decided" sample, "closed decision".
- **Numbering and voice:** one ID per sheet. The header reads `SD04 · Sheet 5 of 9 · <date>`, right-aligned to x 640; the ghost numeral shows the same group. A sheet addressed to Red says "you" ("5 decisions for you", "Your decision").

### 1.3 Tools

- `~/FrontRoomsVisualWork/info_type/sd_restyle.js` applies §1.2 to the SD01–SD08 table sheets in one `use_figma` call. It re-flows rows on the 14 pt grid, is re-runnable, and skips any frame that already holds Info tags. It was tested on copies of SD01, SD03 and SD06 on 2026-10-15. It does not handle SD00 (the map).

### 1.4 Large format: the A0 wall sheet (added 2026-10-15)

Red rejected the Letter sheets as still "像ppt" and asked for "真的大画幅的细密网格的信息设计". 系统设计 rebuilt the system design table as one A0 sheet (frame `3234:7322`).

The large-format profile keeps every Info style at its **absolute** size; nothing is scaled up. A0 adds navigation levels above the Letter ones, and a wider seam between zones.

**Page and grid**
- **Size:** A0 landscape, 3370 × 2384 pt (1189 × 841 mm). 1 pt = 1 px, read at 100 % or printed 1:1.
- **Margin:** 48 pt (17 mm).
- **Columns:** 24, with 12 pt gutters (column about 124.9 pt). Baseline 14 pt. Both are set as layout grids on the frame.
- **Spine:** the left block (cols 1–13) and the right block are separated by one empty column.
  - The right block runs cols 15–24.
  - A 0.5 pt ink hairline runs down the spine's centre, from the header rule to the footer rule.
  - Zones never meet across a single 12 pt gutter.
- **Zone spacing:** 2 baseline units (28 pt) of white above every zone head; zone tops sit on the baseline grid.

**Size ladder**

| Level | Style | Use |
|---|---|---|
| Sheet | **Info / Wall / Title**, Bayon 120/104, −1 % | The sheet title, once, top left. Ink |
| Zone | Info / Sheet / Title, Bayon 32/32 | Zone letter + name (`A  System map`). The letters A–H are safe in Bayon |
| Zone question | Info / Table / Cell, 11/14, #4A4A4A | The one question the zone answers, on the zone head's baseline row, measure ≤ 6 columns |
| Group | Info / Sheet / Label, Bayon 11/12, caps | The group name only. Group numbers go in Info / Table / ID (Plex Mono), because Bayon draws 0 and 1 as O and I |
| Cells, IDs, tags, meta | Letter Info styles at their own sizes | Unchanged |

**Rules and arrows**
- Hairlines: 0.5 pt for table-internal rules and the spine; 0.75 pt ink for head rules and zone rules.
- Arrows: 0.75 pt ink across groups and 0.5 pt #8A8A8A inside a group, ARROW_LINES heads.
- Arrows are routed orthogonally in gutters and never cross a card. If a network is too dense for that, it becomes a matrix (DSM): a grid of 10 pt cells, with dots where a row needs a column.

**Labels on diagrams** (state machines, lanes):
- Use Info / Table / Tag (Plex Mono 7/8 caps), on one line, on a paper knockout that interrupts the line.
- Widen the gap rather than wrap the label.
- On fills: ink text on fills lighter than #B0B0B0, paper text on fills of #4A4A4A or darker. **No text on mid greys.**
- A lane ramp therefore uses #EEEEEE / #C4C4C4 (ink text) and #4A4A4A / #0A0A0A (paper text).
- An axis always names its unit.

**Key:** drawn once, in the header band next to the title (top right), with real tags and swatches. Never repeated as a sentence.

**Numbers:** any numeral in a display role (step numbers, group numbers) is set in Plex Mono or Courier Prime, never Bayon.

### 1.5 Screen profile: the System Atlas (added 2026-10-15)

Red, 2026-10-15, on the A0 sheet: "比之前好很多但是目前的readability不是特别好", and the system visualization moved to 信息设计. 系统设计 keeps the data (`system_design/systems.json`, `views.json`); every visual of it is built here, from the files, never typed by hand.

**What it is:** an interactive page, **System Atlas**, https://claude.ai/artifact/FEPpCB6X8syv17wWXE1P3y (private to Red until shared). The A0 sheet (rev 2, `3239:126`) is no longer the live view; it stays as it is unless Red asks for a print version.

**Why a page and not a bigger poster** (checked in VL289 / VL290):
- At zoom-to-fit on a 1440 × 1080 screen the A0 sheet renders its 11 pt cells at 4.4 px and its tags at 2.8 px. At 100 % the same screen shows 19 % of the sheet. Overview and detail never meet at one zoom.
- One sheet answered nine questions with no reading path; the same 52 systems appeared three times, matched by eye.
- The page answers one question per view, links the views (pick a system once, it lights up everywhere), and puts Red's decisions first.

**Build and update**
- Source: `~/FrontRoomsVisualWork/info_type/atlas/atlas_template.html` (form) + `build.py` (inlines the JSON, lints it, writes `out/system_atlas.html` and a preview `out/index.html`).
- Preview: launch config `system-atlas` (port 8796, course-folder `.claude/launch.json`). Shots: `shoot.sh <name> <w> <h> [dark] [hash]` (headless Chrome, 2×, own profile); phone shots go through `out/phone.html` (a 406 px iframe: headless Chrome will not open a window that narrow).
- Publish: copy `out/system_atlas.html` to the session scratchpad as `system_atlas.html` and publish that path; after a reboot (the scratchpad is wiped), publish with the artifact URL instead.
- Protocol: 系统设计 edits the JSON, bumps `updated` and tells 信息设计; 信息设计 runs `build.py`, reads the lint, checks 1440 light / dark and 406, republishes, and logs a VL row only when the form changes.

**Views, in reading order** (each answers one question)

| Section | Question | Form |
|---|---|---|
| Your decisions | What do I decide, in what order? | 7-step strip (status bar + counts per step), then one card per open decision under its landing step: C-tag, question, suggested answer, the system, how many systems depend on it (transitive); then the decided list, newest first (A-tags) |
| The eight groups | How far along is each group, and what is in it? | 8 cards on the position hints (4 × 2 → 2 → 1 column); stacked status bar; system rows with decision tags; relation tags on demand; group × group link counts |
| Who needs whom | Every dependency at once | 52 × 52 matrix, row needs column, 15 px cells, group blocks, hover ruler, selected row and column ringed |
| Ledger | Every system in one list | sortable, searchable table; filters by status class, group, with an open decision |
| Mechanics | How the Relay and the player meet | encounter lanes on one time axis, with one line per lane on what its tone counts; Relay states (escalation down the left, the way back up the right); who knows what (layers + flows); what each action costs (noise ladder) |
| Terms | What do the words mean? | the glossary, one card per term |

**Type at screen sizes** (Info faces; nothing below 11 px)

| Role | Setting |
|---|---|
| Page title | Bayon 52–88 px (clamp), line 0.9 |
| Section | Bayon 36 / 36 |
| Card, panel, group heads | Bayon 23–27 |
| Running text | Source Serif 4 16 / 24; lede 20 / 28; section questions 18 / 25 |
| Decision question | Source Serif 4 SemiBold 18 / 23 |
| IDs, counts, labels, axes | IBM Plex Mono 11–13, caps labels +6–8 % |
| Tags (decisions, relations) | IBM Plex Mono Medium 11 |

**Colour** (tokens; the dark theme redefines every one)
- Light: page #F4F4F3, panels #FFFFFF, ink #0A0A0A, grey #4A4A4A, quiet #6E6E6E, rule #C4C4C4, hairline #E4E4E2. The print "closed" grey #8A8A8A is 3.45:1 on white, too weak for small screen text, so the screen's quiet grey is #6E6E6E (5.1:1).
- Dark: page #0A0A0A, panels #151514, ink #F4F4F3, grey #BDBDBA, quiet #8E8E8B (5.6:1), rule #3A3A38.
- Yellow #F4DF3B means an open decision for Red and nothing else; an answered one takes an outlined grey tag. Selection is an ink ring; hover is a tint.

**Encoding**
- Status: one mark per class everywhere, marks and bars alike: ink fill = in the game, ink ring = built, dashed = not built. The exact state is a word in the ledger and the detail, never the only signal.
- Relations: on demand, as tags on the rows themselves: `→ B2` (B2 needs it), `B2 →` (it needs B2), `⇄ B2` (both). **No lines across cards:** in the first build, lines with a paper casing erased letters under them, and short hops became hooks.
- Lanes: tone = intensity 0–3 on #EEEEEE / #C4C4C4 (ink text) and #4A4A4A / #0A0A0A (paper text), mirrored in dark. A label that does not fit drops below the lane on a leader; on a narrow screen the timeline keeps its 1100 px width, scrolls inside its panel, opens where the first non-calm segment starts, and labels slide to the visible edge (a label that cannot fit hides; no fragments).
- Noise ladder: bar = metres of walking path on one 0–60 m scale; ink = also raises Attention, grey = only the Relay hears it, arrow = calls the Relay outright. The cell text stays beside the bar.
- Digits never go in Bayon: the page sets any digit on a display line in Plex Mono.
- **Words:** "call" is the game's term (the director sending the Relay, as the glossary defines it). Red's open items are **decisions** on the page; their C ids stay. The idiom "your call" is never marked as the term.
- **Terms in place:** a glossary term's first use in each view is a button with a dotted underline; hover or focus shows the definition (360 px card, kept inside the screen), a click or tap pins it, Esc or an outside click closes it. The Terms section lists them all.
- **A suggestion is the answer, not the argument:** the card shows `recommendation`; reasoning goes in `rationale` and folds under a "Why" toggle (the drawer shows it in full). A recommendation over 120 characters without a rationale is split at its first sentence, and the lint notes it (C10, data 2026-10-15d).
- **Noise cells are data** (since 2026-10-15c): bar = `noise_m`; ink = `attention` > 0, grey = `relay_only` or 0, arrow at the bar's end = `calls_relay`; `noise_m_by_tier` draws an outline to the highest tier; `note` sits under the cell text.

**Layout:** content max 1360, gutters 24 (16 on phones), a sticky bar with the sections and the key. The page body never scrolls sideways; only the matrix, the ledger (≥ 1040), the Relay states (564) and the timeline (≥ 1100) scroll, each inside its own panel.

**Fields from 系统设计:** all four delivered in data 2026-10-15c (segment intensity + per-lane `intensity_note`; structured noise cells; a 10-term `glossary`; decisions with `status` open | answered, `answer`, `date`). Checked in VL291. C10 was reworded by 系统设计 (data 22z9).

**Shots (pitfalls):** headless Chrome paints nothing below ~16k device px (a 1440 × 13300 window at 2× came out blank under the ledger) and nothing after a scroll before capture. Shoot regions through demo copies (`out/demo_*.html`, never published) that pull the content up with a negative top margin and, if needed, pin a definition.

## 2. HUD (current values; owner of motion and keys: see `research/ui_key_icon/lock_motion/README.md`)

| Element | Desktop (1080p px) | Phone (touch ramp, pt) |
|---|---|---|
| Prompt line 1 | Bayon 20, paper, baseline 591 | Bayon 17, baseline 239 |
| Prompt line 2 | IBM Plex Mono 13, paper, baseline 614 | IBM Plex Mono 11, baseline 259 |
| HUD numbers (key tag) | Courier Prime Bold 25 | Courier Prime Bold 21 (Touch / Phone / Key numeral) |
| Room name | Source Serif (game: Source Serif 4, opsz 20) | Touch / Phone / Zone, Source Serif 4 26/26 |

Rules:
- **No dark cards.** Legibility comes from a 1 px ink keyline, #141414 at 85 %: Unity `Outline` on text (distance 1, × the touch scale on phone), and an ink copy 1 px larger behind every paper part of a sprite, in one 85 % group.
- **Motion comes out of an element already on screen** (the crosshair dot) and goes back into it. It is never an overlay.
- **No translucent ink or surfaces in in-game UI.** Unity blends UI alpha in linear light. Figma equivalents measured over game scenes: 90 % → 77 %, 75 % → 58 %, 97 % → 85 %.
- Unity legacy Text needs whole-number sizes.
- Bayon has no `·`; set it in IBM Plex Mono. Bayon draws 1 and 0 as I and O, so numbers go in Courier Prime or Plex Mono.

## 3. Terminal (open)

- The spec is `research/computer_ui/20_design.md` §8 (系统设计): 640 × 480, 40 × 15 cells of 16 × 32, header and footer rules.
- **Open for Red: C20 (face and colour, was D10) and C19 (pager or menu, was D11).** The G0 mocks are done (Figma `3218:7277`, TM01–TM04; `GRAPHIC_VISUAL_TASKS.md` §3h).
- **Time and dates (2026-10-22):** the header clock is live (`h:MM AM`, no seconds, no blinking colon). Every date is computed from the live date by 叙事's rule (`research/station/01_research.md` §5): past dates this month or earlier, rolling into 1989; future dates print no year. Generator `W/info_type/terminal/g0_pages.py`. The Figma masters take the new date renders with the C20 re-render.
- Not the glow-ink yellow-green: one colour keeps one meaning.

## 4. Signage

- **EXIT:** geometry lettering, not a font; colour approved 2026-10-15 on exit_sign_fix1 (`GRAPHIC_VISUAL_TASKS.md` §3f).
- **Stairwell (C22) and first room:** stencil floor tokens and two-ply engraved plates in Heros Bold, from `W/info_type/stair_signs/stair_signs.swift` (SPEC v1.2, VL371). The first room's BUSINESS HOURS plate is 18 × 6 in (Red 10-22 15:33); its desk front carries SECURITY in brushed-aluminium letters (`out/first_room/`, README).
- **Placard legends:** constants in `placard.swift`.

## 5. Log

- 2026-10-22: one word for Red's open items, "decision", everywhere in §1 (the swatch is now "decision yellow", same #F4DF3B), and in IT00/IT01 in Figma (3190:7123, 3190:7133, 3190:7267, 3190:7269, 3195:7202; the footer key row moved 18 pt right). "Call" is kept only for the director sending the Relay.
- 2026-10-22: §3 terminal (G0 done; live clock and computed dates; C19/C20 open) and §4 signage (EXIT colour approved 10-15; stair and first-room signs).
- 2026-10-15: §1.5 updated for data 2026-10-15c: decisions (not calls), terms in place, decided list, noise cells as data; VL291.
- 2026-10-15: §1.5 screen profile: the System Atlas (interactive page from 系统设计's JSON), after Red's readability note on the A0 sheet; checks VL289–290.
- 2026-10-15: §1.4 large format (A0) added from the audit of 系统设计's A0 sheet `3234:7322`; new style Info / Wall / Title.
- 2026-10-15: §1 created from the audit of 系统设计's SD00–SD08 sheets (`3171:7042`): 12 local styles, the Info / Status tag set, the IT00 and IT01 sheets, and the restyle script.
