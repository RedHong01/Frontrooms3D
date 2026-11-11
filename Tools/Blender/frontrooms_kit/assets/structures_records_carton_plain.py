"""Kit_RecordsCarton_Plain (structures_assets G3-a2 2026-11-10, P16 the SP3 file mezzanine; geometry in _struct_l0.records_carton). A plain 18 in moving box, taped; no collider."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_RecordsCarton_Plain'
SMOOTH_ANGLE = 40.0
BUDGET = (120, 60, 20)
VARIANTS = {}
LEVEL = lambda kit, lv: L.records_carton(kit, lv, mode='plain')


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (3.0, 9.0, 35.0), BUDGET, tags=('structures', 'p16', 'file_mezzanine', 'clutter', 'carton'))
    L.meta(kit)
