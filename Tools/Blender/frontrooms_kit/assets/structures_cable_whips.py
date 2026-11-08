"""Kit_CableWhips (structures_assets G1-b 2026-11-06; geometry in _struct_shared). Origins, frames and slots: _struct_shared docstrings."""

import _run_b_common as B
import _struct_common as S
import _struct_shared as H

NAME = "Kit_CableWhips"
SMOOTH_ANGLE = 40.0
BUDGET = (2400, 700, 160)
VARIANTS = {}
LEVEL = H.cable_whips


def build(kit):
    B.levels(kit, LEVEL)
    kit.no_collider()
    kit.min_ceiling = 0.0
    kit.tag("ceiling")
    S.finish(kit, (4.0, 12.0, 40.0), BUDGET, tags=("structures", "p9", "ceiling", "plenum",))
