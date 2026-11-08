"""Kit_RailEnd_Cap: domed end cap of a rail tube (P4 structures_rail_kits, structures_assets G1-b 2026-11-06). Built in _struct_rails.end_cap."""

import _run_b_common as B
import _struct_common as S
import _struct_rails as R

NAME = "Kit_RailEnd_Cap"
SMOOTH_ANGLE = 40.0
BUDGET = (240, 100, 40)
VARIANTS = R.METAL_VARIANTS(NAME)


def build(kit):
    B.levels(kit, R.end_cap)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, R.SMALL, BUDGET, tags=("structures", "rail", "p4",))
