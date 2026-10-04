# Level transitions · 20 · Variation V3 Neck: summary

Date: 2026-10-03. Full report, numbers, diff and logs: `13_var_neck.md`. This page is the short version.
Built and rendered in the private clone `proj_trans_neck` only. Red's project only received the files listed in `13_var_neck.md` §13. Nothing was promoted to main.

## Images

- `images/v_neck_vs_before.jpg`: six rows, BEFORE | NECK, for the four fixed shots and two extra shots.
- `images/v_neck_shot1.jpg` .. `shot4.jpg`: the four fixed shots (same harness and cameras as `shot<k>_before.jpg`).
- `images/v_neck_shot5.jpg`: extra, the Office side of the shot1 arch neck (Office face, cove base, wall angle).
- `images/v_neck_shot6.jpg`: extra, the X = 765 run beside shot2: three necks share cheeks and read as an arcade.
- Also: `var_neck_shot1-4.jpg` (same files), `var_neck_sheet.jpg` (4 rows, before | after), and close frames `var_neck_close_mouth`, `_seam`, `_post`, `_wallend`, `_door`.

## What was built

- **Neck.** Every open edge or arch between Office and Level 0 becomes a 0.60 m deep block centred on the line, over the full 3 m edge. One opening: 1.80 m for open edges, the arch's own width for arches; top 2.20 m. The Level 0 face is paper, the Office face is paint, and the whole reveal and soffit are Office paint. 6 mm beads on the mouth arrises; the paper stops at the bead. Cheeks and header are collision.
- **Floor.** Office carpet from the Level 0 mouth (1 mm overlay), an aluminium binder bar (35 × 5 mm) on the mouth line, rubber cove base (0.10 m, real profile) on the Office face and through the reveal. No base on the Level 0 face.
- **Ceiling.** Each grid dies into the neck face with a 22 mm white wall angle. The 2.20 m soffit hides the change.
- **Door recess.** A border door stands at the back of a 1.60 × 2.40 × 0.30 m recess on its stop side (Office side in the clone). Office lining, beads, cove base; the 2.40 m soffit is the Low height as a built step. The leaf still swings 0–95° with 0.259 m to spare.
- **Runs and corners.** Openings on one line share cheeks (an arcade). Corridor walls that meet a neck end in its cheek, so their seams are buried.
- **B0.** Corner posts (no overlapping faces, no z-fight) and the grime band per side.
- **Unchanged:** windows, lamps, zones, maze, rooms, modules, edge kinds, door positions. No new lights, materials, textures or meshes.

## Change list (clone)

1. `FrontRoomsMapWorld.cs`: hooks marked `// TRANSITION neck`, +140 / −12 lines: `Builder.Append`, one kit per chunk, per-side blocks, neck and recess calls in `BuildEdge`, corner offsets for pieces, a `TransitionView` and a material map.
2. New: `Scripts/Rendering/Transitions/FrontRoomsTransitionKit.cs` (46.6 KB): neck, recess, posts, trims and their profiles.
3. Tools only: `Editor/Transitions/FrontRoomsNeckStats.cs` (stats, flat-seam census, leaf sweep).
4. Harness and `shots.json` unchanged. Baseline 4/4 byte-identical before any edit. Layout frozen: all `-plan` files identical. Lamp lines identical (lit 84 / 82 / 83 / 88).

## Results

| Shot | What it shows | Nearest new solid |
|---|---|---|
| shot1 | The Z = 609 arch as a 0.6 m Office-lined throat; paper stops at the bead; carpet and bar at the mouth; soffit. The X = 795 neck fills the right edge. | 0.700 m |
| shot2 | A 1.8 × 2.2 m deep opening across the corridor; both corridor walls end in its cheeks. The X = 765 necks at left. | 1.450 m |
| shot3 | The door at the back of a lined 0.30 m recess with a 2.40 m soffit. | 2.384 m |
| shot4 | The far arch neck, and the X = 798 neck at right. The flat seam on the right wall is gone. | 4.434 m |

- **Flat seams:** 0.61 per chunk before, **0** with necks (1,620 chunks). They are all buried in a cheek or an inside corner.
- **Per chunk:** 1.26 necks (0.89 open, 0.37 arch), 1.37 recesses, 10.76 posts.
- **Crossings:** open edges 2.84 → 1.80 m (−37 %). Necks 1.10–1.80 m wide (mean 1.69), 2.20 m high. The Relay (0.60 × 2.05 m) fits every neck.

## Costs measured (within 46 m of each eye)

- Triangles: +14.5 k to +16.7 k (+8.9 to +9.7 %). 280 per neck, 256 per recess, 2 per post; 729 per chunk. The cove base is 65 % of it.
- Draws (submeshes in range): +12 to +32 (+0.6 to +1.6 %). Renderers: the same +12 to +32 (trim blocks and per-side wall blocks).
- Materials: +1 (`Painted_Metal` for the wall angle). Lights: 0 change.
- Texture memory: 0 MB of new files. +8.0 MB in range because `Painted_Metal`'s textures are new to the map (the title corridor already uses them).
- Collision: 18.65 boxes per chunk, added to the chunk's existing collision mesh. 0 new colliders.
- Chunk build: about +3 to 4 ms per chunk (editor batch, rough).

## What the real implementation needs

- **Map chat (large):** the hooks; neck and recess collision; constants `NeckDepth` 0.60, `NeckOpening` 1.80, `RecessDepth` 0.30, `RecessWidth` 1.60, `RecessTop` 2.40; Dress keep-clear from the neck face (+0.22 m); `ArchCornerMargin` ≥ 0.36 m at corners with a border neck; recess side opposite `FixedSwing`; nav 60/60, interaction and designer tests rerun.
- **Visual chat:** merge the kit into main's `FrontRoomsTransitionKit.cs` (keep main's GUID `b215858f…`; same path and class name); reuse main's B0 for corners without a neck; decide the wall angle material; Play-mode draw and build-time check; re-render over main's current lamps.
- **Others:** 系统设计 (narrower crossings, chase through a short tunnel); wallpaper chat (paper ends at a bead on every neck face); sound chat (optional neck acoustics).

## Main changed (Codex, 19:10–19:41)

- Main now has Light lead's kit at the same path, plus B0 and the V5 colour lead in `FrontRoomsMapWorld.cs`, on by default. Neck is not in main.
- A Neck merge must keep main's GUID and B0, add only the neck parts, and follow the single-acting door swing.
- The codex audit had written `00_main_state.md` (it agrees: V5 + B0 promoted from the Light lead clone, on by default, no pick). Its `20_findings.md` did not exist yet, so no confirmed finding names this workflow. The map chat has rebased onto Codex's MapWorld; the Neck contract must be re-expressed against main when Red picks it.

## Open issues

1. Space: border cells lose 0.22 m and open crossings lose 37 % of their width. Walk it in Play before picking.
2. 96 plain arches next to a neck (0.06 per chunk) drop to as little as 1.07 m. Needs the `ArchCornerMargin` contract item.
3. With single-acting doors about half the recesses land on the Level 0 side. That case was not rendered.
4. On the Office side the block reads as a 3 m pier, 0.22 m proud. A shorter block is an option.
5. The cove base only exists at necks and recesses. Pairs well with V1 Frame's base on all Office walls.
6. Draw calls need a Play-mode GPU capture.
7. These frames use the old warm Office lamps; main now runs V5's cool colour. Re-render over main before the final pick.
