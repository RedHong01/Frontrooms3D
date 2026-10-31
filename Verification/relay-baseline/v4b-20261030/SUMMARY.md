# V4b indicative bot round, 2026-10-30/31

关卡设计调研. Asked by 系统设计: can a player who flees escape a close first sighting under C37 = 3? (Catch stays 0.7 m; a visit's first lock-on within 6 m holds 1.7 s; RELAY §8.) This is **indicative, not a grade**: G5 is graded after V5 (eased doors). Started on account B at 22:40 and finished on account A after switch #10 (00:1x). One round, per rule 23.

## Setup

- **Code under test:** main at 22:3x with V4b behind `FRONTROOMS_RELAY_V2` (MapHunter a716d075, V4b 7e8561ca, Game 4f075b85).
- **Clones:** `proj66v2` and `proj66`, both with the Standalone define and the CloneOnly silencer.
- **Batch settings:** `-autopilotExit -nographics`. The replay is identical to a run with graphics (checked on noisy 2554 T5); the only differences are render errors, which `report.py` filters. Clone-only `ExitTimeoutSeconds` 3000, because a 10-min run after a 180 s gate passes 600 s.
- **Harness r4**, landed with this data (all new code is behind the define; with it off, the harness is unchanged):
  - the evader's reaction starts at `TargetAcquired`, and evading reads `relay.LockedOn` through the hold;
  - `lockOnLog`, plus per-chase lockOn / lockMetres / lockHold / lockLong / lockReacquired / lockTouch;
  - the director block adds targetAcquisitions, longLockHolds, gateHolds, gateHoldSeconds and chaseNumbers;
  - V4a: `heard` reads `relay.LastHeard`;
  - hunt cause `dispatch`: a visit's first leg from Listen, sent by the director;
  - `-autopilotEvadeLure`, with the `-lure` suffix: the bot breaks a window while the Relay is off the map (a Device call), walks while it is on the map, and never sprints before the cue.

| Set | Runs | What |
|---|---|---|
| `./` plain | 24 | evaderv2 + evader06, 6 seeds × T1/T5, 10 min |
| `./` lure | 48 | evaderv2 + evader06 with `-autopilotEvadeLure`, 12 seeds (2554 20388 7 101 4242 9001 13 77 555 31337 8080 1999) × T1/T5, 6 min |
| `./` reference | 8 | noisy + closedzone, 2 seeds × T1/T5 |
| `whatif-hold07/` | 24 | evaderv2 lure with **`CloseLockOnExtraSeconds = 0`** (no 1.7 s hold; clone only) |
| `whatif-radius9/` | 24 | evaderv2 lure with **`CloseLockOnMetres = 9`** (clone only) |

All 128 runs PASS, with 0 errors after the render filter. The what-if constants were never in main, and both clones were resynced from main afterwards.

Tools in this folder:
- `report.py <folder>`: per-chase table.
- `whatif_pair.py`: the paired comparison by seed and tier.

## Findings

1. **A plain walker meets almost nothing.** In 10 min there are 2 director calls at T1 and 3 at T5, and the Relay is off the map 85–95 % of the time. All 5 plain-round chases came from one encounter (seed 101, the station's Device call). This is why the lure exists.
2. **Escapes by lock-on kind** (evaders' evaded chases, base constants, `./`):

   | Lock-on | Escaped | Median lock distance | Median chase start |
   |---|---|---|---|
   | long 1.7 s (≤ 6 m) | 15/31 (48 %) | 3.9 m | 4.5 m |
   | first lock-on past 6 m, 0.7 s | 4/16 (25 %) | 9.0 m | 7.0 m |
   | re-acquired, 0.35 s | 9/14 (64 %) | 10.2 m | 11.3 m |

   **A close first sighting is now more escapable than a far one.** Past 6 m the 0.7 s hold is all a bot gets.
3. **The 1.7 s hold is what makes close first sightings escapable** (evaderv2 lure, 24 runs per arm):

   | Arm | First chase escaped | Close lock-ons escaped | All evaded chases | Caught runs |
   |---|---|---|---|---|
   | base (6 m, 1.7 s) | 8/20 (40 %) | 6/15 (40 %) | 13/28 (46 %) | 15/24 |
   | no extra hold (0.7 s) | 3/20 (15 %) | 1/14 (7 %) | 6/23 (26 %) | 17/24 |
   | hold radius 9 m | 10/20 (50 %) | 9/18 (50 %) | 13/26 (50 %) | 13/24 |

   - Paired by encounter, removing the hold turns 5 first-chase escapes into catches (0 the other way).
   - Widening the radius to 9 m adds 2 escapes (0 the other way), and the far "first" kind shrinks from 6 chases to 3.
4. **By tier** (base, evaderv2 lure): T1 9/17 escaped (53 %), T5 4/11 (36 %). These sit at G5's bars (≥ 50 % / ≥ 35 %), but under lure conditions and before V5's eased doors, so they are a sign, not a grade.

## For 系统设计 / Red

- C37 = 3's hold does its job: without it, close first sightings are 7 % escapable.
- A 9 m radius buys about +10 points on the first chase (40 → 50 %) and covers most "far first" lock-ons, which escape only 25 % today.
- Neither number needs a decision until V5 lands and G5 is graded. This data goes to 系统设计 for that ask.
