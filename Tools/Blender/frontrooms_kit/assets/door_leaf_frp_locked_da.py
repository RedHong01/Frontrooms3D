"""Kit_DoorLeaf_FRPLocked_DA: the pool map member's LOCKED leaf, row PL-K (zone doors, the Map stage, 2026-11-06;
research/double_doors/20_design.md section B10 "T6", 30_build.md Part M): the same moulded FRP leaf as PL-F in the course aqua
gel coat one step deeper (Door_PoolGelcoat_MapAqua, sRGB about 66 158 163, PD-A's frame colour): T6 turned for the white pool
zones (the free white leaf cannot have a brighter locked leaf, as birch failed; the locked leaf reads at least 1 stop DARKER), with a
630 mortise armor front (8 in, the latch and deadbolt openings) and the key-door anchors.

Shared numbers and builders: door_leaf_map_common.py. Slots: Door_PoolGelcoat_MapAqua, Glass_Tempered, Prop_Stainless630.
"""

import sys

import door_leaf_map_common as mc

NAME = "Kit_DoorLeaf_FRPLocked_DA"
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0


def build(kit):
    mc.build_frp_leaf(kit, mc.GELCOAT_AQUA, True, 61123)
    mc.frp_leaf_meta(kit, sys.modules[__name__], True)
