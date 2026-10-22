# 60 · Walkable stair: the map contract for 关卡设计 (2026-10-22 13:0x; v1.1 13:4x after 关卡设计's code check)

**Status: DECIDED by Red, 2026-10-22 13:00, in the 游戏视觉 chat:** "楼梯间剩下的五项全部按推荐（选 A、C22 合进去之后做真楼梯、钢门用真实声音、到达门不挂不亮的 EXIT、优先级 1 的素材可以下载）".

**When:** the step after C22 lands as 关卡设计 built it: one 3 × 3 vestibule cell X, the seal blackout, every floor at y = 0. Until then the vestibule ships, dressed in look A. This contract changes the map only after that landing; it does not block C22.

Geometry source: `10_directions.md` §1.1–§1.3 (look A, two levels per stair).

**v1.1 (13:4x):** 关卡设计 checked this contract against the C22 code (wf_5d7125fd-8b5; full plan with file:line in `~/FrontRoomsMapWork/notes/walkable_stair_map_plan.md`) and **keeps N′ = N** (far door directly below the EXIT). All map changes are mechanical and theirs.
- Simulation, 1000 seeds × 3 floors: no EXIT lost. Floor 1 widens its band on 2.6 % of runs (1.8 % today). X1 is held to chunk-local j 1..5, so X2 stays in the one no-shift chunk.
- §1, §3 (b) and the shell rules in §6 below are amended to their findings.

## 1. Cells

N is the maze cell in front of the exit door, on the maze side. The exit door is on N's edge toward the stair. **The door normal is +z (map north) on every floor** (关卡设计 v1.1): X1 = N + (0, 1), X2 = N + (0, 2).

| Cell | Offset | What it is |
|---|---|---|
| X1 | N + 1 along the door normal | Door end: the exit landing on top, the far landing below. Both doors are in X1's maze-side edge. |
| X2 | N + 2 along the door normal | Turn end: the turn landing at half height. |

- 1 × 2 cells. **v1.1:** the map builds X1/X2's reserved edges as walls on the cell lines, papered on both faces. The shell's walls sit **inset**, as C22's vestibule does (WallHalf + trim + 0.01), so clear inside is about **2.66–2.74 × 5.66 m** (§6.1). The first draft's 2.84 × 5.84 m is superseded.
- **Reserve X1 and X2 on both floors:**
  - on the old floor, they are the top of the shell;
  - on the new floor, they are its bottom.
  - Neither floor may place anything in X1/X2, and neither may open any edge of them except the door edge.
- **X1 and X2 are off the Relay's nav graph on both floors.** C22 already does this for X.

## 2. Edges (EffectiveEdge)

| Edge | Old floor (y_old) | New floor (y_old − 3.50) |
|---|---|---|
| N ↔ X1 (door edge) | **EXIT door**: steel leaf, crossbar on the maze face, sign on the frame | **Far door**, directly below the exit door: the new floor's start door, the only door with pull trim |
| X1 ↔ X2 | inside the shell, not a map edge (open) | same |
| every other edge of X1, X2 | Wall (shell) | Wall (shell) |

- The new floor's **start cell N′ is at the same (x, z) as N**, one floor down. The start door opens from X1 into N′.
- So each floor starts where the previous floor's exit was. This fits C22's "the new map reseeds in place".

## 3. Heights

- **Floor to floor 3.50 m:** 2 flights of 10 risers × 175 mm, 9 treads × 280 mm. Flights 1.12 m wide. Landings about 1.57 m deep and well 0.42–0.50 m with the inset walls (§6.1; the first draft had 1.66 m and 0.60 m).
- **Exit landing** at the old floor's y. **Turn landing** in X2 at −1.75 m. **Far landing** at −3.50 m, in X1, under the exit landing.
- A slab closes the shaft 3.30 m over the exit landing. Nothing goes below the far landing.
- **Option (Red has not asked for it):** 3.96 m floor to floor (24 × 165 mm risers) fits the same cells, about +0.3 s of walk.

**Map root y. Two ways; recommended: (b) rebase.**
- **(a) Real drop:** floor k's map root at y = −3.50 × (k − 1).
  - Simple. But y keeps falling (−35 m by floor 11), and anything that reads world y has to follow: the wall grime uses world y, and so do the lamp pools.
- **(b) Rebase (recommended; v1.1 = 关卡设计's code-checked order).** The map root never moves. The map bakes world values at chunk build: `_CeilingHeight` = root y + ceiling (MapWorld ~1653; read by FrontRoomsSurface.shader's grime bands and copied by GlassRT), and stored world points for Door.position and Window.position. So the shell and the player move instead. After the EXIT has latched, with the player on the exit landing and at least 0.3 s after the latch:
  1. Park;
  2. NextFloor (1×2, N), regenerated at y = 0;
  3. shell + player + camera +3.50 m, with the CharacterController off and on and Physics.SyncTransforms (auto-sync is off);
  4. PlayerRebased(delta);
  5. shell join (the shell bakes its own `_CeilingHeight` after the move);
  6. PlanExit;
  7. Begin.
  The listener's velocity is off and footsteps drop y, so there is no Doppler and no step. Both doors are shut, so nothing outside the shell is visible during the jump.

## 4. Path, seal, timing

- **Autopilot / path:**
  - N → through the exit door → exit landing (X1, top) → flight 1 down along the **latch side, +x** (the map hinges the EXIT on −x; v1.1) → turn landing (X2) → flight 2 down along −x → far landing (X1, bottom) → far door → N′.
  - About 8–9 m: 2.7 s walking (3.2 m/s), 1.6 s running.
- **Seal:**
  - Keep C22's depth gate. 1.40 m into X1 along the door normal is past the leaf swing (0.9 m leaf) and inside the ~1.57 m landing (§6.1).
  - The exit door shuts and latches itself there (SealExit), as built.
  - No blackout is needed: the walk covers the rebuild (≤ 0.3 s measured).
- **Far door:**
  - Its shut and open rules move to **path distance**: it is about 1 m flat from the exit door, so flat XZ would misfire.
  - If the new floor is not ready when the player reaches it (worst case about 3 s), the far door holds: the lever goes down and the leaf gives 1–2 mm. It never reads as locked (`10_directions.md` §1.6, Figma SW09 `3527:7410`).
- **Light** (`10_directions.md` §1.3):
  - **Old floor:** the exit lamp hold (cells within 2 of N Steady). The old floor's exit lamps stay lit until teardown, with up to 6 forced shadowed spots within 10 m of the shell, desktop only. On WebGL, dim them to 0 as C22 does.
  - **New floor:** the lamps round N′ are held until the far door swings open, as C22 already does.
  - The stair's own lights (look A: two spots 0.6 m apart per landing, soft shadows, all Steady) are part of the 游戏视觉 stair prefab, not map lamps.
- **Exit placement:**
  - N must be a Standard (2.9 m) zone.
  - Use Kit_ExitSign_End when the exit door is on a corridor side wall.
  - The planner rejects an EXIT whose N, or one of the 2 cells in front of it, has a Window edge with line of sight to the sign.

## 5. What 游戏视觉 delivers

- The stair shell prefab, look A: walls, slab, pan flights, landings, picket guards, wall rails, standpipe and valve per door landing, twin-head unit, its own lights. Every floor is identical apart from the numeral plates.
- The A13 chain/padlock states on the exit door, the far door's pull trim, and the hold pose.
- Anchors for the map: the shell root at X1's door-edge centre, the exit and far door hinge points, the seal-gate plane and the rebase point.
- The landing order follows `VISUAL_CHAT_TASKS.md`: first the vestibule dressing for C22 as built, then the walkable shell.

## 6. Shell rules agreed with 关卡设计 (v1.1, 13:4x)

1. **Walls: inset, no map change.**
   - The shell's walls sit inside the map's reserved-edge walls (WallHalf + trim + 0.01, as C22's vestibule).
   - The clear width (2.66–2.74 m) keeps both flights at **1.12 m** (the 44 in code minimum) and narrows the well: **well = clear − 2.24 m, at least 0.40 m** (0.42–0.50 m).
   - The clear length of about 5.66 m keeps 9 treads × 280 mm and makes both landings about **1.57 m** deep. The 1.40 m seal plane still clears the leaf swing.
   - No map "ghost walls".
2. **Hinge:** the EXIT hinges on −x and latches on +x (map as built). Flight 1 runs along +x. No map change.
3. **Colliders:** one smooth ramp collider per flight plus a box per landing. The visible steps carry no colliders. This keeps the CharacterController (stepOffset 0.30, skin 0.03) grounded descending 175 mm risers at 3.2–5.3 m/s.
4. **Anchors and API** (a `FrontRoomsStairShell` component on the shell root, visual-owned):
   - `ExitDoorPoint` + forward (the 1.40 m seal plane);
   - `FarDoorPoint` + hinge;
   - `FloorToFloor` (= the jump, 3.50);
   - ordered path waypoints for the autopilot: exit landing, flight 1, turn, flight 2, far landing, far door;
   - a far-landing trigger plus `PathProgress(feet)` for the far-door rule;
   - far door `Open()` / `Hold()` / `Close()`, with `Moved` / `Latched` events;
   - a static shut EXIT leaf and the exit-level wall shown from the stair side (the map's EXIT leaf goes away with NextFloor);
   - the far-door wall's maze face takes N′'s paper (a Surface slot the map fills);
   - every FrontRoomsSurface material on the shell sets its `_CeilingHeight` from its own world y after the move;
   - a latch-sound anchor on the shell for 声音设计 (not the map hinge 3.5 m below).
5. **Numerals:** from the game. The shell exposes `SetFloor(int floor, string label)` and the map passes `FloorLabel(floor)`. The tokens are set at runtime from 信息设计's `layout.json` / glyph quads (`NOTE_1022_info_design_signs.md`), so one shell serves every floor.
6. **Far door swing:**
   - The leaf swings **into the stair, over the far landing**: the code-true direction for a floor-to-stair door (egress from B1 is into the stair). Pull lever on the stair face; NOT AN EXIT and lever trim on the floor face.
   - Nothing swings into N′, so the map's DoorClearDepth (1.2 + WallHalf) in front of the start door is enough.
   - Auto vs USE for the far door is 交互设计's call. If it is automatic, the autopilot's last waypoint stops on the latch side, outside the 0.9 m swing.
7. **Movable in one frame:** no Static flags, realtime lights only (look A: six soft-shadowed spots, all Steady), box probes as children of the shell, no scene lightmaps. On WebGL any baked light lives in the shell's own textures or vertex colours.
