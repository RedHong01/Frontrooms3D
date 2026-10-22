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
