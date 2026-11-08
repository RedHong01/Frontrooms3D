"""Kit_DoorGuard_Casing38: station D's +Z casing guard (D-3) over Kit_DoorCasing_StationPair's 38 mm band; its side wing stops
on the lining cap (Fix A: the cap's face at z 0.025). See door_guard_casing_common."""

import sys

import interact_door_common as dc
import door_pair_common as pc
import door_guard_casing_common as g

NAME = "Kit_DoorGuard_Casing38"
LOD_DISTANCES = (2.0, 8.0, 30.0)
SMOOTH_ANGLE = 35.0


def build(kit):
    # Fix A: the cap's face now stands at z 0.025 (over the map's south liner); the wing ends 0.05 mm into it (seated: no
    # hairline over the cap, no shared plane)
    side_end = (pc.CAP_FRONT - 0.00005) - pc.FACE_Z
    g.build_guard(kit, pc.STATION_FACE, side_end, "guard 38")
    dc.lod_meta(kit, sys.modules[__name__])
