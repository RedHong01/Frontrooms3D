"""Kit_SystemCabinet_DiskPack (structures_assets G3-a1 2026-11-10, P16 the SP3 computer room; geometry in _struct_l0_computer.cabinet_diskpack).
Origin the floor centre, front +Z (Unity). Collider: the body box. Slots and sizes: _struct_l0_computer docstrings."""

import _run_b_common as B
import _struct_common as S
import _struct_l0_computer as C

NAME = "Kit_SystemCabinet_DiskPack"
SMOOTH_ANGLE = 40.0
BUDGET = (3000, 1000, 220)
VARIANTS = {"Kit_SystemCabinet_DiskPack_Off": {"Prop_CompIndicator": "Prop_CompIndicator_Off"}}
LEVEL = C.cabinet_diskpack


def build(kit):
    B.levels(kit, LEVEL)
    C.cabinet_collider(kit, NAME)
    kit.min_ceiling = 0.0
    S.finish(kit, (5.0, 15.0, 60.0), BUDGET, tags=("structures", "p16", "computer_room", "row_unit"))
    C.meta(kit, service=1.2)
