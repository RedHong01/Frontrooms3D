# Task brief: stop iCloud syncing Frontrooms3D, and fix path dependencies

Written by the visual chat (游戏视觉) on 2026-10-03, for a separate chat, as Red asked. Give this whole file to a new chat as its task.

**Work on the REAL project folder at the absolute path below,** not in a git worktree or a copy.

## Red's request and decision
- Red: "Turn off iCloud sync for this project, because it affects GitHub tracking. Avoid any related path-dependency problems; fix them all. Give this task to a separate chat."
- Approach Red approved:
  - Rename the repo folder to `Frontrooms3D.nosync`, which iCloud skips.
  - Put a relative symlink `Frontrooms3D -> Frontrooms3D.nosync` at the old path.
  - Move iCloud conflict copies to a quarantine folder outside the repo. Delete nothing.
- iCloud's cloud copy of the folder goes away; it stays recoverable from iCloud "Recently Deleted" for 30 days. Local files stay.
- Do **not** turn off "Desktop & Documents" in System Settings.

## Where
- Repo, the git toplevel and the Unity 6000.3.10f1 project: `/Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01 Individual Game Project/Frontrooms3D`.
- The parent folder stays synced.

## Why
- iCloud left about 2,350 conflict copies ("name 2", "name 3", …), 1,526 of them cloud-only. They include 66 in `Library/Bee/PlayerScriptAssemblies`.
- A stale `Assembly-CSharp 4.dll` there made builds serialise `FrontRooms3DGame` with an old layout. That causes "level0 is corrupted" in WebGL and corrupts the Mac cloud build. Details: `Documentation/research/webgl/01_platform_and_build.md`.
- There are 7 copies of `.git/index`.
- Two conflict copies are **tracked by git**: `AudioSource/FMOD_Library/LIBRARY 2.json` and `ProjectSettings/ShaderGraphSettings 3.asset`. Leave them where they are and list them for Red. He commits through GitHub Desktop; don't commit unless he asks.

## Ready script (dry run by default)
- Path: `/private/tmp/claude-501/-Users-redwang-Desktop-ArtCenter-Fall26T7-EGAM-401A-01-Individual-Game-Project/5656cffd-bc90-45f6-86a3-09b26549df8d/scratchpad/icloud_nosync.py`. If that temp path is gone, rebuild it from the steps here.
- What it does:
  1. Quarantines untracked conflict copies into `<parent>/_icloud_conflicts_quarantine.nosync/<relative path>`. It skips git-tracked files and folders that contain them, and skips `AudioSource/` and `FMOD/` (the sound chat cleans those; message it the list).
  2. Downloads the two real cloud-only cache files: `Library/Bee/artifacts/2000b0aE.dag/Unity.Collections.dll.mvfrm` and `…/Unity.PerformanceTesting.dll.mvfrm`.
  3. Renames the folder and creates the symlink.
- It refuses to run while Unity is open on the project or GitHub Desktop is running. Ask Red to quit them; never kill his apps.
- The last dry run (2026-10-03 11:4x) would move 2,339 items: Library 2,260, Builds 52, UserSettings 16, .git 7, Logs 4.

## Extra items found later (WebGL plan rev 5, 2026-10-03 13:0x)
- Conflict copies outside `Library/Bee`:
  - `Library/ScriptAssemblies/Assembly-CSharp-Editor 2.dll` and `.pdb`. The script covers these; check them.
  - `ProjectSettings/ShaderGraphSettings 3.asset`. It is git-tracked, so it waits for Red.
- **The ray-traced glass plugin's install name is an absolute path into the project.** `Assets/Plugins/macOS/libFrontRoomsMetalGlassRT.dylib` (G14 review item R30) has it as its LC_ID_DYLIB.
  - Check with `otool -D`.
  - It works through the symlink, but it should become `@rpath/…` or `@loader_path/…`.
  - The visual chat owns that plugin (task G14, clone `proj_rt`). Report it to the visual chat; don't rebuild the plugin in the real project.
- WebGL G0 console checks will only be read on a build made **after** this task finishes. Tell the WebGL perf chat ("WebGL 卡顿性能分析") when you're done.

## Path-dependency audit (fix everything)
- **Claude Code memory and session keys.** `~/.claude/projects/<cwd-with-dashes>` is keyed by the cwd string. If any tool now opens sessions under a `.nosync` path, make that key resolve to the same memory dir (for example a symlinked dir). Don't edit other sessions' memories.
- **Unity Hub** project entry: the project must open and compile cleanly. In a quick batch run, note what `Application.dataPath` reports. Grep `Assets/` for absolute-path or prefix comparisons.
- **GitHub Desktop and git:** `git status` / `git log` must be identical before and after. Note that `git rev-parse --show-toplevel` prints the physical path.
- **Hard-coded paths in the repo:** grep for `Desktop/ArtCenter/Fall26T7` and `EGAM-401A-01 Individual Game Project` across:
  - `Tools/`, `Documentation/`, `Assets/Editor/*.cs`, `ProjectSettings`;
  - `Packages/manifest.json`, `.vscode`, launch.json files, `NativePlugin/` build scripts;
  - FMOD settings (`Assets/Plugins/FMOD/Resources/FMODStudioSettings.asset`).

  Fix mechanical cases you clearly own. List the rest, each with its owner chat.
- **Tell every chat what changed:** the real path is now `…Frontrooms3D.nosync`, the old path still works, and they must never hard-code `.nosync`. Also tell them where the quarantine is. Chats: 游戏视觉 (visual), 关卡设计 (map), 声音设计 (sound), 平面视觉 (graphic), WebGL 卡顿性能分析 (WebGL perf), 动画和动态图形模型技术 (wallpaper print), 系统设计, and the title chat if it is running. Their batch jobs run in private APFS clones under `/private/tmp` and are unaffected.
- **iCloud:** confirm the `.nosync` folder is no longer tracked. Watch for about 10 minutes: no new " 2" files should appear.

## Report
- Write `Documentation/ICLOUD_NOSYNC.md`. It should cover:
  - what was done;
  - the quarantine location and counts;
  - the git-tracked conflict copies awaiting Red;
  - the path-audit table;
  - how to undo: remove the symlink, rename back.
- Tell Red in plain Chinese what changed and whether he must do anything, for example reopen Unity Hub's entry.
