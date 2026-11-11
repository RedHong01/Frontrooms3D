"""Kit_DotMatrixPrinter_OnStand (structures_assets G3-a1 2026-11-10, P16 the SP3 computer room; geometry in _struct_l0_computer). Wide-carriage dot-matrix printer on its stand with the forms carton below (62 3.1; S8). Origin the floor centre, front +Z; collider the stand only."""

import _run_b_common as B
import _struct_common as S
import _struct_l0_computer as C

NAME = "Kit_DotMatrixPrinter_OnStand"
SMOOTH_ANGLE = 40.0
BUDGET = (3800, 1300, 280)
VARIANTS = {}
LEVEL = lambda kit, lv: C.dot_matrix(kit, lv, on_stand=True)


def build(kit):
    B.levels(kit, LEVEL)
    C.collider_unity(kit, (0.0, 0.34, 0.0), (0.70, 0.68, 0.50))
    kit.min_ceiling = 0.0
    S.finish(kit, (4.0, 12.0, 45.0), BUDGET, tags=("structures", "p16", "computer_room"))
    C.meta(kit)
