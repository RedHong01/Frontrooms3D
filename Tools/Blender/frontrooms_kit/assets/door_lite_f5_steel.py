"""Kit_DoorLite_F5_Steel: the 15 x 15 in pressed-steel lite kit of the title Run! pair (C-1: Run!'s F5, door_vision 50 / 60,
the RN-F goal door's form). See door_lite_steel_pair_common."""

import random
import sys

import interact_door_common as dc
import door_pair_common as pc
import door_lite_steel_pair_common as lc

NAME = "Kit_DoorLite_F5_Steel"
LOD_DISTANCES = (2.0, 8.0, 40.0)
SMOOTH_ANGLE = 35.0


def build(kit):
    lc.build_lite(kit, "F5", pc.F5, random.Random(51165))
    dc.lod_meta(kit, sys.modules[__name__])
