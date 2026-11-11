"""Kit_StairGuard_Flight: the well-side picket guard of one flight, rail at 0.91 m, newel posts (stage STAIR author 2026-11-10, package stairwell_walkable). Built in _struct_stair.guard_flight."""

import _run_b_common as B
import _struct_common as S
import _struct_stair as T

NAME = "Kit_StairGuard_Flight"
SMOOTH_ANGLE = 40.0
BUDGET = (4000, 1200, 150)


def build(kit):
    B.levels(kit, T.guard_flight)
    T.guard_flight_meta(kit)
    S.finish(kit, (5.0, 12.0, 0.0), BUDGET, tags=("structures", "stair", "stairwell_walkable"))
    kit.meta["structures"] = "stairwell_walkable STAIR author 2026-11-10 (research/stairwell/80 section 6)"
