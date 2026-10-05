# FMOD Win64 bank rebuild

The regenerated Desktop bank payload is now byte-for-byte identical across
`Assets/StreamingAssets/FMOD` and the Unreal staging directory. The Mac FMOD
2.3.15 runtime probe loads all five banks, starts a real event instance, and
resolves all 31 Unity events, including `StreamOpen`, `StreamClose`, and
`StreamLock`. A Win64 probe is still required before the migration gate can be
closed.
The source project file is an 88-byte empty `<objects />` root; its authored
event data is present as 31 files under `FMOD/FrontRooms/Metadata/Event`.  This
Windows can verify the bank payload but cannot rebuild it because FMOD Studio's
`fmodstudiocl` is not part of the Unity plugin or Unreal install.

## Build on the Unity/Mac side

Run this from the repository root on a Mac that has the matching FMOD Studio
desktop/CLI installation.  Keep the generated files on a branch until the
strict Windows probe passes.

The Unity-shipped Win64 runtime reports FMOD Studio API version **2.3.15
(build 168126)**, and the source metadata uses serialization model
`Studio.02.03.00`; use a compatible FMOD Studio release when opening the
project.

```sh
cd /path/to/Frontrooms3D
python3 Tools/audio/fmod_frontrooms.py all

# Use the fmodstudiocl shipped by the installed FMOD Studio application.
fmodstudiocl \
  -script Tools/audio/build/fmod_build_frontrooms.js \
  FMOD/FrontRooms/FrontRooms.fspro
```

The builder writes the Desktop banks below
`FMOD/FrontRooms/Build/Desktop`.  Copy exactly these five files to
`Assets/StreamingAssets/FMOD` and commit their hashes with the Unity source
change:

```sh
for bank in Master.bank Master.strings.bank Ambience.bank SFX.bank Music.bank; do
  cp "FMOD/FrontRooms/Build/Desktop/$bank" "Assets/StreamingAssets/FMOD/$bank"
done

# Refresh the committed Unity-side hash snapshot after the bank change.
python3 Tools/UnrealMigration/sync_unity_unreal.py --update
```

Do not copy platform-specific banks or Unity `.meta` files into Unreal by hand.
The Windows sync/pipeline stages the Unity source banks into
`Migration/Unreal/Content/FrontRooms/Audio/FMOD/Banks` after the hashes are
updated.

## Verify on Windows

Run the probe without `--allow-missing-event`; a successful rebuild must report
31 resolved events and render a WAV from a real FMOD event instance.

```powershell
$uePython = 'D:\UE_5.8\Engine\Binaries\ThirdParty\Python3\Win64\python.exe'
& $uePython Tools\audio\fmod_bank_probe.py `
  --root D:\Frontrooms3D `
  --write-manifest `
  --render D:\Frontrooms3D\Migration\Unreal\Saved\fmod-bank-rebuild.wav
```

Then run the normal Windows-only staging, smoke, and package checks:

```powershell
& $uePython Tools\UnrealMigration\sync_unity_unreal.py --root D:\Frontrooms3D --check
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools\UnrealMigration\run_unreal_asset_pipeline.ps1 -Root D:\Frontrooms3D
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools\UnrealMigration\smoke_test.ps1 -Root D:\Frontrooms3D
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools\UnrealMigration\build_windows.ps1 -Root D:\Frontrooms3D
```

`Migration/exports/fmod_bank_manifest.json` records the runtime DLL hash, all
five bank hashes, the 31 metadata-backed expected paths, the event paths
actually found in the loaded banks, and the missing set. The checked-in record
currently uses the macOS runtime and is a preparation artifact; Windows must
rewrite it with the Win64 DLL. A manifest with
`contract.status = "source-bank-drift"` is an explicit incomplete gate; it must
not be treated as a complete FMOD migration.
