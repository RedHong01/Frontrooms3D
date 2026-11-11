"""Kit_DuctWallCollar: the fire damper sleeve's 1-1/2 in retaining angles round the 24 x 12 in duct on a rated wall's face (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; UL 555 damper installation). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_DuctWallCollar"
SMOOTH_ANGLE = 35.0
BUDGET = (350, 100, 24)


def build(kit):
    B.levels(kit, lambda k, lv: R.duct_collar(k, lv))
    R.duct_collar_meta(kit)
    R.finish(kit, (4.0, 12.0, 40.0), BUDGET, tags=("structures", "run", "services", "p20"))
