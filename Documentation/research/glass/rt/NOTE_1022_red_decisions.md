# NOTE 2026-10-22 12:36: Red's answers to G14 D1, D6, D2

**Red, in the 游戏视觉 chat (account B, session `local_a9a1dce2-e603-4a9e-b51e-e02adac1e045`), 2026-10-22 12:36 PDT:** "G14 的 D1、D6、D2 全部按推荐。"

The question was asked at 12:2x with the corrected cost ranges from `30_final.md` §13.2, not the old numbers in `40_for_red.md` (11:16). The recommendations Red accepted:

## D1 · Default quality: (b) now, (c) before a store build with RT reaches base M3/M4 Macs

- **In this package (the G14 fix-3 workflow's package stage applies it):** High at every quality level. Ultra is used only when asked for explicitly: the `-frGlassRT ultra` argument or the `FrontRooms.GlassRT` PlayerPrefs value.
- **The change is one line** in `FrontRoomsGlassRTSystem.DefaultQuality()`, the last `return`:
  - before: `return level >= 5 ? FrontRoomsGlassRTQuality.Ultra : level >= 3 ? FrontRoomsGlassRTQuality.High : FrontRoomsGlassRTQuality.Off;`
  - after: `return level >= 3 ? FrontRoomsGlassRTQuality.High : FrontRoomsGlassRTQuality.Off;`
  - Update the comment or doc lines that say level 5 = Ultra. Re-run Roslyn `--all` (macOS + `--ios`). Check that a player at quality level 5 logs `on: Apple9, High` (a quick player smoke is enough).
- **(c) is NOT in this package.** It is the next G14 follow-up, `g14-followup` in the 游戏视觉 queue. RT is on by default only on GPUs with at least 16 cores (M3/M4 Pro and Max). Smaller GPUs start with today's glass and can switch RT on explicitly. It needs a local IORegistry core-count query (no download) and a test with a forced core count.
  - Until (c) lands, no Mac **store** build that contains the G14 dylib goes to players. Snapshots and class builds are fine.
  - App Store · 构建 is told.

## D6 · Break-shot cost: (a) accept now, (b) with P2

- Nothing to build now. The break shot keeps tracing every lamp in range.
- When the glass-rt track does P2's per-hit lamp cull, add the 4-lamp budget during the 1.65 s hold in the same change.

## D2 · Player body in reflections: (b) now, (c) when the first-person body exists

- (b) is a dark, body-sized silhouette at the camera, seen only by reflection rays and never by the camera. It covers lit windows too: the Relay's ghost 2 m behind must no longer show where the player's body should block it.
- It is built in the `g14-followup` package, after G14 lands, since it touches the same files.
- The silhouette (proportions, coat, 1990 office clothing; no face detail) is 游戏视觉's art.
- VL316 (dark window) and VL284 (showcase) are signed off on the build with the proxy.
- (c) replaces the proxy when the first-person body model exists.
