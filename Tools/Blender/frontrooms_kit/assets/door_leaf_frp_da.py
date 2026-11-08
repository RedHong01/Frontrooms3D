"""Kit_DoorLeaf_FRP_DA: the pool map member's FREE leaf, row PL-F (zone doors, the Map stage, 2026-11-06; research/double_doors/
20_design.md section B10, 30_build.md Part M): PD-A's language (D-5, the recommended look, built) on the D1.6 map leaf.

A moulded FRP DA leaf (Pitt US 4,068,431 / 4,429,498: a one-piece FRP U-collar and two FRP skins over a foam core, the edges
coated seamless; research Part P): the d16 envelope with r 3 mm moulded arrises and a parting seam on the latch end, in the white
gel coat (Door_PoolGelcoat_Map: the pool pair's gel coat + the map leaf's wear mask), the A1 16 in porthole on the hinge side
(centre Z 0.324, Y 1.397) with its 1/4 in clear tempered pane (Glass_Tempered; the row spawns the landed moulded ring
Kit_DoorLite_A1_FRP on both faces), and a 630 bored-latch faceplate at the lever (chrome plate pits in pool air, research N15).

Shared numbers and builders: door_leaf_map_common.py. Slots: Door_PoolGelcoat_Map, Glass_Tempered, Prop_Stainless630.
"""

import sys

import door_leaf_map_common as mc

NAME = "Kit_DoorLeaf_FRP_DA"
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0


def build(kit):
    mc.build_frp_leaf(kit, mc.GELCOAT, False, 61122)
    mc.frp_leaf_meta(kit, sys.modules[__name__], False)
