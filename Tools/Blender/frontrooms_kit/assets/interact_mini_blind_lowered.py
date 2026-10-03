"""Lowered, half-tilted 1" aluminium mini-blind, decor only (interactables
10_spec.md §5.1, P2; 06_period_windows.md §3.2): for Kit_InteriorWindow
(G5), the Office's dark decor window. NEVER on a breakable map window: a
lowered blind hangs in the climb opening (§5.1 rule; use
Kit_MiniBlind_Raised there).

Real-world reference: as Kit_MiniBlind_Raised (Levolor Riviera guide, local
copy hd_alum.txt: 1" x 0.008" slats, 22 mm ladder spacing, 5 ladders for
80-5/8" to 114-1/2" wide, 1.4 mm cord, wand 30" for a 37-52" blind; Sears
1993 almond baked enamel). Generic, no brand.

Geometry (Unity metres, origin = top centre of the head rail on its centre
plane, the same convention as the raised blind's `blind_rail`; front = kit
-Y = Unity +Z; everything hangs below Y 0):
* 2.40 m wide head rail (35 x 35) in box brackets on spacer blocks (the
  placer sets the stand-off; spacer backs at Z -0.0475).
* 56 slats, 1" crowned (1.2 mm), 0.25 mm thick, real geometry, at 22 mm
  pitch, tilted 45 degrees front edge down, 1.30 m drop.
* 5 ladders: front and back cords, a rung under every slat; 3 lift cords
  through the stack beside ladders 2-4; bottom rail 38 x 29 with end caps.
* Tilt wand (30", 0.76 m) and lift cords with a tassel at the right end.

Budget (§9.4): 6,000 / 1,500 / 200; LOD1 now at 0.25 (ladders, rungs and
fittings drop; slats and rails collapse). LOD distances 3 / 10 / 30. Slots:
Prop_SteelAlmond (first), Prop_PlasticWhite. Render-only, no collider.
G5 also asks for Kit_InteriorWindow_Bronze; that is G5's file, not this one.
"""

import math

from mathutils import Matrix, Vector

import interact_window_common as wc
import interact_mini_blind_raised as rb

NAME = "Kit_MiniBlind_Lowered"
LOD1_RATIO = 0.25
LOD2_RATIO = 0.033
LOD1 = LOD1_RATIO
LOD_DISTANCES = (3.0, 10.0, 30.0)
SMOOTH_ANGLE = 40.0

ALMOND, WHITE = rb.ALMOND, rb.WHITE
UB = rb.UB                       # same local frame as the raised blind (virtual rail top at Y 2.195)
TOP = rb.O[1]
DROP = 1.30
HALF_W = 1.20
SLAT_HALF = 1.195
N_SLATS = 56
PITCH = 0.022
TILT = math.radians(45.0)        # front edge down
LADDERS = (-1.124, -0.562, 0.0, 0.562, 1.124)
LIFTS = (-0.542, 0.020, 0.582)
ZC = rb.O[2]                     # slat / rail centre plane (Z)
BOT_DY = (TOP - DROP) - rb.BOT_Y0  # bottom rail section moved down to the 1.30 drop


def slat_section(yc, segs=6, t=0.00025):
    """Closed (Z, Y) section of one crowned slat, tilted about its centre."""
    top, bot = [], []
    for j in range(segs + 1):
        u = -0.0125 + 0.025 * j / segs
        c = rb.CROWN * (1 - (u / 0.0125) ** 2)
        top.append((u, c + t / 2))
        bot.append((u, c - t / 2))
    pts = bot + list(reversed(top))
    ca, sa = math.cos(TILT), math.sin(TILT)
    # rotate (u, v) so +u (front, +Z) goes down: z = u cos - v sin ... front edge down
    return [(ZC + u * ca + v * sa, yc - u * sa + v * ca) for (u, v) in pts]


def edge_point(yc, u, below=0.0):
    """Window-virtual (Z, Y) of slat point u (front +0.0125, back -0.0125) on the
    underside, pushed ``below`` m further along the slat's down-normal."""
    ca, sa = math.cos(TILT), math.sin(TILT)
    v = rb.CROWN * (1 - (u / 0.0125) ** 2) - 0.000125 - below
    return (ZC + u * ca + v * sa, yc - u * sa + v * ca)


def build(kit):
    # ---- almond first
    rail = wc.extrude_x(kit, rb.rail_section(), -(HALF_W - 0.0025), HALF_W - 0.0025, ALMOND, name="head rail", mapf=UB)
    wc.paint_wear(rail, lambda X, Y, Z: (1.0, 1.0, 0.8))
    for s in (-1, 1):
        x = s * (HALF_W - 0.0125)
        wc.paint_wear(kit.box((0.025, 0.0395, 0.0395), UB(x, 2.17825, 0.12825), ALMOND, bevel=0.0007, segments=1, name="box bracket"))
        kn = kit.cylinder(0.0012, 0.025, UB(x, 2.1585, 0.1478), ALMOND, verts=10, rot=(0, 90, 0), bevel=0.0, name="bracket hinge")
        wc.paint_wear(kn)
        kit.lod1_drop(kn)
    y0 = rb.RAIL_Y0 - 0.012
    ycs = [y0 - PITCH * k for k in range(N_SLATS)]
    for yc in ycs:
        sl = wc.extrude_x(kit, slat_section(yc), -SLAT_HALF, SLAT_HALF, ALMOND, name="slat", mapf=UB)
        wc.paint_wear(sl, lambda X, Y, Z: (1.0, 1.0, 0.85))
    bot = wc.extrude_x(kit, [(z, y + BOT_DY) for (z, y) in rb.bottom_rail_section()], -SLAT_HALF, SLAT_HALF, ALMOND,
                       name="bottom rail", mapf=UB)
    wc.paint_wear(bot)
    by = (rb.BOT_Y0 + rb.BOT_Y1) / 2 + BOT_DY
    for s in (-1, 1):
        wc.paint_wear(kit.box((0.004, 0.0390, 0.0300), UB(s * (SLAT_HALF + 0.002), by, (rb.BOT_Z0 + rb.BOT_Z1) / 2), ALMOND,
                              bevel=0.0012, segments=1, name="bottom rail cap"))
    # ladders: front and back cords, one rung under each slat
    zf, yf0 = edge_point(ycs[0], 0.0125, 0.0007)
    zb, yb0 = edge_point(ycs[0], -0.0125, 0.0007)
    for x in LADDERS:
        for (z, ytop_off) in ((zf + 0.0006, yf0), (zb - 0.0006, yb0)):
            dy = ycs[0] - ycs[-1]
            c = kit.tube([UB(x, rb.RAIL_Y0 + 0.001, z), UB(x, ytop_off - dy - 0.004, z)], 0.00045, ALMOND, verts=4, name="ladder cord")
            wc.paint_wear(c)
            kit.lod1_drop(c)
            wc.lod2_drop(c)
        for yc in ycs:
            a = edge_point(yc, 0.0125, 0.0007)
            b = edge_point(yc, -0.0125, 0.0007)
            r = kit.tube([UB(x, a[1], a[0] + 0.0006), UB(x, b[1], b[0] - 0.0006)], 0.0004, ALMOND, verts=4, caps=False, name="ladder rung")
            kit.lod1_drop(r)
            wc.lod2_drop(r)
            wc.paint_wear(r)
    for x in LIFTS:
        c = kit.tube([UB(x, rb.RAIL_Y0 + 0.001, ZC), UB(x, rb.BOT_Y1 + BOT_DY - 0.002, ZC)], 0.0007, WHITE, verts=4, name="lift cord")
        wc.paint_wear(c)
        kit.lod1_drop(c)
        wc.lod2_drop(c)

    # ---- white plastic: spacers, wand, cords, tassel, sill guards
    for s in (-1, 1):
        wc.paint_wear(kit.box((0.025, 0.0285, 0.045), UB(s * (HALF_W - 0.0125), 2.1775, 0.09425), WHITE, bevel=0.001, segments=1, name="spacer block"))
    wx, wz = HALF_W - 0.08, rb.WAND_Z
    wc.paint_wear(kit.cylinder(0.0022, 0.012, UB(wx, 2.1585, 0.1465), WHITE, verts=10, bevel=0.0005, segments=1, name="tilter"))
    wc.paint_wear(kit.cylinder(0.0032, 0.016, UB(wx, 2.140, wz), WHITE, verts=8, bevel=0.0006, segments=1, name="wand head"))
    w_top, w_bot = 2.132, 2.132 - 0.762
    wc.paint_wear(kit.cylinder(0.0045, w_top - w_bot, UB(wx, (w_top + w_bot) / 2, wz), WHITE, verts=6, bevel=0.0005, segments=1,
                               name="tilt wand"), lambda X, Y, Z: (0.75 if Y < w_bot + 0.3 else 1.0, 1.0, 1.0))
    wc.paint_wear(kit.cylinder(0.0030, 0.012, UB(wx, w_bot - 0.006, wz), WHITE, verts=6, radius_top=0.0045, bevel=0.0008,
                               segments=1, name="wand tip"), lambda X, Y, Z: (0.6, 1.0, 1.0))
    cx, ty0 = HALF_W - 0.06, TOP - 0.62
    wc.paint_wear(kit.box((0.022, 0.012, 0.006), UB(cx, rb.RAIL_Y0 - 0.003, 0.142), ALMOND, bevel=0.001, segments=1, name="cord lock"))
    for dx in (-0.0016, 0.0016):
        c = kit.tube([UB(cx + dx, rb.RAIL_Y0 - 0.005, 0.1445), UB(cx + dx, rb.RAIL_Y0 - 0.020, rb.CORD_Z),
                      UB(cx + dx * 0.4, ty0 + 0.036, rb.CORD_Z)], 0.0007, WHITE, verts=5, name="lift cord")
        wc.paint_wear(c)
    prof = [(0.0010, 0.000), (0.0034, 0.0015), (0.0052, 0.0080), (0.0055, 0.0150), (0.0047, 0.0230),
            (0.0030, 0.0300), (0.0016, 0.0350), (0.0012, 0.0370)]
    wc.paint_wear(kit.lathe([(r, z + ty0 - TOP) for (r, z) in prof], UB(cx, TOP, rb.CORD_Z), WHITE, verts=10, name="tassel"),
                  lambda X, Y, Z: (0.65, 1.0, 1.0))
    for s in (-1, 1):
        g = kit.box((0.020, 0.030, 0.0030), UB(s * 0.95, rb.BOT_Y0 + BOT_DY - 0.0015, (rb.BOT_Z0 + rb.BOT_Z1) / 2), WHITE,
                    bevel=0.0008, segments=1, name="sill guard")
        wc.paint_wear(g)
        kit.lod1_drop(g)

    # ---- checks: drop, width, slat clearance to the bottom rail
    lo, hi = wc.unity_bounds(kit)
    assert abs((hi[1] - lo[1]) - DROP) < 0.003 and abs(hi[0] - HALF_W) < 0.001, (lo, hi)
    assert edge_point(ycs[-1], 0.0125)[1] > rb.BOT_Y1 + BOT_DY + 0.002, "last slat touches the bottom rail"
    kit.anchor("blind_rail", UB(*rb.O))
    kit.anchor("wand_tip", UB(wx, w_bot - 0.012, wz))
    kit.anchor("tassel", UB(cx, ty0, rb.CORD_Z))
    kit.anchor("bottom_rail", UB(0.0, rb.BOT_Y0 + BOT_DY, (rb.BOT_Z0 + rb.BOT_Z1) / 2))
    kit.meta["decorOnly"] = "Kit_InteriorWindow (G5); never on a breakable map window"
    wc.lod_meta(kit, LOD1_RATIO, LOD2_RATIO, LOD_DISTANCES)
    kit.no_collider()
    kit.tag("interactable", "window", "blind", "decor")
