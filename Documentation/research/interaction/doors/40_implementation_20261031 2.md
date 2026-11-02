# Body-door source integration · 2026-10-31

Red explicitly assigned Codex: “由 Codex 继续修复并接入，仅限门交互”. This supersedes the earlier runner-only code restriction and implementation wait for doors only. Mapping and all non-door work remain paused. No commit/push/build/upload/production activation is included.

The 1–11 selections in 20_design.md §00 are implemented behind default-off FrontRoomsLevelProfile.bodyDoors and FRONTROOMS_RELAY_V2. Optional assistance remains unapproved and is not built.

## Behavior

- Body contact: slow lean/follow with exact partial rest, walk push, frontal sprint barge within 60°, 0.15 s sprint-release grace. No ordinary USE open/shut.
- Only turn, aim at latch half 0.30 s and step backward 0.12 s to pull shut; a pull within 2 s of sprinting through slams. No SHUT BEHIND or accessibility USE setting.
- Slow locked contact is silent; doors still sound while footsteps are quiet. Blocked special/ordinary contact feeds movement before stamina.
- Alarm bars resist barging, grazing, slow and backward contact; frontal straight full walking push for 0.4 s triggers them.
- Key shot ends at 10° without a movement/noise event or forced body step. First body push owns the opening noise. DoorShotTests and key/exit bots are adapted.
- Actual initial double-door pair and stairwell north leaf share contact logic. Loading/readiness keeps them latched; readiness never opens them. Existing scripted seal remains.
- Held Relay open begins a break at 0.6 s and no longer aborts because its leaf is marked open. A stopped break throw catches the player; EXIT and stairwell sanctuary checks remain.
- Ordinary body-door jambs alternate, so perpendicular leaves avoid a shared corner pocket; map connectivity/locks remain the same. EXIT keeps its dedicated hinge contract.
- Door hints and quiet-footstep wording added to all nine language tables; 12 local CJK text font subsets updated. Mapping layouts and animations remain paused.

## Validation / activation

Evidence: /Users/redwang/FrontRoomsVisualWork/bodydoors_codex_20261031/ and workspace_repo/remote/runner/out/door-interaction-20261031.md.
Core behavior/corner checks 41/0; actual special adapters plus helper 21/0; extended DoorShot suite 257/0. Production-off macOS/iOS and v2 configurations compile. Runtime bot acceptance is reported separately; do not treat edit-mode checks as activation evidence.

The original bot waited 2.6 m out for an automatic open; it now proceeds on readiness after the fixed hold and physically pushes. North-door and unlock-crack routes also physically push. Chase acceptance requires a real Relay chase as well as barge/pull/slam.

Keep production bodyDoors off. Red selected activation alongside Relay v2 after the combined test, ordinary body-door bot and actual chase barge/pull bot. Source integration is not a release or a device playtest. Real controller/touch feel and the consolidated build remain acceptance work.

Final runtime acceptance: ordinary body bot PASS (198 actual cells, 15 body opens, 0 errors); controlled real chase PASS (1 actual chase, 5 barges, 5 pull slams, 0 missed pulls/errors). Actual key/EXIT/north-door traversal PASS through three downward transitions to floor 4, three keys, 27 EXIT checks and 19 station checks with no failed rows or runtime errors. This uses the real controller and shots in a silenced editor clone; fixed-step helper and test flags are clone-only. Production activation still waits for the agreed Relay v2 and combined-test gate.
