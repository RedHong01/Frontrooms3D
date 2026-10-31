"""Hollow-metal DOUBLE-ACTING door leaf of the Run's locked row (RN-K, door vision set A, Red 2026-10-30): Kit_DoorLeaf_Steel_DA
with the low F5 15 x 15 lite instead of F1 (the Lobby and Office locked rows keep F1, so this is its own kit). Everything else
is that leaf: origin, envelope, LODs, slots, the sign on the top rail. See interact_door_leaf_steel_da and
research/door_vision/50_lite_forms.md section 6. Inert until the map's hook M8 returns Run from DoorMemberFor.
"""

import random
import sys

import interact_door_da_common as da
import interact_door_leaf_steel_da as base
import interact_door_lite_common as lite

NAME = "Kit_DoorLeaf_SteelRun_DA"
LOD1_RATIO = base.LOD1_RATIO
LOD2_RATIO = base.LOD2_RATIO
LOD_DISTANCES = base.LOD_DISTANCES
SMOOTH_ANGLE = base.SMOOTH_ANGLE


def build(kit):
    rng = random.Random(41250)
    lrng = random.Random(41255)
    form = lite.FORMS["F5"]
    base.build_steel(kit, rng, extra=lambda k, lv, c: lite.add_lite(k, lv, c, base.ENAMEL, "steel", lrng, base.CHROME, form=form))
    lite.lite_meta(kit, "steel", form)
    kit.meta["liteAsserts"] = lite.assert_lite(kit, "steel", form)
    da.leaf_anchors(kit, locked=True, sign_yz=(lite.SIGN_Y, lite.SIGN_Z))
    kit.no_collider()
    kit.tag("interactable", "door", "door_leaf", "run", "door_type_hollow_metal", "locked", "double_acting")
    kit.meta["doorType"] = "hollow_metal"
    da.lod_meta(kit, sys.modules[__name__])
