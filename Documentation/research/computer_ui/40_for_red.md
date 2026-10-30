# Computer screens (EXAM): the mock, and C19 / C20 for Red

> **ANSWERED 2026-10-30 10:59 (Red, 游戏视觉 chat):** C19 = "在这里选择在那个时代的电脑操作系统所使用的交互方式" — use the interaction of the era's real office computers (c. 1990), not the simplified page-turner. The exact convention (DOS text-mode app / terminal: menu bar, highlight bar, function-key footer, PgUp/PgDn …) and its mapping to desktop / phone / pad is specified in `41_c19_period_interaction.md`. **C20 ANSWERED 10:59 (Red, same chat, quoting option 1):** we draw our own 8×16 bitmap face (no licence). Because C19 copies a real c. 1990 OS, the glyph set may grow beyond the ~50 capitals (lowercase, digits, box-drawing for menus/borders); 41_c19_period_interaction.md sizes it. **Q3 ANSWERED 10:59 (Red, quoting the recommendation):** readable terminals show the E·1 eye; dark and still desk screens never do.


*游戏视觉, 2026-10-30. Docs and images only: nothing in this folder changes the game. The design is `20_design.md` (系统设计, v1.2); this page shows it on today's main and asks the two open rows.*

**Figma:** section `3769:8636` "FRONTROOMS · COMPUTER SCREENS (EXAM) · FOR RED" (file 0tCbAiVUlrPId3RWd9LRif, page 2099:76, at x 7897, y 42700). Seven slides:

| Slide | Node | Shows |
|---|---|---|
| EC00 Read it where it stands | `3769:8637` | (a) walking up on desktop: 3.0 / 2.4 / 1.5 / 0.99 m, with 1:1 pixel crops; the eye E·1 and `E · READ SCREEN` |
| EC01 E: the close-up | `3769:8638` | (b) the close-up with the transcript panel on the right |
| EC02 One 1990 program | `3769:8639` | (c) the front page and three page types through the tube |
| EC03 On a phone | `3769:8640` | (d) the phone glance (USE = READ) and the near-full-bleed phone close-up (updated 10-30, below) |
| EC04 C19: turn pages, or a menu | `3769:8641` | C19 side by side, options, cost, recommendation |
| EC05 C20: which 8 × 16 face | `3769:8642` | C20 faces side by side, options, cost, recommendation |
| EC06 Who builds what | `3769:8643` | the lines, their days, and the third question (the eye) |

**Update 2026-10-30 11:1x: EC03 follows Red's 11:00 feedback** (交互设计 chat, a human message: "手机上的交互这里就不用显示一旁的这个transcribe了，尽可能让电脑屏幕直接圈屏幕full bleed").
- 交互设计's rule for the phone terminal close-up: no TEXT chip and no transcript panel (other close-ups keep TEXT); the 4:3 picture spans the full screen height, centred, never cropped to fill the width, so the header and footer lines stay; the side bands are the tube's dark glass; BACK is the one overlay. The desktop is unchanged.
- Re-shot in one run (clone `W/proj_examcrt`, `-ecMode fullbleed`): the picture is 1206 px tall (all of the screen's height) and 1609 px wide on the iPhone 16 Pro (x 506–2115 of 2622, centred). Main's close-up does not frame this yet, so the harness set the camera for the capture (0.265 m from the glass, FOV 42.2°). The bands outside the picture take the glass colour.
- The old EC03 (with the TEXT panel) is kept at `W/_partial/exam_computer_ec03_1030/`.
- **v3, 11:1x:** Red looked at the full-height shot and said "这里full bleed上下边缘可以保留一点点" (keep a little margin at the top and bottom). Re-shot in one run (`-ecBleed 0.95`): the picture is now **1146 of 1206 px tall (95 %)** and 1529 px wide (x 546–2075), centred, with **30 px of the tube's dark glass above and 30 px below**. The sides, BACK as the only overlay, and no TEXT chip or panel are as before. The full-height version is kept at `W/_partial/exam_computer_ec03_1030/EC03_phone_fullheight_1030-1108.jpg`.

**Images** (Shot format, `W/tools/shotframe`), in `research/computer_ui/images/`: `EC00_walk_up_desktop.jpg`, `EC01_closeup_desktop.jpg`, `EC02_three_pages.jpg`, `EC03_phone.jpg`, `EC04_c19_pager_vs_menu.jpg`, `EC05_c20_faces.jpg`.

## 0. What to answer

| # | Question | Recommendation | Blocks |
|---|---|---|---|
| C19 | Page-turning, or a menu with a cursor? | **A: page-turning** (as systems.json recommends) | Phase B: paging, the page model, page input, the front-page and footer art |
| C20 | Which 8 × 16 face? | **1: draw our own** | All page art, including the front page that Phase A shows |
| Q3 | Does a terminal show the look-eye E·1? | **Yes** on terminals; dark or still desk screens never | 系统设计's grammar line and its "no eye on decoration" test |

One line is enough, for example "C19 A, C20 1, eye yes" or "全部按推荐".

## 1. Background

**Your asks (checked in the transcripts):**
- 2026-10-09 01:57Z (95375b1f): when the player walks up to an interactable computer, they can read its screen directly, and the computer has its own 1990 office UI.
- 2026-10-15 01:13:37Z: you chose "就地读 + E 看": read it in place, and press E to look closer. The close-up pauses the game. The menu-or-pages question (D11) waited for a mock.
- 10-15: the crosshair dot opens into an eye on things E can read (E·1).

**Where it stands today (main, 10-30):**
- Only the design exists (`20_design.md`). No desk computer can be read; desk screens show the ad or, on a floor with a station, a still dark frame (V4).
- What the feature will stand on is already in main:
  - the CRT tube (crt_tube, 10-29), with your 276 × 207 mm desk picture and its dark-glass border;
  - main's EXAMINE close-up, with the phone TEXT chip and panel (10-24);
  - the key HUD's look-eye E·1 (HUD B, live 10-29);
  - the Relay v2 sight tests the READ gate needs (V2, 10-29).
- The audit row EXAM-COMPUTER (总控, 10-29) asked for a mock before you decide; systems.json C19 and C20 are still open.

**How the mock was made (so you know what is real):**
- Real, from main: the map (seed 4242, run 516574485), an Office desk with its CRT, the tube, the lighting, the HUD, the eye and the prompt, the EXAMINE close-up and its camera, the phone touch HUD, and its TEXT panel.
  - A clone-only harness (`W/proj_examcrt`, `Assets/Editor/ExamComputer/ExamComputerMock.cs`; never merged) puts a page on one desk's screen.
  - It gives that screen a READ target built exactly as main builds the station TV's.
  - The game's own aim, E and close-up do the rest.
- Mocked:
  - the page art (`W/exam_computer/pages.py`);
  - the desktop transcript panel, drawn from 平面视觉's G9 spec because 关卡设计 has not built it (C24);
  - Setup A, only in that the chair in the stand box is hidden. This desk already had its monitor on the PC case, at 1.128 m.
- The strings come from existing lore only, as drafts for 叙事设计:
  - R.M. (facilities) and D.K. (the night desk), from `research/station/01_research.md` §6;
  - `WALK, DO NOT RUN.`, the floor warden line, STAIR A, the B levels;
  - the drill-notice print job 叙事 offered in `research/screens/30_screen_strings.md` §6.
  - Dates follow the live calendar: memos dated this month and four months back in 1990; the next drill gives a month and day, no year.
  - `INTEROFFICE` is 信息设计's G0 placeholder name. No desk user is named, because 叙事's rule is initials only and no new people.

## 2. What the mock shows

- **Walking up (EC00).**
  - At 3 m the terminal is a lit blue field on a desk.
  - At the 2.4 m aim reach the headline reads, and there is no prompt yet.
  - Inside 1.6 m the dot opens into the eye and `E · READ SCREEN` shows. Both front-page lines read there.
  - This matches `20_design.md` §2.3.
- **The close-up (EC01, EC02).**
  - E pauses the game and frames the screen at 0.251 m, with the picture at 70 % of the height (§2.4).
  - With transcripts on, the CRT moves into the left 64 % and the words sit on the right.
  - Body text is clear on desktop.
- **The phone (EC03).**
  - USE says READ and the eye shows.
  - The headline reads from the stand box; the second line needs the close-up (§2.3).
  - In the close-up the picture fills about 95 % of the screen's height, with a thin dark-glass margin top and bottom, BACK as the only control and no transcript beside it (Red, 10-30 11:00 and 11:1x).

**Things the mock found (no decision needed from you; they go to the owners):**
1. **The eye sits on the second line.** Aimed dead centre, E·1 covers part of `2 UNREAD`, on desktop and phone. Aiming a little lower reads it. 平面视觉 may lift that line a row in G1.
2. **Lamp reflections on the glass sit on the letters in the close-up** (the white spots in EC01 and EC02). The design already has a reading preset for close-ups (V4) that tones them down. It is not built.
3. **The close-up needs the terminal's own size.** Main's screen target uses the ad quad's 0.28 × 0.19 m. With that size the picture overflowed the frame (first run). The mock used the design's 0.276 × 0.207 m and the §2.4 distance, and then it frames correctly (V1, §9.4).
4. **The desktop transcript panel is not in main** (C24, 关卡设计). The phone panel is.

## 3. The questions

### C19 · Turn pages, or a menu with a cursor? (`20_design.md` D11)

systems.json C19, exactly: question "Terminal: page-turning, or a menu with a cursor?"; recommendation "Page-turning in v1; a cursor menu adds about 6 days (mock TM01, Figma 3218:7277)".

**Background.**
- In both options the glance is the same: the front page.
- The question is what you do after E.
- EC04 shows both on the real CRT.

**Option A · turn pages (recommended).**
- How it feels:
  - E opens the screen on its front page, which lists what is inside without numbers.
  - A / D or the arrows flip one page per press, like a booklet. On touch, you tap either half of the screen or a PAGE chip.
  - There is nothing to aim and nothing that looks as if you could type.
  - Leaving puts the screen back on its front page. E reopens on the page you were reading.
- Cost: in the plan. About 37 working days for the whole feature (§15.3's 39, minus the 2.25 days of EXAMINE touch items that landed 10-24; ESTIMATE).

**Option B · a menu with a cursor.**
- How it feels:
  - A numbered list with a highlight bar. ↑ / ↓ (or a tap on a line) moves it, ENTER opens a memo, and BACK returns to the list.
  - It feels more like operating the PC, but every memo costs two more steps.
  - The footer has to name a key or show a key shape (ENTER) that touch players do not have.
- Cost: about 6 more days (§15.3: cursor layer 2.5, input fields 1, game routing 1, layouts 1, tests 0.5). It also adds a tap-to-select layer on touch.

**Recommendation: A.**
- One new verb works the same on desktop, WebGL and touch.
- The cursor layer (§13 E1) can still be added later without redoing anything.

**What it blocks:**
- Phase B (READ) build: paging and the page model (游戏视觉 V1, V2);
- the front-page index and footer art (平面视觉 G1);
- page input (关卡设计 L3; touch TT0, TT1).
- Phase A (the glance) does not wait on it.

### C20 · Which 8 × 16 face? (`20_design.md` D10)

systems.json C20, exactly: question "Terminal face: our own 8 x 16, a permissive one, or Px437?"; recommendation "Not Px437 in v1".

**Background.**
- The screen is a 640 × 480 VGA raster on a 40 × 15 grid of 16 × 32 cells. An 8 × 16 bitmap face fits it at whole pixels: 2× for the body, 9× for the second line, 12× for the headline (信息设计's G0, §8.7).
- The interim face, VT323 (OFL, already in the project), is a DEC terminal face. It does not sit on the grid at whole pixels, and it lacks the arrow and block glyphs.
- EC05 shows a page in a quick stand-in for option 1 (FR816, about 50 capital glyphs drawn for this mock) next to VT323, with 3× crops.
- Px437 is not shown: its font pack would need a download.

**Option 1 · draw our own (recommended).**
- How it looks: a period IBM-style PC face, crisp at every size because every size is a whole multiple of its pixels.
- Cost: 平面视觉 draws about 50 capital glyphs: A–Z, 0–9, and the punctuation the strings use (`. , : / - ' &`). Arrows and blocks are drawn as shapes anyway. About 1 day with the G1 coverage check (ESTIMATE, ours). Lowercase only if 叙事 wants mixed-case memos (about +0.5 day).
- Licence: none. Nothing to credit; it is ours.

**Option 2 · a permissive 8 × 16 (OFL, MIT or BSD).**
- How it looks: like option 1, and like other retro games that use the same face.
- Cost: no drawing. The licence check 平面视觉 started on 10-15 has not reported, and no candidate is named yet.

**Option 3 · Px437 IBM VGA 8 × 16 (CC BY-SA 4.0).**
- How it looks: the most authentic IBM VGA look.
- Cost: credit in the game, plus a review of the share-alike terms before it ships. systems.json: not in v1.

**Recommendation: 1.** It costs about a day, has no licence question, and the stand-in shows the look already works on the tube at every distance tested.

**What it blocks:**
- all page art (平面视觉 G1), including the front page that Phase A (the glance) shows;
- the string-fit check (G1, G2);
- so it blocks the start of Phase A.

### Q3 · Does a terminal show the look-eye? (raised by the mock)

**Background.**
- On 10-29 you decided (R3, "全部按推荐") that among screens only the playing station TV shows E·1; dark maze screens never do.
- A terminal is a READ target. The crosshair grammar's one rule is "the eye shows exactly when `E · READ SCREEN` shows" (`30_crosshair_grammar.md` §1), so a terminal would show the eye, as in EC00 and EC03.
- Your own 10-15 example was a screen: "当准星移动到电视的时候，准星会变成眼睛眨眼" (95375b1f 10-15T01:13:37Z).

**Options:**
- **(a) Yes on terminals, recommended.**
  - How it feels: the dot opens into the eye on the one lit blue screen in an Office zone, as it does on the evacuation plan and the station TV.
  - Every dark or still desk screen stays plain.
  - Cost: none; it follows from the READ target. 系统设计 adds terminals to the §1 exception line.
- **(b) No eye on terminals, prompt only.**
  - The prompt line still says `E · READ SCREEN`.
  - The eye then means "the plan or the station TV" only.
  - It splits one predicate into two, which the grammar avoids.

**What it blocks:** only 系统设计's grammar text and the "0 eyes on decoration" test list. Nothing else waits on it.

## 4. Who builds what

Days are `20_design.md` §15.3 ESTIMATES. The terminal keeps the agreed split (owners as in §15.1).

| Line | Builds | About |
|---|---|---|
| 游戏视觉 | The terminal page on today's tube (its own 276 × 207 mm picture; V3), the page renderer, deal and page memory (V2), the EXAMINE paging extension with the terminal's size and framing (V1), the close-up reading preset (V4), Setup A at the host desk (on the PC case, +3 cm, no chair, the stand box; V5), and the captures (V6). Part of V3 may come in under, since the tube and the 4:3 desk picture landed 10-29 | 14.5 d |
| 平面视觉 (信息设计) | The screen UI art and type: the face (C20), field colour (VGA blue recommended), header, FRONT / TEXT / LIST templates, drawn arrows, the TELL frame (G1); the envelope check (G2); ambient-frame rules (G3); the HUD words `A D · PAGE`, the PAGE glyph and the tube preset approval (G4) | 4.5 d (G0 done) |
| 叙事设计 | Every string (N1–N5): the program name, the desk user, headlines and second lines, the memo pool (8 to start; no door advice before RELAY step 4), the print job, and the era and lore audit. This mock's strings are drafts for it | 2 d |
| 关卡设计 | Which desks are interactable (L2): one terminal per Office zone, in the chunk holding the zone's key, at the station nearest the zone site, never in the start area, the title stream or a pile; a 100-seed report. Also page input in the close-up (L3), the READ gate on the Relay's perceivable threat (L4), the desktop transcript panel and the screen-effects setting (L5), and the tests (L6) | 9.75 d |
| 交互设计 (touch) | `PageStep` input, the PAGE chip and the screen-half taps (TT0, TT1). EXAMINE's touch items T1, T2, T6 and T7 landed 10-24 | 2.5 d |
| 声音设计 | A page-turn sound through the audio contract, never in a Relay cue family (S-T1); the blow caption radius check (S-T2) | 1.5 d |
| 系统设计 | Spec upkeep, the tests, and the grammar line if Q3 is yes (R1) | 1.5 d |

**Order:**
- After C20, Phase A (the glance) takes about 12 days: L2 + V5, then V3, V2, G1–G3 and N2.
- After C19, Phase B (E · READ) follows on desktop, WebGL and touch together, because EXAMINE and its touch items are already in main.

## 5. Files

- Pages and face stand-in: `W/exam_computer/pages.py` → `W/exam_computer/out/{desk,phone}/*.png`, `out/transcripts.tsv`.
- Captures: `W/exam_computer/caps/{desk,phone}/` (each with `log.txt`; runs desk1 to desk3 and phone1, 10-30 10:37–10:48, rc 0, 0 failures).
- Images: `W/exam_computer/compose.py` → `research/computer_ui/images/EC0*.jpg`.
- Harness: clone `W/proj_examcrt` (main working tree 10-30 10:3x, Library from proj_crttube), `Assets/Editor/ExamComputer/ExamComputerMock.cs` + `Assets/ExamComputerHarness/ExamComputerBoot.cs`; clone-only, never merged.
- Earlier mock: 信息设计's G0, Figma `3218:7277` (TM01–TM04), `W/info_type/terminal/g0_pages.py`.
