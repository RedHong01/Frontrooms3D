"""Kit_PanelFreestanding_2160 (structures_assets G3-b 2026-11-10, P17 structures_office_props; geometry in _struct_office). a freestanding 60 in screen at 85 in (2.159 m); collider the panel body."""

import _run_b_common as B
import _struct_common as S
import _struct_office as O

NAME = 'Kit_PanelFreestanding_2160'
SMOOTH_ANGLE = 40.0
BUDGET = (2700, 950, 200)
VARIANTS = {}
LEVEL = lambda kit, lv: O.freestanding(kit, lv, O.AO_85)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (6.0, 18.0, 70.0), BUDGET, tags=('structures', 'p17', 'office', 'freestanding'))
    O.meta(kit, 'freestanding')
