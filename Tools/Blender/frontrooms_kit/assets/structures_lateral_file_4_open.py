"""Kit_LateralFile_4_Open (structures_assets G3-a2 2026-11-10, P16 the SP3 file mezzanine; geometry in _struct_l0.lateral_file). The 2nd drawer out 0.38 m with hanging folders (the event); colliders body + open drawer."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_LateralFile_4_Open'
SMOOTH_ANGLE = 40.0
BUDGET = (3200, 1200, 130)
VARIANTS = {}
LEVEL = lambda kit, lv: L.lateral_file(kit, lv, drawers=4, open_k=2)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (4.0, 12.0, 45.0), BUDGET, tags=('structures', 'p16', 'file_mezzanine', 'cabinet'))
    L.meta(kit, service=0.9)
