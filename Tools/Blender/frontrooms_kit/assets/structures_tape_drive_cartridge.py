"""Kit_TapeDrive_Cartridge (structures_assets G3-a1 2026-11-10, P16 the SP3 computer room; geometry in _struct_l0_computer). 3480-class cartridge unit in the tape unit's cabinet (LS86). Origin the floor centre, front +Z; collider the body."""

import _run_b_common as B
import _struct_common as S
import _struct_l0_computer as C

NAME = "Kit_TapeDrive_Cartridge"
SMOOTH_ANGLE = 40.0
BUDGET = (4600, 1600, 360)
VARIANTS = {NAME + "_Off": {"Prop_CompIndicator": "Prop_CompIndicator_Off"}}
LEVEL = C.tape_cartridge


def build(kit):
    B.levels(kit, LEVEL)
    C.collider_unity(kit, (0.0, C.TD_H / 2, 0.0), (C.TD_W, C.TD_H, C.TD_D))
    kit.min_ceiling = 0.0
    S.finish(kit, (5.0, 15.0, 60.0), BUDGET, tags=("structures", "p16", "computer_room", "wall_unit"))
    C.meta(kit, service=1.2)
