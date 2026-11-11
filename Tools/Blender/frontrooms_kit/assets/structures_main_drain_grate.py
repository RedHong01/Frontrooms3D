"""Kit_MainDrainGrate: the main drain: a 12 in cast bronze frame and slotted grate over the dark sump (structures G2-c 2026-11-10, package structures_pool_fittings). Built in _struct_pool.main_drain."""

import _run_b_common as B
import _struct_common as S
import _struct_pool as P

NAME = "Kit_MainDrainGrate"
SMOOTH_ANGLE = 40.0
BUDGET = (400, 160, 40)


def build(kit):
    B.levels(kit, P.main_drain)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, P.DIST_SMALL, BUDGET, tags=("structures", "pool", "p11"))
