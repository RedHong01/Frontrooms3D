"""Kit_TapeRack_Gaps (structures_assets G3-a1 2026-11-10, P16 the SP3 computer room; geometry in _struct_l0_computer). The abandoned state: a quarter of the reels pulled."""

import _run_b_common as B
import _struct_common as S
import _struct_l0_computer as C

NAME = "Kit_TapeRack_Gaps"
SMOOTH_ANGLE = 40.0
BUDGET = (4200, 1200, 300)
VARIANTS = {}
LEVEL = lambda kit, lv: C.tape_rack(kit, lv, gaps=True)


def build(kit):
    B.levels(kit, LEVEL)
    C.collider_unity(kit, (0.0, 0.99, 0.0), (1.07, 1.98, 0.38))
    kit.min_ceiling = 0.0
    S.finish(kit, (5.0, 15.0, 60.0), BUDGET, tags=("structures", "p16", "computer_room", "wall_unit"))
    C.meta(kit, service=0.9)
