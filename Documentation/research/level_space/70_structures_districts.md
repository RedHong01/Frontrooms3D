# Level space · 70 · The structures scene: what each level-space district builds

Date: 2026-11-03. Line: 夸张结构调研与设计 (`level_space`), account G, session `local_012d68f4`. Stage: spec for the Plan and Spaces stages of 总控's structures workflow (`~/FrontRoomsMapWork/integration/structures/`).

Red, 2026-11-03 14:37 (总控 chat): "不要再克隆里面做，而是在主工程的其中一个scene里做，专业那个我可以进去体验不同的关卡和空间结构，比起知识看你给我的图片预览…" and "第四个就是所有新的结构，然后我去尝试之后告诉你那些应该放在游戏里" (RULES 5b). Red, 2026-11-03 in this line's chat: "目前你可以接手对应对话的工作".

What this page is: the level-space half of scene 4 `FrontRoomsStructures.unity`, as districts to walk. It turns this folder's designs and Red's decisions into what to build, in what order, with what numbers, and what Red decides in each place. It adds no new design beyond composing what is on disk; where it composes, it says so.

Not done here: no code, no Unity, no clone, no download. The pool family (club, far door, PoolLevel, fire stair, dark zone, PC_/PL_ modules) is the pool line's and is not specified here. The module kit itself is `40` §11 (written 11-03 by 总控's workflow); where §11 and this page differ, §11 wins for piece sizes and this page wins for what each district shows.

Reused, not redone: `20` (devices V1-V7, the well, legibility rules), `40` §4-7 (types, splice rules R1-R8, modules, per level), `30` §8 and `31` §5 (proposed types), `60` (B4-S/M/L), `61` §2.5-2.7, §3, §4 (the slide, Red's 17:15 decisions), `W/level_space/briefs/61c_r4t_redo_build.txt` (support A in numbers, tile, light), `r5_skeleton_build.txt` (the slide inside a skeleton), `62`/`63` (open questions), `bathroom_rooms/16`, `17`, `18` (F1, water, M1-M7).

## 0. Short answer

1. **Six districts, in this order:** SP1 the slide on its wall (Red's A, inside a pool skeleton), SP2 the well (four floors stacked over the pool), SP3 the Level 0 field and its checks, SP4 the Office stack and pit, SP5 the Run! ramp to the dock, SP6 the deep pool terrain and the large stairs. SP1 and SP2 are the must-builds; SP3-SP6 follow in that order.
2. **Every district is a skeleton, not a diorama.** Red's notes say so three times: 15:38 (drama in the skeleton), 16:06 (no symmetry or rings; splice terrains at different heights), 11-02 07:31 (the slide redo ignored the spatial study). So no district is one device in an empty box room; each splices several types at several heights in one view.
3. **Red decides by walking.** Each district and zone has a sign in Chinese: what it is, where in the game it would go, and the open questions this folder has for Red (§8). The signs replace the decision doc's pictures.
4. **Look rules hold everywhere** (§2): the level's own surfaces and lamps; porcelain-white tile with visible joints in the pool family; a dark nosing on every pool step; F1 light and "wrong" turquoise water; darkness never cover.

## 1. Global frame (all districts)

| Item | Value | Source |
|---|---|---|
| Cell, fine module | 3.0 m; 0.6 m | LEVEL_MODULE_SPEC §2 |
| Walls | 0.16 m centred on cell lines (thicker where a district says so: tiled spine body 0.6, shaft 0.3) | LMS §2; `40` §11.6 |
| Ceiling classes | Low 2.4, Standard 2.9, Tall 5.4 (Office: Standard only, `62` Q2) | LMS §2 |
| Storey (floor to floor) | 3.9 m (13 steps of 0.30) for Level 0, Office, Run!; 4.5 m (15 steps) on the pool level | `40` §11.4, `61` §4 |
| Vertical module | tile course 0.15; level step 0.30 (two courses, the player's step); riser 0.15, tread 0.30; terrace 0.60; weir step 0.30-0.60 | `40` §5.1, §11.4 |
| Ramps | 1:12, runs ≤ 0.76 m, landings 1.52 m; Run! service ramp 1:8 | `40` §5.1 |
| Guard | 1.07 m (42 in) at every drop over 0.76 m | `40` E1, R1 |
| Bodies | player 1.75 m, r 0.3, step 0.3, eye 1.62; Relay 2.05 m, r 0.3 | LMS §7 |
| Arches walked under | springing ≥ 2.1 m (the Relay passes) | `61` §2.6, M6 |
| Large spirals | B4-M and B4-L on pool-level districts only; B4-S (1 cell) allowed elsewhere | Red 10-31 17:29 |
| Second route | two ways onto every raised or sunken area of 2 cells or more; a spiral is never the only way | R6, Red 10-31 16:15 |

Placement: each district on its own ground, far enough from the others that no light, fog or geometry bleeds between them. Arrival point at deck level, 1.62 m eye, facing the district's main view (§3-7 name it).

Per-district environment: on entering (or on the hub's jump key), set the district's ambient, fog and white balance, and restore on leaving. Level 0 and Office use the game's own (`FrontRoomsLook.ApplyAmbient`); Run! uses Run! B's; pool districts use F1 (`bathroom_rooms/16` §5-6; the K greybox's trilight .15/.20/.22, .17/.23/.24, .21/.28/.28 read too dark: start from `61c` LIGHT, about .30/.36/.38, .32/.38/.40, .36/.44/.44, fog .11/.16/.17 at density .010, white balance -14) and tune by eye on the walk.

Static Relay stand-ins: a 2.05 m dark matte figure (capsule body, head sphere), never animated, labelled on the sign, placed only where a district tests a sight line (rule 12). Never inside the slide.

Honesty: every greybox element that a production asset will replace is listed on its district's sign ("灰盒占位"). Each district logs tris, renderers and lights to `BUILD_LOG.md` (VISUAL_STANDARD §1.4: report, do not hide over-budget).

## 2. Look rules

- **Level surfaces are the game's own:** `L0_Wallpaper`, `L0_Carpet`, `L0_Ceiling` (0.6 grid, one troffer per cell, LMS §4) for Level 0; `Office_Wall`, `Office_Carpet`, `Office_Ceiling`, `Kit_CubiclePanel*`, `Kit_PanelPost*` for the Office; `RunB_BlockLower`, `RunB_BlockUpper`, `RunB_FloorWax`, `RunB_Soffit`, `RunB_GalvSteel` for Run!. The banal kit at the wrong scale is the drama (`20` V4), never a new material.
- **Pool tile reads as tile:** porcelain white, base about (0.93, 0.95, 0.96), smoothness 0.85 (Red 10-31 17:19); 150 mm (6 in) tiles, 5 mm cool grey-green grout, per-tile tone ±1.2 %, per-tile normal tilt about 0.5°, a cushion edge in the normal (the `61c` TILE recipe, on a 1.5 m sheet of 10 × 10 tiles so it divides 3 m, LMS §5 `TileAlign`, `40` §11.7; 2048 px is enough). UVs in metres, so joints run on from wall to wall and round arch intrados. A new structures-only material; `Prop_CeramicGlaze.mat` (游戏视觉's) is not edited. **Ready to stage:** `~/FrontRoomsVisualWork/level_space/tile/out/LS_PoolTile150_{A,N,S}.png` (2048 px = 1.5 m, seamless, for `FrontRooms/Surface`), import settings and the material recipe in `../README.txt` there; generator `make_pool_tile.py`.
- **Pool steps:** porcelain treads and risers; a dark nosing only, 50 mm on the tread and 25 mm down the riser, about (0.24, 0.27, 0.31) (Red 10-31 17:24). No teal band.
- **Water:** the pool family's "wrong" turquoise (`bathroom_rooms/17` §0; Red Q1, absorption ×3-5); caustics on ceilings only over water.
- **Light on every joint (R8):** lit landings, stair noses, weir lips, slot edges. Dark only in water depth and unreachable voids. Pool halls: wall boxes throwing up so the ceiling is the brightest surface (`61c` LIGHT, the P1-A language), plus a soft down-fill.
- **Era lock:** nothing past 1993, no brands, signs carry no period text unless 叙事设计 supplied it.

## 3. SP1 · The slide on its wall (pool level; must)

**What Red gets:** a double-height tiled hall where the flume rides on an aqueduct of tiled walls (Red 11-02 ~14:30: "A · 墙在下面"), a round tiled tower through the ceiling to a start room on the floor above (Red 10-31 17:15: "塔高方案选择尽可能最高"), a room the flume crosses that you never reach, and a terraced water court under a floating office ceiling. Red can ride it.

**Why this composition:** `61` §2.5-2.7 and `61c` carry Red's decisions, re-laid here on the cell grid with `40` §11.6's pieces; `r5` S2 adds the skeleton (terraces and weirs beside the last run, rider and water falling together, M7); 18's M1 adds the one non-tile surface. The composition is this page's; every part is from those docs.

Layout on the cell grid (`40` §11.6 pieces; this page composes them). District-local metres, x east, z north; the hall is x -10.5…10.5, z 0…21, so its cell lines are x = -10.5, -7.5 … 10.5 and z = 0, 3 … 21:
- **Hall:** 7 × 7 cells, deck ±0, tiled lid on the slab soffit at +8.7 (D5). Upper floor slab 0.3, floor +9.0 (two pool storeys of 4.5 m, 15 steps each, `40` §11.4).
- **Tower shaft:** 2 × 2 cells in the south-west corner (x -10.5…-4.5, z 0…6), round, Ø 3.6 m outside, wall 0.3, centre (-7.5, 3.0). Base with an arched door facing the hall, middles, cap = a tiled half-wall ring 1.07 high at +9.0, open toward the tub. Inside a spiral round a tiled newel: risers 0.1875, 24 per storey, 48 to +9.0 (`40` §11.6; `61` §2.7 had 45 × 0.20), headroom ≥ 2.3; helical slot windows 0.47 × 1.2 so a climber is glimpsed (rule 12). Player shortcut only.
- **Start room:** 5 × 2 cells on the upper floor (x -10.5…4.5, z 0…6), +9.0, 3.0 m clear, porcelain tile. The tub against the shaft's open side, riding surface +9.3 (0.3 m seat, water 0.42); tiled cheeks with the stainless crossbar 1.0 m over the riding surface, a chain across the entry, the red/green start light facing the rider, the top guard's stool, a monitor on a tiled shelf, a blank rules board. The slot = the first two flume cells, x -4.5…1.5 on the line z = 3, 2.4 m wide, tiled half-walls 1.07 on its sides.
- **Second way up (R6):** the start room's east door at x 4.5 → an upper corridor (x 4.5…7.5, z 0…3, +9.0) → a B3 switchback core (x 7.5…10.5, z 0…3), 30 risers of 0.15 per storey, down to the deck. The walk back after a ride, and the Relay's way.
- **Riding line** (flume cells of 3.0 m on cell lines, bends r 3.0 round cell corners in 45° bays; drops in tile courses c = 0.15):
  | # | Piece | From → to | Courses | Riding level after |
  |---|---|---|---|---|
  | 1 | slot, 2 cells, SL-B | (-4.5, 3) → (1.5, 3) | 5 + 5 | 7.80 |
  | 2 | cell, SL-B | → (4.5, 3) | 3 | 7.35 |
  | 3 | bend 180°, 4 bays, centre (4.5, 6), SL-A | → (4.5, 9), heading west | 8 | 6.15 |
  | 4 | 5 cells along z = 9, SL-A, the last SL-C | → (-10.5, 9): WALL PASS, hall west wall | 10 | 4.65 |
  | 5 | cell in the crossing room, SL-C | → (-13.5, 9) | 2 | 4.35 |
  | 6 | bend 90°, centre (-13.5, 12), SL-A | → (-16.5, 12), heading north | 4 | 3.75 |
  | 7 | 3 cells along x = -16.5, SL-A, the last SL-C | → (-16.5, 21): WALL PASS into the service void | 6 | 2.85 |
  | 8 | in the void, all SL-C: cell, bend 90° (centre (-13.5, 24)), cell, bend 90° (centre (-10.5, 24)), cell | → (-16.5, 24) → (-13.5, 27) → (-10.5, 27) → (-7.5, 24) → (-7.5, 21): WALL PASS, hall north wall | 2 + 4 + 2 + 4 + 1 | 0.90 |
  | 9 | cell, SL-C then SL-B | → (-7.5, 18) | 2 | 0.60 |
  | 10 | bend 90°, centre (-4.5, 18), SL-B | → (-4.5, 15), heading east | 4 | 0.00 |
  | 11 | last cell, level, SL-B | → lip at (-1.5, 15), 50 mm over the water | 0 | 0.00 |
  Total 62 courses = 9.3 m over about 79 m, 11.7 % (within M7's ≥ 10 %; `40` §11.6 prefers 12.5 %, which needs one crossing segment less). Joints over the piers; a style changes only at a joint (Red 17:15 "样式混用", `61` §2.5). The tube top must clear the 8.7 soffit: at the start of row 3 it is 8.55. Tube Ø 1.2; trough 0.9 inside with a 70 mm lip flange; flange rings at every joint; banked frame on SL-B only.
- **Support A, the wall under** (`40` §11.6 SPINE BAY, SPINE BEND, WALL PASS): a 0.6 m tiled body on the flume's line, a 0.6 m pier on every cell corner, a 2.4 m round arch on each edge, coping 0.9 × 0.30 on a 0.15 corbel course, one level per pier, the saddle (0.7 long, one course) under the riding line, stepping at mid-arch. Crown rule: c = the lower coping underside over the arch above the bay's floor; springing s = 0.6 c rounded down to a course; crown = s + 1.2. Walk arch where c ≥ 3.6 (s ≥ 2.10, the Relay walks through), sight arch 1.2 wide on a 1.07 sill where c is 2.2-3.6, solid plinth 0.9 thick where c < 2.2. Never more than one 0.6 m pier of solid wall between openings above eye level (R7). Bends: 45° bays, chord 2.30, opening 1.70, every bay a walkable arch. No steel except the crossbar, the chain, the start-light post and flange bolts.
- **Crossing room** (the room you never reach, `61` §3): 3 × 7 cells west of the hall (x -19.5…-10.5, z 0…21), floor -0.12 with ankle water, tiled lid 6.6, no door on the player's side. Seen only through a 2.4 m round arch in the hall's west wall on the edge z 12…15, over a tiled half-wall 1.07 (R7): the tube crosses the view. **Service void** north of both rooms (x -19.5…-4.5, z 21…30): block walls, one dim lamp; the flume turns there unseen.
- **Catch pool:** 3 × 2 cells on the flume's line (x -1.5…7.5, z 12…18), 0.9 m deep (`61`; `63` Q8 asks 0.9 or 1.07), 9 m of water beyond the lip (≥ 6.1, [LS78]), lit from below. Tiled steps out on the east with a tiled cheek, no handrails, a dark nosing on every step. The bottom guard's tiled stand on the south deck.
- **Terraced water court (F2, from `r5` S2):** an L round the pool's north and east sides (z 18…21 for x -4.5…10.5; x 7.5…10.5 for z 9…18), clear of the flume. Terraces +0.6, +1.2, +2.4 at unequal widths, rising to the north-east corner; each holds a shallow basin whose water falls over a tiled weir (C5, 0.30-0.60) into the next and last into the catch pool: rider and water fall together (M7). Straight tiled flights (B2) beside the weirs, two ways onto each terrace (R6); guards where an edge drops more than 0.76.
- **Floating office ceiling (M1):** over the court only, a lay-in grid (`L0_Ceiling`, 0.6 grid, one troffer per cell) at +5.4 above the deck, hung from the tile lid; three tiles missing and two troffers dead, showing the black plenum and the tile lid above. The one surface in the hall that is not tile.
- **Ride (must):** at the tub, a prompt; the camera follows the riding line at the rider's eye (0.9 m above the riding surface, looking ahead), speed from the drop, clamped 3-7 m/s (period about 5.3 m/s, `61` §1.1), about 15 s; it ends standing in the catch pool. Esc returns to the tub. No figure is ever seen in the tube (Red 17:15); the Relay never rides.
- **Night key:** hall lamps to a fifth, ambient halved, SL-A cells lit from inside (emission about .10, .55, .55): the tube becomes a line of light through the walls.
- **Main view on arrival:** on the west deck by the crossing-room arch at (-9.0, 1.62, 13.5), level camera, looking toward (6, 1.62, 9): the arcade along z = 9 in raking perspective, the flume overhead, the court and its weirs beyond. Turning right shows the tower rising through the lid.

Sign (SP1):
> **SP1 · 滑梯：瓷砖墙在下面托着（你选的 A）**
> 泳池层。双层高的瓷砖大厅，滑道骑在一道瓷砖拱墙上，塔从天花板穿到楼上的起滑间（+9.3 米）。滑道从西墙出去，穿过一间你永远走不到的房间，在墙后转一圈，再从北墙回来，落进 0.9 米的落水池，全长约 79 米。东北角是一层层往下溢水的台地，头顶悬着一块办公室吊顶。走到起滑间的池子里按提示可以滑一次。
> 灰盒占位：瓷砖、滑道、拱墙都是灰盒，正式模型有螺栓、法兰和真实贴图。
> 要你决定：1 这套滑梯放进泳池层吗？ 2 滑道里有没有水流（61 P7）？ 3 落水池 0.9 米还是 1.07 米（63 Q8）？ 4 空无一人时起滑灯变绿，作为一次性事件，可以吗（63 Q7）？ 5 台地上方那块办公室吊顶留不留？

## 4. SP2 · The well (cross-level; must)

**What Red gets:** the one void through the building (`20` §3) with four floors stacked round it, so from Level 0's rail Red looks about 17 m down past the Office and Run! floors to turquoise water, then walks down floor by floor. The colour temperature falls with the floors (warm Level 0 to cool pool, `bathroom_rooms/16` §0 item 5).

**In the game** only one floor is loaded at a time and the view down is a stand-in shaft (`20` §3). Here the floors are real so Red can judge the depth on the spot. The sign says so.

Layout on the cell grid (`40` §11.4, the well as an assembly; this page composes it). Each plate is 8 × 7 cells, x 0…24, z 0…21 (cell indices i 0…7, j 0…6); the void is cells i 2…4, j 3…5 (x 6…15, z 9…18), off the plate's centre on both axes (R4), the same column on every floor:
- **Storeys:** F0 pool hall, deck y 0, open to +9.0 round the well's foot (D5, two pool storeys of 4.5 m), with an A5 gallery at +4.5 along its east side; F1 Run! service floor +9.0; F2 Office +12.9; F3 Level 0 +16.8 (3.9 m storeys, `40` §11.4). About 17 m from F3's rail to the deck, 18 m to the basin floor.
- **One stair core for all floors:** a B3 switchback, 2 × 1 cells at the plates' north-west corner (i 0…1, j 6), 0.15 / 0.30, a landing at +4.5 on F0's gallery. Each floor's lamps and finish continue into it.
- **F3 Level 0 rim (the cut, V1):** galleries on the south and east sides, one cell wide, a solid 1.07 half-wall in wallpaper with an oak cap; the void edge shows the floor's section (carpet, slab, plenum, ceiling grid). One bay of the south gallery set back by a cell (the outline variant). West side: two rooms with windows to the void (E3, sill 1.0); north side: a solid wall with one door. One E7 glass-block floor light, 1 × 2 cells, in the east gallery over F2's room below, lit from beneath.
- **F2 Office rim:** a glass guard with a steel band on the west and north sides only; on the north one balcony bay steps 0.30 down (A4 at the rim, two risers, guard continued). **The bridge (A6), this floor only:** across the void on row j 4, from the west gallery to an east office, 1.5 m wide, guards 1.07, carried from below on a deep tiled-over beam bearing on both rims (never hung, `40` §11.3). East and south: office rooms with glass fronts (E3), panels inside.
- **F1 Run! rim:** a mesh guard (E8) on a block upstand on the east side only; the north side is the service corridor's block wall with one long window; south and west: block walls. **The cascade (C5):** water leaves F1's south edge as a sheet down the void's south wall to the basin, about 10 m; a tiled lip on F1 and a splash trough at the bottom.
- **F0 pool hall:** a tiled basin under the void, water at -1.07 (wading, pool D6), lit from below; porcelain deck round it; a round-arch arcade (D2) along the west side under the void's edge, never on all four. The east gallery at +4.5 (A5) looks into the well's foot. **B4-L** circular stair, 3 × 3 cells at i 5…7, j 0…2 (the plate's south-east), from the gallery down to the deck, about 185° with one mid landing, porcelain treads, a dark nosing on every step, step lights every third tread, a square laylight on the grid over it (`60` §2, §4; `60` Q1, Q2 on its sign). Two ways onto the gallery: the B4-L and the stair core.
- **Light:** each floor its own level's lamps (Level 0 troffers, office troffers, Run! fluorescents, pool wall boxes); every rim lit (R8); the basin's glow rising through the void, so the colour runs warm at the top and turquoise at the bottom. The Relay stand-in on F1's east rim, seen across the void from F2's west guard (rule 12).
- **Main view on arrival:** F3, at the south gallery's half-wall, level camera, looking north across and down the void; one step forward shows the basin 18 m below.

Sign (SP2):
> **SP2 · 井：穿过整栋楼的一口竖井**
> 从 Level 0 的栏杆往下看，经过办公层、Run! 设备层，约 17 米下面是泳池的青绿色水面。每层井口的形状都不一样，只有一层有桥，靠泳池的几层有水顺墙往下流。游戏里一次只加载一层，往下看到的会是替身竖井；这里把四层都真的叠起来，让你判断深度。
> 要你决定：1 用这口井做全楼的地标吗？ 2 井口 3×3 格的方井，还是 2×5 格的窄槽（31）？ 3 桥留不留？ 4 泳池层绕井的大圆楼梯：宽 1.45 还是 1.50 米，顶上用方形天窗还是穹顶（60 Q1、Q2）？

## 5. SP3 · Level 0: the field and its checks (should)

**What Red gets:** the uniform field as the ground (`20` §1) and, at zone distances, one vertical check each (`20` §5 rule 1): the round-1 devices Red kept ("全部保留", 10-31 ~15:58) and the round-3 modules Red called fine ("其他的房间本身是ok的", ~16:27), plus three proposed types to judge.

- **Field:** two chunks, 16 × 8 cells (48 × 24 m), Standard 2.9, columns on the 6 m grid, one troffer per cell, corridors and rooms by the map's own rules. Each event sits off its room's centre lines (R4).
- **Events, in priority order:** (a) V1 the cut: 2 × 2 cells open to a patch of the floor below, 3.9 m down, guarded, the section showing carpet, slab and plenum (`20` §4.1); (b) V2 the drop: a suite sunk 2.40 (16 risers) with a dais at its far end, ceiling held at 2.9 above the corridor, so the room is 5.3 m tall (`20` §4.1); (c) V4 the tall grid: a 2.9 corridor through a 2.2 m arch into a room 14.5 m high, the same wallpaper and grid to the top (`20` §4.1); (d) T_L0_ComputerRoom: +0.30 raised floor, a 1:12 ramp 3.6 m along one side, B1 at the door, low lid 2.6 (`40` §6.2); (e) T_L0_SplitSuite: +1.20 platform by B2, +2.70 gallery by a B4-S spiral in one corner and a flight at the other (R6); (f) T_L0_TrainingRoom with 0.30 m tiers one cell deep (`62` Q3's recommendation); (g) F9 parallel bowl (`31` §5) with the Relay stand-in on the far rim: its head cut by the lid, its body in view; (h) F7 island field (`30` §8), one zone only.
- **Not here:** B4-M, B4-L (Red 17:29), arches except the lobby (`40` §7).

Sign (SP3, one per event, short): name, what it does to the player, and "要你决定：这个放进 Level 0 吗？" plus, on (f), "台阶 0.30 米一层、一格深，还是原来的 0.45 米座位台阶（62 Q3）？" and, on (g), "远处那个人影头被天花板切掉、身子看得见，这样算不算'看得见 Relay'？（规则 12）".

## 6. SP4 · Office: the stack and the pit (should)

- **The stack (V3):** an atrium through three office floors (3.9 m storeys), glass guards with panels at the edges, a laylight; Red enters at the atrium floor (main view up) and reaches the balconies by an F1b slot stair hall (`31` §5: flights crossing in a slot 2 cells deep and 6-9 long).
- **The sunken bullpen (V2):** the cubicle field sunk 1.20 below a perimeter walkway; four bays with panels at 1.35, 1.52, 1.65 and 2.16 m; the Relay stand-in at the far aisle crossing (`20` §4.2, `10` OF-1).
- **Modules:** T_OF_RaisedFloor (+0.30, B7, low panels), T_OF_Boardroom (+0.30/+0.60 tiers, glass front), and a B9 creeping slope (1:30 over 3 cells under a flat lid, the room getting lower without a step).
- **Not here:** T_OF_FileMezzanine needs Tall, which the Office does not have; it stays a Level 0 question (`62` Q2).

Sign (SP4): what each is; "要你决定：放进办公层吗？" and, on the bullpen, "隔板高度哪一档（1.35 / 1.52 / 1.65 / 2.16）？" and "铁丝笼和龙骨架这类看得穿的隔断，Relay 能不能隔着看见你（62 Q1）？".

## 7. SP5 · Run!: the ramp to the dock, and SP6 · the deep pool terrain (may)

**SP5 Run!** The corridor stays a corridor (Red cut the catwalk rooms G1/G2: "因为它直接改变了整个房间的性质本身", 10-31 ~15:58).
- The spine as a 1:8 ramp, 24 m long, 3 m down (`20` V6), Run! B's section tightening (services to 2.45 m clear, a fire-door wall every 9 m with B1 thresholds ±0.30, T_RN_Thresholds).
- At its foot T_RN_DockRelease: a Tall 5.4 loading dock, +1.22 dock edge (48 in) with a chain guard and swing gate (`63` Q2's recommendation), B2 down at one end, a roll-up door half raised on a ramp to the street at night under sodium light (`63` Q3): the brightest thing (Run! rule 5).
- One D7 riser shaft (`30` §8): a 1-cell soffit opening into a lit service shaft 8 m tall with a caged ladder that starts above reach; one E8 storage cage along the spine.
- Armed/tripped key: after the trip only battery heads remain and the goal door is the brightest thing.
- Sign: "要你决定：走廊两侧的小房间能进还是只能看（63 Q1）？卷帘门下面露出什么（63 Q3）？".

**SP6 deep pool terrain** (the pool level's own splices, after the pool line's districts; never duplicates a PL_ module):
- The route of `40` §7.4: corridor → arch → beach entry (C3, 1:12-1:15) → steps up to a terrace → bridge over a channel (C4) to an island (A7) → weir down to a basin → B4-M helical stair (4.36 m outside, well 1.6, `60` §2) up to a gallery under an off-centre dome with an oculus (D4) → a stair into water (C7) toward a grotto (C6, crown 2.2-2.6).
- T_PL_Casbah (F6: blocks at 0 / +0.6 / +1.2 / +2.4 joined by flights and bridges, the lowest flooded), one C9 wet block, one C8 covered basin (access floor over a full basin, missing panels guarded).
- Sign: "要你决定：这些地形放进泳池层吗？螺旋楼梯中途要不要歇脚平台（60 Q3）？B4-M 的栏板用实心瓷砖还是玻璃（60 Q4）？泳池盖布卷轴留不留（63 Q6）？".

## 8. Red's open questions, by district

| Question | Source | District |
|---|---|---|
| Water flowing in the flume, on or off | `61` §8 P7 | SP1 |
| Catch pool 0.9 or 1.07 m | `63` Q8 | SP1 |
| The empty start light turning green, once a run | `63` Q7 | SP1 |
| Keep the M1 office ceiling over the court | this page | SP1 |
| The well as the building's landmark; square or slot; one bridge | `20` §3, `31` §5 | SP2 |
| B4-L width 1.45 or 1.50; laylight or dome | `60` Q1, Q2 | SP2 |
| Training-room tiers 0.30 or 0.45 | `62` Q3 | SP3 |
| F9: head cut by the lid counts as visible? | `31` §5, rule 12 | SP3 |
| See-through barriers and the Relay's sight | `62` Q1 | SP4 |
| File mezzanine stays in Level 0 | `62` Q2 | SP4 sign |
| Stub rooms enterable or seen only; what shows under the dock door | `63` Q1, Q3 | SP5 |
| B4-M mid landing; solid or glass ribbon | `60` Q3, Q4 | SP6 |
| Pool cover reel | `63` Q6 | SP6 |
| Every zone: put it in the game, yes or no | rule 5b | all |

Still open and not tied to a district: `62` Q4-Q7 (smoking, the vacant suite's faces, build order, materials), `63` Q4, Q5, Q9, Q10. They go in the Chinese decision doc (handoff item 4).

## 9. Walk tests (the Review stage's space lens)

- From the hub into each district and back; every district's main view as stated.
- Every raised or sunken area of 2 cells or more has two ways onto it; the spiral is never the only one.
- No step over 0.30 m without a stair (player step 0.3); no gap a walker can fall into; guards 1.07 at drops over 0.76.
- Arches walked under spring at 2.1 m or more; the Relay stand-in fits every route it would take.
- SP1: tub reachable both by the shaft and by the stair core; the ride ends in the pool and the walker can climb out by the tiled steps; no steel anywhere except the crossbar, the chain, the start-light post and flange bolts.
- SP2: every floor reachable by the stair core; the B4-L lands flat on a cell line (`60` §0 item 5).
- Pool districts: porcelain white, dark nosing on every pool step, F1 light, wrong-turquoise water, tile joints visible at 3 m.

## 10. Dead ends to avoid

- The slide as an object in an empty box hall (LS11, r4t, `61c`): Red 11-02 07:31.
- One device per box room (r1-r3 dioramas): reads as a catalogue (Red 15:38, 16:06).
- Symmetric or ring layouts: R4; an assembly is placed in one of eight orientations.
- Industrial supports: galvanised columns, saddles, braces, grating (Red 10-31 17:15).
- Warm cream tile (Red 17:19); a teal riser band (17:24); large spirals outside the pool level (17:29).
- A catwalk through a Run! plant room (Red ~15:58).
- Literal craziness: glitches, warping, impossible materials (`20` §5 rule 6).
