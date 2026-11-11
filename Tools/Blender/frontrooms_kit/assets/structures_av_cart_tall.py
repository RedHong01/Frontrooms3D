"""Kit_AVCart_Tall (structures_assets G3-a2 2026-11-10, P16 the SP3 training room; geometry in _struct_l0.av_cart). 24 x 18 x 42 in, VHS deck on the middle shelf; anchors 'tv' / 'top' (Kit_CRTTV); collider the body."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_AVCart_Tall'
SMOOTH_ANGLE = 40.0
BUDGET = (3600, 1300, 160)
VARIANTS = {}
LEVEL = lambda kit, lv: L.av_cart(kit, lv, tall=True)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (4.0, 12.0, 45.0), BUDGET, tags=('structures', 'p16', 'training_room', 'cart'))
    L.meta(kit)
