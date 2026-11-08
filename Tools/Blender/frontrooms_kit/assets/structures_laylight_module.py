"""Kit_Laylight_Module (structures_assets G1-b 2026-11-06; geometry in _struct_shared). Origins, frames and slots: _struct_shared docstrings."""

import _run_b_common as B
import _struct_common as S
import _struct_shared as H

NAME = "Kit_Laylight_Module"
SMOOTH_ANGLE = 40.0
BUDGET = (1500, 700, 120)
VARIANTS = {}
LEVEL = lambda kit, lv: H.laylight(kit, lv, H.CELLS_A)


def build(kit):
    B.levels(kit, LEVEL)
    kit.no_collider()
    kit.min_ceiling = 0.0
    kit.tag("ceiling")
    S.finish(kit, (10.0, 30.0, 0.0), BUDGET, tags=("structures", "p6", "laylight", "lamp",))
