"""Oak-veneered DOUBLE-ACTING door leaf with satin-chrome hardware: the
Office FREE door (OF-F) and the P2 Exit free door (EX-F) (D1.6, A12 / A19).
Its own build (as Kit_DoorLeaf_Veneer_Oak): the same DA mesh as
Kit_DoorLeaf_Veneer_DA with the slab's grain turned onto texture U
(Prop_WoodOak_A is U-grain; interact_door_common.u_grain, critic H3).

Real-world reference, origin, envelope, LODs and hardware: exactly as
Kit_DoorLeaf_Veneer_DA (see its docstring): DOOR ROOT D = the pivot axis,
spawn under "Leaf rig" at 0, label "Leaf (kit)"; radius stile R 0.022, flat
latch end Z 0.995, bored-latch faceplate, pivot plates. Slots Prop_WoodOak
(submesh 0), Prop_Chrome (US26D). Render-only.
"""
import random
import sys

import interact_door_da_common as da
import interact_door_leaf_veneer_da as base

NAME = "Kit_DoorLeaf_Veneer_Oak_DA"
LOD1_RATIO = base.LOD1_RATIO
LOD2_RATIO = base.LOD2_RATIO
LOD_DISTANCES = base.LOD_DISTANCES
SMOOTH_ANGLE = base.SMOOTH_ANGLE

OAK = "Prop_WoodOak"
CHROME = "Prop_Chrome"


def build(kit):
    rng = random.Random(41240)
    base.build_flush(kit, OAK, CHROME, rng, ugrain=True)
    assert any(p.get("fr_ugrain") for p in kit.parts), "the oak slab must carry its grain on texture U"
    da.leaf_anchors(kit, locked=False)
    kit.no_collider()
    kit.tag("interactable", "door", "door_leaf", "office", "exit", "door_type_wood", "double_acting")
    kit.meta["doorType"] = "wood"
    kit.meta["grainAxis"] = "texture U (Prop_WoodOak_A is U-grain; see interact_door_common.u_grain)"
    da.lod_meta(kit, sys.modules[__name__])
