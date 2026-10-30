"""Kit_CoffeeCup: an 8 oz foam cup of cold coffee and a ring stain (30L S24; NN §2) (built by station_desk_small.coffee_cup; plan 70; 游戏视觉 station-look-v1).

ORIGIN: the desk surface under the cup. Budget 1200, 400, 0. No collider. No pile block.
"""

import _station_common as sc
import station_desk_small as small

NAME = "Kit_CoffeeCup"
LODS = (0.94, 0.0)    # small desk/wall kit: LOD1 drops sub-pixel parts only (no collapse), no LOD2
SMOOTH_ANGLE = 40.0


def build(kit):
    small.coffee_cup(kit)
    sc.lods(kit, 3.0, 8.0, 20.0, (1200, 400, 0))
    sc.station_meta(kit, "the desk surface under the cup", "")
