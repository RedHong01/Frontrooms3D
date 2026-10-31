"""Wood DOUBLE-ACTING door frame for the Lobby FREE door (L0-F, D1.6 A12 /
A19: every map door swings both ways). A cased opening with no stop: a plain
walnut casing on both faces in sleeve mode, 57.2 x 17.5 mm (2-1/4 x 11/16 in,
F4) stepping back 4.8 mm from the opening, with a flat crown and a rounded outer
edge (it encloses the map's door-only trims), walnut linings over the bare
reveal, black brush seals on the wall centre plane (hinge seal, latch fin, head fin), an oak saddle, and
brass pivot caps (head soffit, floor). No stops, no butts, no closer.

Real-world reference: a US commercial double-acting door on centre-hung
top and bottom pivots, 1960s-1990 (kitchen, hospital and back-of-house
corridors): "nothing visible but a pivot cap" (02 §5), radius-cut pivot
edge with brush strips "for privacy" on both stiles (04 §3.2), a cased
opening with no stop (02 §2.2). Here in a Lobby walnut casing; no maker
marks. Era fit: everything here was ordinary 1955-1990.

Origin: DOOR ROOT D = the pivot axis: the opening's hinge-jamb edge on the
wall centre plane at floor level (the map's "Door hinge" position; no A2
offset, no handing mirror: A12). Spawn under the chunk root at the hinge's
local position with rotation door.closed and scale 1; label
"Door frame (kit)". +Z along the opening, +X = face S (the door root's +X).

Section (10_design §2.2, F4): linings X +-0.0795, faces Z 0.002 / 0.998, head
2.098; casing X +-(0.0795..0.0975), Z -0.060..0.002 and 0.998..1.060, head
2.098..2.160; seals |X| <= 0.025 (hinge, Z 0..0.0015),
|X| <= 0.004 (latch fin Z 0.994..0.998, head fin Y 2.093..2.098); saddle
0.152 x 0.012; a 1.6 mm ANSI strike pocket (0.032 x 0.124 at Y 1.000) for
Kit_Lock_StrikeBored_Brass_DA.

LODs (hand-built, fr_lods): LOD0 casing, linings with the
strike pocket, saddle, seals, 96-segment caps; LOD1 the same without the
pocket and with 32-segment caps; LOD2 box casing, linings, seals, a plain
saddle. Slots: Prop_WoodWalnut (submesh 0), Prop_WoodOak, Prop_Brass,
Prop_PlasticBlack (seals). Render-only: kit.no_collider().
"""

import random
import sys

import interact_door_common as dc
import interact_door_da_common as da

NAME = "Kit_DoorFrame_Wood_DA"
LOD1_RATIO = 0.40
LOD2_RATIO = 0.09
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0

WOOD = "Prop_WoodWalnut"
OAK = "Prop_WoodOak"
BRASS = "Prop_Brass"
SEAL = "Prop_PlasticBlack"


def build(kit):
    dc.u_grain(kit)
    rng = random.Random(41210)
    envelope = []
    for level in (0, 1, 2):
        lv = str(level)
        parts = []
        if level < 2:
            if level == 0:
                cas = da.casing(kit, WOOD, 0, rng)
                for c in cas:
                    da.tag_lods(c, "01")
                parts += cas
        else:
            parts += da.casing(kit, WOOD, 2, rng)
        if level == 0:
            lin = da.linings(kit, WOOD, 0, strike=da.STRIKE_BORED)
            for o in lin:
                da.tag_lods(o, "0")
            parts += lin
        elif level == 1:
            lin = da.linings(kit, WOOD, 1)
            for o in lin:
                da.tag_lods(o, "12")
            parts += lin
        if level == 0:
            sec = dc.saddle_section(flutes=False)
            saddle = dc.loft_z(kit, [(da.LINING_Z_H, sec), (da.LINING_Z_L, sec)], OAK, "saddle", grain="y")
            saddle["fr_ugrain"] = True
            da.tag_lods(saddle, "01")
            parts.append(saddle)
        elif level == 2:
            sec = [(-da.SADDLE_HALF, 0.0), (-da.SADDLE_FLAT_HALF, da.SADDLE_H), (da.SADDLE_FLAT_HALF, da.SADDLE_H), (da.SADDLE_HALF, 0.0)]
            saddle = dc.loft_z(kit, [(da.LINING_Z_H, sec), (da.LINING_Z_L, sec)], OAK, "saddle lod2", grain="y")
            saddle["fr_ugrain"] = True
            parts.append(saddle)
        if level == 0:
            parts += da.seals(kit, SEAL, "012")
        if level < 2:
            for c in da.pivot_caps(kit, BRASS, level):
                da.tag_lods(c, lv)
                if level == 0:
                    dc.lod2_drop(c)
        for p in parts:
            if "fr_lods" not in p:
                da.tag_lods(p, lv)
        envelope += parts

    dc.finalize(kit, SMOOTH_ANGLE, da.wear_frame, protect=envelope)

    da.assert_frame(kit, NAME, da.WOOD_BAND, da.WOOD_X1)
    da.frame_anchors(kit)
    kit.no_collider()
    kit.tag("interactable", "door", "door_frame", "lobby", "frame_wood", "double_acting")
    kit.meta["frameType"] = "wood"
    da.lod_meta(kit, sys.modules[__name__])
