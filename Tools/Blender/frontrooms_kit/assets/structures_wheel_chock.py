"""Kit_WheelChock: a moulded rubber truck chock with its forged eye, lying out with its chain trailing (structures G4-a 2026-11-10, package structures_dock, 30_plan.md P19; 63 section 6.1). Built in _struct_dock."""

import _run_b_common as B
import _struct_dock as D

NAME = "Kit_WheelChock"
SMOOTH_ANGLE = 40.0
BUDGET = (1400, 400, 60)


def build(kit):
    B.levels(kit, D.chock)
    kit.no_collider(); kit.min_ceiling = 0.0
    D.finish(kit, (3.0, 9.0, 30.0), BUDGET, tags=("structures", "run", "dock", "p19"))
