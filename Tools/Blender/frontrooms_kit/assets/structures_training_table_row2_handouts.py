"""Kit_TrainingTable_Row2_Handouts (structures_assets G3-a2 2026-11-10, P16 the SP3 training room; geometry in _struct_l0.table_row_handouts). Handouts, binders, name tents, pencils at four seats, a cup ('midTask')."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_TrainingTable_Row2_Handouts'
SMOOTH_ANGLE = 40.0
BUDGET = (3200, 1200, 150)
VARIANTS = {}
LEVEL = lambda kit, lv: L.table_row_handouts(kit, lv)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (5.0, 15.0, 55.0), BUDGET, tags=('structures', 'p16', 'training_room', 'table'))
    L.meta(kit)
