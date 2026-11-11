"""Kit_DockBumper: a laminated rubber dock bumper 10 x 18 in, 4-1/2 in projection: 23 tyre-rubber pads between two 4 in angles; origin on the dock face at its base (structures G4-a 2026-11-10, package structures_dock, 30_plan.md P19; 63 section 6.1). Built in _struct_dock."""

import _run_b_common as B
import _struct_dock as D

NAME = "Kit_DockBumper"
SMOOTH_ANGLE = 40.0
BUDGET = (1600, 320, 24)


def build(kit):
    B.levels(kit, D.bumper)
    D.bumper_meta(kit)
    D.finish(kit, (4.0, 12.0, 40.0), BUDGET, tags=("structures", "run", "dock", "p19", "wall_unit"))
