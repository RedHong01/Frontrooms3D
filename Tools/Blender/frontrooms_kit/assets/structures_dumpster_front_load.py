"""Kit_DumpsterFrontLoad: a 6 cu yd front-load container: steel body with the slanted front, two black HDPE lids, side fork sleeves on gussets, top rail, skids, drain plug; its back to the wall (structures G4-a 2026-11-10, package structures_dock, 30_plan.md P19; 63 section 6.1). Built in _struct_dock."""

import _run_b_common as B
import _struct_dock as D

NAME = "Kit_DumpsterFrontLoad"
SMOOTH_ANGLE = 40.0
BUDGET = (3500, 1200, 200)


def build(kit):
    B.levels(kit, D.dumpster)
    D.dumpster_meta(kit)
    D.finish(kit, (6.0, 18.0, 60.0), BUDGET, tags=("structures", "run", "dock", "p19"))
