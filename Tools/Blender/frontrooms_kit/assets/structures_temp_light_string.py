"""Kit_TempLightString: 3 m of yellow SJ cord tied at 1 m with three caged 150 W work lamps hanging, all lit (SP7 #2 FAR SHORE; built, not wired) (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; 62 Kit_TempLightString; LS99; 72 3.2). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_TempLightString"
SMOOTH_ANGLE = 35.0
BUDGET = (2400, 800, 120)


def build(kit):
    B.levels(kit, lambda k, lv: R.temp_light_string(k, lv))
    R.temp_light_string_meta(kit, (True, True, True))
    R.finish(kit, (5.0, 15.0, 50.0), BUDGET, tags=("structures", "sp7", "p20", "unwired"))
