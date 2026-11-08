"""Kit_DoorLite_F1_Steel: the 10 x 30 in pressed-steel lite kit of the back-of-house pair (B-2: the A2 lite, door_vision 50).
See door_lite_steel_pair_common."""

import random
import sys

import interact_door_common as dc
import door_pair_common as pc
import door_lite_steel_pair_common as lc

NAME = "Kit_DoorLite_F1_Steel"
LOD_DISTANCES = (2.0, 8.0, 40.0)
SMOOTH_ANGLE = 35.0


def build(kit):
    lc.build_lite(kit, "F1", pc.F1, random.Random(51161))
    dc.lod_meta(kit, sys.modules[__name__])
