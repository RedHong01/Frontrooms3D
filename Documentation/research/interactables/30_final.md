# 30 — Interactables kit: final state after the fix pass

Status: **DONE (fix stage), 2026-10-08 01:3x.** Written by the interactables-kit workflow (visual chat), fix pass after the render stage (`20_renders.md`) and its critic. This file is the hand-off: the final asset table, the corrected integration contract for the map chat, what was fixed and what was rejected, what is still open, and the exact list of files to promote.

**What changed in Red's project:** only the interactables' own Blender modules (`Tools/Blender/frontrooms_kit/assets/interact_*.py`, 33 edited plus 1 new, `interact_door_leaf_veneer_oak.py`) and files under `Documentation/research/interactables/` plus the verification-log rows. No FBX, JSON, `.meta`, C# or material in `Assets/` was touched. The Blender builds also rewrote 33 tracked `__pycache__/interact_*.pyc` files in main; I restored them with `git checkout` and deleted the one new `.pyc` (it is git-ignored).

**Where the work is:**
- Clone `W/proj_int` (`W` = `/Users/redwang/FrontRoomsVisualWork`), re-synced from main `22bb75f` at 2026-10-07 23:56 (rsync, local patches = no-op, tools re-installed, the clone-only harness restored).
- Built kit: `W/proj_int/Assets/Resources/Props/Models/Kit_*`.
- Staged payload: `W/int_work/fix/payload/` (102 files, `SHA1SUMS.txt`).
- Renders: `W/proj_int/Verification/interactables_fix/` (final run; the first fix run is in `…/interactables_fix_run1/`). Three-views: `W/proj_int/Verification/threeview_fix/`.
- Evidence copied here: `fix_pass/` (LOD probe, LOD1 shape check, T8, hung-pose check, FBX content diff, promote list) and `harness/*.txt` (every script).

---

## 1. Verdict in one table

| Check | Verdict | Deciding number | Evidence |
|---|---|---|---|
| Door gap (T1), re-run on the rebuilt kit | **PASS** (unchanged) | old door 4,685 px inline, 7,133 sweep; kit doors 0 px in 288 of 292 views (the 4 others are the 5 cm floor camera and the spec's 3 mm undercut) | `images/intfix_gap_before_after.jpg`, `fix_pass/metrics_fix.json` |
| Wood grain (H3) | **FIXED** | oak, laminate and key-board grain now on texture U; before/after three-views | VL196 |
| Saddle (M1) | **FIXED** | steel-frame saddle in the frame's dark bronze; the bright line under OF-K is gone | VL196, `images/intfix_fix_before_after.jpg` |
| LODs and cull (H4, T7) | **FIXED** | 57 of 57 kits have a LODGroup whose d01 and dcull equal the sidecar; LOD1 shape error 0 px on the 9 kits the collapse broke, ≤ 0.6 px on every other kit | VL197, `fix_pass/lod_probe.tsv`, `fix_pass/lod1_check_keepall.json` |
| Key fit (T8 / T8b) after the cut-6 fix (L6) | **PASS** | 0 overlaps at 0, ±45, ±90°; blade 0.15 mm off the plug; pin 6 now sits 0.12 mm over its cut (was 1.01 mm over the tip bevel) | `fix_pass/t8_report_seat002.json` |
| Hung key pose, tag on the wall side (C2) | **PASS** (pose) / **FLAG** (single hook range) | 0 BVH overlaps in 18 of 18 host × tag cases; key in front; board and cabinet read at 6 m, a single hook only to about 2 m | VL198, `fix_pass/hung_pose.json` |
| Head dip, yawed pose (H7 proposal) | **PASS** | the key reads at every beat with pose P yawed 25° and the shot ending at 0.20 m, FOV 50 | VL199 |
| Locked door vs an open doorway into a lit room (M4) | **FLAG** | Office +1.80 to +3.55 stops; Level 0 +1.35 to +1.63 at 6 m (the bar is +1.5), +2.5 to +2.8 at 12–20 m | VL200 |
| Locked vs free (T6), unchanged materials | **FLAG** (unchanged) | Level 0 dim +0.77 to +0.82 stop; Run +0.97 at 20 m | `images/intfix_readability_sheet.jpg` |
| Window FBX rebuilt from main's 2026-10-04 modules (H5) | **DONE** | only `_Steel`, `_Steel_Enamel` and `_Alu` change content; all 6 window/blind sidecars change | VL196, `fix_pass/fbx_diff.json` |
| Exit device swing clearance (L4) | **PASS** | the larger latch case stays 5.4 mm off the latch stop over 0–95° on the A2 axis | `Kit_ExitDevice_Crossbar.json` `swingClearWorstZ` 0.97664 |

## 2. What the fix pass changed, critic item by critic item

Every critic item was checked against the files before acting. "Verified" means I reproduced the critic's claim.

| # | Critic claim | Checked | Action | Result |
|---|---|---|---|---|
| C1 | None of the kit doors, locks or keys are in the game | Verified: the map still builds the cube leaf (`FrontRoomsMapWorld.cs:1301-1306`, working tree 2026-10-08 01:23; the map chat is editing it, so lines drift), the cube key (`:2945-2946`), `LockPoint` 0.055 off the centre plane (`:2339-2347`); the hinge sits on the jamb (`:1299`); only the window facade exists (`:1369`) | Not fixable in this stage (modules only). Contract in §5 | **OPEN** (visual chat: door/key facade; map chat: A2 or facade pivot) |
| C2 | Keys on single hooks or surfaces cannot be found past about 2 m; the tag hides the key | Verified: at 4 m a lone hook is a few-pixel dot (`images/intfix_keys_4m_6m.jpg`) | Hung pose flipped: the tag hangs on the wall side (ring yaw 120°, tag twist 150° relative to the host; written into the hosts' sidecar `hungPose`); BVH re-check 0 overlaps; renders at 0.6 / 2 / 4 / ~6 m. Placement rule in the contract (§5.4) | **FIXED** (pose) / **OPEN** (map chat: host defaults) |
| H1 | Metal finishes read black or flip to white | Verified: `Prop_Chrome.mat:59-61` metallic 1, smoothness 0.85; `Prop_Aluminium` smoothness 1. The H7 frames still show near-black hardware | Not a module change. Proposed: tune `Prop_Chrome` toward satin (≈ 0.5 with a brushed mask) or add `Prop_SatinChrome` (a new slot: needs approval) | **OPEN** (visual chat) |
| H2 | Four project materials missing | Verified: no `Door_Enamel`, `Prop_KeyTagNo`, `Prop_SignEngraved` in `Resources/Surfaces/`. `Run_ExitSign_Dead` is now the exit-sign track's (it switched to `ExitSign_Diffuser_Dead`) | P-4, needs approval. Do not spawn `Kit_DoorSign` until `Prop_SignEngraved` exists | **OPEN** (visual chat, Red) |
| H3 | Grain wrong on all three leaf woods | Verified, measured: mean \|dA/dU\| / \|dA/dV\| of the albedo: Oak 1.44 / 4.38, Laminate 1.03 / 3.13, Dark 1.61 / 3.75 (U-grain); DoorVeneer 3.08 / 1.69, Walnut 1.87 / 0.90 (V-grain). kitlib's metre UVs assume V-grain | New helper `interact_door_common.u_grain(kit)` turns flagged parts' UVs 90° after kitlib's projection (wraps the Kit instance's `_uv_metres`; kitlib untouched). Applied to the oak leaf (new module `interact_door_leaf_veneer_oak.py`, no longer a slot-swap variant), the ward leaf, the wood frame's oak saddle and the key board. `DoorVeneer_A` itself (wavy "corduroy" stripes) is a texture: not touched | **FIXED** (UVs) / **OPEN**: `DoorVeneer_A` retexture; Oak, Teak, Cherry, Dark, Ebony and Laminate textures are U-grain library-wide, so every other kit using them shows cross grain (visual chat) |
| H4 | No LOD1 on about 45 small parts, nothing culls | Verified: the importer honours sidecar distances since 663e858 (`FrontRoomsKitImporter.cs:80-95`) | `LOD1 = LOD1_RATIO` in 26 modules. The first build exposed a regression: kitlib's collapse turned the key board into a triangle at 4 m and moved 8 other kits 5–33 px at d01. New helper `interact_door_common.lod1_keep_all(kit)` (protects every vertex; LOD1 = LOD0 minus `lod1_drop` parts) on those 9 modules. LOD2 still needs P-1 | **FIXED** (LOD1 + cull) / **OPEN** (LOD2, P-1) |
| H5 | Window FBX stale | Partly right: main's `Kit_WindowFrame_Wood` and both blinds already equal main's modules; `_Steel`, `_Steel_Enamel` and `_Alu` did not. All 6 sidecars differ (`wall_decor` tag, LOD ratios, `null` → `0.0` cull) | Rebuilt all 6 from main's modules (no module edit). A3 (walnut reveal) needs the map's T first and A11 (screw groove) needs S2 (`window_landing/04_promotion.md` item 7); A9 (mitred apron returns) is not done | **FIXED** (rebuild) / **OPEN** (A3, A9, A11; `FrontRoomsWindowTF.RunAll` gate, §7) |
| H6 | Intact glass reads as a hole or a mirror | Agreed (glass track W1 / G14) | none here | **OPEN** (glass track) |
| H7 | Head dip shows an edge-on key | Verified (VL159). Agreed that a key roll is impossible (the blade enters the vertical keyway only at 0°) | Harness renders the proposed pose; contract §5.3 | **PROPOSED** to the map chat; renders PASS |
| H8 | No real fracture set | Agreed (GD3) | none here; interface anchors unchanged | **OPEN** (glass-break track) |
| M1 | The aluminium saddle reads as light under the door | Verified (gap frames) | `Kit_DoorFrame_Steel` saddle and its screws use the frame's slot (`Prop_SteelBrown`): a dark-bronze anodised threshold, period-true; the `_Alu` variant swaps it to aluminium with the frame | **FIXED** |
| M2 | Run window frame clashes with the Run doors | Verified (`10_spec.md:645` vs `:261`) | Contract: W-RN → `Kit_WindowFrame_Steel` (§5.5). The live facade maps only Lobby and Office today | **CONTRACT** |
| M3 | Level 0 dim misses the +1 stop bar | Verified: +0.77 to +0.82 | Needs the P-4 `Door_Enamel` at luminance ≈ 0.66 | **OPEN** (material) |
| M4 | The readability test never compared a lit doorway | Verified | Two new harness conditions; results in §1 and VL200 | **DONE** (FLAG at 6 m in Level 0) |
| M5, M6 | Steel reads as stucco; walnut profile illegible | Agreed (surface work) | none here | **OPEN** (visual chat) |
| M7 | The no-clip result needs the A2 hinge | Verified (`:1299`) | Contract §5.1 | **OPEN** (map chat) |
| M8 | `LockPoint` 23.5 mm in front of the keyhole | Verified | Contract §5.2 | **CONTRACT** |
| L1 | Closer shoe faceted | Verified (15 points over 250°) | 35 points (48 per circle), pivot screw 32 around; LOD0 922 tris (spec 500: the 48-segment rule wins on a close-up part) | **FIXED** |
| L2 | Key hook flange faceted | Verified (20) | 48-segment flange, 16-sided wire; budget 850 (was 600) | **FIXED** |
| L3 | Knob reads as a radio dial | Verified (0.9 mm sagitta over Ø 43 mm) | Crowned face, sphere R 0.070 through Z 0.063, rounded crown; still 0.065 proud; 4,992 tris | **FIXED** |
| L4 | Crossbar reads as a towel rail | Agreed | Latch end is now a 0.1025 × 0.180 mechanism case with a rim-latch head and the retracted bolt face; swept clear of the stop. A projecting rim bolt and a frame rim strike need a moving bolt part and a new asset: not built (P2, EX-F has no map door) | **PARTLY FIXED** |
| L5 | Spec §1.4 wrong about the collider | Verified: collider ±0.025, visual leaf ±0.022 | Contract §5.2 | **CONTRACT** |
| L6 | Cut 6 under the tip bevel | Verified (5 cuts for 6 pins) | `TIP_BEVEL_Z` / `TIP_START` 0.0215 → 0.0234 in both helpers; T8 re-run PASS | **FIXED** |
| L7 | Escutcheon edge aliasing | Agreed | 1.5 mm round in 4 segments (was 1.0 mm in 3) | **FIXED** |
| L8 | Map trims under window frames | Agreed (codex kits F2) | none here (map applies the r6 "T" diff) | **OPEN** (map chat) |
| L9 | 90° plug turn | Accepted as a game liberty | none | **ACCEPTED** |
| F5 | `Kit_KeyCabinet` in main is the faulty build | Verified (`f7e570c7`) | Rebuilt from main's module (the G3 fix) and now with a keep-all LOD1 | **FIXED** in the payload |
| F6 | `Kit_KeyHookBoard` vs `Kit_KeyRack` | — | Red's pick; nothing renamed | **OPEN** (Red) |

**Codex audit:** `codex_audit/20_findings.md` still does not exist (checked 2026-10-08 01:3x), so I used `10_review_kits.md`. Its findings for this workflow: F3 (windows went live before the doors: Red's decision, unchanged), F4 (the missing slots: H2 above, open), F5 (the faulty key cabinet: fixed in the payload), F6 (the board's name: Red). Nothing in another chat's area was reverted.

**Rejected:** none of the critic's claims was wrong. Two were narrowed: H5 (3 of the 6 window FBX were stale, not 6) and L4 (no frame rim strike, see above). One of the critic's fixes was refused as stated: the key roll in H7 (impossible with a vertical keyway).

## 3. Verification run in this stage

- **Build:** Blender 4.3 headless, `build_asset.py` with `--out-root W/proj_int --no-preview`, 38 modules (exit sign excluded: the exit-sign track owns `Kit_ExitSign*` now). Every module's own asserts passed (envelopes, clear zone on windows, keyway, budgets ±15 % where asserted).
- **Content diff against main** (`fix_pass/fbx_diff.json`, Blender import of both FBX, positions, topology, UVs and materials): 50 FBX changed in content; 7 are byte-different only in FBX header timestamps and are **not** promoted (`Kit_DoorLeaf_Steel`, `_SteelLite`, `_Steel_PaintedMetal`, `Kit_DoorLeaf_Veneer`, `Kit_WindowFrame_Wood`, both blinds). That `Kit_DoorLeaf_Veneer` is content-identical proves the module refactor changed nothing.
- **T7, LOD probe** (`FrontRoomsIntFixBatch.LodProbeThenThreeView`, clone-only): every interactables kit has a LODGroup with LOD0 → LOD1 at the sidecar's d01 and the cull at its dcull (`fix_pass/lod_probe.tsv`). Distances are at lodBias 1 and FOV 76; Standalone Ultra doubles them.
- **LOD1 shape check** (`harness/lod1_check.py.txt`): two-sided LOD0/LOD1 surface deviation in pixels at d01. Before keep-all: cabinet 33.1 px, key board 26.7, exit device 23.2, mortise strike 10.6, deadbolt 10.4, mortise latch 9.8, bored strike 6.9, closer arm 5.8, forearm 5.4. After: 0.0 on all nine. Others: leaves and frames ≤ 0.6 px, knob 0.07, lever 0.03; the window track's blinds 1.1 and 1.7 px (pre-existing, content unchanged).
- **T8 / T8b** (`harness/t8_keyfit.py.txt`): key seated 0.02 mm out of the core face (G3's convention): 0 overlaps key–plug, key–shell, plug–shell at 0, ±45, ±90°; blade 0.150 mm off the plug; pin tips 0.083 mm (cuts 1–5) and 0.117 mm (cut 6) above the blade, measured vertically from the pin domes. Seated exactly at 0, the shoulder face touches the core face (15 coplanar contacts), as designed; both faces are back to back and invisible.
- **Hung pose** (`harness/hung_pose_check.py.txt`): the facade recipe re-created in Unity space with the real meshes; 3 hosts × 3 tags × 2 recipes: 0 overlaps in all 18; tag back 12.4 mm off the wall on the single hook, 11.9 mm off the board face, 17.2 mm off the wall inside the cabinet (clear of its back panel: 0 overlaps).
- **In-engine renders** (`FrontRoomsInteractablesLookdev`, all 12 groups OK, 417 PNG). New in the harness: the lit-doorway readability conditions (M4), the H7 dip frames, the tag-behind hung pose and 4 m / ~6 m key cameras (C2). Copies: `images/intfix_*.jpg` (21 harness sheets + 6 fix-pass sheets), `images/renders_fix/` (296 frames, 60 masks). The render stage's `images/int_*` files stay as logged in VL155–165.
- **Three-views** (`FrontRoomsThreeView`, same tool and scales as the 2026-10-03 hand-off) of the 18 changed kits: `W/proj_int/Verification/threeview_fix/`; before/after sheets `images/intfix_threeview_{doors,parts,windows}.jpg`. The hand-off folder `threeview/png/` was **not** overwritten: 平面视觉's K-sheets need these refreshed after promotion (§6).

## 4. Final asset table

57 assets (the exit-sign track's `Kit_ExitSign` and `_Dead` are not part of this list any more). Size is the mesh bounds in Unity metres (wall hosts have their origin at the floor, so their bounds start at their mounting height). LOD distances at lodBias 1, FOV 76 (doubled on Standalone Ultra); "never" = no cull. LOD2 is not exported until P-1.

| Asset | Size X × Y × Z (m) | Tris LOD0 / LOD1 | LOD d01 / dcull (m, lodBias 1) | Slots | Anchors |
|---|---|---|---|---|---|
| `Kit_DoorCloser_Arm` | 0.266 × 0.0212 × 0.03 | 1,112 / 1,112 | 1.5 / 20 | Prop_SteelBrown | spindle, elbow, elbow_top |
| `Kit_DoorCloser_Body` | 0.2923 × 0.0828 × 0.0506 | 3,236 / 1,067 | 1.5 / 20 | Prop_SteelBrown, Prop_Chrome | spindle, spindle_dir, mount |
| `Kit_DoorCloser_Forearm` | 0.2795 × 0.05 × 0.02194 | 940 / 940 | 1.5 / 20 | Prop_SteelBrown, Prop_Chrome | elbow, shoe_end |
| `Kit_DoorCloser_Shoe` | 0.03 × 0.04 × 0.031 | 922 / 922 | 1.5 / 20 | Prop_SteelBrown, Prop_Chrome | pivot, pivot_dir, mount |
| `Kit_DoorFrame_Steel` | 0.21 × 2.175 × 1.15 | 3,866 / 1,430 | 4 / never | Prop_SteelBrown, Prop_Rubber, Prop_Chrome | strike, strike_bored, hinge_axis, hinge_axis_dir, head_dust_a, head_dust_b, threshold, closer_shoe, exit_sign_p |
| `Kit_DoorFrame_Steel_Alu` | 0.21 × 2.175 × 1.15 | 3,866 / 1,430 | 4 / never | Prop_Aluminium, Prop_Rubber, Prop_Chrome | strike, strike_bored, hinge_axis, hinge_axis_dir, head_dust_a, head_dust_b, threshold, closer_shoe, exit_sign_p |
| `Kit_DoorFrame_Wood` | 0.21 × 2.175 × 1.15 | 3,152 / 1,260 | 4 / never | Prop_WoodWalnut, Prop_WoodOak, Prop_Brass | strike, hinge_axis, hinge_axis_dir, head_dust_a, head_dust_b, threshold |
| `Kit_DoorLeaf_Steel` | 0.06126 × 2.08 × 0.9925 | 3,930 / 1,572 | 4 / never | Door_Enamel, Prop_Aluminium, Prop_Chrome | escutcheon_s, sign_s, tagplate_s, kick_s, escutcheon_p, sign_p, tagplate_p, kick_p, latchbolt, latchbolt_dir, deadbolt, deadbolt_dir, latch_edge_bottom, latch_edge_top, damage_latch, hinge_axis, hinge_axis_dir, closer_mount_s |
| `Kit_DoorLeaf_SteelLite` | 0.06136 × 2.08 × 0.9925 | 4,586 / 1,834 | 4 / never | Door_Enamel, Prop_Aluminium, Prop_Chrome, Prop_Glass | escutcheon_s, sign_s, tagplate_s, kick_s, escutcheon_p, sign_p, tagplate_p, kick_p, latchbolt, latchbolt_dir, deadbolt, deadbolt_dir, latch_edge_bottom, latch_edge_top, damage_latch, hinge_axis, hinge_axis_dir, closer_mount_s, lite_centre |
| `Kit_DoorLeaf_Steel_PaintedMetal` | 0.06126 × 2.08 × 0.9925 | 3,930 / 1,572 | 4 / never | Painted_Metal, Prop_Aluminium, Prop_Chrome | escutcheon_s, sign_s, tagplate_s, kick_s, escutcheon_p, sign_p, tagplate_p, kick_p, latchbolt, latchbolt_dir, deadbolt, deadbolt_dir, latch_edge_bottom, latch_edge_top, damage_latch, hinge_axis, hinge_axis_dir, closer_mount_s |
| `Kit_DoorLeaf_Veneer` | 0.05886 × 2.08 × 0.9925 | 2,464 / 886 | 4 / never | Door_Veneer, Prop_Brass | rose_s, rose_p, latchbolt, latchbolt_dir, latch_edge_bottom, latch_edge_top, damage_latch, hinge_axis, hinge_axis_dir, closer_mount_s |
| `Kit_DoorLeaf_Veneer_Oak` | 0.05886 × 2.08 × 0.9925 | 2,464 / 886 | 4 / never | Prop_WoodOak, Prop_Chrome | rose_s, rose_p, latchbolt, latchbolt_dir, latch_edge_bottom, latch_edge_top, damage_latch, hinge_axis, hinge_axis_dir, closer_mount_s |
| `Kit_DoorLeaf_Ward` | 0.1104 × 2.08 × 0.9925 | 4,930 / 1,972 | 4 / never | Prop_WoodLaminate, Prop_Aluminium, Prop_Chrome | kick_s, kick_p, pull_s, push_p, armor_p, latch_edge_bottom, latch_edge_top, damage_latch, hinge_axis, hinge_axis_dir, closer_mount_s |
| `Kit_DoorNumberPlate` | 0.1 × 0.05 × 0.004237 | 480 / 480 | 1.5 / 15 | Prop_PlasticRed, Prop_KeyTagNo, Prop_Chrome | back, face, number, face_dir |
| `Kit_DoorNumberPlate_Blue` | 0.1 × 0.05 × 0.004237 | 480 / 480 | 1.5 / 15 | Prop_PlasticBlue, Prop_KeyTagNo, Prop_Chrome | back, face, number, face_dir |
| `Kit_DoorNumberPlate_White` | 0.1 × 0.05 × 0.004237 | 480 / 480 | 1.5 / 15 | Prop_PlasticWhite, Prop_KeyTagNo, Prop_Chrome | back, face, number, face_dir |
| `Kit_DoorSign` | 0.254 × 0.076 × 0.004337 | 664 / 664 | 2 / 25 | Prop_PlasticBlack, Prop_PlasticWhite, Prop_SignEngraved, Prop_Chrome | back, face, face_dir |
| `Kit_ExitDevice_Crossbar` | 0.9088 × 0.18 × 0.095 | 3,588 / 3,588 | 2 / 25 | Prop_Aluminium, Prop_Chrome | latch_case, hinge_case, bar_centre, push_point |
| `Kit_KeyCabinet` | 0.7188 × 0.46 × 0.0838 | 6,804 / 6,804 | 3 / 40 | Prop_SteelAlmond, Prop_PlasticWhite, Prop_Chrome, Prop_KeyTagNo | hook_r0_c0, hook_r0_c1, hook_r0_c2, hook_r0_c3, hook_r0_c4, hook_r0_c5, hook_r1_c0, hook_r1_c1, hook_r1_c2, hook_r1_c3, hook_r1_c4, hook_r1_c5, hook_r2_c0, hook_r2_c1, hook_r2_c2, hook_r2_c3, hook_r2_c4, hook_r2_c5, hook_r3_c0, hook_r3_c1, hook_r3_c2, hook_r3_c3, hook_r3_c4, hook_r3_c5, key_hook, door_hinge, door_hinge_dir, body_centre |
| `Kit_KeyHook` | 0.0068 × 0.01439 × 0.0257 | 922 / 922 | 1.5 / 12 | Prop_Brass | key_hook |
| `Kit_KeyHookBoard` | 0.3 × 0.4 × 0.04363 | 4,392 / 4,392 | 3 / 40 | Prop_WoodDark, Prop_Brass, Prop_KeyTagNo | hook_0, hook_1, hook_2, hook_3, hook_4, hook_5, hook_6, hook_7, key_hook |
| `Kit_KeyRing` | 0.02499 × 0.02499 × 0.0018 | 1,972 / 1,972 | 1 / 12 | Prop_Chrome | hook_contact, key_contact, tag_contact, centre, normal_dir |
| `Kit_KeyTag_Long` | 0.076 × 0.032 × 0.0042 | 1,038 / 1,038 | 1 / 15 | Prop_PlasticRed, Prop_KeyTagNo | hole, hole_dir, face, number, face_dir |
| `Kit_KeyTag_Long_Blue` | 0.076 × 0.032 × 0.0042 | 1,038 / 1,038 | 1 / 15 | Prop_PlasticBlue, Prop_KeyTagNo | hole, hole_dir, face, number, face_dir |
| `Kit_KeyTag_Long_White` | 0.076 × 0.032 × 0.0042 | 1,038 / 1,038 | 1 / 15 | Prop_PlasticWhite, Prop_KeyTagNo | hole, hole_dir, face, number, face_dir |
| `Kit_KeyTag_Rect` | 0.057 × 0.039 × 0.0042 | 1,038 / 1,038 | 1 / 15 | Prop_PlasticRed, Prop_KeyTagNo | hole, hole_dir, face, number, face_dir |
| `Kit_KeyTag_Rect_Blue` | 0.057 × 0.039 × 0.0042 | 1,038 / 1,038 | 1 / 15 | Prop_PlasticBlue, Prop_KeyTagNo | hole, hole_dir, face, number, face_dir |
| `Kit_KeyTag_Rect_White` | 0.057 × 0.039 × 0.0042 | 1,038 / 1,038 | 1 / 15 | Prop_PlasticWhite, Prop_KeyTagNo | hole, hole_dir, face, number, face_dir |
| `Kit_KeyTag_Round` | 0.038 × 0.048 × 0.0042 | 1,282 / 1,282 | 1 / 15 | Prop_PlasticRed, Prop_KeyTagNo | hole, hole_dir, face, number, face_dir |
| `Kit_KeyTag_Round_Blue` | 0.038 × 0.048 × 0.0042 | 1,282 / 1,282 | 1 / 15 | Prop_PlasticBlue, Prop_KeyTagNo | hole, hole_dir, face, number, face_dir |
| `Kit_KeyTag_Round_White` | 0.038 × 0.048 × 0.0042 | 1,282 / 1,282 | 1 / 15 | Prop_PlasticWhite, Prop_KeyTagNo | hole, hole_dir, face, number, face_dir |
| `Kit_Key_Zone` | 0.0022 × 0.026 × 0.058 | 2,188 / 2,188 | 1 / 12 | Prop_Brass | shoulder, tip, insert_dir, cuts_up, grip, ring_hole, ring_hole_dir |
| `Kit_Key_Zone_Nickel` | 0.0022 × 0.026 × 0.058 | 2,188 / 2,188 | 1 / 12 | Prop_Aluminium | shoulder, tip, insert_dir, cuts_up, grip, ring_hole, ring_hole_dir |
| `Kit_Lock_CylinderShell` | 0.044 × 0.044 × 0.0075 | 2,708 / 2,708 | 1.5 / 12 | Prop_Chrome, Prop_PlasticBlack, Prop_Brass | keyhole, keyhole_in, keyhole_up, collar_back |
| `Kit_Lock_CylinderShell_Brass` | 0.044 × 0.044 × 0.0075 | 2,708 / 2,708 | 1.5 / 12 | Prop_Brass, Prop_PlasticBlack, Prop_Brass | keyhole, keyhole_in, keyhole_up, collar_back |
| `Kit_Lock_Deadbolt` | 0.0125 × 0.03 × 0.0451 | 380 / 380 | 1 / 8 | Prop_Chrome, Prop_Aluminium | bolt_axis, end_face, throw_dir |
| `Kit_Lock_Escutcheon` | 0.0572 × 0.2032 × 0.008 | 3,248 / 3,248 | 1.5 / 12 | Prop_Chrome, Prop_PlasticBlack | knob_pivot, cylinder, keyhole, keyhole_in, keyhole_up |
| `Kit_Lock_Escutcheon_Brass` | 0.0572 × 0.2032 × 0.008 | 3,248 / 3,248 | 1.5 / 12 | Prop_Brass, Prop_PlasticBlack | knob_pivot, cylinder, keyhole, keyhole_in, keyhole_up |
| `Kit_Lock_Knob` | 0.054 × 0.054 × 0.0628 | 4,992 / 1,996 | 1.5 / 12 | Prop_Chrome | spindle, face, axis_out |
| `Kit_Lock_Knob_Brass` | 0.054 × 0.054 × 0.0628 | 4,992 / 1,996 | 1.5 / 12 | Prop_Brass | spindle, face, axis_out |
| `Kit_Lock_Latchbolt_Bored` | 0.016 × 0.02 × 0.03 | 476 / 476 | 1 / 8 | Prop_Chrome | bolt_axis, tip, plunger, throw_dir |
| `Kit_Lock_Latchbolt_Bored_Brass` | 0.016 × 0.02 × 0.03 | 476 / 476 | 1 / 8 | Prop_Brass | bolt_axis, tip, plunger, throw_dir |
| `Kit_Lock_Latchbolt_Mortise` | 0.01925 × 0.03 × 0.04 | 600 / 600 | 1 / 8 | Prop_Chrome | bolt_axis, tip, plunger, throw_dir |
| `Kit_Lock_Lever` | 0.133 × 0.024 × 0.0536 | 3,974 / 1,588 | 1.5 / 15 | Prop_Chrome | spindle, grip_press, grip_end, axis_out |
| `Kit_Lock_Lever_Brass` | 0.133 × 0.024 × 0.0536 | 3,974 / 1,588 | 1.5 / 15 | Prop_Brass | spindle, grip_press, grip_end, axis_out |
| `Kit_Lock_Plug` | 0.0115 × 0.0115 × 0.03 | 1,884 / 1,884 | 1 / 8 | Prop_Brass, Prop_PlasticBlack | keyhole, keyhole_in, keyhole_up, pin_1, pin_2, pin_3, pin_4, pin_5, pin_6 |
| `Kit_Lock_Rose` | 0.07 × 0.07 × 0.01 | 2,728 / 2,728 | 1.5 / 12 | Prop_Chrome, Prop_PlasticBlack | spindle |
| `Kit_Lock_Rose_Brass` | 0.07 × 0.07 × 0.01 | 2,728 / 2,728 | 1.5 / 12 | Prop_Brass, Prop_PlasticBlack | spindle |
| `Kit_Lock_StrikeBored` | 0.056 × 0.124 × 0.0174 | 674 / 674 | 1.5 / 12 | Prop_Chrome, Prop_PlasticBlack | plate, latch_opening, lip_tip, detach_dir |
| `Kit_Lock_StrikeBored_Brass` | 0.056 × 0.124 × 0.0174 | 674 / 674 | 1.5 / 12 | Prop_Brass, Prop_PlasticBlack | plate, latch_opening, lip_tip, detach_dir |
| `Kit_Lock_StrikeMortise` | 0.056 × 0.2 × 0.0254 | 988 / 988 | 1.5 / 12 | Prop_Chrome, Prop_PlasticBlack | plate, latch_opening, deadbolt_opening, lip_tip, detach_dir |
| `Kit_MiniBlind_Lowered` | 2.4 × 1.308 × 0.07625 | 6,376 / 1,594 | 3 / 30 | Prop_SteelAlmond, Prop_PlasticWhite | blind_rail, wand_tip, tassel, bottom_rail |
| `Kit_MiniBlind_Raised` | 1.55 × 1.06 × 0.07625 | 2,572 / 876 | 3 / 30 | Prop_SteelAlmond, Prop_PlasticWhite | blind_rail, wand_tip, tassel, stack_bottom |
| `Kit_WindowFrame_Alu` | 1.55 × 1.8 × 0.21 | 2,190 / 1,050 | 4 / never | Prop_Aluminium, Prop_Rubber | glass_slab, stop_l, stop_r, stop_t, stop_b, pocket_l, pocket_r, pocket_t, pocket_b, tooth_band_l, tooth_band_r, tooth_band_t, tooth_band_b, face_a, sill_plant_a, sill_plant_b, floor_a, floor_b, blind_rail |
| `Kit_WindowFrame_Steel` | 1.55 × 1.8 × 0.21 | 4,054 / 1,458 | 4 / never | Prop_SteelBrown, Prop_Rubber | glass_slab, stop_l, stop_r, stop_t, stop_b, pocket_l, pocket_r, pocket_t, pocket_b, tooth_band_l, tooth_band_r, tooth_band_t, tooth_band_b, face_a, sill_plant_a, sill_plant_b, floor_a, floor_b, blind_rail |
| `Kit_WindowFrame_Steel_Enamel` | 1.55 × 1.8 × 0.21 | 4,054 / 1,458 | 4 / never | Door_Enamel, Prop_Rubber | glass_slab, stop_l, stop_r, stop_t, stop_b, pocket_l, pocket_r, pocket_t, pocket_b, tooth_band_l, tooth_band_r, tooth_band_t, tooth_band_b, face_a, sill_plant_a, sill_plant_b, floor_a, floor_b, blind_rail |
| `Kit_WindowFrame_Wood` | 1.6 × 1.828 × 0.242 | 2,322 / 1,044 | 4 / never | Prop_WoodWalnut, Prop_Rubber | glass_slab, stop_l, stop_r, stop_t, stop_b, pocket_l, pocket_r, pocket_t, pocket_b, tooth_band_l, tooth_band_r, tooth_band_t, tooth_band_b, face_a, sill_plant_a, sill_plant_b, floor_a, floor_b, blind_rail |

**Budgets against `10_spec.md` §9:** every asset is within ±15 % except `Kit_DoorCloser_Shoe` 922 (spec 500; L1), `Kit_KeyHook` 922 (spec 600; L2) and `Kit_WindowFrame_Alu` 2,190 (spec 2,800, 22 % under; window track's pass-4 module). LOD1 counts equal LOD0 where kitlib protects thin parts or `lod1_keep_all` applies; there the LODGroup's value is the cull.

**Origins, fronts and anchors** are unchanged from `10_spec.md` §1.2–§5.5 (part frame: origin at the pivot or mount, front = Unity +Z; door root D; window root W). New sidecar fields: `grainAxis` (oak and ward leaves), `swingClearWorstZ` (exit device), `hungPose` v2 (hosts).

## 5. Integration contract for the map chat (from `10_spec.md` §6, corrected)

Everything in `10_spec.md` §6.1–§6.7 stands, with these corrections. The API is `FrontRoomsKitLibrary.Spawn(name, parent, localPosition, localRotation, scale, colliders: false, label)`; anchors come from `GetInfo(name).TryAnchor`.

### 5.1 Doors
1. **Option A, single swing,** is already in the map (`swing = FixedSwing(a, b)`, `FrontRoomsMapWorld.cs:1308`). Expose `S_sign = −door.swing` to the dresser.
2. **A2 pivot (required for the kit door, critic M7):** `Door hinge` localPosition += `closed · (S_sign · 0.0295, 0, 0.0098)`; `Door leaf` (collider) localPosition (0, 1.04, 0.50) → (−S_sign · 0.0295, 1.04, 0.4902). The collider's closed pose and size (0.05 × 2.08 × 0.98) do not change. **If the map refuses A2,** the visual facade must turn the `Leaf rig` about the A2 axis itself every LateUpdate from the hinge angle; without one of the two the leaf digs into the lining from about 7.5° (`10_spec.md` §1.3).
3. Spawn recipe as `10_spec.md` §6.2, with the `Door leaf` renderer **disabled, not destroyed,** plus the 0.10 × 2.08 × 0.96 `Leaf shadow` proxy. Members per §2.1. **The oak leaf is now its own asset** (`Kit_DoorLeaf_Veneer_Oak`), with the same anchors as `Kit_DoorLeaf_Veneer`.
4. The bored or mortise latch is held back while the leaf is within 8° of shut, as in the harness; the latch and deadbolt are separate parts driven by the facade.
5. Steel frames: the saddle now takes the frame finish (dark bronze; aluminium on `Kit_DoorFrame_Steel_Alu`).

### 5.2 Lock point and effects
1. **`LockPoint` → the keyhole (critic M8):** for locked doors return door root (±0.0315, 1.000, 0.920), i.e. `DoorHandleProud` 0.0065 instead of 0.03 (`FrontRoomsMapWorld.cs:2339-2347` today returns ±0.055). `DoorUnlocked`'s point and the unlock sound then come from the keyway.
2. **Collider vs visual faces (critic L5):** the collider faces are at ±0.025, the visual leaf faces at ±0.022, so the collider stands 3 mm proud of each visual face. Place hits, dust, decals and sparks from the kit anchors (`latch_edge_*`, `damage_latch`, `head_dust_a/b`, the escutcheon `keyhole`), never from the collider surface. (`10_spec.md` §1.4's "the collider lies inside the visual leaf" holds only in Y and Z.)
3. Key seating: at full insertion the key's `shoulder` equals the escutcheon's `keyhole` anchor (0.0095 off the leaf face); the shoulder face rests on the core face. Insertion is along `keyhole_in`, cuts along `keyhole_up`; the plug and key turn 90° together about the keyhole axis (`Kit_Lock_Plug` `motion`).

### 5.3 Head-dip shot (`FrontRoomsShotTimings.Unlock`, critic H7; proposal, renders VL199)
- Pose P = keyhole + R_y(25°) · (pose P on the door normal − keyhole), turned toward the door centre (eye still 1.37 m, 0.45 m out).
- Push in along that yawed line: 0.30 m at FOV 56 (key 50 % in), 0.24 m at FOV 52 (key home), end 0.20 m at FOV 50 (key and plug turned 90°, deadbolt retracted). The cylinder, key and knob then fill about a third of the frame.
- No key roll: the blade enters the vertical keyway only at 0°.
- `ShotLightIntensity` (`FrontRoomsShotTimings.cs:56`) is still unread; the renders treat it as 0.6 of illuminance at the lock. Whoever wires it decides the unit.
- Pull-face caution from `10_spec.md` §3.4 stands.

### 5.4 Keys (critic C2)
1. `Key · zone {id}` becomes an unscaled empty; hosted keys **do not spin** and have no emission; pickup stays horizontal ≤ 0.9 m.
2. **Hung pose v2** (in every host's sidecar `hungPose`): ring yaw **120°** relative to the host, lift per host (0.0011–0.0014 m), key twist −30°, tag twist **150°**; the tag hangs on the wall side and the brass key faces the room.
3. **Host defaults:**
   - Level 0, procedural rooms (case C): `Kit_KeyHookBoard` on the longest plain wall (it reads at 6 m), not the single hook.
   - Office: `Kit_KeyCabinet` on a perimeter wall.
   - `Kit_KeyHook` (single hook) only at module key spots whose framed sight line is ≤ 3 m; past about 2 m a lone hook is a few pixels.
   - Surface keys only in the over-edge pose with `Kit_KeyTag_Long`.
4. The key cell's lamp is forced Steady (`10_spec.md` §4.3).
5. Until `Prop_KeyTagNo` exists, tags and plates are blank; the HUD carries the number.

### 5.5 Windows
- Member map (critic M2): **W-RN → `Kit_WindowFrame_Steel`** (dark bronze, to match the Run doors' frames), not `_Steel_Enamel`. W-L0 wood, W-OF steel, W-EX aluminium as before. The facade in main (r6) maps only Lobby and Office today.
- Everything else as `window_landing/03_contract_map.md` (R1/R2, optional T).

### 5.6 LODs and renderers
- Every kit now carries `<NAME>_LOD0` and `<NAME>_LOD1` renderers under one LODGroup; only one draws. Anything that walks renderers (masks, the RT bridge, outline effects) must take LOD0 only (G14-K1).
- No colliders on any kit part; no Light components; names `Door hinge*`, `Door leaf`, `Window pane {a}-{b}`, `Key · zone {id}` unchanged; no kit child takes a hazard name (`10_spec.md` §1.6, §6.7).

## 6. Still open, with owners

| # | Open item | Owner | Gate |
|---|---|---|---|
| 1 | Door and key facade (`FrontRoomsInteractableKit.Door.cs`, `.Key.cs`), leaf rig, closer solver, lock-part driver (`10_spec.md` §6.1, §6.3) | visual chat | needs the map hooks below; Red's DW proposal |
| 2 | A2 pivot (or facade pivot), `LockPoint` → keyhole, dress calls guarded like the window's (`:1369`), key empty + host spawn, host defaults, H7 shot constants | map chat | contract §5 |
| 3 | P-4 materials `Door_Enamel` (luminance ≈ 0.66 for L0 dim, M3), `Prop_KeyTagNo`, `Prop_SignEngraved` | visual chat + Red | approval |
| 4 | Satin chrome (H1): tune `Prop_Chrome` or add `Prop_SatinChrome` (new slot) | visual chat | approval for a new slot |
| 5 | `DoorVeneer_A` retexture (corduroy stripes); U-grain textures library-wide (Oak, Teak, Cherry, Dark, Ebony, Laminate) vs kitlib's V-grain UVs | visual chat | — |
| 6 | Steel mottle and gloss (M5), walnut contrast and contact shadow (M6) | visual chat | — |
| 7 | LOD2 (P-1 in kitlib/build_asset) | visual chat | approval |
| 8 | Window profile pass A3 (after T), A9 (apron returns), A11 (after S2) | interactables G4 | map chat T / S2 |
| 9 | Intact glass read (H6), real fracture set (H8) | glass tracks W1 / G14, GD3 | — |
| 10 | Exit-device rim bolt (moving part) and frame rim strike (new asset) | interactables | EX-F gets a map door |
| 11 | `Kit_KeyHookBoard` vs `Kit_KeyRack` name (F6) | Red | — |
| 12 | Level 0 lit-doorway readability at 6 m (+1.35 to +1.63 vs the +1.5 bar) and L0 dim (+0.8 vs +1) | Red / visual chat | item 3 |
| 13 | 平面视觉's K-sheets (K46–K80) show the pre-fix models for the 18 changed kits | 平面视觉 | after promotion; PNGs in `W/proj_int/Verification/threeview_fix/` |
| 14 | door-break track: its damage stages build from `interact_door_leaf_veneer.build_leaf`; an oak stage must pass `ugrain=True` and call `interact_door_common.u_grain(kit)`, not slot-swap | door-break | — |
| 15 | T10 cost check (seed 4242 frames) needs the facade in the map | visual chat | item 1 |

## 7. Files to promote into the real project

**Nothing else is needed:** no `.meta`, no C#, no material. The modules are already in main (written directly, allowed). The harness, `FrontRoomsIntFixBatch.cs` and the other tools are **clone-only** and must not be promoted.

- **Payload:** `W/int_work/fix/payload/Assets/Resources/Props/Models/` = 102 files: 50 `.fbx` + 52 `.json`, sha1 list `W/int_work/fix/payload/SHA1SUMS.txt`, identical to `W/proj_int` at 01:1x.
- **Bases:** main `22bb75f` (working tree checked 2026-10-08 01:2x: no Props/Models file changed since). Full list with payload sha1, main base sha1 and GUID: `fix_pass/promote_list.tsv`.
- **Metas:** keep main's `.meta` for every path (all 57 assets already exist in main; no new path, no second GUID). Red's Unity rewrites the FBX metas' material remaps on import, as it does for any kit FBX.
- **`Kit_KeyCabinet`:** the payload FBX replaces main's faulty build `f7e570c7` (codex audit F5) and keeps GUID `13c85fe1e557741c982832fc03970bc8`. It is a fresh build of the G3-fixed module (plus LOD1), so its sha1 is not the old clone's `63da943c`.
- **Gate before the 3 window FBX** (`Kit_WindowFrame_Steel`, `_Steel_Enamel`, `_Alu`): the window facade is live in main, so run the window-landing harness (`FrontRoomsWindowTF.RunAll`, `window_landing/FrontRoomsWindowTF.r5.cs.txt`) in a clone with them, or promote everything else first and hold those three. Their modules' own clear-zone asserts passed, and the three-views match the 10-04 design.
- **Apply script:** none is written by this stage (no permission into main from here); the merge stage builds the v2 package (`W/apply/README.md`) from this payload and list.

Per asset (payload sha1 first 8 / main base sha1 first 8):

| Asset | `.fbx` | `.json` | main `.fbx.meta` GUID (keep) |
|---|---|---|---|
| `Kit_DoorCloser_Arm` | PROMOTE `ac89f0f3` (main `7ee82570`) | PROMOTE `341afea9` | `23c8502878cc642e6a6b3ee11157df5d` |
| `Kit_DoorCloser_Body` | PROMOTE `a08024e5` (main `a2f37574`) | PROMOTE `cb863998` | `e18f863f9a85646aea2a367d40c71a95` |
| `Kit_DoorCloser_Forearm` | PROMOTE `dac97ca4` (main `b24d656b`) | PROMOTE `49977098` | `d93c9e51d660042328a0eabd6732c095` |
| `Kit_DoorCloser_Shoe` | PROMOTE `3a67b733` (main `2bf555cf`) | PROMOTE `23f7d032` | `03076aba018b44d54b80bb135d3a8922` |
| `Kit_DoorFrame_Steel` | PROMOTE `f19ad102` (main `d4523608`) | PROMOTE `5ab0545d` | `099280317f21d4ed7a84fa0242e47608` |
| `Kit_DoorFrame_Steel_Alu` | PROMOTE `216c41a5` (main `89f4bd72`) | PROMOTE `d92556b1` | `f8ec37229f24748228d140cbafb393de` |
| `Kit_DoorFrame_Wood` | PROMOTE `cb469c14` (main `443f898b`) | keep (identical) | `bcecb5c4705ee43b1b6cbc195f73dd32` |
| `Kit_DoorLeaf_Steel` | keep main's (same content) | keep (identical) | `0f04d403ea7634d2bbb36c3b6f582247` |
| `Kit_DoorLeaf_SteelLite` | keep main's (same content) | keep (identical) | `4e15e97462bec4818986d49e34b209c7` |
| `Kit_DoorLeaf_Steel_PaintedMetal` | keep main's (same content) | keep (identical) | `27f82ba6f8bae4d81bc3a44a49b85def` |
| `Kit_DoorLeaf_Veneer` | keep main's (same content) | keep (identical) | `fbb6e0d8baccf4f62ac581cad7046f59` |
| `Kit_DoorLeaf_Veneer_Oak` | PROMOTE `7f739bc5` (main `9f2c3a31`) | PROMOTE `d13ec77b` | `016ec128d08744905b62a0146d7fdcc8` |
| `Kit_DoorLeaf_Ward` | PROMOTE `b93701b8` (main `6a58ff2b`) | PROMOTE `d5c6d0e9` | `435700f5271484a0bb45e252c0d0634c` |
| `Kit_DoorNumberPlate` | PROMOTE `75b30fb1` (main `07424526`) | PROMOTE `c22beae6` | `bd064aac0d25640b8bd92b4b9c3611ff` |
| `Kit_DoorNumberPlate_Blue` | PROMOTE `d4db7b74` (main `a2c31a6d`) | PROMOTE `e45ecffd` | `c5c1ee42177734353b30644d69b39540` |
| `Kit_DoorNumberPlate_White` | PROMOTE `80e206bc` (main `6e5d195a`) | PROMOTE `b308a61f` | `819657d15e8b84f8d865dffbe57d970c` |
| `Kit_DoorSign` | PROMOTE `f548487d` (main `ac49d16b`) | PROMOTE `8a15594f` | `092161d00c42a4446bf28e882291aa99` |
| `Kit_ExitDevice_Crossbar` | PROMOTE `efba691f` (main `28d6297b`) | PROMOTE `3ea5975a` | `8a76ea7d870184e198a0fe791d261cbc` |
| `Kit_KeyCabinet` | PROMOTE `29d8332f` (main `f7e570c7`) | PROMOTE `028d8712` | `13c85fe1e557741c982832fc03970bc8` |
| `Kit_KeyHook` | PROMOTE `84709253` (main `d3c0c2d9`) | PROMOTE `62082ace` | `182e0588fd3c5476bb8e9c8f38fe3b52` |
| `Kit_KeyHookBoard` | PROMOTE `242f399c` (main `22f3ae0e`) | PROMOTE `cb54ff0d` | `f81dc1c71214641cf912404914e69838` |
| `Kit_KeyRing` | PROMOTE `a0c1bdc0` (main `cb436f72`) | PROMOTE `c15ad4b9` | `8cbc4dde5ac664f33975d2d1b83182f6` |
| `Kit_KeyTag_Long` | PROMOTE `fc655f7f` (main `4c96e206`) | PROMOTE `62800829` | `a73110f13d117426589de3f7c70362b5` |
| `Kit_KeyTag_Long_Blue` | PROMOTE `ac9e2ac9` (main `9659fed3`) | PROMOTE `7db30d57` | `42f7b51a7fb9e4eaaba2a555f95ed546` |
| `Kit_KeyTag_Long_White` | PROMOTE `a27e9594` (main `26dd25bf`) | PROMOTE `d3bec018` | `cbd64996098c842429ce6dbaff1ab34e` |
| `Kit_KeyTag_Rect` | PROMOTE `4c325d3e` (main `46e31bd0`) | PROMOTE `1e30270d` | `39b73c6d8eb6e4d2189a139dd767856e` |
| `Kit_KeyTag_Rect_Blue` | PROMOTE `89e733f6` (main `1ecf46ff`) | PROMOTE `04e319f1` | `6bf1d6fa1d73147c28cbf727994bbf67` |
| `Kit_KeyTag_Rect_White` | PROMOTE `dc28aafa` (main `745afed1`) | PROMOTE `47fe5718` | `1eb3af28bbde841f89c60f52db9a860f` |
| `Kit_KeyTag_Round` | PROMOTE `c3834d78` (main `d30dacd9`) | PROMOTE `00177b40` | `1c47cdbb11bf842ff9f1fe9a162e8c5c` |
| `Kit_KeyTag_Round_Blue` | PROMOTE `04758eb6` (main `413b00b2`) | PROMOTE `d860288f` | `4e935b10a43e94ae8a5d7bcac61d6c70` |
| `Kit_KeyTag_Round_White` | PROMOTE `023b09ed` (main `866d4a0c`) | PROMOTE `7a16afd8` | `345e0cf11698e4be6baf16a982c050ed` |
| `Kit_Key_Zone` | PROMOTE `931c66ff` (main `93f0101b`) | PROMOTE `c897cf88` | `c3c16836352134ba69730820ac3a5cdb` |
| `Kit_Key_Zone_Nickel` | PROMOTE `ec84915b` (main `3374895d`) | PROMOTE `6b1fee0c` | `94bfd45288181442a9ccde09371e7e5d` |
| `Kit_Lock_CylinderShell` | PROMOTE `04879e14` (main `4b9d3773`) | PROMOTE `48de614d` | `1e164b737d64f47c18c2fb9c0518c9ae` |
| `Kit_Lock_CylinderShell_Brass` | PROMOTE `fa5fc05e` (main `117bf750`) | PROMOTE `732ec888` | `abc278b4e103040b38fc82e330a16db6` |
| `Kit_Lock_Deadbolt` | PROMOTE `10902025` (main `871bd45a`) | PROMOTE `d05bfc30` | `357225c236aba46e0b99061a26ee430e` |
| `Kit_Lock_Escutcheon` | PROMOTE `5960e886` (main `fe747558`) | PROMOTE `3eeb426c` | `c79330c6618ac4fefa99d9229675fcec` |
| `Kit_Lock_Escutcheon_Brass` | PROMOTE `d866211d` (main `fd66e95b`) | PROMOTE `0a47d48b` | `897e46981658e4283b85d4b3c91ea0a2` |
| `Kit_Lock_Knob` | PROMOTE `edcd6744` (main `141a5474`) | PROMOTE `e86edc0b` | `efebceb4884b64ebf963eb76e137da81` |
| `Kit_Lock_Knob_Brass` | PROMOTE `006e6057` (main `21780942`) | PROMOTE `106659ec` | `593117186185a4233883f1f96e381e8a` |
| `Kit_Lock_Latchbolt_Bored` | PROMOTE `ba49ceff` (main `d08510ae`) | PROMOTE `36dd2bed` | `e00ee74d7f5c9408bafaff8c5fe878e5` |
| `Kit_Lock_Latchbolt_Bored_Brass` | PROMOTE `861465e4` (main `fd188416`) | PROMOTE `b3a0c11a` | `df25d646b88b0401ea9016b463a6c5f4` |
| `Kit_Lock_Latchbolt_Mortise` | PROMOTE `2b7482ca` (main `890d4623`) | PROMOTE `f09218d8` | `606ffe9787b0d47919a40955c5338922` |
| `Kit_Lock_Lever` | PROMOTE `04d57a40` (main `4be0f4aa`) | PROMOTE `6a662572` | `afa27d1c683284e8cade53c67b9e67cc` |
| `Kit_Lock_Lever_Brass` | PROMOTE `99e84881` (main `efe35751`) | PROMOTE `97398bff` | `897a48ccc4a2f49e4b02f898dd5595aa` |
| `Kit_Lock_Plug` | PROMOTE `560e250c` (main `8782a534`) | PROMOTE `23825016` | `c6a59cc9735c942519b0929c44146021` |
| `Kit_Lock_Rose` | PROMOTE `dd7c199a` (main `f36958c4`) | PROMOTE `50d429a7` | `9e1cb801f401942368d70459a5af8959` |
| `Kit_Lock_Rose_Brass` | PROMOTE `d6058ef9` (main `79b47b54`) | PROMOTE `e9318920` | `a05e855c65ea94e619b5562924dbe8ba` |
| `Kit_Lock_StrikeBored` | PROMOTE `feb7fe18` (main `e1dc943a`) | PROMOTE `78fb7bd6` | `1208615880062400abdf026b7e2c1082` |
| `Kit_Lock_StrikeBored_Brass` | PROMOTE `7895ebae` (main `70373bb5`) | PROMOTE `4731c61f` | `bd459404fbc2346b48c55bfee6daba89` |
| `Kit_Lock_StrikeMortise` | PROMOTE `1ca9f5d9` (main `5a00506c`) | PROMOTE `604ef741` | `a36c40361be304864aecaf3e20b46c30` |
| `Kit_MiniBlind_Lowered` | keep main's (same content) | PROMOTE `be1ad77f` | `75e18a03b47b404696ced4269a2d17cf` |
| `Kit_MiniBlind_Raised` | keep main's (same content) | PROMOTE `8559f008` | `1202dd8dc88f04d88ae7e152cb029efe` |
| `Kit_WindowFrame_Alu` | PROMOTE `99a2814c` (main `e9776aa1`) | PROMOTE `363217d2` | `935afd3ab66be498f882dea647321d3a` |
| `Kit_WindowFrame_Steel` | PROMOTE `9bbda940` (main `f2b4b53f`) | PROMOTE `a0c45d7a` | `c96f7893ea2f94c6b92c29c791f315c7` |
| `Kit_WindowFrame_Steel_Enamel` | PROMOTE `700a16ce` (main `82e1637c`) | PROMOTE `c1dbc6e6` | `195d766c9b6ed483a82e10aa3931a04e` |
| `Kit_WindowFrame_Wood` | keep main's (same content) | PROMOTE `c87cea85` | `1cdcbda9ab3a648c396413a8cb0fcbdb` |

## 8. Verification log

Placed in **FRONTROOMS · VISUAL VERIFICATION LOG** (`2595:6093`, page `2099:76`); the section grew to 51 rows (63,320); the cover's D1 legend now reads `VL009–200 · 28 checks`; rows in `Documentation/VERIFICATION_LOG.md` §3.

| VL | Frame | Check | Verdict | Images |
|---|---|---|---|---|
| VL196 | `2855:6357` | Fix-pass models, before/after | PASS | `intfix_threeview_parts.jpg`, `intfix_threeview_doors.jpg`, `intfix_threeview_windows.jpg` |
| VL197 | `2855:6368` | Small-part LODs hold shape | PASS | `intfix_lod1_keepall_before_after.jpg` |
| VL198 | `2855:6377` | Key faces the room on hooks | FLAG | `intfix_keys_4m_6m.jpg`, `intfix_key_crops.jpg` |
| VL199 | `2855:6387` | Yawed head dip shows the key | PASS | `intfix_dip_strip.jpg` |
| VL200 | `2855:6396` | Locked door vs a lit doorway | FLAG | `intfix_readability_sheet.jpg` |

Not logged as new slides (same verdict as VL155–165): the gap, swing, family, windows and fracture sheets of this run (`images/intfix_*`).

## 9. Files

- Modules (main): `Tools/Blender/frontrooms_kit/assets/interact_*.py` (new: `interact_door_leaf_veneer_oak.py`; helpers `u_grain` and `lod1_keep_all` in `interact_door_common.py`).
- This folder: `30_final.md`; `fix_pass/` (`assets.md`, `promote_list.tsv`, `promote_table.md`, `lod_probe.tsv/.json`, `lod1_check_before_keepall.json`, `lod1_check_keepall.json`, `t8_report.json`, `t8_report_seat002.json`, `hung_pose.json`, `fbx_diff.json`, `metrics_fix.json`); `harness/` (`FrontRoomsInteractablesLookdev.fixpass.cs.txt`, `FrontRoomsIntFixBatch.cs.txt`, `int_post_fix.py.txt`, `fix_sheets.py.txt`, `t8_keyfit.py.txt`, `hung_pose_check.py.txt`, `lod1_check.py.txt`, `fbx_content_diff.py.txt`, `asset_table.py.txt`); `images/intfix_*.jpg`, `images/renders_fix/`.
- Logs: `W/logs/int_fix_*.log`.
