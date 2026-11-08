"""Kit_AccessFloor_Cell_3m (structures_assets G1-b 2026-11-06; geometry in _struct_shared). Origins, frames and slots: _struct_shared docstrings."""

import _run_b_common as B
import _struct_common as S
import _struct_shared as H

NAME = "Kit_AccessFloor_Cell_3m"
SMOOTH_ANGLE = 40.0
BUDGET = (2400, 300, 60)
VARIANTS = {}
LEVEL = H.access_cell


def build(kit):
    B.levels(kit, LEVEL)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, (8.0, 22.0, 90.0), BUDGET, tags=("structures", "p9", "access_floor",))
