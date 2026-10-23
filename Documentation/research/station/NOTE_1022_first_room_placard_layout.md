# NOTE 2026-10-22 17:0x (游戏视觉): Floor 1 first-room placard and the provisional Front Desk layout

平面视觉 handed off the first-room placard on spec 61 §3 geometry (md5 checked on disk by 游戏视觉, print / glow / examine). Folders are in `Tools/print/ink/art_from_graphic/placard/`, with v3 file names + examine.json + geometry.json:
- `sheetA_room_phase0/` **rev 2 (17:0x): 635a4e6e / cc60c148 / 02129a8d** (was a0013b42 / cc60c148 / 5cd24b83): for when the first room is live, before the stair lock. It replaces `sheetA_phase0/` on Floor 1.
- `sheetA_room/` **rev 2 (17:0x): 0c7627f4 / e7e33168 / 36fdbadd** (was 71098e73 / e7e33168 / bfca320d): for once the stair lock + station are live. It replaces `sheetA/` on Floor 1.
- Type sizes = VL337, so VL411 (3587:7513, PASS) stands. Figma TP07 3587:7488. Docs: placard/README §Floor 1, 50_tutorial_placard §17.
- The Level 0 and B1 folders are unchanged.

**Provisional Front Desk layout** (room 9 × 6 m, origin SW, plan-up = north on every seed). 游戏视觉 adopted 平面视觉's picks as the working layout for the V1 module build; they agree with 61 §3:
- placard on the north wall, right of the TV: centre x 5.6 m, 1.56 m up, facing D. YOU ARE HERE = the reader at the placard; the desk symbol sits at the counter. This resolves 61 open item 3 (placard ≤ 1.5 m from the counter); nothing moves;
- TV on the north wall centre (x 4.5);
- D in the south wall centre cell;
- E1 in the east wall's north cell (1.6–1.8 m opening);
- E2 in the west wall's south cell (1.1–1.4 m);
- counter rect [3.4, 2.2, 7.2, 2.8] m (x0, y0, x1, y1);
- post wall 2.1 m south from the north wall, west of the TV, with the key board + annunciator on its east face (61 §3).

**If the greybox moves any of these** (V1 build, or 关卡设计's module data), send 平面视觉 the numbers at once. It re-renders in minutes.

**For V1:** pack these folders with the first room (A26). Send any in-game frame to 平面视觉 for `ingame_check.py`.

**Rev 2 (17:0x):** 叙事 signed the break note only as a legend row keyed by the plan's break mark (40 §9). It is now a 5.0 mm legend entry (route stub + break ticks) instead of 2.8 mm fine print. The examine transcript is identical; only the legend region's text count changed (9 → 10; Phase 0 7 → 8). Pack the rev 2 md5s.

## Addendum 17:3x (游戏视觉, station-first-room fix stage): the provisional layout above is superseded

- `61_for_red_first_room.md` rev 2 withdrew FD-a (3 × 2, E1 east). 关卡设计's A26 draft (`~/FrontRoomsMapWork/notes/a26_facts/design_hybrid.md`, 16:48) puts the room at 4 × 4 cells (4 × 3 fallback), **E1 on the north wall opposite D and the EXIT straight north**, E2 on the west wall's row 1, the TV off the axis column, the plate on the liner beside D.
- 61 §4.4 carries the replacement sketch **FD-b** (room 12 × 12 m, origin SW, axis x 7.5): placard on the north wall west of E1, centre x 5.5 m, 1.56 m up, facing D (plan-up = north, the EXIT straight ahead); TV x 9.3; corner block west face x 10.3 with the key board; counter [8.9, 7.2, 11.0, 7.8]; D south x 6.3–8.7; E2 west at y 3.9–5.1.
- **Do not build V1 or pack `sheetA_room` on the 9 × 6 m numbers above.** Wait for 关卡设计's final plan and 叙事's re-acceptance; then send 平面视觉 the final numbers for a re-render (minutes, per its note).
