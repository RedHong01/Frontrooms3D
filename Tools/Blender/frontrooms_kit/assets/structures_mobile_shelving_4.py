"""Kit_MobileShelving_4 (structures_assets G3-a2 2026-11-10, P16 the SP3 file mezzanine; geometry in _struct_l0.mobile_shelving). 4 bays of 36 in, double-faced, aisle end (hand wheel) at +X (anchor aisleEnd); collider the carriage."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_MobileShelving_4'
SMOOTH_ANGLE = 40.0
BUDGET = (5800, 2300, 260)
VARIANTS = {}
LEVEL = lambda kit, lv: L.mobile_shelving(kit, lv, bays=4)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (6.0, 18.0, 70.0), BUDGET, tags=('structures', 'p16', 'file_mezzanine', 'shelving', 'mobile'))
    L.meta(kit)
