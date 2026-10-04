"""One-hole conduit strap for 1/2 in EMT: the stamped steel saddle that
holds a surface conduit run to the wall every 1.5 m.

Real-world reference: galvanized one-hole EMT strap, 1/2 in trade size
(01_period_research §6.4); stamped steel ~1.6 mm thick, one foot with one
screw. Unchanged since the 1950s. Spec envelope 30 x 22 x 22 mm (W x H x
proud; 10_spec §1.3 ESTIMATE; real straps run ~45-50 mm long and 16-19 mm
wide, so this is a compact one).

Construction (mm): a 1.6 x 22 band swept along its centreline: a short leg
from the wall to the tube's side, a half-round saddle (inner radius 9.1,
0.15 mm clear of the 17.9 tube) over the front, a leg back to the wall, a
1.6 mm bend and a 7.8 mm foot on the wall carrying a slotted pan-head screw
(Ø 7.0). The tube axis sits at y = -11.5 (E_AXIS), so the saddle top is
22.2 mm proud.

ORIGIN = THE WALL POINT BEHIND THE TUBE AXIS AT THE STRAP'S MID-HEIGHT
(Blender y = 0 = wall plane, x = 0 = tube axis, z = 0 = strap centre). The
foot points to +X in Blender (Unity -X). R3 draws one per 1.5 m of conduit
at the tile's axis line. Front = kit -Y (Unity +Z).

Budget (10_spec §1.3): 300 / 100 / - tris, LOD_DISTANCES 2 / 6 m (culled
beyond 6 m: a 22 mm strap is ~3 px there). Slot: Prop_Aluminium. Render-only.
Era: no text, logos or dates.
"""

import math

from mathutils import Vector

import outlet_box_common as B

NAME = "Kit_ConduitStrap"
LOD1 = None
LOD1_RATIO = 100 / 300.0
LOD2_RATIO = 0.0
LOD_DISTANCES = (2.0, 6.0, None)
BUDGET = (300, 100, 0)
SMOOTH_ANGLE = 35.0

MM = B.MM
T = 1.6                       # sheet thickness (ESTIMATE)
WIDTH = 22.0                  # band width along the tube (spec H)
CLEAR = 0.15                  # saddle clearance over the tube
RM = B.EMT_OD / 2 + CLEAR + T / 2   # centreline radius of the saddle
RB = 1.6                      # foot bend centreline radius (ESTIMATE)
FOOT_END = 30.0 - (RM + T / 2)      # foot end x so the width is exactly 30


def centreline(arc_segs, bend_segs):
    """(x, y) mm from the closed leg's end on the wall to the foot end."""
    ax = -B.E_AXIS
    pts = [(-RM, 0.0)]
    for k in range(arc_segs + 1):
        a = math.pi + math.pi * k / arc_segs
        pts.append((RM * math.cos(a), ax + RM * math.sin(a)))
    cx, cy = RM + RB, -(T / 2 + RB)
    for k in range(bend_segs + 1):
        a = math.pi - (math.pi / 2) * k / bend_segs
        pts.append((cx + RB * math.cos(a), cy + RB * math.sin(a)))
    pts.append((FOOT_END, -T / 2))
    return pts


def band(kit, arc_segs, bend_segs, lods, name):
    pts = centreline(arc_segs, bend_segs)
    mb = B.MB()
    stations = []
    for k, (x, y) in enumerate(pts):
        if k == 0:
            tx, ty = pts[1][0] - x, pts[1][1] - y
        elif k == len(pts) - 1:
            tx, ty = x - pts[k - 1][0], y - pts[k - 1][1]
        else:
            ax, ay = x - pts[k - 1][0], y - pts[k - 1][1]
            bx, by = pts[k + 1][0] - x, pts[k + 1][1] - y
            la, lb = math.hypot(ax, ay), math.hypot(bx, by)
            tx, ty = ax / la + bx / lb, ay / la + by / lb
        L = math.hypot(tx, ty)
        tx, ty = tx / L, ty / L
        nx, ny = -ty, tx                       # toward the tube / the wall
        h = T / 2
        ring = []
        for (s, z) in ((1, -WIDTH / 2), (-1, -WIDTH / 2), (-1, WIDTH / 2), (1, WIDTH / 2)):
            ring.append(mb.bm.verts.new(((x + s * nx * h) * MM, (y + s * ny * h) * MM, z * MM)))
        stations.append((Vector((x * MM, y * MM, 0.0)), ring))
    for (ca, ra), (cb, rb) in zip(stations, stations[1:]):
        faces = mb.band(ra, rb, B.AL)
        mid = (ca + cb) / 2
        for f in faces:
            f.normal_update()
            if f.normal.dot(f.calc_center_median() - mid) < 0:
                f.normal_flip()
    for (c, ring), (c2, _) in ((stations[0], stations[1]), (stations[-1], stations[-2])):
        f = mb.face(ring, B.AL)
        f.normal_update()
        if f.normal.dot(c - c2) < 0:
            f.normal_flip()
    return mb.to_part(kit, name, lods)


def build(kit):
    band(kit, 14, 3, "0", "strap")
    foot_x = (RM + RB + FOOT_END) / 2
    B.screw_pan(kit, B.front_frame(foot_x * MM, -T * MM, 0.0), 58.0, segs=16, head_d=7.0, head_h=2.2,
                slot_w=1.0, slot_depth=0.8, name="screw")
    if B.lods_enabled():
        band(kit, 8, 1, "1", "lod1")
        mb = B.MB()
        mb.cap(mb.ring(B.front_frame(foot_x * MM, -T * MM, 0.0), B.circle(3.5, 8), 1.2), B.AL)
        mb.to_part(kit, "lod1_screw", "1")
    B.finalize(kit, SMOOTH_ANGLE, None)
    kit.anchor("screw", (foot_x * MM, -T * MM, 0.0))
    import sys
    module = sys.modules[__name__]
    _, lo, hi = B.box_meta(kit, module, "outlet_surface", 30.0, WIDTH, wall=True, extra_tags=("conduit",),
                           outlet_extra={"axisOffset": B.E_AXIS * MM, "pitch": 1.5})
    span = (hi[0] - lo[0], hi[2] - lo[2], -lo[1])
    assert abs(span[0] - 30.0) <= 1.0 and abs(span[1] - 22.0) <= 1.0 and abs(span[2] - 22.0) <= 1.0, \
        "strap envelope %.2f x %.2f x %.2f" % span
    print("[o3] %s envelope %.2f x %.2f x %.2f mm (spec 30 x 22 x 22)" % ((kit.name,) + span))
