"""Kit_StackChair (structures_assets G3-a2 2026-11-10, P16 the SP3 training room; geometry in _struct_l0.stack_chair_kit). 0.53 x 0.81, seat 18 in; origin the floor centre, the sitter faces +Z; collider the body."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_StackChair'
SMOOTH_ANGLE = 40.0
BUDGET = (2400, 900, 160)
VARIANTS = {'Kit_StackChair_Charcoal': {'Prop_FabricNavy': 'Prop_FabricCharcoal'}, 'Kit_StackChair_Teal': {'Prop_FabricNavy': 'Prop_FabricTeal'}}
LEVEL = lambda kit, lv: L.stack_chair_kit(kit, lv)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (3.5, 10.0, 40.0), BUDGET, tags=('structures', 'p16', 'training_room', 'chair'))
    L.meta(kit)
