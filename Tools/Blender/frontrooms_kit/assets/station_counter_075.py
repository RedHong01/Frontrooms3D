"""Kit_StationCounter_Run075: the 0.75 m cut of the security counter run (plan 70 P10, GAP 5/6): the same section
as Kit_StationCounter_Run120 (station_counter.py), without the release button and the intercom. Budget ~2,500 / 850 / 110."""

import station_counter as base

NAME = "Kit_StationCounter_Run075"
LODS = (0.94, 0.90)   # hard surface: LODs drop sub-pixel parts only (no collapse: it folded boxes into triangles)
SMOOTH_ANGLE = 35.0


def build(kit):
    base.build_run(kit, 0.75, False)
    kit.meta["budget"] = [2500, 850, 110]
