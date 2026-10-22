# 40 — Outlets: fix pass, final package and hand-off

**DECIDED 2026-10-21 (Red, "全部按推荐" in the 游戏视觉 chat): ivory = D (`#A69F8C` plate / `#A39C89` device, 0.95x the paper); P-1 LODs approved (`outlets_lods`).** `outlets_int` landed 10-21 21:21. D reaches the screen through the P-4b slots, so the P-4b/P-6 RenderSetup package is built next with the D hex.

**2026-10-22 07:0x: 关卡设计 landed contract C-1 (MapWorld 7ef0ca45) after re-running the probe (republished 18:10 version): pass, 10 seeds identical to `data/60_probe10_p1810_tonight.json`. Outlets are now in the maze. C-2 (palette filter) landed 10-21. C-3 (ceiling placement) is the map's next item.**

Status: **READY, 2026-10-14 18:1x.** The outlets workflow's fix stage checked the art and engineering critics' issues and fixed every real one. It then rebuilt the landing package and wrote the hand-offs. The package dry-runs clean on today's main (`0a9ee7f` + working tree), with all 90 bases matching:

```
bash /Users/redwang/FrontRoomsVisualWork/apply/outlets_int.sh            # dry run: DRY RUN OK
bash /Users/redwang/FrontRoomsVisualWork/apply/outlets_int.sh --apply    # writes 90 paths; backs main's 4 replaced files up first
```

- **It replaces** the 16:24 `outlets_int` (kept in `W/outlet_fix/pkg_old/`) and `outlets_c1`. Do not run either of those.
- **After it lands:**
  - the map chat applies the contract in `30_contract_map.md`;
  - 平面视觉 places the K-sheets from `threeview/`.
- **Waiting for approval or Red:**
  - P-1 (`outlets_lods`, rebuilt today);
  - P-4b + P-6 (the RenderSetup proposal, v2 today);
  - the ivory tint (VL277).

Written by the outlets workflow's fix stage. `W` = `/Users/redwang/FrontRoomsVisualWork`. All work ran in the two existing outlet clones; Unity was never opened on Frontrooms3D. In Red's project only `Documentation/` changed:
- this folder: `30_contract_map.md`, `40_final.md`, `images/40_*`, `data/30_*`, `data/40_*`, `threeview/`;
- the VL274–280 rows in `Documentation/VERIFICATION_LOG.md`.

**Load rule.** Every millisecond carries the 1-minute load average. Above 32 it is marked *under load, re-measure*; counts do not depend on load.

---

## 0. Results in one table

| Check | Result | Load (1 min) |
|---|---|---|
| Machine | Blender 4.3 and Unity 6000.3.10f1 batch both ran (17 Unity runs, 0 crashes) | 28–193 |
| Package `outlets_int` on main `0a9ee7f` + tree | **DRY RUN OK**, 90 / 90 bases. `rebase_apply.sh --no-install`: PASS. Scratch `--apply`: MANIFEST OK, then ALREADY APPLIED | — |
| Compile | Roslyn, main + payload: **0 errors** with the macOS and with the iOS defines. Unity batch compile of the landing clone: 0 errors | — |
| Engineering critic harness (its own checks, patched to test R3's real culling box), 25 seeds | **Conduit gaps 0** of 27,264 tile views (was 4,120 of 36,481). Wall-row desks powered 25 / 29, all 25 on the desk's side of every wall (was 25 with 2 through a wall). Keep-out hits 0. Order test 0 / 8 | counts |
| Contract probe v5, 10 seeds (clean contract) | **PASS.** 250 chunks, 31,890 faces, 8,835 fixtures; every rule count 0; keep-out hits 0; Tall lines with more than one handy box 0; pods fed 46 / 46 | counts |
| Contract probe v5, 100 seeds | see §3.2 | counts |
| Map suites, outlets on (landing clone) | interaction 152/152, Level Designer 139/139, map verification 100/100, Relay nav 60/60, fixture ticks 28/28, dress step 36 ok | counts |
| Room stream suite + Exit check | 5 / 5 passed; Exit check **PASS**: 5 / 5 variants hold one EXIT sign and no threshold marker | — |
| T-R2 capture (22 shots) | 22 / 22 draw outlets, 0 GameObjects added by rendering | — |
| Autopilot 20388, outlets on | PASS, 58.0 fps, p99 26.6 ms, max colliders 396 (same as off on 10-14) | 66 → 47: *under load, re-measure* |
| Autopilot 2554, outlets on | PASS, 58.2 fps, p99 26.1 ms, max colliders 407 (same as on 10-14) | 47 → 93: *under load, re-measure* |
| Install per chunk (probe) | median 0.52 ms; *under load, re-measure* (the valid figure is still 0.149 ms, C1 at load 12–15) | 71 → 100 |
| Renders | 7 new verification slides, VL274–280, 10 images | images only |
| Three-views | 20 kits × front / side / top / hero, `index.json` | — |

---

## 1. The critics' issues: verified, fixed or rejected

The harness reported "critic returned nothing", but both critics' full reports were in the stage input, so every item below comes from those texts. Each was checked in a clone first.

### 1.1 Art critic

| # | Issue | Verified? | What was done | Evidence |
|---|---|---|---|---|
| 1 | Papered or painted plates show swirling paper | **CONFIRMED**. At 192 m it is still a scramble: about 3.4 m of paper shift per degree of tilt | Fixed: shader `_FR_PlanarSnap` + R3 property block (§2.2) | VL275 |
| 2 | Metal reads gunmetal or black | **CONFIRMED** (steel plate 0.58–0.59× the wall) | Fixed today with `Prop_AluminiumAnodised` (0.80×). Better still with the P-4b proposal slots (0.92×) | VL276 |
| 3 | Both tint options wrong | **CONFIRMED** | Rendered the critic's two options C and D: 1.05× and 0.95× the paper; neither turns olive. **WAIT-RED** | VL277 |
| 4 | Close-ups spoiled by float precision at 4992 m | **CONFIRMED** (PLAUSIBLE → confirmed) | VL231 re-shot at root 192 m: screws and rims are clean | VL278 |
| 5 | One conduit riser per handy box in Tall halls | **CONFIRMED** (1,027 boxes in 100 seeds) | Fixed: one handy box and riser per wall line; the rest are flush steel plates | probe, VL280 |
| 6 | Ground-up per plate; companions forced ivory | **CONFIRMED** | Fixed: ground-up per wall line (15 %) plus 2.5 % single plates; jack plates follow the line (ivory on steel lines) | probe: 89 of 4,094 lines mixed |
| 7 | Switches never wear | **CONFIRMED** | Fixed: finish, crooked, not flush and missing screw; aged more often than receptacles; papered or painted over 5 / 12 %. No geometry variant | — |
| 8 | Steel lines draw domed plastic jacks, switches and blanks in metal | **CONFIRMED** | Fixed: jack and switch plates are ivory on steel lines; a Blank never rolls on a steel line | — |
| 9 | Pod floor feeds stand alone | **CONFIRMED** | Fixed: one feed per pod, 0.05 m outside the end post of the powered spine panel. A flex whip into the panel base is a P3 kit idea | VL280, probe 46 / 46 pods fed |
| 10 | Brown on brown never shown lit | **CONFIRMED** (VL231's tile had a white device) | Shot lit: brown plate + brown device, 0.38× the paper | VL280 |
| 11 | Toggle bat reads black | **REJECTED as a material or AO defect.** Bat luma is 119 at bat height and 160 from eye height, with the plate at 157; the critic's 27 was at 4992 m. The dark face is the bat's down-facing side under the game's SSAO (intensity 1.6, radius 0.45 m, direct 0.35). The Surface shader has no vertex AO and the device mask is white | No change. SSAO tuning is a game-wide setting, not an outlet item | VL280 |
| 12a | LOD pop can't be judged from the dolly | **CONFIRMED** | Same-camera pairs with `R.ForceLod`: mean luma moves 1.1 % at 1.5 m and 0.1 % at 4 m | VL279 |
| 12b | Stage-2 captures nearly black | **CONFIRMED** | Lit Office pair: the JackData behind the copier is hidden; 7 px differ | VL280 |

### 1.2 Engineering critic

| # | Issue | Verified? | What was done | Evidence |
|---|---|---|---|---|
| 1 | Conduit vanishes (0.4 m culling box at the tile foot) | **CONFIRMED** | Fixed: every draw's culling box is the submesh's own bounds through its matrix, in both draw paths | 0 / 27,264; landing and instanced renders differ by at most 7/255 (VL274) |
| 2 | Desks powered through a wall | **CONFIRMED** | Fixed: `EnsureReceptacleNear`'s first loop runs the same `ThroughWall` test; the probe now measures "same side" | critic B: 25 / 25 same side |
| 3 | Landing test clone not exactly main + package; the powered panel's 4th slot | **CONFIRMED** (clone metas pointed at P-4b materials) | The landing clone now takes the payload's FBX metas. Pod check: the panel's `Prop_NylonIvory` slot imports the FBX's preview material (URP/Lit, ivory), as every missing P-4 slot does in this project. The 10-14 report's "PlasticPutty fallback" was true only for R3-drawn kits | 25_integration.md §3.1 corrected here |
| 4 | Switches cover the cue band | **CONFIRMED** (1.163–1.277 m against 1.184–1.391 m) | Level 0 switches (printed walls) now 1.12 m, plate top 1.177 m; Office keeps 1.22 m. **Needs a call** from 平面视觉 and the cue owner (30_contract_map.md §7.2) | — |
| 5 | The probe doesn't compile with the clean contract; old suite sizes; `WallEdgeId` | **CONFIRMED** | Fixed: probe v5 reads `EdgeId` by reflection, and `Install` times itself; the contract drops `WallEdgeId`; `30_contract_map.md` lists today's suite sizes | probe PASS on the clean contract |
| 6 | Some fixture kinds skip keep-outs | **CONFIRMED** (no hits in the critic's 25 seeds; the new 100-seed probe found 1, seed 3015: an own plate in the next chunk's Relay entry, behind its wall) | Fixed: columns, floor boxes, lone switch and moved plates test keep-outs. Wall-mounted fixtures answer only to keep-outs in front of their wall (a keep-out is a floor-plan rect). A plate in another chunk's keep-out is hidden at draw time, foreign or own. The probe counts keep-out hits with the same rule | 0 hits, 10 + 100 + 25 seeds |
| 7 | Two unused kits ship; outlets in the Level Designer palette | **CONFIRMED** | Fixed: Brown and IG dropped from the package (20 kits). Palette: contract request C-2 (map-owned file) | — |
| 8a | "Scorched duplex" mentioned but missing | **CONFIRMED** | Documentation: it draws as a plain duplex (P3 kit) | — |
| 8b | Culling past the brief; conduit outlives its box | **CONFIRMED** | Fixed: plates 12 m; floor boxes and handy box with riser 15 m (a sidecar may ask for less, never more); attachments never past their box | — |
| 8c | Landing not instanced until P-6 | **CONFIRMED** (by design) | Unchanged; P-6 is in the proposal | — |
| 8d | WebGL gate ignores `FRONTROOMS_WEBGL_PREVIEW` | **CONFIRMED** | Fixed: `(UNITY_WEBGL && !UNITY_EDITOR) \|\| FRONTROOMS_WEBGL_PREVIEW`, as the placard mount. **WebGL itself is still unbuilt and unmeasured** | — |
| 8e | `inspect_data` and `outlets_lods` both replace kitlib | **CONFIRMED** | Noted in `W/apply/README.md`: whichever lands second runs `rebase_apply.sh` | — |

---

## 2. What changed (diff: `data/40_int_to_fix.diff.txt`, against the 10-14 integrate payload)

### 2.1 Renderer (`FrontRoomsWallFixtureRenderer.cs`)

- **Culling boxes.** Each bucket keeps its submesh's local bounds. `Append` unions each instance's world AABB, and single draws get their own AABB. The pad is 0.01 m of float slack. `DrawBounds(mesh, submesh, matrix)` is public for tests.
- **Cull distances.**
  - Plates: 12 m (the brief).
  - Floor boxes and the handy box: 15 m.
  - Conduit tiles and straps are culled with their box (`min(own, parent)`).
- **Metals.**
  - The `Prop_Aluminium` slot draws `Stainless()` on plates and `Galvanized()` on the handy box, EMT, straps and floor boxes.
  - Both prefer the P-4b proposal slots and fall back to the existing `Prop_AluminiumAnodised` (the placard frame's clean metal).
- **Wall match.**
  - Papered and painted plates draw the **live** wall material, so they follow any change to it. They take one of two static property blocks:
    - papered: printed walls (`_FR_PRINT`); `_FR_PlanarSnap = 1`;
    - painted: any other wall; also `_BumpScale = 0`, so the plate is smooth.
  - Their screws keep an ivory finish (put back after the job).
  - No material copies and no per-chunk allocation.
- **Tool flags** (tests and captures only):
  - `ForceLod` (−1 = by distance);
  - `FixPassLook` (false = the 10-14 look, for before/after shots).

### 2.2 Shader (`Assets/Resources/Rendering/FrontRoomsSurface.shader`)

- **One new property, `_FR_PlanarSnap`** (hidden, default 0).
  - It sits in the UnityPerMaterial CBUFFER (all passes share the layout).
  - When it is 1, the world-planar UVs and the macro-wear frame are built from the normal's dominant world axis. Lighting and the TBN keep the real normal.
- **No material sets it.** Only R3's per-draw block does, so every existing surface renders exactly as before.
- **Cost:** one uniform branch.
- The `.meta` is kept. The shader compiled in the Unity batch compile and in every capture run.

### 2.3 Planner (`FrontRoomsWallFixtures.cs`, rules version 2)

**Rule changes (§1):**
- one handy box per Tall wall line (`OneHandyBoxPerLine`);
- `GroundUp` per line;
- `WearSwitch`;
- jack plates follow the line;
- `SwitchHeightPrinted` = 1.12 m;
- no thermoset Blank on steel;
- `BlockedBox` keep-out test for every kind, with `InFront`: wall-mounted fixtures ignore keep-outs behind their wall;
- plates hidden in another set's keep-outs, own and foreign (`IsOccludedBy`, `MeetsKeepOut`; `OccluderWorldBounds` and R3's `RefreshOcclusion` now include keep-outs);
- stage 2 counts only same-side sources.

**Tool accessors:** `LineKey`, `ThroughAnyWall`, `Powers`, `PowerPoint`, `InstallTicks` / `InstallCalls` / `InstallMs`, `StatHandyFlattened`.

The four contract functions and the `WallFace` struct are unchanged.

### 2.4 Set (`FrontRoomsWallFixtureSet.cs`)

`OnEnable` and `OnDisable` also bump `OccluderVersion` when the set has keep-outs.

### 2.5 Office kit (`FrontRoomsOfficeKit.cs`)

`AddPodFeeds` places one feed at the powered end (column 0, −halfW side), 0.05 m outside the post, facing the panel. It uses no rng, so the layout is unchanged.

### 2.6 Kits

20 kits ship; `Kit_OutletDuplex_Brown` and `Kit_OutletDuplex20_IG` no longer do. Every remaining FBX, sidecar and `.meta` is byte-identical to the 10-14 payload. **No Blender module changed in this pass.** The modules that built the kits already live in `Tools/Blender/frontrooms_kit/assets/outlet_*.py` (23 files) and are committed in main.

---

## 3. Numbers

### 3.1 Probe, 10 seeds (landing clone, 17:34, before the InFront rule; the fix-pass 18:13 run is now `data/40_probe10.json`, corrected 2026-10-22, `60_keepout_seed2.md`)

| | 10-14 integrate | Fix pass |
|---|---|---|
| Fixtures | 8,881 | **8,835** (−46: one feed per pod) |
| Max per chunk | 62 | 61 |
| Handy boxes | — | 60, and 0 lines with two |
| Keep-out hits | not measured | **0** |
| Wall-row desks powered, same side / any side | 13 / 17 (any side only) | 13 / 13 of 17 |
| Pods with a feed | — | 46 / 46 |
| Colliders and renderers added (same build, on vs off) | 0 / 0 | 0 / 0 |
| Install per chunk | 0.153–0.197 ms (load 34–51) | 0.52 ms (load 71–100): *under load, re-measure* |

### 3.2 Probe, 100 seeds

PROBE100_PLACEHOLDER

### 3.3 Cost per view and per chunk

Rendering cost per drawn fixture is unchanged. The culling box is now exact, which removes false culls but adds no draws.

| | Per chunk | Per view |
|---|---|---|
| What lands (LOD0 only, no instancing) | about 35 fixtures, 0 renderers, 0 colliders, 0 GameObjects | the 10-14 figures: 34.6 draws and 25.1 k tris (mean) |
| With P-1 + P-6 | same | 13.3 draws and 1.1 k tris |

**In play** (autopilot):
- draws: 0.3 per frame on average, max 41–48;
- triangles: 196–218 per frame;
- R3 submit: at most 0.95–1.25 ms in one frame, under load.

### 3.4 Metal and tint, measured (`data/40_fixlog_landing.txt`, `data/40_fixlog_full.txt`)

Plate luma ÷ wall luma at 0.30 m unless noted; seed 2554, root 192 m.

| What | Before | Landing (anodised) | P-4b proposal |
|---|---|---|---|
| Stainless duplex, Office, 0.30 m | 0.59 | 0.80 | 0.92 |
| Stainless duplex, 4 m | 0.67 | 0.77 | 0.92 |
| Handy box + EMT, 0.42 m | 0.66 | 0.71 | 0.97 |
| Floor box sides vs carpet (luma) | 63 vs 62 | 72 vs 62 | 112 vs 74 |

| Ivory option (plate / device) | 0.30 m | Eye height, 1.6 m out |
|---|---|---|
| A `#E6DDC2` / `#E2D9BF` (proposal) | 1.31 | 1.28 |
| B `#9E9147` / `#E2D9BF` (luma fit) | 0.85 | 0.86 |
| C `#B6AF99` / `#B3AB97` | **1.05** | 1.04 |
| D `#A69F8C` / `#A39C89` | **0.95** | 0.95 |
| Photo DSC00161 | 0.88 | — |

---

## 4. Verification log (Figma section `2595:6093`, rows recorded in `VERIFICATION_LOG.md`)

| VL | Node | Check | Verdict | Images |
|---|---|---|---|---|
| VL274 | `3153:7042` | Conduit stays on its box | PASS | `images/40_conduit_attached.jpg` |
| VL275 | `3153:7057` | Papered plates line up | PASS | `40_wallmatch_papered.jpg`, `40_wallmatch_painted.jpg` |
| VL276 | `3153:7067` | Metal reads in game light | PASS | `40_metal_p4b.jpg`, `40_metal_landing.jpg` |
| VL277 | `3153:7077` | Ivory tint: two new options | **WAIT-RED** | `40_tint_options.jpg` |
| VL278 | `3153:7086` | Close-ups near the origin | PASS | `40_close_sheet_root192.jpg`, `40_precision_4992_vs_192.jpg` |
| VL279 | `3153:7096` | LOD switch, same camera | PASS | `40_lod_same_camera.jpg` |
| VL280 | `3153:7105` | Brown, toggle, pod, stage 2 | PASS | `40_misc_checks.jpg` |

- The section grew to 71 rows (8,280 × 88,120) after a bounds check: nothing within 400 px below.
- The cover now reads `Oct 2–14, 2026 · 27 tasks · 279 checks · 743 images · index`; N3 = `VL064–280 · 25 checks`.
- **VL231 and VL235 are superseded in substance** by VL278 and VL277. Their slides stay, as the log's rules require.

---

## 5. Approvals and decisions still open

1. **P-1, LODs in the FBX.**
   - Package: `bash W/apply/outlets_lods.sh`. Rebuilt today: 38 paths, i.e. `kitlib.py`, `build_asset.py` and 18 kits.
   - It is stacked on `outlets_int`, so it refuses on main until that lands. On a scratch copy with the fix-pass `outlets_int` applied: DRY RUN OK, MANIFEST OK, then ALREADY APPLIED.
   - **NEEDS APPROVAL** (visual chat).
   - `inspect_data` also replaces `kitlib.py`, so whichever lands second runs `rebase_apply.sh`.
2. **P-4b + P-6, RenderSetup.**
   - Proposal v2: `data/40_rendersetup_patch.py.txt`, diff `data/40_rendersetup_p4b_p6.0a9ee7f.diff.txt`.
   - It defines seven slots. Five are as before (ThermosetIvory/Aged/Almond, NylonIvory, PlasticOrange). Two are new: `Prop_StainlessBrushed` (metallic .6, smooth .55, `#C7C7C2`) and `Prop_Galvanized` (metallic .5, smooth .35, `#B3B5B3`).
   - It turns instancing on for 10 existing slots (P-6), now including `Prop_AluminiumAnodised`.
   - **NEEDS APPROVAL**, and the ivory waits on Red (VL277).
   - Not packaged on purpose: the ivory hex changes with Red's pick.
3. **Ivory tint (Red):** A, C or D.
   - The critic's reading: C or D are closest to the photo without the olive cast.
   - Office can keep A, because L-1 calibrates Level 0 only.
4. **Switch height on printed walls:** 1.12 m today. 平面视觉 and the cue owner confirm it, or name another height (`SwitchHeightPrinted`).
5. **Map chat:**
   - C-1 is the hook, required for the map to draw outlets.
   - C-2 is the palette filter, recommended.
   - C-3 is pin/forbid, later.
   - See `30_contract_map.md`.

---

## 6. Open items (smaller)

1. **Screws at LOD1:** O1 kits show a dark hole, O2 kits a screw-head stand-in. Pick one rule before P-1 lands (unchanged since O2).
2. **Estimates without photos:** the 5-20R arm, the IBM data connector face and the 4-prong hole pattern (media leads om07 and om08 in `media_candidates.md`; nothing downloaded).
3. **The Bare kit's gap** reads dark brown at 0.3 m. A real hole needs a map-wall cut (map item).
4. **Kit sidecars say `"placement": "Floor"`** (kitlib's default). C-2 hides them from the palette; a `wall_flush` placement in kitlib would need approval.
5. **Floor boxes have no collider** (spec choice). A camera can pass through a 76–86 mm box on open floor.
6. **WebGL:** gated correctly now, but never built or measured.
7. **Frame time** in a windowed Play mode is still unmeasured. Every autopilot today ran under load.
8. **SSAO** (intensity 1.6, radius 0.45 m) darkens small protrusions such as toggle bats and screw rims. This is a game-wide look-dev question for the visual chat, not an outlet item.
9. **9 of 118 wall-row desks** have no receptacle within 2 m in the 100-seed probe. They sit on border walls the stage-2 rule may not use: foreign faces, or no solid wall in reach.
10. **A flex whip** from the pod feed into the panel base would read better than a feed beside the post (P3 kit idea).

---

## 7. Promotion list (relative to `Frontrooms3D/`; `outlets_int.sh` writes exactly these 90 paths, `.meta` first)

**New (82):**
- `Assets/Scripts/Office/FrontRoomsWallFixtures.cs` + `.meta` (GUID `14ab7ea1…`)
- `Assets/Scripts/Office/FrontRoomsWallFixtureSet.cs` + `.meta` (`6f896ecc…`)
- `Assets/Scripts/Office/FrontRoomsWallFixtureRenderer.cs` + `.meta` (`e2a60678…`)
- `Assets/Resources/Props/Models/<Kit>.fbx`, `.fbx.meta`, `.json`, `.json.meta` for these 20 kits:
  - receptacles: `Kit_OutletDuplex`, `Kit_OutletDuplex_Steel`, `Kit_OutletDuplex20`, `Kit_OutletDuplex_Cracked`, `Kit_OutletDuplex_Jumbo`, `Kit_OutletBare`, `Kit_OutletBlank`;
  - screw and switches: `Kit_OutletScrew`, `Kit_OutletToggle1`, `Kit_OutletToggle2`;
  - jacks: `Kit_JackPhone`, `Kit_JackData`, `Kit_JackBNC`, `Kit_JackPhone4Prong`;
  - surface and floor: `Kit_OutletHandyBox`, `Kit_ConduitEMT`, `Kit_ConduitStrap`, `Kit_FloorBoxTombstone`, `Kit_FloorBoxTombstone2`, `Kit_CubiclePanel_Powered`.

**Replaced (4; main's `.meta` kept):**
- `Assets/Resources/Rendering/FrontRoomsSurface.shader` (`.meta` `90befc31…` kept)
- `Assets/Scripts/Office/FrontRoomsOfficeKit.cs` (`.meta` `ae758f35…` kept)
- `Assets/Scripts/FrontRoomsRoomStream.cs` (`.meta` `629abdbd…` kept)
- `Tools/Blender/frontrooms_kit/assets/outlet_common.py`

**Kept, never written:** `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs` (the contract's base, sha1 `11586bbc`). The full list with sha1s is in `W/apply/outlets_int_expected.txt`.

**New Blender modules (already live in `Tools/`, committed):**
- `Tools/Blender/frontrooms_kit/assets/outlet_bare.py`, `outlet_blank.py`, `outlet_box_common.py`, `outlet_common.py`, `outlet_conduit.py`, `outlet_conduit_strap.py`, `outlet_duplex.py`, `outlet_duplex20.py`, `outlet_duplex_cracked.py`, `outlet_duplex_jumbo.py`, `outlet_duplex_steel.py`, `outlet_floorbox.py`, `outlet_floorbox2.py`, `outlet_handy_box.py`, `outlet_jack_bnc.py`, `outlet_jack_data.py`, `outlet_jack_phone.py`, `outlet_jack_phone4.py`, `outlet_panel_powered.py`, `outlet_plate_common.py`, `outlet_screw.py`, `outlet_toggle1.py`, `outlet_toggle2.py`.

**Not in the package:**
- the map contract (`30_contract_map.md`);
- P-1 (`outlets_lods`);
- the P-4b/P-6 proposal;
- the clone-only tests (`Assets/Editor/OutletCensus/*`; copies in `data/`).

---

## 8. What Red will see after `--apply`

- **Title corridor:**
  - Lobby: steel duplexes in light brushed metal, not black, with ivory phone plates beside some.
  - Shift and Exit: ivory plates.
  - Office: plates, jacks, switches and one floor feed per pod beside the powered panel.
  - Run: white plates.
  - No colliders and no shadows. Exit rooms keep their single EXIT sign.
- **Close up:** a papered-over plate continues the wallpaper's print; an Office painted-over plate is smooth.
- **Ivory until P-4b:** the ivory reads as the fallback tan (`Prop_PlasticBeige` plate, `Prop_PlasticPutty` device).
- **The map:** nothing until 关卡设计 applies C-1. After that, every zone has outlets, Tall halls have one surface box and riser per wall line, and switches beside arches sit at 1.12 m (Level 0) and 1.22 m (Office).
- **Cost:** about 25 k extra triangles per view until P-1; 1.1 k with P-1.

---

## 9. Clones and work folders (nothing deleted)

| Path | What | Can it go? |
|---|---|---|
| `W/proj_outlet_int_t` | landing clone: main + fix payload + clean contract + tests | after `outlets_int` lands and the map contract is checked |
| `W/proj_outlet_int` | full-state clone: + P-1 + P-4b v2, three-view renderer | after the P-1 / P-4b / tint decisions |
| `W/proj_outlet` | old C1 + O1–O3 build clone | **yes, now** |
| `W/proj_outlet_crit_eng` | the engineering critic's copy | **yes, now** |
| `W/outlet_int2/roslyn*`, `W/outlet_c1r3` | Roslyn scratch, an empty folder | **yes, now** |
| `W/outlet_fix/roslyn_*`, `W/outlet_fix/applytest*`, `W/outlet_fix/compose` | this stage's scratch | **yes, now** |
| `W/critic_outlets`, `W/critic_outlets_eng` | critic crops; their "before" frames are copied into `images/40_*` | **yes, now** |
| `W/outlet_fix` (rest) | stage overlay, specs, scripts, captures, `pkg_old/` | keep until `outlets_int` lands |

This stage made **no new clone**.

---

## 10. Files

In `Documentation/research/outlets/`:

- **Reports:**
  - `40_final.md` (this file);
  - `30_contract_map.md` (the map request);
  - `threeview/README.md` + `threeview/index.json` + 80 PNGs + `handoff_manifest.json` (for 平面视觉).
- **Images:** `images/40_*.jpg`, 10 sheets (JPG q85, ≤ 1920 px).
- **Data (`data/`):**
  - code copies: `40_FrontRoomsWallFixtures.cs.txt`, `…Set.cs.txt`, `…Renderer.cs.txt`;
  - the diff: `40_int_to_fix.diff.txt`;
  - the contract: `30_contract_patch.py.txt`, `30_contract.0a9ee7f.diff.txt`, `30_FrontRoomsOutletContractProbe.cs.txt`;
  - the P-4b proposal: `40_rendersetup_patch.py.txt`, `40_rendersetup_p4b_p6.0a9ee7f.diff.txt`;
  - harnesses: `40_FrontRoomsOutletIntegrateCapture.cs.txt`, `40_OutletEngCritic.cs.txt`;
  - results: `40_probe10.json`, `40_probe100.json`, `40_critic_harness_25seeds.txt`, `40_fixlog_*.txt`, `40_exit_check.txt`, `40_suites.txt`, `40_timeline_with_load.txt`;
  - build scripts: `40_spec_int.json`, `40_spec_lods.json`, `40_mkpkg.py.txt`, `40_sheets.py.txt`, `40_build_threeview_index.py.txt`, `40_resync.sh.txt`.
- **Packages (`W/apply/`):**
  - `outlets_int.sh` + `_bases.txt` / `_expected.txt` / `_keep.txt` / `_payload/` / `_base/` / `.diff` (READY);
  - `outlets_lods*` (stacked, NEEDS APPROVAL);
  - the `README.md` rows updated.
