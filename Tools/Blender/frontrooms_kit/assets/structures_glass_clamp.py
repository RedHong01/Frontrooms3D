"""Kit_GlassClamp: D clamp of a glass guard pane on a round post (P4 structures_rail_kits, structures_assets G1-b 2026-11-06). Built in _struct_rails.glass_clamp."""

import _run_b_common as B
import _struct_common as S
import _struct_rails as R

NAME = "Kit_GlassClamp"
SMOOTH_ANGLE = 40.0
BUDGET = (900, 300, 60)
VARIANTS = R.GLASS_VARIANTS(NAME)


def build(kit):
    B.levels(kit, R.glass_clamp)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, R.SMALL, BUDGET, tags=("structures", "rail", "p4",))
