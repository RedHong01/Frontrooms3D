"""Kit_SprinklerMain_Slope8: the 4 in sprinkler main's 1:8 pitch change: two stubs in a flexible grooved coupling (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; LS119). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_SprinklerMain_Slope8"
SMOOTH_ANGLE = 40.0
BUDGET = (900, 300, 60)


def build(kit):
    B.levels(kit, lambda k, lv: R.main_slope(k, lv))
    R.main_slope_meta(kit)
    R.finish(kit, (4.0, 12.0, 40.0), BUDGET, tags=("structures", "run", "services", "p20", "ceiling"))
