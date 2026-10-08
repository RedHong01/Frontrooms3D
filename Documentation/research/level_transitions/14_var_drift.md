# Level transitions · 14 · V4 Drift: pre-render report

- **Built:** 2026-10-03 (first renders 17:14–17:49, final set 22:40). Patch option added 2026-10-04.
- **Rebuilt:** 2026-10-07 16:38–18:55. The 2026-10-05 reboot wiped the old scratchpad, with the clone and every output that lived only there. I rebuilt the clone from the recovered code and re-rendered everything. The rebuilt final set matches the 2026-10-03 set (§3).
- **Clone:** `W/proj_trans_drift` (W = `/Users/redwang/FrontRoomsVisualWork`), the frozen BEFORE base plus this variation. Patch option: `W/proj_trans_drift_patch`.
- **Tags:** `drift` (main, W = 4.5 m), `drift_w3`, `drift_w6`, `drift_patch`.
- **Red's project:** nothing is implemented. Red's project received only the files in §12.
- **Short version:** `20_variation_drift.md`.

---

## 0. Short answer (for Red)

- **What it does.** Within about 4.5 m of a Level 0 | Office line, whole building units of one look turn up in the other look:
  - paper drops, 0.75 m wide and full height;
  - carpet squares, 0.6 × 0.6 m;
  - ceiling tiles, 0.6 × 1.2 m.
- **How it spreads.** A foreign unit has a 50 % chance on the line. The chance falls to 0 at 4.5 m. There are no blends and no noise. Every change sits on a unit joint. Each joint where two finishes meet gets a thin joint line: 1.5 mm on paper, 2.5 mm on carpet.
- **Result.** In all four fixed shots the straight cut is gone. The border reads as a patched place, not as a texture seam. Shot5 (from inside the Office) shows the gradient best: pure Office near the camera, a few beige squares and yellow drops near the arch, then Level 0.
- **Patch option (new, 2026-10-04).** With one draw per square, a 50/50 mix on the line can look like a checkerboard (shot2, shot6). The patch option groups carpet squares and ceiling tiles into rectangles of 1 to about 9 units, the way real carpet patches and re-laid tiles come. Paper drops keep one draw each. It reads more like repair (shot5, shot6, `v_drift_patch_vs_drift.jpg`). Same cost.
- **Cost (measured, §4).**
  - Drift itself adds 0 triangles, 0 lights and 0 texture sets. It adds one 3,200-byte field texture and one shader keyword.
  - B0 and the linings add 2.9–3.2 % triangles and 71–86 renderers (3.9–4.9 %) within 46 m of each eye. Almost all of the extra renderers are the linings (two new lining materials). The real version can put the linings in the existing renderers (§11).
  - GPU frame time: within noise. At low machine load, +0.32 ms and −0.12 ms at the first two eyes (frames of about 16.5 ms, noise ±0.5 ms).
- **Weak points.**
  - Ceilings barely show it. In this light the two ceiling tiles differ by 1.5 % in brightness, so only the grid changes (2×2 among 2×4).
  - It is still a shader effect. It does not put the change on a built object (the research's main rule).
  - Its B0 has the same chunk-line problem as main's (codex audit MAP-2, §11.3).

---

## 1. Images

All in `images/`:

| File | What it shows |
|---|---|
| `var_drift_sheet.jpg` | Four rows, BEFORE \| DRIFT W 4.5: the four fixed shots (protocol name). |
| `var_drift_shot1.jpg` .. `var_drift_shot4.jpg` | The four fixed shots, harness JPGs (same cameras as `shot<k>_before.jpg`). |
| `v_drift_shot1.jpg` .. `v_drift_shot4.jpg` | The same four frames (task name). |
| `v_drift_shot5.jpg` | Extra. From the Office (264,205), 8 m back, looking south through the shot1 arch (Z = 609.0) into Level 0. |
| `v_drift_shot6.jpg` | Extra. Close view (pitch 36°) of paper drops and carpet squares on the open edge Z = 588.0 (shot2's border). |
| `v_drift_vs_before.jpg` | Six rows, BEFORE \| DRIFT W 4.5: shots 1–6. |
| `v_drift_plan.jpg` | Plan views (orthographic, straight down and up), BEFORE \| DRIFT: the floor over 36 × 20 m, then the floor and the ceiling over 12 × 6.75 m at the X = 798.0 edge. Walls and the theme line are drawn back from the plan data. |
| `v_drift_widths.jpg` | The four fixed shots at W = 3, 4.5 and 6 m. |
| `v_drift_patch_vs_drift.jpg` | Six rows, BEFORE \| DRIFT \| DRIFT + PATCHES. |
| `v_drift_patch_shot1.jpg` .. `v_drift_patch_shot6.jpg` | The patch option, same six cameras. |
| `v_drift_patch_plan.jpg` | Plan views, BEFORE \| PATCH. |
| `var_drift_close_<type>.jpg` | Close frames from the cut-evidence harness (same candidates as `cut_*.jpg`), BEFORE (hooks off) \| DRIFT. See §1.1. |

### 1.1 What each shot had to show

| Shot | Brief | Result |
|---|---|---|
| shot1 | Through the arch: beige squares on the Office floor and 1–2 yellow drops on Office walls. On the Level 0 side: a grey drop and a few blue-grey squares. White 2×2 tiles among the 2×4. The arch reveal in one finish. | **Yes** for floor, walls and reveal (one grey lining). The Level 0 room shows **more** than one grey drop. Its cell has Office on two sides (Z = 609 and X = 795), so its units sit at s = −1.5 to 0 m and switch 26–50 % of the time. The ceiling change is only a grid change (§0). |
| shot2 | The corridor mixed over ±4.5 m round Z = 588.0, no straight line left. | **Yes.** Squares from about Z = 584 to 592. Both walls carry mixed drops. The far wall turns from yellow to grey and back. |
| shot3 | Mixed drops near the door, a few grey drops in the Level 0 Low room. | **Partly.** One yellow drop on the door wall (X 759.0–759.75, left of the door), three of four drops on the side wall. The Level 0 room's walls near the door are out of view through the frame, so its grey drops do not show. |
| shot4 | The X = 798.0 edge lost in a mixed band. | **Yes.** The right wall alternates grey and yellow drops. Blue-grey squares run 4 m into the Level 0 floor. |

---

## 2. What was built

### 2.1 Theme field (`Scripts/Rendering/Transitions/FrontRoomsThemeField.cs`, 9.8 KB, new, visual-owned)

- One texel per map cell over the built window: 40 × 40 texels for 5 × 5 chunks.
- Value: metres from the cell centre to the nearest cell centre of the other theme, minus 1.5.
  - So the bilinear field is 0 on the shared line, −1.5 at a Level 0 cell centre next to Office and +1.5 at an Office cell centre next to Level 0.
  - Negative in Level 0, positive in Office, clamped to ±6.
- Search: a bounded 7 × 7 cell search with `world.ZoneOf`. Inside the ±6 m clamp this equals a BFS (6 + 1.5 m is 2.5 cells).
- Texture: `RHalf`, bilinear, clamped, no mips: **3,200 bytes**.
- Globals only (no `MaterialPropertyBlock`s): `_FR_ThemeField`, `_FR_ThemeFieldST` (map XZ to uv), `_FR_MapOrigin`, `_FR_DriftWidth` (W = 4.5, or `-driftW`), `_FR_DriftSeed` (the run seed folded to 16 bits).
- It listens to the map's `ChunkBuilt`, `ChunkReleased` and `MapReleased` events and rebuilds the texture.
- The same field, smoothstep and hash also run on the CPU (`Sample`, `POffice`, `UnitRand`). So the stats tool and the lining choices agree with the shader bit for bit.
- `-noDrift` switches the whole variation off (field, shader mix, B0 and linings), to prove the hooks are inert.

### 2.2 Shader (copy of `FrontRooms/Surface`, +270 lines, no lines removed)

- `#pragma shader_feature_local _FR_DRIFT` on the ForwardLit pass (vertex and fragment). Other passes are unchanged.
- New properties:
  - `_FR_ThemeSelf`, `_DriftMode` (walls, or floor/ceiling), `_DriftUnit`;
  - the counterpart set `_AltBaseMap`, `_AltBumpMap`, `_AltMaskMap`, `_AltTileSize`, `_AltBaseColor`, `_AltSmoothness`, `_AltMacroTone`, `_AltStainStrength`;
  - plus `_AltBumpScale`, `_AltMacroDirt`, `_AltWetStrength`, `_AltFloorGrime`, `_AltCeilingGrime`, so a foreign unit is shaded exactly as its own material would shade it.
- The drift properties sit in the `UnityPerMaterial` CBUFFER in every variant, so the SRP Batcher layout is the same with and without the keyword. The Alt textures share the own samplers (no new sampler slots).
- **Units** (metres, anchored at map 0, in the same planar frame as the tile):
  - walls: drops 0.75 m along the wall, full height;
  - floors: 0.6 × 0.6 m (the Office carpet tile; the texture's tile joints are at 0 and 0.6 m);
  - ceilings: 0.6 (X) × 1.2 (Z) m, the Level 0 2×4 tile. Its edges lie on bars of both grids: the 2×4 texture has bars at 0 and 0.6 m in X and at 0 and 1.2 m in Z; the 2×2 texture has bars at 0 and 0.6 m both ways.
- **Per pixel:**
  1. unit index = floor(metres / unit); the unit centre in map XZ from that index;
  2. s = the field at the unit centre; pOffice = smoothstep(−W, +W, s);
  3. h = hash(unit index, plane id = round(dot(mapPos, n) / 0.04 + 0.237), axis, seed);
  4. unit theme = Office if h < pOffice, else Level 0;
  5. own theme: shaded as today. Other theme: the Alt set, with `SAMPLE_TEXTURE2D_GRAD` and derivatives taken outside the branch (no mip seams at unit edges).
- **Face extents.** A wall face can run 8 cm past a cell line (an end over a post), or be only 5–16 cm wide (an end cap, a split jamb).
  - The map passes each face's extent in UV1.
  - The shader lends such a sliver to the drop inside the line.
  - Without this, an end cap or the last 8 cm of a wall took the next drop's finish.
- **Joint line.** Where two units of different finish meet: a 1.5 mm line on paper and a 2.5 mm line on carpet, 38 % darker and half as glossy (dirt in a butt joint).
- **Edge anti-aliasing.** Within half a pixel of a unit edge, the two units are mixed by pixel coverage. There is no blend wider than one pixel.

### 2.3 Patch option (`-driftPatch`, 2026-10-04; clone `proj_trans_drift_patch`, diff `code/drift/patch_option.diff`)

- Why: open issue 2 of the first report. On the line, one draw per square makes a beige / blue-grey checkerboard.
- Floors and ceilings only:
  - 5 × 5-unit blocks (3 × 3 m of carpet) in brick bond. Block rows start 2 units off the cell lines, and each row is shifted 1–4 units, so no block edge follows a cell line.
  - Each block is cut by up to 4 guillotine cuts into rectangles of 1 to about 9 units.
  - One draw per rectangle, made at its centre, with its first unit as its id. A 1-unit rectangle draws exactly as a unit does without the option.
  - Every patch edge is still a unit joint.
- Paper drops keep one draw per drop. A first try that also grouped drops into runs of 1–4 per cell edge moved the line instead of hiding it, so it was removed.
- With the option off, the patch clone renders byte-identical to the main set.
- Cost: a few more integer hashes per pixel in the band. No geometry, no textures. Units switched per chunk are about the same (§4.3).

### 2.4 Materials (`Resources/Surfaces`, prepared by `Editor/Transitions/FrontRoomsDriftPrepare.cs`)

| Material | Own theme | Unit | Counterpart (Alt set) |
|---|---|---|---|
| `L0_Wallpaper` | Level 0 | 0.75 m drops | `Office_Wall` (Office_Drywall_A, tile 1.2 m) |
| `Office_Wall` | Office | 0.75 m drops | `L0_Wallpaper` (Wallpaper_Chevron_A, tile 0.75 × 1.13 m) |
| `L0_Carpet` | Level 0 | 0.6 × 0.6 m | `Office_Carpet` (Office_CarpetTile_A, tile 1.2 m) |
| `Office_Carpet` | Office | 0.6 × 0.6 m | `L0_Carpet` (Carpet_LoopPile_A, tile 1.0 m) |
| `L0_Ceiling` | Level 0 | 0.6 × 1.2 m | `Office_Ceiling` (Office_Ceiling2x2_A, tile 1.2 m) |
| `Office_Ceiling` | Office | 0.6 × 1.2 m | `L0_Ceiling` (Ceiling_Fissured_A, tile 1.2 m) |

- Each pair computes the same unit theme for the same place. So a unit never splits between the two skins of a wall, or between two materials.
- Each material gains 28 lines (the keyword, own theme, unit and the Alt set).

### 2.5 Linings and ends (`FrontRoomsTransitionKit.cs`, 6.5 KB, new; hooks in `BuildEdge` and `BuildColumn`)

Drops line up with the 3 m cell lines. So a jamb, a soffit or a free wall end straddles a drop joint. In the band (|s| < W + 0.75 m):
- **Arches:** jambs and soffit are one 5 mm lining box in a non-drift copy of one finish (`Resources/Transitions/L0_Wallpaper_Lining.mat` or `Office_Wall_Lining.mat`).
  - The finish is drawn per edge with probability pOffice at the edge middle, so it is 50/50 on a border.
  - The opening keeps its size; the wall pieces stop 5 mm short.
- **Free wall ends:** the end cap is a 5 mm lining, chosen the same way per corner.
- **Columns** (and Office bulkheads) in the band take one lining finish, so no drop joint splits a column face.
- No beads. Door and window frames are unchanged. Lamps and the Office post grade are unchanged.

### 2.6 B0 (both fixes, drift form)

- **B0.1 corners.** In the drift band any wall face can show either finish. So the kit treats every corner in the band as a mixed corner:
  - a straight run: the two pieces meet on the cell line (no overlap in the post square);
  - an L: one arm runs over the post, the other stops at the post face. The owner is the arm whose end cap lies on the low side of the drop axis, so the cap takes the drop of the face it turns onto;
  - a T or a cross: the run meets on the line; the other arms stop at the post face.
  - Outside the band, corners keep today's 8 cm overlap (same finish, same pixels).
- **B0.2 grime band.** Each face of a height-border wall is built in its own side's height block, so its `_CeilingHeight` is its own ceiling. Both skins are built (0.08 m each) even when both sides share a material.

### 2.7 Iterations

| Run | When | What changed | Why |
|---|---|---|---|
| r1 | 10-03 17:14 | First full set. | — |
| dbg1, dbg2 | 10-03 17:22–17:37 | Debug renders: unit-index map and face-extent map (`-driftDebug 1/2`). | r1 was one-sided: the Level 0 floor in shot4 had no Office squares, and the shot1 room had one grey drop. The maps showed whole long faces deciding as one unit. |
| r2 | 10-03 17:39 | Walls decide per 0.75 m drop inside each face; only slivers (≤ 8 cm past a line, end caps) borrow the neighbouring drop. | Fix for r1. |
| r3 | 10-03 17:49 | Joint lines (1.5 mm paper, 2.5 mm carpet); pixel-coverage edge between two units. | r2's unit edges were razor-cut and aliased. A real butt joint shows a line. |
| final | 10-03 22:40 | Same code as r3, debug code compiled out. | Byte-identical to r3 on all four shots. |
| W 3, W 6 | 10-03, again 10-07 18:10 | Band width 3 and 6 m. | Optional tags. |
| patch r1 | 10-04 15:05 | Patches on floors, ceilings and walls (runs of drops). | Checkerboard on the line. |
| patch r2 | 10-04 15:30 | Walls back to one draw per drop. | Wall runs moved the line instead of hiding it. |
| rebuild | 10-07 17:19–18:55 | Clone rebuilt from the recovered code; every set re-rendered; stats re-measured. | The 10-05 wipe. |

---

## 3. Checks

| Check | Result |
|---|---|
| Harness unchanged | `Assets/Editor/Audit/FrontRoomsTransitionAudit.cs` md5 `76a624e3217630e637bdb381af7ca5b1`; `shots.json` unchanged. |
| Base | `git archive 2c4e50f` of Red's main (2026-10-03 10:41) over `W/proj_audit`'s Library. `FrontRoomsMapWorld.cs` md5 `e5204f9c7a7e07f9fa6d584915e6297e` (the frozen BEFORE base). |
| Baseline before any edit | `ShotsBatch -tag basecheck` (2026-10-07 16:59): all four JPGs byte-identical to `shot<k>_before.jpg`; all four plans identical. |
| Recovered code reproduces the verified set | Rebuilt `drift` set vs the 2026-10-03 final: shot1 and shot2 byte-identical; shot3 max 5/255, shot4 max 2/255, 0 pixels over 8/255 (GPU shader-compile noise). So the 2026-10-03 checks below hold for the rebuilt code. |
| Layout freeze | All four `-plan` files identical to `logs/shot<k>_plan.json` in every set: `drift`, `drift_w3`, `drift_w6`, `drift_patch`, `inert`. |
| Lamps | lit 84 / 82 / 83 / 88, shadowed 8 / 7 / 7 / 7 in every set: identical to BEFORE. |
| Hooks inert (`-noDrift`) | Re-run 2026-10-07 18:57 (`shots_inert`): plans and lamps identical; shot2 byte-identical; shot1 293 px and shot3 369 px differ by at most 2/255 and 4/255 (the six materials still carry the keyword, so the variant compiles with a different float order). shot4 differs on 15,458 px (> 8/255), all on the two corner-post faces of the far wall: BEFORE's z-fight stripe there (cut D) resolves the other way, because the keyworded materials change the draw order. Geometry is the same; a z-fight's winner depends on draw order (on 10-03 it happened to match). |
| No unit shows two finishes | By construction: one decision per unit, made from the unit centre; both skins and both materials compute the same hash. Checked on the unit-index debug renders (10-03) and on full-size crops of every frame (10-07). |
| No soft or noisy edges | The only mix is pixel-coverage anti-aliasing within half a pixel of a unit edge. |
| No mip seams | Alt maps sampled with `SAMPLE_TEXTURE2D_GRAD`, derivatives from outside the branch. No seam lines at unit edges in the full-size crops. |
| Pixels outside every band identical | The band (\|s\| < W + 0.75 on a theme surface) covers 96.2 / 96.9 / 96.6 / 99.7 % of the four frames, because each shot is aimed at a border. Outside it (mask dilated 16 px for JPEG blocks): shot3 max 2/255; shot4 has no pixels outside; shot1 and shot2 differ only on the new arch linings (a non-theme material, so not in the mask). Measured on 10-03 with the band-mask render (`-driftDebug 3`); valid for the rebuilt set (row above). |
| Nothing floats, clips or z-fights | Checked by eye on all 6 + 6 frames and the close frames. The near plane (0.06 m) touches nothing. |

---

## 4. Measurements

From my own editor method `Editor/Transitions/FrontRoomsDriftStats.cs`, not the harness. It builds through the harness's own private `Build`/`StandAt` (reflection), so scene, camera, post and lamps match the shots. BEFORE = the same clone with `-noDrift` (field, shader mix, B0 and linings all off).

### 4.1 Within 46 m of each eye (no frustum cull)

| Shot | Renderers (lining slots) | Submeshes | Materials | Triangles | Lit lights (shadowed) |
|---|---|---|---|---|---|
| shot1 BEFORE | 1,801 (0) | 2,083 | 42 | 175,766 | 84 (8) |
| shot1 DRIFT | 1,872 (72) | 2,154 | 44 | 180,974 | 84 (8) |
| shot2 BEFORE | 1,779 (0) | 2,036 | 42 | 162,760 | 82 (7) |
| shot2 DRIFT | 1,857 (64) | 2,114 | 44 | 167,908 | 82 (7) |
| shot3 BEFORE | 1,756 (0) | 2,038 | 42 | 178,600 | 83 (7) |
| shot3 DRIFT | 1,842 (77) | 2,124 | 44 | 183,736 | 83 (7) |
| shot4 BEFORE | 1,791 (0) | 2,064 | 42 | 172,554 | 88 (7) |
| shot4 DRIFT | 1,863 (70) | 2,136 | 44 | 177,714 | 88 (7) |

- Change: renderers +71 / +78 / +86 / +72 (+3.9 to +4.9 %); triangles +5,208 / +5,148 / +5,136 / +5,160 (+2.9 to +3.2 %); materials +2 (the two linings); lights 0.
- Drift itself adds no geometry. The change is B0 (two skins on height-border walls, split corners) and the 5 mm linings.
- The extra renderers are the linings: each lining finish is one more renderer per chunk block that holds one. Batch mode gives no draw count (`UnityStats` reads 0), so renderers are the honest proxy for draws before culling.
- The patch option has exactly the same counts (`drift_stats_patch`).

### 4.2 Frame time with and without `_FR_DRIFT`

Method: at each eye, 24 paired rounds of 10 frames, ABBA order, the keyword on and off on the six theme materials. Each frame is synchronised with a 1-pixel `ReadPixels`, so this is render + sync time at 1920 × 1080, 4× MSAA, on an Apple M3 Max. Batch mode exposes no GPU timer, so this is not pure GPU time.

| Run | Load average | shot1 | shot2 | shot3 | shot4 |
|---|---|---|---|---|---|
| 10-07 18:11 (stopped after shot2) | ~60 | median 16.48 vs 16.50 ms; paired Δ **+0.32 ms** (MAD 0.52; 15/24 rounds slower) | 17.67 vs 17.61 ms; Δ **−0.12 ms** (MAD 0.54; 11/24) | — | — |
| 10-07 18:24–18:29 | 590–660 | 70.06 vs 69.96; Δ +0.15 (MAD 7.44) | 60.86 vs 62.12; Δ +2.17 (MAD 8.46) | 52.95 vs 65.92; Δ −6.78 (MAD 10.32) | 77.82 vs 77.68; Δ +1.04 (MAD 10.95) |

- At low load, the cost is below what this method resolves: under ±0.5 ms on a 16.5 ms frame (under 3 %), and the sign is not stable.
- The full four-eye run happened under a load average of about 600 (other workflows), so its numbers are noise.
- Estimate for the shader alone: about 4 extra texture samples and about 25 ALU per band pixel (plan §5).
- A real number needs Play mode on an idle machine with a GPU timer (§11).

### 4.3 Units switched per chunk

Over the 38 distinct chunks built round the four eyes; CPU replica of the shader.

| Unit | Per chunk (all 38) | Per chunk with any | Share |
|---|---|---|---|
| Carpet squares (1,600 per chunk) | 74.2 | 91.0 (31 chunks) | 4.6 % |
| Ceiling tiles (800 per chunk) | 37.3 | 45.8 | 4.7 % |
| Paper drops (525.5 wall drops per chunk, both faces) | 32.3 | 39.5 | 6.1 % |
| Patch option: carpet squares | 75.9 | 96.1 (30 chunks) | 4.7 % |
| Patch option: ceiling tiles | 37.5 | 47.5 | 4.7 % |
| Patch option: paper drops | 32.3 | 40.9 | 6.1 % |

### 4.4 Shader variants

- One more local keyword (`_FR_DRIFT`), on the ForwardLit pass only. That doubles that pass's variant space; the shadow, depth and other passes are unchanged.
- `ShaderUtil.GetVariantCount(FrontRooms/Surface)`: 737,299 for all keyword combinations; 138,247 used by the open scene (with the change).
- It is a `shader_feature`, so a build compiles the drift variants only for the six drift materials.

### 4.5 Memory

- Field texture: 40 × 40 × 2 B = **3,200 B** per map window (one window per map).
- New texture sets: **0**. The Alt maps point at the counterpart's existing textures, which are already loaded.
- Face data: UV1 as float4 on shell meshes = +16 B per shell vertex (about +33 % on those vertex buffers, which hold position, normal, tangent and UV0 = 48 B today).
- Two lining materials, about 3.0 KB each on disk.

---

## 5. Draft contract (map-file diff against the clone base)

`FrontRoomsMapWorld.cs`: 15 hunks, +112 / −9 lines, 17 lines marked `// TRANSITION drift`. Full diff in the appendix and in `code/drift/mapworld_drift.diff`. In order:

1. **Chunk events** (contract item 1): `public static event Action<FrontRoomsMapWorld, GridCoord> ChunkBuilt, ChunkReleased` and `Action<FrontRoomsMapWorld> MapReleased`. Raised at the end of `BuildInto`, in the chunk drop path and at the top of `Release`. The RT track already queued the same events.
2. **Face data** (new contract item): `MeshBuilder.faceData` writes the face's extent round each vertex into UV1 (`Face` → `FaceData`, `ToMesh` → `SetUVs(1, …)`). `Get` turns it on for shell builders. UV1 is the lightmap channel; the map bakes no lightmaps today. If it ever does, move this to UV2.
3. **Ends** (B0 + linings, contract item 3): `EdgeEnds` and `Ends(...)` ask the kit, per edge, for the corner extents (B0.1), the per-side height blocks (B0.2), the end-cap linings and the arch lining. `BuildInto` passes them to `BuildEdge`.
4. **BuildEdge**: `Piece` uses the kit's extents and blocks and builds the end-cap linings. An arch in the band is built as wall pieces plus one lining box (jambs and soffit).
5. **BuildColumn**: an optional `finish` (the column lining in the band).
6. `world.ZoneOf` and `InStartArea` stay public (they already are).

Everything else (field, shader, materials, the kit's rules) is visual-owned.

---

## 6. New assets and files (clone only)

| File | Size | Owner |
|---|---|---|
| `Scripts/Rendering/Transitions/FrontRoomsThemeField.cs` | 9.8 KB | visual |
| `Scripts/Rendering/Transitions/FrontRoomsTransitionKit.cs` (drift version) | 6.5 KB | visual |
| `Resources/Transitions/L0_Wallpaper_Lining.mat` | 3.0 KB | visual |
| `Resources/Transitions/Office_Wall_Lining.mat` | 3.0 KB | visual |
| `Resources/Rendering/FrontRoomsSurface.shader` | +270 lines (320 → 590); the patch option adds +67 / −4 more | visual |
| six `Resources/Surfaces/*.mat` | +28 lines each | visual |
| `Editor/Transitions/FrontRoomsDriftPrepare.cs` | 4.4 KB | tool |
| `Editor/Transitions/FrontRoomsDriftStats.cs` | 15.9 KB | tool |
| `Editor/Transitions/FrontRoomsDriftPlan.cs` | 7.9 KB | tool |

- No textures, meshes or Blender modules.
- Copies of every code file and diff: `code/drift/` (`*.cs.txt`, `mapworld_drift.diff`, `surface_shader_drift.diff`, `materials_drift.diff`, `patch_option.diff`, `shots_extra.json`).

---

## 7. Change list (every clone change, and why)

1. `FrontRoomsMapWorld.cs`: the hooks in §5 (events, face data, ends, linings, column finish). Why: the field must know when chunks come and go; the shader needs face extents to keep slivers whole; B0 and the linings must happen where the wall pieces are cut.
2. `FrontRoomsSurface.shader`: the `_FR_DRIFT` path (§2.2) and the patch option (§2.3, patch clone only). Why: the unit choice must be per pixel and per unit, with no new geometry.
3. Six theme materials: keyword, own theme, unit and counterpart set (§2.4). Why: each material must be able to shade its counterpart.
4. Two lining materials: non-drift copies. Why: a jamb or end cap straddles a drop joint and must be one finish.
5. `FrontRoomsThemeField.cs`, `FrontRoomsTransitionKit.cs`: the field and the B0/lining rules (§2.1, §2.5, §2.6).
6. Tools: `FrontRoomsDriftPrepare` (materials), `FrontRoomsDriftStats` (counts, timing, units, variants), `FrontRoomsDriftPlan` (plan views and the extra shots in both states).
7. Not changed: the harness, `shots.json`, zones, maze, rooms, modules, columns, edge kinds, door positions, lamps, frames.

---

## 8. Deviations from the brief

1. **B0.1 straight runs meet on the cell line** instead of keeping their 0.08 m extensions. With drift, two collinear pieces of different material can show different finishes in the same pixels of the post square (z-fight). V5's B0 (now in main) made the same choice.
2. **B0.1 in the drift band applies to every corner**, not only to mixed-finish corners, because any face there can show either finish. The L owner is chosen by the drop axis, not by "Office outside", so the end cap always matches the face it turns onto.
3. **Field search:** a bounded 7 × 7 search instead of a BFS. Same result inside the ±6 m clamp.
4. **Extra inputs:** UV1 face data from the map (§5 item 2), `_FR_MapOrigin`, and a joint line at changed unit edges. The brief did not ask for these. Without them, end caps and wall-end slivers took the wrong drop, and the unit edges looked like a texture cut.
5. **Extra Alt properties** (`_AltBumpScale`, `_AltMacroDirt`, `_AltWetStrength`, `_AltFloorGrime`, `_AltCeilingGrime`), so a foreign unit is shaded exactly like its own material.
6. **Extra shots 5 and 6 and the plan views** were rendered by `FrontRoomsDriftPlan`. It calls the harness's own `Build`/`StandAt`/`Render` in both states in one Unity call (drift, then everything off). Same camera code; the film grain differs from a `ShotsBatch` run by up to 13/255 per pixel.
7. **Ceilings:** the "white 2×2 tiles" do not read white. The 2×2 and 2×4 tiles differ by 1.5 % in brightness (texture means 202 vs 199), so only the grid shows the swap.
8. **Patch option:** not in the brief. Added as an option (off by default) to answer the checkerboard issue.
9. **Frame time:** measured as render + sync time, not pure GPU time (§4.2).

---

## 9. Era check (1990)

- Carpet tiles (sold from 1973), broadloom loop pile, 2×2 and 2×4 lay-in tiles, vinyl-coated paper drops and painted drywall: all existed by 1990 (`02`: `rb_05`, `rb_06`, `rb_07`).
- Mixed dye lots, a cut-in patch of the wrong carpet and replacement ceiling tiles are ordinary in a 1990 building.
- No new print, text, logo or trademark. Nothing printed after 1990.

---

## 10. Research basis

- Fandom Level 0: the hallways "gradually transition" into Level 1 (`01` §1 point 5, source row "Level 0 The Lobby").
- Fandom Level 37: it leads to 37.1 "without one taking notice" (`01`, Level 37 row).
- Wikidot Level 0 Red Rooms: the paper peels to show the next colour, and the carpet changes as you get closer (`01`, P5).
- A24 production designer Danny Vermette: the deeper you go, the less things are "remembered" (`01`, P5).
- Kane Pixels, *Pitfalls* 5:55: the lower level is the same architecture with a new palette (`01` §3.3).
- `02`: 1990 carpet as broadloom, 6 ft rolls or modular tile (`rb_05`); 2×2 tile designs chosen per room (`rb_06`, `rb_07`); carpet patches of another dye lot where a partition moved.
- Media: no new media and no downloads. The wanted media for this pattern are already listed in `media_candidates.md` (*Pitfalls* lower level, priority 1).

---

## 11. What the real implementation needs

### 11.1 State of main (checked 2026-10-07 18:5x, main HEAD `6c6fe81`)

- Drift is **not** in main. No drift file, no `_FR_DRIFT`, no theme field.
- Main's `FrontRoomsTransitionKit.cs` (GUID `b215858f1c0014f87ae44abb9c680391`) is V5's kit. Codex promoted it with V5's B0 on 2026-10-03 (`8ef5b64`); `FrontRoomsMapWorld.cs` last changed on 2026-10-04 (`b5f381f`). Red keeps V5 on until the Figma pick (`RED_DECISIONS.md`).
- This pre-render is built on the frozen 10-03 base, not on main, so its BEFORE frames match. If Red picks Drift, it is re-rendered over main first (as `13_var_neck.md` §12 also asks).
- Re-checked 2026-10-07 23:3x (workflow retry, main HEAD `ee5c9bb`). Since `6c6fe81`, no map, transition-kit or surface-material file changed. Only `Resources/Surfaces/Textures/Wallpaper_Print_P.png` changed (`3ff05ee`, the print track). The clone, harness md5 `76a624e3…`, base check (4 of 4 byte-identical), `-plan` files (identical in all 6 sets) and lamp lines (84/82/83/88) were checked again. Nothing was re-rendered, because nothing that the drift frames depend on had changed.

### 11.2 Merge rules (codex audit MAP-5)

- Keep main's kit file and GUID `b215858f…`. Never copy the clone's kit (GUID `a8054719…`) over it.
- Merge drift's corner rule, linings and band test into main's kit as new methods. Rename drift's types so they do not collide (`Arm`, `CornerEnd` → for example `DriftCorner`).
- `FrontRoomsThemeField.cs` and the two lining materials are new paths; they keep the clone's GUIDs only if no main path exists.

### 11.3 B0 on chunk lines (codex audit MAP-2)

- Drift's `Corner` reads `Cache.Edge` for all four arms round a post, exactly like main's `PostPieceAt`. At a post on a chunk line, two of those arms belong to the next chunk.
- So drift has MAP-2 too: after a 30 s revisit shift of one chunk, its still-built neighbour keeps ends cut for the old interior (a notch, or a mixed overlap).
- The audit's minimal fix (chunk-line posts keep today's +8 cm reach) does **not** work for drift as it is. In the band, two overlapping coplanar pieces in the post square can borrow different drops through the sliver rule and z-fight.
- What drift needs: the audit's full fix (quarter posts, each built by the chunk that owns the cell it faces, from the chunk's own data plus the stable border edges). With it, drift's corner rule reads only stable data. This belongs in the one B0 rule the map chat picks.

### 11.4 Map chat (关卡设计), contract

1. `ChunkBuilt` / `ChunkReleased` / `MapReleased` events (shared with the RT track).
2. UV1 face extents on shell meshes (or an agreed channel): +16 B per shell vertex.
3. B0 merged on top of main's B0 from V5. Main already meets straight runs on the line and stops perpendicular arms. Drift adds: in the band, treat every corner as mixed, and pick the L owner by the drop axis. Plus the MAP-2 full fix (§11.3).
4. The lining hooks in `BuildEdge` (arch jambs and soffit, end caps) and `BuildColumn`.
5. Map tests: geometry changes only inside the band; collision is unchanged (linings sit inside the wall boxes' old footprint, 5 mm thick).

### 11.5 Visual chat (游戏视觉)

1. The shader path, the field class, the six material pairs and the two lining materials, through `FrontRoomsRenderSetup` (SurfaceDef rows) instead of the clone's prepare tool.
2. **Linings without new renderers.** Write the lining's forced finish into the face data (a fourth UV1 component: −1 own rule, 0 Level 0, 1 Office) and build linings with the theme material. That removes the two lining materials and the +71–86 renderers (§4.1). Important on WebGL, which is draw-bound.
3. An early-out: today every shell pixel of the six materials reads the field and runs the hash, even far from a border. Read the field once at the pixel and skip when |s| > W + 0.75 m; then out-of-band pixels cost one texture read.
4. A real GPU measurement on an idle machine (Play mode, GPU timer), with and without the keyword, at the four eyes.
5. Red's choice of W (3, 4.5 or 6; `v_drift_widths.jpg`) and of the patch option.
6. Ceilings: if Red wants the ceiling to read, give the Office 2×2 tile a whiter finish (a material change outside this variation).
7. WebGL: the same shader cost (GPU only; WebGL is draw-bound). With item 2, no new draws. Drop the joint line on WebGL if it aliases.

### 11.6 Others

- Wallpaper chat and 平面视觉: drift moves Level 0 paper (and its EGRESS ink) onto Office walls near a border and replaces some Level 0 drops with drywall. Ink placement must follow the unit choice (the CPU replica `FrontRoomsThemeField.UnitRand` gives it) or avoid the band.
- Sound chat: none.

---

## 12. Outputs in Red's project

`Documentation/research/level_transitions/`:
- `14_var_drift.md` (this file), `20_variation_drift.md`;
- `images/var_drift_shot1..4.jpg`, `images/var_drift_sheet.jpg`;
- `images/v_drift_shot1..6.jpg`, `images/v_drift_vs_before.jpg`, `images/v_drift_plan.jpg`, `images/v_drift_widths.jpg`;
- `images/v_drift_patch_shot1..6.jpg`, `images/v_drift_patch_vs_drift.jpg`, `images/v_drift_patch_plan.jpg`;
- `images/var_drift_close_*.jpg` (§1.1);
- `code/drift/` (code copies and diffs, 10-07 17:34).

Nothing else in Red's project was written, except the verification-log rows and section note in `Documentation/VERIFICATION_LOG.md` (§14). Unity never ran on Frontrooms3D.

---

## 13. Open issues

1. **Ceilings are weak.** Only the grid changes (1.5 % brightness difference). Accept it, or use a whiter Office tile.
2. **Checkerboard on the line.** Answered by the patch option (§2.3). Red picks per-unit or patches.
3. **Cells next to two borders** (shot1's eye cell) lose about a third of their paper. It fits "half remembered", but it changes the look of small Level 0 rooms between Office zones.
4. **EGRESS ink** on drifted drops (§11.6).
5. **Research rule.** Drift does not put the change on a built object. It pairs with V1 Frame or V3 Neck (plan §8: "later, on top of V1, for tier 4–5 chunks").
6. **Timing** is within noise; a Play-mode GPU timer on an idle machine is still needed.
7. **MAP-2** applies to drift's B0 too (§11.3).
8. **Main:** Codex's V5 + B0 are live in Red's game; Red keeps V5 on until the pick. Drift is not in main.

---

## 14. Verification images (FRONTROOMS · VISUAL VERIFICATION LOG)

Placed 2026-10-07 19:0x in section `2595:6093` (page `2099:76`); rows in `Documentation/VERIFICATION_LOG.md` §3. The section grew to 35 rows (8,280 × 43,480) and the cover VL000 was updated (134 checks, 427 images; N1 = VL043–046, 137–139).

| VL | Node | Check | Verdict | Images (slot order) |
|---|---|---|---|---|
| VL137 | `2795:6097` | Drift hides the zone line | FLAG (ceilings only change their grid) | `var_drift_sheet.jpg`, `v_drift_shot5.jpg`, `v_drift_shot6.jpg`, `v_drift_plan.jpg` |
| VL138 | `2795:6119` | Patches, not a checkerboard | WAIT-RED | `v_drift_patch_vs_drift.jpg`, `v_drift_shot6.jpg`, `v_drift_patch_shot6.jpg`, `v_drift_patch_plan.jpg` |
| VL139 | `2795:6135` | Posts and linings in the band | PASS | `var_drift_close_post.jpg`, `var_drift_close_arch.jpg`, `var_drift_close_postsplit.jpg`, `var_drift_close_wall.jpg` |

`v_drift_widths.jpg`, `v_drift_vs_before.jpg` and the other close frames are in `images/` but not on a slide (no separate question).

---

## 15. Logs and reproduction

W = `/Users/redwang/FrontRoomsVisualWork`; WK = `W/drift_work`. Run with `WK/run_wd.sh <limit_s> <clone> <Class.Method> <log> [args]` (one Unity per clone, `-quit`, `-buildTarget OSXUniversal`, watchdog).

| What | Log | Output |
|---|---|---|
| Base import | `WK/logs/import.log` | `W/proj_trans_drift` |
| Baseline | `WK/logs/basecheck.log` | `WK/shots_basecheck/` |
| Prepare (materials) | `WK/logs/prepare1.log`, `prepare2.log` | clone `Resources/` |
| **Final `drift`** | `WK/logs/drift_rebuild.log` | `WK/shots_drift/` |
| W 3, W 6 | `WK/logs/drift_w3.log`, `drift_w6.log` | `WK/shots_drift_w3/`, `WK/shots_drift_w6/` |
| Plan views, extras | `WK/logs/plan2.log` | `WK/drift_plan/` |
| Patch option shots, plan | `WK/logs/drift_patch.log`, `plan_patch.log` | `WK/shots_drift_patch/`, `WK/drift_plan_patch/` |
| Stats | `WK/logs/stats_drift.log` (low load, stopped after shot2), `stats_drift2.log`, `stats_nodrift.log`, `stats_patch.log` | `WK/drift_stats/`, `WK/drift_stats_patch/` |
| Inert (`-noDrift`) | `WK/logs/inert.log` | `WK/shots_inert/` |
| Close frames | `WK/logs/evidence.log`, `evidence_off.log` | `WK/evidence_drift/`, `WK/evidence_off/` |
| Sheets | `WK/py/sheet.py`, `vs_sheet.py`, `cols_sheet.py`, `plan_sheet.py` | `images/` |

- The 10-03 logs (r1–r3, debug maps, band mask) lived in the wiped scratchpad and are lost. Their results are recorded in §2.7 and §3.
- Render the final set: `run_wd.sh 1500 W/proj_trans_drift FrontRoomsTransitionAudit.ShotsBatch <log> -shots DOC/shots.json -out <dir> -tag drift -plan`. Add `-driftW 3` for W = 3, `-noDrift` for the inert check, or use `W/proj_trans_drift_patch` with `-driftPatch` for the patch option.

---

## Appendix: full diff of `FrontRoomsMapWorld.cs` (clone base → drift)

```diff
--- a/Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs (clone base, md5 e5204f9c)
+++ b/Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs (drift)
@@ -92,6 +92,11 @@
     /// <summary>Seconds from DoorUnlocked to the leaf starting to swing (the key turning in the lock). 0 swings at once.</summary>
     public float UnlockSwingDelay { get; set; }
 
+    // TRANSITION drift: chunk events (draft contract; also queued for the RT track). The theme field listens.
+    public static event Action<FrontRoomsMapWorld, GridCoord> ChunkBuilt;
+    public static event Action<FrontRoomsMapWorld, GridCoord> ChunkReleased;
+    public static event Action<FrontRoomsMapWorld> MapReleased;
+
     /// <summary>What lies between two side-by-side cells right now.</summary>
     public enum Passage { Open, Wall, ClosedDoor, Glass }
 
@@ -164,6 +169,10 @@
         public readonly List<Vector3> normals = new List<Vector3>();
         public readonly List<Vector2> uvs = new List<Vector2>();
         public readonly List<int> triangles = new List<int>();
+        // TRANSITION drift: face data (UV1) for FrontRooms/Surface's drift: the face's extent round
+        // each vertex in metres (vertical faces: along the drop axis; horizontal: X and Z), x - 1024 as a marker.
+        public bool faceData;
+        readonly List<Vector4> faces = new List<Vector4>();
 
         public void Box(Vector3 center, Vector3 size, Vector3 worldOffset, float repeat)
         {
@@ -184,6 +193,7 @@
             Corner(c + u * uHalf - v * vHalf, normal, worldOffset, repeat);
             Corner(c + u * uHalf + v * vHalf, normal, worldOffset, repeat);
             Corner(c - u * uHalf + v * vHalf, normal, worldOffset, repeat);
+            if (faceData) FaceData(c, normal, u * uHalf, v * vHalf); // TRANSITION drift
             triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
             triangles.Add(i); triangles.Add(i + 2); triangles.Add(i + 3);
         }
@@ -199,6 +209,23 @@
             uvs.Add(uv / repeat);
         }
 
+        // TRANSITION drift: same corner order as Face.
+        void FaceData(Vector3 c, Vector3 normal, Vector3 du, Vector3 dv)
+        {
+            var p0 = c - du - dv; var p1 = c + du - dv; var p2 = c + du + dv; var p3 = c - du + dv;
+            if (Mathf.Abs(normal.y) > .5f)
+            {
+                float x0 = Mathf.Min(Mathf.Min(p0.x, p1.x), Mathf.Min(p2.x, p3.x)), x1 = Mathf.Max(Mathf.Max(p0.x, p1.x), Mathf.Max(p2.x, p3.x));
+                float z0 = Mathf.Min(Mathf.Min(p0.z, p1.z), Mathf.Min(p2.z, p3.z)), z1 = Mathf.Max(Mathf.Max(p0.z, p1.z), Mathf.Max(p2.z, p3.z));
+                foreach (var p in new[] { p0, p1, p2, p3 }) faces.Add(new Vector4(x0 - p.x - 1024f, x1 - p.x, z0 - p.z, z1 - p.z));
+                return;
+            }
+            var t = Vector3.Cross(Vector3.up, normal).normalized;
+            float a0 = Vector3.Dot(p0, t), a1 = Vector3.Dot(p1, t), a2 = Vector3.Dot(p2, t), a3 = Vector3.Dot(p3, t);
+            float lo = Mathf.Min(Mathf.Min(a0, a1), Mathf.Min(a2, a3)), hi = Mathf.Max(Mathf.Max(a0, a1), Mathf.Max(a2, a3));
+            foreach (var a in new[] { a0, a1, a2, a3 }) faces.Add(new Vector4(lo - a - 1024f, hi - a, 0f, 0f));
+        }
+
         public Mesh ToMesh(string name)
         {
             var mesh = new Mesh { name = name };
@@ -206,6 +233,7 @@
             mesh.SetVertices(vertices);
             mesh.SetNormals(normals);
             mesh.SetUVs(0, uvs);
+            if (faceData && faces.Count == vertices.Count) mesh.SetUVs(1, faces); // TRANSITION drift
             mesh.SetTriangles(triangles, 0);
             mesh.RecalculateTangents();
             mesh.RecalculateBounds();
@@ -558,6 +586,7 @@
     /// </summary>
     public void Release()
     {
+        MapReleased?.Invoke(this); // TRANSITION drift
         foreach (var chunk in built.Values) FreeMeshes(chunk);
         foreach (var material in ownedMaterials) Kill(material);
         ownedMaterials.Clear();
@@ -722,6 +751,7 @@
         Unregister(chunk);
         built.Remove(coord);
         droppedAt[coord] = Time.time;
+        ChunkReleased?.Invoke(this, coord); // TRANSITION drift
     }
 
     /// <summary>Forget a chunk's doors, windows and shell colliders, and free its meshes and objects.</summary>
@@ -788,7 +818,8 @@
         for (var b = 0; b < builders.Length; b++) builders[b] = new Dictionary<Material, MeshBuilder>();
         MeshBuilder Get(int blockIndex, Material material)
         {
-            if (!builders[blockIndex].TryGetValue(material, out var builder)) builders[blockIndex][material] = builder = new MeshBuilder();
+            if (!builders[blockIndex].TryGetValue(material, out var builder))
+                builders[blockIndex][material] = builder = new MeshBuilder { faceData = FrontRoomsTransitionKit.Enabled }; // TRANSITION drift
             return builder;
         }
         var collision = new MeshBuilder();
@@ -832,14 +863,19 @@
             // A wall's ends reach half a thickness past its corner, except into the start area.
             // The start area's side walls also stop on the door line, where the stream room's end wall closes the corner.
             var startSide = InStartArea(cell) != InStartArea(east);
+            // TRANSITION drift: B0.1 corner extents, B0.2 per-side blocks and the linings, from the kit.
+            var eastEnds = Ends(cell, east, true, eastKind, BlockOf(i, j, eastHeight), BlockOf(i, j, height), BlockOf(i, j, MapGrid.CeilingHeight(eastZone.height)));
+            var northEnds = Ends(cell, north, false, northKind, BlockOf(i, j, northHeight), BlockOf(i, j, height), BlockOf(i, j, MapGrid.CeilingHeight(northZone.height)));
             BuildEdge(chunk, eastKind, cell, east, new Vector3((i + 1) * cs, 0f, j * cs), Vector3.forward,
                 eastHeight, eastA, eastB, BlockOf(i, j, eastHeight), Get, Solid, origin,
                 !BothInStartArea(new GridCoord(cell.x, cell.y - 1), new GridCoord(cell.x + 1, cell.y - 1)),
-                !BothInStartArea(new GridCoord(cell.x, cell.y + 1), new GridCoord(cell.x + 1, cell.y + 1)) && !(startSide && cell.y + 1 == startArea.yMax));
+                !BothInStartArea(new GridCoord(cell.x, cell.y + 1), new GridCoord(cell.x + 1, cell.y + 1)) && !(startSide && cell.y + 1 == startArea.yMax),
+                eastEnds);
             BuildEdge(chunk, northKind, cell, north, new Vector3(i * cs, 0f, (j + 1) * cs), Vector3.right,
                 northHeight, northA, northB, BlockOf(i, j, northHeight), Get, Solid, origin,
                 !BothInStartArea(new GridCoord(cell.x - 1, cell.y), new GridCoord(cell.x - 1, cell.y + 1)),
-                !BothInStartArea(new GridCoord(cell.x + 1, cell.y), new GridCoord(cell.x + 1, cell.y + 1)));
+                !BothInStartArea(new GridCoord(cell.x + 1, cell.y), new GridCoord(cell.x + 1, cell.y + 1)),
+                northEnds);
 
             if (data.pillar[i + j * (n + 1)] && !TouchesStartArea(cell.x, cell.y, cell.x, cell.y))
             {
@@ -847,7 +883,8 @@
                 var style = data.pillarStyle[i + j * (n + 1)];
                 if (TouchesStartArea(cell.x, cell.y, cell.x + 2, cell.y)) style &= unchecked((byte)~MapChunk.ColumnBeamEast);
                 if (TouchesStartArea(cell.x, cell.y, cell.x, cell.y + 2)) style &= unchecked((byte)~MapChunk.ColumnBeamNorth);
-                BuildColumn(style, new Vector3(i * cs, 0f, j * cs), height, zone.theme, theme, b, Get, Solid, origin);
+                BuildColumn(style, new Vector3(i * cs, 0f, j * cs), height, zone.theme, theme, b, Get, Solid, origin,
+                    FrontRoomsTransitionKit.ColumnLining(this, cell.x, cell.y)); // TRANSITION drift
             }
 
             if (!reserved) BuildFixture(chunk, cell, cellCenter, height, theme, data.lamp[index], data.tier);
@@ -882,6 +919,7 @@
         AddZoneGrades(chunk, data);
         Furnish(chunk, data);
         built[coord] = chunk;
+        ChunkBuilt?.Invoke(this, coord); // TRANSITION drift
     }
 
     static readonly int CeilingHeightId = Shader.PropertyToID("_CeilingHeight");
@@ -936,6 +974,31 @@
 
     bool BothInStartArea(GridCoord a, GridCoord b) => InStartArea(a) && InStartArea(b);
 
+    // TRANSITION drift: what the transition kit decided for one edge.
+    struct EdgeEnds
+    {
+        public bool active;
+        public float start, end;                 // metres past the start / end corner (+0.08, 0 or -0.08)
+        public int blockA, blockB;               // B0.2: each face in its own side's height block
+        public Material startLining, endLining;  // free wall ends in the band
+        public Material archLining;              // arch jambs and soffit in the band
+    }
+
+    // TRANSITION drift: ask the kit. a is the west or south cell; east: the edge runs along +Z.
+    EdgeEnds Ends(GridCoord a, GridCoord b, bool east, EdgeKind kind, int edgeBlock, int blockA, int blockB)
+    {
+        var e = new EdgeEnds { start = WallThickness * .5f, end = WallThickness * .5f, blockA = edgeBlock, blockB = edgeBlock };
+        if (!FrontRoomsTransitionKit.Enabled || kind == EdgeKind.Open) return e;
+        e.active = true;
+        if (!InStartArea(a) && !InStartArea(b)) { e.blockA = blockA; e.blockB = blockB; }
+        var s0 = east ? FrontRoomsTransitionKit.Corner(this, a.x + 1, a.y, FrontRoomsTransitionKit.Arm.N) : FrontRoomsTransitionKit.Corner(this, a.x, a.y + 1, FrontRoomsTransitionKit.Arm.E);
+        var s1 = east ? FrontRoomsTransitionKit.Corner(this, a.x + 1, a.y + 1, FrontRoomsTransitionKit.Arm.S) : FrontRoomsTransitionKit.Corner(this, a.x + 1, a.y + 1, FrontRoomsTransitionKit.Arm.W);
+        e.start = s0.extent; e.end = s1.extent;
+        e.startLining = s0.lining; e.endLining = s1.lining;
+        if (kind == EdgeKind.Arch) e.archLining = FrontRoomsTransitionKit.ArchLining(this, a, !east);
+        return e;
+    }
+
     /// <summary>
     /// One 3 m edge from <paramref name="start"/> along <paramref name="along"/>.
     /// Where the two sides are different themes the wall is split in two
@@ -944,16 +1007,43 @@
     /// </summary>
     void BuildEdge(BuiltChunk chunk, EdgeKind kind, GridCoord a, GridCoord b, Vector3 start, Vector3 along, float height,
         Material wallA, Material wallB, int blockIndex, BuilderFn get, SolidFn solid, Vector3 origin,
-        bool mayExtendStart = true, bool mayExtendEnd = true)
+        bool mayExtendStart = true, bool mayExtendEnd = true, EdgeEnds ends = default)
     {
         if (kind == EdgeKind.Open) return;
         var length = MapGrid.CellSize;
         // Positive "across" points from cell a into cell b.
         var across = new Vector3(along.z, 0f, along.x);
+        // TRANSITION drift: a lining box, the full wall thickness, in one non-drift finish.
+        void LiningBox(float from, float to, float bottom, float top, Material finish)
+        {
+            if (to - from < 1e-4f || top - bottom < 1e-4f) return;
+            var lc = start + along * ((from + to) * .5f) + Vector3.up * ((bottom + top) * .5f);
+            solid(blockIndex, finish, lc, Abs(along * (to - from) + across * WallThickness + Vector3.up * (top - bottom)), WallpaperRepeat);
+        }
         void Piece(float from, float to, float bottom, float top, bool extendStart, bool extendEnd)
         {
             if (from > 0f || !mayExtendStart) extendStart = false;
             if (to < length || !mayExtendEnd) extendEnd = false;
+            if (ends.active)
+            {
+                // TRANSITION drift: the kit's corner extents (B0.1), per-side blocks (B0.2), free-end linings.
+                var fs = from - (extendStart ? ends.start : 0f);
+                var ts = to + (extendEnd ? ends.end : 0f);
+                var lt = FrontRoomsTransitionKit.LiningThickness;
+                if (extendStart && ends.startLining != null) { LiningBox(fs, fs + lt, bottom, top, ends.startLining); fs += lt; }
+                if (extendEnd && ends.endLining != null) { LiningBox(ts - lt, ts, bottom, top, ends.endLining); ts -= lt; }
+                if (ts - fs < .05f || top - bottom < .05f) return;
+                var pc = start + along * ((fs + ts) * .5f) + Vector3.up * ((bottom + top) * .5f);
+                if (wallA == wallB && ends.blockA == ends.blockB)
+                {
+                    solid(ends.blockA, wallA, pc, Abs(along * (ts - fs) + across * WallThickness + Vector3.up * (top - bottom)), WallpaperRepeat);
+                    return;
+                }
+                var skin = Abs(along * (ts - fs) + across * (WallThickness * .5f) + Vector3.up * (top - bottom));
+                solid(ends.blockA, wallA, pc - across * (WallThickness * .25f), skin, WallpaperRepeat);
+                solid(ends.blockB, wallB, pc + across * (WallThickness * .25f), skin, WallpaperRepeat);
+                return;
+            }
             var f = from - (extendStart ? WallThickness * .5f : 0f);
             var t = to + (extendEnd ? WallThickness * .5f : 0f);
             if (t - f < .05f || top - bottom < .05f) return;
@@ -990,6 +1080,20 @@
             openingTop = WindowTop;
             sill = WindowSill;
         }
+        if (kind == EdgeKind.Arch && ends.archLining != null)
+        {
+            // TRANSITION drift: jambs and soffit as one lining in one finish (the opening keeps its size).
+            var lt = FrontRoomsTransitionKit.LiningThickness;
+            var l = c - width * .5f;
+            var r = c + width * .5f;
+            Piece(0f, l - lt, 0f, height, true, false);
+            Piece(r + lt, length, 0f, height, false, true);
+            Piece(l - lt, r + lt, openingTop + lt, height, false, false);
+            LiningBox(l - lt, l, 0f, openingTop + lt, ends.archLining);
+            LiningBox(r, r + lt, 0f, openingTop + lt, ends.archLining);
+            LiningBox(l, r, openingTop, openingTop + lt, ends.archLining);
+            return;
+        }
         Piece(0f, c - width * .5f, 0f, height, true, false);
         Piece(c + width * .5f, length, 0f, height, false, true);
         Piece(c - width * .5f, c + width * .5f, openingTop, height, false, false);
@@ -1061,19 +1165,22 @@
     /// bulkhead to the next column on the grid. The bulkhead is
     /// above every head, so it has no collision.
     /// </summary>
-    void BuildColumn(byte style, Vector3 at, float height, ZoneTheme zoneTheme, ThemeMaterials theme, int blockIndex, BuilderFn get, SolidFn solid, Vector3 origin)
+    void BuildColumn(byte style, Vector3 at, float height, ZoneTheme zoneTheme, ThemeMaterials theme, int blockIndex, BuilderFn get, SolidFn solid, Vector3 origin,
+        Material finish = null)
     {
+        // TRANSITION drift: in the band a column (and its bulkheads) is one lining finish, so no drop joint splits a face.
+        var columnWall = finish != null ? finish : theme.wall;
         var w = (style & MapChunk.ColumnLarge) != 0 ? ModuleUnits.ColumnLarge : ModuleUnits.ColumnSmall;
-        solid(blockIndex, theme.wall, at + Vector3.up * (height * .5f), new Vector3(w, height, w), WallpaperRepeat);
+        solid(blockIndex, columnWall, at + Vector3.up * (height * .5f), new Vector3(w, height, w), WallpaperRepeat);
         var cove = w + ModuleUnits.CoveProud * 2f;
         get(blockIndex, trim).Box(at + Vector3.up * (ModuleUnits.CoveHeight * .5f), new Vector3(cove, ModuleUnits.CoveHeight, cove), origin, 1f);
         if (zoneTheme != ZoneTheme.Office) return;
         var span = ModuleUnits.ColumnGrid - w;
         var y = height - ModuleUnits.BulkheadDepth * .5f;
         if ((style & MapChunk.ColumnBeamEast) != 0)
-            get(blockIndex, theme.wall).Box(at + new Vector3(ModuleUnits.ColumnGrid * .5f, y, 0f), new Vector3(span, ModuleUnits.BulkheadDepth, w), origin, WallpaperRepeat);
+            get(blockIndex, columnWall).Box(at + new Vector3(ModuleUnits.ColumnGrid * .5f, y, 0f), new Vector3(span, ModuleUnits.BulkheadDepth, w), origin, WallpaperRepeat);
         if ((style & MapChunk.ColumnBeamNorth) != 0)
-            get(blockIndex, theme.wall).Box(at + new Vector3(0f, y, ModuleUnits.ColumnGrid * .5f), new Vector3(w, ModuleUnits.BulkheadDepth, span), origin, WallpaperRepeat);
+            get(blockIndex, columnWall).Box(at + new Vector3(0f, y, ModuleUnits.ColumnGrid * .5f), new Vector3(w, ModuleUnits.BulkheadDepth, span), origin, WallpaperRepeat);
     }
 
     /// <summary>
```
