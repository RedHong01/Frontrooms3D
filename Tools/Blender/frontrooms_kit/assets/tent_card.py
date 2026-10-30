"""Kit_TentCard: the PLEASE SIGN IN tent card (30L S22; 01 §6) (built by station_desk_small.tent_card; plan 70; 游戏视觉 station-look-v1).

ORIGIN: the ledge surface under the card's centre. Budget 300, 100, 0. No collider. No pile block.
"""

import _station_common as sc
import station_desk_small as small

NAME = "Kit_TentCard"
LODS = (0.94, 0.0)    # small desk/wall kit: LOD1 drops sub-pixel parts only (no collapse), no LOD2
SMOOTH_ANGLE = 40.0


def build(kit):
    small.tent_card(kit)
    sc.lods(kit, 3.0, 8.0, 25.0, (300, 100, 0))
    sc.station_meta(kit, "the ledge surface under the card's centre", "")
