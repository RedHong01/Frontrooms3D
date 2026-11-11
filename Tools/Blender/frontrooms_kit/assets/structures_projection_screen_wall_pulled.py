"""Kit_ProjectionScreen_WallPulled (structures_assets G3-a2 2026-11-10, P16 the SP3 training room; geometry in _struct_l0.projection_screen_wall). 70 x 70 in; origin the wall point at the brackets' top, the case 65 mm out; pulled; no collider."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_ProjectionScreen_WallPulled'
SMOOTH_ANGLE = 40.0
BUDGET = (1300, 500, 60)
VARIANTS = {}
LEVEL = lambda kit, lv: L.projection_screen_wall(kit, lv, mode='pulled')


def build(kit):
    B.levels(kit, LEVEL)
    kit.min_ceiling = 0.0
    S.finish(kit, (5.0, 15.0, 60.0), BUDGET, tags=('structures', 'p16', 'training_room', 'wall_decor'))
    L.meta(kit)
