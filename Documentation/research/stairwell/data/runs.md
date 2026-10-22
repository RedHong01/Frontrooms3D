# Stairwell directions: render runs (clone W/proj_stair, 2026-10-22)

All runs: `Unity -batchmode -quit -projectPath W/proj_stair -buildTarget OSXUniversal -executeMethod FrontRoomsStairLookdev.RunBatch -frGlassRT off ...` (harness: `FrontRoomsStairLookdev.cs.txt`, clone-only, `Assets/Editor/Rendering/`). Load average 120-300 throughout. Exit 0 and 0 `error CS` every time (`run_status.txt`). Roslyn `roslyn_check.py --all` COMPILE OK before each run.

| Run | Args | What it is | Used for |
|---|---|---|---|
| t1 | `-stairShots door,landing,flight,detail -stairNoE -stairNoKits` | first look; found the shaft side walls showing through the maze wall, a bad detail camera | nothing (fixed) |
| r1 | all shots, `-stairLamp centre` (landing fixture over the landing centre) | first full set | `metrics_r1_centre_lamps.json` (centre vs edge comparison; B door was dark grey-brown here: B maze Relay 3.65:1 FAIL) |
| r2 / r3 | all shots, `-stairLamp edge` (fixture over the end-wall edge of each landing), B door grey primer, Relay parts mask added in r3 | the spec'd B and C | final B, C frames |
| sweep_g2, sweep_g3 | `-stairLampGain 2 / 3 -stairNoEndDado -stairShots relay_stair` | does more light fix the stair Relay read? (no) | `metrics_sweep_gain{2,3}.json` |
| r4 / r5 | `-stairRemedy -stairDirs A,B` | readability remedy: A light-grey fill + band only along the flights (adopted); B white band 1.5 m (r5, flat paint; not adopted, still 3.39:1) | `metrics_remedy_AB.json` |
| r6 | `-stairRemedy -stairDirs A`, all shots | the spec'd A | final A frames |
| r7 | `-stairShots detail` (A with `-stairRemedy`) | re-aimed 0.3 m detail camera | final detail frames |
| r8 | `-stairLampGain 0 -stairShots flight,landing` | stair fixtures off: what the game's global fill alone gives | `metrics_lamps_off.json` |

Final set = A from r6 (+ r7 detail), B and C from r3 (+ r7 detail): `metrics_final.json`, `stats_{A,B,C}.txt`, `poses_{A,B,C}.tsv`, `log_{A,B,C}.txt`.
Measure: `measure_stair.py.txt <run dir> <out json>`; images: `make_images.py.txt <run dir> <metrics> <images dir>`.
Textures (our own, procedural): `gen_stair_textures.py.txt` writes `Stair_Concrete_{A,N,S}` and `Stair_ConcreteFloor_{A,N,S}` into the clone's `Resources/Surfaces/Textures`.
Raw renders (PNG, ID masks, float E passes): `W/stair_work/runs/`. Blockout kit prefabs: `W/proj_stair/Assets/StairBlockout/{A,B,C}/BK_*.prefab`.

## Fix stage (stage 5, 2026-10-22 12:15–12:30, after the account switch)

Same command line plus the new flags; harness `FrontRoomsStairLookdev.cs.txt` (fix stage). Roslyn `roslyn_check.py --touch Assets/Editor/Rendering/FrontRoomsStairLookdev.cs --ref W/proj_stair`: 0 errors before every run. Unity exit 0, 0 `error CS` every run (`run_status_fix.txt`). UNDER LOAD: 1-min load 54–214. At most 2 Unity jobs on the visual line: the second pass waited for a slot (`wait_and_run.zsh`).

| Run | Args | What it is | Used for |
|---|---|---|---|
| f1, f2_ladder, f3_ladder_dead | default / `-stairLadder -stairLadderLamps hold` / `… dead -stairShots …` | first pass of the fix harness | found: B wear flecks too busy; the door-head strip unexplained |
| f4_diag | `-stairDirs A -stairShots door,diag_head,diag_head_noshadow,diag_head_side` | the door-head strip: shadow, not LOD | `stair_v_door_head.jpg` (re-made from f8) |
| f5, f6_ladder, f7_ladder_dead | second pass: exit frame + sign cast no shadow, B wear restyled | — | superseded |
| **f8** | `-stairLamp edge` (all shots incl. sign gain sweep with raw HDR, diagnostics) | final stair set: door-landing lights 0.80 m off the door wall, gains A 2.4 / B 2.9 / C 1.5, EXIT gain 2.0 | `metrics_fix_stair.json`, `stats_fix_*`, `poses_fix_*`, `log_fix_*` |
| **f9_ladder** | `-stairLadder -stairLadderLamps hold -stairNoE` | exit approach, 63 × 33 m hall, tier-5 seed 5, exit cells held Steady | `metrics_fix_ladder_hold.json`, `poses_fix_ladder_*`, `log_fix_ladder_*` |
| **f10_ladder_dead** | `-stairLadder -stairLadderLamps dead -stairNoE -stairShots lad12_00_lk,lad20_00_lk,lad12_45_lk,relay_45,relay_70,relay_00` | the same with the exit cells Dead | `metrics_fix_ladder_dead.json` |

Measure: `measure_fix.py.txt <run dir> <out json> [E_ref]` (the ladder runs use the stair run's E_ref 1.2831). Images: `make_fix_images.py.txt <f8> <f8 json> <f9> <f9 json> <f10> <f10 json> <f4> <images dir>`. Placeholder textures: `gen_stair_numerals.py.txt W/proj_stair/Assets/StairLookdevTex`.
