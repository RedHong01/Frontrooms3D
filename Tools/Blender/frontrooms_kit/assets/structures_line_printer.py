"""Kit_LinePrinter (structures_assets G3-a1 2026-11-10, P16 the SP3 computer room; geometry in _struct_l0_computer). Band printer in its acoustic cabinet (S3, Dataproducts B-600 class), warm tone. Origin the floor centre, front +Z; collider the body."""

import _run_b_common as B
import _struct_common as S
import _struct_l0_computer as C

NAME = "Kit_LinePrinter"
SMOOTH_ANGLE = 40.0
BUDGET = (4800, 1600, 360)
VARIANTS = {NAME + "_Off": {"Prop_CompIndicator": "Prop_CompIndicator_Off"}}
LEVEL = C.line_printer


def build(kit):
    B.levels(kit, LEVEL)
    C.collider_unity(kit, (0.0, C.LP_H / 2, 0.0), (C.LP_W, C.LP_H, C.LP_D))
    kit.min_ceiling = 0.0
    S.finish(kit, (5.0, 15.0, 60.0), BUDGET, tags=("structures", "p16", "computer_room"))
    C.meta(kit, service=1.0)
