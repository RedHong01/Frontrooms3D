"""Kit_TrainingTable (structures_assets G3-a2 2026-11-10, P16 the SP3 training room; geometry in _struct_l0.table_kit). 60 x 18 x 29 in; origin the floor centre, the seated side +Z; collider the body, support 'top'."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_TrainingTable'
SMOOTH_ANGLE = 40.0
BUDGET = (1100, 450, 60)
VARIANTS = {'Kit_TrainingTable_Dove': {'Prop_LaminateFawn': 'Prop_LaminateDove'}}
LEVEL = lambda kit, lv: L.table_kit(kit, lv)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (4.0, 12.0, 45.0), BUDGET, tags=('structures', 'p16', 'training_room', 'table'))
    L.meta(kit)
