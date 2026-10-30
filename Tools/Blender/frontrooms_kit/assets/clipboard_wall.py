"""Kit_Clipboard_Wall: the key sign-out sheet on a nail by the key cabinet (30L S15; 01 §6 header
DATE  KEY NO.  NAME  OUT  IN  INIT., the stair key's line last, its OUT blank; plan 70 P08, plan check C1-10).

clipboard.py's board hung on a nail: ORIGIN = THE WALL FACE AT FLOOR LEVEL, front +Z, board y 1.12-1.44.
Top sheet = paper atlas cell ``sign_out`` (blank until 平面视觉's art). EXAMINE-readable. Budget 1,800 / 600 / 60.
"""

import _station_common as sc
import clipboard as base

NAME = "Kit_Clipboard_Wall"
LODS = (0.94, 0.0)    # small desk/wall kit: LOD1 drops sub-pixel parts only (no collapse), no LOD2
SMOOTH_ANGLE = 35.0


def build(kit):
    base.build_board(kit, True, "sign_out")
    kit.tag("wall_decor", "station", "paper")
    sc.lods(kit, 3.0, 8.0, 30.0, (1800, 600, 60))
    sc.station_meta(kit, "wall face at floor level, heights baked in", "the key sign-out sheet (01 §6)")
