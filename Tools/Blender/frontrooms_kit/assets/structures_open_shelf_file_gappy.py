"""Kit_OpenShelfFile_Gappy (structures_assets G3-a2 2026-11-10, P16 the SP3 file mezzanine; geometry in _struct_l0.open_shelf_file). Gaps, leaning runs, red / blue out-guides. Collider the body."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_OpenShelfFile_Gappy'
SMOOTH_ANGLE = 40.0
BUDGET = (4500, 1800, 250)
VARIANTS = {}
LEVEL = lambda kit, lv: L.open_shelf_file(kit, lv, gappy=True, seed=1)


def build(kit):
    B.levels(kit, LEVEL)
    S.finish(kit, (5.0, 15.0, 60.0), BUDGET, tags=('structures', 'p16', 'file_mezzanine', 'shelving'))
    L.meta(kit)
