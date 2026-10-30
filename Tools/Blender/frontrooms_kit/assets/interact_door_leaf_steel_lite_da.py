"""Hollow-metal DOUBLE-ACTING leaf with a narrow vision lite: the P2 Run
locked door if Red picks call 9 (RN-K option) (D1.6, A12 / A19). The
Kit_DoorLeaf_Steel_DA leaf plus a 4" x 25" lite (0.102 x 0.635, centred Y
1.575, Z 0.755) in a pressed glazing kit on both faces, 6 mm wired-free
clear glass, six oval-head screws on face S. As Kit_DoorLeaf_SteelLite, on
the DA leaf (radius stile, flat latch end, both-face kick plates).

Real-world reference: SDI hollow-metal door with a narrow lite kit, 1955-1990
(02 §6.1); hospital and service corridors. No maker marks.

Origin, envelope, LODs: as Kit_DoorLeaf_Steel_DA. The lite is in every LOD
(the glass and the kit's outer frame; the screws drop at LOD1). Slots
Door_Enamel (submesh 0), Prop_Aluminium, Prop_Chrome, Prop_Glass.
"""

import random
import sys

import interact_door_common as dc
import interact_door_da_common as da
import interact_door_leaf_steel_da as base
import interact_door_leaf_steel_lite as lite

NAME = "Kit_DoorLeaf_SteelLite_DA"
LOD1_RATIO = 0.40
LOD2_RATIO = 0.04
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0


def extra(kit, level, cuts, rng):
    z0, z1 = lite.LITE_Z - lite.LITE_W / 2, lite.LITE_Z + lite.LITE_W / 2
    y0, y1 = lite.LITE_Y - lite.LITE_H / 2, lite.LITE_Y + lite.LITE_H / 2
    P = lite.POCKET
    if level == 0:
        cuts.append(dc.cutter((-0.03, y0 - P, z0 - P), (0.03, y1 + P, z1 + P)))
        glass = dc.obox(kit, lite.GLASS, lo=(-0.003, y0 - P + 0.001, z0 - P + 0.001), hi=(0.003, y1 + P - 0.001, z1 + P - 0.001), name="lite glass")
        da.tag_lods(glass, "012")
        for side, tag in ((1, "s"), (-1, "p")):
            k = lite.rect_sweep_yz(kit, lite.kit_section(side), z0, z1, y0, y1, base.ENAMEL, "lite kit " + tag)
            da.tag_lods(k, "01")
        for y, z in ((y0 - 0.012, lite.LITE_Z), (y1 + 0.012, lite.LITE_Z), (y0 + 0.12, z0 - 0.012), (y1 - 0.12, z0 - 0.012),
                     (y0 + 0.12, z1 + 0.012), (y1 - 0.12, z1 + 0.012)):
            s = dc.oval_head(kit, (0.0245, y, z), (1, 0, 0), base.CHROME, rng, d=0.0055, half_segs=6, dome_k=0.16, name="lite screw")
            da.tag_lods(s, "0")
    else:
        # LOD1 / LOD2 slabs are solid: the kit's outer frame (LOD1, shared) or a dark glass skin (LOD2) on each face
        if level == 2:
            for side in (1, -1):
                x0, x1 = sorted((side * da.LEAF_X, side * (da.LEAF_X + 0.0003)))
                g = dc.obox(kit, lite.GLASS, lo=(x0, y0, z0), hi=(x1, y1, z1), name="lite skin lod2")
                da.tag_lods(g, "2")
        else:
            cutter = dc.cutter((-0.03, y0 - P, z0 - P), (0.03, y1 + P, z1 + P))
            cuts.append(cutter)


def build(kit):
    rng = random.Random(41260)
    base.build_steel(kit, rng, extra=lambda k, lv, c: extra(k, lv, c, rng))
    trim = 0.0285
    assert lite.LITE_Z - lite.LITE_W / 2 - trim > 0.500 + 0.127 + 0.02, "lite kit clear of the sign plate"
    assert lite.LITE_Y - lite.LITE_H / 2 - trim > 0.968 + 0.1016 + 0.05, "lite kit clear of the escutcheon"
    base.meta(kit, sys.modules[__name__], extra_tags=("lite", "run"))
    dc.anchor(kit, "lite_centre", 0.0, lite.LITE_Y, lite.LITE_Z)
    kit.meta["glassSlab"] = [lite.LITE_W + 2 * lite.POCKET - 0.002, lite.LITE_H + 2 * lite.POCKET - 0.002, 0.006]
    kit.meta["gameplayNote"] = "see-through lite; the Relay's sight stops at the leaf collider (02 6.1)"
