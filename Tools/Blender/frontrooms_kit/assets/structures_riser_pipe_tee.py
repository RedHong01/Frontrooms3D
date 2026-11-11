"""Kit_RiserPipe_Tee: the grooved tee on the 4 in main that the riser rises from (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; S5-05). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_RiserPipe_Tee"
SMOOTH_ANGLE = 40.0
BUDGET = (1600, 500, 60)


def build(kit):
    B.levels(kit, lambda k, lv: R.riser_tee(k, lv))
    R.riser_tee_meta(kit)
    R.finish(kit, (4.0, 12.0, 40.0), BUDGET, tags=("structures", "run", "shaft", "p20"))
