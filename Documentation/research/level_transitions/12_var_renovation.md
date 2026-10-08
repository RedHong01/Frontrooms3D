# Level transitions · 12 · V2 Renovation: pre-render report

Date: 2026-10-07 (finished after the 2026-10-05 reboot and two usage-limit stops).
Brief: `10_transition_plan.md` §3 V2 and §7.2. Protocol: §6. Short version: `20_variation_renovation.md`.
Everything was built and rendered in a private clone. Nothing in Red's project was edited except the documentation files listed in §14. No downloads. No Figma writes.

## 0. Summary for Red

- **The idea.** The Office is a 1990 fit-out that is still being built into Level 0. Near every border, the Level 0 wall steps from paper, to stripped board, to primer, to fresh Office paint. By the line both sides are paint, so the cut becomes a visible piece of history.
- **What you see.** Torn paper edges with the white base layer showing. Bare beaded reveals at arches. Paper cut round new door frames with a joint-compound halo. Carpet cut back to a glued slab strip. Ceiling tiles missing, pushed up or new. A stool, buckets, a paint tray, a drop cloth and stacks of carpet tiles against the walls.
- **The protocol held.** The BEFORE base was byte-identical (4/4 JPGs). The layout is frozen (4/4 plans identical). Lamps are unchanged (lit 84 / 82 / 83 / 88). The harness is unchanged (md5 `76a624e3…`).
- **The cost is high.** Within 46 m of each eye: triangles ×4.0 to ×4.5 (+398 k to +491 k), renderers +19 % to +24 %, materials +20, new textures 94 MB on desktop. Most triangles are the very fine torn and cut paper edges. §9 lists how to cut this by about 70 % before it ships.
- **Best frames:** `v_renovation_shot4.jpg` (wide), `v_renovation_shot6.jpg` (one wall, paper to paint), `v_renovation_shot1.jpg` (Red's doorway).

## 1. Images (all in `images/`)

| File | What |
|---|---|
| `var_renovation_shot1..4.jpg` = `v_renovation_shot1..4.jpg` | The four fixed shots. Harness JPGs, 1920 × 1080, same cameras as `shot<k>_before.jpg`. |
| `v_renovation_shot5.jpg` | Extra: the shot3 door seen from its Level 0 side (ragged paper cut, compound halo, slab strip). |
| `v_renovation_shot6.jpg` | Extra: the X = 768 corridor wall beside shot2. Paper, torn edge, stripped, primer, paint, then the Z = 588 line. |
| `v_renovation_shot7.jpg` | Extra: the X = 795 border arch close up from Level 0 (bare reveal, galvanised beads, compound). |
| `var_renovation_sheet.jpg` | Four rows, BEFORE \| RENOVATION, labelled. 1920 × 2360, q85. |
| `v_renovation_vs_before.jpg` | Seven rows, BEFORE \| RENOVATION: the four fixed shots and the three extras. 1920 × 4082, q85. The extras' BEFORE frames were rendered in the same clone with the kit off (`-renovationOff`, B0 off too). |

### What each fixed shot shows (brief §7.2 "must show")

| Shot | Must show | Result |
|---|---|---|
| shot1 | Drops stepping toward both arches; a torn edge; a bare beaded reveal; the slab strip before the Z = 609 arch; a missing tile over the eye cell; a stool against the left wall | All shown. Left wall: stripped board with torn paper remnants and islands (the intact drop sits just past the left frame edge). Far wall: primer, then fresh paint, with masking tape on the line. Both arches: compound halo and galvanised beads. Slab strip with chalk line and a loose tile. Two missing tiles at the top edge. Stool and a folded drop cloth against the left wall. |
| shot2 | Both corridor walls stepping down over the last 2.25 m before Z = 588; slab strip and loose tiles; a missing and a new tile overhead | Right wall: stripped (with paper islands), primer, paint. The left side is mostly openings (the X = 765 open edge and arch); its short wall pieces carry primer and paint, and the arch has clean beads. Slab strip across the corridor; two tiles near the eye; a missing tile with plenum and a new tile overhead. |
| shot3 | Through the door, the Level 0 paper cut ragged round the frame with a halo, and the slab strip just past the door | The slab strip just past the door is shown. **The Level 0 face of the door wall cannot be seen from this camera**: it faces away into Level 0. Extra shot5 shows it from the Level 0 side. See §10, deviation 6. |
| shot4 | The band on the Level 0 walls toward X = 798 and round the far arch | Shown. Right wall: paper, a ragged torn edge, stripped board with remnants, primer, paint, then the open edge. Far arch: band round it, stool, bucket, tiles, slab strip. |

## 2. Base, clone and protocol checks

- **Clone:** `W/proj_trans_renovation2`, made with `cp -Rc W/proj_trans` (the verified BEFORE base the drift stage rebuilt on 2026-10-07; see `W/proj_trans/BEFORE_BASE_README.txt`). MapWorld md5 `e5204f9c7a7e07f9fa6d584915e6297e`.
  - The name has a "2" because the interrupted attempt left `W/proj_trans_renovation`. That clone was built from `proj_audit` plus `git archive 2c4e50f` without a full re-import. Its shot4 baseline was **not** identical: 13,146 pixels differed by more than 12/255 at the X = 798 edge. So all its work (125 files) was ported onto the verified base, and nothing from it was trusted without a re-render.
- **Baseline:** `ShotsBatch -tag basecheck -plan` on the new clone before any edit. All four JPGs are byte-identical to `images/shot<k>_before.jpg` (`cmp`), all four plans are identical to `logs/shot<k>_plan.json`, and the lamps are lit 84 / 82 / 83 / 88.
- **Harness unchanged:** `Assets/Editor/Audit/FrontRoomsTransitionAudit.cs` md5 `76a624e3217630e637bdb381af7ca5b1`; `DOC/shots.json` md5 `6406e1998b925235090c94fc5175fe49`.
- **Layout frozen:** the final render (`-plan`) gives four plans identical to `logs/shot<k>_plan.json`.
- **Lamps unchanged:** the final lamp lines read lit 84 / 82 / 83 / 88, shadowed 8 / 7 / 7 / 7, the same as before.
- **No errors:** 0 `error CS`, 0 exceptions in every final log.
- **Iterations:** i1–i4 ran before the interruptions (frames in `W/reno_work/shots_i3`, `shots_i4`). This run made i5, i6 and i7 (final) in the new clone. Every iteration rendered the whole set of four shots.

## 3. What was built (numbers)

All numbers per chunk are averages over the 25 chunks (5 × 5) built round the shot1/4 and shot2/3 eyes, salt 130 (§10, deviation 2).

### 3.1 The band

- Covers Level 0 wall faces in the same wall run as a border crossing, within 3 paper drops (2.25 m) of the line, and the Level 0 face of a border wall within 2.25 m of a border opening.
- Drops are world-anchored at multiples of 0.75 m along the wall, so they match the paper's own seams. A 3 m cell holds 4 drops.
- Each band face is built as one 0.08 m skin per drop (boxes), each in its stage material. No overlay quads.
- Stages by drop distance n from the border:

| n | Stage | Material | Per chunk |
|---|---|---|---|
| 0 | Fresh Office paint, 0.05 m brushed cut-in at the ceiling and at corners and jambs | `Office_Wall_Fresh`, `Office_Wall_CutIn` | 1.5–1.7 m of wall |
| 1 | Primer over taped drywall (tape every 1.2 m, screw spots, compound flashing) | `Wall_Primer` | 1.3–1.4 m |
| 2 | Stripped board (face paper, grey-brown backing remnants, amber paste streaks, gouges) | `Wall_Stripped` | 1.9–2.6 m |
| 3+ | Paper intact | `L0_Wallpaper` (unchanged) | — |

- **Band total:** 4.7–5.8 m of wall face per chunk, plus 2.6–3.0 m of compound halo round border openings.
- **Variation:** one pattern per border crossing, so every face round one opening shows the same state of work: 60 % full (paint, primer, stripped), 25 % stripped only (n = 0..1), 15 % one stripped drop.
- **Joins between stages:** the primer roller stops 10–70 mm short or over the stripped drop with a soft ragged edge (1.4–1.6 per chunk). Beige masking tape sits on the paint | primer line (3.8–4.2 pieces per chunk).

### 3.2 Torn edge and paper remnants

- On every paper | stripped drop line, a real mesh, not alpha: the print runs 3 mm back over the intact drop and 8–25 mm out over the stripped one, with 2–3 tongues reaching 40–150 mm further.
- A white base-layer lip, 2.5–8 mm wide, feathered, round every torn contour.
- The top 0.10 m of the seam curls 15–25 mm off the wall, with its white back showing.
- Remnants the scraper has not reached: a ragged patch at the floor and a strip under the ceiling (often most of the drop wide). Strips peeled from the ceiling remnant hang off the wall with a rolled foot.
- Islands of print still stuck mid-drop: 1–3 per stripped drop, each with its torn rim.
- **Counts per chunk:** 1.8–2.3 tear lines (4.8–5.7 m), 2.4–2.6 remnants, 4.3–6.0 islands, 0.64–0.68 hanging strips. Floor scraps 3.4–3.8.
- The print on every paper piece is the wall's own world-projected material, so the pattern continues exactly. Hanging strips use `Paper_Peel` (the same paper with mesh UVs taken from where the strip lay).

### 3.3 Openings

- **Border arches (0.40 per chunk):** a bare drywall reveal: 3 mm `Wall_Primer` liners over both jambs and the soffit. Galvanised corner beads on the Level 0 arrises: 30 mm flanges, 3.5 mm proud, 3.5 mm round nose (`Prop_Aluminium`). A 60–100 mm compound halo round the arch on the Level 0 face, feathered 4–40 mm onto whatever stage is next to it.
- **Border doors and windows:** on the Level 0 face round the casing, a 0.08–0.20 m halo of joint compound. The paper is cut ragged round it: 3–45 mm over the compound, 2–4 tongues, the white lip, and 1–3 knife smears of compound per side onto the paper. The Office face is finished.

### 3.4 Floor

- In each Level 0 cell at a border crossing (open, arch or door), the loop pile ends on a straight cut 0.6 m before the line, across the opening or corridor. The floor slab is built in pieces round the strip; collision keeps the full slab.
- The strip is `Slab_Adhesive` 7 mm below the pile: grey concrete, amber trowel ridges, and a blue chalk line 0.14–0.28 m from the line.
- A 5 mm frayed edge mesh finishes the cut carpet (9.0 m per chunk).
- Office tiles run to the line (unchanged).
- Loose Office tiles (0.6 × 0.6 × 0.007, `Office_CarpetTile_Loose`): at most one lies on the strip, turned 2–12°; 2–4 more are stacked against a wall beside it. 1.3 stacks and 4.4–4.5 tiles per chunk.
- **Slab strip area:** 3.3 m² per chunk.

### 3.5 Ceiling (each Level 0 crossing cell)

- One 0.6 × 1.2 tile is out. The cell's render slab is built in pieces round the hole. Over it, a dark plenum box 0.25 m deep with T-bar webs, a hint of galvanised duct and a sagging cable.
- One tile is pushed up askew on one long flange, 30–70 mm, with a dark gap.
- Two new 2 × 2 tiles (`Office_Ceiling_New`, 6 % brighter) sit in the old 2 × 4 grid in the 0.6 m nearest the line.
- Per chunk: 2.8–2.9 tiles out, 1.6–1.8 askew, 4.8–5.0 new.

### 3.6 Props (render-only, no colliders)

- Against Level 0 band faces, at least 1.0 m clear of every opening, 1–2 per run.
- The kit picks by the stage of the drop: scraping gear at stripped drops (step stool, bucket, tile box), paint gear where primer has started (paint tray with roller, bucket, drop cloth).
- Iteration 7: wide gear (the tray is 0.54 m) rarely fitted in one 0.75 m drop next to the 1.0 m keep-out, so a prop that does not fit slides along the run to the nearest clear spot inside the band (3.1–3.4 slides per chunk).
- **Per chunk:** 2.1–2.2 props. In the shot1/4 build: 17 buckets, 14 stools, 11 paint trays, 9 tile boxes, 5 drop cloths. Also paper scraps on the floor and tape pieces.
- Kits: `Kit_StepStool` (existing), and four new modules `trans_bucket.py`, `trans_paint_tray.py`, `trans_drop_cloth.py`, `trans_tile_box.py` (§7).

### 3.7 B0 (in every variation)

- **B0.1 corner ownership.** At a post where faces of different finish meet, no two render pieces share the 0.16 m post square. A collinear pair meets at the centre and every other arm stops at the post face. At an L, the arm whose outside face is Office (else the more finished one) owns the post with one full box in its outside finish. Same-finish corners are unchanged. 0.6–0.9 post boxes per chunk. Collision is exactly as before.
- **B0.2 grime band per side.** Each face of a height-border wall is built in its own side's height block, so `_CeilingHeight` is its own ceiling. Two 0.08 m skins wherever the two faces differ.

### 3.8 Unchanged

Zones, maze, rooms, modules, columns, edge kinds, door positions, collision, lamps, the Office side and the title corridor.

## 4. Research basis

- `01_ip_research.md` **P5** (decay or gradient band): Wikidot's Red Rooms, where the paper peels to show the next colour; Kane Pitfalls 11:30 (older, stained paper).
- `01_ip_research.md` **P10** (built insert): Kane Pitfalls 1:40–1:55, Async's outpost built inside Level 0; the stud cavity in Everything Must Go 9:32–10:32.
- The original 2002 photos (`Research/week02/ip-research/stills/ir01_dsc00161_20020612_082113.jpg`, `ir01_dsc00159_…`): a store interior under construction. Credited in `Research/week02/ip-research/SOURCES.md`.
- The A24 set still `Research/week02/kit-references/a24/a24_trailer_0115_blue_painters_tape.jpg`: tape and a half-finished wall. Credited in `Research/week02/kit-references/SOURCES.md`. It is also the source of the existing `Kit_DoorwayStuds`.
- `02_real_buildings.md` §7 (paint over paper, GA-214 levels of finish from 1990, compound flashing, moved partitions leave floor and ceiling scars) and **Rule 6** ("Change leaves evidence").
- No new media were downloaded. No new media candidates were needed.

## 5. Draft contract (the map-file diff)

The full diff against the clone base is in `code/renovation/mapworld_renovation_hooks.diff` and in Appendix A. It is 119 added and 9 removed lines in `FrontRoomsMapWorld.cs`, 14 places marked `// TRANSITION renovation`.

| # | Hook | What it does | Contract item |
|---|---|---|---|
| H1 | `MeshBuilder.Raw(v, n, uv, t)` | Appends kit meshes (torn paper, beads, tiles) to a block's builder | Splitting band faces into drops; kit geometry in the map's blocks |
| H2 | `TransitionKit()` + 2 fields | One kit context per cache: theme, ceiling, edge kind, arch opening, start-area exclusion, seed, materials | The face-run hook's inputs |
| H3 | `kit.Begin(...)`, `Collide()` in `BuildChunk` | Per-chunk reset; a collision-box helper | — |
| H4 | Floor/ceiling in a crossing cell | `CrossingCell()` builds the render floor and ceiling in pieces (slab strip, tile hole); collision keeps the full slabs | Each crossing's floor and ceiling cells; **the ceiling-slab hole for one tile** |
| H5 | Per-side blocks (B0.2) | `eastBlockA/B`, `northBlockA/B` from each side's own ceiling | B0.2 |
| H6 | `BuildEdge(..., kit, blockA, blockB, collide)` | The edge descriptor: ends, along, across (normal), height, opening lo/hi/top/sill, both materials and blocks, extension flags | **The face-run hook** (ends, normal, height, distance along the run) |
| H7 | `Piece(...)` | Collision exactly as before; render from `WallPiece()`: B0.1 corners, B0.2 blocks, drops | Splitting band faces into drops; B0.1 |
| H8 | After the cell loop | Kit boxes and meshes into the blocks; block −1 (loose tiles, beads, ducts, tape, scraps) into a `Renovation` child with one renderer per material; props through `FrontRoomsKitLibrary.Spawn(..., colliders: false)` | Render-only props |
| H9 | Wall edges and openings | `ArchReveal()` on border arches; `EdgeExtras()` (props, scraps, tape) per edge | Dress keep-clear for the props |

**Contract items the real version still needs from the map chat** (not in the prototype hooks):
1. **Run hash and chunk tier** passed to the kit. The prototype hashes each crossing itself (`CrossingHash`) and ignores the tier. The real version should take both from the map, so a tier can tune how often a run is mid-renovation (the plan suggests 25–35 % of border runs, not 100 %).
2. **Dress API keep-clear.** The props only avoid each other and the kit's own 1.0 m opening keep-out. The map's Dress modules do not know about them. The contract needs a keep-clear rectangle per prop (0.04 m margin round the footprint) that Dress respects, and the reverse.
3. **Neighbour reads across chunk lines.** `FaceOf` and `CornerAt` read the edge kinds and themes of cells one step past the piece. On a chunk line this is the next chunk's data. The codex audit's MAP-2 shows that main's B0 has the same problem after a revisit shift. The real hook should hand the kit only this chunk's own cells (or follow MAP-2's minimal fix: keep today's reach on chunk-line posts).
4. **Collision unchanged** is a requirement, not a hook: the prototype rebuilds the same collision boxes the base builds.

## 6. Change list (clone only, every change and why)

| File | Change | Why |
|---|---|---|
| `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs` | Hooks H1–H9 (§5), +119 / −9 lines | The draft contract. Clone only. |
| `Assets/Scripts/Rendering/Transitions/FrontRoomsTransitionKit.cs` (new, 1,841 lines, 104.7 KB) | The whole variation: band, stages, torn edges, remnants, islands, hanging strips, cut lines, smears, arch reveals and beads, crossing floors and ceilings, props, scraps, tape, B0.1 | Visual-owned stand-in for main's `FrontRoomsTransitionKit`. |
| `Assets/Editor/Rendering/FrontRoomsTransitionAssets.cs` (new, 343 lines) | `Setup` (materials from the new textures, FBX re-import, texture report), `BandMap`, `Stats`, `SetupAndBand`, `BandAndStats` | Asset prep and the measurements in §8–9. Tools only. |
| `Tools/lookdev/gen_transition_surfaces.py` (new, 502 lines) | The 10 texture sets (§7) | Authored textures, not shader noise. |
| `Tools/lookdev/cc0_src/` | Copied from main's `Tools/lookdev/cc0_src` (the git base has none) | The generator's CC0 inputs. |
| `Tools/Blender/frontrooms_kit/assets/trans_*.py` (4 new) | Bucket, paint tray, drop cloth, tile box | New props. |
| `Assets/Resources/Props/Models/Kit_Trans*.fbx/.json` (4 new) | Built by Blender, imported by `FrontRoomsKitImporter` | — |
| `Assets/Resources/Surfaces/*.mat` (19 new) | `Office_Wall_Fresh`, `Office_Wall_CutIn`, `Wall_Primer`, `Wall_Stripped`, `Wall_Compound`, `Paper_Backing`, `Paper_Scrap`, `Paper_Peel`, `Slab_Adhesive`, `Office_Ceiling_New`, `L0_CeilingTile_Loose`, `Office_CarpetTile_Loose`, `Plenum_Dark`, `Chalk_Blue`, `Prop_Compound`, `Prop_PaintWet`, `Prop_RollerNap`, `Prop_Canvas`, `Prop_TapeBeige` | All `FrontRooms/Surface`; world-projected unless mesh-UV. |
| `Assets/Resources/Surfaces/Textures/*` (30 new PNGs) | §7 | — |

**Fixes made in this run (i5–i7), and why:**
1. **i5:** a `uint`→`int` cast that stopped the interrupted attempt from compiling.
2. **i6, NaN:** the knife-smear envelope used `Pow(sin(πs/len), 0.6)`; at the last sample `sin(π)` is slightly negative, so the vertex became NaN. The NaN broke the bounds of that block's whole compound renderer, and every door halo in it lit about 40 % darker (`shot5` 165 → 105 luma). Fixed with a clamp, plus a non-finite guard in every mesh writer (0 vertices dropped in the final build).
3. **i6, bead sawtooth:** the bead flanges stood 1.6 mm proud. At about 5.8 km from the origin (capture root 4992 m) the float32 view transform loses 1–2 mm, so the flange sawtoothed against the reveal liner and the face (visible on the shot2 left jamb). Flanges and nose are now 3.5 mm.
4. **i6–i7, props:** paint gear only appeared once in 46 props, because the tray never fitted next to the keep-out. Primer drops now pick a tray 40 % / bucket 25 % / drop cloth 15 % / tile box 10 % / stool 10 %, and a prop that does not fit slides to the nearest clear spot in the band.
5. **Tool switch:** `-renovationOff` on the command line turns the kit off (BEFORE geometry, B0 off), used only for the extras' BEFORE frames.

## 7. New assets and sizes

### Textures (2048² unless noted; A, N, S each; `/usr/bin/python3`, in the style of `gen_surfaces.py`)

| Set | Size | Tile (m, divides 192) | Made from |
|---|---|---|---|
| `Wall_FreshPaint` | 2048 | 1.2 | Office paint +4 % value, roller-lap normal at 0.23 m |
| `Wall_CutIn` | 1024 | 1.2 | brushed cut-in |
| `Wall_Primer` | 2048 | 2.4 | warm white, tape every 1.2 m (4 ft boards, rounded so the tile divides 192 m), screw spots every 0.30 m on 0.40 m studs, flashing |
| `Wall_Stripped` | 2048 | 2.4 | face paper, backing remnants, paste streaks (Leaking001), gouges, grime (SurfaceImperfections013) |
| `Wall_Compound` | 1024 | 1.2 | compound with Smear007 and SurfaceImperfections007 |
| `Slab_Adhesive` | 2048 | 2.4 | concrete (SurfaceImperfections015 stains), 3 mm notch trowel ridges |
| `Paper_Backing` | 1024 | 0.6 | the paper's white base layer |
| `Prop_Canvas` | 2048 | 1.0 (mesh UV) | Poly Haven rough_linen + primer and paint drips (SurfaceImperfections001) |
| `Prop_TapeBeige` | 512 | 0.15 (mesh UV) | ambientCG Tape005, recoloured to beige crepe |
| `Prop_RollerNap` | 512 | 0.15 (mesh UV) | roller nap with paint |

- Disk: 30 PNGs, 70.4 MB (sources only; they compress on import).
- **CC0 sources used** (all already on disk in `Tools/lookdev/cc0_src`, CC0): ambientCG Leaking001, Smear007, SurfaceImperfections001/007/013/015, Tape005; Poly Haven rough_linen. Reused existing materials: `Prop_Cardboard` (from CardboardSet001), `Prop_Aluminium`, `Prop_PlasticWhite`, `Prop_PlasticBlack`, `Painted_Metal`, `Prop_PlasticGrey`.
- Not used, though listed in the brief: Leaking006, beige_wall_001, polystyrene.

### Meshes

| Kit | LOD0 tris | LOD1 tris | Size (m) | FBX |
|---|---|---|---|---|
| `Kit_TransBucket` (5-gallon pail, lid, scraper, wire bail, no label) | 4,386 | 1,972 | 0.32 × 0.40 × 0.33 | 302 KB |
| `Kit_TransPaintTray` (steel tray, roller frame and nap, wet paint) | 1,860 | 1,018 | 0.54 × 0.20 × 0.30 | 146 KB |
| `Kit_TransDropCloth` (folded canvas, paint spots) | 1,480 | 740 | 0.47 × 0.12 × 0.62 | 143 KB |
| `Kit_TransTileBox` (open carton of carpet tiles, tape) | 876 | 438 | 0.97 × 0.51 × 0.90 (flaps open) | 80 KB |

Blender modules: `trans_bucket.py` 4.8 KB, `trans_paint_tray.py` 5.1 KB, `trans_drop_cloth.py` 2.8 KB, `trans_tile_box.py` 3.7 KB (copies in `code/renovation/`).

Everything else (torn edges, remnants, islands, cut lines, beads, tiles, plenum, scraps, tape) is built at run time by the kit, so it has no asset file.

## 8. Measured counts within 46 m of each eye

Method: `FrontRoomsTransitionAssets.Stats` in the clone (not the harness). It builds the map the way the harness does (`BuildForCapture`, root 4992, seed 516574485, 5 × 5 chunks round the eye), once with the kit off and once on, and counts enabled renderers whose bounds come within 46 m of the eye (LOD0 only), their materials and triangles, and enabled lights within 46 m.

| Shot | BEFORE renderers / materials / tris | RENOVATION renderers / materials / tris | Δ tris | Kit-only renderers / tris | Props | Lit / shadowed lights |
|---|---|---|---|---|---|---|
| shot1 | 1,746 / 42 / 143,262 | 2,103 / 62 / 627,412 | +484,150 (×4.38) | 95 / 101,250 | 30 | 84 / 8 → 84 / 8 |
| shot2 | 1,729 / 42 / 133,654 | 2,065 / 62 / 532,126 | +398,472 (×3.98) | 102 / 101,942 | 32 | 82 / 7 → 82 / 7 |
| shot3 | 1,702 / 42 / 144,606 | 2,102 / 62 / 600,732 | +456,126 (×4.15) | 131 / 128,804 | 42 | 83 / 7 → 83 / 7 |
| shot4 | 1,739 / 42 / 141,060 | 2,096 / 62 / 631,820 | +490,760 (×4.48) | 96 / 102,126 | 30 | 88 / 7 → 88 / 7 |

- **Draws:** every renderer here has one material, so renderers ≈ draws before culling: +336 to +400 (+19 % to +24 %). 95–131 of them are props and loose kit pieces (the `Renovation` children: one renderer per material per chunk for block −1 pieces, plus the props' LOD0 parts). The rest are block renderers for the new stage materials. Props and kit meshes also cast shadows into the 7–8 shadowed lights' passes.
- **Where the triangles go:** measured, 101–129 k of the added triangles are props and loose kit pieces. The other 297–390 k sit inside the wall, floor and ceiling blocks. Most of that is the paper geometry: the cut lines round door and window halos and the torn seams, sampled every 4.5 mm with a print strip and a lip strip (about 890 triangles per metre of edge), then the carpet fray (2 triangles per 4 mm, 9 m per chunk).
- **Lights:** no change.
- Logs: `W/reno_work2/logs/stats_i7.log`; table `W/reno_work2/stats_i7/renovation_stats.txt`.

## 9. Texture memory added

- **Desktop: 94.0 MB** (GPU, BC7 at 1 byte per texel, full mip chains): five 2048² sets at 16 MB each, three 1024² sets at 4 MB, two 512² sets at 1 MB. Measured from the imported textures by `TextureReport()`.
- **WebGL estimate: 34.0 MB** (every set capped at 1024², 1 byte per texel with mips).
- This is over the plan's 50–65 MB. Cheap cut for the real version: `Prop_Canvas` and `Slab_Adhesive` to 1024 (−24 MB; the cloth is small on screen and the slab strip is 0.6 m wide) → about 70 MB.
- **Triangle cuts for the real version (estimates, not measured):** cut lines and tears at 12 mm steps with the fine octave moved into a `Paper_TornEdge` alpha atlas (the brief's 2048 × 512 option), fray only within 10 m, and merge block −1 pieces into the blocks. Together about −70 % of the added triangles and about −90 extra renderers. WebGL drops tear meshes and props past 10 m (WebGL-only gate).

## 10. Deviations from the brief

1. **Clone name:** `proj_trans_renovation2` (rebased on the verified `W/proj_trans`; §2).
2. **Staging salt.** `RunSalt = 130` is one of 54 salts in 0–399 where the four fixed crossings (Z 609, X 795, Z 588, X 798) all draw the 60 % "full" pattern, the two X = 765 crossings draw "stripped only", and a stool lands on shot1's left wall. Every other crossing keeps its own draw. The real kit uses salt 0. Per-chunk numbers in §3 use salt 130.
3. **Run hash per crossing, not per run.** Every face round one crossing shares one pattern, so a doorway never shows "painted" on one side and "only stripped" on the other.
4. **Torn edge as run-time mesh.** It is real geometry built by the kit (no alpha, no shader noise), not a Blender `trans_paper_tear.py` mesh and not an alpha atlas. The white lip is 2.5–8 mm, not 0.3 mm: 0.3 mm is below one pixel beyond about 0.4 m.
5. **Hanging strips:** 0.64–0.68 per chunk (about one per three tear lines), fewer than "1–2 per run".
6. **shot3** cannot show the Level 0 face of the door wall (it faces away from the camera). Extra shot5 shows it.
7. **Props:** paint gear slides along the run (§3.6). All props stand at drops n = 1–2 because n = 0 is always inside the 1.0 m keep-out.
8. **Not built:** the raw wall end (cut E, "a paper lip over the gypsum edge", 0.17 per chunk) and the use of chunk tier.
9. **Band on every border run.** The plan recommends 25–35 % of runs. The prototype dresses all of them so the shots show it.
10. **Costs above the estimates:** triangles (§8) and texture memory (§9).
11. **Blue painter's tape not used.** Its pre-1990 date is not confirmed, so all tape is beige crepe masking tape.

## 11. Era check (1990)

- Drywall with paper joint tape and compound, galvanised corner bead, PVA/latex primer, roller and tray, canvas drop cloth, wooden step stool, chalk line, notched-trowel carpet adhesive, carpet tiles (from 1973), 2 × 4 and 2 × 2 lay-in ceilings, beige crepe masking tape: all in use well before 1990.
- No trademarks or labels: the pail is plain white, the tray and roller are plain, the carton carries only tape. No printed dates.
- The paper scraps and remnants show the existing Level 0 print, unchanged.
- **Note for the wallpaper chat:** stripped, primed and painted drops carry no wallpaper print and no EGRESS ink. Ink placed within 2.25 m of a border crossing would be removed with the paper.

## 12. Main state and the codex audit

- Main has moved since the clone base (`2c4e50f`): Codex promoted B0 and V5 (light lead) into main's `FrontRoomsMapWorld.cs` on 2026-10-03 (on by default; Red: keep V5 on until the pick). Main also has single-acting doors.
- `codex_audit/20_findings.md` does not exist. `10_review_map-edits.md` has two findings that apply to this variation:
  - **MAP-5:** main's `FrontRoomsTransitionKit` (GUID `b215858f…`) is V5's; every variation clone defines a different class with that name. If Red picks Renovation, merge into main's file and keep its GUID; never copy this clone's kit over it.
  - **MAP-2:** B0.1 reads the neighbour chunk's cells at chunk-line posts. This kit's B0.1 and its face runs do the same (§5, item 3).
- Nothing here was merged. A merge would be a separate stage over current main.

## 13. Verification frames (for the Figma VERIFICATION LOG)

This stage had no Figma writes. These frames are the verification set for a VL row when the Figma stage runs: `v_renovation_shot1..7.jpg`, `var_renovation_sheet.jpg`, `v_renovation_vs_before.jpg` (largest first: the vs-before sheet).

## 14. Files written in Red's project

- `Documentation/research/level_transitions/12_var_renovation.md` (this file), `20_variation_renovation.md`.
- `images/var_renovation_shot1..4.jpg`, `var_renovation_sheet.jpg`, `v_renovation_shot1..7.jpg`, `v_renovation_vs_before.jpg`.
- `code/renovation/`: `FrontRoomsTransitionKit.cs.txt`, `FrontRoomsTransitionAssets.cs.txt`, `mapworld_renovation_hooks.diff`, `gen_transition_surfaces.py.txt`, `trans_*.py.txt` (4), `shots_extra.json`. Text copies only, so the work survives if the clone is lost again (as on 2026-10-05).

## 15. Logs and paths

- Clone: `/Users/redwang/FrontRoomsVisualWork/proj_trans_renovation2`. Work folder: `/Users/redwang/FrontRoomsVisualWork/reno_work2`.
- Baseline: `reno_work2/logs/basecheck.log`, frames `reno_work2/shots_basecheck/`.
- Asset prep: `reno_work2/logs/setup_i5.log` (materials, FBX re-import, texture report).
- Final renders: `reno_work2/logs/render_i7.log` (four shots, `-plan`), `render_i7x.log` (extras), `render_i7x_off.log` (extras BEFORE); frames `reno_work2/shots_i7/`, `shots_i7x/`, `shots_i7x_off/`.
- Stats and band maps: `reno_work2/logs/stats_i7.log`, `reno_work2/stats_i7/`.
- Earlier iterations: `reno_work2/shots_i5`, `shots_i6` (this run); `W/reno_work/shots_i3`, `shots_i4` (before the interruption, old clone).
- Interrupted attempt and recovery material: `W/proj_trans_renovation` (old clone, not used for results), `W/reno_work`, `W/reno_recover`.
- Unity runner: `reno_work2/run_unity.sh` (always `-buildTarget OSXUniversal`, never two Unity processes on one clone).

## Appendix A. `FrontRoomsMapWorld.cs` diff against the clone base

See `code/renovation/mapworld_renovation_hooks.diff` (223 lines, unified diff, `a/` = the BEFORE base, `b/` = the clone).

```diff
--- a/Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs	2026-10-03 10:41:11
+++ b/Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs	2026-10-07 16:53:37
@@ -197,6 +197,14 @@
             // and continues across chunk borders.
             var uv = Mathf.Abs(normal.x) > .5f ? new Vector2(w.z, w.y) : Mathf.Abs(normal.y) > .5f ? new Vector2(w.x, w.z) : new Vector2(w.x, w.y);
             uvs.Add(uv / repeat);
+        }
+
+        // TRANSITION renovation: kit geometry (torn paper, beads, tiles) appended to a block's builder.
+        public void Raw(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t)
+        {
+            var o = vertices.Count;
+            vertices.AddRange(v); normals.AddRange(n); uvs.AddRange(uv);
+            foreach (var i in t) triangles.Add(o + i);
         }
 
         public Mesh ToMesh(string name)
@@ -251,6 +259,27 @@
     MaterialPropertyBlock block;
     ThemeMaterials level0, office;
     Material trim, doorLeaf, glass, keyGlow;
+    // TRANSITION renovation: the visual kit's view of this map (one per cache).
+    FrontRoomsTransitionKit.Context transitionKit;
+    FrontRoomsMapCache transitionCache;
+
+    // TRANSITION renovation
+    FrontRoomsTransitionKit.Context TransitionKit()
+    {
+        if (!FrontRoomsTransitionKit.Enabled || Cache == null) return null;
+        if (transitionKit != null && transitionCache == Cache) return transitionKit;
+        transitionCache = Cache;
+        return transitionKit = new FrontRoomsTransitionKit.Context
+        {
+            theme = c => Cache.ZoneOf(c).theme,
+            ceiling = c => MapGrid.CeilingHeight(Cache.ZoneOf(c).height),
+            edge = (p, q) => Cache.Edge(p, q),
+            arch = ArchOpening,
+            excluded = InStartArea,
+            seed = Cache.Generator.Seed,
+            mats = FrontRoomsTransitionKit.Mats.Load(level0.ceiling),
+        };
+    }
     bool begun;
 
     static bool dressersResolved;
@@ -793,6 +822,10 @@
         }
         var collision = new MeshBuilder();
         int BlockOf(int i, int j, float ceiling) => (i / BlockCells + j / BlockCells * blocks) * heights + HeightClass(ceiling);
+        // TRANSITION renovation: the kit for this chunk (null when switched off).
+        var kit = TransitionKit();
+        kit?.Begin(new GridCoord(coord.x * n, coord.y * n), chunk.root.transform.position);
+        void Collide(Vector3 c, Vector3 s) => collision.Box(c, s, origin, 1f);
 
         void Solid(int blockIndex, Material material, Vector3 center, Vector3 size, float repeat)
         {
@@ -814,8 +847,17 @@
             var reserved = InStartArea(cell);
             if (!reserved)
             {
-                Solid(b, theme.floor, cellCenter + Vector3.down * (ModuleUnits.FloorSlab * .5f), new Vector3(cs, ModuleUnits.FloorSlab, cs), CarpetRepeat);
-                Solid(b, theme.ceiling, cellCenter + Vector3.up * (height + ModuleUnits.CeilingSlab * .5f), new Vector3(cs, ModuleUnits.CeilingSlab, cs), CeilingRepeat);
+                // TRANSITION renovation: a Level 0 crossing cell builds its floor and ceiling in pieces (render only); collision keeps the full slabs.
+                if (kit != null && FrontRoomsTransitionKit.CrossingCell(kit, cell, new Vector3(i * cs, 0f, j * cs), height, b, theme.floor, theme.ceiling))
+                {
+                    Collide(cellCenter + Vector3.down * (ModuleUnits.FloorSlab * .5f), new Vector3(cs, ModuleUnits.FloorSlab, cs));
+                    Collide(cellCenter + Vector3.up * (height + ModuleUnits.CeilingSlab * .5f), new Vector3(cs, ModuleUnits.CeilingSlab, cs));
+                }
+                else
+                {
+                    Solid(b, theme.floor, cellCenter + Vector3.down * (ModuleUnits.FloorSlab * .5f), new Vector3(cs, ModuleUnits.FloorSlab, cs), CarpetRepeat);
+                    Solid(b, theme.ceiling, cellCenter + Vector3.up * (height + ModuleUnits.CeilingSlab * .5f), new Vector3(cs, ModuleUnits.CeilingSlab, cs), CeilingRepeat);
+                }
             }
 
             // East and north edges of every cell. Chunk borders on the east and
@@ -832,14 +874,23 @@
             // A wall's ends reach half a thickness past its corner, except into the start area.
             // The start area's side walls also stop on the door line, where the stream room's end wall closes the corner.
             var startSide = InStartArea(cell) != InStartArea(east);
+            // TRANSITION renovation (B0.2): each face of a wall goes in its own side's height block, so _CeilingHeight is its own ceiling.
+            var eastStart = InStartArea(cell) || InStartArea(east);
+            var northStart = InStartArea(cell) || InStartArea(north);
+            var eastBlockA = eastStart ? BlockOf(i, j, eastHeight) : BlockOf(i, j, height);
+            var eastBlockB = eastStart ? BlockOf(i, j, eastHeight) : BlockOf(i, j, MapGrid.CeilingHeight(eastZone.height));
+            var northBlockA = northStart ? BlockOf(i, j, northHeight) : BlockOf(i, j, height);
+            var northBlockB = northStart ? BlockOf(i, j, northHeight) : BlockOf(i, j, MapGrid.CeilingHeight(northZone.height));
             BuildEdge(chunk, eastKind, cell, east, new Vector3((i + 1) * cs, 0f, j * cs), Vector3.forward,
                 eastHeight, eastA, eastB, BlockOf(i, j, eastHeight), Get, Solid, origin,
                 !BothInStartArea(new GridCoord(cell.x, cell.y - 1), new GridCoord(cell.x + 1, cell.y - 1)),
-                !BothInStartArea(new GridCoord(cell.x, cell.y + 1), new GridCoord(cell.x + 1, cell.y + 1)) && !(startSide && cell.y + 1 == startArea.yMax));
+                !BothInStartArea(new GridCoord(cell.x, cell.y + 1), new GridCoord(cell.x + 1, cell.y + 1)) && !(startSide && cell.y + 1 == startArea.yMax),
+                kit, eastBlockA, eastBlockB, Collide);
             BuildEdge(chunk, northKind, cell, north, new Vector3(i * cs, 0f, (j + 1) * cs), Vector3.right,
                 northHeight, northA, northB, BlockOf(i, j, northHeight), Get, Solid, origin,
                 !BothInStartArea(new GridCoord(cell.x - 1, cell.y), new GridCoord(cell.x - 1, cell.y + 1)),
-                !BothInStartArea(new GridCoord(cell.x + 1, cell.y), new GridCoord(cell.x + 1, cell.y + 1)));
+                !BothInStartArea(new GridCoord(cell.x + 1, cell.y), new GridCoord(cell.x + 1, cell.y + 1)),
+                kit, northBlockA, northBlockB, Collide);
 
             if (data.pillar[i + j * (n + 1)] && !TouchesStartArea(cell.x, cell.y, cell.x, cell.y))
             {
@@ -853,6 +904,22 @@
             if (!reserved) BuildFixture(chunk, cell, cellCenter, height, theme, data.lamp[index], data.tier);
         }
 
+        // TRANSITION renovation: the kit's boxes and meshes go into the blocks; block -1 (props, ducts, tiles) into its own renderers.
+        Dictionary<Material, MeshBuilder> kitLoose = null;
+        if (kit != null)
+        {
+            kitLoose = new Dictionary<Material, MeshBuilder>();
+            MeshBuilder Sink(int block, Material m)
+            {
+                if (block >= 0) return Get(block, m);
+                if (!kitLoose.TryGetValue(m, out var lb)) kitLoose[m] = lb = new MeshBuilder();
+                return lb;
+            }
+            foreach (var bx in kit.output.boxes) Sink(bx.block, bx.mat).Box(bx.centre, bx.size, origin, bx.repeat);
+            foreach (var r in kit.output.raws.Values) if (r.v.Count > 0) Sink(r.block, r.mat).Raw(r.v, r.n, r.uv, r.t);
+            foreach (var p in kit.output.props) FrontRoomsKitLibrary.Spawn(p.asset, chunk.root.transform, p.position, p.rotation, null, false, "Renovation prop " + p.asset);
+        }
+
         for (var b = 0; b < builders.Length; b++)
         {
             if (builders[b].Count == 0) continue;
@@ -865,6 +932,17 @@
                 if (mesh != null) chunk.meshes.Add(mesh);
             }
         }
+        if (kitLoose != null && kitLoose.Count > 0)
+        {
+            // TRANSITION renovation
+            var go = new GameObject("Renovation");
+            go.transform.SetParent(chunk.root.transform, false);
+            foreach (var pair in kitLoose)
+            {
+                var mesh = AddRenderer(go, pair.Key, pair.Value, chunk.root.transform.position.y + ModuleUnits.StandardCeiling);
+                if (mesh != null) chunk.meshes.Add(mesh);
+            }
+        }
         var col = new GameObject("Collision");
         col.transform.SetParent(chunk.root.transform, false);
         var shell = col.AddComponent<MeshCollider>();
@@ -944,20 +1022,42 @@
     /// </summary>
     void BuildEdge(BuiltChunk chunk, EdgeKind kind, GridCoord a, GridCoord b, Vector3 start, Vector3 along, float height,
         Material wallA, Material wallB, int blockIndex, BuilderFn get, SolidFn solid, Vector3 origin,
-        bool mayExtendStart = true, bool mayExtendEnd = true)
+        bool mayExtendStart = true, bool mayExtendEnd = true,
+        FrontRoomsTransitionKit.Context kit = null, int blockA = -1, int blockB = -1, Action<Vector3, Vector3> collide = null) // TRANSITION renovation
     {
         if (kind == EdgeKind.Open) return;
         var length = MapGrid.CellSize;
         // Positive "across" points from cell a into cell b.
         var across = new Vector3(along.z, 0f, along.x);
-        void Piece(float from, float to, float bottom, float top, bool extendStart, bool extendEnd)
+        // TRANSITION renovation: the edge as the kit sees it (opening filled in below).
+        var desc = new FrontRoomsTransitionKit.Edge
         {
+            kind = kind, a = a, b = b, alongX = along.x > .5f, start = start, along = along, across = across, height = height,
+            wallA = wallA, wallB = wallB, blockA = blockA >= 0 ? blockA : blockIndex, blockB = blockB >= 0 ? blockB : blockIndex,
+            mayExtendStart = mayExtendStart, mayExtendEnd = mayExtendEnd,
+        };
+        var useKit = kit != null && collide != null;
+        void Piece(float from, float to, float bottom, float top, bool extendStart, bool extendEnd, bool atOpeningStart = false, bool atOpeningEnd = false)
+        {
             if (from > 0f || !mayExtendStart) extendStart = false;
             if (to < length || !mayExtendEnd) extendEnd = false;
             var f = from - (extendStart ? WallThickness * .5f : 0f);
             var t = to + (extendEnd ? WallThickness * .5f : 0f);
             if (t - f < .05f || top - bottom < .05f) return;
             var center = start + along * ((f + t) * .5f) + Vector3.up * ((bottom + top) * .5f);
+            if (useKit)
+            {
+                // TRANSITION renovation: collision exactly as before; render geometry from the kit (B0.1 corners, B0.2 blocks, drops).
+                if (wallA == wallB) collide(center, Abs(along * (t - f) + across * WallThickness + Vector3.up * (top - bottom)));
+                else
+                {
+                    var h2 = along * (t - f) + across * (WallThickness * .5f) + Vector3.up * (top - bottom);
+                    collide(center - across * (WallThickness * .25f), Abs(h2));
+                    collide(center + across * (WallThickness * .25f), Abs(h2));
+                }
+                FrontRoomsTransitionKit.WallPiece(kit, desc, from, to, bottom, top, atOpeningStart, atOpeningEnd);
+                return;
+            }
             if (wallA == wallB)
             {
                 var size = along * (t - f) + across * WallThickness + Vector3.up * (top - bottom);
@@ -969,7 +1069,12 @@
             solid(blockIndex, wallB, center + across * (WallThickness * .25f), Abs(half), WallpaperRepeat);
         }
 
-        if (kind == EdgeKind.Wall) { Piece(0f, length, 0f, height, true, true); return; }
+        if (kind == EdgeKind.Wall)
+        {
+            Piece(0f, length, 0f, height, true, true);
+            if (useKit) FrontRoomsTransitionKit.EdgeExtras(kit, desc, MapGrid.CeilingHeight(Cache.ZoneOf(a).height), MapGrid.CeilingHeight(Cache.ZoneOf(b).height)); // TRANSITION renovation
+            return;
+        }
 
         float width, c, openingTop, sill = 0f;
         if (kind == EdgeKind.Arch)
@@ -990,10 +1095,18 @@
             openingTop = WindowTop;
             sill = WindowSill;
         }
-        Piece(0f, c - width * .5f, 0f, height, true, false);
-        Piece(c + width * .5f, length, 0f, height, false, true);
+        // TRANSITION renovation: the opening for the kit.
+        desc.lo = c - width * .5f; desc.hi = c + width * .5f; desc.top = openingTop; desc.sill = sill;
+        Piece(0f, c - width * .5f, 0f, height, true, false, false, true);
+        Piece(c + width * .5f, length, 0f, height, false, true, true, false);
         Piece(c - width * .5f, c + width * .5f, openingTop, height, false, false);
         if (sill > 0f) Piece(c - width * .5f, c + width * .5f, 0f, sill, false, false);
+        if (useKit)
+        {
+            // TRANSITION renovation: reveal and beads on border arches; props, scraps and tape per face.
+            FrontRoomsTransitionKit.ArchReveal(kit, desc);
+            FrontRoomsTransitionKit.EdgeExtras(kit, desc, MapGrid.CeilingHeight(Cache.ZoneOf(a).height), MapGrid.CeilingHeight(Cache.ZoneOf(b).height));
+        }
 
         if (kind == EdgeKind.Arch) return;
 
```
