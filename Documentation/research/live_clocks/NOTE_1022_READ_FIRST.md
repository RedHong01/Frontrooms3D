# NOTE 2026-10-22 14:2x: read first (from the 游戏视觉 session, for every stage of live-clocks)

**1. Ownership correction:** `research/computer_ui/*` (10_research.md, 20_design.md) is **系统设计's spec** (20_design.md v1.2 header), not 游戏视觉's.
- Do NOT edit any file in research/computer_ui/.
- Put the needed changes in `NOTES_for_peers.md` for 系统设计: mark the frozen clock superseded at 10_research.md:556, 20_design.md:571 (§8.6), :764, :1277. The 游戏视觉 session has asked 系统设计 to do it.
- The docs you may edit are `research/live_clocks/` and `research/inspect/` (the S5 wall-clock entry).

**2. Already done by peers (do not redo):**
- **平面视觉 · 信息设计:**
  - updated its brief (graphic_visual_briefs/information_design.md:74) and GRAPHIC_VISUAL_TASKS §3h: header = program · user · live local time;
  - re-rendered the G0 terminal masters in Figma 3218:7277 (TM01–TM04; `g0_pages.py` reads the clock at render time).
- **系统设计:** RELAX stays as decided (C01, no jitter). Live clocks are fine. Recorded in RELAY_PURSUIT_REDESIGN.md §11, which also says the old 10:08 freeze reason no longer applies.

**3. Display format for every SCREEN fed by FrontRoomsRealTime** (信息设计's rule):
- **Time:** US 12-hour `h:MM AM` / `h:MM PM` (no leading zero; period-correct for a US office), right-aligned in the header bar.
- No seconds and no blinking colon. It changes once a minute, so it never reads as a timer and adds no 1 Hz flash.
- **Dates:** the real month and day, year 1990, the 1990 weekday, e.g. `MON 10/22/90` for 2026-10-22.
- FrontRoomsRealTime should offer exactly these strings: `TimeText` "h:MM AM" and `DateText` "DDD MM/DD/90". Allocation-free, recomputed once a minute.
- **The analog wall clock is not a screen:** its red second hand steps once a second as designed.

**4. From 叙事设计 (14:3x): the time model and the computed dates** (`research/station/01_research.md` §5, 9cbeb487).
- **The time model:** the clocks are right, and the building's hours are not. Whatever the clock says, the building runs its night. This is never stated in the game.
- **FrontRoomsRealTime must also provide `NightShiftDateText`** for the Guard's Daily Activity Report header (EXAMINE text, station §6). The header reads `<MM-DD-90>  NIGHT  DESK`, where the date is the night shift that began at 2300:
  - on (in-game date − 1) when the in-game time is ≥ 07:00;
  - otherwise on (in-game date − 2).
  - Format `MM-DD-90`, from the real month/day mapped to 1990.
  - Test: 2026-10-22 14:25 gives `10-21-90`, and 2026-10-22 03:00 gives `10-20-90`. Check month and year boundaries (Jan 1 → `12-30-89` / `12-31-89`) and Feb 29 (1990 has none; map it to 02-28).
- **EXAMINE text substitution:** add a token (for example `{NIGHT_SHIFT_DATE}`) that the inspect text system replaces at display time. The DAR entries themselves are fixed strings (叙事's).
- **Printed dates are 1989 or earlier.** Anything printed (placards, labels, papers) is never ahead of the live calendar, and anything that must look recent is computed.
