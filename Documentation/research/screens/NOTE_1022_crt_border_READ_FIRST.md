# NOTE 2026-10-22 21:2x · Red picked the desk-monitor picture size (read before building the tube layer)

**Red's own words, in the 动画和动态图形模型技术 chat (account A), 2026-10-22 21:23**, pointing at VL429's "1990 OFFICE MONITOR" tile (Figma 3640:7746): "选择这个". The 游戏视觉 session checked it in transcript 6e9bdaff: a queued_command with an image at 04:23:16Z.

**1. Kit_CRTMonitor, any content (ads, V4 still desk frames, later the terminal):**
- Show the WHOLE 4:3 frame: no crop and no pillarbox.
- The lit picture is about **276 × 207 mm**, centred in the 300 × 228 mm bezel opening.
- That leaves about **12 mm of unlit glass left/right and 10 mm top/bottom**.
- The border keeps the tube's DARK GLASS colour, never flat black.
- Today: a 280 × 190 mm quad plus FitInside into a 640 × 434 RenderTexture. That gives about 20 mm of black on every edge (VL429 "NOW IN MAIN" tile); FitInside into the 434-tall RT is what adds the side bars.
- In the tube layer this is UNDERSCAN for this kit, not the 4.5 % overscan.
- Change the monitor's picture quad and RT aspect to 4:3 at that size. It is the same kit V4's FrontRoomsDeskStill draws on (landed 20:41), so its 4:3 fit must land on the new picture area too, still 1:1 with no boost.

**2. Kit_CRTTV:** Red did not pick for it. Keep the period TV behaviour: overscan about 5 % per edge (the spec's 4.5 % is inside the period range), 0 mm of black, never pillarboxed on a 4:3 tube.

**3. The terminal / computer screen** (`research/computer_ui/20_design.md` §8.2, 系统设计 / 平面视觉): the same border on the same monitor; its active area moves from 0.253 × 0.19 m to 0.276 × 0.207 m. Our part is the kit and screen path only.

**Evidence:**
- `Frontrooms3D/Documentation/research/crt_border/10_research.md` (period data: IBM 8512 240 × 180 mm image, Apple 13-in at 69 dpi, ≈ 12 / 9 mm borders; TV overscan per SMPTE RP 27.3 and Sony PVM specs);
- `SOURCES.md` (81 sources);
- the 2D mock maker `W/crt_border/make_vl.sh`.

**Verify in game:** capture the desk monitor at 0.6 m and 1.5 m with the ad and with a V4 still. The border must be glass, not black, with the whole frame visible. Put the result in the VL rows (VL405–407 reserved for crt-tube), so 动画和动态图形模型技术 can show Red the in-game result.
