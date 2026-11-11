"""Kit_Lectern_Lit (structures_assets G3-a2 2026-11-10, P16 the SP3 training room; geometry in _struct_l0.lectern). Lamp on (Prop_BulbLit), notes on the top."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_Lectern_Lit'
SMOOTH_ANGLE = 40.0
BUDGET = (2100, 820, 120)
VARIANTS = {}
LEVEL = lambda kit, lv: L.lectern(kit, lv, lit=True)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (4.0, 12.0, 45.0), BUDGET, tags=('structures', 'p16', 'training_room', 'lectern', 'lit'))
    L.meta(kit)
