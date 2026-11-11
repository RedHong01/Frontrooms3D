"""Kit_FloorDrain: a deck drain: a 5 in chrome strainer with radial slots (structures G2-c 2026-11-10, package structures_pool_fittings). Built in _struct_pool.floor_drain."""

import _run_b_common as B
import _struct_common as S
import _struct_pool as P

NAME = "Kit_FloorDrain"
SMOOTH_ANGLE = 40.0
BUDGET = (420, 150, 40)


def build(kit):
    B.levels(kit, P.floor_drain)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, P.DIST_SMALL, BUDGET, tags=("structures", "pool", "p11"))
