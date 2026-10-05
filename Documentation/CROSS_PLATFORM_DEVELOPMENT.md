# Mac / Windows development contract

This repository is shared between macOS and Windows. The rules below keep a
checkout reproducible on both case-insensitive and case-sensitive file
systems, and keep Unity's serialized assets from producing line-ending noise.

## One checkout per machine

`Frontrooms3D/` is the Git repository. A GitHub Desktop **linked worktree** is
local to the computer that created it; it is not copied by `git clone`. On a
new machine, clone the remote repository and check out the remote branch:

```sh
git clone https://github.com/RedHong01/Frontrooms3D.git
cd Frontrooms3D
git fetch origin
git switch --track -c unreal-migration origin/unreal-migration
```

Use the same branch for a shared change, commit it, and push it before opening
the project on the other machine. Do not copy a linked-worktree directory over
the top of another checkout.

## Git and text policy

Run these once in each clone:

```sh
git config core.autocrlf false
git config core.safecrlf true
python Tools/validate_cross_platform.py
```

The repository's `.gitattributes` and `.editorconfig` require UTF-8 and LF for
source, Unity YAML, JSON and documentation files. If an older checkout reports
CRLF files, make sure the worktree is clean and run:

```sh
python Tools/validate_cross_platform.py --fix-line-endings
```

On Windows, Git does not preserve Unix executable bits; set
`git config core.filemode false` locally. Keep the executable bit only on real
shell/native build tools. Do not commit generated `Library`, `Temp`, `Logs`,
Unity build folders, or `FMOD/FrontRooms/.cache` data.

The validator checks all tracked paths for case/Unicode collisions, Windows
reserved names and illegal characters, UTF-8/LF encoding, JSON/asmdef/input
syntax, Python syntax, and case mismatches in quoted project paths. Run it
before pushing and rely on the three-OS GitHub Actions job for pull requests.

## Unity contract

Use exactly Unity `6000.3.10f1` and the versions committed in
`Packages/manifest.json` and `Packages/packages-lock.json`. Open
`Assets/Scenes/FrontRooms3D.unity`. The authored URP assignments are part of
the project baseline; do not clear them in a build script.

For desktop builds, use the matching menu entries:

- **FrontRoomsss → Cloud Build macOS** writes a macOS build to the configured
  `FRONTROOMS_CLOUD_OUTPUT` directory (or the system temp directory).
- **FrontRoomsss → Cloud Build Windows** writes a Windows 64-bit build to the
  same output root.

These entries share product, scene, quality and URP settings. The legacy
**Build macOS** entry is retained for local work and now preserves URP too.
Use the Windows cloud entry instead of assuming a macOS-only method can build
Windows.

The macOS Metal glass plugin is an Apple-Silicon/OSX Editor enhancement. The
Windows build uses the URP glass fallback; it must not require the `.dylib`.
The current native binary is arm64-only, so an Intel macOS release requires a
separate x86_64 or universal binary before it can be called universal.

FMOD's platform plugin metadata still needs a clean Mac and Windows import/build
report before it is narrowed: some vendor `.meta` files advertise more editor
platforms than their native binaries support. Do not hand-edit those vendor
settings from a single OS; verify them in Unity's Plugin Inspector on both
machines and commit the resulting metadata together.

## Portable tool paths

Tools discover the project root from their file location. When a tool depends
on an external asset or helper, provide it through an environment variable:

- `FRONTROOMS_PROJECT_ROOT` — override the Unity project root.
- `FRONTROOMS_CLOUD_OUTPUT` — choose a build output root.
- `FRONTROOMS_ICONLIB_ROOT` — external `keyicon_design/` folder containing
  `iconlib.py` and `composites.py`, used by the Figma HUD tools.
- `FRONTROOMS_FIGMA_SCRATCH_ROOT` — optional scratch folder for the dark Figma
  audit frame.

Do not add usernames, `/Users/...`, `C:\Users\...`, temporary agent folders,
or machine-specific Unity paths to tracked source. Use `PROJECT_PATH` and
`UNITY_PATH` as documented in `Documentation/WEBGL_BUILD.md`.

## Change checklist

Before pushing a Mac/Windows change:

1. Run `python Tools/validate_cross_platform.py` and
   `python -m unittest discover -s Tools/UnrealMigration -p 'test_*.py'`.
2. Open the project with the pinned Unity version and let packages finish
   importing before editing scenes or prefabs.
3. Check the changed asset's exact case and its `.meta` file. Never fix a case
   mismatch by creating a second file with a different case on macOS.
4. Run the appropriate Cloud Build entry and record the output path and build
   target in the change description.
5. Commit generated verification output only when it is an intentional,
   reviewable artifact; keep machine caches ignored.
