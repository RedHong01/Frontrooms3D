"""Kit_RadioCharger: the empty drop-in radio charger, dark lamp, spare battery (30L S19; NN §2) (built by station_desk_small.radio_charger; plan 70; 游戏视觉 station-look-v1).

ORIGIN: the desk surface under the charger. Budget 2000, 700, 80. No collider. No pile block.
"""

import _station_common as sc
import station_desk_small as small

NAME = "Kit_RadioCharger"
LODS = (0.94, 0.0)    # small desk/wall kit: LOD1 drops sub-pixel parts only (no collapse), no LOD2
SMOOTH_ANGLE = 40.0


def build(kit):
    small.radio_charger(kit)
    sc.lods(kit, 3.0, 8.0, 30.0, (2000, 700, 80))
    sc.station_meta(kit, "the desk surface under the charger", "")
