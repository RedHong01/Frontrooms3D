# FrontRooms Unreal migration slice

`FrontRoomsUE.uproject` is the first Unreal-side source tree. It is intentionally
kept beside the Unity project until a fixed UE5 editor is installed. The module
contains:

- `FrontRoomsMigrationTypes.h`: `UPrimaryDataAsset`-compatible contract types;
- `FrontRoomsContractImporter.*`: JSON contract and kit-manifest validation;
- `FrontRoomsSliceGameMode.*`: the first Title → Playing → Paused/Caught slice
  with deterministic seed, key, door and Relay Listen → Chase state.
- `FrontRoomsSliceCharacter.*`: Windows WASD movement with Shift sprint, wired
  through `DefaultInput.ini` and selected as the GameMode default pawn.
- The slice camera carries the Unity `FrontRoomsPost.asset` baseline: ACES,
  +0.15 EV exposure, temperature +9, tint -7, contrast -6 and saturation -8.

The migration target is Windows only (`Win64`). The Windows workstation now has
UE5.8.3 at `D:\UE_5.8`, and `EngineAssociation` is `5.8`. The `FrontRoomsEditor`
target has been rebuilt successfully with UE5.8.3. Imported assets are present locally;
and the first authored map is saved at
`Content/FrontRooms/Maps/FrontRoomsRuntime.umap`. Open `FrontRoomsUE.uproject`
to inspect its four deterministic modules, Unity props, collision, and HDR lighting;
the map is also configured as the editor and game startup map. The map generator
can be regenerated with `Tools/UnrealMigration/generate_frontrooms_map.py` after
the PythonScriptPlugin is enabled.
and point an editor utility at `Migration/exports/frontrooms_contract.json` and
`Migration/exports/kit_manifest.json`.

HDR calibration is configured for Windows output in `Config/DefaultEngine.ini`
and `Config/DefaultGameUserSettings.ini`: HDR is allowed with SDR fallback,
the display peak is 1000 nits, and paper white is 300 nits. UE5.8 reads the
native `HDRPaperWhiteNits` setting; UE5.6 keeps the same value in the migration
calibration section and UI luminance CVar. HDR output still requires a supported
Windows display/RHI and exclusive fullscreen at runtime.

The compiled editor target also exposes a headless contract gate:

```powershell
& 'D:\UE_5.8\Engine\Binaries\Win64\UnrealEditor-Cmd.exe' `
  'Migration/Unreal/FrontRoomsUE.uproject' -run=FrontRoomsContract
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
with zero import errors; importer bounds/tangent warnings remain for the
sidecar collision/LOD pass (the current local log reports 108 warning lines).

Surface and lighting textures can be imported with the matching batch command:

```powershell
.\Tools\UnrealMigration\import_unity_textures.ps1
```

The current Windows batch run imported all 167 Unity texture files with zero
errors and zero warnings. Generated `.uasset` files stay under the ignored
`Migration/Unreal/Content` tree; the import settings retain direct Unity-side
source paths so the same pass can be regenerated after switching to UE5.8.

Unity URP surface values are exported to
`Migration/exports/unreal_material_profiles.json` (91 profiles with source
SHA-256 values). The Windows asset audit checks those profiles together with
all 123 FBX packages, 167 textures, 113 sidecars and staged audio/video/font
inputs. Run the full repeatable pass with
`Tools/UnrealMigration/run_unreal_asset_pipeline.ps1` before opening a new
editor session.

Run the migration smoke gate from PowerShell after each migration batch:

```powershell
.\Tools\UnrealMigration\smoke_test.ps1
```

It rechecks the Unity export contract and asset hashes, resolves the associated
Windows editor, compiles `FrontRoomsEditor` for `Win64 Development`, runs the
contract commandlet, then runs the in-editor state, movement, deterministic hash
and imported-asset commandlet. Each run writes a timestamped report under the
ignored `Migration/Unreal/Saved/MigrationSmoke` directory.
