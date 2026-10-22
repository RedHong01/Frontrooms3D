# Level transitions · production · 20 · Build

Date: 2026-10-22. Stage "Build" of the workflow `level-transitions-prod` (游戏视觉). The build ran on account B 13:29–14:51 and died without a report; this stage (account A, session local_5656cffd, 15:2x–) checked what it left, finished it and re-checked it on today's main. Plan: `10_plan.md`. Map contract: `CONTRACT_map.diff` + `CONTRACT_map_README.md`. VL rows: `W/vl_pending/level_transitions.md` (VL365–VL370). Images: `images/`.

**Decided by Red, 2026-10-21, "全部按推荐":** a Frame on every zone border (the Office owns every frame and base), Renovation on about 3 in 10 crossings, the colour stays V5. **Not decided, not touched:** Close, the light rule, the ~60 MB media download (nothing was downloaded).

---

## 0. Short answer

- **Built:** the Frame kit and the Renovation kit as four partial-class files merged into main's `FrontRoomsTransitionKit.cs` (GUID kept, V5 / B0 / `CornerReach` byte for byte), 20 materials, 30 textures, 4 new work props (Blender → FBX + sidecars, three-views), the asset/stats tool, the kit tests, and the map hunks as a contract.
- **The visual package is inert until the map hunks land** and compiles on today's main (C22 + V1 included). It is `W/apply/level_transitions.sh`, dry run OK at 15:45 (136 paths: 133 new, 3 replaced, 3 kept .meta, 64 new GUIDs unused).
- **The map contract is re-cut onto today's main after C22** (MapWorld `83c19911`), with C22's exclusions wired in (`EffectiveEdge`, `TransitionExcluded`); `git apply --check` clean; Roslyn 0 errors.
- **Tests:** kit tests 21/21; map interaction 190/190 (with the 12 transition checks); Relay nav 60/60; door shot 234/0; seeds 100/100; Level Designer 140/0; touch 85/0; autopilot 2554, 20388, doors PASS; transition play check 58 crossings with a player capsule, 0 blocked. On today's main: §5.2.
- **Numbers:** Frame meets every budget. **Renovation on desktop at hero detail is over the plan's own budget** (+169.5 k triangles mean at spawn against 110 k; kit renderers in the frustum up to 94 against 60; mesh +18.5–21.2 MB against 12). Frame time could not be resolved under today's load. This is the one Red call (§9).
- **Frames:** VL365–VL370 are made in the Shot format (§7).

---

## 1. Clones and bases

| Clone | What | Used for |
|---|---|---|
| `W/proj_lt` | `mkclone.sh` 13:29 from main `2c450e11` + `git apply` of A12 r2 (`50ec80b2`); Roslyn `--all --ios` 0 errors (13:30) | the build, every render, stats, frame time |
| `W/proj_lt_suite` | APFS copy of `proj_lt` at 14:19 | the map suites (account B) |
| `W/proj_lt2` | main `a50ee7da` **before C22** (A12 r1 + fixes, M2, GD3c, WarnBurst, C10) + the package + the contract cut onto MapWorld `9b82c286` | kit tests 21/21 and the fixed shots on A12 as landed (§5.2) |
| `W/proj_lt3` | main **after C22 + V1** (MapWorld `83c19911`, Game `670219ab`) + the package only | **the landing state** of the visual package (§5.3) |
| `W/proj_lt4` | `proj_lt3` + the contract re-cut after C22 | the full chain on today's main (§5.2) |

Main moved four times while this stage ran (A12 15:11, GD3c 15:22, C22 15:39, V1 lens hook 15:40). The visual package's three replaced bases did not move: `FrontRoomsTransitionKit.cs` `6ef32ddd`, `FrontRoomsWallFixtures.cs` `b5d05f9e`, `FrontRoomsRenderSetup.cs` `256cf3a4`.

Harness: `FrontRoomsTransitionAudit.cs` md5 `76a624e3217630e637bdb381af7ca5b1` (G0). Clone-only tools written by this workflow (never shipped; copies in `W/lt_work/tools/`): `FrontRoomsTransitionLookdev.cs`, `FrontRoomsTransitionPlayCheck.cs`, `FrontRoomsTransitionFrameTime.cs`.

---

## 2. Files

### 2.1 Visual (the package `W/apply/level_transitions.sh`; visual-owned paths only)

| Path | sha1 | What |
|---|---|---|
| `Assets/Scripts/Rendering/Transitions/FrontRoomsTransitionKit.cs` | `73008059` | **merged, not replaced:** `static partial class`; V5, B0, `HasArg`, `PostPiece`, `CornerReach` unchanged; adds `Kits`, `FrameOnly`, `ShareOverride`, `BareRenovatedOpen`, `ReducedTier`/`Reduced` (`#if UNITY_WEBGL && !UNITY_EDITOR`, or `FrontRoomsHandheld.Active` in players), `RenovationShare` |
| `…/FrontRoomsTransitionKit.Chunk.cs` (new) | `e562da40` | `View`, `BeginChunk`, `ChunkBuild` (`TakesCell`, `Open`, `Edge`, `Piece`, `Corner`, `Finish`): the T4/T5 entry points; one renderer per chunk × material × shadow flag, **no MaterialPropertyBlock** |
| `…/FrontRoomsTransitionKit.Frame.cs` (new) | `bb73dae5` | portal (pilasters, header, beads, wall angle, satin bar; arcades with a 0.24 shared pilaster), cased arch, **stopless DA steel sleeve** to D1.6's `Kit_DoorFrame_Steel_DA` envelope + saddle, end caps, corner guards, free posts, 0.10 base on every Office face; the unfinished-portal variant |
| `…/FrontRoomsTransitionKit.Renovation.cs` (new) | `fd62e59b` | the 3-drop band (fresh / primer / stripped), torn edges with lip and curl, remnants, islands, hanging strips, halos, bare reveals, slab strip, ceiling hole (never the troffer slot), props with the A12 sweep rule; **no own corner rule** (plan fix 4) |
| `…/FrontRoomsTransitionKit.Mesh.cs` (new) | `c08d7d9d` | `Sweep`, `EarClip`, profiles, `TransitionMesh` with the non-finite guard |
| `Assets/Scripts/Office/FrontRoomsWallFixtures.cs` | `ba8830b5` | `PendingKeepOuts` (the renovation props' keep-outs, read once by the next `Install`); empty without the map hunks |
| `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs` | `53fe2f89` | 20 SurfaceDef rows; `EnsureTransitionSurfaces`; the per-material body moved into `EnsureSurface` (same effect); `Paper_Scrap`/`Paper_Peel` = `LobbyPrint(...)`, today's L0 print |
| `Assets/Editor/Rendering/FrontRoomsTransitionAssets.cs` (new) | `8515a5a4` | `Prepare`, `SetMode`, `Stats`, `SeedStats`, the texture report |
| `Assets/Editor/Rendering/FrontRoomsTransitionKitTests.cs` (new) | `8d7cc7f6` | 21 checks (§5.1); **15:4x:** without the map hooks it reports the map-built checks as skipped instead of failing |
| `Assets/Resources/Surfaces/*.mat` (20) | — | `Trans_AluminiumSatin` + 19 renovation surfaces |
| `Assets/Resources/Surfaces/Textures/*_{A,N,S}.png` (30) | — | 10 sets from `gen_transition_surfaces.py` + CC0 sources already on disk; WebGL / iOS / Android import tabs capped at 1024, desktop tab untouched |
| `Assets/Resources/Props/Models/Kit_Trans{Bucket,PaintTray,DropCloth,TileBox}.{fbx,json}` | — | LOD0 / LOD1: 4,386 / 1,972, 1,860 / 1,018, 1,480 / 740, 876 / 438 tris; tangents calculated on import (Mikk), as main's kits |
| `Tools/Blender/frontrooms_kit/assets/trans_{bucket,paint_tray,drop_cloth,tile_box}.py` | — | the Blender modules |
| `Tools/lookdev/gen_transition_surfaces.py` | `39496e4f` | the texture generator |

Every new `.meta` GUID was checked unused in main's `Assets/` (dry run step 4). Text files LF only.

### 2.2 Map (contract; clone only; 关卡设计 lands it)

`CONTRACT_map.diff` (today's main, after C22 + V1): `FrontRoomsMapWorld.cs` (T1–T6 + C22 exclusions), `FrontRoomsModuleUnits.cs` (T7), `FrontRoomsLevelProfile.cs` + `Assets/Levels/FrontRoomsLevel0.asset` (T8), `FrontRoomsMapInteractionTests.cs` (T9, 13 checks with the C22 one), `MAP_GENERATION.md` + `LEVEL_MODULE_SPEC.md` (T10). Bases, reasons and decisions per hunk: `CONTRACT_map_README.md`. Not touched: `FrontRoomsMap.cs`/`Resolve` (Close), any lamp code (light rule), `FrontRooms3DGame*.cs`, the Hunter, `Assets/Levels/Modules`.

---

## 3. What was built against the plan

| Plan item | State |
|---|---|
| §3.1 Frame profiles (portal, cased arch, base, end cap, corner guard, DA steel frame + saddle, window = DressWindow, free posts with colliders) | built; per 25 chunks at shot 1: 20 cased arches, 60 portals (100 pilasters), 10 free posts, 78 door frames, 4 end caps, 10 corner guards, 80 bars (187 m), 78 saddles, 2,512 base runs (3.45 km) |
| §3.2 Dressed crossing = unfinished frame (unfinished portal, bare reveal, halo at door and window) | built; `-transitionBareOpen` gives the pick's bare line (VL367) |
| §3.3 Finish set (band, tears, floor, ceiling, props, 19 materials, 30 textures, 4 props) | built; plan fixes 1–5 applied (Paper_Scrap/Peel = L0 print; stage drops carry no print or ink; smear clamp, finite guard, 3.5 mm bead flanges, prop slide; no `WallPiece` corner rule; no MPB) |
| §3.4 `BorderRenovated` (salt 977, pattern salt 811, share 0.30, exclusions) | built in the contract; 31.2 % of 4,797 crossings; exclusions = start area, and after C22 the exit area + the EXIT's 2-cell hold zone |
| §3.5 Posts belong to the map | kept: B0.1 `CornerReach` as main has it; `PostAt` asked of codex C3 |
| §5 A12 (no stop, DA envelope, sweep keep-out, halo from the DA casing, base 5 mm off) | built; G7 0 hits |
| §6.2 reduced tier (12 mm tears; no props, strips, islands, scraps, tape; straight carpet cut; base in the trim mesh; 1024 textures) | built; the tier's own draw budget is missed (§6) |
| Tier lift (P2) | not built (deferred by the plan) |

---

## 4. The look

What the frames show (VL366–VL370): every Office | Level 0 change sits on an Office piece; a renovated crossing reads as work in progress at 3 in 10; the double-acting door swings through a stopless frame both ways. Two things to look at:
- **The unfinished portal** on a renovated open edge is my reading of "a Frame on every border" (VL367, next to the pick's bare line).
- **The reduced tier redraws loose tiles and the ceiling hole in other places** than desktop on the same seed (VL369 lower sheet), because the tier skips draws from the same random stream. Harmless, but the tiers are not the same layout. Open defect D4.

---

## 5. Tests

### 5.1 In `W/proj_lt` / `W/proj_lt_suite` (main `2c450e11` + A12 r2; 14:11–14:51, UNDER LOAD: 1-minute load 37–146)

| Suite | Result |
|---|---|
| Roslyn `--all --ios` (+ WebGL player defines for Assembly-CSharp) | 0 errors |
| `FrontRoomsTransitionKitTests` | **PASS 21/0**: DA sleeve envelope, symmetric, no stop; saddle ±0.076 × 0.012; base 0.10, sweep 0.98 m; share 0.30 / 0 frame-only / override; Editor not reduced; 20 materials; G11 no print or ink on stage drops; G11 Paper_Scrap and Paper_Peel are L0_Wallpaper's print; 30/30 textures capped on WebGL/iOS/Android, 0 desktop overrides; 94.0 / 34.0 MB; 0 non-finite triangles; the kits build (235 kit renderers, 15 props); G8 two builds identical (3,974 meshes); G4 1,598 lights the same on and off; G12 no blue tape or label (32 materials); G7 desktop, reduced and seed 2554: 0 hits over 111 / 111 / 108 doors × 42 poses |
| `FrontRoomsMapInteractionTests` | **PASS 190/0** (A12 r2's 178 + 12 transition checks): collision prefixes 25/25, 5 post boxes = 5 free posts; same doors/windows/lamps; `transitions = false` builds no kit object; share 31.2 %; no wall drawn; same from either cell; rebuild 0/25 differ; revisit shift 24 kept / 0 lost; start area 0; `baseTop` 0.10 on 672 Office faces, 0 off; ModuleUnits = kit numbers |
| `FrontRoomsRelayNavTest` | **60/60**, 23 doors broken, 0 from the swing side, 0 frames in architecture |
| `FrontRoomsDoorShotTests` | **234/0** |
| `FrontRoomsMapVerification` | **100/100 seeds** |
| `FrontRoomsLevelDesignerTests` | **140/0** |
| `FrontRoomsTouchPlaytest` (iPhone) | **85 PASS / 0 FAIL** |
| `FrontRoomsMainScenePlaytest` | 2554 PASS (75 s, 230 m, 81 cells, tier 3), 20388 PASS, `-autopilotDoors` 2554 PASS (2 keys, 2 doors); kits off 2554 PASS |
| `FrontRoomsTransitionPlayCheck` (player capsule through every border crossing in range, 4 scans) | 2554 desktop: 58 crossings, 0 blocked, 0 errors, 0 kit exceptions; 20388 reduced: 12 crossings, 0 blocked |

The one console exception in every run is Unity's own `SearchDatabase` index on startup, not ours.

### 5.2 On today's main

`W/proj_lt2` (main `a50ee7da` before C22: A12 r1 + fixes, M2, GD3c) with the package and the contract cut onto MapWorld `9b82c286`, 15:41: compile 0 errors; **kit tests 21/0**; the fixed shots in pick mode are **pixel-identical** to the build's (7/7, max diff 0) and every plan identical. So A12 as landed changes nothing for the kits.

`W/proj_lt4` (main after C22 + V1, MapWorld `83c19911`) with the package and the contract re-cut after C22: RESULTS_LT4

### 5.3 The landing state (`W/proj_lt3` = today's main + the package only)

RESULTS_LT3

---

## 6. Numbers

Measured by `FrontRoomsTransitionAssets.Stats` (shots 1–4, 8–10; renderers whose bounds are within 46 m of the eye; LOD0 only) and `SeedStats` (20 seeds at each seed's spawn), `W/proj_lt`, 14:30–14:41, 1-minute load 46–132 (**UNDER LOAD**: the build times below are not timing data).

| Within 46 m, against kits off | Frame only | Frame + renovation (desktop, hero) | Reduced tier | Target / ceiling (plan §6.2) | Verdict |
|---|---|---|---|---|---|
| Δ triangles, 7 shots | +25.5 – 34.3 k | +134.3 – 194.5 k | +32.5 – 43.2 k | 45 k \| 110 k (135 k) \| 50 k | renovation FAIL |
| Δ triangles, 20 seeds at spawn, mean (median, max) | +32.7 k | +169.5 k (163.4 k, 303.3 k) | +41.8 k | same | renovation FAIL |
| Δ renderers (draws before culling) | +27 – 40 | +5.8 – 9.1 % | +6.3 – 10.3 % | +70 \| 8 % (10 %) | renovation over target on 2 shots |
| Kit renderers in the frustum | 11 – 24 | 32 – 94 | 24 – 72 | 30 \| 60 (75) \| 40 | renovation FAIL on 4 shots; reduced over target |
| Δ materials | +1 | +19 – 20 | +15 | +1 \| +20 (+21) | PASS |
| Texture memory (GPU) | 0 | 94.0 MB, loaded with the first renovated crossing | 34.0 MB | 94.0 \| 34 | PASS |
| Δ mesh memory, 25 chunks | +3.08 – 3.25 MB | +18.5 – 21.2 MB | +5.6 – 6.4 MB | 5 \| 12 (15) \| 8 | renovation FAIL |
| Δ chunk build, median (editor) | +1.3 – 2.5 ms | +4.1 – 6.2 ms | +1.3 – 2.7 ms | 2 \| 5 (8) | over target, under ceiling |
| Lights, lamp modes | 0 | 0 | 0 | any change fails | PASS |

**Where the desktop renovation triangles go** (shot 1, +187 k): `Paper_Backing` +55 k (the torn edge's white lip, sampled every 4.5 mm), `L0_Wallpaper` +37 k (remnants, islands and tear contours, in the block meshes), `L0_Carpet` +22 k (the frayed cut), props +18 k, `Cove_Base` +14 k, `Prop_SteelBrown` +13 k (the DA frames), the rest under 7 k each. The plan expected the cut to come from dropping the pick's per-face re-cut (`WallPiece`); that rule is gone (pieces taken 192–238 per 25 chunks, all band skins), but the pick's triangles were mostly the paper detail, not the re-cut. **Option measured:** paper detail at 9 mm on desktop (`-transitionDetailMm 9`): shot 2 +111 k, shot 4 +137 k (still over the 135 k ceiling on shot 4).

**Frame time** (`FrontRoomsTransitionFrameTime`, new clone-only probe; edit-mode camera frame = `Camera.Render` + a 1-px `ReadPixels` GPU wait, 1920×1080, 4× MSAA, post on, far 46 m; Apple M3 Max, Metal; 3 rounds × 40 frames per shot and mode, modes interleaved; 15:35–15:36 at a 1-minute load of **146–241**):

| Shot | Kits off (median ms) | Frame | Pick (frame + renovation) | Reduced |
|---|---|---|---|---|
| 2 | 23.44 | +1.98 | +3.70 | +2.77 |
| 4 | 21.60 | −0.43 | +1.26 | +3.43 |
| 10 | 24.61 | −1.28 | −0.57 | +0.20 |

The reduced tier coming out dearer than desktop shows these deltas are inside the run-to-run noise (single rounds of one mode varied by up to ±10 ms). **Not a valid timing; UNDER LOAD.** It does bound the cost: no mode added more than ~4 ms to a ~23 ms edit-mode frame on a machine at load 150–240. WebGL frame time is not measured (no WebGL build here); the reduced tier's draw proxy is +24–72 kit renderers in the frustum, about +0.8–2.3 % of WebGL's ~3,200 draws (ESTIMATE).

---

## 7. Frames (Shot format, `images/`)

| VL | Image | What |
|---|---|---|
| VL365 | `vl365_kits_off.jpg` | today's game with the code in and off: 5/7 byte-identical, 2 shots within 2/255 (D3), plans identical |
| VL366 | `vl366_frame_shots.jpg`, `vl366_frame_close.jpg`, `lt_borders_both_faces.jpg` | NOW \| FRAME on 5 shots; close sheet; **a Low \| Standard door, a Tall \| Standard window and a Standard \| Standard open edge + arch, kits off and on, from both faces** (by `Resolve` a Low edge is a door or a wall and a Tall edge a window or a wall, so these are all the openings each border can have) |
| VL367 | `vl367_renovation_shots.jpg`, `vl367_renovated_borders.jpg`, `vl367_share_plan.jpg`, `lt_walk_10_borders.jpg`, `lt_threeview_props.jpg` | NOW \| PICK; **renovated door, window, arch and the unfinished portal (+ bare-line fallback)**; the share plan; **a seeded walk through 10 borders** (seed 2554, 2 renovated); the four new props' three-views |
| VL368 | `vl368_your_frame.jpg` | shot 4 (Red's G10 frame) and shot 1 (the arch): 1.1 % and 1.9 % of pixels change |
| VL369 | `vl369_costs_table.jpg`, `vl369_desktop_reduced.jpg` | the budget table (FAIL rows, frame-time row) and desktop vs reduced |
| VL370 | `vl370_da_doors.jpg` | shut, 45° each way, broken rest, from both faces |

Three-views per the kit-sheet convention (`Tools/three_view`, sheet ladder 250–1000 px/m, transparent PNGs + `manifest.json`): `images/threeview/`. They are not placed in the Figma kit sheet yet (the 游戏视觉 session places them; `upload_assets` into new `img:Kit_Trans*_<view>` slots).

---

## 8. Gates

| Gate | State |
|---|---|
| G0 base | PASS: `proj_lt` = main `2c450e11` + A12 r2, Roslyn 0 errors, harness md5 `76a624e3…` |
| G1 inert | **PASS with a note:** `-transitionKitsOff` 5/7 frames byte-identical (twice), profile `transitions = false` 6/7; shot 3 differs by ≤ 2/255 in one 8-px band (the far ceiling through the door, 160 px) and shot 10 in one 8×8 block (34 px) in every off run: deterministic, invisible, cause not found (D3). Collision and renderers: identical bar the free posts |
| G2 layout | PASS: every plan identical across now / kits off / profile off / frame / pick / reduced / bare, and on today's main (`proj_lt2`) |
| G3 collision | PASS: kits on = kits off + exactly the free-post boxes (5 = 5); map suites as §5 |
| G4 lamps | PASS: 1,598 lights the same; no lamp code in the diff; V5 unchanged |
| G5 determinism | PASS: 31.2 % (0.30 ± 0.02); same from either cell; rebuild and revisit shift; 0 in the start area; after C22 the exit area + hold zone (check 13 in T9, §5.2) |
| G6 look | PASS (build): close sheet and both-faces sheet show no floating, clipping, z-fight or near-plane contact; independent critics are the next stage |
| G7 A12 fit | PASS: 0 hits over 330 doors × 42 poses |
| G8 tiers | PASS: two desktop builds identical; reduced builds with 0 errors; WebGL / iOS defines compile |
| G9 budgets | **FAIL** (desktop renovation; reduced draw target): §6, Red call §9 |
| G10 outlets | PASS: `baseTop` 0.10 on every Office face; props give `PendingKeepOuts`; the outlets probe itself was not re-run here |
| G11 print | PASS (kit tests) |
| G12 era | PASS (kit tests; three-views: no labels or brands, beige crepe tape) |

---

## 9. For Red (one call; nothing here asks you again about Frame / Renovation / V5)

**The renovation's cost on desktop.** Background: you asked for visuals at the highest spec on desktop, with WebGL as a separate reduced tier. The plan set itself a desktop budget for frame + renovation of +110 k triangles (ceiling +135 k) near the player. Built at hero detail (the torn paper's edge sampled every 4.5 mm, real fray on the cut carpet, the four props at LOD0), a renovated area adds about **+170 k triangles on average and up to +195 k** in the fixed shots, up to 94 extra draw calls in view, and about +20 MB of mesh memory per 25 chunks. What the player sees either way is the same layout; the difference is only how fine the torn edges and the fray are up close.

| Option | What you see | Cost | Notes |
|---|---|---|---|
| **A. Keep the hero detail on desktop** (recommended) | the torn paper edge, its white lip and the carpet fray stay crisp at arm's length | +170 k tris mean, +20 MB mesh, up to 94 draws near a renovated crossing; frame time not measurable today (bounded at ≤ ~4 ms of a ~23 ms editor frame at load 150–240) | matches your highest-spec rule; WebGL and phones already take the reduced tier (12 mm, no props: +42 k tris, 34 MB) |
| B. 9 mm paper detail on desktop | the lip and tears get coarser steps when you stand at the wall | shot 2 +111 k, shot 4 +137 k | still over the old ceiling on shot 4 |
| C. Fewer renovated crossings | renovation rarer than 3 in 10 | linear in the share | changes your 3-in-10 decision, so not recommended |

It blocks nothing today: the visual package is inert until 关卡设计 lands the map hunks. My recommendation is A, plus a proper frame-time capture on a quiet machine before the map hunks go in.

The second open reading, already shown in VL367: **a renovated open edge keeps an unfinished portal** (primer and galvanised beads) so that "a Frame on every border" holds there too; one switch gives the 10-08 pick's bare line instead.

---

## 10. Open defects

| # | Severity | What | Where / next |
|---|---|---|---|
| D1 | high (gate) | G9: desktop renovation over the plan's triangle, draw and mesh budgets; reduced tier over its draw target (24–72 kit renderers in the frustum against 40) | §6, §9. For the reduced tier the cheap fix is to merge the five wall-stage materials into one texture array or atlas on that tier only (WebGL is its own track) |
| D2 | medium | Frame time not resolvable under today's load (146–241) | re-run `FrontRoomsTransitionFrameTime` (6 rounds × 60 frames) at load < 32, and a Play-mode capture with real draw counts |
| D3 | low | G1: with the kits off, shot 3 and shot 10 differ from the pre-kit NOW frames by ≤ 2/255 in 160 + 34 pixels, deterministically | not visible; candidates: a shader variant or material re-serialised when the transition materials were imported. Re-render NOW and kits-off in one session to confirm |
| D4 | low | The reduced tier draws loose tiles and the ceiling hole at other places than desktop on the same seed (the skipped draws shift the stream) | give the reduced-tier skips their own sub-stream if the tiers should match |
| D5 | low | Three-views not placed in the Figma kit sheet; VL365–VL370 not placed | the 游戏视觉 session (Figma) |
| D6 | info | `PostAt` from codex C3 not available; B0.1 `CornerReach` used as main has it | C3 |
| D7 | info | Tier lift (rougher at higher tiers) not built | P2, behind C3 and a cross-chunk test |

---

## 11. Notes for other lines (no messages sent)

- **关卡设计:** `CONTRACT_map.diff` + README, cut onto today's main after C22 + V1. Asks: accept the free-post colliders; keep `EffectiveEdge`, `IsReserved`, `ExitFrontCell` as used; `PostAt` from C3; take T9 into your suite. The visual package must land first (it is inert alone).
- **游戏视觉 (D1.6):** border doors take an Office member (`Kit_DoorFrame_Steel_DA`); one saddle material with the transition proxy; publish `FrontRoomsInteractableKit.DoorFramesLive` when `DressDoor` lands so the proxy steps aside.
- **动画和动态图形模型技术 / 叙事设计:** within ≤ 2.25 m of about 0.9 renovated crossings per chunk, Level 0 drops are stripped board, primer or fresh paint, with no print, motion or EGRESS ink; remnants, islands, torn edges and scraps keep the live print. Wall cues placed by cell should skip renovated band faces (`BorderRenovated` + the drop distance) or accept losing them there.
- **WebGL 卡顿性能分析:** the reduced tier is ready for a WebGL frame-time capture once the map hunks land.
- **交互设计 / 平面视觉 / 声音设计:** nothing to do.

---

## 12. Package

```
bash /Users/redwang/FrontRoomsVisualWork/apply/level_transitions.sh            # dry run
bash /Users/redwang/FrontRoomsVisualWork/apply/level_transitions.sh --apply    # land (visual files only)
```

Dry run 15:45 on main `a50ee7da` + working tree (C22 + V1): **DRY RUN OK** (136 bases match: 133 new paths absent, 3 at their recorded sha1; 3 kept .meta unchanged; 136 payload files match; 64 new GUIDs unused). On a base mismatch: `W/tools/rebase_apply.sh level_transitions`.
