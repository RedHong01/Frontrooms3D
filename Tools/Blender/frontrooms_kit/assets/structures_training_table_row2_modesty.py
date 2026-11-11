"""Kit_TrainingTable_Row2_Modesty (structures_assets G3-a2 2026-11-10, P16 the SP3 training room; geometry in _struct_l0.table_row). Modesty panels on the presenter's side (-Z)."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_TrainingTable_Row2_Modesty'
SMOOTH_ANGLE = 40.0
BUDGET = (2500, 950, 130)
VARIANTS = {'Kit_TrainingTable_Row2_Modesty_Dove': {'Prop_LaminateFawn': 'Prop_LaminateDove'}}
LEVEL = lambda kit, lv: L.table_row(kit, lv, modesty=True)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (5.0, 15.0, 55.0), BUDGET, tags=('structures', 'p16', 'training_room', 'table'))
    L.meta(kit)
