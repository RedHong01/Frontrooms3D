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
