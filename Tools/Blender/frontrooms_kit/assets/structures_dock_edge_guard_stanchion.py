"""Kit_DockEdgeGuard_Stanchion: a 1-1/2 in pipe stanchion 42 in tall on its bolted base flange, chain eyes at 1.00 m both sides (structures G4-a 2026-11-10, package structures_dock, 30_plan.md P19; 63 section 6.1). Built in _struct_dock."""

import _run_b_common as B
import _struct_dock as D

NAME = "Kit_DockEdgeGuard_Stanchion"
SMOOTH_ANGLE = 40.0
BUDGET = (900, 300, 60)


def build(kit):
    B.levels(kit, D.stanchion)
    D.stanchion_meta(kit)
    D.finish(kit, (4.0, 12.0, 40.0), BUDGET, tags=("structures", "run", "dock", "p19"))
