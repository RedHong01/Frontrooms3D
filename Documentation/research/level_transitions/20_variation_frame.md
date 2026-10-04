# Level transitions · 20 · Variation V1 Frame: summary

Date: 2026-10-03. This page is the short version. The full report, numbers, diff and logs are in `11_var_frame.md`.
Built and rendered in the private clone `proj_trans_frame` only. Red's project only received the files listed at the end.

## Images

- `images/v_frame_vs_before.jpg`: six rows, BEFORE | FRAME. Rows 1-4 are the fixed shots, rows 5-6 are extras.
- `images/v_frame_shot1.jpg` .. `shot4.jpg`: the four fixed shots (same harness and cameras as `shot<k>_before.jpg`).
- `images/v_frame_shot5.jpg`: extra, a close view of the portal on Z = 588 (pilaster, header, wall angle, base wrap) and the cased arch on X = 765.
- `images/v_frame_shot6.jpg`: extra, a close view of both shot1 arches (Office reveals, beads, bars, base into the reveals).
- `images/var_b0_shot1..4.jpg` and `var_b0_sheet.jpg`: step 0, B0 alone.
- `images/var_frame_shot1..4.jpg` and `var_frame_sheet.jpg`: Frame (the same files as `v_frame_shot1..4`).
- Close frames:
  - `images/var_frame_close_post.jpg`, `_wallend`, `_seam`, `_door` and `_window`;
  - `images/var_frame_close_sheet.jpg` (before | after for all five).

## What was built

- **B0.**
  - Corner posts: no wall piece of a different finish enters the 0.16 m post square. The post is four render-only quarters, each in the finish of the cell it faces.
  - Grime band: each face of a height-border wall is built in its own height block.
- **Cased arch** (10 in 25 chunks):
  - walls beside the arch are inset 0.012 m;
  - an Office-paint lining with 6 mm beads fills the reveal;
  - an aluminium binder bar (35 × 5 mm) sits on the line.
- **Portal** on every border open edge (30):
  - 0.12 × 0.20 m pilasters and a 0.35 m header (2.55-2.90);
  - beads and white wall angles;
  - a bar on the line;
  - a 0.24 m shared post where two openings meet.
- **Door frame** (39-45): a dark-bronze pressed-steel sleeve to R3's envelope (casing 0.0795-0.105 m off centre, 16 mm stop), with a fluted aluminium saddle (152 × 12 mm). The old trim is hidden inside.
- **Window frame** (10): the same sleeve, with glazing stops.
- **End caps** (2-3) and beige vinyl **corner guards** (1-5).
- **Rubber cove base**, 0.10 m, on every Office wall face: 1.7 km per 25 chunks. It stops at casings and wraps corners, pilasters and reveals.
- All pieces are swept profiles with real fillets. They are render-only. No FBX, no Blender module, no texture.

## Change list (clone)

1. `FrontRoomsMapWorld.cs`: 11 hooks marked `// TRANSITION frame`, +220 / −18 lines.
   - B0;
   - the BuildEdge, corner and base hooks;
   - render extents split from collision extents;
   - `MeshBuilder.Append`;
   - per-chunk kit renderers.
2. New `Scripts/Rendering/Transitions/FrontRoomsTransitionKit.cs` (48 KB): all V1 logic, visual-owned.
3. New `Resources/Surfaces/Trans_AluminiumSatin.mat` (2.1 KB): `Prop_Aluminium`'s maps with a satin finish.
4. Tools only: `Editor/Transitions/FrontRoomsTransitionAssets.cs`, `FrontRoomsTransitionStats.cs`, `FrontRoomsTransitionCollisionCheck.cs`.
5. Checks:
   - the harness and `shots.json` are unchanged;
   - the basecheck is byte-identical to BEFORE, and so is kit-off mode;
   - all `-plan` files match;
   - collision meshes hash the same in off, b0 and frame;
   - lit lamps stay 84 / 82 / 83 / 88.

## Costs measured (within 46 m of each eye, frame vs today)

- Triangles: +34.7 k to +40.6 k (+21 to +24 %). About half of that is the base.
- Draw proxy (renderers in the frustum): +34 to +45. Of these, 23-28 are kit renderers (one per chunk and material) and the rest come from B0.
- Texture memory: +0 MB. Lights: no change. Materials: +1.
- Mesh memory per 25-chunk build: +4.3 to +4.6 MB.
- B0 alone: +3.2 to +3.7 % triangles, +7 to +20 renderers in the frustum.

## What the real implementation needs

- **Map chat:**
  - the BuildEdge hook for all edges outside the start area, and a per-chunk corner descriptor;
  - render extents split from collision (collision must stay byte-identical);
  - `BeadInset`;
  - one B0.1 rule;
  - `MeshBuilder.Append`;
  - new `ModuleUnits` (`BeadInset` 0.012, `PortalPilaster` 0.12, `PortalDepth` 0.20, `PortalHeaderDrop` 0.35, `SharedPilaster` 0.24, `BarWidth` 0.035);
  - a decision on free-post colliders;
  - Dress API keep-clear rules;
  - a rerun of the 66 map tests.
- **Visual chat:**
  - merge the kit into main's existing `FrontRoomsTransitionKit.cs` (keep its GUID);
  - add the satin aluminium to RenderSetup;
  - pick the frame finish and the base colour;
  - swap in R3's `Kit_DoorFrame_Steel` and window frames once the audit clears them;
  - WebGL-only draw savings (merge the base into the block trim, or drop it past 10 m).

## Main changed (Codex, 19:10)

- Codex committed **Light lead's** B0 and V5 code into Red's project. Frame was not promoted. Red has not picked a variation.
- If Red picks Frame:
  - **merge, never copy.** Main already has `FrontRoomsTransitionKit.cs` with another GUID, and its B0.1 uses a different (per-skin) rule;
  - **rebase the door hook** on main's single-acting doors: the stop must follow the door's swing side;
  - **use R3's frame FBX** from main after the audit.
- Two more points from the audit's `00_main_state.md`:
  - **main already frames every map window** (Codex's `DressWindow` call), so Frame's window proxy must be dropped there;
  - **R3 frames would vanish at 12 m after a reimport** (a KitImporter LOD defect), which must be fixed before Frame uses them.
- **B0 (Light lead's rule) is live in Red's game now.** B0 alone could ship as the seam fix (`var_b0_sheet.jpg`).
- This workflow did not edit main.
- `codex_audit/20_findings.md` did not exist at the last check (22:25), and `00_main_state.md` lists no finding against Frame.

## Open issues

1. **Free posts (5 per 25 chunks) have no collider**, so the player can walk through them.
2. **The dark base line** on every Office wall: Red to judge the colour.
3. **Frame is tidy, not moody.** Pair it with V5's cool Office colour.
4. **One B0.1 rule** must be chosen (this clone, main and the brief differ).
5. **Check the stop against the closed leaf** in Play.
6. **The verification log section is not on the Figma canvas yet.** The rows to add are in `11_var_frame.md` §14.

## Outputs in Red's project

- `images/var_b0_*`, `var_frame_*`, `v_frame_*` (the files above);
- `11_var_frame.md`;
- this file.
