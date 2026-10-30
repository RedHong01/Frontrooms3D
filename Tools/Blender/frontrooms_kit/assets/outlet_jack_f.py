"""Kit_JackF: a cable-TV F-connector jack on a 1-gang ivory thermoset plate, for the station set's coax (30L §10.2,
§10.3 S37 "an F-jack plate"; plan 70 P27, GAP 34).

A variant of outlet_jack_bnc.py (Kit_JackBNC): the same plate, washer and hex-nut construction, with a 3/8-32
threaded F barrel (Ø 9.5 mm, 7/16 in nut) instead of the bayonet sleeve and studs, a white insulator and the centre
contact hole. 1990: cable TV F jacks were the domestic and institutional standard [memory]. No text, logos or dates.
ORIGIN = the wall-face point at the plate centre (centre-origin, as every outlet kit): the module sets p.y = 0.30.
"""

import math
import sys

import outlet_plate_common as pc

NAME = "Kit_JackF"
LOD1 = None
BUDGET = (2700, 1000, 56)
LOD1_RATIO = BUDGET[1] / BUDGET[0]
LOD2_RATIO = BUDGET[2] / BUDGET[0]
LOD_DISTANCES = pc.LOD_DISTANCES
SMOOTH_ANGLE = pc.SMOOTH_ANGLE

# ------------------------------------------------ O2 design numbers (mm)
SEGS = 36
WASHER_R, WASHER_T = 6.75, 0.6          # flat washer OD 13.5
NUT_AF, NUT_T = 11.1, 2.4               # 7/16 in hex nut
NUT_FACE_R = 5.30                       # chamfer circle on the nut face (0.95 AF/2)
SLEEVE_R = 4.75                         # Ø 9.5 bayonet sleeve (spec §1.3)
THREAD_R = 4.75                         # 3/8-32 thread showing in front of the nut
BORE_R = 4.15                           # the F barrel bore is BORE_R - 0.9
FRONT = 15.5
BORE_DEPTH = 2.6
INSUL_R, INSUL_H = 3.0, 0.5
SOCKET = (1.0, 0.5, 0.15)               # outer r, hole r, proud of the insulator
STUD_R, STUD_OUT = 0.65, 1.1            # Ø 1.3 studs, 1.1 past the sleeve
STUD_FROM_FRONT = 2.2


def hex_r(theta, af):
    """Radius of a hex with flats ``af`` apart and corners at 0, 60, ... deg."""
    a = math.degrees(theta) % 60.0 - 30.0
    return af / 2 / math.cos(math.radians(a))


def nut(m, h0, h1, segs, face_r, chamfer_rings):
    """Hex nut rings: r(theta, h) = min(hex, chamfer cone) so the 30-degree
    front chamfer cuts the corners into arcs; then the face annulus."""
    t60 = math.tan(math.radians(60))
    rings = []
    for h in [h0] + chamfer_rings + [h1]:
        pts = []
        for k in range(segs):
            th = 2 * math.pi * k / segs
            r = min(hex_r(th, NUT_AF), face_r + (h1 - h) * t60)
            pts.append((r * math.cos(th), r * math.sin(th)))
        rings.append(m.ring(pts, h))
    m.chain(rings)
    return rings


def build(kit):
    pc.begin(kit)
    W, H = pc.PLATE_1G
    screws = [(0.0, pc.BLANK_SCREW_Z), (0.0, -pc.BLANK_SCREW_Z)]
    # No through-opening: the Ø 9.8 D-hole hides under the washer.
    info = pc.thermoset_plate(kit, W, H, screws, [], wear=pc.plate_wear(W, H, [(0.0, 0.0, WASHER_R, WASHER_R)], margin=12.0))
    f = info["hf"](0.0, 0.0)
    w0, w1 = f - 0.05, f - 0.05 + WASHER_T
    h0, h1 = w1, w1 + NUT_T
    # Chrome: washer, nut, sleeve with one thread groove, studs.
    c = pc.Mesh()
    c.lathe(0.0, 0.0, [(WASHER_R, w0), (WASHER_R, w1), (5.9, w1)], SEGS)
    nr = nut(c, h0, h1, SEGS, NUT_FACE_R, [h1 - 0.75, h1 - 0.35])
    face_in = c.ring(pc.circle(0.0, 0.0, THREAD_R, SEGS), h1)
    c.bridge(nr[-1], face_in)
    prof = [(THREAD_R, h1)]
    pitch = 25.4 / 32
    z = h1 + 0.2
    while z < FRONT - 0.6:
        prof += [(THREAD_R, z), (THREAD_R - 0.35, z + pitch / 2)]
        z += pitch * 2
    prof += [(THREAD_R, z), (THREAD_R, FRONT - 0.3), (4.45, FRONT), (BORE_R - 0.9, FRONT), (BORE_R - 0.9, FRONT - BORE_DEPTH)]
    c.lathe(0.0, 0.0, prof, SEGS)
    wear = lambda p, n, s: (0.85 if p[1] > FRONT - 3.0 else 1.0, 0.85 if (p[1] > FRONT - 0.4 or math.hypot(p[0], p[2]) > 6.3) else 1.0, 1.0)
    c.to_object(kit, "f chrome", pc.CH, wear=wear)
    bf = FRONT - BORE_DEPTH
    d = pc.Mesh()
    d.lathe(0.0, 0.0, [(BORE_R - 0.9, bf), (INSUL_R, bf)], SEGS)
    d.lathe(0.0, 0.0, [(SOCKET[1], bf - 0.8), (0.0, bf - 0.8)], 16)
    d.to_object(kit, "f bore", pc.PB, wear=pc.uniform_wear(1.0, 1.0, 0.2))
    t = pc.Mesh()
    t.lathe(0.0, 0.0, [(INSUL_R, bf), (INSUL_R, bf + INSUL_H), (SOCKET[0], bf + INSUL_H)], 24)
    t.to_object(kit, "f insulator", pc.TI)
    b = pc.Mesh()
    # The socket wall starts 0.1 inside the insulator: its 16-gon and the
    # insulator's 24-gon hole do not share edges, so a socket that only met
    # the insulator top left 0.01 mm slivers open onto culled back faces.
    b.lathe(0.0, 0.0, [(SOCKET[0], bf + INSUL_H - 0.1), (SOCKET[0], bf + INSUL_H), (SOCKET[0] - 0.1, bf + INSUL_H + SOCKET[2]),
                       (SOCKET[1], bf + INSUL_H + SOCKET[2]), (SOCKET[1], bf - 0.8)], 16)
    b.to_object(kit, "f socket", pc.BR)
    if pc.HAS_LODS:
        pc.plate_lod1(kit, W, H, screws, [])
        pc.plate_lod2(kit, W, H, crown_fan=True)
        c1 = pc.Mesh()
        c1.lathe(0.0, 0.0, [(WASHER_R, w0), (WASHER_R, w1), (5.9, w1)], 24)
        n1 = nut(c1, h0, h1, 24, NUT_FACE_R, [h1 - 0.6])
        c1.bridge(n1[-1], c1.ring(pc.circle(0.0, 0.0, THREAD_R, 24), h1))
        c1.lathe(0.0, 0.0, [(THREAD_R, h1), (SLEEVE_R, h1 + 1.0), (SLEEVE_R, FRONT - 0.3), (4.5, FRONT), (BORE_R, FRONT), (BORE_R, FRONT - 1.0)], 24)
        c1.to_object(kit, "f chrome lod1", pc.CH, lods="1")
        d1 = pc.Mesh()
        d1.lathe(0.0, 0.0, [(BORE_R, FRONT - 1.0), (INSUL_R, FRONT - 1.0)], 24)
        d1.to_object(kit, "f bore lod1", pc.PB, lods="1")
        t1 = pc.Mesh()
        t1.lathe(0.0, 0.0, [(INSUL_R, FRONT - 1.0), (0.6, FRONT - 1.0)], 24)
        t1.to_object(kit, "f insulator lod1", pc.TI, lods="1")
        pc.dark_poly(kit, pc.circle(0.0, 0.0, 0.6, 8), FRONT - 1.0, "f socket lod1", "1", slot=pc.BR)
        c2 = pc.Mesh()
        n2 = [c2.ring([(hex_r(math.radians(60 * k), NUT_AF) * math.cos(math.radians(60 * k)),
                        hex_r(math.radians(60 * k), NUT_AF) * math.sin(math.radians(60 * k))) for k in range(6)], hh) for hh in (w0, h1)]
        c2.chain(n2)
        c2.cap(n2[-1])
        s2 = [c2.ring(pc.circle(0.0, 0.0, SLEEVE_R, 6, math.radians(30)), hh) for hh in (h1, FRONT)]
        c2.chain(s2)
        c2.cap(s2[-1])
        c2.to_object(kit, "f lod2", pc.CH, lods="2")
    print("[o2] %s F jack: nut AF %.1f, thread Ø %.1f, front %.1f mm, bore %.1f deep" % (kit.name, NUT_AF, 2 * SLEEVE_R, FRONT, BORE_DEPTH))
    pc.end(kit, sys.modules[__name__], "outlet_jack", W, H, pc.PROUD_DATA, info["seats"],
           extra={"jack": "F", "threadD": 2 * THREAD_R})
