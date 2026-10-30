"""Plastic-laminate DOUBLE-ACTING ward door leaf: the P2 Run free door
(RN-F) (D1.6, A12 / A19). The Kit_DoorLeaf_Veneer_DA mesh in wood-grain
plastic laminate (grain on texture U) with satin-stainless armor plates on
BOTH faces (Y 0.018..0.831; a both-ways ward door is pushed by carts from
either side), a bored-latch faceplate and chrome pivot plates. Q-RN default
(10_design §11): it takes the lever set (Red: "lever on free doors"), so it
has NO push plate and NO D-pull (the single-acting Kit_DoorLeaf_Ward's
push/pull hardware is a different, latchless door).

Real-world reference: 1970s-90s hospital ward / corridor double-acting door,
laminate faced, armor plates both faces (02 §5, 04 §3.2). No maker marks.

Origin, envelope, LODs: as Kit_DoorLeaf_Veneer_DA. Armor plates in every
LOD (screws at LOD0). Slots Prop_WoodLaminate (submesh 0), Prop_Aluminium,
Prop_Chrome.
"""

import random
import sys

import interact_door_common as dc
import interact_door_da_common as da
import interact_door_leaf_veneer_da as base

NAME = "Kit_DoorLeaf_Ward_DA"
LOD1_RATIO = 0.36
LOD2_RATIO = 0.05
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0

LAM = "Prop_WoodLaminate"
ALU = "Prop_Aluminium"
CHROME = "Prop_Chrome"
KT = 0.00127
ARMOR_Y = (0.018, 0.831)
ARMOR_Z = (0.030, 0.970)


def extra(kit, level, cuts, rng):
    y0, y1 = ARMOR_Y
    z0, z1 = ARMOR_Z
    for side, tag in ((1, "s"), (-1, "p")):
        x0, x1 = sorted((side * da.LEAF_X, side * (da.LEAF_X + KT)))
        p = dc.obox(kit, ALU, lo=(x0, y0, z0), hi=(x1, y1, z1), bevel=(0.0009 if level < 2 else 0.0), segs=1, name="armor plate " + tag)
        da.tag_lods(p, str(level))
        if level == 0:
            ym, zm = (y0 + y1) / 2, (z0 + z1) / 2
            for y, z in ((y0 + 0.015, z0 + 0.015), (y0 + 0.015, zm), (y0 + 0.015, z1 - 0.015), (ym, z0 + 0.015),
                         (ym, z1 - 0.015), (y1 - 0.015, z0 + 0.015), (y1 - 0.015, zm), (y1 - 0.015, z1 - 0.015)):
                s = dc.oval_head(kit, (side * (da.LEAF_X + KT), y, z), (side, 0, 0), ALU, rng, d=0.0068, half_segs=6, name="plate screw")
                da.tag_lods(s, "0")


def build(kit):
    rng = random.Random(41270)
    base.build_flush(kit, LAM, CHROME, rng, ugrain=True, extra=lambda k, lv, c: extra(k, lv, c, rng))
    da.leaf_anchors(kit, locked=False)
    for tag, sx in (("s", 1), ("p", -1)):
        dc.anchor(kit, "armor_" + tag, sx * 0.0226, (ARMOR_Y[0] + ARMOR_Y[1]) / 2, 0.500)
    kit.no_collider()
    kit.tag("interactable", "door", "door_leaf", "run", "door_type_wood", "double_acting")
    kit.meta["doorType"] = "wood"
    kit.meta["grainAxis"] = "texture U (Prop_WoodLaminate_A is U-grain; see interact_door_common.u_grain)"
    da.lod_meta(kit, sys.modules[__name__])
