"""Kit_OpenShelfFile_Double (structures_assets G3-a2 2026-11-10, P16 the SP3 file mezzanine; geometry in _struct_l0.open_shelf_file). Double-faced 0.914 x 0.610 x 2.134 (folders both faces). Collider the body."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_OpenShelfFile_Double'
SMOOTH_ANGLE = 40.0
BUDGET = (8000, 3000, 300)
VARIANTS = {}
LEVEL = lambda kit, lv: L.open_shelf_file(kit, lv, double=True, seed=2)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (5.0, 15.0, 60.0), BUDGET, tags=('structures', 'p16', 'file_mezzanine', 'shelving'))
    L.meta(kit)
