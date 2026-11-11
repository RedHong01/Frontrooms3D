"""Kit_MicroficheReader_Carousel (structures_assets G3-a2 2026-11-10, P16 the SP3 file mezzanine; geometry in _struct_l0.fiche_reader). With a rotary fiche file at -X; no collider."""

import _run_b_common as B
import _struct_common as S
import _struct_l0 as L

NAME = 'Kit_MicroficheReader_Carousel'
SMOOTH_ANGLE = 40.0
BUDGET = (2800, 1100, 150)
VARIANTS = {}
LEVEL = lambda kit, lv: L.fiche_reader(kit, lv, carousel=True)


def build(kit):
    B.levels(kit, LEVEL)
    kit.min_ceiling = 0.0
    S.finish(kit, (3.0, 9.0, 35.0), BUDGET, tags=('structures', 'p16', 'file_mezzanine', 'desk_top'))
    L.meta(kit)
