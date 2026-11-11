"""Kit_FanfoldBox (structures_assets G3-a1 2026-11-10, P16 the SP3 computer room; geometry in _struct_l0_computer). An open carton of continuous forms with a printout on top (LS98). Origin the floor centre; no collider."""

import _run_b_common as B
import _struct_common as S
import _struct_l0_computer as C

NAME = "Kit_FanfoldBox"
SMOOTH_ANGLE = 40.0
BUDGET = (900, 300, 60)
VARIANTS = {}
LEVEL = lambda kit, lv: C.fanfold_carton(kit, lv)


def build(kit):
    B.levels(kit, LEVEL)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, (3.0, 9.0, 30.0), BUDGET, tags=("structures", "p16", "computer_room", "clutter"))
    C.meta(kit)
