"""Kit_DoorLeaf_SteelLouvre_DA: the back-of-house map member's FREE leaf, row BH-F (zone doors, the Map stage, 2026-11-06; Red
2026-11-05 21:26:27, D-2 option 2: a new back-of-house door, painted steel, kick plate, louvre, so the low areas show doors more
like plant rooms; verbatim in handoff/visual.md section 5 0x; research/double_doors/10_research.md section B.5, 30_build.md Part M). One back-of-house language with the title LOW pair (B-2).

The painted hollow-metal DA leaf (Kit_DoorLeaf_Steel_DA's 16 ga faces, channel lines, satin-stainless kick plates both faces,
the pressed F1 10 x 30 lite painted with the leaf, six oval heads on face S) in the B-2a warm service grey (Door_BackOfHouse_Map:
Door_BackOfHouse + the map leaf's wear mask), with the A2 louvre's cut-out on the F1 axis at 13-29 in (the row spawns the landed
Kit_DoorLouvre_F12 into it) and the bored latch's satin-chrome faceplate at the lever (Y 1.000), as the OF-F set (Red A19:
handles kept). A 1990 service room's single keyed door, hung double-acting: the closest look the D1.6 structure allows.

Shared numbers and builders: door_leaf_map_common.py. Slots: Door_BackOfHouse_Map (submesh 0: the slab and the lite's pressed
frame), Prop_Aluminium (kick plates, swapped to Prop_StainlessSatin by the kit facade), Prop_Chrome, Glass_Wired.
"""

import sys

import door_leaf_map_common as mc

NAME = "Kit_DoorLeaf_SteelLouvre_DA"
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0


def build(kit):
    mc.build_louvre_leaf(kit, mc.BOH, False, 61120)
    mc.louvre_leaf_meta(kit, sys.modules[__name__], False)
