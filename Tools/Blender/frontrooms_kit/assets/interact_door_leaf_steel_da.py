"""Flush hollow-metal DOUBLE-ACTING door leaf in almond enamel: the LOCKED
member of every level (L0-K, OF-K, and the P2 Run / Exit locked doors)
(D1.6, A12 / A19). The hinge stile radiused about the pivot axis (R 0.022),
a flat symmetric latch end at Z 0.995 with its lock seam, faint top and
bottom channel lines, satin-stainless kick plates on BOTH faces (symmetric
now: no stops to clear), a mortise-lock armor front with the latch and
deadbolt openings on the flat end, and satin-chrome pivot plates. Keyed knob
and deadbolt on both faces are Red's game liberty (A12: handles kept; the
keyhole at Y 1.000 on both faces).

Real-world reference: the US "storeroom door", SDI 108 Level 2 hollow-metal
flush leaf, 1-3/4", 1955-1990, hung double-acting on centre-hung pivots
(02 §5; 04 §3.2); .050" satin stainless kick plates both faces; an 8" armor
front. Value rule (05 §1): the almond leaf (luminance ~0.56) in a dark-bronze
frame reads as LOCKED from 6-20 m. No maker marks.

Origin: DOOR ROOT D (closed pose) = the pivot axis; spawn under "Leaf rig"
at 0, label "Leaf (kit)". +X = face S. Envelope: X +-0.022; Y 0.015..2.095;
stile R 0.022 (Z >= -0.022); latch end 0.995; kick plates Y 0.018..0.272,
1.27 mm proud, Z 0.030..0.970 on both faces; armor front 0.032 x 0.2032 x
0.003 centred Y 0.968 on the flat end; openings for the DA latchbolt (Y
0.9365) and the deadbolt (Y 1.000).

LODs (hand-built): LOD0 96-segment stile, seams, channel lines, kick plates
and screws, armor front and screws, pivot plates; LOD1 24-segment stile,
kick plates and armor as plain plates; LOD2 8-segment stile and the kick
plates. Slots: Door_Enamel (submesh 0; Resources/Surfaces/Door_Enamel.mat,
D1.6 §9), Prop_Aluminium (kick plates), Prop_Chrome. VARIANT
Kit_DoorLeaf_Steel_PaintedMetal_DA maps the enamel to Painted_Metal.
"""

import random
import sys

import kitlib
from mathutils import Vector

import interact_door_common as dc
import interact_door_da_common as da

NAME = "Kit_DoorLeaf_Steel_DA"
LOD1_RATIO = 0.40
LOD2_RATIO = 0.04
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0

ENAMEL = "Door_Enamel"
ALU = "Prop_Aluminium"
CHROME = "Prop_Chrome"
kitlib.register_slot(ENAMEL, (0.610, 0.558, 0.434), 0.5, 0.0)
kitlib.register_slot("Painted_Metal", (0.644, 0.610, 0.509), 0.5, 0.0)
VARIANTS = {"Kit_DoorLeaf_Steel_PaintedMetal_DA": {ENAMEL: "Painted_Metal"}}

KICK_Y = (0.018, 0.272)
KICK_T = 0.00127
KICK_Z = (0.030, 0.970)
ARMOR = (0.032, 0.2032, 0.003, 0.968)
BOLT_OPENING = (0.0135, 0.031)
CHANNEL = (0.025, 2.085)


def channel_rings(sec):
    out = []
    w, d = 0.0005, 0.0003
    for yc in CHANNEL:
        out += [(yc - w, sec(0.0)), (yc - w + 0.0002, sec(d)), (yc + w - 0.0002, sec(d)), (yc + w, sec(0.0))]
    return out


def wear(P, N, edge, obj):
    r, g, b = da.wear_leaf(P, N, edge, obj)
    X, Y, Z = P
    nm = obj.name
    if "slab" in nm:
        g -= 0.35 * dc.smoothstep(0.26, 0.28, Y) * (1 - dc.smoothstep(0.33, 0.42, Y))   # scuffs to primer above the kick plates
        if (abs(X) < 0.0006 and Z > 0.99) or any(abs(Y - c) < 0.0008 for c in CHANNEL):
            b -= 0.45
    if "kick" in nm:
        g -= 0.25 if edge else 0.0
        r -= 0.15 * (1 - dc.smoothstep(0.03, 0.15, Y))
    return r, g, b


def build_steel(kit, rng, extra=None):
    """The DA hollow-metal leaf, every LOD level. ``extra(kit, level, cuts)``
    adds parts per level (Kit_DoorLeaf_SteelLite_DA)."""
    slabs = []
    for level in (0, 1, 2):
        lv = str(level)
        s = da.slab(kit, ENAMEL, level, seams=(0.0,), extra_rings=channel_rings, grain=None)
        da.tag_lods(s, lv)
        slabs.append(s)
        cuts = []
        # kick plates, both faces
        for side in (1, -1):
            z0, z1 = KICK_Z
            x0, x1 = side * da.LEAF_X, side * (da.LEAF_X + KICK_T)
            kp = dc.obox(kit, ALU, lo=(min(x0, x1), KICK_Y[0], z0), hi=(max(x0, x1), KICK_Y[1], z1),
                         bevel=(0.0009 if level < 2 else 0.0), segs=1, name="kick plate %s" % ("s" if side > 0 else "p"))   # fix 1: LOD1 keeps the bevel (the 4 m pop)
            da.tag_lods(kp, lv)
            if level == 0:
                for z in (z0 + 0.015, (z0 + z1) / 2, z1 - 0.015):
                    for y in (KICK_Y[0] + 0.015, KICK_Y[1] - 0.015):
                        sc = dc.oval_head(kit, (x1, y, z), (side, 0, 0), ALU, rng, d=0.0068, name="kick screw")
                        da.tag_lods(sc, "0")
        aw, ah, at, ay = ARMOR
        if level == 0:
            cuts += da.pivot_plate_cutters()
            da.pivot_plates(kit, CHROME, 0, rng)
            armor, mort = da.edge_plate(kit, CHROME, ay, aw, ah, at, "armor front", bevel=0.0004)
            cuts.append(mort)
            dc.cut(armor, [da.edge_opening(da.LATCH_Y_MORTISE, *BOLT_OPENING), da.edge_opening(da.DEADBOLT_Y, *BOLT_OPENING)])
            for dy in (-0.088, 0.088):
                sc = dc.flat_head(kit, Vector((0.0, ay + dy, da.LEAF_Z_LATCH)), (0, 0, 1), CHROME, rng, d=0.0076, host=armor,
                                  notch=0.0012, name="armor screw")
                da.tag_lods(sc, "0")
            da.tag_lods(armor, "0")
        elif level == 1:
            armor = dc.obox(kit, CHROME, lo=(-aw / 2, ay - ah / 2, da.LEAF_Z_LATCH - 0.0002), hi=(aw / 2, ay + ah / 2, da.LEAF_Z_LATCH + 0.0002),
                            name="armor front lod1")
            da.tag_lods(armor, "1")
        if extra is not None:
            extra(kit, level, cuts)
        if cuts:
            dc.cut(s, cuts)
    dc.finalize(kit, SMOOTH_ANGLE, wear, protect=slabs)
    da.assert_leaf(kit, slabs[0], kit.name)
    return slabs


def meta(kit, module, extra_tags=()):
    da.leaf_anchors(kit, locked=True)
    kit.no_collider()
    kit.tag("interactable", "door", "door_leaf", "lobby", "office", "door_type_hollow_metal", "locked", "double_acting", *extra_tags)
    kit.meta["doorType"] = "hollow_metal"
    da.lod_meta(kit, module)


def build(kit):
    rng = random.Random(41250)
    build_steel(kit, rng)
    meta(kit, sys.modules[__name__])
