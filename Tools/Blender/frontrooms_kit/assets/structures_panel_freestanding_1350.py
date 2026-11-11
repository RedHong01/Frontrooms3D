"""Kit_PanelFreestanding_1350 (structures_assets G3-b 2026-11-10, P17 structures_office_props; geometry in _struct_office). a freestanding 60 in screen at 53 in: the family panel with moulded cap ends on two T-base stabiliser feet (0.470 x 0.051 x 0.048, toe glides, clamp brackets); collider the panel body."""

import _run_b_common as B
import _struct_common as S
import _struct_office as O

NAME = 'Kit_PanelFreestanding_1350'
SMOOTH_ANGLE = 40.0
BUDGET = (2400, 900, 200)
VARIANTS = {}
LEVEL = lambda kit, lv: O.freestanding(kit, lv, O.AO_53)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (6.0, 18.0, 70.0), BUDGET, tags=('structures', 'p17', 'office', 'freestanding'))
    O.meta(kit, 'freestanding')
