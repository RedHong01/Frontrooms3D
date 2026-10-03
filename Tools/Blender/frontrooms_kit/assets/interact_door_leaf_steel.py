"""Flush hollow-metal door leaf in almond enamel: the LOCKED member of every
level (L0-K, OF-K, and the P2 Run / Exit locked doors). Square edges with
1.5 mm bevels and a vertical edge seam, the 3 deg lock-edge bevel, faint
top and bottom channel lines, satin-stainless kick plates on both faces, a
mortise-lock armor front with latch and deadbolt openings, and the door
halves of three satin-chrome 4-1/2" butts (10_spec §1.4, §2.1, §2.3).

Real-world reference: the US "storeroom door", SDI 108 Level 2 (18 ga)
hollow-metal flush leaf, 1-3/4", 1955-1990: lock-seam (visible seam) edges
with the lock edge bevelled 1/8" in 2", inverted top and bottom channels,
semi-gloss almond enamel; .050" satin stainless kick plates 10" high (push
face door width less 2", pull face less 1-1/2", #6 oval-head screws,
Rockwood K1050 type); a mortise lockset's 8" armor front (25 mm deadbolt +
deadlocking latch, G2). Value rule (05 §1): an almond leaf (luminance ~0.56)
in a dark-bronze frame reads as LOCKED from 6-20 m against the free door's
warm wood. No maker marks.

Origin: DOOR ROOT D, closed pose (§1.2): spawn under "Leaf rig" at 0, label
"Leaf (kit)"; the rig turns about the A2 axis (0.0295, y, 0.0098). +X = S.

Envelope (Unity m): X +-0.022; Y 0.015..2.095; hinge edge Z 0.005; latch
edge Z 0.995 (S) / 0.9927 (P); 0.5 mm seam groove at X 0 on both edges;
1 mm recessed channel lines 10 mm in from the top and bottom. Kick plates
Y 0.018..0.272, 1.27 mm proud: S Z 0.024..0.976, P Z 0.0305..0.9695 (clear
of the push-side stops). Armor front 0.032 x 0.2032 x 0.003 centred Y 0.968
on the bevel; openings for the latchbolt (Y 0.9365) and deadbolt (Y 1.000).
The cylinder and knob bores are under G2's escutcheon (not modelled).

Budget (§9.1): 4,200 / 1,500 / 150 tris; slots Door_Enamel (submesh 0; new
surface, NEEDS APPROVAL P-4; until Resources/Surfaces/Door_Enamel.mat exists
Unity falls back to the FBX's own almond material), Prop_Aluminium (kick
plates), Prop_Chrome (armor front, hinge halves, screws). VARIANT
Kit_DoorLeaf_Steel_PaintedMetal maps the enamel to the existing
Painted_Metal surface (the spec's named fallback, luminance 0.61). LOD1 0.40
keeps the slab and kick plates whole (fr_lod_keep); screws drop at LOD1.
"""

import random
import sys

import kitlib
from mathutils import Vector

import interact_door_common as dc

NAME = "Kit_DoorLeaf_Steel"
LOD1 = 0.40
LOD1_RATIO = 0.40
LOD2_RATIO = 0.04
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0

ENAMEL = "Door_Enamel"
ALU = "Prop_Aluminium"
CHROME = "Prop_Chrome"
# sRGB (205, 197, 176) almond enamel (05 §6.2) -> linear preview colour
kitlib.register_slot(ENAMEL, (0.610, 0.558, 0.434), 0.5, 0.0)
kitlib.register_slot("Painted_Metal", (0.644, 0.610, 0.509), 0.5, 0.0)
VARIANTS = {"Kit_DoorLeaf_Steel_PaintedMetal": {ENAMEL: "Painted_Metal"}}

KICK_Y = (0.018, 0.272)
KICK_T = 0.00127
KICK_Z = {1: (0.024, 0.976), -1: (0.0305, 0.9695)}   # S (pull) / P (push)
ARMOR = (0.032, 0.2032, 0.003, 0.968)                 # w, h, t, centre Y
BOLT_OPENING = (0.0135, 0.031)
LATCH_Y, DEADBOLT_Y = 0.9365, 1.000
CHANNEL = (0.025, 2.085)                              # recessed lines, 10 mm in


def section(o=0.0):
    return dc.leaf_section(inset=o, r_h=dc.ARRIS, r_l=dc.ARRIS, seams=(0.0,), seam_w=0.0005, seam_d=0.0005)


def channel_rings(yc):
    w, d = 0.0005, 0.0003
    return [(yc - w, section()), (yc - w + 0.0002, section(d)), (yc + w - 0.0002, section(d)), (yc + w, section())]


def wear(P, N, edge, obj):
    X, Y, Z = P
    r = g = b = 1.0
    nm = obj.name
    if "slab" in nm:
        hand = dc.smoothstep(0.66, 0.90, Z) * dc.smoothstep(0.82, 0.98, Y) * (1 - dc.smoothstep(1.22, 1.40, Y))
        r -= 0.40 * hand
        # scuffs to grey primer just above the kick plates (02 §2.1)
        g -= 0.35 * dc.smoothstep(0.26, 0.28, Y) * (1 - dc.smoothstep(0.33, 0.42, Y))
        if edge:
            g -= 0.10 + 0.40 * hand + 0.25 * (Z > 0.98)
        if (abs(X) < 0.0006 and (Z < 0.007 or Z > 0.99)) or any(abs(Y - c) < 0.0008 for c in CHANNEL):
            b -= 0.45
    if "kick" in nm:
        g -= 0.25 if edge else 0.0
        r -= 0.15 * (1 - dc.smoothstep(0.03, 0.15, Y))
    if obj.get("fr_screw"):
        b -= 0.35
    return r, g, b


def build(kit):
    rng = random.Random(41040)

    # 1. Slab (enamel first -> submesh 0).
    rings = dc.chamfer_rings(dc.LEAF_Y0, 1, section)
    rings += channel_rings(CHANNEL[0]) + channel_rings(CHANNEL[1])
    rings += list(reversed(dc.chamfer_rings(dc.LEAF_Y1, -1, section)))
    slab = dc.loft_y(kit, rings, ENAMEL, "slab")
    cuts = dc.leaf_mortise_cutters()

    # 2. Kick plates, both faces, 45 deg bevelled edges, six oval-head screws each.
    kicks = []
    for side in (1, -1):
        z0, z1 = KICK_Z[side]
        x0 = side * dc.LEAF_X
        x1 = side * (dc.LEAF_X + KICK_T)
        kp = dc.obox(kit, ALU, lo=(min(x0, x1), KICK_Y[0], z0), hi=(max(x0, x1), KICK_Y[1], z1),
                     bevel=0.0009, segs=1, name="kick plate %s" % ("s" if side > 0 else "p"))
        kicks.append(kp)
        for z in (z0 + 0.015, (z0 + z1) / 2, z1 - 0.015):
            for y in (KICK_Y[0] + 0.015, KICK_Y[1] - 0.015):
                s = dc.oval_head(kit, (x1, y, z), (side, 0, 0), ALU, rng, d=0.0068, name="kick screw")
                kit.lod1_drop(s)
                dc.lod2_drop(s)

    # 3. Mortise-lock armor front on the bevelled edge, two bolt openings.
    aw, ah, at, ay = ARMOR
    armor, mort = dc.edge_plate(kit, CHROME, ay, aw, ah, at, "armor front", bevel=0.0004)
    cuts.append(mort)
    dc.cut(slab, cuts)
    dc.cut(armor, [dc.edge_opening(LATCH_Y, *BOLT_OPENING), dc.edge_opening(DEADBOLT_Y, *BOLT_OPENING)])
    R, n = dc.latch_frame()
    for dy in (-0.088, 0.088):
        s = dc.flat_head(kit, Vector((0.0, ay + dy, dc.LEAF_Z_LATCH_MID)), n, CHROME, rng, d=0.0076, host=armor,
                         notch=0.0012, name="armor screw")
        kit.lod1_drop(s)
        dc.lod2_drop(s)
    dc.lod2_drop(armor)

    # 4. Door halves of the three butts (satin chrome).
    for y0, _ in dc.HINGES:
        for p in dc.hinge_half(kit, "leaf", y0, CHROME, rng):
            dc.lod2_drop(p)
            if p.get("fr_screw") or "web" in p.name:
                kit.lod1_drop(p)

    dc.finalize(kit, SMOOTH_ANGLE, wear, protect=[slab] + kicks)

    # ---- checks ---------------------------------------------------------
    lo, hi = dc.assert_box([slab], (-dc.LEAF_X, dc.LEAF_Y0, dc.LEAF_Z_HINGE), (dc.LEAF_X, dc.LEAF_Y1, dc.LEAF_Z_LATCH_S), what="slab")
    assert abs(hi[0] - dc.LEAF_X) < 1e-4 and abs(lo[1] - dc.LEAF_Y0) < 1e-4 and abs(hi[1] - dc.LEAF_Y1) < 1e-4
    for side, kp in zip((1, -1), kicks):
        klo, khi = dc.bounds_unity([kp])
        assert abs(klo[1] - KICK_Y[0]) < 1e-4 and abs(khi[1] - KICK_Y[1]) < 1e-4
        # push-face plate must clear the stops (Z 0.018 / 0.982) with the leaf shut
        if side < 0:
            assert klo[2] > dc.STOP_Z_H + 0.01 and khi[2] < dc.STOP_Z_L - 0.01
    blo, bhi = dc.bounds_unity(kit.parts)
    assert bhi[0] <= dc.AXIS_X + dc.KNUCKLE_R + 1e-4 and blo[0] >= -(dc.LEAF_X + KICK_T + 0.0015), "proud limit"

    # ---- metadata -------------------------------------------------------
    for tag, sx in (("s", 1), ("p", -1)):
        dc.anchor(kit, "escutcheon_" + tag, sx * dc.LEAF_X, 0.968, 0.920)
        dc.anchor(kit, "sign_" + tag, sx * dc.LEAF_X, 1.524, 0.500)
        dc.anchor(kit, "tagplate_" + tag, sx * dc.LEAF_X, 1.000, 0.790)
        dc.anchor(kit, "kick_" + tag, sx * 0.0226, 0.145, 0.500)
    dc.anchor(kit, "latchbolt", 0.0, LATCH_Y, dc.LEAF_Z_LATCH_MID)
    dc.anchor(kit, "latchbolt_dir", 0.0, LATCH_Y, dc.LEAF_Z_LATCH_MID + 0.10)
    dc.anchor(kit, "deadbolt", 0.0, DEADBOLT_Y, dc.LEAF_Z_LATCH_MID)
    dc.anchor(kit, "deadbolt_dir", 0.0, DEADBOLT_Y, dc.LEAF_Z_LATCH_MID + 0.10)
    dc.anchor(kit, "latch_edge_bottom", 0.0, dc.LEAF_Y0, dc.LEAF_Z_LATCH_MID)
    dc.anchor(kit, "latch_edge_top", 0.0, dc.LEAF_Y1, dc.LEAF_Z_LATCH_MID)
    dc.anchor(kit, "damage_latch", 0.0, 1.10, dc.LEAF_Z_LATCH_MID)
    dc.anchor(kit, "hinge_axis", dc.AXIS_X, 0.0, dc.AXIS_Z)
    dc.anchor(kit, "hinge_axis_dir", dc.AXIS_X, 0.10, dc.AXIS_Z)
    dc.anchor(kit, "closer_mount_s", dc.LEAF_X, 2.0275, 0.545)
    kit.no_collider()
    kit.tag("interactable", "door", "door_leaf", "lobby", "office", "door_type_hollow_metal", "locked")
    kit.meta["doorType"] = "hollow_metal"
    kit.meta["doorRoot"] = "D (closed pose): origin hinge-jamb edge, wall centre line, floor; leaf rig rotates about hinge_axis"
    dc.lod_meta(kit, sys.modules[__name__])
