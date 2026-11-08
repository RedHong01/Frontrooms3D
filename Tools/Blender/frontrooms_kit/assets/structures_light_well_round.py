"""Kit_LightWell_Round (structures_assets G1-b 2026-11-06; geometry in _struct_shared). Origins, frames and slots: _struct_shared docstrings."""

import _run_b_common as B
import _struct_common as S
import _struct_shared as H

NAME = "Kit_LightWell_Round"
SMOOTH_ANGLE = 40.0
BUDGET = (1600, 500, 120)
VARIANTS = {}
LEVEL = H.light_well


def build(kit):
    B.levels(kit, LEVEL)
    kit.no_collider()
    kit.min_ceiling = 0.0
    kit.tag("ceiling")
    S.finish(kit, (8.0, 24.0, 0.0), BUDGET, tags=("structures", "p6", "laylight", "lamp", "pool",))
