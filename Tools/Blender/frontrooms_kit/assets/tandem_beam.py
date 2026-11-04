"""Kit_TandemBeam3: three-seat tandem beam seating, a 1986-89 refit of a US office waiting room (游戏视觉,
bs4-hero-kits, builder A; research/station/72_hero_kits.md §2.1). Replaces the three Kit_TaskChair_Navy stand-ins
P27-P29 of Station_Waiting_3x2 (row A, faces the set).

Period (03_palette B rows, SOURCES §E): E02 Contract 9/1987 beam seating with black urethane arms and base and
recoverable upholstery; E12 12/1986 beam-mounted tandem systems, laminate beam tables, an ashtray; E11 1/1992
polypropylene shells with upholstered cushions, tables in place of seats, arms clamped to the beam; E13 5/1991
steel beams in chrome, epoxy or metallic. 叙事's E15 verdict: steel black or chrome; seats slate blue or charcoal
(navy "is a period colour, but it is not attested on beam seating", D_palette §1). Authored in slate
(Prop_VinylSlate); _Navy (today's stand-in colour) and _Charcoal are the variants for Red's pick (72 §7).

Construction (Unity metres; origin on the floor under the middle seat; front +Z; seats at x -0.65 / 0 / +0.65):
* beam: 51 x 76 mm black epoxy steel tube (r 4), 2.00 m, centre y 0.29 z -0.03, black PP end plugs (2 mm flange,
  recessed face);
* two T-legs at x +-0.325 under the inner arms: 51 mm square upright (r 3) to the beam's underside, a 25 x 51 mm
  floor foot 0.60 long with PP end caps, two 6 mm gussets with weld beads, two levelling glides (black nylon
  D38 x 12, chamfered, 4 mm of the steel stem showing), a U-saddle (bottom plate, cheeks, top strap) clamping the
  beam with two M10 hex bolts (17 A/F heads, washer faces) and nuts;
* per seat a bracket: a 50 x 6 flat bar on the beam with C-clamp cheeks (a carriage head in front, a nut behind),
  two 11 mm bosses under the PP seat pan, and a 38 x 9.5 back upright bent up (r 25) at the 12-degree recline,
  bolted to the back shell (two 13 A/F heads);
* arms: 38 x 9.5 posts, the inner ones off the leg saddles' top straps, the outer ones off beam collars at
  +-0.975 (two bolt heads); black urethane caps 50 x 45 x 380, rounded nose r 22, edges r 3-5, 2 mm crown;
* seat: PP pan (0.345-0.357, a 12 mm band under the cushion), cushion 0.50 x 0.47 x 0.075 with an 8 mm crown,
  a waterfall front (r 40), welt cords (6 mm) on the side and back top seams; back: pad 0.50 x 0.34 x 0.064 with
  a 6 mm crown and a front-seam welt, in a 12 mm PP shell with a 6 mm lip and four domed screws (D9) behind;
* wear (geometry; the shader ignores vertex colour): seat sag 7 / 12 / 6 mm (the middle seat most), and the
  S seat's side welt opened over 25 mm near the front (row A at yaw 90 puts local +x to the south).

LODs are hand-built (fr_lods): LOD1 keeps the silhouette with fewer sides and no hardware under 5 cm (glides as
6-sided pucks), LOD2 is boxes. Budget 9,000 / 3,000 / 600 (cap 10,500); lodDistances [6, 15, 60].
Slots: Prop_SteelBlack, Prop_PlasticBlack, Prop_VinylSlate (3 draws).
"""

import math

from mathutils import Vector

import _station_waiting_common as w

NAME = "Kit_TandemBeam3"
SMOOTH_ANGLE = w.SMOOTH
VARIANTS = {"Kit_TandemBeam3_Navy": {w.SLATE: w.NAVY}, "Kit_TandemBeam3_Charcoal": {w.SLATE: w.CHARCOAL}}

STEEL, PLASTIC, VINYL = w.STEEL, w.PLASTIC, w.SLATE

# ---------------------------------------------------------------- section (72 §2.1 stack-up)
BEAM_W, BEAM_H, BEAM_Y, BEAM_Z, BEAM_L = 0.051, 0.076, 0.29, -0.03, 2.00
BEAM_TOP = BEAM_Y + BEAM_H / 2           # 0.328
BEAM_BOT = BEAM_Y - BEAM_H / 2           # 0.252
BZ0, BZ1 = BEAM_Z - BEAM_W / 2, BEAM_Z + BEAM_W / 2   # -0.0555, -0.0045
PLATE = 0.006
BAR_TOP = BEAM_TOP + PLATE               # 0.334 flat bar / strap top
PAN_Y0, PAN_Y1 = 0.345, 0.357
SEAT_HW, SEAT_HD, SEAT_ZC = 0.25, 0.235, 0.055      # cushion z -0.18 .. +0.29
LEGS = (-0.325, 0.325)
FOOT_Y0, FOOT_Y1, FOOT_L = 0.016, 0.041, 0.596
GLIDE_Z = 0.255
ARM_Y0, ARM_Y1, ARM_Z0, ARM_Z1 = 0.590, 0.635, -0.17, 0.21
RECLINE = 12.0
BACK_HW, BACK_HD, BACK_T = 0.25, 0.17, 0.064
SHELL_LIP = 0.006
BACK_TOP_Y, BACK_TOP_Z = 0.83, -0.315


def back_frame(x):
    """Back frame: local x across, local y = the back's forward normal (from the shell's rear surface toward the
    seat), local z = up the reclined back. Origin = the centre of the shell's rear surface."""
    xa, ya, za = w.tilt_x(RECLINE)
    # tilt_x gives (x, up-reclined, forward); the pad builder wants height along local y = forward.
    up = Vector(ya)
    fwd = Vector(za)
    hd_s = BACK_HD + SHELL_LIP
    top = Vector((x, BACK_TOP_Y, BACK_TOP_Z))
    o = top - up * hd_s
    return w.Frame(o, (1, 0, 0), tuple(fwd), tuple(up))


# ---------------------------------------------------------------- LOD0 parts
def beam(kit, lv):
    prof = w.rr_profile(BEAM_W, BEAM_H, 0.004, 3 if lv == 0 else 1)
    if lv == 2:
        return w.box(kit, (BEAM_L, BEAM_H, BEAM_W), (0, BEAM_Y, BEAM_Z), STEEL, bevel=0.0, segments=1, name="beam lod2")
    w.sweep(kit, "beam", STEEL, [(-BEAM_L / 2, BEAM_Y, BEAM_Z), (BEAM_L / 2, BEAM_Y, BEAM_Z)], prof, side=(0, 0, 1),
            caps=False)
    for sgn in (-1, 1):
        x0 = sgn * BEAM_L / 2
        if lv == 0:
            rings = []
            for dx, grow in ((-0.0015, 0.0006), (0.0012, 0.0006), (0.0020, -0.0008),
                             (0.0020, -0.0055), (0.0008, -0.0068)):
                pr = w.rr_profile(BEAM_W + 2 * grow, BEAM_H + 2 * grow, 0.004 + grow * 0.5, 2)
                rings.append([(x0 + sgn * dx, BEAM_Y + v, BEAM_Z + s) for (s, v) in pr])
            w.part_from_rings(kit, "beam end plug", PLASTIC, rings, closed=True, cap_end="fan")
        else:
            pr = w.rr_profile(BEAM_W + 0.001, BEAM_H + 0.001, 0.004, 1)
            rings = [[(x0 - sgn * 0.002, BEAM_Y + v, BEAM_Z + s) for (s, v) in pr],
                     [(x0 + sgn * 0.002, BEAM_Y + v, BEAM_Z + s) for (s, v) in pr]]
            w.part_from_rings(kit, "beam end plug", PLASTIC, rings, closed=True, cap_end="ngon")


def collar(kit, x, width, lv, name):
    """U-saddle / beam collar: bottom plate, two cheeks, a top strap (6 mm plates) round the beam."""
    if lv == 2:
        return
    bev, seg = (0.0012, 1) if lv == 0 else (0.0, 1)
    zc = BEAM_Z
    full = BEAM_W + 2 * PLATE
    w.box(kit, (width, PLATE, full), (x, BEAM_BOT - PLATE / 2, zc), STEEL, bevel=bev, segments=seg, name=name + " bottom")
    w.box(kit, (width, PLATE, full), (x, BEAM_TOP + PLATE / 2, zc), STEEL, bevel=bev, segments=seg, name=name + " strap")
    h = BEAM_H
    for z in (BZ1 + PLATE / 2, BZ0 - PLATE / 2):
        w.box(kit, (width, h, PLATE), (x, BEAM_Y, z), STEEL, bevel=bev, segments=seg, name=name + " cheek")


def bolt_set(kit, x, y, af, head_h, lv, nut_back=True, thread=0.004, carriage=False, name="bolt", washer=True):
    """A bolt through the beam's cheeks along z: head on the front cheek, nut on the back one."""
    if lv != 0:
        return
    zf = BZ1 + PLATE
    zb = BZ0 - PLATE
    if carriage:
        w.dome_head(kit, name + " head", STEEL, (x, y, zf), (0, 0, 1), af * 1.25, head_h * 0.75, sides=10)
    else:
        w.hex_head(kit, name + " head", STEEL, (x, y, zf), (0, 0, 1), af, head_h, washer=(af * 0.60, 0.0012) if washer else None)
    if nut_back:
        w.hex_head(kit, name + " nut", STEEL, (x, y, zb), (0, 0, -1), af, head_h * 1.25)
        rt = af * 0.3
        w.lathe(kit, name + " thread", STEEL, [(rt, 0.0), (rt * 0.9, thread), (0.0, thread)],
                base=(x, y, zb - head_h * 1.25), axis=(0, 0, -1), verts=6)


def leg(kit, x, lv):
    zc = BEAM_Z
    if lv == 2:
        w.box(kit, (0.051, FOOT_Y1, 0.60), (x, FOOT_Y1 / 2, 0.0), STEEL, bevel=0.0, segments=1, name="foot lod2")
        w.box(kit, (0.051, BEAM_BOT - FOOT_Y1, 0.051), (x, (BEAM_BOT + FOOT_Y1) / 2, zc), STEEL, bevel=0.0,
              segments=1, name="upright lod2")
        return
    # floor foot (25 x 51 rectangular tube along z) and its PP end caps
    fy = (FOOT_Y0 + FOOT_Y1) / 2
    fprof = w.rr_profile(0.051, 0.025, 0.003, 2 if lv == 0 else 1)
    w.sweep(kit, "foot", STEEL, [(x, fy, -FOOT_L / 2), (x, fy, FOOT_L / 2)], fprof, side=(1, 0, 0), caps=False)
    for sgn in (-1, 1):
        z0 = sgn * FOOT_L / 2
        if lv == 0:
            rings = []
            for dz, grow in ((-0.0015, 0.0006), (0.0014, 0.0006), (0.0020, -0.0008)):
                pr = w.rr_profile(0.051 + 2 * grow, 0.025 + 2 * grow, 0.003 + grow * 0.5, 2)
                rings.append([(x + s, fy + v, z0 + sgn * dz) for (s, v) in pr])
            w.part_from_rings(kit, "foot cap", PLASTIC, rings, closed=True, cap_end="fan")
        else:
            pr = w.rr_profile(0.052, 0.026, 0.003, 1)
            rings = [[(x + s, fy + v, z0 - sgn * 0.0015) for (s, v) in pr], [(x + s, fy + v, z0 + sgn * 0.002) for (s, v) in pr]]
            w.part_from_rings(kit, "foot cap", PLASTIC, rings, closed=True, cap_end="ngon")
    # levelling glides
    for gz in (-GLIDE_Z, GLIDE_Z):
        if lv == 0:
            w.lathe(kit, "glide", PLASTIC, [(0.0172, 0.0), (0.0190, 0.0016), (0.0190, 0.0088), (0.0158, 0.0120),
                                           (0.0062, 0.0120), (0.0, 0.0120)], base=(x, 0.0, gz), verts=12)
            w.lathe(kit, "glide stem", STEEL, [(0.0050, 0.0115), (0.0050, FOOT_Y0 + 0.001)], base=(x, 0.0, gz), verts=6)
        else:
            w.lathe(kit, "glide", PLASTIC, [(0.019, 0.0), (0.019, FOOT_Y0 + 0.001), (0.0, FOOT_Y0 + 0.001)],
                    base=(x, 0.0, gz), verts=6)
    # upright (51 square tube) from the foot to the saddle's bottom plate
    uprof = w.rr_profile(0.051, 0.051, 0.003, 2 if lv == 0 else 1)
    w.sweep(kit, "upright", STEEL, [(x, FOOT_Y1 - 0.002, zc), (x, BEAM_BOT - PLATE + 0.001, zc)], uprof, side=(1, 0, 0),
            caps=False)
    # gussets front and back, 6 mm, with weld beads
    g = 0.085
    for sgn, zf in ((1, BZ1), (-1, BZ0)):
        outline = [(zf, FOOT_Y1), (zf + sgn * g, FOOT_Y1), (zf + sgn * 0.010, FOOT_Y1 + g - 0.006), (zf, FOOT_Y1 + g)]
        outline = [(z, y) for (z, y) in outline]
        if lv == 0:
            w.sc.extrude_zy(kit, outline, PLATE, x, STEEL, bevel=0.0009, segments=1, name="gusset")
            for side in (-1, 1):
                xs = x + side * (PLATE / 2 + 0.0006)
                w.weld_bead(kit, "weld", STEEL, [(xs, FOOT_Y1 + 0.0006, zf + sgn * 0.002), (xs, FOOT_Y1 + 0.0006, zf + sgn * (g - 0.004))],
                            r=0.0011, sides=5)
        else:
            w.sc.extrude_zy(kit, outline, PLATE, x, STEEL, bevel=0.0, segments=1, name="gusset")
    # U-saddle round the beam, two M10 bolts
    collar(kit, x, 0.070, lv, "saddle")
    for dx in (-0.022, 0.022):
        bolt_set(kit, x + dx, BEAM_Y, 0.017, 0.0064, lv, name="saddle bolt")


def arm(kit, x, lv, outer, lod2_slot=PLASTIC):
    """Arm post (38 x 9.5) and urethane cap; outer arms stand on a beam collar."""
    zc = BEAM_Z
    if lv == 2:
        w.box(kit, (0.05, ARM_Y1 - ARM_Y0, ARM_Z1 - ARM_Z0), (x, (ARM_Y0 + ARM_Y1) / 2, (ARM_Z0 + ARM_Z1) / 2), lod2_slot,
              bevel=0.0, segments=1, name="arm cap lod2")
        w.box(kit, (0.0095, ARM_Y0 - BAR_TOP, 0.038), (x, (ARM_Y0 + BAR_TOP) / 2, zc), STEEL, bevel=0.0, segments=1,
              name="arm post lod2")
        return
    if outer:
        collar(kit, x, 0.050, lv, "arm collar")
        for dx in (-0.012, 0.012):
            bolt_set(kit, x + dx, BEAM_Y, 0.013, 0.0053, lv, name="collar bolt", washer=False)
    bev, seg = (0.0016, 1) if lv == 0 else (0.0, 1)
    w.box(kit, (0.0095, ARM_Y0 + 0.004 - BAR_TOP, 0.038), (x, (ARM_Y0 + 0.004 + BAR_TOP) / 2, zc), STEEL, bevel=bev,
          segments=seg, name="arm post")
    if lv == 0:
        for side in (-1, 1):
            w.weld_bead(kit, "weld", STEEL, [(x + side * 0.0053, BAR_TOP + 0.0007, zc - 0.018), (x + side * 0.0053, BAR_TOP + 0.0007, zc + 0.018)],
                        r=0.0012, sides=5)
    # cap: plan outline 50 x 380, nose r 22 at the front (+z), back corners r 8; edges r 3.5 / 4.5; crown 2 mm
    hd = (ARM_Z1 - ARM_Z0) / 2
    zc_cap = (ARM_Z1 + ARM_Z0) / 2
    fr = w.Frame((x, ARM_Y0, zc_cap))
    crown = 0.002

    def warp(lx, ly, lz):
        if ly > 0.040:
            f = min(1.0, (ly - 0.040) / 0.0065)
            ly += crown * f * max(0.0, 1 - (lz / hd) ** 2) * max(0.0, 1 - (lx / 0.025) ** 2)
        return lx, ly, lz
    if lv == 0:
        w.pad(kit, "arm cap", PLASTIC, fr, 0.025, hd, [0.022, 0.022, 0.008, 0.008], [5, 5, 2, 2],
              [[], [0.2, 0.45, 0.7], [0.5], [0.3, 0.55, 0.8]],
              [(0.0035, 0.0), (0.0, 0.0035), (0.0, 0.0400), (0.0042, 0.0448), (0.0110, 0.0450)], warp=warp,
              cap_top="fan", cap_bottom="fan")
    else:
        w.pad(kit, "arm cap", PLASTIC, fr, 0.025, hd, [0.020, 0.020, 0.006, 0.006], [3, 3, 1, 1], [[], [], [], []],
              [(0.002, 0.0), (0.0, 0.004), (0.0, 0.041), (0.006, 0.045)], warp=warp, cap_top="fan", cap_bottom="ngon")


def seat_bracket(kit, x, lv, back=True, riser=None):
    """C-clamp flat bar on the beam, bosses, the bent back upright (or a table riser)."""
    zc = BEAM_Z
    if lv == 2:
        if back:
            w.box(kit, (0.038, BAR_TOP - BEAM_TOP + 0.01, 0.38), (x, BAR_TOP - 0.003, 0.01), STEEL, bevel=0.0, segments=1,
                  name="bracket lod2")
        return
    bev, seg = (0.0016, 1) if lv == 0 else (0.0, 1)
    w.box(kit, (0.050, PLATE, 0.37), (x, BEAM_TOP + PLATE / 2, 0.015), STEEL, bevel=bev, segments=seg, name="flat bar")
    if lv == 0:
        for z in (BZ1 + PLATE / 2, BZ0 - PLATE / 2):
            w.box(kit, (0.050, 0.066, PLATE), (x, BAR_TOP - 0.033, z), STEEL, bevel=0.0010, segments=1, name="clamp cheek")
        bolt_set(kit, x, BEAM_Y - 0.008, 0.013, 0.0053, lv, carriage=True, name="clamp bolt")
        if riser is None:
            for bz in (-0.065, 0.150):
                w.lathe(kit, "boss", STEEL, [(0.010, BAR_TOP - 0.001), (0.010, PAN_Y0 + 0.0005)], base=(x, 0.0, bz), verts=12)
    if back:
        f = back_frame(x)
        # centre line 4.75 mm behind the shell's rear surface
        top = f.p(0, -0.00475, 0.06)
        y_leg = BAR_TOP + 0.00475
        # local z where the centre line meets y_leg
        c0 = f.p(0, -0.00475, 0.0)
        t = (y_leg - c0.y) / f.z.y
        corner = f.p(0, -0.00475, t)
        a2 = (-0.10, y_leg)
        pts2 = w.fillet_path(a2, (corner.z, corner.y), (top.z, top.y), 0.025, steps=w.bend_steps(90 - RECLINE, 20.0) if lv == 0 else 2)
        path = [(x, yy, zz) for (zz, yy) in pts2]
        if lv == 0:
            w.bar(kit, "back upright", STEEL, path, 0.038, 0.0095, r=0.0012, steps=1, side=(1, 0, 0), chamfer=0.0008)
            for lz in (-0.105, 0.015):
                base = f.p(0, -0.0095, lz)
                w.hex_head(kit, "back bolt", STEEL, tuple(base), tuple(-f.y), 0.013, 0.0053, ref=(1, 0, 0))
        else:
            w.sweep(kit, "back upright", STEEL, path, w.rr_profile(0.038, 0.0095, 0.0, 0), side=(1, 0, 0), caps="ngon")
    if riser is not None:
        # beam table: a 51 square riser and a 6 mm sub-plate under the top
        top_under = riser
        rprof = w.rr_profile(0.051, 0.051, 0.003, 2 if lv == 0 else 1)
        w.sweep(kit, "table riser", STEEL, [(x, BAR_TOP - 0.001, zc), (x, top_under - PLATE + 0.001, zc)], rprof,
                side=(1, 0, 0), caps=False)
        w.box(kit, (0.30, PLATE, 0.17), (x, top_under - PLATE / 2, -0.075), STEEL, bevel=0.0012 if lv == 0 else 0.0,
              segments=1, name="table plate")


def cushion(kit, x, lv, sag, split=False):
    """Seat cushion on its PP pan (pan 0.345-0.357)."""
    zc = SEAT_ZC
    if lv == 2:
        w.box(kit, (0.50, 0.087, 0.47), (x, PAN_Y0 + 0.0435, zc), VINYL, bevel=0.0, segments=1, name="cushion lod2")
        return
    # pan
    fp = w.Frame((x, PAN_Y0, zc))
    if lv == 0:
        w.pad(kit, "seat pan", PLASTIC, fp, SEAT_HW + 0.0035, SEAT_HD + 0.0035, [0.048, 0.048, 0.038, 0.038], [3, 3, 2, 2],
              [[0.5], [0.5], [0.5], [0.5]], [(0.0025, 0.0), (0.0, 0.0025), (0.0, 0.012), (0.008, 0.012)],
              cap_top=None, cap_bottom="fan")
    fc = w.Frame((x, PAN_Y1, zc))
    hw, hd = SEAT_HW, SEAT_HD
    if lv == 0:
        rings = [(0.0035, 0.000), (0.000, 0.040), (0.000, 0.057), (0.000, 0.0605), (0.000, 0.0640),
                 (0.000, 0.0668), (0.0070, 0.0712), (0.0200, 0.0742), (0.0600, 0.0752), (0.1300, 0.0752)]
        welt = {3: 0.0030, 4: 0.0032, 5: 0.0006}
        steps = [5, 5, 4, 4]
        side_x = [0.33, 0.67] if not split else [0.33, 0.67, 0.79, 0.84, 0.88, 0.93]
        fracs = [[0.33, 0.67], [0.33, 0.67], [0.5], side_x]
    else:
        rings = [(0.0035, 0.000), (0.000, 0.006), (0.000, 0.058), (0.0100, 0.0715), (0.0450, 0.0752)]
        welt = {}
        steps = [3, 3, 2, 2]
        fracs = [[0.5], [0.5], [0.5], [0.5]]
    radii = [0.045, 0.045, 0.035, 0.035]
    split_lo, split_hi = None, None
    if split:
        # +x edge runs from the back (0) to the front (1); the split spans fractions 0.79-0.88 (about 25 mm)
        side_len = 2 * hd - radii[3] - radii[0]
        z_a = -hd + radii[3]
        split_lo, split_hi = z_a + 0.785 * side_len, z_a + 0.885 * side_len

    def front_w(nz):
        return max(0.0, nz) ** 2

    def waterfall(h):
        R, h0 = 0.040, 0.035
        if h <= h0:
            return 0.0
        d = min(h - h0, R)
        return R - math.sqrt(max(0.0, R * R - d * d))

    def bulge(i, bx, bz, nx, nz):
        h = rings[i][1]
        b = welt.get(i, 0.0) * (1.0 - front_w(nz))
        if split and nx > 0.9 and split_lo <= bz <= split_hi and i in (3, 4, 5):
            b = {3: -0.0012, 4: -0.0020, 5: 0.0012}[i]
        return b - front_w(nz) * waterfall(h)

    crown = 0.008

    def warp(lx, ly, lz):
        f = max(0.0, min(1.0, (ly - 0.068) / 0.0072))
        if f > 0:
            cx = max(0.0, 1 - (lx / hw) ** 2)
            cz = max(0.0, 1 - ((lz - 0.01) / hd) ** 2)
            ly += crown * f * cx * cz
            ly -= sag * f * math.exp(-(lx / 0.15) ** 2 - ((lz + 0.01) / 0.15) ** 2)
        return lx, ly, lz
    w.pad(kit, "seat cushion", VINYL, fc, hw, hd, radii, steps, fracs, rings, warp=warp, cap_top="fan", cap_bottom=None,
          bulge=bulge)
    if lv == 1:
        w.pad(kit, "seat pan", PLASTIC, fp, SEAT_HW + 0.0035, SEAT_HD + 0.0035, [0.048, 0.048, 0.038, 0.038], [2, 2, 1, 1],
              [[], [], [], []], [(0.0, 0.0), (0.0, 0.012), (0.008, 0.012)], cap_top=None, cap_bottom="ngon")


def back(kit, x, lv):
    f = back_frame(x)
    hw, hd = BACK_HW, BACK_HD
    if lv == 2:
        c = f.p(0, 0.035, 0.0)
        w.mesh_part(kit, "back lod2", VINYL, *obox(f, 0.0, 0.035, 0.0, 2 * hw + 0.012, 0.072, 2 * hd + 0.012))
        return
    if lv == 0:
        rings = [(0.000, 0.014), (0.000, 0.0545), (0.000, 0.0580), (0.000, 0.0615), (0.000, 0.0645),
                 (0.0070, 0.0695), (0.0200, 0.0735), (0.0600, 0.0752), (0.1200, 0.0758)]
        welt = {2: 0.0030, 3: 0.0032, 4: 0.0006}
        steps = [4, 4, 3, 3]
        fracs = [[0.3, 0.7], [0.33, 0.67], [0.3, 0.7], [0.33, 0.67]]
    else:
        rings = [(0.000, 0.014), (0.000, 0.060), (0.010, 0.0715), (0.045, 0.0755)]
        welt = {}
        steps = [2, 2, 2, 2]
        fracs = [[0.5], [], [0.5], []]
    radii = [0.06, 0.06, 0.045, 0.045]
    crown = 0.006

    def bulge(i, bx, bz, nx, nz):
        return welt.get(i, 0.0)

    def warp(lx, ly, lz):
        f = max(0.0, min(1.0, (ly - 0.066) / 0.009))
        if f > 0:
            ly += crown * f * max(0.0, 1 - (lx / hw) ** 2) * max(0.0, 1 - (lz / hd) ** 2)
        return lx, ly, lz
    w.pad(kit, "back pad", VINYL, f, hw, hd, radii, steps, fracs, rings, warp=warp, cap_top="fan", bulge=bulge)
    # PP shell: rear surface at local y 0, a 6 mm lip round the pad to y 0.0205
    hs, ds = hw + SHELL_LIP, hd + SHELL_LIP
    sradii = [r + SHELL_LIP for r in radii]
    if lv == 0:
        srings = [(0.0058, 0.0170), (0.0028, 0.0212), (0.0, 0.0188), (0.0, 0.0032), (0.0030, 0.0),
                  (0.0600, -0.0020)]

        def dome(lx, ly, lz):
            return lx, ly - 0.0006 * max(0.0, 1 - (lx / hs) ** 2) * max(0.0, 1 - (lz / ds) ** 2), lz
        w.pad(kit, "back shell", PLASTIC, f, hs, ds, sradii, [3, 3, 3, 3], [[0.5], [0.5], [0.5], [0.5]], srings,
              warp=dome, cap_top="fan", cap_bottom=None)
        for sx in (-0.19, 0.19):
            for sz in (-0.115, 0.115):
                base = f.p(sx, -0.0016, sz)
                w.dome_head(kit, "shell screw", w.STEEL, tuple(base), tuple(-f.y), 0.009, 0.0032, sides=8, rings=2)
    else:
        w.pad(kit, "back shell", PLASTIC, f, hs, ds, sradii, [2, 2, 2, 2], [[], [], [], []],
              [(0.0055, 0.016), (0.0, 0.0205), (0.0, 0.0), (0.04, -0.002)], cap_top="fan", cap_bottom=None)


def obox(f, cx, cy, cz, sx, sy, sz):
    """Oriented box in frame f (local centre, local sizes) -> (verts, faces) for mesh_part."""
    vs = []
    for dx in (-0.5, 0.5):
        for dy in (-0.5, 0.5):
            for dz in (-0.5, 0.5):
                vs.append(tuple(f.p(cx + dx * sx, cy + dy * sy, cz + dz * sz)))
    faces = [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]
    return vs, faces


# ---------------------------------------------------------------- the kit
def build_tandem(kit, seats, arms, sags, table_x=None, split_x=None, table_fn=None, arm_lod2_slot=PLASTIC,
                 budget=(9000, 3000, 600), note="front +Z; beam seating, seats at x -0.65 / 0 / +0.65"):
    w.register()
    for lv in (0, 1, 2):
        with w.Level(kit, str(lv)):
            beam(kit, lv)
            for lx in LEGS:
                leg(kit, lx, lv)
            for ax in arms:
                arm(kit, ax, lv, outer=abs(abs(ax) - 0.975) < 1e-6, lod2_slot=arm_lod2_slot)
            for sx, sg in zip(seats, sags):
                seat_bracket(kit, sx, lv)
                cushion(kit, sx, lv, sg, split=(split_x is not None and abs(sx - split_x) < 1e-6))
                back(kit, sx, lv)
            if table_x is not None and table_fn is not None:
                table_fn(kit, table_x, lv)
    w.tris_report(kit, "pre-finish")
    w.finish_parts(kit, SMOOTH_ANGLE)
    # sidecar (72 §2.1)
    for i, sx in enumerate(seats):
        k = i + 1
        kit.support("seat_%d" % k, w.B(sx, 0.445, 0.04), (0.44, 0.40))
        kit.anchor("sit_%d" % k, w.B(sx, 0.445, 0.04))
        kit.anchor("back_top_%d" % k, w.B(sx, 0.83, -0.29))
    kit.collider(w.B(0.0, 0.42, -0.01), w.BS(2.01, 0.84, 0.62))
    kit.tag("station", "seat", "waiting")
    w.sc.lods(kit, 6.0, 15.0, 60.0, budget)
    w.station_block(kit, "floor under the middle seat position (x 0)", note)


def build(kit):
    build_tandem(kit, seats=(-0.65, 0.0, 0.65), arms=(-0.975, -0.325, 0.325, 0.975), sags=(0.007, 0.012, 0.006),
                 split_x=0.65)
