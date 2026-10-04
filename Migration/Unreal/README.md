# FrontRooms Unreal migration slice

`FrontRoomsUE.uproject` is the first Unreal-side source tree. It is intentionally
kept beside the Unity project until a fixed UE5 editor is installed. The module
contains:

- `FrontRoomsMigrationTypes.h`: `UPrimaryDataAsset`-compatible contract types;
- `FrontRoomsContractImporter.*`: JSON contract and kit-manifest validation;
- `FrontRoomsSliceGameMode.*`: the first Title → Playing → Paused/Caught slice
  with deterministic seed, key, door and Relay Listen → Chase state.

The Windows workstation has UE5.6 at `D:\UE_5.6`; the `FrontRoomsEditor` target
has been compiled once. The project is still source-only until the editor
creates the first map and imports the staged assets. Open `FrontRoomsUE.uproject`
and point an editor utility at `Migration/exports/frontrooms_contract.json` and
`Migration/exports/kit_manifest.json`.

The compiled editor target also exposes a headless contract gate:

```powershell
& 'D:\UE_5.6\Engine\Binaries\Win64\UnrealEditor-Cmd.exe' `
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
