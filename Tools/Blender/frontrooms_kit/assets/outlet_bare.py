"""Missing plate: a bare duplex receptacle on its strap in a ragged cut-out
(wear mesh variant, 3 % of Level 0 and 1 % of Office plates; P2).

Real-world reference: a spec-grade 5-15R duplex (Leviton 5320 class) seen
with its plate gone (outlets/01 sections 2.2 and 8): the steel strap
(yoke) 4.20 in (106.7 mm) long with its plaster ears and break-off score
notches, 1.33 in (33.8 mm) wide; the two nylon face towers rising through
it; the empty tapped hole where the plate screw went; two #6-32 box screws
at 3-9/32 in (83.3 mm) centres in the strap slots; side-wired terminal
screws, brass on the hot side and silver on the neutral side, each on its
clamp plate; a 2 x 3 in steel device box whose front edge stands 1 mm
proud of the wall; and a 3-5 mm ragged dark gap where the paper and
drywall were cut round the box.

Size: 61.9 x 106.7 mm on the wall (box cut-out width x strap length),
8.0 mm proud; the box runs 35 mm behind the wall plane (spec 1.1 allows
<= 35). ORIGIN = the wall-face point at the device centre; front -Y (Unity
+Z). Render-only.

Game read (no hole can be cut in the map wall, spec 8 item 7): a dark (PB)
sheet 0.3 mm in front of the wall fills the ragged cut-out, so the box
interior reads as a void instead of wallpaper; a torn lip 0.5-1.3 mm wide
(device ivory, the pale paper core) slopes from the cut edge down onto the
paper, so the edge reads torn rather than as a grey card. The terminal screws and
their clamp plates sit far enough forward (axis 1.5 mm behind the wall
plane) that their front 2 mm show as brass and silver glints in the gap
between the device and the box. Everything else behind the wall plane is
modelled for the day a hole can be cut (then drop the sheet).

Budget (spec 1.3): 3,600 / 1,300 / 60 tris; LOD 1.5 / 4 / 12 m. Slots:
Prop_NylonIvory (device, submesh 0), Prop_Aluminium (strap, box, box
screws, silver terminals), Prop_PlasticBlack (slots, gap, tapped hole),
Prop_Brass (contacts, hot-side terminals). No anchors for plate screws
(the plate is gone); face_top / face_bottom / plate_top (strap top).

Era fit: side-wired screw terminals and plaster ears are period; no TR
shutters, no back-wire release slots, no text, logo, UL mark or date.
"""

import math

import outlet_common as oc

NAME = "Kit_OutletBare"
LOD1 = None
LOD1_RATIO = 0.36
LOD2_RATIO = 0.017
LOD_DISTANCES = (1.5, 4.0, 12.0)
BUDGET = (3600, 1300, 60)
SMOOTH_ANGLE = oc.SMOOTH_ANGLE

FACE_TOP = 8.0                 # spec 1.3 proud
STRAP_W, STRAP_L = 33.8, 106.7
STRAP_BACK, STRAP_FRONT = 1.0, 1.9      # resting on the box ears, 1 mm proud
STRAP_R = 1.5
EAR_NOTCH_Z = 47.0             # plaster-ear break-off score notches
MOUNT_Z = 41.65                # #6-32 box screws at 83.3 centres
MOUNT_SLOT = (3.8, 6.0)
CENTRE_HOLE_D = 3.5
BODY_L = 68.0                  # nylon body behind the strap
BODY_BACK = -25.0
BOX_IN = (50.8, 76.2)          # 2 x 3 in device box
BOX_T = 1.6
BOX_FRONT = 1.0
BOX_BACK = -35.0
CUT_GAP = 4.0                  # mean ragged gap round the box (3-5 mm)
GAP_H = 0.3
LIP_W = (0.5, 1.3)             # torn paper / gypsum lip width range (mm), round 5
LIP_H = (0.08, 0.34)           # lip height: outer edge on the wall, inner edge at the cut
TERM = {"R": 3.6, "crown": 0.9, "side": 1.2, "slot": (1.0, 0.7), "axis_h": -1.5, "z": oc.OPEN_Z}
ARC_DEG_BARE = 5.625           # the faces are the hero here: 64 per circle


def ragged_outline(hw, hh, n=60, seed=7):
    """Rounded-rectangle cut-out with a torn edge: deterministic jitter."""
    base = oc.rrect(hw, hh, 3.0, 4)
    # resample to n points by arc length
    per = []
    L = 0.0
    for i in range(len(base)):
        a, b = base[i], base[(i + 1) % len(base)]
        per.append((a, b, L))
        L += math.hypot(b[0] - a[0], b[1] - a[1])
    pts = []
    for k in range(n):
        t = L * k / n
        for a, b, l0 in per:
            sl = math.hypot(b[0] - a[0], b[1] - a[1])
            if l0 <= t <= l0 + sl + 1e-9:
                f = (t - l0) / max(sl, 1e-9)
                x, z = a[0] + (b[0] - a[0]) * f, a[1] + (b[1] - a[1]) * f
                break
        # outward normal ~ from the centre, jitter in [-1, +1] mm
        hsh = math.sin(k * 12.9898 + seed * 78.233) * 43758.5453
        j = (hsh - math.floor(hsh)) * 2.0 - 1.0
        tear = 0.6 * math.sin(k * 0.9 + seed) + 0.4 * j
        d = math.hypot(x, z)
        pts.append((x + x / d * tear, z + z / d * tear))
    return pts


def torn_lip(cut, seed=11):
    """The torn paper / gypsum lip: each cut point pushed outward (radially,
    as ragged_outline jitters) by a deterministic width in LIP_W."""
    out = []
    n = len(cut)
    for k, (x, z) in enumerate(cut):
        hsh = math.sin(k * 7.137 + seed * 3.71) * 24634.6345
        j = hsh - math.floor(hsh)
        w = LIP_W[0] + (LIP_W[1] - LIP_W[0]) * (0.5 + 0.3 * math.sin(k * 2.3 + 1.0) + 0.2 * (2 * j - 1))
        d = math.hypot(x, z)
        out.append((x + x / d * w, z + z / d * w))
    assert n == len(out)
    return out


def strap_outline():
    hw, hh, r = STRAP_W / 2, STRAP_L / 2, STRAP_R
    pts = []

    def arc(cx, cz, a0):
        for k in range(5):
            a = math.radians(a0 + 90.0 * k / 4)
            pts.append((cx + r * math.cos(a), cz + r * math.sin(a)))
    notch = lambda side, z, sgn: [(side * hw, z - 0.6 * sgn), (side * (hw - 0.8), z), (side * hw, z + 0.6 * sgn)]  # noqa: E731
    pts += notch(1, -EAR_NOTCH_Z, 1) + notch(1, EAR_NOTCH_Z, 1)
    arc(hw - r, hh - r, 0)
    arc(-hw + r, hh - r, 90)
    pts += notch(-1, EAR_NOTCH_Z, -1) + notch(-1, -EAR_NOTCH_Z, -1)
    arc(-hw + r, -hh + r, 180)
    arc(hw - r, -hh + r, 270)
    return pts


def oblong(cx, cz, w, h, n=6):
    r = w / 2
    pts = []
    for i in range(n + 1):
        a = math.pi * i / n
        pts.append((cx + r * math.cos(a), cz + h / 2 - r + r * math.sin(a)))
    for i in range(n + 1):
        a = math.pi + math.pi * i / n
        pts.append((cx + r * math.cos(a), cz - h / 2 + r + r * math.sin(a)))
    return pts


def build(kit):
    oc.register_slots()
    oc.assert_shared_numbers()
    saved = oc.ARC_DEG
    oc.ARC_DEG = ARC_DEG_BARE
    try:
        k = oc.arc_k(oc.OPEN_D / 2, oc.OPEN_FLAT)
    finally:
        oc.ARC_DEG = saved
    ni, al, pb, br = oc.Mesh(), oc.Mesh(), oc.Mesh(), oc.Mesh()
    zs = (oc.OPEN_Z, -oc.OPEN_Z)
    floor_h = FACE_TOP - oc.SLOT_DEPTH                  # 1.0: the full 7.0 mm (the wall is not in the way)
    off0 = -(oc.OPEN_D / 2 - oc.FACE_D / 2)
    towers = []
    for oz in zs:
        oc.receptacle_face(ni, pb, br, oz, FACE_TOP, STRAP_FRONT, k, False, floor_h, floors=True, ground_segs=20)
        towers.append(oc.rwf(0.0, oz, oc.OPEN_D / 2, oc.OPEN_FLAT, k, off0))
    # nylon body behind the strap (seen only from the box gap)
    b_front = ni.ring(oc.rrect(STRAP_W / 2, BODY_L / 2, 2.0, 4), STRAP_BACK)
    b_back = ni.ring(oc.rrect(STRAP_W / 2, BODY_L / 2, 2.0, 4), BODY_BACK)
    ni.bridge(b_back, b_front)
    ni.fill(b_back, flip=True)
    # steel strap: front with the tower holes, the tapped centre hole and the
    # box-screw slots; edge wall; the tapped hole's wall
    so = strap_outline()
    s_front = al.ring(so, STRAP_FRONT)
    s_back = al.ring(so, STRAP_BACK)
    al.bridge(s_back, s_front)
    holes = [al.ring(t, STRAP_FRONT) for t in towers]
    ch_pts = oc.circle(0.0, 0.0, CENTRE_HOLE_D / 2, 16)
    ch = al.ring(ch_pts, STRAP_FRONT)
    ch_b = al.ring(ch_pts, STRAP_BACK)
    al.bridge(ch, ch_b)
    pb.fill(pb.ring(ch_pts, STRAP_BACK))
    slots = [al.ring(oblong(0.0, sgn * MOUNT_Z, *MOUNT_SLOT), STRAP_FRONT) for sgn in (1, -1)]
    al.fill(s_front, holes=holes + [ch] + slots)
    # box: front edge 1 mm proud, walls and back behind the wall plane
    ihw, ihh = BOX_IN[0] / 2, BOX_IN[1] / 2
    ohw, ohh = ihw + BOX_T, ihh + BOX_T
    o_f = al.ring(oc.rrect(ohw, ohh, 2.0, 4), BOX_FRONT)
    i_f = al.ring(oc.rrect(ihw, ihh, 0.8, 4), BOX_FRONT)
    al.fill(o_f, holes=[i_f])
    o_b = al.ring(oc.rrect(ohw, ohh, 2.0, 4), BOX_BACK)
    al.bridge(o_b, o_f)
    i_b = al.ring(oc.rrect(ihw, ihh, 0.8, 4), BOX_BACK)
    al.bridge(i_f, i_b)
    al.fill(i_b)
    # box screws in the strap slots (oval head, seated on the strap)
    for sgn, ang in ((1, 22.0), (-1, 104.0)):
        hm, _ = oc.screw_head(rim_segs=64, rings=((0.894, 32), (0.606, 20), (0.333, 10)), slot_angle=ang, line_segs=8)
        al.merge(hm.xform("+h", (0.0, STRAP_FRONT, sgn * MOUNT_Z)))
    # side terminals: binding heads on clamp plates; brass = hot (+x)
    for side, mesh in ((1, br), (-1, al)):
        for oz in zs:
            x_side = side * STRAP_W / 2
            plate = [mesh.add(x_side + side * 0.6, TERM["axis_h"] + dh, oz + dz) for dh, dz in ((-4, -4), (4, -4), (4, 4), (-4, 4))]
            mesh.facing(plate, (side, 0, 0))
            for (dh0, dz0), (dh1, dz1) in (((-4, -4), (4, -4)), ((4, -4), (4, 4)), ((4, 4), (-4, 4)), ((-4, 4), (-4, -4))):
                q = [mesh.add(x_side, TERM["axis_h"] + dh0, oz + dz0), mesh.add(x_side, TERM["axis_h"] + dh1, oz + dz1),
                     mesh.add(x_side + side * 0.6, TERM["axis_h"] + dh1, oz + dz1), mesh.add(x_side + side * 0.6, TERM["axis_h"] + dh0, oz + dz0)]
                mid = ((dh0 + dh1) / 2, (dz0 + dz1) / 2)
                mesh.facing(q, (0, mid[0], mid[1]))
            hm, _ = oc.screw_head(R=TERM["R"], crown=TERM["crown"], slot_w=TERM["slot"][0], slot_d=TERM["slot"][1],
                                  rim_segs=48, rings=((0.85, 24), (0.5, 12)), side=TERM["side"], line_segs=6,
                                  slot_angle=35.0 + 50.0 * (oz > 0) + 20.0 * (side > 0))
            axis = "+x" if side > 0 else "-x"
            hm.xform(axis, (x_side + side * (0.6 + TERM["side"]), TERM["axis_h"], oz))
            mesh.merge(hm)
    # the ragged dark gap: one sheet 0.3 mm in front of the wall
    cut = ragged_outline(ohw + CUT_GAP, ohh + CUT_GAP)
    pb.fill(pb.ring(cut, GAP_H))
    # torn lip round the cut-out: the pale paper core between the print and
    # the dark gap (reads "torn", not "a grey card"; device ivory slot)
    lip = torn_lip(cut)
    ni.bridge(ni.ring(lip, LIP_H[0]), ni.ring(cut, LIP_H[1]))
    # checks
    for mm_, nm in ((ni, "device"), (al, "steel"), (pb, "dark"), (br, "brass")):
        assert mm_.hmax() <= FACE_TOP + 1e-6, "%s proud %.2f" % (nm, mm_.hmax())
        assert mm_.hmin() >= BOX_BACK - 1e-6, "%s deeper than the box" % nm
    for t in towers:
        tx = [p[0] for p in t]
        assert abs(max(tx) - min(tx) - oc.FACE_D) < 0.05, "tower width"
        assert max(tx) < STRAP_W / 2, "tower wider than the strap"
    gaps = [oc.dist_poly(oc.rrect(ohw, ohh, 2.0, 4), x, z) for x, z in cut]
    assert 2.5 <= min(gaps) and max(gaps) <= 5.5, "ragged gap %.2f..%.2f" % (min(gaps), max(gaps))
    sx = [p[0] for p in so]
    sz = [p[1] for p in so]
    assert abs(max(sx) - min(sx) - STRAP_W) < 0.05 and abs(max(sz) - min(sz) - STRAP_L) < 0.05, "strap envelope"
    ohw_, ohh_ = BOX_IN[0] / 2 + BOX_T, BOX_IN[1] / 2 + BOX_T

    def wear_ni(p, n):
        x, h, z = p
        if 0.0 < h < 0.5 and (abs(x) > ohw_ or abs(z) > ohh_):
            return (1.0, 0.6, 1.0)        # torn lip: fresh, rough edge
        return oc.wear_device(p, n)
    ni.to_object(kit, "device", oc.NI, wear=wear_ni, lods="0")
    al.to_object(kit, "strap box screws", oc.AL, wear=lambda p, n: (1.0, 0.9 if p[1] > STRAP_FRONT - 0.01 else 1.0, 1.0), lods="0")
    pb.to_object(kit, "slots gap", oc.PB, wear=oc.wear_cavity, lods="0")
    br.to_object(kit, "contacts terminals", oc.BR, lods="0")
    counts = [oc.total_tris(kit, "0")]
    if oc.has_lods():
        counts += build_lods(kit, k, towers, cut)
    kit.no_collider()
    kit.tag("outlet", "outlet_receptacle", "wall_flush")
    kit.anchor("face_top", (0.0, -FACE_TOP * oc.MM, oc.OPEN_Z * oc.MM))
    kit.anchor("face_bottom", (0.0, -FACE_TOP * oc.MM, -oc.OPEN_Z * oc.MM))
    kit.anchor("plate_top", (0.0, 0.0, STRAP_L / 2 * oc.MM))
    cw = max(p[0] for p in cut) - min(p[0] for p in cut)
    oc.common_meta(kit, __import__(__name__), cw, STRAP_L, FACE_TOP, {
        "kind": "bare", "behind": -BOX_BACK * oc.MM, "screws": 0, "faces": "5-15R", "groundDown": True,
        "baseDeviceSlot": oc.NI, "basePlateSlot": None, "gapSheetH": GAP_H * oc.MM,
        "note": "drop the dark gap sheet if a real hole is ever cut in the wall"})
    oc.lod_check(NAME, counts, BUDGET)
    kit.meta["trianglesByLod"] = counts


def build_lods(kit, k, towers, cut):
    """LOD1: 24-point faces with a 2-step edge and 0.5 mm slot insets, a
    coarse strap, box edge and gap; LOD2: the strap as a quad, the faces as
    two dark 6-gons, the box edge as an 8-point ring on the dark gap."""
    ni, al, pb, br_ = oc.Mesh(), oc.Mesh(), oc.Mesh(), oc.Mesh()
    k1 = oc.LOD1_ARC_K
    for oz in zs_():
        sk = ni.ring(oc.rwf(0.0, oz, oc.OPEN_D / 2, oc.OPEN_FLAT, k1, -0.4), STRAP_FRONT)
        r1 = ni.ring(oc.rwf(0.0, oz, oc.OPEN_D / 2, oc.OPEN_FLAT, k1, -0.4), FACE_TOP - 0.45)
        r1b = ni.ring(oc.rwf(0.0, oz, oc.OPEN_D / 2, oc.OPEN_FLAT, k1, -0.55), FACE_TOP - 0.12)
        r2 = ni.ring(oc.rwf(0.0, oz, oc.OPEN_D / 2, oc.OPEN_FLAT, k1, -0.9), FACE_TOP)
        ni.bridge(sk, r1)
        ni.bridge(r1, r1b)
        ni.bridge(r1b, r2)
        holes = []
        for poly in (oc.rect(oc.NEUTRAL[2], oz + oc.NEUTRAL[3], oc.NEUTRAL[0], oc.NEUTRAL[1]),
                     oc.rect(oc.HOT[2], oz + oc.HOT[3], oc.HOT[0], oc.HOT[1]),
                     oc.ground_u(0.0, oz + oc.GROUND_Z, oc.GROUND_D, 8)):
            holes.append(ni.ring(poly, FACE_TOP))
            t = pb.ring(poly, FACE_TOP)
            b = pb.ring(poly, FACE_TOP - 0.5)
            pb.bridge(t, b)
            pb.fill(b)
        ni.fill(r2, holes=holes)
    so = oc.rrect(STRAP_W / 2, STRAP_L / 2, STRAP_R, 2)
    sf = al.ring(so, STRAP_FRONT)
    sb = al.ring(so, STRAP_BACK)
    al.bridge(sb, sf)
    th = [al.ring(oc.rwf(0.0, oz, oc.OPEN_D / 2, oc.OPEN_FLAT, k1, -0.4), STRAP_FRONT) for oz in zs_()]
    chp = oc.circle(0.0, 0.0, CENTRE_HOLE_D / 2, 8)
    ch = al.ring(chp, STRAP_FRONT)
    pb.fill(pb.ring(chp, STRAP_FRONT - 0.4))
    msl = [al.ring(oblong(0.0, sgn * MOUNT_Z, MOUNT_SLOT[0], MOUNT_SLOT[1], 3), STRAP_FRONT) for sgn in (1, -1)]
    al.fill(sf, holes=th + [ch] + msl)
    # box screws: 16-point domes; terminal heads: 16-point binding heads on plates
    for sgn in (1, -1):
        rim = al.ring(oc.circle(0.0, sgn * MOUNT_Z, 3.3, 16), STRAP_FRONT)
        mid = al.ring(oc.circle(0.0, sgn * MOUNT_Z, 2.0, 16), STRAP_FRONT + 0.75)
        al.bridge(rim, mid)
        al.fan(mid, al.add(0.0, STRAP_FRONT + 1.0, sgn * MOUNT_Z))
    for side, mesh in ((1, br_), (-1, al)):
        for oz in zs_():
            hm = oc.Mesh()
            low = hm.ring(oc.circle(0.0, 0.0, TERM["R"], 16), -TERM["side"])
            rim = hm.ring(oc.circle(0.0, 0.0, TERM["R"], 16), 0.0)
            mid = hm.ring(oc.circle(0.0, 0.0, TERM["R"] * 0.6, 16), TERM["crown"] * 0.65)
            hm.bridge(low, rim)
            hm.bridge(rim, mid)
            hm.fan(mid, hm.add(0.0, TERM["crown"], 0.0))
            hm.xform("+x" if side > 0 else "-x", (side * (STRAP_W / 2 + 0.6 + TERM["side"]), TERM["axis_h"], oz))
            mesh.merge(hm)
            x0 = side * STRAP_W / 2
            q = [mesh.add(x0 + side * 0.6, TERM["axis_h"] + dh, oz + dz) for dh, dz in ((-4, -4), (4, -4), (4, 4), (-4, 4))]
            mesh.facing(q, (side, 0, 0))
            q2 = [mesh.add(x0, TERM["axis_h"] + 4, oz - 4), mesh.add(x0 + side * 0.6, TERM["axis_h"] + 4, oz - 4),
                  mesh.add(x0 + side * 0.6, TERM["axis_h"] + 4, oz + 4), mesh.add(x0, TERM["axis_h"] + 4, oz + 4)]
            mesh.facing(q2, (0, 1, 0))
    ohw, ohh = BOX_IN[0] / 2 + BOX_T, BOX_IN[1] / 2 + BOX_T
    of = al.ring(oc.rrect(ohw, ohh, 2.0, 1), BOX_FRONT)
    inn = al.ring(oc.rrect(BOX_IN[0] / 2, BOX_IN[1] / 2, 0.8, 1), BOX_FRONT)
    al.fill(of, holes=[inn])
    o0 = al.ring(oc.rrect(ohw, ohh, 2.0, 1), 0.0)
    al.bridge(o0, of)
    i0 = al.ring(oc.rrect(BOX_IN[0] / 2, BOX_IN[1] / 2, 0.8, 1), GAP_H)
    al.bridge(inn, i0)
    pb.fill(pb.ring(cut, GAP_H))
    ni.to_object(kit, "device LOD1", oc.NI, wear=oc.wear_device, lods="1")
    al.to_object(kit, "steel LOD1", oc.AL, lods="1")
    pb.to_object(kit, "dark LOD1", oc.PB, wear=oc.wear_cavity, lods="1")
    br_.to_object(kit, "brass LOD1", oc.BR, lods="1")
    ni2, al2, pb2 = oc.Mesh(), oc.Mesh(), oc.Mesh()
    q = al2.ring(oc.rect(0.0, 0.0, STRAP_W, STRAP_L), STRAP_FRONT)
    al2.fill(q)
    for oz in zs_():
        hexa = ni2.ring([(16.0 * math.cos(math.radians(a)), oz + 13.6 * math.sin(math.radians(a))) for a in (0, 60, 120, 180, 240, 300)], FACE_TOP)
        ni2.fill(hexa)
        dk = pb2.ring([(9.0 * math.cos(math.radians(a)), oz - 2.0 + 7.0 * math.sin(math.radians(a))) for a in (0, 60, 120, 180, 240, 300)], FACE_TOP + 0.3)
        pb2.fill(dk)
    ring8 = [(ohw, -ohh + 3), (ohw, ohh - 3), (ohw - 3, ohh), (-ohw + 3, ohh), (-ohw, ohh - 3), (-ohw, -ohh + 3), (-ohw + 3, -ohh), (ohw - 3, -ohh)]
    o8 = al2.ring(ring8, BOX_FRONT)
    i8 = al2.ring([(x * 0.94, z * 0.96) for x, z in ring8], BOX_FRONT)
    al2.bridge(i8, o8, flip=True)
    o8w = al2.ring(ring8, 0.0)
    al2.bridge(o8w, o8)
    pb2.fill(pb2.ring(cut[::6], GAP_H))
    ni2.to_object(kit, "faces LOD2", oc.NI, lods="2")
    al2.to_object(kit, "steel LOD2", oc.AL, lods="2")
    pb2.to_object(kit, "dark LOD2", oc.PB, lods="2")
    return [oc.total_tris(kit, "1"), oc.total_tris(kit, "2")]


def zs_():
    return (oc.OPEN_Z, -oc.OPEN_Z)
