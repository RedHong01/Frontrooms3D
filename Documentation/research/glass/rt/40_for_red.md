# 40 · G14 ray-traced glass: three decisions for Red (D1, D2, D6)

2026-10-22 · 游戏视觉 · written by the G14 workflow (fix round 2) · details and evidence: `30_final.md` §11 and §12.

**What this is.** G14 makes every window in a Mac build reflect the real room behind the player, traced on the GPU (M3 / M4 Macs; every other Mac, WebGL, iOS and Windows keep today's glass). The package `glass_rt_g14` is ready to land and does **not** need these three answers to land: until you decide, the defaults in the "today" column apply. Each answer is a one-line change in a visual-owned file.

All timings below were measured on this Mac (M3 Max, 40-core GPU, 128 GB) while other Unity jobs ran (UNDER LOAD), so they are upper bounds for this machine. "Added frame time" = the whole frame with RT minus the same frame without it, in the macOS player.

---

## D1 · Which quality does a Mac start at?

**Today (if you say nothing):** every Mac player starts at quality level 5, and level 5 maps to **Ultra**. So after landing, every M3/M4 Mac runs Ultra by default, including the Alpha and the store build. Macs before M3 (and Intel Macs) are not affected: they keep today's glass.

**What the player sees.** High and Ultra look the same in play. We measured no difference in edge shimmer (0.014 vs 0.014), carpet match (3.3 vs 3.2 %) or lamp size (within 1 point). Ultra adds sub-pixel anti-aliasing at high-contrast edges and 8 instead of 6 shadowed lamps per hit.

**Cost on this M3 Max (added frame time, ms, High / Ultra):**

| Pose | 1080p | 1440p |
|---|---|---|
| Standing 1.5 m from a window | 1.28 / 2.71 | 3.47 / 5.45 |
| 0.55 m, the pane fills the frame (break-shot pose) | 2.86 / 4.68 | 4.79 / 7.56 |

For scale: the frame without RT takes about 7–8 ms at 1440p here.

**Projection for smaller M3/M4 GPUs (NOT measured; we have no other Mac).** Ray tracing scales roughly with the number of GPU cores, so the same work takes about 40 ÷ cores times longer. Clocks are similar across the M3/M4 family; M4 cores are somewhat faster, which this ignores.

| Mac (GPU cores) | Factor | 1080p High / Ultra | 1440p High / Ultra |
|---|---|---|---|
| MacBook Air / iMac M3, M4 (8–10) | ×4–5 | 5–14 / 11–23 ms | 14–24 / 22–38 ms |
| M3 Pro (14–18), M4 Pro (16–20) | ×2–2.9 | 2.6–8.2 / 5.4–13 ms | 6.9–14 / 11–22 ms |
| M3 Max (30), M4 Max (32) | ×1.25–1.33 | 1.6–3.8 / 3.4–6.2 ms | 4.3–6.4 / 6.8–10 ms |

The raster part of the frame grows by about the same factor on those Macs, so on a 10-core M3 the game is likely below 60 fps at 1440p even without RT; RT High would add roughly half again, Ultra almost doubles it.

**Memory.** RT keeps 36 bytes per pixel on High and 44 on Ultra (full-resolution float targets), plus about 20–30 MB of scene data. The player renders at the window's real pixels (Retina support is on; full screen uses the panel's native resolution):

| Resolution | High | Ultra |
|---|---|---|
| 1920 × 1080 | 71 MiB | 87 MiB |
| 2560 × 1440 | 127 MiB | 155 MiB |
| 2880 × 1864 (MacBook Air 15" full screen) | 184 MiB | 225 MiB |
| 3456 × 2234 (MacBook Pro 16" full screen) | 265 MiB | 324 MiB |

On an 8 GB MacBook Air this is a noticeable share of memory, though not by itself a problem.

**Options:**
- **(a) Keep Ultra at level 5.** Highest image on a Max; on a base M3/M4 the game likely runs well below 60 fps with RT on.
- **(b) High at every level; Ultra only as an explicit setting.** Same look in play, 1.4–2.8 ms less per frame on this Mac (1.4–1.8 at 1080p, 2.0–2.8 at 1440p), and 18 % less RT memory. One line in `FrontRoomsGlassRTSystem.DefaultQuality()`.
- **(c) (b), and RT on by default only on GPUs with enough cores** (for example 16 or more: M3/M4 Pro and Max); smaller GPUs start with today's glass and can switch RT on in a setting. Needs a small native core-count query (IORegistry, local, no download) and a test on a small Mac. A follow-up, not in this package.

**Recommendation: (b) now, (c) before a store build reaches players with base M3/M4 Macs.** (b) costs nothing visible. (c) protects the frame rate on the most common Macs, which we cannot measure here.

**It blocks:** nothing for landing. It decides what the Alpha and the store build ship with: with (a), every M3/M4 player gets Ultra.

---

## D6 · How much may the break shot cost?

**Today (if you say nothing):** the break shot (the 1.65 s hold with the pane filling the frame) traces like any other frame, with every lamp in range.

**What the player sees.** The pane fills the screen and its reflection shows the room behind, lit by up to ~86 lamps, through S0 → S1 → S2. Since fix round 1 the cracked S1 and S2 panes keep the reflection (before, the S2 beat dropped to the old flat reflection, which made the shot cheaper only by losing the effect).

**Cost at the shot pose on this M3 Max (added frame time, High):** 2.86 ms at 1080p against the plan's +2 ms target; 4.79 ms at 1440p (bar 5 ms: within); Ultra 4.68 / 7.56 ms. Single editor frames looking down at a lit ceiling reached 11–18 ms at the break pose. Projected for a 10-core M3/M4: about +11–14 ms at 1080p High.

**Options:**
- **(a) Accept it:** about +0.9 ms over the target for the 1.65 s hold on this Mac.
- **(b) A 4-lamp budget during the hold:** the measured ablation cut the trace by 56 %, which would put the shot at about +1.6 ms (an estimate). The far lamps then light the reflected room less during the hold; the lamps nearest each reflected point stay. Best done together with P2's per-hit lamp cull (glass-rt-track), which makes it cheap everywhere, not only in the shot.
- **(c) High instead of Ultra for the shot:** matters only if D1 stays Ultra.

**Recommendation: (a) until P2 lands, then (b).** Nobody sees the 0.9 ms on an M3 Max; on a small GPU, D1 (c) is the real protection.

**It blocks:** nothing for landing; P2's lamp cull should follow your answer.

---

## D2 · Should the player have a body in dark windows?

**Today (if you say nothing):** the player has no body in reflections.

**What the player sees.** In a lit window the pane is mostly see-through, and the missing body hardly shows. In a **dark-beyond window** RT turns the pane into a real mirror: the room behind, its lamps and the Relay (if it stands behind you) all show, but nothing stands where you stand (`images/30v_D2_dark_window.jpg`, VL316). Since fix round 1 the mirror also holds through the whole break shot at a dark window (S0 to S2), so the empty spot shows during the shot too. The Relay itself reads clearly in glass now: in a lit window, 2 m behind the player, it darkens the pane by 8.9/255 on average (36/255 at its darkest), a ghost-like figure (art review 2).

**Options:**
- **(a) Keep it: no body.** A "vampire" look. Players who notice may read it as a story beat (am I dead?) or as a bug. Nothing to build.
- **(b) A plain reflection-only body proxy:** a dark, body-sized silhouette at the camera, seen only by reflection rays, never by the camera. About one more moving instance per frame (no measurable cost). Reads as "someone is standing there" in a dark window, without a detailed model. Needs a silhouette (proportions, coat) from art and one register call.
- **(c) The planned first-person body,** reflection-only, once it exists. The best match, but it waits for that model.

**Recommendation: (b) for now, (c) when the first-person body exists.** A mirror that shows the Relay but not you is a strong signal we did not author. If you want that signal on purpose, (a) is free.

**It blocks:** the dark-window shot set (VL316) and the showcase sign-off (VL284).

---

## Also new in fix round 2 (no decision needed)

- **After a jump, a run start or the floor change (C22)** the reflection no longer shows holes or a room lit by one lamp. The window keeps its old reflection (the zone cube) for about 0.4–0.55 s, until the traced one is complete, then fades in over 12 frames (0.2 s). A normal walk never triggers this.
- **Reflected ceiling lamps** no longer shimmer at their edges when you move (the jagged edges art review 2 found). The price: they carry 4–5 % less light than in a perfect mirror. At the glass's ~8 % reflection strength we do not expect anyone to see it, and art can trade it back (`30_final.md` §12.4).
- **The moving wallpaper print** (landed 10:20) is reflected as its first keyframe while the wall may show another one; a follow-up for the glass track (§12.5).
- **The universal Mac app** traces on Apple silicon (natively and under Rosetta 2) with this round's plugin.
