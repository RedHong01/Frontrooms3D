# Office terminals · C19: the era's own interaction (INTEROFFICE)

*游戏视觉, 2026-10-30, account A, session local_5656cffd (C19 spec agent). Docs only: no game code, no Unity, no sound. It follows `20_design.md` (系统设计, v1.2), `30_crosshair_grammar.md` and the mock in `40_for_red.md` (Figma `3769:8636`; work files `W/exam_computer`). Strings shown here are drafts from existing lore; 叙事设计 writes the real ones. Type, sizes and colours are 平面视觉's to set; this page gives the period rules and the envelopes they must meet.*

**Red's decisions this page builds on (2026-10-30, 游戏视觉 chat; C19 verbatim):**
- **C19:** "在这里选择在那个时代的电脑操作系统所使用的交互方式". Use the interaction of the era's real computer operating systems, not our page-turner (A) or an invented cursor menu (B).
- **C20:** we draw our own 8×16 bitmap face (no licence).
- **Q3:** readable terminals show the E·1 look-eye; dark or still desk screens never do.
- **Phone close-up (交互设计 chat, 11:00 and 11:1x):** on a phone the terminal picture fills about 95 % of the screen height, BACK is the only overlay, and there is no TEXT chip or transcript panel (EC03 v3, `40_for_red.md`).

**What this page changes in `20_design.md`** (系统设计 owns that file and applies the changes; nothing here edits it): §7.1 and §7.3 (controls), §8.1 (the shell: menus, selection, a function-key area), §8.2 (the raster: VGA text mode 80×25, not 40×15 cells), §8.4 (panel types), §11 (key sounds and the refusal sound), §12 (hints), §14 (the "F-key menus" rejection is narrowed), §15 (sizes). §6 (Relay rules), §5 (the close-up pauses) and the glance rules stay as they are.

---

## 0. The short version

**The convention: a DOS text-mode program in a 1990 office, run the way IBM's Common User Access (CUA, 1987) and the in-house "lightbar" menu programs of the time ran it.**
- Era-true: in 1990 most US office PCs ran DOS programs in 80×25 text mode (inferred from sales, §1.1). Windows 3.0 had just shipped (22 May 1990) and the leading office programs were still DOS-only. CUA was IBM's published key rule-set; DOS Shell (DOS 4.0, 1988) and the xBase/Clipper in-house systems of the period used its keys and an inverse-video highlight bar.
- Readable: one highlight bar moved by arrow keys, Enter to open, Esc to back out, and a bottom row that names the keys. No pointer, no typing, no small targets on desktop.
- Fits a building's internal system: in 1990 a facilities office's memo and print program would most likely have been an in-house menu program on the tenant LAN. Its grammar (F1 Help, F3 Exit, Cancel backs up one step, Forward / Backward) is the same one the host systems' panels used, so it reads as "the building's own software" without copying any product.

**The screen.**
- VGA text mode: 80 columns × 25 rows of 9×16 cells (720 × 400), stretched to the 4:3 tube as the hardware did.
- Row 1 is a title line, row 3 the panel heading, rows 4–23 the body, row 24 the message line and row 25 the function-key area.
- Four panels: MAIN (the menu and the glance), DOC (a memo, read with paging), LIST (the print queue) and HELP (the keys).

**One verb set on every device:** MOVE, SELECT, CANCEL, EXIT, HELP, PAGE.
- Desktop: the real keys (↑ ↓ ← →, Enter, Esc, PgUp, PgDn, F1, F3). WASD and the mouse wheel are equivalents, and E still leaves.
- Pad: d-pad, A = Enter, B = Cancel, X = Exit, LB / RB = PgUp / PgDn.
- Touch: tap a line (a second tap opens it), tap the soft keys on row 25, swipe for pages; BACK leaves.
- The function-key area always names the control the player actually has, as the key HUD's `WithUseControl` does: `F3=Exit` on a Windows keyboard, `E=Exit` on a Mac or in a browser, `X=Exit` on a pad, boxed soft keys on touch (§5.2).

**The C20 face:** 95 printable ASCII glyphs (0x20–0x7E, lowercase included) plus 14 CP437 glyphs = **109 slots, 108 to draw, about 3.0 days** (ESTIMATE), against the mock's 50 capitals in about 1 day.

**Cost:** about **47.5 working days** across the lines (ESTIMATE). That is +10.75 on option A's 36.75 (≈ 37), +9.75 on A plus the C20 capitals face, and +3.75 on option B (A + 6) plus that face. Phase A (the glance) is about 15 days. §7 has the split by line.

**Asks to owners (not Red), §8:** O-1 the refusal beep against `20_design` §11's ban on tones (声音设计 + 系统设计); O-2 the 80×25 raster replacing the 40×15 grid (平面视觉 + 游戏视觉); O-3 S becomes "down" on terminals instead of "back" (系统设计 + 关卡设计); O-4 F-keys and Esc in WebGL, proved in a clone (游戏视觉 + 交互设计); O-5 a cap height of 11 rows in the C20 face, so the phone's body text passes 18.6′ (平面视觉).

---

## 1. What a 1990 US office desk actually ran

### 1.1 The desk

- About 37 % of employed US adults used a computer at work in October 1989 (Census CPS), up from 25 % in 1984. [S1]
- IBM's PS/2 (April 1987) sold mainly to corporations, and VGA came with it. VGA became the standard PC display within about three years. [S2, S3]
- In big organisations the screen on many desks was still a host terminal: IBM 3270 (mainframe), IBM 5250 (System/36, AS/400) or DEC VT. IBM's mainframe office system, PROFS (renamed OfficeVision in 1989), had over a million users by the late 1980s. [S4–S6]
- The keyboard was the 101-key Enhanced layout of IBM's Model M (1985; adopted by ANSI in 1988 [S7]): F1–F12 along the top, separate arrow keys, and Page Up / Page Down in the editing block.
- **UNVERIFIED:** I found no 1990 figure for how US office desks split between DOS PCs, Windows, Macs and host terminals. The split below is inferred from sales and software releases.

### 1.2 The candidates

| Candidate | Period facts | How it was operated | Verdict for INTEROFFICE |
|---|---|---|---|
| **MS-DOS 3.3–4.01 text-mode programs** | DOS 3.3 (1987) and 4.0 / 4.01 (1988) are the DOS of 1990; 5.0 is June 1991. WordPerfect 5.1 for DOS (Nov 1989) and Lotus 1-2-3 (R2.2 1989, R3 / R3.1 1989–90) were the office programs; WordPerfect for Windows came only in 1991, and in 1992 WordPerfect still had 85 % of the DOS word-processor market. [S8–S11] | Each program had its own keys. WordPerfect: function keys from a plastic template, F3 = Help, F1 = Cancel, pull-down menus new in 5.1. 1-2-3: `/` opens a one-line menu, ← → move the highlight, the first letter picks, Esc backs up one level. [S9–S11] | The right *platform*: text mode on a VGA PC. But WordPerfect's and 1-2-3's keys disagree with each other and with CUA, and their looks are trade dress. Borrow the grammar, never the product |
| **DOS Shell (DOS 4.0, 1988)** | Shipped with DOS 4. Its Start Programs screen: a title line with the date, the title and the time; an action bar (`Program Group Exit`) with `F1=Help`; an instruction line; a list of programs. [S12] | ↑ ↓ move a highlight bar, Enter starts or opens, F10 and Tab reach the action bar, F3 exits, F1 helps. [S12] | **The model for our layout and keys.** It is a CUA program on DOS, on 1990 office PCs |
| **IBM SAA CUA (1987)** | IBM's rule-set for one consistent interface across PCs, AS/400 and mainframes. OS/2, Windows and DOS programs such as the MS-DOS Editor followed it; WordPerfect is the famous exception. [S13–S15] | F1 Help, F3 Exit, F12 Cancel (Esc on PCs), F7 / F8 Backward / Forward, F10 to the action bar, Enter. A **function-key area** at the bottom reminds the user of the keys and doubles as a touch area for mouse users; its short form shows just Enter, Cancel and Help. Cancel backs up one step; Exit leaves the dialogue. Beeps mark requests that did not do what the user expected. [S13, S15, S16] | **The key rules.** |
| **In-house "lightbar" menu programs (dBase / Clipper)** | Clipper Summer '87 shipped 21 Dec 1987 and 5.0 in 1990. Millions of xBase business programs were built with it from 1985 to 1992, including front ends at banks and insurers. [S17] | `MENU TO`: the arrows move the highlight; Home and End jump to the ends; Enter or a prompt's first letter selects; Esc aborts. A message row shows a line about the highlighted choice (`SET MESSAGE`). [S18] | **The menu behaviour.** This is how a building's own system was built in 1990 |
| **Host terminals (3270 / 5250, PROFS / OfficeVision)** | The standard screen in large companies. 24 PF keys; the ISPF key line reads `F1=Help F3=Exit F7=Backward F8=Forward F12=Cancel`. [S4, S5, S19] | Block mode: the user types an option number on a command line or into fields, then presses Enter or a PF key. Green marks the fields you type into. [S4, S20] | Not chosen. It needs typing (rejected, `20_design` §13 E8), its green field colour is excluded (§8.7), and the kit's monitor sits on a PC case. Its PF-key grammar is the same as CUA's, so nothing is lost |
| **Windows 3.0 (22 May 1990)** | About 4 million copies sold in its first year, against about 17 million PC compatibles sold worldwide in 1989 alone. WordPerfect, Lotus and Ashton-Tate, the leading DOS programs, were absent at its launch. [S21, S22] | Mouse first: a pointer, windows, buttons, pull-down menus. | Not chosen. Rare on a 1990 office desk, and a pointer needs the cursor layer (option B's 2.5 days) plus small targets on phones |
| **Mac System 6 / 7** | System 7 is May 1991. Macs were about 10–15 % of the personal-computer market (undated, UNVERIFIED for 1990 offices). Chicago, its system face, belongs to Apple. [S23, S24] | Mouse only. | Not chosen. A minority on US corporate desks, mouse-only, and its face cannot ship |
| **Touch screens** | The HP 150 (Nov 1983) was a touch-screen MS-DOS PC; its coarse infrared grid was used mainly for cursor positioning and function-key control. [S25] | Touch the function keys on the screen. | Not a 1990 office norm, but a real precedent for **tapping the function-key area** on our phones (§5.4) |

### 1.3 What the period agreed on

Programs disagreed about which key did what (WordPerfect's F1 is Cancel; CUA's F1 is Help). They agreed on the shape:
1. **A highlight bar in inverse video** on the current choice, moved by the arrow keys. [S12, S18]
2. **Enter opens the highlighted choice; Esc backs out one level.** [S11, S13, S18]
3. **A title line and a message line.** The message line describes the highlighted choice or reports why a request failed. [S12, S16, S18]
4. **A bottom row that names the keys**, in the form `F1=Help  F3=Exit`. [S12, S19]
5. **A beep when a request does nothing.** Berry (1988), describing CUA: "Audio beeps are used when users' action requests do not cause normally expected results." [S16]
6. **No pointer and no blinking cursor in a menu.** A text cursor appears only where you can type; menu programs switched the hardware cursor off. [S26]

---

## 2. The choice

**INTEROFFICE is a DOS text-mode in-house program that follows CUA's keys and the lightbar menu, as DOS Shell and the 1990 in-house systems did.**

**Why this one:**
1. **Era-true.** It is what an office PC of 1990 most likely showed: DOS, VGA text mode, 80×25, a highlight bar and a key row (§1.1, §1.2).
2. **Readable.** One choice is lit at a time, and the bottom row tells the player which keys to press. It needs no pointer, so it keeps EXAMINE's close-up exactly as it is (no cursor layer, no hit-testing against a crosshair).
3. **It fits a building's internal system.** In 1990 a facilities memo-and-print program would have been in-house software built on these rules. Its keys match the host panels of the same building, so the "INTEROFFICE" fiction (a tenant office PC with mail, memos and a print queue, `20_design` §8.1, D6 a) gets a believable program.

**Not borrowed.** No product names, strings or trade dress:
- not DOS Shell's `Start Programs` / `Main Group`;
- not WordPerfect's blue screen with a `Doc 1 Pg 1 Ln 1` status line (`20_design` §8.7);
- not 1-2-3's three-line control panel;
- not ISPF's `Option ===>`.
We use the grammar only: the title line, the lightbar, the message line, the `Key=Action` row, and Esc / Enter / F1 / F3.

**Left out on purpose:**
- **The action bar** (CUA's row of pull-down menus, reached with F10). It adds a second direction to navigate, F10 opens the window menu on Windows, and a read-only memo program has almost nothing to put in pull-downs. Later, if wanted: about +2 days across 游戏视觉, 平面视觉 and 交互设计.
- **First-letter and digit selection** (Clipper, 1-2-3). Letters are the game's own keys: W, A, S, D and E.
- **Typing, logins and passwords** (`20_design` §13 E8, §14).

---

## 3. The screen

### 3.1 Raster: VGA text mode (amends `20_design` §8.2; O-2)

| Item | Value | Why |
|---|---|---|
| Mode | **80 × 25 cells of 9 × 16 px = 720 × 400** (VGA text mode; 70 Hz on the hardware, per `10_research.md`) | The office PC's own mode (§1). [S3, S26] |
| Face in the cell | the C20 face is 8 × 16. The 9th column is blank, except that box and block glyphs in CP437 0xC0–0xDF repeat their 8th column so lines join, as the VGA hardware did | [S26] |
| On the tube | the 720 × 400 image is stretched to the 0.276 × 0.207 m (4:3) picture by the quad, so pixels are taller than wide, as on a 1990 monitor | The stretch happens in the quad's UVs, not in the raster, so the raster stays at whole-pixel scales |
| Render path | **a char grid:** an 80 × 25 buffer of (glyph, attribute) bytes, rasterised on the CPU into a Texture2D at **2× (1440 × 800)** whenever a cell changes; mipmaps rebuilt after each change. ESTIMATE ≤ 1 ms per change on an iPhone | It replaces the UI Toolkit render target: no runtime text asset, no theme, no repaint question (`20_design` §9.1's two UNVERIFIED items disappear). The 2× copy keeps the phone's 95 %-height close-up within EXAMINE's 2.0× texel cap (1146 px / 800 lines = 1.43×) |
| Memory | 1440 × 800 × 4 B × 4/3 = 5.9 MiB per live terminal: one on handheld and WebGL, at most two on desktop | Up from 1.56 MiB on handheld. The one-live-RT rule still holds: this texture changes only on a key press or the clock's minute |
| Desktop fill | `20_design` §2.4's rule is kept: fill = min(70 %, 2.0 × 800 ÷ screen lines). It is 70 % up to 4K and 56 % at 5K (was 67 %); the 5K body still reads at about 26′ | The texel cap stays EXAMINE's |
| Big type on MAIN | built from full-block glyphs (█), the only way to show large letters in text mode: the headline 7 rows tall, the second line 5 rows | Keeps `20_design` §8.3's glance envelope (§3.6) |
| A later floor plan (E2) | switches that panel to VGA graphics mode (640 × 480), as real programs switched modes | So text mode costs E2 nothing |

`20_design` §8.2 rejected text mode because 720 × 400 does not scale to a 640 × 480 raster at whole pixels and cannot draw a plan. Both reasons fall away above: the raster is 720 × 400 at 1× or 2×, and the tube's quad does the 4:3 stretch, exactly as the monitor did.

**Knock-on:** 平面视觉 · 广告's still desk frames (`Tools/screens/out/still_desk/`, 640 × 480 on the 40 × 15 grid) can stay. They carry glance-size text only and do not pretend to be a program (AT-T19). 平面视觉 may re-cut them on 80 × 25 later for consistency; nothing waits on it.

### 3.2 Row map

| Rows | Area | Content |
|---|---|---|
| 1 | **Title line** | program name at col 2; panel title centred; live clock right-aligned to col 79 (`h:MM AM`, no seconds, no blinking colon; 信息设计's format) |
| 2 | spacer | — |
| 3 | **Panel heading** (none on MAIN, whose headline starts here) | `Memo 1 of 2`, the queue's line, `Keys`; scroll information at the right end (`More: +`, `More: -`, `More: - +`, the host panels' form) only when there is more to see [S16, S35] |
| 4–23 | **Body** | the panel's content (MAIN: headline, second line, menu) |
| 24 | **Message line** | describes the highlighted choice (Clipper's message row); a refusal replaces it until the next accepted key |
| 25 | **Function-key area** | `Key=Action` pairs separated by two spaces, naming the player's real controls (§5.2) |

### 3.3 The four panels

The mock-ups are exact 80 × 25 cell grids (each row was checked at 80 cells; trailing spaces are trimmed here). The strings are drafts. Inverse cells are listed under each panel.

**MAIN (the front page; also the glance).** It keeps the mock's headline, second line and memo list.

```
             1         2         3         4         5         6         7         8
    12345678901234567890123456789012345678901234567890123456789012345678901234567890
 1   INTEROFFICE                        Main Menu                          10:33 AM
 2
 3             ██      ██  ██████████  ██      ██    ██████      ████████
 4             ████  ████  ██          ████  ████  ██      ██  ██
 5             ██  ██  ██  ██          ██  ██  ██  ██      ██  ██
 6             ██  ██  ██  ████████    ██  ██  ██  ██      ██    ██████
 7             ██      ██  ██          ██      ██  ██      ██          ██
 8             ██      ██  ██          ██      ██  ██      ██          ██
 9             ██      ██  ██████████  ██      ██    ██████    ████████
10
11   ██████              ██    ██  ██    ██  ██████    ████████    ████    ██████
12         ██            ██    ██  ████  ██  ██    ██  ██        ██    ██  ██    ██
13     ████              ██    ██  ██  ████  ██████    ██████    ████████  ██    ██
14   ██                  ██    ██  ██    ██  ██  ██    ██        ██    ██  ██    ██
15   ████████              ████    ██    ██  ██    ██  ████████  ██    ██  ██████
16
17             ■ Memo from R.M., Facilities                     10/30/90
18
19             ■ Memo from D.K., Night Desk                      6/30/90
20
21               Print queue
22
23               Help
24    Read the memo from R.M., Facilities.
25   F1=Help  Enter=Select  F3=Exit
```

- Inverse (the highlight bar): row 17, cols 11–70 (one cell of margin each side), on the first item. Exactly one bar on the panel.
- ■ (CP437 0xFE) marks an unread memo. It clears when the memo is opened, and the second line counts what is left (`1 UNREAD`; at zero, 叙事's word). UI state only, reset by restart (`20_design` §4.1 rules).
- Items sit on every second row (17, 19, 21, 23), so a touch hit is two rows tall (§3.7).
- HELP is the last item (CUA puts Help last), so Help can be reached where F1 cannot be pressed (Mac, browser, pad).
- The headline is 7 rows of full blocks, each "pixel" 2 cells wide (cap 58 mm = 28 % of the picture height). The second line is 5 rows (cap 41 mm = 20 %). Both meet `20_design` §8.3 (25 % and 18 %); 平面视觉 draws the two block alphabets (§6.5).
- The eye sits on the picture's centre, row 13, which is inside the second line again (the mock's finding 1). 平面视觉 may move the second line in G1.

**DOC (a memo).**

```
             1         2         3         4         5         6         7         8
    12345678901234567890123456789012345678901234567890123456789012345678901234567890
 1   INTEROFFICE                        Read Memo                          10:33 AM
 2
 3    Memo 1 of 2
 4    ────────────────────────────────────────────────────────────────────────────
 5    FROM:  R.M., Facilities
 6    TO:    All tenants
 7    DATE:  10/30/90
 8    RE:    Fire alarm procedure
 9    ────────────────────────────────────────────────────────────────────────────
10
11    In a fire alarm, walk; do not run. Follow the posted plan to Stair A.
12    On alarm, a floor warden checks every room.
13
14    Breaking any window on this level sounds the alarm for the whole level.
15
16    Next drill: Tuesday, November 13, at 10:00 AM.
17
18    R.M.
19
20
21
22
23
24
25   F1=Help  Esc=Cancel  F3=Exit  PgUp/PgDn=Page
```

- `Memo n of m` is the panel heading; `More: +` / `More: -` appear at cols 70–79 only when the memo is longer than the body.
- ↑ ↓ scroll one line; PgUp / PgDn (and ← →) scroll one screen. **PgDn at the end of a memo opens the next memo; PgUp at the top opens the previous one**, so the folder reads like a stack of pages (option A's page-turning lives on inside the convention).
- Past the last memo, or before the first: a refusal (§4.3) with 叙事's wording for "bottom of the folder".

**LIST (the print queue).**

```
             1         2         3         4         5         6         7         8
    12345678901234567890123456789012345678901234567890123456789012345678901234567890
 1   INTEROFFICE                       Print Queue                         10:33 AM
 2
 3    Queue: [name]          Printer: [name]          Status: Printing
 4
 5    ┌──────────────────────────────────────────────────────────────────────────┐
 6    │ Job   Document                          Owner     Pages   Status         │
 7    ├──────────────────────────────────────────────────────────────────────────┤
 8    │ 0412  Fire drill notice                 R.M.      2       Printing       │
 9    │ 0413  [form]                            [ ]       1       Waiting        │
10    │                                                                          │
11    └──────────────────────────────────────────────────────────────────────────┘
12
13
14
15
16
17
18
19
20
21
22
23
24    Not authorized: only the owner of a job can change it.
25   F1=Help  Enter=Select  Esc=Cancel  F3=Exit
```

- Inverse: row 8, cols 4–77 (the first job).
- Enter on a job is the one **not-authorised** request in v1: every job belongs to someone else, so the message line refuses (§4.3). Shown here with that refusal on row 24.
- PgUp / PgDn jump to the first / last job (the list fits one screen).

**HELP (the keys).** Reached by F1 (where it can be pressed), the Help item on MAIN, or the Help soft key on touch. Cancel returns to the panel it came from.

```
             1         2         3         4         5         6         7         8
    12345678901234567890123456789012345678901234567890123456789012345678901234567890
 1   INTEROFFICE                          Help                             10:33 AM
 2
 3    Keys
 4
 5    ┌────────────────────────────────────────────────────────┐
 6    │ ↑ ↓         Choose an item                             │
 7    │ ← →         Previous or next                           │
 8    │ Enter       Open the chosen item                       │
 9    │ PgUp PgDn   Previous or next page                      │
10    │ Esc         Back one step                              │
11    │ F3          Leave INTEROFFICE                          │
12    │ F1          This help                                  │
13    │                                                        │
14    └────────────────────────────────────────────────────────┘
15
16
17
18
19
20
21
22
23
24
25   Esc=Cancel  F3=Exit
```

- This is the Windows and Linux keyboard version. Every row names the player's real control, from the same table as the function-key area (§5.2): on a Mac `E` leaves; on a pad the rows read `d-pad`, `A`, `LB RB`, `B`, `X`; on touch they read `Tap an item`, `Tap it again`, `Swipe`, `Cancel`, `BACK`.
- So HELP is the in-world tutorial for the terminal, in the program's own voice. It needs no HUD hint beyond the existing `E · BACK`.

### 3.4 Attributes (colour roles; 平面视觉 sets the values)

Roles follow the 16-colour text palette and `20_design` §8.7's exclusions (no greens, ambers, yellows or reds). Suggested values with their contrast:

| Role | Used for | Suggested (fg on bg) | Contrast |
|---|---|---|---|
| FIELD | body text | #AAAAAA on #0000AA | 5.7 : 1 |
| BRIGHT | headline, labels, messages, the key names in row 25 | #FFFFFF on #0000AA | 13.3 : 1 |
| BAR | **the highlight bar: inverse video**, FIELD's colours swapped | #0000AA on #AAAAAA | 5.7 : 1 |
| TITLE | row 1 | #000000 on #00AAAA (cyan) | 7.3 : 1 |
| SOFTKEY | touch soft keys in row 25 | BAR's colours | 5.7 : 1 |

- The bar is the only grey-backed element below the title line, so it reads as "the choice" without hue (a shape and a luminance swap; `30_crosshair_grammar.md` §5's no-meaning-by-hue rule).
- **No blink attribute and no second highlight.** Unavailable choices (CUA dims them) do not occur in v1: every listed choice works (§4.3).

### 3.5 Cursor

- **The text cursor is off on every panel.** INTEROFFICE has no field to type in, and menu programs of the period turned the hardware cursor off (VGA's Cursor Disable bit). [S26]
- So nothing on the screen blinks. For the record: the VGA cursor's blink is fixed by the hardware at 16 frames per state, about 2.2 blinks a second in 70 Hz text mode. Any other screen that ever shows one (an ambient `C>` frame) must stay at or under 3 changes a second (RELAY §9.5). [S26]

### 3.6 Legibility per class

Method and pass mark from `20_design` §2.3–§2.4: a cap reads at ≥ 18.6′. Body caps are a share of the 400 scanlines.

| Class | Close-up framing | Cap 10 rows (mock stand-in FR816) | **Cap 11 rows (O-5)** | 40 × 15 grid, for comparison |
|---|---|---|---|---|
| Desktop 1080p / WebGL 1080p | 70 % of the height (unchanged) | 29.4′ | **32.3′** | 49′ |
| iPad 11-inch | 70 % (the 800-line raster lifts the old 58.5 % texel limit); 35.0′ at 95 % if 交互设计 applies the phone rule | 23.5′ | **25.8′** | 32.7′ |
| iPhone 16 Pro | **95 % of the full height** (Red, 11:1x) | 18.1′ ✗ | **19.9′** | 21.0′ (at 66 %) |

- Lowercase x-height (8 rows) is about 14.4′ on the phone. Body text is mixed case (§6.4).
- **On a phone, a cap of 10 rows fails by 3 %; 11 rows passes with 7 % to spare.** Hence O-5: the C20 face gets an 11-row cap.
- Glance (Phase A), from `20_design` §2.3 scaled to the block type: the headline (58 mm) reads on desktop to about 3.2 m and across the whole phone stand box (24.7′ at its back); the second line (41 mm) reads on desktop to about 2.3 m, past the 1.6 m prompt edge. AT-T02 re-measures both on captures.

### 3.7 Touch hit sizes (phone, 95 % height; iPhone 16 Pro, 382 pt picture)

- One row is 15.3 pt. A MAIN item owns two rows: **30.6 pt**, under the 44 pt guideline. Mitigated by the two-tap rule (§5.4): the first tap only moves the bar, so a near miss never opens the wrong thing.
- Soft keys in row 25 take rows 24–25 plus the 10 pt dark-glass margin below the picture: **about 41 pt** tall, ≥ 60 pt wide.
- On an iPad 11-inch a row is 23 pt at 70 % and 31 pt at 95 %, so items (two rows) pass 44 pt either way.
- AT-C6 measures mis-taps on a phone (§9).

---

## 4. Behaviour

### 4.1 Keys per panel (period keys; FrontRooms' equivalents in §5)

| Key | MAIN | DOC | LIST | HELP |
|---|---|---|---|---|
| ↑ / ↓ | previous / next item | scroll one line | previous / next job | scroll one line |
| ← / → | previous / next item (Clipper) | previous / next screen (= PgUp / PgDn) | previous / next job | previous / next screen |
| Home / End | first / last item | top / bottom of the memo | first / last job | top / bottom |
| PgUp / PgDn | first / last item | one screen; past the end, the previous / next memo | first / last job | one screen |
| Enter | open the item | nothing | not authorised (§4.3) | nothing |
| Esc (Cancel) | **leave the close-up** | back to MAIN | back to MAIN | back to where HELP was opened |
| F3 (Exit) | leave the close-up | leave the close-up | leave the close-up | leave the close-up |
| F1 (Help) | HELP | HELP | HELP | nothing |

- Lists do not wrap (Clipper's default without `WRAP`): ↑ on the first item does nothing, silently.
- **Cancel on MAIN leaves.** Strict CUA does nothing there; we leave because Esc leaves every other close-up in the game (`Game.cs` Inspect branch, C6).
- A key with no job on a panel does nothing and makes no sound. Only refused *requests* (§4.3) beep, so pressing around is never punished.

### 4.2 Timing

- **At most one screen change every 0.34 s** (bar move, scroll, panel change, message): ≤ 3 changes a second, RELAY §9.5 and WCAG 2.3.1. A bar move swaps two rows between inverse and normal: on a 1080p desktop close-up that is about 44,000 px², a quarter of WCAG's flash-area threshold (341 × 256 px at 1024 × 768, scaled), so it is not a flash. It is limited like a page turn anyway, so there is one number.
- A press inside the window is held and applied when it ends; further presses are dropped (`20_design` §7.1's rule).
- **A held key repeats:** after 0.5 s (the PC/AT's default typematic delay, [S27]), then one step per 0.34 s. The period rate of 10.9 a second is cut to the photosafe rate.
- Nothing acts for 0.20 s after the close-up opens (EXAMINE's `CancelAfter`), so the press that opened it never selects.
- **Latches:** a MOVE key held when the close-up opens moves nothing until released (C25, now for W, A, S, D and the arrows). Any of them held when it closes is ignored by walking until released (C25b).
- Hard cuts only: no smooth scrolling, no transitions. Everything runs on unscaled time (`FrontRoomsInspect.Dt`).

### 4.3 Messages, refusals and "not authorised"

**Every refusal works the same way (CUA's beep plus a message, [S16]):**
1. The screen does not change, except row 24, which shows the message in BRIGHT.
2. One `TerminalRefused(kind)` event goes to 声音设计 (§4.5).
3. The message stays until the next accepted key; then row 24 describes the highlighted item again.

**The refusals in v1:**
- **NotAuthorized:** Enter on a print job. Draft: `Not authorized: only the owner of a job can change it.`
- **TopOfData / BottomOfData:** PgUp before the first memo, PgDn after the last.

**Rules (Relay honesty and no false promises):**
- **No login, password, code, lock-out or countdown,** and no hint that one exists elsewhere (`20_design` §14). The refusal is about ownership, never a missing key or code.
- **No red, no blink and no screen flash** for an error (red means the ALARM ZONE card, §8.7; a "visual bell" would be a flash).
- No refusal text names the Relay, warnings, darkness, doors or running (`20_design` §6.4, §8.6).
- There are no unavailable (dimmed) choices in v1, so the player never meets a greyed menu line with no explanation.

### 4.4 Memory and the glance

- **E always opens on MAIN, with the bar on the last item opened in this run** (else the first). A 1990 menu program returned to its main menu, and lightbar menus kept the last choice.
  - This replaces `20_design` §4.1's page memory. Reopening never cuts to a remembered page, and what the glance showed is what E opens.
- The in-world screen always shows MAIN. Only the ■ marks and the unread count change after reading. Those are UI state, caused by the player, deterministic, and blind to the Relay.
- The deal (no repeats within a run), content keys and revisit rules stay as in `20_design` §3.4 and §8.5.

### 4.5 Sounds (named for 声音设计; events only)

| Event | When | Period sound | Status |
|---|---|---|---|
| `TerminalKey` | each accepted key press, tap or swipe | the keyboard's own key | as `20_design` §11: a soft rubber-dome bottoming out, ≥ 80 ms, low attack; never a dry click (the CALLED tick family). The Model M's buckling-spring click is period-true but is that family |
| `TerminalRefused(kind)` | a refusal (§4.3) | **the PC-speaker beep** | **O-1:** `20_design` §11 bans a PC-speaker beep (a tone, and the lock-on chime is the game's only tonal chime). 声音设计 decides with 系统设计: keep the ban (a duller key bottoming out, no tone) or allow one short beep heard only inside the paused close-up, after RELAY §14's A/B test beside the chime |
| `TerminalPanel` | a panel change | none (a CRT redraw tick is banned) | silent |
| `Inspecting`, `Opened` | open and close | EXAMINE's S1–S3 | unchanged |

All of them are 2D, 0 Attention, never a `Disturb`, and play on the unscaled side of `snapshot:/Inspect`. Under COMMON rule 24, nobody plays them aloud on the Mac while building or testing.

### 4.6 Never

- No pointer, no text cursor, no typing field, no blinking.
- No Relay, director, door or lamp state on any panel (`20_design` §6.4); the close-up stays paused (D2).
- No real product names, people, addresses or working numbers; dates follow the live-calendar rule.
- No sound that is not in §4.5.

---

## 5. FrontRooms' controls

### 5.1 One verb set

| Verb (period key) | Windows / Linux keyboard | Mac keyboard | WebGL (any keyboard) | Pad (Xbox / PlayStation) | Touch (phone, iPad) |
|---|---|---|---|---|---|
| **MOVE** (↑ ↓, and ← → as previous / next) | ↑ ↓ ← →; the bound MOVE keys (W S A D); wheel (one notch per step); Home / End | same | same | d-pad; left stick (the game's menu navigation) | tap a line: the bar moves there |
| **SELECT** (Enter) | Enter, numpad Enter, Space | Return, Space | same | A / CROSS | tap the lit line again (a double tap also works); the `Enter` soft key |
| **CANCEL** (Esc; CUA F12) | Esc; Backspace | Esc; Backspace | **Backspace** (Esc is the browser's: it releases the pointer and pauses, `20_design` §7.2) | B / CIRCLE | the `Cancel` soft key; Android Back |
| **EXIT** (F3) | F3; **E** (the USE key, as in every close-up) | **E**; F3 works with fn | **E** | **X / SQUARE**; MENU also leaves, as in every close-up today | **BACK** (the one overlay); the hidden stick's pull-back from outside the picture |
| **HELP** (F1) | F1; the Help item | the Help item; F1 with fn | the Help item | the Help item | the `Help` soft key; the Help item |
| **PAGE** (PgUp PgDn; CUA F7 F8) | PgUp, PgDn; ← → and A D on DOC and HELP | fn + ↑ / ↓ (macOS's Page Up / Down, [S28]); ← → and A D | same as the keyboard it runs on | LB / RB (L1 / R1); d-pad ← → | swipe on the picture: up or left = next, down or right = previous |
| TEXT (the game's transcript) | T | T | T | Y / TRIANGLE (desktop) | none on a phone terminal (Red 11:00) |
| LOOK (±3°) | mouse | mouse | mouse | right stick | off on the picture (a drag there is a swipe) |

- **Unbound on terminals:** zoom (the wheel and the right mouse button are freed; the pad's RT and d-pad zoom too; pinch and double-tap zoom are off), F2, F4–F12, Tab, Alt and Alt + letter. F5 reloads a browser page, F10 opens the Windows window menu and F12 opens developer tools, so `20_design` §7.1's reasons still hold for every key except F1 and F3. [S29]
- **S** stops being "back" on a terminal and becomes "down" with the other MOVE keys (O-3). The leave keys stay E, Esc on MAIN, F3, X, B on MAIN, MENU and BACK, so Esc is never the only way out (`20_design` §7.2).
- R and O stay ignored in a close-up.

### 5.2 The function-key area names the player's real control

**Rule (a `WithControlNames` sibling of `FrontRoomsKeyHud.WithUseControl`):**
- Each footer item is one action plus its period key. The label prints `<control>=<Action>`.
- `<control>` is the **first control in the action's list for this platform that the player can press without a modifier**, named as the player's device prints it:
  - keyboards: the period key-cap words (`Enter`, `Esc`, `Bksp`, `PgUp`, `PgDn`, `F1`, `F3`). The USE key comes from `FrontRoomsInput.UseKey`, so a player who moved USE to F sees `F=Exit`;
  - pads: `FrontRoomsInput.PadName` (`A`, `B`, `X`, `LB/RB`, or `CROSS`, `CIRCLE`, `SQUARE`, `L1/R1`).
- If no such control exists (Help on a Mac or a pad), the item is left out; HELP stays reachable from MAIN.
- On touch the row is CUA's **short form** (Enter, Cancel, Help [S16]): the action words alone, as boxed soft keys in SOFTKEY colours. A tap is a press. This is the same rule as `WithUseControl` returning null on touch, where the glyph carries the verb.
- The row is re-rendered when the input mode changes (`FrontRoomsTouchControls.CurrentMode`; a key or pad press on a handheld).

**What row 25 says** (MAIN / DOC):

| Device | MAIN | DOC |
|---|---|---|
| Windows / Linux keyboard | `F1=Help  Enter=Select  F3=Exit` | `F1=Help  Esc=Cancel  F3=Exit  PgUp/PgDn=Page` |
| Mac keyboard | `Return=Select  E=Exit` | `Esc=Cancel  E=Exit  ←/→=Page` |
| WebGL; a keyboard on a phone or iPad | `Enter=Select  E=Exit` | `Bksp=Cancel  E=Exit  ←/→=Page` |
| Pad (Xbox / PlayStation) | `A=Select  X=Exit` / `CROSS=Select  SQUARE=Exit` | `B=Cancel  X=Exit  LB/RB=Page` / `CIRCLE=Cancel  SQUARE=Exit  L1/R1=Page` |
| Touch | `[Enter] [Help]` | `[Cancel] [Help]` |

- On a Mac and in a browser, PAGE prints `←/→`: MacBooks have no PgUp / PgDn keys, and fn + ↑ / ↓ is a modifier. PgUp, PgDn and fn + ↑ / ↓ still work, and HELP lists them. A Mac's Enter key is printed `Return`, as the key says.
- Row 3's `More:` and the Help panel use the same names.
- 平面视觉 checks every label set against the 80-column row and the face (§6, AT-C7).

### 5.3 The EXAMINE close-up's existing bindings, on a terminal

| Binding (today, main) | In every other close-up | On a terminal |
|---|---|---|
| E / pad A (`UseDown`) | leave | E leaves (EXIT); **pad A selects** |
| S / pad B (`ShotBackDown`) | leave | **S = down** (MOVE); **pad B = Cancel**, which leaves on MAIN |
| Esc / pad MENU (`PauseDown`) | leave (C6) | **Esc = Cancel** (leaves on MAIN); pad MENU leaves |
| Wheel (`Zoom`, one step per gesture) | zoom step | one MOVE step (no zoom on screens, `20_design` §9.4) |
| Right mouse / pad RT (`ZoomToggleDown`) | zoom toggle | nothing |
| Pad d-pad ↑ ↓ (`Zoom` composite) | zoom step | MOVE |
| T / pad Y (`TextToggleDown`) | TEXT transcript | TEXT on desktop (the current panel's lines); not on a phone terminal |
| Touch: BACK, pull-back, Android Back | leave | BACK and the pull-back leave; **Android Back = Cancel** (leaves on MAIN) |
| Touch: drag in the right zone, pinch, double tap | look, zoom, zoom toggle | on the picture: swipe = PAGE, double tap = select; pinch off |

The game's Inspect branch must therefore ask "is this a terminal?" before it treats `UseDown`, `ShotBackDown` or `PauseDown` as "leave" (CONTRACT 关卡设计, L3′). For a terminal it reads the terminal keys below and only MENU, BACK, focus loss and target loss as the close-up's own exits.

### 5.4 Touch (phone and iPad; 交互设计's routing, CONTRACT TT1′)

- **The picture is the input surface.** Taps map to cells through `FrontRoomsInspect.CurrentFaceViewportRect` → (col, row) on the 80 × 25 grid.
- **Lines (two-tap rule):** a tap on an item's two rows moves the bar there; a tap on the lit item opens it. A double tap on any item does both. Taps act on touch-up, with the existing `TapSlop` 8 pt / `TapMaxSeconds` 0.22 s test.
- **Soft keys:** row 25's boxes, each hit area extended over row 24 and the dark-glass margin below (§3.7).
- **Swipe = PAGE:** at least 40 pt within 0.35 s on the picture, on its main axis. On MAIN and LIST it jumps to the first or last item.
- **Leaving:** BACK (the only overlay, Red 11:00), Cancel on MAIN, Android Back on MAIN, and the hidden stick's pull-back when the drag starts outside the picture.
- **Not on a phone terminal:** the TEXT chip, the transcript panel, the PAGE chip of `20_design` §7.3, zoom, and the look-drag on the picture.
- A pad or keyboard on a handheld uses §5.1's pad or WebGL column, and row 25 switches at the first press.

### 5.5 Contract sketch (owners refine; nothing here is built)

```csharp
// 交互设计 · Input/FrontRoomsInput.cs (TT0'): per-key edges, read only while a terminal close-up is open.
public readonly struct TerminalKeys {
    public readonly int Move;            // -1 up, +1 down: ↑ ↓, the bound MOVE keys W / S, d-pad ↑ ↓, stick, wheel; edge + repeat (0.5 s, then 0.34 s)
    public readonly int Side;            // -1 / +1: ← →, A / D, d-pad ← →, stick; the panel maps it (MAIN, LIST: item; DOC, HELP: page)
    public readonly int Page;            // -1 / +1: PgUp PgDn (fn + ↑ ↓ on a Mac), LB RB, swipe
    public readonly int Jump;            // -1 Home, +1 End
    public readonly bool Select;         // Enter, numpad Enter, Space, pad South, Enter soft key, tap on the lit line
    public readonly bool Cancel;         // Esc (standalone), Backspace, pad East, Cancel soft key, Android Back
    public readonly bool Exit;           // F3, the USE key, pad West
    public readonly bool Help;           // F1, Help soft key
    public readonly Vector2Int TapCell;  // touch: the tapped (col, row) on 80 x 25; (-1, -1) for none
}
// 游戏视觉 · Rendering/Inspect (V1'): the terminal consumes TerminalKeys through the EXAMINE extension.
public interface IInspectTerminal { void Apply(in FrontRoomsInput.TerminalKeys keys, float unscaledDt); bool WantsExit { get; }
                                     string Footer { get; }            // row 25 for the current input mode (§5.2)
                                     string PanelTranscript { get; } }
public static event Action<FrontRoomsInspectable, int> TerminalRefused;   // kind: NotAuthorized, TopOfData, BottomOfData
```

---

## 6. The C20 glyph set

### 6.1 Exact list (109 slots, in CP437 order for the atlas)

**Printable ASCII, 0x20–0x7E: all 95** (space included, so 94 to draw):
- 26 capitals A–Z, **26 lowercase a–z**, 10 digits 0–9;
- 32 symbols: `` ! " # $ % & ' ( ) * + , - . / : ; < = > ? @ [ \ ] ^ _ ` { | } ~ ``.

All are drawn, not only the ones today's strings use. Key names (`PgUp`, `Bksp`), `=` and `/` in row 25, dates, job numbers and 叙事's later strings all draw on them, and a period PC face had every one.

**CP437 additions used by the layout: 14**

| CP437 | Unicode | Glyph | Used for |
|---|---|---|---|
| 0xC4 | U+2500 | ─ | rules under the DOC header; box edges |
| 0xB3 | U+2502 | │ | box sides (LIST, HELP) |
| 0xDA | U+250C | ┌ | box corner |
| 0xBF | U+2510 | ┐ | box corner |
| 0xC0 | U+2514 | └ | box corner |
| 0xD9 | U+2518 | ┘ | box corner |
| 0xC3 | U+251C | ├ | the LIST header rule meeting the box |
| 0xB4 | U+2524 | ┤ | the same, right side |
| 0xDB | U+2588 | █ | the block headline and second line on MAIN |
| 0x18 | U+2191 | ↑ | HELP and its key words |
| 0x19 | U+2193 | ↓ | HELP and its key words |
| 0x1B | U+2190 | ← | HELP; row 25 on a Mac and in a browser (`←/→=Page`) |
| 0x1A | U+2192 | → | the same |
| 0xFE | U+25A0 | ■ | the unread mark on MAIN |

Code points checked against the CP437 table [S30]. The 9th-column rule applies to 0xC0–0xDF (─ ┌ └ ┘ ├ █): their 8th column repeats into the 9th. [S26]

### 6.2 In CP437 but not used in v1 (not drawn)

| Glyphs | Why not |
|---|---|
| shades ░ ▒ ▓ (0xB0–0xB2) | Their period uses were fills, drop shadows and scroll-bar troughs [S31]; v1 has no pop-ups, shadows or scroll bars |
| half blocks ▀ ▄ ▌ ▐ (0xDF, 0xDC, 0xDD, 0xDE) | The block type uses whole cells (§3.3). Add them only if 平面视觉's G1 wants half-row steps (+0.1 day) |
| ► ◄ ▲ ▼ (0x10, 0x11, 0x1E, 0x1F) | No scroll bars and no drawn page arrows: the host panels' `More: - +` is used instead |
| double lines ═ ║ ╔ ╗ ╚ ╝ and mixed junctions | One line style; double frames are a BBS / Clipper look, not CUA's |
| ┬ ┴ ┼ | The print queue has no column rules |

### 6.3 Metrics (平面视觉 decides; these are the envelopes)

- Cell 8 × 16 (the hardware adds the 9th column).
- **Cap height 11 rows (rows 2–12)**, x-height 8 (rows 5–12), descenders 3 (rows 13–15); row 0 and part of row 1 left clear for line spacing. Box and block glyphs run edge to edge.
  - The 11-row cap is what lets the phone pass (§3.6, O-5). The mock's FR816 stand-in used 10.
- Stems 2 px and bars 1 px, as in the FR816 stand-in, so strokes survive the tube's scanlines.

### 6.4 Lowercase: needed

- Period programs set their menus, messages and documents in mixed case: DOS Shell's title and instructions, 1-2-3's menus, `F1=Help` itself, and every typed memo. [S10, S11, S12]
- All capitals would read as a host terminal or a sign, not an office PC.
- **Rule for 叙事:** the program name and memo field labels (`FROM:`, `TO:`, `DATE:`, `RE:`) in capitals; panel titles in Title Case; items, messages and memo bodies in sentence case. The mock's all-caps drafts need a mixed-case pass (counted in §7).

### 6.5 Drawing days at 8 × 16 (ESTIMATE; the mock's rate: about 50 glyphs a day including the coverage check)

| Part | Glyphs | Days |
|---|---|---|
| ASCII: capitals, lowercase, digits, symbols | 94 | 1.9 |
| CP437 additions (mostly geometric) | 14 | 0.2 |
| Block alphabets for MAIN: 5 × 7 and 4 × 5, capitals and digits | 2 × 36 small bitmaps | 0.5 |
| Atlas in CP437 order, the 9th-column test, and the fit check of every string and every row-25 label set | — | 0.4 |
| **Total** | **108 drawn + 72 block bitmaps** | **≈ 3.0** |

Against the mock's C20 option 1 (about 50 capitals, about 1 day; +0.5 with lowercase): +2.0 days.

---

## 7. Cost by line (working days, ESTIMATE)

Columns:
- **A** is the mock's figure (`40_for_red.md` §4: `20_design` §15.3 minus EXAMINE's landed touch items), without the C20 face.
- **C19** is this page.

| Line | A | C19 | Δ | What changes |
|---|---|---|---|---|
| **游戏视觉** (EXAMINE extension, screen render, input model) | 14.5 | **17.5** | +3.0 | **V1′ +1.0:** the terminal input model in the EXAMINE extension (verbs, the 0.34 s queue, repeat, latches, tap-to-cell, the row-25 label provider, the 95 % phone framing). **V2′ +1.5:** a panel machine (MAIN, DOC, LIST, HELP, refusals), bar and scroll state, ■ marks, the last-item memory, a transcript per panel. **V3′ ±0:** the char-grid raster replaces UI Toolkit (−0.5 for its proof, +0.5 for the cell buffer, CP437 atlas and attribute palette). **V6 +0.5:** captures of 4 panels × 3 label sets. V0, V4 and V5 unchanged |
| **平面视觉** (the face, the page art) | 4.5 | **8.0** | +3.5 | **The face, 3.0** (§6.5; A had none, C20 option 1 was +1.0). G1 2.5 (MAIN with block type, DOC, LIST, HELP, the refusal state, long and short key rows, attributes). **G2 +0.5:** the 80 × 25 envelope on every class, with the phone at 19.9′ and the touch hits. G0 0.5 (done), G3 0.5, G4 0.5 |
| **叙事设计** (strings) | 2.0 | **3.0** | +1.0 | Mixed case; a message line for every item; refusal texts; HELP in three label sets; panel titles; the print queue's jobs and owners; the folder-end message |
| **关卡设计** (interactable desks, the threat gate) | 9.75 | **10.5** | +0.75 | **L3′ +0.5:** the terminal branch in the Inspect input (E / S / Esc / pad A, B, X; C25 and C25b for every MOVE key; the WebGL pointer-lock poll). **L6 +0.25:** tests per device. L2 (one terminal per Office zone, which desks) unchanged. **L4:** the READ gate is `relay.ThreatPerceivable(cam)` through `CanShowInspect`, in main since 10-29 (V2), so most of L4's 2.5 days is already done in either column (UNVERIFIED how much of its test list is left); the prompt and the eye hide only for perceivable threats, and the paused close-up changes nothing |
| **声音设计** (beeps, keys) | 1.5 | **2.25** | +0.75 | Key Foley for each verb instead of one page sound (+0.5); the refusal sound and its A/B test beside the chime (O-1, +0.25) |
| 交互设计 (touch and pad routing; not in the brief's list, kept for the total) | 2.5 | **3.75** | +1.25 | **TT0′ +0.5:** `TerminalKeys` edges for the keyboard and the pad, and pad X. **TT1′ +0.75:** taps to cells, the two-tap rule, soft keys, swipes, Android Back = Cancel, the PAGE chip dropped |
| 系统设计 (`20_design` upkeep, tests) | 1.5 | **2.0** | +0.5 | §7, §8, §11, §12, §14, §15 amendments; the tests in §9 |
| Red | 0.5 | 0.5 | — | — |
| **Total** | **36.75** | **47.5** | **+10.75** | |

**Against the other options:**
- A plus the C20 capitals face: 37.75. C19 is **+9.75**.
- Option B (A + 6: cursor layer 2.5, input 1, routing 1, layouts 1, tests 0.5) plus that face: 43.75. C19 is **+3.75**. C19 needs no cursor layer, but it adds the key row, HELP, refusals, three label sets and a bigger face.

**Phase A (the glance: MAIN on the tube, no input)** is about **15 days**:
- L2 2, V5 2, V3′ 3.5, V2′ 0.75 (MAIN only), V6 0.75, L6 0.25;
- the face 3.0, G1 MAIN 1.0, G2 0.5, G3 0.5;
- N2 0.5 (MAIN strings).

**Order:**
1. The face (平面视觉) and the MAIN strings (叙事).
2. Phase A.
3. Phase B on desktop, WebGL and touch together: EXAMINE and its touch items are in main.

**Scope cuts, if wanted:**
- No HELP panel: −1.0 (help goes back to the HUD hint).
- No LIST and no not-authorised path: −1.25.
- Neither changes the convention.

---

## 8. Open items for owners (none blocks writing the face)

| # | Owners | Question | Recommendation |
|---|---|---|---|
| O-1 | 声音设计, 系统设计 | `TerminalRefused`: a PC-speaker beep (period-true, a tone) or no tone (`20_design` §11's ban)? | Start without a tone. Allow one short beep only if RELAY §14's A/B test shows players never confuse it with the lock-on chime; it can only sound inside the paused close-up |
| O-2 | 平面视觉, 游戏视觉 | The 80 × 25 text-mode raster (char grid, 1440 × 800) replaces G0's 40 × 15 grid of 16 × 32 cells | Yes: it is the period screen, it passes 18.6′ on every class with an 11-row cap, and it drops the UI Toolkit unknowns |
| O-3 | 系统设计, 关卡设计 | On a terminal, S moves down instead of leaving, and pad A selects instead of leaving | Yes: WASD is a requested equivalent, and E, Esc, F3, X, B, MENU and BACK still leave |
| O-4 | 游戏视觉, 交互设计 | WebGL: do F1 and F3 reach the game, or does the browser take them (Chrome: F1 opens Help, F3 opens Find)? Does Backspace stay harmless? | Prove it in a WebGL clone. Until then WebGL prints `E=Exit` and `Bksp=Cancel` and uses the Help item. [S29, S32, S33] |
| O-5 | 平面视觉 | The C20 face's cap height | 11 rows (§6.3); 10 fails the phone by 3 % |

**UNVERIFIED (to check, not to decide):**
- the 1990 split of US office desks between DOS, Windows, Mac and host terminals (§1.1);
- CUA's own 1987–89 key table (the figures in Berry 1988 are garbled in the OCR; the key list here comes from later IBM and vendor documents that restate CUA [S15, S19]);
- the PC/AT typematic delay (sources give 500 ms or 250 ms; we use 0.5 s) [S27];
- whether fn + ↑ / ↓ on a MacBook arrives in Unity's Input System as Page Up / Page Down;
- the CPU raster time per change on an iPhone (ESTIMATE ≤ 1 ms);
- touch mis-taps at 30.6 pt items and 41 pt soft keys on a phone (AT-C6).

---

## 9. Tests (add to `20_design` §16; owners as in §7)

| # | Test | Pass |
|---|---|---|
| AT-C1 | **Labels are true:** on every platform and input mode, press each control printed in row 25 and HELP | each does its action; 0 mismatches; no control printed in row 25 needs a modifier |
| AT-C2 | **Photosafety:** mash every terminal key at 20 Hz for 10 s; hold ↓ for 10 s | ≤ 3 screen changes in any 1 s; nothing blinks |
| AT-C3 | **Legibility:** captures of DOC on desktop 1080p, iPad 11-inch and iPhone 16 Pro (95 %) | body cap ≥ 18.6′ on each; contrast ≥ 4.5 : 1 after the tube (`20_design` §9.3) |
| AT-C4 | **Latches:** hold S, W, A, D or an arrow across open and close | no bar move on open; no step on close until released |
| AT-C5 | **Relay neutrality:** as AT-T17 and AT-T20, with menus driven | identical director and hunter traces; no terminal sound in a Relay cue family |
| AT-C6 | **Touch:** 100 scripted taps per item and soft key on a phone; 50 swipes | the right cell every time inside the hit; two-tap rule holds; swipes never read as taps |
| AT-C7 | **Glyph coverage:** render every string and every row-25 and HELP label set | 0 missing glyphs; every row ≤ 80 cells |
| AT-C8 | **Leave paths:** every leave control on every platform from MAIN, DOC, LIST and HELP | each leaves (or backs up, for Cancel) as §4.1 says; Esc is never the only way out |

---

## 10. Sources

Web sources were read online on 2026-10-30; nothing was downloaded. "Checked earlier" means a screenshot was checked visually in `10_research.md` (2026-10-08) and is reused here.

| # | Source | Supports |
|---|---|---|
| S1 | [Census Statistical Brief SB91-11, The Growing Use of Computers](https://www.census.gov/library/publications/1991/demo/sb91-11.html); [Tech Monitor: 37 % of workers used a computer in 1989](https://techmonitor.ai/?p=163367) | 36.8 % at work, Oct 1989 (via `10_research.md`) |
| S2 | [IBM Personal System/2 (Wikipedia)](https://en.wikipedia.org/wiki/IBM_Personal_System/2) | PS/2 1987, corporate sales |
| S3 | [Video Graphics Array (Wikipedia)](https://en.wikipedia.org/wiki/Video_Graphics_Array) | VGA 1987, standard within three years (via `10_research.md`) |
| S4 | [IBM 3270 (Wikipedia)](https://en.wikipedia.org/wiki/IBM_3270) | host terminals, PF and PA keys, block mode |
| S5 | [IBM OfficeVision (Wikipedia)](https://en.wikipedia.org/wiki/IBM_OfficeVision); [PROFS announcement, 1983 (bitsavers)](https://bitsavers.mirrorservice.org/pdf/ibm/370/VM/SP/PROFS/profs_ann_198306.txt) | PROFS / OfficeVision, menu of functions |
| S6 | [IBM 5250 (Wikipedia)](https://en.wikipedia.org/wiki/IBM_5250) | 5250 terminals |
| S7 | [Model M keyboard (Wikipedia)](https://en.wikipedia.org/wiki/Model_M_keyboard) | Model M 1985; 101-key ANSI layout adopted 1988 |
| S8 | [Computer Hope: MS-DOS history](https://www.computerhope.com/history/dos.htm); [DOS Shell (Wikipedia)](https://en.wikipedia.org/wiki/DOS_Shell) | DOS 4.0 June 1988, 4.01 Nov 1988, 5.0 June 1991 |
| S9 | [WordPerfect (Wikipedia)](https://en.wikipedia.org/wiki/WordPerfect) | 5.1 DOS Nov 1989; pull-down menus first in 5.1; F3 Help, F1 Cancel; Windows version 1991; 85 % of the DOS market in 1992 |
| S10 | [File:Wordperfect-5.1-dos.png (Wikimedia Commons)](https://commons.wikimedia.org/wiki/File:Wordperfect-5.1-dos.png) | mixed-case menu bar and status line (checked earlier) |
| S11 | [Lotus 1-2-3 (Wikipedia)](https://en.wikipedia.org/wiki/Lotus_1-2-3); [123elf wiki: Getting Started](https://github.com/taviso/123elf/wiki/Getting-Started); [File:Lotus-123-3.0-MSDOS.png](https://commons.wikimedia.org/wiki/File:Lotus-123-3.0-MSDOS.png) | `/` menu, arrow keys, first letter, Esc goes back one level; mixed-case menu words |
| S12 | [Dinosaur Sightings: DOS 4.01 Shell (TechRepublic gallery)](https://www.techrepublic.com/pictures/dinosaur-sightings-dos-401-shell/) | title line with date, title and time; `F1=Help`; F10, Tab and the arrows; F3 exits; mixed-case instructions (checked earlier) |
| S13 | [IBM Common User Access (Wikipedia)](https://en.wikipedia.org/wiki/IBM_Common_User_Access) | CUA 1987; F1 help, F10 menus, Esc cancel, Enter OK, Tab; WordPerfect non-CUA; OS/2, Windows, MS-DOS Editor followed it |
| S14 | [EDM2: CUA documents](https://www.edm2.com/index.php/CUA); [IBM Systems Application Architecture (Wikipedia)](https://en.wikipedia.org/wiki/IBM_Systems_Application_Architecture) | the CUA document series and dates |
| S15 | [LANSA: SAA/CUA function key assignments](https://docs.lansa.com/14/EN/lansa065/content/lansa/dsnbd_0035.htm) | F1 Help, F3 Exit, F7 Backward, F8 Forward, F10 Actions, F12 Cancel (a vendor restating CUA) |
| S16 | [R. E. Berry, "Common User Access – A consistent and usable human-computer interface for the SAA environments", IBM Systems Journal 27(3), 1988 (archive.org)](https://archive.org/details/ibmsj2703E) | the function-key area as key reminder and touch area, its short form (Enter, Cancel, Help); Cancel backs up; Exit leaves; scroll information; beeps (quoted in §1.3) |
| S17 | [Clipper (programming language) (Wikipedia)](https://en.wikipedia.org/wiki/Clipper_(programming_language)) | Summer '87 = 21 Dec 1987, 5.0 in 1990; xBase business programs, banks and insurers |
| S18 | [Clipper Norton Guide: MENU TO (textfiles.com mirror; the guide's version is not stated)](https://discmaster.textfiles.com/file/6633/Collection%20of%20Tools%20and%20Utilities.iso/clipper/clip_ng.zip/CLIPPER.NG/ng2ebf2.html) | the light-bar menu's keys; the message row |
| S19 | [IBM z/OS concepts: ISPF keyboard and function keys](https://www.ibm.com/docs/en/zosbasics/com.ibm.zos.zconcepts/zconc_ispfkeybd.htm) | the `F1=Help … F3=Exit … F12=Cancel` key line; PF7 / PF8 page up / down |
| S20 | [IBM 3205 announcement (1984)](https://sharktastica.co.uk/resources/docs/txt/IBM_184-149_3205_1984_IBM.txt) | field colours by attribute; green = typeable (via `10_research.md`) |
| S21 | [Steven Sinofsky, Hardcore Software 007: Windows 3.0 Buzz](https://hardcoresoftware.learningbyshipping.com/p/007-windows-30-buzz) | Windows 3.0: 4 million copies in its first year; 17 million PC compatibles sold in 1989; the leading DOS programs absent at launch |
| S22 | [Windows 3.0 (Wikipedia)](https://en.wikipedia.org/wiki/Windows_3.0) | 22 May 1990 |
| S23 | [System 7 (Wikipedia)](https://en.wikipedia.org/wiki/System_7); [Mac (computer) (Wikipedia)](https://en.wikipedia.org/wiki/Macintosh) | System 7 May 1991; Mac share 10–15 % (undated) |
| S24 | [Chicago (typeface) (Wikipedia)](https://en.wikipedia.org/wiki/Chicago_(typeface)) | Apple's system face |
| S25 | [HP 150 (Wikipedia)](https://en.wikipedia.org/wiki/HP_150) | Nov 1983 touch-screen MS-DOS PC; touch used mainly for cursor positioning and function-key control |
| S26 | [VGA text mode (Wikipedia)](https://en.wikipedia.org/wiki/VGA_text_mode); [FreeVGA: text-mode cursor (Stanford mirror)](https://www.scs.stanford.edu/21wi-cs140/pintos/specs/freevga/vga/textcur.htm) | 720 × 400, 9-dot cells, the 9th-column rule for C0–DF; the cursor's fixed blink (16 frames per state) and the Cursor Disable bit |
| S27 | [HelpPC 2.10 (1991): keyboard commands](https://people.cs.nycu.edu.tw/~lwhsu/course/os/helppc/keyboard_commands.html); [kbdrate(8)](https://www.ibiblio.org/gferg/ldp/man/man8/kbdrate.8.html) | typematic default 10.9 a second; delay 500 ms (HelpPC) or 250 ms (kbdrate) |
| S28 | [Apple: Mac keyboard shortcuts](https://support.apple.com/en-us/102650); [Apple: use the function keys on a Mac](https://support.apple.com/en-us/102439) | fn + ↑ / ↓ = Page Up / Down; F1–F12 need fn by default |
| S29 | [Google Chrome keyboard shortcuts](https://support.google.com/chrome/answer/157179) | F1 Help, F3 Find, F5 reload, F10 toolbar, F12 developer tools |
| S30 | [Code page 437 (Wikipedia)](https://en.wikipedia.org/wiki/Code_page_437) | the code points in §6.1 |
| S31 | [Box-drawing characters (Wikipedia)](https://en.wikipedia.org/wiki/Box-drawing_characters) | frames in text interfaces; blocks and shades for fills and drop shadows |
| S32 | [Unity: WebGLInput.captureAllKeyboardInput](https://docs.unity.com/en-us/engine/current/script-reference/unityengine/webglinput/captureallkeyboardinput) | keys go to the canvas; browser shortcuts not documented (O-4) |
| S33 | [MDN: Pointer Lock API](https://developer.mozilla.org/en-US/docs/Web/API/Pointer_Lock_API) | Esc releases pointer lock in the browser |
| S34 | [Human-System Interface Style Guide for ACORN (arXiv 2312.12579)](https://arxiv.org/pdf/2312.12579); [Taylor & Francis chapter on character size (b22361-52)](https://www.taylorfrancis.com/books/e/9781315098128/chapters/10.1201/b22361-52) | character height: 16′ minimum, 20–22′ preferred (context for the project's 18.6′ rule, EX §3.4) |
| S35 | [BMC: scroll indicators on TSO/ISPF panels](https://docs.bmc.com/docs/dla69/scroll-indicatorstso-ispf-panels-781018314.html); [IBM Db2: panel DSNTIPRA (More + / -)](https://www.ibm.com/support/knowledgecenter/en/SSEPEK_12.0.0/inst/src/tpc/db2z_dsntipra.html) | the host panels' `More:` indicator with `+` / `-` (later documentation of the convention) |

**Project files:**
- `research/computer_ui/20_design.md` (§2.3–§2.4, §4, §7, §8, §9, §11, §14, §15);
- `research/computer_ui/30_crosshair_grammar.md`;
- `research/computer_ui/40_for_red.md` and `images/EC03_phone.jpg` v3;
- `research/computer_ui/10_research.md` (§ Real office computer UIs);
- `RELAY_PURSUIT_REDESIGN.md` §9.4–§9.5;
- `W/exam_computer/pages.py` (FR816: cap 10 rows);
- code read for current bindings:
  - `Assets/Scripts/Input/FrontRoomsInput.cs` (`FrameSnapshot`, `AddGamepadBindings`, `KeyName`, `WithKeyNames`, `PadName`);
  - `Rendering/UI/FrontRoomsKeyHud.cs` (`WithUseControl`);
  - `FrontRooms3DGame.cs` (Inspect branch, `CanShowInspect`, `ThreatPerceivable`);
  - `Input/FrontRoomsTouchControls.cs` (Inspect roles, `ReadMenuNav`);
  - `Rendering/Inspect/FrontRoomsInspect*.cs` (hints, prompts).
