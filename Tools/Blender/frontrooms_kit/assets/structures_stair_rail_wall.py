"""Kit_StairRail_Wall: the wall rail with extensions, returns and G1 brackets (stage STAIR author 2026-11-10, package stairwell_walkable). Built in _struct_stair.rail_wall."""

import _run_b_common as B
import _struct_common as S
import _struct_stair as T

NAME = "Kit_StairRail_Wall"
SMOOTH_ANGLE = 40.0
BUDGET = (1800, 600, 90)


def build(kit):
    B.levels(kit, T.rail_wall)
    T.rail_wall_meta(kit)
    S.finish(kit, (4.0, 10.0, 0.0), BUDGET, tags=("structures", "stair", "stairwell_walkable"))
    kit.meta["structures"] = "stairwell_walkable STAIR author 2026-11-10 (research/stairwell/80 section 6)"
