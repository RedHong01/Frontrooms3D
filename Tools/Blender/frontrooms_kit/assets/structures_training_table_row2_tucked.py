"""Kit_TrainingTable_Row2_Tucked (structures_assets G3-a2 2026-11-10, P16 the SP3 training room; geometry in _struct_l0.table_row_tucked). Four stack chairs pushed in against the edge ('cleared'); colliders row + chairs."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_TrainingTable_Row2_Tucked'
SMOOTH_ANGLE = 40.0
BUDGET = (5500, 2000, 300)
VARIANTS = {'Kit_TrainingTable_Row2_Tucked_Charcoal': {'Prop_FabricNavy': 'Prop_FabricCharcoal'}, 'Kit_TrainingTable_Row2_Tucked_Teal': {'Prop_FabricNavy': 'Prop_FabricTeal'}}
LEVEL = lambda kit, lv: L.table_row_tucked(kit, lv)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (5.0, 15.0, 55.0), BUDGET, tags=('structures', 'p16', 'training_room', 'table'))
    L.meta(kit)
