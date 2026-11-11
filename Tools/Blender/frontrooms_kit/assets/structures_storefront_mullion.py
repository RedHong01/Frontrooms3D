"""Kit_StorefrontMullion (structures_assets G3-b 2026-11-10, P17 structures_office_props; geometry in _struct_office). HERO: the intermediate mullion of a 1-3/4 x 4-1/2 in centre-set interior storefront (clear anodised), a glazing pocket each side with EPDM wedge gaskets on both glass faces; 1.721 m (between the sill and head channels of the boardroom's 1.81 m opening); origin the sill channel's top on the glass line; no collider."""

import _run_b_common as B
import _struct_common as S
import _struct_office as O

NAME = 'Kit_StorefrontMullion'
SMOOTH_ANGLE = 40.0
BUDGET = (1400, 300, 40)
VARIANTS = {}
LEVEL = lambda kit, lv: O.storefront_member(kit, lv, ('+x', '-x'))


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (5.0, 15.0, 60.0), BUDGET, tags=('structures', 'p17', 'office', 'storefront'))
    O.meta(kit, 'storefront')
