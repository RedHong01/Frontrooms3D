"""Kit_Lectern (structures_assets G3-a2 2026-11-10, P16 the SP3 training room; geometry in _struct_l0.lectern). 0.66 x 0.51 x 1.19 (top); the speaker at +Z; lamp off; collider the body."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_Lectern'
SMOOTH_ANGLE = 40.0
BUDGET = (2000, 800, 120)
VARIANTS = {'Kit_Lectern_Oak': {'Prop_WoodWalnut': 'Prop_WoodOak'}}
LEVEL = lambda kit, lv: L.lectern(kit, lv)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (4.0, 12.0, 45.0), BUDGET, tags=('structures', 'p16', 'training_room', 'lectern'))
    L.meta(kit)
