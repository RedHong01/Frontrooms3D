"""Kit_BinderPostOrders: the post-orders binder, spine POST ORDERS (30L S17; 01 §6) (built by station_desk_small.binder; plan 70; 游戏视觉 station-look-v1).

ORIGIN: the desk surface under the binder. Budget 360, 120, 0. No collider. No pile block.
"""

import _station_common as sc
import station_desk_small as small

NAME = "Kit_BinderPostOrders"
LODS = (0.94, 0.0)    # small desk/wall kit: LOD1 drops sub-pixel parts only (no collapse), no LOD2
SMOOTH_ANGLE = 40.0


def build(kit):
    small.binder(kit)
    sc.lods(kit, 3.0, 8.0, 25.0, (360, 120, 0))
    sc.station_meta(kit, "the desk surface under the binder", "")
