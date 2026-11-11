"""Kit_SlideCrossbar: the tub's stainless crossbar in two escutcheons (structures G2-b 2026-11-08, package structures_slide_start). Built in _struct_slide.crossbar."""

import _run_b_common as B
import _struct_common as S
import _struct_slide as SL

NAME = "Kit_SlideCrossbar"
SMOOTH_ANGLE = 40.0
BUDGET = (1400, 520, 120)


def build(kit):
    B.levels(kit, SL.crossbar)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, SL.DIST_SMALL, BUDGET, tags=("structures", "slide", "p15"))
