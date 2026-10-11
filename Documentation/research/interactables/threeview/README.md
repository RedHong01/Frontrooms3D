# Interactables three-view hand-off (for 平面视觉)

From the visual chat (游戏视觉), interactables-kit workflow, three-view stage, **2026-10-08**. It covers the doors, locks, keys and windows after the fix pass (`../30_final.md`).

平面视觉 places the sheets itself as **K46–Kxx · INTERACTABLES (doors · locks · keys · windows)** inside PROP KIT `2324:852`, re-rendering through its own `Tools/three_view` pipeline. We did not edit Figma.

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
| `heroes/` and `index.json` → `heroes`, `proposalSlots`, **`heroesMeta`** | **Re-rendered 2026-10-08 20:00 on the fix-pass models** (§8): the four grouped heroes (door lineup, lock exploded, key + ring + tag + hosts, window lineup), the 8 door sets and 4 window members, and the 26 proposal-slot crops in `heroes/slots/`. The 10-04 versions are in `heroes/_pre_fix_2026-10-04/`. |
| `png_fix/<Kit>_<top|front|side|persp>.png` | The fix pass's three-views of the **18 kits that changed** since K46–K80 (same tool and scales as `png/`, 72 PNG + `manifest.json`). Use them to refresh those 18 sheets once the kit is applied to main. |

`index.json` has its own `fields` list in `assetsMeta`, so a script can check that each entry carries exactly those fields.

## 2. The models: where they are, and how to load them

- **The FBX path** is `Assets/Resources/Props/Models/<name>.fbx`, next to its sidecar `<name>.json`. The sidecar holds bounds, slots, anchors, `motion` and LOD data.
- **The current builds are not in main yet.** They sit in `/Users/redwang/FrontRoomsVisualWork/proj_int` and in the promote payload `W/int_work/fix/payload/` (102 files; `SHA1SUMS.txt`). The interactables merge stage copies them over main's `Kit_*` files and keeps main's `.meta` GUIDs.
  - Until that merge lands, render from a copy of `proj_int`, or copy the payload over your clone's `Assets/Resources/Props/Models/`.
  - A clone of main alone shows the **pre-fix** models for 50 FBX.
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
| Exit sign (exit-sign track) | frame `exit_sign_p` (−0.08, 2.28, 0.5) | — |

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

**Spare members:** `Kit_WindowFrame_Steel_Enamel` has had no map member since the fix pass gave the Run window the bronze steel frame; the Run doors use that frame too. The brass mortise parts are spares (§4).

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

- **Models:** the fix-pass payload (`W/apply/interactables_kit_payload`, 102 files) applied with `interactables_kit.sh --apply` into the clone `W/proj_3view_int` only (`MANIFEST OK`), over main `bc0aa79` plus its working tree at 19:53. So the heroes show what main will hold after the visual chat runs that apply, including the exit-sign kit merged at 19:40.
- **Renderer:** `FrontRoomsInteractableHeroes.RunBatch -heroOut <dir>` (copy: `FrontRoomsInteractableHeroes.cs.txt`). The only change from 10-04 is the hung pose: v2 from the hosts' sidecar `hungPose` (ring yaw 120, key twist −30, tag twist 150), so the tag hangs on the wall side and the brass key faces the room. Cameras, framing and pixel sizes are unchanged (`heroes/heroes_log.txt`).
- **The four grouped heroes:** `heroes/doors_lineup_pull_face.png` (4800 × 1350; the push face is `doors_lineup_push_face.png`), `heroes/lock_exploded.png` (1600 × 1200; elevation `lock_exploded_front.png`), `heroes/keys_board_hung.png` (1600 × 1200; with `keys_board_hung_close`, `keys_hook_hung`, `keys_cabinet_hung`, `keys_parts_lineup_front`), `heroes/windows_lineup_face_a.png` (4000 × 1100). Each has an `_alpha` copy.
- **What changed visibly:** oak and ward leaves now have vertical grain; the steel frame's saddle is dark bronze (no lit line under the door); larger crossbar latch case; domed knob face; 1.5 mm escutcheon edge; the tag hangs behind the key on every host; the key board's grain runs vertically; the new EXIT signs. The L0 and Exit wallpapers are main's hard-edge print (not a kit change).
- **Slots:** `heroes/slots/` was regenerated with `tools/slots.py`. Four sources moved off the stale `png/` set: Escutcheon, Knob and the raised mini-blind now come from `png_fix/`, and `Kit_ExitSign_persp` from the exit-sign track's three-view (`../../exit_sign/threeview/png/`). `tools/build_index.py` `PROPOSAL_SLOTS` and `index.json` `proposalSlots` record the new sources. Filling the slots in our proposal section `2748:6099` is the Figma stage's job.
- **Check sheets (before | after):** `../images/heroes_fix_doors.jpg`, `heroes_fix_doors_push.jpg`, `heroes_fix_lock.jpg`, `heroes_fix_keys.jpg`, `heroes_fix_windows.jpg`, and `heroes_fix_slots.jpg` (all 26 slot crops). Logged as **VL225** (`2964:6869`, PASS) in the Figma verification log.
- **Still blank or known:** tag inserts and sign faces (missing P-4 surfaces), the L0-F veneer stripes (`DoorVeneer_A`), studio-bright chrome.

