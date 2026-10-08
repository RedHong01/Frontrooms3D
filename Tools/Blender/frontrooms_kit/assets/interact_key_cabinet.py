"""Wall key cabinet: a painted-steel numbered-hook cabinet with its door
swung back flat to the wall (spec 10 §4.2; 03 §2.3 A; 02 §9).

Real-world reference: the institutional key cabinet (Telkee-type, sold for
decades; no brand here): a 1 mm sheet-steel box 0.36 x 0.46 x 0.08 with a
rolled 9 mm front lip, a light hook panel inside with 4 rows x 6 plated wire
hooks on formed hook strips, a pan door 0.36 x 0.46 x 0.019 on a full-height
piano hinge, a cam lock with an interchangeable-core figure-8 face (the same
language as the doors, era note R10), a typed number label beside every
hook (01-24, read left to right, top row first, as on a numbered hook bar),
and a 3" x 5" index card in a formed holder on the inside of the door.

Pose: the door is hinged on the LEFT as seen from the room (Unity +X) and
swung past 180 deg until its cam lock rests on the wall (about 188 deg from
shut; the spec's "about 175 deg, flat to the wall": a door hinged at the
front of a 0.08 deep box must pass 180 deg to lie back toward the wall): the inside of the door,
with the card and the back of the cam lock, faces the room and nothing
stands more than 0.083 off the wall. The lock face looks at the wall.

Origin = THE WALL FACE PLANE AT FLOOR LEVEL, CENTRED on the open assembly
(§4.2): the body is centred at X -0.1794 and the door reaches X +0.359.
Body Y 1.24 - 1.70. Front +Z = the room. Anchors hook_r{0..3}_c{0..5} (r0
top row, c0 the left column as seen from the room), key_hook = hook_r1_c4,
door_hinge (+ door_hinge_dir, the hinge axis), body_centre.

Budget §9.3: 7,000 / 2,400 / 250 tris, LOD 3 / 8 / 40 m; 0.72 m wide;
LOD1 exported since the 2026-10-08 fix pass (critic H4); LOD1 keeps the LOD0 shape (lod1_keep_all). Slots (dominant first): Prop_SteelAlmond (body, door,
strips), Prop_PlasticWhite (hook panel), Prop_Chrome (hooks, lock, screws),
Prop_KeyTagNo (the 24 hook labels, cells 01-24, and the index card, which
maps to a blank paper band of the atlas between rows 0 and 1 so it stays
blank; NEW slot, fallback Prop_Paper - that keeps the asset at 4 slots).
Tags interactable, key_host, wall_decor.
"""

import math

import interact_door_common as dc
import interact_key_common as kc
from interact_key_common import U

NAME = "Kit_KeyCabinet"
LOD1_RATIO, LOD2_RATIO = 2400 / 7000, 250 / 7000
LOD1 = LOD1_RATIO  # fix pass 2026-10-08 (critic H4): FrontRoomsKitImporter honours the sidecar distances since 663e858, so LOD1 = LOD0->LOD1 at d01 and a cull at dcull
LOD_DISTANCES = (3.0, 8.0, 40.0)
BUDGET = (7000, 2400, 250)

STEEL, PANEL, CHROME = "Prop_SteelAlmond", "Prop_PlasticWhite", "Prop_Chrome"
PAPER = kc.KEYTAGNO                 # labels + index card (fallback Prop_Paper)
LABEL = (0.011, 0.006)              # typed hook label, beside each hook on the strip
LABEL_DX = 0.0092                   # label centre, toward the viewer's right (Unity -X) of the hook axis
LABEL_CROP = (0.568, 0.31)          # 4.6 mm digits on a 6 mm label
CARD_UV = (0.020, 0.866, 0.1336, 0.934)   # blank paper band between atlas rows 0 and 1 (digits span 0.38-0.62 of a cell)
BW, BH, BD = 0.36, 0.46, 0.080
Y0, Y1 = 1.24, 1.70
YC = (Y0 + Y1) / 2
XB = -0.1794                       # body centre (assembly centred on X = 0)
SHEET = 0.001
LIP = 0.009
ROWS = (1.640, 1.535, 1.430, 1.325)
COLS = (0.130, 0.078, 0.026, -0.026, -0.078, -0.130)   # left to right as seen from the room
STRIP_Z = 0.0065
HOOK_WIRE = 0.00125
DOOR_T = 0.019
HINGE = (XB + BW / 2 + 0.002, BD + 0.001)               # axis (X, Z)


LOCK_S = BW + 0.002 - 0.022 + 0.001       # cam lock axis, door-local s


def door_angle():
    """Opening angle (deg) at which the door first touches the wall: the free
    edge's outer corner or the cam lock's bezel and core face, whichever
    meets the wall face (Z 0) first, plus 0.3 mm of air."""
    ax, az = HINGE
    pts = [(BW + 0.002, DOOR_T), (LOCK_S + 0.0098, DOOR_T + 0.0012), (LOCK_S + 0.0072, DOOR_T + 0.0030),
           (LOCK_S + 0.0042 + 0.0033, DOOR_T + 0.0034)]
    lo, hi = 180.0, 200.0
    for _ in range(60):
        mid = (lo + hi) / 2
        psi = math.radians(mid)
        z = min(az + s * math.sin(psi) + t * math.cos(psi) for s, t in pts)
        if z > 0.0003:
            lo = mid
        else:
            hi = mid
    return (lo + hi) / 2


PSI = door_angle()


def door_to3(u, v, w):
    """Door-local (s along the door from the hinge, Y, t through the door
    toward its outer face) -> Blender, at the open angle PSI."""
    psi = math.radians(PSI)
    ex, ez = -math.cos(psi), math.sin(psi)
    nx, nz = math.sin(psi), math.cos(psi)
    ax, az = HINGE
    return U(ax + u * ex + w * nx, v, az + u * ez + w * nz)


def door_point(s, y, t):
    b = door_to3(s, y, t)
    return (-b[0], b[2], -b[1])          # back to Unity


def build(kit):
    dc.lod1_keep_all(kit)   # fix pass 2026-10-08: LOD1 keeps the LOD0 shape (collapse broke it; see dc.lod1_keep_all)
    kc.register_slots()
    # ---------------------------------------------------------- body shell
    r = 0.0015
    prof = [(0.0, 0.0), (0.0, BD - r)]
    for i in range(1, 3):
        a = math.radians(90 * i / 2)
        prof.append((r * (1 - math.cos(a)), BD - r + r * math.sin(a)))
    prof += [(LIP - 0.0005, BD), (LIP, BD - 0.0005), (LIP - 0.0005, BD - SHEET), (0.002, BD - SHEET),
             (SHEET, BD - 0.002), (SHEET, SHEET)]
    # rrect_sweep: the 9 mm lip reaches past the 2 mm corner radius (exact offset corners, no fold-over)
    shell = kc.rrect_sweep(kit, BW, BH, 0.002, 2, XB, YC, prof, STEEL, lambda u, v, w: U(u, v, w), name="body shell")
    back = kit.box(kc.U_size(BW - 0.004, BH - 0.004, SHEET), U(XB, YC, SHEET / 2), STEEL, bevel=0, name="body back")
    # hook strips (formed steel) - still the dominant slot
    strips = []
    for y in ROWS:
        strips.append(kit.box(kc.U_size(BW - 0.03, 0.014, 0.0015), U(XB, y + 0.002, STRIP_Z - 0.00075), STEEL,
                              bevel=0.0004, segments=1, name="hook strip"))
    # door pan (SteelAlmond, part of the dominant slot)
    rd = 0.0015
    dprof = [(0.003, DOOR_T)]
    for i in range(0, 3):
        a = math.radians(90 * i / 2)
        dprof.append((rd * (1 - math.sin(a)), DOOR_T - rd + rd * math.cos(a)))
    dprof += [(0.0, 0.0), (0.0015, -0.001), (0.008, -0.001), (0.0085, -0.0005), (0.008, 0.0),
              (0.002, 0.0), (SHEET, 0.001), (SHEET, DOOR_T - 0.002), (0.0025, DOOR_T - SHEET)]
    dprof_clean = []
    for p in dprof:
        if not dprof_clean or (abs(p[0] - dprof_clean[-1][0]) > 1e-7 or abs(p[1] - dprof_clean[-1][1]) > 1e-7):
            dprof_clean.append(p)
    door = kc.rrect_sweep(kit, BW, BH, 0.002, 2, (BW + 0.002) / 2 + 0.001, YC, dprof_clean, STEEL, door_to3,
                          name="door pan", cap_first=True, cap_last=True)
    # card holder frame (formed steel) on the inside of the door
    hs, hy = (BW + 0.002) / 2 + 0.001, 1.600
    holder = kc.rrect_sweep(kit, 0.137, 0.085, 0.002, 2, hs, hy, [(0.0, DOOR_T - SHEET), (0.0, DOOR_T - SHEET - 0.0025), (0.002, DOOR_T - SHEET - 0.003),
                                           (0.0055, DOOR_T - SHEET - 0.003), (0.0055, DOOR_T - SHEET - 0.0012)],
                              STEEL, door_to3, name="card holder")
    # piano hinge: barrel + the two leaves (painted with the body)
    hx, hz = HINGE
    barrel = kit.cylinder(0.0028, BH - 0.004, (0, 0, 0), STEEL, verts=10, bevel=0.0005, segments=1, name="hinge barrel")
    kc.orient(barrel, (hx, YC, hz), (0, 1, 0), (0, 0, 1))
    leaf_b = kit.box(kc.U_size(0.0012, BH - 0.004, 0.012), U(hx - 0.0025, YC, hz - 0.006), STEEL, bevel=0, name="hinge leaf body")
    # ------------------------------------------------------------ hook panel
    panel = kit.box(kc.U_size(BW - 2 * SHEET - 0.002, BH - 2 * SHEET - 0.002, 0.004), U(XB, YC, SHEET + 0.002), PANEL,
                    bevel=0.0006, segments=1, name="hook panel")
    # ------------------------------------------------------------ chrome
    hooks = {}
    hook_objs = []
    for ri, y in enumerate(ROWS):
        for ci, x in enumerate(COLS):
            objs, hp = kc.cup_hook(kit, (XB + x, y - HOOK_WIRE, STRIP_Z), CHROME, wire_r=HOOK_WIRE, leg=0.0165,
                                   bend_r=0.0038, rise=0.0042, shoulder_d=0.0042, shoulder_t=0.0009, verts=6,
                                   name="hook r%d c%d" % (ri, ci), rest_dz=-0.0011,
                                   shoulder_verts=8, rivet=True)
            hooks[(ri, ci)] = hp
            hook_objs += objs
    for o in hook_objs:
        if "shoulder" in o.name:
            kc.drop2(o)
    # typed number labels beside the hooks (01-24, top row first, left to right from the room)
    labels = []
    lw, lh = LABEL
    zl = STRIP_Z + 0.00015
    for (ri, ci), hp in sorted(hooks.items()):
        n = ri * len(COLS) + ci + 1
        lx, ly = XB + COLS[ci] - LABEL_DX, ROWS[ri] - HOOK_WIRE
        u0, v0, u1, v1 = kc.number_cell_rect(n, LABEL_CROP[0], LABEL_CROP[1])
        q = kc.quad_uv(kit, [(lx + lw / 2, ly - lh / 2, zl), (lx - lw / 2, ly - lh / 2, zl),
                             (lx - lw / 2, ly + lh / 2, zl), (lx + lw / 2, ly + lh / 2, zl)],
                       [(u0, v0), (u1, v0), (u1, v1), (u0, v1)], kc.KEYTAGNO, name="hook label")
        labels.append(q)
    # cam lock through the door, 22 mm in from the free edge, mid height
    ls, ly = LOCK_S, YC
    out_axis = door_point(ls, ly, 1.0)
    base = door_point(ls, ly, 0.0)
    n_out = tuple(out_axis[i] - base[i] for i in range(3))
    n_in = tuple(-c for c in n_out)
    # outer face (toward the wall): bezel collar and the figure-8 core face
    bez = kit.lathe([(0.0098, 0.0), (0.0098, 0.0012), (0.0088, 0.0026), (0.0072, 0.0030), (0.0, 0.0030)], (0, 0, 0), CHROME,
                    verts=20, name="cam lock bezel")
    kc.orient(bez, door_point(ls, ly, DOOR_T), n_out, (0, 1, 0))
    for dy, name in ((0.0, "core lobe low"), (0.0055, "core lobe high")):
        lobe = kit.cylinder(0.0042, 0.0006, (0, 0, 0), CHROME, verts=14, bevel=0.0, name=name)
        kc.orient(lobe, door_point(ls, ly + dy - 0.0022, DOOR_T + 0.0031), n_out, (0, 1, 0))
        kc.drop2(lobe)
    # inside (toward the room): barrel, hex nut, cam bar standing vertical (unlocked)
    bar = kit.cylinder(0.0078, 0.014, (0, 0, 0), CHROME, verts=16, bevel=0.0004, segments=1, name="cam lock barrel")
    kc.orient(bar, door_point(ls, ly, DOOR_T - SHEET - 0.007), n_in, (0, 1, 0))
    nut = kit.cylinder(0.0115, 0.0035, (0, 0, 0), CHROME, verts=6, bevel=0.0004, segments=1, name="cam lock nut")
    kc.orient(nut, door_point(ls, ly, DOOR_T - SHEET - 0.002), n_in, (0, 1, 0))
    cam = kit.box((0.010, 0.030, 0.0022), (0, 0, 0), CHROME, bevel=0.0008, segments=1, name="cam")
    kc.orient(cam, door_point(ls, ly + 0.009, DOOR_T - SHEET - 0.0145), n_in, (0, 1, 0))
    # two mounting screws inside the body (through the hook panel)
    for sx in (-BW / 2 + 0.03, BW / 2 - 0.03):
        sc = kc.oval_screw(kit, (XB + sx, Y1 - 0.025, SHEET + 0.004), CHROME, head_d=0.0072, dome_h=0.0014, slot_w=0.0008,
                           slot_d=0.0008, slot_deg=15 if sx < 0 else 70, segs=10, name="mount screw")
        kc.drop2(sc)
    # ------------------------------------------------------------ paper
    card = kc.quad_uv(kit, [door_point(hs + 0.0635, hy - 0.038, DOOR_T - SHEET - 0.0021),
                            door_point(hs - 0.0635, hy - 0.038, DOOR_T - SHEET - 0.0021),
                            door_point(hs - 0.0635, hy + 0.038, DOOR_T - SHEET - 0.0021),
                            door_point(hs + 0.0635, hy + 0.038, DOOR_T - SHEET - 0.0021)],
                      [(CARD_UV[0], CARD_UV[1]), (CARD_UV[2], CARD_UV[1]), (CARD_UV[2], CARD_UV[3]), (CARD_UV[0], CARD_UV[3])],
                      PAPER, name="index card")
    # ------------------------------------------------------------ wear
    def door_grime(p):
        X = -p.x
        g = 0.80 if X > XB + BW / 2 + 0.25 else 1.0
        return (g, 1.0, 1.0, 1.0)
    kc.paint_wear(door, door_grime)
    kc.wear_all(kit)

    # The card quad must face the room: flip it if it was wound the other way.
    from mathutils import Vector
    me = card.data
    n = me.polygons[0].normal
    if Vector(n).y > 0:      # Blender -y = room
        me.flip_normals()

    for (ri, ci), hp in hooks.items():
        kit.anchor("hook_r%d_c%d" % (ri, ci), U(*hp))
    kit.anchor("key_hook", U(*hooks[(1, 4)]))
    kit.anchor("door_hinge", U(hx, Y0, hz))
    kit.anchor("door_hinge_dir", U(hx, Y0 + 0.10, hz))
    kit.anchor("body_centre", U(XB, YC, BD / 2))
    # Hung pose recipe (solved by the G3 self-check with BVH overlap tests on
    # the real meshes; the facade applies it, spec 10 §4.1 "hung"): the ring's
    # hook_contact goes ringLiftM above key_hook, the ring turned
    # Euler(0, -60, 0) from the host (its tangent 60 deg toward the room); the
    # key hangs tip down from key_contact with its flat normal (+X) square to
    # the room (30 deg of twist in its Ø 4.8 hole) and the tag hangs from
    # tag_contact face to the room (30 deg in its Ø 5 hole on the 2.4 mm tab).
    # Fix pass 2026-10-08 (critic C2): tag on the WALL side of the ring so the brass key faces the room. The ring turns
    # 180 deg about the vertical (same plane; the hook still passes through it) and the tag turns back to face the room.
    # Re-checked with BVH on the real meshes, every host x every tag: 0 overlaps (int_work/fix/hung_pose.json).
    kit.meta["hungPose"] = {"ringYawDeg": 120.0, "ringLiftM": 0.0011, "keyTwistDeg": -30.0, "tagTwistDeg": 150.0, "tagSide": "wall (behind the key)",
                            "key": "LookRotation(down, Cross(down, hostForward)): tip down, flats to the room",
                            "tag": "LookRotation(hostForward, up): face to the room, hanging along -Y"}
    kit.no_collider()
    kit.tag("interactable", "key_host", "wall_decor")
    kc.lod_meta(kit, LOD_DISTANCES, BUDGET)
    kit.meta["keyHostDefault"] = "hook_r1_c4"
    kit.meta["doorOpenDeg"] = round(PSI, 2)
    kit.meta["numberLabels"] = {"slot": kc.KEYTAGNO, "fallback": kc.KEYTAGNO_FALLBACK, "cells": list(range(1, 25)),
                                "order": "hook_r{r}_c{c} -> r * 6 + c + 1", "indexCardUV": list(CARD_UV)}

    lo, hi = kc.eval_bounds_unity(kit)
    assert abs(lo[1] - Y0) < 2e-4 and abs(hi[1] - Y1) < 2e-4, ("heights", lo, hi)
    assert hi[2] <= 0.10 and lo[2] > -1e-4, ("proud", lo, hi)
    assert abs(lo[0] + hi[0]) < 0.006, ("centred", lo, hi)
    for hp in hooks.values():
        assert 1.30 <= hp[1] <= 1.66 and hp[2] < BD, hp
    # labels sit on the strip face, clear of the hook collar (R 2.1 mm) and inside the 14 mm strip
    assert LABEL_DX - LABEL[0] / 2 > 0.0021 + 0.0005, "label clear of the hook collar"
    assert -HOOK_WIRE - LABEL[1] / 2 >= 0.002 - 0.007 + 0.0003 and -HOOK_WIRE + LABEL[1] / 2 <= 0.002 + 0.007 - 0.0003, "label on the strip"
    assert COLS[-1] - LABEL_DX - LABEL[0] / 2 > -(BW - 0.03) / 2, "last label on the strip"
    # the card UV band holds no digits: atlas rows 0/1 ink spans cell v 0.38-0.62
    assert CARD_UV[1] > 0.85 + 0.012 and CARD_UV[3] < 0.95 - 0.012
    kc.check_budget(kit, BUDGET[0])
