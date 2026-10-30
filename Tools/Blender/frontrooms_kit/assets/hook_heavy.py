"""Kit_HookHeavy: the empty heavy watchclock hook with the strap's buckle loop (30L S26 as reworded in §10.3) (built by station_desk_small.hook_heavy; plan 70; 游戏视觉 station-look-v1).

ORIGIN: the wall face at floor level, hook at y 1.60. Budget 1200, 400, 0. No collider. No pile block.
"""

import _station_common as sc
import station_desk_small as small

NAME = "Kit_HookHeavy"
LODS = (0.94, 0.0)    # small desk/wall kit: LOD1 drops sub-pixel parts only (no collapse), no LOD2
SMOOTH_ANGLE = 40.0


def build(kit):
    small.hook_heavy(kit)
    sc.lods(kit, 3.0, 8.0, 25.0, (1200, 400, 0))
    sc.station_meta(kit, "the wall face at floor level, hook at y 1.60", "")
