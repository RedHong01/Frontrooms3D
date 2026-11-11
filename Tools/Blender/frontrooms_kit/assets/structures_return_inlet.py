"""Kit_ReturnInlet: a return inlet: the chrome-plated bronze eyeball fitting on the wall (structures G2-c 2026-11-10, package structures_pool_fittings). Built in _struct_pool.return_inlet."""

import _run_b_common as B
import _struct_common as S
import _struct_pool as P

NAME = "Kit_ReturnInlet"
SMOOTH_ANGLE = 40.0
BUDGET = (420, 160, 60)


def build(kit):
    B.levels(kit, P.return_inlet)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, P.DIST_SMALL, BUDGET, tags=("structures", "pool", "p11"))
