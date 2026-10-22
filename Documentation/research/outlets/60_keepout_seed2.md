# 60: The seed 2 keep-out hit (2026-10-21)

From the visual chat (游戏视觉), outlets diagnosis, **2026-10-21 22:10–22:25**. This answers 关卡设计's report of about 22:00: after tonight's landings plus C-1, the contract probe gives one keep-out hit, `KEEPOUT seed 2 wall Duplex at (5024.92, 0.41, 5004.30)`. Seed 2 has 879 → 880 fixtures and 415 → 416 ground-up lines, and the planner's `keptOut` went 1 → 0.

## 1. Verdict

**Cause (c): a probe defect, plus a stale baseline. Both are in the visual chat's published files. The game is not affected, and nothing in the map chat's files has to change.**

- **The planner is right (not a).** The plate sits on the corridor face of the x = 9 m wall, 0.30 m from the corner. The keep-out it touches is the **zone key spot inside room 2, behind that wall.** The key is 0.28 m behind the plate's wall plane and on the far side of the z = 12 m room wall. The key spot's 1.2 m square reaches through both walls, and the plate overlaps its last 14 mm in z. Since the fix pass, the planner's rule is that a wall fixture answers only to keep-outs in front of its own wall. `InFront` gives −0.28, so this keep-out is ignored, as designed.
- **The map change does nothing here (not b).** The probe was rebuilt with main's 10-14 MapWorld (`11586bbc`) + C-1, keeping everything else as it is tonight. For seed 2 it gives a **byte-identical** plan signature: all 25 chunks' faces, keep-outs, fixtures and occluders. It also gives the same hit. MapWorld `c0946262` (broken doors, AimKind, `EdgeNoGenerate` in `LampModeOf`, the owned key material) does not touch `BuildEdge`, `AddWallFaces`, the chunk data or the key spot. The only change on the map side is in the renderer count, and that count is the same with outlets on and off.
- **The probe is wrong (c).** The probe the map chat ran, `data/30_FrontRoomsOutletContractProbe.cs.txt`, was copied at 10-14 **18:03**. That was before the fix pass added `InFront`, at about 18:08–18:10. The fix pass added it to the planner (`BlockedBox`, `MeetsKeepOut`) and to its own probe (`proj_outlet_int_t`, 18:10, sha `1ba48cea`). The published probe tests keep-outs **without** `InFront`, so it counts a plate the design allows.
- **The baseline is stale too.** `data/40_probe10.json` is the **17:34** run, which used the planner before `InFront`. It is byte-identical to `W/outlet_fix/out/probe10.json`, sha `1ed338e2`. In that run, this same plate was kept out (`keptOut 1`, 879 fixtures). The final 10-14 run is `W/outlet_fix/out/probe10b.json` (18:13, InFront planner and InFront probe). It already reads **seed 2 880 fixtures, keptOut 0, 416 ground-up lines, 0 hits**: exactly tonight's numbers. So "1 → 0" and "879 → 880" are not changes between 10-14 and tonight. They are the 17:34 and 18:13 runs of 10-14.

**With the 18:10 probe, tonight's main + C-1 passes.** Over the 10 seeds with HEAVY 10, `"pass": true` and 0 keep-out hits. Every number equals the final 10-14 run (`probe10b`) except the renderer counts. Those are the C2 posts, and on and off are equal for every seed.

## 2. What was run

| Step | State | Result |
|---|---|---|
| Re-sync | `W/proj_es_fix1` ← main `dfc0086` + working tree at 22:11, with the mkclone steps: rsync of the 5 folders with the iCloud excludes, `apply_local_patches.py` ("nothing to do"), `install_tools.py`. Log: `W/logs/es_fix1_resync_60.log` | Scripts identical to main |
| C-1 | `python3 data/30_contract_patch.py.txt <clone>/…/FrontRoomsMapWorld.cs`. The base is main's `c0946262` and the patch's own `assert "FrontRoomsWallFixtures" not in s` passed. The patch has `probe = False` hard-coded, so it writes no timers | MapWorld sha `7ef0ca45`, **identical to the map chat's `~/FrontRoomsMapWork/proj3`**, so the hunk is the same |
| C-2, EXAMINE step 1 | Already in main (`FrontRoomsModuleEditing.PlannerOnly`, line 70). C-2 only filters the Level Designer palette, so it cannot change the probe | — |
| Probe | `data/30_…Probe.cs.txt` → `Assets/Editor/OutletProbeClone/FrontRoomsOutletContractProbe.cs`. Sha `89a20413`, identical to proj3's. The `.meta` was copied from proj3 | — |
| Compile + `Assets/Scripts` force reimport | `-buildTarget OSXUniversal` (`W/logs/es_fix1_60_compile.log`) | **0 errors** |
| Reproduction | `OUTLET_PROBE_SEEDS=2 OUTLET_PROBE_HEAVY=10` (`W/logs/es_fix1_60/probe_seed2.log`) | **`KEEPOUT seed 2 wall Duplex at (5024.92, 0.41, 5004.30)`**: seed 2 880 fixtures, 1 hit, 416 lines, 3480 renderers, the same as the map chat's json → `data/60_probe_seed2_v5_tonight.json` |
| Diagnostics | Clone-only `FrontRoomsDiag60.Run` (`data/60_FrontRoomsDiag60.cs.txt`, ASCII, no tuples). It builds seed 2 exactly as `RunSeed` does: root at 26 × WorldPeriod, the profile with the seed overridden, `BuildForCapture`. It also uses three clone-only hooks in `FrontRoomsWallFixtures.cs` (`data/60_diag60_planner_hooks.diff.txt`): `Diag60NoInFront` (the 17:34 rule) and a list of what `AddReceptacle` keeps out. With the flag off the plan is unchanged: pass 1's signature has the same fixtures as the probe | `data/60_diag_tonight.json`, `data/60_sig_seed2_mainrule.txt`, `data/60_sig_seed2_noinfront.txt` |
| Baseline (map side) | Scratch copy `W/proj_es_fix1_b1014`, now `W/_partial/proj_es_fix1_b1014`. Only two files differ from the clone: `FrontRoomsMapWorld.cs` = `git show HEAD:` (`11586bbc`) + C-1 → `195cb75d`, which is byte-identical to the 10-14 landing clone `proj_outlet_int_t`'s; and `FrontRoomsMapInteractionTests.cs` = `proj_outlet_int_t`'s, because only that test uses the new AimKind/Broken API. Roslyn and Unity compile: 0 errors | Probe v5: the **same hit** (`data/60_probe_seed2_v5_b1014.json`). Diag (`data/60_diag_b1014.json`): **both signature files are byte-identical to tonight's** (sha `466d82be` main rule, `15fb85d8` no-InFront rule) |
| Fixed probe | The 18:10 probe (`data/60_FrontRoomsOutletContractProbe_1810.cs.txt`, sha `1ba48cea`) in the clone, 10 seeds, HEAVY 10 | **`pass: true`**, 0 hits (`data/60_probe10_p1810_tonight.json`) |

**What changed since 10-14 (scripts):** the landing clone `proj_outlet_int_t` was compared with tonight's clone. `FrontRoomsWallFixtures.cs`, `…Set.cs`, `…Renderer.cs` and `FrontRoomsOfficeKit.cs` are byte-identical. `FrontRoomsRoomStream.cs` differs only in the title-stream exit sign. Everything else in `Assets/Scripts/FrontRoomsMap` is identical apart from MapWorld. In Resources only the outlet kits (LODs) and the exit-sign sidecars changed, and the module data did not. So MapWorld is the only file that changed and could change the planner's inputs, and the baseline above rules it out.

## 3. The hit, dumped (`data/60_diag_tonight.json`, pass 1)

| Field | Value |
|---|---|
| Chunk | (1, 0), root (5016, 0, 4992). Fixture #25, category `wall` (own face, not moved, not a companion) |
| Fixture | `Duplex`, `Level0Corridor`, flags None, Almond/Ivory, local (8.92, 0.41, 12.30), normal (−1, 0, 0), along 0.30 (the end margin) |
| Face | #74, edgeId `85899345929`, side 0, **Wall**, start (8.92, 0, 12), direction +Z, length 3, not foreign, room −1. It serves cell (10, 4), a corridor; the cell behind it is (11, 4), also a corridor; zone (1, 0) Level0 Standard |
| PlateBounds = FixtureVolume | x 8.908–8.920, y 0.3529–0.4672, z 12.2451–12.3549 |
| Keep-out met | Its own set's #6, source **keySpot**: keyCell (11, 3), key at local (9.20, 11.66), y 0.66. Rect x 8.60–9.80, z 11.06–12.26, y 0–2.1 (world x 5024.6–5025.8, z 5003.06–5004.26) |
| Where the keep-out's centre is | Cell (11, 3), local (3, 3), **room 2, a 4 × 3 Level0 module** |
| Overlap | x 8.909–8.919 × z 12.2459–12.2600: a sliver 14 mm deep in z, across the plate's 12 mm volume |
| InFront | dot = (9.20 − 8.92) × (−1) = **−0.28 → behind** (threshold −0.05) |
| `Blocked()` with its own list (main) | **false**. Called by reflection on the real method. Trace: "own ko#6 overlaps, InFront dot −0.28 (behind)" |
| `BlockedBox` without InFront (the 17:34 rule) | **true** |
| `set.Occluded(25)` | **false**: no own occluders; the face is not foreign, so the set's own keep-outs are not consulted; no other set's occluders; no other set's keep-out meets it with InFront |
| Probe v5 rule (no InFront) | **hit** |
| Probe 18:10 rule (InFront for wall fixtures) | no hit |

**The geometry** (`data/60_sig_seed2_mainrule.txt`, chunk 1,0):

- **Room 2** holds cells (10, 3) and (11, 3). The wall along **z = 12 m** separates it from the corridor cells (10, 4) and (11, 4): edges `85899345926` and `94489280518`, Wall, with faces at z 11.92 (room) and 12.08 (corridor).
- The **x = 9 m wall** (`85899345929`) splits the two corridor cells. The plate is on its west face, 0.30 m north of the corner at (9, 12).
- The key lies in room 2, 0.20 m east of the x = 9 line and 0.26 m in from the room face of the z = 12 wall.
- The 0.6 m keep-out half-size carries its square through both walls into corridor cell (10, 4), to z 12.26: 0.18 m past the corridor face.
- **The plate and the key are in different spaces, with a wall between them.** The plate cannot hide, block or crowd the key.

## 4. Which plate the planner kept out on 10-14

Pass 2 of the same run sets `Diag60NoInFront = true`, which gives the planner of 10-14 17:34, before InFront. That run gives **879 fixtures, StatKeptOut 1, 0 keep-out hits.** The one receptacle it keeps out is **this plate**: chunk (1, 0), Duplex, local (8.92, 0.41, 12.30), face `85899345929`/0, along 0.30. The two signatures differ in exactly that line (`60_sig_seed2_mainrule.txt` vs `60_sig_seed2_noinfront.txt`: chunk 1,0 has 45 vs 44 fixtures). The plate is a non-ground-up Duplex on its own wall line, so it also accounts for 415 → 416 ground-up lines. The same holds with the 10-14 MapWorld (`60_diag_b1014.json`).

**The 10-14 timeline:**

| Time | Run (`W/outlet_fix`) | Planner | Probe | Seed 2 | 10-seed keptOut / hits |
|---|---|---|---|---|---|
| 17:34 | `probe10.json` = **`data/40_probe10.json`** (sha `1ed338e2`) | before InFront | v5, no InFront | 879, kept 1 | 1 / 0 |
| 18:03 | `data/30_FrontRoomsOutletContractProbe.cs.txt` copied to the docs | — | **v5, no InFront** | — | — |
| 18:08 | `probe100.json` (100 seeds) | before InFront | v5 | — | 54 kept / **1 hit (seed 3015)** |
| 18:10 | planner + probe fixed in `proj_outlet_int_t` (40_final.md issue 6) | InFront | **InFront** (sha `1ba48cea`) | — | — |
| 18:13 | `probe10b.json` (sha `cc59020d`, now `data/60_baseline_probe10b_1014_1813.json`) | InFront | InFront | **880, kept 0** | **0 / 0** |
| 18:20 | `probe100b.json` | InFront | InFront | — | 29 kept / 0 hits |
| 18:23 | `outlets_int` payload frozen. Its planner sha `b5d05f9e` is main's tonight | InFront | — | — | — |

40_final.md issue 6 says "The probe counts keep-out hits with the same rule". That is true of the 18:10 probe, but not of the copy that was published. Its §3.1 and `30_contract_map.md` §8.1 step 3 still give the 17:34 file as the expected result.

## 5. The cause, line by line

- **The probe defect:** `data/30_FrontRoomsOutletContractProbe.cs.txt`, `FixPassAudit`, **lines 508–516**. The `foreach (var (other, k) in kos)` test has no InFront term. The 18:10 version adds one at its lines 508–521 (`data/60_probe_v5_to_1810.diff.txt`, first hunk: `wallMounted`; skip when `(c − p)·n ≤ −0.05`).
- **The rule it disagrees with**, in main's `Assets/Scripts/Office/FrontRoomsWallFixtures.cs` (sha `b5d05f9e`):
  - **line 653**: `BlockedBox` … `(!wallMounted || InFront(k, centre, normal))`;
  - **lines 662–666**: `InFront`;
  - **line 413**: `if (Blocked(x, f, keepOut)) { StatKeptOut++; return; }`, which no longer fires for this plate;
  - **line 1118**: `MeetsKeepOut`, the draw-time rule, which also uses InFront.
- **The stale baseline and its docs:**
  - `data/40_probe10.json` (the 17:34 run);
  - `30_contract_map.md` **line 191** ("Expected (`data/40_probe10.json`, landing clone, 2026-10-14 17:34)");
  - `40_final.md` **line 155** (§3.1, "17:34; `data/40_probe10.json`");
  - `40_final.md` line 343 and `30_contract_map.md` line 234, which list `40_probe100.json` and `40_suites.txt`, but neither file exists in `data/`.
- **Not the cause**, each one checked:
  - the sidecars and `PlateHalf`, which are constants;
  - `FootprintCache`: all 21 keep-outs equal a fresh recomputation, with 0 source mismatches;
  - chunk ownership: the plate's face and the keep-out belong to the same chunk;
  - `Occluded`: it covers this case as designed, because a non-foreign plate is not drawn-hidden by its own set's keep-outs, and it is not in front of this one anyway;
  - MapWorld `c0946262`: the plan signature is identical (§2).

## 6. The fix and its owner

**Owner: 游戏视觉 (the outlets docs). Nothing for 关卡设计 to change in MapWorld; their C-1 landing is not blocked by this.**

1. Replace `data/30_FrontRoomsOutletContractProbe.cs.txt` with `data/60_FrontRoomsOutletContractProbe_1810.cs.txt`. It is the probe that verified the fix pass, and it compiles against the clean C-1 contract.
2. Replace `data/40_probe10.json` with `data/60_baseline_probe10b_1014_1813.json`. Correct `30_contract_map.md` §8.1 step 3 (line 191) and `40_final.md` §3.1 (line 155) to "18:13". Either publish `40_probe100.json` and `40_suites.txt`, which are `W/outlet_fix/out/suites/probe100b.json` and the suite logs, or drop the references.
3. **The map chat re-runs** §8.1 with the 18:10 probe. Expected, and measured tonight on main + C-1 (`data/60_probe10_p1810_tonight.json`):

   | Check | Value |
   |---|---|
   | `pass` | **true** |
   | Chunks / faces | 250 / 31,890 |
   | Fixtures | 8,836 (35.3 per chunk, max 61) |
   | `keptOut` | 0 |
   | Keep-out hits | 0 |
   | Rule, geometry, door/window, back-to-back, same-face, height and face-mismatch counts | all 0 |
   | Rebuild, shift and independent-build differences | 0 |
   | Collider / renderer delta, on vs off | 0 / 0 |
   | Handy boxes | 60, no line with more than one |
   | Ground-up lines | 4,095, 89 mixed |
   | Pods fed | 47 / 47 (the 18:10 probe counts monitorless pod runs, hence 47, not 46) |
   | Seed 2 | 880 fixtures, 416 lines, 3,480 renderers |

   The only difference from 10-14 18:13 is the renderer counts (C2's posts).
4. **Prediction, not run:** the old v5 probe on the 100-seed set would flag up to the 25 plates that InFront released between 18:08 and 18:20 (54 → 29 kept out). Those flags would be false too.

**A latent approximation, no action asked.** `InFront` tests the plate's **own** wall plane only, not the wall that actually separates the plate from the keep-out. Here the outcome is right: the z = 12 room wall separates them, and the key is also behind the plate's plane. A keep-out could, however, lie behind the plate's plane but in the plate's own space, round the free end of a wall stub. That keep-out would be ignored. No seed in the 10 + 100 + 25 has shown it. If it matters, the planner fix is to test the keep-out centre's cell against the face's cell (same cell, or open edges between them) instead of the plane. That is a visual-owned file, `FrontRoomsWallFixtures.InFront`.

## 7. Files (`data/60_*`)

- `60_diag_tonight.json`, `60_diag_b1014.json`: the full dumps. Pass 1 = main's rule, pass 2 = the 17:34 rule; plate, face, keep-outs, Blocked, Occluded.
- `60_sig_seed2_mainrule.txt`, `60_sig_seed2_noinfront.txt`: seed 2 plan signatures (all 25 chunks: faces, keep-outs, fixtures, occluders). Tonight's and the 10-14-MapWorld runs are byte-identical.
- `60_probe_seed2_v5_tonight.json`, `60_probe_seed2_v5_b1014.json`: the reproduction with the published probe.
- `60_probe10_p1810_tonight.json`: the 10 seeds with the 18:10 probe (pass).
- `60_baseline_probe10b_1014_1813.json`: the correct 10-14 baseline (`W/outlet_fix/out/probe10b.json`).
- `60_FrontRoomsOutletContractProbe_1810.cs.txt`, `60_probe_v5_to_1810.diff.txt`: the probe to publish, and its diff against the published one.
- `60_FrontRoomsDiag60.cs.txt`, `60_diag60_planner_hooks.diff.txt`: the clone-only diagnostics and planner hooks. They are **never for main**.
- `60_mapworld_11586bbc_to_c0946262.diff.txt`: the map-side change since 10-14, for reference.

**Clone state after this run.** `W/proj_es_fix1` = main 22:11 + C-1 + the Diag60 hooks + the 18:10 probe; see its `MKCLONE.txt`. The baseline scratch copy is in `W/_partial/proj_es_fix1_b1014`. Logs are in `W/logs/es_fix1_60/`. Main was not written outside `research/outlets/60_*`. No Unity process ran on main or proj3, and no git write was made.
