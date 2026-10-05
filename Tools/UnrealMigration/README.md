# FrontRooms Unreal migration tools

This directory contains the first migration slice that can run without the
Unreal Editor:

- `export_contract.py` reads the committed Unity source assets and writes
  `Migration/exports/frontrooms_contract.json`.
- `sync_unity_unreal.py` hashes Unity-side source/data/assets and all generated
  migration inputs. It works with Python 3 on macOS or Windows, emits a
  reviewable `unity_sync_manifest.json`, and reports exactly what the Windows
  Unreal work item must update.
- `map_hash_reference.py` is a Python reference for Unity's unchecked
  `MapHash`/xorshift functions. It is the reference oracle for the future UE
  C++ implementation.
- `test_*.py` are source-backed checks for the exporter and hash contract.

Run from the Unity project root of this worktree:

```bash
python3 Tools/UnrealMigration/export_contract.py
python3 -m unittest -v Tools/UnrealMigration/test_export_contract.py Tools/UnrealMigration/test_map_hash_reference.py

# On a Mac Unity workstation (after the contract, kit, asset-bridge and import-settings exporters):
"/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity" \\
  -batchmode -nographics -quit -projectPath . \\
  -executeMethod FrontRoomsUnrealChunkExporter.ExportGoldenChunksCommandLine \\
  -frontRoomsExportRadius 1 -frontRoomsExportSeeds 2554,20388,20261001 \\
  -logFile Migration/exports/unity_chunks/golden_export.log
python3 Tools/UnrealMigration/validate_golden_chunks.py --root .
python3 Tools/UnrealMigration/sync_unity_unreal.py --update

# On Windows, before importing into Unreal:
python Tools/UnrealMigration/sync_unity_unreal.py --check
python Tools/UnrealMigration/validate_golden_chunks.py --root .
```

The JSON output is deliberately portable: source paths are repository-relative,
the four authored room modules are expanded into data, the 31 FMOD event paths
and 19 parameters are preserved, and the deterministic map constants and
validation seeds are explicit. It is an input contract for an Unreal
DataAsset/commandlet importer, not a replacement for runtime generation.

The sync manifest uses repository-relative POSIX paths and SHA-256 bytes, so a
Mac checkout and a Windows checkout produce the same file identity. It includes
Unity GUID sidecars, authored scenes/modules, gameplay/rendering code, models,
textures, fonts, audio/video, FMOD source and project/package settings while
excluding Unity/Unreal caches. The Unreal project is checked separately and is
required to declare `TargetPlatforms: ["Win64"]`.

Before creating UE material instances, export the Unity URP surface values:

```powershell
node Tools/UnrealMigration/export_unreal_material_profiles.mjs
node Tools/UnrealMigration/export_unreal_material_factory.mjs
```

This writes `Migration/exports/unreal_material_profiles.json` with 91 source
profiles. It preserves Unity base color, metallic, smoothness, emission and
texture references; the UE material factory maps smoothness to `1 - Roughness`
and keeps Unity texture channel conventions explicit.

The factory manifest at `Migration/exports/unreal_material_factory.json` is the
repeatable UE 5.8 material/texture contract. It preserves A (sRGB base color),
N (linear tangent normal), S (linear smoothness, inverted to UE roughness), E
(linear emissive), M (linear mask/macro data), and P (sRGB print/decal). It
resolves Unity texture GUIDs to `/Game/FrontRooms/UnityImported/Textures`
assets and carries the original scalar/color values. The Windows editor
material factory consumes this manifest and writes the serialized UTexture2D
settings (`SRGB`, `CompressionSettings`, `bNormalizeNormals`, and mask alpha
compression) to every imported texture, including source textures that are not
yet referenced by a material profile:

```powershell
& 'D:\UE_5.8\Engine\Binaries\Win64\UnrealEditor-Cmd.exe' `
  'Migration/Unreal/FrontRoomsUE.uproject' -run=FrontRoomsMaterialFactory `
  '-Manifest=Migration/exports/unreal_material_factory.json' `
  '-Report=Migration/exports/unreal_material_asset_factory.json'
```

The report is required to show 167/167 applied textures and zero errors. A
second run is deterministic and reports `changed=0`; this is the evidence that
the settings are serialized in the `.uasset` files rather than remaining only
metadata. The Windows audit consumes this report when it exists, and the
smoke gate runs the editor factory before runtime checks.

Export the portable audio import contract from the same Unity source checkout:

```powershell
node Tools/UnrealMigration/export_unreal_audio_import_settings.mjs
```

It records the selected Unity/FMOD WAV files and repository-relative UE
destinations. Windows imports it with `import_unity_audio.ps1`; the generated
SoundWave packages are included in the asset audit and commandlet smoke gate.

Win64 keeps the original FMOD path as well. The five bank files are staged
under `Migration/Unreal/Content/FrontRooms/Audio/FMOD/Banks`, and
`FrontRooms.Build.cs` stages the Unity FMOD `fmodstudio.dll` beside the Win64
binary. The runtime bridge dynamically loads that DLL and emits migrated FMOD
events, with SoundWave assets as a fallback. The `fmod-banks` smoke stage runs
`Tools/audio/fmod_bank_probe.py`: it loads all five banks, resolves the Unity
contract, starts an event, and renders a real WAV block. The three missing
`StreamOpen`, `StreamClose` and `StreamLock` events remain visible in
`pendingCoverage` until the Unity bank build is regenerated.

Audit reusable assets and sidecars on Windows with:

```powershell
.\Tools\UnrealMigration\audit_unreal_assets.ps1
```

The audit checks source and staged SHA-256 coverage, imported FBX package and
texture coverage, all 113 collider/anchor/LOD sidecars, material profile count,
and audio/bank/video/font inputs plus the eight imported SoundWave packages. It writes
`Migration/exports/unreal_asset_audit.json`. UE bounds/tangent messages remain
warnings until the editor asset factory applies sidecar collision and LOD
metadata; missing or malformed source data fails the audit. `smoke_test.ps1`
runs this audit as its `asset-audit` stage before compiling UE.

The FBX importer warning gate has a reversible single-asset probe. It duplicates
one imported `UStaticMesh` into a transient package, enables degenerate-triangle
removal and MikkTSpace normal/tangent recomputation on the duplicate, builds it,
and writes the bounds and build diagnostics without saving the source `.uasset`:

```powershell
$ue = 'D:\UE_5.8\Engine\Binaries\Win64\UnrealEditor-Cmd.exe'
& $ue 'Migration/Unreal/FrontRoomsUE.uproject' -run=FrontRoomsMeshBuildProbe `
  '-Asset=/Game/FrontRooms/UnityImported/Props/Kit_Binders/Kit_Binders' `
  '-Report=Migration/exports/unreal_mesh_build_probe_binders.json' `
  -unattended -nop4 -nullrhi -nosplash -NoSound -stdout -UTF8Output -NoZenStore
```

The commandlet accepts `-RemoveDegenerates=`, `-RecomputeNormals=`,
`-RecomputeTangents=` and `-UseMikk=` for comparison probes. The checked-in
reports record the source SHA-1, the before/probe settings and
`sourceModified:false`. On UE 5.8.3, the `Kit_Binders` probe removes the
RenderData-vs-MeshDescription bounds warning with no build errors. The
`Kit_DoorFrame_Steel_LOD0` probe still reports near-zero tangents and
bi-normals after the same settings; that is source geometry/UV data requiring a
Unity-side re-export or an explicitly reviewed per-asset exception. Do not
mass-reimport with these settings until the source asset has passed this probe
and a visual smoke comparison.

The UE sidecar importer validates the same 113 JSON contracts in a commandlet
and writes `Migration/exports/unreal_sidecar_report.json`:

```powershell
& 'D:\UE_5.8\Engine\Binaries\Win64\UnrealEditor-Cmd.exe' `
  'Migration/Unreal/FrontRoomsUE.uproject' -run=FrontRoomsSidecar `
  '-ApplyToAssets' '-Sidecars=Assets/Resources/Props/Models'
```

`smoke_test.ps1` runs this commandlet automatically with `-ApplyToAssets`. The
factory resolves each prop folder (including FBX variants whose internal mesh
name differs), writes the 55 Unity box colliders to the mesh `BodySetup` in
centimetres, applies the 59 LOD screen-size ratios, and stores the 490 anchor
names plus source hash/axis/units in package metadata. It writes applied,
skipped and error arrays to `Migration/exports/unreal_sidecar_asset_factory.json`.
The operation is repeatable and replaces only the generated simple box set;
the runtime `ApplyBoxColliders` helper still converts Unity metres when it
creates transient `UBoxComponent` shapes.

For a complete repeatable Windows asset pass (including restaging Unity files,
FBX/texture/audio import and the audit), run:

```powershell
.\Tools\UnrealMigration\run_unreal_asset_pipeline.ps1
```

Use `-SkipImport` when the UE editor is open and only source/profile coverage
needs refreshing; use `-WhatIf` to preview the staging copy set.

Open the Windows-only UE 5.8.3 editor with the workspace-local profile and the
saved runtime map:

```powershell
.\Tools\UnrealMigration\open_unreal_editor.ps1
```

The launcher sets `UE_SKIP_UBT_SDK_SETUP=1` so UE does not invoke the failing
all-platform `dotnet` SDK probe on startup. The project still builds only
Win64; this only removes checks for unavailable Mac/mobile/Linux SDKs.

The launcher also starts the editor with `-dx12 -sm6`. The project config keeps
`DefaultGraphicsRHI=DefaultGraphicsRHI_DX12`, targets `PCD3D_SM6`, removes the
D3D11 SM5 target, and enables `r.Nanite.ProjectEnabled=1` so Nanite assets do not
trigger the missing-project-settings prompt. The smoke gate runs
`validate_nanite_config.mjs` and fails if the project is changed back to a
non-Win64 or SM5 renderer target.
