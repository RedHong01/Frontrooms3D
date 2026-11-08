"""Kit_SteelShelving (structures_assets G1-b 2026-11-06; geometry in _struct_shared). Origins, frames and slots: _struct_shared docstrings."""

import _run_b_common as B
import _struct_common as S
import _struct_shared as H

NAME = "Kit_SteelShelving"
SMOOTH_ANGLE = 40.0
BUDGET = (9000, 1800, 300)
VARIANTS = {"Kit_SteelShelving_Beige": {"Prop_SteelPutty": "Prop_SteelBeige"}}
LEVEL = H.steel_shelving


def build(kit):
    B.levels(kit, LEVEL)
    kit.min_ceiling = 0.0
    S.finish(kit, (5.0, 15.0, 60.0), BUDGET, tags=("structures", "p9", "shelving", "storage",))
