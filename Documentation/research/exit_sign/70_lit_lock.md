# EXIT sign: the LIT lock (C23 step 1)

Stage `exit-sign-lit-lock`, 2026-10-22 07:24 to 07:4x. Red decided C23 on 2026-10-22 in the 系统设计 chat (verified): "C23 按游戏视觉的两步方案：现在门上常亮一块，楼梯间做完后在出口区域里挂箭头吊牌" (`Documentation/system_design/systems.json` 2026-10-22c, decision C23, system N2). Step 1, now: each floor's EXIT door gets a `Kit_ExitSign` on its header that is **locked LIT** (never HALF, LOOSE or Dead), plus `Kit_ExitDevice_Crossbar` on the leaf. 关卡设计 mounts both with the stairwell minimum. This stage is 游戏视觉's part: the lock flag on the sign driver and a test for it.

Nothing was written into Red's project except this file. Main's two `.cs` files are unchanged (sha1 checked after the run).

## Status: built and verified in a clone, not applied

- **Payload:** `W/es_litlock/payload/Assets/Scripts/Rendering/FrontRoomsExitSign.cs` and `W/es_litlock/payload/Assets/Editor/Rendering/FrontRoomsExitSignTests.cs`. Both files are visual-owned. No other file changes, and no `.meta` changes.
- **Diff:** `W/es_litlock/lit_lock.diff` (2 files, +189 / −7). Base copies of main's files are in `W/es_litlock/base/` (sha1 `e2811567…` and `1eebed73…`, main `0ea38bb` + working tree at 07:25).
- **Clone:** `W/proj_es_final`, re-synced from main at 07:25 (the five folders with the iCloud excludes, then `apply_local_patches.py`: "nothing to do"). Its old `Library/ScriptAssemblies` was moved to `W/_partial/proj_es_final_ScriptAssemblies_1022`, so every script was recompiled.
- **Results:** Unity compile 0 `error CS`. `FrontRoomsExitSignTests` **13 / 13 PASS**, including the new G3. Roslyn over main's tree plus the two files: `COMPILE OK`, 0 errors, with both the macOS and the iOS defines.
- **Applying it:** `W/apply/exit_sign_litlock.sh` (apply convention v2, §7). It replaces the two files and keeps both `.meta`. Nobody else calls `LockLit` yet, so the game behaves the same until 关卡设计 sets it.
- **Independent verification (07:39–07:42): READY**, no blocking defects. Tests re-run 13 / 13 (byte-identical `tests.json` and `level_trace.csv`), Roslyn re-run against main as of 07:34: COMPILE OK on macOS and iOS.

## 1. API

```csharp
// FrontRoomsExitSign
public bool LockLit { get; set; }   // default false
```

| Call | With `LockLit` true |
|---|---|
| `Configure(seed, t, light, base)` | Deals **Lit**, whatever `t` is. Seed, light and light base are taken as usual. A `_Dead` model is still Dead |
| `SetMains(level)` | **Ignored.** No brownout, no transfer to the DC lamps, no black gap |
| `LockLit = true`, at any time (after `Configure` too) | Forces **Lit with settled mains at once** (AC lamps 1, DC lamps 0, no gap pending), then re-applies the lamp field and the light in the same call. The self-tick `Update` is switched off |
| `Tick(dt)` | Returns at once: `NeedsTick` is false (`!lockLit` is part of it) |
| Property block | None at `EmissionGain` 1: the field is plain LIT, so the renderers stay SRP-batched. Any other gain keeps a block, as on every sign (G1) |
| Red light | `intensity = base × 1`, enabled while base > 0 |
| `LockLit = false` | Clears the flag only. The sign stays LIT until the next `Configure` or `SetMains` |

**How 关卡设计 mounts it** (step 1, with the stairwell minimum):

```csharp
var sign = FrontRoomsKitLibrary.Spawn("Kit_ExitSign", header, position, yaw, null, false, label);
var driver = sign.GetComponent<FrontRoomsExitSign>();   // the kit library attaches it (tag exit_sign)
driver.SelfTick = false;                                // optional: the stream never ticks it anyway
driver.LockLit = true;                                  // before or after Configure: either order gives LIT
driver.Configure(0u, FrontRoomsExitSign.Temperament.Lit, null, 0f);   // the header sign has no light (spec §8.6)
```

- **Mount a live kit.** Use `Kit_ExitSign`, never `Kit_ExitSign_Dead`. A `_Dead` model has no live diffuser, so it stays Dead with the lock on (G3 documents this, and it is not an error).
- **Placement** is the existing door-header placement in `FrontRoomsRoomStream.BuildDoorExitSign`: back on the header's room face, bottom at 2.674 m and top at 2.886 m, centred on the 2.4 m opening, clear of the 2.62 m leaves. The crossbar goes on the leaf. Both are 关卡设计's, inside the map's own files.

## 2. Why each rule

- **Lit whatever it is dealt.** The floor exit's sign is the one sign the player must be able to trust. C23's rationale: the exit door's sign "is locked to LIT (never half-lit, loose or dead)". Today the stream deals LIT to door signs anyway. The lock makes that a guarantee instead of a property of the current deal, so a future hash, a random temperament or a copied call cannot dim it.
- **`SetMains` is ignored.** In the fiction the true exit's sign is on its own emergency circuit, so brownouts and power failures do not dim it. NFPA 101 asks for exit signs to stay lit on emergency power, and the game's EXIT is the one place where that rule should be visible. Every other sign keeps its mains behaviour (brownout `level^3.4`, the 0.2 s transfer gap, BATTERY at 0.166), so a power event still reads everywhere else, and the one sign that holds steady through it is the exit.
- **Setting it after `Configure` takes effect at once.** The map can mount the sign and lock it in either order. A sign caught mid-drop or inside a transfer gap does not ease back over several frames: it is LIT on the frame the flag is set. A locked sign needs no ease, because it never changes again.
- **`NeedsTick` stays false.** A locked sign does no per-frame work, keeps no property block at gain 1, and its renderers stay in the SRP batch. That costs nothing next to a plain LIT sign.
- **A `_Dead` model stays Dead.** The driver cannot light a model built with no live diffuser (the `_Dead` meshes carry `ExitSign_Diffuser_Dead`). Faking LIT there would leave Level and the light saying LIT over unlit letters. The rule is on the map's side: mount `Kit_ExitSign`.
- **Not serialized.** The flag is code-set: the map builds the floor exit at run time, and the kit library adds the driver at spawn. So a plain property is enough, and the component's serialized layout is unchanged.

## 3. G3: the test

`G3 LIT lock: the floor exit's sign stays LIT` is entry 13 of `FrontRoomsExitSignTests.RunBatch` (edit mode, `-quit`).

- **Sweep.** `Kit_ExitSign` (2 diffuser renderers, LOD0 and LOD1) with `LockLit`: every temperament (`Enum.GetValues`, 6), with REDUCE FLASHING off and on. Each one runs `Configure`, then `SetMains(0.8)` + 60 Ticks, then `SetMains(0.3)` + 120 Ticks, then `SetMains(1)` + 600 Ticks.
- **Every sign-frame must hold all of these:** State Lit; Level 1 (±1e-5); weights (1, 1, 0, 0); light enabled at `3.6 × Level` = 3.6; 0 property blocks on any renderer; `NeedsTick` false; the self-tick `Update` off.
- **Self-tick:** a self-ticking sign, locked, then given LOOSE and `SetMains(0.3)`, never turns its `Update` on.
- **Set after `Configure`:**
  - LOOSE, caught in a drop-out and then 0.1 s into a mains transfer (Level 0.042, `NeedsTick` true), is LIT on the same call;
  - HALF A (2 blocks) goes to LIT with 0 blocks;
  - BATTERY goes to LIT.
- **Controls without the lock:** the next `Configure` deals HalfB again, and `SetMains(0.8)` dims the sign to 0.468 (0.8^3.4). Unlocking leaves the sign LIT until then.
- **`Kit_ExitSign_Dead` with `LockLit`:** Dead, Level 0, light off, `NeedsTick` false. This is the documented behaviour.

## 4. Results

Load averages were 276–348 throughout, so every timing below is **UNDER LOAD**.

| Step | Result | Time |
|---|---|---|
| Clone re-sync (rsync × 5, local patches) | OK, main `0ea38bb` + working tree; local patch "nothing to do" | 2 s |
| Unity compile, `-batchmode -nographics -quit` | rc 0, **0 `error CS`**; 166 imports, scripts compile 5.9 s (`W/es_litlock/logs/c01_compile.log`) | 18 s, UNDER LOAD (load 276–316) |
| `FrontRoomsExitSignTests.RunBatch -quit -esTestOut …/tests_litlock_1022` | rc 0, **13 passed, 0 failed** (`W/es_litlock/logs/t01_tests.log`; `tests.json`, `log.txt`, `level_trace.csv`, `sign_names.txt` copied to `W/es_litlock/tests/`) | 15 s, UNDER LOAD (load 339–348) |
| Roslyn `--tree <main> --override` both files `--all` | **COMPILE OK**, 0 errors: Assembly-CSharp 101 files, Assembly-CSharp-Editor 62, FMOD ×5 | 20 s, UNDER LOAD |
| Same with `--ios` | **COMPILE OK (iOS defines)**, 0 errors | 8 s, UNDER LOAD |

**Test lines** (`[ExitSignTests]`, details shortened):

```
PASS T5 photosafety, LOOSE, 200 seeds x 120 s | max 1 flashes in any 1 s (bar <= 2); longest episode 2.65 s (bar <= 5); darkest Level 0.486; 200/200 seeds flickered
PASS T5 photosafety, REDUCE FLASHING | 50 seeds x 120 s: 0 transitions (bar 0); Level 0.783-0.817; largest frame step 0.007
PASS T5 mains transfer (gap, crossfade) | brownout 0.8 -> Level 0.468; black 0.217 s, BATTERY 0.166; mains back: no gap; REDUCE FLASHING: lowest 0.166, step 0.028
PASS D1 static signs, property blocks | Lit blocks 0; HalfA/HalfB/Battery/Dead blocks 2; Loose needsTick True; self-tick LIT False, LOOSE True
PASS D2 _Dead models are always Dead | 4 _Dead models: Dead, Level 0, light off
PASS D3 kit library attaches the driver to exit_sign kits only | 8 exit-sign kits get the driver; 133 other kits spawned: none has one
PASS D4 no allocation per Tick | 0 bytes over 36,000 LOOSE ticks
PASS R1 RoomStream preview: placement, light, names | Run 2 active (light y 1.96), Exit 1 active; 0 forbidden names
PASS R2 RoomStream walk: temperaments, light, cueing | room 86, 17 Run, 17 Exit; LIT 40, HALF 9, LOOSE 2; mismatches 0; loose flashes 6 (max 1 in 1 s)
PASS R2 RoomStream walk with REDUCE FLASHING | same deal; loose flashes 0
PASS G1 EmissionGain: emission only, Level and light un-gained | gain 3 fields x3, blocks 2; Run signs gain 1, Spot 162/140; LOOSE trace identical
PASS G2 loose gate: nothing rewrites a sign after the terminal door shuts | 353 moves before the shut; after it 0 rewrites, 0 moves
PASS G3 LIT lock: the floor exit's sign stays LIT | Kit_ExitSign (2 diffuser renderers) with LockLit: 6 temperaments x REDUCE FLASHING off/on, each Configure, SetMains 0.8 + 60 Ticks, 0.3 + 120, 1 + 600: 9408 sign-frames; self-tick Update enabled after Configure False, after SetMains 0.3 False; set after Configure: LOOSE in a drop-out (Level 0.825) then 0.1 s into a transfer (Level 0.042, needsTick True) -> LIT at once, HALF A (Level 0.541, 2 blocks) -> LIT with 0 blocks, BATTERY (Level 0.166) -> LIT; unlocked: stays LIT, the next Configure deals HalfB, SetMains 0.8 dims to 0.468; 9413 sign-frames checked, 0 off LIT (bar 0: State Lit, Level 1, weights (1, 1, 0), light = 3.6 x 1, 0 property blocks, NeedsTick false); Kit_ExitSign_Dead with LockLit: Dead, Level 0, light off, needsTick False (documented: a _Dead model stays Dead)
[ExitSignTests] 13 passed, 0 failed
```

The 12 earlier checks' detail strings are **byte-identical** to the last run before this change (`W/es_fix1/r1021b_tests/tests.json`, 2026-10-21 21:36), so the unlocked path is untouched.

## 5. Flags and hand-offs

- **关卡设计:** mount `Kit_ExitSign` + `LockLit = true` on each floor's EXIT header, and `Kit_ExitDevice_Crossbar` on the leaf, with the stairwell minimum. Both kits are already in `Assets/Resources/Props/Models`. The title run's Exit-room door sign (`BuildDoorExitSign`) is not locked by this stage. It is dealt LIT and has no light, so it already reads the same. If that room becomes a floor exit, the map can set the flag there too (that is a map file, so the change is theirs).
- **Gain:** a locked sign at `EmissionGain` other than 1 keeps a property block (Weights × gain), like any other sign. The door sign uses gain 1.
- **Step 2** (after the stairwell): chevron hanging signs inside the exit zone (`50_for_red.md`, C23 rationale). Not part of this stage.
- **Clone sync, a process note.** macOS's `/usr/bin/rsync` is openrsync (protocol 29). It accepted `--backup --backup-dir` but did not back deletions up. So the re-sync removed 94 clone-only files from `W/proj_es_final`: the 10-08 tools, the old exit-sign probe and tests, and Office files that main has since deleted. The clone had no unique work, and every one of those files had a copy elsewhere in W. Those copies are now in `W/_partial/proj_es_final_presync_1022`, with `RESTORE_MAP.txt`, the dry-run list and a README. They are the sources the clone was built from, not byte-checked against the removed files. Next time with openrsync: dry-run the deletions and `mv` them out first.

## 6. Files

| Path | What |
|---|---|
| `W/es_litlock/payload/…/FrontRoomsExitSign.cs` | `LockLit`, `SettleMains`, `Configure` / `SetMains` / `NeedsTick` gates, summary comments |
| `W/es_litlock/payload/…/FrontRoomsExitSignTests.cs` | G3 (`G3LitLock`), header entry, the 13th `Run` line |
| `W/es_litlock/lit_lock.diff` | Unified diff against main's files |
| `W/es_litlock/base/` | Main's two files as of 07:25 |
| `W/es_litlock/logs/` | `c01_compile.log`, `t01_tests.log` (+ `.status`), `roslyn_osx_pre.txt`, `roslyn_ios_pre.txt` |
| `W/es_litlock/tests/` | `tests.json`, `log.txt`, `level_trace.csv`, `sign_names.txt` |
| `W/tools/_rebase/es_litlock_osx`, `…_ios` | The Roslyn rsp files that were run |

## 7. Apply package (v2)

Made 2026-10-22 07:43 on main `0ea38bb` + working tree. Load was 290–325 throughout, so the timings are **UNDER LOAD**.

| File in `W/apply/` | What |
|---|---|
| `exit_sign_litlock.sh` | The apply script. It carries `# apply-convention: v2` and honours `APPLY_REAL`, `APPLY_DIR` and `APPLY_BACKUP` |
| `exit_sign_litlock_bases.txt` | `e2811567…  Assets/Scripts/Rendering/FrontRoomsExitSign.cs`, `1eebed73…  Assets/Editor/Rendering/FrontRoomsExitSignTests.cs` (main now; these are the fix1a and fix1b payloads) |
| `exit_sign_litlock_keep.txt` | Both `.meta` files, never written: `3f62e9fa…` and `5697d1d4…` |
| `exit_sign_litlock_expected.txt` | `4848e486…` and `4132f23c…`: the verified payload |
| `exit_sign_litlock_payload/`, `exit_sign_litlock_base/` | The two payload files, and exact copies of main's two files (sha1 = bases) |
| `exit_sign_litlock.diff` | Unified diff from base to payload. Its hunks equal `W/es_litlock/lit_lock.diff` |

```
bash /Users/redwang/FrontRoomsVisualWork/apply/exit_sign_litlock.sh            # dry run: prints DRY RUN OK, writes nothing
bash /Users/redwang/FrontRoomsVisualWork/apply/exit_sign_litlock.sh --apply    # writes the 2 files (main's copies backed up to W/backup/exit_sign_litlock_<stamp>/)
```

- **Dry run against main** (07:45): `DRY RUN OK`, exit 0. Main's two files and their `.meta` still had the same sha1 afterwards, and no backup folder was made. Log: `W/tools/_test/exit_sign_litlock_1022/main_dry_run.log`.
- **Scratch tests** (`W/tools/_test/exit_sign_litlock_1022/run_tests.sh`, 8 s, UNDER LOAD): **18 passed, 0 failed.** Log: `t_apply_script_tests.log`.
  - A fresh copy: the dry run is OK and writes nothing. `--apply` gives MANIFEST OK: the target equals the payload, the metas are unchanged, the backup equals the bases, and only the two paths changed. The manifest is written next to the scratch target, never into `W/apply/`. Then the dry run and a second `--apply` both print ALREADY APPLIED and write nothing.
  - Each of these is REFUSED (exit 1, nothing written): half applied (1 of 2), a moved base, a changed `.meta` GUID, a tampered payload, a stray payload file, and a folder that is not the project. A bad argument prints usage (exit 2).
- **On a base mismatch** (someone edits either file first), run `W/tools/rebase_apply.sh exit_sign_litlock` before any re-verification.
- **After `--apply`:** an open Unity recompiles once. The check is `FrontRoomsExitSignTests.RunBatch -quit`, which must give 13 passed. Log the merge in `W/apply/MERGED.md`.

