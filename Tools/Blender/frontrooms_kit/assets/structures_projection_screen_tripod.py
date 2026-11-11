"""Kit_ProjectionScreen_Tripod (structures_assets G3-a2 2026-11-10, P16 the SP3 training room; geometry in _struct_l0.projection_screen_tripod). 1.90 x 0.70 x 2.36; surface 70 in square facing +Z; collider legs to 1.2 m."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_ProjectionScreen_Tripod'
SMOOTH_ANGLE = 40.0
BUDGET = (2200, 800, 120)
VARIANTS = {'Kit_ProjectionScreen_TripodLit': {'Prop_ProjectionLit_Off': 'Prop_ProjectionLit'}}
LEVEL = lambda kit, lv: L.projection_screen_tripod(kit, lv)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (5.0, 15.0, 60.0), BUDGET, tags=('structures', 'p16', 'training_room', 'screen'))
    L.meta(kit)
