# Build G3: keys, tags, hosts and door signage

Status: BUILT and checked (2026-10-03). Third pass 22:0x–23:0x, after Codex copied G3 into main: one model fix (the key cabinet), the cut-off checks finished, main compared file by file. §0 is this pass; §1–§7 are the second run (16:4x–17:5x), updated where this pass changed them.
Spec: `10_spec.md` §2.4, §3.2, §3.3, §4, §9.3, §10.0, §10.3.

- Built into the private clone only: `scratchpad/proj_int/Assets/Resources/Props/Models/`.
- Previews and check renders: `scratchpad/interact_previews/G3/`. Check scripts and their JSON reports: `scratchpad/g3/`. Verification images for the log: `research/interactables/images/g3/` (§8).
- Nothing under the real project's `Assets/` was touched. Unity was not run. `kitlib.py` and `build_asset.py` were not edited.
- `scratchpad` = `/private/tmp/claude-501/-Users-redwang-Desktop-ArtCenter-Fall26T7-EGAM-401A-01-Individual-Game-Project/5656cffd-bc90-45f6-86a3-09b26549df8d/scratchpad`.

## 0. Third pass (after Codex): main's state, corrections, what to promote

### 0.1 What main holds now

- **Codex commit `8ef5b64` (19:10) copied all 42 G3 files** (21 assets with their variants, FBX + JSON) from `proj_int` into `Assets/Resources/Props/Models/`. Each one is byte-identical (md5) to this workflow's second-run build (16:45–17:08). The codex audit says the same (`research/codex_audit/00_main_state.md` §3.3).
- **Metas:** `proj_int` has no `.meta` files. Red's Unity made all G3 metas at 18:57 with fresh GUIDs (audit §3.3). **Main's GUIDs are the canonical ones.** Example: `Kit_KeyCabinet.fbx.meta` GUID `13c85fe1e557741c982832fc03970bc8`.
- **The desk keyboard is safe.** Main's `Kit_Keyboard.fbx/.json` are the desk keyboard (last changed in `9abf01a`, 2026-10-02), as used by `FrontRoomsOfficeKit.cs:47`. The key board ships only as `Kit_KeyHookBoard` (§1).
- **Nothing is live.** No script in `Assets/Scripts` or `Assets/Editor` names a G3 asset (grep, 22:1x). The Level Designer palette and `FrontRoomsMapWorld.Prewarm()` (`FrontRoomsMapWorld.cs:1935`) do read every sidecar (audit §3.3).
- **Slots without a Unity material.** The importer remaps a slot only when `Resources/Surfaces/<slot>.mat` exists (`FrontRoomsKitImporter.cs:39-45`). Main has no `Prop_KeyTagNo.mat`, `Prop_SignEngraved.mat` or `Run_ExitSign_Dead.mat` (P-4 is not approved). So in Unity today the tag and plate inserts, the board and cabinet labels, the sign face and the dead exit face use the FBX's own plain material: blank paper, a flat dark plate and flat grey. Every other G3 slot exists in `Resources/Surfaces/`.
- **Codex's LOD change does not touch G3.** `FrontRoomsKitImporter.cs:73-91` now turns a sidecar's `lodDistances` into LOD switch heights, but only on a model with a LODGroup. No G3 asset has one (no `LOD1`, §2).

### 0.2 Corrections made in this pass

1. **Key cabinet: black notches at the corners (FAIL → FIXED).**
   - Seen in `G3_cabinet_34_v2.png` and `G3_cabinet_corner_close.png`: a black triangle at every corner of the body's front lip, the door pan and the card holder.
   - Cause: `profile_sweep` offsets the path with `offset_poly`, which moves each corner-arc vertex along its own normal. The lip reaches 9 mm in (door pan 8.5 mm, card holder 5.5 mm) past a 2 mm corner radius, so the arc folds through its centre and those faces turn inside out.
   - Fix: a new helper `rrect_sweep` (`interact_key_common.py:676`) builds every ring as the exact offset curve, a rounded rectangle of radius r − inset, never below 0.3 mm (the inside bend of the fold). The cabinet uses it for the shell, the door pan and the card holder (`interact_key_cabinet.py:116, 135, 139`).
   - Same 6,804 triangles; the sidecar is byte-identical (bounds, anchors, hung pose). Only the corner vertices moved.
   - Regression: `profile_sweep` keeps its old output (it now calls the shared `_bridge_rings`, `interact_key_common.py:695`). Six other G3 modules rebuilt after the change (`Kit_KeyHookBoard`, `Kit_DoorSign`, `Kit_ExitSign`, `Kit_Key_Zone`, `Kit_KeyTag_Rect`, `Kit_DoorNumberPlate`) have the same vertices, faces and slots as main's FBX (`scratchpad/g3/geo_cmp.py`, all "SAME GEOMETRY"). Their sidecars are identical too.
   - The other G3 sweeps were checked for the same fault: the board (radius 4 mm, deepest inset 3.2 mm), the exit-sign housing (10 mm / 4.4 mm) and face frame (6 mm / 2 mm), and the sign (square corners) are all safe.
2. **The T8 "face" picture was blank.** `G3_T8_face.png` (16:47) put the camera inside the key's bow, so it shows only a brass gradient. The numbers were right; the picture was useless. It is replaced by `G3_T8_key_in_plug_0deg.png` and `_90deg.png` (key seated in G2's plug and shell, 3/4 view).
3. **The flat-pose close-up was cut off** at 17:54 by the usage limit (sample 57/96). It was re-rendered with the final pose as `G3_flat_desk_close_v2.png`. The 17:20 `G3_flat_desk_close.png` shows an older pose (ring standing upright); ignore it.
4. **Checks finished after the 17:31 note** (they ran at 17:46–17:48 but never reached the note) are now in §4: the hung pose on every host with every tag, T8b (blade-only clearances and pin gaps) and the final flat pose.

### 0.3 What to promote (main is the base)

- **Only `Kit_KeyCabinet.fbx` changed.** Copy `scratchpad/proj_int/Assets/Resources/Props/Models/Kit_KeyCabinet.fbx` (22:15, sha1 `6f35795b…`) over main's (sha1 `f7e570c7…`). **Keep main's `Kit_KeyCabinet.fbx.meta`** (GUID `13c85fe1e557741c982832fc03970bc8`). Its JSON is byte-identical to main's, so leave it.
- The other 41 G3 files in main are this workflow's final versions. Leave them and their metas as they are.
- **Module files changed in main's `Tools/`** (G3's own files, made by this workflow earlier today): `interact_key_common.py` (adds `rrect_sweep` and `_bridge_rings`) and `interact_key_cabinet.py` (uses it). Red's commit `03c43ed` (22:48) already holds both. Blender also rewrote their `.pyc` caches in `assets/__pycache__/` (tracked in git) when it imported them; that is the normal build by-product. No other file in `Tools/` was touched.
- `proj_int` was **not** re-synced from main. G4 was building into it while this pass ran, and the G3 files in it equal main's (md5, 22:0x). G3 needs no Unity file from main, because the builds use main's own `kitlib.py` and `build_asset.py` (the scripts run from main's `Tools/`).

### 0.4 Codex audit

- `research/codex_audit/20_findings.md` did not exist when this pass ended (checked 22:58). `00_main_state.md` §3.3 asks the interactables workflow to "finish G2/G3, the renders and the critics" and "merge the verified FBX/JSON over Codex's copies and keep main's metas". §0.3 does that for G3.
- The audit's kit review (`codex_audit/10_review_kits.md`, 22:54) lists three G3 items:
  - **F4** (the four slots with no material in main, §0.1): owed to P-4. The audit says **not** to add placeholder materials in main. G3 adds none.
  - **F5** (`Kit_KeyCabinet` in main is the faulty build): fixed in this pass; promote as §0.3 says. The audit measured the same thing: main's FBX sha1 `f7e570c7…`, the fix `6f35795b…` (160,748 B), JSON byte-identical.
  - **F6** (`Kit_KeyHookBoard` vs `Kit_KeyRack`): Red's call (§7 item 12).
  - It also confirms what §0.1 says: no duplicate GUIDs, `Kit_Keyboard` unchanged, every G3 LOD0 within ±15 % of §9, `noCollider` on all, and the `null` third entry in the exit signs' `lodDistances` (§9.3 "—") reads as 0 = never cull. The exit sign now passes to the exit-sign workflow, so G3 leaves that `null` as it is.
- **Audit item "Name clash: `Kit_ExitSign*`"** (§3.3). The exit-sign workflow (`research/exit_sign/`) is researching the period sign (NFPA 101 1988 6 in letters, UL 924 (1989), stencil faces) and names further assets after it (`Kit_ExitSign_Hanging`, `_HangingFlush`, `_End`, `research/exit_sign/tools/make_diagrams.py:262-266`). G3's `Kit_ExitSign`/`_Dead` is a P2 placeholder that reuses the Run level's face (§7 item 6). **Recommendation: one owner.** The exit-sign workflow takes over `Kit_ExitSign`, `Kit_ExitSign_Dead` and `interact_exit_sign.py`, builds over main's files and keeps main's metas. G3 makes no more changes to them. Main's current copies stay valid until it does.

## 1. Blocking finding: `Kit_KeyBoard` overwrote `Kit_Keyboard`

The spec's name `Kit_KeyBoard` differs from the desk keyboard `Kit_Keyboard` only in case (`assets/keyboard.py:50`; used by `Assets/Scripts/Office/FrontRoomsOfficeKit.cs:47`).

- The first G3 run built `Kit_KeyBoard` into the clone. On the case-insensitive macOS volume that **overwrote the clone's `Kit_Keyboard.fbx/.json`** with the key board. The real project was not affected (its `Kit_Keyboard.*` are dated 2026-10-02 18:07).
- Unity's `Resources.Load` and the asset database are not case-safe either, so the name cannot ship.
- **Fixed:** the asset ships as **`Kit_KeyHookBoard`**. Its sidecar carries `meta.specName = "Kit_KeyBoard"`. The clone's `Kit_Keyboard.fbx/.json` were restored by copying them from the real project.
- **For the map chat and the facade:** §4.3's KeySpot `host` value is `Kit_KeyHookBoard`. §9.3 and §4.2 should be renamed in the spec.

## 2. Assets

Triangles are the exported LOD0 (`[kit] exported` line of the build). No asset sets `LOD1` (all are under 1.0 m or would be culled by today's importer, §1.8); every sidecar carries `lodDistances`, `lodBudget` and `lodRatios`, and small parts are marked `fr_lod2_drop`.

| Asset | Module | Tris (budget) | Size in the sidecar (Unity X × Y × Z, m) | Slots, in submesh order | Variants |
|---|---|---|---|---|---|
| `Kit_Key_Zone` | `interact_key.py` | 2,184 (2,400, −9 %) | 0.0022 × 0.026 × 0.058 | Prop_Brass | `_Nickel` (Prop_Aluminium) |
| `Kit_KeyRing` | `interact_key_ring.py` | 1,972 (2,000, −1 %) | 0.025 × 0.025 × 0.0018 | Prop_Chrome | — |
| `Kit_KeyTag_Rect` | `interact_key_tag_rect.py` | 1,038 (1,200, −14 %) | 0.057 × 0.039 × 0.0042 (body 0.029 + tab) | Prop_PlasticRed, Prop_KeyTagNo | `_Blue`, `_White` |
| `Kit_KeyTag_Round` | `interact_key_tag_round.py` | 1,282 (1,200, +7 %) | 0.038 × 0.048 × 0.0042 (Ø 0.038 + tab) | as Rect | `_Blue`, `_White` |
| `Kit_KeyTag_Long` | `interact_key_tag_long.py` | 1,038 (1,200, −14 %) | 0.076 × 0.032 × 0.0042 (body 0.022 + tab) | as Rect | `_Blue`, `_White` |
| `Kit_KeyHookBoard` (spec `Kit_KeyBoard`) | `interact_key_board.py` | 4,392 (4,000, +10 %) | 0.30 × Y 1.25–1.65 × 0.044 | Prop_WoodDark, Prop_Brass, Prop_KeyTagNo | — |
| `Kit_KeyCabinet` | `interact_key_cabinet.py` | 6,804 (7,000, −3 %) | 0.719 × Y 1.24–1.70 × 0.084 | Prop_SteelAlmond, Prop_PlasticWhite, Prop_Chrome, Prop_KeyTagNo | — |
| `Kit_KeyHook` | `interact_key_hook.py` | 530 (600, −12 %) | 0.0068 × Y 1.471–1.486 × 0.026 | Prop_Brass | — |
| `Kit_DoorSign` | `interact_door_sign.py` | 664 (600, +11 %) | 0.254 × 0.076 × 0.0043 (plate 0.003 + screw domes) | Prop_PlasticBlack, Prop_PlasticWhite, Prop_SignEngraved, Prop_Chrome | — |
| `Kit_DoorNumberPlate` | `interact_door_number_plate.py` | 480 (500, −4 %) | 0.100 × 0.050 × 0.0042 | Prop_PlasticRed, Prop_KeyTagNo, Prop_Chrome | `_Blue`, `_White` |
| `Kit_ExitSign` (P2) | `interact_exit_sign.py` | 1,916 (2,000, −4 %) | 0.330 × 0.2016 × 0.060 (housing + knockout) | Prop_SteelBlack, Run_ExitSign | `_Dead` (Run_ExitSign_Dead) |

All within ±15 % of §9.3. Every module asserts its envelope, anchors and budget at build time (`kc.check_budget`, `kc.eval_bounds_unity`), and every part carries the `fr_wear` point colour attribute.

**Slot names actually used.**
- Existing: Prop_Brass, Prop_Aluminium, Prop_Chrome, Prop_PlasticRed, Prop_PlasticBlue, Prop_PlasticWhite, Prop_PlasticBlack, Prop_WoodDark, Prop_SteelAlmond, Prop_SteelBlack.
- Registered with `kitlib.register_slot` (NEEDS APPROVAL, P-4): **`Prop_KeyTagNo`** (fallback Prop_Paper, blank), **`Prop_SignEngraved`** (fallback: the face reads as a dark plate), **`Run_ExitSign_Dead`**. `Run_ExitSign` is an existing Unity surface, registered only so the kit can name it.

## 3. Per-asset notes

### `Kit_Key_Zone` (+ `_Nickel`)
- Origin at the shoulder on the turning axis. Unity +Z = insertion toward the tip, +Y = cuts up, X = the flat normal (§3.3).
- Blade Z 0 → 0.025, 2.0 mm thick, the §3.2 section with 0.2 mm spine chamfers, extruded and cut with exact booleans: the bitting envelope (6 cuts, 0.8 mm flats, 50° flanks, depths [2, 4, 1, 5, 3, 2] × 0.38 mm + 0.2 mm) and the tip bevel from Z 0.0215 to Y 0.0005 at 0.025, with a 0.5 mm nose radius at the spine.
- Shoulder neck Z −0.003 → 0, Y −0.0045 → +0.0085; its front face above the blade is the stop on the core face.
- Paddle bow Z −0.003 → −0.033, Y −0.0125 → +0.0135, R 0.008 corners, 1 mm flares into the neck, 2.2 mm thick, 0.4 mm edge rounds in 3 segments; ring hole Ø 0.0048 at (0, 0.0005, −0.0265) with 0.3 mm chamfers, 48 segments.
- One rigid mesh. Hand grime (fr_wear R 0.8) on the bow.
- Anchors (sidecar, verified): `shoulder` (0, 0, 0), `tip` (0, 0, 0.025), `insert_dir` (0, 0, 0.10), `cuts_up` (0, 0.10, 0), `grip` (0, 0.0005, −0.018), `ring_hole` (0, 0.0005, −0.0265), `ring_hole_dir` (0.10, 0.0005, −0.0265).
- The §3.2 numbers in `interact_key_common.py` are identical to G2's `interact_lock_common.py` (section, keyway offset 0.15 mm, cut Z, steps, unit, base, flat, flank, tip bevel, nose).

### `Kit_KeyRing`
- A two-turn flat split ring: Ø 0.025 outside, 1.6 (radial) × 0.9 (axial) mm wire, coils stacked 1.8 mm, 128 segments per turn, a smoothstep crossover at the split (35°, upper right), both ends cut on a 30° slant so the split reads.
- Origin = `hook_contact`, the top inner point. Anchors: `hook_contact` (0, 0, 0), `key_contact` (−0.004, −0.0215, 0), `tag_contact` (+0.004, −0.0213, 0), plus `centre` (0, −0.0109, 0) and `normal_dir`.

### Tags: `Kit_KeyTag_Rect`, `_Round`, `_Long` (+ `_Blue`, `_White`)
- Plastic tag with a paper insert (era note R10). Body 4.2 mm with 0.6 mm rounded edges; a recessed window 0.8 mm deep with a 0.3 mm lip over a 0.6 mm undercut; one insert quad (decal UVs) sits 0.75 mm below the face and runs under the lip.
- Windows: Rect 0.040 × 0.019, Round Ø 0.026, Long 0.050 × 0.014.
- The ring tab is moulded 2.4 mm thick (thinner than the body) round the Ø 0.005 hole. This is what lets the tag turn ~30° on the 1.6 × 1.8 mm ring and hang face-on to the room.
- Origin = the ring hole; the tag hangs along −Y; front +Z. Anchors `hole`, `hole_dir`, `face` = `number` (insert centre), `face_dir`.
- The insert ships pointing at atlas cell "00", cropped to the window. The facade shows number n with a MaterialPropertyBlock `_BaseMap_ST = (1, 1, (n % 10) / 10, −(n // 10) / 10)` (recorded in the sidecar `numberAtlas`). Digit heights: Rect 15 mm, Round 11 mm, Long 12 mm (the Long crop stretches the digits 3.5 % horizontally).

### `Kit_KeyHookBoard` (Lobby default)
- Board 0.30 × 0.40 × 0.018, top at 1.65, back on the wall face, 3 mm round-overs; 8 brass cup hooks (Ø 3 mm wire, turned shoulder disc, J cup tip up) at X ±0.035 / ±0.105 in rows Y 1.54 / 1.42; a typed paper number strip 24 × 12 mm under each hook (cells 01–08); two slotted brass mounting screws. Hand grime round the hooks.
- Anchors `hook_0`…`hook_7` (top row then bottom row, each left to right from the room) at Z 0.0355; `key_hook` = `hook_6` (−0.035, 1.42, 0.0355). Proud 0.044.

### `Kit_KeyCabinet` (Office default)
- 1 mm sheet-steel body 0.36 × 0.46 × 0.08 at Y 1.24–1.70 with a rolled 9 mm front lip; a white hook panel; 4 × 6 chrome wire hooks on formed hook strips; a pan door 0.019 deep on a full-height piano hinge, swung open until its cam lock rests on the wall (189.7°); an IC figure-8 cam lock (bezel, two core lobes) facing the wall, with its barrel, hex nut and cam bar facing the room; a 3 × 5 in index card in a formed holder on the door; two mounting screws.
- **New in this run:** a typed number label (11 × 6 mm, cells 01–24) on the strip beside every hook, as on a numbered hook bar. To stay at 4 slots, the index card now uses `Prop_KeyTagNo` too, mapped to a blank paper band of the atlas (between rows 0 and 1, asserted digit-free); with the fallback it is Prop_Paper as before.
- **Third pass:** the lip, door pan and card holder corners are clean folded corners now (§0.2 item 1). Rendered: `G3_cabinet_34_v3.png`, `G3_cabinet_corner_close_v2.png`, `G3_cabinet_hinge_close_v2.png`, labels in `G3_cabinet_labels_close.png`.
- Origin centred on the open assembly: the body centre is at X −0.1794 and the door reaches X +0.359. Anchors `hook_r{0..3}_c{0..5}` (r0 top, c0 left from the room), `key_hook` = `hook_r1_c4` (−0.2574, 1.535, 0.0228), `door_hinge` (0.0026, 1.24, 0.081) + `door_hinge_dir`, `body_centre`. Proud 0.084.

### `Kit_KeyHook`
- One brass cup hook. `key_hook` (0, 1.476, 0.018) exactly (asserted). Proud 0.026. Tip at about 1.486 m.

### `Kit_DoorSign`
- Two-ply engraved plate 0.254 × 0.076 × 0.003: the dark cap ply (Prop_PlasticBlack sides and back), a 1 mm × 45° face bevel that cuts into the ivory core (Prop_PlasticWhite: the fine ivory outline of a real two-ply plate), the face as one quad on `Prop_SignEngraved` at 1:1 (4031 px/m), and four slotted oval-head screws 10 mm in from the corners.
- Origin = centre of the back face on the leaf face; front +Z. Anchors `back`, `face`, `face_dir`.
- Cell n is chosen with `_BaseMap_ST = (1, 1, (n % 2) / 2, −(n // 2) / 2)`: 0 EMPLOYEES ONLY, 1 STAFF ONLY, 2 STORAGE, 3 spare.

### `Kit_DoorNumberPlate` (+ `_Blue`, `_White`)
- 0.100 × 0.050 × 0.003, R 0.004 corners, 0.5 mm edge round; a 0.070 × 0.034 window 0.6 mm deep holding a typed `Prop_KeyTagNo` insert (27 mm digits); two slotted oval-head screws. Same construction and colour family as the key tags, so door and tag match by colour (2–5 m) and by number (close).
- Anchors `back`, `face` = `number`, `face_dir`. Same `_BaseMap_ST` rule as the tags.

### `Kit_ExitSign` (+ `_Dead`) (P2)
- A black stamped-steel housing 0.330 × 0.200 with a 0.050 body, a stamped face frame with a raised bead to 0.060, four slotted face screws, a conduit knockout on top. No pilot light, no test button, no brand, **no Light component**.
- The face quad uses the existing `Run_ExitSign` surface through that material's own UV transform (`_TileSize` 0.36 × 0.18, `_BaseMap_ST` (−1, 1, 1, 0)), showing the texture region with the word and without the Run chevrons. `_Dead` swaps in `Run_ExitSign_Dead`.
- Origin = centre of the back on the wall (at `exit_sign_p`, Y 2.280, so the top is at 2.380).

## 4. Self-checks (all on the current FBX files)

- **(a) Blade section = §3.2: PASS.** `check_section.py` slices the exported key at Z 2.0, 6.0, 11.6, 15.4, 20.0 mm; all 14 corners of the §3.2 section (with the bitting clamp) are on the cut, X ±0.0010, Y −0.0040 to the expected bitting height (`g3/section_check.json`). The module also asserts the polygon, the keyway offsets and the cut depths at build time.
- **(a) T8 key fit against G2's `Kit_Lock_Plug` + `Kit_Lock_CylinderShell` (rebuilt by G2 at 16:45): PASS.** Key and plug turned together 0°, ±45°, ±90° about the plug axis: zero BVH overlaps key–plug, key–shell, plug–shell (`g3/t8_report.json`). The 0.02 mm minimum is the shoulder's seat offset on the core face.
- **(a) T8b, blade only (17:46): PASS.** With the shoulder left out, the closest blade vertex is 0.15 mm from the plug at 0°, 45° and 90°: exactly §3.2's keyway clearance. Pin tips hang 0.19 mm above the blade at cuts 1–5 (§3.2 asks 0.2 mm). Pin 6 is 1.01 mm above the blade, because cut 6 lies under the tip bevel (§7 item 3). `g3/t8b_report.json`.
- **(a) T8 pictures (third pass):** `G3_T8_section.png` (half plug cut away: the five cuts under their pins) and `G3_T8_key_in_plug_0deg.png` / `_90deg.png` (key seated, shoulder on the core face, before and after the turn).
- **(b) Hung assembly on `Kit_KeyHookBoard`:** renders at 0.35 m (FOV 76° and 62°), 2 m, 6 m and a threading close-up. The typed "14" reads at 0.35 m with the proposal atlas. Zero overlaps ring–board, key–board, tag–board, ring–key, ring–tag, key–tag; ring clears the cup by 0.12 mm.
- **(b) Hung pose on every host with every tag (17:47): PASS, 9 of 9.** Each host's `hungPose` recipe applied to {board, single hook, cabinet} × {Rect, Round, Long}: zero overlaps between any two parts, ring clear of the hook by 0.08–0.12 mm, the whole hung assembly at most 0.047 m proud, lowest point 1.348 m (board), 1.404 m (hook), 1.463 m (cabinet). `g3/verify_hung.json`.
- **(c) Flat pose on a 0.74 m desk: PASS** (final pose, 17:48): key flats-down tilted 6° where the ring passes its hole, ring leaning 30°, tag face up with its face 10.2 mm above the desk; lowest points key 0.74000, ring 0.74017, tag 0.74027 m (resting, not floating, not sunk); zero overlaps. `g3/flat_report.json`; render `G3_flat_desk_close_v2.png` and `G3_flat_desk_standing.png` (eye 1.62 m, FOV 76°).
- **(d) Hosts:** board `key_hook` 1.42 m, 0.0355 proud; cabinet 1.535 m, 0.0228 proud; hook 1.476 m, 0.018 proud. Host proud maxima 0.044 / 0.084 / 0.026 (≤ 0.10). All hook heights 1.40–1.55 except the cabinet's top row (1.640: real cabinet layout; the map should use rows 1–2).

## 5. Hung and flat pose recipes (for the facade)

**Hung** (§4.1; stored as `hungPose` in each host's sidecar):
- the ring's `hook_contact` goes `ringLiftM` above the host's `key_hook` (board 1.2 mm, hook 1.4 mm, cabinet 1.1 mm), the ring turned `Euler(0, −60, 0)` from the host (its plane 60° toward the room);
- the key hangs tip down from `key_contact` by its `ring_hole`: `LookRotation(down, Cross(down, hostForward))`, flats square to the room (30° of twist in its Ø 4.8 hole);
- the tag hangs from `tag_contact` by its `hole`: `LookRotation(hostForward, up)`, face to the room (30° of twist on the 2.4 mm tab);
- hung keys do not spin and have no emission.
- The tag hangs in front of the key on this ring yaw, so the number is never covered; the blade shows below the tag and the bow's top above it. Mirroring the yaw (+60°) would put the key in front of the tag's window.

**Flat** (`r_flat.py`, solved by search with BVH overlap tests): the key lies flats-down, tilted a few degrees where the ring passes through its hole; the ring leans through the hole; the tag lies beside it, face up. Values in `g3/flat_report.json`.

## 6. Proposal textures (P-4; scratchpad only, never `Assets/`)

- `scratchpad/g3/textures/Prop_KeyTagNo_A.png`: 2048², 10 × 10 cells "00"–"99" (row-major from the top-left), Courier Prime Regular from `Assets/Fonts/Period1990/CourierPrime/`, typewriter strike jitter and ribbon grain, on an index-card paper ground. Digit ink height 0.24 of a cell.
- `scratchpad/g3/textures/Prop_SignEngraved_A.png`: 2048 × 1024, 2 × 2 cells of 1024 × 512 (0.254 × 0.127 m at 1:1): EMPLOYEES ONLY, STAFF ONLY, STORAGE, spare. TeX Gyre Heros Bold, ivory core on dark brown sRGB (48, 36, 30), a thin engraved rim. No dates, no logos.
- Generator: `scratchpad/g3/gen_textures_g3.py`.

## 7. Deviations and open items

1. **`Kit_KeyBoard` → `Kit_KeyHookBoard`** (§1). Spec, map KeySpot `host` values and facade must follow.
2. **Ring section is a 4-sided flat bar, not 8-sided.** At 128 segments per turn, 8 sides would be 4,096 triangles, twice the budget, and the extra corners are 0.1 mm rounds, sub-pixel at 0.3 m.
3. **Cut 6 is inside the tip bevel** (spec conflict, also reported by G2). At Z 0.023 the bevel is already at Y 0.00282, 0.82 mm below cut 6's floor (0.00364), so the blade shows 5 cuts and pin 6 hangs about 1.0 mm above the blade (T8b: 1.01 mm). The numbers stay identical to G2's. Fix in the spec if wanted: start the bevel at Z ≥ 0.0234, or move the cuts 1.5 mm toward the shoulder (both groups must change together).
4. **Sign lettering is 17 mm caps, tracking +20, not 19 mm / +40.** Measured with the font file (`texgyreheros-bold.otf`): "EMPLOYEES ONLY" at 19 mm caps and +40 tracking is 0.255 m wide, wider than the 0.252 face inside the bevel; at 19 mm with no tracking it is 0.242 m, leaving 5 mm margins. At 17 mm / +20 it is 0.2225 m with 15 mm margins. The same cap height is used on all three texts (STAFF ONLY 0.151 m, STORAGE 0.118 m).
5. **Number plate is not "two-ply engraved".** Engraved digits would need a second number atlas; the plate uses the tag construction with the typed `Prop_KeyTagNo` insert, as §10.3 asks ("digits quad on Prop_KeyTagNo").
6. **Exit sign letters are 0.10 m, not 6 in,** because the face reuses the Run level's `Run_ExitSign` texture. A 6 in legend needs its own face texture (Red's colour call, §2.4, is still open).
7. **Cabinet door rests at 189.7°, not "about 175°":** a door hinged at the front of a 0.08 m box must pass 180° to lie back toward the wall. Nothing stands more than 0.084 off the wall.
8. **Round tag body uses 32 segments** (§1.8 asks ≥ 48 for parts ≤ 0.07 m, written for door hardware). The silhouette sagitta is 0.09 mm, sub-pixel at 0.3 m; 48 would put the tag ~25 % over budget.
9. **Board number strips sit under the hooks (per §4.2)**, so a hung ring covers its own hook's number. Real boards often print the number above the hook; it does not matter for play (the tag carries the identity).
10. The **over-edge pose** (§4.1) is not rendered; it is a facade pose from the same anchors.
11. ESTIMATES that became numbers: the cup-hook geometry (leg 17 mm, bend R 5.5 mm, rise 4.5 mm), the tab thickness 2.4 mm, the cabinet hook spacing (52 × 105 mm) and the exit-sign housing profile.
12. **Key rack name still open (WAIT-RED).** Main ships `Kit_KeyHookBoard`; the proposal suggests `Kit_KeyRack` (`VISUAL_CHAT_TASKS.md` D1.3). If Red picks `Kit_KeyRack`, rename the FBX, JSON and both metas together in main so the GUIDs stay, and rename `NAME` in `interact_key_board.py`. Never `Kit_KeyBoard`.
13. **G3 part assets show up in the Level Designer palette as Floor props.** kitlib sets `placement` from tags only (`kitlib.py:820-821`), so the key, ring, tags, sign, number plate and exit sign get "Floor", and the palette lists every kit by placement (`FrontRoomsModuleEditing.cs`, `PaletteKits`). Dropped from the palette they would sit at their part origin on the floor. The G1/G2 part assets have the same problem. A fix needs a kitlib or palette change (for example a `part` tag that the palette skips); this workflow may not make it. The three wall hosts are correctly "Wall".
14. **FBX tangent warning** ("polygons with more than 4 vertices, cannot compute/export tangent space") on most G3 meshes (cap n-gons). Harmless: the importer recomputes MikkTSpace tangents (`FrontRoomsKitImporter.cs`, `importTangents = CalculateMikk`).
15. **`Kit_ExitSign` ownership** moves to the exit-sign workflow (§0.4).

## 8. Verification images (Figma log, `VERIFICATION_LOG.md` rows VL069–VL074)

Images saved as JPG q85 in `research/interactables/images/g3/`. Slides placed in **FRONTROOMS · VISUAL VERIFICATION LOG** (section `2595:6093`, page `2099:76`), which this pass grew to 19 rows (bounds below checked clear first).

| VL | Slide | Check | Verdict | Images (slot order) |
|---|---|---|---|---|
| VL069 | `2605:6099` | Key seated in the lock (T8) | PASS | `G3_T8_key_in_plug_0deg`, `G3_T8_section`, `G3_key_side`, `G3_T8_key_in_plug_90deg` |
| VL070 | `2605:6119` | Hung key on the key board | PASS | `G3_hung_0.35m_fov62`, `G3_hung_threading`, `G3_hung_2m`, `G3_hung_6m` |
| VL071 | `2605:6139` | Key lying flat on a desk | PASS | `G3_flat_desk_close_v2`, `G3_flat_desk_standing` |
| VL072 | `2605:6153` | Key cabinet corner notches | FAIL→FIXED | `G3_cabinet_corner_before_after_pair`, `G3_cabinet_34_v2` |
| VL073 | `2605:6169` | Wall hosts for the key | PASS | `G3_board_34`, `G3_cabinet_34_v3`, `G3_keyhook_close`, `G3_cabinet_labels_close` |
| VL074 | `2605:6189` | Door sign, plate, exit sign | FLAG | `G3_signage_door_1.5m`, `G3_sign_corner_close`, `G3_numberplate_close`, `G3_exit_sign_close`, `G3_exit_signs_2.6m` |

Not logged: the kit's own turntable previews (`Kit_*_a/_b.png`, raw previews), `G3_T8_face.png` (blank, §0.2 item 2), `G3_flat_desk_close.png` (old pose), and the scratch views in `scratchpad/g3/view/`.

On the flat pose (VL071): the ring stands steep (30° off the hole axis) because a 0.9 mm wire through a Ø 4.8 hole in a 2.2 mm bow can lean at most about 60° off the hole axis, and the desk stops the loop from passing under the bow. A flatter ring would need the bow lifted about 25°. The facade can use either. The search scores the smallest key tilt first and then the lowest ring, so it picked the key nearly flat (6°); it does not simulate gravity.
