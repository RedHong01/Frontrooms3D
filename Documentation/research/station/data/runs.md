# Station blockouts: render runs (clone W/proj_station, 2026-10-22)

All runs: `Unity -batchmode -quit -projectPath W/proj_station -buildTarget OSXUniversal -executeMethod FrontRoomsStationLookdev.RunBatch -stationOut W/station_work/runs/<run> -frGlassRT off`.
- **Harness:** `FrontRoomsStationLookdev.cs.txt`, clone-only, in `Assets/Editor/StationLookdev/`. ASCII only, no tuples. Compiled with `roslyn_check.py` before every run: 0 errors.
- **Clone:** `mkclone.sh` 2026-10-22 11:04 from main 18acaa16 + working tree. Compile log `W/logs/station_compile.log`: 0 `error CS`. On 12:0x its `Assets/Scripts`, `Resources`, `Shaders` and `Settings` were byte-identical to main c462951a (`diff -rq`). The only differences are the shared clone tools and this harness.
- **Card textures:** the five still cards of `FrontRooms_Ad_01.mp4` (sha1 8bde2101), decoded with ffmpeg to `W/station_work/cards/`. Copied to the clone as `Assets/StationLookdevTex/card_{1..5}.png`.
- **Load:** every run was UNDER LOAD (1-min load 46–314). Each run took 40–67 s for all its plans, about 5 s a plan.
- **Unity exits:** 0 every time, with 0 `error CS` (`W/station_work/runs/*/run_status.txt`). Logs: `W/logs/station_r{1..6}.log`.

| Run | Plans | What changed | Used for |
|---|---|---|---|
| r1 | W, W′, D, D′, H | First full set. Found: the D/D′ key cabinet sat inside E2's keep-clear strip, so the map left it out; virtual footprints for the module check were too loose | nothing (fixed) |
| r2 | same | Cabinet moved to E-wall z 8.15. Bracket and plate footprints measured. Wall clipboard dropped in D. Validate 0/0 | nothing (set bottom 2.09) |
| r3 | same | Bracket screen 2.40 m, so the set bottom is 2.11 ≥ 2.10 | First measure. Found: the ID portal boxes hid what lies beyond the openings, and the instanced wall outlets leaked into the ID passes |
| r4 | + W′ rev 2 | ID pass without portals (`_idnp`). Outlets off in the flat passes. W′ rev 2 = counter 0.56 m west, strip 2.0 m. H chair pushed in | nothing (wiggle still used portal IDs) |
| r5 | + W′ rev 3, D′ rev 3 | Wiggle on `_idnp`. Rev 3 = E1 on the set's axis | nothing (lamps not yet per V1) |
| **r6** | W, W′, W′ rev 2, W′ rev 3, D, D′, D′ rev 3, H | **Lamps per tutorial V1:** ≥ 2 Steady on the entrance side, TV side Off. The conduit runs straight at hook height, clear of the sign. The rev 2/3 key cabinet moved to the N wall, off the strip. H counter 0.2 m east (strip 1.64 m) | **Final set:** `blockout_metrics.json`, every image in `../images/` (VL354–VL358) |

**Measure:** `measure_station.py.txt <run dir> <ad30.rgb> <out json>`. **Images:** `make_station_images.py.txt <run dir> <metrics json> <images dir>` (shotframe, SHOT_FORMAT).
**Raw renders** (PNG beauty, ID masks, float E passes, walk grids): `W/station_work/runs/r6/<plan>/`.
**Per plan:** poses in `poses_<plan>.tsv` (eye, look, vFOV); the module plus virtual props in `props_<plan>.tsv`.

## r7 (fix stage after the two critics, account B 14:09–14:48; checked and re-framed on account A 15:0x)

Harness r7 (`FrontRoomsStationLookdev.cs.txt`; r6 kept as `FrontRoomsStationLookdev_r6.cs.txt`), measure `measure_station.py.txt` (r6 kept as `_r6`), images `make_station_images.py.txt` (r6 kept as `_r6`), V3 grid `grid_r7.py.txt`. Logs `W/logs/station_r7*.log`. Every run exit 0, 0 `error CS`, UNDER LOAD (1-min load 45–127).

| Run | Time | Plans | What | Used for |
|---|---|---|---|---|
| r7a | 14:09–14:10 | Wp4, Dp4, H (`-stationGridOnly`) | V3 light grid at the take: 7 light options × 5 cards | V3 r7 = k 6, floor 0.81, luminance kept, 60 % white, cookie (`v3_grid_r7a.json`, `station_r7_c3_v3grid.jpg`) |
| r7 | 14:13–14:19 | Wp4, Wp4o, Dp4, H + `sweep_Wp4`, `sweep_Dp4` | first r7 set; 30 random seeds per module + one door seed in the real generator (1,201 and 4,192 scanned) | the sweeps (symlinked into r7b) |
| r7b | 14:26–14:27 | Wp4, Wp4o, Wp4s, Dp4, H | **final hero plans** | `blockout_metrics_r7.json` (measured 14:48), every `../images/station_r7_*` |
| r7c–r7e | 14:28–14:38 | Wp4 | light-leak diagnosis: shadow modes, cookie, outer cone, a closed test box, a fresh light, all other lights off | VL380 (the leak is the same in every mode) |
| r7s, r7s2 | 14:2x–14:37 | an isolated test wall | shadows away from the map, and 20 m above it | `shadowtest_r7s.tsv` |

Relay clip: `relay_clip_r7.json` (prototype rig 3.03–3.15 m tall, so no clip test). Poses and props per plan: `poses_r7_<plan>.tsv`, `props_r7_<plan>.tsv`.
