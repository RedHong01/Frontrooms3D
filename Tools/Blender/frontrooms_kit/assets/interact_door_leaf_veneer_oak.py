"""Flush oak-veneer door leaf with satin-chrome hinge halves and faceplate:
the FREE door of the Office (OF-F) and the P2 Exit free door (EX-F)
(10_spec §2.1 table, §2.3 "Kit_DoorLeaf_Veneer (VARIANT _Oak)").

Fix pass 2026-10-08 (critic H3): until now this asset was a VARIANT of
interact_door_leaf_veneer.py, i.e. the same mesh with Door_Veneer swapped for
Prop_WoodOak. The swap kept the veneer's UVs, which run the grain along
texture V; Prop_WoodOak_A draws its grain along texture U, so the oak leaf
showed horizontal grain (threeview Kit_DoorLeaf_Veneer_Oak_front.png). This
module builds the identical mesh (same code, same seed, same envelope, same
anchors and sidecar fields) with the slab's UVs turned 90 deg onto U
(interact_door_common.u_grain), so the oak grain runs up the leaf like a
real rotary/plain-sliced oak veneer door.

Real-world reference, origin, envelope, LOD and hardware: exactly as
Kit_DoorLeaf_Veneer (see its docstring): DOOR ROOT D, closed pose, spawn
under "Leaf rig" at 0 with label "Leaf (kit)"; X +-0.022, Y 0.015..2.095,
hinge edge Z 0.005, latch edge 0.995 (S) / 0.9927 (P); 3 deg lock-edge
bevel; bored-latch faceplate; leaf halves of three 4-1/2" butts.

Budget (§9.1): 2,600 / 900 / 120 tris (as the veneer leaf). Slots
Prop_WoodOak (submesh 0) and Prop_Chrome (US26D). Render-only.
Era: oak-veneered flush doors with satin-chrome butts are the 1970s-90s US
office standard (02 §2.1).
"""

import sys

import interact_door_leaf_veneer as base

NAME = "Kit_DoorLeaf_Veneer_Oak"
LOD1 = base.LOD1
LOD1_RATIO = base.LOD1_RATIO
LOD2_RATIO = base.LOD2_RATIO
LOD_DISTANCES = base.LOD_DISTANCES
SMOOTH_ANGLE = base.SMOOTH_ANGLE

OAK = "Prop_WoodOak"
CHROME = "Prop_Chrome"


def build(kit):
    base.build_flush(kit, OAK, CHROME, ugrain=True)
    assert any(p.get("fr_ugrain") for p in kit.parts), "the oak slab must carry its grain on texture U"
    base.leaf_meta(kit, sys.modules[__name__])
    kit.meta["grainAxis"] = "texture U (Prop_WoodOak_A is U-grain; see interact_door_common.u_grain)"
