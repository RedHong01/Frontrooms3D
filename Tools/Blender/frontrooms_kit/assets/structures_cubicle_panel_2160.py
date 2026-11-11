"""Kit_CubiclePanel_2160 (structures_assets G3-b 2026-11-10, P17 structures_office_props; geometry in _struct_office). the family's 60 in panel at Action Office's 85 in (2.159 m), one panel as AO built it; HERO cap at 2.16; 1.524 x 0.064; collider the body."""

import _run_b_common as B
import _struct_common as S
import _struct_office as O

NAME = 'Kit_CubiclePanel_2160'
SMOOTH_ANGLE = 40.0
BUDGET = (1900, 750, 120)
VARIANTS = {}
LEVEL = lambda kit, lv: O.panel(kit, lv, O.W_60, O.AO_85)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (6.0, 18.0, 70.0), BUDGET, tags=('structures', 'p17', 'office', 'panel'))
    O.meta(kit, 'panel')
