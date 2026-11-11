"""Kit_CubiclePanelShort_1350 (structures_assets G3-b 2026-11-10, P17 structures_office_props; geometry in _struct_office). the family's 30 in panel at 53 in (1.346 m), the T-cap of a bay arm; 0.762 x 0.064; collider the body."""

import _run_b_common as B
import _struct_common as S
import _struct_office as O

NAME = 'Kit_CubiclePanelShort_1350'
SMOOTH_ANGLE = 40.0
BUDGET = (1300, 600, 100)
VARIANTS = {}
LEVEL = lambda kit, lv: O.panel(kit, lv, O.W_30, O.AO_53)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (5.0, 15.0, 60.0), BUDGET, tags=('structures', 'p17', 'office', 'panel'))
    O.meta(kit, 'panel')
