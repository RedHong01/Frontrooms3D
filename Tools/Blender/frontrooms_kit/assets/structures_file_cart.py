"""Kit_FileCart (structures_assets G3-a2 2026-11-10, P16 the SP3 file mezzanine; geometry in _struct_l0.file_cart). 0.81 x 0.41 x 0.91, handle at -X; collider the body."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_FileCart'
SMOOTH_ANGLE = 40.0
BUDGET = (2600, 1000, 150)
VARIANTS = {}
LEVEL = lambda kit, lv: L.file_cart(kit, lv, full=True)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (4.0, 12.0, 45.0), BUDGET, tags=('structures', 'p16', 'file_mezzanine', 'cart'))
    L.meta(kit)
