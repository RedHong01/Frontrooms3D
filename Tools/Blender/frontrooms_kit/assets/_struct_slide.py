"""The slide start's kits (structures G2-b, line visual (game visual), 2026-11-08; package structures_slide_start, 30_plan.md P15's start
part; g2_review_assets.md S-7 to S-9; level_space 61 section 2.7, 63 section 6.2; LS114, LS115).

All in UNITY space (x, y, z; the front +Z), converted by _struct_common.ub(); hand-built LOD0 / LOD1 / LOD2 (B.levels), hero rule as
G1-b (curves at a 0.2 mm chord at LOD0, exposed edges bevelled at LOD0). No text, label, brand or logo. ASCII only.
  Kit_SlideTrafficLight  the start light (LS114, the UK code of 1990: a red and green light over each entry, "essential where staff
                         cannot see the run"; 63 6.2: black head, red and green glass lenses, a lit bulb, "incandescent, not LED
                         dots"): a black painted head 0.24 x 0.50 x 0.18 on a 40 mm post and plate, two 160 mm aspects on each face
                         (red over green) behind domed glass lenses in their retaining rings, under tunnel visors.
  Kit_SlideCrossbar      the stainless crossbar over the tub: a 1 1/4 in NPS tube (42.2 mm) 1.80 m between the tiled cheeks, in two
                         flanged escutcheons with three screws each.
  Kit_SlideChain         the chain across the tub's entry: a hook plate at each end (x +-0.60), 6 mm stainless oval links (34 x 22 mm,
                         alternate links turned) hanging 70 mm in the middle.
  Kit_GuardStool         the top guard's stool: a round vinyl seat at 0.76 m on a steel pan, four splayed 1 in tube legs with rubber
                         feet and a foot ring.
  Kit_RulesBoard         the blank rules board: a white enamelled panel 0.90 x 1.20 in an aluminium frame on four stand-offs (the
                         words are 叙事设计's, the faces 平面视觉's; blank until then).
"""

import math

import kitlib
import _run_b_common as B
import _struct_common as S

SS = "Prop_StainlessSatin"
BLACK = "Prop_SteelBlack"
LENS_R = "Prop_SlideLensRed"
LENS_G = "Prop_SlideLensGreen"
VINYL = "Prop_Vinyl"
RUBBER = "Prop_Rubber"
WHITE = "Prop_PlasticWhite"
ALU = "Trans_AluminiumSatin"

kitlib.register_slot(LENS_R, (0.55, 0.04, 0.03), 0.05, 0.0)
kitlib.register_slot(LENS_G, (0.05, 0.45, 0.20), 0.05, 0.0)

DIST_SMALL = (2.5, 9.0, 32.0)


# ----------------------------------------------------------------------------- the start light
HEAD_W, HEAD_H, HEAD_D = 0.24, 0.50, 0.18
POST_H = 0.09
LENS_RAD = 0.078


def _aspect(kit, yc, zface, sign, lens_slot, lv):
    """One aspect on the face at z = zface (sign +1 the front, -1 the back): the retaining ring, the domed lens, the tunnel visor."""
    # retaining ring: an annulus r 0.078..0.094, 9 mm proud, its outer edge rounded (a lathe about the Unity z axis)
    ring = [(0.094, 0.0), (0.094, 0.006), (0.091, 0.009), (0.081, 0.009), (0.078, 0.006)]
    if lv >= 1:
        ring = [(0.094, 0.0), (0.094, 0.009), (0.078, 0.009)]
    axis = "z" if sign > 0 else "-z"
    S.lathe_axis(kit, ring, (0.0, yc, zface), axis, BLACK, lv, name="lens ring", close_top=False, close_bottom=False)
    # the lens: a shallow glass dome, 6 mm proud at its rim, 15 mm at its centre (a signal roundel), its own decal UVs for the map
    n = 6 if lv == 0 else 3 if lv == 1 else 2
    prof = []
    for k in range(n + 1):
        r = LENS_RAD * (1.0 - k / float(n))
        h = 0.006 + 0.009 * (1.0 - (r / LENS_RAD) ** 2)
        prof.append((r if k < n else 0.0, h))
    rot = {"z": (90, 0, 0), "-z": (-90, 0, 0)}[axis]
    kit.lathe(prof, S.ub((0.0, yc, zface)), lens_slot, verts=S.segs(LENS_RAD, lv, 16), rot=rot, name="lens", uv="decal", close_bottom=False)
    if lv >= 2:
        return
    # the tunnel visor: 3 mm steel, r 0.097, 0.13 out from the face, open over its bottom quarter (a C section swept along z)
    m = S.Mesh()
    steps = 14 if lv == 0 else 7
    a0, a1 = math.radians(-45.0), math.radians(225.0)
    outer = [(0.097 * math.cos(a0 + (a1 - a0) * k / steps), 0.097 * math.sin(a0 + (a1 - a0) * k / steps)) for k in range(steps + 1)]
    inner = [(0.094 * math.cos(a1 - (a1 - a0) * k / steps), 0.094 * math.sin(a1 - (a1 - a0) * k / steps)) for k in range(steps + 1)]
    sec = outer + inner
    # the section in the frame (a = x across, b = y up) swept along the face's normal
    path = [(0.0, yc, zface), (0.0, yc, zface + sign * 0.13)]
    fr = [(S.Vector((0, 0, sign)), S.Vector((1, 0, 0)), S.Vector((0, 1, 0)))] * 2
    S.sweep_section(m, path, sec, closed_section=True, frames=fr, cap0=False, cap1=True, flip=sign < 0)
    m.part(kit, "visor", BLACK)


def traffic_light(kit, lv):
    """Kit_SlideTrafficLight: origin at the foot of its post (on the cheek's top), the head's faces +Z (front) and -Z (back)."""
    # the plate and the post
    S.box(kit, (0.10, 0.006, 0.10), (0.0, 0.003, 0.0), BLACK, lv, w=0.0012, name="base plate")
    S.box(kit, (0.04, POST_H - 0.006, 0.04), (0.0, 0.006 + (POST_H - 0.006) / 2, 0.0), BLACK, lv, w=0.0015, name="post")
    if lv < 2:
        for (x, z) in ((-0.035, -0.035), (0.035, -0.035), (-0.035, 0.035), (0.035, 0.035)):
            S.hex_nut(kit, 0.013, 0.006, (x, 0.006 + 0.003, z), "y", SS, lv, name="plate bolt")
    # the head: a painted steel box with eased edges, a seam round its middle (the two shells), a hinge each side
    y0 = POST_H
    S.box(kit, (HEAD_W, HEAD_H, HEAD_D), (0.0, y0 + HEAD_H / 2, 0.0), BLACK, lv, w=0.004, name="head")
    if lv == 0:
        S.box(kit, (HEAD_W + 0.004, 0.006, HEAD_D + 0.004), (0.0, y0 + HEAD_H / 2, 0.0), BLACK, lv, w=0.0008, name="seam band")
        S.box(kit, (HEAD_W + 0.006, 0.012, 0.03), (0.0, y0 + HEAD_H - 0.012, 0.0), BLACK, lv, w=0.001, name="lifting lug")
    for sign in (1.0, -1.0):
        zf = sign * HEAD_D / 2
        _aspect(kit, y0 + HEAD_H * 0.75, zf, sign, LENS_R, lv)
        _aspect(kit, y0 + HEAD_H * 0.25, zf, sign, LENS_G, lv)
        if lv == 0:
            for (x, y) in ((-0.10, y0 + 0.03), (0.10, y0 + 0.03), (-0.10, y0 + HEAD_H - 0.03), (0.10, y0 + HEAD_H - 0.03)):
                S.screw_head(kit, 0.005, (x, y, zf), "+z" if sign > 0 else "-z", SS, lv, name="face screw")


# ----------------------------------------------------------------------------- the crossbar
BAR_R = 0.0211
BAR_L = 1.80


def crossbar(kit, lv):
    """Kit_SlideCrossbar: origin at the bar's middle, along Unity X between the cheeks' faces at x +-0.90."""
    S.cyl_x(kit, BAR_R, BAR_L - 0.016, (0.0, 0.0, 0.0), SS, lv, w=0.0006, name="bar")
    for sx in (-1.0, 1.0):
        x = sx * BAR_L / 2
        # the escutcheon: a 72 mm flange 6 mm thick on the tile, a collar round the bar, eased edges
        prof = [(0.036, 0.0), (0.036, 0.0045), (0.034, 0.006), (0.026, 0.006), (0.0245, 0.0145), (BAR_R + 0.0004, 0.0145)]
        if lv >= 1:
            prof = [(0.036, 0.0), (0.036, 0.006), (0.025, 0.006), (0.025, 0.0145), (BAR_R, 0.0145)]
        S.lathe_axis(kit, prof, (x, 0.0, 0.0), "-x" if sx > 0 else "x", SS, lv, name="escutcheon", close_top=False, close_bottom=False)
        if lv < 2:
            for k in range(3):
                a = math.radians(90.0 + 120.0 * k)
                S.screw_head(kit, 0.0042, (x - sx * 0.006, 0.030 * math.sin(a), 0.030 * math.cos(a)), "-x" if sx > 0 else "+x", SS, lv, name="escutcheon screw")


# ----------------------------------------------------------------------------- the chain
CHAIN_HALF = 0.60
SAG = 0.07


def _catenary(x):
    """The chain's line: y 0 at the hooks (x +-0.58), down SAG in the middle (a parabola is within 1 mm of the catenary here)."""
    f = x / (CHAIN_HALF - 0.02)
    return -SAG * (1.0 - f * f)


def chain(kit, lv):
    """Kit_SlideChain: origin between the two hook plates (x +-0.60, plates on the faces there, normals +-X), the hooks at y 0."""
    for sx in (-1.0, 1.0):
        x = sx * CHAIN_HALF
        S.box(kit, (0.004, 0.08, 0.05), (x - sx * 0.002, 0.0, 0.0), SS, lv, w=0.0008, name="hook plate")
        if lv < 2:
            for y in (0.028, -0.028):
                S.screw_head(kit, 0.004, (x - sx * 0.004, y, 0.0), "-x" if sx > 0 else "+x", SS, lv, name="plate screw")
        # the hook: a 6 mm rod out of the plate, turned up at its end (an open J)
        pts = [(x - sx * 0.004, 0.0, 0.0), (x - sx * 0.022, 0.0, 0.0), (x - sx * 0.030, 0.006, 0.0), (x - sx * 0.032, 0.016, 0.0)]
        S.tube(kit, "hook", pts, 0.003, SS, lv, cap0=False, cap1=True, lo=6)
    if lv >= 2:
        pts = [(x, _catenary(x) - 0.004, 0.0) for x in [(-CHAIN_HALF + 0.035) + (2 * CHAIN_HALF - 0.07) * k / 16.0 for k in range(17)]]
        S.tube(kit, "chain", pts, 0.006, SS, lv, cap0=True, cap1=True, lo=6)
        return
    # oval links: wire r 2.5 mm, centre-line stadium 29 x 17 mm (inside 24 x 12), pitch 24 mm, alternate links turned 90 deg
    wire = 0.0025
    rc, half = 0.0085, 0.0145
    path_n = 10 if lv == 0 else 6
    wire_n = 5 if lv == 0 else 3
    x = -CHAIN_HALF + 0.035
    i = 0
    while x < CHAIN_HALF - 0.035:
        xm = x + 0.012
        t = S.Vector((1.0, (_catenary(xm + 0.001) - _catenary(xm - 0.001)) / 0.002, 0.0)).normalized()
        nrm = S.Vector((0.0, 0.0, 1.0)) if i % 2 == 0 else S.Vector((-t.y, t.x, 0.0)).normalized()
        side = t.cross(nrm).normalized()
        c = S.Vector((xm, _catenary(xm) - 0.004, 0.0))
        # the stadium round the link's centre in the plane (t, side)
        loop = []
        for k in range(path_n):
            a = 2.0 * math.pi * k / path_n
            ca, sa = math.cos(a), math.sin(a)
            along = (half - rc) * (1.0 if ca >= 0 else -1.0) + rc * ca
            loop.append(tuple(c + t * along + side * (rc * sa)))
        m = S.Mesh()
        S.sweep_section(m, loop + [loop[0]], S.circle(wire, wire_n), closed_section=True)
        m.part(kit, "link", SS)
        x += 0.024
        i += 1


# ----------------------------------------------------------------------------- the guard's stool
SEAT_Y = 0.76


def stool(kit, lv):
    """Kit_GuardStool: standing on y 0, seat top at 0.76."""
    # the seat: a vinyl cushion 360 mm round, 55 mm, its edge rolled, on a steel pan
    n = 8 if lv == 0 else 4 if lv == 1 else 2
    prof = [(0.168, SEAT_Y - 0.055), (0.180, SEAT_Y - 0.050)]
    for k in range(n + 1):
        a = math.pi / 2 * k / n
        prof.append((0.180 - 0.020 + 0.020 * math.cos(a), SEAT_Y - 0.020 + 0.020 * math.sin(a)))
    prof.append((0.0, SEAT_Y))
    S.lathe_y(kit, prof, (0.0, 0.0, 0.0), VINYL, lv, name="seat", close_top=False)
    S.lathe_y(kit, [(0.172, SEAT_Y - 0.068), (0.172, SEAT_Y - 0.056), (0.0, SEAT_Y - 0.056)], (0.0, 0.0, 0.0), SS, lv, name="seat pan", close_top=False)
    S.cyl_y(kit, 0.05, 0.012, (0.0, SEAT_Y - 0.074, 0.0), SS, lv, name="hub")
    # four legs of 1 in tube, splayed: under the pan at r 0.12, on the floor at r 0.20; rubber feet; a foot ring at 0.30
    leg_r = 0.0127
    for k in range(4):
        a = math.radians(45.0 + 90.0 * k)
        top = (0.12 * math.cos(a), SEAT_Y - 0.068, 0.12 * math.sin(a))
        bot = (0.20 * math.cos(a), 0.022, 0.20 * math.sin(a))
        S.tube(kit, "leg", [top, bot], leg_r, SS, lv, cap0=False, cap1=False, lo=8)
        S.cyl_y(kit, 0.016, 0.022, (bot[0], 0.011, bot[2]), RUBBER, lv, name="foot")
    ring_y = 0.30
    f = (SEAT_Y - 0.068 - ring_y) / (SEAT_Y - 0.068 - 0.022)
    ring_rad = 0.12 + (0.20 - 0.12) * f + 0.004
    steps = 24 if lv == 0 else 12 if lv == 1 else 8
    pts = [(ring_rad * math.cos(2 * math.pi * k / steps), ring_y, ring_rad * math.sin(2 * math.pi * k / steps)) for k in range(steps + 1)]
    S.tube(kit, "foot ring", pts, 0.0095, SS, lv, cap0=False, cap1=False, lo=8)


# ----------------------------------------------------------------------------- the rules board
BOARD_W, BOARD_H = 0.90, 1.20


def rules_board(kit, lv):
    """Kit_RulesBoard: its back on the wall (z 0), its bottom at y 0, the face +Z, 45 mm off the wall."""
    zf = 0.045
    # the panel: white enamelled steel, 1.2 mm on a 12 mm core
    S.box(kit, (BOARD_W - 0.04, BOARD_H - 0.04, 0.012), (0.0, BOARD_H / 2, zf - 0.006), WHITE, lv, w=0.0008, name="panel")
    # the frame: a 30 x 25 mm aluminium extrusion round it, mitred (four bars, eased)
    for (sx, sy, l, horiz) in ((0.0, 0.015, BOARD_W, True), (0.0, BOARD_H - 0.015, BOARD_W, True), (-BOARD_W / 2 + 0.015, BOARD_H / 2, BOARD_H - 0.06, False), (BOARD_W / 2 - 0.015, BOARD_H / 2, BOARD_H - 0.06, False)):
        size = (l, 0.030, 0.025) if horiz else (0.030, l, 0.025)
        S.box(kit, size, (sx, sy, zf - 0.0125 + 0.004), ALU, lv, w=0.002, name="frame")
    # four stand-offs to the wall
    if lv < 2:
        for (x, y) in ((-0.36, 0.10), (0.36, 0.10), (-0.36, BOARD_H - 0.10), (0.36, BOARD_H - 0.10)):
            S.cyl_z(kit, 0.010, zf - 0.012, (x, y, (zf - 0.012) / 2), ALU, lv, name="stand-off")
