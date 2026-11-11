"""Kit_DotMatrixPrinter_Desk (structures_assets G3-a1 2026-11-10, P16 the SP3 computer room; geometry in _struct_l0_computer). The printer alone on a desk with a short fanfold stack behind. Origin the desk point, front +Z; no collider."""

import _run_b_common as B
import _struct_common as S
import _struct_l0_computer as C

NAME = "Kit_DotMatrixPrinter_Desk"
SMOOTH_ANGLE = 40.0
BUDGET = (2200, 800, 180)
VARIANTS = {}
LEVEL = lambda kit, lv: C.dot_matrix(kit, lv, on_stand=False)


def build(kit):
    B.levels(kit, LEVEL)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, (3.0, 9.0, 35.0), BUDGET, tags=("structures", "p16", "computer_room", "desk_top"))
    C.meta(kit)
