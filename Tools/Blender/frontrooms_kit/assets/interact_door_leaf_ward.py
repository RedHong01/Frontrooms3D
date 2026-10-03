"""Hospital ward door leaf for the Run level's FREE door (RN-F, P2): the
44 mm flush leaf in wood-grain plastic laminate, push/pull with no latch:
a satin-stainless armor plate and a rounded push plate on the push face P,
a kick plate and an offset D-pull on the pull face S, three satin-chrome
butts (10_spec §2.3 "Kit_DoorLeaf_Ward").

Real-world reference: the 1970s-90s US hospital corridor door: a
plastic-laminate-faced flush leaf on a closer (the Run member has one),
push/pull hardware with no latch (04 §5.3), a 34"-class stainless armor
plate against carts and beds, a 4" x 16" push plate, a 1" round offset pull
on 10" centres with 2-1/2" projection. No maker marks, no signage.

Origin: DOOR ROOT D, closed pose (§1.2), exactly as Kit_DoorLeaf_Veneer:
spawn under "Leaf rig" at 0, label "Leaf (kit)". +X = the pull face S.

Geometry (Unity m): the veneer leaf's slab and hinge halves (shared code)
without the latch faceplate. P face: armor plate Y 0.018..0.831, Z
0.0305..0.9695 (a 32" plate: the spec's 34" top, 0.880, would run under
the push plate, which starts at 0.850), 1.27 mm, eight oval-head screws;
push plate 0.102 x 0.406 x 0.0013, R 6 mm corners, Y 0.850..1.256, Z
0.820..0.922, four screws. S face: kick plate Y 0.018..0.272, Z
0.024..0.976, six screws; D-pull Ø 0.025 on 0.254 centres at Y 1.000, Z
0.920, projection 0.064 (inside the 0.065 proud rule), on Ø 0.030 roses.

Budget (§9.1): 5,000 / 1,800 / 180 tris; slots Prop_WoodLaminate (submesh
0), Prop_Aluminium, Prop_Chrome. LOD1 0.40 (slab and plates protected,
screws dropped). Render-only.
"""

import math
import random
import sys

from mathutils import Vector

import interact_door_common as dc
import interact_door_leaf_veneer as base

NAME = "Kit_DoorLeaf_Ward"
LOD1 = 0.40
LOD1_RATIO = 0.40
LOD2_RATIO = 0.04
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0

LAM = "Prop_WoodLaminate"
ALU = "Prop_Aluminium"
CHROME = "Prop_Chrome"
KT = 0.00127
ARMOR = (0.018, 0.831, 0.0305, 0.9695)        # Y0, Y1, Z0, Z1 on P
KICK = (0.018, 0.272, 0.024, 0.976)           # on S
PUSH = (0.850, 1.256, 0.820, 0.922, 0.0013)   # on P
PULL_Y, PULL_Z, PULL_CC, PULL_D, PULL_PROJ = 1.000, 0.920, 0.254, 0.025, 0.064


def wear(P, N, edge, obj):
    X, Y, Z = P
    r, g, b = base.wear(P, N, edge, obj)
    nm = obj.name
    if "push plate" in nm:
        r -= 0.35 * (1 - min(1.0, math.hypot((Y - 1.10) / 0.12, (Z - 0.87) / 0.04)))
    if "pull" in nm:
        r -= 0.30 * (1 - dc.smoothstep(0.03, 0.10, abs(Y - PULL_Y)))
    if ("armor" in nm or "kick" in nm) and edge:
        g -= 0.3
    return r, g, b


def plate(kit, side, y0, y1, z0, z1, t, name, radius=0.0):
    x0 = side * dc.LEAF_X
    x1 = side * (dc.LEAF_X + t)
    if radius <= 0:
        return dc.obox(kit, ALU, lo=(min(x0, x1), y0, z0), hi=(max(x0, x1), y1, z1), bevel=0.0009, segs=1, name=name)
    rr = dc.rounded_rect(y1 - y0, z1 - z0, radius, 4, cx=(y0 + y1) / 2, cy=(z0 + z1) / 2)
    inset = dc.rounded_rect(y1 - y0 - 0.0016, z1 - z0 - 0.0016, radius - 0.0008, 4, cx=(y0 + y1) / 2, cy=(z0 + z1) / 2)
    return dc.loft_x(kit, [(x0 - side * 0.0002, rr), (x0 + side * (t - 0.0006), rr), (x1, inset)], ALU, name)


def screws_on(kit, side, pts, rng, slot=ALU, d=0.0068):
    out = []
    for y, z in pts:
        s = dc.oval_head(kit, (side * (dc.LEAF_X + KT), y, z), (side, 0, 0), slot, rng, d=d, half_segs=6, name="plate screw")
        kit.lod1_drop(s)
        dc.lod2_drop(s)
        out.append(s)
    return out


def extra(kit, slab, cuts, rng):
    y0, y1, z0, z1 = ARMOR
    armor = plate(kit, -1, y0, y1, z0, z1, KT, "armor plate p")
    ym, zm = (y0 + y1) / 2, (z0 + z1) / 2
    screws_on(kit, -1, [(y0 + 0.015, z0 + 0.015), (y0 + 0.015, zm), (y0 + 0.015, z1 - 0.015), (ym, z0 + 0.015),
                        (ym, z1 - 0.015), (y1 - 0.015, z0 + 0.015), (y1 - 0.015, zm), (y1 - 0.015, z1 - 0.015)], rng)
    ky0, ky1, kz0, kz1 = KICK
    kick = plate(kit, 1, ky0, ky1, kz0, kz1, KT, "kick plate s")
    screws_on(kit, 1, [(ky0 + 0.015, kz0 + 0.015), (ky0 + 0.015, (kz0 + kz1) / 2), (ky0 + 0.015, kz1 - 0.015),
                       (ky1 - 0.015, kz0 + 0.015), (ky1 - 0.015, (kz0 + kz1) / 2), (ky1 - 0.015, kz1 - 0.015)], rng)
    py0, py1, pz0, pz1, pt = PUSH
    push = plate(kit, -1, py0, py1, pz0, pz1, pt, "push plate p", radius=0.006)
    screws_on(kit, -1, [(py0 + 0.012, pz0 + 0.012), (py0 + 0.012, pz1 - 0.012), (py1 - 0.012, pz0 + 0.012),
                        (py1 - 0.012, pz1 - 0.012)], rng, slot=ALU, d=0.0060)
    # D-pull: one tube along a D path with 20 mm bends, on two roses
    xc = dc.LEAF_X + PULL_PROJ - PULL_D / 2
    rb = 0.020
    ya, yb = PULL_Y - PULL_CC / 2, PULL_Y + PULL_CC / 2
    path = [(dc.LEAF_X - 0.001, ya, PULL_Z), (xc - rb, ya, PULL_Z)]
    for k in range(1, 6):
        a = math.radians(-90 + 90 * k / 5)
        path.append((xc - rb + rb * math.cos(a), ya + rb + rb * math.sin(a), PULL_Z))
    path.append((xc, yb - rb, PULL_Z))
    for k in range(1, 6):
        a = math.radians(0 + 90 * k / 5)
        path.append((xc - rb + rb * math.cos(a), yb - rb + rb * math.sin(a), PULL_Z))
    path.append((dc.LEAF_X - 0.001, yb, PULL_Z))
    sec = [(PULL_D / 2 * math.cos(2 * math.pi * i / 24), PULL_D / 2 * math.sin(2 * math.pi * i / 24)) for i in range(24)]
    pull = dc.sweep_path(kit, path, sec, CHROME, "pull bar", up=(0, 0, 1))
    for y in (ya, yb):
        dc.revolve(kit, [(0.0150, -0.0005), (0.0150, 0.0018), (0.0138, 0.0030), (0.0, 0.0030)],
                   (dc.LEAF_X, y, PULL_Z), (1, 0, 0), 32, CHROME, "pull rose", close_start=False)
    return [armor, kick, push]


def build(kit):
    rng = random.Random(41090)
    base.build_leaf(kit, LAM, CHROME, rng, faceplate=False, extra=lambda k, s, c: extra(k, s, c, rng), wear_fn=wear)
    blo, bhi = dc.bounds_unity(kit.parts)
    assert bhi[0] <= dc.LEAF_X + PULL_PROJ + 1e-4, "pull projection %.4f" % (bhi[0] - dc.LEAF_X)
    assert blo[0] >= -(dc.LEAF_X + KT + 0.0015), "push face hardware stays flat"
    assert ARMOR[1] < PUSH[0], "armor plate must stop below the push plate"

    for tag, sx in (("s", 1), ("p", -1)):
        dc.anchor(kit, "kick_" + tag, sx * 0.0226, 0.145, 0.500)
    dc.anchor(kit, "pull_s", dc.LEAF_X, PULL_Y, PULL_Z)
    dc.anchor(kit, "push_p", -dc.LEAF_X, (PUSH[0] + PUSH[1]) / 2, (PUSH[2] + PUSH[3]) / 2)
    dc.anchor(kit, "armor_p", -0.0226, (ARMOR[0] + ARMOR[1]) / 2, 0.500)
    dc.anchor(kit, "latch_edge_bottom", 0.0, dc.LEAF_Y0, dc.LEAF_Z_LATCH_MID)
    dc.anchor(kit, "latch_edge_top", 0.0, dc.LEAF_Y1, dc.LEAF_Z_LATCH_MID)
    dc.anchor(kit, "damage_latch", 0.0, 1.10, dc.LEAF_Z_LATCH_MID)
    dc.anchor(kit, "hinge_axis", dc.AXIS_X, 0.0, dc.AXIS_Z)
    dc.anchor(kit, "hinge_axis_dir", dc.AXIS_X, 0.10, dc.AXIS_Z)
    dc.anchor(kit, "closer_mount_s", dc.LEAF_X, 2.0275, 0.545)
    kit.no_collider()
    kit.tag("interactable", "door", "door_leaf", "run", "door_type_wood", "push_pull")
    kit.meta["doorType"] = "wood"
    kit.meta["latch"] = "none (push/pull; no Unlatch beat)"
    dc.lod_meta(kit, sys.modules[__name__])
