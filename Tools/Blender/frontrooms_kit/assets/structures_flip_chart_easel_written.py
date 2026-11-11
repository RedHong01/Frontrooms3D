"""Kit_FlipChartEasel_Written (structures_assets G3-a2 2026-11-10, P16 the SP3 training room; geometry in _struct_l0.flip_easel). 0.70 x 0.65 x 1.85, 27 x 34 in pad facing +Z; collider legs to 1.2 m."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_FlipChartEasel_Written'
SMOOTH_ANGLE = 40.0
BUDGET = (1600, 600, 100)
VARIANTS = {}
LEVEL = lambda kit, lv: L.flip_easel(kit, lv, face='written')


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (4.0, 12.0, 45.0), BUDGET, tags=('structures', 'p16', 'training_room', 'easel'))
    L.meta(kit)
