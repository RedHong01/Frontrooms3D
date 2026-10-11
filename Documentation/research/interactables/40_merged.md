# 40 — Interactables kit: merge into main

Status: **READY, NOT APPLIED (merge stage, 2026-10-08 19:2x–19:5x).** Written by the interactables-kit workflow (visual chat), merge stage after the fix pass (`30_final.md`).

The verified finals are packaged and checked over today's main. The dry run against Red's project passes. This stage did not write into Red's project: Red's merge rule (d) says subagents finish at a dry-run-OK apply script, and the visual chat runs `--apply`.

**Run it (visual chat):**

```
bash /Users/redwang/FrontRoomsVisualWork/apply/interactables_kit.sh            # dry run: must print DRY RUN OK
bash /Users/redwang/FrontRoomsVisualWork/apply/interactables_kit.sh --apply    # must end with MANIFEST OK
```

A later dry run prints `ALREADY APPLIED`. That holds even after Unity has re-serialised the metas (see §3).

**Main moved during this stage, and the package was rebased.** At 19:40 the visual chat applied the EXIT-sign package (`W/apply/MERGED.md`, 19:5x row). It changed two of this package's paths and one of the files the verification relied on:
- `Kit_DoorFrame_Steel.json` and `Kit_DoorFrame_Steel_Alu.json`: one anchor value, 2.28 → 2.18.
- `FrontRoomsKitLibrary.cs`: `4eb2e47d` → `a2c7ebc8`.

`W/tools/rebase_apply.sh interactables_kit` (19:45) returned **PASS**:
- both JSON files merged cleanly in a 3-way merge: main's anchor value stays, and our slot-list change is added (new payload sha1 `60ab758…`, `4fb7491…`);
- the dry run was OK.

The track's own gates were then re-run (rule c) on a fresh `mkclone.sh` clone of main as it stood after the exit-sign merge, `W/proj_merge_interactables_kit_3`, plus the rebased payload:
- compile: 0 `error CS`;
- `MikKitProbe`: 57/57 PASS;
- `FrontRoomsWindowTF.RunAll`: kitoff 8,847/0, kitthrow 307/0, kit 20,391/0, W-OF 4,054 / 1,458 tris.

`interactables_kit_keep.txt` now records the `FrontRoomsKitLibrary.cs` that was verified (`a2c7ebc8`). The final dry run, 19:5x, was clean: 102/102 bases, 102 metas, 3 code notes as verified. The rebased package was also tested on a fresh scratch copy: `MANIFEST OK`, then `ALREADY APPLIED`.

---

## 1. What was merged, and from where

- **Input:** the fix stage's verified payload `W/int_work/fix/payload/` (`W` = `/Users/redwang/FrontRoomsVisualWork`). It holds 102 files: 50 `Kit_*.fbx` and 52 `Kit_*.json` under `Assets/Resources/Props/Models/`. Its sha1 list is `SHA1SUMS.txt`. All 102 still equal `W/proj_int`, and none drifted (checked 19:28).
- **The verdict this merge rests on:** the fix stage finished (`30_final.md`). Every model gate it ran passed:
  - the door gap test, T1;
  - LOD and cull, T7;
  - key fit, T8/T8b;
  - hung pose;
  - the H7 dip;
  - exit-device clearance.

  Its FLAGs are about materials that do not exist yet (Level 0 readability: `Door_Enamel`, `Prop_KeyTagNo`) and about map work. Neither is a model fault, and this merge changes neither.
- **What was left out, and why:**
  - **7 FBX that differ from main only in their FBX export timestamp** (`fbx_diff.json`): `Kit_DoorLeaf_Steel`, `_SteelLite`, `_Steel_PaintedMetal`, `Kit_DoorLeaf_Veneer`, `Kit_WindowFrame_Wood` and both mini-blinds. Main keeps its copies.
  - **No `.meta`, no C#, no material, no clone harness.**
  - **No visual-owned code changes.** `FrontRoomsKitImporter.cs`, `FrontRoomsKitLibrary.cs` and the window facade are unchanged; the kit verified against main's copies.
- **Base:** main `bc0aa79` plus its working tree. `Assets/Resources/Props/Models` is clean there and unchanged since the fix stage recorded its bases on `22bb75f` (102/102 equal).
  - Main moved after `22bb75f` (`bc0aa79` 01:47 plus about 100 uncommitted paths: map, shots, touch, audio).
  - None of those paths are in this package.
  - The map chat's new `FrontRoomsKitSeam.cs` and `FrontRoomsDoorShot.cs` define the door and key rig interfaces, but nothing in main spawns a door, lock or key kit yet. The only kits the game draws are the window frames, through `FrontRoomsInteractableKit.Window.cs:112`.

## 2. Kit by kit: what changes

The full table is `merge/change_table.md` (machine copy `merge/change_table.json`). It compares main's sidecar and FBX with the payload. Summary:

| Group | Kits | Geometry | Materials | Anchors |
|---|---|---|---|---|
| Closer | Arm, Body, Forearm | FBX gains a LOD1 (Arm and Forearm keep-all, 1,112 / 940; Body 3,236 → LOD1 1,067); LOD0 unchanged | same | same |
| Closer | Shoe | 570 → 922 tris (48-segment ear, 32-sided screw dome; L1), + LOD1 | same | same |
| Door frames | `Kit_DoorFrame_Steel`, `_Steel_Alu` | same tris (3,866 / 1,430). The saddle and its screws move to the frame's slot (M1) | slot list loses its duplicate `Prop_Aluminium`. Steel: the saddle is now `Prop_SteelBrown` (dark bronze). `_Alu`: aluminium as before | same |
| Door frames | `Kit_DoorFrame_Wood` | FBX: the oak saddle's UVs turn to U-grain (H3); tris same (3,152 / 1,260); JSON unchanged | same | same |
| Leaves | `Kit_DoorLeaf_Veneer_Oak`, `Kit_DoorLeaf_Ward` | UVs turn 90° so the grain runs up the leaf (H3); tris same | same | same; new sidecar field `grainAxis` |
| Plates, sign | `Kit_DoorNumberPlate` ×3, `Kit_DoorSign` | same LOD0 (480 / 664), + keep-all LOD1 so they cull at 15 / 25 m (H4) | same | same |
| Exit device | `Kit_ExitDevice_Crossbar` | 3,948 → 3,588 tris. A larger latch case (0.1025 × 0.180) with a rim-latch head (L4); bounds grow ≤ 31.6 mm; + keep-all LOD1, cull 25 m | same | same; new field `swingClearWorstZ` 0.97664 |
| Key hosts | `Kit_KeyCabinet` | **main's faulty build replaced** (codex audit F5: inside-out corner faces). 6,804 tris, + keep-all LOD1, cull 40 m. GUID `13c85fe1e557741c982832fc03970bc8` kept | same | same; `hungPose` v2 (ring yaw −60 → 120°, tag twist −30 → 150°, tag on the wall side) |
| Key hosts | `Kit_KeyHookBoard` | UVs to U-grain (H3); 4,392 tris; + keep-all LOD1 | same | same; `hungPose` v2 |
| Key hosts | `Kit_KeyHook` | 530 → 922 tris (48-segment flange, 16-sided wire; L2); + LOD1 | same | same; `hungPose` v2; budget 600 → 850 |
| Keys | `Kit_Key_Zone`, `_Nickel` | 2,184 → 2,188 tris: the tip bevel starts at Z 0.0234 (was 0.0215), so cut 6 shows (L6); + LOD1 | same | same |
| Key parts | `Kit_KeyRing`, 9 × `Kit_KeyTag_*` | same LOD0, + keep-all LOD1 (cull 12 / 15 m) | same | same |
| Lock | `Kit_Lock_Escutcheon`, `_Brass` | 3,176 → 3,248 tris (1.5 mm top round in 4 segments; L7); + LOD1 | same | same |
| Lock | `Kit_Lock_Knob`, `_Brass` | 4,608 → 4,992 tris, crowned face (sphere R 0.070, still 0.065 proud; L3); LOD1 1,996 | same | same |
| Lock | 13 other lock parts (cylinder shells, deadbolt, latchbolts, lever, plug, roses, strikes) | same LOD0, + LOD1 and cull at the sidecar distance (H4) | same | same |
| Windows | `Kit_WindowFrame_Steel`, `_Steel_Enamel` | rebuilt from main's 10-04 modules (H5): 3,862 / 1,438 → 4,054 / 1,458 tris | same | same; `wall_decor` tag; cull `null` → `0.0` (both read as never) |
| Windows | `Kit_WindowFrame_Alu` | rebuilt from main's module: 1,088 / 1,064 → 2,190 / 1,050 tris | same | same; as above |
| Windows | `Kit_WindowFrame_Wood`, `Kit_MiniBlind_Raised`, `_Lowered` | FBX kept (main's; same content) | same | JSON only: the `wall_decor` tag (and cull `null` → `0.0` on the wood frame). No code reads `wall_decor` (grep of `Assets/Scripts` and `Assets/Editor`) |

- **Totals for the 57 kits:** LOD0 133,860 → 136,650 tris (+2.1 %).
- **LODGroups:** every kit now has a LODGroup whose switch and cull match its sidecar. Before this merge, 42 of the 57 had none, so they never culled.
- **Anchors:** none added, removed or moved in any kit (diffed per anchor, tolerance 0.1 mm).

## 3. Package (`W/apply/`, convention v2)

| File | What |
|---|---|
| `interactables_kit.sh` | The apply script (`# apply-convention: v2`; honours `APPLY_REAL`, `APPLY_DIR`, `APPLY_BACKUP`). It writes the 52 sidecars first, then the 50 FBX. The order matters because `FrontRoomsKitImporter.OnPreprocessModel`/`OnPostprocessModel` read the sidecar's `slots` and `lodDistances` while Unity imports the FBX, and nothing re-imports an FBX when only its sidecar changes. It backs up main's copies to `W/backup/interactables_kit_<stamp>/`. |
| `interactables_kit_bases.txt` | 102 base sha1, recorded 2026-10-08 19:35:46 on main `bc0aa79` (worktree; Props/Models clean). The 2 door-frame sidecars were re-based at 19:45 on the exit-sign versions (`8fa919c…`, `827c663…`) |
| `interactables_kit_expected.txt` | 102 payload sha1, in write order |
| `interactables_kit_keep.txt` | Main's 102 `.meta` sha1 (a change before the apply refuses it), plus notes on `FrontRoomsKitImporter.cs` `08daf35f`, `FrontRoomsKitLibrary.cs` `a2c7ebc8` (after the exit-sign merge; was `4eb2e47d`) and `FrontRoomsInteractableKit.Window.cs` `07dc067e` (a move is a NOTE) |
| `interactables_kit_guids.txt` | The 102 kept GUIDs. After the apply, the manifest checks each `.meta` by GUID, not by sha1 (see below) |
| `interactables_kit_payload/`, `interactables_kit_base/`, `interactables_kit.diff` | Payload, exact base copies (written by `rebase_apply.sh interactables_kit --init`: INIT-OK), and the diff: 52 sidecar hunks plus 50 "Binary files differ" lines |

**Meta re-serialisation (expected).** When Unity imports an FBX that now has `_LOD0`/`_LOD1`, it rewrites one field in that FBX's `.meta`: `lODScreenPercentages: []` becomes `[0.25, 0.01]`.
- In the merge clone this happened to exactly the 42 small-part FBX that gain a LOD1. That field was the only change in every one of them, and every GUID line was unchanged.
- Red's Unity will do the same on import, and his auto-commit will then pick up those 42 `.meta` files.
- The script never writes a `.meta`.

**Tests of the script:**
- **Dry run against Red's project:** `DRY RUN OK` at 19:36 and again after the rebase at 19:5x; 102/102 bases match, 102 kept `.meta` unchanged, 3 notes as verified.
- **`--apply` on a scratch copy** (`W/int_work/merge2/scratch_target`, via `APPLY_REAL`/`APPLY_BACKUP`): `MANIFEST OK`, 102 backups. The test manifest was moved out of `W/apply` to `W/int_work/merge2/logs/scratch_applied_manifest.txt`, so it cannot pass for a real apply.
- **After a Unity-style meta re-serialisation:** `ALREADY APPLIED` (GUID kept).
- **With a changed GUID:** `REFUSED`.

## 4. Verification over today's main

Two clones were made with `W/tools/mkclone.sh` at 19:29 from main `bc0aa79` plus its working tree:
- **before:** `W/proj_merge_interactables_kit_base`, main only;
- **after:** `W/proj_merge_interactables_kit_2`, main plus the 102 payload files, with main's metas.

`apply_local_patches.py` reported "main already compiles here" (nothing to patch). Both clones got the FMOD banks, plus the clone-only window harness (`merge/FrontRoomsWindowTF.merge.cs.txt`) and kit probe (`merge/MikKitProbe.cs.txt`). Each check ran in both clones with the same arguments, one Unity per clone, at load 90–280.

| Check | Before (main) | After (main + kit) | Verdict |
|---|---|---|---|
| Batch compile, OSXUniversal (`Library/ScriptAssemblies` cleared first) | 0 `error CS` | 0 `error CS`. The import logs differ only in clone paths and in the 50 extra kit FBX imports. No new warning line | **PASS** |
| Kit probe (`MikKitProbe.Run`, 57 kits as Unity imports them: LOD switch and cull at lodBias 1 and FOV 76 vs the sidecar, tris per LOD vs the sidecar, colliders, lights, stray renderers, materials) | FAIL: 42 kits have sidecar distances but no LODGroup (the pre-fix H4 state) | **57/57 PASS**: every d01 and cull equals its sidecar (windows and door frames/leaves: 4 m / never; blinds 3 / 30; the rest as `30_final.md` §4); tris equal the sidecar; 0 colliders, 0 lights, 0 stray renderers | **PASS** |
| Materials as imported | — | Every slot maps to its `Resources/Surfaces` material, except the known missing slots (codex audit F4): `Door_Enamel` (steel leaves, `_Steel_Enamel` frame), `Prop_KeyTagNo` (tags, plates, board, cabinet), `Prop_SignEngraved` (`Kit_DoorSign`). They fall back to the FBX's plain material, as before the merge | FINDING (unchanged) |
| Window gate (`FrontRoomsWindowTF.RunAll`, the window-landing harness on main's own call site; seeds 20261001, 516574485, 4242, 1990, 7, 1955, 1993, 8086, 6502, 68000) | kitoff 8,847/0, kitthrow 307/0, kit 20,391/0; W-OF 3,862 / 1,438 tris | kitoff 8,847/0, kitthrow 307/0, **kit 20,391/0**; W-OF 4,054 / 1,458 tris (budget 3,600 / 1,300 ±15 % = 4,140 / 1,495); no collider in the opening; envelopes held; rooms under budget (worst Office 85,556 / 120,000) | **PASS** |
| Project suites + 20 map seeds (`FrontRoomsCxSuites.RunBatch`) | 10 suites without a harness exception. MapInteractionTests logs main's known 2 errors / 4 exceptions. 20 seeds: 0 + 0 determinism failures | Identical suite lines and console counts. The 10 suite reports are byte-identical apart from timestamps and one timing (`worstPlanMs`). 20 seeds: 0 + 0 determinism failures | **PASS** |
| Map signatures (renderers, lights, colliders, per chunk, 20 seeds) | — | 44 of 1,500 rows differ. All are the `renderers` hash of chunks with an Office window. The only changed detail lines are `Kit_WindowFrame_Steel_LOD0/LOD1` vertex counts (4,304 → 4,414 and 1,638 → 1,639); materials, bounds and position are the same. Colliders and lights: identical | **PASS** (expected change only) |
| 120 s Play Mode (`FrontRoomsCxPlayRun`, seed 516574485, autopilot) | 21 warnings, 0 errors, 1 exception | 31 warnings, 0 errors, **1 exception**. The same exception in both runs: Unity's own `SearchDatabase.EnumerateAll` at editor start-up. No new exception type, 0 pink renderers, G14 on. The extra warnings are 9 more "FMOD port 9264 in use" lines (both clones ran at once) and 2 "Module prop … would block an opening" lines from a different autopilot route (the Relay caught the before run once) | **PASS** |
| What Red sees in Play Mode (`FrontRoomsLandCheckCapture`, 37 frames, same seed) | — | 30 of 37 frames are pixel-identical. The 7 that change all show the live Office window (`Kit_WindowFrame_Steel`): 20, 21, 22, 32–35, with 152–1,412 px over 8/255 (≤ 0.07 % of a frame), all on the frame | **PASS** (`images/mik_window_before_after.jpg`) |
| Kit close-ups (`FrontRoomsKitLookdev`, game post) | main's FBX | merged FBX | see §5 |

## 5. Merge frames (verification log)

- `images/mik_window_before_after.jpg`: Play-mode frames of the live Office window, before | after | diff ×8, at 2.0 m, 1.2 m and at the pane (0.95 m). The rebuilt steel frame (main's 10-04 module: screw slots, the pass-4 LOD1) is the only change Red will see in play today.
- `images/mik_kits_before_after.jpg`: look-dev close-ups of changed kits, before | after.
- `merge/mik_landcheck_diff.txt`: changed pixels per LandCheck frame.

Figma: **VL219**, frame `2956:6863`, in FRONTROOMS · VISUAL VERIFICATION LOG (`2595:6093`, row 54, no growth). It shows `mik_window_before_after.jpg` (large) and `mik_kits_before_after.jpg`. Verdict PASS. The cover VL000 was recounted from the canvas (218 placed checks, 636 images, 26 tasks), and the D1 legend now reads `VL009–219 · 29 checks`. The index row is in `Documentation/VERIFICATION_LOG.md` §3.

The rebase after the exit-sign merge produced no new image (the sidecar change is one anchor that no frame shows), so it has no slide.

## 6. Codex audit findings for this workflow

`codex_audit/20_findings.md` still does not exist (checked 19:2x). From `10_review_kits.md`:

| # | Finding | State after this merge |
|---|---|---|
| F1 | Importer culled frames at 12 m | Fixed in main by `663e858`. Re-checked here on all 57 kits: LOD and cull equal the sidecars (§4) |
| F3 | Windows live before the doors | Red's call, unchanged. This merge changes no door in the game |
| F4 | 4 slots without a material (`Door_Enamel`, `Prop_KeyTagNo`, `Prop_SignEngraved`; `Run_ExitSign_Dead` is the exit-sign track's) | **Open.** It needs P-4 approval, and the audit says not to add placeholder materials to main. Until then, no game code may spawn `Kit_DoorSign` or a tag |
| F5 | Faulty `Kit_KeyCabinet` | **Resolved by this package.** The cabinet is replaced and GUID `13c85fe1…` kept |
| F6 | `Kit_KeyHookBoard` vs `Kit_KeyRack` | Red's pick, open. Nothing renamed |
| F7 | `VISUAL_CHAT_TASKS.md` misstates the kit state | Docs, the visual chat's. After the apply, D1.3/D1.4 should say "59 kits merged inert, fix-pass finals" |

No change outside this workflow's area was made or reverted.

## 7. Contracts the visual chat must send (map chat)

This merge needs **no** map change: the doors, locks and keys stay inert, and the window facade is unchanged. The open map items stand as in `30_final.md` §5, updated against main's working tree today:

1. **Hinge pivot (A2, `30_final.md` §5.1, critic M7). Open.** `Door hinge` is still placed at `start + along * (c - width * .5f)` (`FrontRoomsMapWorld.cs:1441-1443`, working tree). Without A2 or a facade-side pivot, the kit leaf digs into the lining from about 7.5°.
2. **Lock point → keyhole (§5.2, M8). Done by the map chat in the working tree.** `LockPose` uses `KeyholeProud` 0.0065 (±0.0315) and the rig's `Keyhole` when there is one (`FrontRoomsMapWorld.cs:2614-2634`). `LockPoint` keeps the handle point for `DoorLocked`/`DoorHandleTurned`. That is fine, because the unlock now aims at `LockPose`.
3. **Head dip (§5.3, H7). Differs from the verified pose.** `FrontRoomsShotTimings.Unlock.PoseYawTowardHingeDeg = 12f` (`FrontRoomsShotTimings.cs:67`) and `DipDistanceFromLock .45` (`:46`). VL199 verified 25° toward the door centre, ending at 0.20 m and FOV 50. At 12° the key is less edge-on than at 0°, but this was not rendered. The visual chat should send the 25° / 0.20 m / FOV 50 numbers or render 12°.
4. **Door and key rigs.** The map defines `IFrontRoomsDoorRig` / `IFrontRoomsKeyRig` (`FrontRoomsKitSeam.cs`, untracked). The visual chat's `FrontRoomsInteractableKit.Door.cs` / `.Key.cs` implement them; not written yet (`30_final.md` §6 item 1).
5. **Key hosts (§5.4, C2):**
   - Level 0 procedural rooms: `Kit_KeyHookBoard`.
   - Office: `Kit_KeyCabinet`.
   - The single hook only where the sight line is ≤ 3 m.
   - Hosted keys do not spin.
   - Hung pose v2 is now in the hosts' sidecars (this merge).
6. **Window trims "T"** (`window_landing/FrontRoomsMapWorld.window-kit.r6-trims.diff`). Optional and unchanged.

## 8. Files

- Package: `W/apply/interactables_kit.sh`, `_bases.txt`, `_expected.txt`, `_keep.txt`, `_guids.txt`, `_payload/`, `_base/`, `.diff`.
- Evidence here: `merge/` (change table, kit probe before/after, window-gate, suites and play summaries, the clone-only probe/harness/chain scripts as `.txt`); `images/mik_*.jpg`.
- Work dir: `W/int_work/merge2/` (logs `logs/*.log`, `runs_load.txt`; scratch apply tests `scratch_target*/`). Clones: `W/proj_merge_interactables_kit_2` (after), `W/proj_merge_interactables_kit_base` (before), `W/proj_merge_interactables_kit_3` (after, re-synced past the exit-sign merge, rebased payload). Rebase work dir: `W/tools/_rebase/interactables_kit/20261008-194557/` (log copied to `merge/rebase_20261008-194557.log`). The 01:52 attempt's clone `W/proj_merge_interactables_kit` was synced but never run (its `Logs/` is empty); it was left as it is.
