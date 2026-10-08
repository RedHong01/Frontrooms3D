# Level transitions · 20 · Variation V2 Renovation: summary

Date: 2026-10-07. Full report, numbers, diff and logs: `12_var_renovation.md`. This page is the short version.
Built and rendered in the private clone `W/proj_trans_renovation2` only (W = `/Users/redwang/FrontRoomsVisualWork`). Red's project only received documentation (the files in `12_var_renovation.md` §14). Nothing was promoted to main. No downloads. No Figma writes.

## Images

- `images/v_renovation_vs_before.jpg`: seven rows, BEFORE | RENOVATION: the four fixed shots and three extras.
- `images/v_renovation_shot1..4.jpg`: the four fixed shots (same harness and cameras as `shot<k>_before.jpg`). Also saved as `var_renovation_shot1..4.jpg`, with `var_renovation_sheet.jpg` (4 rows, before | after).
- `images/v_renovation_shot5.jpg`: extra, the shot3 door from its Level 0 side: ragged paper cut, compound halo, slab strip.
- `images/v_renovation_shot6.jpg`: extra, the X = 768 corridor wall: paper, torn edge, stripped, primer, paint, then the Z = 588 line.
- `images/v_renovation_shot7.jpg`: extra, the X = 795 arch close up: bare reveal, galvanised beads, compound.

## What was built

- **The band.** Within 3 paper drops (2.25 m) of every Level 0 | Office crossing, the Level 0 wall steps drop by drop: fresh Office paint (n = 0), primer over taped drywall (n = 1), stripped board (n = 2), then paper. Drops sit on the paper's own 0.75 m world grid. Each drop is its own 0.08 m skin box in its own material; no overlays. One pattern per crossing: 60 % full, 25 % stripped only, 15 % one stripped drop.
- **Torn paper.** On every paper | stripped line: a real mesh with a ragged 5–27 mm contour, 2–3 tongues up to 150 mm, a 2.5–8 mm white base-layer lip, the top 0.10 m curling 15–25 mm off the wall. Remnants at the floor and under the ceiling, islands of print mid-drop, hanging peeled strips.
- **Openings.** Border arches: bare drywall reveal, galvanised corner beads (30 mm flanges, 3.5 mm proud), compound halo. Border doors and windows: a 0.08–0.20 m compound halo on the Level 0 face with the paper cut ragged round it and knife smears. The Office face is finished.
- **Floor.** Level 0 carpet cut back 0.6 m before the line with a 5 mm frayed edge; a glued slab strip (trowel ridges, chalk line); loose and stacked Office carpet tiles. Office tiles run to the line.
- **Ceiling.** In each Level 0 crossing cell: one tile out (dark plenum, duct, sagging cable), one pushed up askew, two new 2 × 2 tiles 6 % brighter in the line strip.
- **Props (render-only).** Step stool, 5-gallon compound pail, paint tray with roller, folded drop cloth, open carton of carpet tiles; paper scraps and beige masking tape. 2.1–2.2 per chunk, at least 1.0 m clear of openings. No labels, no brands.
- **B0.** Corner posts with one owner (no z-fight) and the grime band per side.
- **Unchanged:** zones, maze, rooms, modules, columns, edge kinds, door positions, collision, lamps, the Office side.

## Change list (clone)

1. `FrontRoomsMapWorld.cs`: 14 hooks marked `// TRANSITION renovation`, +119 / −9 lines (full diff in `12_var_renovation.md` Appendix A and `code/renovation/mapworld_renovation_hooks.diff`).
2. New `Scripts/Rendering/Transitions/FrontRoomsTransitionKit.cs` (1,841 lines, 104.7 KB): the whole variation and B0.1.
3. New tools: `Editor/Rendering/FrontRoomsTransitionAssets.cs` (materials, band maps, stats), `Tools/lookdev/gen_transition_surfaces.py` (10 texture sets).
4. New props: `trans_bucket.py`, `trans_paint_tray.py`, `trans_drop_cloth.py`, `trans_tile_box.py` and their FBX (4,386 / 1,860 / 1,480 / 876 LOD0 triangles).
5. New: 19 materials, 30 textures.
6. Harness and `shots.json` unchanged. Baseline 4/4 byte-identical before any edit. Layout frozen: all `-plan` files identical. Lamp lines identical (lit 84 / 82 / 83 / 88).
7. Fixed in this run: a NaN in the knife smears that darkened whole compound renderers by about 40 %; bead flanges raised from 1.6 to 3.5 mm to stop a sawtooth at 5.8 km from the origin; paint gear now appears (11 trays and 5 drop cloths in 56 props, up from 1 tray).

## Costs measured (within 46 m of each eye)

| | BEFORE | RENOVATION | Change |
|---|---|---|---|
| Triangles | 133.7 k – 144.6 k | 532.1 k – 631.8 k | +398 k to +491 k (×4.0 to ×4.5) |
| Renderers (≈ draws before culling) | 1,702 – 1,746 | 2,065 – 2,103 | +336 to +400 (+19 % to +24 %) |
| Materials | 42 | 62 | +20 |
| Lit / shadowed lights | 84/8, 82/7, 83/7, 88/7 | the same | 0 |
| Props in range | 0 | 30 – 42 | |

- Props and loose kit pieces are 101–129 k of the added triangles. The rest is mostly the paper edges (4.5 mm steps, about 890 triangles per metre of edge) and the carpet fray.
- **Texture memory: 94.0 MB desktop** (five 2048² sets, three 1024², two 512², BC7 with mips); **WebGL estimate 34.0 MB** (1024² cap). The plan estimated 50–65 MB.
- **Per chunk:** 4.7–5.8 m of band face plus 2.6–3.0 m of compound halo; 1.8–2.3 tear lines; 3.3 m² of slab strip; 2.8–2.9 tiles out; 2.1–2.2 props.

## What the real implementation needs

- **Map chat (contract):**
  - the face-run hook (ends, normal, height, distance along the run) with a run hash and the chunk tier from the map, so a tier can set how often a run is mid-renovation (the plan suggests 25–35 % of border runs);
  - splitting band faces into drop pieces in `Piece()` (render only; collision as today);
  - the floor and ceiling of each Level 0 crossing cell built in pieces, with one ceiling-slab hole for one tile (render only; collision keeps the full slabs);
  - a Dress API keep-clear for each render-only prop, and Dress kept out of the props' spots;
  - neighbour reads only from the chunk's own cells (codex audit MAP-2 applies to this kit's B0.1 and face runs too);
  - B0, merged with whatever B0 rule main keeps.
- **Visual chat:**
  - merge this kit into main's `FrontRoomsTransitionKit.cs` (keep main's GUID `b215858f…`; codex audit MAP-5), on top of main's B0 and V5;
  - cut the triangles about 70 % before shipping: 12 mm edge steps with the fine detail in a `Paper_TornEdge` alpha atlas, fray only near the eye, block −1 pieces merged into the blocks;
  - cut textures to about 70 MB (`Prop_Canvas` and `Slab_Adhesive` to 1024);
  - the WebGL tier (1024² sets, no tear meshes or props past 10 m, WebGL-only);
  - re-render over main's current lamps and single-acting doors before the final pick.
- **Others:** the wallpaper chat (stripped and painted drops carry no print and no EGRESS ink); 平面视觉 (nothing printed on the props); the sound chat (optional work-site room tone near borders).

## Open issues

1. Cost: triangles ×4.0–4.5 within 46 m and 94 MB of textures. The look holds only if the optimisations above keep it.
2. If every border is under renovation the map reads as a building site. The prototype dresses every border run so the shots show it; the real version should dress 25–35 %.
3. shot3 cannot show the Level 0 face of the door (it faces away); shot5 shows it.
4. Not built: the raw wall end (cut E) and the tier link.
5. Hanging strips are about one per three tear lines, fewer than the brief's 1–2 per run.
6. The fixed shots use salt 130 so all four crossings draw the full pattern; the real kit uses salt 0.
7. These frames use the clone's warm Office lamps; main now runs V5's cool colour.
8. Verification frames are listed in `12_var_renovation.md` §13 for the Figma VERIFICATION LOG (no Figma writes in this stage).
