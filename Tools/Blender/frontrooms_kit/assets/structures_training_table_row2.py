"""Kit_TrainingTable_Row2 (structures_assets G3-a2 2026-11-10, P16 the SP3 training room; geometry in _struct_l0.table_row). Two tables ganged, 3.05 x 0.457; collider the row, support 'top'."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_TrainingTable_Row2'
SMOOTH_ANGLE = 40.0
BUDGET = (2300, 900, 120)
VARIANTS = {'Kit_TrainingTable_Row2_Dove': {'Prop_LaminateFawn': 'Prop_LaminateDove'}}
LEVEL = lambda kit, lv: L.table_row(kit, lv)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (5.0, 15.0, 55.0), BUDGET, tags=('structures', 'p16', 'training_room', 'table'))
    L.meta(kit)
