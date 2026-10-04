# Build G3: keys, tags, hosts and door signage

Status: IN PROGRESS (2026-10-03, second run). Spec: `10_spec.md` §2.4, §3.2, §3.3, §4, §9.3, §10.0, §10.3.

Built into the private clone only (`scratchpad/proj_int`), previews in `scratchpad/interact_previews/G3`. Nothing under the real project's `Assets/` was touched. Unity was not run.

## Blocking finding: `Kit_KeyBoard` overwrote `Kit_Keyboard`

The spec's name `Kit_KeyBoard` differs from the existing desk keyboard `Kit_Keyboard` (`assets/keyboard.py:50`, used by `FrontRoomsOfficeKit.cs:47`) only in case. The first G3 run built `Kit_KeyBoard` into the clone, and on the case-insensitive macOS volume it overwrote `Kit_Keyboard.fbx/.json` there (the real project was not affected). Unity's `Resources.Load` is not case-safe either.

- Fixed: the asset now ships as **`Kit_KeyHookBoard`** (sidecar `meta.specName` = `Kit_KeyBoard`). The clone's `Kit_Keyboard.fbx/.json` were restored from the real project.
- Needs the map chat and the facade: §4.3's KeySpot `host` value is `Kit_KeyHookBoard`, not `Kit_KeyBoard`.

(Details per asset follow below as the run fills them in.)
