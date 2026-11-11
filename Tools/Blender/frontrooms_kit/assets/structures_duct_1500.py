"""Kit_Duct_1500: one 1.5 m section of the 24 x 12 in duct (fills a run; K4 scales it 0.75-1.25 along its axis) (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; 63 6.1). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_Duct_1500"
SMOOTH_ANGLE = 30.0
BUDGET = (450, 150, 20)


def build(kit):
    B.levels(kit, lambda k, lv: R.duct(k, lv, 1.5, 1))
    R.duct_meta(kit, 1.5)
    R.finish(kit, (6.0, 18.0, 60.0), BUDGET, tags=("structures", "run", "services", "p20", "ceiling"))
