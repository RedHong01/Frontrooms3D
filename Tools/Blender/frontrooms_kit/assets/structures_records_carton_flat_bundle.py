"""Kit_RecordsCarton_FlatBundle (structures_assets G3-a2 2026-11-10, P16 the SP3 file mezzanine; geometry in _struct_l0.records_carton). Flattened cartons strapped, 0.60 x 0.08 x 0.90; no collider."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_RecordsCarton_FlatBundle'
SMOOTH_ANGLE = 40.0
BUDGET = (200, 90, 24)
VARIANTS = {}
LEVEL = lambda kit, lv: L.records_carton(kit, lv, mode='bundle')


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (3.0, 9.0, 35.0), BUDGET, tags=('structures', 'p16', 'file_mezzanine', 'clutter', 'carton'))
    L.meta(kit)
