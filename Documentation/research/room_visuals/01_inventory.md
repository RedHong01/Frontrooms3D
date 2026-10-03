# 01 — Room visuals inventory: what every room looks like today

Date: 2026-10-03 (about 11:00–11:40). Status: DONE (inventory step of the room-visuals workflow, task N2 in `Documentation/VISUAL_CHAT_TASKS.md`).
Read-only. Nothing in the real project was changed. Only this folder was written.

**How this was made**
- Code was read in the private clone `scratchpad/proj_rooms`. `diff -rq` against the real project showed **no difference** in `Assets/Scripts`, `Assets/Editor`, `Assets/Shaders` and `Assets/Levels` at 10:5x, so every `file:line` below is valid for the real project at that time. Paths are relative to `Frontrooms3D/`.
- Saved material values were read from `Assets/Resources/Surfaces/*.mat`. They match the generator in `FrontRoomsRenderSetup.cs` exactly (nobody has hand-tuned a room material yet).
- Colours in hex come from the texture generator `Tools/lookdev/gen_surfaces.py` (the base colour before tint, wear and light).
- Current frames: the sibling capture step of this workflow, `images/room_*.jpg` (2026-10-03 11:26; harness in `harness/`). Run seed 516574485 (`Random.InitState(4242)`), map root (−576, 0, −576). **They use a 72° vertical FOV; the game camera is 76°** (`Assets/Scenes/FrontRooms3D.unity:313`), so the game shows slightly more.
- Known problems cite the audit (`research/interaction_audit/10_audit_report.md`, findings F1–F17) and the task queue (`VISUAL_CHAT_TASKS.md`, rows G, Q, WG, N, R). Claims I only read in code and did not see in a frame are marked **UNVERIFIED**.

**Status words used below**
- **FINAL**: Red signed it off. *No room is FINAL today.*
- **FIRST PASS**: built from research, in the game, not signed off.
- **PLACEHOLDER**: primitives or stand-in values, meant to be replaced.
- **NOT IN PLAY**: exists in code, but no path in the game reaches it.
- **MISSING**: does not exist.

---

## 0. Short answer (for Red)

1. **What a player actually sees in one run (4 room families):**
   - the title corridor: a pool of 5 **Lobby** rooms, 11.5 × 12 × 2.9 m each, with double doors;
   - the **start rooms**: the same Lobby rooms, frozen, with the start door into the map;
   - the map's **Level 0** zones at three heights: **Low 2.4 m, Standard 2.9 m, Tall 5.4 m**;
   - the map's **Office** zones (Standard height only).
2. **Shift, the stream Office, Run and Exit are NOT IN PLAY.** The stream forces Lobby-only (`FrontRoomsRoomStream.cs:316`). The only caller of `BeginPlayableSequence` is an editor test (`Assets/Editor/FrontRoomsStreamVerification.cs:80`). These four profiles are seen only in the Edit-mode preview (`EDITOR_PREVIEW / Room profiles (generated)`, `FrontRooms3DGame.cs:200-212`) and in captures.
3. **Run! today** is not a corridor. It is an 11.5 × 12 m white room with a VCT floor, two hanging red EXIT signs, a primitive gurney, three chairs and wall rails. Its tubes are mostly dead (70 %), so it reads red. It has had **four different looks in four documents** and none of them came from a research pass or a plan (§3.4). That matches Red's note: there is no visual plan for Run!.
4. **There is no true Exit (ending).** No exit, goal or end door exists in the map or the game. A run ends only when the Relay catches you (`research/interactables/01_inventory.md` §0). The stream room named "Exit" is a cyan *breather* that loops back to Lobby.
5. **Biggest cross-room problems today** (details in §6):
   - an even light wash with lamp shadows erased (audit F5);
   - nothing to reflect: no probes, reflection intensity 0.3, which is only 7 % linear (F4; `research/glass/10_implementation.md` §1);
   - the map's Level 0 troffer is a plain white slab, not the title's troffer (F10, Q4);
   - zone borders are hard cuts in paper and height (N1);
   - lamps light through walls (Q5);
   - doors, keys and glass are primitive cubes (F10; R3/G1 running);
   - the title rooms' baseboard floats 10 cm above the carpet (new, §3.0).

---

## 1. Shared foundation (true for every room)

### 1.1 Pipeline and camera

| Item | Value | Source |
|---|---|---|
| Pipeline | URP 17.3, Forward+, HDR, 4× MSAA, render scale 1, depth texture on | `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs:77-105`; `Assets/Settings/FrontRooms_URP.asset:28` |
| Shadows | main light 2048, additional-light atlas 4096, soft High, distance 40 m, 2 cascades, bias 0.6/0.6 | `FrontRoomsRenderSetup.cs:92-102`; `FrontRooms_URP.asset:46-69` |
| Probes and layers | reflection-probe blending **off**, box projection **off**, light layers **off** | `FrontRooms_URP.asset:54-55, 76` |
| SSAO | depth-normals source, radius 0.45 m, intensity 1.6, direct-light strength 0.35, high samples and blur | `FrontRoomsRenderSetup.cs:130-141` |
| Extra renderer feature | "FrontRooms Metal Glass RT" (ChatGPT's ray-traced glass prototype; draws nothing today, `research/glass/rt/01_code_review.md` §1) | `Assets/Settings/FrontRooms_URP_Renderer.asset:94`; `Assets/Plugins/macOS/libFrontRoomsMetalGlassRT.dylib` |
| Camera | vertical FOV **76°**, near 0.06, far 80 in the title; in the map far = build radius × 24 − 2 = **46 m** | scene `FrontRooms3D.unity:311-313`; `FrontRooms3DGame.cs:247, 755-757`; `FrontRoomsMapWorld.cs:69` |
| Clear colour | solid #22231C (no skybox) | `FrontRooms3D.unity:289-290` |
| Camera AA / dither | camera AA off (MSAA only), dithering on | `Assets/Scripts/Rendering/FrontRoomsPostStack.cs:64-74` |
| HDR toggle | display setting writes `allowHDR` (player pref) | `FrontRooms3DGame.cs:1292-1306` |

### 1.2 The surface shader `FrontRooms/Surface`

File: `Assets/Resources/Rendering/FrontRoomsSurface.shader` (320 lines). Used by every room material.

- **World-projected UVs** (`:155-162, 176-183`). Walls run along the wall with V up; floors and ceilings use world X/Z. The print is continuous across slabs and chunks. `_TileSize` = metres per repeat.
- **Mesh-UV mode** `_FR_MESH_UV` (`:22, 96, 176-179`) for props, doors, lenses and signs (UVs in metres).
- **Macro wear in world space** (`:189-198`). Two samples of `MacroWear_M` (1024²): 8 m, and 12.8 m turned 90°. R tone, G dirt, B damp, A streaks. Controls: `_MacroTone`, `_MacroDirt`.
- **Water stains** (`:200-205`): tide-mark rings and pools, `_StainStrength`, `_StainColor` (default #6B4F29).
- **Damp carpet** (`:210-214`): darker (−32 %), smoother (toward 0.62), flatter normal. `_WetStrength`.
- **Wall grime** (`:216-223`): dirt in the 0.35 m above the floor (`_FloorGrime`), streaks in the 1.6 m under the ceiling (`_CeilingGrime`). The ceiling height comes from `_CeilingHeight`, which the map sets per renderer with a MaterialPropertyBlock (`FrontRoomsMapWorld.cs:896-913`).
- Lighting: `UniversalFragmentPBR`, ambient from SH only (`:240, 255`). Fog: URP `MixFog`.
- **Not in the shader today:** reflection-probe keywords (no box projection possible), a MotionVectors pass (TAA would smear), the wallpaper print layer `_FR_PRINT` (R2, still in a private clone).
- All tile sizes divide **192 m** (`ModuleUnits.WorldPeriod`, `Assets/Scripts/FrontRoomsMap/FrontRoomsModuleUnits.cs:29`). Map roots, capture roots and the title's floating-origin shift all sit on multiples of 192 m.

### 1.3 Room surface materials (all in `Assets/Resources/Surfaces/`)

Definitions: `FrontRoomsRenderSetup.cs:269-301`. Textures: `Resources/Surfaces/Textures/<stem>_A/_N/_S(/_E).png`, made by `Tools/lookdev/gen_surfaces.py`. Imported trilinear, 16× aniso, BC HQ, max 4096 (`FrontRoomsRenderSetup.cs:617-641`).

| Material | Texture (px) | Base colour (generator) | World tile (m) | Tint | Wear: tone / dirt / stain / wet / floor grime / ceiling grime | Used by |
|---|---|---|---|---|---|---|
| `L0_Wallpaper` | `Wallpaper_Chevron` 2048×3072 (CC0 chevron recreation, `Tools/lookdev/ref/Backrooms_Chevron_CC0.png`) | ground #D2C27C, mid #AC9A52, deep #766A34, cream #E3D594 | 0.75 × 1.125 (one roll) | white | .45 / .30 / .18 / 0 / .60 / .50 | stream Lobby; map Level 0 (all heights); start rooms |
| `L0_Wallpaper_Shift` | same | same | 0.75 × 1.125 | .90/.88/.80 | .60 / .45 / .45 / 0 / .80 / .85 | stream Shift only |
| `Exit_Wallpaper` | `Wallpaper_Chevron_Cold` 2048×3072 | ground #AFC0B6, mid #87998F, deep #56655E, cream #C5D3C9 | 0.75 × 1.125 | white | .35 / .20 / 0 / 0 / .40 / .20; stain colour #4D5752 | stream Exit only |
| `L0_Carpet` | `Carpet_LoopPile` 2048² | yarn #9A8558 (flecks #7C6A47, #B09C6E) | 1.0 | white | .40 / .35 / 0 / **.55** | stream Lobby; map Level 0 |
| `L0_Carpet_Shift` | same | same | 1.0 | .92/.90/.84 | .50 / .50 / 0 / **.85** | stream Shift |
| `Exit_Carpet` | same | same | 1.0 | .66/.72/.74 (cold) | .35 / .20 / 0 / .30 | stream Exit |
| `L0_Ceiling` | `Ceiling_Fissured` 2048² (2'×4' fissured tile + T-bar) | face #D9D2BF, T-bar #E3DFD3 | 1.2 (two 0.6 × 1.2 tiles) | white | .25 / .20 / .22 | stream Lobby, Exit; map Level 0 |
| `L0_Ceiling_Shift` | same | same | 1.2 | .93/.92/.86 | .45 / .20 / .55 | stream Shift |
| `Office_Wall` | `Office_Drywall` 1024² (knockdown) | #BDB6A4 | 1.2 | white | .30 / .20 / 0 / 0 / .40 / .20 | stream Office; map Office |
| `Office_Carpet` | `Office_CarpetTile` 2048² (24" tiles, quarter-turned) | #5B636B, flecks #3E8079 #8C6A7D #2E3237 #9AA2A8 | 1.2 (2×2 tiles of 0.6) | white | .30 / .30 / 0 / .15 | stream Office; map Office |
| `Office_Ceiling` | `Office_Ceiling2x2` 2048² | face #DCD8CC | 1.2 (2×2 tiles of 0.6) | white | .25 / .20 / .30 | stream Office; map Office |
| `Run_Wall` | `Run_HospitalWall` 1024² (white semi-gloss paint, scuffs) | #E3E1D9 | 1.2 | white | .25 / .25 / 0 / 0 / .45 / .15 | stream Run only |
| `Run_Floor` | `Run_VCT` 2048² (12" VCT, chips, heel scuffs, wax lane) | #DAD7CC, chips #8F8A7E | 1.2 (4×4 tiles of 0.3) | white | .20 / .35 | stream Run only |
| `Run_Ceiling` | `Office_Ceiling2x2` | #DCD8CC × 1.04 | 1.2 | 1.04 | .20 / .20 / .20 | stream Run only |
| `Run_ExitSign` | `Run_ExitSign` 1024×512, emission map | panel #E2E0D8, letters #C81A16 ("EXIT" + chevrons, Arial Bold) | mesh UV 0.36 × 0.18 | white, emission 3.2 | — | stream Run signs |
| `Troffer_Lens` | `TrofferLens` 1024×2048 (prismatic, three tube stripes) | #EDEBE3 | mesh UV 0.6 × 1.2 | emission (2.2, 2.1, 1.8) | — | all stream lenses; map **Office** lenses |
| `Office_Louver` | `Office_Louver` 1024² | #BFC2C4, metallic .85 | 0.6 | emission (1.6, 1.55, 1.4) | — | **unused** (`OfficeLouver` now returns `Troffer_Lens`, `FrontRoomsSurfaces.cs:44-49`) |
| `Door_Veneer` | `DoorVeneer` 1024×2048 (oak) | #BE8A4E / #8A5A2E | mesh UV 1.12 × 2.62 | white | .15 / .10 | stream doors; map doors |
| `Painted_Metal` | `PaintedMetal` 1024² | #DAD4C4 | mesh UV 1.0 | white | .15 / .15 | stream troffer pans |
| `Cove_Base` | none | #3B3024 (tint .23/.19/.14), smoothness .38 | — | — | .20 / .20 | stream baseboards; map door/window trim, column coves |
| `Office_BlackedGlass` | none | (.018, .02, .022), smoothness .95 | — | — | .10 | stream exit-sign housings, gurney wheels; Office kit |

Notes:
- `gen_surfaces.py:1-5` still describes the old 256 m grid (paper 256/373 m, 4 ft = 1.219 m). The live tiles are metric (0.75 / 1.2 m, `FrontRoomsRenderSetup.cs:29-33`). The art is shown about 1.6 % smaller than drawn. Harmless, but the doc numbers in `VISUAL_RESEARCH_LOOKDEV.md` are stale.
- The chevron's ink is baked into the paper's normal, smoothness and cavity maps (`research/wallpaper_motion/10_synthesis.md` §2). This blocks the moving-print plan until R2 splits it.
- Red picked the **WP03 "Hard edge"** print for the paper (Figma 2407:884; `VISUAL_CHAT_TASKS.md` Q1b). It is not in the game yet. Every Level 0 frame today shows the CC0 chevron.
- The six 2048×3072 wallpaper maps are non-power-of-two. They load **uncompressed** (96 MB in the map, `research/webgl/03_measured_budgets.md` §0.6). This is a memory cost on desktop too.

### 1.4 Ambient, fog and reflections

| Item | Value | Source |
|---|---|---|
| Ambient | Trilight. Sky (.20, .19, .15) ≈ #333026; equator (.26, .24, .17) ≈ #423D2B; ground (.40, .36, .24) ≈ #665C3D | scene `FrontRooms3D.unity` RenderSettings; `Assets/Scripts/Rendering/FrontRoomsLook.cs:17-19` (equal since F5 fix, 2026-10-03) |
| Fog | Exponential², colour (.16, .15, .11) ≈ #29261C, density 0.014. One value for every zone | scene; `FrontRoomsLook.cs:20-21` |
| Reflection | default reflection = Unity's built-in skybox cube at 128 px, intensity 0.3. Unity applies it in gamma, so 0.3 = **0.073 linear** | scene; `research/glass/10_implementation.md` §1 finding 1 |
| Reflection probes | **0** at runtime | audit 05 §A |
| Zone reflections | `FrontRoomsLook.SetZoneReflection(Level0 / Office / Tall / DeadLamp)` is a **stub** in main (`FrontRoomsLook.cs:24-36`). The real version and four 256 px HDR cubes exist in the glass clone (G6), not promoted. Nobody calls it yet | G6 |
| Fill light | scene object "Soft ambient direction": directional, (.769, .816, .80), intensity 0.16, soft shadows at strength 0.18 | scene `FrontRooms3D.unity:2255-2293`. The audit says it lights through every ceiling (F5) and costs a whole shadow pass (WG6) |
| Who applies it | the game reads the scene's Lighting settings (`FrontRooms3DGame.cs:323` only rebuilds the SH probe). `FrontRoomsLook.ApplyAmbient` runs in the standalone map scene and in edit-mode captures (`FrontRoomsMapWorld.cs:603-607`) | audit F4 |

### 1.5 Post (film look)

- **Global volume** `Resources/Rendering/FrontRoomsPost.asset` (`FrontRoomsRenderSetup.cs:184-237`; added by `FrontRoomsPostStack.Ensure`, `FrontRooms3DGame.cs:329`):
  - ACES; bloom threshold 1.05, intensity .55, scatter .72, tint (1, .93, .78), HQ filtering;
  - white balance +9 temperature, −7 tint (toward green);
  - post-exposure +.15, contrast −6, saturation −8;
  - lift (1, .99, .94, +.035) for lifted warm blacks;
  - shadows (.97, 1, .94), highlights (1, .99, .93);
  - vignette .26 (smoothness .45, colour #0D0B08);
  - grain Medium3 .22 (response .75); chromatic aberration .06; lens distortion −.04 (scale 1.01).
- **Office local volume** `FrontRoomsPost_Office.asset` (`FrontRoomsRenderSetup.cs:151-182`): white balance +2 / −10, post-exposure +.32, contrast +4, saturation −18, colour filter (.97, 1, .98), lift (.98, 1, .98, .02), grain .18, CA .03. Priority 1, 2.5 m blend, placed over Office cells per chunk (`FrontRoomsMapWorld.cs:1686-1728`). Not used in the title stream.
- No grade for Tall, Low, dead-lamp areas or any stream profile.

### 1.6 Two different lamp systems

The title stream and the map do not share lamp code. Both are drawn as downward spot lights (162° outer / 96° inner).

| | Title stream (`FrontRoomsRoomStream.cs`) | Map (`FrontRoomsMapWorld.cs`) |
|---|---|---|
| Fixture | painted-steel pan 0.588 × 0.03 × 1.188 + `Troffer_Lens` diffuser 0.54 × 0.006 × 1.14 + an additive "volumetric beam" frustum 1.0 m long (`:1083-1113, 1156-1207`) | one lens cube 0.6 × 0.025 × 1.2, no pan, no beam (`:1173-1227`) |
| Density | 12 per room: 3 columns (centre, ±3.6 m) × 4 rows (z 1.7 / 4.55 / 7.4 / 10.25), snapped to the world 0.6 × 1.2 grid (`:58-66, 1142-1154`). 1 per 11.5 m² | 1 per 3 m cell, lens at +0.3 m Z (`ModuleUnits.TrofferOffsetZ`). 1 per 9 m² |
| Shadows | the 4 centre lamps cast soft shadows; side lamps never (`:1109`) | 34 % of lamps may cast (`:1215`), soft, only within 9 m (`shadowRadius`) |
| On/off budget | every lamp of a loaded room | lit within 16 m, fading over the last 3 m (`lightRadius`) |
| Temperament | per-room ballast model: strike delay 0.4–2.2 s after the door opens, 0–5 strike flickers, rise 0.45–1.6 s; dead or failing by profile odds (`:1618-1664, 804-873`) | 5 modes rolled per lamp from the seed and the tier (§2.5) |
| Lens glow | colour × lerp(0.02, 2.6, level) (`:1725-1737`) | lens emission × max(0.04, level) (`:1287-1288`) |

### 1.7 Era lock (applies to every room)

`research/office_and_film/22_era_lock.md` §2: now = **1990**; newest design **1993**; second-hand back to about **1955**; no printed date after 1990; no trademarks. The prop kit is checked against it (54 assets). The room shells are not dated objects, except the Run room's hospital props and signs (§3.4).

---

## 2. Map rooms (where the run happens)

Map facts used in all of §2:
- Code: `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs` (2090 lines), generator `FrontRoomsMap.cs`, units `FrontRoomsModuleUnits.cs`, numbers `Assets/Levels/FrontRoomsLevel0.asset`.
- Grid: 3 m cells, 24 m chunks (8 × 8 cells), 5 × 5 chunks built (`buildRadius` 2).
- Walls 0.16 m centred on cell lines. Floor slab 0.2 m, ceiling slab 0.16 m (`FrontRoomsModuleUnits.cs:33-42`).
- Zone mix (`FrontRoomsMap.cs:79-123`; level asset): **Low 35 %, Standard 55 %, Tall 10 %**. Office = 30 % of Standard zones (about 16.5 % of all zones). So about 38.5 % of zones are Level 0 Standard. Zones are Voronoi cells, median 63 cells (`research/relay_pursuit/20_level_design.md` §1).
- **The map uses one Level 0 material set everywhere** (Lobby's). It never uses the Shift or Exit materials (`FrontRoomsMapWorld.cs:2039-2071`).
- Shell meshes are merged per 6 m block, per ceiling-height class and per material (`:783-867`).
- The level asset is missing the newer column, module and tier fields (`lowPillar/standardPillar/tallPillar` are stale names). Unity therefore uses the code defaults for `columnMinRoomCells`, the three column chances, `moduleChance` and the tier table.

### 2.1 Level 0 · Standard (2.9 m) — the main maze

- **Player sees** (`images/room_map-l0-standard_wide.jpg`, `_detail.jpg`): the yellow chevron paper wall to wall, sand loop-pile carpet with darker damp patches, 2'×4' tile ceiling with brown tide marks, one bright white slab per cell. Mostly wall-lined corridors (82 % of non-tree edges are walls), off-centre doorless doorways, sometimes a room with 0.6 m columns.
- **Surfaces:** `L0_Wallpaper` / `L0_Carpet` / `L0_Ceiling` (§1.3). Walls between two Level 0 cells use one material through the full 0.16 m thickness.
- **Ceiling:** 2.9 m. Ceiling grime band 1.3–2.9 m on walls (`_CeilingHeight` = 2.9). Tiles 0.6 × 1.2 from the texture (no T-bar geometry).
- **Floor:** loop pile, `_WetStrength` .55.
- **Trims:** **no baseboard or cove on walls** (`research/outlets/02_code_paths.md` §0.1). Trim (`Cove_Base`) only on door and window frames and column coves.
- **Lights:** spot at 0.06 m under the lens, colour (1, .96, .88) ≈ 4000 K-ish warm white, intensity **5** × level, range **10 m**, shadows as in §1.6 (`:1197-1215`; lamp intensity `:2050`).
- **Lens:** `Map / Level 0 lens`, a plain URP Lit material: albedo (1, .98, .92), smoothness .1, emission (1, .96, .84) × 2.6 (`:2043`). **No texture, no pan, no beam.**
- **Fog/ambient/reflection:** global (§1.4). No zone grade.
- **Props/dressing:**
  - halls of at least 4 × 4 cells: 35 % get a furniture pile (`FrontRoomsLevelProfile.cs:53`; `FrontRoomsMapWorld.cs:1559-1571`);
  - module rooms (30 % of rooms a module fits): `L0_WaitingRoom_4x3` (3 ladder-back chairs, turned side table, torchiere, wall clock; one Dead and one Failing lamp; fill None);
  - no outlets, no signs, no decals.
- **Sound hooks:** footsteps on `Surface.Carpet`, dampness 0.4 (`Assets/Scripts/Audio/FrontRoomsSoundDirector.cs:274-296`). The fixture hum follows the 4 nearest Lights within 10 m and reads their intensity, so a dead lamp is silent and a flicker is heard (`:298-355`).
- **Source:** `FrontRoomsMapWorld.cs:772-885` (build), `:945-1054` (edges), `:1173-1227` (fixture), `:2039-2071` (materials); generator `FrontRoomsMap.cs:292-310, 360-383`.
- **Status:** FIRST PASS (surfaces, wear, grade); lens PLACEHOLDER; wall trim MISSING.
- **Known problems:**
  - even wash, no readable pools; the 162° cone pushes the shadow bias to about 9–13 cm, which erases contact shadows (F5; audit images 01, 37);
  - the lens is a flat white slab and does not match the title troffer the player just walked past (F10 rank 5; Q4);
  - unshadowed lamps light the next rooms through walls (Q5; `FrontRoomsMapWorld.cs:1229-1236` says so itself);
  - no baseboard, outlets or any wall detail at eye level (N3 outlets is RUNNING);
  - the paper is still the CC0 chevron, not WP03.

### 2.2 Level 0 · Low (2.4 m)

- **Player sees** (`images/room_map-l0-low_wide.jpg`, `_detail.jpg`): the same paper and carpet under a ceiling 0.5 m lower. More walls and more doorless gaps (`lowWall` .5, `lowArch` .2), small rooms (2 × 2–3 cells). Doors appear on Low ↔ Standard borders.
- **Surfaces:** as §2.1. Grime band 0.8–2.4 m (`_CeilingHeight` 2.4).
- **Ceiling:** 2.4 m. Arch tops 2.2 m (`ArchTop`), the same as in Standard.
- **Lights:** as §2.1 (intensity 5, range 10). The lens is 2.4 m up, so pools are smaller and brighter.
- **Columns:** never in Low zones (`FrontRoomsMap.cs:531`).
- **Props:** the module `Low_Storage_2x3` (bookcase, crate, pallet, ply cabinet; lamps Dim, Off, Failing; fill None). Piles need 4 × 4 cells, which Low rooms never reach, so Low has no piles.
- **Sound:** carpet, dampness **0.55** (the "deep, sticky carpet", `FrontRoomsSoundDirector.cs:285-296`). The shader's wetness is the same .55 in every zone, so picture and sound disagree.
- **Source:** as §2.1; heights `FrontRoomsMap.cs:62-70`.
- **Status:** FIRST PASS (same look as Standard).
- **Known problems:** nothing distinguishes Low visually except height. No damper carpet, no lower troffer style, no stains. The door-in-a-wall border is a hard cut (N1).

### 2.3 Level 0 · Tall halls (5.4 m)

- **Player sees** (`images/room_map-l0-tall_wide.jpg`, `_detail.jpg`, `_ceiling.jpg`, `room_map-l0-pile_wide.jpg`): large open halls (one hall of 5–7 cells per chunk, `tallWall` .3) with 5.4 m of chevron paper, a dark ceiling far above, a grid of bright slabs, windows into the neighbouring zones, 0.9 m columns in 70 % of halls, and often a furniture pile.
- **Surfaces:** Level 0 set. Tall zones are never Office (`FrontRoomsMap.cs:306`). Grime band 3.8–5.4 m.
- **Ceiling:** 5.4 m. Same lens and tiles as Standard.
- **Lights:** intensity **8** (5 × 1.6), range **12 m** (`FrontRoomsMapWorld.cs:1206, 1210`). Lens emission is not scaled up.
- **Columns:** 0.9 m ("large"), faced in the wallpaper, `Cove_Base` cove 0.10 m, no bulkheads (`:1064-1077`).
- **Windows:** every opening on a Tall border is a window, 1.4 m wide, sill 0.35, head 2.0 (`FrontRoomsMap.cs:366`). Pane: a 30 mm cube with a code-built transparent Lit material (.75, .85, .88, α .28, smoothness .9), plus jamb and head trim, no sill trim (`FrontRoomsMapWorld.cs:1040-1053, 2073-2089`).
- **Props:** piles at **≥ 60 %** in Tall halls (`:1560`). Tableaux: ZeroPile 10 % when the ceiling is over 4.5 m, CentreSculpture to 72 %, CopyPasteRow to 88 % (`Assets/Scripts/Office/FrontRoomsFurniturePile.cs:174-189`). Module `Tall_PillarHall_6x5`: 10 Dead and 1 Failing lamp out of 30, fill Pile.
- **Sound:** carpet, dampness 0.3. No reverb change for the 5.4 m volume is set from the room (UNVERIFIED on the FMOD side).
- **Source:** as §2.1; piles `FrontRoomsFurniturePile.cs` (714 lines).
- **Status:** FIRST PASS (geometry, piles); windows PLACEHOLDER (G1–G4 done in a clone; G9 waits on the map chat).
- **Known problems:**
  - Tall lamps read as the same flat slabs, now far away; no fixture change for a tall hall (suspended or high-bay would be period options);
  - the window pane reads as a pale veil (F3). The new glass fixes the veil but makes the pane invisible (`research/glass/20_verification.md` §0);
  - the earlier play screenshots showed lenses and panes drawing flat cyan here: shader variants compiling. Fixed for Play Mode by `FrontRoomsPlayModeShaderCompile.cs` (F1 in the queue);
  - a centre-sculpture pile may rise to 0.82–0.95 × H (`FrontRoomsFurniturePile.cs:115, 191-200`): 4.4–5.1 m in a Tall hall. The synthesis capped Tall piles at 3.6 m (`research/office_and_film/10_synthesis.md` decision 10). The captured pile is 4.24 m tall (`harness/room_capture_log.txt`, t 13.2).

### 2.4 Office (Level 4) · Standard (2.9 m)

- **Player sees** (`images/room_map-office_wide.jpg`, `_detail.jpg`, `room_map-column-office_*.jpg`): greige knockdown drywall, blue-grey carpet tiles, 2'×2' ceiling, cubicle pods, desks with beige CRTs, task chairs, vending machine, water cooler, copier, filing cabinets, a blacked-out interior window, 0.9 m columns joined by bulkheads. A cooler, greener, flatter grade fades in over 2.5 m.
- **Surfaces:** `Office_Wall` / `Office_Carpet` / `Office_Ceiling` (§1.3).
- **Ceiling:** 2.9 m; bulkheads 0.35 m deep under it, column-wide, in drywall, render-only (`FrontRoomsMapWorld.cs:1070-1076`).
- **Lights:** colour (1, .96, .88) (the same as Level 0), intensity **5.5**, range 10 (`:2059`). Lens `Troffer_Lens` (textured prismatic, three tube stripes).
- **Grade:** Office local volume (§1.5). **Office fog is not implemented** (the synthesis asked for #3B3F35, density 0.018, `research/office_and_film/10_synthesis.md` §6.5).
- **Props/dressing:** `FrontRoomsOfficeKit.Dress` (6-argument map path, columns as obstacles) (`Assets/Scripts/Office/FrontRoomsOfficeKit.cs`, 726 lines):
  - pods in rows with 1.6 m chase lanes, 1.05 m off the walls; 8 % of bays left empty; 15 % of pods with tall 1.65 m panels;
  - per desk: 12 % no monitor; paper 60 %, phone 45 %, binders 35 %, chair 85 % (pushed out and turned), bin 50 %;
  - wall units: interior window 50 % (ceiling ≥ 2.6 m), vending 70 % / 30 % (room area over/under 60 m²), cooler 65 %, copier 60 % / 25 %, filing cabinets in a run;
  - module `Office_Bullpen_4x4` (copier, 2 filing cabinets, water cooler; one Dim lamp; fill Office);
  - all kit models are Blender-built (`Tools/Blender/frontrooms_kit/assets/*.py`), loaded by `FrontRoomsKitLibrary.cs`.
- **Sound:** `Surface.CarpetTile`, dampness 0.15.
- **Source:** `FrontRoomsMapWorld.cs:1548-1558, 1686-1728, 2052-2060`; `FrontRoomsOfficeKit.cs`; `FrontRoomsRenderSetup.cs:151-182`.
- **Status:** FIRST PASS (kit round 2 done 2026-10-02; grade in). Lens decision open.
- **Known problems:**
  - synthesis decisions not landed: flat opal lens `Office_LensOpal` (the lens is still the prismatic striped one); Office lamp odds 25 % dead off the spine; zone fog; 72° FOV (§6 of the synthesis);
  - the grade values in code differ from the synthesis table (temperature +2 vs +1, saturation −18 vs −22, exposure +.32 vs −.15);
  - dressing one Office room costs 42–166 ms in one frame (Q13 RUNNING);
  - `Kit_InteriorWindow`'s pane is an opaque black slab (G5);
  - the Office ↔ Level 0 border is a hard paper cut inside one 0.16 m wall (`images/room_map-border-theme_detail.jpg`; N1).

### 2.5 Lamp temperaments and the dead-lamp look (map)

Rolled once per lamp from the seed, the cell and the run tier (`FrontRoomsMapWorld.cs:1217-1225`). A module can force a mode or remove the lamp (`ModuleLamp.Off`, no fixture at all, `:1176`).

| Mode | Level over time (`:1384-1416`) | What the player sees (`images/room_map-lamp-*_detail.jpg`) |
|---|---|---|
| 0 Steady | 0.98 ± 0.02 at 110 rad/s | white lens, full pool |
| 1 Stutter | 0.97 ± 0.03; every 5–20 s a 0.15–0.85 s stutter, on/off 0.95 / 0.05 by Perlin noise | steady, then a short burst of on/off |
| 2 Failing | (0.25 + 0.45·wave) × dropout; dropout to 0.05 when Perlin > 0.72. Never above 0.70 | breathing light with hard dropouts (`room_map-failing-lamp_high.jpg` level 0.664 vs `_dropout.jpg` level 0.034) |
| 3 Dead | 0; blinks to 0.8 for 0.06–0.18 s every 8–28 s | Light off; **lens keeps 4 % glow** (grey slab, `room_map-dead-lamp_detail.jpg`) |
| 4 Dim | 0.42 ± 0.03 at 90 rad/s | pool at 42 %; the lens itself still clips to white (`room_map-lamp-dim_detail.jpg`) |

Odds by run tier (`FrontRoomsLevelProfile.cs:164-171`; dim = the rest):

| Tier | Steady | Stutter | Failing | Dead | Dim |
|---|---|---|---|---|---|
| 1 | 62 % | 20 % | 10 % | 5 % | 3 % |
| 2 | 57 % | 21 % | 12 % | 6 % | 4 % |
| 3 | 52 % | 22 % | 14 % | 8 % | 4 % |
| 4 | 46 % | 24 % | 16 % | 10 % | 4 % |
| 5 | 40 % | 25 % | 18 % | 12 % | 5 % |

- The tier rises every 4 new zones or after 120 s without one (`FrontRoomsLevelProfile.cs:157-160`). Odds are the same for Level 0, Office, Low and Tall.
- **Dead-lamp look:** a grey lens, no pool. The cell is lit only by its neighbours' wide cones. The glass track captured a separate "DeadLamp" reflection cube (G6, clone). Frames: `images/room_map-dead-lamp_wide.jpg`; audit `images/37_rep_Dark.png`; `research/glass/images/g10_06_deadlamp_windows.jpg`.
- **Known problems:**
  - in a still frame, Dim and Failing lenses look as white as Steady ones (the lens clips at emission 2.6 × 0.42 ≈ 1.1, above the bloom threshold 1.05); the difference is only in the pool;
  - stutter and failing reach the hum (the Light's intensity), but there is no matching visual on the lens edge (no end-darkening, no tube flicker pattern);
  - the planned lamp-override layer (Omen / Dip / Sag; "your room's lamps flicker" as warning stage 1) is not built (`RELAY_PURSUIT_REDESIGN.md`, `research/relay_pursuit/20_level_design.md` §2).

### 2.6 Columns and bulkheads

- Columns stand on cell corners of the world 6 m grid, only inside carved rooms of at least 3 × 3 cells, never in Low zones or corridors. A qualifying room rolls once: Level 0 **25 %**, Office **75 %**, Tall **70 %**; on a hit every grid corner inside gets one (`FrontRoomsMap.cs:107-114, 501-552`).
- Size: **0.6 m** in Level 0 rooms, **0.9 m** in Office and Tall (`FrontRoomsModuleUnits.cs:80`).
- Faced in the room's wall material; `Cove_Base` cove 0.10 m high, 6 mm proud, on **every** column (the unit comment says Office only) (`FrontRoomsMapWorld.cs:1064-1069`).
- **Office only:** a bulkhead to the next column, 0.35 m deep, column-wide, render-only (`:1070-1076`).
- Frames: `images/room_map-column-l0_wide.jpg`, `_detail.jpg`, `room_map-column-office_wide.jpg`, `_detail.jpg`.
- **Status:** FIRST PASS (rule v1 agreed with the map chat, `OFFICE_LEVEL_FURNITURE_RESEARCH.md` decision 3).
- **Known problems:** a wallpapered square column reads as "more wall"; there is no corner bead, no column cap at the ceiling, no T-bar trim where the ceiling meets the column.

### 2.7 Openings between rooms (what sits in the walls)

| Opening | Where | Size | Look today | Source |
|---|---|---|---|---|
| Open edge | inside rooms and maze corridors | full 3 m | nothing | `FrontRoomsMap.cs:369-372` |
| Arch (doorless doorway) | inside a zone | 1.1–1.8 m wide, off-centre, top 2.2 m (or 0.2 m under the ceiling) | bare cut, **no trim** | `FrontRoomsMapWorld.cs:975-979, 1084-1090` |
| Door | Low ↔ Standard borders | 1.0 × 2.1 m | `Door_Veneer` cube leaf 0.05 × 2.08 × 0.98, no handle; `Cove_Base` jambs and head 0.07 face, 0.20 deep; swings 95° both ways in 0.55 s (0.18 s broken) | `:1000-1039, 1937`; `images/room_map-border-door_*.jpg` |
| Window | any border with a Tall zone | 1.4 × (0.35–2.0) m | 30 mm milky pane, trims on jambs and head, no stool | `:1040-1053`; `images/room_map-border-window_*.jpg` |
| Theme border (Level 0 ↔ Office, same height) | normal edge rules | — | the wall is split in two 0.08 m halves, each in its own paper: a hard cut at the arch or open edge | `:961-969`; `images/room_map-border-theme_*.jpg` |
| Height border wall | where heights change | — | wall rises to the taller ceiling; no transition piece | `:827-828` |

Known problems: see-through slits at shut doors (R4; `research/interactables/04_door_re8_gap.md`); doors swing both ways (W6 decided single-acting); cube leaf, no hardware (F10); no transitions between levels (N1 RUNNING).

### 2.8 Room modules (Level Designer)

Four assets in `Assets/Levels/Modules/` (placed into 30 % of carved rooms they fit, `FrontRoomsMap.cs:119-123`):

| Module | Zone | Props (kit) | Lamps set | Fill | Columns |
|---|---|---|---|---|---|
| `L0_WaitingRoom_4x3` | Level 0 Standard | 3 × LadderChair, SideTableTurned, Torchiere, WallClock | 1 Dead, 1 Failing | None | None |
| `Low_Storage_2x3` | Level 0 Low | Bookcase, Crate, Pallet, PlyCabinet | Dim, Off, Failing | None | None |
| `Office_Bullpen_4x4` | Office | Copier, 2 × FilingCabinet, WaterCooler | 1 Dim | Office kit | Auto |
| `Tall_PillarHall_6x5` | Level 0 Tall | none | 10 Dead, 1 Failing | Pile | Auto |

Source: `Assets/Scripts/FrontRoomsMap/FrontRoomsRoomModuleData.cs:14-77`; props placed at `FrontRoomsMapWorld.cs:1601-1670`. Status: FIRST PASS (P1–P3 landed 2026-10-02).

### 2.9 Zone keys (visual only)

A 0.32 × 0.12 × 0.12 m cube in emissive yellow ("Map test / key", (.96, .87, .23), emission × 0.8), spinning at 1.05 m, or lying still on a module key spot (`FrontRoomsMapWorld.cs:1950-1974, 2069`). Status: PLACEHOLDER (F10, F14; R3 builds the real key, tags and hosts).

---

## 3. Title-stream rooms (`Assets/Scripts/FrontRoomsRoomStream.cs`, 1866 lines)

`RoomRule { Lobby, Shift, Office, Run, Exit }` (`Assets/Scripts/FrontRoomsRoomRule.cs:5`). The playable cycle is Shift → Office → Run → Exit → Lobby (`FrontRoomsRoomStream.cs:394-404`), after 2 empty Lobby lead rooms (`:81`). **In play only Lobby appears** (§0 point 2).

### 3.0 Shared stream room shell (all five profiles)

- **Size:** 11.5 m wide × 12 m long × 2.9 m high (`:16-18`). Walls 0.26 m thick. Rooms are joined end to end; a pool of 5 is recycled (`:15, 875-935`), with a 192 m floating-origin shift (`:52, 937-966`).
- **Shell:** floor box 0.24 thick, ceiling box 0.2 thick, two side walls, a rear seal; all with the profile's materials (`:1056-1127`). Built with flat planar boxes (`Assets/Scripts/FrontRoomsFilmMesh.cs`). No modelled T-bars or seams; the texture carries them (`:1076-1079`).
- **Baseboards:** `Cove_Base`, 0.08 × 0.16 m, centre height 0.18 m (`:1074-1075`). **It spans 0.10–0.26 m, so it floats 10 cm above the carpet** with paper showing under it (`images/room_title-lobby_detail.jpg`). This is in the start rooms of every run.
- **Fixtures:** 12 per room (§1.6). Each has a pan (`Painted_Metal`), a diffuser (`Troffer_Lens` in every profile; the code's "louvers in the Office" comment is stale since `OfficeLouver` = `Troffer_Lens`, `:1537-1541`), and an additive beam (`Resources/Lighting/VolumetricBeam.shader`, density 0.012 × level, 1.0 m long, 0.36 → 0.56 m wide).
- **Light output:** base intensity per profile × level × 0.90 (diffuse scale lerp(0.45, 1, 0.82), `:1703-1723`). `bounceIntensity` 0.82 (no GI, so it does nothing).
- **Door** (`:1209-1300`): a 2.4 m opening in a 0.04 m end wall with a 0.24 m header; two `Door_Veneer` leaves 1.12 × 2.62 × 0.08 hung at ±1.12 m; brushed-steel bar handles 0.055 × 0.18 × 0.045 at 1.22 m and kick plates 1.07 × 0.25 on both faces ("Door / brushed steel" Lit (.64, .63, .60), smoothness .62, metallic .9, `:1403`). Paper "reveal" quads hide the returns. The leaves swing 88° in 0.9 s when the camera is within 4 m (`:722-790`). Every piece has a BoxCollider, handles too (`research/interactables/01_inventory.md` §6).
- **Lights on arrival:** the room beyond a door is dark until its door opens; then each ballast strikes on its own (`:804-873`). The very first room starts lit (`:346, 1680-1701`).
- **Sound:** door creak AudioSource on each room (`:1284-1297`), a legacy fallback only when FMOD is not running (AUDIO_CONTRACT rule 3; the FMOD door Foley is timed to the 0.9 s / 88° swing). Footsteps in the start area are Carpet, dampness 0.4.
- Frames: `images/room_contact_sheet_title.jpg`.

### 3.1 Lobby (the title corridor) — IN PLAY

- **Player sees** (`images/room_title-lobby_title.jpg`, `_wide.jpg`, `_detail.jpg`, `_doorway.jpg`, `_doorway-opening.jpg`): an empty 12 m yellow room, a pair of oak double doors at the far end that open as the camera crawls up (1.15 m/s), the next room dark, then its lamps striking one by one. The logo sits over it.
- **Surfaces:** `L0_Wallpaper`, `L0_Carpet`, `L0_Ceiling`; floating `Cove_Base` baseboard.
- **Lights:** (1, .96, .88), base 5.2 (≈ 4.7 after the 0.90 scale), range 10. Odds: **2 % dead, 10 % failing** (`:1359, 1373, 1389`).
- **Props:** three outlet plates per room, 0.012 × 0.115 × 0.07 m at 0.32 m, blank (no slots), Lit (.82, .78, .66), no shadows (`:1418-1427`).
- **Fog/post:** global only.
- **Status:** FIRST PASS (the most-seen room in the game).
- **Known problems:** floating baseboard; blank outlet plates; 12 lamps per room vs 1 per cell in the map, so the start door reveals a different lamp density (Q4); side lamps light the map floor through the walls (Q5 i).

### 3.2 Shift — NOT IN PLAY

- **Player would see** (`images/room_title-shift_wide.jpg`, `_detail.jpg`, `_doorway.jpg`): the Lobby room, older and dirtier: greyer paper with heavy stains and grime, very damp carpet, green-drifting tubes, many failing.
- **Surfaces:** `L0_Wallpaper_Shift`, `L0_Carpet_Shift` (wet .85), `L0_Ceiling_Shift` (stain .55).
- **Lights:** (.86, .93, .78) green-grey, base 4.2, range 10. **10 % dead, 32 % failing**.
- **Props:** the Lobby outlets.
- **Status:** FIRST PASS look, NOT IN PLAY.
- **Known problems:** no gameplay rule or place in the map; its "aged" materials are not used by any map zone.

### 3.3 Office (stream) — NOT IN PLAY

- **Player would see** (`images/room_title-office_wide.jpg`, `_detail.jpg`, `_doorway.jpg`): the map Office inside one 11.5 × 12 m room: drywall, carpet tiles, pods, copier, vending, its own 0.9 m columns, a 2.8 m clear lane down the middle.
- **Surfaces:** Office set.
- **Lights:** (.93, .96, 1.0) cool white, base **6.0**, range 10. 3 % dead, 6 % failing. **Different from the map Office lamp** ((1, .96, .88), 5.5).
- **Props:** `FrontRoomsOfficeKit.Dress` 5-argument path (it places its own columns), seed 7919 × sequence + 104729, lane rect −1.4…+1.4 m (`:1428-1439`).
- **Grade:** none (the Office volume is map-only).
- **Status:** FIRST PASS look, NOT IN PLAY.
- **Known problems:** lamp colour and grade disagree with the map Office. `LEVEL_MODULE_SPEC.md` §11 still says the stream uses the old Codex `FrontRoomsOfficeFurniture`; that is stale (the code uses the kit).

### 3.4 Run (Level !) — NOT IN PLAY

- **Player would see** (`images/room_title-run_wide.jpg`, `_detail.jpg`, `_doorway.jpg`, `_sign.jpg`, `_props.jpg`): a wide white room washed red. Two hanging EXIT signs, a bare VCT floor, low wall rails, three teal chairs on the left, a gurney turned 14° and an IV pole on the right. Almost no tube light. **It is a room, not a corridor**, the same 11.5 × 12 m box as every profile.
- **Surfaces:** `Run_Wall` (white paint #E3E1D9), `Run_Floor` (12" VCT #DAD7CC), `Run_Ceiling` (white 2'×2' tile). Lens `Troffer_Lens` (same as Lobby).
- **Ceiling:** 2.9 m.
- **Lights:**
  - tubes: (.88, .92, .94) neutral cool, base **0.6** (≈ 0.54), range **6.5** (`:1357, 1371, 1377`). Odds **70 % dead, 45 % of the rest failing** (`:1387`). Expected: 3.6 of 12 tubes alive, about 2 steady. In the captured preview: **0 steady, 5 failing at 40 %, 7 dead** (`harness/room_capture_log.txt`, t 20.98);
  - two red point lights under the signs: (1, .10, .06), intensity 3.6, range 9.5, **soft shadows** (strength .85), always on, no flicker (`:1482-1490`). They are not in the room's lamp list (cached before the props are built, `:1118, 1125`), so the ballast model never touches them.
- **Props** (all primitive boxes and one cylinder, plain URP Lit; `:1440-1492`, materials `:1396-1402`):
  - 2 wall rails 0.07 × 0.11 m at 0.92 m, "vinyl handrail" (.72, .68, .58);
  - 3 waiting chairs at x −5.2 m, z 2.6–3.84 m: "teal vinyl seat" (.20, .36, .35), chrome legs;
  - a gurney at (3.4, 7.3) turned −14°: chrome frame (.70, .70, .68, metallic .9), "mattress" (.72, .80, .77), side rails, posts, black wheels;
  - an IV pole (cylinder 0.03 × 1.9 m) at (4.3, 8.6);
  - 2 hanging EXIT signs at z 3.4 and 9.2, 2.36 m high: housing 0.40 × 0.21 × 0.05 in `Office_BlackedGlass`, two faces 0.36 × 0.18 in `Run_ExitSign` (emission 3.2), two chrome rods 0.43 m.
- **Fog/post:** global only. The red comes from the two point lights, not from a grade.
- **Measured in the captures** (`04_captures.md` §0 point 2): mean frame value 0.283 (Lobby 0.320, Shift 0.230); its darkest 5 % sit at 0.173, the highest black level of all 17 wide frames. **The Run room reads as red-lit, not dark.**
- **Game role in code:** the old stream Relay brain (`Assets/Scripts/FrontRoomsHunter.cs:122-126`) alarms when the player enters a Run room. Only `FrontRoomsStreamVerification.cs` uses that brain; the game uses `FrontRoomsMapHunter`. So **Run has no role in the current game**.
- **Sound:** none specific. FMOD has no hard-floor footstep surface (`SoundIds.Surface { Carpet, CarpetTile, Metal }`, `Assets/Scripts/Audio/FrontRoomsSoundIds.cs:87`). VCT would need one.
- **Era:** a 1990 US hospital or institutional corridor is plausible (VCT, rails, red EXIT signs). The sign art uses Arial Bold from macOS (`gen_surfaces.py:281-299`); the period type kit (`FONTS_PERIOD_1990.md`) should supply the face.
- **Status:** PLACEHOLDER, NOT IN PLAY. **No research and no plan.**
- **History: four Run looks, none planned**

| When | Document | Run look |
|---|---|---|
| 2026-09-24 | Figma Week 1 slide "Different rooms, different rules" (2235:735) | "A red corridor. The hunter behind. Nothing to read." Tell: the lights turn red. Counter: speed, and the window you learned to break. Media: Escape the Backrooms' Level ! clip (`Research/week01/assets/clips/ETB_3_run.*`) |
| 2026-09-30 | `../Documentation/BACKROOMS_VISUAL_DIRECTION.md` (course folder) | "Red Loop / Threat": crimson wallpaper bleed, coarser dark carpet, low red pulse, retreat is the correct read |
| 2026-10-01 | `Documentation/LEVELS_AND_ENTITIES.md`, `BACKROOMS_VISUAL_SPEC.md`, `LIGHTING_SPEC.md` | utility pipes, junction boxes, warning bars, a red service cue; or red paper, dark carpet, dark red ceiling, red pulse (#D8493D, 1.15, range 6.2); entering Run always wakes the Relay |
| 2026-10-01 | `Documentation/VISUAL_RESEARCH_LOOKDEV.md` §1 "Level ! (Run For Your Life)" | the wiki's canon: a long white hospital corridor lit dim red by hanging exit signs, chairs and beds in the way. **This is the code today**, except that it is a room, not a long corridor |

- **Known problems:**
  - not reachable in play; no place in the map;
  - not a corridor (canon and Red's pitch both say corridor);
  - primitive props; the gurney reads as a table;
  - red point lights cast full soft shadows (2 extra cube shadow maps);
  - the white wall reads salmon-grey under red light: the white of the canon is lost (`images/room_title-run_wide.jpg`);
  - the doc says 55 % dead / 40 % failing; the code says 70 % / 45 %;
  - the interactables spec already reserves a Run door pair (RN-F ward door, RN-K "STAFF ONLY" utility door) and a Run window (W-RN, white enamel steel, wired glass) for when Run exists (`research/interactables/10_spec.md` table D5; `06_period_windows.md` §3.3).

### 3.5 Exit (stream "cold threshold") — NOT IN PLAY

- **Player would see** (`images/room_title-exit_wide.jpg`, `_detail.jpg`, `_doorway.jpg`, `_threshold.jpg`): the Lobby room in a cold green-blue print: grey-green paper, blue-grey damp carpet, cyan tubes, every lamp lit. A thin strip across the floor at 5.9 m.
- **Surfaces:** `Exit_Wallpaper` (cold print run), `Exit_Carpet` (tint .66/.72/.74), `L0_Ceiling`.
- **Lights:** (.62, .92, .90) cyan, base 4.4, range 10. **0 % dead, 5 % failing**.
- **Props:** a "threshold marker" 2.8 × 0.03 × 0.12 m at z 5.9, in the wall paper material (`:1493-1496`). No exit sign, no door hardware change.
- **Role:** a release beat. It loops into Lobby; it is not an ending.
- **Status:** FIRST PASS look, NOT IN PLAY.
- **Known problems:** the threshold strip is wallpaper on the floor; nothing says "exit" except colour; there is no relation to the true Exit (§5).

---

## 4. Start rooms and the title → map handoff — IN PLAY

- **What happens** (`FrontRooms3DGame.cs:538-639, 738-811`):
  1. Space: the player takes over where the title camera is. The stream ends at its first shut door (`EndStreamAt`, `FrontRoomsRoomStream.cs:503-537`).
  2. The map is placed so that the door opens onto a map cell. The map root is searched on 192 m multiples so that the 5 cells across the door are **Level 0 Standard** (`FrontRooms3DGame.cs:654-682`).
  3. The stream rooms become the map's **start area**: a 5-cell (15 m) strip with no map floor, ceiling, lamp, column, grade or furniture inside, and plain walls in the outside cells' paper around it (`FrontRoomsMapWorld.cs:456-517, 924-935`).
  4. The door stays held until the map in sight is built, at most 3 s. Map lamps near the area are held dark (`StartLampsNorth/South`, `:1229-1251`) and come up over 0.6 s with the swing.
  5. When the player is 4 m into the map, the door shuts for good; the stream lamps fade over 1.2 s; the rooms are destroyed one per frame (`FrontRoomsRoomStream.cs:546-584`).
  6. Two wall strips extend the terminal room's end wall to the start-area walls (`BuildFacade`, `:593-601`).
- **Player sees** (`images/room_start_wide.jpg`, `_doorway.jpg`, `_back.jpg`; audit `images/00_start_room_view.png`): Lobby rooms behind; through the double door the map's first Level 0 cell.
- **Visual seam at the door** (the first thing every run shows):

| | Stream (behind) | Map (ahead) |
|---|---|---|
| Paper, carpet, ceiling print | `L0_*` | the same `L0_*`, world-projected, so the print runs on (`04_captures.md` §0 point 7) |
| Wall | 0.26 m, floating baseboard | 0.16 m, no baseboard |
| Troffer | pan + textured prismatic lens + beam, 12 per 138 m² | flat white slab, 1 per 9 m² |
| Lamp | 5.2 × 0.90, own ballast model | 5.0, temperament model |
| Room | one 11.5 × 12 m box | 3 m cells, corridors |

- **Status:** FIRST PASS (handoff logic DONE 2026-10-02; visual match not done).
- **Known problems:** Q4 (make the map lamp match the stream troffer: pan, lens, beam); Q5 (stream side lamps light the map floor through walls); the start-room baseboard (§3.0).

---

## 5. The true Exit (ending) — MISSING

- No exit, goal or terminal object exists in the generated map. A run ends only on Caught (`research/interactables/01_inventory.md` §0; `FrontRooms3DGame.cs` `End`).
- The only "terminal" door is the title stream's last shut door, which leads **into** the map.
- What exists on paper:
  - the interactables spec defines an exit door pair: **EX-F** (oak leaf, clear-anodised aluminium frame, crossbar exit device, a lit wall `Kit_ExitSign` that reuses `Run_ExitSign`) and **EX-K** (locked steel leaf, unlit sign) (`research/interactables/10_spec.md` table D5, `Kit_ExitSign`);
  - the window family has an Exit member **W-EX** (clear-anodised aluminium office front) (`06_period_windows.md` §0);
  - EGRESS (Red's chosen narrative) frames the glow ink as the building's own exit plan (`research/wallpaper_motion/30_narrative_phosphor.md` §4);
  - the Week 1 deck's "THREE WAYS OUT" slide (2235:626) covers escaping the Relay (key, door, glass), not an ending.
- **Status:** MISSING (no design, no visual, no code).

---

## 6. Cross-room problems, ranked (with owners)

| # | Problem | Rooms | Evidence | Owner | Queue row |
|---|---|---|---|---|---|
| 1 | Even wash: 162° cones and 9–13 cm shadow bias erase lamp shadows; one lamp in three shadowed; directional fill leaks through ceilings | all map rooms, stream | audit F5; images 01, 34, 37 | map + visual | Q5, WG6 |
| 2 | Nothing to reflect: no probes, no sky, reflection 0.073 linear; the surface shader cannot box-project | all | audit F4; `glass/10_implementation.md` | visual (+ map call) | G6, G7 |
| 3 | Map Level 0 lens is a flat slab; stream troffer is richer; the start door shows both | Level 0 map, start | audit F10; §4 | map + visual | Q4 |
| 4 | Lamps light neighbouring rooms through walls | map, stream sides | audit F5; `MapWorld:1229-1236` | map + visual | Q5 |
| 5 | Hard cuts at zone and height borders | map | `images/room_map-border-*`; N1 | visual (plan) + map | N1 RUNNING |
| 6 | Primitive doors, keys and glass in hero positions; see-through door slits | map | F3, F10, F14; R4 | visual + map | R3, R4, R7–R9, G1–G5 RUNNING |
| 7 | No wall-level detail: no baseboards in the map, blank outlets in the stream, no decals | all | `outlets/02_code_paths.md` §0 | visual | N3 RUNNING; audit rank 10 |
| 8 | Stream baseboard floats 10 cm above the carpet | start rooms (in play), all stream profiles | `FrontRoomsRoomStream.cs:1074-1075`; `images/room_title-lobby_detail.jpg` | visual | new |
| 9 | Paper is still the CC0 chevron; ink baked into normals; NPOT maps uncompressed | Level 0 | `wallpaper_motion/10_synthesis.md` §2; webgl 03 | visual + wallpaper chat | R2, Q1b, WG7 |
| 10 | Office synthesis items not landed (opal lens, fog, lamp odds, FOV 72°) | Office | `office_and_film/10_synthesis.md` §0, §6 | visual + map | Q6 |
| 11 | Low and Tall have no visual identity beyond height | Low, Tall | §2.2, §2.3 | visual | — |
| 12 | Run, Shift, stream Office and Exit are unreachable; no true Exit | stream | §0, §5 | Red + map + visual | N2 (this workflow) |
| 13 | Lifted blacks everywhere: in 46 of 53 frames no pixel is below 0.08; a dead lamp darkens its corridor by only 23 %; a failing lamp's dropout changes the frame by 7 % | all except Office | `04_captures.md` §0 points 4–5; post lift +.035, trilight ambient, fill light | visual | — |
| 14 | ~1,000 MaterialPropertyBlocks (`_CeilingHeight` on shells, emission on lenses) break the SRP Batcher | map | webgl 00, 10 | visual + map | WG5 |

---

## 7. Frames on disk, by room

Current (2026-10-03 11:26, this workflow, `images/`, vFOV 72):

| Room | Frames |
|---|---|
| Title Lobby | `room_title-lobby_title`, `_doorway-opening`, `_doorway`, `_wide`, `_detail` |
| Start rooms | `room_start_wide`, `_doorway`, `_back` |
| Level 0 Standard | `room_map-l0-standard_wide`, `_detail` |
| Level 0 Low | `room_map-l0-low_wide`, `_detail` |
| Level 0 Tall | `room_map-l0-tall_wide`, `_detail`, `_ceiling`; pile `room_map-l0-pile_wide` |
| Office | `room_map-office_wide`, `_detail` |
| Columns | `room_map-column-l0_wide`, `_detail`; `room_map-column-office_wide`, `_detail` |
| Borders | `room_map-border-door_wide`, `_open`, `_detail`; `room_map-border-window_wide`, `_detail`; `room_map-border-theme_wide`, `_detail` |
| Lamps | `room_map-dead-lamp_wide`, `_detail`; `room_map-failing-lamp_high`, `_dropout`; `room_map-lamp-{steady,stutter,failing,dead,dim}_detail` |
| Shift / Office / Run / Exit (preview) | `room_title-shift_*`, `room_title-office_*`, `room_title-run_*` (incl. `_sign`, `_props`), `room_title-exit_*` (incl. `_threshold`) |
| Contact sheets | `room_contact_sheet_all`, `_map`, `_title` |

Older frames (still useful, but made before later changes):

| Frames | Date | Note |
|---|---|---|
| `Verification/lookdev/{0_Lobby,1_Shift,2_Office,3_Run,4_Exit}_{forward,back}.png`, `round1_sheet.jpg` | 2026-10-01 | URP upgrade round 1; before the metric grid; Office with the old furniture |
| `Verification/kit_lookdev/*.jpg` | 2026-10-02 | Office kit and piles in the look-dev hall |
| `Verification/map-*.png` | 2026-10-02 16:50–16:59 | map test scene (ceiling, Office columns, four directions) |
| `Verification/main-autopilot/*.png` | 2026-10-02 | made with `Camera.CopyFrom`: **no tonemapping, grade or grain**; do not judge the look from them (audit 05 §0 point 6) |
| `research/interaction_audit/images/*.png` | 2026-10-02 21:15 | 55 game-camera frames, seed 4242 (same map as today's captures) |
| `research/glass/images/g10_*.jpg` | 2026-10-03 | before/after glass, Level 0, Office, dead lamp |

Missing frames: the true Exit (does not exist); any Run in a map context (does not exist).

---

## 8. Documentation that disagrees with the code

| Document | What it says | What the code does |
|---|---|---|
| `VISUAL_RESEARCH_LOOKDEV.md` | tiles divide 256 m (paper 256/373, 4 ft); soft shadows on every practical; Run 55 % dead / 40 % failing; Office has louvres in places | metric 0.75 / 1.2 m on a 192 m period; stream: 4 shadowed per room, map: 1 in 3 within 9 m; Run 70 % / 45 %; all lenses `Troffer_Lens` |
| `LIGHTING_SPEC.md` | point lights, intensity ~1, range ~6, fog #1B1A17 at 0.024, red Run light #D8493D | spots 162°, intensities 0.6–8, ranges 6.5–12, fog #29261C at 0.014, Run tubes neutral + 2 red points |
| `BACKROOMS_VISUAL_SPEC.md` | Run = red paper, dark carpet, dark red ceiling; Office = beige diamond paper | Run = white paint, VCT; Office = drywall |
| `WALLPAPER_UV.md`, `BACKROOMS_WALLPAPER_RESEARCH.md` | 256 px runtime textures, per-slab material clones, 2.25 × 2.4 m repeat | 2048 × 3072 baked textures, world projection, 0.75 × 1.125 m |
| `BACKROOMS_DOOR_RESEARCH.md` | door with gasket, panel, three hinges, closer | flat veneer leaves, bar handles, kick plates |
| `TITLE_SEQUENCE.md` | pool of 3 rooms, rebase every 256 m, a glide to the next room's entry before control | pool of 5, rebase every 192 m, control in place (handoff 2026-10-02) |
| `LEVEL_MODULE_SPEC.md` §11 | the title stream still uses `FrontRoomsOfficeFurniture` | it uses `FrontRoomsOfficeKit`; `FrontRoomsOfficeFurniture.cs` and `Resources/Models/Office/*.fbx` are unreferenced Codex leftovers (W3) |
| `FrontRoomsModuleUnits.cs:83` | cove base on Office columns | cove on every column |
| `FrontRoomsRoomStream.cs:1537` comment | parabolic louvers in the Office | `OfficeLouver` returns `Troffer_Lens` |
| `gen_surfaces.py:1-5` docstring | repeats divide 256 | RenderSetup sets metric tiles |
