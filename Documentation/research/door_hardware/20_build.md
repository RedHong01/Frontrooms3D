# Door hardware build: HW-DA1 on the start double door (20)

2026-10-30 · 游戏视觉 line (account A, session local_5656cffd), workflow stage "Build + one test round + package".
Red's ask (2026-10-30): "然后我需要你research那个年代的双开门把手/门栓结构，对其进行revise". Research: `10_research.md` (same folder).
Status: **READY, not applied.** Package `W/apply/double_door_hardware.sh` (apply convention v2). Dry run on main: DRY RUN OK. Rehearsal on a scratch copy: MANIFEST OK, then ALREADY APPLIED.
`W` = `/Users/redwang/FrontRoomsVisualWork`. Clone: `W/proj_doorhw`. Work dir: `W/doorhw_work`.

## 1. The set (HW-DA1, research section 9)

A c. 1990 double-acting pair has no latch, bolt (门栓) or pull. Each face of each leaf gets a push plate and a kick plate in US32D / BHMA 630 satin stainless. The three DA spring hinges per leaf hold the door shut and stay as built.

| Part | Spec as built |
|---|---|
| Push plate, 1 per face | 4" × 16" × 0.050" = 0.1016 × 0.4064 × 0.00127 m. Vertical grain. Four 45° bevels (1.0 × 1.0 mm, 0.27 mm land) with eased edges. 0.5 mm sheared corners. Six #6 oval-head Phillips screws (Ø6.8 mm, 1.2 mm dome), 3/8" from the long edges, 1/2" from the ends and at mid-height |
| Kick plate, 1 per face | 10" × (leaf − 1-1/2") × 0.050" = 0.254 × 1.1574 × 0.00127 m. Horizontal grain. Same bevels. Ten screws in 2 rows: 1/2" from the long edges, 1" from the ends, 0.2766 m pitch (ESTIMATE) |
| Hinges | 3 DA spring hinges per leaf, unchanged |
| Not in the set | Pull, lock, latch, flush bolt, astragal, closer, exit device |

## 2. Placement (kit carrier space)

x runs from the hinge edge, with `along = −hand`. y is up from the floor. z runs from the leaf's centre plane. Each plate's origin is the centre of its back face, sitting on the leaf face at z = ±0.02225; its front faces outward.

| Part | Centre | Notes |
|---|---|---|
| Push plate | x = `along · (LeafWidth − 0.0381 − 0.0508)` = along · 1.1066; y = 1.143 (45") | Long edge 38.1 mm from the meeting edge. The right leaf's plate is turned 180° in its plane: the screw pattern is symmetric, and the wear map no longer repeats across the pair |
| Kick plate | x = along · 0.59775 (centred); y = 0.148 (bottom at 0.021, 3 mm over the leaf bottom) | Inset 19.05 mm at each end, clear of the hinge barrels. Left leaf uses `Kit_DoorKickPlate` (A half of the wear map), right leaf uses `Kit_DoorKickPlate_B` (B half) |
| Faces | `Faces.Both` (stream): near (yaw 180°) and far. `Faces.Plus` (station entrance): far (+Z, the station side) only | |

New constants in `FrontRoomsDoubleDoorKit`: `PushPlateWidth/Height/CentreY/Edge`, `KickPlateWidth/Height/Bottom`, `PlateThickness`, `PushPlateKit`, `KickPlateKit`, `KickPlateKitB`.

## 3. What changed in code (`Assets/Scripts/Rendering/FrontRoomsDoubleDoorKit.cs`, the only replaced file)

- **Removed:**
  - the two-post block pulls (the `door handle / near|far` render boxes and the pull-post cylinders in `LeafHardwareMesh`);
  - the 4 mm render kick plates.
- **Added `MountPlate`:** spawns the kit through `FrontRoomsKitLibrary.Spawn` with no colliders, no EXAMINE and shadows off. A plate 1.27 mm thick has a sub-pixel shadow. If the kit model is missing, it falls back to a bevelled slab in `Door / brushed steel`.
- **Added `ReducedTier`:** only under `#if UNITY_WEBGL || UNITY_IOS || UNITY_ANDROID`. It deactivates LOD0 and lets LOD1 take its range. Desktop and the Editor are untouched.
- **Colliders unchanged.** When the caller asks for colliders (the stream), `ColliderOnly` rebuilds the 10-29 BoxColliders with the same names, local positions, centres and sizes, but no renderer. The station (colliders false) gets none, as before.
- **Unchanged:**
  - the `BuildLeaf` / `PlaceLeaf` / `BuildFrame` signatures;
  - motion, pivots, hinges and gaps;
  - frame, saddle and brushes;
  - leaf slab and leaf material (the `door_wood_walnut` job's area).
- **Untouched:** `FrontRoomsRoomStream.cs` and `FrontRoomsStationDress.cs`. Two of their comments still say "pulls" (RoomStream:1332, StationDress:138). This is wording only, left alone so the package touches one script.

## 4. Assets (all new)

**Models.** Built by `Tools/Blender/frontrooms_kit/assets/interact_door_push_plate.py` and `interact_door_kick_plate.py`, with shared code in `interact_door_plate_common.py`. Blender 4.3 runs `build_asset.py` with hand-built LODs (`fr_lods`). Each model has a sidecar with anchors: `back_centre`, `face_centre` and `screw_N`.

| Model | LOD0 / LOD1 / LOD2 tris | Switch distances (vFOV 76) | Slots |
|---|---|---|---|
| `Kit_DoorPushPlate` | 2,238 / 18 / 10 | 1.5 / 6 / 40 m (heights 0.1734 / 0.0433 / 0.0065) | `Prop_PushPlate630`, `Prop_PlasticBlack` (countersink lips, Phillips recesses), `Prop_Stainless630` (heads) |
| `Kit_DoorKickPlate`, `_B` | 2,978 / 18 / 10 | 2.5 / 10 / 50 m (0.2963 / 0.0741 / 0.0148) | `Prop_KickPlate630` / `Prop_KickPlate630B`, `Prop_PlasticBlack`, `Prop_Stainless630` |

- **LOD0** is the hero: bevelled plate, countersunk seats, domed heads with a modelled Phillips cross (EXACT booleans). It holds at 0.3 m (capture `2_hero_03m.jpg`).
- **LOD1** is a 45°-bevelled slab, with the screws baked into the maps.
- **LOD2** is a slab.

**Materials.** All are `FrontRooms/Surface` in mesh-UV mode with base `(0.64, 0.63, 0.60)` and metallic 0.9, the same as the hinges' `Door / brushed steel`.

| Material | Maps | Settings |
|---|---|---|
| `Prop_PushPlate630` | Unique maps | `_TileSize` = 0.1016 × 0.4064 |
| `Prop_KickPlate630` | Unique maps, A half | `_TileSize` = 2.3148 × 0.254 |
| `Prop_KickPlate630B` | Same maps, B half | `_BaseMap` offset 0.5 |
| `Prop_Stainless630` | None (screw heads) | Smoothness 0.6 |

**Maps** (`Resources/Surfaces/Textures`):
- **Files:** `Prop_PushPlate630_{A,N,S}` at 512 × 2048 (0.2 mm/px) and `Prop_KickPlate630_{A,N,S}` at 4096 × 512 (two plates, 0.57 × 0.50 mm/px).
- **Generator:** `W/doorhw_work/gen_plate_maps.py` (numpy, fixed seeds).
- **Content:**
  - satin #4 grain;
  - push plate: palm-burnished oval (70 × 220 mm, smoothness 0.62 rising to 0.75), finger haze (falling to about 0.5), hairline scratches;
  - kick plate: black heel streaks in the 50–180 mm band, scuff bundles, top-edge dings, a creased corner, a mop tide line in the bottom 24 mm, smoothness about 0.50;
  - screw grime and edge grime;
  - baked heads for LOD1.
- **Bevel zone:** no grain normal and an even 0.52 smoothness. The first capture showed dotted glints on the bottom edges, and this fixed them.
- **Import:** the project importer's rules apply (BC7, repeat, trilinear, aniso 16). The WebGL and iOS tabs are capped at 1024.

## 5. Budgets

| | Before (10-29) | After, beyond LOD0 range | After, all 8 plates at LOD0 |
|---|---|---|---|
| Stream door leaves (both pivots) | 908 tris, 13 draws | 316 tris, 13 draws | about 21,000 tris, 29 draws |
| Station entrance (Plus) | 3,466 tris, 17 draws | 3,170 tris, 17 draws | about 13,500 tris, 25 draws |

Notes on the table:
- LOD0 range is ≤ 1.5 m for push plates and ≤ 2.5 m for kick plates, so only the door you stand at draws it.
- WebGL and phones never draw LOD0: at most 8 draws and 144 tris per stream door.
- Texture memory: about 12.6 MB on desktop (BC7 with mips); about 1.6 MB at the WebGL/iOS 1024 cap.
- Shadow casters: the 4 pull boxes and 4 kick boxes per door that cast shadows are gone. The plates cast none.

## 6. Test round (rules 22 and 23)

**Roslyn**, `W/tools/roslyn_check.py --all`:
- Clone tree: mac and iOS, 0 errors; Assembly-CSharp warnings stay at 33.
- Main plus the payload kit (`--override`): mac and iOS, 0 errors (`W/doorhw_work/roslyn_main_{mac,ios}`).

**FrontRoomsStreamVerification:** 5 passed, 0 failed (`W/proj_doorhw/Verification/stream-verification-latest.json`).

**Station run** (`FrontRoomsMainScenePlaytest -autopilotExit -autopilotStation 4 -autopilotSeed 2554`):
- **Result:** PASS · ended by exits; floor 4, 0 errors. Checks all ok: 15 station, 27 exit, 2 door.
- **Frame times:** 57.7 fps, p95 16.73 ms, p99 21.0 ms (`W/doorhw_work/main-autopilot_r2_pass`).
- **Fix applied:** the first attempt failed only on FMOD `BankLoadException`, because the clone had no banks. The banks were copied from `W/proj_streamdoor` (silent clone, rule 24) and the re-run passed.
- **Diagnostic baseline:** the handoff's worst frame is 295 ms. The same run on the same clone with main's kit (`W/doorhw_work/main-autopilot_base`, PASS) gives 329 ms, so the spike is not caused by this change.

**Capture** (`W/proj_doorhw/Assets/Editor/DoorHwCheck`, clone-only; before and after, game camera, seed 516574485):
- 10 frames each in `W/proj_doorhw/Verification/door_hw/{before,after}`.
- **Colliders:** `colliders.txt` is **identical** before and after: 10 BoxColliders per pose, shut, open 88° and open −60°.
- `renderers.txt` and `lods.txt` hold the budget and LOD numbers above.

**Sheet** (Shot format): `W/doorhw_work/images/double_door_hardware_before_after.jpg`. It holds before/after pairs at Red's close-up, Red's view through the door, the door shut from 3 m and the station-side entrance, plus four after-only details (hero 0.3 m, station close-up, kick plate, swung leaf). It waits for the daily VL batch.

## 7. Package

`W/apply/double_door_hardware.sh` replaces 1 file (`FrontRoomsDoubleDoorKit.cs`, keeping main's .meta) and adds 35 new paths:
- 3 models and 3 sidecars, each with a .meta;
- 4 materials, each with a .meta;
- 6 maps, each with a .meta;
- 3 generator modules.

The 16 new GUIDs are unused in main. Bases are main's state at 2026-10-30 12:2x: kit sha1 `fe28b961…`, .meta `608e128f…`.

- Dry run: `bash /Users/redwang/FrontRoomsVisualWork/apply/double_door_hardware.sh`
- Apply: `bash /Users/redwang/FrontRoomsVisualWork/apply/double_door_hardware.sh --apply`

**If main moves first:** if another job lands in `FrontRoomsDoubleDoorKit.cs` before this package (for example `door_wood_walnut`), the dry run refuses on the base. Then run `W/tools/rebase_apply.sh double_door_hardware`.

## 8. Open for Red

1. **Invisible pull colliders.** The two 51 mm pull colliders per leaf are kept, invisible, so collision is unchanged. Dropping them changes the colliders, so it needs your yes.
2. **Research section 10, not built:**
   - DA view panel (≥ 200 sq in under the 1979 UBC);
   - a fourth hinge for a 104" leaf;
   - the hinge layout;
   - the oversize leaf, which in 1990 would hang on a floor closer (this would change the pivot, so it is not done);
   - period photos and film frames, still unverified (research section 11).
