# Interactables three-view hand-off (for 平面视觉)

From the visual chat (游戏视觉), interactables-kit workflow, three-view stage, **2026-10-08**; updated **2026-10-14** (§9). It covers the doors, locks, keys and windows after the fix pass (`../30_final.md`). **These models are in main since 2026-10-14**, so a clone of main renders exactly what is here.

平面视觉 places the sheets itself as **K46–Kxx · INTERACTABLES (doors · locks · keys · windows)** inside PROP KIT `2324:852`, re-rendering through its own `Tools/three_view` pipeline. We never edit PROP KIT `2324:852`. Our only Figma work is our own proposal section DOORS + WINDOWS `2748:6099`, which we filled with these renders on 2026-10-14 (§10, §11).

## 1. What is here

| Path | What |
|---|---|
| `index.json` → **`assets`** | **57 entries, one per new asset.** Each entry has exactly these fields: `name`, `fbx`, `dims_m`, `tris_lod0`, `tris_lod1`, `materials`, `variants`, `level`, `era_note`, `suggested_sheet_scale`. |
| `index.json` → `assetsMeta` | Units, the variant kinds, image naming, sources, what is not included, and **`changedSincePlaced`** (per asset, what differs from the K46–K80 sheets you placed on 2026-10-04). |
| `<name>_{front,side,top}.png` | Our fallback renders: third-angle orthographic views at `suggested_sheet_scale` (1 PNG px = 1 sheet px, so place them at 100 %), on a transparent background with 16 px clear padding. 171 files. |
| `<name>_hero.png` | A 3/4 hero, 1152 × 864, transparent. It looks from the front-left, 22° down. 57 files. |
| `handoff_manifest.json` | The renderer's own record: px sizes, bounds, `viewYaw`, `sheetPpm`. Its `persp` = our `_hero`. |
| `FrontRoomsThreeView.handoff.cs.txt` | The renderer used. It is a superset of `Tools/three_view/FrontRoomsThreeView.cs`; see §3. |
| `tools/build_assets.py` | Writes `assets` and `assetsMeta` from the sidecars and the manifest. `tools/build_index.py` (the 10-04 K-sheet record) now keeps both blocks when it rebuilds. |
| `index.json` → `sheets`, `kits`, and `png/` | The 2026-10-04 record behind the placed K46–K80. Kept unchanged. Where it differs from `assets`, **`assets` wins.** |
| `heroes/` and `index.json` → `heroes`, `proposalSlots`, **`heroesMeta`** | **Re-rendered 2026-10-08 20:00 on the fix-pass models** (§8): the four grouped heroes (door lineup, lock exploded, key + ring + tag + hosts, window lineup), the 8 door sets and 4 window members, and the 26 proposal-slot crops in `heroes/slots/`. The window lineup and W-RN were re-rendered on 2026-10-14 (§9). The gap close later that day added 4 renders (the over-edge desk key, its wide view, two 0.3 m window close-ups) and 3 crops, so `heroes/` now holds 37 renders, each with its `_alpha` copy, and `heroes/slots/` 29 crops plus the 4 `DW14_*` (§11). The 10-04 versions are in `heroes/_pre_fix_2026-10-04/`. `heroes/hero_notes.json` holds the current hero notes. |
| `png_fix/<Kit>_<top|front|side|persp>.png` | The fix pass's three-views of the **18 kits that changed** since K46–K80 (same tool and scales as `png/`, 72 PNG + `manifest.json`). The kit is in main since 2026-10-14, so refresh the sheets now: **13 sheets**, not 18, because 4 kits render pixel-identical and K77 holds two kits (§10). K53 also needs its view slots resized (`HANDOFF_平面视觉.md` §3). |

`index.json` has its own `fields` list in `assetsMeta`, so a script can check that each entry carries exactly those fields.

## 2. The models: where they are, and how to load them

- **The FBX path** is `Assets/Resources/Props/Models/<name>.fbx`, next to its sidecar `<name>.json`. The sidecar holds bounds, slots, anchors, `motion` and LOD data.
- **The current builds are in main** since 2026-10-14: `W/apply/interactables_kit.sh --apply` wrote the 102 fix-pass files (50 FBX, 52 JSON) over main's `Kit_*` files with `MANIFEST OK` and kept main's `.meta` GUIDs.
  - Checked 2026-10-14: for all 57 assets, main's FBX and JSON match the files these PNGs were rendered from. 105 of the 114 files are byte-identical. The other 9 differ in ways no image shows:
    - 7 FBX differ only by their export timestamp; main kept its own copies at the merge;
    - `Kit_DoorFrame_Steel.json` and `_Alu.json` also carry the exit-sign track's anchor move (`exit_sign_p` Y 2.28 → 2.18, 2026-10-08 19:40).
  - So render from any fresh clone of main; no payload copy is needed any more.
- **Materials.** The FBX's embedded materials are only preview colours. `FrontRoomsKitLibrary.Spawn` remaps every slot to `Assets/Resources/Surfaces/<slot>.mat`, so always spawn through the kit library. The three-view tool does this.
  - **Three slots have no surface yet** (P-4). They render with the FBX preview colour:
    - `Door_Enamel`, used by `Kit_DoorLeaf_Steel`, `Kit_DoorLeaf_SteelLite` and `Kit_WindowFrame_Steel_Enamel`. Target almond #cdc5b0.
    - `Prop_KeyTagNo`, used by every tag, number plate and board label: the inserts are blank. Target #dcd8cc, typed numbers in Courier Prime.
    - `Prop_SignEngraved`, used by `Kit_DoorSign`. It renders a light grey-brown; the target is #30241e with ivory letters.
  - The swatch rule and the target hexes are unchanged from the 10-04 hand-off; they are in `kits[*].materials` in `index.json`.
- **Not separate assets:**
  - **Hinges.** The 5-knuckle butts are merged into the meshes. The frame halves (knuckles 1, 3, 5) are in `Kit_DoorFrame_*`; the leaf halves (knuckles 2, 4) are in `Kit_DoorLeaf_*`.
  - **Glass fracture, remnants and shards.** These belong to the glass tracks (Red's decision; `10_spec.md` D12, §5.3). The window frames carry only the interface anchors.
  - **`Kit_ExitSign*`** now belongs to the exit-sign track.
  - **`Kit_DoorwayStuds*`** are the existing K45, unchanged.

## 3. Views, scale and re-rendering

- **Views.** All views are third-angle:
  - front: the camera on the kit's +Z;
  - side: the kit's right;
  - top: from above, with the front toward the bottom of the image.
- **Turned kits.** Door frames, door leaves and keys show their face on +X, so they are turned −90° (`viewYaw`):
  - a door's front is its pull face, with the hinge at the image's left;
  - a key's front has the tip at the right and the cuts up.
  - For these kits, `dims_m` is in that turned frame: w = opening width (or key length), d = thickness.
- **The ground line is not the floor.** The PNG's bottom edge, minus the padding, is the kit's lowest point.
  - Wall-hung kits (windows, blinds, key hosts) have bounds that start at their mounting height.
  - Small parts are drawn about their own pivot. Do not draw a floor line under them.
- **Scale.** `suggested_sheet_scale` comes from `Tools/three_view`'s own ladder (2000/1000/500/250/125 px/m): anything ≥ 0.75 m goes at 250; otherwise it is the largest step whose top + front + side layout fits 1080 × 688. The result is 16 assets at 250, 2 at 1000 (`Kit_KeyHookBoard`, `Kit_KeyCabinet`) and 39 at 2000.
  - The 10-04 sheets used extra 4000 and 8000 steps for 33 small parts. If you keep those scales, nothing else changes.
- **Re-rendering in your pipeline.** Main's `Tools/three_view/FrontRoomsThreeView.cs` has no yaw argument, so door frames, leaves and keys would come out edge-on. Use `FrontRoomsThreeView.handoff.cs.txt` instead: copy it to `<clone>/Assets/Editor/Rendering/FrontRoomsThreeView.cs`. Its defaults behave like main's tool, plus:
  - `-threeViewYaw Kit_A=-90,…`
  - `-threeViewMaxPpm 2000` caps the ladder at main's ladder.
  - `-threeViewOversample N`: the PNG px per sheet px. The default is 2; our files use 1.

  ```zsh
  Unity -batchmode -projectPath <clone> -buildTarget OSXUniversal \
    -executeMethod FrontRoomsThreeView.RunBatch -threeViewOut <dir> \
    -threeViewOnly <comma list of names from index.json assets> \
    -threeViewYaw Kit_DoorFrame_Steel=-90,Kit_DoorFrame_Steel_Alu=-90,Kit_DoorFrame_Wood=-90,Kit_DoorLeaf_Steel=-90,Kit_DoorLeaf_SteelLite=-90,Kit_DoorLeaf_Steel_PaintedMetal=-90,Kit_DoorLeaf_Veneer=-90,Kit_DoorLeaf_Veneer_Oak=-90,Kit_DoorLeaf_Ward=-90,Kit_Key_Zone=-90,Kit_Key_Zone_Nickel=-90 \
    -threeViewMaxPpm 2000 -quit -logFile <log>
  ```

  It needs graphics, so do not pass `-nographics`. Our run took 4 minutes for all 57 assets (log `W/logs/int_threeview_handoff.log`). It reported no failures and no "renders outside its sidecar bounds" warnings. We checked every PNG: transparent corners, ≥ 15 px clear margin, nothing empty.

## 4. What the variants mean

Each entry's `variants` is a list of `{name, kind, differs}`. `kind` is one of:

| kind | Meaning |
|---|---|
| `theme` | A level's version of the same part. Frames: wood (Lobby), dark-bronze steel (Lobby locked, Office, Run), aluminium (Exit). Lock finish: brass on the Lobby's free door, satin chrome everywhere else. Windows: wood L0, steel OF/RN, aluminium EX. |
| `free/locked` | The counterpart on the other door type in the same level. **FREE door** = warm wood leaf (veneer, oak or laminate) with a lever and no keyhole. **LOCKED door** = almond-enamel steel leaf in a dark-bronze frame, with a tall escutcheon, a knurled knob, an IC cylinder, kick plates, a sign and a number plate. A locked door keeps this look after it is unlocked. |
| `finish` | A metal swap only. The brass mortise parts (`Kit_Lock_Escutcheon_Brass`, `_CylinderShell_Brass`, `_Knob_Brass`) are spares: locked hardware stays chrome in every level. `Kit_Key_Zone_Nickel` is kept for a later master key. |
| `colour`, `shape` | Zone identity: tag colour × shape (3 × 3) and the matching number-plate colour. |
| `fallback` | `Kit_DoorLeaf_Steel_PaintedMetal` stands in until `Door_Enamel` exists. |
| `option` | Red's optional pieces: `Kit_DoorLeaf_SteelLite` (Run locked, with a vision lite) and `Kit_MiniBlind_Raised` (Office windows). |
| `state` | A runtime pose of the same mesh, not a separate asset. Examples: a door 0–95° open, the plug 0/90°, the deadbolt thrown or retracted, the key inserted or turned, the strike torn off, the closer arm angle. |
| `intact/broken` | The window frame is the same mesh in both states. The glass, its crack stages, the remnant teeth and the floor glass are the glass tracks'; they hang from the frame's anchors. |
| `text`, `assembly` | Atlas text that does not exist yet, and the parts that go together. |

`level` uses Lobby (Level 0), Office, Run and Exit. `shared` means every level, or a part every door of that type uses.

## 5. Assemblies and relative transforms (from the sidecar anchors)

Unity metres. Every part's front is its +Z. Rotations are Euler degrees.

### 5.1 Door: frame + leaf + hinges

**Door root D** (all door kits are authored in it):
- origin: the hinge-jamb edge of the opening, on the wall centre line, at floor level;
- +Z runs along the opening, from the hinge (Z 0) to the latch (Z 1.0);
- +Y is up;
- +X is the pull (swing) face, `_s`; −X is the push (stop) face, `_p`.
- Handing: the frame and the leaf rig take `localScale (S_sign, 1, 1)`.

| Part | Placed at | Rotation / scale |
|---|---|---|
| `Kit_DoorFrame_*` | D origin | identity / (S_sign, 1, 1) |
| `Kit_DoorLeaf_*` | D origin when closed | identity. It turns about the frame's `hinge_axis` (0.0295, y, 0.0098), up to 95° toward +X. |
| Hinges (merged) | Butts at Y 0.254–0.368, 1.0565–1.1705 and 1.859–1.973 on that axis | — |
| Strike, locked doors (`Kit_Lock_StrikeMortise`) | frame `strike` (0, 0.968, 0.998) | (0, 180, 0) |
| Strike, free doors (`Kit_Lock_StrikeBored*`) | frame `strike_bored` (0, 1.000, 0.998) on steel frames; the wood frame's `strike` (0, 1.000, 0.998) | (0, 180, 0) |
| Closer shoe | frame `closer_shoe` (0.125, 2.13, 0.10) | (0, 90, 0) |
| Exit sign (exit-sign track) | frame `exit_sign_p` (−0.08, 2.18, 0.5) (the exit-sign apply lowered it from 2.28 on 2026-10-08) | — |

### 5.2 Lock on the leaf

**Locked doors** (`Kit_DoorLeaf_Steel*`):

| Part | Placed at | Rotation / scale |
|---|---|---|
| Escutcheon | leaf `escutcheon_s` (0.022, 0.968, 0.92) | (0, 90, 0) |
| Escutcheon | leaf `escutcheon_p` (−0.022, 0.968, 0.92) | (0, −90, 0) / (−1, 1, 1) |
| Knob | escutcheon `knob_pivot` (0, −0.0315, 0.002) | identity in the escutcheon frame; turns ±40° about +Z |
| Cylinder shell and plug | escutcheon `keyhole` (0, 0.032, 0.0095), which is the shell's own `keyhole` (0, 0, 0) = the core face. That is door (±0.0315, 1.000, 0.920). | identity. The plug turns 0 → 90° about −Z with the key. Never mirror the plug: scale (S_sign, 1, 1) on both faces. |
| Deadbolt | leaf `deadbolt` (0, 1.000, 0.9939) | identity (throw +Z). Authored thrown; it retracts 0.025 along −Z. |
| Mortise latch | leaf `latchbolt` (0, 0.9365, 0.9939) | identity. It retracts 0.019. |
| Number plate | leaf `tagplate_s/_p` (±0.022, 1.000, 0.79) | face rotation / (S_sign, 1, 1) |
| Sign | leaf `sign_s/_p` (±0.022, 1.524, 0.5) | face rotation / (S_sign, 1, 1) |

**Free doors** (`Kit_DoorLeaf_Veneer`, `_Oak`):

| Part | Placed at | Rotation / scale |
|---|---|---|
| Rose | leaf `rose_s/_p` (±0.022, 1.000, 0.92) | face rotations as the escutcheon |
| Lever | the rose's `spindle` (0, 0, 0.010), i.e. door X ±0.032 | grip toward the hinge; 35° down |
| Bored latch | leaf `latchbolt` (0, 1.000, 0.9939) | retracts 0.013 |

**Ward door:** the pull, push plate, armor and kick plates are part of the leaf mesh. It has no lock.

**Exit free door:** `Kit_ExitDevice_Crossbar` at door (−0.022, 1.0, 0.9), rotation (0, −90, 0), scale (−1, 1, 1), on the push face.

### 5.3 Closer (4 parts)

| Part | Placed at | Notes |
|---|---|---|
| Body | leaf `closer_mount_s` (0.022, 2.0275, 0.545), rotation (0, 90, 0) | Its `spindle` lands at door (0.062, 2.068, 0.52). |
| Arm | the body's `spindle` | Elbow at arm (0.24, 0, 0). |
| Forearm | the arm's `elbow` | It reaches the shoe at forearm `shoe_end` (0.26, 0.05, 0). |
| Shoe | frame `closer_shoe` | Shoe `pivot` = door (0.125, 2.13, 0.10). |

The arm and forearm are aimed by a solver. The elbow's door XZ is (0.222, 0.341) at 0°, (0.385, 0.109) at 45° and (0.297, −0.095) at 95° (arm sidecar `motion.elbowDoorAt`).

### 5.4 Key, ring, tag and host

- **Key frame:** the origin is `shoulder` on the turning axis; it inserts along +Z; the cuts face +Y; the flats face X. Anchors: `tip` (0, 0, 0.025), `grip` (0, 0.0005, −0.018), `ring_hole` (0, 0.0005, −0.0265).
- **In the lock:** the key's `shoulder` meets the escutcheon `keyhole`, with the key turned (0, 180, 0) in the escutcheon frame, so its +Z points into the door and the cuts stay up.
  - At insertion fraction t, the shoulder sits (1 − t) · 0.025 out along the escutcheon's +Z.
  - Then the key turns 90° with the plug.
- **Ring:** `hook_contact` (0, 0, 0) goes on the host's `key_hook`. Ring `key_contact` (−0.004, −0.0215, 0) meets the key's `ring_hole`; ring `tag_contact` (0.004, −0.0213, 0) meets the tag's `hole` (0, 0, 0).
- **Hung pose v2** (each host's sidecar `hungPose`): ring yaw 120° about the hook, lift 0.0011–0.0014 m, key twist −30°, tag twist 150°. The tag hangs on the wall side and the key faces the room. Hosted keys do not spin.
- **Hosts:**
  - `Kit_KeyHook`: `key_hook` (0, 1.476, 0.018).
  - `Kit_KeyHookBoard`: 8 hooks at Y 1.54 / 1.42, X ±0.035 and ±0.105, Z 0.0355; `key_hook` = hook 6.
  - `Kit_KeyCabinet`: 24 hooks `hook_r{0-3}_c{0-5}`, `key_hook` = `hook_r1_c4`. Its door is modelled open 189.7°, resting against the wall.
  - The host origins are at the floor under the mount.

### 5.5 Window + blind + glass interface

**Window root W:** the opening centre, on the wall centre line, at floor level; +Z = face A.
- The frame sits at W with identity rotation, or (0, 180, 0) so that face A looks into the non-tall cell.
- The blind's origin is its `blind_rail`, placed at the frame's `blind_rail` (0, 2.195, 0.1275) on face A. Only `Kit_MiniBlind_Raised` goes on map windows.
- **Glass interface,** the same on all four frames:

| Anchor | Position |
|---|---|
| `glass_slab` | (0, 1.175, 0); the pane is 1.391 × 1.642 × 0.006 |
| `stop_l/r/t/b` | the sight line, X ±0.6835, Y 0.3665 / 1.9835 |
| `pocket_*` | X ±0.6955, Y 0.354 / 1.996 |
| `tooth_band_*` | X ±0.600, Y 0.39 / 1.90 |
| `sill_plant_a/b` | (0, 0.3505, ±0.05) |
| `floor_a/b` | (0, 0, ±0.45), the floor glass |

Nothing from the kit sits inside the 1.4 × (0.35–2.0) m climb opening.

## 6. Before you lay out

**17 assets changed visibly since your 10-04 sheets** (`assetsMeta.changedSincePlaced`, lines starting `VISIBLE`); re-render their images:
- Wood grain: `Kit_DoorLeaf_Veneer_Oak` and `Kit_DoorLeaf_Ward` are now vertical; `Kit_KeyHookBoard`'s board grain and `Kit_DoorFrame_Wood`'s saddle grain are turned.
- `Kit_DoorFrame_Steel`: the saddle is now dark bronze.
- `Kit_ExitDevice_Crossbar`: a larger latch case; it is now 0.909 × 0.18 m.
- Rounder `Kit_DoorCloser_Shoe` and `Kit_KeyHook`.
- `Kit_Lock_Knob` and `_Brass`: crowned face.
- `Kit_Lock_Escutcheon` and `_Brass`: 1.5 mm edge.
- `Kit_Key_Zone` and `_Nickel`: all 6 cuts show.
- The steel, enamel and aluminium window frames.

Most of the other entries change only in data: small parts now have a LOD1 count. The LOD1 count equals LOD0 where the LODGroup only culls.

**Known looks that are not render faults:**
- `Kit_DoorLeaf_Veneer` is striped. That is the game's `DoorVeneer_A` texture, and a redraw is open.
- Chrome reads as a bright mirror in the studio light but near-black in the game. A satin-chrome tune is open (critic H1).
- Tag, plate and board inserts and the sign face are blank (§2).

**Open naming:** `Kit_KeyHookBoard` may be renamed `Kit_KeyRack`. It is Red's pick (codex audit F6); only the kit name and the `img:` layer names would change.

**Spare members:** `Kit_WindowFrame_Steel_Enamel` has had no map member since the fix pass gave the Run window the bronze steel frame; the Run doors use that frame too (`../30_final.md` §5.5, critic M2; `../10_spec.md` line 645 still lists enamel, and main's window facade dresses only Lobby and Office windows today). The brass mortise parts are spares (§4).

**The key in the HUD (Red's decision, 2026-10-14).** The HUD key on phone and desktop is direction **B**: the key on its ring and tag. At a locked door the HUD shows a **padlock in the tag's language**, with the zone tag's shape and colour. That is your component set `3113:6999` (boards KV11 `3113:7000` and KV12 `3113:7147`; spec `../../ui_key_icon/lock_motion/README.md`). A, A + chip and C are history only. On the kit side, the B glyph and the padlock body follow `Kit_KeyTag_{Rect,Round,Long}` in red (no suffix), `_Blue` and `_White`, and the locked door's `Kit_DoorNumberPlate*` carries the same zone colour and number. `assetsMeta.keyUi` in `index.json` says the same.

## 7. Earlier record (10-03 / 10-04 hand-off, unchanged)

- `png/<Kit>_<top|front|side|persp>.png` (248 files, 2× sheet scale, ladder up to 8000) and the 10-04 heroes (now in `heroes/_pre_fix_2026-10-04/`: grouped heroes, door sets, `slots/` for our proposal section `2748:6099`) are what K46–K80 were built from; §8 replaced `heroes/` itself. `INDEX_FORMAT.md` is the K-sheet field format agreed on 10-03. The `sheets[]` and `kits{}` records in `index.json` follow it.
- Placement: 平面视觉 placed K46–K80 on 2026-10-04 in PROP KIT `2324:852`, file `0tCbAiVUlrPId3RWd9LRif`, page `2099:76`:
  - K46 is `2648:6094`; K47 is `2648:6139`;
  - K48–K80 sit in the four-column grid from `kplan.json`;
  - the CONT section `2648:6093` no longer exists (merged on 2026-10-07).
- Image layers are named `img:<Kit>_<top|front|side|persp>`, so re-renders go in with `upload_assets` and `nodeIds`.
- Era rules (binding): unbranded interchangeable-core cylinders; push bars on exits (our crossbar is `Second-hand`); plastic tags with typed Courier Prime numbers; 1-inch aluminium mini-blinds. Never keypads, card readers or LED lock lights. Nothing newer than 1993, and no printed date after 1990 (`../../office_and_film/22_era_lock.md`).
- iCloud conflict copies (`* 2.png`) in `heroes/` and `png/` are not ours to delete.

## 8. Hero re-render, 2026-10-08 20:00 (kit-documentation workflow, render stage)

- **Models:** the fix-pass payload (`W/apply/interactables_kit_payload`, 102 files) applied with `interactables_kit.sh --apply` into the clone `W/proj_3view_int` only (`MANIFEST OK`), over main `bc0aa79` plus its working tree at 19:53. So the heroes show what main will hold after the visual chat runs that apply, including the exit-sign kit merged at 19:40. The apply ran on 2026-10-14; see §9.
- **Renderer:** `FrontRoomsInteractableHeroes.RunBatch -heroOut <dir>` (copy: `FrontRoomsInteractableHeroes.cs.txt`). The only change from 10-04 is the hung pose: v2 from the hosts' sidecar `hungPose` (ring yaw 120, key twist −30, tag twist 150), so the tag hangs on the wall side and the brass key faces the room. Cameras, framing and pixel sizes are unchanged (`heroes/heroes_log.txt`).
- **The four grouped heroes:** `heroes/doors_lineup_pull_face.png` (4800 × 1350; the push face is `doors_lineup_push_face.png`), `heroes/lock_exploded.png` (1600 × 1200; elevation `lock_exploded_front.png`), `heroes/keys_board_hung.png` (1600 × 1200; with `keys_board_hung_close`, `keys_hook_hung`, `keys_cabinet_hung`, `keys_parts_lineup_front`), `heroes/windows_lineup_face_a.png` (4000 × 1100). Each has an `_alpha` copy.
- **What changed visibly:** oak and ward leaves now have vertical grain; the steel frame's saddle is dark bronze (no lit line under the door); larger crossbar latch case; domed knob face; 1.5 mm escutcheon edge; the tag hangs behind the key on every host; the key board's grain runs vertically; the new EXIT signs. The L0 and Exit wallpapers are main's hard-edge print (not a kit change).
- **Slots:** `heroes/slots/` was regenerated with `tools/slots.py`. Four sources moved off the stale `png/` set: Escutcheon, Knob and the raised mini-blind now come from `png_fix/`, and `Kit_ExitSign_persp` from the exit-sign track's three-view (`../../exit_sign/threeview/png/`). `tools/build_index.py` `PROPOSAL_SLOTS` and `index.json` `proposalSlots` record the new sources. Filling the slots in our proposal section `2748:6099` is the Figma stage's job.
- **Check sheets (before | after):** `../images/heroes_fix_doors.jpg`, `heroes_fix_doors_push.jpg`, `heroes_fix_lock.jpg`, `heroes_fix_keys.jpg`, `heroes_fix_windows.jpg`, and `heroes_fix_slots.jpg` (all 26 slot crops). Logged as **VL225** (`2964:6869`, PASS) in the Figma verification log.
- **Still blank or known:** tag inserts and sign faces (missing P-4 surfaces), the L0-F veneer stripes (`DoorVeneer_A`), studio-bright chrome.

## 9. Update 2026-10-14 (kit-threeview render stage, resumed after the 2026-10-08 stop)

- **Main holds the kit.** `interactables_kit.sh` was applied to main on 2026-10-14 (`MANIFEST OK`); its dry run now reports `ALREADY APPLIED`. Everything in this folder therefore shows main's models (§2 has the file-by-file check). Main's `Resources/Surfaces` and URP `Settings` are also byte-identical to the render clone's, so the materials match too.
- **Toolchain after the macOS 27.0.1 upgrade:** Unity 6000.3.10f1 batch compile of the re-synced clone `W/proj_3view_int` (main `0a9ee7f` + working tree) gave 0 errors (`W/logs/3view_int_compile_1014.log`). Blender 4.3.0 rendered a Cycles test frame.
- **Window lineup corrected.** The 10-08 window heroes still drew W-RN with `Kit_WindowFrame_Steel_Enamel`, the older entry in `../10_spec.md` line 645. The fix pass's member map (`../30_final.md` §5.5, critic M2) and this hand-off's `assets` give W-RN the dark-bronze `Kit_WindowFrame_Steel` that the Run doors use.
  - Re-rendered with `-heroOnly windows` (the clone harness now defaults W-RN to `Kit_WindowFrame_Steel`; `-heroWRN <frame>` overrides it; copy `FrontRoomsInteractableHeroes.cs.txt`).
  - W-L0, W-OF and W-EX came out **pixel-identical** to the 10-08 files, which also confirms that the 10-08 heroes equal what current main renders.
  - Replaced: `heroes/windows_lineup_face_a(_alpha).png`, `heroes/window_W-RN_face_a(_alpha).png`.
  - Kept: the 10-08 enamel render as `heroes/window_Steel_Enamel_face_a(_alpha).png`, which still feeds the proposal slot `img:Kit_WindowFrame_Steel_Enamel_persp` (its crop in `heroes/slots/` is unchanged), and the 10-08 lineup in `heroes/_wrn_enamel_2026-10-08/`.
  - Check sheet: `../images/heroes_1014_windows_wrn.jpg`.
- **Data fixes:** §5.1 `exit_sign_p` is Y 2.18 (was 2.28); every other anchor quoted in §5 was checked against main's sidecars and matches. `index.json` gained `assetsMeta.keyUi` (Red's HUD key decision, §6) and `heroesMeta.update20261014`, and `assetsMeta.source.models` now says the kit is in main. `heroes/hero_notes.json` carries the current hero notes (key hung pose v2, W-RN). `tools/build_index.py` keeps `heroesMeta` on a rebuild, and `tools/build_assets.py` writes the new `source` and `keyUi` text.
- **Verification log:** **VL228** `3117:7031` (hand-off three-views, 57 of 57 assets, PASS; pending item 6 of 10-08) and **VL229** `3117:7041` (W-RN hero on the steel frame, PASS). The images are `../images/threeview_handoff_heroes57.jpg`, `../images/threeview_handoff_views.jpg` and `../images/heroes_1014_windows_wrn.jpg`.
- **Not done here (other owners):** the K-sheets in PROP KIT `2324:852` (yours; 13 sheets from the 18 `png_fix` kits, §10), filling the proposal slots (Figma stage), the missing P-4 surfaces and the satin-chrome tune (visual chat), and the door/key facade code (visual chat with the map chat).

## 10. Update 2026-10-14 (kit-threeview Figma stage)

- **Start here:** `HANDOFF_平面视觉.md` is the cover note for 平面视觉: paths, counts, the K46–K80 refresh table by node id, and what is unusual.
- **Sheet refresh, measured:** of the 18 `png_fix` kits, 14 differ from your `png/` images and 4 are pixel-identical (`Kit_KeyCabinet`, `Kit_WindowFrame_Wood`, both mini-blinds). That makes **13 sheets to refresh**, not 18: K46, K47, K48, K51, K52, K53, K57, K60, K68, K73, K75, K77 and K78. K53 also changes its dims label.
- **K numbers:** none new for this kit. K81 is the placard's Evacuation plan; the next free cells in `2324:852` are K82 and K83, and a further row would run into PROP KIT · REAL vs MODEL `2349:852`.
- **Our proposal section** `2748:6099` (moved to x 25257, y 13920) now shows these renders. We filled 25 slots from `heroes/slots/`, put kit names on the chips, and added **DW14 · The kit, as built** (`3122:7042`) from four new crops `heroes/slots/DW14_*.png`, made by `tools/dw14_crops.py`. The record is `../proposal/02_figma.md` §P.

## 11. Update 2026-10-14 (gap close after the kit-threeview check)

- **New renders** in the same clone and harness as the window heroes (`W/proj_3view_int`, main `0a9ee7f`; `FrontRoomsInteractableHeroes.RunBatch -heroOnly deskkey,windowclose`, log `W/logs/3view_int_gapclose_final2_1014.log`; harness copy `FrontRoomsInteractableHeroes.cs.txt`, now ASCII only):
  - `heroes/keys_desk_overedge.png` (+ `_alpha`, + `_wide`): the surface key in the **over-edge pose**, the only surface pose the fix pass allows (`../30_final.md` §5.4 rule 3, critic C2). The key lies flats-down on the `Kit_OfficeDesk` top, its tip in the desk; only the bow's end passes the front edge, so the ring can drop through the hole. The red `Kit_KeyTag_Long` hangs down the front face over the pedestal;
  - `heroes/window_W-OF_stop_close.png`: `Kit_WindowFrame_Steel`, camera 0.3 m from the lower-left corner as seen from face A (world +X, inside the opening). Two slotted oval-head screws on the loose stop, the mitre, the tape line. Re-lit the same day (below);
  - `heroes/window_W-L0_stool_close.png`: `Kit_WindowFrame_Wood`, camera 0.3 m from the lower-right corner as seen from face A (world -X). Stool horn, nosing, apron, casing and glazing stop.
  - Corners are named as seen from face A, as DW08 and the close-ups show the windows (world +X is image left there). Corrected 2026-10-14 18:0x: these lines said W-OF "lower right" and W-L0 "lower left".
- **Crops:** `tools/slots.py`, with 3 new rows in `tools/build_index.py` `PROPOSAL_SLOTS`, made `heroes/slots/KeySet_Desk_persp.png` (852 × 632), `Kit_WindowFrame_Steel_close.png` (1152 × 632) and `Kit_WindowFrame_Wood_close.png` (1152 × 634). The record of the uploads to `2748:6099` is `../proposal/02_figma.md` §P.8. Corrected 2026-10-14 18:0x: this line said the 26 earlier crops came out pixel-identical. A scratch run of `tools/slots.py` reproduces **28 of the 29** crops on disk pixel for pixel. `Kit_ExitSign_persp` differs (max 50/255): its source `../../exit_sign/threeview/png/Kit_ExitSign_persp.png` was rewritten at 17:22 by the exit-sign fix1 work, which is not merged into main. Its crop here and the DW12 tile `2753:6293` stay the 2026-10-08 version.
- **`index.json`:**
  - `heroes` now lists every `_alpha` file. The four hung-key heroes had `alpha: null` and the note "no transparent copy", although their `_alpha` files exist. Those copies are opaque because the wall fills the frame, and the notes now say so;
  - `tools/build_index.py` lists any `_alpha` that exists and reads its opacity from the file. `tools/build_assets.py` `sync_hero_alpha()` fills in a missing `_alpha` path when it rebuilds an older `index.json`. The new `heroes` and `proposalSlots` blocks were taken from a scratch run of the edited `build_index.py`, so a rebuild gives the same. (A full `build_index.py` run also rewrites `sheets` and `kits` from whatever clone `TV_CLONE` points at, and drops `counts.pngFix`. That was already so before this pass; the scratch run with today's clone changed both blocks, so it was not written to this folder);
  - `counts` reads heroes 37, proposalSlots 29, and `heroesMeta.gapClose20261014` records the pass.
- **K53** (`2653:9218`) needs its view slots resized before the swap. The crossbar's `png_fix` views grew (front 471 × 92 → 487 × 122, side 80 × 92 → 80 × 122, top 471 × 80 → 487 × 80), so the FIT slots become 243.5 × 61, 40 × 61 and 243.5 × 40. The steps are in `HANDOFF_平面视觉.md` §3. No other `png_fix` image changed size.

## 12. Fix 2026-10-14 18:0x (after the verifier's check of the gap close)

- **W-OF close-up re-lit.** At the shot's camera yaw (147°) the camera-relative rig with `keyFromRight` put the key (world z −0.19) and the fill (z −0.09) behind the wall, so no face-A surface took direct light: 6.4 % of the pixels exactly black, mean luminance 0.22 (DW08's W-OF tile 0.68, the W-L0 close-up 0.33), and the two stop screws read as dots. The harness now has a world-space rig for this shot only (`PlaceRig`, `worldKeyFrom` / `worldFillFrom`): key from (−0.45, 0.45, 0.77), the room side, above and to the right as seen from face A, intensity 2.0; fill from (0.30, 0.20, 0.93), intensity 0.5. Ambient, cube, shadows and camera are unchanged. Picked from 18 candidate rigs (`W/int_work/gap_close_fix/render_explore*/`); log `W/logs/3view_int_gapfix_final_1014.log`; harness copy `FrontRoomsInteractableHeroes.cs.txt` (ASCII, no tuples, Unity compile 0 errors).
- **Measured** (Rec. 709 luminance on sRGB values): the render 2304 × 1264 has mean 0.340, 0.001 % of pixels exactly black, 0.012 % under 0.02; the crop has mean 0.340 and 0.07 % under 0.02, all on the black tape line. The W-L0 close-up, rendered in the same run, is pixel-identical to the file here.
- **Replaced:** `heroes/window_W-OF_stop_close.png`, its `_alpha` copy (opaque, same pixels) and `heroes/slots/Kit_WindowFrame_Steel_close.png` (1152 × 632, `tools/slots.py` run in a scratch tree; only this crop was copied here). Uploaded to DW09 `2751:6245` and VL271 `3141:7218`: `../proposal/02_figma.md` §P.9.
- **Left:** re-crop DW12 only after exit_sign_fix1 lands in main.
