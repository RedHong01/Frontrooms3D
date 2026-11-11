"""Kit_AccessFloor_CellOpen_3m (structures_assets G3-a1 2026-11-10, P16 the SP3 computer room; geometry in _struct_l0_computer). G1's 3 m access floor cell with panel (1, 3) out and the void below. Origin the cell centre at the floor top (y 0); no collider (the plate is the floor)."""

import _run_b_common as B
import _struct_common as S
import _struct_l0_computer as C

NAME = "Kit_AccessFloor_CellOpen_3m"
SMOOTH_ANGLE = 40.0
BUDGET = (4600, 1700, 400)
VARIANTS = {}
LEVEL = C.access_cell_open


def build(kit):
    B.levels(kit, LEVEL)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, (8.0, 22.0, 90.0), BUDGET, tags=("structures", "p16", "access_floor"))
    C.meta(kit)
