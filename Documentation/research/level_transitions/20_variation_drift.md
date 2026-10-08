# Level transitions · 20 · Variation V4 Drift: summary

- Built 2026-10-03; patch option 2026-10-04; rebuilt and re-rendered 2026-10-07 after the 10-05 wipe (the rebuilt set matches the 10-03 set).
- Full report, numbers, diff and logs: `14_var_drift.md`. This page is the short version.
- Built and rendered in private clones only: `W/proj_trans_drift` (frozen 10-03 BEFORE base) and `W/proj_trans_drift_patch`. Nothing is implemented in Red's project. Drift is **not** in main.

## Images (`images/`)

| File | What |
|---|---|
| `v_drift_vs_before.jpg` | Six rows, BEFORE \| DRIFT (W 4.5 m): the four fixed shots and two extra shots. |
| `v_drift_shot1.jpg` .. `shot4.jpg` | The four fixed shots (same harness and cameras as `shot<k>_before.jpg`). Same files as `var_drift_shot1..4.jpg`. |
| `v_drift_shot5.jpg` | Extra: from inside the Office, 8 m back, through the shot1 arch into Level 0. It shows the gradient best. |
| `v_drift_shot6.jpg` | Extra: close view of drops, carpet squares and joint lines on the Z = 588 open edge. |
| `v_drift_plan.jpg` | Plan views (straight down and up), BEFORE \| DRIFT, with walls and the theme line drawn on. |
| `v_drift_widths.jpg` | The four fixed shots at W = 3, 4.5 and 6 m. |
| `v_drift_patch_vs_drift.jpg`, `v_drift_patch_shot1..6.jpg`, `v_drift_patch_plan.jpg` | The patch option (below). |
| `var_drift_sheet.jpg` | Four rows, before \| after (protocol name). |
| `var_drift_close_<type>.jpg` | Close frames, BEFORE (hooks off) \| DRIFT: arch, post, postsplit, wall, seam, door, window, open. |

## What was built

- **Theme field.** One texel per cell (40 × 40 for 5 × 5 chunks): metres to the other theme's line, negative in Level 0, clamped to ±6. RHalf, 3,200 bytes, a global texture.
- **Shader keyword `_FR_DRIFT`** on the six theme materials. Each carries its counterpart's textures and values.
- **Units.** Paper drops 0.75 m × full height; carpet squares 0.6 × 0.6 m; ceiling tiles 0.6 × 1.2 m (on the bars of both grids). A foreign unit's chance is smoothstep(−W, W, s): 50 % on the line, 0 at 4.5 m.
- **Joints.** Every change sits on a unit joint and gets a thin joint line (1.5 mm paper, 2.5 mm carpet). No blends, no noise.
- **Linings.** Arch jambs and soffits, free wall ends and columns in the band are one 5 mm lining in one finish (50/50 on a border), so no drop joint splits them.
- **B0.** Corners in the band never overlap in the post square. Each face of a height-border wall keeps its own grime band.
- **Patch option (`-driftPatch`, off by default).** Carpet squares and ceiling tiles come in rectangles of 1 to about 9 units (brick-bond 5 × 5 blocks, guillotine cuts), like cut-in carpet patches. Paper drops stay one per drop. It answers the checkerboard on the line at the same cost.

## Change list (clone)

1. `FrontRoomsMapWorld.cs`: 15 hunks, +112 / −9 lines, 17 lines marked `// TRANSITION drift`: chunk events, UV1 face extents on shell meshes, corner extents and per-side blocks (B0), lining boxes in `BuildEdge`, a column finish in `BuildColumn`.
2. `FrontRoomsSurface.shader`: +270 lines (the `_FR_DRIFT` path); the patch option adds +67 / −4.
3. Six theme materials: keyword, own theme, unit, counterpart set (+28 lines each).
4. New: `Scripts/Rendering/Transitions/FrontRoomsThemeField.cs` (9.8 KB), `FrontRoomsTransitionKit.cs` (drift version, 6.5 KB), two lining materials (3.0 KB each).
5. Tools only: `Editor/Transitions/FrontRoomsDriftPrepare.cs`, `FrontRoomsDriftStats.cs`, `FrontRoomsDriftPlan.cs`.
6. Unchanged: the harness (md5 `76a624e3…`), `shots.json`, zones, maze, rooms, modules, columns, edge kinds, doors, lamps, frames. Every `-plan` file is identical to `logs/shot<k>_plan.json`. Lamps identical (lit 84 / 82 / 83 / 88).
7. Code copies and diffs: `code/drift/`.

## Results

| Shot | What changed |
|---|---|
| shot1 | Beige squares and yellow drops through the arch; grey drops and blue-grey squares in the Level 0 room (more than the brief's "one", because the room has Office on two sides); the arch reveal in one grey lining. |
| shot2 | The corridor floor mixes over about Z 584–592; both walls carry mixed drops; no straight line left. |
| shot3 | One yellow drop on the door wall and three on the side wall; the Level 0 room is mostly out of view. |
| shot4 | The X = 798 edge is lost: grey and yellow drops alternate; blue-grey squares run 4 m into the Level 0 floor. |

- Units switched per chunk that touches a border: 91 carpet squares, 46 ceiling tiles, 40 paper drops (4.6 %, 4.7 % and 6.1 % of all units over 38 chunks). Patch option: 96, 48, 41.
- Ceilings read weakly: the two ceiling tiles differ by 1.5 % in brightness, so only the grid changes (2×2 among 2×4).

## Costs measured (own stats method, within 46 m of each eye)

| | shot1 | shot2 | shot3 | shot4 |
|---|---|---|---|---|
| Renderers, BEFORE → DRIFT | 1,801 → 1,872 | 1,779 → 1,857 | 1,756 → 1,842 | 1,791 → 1,863 |
| Triangles, BEFORE → DRIFT | 175,766 → 180,974 | 162,760 → 167,908 | 178,600 → 183,736 | 172,554 → 177,714 |
| Materials | 42 → 44 | 42 → 44 | 42 → 44 | 42 → 44 |
| Lit lights (shadowed) | 84 (8) | 82 (7) | 83 (7) | 88 (7) |

- Drift itself: 0 triangles, 0 lights, 0 new texture sets, one 3,200-byte field texture, one shader keyword (737,299 variants for all keyword combinations; 138,247 used by the scene).
- B0 and linings: +2.9–3.2 % triangles; +71–86 renderers (+3.9–4.9 %), almost all from the two lining materials. Batch mode gives no draw count, so renderers stand in for draws.
- Frame time (render + 1-px sync, 1080p, 4× MSAA, M3 Max): +0.32 ms and −0.12 ms at the first two eyes at low load (frames ~16.5 ms, MAD ~0.5 ms). A full run under a load average of ~600 was noise. The cost is below what this method resolves.
- UV1 face data: +16 B per shell vertex.

## What the real implementation needs

- **Map chat (contract):**
  - `ChunkBuilt` / `ChunkReleased` / `MapReleased` events (shared with RT);
  - UV1 face extents on shell meshes;
  - B0 merged over main's B0 from V5: in the band every corner counts as mixed, and the L owner follows the drop axis;
  - the lining hooks in `BuildEdge` and `BuildColumn`;
  - the codex audit's MAP-2 full fix (quarter posts on chunk lines). Drift's corner rule reads the neighbour chunk's arms just like main's, and the audit's minimal fix (keep +8 cm at chunk-line posts) would z-fight in the band.
- **Visual chat:**
  - shader, field class, six material pairs as `SurfaceDef` rows in `FrontRoomsRenderSetup`;
  - merge into main's kit file and GUID `b215858f…` (codex audit MAP-5), never copy the clone's kit over it;
  - put the lining finish in the face data and drop the two lining materials, so linings add 0 renderers (matters on WebGL);
  - an early-out for pixels far from a border;
  - a Play-mode GPU timing on an idle machine;
  - Red's choice of W and of the patch option.
- **Others:** wallpaper chat and 平面视觉: EGRESS ink on drifted drops must follow the unit choice or avoid the band.

## Main (Codex) and this variation

- Main (`6c6fe81`, 2026-10-07) runs V5 light lead and its B0, as Codex promoted them on 10-03. Red keeps V5 on until the Figma pick.
- No drift file is in main. I changed nothing in main.
- If Red picks Drift, it is re-rendered over main first, then merged as a diff over main's B0 (`14` §11).

## Open issues

1. Ceilings: only the grid changes. Accept, or use a whiter Office tile.
2. Per-unit squares make a checkerboard on the line; the patch option fixes it. Red picks.
3. Small Level 0 rooms between two Office borders lose about a third of their paper.
4. EGRESS ink on drifted drops.
5. Drift does not put the change on a built object (the research's main rule). The plan pairs it with V1 Frame, for tier 4–5 chunks.
6. GPU cost not resolved (machine load); needs a Play-mode GPU timer.
7. Drift's B0 has the MAP-2 chunk-line dependency too.

## Verification log (Figma)

| VL | Node | Check | Verdict |
|---|---|---|---|
| VL137 | `2795:6097` | Drift hides the zone line | FLAG |
| VL138 | `2795:6119` | Patches, not a checkerboard | WAIT-RED |
| VL139 | `2795:6135` | Posts and linings in the band | PASS |

## Outputs in Red's project

`Documentation/research/level_transitions/`: `14_var_drift.md`, `20_variation_drift.md`, `code/drift/`, and in `images/`: `var_drift_shot1..4.jpg`, `var_drift_sheet.jpg`, `var_drift_close_*.jpg` (8), `v_drift_shot1..6.jpg`, `v_drift_vs_before.jpg`, `v_drift_plan.jpg`, `v_drift_widths.jpg`, `v_drift_patch_shot1..6.jpg`, `v_drift_patch_vs_drift.jpg`, `v_drift_patch_plan.jpg`. Plus three rows in `Documentation/VERIFICATION_LOG.md`.
