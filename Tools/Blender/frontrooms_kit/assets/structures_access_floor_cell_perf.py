"""Kit_AccessFloor_CellPerf_3m (structures_assets G3-a1 2026-11-10, P16 the SP3 computer room; geometry in _struct_l0_computer). G1's 3 m cell with its z -1.2 row of five perforated air panels. Origin the cell centre at the floor top; no collider."""

import _run_b_common as B
import _struct_common as S
import _struct_l0_computer as C

NAME = "Kit_AccessFloor_CellPerf_3m"
SMOOTH_ANGLE = 40.0
BUDGET = (3000, 1200, 320)
VARIANTS = {}
LEVEL = C.access_cell_perf


def build(kit):
    B.levels(kit, LEVEL)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, (8.0, 22.0, 90.0), BUDGET, tags=("structures", "p16", "access_floor"))
    C.meta(kit)
