"""Kit_RiserPipe_3m: a 4 in sch 10 sprinkler riser 3 m long, roll-grooved, its grooved coupling at the top joint (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; S5-05; LS119). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_RiserPipe_3m"
SMOOTH_ANGLE = 40.0
BUDGET = (1400, 500, 60)


def build(kit):
    B.levels(kit, lambda k, lv: R.riser(k, lv, 3.0))
    R.riser_meta(kit, 3.0, R.RISER_R)
    R.finish(kit, (6.0, 18.0, 60.0), BUDGET, tags=("structures", "run", "shaft", "p20"))
