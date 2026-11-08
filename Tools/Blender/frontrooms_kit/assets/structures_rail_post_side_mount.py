"""Kit_RailPost_SideMount: slab-edge side mount of a rail post (P4 structures_rail_kits, structures_assets G1-b 2026-11-06). Built in _struct_rails.side_mount."""

import _run_b_common as B
import _struct_common as S
import _struct_rails as R

NAME = "Kit_RailPost_SideMount"
SMOOTH_ANGLE = 40.0
BUDGET = (1800, 600, 120)
VARIANTS = R.METAL_VARIANTS(NAME)


def build(kit):
    B.levels(kit, R.side_mount)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, R.SMALL, BUDGET, tags=("structures", "rail", "p4",))
