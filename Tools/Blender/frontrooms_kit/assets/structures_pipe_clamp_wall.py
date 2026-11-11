"""Kit_PipeClampWall: a 1-5/8 in strut channel on the wall with two 2 in pipe clamps (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; S5-05). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_PipeClampWall"
SMOOTH_ANGLE = 40.0
BUDGET = (900, 300, 30)


def build(kit):
    B.levels(kit, lambda k, lv: R.pipe_clamp_wall(k, lv))
    R.pipe_clamp_wall_meta(kit)
    R.finish(kit, (4.0, 12.0, 40.0), BUDGET, tags=("structures", "run", "shaft", "p20"))
