# W2 · Office prop period fixes: build report

Status: **BLOCKED by verify round 3 (2026-10-22 ~14:00): era-art NOT READY, engineering READY. Do not land `office_props_w2`; fix round 3 is needed (§0b, §15).** Built, fix rounds 1 and 2 done in the clone, not landed. Build stage of workflow `office-props-w2`, 2026-10-22 06:52–07:22; fix round 1 07:40–08:30; fix round 2 13:15–13:50 (account B, after the account switch; nothing of fix 2 had been started on account A). Owner: 游戏视觉. Spec: `10_spec.md` (errata for fix 1 and fix 2 at the top). Red's decision (2026-10-21): "the torchiere to brass with a glass/acrylic shade; the task chair to armless grey/coloured fabric (keep the black armed one as a variant)".

**What was built:**
- `Kit_Torchiere` is now the brass halogen torchiere after Sears 1993 p.319 N: 1.689 m, a stepped frosted glass bowl (6 bands, 5 ledges of 8 mm) whose glass lip shows above a thin brass band, a knurled dimmer on the right side of the cup, a beaded brass base, lamp off. It has two slots of its own, `Prop_BrassPolished` (fix 2: warmer, sRGB 0.95/0.72/0.32) and `Prop_FrostedGlass`; the outside of the bowl takes the room light through tilted normals (fix 2).
- `Kit_TaskChair` is now the armless 1990 task chair in grey fabric, with about 2 cm of chrome piston exposed (fix 2; it reads dark at 0.3 m, a limit: §16.4).
- New: `Kit_TaskChair_Navy` and `Kit_TaskChair_Armed` (the old black armed chair). All three chairs have a hand-built LOD2 (fix 2).
- The Office gives 30 % of rooms navy chairs and about 15 % of stations the armed chair. Positions are unchanged.
- In furniture piles the torchiere stays walk-through, as before W2; in the L0 waiting-room module it now blocks (keep-out note in §10).

**Verification (fix round 2; clone = main `0ea38bb` + working tree of 07:59 + the W2 payload with fixes 1 and 2; payload bases re-checked on main `2c450e11` at 13:43):**
- 0 `error CS` in every Unity run (runs 100–126). Roslyn `--all` on main `2c450e11` + working tree (13:33) + the payload: COMPILE OK for macOS and iOS, with and without the RenderSetup hunk.
- Era-art targets, measured in CIELAB after the post stack on masks rendered by the harness (§14.1–14.4): bowl underside lighter than the walls at 0.3 m, 0.8 m and standing eye; notches 3–4 px at 2.4 m; pole hue h 75; ~~highlights C 22–29 on base, cup, knob and pole~~ highlights on the base (C 22) only, the cup and pole have none (§16.2); knob in profile. Two points miss by a hair (the thin rim band's highlights, the 0.8 m wall lamp's lower bowl) and are listed in §11.3. **Verify round 3 refuted two claims here:** the cup's and pole's "highlights" are body colour (brightest-10 % L\* only 3–7 above the median), and the bowl rim and lip are faceted at 0.3 m, so the hero LOD0 does not hold up there yet (§15).
- Map suites all PASS (run 120). Signatures: 0 + 0 determinism failures (run 121); the same 76 chunks change as in fix 1, every changed object explained, 0 other lines (§14.6).
- The 5 rebuilt map piles (4 seeds) tested where they are: `W2PileNav` PASS on all 5 (0 doorways or doors touched, 0 cells cut off, 60/60 hunts each); main's Relay nav test PASS 60/60 on all 4 seeds; MainScenePlaytest on 20261008 PASS twice, but its walk does not reach the rebuilt chunks (§14.7).
- Play mode: DressMapStepTest PASS; DoorShotTests 191 / 0 (run 111). Dressing probe byte-identical to fix 1.
- Apply package `W/apply/office_props_w2` (v2) rebuilt at 13:43: DRY RUN OK on main `2c450e11`, `rebase_apply.sh` PASS (§13).
- Runs 101, 107–116, 120, 123 (one), 124–126 were UNDER LOAD (1-minute load up to 309); 99–100, 103–105, 121, 122 and three of the four 123 runs ran at 1-minute load 17–31 (121 started at 31.0; `123_relaynav_20261010` peaked at 38.6 and is UNDER LOAD).

**Verification (fix round 1, main `0ea38bb` + working tree at 07:59):** kept for the record.
- 0 `error CS` in every Unity run. Roslyn `--all` on main + payload: COMPILE OK for macOS and iOS, with and without the separate RenderSetup hunk (08:09).
- Map suites all PASS. Relay nav also PASS on seeds 293 and 12, which have a pile torchiere next to the spawn.
- Play mode: DressMapStepTest PASS; DoorShotTests 191 / 0; MainScenePlaytest PASS on 2554, 20388, 2554 + glass, 2554 + doors and 293 (pile torchiere in the spawn chunk).
- Map signatures: 0 determinism failures. 76 of 500 chunks change, and every changed object is explained (§12.7).

## 0b. Verify round 3 (after fix round 2): BLOCKED

The package stage (14:10–14:20, session local_a9a1dce2) did **not** package: the era-art lens is NOT READY. The engineering lens is READY. Both verdicts are copied word for word into `W/w2_props/fix3/verify_era-art_3.md` and `verify_engineering_3.md`, and the verifier's evidence crops are in `W/w2_props/fix3/evidence_v3/`. The package stage edited nothing in the clone or the payload and ran no Unity or Blender.

| # | Era-art blocker | What fix round 3 must do | Pass when |
|---|---|---|---|
| 1 | **Bowl rim and lip faceted at 0.3 m** (outer rim, brass band at 48 sides; inner lip edge at 32); a dark crescent seam under the lip from above | lip, brass band and top band to 96 sides, inner rim to 64; remove the crescent (inner-wall normals tilted up, or a rounded lip edge); needs Red's budget call (§15.2) | no corner on the rim at 1:1 in the 0.3 m frames from above and below |
| 2 | **Cup and pole read dull bronze, no specular highlight** (0.3 m; the cup also from standing eye). The "highlights C 27/29" claim measured body colour | try higher smoothness on the cup and knob (0.85–0.9), else list it as a VL308 limit (G7 reflection cap) and strike the claim | cup brightest-10 % L\* minus median ≥ 20 at 0.3 m (now 7.4), or the limit is written for Red |
| 3 | VL308 composites carry a baked PASS chip and "it holds up at 0.3 m" | re-frame with `--status WAIT-RED` (or FLAG) and fix the sentence | — |
| 4 | VL309 says "2 cm of chrome shows"; the piston reads black (L\* 10) | reword as a limit, or make the band read as chrome | — |
| 5 (low) | bowl reads cool white, not "ivory, as on the scan" | fix the §14.1 wording, or warm `Prop_FrostedGlass` if Red wants ivory | — |
| 6 (low) | small pop at the torchiere's 20 m LOD1 → LOD2 switch | note it, or two more sides on LOD2's outer bowl ring | — |
| 7 (low) | facets at the bottom corners of the chair's oval back shell at 0.3 m | more corner steps on the back shell and seat front (566 tris of headroom under 5,200) | — |

Engineering (READY) left five process notes; 1, 2 and 5 are closed by this stage (LANDINGS lines, dead links, load note), 3 and 4 go into fix round 3 (§15.4). Full detail, triangle costing and the decision for Red: §15.

## 0a. Fix round 2 at a glance

| Verifier finding (round 2) | Fix | Evidence |
|---|---|---|
| era-art 1: the bowl is a dark olive cone, not glass | custom normals on the bowl's outside tilted 45° above the outward horizontal (5° lower on the ledges), set on every LOD; a 4.5 mm glass lip above the band. The verifier's exact recipe (copy the inside face's normal) was built and measured: it lights the underside like an upward white surface (L\* 83 against walls at 42) | underside L\* 66 vs 22 at 0.3 m, 52 vs 36 at 0.8 m, 60 vs 42 at standing eye (fix 1: 22 vs 39, 19 vs 47); §14.1 |
| era-art 2: terraces gone | 6 bands (45°) and 5 ledges of 8 mm radial | ledge jumps 3–4 px at 2.4 m on both sides; darkest band 0.90× (eye), 0.95× (0.3 m below), 0.84× (0.8 m wall lamp, a smooth gradient, no ring); §14.2 |
| era-art 3: brass khaki, base pewter, blotches | `Prop_BrassPolished` sRGB 0.95/0.72/0.32, smoothness 0.72, macro tone/dirt 0, from a 12-candidate line-up measured in hue and chroma | pole h 88 → 75 (target 70–82); highlights C ≥ 18 on base (22; fix 1: 9), base from above (18; 10), ~~cup and knob (27), pole (29)~~ (withdrawn: body colour, §15.1, §16.2); only the thin rim band's stay bleached (C 12); §14.3 |
| era-art 4: knob reads as a hole | knob on the cup's right side, on a shank, 11 mm knurled cylinder with shallow ribs | in profile in the front and 0.3 m frames; `22_torch_measure.jpg`; §14.4 |
| era-art 5 / eng 1: chair LOD2 falls apart at 12 m | hand-built LOD2 proxies (whole star, hub, caster boxes, column, mechanism, pads, shells, bar; arms on `_Armed`), LOD1 still decimated; GUIDs kept | Armed LOD2 428 tris in 23 parts (was 447 in 55 shards); armless 420 (was 416 in 30); `23_lod_switch_zoom.jpg`; §14.5 |
| era-art 6: a VL309 frame shows the round-0 lamp | every W2Lookdev, KitLookdev and three-view frame re-rendered with fix 2 | `20_chair_hero_close.jpg` and all composites rebuilt in the Shot format |
| era-art 7 / eng 3: forced-LOD torchiere frames show only the pole | LOD triple camera backs off to fit the kit (3.9 m for the torchiere) | `20_lod_forced.jpg` row 1 |
| era-art 8: chrome piston looks rusty | 2 cm of piston shows (was 6 cm); fix 3: it reads dark, not chrome (§16.4) | `c03_chair_lever_piston` in `20_chair_hero_close.jpg` |
| era-art 9: dimmer under a 300 W tube (low priority) | kept on the cup, where the documented reference (Sears N) has it | question for Red, §11.3 |
| eng 2: "Relay nav and the playtests pass on the new piles" unsupported | `W2PileNav` tests each rebuilt pile in its own chunk; main's Relay nav on the 4 seeds; a playtest on 20261008 with a chunk tracker; §10 now says what was tested | §14.7 |
| (task) Kit_Torchiere is a module prop | keep-out note for 关卡设计 | §10 |

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
- **Fix round 2** did not re-sync it (§11.11): it put the fix-2 modules, the fix-2 brass and the fix-2 harness into the 07:59 state, so its frames compare with fix 1's and the verifier's; the payload's bases and the compile were checked on main `2c450e11` instead.
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
| 99–126 | **fix round 2**: see §14.9 | | 17–309 (most **UNDER LOAD**) | |

## 2. Files (the payload, not landed)

The payload is staged in `W/w2_props/payload/` with paths relative to `Frontrooms3D/`, and packaged as `W/apply/office_props_w2*` (§13). The full lists are `W/w2_props/payload_sha1.txt` (fix 2) and `payload_base_sha1.txt`; the latter records `ABSENT` for every new path. Earlier payloads are kept: `W/w2_props/payload_r0/` (round 0), `payload_fix1/` and `payload_sha1_fix1.txt` (fix 1).

| Path | sha1 (new) | Base in main (sha1) | Note |
|---|---|---|---|
| `Assets/Resources/Props/Models/Kit_Torchiere.fbx` | `ff817166` | `ea8a7e66` | replaced (fix 2: bowl, glass normals, knob); GUID `a2e24975…` kept |
| `…/Kit_Torchiere.fbx.meta` | `b108376f` | `ba7eef4e` | Unity's post-import meta: same GUID; remaps to the two new materials, a third LOD % |
| `…/Kit_Torchiere.json` | `79a0d0c8` | `35c348b3` | sidecar (fix 1: slots, tag `pile_walkthrough`; fix 2: `dimmer` anchor, triangles) |
| `…/Kit_Torchiere.json.meta` | `6aa1caad` | `6aa1caad` | unchanged (kept, GUID `a954ac71…`) |
| `…/Kit_TaskChair.fbx` | `456c533b` | `7a5e244d` | replaced (fix 2: short piston, LOD2 proxy); GUID `99ce0ad6…` kept |
| `…/Kit_TaskChair.fbx.meta` | `f4167824` | `e876a57b` | post-import: same GUID; adds the `Prop_FabricCubicle` remap and a third LOD % |
| `…/Kit_TaskChair.json` | `9f44ec44` | `a6950823` | sidecar (fix 2: `trianglesLod2` 420) |
| `…/Kit_TaskChair.json.meta` | `eed7c0e7` | `eed7c0e7` | unchanged (kept, GUID `7a8141bd…`) |
| `…/Kit_TaskChair_Navy.fbx` + `.meta` | `15a177cb` / `ad5ed974` | ABSENT | GUID `216f0b38920c4f789f294729a9e867e3` |
| `…/Kit_TaskChair_Navy.json` + `.meta` | `23dc3d95` / `0f60d02b` | ABSENT | GUID `d105b27e4fb44bcd9f275d700cb92304` |
| `…/Kit_TaskChair_Armed.fbx` + `.meta` | `0880b632` / `6f89f8aa` | ABSENT | GUID `3cd9526245414452b925066da4411b32` (fix 2: LOD2 proxy) |
| `…/Kit_TaskChair_Armed.json` + `.meta` | `ebbe2ec5` / `e618e6d1` | ABSENT | GUID `13007464972f425c94c69ea5485fecb6` (fix 2: `trianglesLod2` 428) |
| `Assets/Resources/Surfaces/Prop_BrassPolished.mat` + `.meta` | `4188b068` / `6805e3fb` | ABSENT | fix 1; **fix 2** tint 0.95/0.72/0.32, smoothness 0.72, macro 0; GUID `3475294186a44ecfbb6f2bca61855237` |
| `Assets/Resources/Surfaces/Prop_FrostedGlass.mat` + `.meta` | `ce77be63` / `c5be76fb` | ABSENT | fix 1 (unchanged in fix 2); GUID `47927cf621bc4cd6908122cf0ee40342` |
| `Assets/Scripts/Office/FrontRoomsFurniturePile.cs` | `b6a48041` | `901503fc` | **fix 1**: `pile_walkthrough` (§12.6); visual-owned |
| `Assets/Scripts/Office/FrontRoomsOfficeKit.cs` | `d566b5a6` | `cfe0105a` | chair choice (§6) |
| `Tools/Blender/frontrooms_kit/assets/torchiere.py` | `289eb564` | `0d1a3af9` | rewritten (§3; fixes 1 and 2) |
| `Tools/Blender/frontrooms_kit/assets/task_chair.py` | `47819f54` | `1d3ddc99` | rewritten (§4; fix 2: piston, LOD2 proxy) |
| `Tools/Blender/frontrooms_kit/assets/task_chair_armed.py` | `f898b8a4` | ABSENT | today's chair, renamed (§4); fix 2: LOD2 proxy |

**Not in the payload:** the two SurfaceDefs for `FrontRoomsRenderSetup.cs`, as a separate diff: `W/apply/office_props_w2_rendersetup.diff` (fix 2: sha1 `d7007126`, made against main's `256cf3a4`, which is still main's at 13:33; source `W/w2_props/fix2/rendersetup/`; the brass line now carries the fix-2 values). outlets_p4b is changing RenderSetup tonight, so this hunk lands after it. Without the hunk the two .mat files work as shipped: the kit importer remaps slots by file name. The hunk only makes "Set up" regenerate them with the same values (every field matches the .mat).

**About the new GUIDs:**
- They were made with `uuid4` or by hand and grepped against every `.meta` in main's `Assets` and `Packages`: 0 hits. The package's dry run repeats that check (§13).
- `kitlib.py` and `build_asset.py` are unchanged. The two new slots are registered from `torchiere.py` with `kitlib.register_slot` (Blender preview colours only), so `kitlib.py` (also touched by `inspect_data`) stays out of the payload.

**Compile gate.** `roslyn_check.py --tree <main> --override FrontRoomsOfficeKit.cs,FrontRoomsFurniturePile.cs[,FrontRoomsRenderSetup.cs with the hunk] --all`: COMPILE OK, 0 errors, for macOS and with `--ios`, both with and without the RenderSetup hunk; fix 1 on main at 08:09 (`W/w2_props/fix1/roslyn/`), fix 2 on main `2c450e11` + working tree at 13:33 (`W/w2_props/fix2/roslyn/{osx,ios,osx_rs,ios_rs}`). The two `.cs` files did not change in fix 2.

**Clone-only harness** (ASCII, no tuples, never merged): `W/w2_props/tools/`, installed in the clone as `Assets/Editor/W2/`.
- `W2KitReimport`, `W2DressProbe`, `W2Chain`, `W2Lookdev`, `vl_images.py`: as in the build stage.
- Fix round 2 added: `W2Fix2Variants` + `W2Mask.shader` (line-ups and measuring frames, each with a mask pass: slot and object height per pixel, no post, no MSAA; `-w2Kits` shoots clone-only variant kits), `W2PileNav` (a rebuilt pile tested in its own chunk), `W2PlayTrack` (the chunks a playtest's camera enters); `W2Lookdev` now backs the forced-LOD camera off to fit the kit, `W2Playtest` switches the tracker on; `vl_images_fix2.py` (Shot format); and `W/w2_props/fix2/measure2.py`, `bowl_profile.py`, `measure_json.py`, `bl_lodcheck.py`, `make_rs_diff2.py`, `build_package2.py`. The variant kits (`Kit_TorchiereS45/S55/S65/E08/E35/E45/E55`, wrappers in `fix2/variants_src/`) were parked in `W/_partial/w2_fix2_parked/`; the clone holds none of them.
- Fix round 1 added: `W2SigDetail` (object-level dump of every chunk of the 20 signature seeds), `W2Fix1Chain` (reimport, probe, W2SigDetail, DressMapStepTest, DoorShotTests, LOD dump), `W2Variants` (material line-up on the torchiere), `W2PileSeeds` (map piles with a torchiere over a seed range; Relay nav on a chosen seed), `W2Playtest` (main's playtest with a longer wall-clock guard), `vl_images_fix1.py`; and `W/w2_props/fix1/sigdiff.py`, `measure.py`, `bowlscan.py`, `build_package.py`, `make_rs_diff.py`.

## 3. Kit_Torchiere (brass halogen torchiere)

Built as specified in spec §3, with the fix-1 and fix-2 corrections (errata at the top of `10_spec.md`):
- 1.689 m to the **frosted glass lip**; a brass band 4.5 mm tall, 2 mm proud of the glass, sits just under it, so 4.5 mm of white glass edge shows above the band (fix 2);
- a frosted bowl Ø0.39 × 0.11 with **6 bands sloped 45° and 5 ledges of 8 mm radial** sloped 12° up and out, from a Ø0.12 foot at 1.579 m to Ø0.386 at the band (fix 2), on a polished brass cup Ø0.10 with a rolled lip and a neck collar;
- a plain brass pole Ø0.024;
- a Ø0.245 base: a 20 mm vertical rim with a rounded top edge and a 14 mm flat ring, then **5 rounded beads** (each a 70° riser rolling over a 35° crest into a near-flat tread, with a crisp groove at its foot) up to a Ø0.045 boss with a rounded shoulder;
- a black felt disc and a cord to a two-pin plug;
- an R7s tube on a brass strap and holders;
- a **knurled brass dimmer knob on the right side of the cup** as seen from the front (Blender +X, Unity −X), where Sears N has its switch, at 1.555 m: a Ø8.4 mm shank (4 mm showing), an 11 mm knurled cylinder Ø21 mm with 12 shallow ribs (0.4 mm), a 1.5 mm 45° chamfer and a 0.9 mm dome (fix 2; fix 1 had it facing front).

**The glass outside (fix 2).** The bowl's outside, bottom ring and the strip behind the band carry custom split normals tilted 45° above the outward horizontal at each corner's azimuth, 40° on the downward-facing ledges (`glass_normals()` in `torchiere.py`, run after kitlib has made the LODs, on LOD0, LOD1 and LOD2). The surface shader is unchanged; the underside simply takes the ceiling light the way light coming through frosted glass would, and every ledge reads as a faint ring. Nothing is emissive, so a dead room keeps it dark. The inside face and the lip top keep their own normals. §14.1 has the line-up that chose 45°.

**Segments:**

| Part | Sides |
|---|---|
| bowl outside, bottom ring, brass band | 48 (0.41 mm sagitta at r 0.193: ≤ 1 px at 0.3 m); fix 1: 64 |
| bowl inside (seen only from above) | 32 |
| base, felt | 40 |
| cup | 24 (0.43 mm sagitta at r 0.05); fix 1: 32 |
| pole | 20 |
| knob | 24 points |

The inside is the floor, then a straight wall 4 mm inside the line through every step's inner corner (those corners are collinear), then straight up to the lip, tucked 0.8 mm under the lip top: it clears the outside by ≥ 2.8 mm everywhere (4 mm at the foot and at the lip). Smoothing angle 50°: the bead crests, rounded rim and boss edges, knob chamfer and cup shade round; the band corners, the bead grooves, the collar and the bowl's step corners stay sharp (the bowl's shading comes from its custom normals anyway).

**Slots:** `Prop_BrassPolished` (fix 2: sRGB 0.95/0.72/0.32, smoothness 0.72, metallic 1, macro tone and dirt 0), `Prop_FrostedGlass` (unchanged since fix 1) and `Prop_PlasticBlack`. `Prop_Brass` itself is unchanged for its other kits.

**Lamp off.** Nothing is emissive.

**Sidecar (Unity space):**
- bounds 0.390 × 1.689 × 0.390;
- one box collider, 0.39 × 1.689 × 0.39, centred at y 0.845. It replaces `noCollider` for module placements; in piles it is not spawned (§12.6);
- footprint 0.39 × 0.39; minCeiling **1.739**; placement Floor;
- anchors: `light` (0, 1.62, 0) and `dimmer` (−0.062, 1.555, 0) (fix 2; was (0, 1.555, 0.062)). No script reads `dimmer` (grep of main's Assets);
- `lodDistances` [8, 20, 54];
- tags `pile`, `lamp`, `domestic`, **`pile_walkthrough`**; pile block unchanged (Tall, mass 0, domestic70s, topper, Upright/EdgeLean, tolerance 0.3).

**One deviation from the spec:** `LODS = (0.35, 0.088)` instead of 0.09, so that LOD2 lands under 400.

## 4. Kit_TaskChair (armless, grey), Kit_TaskChair_Navy, Kit_TaskChair_Armed

Unchanged in fix round 1. **Fix round 2** shortened the armless chair's exposed piston to about 2 cm (housing lip at 0.2925 m, socket at 0.312 m; was 6 cm) and gave all three chairs a hand-built LOD2 (§14.5); LOD0 bounds, slots, collider, anchors, supports and lodDistances are unchanged.

**`task_chair.py`** follows spec §4. It is armless and after Sears 1993 M/N:
- a plain contoured seat pad, no welt: 0.48 × 0.46, top at 0.45 m;
- a shield/oval back 0.44 × 0.42, top at 0.93 m;
- a visible black back bar, a back-angle knob at the bar root, and a back-height knob with four chrome screws on the rear;
- a long height lever to the user's right (−X): a chrome rod with a black paddle;
- a smooth black housing Ø0.07 with a chrome piston Ø0.05 showing for about 0.02 m (fix 2; was 0.06 m);
- today's 5-star base with twin casters.

There is no tilt lever and no front tension knob.

**Hero detail:** 8 corner steps on the pads; the hub, housing, piston and socket are 24-sided; the caster wheels are 16-sided.

**Slots:** `Prop_FabricCubicle`, `Prop_PlasticBlack`, `Prop_Chrome`.

**The navy chair** is made by `VARIANTS = {"Kit_TaskChair_Navy": {"Prop_FabricCubicle": "Prop_FabricNavy"}}`. Its sidecar is the same, pile block included.

**Docstrings.** The brand names are gone from both modules. A grep for them finds 0 hits.

**`task_chair_armed.py`** is today's module with these changes only: `NAME = "Kit_TaskChair_Armed"`; `LODS = (0.40, 0.10)`; `lodDistances` [6, 12, 30]; no `kit.pile`; the docstring; and (fix 2) a hand-built LOD2 proxy, with LOD1 still collapse-decimated to 0.40.

**Sidecars:**

| Kit | Bounds (w × h × d) | Collider | Footprint | minCeiling | Anchors | Pile |
|---|---|---|---|---|---|---|
| Kit_TaskChair / _Navy | 0.617 × 0.933 × 0.654 | 1 box = the bounds (centre −0.003, 0.467, −0.030) | 0.617 × 0.654 | 0.983 | `sit` (0, 0.45, 0.03), `back_top` (0, 0.93, −0.328) | Seat, mass 1, office90s, U/I/S/B, 0.45 |
| Kit_TaskChair_Armed | 0.617 × 0.953 × 0.653 | today's hand box 0.624 × 0.954 × 0.656 | 0.624 × 0.656 | 1.003 | as today (`back_top` y 0.95) | none |
| (old Kit_TaskChair) | 0.617 × 0.953 × 0.653 | 0.624 × 0.954 × 0.656 | 0.624 × 0.656 | 1.003 | | Seat |

Support `seat` is 0.40 × 0.38 at y 0.45 on all three. The new chair is within ±2 cm of the old bounds: it is 20 mm lower and its box is 7 mm narrower.

## 5. Triangles and LOD switches

Counts are from the sidecars and match Unity's import (`W2KitReimport`, fix 2: `W/w2_props/fix2/probe_final/w2_reimport.txt`) and Blender's own count of the exported FBX (`fix2/bl_check`). Switch distances are from `FrontRoomsCxVisLodDump` (`fix2/probe_final/cxvis_lod_dump.txt`).

| Kit | LOD0 | LOD1 | LOD2 | Budget | Switch (m), at screen height | LODGroup size |
|---|---|---|---|---|---|---|
| Kit_Torchiere (fix 2) | **4,488** | **1,569** | **394** | 4,500 / 1,600 / 400 ✔ | 8.0 / 20.0 / 54.0 cull (h 0.1351 / 0.0540 / 0.0200) | 1.689 |
| Kit_TaskChair (fix 2) | **4,634** | **1,620** | **420** (proxy) | 5,200 / 1,800 (≤ 1,792) / 450 ✔ | 6.0 / 12.0 / 30.0 cull (h 0.0995 / 0.0498 / 0.0199) | 0.933 |
| Kit_TaskChair_Navy | 4,634 | 1,620 | 420 (proxy) | same ✔ | same | 0.933 |
| Kit_TaskChair_Armed (fix 2) | 4,484 | 1,792 | **428** (proxy) | ≈ 4,484 / 1,792 / 450 ✔ | 6.0 / 12.0 / 30.0 (h 0.1016 / 0.0508 / 0.0203) | 0.953 |
| (fix 1 torchiere / chairs LOD2) | 4,392 / — | 1,536 / — | 385 / 416, 447 (decimated) | | | |
| (old torchiere / chair) | 636 / 4,484 | 267 / 1,792 | none | | default 10 % / 2 % | |

**Office cost.** A station chair at LOD1 averages 0.85 × 1,620 + 0.15 × 1,792 ≈ 1,646 tris, against 1,792 before (−8 %). At LOD2 the proxies cost 420 / 428.

**Draws.** The torchiere's LOD0 has 3 submeshes (3 slots), up from 2 before W2, so each lamp costs +1 draw. The chairs keep 3; their LOD2 proxies keep 3 too (the armed one's gas cylinder is chrome).

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

**Images.** The VL composites are in `images/`, all our own renders. Fix round 2 re-rendered **every** frame (W2Lookdev run 113, KitLookdev run 114, three-views runs 115–116) and rebuilt every composite in the **Shot format** (`Documentation/SHOT_FORMAT.md`, `W/tools/shotframe`: FRONTROOMSSS watermark, fixed header, JPG q92). The fix-1 composites are kept in `W/_partial/w2_images_fix1_134*/`, round 0's in `W/_partial/w2_images_r0_0820/`.

| Image | VL | Frames |
|---|---|---|
| `20_torch_hero_close.jpg` | VL308 | hero 3/4, standing eye (1.62 m, 2.4 m away); **0.3 m:** cup + knob, bowl underside, from above (glass lip, tube), base |
| `21_torch_before_after.jpg` (new name) | VL308 | round 0, fix 1, fix 2 side by side, same cameras: 0.3 m cup, 0.3 m bowl, 0.3 m base, 0.8 m wall lamp, standing eye, 8 m |
| `22_torch_measure.jpg` (new) | VL308 | the notches at 2.4 m (6× pixels) against fix 1's smooth outline; the knob in round 0, fix 1, fix 2; the 0.8 m wall lamp; chip FLAG for the two near misses (§11.3) |
| `20_torch_ownlight.jpg` | VL308 | lamp light on with troffers on; its own light only (3000 K up-spot + ceiling bounce); under; above |
| `20_torch_threeview.jpg` | VL308 | the 1× three-view on paper |
| `20_chair_hero_close.jpg` | VL309 | grey and navy heroes (the lamp beside them is fix 2's); **0.3 m:** navy seat, grey seat, lever + the 2 cm piston, back bar, caster; the armed hero for scale |
| `20_chair_threeview.jpg` | VL309 | grey, navy and armed, 1× on paper |
| `20_armed_office_share.jpg` | VL310 | armed hero + **0.3 m** arm; KitLookdev `office_desk` seed 3 (the gate frame); seed 1 grey room; seed 13 navy room |
| `20_pile_l0.jpg` | VL311 | KitLookdev `pile`, `pile_close`, `pile_side`, `office_entrance` (seed 3); the L0_WaitingRoom placement and the 16 mm bowl-to-wall gap |
| `20_lod_forced.jpg` | VL311 | the 4 kits × LOD0/1/2 (`LODGroup.ForceLOD`, vFOV 30; the camera backs off to fit the kit: 3.9 m for the torchiere, 2.8 m for the chairs) |
| `20_lod_switch.jpg` | VL311 | the 4 kits at each switch distance (vFOV 76, eye 1.62), with the two levels that meet there |
| `23_lod_switch_zoom.jpg` (new) | VL311 | 4× pixel crops of those frames: chairs at 12 m LOD1 vs LOD2, torchiere at 20 m |

The side-by-side with the Sears 1993 p.319 N crops (third-party media) stays outside the public repo: `W/w2_props/fix2/cmp_sears_fix2.jpg` (fix 1's: `fix1/cmp_sears_fix1.jpg`).

**Verdicts (fix 2, measured; §14):**
- **Torchiere.** A polished-brass torchiere with a stepped frosted glass bowl, after Sears N:
  - the bowl is lighter than the room at every distance (L\* 66 vs 22 at 0.3 m from below, 52 vs 36 at 0.8 m, 60 vs 42 from standing eye) and shows its terraces in the outline (ledge jumps 3–4 px at 2.4 m) and as faint rings, never dark ones;
  - the glass lip shows above the brass band;
  - the brass is warm: pole h 75 at 0.3 m and standing eye (fix 1: 88, khaki), base C 32 h 88 at 0.3 m and C 37 h 96 from above (no longer silvery); base highlights C 22;
  - the knob stands out in profile on the cup's right side.
  - Two near misses and one grade limit are listed in §11.3.
- **Chairs.** They read armless; grey and navy separate clearly from the black base and shell. At 0.3 m the pads and the 16-sided wheels hold up; ~~2 cm of chrome shows instead of the 6 cm rod that read rusty~~ the 2 cm piston no longer reads rusty, but it reads dark (L\* 10), not chrome: a limit (§16.4).
- **Spec §4.3 gate (grey chair vs the cubicle panel): PASS**, unchanged since round 0 (seat vs panel ΔE76 15.8; back pad facing away 6.6).
- **LOD.** At the 12 m switch the chairs' LOD2 keeps the star, the casters (as blocks), the column, seat, back and, on `_Armed`, both loop arms whole (`23_lod_switch_zoom.jpg`). Round 2's finding was right: fix 1's sentence "LOD2 keeps the silhouette" was false for `_Armed` (55 shards) and loose for the armless chairs (casters as specks).

The KitLookdev `asset_*` frames (in `W/w2_props/kit_lookdev4/seed3/`) are dark because of that tool's line-up room. Main's tool does the same for every kit, so the hero frames come from `W2Lookdev`.

## 8. Three-views for 平面视觉 · 印刷与演示

The hand-off is in `threeview/`; see its `README.md`. **All four kits were re-rendered in fix round 2** with the same renderers, scale and padding (1× hand-off renderer, oversample 1: run 116; 2× W/tools renderer: run 115), and `handoff_manifest.json`, `2x/manifest.json` and `index.json` were refreshed (triangles, px, the torchiere's lede, swatch and era note, the chairs' era notes). `Kit_TaskChair_Armed`'s views came out byte-identical (its LOD0 is unchanged); the armless chairs' top views too (the piston does not show from above). The fix-1 folder is kept in `W/_partial/w2_threeview_fix1_134645/`.
- `<Kit>_{front,side,top}.png` at 250 px/m (1 PNG px = 1 sheet px, 16 px padding, transparent);
- `<Kit>_hero.png` (1152 × 864);
- `handoff_manifest.json`;
- `index.json`, in the same schema as `research/outlets/threeview/index.json`;
- `2x/`: the 500 px/m set with the W/tools manifest, for print.

**Swatches:** polished brass `#F2B852` (fix 2; fix 1 `#F2DE9E`, `Prop_Brass` `#B08A4A`), frosted bowl `#EBE7DE`, grey `#5A646E`, navy `#23304A`, black fabric `#1C1C1E`, black plastic `#1A1A1A`, chrome `#D8D8D8`. In a studio three-view the bowl shows as an even white bowl: its tilted normals only matter under the game's ceiling light.

## 9. Map suites (`FrontRoomsCxSuites.RunBatch -cxOnly suites`)

| Suite | Base 06:57 | Base 07:13 | **Fix 1** (main 07:59 + W2 + fix 1, run 61) | **Fix 2** (run 120) |
|---|---|---|---|---|
| FrontRoomsMapVerification | 100 / 0 seeds | 100 / 0 | 100 / 0 | 100 / 0 |
| FrontRoomsRelayNavTest | PASS 60/60 | PASS 60/60 | PASS 60/60, 0 pass-throughs, 0 furniture frames | PASS 60/60, 0 pass-throughs |
| FrontRoomsMapInteractionTests | 163 / 0 | 174 / 0 | 174 / 0 | 174 / 0 |
| FrontRoomsLevelDesignerTests | 140 / 0 | 140 / 0 | 140 / 0 | 140 / 0 |
| FrontRoomsFixtureTickTests | 28 / 0 | 28 / 0 | 28 / 0 | 28 / 0 |
| FrontRoomsCameraRigTests | 33 / 0 | 33 / 0 | 33 / 0 | 33 / 0 |
| FrontRoomsCaptionsTests | 9 / 0 | 9 / 0 | 9 / 0 | 9 / 0 |
| FrontRoomsGlassShotTests | 20 / 0 | 20 / 0 | 20 / 0 | 20 / 0 |
| FrontRoomsStreamVerification | 5 / 0 | 5 / 0 | 5 / 0 | 5 / 0 |
| FrontRoomsGlassCompileCheck | no exception, 0 FAIL | no exception, 0 FAIL | no exception, 0 FAIL | no exception, 0 FAIL |

**Console noise.** Only the tests' own injected errors: 2 errors "forced build failure" (chunk (1, 0) left out) and 4 exceptions "test: a broken glass kit" in MapInteractionTests, plus module-prop warnings. None names a chair or the torchiere. Reports: `W/w2_props/cx_suites/`, `cx_suites_final/`, `cx_suites_fix1/`, `cx_suites_fix2/`.

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
- **in furniture piles it has no collider, as before W2** (tag `pile_walkthrough`, read by `FrontRoomsFurniturePile`). Over map seeds 1–400 there are 42 pile torchieres, all with 0 colliders;
- fix 2: the `dimmer` anchor moved to (−0.062, 1.555, 0), the knob's new place on the cup's side. No code reads it. The fix-2 mesh, normals and material changed nothing the map sees (bounds, collider, footprint, minCeiling and tags are as in fix 1; the signatures differ from fix 1 only in renderer hashes, §14.6).

**Keep-out note: Kit_Torchiere as a module prop (L0_WaitingRoom_4x3 and any future module).**
- **Solid volume.** One box, 0.39 × 0.39 × 1.689 m, centred on the lamp (its whole height, not only the bowl). The player and the Relay route round it; plan their clearance from the lamp centre: Relay 0.195 + 0.30 = **0.50 m**, player 0.195 + 0.30 = **0.50 m** (+0.04 m planner margin for the player). Do not put it in a 1.2 m doorway approach or a 1-cell corridor: with the bowl it leaves ≤ 0.8 m.
- **Wall.** The bowl (Ø0.39, from 1.579 m up to 1.689 m) is the widest part; the base is Ø0.245 and the pole Ø0.024. Against a wall, set the stored offset so the bowl clears the wall face: x = half-wall 0.08 + gap + 0.195. The module's stored x 0.291 gives a **16 mm** gap (there is no clash); x **0.305** gives the 30 mm gap the module was built with. That edit is 关卡设计's file.
- **Wall-mounted items.** Keep signs, placards, clocks, switches and the like out of a zone **0.50 m wide × 0.15 m tall** (1.57–1.72 m) centred on the lamp on that wall: a 30 mm-deep item there would cut into the bowl (the bowl comes within 30 mm of the wall face over ±0.07 m along it, more for deeper items).
- **Ceiling.** minCeiling 1.739 m: fine under every zone height (Low 2.4 m), but not under a soffit or a low shelf.
- **Knob.** It sticks out to 62 mm from the axis on the lamp's local −X (the viewer's right from the front) at 1.555 m: inside the bowl's footprint, so no extra keep-out. In L0_WaitingRoom_4x3 (yaw 90, west wall) it points along the wall.
- **Piles.** Unchanged: a pile torchiere has no collider and no keep-out of its own; the pile's own rules apply.
- **Light.** The lamp is off and nothing is emissive, so it needs no light-budget or flicker planning. A lit variant would be a separate kit (§11.7).

**Map piles change.** `Kit_TaskChair_Navy` joins the pile library and the chair and lamp bounds changed in round 0, so a map pile whose solver reaches those picks or fit tests is rebuilt with other pieces. In the 20 signature maps that is **5 of the 6 map piles, in 4 seeds** (4242 chunk 2_0, 20261005 2_−2, 20261008 −1_−1 and −2_0, 20261010 −2_2); §12.7 lists their pieces before and after. Fix 2 changed no pile (same pieces, same colliders). Every map still builds the same way twice.

**What was tested on those piles (fix 2, §14.7; fix 1's sentence "Relay nav and the playtests pass on the new piles" was not backed and is withdrawn):**
- `W2PileNav`, each pile in its own chunk: 0 doorways or doors touched by a pile piece, 0 open cells cut off, 60/60 Relay hunts from and round the pile with 0 pass-throughs and 0 frames in furniture: **PASS for all 5**.
- Main's `FrontRoomsRelayNavTest` on the 4 seeds: PASS 60/60 each. Its hunts start in the 3 × 3 chunks round the spawn chunk (0, 0), so it reaches only 20261008's −1_−1 pile; the other four are 2 chunks out.
- MainScenePlaytest on 20261008: PASS twice (75 s, 76–79 cells, 0 errors). Its walk (tracked) stays in chunks (8…10, −1…1): the main scene starts it elsewhere on the seed's map, so it did **not** pass the rebuilt piles. It shows only that the seed plays.

**L0_WaitingRoom_4x3.** The stored x is 0.291, so the bowl is **16 mm** from the wall face (was 30 mm); measured in the clone, `l0_wall_torchiere_gap`. There is no clash. Regenerating the module restores the 30 mm gap; that is 关卡设计's call and file (see the keep-out note).

**Kit_TaskChair:**
- 20 mm lower: minCeiling 1.003 → 0.983;
- its box is the true bounds, 0.617 × 0.933 × 0.654 (was 0.624 × 0.954 × 0.656);
- anchor and support names are unchanged;
- fix 2: a hand-built LOD2 on all three chairs (switch distances unchanged, 6 / 12 / 30 m).

**Office stations.** The hierarchy now names the chair `Kit_TaskChair`, `Kit_TaskChair_Navy` or `Kit_TaskChair_Armed`. No code looks the name up (grep of Assets): the only reference is `FrontRoomsOfficeKit.Chair`.

**Files of theirs this touches:** none. `FrontRoomsFurniturePile.cs` and `FrontRoomsOfficeKit.cs` are visual-owned, and fix 2 changed neither.

## 11. Open issues

1. **Not landed.** The package `office_props_w2` is ready (§13): CLAIM in `LANDINGS.md`, dry run, `--apply`. The RenderSetup hunk (`office_props_w2_rendersetup.diff`, now with the fix-2 brass) lands separately, after outlets_p4b.
2. **Metals in the game run at half reflection.** `FrontRoomsZoneReflection.MaxLinear` caps every zone's reflection at 0.5 linear until per-room probes land (G7). So every metal reads darker than in a catalogue photo; the warmer fix-2 tint costs a little luminance too (pole L\* 25 at 0.3 m, 30 at standing eye; fix 1: 28 and 33; round 0: 19 and 23). A vertical pole cannot mirror a ceiling light from standing eye height; ~~its highlights come on the cup, the knob and the base beads~~ its highlights come on the base beads and the rim band only: the cup faces down under the bowl and mirrors the walls and the floor (§16.2). Raising the zone reflection is a global G7 decision, not W2's.
3. **For Red: accept these limits, or say which to chase** (VL308 is "PASS with limits" until then):
   - **Highlights** (refuted for the cup and pole by verify round 3: those pixels are body colour, not highlights; §15.1). At 0.3 m ~~the brightest 10 % of the cup and knob are C 27, the pole's C 29 and~~ the base's highlights are C 22 (target ≥ 18; fix 1: base 9); from above the base's are C 18. Only the thin brass rim band, which mirrors a troffer almost edge-on, keeps bleached highlights (C 12, h 107). Highlight hue stays at 95–105 against Sears' h 80: the cores clip toward the grade's white (the troffer lens itself measures h 103 in the same frames), so no tint moves it. A more saturated tint (sRGB 0.96/0.71/0.28) lifts the base and band a little but turns the pole to h 72–73 and the base orange.
   - **The 0.8 m wall-lamp frame:** the lower third of the bowl is 0.84× of the bowl's median (target 0.85×). It is a smooth gradient from the foot (0.84) to the lip (0.98), on the side facing the wall, not a ring: neighbouring bands and ledges differ by ≤ 0.07. From standing eye the darkest band is 0.90×, from below 0.95×.
   - **The dimmer's place** (round 2, era-art 9, general knowledge, not in our files): a 300 W halogen torchiere usually has its dimmer on the pole at hand height or on the cord, not under the tube. We kept it on the cup's side, where the documented reference (Sears N, a three-way incandescent) has its switch. Options: (a) keep it (Sears N's place; the K-sheet already shows it), (b) move it to the pole at about 1.1 m (the pre-W2 kit had a pod at 1.05 m; it would read from standing eye but not as Sears N), (c) a cord dimmer on the floor (hard to see). Recommendation: (a), because the lamp is unplugged and the catalogue look is what the era-art review checks. Nothing waits on it.
4. **`Prop_Brass` for other kits.** 38 kits keep the shared `Prop_Brass`, which reads bronze. Whether to warm or brighten it for all of them is Red's call; fix 2's measurements (§14.3) show what a tint does after the grade.
5. **Gate margin.** The grey back pad facing away from the light is close to the cubicle panel (ΔE76 6.6). If Red wants more separation, the spec's `Prop_FabricGrey` fallback is ready: a new `.mat` plus a separate RenderSetup diff.
6. **Halogen vs the literal Sears N.** Halogen with frosted glass rests on general heat reasoning ([GEN] in the spec). The acrylic, 150 W, three-way N is the documented alternative.
7. **Later, and needing Red:**
   - a lit torchiere variant (`Prop_LampShadeLit` / `Prop_BulbLit` plus a real up-light; foot-bright needs an emission mask);
   - red and pink chairs, which need new fabric materials.
8. **Follow-ups for other owners:**
   - `22_era_lock.md` rows 40 and 76: the descriptions, plus rows for `_Armed` and `_Navy`;
   - K40 and K04 re-render, R03/R39 refresh, and new K-sheets for `_Navy` and `_Armed` (平面视觉 · 印刷与演示, from `threeview/index.json`; every record changed in fix 2);
   - VL308–VL311: specs in `W/vl_pending/w2_office_props.md` (rewritten for fix 2), to be placed by the 游戏视觉 main session.
9. **Tool quirks** (main's tools, not W2):
   - KitLookdev's `office_desk` pose sits inside a panel for seeds 1 and 13; seed 3 frames the gate;
   - its `asset_*` line-up frames are dark for every kit;
   - `FrontRoomsMainScenePlaytest` fails in a clone without the FMOD banks (the `BankLoadException` counts as an autopilot error); its report has no positions (W2PlayTrack fills that in for W2 runs);
   - the game's first-person camera is not tagged `MainCamera`.
10. **Stale remaps.** The post-import `.fbx.meta` files keep remap entries for slots the kits no longer use (`Prop_SteelBlack`, `Prop_Glass`, `Prop_Rubber`, and for the torchiere also `Prop_Brass`, `Prop_PlasticWhite`). Harmless: FrontRoomsKitImporter only adds and removes remaps for current slots. Fix 2 did not change the metas.
11. **Clone state.** `W/proj_w2` is main `0ea38bb` + working tree of 07:59 + the final W2 payload (fix 2) + the W2 harness + main's FMOD banks, imported. It was **not** re-synced to main `2c450e11`: main's later changes are in audio, the print driver, the Ad, broadcast tools and the touch layer, none in the payload's files or bases, and keeping the 07:59 state keeps fix 2's frames comparable with fix 1's and the verifier's; the payload's 23 bases and the compile were checked on `2c450e11` instead (§13). Its `FrontRoomsThreeView.cs` is the W/tools version again. The variant kits are parked; nothing was deleted (earlier copies in `W/w2_props/payload_r0/`, `payload_fix1/`, `fix2/base_fix1/` and `W/_partial/`).
12. **Red's commits.** Red committed the build stage's files at 07:21 (`0ea38bb`) and fix 1's at 09:10 (`d4d0e7a5`). Fix 2 changed `10_spec.md`, this report, every `images/2*.jpg` composite (21_torch_fix1_before_after.jpg moved to `W/_partial/w2_images_fix1_134*`; 21_torch_before_after, 22_torch_measure and 23_lod_switch_zoom are new) and the `threeview/` files; uncommitted.

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

So relative to its surroundings the pole matches the catalogue; in absolute terms it stays darker, for the reasons in §11.2. The verifier's frames are re-shot (eye, 0.3 m, 8 m, three-view) and laid beside the Sears crops in `W/w2_props/fix1/cmp_sears_fix1.jpg` (outside the repo; the repo copy without the scans was `images/21_torch_fix1_before_after.jpg`, moved in fix 2 to `W/_partial/w2_images_fix1_134711/`; the current repo composite is `images/21_torch_before_after.jpg`).

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

A custom mesh replaces `kit.extrude(..., bevel=0.0)`: the 24-point knurl (12 ribs), a 1.8 mm 45° chamfer on the front edge and a low dome (about 1 mm) on the face, plus a back cap. At 0.3 m the rim and the dome now catch the troffers (a highlight on the dome, a dark knurled rim), where round 0 showed a pale flat disc (`#83652E` against the cup); see `W/_partial/w2_images_fix1_134711/21_torch_fix1_before_after.jpg`, row 1 (moved out of the repo in fix 2; `images/21_torch_before_after.jpg` shows round 0, fix 1 and fix 2).

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

In `W/apply/` (convention v2, `W/apply/README.md`), rebuilt for fix 2 by `W/w2_props/fix2/build_package2.py` at 13:43 on main `2c450e11` + working tree (fix 1's builder was `fix1/build_package.py`, 08:18; its files are kept in `W/_partial/apply_office_props_w2_fix1_*` and `apply_office_props_w2_{payload,base}_134323`):

| File | What |
|---|---|
| `office_props_w2.sh` (sha1 `7c4770da`) | dry run by default, `--apply` writes; marker `apply-convention: v2`; honours `APPLY_REAL`, `APPLY_DIR`, `APPLY_BACKUP`; checks that the 6 new GUIDs are unused in the target |
| `office_props_w2_bases.txt` | 23 paths: 10 sha1 bases, 13 **ABSENT** (the new paths); the bases are the same as fix 1's |
| `office_props_w2_keep.txt` | 6 kept, never written: `Kit_Torchiere.json.meta`, `Kit_TaskChair.json.meta`, the two Office `.cs.meta`, `Resources/Surfaces.meta`, `Resources/Props/Models.meta` |
| `office_props_w2_guids.txt` | the 6 new GUIDs |
| `office_props_w2_expected.txt` | the payload in write order: material metas and materials, then per kit sidecar meta, sidecar, FBX meta, FBX; then the two scripts and the three Blender modules |
| `office_props_w2_payload/`, `office_props_w2_base/` | the 23 files; exact copies of the 10 bases |
| `office_props_w2.diff` (sha1 `cf1eb01b`) | unified diff base → payload for the text files |
| `office_props_w2_rendersetup.diff` (sha1 `d7007126`) | the separate SurfaceDef hunk with the fix-2 brass; **not applied by the script** |

Fix 2 changed 12 of the 23 payload files: the four FBX and their sidecars, `Prop_BrassPolished.mat`, and the three Blender modules. The `.fbx.meta` files, the GUIDs, `Prop_FrostedGlass.mat` and the two `.cs` files are byte-identical to fix 1's.

**Checks (13:43):**
- Dry run on main `2c450e11`: **DRY RUN OK** (23 bases match, 6 kept metas unchanged, 6 GUIDs unused, 23 payload files match).
- `W/tools/rebase_apply.sh office_props_w2 --no-install`: **PASS**, nothing to rebase.
- Roslyn `--all` on main `2c450e11` + working tree + the two scripts: COMPILE OK, macOS and iOS, with and without the RenderSetup hunk (§2).
- No other package in `W/apply` with a live claim touches these paths (the older ones that touched the Office scripts, `inspect`, `office_dress_hitch`, `outlets_c1/int`, landed on 10-07..10-21 and their bases are behind main's).
- Fix 1's scratch-target test (`--apply` → MANIFEST OK → ALREADY APPLIED → REFUSED on a changed base) exercised the same script template; it was not repeated.

**To land:** CLAIM in `LANDINGS.md`; `bash W/apply/office_props_w2.sh` (expect DRY RUN OK; on a base mismatch run `rebase_apply.sh` first); `--apply`; then Unity reimports the four kits and the two materials. The RenderSetup hunk follows after outlets_p4b.

## 14. Fix round 2 (2026-10-22 13:15–13:50)

Round 2's verifiers: era-art NOT READY (9 findings), engineering NOT READY (3). Their full texts are kept in `W/w2_props/fix2/verify_era-art_2.md` and `verify_engineering_2.md`. Every frame was measured on a mask rendered with it (`W2Fix2Variants`: a second camera without post or MSAA draws the torchiere with `Hidden/W2Mask`, slot in R, object height in G and B), so parts are separated by slot and height, not by hand-drawn boxes (`fix2/measure2.py`, `bowl_profile.py`). CIELAB D65, after the post stack. "Surround" = the frame's non-lamp pixels in a ring 40–120 px round the lamp. The verifier's own numbers came from boxes on fix 1's frames, so their wall values (39, 47) differ from our surround values (22–42) for the same scenes.

### 14.1 The glass (era-art 1)

**What the verifier proposed** (outer normals copied from the inner face) was built and measured first (runs 99–104). It works too well: the underside reads like an upward-facing white surface, L\* 83 against walls at 42 from standing eye, a lamp that looks switched on. Blending it with the geometric normal does not tame it: between 55 % and 45 % the underside drops from L\* 80 to 13, because the two normals point almost opposite ways. A **fixed tilt** is stable, so the line-up went over that (runs 106–109, clone-only variant kits; ledge dip 8° unless noted):

| Bowl outside normal | 0.3 m from below: bowl / surround | 0.8 m wall lamp: bowl / surround | standing eye: bowl / surround | darkest band (0.8 m) |
|---|---|---|---|---|
| fix 1 (geometric) | 22 / 39 (verifier) | 19 / 47 (verifier) | darker than the wall | |
| copied inside normal, 85 % (verifier's recipe) | 83 / 22 | 56 / 36 | 82 / 42 | 0.93 |
| copied, 55 % | 80 / 22 | 50 / 36 | 82 / 42 | 0.72 |
| copied, 45 % | 13 / 22 | 21 / 36 | 21 / 42 | 0.75 |
| tilt 8° | 35 / 22 | 37 / 36 | 41 / 42 | 0.74 |
| tilt 20° | 50 / 22 | 43 / 36 | 40 / 42 | 0.75 |
| tilt 35° | 56 / 22 | 48 / 36 | 44 / 42 | 0.87 |
| **tilt 45°, ledges 40° (shipped)** | **66 / 22** | **52 / 36** | **60 / 42** | **0.84** |
| tilt 55° | 73 / 22 | 55 / 36 | 73 / 42 | 0.86 |

45° was chosen: clearly lighter than the walls (~~ivory, as on the scan~~ a neutral to cool white next to the warm walls, h 97 C 14 against walls at C 24–31; Sears N's bowl is ivory, h 67: §16.5) without the glow; 35° sits at the wall's level from standing eye, 55° starts to glow. The 4.5 mm glass lip above the band shows as a white edge from standing eye and from above. `Prop_FrostedGlass` is unchanged (G1–G3 line-ups of lighter/darker and rougher variants moved the underside ≤ 3 L\*).

### 14.2 The terraces (era-art 2)

- **Profile.** 6 bands at 45° (15.5 mm run, 15.4 mm rise) and 5 ledges of 8 mm radial at 12° up and out; the 4.5 mm band and the glass lip above them.
- **Outline at 2.4 m** (standing eye, vFOV 60, from the mask): the left edge steps 1 px per row along a band and jumps **4, 3, 3, 3 px** at the ledges, the right edge **4, 4, 3, 3 px** (fix 1: a smooth curve). Target ≥ 3 px: met.
- **Bands** (mean grey of each band and ledge ÷ the bowl's median):

| Frame | Range | Darkest |
|---|---|---|
| standing eye, 2.4 m | 0.90–1.08 | band 1 (next to the cup) 0.90× |
| 0.3 m from below | 0.95–1.09 | ledge 4 0.95× |
| 0.3 m cup frame | 0.96–1.23 | ledge 5 0.96× |
| 0.8 m wall lamp | 0.84–1.00 | bands 1–2 and ledge 1, 0.84× (§11.3: a gradient toward the foot on the wall side, not a ring) |

### 14.3 Brass hue (era-art 3)

`_MacroTone` and `_MacroDirt` went to 0 first; at 0.02 they were never the blotches (they move albedo by < 2 %). The mottling on the cup and pole is the reflected room (the Level 0 zone cube with its patterned walls) in a near-mirror. Line-ups (runs 101, 105) of 12 candidates, measured at 0.3 m (cup frame, base frame), from above and from standing eye:

| Candidate (sRGB tint, smoothness) | pole h (0.3 m / eye) | pole L\* eye | base C, h (0.3 m) | base highlights C (0.3 m / above) |
|---|---|---|---|---|
| fix 1 tint 0.95/0.87/0.62, 0.78 | 88 / 88 | 33 | 21, 94 | 9 / 10 |
| W5 0.94/0.78/0.50, 0.78 | 81 / 80 | 31 | 22, 89 | 12 / 12 |
| W1 0.93/0.75/0.45, 0.78 (verifier's start) | 79 / 78 | 30 | 23, 87 | 14 / 12 |
| W2 0.95/0.72/0.40, 0.78 | 75 / 74 | 29 | 25, 85 | 16 / 13 |
| Y1 0.95/0.72/0.32, 0.78 | 75 / 74 | 29 | 27, 85 | 22 / 17 |
| **Y3 0.95/0.72/0.32, 0.72 (shipped)** | **75 / 75** | **30** | **32, 88** | **22 / 18** |
| Y4 0.96/0.70/0.30, 0.74 | 73 / 72 | 29 | 31, 87 | 23 / 18 |
| Y2 0.96/0.71/0.28, 0.78 | 73 / 72 | 29 | 28, 85 | 25 / 19 |
| W3 0.96/0.68/0.34, 0.78 | 71 / 69 | 28 | 26, 82 | 19 / 15 |

(Also W1 and W2 at smoothness 0.70: pole hue as at 0.78, base body lighter, as Y3 against Y1; and Y5 0.97/0.74/0.30: pole h 75, base highlights 24 / 18, base body L\* 35.) Y3 is the only candidate with the pole inside h 70–82 at both distances and the base's highlights ≥ 18 from the side and from above; smoothness 0.72 also lifts the base's body (median L\* 49 at 0.3 m against 34 at 0.78) and blurs the reflected room on the cup (L\* spread on the cup at 0.3 m: std 2.9 against fix 1's 3.3; pole 2.1 against 2.6). Final frames (run 112): pole h 75 (0.3 m and eye), C 26–31; ~~cup and knob highlights C 27, pole C 29~~ (withdrawn, §16.2), base C 22 (above 18); rim band C 12 (§11.3). New .mat values: sRGB 0.95/0.72/0.32, smoothness 0.72, metallic 1, macro 0/0 (same GUID); the RenderSetup hunk carries the same values.

### 14.4 Knob (era-art 4)

Moved to the cup's right side (Unity −X) at 1.555 m: a 4 mm shank, then an 11 mm knurled cylinder Ø21 mm with 12 ribs only 0.4 mm deep (smooth-shaded, so the knurl reads as a light ripple, not dark slots), a 1.5 mm chamfer and a 0.9 mm dome. From the front and in the 0.3 m frame it shows as a brass cylinder standing off the cup in profile, like Sears N's switch (`22_torch_measure.jpg`: round 0 "pale disc", fix 1 "bead in a crater", fix 2). 190 + 28 tris.

### 14.5 Chair LOD2 (era-art 5, engineering 1)

- **Cause** (confirmed): `LODS = (0.40, 0.10)` decimated all 55 parts of the armed chair evenly (Blender 423 tris in 55 islands; Unity 447, the 24 missing were collapsed faces); the armless chair dropped its small parts but its casters still fell to specks.
- **Fix:** each chair module now builds its LOD2 by hand (`fr_lods "2"`, kitlib's P-1b path) and makes LOD1 itself by kitlib's own `_decimated` at the old ratio (kitlib decimates only when no level is hand-built; `build_asset.py` and `kitlib.py` are unchanged). The proxy: a 6-sided hub, 5 tapered legs (no bevel), a box per caster at its swivel angle, an open 6-sided column and piston, the mechanism box, seat and back pads and shells as rounded-rectangle lofts with fewer corner steps (the shield back keeps 3 so it stays an oval), the back bar; `_Armed` adds both loop arms (4-corner loops) and their pads.
- **Result:** `Kit_TaskChair`/`_Navy` LOD2 **420 tris in 19 parts** (was 416 in 30), `Kit_TaskChair_Armed` **428 in 23** (was 447 in 55); Blender and Unity agree on every count. LOD1 unchanged (1,620 / 1,792). GUIDs kept (`3cd95262…`, `13007464…` for `_Armed`). At the 12 m switch (`23_lod_switch_zoom.jpg`, 4× pixels) the star, casters, column, seat, back and arms stay whole.
- **Re-run:** reimport + `FrontRoomsCxVisLodDump` (6 / 12 / 30 m unchanged), signatures (§14.6), forced and switch frames, package (§13).

### 14.6 Signatures and dressing

- `W2Fix1Chain` (run 111): dressing probe **byte-identical to fix 1** (all four probe files); DressMapStepTest PASS (36 checks, 0 FAIL); DoorShotTests 191 / 0; LOD dump as §5.
- `W2SigDetail` against the no-W2 dumps (`sigdiff.py`): **500 chunks, 76 changed in 18 seeds, chair 1,476 / torchiere 200 / pile 480 / other 0 lines**: the same chunks and classes as fix 1, the same 5 rebuilt piles with the same pieces.
- Fix 1 → fix 2: 394 changed lines in 74 chunk files, every one on a `Kit_Torchiere` or `Kit_TaskChair*` path; CxSuites' TSV differs from fix 1's only in 74 renderer rows (hash only; counts equal), 0 collider or light rows.
- CxSuites signatures (run 121): 20 seeds, 0 second-world and 0 rebuild determinism failures; 105 console warnings, the same as fix 1, none naming a W2 kit. Map suites (run 120): MapVerification 100/0, RelayNav 60/60, MapInteraction 174/0, LevelDesigner 140/0, FixtureTick 28/0, CameraRig 33/0, Captions 9/0, GlassShot 20/0, Stream 5/0, GlassCompileCheck clean; the only console errors are MapInteractionTests' own injected ones (2 errors, 4 exceptions), as before.

### 14.7 The rebuilt piles (engineering 2)

All five sit 1–2 chunks from the capture build's spawn chunk (0, 0), at the edge of its 5 × 5 chunks; main's Relay nav test starts its hunts in the 3 × 3 round the spawn, so it reaches only one. `W2PileNav` (clone-only) builds the seed's map the same way and tests each pile in its own chunk (runs 125, after a first pass, run 122, that also judged open floor inside a room and so flagged a CentreSculpture standing on its own room's cell edges; those are now counted and judged by the cut-off check):

| Seed | Chunk (from spawn) | Pile | Doorways + doors touched | Open-floor edges under the pile | Open cells cut off | Hunts (pass-throughs) | Verdict |
|---|---|---|---|---|---|---|---|
| 4242 | 2_0 (2) | CentreSculpture, 22 pieces | 0 of 32 | 6 | 0 | 60/60 (0) | PASS |
| 20261005 | 2_−2 (2) | CopyPasteRow, 14 SideTableTurned | 0 of 44 | 2 | 0 | 60/60 (0) | PASS |
| 20261008 | −1_−1 (1) | CentreSculpture, 14 | 0 of 18 | 4 | 0 | 60/60 (0) | PASS |
| 20261008 | −2_0 (2) | CentreSculpture, 29 | 0 of 11 | 6 | 0 | 60/60 (0) | PASS |
| 20261010 | −2_2 (2) | CentreSculpture, 12 | 0 of 40 | 0 | 0 | 60/60 (0) | PASS |

Hunts: 40 from the chunk's cells to goals 5–14 cells away and 20 between open cells within 7.5 m of the pile, blind, by ear, 60 Hz, 30 s each, as in main's test; 0 frames in architecture, 0 in furniture outside a pass-through, 0 exceptions in every chunk. The 20261005 pile (5 → 14 pieces, +4 colliders) is a row of side tables along a wall: it touches no doorway and cuts no cell off.

Main's `FrontRoomsRelayNavTest` on the 4 seeds (run 123): PASS 60/60 each, door rule PASS; pass-throughs 3 / 0 / 18 / 1 (allowed by the test; it does not log where, and its 275 scattered test boxes in 20261008 are the likely cause, since the 60 hunts at that seed's two piles had none).

MainScenePlaytest on 20261008 (runs 124 and 126): **PASS** both times, 75 s, 76 and 79 cells, 0 errors, 0 Relay ghosts. `W2PlayTrack` logged the chunks the camera entered in run 126: (10, 0) → (10, 1) → (9, 1) → (8, 1) → (8, 0) → (8, −1) → (9, −1) → (8, −1). The main scene starts its walk away from the capture build's spawn chunk, so the walk never came near the rebuilt piles; the run proves only that the seed plays. §10 now says exactly this.

### 14.8 Other findings

- **era-art 6:** every frame was re-rendered; `20_chair_hero_close.jpg` shows the fix-2 lamp.
- **era-art 7 / engineering 3:** `W2Lookdev`'s forced-LOD camera now backs off to fit the kit (3.9 m for the torchiere, 2.8 m for the chairs); `20_lod_forced.jpg` row 1 shows the whole lamp at each level. LOD2's bowl is coarse when forced at 3.9 m; at its 20 m switch it reads as the same lamp (`23_lod_switch_zoom.jpg`).
- **era-art 8:** 2 cm of piston shows (§4). The chrome still mirrors a dim room; ~~at 2 cm it reads as a ring of metal, not a rusty rod~~ at 2 cm it no longer reads as a rusty rod, but it reads dark (L\* 10), not chrome (§16.4).
- **era-art 9:** question for Red (§11.3).

### 14.9 Fix-2 runs (`W/w2_props/logs/run_fix2.status`)

| Run | What | Time | 1-min load | Result |
|---|---|---|---|---|
| 99 | Blender: the three modules into the clone | 1 s | n/a | 4,488 / 1,569 / 394; 4,634 / 1,620 / 420; 4,484 / 1,792 / 428 |
| 100 | `W2Fix1Chain` (first fix-2 build) | 36 s | 28 → 26 | 0 `error CS`; PASS / 191-0 |
| 101 | brass + frost line-up 1 (7 + 3 candidates) | 20 s | 95 → 98 **UNDER LOAD** | 47 frames + masks |
| 102–105 | variant kits (normal share 65/55/45 %), reimport, bowl and brass line-up 2 | 15–16 s each | 21–24 | 4 kits; 32 + 35 frames |
| 106–109 | tilt variants (8/20/35/45/55°), reimports, line-ups | 10–66 s | 17 → 115 **UNDER LOAD** | tilt 45° chosen |
| 110 | Blender: final modules | 2 s | n/a | as §5 |
| 111 | `W2Fix1Chain` final | 41 s | 54 → 47 **UNDER LOAD** | 0 `error CS`; probe identical; PASS / 191-0 |
| 112 | measuring frames (`W2Fix2Variants cur`) | 15 s | 47 → 55 **UNDER LOAD** | 9 frames + masks |
| 113 | `W2Lookdev`, every frame | 20 s | 53 → 57 **UNDER LOAD** | 66 frames |
| 114 | KitLookdev seed 3 (assets, office, pile) | 15 s | 57 → 52 **UNDER LOAD** | 10 frames |
| 115 / 116 | three-views 2× / 1× | 20 / 15 s | 52 → 45 **UNDER LOAD** | 16 + 16 PNG |
| 120 / 121 | map suites / signatures | 45 / 25 s | 45 → 31 **UNDER LOAD** / 31 → 25 | PASS / 0 + 0 |
| 122 | `W2PileNav` × 4 seeds (first rule) | 10–15 s | 23–27 | superseded by 125 |
| 123 | main's Relay nav × 4 seeds | 15–16 s | 24–39 (last **UNDER LOAD**) | PASS × 4 |
| 124 | playtest 20261008 | 95 s | 37 → 58 **UNDER LOAD** | PASS (tracker found no camera) |
| 125 | `W2PileNav` × 4 seeds | 10–46 s | 140 → 309 **UNDER LOAD** | PASS × 5 piles |
| 126 | playtest 20261008 with the tracker | 96 s | 140 → 65 **UNDER LOAD** | PASS; chunks logged |

No run was killed; no run shared the clone; the visual line never had more than 2 Unity jobs from this stage's gate (it counts Unity executables, not wrapper shells).

## 15. Verify round 3: BLOCKED (package stage, 2026-10-22 14:10–14:20)

The package stage of workflow `office-props-w2` (account B, session local_a9a1dce2) read both verdicts of verify round 3. The era-art lens is NOT READY, so by the workflow's rule it did not build or refresh an apply package, did not write the three-view hand-off, the notes for 平面视觉 and 关卡设计, or new VL specs. It ran no Unity and no Blender (1-minute load 37 at 14:11). `W/apply/office_props_w2*` is the fix-2 package of 13:43, unchanged; its 23 bases still match main `2c450e11` + working tree at 14:15 (10 sha1s, 13 ABSENT), but it must not be applied.

**Where the verdicts are:** `W/w2_props/fix3/verify_era-art_3.md`, `W/w2_props/fix3/verify_engineering_3.md` (verbatim). Evidence crops: `W/w2_props/fix3/evidence_v3/` (`rim_top_1x.png`, `rim_top_zoom.png`, `bowl_left_edge.png`, `cup_crop.png`, `piston_zoom.png`, `torch20_pop.png`, `back_rear_crop.png`, Figma cards `r39_now.png`, `r03_now.png`, script `lab.py`; `sears_pole.png` is a third-party crop and stays outside the repo).

### 15.1 The era-art blockers, checked

The stage checked the two main defects against the files before writing them here:

- **Facets (1).** `torchiere.py` (payload, line 111) has `OUTER_SIDES = 48` for the bowl's outside, its bottom ring and the brass band, and the dished inside at 32 sides (line 385, "seen only from above"). In `W/w2_props/lookdev5/c03_torch_bowl_rim_top.png` the rim's outline is a polygon at 1:1, and the shadowed inner wall under the lip shows as a dark crescent, so the bowl reads as a dish set into a ring. The VL308 claim "holds up at 0.3 m" does not stand.
- **Brass (2).** `W/w2_props/fix2/final_measure.tsv`, median L\* / brightest-10 % L\*: cup 20.3 / 27.7 and pole 24.8 / 27.8 at 0.3 m; cup 22.4 / 28.7 and pole 30.1 / 33.3 at standing eye. The verifier's Sears N figures: pole L\* 70 with highlights at 93, cup 65 with 87 (spread 16–18). Only the base reaches real highlights (brightest-10 % L\* 83). The "C 27 / C 29 highlights" quoted in §0a, §11.3 and VL308 are the chroma of the brightest body-colour pixels, not of a highlight. Those claims are withdrawn for the cup and pole; the base's (C 22) stands.

Defects 3–7 are as listed in §0b. What passed (verifier's words, condensed): no anachronism or brand in game content; at room distance the torchiere matches R39 (bowl lighter than the walls, notches 3–4 px at 2.4 m, glass lip, knob in profile, base polished brass at 0.3 m); the chairs match R03 and Sears M/N (fabric reads at 0.3 m, twin casters, back bar, LOD2 holds at 12 m); the dimmer question is rightly Red's.

### 15.2 Triangle cost of the facet fix (estimate) and the decision for Red

Each lathe segment costs 2 × sides triangles. Counting the profile points in `torchiere.py` reproduces today's LOD0 (about 4,400 of the real 4,488: bowl outside 13 segments × 2 × 48 = 1,248; base 22 × 2 × 40 = 1,760; cup 8 × 2 × 24 = 384; band 3 × 2 × 48 = 288; inside 224; bottom ring 96; small parts about 400). The verifier's fix costs:

| Change | Extra tris |
|---|---|
| brass band 48 → 96 sides (3 segments) | +288 |
| glass lip 48 → 96 (2 segments above band 6) | +192 |
| top band (band 6) 48 → 96 | +96 |
| stitch ring 96 → 48 at the ledge under band 6 (no T-junctions) | +48 |
| inner rim 32 → 64, plus its stitch | +96 |
| crescent: inner-wall normals tilted up | 0 |
| (or) crescent: rounded lip-to-inner edge, 2 more rings at 64 | +256 |

So the minimum fix is about **+720 → LOD0 ≈ 5,210** (normals for the crescent), or about +980 → 5,470 with a rounded edge. Today's budget is 4,500 with 12 to spare.

**Decision for Red (blocks fix round 3's torchiere part).**
- **Background.** The torchiere is the hero prop of the waiting room and Red's rule is "highest spec, desktop is the reference". The player can walk right up to the lamp and look down into the bowl, so the rim's outline is the edge the eye catches first. Today you see a polygon with a corner every 7.5°.
- **Option A, recommended:** raise Kit_Torchiere's LOD0 budget to **5,300** (the chair's is 5,200), keep LOD1 ≤ 1,600 and LOD2 ≤ 400 by lowering the LOD ratios (about 0.30 / 0.075). The player sees a round rim at 0.3 m from above and below. Cost: about +720 tris per lamp, LOD0 only (within 8 m), at most a couple of lamps per room; WebGL is a separate tier and untouched.
- **Option B:** stay at 4,500 and pay from the base (beads 3 → 2 points each, about −400; base 40 → 36 sides, about −170) and the cup. The rim rounds out, but the base, which the verifier now passes as polished brass at 0.3 m, gets coarser, and that still leaves about 150 tris to find.
- **Option C:** keep 48 sides and write "rim faceted at 0.3 m" as a VL308 limit. The player sees a polygonal rim when looking into the bowl.
- **What waits on it:** the torchiere's geometry in fix round 3. The chair fixes (defects 4 and 7), the wording fixes (3, 5) and the brass line-up (2) do not wait.

### 15.3 Plan for fix round 3 (in `W/proj_w2`, one clone, at most 2 Unity jobs on the visual line)

1. **Torchiere geometry (defect 1, after Red's call).** Separate lathes or per-segment side counts in `_surfaces` for the band, the lip and band 6 at 96 and the inner rim at 64, joined by stitch rows (no T-junctions, so the custom glass normals stay continuous). Remove the crescent with upward inner-wall normals; measure against a rounded edge only if Option A has room. Check the dimmer anchor and collider are unchanged. Re-shoot `c03_torch_bowl_rim_top` and `c03_torch_bowl_under`; pass = no corner on the rim at 1:1.
2. **Brass (defect 2).** A line-up of `Prop_BrassPolished` smoothness 0.80 / 0.85 / 0.90, and one variant with a cup-and-knob-only slot at 0.88 (a third material: new `.mat`, new GUID, SurfaceDef as a separate diff, one more submesh). Measure brightest-10 % L\* minus median on the cup at 0.3 m (target ≥ 20) and the pole's mottle. If nothing reaches 20 under the G7 0.5 reflection cap, take option (a): write "cup and pole read dark bronze at 0.3 m, and the cup at standing eye (G7 reflection cap)" into VL308's limits and §11.3.
3. **Composites (defect 3).** Re-frame the VL308 composites (`20_torch_hero_close.jpg`, `21_torch_before_after.jpg`, `22_torch_measure.jpg`) with `shotframe.py --status WAIT-RED`, and drop the "holds up at 0.3 m" sentence until 1 passes.
4. **Chairs (defects 4, 7).** Piston: reword VL309 ("the piston reads dark at 0.3 m") or move the chrome band to where it picks up the room light. Back shell and seat front: more corner steps within the 566 tris of headroom; re-shoot `c03_chair_back_rear`.
5. **Low (5, 6).** Fix the §14.1 "ivory" wording (or warm `Prop_FrostedGlass` if Red wants Sears' ivory); note the 20 m pop or give LOD2's outer bowl ring two more sides.
6. **Engineering notes 3 and 4.** Run main's `FrontRoomsRelayNavTest` on seeds 4242, 20261005, 20261008 and 20261010 once **without** W2 (swap `W/w2_props/payload_base` in, run, swap back) and record both pass-through counts (with W2: 3 / 0 / 18 / 1). Re-run the map suites after the clone is re-synced to current main, after the frames are shot (so the frames stay comparable).
7. Rebuild the package `W/apply/office_props_w2` from the fix-3 payload (bases from main at that moment), refresh `threeview/`, `30_for_graphic.md`, `31_for_map.md` and `W/vl_pending/w2_office_props.md` (VL308–VL311 only).

**Package name.** The package stage's task named `W/apply/w2_office_props.sh`, but every existing file, this report and `LANDINGS.md` use `office_props_w2` (`.sh`, `_bases.txt`, `_expected.txt`, `_keep.txt`, `_guids.txt`, `_payload/`, `_base/`, `.diff`, `_rendersetup.diff`). Keep `office_props_w2` unless the coordinator renames all of it.

### 15.4 Engineering lens (READY): its five notes

1. **No LANDINGS claim for the fix-round doc writes.** Closed by this stage: a CLAIM and a LANDED line in `LANDINGS.md` (14:14 and the LANDED after this edit) record that fix rounds 1 and 2 changed `10_spec.md`, this report, the `images/2*.jpg` composites and `threeview/**` without a claim, and that `images/21_torch_fix1_before_after.jpg` moved to `W/_partial/w2_images_fix1_134711/`. The sha1 of every file under `w2_props/` at 14:20 is in `W/w2_props/fix3/main_docs_manifest_1420.txt`.
2. **Dead image links in §12.1 and §12.3.** Fixed in this edit (they point to the `_partial` copy and to `images/21_torch_before_after.jpg`).
3. **No no-W2 baseline for the Relay pass-throughs** (18 on 20261008). Low severity, the verdict is PASS either way; goes into fix round 3 (§15.3, step 6).
4. **Suites and playtests ran on main `0ea38bb` + the 07:59 working tree,** not `2c450e11` (later changes: a warn-lamp check in `FrontRoomsMapInteractionTests.cs`, `FrontRoomsWarnLamps.cs`, `FrontRooms3DGame.cs`, `FrontRoomsExitSign.cs`, the touch layer; none is a package base or reads kit tags). Goes into fix round 3 (step 6).
5. **Load note.** Fixed in the header (runs 121–123: load 17–31, one 123 run UNDER LOAD).

### 15.5 What the VL specs say now

`W/vl_pending/w2_office_props.md` has a HOLD banner at the top (14:20): VL308–VL311 must not be placed until fix round 3 rewrites them. VL308's "holds up at 0.3 m", its cup and pole highlight figures and VL309's "2 cm of chrome shows" are refuted (§15.1, §0b).
