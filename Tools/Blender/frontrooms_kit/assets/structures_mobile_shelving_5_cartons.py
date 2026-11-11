"""Kit_MobileShelving_5_Cartons (structures_assets G3-a2 2026-11-10, P16 the SP3 file mezzanine; geometry in _struct_l0.mobile_shelving). 5 bays, records cartons long side out on the lower tiers."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_MobileShelving_5_Cartons'
SMOOTH_ANGLE = 40.0
BUDGET = (7000, 2800, 300)
VARIANTS = {}
LEVEL = lambda kit, lv: L.mobile_shelving(kit, lv, bays=5, cartons=True, seed=4)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (6.0, 18.0, 70.0), BUDGET, tags=('structures', 'p16', 'file_mezzanine', 'shelving', 'mobile'))
    L.meta(kit)
