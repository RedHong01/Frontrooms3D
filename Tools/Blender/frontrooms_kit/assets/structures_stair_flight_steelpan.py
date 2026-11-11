"""Kit_StairFlight_SteelPan: the steel pan flight: 10 risers x 175, 9 treads x 280, 1.12 m, C10 stringers, filled pans (stage STAIR author 2026-11-10, package stairwell_walkable). Built in _struct_stair.flight."""

import _run_b_common as B
import _struct_common as S
import _struct_stair as T

NAME = "Kit_StairFlight_SteelPan"
SMOOTH_ANGLE = 40.0
BUDGET = (7000, 2200, 400)


def build(kit):
    B.levels(kit, T.flight)
    T.flight_meta(kit)
    S.finish(kit, (8.0, 18.0, 0.0), BUDGET, tags=("structures", "stair", "stairwell_walkable"))
    kit.meta["structures"] = "stairwell_walkable STAIR author 2026-11-10 (research/stairwell/80 section 6)"
