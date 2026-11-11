"""Kit_Bollard: a 6-5/8 in schedule 40 pipe bollard, concrete filled and crowned, 1.07 m, set in the slab with a caulked joint, dented where a truck hit it (structures G4-a 2026-11-10, package structures_dock, 30_plan.md P19; 63 section 6.1). Built in _struct_dock."""

import _run_b_common as B
import _struct_dock as D

NAME = "Kit_Bollard"
SMOOTH_ANGLE = 40.0
BUDGET = (1400, 300, 60)


def build(kit):
    B.levels(kit, D.bollard)
    D.bollard_meta(kit)
    D.finish(kit, (4.0, 12.0, 40.0), BUDGET, tags=("structures", "run", "dock", "p19"))
