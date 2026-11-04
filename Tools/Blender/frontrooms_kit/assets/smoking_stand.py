"""Kit_SmokingStand: the lobby smoking stand under the NO SMOKING sign (游戏视觉, bs4-hero-kits, builder A;
research/station/72_hero_kits.md §2.2). New in Station_Waiting_3x2 (S wall, x 4.60; absent in BS4).

Period: 01 §2, an ashtray under a NO SMOKING sign is the period irony (workplace smoking rules spreading 1986-90,
P67-P69, while the lobby ashtray often stayed); 30_look S25, a floor stand of about 0.6 m, chrome with a black bowl,
and butts; typology [memory]: the US office smoking stand of the 1950s-80s, a weighted round base, a chrome column, a
top bowl with cigarette rests.

Unity metres, origin on the floor at the centre, front +Z (a rest faces front); 0.62 m tall overall:
* base: black enamelled spun-steel dome D0.33 x 0.044 with a 6 mm rolled bead, on a foot ring D0.31 x 6 mm (inset
  10 mm, the dark gap under the rim; in the base's slot, 72 second pass);
* chrome base collar D54 x 22 with two turned beads; chrome column 1.25 in (31.75 mm) OD, 0.50 m; chrome top collar
  D44 x 18 under the bowl's socket;
* bowl: black spun steel, D0.26 at the rim, 0.07 deep; its chrome rolled rim (bead D9) dips into three U-notch rests
  (12 mm wide, 10 mm deep) at 120 degrees;
* a removable chrome ash pan D0.22, 25 mm deep, 5 mm lip; in it an ash bed about 6 mm deep with two soft heaps (no
  finger dents: those belong to a sand urn), 4 butts D7.8 (25-40 mm of paper and a 20 mm cork-tipped filter; two
  stubbed and bent 20-40 degrees, one long with a grey ash tip) and one bent, burnt paper match; two tapped ash
  columns lying whole on the bed, five charred paper flecks and three tobacco crumbs by the crushed butts (the
  first look read the bed as a clean white plate: the variety is geometry, as the shader ignores vertex colour).

Slots: Prop_Chrome, Prop_SteelBlack (base, ring, bowl, the match head), Prop_AshSand, Prop_Paper (butt paper, the
match), Prop_Cork (filter tipping): 5 draws at LOD0 (the butts need paper and cork, 72 §2.2); LOD1 drops the butts
and the match (3); LOD2 is three 8-sided lathes (2). Large lathes 40 sides at LOD0, 20 at LOD1.
Budget 3,000 / 1,000 / 150 (cap 4,000); lodDistances [4, 10, 40]; collider (0, 0.31, 0) 0.33 x 0.62 x 0.33.
"""

import math

from mathutils import Vector

import _station_waiting_common as w

NAME = "Kit_SmokingStand"
SMOOTH_ANGLE = w.SMOOTH

CHROME, STEEL, ASH, PAPER, CORK = w.CHROME, w.STEEL, w.ASH, w.PAPER, w.CORK
RIM_Y = 0.6155            # bead top 0.62
NOTCH_HALF, NOTCH_FALL, NOTCH_DEPTH = 0.006, 0.0035, 0.010


def notch_angles(n_base, r):
    """Even angles plus extra samples across the three rests (Unity: angle 0 = +x, 90 = +z, the front)."""
    centres = [math.radians(90.0 + 120.0 * k) for k in range(3)]
    half, fall = NOTCH_HALF / r, NOTCH_FALL / r
    base = [2 * math.pi * i / n_base for i in range(n_base)]

    def dist(a):
        return min(abs((a - c + math.pi) % (2 * math.pi) - math.pi) for c in centres)
    angs = [a for a in base if dist(a) > half + fall + 0.02]
    for c in centres:
        for d in (-half - fall, -half, -half * 0.4, half * 0.4, half, half + fall):
            angs.append((c + d) % (2 * math.pi))
    angs.sort()

    def dip(a):
        d = dist(a)
        if d >= half + fall:
            return 0.0
        if d <= half:
            return 1.0
        return 0.5 * (1 + math.cos(math.pi * (d - half) / fall))
    return angs, dip


def butt(kit, path, paper_len, r=0.0039, sides=8, ash_tip=0.0, crushed=0.0, name="butt"):
    """A cigarette end along a path (filter first): 20 mm cork-tipped filter, then paper, then an optional ash
    tip; a crushed end pinches the last ring."""
    pts = [Vector(p) for p in path]
    # resample: filter end at pts[0]; total length = 0.020 + paper_len (+ ash)
    seg = []
    for a, b in zip(pts, pts[1:]):
        seg.append((a, b, (b - a).length))
    total = sum(s[2] for s in seg)

    def at(d):
        d = max(0.0, min(total, d))
        for a, b, L in seg:
            if d <= L:
                return a + (b - a) * (d / L)
            d -= L
        return pts[-1]
    f_end = 0.020
    p_end = 0.020 + paper_len
    fpath = [tuple(at(0.0)), tuple(at(f_end * 0.5)), tuple(at(f_end))]
    w.tube(kit, name + " filter", CORK, fpath, r, sides, caps="fan")
    n = max(2, int(paper_len / 0.012) + 1)
    ppath = [tuple(at(f_end + (p_end - f_end) * k / n)) for k in range(n + 1)]
    o = w.tube(kit, name + " paper", PAPER, ppath, r * 1.002, sides, caps="fan")
    if crushed > 0:
        # pinch the burnt end: scale the last ring flat
        import bmesh
        bm = bmesh.new()
        bm.from_mesh(o.data)
        end = Vector(w.B(*ppath[-1]))
        for v in bm.verts:
            dd = (v.co - end).length
            if dd < r * 2.2:
                v.co = end + (v.co - end) * (1.0 - crushed) + Vector((0, 0, -crushed * 0.0015))
        bm.to_mesh(o.data)
        bm.free()
    if ash_tip > 0:
        apath = [tuple(at(p_end)), tuple(at(p_end + ash_tip))]
        w.tube(kit, name + " ash", ASH, apath, r * 0.97, sides, caps="fan")


def build_level(kit, lv):
    big = {0: 40, 1: 20, 2: 8}[lv]
    small = {0: 24, 1: 12, 2: 8}[lv]
    if lv == 2:
        w.lathe(kit, "base lod2", STEEL, [(0.165, 0.004), (0.165, 0.012), (0.03, 0.048), (0.0, 0.048)], verts=8)
        w.lathe(kit, "column lod2", CHROME, [(0.016, 0.048), (0.016, 0.55)], verts=8)
        w.lathe(kit, "bowl lod2", STEEL, [(0.02, 0.537), (0.06, 0.551), (0.122, 0.580), (0.131, 0.615), (0.0, 0.612)], verts=8)
        return
    # foot ring and dome
    if lv == 0:
        w.lathe(kit, "foot ring", STEEL, [(0.146, 0.0), (0.1552, 0.0008), (0.1552, 0.0072)], verts=16)
        w.lathe(kit, "base dome", STEEL, [(0.1560, 0.0060), (0.1640, 0.0072), (0.1652, 0.0098), (0.1628, 0.0124),
                                         (0.1560, 0.0132), (0.1280, 0.0216), (0.0880, 0.0336), (0.0500, 0.0436),
                                         (0.0270, 0.0478)], verts=big)
        w.lathe(kit, "base collar", CHROME, [(0.0270, 0.0476), (0.0273, 0.0515), (0.0249, 0.0545), (0.0246, 0.0608),
                                            (0.0263, 0.0638), (0.0232, 0.0688), (0.0159, 0.0702)], verts=small)
    else:
        w.lathe(kit, "foot ring", STEEL, [(0.146, 0.0), (0.155, 0.0072)], verts=16)
        w.lathe(kit, "base dome", STEEL, [(0.1560, 0.0060), (0.1650, 0.0098), (0.1560, 0.0132), (0.0880, 0.0336),
                                         (0.0270, 0.0478)], verts=big)
        w.lathe(kit, "base collar", CHROME, [(0.0270, 0.0476), (0.0262, 0.0638), (0.0159, 0.0702)], verts=small)
    w.lathe(kit, "column", CHROME, [(0.015875, 0.069), (0.015875, 0.520)], verts=small)
    if lv == 0:
        w.lathe(kit, "top collar", CHROME, [(0.0159, 0.5190), (0.0206, 0.5196), (0.0220, 0.5216), (0.0220, 0.5344),
                                           (0.0206, 0.5368), (0.0180, 0.5372)], verts=small)
        w.lathe(kit, "bowl socket", STEEL, [(0.0180, 0.5368), (0.0201, 0.5382), (0.0201, 0.5488), (0.0320, 0.5510)], verts=20)
        w.lathe(kit, "bowl", STEEL, [(0.0300, 0.5506), (0.0750, 0.5524), (0.1050, 0.5622), (0.1220, 0.5800), (0.1276, 0.6002)],
                verts=big)
        # the top band of the bowl and the rolled chrome rim follow the three rests
        angs, dip = notch_angles(big, 0.127)

        def dy_band(a, i):
            k = (0.0, 1.0, 1.0)[i] if i < 3 else 0.0
            return -NOTCH_DEPTH * dip(a) * k

        w.lathe(kit, "bowl band", STEEL, [(0.1272, 0.5960), (0.1281, 0.6050), (0.1262, 0.6066)], angles=angs, dy=dy_band)
        rim = [(0.1254, 0.6080), (0.1262, 0.6128), (0.1290, 0.6153), (0.1322, 0.6140), (0.1334, 0.6104), (0.1318, 0.6066)]

        def dy_rim(a, i):
            return -NOTCH_DEPTH * dip(a)
        w.lathe(kit, "rim bead", CHROME, rim, angles=angs, dy=dy_rim)
        # bowl inside (above the pan) and the pan
        w.lathe(kit, "bowl inside", STEEL, [(0.1252, 0.6082), (0.1180, 0.5975), (0.1140, 0.5935)], angles=angs,
                dy=lambda a, i: -NOTCH_DEPTH * dip(a) * (1.0 if i == 0 else 0.0))
        w.lathe(kit, "ash pan", CHROME, [(0.1160, 0.6040), (0.1150, 0.6062), (0.1128, 0.6066), (0.1105, 0.6050),
                                        (0.1080, 0.5960)], verts=big)
        # ash bed: about 6 mm over the pan floor, two soft heaps
        heaps = ((math.radians(30.0), 0.0040, 0.55), (math.radians(215.0), 0.0028, 0.45))

        def ash_dy(a, i):
            k = (0.0, 0.7, 1.0, 0.8)[i] if i < 4 else 0.0
            h = 0.0
            for (c, amp, wd) in heaps:
                d = abs((a - c + math.pi) % (2 * math.pi) - math.pi)
                h += amp * math.exp(-(d / wd) ** 2)
            return h * k + 0.0004 * math.sin(5 * a) * k
        w.lathe(kit, "ash bed", ASH, [(0.1088, 0.5985), (0.080, 0.6000), (0.045, 0.6003), (0.015, 0.6004), (0.0, 0.6004)],
                verts=24, dy=ash_dy)
        # butts: two stubbed and bent, one long with an ash tip, one plain
        y0 = 0.6045
        butt(kit, [(0.040, y0 + 0.001, 0.030), (0.020, y0, 0.012), (0.012, y0 + 0.002, -0.012)], 0.022, crushed=0.45, name="butt a")
        butt(kit, [(-0.060, y0 + 0.003, 0.050), (-0.036, y0 + 0.001, 0.035), (-0.030, y0 + 0.004, 0.010)], 0.028, crushed=0.35,
             name="butt b")
        butt(kit, [(-0.020, y0 + 0.002, -0.055), (0.030, y0 + 0.001, -0.068)], 0.040, ash_tip=0.014, name="butt c")
        butt(kit, [(0.072, y0 + 0.004, -0.010), (0.078, y0 + 0.002, 0.028)], 0.026, name="butt d")
        # a bent paper match, its head and the burnt half black
        mp = [(-0.070, y0 + 0.0004, -0.020), (-0.052, y0 + 0.0008, -0.030), (-0.032, y0 + 0.0040, -0.034)]
        w.bar(kit, "match", PAPER, mp, 0.0030, 0.0007, r=0.0002, steps=1, side=(0, 0, 1), chamfer=0.0)
        w.bar(kit, "match burnt", STEEL, [(-0.032, y0 + 0.0040, -0.034), (-0.022, y0 + 0.0052, -0.036)], 0.0026, 0.0006,
              r=0.0002, steps=1, side=(0, 0, 1), chamfer=0.0)
        w.lathe(kit, "match head", STEEL, [(0.0, -0.0021), (0.0018, -0.0012), (0.0019, 0.0010), (0.0, 0.0024)],
                base=(-0.0205, y0 + 0.0053, -0.0363), axis=(0.98, 0.12, -0.18), verts=6)
        # what makes a bed of ash read as ash at 0.35 m (geometry: the Surface shader ignores vertex colour):
        # two tapped ash columns lying whole on the bed, charred paper flecks, tobacco crumbs by the crushed butts
        w.tube(kit, "ash log", ASH, [(0.050, 0.6036, 0.050), (0.060, 0.6037, 0.039), (0.067, 0.6035, 0.030)], 0.0034, 8,
               caps="fan")
        w.tube(kit, "ash log", ASH, [(-0.078, 0.6038, 0.020), (-0.071, 0.6037, 0.005)], 0.0031, 8, caps="fan")
        flecks = (((0.000, 0.6007, 0.045), 0.0034, 0.3), ((-0.040, 0.6006, -0.008), 0.0026, 1.4),
                  ((-0.060, 0.6003, -0.060), 0.0040, 2.2), ((0.090, 0.5995, 0.040), 0.0022, 0.9),
                  ((0.004, 0.6016, -0.030), 0.0018, 2.9))
        for (c, rr, ph) in flecks:
            pts = []
            for k in range(5):
                a = ph + 2 * math.pi * k / 5
                f = (1.0, 0.62, 0.95, 0.55, 0.85)[k]
                pts.append((c[0] + rr * f * math.cos(a), c[1] + 0.0004 * (k % 2), c[2] + rr * f * math.sin(a)))
            w.mesh_part(kit, "char fleck", STEEL, pts, [(0, 1, 2, 3, 4)], face_dirs=[(0.0, 1.0, 0.0)])
        for (c, rr) in (((0.008, 0.6015, -0.016), 0.0013), ((0.014, 0.6014, -0.021), 0.0010), ((-0.027, 0.6016, 0.006), 0.0012)):
            w.lathe(kit, "tobacco crumb", CORK, [(rr, -0.0003), (rr * 0.8, rr * 0.7), (0.0, rr)], base=c, verts=4)
    else:
        w.lathe(kit, "top collar", CHROME, [(0.0159, 0.5190), (0.0220, 0.5216), (0.0220, 0.5344), (0.0180, 0.5372)], verts=small)
        w.lathe(kit, "bowl", STEEL, [(0.0180, 0.5368), (0.0201, 0.5488), (0.0750, 0.5524), (0.1220, 0.5800), (0.1281, 0.6050),
                                     (0.1252, 0.6082), (0.1140, 0.5935)], verts=big)
        w.lathe(kit, "rim bead", CHROME, [(0.1254, 0.6080), (0.1290, 0.6153), (0.1334, 0.6104), (0.1318, 0.6066)], verts=big)
        w.lathe(kit, "ash pan", CHROME, [(0.1160, 0.6040), (0.1128, 0.6066), (0.1080, 0.5960)], verts=big)
        w.lathe(kit, "ash bed", ASH, [(0.1088, 0.5985), (0.0, 0.6010)], verts=big)


def build(kit):
    w.register()
    for lv in (0, 1, 2):
        with w.Level(kit, str(lv)):
            build_level(kit, lv)
    w.tris_report(kit, "pre-finish")
    w.finish_parts(kit, SMOOTH_ANGLE)
    kit.collider(w.B(0.0, 0.31, 0.0), w.BS(0.33, 0.62, 0.33))
    kit.anchor("bowl", w.B(0.0, 0.60, 0.0))
    kit.tag("station", "smoking")
    w.sc.lods(kit, 4.0, 10.0, 40.0, (3000, 1000, 150))
    w.station_block(kit, "floor, centred", "front +Z (one rest faces front); stands 0.17 m off the S wall in the module")
