"""Kit_StairGuard_Landing_420: the 42 in landing guard, across the well at the turn landing, between the two flight guards' posts (stage STAIR author 2026-11-10, package stairwell_walkable). Built in _struct_stair.guard_landing."""

import _run_b_common as B
import _struct_common as S
import _struct_stair as T

NAME = "Kit_StairGuard_Landing_420"
SMOOTH_ANGLE = 40.0
BUDGET = (1600, 500, 60)


def build(kit):
    B.levels(kit, lambda k, lv: T.guard_landing(k, lv, 420 / 1000.0, False, False))
    T.small_meta(kit)
    S.finish(kit, (5.0, 12.0, 0.0), BUDGET, tags=("structures", "stair", "stairwell_walkable"))
    kit.meta["structures"] = "stairwell_walkable STAIR author 2026-11-10 (research/stairwell/80 section 6)"
