# D1.6 doors · locks · keys — package status: BLOCKED (2026-10-29 17:0x)

Written by the Package stage of workflow x11-d16-doors-locks-keys (wf_ab7fda06-341, 游戏视觉, account B, session local_a9a1dce2), 2026-10-29 17:0x PDT, after verify on the fix-2 build.

Nothing new was packaged. This stage wrote no `d16_doors_keys.sh`, no `contract_map_D16.md`, no `31_for_sound.md` and no VL rows. The fix-2 package `W/apply/d16.sh` and the contract `data/contract_map_D16.diff` (v7) are unchanged. The record of 2026-10-22 06:51 is kept below as history.

## Verdicts (verify after fix:2)

| Lens | Verdict |
|---|---|
| spec | NOT READY |
| engineering | NOT READY |
| look | NOT READY |

All three lenses are NOT READY, so the rule applies: do not package; write the blockers here; return BLOCKED.

## Re-checked on disk, 17:03–17:06

- `bash /Users/redwang/FrontRoomsVisualWork/apply/d16.sh` (plain dry run): **DRY RUN OK** on main `c5e03368` + tree, which now holds 关卡设计's first-room group (16:58): MapWorld `6bbc3341`, FrontRooms3DGame.cs `2e93c426`. The visual package itself still applies.
- Contract v7 (`data/contract_map_D16.diff`, bases MapWorld `527ddb39`, DoorShot `ef941ca9`, ModuleEditing `ed81b895`): `patch -p1 --dry-run` on a scratch copy of main's three files. **MapWorld: 2 of 4 hunks fail.** DoorShot and ModuleEditing apply. Main's MapWorld now makes every key through `NewKey` (line 5053). Its callers are `SpawnKey` (5030) and `SpawnFloorKey` (5081).
- Payload `Assets/Scripts/Office/FrontRoomsInteractableKit.Door.cs:87`: `ShowKitTypeShipped = false, ShowTagTypeShipped = true`.
- `data/fix2/held_pose_measure.json` measures one shape only (Rect, edit mode): centre x 1262 against the hand point 1248; 200 × 123 px against the 3× glyph's 216 × 144.
- Visual-line Unity jobs: 0. The 1-minute load was 40.6 at 17:03, so any timing taken now counts as UNDER LOAD.

## Blockers

### spec

**S1. The real key does not hand off to the HUD key at the same spot (KP-1 / DECISIONS #37).**
- The verifier measured the raw Play frames of run k03 (`proj_d16/Verification/d16k_hud/1920x1080/lit/pickup/`, Long / White / 00).
  - The last 3D frames centre at about (1182, 743).
  - The first HUD frames centre at about (1245, 741), which is on the hand point (0.65, 0.30).
  - So at the swap the key jumps about 64 px to the right, about 30 % of its width. The sizes match within 3 %.
- Shapes come from the zone hash, so about one key in three (Long) jumps. Round was never measured.
- `20_build.md` §12.1 S1 ("same spot and size on the swap frame") and the sheet label "HUD KEY, SAME SPOT" (`images/d16h_hud_pickup_sheet.jpg`) are contradicted by these frames.

**S2. The printed key-tag type ships without 平面视觉's sign-off, and the round-tag B10+ rule contradicts DECISIONS #33.**
- `ShowTagTypeShipped = true` prints the floor token on every tag. `10_design.md` §9 sends type sign-off to 平面视觉 before landing. The proof `images/d16h_type_proof_tags.jpg` is itself captioned "For their sign-off". No sign-off on the 3D tag is recorded in `handoff/graphic.md` or `GRAPHIC_VISUAL_TASKS.md`.
- #33 (平面视觉, 10-22): from B10 on, the token is set at 0.8× size on the same baseline, never scaled to fit. The build scales round-tag B10+ into the window (×1.20) instead.
- Face: the build's Courier Prime Regular follows #23 (Red confirmed, 10-22), which supersedes G12's Bold. Nothing to change here, but the record should say so.

### engineering

**E1. The map contract v7 no longer applies to main.**
- See the re-check above. Hunk 1 (about line 2325) fails because the call-site `if` gained `!floorKey`. Hunk 4 (about line 4994) fails because `SpawnKey` builds its cube through `NewKey` now. Hunk 3 only lands with fuzz 2. The anchor applier `data/apply_map_hunks_v4.py.txt` stops with "anchor count 0".
- There is also a functional gap. Main's M3 floor key ("Key · floor") is built by `SpawnFloorKey` through `NewKey`, and station floors spawn no zone keys. With v7 that floor key stays a glowing cube, so on station floors the real key for P4 / #37 never appears. Main's own comment says D1.6's key model replaces the cube through `NewKey`.

**E2. The map suites have not run on the final code.** The last `mapsuites` runs (i01 / i02, `20_build.md` §11) were before the fix-2 changes. Note: the verifier's text for this item was cut off in the stage input; this summary is what the files show.

### look

**L1. The held key reads dark brown, not brass, at a level look.**
- In `proj_d16/Verification/d16kcap/fix2_held_050.jpg` the key bow at the hand averages sRGB (78, 60, 27), which is 0.070 linear. The same key on the floor (`close_key_Flat.jpg`) averages (211, 185, 114), pale brass.
- The round-2 bar was 0.10 linear. Fix 2 met it only by replacing it with a "within 1.5 stops of what is behind it" rule.
- The Play evidence (0.23 linear) comes from a floor pickup with the camera pitched down. A13 hangs each floor's one key at the station, at about eye height, so the decided P4 pickup mostly happens at a level look, which is the case that reads brown.

**L2 (evidence gap, not a blocker today).** No capture shows a key taken off a hook (the A13 case). When a hung key is lifted, `FrontRoomsKeyAssembly` blends its parts from `LayHung` (ring yawed 120° and lifted) while the map's presenter moves the root straight to the hand. Whether the ring passes through the hook lip is unverified. It cannot happen in game until M3 hosts and A13 land.

## What the next fix round must do (one round, then one verify round, then package)

1. **Contract v8 on MapWorld `6bbc3341`** (and on Game.cs `2e93c426` if a hunk needs it).
   - Put the dressed-key hook in `NewKey`, so zone keys and the floor key both get the kit.
   - For a dressed root, drop the cube's 0.06 m lift in both callers (`keyY + .06f` in `SpawnKey`, `at + Vector3.up * .06f` in `SpawnFloorKey`). The dressed root sits on the surface point.
   - Keep `SpawnKeyHost` on every build, re-anchored to the new `if (data.hasKey && !floorKey && ...)` line.
   - Check with `git apply --check` and `patch --dry-run` on scratch copies. Then run Roslyn on main + payload + v8 for macOS and `--ios`.
   - Re-run the real-key hand-off with a station floor on. The floor key's identity already agrees with the HUD (both hash `FloorKeyZone`).
   - Optional: the door plates' FaceIdentity stand-in can use main's `KeyZoneAt(cell)`.
2. **S1 fix.** In `FrontRoomsKeyAssembly.PoseHeld`, use per-shape held offsets so that each shape's pixel footprint is centred on the `KeyHandoff` viewport point (as `held_arrangement.json`'s glyph anchor (36, 24) intends).
   - Re-measure Rect, Round and Long in Play at the swap. Pass: centre within a few px, size within ±5 %.
   - If sizes still differ, give the HUD one O-16 factor per shape.
   - Correct the §12.1 S1 claim and the sheet label.
3. **L1 fix (visual-owned), one of:**
   - a weak held-key fill on the HeldItem layer (like V5);
   - `Prop_BrassSatin` metallic about 0.7 with a warmer base colour;
   - a tilt of about 30° on the held arrangement so its face catches the ceiling.

   Pass: the level look and the down look both reach at least 0.10 linear and read as brass. If the held pose changes, redo `data/fix2/held_pose_measure.json` (O-16) together with item 2.
4. **S2 fix.** Set round-tag B10+ to #33 (0.8× on the same baseline). Rebuild the proof sheet and record #23 (Regular) as the face rule. The 游戏视觉 session routes the rebuilt proof to 平面视觉 · 信息设计 for sign-off.
   - Until that sign-off is recorded, ship `ShowTagTypeShipped = false`, as the plates already do. The trade-off: the token then appears only on the HUD glyph at the swap.
   - Or keep `true` only with the sign-off recorded in `20_build.md` and the graphic handoff.
5. **E2.** Run the map suites (plain and dressed variants) on the final code with contract v8.
6. **Optional (L2).** Capture a hung-key lift (rack hook → hand) to check the ring against the hook lip.

When the package is built: the task names `W/apply/d16_doors_keys.sh`, but this line's package is `W/apply/d16.sh` (v2 convention). Keep one name and say which one in the report. VL317–VL324 were reserved in 2026-10; the VL counter has moved past VL440 since. Re-read `Documentation/VERIFICATION_LOG.md`'s next free number before placing any rows from `W/vl_pending/d16.md`.

## Peer and Red actions

- **Peer (平面视觉 · 信息设计, routed by the 游戏视觉 session; this stage sends no messages):** sign-off on the 3D key-tag type after the #33 fix (item 4).
- **RED_ACTIONS: none.** Every fix is visual-owned or contract text for 关卡设计.

---

# History

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
