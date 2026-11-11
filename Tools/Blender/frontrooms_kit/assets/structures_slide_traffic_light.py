"""Kit_SlideTrafficLight: the start light: a black head, two 160 mm aspects a face behind domed glass lenses under tunnel visors (63 6.2; LS114) (structures G2-b 2026-11-08, package structures_slide_start). Built in _struct_slide.traffic_light."""

import _run_b_common as B
import _struct_common as S
import _struct_slide as SL

NAME = "Kit_SlideTrafficLight"
SMOOTH_ANGLE = 40.0
BUDGET = (3200, 1300, 260)


def build(kit):
    B.levels(kit, SL.traffic_light)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, SL.DIST_SMALL, BUDGET, tags=("structures", "slide", "p15"))
