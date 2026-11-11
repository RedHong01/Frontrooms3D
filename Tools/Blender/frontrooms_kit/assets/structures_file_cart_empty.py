"""Kit_FileCart_Empty (structures_assets G3-a2 2026-11-10, P16 the SP3 file mezzanine; geometry in _struct_l0.file_cart). Empty basket; collider the body."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_FileCart_Empty'
SMOOTH_ANGLE = 40.0
BUDGET = (2200, 900, 140)
VARIANTS = {}
LEVEL = lambda kit, lv: L.file_cart(kit, lv, full=False)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (4.0, 12.0, 45.0), BUDGET, tags=('structures', 'p16', 'file_mezzanine', 'cart'))
    L.meta(kit)
