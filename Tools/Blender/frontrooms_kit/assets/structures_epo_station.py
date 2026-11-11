"""Kit_EPOStation (structures_assets G3-a1 2026-11-10, P16 the SP3 computer room; geometry in _struct_l0_computer). Emergency power-off station (LS83-LS85). Origin the wall point under the plate's bottom centre (mount at 1.2 m), front +Z; no collider."""

import _run_b_common as B
import _struct_common as S
import _struct_l0_computer as C

NAME = "Kit_EPOStation"
SMOOTH_ANGLE = 40.0
BUDGET = (1400, 500, 100)
VARIANTS = {}
LEVEL = lambda kit, lv: C.epo_station(kit, lv, halon=False)


def build(kit):
    B.levels(kit, LEVEL)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, (3.0, 8.0, 30.0), BUDGET, tags=("structures", "p16", "computer_room", "wall_decor"))
    C.meta(kit)
