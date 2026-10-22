# 40 · G14 ray-traced glass: Red's decisions D1, D6, D2 (record)

2026-10-22 · 游戏视觉 · rewritten by the G14 workflow, fix round 4 (14:0x) · details and evidence: `30_final.md` §15 (fix round 4), §14 (fix round 3), §13 (review round 3).

**Red answered at 12:36:** "G14 的 D1、D6、D2 全部按推荐" (D1, D6 and D2 all as recommended). The 游戏视觉 session wrote the answer down from Red's own words in `NOTE_1022_red_decisions.md`. **This file now records those decisions. It asks nothing.** It replaces the 13:17 version, which still asked the three questions. Review round 4 found that version out of date: the note existed from 12:36, but fix round 3 missed it. The cost numbers below are also corrected (review round 4): they are ranges over all 21 player runs of the landing GPU code, and they show the quiet runs on their own.

## What was decided

| | Decision | Built where | What the player gets |
|---|---|---|---|
| **D1** default quality | **(b) now:** High at every quality level from 3 up. Ultra only when asked for: the `-frGlassRT ultra` argument or the `FrontRooms.GlassRT` setting. **(c) next:** RT on by default only on GPUs with at least 16 cores (M3/M4 Pro and Max). Smaller GPUs start with today's glass and can switch RT on. | (b): this package, `FrontRoomsGlassRTSystem.DefaultQuality()`. It is applied in the clone (fix round 4) and proven in the player (below). (c): the `g14-followup` package. It needs a local IORegistry core-count query (no download) and a test with a forced core count. | The same look as Ultra in play (edge shimmer 0.014 vs 0.014, carpet match 3.0–3.4 % vs 2.9–3.2 %), for 1.8–10.2 ms less per frame at 1440p on this Mac. |
| **D6** break-shot cost | **(a) now:** accept it. The shot traces every lamp in range. **(b) with P2:** a 4-lamp budget during the 1.65 s hold, added in the same change as P2's per-hit lamp cull. | Nothing to build now. (b) goes to the glass-rt track with P2. | The full reflection through S0–S2. |
| **D2** player body in reflections | **(b) now:** a dark, body-sized silhouette at the camera. Only reflection rays see it, never the camera. It covers lit windows too: the Relay's ghost 2 m behind must no longer show where your body should block it. **(c) later:** the first-person body model replaces the proxy once it exists. | (b): the `g14-followup` package, after G14 lands (it touches the same files). The silhouette art (proportions, coat, 1990 office clothing, no face detail) is 游戏视觉's. | Someone stands in the mirror where you stand. |

**Store builds:** until D1 (c) lands, no Mac **store** build that contains the G14 dylib goes to players. Snapshots and class builds are fine. App Store · 构建 has been told (Red's note).

**Sign-offs:** VL316 (dark window) and VL284 (showcase) are signed off on the `g14-followup` build with the body proxy, not on this landing. Their chips are now PENDING, not WAIT-RED.

## Where the package stands

- **D1 (b) is in the clone `W/proj_rt_eng3`.** It is one line in `FrontRoomsGlassRTSystem.cs` (l. 194) and the header comment of `FrontRoomsGlassRT.cs` (l. 6–7), so the payload now changes 10 paths (fix round 3 changed 9).
- **Proof:** the universal player built this round (`ps16`, native) starts at quality level 5 (Ultra) and logs "default RT quality High". Its run ends "on: Apple9, High", 0 errors, and the 600-frame walk ran at High. The same player under Rosetta 2 (`ps17`) gives the same result.
- Roslyn `--all`, macOS and iOS defines: COMPILE OK on the clone, and on main `2c450e11` with the 8 payload `.cs` files.
- **The package in `W/apply/glass_rt_g14*` is still the round-2 package** (dylib `ec31f4fa`, default Ultra). It must not be applied. Next come review round 5 on the clone and then the package stage (`30_final.md` §15.5).

## The numbers behind D1 and D6

**How to read them.**
- **Machine:** this Mac only (M3 Max, 40-core GPU). We have no other Mac.
- **Load:** every timing was taken while other jobs ran (UNDER LOAD).
- **Added frame time:** the whole frame with RT minus the same frame without it, in the real macOS player. Each run gives the median of 16 interleaved rounds.
- **Ranges:** each cell is the range over runs, never a single best run and never a bound. FAIL means the whole range is over the bar.
- **The pool: 21 runs of the landing GPU code** (round 2 `ec31f4fa` and round 3 `fa0232d2` share it): fix32 ab1_new, ab2_new; fix33 ab1–ab5 old and new; fix33 ps14; and review round 4's eight ABBA runs q1–q8 (13:27–13:34). In those eight runs the frame without RT ran at its normal speed (6.9–7.3 ms at 1440p). They are the "quiet" column.
- **Runs left out of the pool, and why:**
  - fix32 ps12 (native): a smoke run, not an A/B pair. Its frame without RT took 23 ms at 1440p, so it was disturbed: v2 1440p High +18.75, v5 1440p High +20.77.
  - fix32 ps13 (Rosetta 2): a smoke run, disturbed (frame without RT 15–16 ms at 1440p): v2 1440p High +7.24, v5 1440p High +12.26.
  - fix33 ps15 (Rosetta 2): a smoke run. Every value lies inside the pool's ranges.
  - This round's ps16 (native) and ps17 (Rosetta 2): smoke runs of the D1 (b) build. Every value lies inside the pool's ranges. v5 1440p High: 4.80 and 4.97.

**Added frame time, ms:**

| Pose | High, 21 runs | High, 8 quiet | Ultra, 21 runs | Ultra, 8 quiet | Bar High / Ultra | Verdict |
|---|---|---|---|---|---|---|
| Standing 1.5 m from a window, 1080p | −0.04–2.15 | 0.96–1.50 | 1.44–3.73 | 2.31–2.85 | — | — |
| Standing 1.5 m, 1440p | 2.06–6.13 | 3.17–3.52 | 4.36–9.64 | 5.06–5.62 | 2.0 / 3.5 | **FAIL / FAIL** |
| 0.55 m, the pane fills the frame (break-shot pose), 1080p | 2.78–5.31 | 2.78–2.96 | 4.58–9.45 | 4.58–4.78 | +2 shot target (High) | **over the target by 0.8–3.3** |
| 0.55 m, 1440p | 4.72–13.05 | 4.72–4.99 | 7.59–22.93 | 7.59–8.00 | 5 / 7 | High: **at the bar when the machine is quiet (4.7–5.0), over it in disturbed runs (up to 13.1)**; Ultra: **FAIL** |

- **D1 (b) saves** (Ultra minus High, per run): 1.3–4.1 ms at 1080p, 1.8–10.2 ms at 1440p (quiet runs: 1.8–3.0 ms at 1440p). It also saves 18 % of the RT memory.
- **For scale:** the frame without RT takes 6.9–18.1 ms at 1440p here.
- **Where the top of each range comes from.** Mostly from runs whose own frame without RT was slow (16.7–18.1 ms at v5 1440p: fix33 ab1_new, ab3_old, ab3_new, which started at 1-minute load 47–115). But not every top: the v2 1080p top (High 2.15, Ultra 3.73) is fix32 ab1_new, whose frame without RT was a normal 4.34 ms. And fix33 ab2_old ran at load 122–155 with a near-normal frame (8.6 ms at v5 1440p). So the load average alone does not tell a disturbed run apart from a quiet one.
- **Round 3 against round 2** (review round 4's four ABBA pairs): round 3 minus round 2, added High, is −0.22 to −0.01 ms at v5 1440p and −0.36 to +0.32 ms at v2 1080p. The frame without RT differed by −0.25 to +0.37 ms. So fix round 3 added no GPU cost. Fix round 3's "5 of 5 pairs positive" was machine drift, and fix round 4 changed no GPU code.

**Projection for smaller M3/M4 GPUs (NOT measured; we have no other Mac).** Ray tracing scales roughly with the number of GPU cores, so the same work takes about 40 ÷ cores times longer. Each cell multiplies the low end of the 21-run range by the low factor and the high end by the high factor.

| Mac (GPU cores) | Factor | 1080p High, 1.5 m / 0.55 m | 1440p High, 1.5 m / 0.55 m | 1440p Ultra (opt-in now), 1.5 m / 0.55 m |
|---|---|---|---|---|
| MacBook Air / iMac M3, M4 (8–10) | ×4–5 | 0–11 / 11–27 ms | 8–31 / 19–65 ms | 17–48 / 30–115 ms |
| M3 Pro (14–18), M4 Pro (16–20) | ×2–2.9 | 0–6 / 6–15 ms | 4–18 / 9–38 ms | 9–28 / 15–67 ms |
| M3 Max (30), M4 Max (32) | ×1.25–1.33 | 0–3 / 3–7 ms | 3–8 / 6–17 ms | 5–13 / 9–30 ms |

The raster part of the frame grows by about the same factor on those Macs. So a base M3/M4 is likely below 60 fps at 1440p even without RT. That is why D1 (c) keeps RT off by default there.

**Memory.** RT keeps 36 bytes per pixel on High and 44 on Ultra, plus about 20–30 MB of scene data:

| Resolution | High | Ultra |
|---|---|---|
| 1920 × 1080 | 71 MiB | 87 MiB |
| 2560 × 1440 | 127 MiB | 155 MiB |
| 2880 × 1864 (MacBook Air 15" full screen) | 184 MiB | 225 MiB |
| 3456 × 2234 (MacBook Pro 16" full screen) | 265 MiB | 324 MiB |

**D6, the shot.**
- At the shot pose, High at 1080p is 2.78–5.31 ms: over the +2 ms target by 0.8–3.3 ms (by 0.8–1.0 ms in the quiet runs).
- At 1440p it is at or over its 5 ms bar, as in the table.
- Single editor frames looking down at a lit ceiling at the break pose reached 11–18 ms (§5.1).
- Projected for an 8–10-core M3/M4 at 1080p High: 11–27 ms.
- A measured ablation with 4 lamps cut the trace by 56 %. The trace is about two thirds of the RT stages at this pose, so (b) would land the shot at roughly +2.0 to +3.4 ms at 1080p. That figure is an ESTIMATE, not a measurement. It is built with P2.

## What D2 fixes (the evidence)

- **Dark-beyond window:** RT turns the pane into a mirror. The room, its lamps and the Relay show, but nothing stands where you stand (`images/30v_D2_dark_window.jpg`, VL316).
  - The Relay 2 m behind reads −2.4/255 over its 43,979-px silhouette against the same view without it (darkest 5 %: −12/255).
  - Same figure in run r9c: −2.4/255, p5 −12.0.
- **Lit window:** the Relay shows as a ghost exactly where your own reflection should hide it.
  - Over its ~44,000-px silhouette: −1.1 to −2.1/255 against the same view without it. The range spans runs r10a and r9c, both on the frame with accumulation off; darkest 5 %: −7.6 to −7.9/255.
  - On the accumulated frame of r10a: −2.1/255, p5 −11.3.
  - The px that change visibly: −5.7/255, p5 −24 over 32,820 px (art review 3); −8.9/255, p5 −36 in real game frames (art review 2).
  - Since fix round 4 the frame that is measured is saved (`P1_litrelay_measured_high.png`). The figures recompute from the saved files to the logged value (`W/rt_work/fix34/relay34.json`).
- The proxy (b) puts a dark body where these show nothing.

## Also new (no decision needed)

- **Restart and key pickup (fix round 3, re-shown in fix round 4):** they are now tested at a window with a dark room beyond, so the reflection is the whole picture.
  - A presented key: 117 traced frames, all at weight 1, 0 frames with misses.
  - After each of two restarts: the zone cube shows only until the new camera's gate opens (+22 / +23 frames). Then 121 traced frames with 0 misses, no stuck loading budget, no orphan gates (`images/30v_F33_restart_key.jpg`).
- **The thin-glass back image (fix round 4):** the harness fit was broken (it divided by an energy it never set). Fixed.
  - The back image is real (radiance changes by 0.006 rms; the render is deterministic, so this is not noise).
  - A single 1-px shifted copy explains only 9 % of it. The offset depends on how far the reflected surface is (about 0.3–2 px at this pose for a 6 mm pane).
  - "As the optics predict" is withdrawn: the size of the best shift fits the optics, but the fit itself proves little.
- **Still open (glass track):**
  - PRINT-1: the moving wallpaper print is reflected as its first keyframe.
  - AD-2: a straight-edged shadow step in the reflected floor at the closest pose. In the traced light it is +2 to +14 % at the two measured points, over runs r8a–r10a (bar 3 %). In the final frame there, RT adds less than one 8-bit level across it, in a lit and in a dark-beyond window.
  - LENS-E: the reflected lamps are 4–5 % dimmer than a perfect mirror.
  - The main thread on a walk: 0.35–0.45 ms against a 0.3 ms bar.
