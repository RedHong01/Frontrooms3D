"""Kit_AVCart_Low (structures_assets G3-a2 2026-11-10, P16 the SP3 training room; geometry in _struct_l0.av_cart). 24 x 18 x 26 in; anchor 'top' (the projector); collider the body."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_AVCart_Low'
SMOOTH_ANGLE = 40.0
BUDGET = (2200, 800, 120)
VARIANTS = {}
LEVEL = lambda kit, lv: L.av_cart(kit, lv)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (4.0, 12.0, 45.0), BUDGET, tags=('structures', 'p16', 'training_room', 'cart'))
    L.meta(kit)
