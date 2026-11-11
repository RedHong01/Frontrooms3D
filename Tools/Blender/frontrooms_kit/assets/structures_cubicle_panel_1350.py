"""Kit_CubiclePanel_1350 (structures_assets G3-b 2026-11-10, P17 structures_office_props; geometry in _struct_office). the family's 60 in panel at Action Office / Series 9000's 53 in (1.346 m): fabric both faces, putty cap rail (HERO at 0.35 m), end trims with slotted standards, raceway base on glides; 1.524 x 0.064; origin the floor centre, length on x, faces +-Z; collider the body."""

import _run_b_common as B
import _struct_common as S
import _struct_office as O

NAME = 'Kit_CubiclePanel_1350'
SMOOTH_ANGLE = 40.0
BUDGET = (1700, 700, 120)
VARIANTS = {}
LEVEL = lambda kit, lv: O.panel(kit, lv, O.W_60, O.AO_53)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (6.0, 18.0, 70.0), BUDGET, tags=('structures', 'p17', 'office', 'panel'))
    O.meta(kit, 'panel')
