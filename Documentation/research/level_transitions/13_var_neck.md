# Level transitions · 13 · V3 Neck: pre-render report

Date: 2026-10-03. Variation key `neck` (plan `10_transition_plan.md` §3 V3, §7.3).
Built and rendered in the private clone `SCR/proj_trans_neck` only (SCR = the session scratchpad, full path in §16). Nothing in Red's project changed except the files in §13. No downloads. No Figma writes.

Work history. Images were made at 17:11–17:50. A usage limit stopped the workflow at 17:54, before the reports and the last stats run. I resumed at 22:00. I re-checked every output (§3.6). I re-ran the stats with one new census (flat seams). I rendered the two extra shots on the BEFORE base for a 6-row comparison. Then I wrote this report. No geometry changed after 17:42.

## 0. Short answer (for Red)

- **The idea reads as built.** Every frameless crossing (open edge or arch) between Office and Level 0 is now a short deep doorway: 0.60 m deep, 1.80 m wide (or the arch's own width), 2.20 m high. Its whole inside is Office paint. The yellow paper stops at the outer bead. The Office carpet starts at the Level 0 mouth, behind an aluminium bar. The soffit hides the ceiling change.
- **The cut is gone, not hidden.** No flat paper seam is left anywhere. Flat seams fall from 0.61 per chunk to 0 (1,620 chunks, §3.3). The finish changes only on built corners: the mouth bead, the bar, the wall angle.
- **Doors get a lined recess.** A border door now stands at the back of a 1.60 × 2.40 × 0.30 m recess. The 0.30 m thickening runs over the whole 3 m edge. The 2.4 m Low ceiling height becomes a built soffit (shot3).
- **Rows of openings become an arcade.** Where several border openings sit on one line, the necks share cheeks (extra shot 2, `v_neck_shot6.jpg`).
- **It changes gameplay.** Open-edge crossings narrow from 2.84 m to 1.80 m (−37 %). Each neck takes 0.22 m from both cells. The Relay (0.60 m wide, 2.05 m tall) fits every neck: the narrowest is 1.10 × 2.20 m. One exception: 96 plain arches next to a neck lose part of a jamb, down to 1.07 m (§9).
- **Cost (measured, within 46 m of the eye):** +14.5 k to +16.7 k triangles (+8.9 to +9.7 %); +12 to +32 draws (+0.6 to +1.6 %); +1 material; 0 new lights; 0 new texture files. Per chunk: 1.26 necks and 1.37 recesses, 729 triangles and 18.7 collision boxes.
- **Collision changes.** Cheeks, headers and recess walls go into the chunk's own collision mesh: 0 new colliders. The map chat must rerun nav 60/60 and the interaction and designer tests.
- **Main has moved.** Codex promoted the Light lead kit, B0 and the V5 colour lead into Red's project at 19:10–19:41. Neck was **not** promoted. If Red picks Neck, it must be merged over main (§12).
- **My read:** this is the strongest fix of the joint itself, and it reads as architecture, like Kane FF#3 at 34:21. The price is space and map-chat work. If Red likes it, use it on arches and open edges first. Keep the door recess optional: it is subtle, and with single-acting doors it lands on the Level 0 side half the time.

## 1. Images

All 1920 px wide, JPG q85, in `images/`.

| File | What |
|---|---|
| `var_neck_shot1.jpg` .. `shot4.jpg` | The four fixed shots, harness output unchanged (same cameras as `shot<k>_before.jpg`). |
| `v_neck_shot1.jpg` .. `shot4.jpg` | Byte-identical copies of `var_neck_shot1..4` (the workflow asked for both names). |
| `v_neck_shot5.jpg` | Extra 1: the Office side of the shot1 arch neck (Office face, cove base, wall angle). |
| `v_neck_shot6.jpg` | Extra 2: the X = 765 run beside shot2. Three necks share cheeks and read as an arcade. |
| `var_neck_sheet.jpg` | Four rows, BEFORE left, NECK right. |
| `v_neck_vs_before.jpg` | Six rows, BEFORE left, NECK right: the four fixed shots plus the two extras. The extras' BEFORE frames were rendered on a fresh copy of the frozen base (`proj_trans_neck_bx`). |
| `var_neck_close_mouth.jpg` | Close (extra 3): the Level 0 mouth of the Z = 588 neck. Bead, bar, Office carpet, cove end. |
| `var_neck_close_seam.jpg` | Close (evidence `seam_17_A`, same pose as `cut_seam_1`): the flat seam is buried in a neck. |
| `var_neck_close_post.jpg` | Close (evidence `post_20_B`, pose of `cut_post_2`): B0.1 post, and two recessed border doors. |
| `var_neck_close_wallend.jpg` | Close (evidence `postsplit_22_Z`, same pose as `cut_wallend_1`): the split wall end is now a neck cheek in one finish. |
| `var_neck_close_door.jpg` | Close (evidence `door_09_B`, pose of `cut_door_2`): the open leaf in front of the recess. |

The evidence harness re-picks close cameras from visibility, so two close frames moved a little (`post_20_B`, `door_09_B`). The fixed shots did not move.

### What each shot had to show

| Shot | Brief | Result |
|---|---|---|
| shot1 | Z = 609.0 arch as a 0.6 m Office-lined throat; paper stops at the outer bead; Office carpet from the Level 0 mouth with a bar; soffit hides the ceiling change. X = 795.0 arch also a neck, cheek 0.70 m right of the eye. | Yes. The arch opening is its hashed 1.13 m (1.24–2.37 along the edge). The X = 795 neck's cheek face is 0.700 m from the eye and fills the right edge of the frame, as expected. Its reveal, soffit, cove base and bar are all visible. |
| shot2 | A 1.8 × 2.2 m deep opening across the corridor on Z = 588.0, with both corridor walls dying into the cheeks. On X = 765.0 at left, the open edge in row 194 and the arch in row 195 are necks too, 1.45 m away. | Yes. The corridor's flat seams are gone: both walls end in the cheeks. At left, the row-194 neck's reveal (Office paint) and the row-195 arch neck are visible. Nearest cheek 1.450 m. |
| shot3 | The door at the back of a 0.30 m Office-lined recess with a 2.4 m soffit. | Yes. 1.60 × 2.40 × 0.30 m recess. The frame stays on the wall line. Cove base wraps the thickened face and the recess. |
| shot4 | The far arch neck and the X = 798.0 neck at right. | Yes. The far arch (7.5 m) shows its throat. The X = 798 neck at right shows the deep reveal and the bar. The flat seam that was in the middle of the right wall is gone. |

## 2. What was built

All geometry comes from one visual-owned kit, `Assets/Scripts/Rendering/Transitions/FrontRoomsTransitionKit.cs` (clone). The map calls it from small hooks (§4). The kit owns no GameObjects, renderers or MaterialPropertyBlocks. It returns triangles per material and height block, plus collision boxes. The map appends them to the chunk's own builders and its collision mesh.

**Neck** (every Open or Arch edge whose two cells differ in theme; 1.26 per chunk):
- A block 0.60 m deep, centred on the cell line (0.30 m each side, 0.22 m beyond today's 0.08 m skins). It spans the full 3 m edge.
- One opening. Open edge: 1.80 m (`ArchMaxWidth`), centred. Arch: its hashed width and centre (`ArchOpening`). Top: 2.20 m (`ArchTop`, or ceiling − 0.20 if lower).
- Faces: Level 0 side `L0_Wallpaper`; Office side `Office_Wall`; both cheeks and the soffit of the 0.60 m reveal `Office_Wall`.
- 6 mm round beads (4 segments) on the four mouth arrises, in Office paint. The paper stops at the bead.
- Exposed outer corners at the block's ends get 6 mm corner beads. End faces take the paper of the cell they face.
- Runs: consecutive border openings on one line share cheeks (end extension 0). At an L of two necks, the one whose opening sits further from the corner owns it (+0.30 m). The other stops at its face (−0.30 m).
- Corner rule: a neck's opening keeps at least 0.36 m from a corner where a perpendicular neck stands. If that would make an arch narrower than 1.10 m, the opening grows away from the corner. 58 opening ends were moved over 1,620 chunks.
- Corridor walls that meet a neck stop at its face (−0.30 m), so their paper change is buried in the cheek.
- Collision: two cheek boxes and one header box, full height, in the chunk's collision mesh.

**Floor at a neck:**
- `Office_Carpet` overlay, 0.30 m deep × the opening width, 1 mm above the Level 0 slab, from the Level 0 mouth to the line.
- Aluminium binder bar (`Prop_Aluminium`), 35 × 5 mm, crowned profile (9 points), on the mouth line.
- Rubber cove base (`Cove_Base`), 0.10 m high, 3.2 mm thick, with a 12.5 mm toe and a quarter-ellipse cove (real profile, 13 points). It runs along the Office face, round the mouth corner and through both cheeks to the bar. None on the Level 0 face.

**Ceiling at a neck:** each ceiling grid dies into the neck face with a 22 mm white steel wall angle (`Painted_Metal`, 0.8 mm leg). The 2.20 m soffit hides the change of grid and height.

**Door recess** (every Door edge whose two cells differ in theme; 1.37 per chunk):
- The wall is thickened by 0.30 m on the stop side over the full 3 m. Clone rule: the Office side (see §7, item 5).
- A recess 1.60 m wide × 2.40 m high × 0.30 m deep, centred on the door. The door and its frame stay on the wall line at the back.
- Lining `Office_Wall`, beads on the recess arrises and the block's outer corners, cove base round the face and the recess, wall angle at the ceiling.
- Soffit at 2.40 m (the Low ceiling height), so the height step becomes a built soffit.
- Collision: the thickening, the two recess jambs and the recess head.
- Leaf check: the leaf swept 0–95° both ways in 5° steps. Least clearance to any recess solid: 0.259 m (at −95°).

**B0** (in every variation):
- **B0.1 corner posts.** Where wall faces of different finish meet at a cell corner, every piece stops at the post face. The kit builds a 0.16 m post whose exposed faces take the finish of the cell they face. Finish = theme + ceiling class. No two coplanar faces overlap, so there is no z-fight. 10.76 posts per chunk, 2 triangles each (only exposed faces are built). This differs from the letter of the brief (§7, item 1).
- **B0.2 grime band per side.** Each skin of a height-border wall goes in the height block of the cell it faces, so `_CeilingHeight` is its own ceiling. Same-material walls still get two skins when the blocks differ.

**Not built:**
- Pilaster stubs for flat seams away from a neck. The census found none (§3.3).
- Windows: unchanged (the climb path must not move).
- Lamps: unchanged. No new lights.
- No Blender modules. Every trim is a procedural C# profile swept along its path. The profiles are real (bead, cove toe, crowned bar, angle flange) and hold up at 0.3 m (`var_neck_close_mouth.jpg`).

### Iterations

| Round | Time | What changed |
|---|---|---|
| basecheck | 16:44 | Clone before any edit: all four frames byte-identical to `shot<k>_before.jpg`. |
| r1 | 17:11 | First full set. Reviewed with crops of the shot1 mouth, the shot1 right neck, the shot2 mouth and the shot3 recess. |
| r2 | 17:17 | Cove end at the thickened block's end in shot3 (24 px). Extras and evidence round 1. |
| r3 | 17:39 | 6 mm corner bead on the outer corners of the recess thickening (shot3, 486 px). A 1 px sliver at a neck corner in shot4. Stats rounds 1–3. |
| final | 17:44 | Byte-identical to r3. Extras and evidence final round. |
| resume | 22:04–22:14 | No geometry change. Stats rerun with the flat-seam census. Extras' BEFORE frames rendered on a copy of the frozen base. |

## 3. Measurements

### 3.1 Lamp lines (harness `.txt`)

| Shot | Before | Neck |
|---|---|---|
| shot1 | lamps 1598, lit 84, shadowed 8 | lamps 1598, lit 84, shadowed 8 |
| shot2 | lamps 1597, lit 82, shadowed 7 | lamps 1597, lit 82, shadowed 7 |
| shot3 | lamps 1597, lit 83, shadowed 7 | lamps 1597, lit 83, shadowed 7 |
| shot4 | lamps 1598, lit 88, shadowed 7 | lamps 1598, lit 88, shadowed 7 |

Identical. The neck adds no lights and no lamp changes.

### 3.2 Within 46 m of each eye (my stats method, not the harness)

Method: `FrontRoomsNeckStats.RunBatch` (clone, `Assets/Editor/Transitions/`). For each pose it builds the map twice the way the harness does (`BuildForCapture`, rooms furnished): kit off (BEFORE), then kit on (NECK). It counts every enabled renderer whose bounds lie within 46 m of the eye: renderers, submesh draws, unique materials, triangles, and the runtime size of the textures those materials use. Lights count when enabled, intensity > 0 and within 46 m.

| Shot | Build | Renderers | Submesh draws | Materials | Triangles | Lit lights | Textures | Texture MB |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| shot1 | before | 1801 | 2083 | 42 | 175,766 | 84 | 93 | 637.4 |
| shot1 | neck | 1813 | 2095 | 43 | 192,424 | 84 | 96 | 645.4 |
| shot1 | **delta** | **+12** | **+12** | **+1** | **+16,658 (+9.5 %)** | **0** | **+3** | **+8.0** |
| shot2 | before | 1779 | 2036 | 42 | 162,760 | 82 | 93 | 637.4 |
| shot2 | neck | 1806 | 2063 | 43 | 177,302 | 82 | 96 | 645.4 |
| shot2 | **delta** | **+27** | **+27** | **+1** | **+14,542 (+8.9 %)** | **0** | **+3** | **+8.0** |
| shot3 | before | 1756 | 2038 | 42 | 178,600 | 83 | 93 | 637.4 |
| shot3 | neck | 1788 | 2070 | 43 | 194,926 | 83 | 96 | 645.4 |
| shot3 | **delta** | **+32** | **+32** | **+1** | **+16,326 (+9.1 %)** | **0** | **+3** | **+8.0** |
| shot4 | before | 1791 | 2064 | 42 | 172,554 | 88 | 93 | 637.4 |
| shot4 | neck | 1806 | 2079 | 43 | 189,240 | 88 | 96 | 645.4 |
| shot4 | **delta** | **+15** | **+15** | **+1** | **+16,686 (+9.7 %)** | **0** | **+3** | **+8.0** |
| extra1 | delta | +13 | +13 | +1 | +16,692 | 0 | +3 | +8.0 |
| extra2 | delta | +27 | +27 | +1 | +15,666 | 0 | +3 | +8.0 |
| spawn 2554 | delta | +15 | +15 | +1 | +23,506 (+8.4 %) | 0 | +3 | +8.0 |
| spawn 20388 | delta | +2 | +2 | +1 | +27,252 (+8.1 %) | 0 | +3 | +8.0 |

Renderer changes by material (shot3, the largest): `Painted_Metal` +12, `Cove_Base` +10, `Prop_Aluminium` +9, `L0_Wallpaper` +4, `Office_Carpet` +1, `Office_Wall` −4. The new renderers are the trim blocks (cove, bar, wall angle) and B0.2's per-side blocks.

The one new material in range is `Painted_Metal` (the wall angle). Its three textures (`PaintedMetal_A`, `_N`, `_S`, 2.7 MB each) explain the +8.0 MB. They are not new files: the title corridor's troffer pans (`FrontRoomsRoomStream`) already use `Painted_Metal`. The resident memory delta is 0 MB while that material is loaded, and 8.0 MB otherwise. Using a material the map already loads for the angle would make it 0 MB in every case (§14, item 6).

Chunk build time (editor batch, warm, 25 chunks): 138–151 ms before, 222–258 ms with necks. About +3 to 4 ms per chunk. The Mac was busy with other Unity jobs, so treat this as a rough figure.

### 3.3 The kit per chunk (20 seeds × 81 chunks = 1,620 chunks, generator only)

| Per 24 m chunk | Mean | Chunks with ≥ 1 | Max |
|---|---:|---:|---:|
| Necks (all) | 1.259 | 32.0 % | 14 |
| · from open edges | 0.890 | 28.3 % | 12 |
| · from arches | 0.369 | 21.0 % | 7 |
| Door recesses | 1.370 | 35.9 % | 15 |
| B0.1 posts | 10.763 | 95.2 % | 29 |
| Kit triangles | 729 | | 4,680 |
| Kit collision boxes | 18.65 | | 70 |

- Triangles per neck: 280 (shell, beads, cove, bar, carpet, wall angles). Per recess: 256. Per post: 2. The plan estimated about 400 per neck.
- Triangles by part, per chunk: recess cove 282.9, neck cove 191.4, neck beads 67.3, recess beads 39.5, neck bar 37.8, neck shell 33.7, post 24.7, neck wall angle 20.4, recess shell 19.7, recess wall angle 8.8, neck carpet 2.5. The cove base is 65 % of the kit's triangles.
- Neck openings: 2,040 necks. Width min 1.10, mean 1.69, max 1.80 m. Top 2.20 m in every case (min(2.20, ceiling − 0.20), ceilings ≥ 2.4 m).
- **Flat seams (cut C)**: before 0.614 per chunk (994; the inventory says 0.61). With necks: 0.778 buried in a neck cheek, 2.388 in an inside corner, **0 left flat**. The pilaster-stub case never happens.

In the 25 chunks round each shot: 36–40 necks (26–30 open, 10 arch), 39–45 recesses, 243–257 posts, 21.4–21.8 k kit triangles, 486–494 collision boxes.

### 3.4 Narrowing of each crossing

| Crossing | Before | Neck |
|---|---|---|
| Open edge | 2.84 m clear (3 m − 0.16 m wall) | 1.80 m × 2.20 m (−1.04 m, −37 %) |
| Arch | hashed 1.10–1.80 m × 2.20 m | same width and centre; 58 opening ends over 1,620 chunks were moved to keep 0.36 m from a perpendicular neck (width kept ≥ 1.10 m) |
| Plain arch next to a neck | hashed width | 96 arches over 1,620 chunks (0.06 per chunk) lose part of a jamb to a neck face; narrowest 1.07 m |
| Door | 1.00 × 2.10 m | unchanged; it sits 0.30 m deep in a 1.60 × 2.40 m recess |
| Cells beside a neck | 3 m | 0.22 m less on each side, over 3 m |

### 3.5 Checks

| Check | Result |
|---|---|
| Baseline (clone before any edit) | 4/4 frames byte-identical to `shot<k>_before.jpg` (16:44 run) |
| Harness unchanged | `FrontRoomsTransitionAudit.cs` md5 `76a624e3217630e637bdb381af7ca5b1`; `shots.json` unchanged |
| Layout freeze | 4/4 `<id>_plan.json` identical to `logs/shot<k>_plan.json` |
| Nearest new solid to the eye (near plane 0.06 m) | shot1 0.700 m (X = 795 cheek), shot2 1.450 m, shot3 2.384 m (recess jamb), shot4 4.434 m, extras 1.160–1.833 m, spawns 7.26–7.30 m. All clear. |
| Relay body (r 0.30, h 2.05) fits every neck (≥ 1.1 × 2.2) | Yes: narrowest neck 1.10 × 2.20 m. Exception: 96 plain arches next to a neck, narrowest 1.07 m (still wider than the 0.60 m body, but under the 1.1 m rule). |
| Furnished props cutting a cheek, header or recess | None in 9 builds of 25 chunks (225 chunk builds, rooms furnished). |
| Door leaf 0–95° both ways | Least clearance 0.259 m to a recess solid. Clear. |
| Log errors | 0 exceptions and 0 compile errors in the shot, extra, evidence, stats and extras-before logs |

### 3.6 Re-checks after the interruption

- `var_neck_shot1..4.jpg` = `v_neck_shot1..4.jpg` = the final harness output in `SCR/shots_neck` (cmp, byte-identical).
- `v_neck_shot5/6.jpg` = `SCR/shots_neck_x/extra1/2_neck.jpg`; `var_neck_close_mouth.jpg` = `extra3_neck.jpg`.
- Kit source last changed 17:42; final renders 17:44. No geometry change since.
- The 17:52 stats run crashed on the spawn builds (a parse error in the tool, not the kit). The 22:04 rerun completed (§3.2–3.4).
- Two duplicate files from the first attempt (`var_neck_extra1/2.jpg`, byte-identical to `v_neck_shot5/6.jpg`) were outside the output list. I moved them to `SCR/neck_work/moved_from_doc/`.

## 4. Draft contract (map-file diff against the clone base)

Only `FrontRoomsMapWorld.cs` changed among the map files: +140 / −12 lines, 11 lines marked `// TRANSITION neck`. The full diff is in the appendix.

| Hook | Where | What |
|---|---|---|
| H1 | `Builder.Append` | Appends the kit's chunk-local triangles to a builder, with the same world-planar UVs. |
| H2 | `BuildInto`, before the cell loop | Makes one `FrontRoomsTransitionKit.Chunk` per chunk through a `TransitionView`, and a `FacedBlock` function (block of the faced cell, clamped into the chunk). |
| H3 | `BuildInto`, the two `BuildEdge` calls | B0.2: passes the per-side blocks (`SideBlock`) and the kit to `BuildEdge`. |
| H4 | `BuildInto`, after the edges | B0.1: `trans.Post(...)` at the cell's north-east corner. |
| H5 | `BuildInto`, after the cell loop | Drains the kit's buffers into the chunk builders by material, and its boxes into the chunk collision mesh. |
| H6 | `BuildEdge` start | A border Open or Arch edge goes to `trans.Neck(...)` and nothing else is built on it. |
| H7 | `BuildEdge.Piece` | B0.1 and neck corners: each piece takes the kit's start and end offsets (+ runs past the corner, − stops at a post or neck face). Two skins whenever finish or height block differ. |
| H8 | `BuildEdge`, door branch | A border door calls `trans.Recess(...)` on the Office side. |
| H9 | new `TransitionView` | The map as the kit sees it: edge kind (start area included), themes, ceilings, arch, door and window openings. |
| H10 | new `TransitionMaterial` | Kit material ids → `level0.wall`, `office.wall`, `office.floor`, `trim` (`Cove_Base`), `Prop_Aluminium`, `Painted_Metal`. |

**What the contract asks the map chat for:**
1. Neck and recess geometry with collision in `BuildEdge` (H6–H8), and the kit hooks (H1–H5, H9, H10).
2. New `ModuleUnits` constants: `NeckDepth` 0.60, `NeckOpening` 1.80, `RecessDepth` 0.30, `RecessWidth` 1.60, `RecessTop` 2.40. (Also `NeckCornerClear` 0.36, used by the corner rule.)
3. Dress API keep-clear measured from the neck face: `EntryClearDepth` 1.00 m becomes 1.22 m from the cell line at a neck (+0.22 m).
4. Arch hash near necks: `ArchCornerMargin` is 0.20 m. At a corner with a border neck it should be at least 0.36 m, so no plain arch loses jamb width (the 96 cases in §3.4).
5. Recess side from the door's swing: the stop side is opposite `FixedSwing` in main (§12).
6. Reruns: nav 60/60, the 66 interaction tests and the designer tests.

## 5. New assets and files (clone only)

| File | Size | Kind |
|---|---|---|
| `Assets/Scripts/Rendering/Transitions/FrontRoomsTransitionKit.cs` (+ `.meta`, GUID `191115fdf79744816a94003496b9bbe9`) | 46.6 KB, 875 lines | Runtime kit (visual-owned) |
| `Assets/Editor/Transitions/FrontRoomsNeckStats.cs` (+ `.meta`) | 30.2 KB, 494 lines | Tools only: stats, census, leaf sweep |
| `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs` | +140 / −12 lines | Map hooks (draft contract) |

No new materials, textures, meshes, Blender modules or FBX. The kit uses existing materials: `L0_Wallpaper`, `Office_Wall`, `Office_Carpet`, `Cove_Base`, `Prop_Aluminium`, `Painted_Metal`. Texture memory added: 0 MB of new files (see §3.2 for the in-range +8.0 MB).

## 6. Change list (every clone change, and why)

1. `FrontRoomsMapWorld.cs`, `Builder.Append`: the kit returns raw triangles; the map's builders own UVs and blocks.
2. `FrontRoomsMapWorld.cs`, `BuildInto`: one kit per chunk; `FacedBlock` so each face sits under its own ceiling (B0.2) and the kit can pick blocks.
3. `FrontRoomsMapWorld.cs`, `BuildEdge` signature (+4 optional parameters: `trans`, `blockAt`, `blockA`, `blockB`): border Open and Arch edges become necks; pieces take corner offsets (B0.1, neck corners); two skins when finish or block differ (B0.2).
4. `FrontRoomsMapWorld.cs`, door branch of `BuildEdge`: border doors get the recess.
5. `FrontRoomsMapWorld.cs`, `TransitionView` and `TransitionMaterial`: the kit needs edges, themes, ceilings and arch openings without touching the cache directly; materials stay the map's.
6. New `FrontRoomsTransitionKit.cs`: all neck, recess, post and trim geometry; the corner table; proposed constants.
7. New `FrontRoomsNeckStats.cs`: the measurements in §3 (tools only). At 22:00 I added the flat-seam census and nothing else.
8. Not changed: the harness, `shots.json`, zones, maze, rooms, modules, columns, edge kinds, door positions, windows, lamps, materials, textures, shaders.

## 7. Deviations from the brief

1. **B0.1 is built as an explicit post.** The brief says collinear pieces keep their 0.08 m extensions and, at an L, the Office-outside piece wraps the post. The clone instead stops every piece at the post face and builds a 0.16 m post whose exposed faces take the faced cell's finish. Same result: no overlap, no z-fight. Fewer cases. Main now holds Light lead's B0.1 instead (pieces reach over the post by rule). See §12.
2. **B0.2 picks the faced cell's block**, clamped into the chunk, rather than "this cell's block with that side's ceiling". Same intent: the grime band follows each face's own ceiling.
3. **No pilaster stubs.** The census found 0 flat seams left once necks exist, so there was nothing to put a stub on.
4. **Corner rule added.** The brief did not say what happens where two necks meet at an L, or where a neck meets an arch near a corner. The clone keeps openings 0.36 m from such a corner (58 ends moved), lets one neck own the corner, and lets plain arches lose jamb width (96 cases, to 1.07 m). Map-chat decision (§4, item 4).
5. **Recess side.** The brief: the stop side is the side the leaf does not swing into. Clone doors are double-acting, so the clone puts every recess on the Office side (shot3's leaf swings into Level 0, which matches). In main, doors are single-acting with a hashed side; the real rule is "opposite `FixedSwing`" (§12).
6. **Wall angle material.** `Painted_Metal` is new to the map (+1 material, +8.0 MB textures in range, 0 MB new files).
7. **Necks take the taller ceiling** for their height (`H` = max of the two sides). Where one side is Low 2.4 m, the header runs up behind that ceiling. The opening top is min(2.20, H − 0.20), so it is 2.20 m in every case.
8. **CrossingPoint.** It stays the opening centre for open edges and unclamped arches. For the 58 clamped arch ends the opening centre moves by up to 0.08 m; `CrossingPoint` (still the hashed centre) stays inside the opening.

## 8. Era check (1990)

- Thick chase walls and short vestibules between tenant areas: standard in 1970s–80s office fit-outs (`02_real_buildings.md` Rule 4).
- Rubber cove base 4 in (0.10 m): in use from the 1950s.
- Aluminium carpet binder bars: in use by the 1960s.
- Metal corner bead (here 6 mm round): long before 1990.
- Steel wall angle for lay-in ceilings: standard with 2 × 2 and 2 × 4 grids by the 1970s.
- Carpet tiles: from 1973.
- No print, no lettering, no trademarks were added.

## 9. Gameplay notes (for 系统设计 and the map chat)

- Open-edge crossings narrow from 2.84 m to 1.80 m. Sightlines through borders get shorter and narrower.
- The Relay (0.60 m wide, 2.05 m tall) fits every neck. The narrowest neck is 1.10 × 2.20 m. 96 plain arches next to a neck drop to as little as 1.07 m. That is still passable but breaks the 1.1 m rule.
- Necks are 0.60 m long. A chase through a border passes a short tunnel, and the view into the next cell is framed by the mouth.
- Cells next to a neck lose 0.22 m on that side. Props placed at `EntryClearDepth` (1.00 m) from the line now sit 0.78 m from the neck face. None intersected a neck in the 225 chunk builds measured.
- Door recesses keep the leaf's full swing (0.259 m clearance at 95°). With single-acting doors, the recess sits on the push side, so the pull step (`DoorPulled`) is not affected.
- Collision mass rises by 18.65 boxes per chunk. They join the existing chunk mesh collider: no new Collider components.

## 10. Research basis

- **Kane Pixels, *Found Footage #3*, 34:18–34:28** (`01_ip_research.md` §2): a yellow room with a deep box recess. In it, a doorway with white jambs. The yellow stops at the outside corner, the base and carpet run through, the space beyond has its own light. The neck is this recess. Here the carpet change moves to the Level 0 mouth and the cove base starts inside the neck. (Clip not on disk; it is item 1 in `media_candidates.md`.)
- **Valve, "Level Transitions"** (Valve Developer Community, S34 in `01_ip_research.md`): a seamless transition is an area that exists on both sides, a door or a hallway. The neck is that shared area, scaled down to 0.60 m.
- **Backrooms Fandom, Level 4** (S-table in `01_ip_research.md`): the office level is entered through an "office sector" metal door, or through long dry corridors. The transition is a built passage.
- **Dsc00159** (`Research/week02/ip-research/stills/ir01_dsc00159_20020612_082017.jpg`, Wikimedia Commons, copyrighted free use, 2002): a wall of arched openings divides two areas. That is extra shot 2's arcade.
- **`02_real_buildings.md` Rule 4**: the threshold is thick. The wall depth is where the transition lives.

## 11. What the real implementation needs

**Map chat (large):**
- The hooks in §4 and the five constants in `ModuleUnits`, `LEVEL_MODULE_SPEC.md` updated.
- Neck and recess collision through the chunk collision mesh.
- The corner rule (one owner per L of necks; openings ≥ 0.36 m from such corners) and `ArchCornerMargin` ≥ 0.36 m at corners with a border neck.
- Recess side = opposite `FixedSwing`.
- Dress API keep-clear from the neck face (+0.22 m).
- Reruns: nav 60/60, interaction tests, designer tests.

**Visual chat:**
- The kit (neck, recess, posts, trims, profiles), merged into main's `FrontRoomsTransitionKit.cs` (§12).
- The wall angle material decision (keep `Painted_Metal`, or reuse a map material for 0 MB).
- A Play-mode check of draw calls and chunk build time with the real streaming.
- A look check on top of main's current lamps (main runs V5's cool Office colour by default; these frames used the old warm lamps).
- WebGL: same geometry; the kit adds about 1 renderer per trim block. If WebGL needs it, drop the cove's 13-point profile to 5 points there only (cove is 65 % of the kit's triangles).

**Others:**
- 系统设计: the narrower crossings and the Relay chase through necks.
- Wallpaper chat / 平面视觉: the Level 0 paper now ends at a bead on every neck face. EGRESS ink placement should avoid the 0.30 m end faces.
- Sound chat: a neck is a 0.60 m tunnel; footsteps and the Relay's sound could change inside it (optional).

## 12. Main changed (Codex, 2026-10-03 19:10–19:41): what it means for Neck

Read-only check of Red's repository (HEAD `75cfdff`; pre-Codex base `7320ed1`). I changed nothing in main.

- Main now has `Assets/Scripts/Rendering/Transitions/FrontRoomsTransitionKit.cs` (Codex commit `8ef5b64`). It is Light lead's clone kit, byte-identical (md5 `7a8c8af12d4cca733d6242a06f772d6e`), GUID `b215858f1c0014f87ae44abb9c680391`. The Neck clone's kit has the **same path and class name** with a different GUID (`191115fd…`). If Red picks Neck: keep main's GUID, and merge the neck code into main's kit (or a new file, for example `FrontRoomsTransitionNeck.cs`) instead of copying the clone file over it.
- Main's `FrontRoomsMapWorld.cs` holds Light lead's B0.1 (`CornerReach`, pieces reach over the post by rule), B0.2 (per-side blocks) and the V5 colour lead (cool Office lamps and lens on by default; the dead/dim border rule is opt-in). Red has not picked a variation yet. This is for the `codex-audit` workflow to judge; I did not revert it.
- `BuildEdge` signatures conflict: main added `blockA, blockB`; Neck adds `trans, blockAt, blockA, blockB`. A Neck merge should keep main's B0 for corners without a neck and add only the neck offsets (`NeckExt`, `PieceOffset` at neck corners). The clone's own posts would then be dropped, unless the audit rejects main's B0.
- Main has Red's single-acting doors (`FixedSwing`, a hash per edge, about 50/50). The recess must follow it, so about half the recesses would sit on the Level 0 side, lined in Office paint (the newer layer owns the joint). On a Low 2.4 m side the 2.40 m soffit meets the ceiling, so no soffit shows there.
- The BEFORE base for all variation frames is the frozen clone (`MapWorld` md5 `e5204f9c…`), by protocol. The frames compare variations fairly with each other, but they do not show main's current look. A final pick should be re-rendered over main.
- The codex audit (`research/codex_audit/`) had written `00_main_state.md` (22:20) and a map-chat note when I finished; `20_findings.md` did not exist yet, so no confirmed finding names this workflow. `00_main_state.md` §3.5 agrees with the above: N1 V5 + B0 were copied from `proj_trans_lightlead`, are on by default, and Red has not picked; after the pick, the visual files plus a MapWorld contract are owed.
- The map chat's note (`codex_audit/NOTE_map_chat_attribution.md`) says it has rebased its own copy onto Codex's MapWorld hunks (B0, V5) and is waiting for contracts. So the Neck contract in §4 should be re-expressed against main's `FrontRoomsMapWorld.cs`, not the frozen clone base, when Red picks it.

## 13. Outputs in Red's project

- `images/var_neck_shot1..4.jpg`, `images/var_neck_sheet.jpg`
- `images/v_neck_shot1..6.jpg`, `images/v_neck_vs_before.jpg` (rebuilt at 22:14 with 6 rows)
- `images/var_neck_close_mouth.jpg`, `_seam.jpg`, `_post.jpg`, `_wallend.jpg`, `_door.jpg`
- `13_var_neck.md` (this file), `20_variation_neck.md` (summary)

## 14. Open issues

1. **Space.** Every border cell loses 0.22 m on the neck side, and open crossings lose 37 % of their width. Red should walk it in Play before picking it.
2. **Plain arches next to a neck** (96 over 1,620 chunks) can drop to 1.07 m. Needs the `ArchCornerMargin` contract item.
3. **Door recess side** with single-acting doors (above). The Level 0-side recess was not rendered.
4. **The block reads as a 3 m pier** on the Office side: it stands 0.22 m proud over the whole edge (`v_neck_shot5.jpg`). That is honest construction (a chase wall), but a shorter block (opening + 2 × 0.30 m cheeks) is an option if Red finds it heavy.
5. **Cove base only at necks and recesses.** Office walls elsewhere have no base, so the base starts and stops at the neck. V1 Frame adds a base to all Office walls; the two could combine.
6. **Wall angle material**: `Painted_Metal` (+8.0 MB in range unless the title corridor keeps it loaded). Alternative: an existing map material.
7. **Draw calls** were measured as submesh draws in range, not as a GPU capture. A Play-mode capture is still needed.
8. **Main's look moved** (V5 colour lead on by default). Re-render over main before a final decision.

## 15. Verification log

The FRONTROOMS · VISUAL VERIFICATION LOG section is not on the Figma canvas yet (`VERIFICATION_LOG.md`: "section id: —"), and this brief forbids Figma writes. So the frames are listed here, with a ready row for the placing workflow (it claims the next VL number; I did not edit `VERIFICATION_LOG.md`, which is outside this workflow's output list).

| VL | Date | Task | Check | Verdict | Question (lede) | Statement | Notes | Images |
|---|---|---|---|---|---|---|---|---|
| (next) | 2026-10-03 17:44–22:14 | N1 | V3 Neck | WAIT-RED | Does a thick lined threshold remove the cut at Office borders? | Flat seams 0.61 → 0 per chunk; openings 2.84 → 1.80 m. | Neck 0.60 m deep, opening 1.80 (arch: own), top 2.20; recess 1.60 × 2.40 × 0.30. +8.9–9.7 % tris, +12–32 draws, 0 lights, 0 new texture files. Long verdict: PASS as briefed (WAIT-RED). Report `research/level_transitions/13_var_neck.md`. | `research/level_transitions/images/v_neck_vs_before.jpg`<br>`research/level_transitions/images/var_neck_sheet.jpg`<br>`research/level_transitions/images/v_neck_shot6.jpg`<br>`research/level_transitions/images/var_neck_close_mouth.jpg`<br>`research/level_transitions/images/v_neck_shot5.jpg`<br>`research/level_transitions/images/var_neck_close_seam.jpg` |

## 16. Logs and reproduction

SCR = `/private/tmp/claude-501/-Users-redwang-Desktop-ArtCenter-Fall26T7-EGAM-401A-01-Individual-Game-Project/5656cffd-bc90-45f6-86a3-09b26549df8d/scratchpad`

| What | Log | Output |
|---|---|---|
| Baseline (before any edit) | `SCR/neck_logs/basecheck.log` | `SCR/shots_neck_basecheck2/` |
| Rounds r1, r2, r3 | `SCR/neck_logs/r1.log`, `r2.log`, `r3.log` | `SCR/shots_neck_r1/`, `_r2/`, `_r3/` |
| Final four shots (`-plan`) | `SCR/neck_logs/shots_neck.log` | `SCR/shots_neck/` |
| Extras (neck) | `SCR/neck_logs/shots_neck_x.log` | `SCR/shots_neck_x/` (shots file `SCR/neck_work/extra_shots.json`) |
| Extras (before, on `SCR/proj_trans_neck_bx`, a fresh copy of the frozen base) | `SCR/neck_logs/extras_before.log` | `SCR/shots_neck_x_before/` |
| Evidence close frames | `SCR/neck_logs/evidence_final.log` | `SCR/evidence_neck_final/` |
| Stats (final) | `SCR/neck_logs/stats_r4.log` | `SCR/neck_stats_r4/neck_stats.md` (poses `SCR/neck_work/stats_shots.json`) |
| Map diff | — | `SCR/neck_work/mapworld_now.diff` |
| Source backups | — | `SCR/neck_work/FrontRoomsTransitionKit.cs.final`, `FrontRoomsNeckStats.cs.final` |
| Sheet script | — | `SCR/neck_work/r4/vs_sheet.py` |

Reproduce the four shots (graphics on, one Unity per clone):

```
/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath SCR/proj_trans_neck \
  -executeMethod FrontRoomsTransitionAudit.ShotsBatch -quit -logFile <log> \
  -shots "<DOC>/shots.json" -out <dir> -tag neck -plan
```

Stats: `-executeMethod FrontRoomsNeckStats.RunBatch -quit -logFile <log> -shots SCR/neck_work/stats_shots.json -out <dir>`.

## Appendix: full diff of `FrontRoomsMapWorld.cs` (clone base → neck)

Base md5 `e5204f9c7a7e07f9fa6d584915e6297e`, neck md5 `7253cb3d076a1cbba9f3af37826eb6a5`.

```diff
--- base
+++ neck
@@ -199,6 +199,14 @@
             uvs.Add(uv / repeat);
         }
 
+        // TRANSITION neck: append the transition kit's triangles (chunk-local), with the same world-planar UVs.
+        public void Append(List<Vector3> v, List<Vector3> n, List<int> t, Vector3 worldOffset, float repeat)
+        {
+            var i0 = vertices.Count;
+            for (var k = 0; k < v.Count; k++) Corner(v[k], n[k], worldOffset, repeat);
+            for (var k = 0; k < t.Count; k++) triangles.Add(i0 + t[k]);
+        }
+
         public Mesh ToMesh(string name)
         {
             var mesh = new Mesh { name = name };
@@ -800,6 +808,13 @@
             collision.Box(center, size, origin, 1f);
         }
 
+        // TRANSITION neck: the transition kit sees this chunk's edges and corners through the map.
+        // Faces go in the block of the cell they face (its 6 m block, clamped into this chunk); grime-free trims share one block.
+        var trans = FrontRoomsTransitionKit.Enabled ? new FrontRoomsTransitionKit.Chunk(transitionView ??= new TransitionView(this), origin, BlockOf(0, 0, ModuleUnits.StandardCeiling)) : null;
+        var chunkCell0 = MapGrid.ChunkOrigin(coord);
+        int FacedBlock(GridCoord faced, float ceiling) => BlockOf(Mathf.Clamp(faced.x - chunkCell0.x, 0, n - 1), Mathf.Clamp(faced.y - chunkCell0.y, 0, n - 1), ceiling);
+        Func<GridCoord, float, int> blockAt = trans != null ? new Func<GridCoord, float, int>(FacedBlock) : null;
+
         for (var j = 0; j < n; j++)
         for (var i = 0; i < n; i++)
         {
@@ -832,14 +847,27 @@
             // A wall's ends reach half a thickness past its corner, except into the start area.
             // The start area's side walls also stop on the door line, where the stream room's end wall closes the corner.
             var startSide = InStartArea(cell) != InStartArea(east);
+            // TRANSITION neck (B0.2): each skin of a wall goes in the block of the cell it faces, with that cell's ceiling, so its grime band sits under its own ceiling.
+            var ii = i; var jj = j;
+            // A wall with one finish and one ceiling class on both sides stays one box in this cell's block, as before.
+            int SideBlock(GridCoord a, GridCoord b, float fallback, bool same)
+            {
+                if (InStartArea(a) || InStartArea(b)) return BlockOf(ii, jj, fallback);
+                var ca = MapGrid.CeilingHeight(Cache.ZoneOf(a).height);
+                return same && HeightClass(ca) == HeightClass(MapGrid.CeilingHeight(Cache.ZoneOf(b).height)) ? BlockOf(ii, jj, fallback) : FacedBlock(a, ca);
+            }
             BuildEdge(chunk, eastKind, cell, east, new Vector3((i + 1) * cs, 0f, j * cs), Vector3.forward,
                 eastHeight, eastA, eastB, BlockOf(i, j, eastHeight), Get, Solid, origin,
                 !BothInStartArea(new GridCoord(cell.x, cell.y - 1), new GridCoord(cell.x + 1, cell.y - 1)),
-                !BothInStartArea(new GridCoord(cell.x, cell.y + 1), new GridCoord(cell.x + 1, cell.y + 1)) && !(startSide && cell.y + 1 == startArea.yMax));
+                !BothInStartArea(new GridCoord(cell.x, cell.y + 1), new GridCoord(cell.x + 1, cell.y + 1)) && !(startSide && cell.y + 1 == startArea.yMax),
+                trans, blockAt, SideBlock(cell, east, eastHeight, eastA == eastB), SideBlock(east, cell, eastHeight, eastA == eastB));
             BuildEdge(chunk, northKind, cell, north, new Vector3(i * cs, 0f, (j + 1) * cs), Vector3.right,
                 northHeight, northA, northB, BlockOf(i, j, northHeight), Get, Solid, origin,
                 !BothInStartArea(new GridCoord(cell.x - 1, cell.y), new GridCoord(cell.x - 1, cell.y + 1)),
-                !BothInStartArea(new GridCoord(cell.x + 1, cell.y), new GridCoord(cell.x + 1, cell.y + 1)));
+                !BothInStartArea(new GridCoord(cell.x + 1, cell.y), new GridCoord(cell.x + 1, cell.y + 1)),
+                trans, blockAt, SideBlock(cell, north, northHeight, northA == northB), SideBlock(north, cell, northHeight, northA == northB));
+            // TRANSITION neck (B0.1): the post at this cell's north-east corner, where finishes meet.
+            trans?.Post(cell.x + 1, cell.y + 1, blockAt);
 
             if (data.pillar[i + j * (n + 1)] && !TouchesStartArea(cell.x, cell.y, cell.x, cell.y))
             {
@@ -851,6 +879,18 @@
             }
 
             if (!reserved) BuildFixture(chunk, cell, cellCenter, height, theme, data.lamp[index], data.tier);
+        }
+
+        // TRANSITION neck: the kit's necks, recesses, posts and trims join the chunk's own builders and collision.
+        if (trans != null)
+        {
+            foreach (var pair in trans.buffers)
+            {
+                var material = TransitionMaterial(pair.Key.mat);
+                if (material == null || pair.Value.v.Count == 0) continue;
+                Get(pair.Key.block, material).Append(pair.Value.v, pair.Value.n, pair.Value.t, origin, WallpaperRepeat);
+            }
+            foreach (var box in trans.boxes) collision.Box(box.center, box.size, origin, 1f);
         }
 
         for (var b = 0; b < builders.Length; b++)
@@ -944,29 +984,44 @@
     /// </summary>
     void BuildEdge(BuiltChunk chunk, EdgeKind kind, GridCoord a, GridCoord b, Vector3 start, Vector3 along, float height,
         Material wallA, Material wallB, int blockIndex, BuilderFn get, SolidFn solid, Vector3 origin,
-        bool mayExtendStart = true, bool mayExtendEnd = true)
+        bool mayExtendStart = true, bool mayExtendEnd = true,
+        FrontRoomsTransitionKit.Chunk trans = null, Func<GridCoord, float, int> blockAt = null, int blockA = -1, int blockB = -1)
     {
-        if (kind == EdgeKind.Open) return;
         var length = MapGrid.CellSize;
         // Positive "across" points from cell a into cell b.
         var across = new Vector3(along.z, 0f, along.x);
+        // TRANSITION neck: a frameless crossing on an Office | Level 0 border is a 0.6 m neck (kit), nothing else.
+        if (trans != null && (kind == EdgeKind.Open || kind == EdgeKind.Arch) && trans.IsNeck(a, b))
+        {
+            trans.Neck(a, b, start, along, across, blockAt);
+            return;
+        }
+        if (kind == EdgeKind.Open) return;
+        // TRANSITION neck (B0.1): end offsets at the two corners (+ runs past the corner, - stops at a post or neck face).
+        float startOff = WallThickness * .5f, endOff = WallThickness * .5f;
+        if (trans != null) trans.Offsets(a, b, out startOff, out endOff);
+        if (!mayExtendStart) startOff = Mathf.Min(startOff, 0f);
+        if (!mayExtendEnd) endOff = Mathf.Min(endOff, 0f);
+        // TRANSITION neck (B0.2): two skins whenever the sides differ in finish or in height block.
+        if (blockA < 0) blockA = blockIndex;
+        if (blockB < 0) blockB = blockIndex;
+        if (trans == null) blockA = blockB = blockIndex;
         void Piece(float from, float to, float bottom, float top, bool extendStart, bool extendEnd)
         {
-            if (from > 0f || !mayExtendStart) extendStart = false;
-            if (to < length || !mayExtendEnd) extendEnd = false;
-            var f = from - (extendStart ? WallThickness * .5f : 0f);
-            var t = to + (extendEnd ? WallThickness * .5f : 0f);
+            // TRANSITION neck: pieces touching a corner take its offset; any piece is clipped clear of a post or neck there.
+            var f = from <= 0f && extendStart ? -startOff : startOff < 0f ? Mathf.Max(from, -startOff) : from;
+            var t = to >= length && extendEnd ? length + endOff : endOff < 0f ? Mathf.Min(to, length + endOff) : to;
             if (t - f < .05f || top - bottom < .05f) return;
             var center = start + along * ((f + t) * .5f) + Vector3.up * ((bottom + top) * .5f);
-            if (wallA == wallB)
+            if (wallA == wallB && blockA == blockB)
             {
                 var size = along * (t - f) + across * WallThickness + Vector3.up * (top - bottom);
-                solid(blockIndex, wallA, center, Abs(size), WallpaperRepeat);
+                solid(blockA, wallA, center, Abs(size), WallpaperRepeat);
                 return;
             }
             var half = along * (t - f) + across * (WallThickness * .5f) + Vector3.up * (top - bottom);
-            solid(blockIndex, wallA, center - across * (WallThickness * .25f), Abs(half), WallpaperRepeat);
-            solid(blockIndex, wallB, center + across * (WallThickness * .25f), Abs(half), WallpaperRepeat);
+            solid(blockA, wallA, center - across * (WallThickness * .25f), Abs(half), WallpaperRepeat);
+            solid(blockB, wallB, center + across * (WallThickness * .25f), Abs(half), WallpaperRepeat);
         }
 
         if (kind == EdgeKind.Wall) { Piece(0f, length, 0f, height, true, true); return; }
@@ -1007,6 +1062,14 @@
             trims.Box(jc, Abs(along * frame + across * jambDepth + Vector3.up * (openingTop - sill)), origin, 1f);
         }
         trims.Box(start + along * c + Vector3.up * (openingTop + frame * .5f), Abs(along * (width + frame * 2f) + across * jambDepth + Vector3.up * frame), origin, 1f);
+
+        // TRANSITION neck: a border door stands at the back of a 0.30 m lined recess on its stop side.
+        // Clone rule: the stop side is the Office side (doors here are double-acting; Red's 12:21 file: the side opposite FixedSwing).
+        if (trans != null && kind == EdgeKind.Door && wallA != wallB)
+        {
+            var officeB = Cache.ZoneOf(b).theme == ZoneTheme.Office;
+            trans.Recess(a, b, start, along, across, officeB ? 1f : -1f, c, width, blockAt);
+        }
 
         var edge = EdgeId(a, b);
         var openingCenter = chunk.root.transform.TransformPoint(start + along * c);
@@ -2069,7 +2132,72 @@
         keyGlow = Own(FrontRoomsSurfaces.Lit("Map test / key", new Color(.96f, .87f, .23f), .4f, 0f, new Color(.96f, .87f, .23f) * .8f));
         glass = Own(TransparentGlass("Map test / glass", new Color(.75f, .85f, .88f, .28f)));
     }
+
+    // ---------- TRANSITION neck: the map as the transition kit sees it ----------
+
+    TransitionView transitionView;
 
+    /// <summary>Edges as built (start area included), each cell's theme and ceiling, and the arch openings.</summary>
+    sealed class TransitionView : FrontRoomsTransitionKit.IMap
+    {
+        readonly FrontRoomsMapWorld w;
+        public TransitionView(FrontRoomsMapWorld w) { this.w = w; }
+
+        public FrontRoomsTransitionKit.EdgeInfo Edge(GridCoord lo, GridCoord hi)
+        {
+            var za = w.Cache.ZoneOf(lo);
+            var zb = w.Cache.ZoneOf(hi);
+            var info = new FrontRoomsTransitionKit.EdgeInfo
+            {
+                kind = w.Cache.Edge(lo, hi),
+                themeLo = za.theme, themeHi = zb.theme,
+                ceilLo = MapGrid.CeilingHeight(za.height), ceilHi = MapGrid.CeilingHeight(zb.height),
+            };
+            if (w.InStartArea(lo) || w.InStartArea(hi))
+            {
+                // As StartAreaEdge: open inside, a plain wall in the outside cell's paper on its rim.
+                var inA = w.InStartArea(lo); var inB = w.InStartArea(hi);
+                info.kind = inA && inB || inA && hi.y > lo.y ? EdgeKind.Open : EdgeKind.Wall;
+                info.themeLo = info.themeHi = inA ? zb.theme : za.theme;
+            }
+            switch (info.kind)
+            {
+                case EdgeKind.Arch:
+                    w.ArchOpening(lo, hi.y > lo.y, out var width, out var center);
+                    info.open0 = center - width * .5f; info.open1 = center + width * .5f;
+                    break;
+                case EdgeKind.Door:
+                    info.open0 = MapGrid.CellSize * .5f - DoorWidth * .5f; info.open1 = MapGrid.CellSize * .5f + DoorWidth * .5f;
+                    break;
+                case EdgeKind.Window:
+                    info.open0 = MapGrid.CellSize * .5f - WindowWidth * .5f; info.open1 = MapGrid.CellSize * .5f + WindowWidth * .5f;
+                    break;
+                default:
+                    info.open0 = 0f; info.open1 = MapGrid.CellSize;
+                    break;
+            }
+            return info;
+        }
+
+        public ZoneTheme Theme(GridCoord cell) => w.Cache.ZoneOf(cell).theme;
+        public float Ceiling(GridCoord cell) => MapGrid.CeilingHeight(w.Cache.ZoneOf(cell).height);
+        public bool Reserved(GridCoord cell) => w.InStartArea(cell);
+    }
+
+    Material TransitionMaterial(FrontRoomsTransitionKit.Mat mat)
+    {
+        switch (mat)
+        {
+            case FrontRoomsTransitionKit.Mat.L0Wall: return level0.wall;
+            case FrontRoomsTransitionKit.Mat.OfficeWall: return office.wall;
+            case FrontRoomsTransitionKit.Mat.OfficeCarpet: return office.floor;
+            case FrontRoomsTransitionKit.Mat.Cove: return trim;
+            case FrontRoomsTransitionKit.Mat.Aluminium: return FrontRoomsSurfaces.TryGet("Prop_Aluminium") ?? trim;
+            case FrontRoomsTransitionKit.Mat.WallAngle: return FrontRoomsSurfaces.TryGet("Painted_Metal") ?? office.ceiling;
+            default: return null;
+        }
+    }
+
     static Material TransparentGlass(string name, Color color)
     {
         var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
```
