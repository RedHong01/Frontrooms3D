# D1.6 doors · locks · keys — build record (stage: Build, workflow d16-doors-locks-keys, 游戏视觉)

2026-10-22. The account-A run of this stage (session local_5656cffd) started at 15:10 and died at 16:12 without returning. This record is from the re-run on account B (session local_a9a1dce2), 16:47–17:36, which continued from that partial work (it rebuilt nothing that was finished). Design: `10_design.md`. A12 facts and gap numbers: `05_for_interaction.md`.

## 0. Status

**READY (visual package) + a map contract for 关卡设计.**

- **Package** `W/apply/d16.sh` (apply convention v2): 119 paths, 118 new and 1 replaced (`FrontRoomsRenderSetup.cs`, 3 SurfaceDefs lines), 7 kept `.meta`. `DRY RUN OK` on Red's main HEAD `aca6240b` + working tree (17:0x). `rebase_apply.sh d16 --no-install --ios`: PASS (nothing to rebase). Roslyn `--all`, main + payload: COMPILE OK on macOS and iOS defines; main + payload + map contract: COMPILE OK on both. Scratch rehearsal: `--apply` MANIFEST OK, then ALREADY APPLIED (`W/tools/_test/d16_1022/t_apply_script_tests.log`).
  - Apply: `bash /Users/redwang/FrontRoomsVisualWork/apply/d16.sh --apply` (dry run first: `bash /Users/redwang/FrontRoomsVisualWork/apply/d16.sh`). On a base mismatch: `W/tools/rebase_apply.sh d16` (RenderSetup is shared with `outlets_p4b` and `w2_office_props`).
  - With the package alone, nothing calls `DressDoor` or `DressKey`: doors and keys stay the map's cubes. The shot look (V4) and the shot light (V5) are live at once (they install themselves at load and read `FrontRoomsDoorShot.Weight` / `FrontRoomsGlassShot.Weight`).
- **Map contract** `data/contract_map_D16.diff` (M1, M3, M4, M7, M9; lines added only) against MapWorld `f742f592`, DoorShot `b2014396`, ModuleEditing `76c3de75`, with an idempotent anchor applier `data/apply_map_hunks_v2.py.txt`. Map-owned: 关卡设计 applies it after the package. The doors and keys show in game only then.
- Lands now (Red 15:02: no Alpha gate; Red 14:59: integrate the doors).

## 1. Clone and bases

- Clone `W/proj_d16`: made by `mkclone.sh` at 08:25, resynced 15:12 by the account-A stage, and resynced again **16:56** by this stage from main `aca6240b` + working tree (rsync of the five folders **without** `--delete`, so the D1.6 files stayed; `apply_local_patches.py`; `install_tools.py`, test bundle id `com.redwang.frontroomsss.test.d16`). The D1.6 files were saved first to `W/d16_work/payload_1650/`.
- MapWorld moved from `9b82c286` (the account-A contract's base) to `83c19911` (C22, V1). The contract was re-applied with a new anchor applier (`W/d16_work/contract/apply_map_hunks_v2.py`): M1 now sits after C22's EXIT block, and the EXIT door is skipped (§4). DoorShot (`b2014396`), CameraRig, ShotTimings, GlassShot, KitSeam, KitLibrary and RenderSetup are unchanged since the account-A run. At 17:36 MapWorld moved once more (`f742f592`, 关卡设计 17:12: a read-only `InStairwell` accessor for 声音设计); the applier re-ran clean on it and the contract was rewritten against it (Roslyn main + payload + contract OK, macOS and iOS).
- Bases at package time (all re-checked by sha1): RenderSetup `256cf3a4` (only replaced path); MapWorld `f742f592`, DoorShot `b2014396`, ModuleEditing `76c3de75` (contract); KitSeam `dba67c53`, Game `fc76109d`, CameraRig `a77221fb`, ShotTimings `865e3e10`, GlassShot `7f3c2929`, KitLibrary `1f7dac20` (read, not touched).
- Compile in the clone: Unity batch `-nographics -quit -buildTarget OSXUniversal` 0 `error CS` (`W/logs/d16/b05_compile.log`, 16:57); every later batch run 0 too.

## 2. What was built (visual-owned; sha1 = first 8 of the payload)

| Path | sha1 | What |
|---|---|---|
| `Assets/Scripts/Rendering/Doors/FrontRoomsDoorRig.cs` | `8b90716c` | `IFrontRoomsDoorRig` on "Leaf rig": the 9 seam members + `DetachStrike`, `ForceLod0`, `Nearest`, `All`; keyhole on both faces through the map's `FaceOf`; plug / knob / lever / bolts on two channels (shot, beats); `Jolt` about the pivot toward −swing·X; the collider follow (crooked hang, rattle, blows); 0 B per tick |
| `…/Doors/FrontRoomsDoorRigDriver.cs` | `99c15ab7` | the one hidden LateUpdate host; frozen while `map.DoorsPaused`; taken down on `Application.quitting` (DontSave objects outlive Play Mode in the Editor) |
| `…/Doors/FrontRoomsDoorDressEvents.cs` | `25d206fe` | beats on the map's existing events: lever / knob on `DoorHandleTurned`, rattle, latch click on `DoorLatched`, strike detach on `DoorBrokenFrom`, wobble on `DoorBrokenTried`, deadbolt drawn for the run on `DoorUnlocked` |
| `Assets/Scripts/Rendering/Keys/FrontRoomsKeyAssembly.cs` | `ac4137c4` | `IFrontRoomsKeyRig` on "Key · zone {id}"; hung / flat / over-edge rest; `PoseHeld` from `held_arrangement.json`; the HeldItem switch at 1.5 × HeldDistance (0.525 m); the look+E trigger **on the root** |
| `…/Keys/FrontRoomsKeyIdentity.cs` | `151f3f33` | shape / tint / number; `Stub(zone)` = the HUD's stub hash |
| `Assets/Scripts/Rendering/HeldItem/FrontRoomsHeldItem.cs` | `9904eaee` | V3: layer 30, a pass after opaques (depth punch to far, then redraw), injected per camera only while something is on the layer; off on WebGL in code |
| `Assets/Scripts/Rendering/Shots/FrontRoomsShotLook.cs` (+ `Host`) | `e5fbd816` (`579e509d`) | V4 unlock (vignette +0.10, exposure +0.15, Gaussian far blur where DOF is supported) and glass (vignette +0.08, CA pulse on the shatter, none with Reduce Flashing); LOD0 on the shot door; V5 0.6 shot light (not on WebGL); volumes, profiles and light taken down on `Application.quitting`, so a volume left at a shot's weight never tints the next session |
| `Assets/Scripts/Office/FrontRoomsInteractableKit.Door.cs` | `9c2f0505` | `DressDoor` (member by theme, LOCKED by `LockedByRule`, the parts table as data, all-or-nothing, cube renderer off, collider untouched; skips the EXIT door); dormant `DoorActing.Single` |
| `Assets/Scripts/Office/FrontRoomsInteractableKit.Key.cs` | `9d30416d` | `DressKey`, `DressKeyHost`, `DressShotKey` |
| `Assets/Editor/Rendering/FrontRoomsDoorRigTests.cs` | `b07114d3` | 65 checks (§3) |
| `Assets/Editor/Rendering/FrontRoomsKeyAssemblyTests.cs` | `b5c76eab` | 26 checks (§3) |
| `Assets/Resources/Rendering/FrontRoomsHeldItemDepthPunch.shader` | `b4aea56a` | ColorMask 0, ZTest Always, depth = far (reversed-Z aware) |
| `Assets/Resources/Surfaces/Door_Enamel.mat` | `83371ba5` | RD10: PaintedMetal textures, calibrated almond tint (target sRGB 205/197/176, luminance 0.56), smoothness 0.50 |
| `…/Prop_KeyTagNo.mat` + `Textures/Prop_KeyTagNo_A/_S.png` | `4da0ad0f` + `5969430e` / `e53adce9` | 4096² 10 × 10 atlas "00"–"99", Courier Prime Bold #1E1D1A, digit ink 0.239–0.242 of a cell (G12); WebGL import tab 2048 (overridden), desktop unchanged |
| `…/Prop_SignEngraved.mat` + `_A/_N/_S.png` | `4a93d1e4` + `8a03675f` / `20d3126c` / `fd502e3f` | 2048 × 1024 2 × 2 atlas: EMPLOYEES ONLY, STAFF ONLY, STORAGE, blank; TeX Gyre Heros Bold, cap 17.1 mm, ivory #E9E1C9 on brown 48/36/30 (proposal for 平面视觉) |
| `Assets/Resources/Props/Models/` 15 `_DA` kits (FBX + JSON, + metas) | `package_d16_expected.txt` | frames Wood / Steel / Steel_Alu, leaves Veneer / Veneer_Oak / Steel / Steel_PaintedMetal / SteelLite / Ward, latchbolts Bored / Bored_Brass / Mortise, strikes Bored / Bored_Brass / Mortise; tag `double_acting`; LOD0/1/2 |
| `Tools/Blender/frontrooms_kit/assets/` 12 `_da` modules | idem | the sources; `kitlib.py`, `build_asset.py` and every single-acting module untouched (main's 9 stop kits and their GUIDs stay for the dormant mode) |
| `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs` (replaced) | `406a568f` (base `256cf3a4`) | 3 SurfaceDefs lines, nothing else |

Full list with sha1s: `data/package_d16_expected.txt` (= `W/apply/d16_expected.txt`). New GUIDs: 55, none used in main (checked at dry run).

Kit triangles (LOD0 / LOD1 / LOD2, from the sidecars): Wood_DA frame 1,736 / 952 / 264; Steel_DA and Steel_Alu_DA frames 2,046 / 740 / 356; Veneer_DA leaf 2,742 / 240 / 40 (Oak 2,748); Steel_DA leaf 4,974 / 264 / 64; SteelLite_DA 5,624 / 424 / 100; Ward_DA 4,142 / 264 / 64; DA latches 500–508 / 80 / 20; DA strikes 730–792 / 166–226 / 30–32. Per door at LOD0 (edit-mode count, `FrontRoomsDoorRigTests`): LOCKED (L0-K = OF-K parts) 36,656 tris, 18 renderers, 17 LOD groups; FREE L0-F 19,132 tris, 9 renderers.

## 3. Tests (clone, edit mode unless marked)

| Suite | Result | Notes |
|---|---|---|
| `FrontRoomsDoorRigTests` (new) | **65 / 65** | hierarchy, cube renderer off, collider untouched, no collider or forbidden name; keyhole ±0.0315 / spindle point ±0.0385 at Y 1.000, Z 0.920 both faces (world within 1 mm: float ulp 0.49 mm at the test roots' 4.7 km; in the rig's own space within 0.01 mm); keyway and up; LockPose and FaceOf through the rig; plug, knob, lever signs; deadbolt 0.025, latch 0.019 / 0.013; Jolt toward −swing for swing ±1 and home; collider follow; 240 ticks 0 B; strike detach; cost |
| `FrontRoomsKeyAssemblyTests` (new) | **26 / 26** | stub = HUD hash (5 zones); trigger on the root; no part collider; HeldItem layer at 0.50 m, not at 1.0 m; held parts 0.000 mm / 0.000° from `held_arrangement.json`; 240 held frames 0 B; Release; PoseInLock; hung tip down |
| `FrontRoomsDoorShotTests` (map's) | plain **245 / 245**; doors dressed **245 / 245**; doors + keys dressed 243 / 245 | the 2 are its "no model" key tests (20, 21), which by design see a model once keys are dressed |
| `FrontRoomsCameraRigTests` | 33 / 33 in all three variants | |
| `FrontRoomsGlassShotTests` | 20 / 20 in all three variants | |
| `FrontRoomsMapInteractionTests` | plain **279 / 279**; doors 278 / 279; doors + keys 277 / 279 | doors: "CommitUnlock … the leaf shifts 0.0 mm" (the map's own `CommitUnlock` jolts `door.rig` instead of the cube when a rig is set, and the test measures the cube). Keys: "key spot: the key lies at the marker" expects the cube's centre at y 0.06; the dressed root is the surface point (y 0.00, x and z exact). Both are the tests' cube premise, not kit faults; main runs these suites with the cube (edit mode does not dress unless a tool asks: `FrontRoomsInteractableKit.DressInEditMode`) |
| `FrontRoomsCxPlayRun` (Play, 120 s, seed 516574485, G14 on High) | **0 errors, 1 exception** (Unity's QuickSearch indexer at start, expected), 0 pink renderers, 2 restarts after caught, 7,307 frames | `data/tests/play_d16_summary.txt`. Play Mode dresses every map door and key. 17:07–17:11, UNDER LOAD (1-min load 80–407). Re-run on the final package code (the `Application.quitting` clean-up, 30 s): 0 errors, the same 1 expected exception, 0 pink, 0 `error CS` (`data/tests/play_d16b_summary.txt`, 17:33–17:35, UNDER LOAD) |
| Roslyn `--all` | COMPILE OK, macOS + iOS | on the clone; on main + payload; on main + payload + contract |
| `FrontRoomsCxSuites -cxOnly suites` (the project's 10 edit-mode suites) | **all pass, no harness exception** | MapVerification 100 / 100, RelayNav 60 / 60, MapInteraction 279 / 279 (its 2 errors / 5 exceptions are its own forced failures, as on main), LevelDesigner PASS, FixtureTick 221 / 221, CameraRig 33 / 33, Captions 9 / 9, GlassShot 20 / 20, StreamVerification 5 / 5, GlassCompileCheck OK. 17:15–17:25, UNDER LOAD. `data/tests/cxsuites/` |

Variants: "plain" = as main (cubes); "doors" = `DressInEditMode` on, keys off; "doors + keys" = both on. JSON in `data/tests/`.

## 4. Deviations from `10_design.md` (and why)

1. **The EXIT door is not dressed.** C22/C23 (landed 15:39) gave it the map's own "Leaf rig", crossbar and LIT sign, and the stairwell production dresses it on `ExitDoorBuilt`. M1 sits in the `else` of the EXIT branch and `DressDoor` refuses `map.IsExitDoor(door)`. A19's "EXIT … too" is the stairwell production's, with these DA kits (Q-EX stays open).
2. **Edit mode keeps the cubes unless a tool asks** (`DressInEditMode`): the map's edit-mode suites and the Level Designer preview see main's map unchanged; Play Mode always dresses.
3. **HeldItem without a renderer-asset change:** the pass is enqueued from `RenderPipelineManager.beginCameraRendering` only while something is on layer 30, so `RenderSetup` needs no feature registration and no layer-mask change (the design's `m_OpaqueLayerMask` hunk is not needed: the redraw after the depth punch covers the opaque draw). The SSAO-white rebind for the redraw is not done (open item 6).
4. **M5 (the first-key `OpenHeld` hand-off in Game) is not in the contract yet**: Game has no C21 first-key hook to attach it to. The assembly exposes `Root` and `TagKit`; `FrontRoomsInspect.OpenHeld` is in main. M6 (the A13 key identity) and M8's `DoorMemberFor` wait for the map; the stub hash and the theme rule stand in.
5. **M9 added to the contract** (one line): the Level Designer palette skips kits tagged `interactable` + (`door` or `lock_part`); key hosts, keys, signs and plates stay.
6. **Deferred to later stages:** the LOD re-export of the 46 lock / key / tag / host / sign / plate kits (the design allows it as a separate package), T6/T7/T8/T10 captures (VL320–VL324), the three-view sheets and the `threeview/index.json` assets block (raw renders made, §8), atlas proofs to 平面视觉.

## 5. A12 record (done first)

`05_for_interaction.md`; images `images/a12_*` (Shot format): `a12_sheet.jpg` (VL317), `a12_gap_sheet.jpg` (VL318), `a12_junction95_sheet.jpg` (VL319) and 26 single frames. Seams: **0 lit pixels** in every band, both faces, 1.0 and 2.5 m, brush on and off; Blender rays: with the brush 0 rays through latch, head and hinge; without it the latch and head slits open only within ±1.0° of their plane; the floor 3 mm within ±1.5°. Hinge junction: from 24.5° each way the leaf runs into the hinge-side reveal (54.5 mm from 76°), and tucks 24–33 mm into the casing at 90–95° (open item 1).

## 6. Map contract (for 关卡设计; not applied in main)

`data/contract_map_D16.diff`, applier `data/apply_map_hunks_v2.py.txt` (each anchor exactly once; a hunk already present is skipped):

- **M1** BuildEdge: `door.rig = FrontRoomsInteractableKit.DressDoor(this, door, chunk.root.transform)` in a try/catch that logs, for every map door but the EXIT (null keeps the cube).
- **M3** SpawnKey: `SpawnDressedKey` first: an unscaled "Key · zone {id}" root (host-hung via `DressKeyHost` when the module key spot names a host, else on its spot or on the floor of its site cell), `DressKey(root, Stub(zone), placement, KeyPickupNeedsE)`, `keyByCollider[assembly.Trigger] = zone`; no key spins; the cube path stays as the fallback.
- **M4** DoorShot: the unlock's key (`DressShotKey`) posed with `PoseInLock` on the shot's beats, `Sway` from the turn, released and destroyed at `KeyOutEnd` or after a cancel's `CancelKeyOut`; only on a dressed door.
- **M7** `public bool IsUnlocked(Door)`.
- **M9** the palette filter (§4.5).
- The swing rule, `FaceOf`, `doorSides` and the step-aside are untouched (A12 as landed). For 关卡设计's own tests, the two dressed-variant differences in §3 are worth a branch on `door.rig` when they want to test the dressed path.

## 7. Open issues

1. **Hinge junction (VL319, FINDING):** Option B's pivot on the centre plane runs the leaf into the hinge-side reveal from 24.5° and into the casing beyond 76°. The game-light frames are too dark to judge it. Fix ladder (not built): a cove on the hinge-side casing reveal, or ask the map for a 90° DA rest. Needs a lit doorway view at 0.6 m in Capture.
2. **Leaf LOD1 is far below the design budget** (Veneer 240 vs 1,050; Steel 264 vs 1,600): the decimator collapsed the 96-segment stile. Check for a pop at 4 m in T7.
3. **DoorShot "doors + keys" and MapInteraction dressed variants** fail 2 / 1–2 checks on the tests' cube premise (§3). Main is unaffected (suites run plain).
4. **Q-EX** (crossbar vs keyed knob on the EXIT leaf) and **Q-RN** (lever on the ward door) stay as in the design's defaults.
5. **Type sign-off**: the tag numbers (G12) and the engraved sign (ivory proposal) go to 平面视觉 · 信息设计 and · 印刷与演示 before Red sees them in game.
6. HeldItem redraw does not rebind SSAO to white, so the background's AO can tint a held key where it was occluded (minor; check in VL322).
7. Cost: no budget set until T10 (desktop and WebGL lodBias 1). G14 sees the dressed doors as receivers / BLAS (meshes 1,543 at the end of the play run).

## 8. Runs after the package (filled in at the end of this stage)

- Three-views of the 15 DA kits (`FrontRoomsThreeView`, 17:13–17:14, frames ±90° yaw): 60 transparent PNGs (top / front / side / persp) + `manifest.json`, copied to `images/threeview_da/` for 平面视觉 · 印刷与演示. The `threeview/index.json` rows for them are drafted in `data/threeview_DA_assets.json` (dims, tris per LOD, materials, what replaces what); merging them into the index and composing the sheets is the hand-off.
- `FrontRoomsCxSuites -cxOnly suites`: all pass (§3); log `W/logs/d16/b10_cxsuites.log`, reports copied to `data/tests/cxsuites/`.

## 9. How to resume

- Clone `W/proj_d16` (= main 16:56 + package + contract + the shared tools + the clone-only harness `Assets/Editor/D16/FrontRoomsD16Capture.cs`: steps `materials`, `tests`, `a12`, `doorshot`, `mapsuites`; args `-d16Steps`, `-d16Suites`, `-d16Variants`, `-d16Seed`, `-d16Out`).
- Work dir `W/d16_work`: `build_package.py` (rebuilds `W/apply/d16*`, moving the old one to `W/_partial`), `contract/apply_map_hunks_v2.py`, `compose_a12.py` (A12 sheets), `run_when_free2.sh` (waits for a visual-line Unity slot via `W/tools/unity_jobs.sh`), `payload_1650/` (the account-A files as found).
- Logs `W/logs/d16/b05`–`b10`. VL specs `W/vl_pending/d16.md` (VL317–VL324).
