"""Kit_GlassShoe_EndCap: end cap of the aluminium glass shoe (P4 structures_rail_kits, structures_assets G1-b 2026-11-06). Built in _struct_rails.shoe_end_cap."""

import _run_b_common as B
import _struct_common as S
import _struct_rails as R

NAME = "Kit_GlassShoe_EndCap"
SMOOTH_ANGLE = 40.0
BUDGET = (500, 220, 60)
VARIANTS = R.GLASS_VARIANTS(NAME)


def build(kit):
    B.levels(kit, R.shoe_end_cap)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, R.SMALL, BUDGET, tags=("structures", "rail", "p4",))
