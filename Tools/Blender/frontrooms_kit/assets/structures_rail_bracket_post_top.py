"""Kit_RailBracket_PostTop: post-top saddle under a guard's top rail (P4 structures_rail_kits, structures_assets G1-b 2026-11-06). Built in _struct_rails.bracket_post_top."""

import _run_b_common as B
import _struct_common as S
import _struct_rails as R

NAME = "Kit_RailBracket_PostTop"
SMOOTH_ANGLE = 40.0
BUDGET = (900, 320, 80)
VARIANTS = R.METAL_VARIANTS(NAME)


def build(kit):
    B.levels(kit, R.bracket_post_top)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, R.SMALL, BUDGET, tags=("structures", "rail", "p4",))
