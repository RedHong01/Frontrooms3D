"""Kit_FloorTileLifted (structures_assets G3-a1 2026-11-10, P16 the SP3 computer room; geometry in _struct_l0_computer). A lifted 600 mm panel leaning back, its suction lifter on it (62 3.1, LS84). No collider."""

import _run_b_common as B
import _struct_common as S
import _struct_l0_computer as C

NAME = "Kit_FloorTileLifted"
SMOOTH_ANGLE = 40.0
BUDGET = (1100, 400, 80)
VARIANTS = {}
LEVEL = lambda kit, lv: C.floor_tile_lifted(kit, lv, flat_on_floor=False)


def build(kit):
    B.levels(kit, LEVEL)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, (3.0, 9.0, 30.0), BUDGET, tags=("structures", "p16", "computer_room", "clutter", "access_floor"))
    C.meta(kit)
