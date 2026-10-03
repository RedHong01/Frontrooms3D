# 11a — Which surfaces would show a secondary reflection: the glossy-surface inventory

2026-10-03 · visual chat (游戏视觉) · workflow glass-rt-track, secondary-reflection investigation, surfaces stage · **status: DONE** (read only; no project file changed)

**The question.** Red showed a Control RTX on/off pair: a glass cabinet reflects the room, and a glossy parquet floor reflects the cabinet (labelled "二次反射", secondary reflection). He says our window glass has none. This stage answers one part of that: **which surfaces in FrontRooms are glossy enough that a real camera would see a reflection in them**, if they were ray-traced reflection receivers or were hit by a glass reflection ray. The cause analysis and the fix are other stages of this workflow.

**Inputs, read in full:** `01_code_review.md`, `02_runtime_probe.md` (§0), `03_research.md` (§0, §3 table), `10_rt_glass_design.md`. Red's working copy, read only, at 13:00:
- every material in `Assets/Resources/Surfaces/` (86 `.mat`), and every `_S` (smoothness), `_N` and `_A` texture they use;
- `FrontRoomsRenderSetup.cs` (`SurfaceDefs`, `GlassDefs`), `FrontRoomsSurfaces.cs`, `FrontRoomsSurface.shader`;
- `FrontRoomsMapWorld.cs` `BuildMaterials` and `TransparentGlass`, `FrontRoomsRoomStream.cs` `EnsurePropMaterials` and the profile props, `FrontRooms3DGame.cs` `BuildMaterials`;
- the 54 kit sidecars `Assets/Resources/Props/Models/Kit_*.json` (slots, tags, pile palettes), `FrontRoomsOfficeKit.cs`, `FrontRoomsKitLibrary.cs`, the three room modules in `Assets/Levels/Modules/`;
- `FrontRoomsLook.cs`, the scenes' `RenderSettings`, `FrontRooms_URP_Renderer.asset`.

The glass track's clone `proj_glass` was read for `Glass_*.mat` only.

**Tags.**
- **MEASURED** = read from the asset files or computed from texture pixels by a script (no Unity was run).
- **ESTIMATE** = arithmetic or judgement, including every "real-world" value.
- **UNVERIFIED** = not confirmed.

---

## 0. For Red

1. **The large surfaces of the main game are matte, and that is physically right.** The game's map zones are Level 0 and Office. There, a real camera would see no reflection of a cabinet in any floor, wall or ceiling (MEASURED effective smoothness):
   - carpet 0.08 (Level 0) and 0.075 (Office);
   - wallpaper 0.21, drywall 0.31;
   - ceiling tile 0.10–0.11;
   - cove base and door/window frames 0.38.

   Control's glossy parquet has no counterpart: **the main game has no hard floor at all.**
2. **The only reflective floor anywhere is the Run hospital corridor in the title stream.** It is VCT tile at 0.525 mean smoothness with almost flat normals. A camera sees a **broad sheen** there: the red exit signs and any lit lens show as soft streaks, but no object shapes. Real buffed hospital VCT is glossier (ESTIMATE 0.7–0.85). Raising it would be an art decision.
3. **The surfaces that really are glossy or mirror-sharp are small, and most are dark.** They sit in the Office, in the furniture piles and in the Run room:
   - the other window panes (0.90 today, 0.96 on `Glass_Window`);
   - black glass: `Office_BlackedGlass` 0.95, and the Office interior window's pane 0.84;
   - CRT faces (0.86);
   - cabinet, hutch, clock and vending glass (0.92);
   - chrome (0.85, metal) and Run chrome (0.78, metal);
   - glazed ceramics (0.80–0.85);
   - the vending front (0.76);
   - lacquered cherry (0.71) and ebony (0.66);
   - brushed-steel door hardware (0.62, metal) and brass (0.60, metal).

   These are the only places where a secondary reflection would be visible, both inside a window's reflection and in direct view. The strongest case is **pane in pane**: a window seen in another window.
4. **Today none of these surfaces reflects the room, in the raster either.**
   - No scene has a reflection probe, the renderer has no SSR, and `FrontRoomsLook.SetZoneReflection` is still a stub in your copy (`FrontRoomsLook.cs:31-36`).
   - Every glossy surface therefore reflects only Unity's default reflection × 0.3, with no skybox material set (`FrontRooms3D.unity:29-39`).
   - G14's design makes `Glass_Window` the only traced receiver, with one bounce (`10` §1.3, §1.5). All of point 3 stays unreflective in direct view. Inside a window's reflection it is shaded flat, with only the zone cube as its own reflection.
5. **Three material values would become wrong reflections once anything is traced:**
   - `Office_BlackedGlass` (a 0.95 black mirror) is the material on the Run gurney **wheels** and the exit-sign **housings**;
   - `Troffer_Lens` and `Run_ExitSign` have no smoothness map, so they are perfect 1.0 mirrors;
   - `Kit_InteriorWindow`'s "glass" is the opaque `Prop_GlassCRT`: a dark 0.84 mirror, not a window (§6).

---

## 1. Method

**Effective smoothness.** All room and kit materials use `FrontRooms/Surface`. Its smoothness is `mask.r × _Smoothness`, where `_MaskMap` defaults to white (`FrontRoomsSurface.shader:17, 207`). Damp patches then pull it toward 0.62:
- `wet = smoothstep(.35, .85, m1.b·.7 + m2.b·.5) × _WetStrength`;
- `s = lerp(s, .62, wet)` (`:211-213`).

So:
- for a material with an `_S` map, the value that counts is the map's R channel times `_Smoothness`. MEASURED: the mean, p10, p50 and p90 of R over every pixel (`scratchpad/gloss/matscan.py`);
- for a material without one, it is `_Smoothness` itself;
- for the URP/Lit materials made at runtime (`FrontRoomsSurfaces.Lit`), it is the `smoothness` argument.

**Damp carpet.** `MacroWear_M` was sampled exactly as the shader does: floor planar frame, scales 8 m and 12.8 m, which repeat together every 64 m. The grid was 64 × 64 m at 5 cm spacing (`scratchpad/gloss/wet.py`).

**Normal roughness.** For each `_N` map: the mean tilt of the unpacked normal × `_BumpScale`, in degrees. A large tilt (carpet 10°, troffer prisms 17°) scrambles a reflection even when smoothness is high.

**Albedo.** Mean linear luminance of `_A` × the linearised `_BaseColor` (`scratchpad/gloss/albedo.py`). It decides **how strongly a reflection reads**. For a dielectric seen face-on, the reflection is about F0 = 0.04 of the reflected radiance, against a diffuse term proportional to albedo. A dark glossy surface (black glass, CRT, ebony) shows its reflection clearly. A light one (white plastic, the Run wall) shows only the lamps.

**Machine load.** Nothing in this report is a timing. For the record, `uptime` load averages were 733 / 689 / 584 at 13:00 during the material scan and 531 / 586 / 581 at 13:09.

## 2. Gloss classes: what a real camera would see

URP's GGX uses α = (1 − smoothness)². The table gives the half-width at half maximum of the reflected lobe (2 × the half-vector HWHM, computed numerically), and the blur of a reflected edge 1 m and 3 m behind the surface. The view is face-on. At grazing angles the lobe stretches into vertical streaks, which is why floors show streaks of lamps.

| Smoothness | α | Reflected HWHM | Blur at 1 m | Blur at 3 m | Class used here |
|---|---|---|---|---|---|
| 0.96 | 0.0016 | 0.12° | 0.2 cm | 0.6 cm | **Sharp** (mirror) |
| 0.90 | 0.010 | 0.74° | 1.3 cm | 3.9 cm | Sharp |
| 0.85 | 0.023 | 1.7° | 2.9 cm | 8.7 cm | Sharp |
| 0.80 | 0.040 | 3.0° | 5.2 cm | 16 cm | Sharp / glossy boundary |
| 0.70 | 0.090 | 6.7° | 12 cm | 35 cm | **Glossy**: shapes read, softened with distance |
| 0.60 | 0.160 | 12° | 21 cm | 64 cm | Glossy / sheen boundary |
| 0.50 | 0.250 | 19° | 35 cm | 104 cm | **Broad sheen**: only bright things (lit lenses, signs, a lit doorway) as soft blobs, mostly at grazing view |
| 0.40 | 0.360 | 29° | 55 cm | 165 cm | Sheen / none boundary |
| 0.30 | 0.490 | 42° | 91 cm | 274 cm | **None**: indistinguishable from diffuse |

- **Sharp:** s ≥ 0.80. A camera sees a readable image of objects.
- **Glossy:** 0.60 ≤ s < 0.80. Objects are recognisable and blurred. This matches Red's "glossy enough" line of s ≥ 0.6.
- **Broad sheen:** 0.40 ≤ s < 0.60.
- **None:** s < 0.40, or normals that scramble the image (carpet).

**Metals.** F0 is 0.5–0.95, so the reflection is nearly the whole colour. A metal at a given smoothness reads much more strongly than a dielectric.

**Reference from shipped RT.** Battlefield V traced only materials at "0.9 smoothness or higher" on Low/Medium and "0.5 smoothness or higher" on High/Ultra (`03` §3, [G3]).

**Fresnel on floors** (dielectric F0 0.04, Schlick, eye at 1.6 m). A floor point 3 m ahead is seen at 62° and reflects 8 %. At 6 m it is 75° and 25 %; at 10 m it is 81° and 45 % (ESTIMATE).

So a floor of only moderate gloss shows lamp streaks down a long corridor. That is the Run case.

## 3. Inventory by place

Columns:
- **s** = effective smoothness: mean, with [p10–p90] when a map varies it;
- **met** = metallic;
- **normal** = `_BumpScale`, then the mean tilt;
- **albedo** = linear luminance.

All s, met, normal and albedo values are MEASURED.

### 3.1 Main game map, Level 0 (also the start area and the title stream's Lobby room)

| Surface | Material (shader) | s | met | Normal | Albedo | Class |
|---|---|---|---|---|---|---|
| Floor | `L0_Carpet` (Surface) | **0.082** [0.071–0.090]. Damp patches cover 11 % of the floor, reaching at most 0.38 (p99 0.33) | 0 | 1.0, **10.4°** | 0.196 | **None** |
| Walls | `L0_Wallpaper` | 0.213 [0.19–0.24] | 0 | 0.8, 4.5° | 0.432 | None |
| Ceiling | `L0_Ceiling` | 0.103 [0.08–0.09; p99 0.42] | 0 | 1.0, 2.3° | 0.570 | None |
| Cove base, door and window frames (`trim`) | `Cove_Base` | 0.38 (no map) | 0 | none | 0.032 | None. It is dark, so at grazing view a faint lens glint is possible (ESTIMATE) |
| Door leaves | `Door_Veneer` | 0.470 [0.455–0.478] | 0 | 1.0, 3.0° | 0.187 | **Broad sheen** |
| Troffer lenses | `Map / Level 0 lens`: a copy of `Troffer_Lens` with emission × 1.5 | **1.0** (no `_S` map exists, so white) | 0 | 1.0, **16.6°** (prisms) | 0.773 | Lit: emission dominates, so no reflection reads. Dead lamp: sharp but scrambled by the prisms |
| Keys | `Map test / key` (URP/Lit, emissive) | 0.40 | 0 | — | (0.96, 0.87, 0.23) | None / sheen |
| Window panes (today) | `Map test / glass` (URP/Lit, transparent, premultiplied, α 0.28; `MapWorld.cs:2835-2853`) | **0.90** | 0 | — | — | **Sharp** |

### 3.2 Main game map, Office (also the title stream's Office room)

| Surface | Material | s | met | Normal | Albedo | Where | Class |
|---|---|---|---|---|---|---|---|
| Floor | `Office_Carpet` | **0.075** [0.071–0.078]. Damp at most 0.16 | 0 | 0.8, 3.3° | 0.102 | every Office room | **None** |
| Walls, columns | `Office_Wall` | 0.309 [0.302–0.325] | 0 | 0.8, 2.9° | 0.427 | walls, `office column` | None |
| Ceiling | `Office_Ceiling` | 0.110 | 0 | 1.0, 2.5° | 0.589 | | None |
| Lenses | `Troffer_Lens` (the Office shares it; `FrontRoomsSurfaces.cs:43-48`) | 1.0 | 0 | 1.0, 16.6° | 0.773 | every fixture | as Level 0 |
| Cubicle panels | `Prop_FabricCubicle` | 0.139 | 0 | 1.0, 8.1° | 0.123 | `Kit_CubiclePanel*` | None |
| Desk worksurface | `Prop_LaminateBeige` | 0.43 | 0 | none | 0.377 | `Kit_OfficeDesk` | Broad sheen (weak: the light colour washes it out) |
| Painted steel | `Prop_SteelPutty` / `SteelBrown` / `SteelBlack` | 0.432 [0.40–0.46] | 0 (painted) | 1.0, 3.3° | 0.348 / 0.030 / 0.013 | desk frame, panels, posts, filing cabinet, PC, interior window frame | Broad sheen. On the dark brown and black versions the sheen reads |
| **CRT faces** | `Prop_ScreenCRT` | **0.79**: face 0.86 (p50–p90), bezel area 0.47 (p10) | 0 | — | **0.025** | `Kit_CRTMonitor` (every desk station), `Kit_CRTTV` (piles) | **Sharp**. Dark, so the reflection reads strongly |
| Beige, black and grey plastics | `Prop_PlasticBeige` / `Black` / `Grey` | 0.516 / 0.448 / 0.449 | 0 | 1.0, 4.3° | 0.587 / 0.010 / 0.111 | monitors, PC, keyboard, copier, bins | Broad sheen |
| Keycaps | `Prop_KeyboardKeys`, `Prop_PhoneKeys` | 0.47, 0.51 | 0 | 1.0, 5.8° / 3.1° | 0.38 / 0.35 | | Broad sheen (tiny) |
| Phone display | `Prop_GlassCRT` | 0.84 | 0 | — | 0.006 | `Kit_DeskPhone` | Sharp (tiny) |
| **Interior window pane** | `Prop_GlassCRT` (opaque Surface) | **0.84** | 0 | — | **0.006** | `Kit_InteriorWindow`: Office rooms with ceiling ≥ 2.6 m, 50 % chance (`FrontRoomsOfficeKit.cs:405`) | **Sharp**: a near-black mirror (see §6) |
| **Task chair base** | `Prop_Chrome` | **0.85** | **1** | — | 0.687 (F0) | `Kit_TaskChair` at every station | **Sharp, metal** |
| Filing cabinet trim | `Prop_Aluminium` | 0.562 [0.53–0.59] | **1** | 1.0, 0.5° | 0.477 | `Kit_FilingCabinet` (Office kit; `Low_Storage`, `Office_Bullpen` modules) | Broad sheen, but metal, so it reads strongly |
| Vending front | `Prop_VendingFront` (emissive) | 0.761 [0.67–0.82] | 0 | — | 0.181 | `Kit_VendingMachine` | Glossy. On the lit panel, emission dominates |
| Vending window, wall-clock glass | `Prop_Glass` (URP/Lit, transparent, α 0.16) | 0.92 | 0 | — | — | `Kit_VendingMachine`, `Kit_WallClock` | Sharp |
| Copier panel | `Prop_CopierPanel` | 0.502 | 0 | 1.0, 1.9° | 0.421 | `Kit_Copier` | Broad sheen |
| Water-cooler bottle | `Prop_BottleBlue` (URP/Lit, transparent, α 0.42) | 0.90 | 0 | — | — | `Kit_WaterCooler` (also the `L0_WaitingRoom` module) | Sharp |
| Red and white clock/cooler parts | `Prop_PlasticRed`, `Prop_PlasticWhite` | 0.50, 0.516 | 0 | | 0.060 / 0.666 | | Broad sheen |

### 3.3 Furniture piles (halls of ≥ 4 × 4 cells, chance 0.35; `FrontRoomsLevelProfile.cs:52`) and room-module props

The pile palettes come from the kit sidecars: domestic70s, office90s and storage.

| Surface | Material | s | met | Normal | Albedo | Kits | Class |
|---|---|---|---|---|---|---|---|
| **Cabinet and hutch glass** (Control's cabinet) | `Prop_Glass` | **0.92** | 0 | — | — | `Kit_DisplayCabinet`, `Kit_Hutch`, `Kit_Hutch_Cherry` | **Sharp** |
| Lacquered cherry | `Prop_WoodCherry` | **0.713** [0.68–0.74] | 0 | 1.0, 0.4° | **0.038** | Credenza, DeskPedestal, DresserLow, Hutch_Cherry, RollingCabinet | **Glossy**. Dark, so it reads |
| Ebony | `Prop_WoodEbony` | **0.663** [0.64–0.69] | 0 | 1.0, 1.1° | **0.008** | Hutch | **Glossy**. Near black, so it reads strongly |
| Dark wood | `Prop_WoodDark` | 0.587 [0.565–0.608]; 26 % of texels ≥ 0.6 | 0 | 1.0, 1.1° | 0.015 | sofa and club-chair frames, HatStand | Broad sheen, close to glossy |
| Oak, teak, walnut | `Prop_WoodOak` / `Teak` / `Walnut` | 0.559 / 0.528 / 0.509 | 0 | 1.0, 1.2–1.9° | 0.175 / 0.145 / 0.037 | Bookcase, DisplayCabinet, chairs, Dresser70s, side table | Broad sheen |
| **Brass hardware** | `Prop_Brass` | **0.60** | **1** | — | 0.279 (F0) | 11 kits (pulls, rails) | **Glossy, metal** |
| **Chrome** | `Prop_Chrome` | **0.85** | **1** | — | 0.687 | BarStool, PlyCabinet, TaskChair | **Sharp, metal** |
| Urn | `Prop_Ceramic` | **0.80** | 0 | — | 0.053 | Kit_Urn | **Sharp** (dark glaze) |
| Lamp base | `Prop_CeramicGlaze` | 0.85 | 0 | — | 0.690 | TableLampPleated | Sharp but light: shows only lamps and dark objects |
| Binders | `Prop_Vinyl` | 0.403 | 0 | 1.0, 4.5° | 0.007 | Binders | Sheen / none boundary |
| Everything else | hardboard 0.24, chipboard 0.24, plywood 0.29, pine 0.21, studs 0.21, tape 0.30, label 0.30, paper 0.24, cardboard 0.19, backer 0.19, fabrics 0.13–0.17, velvet 0.26, rubber 0.15, lamp shade 0.10, stencil 0.20 | | 0 | | | | **None** |

### 3.4 Title stream (`FrontRoomsRoomStream`): Shift, Exit, Run, and the doors

| Surface | Material | s | met | Normal | Albedo | Where | Class |
|---|---|---|---|---|---|---|---|
| Shift floor | `L0_Carpet_Shift` (`_WetStrength` 0.85) | 0.082 dry. Damp up to **0.54**: s ≥ 0.4 on 2.8 % of the floor, ≥ 0.5 on 0.8 % | 0 | 1.0, 10.4° (flattened × 0.7·wet in damp patches) | 0.156 | Shift rooms | None, except a faint lamp sheen in the wettest patches |
| Exit floor | `Exit_Carpet` | 0.082. Damp at most 0.25 | 0 | 1.0, 10.4° | 0.089 | Exit rooms | None |
| Exit floor strip | "exit threshold marker", drawn in the wall material `Exit_Wallpaper` (`RoomStream.cs:1495`, 1514) | 0.213 | 0 | 0.8, 4.5° | 0.401 | 2.8 × 0.12 m strip | None |
| **Run floor** | `Run_Floor` (VCT) | **0.525** [0.459–0.584] | 0 | 1.0, **0.8°** (flat) | 0.652 | the Run hospital corridor | **Broad sheen**: the only reflective floor in the game. Red exit signs and lit lenses show as soft streaks at grazing view |
| Run walls | `Run_Wall` | 0.579 [0.573–0.588] | 0 | 0.6, 2.1° | 0.737 | Run corridor | Broad sheen (semi-gloss paint; light colour, so only the signs read) |
| Run ceiling | `Run_Ceiling` | 0.110 | 0 | | 0.644 | | None |
| **Run chrome** | `Run / chrome` (URP/Lit) | **0.78** | **0.9** | — | (0.70, 0.70, 0.68) | chair legs, gurney frame, rails and posts, IV pole, exit-sign rods | **Glossy to sharp, metal** |
| Run seat, handrail, mattress | `Run / teal vinyl seat` / `vinyl handrail` / `mattress` | 0.42 / 0.48 / 0.25 | 0 | — | | | Sheen / sheen / none |
| **Gurney wheels, exit-sign housings** | `Office_BlackedGlass` (`RoomStream.cs:1397, 1467, 1477`) | **0.95** | 0 | — | **0.002** | Run room | **Sharp black mirror**. Wrong for rubber and plastic (§6) |
| Exit-sign faces | `Run_ExitSign` (emissive 3.2) | **1.0** (no `_S` map) | 0 | — | 0.663 | Run room | Emission dominates |
| Troffer pans | `Painted_Metal` (dielectric) | 0.523 [0.50–0.54] | 0 | 1.0, 2.9° | 0.611 | title-room recessed pans | Broad sheen |
| Door leaves | `Door_Veneer` | 0.47 | 0 | 1.0, 3.0° | 0.187 | title doors (the title corridor's `darkMat` too) | Broad sheen |
| **Door handles, kick plates** | `Door / brushed steel` (URP/Lit) | **0.62** | **0.9** | — | (0.64, 0.63, 0.60) | title double doors (`RoomStream.cs:1212, 1260-1270`) | **Glossy, metal** (real brushed steel is anisotropic; URP is isotropic) |
| Baseboards | `Cove_Base` | 0.38 | 0 | | 0.032 | | None |
| Outlet plates | `Level 0 / outlet plate` (URP/Lit) | 0.35 | 0 | | | Lobby, Shift and Exit rooms | None |

**There are no steel door leaves anywhere in the game.**
- Map doors are a veneer leaf (0.47) with `Cove_Base` jambs (0.38).
- The only steel on any door is the title stream's brushed-steel handles and kick plates (0.62, metallic 0.9).

### 3.5 Glass, every variant

| Material | Shader | s | Where | Class |
|---|---|---|---|---|
| `Map test / glass` | URP/Lit transparent, premultiplied, α 0.28 | 0.90 | every map window in Red's copy (`MapWorld.cs:1142`) | Sharp |
| `Glass_Window` (`proj_glass`) | FrontRooms/Glass, `_PaneF0` 0.08, `_RTReceive` 1 | **0.96**; smudges 0.62; edges 0.60 | planned panes (contract C8 in `10` §2.4); the only G14 receiver | Sharp (mirror) |
| `Glass_ShardClear` (`proj_glass`) | FrontRooms/Glass, `_RTReceive` 0 | 0.94 | planned fracture pieces | Sharp |
| `Glass_Edge` (`proj_glass`) | URP/Lit opaque | 0.60 | pane edges | Glossy |
| `Glass_Shard` (`proj_glass`) | URP/Lit opaque | 0.95 | planned floor shards | Sharp |
| `Prop_Glass` | URP/Lit transparent, α 0.16 | 0.92 | cabinet, hutch, vending and clock glass | Sharp |
| `Prop_BottleBlue` | URP/Lit transparent, α 0.42 | 0.90 | water cooler | Sharp |
| `Prop_GlassCRT` | Surface, opaque | 0.84 | interior-window pane, phone display | Sharp |
| `Office_BlackedGlass` | Surface, opaque | 0.95 | used only on Run gurney wheels and sign housings | Sharp |

### 3.6 Defined but not placed today (no caller, or no kit slot uses them)

| Material | s / met | Why unused |
|---|---|---|
| `Office_Louver` | 0.756 [0.66–0.82] / 0.85, emissive | `ParabolicLouver` has no callers. Office lenses now use `Troffer_Lens` (`FrontRoomsSurfaces.cs:43-49`) |
| `Office_Fabric` | 0.078 | `CubicleFabric` is used only by `FrontRoomsOfficeFurniture`, which nothing calls |
| `FrontRoomsOfficeFurniture` runtime set | CRT glass 0.86, dead screen 0.66, painted steel 0.46 / 0.72, vinyl 0.58, wood 0.28–0.34 | class unreferenced |
| `Office / putty steel` | 0.45 / 0.55 | created in `EnsurePropMaterials`, never assigned |
| `Prop_WoodLaminate` 0.624, `Prop_LCD` 0.60, `Prop_LED*` 0.70, `Prop_PVCEdge` 0.55, `Prop_VendingHeader` 0.55, `Prop_ScreenCRT_On` 0.79, `Prop_PlasticPutty` 0.52, `Prop_PlasticBlue` 0.50, `Prop_SteelAlmond` 0.43, `Prop_BulbLit` 0.30, `Prop_FoamPU` 0.25, `Prop_LampShadeLit` 0.10 | as listed | no kit sidecar slot and no FBX names them (round-1 requests not yet modelled) |

## 4. Summary

| Class | Surfaces (s; m = metal) | Where they appear |
|---|---|---|
| **Sharp** (s ≥ 0.80, or emissive at 1.0) | window panes 0.90 now / 0.96 `Glass_Window`; `Office_BlackedGlass` 0.95; `Glass_Shard` 0.95; `Prop_Glass` 0.92; `Prop_BottleBlue` 0.90; CRT face 0.86 (`Prop_ScreenCRT` mean 0.79); `Prop_Chrome` 0.85 m; `Prop_CeramicGlaze` 0.85; `Prop_GlassCRT` 0.84; `Prop_Ceramic` 0.80; lenses and exit signs 1.0 (emission-dominated) | every map window; Office stations (CRTs, chairs); interior windows; furniture-pile cabinets and hutches; vending, clock and cooler; Run wheels and sign housings |
| **Glossy** (0.60–0.80) | `Run / chrome` 0.78 m; `Prop_VendingFront` 0.76; `Prop_WoodCherry` 0.71; `Prop_WoodEbony` 0.66; `Door / brushed steel` 0.62 m; `Prop_Brass` 0.60 m; `Glass_Edge` 0.60 | Run props; vending; pile case goods; title door hardware; brass pulls |
| **Broad sheen** (0.40–0.60) | `Prop_WoodDark` 0.59; `Run_Wall` 0.58; `Prop_Aluminium` 0.56 m; `Prop_WoodOak` 0.56; `Prop_WoodTeak` 0.53; **`Run_Floor` 0.525**; `Painted_Metal` 0.52; plastics 0.45–0.52; `Prop_WoodWalnut` 0.51; `Prop_CopierPanel` 0.50; `Door_Veneer` 0.47; keycaps 0.47–0.51; Run handrail 0.48; painted steel 0.43; `Prop_LaminateBeige` 0.43; Run seat 0.42; `Prop_Vinyl` 0.40; key 0.40 | Run floor and walls; every door leaf; desks; filing cabinets; pile wood; title pans |
| **None** (< 0.40, or scrambled normals) | `L0_Carpet` 0.08 (damp ≤ 0.38); `L0_Carpet_Shift` 0.08 (damp ≤ 0.54 on < 1 %); `Exit_Carpet` 0.08; `Office_Carpet` 0.075; wallpapers 0.21; `Office_Wall` 0.31; ceilings 0.10–0.11; `Cove_Base` 0.38; outlet 0.35; fabrics 0.13–0.17; board, paper and cardboard 0.19–0.29; rubber 0.15; Relay 0.12 | every floor, wall and ceiling of Level 0 and Office; the Exit strip; trims |

## 5. What this means for the secondary-reflection work (handoff, ESTIMATE)

1. **"No secondary reflection" in the windows is not a material problem for walls and floors.** Inside a window's reflection, the room behind you in Level 0 or Office is carpet, wallpaper or drywall, and ceiling tile. A single-bounce trace is physically right for those hits: a real camera sees no reflection in them.

   What a window should show as a second bounce is limited to the sharp and glossy rows of §4:
   - another pane;
   - a black interior window;
   - CRT faces;
   - chrome chair bases;
   - cabinet glass;
   - cherry and ebony case goods;
   - brass.

   G14 §1.5 shades those hits flat, and shades another pane as see-through plus the zone cube.
2. **Control's floor shot is a primary reflection on the floor, not something in the glass.** In FrontRooms the matching case exists only on the Run VCT floor (broad sheen), and in direct view of the glossy props of point 1. None of these is a G14 receiver. The raster gives them no room reflection either: no probe, no SSR, and the zone cube stubbed.
3. **Threshold proposal for the fix stages.**
   - Recurse, meaning a second reflection ray from the hit, for hits with s ≥ 0.6, or metallic ≥ 0.5 and s ≥ 0.5.
   - Use the zone cube for 0.4 ≤ s < 0.6.
   - Use nothing below 0.4.

   This lines up with BFV's High/Ultra cut-off of 0.5 [G3] while staying physical.
4. **Cost scales with the glossy share of what a window sees.** In Level 0 and Office rooms the glossy surfaces are small objects. Recursive rays would be spawned on a small share of glass pixels (ESTIMATE ≤ 5–15 % in Office, ≤ 5 % in Level 0 halls without a pile). The rest is pane-in-pane, which only happens where two windows face each other. Measuring this needs the bench (`rt_bench2`), not this inventory.
5. **Primary receivers beyond glass, if Red wants Control's floor look at the top spec**, in order of visual payoff (ESTIMATE):
   1. CRT faces, interior-window panes and `BlackedGlass`: dark, sharp, and in the Office at eye level.
   2. Chrome and brass.
   3. Cabinet, hutch and vending glass (needs the same see-through treatment as panes).
   4. Cherry and ebony.
   5. The Run VCT floor and walls, as a rough-reflection tier needing a blur or several rays at s 0.5.

## 6. Material values to check before anything traces them (owner: lookdev / map chat; nothing changed here)

| # | Value | Why it matters for reflections | Suggested |
|---|---|---|---|
| M1 | `Office_BlackedGlass` (0.95, albedo 0.002) on Run gurney **wheels** and exit-sign **housings** (`RoomStream.cs:1467, 1477`) | Traced, the wheels become black mirrors | Wheels on `Prop_Rubber` (0.15). Housings on a black plastic at about 0.45 (`Prop_PlasticBlack`) |
| M2 | `Troffer_Lens`, `Run_ExitSign`: no `_S` map, so s = 1.0 | Lit, it is invisible under the emission. A **dead** lens (Run 70 %, Shift 10 % dead; `RoomStream.cs:1385-1389`) becomes a perfect prism mirror | Add a mask, or set `_Smoothness` to about 0.85 for clear prismatic acrylic (ESTIMATE) |
| M3 | `Kit_InteriorWindow` pane = `Prop_GlassCRT`, opaque | A window that is really an opaque dark mirror. It is also not a receiver (no `_RTReceive`), so G14 skips the most mirror-like surface in the Office | Move the pane to `Glass_Window` (see-through, a receiver), or flag it as a receiver |
| M4 | `Run_Floor` 0.525 | Real buffed hospital VCT reads as a glossy floor with lamp streaks (ESTIMATE 0.7–0.85) | An art decision for Red. It is the one place a Control-like floor reflection fits the fiction |
| M5 | `Prop_Glass` is URP/Lit transparent, not FrontRooms/Glass | It cannot take the `_FR_GLASS_RT` hook; cabinet glass would stay on the environment term | Move it to FrontRooms/Glass if cabinets become receivers |

## 7. Reproduce

The scripts are in `scratchpad/gloss/`. They use Xcode's `/usr/bin/python3`, which has numpy and PIL.
- `matscan.py` parses every `.mat` and gives the `_S` R-channel statistics and the `_N` tilt. Output: `mats.json`.
- `wet.py` gives the damp-patch smoothness over a 64 m floor.
- `albedo.py` gives the mean linear albedo.

The scripts run no Unity and change no project file.

## 8. Sources (Red's working copy, read only, 13:00)

- `Assets/Resources/Rendering/FrontRoomsSurface.shader:13-37` (properties), `:207` (smoothness = mask.r × `_Smoothness`), `:211-214` (damp patches).
- `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs:269-370` (`SurfaceDefs`), `:373-377` (`GlassDefs`), `:448-458` (masks are `<stem>_S`, white when missing).
- `Assets/Scripts/Rendering/FrontRoomsSurfaces.cs:20-52, 85-101`.
- `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs:1089-1143` (trim, door leaf, pane), `:2793-2853` (`BuildMaterials`, `TransparentGlass`). The map chat is editing this file, so line numbers move.
- `Assets/Scripts/FrontRoomsRoomStream.cs:1074-1075, 1089, 1212-1270, 1393-1404, 1418-1496, 1512-1514`.
- `Assets/Scripts/FrontRooms3DGame.cs:297, 373-402`.
- `Assets/Scripts/Office/FrontRoomsOfficeKit.cs:38-58, 381-382, 405, 474-487`; `FrontRoomsKitLibrary.cs:211-237`.
- `Assets/Resources/Props/Models/Kit_*.json` (54 sidecars: `slots`, `tags`, `pile.palette`); `Assets/Levels/Modules/*.asset`.
- `Assets/Scripts/Rendering/FrontRoomsLook.cs:22, 31-36, 46`; `Assets/Scenes/FrontRooms3D.unity:29-39` (no skybox, Skybox reflection mode, intensity 0.3; the cube's content is UNVERIFIED); `Assets/Settings/FrontRooms_URP_Renderer.asset` (features: SSAO and the RT feature only); 0 `ReflectionProbe` components in the three scenes and none added by script.
- `proj_glass/Assets/Resources/Surfaces/Glass_Window.mat`, `Glass_ShardClear.mat`, `Glass_Edge.mat`, `Glass_Shard.mat`.
- `03_research.md` §3 [G3] (BFV smoothness thresholds).
