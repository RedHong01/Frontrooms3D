"""Kit_SystemCabinet_Disk (structures_assets G3-a1 2026-11-10, P16 the SP3 computer room; geometry in _struct_l0_computer.cabinet_disk).
Origin the floor centre, front +Z (Unity). Collider: the body box. Slots and sizes: _struct_l0_computer docstrings."""

import _run_b_common as B
import _struct_common as S
import _struct_l0_computer as C

NAME = "Kit_SystemCabinet_Disk"
SMOOTH_ANGLE = 40.0
BUDGET = (3800, 1300, 280)
VARIANTS = {"Kit_SystemCabinet_Disk_Off": {"Prop_CompIndicator": "Prop_CompIndicator_Off"},
            "Kit_SystemCabinet_Disk_Charcoal": {"Prop_PlasticPutty": "Prop_PlasticGrey"}}
LEVEL = C.cabinet_disk


def build(kit):
    B.levels(kit, LEVEL)
    C.cabinet_collider(kit, NAME)
    kit.min_ceiling = 0.0
    S.finish(kit, (6.0, 18.0, 70.0), BUDGET, tags=("structures", "p16", "computer_room", "row_unit"))
    C.meta(kit, service=1.2)
