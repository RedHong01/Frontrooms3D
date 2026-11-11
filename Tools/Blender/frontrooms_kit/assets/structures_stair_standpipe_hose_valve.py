"""Kit_StandpipeHoseValve: the 4 in standpipe stub with a 2-1/2 in angle hose valve (stage STAIR author 2026-11-10, package stairwell_walkable). Built in _struct_stair.standpipe_valve."""

import _run_b_common as B
import _struct_common as S
import _struct_stair as T

NAME = "Kit_StandpipeHoseValve"
SMOOTH_ANGLE = 40.0
BUDGET = (4000, 1200, 200)


def build(kit):
    B.levels(kit, T.standpipe_valve)
    T.standpipe_meta(kit)
    S.finish(kit, (2.5, 8.0, 0.0), BUDGET, tags=("structures", "stair", "stairwell_walkable"))
    kit.meta["structures"] = "stairwell_walkable STAIR author 2026-11-10 (research/stairwell/80 section 6)"
