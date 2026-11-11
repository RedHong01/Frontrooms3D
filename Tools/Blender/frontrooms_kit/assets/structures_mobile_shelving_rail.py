"""Kit_MobileShelving_Rail (structures_assets G3-a2 2026-11-10, P16 the SP3 file mezzanine; geometry in _struct_l0.mobile_rail). 3 m floor rail along Unity Z, 30 mm high; no collider (walkable)."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_MobileShelving_Rail'
SMOOTH_ANGLE = 40.0
BUDGET = (160, 60, 24)
VARIANTS = {}
LEVEL = lambda kit, lv: L.mobile_rail(kit, lv)


def build(kit):
    B.levels(kit, LEVEL)
    kit.min_ceiling = 0.0
    S.finish(kit, (4.0, 12.0, 40.0), BUDGET, tags=('structures', 'p16', 'file_mezzanine', 'floor_rail'))
    L.meta(kit)
