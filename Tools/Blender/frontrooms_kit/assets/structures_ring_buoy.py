"""Kit_RingBuoy: a 20 in ring life buoy, orange, its white grab line in four bights on four beckets, on a stainless wall bracket (structures G2-d 2026-11-10, package structures_pool_finish). Built in _struct_pool_finish.ring_buoy."""

import _run_b_common as B
import _struct_common as S
import _struct_pool_finish as F

NAME = "Kit_RingBuoy"
SMOOTH_ANGLE = 40.0
BUDGET = (7000, 1700, 650)


def build(kit):
    B.levels(kit, F.ring_buoy)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, F.DIST_SMALL, BUDGET, tags=("structures", "pool", "p15"))
