"""Kit_RecordsCarton (structures_assets G3-a2 2026-11-10, P16 the SP3 file mezzanine; geometry in _struct_l0.records_carton). 0.31 x 0.27 x 0.39 (front +Z = the printed end); no collider (clutter)."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_RecordsCarton'
SMOOTH_ANGLE = 40.0
BUDGET = (400, 140, 30)
VARIANTS = {}
LEVEL = lambda kit, lv: L.records_carton(kit, lv, mode='single')


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (3.0, 9.0, 35.0), BUDGET, tags=('structures', 'p16', 'file_mezzanine', 'clutter', 'carton'))
    L.meta(kit)
