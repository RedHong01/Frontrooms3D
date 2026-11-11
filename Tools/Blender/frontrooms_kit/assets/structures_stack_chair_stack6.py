"""Kit_StackChair_Stack6 (structures_assets G3-a2 2026-11-10, P16 the SP3 training room; geometry in _struct_l0.chair_stack). Six nested ('piled' / 'cleared'); collider the stack."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_StackChair_Stack6'
SMOOTH_ANGLE = 40.0
BUDGET = (6000, 2400, 400)
VARIANTS = {'Kit_StackChair_Stack6_Charcoal': {'Prop_FabricNavy': 'Prop_FabricCharcoal'}, 'Kit_StackChair_Stack6_Teal': {'Prop_FabricNavy': 'Prop_FabricTeal'}}
LEVEL = lambda kit, lv: L.chair_stack(kit, lv, 6)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (5.0, 15.0, 55.0), BUDGET, tags=('structures', 'p16', 'training_room', 'chair'))
    L.meta(kit)
