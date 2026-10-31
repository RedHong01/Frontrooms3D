"""Flush veneered DOUBLE-ACTING door leaf, the Lobby FREE door (L0-F, D1.6
A12 / A19). 44 mm solid-core flush leaf with hardwood edge bands, its hinge
stile radiused about the pivot axis (a half-cylinder of R 0.022 that runs
22 mm into the jamb: the same shape at every angle), a flat symmetric latch
end at Z 0.995 (no 3 deg bevel: a DA leaf leads with either face), a
bored-latch faceplate for the double-bevel DA latch, and brass top-pivot
and bottom-arm plates flush in its top and foot. No butts, no closer.

Real-world reference: the US commercial 1-3/4" wood flush door, 1955-1990,
hung double-acting on centre-hung pivots (02 §5; 04 §3.2: "radius-cut pivot
edge"). Keeping a lever and a latch on a both-ways leaf is a game liberty
(Red, A12: handles kept; a real double-acting door "generally cannot carry a
traditional latching lockset", 04 §3.2). No maker marks.

Origin: DOOR ROOT D (closed pose) = the pivot axis. Spawn under "Leaf rig"
(child of "Door hinge {a}-{b}" at 0, scale 1: no A2 offset, no handing
mirror) at localPosition 0, label "Leaf (kit)". +X = face S.

Envelope (Unity m): X +-0.022; Y 0.015..2.095; hinge stile R 0.022 about
(0, 0) (Z >= -0.022); latch end Z 0.995. 1.5 mm two-segment arrises on the
latch end and the top and bottom, a 3 mm latch arris between Y 0.85 and 1.20
(hands push the leaf from both faces), 0.3 mm edge-band seams on the latch
end at X +-0.0185. Every vertex stays within r 0.9965 of the axis (the latch
lining is at 0.998). The 0.05 x 2.08 x 0.98 gameplay collider sits inside
this envelope except its bottom 15 mm (behind the saddle) and its 3 mm past
the leaf faces (render-only differences; the collider is the map's).

LODs (hand-built): LOD0 96-segment stile, wear rings, seams, faceplate and
screws, pivot plates; LOD1 24-segment stile, faceplate as a plain plate;
LOD2 8-segment stile, square latch corners. Slots: Door_Veneer (submesh 0),
Prop_Brass.
"""

import random
import sys

import kitlib
from mathutils import Vector

import interact_door_common as dc
import interact_door_da_common as da
import interact_door_lite_common as lite

NAME = "Kit_DoorLeaf_Veneer_DA"
LOD1_RATIO = 0.36
LOD2_RATIO = 0.05
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0

VENEER = "Door_Veneer"
BRASS = "Prop_Brass"
kitlib.register_slot(VENEER, (0.337, 0.153, 0.044), 0.5, 0.0)
kitlib.register_slot(lite.GLASS_SLOT, (0.80, 0.85, 0.83), 0.05, 0.0)

SEAMS = (-0.0185, 0.0185)
WEAR_Y = (0.80, 0.85, 1.20, 1.25)
FACEPLATE = (0.0286, 0.0572, 0.0015)     # bored latch front, w x h x t
LATCH_OPENING = (0.011, 0.021)           # the DA latch: X +-0.0055 (no plunger), Y +-0.0105


def build_flush(kit, leaf_slot, hw_slot, rng, ugrain=False, extra=None, faceplate=True):
    """The veneer-type DA leaf, every LOD level. ``extra(kit, level, cuts)``
    adds parts (and LOD0 slab cutters) per level: Kit_DoorLeaf_Ward_DA."""
    if ugrain:
        dc.u_grain(kit)
    slabs = []
    for level in (0, 1, 2):
        s = da.slab(kit, leaf_slot, level, seams=SEAMS, wear_y=WEAR_Y)
        if ugrain:
            s["fr_ugrain"] = True
        da.tag_lods(s, str(level))
        slabs.append(s)
        cuts = []
        if level == 0:
            cuts += da.pivot_plate_cutters()
            da.pivot_plates(kit, hw_slot, 0, rng)
        if faceplate:
            fw, fh, ft = FACEPLATE
            if level == 0:
                plate, mort = da.edge_plate(kit, hw_slot, da.LATCH_Y_BORED, fw, fh, ft, "latch faceplate", bevel=0.0002)
                cuts.append(mort)
                dc.cut(plate, [da.edge_opening(da.LATCH_Y_BORED, *LATCH_OPENING)])
                for dy in (-0.0215, 0.0215):
                    c = Vector((0.0, da.LATCH_Y_BORED + dy, da.LEAF_Z_LATCH))
                    sc = dc.flat_head(kit, c, (0, 0, 1), hw_slot, rng, d=0.0076, host=plate, notch=0.0009, name="faceplate screw")
                    da.tag_lods(sc, "0")
                da.tag_lods(plate, "0")
            elif level == 1:
                # LOD1: the plate as a 0.2 mm proud skin (no mortise, no opening): a brass rectangle on the edge
                plate = dc.obox(kit, hw_slot, lo=(-fw / 2, da.LATCH_Y_BORED - fh / 2, da.LEAF_Z_LATCH - 0.0002),
                                hi=(fw / 2, da.LATCH_Y_BORED + fh / 2, da.LEAF_Z_LATCH + 0.0002), name="latch faceplate lod1")
                da.tag_lods(plate, "1")
        if extra is not None:
            extra(kit, level, cuts)
        if cuts:
            dc.cut(s, cuts)
    dc.finalize(kit, SMOOTH_ANGLE, da.wear_leaf, protect=slabs)
    da.assert_leaf(kit, slabs[0], kit.name)
    return slabs


def build(kit):
    rng = random.Random(41230)
    # door vision (2026-10-30, research/door_vision/30_design.md 1): F1 10 x 30, walnut bead in the leaf's own slot
    build_flush(kit, VENEER, BRASS, rng, extra=lambda k, lv, c: lite.add_lite(k, lv, c, VENEER, "wood"))
    lite.lite_meta(kit, "wood")
    kit.meta["liteAsserts"] = lite.assert_lite(kit, "wood")
    da.leaf_anchors(kit, locked=False)
    kit.no_collider()
    kit.tag("interactable", "door", "door_leaf", "lobby", "office", "door_type_wood", "double_acting")
    kit.meta["doorType"] = "wood"
    da.lod_meta(kit, sys.modules[__name__])
