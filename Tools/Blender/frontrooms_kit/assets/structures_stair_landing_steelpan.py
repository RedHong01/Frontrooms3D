"""Kit_StairLanding_SteelPan: the pan landing 2.66 x 1.57: fill, pan, C10 header nosing, C6 joists, wall angle (stage STAIR author 2026-11-10, package stairwell_walkable). Built in _struct_stair.landing."""

import _run_b_common as B
import _struct_common as S
import _struct_stair as T

NAME = "Kit_StairLanding_SteelPan"
SMOOTH_ANGLE = 40.0
BUDGET = (1800, 600, 100)


def build(kit):
    B.levels(kit, T.landing)
    T.landing_meta(kit)
    S.finish(kit, (8.0, 18.0, 0.0), BUDGET, tags=("structures", "stair", "stairwell_walkable"))
    kit.meta["structures"] = "stairwell_walkable STAIR author 2026-11-10 (research/stairwell/80 section 6)"
