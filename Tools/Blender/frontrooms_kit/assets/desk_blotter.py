"""Kit_DeskBlotter: a desk blotter with a typed phone list under the desk glass, no printed date (30L S39) (built by station_desk_small.blotter; plan 70; 游戏视觉 station-look-v1).

ORIGIN: the desk surface under the pad's centre. Budget 600, 200, 0. No collider. No pile block.
"""

import _station_common as sc
import station_desk_small as small

NAME = "Kit_DeskBlotter"
LODS = (0.94, 0.0)    # small desk/wall kit: LOD1 drops sub-pixel parts only (no collapse), no LOD2
SMOOTH_ANGLE = 40.0


def build(kit):
    small.blotter(kit)
    sc.lods(kit, 3.0, 8.0, 25.0, (600, 200, 0))
    sc.station_meta(kit, "the desk surface under the pad's centre", "")
