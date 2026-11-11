"""Kit_CCTVCamera: a 1980s CCTV box camera in an extruded housing with a sunshield, on a wall arm with a tilt head, coax and power to a wall box; origin on the wall plane, looking 35 deg to +X and 25 deg down (structures G4-a 2026-11-10, package structures_dock, 30_plan.md P19; 63 section 6.1). Built in _struct_dock."""

import _run_b_common as B
import _struct_dock as D

NAME = "Kit_CCTVCamera"
SMOOTH_ANGLE = 40.0
BUDGET = (1600, 500, 60)


def build(kit):
    B.levels(kit, D.cctv)
    D.cctv_meta(kit)
    D.finish(kit, (3.0, 9.0, 30.0), BUDGET, tags=("structures", "run", "dock", "p19", "wall_unit"))
