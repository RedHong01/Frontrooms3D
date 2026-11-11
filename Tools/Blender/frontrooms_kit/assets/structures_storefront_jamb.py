"""Kit_StorefrontJamb (structures_assets G3-b 2026-11-10, P17 structures_office_props; geometry in _struct_office). the storefront's wall jamb: one pocket (+x) with its gaskets, the back on the reveal with a sealant fillet each face; 1.721 m; no collider."""

import _run_b_common as B
import _struct_common as S
import _struct_office as O

NAME = 'Kit_StorefrontJamb'
SMOOTH_ANGLE = 40.0
BUDGET = (900, 200, 30)
VARIANTS = {}
LEVEL = lambda kit, lv: O.storefront_jamb(kit, lv)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (5.0, 15.0, 60.0), BUDGET, tags=('structures', 'p17', 'office', 'storefront'))
    O.meta(kit, 'storefront')
