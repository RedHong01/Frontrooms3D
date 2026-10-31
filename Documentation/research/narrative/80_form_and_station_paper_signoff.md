# 80 · Sign-offs: playtest form REV and station paper (叙事设计, 2026-10-30)

Two asks from 教程设计: PT3 (`research/tutorial/30_playtest_plan.md` §4, §7) and BS10 (`research/tutorial/20_requests.md`). Faces and layout stay with their owners.

## 1. Playtest form (round 3): signed

Every row of `30` §7 is signed as written, with one change.

| Slot | String | Status |
|---|---|---|
| Rating 02 | `The evacuation plan told me what I needed to know.` | Signed |
| Rating 03 | `I noticed arrows in the wallpaper.` | Signed |
| Rating 05 | `I trusted the arrows in the wallpaper.` | Signed; see the note below |
| Rating 06 | `The sound of the TV led me somewhere.` | Signed |
| **Rating 07** | **`I noticed something coming by sound or light before it reached me.`** (66 characters; today's line is 63) | **Changed** from "the hunter" |
| Interview 05 | `Did the wallpaper ever seem to change? What did you make of it?` | Signed |
| Most helpful cue | `PLAN · TV · ARROWS · EXIT SIGN · LIGHT · HUD` | Signed |

- **Rating 07.** "The hunter" is a name players never see, the same fault as "Relay". `FLOOR WARDEN` is no better: a player may not link it, and it would tie the form to fine print. "Something coming" needs no name, is true however the player thought of it, and matches the game's own "it" (`DOORS IT BROKE`, `IT  23 M`).
- **Rating 05 (for 教程设计's analysis, no string change):** a player who saw no arrows has nothing to trust, yet must still mark a number. Read 05 only where 03 is 3 or higher.

**Moderator script (`30` §4): no line breaks a rule.** Nothing names the hunter, the arrows or the key before the form, and nothing implies that the dark hides you.

## 2. BS10: floors below LEVEL 0

- **`LEVEL B10+`: signed** for the shared sheets (`sheetA_B10plus`, `sheetA_phase0_B10plus`). The `+` appears on these sheets only; the HUD, CAUGHT and the stair signs keep the exact floor (`LEVEL B12`, `LEVELS B12 TO B13`). A building printing one plan for "everything deeper" suits `THIS FLOOR MAY DIFFER FROM PLAN`.
- **Key tag `STAIR A` below LEVEL 0: signed.** Every stair is STAIR A (`40` §1, §5); the HUD's floor token says which.
- **The station's fiction on B floors: no new strings.** Every B-floor desk carries the same paper as Floor 1: the same log, the same `D.K.`, the same `0200 TOUR 3`, dated by the same rule. A building whose every floor has that desk is the quiet wrongness; nobody says so. `KEY AT SECURITY DESK.` is true on a floor with its own station (`40` §3). The BUSINESS HOURS plate stays on Floor 1's main entrance only.

## 3. Station paper: corrections (`station_paper/build_atlas.py`, 平面视觉)

The plate, the DAR lines, the headers, `PLEASE SIGN IN`, `KEY CONTROL`, `POST ORDERS` and the initials-and-trades approach are signed. Seven corrections:

| # | Where | Now | Change to | Why |
|---|---|---|---|---|
| P1 | Register, last two rows | `T.R.` 1705 before `D.N.` 1630 | `D.N.` row first, then `T.R.` | People sign in as they arrive, so TIME IN must rise. Everyone is still out by 1745 |
| P2 | Register, row 3 | `ELEVATOR SVC` | `HVAC SVC` | The game has no elevators (`01` §2). A line about one sends a careful reader looking for it |
| P3 | Phone list | `ELEVATOR SVC  214` | `BOILER RM  214` | Same reason; a plain in-house line |
| P4 | Phone list | `NIGHT DESK  200` | `SECURITY DESK  200` | One name per thing (`70` §2). `NIGHT  DESK` stays only in D.K.'s own log |
| P5 | Phone list | `PANEL  299` | `CONTROL RM  299` | Nobody phones a panel. The control room is the separate locked room of `40` §1, and the line quietly says it exists |
| P6 | Sign-out, names | row 1 `D.K.` 0810; row 3 `T.R.` 1120; row 5 `L.H.` 1500 | `A.W.` / `AW`; `G.S.` / `GS`; `N.F.` / `NF` | D.K. works 2300–0700. T.R. and L.H. are visitors, and the register shows them out of the building at those times |
| P7 | Sign-out, first row's DATE | empty, no runtime slot | add a `sign_out_date` slot to `date_slots.json`, same date as the register | Otherwise the ditto marks below it copy a blank |

`STOCK RM 3` in row 6 of the register and in the log's `0112` line is a harmless echo; keep it. Do not add a second hint of the electrician (`01` §5).
