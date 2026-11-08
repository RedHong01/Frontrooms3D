"""Kit_DoorLeaf_SteelLouvreLocked_DA: the back-of-house map member's LOCKED leaf, row BH-K (zone doors, the Map stage,
2026-11-06; research/double_doors/10_research.md section B.4-B.5, 30_build.md Part M): the same leaf as BH-F
(Kit_DoorLeaf_SteelLouvre_DA) in the key-door look, almond Door_Enamel (T6: a locked steel door reads at least 1 stop brighter than
the free one: the warm grey is 1.5 stops under the almond by albedo), with Kit_DoorLeaf_Steel_DA's mortise armor front (8 in, the
latch and deadbolt openings) and the key-door anchors (escutcheon, the top-rail sign, the number plates).

Shared numbers and builders: door_leaf_map_common.py. Slots: Door_Enamel, Prop_Aluminium, Prop_Chrome, Glass_Wired.
"""

import sys

import door_leaf_map_common as mc

NAME = "Kit_DoorLeaf_SteelLouvreLocked_DA"
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0


def build(kit):
    mc.build_louvre_leaf(kit, mc.ENAMEL, True, 61121)
    mc.louvre_leaf_meta(kit, sys.modules[__name__], True)
