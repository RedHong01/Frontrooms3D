# FrontRoomsss Unreal migration slice

`FrontRoomsss.uproject` is the Unreal-side source tree kept beside the Unity
project so the two sides can be synchronized from either workstation. The module
contains:

- `FrontRoomsMigrationTypes.h`: `UPrimaryDataAsset`-compatible contract types;
- `FrontRoomsContractImporter.*`: JSON contract and kit-manifest validation;
- `FrontRoomsSliceGameMode.*`: the first Title → Playing → Paused/Caught slice
  with deterministic seed, key, door and Relay Listen → Chase state.
- `FrontRoomsSliceCharacter.*`: Windows WASD movement with Shift sprint, wired
  through `DefaultInput.ini` and selected as the GameMode default pawn.
- `FrontRoomsHUDWidget.*`: native UMG overlay attached by the GameMode at login;
  it mirrors Title/Playing/Paused/Caught, seed, Relay, key and door state and
  remains usable before a designer-authored Widget Blueprint is imported.
- `FrontRoomsAudioBridge.*`: stable native event seam (`run.begin`, pause/resume,
  door, key and Relay events). On Win64 it first loads the Unity-shipped FMOD
  Studio runtime and staged banks through `FrontRoomsFmodRuntime.*`, then falls
  back to imported UE SoundWave assets if the native payload is absent.
- The slice camera carries the Unity `FrontRoomsPost.asset` baseline: ACES,
  +0.15 EV exposure, temperature +9, tint -7, contrast -6 and saturation -8.

The migration target is Windows only (`Win64`). The Windows workstation now has
UE5.8.3 at `D:\UE_5.8`, and `EngineAssociation` is `5.8`. The `FrontRoomsEditor`
target has been rebuilt successfully with UE5.8.3. Imported assets are present locally;
and the first authored map is saved at
`Content/FrontRooms/Maps/FrontRoomsRuntime.umap`. Open `FrontRoomsss.uproject`
to inspect its four deterministic modules, Unity props, collision, and HDR lighting;
the map is also configured as the editor and game startup map. The map can be
regenerated idempotently with `Tools/UnrealMigration/create_runtime_map.py`
after the PythonScriptPlugin is enabled.
The importer and editor utility consume `Migration/exports/frontrooms_contract.json`
and `Migration/exports/kit_manifest.json`.

Nanite is enabled at the project level and the Win64 renderer is pinned to DX12
with `PCD3D_SM6`; the editor launcher also passes `-dx12 -sm6` so the viewport
does not fall back to the SM5 shader path.

UE5.8 renderer settings explicitly select Lumen for global illumination and
reflections. Hardware ray tracing is enabled for Lumen when the Windows DX12
device exposes DXR, with generated Nanite ray-tracing proxies. Direct-light
shadows use Virtual Shadow Maps and global ray-traced shadows remain disabled
to avoid a per-light dispatch for the runtime room's movable point lights.
The complete audit and real-RHI restart procedure is in
`Migration/RAYTRACING_AUDIT.md`; `validate_nanite_config.mjs` checks this
renderer contract together with the Win64/DX12/SM6/Nanite settings.

HDR calibration is configured for Windows output in `Config/DefaultEngine.ini`
and `Config/DefaultGameUserSettings.ini`: HDR is allowed with SDR fallback,
the display peak is 1000 nits, and paper white is 300 nits. UE5.8 reads the
native `HDRPaperWhiteNits` setting; UE5.6 keeps the same value in the migration
calibration section and UI luminance CVar. HDR output still requires a supported
Windows display/RHI and exclusive fullscreen at runtime.

The compiled editor target also exposes a headless contract gate:

```powershell
& 'D:\UE_5.8\Engine\Binaries\Win64\UnrealEditor-Cmd.exe' `
  'Migration/Unreal/FrontRoomsss.uproject' -run=FrontRoomsContract
```

Before opening Unreal, run the Windows-only export gate from the Unity project
root:

```powershell
node Tools/UnrealMigration/validate_exports.mjs
```

The UE tree reuses Unity assets first. Generate and check the bridge manifest,
then stage copies into the UE `Content` directory when the editor is installed:

```powershell
node Tools/UnrealMigration/export_asset_bridge.mjs
node Tools/UnrealMigration/test_asset_bridge.mjs
.\Tools\UnrealMigration\stage_unity_assets.ps1 -WhatIf
```

The bridge preserves the Unity-relative source path and SHA-256 for every
FBX (123 kit/office meshes), sidecar, surface texture/material, font, logo, FMOD bank and video. Visual
upgrades stay out of this stage; import presets and material replacements are a
later gate after the source assets are present in UE.

To rebuild the Unreal static meshes from the staged Unity FBX files:

```powershell
.\Tools\UnrealMigration\import_unity_fbx.ps1
```

The generated `Migration/exports/unreal_import_settings.json` is machine-local
configuration because it contains absolute source paths. The resulting
`.uasset` files stay local under the ignored UE `Content` directory and can be
regenerated from the bridge. The Windows batch run completed all 123 FBX files
with zero import errors. The sidecar asset-factory pass runs after import and
applies the authored collision and LOD contract before smoke validation.

Surface and lighting textures can be imported with the matching batch command:

```powershell
.\Tools\UnrealMigration\import_unity_textures.ps1
```

The current Windows batch run imported all 167 Unity texture files with zero
errors and zero warnings. Generated `.uasset` files stay under the ignored
`Migration/Unreal/Content` tree; the import settings retain direct Unity-side
source paths so the same pass can be regenerated after switching to UE5.8.

The selected Unity/FMOD WAV sources are exported to
`Migration/exports/unreal_audio_import_settings.json` and imported with:

```powershell
.\Tools\UnrealMigration\import_unity_audio.ps1
```

The repeatable asset pipeline runs this audio import after meshes and textures.
The audit and smoke commandlet require all eight SoundWave packages, including
the door, key, ambience, relay, window and player-footstep sources.

The original Win64 FMOD runtime and opaque bank bytes are staged as part of the
same migration. `Tools/audio/fmod_bank_probe.py` loads all five banks through
`fmodstudio.dll`, resolves the Unity event/parameter/bus contract, starts a real
event instance, and can render it through FMOD's WAV writer. The current Unity
bank build loads and plays 28 of the 31 source events. The three
`StreamOpen`, `StreamClose` and `StreamLock` paths are retained as an explicit
source/bank regeneration gap; strict probe mode fails on them and smoke keeps
the gap visible in `pendingCoverage`.

Unity URP surface values are exported to
`Migration/exports/unreal_material_profiles.json` (91 profiles with source
SHA-256 values). The Windows asset audit checks those profiles together with
all 123 FBX packages, 167 textures, 113 sidecars and staged audio/video/font
inputs. Run the full repeatable pass with
`Tools/UnrealMigration/run_unreal_asset_pipeline.ps1` before opening a new
editor session.

The Windows commandlet can apply the same metadata directly to imported prop
meshes. `-ApplyToAssets` updates the LOD0 `BodySetup` with Unity's box
colliders (metres converted to centimetres), writes LOD screen-size ratios,
and stores anchor names, axis, units and source hash in package metadata:

```powershell
& 'D:\UE_5.8\Engine\Binaries\Win64\UnrealEditor-Cmd.exe' `
  'Migration/Unreal/FrontRoomsss.uproject' -run=FrontRoomsSidecar `
  '-ApplyToAssets' '-Sidecars=Assets/Resources/Props/Models'
```

The generated `Migration/exports/unreal_sidecar_asset_factory.json` reports
113/113 applied assets, 55 box colliders, 59 LOD values and 490 anchors.

`Tools/UnrealMigration/export_unreal_material_factory.mjs` additionally writes
`Migration/exports/unreal_material_factory.json`, the UE 5.8 material/texture
contract consumed by the Windows audit. A/N/S/E/M/P remain explicit: A is
sRGB base color, N is a linear tangent normal, S is Unity smoothness mapped to
UE roughness with inversion, E is linear emissive, M is linear mask/macro data,
and P is sRGB print/decal. The manifest resolves Unity GUIDs to imported UE
texture assets and retains the original material scalar/color parameters.

Run the migration smoke gate from PowerShell after each migration batch:

```powershell
.\Tools\UnrealMigration\smoke_test.ps1
```

It rechecks the Unity export contract and asset hashes, resolves the associated
Windows editor, compiles `FrontRoomsEditor` for `Win64 Development`, runs the
contract commandlet, then runs the in-editor state, movement, deterministic hash
and imported-asset commandlet. Each run writes a timestamped report under the
ignored `Migration/Unreal/Saved/MigrationSmoke` directory.
