# Office dress hitch: merge into main (stage 40)

Workflow office-dress-hitch, merge stage, attempt 3 (2026-10-07 23:24 to 2026-10-08 00:23). Task Q13 in `Documentation/VISUAL_CHAT_TASKS.md`.

## 0. Verdict

**READY, NOT APPLIED by this stage.** Every check passes on today's main. The apply package is complete and its dry run against Red's project is OK. Red's merge rule (d) says subagents do not write into Red's project, so this stage stops at the dry run. The visual chat applies it with one command:

```
bash /Users/redwang/FrontRoomsVisualWork/apply/office_dress_hitch.sh --apply
```

After that, the map chat applies its contract (§5.1). Until then the game builds exactly what it builds today, with dressers about 3x faster. The hitch only goes away once the map steps the jobs.

Why it took three attempts: attempt 1 (16:5x) verified, but its package was never written. Attempt 2 (18:1x to 20:06) re-verified on a newer main, and then hit the session limit before it wrote the package. Attempt 3 (this one) re-verified on main ee5c9bb, wrote the v2 package, and dry-ran it.

## 1. What merges

These five paths are visual-owned (`Assets/Scripts/Office/`). They are listed in write order, as in `W/apply/office_dress_hitch_expected.txt`.

| Path | Action | Base sha1 (main) | After sha1 |
|---|---|---|---|
| `FrontRoomsDressJob.cs.meta` | add (new GUID `a58cc79c163934fbebbd902e9a67eeba`, unused in main; written before its `.cs`) | ABSENT | `12c27dc9` |
| `FrontRoomsDressJob.cs` | add: the Step API | ABSENT | `7f4ea7a7` |
| `FrontRoomsKitLibrary.cs` | replace (+132 / −32 lines) | `b55b9f57` | `4eb2e47d` |
| `FrontRoomsFurniturePile.cs` | replace (+418 / −154) | `029c766b` | `767451c6` |
| `FrontRoomsOfficeKit.cs` | replace (+326 / −142) | `7c63d918` | `b4c66afb` |

- **Kept, never written.** The three `.meta` files keep main's GUIDs:
  - `FrontRoomsKitLibrary.cs.meta` `a1464473`;
  - `FrontRoomsFurniturePile.cs.meta` `48ab681e`;
  - `FrontRoomsOfficeKit.cs.meta` `ae758f35`.
  The map contract's base, `FrontRoomsMapWorld.cs` `9abacc6f`, is a note only.
- **Bases.** They were first recorded 2026-10-07 16:52 (main 16f520e). They were the same at 18:19 (a5262fb) and at 23:30 (ee5c9bb), when they were recorded again, and the dry run at 00:23 on main 22bb75f matched them. Codex's 19:10–19:41 work and every commit since 16f520e left these five paths alone.
- **Package (apply convention v2), in `W/apply/`:**
  - `office_dress_hitch.sh`;
  - `_bases.txt`, `_expected.txt`, `_keep.txt`;
  - `_payload/` (the 5 files) and `_base/` (exact copies of main's 3 replaced files);
  - `office_dress_hitch.diff` (the unified diff, 2,264 lines).
- **How the script behaves:**
  - It refuses (exit 1, writes nothing) on any base, kept `.meta` or payload drift, if a new path is taken, or if the new GUID is already in use.
  - When every path already holds the payload, it prints ALREADY APPLIED and exits 0.
  - `--apply` first backs main's 3 files up to `W/backup/office_dress_hitch_<stamp>/`, then writes the files in place (no temporary file under `Assets/`), then checks the sha1 manifest.
- **Tested on scratch copies** under `W/dress_hitch/merge3/apply_test/`:
  - `--apply` → MANIFEST OK;
  - a second dry run → ALREADY APPLIED;
  - a drifted base → REFUSED, with nothing written and no backup made.
  - `W/tools/rebase_apply.sh office_dress_hitch --no-install` → `PASS … nothing to rebase (every touched base matches); dry run OK`.

**What the code does.**
- `FrontRoomsOfficeKit.BeginDress(Transform, Rect, float, int, Rect[] keepClear, Rect[] obstacles)` at `FrontRoomsOfficeKit.cs:450` (5-argument overload at `:456`) returns a `FrontRoomsDressJob`.
- `FrontRoomsFurniturePile.BeginBuild(…)` at `FrontRoomsFurniturePile.cs:211`, and `BeginBuildPile` at `:217`.
- `FrontRoomsDressJob.Step(float budgetMs)` at `FrontRoomsDressJob.cs:89`, `Complete()` at `:121`, `Cancel()` at `:130`, and `static WarmUp()` at `:180`.
- `Dress`, `Build` and `BuildPile` keep their signatures (`FrontRoomsOfficeKit.cs:433/439`, `FrontRoomsFurniturePile.cs:185/191`). They run the same job to completion, so every caller is unchanged:
  - RoomStream `FrontRoomsRoomStream.cs:1440`;
  - the map's reflection binding `FrontRoomsMapWorld.cs:1824/1835`;
  - the Level Designer, the tests and the look-dev tools.
- `FrontRoomsKitLibrary.Spawn` keeps both signatures (`FrontRoomsKitLibrary.cs:209/214`). `Prewarm()` is new, at `:140`.

## 2. Provenance (recovered code, merged over main)

- **The fix-stage final.**
  - Where it came from: workflow wf_11cd7561-ccc, verify PASS 2026-10-03, fix done 2026-10-04 13:19 UTC. The 2026-10-05 reboot wiped it. It was rebuilt from the stage transcripts by replaying every Write/Edit, starting from `git archive 7320ed1` (`W/dress_hitch/recovered/`, SHA1SUMS there).
  - How it was checked: the recovered files reproduce the fix stage's own results. The 1,111-case identity run (§3) is that stage's test, re-run on the recovered code.
- **Merged over main as a diff** (`W/dress_hitch/merged/`, `git merge-file` 3-way). Two changes in main sit inside our files, and both are kept:
  - Codex's `float[] lodDistances` (`FrontRoomsKitLibrary.cs:59`). It is unchanged.
  - `04c3a6a` (2026-10-04) puts `FrontRoomsScreenVideo.Attach(monitorObject)` on every monitor (main `FrontRoomsOfficeKit.cs:675`). The job records objects first and creates them later, so the call moved:
    - it is now the `AttachScreen` callback (`FrontRoomsOfficeKit.cs:851`), passed to the monitor's kit node (`:875`);
    - `FrontRoomsDressJob` runs it on the spawned instance in the same step, right after `Spawn` (`FrontRoomsDressJob.cs:231, 276–284, 307`).
    - Monitors therefore get their `FrontRoomsScreenVideo` component in the same order as before. The identity dump lists every component type, so a missing or misplaced attach would count as a mismatch.

## 3. Verification

Three runs. Each was a fresh clone of the main of the moment (`cp -Rc W/proj_audit` + rsync, or `W/tools/mkclone.sh`). B = main's dressers, A = the 5 payload files. All were batch runs with `-buildTarget OSXUniversal`, and nothing ran in Red's project.

| Check | Attempt 1, main 16f520e (16:5x) | Attempt 2, main a5262fb (18:1x–20:06) | Attempt 3, main ee5c9bb (23:28–00:18) |
|---|---|---|---|
| Batch compile, B and A | 0 errors, 0 warnings | 0 errors, 0 warnings (A 1,125 s at load ~800) | 0 errors, 0 warnings (A 262 s; final 125 s) |
| Roslyn compile, newest main 22bb75f (00:2x) + payload | — | — | Assembly-CSharp 0 errors 0 warnings; -Editor 0 / 0 |
| Suites (EditMode: MapInteraction 143/143, LevelDesigner 138/138, FixtureTick 28/28, CameraRig 25/25, Captions 9/9, GlassShot 20/20, RelayNav 60/60, Stream 5/5) + map validator 100/100 seeds | PASS | PASS, B = A | PASS, B = A; suite console message sets identical |
| 500-map signatures (renderers, lights, colliders) B vs A | — | byte-identical | byte-identical (1,501 rows) |
| Identity, `FrontRoomsDressVerify` vs renamed copies of main's dressers | warm-up run PASS | 1,111 cases, 0 mismatches | **1,111 cases, 0 synchronous and 0 stepped mismatches at 0 / 0.5 / 3 / 100 ms**; 452/452 kit spawns identical over 113 kits; every safety case as designed (below) |
| Map contract, `FrontRoomsDressMapStepTest` (MapWorld patched in the clone only, then restored) | PASS | PASS | **PASS, 43/43** against a fresh baseline of today's map (B_mapbase, 7 maps) |
| 120 s Play Mode, autopilot seed 516574485 | A: 120 s map, 7,307 frames, same messages as B | B only (load ~800: 109 frames, unusable) | **A: 120 s map, 7,307 frames, 0 restarts**; no new exception (below) |
| Look-dev frames, kit seed 1, 7 views | — | 6 identical; pile_close 21 px (noise, below) | not re-shot (rule (c)) |

**Safety cases.** Each one was run in the attempt-3 identity check:
- A parent destroyed mid-job, while solving or while spawning, cancels the job, and nothing more is created.
- `Cancel()` stops the job.
- A null parent returns quietly.
- Three jobs under separate parents, stepped round-robin, match the originals.
- `keepClear` edited after `BeginDress` does not change the result.
- One designed limit, `false` on purpose: two jobs interleaved under the **same** parent change the child order (pile root before the office root). This is the documented rule at `FrontRoomsDressJob.cs:23–28` and `FrontRoomsFurniturePile.cs:198–210`. The map contract enforces it: one open job at a time, and the next room's PlaceProps waits.

**Play Mode console, attempt 3, A** (`W/dress_hitch/merge3/runs/A_play120/summary.txt`). Everything in it is present in attempt 1's B run of main too:
- FMOD `Net_Listen` port in use (98);
- the FMOD Master.strings bank missing in the clone (1 error + 1 BankLoadException);
- the editor's `UnityEditor.Search.SearchDatabase` ArgumentOutOfRangeException at startup;
- "No Theme Style Sheet";
- the map's module-prop "would block an opening" warnings.

0 console lines mention `FrontRoomsDressJob`, `FrontRoomsOfficeKit`, `FrontRoomsFurniturePile` or `FrontRoomsKitLibrary`. Attempt 3's own B run was stopped at 7 min with the map stalled at frame 1 under load ~300. The baseline is therefore attempt 1's B (main 16f520e, same seed, 120 s, 7,307 frames).

**The 21 px in pile_close** (VL179, second image):
- 21 scattered pixels inside x 936–1035, y 580–602, max 7/255.
- In attempt 1 the merged dressers drew that view **byte-identical** to main's frame.
- In attempt 2 the same files differed by these 21 px.
- Same code, two outcomes: this is run-to-run render noise, not geometry. The object-level identity (1,111 cases, every component type listed) rules out any geometry difference.

**Timing** was not re-measured as a gate here: load was 300–490 during attempt 3, and its per-frame times swing by 50–150 ms either way. The measured result stands from the fix stage (`30_fix_final.md`, quiet machine):
- the hitch room (seed 20388, chunk (16,10), room 1) goes from 43.3 ms to 8.6 ms synchronous;
- with the map contract it takes 2 frames at 3 ms each (worst step 3.0–3.3 ms);
- 0 of 4,037 steps took over 3.5 ms at Step(3).

## 4. Codex audit findings for this track

- `Documentation/research/codex_audit/20_findings.md` does not exist. I checked at 23:2x and again at 00:2x, right before the last step. Following RECOVERY note 9, I read `00_main_state.md` and every `10_review_*.md`. `00_main_state.md:262` lists "D1 dress hitch … not touched". No review lists a finding against these dressers, so there is nothing to resolve.
- **Related, owned elsewhere, not changed here.** The CRT ad on every Office monitor has open findings: codex-audit glass-look F6 (player builds cannot create the material) and F7 (the quad floats on after its monitor culls), both in `Assets/Scripts/Media/FrontRoomsScreenVideo.cs`, plus 10_review_visual V3 (content; Red picks (a) or (b)).
  - This merge keeps the Attach behaviour exactly as it is.
  - Their line references to `FrontRoomsOfficeKit.cs:674–675` move once this merge lands. The attach is then `AttachScreen` (`FrontRoomsOfficeKit.cs:851`) at the monitor node (`:875`).
  - V3 option (a), "stop attaching the ad", then becomes passing `null` instead of `AttachScreen` at `:875`.

## 5. Contracts the visual chat must send

### 5.1 To the map chat: step the jobs (the change that removes the hitch in game)

Files in `Documentation/research/dress_hitch/map_contract/`:
- `FrontRoomsMapWorld.dress_step.patch`. Base: main's `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs` sha1 `9abacc6f11fcc4bf750664ca10a9c173165634b4` (unchanged from 16f520e through 22bb75f). Result: `6b76a00c9228d7682efb7e3272cfa59dad5ad373`. It applies cleanly with `patch -p1` and with `git apply` (checked 00:0x), and gives exactly that result.
- `FrontRoomsDressMapStepTest.cs` + `.meta` (GUID `a9b77e1a91c474764b97b11d1d90c64d`, unused in main) → suggested target `Assets/Editor/FrontRoomsMap/`.
- **Precondition:** `office_dress_hitch.sh --apply` has run. The patch casts to `FrontRoomsDressJob`, so it does not compile without it.
- **What it does:**
  - `ResolveDressers` binds `BeginDress` (6 arguments) and `BeginBuild` (5 arguments).
  - One open job is held in `currentJob`. `DressNext` steps it by `dressBudgetMs` before it dequeues the next room.
  - `StepRoom` cancels the job when its chunk is dropped or rebuilt, completes it when `all` is set, and cancels it with a warning on an exception.
  - `RebuildChunk` and `Begin(buildAllNow)` finish at once.
  - `ReadyAround` and `Settled` wait for the open job.
  - `Prewarm` calls `FrontRoomsDressJob.WarmUp()`.
  - The budget is 3 ms on desktop and in the Editor. 2 ms applies only under `#if UNITY_WEBGL && !UNITY_EDITOR`, Red's WebGL-only rule.
- **Proof for the map chat to re-run:**
  - before the patch: `-executeMethod FrontRoomsDressMapStepTest.RunBaseline -mapStepOut base.txt -mapStepBaseline <dir>`;
  - after it: `-executeMethod FrontRoomsDressMapStepTest.RunBatch -mapStepOut step.txt -mapStepBaseline <dir>`.
  - Required result: PASS, 43/43. That is what attempt 3 got on today's map.

### 5.2 Notice (no file): CRT-ad owners

Whoever does glass-look F6/F7 or picks V3 should use the new line references in §4. Nothing else changes for them.

### 5.3 Optional, editor only, visual-owned

- `harness/FrontRoomsDressPerfTest.cs.txt`: the timing and corpus harness. It can be promoted to `Assets/Editor/Tests/` with its own `.meta`, GUID `1723927f2e84a4a43b84b4bdcd135eaf`.
- Never promote `FrontRoomsDressVerify.cs.txt`. It needs the renamed `*Orig` copies that `harness/make_orig_copies.sh` writes into a clone.

## 6. Verification log

- **VL179** · `2843:6157` · Q13 · "Dress merge keeps the look" · PASS. Placed 2026-10-08 in row 44 of FRONTROOMS · VISUAL VERIFICATION LOG (2595:6093); the section did not grow.
  - `images/dh_merge_lookdev_before_after.jpg` (1920 × 1282, large slot `2843:6165`): the 7 look-dev views, main's dressers vs the merged dressers.
  - `images/dh_merge_pile_close_noise.png` (1888 × 942, slot `2843:6166`): the 21 px crop ×4 and the ×36 difference.
- The cover VL000 now has the legend entry `Q13 · VL179 · 1 check` at (972, 805). Its header was recounted from the canvas: 23 tasks · 184 checks · 557 images.

## 7. What Red will see

- **After `--apply`:** nothing visible changes. Same rooms, same objects, same frames. The open editor recompiles the scripts once.
- **After the map chat's patch:** the hitch room is furnished over 2 frames at about 3 ms each, instead of a single 35–43 ms frame. The other Office and pile rooms take 1–4 frames.

## 8. Where things are

- **Package:** `W/apply/office_dress_hitch.sh` (+ `_bases.txt`, `_expected.txt`, `_keep.txt`, `_payload/`, `_base/`, `.diff`). The superseded attempt-2 script is kept at `W/dress_hitch/merge3/office_dress_hitch.attempt2.sh`.
- **Runs:**
  - attempt 3: `W/dress_hitch/merge3/runs/` (B_/A_ compile logs, B_/A_cx_audit, B_mapbase, A_verify.json, C_mapstep.txt, A_play120);
  - attempt 2: `W/dress_hitch/merge2/runs/`;
  - attempt 1: `W/dress_hitch/merge/runs/`.
- **Clone:** `W/proj_merge_office_dress_hitch_2` (main ee5c9bb + the 5 files; no harness left in it).
- **This folder:** `30_fix_final.md` (the fix stage's report), `map_contract/`, `harness/`, `images/`.
