# Bathroom rooms · 70 · Look build: the pool family's look and water

Line: 游戏视觉 (ledger key `visual`). This is the line's build log for the pool family's look and water: the build of `16_pool_palette_fusion.md` §6 and `17_water.md` §7 (card bathroom-109; this line's answer bathroom-004 on 11-01: "we take 16 §6, 17 §7, 18 §4"). One section per workflow stage; later stages append their own.

- **Workflow:** `visual-pool-look-and-relay-lamps` (wf_f6fde9bb-4b3), launched by the line's session local_1fc9f09c on account G, 2026-11-03 16:5x (`handoff/archive/visual_history.md` log, line 1109). Stages: Prepare, PL01, Zone look, Tile, Ripples, Water surface, Submerged, Renders, Package.
- **This stage:** Prepare, 2026-11-03 16:58-18:25 (the first agent stopped at about 17:58, after the run that became the baseline; a second pass finished from the stage journal `W/pool_look/journal.txt`). Clone `W/proj_unlock` resynced from main (17:00, plus a copy-in of main's changes at 18:17); a clone-only look-dev harness for the pool family; the TODAY baseline (seven cameras, sheet `sheets/70_today_baseline.jpg`); this file.
- **Not done here:** nothing landed in main. Main was read only; no Unity ran on main; nothing was downloaded; no package was made. The harness and its texture are clone-only and never land.
- **Reused, not redone:** `16` (colour script, the fusion rule, the clone values of §5), `17` (water physics, per-tier stack, asks), `18` (moves M1-M7, the dark zone), `50` and `pool_modules.json` (module sizes, water classes, edges), `zone_lamps/30_for_red.md` §1.2 and §2.2-2.3 (pool lamps P1/P2), the bathroom line's stills harness and its stand-in room (`W/proj_pooldir/Assets/Editor/ZoneLamps/`, read only), main's `FrontRoomsKitLookdev` (edit-mode look-dev in a new scene), `level_space/70_structures_districts.md` §2 (look rules for the structures scene) and the level-space TILE recipe (`W/level_space/briefs/61c_r4t_redo_build.txt`, `W/level_space/make_tile.py`).

## Red's decisions that this build follows

Quoted from the records; Red's words stay verbatim, picks are given as recorded. `BH` = `Documentation/Takeover_2026-10-18/handoff/archive/bathroom_history.md` (sha1 890149ba).

| Item | Red's words or pick | When | Source |
|---|---|---|---|
| Palette | Club palette: selected "F1" (turquoise white from the club on) | 10-31 ~15:35, K chat | BH line 116 |
| Structure | Structural moves: selected all of "M7, M6, M5, M4, M3, M2, M1" | 10-31 ~15:35, K chat | BH line 116; `18` §2 |
| Dark zone | Night look: "做成更深处的一片固定区域" | 10-31 ~15:3x, K chat | BH line 117; `18` §3 |
| Dark zone in v1 | Selected "第一版就做成真正往下一层（Relay 要跨高度寻路，成本更大）" | 10-31 ~15:36, K chat | BH line 114; `50` §0 item 4 (floor -2.52, water -1.5) |
| Water Q1 | Selected "wrong": club water physical; level water absorption ×3-5, one or two rooms ×9 | 10-31 ~15:4x, K chat | BH line 115; `17` §6.5 |
| Water Q2 | Selected "v1": P4c (desktop wake simulation and surface caustics) in v1 | same | BH line 115 |
| Water Q3 | Selected "v1": ray-traced water on the Mac in v1, against the "later" recommendation | same | BH line 115; P4d in v1 (BH line 172, card bathroom-108) |
| Water Q4 | Selected "follow": the dark zone's white balance follows the warm wet-niche lamps | same | BH line 115 |
| Tile colour | "我希望底色不应该是偏暖的米白，而是和dream pool更相似的瓷白" | 10-31 17:19 | BH line 192 |
| Pool stairs | A pick (answer to `level_space/60` §8 question 8), recorded as a choice, no verbatim text on file: porcelain white on treads and risers, only a dark nosing strip marks each step, no teal band | 10-31 17:24 | BH line 193; `level_space/60_large_stairs.md` §3, §8 q8 |
| Slide supports | "管子里不会出现人影，样式混用，玩家可以滑，Relay不能走滑梯，塔高方案选择尽可能最高。其次就是滑梯当前的支架样式我不喜欢，看起来非常的工业，我认为应该是让瓷砖作为墙壁去托举滑梯" | 10-31 17:15 | BH line 190 |
| Large spirals | "办公室里/level 0还有level run里都不应该出现大型的螺旋滑梯" (read as large spiral stairs: B4-M and B4-L on the pool level only) | 10-31 17:29 | BH line 194 |

**Still Red's: the pool lamps.** P1 (overhead) and P2 (underwater) are open in `zone_lamps/30_for_red.md` §2.2 and §2.3; the recommendations are **P1-A** (indirect metal-halide wall boxes, 3.4 m up the long walls every 6 m, the ceiling lit, no lamp seen) and **P2-A** (300 W wet-niche lamps every 3 m, 0.7 m under the water, with the two caustic layers). This build works to the recommendations and keeps both switchable (the harness's `-plLamps` layer; the lamp presets of the Zone look stage). Nothing here decides them.

**The 11-01 pause and its lift.** The line's state file records Red, 11-01 17:34: "我希望你先暂停泳池" (`handoff/visual.md` §5 item 0), and the lift, 11-03 14:19: "解除泳池" (`handoff/visual.md` §3 item 2: "pool_look_wf builds look + water v1 to rec A lamps; land its packages; then the structures scene (RULES 5b, scene 4) uses them"). This workflow was launched by the line's session on 11-03 (`visual_history.md` line 1109).

## 0. Inventory (Prepare)

### 0.1 Main files this build reads (R) or will change through its packages (W)

sha1 (first 8) at main git `606036b2` plus the working tree, 2026-11-03 17:00. "W" files are 游戏视觉's (RULES rule 3); files of other owners are reached only through contracts.

| sha1 | Path (under `Frontrooms3D/`) | R/W | Why |
|---|---|---|---|
| 898dee87 | `Assets/Scripts/Rendering/FrontRoomsLook.cs` | W | one warm trilight and fog for the whole game today; the pool zones need their own (`16` §0.1, §6.1) |
| 68824661 | `Assets/Scripts/Rendering/FrontRoomsPostStack.cs` | W | zone grade volumes (`EnsureZoneVolume` loads `FrontRoomsPost_<zone>`) |
| f385ea86 | `Assets/Resources/Rendering/FrontRoomsPost.asset` | R | the global film look (white balance +9, tint -7, saturation -8: the yellow) |
| 040ba42b | `Assets/Scripts/Rendering/FrontRoomsZoneReflection.cs` | W | `Refl_Pool` and its dead-lamp twin (`17` §7.11) |
| 801aca30 | `Assets/Scripts/Rendering/FrontRoomsZoneReflectionDriver.cs` | R | the reflection crossfade the probes reuse |
| 8f3587e0 | `Assets/Resources/Rendering/FrontRoomsSurface.shader` | W | submerged shading, caustics, wet band (`17` §7.2, §7.8); used by the whole game, so every new term is off unless enabled |
| fe0a1ed7 | `Assets/Scripts/Rendering/FrontRoomsSurfaces.cs` | R | material lookup (`Troffer_Lens`, room sets) |
| 1bcc9d0d | `Assets/Resources/Surfaces/Prop_CeramicGlaze.mat` | R | the game's only glazed tile today (0.86 / 0.85 / 0.80, smoothness 0.85, no tile map); the pool tile becomes new `Prop_PoolTile_*` materials, this one stays |
| 8ac9b038 | `Assets/Resources/Surfaces/Troffer_Lens.mat` | R | the L03 lens drive |
| 2ab5cdf1 | `Assets/Scripts/Rendering/FrontRoomsTrofferKit.cs` | R (W in PL01) | the L03 kit; PL01's option A would add a fixture registry |
| b951084f, 43b4a8c4 | `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRT.cs`, `FrontRoomsGlassRTSystem.cs` | R | G14; water as a receiver is P4d, after this workflow |
| 68eac09e | `NativePlugin/FrontRoomsMetalGlassRT.mm` | R | same |
| 556b9099 | `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs` | R (关卡设计's) | the lamp rule today (`BuildFixture`, `TickFixtures`); C1/C2 hooks come by contract |
| 76a6fc47 | `Assets/Scripts/FrontRoomsMap/FrontRoomsModuleUnits.cs` | R (关卡设计's) | cell 3 m, walls 0.16, ceilings 2.4 / 2.9 / 5.4, eye 1.62, 192 m world period |
| 11d34761 | `Assets/Levels/FrontRoomsLevel0.asset` | R (关卡设计's) | lightRadius 16, shadowRadius 9 |
| 0fb29ab1 | `Assets/Scenes/FrontRooms3D.unity` | R | the scene's fill light "Soft ambient direction" and render settings |
| 88027cf8 | `Assets/Scripts/FrontRoomsQuality.cs` | R (设置菜单与画质预设's) | ULTRA / HIGH / handheld rules; water tiers come by contract |
| 078d0a30 | `Assets/Scripts/Input/FrontRoomsHandheld.cs` | R (交互设计's) | the phone tier (`Active`, `Force`, `AspectFov`) |
| 18584f2b, 15ec2379 | `Assets/Settings/FrontRooms_URP.asset`, `FrontRooms_URP_Renderer.asset` | R (设置菜单's for tiers) | Forward+, MSAA 4, rendering layers off, box projection off |
| a8ae1402 | `ProjectSettings/QualitySettings.asset` | R | Standalone default Ultra (5), iPhone Medium (2), one URP asset for all levels |
| 72e0694b | `Assets/Editor/Rendering/FrontRoomsKitLookdev.cs` | R | the edit-mode look-dev pattern the harness follows |
| cb1372b7, 1ec7762d, e3622f0e, 90e792fb, ff54719f | `Documentation/research/bathroom_rooms/16`, `17`, `18`, `50`, `pool_modules.json` | R | the asks and the modules |
| 203c6b44 | `Documentation/research/zone_lamps/30_for_red.md` | R | P1/P2 |
| 9262342a | `Documentation/research/level_space/70_structures_districts.md` | R | the structures scene's pool look rules |
| 009fc4ed | `Assets/Scripts/Rendering/FrontRoomsLampKit.cs` (new in main at 17:00 and 17:16, 灯具适配's landings `zone_lamps_run`, `zone_lamps_office`) | R (W if the pool presets join it) | the zone-lamp factory past L03 (`ForCell`, `BuildMapLamp`, `LensDrive`; families Strip, RemotePlate, Pendant, Wrap); the pool's P1/P2 families would be added here once Red picks |
| (folder) | `Assets/Resources/Props/Lamps/` (same landings) | R | the kits on the troffer's trim sheet, and `Shaders/FrontRoomsLampGlow.shader` (FrontRooms/LampGlow: an additive analytic lit-ceiling patch as its own renderer), the shape P1-A's lit ceiling can reuse |

Re-checked 2026-11-03 18:15 at main git `64eb14d4` (Red's commit after the 17:00 sync): every sha1 above is unchanged.

New files the packages will add (paths proposed, decided in the Package stage) are listed in §1.2.

### 0.2 What exists in clones (stand-ins: references, never landed as they are)

| What | Where | Use here |
|---|---|---|
| Zone-lamps kit: HIDUP (= P1-A wall box), MHLB (P1-B), VT8 (P1-C), NICHE (= P2-A), PORT (P2-C), STRIP, INDR, PEND, MHDL; ZL tile maps; `ZL_Water`; `ZL_Caustic` with `ZoneLampsCausticAdd.shader` and `ZoneLampsGlowAdd.shader` | `W/proj_pooldir` and `W/proj_zonelamps`, `Assets/Resources/Props/ZoneLamps/` and `Assets/Resources/ZoneLamps/`; sources `W/zone_lamps/kit/zl_build.py`, `zl_tex.py` | P1-A / P2-A working picks; the lamp shapes, intensities and spot cones the Zone look stage starts from |
| The bathroom stills harness and its pool STAND-IN ROOM (hall 14.84 × 23.84 × 5.4 m, basin 9 × 18 m) | `W/proj_pooldir/Assets/Editor/ZoneLamps/FrontRoomsZoneLampsShots.cs`, `.PoolDir.cs` (copy in `W/bathroom_dir/src/`); frames `W/bathroom_dir/renders/pooldir/{all,fusion,structure}` | dimensions reused by this harness; its first-pass water is TODAY's water |
| Porcelain tile in the level-space greyboxes: `Prop_CeramicGlaze` set to (0.93, 0.95, 0.96) in the clones only | `W/proj_levelspace`, `W/proj_levelspace_slides` | Red's 17:19 colour, as tried; main's material is unchanged |
| TILE recipe: 150 mm (6 in) tile, 5 mm cool grey grout, per-tile tone about 1.2 %, normal tilt about 0.5°, cushion edge; 4096 px = 2.4 m sheet | `W/level_space/briefs/61c_r4t_redo_build.txt`, `W/level_space/make_tile.py` | starting point of the Tile stage's generator |
| The water's first pass: URP Lit, premultiplied transparent, base (0.05, 0.20, 0.22) at a constant alpha 0.30, smoothness 0.95, ripple normals `ZL_WaterRipple_N` at 0.30 every 1.8 m | `FrontRoomsZoneLampsSetup.MakeRoom` in `W/proj_pooldir` | TODAY's water in this harness (texture copied as `PL_TodayRipple_N.png`, sha1 e255eed5) |
| This harness | `W/proj_unlock/Assets/Editor/PoolLook/` (§2.6) | every stage of this workflow |

### 0.3 The bathroom stills' clone-only values (`16` §5): a starting point for the Zone look stage, not a spec

- Trilight sky 0.15 / 0.20 / 0.22, equator 0.17 / 0.23 / 0.24, ground 0.21 / 0.28 / 0.28; fog 0.11 / 0.16 / 0.17 at exp² 0.010.
- Grade: white balance -12 (the global film look has +9), tint -2, post-exposure +0.45, contrast -4, saturation +4.
- Lamps: metal halide 0.90 / 0.97 / 1, club cool white 0.93 / 0.97 / 1, level daylight 0.86 / 0.96 / 1 at 1.2×, wet-niche seen through water 0.55 / 0.92 / 1.
- Water base 0.16 / 0.60 / 0.62 at alpha 0.36; waterline course 0.40 / 0.76 / 0.77; caustic colour 0.32 / 0.50 / 0.50, ceiling levels 0.55-0.65.

### 0.4 The clone

- `W/proj_unlock` is the workstation "visual" (`W/tools/workstations.json`, holder 游戏视觉). Resynced 2026-11-03 17:00 from main git `606036b2` plus the working tree, by the manual `W/TOOLS.md` §2 recipe (main dataless count 0 in all five folders; rsync of Assets, Packages, ProjectSettings, NativePlugin, Tools with the iCloud excludes; `apply_local_patches.py`, a no-op; `install_tools.py`, test bundle id `com.redwang.frontroomsss.test.unlock`; `m_DisableAudio: 1`; the silencer's `.meta` files restored). Stamp: `W/proj_unlock/FRSYNC.txt` with nine key files' sha1.
- Before the rsync, every clone file that differed from main or was absent in it was saved: `W/pool_look/clone_only_backup/` (93 files, `MANIFEST.txt`: 46 clone-only = the installed W tools and the silencer, 47 differing = main's edits since the 11-02 sync and the clone's ProjectSettings). The unlock probe was already gone from the clone (removed by the 11-02 combined-test sync); its copy stays in `W/unlock_work/harness/`.
- Batch compile after the resync: 0 `error CS` (`W/pool_look/logs/compile_resync.log`).
- 18:17, main had moved (git `606036b2` → `64eb14d4`; the zone-lamp kits, world text, terminals; every CLAIM in LANDINGS had its LANDED row): a copy-in without deletions, `rsync -a` of Assets and Tools with the iCloud excludes, 1008 files (`W/pool_look/logs/resync_delta_1820.log`). Main deleted nothing since 17:00 (the 55 paths a `--delete` would remove are all clone-only and in `MANIFEST.txt`); ProjectSettings kept (only `ProjectSettings.asset` differs: the clone's test id). After it the clone is main plus its clone-only files. Roslyn on the clone, `--all`: COMPILE OK, 0 errors (`W/pool_look/tmp/roslyn_h7`). `FRSYNC.txt` carries both stamps.

## 1. Plan (Prepare)

### 1.1 Stages and what each covers

| Stage | Covers | Output |
|---|---|---|
| Prepare | clone, harness, TODAY | this file §0-§2; sheet `sheets/70_today_baseline.jpg`; frames `W/pool_look/renders/today_v4/` |
| PL01 | not the pool: the Relay's hood against the ceiling troffers (combined-test finding) | its own docs under `research/hunter/production_b/`; package `relay_lamp_clearance` (not applied) |
| Zone look | `16` §6.1 (per-zone ambient, fog, grade, blended at borders) and §6.2 (lamp colours); `17` §7.10 (cool fog, height fog over water, the dark zone's white balance following the niche lamps: Q4); `17` §7.9 in part (compact sources: P1-A / P2-A as presets) | profiles PoolClub, PoolHall, PoolLevel, PoolDark; `FrontRoomsZoneLookVolume`; `Attach(map)` / `Bind(map, viewer)` (bathroom-005), inert until the map exposes pool zones (C1) |
| Tile | `16` §6.4; Red 17:19 (porcelain white), 17:24 (dark nosing only); 17:15 (tile as the structure) | a generator under `Tools/lookdev/pool_tile/`, `Prop_PoolTile_*` materials (desktop 2048, phone 1024): field tile, waterline course, 1 in mosaic, bullnose and coping, stair treads with nosing, wet band |
| Ripples | `17` §7.3 (baked dispersive FFT loop, isotropic, distance roughness), §7.4 (rings) | `Tools/lookdev/pool_water/` generator, Texture2DArray loops per tier, `FrontRoomsWaterRings` |
| Water surface | `17` §7.1 (reflection only, alpha = Fresnel), §7.6 (planar camera), §7.7 noted (Relay-only overlay as the follow-up) | `FrontRooms/Water` shader, `FrontRoomsWaterPlanar`; tier rules by contract to 设置菜单与画质 |
| Submerged | `17` §7.2 (absorption, trapping LUT, `_WaterAbsorbScale` per zone: Q1), §7.8 (caustics by tier), §7.9 (lamps feed caustics), §7.11 (probes); `16` §6.3 | terms in `FrontRooms/Surface`, off unless enabled; `Refl_Pool` and its dead-lamp twin |
| Renders | the decision material | TODAY vs v1 sheets, MP4 walk, `71_look_for_red.md` (Chinese), `W/vl_pending/pool_look.md` |
| Package | landing | v2 tracks `pool_zone_look`, `pool_tile`, `pool_water`; contracts; combined-test rows |

Q2 (P4c in v1) runs through Ripples (rings), Water surface and Submerged (desktop caustics from the surface); the two-band wave window around the camera (`17` §7.5) is the part of P4c this list does not name: the Ripples stage decides whether it fits or becomes the first follow-up, and says so here. **Ripples decided (§5.8): the first follow-up, inside v1, desktop only; the hooks it needs are built.**

### 1.2 Packages and files (proposed; the Package stage decides)

| Track | Files (under `Frontrooms3D/`) | Owner |
|---|---|---|
| `pool_zone_look` | `Assets/Scripts/Rendering/` zone-look code (profiles, `FrontRoomsZoneLookVolume`, Attach/Bind, lamp presets); `Assets/Resources/Rendering/FrontRoomsPost_PoolClub`, `_PoolHall`, `_PoolLevel`, `_PoolDark` volume profiles; possibly `FrontRoomsLook.cs` and `FrontRoomsPostStack.cs` | 游戏视觉 |
| `pool_tile` | `Tools/lookdev/pool_tile/` (generator); `Assets/Resources/Surfaces/Pool/` (`Prop_PoolTile_*` materials, maps, phone variants) | 游戏视觉 |
| `pool_water` | `Tools/lookdev/pool_water/`; `Assets/Scripts/Rendering/Water/` (rings, planar); `Assets/Resources/Rendering/FrontRoomsWater.shader`; `FrontRoomsSurface.shader` (submerged terms); ripple loops; `Refl_Pool*` | 游戏视觉 |
| `relay_lamp_clearance` (PL01) | per PL01's recommendation | 游戏视觉 |
| contracts | map (关卡设计): C1 ZoneTheme and C2 `WaterAt` for `Attach(map)`; settings (设置菜单与画质预设): water quality tiers | the owners |

Every track is inert by itself: nothing outside a pool zone, a zone-look volume or a water component changes (RULES rule 5: Editor, Mac and Win unchanged outside them; WebGL behind its own gate).

**Decided by the Package stage (§9.1):** the three tracks as above, landing in that order (`FrontRoomsSurface.shader` stacked: 37ccbb59, cef7facc, 8067dc67); `FrontRoomsLook.cs` changes in `pool_water` (the pool cubes), `FrontRoomsPostStack.cs` does not change; the contracts are rebased in §9.6. Their textures sit in `Resources` and so ship in every build: §9.5.

### 1.3 Per tier (`17` §6.1, the reference for every stage)

| Layer | Desktop Ultra (1440p reference) | Desktop High | Handheld (iPhone, iPad) | WebGL |
|---|---|---|---|---|
| Normals | baked dispersive FFT loop 512² × 64 (or live 256²), 16 rings, two-band simulation, distance roughness | baked loop, 8 rings, ripple-band simulation 512² | baked loop 256² × 32, 4-6 rings | baked loop 256² × 16-32, 4 rings |
| Reflection | planar ½ res, overscan 1.15, mips; probe; lamp specular | planar ⅓-¼, every 2nd frame; probe; lamp specular | probe; lamp specular; Relay-only overlay | probe; lamp specular; Relay-only overlay (G11 Web plan) |
| Refraction and colour | analytic basin with the trapping LUT; ankle tint in Surface; opaque-texture wobble for ankle water | same | analytic basin; ankle tint; no wobble | same as handheld |
| Caustics | projected from the surface, ≤ 2 compact sources per room, plus thrown light | same at ½ res, ½ rate | flipbook | two scrolled layers |
| Wakes | two-band simulation | ripple band | rings | rings |
| Haze | per-zone exp² fog + height fog over water | same | same | same |
| Later | GlassRT water (Mac, 16+ GPU cores) replaces the planar on water; DXR on Windows | - | - | - |
| Budget (`17`, estimates) | ≤ 4-5.5 ms; RT option +3-5 ms | ≤ 2.5-3.5 ms | ≤ 2 ms at device scale | no new draws except the overlay camera; lights + probes ≤ 32 |

### 1.4 What stays for later

- **P4d, ray-traced water (water as a G14 receiver through the Surface hook, `17` §7.12), comes after this workflow's v1.** Red's Q3 keeps it inside the pool family's v1 (not v1.1); it needs G14's open items and a timing on a pool-sized quad first, so it is the next package after this workflow, not one of its stages.
- The Relay-only overlay on probe tiers (`17` §7.7, G11 §2.4b): named by the Water surface stage as its follow-up.
- Observation windows' underwater view (C10, M4): a shader of this line, not in this workflow's stages.
- Production lamp kits (P1/P2 hero LOD0-LOD2): after Red's P1/P2 picks; until then the Zone look stage uses the stand-in kits' shapes as presets.
- Map integration: the zone looks reach the game when 关卡设计 exposes pool zones (C1) and water classes (C2); until then they show only through `FrontRoomsZoneLookVolume` (the structures scene, RULES 5b) and the harness.
- Never in v1: underwater views and swimming, splash, spray and foam (`17` §8).

## 2. Baseline: TODAY (Prepare)

The game has no pool zone today. TODAY is what these rooms would look like if the map built them with today's code: Level 0's look everywhere.

![TODAY baseline](sheets/70_today_baseline.jpg)

Sheet `sheets/70_today_baseline.jpg` (3840 × 1600, Shot format, chip BASELINE, sha1 e1e8bfbc; every label checked at full size: ALL LABELS FIT, `W/pool_look/sheets/70_today_baseline_fit.txt`). Frames: `W/pool_look/renders/today_v4/` (S1-S7 and the lamp check `S2_diag_nolights`). `today_v5` is the same run in the clone after the 18:17 copy-in: every frame pixel-identical (mean absolute difference 0.000, max 0). Runs `today_v1`-`v3` and `diag_v3` are void (lamps dark, §2.5) and are deletion candidates.

### 2.1 What "today" is, per layer

| Layer (flag) | TODAY | From (main) |
|---|---|---|
| Zone (`-plZone today`) | `FrontRoomsLook.ApplyAmbient`: trilight sky 0.20 / 0.19 / 0.15, equator 0.26 / 0.24 / 0.17, ground 0.40 / 0.36 / 0.24; fog 0.16 / 0.15 / 0.11 at exp² 0.014. The global film look (`FrontRoomsPost`: white balance +9, tint -7, saturation -8), no zone grade. The scene's fill light "Soft ambient direction" (0.77 / 0.82 / 0.80 at 0.16, soft shadows at 0.18). The reflection cube by zone: hall `Tall`; corridor, turn and level `Level0`; dark zone `DeadLamp` | `FrontRoomsLook.cs`, `FrontRoomsPostStack.cs`, `FrontRooms3D.unity`, `FrontRoomsZoneReflection.cs` |
| Lamps (`-plLamps today`) | the map's rule (`BuildFixture`, `TickFixtures`, Level 0 profile): an L03 troffer per 3 m cell, its root at the cell centre +0.3 z on the ceiling plane; its light a 162° / 96° spot 0.06 m under it, 1 / 0.96 / 0.88 at 5 (×1.6 and range 12 under the 5.4 m ceiling, range 10 elsewhere), off past 16 m (fading from 13 m), about one lamp in three soft-shadowed within 9 m; the lens at the L03 drive (1.8). Every overhead of the dark zone dead (lens ×0.04, light off). 69 troffers, 10 dead | `FrontRoomsMapWorld.cs`, `FrontRoomsTrofferKit.cs`, `FrontRoomsLevel0.asset` |
| Tile (`-plTile today`) | `Prop_CeramicGlaze` on all 20 tile slots: warm cream 0.86 / 0.85 / 0.80, smoothness 0.85, no tile map, no grout. Structure slots from main's Surfaces: `RunB_BlockUpper` above the hall tile, `Office_Wall_Fresh` paint and hall ceiling, `L0_Ceiling` grid (soffit, club), `RunB_Floor` (club VCT), `Prop_StainlessSatin` (ladders) | `Resources/Surfaces/` |
| Water (`-plWater today`) | the bathroom stills' first pass: URP Lit, transparent premultiplied, base 0.05 / 0.20 / 0.22 at a constant alpha 0.30, smoothness 0.95, the stand-in ripple normals at 0.30 every 1.8 m, static. Reflection = the zone cube; the stills' box-projected realtime probe is not reproduced (the game has no water probe today, `17` §7.11) | `FrontRoomsZoneLampsSetup.MakeRoom` (`W/proj_pooldir`) |
| Submerged (`-plSub today`) | none: no absorption, no caustics, no wet band | - |

### 2.2 Spaces and cameras (the same in every stage)

| Space | Module and size | Water | Origin |
|---|---|---|---|
| hall | PC_Hall, 5 × 8 cells (faces 14.84 × 23.84 m), Tall 5.4. Glazed tile to 2.25, cap band to 2.40, painted block above; painted structure with three downstand beams (z 6.04, 12, 17.96; 0.45 deep); M1 soffit at 2.9 over the south deck (z 0-3); coping 0.25 m, 30 mm proud, on W, E and N; two ladders; south arch 1.2 × 2.2 from the club, north round arch 1.4, crown 2.3 (the far door) | basin x 3-12, z 3-21, water -0.12. Floor: beach 0 → -0.40 (z 3-6), → -1.07 at z 9, -1.07 → -1.37 (z 9-15), -1.37 to the deep end. Waterline course from -0.155 to the deck | (0, 0, 0) |
| turn | PL_Turn behind the far door, 2 × 2 cells, Standard 2.9; tile to the ceiling, band at floor + 1.2-1.5 | ankle: floor -0.15, water -0.03 | the hall's |
| corridor | the club, 1 × 3 cells south of the hall, Standard 2.9; tile to 1.5 (band 1.25-1.40), paint above, VCT floor, the tower's 2' × 2' grid | none | the hall's |
| level | three PL_Chain rooms of 2 × 2 cells, Low 2.4; round arches 1.4 wide, crown 2.1 | ankle 0.12 (floor -0.15, water -0.03) | (192, 0, 0) |
| dark | two PL_Dark rooms (2 × 2 and 2 × 3 cells), floor -2.52, ceiling 0.38 (2.9 above its floor), round arch crown 2.3 above the floor | water -1.5 (1.02 deep) | (384, 0, 0) |

Fixture anchors (data only, TODAY places nothing on them): 8 P1-A wall boxes on the hall's W and E walls at 3.4 m, at the centres of the bays between the soffit's fascia, the beams and the north wall (z 4.52, 9.02, 14.98, 20.98); 18 P2-A wet niches, ring centre 0.7 m under the water, one per 3 m cell on the long walls where the floor clears the ring by 0.10 m (the basin's 4 deep cells per side at -0.82, z 10.5-19.5; the dark zone's 5 per side at -2.20, z 1.5-13.5). P2-A in the stand-in room had 12 (one per cell on both long walls); the decided basin's beach entry and 1.07 m shallow end leave room for 8 at that depth.

| Id | Space | Eye (world, m) | Looks at | vFOV | Frame | For |
|---|---|---|---|---|---|---|
| S1 | hall | (2.55, 1.62, 6.20), standing on the west deck at the coping over the shallow end | (9.2, -0.25, 17.0), down the basin to the deep end | 76 | 1920 × 1080, Ultra | the hall's read: water as a mirror, lamp streaks, the ceiling, tile to 2.4 m |
| S2 | level | (193.6, 1.47, 1.0), standing (floor -0.15 + 1.62) | (196.5, 0.55, 11.0), through the arch | 76 | 1920 × 1080 | the 2.4 m room, ankle water, the sourceless wash |
| S3 | corridor | (0.75, 1.62, -2.85) | (1.75, 1.15, 0.0), the arch into the hall | 60 | 1920 × 1080 | 3/4 view at about 3 m: the club's tile and VCT, the club-to-pool change |
| S4 | hall | (11.65, -0.05, 13.0), 0.35 m from the east basin wall, 0.07 m above the water | (12.0, -0.11, 13.09), 14° off square | 60 | 1920 × 1080 | hero: joints, the aqua waterline course, meniscus and wet band, coping |
| S5 | hall | as S1 | as S1 | 76 | 2622 × 1206 | the phone tier: `FrontRoomsHandheld.Force(IPhonePro)`, quality Medium (2), the URP asset's own shadow atlas, the kits' phone meshes |
| S6 | dark | (385.5, -0.90, 1.0), wading: 1.62 above the floor, 0.60 above the water | (388.2, -1.25, 13.0) | 76 | 1920 × 1080 | Red's fixed dark zone: niche lamps only, Q4's white balance |
| S7 | level | (195.0, 0.45, 1.4), crouched: 0.6 above the floor, 0.48 above the water | (196.5, 0.55, 9.0), along the water through the arches | 76 | 1920 × 1080 | the water close up: ripples, streaks, reflections |

### 2.3 Numbers (today_v4)

| Cam | Mean luma (0-255) | Min-max | Lamps on within 30 m (soft-shadowed) |
|---|---|---|---|
| S1 | 135.1 | 23-209 | 37 (5) |
| S2 | 116.6 | 21-211 | 11 (3) |
| S3 | 125.8 | 22-210 | 20 (2) |
| S4 | 123.4 | 77-152 | 42 (5) |
| S5 | 138.7 | 23-210 | 37 (5) |
| S6 | 50.0 | 22-103 | 0 |
| S7 | 133.2 | 21-209 | 12 (3) |
| S2 with every light off (the lamp check) | 49.0 | 21-209 | 0 |

No clipped pixels in any frame. The lamps add 67.6 of S2's mean (they added nothing in the void runs). Build cost (`cost.tsv`, all layers today): hall 232 renderers, 29,340 tris, 40 lights; turn 25 / 2,798 / 4; corridor 20 / 2,074 / 3; level 65 / 8,558 / 12; dark 55 / 6,990 / 10 (all dead); plus the fill light. Most of the tris are the L03 housings (desktop LOD0). Frame time and memory: not measured (rule 25; batch renders on a shared machine would be UNDER LOAD anyway).

### 2.4 Reading TODAY: what the later stages change

Against `16` §3 (colour script) and F1, `17`, and Red's words:
1. **Warm everywhere.** Level 0's yellow trilight, warm fog and the film look's +9 white balance turn the white tile cream-olive; nothing reads turquoise white (F1), and club, hall, level and dark zone share one look. → Zone look.
2. **Cream tile without joints.** The glaze is the warm cream Red turned down on 10-31 at 17:19; no joints, grout, courses or nosing anywhere, so S4 at 0.35 m is a blank field and the 3 m views read as painted plaster (1 px is about 2 mm at 3 m: joints must carry tone, not only relief). → Tile.
3. **Milky water.** A constant alpha 0.30 over the cream basin, lit by the warm cube: the deep end and the ankle water look alike (no depth colour), the surface is not a mirror at S1's grazing angles (no Fresnel), lamp highlights are round blobs instead of streaks, and the static normals repeat every 1.8 m (the scrolling-texture look `17` rejects). → Ripples, Water surface, Submerged.
4. **A black lid over the hall.** The troffer spots point down from 5.4 m, so the structure reads dark above bright tile and every lens shows in the water; P1-A lights the ceiling and shows no lamp. → Zone look (lamp presets).
5. **The dark zone is Level 0 with its lamps dead** (S6): dim olive, nothing glows; Red's fixed dark zone is lit only by the wet niches, the basin glowing aqua under a warm white balance (Q4). → Zone look and Submerged.
6. **No caustics, no wet band, no meniscus.** → Submerged.
7. **The phone tier (S5) reads like the desktop**, faults included: the later stages keep that parity without extra passes on phones.

### 2.5 Limits of the harness and of these frames

- Procedural stand-ins on the decided module sizes, not the map's pieces: no props, signs, Relay or player; walls are single faces on the 3 m lattice, arches without trim.
- URP rendering layers did not isolate lights here (run `diag_v3`: with them on, the lamps lit nothing, S2 49.0 either way, 116.6 with them off). Every space is on the Default layer and kept apart by distance (192 m); the hall, its turn room and the club corridor light each other through their walls as the map's unshadowed lamps do. The void runs `today_v1`-`v3` and `diag_v3` predate this finding.
- The new scene's ambient probe is rebuilt by Unity only between editor updates, so a run is staged over editor ticks and exits by itself. In both valid runs Unity rebuilt it after 3 updates; the computed fallback (logged COMPUTED) was never used.
- The ripple normal loads from the PNG's bytes: a texture imported inside the same batch call does not load through `AssetDatabase.LoadAssetAtPath`. The fallback is correct for an RGB normal map and is logged. Stages that bring their own textures import them in an earlier run, or read bytes for look-dev only and check the import settings before packaging.
- S4 is not a player view (inside the basin, 0.07 m above the water): a material frame only.
- Frame time and memory: not measured (rule 25).

### 2.6 The harness (for every later stage)

- **Files:** `W/proj_unlock/Assets/Editor/PoolLook/` (clone only, never lands): `FrontRoomsPoolLook.cs` (entry, staging, cameras, render, URP in memory; sha1 3bf69ff9), `FrontRoomsPoolLook.Spaces.cs` (spaces, lamp slots, water rects, anchors; 4a63f268), `FrontRoomsPoolLook.Layers.cs` (the five layers' TODAY cases; 38697c44), `Textures/PL_TodayRipple_N.png` (e255eed5). ASCII, no tuples, fresh GUIDs.
- **Entry point:** `FrontRoomsPoolLook.RunBatch` (batch). In code: `FrontRoomsPoolLook.Begin(LookSpec spec, string run, string camList, bool phone, bool png, bool exitAfter)`. Menu: FrontRooms > Pool look (clone only) > Render TODAY, all cameras.
- **Run it** through the launcher, which keeps the line at one Unity process (it waits for three quiet checks 5 s apart with no Unity on any `W/proj_*`, then writes `W/pool_look/logs/<run>.status`):

```
cd /Users/redwang/FrontRoomsVisualWork/pool_look/tmp && QUIET_CHECKS=3 /bin/zsh ./run_when_free.zsh <run> [-plZone <v>] [-plLamps <v>] [-plTile <v>] [-plWater <v>] [-plSub <v>] [-plCams S1,S4] [-plNoPhone] [-plPng] [-plDiag]
```

  which runs exactly:

```
/Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/redwang/FrontRoomsVisualWork/proj_unlock -buildTarget OSXUniversal -executeMethod FrontRoomsPoolLook.RunBatch -plRun <run> [flags] -frGlassRT off -logFile /Users/redwang/FrontRoomsVisualWork/pool_look/logs/<run>.log
```

  No `-quit` (the harness exits by itself: 0 = every camera written, 3 = a failure; deadline 25 min) and no `-nographics` (it renders). `today_v5` took 72 s from launch to exit, most of it importing the 18:17 copy-in; the harness itself takes 1-7 s.
- **Output:** `W/pool_look/renders/<run>/`: `S1.jpg` … `S7.jpg` (q92; `<id>_raw.png` too with `-plPng`, for pixel comparisons), `shots.tsv` (pose, quality, lights per camera), `look.txt`, `cost.tsv`, `run_log.txt` ending `FINISHED fail=0`. The TODAY sheet's script is `W/pool_look/tmp/make_today_grid.py`; a stage's TODAY-against-new grid copies it.
- **Flags:** one value per layer, `today` by default; an unknown value fails the run (no silent fallback). `-plCams` picks cameras, `-plNoPhone` skips S5's tier switch, `-plDiag` adds S2 with every light off.
- **Adding a look:** a `case "<value>":` in the layer's switch, best in the stage's own partial file (`FrontRoomsPoolLook.<Stage>.cs`): `BuildTileSet` fills `slots[...]` for every name in `TileSlots` and the structure slots; `ApplyZone` sets RenderSettings and volumes under `zoneRoot`, and `perCamera` for per-camera state (reflection, zone weights); `BuildLamps(value, phoneTier)` places lamps on `lampSlots` and `anchors`, adds their lights to `sceneLights` and its per-camera rule to `lampTick` (it is rebuilt with `phoneTier` true for S5); `BuildWater` builds per `waterRects`; `BuildSubmerged` sets globals and keywords. TODAY's cases stay as they are, so the TODAY column can be re-rendered at any time; every new value gets a line in its stage's section below.
- **Data the layers read:** `spaces` (id → `Space` with its transform, origin, reflection zone and pool zone PoolHall / PoolClub / PoolLevel / PoolDark), `lampSlots` (69 ceiling cells), `waterRects` (4: basin, two ankle, dark), `anchors` (26), and the constants of §2.2 (`HallWater`, `BasinFloor(z)`, `LevelFloor`, `LevelWater`, `DarkFloor`, `DarkWater`, …).
- **Cameras:** `AllCams()`. Never move one; a stage that needs another view adds a new id (S8, …) and says so here.
- **Tiers:** desktop = quality Ultra (5) with the additional-light shadow atlas at 8192, as `FrontRoomsQuality`'s ULTRA (in memory, restored at the end); phone = S5 as in §2.2.
- **For the walk film (Renders):** the club corridor, the hall and the turn room are one connected set; the level chain (x 192) and the dark zone (x 384) stand apart, so a walk cuts between them.

## 3. Zone look

Stage 3 of the workflow, 2026-11-03 21:07-23:0x. The first agent (account B, relay run `wf_48851110-b3c`) built the runtime, the harness layer and value iterations 1-3, and stopped at B's plan limit about 21:52 with its next runs queued; a relay agent on account G (local_1fc9f09c's workflow) ran them from the stage journal at 22:24 and finished the stage. **Nothing landed in main:** main was read only, no Unity ran on main, nothing was downloaded, no package was built (the Package stage builds `pool_zone_look`), the map contract is not applied.

The asks: `16` §6 item 1 ("per-zone ambient, fog and grade ... blended at zone borders") and item 2 (lamp colours per zone), `17` §7 item 10 (the pool family's cool fog, a height-fog layer over water, a night-state white balance that follows the niche lamps), Red's picks F1 and Q4 "follow" (recorded as picks, no verbatim words on file: BH lines 115-116, "Red's decisions" above). The agreed tool entries: bathroom-005 (游戏视觉 → bathroom, 11-01): "the pool look ... hangs off public tool entries Attach(map) and Bind(map, viewer) so the Level Designer preview shows it, as the station looks do".

![Zone look](sheets/70_zone_look.jpg)

Sheet `sheets/70_zone_look.jpg` (3838 × 7400, Shot format, chip WAIT-RED: P1 and P2 stay Red's; columns TODAY = `today_v4`, ZONE = the zone look with every other layer TODAY = `zl_v8`, ZONE+LAMPS = the zone look with the lamp presets = `zl_v8L`; rows S1-S7, the club-to-hall border walk, COST; every label checked at full size: ALL LABELS FIT, `W/pool_look/sheets/70_zone_look_fit.txt`). Frames: `W/pool_look/renders/zl_v8/`, `zl_v8L/` (plus `B1`-`B7`, the `_pre` and `_off` frames of the checks).

### 3.1 What was built (in the clone `W/proj_unlock`; the Package stage carries it to main)

| Path (under `Frontrooms3D/`) | sha1 | GUID (`.meta`, fresh: none of main's 3,222) | What |
|---|---|---|---|
| `Assets/Scripts/Rendering/ZoneLook/FrontRoomsZoneLook.cs` | 8bf5b76f | 3fdfc4c3843d4e63906370be0a0eea3c | the runtime: registry, per-render-context blend and exact give-back, the trilight → probe basis, tool entries, lamp presets |
| `Assets/Scripts/Rendering/ZoneLook/FrontRoomsZoneLookProfile.cs` | 760264ae | 5f2a7b9a3c3f45b6b2be9f98d3890216 | one zone's look (ScriptableObject) and the built-in values per zone (`Defaults`, `Fill`) |
| `Assets/Scripts/Rendering/ZoneLook/FrontRoomsZoneLookVolume.cs` | 9f202db7 | 4fe7cbd07d1b45728c039b9ec308b324 | the scene-placeable box (Add Component > FrontRooms > Zone look volume) |
| `Assets/Scripts/Rendering/ZoneLook` (folder) | - | 1b87eaf8d06b453cb2de8fcfe3863119 | |
| `Assets/Resources/Rendering/ZoneLook/FrontRoomsZoneLook_PoolClub`, `_PoolHall`, `_PoolLevel`, `_PoolDark` `.asset` | aed7d2a3, c6593682, cd1bae91, 91a1bcb8 | a8b90e68…, eb299b7d…, e6567319…, 9b3a2154… | the four profiles (written from `Fill` by the harness; a missing asset falls back to the same code values) |
| `Assets/Resources/Rendering/ZoneLook` (folder) | - | c28e154671d4b45e08f8b851a2134772 | |
| `Assets/Resources/Rendering/FrontRoomsPost_PoolClub`, `_PoolHall`, `_PoolLevel`, `_PoolDark` `.asset` | 79990efa, a0ff106e, cd59b2d0, 59ef1a63 | 3486aa35…, a39672bd…, 0441227a…, 02697765… | the four grades (URP volume profiles, loaded by `FrontRoomsPostStack.EnsureZoneVolume`, as Office and Run_B) |
| `Assets/Resources/Rendering/FrontRoomsSurface.shader` | 8f3587e0 → 37ccbb59 | (unchanged) | `FRHeightFog` after each `MixFog` in the forward pass (3 lines + a 17-line function); off unless a zone writes it |

All ASCII, LF. `FrontRoomsLook.cs` and `FrontRoomsPostStack.cs` stay as they are (§1.2 had them as "possibly"): nothing in the game's own path changes. Clone-only and never landing: the harness layer `Assets/Editor/PoolLook/FrontRoomsPoolLook.ZoneLook.cs` (995a963c; flags in §3.9) and the stand-in lamp kit copied from `W/proj_pooldir` (P1-A `ZL_HIDUP`, P2-A `ZL_NICHE`, their meshes, materials, textures, `LampOpt_Trim.mat`, `ZoneLampsGlowAdd.shader`; same GUIDs as there, 27 entries listed with sha1 in `W/pool_look/tmp/zl_kit_copy.txt`).

### 3.2 How it works

- **Inert without a volume.** Nothing is hooked until a `FrontRoomsZoneLookVolume` is enabled; the last one disabled unhooks. With none, no value is read or written: the game, Level 0 and the Office floors run exactly as today.
- **Per render context, at that context's camera.** `RenderPipelineManager.beginContextRendering` (before URP reads `RenderSettings` and culls): capture what the context finds, weigh every look volume at the camera's volume trigger (else the camera), write the blend; `endContextRendering`: write back what was found. So outside rendering `RenderSettings` always hold the game's values (a scene saved in the editor never keeps a zone's look) and a camera outside every zone renders Level 0's values to the bit. The camera of a context is the one the player sees: an on-screen camera wins over an offscreen one (the exit cut's cross camera renders to a texture one depth below the view in the same context), then Game and Scene view cameras, then the highest depth; preview cameras never count. A context of an earlier frame that never ended (an exception inside a render) is unwound at the next context, so a lost end can never leave a zone's values behind; tools that call `ApplyNow` hold their level until `RestoreNow`.
- **The blend is URP's.** Weight 1 inside the box, `1 - (d / blend)²` within `blendDistance` (2.5 m) outside it, volumes in ascending priority each lerping what is below; the grade is a child URP volume over the same box, blend and priority, so the light and the grade never disagree at a border. The deeper zone wins where two meet (club 1.0, hall 1.05, level 1.1, dark 1.15, all above the Office grade's 1, which never meets them): its look reaches 2.5 m into the shallower zone, so the change starts before the player steps through (border walk: 1.9 m before the club-to-hall arch the hall weighs 0.42, at 0.9 m 0.87, in the arch 1.0). The reflection cube keeps its own crossfade (`FrontRoomsZoneReflection`; `Refl_Pool` is the Submerged stage's).
- **What a zone writes.**
  - The ambient: `RenderSettings.ambientProbe` in ambient mode Custom, the captured probe lerped toward the zone's trilight probe. Unity ignores a probe set from code in Trilight mode (measured, run `zl_v1`: the probe read back unchanged and the frames did not move), so the mode switches to Custom while a zone weighs in and back after. The zone's trilight is turned into a probe by Unity's own projection, measured from Unity 6000.6.3f1's rebuild (each colour alone at 1.0): sky (1/6, L1y +0.2722, L20 -0.0260, L22 -0.0781), equator (2/3, 0, +0.0521, +0.1563), ground = sky with L1y negated. It predicts Unity's probe of Level 0's trilight to 7.5e-9 and of PoolHall's to 3.7e-9, and every run checks the constants again (`zl_basis.txt`; the analytic `AddDirectionalLight` projection tried first had the wrong sign and scale).
  - The haze: `fogColor` and `fogDensity` (exp², as Level 0).
  - The height fog: the globals `_FR_HeightFog` (density per metre at the water, water height, 1 / scale height) and `_FR_HeightFogColor`, read by `FrontRooms/Surface` (§3.4).
  - The fill: the scene's directional light ("Soft ambient direction", the bounce of the room's lamps) times the zone's `fillScale` when that is not 1 (only the dark zone: 0.2), given back per context.
- **Exact give-back.** Every value is restored as found, the fill per nesting level. Checked in every harness run (§3.7).

### 3.3 Entry points

- **`FrontRoomsZoneLookVolume`** (`[ExecuteAlways]`): `zone` (PoolClub, PoolHall, PoolLevel, PoolDark), `center`/`size` (local box), `blendDistance` (2.5), `priority`, `weight`, `hasWater` + `waterLevel` (local y; the height fog sits on it), optional `profileOverride` for look-dev. It makes its grade child (`Post / zone <zone>`, through `FrontRoomsPostStack.EnsureZoneVolume`) once and keeps it matched to the box. Gizmo: the box, the blend margin, the water plane. The structures scene (RULES 5b, scene 4) shows the look by placing one per pool room with its water height; nothing else to call.
- **`FrontRoomsZoneLook.Attach(FrontRoomsMapWorld)`** and **`Bind(FrontRoomsMapWorld, Transform viewer)`**, the agreed entries (bathroom-005). The map exposes no pool zone yet (C1), so `Attach` finds none and does nothing (it counts the look volumes under the map: 0 today); `Bind` also keeps the viewer for the queries that run outside rendering (`ZoneAtViewer`: the later stages' lamp rigs and water). Rendering needs neither: each context is weighed at its own camera, in the game, the Level Designer and the editor alike.
- **`AddMapZone(holder, zone, localBounds, blendDistance, waterY)`**: one look volume over a rectangle of pool cells under a chunk's holder (NaN = a dry zone), at the zone's default priority. **The map contract** `W/apply/pool_zone_look_contract/` (README + `CONTRACT_map.diff`, owner 关卡设计, never applied by this line) calls it from the chunk build after `AddZoneGrades`: rectangles of cells of one pool look zone, PoolClub's Tall cells as PoolHall, by C1's theme names so it compiles and stays inert today; C2 later passes the module's `waterY` (until then the height fog is off in the game; ambient, haze and grade work). Base main `b71b023f` (关卡设计's H2 + H6 hooks, 22:29), patched `7c115d21`, `patch --dry-run` clean.
- Also public: `ZoneAt(position, out weight)`, `ZoneAtViewer`, `Evaluate(...)` (the blend without writing), `ApplyNow`/`RestoreNow` (tools that read `RenderSettings`), `Profile(zone)`, `ReloadProfiles`, `TrilightProbe(sky, equator, ground)`, `Lamps(zone)` (§3.5).

### 3.4 The values

Iteration 1 started from `16` §5's clone values for the whole family; every later value was judged on the harness renders against `16` §3 (the colour script) and F1, not taken as a spec. Colours gamma, as the Lighting window and `RenderSettings` take them.

| | Level 0 (TODAY) | 16 §5 start | PoolClub | PoolHall | PoolLevel | PoolDark |
|---|---|---|---|---|---|---|
| Ambient sky (floors face it) | .20/.19/.15 | .15/.20/.22 | .19/.20/.20 | .30/.33/.34 | .27/.30/.31 | .075/.083/.045 |
| Ambient equator (walls) | .26/.24/.17 | .17/.23/.24 | .23/.245/.245 | .24/.27/.28 | .28/.31/.32 | .092/.10/.055 |
| Ambient ground (ceilings) | .40/.36/.24 | .21/.28/.28 | .32/.335/.33 | .30/.33/.33 | .38/.41/.41 | .14/.155/.085 |
| Haze colour, exp² density | .16/.15/.11, .014 | .11/.16/.17, .010 | .14/.155/.16, .012 | .14/.17/.18, .009 | .20/.23/.24, .008 | .065/.07/.032, .014 |
| Height fog: density /m, scale, colour | - | - | off (no water) | .015, 0.7 m, .42/.48/.50 | .020, 0.6 m, .52/.58/.60 | .030, 0.5 m, .13/.155/.10 |
| Fill ("Soft ambient direction") | ×1 | - | ×1 | ×1 | ×1 | ×0.2 |

The grades (`FrontRoomsPost_Pool*`), each over the film look `FrontRoomsPost` (white balance +9, tint -7, post-exposure +0.15, contrast -6, saturation -8, warm shadows .97/1/.94 and highlights 1/.99/.93, lift 1/.99/.94 +.035, warm vignette .05/.045/.03, warm bloom tint 1/.93/.78). An override replaces the film look's value inside the zone; lens distortion, grain, chromatic aberration, tonemapping and bloom strength stay the film look's.

| | 16 §5 start | PoolClub | PoolHall | PoolLevel | PoolDark |
|---|---|---|---|---|---|
| White balance, tint | -12, -2 | -2, -3 | -8, -2 | -5, -1 | -72 (about 3200 K; -90 is 2800 K), 0 |
| Post-exposure, contrast, saturation | +0.45, -4, +4 | +0.25, -5, -6 | +0.45, -4, 0 | +0.55, -6, -2 | +0.40, -4, +4 |
| Shadows (SMH), midtones and highlights neutral | - | .98/1/1 | .98/1/1 | .98/1/1 | neutral |
| Lift (LGG), gamma and gain neutral | - | 1/1/1 +.035 | .99/1/1 +.03 | .99/1/1 +.03 | 1/1/1 +.025 |
| Vignette colour | - | .04/.045/.048 | .035/.045/.05 | .035/.05/.055 | .01/.02/.022 |
| Bloom tint | - | .98/1/1 | .96/1/1 | .95/1/1 | the film look's (its only glows are the 2800 K lenses) |

How the values got there (runs in `W/pool_look/renders/`, stage journal for the detail):
1. `zl_v1` (21:32), 16 §5's values: the measured basis replaced the computed one; two runtime defects found and fixed (the Trilight-mode probe above; `EnsureZoneVolume` finds its child by a name holding '/', which `Transform.Find` reads as a path, so a second call made a second grade volume: the look volume now finds its own grade first, a latent quirk of main's helper that main never triggers).
2. `zl_v2`/`v2L` (21:38): too blue and too saturated (the zone grade's own cool shadow and lift tints and a cyan ambient stacked on white balance -12): white balance -8 and saturation 0 in the hall, greyer ambients, club -2; the dark zone's ambient made a warm olive before the grade, computed through URP's white-balance matrix to land teal-black at -72.
3. `zl_v3`/`v4` (21:44-21:50): the hall under P1-A still dim (S1 mean 115 against TODAY's 135): ambient up, P1-A ×2.2, exposure +0.45; the dark zone read navy because the scene's neutral fill turned blue under -72 and lit the room evenly, which dead overheads cannot: `fillScale` 0.2; the level's ground term up and white balance -5. The phone tier's harness rebuild left the lamps on phone meshes for every later frame (fixed in the harness at 21:50; TODAY's S1-S7 were never affected).
4. `zl_v7`/`v7L` (22:43, the relay): the P2-A stand-in's spots lit the walls above the water (§3.5), and once that was cut the dark zone above the water lost the light that had stood in for the water's diffuse glow: its ground term ×1.5 and wall term ×1.15 (the water's glow is what ceilings and walls face at night; the Submerged stage's thrown light adds the caustic pattern on top, and its energy has to come out of this term then). The hall's niche light moved to 17 §4.3's 4 m value (§3.5).
5. `zl_v8`/`v8L` (22:50): the film look's warm bloom tint, left in every pool zone, overridden to neutral-cool in the club, hall and level. Final.

### 3.5 Lamp presets (16 §6 item 2)

`FrontRoomsZoneLook.Lamps(zone)` returns the preset the lamp kit path takes per zone: the rigs, the light and lens colours (gamma, as `Light.color`), intensity multipliers, and the wet-niche colours. The rigs are switches until Red picks P1 and P2 (`zone_lamps/30_for_red.md` §2.2-2.3): `FrontRoomsZoneLook.PoolHallOverhead` (MapTroffer, WallBoxP1A, LowBayP1B, VaporTightP1C, Dead; working pick P1-A) and `PoolUnderwater` (None, NicheP2A, NicheP2B, PortP2C; working pick P2-A, 300 W every 3 m, 0.7 m under the water).

| Zone | Overheads | Light | Lens | × | Underwater | Niche lens | Niche light | × |
|---|---|---|---|---|---|---|---|---|
| PoolClub (and the hall's soffit row) | the map's troffer | .93/.97/1 cool white | .93/.97/1 | 1.0 | none | - | - | - |
| PoolHall | P1-A indirect metal-halide wall boxes (the map's troffers off inside the rig, C7) | .90/.97/1 | .90/.97/1 | 2.2 (on the stand-in's 9 + 5.5 per box) | P2-A | 1/.80/.58 (2800 K) | .82/1/.78 | 0.8 |
| PoolLevel | the map's lamp, recoloured (its valance and flush-lens kit is later) | .90/.97/1 daylight | .88/.96/1 | 1.2 | none (17 §7.9's hidden compact lamps are the Submerged stage's) | - | - | - |
| PoolDark | dead | - | - | 0 | P2-A | 1/.80/.58 | 1/.92/.54 before the 3200 K grade | 3.0 |

Against 16's list: the club and hall colours are 16's. The level's light is .90/.97/1 rather than .86/.96/1, because under the zone's own cooler balance the bluer lamp went blue (`zl_v2`). The niches are warm at the lens (the one warm thing, 16 §3) and their light carries the colour it has after its own path through the water, since nothing applies absorption to a light's path yet: in the hall 17 §4.3's value after 4 m at the overheads' 4200 K balance, the basin's mean lit path (#D2FFC7: mint, kept subtle), which with the Submerged stage's view-path tint (#8AD0E7 at 1.07 m) lands near 16's ".55/.92/1 seen through water" (the 2 m value .90/1/.72 of iteration 3 read yellow); in the dark zone a warm light the 3200 K grade turns pale aqua (the floor pools measure .78/1/1 on screen in `zl_v8L` S6; the water stages' view-path tint deepens it). When the Submerged stage applies absorption along a submerged light's path, the niche light becomes the lens colour and the shader does the rest.

**The water-plane rule for wet niches** (found here, for the production kit too): a ray leaving a niche above the horizontal meets the surface from below at more than 48.6° from the vertical and is turned back down (total internal reflection; with the stand-in's 110°/40° cone tilted 12.4° down that is 29.4 % of its output, and none of it steep enough to leave the water). So no direct niche light reaches anything above the water. The stand-ins' spots did light the walls above the water (`zl_v5L`: the blooms on the dark zone's walls); the harness now gives each niche spot a water-plane cookie (rays above the light's horizontal cut, soft from -1° to +2.5°) and ×1.417 for the light the surface returns into the basin. What does leave the water is the Submerged stage's thrown light.

The lamp kit path: `FrontRoomsLampKit.ForCell` / `BuildMapLamp` choose a family per cell by theme and height; once C1 gives the pool themes, a pool cell asks `FrontRoomsZoneLook.Lamps(lookZone)` for its rig and colours, and C7 turns the map's troffers off inside a P1 rig. The production P1/P2 kits (hero LOD0-LOD2, phone meshes, their lamp logic: flicker, dim, dead, Relay warning, blackout) come after Red's picks; until then the stand-ins show the shapes and the light.

### 3.6 Per tier

| | Desktop (Ultra, High) | Phones and iPad (handheld) | WebGL |
|---|---|---|---|
| Values | the same | the same (S5: the phone tier at Medium reads as S1) | the same |
| Ambient, haze, fill | `RenderSettings` per render context: no GPU cost | same | same |
| Grade | URP's own LUT pass of the film look: no pass added | same | same |
| Height fog | about 20 ALU in `FrontRooms/Surface`'s forward pass behind a uniform branch, run only where a zone with water weighs in | same | same |
| Lamp presets | stand-ins: hall 29 lights, dark zone 20, none shadowed | phone meshes of the stand-ins; S5 32 lights at Medium | not rendered here |

Rule 5: no WebGL or phone branch is needed, since nothing is added per tier. Not checked here: the shader's iOS and WebGL compiles (the clone's batch compiled it for Metal only; the combined test's builds compile the rest), and frame time on any tier (rule 25; a shared machine would be UNDER LOAD anyway).

### 3.7 Checks (every value from `zl_v8`/`zl_v8L` unless named)

- **Calibration:** the runtime's basis equals Unity's rebuild (max difference 0); prediction errors 7.45e-9 (Level 0) and 3.73e-9 (PoolHall).
- **Give-back:** RESTORE EXACT after the frames (14 contexts applied, 14 restored); OUTSIDE (-500, 1.6, -500): nothing written; INSIDE the hall: probe, ambient mode (Trilight → Custom), fog colour and density and both height-fog globals change (height fog (0.015, -0.12, 1/0.7)), then RESTORE EXACT.
- **Off = TODAY:** with every look volume off, the six desktop cameras rendered again (`_off`) equal the same cameras rendered before any look frame (`_pre`) byte for byte, 6 of 6 (grain and dither off for these frames only, since both advance per render); S1 rendered twice in a row is identical (the harness's own repeatability). All grade volumes off with them.
- **The shader term is inert:** TODAY rendered with the height-fog term in the shader (`today_v8`) equals TODAY with the base shader (`today_v7`) and the baseline (`today_v4`) on all eight frames, 0 pixels different. (`today_v6`, run by the first agent, differed from `today_v4` in about 84 % of the pixels by 1-2 levels: a film-grain phase of that run, since URP's grain draws `UnityEngine.Random`; not the shader.)
- **Gate (rule 25),** 22:51, `W/pool_look/zl/tmp/gate.zsh g2` (each override its own zsh array element; the response files read): Roslyn against main with the three runtime files as overrides, macOS and iOS: COMPILE OK, 0 errors, 3 overrides; with the map contract's patched `FrontRoomsMapWorld.cs` as a fourth: COMPILE OK on both, 4 overrides. The shader compiled in every Unity run in the clone (0 shader errors, 0 `error CS` in the logs).

### 3.8 Reading the renders

| Cam | TODAY | ZONE (lamps TODAY) | ZONE+LAMPS |
|---|---|---|---|
| S1 hall deck | 135.7 | 146.0 | 128.0 |
| S2 level room | 117.1 | 132.9 | 137.4 |
| S3 club corridor | 126.3 | 130.2 | 133.0 |
| S4 hero waterline | 123.8 | 137.0 | 67.3 |
| S5 phone tier | 139.2 | 150.0 | 128.8 |
| S6 dark zone | 50.4 | 27.8 | 37.2 |
| S7 crouched | 133.7 | 149.2 | 154.0 |

Mean luma 0-255; no clipped pixel in any frame. Border walk (ZONE+LAMPS): B1 club 120.9, B2 (hall 0.42) 125.3, B4 in the arch 140.1, B7 in the hall 127.3.

Against `16` §3 and F1:
- **The yellow is gone** from every pool space (S1 RGB 142/136/109 → 144/147/137 with TODAY's warm troffers still in, 119/130/126 with the presets), and the four zones read apart: the club neutral, a touch cool and plain (F1's "cool grey"); the hall cool aqua-grey with P1-A's lit ceiling instead of the black lid of troffers; the level high key and cool white (the brightest space); the dark zone near-black teal where only the niches light the floor and the water glows aqua (Q4).
- **The colour-temperature descent** of 16 §0 shows along the route: Level 0's yellow (TODAY column) → the club's grey → the hall → the level's white.
- **Still TODAY's, so still wrong:** the tile is the warm cream glaze (the Tile stage), the water is the stills' milky first pass reflecting the warm Level 0 "Tall" cube, which turns the far water khaki at grazing angles in S1 and B4-B7 (the water and Submerged stages, `Refl_Pool`), and there are no caustics. S4 under P1-A (67 against 124) is dark because the stand-in's ceiling bounce sits 1.2 m from the walls and reaches the basin's inner faces weakly; P1-A's real lit ceiling is an area source. The dark zone above the water (S6, walls about 25-30 of 255) is lit only by the ambient until the Submerged stage's thrown light: the Renders stage checks it against rule 11 (darkness is never cover) with the Relay in frame.

### 3.9 Limits and open items

- The height fog lives in `FrontRooms/Surface` only: the water (FrontRooms/Water, next stages) takes the same `FRHeightFog`; URP Lit materials (the stand-in kits, today's water) do not. In the game it is off until C2 gives the map a module's water height (the contract passes NaN); in the structures scene a volume carries its water level. In these frames it is subtle by design: about 8 % over the hall's 15 m view down the basin, 10-19 % along the level's ankle water from a crouch, 19 % across the dark zone at the waterline, nearly nothing 2 m above the water.
- Not verified here: the editor's Scene view with a volume (the batch has none; the harness renders in edit mode through the same hook), whether writing `RenderSettings` in edit mode marks a scene dirty, and contexts with several cameras (the harness renders one camera per context; the camera choice is reasoned from URP's order, not rendered).
- The P1/P2 rigs, their kits and their lamp logic stay Red's and the later kit's; the presets carry them switchable.
- 16 §5 had the grade on one `FrontRoomsPost_Pool` profile; here each look zone has its own (club, hall, level, dark), as 16 §3 lists four looks.

### 3.10 For the next stages

- **Package (`pool_zone_look`):** the 3 runtime files, 2 folder metas, 4 profile assets and 4 grade assets of §3.1 with their metas, `FrontRoomsSurface.shader` (base 8f3587e0, unchanged in main since 17:00). The contract stays a contract (`W/apply/pool_zone_look_contract/`).
- **Harness:** `-plZone pool` (the look volumes on TODAY's base, the assets rewritten from code each run), `-plLamps pool` (the presets on the stand-in kits), `-plCalibrate`, `-plOffCheck`, `-plBorder` (§3.7). A later stage keeps `-plZone pool -plLamps pool` and swaps its own layer, for example `-plZone pool -plLamps pool -plTile <v>`.
- **Submerged:** absorption along a submerged light's path (then the niche light = its lens colour), the thrown light from the niches (its energy out of PoolDark's ground term), `Refl_Pool` and its dead-lamp twin (the warm cube's khaki), `_WaterAbsorbScale` per zone (Q1).
- **Superseded runs** (inside `W/pool_look`, deletion candidates for Red): `renders/zl_v1`-`zl_v6L` (`zl_v8`/`v8L` are final, `zl_v7`/`v7L` the run before the bloom tint), `renders/today_v6` (grain phase); keep `today_v7`, `today_v8` (the inertness proof); `zl/tmp/roslyn_c1`-`c9`, `gate_*`, `g1_*` (`g2_*` is the gate of record), `zl/tmp/view/`, `zl/contract/base`, `patched` (the draft against `a348751f`).

## 4. Tile

Stage 4 of the workflow, 2026-11-03 22:57 to 2026-11-04 06:1x, one agent on account G (the relay of `wf_48851110-b3c`). **Nothing landed in main:** main was read only, no Unity ran on main, nothing was downloaded (web pages read as text only), no package was built (the Package stage builds `pool_tile`). The stage lost about six hours of wall time to the machine: it idled from about 23:35 to 00:16, then ran at a load average of 500-840 (another line's iOS simulator, `log` and `diagnosticd`), so `tile_zl` took 58 minutes and `tile_v1` waited 4.5 hours to start; the harness itself takes 2-20 s. Frame times are not measured (rule 25; they would be UNDER LOAD anyway).

The asks: `16` §6 item 4, "Tile set: white glazed 6 in tile, an aqua waterline course, 1 in mosaic for basins, pristine in the level and worn in the club (P5)"; Red, 10-31 17:19: "我希望底色不应该是偏暖的米白，而是和dream pool更相似的瓷白" (BH line 192); the stairs, Red's pick of 10-31 17:24 (recorded as a choice, no verbatim text on file): porcelain white treads and risers, only a dark nosing strip marks each step, no teal band (BH line 193; the level-space pass set the nosing at 50 mm on the tread and 25 mm down the riser in deep blue-grey 0.24 / 0.27 / 0.31); D8, the colour and material sheet, answered in the batch "D2–D12 按推荐" (BH line 36), whose recommendation was A, "让游戏视觉出 6 个方案的对比图再选" (`20_for_red.md` D8). Red's 17:15 words on the slide ("让瓷砖作为墙壁去托举滑梯") are a structure question for the structures scene; the tile walls there take these materials.

![Tile](sheets/70_tile.jpg)

Sheet `sheets/70_tile.jpg` (3838 × 9026, Shot format, chip WAIT-RED: D8's colours stay Red's; columns TODAY = `tile_today` (all layers today; S1-S7 byte-identical to `today_v4`/`today_v8`), BEFORE TILE = `tile_zl` (section 3's zone look and lamp presets with today's tile: the light every TILE frame is rendered in), TILE T1 = `tile_v2` (the recommendation); rows S1-S10 and COST; every label checked at full size: ALL LABELS FIT, `W/pool_look/sheets/70_tile_fit.txt`).

![Tile options, D8](sheets/70_tile_options.jpg)

Sheet `sheets/70_tile_options.jpg` (3835 × 2294, chip WAIT-RED; columns TODAY, T1 REC, T2-T6, each one change from T1; rows S1, S3, S4, S8, COST; ALL LABELS FIT, `W/pool_look/sheets/70_tile_options_fit.txt`). Frames: `W/pool_look/renders/tile_v2/` (`S1`-`S10` and `<cam>_T1`-`T6`), `tile_today/`, `tile_zl/`.

### 4.1 Research: only the gaps

Reused, not redone: `10` §2.2 rows 1-4 and §2.5 (shell white or light blue, dark lane lines with T-ends and end-wall targets, glazed wall tile with cove and bullnose, the recessed gutter, the deck non-slip; S40, S43, P1-P3), `15` §4 (6 in and 1 in glazed tile; pristine in the level, worn in the club), `16` §0 item 2 and §5 (the palette's grey-green grout; the course .40 / .76 / .77), `18` §3 (the dark zone). New on 11-03, read as text, nothing downloaded; source ids local to this section, for `SOURCES.md` (the parent appends them):

| # | Source | Date | What it gives | Status |
|---|---|---|---|---|
| T1 | S43 again: Hoeganaes, *Architectural ceramic tile*, https://archive.org/stream/architecturalcer00hoeg/architecturalcer00hoeg_djvu.txt | 1965, PERIOD | Glazed wall tile 3⅝ × 7⅝ and 2 × 10 in; bullnose and cove base; non-skid faces (waffle, pebble, silicon carbide through the body; sanded glaze); coverage tables at ⅛ in joints; "special tile shapes for large and small pools"; colours include Turquoise, Cobalt Blue, Skyline Blue | READ (fetch summary of the OCR) |
| T2 | S40 again: TM 5-662 (1986), https://archive.org/stream/ost-military-engineering-tm5_662/tm5_662_djvu.txt | 1986, PERIOD | 2-8 white, light blue and aqua blue walls and bottom, dark colours discouraged; 4-4 a falling level "causes a scum ring"; 3-6 about 6 in from water to deck at skimmers; 2-11 non-slip treatments | READ (first half of the text) |
| T3 | Old House Online, "Guide to 20th Century Bathroom Tile", https://www.oldhouseonline.com/kitchens-and-baths-articles/guide-20th-century-bathroom-tile | n.d. | The 4 × 4 (4¼ in) square became the ubiquitous wall tile from the 1950s | READ (search excerpt), secondary |
| T4 | Pacific Tile & Porcelain range, https://wellstile.com/?p=11521 | 1936-1960 | 4¼ in square × ¼ in thick | READ (search excerpt) |
| T5 | Daltile Semi-Gloss sell sheet, https://designerpages.s3.amazonaws.com/assets/78007596/DAL_SemiGlossMatte_SS.pdf | modern, LATER | 4¼ and 6 × 6 in wall tile, 5/16 in thick; radius bullnose; sanitary (round-top) cove base | READ (search excerpt) |
| T6 | American Olean unglazed mosaics sheet, https://www.scribd.com/document/638979007/AO-UnglazedMosaics-SS | modern, LATER | 1 × 1 and 2 × 1 in unglazed porcelain for pool linings; the recurring joints give the slip resistance | READ (search excerpt; the page itself would not render) |
| T7 | Florida Building Commission, staff analysis DS2017-070, https://www.floridabuilding.org/FBC/commission/FBC_1217/DEC_Statements/STAFF_ANALYSIS-DS2017-070.htm | 2017, LATER | A waterline band at least 4 in high of tiles at least 1 in, at most 12 in high if dark | READ (search excerpt) |
| T8 | City of Winnipeg, tender 284-2008 addendum 1, https://legacy.winnipeg.ca/finance/findata/matmgt/documents/2008/284-2008/284-2008_Addendum_1/284-2008_Addendum_1.pdf | 2008, LATER | Black lane markings; a 4 in tiled line at the deep end's drop-off | READ (search excerpt) |
| T9 | Pool-care guides on waterline scale, e.g. https://www.flooringclarity.com/pool-tile-calcium-buildup-causes-prevention-effective/ | modern, low authority | A white calcium line where evaporation concentrates minerals; grey hard silicate scale; body-oil grime; scale wicks into grout | READ (search excerpts) |

What follows for the build:
- **Field tile 150 mm (6 in).** `16` §6 asks 6 in, and the period made it (T5); the most common square wall tile of the period was 4¼ in (T3, T4), kept here as the alternative (a new map set, not rendered). 150 mm divides 192 m, so the room stream's rebase never moves a joint.
- **Joints 3 mm** (⅛ in, T1's coverage tables) on wall tile; **2 mm** on the mosaic; **4 mm** on the deck. At 3 m a pixel is 3-4 mm here, so every joint also carries tone: the grout's own colour, its recess and the cushion edge's shading are baked into the data map (relief into tone, 1.2 above).
- **Basin: 1 in unglazed porcelain mosaic** (T6; `10` row 1's "unverified" small mosaic), on 300 mm face-mounted sheets whose joints show a little. Unglazed, so it is matte and grips under bare feet.
- **The waterline course:** one 150 mm row of the aqua glazed tile, the row the water sits in (T2 white or aqua; T7, later, asks at least 4 in; `16` the hinge colour).
- **Deck: unglazed abrasive tile with silicon-carbide grit** (T1's non-skid faces), porcelain white; red-brown quarry stays an option (D8 B).
- **Coping: a full bullnose,** 30 mm thick, its nose (r 15 mm) 20 mm over the basin wall; the wall's top course a bullnose cap (T1, T5).
- **Stairs:** treads and risers white, a dark L nosing: flat 42 mm + r 8 mm + flat 17 mm (seen: 50 mm from above, 25 mm from the front), three safety ribs on the tread leg.
- **The club's waterline shows a scale line** (T2 4-4, T9); the pool level shows none (pristine, `15` §2).

### 4.2 What was built (in the clone `W/proj_unlock`; the Package stage carries it to main)

| Path (under `Frontrooms3D/`) | sha1 | GUID (`.meta`, fresh: none of main's 3,222) | What |
|---|---|---|---|
| `Tools/lookdev/pool_tile/gen_pool_tile.py` | 61f41010 | - | the generator (numpy, Pillow; deterministic): six map sets, both tiers, every `.meta`, the 26 materials, `pool_tile_manifest.json` |
| `Assets/Resources/Surfaces/Pool/Textures/PoolTile_<set>_{D,N,S}[_Phone].png` | manifest | 36 GUIDs, manifest | sets Field, Mosaic, Deck (2048² / 1024²), Cap (2048 × 256), Coping (2048 × 512), Nosing (2048 × 128); linear; trims clamp in V |
| `Assets/Resources/Surfaces/Pool/Prop_PoolTile_<kind>[_Phone].mat` | manifest | 26 GUIDs, manifest | 13 kinds × 2 tiers (4.4) |
| `Assets/Resources/Surfaces/Pool`, `.../Pool/Textures` (folders) | - | 2 | |
| `Assets/Scripts/Rendering/PoolTile/FrontRoomsPoolTile.cs` | 95e63dbc | edc6429c9747495bbe9076893ba70b89 | the runtime: `Get(kind)` (tier from `FrontRoomsTrofferKit.PhoneTier`), `ForWater(kind, waterY)` (one instance per kind, tier and water height to the mm), `MoveWater`, `ReleaseInstances` |
| `Assets/Scripts/Rendering/PoolTile` (folder) | - | 9431cc2f3c0e4bd89978d43fee95c4e4 | |
| `Assets/Resources/Rendering/FrontRoomsSurface.shader` | 37ccbb59 (zone look) → cef7facc | (unchanged) | `_FR_TILE` (a `shader_feature_local_fragment`) and `FRTileSurface`; the wet band `FRWetBand` for every material, behind a uniform branch on `_WaterY`; 10 new per-material values in the CBUFFER (+134 lines over the zone look's version; main still 8f3587e0) |

The manifest is `Assets/Resources/Surfaces/Pool/Textures/pool_tile_manifest.json` (every file's size, sha1 and GUID). The PNGs weigh 44.1 MiB in the repository (35.6 desktop, 8.5 phone). All text LF, ASCII. Clone only, never landing: the harness layer `Assets/Editor/PoolLook/FrontRoomsPoolLook.Tile.cs` (b997130a, GUID 2038d634…) and its hooks (4.5).

### 4.3 How it works

- **The maps are data, the material holds the colours.** `_D` (linear): R the tone, the albedo multiplier 0.75 + 0.5 R (the glaze's lot and mottle, the cushion edge's shading; the grout's grain and its recess shadow), G the glaze's thickness (1 on the face, about 0.45 at the rounded edge, where the body shows), B grout. `_N`: OpenGL normal in the surface's UV frame (the cushion edge r 1.8 mm, the grout struck concave 0.7-1.3 mm under the face, per-tile lippage ±0.1 mm, set ±0.1°, a convex bow of 0.05-0.2 mm, the glaze's orange peel at 2.5 mm and its swell at 12 mm: what makes a lamp's reflection wobble from tile to tile). `_S`: R smoothness (bright glaze 0.935, its edge 0.90, unglazed mosaic 0.47, abrasive deck 0.31 with glassy grit at 0.6, grout 0.24), G cavity, B dirt potential (patchy grout, the horizontal joints' ledges, the crazing network on a third of the tiles, edges, specks), A chips. Maps are supersampled ×2 and seamless; each holds 64 tiles (Field, Deck), 576 chips (Mosaic) or 4-8 units (trims).
- **`_FR_TILE`** (`FRTileSurface`): the colour is `lerp(_EdgeColor, _BaseColor, glaze thickness)` × lot, the grout `_GroutColor`, both × the tone; the dirt `_DirtColor` as far as `_TileWear` (0 pristine, 1 club), which also shows the chips (matte bisque) and dulls an old glaze. **Every tile is its own:** a hash of its cell gives a shade jitter (`_TileGrid.w`: 1 % in the club, 0.4 % in the level), now and then a tile from another lot, bluer or creamier (`_TileLot`: 2.5 % of the club's tiles), and a set of ±0.18° (`_TileGrid.z`), so no repeat of the map shows and reflections break up tile by tile. The cell index wraps every 192 m, so a rebase never re-rolls a tile. On walls a little of the joints' relief goes into the tone (`_TileShade`, lamps overhead). Crazing and chips are hairlines and bites of a few texels: they show at the hero distance and vanish into the mip average beyond about 1 m.
- **The wet band** (`FRWetBand`, any FrontRooms/Surface material, off while `_WaterY` is below -999, the default): under the water porous parts soak and darken (grout, unglazed faces; porosity from the grout channel, else from smoothness, as Lagarde does); above it a band `_WetBand.x` high with a soft top `_WetBand.y` that wanders along the wall (`_WaterLine.z`) holds a water film (gloss up, grout recess filled); the meniscus, the 2.7 mm skin water draws up a wall, catches the light above (`_WetBand.w`, faded by its size on screen so it never shimmers); a club's pool shows its scale line (`_WaterLine.x`, `.y`; TM 5-662's "scum ring"). The water stages drive `_WaterY` (and may move it with the ripples): `FrontRoomsPoolTile.ForWater(kind, waterY)` hands out one instance per water height, `MoveWater` moves a water's line. Under the water the specular is the Submerged stage's (glaze under water reflects about a tenth of what it does in air).
- **Set-out.** Rows are world-planar on 150 mm from world 0; the map's pool floors at 0 and -0.15 sit on them. A floor off the rows (the dark zone's -2.52) takes a V offset on its wall tile so rows start at its floor (`_BaseMap` offset, the harness's datum); bands and caps belong on whole rows (course -0.15 to 0, club band 1.20-1.35, level band floor + 1.20 to 1.50). The stair and coping pieces carry their own UVs in metres, set out from each nosing and along each edge, so their joints line up (nosing joints on the riser joints).
- **Tiers.** The phone tier (handheld, WebGL players: the lamp kits' `PhoneTier`) takes `Prop_PoolTile_<kind>_Phone`, the same material on maps at half size; nothing else differs. The shader path is the same on every tier.

### 4.4 The values

Colours as the Inspector shows them (gamma). Every kind: `_TileGrid.z` set 0.18°, shade jitter 0.4 % (pristine) or 1 % (worn); grout .66 / .70 / .67 (light grey-green, `16` §0 item 2); dirt .36 / .33 / .27.

| Kind | Maps | Glaze (base / edge) | Wear | Wet band (height, fade, soak, meniscus); scale line | Where (harness) |
|---|---|---|---|---|---|
| Field | Field | .93/.95/.96 (Red's porcelain white) / .80/.81/.80 | 0 | .02, .04, 1, .75; none | pool level walls and ceilings, turn, dark zone, stair room |
| FieldClub | Field | same | 1 (lots 2.5 %) | .05, .08, 1, .75; .6 | hall wainscot, club corridor |
| Course | Field | .40/.76/.77 (`16` §5) / .70/.86/.86 | 0 | as Field | level and turn bands, dark band, the stair room's course |
| CourseClub | Field | same | 1 | as FieldClub | the hall's waterline course, the club band |
| Cap | Cap | white | 1 | .05, .08, 1, .75 | the hall wainscot's top course (2.25-2.40) |
| Mosaic | Mosaic | .92/.94/.95 unglazed | 0 | .02, .04, 1, 0 | flooded floors of the pool level |
| MosaicClub | Mosaic | same | 0.6 | .05, .08, 1, 0; .4 | the hall basin's floor and walls under the course |
| Lane | Mosaic | .10/.12/.16 near-black | 0.4 | .05, .08, 1, 0 | lane lines, T-ends, deep-end targets |
| Deck | Deck | .86/.88/.88 unglazed abrasive | 1 | (dry) | the hall deck |
| Tread | Deck (mesh UV) | same | 0 | .02, .04, 1, .75 | stair treads and landing |
| Riser | Field (mesh UV) | white | 0 | as Field | stair risers |
| Nosing | Nosing (mesh UV) | .24/.27/.31 / .42/.45/.50 | 0 | as Field | the stair's nosing L |
| Coping | Coping (mesh UV) | white; sanded top .62, bright nose .92 | 1 | .05, .08, 1, .75; .6 | the hall basin's W, N, E edges |

Macro wear (`_MacroTone` / `_MacroDirt`): .10 / .08 on the club's wall kinds, .12 / .12 on the deck, 0 in the pool level (pristine, as `15` §2's canon asks). `_FloorGrime` .25 on the club's wall kinds.

### 4.5 The harness (clone only)

- `-plTile pool` (T1, the recommendation) and `-plTile T2`…`T6` (D8): every tile slot gets an instance of its kind (the assets are never edited), slots at a water with its `_WaterY` (hall -0.12, pool level -0.03, dark zone -1.5, stair room -0.57). A pool value also builds what only a tiled pool has: the **bullnose coping** (mesh, mitred corners, 45 m, 114 tris) in place of TODAY's boxes, and the **lane lines** (four lanes of 2.25 m, lines 0.25 m wide on the lane centres from z 7 to z 19 with 1 m cross bars, 2 mm over the sloped floor; targets on the deep-end wall: a strip to 0.33 m under the water with a 0.5 m bar).
- `-plTileSweep T1,T2,…` with `-plSweepCams S1,S3,S4,S8`: the options differ only in material values, so one Unity start renders all of them (`<cam>_<option>.jpg`), after the run's own frames. The sweep's T1 frames equal the main frames within the grain (mean difference 0.8-1.0 levels, same means).
- **New for every value, TODAY included:** the stair space at x 576 (a room 6 × 12 m, ceiling 2.9 over a landing at 0, a straight flight of 8 risers of 0.175 and goings of 0.35 into water at -0.57 over a floor at -1.40: risers 1-3 dry, the waterline crossing riser 4 20 mm under its nosing, 5-8 under water; its own look volume (PoolLevel) and lamp cells), and three cameras: **S8** a 3 m read of a pool-level wall, square on (vFOV 60); **S9** standing on the stair's landing 1.8 m from the first nosing (vFOV 76); **S10** hero 0.35 m from riser 4, eye 0.07 m above the water, 14° off square (vFOV 60).
- **Set-out constants moved** onto the tile rows: the hall course from 0.155 to 0.15 deep, the club band from 1.25-1.40 to 1.20-1.35 (TODAY renders both sides of each split with one material, so its frames do not change); the dark zone's wall tile takes the V offset of 4.3.
- The phone tier swaps every tile renderer to the `_Phone` twins (S5) and back. `tile_cost.tsv` lists each map's format and size per tier.
- Runs: `tile_today` (`-plDiag`), `tile_zl` (`-plZone pool -plLamps pool`), `tile_v1` (+ `-plTile pool`), `tile_v2` (+ `-plTileSweep T1,T2,T3,T4,T5,T6`); launcher as in 2.6.

### 4.6 Numbers

| | Desktop | Phone tier |
|---|---|---|
| Maps | 18: three 2048² sets (Field, Mosaic, Deck) and three trims | the same 18 at half size |
| Texture memory | **55.0 MiB** in a player (BC7, 1 byte a texel with mips; the editor measures 110.0 MiB, counting its CPU copy) | 13.8 MiB BC7; about **6.1 MiB** as ASTC 6 × 6 on iOS (estimate) |
| Materials | 13 shared, plus one instance per kind and water height at run time (16 in these spaces: 4 at the hall's water, 3 at the pool level's, 3 at the dark zone's, 6 at the stair room's) | 13 shared `_Phone` twins, the same instances |
| Draws | no new renderer on any wall: each slot is one renderer with one material, as TODAY's single glaze was; kit pieces: 1 coping (114 tris), 1 lane-line mesh (hall), 3 stair meshes (treads, risers, nosings) | the same |
| Repository | 35.6 MiB of PNG | 8.5 MiB of PNG |
| GPU | the tile path samples the same three maps as the plain one plus about 40 ALU (six hashes, lerps); the wet band about 30 ALU where `_WaterY` is set, nothing elsewhere. Not timed (rule 25) | the same |

Mean luma (0-255) per camera, TODAY | BEFORE TILE | TILE T1: S1 135.7 | 128.0 | 125.9; S2 117.1 | 137.4 | 141.7; S3 126.3 | 133.0 | 135.6; S4 123.8 | 67.3 | 63.3; S5 139.2 | 128.8 | 127.8; S6 50.4 | 37.2 | 38.6; S7 133.7 | 154.0 | 156.1; S8 119.5 | 141.9 | 145.7; S9 157.6 | 182.2 | 185.7; S10 106.8 | 130.8 | 123.4. No clipped pixel in any TILE frame. Build cost (`tile_v2/cost.tsv`): hall 122 renderers, 35.9 k tris, 19 materials; stair 49 renderers, 5.6 k tris, 12 materials.

### 4.7 Checks

- **The shader is inert for every other material:** TODAY rendered with the tile layer and the wet band in the shader and with the stair space in the scene (`tile_today`) equals `today_v8` byte for byte on S1, S2, S3, S4, S6 and S7. S5 and the S2 lamp check differ by the dither and grain phase only (mean 0.6 levels): they render after three more cameras now.
- **Gate (rule 25)**, `W/pool_look/tile/tmp/gate.zsh g1` (the override its own zsh array element; the response files read): Roslyn against main with `FrontRoomsPoolTile.cs`, macOS and iOS: COMPILE OK, 0 errors, 1 override. The clone with the harness: COMPILE OK (`tile/tmp/roslyn_t2`). The shader compiled in every Unity run in the clone: 0 shader errors, 0 warnings on FrontRooms/Surface, 0 `error CS` (`logs/tile_*.log`). Unity rewrote none of the generated files (materials, maps and metas equal the manifest's sha1).
- **Assets:** 67 new GUIDs, none in main's 3,222; the three new folders are absent in main; every text file LF, ASCII.
- **Not checked here:** the shader's iOS and WebGL compiles (the clone's batch compiles Metal only; the combined test's builds compile the rest), GPU time on any tier.

### 4.8 Reading the renders

- **The pool family now reads as tile, and as the right tile.** Porcelain white, not cream (Red 17:19), on every surface the canon tiles; the grid reads at 3 m (S8, S3) without becoming a drawing: joints are thin grey-green lines that fade into the wall's tone past 6-8 m, as in a real tiled hall. The pool level (S2, S7, S8) is the canon's pristine white field with one aqua band; the club (S3, the hall) is the same tile with darker joints. The tile's gloss shows where it should: a lamp's reflection on the level's walls sits in one tile and stops at its joints, and the cushion edges glint along the joints (S7), each tile at its own small angle.
- **The hall (S1, S5)** has its period equipment in tile: the porcelain abrasive deck, the bullnose coping round the basin, the lane lines with T-ends and the targets on the deep-end wall, the cap course under the painted block. The basin still reads khaki, because TODAY's water (a constant-alpha teal reflecting the warm Tall cube) lies over it: the water stages' work, not the tile's.
- **Hero (S4, 0.35 m):** the coping's round nose with its joint, the aqua course with dirty club joints, the meniscus as a thin bright line at the water, the 1 in mosaic under the water. P1-A leaves this spot dark (section 3.8), so the aqua reads deep teal here; at S8 under the level's light it is the aqua of `16`.
- **The stairs (S9, S10):** from the landing the steps read as dark lines descending into the water (the nosing strips), white everywhere else; up close the nosing L with its ribs and joints lines up with the riser tile below, and the waterline draws its meniscus across riser 4 just under the nosing.
- **Crazing and chips** are there for the hero distance only, in the club; at S4's light they are barely visible on the dark course. A hero frame on white club tile would show them better; none of the fixed cameras has one.

### 4.9 D8: the choice that remains Red's

`20_for_red.md` D8 left the band colour and the deck open and promised a sheet. F1 and Red's porcelain white since narrowed it to: the grout, the course's hue, the deck. The options sheet shows each as one change from T1:

| Option | What changes | As the player sees it | Cost |
|---|---|---|---|
| **T1 REC** | grout .66/.70/.67 grey-green, aqua course .40/.76/.77, porcelain abrasive deck | a white tiled room whose grid you notice at arm's length and stop noticing across the hall; one aqua line where the water is; the deck as pale as the walls | the build |
| T2 | white grout .86/.87/.85 | the Dream Pool's seamless white; at 3 m the joints nearly vanish, the walls read as one surface | material value |
| T3 | mid-grey grout .50/.51/.50 | a period commercial installation; the grid reads hardest at every distance | material value |
| T4 | the course deep teal .12/.50/.52 (1990's teal) | a heavier, period line that reads darker than the water | material value |
| T5 | no course: the course rows in field white | the canon's white only; the water is the only colour | material value |
| T6 | red-brown quarry deck .56/.29/.20, grout .45/.44/.42 (D8 B) | a warm period floor round a cold pool; it brings back the warmth `16` takes out of the hall | material value |

**Recommendation T1:** the grey-green joint is the palette `16` chose for the fusion and keeps the tile legible at 3 m (Red's 1.2 rule) without turning the wall into a drawing; the aqua course is the one colour `16` lets through the turn into the pool level; the porcelain deck keeps the hall's floor in the same white as its walls. Its honest flaw: until the water stages, the basin's colour is still TODAY's khaki, so the course and the mosaic cannot yet be judged under real water. Any option is a change of material values in `gen_pool_tile.py`'s table, then one generator run.

### 4.10 Limits and open items

- **The water is TODAY's** in every frame; the basin's colours, the wet band's look under ripples and the meniscus's brightness against real water are judged again after the Water surface and Submerged stages.
- **Specular under water** stays the glaze's in air until the Submerged stage (glaze under water reflects about a tenth as much).
- **Set-out in the map** (owner 关卡设计): pool rooms' bands and caps on whole 150 mm rows from world 0, or a V offset per room as the dark zone takes here; the module data in `pool_modules.json` puts the club band at 1.25-1.40 and the dark floor at -2.52. A line for the map contract (Package stage), not a change made here.
- **Kit pieces are the harness's:** the bullnose coping, the stair's nosing L and the lane lines are built procedurally here; production pieces (a coping kit with gutter grates, `10` §2.2 row 4; the stair kits of `level_space/60`) take these materials and UV layouts (coping: U along the edge, V across the 0.34 m profile from the deck side; nosing: U along the step, V across the 0.075 m profile from the tread side).
- **4¼ in tile** (the period's commonest square) would be a new map set (about 18 MiB more on desktop); not rendered.
- **The coping's ends** at the beach are open (no end caps; no camera sees them).
- Frame time and memory on devices: not measured (rule 25).

### 4.11 For the next stages

- **Package (`pool_tile`):** `Tools/lookdev/pool_tile/`, `Assets/Resources/Surfaces/Pool/` (36 maps, 26 materials, manifest, metas), `Assets/Scripts/Rendering/PoolTile/FrontRoomsPoolTile.cs`; `FrontRoomsSurface.shader` cef7facc (shared with `pool_zone_look`: one payload, base 8f3587e0, unchanged in main since 17:00). The material assets keep `_WaterY` -1000 (no water): the map (C2) or a water component sets it through `ForWater`.
- **Water surface, Submerged:** drive `_WaterY` (and its ripple height) on the tile instances; take the specular under water; check the meniscus (`_WetBand.w`, now .75) and the scale line (`_WaterLine.x`, .6 in the club) against the real water.
- **Harness:** `-plZone pool -plLamps pool -plTile pool` is the base for the next stages; the TODAY columns for S8-S10 are `tile_today`.
- **Deletion candidates** (inside `W/pool_look`, Red deletes): `renders/tile_v1` (superseded by `tile_v2`), `tile/tmp/gen_test`, `tile/tmp/roslyn_t1`, `tile/tmp/preview_v1`, `tile/tmp/qa`. Keep `tile_today`, `tile_zl`, `tile_v2`, `tile/tmp/gate_g1_*`, `roslyn_t2`.

## 5. Ripples

Stage 5 of the workflow, 2026-11-04 06:00 to 07:05, one agent on account G (the relay of `wf_48851110-b3c`). **Nothing landed in main:** main was read only, no Unity ran on main, nothing was downloaded, no package was built (the Package stage builds `pool_water`). The machine ran at a load average of 13-41 on 16 cores during the Unity runs, so every frame time below is **UNDER LOAD** and indicative only.

The asks: `17` §7 item 3, "Ripple normals: a baked looping FFT sequence. Capillary–gravity dispersion, isotropic, λ from 5 mm to 30 cm, calm slopes 1–3°. Normal variance stored for distance roughness (anisotropic along the view). Disturbance raises amplitude locally"; item 4, "Rings: dispersive packets growing at 0.2–0.5 m/s with crests running through them. 4–16 by tier, mirrored at basin walls"; §3.3 (the scrolling-texture look and how to avoid it), §3.5 (calm, then uneasy; Reduce Motion; ≤ 3 Hz). Sizes from §6.1: desktop 512² × 64 frames, handheld 256² × 32, WebGL 256² × 16-32. No Red decision is open in this stage: the sheet's chip is PENDING (its VL row comes with the line's daily batch).

![Ripples](sheets/70_ripples.jpg)

Sheet `sheets/70_ripples.jpg` (3840 × 4979, Shot format, chip PENDING; every label checked at full size: ALL LABELS FIT, `W/pool_look/sheets/70_ripples_fit.txt`). Rows A: six frames 1/30 s apart of one held view, the control (today's stand-in map scrolled the usual way, `17` §3.2 row 1) against the new ripple, as normals under one grazing lamp and as that lamp's reflection; B: one turn at six headings; C: the rings clip; D: 0.4 m, and the phone tier against the desktop; E: COST. Silent videos (h264, no audio stream; Shot header on every frame): `video/70_ripples_turntable.mp4` (12 s: holds 3 s, then one turn), `video/70_ripples_rings.mp4` (12 s: drip, footstep, wading, plunge, walls), `video/70_ripples_close.mp4` (2 s at 0.4 m), `video/70_ripples_phone.mp4` (6 s). Masters at 1920 wide: `W/pool_look/rip/video/`. Frames: `W/pool_look/rip/renders/rip_v2/` (desktop clips), `rip_v3/` (phone tier, timing).

### 5.1 What was built (in the clone `W/proj_unlock`; the Package stage carries it to main)

| Path (under `Frontrooms3D/`) | sha1 | .meta GUID | What |
|---|---|---|---|
| `Tools/lookdev/pool_water/gen_pool_ripples.py` | 165baa58 | - | the generator: FFT bake, sheets, metas, stats, QA (`--qa <dir>`); 7 s, deterministic (same bytes every run) |
| `Tools/lookdev/pool_water/ring_model.py` | 75373242 | - | the ring model's constants, Airy fit and calibration, checked against the exact solution |
| `Assets/Resources/Rendering/Water/FR_Ripple_Fine.png` | 2ee03f5b | ed818fd5 | desktop fine cascade: 64 slices of 512² as an 8 × 8 sheet (4096², 26.3 MiB) |
| `.../Water/FR_Ripple_FinePhone.png` | a80fafe2 | 10bf5eaf | phone tier (handheld and WebGL) fine cascade: 32 slices of 256², 8 × 4 sheet (3.8 MiB) |
| `.../Water/FR_Ripple_Long.png` | f9fefd2f | 931f1523 | long cascade, every tier: 128 slices of 128², 16 × 8 sheet (3.5 MiB) |
| `.../Water/FR_Ripple_Stats.json` | 59f49793 | e7e2db4d | per cascade: encoding, variances, frame-blend loss, the footprint table, sizes, sha1, GUIDs |
| `Assets/Resources/Rendering/Water.meta` (folder) | - | 2b21685d | |
| `Assets/Scripts/Rendering/Water/FrontRoomsWaterRipples.cs` | b6961916 | 31fc2378 | runtime: loads the tier's loops, keeps the ripple clock (Reduce Motion freezes it), sets the globals, ticks the rings |
| `Assets/Scripts/Rendering/Water/FrontRoomsWaterRings.cs` | 65298037 | 9eff4f0e | runtime: ring pool, hooks, wall images, slot budget, the wave-number table |
| `Assets/Scripts/Rendering/Water.meta` (folder) | - | a0920ec5 | |
| `Assets/Resources/Rendering/FrontRoomsWaterRipples.hlsl` | 800bd3fc | d26e59c1 | the include the water shaders call: `FRWaterRipples(xz)` → slope, unresolved covariance, footprint axis, ring height, agitation |
| `Assets/Editor/Rendering/FrontRoomsRippleArray.cs` | 7f90c11b | 1ad91ac1 | the import rules: menu "FrontRooms > Rendering > Ripple Arrays: Apply Import Rules" and `Check()`; not a postprocessor (§5.7) |
| clone only: `Assets/Editor/PoolLook/FrontRoomsRipplePreview.cs`, `.shader` | 252ebba8, 2dc13cd7 | f4075895, 85597cdc | the preview harness and its look-dev shader (never land) |

All 10 GUIDs of the package files are absent in main's `Assets` (checked against every `.meta`); the folders `Scripts/Rendering/Water` and `Resources/Rendering/Water` do not exist in main. Text files LF; C# and HLSL ASCII.

### 5.2 The constant ripple

**The spectrum.** Slope variance per ln k (`17` §3.1: "0.05–0.3 mm high at 1–5 cm wavelength (slopes 1–3°)"; §3.5: the constant ripple is capillary-scale sparkle, anything larger is caused by someone): a log-normal bump at 2 cm (width 0.55 in ln k) plus a faint shelf at 12 cm (8 %: the slow wobble of far reflections), soft edges a quarter octave wide at 5 mm and 30 cm. Half the slope variance lies between 1.4 and 3.3 cm, 80 % between 1.0 and 5.4 cm. At gain 1 the whole ripple has an rms slope of 2.0° (17's calm range 1-3° is gain 0.5-1.5); its rms height is 0.26 mm.

**The physics.** Capillary-gravity dispersion ω² = g k + (σ/ρ) k³ (σ/ρ = 0.0728/998 m³/s²), every direction equally (no wind, no current), random phases (Tessendorf's method, `17` W17): each wavelength moves at its own speed, so crests change shape and nothing slides. Each frequency is rounded to a whole number of turns per loop (never zero, so nothing stands still): the loop is exact.

**Why two cascades.** A 5 mm wave turns once in 16 ms, a 30 cm wave in 0.44 s; a loop of T seconds gives every wave a frequency that is a multiple of 1/T, and 64 frames cut T into 64 steps. A study over T (`W/pool_look/rip/tmp/study_t.py`, `study_t2.py`) found no single 64-frame loop that holds both ends: T 0.5 s quantises the 10-30 cm waves by 21 % rms (the long wobble would pulse on a fixed 2 Hz beat), T 1.0 s loses 18-37 % of the energetic band to the frame step. So the bake has two cascades, as ocean FFTs do:

| Cascade | Band | Sheet | Patch | Loop | Texel | Holds | Left to roughness | Frame-blend loss D | Frequency error |
|---|---|---|---|---|---|---|---|---|---|
| Fine (desktop) | 5 mm to ~7 cm (93 % of the slope variance) | 512² × 64 | 1.2 m | 0.50 s (7.8 ms a frame) | 2.34 mm | 97.3 % of its band; 1.34° rms per axis | 0.22° per axis | 0.120 | 5.9 % rms |
| FinePhone (handheld, WebGL) | the same field (the desktop's components, cut at its own resolution) | 256² × 32 | 1.2 m | 0.32 s (10 ms) | 4.69 mm | 93.9 %; 1.28° | 0.33° (waves under ~1 cm) | 0.143 | 9.3 % |
| Long (every tier) | ~7 to 30 cm (7 %) | 128² × 128 | 2.4 m | 2.56 s (20 ms) | 18.75 mm | 100 %; 0.38° | 0 | 0.054 | 3.2 % |

A wave that would turn more than 90-135° of phase per frame (45-90° on Long) is faded out of the sheet (linear blending of two frames cannot show it, and a 60 Hz display shows a 16-40 ms wave only as blur); its variance goes to roughness, like a wave too short for the texels. Encoding: R = ∂h/∂x, G = ∂h/∂z (world x along U, world z along V), 8 bit, slope = (2v - 1) × 5σ (clipped share 1e-6; quantisation 0.015° rms).

**No scrolling, measured** (`gen_pool_ripples.py --qa`, read back from the sheets by `W/pool_look/rip/tmp/qa_sheet.py`): the cross-correlation of a frame with a later one never finds a translation. From two frames on (15.6 ms, desktop) the best match anywhere is 0.10-0.32, with a mirror lobe of equal strength on the opposite side (waves leaving in both directions) and a random direction; a control scrolled at 0.25 m/s keeps 0.97 at every lag. Isotropy: the spectrum's energy in 8 direction sectors is within ±2.5 % (fine) and ±7 % (long; fewer lattice points); the x and z slopes have equal variance and correlation 0.0000. The patch-mean slope variance changes by 0.6 % over the loop (nothing pulses over a large area).

**Sampling** (`FrontRoomsWaterRipples.hlsl`): the fine loop twice on desktop, A as baked and B scaled 8/7, turned by the 3-4-5 angle and played at 0.8613 × (its phase speeds stay within 1.6 % of A's), so A + B never repeats exactly and no tile grid shows; the long loop once, turned by the 4-3-5 angle. Each patch divides 192 m along both world axes (B's turned patch 1.3714 m included), so the room stream's origin rebase never moves the pattern. A slow calm-patch mask (smooth value noise over 3.2 m and 24 s, ±25 %, `17` §3.3 item 4, W22's slicks) scales the slope and its roughness together; the phone tier samples A alone and skips the mask (6 array fetches become 4, and the mask's 8 hashes go).

**What it looks like** (sheet rows A, B, D; videos): in the held view the control's whole pattern slides one way and its 1.8 m tiles repeat across the frame; the new water changes in place, the fine sparkle boiling quickly while the long wobble moves the reflection slowly. The lamp's reflection is a streak along the view with glitter in its tail; at 0.4 m the capillaries are individual crests. The phone tier reads as the desktop at player distance (row D).

### 5.3 Distance roughness (anisotropic along the view)

The game has MSAA and no TAA, so what a pixel cannot show must become roughness (`17` §2.2, W23). `FRWaterRipples` returns the slope the pixel resolves and `cov`, the slope covariance it does not:
- **Footprint:** the pixel's footprint on the water from the screen derivatives of world x and z: its long axis (along the view) and short axis, in metres.
- **The variance of the normal map's mips, per footprint shape:** the generator computes, from each sequence's own spectrum, the share of each slope component a footprint of 2^a × 2^b texels averages away (box mip times bilinear, sinc² per axis), for a and b from 0 to 9 in half octaves (`lut_major_flat`, `lut_minor_flat`, 19 × 19 per cascade). The runtime packs them into one 19² RGBAHalf texture; the shader reads the share along and across the footprint. Isotropic footprints (the "variance of the mips", desktop fine): 12 % at 1 texel, 35 % at 2, 71 % at 4 (9.4 mm), 94 % at 8, 99.7 % at 16. A long footprint also raises the across share (16 × 4 texels: 99 % along, 89 % across, against 71 % for 4 × 4), which is why lamp reflections stretch into streaks. Under one texel the share fades to 0 (bilinear shows what the texels hold).
- **Plus** the sub-band (waves too short or too fast for the sheet) and the frame-blend loss of what stays resolved (4 f (1 - f) D, where f is the blend between two slices), both isotropic, so the total slope variance a pixel shows is constant through the loop.
- `FRRippleAlpha(r, alpha0)` turns `cov` into GGX alphas along and across the footprint (α² = α0² + 2σ² per axis); the preview's lamp uses an anisotropic GGX with them. The Water surface stage takes the same alphas for the lamp specular and the reflection's roughness mips.

### 5.4 Rings

**Physics** (`ring_model.py`; `FrontRoomsWaterRings.cs` builds the same constants at run time): a disturbance is a hump of volume V and radius a (plus a splash share at radius a_s) released from rest, the Cauchy-Poisson problem with the same dispersion. Energy travels at the group speed, whose minimum is **0.1777 m/s at 4.36 cm** (ω 40.42 rad/s): every ring has a calm centre whose rim grows at that speed; beyond it two wave trains share every point, long gravity waves whose crests run out through the packet (phase speed above group speed) and short capillary waves that race ahead of their crests, the front reaching 0.3-0.5 m/s. The shader evaluates both trains in closed form (stationary phase; the wave number at r / t comes from a 256-entry table on the gravity and capillary branches) and the rim by the Airy function (a degree-6 fit, error 4e-4); amplitudes fall as the waves spread, with viscous damping ×3 for a used pool's surface film and a 3 s fade (a ring is gone in 4-8 s, `17` §3.4). Each ring's crests are filtered by the pixel footprint along its radius; what is filtered goes to `cov`. Checked against the exact integral (correlation of the radial profiles at 0.5 / 1 / 2 / 4 s): drip 0.943 / 0.976 / 0.991 / 0.997, footstep 0.967 / 0.981 / 0.988 / 0.992, wade 0.786 / 0.928 / 0.954 / 0.988, plunge 0.839 / 0.890 / 0.964 / 0.986 (the closed form is weakest within a source radius or two of a fresh big source, where the splash, which v1 does not draw, dominates anyway).

**Hooks** (public static methods; callers never edit this file):

| Hook | Source (a, splash a_s and share) | Volume (calibrated to `17` §3.1) | Life | Agitation |
|---|---|---|---|---|
| `Drip(p)` | 3 mm, 1.5 mm 30 % | 0.74 ml: 0.4 mm crest at 0.10 m | 5 s | 0 |
| `Footstep(p)` | 40 mm, 8 mm 25 % | 0.30 l: 5 mm crest at 0.30 m | 7 s | 0.6 |
| `Wade(p)` | 180 mm, 20 mm 4 % | 10.1 l: 12 mm crest at 0.50 m | 6 s | 0.8 |
| `Plunge(p, size)` | 300 mm, 20 mm 8 % (size scales a and V) | 92 l: 30 mm crest at 1.0 m | 8 s | 1.2 |

`ReportBody(Body.Player / Body.Relay, feet, radius)` is the public method for the player's and the Relay's positions, called every frame they can move: in water shallower than 0.45 m at the feet a body leaves a footstep ring at each 0.7 m stride (feet 0.2 m apart); deeper, a wading ring every 0.3 m, slightly ahead of the body; standing in water, a faint ring every 1.5-3 s (the water keeps answering, `17` §3.5); stepping in, one stronger ring; a jump of more than 2 m makes none. `AutoSteps = false` hands footsteps to the game's own footstep events through `Footstep`. "Disturbance raises amplitude locally" (`17` §7 item 3): each ring stirs a patch around its source (radius a + 5 cm/s × age, fading over 1.5 s) where the constant ripple's slope rises by up to 80 % × agitation.

**Walls:** `RegisterWater(id, rect, waterY)` names the rectangles whose sides reflect; a ring inside one is mirrored across its four sides (×0.85) and corners (×0.72), and an image takes a slot only once its waves can have reached the water. **Slots:** 16 desktop Ultra, 8 other desktop presets, 4 the phone tier (`FrontRoomsTrofferKit.PhoneTier`: handheld and WebGL players); the preset is read from `FrontRoomsQuality.CurrentPreset`, which belongs to 设置菜单与画质预设 (`Budget` overrides it). Slots go to the strongest, freshest rings nearest the camera, then to images. Uploaded as two `float4[16]` globals (`_FR_Ring0`: centre, age, volume; `_FR_Ring1`: radius, splash radius and share, agitation) and a count.

**What it looks like** (row C, rings video): the drip and the footstep make the classic ring, a calm disc with a bright inner rim and wave trains outside it whose crests visibly move against the ring; the plunge sends a broad long-wave ring with short rings inside it, and its reflections come back off the near walls; a body wading leaves a cone of overlapping rings. **Its honest flaw:** the wake is a chain of rings, not yet the 19.5° Kelvin V of `17` §3.1; the desktop answer is the wave window (§5.8).

### 5.5 Safety

- **Reduce Motion** (the game's flag, `FrontRoomsHandheld.ReduceMotion`: the OS switch or CAMERA MOTION off; tools override it with `FrontRoomsWaterRipples.EditorReduceMotion`) freezes the ripple: the clock stops on the current frame, no new rings start, live rings stop where they are and fade out in one second. Measured (`still` clip): two frames 1/30 s apart with it on differ in 0 channel values; with it off, in 2,037,656.
- **Large-area modulation ≤ 3 Hz:** the loop's patch-mean slope variance is constant to 0.6 %, the frame-blend loss is returned as roughness (constant total), the calm-patch mask changes over 24 s, and the only waves that move large areas, the plunge's and the wade's long ones, have periods of 0.4-0.8 s (at most 2.5 Hz). Measured on the lamp-reflection panels (`W/pool_look/sheets/70_ripples_photosafety.json`, WCAG 2.3.1 as RELAY §9.5 defines a flash, whole panel and each quarter): 0 flashes per second in every clip (turntable, rings, close, phone); the largest frame-to-frame change of a mean is 0.012 of full scale (the turn moving the streak), and the panel mean's variation above 3 Hz has an rms of 0.0002.

### 5.6 Per tier and numbers

| | Desktop (Ultra / other presets) | Phone tier: handheld | Phone tier: WebGL |
|---|---|---|---|
| Fine loop | FR_Ripple_Fine 512² × 64, BC5 | FR_Ripple_FinePhone 256² × 32, EAC_RG | FR_Ripple_FinePhone 256² × 32, RG16 |
| Long loop | 128² × 128, BC5 | 128² × 128, EAC_RG | 64² × 128 (its WebGL tab), RG16 |
| Other tiers' sheets | 32 px stubs | 32 px stubs | 32 px stubs |
| **Memory** | **24.0 MiB** (21.33 + 2.67 + stubs 0.04) | **5.4 MiB** (2.67 + 2.67 + 0.08) | **6.8 MiB** (5.33 + 1.33 + 0.17) |
| Tables built at run time | footprint 19² RGBAHalf 2.9 KB, ring table 256 RGHalf 1 KB | same | same |
| Samples per pixel | fine A + B + long (6 array fetches), footprint table, mask | fine A + long (4), table | same as handheld |
| Ring slots | 16 / 8 | 4 | 4 |
| **GPU** (this Mac, UNDER LOAD, full-screen water at 2560 × 1440) | ripple +0.35-0.43 ms; 16 rings over the whole screen +2.2 ms (worst case) | not timed on a phone; on this Mac +0.49-0.51 ms, 4 rings +0.66-0.72 ms (from an uncompressed RGBA32 array: a phone samples 1 byte a texel) | not timed |

Repository: 33.6 MiB of PNG (26.3 + 3.8 + 3.5) and a 28 KB stats file. The editor measures double memory for each array (its CPU copy). Timing method: `FrontRoomsRipplePreview.Timing`, 7680 × 3544 (VISUAL_STANDARD 1.4), the same shader flat and with the ripple, wall clock with a one-pixel readback per frame, median of 20; two runs (`rip_v2`, `rip_v3`) differ by up to 20 %. A ring costs only where its waves are (each slot is skipped beyond its fastest front, `r > v_front t + 3a`), so the 16-ring figure (every slot covering the whole screen) is the ceiling, not the typical cost. Against `17` §6.1's budgets (≤ 4-5.5 ms Ultra, ≤ 2 ms handheld at device scale for all of the water), the ripple itself is small; rings are the item to watch on phones.

### 5.7 Checks

- **Gate (rule 25)**, `W/pool_look/rip/tmp/gate.zsh g2` (overrides as zsh array elements, response files read; g1 before the last edit gave the same): Roslyn against main with `FrontRoomsWaterRipples.cs`, `FrontRoomsWaterRings.cs` and `FrontRoomsRippleArray.cs`: macOS COMPILE OK, 0 errors, 3 overrides; iOS COMPILE OK, 3 overrides (`Assembly-CSharp.ios.rsp` holds the Water files and UNITY_IOS). The clone with the harness: COMPILE OK (`rip/tmp/roslyn_r5`). The include compiled for Metal in every Unity run (the preview shader includes it): 0 shader errors, 0 `error CS` (`W/pool_look/logs/rip_v1..v3.log`).
- **Import:** the arrays import as planned on the Standalone target (`rip_v2/import.txt`): Fine 512 × 512 × 64, 10 mips, BC5, trilinear, aniso 16, repeat, box mips, linear; Long 128² × 128 BC5; FinePhone a 32 px stub; the iPhone, Android and WebGL tabs EAC_RG / EAC_RG / RG16 at the sizes above. `FrontRoomsRippleArray.Check()`: all 3 sheets match the rules. The generator's `.meta` files are byte-identical to what Unity writes back after import.
- **Why the importer is not an AssetPostprocessor:** the first run had one (as `FrontRoomsPrintArray` does) and Unity reimported every texture in the project because a texture postprocessor was added (1,262 texture imports in the clone's log). In Red's Editor that would be a long stall for nothing, so the rules live in the `.meta` files the generator writes, plus a menu command and `Check()`.
- **Inert:** nothing changes until something calls `FrontRoomsWaterRipples.Ensure` or `Tick`; no shader in main includes the new file yet; no existing file was edited.
- **Runs:** `rip_v1` (06:42-06:44; found the postprocessor reimport, the stand-in not loading, the phone stub, the wake as a chain of drawn circles), `rip_v2` (06:49-06:50, the desktop clips of record), `rip_v3` (06:54, phone tier after its path changed, timing); all FINISHED fail=0, 9-42 s each.
- **Not checked here:** the include's compile for GLES3/WebGL, Vulkan and D3D (the batch compiles Metal only; the combined test's builds compile the rest); GPU time on any phone or browser; EAC_RG's error on the slopes (the phone preview used the sheet uncompressed).

### 5.8 The two-band wave window (`17` §7 item 5): the first follow-up, not this stage

Red chose P4c (desktop wake simulation and surface caustics) for v1 (Q2, BH line 115). This stage builds the hooks the window will use (`ReportBody`, `Footstep`, `Wade`, `Plunge` feed it as they feed the rings) but not the window: it needs a GPU height-field solver (two bands, 12 m and 24 m around the camera, `17` §3.4) and the water shader that consumes its heights, normals and slope variance, which is the Water surface stage's. Until then the desktop wake is the ring chain of §5.4. Proposed as the first package after this workflow's water stages, inside v1 (Q2), on desktop only.

### 5.9 Limits and open items

- **The wake** reads as overlapping rings (a cone of arcs), not a Kelvin V (§5.8). In ankle water the true wake is a supercritical V narrowing with speed (`17` §3.1); the rings give footsteps there, which is what a walker leaves at close range.
- **Ankle water:** the rings use deep-water dispersion; waves longer than about 40 cm travel slower in 0.12 m of water. The footstep's energetic waves (3-15 cm) are unaffected.
- **The constant ripple's height** is not baked (under 0.3 mm); refraction and caustics take the rings' height and the slopes.
- **A loop of 0.5 s** (0.32 s on phones) underlies the fine sparkle; A + B + Long never repeat exactly, but the fine pattern's own period is short, so a viewer staring at one glint for a long time might sense a rhythm. Not seen in the clips; a live FFT on desktop Ultra (`17` §6.1's "or live 256²") would remove it.
- **Rings on phones** are the expensive item (§5.6); the Water surface stage may want 2 slots on small phones.
- **The tier rule** (16 / 8 / 4) reads `FrontRoomsQuality.CurrentPreset`, which belongs to 设置菜单与画质预设: it goes to them with the Water surface stage's tier contract.

### 5.10 For the next stages

- **Water surface:** include `FrontRoomsWaterRipples.hlsl` after URP's Core.hlsl and call `FRWaterRipples(positionWS.xz)` in the fragment stage, unconditionally (it uses ddx / ddy); the normal is `FRRippleNormal(r.slope)`, the lamp specular's anisotropic GGX takes `FRRippleAlpha(r, alpha0)` along `r.axis`, and the planar or probe reflection picks its roughness mip from the same alphas. The water component calls `FrontRoomsWaterRipples.Tick(Time.deltaTime, camera position)` once a frame while water is visible (a second call in the same frame does nothing) and registers each water rectangle with `FrontRoomsWaterRings.RegisterWater`. `FrontRoomsWaterRipples.Gain` sets the calm (1 = 2.0°); a zone may want less in the dark zone. The wiring of `ReportBody` for the player and the Relay goes in that stage's contract (the callers are other lines' files, or the water component reads the camera and `FrontRoomsRelayRig`).
- **Submerged:** caustics can take the slopes (desktop) and `r.height` (rings); the long cascade's slopes drive the slow light pattern.
- **Package (`pool_water`):** `Tools/lookdev/pool_water/` (2 files), `Assets/Resources/Rendering/Water/` (3 PNG, the stats JSON, 4 metas, folder meta), `Assets/Scripts/Rendering/Water/` (2 files, metas, folder meta), `Assets/Resources/Rendering/FrontRoomsWaterRipples.hlsl` (+ meta), `Assets/Editor/Rendering/FrontRoomsRippleArray.cs` (+ meta). Base: all ABSENT in main. After `--apply`, the Editor imports three textures (no project-wide reimport).
- **Harness:** `FrontRoomsRipplePreview.RunBatch` (`-rpRun name -rpClips turntable,rings,close,phone,still,timing`) through `W/pool_look/rip/tmp/run_rip_when_free.zsh`; media `W/pool_look/rip/tmp/make_rip_media.py`.
- **Deletion candidates** (inside `W/pool_look`, Red deletes): `rip/renders/rip_v1` (110 MB, superseded), `rip/tmp/roslyn_r1`-`r3`, `rip/tmp/meta_after_import`, `rip/tmp/crop_*.png`, `rip/tmp/cmp_desk_phone.jpg`, `rip/tmp/sheet_*.jpg`, `rip/tmp/vcheck_rings.jpg`. Keep `rip/renders/rip_v2`, `rip_v3`, `rip/video`, `rip/qa`, `rip/tmp/gate_g2_*`, `roslyn_r5`, the scripts. Also candidates: `rip/tmp/gate_g1_*`, `roslyn_r4`.

## 6. Water surface

Stage 6 of the workflow, 2026-11-04 07:02 to 08:15, one agent on account G (the relay of `wf_48851110-b3c`). **Nothing landed in main:** main was read only, no Unity ran on main, nothing was downloaded, no package was built (the Package stage builds `pool_water`), and the contract below is not applied. The Unity runs met a load average of 17-260 on 16 cores: every frame time below is **UNDER LOAD**.

The asks: `17` §7 item 1, "Water surface = reflection only (alpha = Fresnel): planar or probe, plus lamp specular from the ripple normal. Retire the stand-in's constant 0.3 alpha and teal base"; item 6, "Planar camera per §2.3: submitted from `LateUpdate`, ½ / ⅓–¼ resolution, overscan 1.1–1.2, oblique clip, layers including the Relay, roughness mips. One camera per water height, one for a whole connected level. Quality tiers go through 设置菜单与画质"; item 7 (the Relay-only overlay), named here as the follow-up (§6.9); §2 (a window near, a mirror far; streaks whose width/length is the sine of the sight line's elevation, W20; the planar recipe) and §6.1's tiers. No Red decision is open in this stage: both sheets carry PENDING (their VL rows come with the line's daily batch).

![Water surface](sheets/70_water_surface.jpg)

Sheet `sheets/70_water_surface.jpg` (3838 × 9086, Shot format, chip PENDING; every label checked at full size: ALL LABELS FIT, `W/pool_look/sheets/70_water_surface_fit.txt`): TODAY (`today_v4`; `tile_today` for S8-S10) | BEFORE WATER (`tile_v2`: the zone look, the lamp presets and the pool tile with TODAY's water) | WATER (`ws_v14`), rows S1-S10, COST. Sheet `sheets/70_water_surface_parts.jpg` (3838 × 5945, PENDING, ALL LABELS FIT, `…_parts_fit.txt`): the three tiers at S1 and S7, the debug views that take a frame apart (Fresnel, reflection only, unresolved slope; at S7 reflection only, F × reflection, what shows through), TODAY's troffers and the streak check, the mirror image itself, the default renderer, the asset's 2× opaque copy, and panels for timing, streaks and what is open. Silent films (h264, no audio stream, Shot header on every frame; 12 s at 30 fps, the ripple clock running, three drips each): `video/70_water_surface_S1.mp4`, `video/70_water_surface_S7.mp4` (1440 wide, crf 26; masters at 1920, crf 18: `W/pool_look/ws/video/`). Frames of record: `W/pool_look/renders/ws_v14/`; TODAY's troffers and the streak check: `ws_lamps3/`.

### 6.1 What was built (in the clone `W/proj_unlock`; the Package stage carries it to main)

| Path (under `Frontrooms3D/`) | sha1 | .meta GUID | What |
|---|---|---|---|
| `Assets/Resources/Rendering/FrontRoomsWater.shader` | 1de7c0e4 | fd0021231e7945fd8dd5db29370c061a | `FrontRooms/Water`: the surface (Fresnel, mirror image or probe, lamp reflections, refraction) |
| `Assets/Scripts/Rendering/Water/FrontRoomsWaterPlanar.cs` | 0706f3a7 | 267b1996d6ee4cd497567a25b5629f93 | the mirror cameras, the tier rule, each body's parameters, the opaque-texture request |
| `Assets/Scripts/Rendering/Water/FrontRoomsWaterSurface.cs` | 3817ecfa | 4d86b91af1a6433e93b2c30308c736eb | the component per body of water (Add Component > FrontRooms > Water surface) |
| contract, settings line: `Assets/Settings/FrontRoomsWaterReflection_Renderer.asset` | 4cdf3cd7 | 95693ae2507624779bd436d04a9ef870 | the mirror renderer (§6.10) |
| clone only: `Assets/Editor/PoolLook/FrontRoomsPoolLook.Water.cs` | f71b113f | 63aeeb40b87546b59b168d5e022df85d | the harness's water layer `pool`, tiers, debug views, films, timing (never lands) |

Clone only as well: the harness's `FrontRoomsPoolLook.cs` (0a2f1796: the flags, the per-shot water hook, the restore) and `FrontRoomsPoolLook.Layers.cs` (9cd5c7cf: the `pool` case; TODAY's water moved into `BuildWaterTodaySet` with the same statements); pre-edit copies in `W/pool_look/ws/base/`. The three package files are ASCII and LF; their GUIDs are absent in main's 3,328 `.meta` files and in the clone's; the folders exist since the Ripples stage.

### 6.2 The surface: reflection only

- **What it writes:** premultiplied, `rgb = F × reflection (+ the lamps' reflections)`, `alpha = F`, blended One / OneMinusSrcAlpha, so what lies under the water shows by `1 - F` with its own colour. No base colour and no constant alpha: the stand-in's 0.30 alpha and teal base are gone (`17` §7 item 1); the water's colour is the Submerged stage's (absorption, the trapping term, `17` §4.2).
- **F:** the exact unpolarised Fresnel of an air-to-water interface, n = 1.333 (0.0204 looking straight down, 1 at grazing), with the pixel's resolved ripple normal; where a resolved slope is steeper than the sight line it is scaled so the eye stays on the facet's front.
- **Read at S1** (debug view, through the grade): about 3 % at the coping below the eye, 0.45 at the deep end 15 m off, as `17` §2.1's table.

### 6.3 Reflection

**The mirror image (desktop High and Ultra).** `17` §2.3's recipe, with what the runs settled:
- **One camera per water height** (heights keyed to 1 mm). In the harness: the hall's basin (-0.12) and the ankle water of the turn room and of the level chain (-0.03, the same height: one camera serves the hall's far door and the whole level). Ultra renders up to two heights a frame (the nearest first), High one; the rest keep the probe.
- **The camera:** an ordinary camera at the eye mirrored through the water plane, looking along the mirrored view with the mirrored up turned over, which is a proper rotation, so culling stays as it is (no inverted culling); an oblique near plane 5 mm above the water, so nothing under the water reaches the image and a leg or a pillar meets its reflection at the line.
- **A fitted frustum:** off-centre, covering only where this height's water lies on the viewer's screen (each body's rectangle clipped to the near plane, widened ×1.15 about its centre plus 0.05 for rippled samples, never finer than the viewer's pixels). S1's image covers x -0.57..1.15, y -1.13..0.19 of the viewer's screen, S7's the full width and y -1.12..0.07: the texels go to the water and the room above it instead of the clipped space under the mirror's horizon (about 1.7-2× the texels on the water at the same size; the first symmetric frustum spent half its image there). The image size is fixed per tier, so turning never reallocates it.
- **Size and rate:** Ultra ½ of the viewer's pixels × 1.15 (1104 × 621 at 1080p, 1472 × 828 at 1440p), every frame. High ⅓ (¼ above 1440 rows) × 1.15, every second frame; the water samples an image with the view-projection of the frame it was rendered in, so an image one frame old still lands on the right place (world-space reprojection; things that moved lag by a frame).
- **Settings:** R11G11B10 HDR with a mip chain made after each render (the roughness filter reads it), MSAA off, no post-processing, no shadows (§2.3), no AA, camera type Reflection (URP keeps render scale off for it and the glass RT feature skips it). Culling mask: the viewer's minus Water (4), UI (5), the held glass (28) and the held item (30); the Relay and everything else stays. Its volume trigger is the viewer, so the zone look weighs the mirror's render context where the player stands (FrontRoomsZoneLook picks the context's camera and reads its trigger).
- **Submitted** with `RenderPipeline.SubmitRenderRequest` (`UniversalRenderPipeline.SingleCameraRequest`) from `Application.onBeforeRender`, after every `LateUpdate` and outside the render loop (never from a camera callback, which errors under Render Graph, W2/W3). Tools call `FrontRoomsWaterPlanar.RenderFor(camera, frame)` themselves, as the harness does.
- **Renderer:** the contract's mirror renderer (no SSAO, no glass RT; §6.10), found by name; until it lands, the default renderer, whose SSAO then runs on the oblique projection (`S1_defrenderer` on the parts sheet: close, but wrong in principle).
- **How the water reads it:** the reflected ray of the pixel's resolved slope is followed to where it leaves the room's box (the body's rectangle, up to the room's height), and that point is projected with the mirror's view-projection: for flat water exactly the mirror point, for rippled water a distortion scaled by distance. The slope covariance the pixel cannot resolve (`r.cov`, §5.3) becomes the filter: its two principal axes are followed as rays into texture space, the mip covers the narrow axis and up to 12 taps (Gaussian over ±2 sd) the long one. A tap whose ray would point into the water is masked by the waves in front of it (Smith's masking) and drops out; the rest renormalise (without this the far water pulled in the clear colour under the mirror's horizon, `ws_v2`). Outside the image the water falls back to the probe.

**The probe (phones, WebGL, desktop Medium and Low).** The zone's cube (`RenderSettings`' custom reflection; `_GlossyEnvironmentCubeMap` under Forward+) projected on the same room box from a capture point at the room's centre 1.6 m above the water, and filtered the same way in direction space (the mip from the narrow axis through GGX's roughness, 6 taps along the long axis on desktop, 3 on the phone tier), with the zone's fog and height fog for the whole reflected path.

*Changed by the Package stage (§9.3):* the probe is the water renderer's own (`unity_SpecCube0`): a local box probe (the structures scene's hall probes) on its own box, else the zone cube as described here.

**The lamps.** Every visible additional light above the water (Forward+ cluster; wet niches under the water are skipped: from above, their light comes through the surface, not off it) reflects as Fresnel × its radiance × the share of the pixel's slope distribution that points it at the eye: a Gaussian in slope space whose mean is the resolved slope and whose spread is the unresolved covariance, the calm roughness (α 0.012) and the lamp's own size (a downward lens of radius 0.4 m, its solid angle carried into slope space). The radiance is the light's colour × intensity × 0.36 with its range window and cone but without the inverse square (a source seen in a mirror keeps its radiance): an L03 at intensity 5 reflects at 1.8, its own lens drive. On the mirror tiers the lamps are already in the image (every lens to the far wall, with its flicker and dead states and the Relay passing in front), so this term is off there: one reflection per lamp on every tier, never two. On the probe tiers it shows the lamps whose lights are on (the map turns lights off past about 16 m): far lamps do not reflect on phones.

### 6.4 Refraction (desktop High and Ultra)

- The viewer camera asks URP for its opaque texture as `FrontRoomsGlassSceneColor` does: a pass that records nothing but declares the input, enqueued from `beginCameraRendering` for that camera only, while refracting water is in view; `_FR_WaterOpaque` tells the shader which camera has it. No camera or asset setting changes.
- The refracted ray of the resolved normal meets the depth of what lies straight behind the pixel (flat floors exactly, walls closely): the floor shows at its apparent depth and wobbles with the ripple; whatever stands above the water at the refracted pixel is not used (faded over 3 cm, so the coping's edge cuts no hard line).
- **The frame's edges:** looking steeply down near the bottom of the frame, the refracted ray lands nearer than the straight one, below the frame, where no image exists. Over the bottom and top 35 % of the frame the shift morphs into the ripple's wobble alone (the rippled minus the flat refraction, moved onto the floor seen straight), so lane lines curve gently into the edge. Tried first: a hard fallback (the lane line jumped), a shortened shift (the edge rows smeared), a 15 % band (the lane line kinked).
- **Every body refracts** by default (`FrontRoomsWaterSurface.refraction`). `17` §6.1 named the ankle water; the basin is included because under 1.2 m of rippled water its lane lines did not move at all (`ws_v8`), which read as an empty pool.
- With the asset's 2× opaque copy the joints under the water soften next to the dry tile (`S7_opaque2x`): the contract asks for the full copy.

### 6.5 The streak geometry (`17` §2.2, W20)

Measured on TODAY's troffers in the hall (run `ws_lamps3`, S1) with the resolved slope set to 0 (debug view 8), so each lamp's reflection is the unresolved lobe on the lamp; `W/pool_look/ws/tmp/streaks.py` takes each blob's second moments in angle and the elevation e of the view ray through its centre:
- **mirror image:** 15 lamps, (width/length) / sin e median **1.13** (p25 0.90, p75 1.55, range 0.55-1.82; the lens's own 0.6 × 1.2 m and neighbours merged in the blur widen some);
- **lamp term** (probe tier): 3 clean blobs, 0.51-1.08 (median 0.93).

W20's geometry comes out of the filter itself, with no texture trick: far lamps (low elevation) draw long thin streaks, near ones round blobs (tables `W/pool_look/ws/tmp/streaks_planar_v3.tsv`, `streaks_probe_v3.tsv`).

### 6.6 Per tier and numbers

| | Desktop Ultra | Desktop High | Desktop Medium, Low | Phones, iPad | WebGL |
|---|---|---|---|---|---|
| Reflection | mirror ½ × 1.15, every frame, up to 2 heights | mirror ⅓ (¼ above 1440 rows) × 1.15, every 2nd frame, 1 height | probe + lamps | probe + lamps | probe + lamps (`#if UNITY_WEBGL && !UNITY_EDITOR`) |
| Refraction | opaque texture | opaque texture | none | none | none |
| Memory per mirror image (colour with mips + depth) | 6.10 MiB at 1080p, 10.85 MiB at 1440p | 4.82 MiB at 1440p | 0 | 0 | 0 |
| Draws | one per body; the mirror renders the room again (opaque and transparent, no shadows) per height per frame | the same, every 2nd frame | one per body | one per body | one per body |

**GPU** (this Mac, UNDER LOAD; 7680 × 3544 as VISUAL_STANDARD 1.4, MSAA off, 30 interleaved rounds of the six variants, the median of each round's difference against no water; `ws_v14/water_timing.tsv`):

| | S1 at 7680 × 3544 | S1 at 1440p | S7 at 7680 × 3544 | S7 at 1440p |
|---|---|---|---|---|
| TODAY's water (stand-in) | +3.8 ms | +0.52 ms | +5.2 ms | +0.70 ms |
| probe tier | +4.1 | +0.55 | +4.6 | +0.62 |
| High (render frame / reuse frame) | +4.7 / +2.8 | +0.51 (average) | +4.8 / +3.8 | +0.58 (average) |
| Ultra (S1: two mirror images) | +10.2 (p25-p75 9.6-11.3) | +1.38 | +7.4 | +1.01 |

The 1440p column scales by pixels; the mirror render's draw-call part does not scale that way (the harness rooms have few renderers; G11's editor bench found about 2.5 ms fixed plus 20 µs per renderer, `17` §2.3), so a dressed room costs more. The probe tier's cost is mostly the lamp term (S1 has 32 lights in view). An earlier timing with independent medians (`ws_v8`) was noise and was replaced by the interleaved method. Against `17` §6.1's budgets for all of the water (Ultra ≤ 4-5.5 ms, High ≤ 2.5-3.5 ms, handheld ≤ 2 ms at device scale): inside them here; phones were not timed.

### 6.7 Checks

- **Gate (rule 25)**, `W/pool_look/ws/tmp/gate.zsh g2` (overrides as zsh array elements, response files read): Roslyn against main with `FrontRoomsWaterPlanar.cs`, `FrontRoomsWaterSurface.cs` and the Ripples stage's `FrontRoomsWaterRipples.cs`, `FrontRoomsWaterRings.cs`, `FrontRoomsRippleArray.cs`: macOS COMPILE OK, 0 errors, 5 overrides; iOS COMPILE OK, 5 overrides (the Water files and UNITY_IOS in `Assembly-CSharp.ios.rsp`). With the map contract's patched `FrontRooms3DGame.cs` as a sixth (`gate_con.zsh c2`): macOS and iOS COMPILE OK, 6 overrides. The clone with the harness: COMPILE OK (`ws/tmp/roslyn_w12`).
- **Shader:** compiled for Metal in every Unity run in the clone (`ws_v1`-`ws_v14`, `ws_lamps`-`ws_lamps3`, `ws_t1`): 0 shader errors, 0 warnings for `FrontRooms/Water`, 0 `error CS`.
- **Inert:** nothing runs until a `FrontRoomsWaterSurface` is enabled (no camera, texture, callback or global before that); the package's files are new; the map line and the settings change are the contract's.
- **Photosafety** (both films, WCAG 2.3.1 as RELAY §9.5 defines a flash; the whole frame, each quarter and the water's band): 0 flashes per second; the largest frame-to-frame change of a mean is 0.0008 of full scale (`W/pool_look/sheets/70_water_surface_photosafety.json`). The mirror and the refraction add no motion of their own; Reduce Motion freezes the ripple as §5.5 measured.
- **Runs:** `ws_v1`-`ws_v14` (07:30-08:05), `ws_lamps`, `ws_lamps2`, `ws_lamps3`, `ws_t1`; all FINISHED fail=0, 2-45 s of harness each. What they found and what was fixed: the first mirror render of a run drew the pool tile red (the tile not resident for that first render; the harness renders once before its first shot); the far water pulled in the mirror's clear colour (masked taps); the symmetric frustum spent half its image under the horizon (fitted frustum); the basin did not refract (refraction on every body); the refraction's frame edge (the morph band). The mirror renderer runs with intermediate textures Always, like the game's renderer (it was briefly Auto while the red frames were blamed on it; they were the first render).

### 6.8 Reading the renders

- **S1, hall deck:** the basin reads as water now: the lane lines bend, shorten by the apparent depth and wobble; the far water mirrors the walls; the lit ceiling's patches show as broken bright reflections; at the coping the water is a window (about 3 %). Against BEFORE WATER the milky teal veil is gone, as `17` §7 item 1 asks; the basin's colour arrives with the Submerged stage (`17` §4.2: #8AD0E7 at 1.07 m).
- **S2, S8, level, standing:** the floor under the ankle water is near white in the level's high-key look and outshines what the water reflects (walls of the same white, a dark ceiling), so the water shows mostly as refracted, wobbling joints. Taken apart at S7: F × reflection 23-150 on screen (near to far), what shows through 171-209: the reflection is there, under a floor in the tonemapper's shoulder. The next stage changes this twice over: the floor's glaze still reflects as in air (under water it reflects about a tenth as much), and the level's absorption ×3-5 (Q1) darkens and tints what shows through.
- **S7, crouched:** at grazing angles the far floor mirrors the walls, the teal band and the arch (reflection only, parts sheet); in the composite the wobble of the refracted grid is the strongest cue.
- **S4, S10, hero at the waterline:** a clean mirror of the course tile and the coping over a refracted, broken grid, with the meniscus between.
- **S5, phone tier:** the probe reflects the warm Level 0 cube (khaki at grazing) until the Submerged stage's `Refl_Pool`.
- **S6, dark zone:** the niche pools under dark glassy water, the room's few lights mirrored.
- **TODAY's troffers** (parts sheet): the mirror carries every lens to the far wall as rippled streaks; in the composite the bright basin under them swamps those reflections, because the game's lens drive (1.8) is only about 1.25× the floor it lights, where a real troffer's lens is 10-40× its floor. The reflections are as bright as the lenses the player sees, no brighter.

### 6.9 The Relay-only overlay (`17` §7 item 7, G11 §2.4b): the follow-up

On the mirror tiers the Relay is in the water (its renderers are on a layer the mirror keeps). On the probe tiers it is not: the probe is static. The overlay keeps the Relay "seen in the water first" on phones, WebGL and desktop Medium and Low: a second, Relay-only camera at the mirrored eye (culling the Relay alone, a small target, no clear of the scene), composited into the water where the probe shows. It needs the Relay on its own layer or rendering-layer mask and, from the hunter's line, which body to draw. Proposed as the first package after this workflow's water stages, with G11 §2.4b's design; not built here.

### 6.10 The contract and the Relay's rings

- **`W/apply/pool_water_contract/`** (README, `CONTRACT_settings.diff`, `CONTRACT_map.diff`; never applied by this line). Settings (设置菜单与画质预设, `FrontRooms_URP.asset` base 18584f2b): the mirror renderer as the pipeline's second renderer (no SSAO, no glass RT; it also keeps the non-SSAO shader variants in the build) and the opaque copy at full resolution (`m_OpaqueDownsampling` 1 → 0; the glass line's crack mirrors get the sharper copy too); the tier rule written out for the settings line to confirm (no diff: the water only reads `FrontRoomsQuality.CurrentPreset`, `LowPowerActive` and `FrontRoomsTrofferKit.PhoneTier`; `FrontRoomsWaterPlanar.ForceTier` is the hook for a "WATER REFLECTIONS" row if they want one). Map (关卡设计, `FrontRooms3DGame.cs` base d059a6de, patched 36a7d5b0): one `FrontRoomsWaterRings.ReportBody(Body.Player, playerRoot.position, ModuleUnits.PlayerRadius)` after the per-frame move, inert away from water. Both `patch --dry-run -p1` clean from the course root; main untouched.
- **The Relay's rings:** one line in `FrontRoomsRelayRig.TickAnimation` (Play mode), this line's own file: `FrontRoomsWaterRings.ReportBody(Body.Relay, transform.position, .3f)`. The Package stage adds it to `pool_water` against the then-current main (`542df78c` today).

### 6.11 Limits and open items

- **The mirror's hit distance is the room box's**, not the mirror's depth (URP renders through an intermediate texture, so the image keeps no depth): something near in the reflection (a ladder, the Relay standing in the water) is distorted as much as the far wall. A mirror renderer that renders straight into its target could keep the depth; a later refinement.
- **Far lamps on the probe tiers** do not reflect (their lights are off).
- **No self-reflection:** the player has no body; the GlassRT body proxy could be drawn into the mirror later.
- **Height fog in the mirror:** the mirrored first leg runs under the water plane, where `FRHeightFog` takes the density at the water; the far reflection's haze is up to about 5 % too dense.
- **The first render:** the harness's first mirror render of a run drew the pool tile red; check the first frame in the structures scene during the combined test.
- **Not verified here:** Play mode (`Application.onBeforeRender`; the harness calls the same `RenderFor` in edit mode), the Scene view, the shader's GLES3/WebGL, Vulkan and D3D compiles (Metal only), GPU time on any phone or browser.
- **Submerged items:** the basin's colour, caustics, the glaze under water, `Refl_Pool`.

### 6.12 For the next stages

- **Submerged:** the floor's glaze under water (specular about ×0.1), absorption with `_WaterAbsorbScale` per zone (Q1), the trapping term, caustics, `Refl_Pool` for the probe tiers (the box projection is already per body). The refraction reads the opaque image, so whatever Submerged draws on the floor and walls is refracted with it.
- **Renders:** the walk film and the decision sheets; this stage's films are the stills' two views.
- **Package (`pool_water`):** this stage's three files and their metas join the Ripples stage's; plus the Relay line in `FrontRoomsRelayRig.cs`. The contract stays a contract.
- **Harness:** `-plWater pool` (with `-plWaterTiers`, `-plWaterFilm S1,S7`, `-plFilmSeconds 12`, `-plWaterTiming`); media `W/pool_look/ws/tmp/make_ws_media.py`, sheets `make_ws_sheets.py`, the streak check `streaks.py`.
- **Deletion candidates** (inside `W/pool_look`, Red deletes): `renders/ws_v1`-`ws_v13`, `ws_lamps`, `ws_lamps2`, `ws_t1` (superseded by `ws_v14` and `ws_lamps3`); `ws/tmp/roslyn_w1`-`w11`, `ws/tmp/gate_g1_*`; `ws/tmp/*.jpg` previews; `ws/logs/*.launcher.txt`. Keep `renders/ws_v14`, `ws_lamps3`, `ws/video`, `ws/contract`, `ws/tmp/gate_g2_*`, `gate_c2_*`, `roslyn_w12`, the scripts.

## 7. Submerged and caustics

Stage 7 of the workflow, 2026-11-04 08:15 to 09:45, account G (the relay of `wf_48851110-b3c`; the first Submerged agent stopped at about 08:20 with nothing on disk but its folders, and a second agent did the stage from 08:38). **Nothing landed in main:** main was read only, no Unity ran on main, nothing was downloaded, no package was built (the Package stage carries this stage's files into `pool_water` and `pool_zone_look`), and the contract below is not applied. The Unity runs met a load average of 12-50 on 16 cores: every frame time below is **UNDER LOAD**.

The asks: `17` §7 item 2, "Submerged shading below `_WaterY` in `FrontRooms/Surface`, the analytic basin and the Relay's shader: absorption (0.29, 0.048, 0.006) per metre; a small depth LUT with the trapping term (§4.2); the per-zone `_WaterAbsorbScale` (Q1); caustics; the wet band and meniscus (10 §3.8)"; item 8, "Caustics: projected from the surface on desktop (floor nets from compact sources; thrown light on ceiling and walls from niche lamps); the flipbook on phones; scrolled layers on WebGL; ≤ 3 Hz large-area modulation; Reduce Motion freezes them"; item 9 (compact sources where moving light is wanted); item 11, "Probes: `Refl_Pool` with its dead-lamp twin, per-room probes in the level with blending, and a crossfade when a room's lamps die"; `16` §6 item 3; the note of `zone_lamps/30_for_red.md` §2.3 ("天花焦散是同一张网 … 落地时要把焦散按灯分区": the ceiling caustics were one net shared by every lamp). Red's picks this stage follows (recorded as picks, BH line 115): Q1 "wrong" (the club's water physical, the level's ×3-5, one or two rooms ×9), Q2 "v1" (surface caustics in v1), Q4 "follow" (the dark zone's white balance follows the niche lamps). **Open for Red:** only where inside Q1's range each zone sits (sheet `70_submerged_q1.jpg`, WAIT-RED; the recommendation: club ×1, level ×4, dark zone ×3, no ×9 room yet).

![Submerged](sheets/70_submerged.jpg)

Sheets (Shot format; every label checked at full size, ALL LABELS FIT, fit files `W/pool_look/sheets/70_submerged*_fit.txt`): `sheets/70_submerged.jpg` (3838 × 9086, PENDING): TODAY (`today_v4`; `tile_today` for S8-S10) | BEFORE (`ws_v14`, the water surface stage) | SUBMERGED (`sub_v4`), rows S1-S10, COST. `sheets/70_submerged_q1.jpg` (3838 × 2122, **WAIT-RED**): TODAY | ×1 | ×3 | ×4 | ×5 | ×9 on S1, S2, S6, REC per row, COST. `sheets/70_submerged_parts.jpg` (3838 × 5152, PENDING): the calibration swatches, the caustic check, the thrown light with two niches dead, the pool cube on the probe tier, the phone tier, Reduce Motion, a film frame, timing, safety, open items. Silent films (h264, no audio stream, Shot header on every frame, 8 s at 30 fps): `video/70_submerged_S6.mp4` (something wades through the dark zone 2-4 m ahead) and `video/70_submerged_S7.mp4` (someone walks through the level's ankle water), 1440 wide crf 26 (masters 1920 crf 18 in `W/pool_look/sub/video/`). Frames of record: `W/pool_look/renders/sub_v4/`.

### 7.1 What was built (in the clone `W/proj_unlock`; the Package stage carries it to main)

| Path (under `Frontrooms3D/`) | sha1 | .meta GUID | What |
|---|---|---|---|
| `Assets/Resources/Rendering/FrontRoomsWaterSubmerged.hlsl` | 9ed1a464 | 32ed71877b784fe4a2509a8a6b080004 | the include: body lookup, `FRSubmergedPBR`, the caustic loops, the rings' lens, the thrown light |
| `Assets/Scripts/Rendering/Water/FrontRoomsWaterSubmerged.cs` | 1b96d77e | 11c2bdd11ab9477082ef9316fa3a4844 | runtime: the registry of bodies, niches and compact sources, the LUT, the tier's caustic loops, `Publish` |
| `Assets/Resources/Rendering/FrontRoomsSurface.shader` | cef7facc → 8067dc67 | (unchanged) | includes the above; finds the body at the top of `Frag`; under the water `FRSubmergedPBR` instead of `UniversalFragmentPBR`, above it in a pool room + the thrown light; the wet band takes the body's height when the material has none (+24 lines; no new keyword) |
| `Assets/Scripts/Rendering/Water/FrontRoomsWaterSurface.cs` | 3817ecfa → dfafe86c | 4d86b91a… | + `absorbScale`, `basinAlbedo`, `causticSourceA/B`, `causticSourceRadius`, `thrownGain`; registers its body; `RefreshSubmerged()` |
| `Assets/Scripts/Rendering/Water/FrontRoomsWaterPlanar.cs` | 0706f3a7 → abffda7b | 267b1996… | `RenderFor` sets `FrontRoomsWaterSubmerged.ViewerPosition` and calls `Publish` (2 lines) |
| `Assets/Scripts/Rendering/ZoneLook/FrontRoomsZoneLookProfile.cs` | 760264ae → 663efcde | 5f2a7b9a… | + `waterAbsorbScale` (club 1, hall 1, level 4, dark 3), `thrownLight` (hall 1, dark 2.5); niche light = its lens colour (1/.80/.58, since the shader now absorbs along the path); PoolDark's ground and wall terms back to the bounce alone (.095/.105/.058, .08/.088/.048) |
| `Assets/Scripts/Rendering/FrontRoomsLook.cs` (main 898dee87) | → 61f9b581 | (unchanged) | `ReflectionZone` + `Pool`, `PoolDead`, `PoolLevel` (appended) |
| `Assets/Scripts/Rendering/FrontRoomsZoneReflection.cs` (main 040ba42b) | → fc95d511 | (unchanged) | their cube names and intensities (0.5 linear, as Level 0's) |
| `Assets/Editor/Rendering/FrontRoomsReflectionCapture.cs` (main ad9ebcb1) | → 7a4f7c55 | (unchanged) | keeps the pool cubes (its `Matches` default would have baked Level 0 cells into them) |
| `Assets/Resources/Rendering/Water/FR_Caustic_Fine.png`, `_FinePhone.png`, `_Long.png` | 4b26cc4a, 71fb7033, abdd3340 | 92303e65…, 3a39c89c…, 1e68d9e6… | the caustic loops (7.4); 10.1, 1.1, 4.3 MiB of PNG |
| `Assets/Resources/Rendering/Water/FR_Caustic_Stats.json` | b3f81e58 | ffedcb16… | per loop: size, slices, lens distances, contrast per distance, sha1, GUIDs |
| `Tools/lookdev/pool_water/gen_pool_caustics.py` | f319a97a | - | the generator (imports `gen_pool_ripples.bake`: the same surface), deterministic, 4 min |
| `Assets/Resources/Rendering/Reflections/Refl_Pool.exr`, `Refl_PoolDead.exr`, `Refl_PoolLevel.exr` | 31dc19b5, 72708dc6, 8dae4d85 | 87e3941e…, c420d827…, bdbe573f… | the pool family's cubes (7.6), 256 px BC6H, 9 mips; WebGL RGB9E5 at 128 |
| `Assets/Resources/Rendering/ZoneLook/FrontRoomsZoneLook_Pool*.asset` | rewritten from code | (unchanged) | the profiles with the two new fields |
| contract `W/apply/pool_submerged_contract/` | | | `FrontRooms3DGame.UpdateReflectionZone` picks the pool cubes (7.6) |
| clone only: `Assets/Editor/PoolLook/FrontRoomsPoolLook.Submerged.cs` | 28718144 | 52097edc… | the harness layer `-plSub pool` and its checks (7.9); hooks in `FrontRoomsPoolLook.cs`, `.Layers.cs` (pre-edit copies `W/pool_look/sub/base/`) |

Every new GUID is absent in main's `Assets` and appears once in the clone. C# and HLSL are ASCII; every text file LF.

### 7.2 Under the water: the colour comes from the light's path

`17` §4.2 says where the colour lives: "everything below `_WaterY` gets the trapping tint in its own shader". So the water surface stays colourless (§6.2) and every `FrontRooms/Surface` pixel that lies under a registered body of water is lit through the water. The body is found **by position** (`FRSubFind`: up to 12 bodies, each a water rectangle, its height, its room's rectangle and top, its absorption scale and basin albedo), not by material, so the tile, the ladders, the props and the Relay's legs all take it with no material change; the Relay's three materials (`Creature_TwillSpruce`, `_LensOpal`, `_TapeSilver`) use `FrontRooms/Surface` (checked), so it has no shader of its own to change.

For a point d metres under the surface, per colour channel (a = (0.29, 0.048, 0.006) per metre × the zone's scale):
- **Each light** (its own loop, as `UniversalFragmentPBR` with the light refracted): a light above the water crosses the surface with 1 − F at its angle (exact unpolarised Fresnel, n = 1.333), arrives bent (Snell keeps the azimuth and divides the horizontal part by n, so the irradiance across the beam is × cos θa / cos θw: the flux per horizontal area is kept) and loses exp(−a d / cos θw); a light under the water (a wet niche) loses exp(−a |L − P|) over its whole path, so the niche's warm light turns neutral after about 2 m and mint beyond, as `17` §4.3 computes, by itself: the zone look's pre-tinted niche colours are gone (its lens colour 1/.80/.58 is the light).
- **The ambient** (the zone's probe, the light from above): × D(τ), the mean over a uniform sky of (1 − F) exp(−τ / cos θw) (the LUT's R; D(0) = 0.934).
- **The glaze under water:** against water a glaze of n 1.52 reflects 0.0043 instead of 0.0426: the specular and its grazing term × 0.10 (`_FR_SubMode.y`); the view direction is the refracted one.
- **The way out:** exp(−a d / cos θv) along the view's refracted path; the trapping, 1 / (1 − ρ P(τ)) with P(τ) the share of the upwelling diffuse light the surface sends back down (the mean over a Lambertian of the reflectance from below, 1 past the critical angle, × exp(−2τ / cos θ); P(0) = 0.475, W26's p) and ρ the basin's albedo (0.8); and 1 / n² (radiance leaving a denser medium). The surface's own 1 − F at the eye is the water shader's (§6.2), on every tier.
- **The LUT** (`FR_SubLut`): 64 texels over u = τ / (τ + 1), R = D(τ), G = P(τ), built at start-up (512-step integrals, 0.5 KB); three taps per pixel, one per channel.

**The wet band and the meniscus** (`10` §3.8, the Tile stage's `FRWetBand`): a material without its own `_WaterY` now takes the height of the body it lies in, so the ladder, a prop or the Relay's cloth darken where porous under the water and carry the film and the meniscus at the line, as the pool tile already did.

### 7.3 The absorption scale per zone (Q1) and the colours measured off the frames

The scale is a field of each zone's profile (`FrontRoomsZoneLookProfile.waterAbsorbScale`; a body may override it, `FrontRoomsWaterSurface.absorbScale`, for the "one or two rooms ×9"): **PoolClub and PoolHall ×1** (the club's water is physical), **PoolLevel ×4** (Red's "wrong" ×3-5; the turn room and the stair room are PoolLevel), **PoolDark ×3** (`18` §3: "dark tile and absorption make it read deeper"; ×3 keeps the niche pools from going black, sheet `70_submerged_q1.jpg`).

**Calibration** (`-plSubCal`, table `W/pool_look/renders/sub_v4/sub_cal.tsv`): a tank at x 768 with a white Lambertian floor (albedo 0.80, `17` §4.2's dry tile) under 0.12, 1.07 and 1.37 m of water, a uniform white sky, no lamps, looking straight down, post-processing off, a linear float readback; each patch rendered with the term on and off, so the ratio is the water's alone; × (1 − F) looking straight down (0.98); shown as `17` prints its colours (the brightest channel at the dry tile's 0.80, sRGB):

| Depth | Scale | Measured (shader) | Model (this build's formula, C#) | `17` §0 item 3 / §4.2 | Brightest channel vs dry (`17`'s "brightness vs dry") |
|---|---|---|---|---|---|
| 0.12 m | ×1 | **#D6E4E7** | #D6E4E7 | #D6E4E7 | 0.83 (17: 0.83) |
| 1.07 m | ×1 | **#92D2E7** | #92D3E7 | #8AD0E7 | 0.80 (17: 0.81) |
| 1.37 m | ×1 | **#85CEE7** | #85CEE7 | #7ACBE7 | 0.79 (17: 0.81) |
| 0.12 m | ×3 | **#BEDFE7** | #BEDFE7 | #BCDEE7 | 0.82 |
| 0.12 m | ×5 | **#ADDBE7** | #ADDBE7 | #A8D9E7 | 0.81 |
| 0.12 m | ×9 | **#92D2E7** | #92D2E7 | #8AD0E7 | 0.80 |

The shader equals the model to the hex (one level of R at 1.07 m: the LUT's half floats). Against `17`'s numbers: the shallow case and the brightness match; at depth this build is a little paler (R 146 against 138 at 1.07 m, 133 against 122 at 1.37 m): `17` made its colours from Pope and Fry's spectrum through the colour-matching functions, this build uses `17` §4.1's three effective coefficients, which cannot follow the red band's spread of absorption at depth. Not tuned away: the difference is under the frames' own lighting differences, and §4.1 asks for these coefficients.

**In the frames** (sheet, `sub_v4`): the club's basin (S1, ×1) is pale turquoise near the beach and deeper toward the deep end under P1-A's cool light; the level's ankle water (S2, S7, S8, ×4) a pale turquoise against the white tile, the "wrong" water that reads at a glance; the stair room's pit (S9, ×4 over 0.83 m) goes sky-blue (×4 at that depth is 3.3 m of real water); the dark zone (S6, ×3) deep blue-teal with aqua pools under the niches, under the 3200 K grade (Q4). Mean luma 0-255 TODAY | BEFORE | SUBMERGED: S1 135.7 | 130.7 | 123.4, S2 117.1 | 146.5 | 146.6, S3 126.3 | 135.7 | 135.0, S4 123.8 | 62.4 | 62.0, S5 139.2 | 129.0 | 122.9, S6 50.4 | 38.1 | 39.7, S7 133.7 | 159.1 | 159.9, S8 119.5 | 146.0 | 146.2, S9 157.6 | 182.6 | 182.9, S10 106.8 | 121.7 | 121.4.

### 7.4 Caustics: projected from the same surface

**The loops** (`Tools/lookdev/pool_water/gen_pool_caustics.py`): the caustics are the projection of the surface the ripple loops hold, frame by frame (`gen_pool_ripples.bake`: the exact float slope fields, before 8-bit encoding), not a separate texture. A ray crossing the surface at x leaves it bent by the slope s(x) and meets a receiver at x + D s(x), with **D = d (1 − 1/n)** for light refracted down onto a floor d under the water and **D = h (n − 1)** for light leaving the water up to a ceiling h above it; the brightness there is the density of arriving rays (`17` §4.4's "refract a grid through the surface; brightness from the area change", folds included): every source texel's energy is splatted where its ray lands on a field upsampled to 64 samples per output texel (FFT, exact for the band-limited bake), then a 0.5-texel blur. Each slice keeps its mean exactly (1.000000: energy is only moved). Four lens distances per loop go into RGBA; the shader interpolates in log D, fades to flat below the first and holds the last (the nets of random waves keep their contrast past the focus).

| Loop | Tier | Slices | Lens D (m) | Contrast (std of I) at each D | Format, memory |
|---|---|---|---|---|---|
| `FR_Caustic_Fine` (fine cascade, each desktop layer at its 1/√2 weight) | desktop | 256² × 64 (0.5 s, the ripple's slices) | 0.01, 0.03, 0.10, 0.32 (floors 4 cm to 1.3 m) | .04, .12, .36, .58 | BC7, 4.0 MiB + mips |
| `FR_Caustic_FinePhone` (layer A at full weight) | phones; WebGL's scrolled tile (slice 0) | 128² × 32 (0.32 s) | same | .02, .06, .19, .33 | ASTC 4×4 0.5 MiB; WebGL RGBA32 |
| `FR_Caustic_Long` (7-30 cm) | every tier | 128² × 128 (2.56 s) | 0.06, 0.2, 0.6, 1.8 (floors 0.24-7 m, ceilings 0.2-5.4 m) | .02, .08, .23, .62 | BC7 2.0 MiB + mips; WebGL 64 px RGBA32 |

Encoding: linear, intensity / 6 in 8 bits (box mips stay energy-true; the share of energy above 6 is 1e-6 at most); the fourth distance is the alpha channel, so the importer takes alpha from the input (`alphaUsage: 1`: the ripple sheets' template drops alpha, which read the fourth channel as intensity 6 in `sub_v1`-`v3` and made the thrown light up to six times too bright; found by the caustic check, fixed in the generator and the three `.meta` files).

**In the shader:** the loops play on the ripple's own clock (`_FR_RippleA/B/L`: the same slices and blend), so they move with the surface the player sees and **Reduce Motion freezes them with it** (measured: S7 and S6 rendered 1/30 s apart with it on differ in 0 channel values; with it off 1.8 M values differ). Desktop multiplies the fine loop at layer A, the fine loop at layer B (scale 8/7, its caustic at D × 7/8) and the long loop; phones the phone loop and the long loop (the flipbook); WebGL two scrolled layers of one tile crossing at under 4 cm/s, on the same clock (frozen by Reduce Motion too). The ripple's gain scales D (a stirred surface focuses sooner). The mip is the larger of the pixel's footprint (taken at the top of `Frag`, in uniform control flow) and the source's blur (its angular radius × the path): a large source sees flat light, a small one crisp nets, with no aliasing.

**The rings throw caustics too** (`FRRingLens`): a footstep, a drip, a wading body's packets (the Ripples stage's closed form) give each point their curvature along the radius (h″) and across it (h′/r); the lens 1 / |(1 + D h″)(1 + D h′/r)| (clamped at a quarter: a finite source never focuses to a point) multiplies the loops' caustic. Film `70_submerged_S7.mp4`: each footstep spreads a ring of light and shadow across the floor under the compact lamps.

**Which lights carry them** (`17` §4.4, §7 item 9): at most **two compact sources per body**, matched by position in the light loop (`causticSourceA/B` of the water). The harness gives each pool-level room (the turn, the three level rooms, the stair room) two hidden compact lamps: 1990 low-voltage downlights hidden at the ceiling's edge, 50 mm aperture (radius 0.025 m), daylight .90/.97/1, a 54° beam aimed across the room at the water, intensity 7.5 (at 3.2 their caustics drowned in the overheads' light, `sub_v3`). No fixture is drawn (hidden, as `17` asks); the production kit is the lamp line's. The hall's P1-A wash and the dark zone's dead overheads carry none: the basin floor under an extended source is flat light, as `17` §4.4 and `zone_lamps/10_pool.md` §5 say ("faint with the overheads on").

**What they look like, honestly:** in calm water the level's floor nets are faint. The calm ripple (2.0° rms) at ankle depth gives a contrast of about ±12-17 % in the compact lamps' light, and their cells (1-2 cm) are finer than a pixel beyond about 2 m: the caustic check (`S7_c0`, `S7`, `S7_c3`) moves the frame by 1.4 levels of std (means equal, 159.3: energy kept). The water stirred (gain 1.8) or a footstep makes them strong where they fall. This is the physics of `17`'s "near-still" water (§0 item 5): moving light comes from what disturbs the water.

### 7.5 The thrown light: per lamp, so it dims with its lamp

The zone lamps' note (`30_for_red.md` §2.3: the ceiling caustics were "one net" and a dead lamp left the ceiling as it was) is answered by building the thrown light lamp by lamp (`FRSubThrown`, above the water in a body's room: ceilings and walls):
- **Each wet niche** (registered with `FrontRoomsWaterSubmerged.RegisterNiche`; the lamp kit calls it, the harness does for the 18 P2-A stand-ins) is a glow under the water at its lamp: the light it puts into the basin comes back up from the tile around it (`17` §4.3's three bounces; I0 = 0.25 × its intensity, upward Lambertian), in the water's colour (exp(−a (2 × the lamp's depth + 1 m))).
- **Its rays leave the flat surface where Snell puts them:** the exit point solves hL tan θw + hQ tan θa = r with sin θa = n sin θw (Newton on t = tan θa, four steps: the residual is 0 to 1e-15 for lamp depths 0.05-0.7 m, ceilings 1-5.5 m, distances 0-20 m; the first version iterated on sin θw, stalled near the critical angle and drew hard-edged discs, `sub_v1`); the flat interface spreads them (E = I T sin θw / (r dr/dθw)), they carry 1 − F out, and on any surface the cosine of their arrival.
- **The caustic** of their path through the air: the long loop at D = (n − 1) × the path, blurred by the glow's size (radius 0.15 m: the niche's lens and its lit tile), and the rings' lens at the exit point of the lamp that gives the point most. With a source that wide, the thrown light is a soft glow that wavers little in calm water or under a wade and moves in broad bands only when the water is thrown about (a plunge): what an indoor pool's ceiling does at night (`17` §4.4: "thrown light on the ceiling, softened by distance").
- **Each lamp's live share** (its light's intensity against its base, every frame; 0 when off or dead) scales its glow: **a niche that flickers, dims or dies takes its part of the ceiling with it** (frame `S6_dead2`: two niches of the first dark room dead).
- **Energy:** the zone look had lifted PoolDark's ground term ×1.5 and its wall term ×1.15 to stand in for the water's glow (§3.4 item 4); the glow is now the thrown light, so both terms are back at the bounce alone, and PoolDark's `thrownLight` is 2.5: at the estimated physical share (1.0) the dark zone's ceiling stayed near black (`sub_v3` S6 mean 32), against `16` §3's night ("ceiling caustics carry the room"). S6 now 39.7 (TODAY 50.4, zone look 37.2): the ceiling and the upper walls lit teal by the water, the pools aqua under the niches.

### 7.6 The pool family's reflection cubes

- **Baked** (`-plSubProbes`, after the frames of `sub_v2`) the way the game's cubes are (`Lightmapping.BakeReflectionProbe`, 256 px, HDR, then the capture tool's importer: cube, specular convolution, 9 mips, BC6H; WebGL RGB9E5 at 128), with the zone look applied at the point (`FrontRoomsZoneLook.ApplyNow`), the water surfaces hidden (their mirror image belongs to the viewer) and the submerged term on (the basins show their colour): **`Refl_Pool`** at the hall's centre 1.6 m over the water, its lamps on; **`Refl_PoolDead`**, the same with the 16 P1-A lights dead and the niches on (the night or mains-dip state, as `Refl_DeadLamp` is Level 0's dead-lamp twin); **`Refl_PoolLevel`** in the middle level room.
- **Used** through the existing path: `FrontRoomsLook.ReflectionZone` gains `Pool`, `PoolDead`, `PoolLevel`; `FrontRoomsZoneReflection` blends them as it blends Level 0's cubes (desktop: the cube render texture lerped per face and mip over 0.5 s; WebGL: the intensity dip), so **a room whose lamps die crossfades to `PoolDead`** as a Level 0 cell crossfades to `Refl_DeadLamp`. Which cube the player's position takes is the map's (`FrontRooms3DGame.UpdateReflectionZone`): contract `W/apply/pool_submerged_contract/` (7.8).
- **In the frames** (`-plSubCubes`): the probe tiers' water reflects the hall's own cool room instead of Level 0's warm Tall cube (`S1_probe`: the khaki at grazing angles of §6.8 is gone), and every glaze in the pool rooms reflects its own family.
- **Per-room probes in the level:** each room's water already projects its cube on its own box (`FrontRoomsWaterSurface`, one body per room, §6.3); per-room *captured* cubes need either URP's probe blending (off in `FrontRooms_URP.asset`: the settings line's) or a capture per room at load: the follow-up, with the structures scene's real rooms (7.11).

### 7.7 Per tier and numbers

| | Desktop (Ultra, High) | Phones, iPad | WebGL |
|---|---|---|---|
| Under the water | own light loop, 3 LUT taps, the glaze, the trapping | same | same |
| Floor caustics | ≤ 2 compact sources per body: fine A × fine B × long (6 array taps each) × the rings' lens | phone loop × long (4 taps), rings' lens (4 ring slots) | two scrolled layers of the phone loop's slice 0 (2 taps) |
| Thrown light | per niche of the body: exit point (4 Newton steps), long loop (2 taps); rings' lens once | same | two scrolled layers of the long loop |
| Caustic memory | 4.0 + 2.0 MiB (BC7) + mips | 0.5 + 2.0 MiB (ASTC 4×4) | the phone loop and a 64 px long loop, RGBA32 |
| Cubes | 3 × 0.5 MiB (BC6H, 256 px) | same | RGB9E5, 128 px |

**GPU** (this Mac, UNDER LOAD, 7680 × 3544 as VISUAL_STANDARD 1.4, MSAA off, 20 interleaved rounds against the term off, scaled to 2560 × 1440 by pixels; `W/pool_look/renders/sub_v4/sub_timing.tsv`): S1 (the hall, 34 lights in view, the basin over a third of the frame) +1.89 ms (absorption alone +1.53), S6 +1.05 (+0.85), S7 +0.75 (+0.60); the phone path on this Mac within 0.05 ms of the desktop path. The light loop of its own is most of it (each light's refraction, Fresnel and three exponentials); the caustics add 0.15-0.35 ms. With the water surface (§6.6: S1 Ultra +1.38) all of the hall's water is about 3.3 ms at Ultra, inside `17` §6.1's ≤ 4-5.5 ms. Phones and browsers were not timed.

### 7.8 The contract

`W/apply/pool_submerged_contract/` (README, `CONTRACT_map.diff`; owner 关卡设计, never applied by this line): in `FrontRooms3DGame.UpdateReflectionZone`, inside a pool look zone (`FrontRoomsZoneLook.ZoneAt`, weight over 0.5) the cube is `PoolDead` for PoolDark or a dead, dimmed or switched-off lamp in the cell, `PoolLevel` for PoolLevel, `Pool` for the hall and the club. Inert until pool look zones exist (C1; weight 0 keeps today's choice). Base main `d059a6de`, patched `a969ce31`, `patch --dry-run -p1` clean from the course root; a different function from `pool_water_contract`'s line, so both apply in either order. It compiles after `pool_zone_look` and `pool_water` land.

### 7.9 Checks

- **Gate (rule 25)**, `W/pool_look/sub/tmp/gate.zsh` (each override its own zsh array element; the response files read: the new files are listed, UNITY_IOS in the iOS file): Roslyn against main with the twelve runtime and editor files this stage touches or depends on (`FrontRoomsWaterSubmerged`, `WaterSurface`, `WaterPlanar`, `WaterRipples`, `WaterRings`, `RippleArray`, `ZoneLook`, `ZoneLookProfile`, `ZoneLookVolume`, `FrontRoomsLook`, `FrontRoomsZoneReflection`, `FrontRoomsReflectionCapture`): macOS and iOS COMPILE OK, 0 errors, 12 overrides; with the contract's patched `FrontRooms3DGame.cs`: COMPILE OK on both, 13 overrides. The clone with the harness: COMPILE OK (`W/pool_look/sub/tmp/roslyn_s8`).
- **Shader:** compiled for Metal in every Unity run in the clone (`sub_v1`-`v4`, `sub_today`, `sub_today2`, `sub_inert_*`): 0 shader errors, 0 `error CS`.
- **Level 0's materials keep their variants:** the shader's `#pragma` and keyword lines are identical to the water surface stage's version (against main's 8f3587e0 the only difference is the Tile stage's `_FR_TILE`, a `shader_feature_local` only the pool tile enables); every new term is a branch on a global (no body registered = count 0 = nothing runs), so no material's keyword list or variant count changes.
- **TODAY unchanged, proven:** with grain and dither off (`-plNoNoise`), TODAY rendered with this stage's shader (`sub_inert_new`) and with the shader before it (`sub_inert_base`, cef7facc swapped back, the same harness) is **byte-identical in the raw PNGs on S1, S2, S3 and S4 (0 pixels differ)**. With noise on, `sub_today2` equals `tile_today` byte for byte on S2, S6-S10 and the lamp check, and the hall views differ in 0.01-0.1 % of their pixels by at most 4/255 in isolated JPEG blocks: the grain's phase, which the noise-off pair rules out as the shader's (`sub_today_base` came out on a whole other grain phase, 85-90 % of pixels by up to 16, as `today_v6` did in the zone look).
- **Calibration** (7.3): the shader equals the model to the hex at every depth and scale.
- **The other tiers' paths:** the phone flipbook (mode 2) and WebGL's two scrolled layers (mode 3) rendered on this Mac by forcing the mode (`sub_tiers`, 09:42: S7, S2, S6 `_mode_phone`, `_mode_webgl`): they compile and draw the same colours (S7 mean 159.9 on all three paths). The editor's Standalone target holds the phone loop as a 32 px stub (its platform tab), so these frames show the long loop's caustic only; on iOS the phone loop imports at 128² (its `.meta`), not seen here.
- **Reduce Motion:** frozen (0 values differ, S7 and S6); motion on, 1.8 M values in 1/30 s (`sub_checks.tsv`).
- **Energy:** the caustics move light and keep its mean (S7 with them at 0, 1 and 3: means 159.34, 159.33, 159.29).
- **Photosafety** (both films, WCAG 2.3.1 as RELAY §9.5 defines a flash; the whole frame, its quarters, the water band and the upper third): **0 flashes per second**; the largest frame-to-frame change of a mean 0.0009 of full scale (`W/pool_look/sheets/70_submerged_photosafety.json`). Large areas move only with the long cascade and the rings, every caustic slice keeps its mean.
- **Runs:** `sub_v1` (09:10-09:13: the look works; three defects found: the thrown light's hard edges, the calibration tank beyond 8 bodies, grain in the Reduce Motion check), `sub_v2` (09:17-09:21: fixed; the cubes baked), `sub_v3` (09:24-09:30: the caustic sheets' alpha fixed, the rings' lens, the cubes in use), `sub_v4` (09:31-09:32, the frames of record), `sub_today`, `sub_today2`, `sub_today_base` (another grain phase: unusable), `sub_inert_new`, `sub_inert_base` (09:37, the noise-off pair), `sub_tiers` (09:42); all FINISHED fail=0 except `sub_v1` (fail 2: the two Reduce Motion checks, before grain was turned off for them).

### 7.10 Reading the renders

- **S1, the hall (the club's water, ×1):** the basin finally has the colour `17` predicts for real water: pale turquoise at the beach, deepening down the basin to the deep end, the lane lines bending through it, the lit ceiling's patches mirrored on top. Against BEFORE the floor no longer reads as dry tile under glass.
- **S2, S7, S8, the pool level (×4):** the ankle water is the canon's turquoise over white tile, the "wrong" water `17` Q1 describes, while the walls stay porcelain white; its moving light is faint in calm water and wakes with every footstep (film S7).
- **S6, the dark zone (×3, niches only, the 3200 K grade):** deep blue-teal water, aqua pools at the niches, and the ceiling and upper walls carried by the niches' glow: `16` §3's night ("the water is the light"). Two dead niches take their share of the ceiling (`S6_dead2`).
- **S9, the stair room:** ×4 over 0.83 m reads as a deep pit, sky-blue (the Q1 sheet's flaw: ×5 and ×9 belong only to ankle-deep rooms).
- **S4, S10, at the waterline:** the course tile and the riser under the water take the water's colour and lose most of their glaze's gloss (×0.1), the meniscus and wet band at the line.
- **S5, the phone tier:** the same colours (the term is the same on every tier); the probe reflects `Refl_Pool`.

### 7.11 Limits and open items

- **Calm water makes faint nets** in the level (7.4): physics, kept honest; brighter nets would need a rougher constant ripple (the Ripples stage's calm, 2.0°) or brighter compact lamps (the lamp line's kit).
- **The thrown light's pattern is soft** (a 0.15 m glow at 0.7 m): it moves in broad bands only under strong waves; a sharper pattern would need a more compact source (the niche's lens alone, at a fraction of the energy) or reflected caustics from compact lamps above the water (a follow-up: the same lens on the reflected ray).
- **The rings' lens is a single-sheet Jacobian** per ring (clamped), not a projection: right for the packets' long crests, approximate where two rings cross.
- **The colour model** uses three effective coefficients (§4.1): at depth a little paler than `17`'s spectral numbers (7.3).
- **The stand-in lamps:** the niche lenses keep glowing when their light is cut (`S6_dead2`): the P2-A kit is the lamp line's, with its lamp logic.
- **The cubes** come from the stand-in rooms; rebake them in the structures scene once its pool rooms exist (the recipe is `SubProbes`). Per-room captured cubes in the level: a follow-up (7.6).
- **Not verified here:** Play mode (`Publish` from `onBeforeRender` through `FrontRoomsWaterPlanar`), the Scene view, the include's GLES3/WebGL, Vulkan and D3D compiles (Metal only), GPU time on any phone or browser, the crossfade to `PoolDead` in Play mode (the existing blend path; edit mode switches at once).

### 7.12 For the next stages

- **Renders:** the walk film and the decision sheets with every layer on (`-plZone pool -plLamps pool -plTile pool -plWater pool -plSub pool -plSubCubes`); the Q1 sheet is the one open choice (WAIT-RED).
- **Package:** `pool_water` takes `FrontRoomsWaterSubmerged.hlsl` (+ meta), `FrontRoomsWaterSubmerged.cs` (+ meta), the changed `FrontRoomsWaterSurface.cs` and `FrontRoomsWaterPlanar.cs`, the three caustic loops + stats (+ metas), `gen_pool_caustics.py`, the three cubes (+ metas), `FrontRoomsLook.cs`, `FrontRoomsZoneReflection.cs`, `FrontRoomsReflectionCapture.cs` (bases 898dee87, 040ba42b, ad9ebcb1 = main); `pool_zone_look` takes the changed profile and the four rewritten profile assets; `FrontRoomsSurface.shader` 8067dc67 is the one payload all three tracks share (base 8f3587e0, unchanged in main) and it now includes `FrontRoomsWaterRipples.hlsl` and `FrontRoomsWaterSubmerged.hlsl`: **the shader cannot land before both includes** (one combined landing, or `pool_water` first). The contract stays a contract.
- **Harness:** `-plSub pool` with `-plSubCubes`, `-plSubCal`, `-plSubStill`, `-plSubDead`, `-plSubCaustic`, `-plSubSweep 1,3,4,5,9`, `-plSubTiming`, `-plSubFilm S6,S7`, `-plSubProbes`, `-plNoNoise`; media `W/pool_look/sub/tmp/make_sub_media.py`, sheets `make_sub_sheets.py`, gate `gate.zsh`.
- **Deletion candidates** (inside `W/pool_look`, Red deletes): `renders/sub_v1`, `sub_v2`, `sub_v3` (superseded by `sub_v4`), `sub_today`, `sub_today_base` (grain phase); `sub/tmp/roslyn_s1`-`s7`, `sub/tmp/view/`, `sub/tmp/gentest`. Keep `renders/sub_v4`, `sub_today2`, `sub_inert_new`, `sub_inert_base`, `sub_tiers`, `sub/video`, `sub/qa`, `sub/contract`, `sub/tmp/gate_g2_*`, `roslyn_s8`, the scripts.

## 8. Renders

Stage 8 of the workflow, 2026-11-04 09:44 to 10:45. Account G's agent read and planned (09:44-10:02) and wrote the harness partial; it died at G's limit at about 10:03 before any run; account B's agent (the relay of `wf_d2994299-a35`) finished the stage from the journal. **Nothing landed in main:** main was read only, no Unity ran on main, nothing was downloaded, no package was built, and this stage changed no runtime or package file (only the clone-only harness). What Red sees and decides: `71_look_for_red.md` (Chinese), with the sheets and the film in `renders/look/`.

![Final renders](renders/look/71_look_final.jpg)

### 8.1 Runs

| Run | When | What | Result |
|---|---|---|---|
| `rend_v1` | 10:17:11-10:18:24 | every layer on (`-plZone pool -plLamps pool -plTile pool -plWater pool -plSub pool -plSubCubes -plCubeContract`), every camera at the phone tier (`-plPhoneAll`), the D8 sweep T1-T6 on S1 S3 S4 S8, the Q1 sweep ×1 ×3 ×4 ×5 ×9 on S2 S7 S9 S6, the Relay in the dark zone at the thrown light ×1 / ×2.5 / ×4 (`-plRelay -plNight`), cost per camera (`-plCost`), the walk film (`-plWalk`) | FINISHED fail=0 in 59.9 s, 80 stills + 699 film frames, 0 `error CS` |
| `rend_today` | 10:18:40-10:19:01 | TODAY on every camera, the Relay check, cost | FINISHED fail=0 in 13.4 s |
| `rend_walk2` | 10:21:41-10:22:19 | the walk again: the dark zone's last leg ends on the arch's centre line (in `rend_v1` it ended 0.1 m inside the left jamb, which filled the last second) | FINISHED fail=0 in 26.0 s; the film of record |

Frames: `W/pool_look/renders/rend_v1/`, `rend_today/`, `rend_walk2/walk/`.

**The harness partial** (clone only): `Assets/Editor/PoolLook/FrontRoomsPoolLook.Renders.cs` (321df097, meta guid 85cdfb81dfd64d0bb3676d7c72ffbd78) and one hook in `FrontRoomsPoolLook.cs` (3e0153d6; pre-edit copy `W/pool_look/rend/base/`): `-plPhoneAll` (every desktop camera again at the phone tier, `<cam>_ph`; S5 already is S1's), `-plRelay` and `-plNight v,v,v` (the production Relay, `FRONTROOMS_RELAY_V2`, walks the last 0.96 m toward S6 at 0.8 m/s with its lens at Walk and its own wading rings; R1 4.1 m ahead before the back wall, R2 4.3 m ahead before the west wall; for each, noise-off raw PNG pairs with and without it), `-plCost` (renderers, submeshes, triangles and lights in each view; Unity's profiler draw counters were read around each render and did not move in an edit-mode batch), `-plWalk` (the film), `-plCubeContract` (each frame takes the reflection cube `pool_submerged_contract` would give it, `FrontRoomsZoneLook.ZoneAt` at the eye: only the club corridor changes against `-plSubCubes`, Level0 → Pool, so the walk has no cube pop at the arch).

### 8.2 Sheets and film (staged for `renders/look/`)

| File | Size | sha1 | What |
|---|---|---|---|
| `71_look_final.jpg` | 3838 × 8364 | c769ace9 | TODAY \| DESKTOP ULTRA \| PHONE TIER on S1-S10 (S5 = S1 at the phone tier), COST; WAIT-RED |
| `71_tile_d8.jpg` | 3835 × 2674 | 475d114f | D8 under the finished water: TODAY \| T1 REC \| T2-T6 on S1 S3 S4 S8, an 800 × 450 crop of S8 at 1:1, COST; WAIT-RED |
| `71_water_q1.jpg` | 3838 × 2554 | 3dced059 | Q1: TODAY \| ×1 \| ×3 \| ×4 \| ×5 \| ×9 on S2 S7 S9 S6, REC per row, the floor's colour measured on each frame, COST; WAIT-RED |
| `71_night.jpg` | 3840 × 3459 | 4e26f55b | the dark zone's thrown light: TODAY \| N-A ×1 \| N-B ×2.5 REC \| N-C ×4 with the Relay at R1 and R2, a ×1.5 crop, the empty room, the rule 11 numbers; WAIT-RED |
| `71_look_walk.mp4` | 17.4 MB | daa69316 | 23.3 s at 30 fps, 1920 × 1080, H.264 CRF 23 (preset slow), yuv420p (tv range, BT.709), faststart, one stream (silent), no logo on the picture: the club corridor through the arch onto the hall's west deck in one shot (285 frames, 12.85 m, 1.35 m/s, the slow gait), cut, the pool level's first room through its arch (210 frames, 9.25 m, 1.32 m/s), cut, wading in the dark zone toward its arch (204 frames, 4.81 m, 0.71 m/s); no head bob, the player's own rings |

Every label checked at full size: fit files `W/pool_look/sheets/71_*_fit.txt`, each ending ALL LABELS FIT. Builder `W/pool_look/rend/tmp/make_rend_sheets.py` (86f42acc).

### 8.3 Cost

| | TODAY | Desktop Ultra | Phone tier (iOS) |
|---|---|---|---|
| Textures | (Level 0's) | **88.5 MiB**: tile 55.0 (BC7), ripples 24.0 (BC5), caustics 8.0 (BC7 with mips), cubes 1.5 (BC6H) | about **15.5 MiB**: tile 6.1 (ASTC 6×6, estimate; 13.8 as BC7), ripples 5.4, caustics 2.5 (ASTC 4×4), cubes 1.5 |
| Mirror image | none | 6.10 MiB at 1080p, 10.85 at 1440p per water height (the hall shows two); High 4.82 at 1440p | none (probe and lamp reflections) |
| GPU, 1440p, measured per stage, UNDER LOAD | - | hall water +1.38 ms (§6.6) + submerged +1.89 (§7.7) = about 3.3 ms at Ultra (`17` §6.1: ≤ 4-5.5); High water +0.51; ripple +0.35-0.43 inside the water's figure; 16 rings worst case +2.2; tile and zone look not timed (about 40 and 20 ALU) | not measured on a phone; on this Mac the water's probe path +0.55 ms (S1), the submerged term's phone path within 0.05 ms of the desktop one |
| Draws | UNMEASURED | UNMEASURED (the profiler counters stay at 0 in an edit-mode batch; `rend_cost.tsv` says so per row); each renderer in view is at least one draw per pass, and the mirror draws its room again per water height | UNMEASURED |
| Renderers in view (`rend_cost.tsv`) | S1 172, S2 64, S3 247, S4 6, S6 50, S7 55, S8 5, S9 19, S10 7 | S1 110, S2 66, S3 147, S4 7, S6 83, S7 57, S8 6, S9 19, S10 7 | S1 (S5) 56, S2 19, S3 68, S6 23, S7 17 (the kits' phone culls) |
| Triangles in view | S1 20.7 k, S6 6.3 k | S1 33.0 k, S3 38.0 k, S6 30.0 k (the ten P2-A stand-in niches) | S1 3.5 k, S6 1.2 k |

The hall's renderer count falls (one P1-A wall box per bay instead of a troffer per 3 m cell); the dark zone's rises with the stand-in niches, which are the zone lamps study's kit (the lamp line's production kit replaces them).

### 8.4 Rule 11: the Relay in the dark zone

The Zone look stage asked for this check (§3.8: "darkness is never cover"). The production Relay walks toward the player at 0.8 m/s with its lens at Walk (0.25 and its 1.6 m self-light, `FrontRoomsRelayLens`); noise-off pairs of the same frame with and without it, above the water line (its mirror image and its legs under the water are below it): the silhouette is every pixel it darkens by more than 3 levels (about 65,000 px), the background the hidden frame at those pixels (`W/pool_look/rend/qa/relay_contrast.tsv`, script `rend/tmp/relay_contrast.py`).

| | Weber contrast, linear (R1 / R2) | Relay luma / background luma (sRGB, R1 / R2) | Lens peak | Frame mean |
|---|---|---|---|---|
| TODAY (Level 0 look, every lamp dead) | -0.585 / -0.575 | 33.0 / 56.4, 32.7 / 54.8 | 81 | 49.3 |
| N-A ×1 (the physical share) | -0.560 / -0.654 | 20.0 / 33.7, 20.2 / 38.2 | 85 | 32.7 |
| **N-B ×2.5 (as built)** | -0.639 / -0.783 | 21.6 / 41.0, 21.5 / 51.8 | 85-92 | 39.5 |
| N-C ×4 | -0.689 / -0.839 | 22.9 / 47.5, 22.5 / 64.6 | 86-114 | 45.6 |

At every value the Relay is a dark figure against the lit walls and ceiling, with its lens lit: rule 11 holds and does not decide between the values; the choice is the room's mood (`71` item 3).

### 8.5 Caustic strength: not a row

The caustics' strength is not a visible choice at the game's framing: on the crouched view of the level (S7, the frames of record `sub_v4`, rows 700-1080, the floor under the ankle water) the fine-scale contrast (luma minus its 13 px box blur, std in 8-bit levels) is 7.70 with the caustics off, 7.73 at ×1, 7.84 at ×3 and 7.40 stirred (the stirred ripple blurs the refracted joints more than its nets add); the floor's mean moves by 0.2 of a level (energy is only moved, §7.4). In calm ankle water the nets are physically faint; a footstep or a wade makes them strong where they fall (films `70_submerged_S7.mp4`, `70_ripples_rings.mp4`). Clear nets on a basin floor need a compact source over deep water, which is the P1 lamp pick (P1-A is an extended wash: no nets, `17` §4.4); `71` says so under P1.

### 8.6 Checks

- Roslyn, the clone with the harness: COMPILE OK, 0 errors (`W/pool_look/rend/tmp/roslyn_r1`, `r2`, `r3`; the response file lists the partial). The Renders partial and the edited hook are ASCII and LF.
- Every run FINISHED fail=0, 0 `error CS`, 0 shader errors in `W/pool_look/logs/rend_*.log`.
- Photosafety of the walk film (WCAG 2.3.1 as RELAY §9.5 defines a flash, whole frame, quarters, the water band and the upper third; `W/pool_look/rend/qa/71_look_walk_photosafety.json`): at most 2 opposing changes of 10 % in any 1 s, counted conservatively from extremum to extremum; both are the two cuts (the largest step, 0.34 of full scale, is the cut into the dark zone); the limit is 3.
- The phone tier's frames read as the desktop's (same colours; S2 146.0 against 147.2, S7 160.0 against 160.4).

### 8.7 Limits

- The rooms, the P1-A wall boxes and the P2-A niches are stand-ins; the frames show the look, not the production kits. S4 at the hall's basin wall stays dim (mean 62) because the stand-in's ceiling bounce barely reaches the basin's inner faces.
- Draw counts are unmeasured; GPU figures are the earlier stages' and UNDER LOAD; nothing was timed on a phone or in a browser.
- The walk film's cuts are edits, not game transitions; the film shows the look in motion, not the level's route.
- The Relay check covers the Walk state at 4.1-4.3 m; Listen (the 0.06 standby glow) in the dark zone at longer range is the hunter line's test (`hunter/production_b/20_build.md` §"Darkness is never cover").

### 8.8 For the Package stage

- Nothing new to package from this stage: the packages' inputs are those of §3.10, §4.11, §5.10, §6.12 and §7.12. The look values that `71` puts to Red (D8, the Q1 placement, PoolDark's thrown light) are one value each after landing.
- Deletion candidates (inside `W/pool_look`, Red deletes): `renders/rend_v1/walk/` (superseded by `rend_walk2`, 218 MB), `rend/tmp/view/` (7.4 MB of review contact sheets), `rend/tmp/roslyn_r1`-`r2`. Keep `renders/rend_v1` (stills), `rend_today`, `rend_walk2`, `rend/qa`, `rend/sheets`, `rend/video`, `rend/tmp/roslyn_r3`, the scripts.

## 9. Package

Stage 9 of the workflow, 2026-11-04 10:37-12:3x. The first agent (account B, relay of G's `wf_d2994299-a35`) stopped at about 10:40 with an inventory only; a second agent on B built the stage from the journal from 11:32. **Nothing landed in main:** main was read only, no Unity ran on main, nothing was downloaded; the packages and contracts are in `W/apply` for the line's session to land. The 10:33 note from 水体仿真调研 (relayed by the line's session in the journal) was checked in code and answered in 9.3.

![Water probe](sheets/70_package_water_probe.jpg)

Sheet `sheets/70_package_water_probe.jpg` (3840 × 1600, Shot format, chip PASS; fit `W/pool_look/sheets/70_package_water_probe_fit.txt`: ALL LABELS FIT): the water against a local box probe, 9.3.

### 9.1 Tracks and the landing order

| Track (`W/apply/`) | What | Paths | `FrontRoomsSurface.shader` | Dry run on main today |
|---|---|---|---|---|
| `pool_zone_look` | the zone look (3.1): `FrontRoomsZoneLook`, `FrontRoomsZoneLookProfile` (with the Submerged stage's two fields), `FrontRoomsZoneLookVolume`, the four profiles and four grades | 25: 24 new (3 C#, 8 assets, their metas, 2 folder metas), 1 replaced; 1 kept (the shader's `.meta`) | 8f3587e0 → 37ccbb59 (the height fog) | DRY RUN OK |
| `pool_tile` | the tile (4.2): `gen_pool_tile.py`, 36 maps, 26 `Prop_PoolTile_*` materials, the manifest, `FrontRoomsPoolTile` | 133: 132 new, 1 replaced; 1 kept | 37ccbb59 → cef7facc (`_FR_TILE`, the wet band) | refuses on the shader only, until `pool_zone_look` lands |
| `pool_water` | ripples and rings (5.1), the surface (6.1, with 9.3's probe), submerged and caustics (7.1), the pool cubes, the Relay's rings | 50: 45 new, 5 replaced (the shader; `FrontRoomsLook.cs` 898dee87 → 61f9b581, `FrontRoomsZoneReflection.cs` 040ba42b → fc95d511, `Editor/Rendering/FrontRoomsReflectionCapture.cs` ad9ebcb1 → 7a4f7c55, `FrontRoomsRelayRig.cs` 542df78c → 5c19eb59); 5 kept (their metas) | cef7facc → 8067dc67 (submerged, caustics, thrown light) | refuses on the shader only, until `pool_tile` lands |

- **Order: `pool_zone_look`, `pool_tile`, `pool_water`.** Each lands alone and compiles (9.2). The shader is stacked because each stage added one term to `FrontRooms/Surface` and each version is the one that stage proved inert in Unity (3.7, 4.7, 7.9); the stacked bases make each script refuse a wrong order. C# depends the same way: the water reads the zone look (`ZoneAt`, `Profile`, the profile's `waterAbsorbScale` and `thrownLight`); the tile's C# needs only main.
- **After the last landing** `pool_zone_look.sh` and `pool_tile.sh` print REFUSED on the shader (it holds the later version, "partially applied"): expected, not a fault; `pool_water.sh` prints ALREADY APPLIED. The MERGED rows are the record.
- `FrontRoomsRelayRig.cs` (this line's file, base 542df78c = main): three lines in `TickAnimation`, after the animation clock: `if (Application.isPlaying) FrontRoomsWaterRings.ReportBody(FrontRoomsWaterRings.Body.Relay, transform.position, .3f);` with a comment (6.10).
- Built by `W/pool_look/pkg/tools/build_overlays.py` (the overlays: only files that differ from main; LF; ASCII except main's own lines in the Surface shader and `FrontRoomsReflectionCapture.cs`; every new asset with its `.meta`; no new GUID among main's 3,636) and `mkapply_stacked.py` (`W/tools/mkapply.py` with a base view of main plus the earlier tracks). Diffs: 2,091, 11,302 and 8,580 lines. Manifest: `W/pool_look/pkg/tmp/overlay_manifest.tsv`. API rows the scripts check on main: `FrontRoomsPostStack.EnsureZoneVolume`; `FrontRoomsTrofferKit.PhoneTier`; `FrontRoomsQuality.CurrentPreset`, `LowPowerActive`, `FrontRoomsHandheld.ReduceMotion` (all ok).

### 9.2 Gate (rule 25)

- **Roslyn against main**, every landing state, macOS and iOS (`W/pool_look/pkg/tools/gate.zsh g1 <state>`, each override its own zsh array element; the response files read: the override paths listed, `UNITY_IOS` in every iOS file): `pool_zone_look` alone (3 overrides), `pool_tile` alone (1), zone look + tile (4), all three (14), all three + the three contracts' patched files (16): **COMPILE OK, 0 errors** on both platforms (`W/pool_look/pkg/gate/g1_*`).
- **Shader, one Unity batch in the clone with the final files** (`pkg_wnew`, 11:51, Unity 6000.6.3f1, OSXUniversal): `FrontRooms/Surface` (8067dc67) and `FrontRooms/Water` (76892c0f) 0 errors (`ShaderUtil`), 0 shader errors and 0 `error CS` in the log. **Level 0's materials keep their keywords:** main's 225 `FrontRooms/Surface` materials hold 7 keywords (`_FR_MESH_UV` 186, `_EMISSION` 27, `_FR_DAMAGE` 12, `_FR_OWN_LIGHTS` 11, `_FR_PRINT` 5, `_FR_SHEEN` 4, `_FR_LAMP_FIELD` 1; none invalid): all declared by the final shader; the clone's 245 Surface and Water materials load with exactly their files' keywords (`renders/pkg_wnew/pkg_matcheck.tsv`). The intermediate versions: the keyword pragmas of 37ccbb59 equal main's, those of cef7facc and 8067dc67 are main's plus `_FR_TILE` (a `shader_feature_local` only the 26 pool tile materials enable); 37ccbb59 compiled with 0 shader errors in `zl_v8`/`zl_v8L`, cef7facc in `tile_today` and `tile_v2`.
- **Dry runs and rehearsal:** `pool_zone_look.sh` DRY RUN OK on main. On a scratch copy of main (`W/pool_look/tmp/rehearse_pool`: `ProjectVersion.txt`, all 3,673 `.meta` files, the 9 base and API files), in order, each track DRY RUN OK, then `--apply` **MANIFEST OK** (25, 133, 50 paths; 1, 1, 5 kept metas unchanged), then `pool_water` ALREADY APPLIED (logs `W/pool_look/pkg/logs/rehearse_*`, `dry_main_*`).

### 9.3 The water's probe (the 10:33 finding)

- **The finding** (水体仿真调研, relayed by the line's session): `FRProbeSample` in `FrontRooms/Water` read `_GlossyEnvironmentCubeMap` under the cluster loop (Forward+, the game's renderer), so the structures scene's hall probes (`FrontRoomsStructuresLook.Probe`: realtime, box projection) never reached the water on the probe tier, nor on water heights past the mirror budget (2 at Ultra, 1 at High). Confirmed in code.
- **What this project's URP does** (read in URP 17.6.0): `FrontRooms_URP.asset` keeps probe blending off (`m_ReflectionProbeBlending: 0`, GPU resident drawer off), so under Forward+ URP asks for per-object probes (`GetPerObjectLightFlags`: `PerObjectData.ReflectionProbes`) and never uploads the cluster's probe atlas (`ForwardLights` calls `UpdateGpuData` only with `reflectionProbeAtlas`, which needs blending). `FrontRooms/Surface` reads `unity_SpecCube0` (no box projection: that keyword is off). Sampling the atlas "the URP way" would read nothing here.
- **The choice:** the water reads `unity_SpecCube0` on every path, as Surface does. A probe with box projection (`unity_SpecCube0_ProbePosition.w > 0`, the hall probes) is projected on its own box from its own position (URP's `BoxProjectedCubemapDirection`); without one the renderer gets the default reflection, the zone cube, projected on the body's room box from its capture point as before. Water shader 1de7c0e4 → 76892c0f (one function, 6 lines, and two comments); `FrontRoomsWaterSurface`'s public API unchanged (水体仿真调研's stage 2 builds on it).
- **Numbers** (`renders/pkg_wold` = the water of record, `pkg_wnew` = this; every pool layer, grain and dither off, raw PNG, 1920 × 1080; `W/pool_look/pkg/tmp/compare_p1.json`):

| Check | Result |
|---|---|
| No local probe, desktop Ultra, S1 S2 S6 S7 | 0 px differ |
| No local probe, probe tier, S2 S6 S7 | 0 px differ |
| No local probe, probe tier, S1 | 11 px of 2,073,600 by 1/255 (float rounding of the recompiled function) |
| Water renderers BlendProbes instead of Off, no local probe | 0 px differ from Off, both shaders |
| A box probe over each water room (Custom, `Refl_PoolDead`), probe tier, S1 | water of record: water luma -0.04 (it ignores the probe) while the tile around it changes -0.93; Package: water -6.79 over 563,977 px, the tile the same -0.93 |
| The same, S2 (the level's ankle water) | record -0.06, Package -2.89 over 485,284 px; tile -7.07 both |
| The same, S1 at Ultra | 3,718 px by at most 4/255: the mirror image's edges fall back to the probe |

- **Limits:** with blending off Unity gives each renderer one probe (the most important at its bounds), so a water renderer must not span two halls (one `FrontRoomsWaterSurface` per room, as 6.3 asks). If the settings line turns probe blending on, Surface and Water both need the atlas path: one change for both, not this package.

### 9.4 Inert check: what in the game can reach the new code

| Entry | Reached when | With no pool zone, look volume or water |
|---|---|---|
| `FrontRooms/Surface` (every Surface material) | every frame | the height fog runs only while `_FR_HeightFog.x > 0` (a zone writes it per context), the submerged term only with a water body registered (global count 0), the wet band only where a material's `_WaterY` is above -999 (default -1000), `_FR_TILE` only in the 26 pool tile materials: pixel-inert, proven per stage (`today_v8` = `today_v7`; `tile_today` = `today_v8`; `sub_inert_new` = `sub_inert_base`, 0 px) |
| `[RuntimeInitializeOnLoadMethod]` statics: `FrontRoomsZoneLook`, `FrontRoomsPoolTile`, `FrontRoomsWaterRipples`, `FrontRoomsWaterRings`, `FrontRoomsWaterPlanar`, `FrontRoomsWaterSubmerged` | once at load | static resets; `FrontRoomsZoneLook` subscribes `SceneManager.sceneUnloaded` (removes nulls from an empty list, unhooks nothing); `FrontRoomsWaterSubmerged` writes its two count globals = 0 (the off state) |
| `RenderPipelineManager` hooks (zone look), `Application.onBeforeRender` (mirrors) | only after a `FrontRoomsZoneLookVolume` / `FrontRoomsWaterSurface` is enabled | never subscribed |
| `FrontRoomsZoneLookVolume`, `FrontRoomsWaterSurface` (`[ExecuteAlways]`) | when placed | new GUIDs: no scene or prefab of main references them |
| `FrontRoomsLook.ReflectionZone` (+ Pool, PoolDead, PoolLevel, appended), `FrontRoomsZoneReflection` (cube arrays of 7) | every zone switch | cubes load one zone at a time on request; the game asks only for Level0, Office, Tall, DeadLamp until `pool_submerged_contract` |
| `FrontRoomsReflectionCapture` (editor menu) | when run | skips the pool zones; Level 0's four cubes are captured as before |
| `FrontRoomsRelayRig.TickAnimation` | every Relay frame, Play mode | `ReportBody` finds no registered water and returns after noting the position |
| `FrontRoomsRippleArray` (editor) | menu only | not a postprocessor (5.7) |
| The contracts' map code | chunk builds | places volumes only while a pool plan is held (the structures scene) |

**Not inert: build content (9.5).**

### 9.5 Build content (open, for the line's session and Red)

Everything under `Assets/Resources` ships in every player build, used or not. The three packages put about **96.8 MiB of PNG and EXR** there (tile 44.1, ripples 33.7, caustics 15.5, cubes 3.5). In a player that is about **100 MiB of textures on desktop** (tile maps 55.0 + 13.8, ripple arrays 24.0, caustic loops about 8, cubes 1.5), **about 75-80 MiB on iOS and WebGL** (the tile maps have no platform override, so their phone and desktop sets both import at high-quality 8 bits a texel; the arrays are per platform, 5.4 and 6.8 MiB): estimates from 4.6, 5.6 and 7.7, not measured in a build (rule 25). Nothing loads them in the game, but they make every shipped build that much larger until the pool enters the game; the structures scene keeps its own textures out of builds (`Assets/Structures/Textures`, serialized fields; scene 4 is not in the build list).
- **(a) Land as built:** scene 4 has the look at once; every build carries the textures until the pool enters the game.
- **(b) REC: an editor-only home until Red takes the pool into the game.** Unity strips a `Resources` folder that sits inside an `Editor` folder from builds and still loads it in the editor (Unity Manual, special folder names): the textures, materials and cubes move to `Assets/Editor/Rendering/Resources/` with the same Resources paths and the same GUIDs, so `Resources.Load` and every material keep working in the editor and in scene 4, and builds stay as they are; one move back when the pool enters the game. A follow-up package of this line (the generators' output paths, `FrontRoomsRippleArray`'s paths, one harness run to confirm the load), about an hour; the C# and the shaders stay where they are.

### 9.6 Contracts

- **`W/apply/pool_zone_look_contract/`** rewritten for main as it is now: C1 and C2 have landed (`ZoneTheme` PoolClub, PoolLevel, PoolDark; `FrontRoomsMapWorld.WaterAt`; the pool pass `FrontRoomsMapWorld.Pool.cs`). `AddPoolLooks` returns unless a pool plan is held, `PoolLookZone` compares the enum and skips non-pool cells, the height fog takes the rectangle's C2 water height. Base `FrontRoomsMapWorld.cs` d660ed55 (the 11-03 diff against b71b023f no longer applied), patched 178e425b, `patch --dry-run -p1` clean. Inert in the game (a plan is held only on the structures scene's profile copy).
- **`W/apply/pool_water_contract/`** (settings: the mirror renderer, the full-resolution opaque copy, the tier rule; map: the player's rings): bases unchanged (`FrontRooms_URP.asset` 18584f2b, `FrontRooms3DGame.cs` d059a6de), dry runs clean; a re-check section added. The map's own water (the pool pass's greybox water and caustics) is described there as the map's next step (one water renderer per room and height with `FrontRoomsWaterSurface`), no diff yet.
- **`W/apply/pool_submerged_contract/`** (the pool cubes in `UpdateReflectionZone`): base unchanged, dry run clean, both map diffs apply together (44a6d8f2).
- All three contracts compile with all three packages (9.2, state `con`).

### 9.7 The structures scene (RULES 5b, scene 4): what to drop on a stand-in pool

Once the three packages have landed, a pool room in scene 4 shows the look with:
1. **The zone look:** a `FrontRoomsZoneLookVolume` per pool room (Add Component > FrontRooms > Zone look volume): `zone` PoolClub, PoolHall, PoolLevel or PoolDark; `center`/`size` the room's box (local), `blendDistance` 2.5, `hasWater` + `waterLevel` (local y of the water: the height fog sits on it). With `pool_zone_look_contract` applied the map places them from the pool plan. `FrontRoomsStructuresLook` keeps its `Game` preset (pool grade weight 0, no trilight of its own) in districts with volumes, else its global pool grade (priority 100) sits on top of the zone's grade.
2. **The water:** per room and water height a flat mesh at the water (MeshRenderer, shadows off) with a material on `FrontRooms/Water` (`new Material(Shader.Find("FrontRooms/Water"))`; the defaults are the tuned values) and a `FrontRoomsWaterSurface` (Add Component > FrontRooms > Water surface): `kind` Basin or Ankle, `roomHeight` the ceiling over the water, `roomMargin` out to the walls, `probeHeight` 1.6; for floor nets `causticSourceA/B` (up to two compact lamps); `absorbScale` and `thrownGain` -1 take the zone's (Q1: club 1, level 4, dark 3). The component puts its object on the Water layer itself. The stand-in water and greybox caustics go in those rooms (the submerged term draws the water's colour and the caustics on whatever lies under it).
3. **The tile:** renderers take `FrontRoomsPoolTile.Get(kind)` (Field, FieldClub, Course, CourseClub, Cap, Mosaic, MosaicClub, Lane, Deck, Tread, Riser, Nosing, Coping; the `_Phone` twins come by tier) or the materials `Resources/Surfaces/Pool/Prop_PoolTile_<kind>.mat` directly; pieces that meet water take `FrontRoomsPoolTile.ForWater(kind, waterY)` (wet band, meniscus). Rows are world-planar on 150 mm from world 0 (a floor off the rows: a V offset, 4.3); coping and nosing need the mesh UVs of 4.10.
4. **Reflections:** the hall probes of `FrontRoomsStructuresLook.Probe` now reach the water as they reach the tile (9.3). Where no local probe covers a pool, `FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Pool / PoolDead / PoolLevel, ...)` gives the pool family's cubes (rebake them in scene 4's rooms with the recipe of 7.6 once they exist).
5. **The Relay's rings** come by themselves in Play mode; the walker's need `pool_water_contract`'s map line, or scene 4's bootstrap calls `FrontRoomsWaterRings.ReportBody(FrontRoomsWaterRings.Body.Player, feet, .25f)` each frame.
6. **First look in scene 4:** the first mirror render may draw the pool tile red for one frame (6.11); the lamp presets for P1/P2 are `FrontRoomsZoneLook.Lamps(zone)`, the kits themselves stay Red's pick.

### 9.8 Combined-test rows (rule 25)

For `handoff/visual_combined_test.md`, one heading per landing (time and account filled in when landed):

- `pool_zone_look`: [ ] Game, Level 0 and Office (no pool plan): no look volume exists and every frame is as before (`FrontRoomsZoneLook.AppliedContexts` 0). [ ] Scene 4 with volumes: club → hall → level → dark zone, the ambient, haze and grade change over 2.5 m before each arch (hall 0.42 at 1.9 m, 0.87 at 0.9 m); club neutral cool grey, hall white and cool, level high-key cool white, dark zone near-black teal; leaving every volume gives Level 0's values back exactly; a scene saved in the editor keeps no zone ambient.
- `pool_tile`: [ ] Scene 4: the 26 materials on the stand-ins: porcelain white field, 3 mm grout, the aqua course on whole 150 mm rows, a dark nosing on every stair edge, lane lines; wet band and meniscus at the water; phone tier takes the `_Phone` twins. [ ] Every other Surface material unchanged (main's 7 keywords all declared). [ ] Build size delta measured (9.5).
- `pool_water`: [ ] Scene 4, desktop Ultra: the room mirrored in the water (up to 2 heights; High 1, every second frame), Medium/Low and phones the probe and lamp reflections; under the water the club's pale turquoise deepening to the deep end, the level ×4, the dark zone ×3 lit by the niches' thrown light; faint nets in calm water that wake with footsteps. [ ] A hall probe reaches the water as it reaches the tile (9.3). [ ] The Relay leaves rings in water; Reduce Motion freezes ripples and rings; 0 flashes a second. [ ] Level 0 and the game unchanged; the capture menu still captures Level 0's four cubes. [ ] GPU on device (desktop 1440p hall ≈ +3.3 ms UNDER LOAD; phones and WebGL never timed).

### 9.9 Files of this stage

- Clone only (never land): `Assets/Editor/PoolLook/FrontRoomsPoolLook.Package.cs` (`-plProbePair`, `-plLocalProbe`, `-plMatCheck`; `.meta` cf92a6e7) and two hook lines in `FrontRoomsPoolLook.cs` (pre-edit copy `W/pool_look/pkg/base/`); the clone's water shader is now 76892c0f (pre-edit copy `pkg/base/FrontRoomsWater.shader.pre_pkg`). Roslyn on the clone: COMPILE OK (`pkg/tmp/roslyn_c2`).
- Tools: `W/pool_look/pkg/tools/` (`build_overlays.py`, `mkapply_stacked.py`, `gate.zsh`, `chain_p1.zsh`, `compare_p1.py`, `surface_mats.py`, `make_zl_contract.py`, `make_pkg_sheet.py`).
- Deletion candidates (inside `W/pool_look`, Red deletes): `pkg/view/` (the base views, rebuilt by `mkapply_stacked.py`), `tmp/rehearse_pool*`, `tmp/pool_*_applied_manifest.txt` (the scratch rehearsal), `pkg/tmp/panels/`, `pkg/tmp/roslyn_c2`, `pkg/tmp/c_*.txt`, `m_to_c_candidates.txt`, `m_newer_or_diff.txt` (inventories). Keep `renders/pkg_wold`, `pkg_wnew`, `pkg/gate/`, `pkg/overlay/`, `pkg/contract/`, `pkg/logs/`, the tools.

## 10. Landing (2026-11-04 12:52-13:04, account A)

- **Landed in order:** `pool_zone_look` 12:52 (25 paths; `FrontRooms/Surface` 8f3587e0 → 37ccbb59), `pool_tile` 12:52 (133 paths; → cef7facc), `pool_water` 13:04 (50 paths; → 7be4e235). Gate per landing state on main as it stood then (after 关卡设计's and 交互设计's 12:46-12:48 landings): `fr gate` macOS + iOS 0 errors with 3, 4 and 10 overrides; each dry run OK on main; MANIFEST OK. Rows in LANDINGS.md, `W/apply/MERGED.md`, `handoff/visual_combined_test.md`.
- **The fix that held `pool_water` (12:31-13:04).** The Submerged stage's `FRSub sub = FRSubFind(...)` (top of `Frag`) and the paper substrate's `half3 sub` inside `#if defined(FR_SUBSTRATE)` share one scope. `FR_SUBSTRATE` is defined only for `_FR_PRINT` off GLES3 / WebGPU, so every hung-paper variant (Level 0's walls) failed with "redefinition of 'sub'" on Metal and D3D, while WebGL and every material rendered in this stage's harness compiled (found by 水体仿真调研 in their render clone). Fix: the substrate's local is `subN` (three lines, 1087 / 1088 / 1092); nothing else changed (8067dc67 → 7be4e235).
- **Why §9.2's check missed it, and the check that replaced it.** `-plMatCheck` read `ShaderUtil.GetShaderMessages`, which reports only variants already compiled, and no rendered view used a paper wall. The landing check compiles instead: clone-only `Assets/Editor/PoolLook/FrontRoomsPoolSurfaceCompileCheck.cs` calls `ShaderData.Pass.CompileVariant` on the ForwardLit pass for every keyword set of the clone's 245 Surface materials (14 sets), each `shader_feature` alone and three `_FR_PRINT` pairs, with no and with full URP globals, on Metal macOS, Metal iOS and D3D (fragment) plus Metal macOS (vertex): **105 of 105 OK**. Control: the 8067dc67 text under a hidden name, `_FR_PRINT` sets, Metal macOS and iOS: **8 of 8 FAIL**, each "redefinition of 'sub' (line 1087)", so the check sees the fault it is for. Output `W/proj_unlock/Verification/pool_look/surface_compile_check.txt`, log `W/pool_look/logs/surface_compile_check.log`.
- **Still open:** the contracts (§9.6) wait on their owners through 总控; §9.5 build content (~100 MiB of pool textures in Resources in every desktop build until the pool enters the game).
