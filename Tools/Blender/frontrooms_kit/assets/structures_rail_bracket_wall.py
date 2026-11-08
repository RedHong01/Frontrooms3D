"""Kit_RailBracket_Wall: wall bracket of a handrail, rail axis at the origin (P4 structures_rail_kits, structures_assets G1-b 2026-11-06). Built in _struct_rails.bracket_wall."""

import _run_b_common as B
import _struct_common as S
import _struct_rails as R

NAME = "Kit_RailBracket_Wall"
SMOOTH_ANGLE = 40.0
BUDGET = (1800, 600, 120)
VARIANTS = R.METAL_VARIANTS(NAME)


def build(kit):
    B.levels(kit, R.bracket_wall)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, R.SMALL, BUDGET, tags=("structures", "rail", "p4",))
    kit.anchor("rail", S.ub((0.0, 0.0, 0.0)))
    kit.anchor("wall", S.ub((0.0, -0.085, S.WALL_Z)))
