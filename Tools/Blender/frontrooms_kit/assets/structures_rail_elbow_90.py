"""Kit_RailElbow_90: 90 degree bend from a top rail into a post (P4 structures_rail_kits, structures_assets G1-b 2026-11-06). Built in _struct_rails.elbow_90."""

import _run_b_common as B
import _struct_common as S
import _struct_rails as R

NAME = "Kit_RailElbow_90"
SMOOTH_ANGLE = 40.0
BUDGET = (1000, 360, 90)
VARIANTS = R.METAL_VARIANTS(NAME)


def build(kit):
    B.levels(kit, R.elbow_90)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, R.SMALL, BUDGET, tags=("structures", "rail", "p4",))
