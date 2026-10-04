"""Four-prong telephone jack, square surface block (Western Electric 404A
type, about 1960), brown: a second-hand relic left on a 1990 wall after the
1970s switch to modular jacks ("the standard line connection for all
portable telephone sets until the conversion to modular jacks in the
1970s", outlets 01 §5.1, S25). P2.

Real reference: a square brown Bakelite-look block screwed to the wall or
baseboard, a cover on a base (the seam shows), four prong holes for the
283B plug in a polarised pattern, one cover screw. The hole pattern here is
a reading (a trapezoid: the top pair 14 mm apart, the bottom pair 19 mm,
the bottom-right hole larger): UNVERIFIED, media_candidates.md om07
(Western Electric 404A and 283B, ca. 1960) is the check.

Size 52 x 52 x 18 mm (10_spec §1.3). ORIGIN = the wall-face point at the
block centre; the back is the plane y = 0; front -Y (Unity +Z). No plate,
so no screw_k anchors: the cover screw is part of the mesh (brass), and R3
must not draw Kit_OutletScrew on this kit (sidecar outlet.screws = 0).
plate_top = the block's top edge on the wall.

Budget (10_spec §1.3) LOD0 1,800 / LOD1 700 / LOD2 40, 1.5 / 4 / 12 m;
LOD1/LOD2 hand-built, exported once make_lods exists (P-1/P-1b).
Slots: Prop_Ceramic (the existing brown slot), Prop_PlasticBlack (hole
depths), Prop_Brass (contact springs and the cover screw). Render-only.
Proud 18 mm: over the 7.5 plate limit by design (a surface block), under
the 25 mm jack limit and the 30 mm furniture stand-off.

Era fit: 1950s-60s hardware, allowed as second-hand stock by the era lock
(oldest ~1955); no maker marks, no "Bell System" text, no dates.
"""

import math
import sys

import outlet_plate_common as pc

NAME = "Kit_JackPhone4Prong"
LOD1 = None
BUDGET = (1800, 700, 40)
LOD1_RATIO = BUDGET[1] / BUDGET[0]
LOD2_RATIO = BUDGET[2] / BUDGET[0]
LOD_DISTANCES = pc.LOD_DISTANCES
SMOOTH_ANGLE = pc.SMOOTH_ANGLE

# ------------------------------------------------ O2 design numbers (mm)
SIZE = 52.0
PLAN_R = 6.0
PSEGS = 8                     # per plan corner (r 6: sagitta 0.058 mm)
HEIGHT = 18.0
TOP_ROUND = 4.0
GROOVE = (5.2, 5.9, 0.4)      # cover/base seam: h from, h to, depth
HOLES = ((-7.0, 7.0, 1.8), (7.0, 7.0, 1.8), (-9.5, -7.0, 1.8), (9.5, -7.0, 2.2))   # x, z, r
FACE_SUPPORT = 1.2
HOLE_SEGS = 12                # r 1.8: sagitta 0.061 mm
HOLE_CHAMFER = 0.3
BROWN_DEPTH = 2.5             # brown hole wall below the face, then dark
SPRING = (0.4, 3.5, 3.5)      # contact spring: wall inset, depth of its top below the face, length
HOLE_FLOOR = 11.0
CBORE_R, CBORE_D = 3.5, 1.5   # cover-screw counterbore
HEAD = (2.75, 0.8, 0.35, 0.7, 0.65)   # r, cylinder height, crown, slot w, slot depth
HEAD_SEGS = 32


def walk():
    w = [(0.0, 0.0), (0.25, GROOVE[0]), (0.25 + GROOVE[2], GROOVE[0] + 0.2), (0.25 + GROOVE[2], GROOVE[1] - 0.0),
         (0.3, GROOVE[1] + 0.2)]
    w += pc.convex_walk(GROOVE[1] + 0.2, HEIGHT, 0.5, TOP_ROUND, 4)[1:]
    return w


def build(kit):
    pc.begin(kit)
    S = SIZE
    m = pc.Mesh()
    outline = pc.rr_outline(0.0, 0.0, S / 2, S / 2, PLAN_R, PSEGS, rmin=1.5)
    rings = m.body(outline, walk())
    # Support ring 1.2 mm inside the face edge: the smooth top round only
    # tilts the normals of this strip, not the long face triangles.
    face = m.ring(outline(TOP_ROUND + 0.5 + FACE_SUPPORT), HEIGHT)
    m.bridge(rings[-1], face)
    hole_tops = []
    d, b = pc.Mesh(), pc.Mesh()
    for x, z, r in HOLES:
        hr = m.lathe(x, z, [(r + HOLE_CHAMFER, HEIGHT), (r, HEIGHT - HOLE_CHAMFER), (r, HEIGHT - BROWN_DEPTH)], HOLE_SEGS)
        hole_tops.append(hr[0])
        top = HEIGHT - SPRING[1]
        d.lathe(x, z, [(r, HEIGHT - BROWN_DEPTH), (r, top)], HOLE_SEGS)
        b.lathe(x, z, [(r, top), (r - SPRING[0], top), (r - SPRING[0], top - SPRING[2])], HOLE_SEGS)
        d.lathe(x, z, [(r - SPRING[0], top - SPRING[2]), (0.0, top - SPRING[2])], HOLE_SEGS)
    cb = m.lathe(0.0, 0.0, [(CBORE_R, HEIGHT), (CBORE_R, HEIGHT - CBORE_D), (HEAD[0] - 0.1, HEIGHT - CBORE_D)], HEAD_SEGS)   # floor runs under the head: no see-through ring
    hole_tops.append(cb[0])
    m.fill(face, hole_tops)
    wear = lambda p, n, s: (0.85 if (p[1] > HEIGHT - 0.5 and math.hypot(p[0], p[2]) < 16.0) else 1.0,
                            0.85 if (p[1] > HEIGHT - TOP_ROUND and p[1] < HEIGHT - 0.3) else 1.0, 1.0)
    m.to_object(kit, "block", pc.CE, wear=wear)
    d.to_object(kit, "prong holes", pc.PB, wear=pc.uniform_wear(1.0, 1.0, 0.2))
    h0 = HEIGHT - CBORE_D
    head = pc.slotted_head(0.0, 0.0, HEAD[0], h0, h0 + HEAD[1], HEAD[2], HEAD[3], HEAD[4], HEAD_SEGS)
    # one brass part: the springs and the head
    off = len(b.v)
    b.v.extend(head.v)
    b.f.extend(tuple(i + off for i in f) for f in head.f)
    b.to_object(kit, "springs and cover screw", pc.BR, wear=lambda p, n, s: (1.0, 0.85 if p[1] > h0 + 0.6 else 1.0, 1.0))
    if pc.HAS_LODS:
        lod1(kit)
        lod2(kit)
    print("[o2] %s block %.0f x %.0f x %.0f mm, %d prong holes, cover screw head Ø %.1f" % (kit.name, S, S, HEIGHT, len(HOLES), 2 * HEAD[0]))
    pc.end(kit, sys.modules[__name__], "outlet_jack", S, S, pc.PROUD_DATA, [],
           extra={"jack": "4-prong 404A type", "screws": 0, "surfaceBlock": True})


def lod1(kit):
    m = pc.Mesh()
    outline = pc.rr_outline(0.0, 0.0, SIZE / 2, SIZE / 2, PLAN_R, 6, rmin=1.5)
    w = [(0.0, 0.0), (0.25, GROOVE[0]), (0.25 + GROOVE[2], GROOVE[0] + 0.3), (0.3, GROOVE[1] + 0.2)]
    w += pc.convex_walk(GROOVE[1] + 0.2, HEIGHT, 0.5, TOP_ROUND, 2)[1:]
    rings = m.body(outline, w)
    holes = []
    d = pc.Mesh()
    for x, z, r in HOLES:
        hr = m.lathe(x, z, [(r, HEIGHT), (r, HEIGHT - 1.0)], 8)
        holes.append(hr[0])
        d.cap(d.ring(pc.circle(x, z, r, 8), HEIGHT - 1.0))
    cb = m.lathe(0.0, 0.0, [(CBORE_R, HEIGHT), (CBORE_R, HEIGHT - CBORE_D)], 12)
    holes.append(cb[0])
    m.fill(rings[-1], holes)
    out = [m.to_object(kit, "block lod1", pc.CE, lods="1"), d.to_object(kit, "holes lod1", pc.PB, lods="1")]
    hb = pc.Mesh()
    hb.lathe(0.0, 0.0, [(CBORE_R, HEIGHT - CBORE_D), (HEAD[0], HEIGHT - CBORE_D + HEAD[1]), (0.0, HEIGHT - CBORE_D + HEAD[1] + HEAD[2])], 12)
    out.append(hb.to_object(kit, "screw lod1", pc.BR, lods="1"))
    return out


def lod2(kit):
    m = pc.Mesh()
    r0 = m.ring(pc.oct_outline(SIZE / 2, SIZE / 2, 3.0), 0.0)
    r1 = m.ring(pc.oct_outline(SIZE / 2 - 0.5, SIZE / 2 - 0.5, 3.0), HEIGHT - TOP_ROUND)
    r2 = m.ring(pc.oct_outline(SIZE / 2 - 0.5 - TOP_ROUND, SIZE / 2 - 0.5 - TOP_ROUND, 1.5), HEIGHT)
    m.chain([r0, r1, r2])
    m.cap(r2)
    return [m.to_object(kit, "block lod2", pc.CE, lods="2")]
