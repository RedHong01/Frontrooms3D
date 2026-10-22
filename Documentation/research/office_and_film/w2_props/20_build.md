# W2 · Office prop period fixes: build report

Status: **BUILT, FIX ROUND 1 DONE, VERIFIED IN THE CLONE, NOT LANDED.** Build stage of workflow `office-props-w2`, 2026-10-22 06:52–07:22; fix round 1 07:40–08:30. Owner: 游戏视觉. Spec: `10_spec.md` (now with fix-1 errata at the top). Red's decision (2026-10-21): "the torchiere to brass with a glass/acrylic shade; the task chair to armless grey/coloured fabric (keep the black armed one as a variant)".

**What was built:**
- `Kit_Torchiere` is now the brass halogen torchiere: 1.689 m, frosted glass bowl, a knurled dimmer on the cup, a beaded brass base, lamp off. Since fix round 1 it has two slots of its own, `Prop_BrassPolished` and `Prop_FrostedGlass`.
- `Kit_TaskChair` is now the armless 1990 task chair in grey fabric.
- New: `Kit_TaskChair_Navy` and `Kit_TaskChair_Armed` (the old black armed chair).
- The Office gives 30 % of rooms navy chairs and about 15 % of stations the armed chair. Positions are unchanged.
- In furniture piles the torchiere stays walk-through, as before W2; in the L0 waiting-room module it now blocks.

**Verification (fix round 1, main `0ea38bb` + working tree at 07:59):**
- 0 `error CS` in every Unity run. Roslyn `--all` on main + payload: COMPILE OK for macOS and iOS, with and without the separate RenderSetup hunk (08:09).
- Map suites all PASS. Relay nav also PASS on seeds 293 and 12, which have a pile torchiere next to the spawn.
- Play mode: DressMapStepTest PASS; DoorShotTests 191 / 0; MainScenePlaytest PASS on 2554, 20388, 2554 + glass, 2554 + doors and 293 (pile torchiere in the spawn chunk).
- Map signatures: 0 determinism failures. 76 of 500 chunks change, and every changed object is explained (§12.7): chairs in 46 chunks, the module torchiere in 25, map piles rebuilt in 5 (4 seeds). 0 other lines.
- Apply package `W/apply/office_props_w2` (v2): DRY RUN OK on main, `rebase_apply.sh` PASS, scratch `--apply` MANIFEST OK then ALREADY APPLIED (§13).
- Every Unity run was UNDER LOAD (1-minute load 62–710).

## 0. Fix round 1 at a glance

| Verifier finding | Fix | Evidence |
|---|---|---|
| era-art 1: brass reads dark bronze / mustard | torchiere-only `Prop_BrassPolished` (sRGB 0.95/0.87/0.62, smoothness 0.78, metallic 1), new GUID; SurfaceDef in a separate diff | pole at 0.3 m L\* 19 → 27; pole ÷ wall luminance 0.22 → **0.41** (Sears N pole ÷ sweep **0.39**); three-view pole L\* 45 → **67** (Sears 63); §12.1 |
| era-art 2: four black rings on the bowl | steps cut to 4 mm, sloped 15° (no downward face); `Prop_FrostedGlass` without a normal map | darkest scan point 0.49× → **0.91×** of the band level; §12.2 |
| era-art 3: knob reads as a hole or coin | custom knob: 12 ribs, 1.8 mm 45° chamfer, domed face | rim and dome catch highlights; §12.3 |
| era-art 4: base reads as discs with black gaps | 5 rounded beads (riser 70°, crest 35°, crisp groove), 40 sides | base median L\* 51 → 66 (Sears 57), a highlight per bead; §12.4 |
| report contradicts itself (§7 vs §11.2) | one verdict, measured | §7, §12.5 |
| eng 1: pile torchieres block | tag `pile_walkthrough`; `FrontRoomsFurniturePile` skips colliders for it; spec §3.9 corrected | 42 pile torchieres in seeds 1–400, all with 0 colliders; Relay nav + playtest PASS on seeds with one; §12.6 |
| eng 2: signatures checked for 4 of 18 seeds | object-level dumps of all 20 seeds, without and with W2 | 76 chunks explained, 0 unexplained lines; map piles change for 4 of 20 seeds; §12.7 |
| eng 3: play-mode suites not run | all run, all PASS | §12.8 |
| eng 4: no apply package | `office_props_w2` v2 with bases (ABSENT recorded), keep, GUID check, base copies, diff | §13 |

## 1. Clone and runs

**Clone `W/proj_w2`.** It was made with `mkclone.sh proj_w2` by the first run of this stage (2026-10-21 22:07). There is still only this one clone.
- The build stage re-synced it from main three times (06:57, 07:13, 07:17), the way `mkclone.sh` does: rsync of the five folders, local patches, `install_tools.py`.
- **Fix round 1** re-synced it once more at 07:59 (main `0ea38bb` + working tree; main had changed `FrontRooms3DGame.cs`, `FrontRoomsCloudBuild.cs` and `Tools/build_mac_testflight.sh` since 07:17), took the "no W2" dumps, then put the W2 payload and fix 1 back (§12.10).
- For the play-mode runs it also copied main's FMOD banks into the clone (`FMOD/FrontRooms/Build`, 1.5 MB; TOOLS.md §2). Without them Play logs one `BankLoadException`, and the autopilot counts that as an error, so its verdict is FAIL (runs 71 and 72 in §12.10).

**Main at the fix-1 sync (07:59):**

| File | sha1 |
|---|---|
| `FrontRoomsOfficeKit.cs` | `cfe0105a` |
| `FrontRoomsFurniturePile.cs` | `901503fc` |
| `FrontRoomsRenderSetup.cs` | `256cf3a4` |
| `FrontRoomsMapWorld.cs` | `7ef0ca45` |
| `FrontRooms3DGame.cs` | `88c93b09` |

The bases of every payload file were re-checked at 08:18 by the package dry run: all 23 match.

The build stage's own runs (ids 11–43) are kept in `W/w2_props/logs/run_w2.status`; fix round 1's runs (ids 50–85) are in `run_fix1.status` and listed in §12.10.

| Run (log id) | What | Time | 1-min load | Result |
|---|---|---|---|---|
| 11_compile_probe_before | compile + W2DressProbe on main (no W2) | 23 s | 303 → 330 **UNDER LOAD** | 0 `error CS` |
| 12_blender_build | `build_asset.py torchiere task_chair task_chair_armed` | 26 s | n/a (no Unity) | 3 kits + Navy variant |
| 13_reimport_probe_after_loddump | W2Chain: reimport (sidecar first) + probe + `FrontRoomsCxVisLodDump` | 54 s | 399 → 375 **UNDER LOAD** | 0 `error CS` |
| 20_kitlookdev_s3 | `FrontRoomsKitLookdev -kitSeed 3 -kitShots asset_* (4), office_entrance, office_desk, office_diagonal, pile*` | 20 s | 298 → 426 **UNDER LOAD** | 10 frames |
| 25_w2lookdev_r3 | `W2Lookdev` (heroes, 0.3 m, LOD, own light, L0 placement, Office share) | 21 s | 144 → 135 **UNDER LOAD** | 66 frames |
| 22_threeview / 26_threeview_1x | `FrontRoomsThreeView -threeViewOnly` the 4 kits (2× W/tools; 1× hand-off renderer) | 30 s / 18 s | 340 / 135 **UNDER LOAD** | 16 + 16 PNG + manifests |
| 23_cxsuites / 24_cxsig | map suites + signatures, base 06:57 | 80 s / 50 s | 253 / 118 **UNDER LOAD** | PASS / 0+0 |
| 31 / 32 / 33 | the same three on base 07:13 | 50 / 90 / 40 s | 264–287 **UNDER LOAD** | 0 errors / PASS / 0+0 |
| 41_cxsig_before | signatures on base 07:13 without W2 | 40 s | 190 **UNDER LOAD** | 0+0 |
| 42 / 43 | W2 put back + reimport; signatures again for the per-line detail | 26 / 36 s | 175 / 162 **UNDER LOAD** | byte-identical to run 33 |
| 50–85 | **fix round 1**: see §12.10 | | 62–710 **UNDER LOAD** | |

## 2. Files (the payload, not landed)

The payload is staged in `W/w2_props/payload/` with paths relative to `Frontrooms3D/`, and packaged as `W/apply/office_props_w2*` (§13). The full lists are `W/w2_props/payload_sha1.txt` and `payload_base_sha1.txt`; the latter now records `ABSENT` for every new path. The round-0 payload is kept in `W/w2_props/payload_r0/`.

| Path | sha1 (new) | Base in main (sha1) | Note |
|---|---|---|---|
| `Assets/Resources/Props/Models/Kit_Torchiere.fbx` | `f58d1694` | `ea8a7e66` | replaced (fix 1); GUID `a2e24975…` kept |
| `…/Kit_Torchiere.fbx.meta` | `b108376f` | `ba7eef4e` | Unity's post-import meta: same GUID; remaps to the two new materials, a third LOD % |
| `…/Kit_Torchiere.json` | `b312742b` | `35c348b3` | sidecar (fix 1: slots, tag `pile_walkthrough`, triangles) |
| `…/Kit_Torchiere.json.meta` | `6aa1caad` | `6aa1caad` | unchanged (kept, GUID `a954ac71…`) |
| `…/Kit_TaskChair.fbx` | `99ddeec2` | `7a5e244d` | replaced; GUID `99ce0ad6…` kept |
| `…/Kit_TaskChair.fbx.meta` | `f4167824` | `e876a57b` | post-import: same GUID; adds the `Prop_FabricCubicle` remap and a third LOD % |
| `…/Kit_TaskChair.json` | `a7a8f53a` | `a6950823` | sidecar |
| `…/Kit_TaskChair.json.meta` | `eed7c0e7` | `eed7c0e7` | unchanged (kept, GUID `7a8141bd…`) |
| `…/Kit_TaskChair_Navy.fbx` + `.meta` | `af4270ca` / `ad5ed974` | ABSENT | GUID `216f0b38920c4f789f294729a9e867e3` |
| `…/Kit_TaskChair_Navy.json` + `.meta` | `e65e52ba` / `0f60d02b` | ABSENT | GUID `d105b27e4fb44bcd9f275d700cb92304` |
| `…/Kit_TaskChair_Armed.fbx` + `.meta` | `ea42b078` / `6f89f8aa` | ABSENT | GUID `3cd9526245414452b925066da4411b32` |
| `…/Kit_TaskChair_Armed.json` + `.meta` | `64330777` / `e618e6d1` | ABSENT | GUID `13007464972f425c94c69ea5485fecb6` |
| `Assets/Resources/Surfaces/Prop_BrassPolished.mat` + `.meta` | `0605fb3d` / `6805e3fb` | ABSENT | **fix 1**; GUID `3475294186a44ecfbb6f2bca61855237` |
| `Assets/Resources/Surfaces/Prop_FrostedGlass.mat` + `.meta` | `ce77be63` / `c5be76fb` | ABSENT | **fix 1**; GUID `47927cf621bc4cd6908122cf0ee40342` |
| `Assets/Scripts/Office/FrontRoomsFurniturePile.cs` | `b6a48041` | `901503fc` | **fix 1**: `pile_walkthrough` (§12.6); visual-owned |
| `Assets/Scripts/Office/FrontRoomsOfficeKit.cs` | `d566b5a6` | `cfe0105a` | chair choice (§6) |
| `Tools/Blender/frontrooms_kit/assets/torchiere.py` | `49706400` | `0d1a3af9` | rewritten (§3; fix 1) |
| `Tools/Blender/frontrooms_kit/assets/task_chair.py` | `af3849b9` | `1d3ddc99` | rewritten (§4) |
| `Tools/Blender/frontrooms_kit/assets/task_chair_armed.py` | `b4b6ae87` | ABSENT | today's chair, renamed (§4) |

**Not in the payload:** the two SurfaceDefs for `FrontRoomsRenderSetup.cs`, as a separate diff: `W/apply/office_props_w2_rendersetup.diff` (sha1 `a2aed9e4`, made against main's `256cf3a4`; source `W/w2_props/fix1/rendersetup/`). outlets_p4b is changing RenderSetup tonight, so this hunk lands after it. Without the hunk the two .mat files work as shipped: the kit importer remaps slots by file name. The hunk only makes "Set up" regenerate them with the same values (every field matches the .mat).

**About the new GUIDs:**
- They were made with `uuid4` or by hand and grepped against every `.meta` in main's `Assets` and `Packages`: 0 hits. The package's dry run repeats that check (§13).
- `kitlib.py` and `build_asset.py` are unchanged. The two new slots are registered from `torchiere.py` with `kitlib.register_slot` (Blender preview colours only), so `kitlib.py` (also touched by `inspect_data`) stays out of the payload.

**Compile gate.** `roslyn_check.py --tree <main at 08:09> --override FrontRoomsOfficeKit.cs,FrontRoomsFurniturePile.cs[,FrontRoomsRenderSetup.cs with the hunk] --all`: COMPILE OK, 0 errors, for macOS and with `--ios`, both with and without the RenderSetup hunk (`W/w2_props/fix1/roslyn/`).

**Clone-only harness** (ASCII, no tuples, never merged): `W/w2_props/tools/`, installed in the clone as `Assets/Editor/W2/`.
- `W2KitReimport`, `W2DressProbe`, `W2Chain`, `W2Lookdev`, `vl_images.py`: as in the build stage.
- Fix round 1 added: `W2SigDetail` (object-level dump of every chunk of the 20 signature seeds), `W2Fix1Chain` (reimport, probe, W2SigDetail, DressMapStepTest, DoorShotTests, LOD dump), `W2Variants` (material line-up on the torchiere), `W2PileSeeds` (map piles with a torchiere over a seed range; Relay nav on a chosen seed), `W2Playtest` (main's playtest with a longer wall-clock guard), `vl_images_fix1.py`; and `W/w2_props/fix1/sigdiff.py`, `measure.py`, `bowlscan.py`, `build_package.py`, `make_rs_diff.py`.

## 3. Kit_Torchiere (brass halogen torchiere)

Built as specified in spec §3, with the fix-1 corrections:
- 1.689 m tall: a brass rim band (4 mm, 2 mm proud), then a frosted bowl Ø0.39 × 0.11 with **4 terraces: tall sloped bands (41°) and 4 mm steps sloped 15° up and out**, on a polished brass cup Ø0.10 with a rolled lip and a neck collar;
- a plain brass pole Ø0.024;
- a Ø0.245 base: a 20 mm vertical rim with a rounded top edge and a 14 mm flat ring, then **5 rounded beads** (each a 70° riser rolling over a 35° crest into a near-flat tread, with a crisp groove at its foot) up to a Ø0.045 boss with a rounded shoulder;
- a black felt disc and a cord to a two-pin plug;
- an R7s tube on a brass strap and holders;
- a **knurled brass dimmer knob** (12 ribs, a 1.8 mm 45° chamfer and a low dome on its face) on the cup front at 1.555 m.

**Segments:**

| Part | Sides |
|---|---|
| bowl outside, bottom ring | 64 |
| bowl inside (seen only from above) | 32 (was 48) |
| base, felt | 40 (was 48: spec §8 fallback; 0.38 mm sagitta ≈ 0.9 px at 0.3 m) |
| cup | 32 |
| rim band | 64 |
| pole | 20 |
| knob | 24 points |

The bowl's moulded groove per tread is gone: at 1 mm it made 3° kinks that showed in no frame, and its 512 triangles paid for the base beads. The bowl wall clears the outside by ≥ 2.6 mm everywhere (4 mm at the foot and lip). Smoothing angle 50°: the bowl steps (26°), bead crests (35–37°), rounded rim and boss edges (≤ 49°), knob chamfer and cup shade round; the rim band corners, the bead grooves (72°) and the collar stay sharp.

**Slots:** `Prop_BrassPolished`, `Prop_FrostedGlass` (both new, torchiere-only, §12.1–12.2) and `Prop_PlasticBlack`. `Prop_Brass`, `Prop_PlasticWhite` and `Prop_SteelBlack` are no longer used by this kit; `Prop_Brass` itself is unchanged for its other kits.

**Lamp off.** Nothing is emissive.

**Sidecar (Unity space):**
- bounds 0.390 × 1.689 × 0.390;
- one box collider, 0.39 × 1.689 × 0.39, centred at y 0.845. It replaces `noCollider` for module placements; in piles it is not spawned (§12.6);
- footprint 0.39 × 0.39; minCeiling **1.739**; placement Floor;
- anchors: `light` (0, 1.62, 0) and `dimmer` (0, 1.555, 0.062);
- `lodDistances` [8, 20, 54];
- tags `pile`, `lamp`, `domestic`, **`pile_walkthrough`**; pile block unchanged (Tall, mass 0, domestic70s, topper, Upright/EdgeLean, tolerance 0.3).

**One deviation from the spec:** `LODS = (0.35, 0.088)` instead of 0.09, so that LOD2 lands under 400.

## 4. Kit_TaskChair (armless, grey), Kit_TaskChair_Navy, Kit_TaskChair_Armed

Unchanged in fix round 1 (FBX, sidecars and metas byte-identical to the build stage).

**`task_chair.py`** follows spec §4. It is armless and after Sears 1993 M/N:
- a plain contoured seat pad, no welt: 0.48 × 0.46, top at 0.45 m;
- a shield/oval back 0.44 × 0.42, top at 0.93 m;
- a visible black back bar, a back-angle knob at the bar root, and a back-height knob with four chrome screws on the rear;
- a long height lever to the user's right (−X): a chrome rod with a black paddle;
- a smooth black housing Ø0.07 with a chrome piston Ø0.05 showing for about 0.06 m;
- today's 5-star base with twin casters.

There is no tilt lever and no front tension knob.

**Hero detail:** 8 corner steps on the pads; the hub, housing, piston and socket are 24-sided; the caster wheels are 16-sided.

**Slots:** `Prop_FabricCubicle`, `Prop_PlasticBlack`, `Prop_Chrome`.

**The navy chair** is made by `VARIANTS = {"Kit_TaskChair_Navy": {"Prop_FabricCubicle": "Prop_FabricNavy"}}`. Its sidecar is the same, pile block included.

**Docstrings.** The brand names are gone from both modules. A grep for them finds 0 hits.

**`task_chair_armed.py`** is today's module with these changes only: `NAME = "Kit_TaskChair_Armed"`; `LODS = (0.40, 0.10)`; `lodDistances` [6, 12, 30]; no `kit.pile`; the docstring.

**Sidecars:**

| Kit | Bounds (w × h × d) | Collider | Footprint | minCeiling | Anchors | Pile |
|---|---|---|---|---|---|---|
| Kit_TaskChair / _Navy | 0.617 × 0.933 × 0.654 | 1 box = the bounds (centre −0.003, 0.467, −0.030) | 0.617 × 0.654 | 0.983 | `sit` (0, 0.45, 0.03), `back_top` (0, 0.93, −0.328) | Seat, mass 1, office90s, U/I/S/B, 0.45 |
| Kit_TaskChair_Armed | 0.617 × 0.953 × 0.653 | today's hand box 0.624 × 0.954 × 0.656 | 0.624 × 0.656 | 1.003 | as today (`back_top` y 0.95) | none |
| (old Kit_TaskChair) | 0.617 × 0.953 × 0.653 | 0.624 × 0.954 × 0.656 | 0.624 × 0.656 | 1.003 | | Seat |

Support `seat` is 0.40 × 0.38 at y 0.45 on all three. The new chair is within ±2 cm of the old bounds: it is 20 mm lower and its box is 7 mm narrower.

## 5. Triangles and LOD switches

Counts are from the sidecars and match Unity's import (`W2KitReimport`, fix 1: `W/w2_props/fix1/probe/w2_reimport.txt`). Switch distances are from `FrontRoomsCxVisLodDump` (`fix1/probe/cxvis_lod_dump.txt`).

| Kit | LOD0 | LOD1 | LOD2 | Budget | Switch (m), at screen height | LODGroup size |
|---|---|---|---|---|---|---|
| Kit_Torchiere (fix 1) | **4,392** | **1,536** | **385** | 4,500 / 1,600 / 400 ✔ | 8.0 / 20.0 / 54.0 cull (h 0.1351 / 0.0540 / 0.0200) | 1.689 |
| Kit_TaskChair | **4,634** | **1,620** | **416** | 5,200 / 1,800 (≤ 1,792) / 450 ✔ | 6.0 / 12.0 / 30.0 cull (h 0.0999 / 0.0499 / 0.0200) | 0.936 |
| Kit_TaskChair_Navy | 4,634 | 1,620 | 416 | same ✔ | same | 0.936 |
| Kit_TaskChair_Armed | 4,484 | 1,792 | 447 | ≈ 4,484 / 1,792 / 450 ✔ | 6.0 / 12.0 / 30.0 (h 0.1018 / 0.0509 / 0.0204) | 0.955 |
| (round-0 torchiere) | 4,478 | 1,567 | 394 | | | |
| (old torchiere / chair) | 636 / 4,484 | 267 / 1,792 | none | | default 10 % / 2 % | |

**Office cost.** A station chair at LOD1 averages 0.85 × 1,620 + 0.15 × 1,792 ≈ 1,646 tris, against 1,792 before (−8 %).

**Draws.** The torchiere's LOD0 has 3 submeshes (3 slots), up from 2 before W2, so each lamp costs +1 draw. The chairs keep 3.

## 6. Office dressing: the share and determinism

**The change (`FrontRoomsOfficeKit.cs`):**
- **Room lot:** `chairs = Mix((uint)seed)`, set in `Solve` before any placement. It is passed down as a parameter: `PlacePods` / `PlaceWallRows` → `BuildRun` → `BuildStation`. There is no static.
- **`ChairFor(room, station)`:**
  - if `Mix(room, station) < 0.15·2³²` and `Kit_TaskChair_Armed` exists, the station gets the armed chair;
  - otherwise, if `room < 0.30·2³²` and `Kit_TaskChair_Navy` exists, it gets the navy chair;
  - otherwise `Kit_TaskChair`.
- **`Mix`** is the lowbias32 integer finaliser.
- **The rng is untouched.** The chair keeps its `Chance(.85)`, z, x and yaw draws in today's order. Only the asset name in `b.Kit(...)` changes.

**Probe (`W2DressProbe`).** It covers 3 floors (11.84², 5.84 × 8.84, 17.84 × 11.84) × seeds 1–40 = 120 rooms, 16,375 kit instances and 1,273 chairs. The fix-1 "after" run (main 07:59 + W2 + fix 1) gives the same hashes as the build stage's, line for line, piles included (`W/w2_props/fix1/probe/`).

| | before (main) | after (main + W2) |
|---|---|---|
| hash of every placement | `24823d7fb4868945` | `51a0af9a54c32ad7` |
| **hash with chair names masked** | `9191f69f7e2815b5` | **`9191f69f7e2815b5`** (equal: every non-chair placement and every chair position, yaw and path is bit-identical) |
| chair choice list hash | `12c2ea315520d11c` | `667ac93a0fd4d27c` |
| same seed twice (seeds 1–12) | 0 mismatched lines | 0 |
| BeginDress `Step(0.05 ms)` vs `Dress` (seeds 1–12) | 0 | 0 |
| chairs | 1,273 `Kit_TaskChair` | 618 grey · 454 navy · 201 armed (**15.8 %**) |
| rooms with chairs | 119 | 69 grey, 48 navy, 2 with only armed chairs; **0 mixed colours** |
| pile library | 141 names, 45 pile pieces | 143 names, 46 pile pieces (`_Navy` joins; `_Armed` has no pile block) |
| probe test piles (one 3.2 m pile per seed 1–20, two builds each) | 0 repeat mismatches | 0 repeat mismatches; 16 of 20 seed hashes change |

**Every difference explained:**
1. The chair choice list differs only in the asset names. A row-by-row compare of `before_chairs.tsv` and `after_chairs.tsv` shows identical seed, room, path, x, z and yaw.
2. Navy rooms: 16 of the 40 seeds have `Mix(seed) < 0.30·2³²`: 9, 11, 13, 16–21, 23, 25, 27, 31–33, 37. That is 40 % of this small sample. Over seeds 1–1000 the share is 30.4 %, so navy chairs are 42 % of the non-armed chairs here by sampling, not by a bias in the hash.
3. Armed: 15.8 % here; 15.06 % over 1000 seeds × 200 nodes.
4. Piles: the library is sorted by name and now holds `Kit_TaskChair_Navy`, and the chair and torchiere bounds are new. So every pick index after the navy chair and every fit test shift. **This row is the probe's own test pile** (one 3.2 m pile at a fixed spot per seed), not the map: the map's piles are counted in §12.7 (5 of the 6 piles in the 20 signature maps change).
   - Probe seeds 2, 4, 17 and 19 do not change: they are 6-piece tableaux that never reach those pieces.
   - Probe seeds 1–20 hold 13 `TaskChair` pieces before; after, 5 `TaskChair` + 6 `TaskChair_Navy`. The torchiere is in 3 piles before and 5 after.
   - Two builds of every seed still match.
5. The torchiere has no Office dressing change.

**Map signatures.** See §12.7 for the object-level account of all 20 seeds. The build stage's sentence "every line that changed is on a chair or torchiere path" was checked only on the first 4 seeds and was incomplete: 5 chunks in 4 other seeds hold map piles rebuilt with different pieces.

## 7. Look-dev (clone, game post stack, 1920 × 1080, 4× MSAA)

**Images.** The VL composites are in `images/`: JPG q85, all our own renders. Fix round 1 re-rendered every frame that shows the torchiere and rebuilt the composites that hold one; the chair-only composites are unchanged. The round-0 versions are kept in `W/_partial/w2_images_r0_0820/`.

| Image | Frames |
|---|---|
| `20_torch_hero_close.jpg` (fix 1) | hero 3/4, standing eye (1.62 m, 2.4 m away); **0.3 m:** cup + knob, bowl underside, rim + tube from above, base |
| `20_torch_ownlight.jpg` (fix 1) | lamp light on with troffers on; **its own light only** (troffers off: a 3000 K up-spot at the `light` anchor + a ceiling-bounce point); under; above |
| `21_torch_fix1_before_after.jpg` (new) | round 0 and fix 1 side by side, same cameras: 0.3 m cup/knob/pole, 0.3 m bowl underside, 0.3 m base, standing eye, 8 m |
| `20_chair_hero_close.jpg` | grey and navy heroes; **0.3 m:** navy seat, grey seat, lever + piston, back bar, casters |
| `20_armed_office_share.jpg` | armed hero + **0.3 m** arm; KitLookdev `office_desk` seed 3 (the gate frame); seed 1 grey room; seed 13 navy room |
| `20_pile_l0.jpg` (fix 1) | KitLookdev `pile`, `pile_close`, `pile_side` (seed 3, re-rendered), `office_entrance` (round 0); the L0_WaitingRoom placement and the **16 mm** bowl-to-wall gap |
| `20_lod_forced.jpg` (fix 1) | the 4 kits × LOD0/1/2, same camera (`LODGroup.ForceLOD`, 2.8 m, vFOV 30) |
| `20_lod_switch.jpg` (fix 1) | the 4 kits at each switch distance (vFOV 76, eye 1.62), with the two levels that meet there |
| `20_torch_threeview.jpg` (fix 1), `20_chair_threeview.jpg` | the 1× three-views on paper |

The side-by-side with the Sears 1993 p.319 N crops (third-party media) stays outside the public repo: `W/w2_props/fix1/cmp_sears_fix1.jpg`.

**Verdicts:**
- **Torchiere (fix 1).** It reads as polished brass with a frosted white bowl. Measured, not judged by eye (§12.1–12.4):
  - the pole now reflects its room, gold at every distance (at 8 m a thin gold line, where round 0 read copper-brown);
  - relative to its surroundings it is as bright as the Sears pole: pole ÷ wall luminance 0.41 at 0.3 m, Sears pole ÷ sweep 0.39;
  - in absolute terms it is darker than the catalogue (L\* 27–33 against the scan's 63), because the game's rooms are dark (walls L\* 40–42) and the zone reflection runs at half strength (§12.1, §11);
  - the bowl is evenly toned (no ring darker than 0.91× of the band level) and its steps show in the outline only, as on the scan;
  - the knob reads as a knob, and the base as beaded, polished brass with a highlight per bead.
- **Chairs.** They read armless; grey and navy separate clearly from the black base and shell. At 0.3 m the pads, the 16-sided wheels and the 24-sided piston hold up.
- **Spec §4.3 gate (grey chair vs the cubicle panel): PASS.**
  - In the KitLookdev `office_desk` frame (seed 3) and in `office_s1_TaskChair_front`, the chair separates from the panel. The black shell keyline does it, and so does the lit seat: seat vs panel ΔE76 **15.8**.
  - The back pad facing away from the troffers is close to the panel (ΔE76 **6.6**). The `Prop_FabricGrey` fallback was not needed and is not built.
- **LOD.** No pop is visible at the authored switch distances, and LOD2 keeps the silhouette.

The KitLookdev `asset_*` frames (in `W/w2_props/kit_lookdev2/seed3/`) are dark because of that tool's line-up room. Main's tool does the same for every kit, so the hero frames come from `W2Lookdev`.

## 8. Three-views for 平面视觉 · 印刷与演示

The hand-off is in `threeview/`; see its `README.md`. `Kit_Torchiere` was re-rendered in fix round 1 with the same renderers, scale and padding (1× hand-off renderer, oversample 1: run 66; 2× W/tools renderer: run 65), and its records in `handoff_manifest.json`, `2x/manifest.json` and `index.json` were updated (slots, triangles, tags, lede, swatches). The chair files are unchanged.
- `<Kit>_{front,side,top}.png` at 250 px/m (1 PNG px = 1 sheet px, 16 px padding, transparent);
- `<Kit>_hero.png` (1152 × 864);
- `handoff_manifest.json`;
- `index.json`, in the same schema as `research/outlets/threeview/index.json`;
- `2x/`: the 500 px/m set with the W/tools manifest, for print.

**Studio pole:** the torchiere's pole in the hero three-view now runs L\* 67–68 (round 0: 44–50); in the 2× front view median L\* 69 with highlights to 100 (round 0: 52). Sears N: median 63, highlights 87.

**Swatches:** polished brass `#F2DE9E` (was `#B08A4A`), frosted bowl `#EBE7DE` (was `#D9D5C8`), grey `#5A646E`, navy `#23304A`, black fabric `#1C1C1E`, black plastic `#1A1A1A`, chrome `#D8D8D8`.

## 9. Map suites (`FrontRoomsCxSuites.RunBatch -cxOnly suites`)

| Suite | Base 06:57 | Base 07:13 | **Fix 1** (main 07:59 + W2 + fix 1, run 61) |
|---|---|---|---|
| FrontRoomsMapVerification | 100 / 0 seeds | 100 / 0 | 100 / 0 |
| FrontRoomsRelayNavTest | PASS 60/60 | PASS 60/60 | PASS 60/60, 0 pass-throughs, 0 furniture frames |
| FrontRoomsMapInteractionTests | 163 / 0 | 174 / 0 | 174 / 0 |
| FrontRoomsLevelDesignerTests | 140 / 0 | 140 / 0 | 140 / 0 |
| FrontRoomsFixtureTickTests | 28 / 0 | 28 / 0 | 28 / 0 |
| FrontRoomsCameraRigTests | 33 / 0 | 33 / 0 | 33 / 0 |
| FrontRoomsCaptionsTests | 9 / 0 | 9 / 0 | 9 / 0 |
| FrontRoomsGlassShotTests | 20 / 0 | 20 / 0 | 20 / 0 |
| FrontRoomsStreamVerification | 5 / 0 | 5 / 0 | 5 / 0 |
| FrontRoomsGlassCompileCheck | no exception, 0 FAIL | no exception, 0 FAIL | no exception, 0 FAIL |

**Console noise.** Only the tests' own injected errors: 2 errors "forced build failure" (chunk (1, 0) left out) and 4 exceptions "test: a broken glass kit" in MapInteractionTests, plus module-prop warnings. None names a chair or the torchiere. Reports: `W/w2_props/cx_suites/`, `cx_suites_final/`, `cx_suites_fix1/`.

The play-mode gate suites are in §12.8.

## 10. For 关卡设计 (package notes)

**New kit names:** `Kit_TaskChair_Armed` and `Kit_TaskChair_Navy`. Both are Floor and office/seat. Only `_Navy` is a pile piece.

**New material names (fix 1):** `Prop_BrassPolished` and `Prop_FrostedGlass` (`Assets/Resources/Surfaces/`), used only by `Kit_Torchiere`.

**Frozen names, content replaced:** `Kit_Torchiere` and `Kit_TaskChair`.

**Kit_Torchiere changes the map can see:**
- height 1.852 → **1.689**;
- minCeiling 1.902 → **1.739**;
- footprint 0.362² → **0.39²**;
- **in module placements it now has a box collider**, so the player can no longer walk through it. Map signatures show it in `module props` of L0_WaitingRoom_4x3 (25 placements in the 20 signature maps, +1 collider and +1 LOD renderer each);
- **in furniture piles it has no collider, as before W2** (tag `pile_walkthrough`, read by `FrontRoomsFurniturePile`). Over map seeds 1–400 there are 42 pile torchieres, all with 0 colliders.

**Map piles change.** `Kit_TaskChair_Navy` joins the pile library and the chair and lamp bounds changed, so a map pile whose solver reaches those picks or fit tests is rebuilt with other pieces. In the 20 signature maps that is **5 of the 6 map piles, in 4 seeds** (4242, 20261005, 20261008 × 2, 20261010); §12.7 lists their pieces before and after. Every map still builds the same way twice. Relay nav and the playtests pass on the new piles.

**L0_WaitingRoom_4x3.** The stored x is 0.291, so the bowl is now **16 mm** from the wall face (was 30 mm); measured in the clone, `l0_wall_torchiere_gap`. There is no clash. Regenerating the module restores the 30 mm gap; that is 关卡设计's call and file.

**Kit_TaskChair:**
- 20 mm lower: minCeiling 1.003 → 0.983;
- its box is the true bounds, 0.617 × 0.933 × 0.654 (was 0.624 × 0.954 × 0.656);
- anchor and support names are unchanged.

**Office stations.** The hierarchy now names the chair `Kit_TaskChair`, `Kit_TaskChair_Navy` or `Kit_TaskChair_Armed`. No code looks the name up (grep of Assets): the only reference is `FrontRoomsOfficeKit.Chair`.

**Files of theirs this touches:** none. `FrontRoomsFurniturePile.cs` and `FrontRoomsOfficeKit.cs` are visual-owned.

## 11. Open issues

1. **Not landed.** The package `office_props_w2` is ready (§13): CLAIM in `LANDINGS.md`, dry run, `--apply`. The RenderSetup hunk (`office_props_w2_rendersetup.diff`) lands separately, after outlets_p4b.
2. **Metals in the game run at half reflection.** `FrontRoomsZoneReflection.MaxLinear` caps every zone's reflection at 0.5 linear until per-room probes land (G7). So every metal reads darker than in a catalogue photo: the chairs' chrome piston is L\* 11 at 0.3 m. The new brass is as bright, relative to its room, as the Sears pole is relative to its sweep, but not as bright in absolute terms. A vertical pole also cannot mirror a ceiling light from standing eye height (a vertical mirror keeps the ray's downward slope, so the reflection of a troffer at 2.9 m lands above 1.62 m, above the pole); its highlights come on the cup, the knob and the base beads. Raising the zone reflection is a global G7 decision, not W2's.
3. **The bowl from below is evenly toned but not milky.** The opaque bowl (spec §3.4) is lit from below only (L\* ≈ 23); real frosted glass would glow with the ceiling light passing through it. That needs a translucency term the Surface shader does not have; an emissive fake would glow in dead-lamp rooms. Left as it is.
4. **`Prop_Brass` for other kits.** 38 kits keep the shared `Prop_Brass`, which reads bronze in the same way. Whether to brighten it for all of them is Red's call.
5. **Gate margin.** The grey back pad facing away from the light is close to the cubicle panel (ΔE76 6.6). If Red wants more separation, the spec's `Prop_FabricGrey` fallback is ready: a new `.mat` plus a separate RenderSetup diff.
6. **Halogen vs the literal Sears N.** Halogen with frosted glass rests on general heat reasoning ([GEN] in the spec). The acrylic, 150 W, three-way N is the documented alternative.
7. **Later, and needing Red:**
   - a lit torchiere variant (`Prop_LampShadeLit` / `Prop_BulbLit` plus a real up-light; foot-bright needs an emission mask);
   - red and pink chairs, which need new fabric materials.
8. **Follow-ups for other owners:**
   - `22_era_lock.md` rows 40 and 76: the descriptions, plus rows for `_Armed` and `_Navy`;
   - K40 and K04 re-render, R03/R39 refresh, and new K-sheets for `_Navy` and `_Armed` (平面视觉 · 印刷与演示, from `threeview/index.json`; the torchiere records changed in fix 1);
   - VL308–VL311: specs in `W/vl_pending/w2_office_props.md` (updated for fix 1), to be placed by the 游戏视觉 main session.
9. **Tool quirks** (main's tools, not W2):
   - KitLookdev's `office_desk` pose sits inside a panel for seeds 1 and 13; seed 3 frames the gate;
   - its `asset_*` line-up frames are dark for every kit;
   - `FrontRoomsMainScenePlaytest` fails in a clone without the FMOD banks (the `BankLoadException` counts as an autopilot error).
10. **Stale remaps.** The post-import `.fbx.meta` files keep remap entries for slots the kits no longer use (`Prop_SteelBlack`, `Prop_Glass`, `Prop_Rubber`, and for the torchiere now also `Prop_Brass`, `Prop_PlasticWhite`). Harmless: FrontRoomsKitImporter only adds and removes remaps for current slots.
11. **Clone state.** `W/proj_w2` is main (07:59 sync) + the final W2 payload (fix 1) + the W2 harness + main's FMOD banks, imported. Its `FrontRoomsThreeView.cs` is the W/tools version again (the hand-off renderer was swapped in for run 66 only). Nothing was deleted: earlier copies are in `W/w2_props/payload_r0/`, `payload_prev/`, `fix1/s085/` (the frames at brass smoothness 0.85) and `W/_partial/`.
12. **Red's commit.** Red committed the working tree at 07:21 as `0ea38bb` ("1"), with the build stage's `images/20_*.jpg` and `threeview/` files (our own renders). Fix round 1 changed some of those files afterwards (§7, §8); they are uncommitted.

## 12. Fix round 1 (2026-10-22 07:40–08:30)

### 12.1 Brass (era-art 1): a torchiere-only polished brass

**The cause.** `Prop_Brass` is sRGB (0.69, 0.541, 0.29) at smoothness 0.6, about linear (0.43, 0.25, 0.07): half the reflectance of polished brass. On a metal there is no diffuse term, so in the game's dim rooms, with the zone reflection at half strength, the pole read dark bronze (verifier's numbers confirmed: pole at 0.3 m mean `#3A2C12`, L\* 19).

**The line-up** (`W2Variants`, runs 54–55, `W/w2_props/fix1/variants*/`; pole and cup regions fixed, same cameras as round 0):

All candidates are on the fix-1 geometry; "ref" puts the shared `Prop_Brass` back on it.

| Candidate (sRGB tint, smoothness, metallic) | pole at 0.3 m, L\* median | pole at eye height, L\* median | cup, max L\* | base top, L\* median |
|---|---|---|---|---|
| ref `Prop_Brass` (0.69/0.54/0.29, 0.6, 1) | 19 | 23 | 68 | 68 (flat, "gold paint") |
| A (0.94/0.84/0.56, 0.85, 1), the verifier's suggestion | 26 | 32 | 84 | 40 (mirrors the dark floor) |
| B (0.95/0.87/0.62, 0.85, 1) | 27 | 33 | 84 | 41 |
| C (0.95/0.85/0.58, 0.78, 1) | 27 | 33 | 84 | 66 |
| D (0.96/0.88/0.64, 0.90, 1) | 27 | 33 | 84 | 22 |
| M90 / M80 (B with metallic 0.9 / 0.8) | 28 / 29 | 36 / 39 | 84 | 48 / 54 |
| S92 (B at smoothness 0.92) | 27 | 33 | 84 | 19 |

**The choice: B's tint at C's smoothness**, `Prop_BrassPolished` = sRGB (0.95, 0.87, 0.62), smoothness 0.78, metallic 1, macro tone/dirt 0.02, cavity off.
- The tint is close to measured polished brass (B/R 0.38 in linear; A's 0.31 is more orange) and keeps the colour of the old slot's hue family.
- Smoothness 0.78 keeps the pole a mirror of its room, and it lifts the base: at 0.85 the treads mirror the dark floor (median L\* 41); at 0.78 they read gold (66) with a highlight on every bead, closer to the Sears base (57).
- Lowering metallic was rejected: M80 lifts the pole by 2–6 L\* but turns it to khaki paint, the "matte mustard" the verifier objected to.

**The result** (final frames, run 63, `W/w2_props/lookdev4/`; regions as the verifier's):

| | round 0 | fix 1 | Sears 1993 p.319 N |
|---|---|---|---|
| pole at 0.3 m: mean, top 5 %, L\* median | `#3A2C12`, `#4A3817`, 19 | `#4C3E18`, `#59481C`, 27 | `#B5916F`, `#FADFBA`, 63 |
| walls beside it, L\* median | 42 | 42 | sweep 92 |
| **pole ÷ surround, luminance** | 0.22 | **0.41** | **0.39** |
| pole at standing eye (2.4 m), L\* median; ÷ wall | 23; 0.33 | 33; 0.68 | |
| cup at 0.3 m: top 5 %, max L\* | `#8A6A32`, 51 | `#91855B`, 84 | max 96 |
| base top at 0.3 m, L\* median / p95 | 51 / 80 | 66 / 84 | 57 / 74 |
| studio three-view pole (hero), L\* | 44–50 | 67–68 | 63 median |
| at 8 m (`sw_Kit_Torchiere_d08_LOD0`) | copper-brown line | gold line | |

So relative to its surroundings the pole matches the catalogue; in absolute terms it stays darker, for the reasons in §11.2. The verifier's frames are re-shot (eye, 0.3 m, 8 m, three-view) and laid beside the Sears crops in `W/w2_props/fix1/cmp_sears_fix1.jpg` (outside the repo; the repo copy without the scans is `images/21_torch_fix1_before_after.jpg`).

**Shipping.** A new `.mat` with a new GUID (`3475294186a44ecfbb6f2bca61855237`), its SurfaceDef as the separate diff (§2). `Prop_Brass` is unchanged, so the 38 other kits keep their look (§11.4).

### 12.2 Bowl rings (era-art 2)

- **Geometry.** Each step out between bands is now 4 mm radial and sloped 15° up and out (it was a flat 11.5 mm ledge facing straight down). The bands slope 41°. The 26° kinks are inside the 50° smoothing angle, so the steps shade round and show in the outline, as on the Sears scan.
- **Material.** `Prop_FrostedGlass`: opaque, sRGB (0.92, 0.905, 0.87) = `#EBE7DE`, smoothness 0.68, no texture, so no scratch normal map. Of the three candidates (`#E4E0D6` at 0.68 and 0.55, `#EBE7DE` at 0.68, and smoothness 0.80 tried too) none differed by more than 1 L\* from below; the lighter one was kept. New GUID `47927cf621bc4cd6908122cf0ee40342`, SurfaceDef in the separate diff. The halogen tube uses it too.
- **Scan** (`bowlscan.py`: mean grey of a 40 px strip down the bowl, smoothed 5 px):

| Frame | round 0: band level, darkest | fix 1: band level, darkest |
|---|---|---|
| `c03_torch_cup_knob` | 53.9, 26.6 (**0.49×**), 5 dips | 50.0, 45.2 (**0.91×**), 0 dips below 0.9× |
| `c03_torch_bowl_under` | 51.7, 28.1 (**0.54×**), 5 dips | 52.0, 44.9 (0.86×: the foot next to the cup, not a ring) |

### 12.3 Dimmer knob (era-art 3)

A custom mesh replaces `kit.extrude(..., bevel=0.0)`: the 24-point knurl (12 ribs), a 1.8 mm 45° chamfer on the front edge and a low dome (about 1 mm) on the face, plus a back cap. At 0.3 m the rim and the dome now catch the troffers (a highlight on the dome, a dark knurled rim), where round 0 showed a pale flat disc (`#83652E` against the cup); see `images/21_torch_fix1_before_after.jpg`, row 1.

### 12.4 Base (era-art 4)

Five rounded beads (§3), 40 sides. Each bead rolls from a 70° riser over a 35° crest into a near-flat tread; only the groove at its foot stays a crisp crease. At 0.3 m every bead carries its own highlight, like the Sears base zoom, and there are no wide flat discs or black risers. The base's extra profile points cost 512 triangles; the dropped bowl grooves, the 32-sided bowl inside and the 40-sided base paid for them (LOD0 4,392 ≤ 4,500).

### 12.5 The report's contradiction

Round 0's §7 said the torchiere "reads as a polished-brass torchiere" while §11.2 said "warm bronze". The second was right. §7 now gives one verdict, with the measurements behind it.

### 12.6 Pile torchieres (eng 1)

- **Decision:** a pile torchiere does not block, as before W2. Its one box is the bowl's footprint round a 24 mm pole; in a pile, tilted in EdgeLean, that is an invisible wall. In the L0 waiting-room module it blocks (the reason for the box).
- **Code:** `torchiere.py` tags the kit `pile_walkthrough`. `FrontRoomsFurniturePile.cs` (visual-owned) adds `&& !p.piece.info.HasTag("pile_walkthrough")` to its `blocks` rule. No other kit has the tag, so no other pile piece changes. Diff in `W/apply/office_props_w2.diff`.
- **Spec §3.9** is corrected (the false sentence is struck through, with the errata at the top of `10_spec.md`).
- **Evidence:**
  - `W2PileSeeds` over map seeds 1–400 (run 60, `W/w2_props/fix1/w2_pile_seeds.tsv`): 264 map piles, 42 of them with a torchiere (38 seeds, Upright and EdgeLean); **every pile torchiere has 0 enabled colliders**.
  - Relay nav on seed 293 (a pile torchiere in the spawn chunk) and seed 12 (one chunk away): both PASS 60/60 hunts, 0 frames in architecture, 0 frames in furniture outside a pass-through (seed 12 has 1 pass-through, which the test allows; 293 has 0) (run 70; `fix1/play/relay-nav-test_{293,12}.json`).
  - Playtest on seed 293: PASS (run 85).
  - The probe's pile hashes are unchanged by the fix (placements only), and the map signatures differ from round 0's only in the 25 module-torchiere renderer rows (new mesh); no collider row changes, because none of the 20 signature maps has a pile torchiere.

### 12.7 Map signatures, object by object (eng 2)

`W2SigDetail` dumps every chunk of the 20 signature seeds (renderer, light and collider lines as CxSuites writes them, plus one line per kit instance with path, asset, position, rotation and enabled colliders). It ran on the clone without W2 (run 51, main 07:59) and with W2 + fix 1 (run 53). `sigdiff.py` compares them (`W/w2_props/fix1/sigdiff/`):

- **500 chunks, 76 changed, in 18 seeds**: the same 76 chunks as CxSuites' TSVs (`cx_sig_before` → `cx_sig_fix1`: 152 rows, 0 lights rows).
- **Changed lines: chair 1,476, torchiere 200, pile 480, other 0** (`other_lines.txt` is empty).

| Class | Chunks | What changed |
|---|---|---|
| chairs only | 46 | Office station chairs: same positions and yaws; LOD2 added (+1 renderer per chair; 164 in all); box sizes changed; 31 became `_Armed` and 28 `_Navy` (in the 20 maps: 164 chairs → 105 grey, 31 armed, 28 navy) |
| module torchiere | 25 | `module props/Kit_Torchiere` in L0_WaitingRoom_4x3: new mesh, +1 renderer (LOD2) and +1 enabled box collider each |
| map pile rebuilt | 5 (4 seeds) | other pieces (below); no pile in these maps holds a torchiere, before or after |

The five rebuilt piles (renderer / collider deltas as in the verifier's table):

| Seed | Chunk | Pile | Pieces before → after | Lost | New |
|---|---|---|---|---|---|
| 4242 | 2_0 | CentreSculpture | 24 → 22 (R −3, C −1) | Bookcase, CRTMonitor, Keyboard ×2, LadderChair ×3, Pouf, RollingCabinet, TaskChair ×2 | BarStool, Binders, CRTTV, FilingCabinet, PaperStack, QueenAnneArmchair, TaskChair_Navy ×2, Urn |
| 20261005 | 2_−2 | CopyPasteRow | 5 → 14 (R +18, C +4) | QueenAnneArmchair ×5 | SideTableTurned ×14 |
| 20261008 | −1_−1 | CentreSculpture | 16 → 14 (R −3, C +2) | CRTMonitor, DeskPhone ×2, Keyboard, PaperStack ×2, TableLampPleated, Urn, WaterCooler | Binders, Nightstand2, PCDesktop ×2, SideTableTurned, StepStool ×2 |
| 20261008 | −2_0 | CentreSculpture | 26 → 29 (R +13, C −1) | Binders ×2, CRTMonitor, ClubChair_Oatmeal, Crate, Credenza, Dresser70s, Hutch_Cherry, OfficeDesk ×2, PCDesktop, Pouf, QueenAnneArmchair, Sofa3_Charcoal, Urn | BarStool, CRTTV ×2, DeskPedestal, DeskPhone ×2, PaperStack, StepStool ×2, TableLampPleated, TaskChair ×5, TaskChair_Navy, TrashBin, VendingMachine |
| 20261010 | −2_2 | CentreSculpture | 12 → 12 (R +3, C −2) | Crate, PaperStack, QueenAnneArmchair, SideTableTurned, TaskChair, TrashBin, Urn | Credenza, DeskPhone ×2, LadderChair, PCDesktop, Pouf, TaskChair_Navy |

Why: the pile library is sorted by name and now holds `Kit_TaskChair_Navy`, and the chair and lamp bounds are new, so the solver's picks after that index and its fit tests shift (§6, item 4). This goes into the 关卡设计 notes (§10). Determinism is unaffected: CxSuites signatures with W2 + fix 1 (run 62) give 0 second-world and 0 rebuild failures.

### 12.8 Play-mode gate suites (eng 3)

| Suite | Result | Run |
|---|---|---|
| FrontRoomsDressMapStepTest (7 maps: stepped 3 ms / 0 ms / Complete == synchronous, mid-job rebuild) | **PASS**, 0 failed | 53 (`fix1/probe/w2_mapstep.txt`) |
| FrontRoomsDoorShotTests | **PASS**, 191 passed, 0 failed | 53 (`fix1/probe/door-shot-tests.json`) |
| MainScenePlaytest `-autopilotSeed 2554` | **PASS** · ended by caught (38.5 s played, 42 cells, 0 errors) | 81 |
| MainScenePlaytest `-autopilotSeed 20388` | **PASS** · ended by time (75 s, 85 cells, 4 doors, 0 errors) | 82 |
| MainScenePlaytest `-autopilotSeed 2554 -autopilotGlass` | **PASS** · ended by caught (glass scenario 7 ok, 0 FAIL) | 83 |
| MainScenePlaytest `-autopilotSeed 2554 -autopilotDoors` | **PASS** · ended by time (33 door checks, 0 FAIL, 3 unlock shots) | 84 |
| MainScenePlaytest `-autopilotSeed 293` (pile torchiere in the spawn chunk) | **PASS** · ended by time (75 s, 68 cells, 0 errors) | 85 |

- Reports: `W/w2_props/fix1/play/<run>/report.json`, verdicts in `fix1/play/verdicts.txt`.
- The playtests ran through `W2Playtest`, which calls main's `FrontRoomsMainScenePlaytest.Run()` unchanged and only moves its wall-clock guard from 240 s to 1,800 s (the verifier's run hit the 240 s guard at load 211–307). At this session's load (62–216) every run finished in 60–105 s of wall time, inside main's own 240 s, so the longer guard was never used.
- The first two tries (runs 71, 72) FAILed only on the missing FMOD banks (`errors 1`: `BankLoadException … Master.strings.bank`); after copying main's banks into the clone, all passed.

### 12.9 Apply package (eng 4)

See §13.

### 12.10 Fix-1 runs (`W/w2_props/logs/run_fix1.status`)

| Run | What | Time | 1-min load | Result |
|---|---|---|---|---|
| 50_resync_before | rsync of main 07:59 (no W2), patches, tools, W2 harness | — | — | ok |
| 51_compile_sig_before | compile + `W2SigDetail` without W2 | 121 s | 522 → 679, peak 710 **UNDER LOAD** | 0 `error CS`, 500 chunk dumps |
| 52_blender_torchiere | `build_asset.py torchiere` into the clone | 2 s | n/a | 4,392 / 1,536 / 385 |
| 53_fix1_chain | W2 payload + fix 1: reimport, probe, `W2SigDetail`, DressMapStepTest, DoorShotTests, LOD dump | 86 s | 665 → 632 **UNDER LOAD** | 0 `error CS`; PASS / 191-0 |
| 54 / 55_variants | material line-ups (§12.1, §12.2) | 25 / 25 s | 638 / 523 **UNDER LOAD** | 28 + 20 frames |
| 56–59 | torchiere frames, KitLookdev, three-views at brass smoothness 0.85 (kept in `fix1/s085/`) | 15–20 s each | 404 → 277 **UNDER LOAD** | superseded |
| 60_pileseeds | `W2PileSeeds` seeds 1–400 | 125 s | 84 → 96, peak 111 **UNDER LOAD** | 42 pile torchieres, 0 colliders |
| 61_cxsuites / 62_cxsig | map suites; signatures | 61 / 25 s | 96 → 107 **UNDER LOAD** | PASS; 0 + 0 |
| 63–66 | final torchiere frames (smoothness 0.78), KitLookdev pile, three-views 2× and 1× | 10–15 s each | 87 → 80 **UNDER LOAD** | 19 + 4 frames, 4 + 4 views |
| 70_relaynav_293 / _12 | Relay nav on pile-torchiere seeds | 15 / 15 s | 80 → 122 **UNDER LOAD** | PASS / PASS |
| 71 (72) | playtest 2554 (20388) without FMOD banks | 100 s | 122 → 154 **UNDER LOAD** | FAIL: BankLoadException only |
| 81–85 | playtests with the banks (§12.8) | 60–105 s | 62 → 216 **UNDER LOAD** | 5 × PASS |

## 13. Apply package `office_props_w2` (v2)

In `W/apply/` (convention v2, `W/apply/README.md`), built by `W/w2_props/fix1/build_package.py` at 08:18 on main `0ea38bb` + working tree:

| File | What |
|---|---|
| `office_props_w2.sh` | dry run by default, `--apply` writes; marker `apply-convention: v2`; honours `APPLY_REAL`, `APPLY_DIR`, `APPLY_BACKUP`; adds a check that the 6 new GUIDs are unused in the target |
| `office_props_w2_bases.txt` | 23 paths: 10 sha1 bases, 13 **ABSENT** (the new paths) |
| `office_props_w2_keep.txt` | 6 kept, never written: `Kit_Torchiere.json.meta`, `Kit_TaskChair.json.meta`, the two Office `.cs.meta`, `Resources/Surfaces.meta`, `Resources/Props/Models.meta` |
| `office_props_w2_guids.txt` | the 6 new GUIDs |
| `office_props_w2_expected.txt` | the payload in write order: material metas and materials, then per kit sidecar meta, sidecar, FBX meta, FBX; then the two scripts and the three Blender modules |
| `office_props_w2_payload/`, `office_props_w2_base/` | the 23 files; exact copies of the 10 bases |
| `office_props_w2.diff` | unified diff base → payload for the text files |
| `office_props_w2_rendersetup.diff` | the separate SurfaceDef hunk (§2); **not applied by the script** |

**Checks (08:18):**
- Dry run on main: **DRY RUN OK** (23 bases match, 6 kept metas unchanged, 6 GUIDs unused, 23 payload files match).
- `W/tools/rebase_apply.sh office_props_w2 --no-install`: **PASS**, nothing to rebase.
- Scratch target (`W/w2_props/fix1/scratch_target`: main's `.meta` files + the 10 bases): `--apply` → **MANIFEST OK**; second dry run → **ALREADY APPLIED**; a changed base afterwards → **REFUSED** (partial apply, GUID in use), nothing written. The scratch run's manifest was moved out of `W/apply/` (`fix1/scratch_applied_manifest.txt`) so it cannot be mistaken for a landing.

**To land:** CLAIM in `LANDINGS.md`; `bash W/apply/office_props_w2.sh` (expect DRY RUN OK; on a base mismatch run `rebase_apply.sh` first); `--apply`; then Unity reimports the four kits and the two materials. The RenderSetup hunk follows after outlets_p4b.
