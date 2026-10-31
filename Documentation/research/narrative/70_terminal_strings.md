# 70 · Terminal strings: the C19 program (叙事设计, 2026-10-30)

**Status (2026-10-30 15:15):**
- **In main:** §2–§3 as `Assets/Resources/Terminal/Content/interoffice.json` 83d5f14c (§7 lists how it landed). Readable terminals are not on in the game yet, so nobody has seen these strings on screen; 游戏视觉's next capture will show them.
- **Installed (2026-10-30 15:24):** the Phase B memo pool (§6) as `Resources/Terminal/Content/terminal_1.json` (74973f66), `terminal_2.json` (26ffac7e) and `terminal_3.json` (24dbb071). 关卡设计 deals keys 0–3; key 0, or a key with no file, shows `interoffice.json`.
- **Layout check:** a script mirroring `FrontRoomsTerminal.cs` (widths, rows, tokens, 1990 calendar) reports 0 problems on main and on sets 1–3.

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
| Memo item | `■ R.M., Facilities  Fire alarm procedure` + the date right-aligned (`10/30/90` style) | Writer plus subject (changed 2026-10-30 from `Memo from R.M., Facilities`), so a second terminal's menu never looks like the first. `■` only while unread. Writer padded to 18 columns so the subjects line up |
| Memo item | `■ D.K., Security    After hours` + the date | **`Security` was `Night Desk`.** One name per thing: the player knows that post as the `SECURITY DESK` (placard, legend, counter front). `NIGHT  DESK` stays only as the shift column in D.K.'s own log |
| Item | `Print queue` | |
| Item | `Help` | last, as CUA puts it |
| Row 24, on a memo | `Read the memo from R.M., Facilities.` / `Read the memo from D.K., Security.` | the message line describes the lit item |
| Row 24, on Print queue | `See what is waiting at the printer.` | |
| Row 24, on Help | `See which keys do what.` | |
| Row 25 | per `41` §5.2's table (`F1=Help  Enter=Select  F3=Exit`, `Return=Select  E=Exit`, `A=Select  X=Exit`, touch `[Enter] [Help]` …) | signed as specified. The action words are CUA's own: Help, Select, Exit, Cancel, Page |

**Dates.** Month unpadded, day two digits, as landed: `10/30/90`, `1/01/90`. The two memos are dealt from the pool. The newer is dated this month, the older four months back, both computed from the live date with 1990 weekdays (`20_design` §8.5; `station/01` §5). The mock's `10/30/90` and `6/30/90` were that rule applied on 10-30, not fixed strings.

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
- **No door advice until RELAY step 4:** nothing on closing, holding or what a door stops. A plain fact about the building is not advice (`After hours the main entrance is locked.`).

## 5. For the owners

- **游戏视觉 (`41`, the mock):** the rename (§1), `D.K., Security` (§2), the two memo lines (§3), the print queue rows. Nothing here changes a layout. Every row stays inside its frame: `OFFICE MEMO` has the same 11 characters as `INTEROFFICE`, so the title row and HELP's leave row keep their lengths.
- **平面视觉 (the face and the block alphabets):** no new glyphs. The second line's block letters are `1 2 A D E L N R U`; the headline's are `E M O S`.
- **系统设计 (`20_design` upkeep):** §8.6 rule 5 is superseded by C19 (§2).

## 6. The memo pool (Phase B): 4 sets of 2, written 2026-10-30

Each set pairs one memo from R.M. (Facilities) with one from D.K. (Security): the newer dated this month, the older four months back. The aim: a second terminal never reads like the first. All four sets share the same `list`, `help`, `words` and `refusals`.

**Files:** set 0 is `Assets/Resources/Terminal/Content/interoffice.json`; sets 1–3 are `terminal_1.json` to `terminal_3.json` in the same folder. **The files in main are the source of truth for the text;** `research/narrative/terminal_sets/` keeps the first drafts only. `FrontRoomsTerminalContent.Load(contentKey)` reads `terminal_<key>.json` and falls back to `interoffice.json`; 关卡设计's deal passes the key through `FrontRoomsTerminalScreen.Attach(monitor, contentKey)`.

An L6 check of terminals met per run may change the pool's final size. Door memos wait for RELAY step 4.

| Set | This month | Four months back |
|---|---|---|
| 0 | R.M., `Fire alarm procedure` | D.K., `After hours` |
| 1 | D.K., `Key sign-out` | R.M., `Remodeling` |
| 2 | R.M., `Carpet cleaning` | D.K., `Lost and found` |
| 3 | D.K., `Visitors` | R.M., `No smoking` |

**Set 1**

D.K., Security, `Key sign-out`:
```
Please sign keys back in before you leave for the day.
Last week three office keys were not returned.

A key that is not signed back in by 0700 is reported missing.
Please do not lend your key. Each key is signed out to one person.
```

R.M., Facilities, `Remodeling`:
```
Remodeling continues on several floors.
Until it is finished, a floor may differ from its posted plan.

If the posted plan on your floor is out of date, please tell Facilities.

Thank you for your patience.
```

**Set 2**

R.M., Facilities, `Carpet cleaning`:
```
Carpets on every floor will be cleaned this weekend.
Please clear the floor under your desk before you leave on Friday.

The carpet may still be damp on Monday.
The smell is from the cleaning solution and fades in a day or two.

Questions to Facilities.
```

D.K., Security, `Lost and found`:
```
Items found on the night tours are kept at the security desk for 30 days.
Please ask for them during business hours.

Items not claimed in 30 days are given to charity.
```

**Set 3**

D.K., Security, `Visitors`:
```
All visitors sign the register at the security desk.
Please meet your visitors at the desk and see them out when they leave.

After 1800, visitors are admitted only if a tenant has called the desk.
```

R.M., Facilities, `No smoking`:
```
As of the first of the month, there is no smoking anywhere in the building.
This includes offices, restrooms and the stairs.

Please smoke outside, away from the main entrance.
```

**Why they pass the rules:** initials only; ordinary tenant business; nothing about the guard's fate; no hunter, warnings, darkness or running. `a floor may differ from its posted plan` echoes the placard's `THIS FLOOR MAY DIFFER FROM PLAN`, and `Security tours` and `night tours` agree with the guard's log.

## 7. As landed (`interoffice.json` 83d5f14c)

Differences from §2–§3's first text:
- **Case:** mixed case on the C20 face, with lowercase (`41` §6.4).
- **`DATE:` row:** every memo's head has four rows: `FROM:`, `TO:`, `DATE:`, `RE:`.
- **Tokens:** `{DATE_NOW}`, `{DATE_BACK4}`, `{DRILL_WEEKDAY}`, `{DRILL_MONTH}`, `{DRILL_DAY}`, `{UNREAD}`, `{N}`, `{M}`, `{PROGRAM}`.
- **Set 0's second memo,** D.K., Security, `After hours`:
```
After hours the main entrance is locked.
Please sign in and out at the security desk.

All keys must be signed out.

Security tours the building once an hour, 2300 to 0700.
```
- **Menu lines:** writer plus subject (§2), landed 2026-10-30. The row-24 messages still read `Read the memo from R.M., Facilities.`
