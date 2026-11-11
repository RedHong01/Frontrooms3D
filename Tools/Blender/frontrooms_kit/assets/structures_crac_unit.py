"""Kit_CRACUnit (structures_assets G3-a1 2026-11-10, P16 the SP3 computer room; geometry in _struct_l0_computer). Downflow computer-room air unit (S5, 62/LS84 1.83 x 0.86 x 1.90). Origin the floor centre, back to the wall, front +Z; collider the body; 0.9 m service zone."""

import _run_b_common as B
import _struct_common as S
import _struct_l0_computer as C

NAME = "Kit_CRACUnit"
SMOOTH_ANGLE = 40.0
BUDGET = (4200, 1400, 300)
VARIANTS = {NAME + "_Off": {"Prop_CompIndicator": "Prop_CompIndicator_Off"}}
LEVEL = C.crac_unit


def build(kit):
    B.levels(kit, LEVEL)
    C.collider_unity(kit, (0.0, C.CR_H / 2, 0.0), (C.CR_W, C.CR_H, C.CR_D))
    kit.min_ceiling = 0.0
    S.finish(kit, (7.0, 20.0, 80.0), BUDGET, tags=("structures", "p16", "computer_room", "wall_unit"))
    C.meta(kit, service=0.9)
