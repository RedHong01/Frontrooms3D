"""Flush veneered wood door leaf, the FREE door of the Lobby (L0-F; VARIANT
Kit_DoorLeaf_Veneer_Oak with chrome hardware is the Office free door OF-F,
and the P2 Exit free door EX-F). 44 mm solid-core flush leaf with hardwood
edge bands, a 3 deg lock-edge bevel, a bored-latch faceplate and the door
halves of three 4-1/2" five-knuckle butts (10_spec §1.4, §2.1, §2.3).

Real-world reference: the US commercial 1-3/4" wood flush door, 1955-1990
(particleboard or staved core, sliced veneer faces, matching hardwood edge
bands, a 1/8" in 2" lock-edge bevel), on full-mortise standard-weight
butts, with a cylindrical (bored) passage latch behind a 1-1/8" x 2-1/4"
square-corner faceplate. Flush, not panelled: RE8's raised panels belong
to a château; the 1990 office door carries its relief in the edge bands,
the bevel, the mortised hinge leaves and the hardware (§2.1 "Why no
panelled doors"). No maker marks, no lever bore (the G2 rose covers it).

Origin: DOOR ROOT D, closed pose (§1.2). Spawn under the "Leaf rig" (child
of "Door hinge {a}-{b}" at (-S_sign 0.0295, 0, -0.0098), scale (S_sign, 1,
1)) at localPosition 0, label "Leaf (kit)"; the leaf rig rotates about the
A2 axis (0.0295, y, 0.0098) with the hinge. +X is the pull face S.

Envelope (Unity m, §1.4): X +-0.022; Y 0.015..2.095; hinge edge Z 0.005;
latch edge Z 0.995 (S) / 0.9927 (P). 1.5 mm two-segment arrises on every
face edge, a 3 mm radius on the latch-stile arrises between Y 0.85 and 1.20
(hand wear), 0.3 mm seam lines on both edge faces at X +-0.0185 (the edge
band's glue lines). The 0.05 x 2.08 x 0.98 gameplay collider sits inside
this envelope except its bottom 15 mm, hidden behind the saddle.

Budget (§9.1): 2,600 / 900 / 120 tris; slots Door_Veneer (submesh 0;
registered with kitlib.register_slot, mapped by the importer to
Resources/Surfaces/Door_Veneer.mat: metre UVs, grain vertical, fixing the
map cube's 2.08x stretch) and Prop_Brass. LOD1 0.40 keeps the slab
uncollapsed (fr_lod_keep), so the leaf outline never shrinks at LOD1;
screws drop at LOD1, hinge halves and the faceplate flagged for LOD2.
"""

import random
import sys

import kitlib
from mathutils import Vector

import interact_door_common as dc

NAME = "Kit_DoorLeaf_Veneer"
LOD1 = 0.40
LOD1_RATIO = 0.40
LOD2_RATIO = 0.05
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0

VENEER = "Door_Veneer"
BRASS = "Prop_Brass"
# Door_Veneer mean albedo sRGB (157, 109, 59) (10_spec §7.3), linear for the preview
kitlib.register_slot(VENEER, (0.337, 0.153, 0.044), 0.5, 0.0)
VARIANTS = {"Kit_DoorLeaf_Veneer_Oak": {VENEER: "Prop_WoodOak", BRASS: "Prop_Chrome"}}

SEAMS = (-0.0185, 0.0185)
WEAR_Y = (0.80, 0.85, 1.20, 1.25)        # 1.5 -> 3 mm latch arris and back
FACEPLATE = (0.0286, 0.0572, 0.0015)     # bored latch front, w x h x t
LATCH_OPENING = (0.011, 0.021)


def section(o=0.0, wear=False):
    return dc.leaf_section(inset=o, r_h=dc.ARRIS, r_l=0.003 if wear else dc.ARRIS, seams=SEAMS)


def wear(P, N, edge, obj):
    X, Y, Z = P
    r = g = b = 1.0
    nm = obj.name
    if "slab" in nm:
        # hand grime: both faces near the latch stile, lever height (push on P)
        hand = dc.smoothstep(0.62, 0.92, Z) * dc.smoothstep(0.80, 1.00, Y) * (1 - dc.smoothstep(1.30, 1.50, Y))
        r -= 0.40 * hand
        if edge:
            g -= 0.10 + 0.45 * hand + 0.35 * (1 - dc.smoothstep(0.05, 0.30, Y))   # kicked bottom arrises
        if abs(abs(X) - 0.0185) < 0.0004 and (Z < 0.012 or Z > 0.985):
            b -= 0.45                                                           # seam lines
    if obj.get("fr_screw"):
        b -= 0.35
    return r, g, b


def build(kit):
    rng = random.Random(41030)

    # 1. Slab (Door_Veneer first -> submesh 0): lofted section with the
    #    top/bottom arris rounds and the hand-wear transition on the latch stile.
    rings = dc.chamfer_rings(dc.LEAF_Y0, 1, lambda o: section(o))
    rings += [(WEAR_Y[0], section()), (WEAR_Y[1], section(wear=True)),
              (WEAR_Y[2], section(wear=True)), (WEAR_Y[3], section())]
    rings += list(reversed(dc.chamfer_rings(dc.LEAF_Y1, -1, lambda o: section(o))))
    slab = dc.loft_y(kit, rings, VENEER, "slab", grain="z")
    cuts = dc.leaf_mortise_cutters()

    # 2. Bored-latch faceplate, mortised flush on the bevelled edge.
    fw, fh, ft = FACEPLATE
    plate, mort = dc.edge_plate(kit, BRASS, 1.000, fw, fh, ft, "latch faceplate", bevel=0.0002)
    cuts.append(mort)
    dc.cut(slab, cuts)
    dc.cut(plate, [dc.edge_opening(1.000, *LATCH_OPENING)])
    R, n = dc.latch_frame()
    for dy in (-0.0215, 0.0215):
        c = Vector((0.0, 1.000 + dy, dc.LEAF_Z_LATCH_MID))
        s = dc.flat_head(kit, c, n, BRASS, rng, d=0.0076, host=plate, notch=0.0009, name="faceplate screw")
        kit.lod1_drop(s)
        dc.lod2_drop(s)
    dc.lod2_drop(plate)

    # 3. Door halves of the three butts.
    for y0, _ in dc.HINGES:
        for p in dc.hinge_half(kit, "leaf", y0, BRASS, rng):
            dc.lod2_drop(p)
            if p.get("fr_screw") or "web" in p.name:
                kit.lod1_drop(p)

    dc.finalize(kit, SMOOTH_ANGLE, wear, protect=[slab])

    # ---- checks ---------------------------------------------------------
    lo, hi = dc.assert_box([slab], (-dc.LEAF_X, dc.LEAF_Y0, dc.LEAF_Z_HINGE), (dc.LEAF_X, dc.LEAF_Y1, dc.LEAF_Z_LATCH_S), what="slab")
    assert abs(hi[0] - dc.LEAF_X) < 1e-4 and abs(lo[1] - dc.LEAF_Y0) < 1e-4 and abs(hi[1] - dc.LEAF_Y1) < 1e-4
    assert abs(hi[2] - dc.LEAF_Z_LATCH_S) < 2e-4, "latch edge S %.4f" % hi[2]
    blo, bhi = dc.bounds_unity(kit.parts)
    assert bhi[0] <= dc.AXIS_X + dc.KNUCKLE_R + 1e-4 and blo[0] >= -dc.LEAF_X - 1e-4, "nothing proud of the faces but the knuckles"

    # ---- metadata -------------------------------------------------------
    dc.anchor(kit, "rose_s", dc.LEAF_X, 1.000, 0.920)
    dc.anchor(kit, "rose_p", -dc.LEAF_X, 1.000, 0.920)
    dc.anchor(kit, "latchbolt", 0.0, 1.000, dc.LEAF_Z_LATCH_MID)
    dc.anchor(kit, "latchbolt_dir", 0.0, 1.000, dc.LEAF_Z_LATCH_MID + 0.10)
    dc.anchor(kit, "latch_edge_bottom", 0.0, dc.LEAF_Y0, dc.LEAF_Z_LATCH_MID)
    dc.anchor(kit, "latch_edge_top", 0.0, dc.LEAF_Y1, dc.LEAF_Z_LATCH_MID)
    dc.anchor(kit, "damage_latch", 0.0, 1.10, dc.LEAF_Z_LATCH_MID)
    dc.anchor(kit, "hinge_axis", dc.AXIS_X, 0.0, dc.AXIS_Z)
    dc.anchor(kit, "hinge_axis_dir", dc.AXIS_X, 0.10, dc.AXIS_Z)
    dc.anchor(kit, "closer_mount_s", dc.LEAF_X, 2.0275, 0.545)
    kit.no_collider()
    kit.tag("interactable", "door", "door_leaf", "lobby", "office", "door_type_wood")
    kit.meta["doorType"] = "wood"
    kit.meta["doorRoot"] = "D (closed pose): origin hinge-jamb edge, wall centre line, floor; leaf rig rotates about hinge_axis"
    dc.lod_meta(kit, sys.modules[__name__])
