"""Kit_MicroficheReader (structures_assets G3-a2 2026-11-10, P16 the SP3 file mezzanine; geometry in _struct_l0.fiche_reader). 0.305 x 0.457 x 0.521, origin the desk point, front +Z; screen dark (Prop_ProjectionLit_Off); no collider."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_MicroficheReader'
SMOOTH_ANGLE = 40.0
BUDGET = (2200, 900, 120)
VARIANTS = {}
LEVEL = lambda kit, lv: L.fiche_reader(kit, lv)


def build(kit):
    B.levels(kit, LEVEL)
    kit.min_ceiling = 0.0
    S.finish(kit, (3.0, 9.0, 35.0), BUDGET, tags=('structures', 'p16', 'file_mezzanine', 'desk_top'))
    L.meta(kit)
