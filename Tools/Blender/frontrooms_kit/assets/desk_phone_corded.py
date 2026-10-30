"""Kit_DeskPhone_Corded: the guard's key telephone (30L S18, §10.3 S38; 20L G9; plan 70 P21).

desk_phone.py's Kit_DeskPhone (putty key set, coiled handset cord and line cord, its authored LOD1) under a station
name: line lamps dark (no hold lamp: 30L §4.3), and NO pile block / pile_piece tag (plan check C2-4), so
FrontRoomsFurniturePile never picks it and every Office desk phone stays as it is. ORIGIN = the desk surface.
"""

import _station_common as sc
import desk_phone as base

NAME = "Kit_DeskPhone_Corded"
LOD1 = base.LOD1
SMOOTH_ANGLE = getattr(base, "SMOOTH_ANGLE", 35.0)


def build(kit):
    base.build(kit)
    sc.no_pile(kit)
    kit.meta["tags"] = [t for t in kit.meta["tags"] if t != "office"] + ["station"]
    kit.meta["lineLamps"] = "dark (30L §4.3)"
    sc.station_meta(kit, "desk surface", "Kit_DeskPhone geometry; no pile block")
