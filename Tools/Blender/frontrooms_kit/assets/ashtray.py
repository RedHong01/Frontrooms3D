"""Kit_Ashtray: a pressed-glass desk ashtray (30L S25 A variant; GAP 26) (built by station_desk_small.ashtray; plan 70; 游戏视觉 station-look-v1).

ORIGIN: the desk surface under the ashtray. Budget 1500, 500, 0. No collider. No pile block.
"""

import _station_common as sc
import station_desk_small as small

NAME = "Kit_Ashtray"
LODS = (0.94, 0.0)    # small desk/wall kit: LOD1 drops sub-pixel parts only (no collapse), no LOD2
SMOOTH_ANGLE = 40.0


def build(kit):
    small.ashtray(kit)
    sc.lods(kit, 3.0, 8.0, 20.0, (1500, 500, 0))
    sc.station_meta(kit, "the desk surface under the ashtray", "")
