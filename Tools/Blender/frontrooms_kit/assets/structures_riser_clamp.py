"""Kit_RiserClamp: a two-bolt riser clamp for 4 in pipe bearing on a beam (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; S5-05). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_RiserClamp"
SMOOTH_ANGLE = 40.0
BUDGET = (700, 220, 40)


def build(kit):
    B.levels(kit, lambda k, lv: R.riser_clamp(k, lv))
    R.riser_clamp_meta(kit)
    R.finish(kit, (4.0, 12.0, 40.0), BUDGET, tags=("structures", "run", "shaft", "p20"))
