"""Kit_StairGuard_Landing_1540: the 42 in landing guard, the exit landing's open edge (wall flange at -X, one middle post) (stage STAIR author 2026-11-10, package stairwell_walkable). Built in _struct_stair.guard_landing."""

import _run_b_common as B
import _struct_common as S
import _struct_stair as T

NAME = "Kit_StairGuard_Landing_1540"
SMOOTH_ANGLE = 40.0
BUDGET = (1600, 500, 60)


def build(kit):
    B.levels(kit, lambda k, lv: T.guard_landing(k, lv, 1540 / 1000.0, True, True))
    T.small_meta(kit)
    S.finish(kit, (5.0, 12.0, 0.0), BUDGET, tags=("structures", "stair", "stairwell_walkable"))
    kit.meta["structures"] = "stairwell_walkable STAIR author 2026-11-10 (research/stairwell/80 section 6)"
