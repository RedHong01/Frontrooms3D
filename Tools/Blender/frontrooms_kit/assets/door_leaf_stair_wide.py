"""Kit_DoorLeaf_StairWide: the stairwell's sealed south leaf (stairwell doors, 2026-11-06; research/double_doors/30_build.md
Part S): the EXIT door as seen from inside X once it has sealed, in the shell's 1.12 m south doorway (FrontRoomsStairwell
Doorway: the map's cube 0.05 x 2.08 x 1.10 m, shown from Seal on). Kit_DoorLeaf_Stair's build at W 1.12 (a 3'8" leaf, 4 mm
gaps), the same F13 cut-out at the same place from the hinge edge (the dresser hangs the dead lite there), every face in
Stair_DoorSteelInside as today's cube (seen only from inside). Its face u is squeezed onto the standard leaf's 0.992 m, so
DoorWear_Stair lands as on the EXIT. LODs as Kit_DoorLeaf_Stair. Switch 5 / 15 m, never culled.
"""

import sys

import kitlib

import door_stair_common as sc

NAME = "Kit_DoorLeaf_StairWide"
LOD_DISTANCES = (5.0, 15.0, None)
SMOOTH_ANGLE = 35.0

sc.register_slots(kitlib)


def build(kit):
    sc.build_leaf(kit, sys.modules[__name__], sc.SOUTH_W, True, False, sc.STEEL_IN, sc.STEEL_IN, sc.STEEL_IN, 61108)
    kit.meta["stairDoor"] = "sealed south leaf (shell doorway, shown from Seal on): every face the stair's own lights"
