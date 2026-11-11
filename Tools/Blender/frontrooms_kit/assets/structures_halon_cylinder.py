"""Kit_HalonCylinder (structures_assets G3-a1 2026-11-10, P16 the SP3 computer room; geometry in _struct_l0_computer). Halon 1301 container on its wall straps (S6). Origin the floor centre, the wall at z -0.20; collider the cylinder."""

import _run_b_common as B
import _struct_common as S
import _struct_l0_computer as C

NAME = "Kit_HalonCylinder"
SMOOTH_ANGLE = 40.0
BUDGET = (2600, 900, 200)
VARIANTS = {}
LEVEL = lambda kit, lv: C.halon_cylinder(kit, lv, pair=False)


def build(kit):
    B.levels(kit, LEVEL)
    C.collider_unity(kit, (0.0, 0.50, 0.0), (0.32, 1.0, 0.32))
    kit.min_ceiling = 0.0
    S.finish(kit, (4.0, 12.0, 45.0), BUDGET, tags=("structures", "p16", "computer_room", "wall_unit"))
    C.meta(kit)
