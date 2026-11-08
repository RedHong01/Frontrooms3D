"""Kit_CapRail_EndCap: end of the cap rail over a glass guard (P4 structures_rail_kits, structures_assets G1-b 2026-11-06). Built in _struct_rails.cap_rail_end."""

import _run_b_common as B
import _struct_common as S
import _struct_rails as R

NAME = "Kit_CapRail_EndCap"
SMOOTH_ANGLE = 40.0
BUDGET = (400, 160, 50)
VARIANTS = R.GLASS_VARIANTS(NAME)


def build(kit):
    B.levels(kit, R.cap_rail_end)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, R.SMALL, BUDGET, tags=("structures", "rail", "p4",))
