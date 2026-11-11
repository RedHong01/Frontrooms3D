"""Kit_PanelFreestanding_1650 (structures_assets G3-b 2026-11-10, P17 structures_office_props; geometry in _struct_office). a freestanding 60 in screen at 65 in (1.651 m, Series 9000's 65 in, the family's tall height); collider the panel body."""

import _run_b_common as B
import _struct_common as S
import _struct_office as O

NAME = 'Kit_PanelFreestanding_1650'
SMOOTH_ANGLE = 40.0
BUDGET = (2500, 900, 200)
VARIANTS = {}
LEVEL = lambda kit, lv: O.freestanding(kit, lv, 1.651)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (6.0, 18.0, 70.0), BUDGET, tags=('structures', 'p17', 'office', 'freestanding'))
    O.meta(kit, 'freestanding')
