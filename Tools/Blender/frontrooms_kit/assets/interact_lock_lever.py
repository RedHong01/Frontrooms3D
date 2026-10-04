"""Passage LEVER: the FREE door's handle, one per face on Kit_Lock_Rose
(10_spec §2.1 D7 "lever = bar", §3.1, §3.4 "Open": 35 deg down). A
commercial Grade 1 return lever: a turned Ø 0.024 hub, a Ø 0.0176 neck out to
the grip, and a 0.121 grip of soft-radiused 0.020 x 0.014 section that runs
along part +X (toward the hinge after placement) and returns toward the
door over its last 0.015 (the ADA-era "return" that keeps sleeves from
catching). Satin chrome; bright brass VARIANT for the Lobby.

Real-world reference: Arrow QL / Schlage-type commercial lever, 4-3/4"
(0.121) long, 2-17/32" (0.064) projection (02 §4.1). Levers were the
published accessible standard since UFAS 1984 and common on 1980s office
fit-outs (02 §3.2): the free door's lever against the locked door's knob is
the 2-5 m hardware read (05).

Origin (PART frame, 10_spec §1.2): the spindle axis on the rose's front
face (door X +-0.032, Y 1.000, Z 0.920). Front = Unity +Z (kit -Y). The
hub stands 0.4 mm off the rose boss (a visible turning gap). Front of the
grip at Z 0.054 = 0.064 proud of the leaf face (map limit ~0.07).
Placement: §1.2 face rule; the _p mirror (scale -1 in X) keeps the grip
pointing at the hinge on both faces. On a mirrored placement negate the
motion angle to keep "down".

Motion: rotate about part -Z, 0 -> 35 deg (+angle moves the grip end -Y,
"down"), spring back.
Anchors: spindle (0, 0, 0); grip_press (0.070, 0, 0.054) (where the hand
pushes); grip_end (0.121, 0, 0.033); axis_out (0, 0, 0.10).
Budget (§9.2): LOD0 4,000 (asserted +-15 %), LOD1 1,600, LOD2 300; LOD
distances 1.5 / 4 / 15 m; no LOD1 export (part < 1 m).
Slots: Prop_Chrome. VARIANT _Brass. 96-segment hub; the grip root is a
half-turned cap (48 steps) welded to a 30-point swept section
(16 steps round the return bend).
"""

import math

import interact_lock_common as lc

NAME = "Kit_Lock_Lever"
LOD1 = None
LOD1_RATIO = 0.40
LOD2_RATIO = 0.075
LOD_DISTANCES = (1.5, 4.0, 15.0)
BUDGET = 4000
VARIANTS = {"Kit_Lock_Lever_Brass": {lc.CHROME: lc.BRASS}}
SMOOTH_ANGLE = 40.0

HUB_R, HUB_TOP, NECK_R = 0.012, 0.0115, 0.0088
SEC_H, SEC_D, SEC_RC = 0.020, 0.014, 0.005
GRIP_Z = 0.047                  # section centre line: Z 0.040 .. 0.054
GRIP_LEN = 0.121
BEND_R = 0.014
STRAIGHT_END = GRIP_LEN - SEC_D / 2 - BEND_R      # 0.100
RETURN_END = 0.026
END_R = 0.006
CORNER_K = 6                    # run 2: 5 -> 6 (the section corners carry the highlight)
BEND_STEPS = 16                 # run 2: 12 -> 16 (the return bend is the 0.3 m silhouette)
ROOT_STEPS = 48


def half_section():
    """(rho, z) from the back centre round the +Y half to the front centre."""
    a, b, rc = SEC_H / 2, SEC_D / 2, SEC_RC
    z0, z1 = GRIP_Z - b, GRIP_Z + b
    pts = [(0.0, z0), (a - rc, z0)]
    pts += lc.arc_pts(a - rc, z0 + rc, rc, -90, 0, CORNER_K, include_start=False)
    pts += [(a, z1 - rc)]
    pts += lc.arc_pts(a - rc, z1 - rc, rc, 0, 90, CORNER_K, include_start=False)
    pts += [(0.0, z1)]
    return pts


def full_section():
    """Closed (Y, z_local) loop: the +Y half, then the -Y half back."""
    h = half_section()
    up = [(r, z - GRIP_Z) for r, z in h]
    down = [(-r, z - GRIP_Z) for r, z in reversed(h[1:-1])]
    return up + down


def _arm():
    m = lc.Mesh()
    # Root cap: the half-section turned 180 deg about the spindle on the -X
    # side, so the grip's root reads as a disc in front view.
    h = half_section()
    rings = []
    for k, (rho, z) in enumerate(h):
        if rho < 1e-9:
            rings.append(None)
            continue
        ring = []
        for i in range(ROOT_STEPS + 1):
            t = math.radians(90.0 + 180.0 * i / ROOT_STEPS)
            ring.append(m.add((rho * math.cos(t), rho * math.sin(t), z)))
        rings.append(ring)
    pole0 = m.add((0.0, 0.0, h[0][1]))
    pole1 = m.add((0.0, 0.0, h[-1][1]))
    for k in range(len(h) - 1):
        A, B = rings[k], rings[k + 1]
        for i in range(ROOT_STEPS):
            if A is None:
                m.face((pole0, B[i + 1], B[i]))
            elif B is None:
                m.face((A[i], A[i + 1], pole1))
            else:
                m.face((A[i], A[i + 1], B[i + 1], B[i]))
    # Swept grip: straight, a 90 deg bend toward the door, a short return,
    # a rounded end. Frames: up = +Y, n = outside of the bend.
    sec = full_section()
    frames = []
    for x in (0.0, STRAIGHT_END * 0.33, STRAIGHT_END * 0.66, STRAIGHT_END):
        frames.append(((x, GRIP_Z), (0.0, 1.0), 1.0))
    cz = GRIP_Z - BEND_R
    for i in range(1, BEND_STEPS + 1):
        ph = math.radians(90.0 - 90.0 * i / BEND_STEPS)
        frames.append(((STRAIGHT_END + BEND_R * math.cos(ph), cz + BEND_R * math.sin(ph)),
                       (math.cos(ph), math.sin(ph)), 1.0))
    xr = STRAIGHT_END + BEND_R
    frames.append(((xr, RETURN_END), (1.0, 0.0), 1.0))
    for a in (25.0, 50.0, 70.0, 84.0):
        t = math.radians(a)
        frames.append(((xr, RETURN_END - END_R * math.sin(t)), (1.0, 0.0), math.cos(t)))
    sections = []
    for (px, pz), (nx, nz), s in frames:
        sections.append([(px + nx * zl * s, y * s, pz + nz * zl * s) for y, zl in sec])
    rings = m.loft(sections, cap0=False, cap1=False)
    tip = m.add((xr, 0.0, RETURN_END - END_R))
    last = rings[-1]
    for i in range(len(last)):
        m.face((last[i], last[(i + 1) % len(last)], tip))
    return m


def _hub():
    m = lc.Mesh()
    c45 = math.radians(45)
    rr = 0.0025
    prof = [(0.0, 0.0004), (HUB_R - 0.0005, 0.0004), (HUB_R, 0.0009), (HUB_R, HUB_TOP - rr),
            (HUB_R - rr + rr * math.cos(c45), HUB_TOP - rr + rr * math.sin(c45)),
            (HUB_R - rr, HUB_TOP), (NECK_R + 0.0004, HUB_TOP + 0.0004), (NECK_R, HUB_TOP + 0.0014),
            (NECK_R, GRIP_Z - 0.001), (0.0, GRIP_Z - 0.001)]
    m.lathe(prof, 96)
    return m


def _wear(p, n, slot):
    x, y, z = p
    r = g = 1.0
    if x > 0.035:
        r = 0.74                                       # the gripped bar
        if n[2] > 0.5 or n[0] > 0.6:
            g = 0.80                                   # front and end faces rub
    elif z < 0.013:
        r = 0.92
    return (r, g, 1.0)


def build(kit):
    hub = _hub().to_object(kit, "lever hub", lc.CHROME)
    lc.boolean(kit, hub, _arm(), op="UNION")
    lc.finish_part(hub, _wear)

    kit.anchor("spindle", lc.U(0.0, 0.0, 0.0))
    kit.anchor("grip_press", lc.U(0.070, 0.0, GRIP_Z + SEC_D / 2))
    kit.anchor("grip_end", lc.U(GRIP_LEN, 0.0, GRIP_Z - BEND_R))
    kit.anchor("axis_out", lc.U(0.0, 0.0, 0.10))
    lc.common_meta(kit, {"type": "rotate", "axis": [0, 0, -1], "min": 0, "max": 35, "rest": 0,
                         "note": "+angle = grip end down (-Y); retracts the bored latch 0.013; spring back. "
                                 "Negate on mirrored (_p) placements."},
                   LOD_DISTANCES, LOD1_RATIO, LOD2_RATIO, (BUDGET, 1600, 300))
    kit.tag("lever", "lock_moving")
    assert abs(STRAIGHT_END - 0.100) < 1e-9 and abs(GRIP_Z + SEC_D / 2 - 0.054) < 1e-9
    lc.check(kit, BUDGET, (-HUB_R, -HUB_R, 0.0004), (GRIP_LEN, HUB_R, GRIP_Z + SEC_D / 2), tol=0.00015)
