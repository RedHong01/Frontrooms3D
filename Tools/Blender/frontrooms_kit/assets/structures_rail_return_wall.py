"""Kit_RailReturn_Wall: handrail end returned to the wall (P4 structures_rail_kits, structures_assets G1-b 2026-11-06). Built in _struct_rails.return_wall."""

import _run_b_common as B
import _struct_common as S
import _struct_rails as R

NAME = "Kit_RailReturn_Wall"
SMOOTH_ANGLE = 40.0
BUDGET = (1500, 520, 120)
VARIANTS = R.METAL_VARIANTS(NAME)


def build(kit):
    B.levels(kit, R.return_wall)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, R.SMALL, BUDGET, tags=("structures", "rail", "p4",))
