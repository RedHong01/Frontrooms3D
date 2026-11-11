"""Kit_RulesBoard: the blank rules board in its aluminium frame on four stand-offs (structures G2-b 2026-11-08, package structures_slide_start). Built in _struct_slide.rules_board."""

import _run_b_common as B
import _struct_common as S
import _struct_slide as SL

NAME = "Kit_RulesBoard"
SMOOTH_ANGLE = 40.0
BUDGET = (700, 300, 60)


def build(kit):
    B.levels(kit, SL.rules_board)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, SL.DIST_SMALL, BUDGET, tags=("structures", "slide", "p15"))
