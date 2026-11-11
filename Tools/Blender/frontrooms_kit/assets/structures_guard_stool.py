"""Kit_GuardStool: the top guard's stool: a vinyl seat on four splayed tube legs, a foot ring (structures G2-b 2026-11-08, package structures_slide_start). Built in _struct_slide.stool."""

import _run_b_common as B
import _struct_common as S
import _struct_slide as SL

NAME = "Kit_GuardStool"
SMOOTH_ANGLE = 40.0
BUDGET = (2600, 1100, 300)


def build(kit):
    B.levels(kit, SL.stool)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, SL.DIST_SMALL, BUDGET, tags=("structures", "slide", "p15"))
