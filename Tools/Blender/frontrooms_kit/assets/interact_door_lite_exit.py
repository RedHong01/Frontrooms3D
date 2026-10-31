"""The C22 EXIT door's vision lite (door vision set A, F13 4 x 25, Red 2026-10-30): one rated narrow lite, a 1/4 in wired pane
in an 18 ga pressed steel kit, for FrontRoomsStairwellDresser to hang on the map's EXIT leaf rig. The only lite under 200 sq in:
a stair door is rated and single-acting, so Title 8's double-acting rule does not apply and NFPA 80 caps it at 100 sq in
(research/door_vision/50_lite_forms.md sections 1, 6). No peek. Six slotted oval-head screws on face S.

Origin: the EXIT leaf rig (the map's hinge: +Y up from the floor, +Z along the opening, +X one face). Authored for a 44 mm leaf;
the dresser scales X 25/22 for the map's 50 mm cube leaf. Slots: Door_Enamel on face S, Painted_Metal on face P, Glass_Wired;
the dresser maps the two faces to the stair's finishes (the stair side must not dip with W1). Slab cut-out: none here, the
dresser builds the holed leaf (FrontRoomsDoorLite.HoledUnitLeaf).
"""

import random
import sys

import kitlib

import interact_door_common as dc
import interact_door_lite_common as lite

NAME = "Kit_DoorLite_Exit"
LOD1_RATIO = 0.6
LOD2_RATIO = 0.3
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0

FACE_S, FACE_P = "Door_Enamel", "Painted_Metal"
kitlib.register_slot(FACE_S, (0.610, 0.558, 0.434), 0.5, 0.0)
kitlib.register_slot(FACE_P, (0.644, 0.610, 0.509), 0.5, 0.0)
kitlib.register_slot(lite.GLASS_SLOT, (0.80, 0.85, 0.83), 0.05, 0.0)


def build(kit):
    rng = random.Random(41290)
    form = lite.FORMS["F13"]
    slots = {1: FACE_S, -1: FACE_P}
    for level in (0, 1, 2):
        lite.add_lite(kit, level, [], slots, "steel", rng, FACE_S, form=form)
    dc.finalize(kit, SMOOTH_ANGLE)
    kit.meta["liteRect"] = list(form.rect)
    kit.meta["liteForm"] = form.key
    kit.meta["liteAreaSqIn"] = round(form.area_sq_in, 1)
    kit.meta["liteAsserts"] = lite.assert_lite(kit, "steel", form)
    kit.no_collider()
    kit.tag("door", "lite", "vision_lite", "exit")
    dc.lod_meta(kit, sys.modules[__name__])
