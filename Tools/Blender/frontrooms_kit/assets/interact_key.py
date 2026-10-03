"""Zone key: a cut brass duplicate of a 1990 interchangeable-core (IC) system
key, the kind a facility hands out with a numbered tag (spec 10 §3.2-§3.3).

Real-world reference: US 6-pin commercial key, 55-60 mm long, nickel-silver
original or a hardware-store brass copy (02 §8.1). Generic rounded "paddle"
bow (no maker's outline, no stamping in v1), 2.2 mm thick with 0.4 mm edge
rounds in three segments (the glint, 03 P5), a 4.8 mm ring hole with 0.3 mm
chamfers, a 13 mm shoulder neck whose front face above the blade is the stop
that seats on the core face, and a 25 mm blade (= Unlock.InsertDepth) with
the §3.2 milled section (two side grooves, chamfered spine) and six cuts.

Origin = THE SHOULDER ON THE TURNING AXIS (key part frame, §3.3): Unity +Z =
insertion toward the tip (kit front, Blender -Y), +Y = cuts up, X = the flat
normal. At full insertion the origin equals the lock's `keyhole` anchor.
Overall 0.058 (Z -0.033 .. +0.025), 0.026 wide (Y -0.0125 .. +0.0135),
0.0022 thick. One rigid mesh: bow and blade never move apart; the shot uses
the anchors (shoulder, tip, insert_dir, cuts_up, grip, ring_hole,
ring_hole_dir).

Budget §9.3: 2,400 / 900 / 150 tris, LOD distances 1.0 / 3 / 12 m. Under
1.0 m, so no LOD1 until P-1 (§1.8). Slot Prop_Brass; VARIANT
Kit_Key_Zone_Nickel = Prop_Aluminium (the satin nickel-silver original, for a
later master key). Era: pin-tumbler IC keys since the 1920s; brass copies
cut at a hardware store are period-true for 1990 (02 §8.1, §13).
"""

import math

import interact_key_common as kc
from interact_key_common import U

NAME = "Kit_Key_Zone"
VARIANTS = {"Kit_Key_Zone_Nickel": {"Prop_Brass": "Prop_Aluminium"}}
LOD1_RATIO, LOD2_RATIO = 900 / 2400, 150 / 2400
LOD_DISTANCES = (1.0, 3.0, 12.0)
BUDGET = (2400, 900, 150)

BRASS = "Prop_Brass"

BOW_T = 0.0022
BOW_ROUND = 0.0004
HOLE_C = (-0.0265, 0.0005)       # (Z, Y)
HOLE_R = 0.0024
HOLE_CHAMFER = 0.0003
NECK_Y = (-0.0045, 0.0085)
BOW_Z = (-0.033, -0.003)
BOW_Y = (-0.0125, 0.0135)
BOW_R = 0.008
FLARE_R = 0.001


def _fillet_centre(line_y, side, corner_c, R, rf):
    """Centre of a radius-rf fillet outside a circle (corner_c, R) and on the
    ``side`` (+1 above / -1 below) of the line y = line_y; front solution."""
    fy = line_y + side * rf
    dy = fy - corner_c[1]
    dz = math.sqrt((R + rf) ** 2 - dy ** 2)
    return (corner_c[0] + dz, fy)


def bow_outline():
    """Bow + neck outline in (Z, Y), CCW, with 1 mm flares into the neck."""
    z0, z1 = BOW_Z
    y0, y1 = BOW_Y
    c_tf = (z1 - BOW_R, y1 - BOW_R)      # top-front corner centre
    c_tb = (z0 + BOW_R, y1 - BOW_R)
    c_bb = (z0 + BOW_R, y0 + BOW_R)
    c_bf = (z1 - BOW_R, y0 + BOW_R)
    ny0, ny1 = NECK_Y
    # Top flare: tangent to the neck top (y = ny1) and the top-front arc.
    f1 = _fillet_centre(ny1, +1, c_tf, BOW_R, FLARE_R)
    t1_arc = math.degrees(math.atan2(f1[1] - c_tf[1], f1[0] - c_tf[0]))
    # Bottom flare: tangent to the neck bottom (y = ny0) and the bottom-front arc.
    f2 = _fillet_centre(ny0, -1, c_bf, BOW_R, FLARE_R)
    t2_arc = math.degrees(math.atan2(f2[1] - c_bf[1], f2[0] - c_bf[0]))
    pts = [(0.0, ny0), (0.0, ny1)]
    # top flare from straight down (-90) clockwise to the arc tangent point
    a_end = math.degrees(math.atan2(c_tf[1] - f1[1], c_tf[0] - f1[0]))
    if a_end > -90:
        a_end -= 360
    pts += kc.arc(f1[0], f1[1], FLARE_R, -90.0, a_end, 4)
    pts += kc.arc(c_tf[0], c_tf[1], BOW_R, t1_arc, 90.0, 12)
    pts += kc.arc(c_tb[0], c_tb[1], BOW_R, 90.0, 180.0, 16)
    pts += kc.arc(c_bb[0], c_bb[1], BOW_R, 180.0, 270.0, 16)
    pts += kc.arc(c_bf[0], c_bf[1], BOW_R, 270.0, 360.0 + t2_arc, 15)
    a_start = math.degrees(math.atan2(c_bf[1] - f2[1], c_bf[0] - f2[0]))
    pts += kc.arc(f2[0], f2[1], FLARE_R, a_start, 90.0, 4)
    return kc.dedupe(pts)


def build_blade(kit):
    """The §3.2 section extruded 0 -> 25 mm (plus 0.5 mm hidden in the neck),
    then the bitting envelope and the nose cut with exact booleans."""
    import bmesh
    sec = kc.blade_section()
    z_start, z_end = -0.0005, kc.BLADE_LEN
    bm = bmesh.new()
    a = [bm.verts.new(U(x, y, z_start)) for x, y in sec]
    b = [bm.verts.new(U(x, y, z_end)) for x, y in sec]
    bm.faces.new(a)
    bm.faces.new(list(reversed(b)))
    n = len(sec)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((a[i], a[j], b[j], b[i]))
    blade = kc.new_obj(kit, bm, BRASS, "blade")

    # Top cutter: everything above the bitting envelope (raised where the
    # envelope is the uncut edge, so no cutter face is coplanar with it).
    TOP, RAISE = kc.BLADE_TOP, 0.0002
    prof = kc.top_profile(0.0, kc.BLADE_LEN)
    at_top = [abs(y - TOP) < 1e-9 for _, y in prof]
    seg_raised = [at_top[i] and at_top[i + 1] for i in range(len(prof) - 1)]
    lower = [(-0.0003, TOP + RAISE)]
    for i, (z, y) in enumerate(prof):
        left = seg_raised[i - 1] if i > 0 else at_top[0]
        right = seg_raised[i] if i < len(seg_raised) else False
        if left and right:
            lower.append((z, TOP + RAISE))
        elif left and not right:
            lower += [(z, TOP + RAISE), (z, y)]
        elif right and not left:
            lower += [(z, y), (z, TOP + RAISE)]
        else:
            lower.append((z, y))
    slope = (prof[-1][1] - prof[-2][1]) / (prof[-1][0] - prof[-2][0])
    lower.append((kc.BLADE_LEN + 0.0003, kc.TIP_Y + slope * 0.0003))
    poly = kc.dedupe(lower + [(kc.BLADE_LEN + 0.0003, 0.007), (-0.0003, 0.007)])
    top_cut = kc.prism([U(0.0, y, z) for z, y in poly], (1, 0, 0), 0.006, name="bitting cutter")

    # Nose: R 0.5 mm at the spine, centre nudged 0.02 mm out so the arc
    # crosses the bottom and tip faces instead of grazing them.
    cz, cy, R = kc.BLADE_LEN - kc.NOSE_R + 0.00002, -0.0040 + kc.NOSE_R - 0.00002, kc.NOSE_R
    nose = kc.arc(cz, cy, R, 10.0, -90.0, 8)
    nose += [(cz, -0.0047), (kc.BLADE_LEN + 0.0004, -0.0047), (kc.BLADE_LEN + 0.0004, cy + R * math.sin(math.radians(10)))]
    nose_cut = kc.prism([U(0.0, y, z) for z, y in nose], (1, 0, 0), 0.006, name="nose cutter")
    kc.boolean_cut(blade, [top_cut, nose_cut])
    return blade


def build(kit):
    kc.assert_blade_spec()
    # Bow + neck: one plate with 3-segment edge rounds and the chamfered ring hole.
    outline = bow_outline()
    hole = kc.circle(HOLE_C[0], HOLE_C[1], HOLE_R, 48)
    to3 = lambda u, v, w: U(w, v, u)       # u = Z, v = Y, w = X (flat normal)
    bow = kc.slab(kit, outline, BOW_T, BRASS, to3, round_r=BOW_ROUND, round_segs=3,
                  holes=[{"poly": hole, "chamfer": HOLE_CHAMFER}], name="bow")
    blade = build_blade(kit)

    # Hand grime on the bow (thumb and finger), clean blade.
    def grime(p):
        z = -p.y                      # Unity Z of a Blender point
        g = 0.80 if z < -0.006 else 1.0
        return (g, 1.0, 1.0, 1.0)
    kc.paint_wear(bow, grime)
    kc.paint_wear(blade)

    # Anchors (Unity key frame).
    kit.anchor("shoulder", U(0, 0, 0))
    kit.anchor("tip", U(0, 0, 0.025))
    kit.anchor("insert_dir", U(0, 0, 0.10))
    kit.anchor("cuts_up", U(0, 0.10, 0))
    kit.anchor("grip", U(0, 0.0005, -0.018))
    kit.anchor("ring_hole", U(0, 0.0005, -0.0265))
    kit.anchor("ring_hole_dir", U(0.10, 0.0005, -0.0265))
    kit.no_collider()
    kit.tag("interactable", "key")
    kc.lod_meta(kit, LOD_DISTANCES, BUDGET)
    kit.meta["keyFrame"] = {"origin": "shoulder on the turning axis", "insert": "+Z", "cutsUp": "+Y", "flatNormal": "X"}
    kit.meta["bitting"] = {"cutZ": list(kc.CUT_Z), "steps": list(kc.CUT_STEPS), "step": kc.CUT_STEP,
                           "base": kc.CUT_BASE, "flat": kc.CUT_FLAT, "flankDegFromVertical": kc.CUT_FLANK_DEG,
                           "tipBevelZ": kc.TIP_BEVEL_Z, "tipY": kc.TIP_Y}

    # Envelope checks on the evaluated parts.
    lo, hi = kc.eval_bounds_unity(kit)
    assert abs(hi[2] - 0.025) < 2e-5 and abs(lo[2] + 0.033) < 2e-5, ("length", lo, hi)
    assert abs(hi[2] - lo[2] - 0.058) < 4e-5
    assert abs(lo[1] + 0.0125) < 2e-5 and abs(hi[1] - 0.0135) < 2e-5, ("bow width", lo, hi)
    assert abs(hi[0] - lo[0] - 0.0022) < 2e-5, ("thickness", lo, hi)
    kc.check_budget(kit, BUDGET[0])
