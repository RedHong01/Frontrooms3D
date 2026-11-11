"""Kit_MarkerBoard_Chalk (structures_assets G3-a2 2026-11-10, P16 the SP3 training room; geometry in _struct_l0.marker_board). 8 x 4 ft, sill 0.90; origin the wall point at floor level; chalk; no collider."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_MarkerBoard_Chalk'
SMOOTH_ANGLE = 40.0
BUDGET = (700, 300, 40)
VARIANTS = {}
LEVEL = lambda kit, lv: L.marker_board(kit, lv, face='chalk')


def build(kit):
    B.levels(kit, LEVEL)
    kit.min_ceiling = 0.0
    S.finish(kit, (5.0, 15.0, 60.0), BUDGET, tags=('structures', 'p16', 'training_room', 'wall_decor'))
    L.meta(kit)
