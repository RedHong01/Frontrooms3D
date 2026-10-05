# FrontRooms Unreal migration slice

`FrontRoomsss.uproject` is the first Unreal-side source tree. It is intentionally
kept beside the Unity project until a fixed UE5 editor is installed. The module
contains:

- `FrontRoomsMigrationTypes.h`: `UPrimaryDataAsset`-compatible contract types;
- `FrontRoomsContractImporter.*`: JSON contract and kit-manifest validation;
- `FrontRoomsSliceGameMode.*`: the first Title → Playing → Paused/Caught slice
  with deterministic seed, key, door and Relay Listen → Chase state.

The migration target is Windows only (`Win64`). The Windows workstation currently has UE5.6 at `D:\UE_5.6`; the `FrontRoomsEditor` target
has been compiled once. Epic has released UE5.8 (including the 5.8.3 hotfix),
but UE5.8 is not installed in the local Epic manifest yet, so `EngineAssociation`
stays at `5.6` until that editor is installed. Imported assets are present locally;
the first playable map remains to be created. Open `FrontRoomsss.uproject`
and point an editor utility at `Migration/exports/frontrooms_contract.json` and
`Migration/exports/kit_manifest.json`.

The compiled editor target also exposes a headless contract gate:

```powershell
& 'D:\UE_5.6\Engine\Binaries\Win64\UnrealEditor-Cmd.exe' `
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
with zero import errors; UE reported 76 bounds warnings that remain for the
sidecar collision/LOD pass.

Surface and lighting textures can be imported with the matching batch command:

```powershell
.\Tools\UnrealMigration\import_unity_textures.ps1
```

The current Windows batch run imported all 167 Unity texture files with zero
errors and zero warnings. Generated `.uasset` files stay under the ignored
`Migration/Unreal/Content` tree; the import settings retain direct Unity-side
source paths so the same pass can be regenerated after switching to UE5.8.

Run the migration smoke gate from PowerShell after each migration batch:

```powershell
.\Tools\UnrealMigration\smoke_test.ps1
```

It rechecks the Unity export contract and asset hashes, resolves the associated
Windows editor, compiles `FrontRoomsEditor` for `Win64 Development`, runs the
contract commandlet, then runs the in-editor state, deterministic hash and
imported-asset commandlet. Each run writes a timestamped report under the
ignored `Migration/Unreal/Saved/MigrationSmoke` directory.
