"""Kit_OverheadProjector_Lit (structures_assets G3-a2 2026-11-10, P16 the SP3 training room; geometry in _struct_l0.overhead_projector). The stage lit with the transparency (Prop_ProjectionLit)."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_OverheadProjector_Lit'
SMOOTH_ANGLE = 40.0
BUDGET = (2600, 1000, 150)
VARIANTS = {}
LEVEL = lambda kit, lv: L.overhead_projector(kit, lv, lit=True)


def build(kit):
    B.levels(kit, LEVEL)
    kit.min_ceiling = 0.0
    S.finish(kit, (3.0, 9.0, 35.0), BUDGET, tags=('structures', 'p16', 'training_room', 'desk_top', 'lit'))
    L.meta(kit)
