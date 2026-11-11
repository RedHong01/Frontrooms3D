"""Kit_TapeCart (structures_assets G3-a1 2026-11-10, P16 the SP3 computer room; geometry in _struct_l0_computer). Mobile tape cart (62 3.1). Origin the floor centre; collider the body."""

import _run_b_common as B
import _struct_common as S
import _struct_l0_computer as C

NAME = "Kit_TapeCart"
SMOOTH_ANGLE = 40.0
BUDGET = (3600, 1100, 260)
VARIANTS = {}
LEVEL = C.tape_cart


def build(kit):
    B.levels(kit, LEVEL)
    C.collider_unity(kit, (0.0, 0.60, 0.0), (0.91, 1.20, 0.51))
    kit.min_ceiling = 0.0
    S.finish(kit, (4.0, 12.0, 45.0), BUDGET, tags=("structures", "p16", "computer_room"))
    C.meta(kit)
