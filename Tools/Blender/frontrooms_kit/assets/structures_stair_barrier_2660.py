"""Kit_StairBarrier_2660: the ADAAG 4.4.2 headroom barrier under flight 1 (stage STAIR author 2026-11-10, package stairwell_walkable). Built in _struct_stair.barrier."""

import _run_b_common as B
import _struct_common as S
import _struct_stair as T

NAME = "Kit_StairBarrier_2660"
SMOOTH_ANGLE = 40.0
BUDGET = (900, 300, 60)


def build(kit):
    B.levels(kit, T.barrier)
    T.small_meta(kit)
    S.finish(kit, (4.0, 10.0, 0.0), BUDGET, tags=("structures", "stair", "stairwell_walkable"))
    kit.meta["structures"] = "stairwell_walkable STAIR author 2026-11-10 (research/stairwell/80 section 6)"
