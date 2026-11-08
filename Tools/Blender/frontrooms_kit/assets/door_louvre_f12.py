"""Kit_DoorLouvre_F12: the back-of-house pair's door louvre (B-2, the A2 lite + louvre of door_vision 50, already researched:
built from its numbers; zone doors Build A, 2026-11-06, research/double_doors/20_design.md Part A, sections A4.4 and A5;
the D-2 map member's leaf takes the same louvre later).

A sight-proof inverted-V louvre, 10 x 16 in visible (0.254 x 0.4064 m), centred on the leaf 13-29 in above the floor: its frame
stands 36 mm over the 10 in kick plate, the 1992 city spec's louvre case (S1 SD 08710, Feb 1992: kick plates "16 inches high,
except ... to clear a louver ... 10 inches", which is why HW-DA1's 10 in plate stays). 16 blades of 20 ga steel at 25.4 mm
pitch, each an inverted V 41 mm deep rising 29.3 mm (55 deg) with a 5 mm hemmed lip down at both feet (Fix A: the hem modelled): every horizontal sight
line meets a blade (rise + lip is over the pitch), while light and sound pass between them: the next room's real lamps light
through the blades, no trick. Seen from standing height the lips are the louvre's horizontal lines, each with its shadow on
the slope below (Build A lab, 2026-11-06: without the lips the slopes, all facing the same way, read as one flat panel). The
frame is the pressed lite kit's section with its leg run down to the centre plane (a closed liner through the cut-out), both
faces, eight slotted oval-head screws on the -Z face.

Origin: the louvre's centre on the leaf's centre plane (x across, y up, z through); spawned under the slab. Slot Painted_Metal
(swapped to the leaf's paint at spawn). LODs (fr_lods): LOD0 the frame section, 16 blades with their sheet thickness and
lips, screws; LOD1 the 7-point frame, the blades' outer faces only (seen from above, as the louvre always is at 13-29 in);
LOD2 a flat ring per face and a recessed chevron panel (2 facets a face). Switch 2 / 8 m, culled at 40 m.
"""

import math
import random
import sys

import interact_door_common as dc
import door_pair_common as pc
import kitlib

NAME = "Kit_DoorLouvre_F12"
LOD_DISTANCES = (2.0, 8.0, 40.0)
SMOOTH_ANGLE = 35.0
SLOT = "Painted_Metal"
kitlib.register_slot(SLOT, (0.644, 0.610, 0.509), 0.5, 0.0)

BLADES = 16
PITCH = 0.0254
BLADE_T = 0.0009                          # 20 ga
BLADE_Z = 0.0205                          # half depth: inside the leaf faces (0.02225), under the frame's top
BLADE_ANGLE = math.radians(55.0)
RISE = BLADE_Z * math.tan(BLADE_ANGLE)    # 0.0293
LIP = 0.005                               # the hemmed lip down at each foot
assert RISE > PITCH, "sight-proof: each blade rises past the next one's feet"


def rect():
    x0, x1, y0, y1 = pc.F12
    yc = (y0 + y1) / 2.0
    return (x0, x1, y0 - yc, y1 - yc), yc


def blade(kit, k, level, r):
    """One blade along x (into the frame's legs at both ends, where its open ends are never seen): LOD0 the closed sheet
    section (outer path, then the 20 ga offset back), LOD1 the outer faces only; every face turned by its section normal."""
    import bmesh
    x0, x1, y0, y1 = r
    ya = y0 + (k + 1) * PITCH                # the apex (the top blade's apex on the opening's top edge)
    yf = ya - RISE                           # the feet at z +-BLADE_Z
    outer = [(-BLADE_Z, yf - LIP), (-BLADE_Z, yf), (0.0, ya), (BLADE_Z, yf), (BLADE_Z, yf - LIP)]
    if level == 0:
        inner = dc.offset_polyline(outer, BLADE_T, normals_left=False, closed=False)
        # Fix A: each lip ends in a hem (the sheet turned round a 0.45 mm radius, 4 segments) instead of a flat 0.9 mm cut,
        # which faced straight down and, seen edge-on near the eye's height, broke into dotted dark pixels
        r = BLADE_T / 2.0

        def hem(cz, cy):
            return [(cz + r * math.cos(a), cy + r * math.sin(a)) for a in (math.radians(-45.0), math.radians(-90.0), math.radians(-135.0))]
        sec = outer + hem(BLADE_Z - r, yf - LIP) + list(reversed(inner)) + hem(-BLADE_Z + r, yf - LIP)
        closed = True
    else:
        sec = outer
        closed = False
    reach = x1 + pc.POCKET - 0.0005          # into the frame's legs
    bm = bmesh.new()
    a = [bm.verts.new(pc.U(-reach, y, z)) for z, y in sec]
    b = [bm.verts.new(pc.U(reach, y, z)) for z, y in sec]
    n = len(sec)
    area = pc._signed_area(sec)
    for i in range(n if closed else n - 1):
        j = (i + 1) % n
        dz, dy = sec[j][0] - sec[i][0], sec[j][1] - sec[i][1]
        ln = math.hypot(dz, dy) or 1.0
        if closed:
            out = (dy / ln, -dz / ln) if area > 0 else (-dy / ln, dz / ln)
        else:
            out = (-dy / ln, dz / ln)            # the outer path's visible side is on the left of its walk
        pc._face(bm, (a[i], a[j], b[j], b[i]), (0.0, out[1], out[0]))
    o = dc._part(kit, bm, SLOT, "blade %02d" % k if level == 0 else "blade %02d lod1" % k)
    pc.tag(o, str(level))
    return o


def lod2_panel(kit, r, sign):
    """LOD2: a recessed chevron panel on one face (2 facets), in the opening's outline."""
    import bmesh
    x0, x1, y0, y1 = r
    ym = (y0 + y1) / 2.0
    w_edge, w_mid = 0.016, 0.006
    bm = bmesh.new()
    pts_lo = [(x0, y0, sign * w_edge), (x1, y0, sign * w_edge), (x1, ym, sign * w_mid), (x0, ym, sign * w_mid)]
    pts_hi = [(x0, ym, sign * w_mid), (x1, ym, sign * w_mid), (x1, y1, sign * w_edge), (x0, y1, sign * w_edge)]
    pc._face(bm, [bm.verts.new(pc.U(*p)) for p in pts_lo], (0.0, -0.3, sign))
    pc._face(bm, [bm.verts.new(pc.U(*p)) for p in pts_hi], (0.0, 0.3, sign))
    o = dc._part(kit, bm, SLOT, "louvre panel lod2 %s" % ("p" if sign > 0 else "n"))
    pc.tag(o, "2")
    return o


def build(kit):
    rng = random.Random(51112)
    r, yc = rect()
    for level in (0, 1):
        for sign, tag in ((1, "p"), (-1, "n")):
            o = pc.rect_sweep(kit, pc.steel_section(level, leg_to=0.0), r, sign, SLOT,
                              "louvre frame %s" % tag if level == 0 else "louvre frame %s lod1" % tag)
            pc.tag(o, str(level))
        for k in range(BLADES):
            blade(kit, k, level, r)
    for sign, tag in ((1, "p"), (-1, "n")):
        o = pc.flat_ring(kit, r, sign, pc.KIT_TOP, pc.KIT_FACE, SLOT, "louvre ring lod2 %s" % tag, w_outer=pc.KIT_BACK)
        pc.tag(o, "2")
        lod2_panel(kit, r, sign)
    # eight screws on -Z: two a rail, two a stile
    x0, x1, y0, y1 = r
    sd = pc.SCREW_D                          # on the flat, inside the feathered edge (Fix A)
    pts = [(x0 / 2.0, y0 - sd), (x1 / 2.0, y0 - sd), (x0 / 2.0, y1 + sd), (x1 / 2.0, y1 + sd),
           (x0 - sd, y0 / 2.0), (x0 - sd, y1 / 2.0), (x1 + sd, y0 / 2.0), (x1 + sd, y1 / 2.0)]
    for x, y in pts:
        s = dc.oval_head(kit, (x, y, -pc.KIT_TOP), (0, 0, -1), SLOT, rng, d=0.0055, half_segs=5, dome_k=0.16, name="louvre screw")
        pc.tag(s, "0")
    dc.finalize(kit, SMOOTH_ANGLE)
    lo, hi = pc.bounds(pc.level_parts(kit, 0))
    assert hi[0] <= x1 + pc.KIT_FACE + 1e-4 and lo[1] >= y0 - pc.KIT_FACE - 1e-4, "louvre %s %s" % (lo, hi)
    # in the leaf: the frame's bottom 36 mm over the 10 in kick plate's top, clear of the push plate
    frame_bottom = pc.F12[2] - pc.KIT_FACE
    kick_top = pc.LEAF_BOTTOM + 0.003 + 0.254
    assert frame_bottom - kick_top >= 0.035, "louvre frame %.4f m over the kick plate" % (frame_bottom - kick_top)
    pc.anchor(kit, "louvre_centre", 0.0, 0.0, 0.0)
    kit.meta["louvreRect"] = [r[0], r[1], r[2], r[3]]
    kit.meta["cutRect"] = [r[0] - pc.POCKET, r[1] + pc.POCKET, r[2] - pc.POCKET, r[3] + pc.POCKET]
    kit.meta["louvreCentreOnLeaf"] = [0.0, yc]
    kit.meta["blades"] = BLADES
    kit.meta["bladePitch"] = PITCH
    kit.meta["bladeRise"] = round(RISE, 5)
    kit.meta["bladeLip"] = LIP
    kit.meta["screws"] = len(pts)
    kit.meta["frameOverKickPlate"] = round(frame_bottom - kick_top, 4)
    kit.meta["origin"] = "the louvre's centre on the leaf's centre plane; x across, y up, z through; screws on -Z"
    kit.no_collider()
    kit.tag("door", "louvre", "zone_pair", "back_of_house")
    dc.lod_meta(kit, sys.modules[__name__])
