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
python3 Tools/UnrealMigration/sync_unity_unreal.py --update

# On Windows, before importing into Unreal:
python Tools/UnrealMigration/sync_unity_unreal.py --check
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
```

This writes `Migration/exports/unreal_material_profiles.json` with 91 source
profiles. It preserves Unity base color, metallic, smoothness, emission and
texture references; the UE material factory maps smoothness to `1 - Roughness`
and keeps Unity texture channel conventions explicit.

Audit reusable assets and sidecars on Windows with:

```powershell
.\Tools\UnrealMigration\audit_unreal_assets.ps1
```

The audit checks source and staged SHA-256 coverage, imported FBX package and
texture coverage, all 113 collider/anchor/LOD sidecars, material profile count,
and audio/bank/video/font inputs. It writes
`Migration/exports/unreal_asset_audit.json`. UE bounds/tangent messages remain
warnings until the editor asset factory applies sidecar collision and LOD
metadata; missing or malformed source data fails the audit. `smoke_test.ps1`
runs this audit as its `asset-audit` stage before compiling UE.

For a complete repeatable Windows asset pass (including restaging Unity files,
FBX/texture import and the audit), run:

```powershell
.\Tools\UnrealMigration\run_unreal_asset_pipeline.ps1
```

Use `-SkipImport` when the UE editor is open and only source/profile coverage
needs refreshing; use `-WhatIf` to preview the staging copy set.
