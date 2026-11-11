"""Kit_Duct_0500: a 0.5 m cut length of the 24 x 12 in duct (the short pieces past the fire walls) (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; 63 6.1). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_Duct_0500"
SMOOTH_ANGLE = 30.0
BUDGET = (300, 120, 20)


def build(kit):
    B.levels(kit, lambda k, lv: R.duct(k, lv, 0.5, 1))
    R.duct_meta(kit, 0.5)
    R.finish(kit, (6.0, 18.0, 60.0), BUDGET, tags=("structures", "run", "services", "p20", "ceiling"))
