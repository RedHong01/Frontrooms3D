# Migration status

Branch: `unreal-migration`  
Unity baseline: `6000.3.10f1` / URP `17.3.0`  
Contract schema: `1`

Requested engine target: UE5.8.3. UE5.8.3 is installed at `D:\UE_5.8`, the project
association is `5.8`, and the Windows smoke gate passes with that editor.
Build target: Windows `Win64` only. Other target platforms are outside this migration.

## Synchronization rule

**Any Unity-side update must be assessed and synchronized on the Unreal side in
the same work item.** Track it as `Unity Changed` → `Contract Exported` →
`Unreal Updated` → `Unity/Unreal Verified`. Until the last state passes, mark
the item `Pending Unreal Sync`; never silently advance only one implementation.

Full policy: `Migration/UNITY_UNREAL_SYNC_POLICY.md`.

## Current synchronization record

| Work item | Unity Changed | Contract Exported | Unreal Updated | Unity/Unreal Verified |
|---|---|---|---|---|
| Initial map/data migration slice | Baseline recorded; later Unity changes remain tracked separately | Profile, 4 modules, 113 sidecars, FMOD and MapHash exported | Python/C++ MapHash, C++ data shapes and Unity golden-chunk exporter added | Python/C++ checks pass; Unity Editor chunk export pending |

## Completed in this worktree

- [x] Copy the audit into the Git worktree.
- [x] Export the authored level profile and four room modules into a portable JSON contract.
- [x] Record map units, chunk dimensions, rebase threshold, build scene and golden seeds.
- [x] Record the 31 FMOD events, 5 buses and 19 parameters.
- [x] Add the Python `MapHash`/xorshift oracle with Unity-derived golden vectors.
- [x] Add source-backed regression tests for the exporter and hash oracle.
- [x] Add the Unity Editor golden-chunk exporter for the three validation seeds.
- [x] Export all 113 prop sidecars into a mesh/collider/anchor/LOD manifest.
- [x] Add a UE5.8 project/module skeleton with contract USTRUCTs and a JSON importer.
- [x] Add a first Title/Playing/Paused/Caught + key/door + Relay Listen→Chase runtime slice.
- [x] Add the first Windows Character movement/sprint slice with asset-free WASD/Shift input mappings and smoke coverage.
- [x] Add a Windows Node export gate that runs without Python or Unreal Editor.
- [x] Add a Unity-side asset bridge manifest and Windows staging script so UE can reuse existing FBX, textures, fonts, video and FMOD banks.
- [x] Establish the Windows HDR/ACES calibration baseline from Unity (1000 nit peak, 300 nit paper white, 0.15 EV exposure, warm/green grade) with smoke coverage.
- [x] Compile `FrontRoomsEditor` with UE5.8.3 on Windows.
- [x] Run the UE `FrontRoomsContract` commandlet against the generated contract and kit manifest.
- [x] Stage and byte-count all 519 Unity source assets locally (123 FBX, 113 sidecars, 167 textures, 5 FMOD banks, video and fonts).
- [x] Import one staged Office FBX through UE5.8.3 `ImportAssets` and verify static meshes/materials are generated.
- [x] Batch-import all 123 Unity FBX files into local `/Game/FrontRooms/UnityImported` (0 errors; importer bounds/tangent warnings are queued for sidecar review; latest local log reports 108 warning lines).
- [x] Batch-import all 167 Unity surface/lighting textures into local `/Game/FrontRooms/UnityImported/Textures` (0 errors, 0 warnings).
- [x] Export 91 Unity URP material profiles into `Migration/exports/unreal_material_profiles.json` (base color, metallic, smoothness/roughness, normal, emission and texture references).
- [x] Add `audit_unreal_assets.ps1` and run it from the Windows smoke gate (123/123 FBX packages, 167/167 textures, 113/113 sidecars, 10/10 audio/video/font/bank files).
- [x] Add the repeatable Windows `run_unreal_asset_pipeline.ps1` pass for Unity staging, material-profile export, FBX/texture import and audit.
- [x] Add a Windows-only smoke runner covering export/asset hashes, UE compile, contract commandlet, state transitions, deterministic hash vectors and imported asset coverage.

## Next implementation gates

- [x] Compile the engine-independent C++ hash implementation against the Python oracle.
- [x] Define engine-independent C++ grid/zone/chunk data shapes from the Unity contract.
- [ ] Run the golden-chunk exporter in Unity and review the per-seed JSON.
- [ ] Promote the importer to an editor asset factory for profile/modules/sidecars.
- [ ] Verify units and axes and apply LOD/collision presets to the imported 123 FBX meshes (sidecar audit is now blocking missing/invalid metadata; UE asset factory remains).
- [ ] Apply sidecar collision/LOD metadata to the imported meshes and resolve the remaining importer bounds/tangent warnings.
- [ ] Assign Unity A/N/S/E/M/P channel settings to the imported textures.
- [ ] Run `stage_unity_assets.ps1` after the UE content layout is approved, then configure FBX/material import presets.
- [ ] Add the first UE world slice: one generated chunk and UMG HUD (Character movement/sprint and HDR camera baseline are now in place).
- [ ] Add screenshot/input/audio traces before upgrading materials and lighting.

The JSON contract is generated, so rerun the exporter after changing the Unity
source. Do not hand-edit `Migration/exports/frontrooms_contract.json`.
