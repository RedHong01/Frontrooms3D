"""Kit_DockEdgeGuard_Gate: the self-closing 1 in pipe swing gate between two stanchions 1.20 m apart, clamp hinges (the upper sprung), gravity latch and keeper; origin on the hinge stanchion's axis at the floor, the leaf closed along +X (structures G4-a 2026-11-10, package structures_dock, 30_plan.md P19; 63 section 6.1). Built in _struct_dock."""

import _run_b_common as B
import _struct_dock as D

NAME = "Kit_DockEdgeGuard_Gate"
SMOOTH_ANGLE = 40.0
BUDGET = (1800, 600, 80)


def build(kit):
    B.levels(kit, D.gate)
    D.gate_meta(kit)
    D.finish(kit, (4.0, 12.0, 40.0), BUDGET, tags=("structures", "run", "dock", "p19"))
