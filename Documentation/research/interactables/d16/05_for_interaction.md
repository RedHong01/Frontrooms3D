# D1.6 · A12 double-acting doors: facts for 交互设计 (and a record for Red)

From 游戏视觉's D1.6 Build stage, 2026-10-22 (started on account A, session local_5656cffd; finished on account B, session local_a9a1dce2). Clone `W/proj_d16` = Red's main HEAD `aca6240b` + working tree as of 16:56 (A12, GD3c, C22, G1b and the stairwell safe zone all in), plus the D1.6 build. Nothing here is in main yet. Red decided A12 on 2026-10-22 ("全部按推荐：A 双向门，保留把手，站在门板那边关门时照现在让一步，Alpha 之后上"), so this is a **record, not a decision aid**. Red's 15:02 message lifted the Alpha gate, so D1.6 lands as soon as it is verified.

## 0. Update after fix 1, fix 2 and fix 3 (2026-10-24; 2026-10-29 on Unity 6000.6.3f1): what changed for 交互设计

These lines replace the matching rows below, and §1 and §2 have been edited in place to agree with them (fix 2 and fix 3, 2026-10-29); everything else in this note stands. Details: `20_build.md` §11 (fix 1), §12 (fix 2), §13 (fix 3).

| Item | Now |
|---|---|
| Pivot (VL319) — **a deviation from 10_design D3 and the A12 rule, needs 交互设计's and 关卡设计's OK** | The kit leaf turns about its **own pivot axis on the wall centre plane, 36 mm into the opening** from the map's hinge point (the leaf sidecar's `pivot_axis`), as a real centre-hung double-acting pivot is set in from the jamb. The map's hinge, collider and swing rule are unchanged; the rig adds the translation R(−a)·p − p, render-only. Result (Blender T2, 0–95° both ways): 0 mm into the reveal, 0 mm into the casing, hinge clearance 8.8 mm (steel) / 14.0 mm (veneer); only the soft brush seals touch. Still no A2 offset (the pivot stays on the centre plane) |
| Collider vs kit leaf | Because of that 36 mm, the map's leaf collider (what the leaf stops on, what the Relay's blows and the aim ray hit) and the kit leaf differ by at most 2·p·sin(a/2): 0 mm shut, 9 mm at 15°, 28 mm at 45°, 53 mm at the 95° rest. The two ways to settle it: (a) keep the deviation (render-only; a body or the aim can touch air up to 53 mm from the drawn leaf near full open), or (b) 关卡设计 moves the map's hinge 36 mm into the opening on the centre plane so collider and drawn leaf coincide (a map change: their contract, not D1.6's). Putting the pivot back on the map hinge instead re-opens VL319 (the leaf runs into the hinge-side reveal from 24.5° and the casing beyond 76°). 游戏视觉 recommends (b) after landing; until then (a) |
| Seams, a control that can see light | An eye on the latch seam plane at 2.5 m, the far room lit: **brush seals on: 0 lit px** at hinge, latch, head and floor, both faces, Lobby and Office; **brush off: 814–842 lit px** at the latch, also with the leaf 1° ajar. So the test does detect a slit, and the kit's black seal fins are what close it. The kit ships with them |
| Handles, latch | Unchanged (lever on FREE, knob + deadbolt on LOCKED, keyhole Y 1.000 both faces, KnobTurn 0.92). The double-bevel latchbolt follows the handle: retracted while the lever / knob is down, out again when it returns (a spring latch is out whenever the handle rests, open or shut; that is what lets a pushed leaf latch from either face) |
| Unlocked door | Keeps the LOCKED model with the deadbolt drawn, from both faces, **through a chunk rebuild and a drop + stream back** (rebuild test 2026-10-29: 92 / 92 doors dressed the same after RebuildChunk, 53 / 53 after a stream; the unlocked door's deadbolt 1.00 and the broken door's strike off kept) |
| Lock rule flipping mid-run | A door whose `LockedByRule` flips after it was built is dressed again (FREE ↔ LOCKED) by the rig driver's audit, so the unlock shot never aims at a lever rose |
| The key's hand-off (DECISIONS #37) | Fix 2 measured it with the REAL key (`FrontRoomsKeyAssemblyTests` "hand-off (real key)": a `DressKey` key through `FrontRoomsDoorShot.BeginKeyPickup` with a `KeyHandoff` listener): one hand-off at 0.750 s, viewport (0.65, 0.30), depth 0.350 m; on the HeldItem layer before it; the root inactive from that frame; off the HeldItem holder list and destroyed at 0.950 s. In Play with HK1 live (the HUD harness pickup) the HUD key then flies from the hand into the panel slot (`20_build.md` §12). (The map's DoorShotTests "21 hand-off" uses a placeholder cube, not the kit's key.) The tag token and the HUD glyph agree for every zone and floor (7,203 cases). **Fix 3:** measured in Play at the swap frame per tag shape (frame before KeyHandoff vs the KeyHandoff frame), the real key's silhouette centroid lands on the B glyph's within 0.2 px on desktop (1920 x 1080) and 0.9 px on the phone (2622 x 1206), through per-shape held offsets in `FrontRoomsKeyAssembly.PoseHeld` (2-3 mm in the held frame; the depth unchanged). Fix 2's "64 px jump" was the harness's 4:3 batch screen against a 16:9 capture, not the game. On a station floor the M3 floor key is dressed too (contract v8) |
| Printed type | **Fix 3: the key tag's token is HELD too** (`ShowTagTypeShipped = false`: blank paper) until 平面视觉 · 信息设计 signs off the 3D tag type on `images/d16x_tag_type_proof.jpg`; until then the token appears only on the HUD glyph at the swap. Every tag shape takes its token as set, B10+ at 0.8x on the same baseline (DECISIONS #33; fix 2's x1.20 round-tag fit is gone). Courier Prime Regular (#23). Number plates, the key rack's hook labels and the engraved signs stay held blank (`ShowKitType = false`) as before; on a floor-key floor the plates take the floor key's identity (`KeyZoneAt`) |
| Surfaces | Fix 2: the steel leaves' kick and push plates are satin stainless (they were the mirror `Prop_Aluminium` and read black at the foot of every LOCKED door); the key is a worn satin brass (`Prop_BrassSatin`, smoothness 0.45). **Fix 3:** while a key is held (up to the HUD hand-off) a weak point light sits at the camera (`FrontRoomsHeldItem` held fill, 0.05, range 0.6 m; nothing beyond 0.6 m from the lens is lit), so the held brass reads 0.12 linear at a level look (was 0.07, dark brown) |
| WebGL | Dressing is off in WebGL players (`DressOnWebGL = false`); doors and keys stay the map's cubes there until the WebGL track measures them |

## 1. What the kit now does for "away from the player"

| Item | As built |
|---|---|
| Mode | Every map door is **double-acting** (`FrontRoomsDoorRig.Acting = DoorActing.Double`). The single-acting stop door stays as a dormant rig mode (`DoorActing.Single`, the 9 stop kits in main, A2 offset, handing mirror). D1.6 never selects it |
| Pivot | On the wall centre plane, **36 mm into the opening from the map's hinge point** (the leaf sidecar's `pivot_axis`; fix 1, VL319). No A2 offset, no handing mirror. The leaf turns by the map's hinge angle; the rig adds the render-only translation R(−a)·p − p so the drawn leaf turns about its own pivot. A deviation from 10_design D3 (§0: collider vs drawn leaf ≤ 53 mm at 95°; needs 交互设计's and 关卡设计's OK) |
| Frames | `Kit_DoorFrame_Wood_DA`, `Kit_DoorFrame_Steel_DA`, `Kit_DoorFrame_Steel_Alu_DA`: stopless, a sleeve casing on both faces over the map's trims, a 12 mm saddle, pivot caps only, black brush seals and fins on the centre plane. No stops, no butt hinges, no closer shoe |
| Leaves | `Kit_DoorLeaf_{Veneer, Veneer_Oak, Steel, Steel_PaintedMetal, SteelLite, Ward}_DA`: symmetric (no 3 degree bevel), radius hinge stile R 22 mm about the pivot, pivot plates at the top and the foot, 3 mm gaps at the latch and head |
| Self-centring | **None.** No spring hinges, no closers on map doors. The leaf rests wherever the map leaves it (rig = the map's hinge + render-only jolts) |
| Handles | Kept: lever + rose on FREE doors (both faces); knob + deadbolt + keyed cylinder on LOCKED doors (both faces). Keyhole at **Y 1.000 on both faces** (X ±0.0315 from the centre plane, Z 0.920 from the pivot). `KnobTurn` 0.92 unchanged |
| Latch | New double-bevel latchbolts (bored 13 mm, mortise 19 mm; no deadlatch plunger) and strikes with an eased lip on **both** edges, so a leaf shuts from either face. A single-bevel latch cannot |
| An unlocked door | Keeps the LOCKED model; its deadbolt is drawn for the run, from both faces (C15). It opens on E |
| A broken door | The strike comes off the frame (render-only fall, 0.4 s) and lies on the floor; the leaf hangs crooked by following the map's collider; the deadbolt stays thrown on a broken locked door |

## 2. The rig's map contract (as agreed; the rig needs nothing else)

Checked against main `43268c36` + working tree, 2026-10-29 19:0x (MapWorld `0b406078` with the first-room group, Q1 B and Relay v2 V0, DoorShot `ef941ca9` with HK1 / KP-1, ModuleEditing `ed81b895`, KitSeam `dba67c53`, Game `9c4da7c9`); first written against main at 10-22 16:56:

- `DoorFace` is geometric: S = the door root's +X face, P = −X, no handing mirror (`FaceOf`, KitSeam `dba67c53`). The rig resolves a face through `FrontRoomsMapWorld.FaceOf`, so S = +X is read from the map, never assumed.
- `door.swing` (+1 sends the leaf toward root −X) is set by the map before every open, rattle, key turn and break from a latched leaf (`SwingAwayFrom`). The rig's own jolt (`Jolt`, the unlock's 1.5 mm shift) turns the leaf about the pivot toward −swing·X, i.e. away from the opener. All other leaf motion (crooked hang, rattle, Relay blows) is the map's `Door leaf` collider pose, which the rig copies; no map change.
- `Game.BeginUnlock` passes `pull = OnSwingSide(...)`, false after the key turn's `SwingAwayFrom`: every unlock from a latched leaf is the push path, so the knob turns at 0.92 s and the leaf leaves the frame at 1.00 s.
- Shutting from the leaf's side keeps today's step aside (`DoorPulled`): map behaviour, untouched by the kit.
- The map contract diff for the kit itself is `data/contract_map_D16.diff` **v8** (fix 3; M1 DressDoor, M3 dressed keys in `NewKey` for zone keys **and the M3 floor key**, key hosts, M4 the unlock's key, M9 the Level Designer palette filter; M7 `IsUnlocked` was dropped in fix 1, `DressDoor` reads main's `IsUnlockedForTools`), against MapWorld `0b406078` (Relay v2 V0, 19:03; the same lines apply to `6bbc3341` and `5c57cfa5`), DoorShot `ef941ca9` and ModuleEditing `ed81b895`. 197 lines added, 9 changed (NewKey's signature gains `out bool dressed`; a dressed root drops the cube's 6 cm lift in `SpawnKey` and `SpawnFloorKey`). Applier `data/apply_map_hunks_v5.py.txt`.
- **The EXIT door is not dressed by D1.6.** It already has the map's own leaf rig, crossbar and LIT sign (C22/C23), and the stairwell production dresses it on `ExitDoorBuilt`. M1 skips it, and `DressDoor` refuses it too (`map.IsExitDoor`). The stairwell's own doors are not map doors. A19 ("EXIT and stairwell too") is therefore the stairwell production's to carry out with these DA kits (Q-EX still open).
- The rig needs nothing else from the map: no change to `SwingAwayFrom`, `FaceOf`, `doorSides` or the step-aside.

## 3. Seams: the measured see-through gaps

### 3.1 Geometry (Blender ray test, T1-DA)

Blender BVH rays (`data/T1_T2_da_checks.json`), both faces, 0.5° grid, through each gap. "Brush on" = the black brush seal and fins on the centre plane; "off" = the same frame without them.

| Pair (frame + leaf) | Brush | Hinge | Latch | Head | Floor |
|---|---|---|---|---|---|
| DoorFrame_Wood_DA + DoorLeaf_Veneer_DA | on | 0 rays (closed) | 0 rays (closed) | 0 rays (closed) | 9039 of 726975 rays; widest 3.00 mm; within ±1.5° of its plane |
| DoorFrame_Wood_DA + DoorLeaf_Veneer_DA | off | 0 rays (closed) | 5213 of 726975 rays; widest 3.00 mm; within ±1.0° of its plane | 5109 of 726975 rays; widest 2.95 mm; within ±1.0° of its plane | 9039 of 726975 rays; widest 3.00 mm; within ±1.5° of its plane |
| DoorFrame_Steel_DA + DoorLeaf_Steel_DA | on | 0 rays (closed) | 0 rays (closed) | 0 rays (closed) | 9039 of 726975 rays; widest 3.00 mm; within ±1.5° of its plane |
| DoorFrame_Steel_DA + DoorLeaf_Steel_DA | off | 0 rays (closed) | 5213 of 726975 rays; widest 3.00 mm; within ±1.0° of its plane | 5109 of 726975 rays; widest 2.95 mm; within ±1.0° of its plane | 9039 of 726975 rays; widest 3.00 mm; within ±1.5° of its plane |

Reading: **with the brush there is no straight sight line through the latch, head or hinge seam.** Without it, the latch and head seams are straight 3 mm slits, open only within ±1° of their own plane (the design predicted ±3.9°; the measured window is narrower because the fins' slots and the lining edges cut it). The hinge seam never opens (the leaf runs into the wall end). The floor gap is the 3 mm under the leaf above the 12 mm saddle, open within ±1.5° of the floor plane both with and without the brush: an eye at 1.62 m would have to be more than 60 m away along the floor to look through it.

### 3.2 In the engine (lit pixels per gap)

In-engine, seed 516574485, Lobby L0-F and Office OF-K: the camera's room dark (its lights off, no ambient), the far room lit; camera on the face normal at 1.0 and 2.5 m, eye 1.62 m, FOV 76. Pixels in a band around each seam brighter than the face's own 90th percentile + 0.10 count as "lit" (light from the far room). `data/a12_capture.json`. Captured 16:59–17:00 UNDER LOAD (1-min load 100–143).

| Door | Face | Distance | Brush | Hinge | Latch | Head | Floor | Face p90 / threshold |
|---|---|---|---|---|---|---|---|---|
| lobby L0-F | S | 1.0 m | on | 0 / 2171 px | 0 / 2170 px | 0 / 2425 px | 0 / 940 px | 0.123 / 0.223 |
| lobby L0-F | S | 1.0 m | off | 0 / 2171 px | 0 / 2170 px | 0 / 2425 px | 0 / 940 px | 0.123 / 0.223 |
| lobby L0-F | S | 2.5 m | on | 0 / 1273 px | 0 / 1274 px | 0 / 680 px | 0 / 570 px | 0.102 / 0.202 |
| lobby L0-F | S | 2.5 m | off | 0 / 1273 px | 0 / 1274 px | 0 / 680 px | 0 / 570 px | 0.102 / 0.202 |
| lobby L0-F | P | 1.0 m | on | 0 / 2171 px | 0 / 2170 px | 0 / 2430 px | 0 / 940 px | 0.116 / 0.216 |
| lobby L0-F | P | 1.0 m | off | 0 / 2171 px | 0 / 2170 px | 0 / 2430 px | 0 / 940 px | 0.113 / 0.213 |
| lobby L0-F | P | 2.5 m | on | 0 / 1275 px | 0 / 1272 px | 0 / 680 px | 0 / 570 px | 0.132 / 0.232 |
| lobby L0-F | P | 2.5 m | off | 0 / 1275 px | 0 / 1272 px | 0 / 680 px | 0 / 570 px | 0.132 / 0.232 |
| office OF-K | S | 1.0 m | on | 0 / 2176 px | 0 / 2173 px | 0 / 2420 px | 0 / 940 px | 0.069 / 0.169 |
| office OF-K | S | 1.0 m | off | 0 / 2176 px | 0 / 2173 px | 0 / 2420 px | 0 / 940 px | 0.069 / 0.169 |
| office OF-K | S | 2.5 m | on | 0 / 1273 px | 0 / 1275 px | 0 / 680 px | 0 / 570 px | 0.081 / 0.181 |
| office OF-K | S | 2.5 m | off | 0 / 1273 px | 0 / 1275 px | 0 / 680 px | 0 / 570 px | 0.081 / 0.181 |
| office OF-K | P | 1.0 m | on | 0 / 2171 px | 0 / 2170 px | 0 / 2420 px | 0 / 940 px | 0.048 / 0.148 |
| office OF-K | P | 1.0 m | off | 0 / 2171 px | 0 / 2170 px | 0 / 2420 px | 0 / 940 px | 0.050 / 0.150 |
| office OF-K | P | 2.5 m | on | 0 / 1273 px | 0 / 1273 px | 0 / 680 px | 0 / 570 px | 0.073 / 0.173 |
| office OF-K | P | 2.5 m | off | 0 / 1273 px | 0 / 1273 px | 0 / 680 px | 0 / 570 px | 0.073 / 0.173 |

Reading: **0 lit pixels in every band, both faces, at 1.0 and 2.5 m, with and without the brush.** From a standing eye on the face normal the 3 mm seams are seen 11°–27° off their own plane (latch and head; the floor seam 33°–58°), well outside their ±1° window, so the brush makes no visible difference at these points; it closes the narrow window the ray test finds. Red's 2026-10 R4 rule (no see-through slits) holds in both cases; the brush stays in the kit because it is what a real double-acting door has and it closes the last window.

### 3.3 The hinge junction at the rest angle (T2-DA)

Option B keeps the pivot on the wall centre plane (A12: no A2 offset), so near the pivot the leaf passes into the hinge-side jamb as it opens. The ray test swings each pair 0–95° both ways in 0.5° steps against the frame parts, a stand-in wall and a stand-in of the map trims:

- The latch end clears the latch lining at every angle, both ways (r 0.995 < 0.998), and nothing touches the brush seals except by design.
- **From 24.5° on, the leaf runs into the hinge-side reveal (rigid frame part)**, the same both ways and for every pair. Depth into the reveal (analytic): 25° 0.2 mm, 30° 1.6 mm, 45° 8.1 mm, 60° 22.5 mm, 76° 54.5 mm. Beyond 76° the leaf also tucks into the hinge-side casing: 80° 5.8 mm, 90° 24.0 mm, 95° 33.3 mm.
- This is the junction `04_door_re8_gap.md` §5.2 called "unchanged from today": the map door has always turned about the centre plane. At the 95° rest the overlapping part is behind the leaf from the room the leaf opened into; from the doorway on the other side it shows as the leaf face running into the jamb face, with no gap. The engine frames `a12_*_open95_hinge_*` (game lights) are too dark to show it either way.
- Fix ladder if Verify or Red flags it (10_design §2.3): (1) a concave cove on the hinge-side casing reveal, both faces, without exposing the map trims; (2) ask the map for a 90° rest in DA. Not built yet.

## 4. Period options for "away from the player" (facts, no choice)

| Option | Period use (1990) | Lock | Gaps | What it costs us |
|---|---|---|---|---|
| **A. Double-acting, centre-hung pivots (built)** | Common in hospital corridors, kitchens, service and back-of-house doors; pivots show only as caps at the head and floor | A real one rarely has a latching lockset: it is held by a deadbolt into the head or floor, or not locked at all. Our knob + deadbolt on both faces is a game liberty (Red, A12) | 3 mm at latch and head, closed by brush fins; the hinge stile is round about the pivot | The 9 `_DA` kits and 6 DA lock parts (done); the leaf tucks into the hinge-side casing beyond about 76 degrees (§3.3) |
| A′. Double-acting on spring (or gravity) pivots | The same doors with a self-closing pivot or a double-acting spring hinge | As A | As A | Self-centring: Red's A12 rule keeps the leaf where it is left, so not built |
| B. Mixed: single-acting doors with stops, handed per edge | Ordinary office doors | Normal latch sets | Stops close the gaps fully | A swing side fixed per edge cannot always open away from the player: A12 rejects it |
| C. Code only (today's kit, swing either way) | — | — | The stop door shows its stop gap and clips through its stops on the wrong side | No asset work; visibly wrong |

Push plates instead of handles are the honest hardware for A; Red kept handles (A12), and the Run ward door takes the lever set too (Q-RN default).

## 5. What the kit lacked before D1.6, and what is still missing

- Built now: stopless frames (3), DA leaves (6), pivot caps and plates, brush seals and fins, double-bevel latches (3), symmetric strikes (3).
- Still missing: push plates for honest ward doors (not wanted while Q-RN stands); an EXIT crossbar that fits a keyed DA leaf (Q-EX: today EX-K takes the knob set and no crossbar); a hinge-side casing cove for the 95 degree rest (§3.3); spring pivots (not wanted).

## 6. Sources

- Design: `d16/10_design.md` (§2 kit, §3 dress, §4 rig). Build record: `d16/20_build.md`.
- Ray tests: `d16/data/T1_T2_da_checks.json` (script `data/da_checks.py.txt`). Capture: `d16/data/a12_capture.json`.
- Images: `d16/images/a12_*` (Shot format): the sheet `a12_sheet.jpg` (VL317), the seams `a12_gap_sheet.jpg` (VL318), the junction `a12_junction95_sheet.jpg` (VL319), and every single frame.
- The map contract the rig needs: `d16/data/contract_map_D16.diff` (M1, M3, M4, M7, M9), applier `data/apply_map_hunks_v2.py.txt`.
