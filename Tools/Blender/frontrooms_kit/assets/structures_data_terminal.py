"""Kit_DataTerminal (structures_assets G3-a1 2026-11-10, P16 the SP3 computer room; geometry in _struct_l0_computer). VT320-class character terminal (S4), screen dark. Origin the desk point under its foot, front +Z; no collider (desk top); the keyboard is Kit_Keyboard at anchor 'keyboard'."""

import _run_b_common as B
import _struct_common as S
import _struct_l0_computer as C

NAME = "Kit_DataTerminal"
SMOOTH_ANGLE = 40.0
BUDGET = (2000, 700, 160)
VARIANTS = {}
LEVEL = C.data_terminal


def build(kit):
    B.levels(kit, LEVEL)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, (3.0, 9.0, 35.0), BUDGET, tags=("structures", "p16", "computer_room", "desk_top"))
    C.meta(kit)
