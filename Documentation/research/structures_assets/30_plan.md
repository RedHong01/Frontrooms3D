# Structures assets · 30 · Plan: what replaces every greybox piece of scene 4, in what order, at what cost

Line 游戏视觉 (`visual`), workflow `structures_assets` step 1 (Survey, Research x3, Plan; a second workflow builds), stage Plan, 2026-11-05 19:03-19:5x (America/Los_Angeles), account G (switch #21), parent session `local_1fc9f09c`.

Red's ask, verbatim (this line's chat, 2026-11-05 17:42:27 and 17:42:55, plain user turns): "我需要你开始对structure scene里面的资产进行全方位的升级和revise，就像是主游戏项目的标准一样" and "升级的逻辑和之前一样，调研现行，再进行revise". The logic as before: surface_detail (Red 11-04 13:4x: "调研3A游戏是如何通过法线贴图等一系列技巧来提升这些模型信息量并且和光线追踪协作的，来优化每一个房间"), research first (Red 10-31: "…research在那个年代的锁是什么样的…").

**Plan only: nothing built, nothing landed.** Built on `10_survey.md` (115 elements, the TODAY baseline), `20_research.md` (Parts A pool construction, B large structural elements, C families at scale, lamps and props; ids as its head explains), the four TODAY sheets (`images/structures_today.jpg`, `images/pool_construction_today.jpg`, `images/structural_today.jpg`, `sheets/families_scale_today.jpg`). **Not done here:** no Unity run and no render; nothing written to main; no Structures file changed; no git; no download; nothing played aloud (rule 24); frame time not measured (rule 25). The machine-readable copy of section 3 is `W/structures_assets/plan.tsv` (one row per survey id; the second workflow reads it).

**Owners (binding, journal PARENT NOTE 17:5x).** Scene 4 and `Structures/*.cs` are co-claimed by 夸张结构调研与设计 (`level_space`) and 关卡设计 (`map`); `FrontRoomsPoolMaterials.cs`, `FrontRoomsPoolStandIns.cs` and `FrontRoomsMapWorld.Pool.cs` are 关卡设计's; `FrontRoomsStructuresWater.cs` is 水体仿真调研's, through 夸张结构调研与设计. This line owns the assets: `Resources/{Surfaces,Rendering,Props}`, `Tools/Blender/frontrooms_kit`, `Tools/lookdev`, `Scripts/Rendering`. So every upgrade is our package (inert by itself) plus a staged diff of their files with TODAY | NEW frames at their stops, which they review and land (or tell us to land). Hands off: SP7's `RunRecede.cs` and `RunEnd*.cs` (Red's #3 pending). Rebase at land: `PoolTerrain.cs` (dome A / B on key L). Fold in: their pending `ShippedShadowFilter` line in `Well.cs`.

## 0. Short answer

1. **Every one of the Survey's 115 rows has a target** (section 3): 72 must (on a district sign's 灰盒占位 list, or below the game's standard), 8 should, 15 may (11 of them SP7's head and ends, which wait on Red's #3), 20 unchanged (the game's own pieces, the signs, the zone looks).
2. **What the player will see change:** soft, real edges everywhere (bullnose and cove tile, caps, nosings, corner beads and chamfers); curved tile walls that break the light tile by tile; pools with real edges, fittings, wet niches that throw light and caustics, and lamps that are P1-A wall boxes (no lamp bodies on the ceilings); a fibreglass flume that is built, bolted and bearing on its tile saddles; falls that are water; stairs with soffits, stringers and handrails; guards built as the period built them; cut floors that show their floor; laylights that are lit grids; O-C in the Office; the far end of SP3's spine lit; and the greybox props replaced by kits (the dock, the computer room, files, training room, panels, cage, shaft, the 24 pool kinds).
3. **One switch, TODAY / NEW,** per district (a scene-4 key, `;` proposed, as `\` is for the surfaces): Red compares in place, and the sheets come from the same switch at the same stops. NEW off reproduces today exactly.
4. **The work:** 20 packages of ours (P1-P20) and 5 staged contracts of theirs (K1 shared, K2 pool, K2m the map's pool, K3 Level 0 and Office, K4 Run!) in 4 build groups for step 2 (9 agents): **G1** the shared foundation with KIT and the hub first, then **G2** the pool family (SP1, SP2 on all four floors, SP6, P), **G3** Level 0 and the Office (SP3, SP4), **G4** Run! (SP5; SP7's end kits built, not wired). One clone (`W/proj_unlock`), one Unity at a time.
5. **Red's picks stay** (section 8): DishDeep 1.5, GZ-C, porcelain white, the dark nosing only, the slide on its tile wall, P1-A + P2-A + caustics, the water versions on K, O-C, L03 at 1.8, R-B and T-A + T-B, S1 C / S2 B / S4 B, the #3 rows, large spirals in the pool only, every structure walkable. Each pick keeps its decision and gains its construction.
6. **Cost (ESTIMATE; section 6):** LOD0 triangles without P from 1.19 M to about 1.83-2.32 M (+53-95 %); the heaviest districts after (SP3 at about 0.40-0.51 M, SP7 at 0.42 M unchanged) stay under a sixth of what the game's own pool route draws (P, 3.12 M at LOD0); the flume falls from 161.5 k to about 55 k; about 60-80 MiB of new maps on desktop (about a quarter on phones), all under `Resources` (they ship in every build until the editor-only follow-up, section 7); the same number of lights and fewer shadowed; GPU about +0.05-0.6 ms in the views that show falls, niches and caustics. Frame time is measured in the combined test.
7. **Five real choices for Red** (section 5), each built as the recommendation plus the alternatives on a key, so nothing waits: the pool edge per district; small tile on tight curves; pickets on public-floor rails; the paper's lot steps and butt joins in tall rooms (built, off by default: it is Red's pick); the game's lamp modes in scene 4.
8. **Risks** (section 7): the files in flux; `Kit.cs` touched by four contracts (K1 first, the rest stacked); kit colliders and the walk gate; the 0d glint finding before any pool structure enters the game; the size of `Resources`; GlassRT parity for the new tile pieces; Red's open Editor importing new kits.
9. **Corrections found on the way:** the Survey's triangles count every LOD level (LOD0 only: 4.31 M, 1.19 M without P); scene 4's water already registers the underwater cubes as niches under key I; `~/FrontRoomsMapWork/integration/structures/BUILD_LOG.md` exists (the baseline line is staged in `W/structures_assets/buildlog_baseline_line.txt`; this stage's append into another line's folder was refused by the permission check, so the parent or the owners append it).

## 1. Rules every package keeps

1. **TODAY / NEW.** `FrontRoomsStructuresAssets.New` (ours, `Scripts/Rendering/Structures`) is read by every new branch of their builder; with it false the scene builds exactly today's greybox (proof per district: one raw PNG pair, 0 px over 1/255, as surface_detail rule 1). A key (`;` proposed; theirs to pick) flips it and rebuilds the current district; the harness sets it before Play for the sheets. Default NEW once a group's contract lands.
2. **Red's picks stay** (section 8). Quality is ours to raise; design choices go to Red (section 5) or to the owners.
3. **Geometry where the reflections look.** The planar water and GlassRT see geometry, not normal maps, decals or anything at queue 2450 or above (`20_research.md` section 0 item 3): edges, coves, nosings, lips, facets, rails and slab sections are geometry; the surface stays in the maps; decals only where neither sees, after tech_art row 10. Nothing shader-drawn as wear (VISUAL_STANDARD 1.1).
4. **The game's asset standard.** Kits from `Tools/Blender/frontrooms_kit` (`build_asset.py`, `kitlib.py` bevels 3-6 mm) with LOD0 / LOD1 / LOD2 and a sidecar (materials from `Resources/Surfaces`, colliders, footprint, placement), in `Assets/Resources/Props/Models/` (the only folder KitImporter v3 imports as kits, with the LOD cross-fade of tech_art row 7); hero LOD0 holds at 0.35 m; LOD1 at about 45 % and a cull below 2.5 % of screen height (`62` §3.1); period 1988-91, no brands. Lamps in `Resources/Props/Lamps/<Family>/{Models,Prefabs,Materials,Textures}` with `_Phone` prefabs and a `FrontRoomsLampKit` family. Materials on `FrontRooms/Surface` with surface_detail's kinds and K-terms, a `_Phone` twin each, WebGL compiled out.
5. **Walkable.** Edges and rails are render-only (colliders only where a kit's sidecar asks for one); after any contract that touches geometry or colliders the clone-only walk test (`FrontRoomsStructuresWalkTest.cs`, copied from `W/level_space/clone_code/proj_levelspace/Assets/Editor/LevelSpaceReview/`) stays fail=0, and the flume's ride is ridden once (Red 11-04: "我需要所有结构都能够正常让玩家通行").
6. **Lamps in the game's logic.** Every new light through `FrontRoomsMapWorld.ShippedShadowFilter(light)` (tech_art row 16); lens and glow on `FrontRoomsLampKit`'s lens drive (flicker, dim, dead, Relay warning, blackout); shadows by kind (laylights, P1-A's indirect boxes and the down-fill bounce none; troffers and strips one in three within 9 m; sodium heads always).
7. **Era lock and blank signs.** Signs inside the fiction stay blank unless 叙事设计 supplies the words (faces 平面视觉); no EXIT in the SP districts; each district's 灰盒占位 line loses an item when that item lands.
8. **Formats.** Ours: convention-v2 packages `W/apply/structures_<name>` (`.sh`, `_bases.txt`, `_expected.txt`, `_keep.txt`, `_payload/`, `_base/`, `.diff`), inert alone. Theirs: `W/apply/structures_<group>_contract/` with `README.md` (VISUAL_STANDARD 2.6), `CONTRACT.diff`, `files/`, `base/SHA1S.txt`, `GATE.txt` (`fr gate --staging`) and the TODAY | NEW frames of their stops.
9. **Numbers on every package and sheet:** triangles LOD0 · LOD1 · LOD2 counted at LOD0 (unlike the Survey, which summed every level), MiB desktop / phone, renderers, lights (on, shadowed), and "frame time not measured" (rule 25).
10. **Sheets.** Shot format (`W/tools/shotframe/shotframe.py`), TODAY first; rows: eye 1.62 at the stops, the phone row, 3/4 at about 3 m, hero 0.35 m on named pieces, the off state for lamp packages, a COST row; a fit file ending ALL LABELS FIT; English only; staged under `sheets/`; Red sees them through SendUserFile (render).

## 2. Packages and contracts

Ours (G = build group of section 4; MiB = new maps, desktop / phone, ESTIMATE):

| Id | Package (`W/apply/`) | G | What | Paths | MiB | Note |
|---|---|---|---|---|---|---|
| P1 | `structures_profiles` | G1 | the profile library `FrontRoomsStructureProfiles` (2-D sections in metres: bullnose, cove, cap, nosing L, coping, three gutter sections, weir lip, niche surround, handrail tube 42 mm, pipe rail, glass shoe, cap rail, channel frame, oak and tile caps, a slab edge per level, pour stop, wall angle, T-bar, I-beam, fascia, storefront channel, curb angle) with sweep helpers, face-weighted normals and the bevel radius by material; the TODAY / NEW switch `FrontRoomsStructuresAssets.New` | `Assets/Scripts/Rendering/Structures/` (new folder in our Scripts/Rendering) | 0 | inert until K1 calls it; its home is to be agreed with the owners (Part B, B.9 Q2) |
| P2 | `structures_tile_trims` | G1 | four new pool tile pieces `Prop_PoolTile_Bullnose`, `_Cove`, `_Gutter` (with the finger grate), `_WeirLip` (+ `_Phone`) on `_FR_MESH_UV _FR_TILE`; the small-tile Field materials at 108 / 75 / 50 / 25 mm with a joint-width knob; the C# side of GlassRT's material table for them | `Resources/Surfaces/Pool`, `Tools/lookdev/gen_pool_tile.py`, `Scripts/Rendering/PoolTile` | 9.3 / 2.3 | the native hit shader's `_FR_TILE` stays the open item `surface_rt_parity` |
| P3 | `structures_trim_sheet` | G1 | `Prop_StructTrim` 2048 x 1024 (sealant, shoe aluminium, pipe paint, cast abrasive nosing, wall angle and T-bar, pour stop, fascia band, carpet bar, fascia cap, steel angle nosing, bullnose block end, curb angle, sawcut) + `_Phone`; `Prop_WireMesh` (alpha-cut 10 ga, 1.5 in diamond); `Prop_Fireproofing` 1024² | `Resources/Surfaces`, `Tools/lookdev` | 13-14 / 6 | = tech_art row 14 and surface_detail package 7, for scene 4 |
| P4 | `structures_rail_kits` | G1 | rail brackets (wall, post top), post base flange, side-mount plate, glass-shoe end caps and clamps, rail returns where not swept | `Resources/Props/Models` | ~1 / 0.3 | the metals exist per level |
| P5 | `structures_stair_kits` | G1 | `Kit_SpiralStair_Steel` (B4-S: centre column, checker-plate pie treads, balusters, helical rail) | `Resources/Props/Models` | ~2 / 0.5 | 10-15 k LOD0, 4 k LOD1 |
| P6 | `structures_laylight` | G1 | `Kit_Laylight_Module` (2 x 2 ft T-bar grid, prismatic panel, cavity), `Kit_LightWell_Round`, `Prop_LaylightDiffuser` (lamp-row emission, per-panel age, specks; driven by the lens drive) | `Resources/Props/Models`, `Resources/Surfaces` | 2.7 / 1.2 | one set, counted once (Parts B and C) |
| P7 | `structures_lamp_common` | G1 | far lamp pools (a `FrontRooms/LampGlow` footprint variant, 2-16 triangle mesh); `ZL_FLUSH` (ZL F5 sealed flush lens) and `ZL_GLOBE` (ZL F7 jelly jar with its cast guard) with their `FrontRoomsLampKit` families; the lamp-state helper for the game's modes (with 关卡设计) | `Resources/Props/Lamps`, `Resources/Rendering`, `Scripts/Rendering` | 1-2 / 0.5 | ShippedShadowFilter on every new light |
| P8 | `structures_surfaces` | G1 | the runtime stand-ins mapped to real materials (appendix A); new only where no main surface fits (`Prop_AccessFloor` laminate; the dock's tread plate if `Prop_SteelBlack` cannot carry it) | `Resources/Surfaces` | 1-3 / 0.5 | most targets exist in main |
| P9 | `structures_shared_kits` | G1 | `Kit_SteelShelving`, `Kit_AccessFloorPanel_600` (+ edge trim, ramp nosing), `Kit_AccessGrate_600`, `Kit_CableWhips` | `Resources/Props/Models` | 1-2 / 0.5 | used by two or more groups |
| P10 | `structures_pool_lamps` | G2 | P1-A `ZL_HIDUP` and P2-A `ZL_NICHE` promoted from `W/zone_lamps/kit/out` the way O-C was (`W/zone_lamps/promote/make_package.py`): the niche with Part A's construction, visible parts only, registering its own light; `ZL_NICHE_S` (6.5 in, steps and weirs); HID modes in the lens drive (dip-out, compressed restrike, auxiliary quartz lamp, colour drift) | `Resources/Props/Lamps/HidUp`, `/Niche`, `Scripts/Rendering` (FrontRoomsLampKit) | 2-3 / 0.8 | 灯具适配's kits, built by us unless they have started (ask 总控 at step 2's start) |
| P11 | `structures_pool_fittings` | G2 | `Kit_PoolLadder`, `Kit_PoolGrabRail`, `Kit_ReturnInlet`, `Kit_MainDrainGrate` (+ Lane frame), `Kit_FloorDrain`, `Kit_GutterGrate_3m` (+ corner, end); stainless wear masks (tea staining at welds and escutcheons, the scale band) | `Resources/Props/Models`, `Resources/Surfaces` | ~3 / 0.8 | the map table's names where it has them |
| P12 | `structures_flume` | G2 | `Kit_Flume_SLA_*`, `_SLB_*`, `_SLC_*` (cell, bend, drop, transition), `Kit_Flume_Flange` (40-60 bolts), `_Tub`, `_ExitLip`, `_SaddlePad`; `Kit_SlideTube` (the map's name); `Prop_Gelcoat_Ivory`, `_TealBand`, `_SLC_Cap`, `_SLA_Milky` (+ `_Phone`) | `Resources/Props/Models`, `Resources/Surfaces` | ~9 / 2.3 | about 30 pieces on the 79 m line |
| P13 | `structures_water_flow` | G2 | a flow variant of `FrontRooms/Water` (flow along the UVs, two phases of `FR_Ripple_*`, a foam mask, the wet face behind), a foam ring, one `flash_check.py` film per fall | `Resources/Rendering/Water`, `Scripts/Rendering/Water` | 1-1.5 / 0.4 | with 水体仿真调研 (their K versions stay) |
| P14 | `structures_glass_block` | G2 | `Kit_GlassBlockWall`, `Kit_GlassBlockHalfWall`, `Kit_GlassBlockPaver`, `Kit_ObsWindow`, `Kit_Oculus`; `Prop_GlassBlock` (the mid seam, wavy inner faces, the cavity by interior mapping, light from behind) | `Resources/Props/Models`, `Resources/Surfaces` | 3-4 / 1 | - |
| P15 | `structures_pool_props` | G2 | `Kit_TowelDesk`, `Kit_SignBlank`, `Kit_LockerBank`, `Kit_LockerBench`, `Kit_ShowerSet`, `Kit_GuardChair`, `Kit_RingBuoy`, `Kit_RulesBoard`, `Kit_PaceClock`, `Kit_DepthMark`, `Kit_Soffit`, `Kit_Beam`, `Kit_PipeRail`, `Kit_GateBars`, `Kit_PipeRun`, `Kit_CourtLines`; the slide start `Kit_SlideTrafficLight`, `Kit_DispatcherPost`, `Kit_GuardStool` | `Resources/Props/Models` | 5-8 / 2 | 63 §6.2 and BR §2.2 specs |
| P16 | `structures_l0_props` | G3 | the computer room (system cabinets, tape drive, line printer, CRAC unit, data terminal, dot-matrix printer, EPO station, tape rack, fanfold box, lifted floor tile, halon cylinder, chart recorder), the training room (table row sets, stack chair, projection screen, overhead projector, lectern, AV cart, marker board, easel), the file mezzanine (open shelf file, mobile shelving, lateral file, file cart, records carton, microfiche reader) | `Resources/Props/Models` | 6-10 / 2.5 | 62's specs and room budgets |
| P17 | `structures_office_props` | G3 | `Kit_CubiclePanel_1350`, `_2160`, `Kit_PanelPost_1350`, `_2160`, `Kit_PanelFreestanding_1350`, `_1650`, `_2160`; storefront glazing corners and posts; `Kit_Workstation_Baked` (only if the owners want desks) | `Resources/Props/Models` | 2-3 / 0.8 | new meshes, never scales of the 1.65 m kit |
| P18 | `structures_scale_terms` | G3 | in `FrontRooms/Surface`: the paper's lot step and butt joins at the wrong scale (ask 4; off by default) from a per-wall value; the lay-in ceiling's T-bar as a footprint-filtered line (K7) in tall rooms | `Resources/Rendering/FrontRoomsSurface.shader` (+ GlassRT material table) | 0 | the shared Surface file: built against main, rebased |
| P19 | `structures_dock` | G4 | `Kit_OverheadCoilingDoor`, `Kit_DockLeveler`, `Kit_DockLightArm`, `Kit_DockBumper`, `Kit_DockEdgeGuard`, `Kit_Bollard`, `Kit_PlatformTruck`, `Kit_DumpsterFrontLoad`, `Kit_CCTVCamera`, `Kit_ReceivingDesk`, `Kit_PalletLoad_Wrapped`, `Kit_PalletJack`, `Kit_WheelChock`, `Kit_WallPackHPS` | `Resources/Props/Models`, `Resources/Props/Lamps` | 4-8 / 2 | 63 §6.1 |
| P20 | `structures_run_service` | G4 | `Kit_CagedLadder`, `Kit_RiserPipe_3m` (+ clamps), `Kit_WireCagePartition`, `Kit_HoseCabinet`, `Kit_TimeClockStation`, `Kit_Duct_3m` (+ slope), the services' slope pieces, the closet kits (63 Q1); for SP7, built and not wired: `Kit_TempLightString`, `Kit_ExpansionCollar`, `Kit_CombPlate`, `Kit_TIScrap` | `Resources/Props/Models` | 3-5 / 1.2 | - |

Theirs (staged by us, landed by the owners after review):

| Id | Contract (`W/apply/`) | G | Files and hunks | Owner | Order |
|---|---|---|---|---|---|
| K1 | `structures_shared_contract` | G1 | `FrontRoomsStructuresMesh.cs` (the edge option, the switch, the per-wall value for ask 4); `FrontRoomsStructuresKit.cs` (WallRun reveals; RoundHead and Ring per-tile facets and true circles; Flight soffits, stringers and rails; StepRun; Spiral soffit, cap and rail; Guard and RakingGuard by profile and kit; Ramp curbs and rails; CoreB3; Coping and Basin on the tile pieces; the Plate's coves, sealant joints, slab edges and the Office O-C hook; Lamp() by role); `FrontRoomsStructuresLook.cs` (far pools in Tick, the shadow policy in AddLamp, O-C's glow renderer, the lamp modes); `FrontRoomsStructures.cs` (the TODAY / NEW key); `KitRow.cs`, `Hub.cs` | 夸张结构调研与设计 + 关卡设计 | first; the others stack on it; may be split into a builder half and a lamp half if the owners prefer |
| K2 | `structures_pool_contract` | G2 | `Slide.cs`, `Well.cs` (with their pending ShippedShadowFilter line at ~:660), `PoolTerrain.cs` (rebased on dome A / B at land), the pool hunks of `Kit.cs` (WallBox to P1-A, DownFill bodies out and driven by their box, Underwater to P2-A, the edge systems, weir lips, nappe sheets), `Water.cs` (水体仿真调研's file: niches registered in NEW and ordered by the walker's district, the water light's Recommended state as NEW's default, the flow sheets), the signs' 灰盒占位 lines | 夸张结构调研与设计 (and 水体仿真调研's file through them) | after K1 |
| K2m | `structures_pool_map_contract` | G2 | `FrontRoomsPoolStandIns.cs` (FrontRoomsKitLibrary first, by the table's names), `FrontRoomsPoolMaterials.cs` (the nosing to `Prop_PoolTile_Nosing`, `Caustic()` retired for the landed caustics, the Lit greys to Surfaces), `FrontRoomsMapWorld.Pool.cs` (P1-A / P2-A in the pool cells: the map's pool lamp contract; the recessed gutter; edges) | 关卡设计 | after the pool packages land |
| K3 | `structures_l0_office_contract` | G3 | `Level0.cs`, `Level0Beyond.cs`, `Office.cs` (the kits, the lounge wiring, the loose tiles, V4's up-fill, the panel heights, the blades, the laylight, slab edges), the signs' lines | 夸张结构调研与设计 + 关卡设计 | after K1 |
| K4 | `structures_run_contract` | G4 | `Run.cs` (the dock set, the services selection, the RN-F door kits, frames and magnets, T-B heads, the shaft, the cage, the closet, the hose cabinet, the time clock, the duct, the HPS packs, the floor, face and sawcuts), the signs' lines; SP7's files later, after Red's #3 | 夸张结构调研与设计 + 关卡设计 | after K1 |

Where the packages come from: Part A (P2, P10-P15), Part B (P1, P3-P6, P9), Part C (P7, P8, P10, P16-P20); the overlaps are settled in `20_research.md` section 1 (one niche kit, one laylight set, one trim sheet, one profile library).

## 3. Every element: what replaces it

Columns: **Pri** = must (on the district sign's 灰盒占位 list, or below the game's standard), should (the standard's depth, not on a sign), may (optional, or waiting on Red, the owners or a file in flux), - (no change). **Ours** = packages of section 2. **Theirs** = the contract and its hook. **Cost** = LOD0 triangles (ESTIMATE; k = thousand), maps and GPU where they matter. **Risk · stays** = what can go wrong, and what of Red's or the owners' stays as it is.

### HUB (key 0) · the Level 0 hall

No sign of its own; the hall is the map's Level 0 build.

| Id | Element | Today -> upgrade | Pri | Ours | Theirs | Cost | Risk · stays |
|---|---|---|---|---|---|---|---|
| H01 | hall shell | knife-edged boxes; gains the builder edge option (exposed edges at r 2-3 mm, as corner bead on board); base and cove as the map's Level 0 builds them | may | P1 | K1 Mesh.cs edge option | +0.1 k | none; L0 surfaces as the map's |
| H02 | 40 L03 troffers | unchanged; far lamp pools; the game's lamp modes if Red says yes (ask 5) | may | P7 | K1 Look.cs | +0.1-0.6 k; CPU only | a flicker in the hub (key off); L03 at lens 1.8 |
| H03 | pads, placards | none: outside the fiction | - | - | - | 0 | 平面视觉's sign E (VL509) |

### SP1 (key 1) · the slide on its tile wall

Sign 灰盒占位: 滑道（没有真实的玻璃钢和螺栓）、拱墙细部、起滑灯、监视器、规则牌、水和焦散.

| Id | Element | Today -> upgrade | Pri | Ours | Theirs | Cost | Risk · stays |
|---|---|---|---|---|---|---|---|
| S1-01 | pool hall shell 8.7 m | knife arrises and folds -> bullnose r 8 mm on every exposed arris, cove base r 25 mm round the hall, sealant joints on the 3 m cell where wet, the shaft hole cut on the true circle with a bullnose reveal | must | P1, P2, P3 | K1 Mesh edge option; Plate coves and joints; SlabRoundHole | +30-62 k (bevels 24-48, coves and joints 3-7, tile edges 4-7) | render-only edges, colliders unchanged (walk gate); DishDeep 1.5, GZ-C, porcelain white |
| S1-02 | upper floor: start room, stool, CRT, rules board | stand-in cubes -> Kit_GuardStool, Kit_RulesBoard (blank face, world-text slot), Kit_CRTMonitor (main) | must | P15 | K2 Slide.cs | +4-6 k | the board stays blank (era lock, 叙事设计) |
| S1-03 | tower: ring walls, spiral, landings | Lit nosing -> the family's Nosing L, Tread, Riser; ring walls one facet per tile column (75 / 63 round); the door head on the true circle with bullnose; helicoid soffit; inside helical rail on stainless brackets | must | P1, P2, P4 | K1 Ring, Spiral | +4-8 k (a rail 1.9 k a turn) | rail without collider; the tower as high as possible (Red 10-31 17:15); dark nosing only |
| S1-04 | core B3 | sloped soffit and cheeks under every flight, the Nosing L, rails both sides with 12 in extensions, landing edges | must | P1, P2, P4 | K1 Flight, CoreB3 | +2-4 k | - |
| S1-05 | crossing room, ankle sheet | none here: the water line's K versions; the ankle read in white rooms stays visual.md item 3b | - | - | - | 0 | Red picks the water by walking |
| S1-06 | service void lamp | cube -> Kit_WrapFixture_4ft (main) by FrontRoomsLampKit Family.Wrap | must | - | K2 Slide.cs | +0.4 k | - |
| S1-07 | support A (the tile wall under the flume) | arches per course (150 mm at R 1.2; 108 mm at the 1.70 bend bays) with bullnose on both edges of every intrados; copings -> Cap on a corbel course with bullnose under its nose; the saddle a cap course coved into the flume's bearing pad | must | P1, P2 | K1 RoundHead, WallRun reveals; K2 Slide.cs supports | +3-6 k | no gap under the flume (Red's door lesson); Red's A "让瓷砖作为墙壁去托举滑梯", never industrial |
| S1-08 | catch pool | roll-out edge (ask 1): tiled lip, porcelain grate 0.20 m, 50 mm curb in Coping trim, pool cove r 50; 3 P2-A niches registered; main drain in a Lane frame; 4-6 inlets; gutter drains every 3 m; steps with the Nosing L | must | P2, P10, P11 | K2 Basin, Coping, Underwater, Slide.cs | +8-12 k | flash_check with niches on (0d); no ladder (Red's composition) |
| S1-09 | court terraces and falls | flat glowing quads -> square porcelain weir lips with a drip groove, nappe sheets on the flow water landing ~0.23 m out, a foam line | must | P2, P13 | K2 Slide.cs court; 水体仿真调研's K versions | +1-2 k; GPU +0.05-0.2 ms in view | flash_check per fall |
| S1-10 | floating office ceiling | the lid's knife edge -> T-bar edge and wall angle from the trim sheet; the lid's section | should | P3 | K1 slab-edge profile | +0.5-2 k | L0_Ceiling and L03 kept |
| S1-11 | start tub and start light | glow cubes -> Kit_SlideTrafficLight (incandescent lenses: red, green, dark), Kit_DispatcherPost (may) | must | P15 | K2 Slide.cs | +1.5-3 k | its own states, never a lamp-mode flicker; the F key |
| S1-12 | the flume SL-A / SL-B / SL-C | zero-thickness two-sided sweep in 7 runtime materials -> Kit_Flume_* on the riding line: walls with thickness, flange rings with 40-60 bolts and a sealant line, the tub, the exit lip, a 10 mm bearing pad on every saddle; four gelcoat Surfaces (waxed gloss with orange peel, the riding track, print-through at grazing light, crazing at bolt holes, scale line); SL-A milky | must | P12 | K2 Slide.cs (kits along the line; the ride's collider path unchanged) | 161.5 k -> ~55 k (LOD1 20 k); ~9 MiB; ~10 renderers | ride test + walk gate; mixed styles, colours, the riding line, the player rides, the Relay never |
| S1-13 | flume film, lip foam | runtime "water Level" -> a 2-3 cm flow film along the ride, foam and spill at the tub lip and the exit lip | must | P13 | K2 Slide.cs; 水体仿真调研 | +0.5-1 k | flash_check; the F key |
| S1-14 | lamps: 10 wall boxes, 16 down-fills, 3 underwater, 3 tower, 3 up-fill, 12 night line | wall boxes -> P1-A; down-fill bodies removed, their light follows their box; underwater -> P2-A registered; tower -> ZL F5 flush lens; up-fill and night line kept | must | P7, P10 | K2 Kit.WallBox, DownFill, Underwater | +10.5-14.7 k; -32 renderers | light layers are off: niches light what is in range, as today; "全部按推荐" P1-A + P2-A + caustics |
| S1-15 | pool zone look, probe | none | - | - | - | 0 | Q on (share 3, night 0.7) |
| S1-16 | Relay stand-in, sign, marks | outside the fiction; the sign's 灰盒占位 line shortened as items land | - | - | K2 Signs text | 0 | the sign at 2.5 m |

### SP2 (key 2) · the well, four floors over the pool

Sign 灰盒占位: 四层的栏杆、桥、楼梯细部、玻璃砖地灯、大圆楼梯顶上的天窗、流水和泳池水. The whole district is one group (G2), so Well.cs gets one diff.

| Id | Element | Today -> upgrade | Pri | Ours | Theirs | Cost | Risk · stays |
|---|---|---|---|---|---|---|---|
| S2-01 | F0 pool hall: basin, arcade, cascade wall | as S1-01 and S1-08: bullnose, coves, Cap on tile guards, Nosing L; roll-out basin (ask 1); arcade arches per course with bullnose reveals | must | P1, P2, P11 | K1; K2 Well.cs F0 | +5-10 k | walk gate |
| S2-02 | B4-L spiral and its laylight | sector steps -> helicoid soffit, ribbon Cap with bullnosed ends, inside helical rail, Nosing L, ribbons in 150 mm columns; the skylight -> the laylight kit (frame, lamp-row diffuser, lamp logic) | must | P1, P2, P4, P6 | K1 Spiral; K2 Well.cs | +7-12 k | large spirals pool-only, player-only; a lit "skylight" on a B level stays only if level_space keeps it |
| S2-03 | gallery +4.5: glass and tile guards | pane prism on a box band -> glass in an aluminium shoe under a cap rail (1988 UBC), polished edges, 6 mm gaps; tile guards with Cap | must | P1, P3, P4 | K1 Guard | +1-2 k | shoe and cap traced by GlassRT, the pane is Glass; level_space's styles stay |
| S2-04 | core B3 through five levels | soffits and stringers, rails both sides, nosing per storey (pool: Nosing tile; others: Stair_* + abrasive strip); 17 core lamps -> Kit_WrapFixture_4ft | must | P1, P2, P3, P4 | K1 CoreB3; K2 Well.cs | +4-8 k | the Relay's route unchanged |
| S2-05 | F1 Run! service floor | imitation -> the game's Run! B kits: bumper rail, corner guard, cove, sprinkler main and heads, tray (EMT optional) | must | - | K2 Well.cs F1 | +20-40 k as a selection (the full FrontRoomsRunDress ~21 k LOD0 a 3 m cell) | budget: EMT is the heavy part |
| S2-06 | F2 Office floor | L03 cool -> O-C (ZL_PEND) where ForCell says Pendant | must | - | K1 Plate.Office hook (shared with SP4) | ~+13 k; +1 draw a lamp | pendants turn along the void edge; "办公室选择O-C" |
| S2-07 | bridge | slab-edge profiles both sides, real guards | must | P1, P3, P4 | K1 | +1-2 k | walk gate |
| S2-08 | F3 and the glass-block floor light E7 | runtime emissive -> Kit_GlassBlockPaver: prism pavers in a frame, lit by F0 from below | must | P14 | K2 Well.cs F3 | +2-10 k (LOD1 one panel) | lit from below, never a glowing floor (Red: walls never glow) |
| S2-09 | rims: the cut floors | flat boxes -> a slab edge per level (F0 tile with a bullnose coping, F1 block and a steel angle, F2 gypsum fascia and a glass shoe, F3 papered fascia), beams at the cell lines in fireproofing, the plenum with the game's services, the T-bar edge | must | P1, P3 | K1 Plate slab edges; K2 | +3-8 k; fireproofing 4 MiB (shared) | the well reads as four levels from the pool deck |
| S2-10 | cascade C5 | one transparent sheet -> a clinging film down the tile (REC; the free sheet on a key): rivulets, a bright line at each joint, the wet face, a splash trough with a grate | must | P13, P2 | K2 Well.cs; 水体仿真调研 | +0.5 k; GPU +0.2-0.5 ms filling the view | flash_check |
| S2-11 | F0 lamps: 12 wall boxes, 9 down-fills, 8 basin, 8 step, 6 up | wall boxes -> P1-A; down-fill bodies out; basin lamps -> P2-A registered; step lights -> small niches; uplights -> P1-A tall lens | must | P7, P10 | K2 | +25-35 k | 16 niches a frame (registration order) |
| S2-12 | F1 R-B strips | none; fold in ShippedShadowFilter(light) after the AddComponent at ~Well.cs:660 (their pending line) | - | - | K2 | 0 | R-B |
| S2-13 | zone volumes | none | - | - | - | 0 | - |
| S2-14 | sign, marks, Relay stand-in | outside the fiction; sign text | - | - | K2 Signs text | 0 | - |

### SP3 (key 3) · the Level 0 field

Sign 灰盒占位: 楼下那一层、柜子、档案架和家具、栏杆和玻璃栏板.

| Id | Element | Today -> upgrade | Pri | Ours | Theirs | Cost | Risk · stays |
|---|---|---|---|---|---|---|---|
| S3-01 | the field: 12 openings, arches | far lamp pools on the spine (the measured 16-20 m band); the game's lamp modes (ask 5); arch casings from the trim sheet | should | P7, P3 | K1 Look.Tick pools; Level0.cs view lamps | +0.6-4.7 k; <0.1 ms | a pool lights nothing standing in it; the map's Level 0 build, L03 1.8 |
| S3-02 | missing ceiling tiles | dark box -> exposed T-bar on its wall angle, cut panel edges, L0_CeilingTile_Loose (main) hanging, Kit_CableWhips | should | P3, P9 | K3 Level0.cs | +2-3 k | - |
| S3-03 | V1 cut | runtime "plenum lining" -> the Level 0 slab edge: carpet bar, slab 0.20, plenum with the game's services, the grid cut back on a wall angle, papered fascia | must | P1, P3 | K1 Plate; K3 | +1-3 k | - |
| S3-04 | V1 patch below (desks) | none (kits already) | - | - | - | 0 | - |
| S3-05 | V2 drop: flight, guard, step run, sloped rail | sawtooth -> soffit and stringers; rails both sides with returns; guards real: pipe rail on flanged posts with pickets (ask 3) or the two-line rail; glass in a shoe; the stage ledge's carpet bar | must | P1, P3, P4 | K1 Flight, Guard | +5-10 k | walk gate; Rail style where level_space put it |
| S3-06 | V4 tall grid 14.6 m | 1-2 shadowless up-fill lights; the lay-in T-bar as a footprint-filtered line past ~1 cm a pixel; paper lot step and butt joins (ask 4, off until Red) | should | P18 | K3 Level0.cs up-fill; K1 Mesh.cs per-wall value | 10-20 ALU tall rooms; 1-2 lights | the paper is Red's pick; "这里用当前现有的" (L03 in tall halls) |
| S3-07 | computer room | 3 grey cubes -> cabinets (CPU, disk, comms, PDU, disk pack), tape drives, line printer, CRAC, console, EPO station, tape rack, clutter; 600 mm raised-floor panels with edge trim and ramp nosing (no wallpaper on the riser) | must | P16, P9 | K3 Level0.cs | +45-55 k (room budget 60 k) | kit colliders -> walk gate |
| S3-08 | split suite: flight, rings, spiral | sector spiral -> Kit_SpiralStair_Steel (B4-S); soffit under the flight; guards | must | P5, P4 | K1; K3 | +10-15 k | stays a one-cell spiral (Red: no large spirals outside the pool) |
| S3-09 | training room | 2 cubes -> table row sets, stack chairs, projection screen, overhead projector, lectern, AV cart with Kit_CRTTV, marker board, easel | must | P16 | K3 Level0Beyond.cs | +40-55 k (rows baked, back rows LOD1) | 62 Q3 (tiers) is theirs |
| S3-10 | F7 islands, half-wall guards | caps real (rounded, end returns); corner edges | should | P1, P3 | K1 Guard HalfWall | +1-2 k | - |
| S3-11 | sunken lounge | empty -> the game's seating: Kit_Sofa3_Charcoal, Kit_ClubChair, Kit_SideTableTurned, Kit_SmokingStand, lamps, ashtray | must | - | K3 Level0Beyond.cs | +15-25 k | 62 Q4 (the smoking event) is theirs |
| S3-12 | file mezzanine | stand-in rows -> open steel file shelving, mobile shelving on rails, lateral files, cart, cartons, microfiche reader; the gallery's rails and slab edge | must | P16, P9 | K3 Level0Beyond.cs | +30-45 k | blind aisles on purpose (62 §0); 2.13 m shelving under a 2.5 m gallery |
| S3-13 | F9 bowl, signs, Relay stand-ins | outside the fiction | - | - | - | 0 | - |

### SP4 (key 4) · the Office stack and pit

Sign 灰盒占位: 只有隔板，没有桌椅；1.35 和 2.16 米的隔板和三楼的独立隔板是同色的灰盒板；玻璃、采光天花、楼梯都是灰盒.

| Id | Element | Today -> upgrade | Pri | Ours | Theirs | Cost | Risk · stays |
|---|---|---|---|---|---|---|---|
| S4-01 | F0 atrium floor | L03 cool -> O-C under 2.9 m lids; L03 cool kept under tall lids (the game's own rule) | must | - | K1 Plate.Office hook; K3 | +31.6 k for the 85 lamps; +85 draws; +1.67 MiB once | pendants clear the fascia; "办公室选择O-C" |
| S4-02 | F1 gallery, boardroom glass, panel row | glass walls as guard prisms -> storefront glazing (aluminium head, sill and jamb channels, mullions every 1.2 m, gaskets, Glass_Window panes); atrium guards glass in a shoe under a cap rail | must | P1, P17, P4 | K3 Office.cs | +3-6 k | the boardroom stays empty (70) |
| S4-03 | F2 north band, east field | slab edges -> the Office fascia (1.0 m Office_Wall fascia with cap and reveal, the guard in a shoe) | should | P1, P3 | K1 Plate | +1-3 k | - |
| S4-04 | pit and four panel bays | greybox prisms at 1.35 / 2.16 m -> new panel meshes at those heights (the family's fabric, frame and cap, never a scale of the 1.65 m kit) and posts; desks only if the owners want them (a baked workstation kit, or LOD1 past 6 m) | must | P17 | K3 Office.cs | +30-40 k (80 panels); desks +60-90 k baked, +150-200 k with the game's kits | 62's 120 k Office room budget if desks come; S2 B T-caps on the arm ends (Red 11-04) |
| S4-05 | raised floor, creeping slope B9 | 600 mm panels with an edge nosing; the kerb with a steel handrail (Office.cs:297 plans it) | should | P9, P1, P4 | K3 Office.cs | +2-4 k | walk gate (kerb) |
| S4-06 | boardroom tiers | none: empty by design | - | - | - | 0 | 70: it stays empty |
| S4-07 | E5b blade field | greybox blades -> freestanding panels with stabiliser feet at 1.35 / 1.65 / 2.16 m | must | P17 | K3 Office.cs | in S4-04 | - |
| S4-08 | stack faces, the 11.9 m laylight | glow lens and Lit T-bars -> the laylight kit (T-bar grid on the module, prismatic diffusers with lamp rows, per-panel age, dead rows), its 12 lights without shadows; fascias per level | must | P6, P3 | K1 shadow policy; K3 | +2-3 k; 2.7 MiB (shared); shadowed lights 2 -> 0 | the laylight's place and size |
| S4-09 | slot stair hall | box flights -> steel pan stairs (channel stringers, closed risers, abrasive nosings, sloped soffit), rails both sides; landing lamps -> wall wraps or O-C under 2.9 m soffits | must | P1, P3, P4 | K1 Flight; K3 | +3-6 k | walk gate |
| S4-10 | B3 core (second way) | as S4-09; core lamps -> Kit_WrapFixture_4ft | must | P1, P3, P4 | K1; K3 | +2-4 k | - |
| S4-11 | zone volume, sign, Relay stand-in | none; sign text | - | - | K3 Signs text | 0 | - |

### SP5 (key 5) · Run!: the ramp to the dock

Sign 灰盒占位: 铁丝网储物笼；竖井里的管线和爬梯；卸货间的混凝土地面和平台边；门外的坡道和钠灯；卷帘门、卸货调节板、链条护栏、风管、消防水带箱、打卡钟.

| Id | Element | Today -> upgrade | Pri | Ours | Theirs | Cost | Risk · stays |
|---|---|---|---|---|---|---|---|
| S5-01 | spine: head, three 1:8 runs, landings | rails both sides and 50 mm curbs as the period's ramp rules ask (the owners' call: it changes the corridor's read) | may | P1, P4 | K4 Run.cs Ramp | +3-6 k | the bumper rail and VCT stay |
| S5-02 | walls: block, bumper, cove | quads -> Kit_BumperRail_1200, Kit_CornerGuard_1200 (main); sloped bumper pieces if needed | must | P20 | K4 Run.cs | in S5-03's selection | two-tone block |
| S5-03 | services on the slope | tilted boxes -> Kit_SprinklerMain_3m and heads, Kit_CableTray_3m, Kit_ConduitEMT, Kit_PipeHanger_Band (main) with sloped pieces; Kit_Duct_3m with a slope piece | must | P20 | K4 Run.cs | +45-72 k as a selection (the full dress ~21 k a 3 m cell) | budget (EMT) |
| S5-04 | fire-door walls FD1 / FD2 | boxes -> Kit_RunCompartmentFrame, Kit_DoorLeaf_SteelRun_DA, Kit_DoorCloser_*, Kit_RunMagnetHolder (main) | must | - | K4 Run.cs | +3-6 k | the doors open at the trip (71) |
| S5-05 | riser shaft D7 | boxes and a chain-link stand-in -> Kit_CagedLadder (bottom rung 3.5 m up), Kit_RiserPipe_3m with clamps, the tray riser, beams as I-profiles, block control joints | must | P20, P1, P3 | K4 Run.cs | +4-6 k | view-only |
| S5-06 | janitor closet and the E8 cage | mesh guard -> Kit_WireCagePartition (panels, a door with a hasp for Kit_Padlock_*, posts) in Prop_WireMesh; the closet lamp -> ZL F7 jelly jar; closet kits if it stays enterable (63 Q1) | must | P20, P7, P3 | K4 Run.cs | +3-5 k (+0-10 k closet) | a body-only collider so sight crosses |
| S5-07 | goal door RN-F, F5 lite, C42 | boxes -> Kit_DoorLeaf_SteelRun_DA, Kit_DoorLite_Exit, Kit_ExitDevice_Crossbar, Kit_DoorFrame_Steel (main) | must | - | K4 Run.cs | +3-5 k | never locked; the lite a window back (71) |
| S5-08 | the dock | 16 boxes and runtime "sealed concrete" -> coiling door, leveler, light arm, 2 bumpers, edge guard with chain and gate, 4 bollards, platform truck, dumpster, CCTV camera, receiving desk, pallets, pallet jack, steel shelving, wheel chocks; the floor Stair_Fill with 3 m sawcuts; a steel curb angle on the dock face; a painted door number | must | P19, P9, P1, P3 | K4 Run.cs | +33-40 k | kit colliders -> walk gate; S4 B dock strips x1.0, the sodium slot the brightest |
| S5-09 | outside ramp, two sodium heads | cubes -> Kit_WallPackHPS x2 (prismatic refractor, die-cast bronze housing, mustShadow kept); the retaining wall in Run!'s block with control joints (no new concrete family); a broom-finished ramp with sawcuts | must | P19, P3 | K4 Run.cs | +1.5-3 k | sodium 240 (S4 B) |
| S5-10 | armed lamps: R-B strips, dock light | none (the dock light's body is S5-08's arm) | - | - | - | 0 | R-B |
| S5-11 | tripped heads T-A, T-B | 7 grey T-B boxes -> ZL_RPLATE + Kit_EmergencyTwinHead_Yoke + _Lamp (main), as SP7 | must | - | K4 Run.cs | +45 k (6.5 k a unit) | "这里应该是TA和TB相结合" |
| S5-12 | hose cabinet, time clock, ducts | not built -> Kit_HoseCabinet, Kit_TimeClockStation (card faces 平面视觉, strings 叙事设计), Kit_Duct_3m | must | P20 | K4 Run.cs | +8-15 k | world-text slots on the cards |
| S5-13 | zone volume, trip key, sign, Relay | none; sign text | - | - | K4 Signs text | 0 | the trip key T |

### SP6 (key 6) · the deep pool terrain and the large stairs

Sign 灰盒占位: 全部地形块、拱、穹顶和圆洞上方的灯井、楼梯和栏板细部、检修地板、水（没有焦散）; B 的小灯、灯臂和圆洞的灯罩.

| Id | Element | Today -> upgrade | Pri | Ours | Theirs | Cost | Risk · stays |
|---|---|---|---|---|---|---|---|
| S6-01 | corridor (dry, 2.9 m) | bullnose and cove base; the arch (R 0.75) in 2 in or 108 mm courses with a bullnose reveal (ask 2); one ZL F5 flush lens per cell | must | P1, P2, P7 | K1; K2 PoolTerrain.cs | +1-2 k | - |
| S6-02 | BeachHall 5.4 m | Deck tile on the beach (waterline landed), the apse in 150 mm columns, the D2 arcade per course with bullnose, Cap on the cheek, Nosing L on step runs, P1-A | must | P1, P2 | K1; K2 | +4-8 k | the 1:12 beach (LS135) |
| S6-03 | IslandChannel | bullnose copings at the water, Cap on the bridge and island parapets; the stepping drums (R 0.45) in 2 in tile with a bullnose cap, tops 3 cm proud | must | P1, P2 | K1; K2 | +2-4 k | walk gate on the drums |
| S6-04 | C9 wet block and its globe | cube -> ZL F7 jelly jar; one niche in the plunge | must | P7, P10 | K2 | +1.5-3 k | - |
| S6-05 | WeirTerraces and the falls | flat quads -> three square porcelain weir lips with a drip groove, free curtains on the flow water with a foam line, Nosing L on the step runs, the basin under W3 as rim-flow (ask 1) | must | P2, P13, P11 | K2; 水体仿真调研 | +3-6 k; GPU +0.05-0.2 ms a fall in view | flash_check per fall |
| S6-06 | DomeGallery deck -1.80 | Nosing, Tread and Riser on the C7 stair into the water (waterline under a nosing), bullnose, coves, C1's niches | must | P1, P2, P10 | K1; K2 | +3-6 k | C7 has no rail by design |
| S6-07 | B4-M spiral | helicoid soffit, ribbon Cap, inside rail, Nosing L; the inner ribbon (R 0.8) in 108 mm or 2 in tile (ask 2) | must | P1, P2, P4 | K1 Spiral; K2 | +3-5 k | pool-only, player-only |
| S6-08 | D4 saucer dome and oculus | 12 x 64 quads -> 45 rings of whole tiles (~6,050 tiles, ~12 k triangles); the oculus shaft in 2 in tile with a bullnose curb and a cap ring | must | P1, P2 | K2 BuildDome | +10.5 k | dome A / B (key L) waits on Red: build for both, rebase at land |
| S6-09 | dome hall light B | B's small spots, arm and oculus shade stay as they are until Red picks A or B; then B's kits (the ZL_MHDL family, an arm, a shade) | may | (P7) | K2 | +2-5 k if B stays | A or B is Red's |
| S6-10 | Grotto | the vault (R 3.9) in 150 mm courses, one facet a course; four niches at 3 m; the E6 niche with bullnose reveals | must | P1, P2, P10 | K2 | +3-6 k | re-check the spring line (fixed 11-04 21:47) |
| S6-11 | Casbah F6 | every block's arrises bullnosed, Cap on parapets, the flooded floor's six niches | must | P1, P2, P10 | K1; K2 | +5-9 k | - |
| S6-12 | C8 covered basin, access floor | runtime "access floor panel (greybox)" -> Kit_AccessGrate_600 (moulded grating on pedestals over the balance tank), tube rails at the holes; its 3 niches registered so the holes glow and ripple | must | P9, P4, P10 | K2 | +2-4 k | walk gate (grate); Red's access floor stays |
| S6-13 | light wells over the round holes | lamp and lid -> Kit_LightWell_Round (tiled shaft, diffuser, a cavity lamp in the lamp logic) | must | P6 | K2 | +0.5-1 k | - |
| S6-14 | lamps: 21 wall boxes, 15 down-fills, 25 underwater, step, weir, flush, soffit | as SP1: P1-A; down-fill bodies out; P2-A registered and ordered by district; small niches on wet steps and weirs; ZL F5 on dry landings and soffits | must | P10, P7 | K2 | +53-91 k | 12 bodies and 16 niches a frame |
| S6-15 | 13 water bodies; caustics | caustics and thrown light on by default in NEW (the water light's Recommended state, as the title stream ships it), registration ordered | must | (landed API) | K2 Water.cs via 夸张结构调研与设计 | GPU +0.2-0.6 ms in the busiest views (unmeasured) | 0d stands: fix before the pool enters the game; W-D and the K versions stay |
| S6-16 | zone look, probes, sign, Relay | none; sign text | - | - | K2 Signs text | 0 | - |

### KIT (key 8) · every level edge by the plate

Sign 灰盒占位: 护栏、压顶、截面 (the grid lines and gauge are measuring aids and stay). KIT becomes the reference row for every edge, guard and pool edge system.

| Id | Element | Today -> upgrade | Pri | Ours | Theirs | Cost | Risk · stays |
|---|---|---|---|---|---|---|---|
| K-01 | Level 0 hall: plinths, void cell, sections | the reference row of every edge: bevels, the family edge set, a slab section per level, every guard style built real, side by side | must | P1, P3, P4 | K1 KitRow.cs | +12-25 k | - |
| K-02 | tile hall | bullnose, cove, Cap, Nosing L; one bay of each edge system (roll-out, rim-flow, recessed) and one of each fitting; 14 P1-A, down-fill bodies out | must | P2, P10, P11 | K1; K2 | +12-20 k | - |
| K-03 | units, gauge, labels | none: a measuring aid | - | - | - | 0 | - |

### SP7 (key 9) · Run!: the door that backs away, and its five ends

Hands off (journal PARENT NOTE): RunRecede.cs and RunEnd*.cs wait on Red's #3 combination; the corridor is already the game's dress. This plan only builds the ends' kits, unwired.

| Id | Element | Today -> upgrade | Pri | Ours | Theirs | Cost | Risk · stays |
|---|---|---|---|---|---|---|---|
| S7-01 | vestibule | none | - | - | - | 0 | - |
| S7-02 | corridor 1 x 14 (the game's dress) | none: the production dress | - | - | - | 0 | Run! B |
| S7-03 | entrance arch or Run door, tell, NOTICE | the RN-F door kits wait: RunRecede.cs is in flux until Red's #3 | may | - | later (RunRecede.cs) | - | hands off |
| S7-04 | compartment frames and magnets | Kit_RunCompartmentFrame, Kit_RunMagnetHolder (main) wait for RunRecede.cs | may | - | later | - | hands off |
| S7-05 | recede track, slot, trolleys | Kit_RunRecede_Track_3m, _FloorSlot_3m, _TrackStop (main) wait | may | - | later | - | hands off |
| S7-06 | ghost bands | RunB_*_Ghost (main) wait | may | - | later | - | hands off |
| S7-07 | release hall, blind doorway | none | - | - | - | 0 | - |
| S7-08 | the head and its machine | the game's FrontRoomsRunRecede with Kit_RunRecede_* (main, 11-05 13:3x-13:44) waits on Red's #3 combination; a ready list is kept | may | - | later, on our API | - | #3 THE LIGHT LEAVES rows on N |
| S7-09 | T2 blocks | FrontRoomsRunRestack with Kit_RunRestack_* (main) waits | may | - | later | - | hands off |
| S7-10 | lamps and the scene-4 rig copy | the game's FrontRoomsRunRig when RunRecede.cs is free | may | - | later | - | look B, the red tube at its brightest |
| S7-11 | #2 FAR SHORE | build, do not wire: Kit_TempLightString (62), Kit_ExpansionCollar with fire-stop wool, Kit_TIScrap debris | may | P20 | later (RunEndFarShore.cs) | +5-10 k when wired | in flux |
| S7-12 | #3 THE LIGHT LEAVES | waits on Red's combination (72 §9) | may | - | - | - | "我喜欢第三个，以这个去深化" |
| S7-13 | #4 DON'T LOOK BACK | the head wall wiring waits | may | - | later | - | in flux |
| S7-14 | #5 DREAM RUN | build, do not wire: Kit_CombPlate; pallets from Kit_Pallet and Kit_TransTileBox (main) | may | P20 | later (RunEndDreamRun.cs) | +3-6 k when wired | in flux |
| S7-15 | sign, end placard, marks, Relay | none | - | - | - | 0 | - |

### P (key 7) · the pool route: the map's own pool family

Sign 灰盒占位: 所有道具（前台、储物柜、淋浴、梯子、救生椅、标牌、滑梯管、铁门、扶手）、水面、天花板光斑. Through the map contract (关卡设计).

| Id | Element | Today -> upgrade | Pri | Ours | Theirs | Cost | Risk · stays |
|---|---|---|---|---|---|---|---|
| P-01 | pool club and pool level modules | through the map contract: P1-A / P2-A in the pool cells, bullnose and coves on the pool family's geometry, the club basin's recessed gutter with a handhold (ask 1) | must | P2, P10, P11 | K2m FrontRoomsMapWorld.Pool.cs | +25-45 k | the game's own pool route: the map's call; club x1, level x4, dark x3 |
| P-02 | 24 pool stand-in kinds | merged boxes -> 24 kits under the table's names (KitLibrary first): construction kinds in P11, P12, P14; furniture in P15 | must | P11, P12, P14, P15 | K2m FrontRoomsPoolStandIns.cs | +72-125 k on P's 3.12 M LOD0 | kit colliders -> walk gate on the route |
| P-03 | ceiling caustics (cell web) | the runtime cell web -> the landed caustics (FR_Caustic_* loops and the niches' thrown light) | must | (landed) | K2m FrontRoomsPoolMaterials.Caustic retired | 0 draws | photosafety (0d) |
| P-04 | fire stair: iron gate, rails, wall lamps | stand-ins -> Kit_GateBars, pipe rails on brackets, ZL F7 jelly-jar wall lamps on the pool side | must | P15, P4, P7 | K2m | +2-4 k | the game's stairwell stays as it is |

### Every district · the shared rows

The survey's cross-cutting rows; they are G1's foundation.

| Id | Element | Today -> upgrade | Pri | Ours | Theirs | Cost | Risk · stays |
|---|---|---|---|---|---|---|---|
| X-01 | FrontRoomsStructuresMesh (every shell) | the edge option: radius, 1 or 2 segments, an edge mask (top 4 and vertical 4 by default; all 12 on free-standing pieces), face-weighted normals, planar UVs per face; render-only; the TODAY / NEW switch | must | P1 | K1 Mesh.cs | +103-207 k across the scene (8,608 box-equivalents) | vertex cost 0.05-0.2 ms (estimate); colliders unchanged |
| X-02 | guards: five styles | real builds by style and length: pipe rail on flanged posts; pickets in public zones (ask 3); glass in a shoe under a cap rail; framed woven wire; capped half walls; metals by level | must | P1, P3, P4 | K1 Guard, RakingGuard | +46-107 k | woven wire absent from RT glass (cut-out); level_space's style per place |
| X-03 | stairs: Flight, StepRun, Spiral, CoreB3 | soffits, stringers, nosings (pool: the family's L; cores: Stair_* and an abrasive strip), rails both sides with extensions | must | P1, P2, P3, P4, P5 | K1 | +40-77 k | walk gate; dark nosing only, porcelain treads |
| X-04 | Lamp(): the districts' own fixtures | a kit per role (P1-A, P2-A, small niche, ZL F5, ZL F7, wrap, HPS pack, dock arm), ShippedShadowFilter and the lens drive on every new light, HID modes for P1-A, a shadow policy by kind, far pools; lamp modes (ask 5) | must | P7, P10, P19 | K1, K2, K4 | per district | each zone's own lamp |
| X-05 | structures-only runtime materials (86 stand-ins) | each to a real material (appendix A) | must | P8 and each family's package | K1-K4, K2m | +60-80 MiB of maps overall (section 6) | Resources ship in every build (section 7) |
| X-06 | look presets | none (the zone looks stay); the Run! rig copy is S7-10 | - | - | - | 0 | Q on |
| X-07 | tiers | a phone row on every sheet (the iPhone profile on the Mac, quality Medium), _Phone twins of every new surface, LOD1 / LOD2 on every kit; devices, frame time and WebGL in the combined test | should | all | - | - | not a device; RULES 5b: desktop Editor only until Red takes a structure in |

## 4. Build groups for step 2 (the second workflow)

**Common to every group.**
- **Clone:** `W/proj_unlock` only (no new clone). At most one Unity of this line at a time: wait until `pgrep -fl "Unity.*-projectPath /Users/redwang/FrontRoomsVisualWork/proj_"` shows none of ours for 15 s; `-batchmode -buildTarget OSXUniversal`; logs in `W/structures_assets/logs/`; silent (rule 24); productName private. Sync only what a group needs from main (`fr sync visual --line 游戏视觉`), saving clone-only files first in `W/structures_assets/clone_only_save/`.
- **Harness:** the Survey's `Assets/Editor/StructuresAssets/FrontRoomsStructuresAssetSurvey.cs` (clone-only) extended with `SAS_NEW` (the switch), `SAS_PHONE` (`FrontRoomsHandheld.Force` with the iPhone 16 Pro profile, 2622 x 1206, quality Medium, set before Play: the method of `W/level_space/glaze/RUN.txt` step 8), `SAS_HERO` (new 0.35 m and 3 m cameras per named piece), `SAS_OFF` (lamps dead or blackout) and LOD0-only counts; launcher `W/structures_assets/run_today.sh` (`SAS_RUN=<name> SAS_ONLY=<keys>`). The walk test and the shader compile checks (`FrontRoomsPoolSurfaceCompileCheck.cs` and siblings) are restored from `W/structures_assets/clone_only_save/` and `W/level_space/clone_code/` (copies; never landed).
- **Authoring:** Blender kits in `W/structures_assets/kits/` from a copy of main's `Tools/Blender/frontrooms_kit` (generators land with the package); maps from `Tools/lookdev` generators; nothing written under main.
- **Gates (rule 25):** `fr gate --staging` Roslyn macOS + iOS 0 errors with the group's contract applied; a Unity batch shader compile (CompileVariant per keyword set, Metal mac / iOS + D3D, with its known-bad control failing) for any shader change; the walk test fail=0; the NEW-off PNG pair 0 px over 1/255 at one stop per district; `flash_check.py` turns where light moves (falls, caustics, lamp modes; limit 3 flashes a second); package dry run and a scratch `--apply` rehearsal with MANIFEST OK. No other test rounds; one combined test at the stopping point.
- **Outputs:** packages in `W/apply/structures_<name>`, the contract in `W/apply/structures_<group>_contract/`, sheets in the staged `sheets/` (`structures_<group>_<district>.jpg` + fit files), one section per stage in the staged `60_build.md` (a stage log, VISUAL_STANDARD 2.1), a `W/vl_pending/structures_<group>.md` spec, combined-test rows for the parent, and the journal `W/structures_assets/journal.txt` from the first minutes (disk first).
- **Order:** G1 first: its packages land at once (inert), then K1 goes to the owners with KIT and one main view per district. G2, G3 and G4 author in parallel with G1 (Blender and maps need no Unity); their renders queue on the one clone (G2, then G3, then G4); their contracts stack on K1 and are rebased with `W/tools/rebase_apply.sh` when main moves.

| Group | Agents (9 in all) | Packages (ours) | Contract (theirs) | Stops rendered TODAY / NEW (desktop Ultra 1920 x 1080, the Survey's cameras) | Phone row | Hero 0.35 m and 3 m | Off state, flash_check | Depends on |
|---|---|---|---|---|---|---|---|---|
| **G1** shared foundation + KIT + HUB | G1-a profiles, tile trims, trim sheet, surfaces, the K1 diff; G1-b rail, stair and shared kits, the laylight, lamp common | P1-P9 | K1 | KIT s00-s05; HUB s00; every district's s00 plus one main view (SP1 s02, SP2 s08, SP3 s01, SP4 s00, SP5 s03, SP6 s04, P s03) | KIT s00, s03; SP6 s04 | a coping, a nosing, a rail bracket, a glass-shoe end, a slab edge, a sealant joint | lamp modes on (if Red says yes): a 10 s film per district, flash_check | - |
| **G2** pool family: SP1, SP2 (all floors), SP6, P | G2-a pool lamps, niche registration, water flow; G2-b the flume and the slide start; G2-c fittings, edge systems, glass block, the 24 P kinds | P10-P15 | K2, K2m | SP1 s00-s04; SP2 s00-s09; SP6 s00-s10; P s00-s16; KIT s03-s05 | SP1 s02; SP2 s08; SP6 s04, s07; P s03 | a niche, a ladder escutcheon, a flume flange and pad, a weir lip, a gutter grate, a drain frame | a wall box dead (its bay dark), a side of niches out; flash_check at the beach water turn, each fall, the caustics with niches on; the flume ridden once | P1-P3, P7 (G1) |
| **G3** Level 0 + Office: SP3, SP4 | G3-a Level 0 props and the access floor; G3-b Office props, the scale terms, the K3 diff | P16-P18 | K3 | SP3 s00-s14; SP4 s00-s15 | SP3 s00; SP4 s00 | a tape-drive reel window, a panel cap at 1.35 and 2.16 m, a laylight panel, a storefront mullion, a file shelf | laylight rows dead; lamp modes (ask 5) | P1, P3, P6, P7, P9 (G1) |
| **G4** Run!: SP5 (SP7 kits only) | G4-a the dock, HPS packs, the light arm; G4-b services, cage, duct, SP7 kits, the K4 diff | P19-P20 | K4 | SP5 s00-s05; SP7 s00-s06 (TODAY only: unchanged until #3) | SP5 s03 | the coiling door's hood and slats, the leveler lip, the HPS refractor, the cage hasp, the time clock | SP5 tripped (T) | P1, P3, P4, P7, P9 (G1) |

Each group's last agent renders, makes its sheets and packages; the parent lands our packages, sends the contracts and the sheets (SendUserFile, render), and updates the handoff. A group that waits on an answer keeps building the rest.

## 5. Questions for Red (only real design choices)

Quality is ours to raise; these five change what Red designed or picked, so they are Red's. Red's own way in scene 4 is to try every version ("把每一种都做到那个structure scene里我自己尝试", 11-05 ~12:4x, `handoff/level_space.md` §5), so each is built as the recommendation plus the alternatives on a key, and nothing waits for the answer. They go to Red in one message, in Chinese, as below (VISUAL_STANDARD §3 step 6).

| # | Choice | Options | REC | Default while waiting |
|---|---|---|---|---|
| 1 | the pool edge per district | A rim-flow, B roll-out, C recessed gutter, D one everywhere | SP6 A, SP1 and SP2 B, P C; KIT all three | REC built; KIT side by side |
| 2 | tile size on tight curves | A by radius (150 / 108-75 / 50 / 25 mm), B 150 mm everywhere, faceted | A | A, B on a key |
| 3 | public-floor rails (SP3 V1 / V2, SP4) | A the two-line rail made real, B the same with pickets (the period 6 in rule) | B on public floors, A in service zones, glass stays glass | REC, A on a key |
| 4 | the paper at the wrong scale | A as today, B lot steps per wall, C B + butt joins above a roll's length | C | built, OFF (Red's pick) until Red sees the pair |
| 5 | the game's lamp modes in scene 4 | A all steady (today), B the game's tier-1 modes | B (pool fluorescents steady until 0d) | B, A on a key |

**For Red (Chinese, ready to send):**

先说怎么读、怎么回：下面是 5 个需要你决定的设计选择，每个都写了背景、你在场景里会看到什么、代价、推荐和缺点。可以只回“全部按推荐”，也可以逐条回。我们不会等回复：先按推荐做好，其他选项放在键上，你走过场景 4 以后再定。

**问题 1 · 泳池池边做成哪种真实的溢水边（每个区一种）**

背景：真实的 1960-1993 年泳池，池边是一整套构件：溢水槽、格栅、压顶砖、扶手位。现在场景 4 的池边只是一条方砖压顶，直角像刀口，三种真实做法一种都没有。池边的做法会改变“水和地面的关系”，所以这是设计选择，不只是质量。

选项（你在场景里会看到和感到的）：
- A 齐平溢流：水面和地面一样高，水一直满到边上，池边只有一圈白瓷格栅。最安静，最像“梦池”。
- B 翻出式：水面比地面低约 5 cm，池边有一道很矮的瓷砖坎和格栅。像社区泳池，人能直接爬上来。
- C 嵌入式溢水槽：池边比水面高约 30 cm，侧壁里有一道带扶手的槽。像 80 年代的比赛池和俱乐部池，最有“机构感”。
- D 所有池子都用同一种。

- 代价：每 3 米池边约 24 个三角面，每个转角约 100 个；近处格栅是真几何（约每米 200 面），远处换成贴图；新贴图约 5 MB。三种差不多。
- 推荐：SP6 的平地浅池用 A（满水、安静）；SP1 落水池和 SP2 F0 用 B（滑下来的人要能爬出来）；P 的俱乐部用 C（那个年代的商业泳池）。KIT 的瓷砖台子三种并排放，方便你比。
- 缺点：A 近看会在整片白瓷边上多一圈格栅线；C 会改变池边高度，要 夸张结构调研与设计 确认走路不受影响。
- 默认：不用等你回复，我们按推荐做，KIT 里三种并排；你走过以后想改哪个池子，只是一个参数。

**问题 2 · 弯曲的墙用多大的瓷砖**

背景：真瓷砖是平的。贴在柱子、拱、穹顶上，会变成一块一块的小平面，灯的反光会一块砖一块砖地跳，Pools 里的柱子就是这样。现在场景 4 是把 150 mm 方砖的贴图“弯”在曲面上，反光是一条光滑的光带，看起来像 CG。真实的贴砖师傅按最小的弯曲半径选砖：越弯，砖越小。

选项（你在场景里会看到和感到的）：
- A 按真实做法换砖：半径 1.2 m 以上用 150 mm；0.6-1.2 m 用 108 或 75 mm；0.3-0.6 m 用 50 mm；更小用 25 mm 马赛克。细柱子、小拱、踏步石墩和塔里的中柱会是更小的砖，像真的泳池。
- B 全部保持 150 mm，只把曲面切成“一列砖一个平面”。大面积统一，但细柱子（半径 0.25-0.45 m）会明显变成多边形，砖离开曲面 5-11 mm。

- 代价：几乎为零。几何反而更少（只有穹顶多约 1 万个三角面），不需要新贴图。
- 推荐：A。
- 缺点：A 让小构件和大墙面的砖不一样大。这是真实的，但会改变“到处都是同一种砖”的画面。
- 默认：按推荐做 A，留一个键切到 B 对比。

**问题 3 · Level 0 和办公层开放楼板边的护栏，要不要加竖条**

背景：SP3 的 V1、V2 和 SP4 的一些开放楼板边，现在是方钢条做的两道横杆，这是工业做法。1990 年的规范里，公共区域的护栏要挡得住一个 6 英寸的球：要么加竖条，要么是玻璃加扶手。两道横杆只用在机房、仓库这种服务区。你说过滑梯支架“看起来非常的工业”不喜欢，所以这里是一个风格选择。护栏放在哪里、用哪种样式，仍然是 夸张结构调研与设计 的设计。

选项（你在场景里会看到和感到的）：
- A 保持两道横杆，只把它做真：42 mm 钢管、法兰底座、弯头收口。开放、空、有点工业。
- B 同样的立柱和扶手，中间加竖条（约 14 cm 一根）。远看还是线，近看是办公楼里真实的护栏，边缘更密、更安全。

- 代价：B 每 1.2 m 约 250 个三角面（A 约 150-200，今天约 60）；材质都已经有。
- 推荐：公共楼层用 B（SP3 的 V1、V2 和 SP4）；机房和服务区保持 A。现在是玻璃栏板的地方保持玻璃，只把它做真（铝槽底座加顶扶手）。
- 缺点：B 往下看会多一层竖线，视线稍微被挡。
- 默认：按推荐做，每处留一个键切回 A。

**问题 4 · 你选的墙纸在超高、超长的墙上，要不要有“批次色差”和“横向接缝”**

背景：你选的 C+2 墙纸在游戏里 2.9 m 高的房间是对的。但在场景 4 的 V4（14.6 m 高）、39 m 长的主走廊和 SP4 的 11.9 m 墙上，同一卷纸被无限复制，看起来像贴图。真实的墙纸：不同生产批次颜色有一点差别，工人一面墙用一个批次，一般在墙角换；墙比一卷纸还高的时候，每一卷会有一道横向接缝，图案会错开一点点。

选项（你在场景里会看到和感到的）：
- A 保持现在：所有墙一模一样。
- B 加批次色差：每面墙差 1.5-2.5%，墙角换批次。
- C B 再加横向接缝：只在比一卷纸更高的墙上出现（V4 会有两道）。游戏里的房间完全不变。

- 代价：每个像素 4-18 次运算，没有新贴图，每个顶点多 4 个字节。
- 推荐：C。
- 缺点：这是第一次改你选过的墙纸外观（只在场景 4 的超高墙上）。
- 默认：因为是你选过的东西，我们做出来但默认关，放在一个键上；先给你 V4 和主走廊的 TODAY | NEW 对比图，你决定开不开。

**问题 5 · 场景 4 的灯，要不要像游戏里一样会坏**

背景：游戏里每盏灯都有自己的状态（第一级：62% 正常，20% 偶尔闪，10% 快坏，5% 灭，3% 偏暗）。场景 4 的 1,019 盏灯全部常亮。在 SP3 的 422 盏灯、SP4 的三层办公和 60 m 长的 Run! 灯带里，每盏灯都一模一样亮，这是“复制感”的主要来源之一；也意味着你在场景 4 看到的，不是这个结构进游戏以后的样子。

选项（你在场景里会看到和感到的）：
- A 保持全部常亮（今天）。
- B 默认用游戏里的灯状态和概率，留一个键切回全部常亮。

- 代价：只有 CPU（约 1,000 盏灯，每秒 10 次，估计不到 0.2 ms），不加灯。
- 推荐：B。泳池区的荧光灯在“水面反光闪烁”的安全问题（0d）解决之前保持常亮。
- 缺点：坏灯会让一些角落变暗；你要看结构本身时，可以切回常亮。
- 默认：按推荐做 B，用键切换。

**只是告诉你，不需要你决定：**

- 泳池天花上的 49 个“下照灯方块”会去掉灯体：光保留，跟着墙上的灯箱一起亮灭（P1-A 的“抬头看不到任何灯泡”）。
- SP2 的瀑布：贴墙流下（推荐）和从槽口飞出两种都做，用键切换，你走过以后选。
- 场景 4 的池子不加爬梯（你的构图是从台阶走出水）；P 的俱乐部按那个年代加爬梯。
- 深度标记：叙事设计给出数字之前不放（空框会像缺了东西）。
- 不加新的“清水混凝土”材质：卸货区、竖井和水箱继续用 Run! 自己的砌块和混凝土（同一个关卡用它自己的材质）。
- 穹顶保持瓷砖壳，不改成肋拱；A / B 仍然等你走过以后选。SP7 的结尾仍然等你选 #3 的组合，这次只把模型做好，不接上。
- SP4 坑底要不要放桌椅，由 夸张结构调研与设计 决定（他们是故意不放的，用来做视线测试）。
- 新的模型和贴图放在 Resources 里，会跟着每一个构建一起打包（新贴图约 60-80 MB）。这是我们已有的老问题（泳池贴图约 100 MB 也是这样），交给 总控 和 构建 决定怎么处理。

**计划摘要：**

1. 调研做完了：三份研究（泳池构造、大型结构、超大尺度下的关卡家族、灯和道具）合成了一份，每一条都有真实来源和 3A 的做法。
2. 结论：场景 4 的表面已经是游戏的标准（瓷砖、墙纸、地毯都是游戏自己的）；差的是“构造”：刀口一样的边角、没有底板和扶手的楼梯、方钢栏杆、弯曲墙上被弯过去的贴图、方块灯、灰盒道具。
3. 115 个元素都有了目标：72 个必须改（标牌上写着“灰盒占位”的，和不符合游戏标准的），8 个应该改，15 个可选或在等决定（其中 11 个是 SP7，等你选 #3），20 个不用动。
4. 第二个工作流分 4 组：① 公共基础（圆角砖、阴角砖、压顶、踏步收边、护栏、楼梯底板、楼板截面、通用灯具）和 KIT / HUB 参考排；② 泳池（SP1、SP2、SP6、P）：P1-A 灯箱、P2-A 水下灯（自带焦散和回光）、溢水边、爬梯和排水口、玻璃钢滑梯套件、真实的流水；③ Level 0 和办公（SP3、SP4）：机房、培训室、档案架、1.35 / 2.16 m 隔板、采光天花、O-C 吊灯、补上主走廊远处的暗带；④ Run!（SP5）：卸货平台全套、竖井爬梯和管线、储物笼、钠灯壁灯；SP7 只做模型、不接线。
5. 每个区都会有一个“新 / 旧”键（像 \ 切表面一样），你可以在场景 4 里原地对比。你选过的东西全部保留，只提高做工。
6. 分工按约定：模型、材质、灯具、贴图是我们的；场景代码的改动做成 diff，交给 夸张结构调研与设计 和 关卡设计 审核落地；每次动到几何都跑行走测试，保证每个结构都能走。
7. 代价（估算，还没实测）：不算 P，LOD0 三角面从 119 万到约 183-232 万；最重的 SP3（改完约 40-51 万）和 SP7（42 万，不变）都不到游戏泳池路线（P，312 万）的六分之一；滑梯从 16 万降到约 5.5 万；新贴图约 60-80 MB；灯的数量不变，投影的灯更少。帧时间在合并测试里测。
8. 有五个真正的设计选择要你决定：池边做法、弯墙的瓷砖大小、公共楼层的护栏、墙纸在高墙上的色差和接缝、灯会不会坏。都会先按推荐做好，再放一个键给你切换，不用等你回复。
9. 下一步：第二个工作流开工；每组做完都出 TODAY | NEW 对比图（桌面、手机档、0.35 m 近景），直接发给你看。

## 6. Cost

ESTIMATE throughout; nothing measured (rule 25). Triangles are LOD0 in thousands. TODAY LOD0 is this stage's recount of the Survey's inventories counting only LOD0 renderers (the Survey's column, which summed every LOD level of every kit, is shown beside it). The sums take each part's estimate once: Part B's bevels (half to all of a district's box-equivalents at +24 triangles each, exposed edges at one segment) carry the bullnose arrises; Part A adds only coves, joints, nosings, copings and facets (half its edge line); the niche is counted once at 1.2-2.4 k; the laylight set and the trims once. Wiring the game's Run! B services costs about 21 k LOD0 a 3 m cell with every piece (SP7's corridor, measured: EMT runs are 118 k of its 296 k of dress); the rows use a selection (bumper, cove, sprinkler main and heads, tray) at 5-8 k a cell.

| District | TODAY LOD0 (all levels) | Delta LOD0 | AFTER LOD0 | Renderers | New maps (MiB) | Where the triangles go |
|---|---|---|---|---|---|---|
| HUB | 18.5 k (28.9 k) | +0.2 .. +0.7 k | 18.7-19.3 k | +0 | 0 | bevels +0.1..+0.1, lamps +0.1..+0.6 |
| SP1 | 167.6 k (169.7 k) | -49 .. +15 k | 119-182 k | -15 to +5 (16 down-fill bodies out, the flume in about 10 pieces) | +2 (shares the pool sets) | bevels +24..+48, guards +0.8..+1.8, tile edges +4.0..+7.0, coves +1.0..+3.0, joints +2.0..+4.0, curves +0.0..+2.0, stairs + rails +8.0..+14, slab edges +0.5..+2.0, lamps +10..+15, fittings + water +6.0..+11, flume -112..-102, props +6.0..+9.0 |
| SP2 | 138.0 k (189.7 k) | +106 .. +188 k | 244-326 k | +100 to +200 (with the F1 dress selection) | +3-4 | bevels +12..+25, guards +4.5..+10, tile edges +1.5..+3.0, coves +1.0..+2.0, joints +1.0..+2.0, curves +0.0..+1.0, stairs + rails +10..+18, slab edges +3.0..+8.0, laylights +1.0..+2.0, lamps +45..+58, fittings + water +4.5..+8.5, props +2.0..+10, Run! B dress (selection) +20..+40 |
| SP3 | 214.9 k (331.2 k) | +185 .. +295 k | 400-510 k | +150 to +300 | +6-10 | bevels +20..+40, guards +15..+34, stairs + rails +16..+27, slab edges +2.0..+6.0, lamps +0.6..+4.7, props +132..+183 |
| SP4 | 70.8 k (100.0 k) | +111 .. +176 k | 182-247 k | +170 to +260 (no desks) | +3-5 | bevels +14..+28, guards +20..+45, stairs + rails +6.0..+12, slab edges +3.0..+8.0, laylights +2.0..+3.0, lamps +35..+36, props +32..+44 |
| SP5 | 105.3 k (140.7 k) | +149 .. +219 k | 255-324 k | +255 to +530 (with the services selection) | +5-9 | bevels +4.0..+8.1, guards +2.1..+4.8, joints +0.5..+1.0, stairs + rails +3.0..+6.0, lamps +47..+47, props +48..+80, Run! B dress (selection) +45..+72 |
| SP6 | 26.4 k (26.4 k) | +109 .. +190 k | 135-216 k | +25 to +55 | +2-3 | bevels +18..+36, guards +2.0..+4.5, tile edges +4.8..+8.8, coves +2.0..+4.0, joints +3.0..+5.0, curves +11..+12, stairs + rails +6.0..+12, slab edges +0.5..+2.0, laylights +0.5..+1.0, lamps +53..+91, fittings + water +7.0..+10, props +1.0..+3.0 |
| KIT | 28.5 k (41.5 k) | +24 .. +44 k | 53-73 k | -10 to +5 | +1 | bevels +7.1..+14, guards +2.8..+6.5, tile edges +1.0..+2.0, coves +0.5..+1.0, joints +0.5..+1.0, stairs + rails +1.0..+3.0, slab edges +1.0..+3.0, lamps +8.3..+8.7, fittings + water +2.0..+5.0 |
| SP7 | 420.4 k (584.4 k) | 0 now | 420 k (unchanged now) | 0 now | 0 now | nothing now (in flux); the end kits +8-16 k when wired |
| P | 3120.2 k (4431.1 k) | +97 .. +170 k | 3217-3290 k | +70 to +160 | +5-8 (the 24 kits) | fittings + water +25..+45, props +72..+125 |
| **without P** | **1191 k (1613 k)** | **+636 .. +1127 k** | **1827-2317 k** | | | +53-95 % |

- **Per view.** Districts are 384 m apart and drawn one at a time; with LOD1 at about 45 % of screen height and the cross-fade, the heaviest views (SP3's rooms, SP5's dock, SP2 from F0) add about 40-70 k drawn triangles each (Part C, C.7). The game's own pool route already draws 3.12 M at LOD0.
- **Maps:** about 60-80 MiB new on desktop (BC7 with mips; phone twins about a quarter): tile pieces 9.3, trim sheet and fireproofing 13-14, flume gelcoats 9, glass block 3-4, laylight 2.7, lamps 3-5, fittings 3, flow 1-1.5, props 20-30. All in `Resources`, so they ship in every desktop build until the editor-only follow-up (section 7, R4). The wear decal atlas (16 MiB) waits for tech_art row 10.
- **Draws:** the shells stay merged per material per room; one trim material per district (+1 draw a pass); props add renderers (table) under the SRP Batcher; no instancing in the project (tech_art 21); per-instance tint and lamp level through `SetShaderUserValue` (row 5) to keep the fast path.
- **Lights:** the same number; shadowed lights fall (SP4's laylight 2 -> 0; P1-A and the down-fill bounce cast none); +1-2 up-fill lights in V4.
- **GPU (desktop, M3 Max, 1080p; ESTIMATE):** bevel vertices +0.05-0.2 ms; a fall in view +0.05-0.2 ms, the cascade filling the view +0.2-0.5 ms; niches' thrown light and caustics +0.15-0.6 ms in the busiest pool views (70 §7.7 measured +0.15-0.35 ms of caustics in a hall); far pools under 0.1 ms; lamp modes under 0.2 ms CPU; geometry otherwise well under 1 ms a view. The combined test measures desktop Ultra frame time per district, NEW against TODAY.
- **Phone and WebGL:** scene 4 is desktop Editor only (RULES 5b); the sheets' phone row is the iPhone profile on the Mac's Metal, not a device. When Red takes a structure into the game: LOD1 kits, `_Phone` twins, edges kept, at most about 24 real lights a view with far pools beyond (Forward+ caps 32 on mobile), the caustic flipbook. WebGL: nothing changes until the WebGL line asks (rule 5; the bevel contract keeps box edges there).

## 7. Risks and dependencies

| # | Risk | What we do |
|---|---|---|
| R1 | Files in flux: SP7's `RunRecede.cs` and `RunEnd*.cs` (Red's #3), `PoolTerrain.cs` (dome A / B) | SP7 gets kits only; PoolTerrain's hunks are rebased at land; nothing waits on them |
| R2 | `Kit.cs` (and `Look.cs`) touched by four contracts | K1 lands first; K2-K4 are built on K1's staged files and rebased with `rebase_apply.sh` (PASS / NOOP / RETRY / ESCALATE) |
| R3 | New kit colliders (lockers, cabinets, panels, ladders, grates, the dock) change walkability | the walk test after every contract; colliders from sidecars, body-only where sight must cross (the cage, panels) |
| R4 | `Resources` ships in every build: +60-80 MiB of maps and the kits (on top of ~100 MiB of pool textures already) | listed per package; the line's open item (70 §9.5, an editor-only Resources follow-up) goes to 总控 and 构建 (and the WebGL line) before the pool enters the game; not blocking |
| R5 | Photosafety: caustics, flowing sheets and lamp modes add moving light (the 0d finding: 4.0 flashes in the worst second on the beach water) | a `flash_check.py` turn per fall and with the niches on; pool fluorescents stay steady until 0d is fixed; scene 4 only until then |
| R6 | Light layers are off (`m_SupportsLightLayers: 0`): P2-A niches light whatever is in range | as today; turning rendering layers on is 设置菜单与画质预设's asset (ask, not blocking) |
| R7 | GlassRT parity: the hit shader lacks `_FR_TILE`; decals and transparents are never traced | geometry and texture sets carry the information; the native `_FR_TILE` port stays the open item `surface_rt_parity` |
| R8 | Red's open Editor imports new kits with the importer it has | no importer change in these packages; if one is needed (readable meshes), it lands alone first; derived files re-checked a minute after each landing |
| R9 | Budgets: SP4 with desks passes 62's 120 k Office room; the Run! B dress is heavy | desks only if the owners want them, as a baked workstation kit or LOD1 past 6 m; the services as a selection unless the owners ask for the full dress |
| R10 | Caustic limits: 12 bodies and 16 niches published a frame (SP6: 13 bodies, 25 niches) | registration ordered by the walker's district and distance (K2, the water file) |
| R11 | P is the map's: seeds and placement are 关卡设计's | P renders on the Survey's seed; K2m lands on their word |
| R12 | 灯具适配 was routed P1-A / P2-A on 11-04 and is idle | we build them in G2 from their W kits unless they have started; ask 总控 at step 2's start |
| R13 | Plan limits, the lid, quota | disk first: every agent journals from its first minutes; resume text in the ledger job |

## 8. What stays (Red's picks, verbatim)

- The slide on its tile wall: "A · 墙在下面" (11-02); "让瓷砖作为墙壁去托举滑梯", mixed styles, the player rides, the Relay never, the tower as high as possible (10-31 17:15); never industrial: "滑梯当前的支架样式我不喜欢，看起来非常的工业" (10-31 17:15).
- The pool tile: "目前默认先选dish deep最后一个" (DishDeep at tone 1.5, 11-05 ~12:5x); GZ-C "选这个" (11-04 ~16:4x); "和dream pool更相似的瓷白" (10-31 17:19); porcelain stairs, dark nosing only (10-31 17:24).
- The pool look and lamps: "全部按推荐" (11-04 13:10): P1-A + P2-A + caustics; water level x4, dark x3, club x1.
- The water: every version on K, Red picks by walking ("我需要你把每一版的水都做进去，然后让我在structure scene试了之后我选择对应的", 11-04 13:1x); W-D "我目前认为这个ok".
- Scene-4 looks: "每个都按推荐来做" (11-04 ~14:1x): S1 C (SP6 dome A / B on L), S2 B (SP4 T-caps on the pinwheel arm ends), S3 A, S4 B (SP5 dock strips 1.6 -> 1.0, the sodium slot the brightest).
- Run!: "run选择R-B" (10-31); "这里应该是TA和TB相结合"; the red tube "加，用更亮档", "用这个点位吧"; look B "用我们自己的检查" (11-04 20:0x); #3 "我喜欢第三个，以这个去深化" (11-04 ~20:2x), its combination after "我先去走一遍" (11-05 ~12:5x).
- Zone lamps: "选择L03但是我希望灯罩亮度可以是1.8", "办公室选择O-C", "这里用当前现有的" (the tall hall keeps L03) (10-31).
- "办公室里/level 0还有level run里都不应该出现大型的螺旋滑梯" (10-31 17:29); "我需要所有结构都能够正常让玩家通行" (11-04 ~09:2x); walls never glow ("Q4 保持 B：墙不发光", 10-22 12:34); the surface key `\` ("加一个", 11-05 ~13:3x); modules on one 3D grid (11-03 ~18:3x); era lock 1988-91, nothing past 1993, no brands; staged assets, not shader tricks.

## 9. Files and how step 2 resumes

- This plan: `30_plan.md` (staged in `W/structures_assets/to_main/Documentation/research/structures_assets/`, landed by the parent with `10_survey.md`, `20_research.md`, `SOURCES.md`, `images/`, `sheets/`).
- Machine-readable rows: `W/structures_assets/plan.tsv` (id, district, element, upgrade, pri, ours, theirs, group, cost, risk_stays; LF). Generators (session scratchpad, copied to `W/structures_assets/tools/plan/`): `make_plan.py`, `plan_rows.py`, `red_asks_zh.py`, `merge_research.py`, `cost.py`.
- Inputs: `W/structures_assets/survey.tsv`, `renders/today/` (94 frames, inventories), the three research tracks (`research_pool.md`, `research_structure.md`, `research_families.md`, merged into `20_research.md`).
- The scene build log line: `W/structures_assets/buildlog_baseline_line.txt` (for `~/FrontRoomsMapWork/integration/structures/BUILD_LOG.md`).
- Step 2 starts from this section 4, reads `plan.tsv` for its rows, and journals in `W/structures_assets/journal.txt`.

## Appendix A · The runtime stand-in materials and what replaces them

From the Survey's ten material dumps (113 distinct runtime materials; 86 stand in for real ones; the rest are scene UI or the game's own runtime copies).

| Group (count) | Runtime materials (examples) | Replaced by | Package |
|---|---|---|---|
| lamp lenses and glows (about 30) | "glow lamp lens" x16 tints, halide, niche, globe, soffit lens, valance, window, start light red / green, SP5 goal lite, head pilot, lens off, SP7 goal lite | the kits' own lens and glow materials on the lens drive (P1-A, P2-A, small niche, ZL F5, ZL F7, HPS refractor, the dispatch head, the game's exit lite) | P7, P10, P15, P19 |
| laylights and wells (4) | SP2 laylight diffuser, SP4 laylight lens, oculus diffuser (B), ceiling t-bar white | `Prop_LaylightDiffuser`, the trim sheet's T-bar | P6, P3 |
| the flume (8) | slide gelcoat ivory, band teal, flange ivory, SL-A gelcoat, SL-A flange, SL-C cap, SL-C inside, slide tube | `Prop_Gelcoat_Ivory`, `_TealBand`, `_SLC_Cap`, `_SLA_Milky` | P12 |
| water stand-ins (7) | water Club, water Level, water sheet, weir fall (white water), lip outflow foam, gutter, caustic | the game's water (W-D) and its flow variant; the Gutter tile; the landed caustics | P13, P2 |
| pool trims (1) | nosing (x5 districts) | `Prop_PoolTile_Nosing`, `_Tread`, `_Riser` | P2 |
| construction greys (7) | SP3 plenum lining, SP5 sealed concrete, access floor panel, beam paint, glass block grout, chain link, SP2 glass-block floor light | `Plenum_Dark`, `Stair_Fill`, `Prop_AccessFloor` / the grate, `Prop_Fireproofing` or `Painted_Metal`, `Prop_GlassBlock`, `Prop_WireMesh` | P3, P8, P9, P14 |
| prop greys (about 20) | cctv beige, chair, computer cabinet grey, court red, crt beige, desk, dumpster green, head beige, locker, rubber black, rubber wedge, rules board, screen white, sign, start light head, towel, valance, board face, stand-in dark, SP7 C42 box, guide slot, rubber sweep, globe glass | the kits' own materials from `Resources/Surfaces` | P15-P20 |
| SP7 ghost bands (4, runtime copies) | ghost band lower, upper, soffit, wax | `RunB_*_Ghost` (main), when RunRecede.cs is free | later |
| scene UI (stays, about 18) | placard panel E, placard post, placard ink, font material, pad plate, pad rims, mark lines, kit-row gauge and grid ink, relay stand-in, reflection proxy | none: outside the fiction | - |
| the game's own runtime copies (stay, about 13) | Alarm_LED, CRT ad surface and faceplate glass, Glass_Window (merged panes), map pool club / level lens, Run B soffit halo, SP2 rim section slab (Stair_Slab), Stairwell x5, Wall_Compound @2.4 m, pool_water | none | - |
