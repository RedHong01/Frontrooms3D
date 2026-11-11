"""Kit_Duct_Slope8: the 24 x 12 in duct's 1:8 pitch change (7.125 deg) as a gored two-piece fitting with joints both ends and a standing gore seam (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; 63 6.1 (slope piece)). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_Duct_Slope8"
SMOOTH_ANGLE = 30.0
BUDGET = (600, 200, 30)


def build(kit):
    B.levels(kit, lambda k, lv: R.duct_slope(k, lv))
    R.duct_meta(kit, 0.15)
    R.finish(kit, (6.0, 18.0, 60.0), BUDGET, tags=("structures", "run", "services", "p20", "ceiling"))
