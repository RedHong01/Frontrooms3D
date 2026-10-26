# 40 · G14 ray-traced glass: Red's decisions D1, D6, D2 (record)

2026-10-22 · 游戏视觉 · rewritten by the G14 workflow, fix round 4 (14:0x); updated by the package stage (14:5x, review round 5's text items) · details and evidence: `30_final.md` §16 (package stage), §15 (fix round 4), §14 (fix round 3), §13 (review round 3).

**Red answered at 12:36:** "G14 的 D1、D6、D2 全部按推荐" (D1, D6 and D2 all as recommended). The 游戏视觉 session wrote the answer down from Red's own words in `NOTE_1022_red_decisions.md`. **This file now records those decisions. It asks nothing.** It replaces the 13:17 version, which still asked the three questions. Review round 4 found that version out of date: the note existed from 12:36, but fix round 3 missed it. The cost numbers below are also corrected (review round 4): they are ranges over all 21 player runs of the landing GPU code, and they show the quiet runs on their own.

## What was decided

| | Decision | Built where | What the player gets |
|---|---|---|---|
| **D1** default quality | **(b) now:** High at every quality level from 3 up. Ultra only when asked for: the `-frGlassRT ultra` argument or the `FrontRooms.GlassRT` setting. **(c), built 10-24:** RT on by default only on GPUs with at least 16 cores (M3/M4 Pro and Max). Smaller GPUs, and Macs whose count cannot be read, start with today's glass and can switch RT on (`-frGlassRT`, the `FrontRooms.GlassRT` setting). | (b): this package, `FrontRoomsGlassRTSystem.DefaultQuality()`. It is applied in the clone (fix round 4) and proven in the player (below). (c): built and verified 10-24 in clone `W/proj_g14gate` (`30_final.md` §17): IORegistry `gpu-core-count` read from C#, no dylib change; forced counts 8 / 10 / 14 / 16 / 40 / unknown give Off / Off / Off / High / High / Off; this Mac reads 40 (also inside the App Sandbox). **Not in main yet:** it lands with `W/apply/glass_rt_g14_gate.sh --apply` (package 10-24 12:2x: DRY RUN OK on main `4049c70b`, Roslyn macOS + iOS COMPILE OK). | The same look as Ultra in play (edge shimmer 0.014 vs 0.014, carpet match 3.0–3.4 % vs 2.9–3.2 %), for 1.8–10.2 ms less per frame at 1440p on this Mac. |
| **D6** break-shot cost | **(a) now:** accept it. The shot traces every lamp in range. **(b) with P2:** a 4-lamp budget during the 1.65 s hold, added in the same change as P2's per-hit lamp cull. | Nothing to build now. (b) goes to the glass-rt track with P2. | The full reflection through S0–S2. |
| **D2** player body in reflections | **(b) now:** a dark, body-sized silhouette at the camera. Only reflection rays see it, never the camera. It covers lit windows too: the Relay's ghost 2 m behind must no longer show where your body should block it. **(c) later:** the first-person body model replaces the proxy once it exists. | (b): the `g14-followup` package, after G14 lands (it touches the same files). The silhouette art (proportions, coat, 1990 office clothing, no face detail) is 游戏视觉's. | Someone stands in the mirror where you stand. |

**Store builds:** until D1 (c) is in main, no Mac **store** build that contains the G14 dylib goes to players (10-24: D1 (c) is built and verified in the clone, §17 of `30_final.md`; the rule lifts when its payload lands). Snapshots and class builds are fine. App Store · 构建 has been told (Red's note).

**Sign-offs:** VL316 (dark window) and VL284 (showcase) are signed off on the `g14-followup` build with the body proxy, not on this landing. Their chips are now PENDING, not WAIT-RED.

## Where the package stands

- **D1 (b) is in the package.** It is one line in `FrontRoomsGlassRTSystem.cs` (l. 194: `return level >= 3 ? FrontRoomsGlassRTQuality.High : FrontRoomsGlassRTQuality.Off;`) and the header comment of `FrontRoomsGlassRT.cs` (l. 6–7). Against the round-2 package, 10 of its 20 paths changed.
- **Proof:** the universal player built this round (`ps16`, native) starts at quality level 5 (Ultra) and logs "default RT quality High". Its run ends "on: Apple9, High", 0 errors, and the 600-frame walk ran at High. The same player under Rosetta 2 (`ps17`) gives the same result.
- Roslyn `--all`, macOS and iOS defines: COMPILE OK on main `6521e6ad` with the 8 payload `.cs` files (package stage, 14:4x).
- **The package is refreshed and ready to apply** (package stage, 14:40; `30_final.md` §16): `W/apply/glass_rt_g14.sh`, 20 paths, the universal plugin `fa0232d2` (macOS 12.0 and later), the D1 (b) default, bases recorded on main `6521e6ad`. Review round 5 found no payload defect. Dry run: `bash /Users/redwang/FrontRoomsVisualWork/apply/glass_rt_g14.sh` (prints DRY RUN OK); apply: the same with `--apply`, then restart Unity. The optional clean-up `--with-deletions` needs your OK. The round-2 package (dylib `ec31f4fa`, Ultra by default) is retired.

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
- A measured ablation (r3b, this pose, 1080p High) with 4 lamps cut trace + resolve by 56 % (5.85 → 2.55 ms). Applied to the 21-run shot range 2.78–5.31 ms, (b) would land the shot at roughly **+1.6 to +3.5 ms at 1080p** (+1.6 to +1.9 in the quiet runs).
  - How: added time × (1 − 0.56 × s). s is the share of the RT stages the cut acts on, measured at this pose in the four universal players ps14–ps17: 0.75–0.77 if it covers trace + resolve, as the ablation measured (→ +1.6 to +3.1), or 0.62–0.65 if it covers the trace only (→ +1.8 to +3.5). It assumes the added frame time shrinks in proportion to the stages.
  - That figure is an ESTIMATE, not a measurement. It is built with P2. *(The 14:10 figure, +2.0 to +3.4, came from fix round 3's 13-run low end, not from the 21-run pool: review round 5.)*

## What D2 fixes (the evidence)

- **Dark-beyond window:** RT turns the pane into a mirror. The room, its lamps and the Relay show, but nothing stands where you stand (`images/30v_D2_dark_window.jpg`, VL316).
  - The Relay 2 m behind, over its 43,979-px silhouette, against the same view without it, with accumulation on as in the game (run r10a): mean change 6.8/255 either way; darkest 5 % −12/255, brightest 5 % +19/255; 26 % of its pixels change by more than 8 levels. Outside the silhouette the frame changes by 0.9/255 (noise).
  - The signed mean (−2.4/255, also in run r9c) cancels the brighter parts against the darker ones, so it understated the ghost. It is no longer the headline (review round 5).
- **Lit window:** the Relay shows as a ghost exactly where your own reflection should hide it.
  - Over its 43,989-px silhouette, with accumulation on as in the game (run r10a): mean change 5.5/255 either way; darkest 5 % −11/255, brightest 5 % +13/255; 19 % of its pixels change by more than 8 levels (noise outside 0.9/255).
  - With accumulation off (the frame fix round 4 measured): 4.9/255, p5 −8 / p95 +14, 14 % over 8 levels. Signed means, for the earlier text: −2.1/255 accumulated, −1.1 with accumulation off (r9c: −2.1).
  - The px that change visibly: −5.7/255, p5 −24 over 32,820 px (art review 3); −8.9/255, p5 −36 in real game frames (art review 2).
  - The measured frames are saved (`P1_litrelay_high.png` with accumulation on, `P1_litrelay_measured_high.png` with it off). The figures recompute from the saved files (`W/rt_work/fix34/relay34.json`, `W/rt_work/package5/measure_p5.json`).
- The proxy (b) puts a dark body where these show nothing.

## Also new (no decision needed)

- **Restart and key pickup (fix round 3, re-shown in fix round 4):** they are now tested at a window with a dark room beyond. There RT High differs from the zone cube by a mean 4.4/255 over the pane, and plainly in the reflected lamp rows (2,966 px at 88 vs 38/255), so a drop to the zone cube would show.
  - A presented key: 117 traced frames, all at weight 1, 0 frames with misses.
  - After each of two restarts: the zone cube shows only until the new camera's gate opens (+22 / +23 frames). Then 121 traced frames with 0 misses, no stuck loading budget, no orphan gates (`images/30v_F33_restart_key.jpg`).
- **The thin-glass back image (fix round 4):** the harness fit was broken (it divided by an energy it never set). Fixed.
  - The back image is real (radiance changes by 0.006 rms; the render is deterministic, so this is not noise).
  - A single 1-px shifted copy explains only 9 % of it. The offset depends on how far the reflected surface is (about 0.3–2 px at this pose for a 6 mm pane).
  - "As the optics predict" is withdrawn: the size of the best shift fits the optics, but the fit itself proves little.
- **Still open (glass track):**
  - PRINT-1: the moving wallpaper print is reflected as its first keyframe.
  - AD-2: a straight-edged shadow step in the reflected floor at the closest pose. In the traced light it is +2 to +14 % at the two measured points, over runs r8a–r10a (bar 3 %). In the final frame there, RT adds less than one 8-bit level across it, in a lit and in a dark-beyond window, because the pane's final value there is mostly a base that RT does not set (present with RT Off).
  - LENS-E: the reflected lamps are 4–5 % dimmer than a perfect mirror.
  - The main thread on a walk: 0.35–0.45 ms against a 0.3 ms bar.
