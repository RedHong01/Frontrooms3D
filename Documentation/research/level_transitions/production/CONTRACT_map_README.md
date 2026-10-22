# Level transitions · CONTRACT_map.diff · README (for 关卡设计)

Date: 2026-10-22 15:5x PDT. Written by the 游戏视觉 workflow `level-transitions-prod` (Build stage, account A resume after switch #3). First cut 14:22 in the private clone `W/proj_lt` (main `2c450e11` + A12 r2 `50ec80b2`). **Re-cut by 3-way onto today's main after your C22 (15:39), V1 lens hook (15:40) and G1 (15:52) landed**: MapWorld `83c19911`, interaction tests `dc732287`, MAP_GENERATION `964b4b38`. **Nothing here is in main.** The map owns every file in this diff; 游戏视觉 does not apply it.

**Decided by Red, 2026-10-21, "全部按推荐" in the 游戏视觉 chat:** a Frame on every zone border (the Office owns every frame and base), Renovation on about 3 in 10 crossings, the colour stays V5. LT16 defaults that hold: free posts get a collider; the base is on Office walls only; fix the corner flicker in the map first (C3). **Not decided and not touched:** Close (`FrontRoomsMap.cs` / `Resolve` unchanged), the light rule (no lamp code changed), the media download.

**When:** it needs the visual package `level_transitions` in main first (the kit it calls: `FrontRoomsTransitionKit.BeginChunk`, `View`, `ChunkBuild`, `RenovationShare`, the `FrameKit` numbers). That package is inert on its own. The Alpha gate is gone (Red 15:02) and C22 is in, so the order is only your queue: the visual package → this diff, with or after codex C3 (the walkable stair adds one line to `TransitionExcluded`).

## Bases (today's main, read 15:5x: HEAD `a50ee7da` + working tree with C22, V1 and G1)

| File | Base sha1 | Lines (`git apply --numstat`) | Hunks |
|---|---|---|---|
| `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs` | `83c19911` | +144 / −4 | T1–T6 + C22 exclusions |
| `Assets/Scripts/FrontRoomsMap/FrontRoomsModuleUnits.cs` | `f374d320` | +11 | T7 |
| `Assets/Scripts/FrontRoomsMap/FrontRoomsLevelProfile.cs` | `d9e17bd0` | +4 | T8 |
| `Assets/Levels/FrontRoomsLevel0.asset` | `07514c33` | +2 | T8 |
| `Assets/Editor/FrontRoomsMap/FrontRoomsMapInteractionTests.cs` | `dc732287` | +261 | T9 (13 checks) |
| `Documentation/MAP_GENERATION.md` | `964b4b38` | +10 / −1 | T10 |
| `Documentation/LEVEL_MODULE_SPEC.md` | `53ef2ea1` | +1 | T10 |

- `CONTRACT_map.diff` sha1 `ebacd14c`. `git apply --check` is clean on main at 15:5x (after G1); applied, every file equals the merged clone copy.
- **How it was cut:** the 14:22 hunks were merged 3-way onto MapWorld `9b82c286` (A12 r1 + fixes, M2, GD3c: 0 conflicts, changed lines byte-identical to 14:22), then onto C22 as landed (`10f71ffc`: 0 conflicts in MapWorld; one trivial conflict in the test call list, resolved by keeping your five C22 calls and adding `Transitions(roots, profiles)`), then onto V1 (`83c19911`: 0 conflicts), then onto G1 (interaction tests `dc732287`, MAP_GENERATION `964b4b38`: 0 conflicts; the +/− lines are byte-identical to the V1 cut, only offsets moved). G1 does not touch MapWorld. The C22 changes in the view (below) were made on the C22 base.
- **C22 changes in this cut** (T2/T3): the view's `edge` is `EffectiveEdge(lo, hi)`; `excluded` and `BorderRenovated` use a new private `TransitionExcluded(c) = IsReserved(c) || (hasExitArea && Chebyshev(c, exitFront) <= 2)`: the start area, the exit area X (so the EXIT door edge N | X) and C22's exit-lamp hold zone round N, where C23 mounts the LIT sign. T9 gains check 13: with every crossing forced renovated (`ShareOverride = 1`), none touches X or the hold zone, while every crossing elsewhere within 8 cells is.
- Every hunk is marked `// TRANSITION (Tn)`. The kit is reached through **one field** (`transition`), not new `BuildEdge` parameters, so outlets' `faceData`/`faces`, A12's door lines, GD3c's glass lines and C22's lines keep their text.

## Each hunk: what, why, which decision

| # | Where | What | Why (decision) |
|---|---|---|---|
| T1 | `MeshBuilder.Append(v, n, uv, t)` | Appends raw geometry to a block builder | Office-wall kit pieces (linings, portals, end caps, free posts), the band's paper (tears, remnants, islands) and cut carpet join their 6 m block's mesh: **0 extra draws**, and they keep the block's `_CeilingHeight` grime band. Frame + Renovation |
| T2 | fields `transitionView`, `transitionViewCache`, `transition`, `transitions`, `renovationShare`; `TransitionView()`; `TransitionEdgeKind(a, b)` | One read-only view per cache: theme, ceiling, edge kind as built (`EffectiveEdge`), excluded (`TransitionExcluded`), renovated (`BorderRenovated`), arch (`ArchOpening`), seed, the theme materials and `trim` | The kits stay pure; the map stays the only reader of its cache and rules. Later: X1/X2 with the walkable stair, station rects (M4), one line each in `TransitionExcluded`. Every decision |
| T3 | `public bool BorderRenovated(a, b)`, `const RenovationSalt = 977` | An Office \| Level 0 edge that is not a wall, both cells not excluded, `MapHash.Unit(MapHash.Hash(seed, lo.x*2 + east, lo.y, 977)) < RenovationShare(renovationShare)` | **The map decides which crossing is renovated**: seeded, no revision, tier or build order, so both chunks, rebuilds and revisit shifts agree while the edge is a crossing. Salt 977 is unused by `MapHash` (11–73) and the map (89, 97, 211, 223, 307). Renovation 3 in 10 |
| T4a | `BuildInto`, after `Solid` | `TransitionFurnished(cell)` (a room Furnish dresses, a module room, or another chunk's cell) and `transition = FrontRoomsTransitionKit.BeginChunk(TransitionView(), coord, root, BlockOf, blk => HeightOfClass(blk % heights), collision box, TransitionFurnished)`; null when the kits or the profile are off | One chunk's kit. The renovation's render-only props never stand in a room the dressing contract furnishes (LEVEL_MODULE_SPEC §6) or in a module room, and the kit never reads another chunk's rooms |
| T4b | the two `Solid` slabs | When `transition.TakesCell(...)` (a Level 0 cell of a renovated crossing), both full slabs go to `collision` only (same boxes, same order); else today's `Solid` | The cut carpet, slab strip, ceiling hole and new tiles are render only. Renovation |
| T4c | after the two `BuildEdge` calls | `transition?.Corner(cell)` | The cell's south-west lattice point (each point once): free posts, end caps, corner guards. Frame |
| T4d | after the cell loop, before the block renderers | `transition.Finish(box, append, chunk.meshes)`: the kit's block pieces go into the builders, its own renderers and props are made (no MaterialPropertyBlock, no colliders); the returned boxes (free posts) go to `collision.Box`; then `transition = null` | The only collision the kits add: one box per free post (LT16 default "free posts get a collider") |
| T5a | `BuildEdge`, open edge | `transition?.Open(a, b, start, along, height)` before the return | A border portal (finished, or unfinished on a renovated crossing). Frame |
| T5b | `Piece`, after the B0 extents | When `transition.Piece(from, to, bottom, top, fA, tA, fB, tB, blockIndex)` is true, the map adds **only its collision boxes, the same extents in the same order** (one full box, or a box per skin) through `transition.Collide`; the kit builds the render | Render-only bead insets beside an undressed border arch, end-cap insets at a free border wall end, the renovation band's per-drop skins and halos, and the band posts' reach (B0.1's own rule with the band's stage in the finish). **Collision byte-identical** (T9 test 1). Every other piece keeps today's path |
| T5d | `BuildEdge`, before the wall pieces (wall) and before the opening's pieces (door, window, arch) | `transition?.Edge(kind, a, b, start, along, height, c, width, openingTop, sill, sideA, sideB, wallA, wallB, mayExtendStart, mayExtendEnd)` | **The map's own computed opening**, so a module's per-edge `archWidth` (M4) and C22's EXIT door need no second copy of the rule. Called before the pieces because the piece split needs the edge first |
| T6 | `AddWallFaces` | `baseTop = transition != null && zone.theme == Office ? ModuleUnits.CoveHeight : 0f` | Outlets C-1 keep `BaseClear` 0.03 above the kit's 0.10 rubber base on Office faces (T9 test 6) |
| T7 | `ModuleUnits` | `BeadInset .012`, `PortalPilaster .12`, `PortalDepth .20`, `PortalHeaderDrop = BulkheadDepth`, `SharedPilaster .24`, `FreePost .20`, `BarWidth .035`, `BarHeight .005` | One source for the keep-clears in LEVEL_MODULE_SPEC (T9 checks they equal the kit's) |
| T8 | `FrontRoomsLevelProfile` "Dressing" + `Level0.asset`; working copies in `TakeProfile` and `ApplyLive` | `public bool transitions = true;` `[Range(0, 1)] public float renovationShare = .30f;` | Data-driven share; the Level Designer's live tuning re-reads it for chunks built afterwards; `transitions = false` builds exactly as before |
| T9 | `FrontRoomsMapInteractionTests`, section "Transitions" (13 checks) | (1) collision with the kits on = kits off byte for byte + one 0.20/0.24 full-height box per free post; (2) the same doors, windows, lamps; `transitions = false` builds no kit object; (3) share 0.30 ± 0.02 over 20 seeds × 81 chunks, never a wall; (4) the same from either cell, the same kit geometry after a rebuild, unchanged after a revisit shift where the edge stays the same crossing; (5) not inside the start area; (6) `baseTop` 0.10 on Office faces (0 with them off); ModuleUnits = kit numbers; (7) C22: nothing renovated in the exit area or the hold zone | Determinism, layout and collision freeze |
| T10 | MAP_GENERATION.md, LEVEL_MODULE_SPEC.md | A "Level transitions" section; the dressing contract's keep-clears | Contract text |

## What A12 (landed 15:11) means for this diff

- No stop and no push side: the steel frame is symmetric and nothing reads `door.swing`. The 10-08 to-do "rebase the door hook on single-acting doors" is withdrawn.
- `FrontRoomsTransitionKitTests` G7 sweeps every kit piece and prop against the leaf at 0–95° on both faces plus the break rest: 0 hits above 0.012 m (seed 516574485, 2554, reduced tier).
- `FrontRoomsRelayNavTest` 60/60 with the free-post colliders in; 23 doors broken, 0 from the swing side.

## Verified

**In `W/proj_lt` (main `2c450e11` + A12 r2, 14:1x–14:5x, UNDER LOAD: 1-minute load 37–146):** Roslyn `--all` 0 errors; `FrontRoomsMapInteractionTests` **190/0** (A12 r2's 178 + 12 transition checks; share **31.2 %** of 4,797 crossings, 2.96 crossings and 0.92 renovated per chunk; collision 25/25 chunks byte-identical prefixes, 5 post boxes = 5 free posts); `FrontRoomsRelayNavTest` **60/60**; `FrontRoomsDoorShotTests` **234/0**; `FrontRoomsMapVerification` seeds **100/100**; `FrontRoomsLevelDesignerTests` **140/0**; touch playtest **85 PASS / 0 FAIL**; autopilot 2554, 20388 and `-autopilotDoors` 2554 PASS; transition play check 58 border crossings with a player capsule, 0 blocked.

**On main before C22 (`W/proj_lt2` = main `a50ee7da` with A12 r1 + fixes, M2, GD3c + the visual package + the cut onto `9b82c286`, 15:41):** compile 0 errors; kit tests 21/0; the 7 fixed shots in pick mode pixel-identical to the build's, plans identical.

**On today's main after C22 + V1 (`W/proj_lt4` = main + the visual package + this diff):** Roslyn `--all` 0 errors (macOS and iOS defines). Unity suites: `20_build.md` §5.2.

## Asked of you

1. Accept the free-post colliders as the one collision change (LT16 default; T9 test 1 isolates them).
2. Keep `EffectiveEdge`, `IsReserved`, `hasExitArea` / `exitFront` (`ExitFrontCell`) as `TransitionExcluded` uses them.
3. Codex C3: give the view a `PostAt(GridCoord)` (four arms, finish per quarter, owner, special, FreeEnd). Until then the kit uses B0.1's `CornerReach` exactly as main has it, with the band's stage folded into the finish only at posts next to a renovated drop.
4. Put the T9 section in your suite.
