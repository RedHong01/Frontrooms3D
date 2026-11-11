"""Kit_SlideChain: the chain across the tub's entry: hook plates and 6 mm oval links (structures G2-b 2026-11-08, package structures_slide_start). Built in _struct_slide.chain."""

import _run_b_common as B
import _struct_common as S
import _struct_slide as SL

NAME = "Kit_SlideChain"
SMOOTH_ANGLE = 40.0
BUDGET = (4200, 1800, 220)


def build(kit):
    B.levels(kit, SL.chain)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, SL.DIST_SMALL, BUDGET, tags=("structures", "slide", "p15"))
