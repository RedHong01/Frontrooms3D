# Inspect research: sources and media ledger

Folder: `Frontrooms3D/Documentation/research/inspect/`. This ledger covers the frames in `images/r_*.jpg`, made for `02_game_research.md` on 2026-10-07.

**Nothing was downloaded for this research.**
- Every frame below is built from media already on disk (credited to the ledger it came from) or from our own renders.
- Official Steam trailers and store screenshots were **viewed** in the browser pane to measure timings. Nothing from them was saved or put in a frame.
- Wanted downloads are listed in `media_candidates.md` and wait for Red's approval.

Rules carried over from `Research/week02/ip-research/SOURCES.md`: copyrighted game footage is quoted in short excerpts for course research only. Never ship it in the game.

Other stages may add their own sections below this one.

---

## 1. Frames in `images/` (02 game research)

All are JPG q85, built with `W/venv` PIL by the scratch script `make_frames.py` / `make_diagrams.py` (session scratchpad, not in the project). The header text on each frame repeats its credit.

| Frame | Built from (on disk) | Original source and owner | Upstream ledger |
|---|---|---|---|
| `r_01_exit8_rules_walkup.jpg` | 4 stills (0.0 / 1.5 / 3.0 / 4.4 s) from `Research/week02/phosphor-narrative/clips/gn_exit8_rules_sign.mp4` | IGN, *The Exit 8 – Official PlayStation Launch Trailer* (2024-08-11), https://www.youtube.com/watch?v=pDTFOTTlw7I, 0:02.5–0:07.0 · © KOTAKE CREATE / PLAYISM | `Research/week02/phosphor-narrative/SOURCES.md` |
| `r_02_exit8_posters.jpg` | `Research/week01/assets/clips/EX8_1_loop.jpg`, `EX8_3_anomaly.jpg` | The Exit 8 Steam trailer, 0:00–0:06 and 1:12–1:16 · © KOTAKE CREATE / PLAYISM | `Research/week01/assets/clips/for-slides/INSERT_VIDEOS_INSTRUCTIONS.md` (slot table) |
| `r_03_lethal_terminal_live.jpg` | `Research/week01/assets/clips/LC_5_bestiary.jpg` | Lethal Company trailer (Zeekerss). Cut 2026-09-24; **the trailer time was never logged** (not in the week01 slot table) | none: a gap in the week01 ledger |
| `r_04_alien_tracker_realtime.jpg` | `Research/week03/touch-research/stills/alien_iphone.png`, cropped to x 0–1300 (drops the store banner) | Feral Interactive, Alien: Isolation iOS App Store screenshot 1 (2021) · © SEGA / Creative Assembly / Feral Interactive | `Research/week03/touch-research/SOURCES.md` |
| `r_05_darkdeception_live_map.jpg` | 3 stills (0.3 / 2.0 / 4.0 s) from `Research/week01/assets/clips/DD_2_counter.mp4` | Dark Deception Enhanced Trailer 0:23–0:27 · © Glowstick Entertainment | `INSERT_VIDEOS_INSTRUCTIONS.md` slot table |
| `r_06_frontrooms_placard_distance.jpg` | `Documentation/research/placard/images/q16_u1_A8_walk_2m.jpg`, `q16_u1_A3_legend_0p6m.jpg`, `q16_u2_A4_legend_0p6m.jpg` | Our renders (placard workflow, 2026-10-07). The art is 平面视觉's | `Documentation/research/placard/SOURCES.md` |
| `r_07_measured_transitions.jpg` | Diagram. Numbers from §2 below and from `Assets/Scripts/FrontRoomsShots/FrontRoomsShotTimings.cs` (read only) | ours | this file |
| `r_08_design_space.jpg` | Diagram. Placement of each game from `02_game_research.md` §2 | ours | this file |

## 2. Viewed, not downloaded: timing measurements (M1–M6)

Method:
- The official trailer's HLS stream was played in the browser pane with hls.js 1.5.13 (cdnjs).
- Frames were stepped by setting `currentTime` and drawn to an on-page canvas, then looked at in pane screenshots.
- Steps were 1 s, then 0.5 s, then 0.05–0.1 s around each event.
- Times are seconds into the Steam movie.
- Steam movie ids come from the public `https://store.steampowered.com/api/appdetails?appids=<id>` JSON (the text JSON was read; no media).

| # | Trailer (Steam app · movie id) | Stream | Ranges looked at | Used for |
|---|---|---|---|---|
| M1 | Amnesia: The Bunker – Gameplay Demo (1944430 · 256948231) | `store_trailers/1944430/570228/…/hls_264_master.m3u8` | 0–607 s at 15 s; 238–286 s at 1 s; 257.6–259.4 s at 0.1 s | Note-to-journal timing; hand frozen while reading |
| M2 | Amnesia: The Bunker – Halloween Update Trailer (1944430 · 256978123) | `store_trailers/1944430/622444/…` | 0–141.5 s at 5.9 s; 55–66.5 s at 0.5 s; 57.9–58.75 s at 0.05 s | Inventory pop-in; "NO PAUSING IN INVENTORY" card |
| M3 | Gone Home – Launch Trailer (232430 · 2029688) | `store_trailers/232430/14286/…` | 0–88.7 s at 2.2 s; 18.5–24.5 s at 0.25 s; 21.75–22.6 s at 0.05 s | Note lift to the camera |
| M4 | SILENT HILL 2 – SH2_Gameplay (2124490 · 257026897) | `store_trailers/2124490/706859/…` | 0–825 s at 20.6 s; 362–385 s at 1 s; 370.2–371.3 and 373.5–374.6 s at 0.1 s; 608–631 s at 1 s | Wall-photo close-up: cut in, hold, cut out |
| M5 | SIGNALIS – Gameplay Overview (1262350 · 256910985) | `store_trailers/1262350/504958/…` | 0–75 s at 1.9 s; 18–24 and 56–62 s at 0.5 s | First-person panel close-up; 3D documents |
| M6 | Dead Space – Extended Gameplay Walkthrough (1693980 · 256911793) | `store_trailers/1693980/506378/…` | 0–485 s at 12 s; 333–357 s at 1 s; 466–485 s at 0.8 s | Text hologram read in the live world |

Full m3u8 URLs are in `media_candidates.md` §B.

Also viewed: Visage Official Gameplay Trailer (594330 · 256727786), 0–146.6 s at 3.7 s. No inspect view was found.

Store screenshots: all 163 store screenshots of the 18 apps in `media_candidates.md` §A were viewed as 600×338 thumbnails. Picks are listed there.

YouTube: one page was opened, GameSpot "Maiden - Full Resident Evil Village Demo (4K)" (https://www.youtube.com/watch?v=fYbRaLxahxc). It is age-restricted and needs sign-in, so it was not played. No sign-in was attempted.

## 3. Text sources

The numbered web sources S1–S47 (URL, date read, what each supports) are in `02_game_research.md` §9. Tags used there:
- `[S#]`: the page was opened and read.
- `[search]`: a search-engine summary only; the page was not opened.
- `[PLAY]`: play or footage memory, UNVERIFIED.

## 4. Design diagrams in `images/` (10 design, 2026-10-07)

JPG q85, 1920 × 1080, drawn with `W/venv` PIL by scratch scripts (`d01.py`–`d03.py`, session scratchpad, not in the project). Support diagrams, not verification images, so they have no VERIFICATION LOG row. Nothing was downloaded.

| Frame | Built from |
|---|---|
| `d_01_inspect_flow_timeline.jpg` | Drawn from `10_design.md` §2.3–§2.4 (states, times, events). No media |
| `d_02_placard_base_vs_zoom.jpg` | Our own render `research/placard/images/q16_u1_A3_legend_0p6m.jpg` (visual chat, clone `proj_placard`, art v2 by 平面视觉). Left: the render scaled to the panel. Right: crop x 477–1435, y 255–794 of the same render, ×2 bicubic, which simulates the 0.18 m zoom step. Bottom strip: crop x 800–1012, y 716–748 at 1:1 and ×2 |
| `d_03_inspect_poses.jpg` | Drawn from `10_design.md` §3.3 numbers (side views). No media |
