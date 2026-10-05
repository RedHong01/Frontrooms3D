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
| Initial map/data migration slice | Baseline recorded; later Unity changes remain tracked separately | Profile, 4 modules, 113 sidecars, FMOD and MapHash exported | Python/C++ MapHash, C++ data shapes and Unity golden-chunk exporter added | Python/C++ checks pass; Unity 6000.3.10f1 golden chunks validated for all three seeds |

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
- [x] Lock the Win64 renderer to DX12 / Shader Model 6 and enable the project Nanite switch so imported Nanite meshes do not trigger the missing-project-settings prompt.
- [x] Compile `FrontRoomsEditor` with UE5.8.3 on Windows.
- [x] Run the UE `FrontRoomsContract` commandlet against the generated contract and kit manifest.
- [x] Stage and byte-count all 519 Unity source assets locally (123 FBX, 113 sidecars, 167 textures, 5 FMOD banks, video and fonts).
- [x] Import one staged Office FBX through UE5.8.3 `ImportAssets` and verify static meshes/materials are generated.
- [x] Batch-import all 123 Unity FBX files into local `/Game/FrontRooms/UnityImported` (0 errors; importer bounds/tangent warnings are queued for sidecar review; latest local log reports 108 warning lines).
- [x] Batch-import all 167 Unity surface/lighting textures into local `/Game/FrontRooms/UnityImported/Textures` (0 errors, 0 warnings).
- [x] Export 91 Unity URP material profiles into `Migration/exports/unreal_material_profiles.json` (base color, metallic, smoothness/roughness, normal, emission and texture references).
- [x] Generate the UE 5.8/Win64 A/N/S/E/M/P material factory contract (`unreal_material_factory.json`): 91 profiles, 157 referenced imported textures, 0 unresolved GUIDs.
- [x] Apply and serialize the UE 5.8 texture settings for all 167 imported textures (sRGB, compression, normal-map flag, no-alpha masks and LOD group); the asset-factory report has `167/167 applied`, `0 errors`, and a repeat run with `changed=0`.
- [x] Add `audit_unreal_assets.ps1` and run it from the Windows smoke gate (123/123 FBX packages, 167/167 textures, 113/113 sidecars, 10/10 audio/video/font/bank files).
- [x] Add the repeatable Windows `run_unreal_asset_pipeline.ps1` pass for Unity staging, material-profile export, FBX/texture import and audit.
- [x] Export and import the first Unity/FMOD WAV slice as eight UE SoundWave assets; keep them as a fallback behind the stable audio seam.
- [x] Stage the Unity Win64 `fmodstudio.dll` and five opaque bank files, load them dynamically from Unreal, resolve the Unity event contract, and verify native event Start/Update plus a real FMOD WAV render in smoke. The prior Win64 probe recorded the three stale Stream* events as explicit source-bank drift; the regenerated payload is awaiting the strict Win64 re-probe.
- [x] Add the UE sidecar importer/commandlet for all 113 prop contracts (55 collider boxes, 490 anchors, 59 LOD records) with a report and smoke stage.
- [x] Promote the sidecar importer to a repeatable UE 5.8 editor asset factory: 113/113 imported prop meshes updated with 55 box colliders, 59 LOD values and 490 anchor metadata entries.
- [x] Complete the native Relay Listen → Chase → Search → Listen loop, key/door/window interactions, Complete/Caught states and trace coverage.
- [x] Add a native Win64 FMOD adapter that dynamically loads the Unity-shipped `fmodstudio.dll`, stages all five banks, and plays the migrated door/key/relay/window event paths; the external probe renders a real WAV. The Mac-side rebuild now supplies all 31 contract events, while Win64 acceptance remains pending.
- [x] Rebuild the five Desktop FMOD banks with the matching Mac FMOD Studio CLI (`2.03.15`, build `168126`), copy identical hashes to Unity StreamingAssets and the Unreal staging directory, and verify `31/31` events plus a rendered event instance with the macOS runtime. The staged hashes and Mac runtime path are recorded in `Migration/exports/fmod_bank_manifest.json`; Windows must regenerate this manifest with the Win64 DLL.
- [x] Add and pass a live possessed-character movement trace (WASD input, walking delta and sprint delta) in the UE 5.8 commandlet world.
- [x] Add a Windows-only smoke runner covering export/asset hashes, UE compile, contract commandlet, state transitions, deterministic hash vectors and imported asset coverage.
- [x] Full smoke passed on UE 5.8.3 at `Migration/Unreal/Saved/MigrationSmoke/20261005T041821599Z/report.json`; all stages passed, including the DX12/SM6 Nanite configuration gate, golden chunks, material-assets, sidecars, live possession movement, HDR and FMOD playback.
- [x] Rebuilt and launched the Win64 Development package at `Migration/Unreal/Builds/Windows/Development` after the SM6 configuration change; the UE 5.8.3 archive contains the `PCD3D_SM6` shader libraries, `fmodstudio.dll` and all five FMOD banks, and cook/stage/archive completed successfully for Win64. Package startup smoke logged `rhifeaturelevel=SM6`, `shaderplatform=PCD3D_SM6`, and the saved 4-module/122-component runtime map under `Builds/Windows/Development/FrontRoomsss/Saved/Logs/FrontRoomsss.log`.
- [x] Audit and optimize the Win64 DX12 renderer for Lumen/HDR/Nanite: explicit Lumen GI/reflections, hardware RT when DXR is available, Nanite RT proxies, and Virtual Shadow Maps with global RT shadows disabled to avoid per-point-light ray dispatch. Static gate: `node Tools/UnrealMigration/validate_nanite_config.mjs D:\Frontrooms3D`; audit and restart evidence: [`Migration/RAYTRACING_AUDIT.md`](RAYTRACING_AUDIT.md).

## Next implementation gates

- [x] Compile the engine-independent C++ hash implementation against the Python oracle.
- [x] Define engine-independent C++ grid/zone/chunk data shapes from the Unity contract.
- [x] Run the Unity 6000.3.10f1 golden-chunk exporter for seeds 2554, 20388 and 20261001; validate all three radius-1 exports (9 chunks each) with `validate_golden_chunks.py` and include their hashes in the sync manifest. Evidence: `Migration/exports/unity_golden_chunk_validation.json`.
- [x] Promote the importer to an editor asset factory for profile/modules/sidecars.
- [x] Verify units and axes and apply LOD/collision presets to the imported 123 FBX meshes (sidecar factory applied the authored metadata to all 113 prop contracts).
- [ ] Resolve the remaining importer bounds/tangent warnings with source-specific reimport settings; the authored collision/LOD metadata is now applied and smoke-gated. A reversible UE 5.8.3 probe now demonstrates that `bRemoveDegenerates=true` removes the bounds mismatch for `Kit_Binders` on a transient duplicate; the source `.uasset` remains unchanged.
- [x] Assign Unity A/N/S/E/M/P channel settings to all 167 imported textures with the UE 5.8 editor material asset factory (`unreal_material_asset_factory.json`: 167/167, 0 errors; repeat run changes 0 assets).
- [x] Run the Windows Unity asset staging/import pipeline and configure FBX/material import presets for the approved UE content layout.
- [x] Add the first UE world slice: one generated chunk and native UMG HUD (Character movement/sprint and HDR camera baseline are now in place).
- [x] Save `FrontRoomsRuntime.umap` with four modules, Unity props, collision, lights and HDR post process; the runtime map smoke gate verifies the saved map.
- [x] Add the native HUD and stable audio event seam; smoke verifies state presentation, mapped SoundWave packages and run/door/key/Relay event names.
- [x] Produce a Windows `Development` IoStore package and launch it against the saved runtime map; packaged logs confirm 4 modules and 122 saved components.
- [x] Add deterministic input, gameplay, movement, audio-event, FMOD-bank and HDR smoke traces before further material/lighting upgrades.

## Explicit remaining source gates

- [ ] Resolve the 108 remaining FBX importer bounds/tangent warnings with source-specific reimport settings; authored sidecar collision/LOD metadata is already applied and smoke-gated. Current evidence is recorded in `Migration/exports/unreal_mesh_build_probe_binders.json` and `Migration/exports/unreal_mesh_build_probe_doorframe.json`: the representative bounds warning is fixed on the transient `Kit_Binders` build, while `Kit_DoorFrame_Steel_LOD0` still emits near-zero tangent/bi-normal diagnostics after normal/tangent recomputation. Keep this gate pending until each affected source asset is re-exported or receives a reviewed per-asset exception.
- [ ] Complete Windows acceptance for the regenerated Unity FMOD banks so `event:/Mechanism/Door/StreamOpen`, `StreamClose`, and `StreamLock` resolve through the Win64 DLL. The Mac rebuild and `31/31` macOS probe are complete; rerun the strict Windows probe, asset pipeline, smoke and package gates before closing this item. Source-side and Windows-side commands are recorded in [`Migration/FMOD_BANK_REBUILD.md`](FMOD_BANK_REBUILD.md).

The Unity golden-chunk source gate is complete on Windows with Unity
`6000.3.10f1`: the exporter produced three radius-1 JSON snapshots (9 chunks
per seed), and `validate_golden_chunks.py` checked their contract shape and
the four shared Unity/Unreal MapHash vectors. The seed-file SHA-256 values are
recorded in `Migration/exports/unity_golden_chunk_validation.json` and in the
Unity/Unreal sync manifest.

The editor launcher `Tools/UnrealMigration/open_unreal_editor.ps1` uses the UE5.8-specific workspace profile `.ue58-profile` and `UE_SKIP_UBT_SDK_SETUP=1`. This avoids the recurring `dotnet.exe` `0xe0434352` popup caused by UE's all-platform SDK validation probing invalid non-Win64 SDKs; the project and packaging remain Win64-only.

Mac-side source repairs and their Windows acceptance commands are recorded in
[`MAC_SIDE_REPAIR_QUEUE.md`](MAC_SIDE_REPAIR_QUEUE.md).

The current [`fmod_bank_manifest.json`](exports/fmod_bank_manifest.json) is the
Mac-side rebuild record: the 88-byte `.fspro` has zero serialized objects, its
Metadata folder has 31 event files, the staged Unity and Unreal bank hashes
match, and the matching macOS FMOD runtime resolves 31/31 events. The manifest
runtime path is intentionally macOS evidence; the Windows acceptance step must
run the same probe with `Assets/Plugins/FMOD/platforms/win/lib/x86_64/fmodstudio.dll`
and rewrite the manifest before this gate is complete.
The JSON contract is generated, so rerun the exporter after changing the Unity
source. Do not hand-edit `Migration/exports/frontrooms_contract.json`.
