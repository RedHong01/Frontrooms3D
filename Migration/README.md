# FrontRooms migration workspace

This directory is the migration-side source of truth that can be maintained
before the Unreal Editor is installed.

Read [`UNITY_UNREAL_SYNC_POLICY.md`](UNITY_UNREAL_SYNC_POLICY.md) before
changing either side. Every Unity-side change must have a corresponding Unreal
assessment, implementation or explicit no-change record, regenerated contract
outputs, and verification status.

## Current artifacts

- `exports/frontrooms_contract.json` is generated from the committed Unity
  source by `Tools/UnrealMigration/export_contract.py`.
- `exports/unity_sync_manifest.json` is the cross-platform Unity→Unreal
  snapshot. It records repository-relative SHA-256 identities for Unity
  source/data/assets and the generated migration inputs; use
  `Tools/UnrealMigration/sync_unity_unreal.py --check` on Windows before
  importing or building.
- `contract.schema.json` describes the stable top-level shape consumed by the
  future Unreal DataAsset importer.
- `UnrealCore/` contains engine-independent C++ contracts that can be checked
  against the Python reference before they are wrapped in UE types.

The first C++ check can be run without UE:

```bash
clang++ -std=c++17 -Wall -Wextra -pedantic \
  Migration/UnrealCore/frontrooms_map_hash_test.cpp \
  -o /tmp/frontrooms_map_hash_test
/tmp/frontrooms_map_hash_test
```

The generated contract keeps repository-relative paths and source hashes. It
records the authored profile/modules, map units, validation seeds, FMOD event
and parameter names, and the current asset inventory. It is deliberately not a
rendered level: FrontRooms creates its world at runtime, so importing a scene
would lose the gameplay contract.
