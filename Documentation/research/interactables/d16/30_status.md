> **SUPERSEDED 2026-10-22 17:2x.** The Build stage re-run (account B, session local_a9a1dce2) built and packaged D1.6: see `20_build.md` (status READY: `W/apply/d16.sh` dry run OK; the map contract `data/contract_map_D16.diff` for 关卡设计). The text below is the 06:51 record of the first run, kept for history.

# D1.6 doors · locks · keys — package status: BLOCKED

Written by the Package stage of workflow wf_c071884a-103 (游戏视觉), 2026-10-22 06:51 PDT.
Nothing was packaged. No apply script, contract or VL spec was written.

## Verifier verdicts (round 2)

| Lens | Verdict |
|---|---|
| spec | NOT READY |
| engineering | NOT READY |
| look | NOT READY |

All three lenses are NOT READY, so the packaging rule applies: do not package. The rule says to write the blockers here and return BLOCKED.

## Cause

Every stage before fix:2 failed on a network outage. Each one ended with "API Error: Can't reach the API server ... (ENOTFOUND)". The journal is `~/.claude/projects/-Users-redwang-Desktop-ArtCenter-Fall26T7-EGAM-401A-01-Individual-Game-Project/c662b9be-caf5-4011-a0a6-49417021dfbd/subagents/workflows/wf_c071884a-103/journal.jsonl`. These stages are all marked `failed`:

- design
- build
- capture
- verify ×3, round 1
- fix:1
- verify ×3, round 2

Design only read files and wrote nothing. fix:2 ran at 06:32 with the API reachable again. It found no build to fix and changed nothing.

## Blockers (re-checked 06:50 PDT)

1. **No clone.** `/Users/redwang/FrontRoomsVisualWork/proj_d16` does not exist, and no Unity process is running on it. The only Unity process is the map chat's `~/FrontRoomsMapWork/proj2`.
2. **No visual-side code.** None of these exist anywhere, in main or in the clones:
   - `FrontRoomsDoorRig` on "Leaf rig" (IFrontRoomsDoorRig)
   - `FrontRoomsKeyAssembly` on "Key · zone {id}" (IFrontRoomsKeyRig)
   - the V3 HeldItem layer and the desktop pass with the 1.5 × HeldDistance depth rule
   - the V4 post driver: it reads FrontRoomsDoorShot.Weight and FrontRoomsGlassShot.Weight and forces LOD0 on the shot door
   - the V5 0.6 shot light during Unlock
   - the V6 notes
3. **The 3 RD10 materials are still missing.** `Door_Enamel`, `Prop_KeyTagNo` and `Prop_SignEngraved` are not in `Assets/Resources/Surfaces`. There is no RenderSetup SurfaceDefs hunk.
4. **No map-side contract.** No unified diff exists for any of these:
   - DressDoor returning "Leaf rig"
   - the "Key · zone {id}" hookup
   - the shot weight reads
   - the A2 hinge-pivot fix

   That also means no base sha1 re-check (MapWorld c0946262, CameraRig a77221fb, ShotTimings 865e3e10) and no `git apply --check`.
5. **No checks.** There is no compile log, GUID collision check, Roslyn check (macOS and --ios) or map-suite pass.
6. **No evidence.** This folder held no 20_build.md and no images before this file. `/Users/redwang/FrontRoomsVisualWork/vl_pending/d16.md` does not exist. VL317–VL324 stay reserved and empty.
7. **Red's A09 / SD04 decisions are unverified.** None can be checked against frames or logs:
   - doors swing in
   - 0.45 m pull step-back
   - deadbolt + knob
   - an unlocked door looks locked but opens on E
   - clear Lobby glass
   - bent-slat blinds
   - LOD0/1/2
   - dark-bronze Run frame
   - "key rack"
   - T trims
   - C14 look + E
   - C15 1 in 3
   - broken stays broken
   - B·4
   - no motor or closer

   The same goes for the 10_spec anchors and budgets, the KP-1 / DECISIONS #18 / #20 seam, and G12 Courier Prime Bold.

## Machine state

At 06:50 the load average was 135.72 / 192.96 / 337.78 (1, 5 and 15 min). The 1-minute figure is above 32, so any timing taken now counts as UNDER LOAD. The main load is macOS StorageManagement: ApplicationsStorageExtension at about 234 % CPU, StorageManagementService and CacheDelete `deleted`.

## What unblocks it

1. Stop this run.
2. Relaunch the d16-doors-locks-keys workflow from Design with the same inputs and the same VL317–VL324 range.
3. Build must create `proj_d16` with `mkclone.sh` and compile with 0 'error CS'. It must:
   - implement the D1.6 row of VISUAL_CHAT_TASKS.md §0a′ on visual-owned paths only
   - add the minimal SurfaceDefs hunk for the 3 materials
   - write the map contract diff, including the A2 hinge-pivot fix, against main's current sha1s
4. Capture and Verify then run on real frames and logs.
5. At package time, re-base onto outlets_p4b and w2_office_props with `tools/rebase_apply.sh`.

If possible, relaunch once StorageManagement load drops.

This stage created no clone, so it cannot collide with the relaunch.
