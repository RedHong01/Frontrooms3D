# Recording mark · 20 · watermark for gameplay recordings, and type and yellow in the input bar

Line: 平面视觉 · 印刷与演示. Stage: decided. **Red, 2026-11-01 23:00, in this line's chat, to "水印和输入条都按推荐（M1 加 A2）吗？": "全部按推荐".** So: M1 on clean clips, A2 (logo in the bar, game answer yellow 1 s then keyline) when there is a bar. 周志 installed the audited tool as `input_band.py` 11-01 23:01 (sha1 d6d7f235, identical to `input_band_audited.py`) and owns the rollout. The Shot format is unchanged; nothing landed in the game.
Red, 2026-11-01 17:54: "同时可以合理有效的使用上accent 黄色，目前没有被很好的使用上，其次就是我需要平面视觉去develop不妨碍观看的水印。以及目前现在这种 input visualization没有真的visualize 游戏中的input".
Reused: 周志's real recording `W/input_band/real/doors/` (clean.mp4 and input.jsonl, seed 516574485, tag SCRIPT), tool `W/tools/input_band/`, first-pass sheets of this folder. No Unity, no download.
Choice in Chinese: `30_for_red.md`. Sheets: `sheets/mark_options.jpg`, `sheets/bar_yellow_options.jpg` (`options_fit.txt` ends ALL LABELS FIT). Motion: `video/mark_motion_test.mp4`, `video/bar_audit_motion.mp4`, silent. Tools: `~/FrontRoomsVisualWork/tools/recording_mark/`. Sources: `SOURCES.md`.

## Findings on real footage
- **The first M1 failed on the real recording.** It chose lift or darken per frame with a band at luma 0.27-0.33. The real clip's corner has a median of 0.33, so the mark changed direction 3 times at slot 1176 and 4 at slot 540 in 9.3 s. It blinked, in a game where a flicker means something. The earlier sheets used stills and could not show this.
- **Fix:** the direction is chosen once per shot from the whole shot (`plan_shots`; a shot ends only at a hard cut), and the strength keeps one tone step, DELTA 0.17 in gamma space, on every ground (`KMAX` 0.60; darken only above a shot median of 0.62).
- **Measured after the fix, through H.264 CRF 20 at each file's raster, median and range of the letters' tone step, 0 direction changes on every clip:**

| Clip | Raster, slot | Letters in source | Ground min / median / max | Tone step min / median / max |
|---|---|---|---|---|
| real recording | 1920x1080, 576 | 40 px | 0.10 / 0.35 / 0.46 | 0.14 / 0.17 / 0.22 |
| real recording | 1920x1080, 1176 | 20 px | 0.09 / 0.32 / 0.44 | 0.13 / 0.16 / 0.21 |
| W6_lamp_warning | 1280x720, 576 | 27 px | 0.28 / 0.30 / 0.30 | 0.16 / 0.16 / 0.16 |
| W6_broken | 1080x600, 576 | 22 px | 0.54 / 0.58 / 0.58 | 0.15 / 0.16 / 0.18 |
| W6_read_closeup | 1080x600, 576 | 22 px | 0.46 / 0.57 / 0.69 | 0.06 / 0.17 / 0.19 |
| W6_unlock | 1080x600, 576 | 22 px | 0.14 / 0.36 / 0.57 | 0.10 / 0.18 / 0.23 |
| W6_pickup | 1400x480, 576 | 29 px | 0.43 / 0.44 / 0.44 | 0.16 / 0.17 / 0.17 |
| W7_broken | 360x200, 576 | 8 px | 0.54 / 0.58 / 0.58 | 0.14 / 0.14 / 0.15 |
| W7_unlock | 360x200, 576 | 8 px | 0.13 / 0.36 / 0.57 | 0.08 / 0.14 / 0.18 |
| 04_locked_has_key | 1920x1080, 1176 | 20 px | 0.13 / 0.39 / 0.39 | 0.14 / 0.16 / 0.21 |

  The tone step survives on the 360x200 clips. Judged 11-02 at deck size beside their 1080x600 twins: the letters are as soft as the picture and still read as the word, so these clips are marked too.
- **The accent is #F4DF3B, not #FCF7CE.** #FCF7CE is the canary paper of the forms (`PRINT_FORM_SYSTEM.md` §4). RULES rule 9 now says so.
- **Logo floor:** minimum lockup height 12 px on screen (`AD_TYPE_SYSTEM.md` §6). Today's on-picture mark is 18 px at slot 1176 and 9 px at slot 576, under the floor below slot 784.
- **The on-picture logo was never in the Shot format.** `SHOT_FORMAT.md` §2 keeps it in the header band, and §1 exempts decks. The films burned it by hand (`W/run_b66/video/build.py`). **The Shot format does not change under any option here.**
- **Corners:** the HUD holds top left and bottom left, the phone's pause button top right. Lower right is the free corner, and in first-person footage it is usually floor. A corner bug in lower right is also standard practice (S1, S2).

## The mark on a clean clip: options on the sheet
| | What the viewer sees | For | Against |
|---|---|---|---|
| TODAY | white wordmark, top right | strong | brightest thing in a dark frame; under the logo floor below slot 784; replaces pixels |
| M1 TONE, REC | wordmark in the picture's own tone, lower right, 12 px | no pixel replaced, pattern shows through, 0 flips, same strength in dark and light | faint by design; busy wallpaper at a small slot swallows part of it; one render per slot |
| M2 SSS | the SSS mark in the same tone, 16 px | smallest, 0.03 % of the frame | does not spell the name. Kept as M1's fallback under slot 426 |
| M3 CHIN | ink strip of 24 px added under the picture, white wordmark | picture untouched, full strength, no exception to any rule | file is 16:9 + 24 px, slots change, no bleed; six strong logos on a 6-up page |
| M4 HEAD | white wordmark for the first 1 s and last 0.5 s (S2 offers timed display too) | nothing on the picture while it plays | a clip cut mid-way has no mark; pops on at every loop on a slide |

M3 replaced the first sheet's EDGE, which covered 3.6 % of the picture ("evidence is never covered").

## Type and yellow in the chosen bar: sign-off
Red chose the ink bar with yellow keys and "不应该是一直使用accent黄色，而是在映射（有玩家输入的时候用这个黄色）" (11-01 ~20:0x, in the 周志 chat). The sheet's Y1-Y4 are closed by that. Answers to 周志's four questions:
1. **Bayon 20 and Plex Mono 13: accepted, with corrections.** P1 Label is Bayon 20/20 caps **+3 %**; the tool had no tracking. P1 Meta is Mono 13/**16**; the tool's rows stood at pitch 13. Digits and "·" in Bayon words fall back to Plex Mono Medium (Shot format §3: Bayon draws 1 and 0 as I and O, and has no dot).
2. **Mono inside media: accepted as one named exception.** The rule "small mono only in the running header" is about captions on slides. LOOK, the row names and the source tag are the instrument's own labels, as on the game's key map (`FrontRoomsKeyMapView`). Applies to this bar only.
3. **Yellow: A1 and A2 add only what follows from Red's rule.** A game answer is yellow for 1 s, then a keyline, so at most one yellow flag shows. A held row and a moving LOOK trace have a 6 px yellow live end, in step with the lit key. TODAY keeps up to three yellow flags for 6 s each. This is Red's call: `30_for_red.md`.
4. **Slots: the policy's totals are stale** (758 / 589 / 408 for a 96 band). The band is 112 now:

| Slot | Picture + bar | Ends at y, title at 232 (margin 1008) |
|---|---|---|
| 1176 | 662 + 112 = 774 | 1006 OK |
| 1026 | 577 + 112 = 689 | 921 OK |
| 876 | 493 + 112 = 605 | 837 OK |
| 726 | 408 + 112 = 520 | 752 OK |
| 576 | 324 + 112 = 436 | 668 OK; two rows do not fit (1128), so 6-up pages use clean clips |
| 426 | 240 + 112 = 352 | 584 OK |
| 276 | 155 + 112 = 267 | 499 OK |
| 1176 at k 1.4 | 662 + 157 = 819 | **1051, over.** Use 1026 at k 1.4: 734, ends 966 |

**Grid defect in the tool on an ink bar:** keys started at x 0 and the tape ended on the slot edge. On a white slide the white keyline vanishes into the slide. The audited tool keeps 12 from the sides, as from top and bottom; columns 2 onward do not move. Pad layout adjusted the same way.
**Grid defect on the deck:** the VIDEO SLOT frames on 2159:765/790/807/832 are 540 wide at x 72/690/1308, y 210 and 620. On the 12-col grid three across are 576 wide at x 72/672/1272, from y 232.

The audited tool is `tools/recording_mark/input_band_audited.py`, diff against 周志's file `input_band_audit.diff`. `--check` passes: 0 failing frames at slots 1176, 876, 576, 426, 276 and pad 1176. `--no-logo` gives A1.

## Logo in the bar (A2)
White wordmark, letters 14 px as tall as Bayon 20's caps (Shot format §4), right end, one gutter from the tape. The tape goes from 660 to 531 px, 6 s at 89 px per s. At slot 876 it has 231 px, and words cover about half. Slots 450-699: logo on the second key row, last word on the first. Under 450 the bar has no room, so the picture carries M1.

## Clips, if Red takes the recommendation
- **M1 at their slot:** W6_lamp_warning, W6_eye_gate, W6_read_closeup, W6_locked_no_key, W6_unlock, W6_broken, W6_pickup, W7_key_panel.
- **No new mark:** W6_wall_arrows, W6_wall_cue_walk (Shot films with the logo in their header band).
- **Logo burned top right, caption band at the foot:** W7_door_break, W7_door_vision, W7_run_room. **On hold (总控, 11-02): they stay as they are until their owning lines are awake.** Then: rebuild each film without its logo line (`im.paste(logo, …)` in `W/door_break/video/build_prod.py`, `W/door_vision/video/build.py`, `W/run_b66/video/build.py`; their frames are on disk: `W/door_break/P6/out/B_video2`, `W/door_vision/out/film`, `W/run_b66/video/frames_raw`), into a new output, never over the original, then M1 like any clip. M1 then falls inside the film's own ink caption band, as a quiet grey word at its right end. Never `delogo` for the deck.
- **360x200 sources:** W7_eye_gate, W7_read_closeup, W7_locked_no_key, W7_unlock, W7_broken: M1 at the source raster (letters 8 px), no upscale. Checked 11-02, it reads.
- **Placed historical clips** (2122:767, five MP4s): no change unless Red asks.

## Not done, not verified
No real projector: the projector rows are simulated. No Figma playback of a marked clip, and no MP4 placed. The motion MP4s were measured (0 direction changes over 639 frames); of their frames, only 3.3 s of the mark test and 7.0 s of the bar test were looked at, and nobody has watched them play. Live data: the log is a scripted player on the real controller (SCRIPT), not a person. The Figma connector is not on this session (11-01 23:00: only Claude Docs), so no VL slide; VL row and slide go in the line's next daily batch.
