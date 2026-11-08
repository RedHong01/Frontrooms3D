"""Kit_DoorGuard_Casing51: the night post's casing guard (S-2) over Kit_DoorFrame_SteelPair_DA's 50.8 mm band, on the night
post's face only; its side wing stops above the frame's 1.5 mm concave bend at the land. See door_guard_casing_common."""

import sys

import interact_door_common as dc
import door_pair_common as pc
import door_guard_casing_common as g

NAME = "Kit_DoorGuard_Casing51"
LOD_DISTANCES = (2.0, 8.0, 30.0)
SMOOTH_ANGLE = 35.0


def build(kit):
    side_end = (pc.LEAF_HALF_T + pc.BEND_RI + 0.0003) - pc.FACE_Z     # 0.3 mm over the frame's concave bend
    g.build_guard(kit, pc.STEEL_FACE, side_end, "guard 51")
    dc.lod_meta(kit, sys.modules[__name__])
