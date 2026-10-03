"""Raised 1" aluminium mini-blind for the office face of an Office window
(interactables 10_spec.md §5.1, 06_period_windows.md §2.5, §3.2): the most
"1990" thing a window can carry. Optional; the map places it on face A of
Office windows by edge hash. Always RAISED: a lowered blind would hang in the
breakable opening (§5.1 rule).

Real-world reference (06 §2.5 [read]): mini-blinds were 70-80 % of US window
coverings by 1981; the 1993 Sears catalogue sells 1" aluminium mini-blinds
with a "baked-on enamel finish", a "steel headrail" and a "bottom rail [with]
plastic sill guards", in Almond among other colours. Levolor Riviera product
guide (modern catalogue, timeless form; local copy hd_alum.txt): 1" slat,
0.008" gauge; headrail 1.375" x 1.375"; bottom rail 1-1/2" x 1-1/8"; cord
1.4 mm; ladder spacing 22 mm; 4 ladders for 60-1/8" to 84" wide; box
brackets with spacer blocks for outside mounts; stack 5" for a 72" blind.
Generic: no brand, no label, no valance.

Placement (window root W, Unity metres; origin = the frame's `blind_rail`
anchor (0, 2.195, 0.1275) = top centre of the head rail, front = face A =
kit -Y = Unity +Z):
* Head rail 35 x 35 x 1.545, Y 2.160-2.195, Z 0.110-0.145, its ends in two
  box brackets (X +/-0.750-0.775) on plastic spacer blocks that stand it off
  the wall (Z 0.080 -> 0.1085) clear of the frame's head band (Z 0.105, top
  Y 2.075).
* Raised stack Y 2.035-2.160: 12 grouped slabs of 7 nested crowned slats
  (1.2 mm crown), each with real slat noses and gaps on its front face and a
  few tenths of a millimetre of scatter, so it reads as stacked slats, not a
  block. Ladder cords loop out of the stack front at the 4 ladders.
* Bottom rail 38 x 29, Y 2.006-2.035, plastic end caps, 2 sill guards.
  Lowest point over the opening: Y 2.003 (>= 2.000, self-check e).
* Tilt wand: 9 mm hex, X +0.72, from the tilter hook to Y 1.14. Lift cords
  (1.4 mm, two) from the cord lock at X +0.74 to a tassel ending at Y 1.20.
  Both hang beside the opening (|X| >= 0.715 > 0.70) in front of the jamb
  face band (Z 0.152 / 0.150 vs 0.105).

Budget (§9.4): 2,400 / 800 / 120 tris; LOD1 now at 0.33 (1.55 m wide): the
slat slabs, ladder cords and small fittings drop and a plain stack block
(hidden inside the slabs at LOD0) takes over; LOD2 also drops the cords.
LOD distances 3 / 10 / 30. Slots: Prop_SteelAlmond (first), Prop_PlasticWhite
(wand, cords, tassel, spacers, sill guards). Render-only: no collider.

Era: 1" metal mini-blinds, almond baked enamel, wand tilt and cord lift are
exactly 1980s-1990 office stock (cordless and 2" faux-wood came later).
Red's open option (HR03 countable wrongness): one bent slat at the same
index in every blind; not built (a raised stack hides it; it needs a
lowered decor blind or a slat sticking out of the stack).
"""

import math

import kitlib  # noqa: F401
import interact_window_common as wc

NAME = "Kit_MiniBlind_Raised"
LOD1_RATIO = 0.33
LOD2_RATIO = 0.05
LOD1 = LOD1_RATIO
LOD_DISTANCES = (3.0, 10.0, 30.0)
SMOOTH_ANGLE = 40.0

ALMOND = "Prop_SteelAlmond"
WHITE = "Prop_PlasticWhite"

O = wc.BLIND_RAIL                        # asset origin in the window root


def UB(X, Y, Z):
    return wc.U(X - O[0], Y - O[1], Z - O[2])


RAIL_Y0, RAIL_Y1, RAIL_Z0, RAIL_Z1 = 2.160, 2.195, 0.110, 0.145
STACK_Y0, STACK_Y1 = 2.035, 2.160
SLAT_Z0, SLAT_Z1 = 0.115, 0.140
CROWN = 0.0012
NOTCH = 0.0012
BOT_Y0, BOT_Y1, BOT_Z0, BOT_Z1 = 2.006, 2.035, 0.1085, 0.1465
SLAT_X = 0.770
LADDERS = (-0.699, -0.233, 0.233, 0.699)
WAND_X, WAND_Z, WAND_Y0 = 0.72, 0.152, 1.14
CORD_X, CORD_Z, TASSEL_Y0 = 0.74, 0.150, 1.20


def crown(z):
    t = (z - (SLAT_Z0 + SLAT_Z1) / 2) / ((SLAT_Z1 - SLAT_Z0) / 2)
    return CROWN * max(0.0, 1.0 - t * t)


def slab_section(y_b, y_t, nslats=7, dz=0.0):
    """(Z, Y) outline of one grouped slab: bottom slat's underside, the
    front column of slat noses and gaps, top slat's crown, flat back."""
    p = (y_t - y_b) / nslats
    zf = SLAT_Z1 - NOTCH
    pts = []
    for j in range(5):                                     # underside, back -> front
        z = SLAT_Z0 + (zf - SLAT_Z0) * j / 4
        pts.append((z + dz, y_b + crown(z)))
    for k in range(nslats):                                # noses and gaps, going up
        pts.append((SLAT_Z1 + dz, y_b + (k + 0.5) * p + crown(SLAT_Z1)))
        if k < nslats - 1:
            pts.append((zf + dz, y_b + (k + 1) * p + crown(zf)))
    for j in range(5):                                     # top crown, front -> back
        z = zf - (zf - SLAT_Z0) * j / 4
        pts.append((z + dz, y_t + crown(z)))
    return pts


def rail_section():
    return wc.fillet([(RAIL_Z0, RAIL_Y0, 0.001, 2), (RAIL_Z1, RAIL_Y0, 0.0025, 3), (RAIL_Z1, RAIL_Y1, 0.001, 2),
                      (0.1415, RAIL_Y1), (0.1415, 2.1935), (0.1135, 2.1935), (0.1135, RAIL_Y1),
                      (RAIL_Z0, RAIL_Y1, 0.001, 2)], closed=True)


def bottom_rail_section():
    mid = (BOT_Z0 + BOT_Z1) / 2
    return wc.fillet([(BOT_Z0, BOT_Y0, 0.006, 3), (BOT_Z1, BOT_Y0, 0.006, 3), (BOT_Z1, BOT_Y1 - 0.0015, 0.003, 2),
                      (mid, BOT_Y1, 0.0, 0), (BOT_Z0, BOT_Y1 - 0.0015, 0.003, 2)], closed=True)


def build(kit):
    # kit.box sizes are kit (x, y, z) = Unity (SX, SZ, SY)
    rnd = wc.rng(1990)
    dust = lambda X, Y, Z: (1.0, 1.0, 0.8)                 # dust on the stack and rail tops
    # ---- almond first (submesh 0)
    rail = wc.extrude_x(kit, rail_section(), -0.7725, 0.7725, ALMOND, name="head rail", mapf=UB)
    wc.paint_wear(rail, dust)
    for s in (-1, 1):
        x = s * 0.7625
        br = kit.box((0.025, 0.0395, 0.0395), UB(x, 2.17825, 0.12825), ALMOND, bevel=0.0007, segments=1, name="box bracket")
        wc.paint_wear(br)
        kn = kit.cylinder(0.0012, 0.025, UB(x, 2.1585, 0.1478), ALMOND, verts=10, rot=(0, 90, 0), bevel=0.0, name="bracket hinge")
        wc.paint_wear(kn)
        kit.lod1_drop(kn)
    n = 12
    edge_top = STACK_Y1 - CROWN
    h = (edge_top - STACK_Y0) / n
    for i in range(n):
        y_b = STACK_Y0 + i * h
        dz = rnd.uniform(-0.0004, 0.0004)
        x1 = SLAT_X - rnd.uniform(0.0, 0.0008)
        slab = wc.extrude_x(kit, slab_section(y_b, y_b + h, 7, dz), -x1, x1, ALMOND, name="slat slab", mapf=UB)
        wc.paint_wear(slab, dust)
        kit.lod1_drop(slab)
        wc.lod2_drop(slab)
    block = kit.box((2 * (SLAT_X - 0.001), 0.0222, 0.1230), UB(0.0, 2.0971, 0.1269), ALMOND, bevel=0.0, name="stack block (LOD1)")
    wc.paint_wear(block, dust)
    bot = wc.extrude_x(kit, bottom_rail_section(), -SLAT_X, SLAT_X, ALMOND, name="bottom rail", mapf=UB)
    wc.paint_wear(bot)
    for s in (-1, 1):
        cap = kit.box((0.004, 0.0390, 0.0300), UB(s * (SLAT_X + 0.002), (BOT_Y0 + BOT_Y1) / 2, (BOT_Z0 + BOT_Z1) / 2),
                      ALMOND, bevel=0.0012, segments=1, name="bottom rail cap")
        wc.paint_wear(cap)
    for x in LADDERS:                                      # front ladder cords, slack loops out of the stack
        pts = [(x, RAIL_Y0 + 0.001, SLAT_Z1 + 0.0005)]
        for k in range(6):
            yl = STACK_Y1 - (k + 0.5) * (STACK_Y1 - STACK_Y0) / 6
            pts.append((x + rnd.uniform(-0.0015, 0.0015), yl, SLAT_Z1 + 0.0035 + rnd.uniform(0.0, 0.0015)))
            pts.append((x, yl - (STACK_Y1 - STACK_Y0) / 12, SLAT_Z1 + 0.0007))
        pts[-1] = (x, BOT_Y1 - 0.002, SLAT_Z1 + 0.0007)    # into the bottom rail
        cord = kit.tube([UB(*p) for p in pts], 0.00045, ALMOND, verts=4, name="ladder cord")
        wc.paint_wear(cord)
        kit.lod1_drop(cord)
        wc.lod2_drop(cord)

    # ---- white plastic: spacers, wand, cords, tassel, sill guards
    for s in (-1, 1):
        sp = kit.box((0.025, 0.0285, 0.045), UB(s * 0.7625, 2.1775, 0.09425), WHITE, bevel=0.001, segments=1, name="spacer block")
        wc.paint_wear(sp)
    tilter = kit.cylinder(0.0022, 0.012, UB(WAND_X, 2.1585, 0.1465), WHITE, verts=10, bevel=0.0005, segments=1, name="tilter")
    wc.paint_wear(tilter)
    hook = kit.tube([UB(WAND_X, 2.1535, 0.1465), UB(WAND_X, 2.1495, 0.1495), UB(WAND_X, 2.1515, 0.1530),
                     UB(WAND_X, 2.1560, 0.1525)], 0.0007, WHITE, verts=5, name="tilter hook")
    wc.paint_wear(hook)
    kit.lod1_drop(hook)
    con = kit.cylinder(0.0032, 0.016, UB(WAND_X, 2.140, WAND_Z), WHITE, verts=8, bevel=0.0006, segments=1, name="wand head")
    wc.paint_wear(con)
    wand_top, wand_bot = 2.132, WAND_Y0 + 0.012
    wand = kit.cylinder(0.0045, wand_top - wand_bot, UB(WAND_X, (wand_top + wand_bot) / 2, WAND_Z), WHITE, verts=6,
                        bevel=0.0005, segments=1, name="tilt wand")
    wc.paint_wear(wand, lambda X, Y, Z: (0.7 if Y < 1.5 else 1.0, 1.0, 1.0))
    tip = kit.cylinder(0.0030, 0.012, UB(WAND_X, WAND_Y0 + 0.006, WAND_Z), WHITE, verts=6, radius_top=0.0045,
                       bevel=0.0008, segments=1, name="wand tip")      # tapered end
    wc.paint_wear(tip, lambda X, Y, Z: (0.6, 1.0, 1.0))
    lock = kit.box((0.022, 0.012, 0.006), UB(CORD_X, RAIL_Y0 - 0.003, 0.142), ALMOND, bevel=0.001, segments=1, name="cord lock")
    wc.paint_wear(lock)
    kit.lod1_drop(lock)
    for dx in (-0.0016, 0.0016):
        c = kit.tube([UB(CORD_X + dx, RAIL_Y0 - 0.005, 0.1445), UB(CORD_X + dx, RAIL_Y0 - 0.020, CORD_Z),
                      UB(CORD_X + dx * 0.4, TASSEL_Y0 + 0.036, CORD_Z)], 0.0007, WHITE, verts=5, name="lift cord")
        wc.paint_wear(c, lambda X, Y, Z: (0.8 if Y < 1.6 else 1.0, 1.0, 1.0))
        wc.lod2_drop(c)
    prof = [(0.0010, 0.000), (0.0034, 0.0015), (0.0052, 0.0080), (0.0055, 0.0150), (0.0047, 0.0230),
            (0.0030, 0.0300), (0.0016, 0.0350), (0.0012, 0.0370)]
    tassel = kit.lathe([(r, z + TASSEL_Y0 - O[1]) for (r, z) in prof], UB(CORD_X, O[1], CORD_Z), WHITE, verts=10, name="tassel")
    wc.paint_wear(tassel, lambda X, Y, Z: (0.65, 1.0, 1.0))
    for s in (-1, 1):
        g = kit.box((0.020, 0.030, 0.0030), UB(s * 0.60, BOT_Y0 - 0.0015, (BOT_Z0 + BOT_Z1) / 2), WHITE, bevel=0.0008,
                    segments=1, name="sill guard")
        wc.paint_wear(g)
        kit.lod1_drop(g)
        wc.lod2_drop(g)

    # ---- self-checks: (d) nothing inside the opening volume, (e) lowest point over the opening >= 2.000
    pts = [(p[0] + O[0], p[1] + O[1], p[2] + O[2]) for _, p in wc.unity_verts(kit)]
    inside = [p for p in pts if abs(p[0]) < wc.OPEN_X and p[1] < wc.OPEN_Y1]
    assert not inside, "blind inside the opening: %r" % inside[:3]
    low = min(p[1] for p in pts if abs(p[0]) <= wc.OPEN_X)
    assert low >= 2.000, low
    assert min(p[2] for p in pts if p[1] < 2.075 + 0.001) > wc.FACE_Z, "blind touches the frame's face band"
    assert abs(min(p[1] for p in pts) - WAND_Y0) < 0.001 and min(p[2] for p in pts) >= wc.WALL_Z - 1e-6

    kit.anchor("blind_rail", UB(*O))
    kit.anchor("wand_tip", UB(WAND_X, WAND_Y0, WAND_Z))
    kit.anchor("tassel", UB(CORD_X, TASSEL_Y0, CORD_Z))
    kit.anchor("stack_bottom", UB(0.0, BOT_Y0, (BOT_Z0 + BOT_Z1) / 2))
    kit.meta["mount"] = {"anchor": "blind_rail", "on": "Kit_WindowFrame_Steel face A", "lowestOverOpening": round(low, 4)}
    wc.lod_meta(kit, LOD1_RATIO, LOD2_RATIO, LOD_DISTANCES)
    kit.no_collider()
    kit.tag("interactable", "window", "blind")
