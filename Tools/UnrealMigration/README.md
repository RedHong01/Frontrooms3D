# FrontRoomsss Unreal migration tools

This directory contains the first migration slice that can run without the
Unreal Editor:

- `export_contract.py` reads the committed Unity source assets and writes
  `Migration/exports/frontrooms_contract.json`.
- `map_hash_reference.py` is a Python reference for Unity's unchecked
  `MapHash`/xorshift functions. It is the reference oracle for the future UE
  C++ implementation.
- `test_*.py` are source-backed checks for the exporter and hash contract.

Run from the Unity project root of this worktree:

```bash
python3 Tools/UnrealMigration/export_contract.py
python3 -m unittest -v Tools/UnrealMigration/test_export_contract.py Tools/UnrealMigration/test_map_hash_reference.py
```

The JSON output is deliberately portable: source paths are repository-relative,
the four authored room modules are expanded into data, the 31 FMOD event paths
and 19 parameters are preserved, and the deterministic map constants and
validation seeds are explicit. It is an input contract for an Unreal
DataAsset/commandlet importer, not a replacement for runtime generation.
