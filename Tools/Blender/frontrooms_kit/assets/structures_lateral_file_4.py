"""Kit_LateralFile_4 (structures_assets G3-a2 2026-11-10, P16 the SP3 file mezzanine; geometry in _struct_l0.lateral_file). 4 drawers, 0.914 x 0.457; origin the floor centre, front +Z; collider the body."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_LateralFile_4'
SMOOTH_ANGLE = 40.0
BUDGET = (1800, 700, 100)
VARIANTS = {}
LEVEL = lambda kit, lv: L.lateral_file(kit, lv, drawers=4)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (4.0, 12.0, 45.0), BUDGET, tags=('structures', 'p16', 'file_mezzanine', 'cabinet'))
    L.meta(kit, service=0.7)
