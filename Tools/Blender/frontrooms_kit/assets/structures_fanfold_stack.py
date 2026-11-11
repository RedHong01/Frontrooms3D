"""Kit_FanfoldBox_Stack (structures_assets G3-a1 2026-11-10, P16 the SP3 computer room; geometry in _struct_l0_computer). A printout stack set down (floor or desk). No collider."""

import _run_b_common as B
import _struct_common as S
import _struct_l0_computer as C

NAME = "Kit_FanfoldBox_Stack"
SMOOTH_ANGLE = 40.0
BUDGET = (300, 100, 30)
VARIANTS = {}
LEVEL = C.fanfold_stack


def build(kit):
    B.levels(kit, LEVEL)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, (2.5, 7.0, 25.0), BUDGET, tags=("structures", "p16", "computer_room", "clutter", "desk_top"))
    C.meta(kit)
