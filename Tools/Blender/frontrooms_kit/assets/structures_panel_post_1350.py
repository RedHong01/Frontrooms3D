"""Kit_PanelPost_1350 (structures_assets G3-b 2026-11-10, P17 structures_office_props; geometry in _struct_office). the family's 64 mm connector post at 53 in, its end cap flush with the 1.346 m caps; no collider."""

import _run_b_common as B
import _struct_common as S
import _struct_office as O

NAME = 'Kit_PanelPost_1350'
SMOOTH_ANGLE = 40.0
BUDGET = (500, 200, 40)
VARIANTS = {}
LEVEL = lambda kit, lv: O.post(kit, lv, O.AO_53)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (4.0, 12.0, 45.0), BUDGET, tags=('structures', 'p17', 'office', 'post'))
    O.meta(kit, 'post')
