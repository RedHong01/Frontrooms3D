"""Kit_WheelChock_Hung: the rubber chock hung on its wall hook by its chain (origin on the wall plane at the hook) (structures G4-a 2026-11-10, package structures_dock, 30_plan.md P19; 63 section 6.1). Built in _struct_dock."""

import _run_b_common as B
import _struct_dock as D

NAME = "Kit_WheelChock_Hung"
SMOOTH_ANGLE = 40.0
BUDGET = (1400, 400, 60)


def build(kit):
    B.levels(kit, D.chock_hung)
    kit.no_collider(); kit.min_ceiling = 0.0
    D.finish(kit, (3.0, 9.0, 30.0), BUDGET, tags=("structures", "run", "dock", "p19", "wall_unit"))
