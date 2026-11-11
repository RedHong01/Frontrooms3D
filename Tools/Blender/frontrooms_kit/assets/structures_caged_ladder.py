"""Kit_CagedLadder: a 4.50 m fixed steel ladder with its cage, hot-dip galvanised (OSHA 1910.27, 1971) (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; S5-05; OSHA 1910.27). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_CagedLadder"
SMOOTH_ANGLE = 40.0
BUDGET = (7000, 2500, 400)


def build(kit):
    B.levels(kit, lambda k, lv: R.caged_ladder(k, lv))
    R.caged_ladder_meta(kit)
    R.finish(kit, (8.0, 24.0, 0.0), BUDGET, tags=("structures", "run", "shaft", "p20"))
