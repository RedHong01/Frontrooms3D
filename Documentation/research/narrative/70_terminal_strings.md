# 70 · Terminal strings: the C19 program (叙事设计, 2026-10-30)

**Why now.** Red answered the terminal questions on 2026-10-30 in 游戏视觉's chat (systems.json C19, C20, A38):
- C19, 10:58: "在这里选择在那个时代的电脑操作系统所使用的交互方式";
- C20 and A38 at 10:59.

游戏视觉's spec `computer_ui/41_c19_period_interaction.md` (§7) orders the work: "1. The face (平面视觉) and the MAIN strings (叙事)." This page delivers step 1, the MAIN strings (N2, 0.5 d). It also corrects the spec's draft strings for the other panels wherever they break a string rule, so nobody builds on them. The memo pool (8 memos) comes with Phase B (§6).

**Rules applied:**
- `20_design` §8.6 (no brands; dates on the live calendar; teaching true for the shipping Relay; no warning cues; no door advice before RELAY step 4);
- `41` §4.3 (no login, code or lock-out) and §4.6;
- `41` §6.4 case: the program name and memo field labels in capitals, panel titles in Title Case, everything else in sentence case;
- every character in the C20 face: ASCII plus `─ │ ┌ ┐ └ ┘ ├ ┤ █ ↑ ↓ ← → ■`. There is **no `·`**, so none is used here.

## 1. The program's name: `OFFICE MEMO` (replaces the draft `INTEROFFICE`)

`InterOffice` was Oracle's groupware product in the mid-1990s (trade press: Tech Monitor, "Oracle boosts InterOffice groupware with dedicated division"; Computing, "Questions for Oracle InterOffice"). That makes it a real product name, which rule 1 forbids. It is the same case as WXRM-LP.
- `OFFICE MEMO` is generic, says what the program holds, and reads as an in-house tool from a tenant's own IT person (D6 (a)).
- It appears on row 1 of every panel and in HELP's leave row.
- 游戏视觉 changes the 10 places in `41` and the mock's frames when convenient. The mock that was shown to Red asked nothing about the name.

## 2. MAIN (the front page and the glance): signed

| Slot | String | Notes |
|---|---|---|
| Row 1, col 2 | `OFFICE MEMO` | program name, capitals |
| Row 1, centred | `Main Menu` | panel title |
| Headline (7 block rows) | `MEMOS` | kept from the mock |
| Second line (5 block rows) | `2 UNREAD` / `1 UNREAD` / `ALL READ` | `ALL READ` is the zero word `41` §3.3 asks for. All three are 8 characters, so the block line never changes width. 平面视觉's second-line block alphabet needs `1 2 A D E L N R U` and the space |
| Memo item | `■ Memo from R.M., Facilities` + the date right-aligned (`10/30/90` style, M/D/YY) | `■` only while unread |
| Memo item | `■ Memo from D.K., Security` + the date | **was `Night Desk`.** One name per thing: the player knows that post as the `SECURITY DESK` (placard, legend, counter front). `NIGHT  DESK` stays only as the shift column in D.K.'s own log |
| Item | `Print queue` | |
| Item | `Help` | last, as CUA puts it |
| Row 24, on a memo | `Read the memo from R.M., Facilities.` / `Read the memo from D.K., Security.` | the message line describes the lit item |
| Row 24, on Print queue | `See what is waiting at the printer.` | |
| Row 24, on Help | `See which keys do what.` | |
| Row 25 | per `41` §5.2's table (`F1=Help  Enter=Select  F3=Exit`, `Return=Select  E=Exit`, `A=Select  X=Exit`, touch `[Enter] [Help]` …) | signed as specified. The action words are CUA's own: Help, Select, Exit, Cancel, Page |

**Dates.** The two memos are dealt from the pool. The newer is dated this month, the older four months back, both computed from the live date with 1990 weekdays (`20_design` §8.5; `station/01` §5). The mock's `10/30/90` and `6/30/90` were that rule applied on 10-30, not fixed strings.

**`20_design` §8.6 rule 5** ("no word for leaving the program") was written for the pager. Red's C19 answer brings the period's own EXIT verb (`F3=Exit`; HELP's leave row), so `41` supersedes the rule. 系统设计 amends §8.6 with the §7/§8 amendments `41` §7 already lists.

## 3. DOC, LIST and HELP: frames signed, two draft lines corrected

**Panel titles:** `Read Memo`, `Print Queue`, `Help` (Title Case). Headings: `Memo 1 of 2`, `Keys`.

**DOC field labels:** `FROM:`, `TO:`, `DATE:`, `RE:` (capitals, as drafted).

**The mock's memo** (R.M., Facilities, to `All tenants`, RE: `Fire alarm procedure`). It is the first memo of the pool. Two lines change:

| Draft | Signed | Why |
|---|---|---|
| `Breaking any window on this level sounds the alarm for the whole level.` | `Breaking a window sounds the alarm.` | `20_design` §8.5's teaching line. "For the whole level" promises a level-wide bell that nothing builds. v2's device sounds at the device and calls to it (RELAY §6) |
| `Next drill: Tuesday, November 13, at 10:00 AM.` | `Next drill: <Weekday>, <Month> <D>, at 10:00 AM.` | A future date is computed from the live date: month and day only, 1990 weekday, never a year (`station/01` §5). The mock's line was that rule on 10-30 |

The other lines stand:
- `In a fire alarm, walk; do not run. Follow the posted plan to Stair A.`
- `On alarm, a floor warden checks every room.` (the placard's own sentence; a tenant memo repeating a posted rule is ordinary)
- the sign-off `R.M.`

**Folder-end refusals** (row 24, BRIGHT, until the next accepted key):
- before the first memo: `Top of folder: no earlier memos.`
- after the last: `End of folder: no more memos.`

**LIST (the print queue):**
- Header: `Queue: MAIN`, `Printer: LASER 2`, `Status: Printing`. Generic names; no maker.
- Columns as drafted: `Job  Document  Owner  Pages  Status`.
- Jobs:

| Job | Document | Owner | Pages | Status | Row 24 when lit |
|---|---|---|---|---|---|
| `0412` | `Fire drill notice` | `R.M.` | `2` | `Printing` | `Printing page 2 of 2.` |
| `0413` | `Key sign-out sheet` | `D.K.` | `1` | `Waiting` | `Waiting for the printer.` |

- **Why these jobs:**
  - 0412 is the memo above, stuck on its last page (`screens/30` §6's stuck tenant job).
  - 0413 is the desk's own form (the key sign-out sheet at the security desk). It says nothing about the hunter.
  - Neither job uses a lamp, dark or door word (`20_design` §8.6).
- **NotAuthorized** (Enter on a job), as drafted: `Not authorized: only the owner of a job can change it.` It is about ownership, never a code (`41` §4.3).

**HELP: three label sets.** Same rows, each naming the player's real control (`41` §5.2):

| Keyboard (Windows / Linux) | Mac keyboard | Pad (Xbox; PlayStation names in brackets) | Touch |
|---|---|---|---|
| `↑ ↓` Choose an item | `↑ ↓` Choose an item | `d-pad` Choose an item | `Tap an item` Choose it |
| `← →` Previous or next | `← →` Previous or next | — | — |
| `Enter` Open the chosen item | `Return` Open the chosen item | `A (CROSS)` Open the chosen item | `Tap it again` Open it |
| `PgUp PgDn` Previous or next page | `fn ↑ ↓` Previous or next page | `LB RB (L1 R1)` Previous or next page | `Swipe` Previous or next page |
| `Esc` Back one step | `Esc` Back one step | `B (CIRCLE)` Back one step | `Cancel` Back one step |
| `F3` Leave OFFICE MEMO | `E` Leave OFFICE MEMO | `X (SQUARE)` Leave OFFICE MEMO | `BACK` Leave OFFICE MEMO |
| `F1` This help | — (Help is the menu's last item) | — | `Help` This help |

On WebGL, `Bksp` replaces `Esc` (Esc belongs to the browser, `41` §5.1). A player who has moved USE sees their own key in the leave row (`41` §5.2). Words and keys are both in the face.

## 4. What stays the same

- **Nothing on any panel names the hunter, warnings, darkness, doors or running.** No login, code, countdown or all-clear (`41` §4.3, §4.6).
- **Memos never say walking is silent.** Walking is heard on every platform since A36.
- **No door advice until RELAY step 4.**

## 5. For the owners

- **游戏视觉 (`41`, the mock):** the rename (§1), `D.K., Security` (§2), the two memo lines (§3), the print queue rows. Nothing here changes a layout. Every row stays inside its frame: `OFFICE MEMO` has the same 11 characters as `INTEROFFICE`, so the title row and HELP's leave row keep their lengths.
- **平面视觉 (the face and the block alphabets):** no new glyphs. The second line's block letters are `1 2 A D E L N R U`; the headline's are `E M O S`.
- **系统设计 (`20_design` upkeep):** §8.6 rule 5 is superseded by C19 (§2).

## 6. Next (Phase B, after Phase A lands)

- **The memo pool (8 memos, ≤ 11 lines each):** the R.M. and D.K. thread (`20_design` §8.5), initials only, nothing about the guard's fate beyond plain duties.
  - D.K.'s memos are desk business: the night post, the sign-out sheet, the business-hours plate.
  - R.M.'s are drills and posted rules.
  - Door memos wait for RELAY step 4.
- **An L6 check of terminals met per run** sets the pool's final size.
