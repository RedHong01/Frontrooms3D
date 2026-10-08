# Level transitions · 16 · The pick and the closed border, rendered over today's game

Date: 2026-10-08. Made by the fix stage of the level-transitions workflow, after the critic review of the Figma section. Nothing was implemented in Red's project. Unity never ran on Frontrooms3D. Nothing was downloaded.

## 0. Short answer

- **The pick now has pictures.** I rendered "Frame on every border + renovation on 3 in 10 + the colour as it runs now" in a clone of **today's main** (22bb75f, with the V5 colour and B0 live). Same harness, same cameras, same seed as every other variation. The layout did not move: all plan files match.
- **It works where you look at a crossing, and it is quiet in your own frame.**
  - Shot 2 (a dressed crossing on the left, a portal ahead): 12.7 % of pixels change.
  - Shot 4 (your G10 frame): only **1.1 %** change. The portal at the open edge on the right is a thin strip at that distance.
  - The new camera 3.4 m from that open edge (shot 8) shows the portal clearly: pilasters, header, Office lining, base.
- **A new option family: Close.** The map closes Office | Level 0 borders the way it already closes a height change: a door on the maze tree, otherwise a door or a wall. It removes every frameless crossing (1.26 per chunk → 0). Doors go from 1.37 to 2.53 per chunk. In your frame, 8 % of pixels change; at the arch, 34.5 %.
- **Cost of the pick** (within 46 m of each eye): triangles 145 k → 314 k (×2.2), renderers +9–11 %, materials 42 → 62–63, 94 MB of renovation textures. Lights unchanged.

## 1. Images (all in `images/`)

| File | What |
|---|---|
| `pick_sheet.jpg` | Rows shots 1, 2, 4, 8, 9 × columns NOW (main today) · FRAME (V1 over main) · PICK (V1 + V2 on 30 %) |
| `close_sheet.jpg` | Rows shots 1, 2, 4, 3 × columns NOW · CLOSED (map rule + door frames) |
| `pick_now_shot{1,2,3,4,8,9,10}.jpg` | The game today (`-pickMode now`) |
| `pick_frame_shot{…}.jpg` | Frame only over main (`-pickMode frame`) |
| `pick_mix_shot{…}.jpg` | The pick (`-pickMode pick`) |
| `close_shot{…}.jpg` | Closed borders, with the frame kit on the new doors (`-pickMode frame -closeBorders`) |
| `close_plan_shot4.jpg`, `close_plan_shot2.jpg` | Plan drawings, NOW and CLOSED, of the 13 × 13 cells round shots 4 and 2, drawn from the harness `-plan` files |
| `v_lightlead_soft_shot1.jpg`, `_shot2.jpg` | 636 × 360 crops of the SOFT column of `v_lightlead_vs_before.jpg` (no new render) |
| `rb_20b_arch_cased_opening.jpg`, `rb_20c_open_edge_portal.jpg` | Panels B and C of `rb_20_fr_scale_plan_details.jpg` at full resolution |

Shots 1–4 are the fixed cameras of `shots.json`. The extras:
- **shot8:** eye (794.6, 1.62, 604.5), yaw 90: from Level 0 cell (264,201), 3.4 m from the open edge at X = 798.
- **shot9:** eye (770.0, 1.62, 580.2), yaw −90: from cell (256,193), 5 m from the arch (254,193)|(255,193) on X = 765, which is dressed at salt 0.
- **shot10:** eye (767.3, 1.62, 590.3), yaw −160: from Office cell (255,196) through the open edge at Z = 588 (framed) into the X = 765 corridor, where a dressed open edge and a dressed arch follow.
- A first shot9 (eye 766.3, 577.5, yaw −12) faced a wall; it was replaced and is not used. Extras 9 and 10 were rendered in a second call, so their film grain differs from a one-call run; nothing else does.

## 2. The clone and the protocol

- **Clone:** `W/proj_trans_pick`, made with `W/tools/mkclone.sh` at 00:23 on 2026-10-08 from main **22bb75f**. Local patch: none needed. Tools installed.
- **Harness:** `Assets/Editor/Audit/FrontRoomsTransitionAudit.cs` md5 `76a624e3217630e637bdb381af7ca5b1` (unchanged). Every run passed `-buildTarget OSXUniversal`. Never two Unity processes on the clone.
- **Layout frozen:** the `-plan` files of shots 1–4 in modes now, frame and pick are byte-identical to `logs/shot<k>_plan.json` (the Oct 3 BEFORE). So today's main builds the same layout as the Oct 3 base for this seed.
- **Lamps:** lit 84 / 82 / 83 / 88, shadowed 8 / 7 / 7 / 7 in every mode, the same as the Oct 3 BEFORE.
- **Logs:** 0 `error CS` and 0 exceptions in every log (`W/trans_pick_work/logs/`). Compile also checked outside Unity with `W/tools/roslyn_check.py --all`: 0 errors.
- **Copies:** `logs/pick/` (shot logs, counts, stats) and `code/pick/` (all code, diffs and shot files).

## 3. What was built (clone only)

The three variations were never built together. The frame clone (`proj_trans_frame`) was lost in the 10-05 reboot, so its code was **recovered from the transcripts** first.

### 3.1 Recovering V1 Frame

- `W/tools_recovery/sim.py` replayed every edit to `proj_trans_frame` (17 operations, 0 failed) on `git archive 2c4e50f` (the Oct 3 base).
- The replayed `FrontRoomsMapWorld.cs` is **byte-identical** to the base plus the diff in `11_var_frame.md` (appendix). The kit has 1,016 lines, as the report says.
- Recovered files are saved in `code/frame/` (kit, map diff, asset prep, stats, collision check).

### 3.2 The pick over main

| File (clone) | What |
|---|---|
| `Assets/Scripts/Rendering/Transitions/FrontRoomsTransitionPick.cs` (new) | The switch: `-pickMode now | frame | pick | renoall`, `-renoSalt` (default 0), `-renoShare` (default 0.30), `-pickFrameWindows` |
| `…/FrontRoomsTransitionFrameKit.cs` (new) | The V1 kit, class renamed. Window casings off by default (main's `DressWindow` already frames every map window). Its own B0 post quarters are not used |
| `…/FrontRoomsTransitionRenovationKit.cs` (new) | The V2 kit, class renamed. Changes: salt 0; `Dressed(p, q)`: one draw per border crossing (`Hash(seed, edge, 0, 977 + salt) < 0.30`) gates the band events, door and arch halos, arch reveals and crossing floors and ceilings; `WallPiece` takes the frame kit's insets |
| `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs` | Hooks marked `// PICK`: +255 / −11 lines over main (`code/pick/mapworld_pick.diff`) |
| `Assets/Editor/Rendering/FrontRoomsTransitionAssets.cs`, `Assets/Editor/Transitions/FrontRoomsTransitionFrameAssets.cs`, `…/FrontRoomsTransitionPickTools.cs` | Asset prep: the 19 renovation materials made against main's `FrontRooms/Surface`, the 4 prop FBXs re-imported, `Trans_AluminiumSatin` |
| `Assets/Resources/Surfaces/Textures/*` (30), `Assets/Resources/Props/Models/Kit_Trans*` (4 + json) | Copied with their `.meta` from `W/proj_trans_renovation2` |

**How the two kits share one wall:**
- Outside the start area, **every wall piece is built by the renovation kit's `WallPiece`**: its corner rule owns each post (B0.1), each skin goes in its own side's block (main's B0.2 blocks), and dressed faces get the band.
- The frame kit adds its pieces on top: cased arches, portals, steel door frames with saddles, end caps, corner guards, free posts and the cove base on every Office wall.
- Its bead insets (cased arches) and end-cap insets are passed into `WallPiece`, so no piece overlaps the Office lining.
- **A dressed crossing belongs to the renovation kit:** no casing or portal there, a bare reveal with corner beads instead. Doors keep their steel frame either way.
- Collision boxes are the pre-B0 extents. Main's own B0 path is untouched and still builds the start-area edges.

At salt 0, 11 of the 43 border crossings in the 64 × 36 m plan round the shots are dressed (26 %). None of the four fixed crossings is dressed; the X = 765 corridor (shots 2, 9, 10) has three dressed crossings.

### 3.3 Close (the map rule)

- `FrontRoomsMap.cs`, `Resolve`: `if (ha != hb || (CloseThemeBorders && ZoneOf(a).theme != ZoneOf(b).theme))`. So a theme border becomes a door (a window when one side is Tall) on the maze tree, otherwise a door with the existing `borderOpening` chance, else a wall. Clone only, behind `-closeBorders` (`code/pick/map_close_rule.diff`, 20 lines).
- Rendered with `-pickMode frame`, so the new doors get the frame kit's steel frame, saddle and base.

## 4. Measurements

### 4.1 Share of pixels that change (> 16/255), against NOW

| Shot | Frame | Pick | Closed |
|---|---|---|---|
| shot1 (the arch) | 1.9 % | 1.9 % | 34.5 % |
| shot2 (corridor) | 4.2 % | **12.7 %** | 38.1 % |
| shot3 (door) | 2.5 % | 2.5 % | — |
| shot4 (your frame) | 1.1 % | 1.1 % | 8.0 % |
| shot8 (open edge, 3.4 m) | 3.2 % | 3.2 % | — |
| shot9 (dressed arch) | 1.2 % | 3.6 % | — |
| shot10 (corridor from Office) | — | 11.3 % | — |

### 4.2 Cut counts, 20 seeds × 81 chunks (`CountBatch`; per chunk)

| Key | Now | Closed |
|---|---|---|
| open theme edges | 0.890 | 0 |
| arch theme edges | 0.369 | 0.001 (one module-stamped arch in 1,620 chunks) |
| door theme edges | 1.370 | 2.530 |
| wall theme edges | 2.281 | 2.380 |
| window theme edges | 0.331 | 0.331 |
| flat seams | 0.614 | 0 |
| floor cut (m) | 4.58 | 2.53 (only in doorways) |
| ceiling cut (m) | 2.67 | 0 |
| corner-post z-fight (counted before B0) | 3.17 | 3.12 |

The Now column equals the Oct 3 counts in `03_cut_inventory.md`, so today's generator is unchanged for these keys.

### 4.3 Cost of the pick, within 46 m of each eye (`FrontRoomsTransitionAssets.Stats`)

| Shot | Renderers | Materials | Triangles | Kit renderers | Props |
|---|---|---|---|---|---|
| shot1 | 1,744 → 1,908 | 42 → 62 | 145,290 → 314,102 | 72 | 9 |
| shot2 | 1,743 → 1,919 | 42 → 63 | 135,970 → 304,506 | 81 | 14 |
| shot3 | 1,711 → 1,898 | 42 → 63 | 146,442 → 312,326 | 91 | 14 |
| shot4 | 1,741 → 1,893 | 42 → 62 | 143,208 → 309,242 | 68 | 9 |

- Lights are unchanged (84 / 82 / 83 / 88 lit).
- Texture memory is the renovation set: 94 MB on desktop (WebGL estimate 34 MB), loaded as soon as one dressed crossing is in range.
- Triangles more than double even at 30 %. Part is the renovation (55–69 k kit triangles) and part is that every wall is now cut into per-face boxes. The renovation report's plan to cut triangles about 70 % still applies.

## 5. What the pictures show

- **Frame over today's game:** portals read well up close (shot 8), the steel frame and saddle read at the door (shot 3), and the cased arch is a thin white edge (shot 1). In your frame (shot 4) the portal is a strip at the right edge.
- **Pick:** where a crossing is dressed, it reads at once (shot 2: stripped paper, primer drops and paper scraps beside a framed portal; shot 9: a bare arch reveal with corner beads, the slab strip, loose and missing ceiling tiles, a pail).
- **Closed:** the arch of your screenshot becomes a closed wooden door in a steel frame; the corridor in shot 2 ends at doors on both sides. The Office is only seen through an open door.

## 6. Open issues

1. **Frame is quiet in your frame.** 1.1 % of pixels in shot 4. If you want the change to read from there, the choice is Close or Neck, not a better frame.
2. **Paper scraps use the older print.** The renovation's `Paper_Scrap` material still points at `Wallpaper_Chevron`; main's Level 0 paper now uses the hard-edge print. It must copy `L0_Wallpaper`, as `Paper_Peel` already does, before any merge.
3. **Cost.** Triangles ×2.2 and 94 MB of textures for the pick. Needs the renovation's planned cuts.
4. **Corner rule and neighbours (codex audit MAP-2).** The renovation kit's corner rule and the frame kit's corner descriptor read the neighbour chunk's edges, as main's B0 does. The audit found that a revisit can bring overlaps back. The map's fix comes first.
5. **Close changes the layout.** Nav, key and Relay tests must rerun. Doors also change pacing: every border becomes a door to open.
6. **Main kit file (codex audit MAP-5).** Any merge goes into main's `FrontRoomsTransitionKit.cs` (GUID `b215858f…`) as a contract, never a file copy.
7. **Free posts** (5 per 25 chunks) have no collider.

## 7. Paths

- Clone: `/Users/redwang/FrontRoomsVisualWork/proj_trans_pick` (also holds the close rule behind `-closeBorders`).
- Work folder: `/Users/redwang/FrontRoomsVisualWork/trans_pick_work` (frames `shots_*`, `shots2_*`, counts, stats, logs, the recovered frame sandbox `sim_frame/`).
- Verification log: VL201 (`2858:6157`, the pick) and VL202 (`2858:6173`, closed borders).
