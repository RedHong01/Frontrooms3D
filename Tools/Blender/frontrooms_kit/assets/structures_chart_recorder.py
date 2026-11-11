"""Kit_ChartRecorder (structures_assets G3-a1 2026-11-10, P16 the SP3 computer room; geometry in _struct_l0_computer). Circular-chart temperature and humidity recorder (S7). Origin the wall point under the case (mount at 1.5 m); no collider."""

import _run_b_common as B
import _struct_common as S
import _struct_l0_computer as C

NAME = "Kit_ChartRecorder"
SMOOTH_ANGLE = 40.0
BUDGET = (1600, 500, 100)
VARIANTS = {}
LEVEL = C.chart_recorder


def build(kit):
    B.levels(kit, LEVEL)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, (3.0, 8.0, 30.0), BUDGET, tags=("structures", "p16", "computer_room", "wall_decor"))
    C.meta(kit)
