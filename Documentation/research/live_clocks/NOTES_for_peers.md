# Live clocks: notes for the peer lines (from 游戏视觉)

**Written:** 2026-10-22 15:3x by the live-clocks build stage (游戏视觉 workflow `live-clocks`, account A run).
**Why:** Red (游戏视觉 chat, 14:1x) asked that the clock, and every device in the game that shows time, run and match the real-world time: “如果是旧的我希望新的钟表是可以正常走动，并且呼应现实世界的时间的（钟表只是一点，还有游戏里面的电脑等一系列有时间显示相关的设备的）”.
**Design:** `10_design.md` (this folder). **Build report:** `20_build.md`. **Package:** `W/apply/live_clocks.sh` (landed by the 游戏视觉 session, not by this stage).

None of these notes asks for a decision unless it says "your call". This file is the only channel: the stage sends no messages.

## What exists once the package lands

- **`FrontRoomsRealTime`** (`Assets/Scripts/Rendering/Clock/`): one static service for the whole game.
  - It reads the player's local wall time: the device clock and zone, or the browser's zone on WebGL (`Assets/Plugins/WebGL/FrontRoomsRealTime.jslib`). It runs on real time, never `Time.time`, so it keeps going in pause and in EXAMINE.
  - **Fields:** `LocalNow`, `Hour`, `Minute`, `Second`, `SubSecond`, `HourF` / `MinuteF` / `SecondF`, `EraToday` (1990-MM-DD), `EraWeekday`.
  - **Strings** (cached, rebuilt only when they change, no garbage): `TimeText` `2:25 PM`, `DateText` `MON 10/22/90`, `DateDashText` `10-22-90`, `NightShiftDateText` `10-21-90`; `WriteTimeText(char[], int)`.
  - **Events:** `SecondChanged`, `MinuteChanged`, `DayChanged`.
  - **Pin for captures:** `-frClock HH:MM:SS` (fixed) or `-frClock HH:MM:SS+` (running from it), `-frClockDate MM-DD`, or `FrontRoomsRealTime.Pin(h, m, s)` / `PinDate(m, d)` / `Unpin()`.
  - **Tokens** in EXAMINE transcripts: `{NIGHT_SHIFT_DATE}`, `{DATE}`, `{DATE_DASH}`, `{TIME}` (replaced when the transcript is shown).
- **The wall clock** `Kit_WallClock`: same name, GUIDs, envelope and origin; a hero rebuild with three hand kits (`Kit_WallClock_HandHour` / `_HandMinute` / `_HandSecond`). `FrontRoomsKitLibrary.Spawn` attaches `FrontRoomsWallClock` to kits tagged `live_clock`. The red second hand steps once a second with a short settle; the other hands move with it.
- **Gameplay never reads the clock**, and the clock reads no gameplay state.

## 系统设计 (FYI; nothing to decide)

- You kept RELAX as C01 with live clocks (RELAY_PURSUIT_REDESIGN §11 "Countable RELAX"; systems.json, LANDINGS 14:28). This build changes no gameplay: the director, RELAX, the dry clock, the tier clock and `elapsed` stay on game time.
- **Text still to update in `research/computer_ui/`** (yours; this workflow does not edit it). These were "unchanged" when the design was written (10_design.md §9.2):
  - `10_research.md:791` (ALARM HISTORY: "no live clock, so RELAX cannot be counted down"): the reason no longer holds. Keep the entries without timestamps (a timestamped RESET would hand over the exact RELAX start) or reword. Your call.
  - `20_design.md` AT-T09 ("No RT render happens without an event") and AT-T10 ("0 UI renders on idle frames"): add the minute (the header clock) to the events, and "except the header's once-a-minute render".
  - `20_design.md` AT-T16 (the FRONT glance texture hash is identical across states): the hash now changes every minute. Pin the clock in that test: `FrontRoomsRealTime.Pin(10, 8, 37)` or `-frClock 10:08:37`.
  - `00_interaction_inventory.md:463` ("the clock frozen at 10:08:37") → "the wall clock shows the live time".
  - `SOURCES.md:201`: optional "(superseded 10-22)".
- **Terminal header** (when it is built): read `FrontRoomsRealTime.TimeText`, re-render the FRONT RT on `FrontRoomsRealTime.MinuteChanged` (once a minute per active terminal, never per frame).

## 平面视觉 · 信息设计

- Your format is implemented as you wrote it (NOTE_1022_READ_FIRST §3): `TimeText` = US 12-hour `h:MM AM` / `h:MM PM`, no leading zero, no seconds, no blinking colon; `DateText` = `MON 10/22/90`.
- The widest `TimeText` is `12:59 PM` (8 characters; test E6 checks all 1,440 minutes). Please confirm the header slot fits 8 characters right-aligned.
- The analog wall clock is not a screen: its second hand steps once a second, as you noted.

## 叙事设计

- Your rules are implemented once, in the service (tests E5, E14):
  - a real Feb 29 shows Feb 28 (`WED 02/28/90`), never ahead;
  - `NightShiftDateText` = EraToday − 1 day from 07:00, else − 2 days, formatted `MM-DD-90`; all six cases of 10_design.md §5.3 pass, including `12-31-89` / `12-30-89` on Jan 1 and `02-27-90` around Feb 29;
  - date arithmetic is done in the 1990 calendar, so it crosses into 1989.
- The Daily Activity Report header can be written as `{NIGHT_SHIFT_DATE}  NIGHT  DESK` in the EXAMINE transcript; `FrontRoomsInspect.TranscriptText` replaces the token when the close-up shows it. The entries stay your fixed strings.
- Our inventory entry `research/inspect/01_inventory.md` S5 no longer says "Time has stopped" (it goes in with the package).
- **Your call:** the "because time here is wrong" lines (`wallpaper_motion/30_narrative_phosphor.md:595`, `20_level_design_phosphor.md:867`), and `R.M. 6/90` (A.11, frozen ink), which lies in the future of the live 1990 date from January to May.

## 平面视觉 · 广告

- If the off-air bars are ever built in the game, the time and the date are a runtime CG overlay fed by `TimeText` and `DateDashText`, redrawn on `MinuteChanged` (10_design.md §8). Nothing to do now.

## 声音设计 (FYI)

- No clock sound was added. A 1 Hz quartz tick is period-true, but it could read as a timer cue: your call.
- Hooks if you want one: `FrontRoomsRealTime.SecondChanged` (the step), and the clocks' positions (every `FrontRoomsWallClock` in the scene; `FrontRoomsWallClock.Count`).

## 关卡设计 (FYI)

- No module or map change: the kit keeps its name, GUIDs, envelope and wall origin; `L0_WaitingRoom_4x3` places it as before (y 2.1, yaw 180).
- `Kit_WallClock_Hand*` appear in the Level Designer palette, as the closer and lock parts do (tags `clock_hand`, `part`). Hiding kits tagged `part` is optional and yours.
- Each placed clock adds 3 child renderers (the hands, culled at 18 m at lodBias 1).

## WebGL track (FYI)

- New `Assets/Plugins/WebGL/FrontRoomsRealTime.jslib` and one `#if UNITY_WEBGL && !UNITY_EDITOR` branch in `FrontRoomsRealTime`: the zone offset always comes from the browser. Mac / Windows / iOS / Editor compile none of it (checked: their assemblies carry no `FR_*` import).
- The probe results (what `DateTime.Now` and `TimeZoneInfo.Local` return in a Unity 6000.3 WebGL build) are in `20_build.md` §G6.
- The clock kit has no WebGL-specific asset; WebGL High (lodBias 1) culls the hands at 18 m.

## Every capture harness (all visual lines, codex audit)

- **Pin the clock** in any deterministic capture, pixel diff, hash test or signature dump: `-frClock 10:08:37` (or `FrontRoomsRealTime.Pin(10, 8, 37)`). Without a pin the hands show the real time and frames differ run to run.
- `Kit_WallClock`'s signature changes once: triangles, submeshes (3 slots, the red one moved to the second hand) and 3 child hand objects.
- Never flag the hands Dynamic for the Glass RT (a stepping hand would reset reflection accumulation every second).
