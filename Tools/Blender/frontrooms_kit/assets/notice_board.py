"""Kit_NoticeBoard: a small cork notice board, blank-text notices from the atlas (30L S40) (built by station_desk_small.notice_board; plan 70; 游戏视觉 station-look-v1).

ORIGIN: the wall face at floor level, heights baked in (board y 1.20-1.65). Budget 1500, 500, 60. No collider. No pile block.
"""

import _station_common as sc
import station_desk_small as small

NAME = "Kit_NoticeBoard"
LODS = (0.94, 0.90)   # hard surface: LODs drop sub-pixel parts only (no collapse: it folded boxes into triangles)
SMOOTH_ANGLE = 40.0


def build(kit):
    small.notice_board(kit)
    sc.lods(kit, 3.0, 8.0, 40.0, (1500, 500, 60))
    sc.station_meta(kit, "the wall face at floor level, heights baked in (board y 1.20-1.65)", "")
