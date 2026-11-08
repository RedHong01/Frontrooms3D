"""Shared helpers for the zone kits of the title / station double door (zone doors, Build A, 2026-11-06;
Documentation/research/double_doors/20_design.md Part A, sections A4 and A5).

NOT AN ASSET: no NAME and no build(). The asset modules import it (``import door_pair_common as pc``):
door_frame_steel_pair_da (Kit_DoorFrame_SteelPair_DA), door_casing_station_pair (Kit_DoorCasing_StationPair),
door_guard_casing38 / door_guard_casing51 (Kit_DoorGuard_Casing38 / _51), door_lite_f1_steel / door_lite_f5_steel
(Kit_DoorLite_F1_Steel / _F5_Steel) and door_louvre_f12 (Kit_DoorLouvre_F12).

Frame: the PAIR'S DOOR SPACE (FrontRoomsDoubleDoorKit), Unity metres: x across the opening (0 its centre, the left leaf hinged
at -x), y up from the floor, z through the wall (0 the leaves' centre plane, +Z the face the stream door swings toward, the
station's side). The frame and casing kits are spawned at the door space's origin, the guards at the casing's inner arris,
the lite and louvre kits at the opening's centre on the leaf's centre plane (their own x and y), all unturned except the
guards' three mirrored placements (turned 180 deg about y and / or z; never a negative scale).

The binding spec (Red 2026-11-05 19:17 "规格和结构上的要求和之前一样"): the 2.4 x 2.66 m opening, 1-3/4 in leaves
(1.1955 x 2.639 x 0.0445 m), the DA hinge barrels (r 11 mm, axis x +-1.1985, z +-0.03425), the 12 mm hinge step, the
12 mm saddle (152 mm), HW-DA1. The F4 pressed-steel section (door_frame 10, Red "F4 当年尺寸"): 16 ga (1.5 mm), 50.8 mm
face, 11.1 mm proud of the paper plane (|z| 0.022), 3 mm convex and 1.5 mm concave bends. Every return foot reaches
z 0.0195, 0.5 mm into the wall face (the stream's paper lies at 0.022, D's data wall face at 0.020): no slit at either
builder (Red 10-29: "为什么现在门还是悬空的，两处还是有缝隙").
"""

import math

import bmesh
from mathutils import Vector

import interact_door_common as dc

U = dc.U

# ---------------------------------------------------------------- the pair (FrontRoomsDoubleDoorKit, b7ab5af0)
WIDTH = 2.4
HALF = WIDTH / 2.0                        # 1.200: the lining / soffit plane
HEAD = 2.9 - 0.24                         # 2.660: the head soffit
GAP = 0.003
LEAF_T = 0.0445
LEAF_HALF_T = LEAF_T / 2.0                # 0.02225: the leaf faces and the linings' ends
LEAF_W = (WIDTH - 3 * GAP) / 2.0          # 1.1955
SADDLE_H = 0.012
LEAF_BOTTOM = SADDLE_H + 0.006            # 0.018
LEAF_H = HEAD - GAP - LEAF_BOTTOM         # 2.639
LEAF_CY = LEAF_BOTTOM + LEAF_H / 2.0      # 1.3375: the slab's centre height
BARREL_R = 0.011
AXIS_X = HALF - GAP / 2.0                 # 1.1985
AXIS_Z = LEAF_HALF_T + BARREL_R + 0.001   # 0.03425
BARREL_X_MAX = AXIS_X + BARREL_R          # 1.2095: the barrels' outer reach (shut; they move toward -x in a swing)

# ---------------------------------------------------------------- the F4 section (door_frame 10 / 20, interact_door_da_common)
SHEET_T = dc.SHEET_T                      # 0.0015, 16 ga
BEND_RI = dc.BEND_RI                      # 0.0015 inside bend radius
CONVEX = BEND_RI + SHEET_T                # 0.003 outer radius of a convex bend
STEP = 0.012                              # the hinge step: the rise stands 12 mm off the soffit plane (x 1.212)
RISE_X = HALF + STEP                      # 1.212
FACE_Z = 0.022 + 0.0111                   # 0.0331: 7/16 in proud of the paper plane
FOOT_Z = 0.0195                           # every return foot: 0.5 mm into the wall face (z 0.020)
STEEL_FACE = 0.0508                       # 2 in face
STATION_FACE = 0.038                      # D-3: today's 38 mm band (the rail and base stop 50 mm out, the plate 75 mm out)
FRAME_OUTER = RISE_X + STEEL_FACE         # 1.2628
FRAME_TOP = HEAD + STEP + STEEL_FACE      # 2.7228
STATION_OUTER = RISE_X + STATION_FACE     # 1.250 (today's casing edge)
STATION_TOP = HEAD + STEP + STATION_FACE  # 2.710
CAP_T = 0.0005                            # D-3: the bronze cap over the walnut lining's +Z end (thin, inside the notches)
# Fix A (2026-11-06, Review A finding 2): in the start-area phase the stream's terminal pair stands in the map's station wall,
# whose south liner (FrontRoomsStationSet: FacadeFace 0.020 + LinerProud 0.004, its opening edge on the soffit plane x +-1.200)
# covers the lining's +Z end up to z 0.024, so a 0.5 mm cap at 0.02275 never showed (a pale 12 mm strip of the wall's paint
# ran up both jambs). The cap now stands 1.0 mm in front of the liner (CAP_FRONT), 0.5 mm into the opening past the soffit
# plane (CAP_IN: no shared plane with the liner's edge), and steps down to the thin cap round each hinge barrel (the barrels
# reach down to z 0.02325 within x <= 1.2045; NOTCH_U keeps 0.6 mm clear of them shut and in a swing toward -Z).
LINER_FRONT = 0.024                       # the map's south liner face in the door space (start-area phase)
CAP_FRONT = LINER_FRONT + 0.001           # 0.025
CAP_IN = 0.0005                           # the cap's inner face 0.5 mm into the opening (x 1.1995)
NOTCH_U = 0.0055                          # the notch round each barrel: u 0 .. 5.5 mm off the soffit plane stays thin
# the hinges (FrontRoomsDoubleDoorKit.HingeCentres, Bommer's layout): flange 178 mm, barrel 203 mm (+ 5 mm tips, r 8.6 mm)
HINGE_FLANGE = 0.178
BARREL_L = 0.203


def hinge_centres():
    c1 = LEAF_BOTTOM + 0.06 + HINGE_FLANGE * 0.5
    c2 = c1 + HINGE_FLANGE + 0.06
    c3 = LEAF_BOTTOM + LEAF_H - 0.15 - HINGE_FLANGE * 0.5
    return [c1, c2, c3]


assert [round(c, 4) for c in hinge_centres()] == [0.167, 0.405, 2.418], hinge_centres()
assert CAP_FRONT < FACE_Z - 0.005
# the thick cap's inner corner (x HALF + NOTCH_U, z CAP_FRONT) clears a barrel: shut, and turned up to 10 deg with its leaf
for _d in range(0, 11):
    _a = math.radians(_d)
    _cx, _cz = AXIS_X - (2 * AXIS_Z) * math.sin(_a), -AXIS_Z + (2 * AXIS_Z) * math.cos(_a)
    assert math.hypot(HALF + NOTCH_U - _cx, CAP_FRONT - _cz) >= BARREL_R + 0.0005, "cap vs barrel at %d deg" % _d

assert abs(FRAME_OUTER - 1.2628) < 1e-9 and abs(FRAME_TOP - 2.7228) < 1e-9
assert abs(STATION_OUTER - 1.250) < 1e-9 and abs(STATION_TOP - 2.710) < 1e-9
assert RISE_X - BARREL_X_MAX >= 0.0025 - 1e-9, "2.5 mm from the barrels to the rise"
assert AXIS_Z - BARREL_R - LEAF_HALF_T >= 0.001 - 1e-9, "the barrels stand 1 mm in front of the land"

# ---------------------------------------------------------------- guards (Station_StainlessGuard, 1.5 mm #4)
GUARD_T = 0.0015
GUARD_Y0, GUARD_Y1 = 0.10, 1.32           # E1's band (FrontRoomsStationTrim)
GUARD_CLEAR = RISE_X - GUARD_T - BARREL_X_MAX
assert GUARD_CLEAR >= 0.001 - 1e-9, "the guard's side wing 1 mm clear of the barrels (%.4f)" % GUARD_CLEAR

# ---------------------------------------------------------------- lites and louvre (door vision set A, door_vision 50 / 60)
IN = 0.0254
POCKET = 0.0095                           # the slab's cut-out = visible + 9.5 mm a side
GLASS_T = 0.0068
LIP = GLASS_T / 2 + 0.0001                # 0.0035
KIT_FACE = 0.019                          # 19 mm sight face
FACE_EMBED = 0.0001                       # the kit's flange back 0.1 mm under the leaf face (seated, no slit)
KIT_BACK = LEAF_HALF_T - FACE_EMBED       # 0.02215
KIT_TOP = LEAF_HALF_T + 0.00255           # 0.0248: 2.55 mm proud (the D1.6 kit stands 2.5 mm off its 44 mm leaf)

# visible rects in the pair's door space, centred on each leaf (x0, x1, y0, y1)
F1 = (-0.127, 0.127, 1.143, 1.905)        # 10 x 30 in, bottom 45 in (Office: walnut bead; back-of-house: pressed kit)
F5 = (-0.1905, 0.1905, 1.1938, 1.5748)    # 15 x 15 in, bottom 47 in (Run!)
F12 = (-0.127, 0.127, 0.3302, 0.7366)     # the A2 louvre, 10 x 16 in (back-of-house: the 1992 louvre rule's 10 in kick plate)


def area_sq_in(rect):
    return (rect[1] - rect[0]) * (rect[3] - rect[2]) / (IN * IN)


# the pressed steel lite kit's section on one face (d outward from the visible edge, w from the leaf's centre plane), closed;
# interact_door_lite_common.STEEL_SECTIONS on the title pair's 44.5 mm leaf (its faces at 0.02225, not 0.022).
# Fix A (2026-11-06, Review A finding 3): the flange ends in a low feathered slope that runs into the leaf face, as a
# low-profile pressed kit's edge does, instead of Build A's 45 deg outer chamfer over a 0.65 mm square step. Under the game's
# ceiling lamps the old chamfer faced away from the light on the bottom rail (a near-black 1 mm line along the frame's foot,
# read as a crack, Red 10-29 "两处还是有缝隙") and caught acne and coverage dots round the louvre; the slope (16-22 deg off
# the face) is lit like the face, a little darker on the bottom rail, and meets the face along one clean line at d 0.0187
# (its foot is 0.1 mm under the face, so the flange is seated with no slit and no shared plane). LOD1 keeps the slope (one
# facet) so the phone tier keeps the frame's outline.
FLAT_END = 0.0115                         # the flat top 3.0-11.5 mm out; screws on it at SCREW_D
SCREW_D = 0.0075


def steel_section(level, leg_to=LIP):
    if level == 0:
        return [(0.0, leg_to), (0.0, KIT_TOP - 0.003), (0.003, KIT_TOP), (FLAT_END, KIT_TOP), (0.0152, KIT_TOP - 0.0011),
                (KIT_FACE, KIT_BACK), (POCKET, KIT_BACK), (POCKET, leg_to)]
    return [(0.0, leg_to), (0.0, KIT_TOP - 0.003), (0.003, KIT_TOP), (FLAT_END, KIT_TOP), (KIT_FACE, KIT_BACK),
            (POCKET, KIT_BACK), (POCKET, leg_to)]


def tag(obj, levels):
    """kitlib P-1b: the LOD levels this part belongs to ("0", "01", "012", ...)."""
    obj["fr_lods"] = levels
    return obj


# ---------------------------------------------------------------- geometry helpers
def _signed_area(poly):
    a = 0.0
    for i in range(len(poly)):
        x0, y0 = poly[i]
        x1, y1 = poly[(i + 1) % len(poly)]
        a += x0 * y1 - x1 * y0
    return a / 2.0


def _edge_out(poly, i):
    """Outward 2D normal of the closed polygon's edge i -> i + 1."""
    x0, y0 = poly[i]
    x1, y1 = poly[(i + 1) % len(poly)]
    dx, dy = x1 - x0, y1 - y0
    ln = math.hypot(dx, dy) or 1.0
    n = (dy / ln, -dx / ln)                # right-hand normal: outward for a CCW polygon
    return n if _signed_area(poly) > 0 else (-n[0], -n[1])


def _face(bm, verts, want_unity):
    """A face over ``verts`` turned so its normal follows ``want_unity`` (a Unity direction)."""
    f = bm.faces.new(verts)
    f.normal_update()
    w = Vector(U(*want_unity))
    if f.normal.dot(w) < 0:
        f.normal_flip()
    return f


def sheet_section(outer, radii, segs):
    """A closed 16 ga section from its visible outer path (2D points, open polyline, the visible side on the LEFT of the
    walk) with fillets: the inner path is the outer path offset by SHEET_T to the right."""
    o = dc.fillet_polygon(outer, radii, segs, closed=False)
    i = dc.offset_polyline(o, SHEET_T, normals_left=False, closed=False)
    return o + list(reversed(i))


def opening_sweep(kit, section, slot, name, bottoms=True):
    """The closed (u, z) ``section`` swept round the opening: u >= 0 outward from the soffit plane (x +-HALF on the jambs,
    y HEAD on the head), mitred at the head corners, the jambs from the floor. No caps (the jamb feet stand on the floor)."""
    rings = []
    for key in ("hb", "hm", "lm", "lb"):
        pts = []
        for u, z in section:
            if key == "hb":
                pts.append((HALF + u, 0.0, z))
            elif key == "hm":
                pts.append((HALF + u, HEAD + u, z))
            elif key == "lm":
                pts.append((-(HALF + u), HEAD + u, z))
            else:
                pts.append((-(HALF + u), 0.0, z))
        rings.append(pts)
    bm = bmesh.new()
    vr = [[bm.verts.new(U(*p)) for p in r] for r in rings]
    n = len(section)
    for s in range(3):
        a, b = vr[s], vr[s + 1]
        for i in range(n):
            j = (i + 1) % n
            nu, nz = _edge_out(section, i)
            if s == 0:
                want = (nu, 0.0, nz)                         # +x jamb: u along +x
            elif s == 1:
                want = (0.0, nu, nz)                         # head: u along +y
            else:
                want = (-nu, 0.0, nz)                        # -x jamb: u along -x
            _face(bm, (a[i], a[j], b[j], b[i]), want)
    return dc._part(kit, bm, slot, name)


def rect_sweep(kit, section, rect, sign, slot, name):
    """A closed (d, w) section swept round the visible rect (x0, x1, y0, y1) in the leaf's face plane, d outward from the
    visible edge, w from the centre plane on the face ``sign`` (z = sign * w); mitred corners."""
    x0, x1, y0, y1 = rect
    bm = bmesh.new()
    rings = []
    for d, w in section:
        z = sign * w
        rings.append([bm.verts.new(U(x, y, z)) for x, y in ((x0 - d, y0 - d), (x1 + d, y0 - d), (x1 + d, y1 + d), (x0 - d, y1 + d))])
    n = len(section)
    for k in range(n):
        a, b = rings[k], rings[(k + 1) % n]
        nd, nw = _edge_out(section, k)
        for i in range(4):
            j = (i + 1) % 4
            # side i: 0 bottom (outward -y), 1 right (+x), 2 top (+y), 3 left (-x)
            ox, oy = ((0.0, -1.0), (1.0, 0.0), (0.0, 1.0), (-1.0, 0.0))[i]
            _face(bm, (a[i], a[j], b[j], b[i]), (ox * nd, oy * nd, sign * nw))
    return dc._part(kit, bm, slot, name)


def flat_ring(kit, rect, sign, w, face, slot, name, w_outer=None):
    """LOD2: a mitred ring (4 trapezoids) of width ``face`` round the rect, its inner edge at z = sign * w and its outer edge
    at z = sign * w_outer (default w: flat; Fix A: the lite kits' ring slopes into the leaf face, no floating outer edge)."""
    x0, x1, y0, y1 = rect
    bm = bmesh.new()
    z = sign * w
    zo = sign * (w if w_outer is None else w_outer)
    inner = [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]
    outer = [(x0 - face, y0 - face), (x1 + face, y0 - face), (x1 + face, y1 + face), (x0 - face, y1 + face)]
    vi = [bm.verts.new(U(x, y, z)) for x, y in inner]
    vo = [bm.verts.new(U(x, y, zo)) for x, y in outer]
    for k in range(4):
        j = (k + 1) % 4
        _face(bm, (vi[k], vi[j], vo[j], vo[k]), (0.0, 0.0, sign))
    return dc._part(kit, bm, slot, name)


def rect_screws(kit, rect, sign, slot, rng, count_per_stile=2, name="screw"):
    """Slotted oval-head screws on the kit's flat top, SCREW_D (7.5 mm) outside the visible edge (lite_common's rule was 12 mm
    on its wider flat; Fix A's feathered edge starts at 11.5 mm, so the 5.5 mm heads sit wholly on the flat): one a rail
    (centred), ``count_per_stile`` a stile (0.15 m from the ends on a tall lite, else at a quarter of its height)."""
    x0, x1, y0, y1 = rect
    h = y1 - y0
    xc = (x0 + x1) / 2.0
    if count_per_stile == 2:
        ys = [y0 + 0.15, y1 - 0.15] if h > 0.45 else [y0 + h / 4, y1 - h / 4]
    else:
        ys = [(y0 + y1) / 2.0]
    s = SCREW_D
    pts = [(xc, y0 - s), (xc, y1 + s)] + [(x0 - s, y) for y in ys] + [(x1 + s, y) for y in ys]
    out = []
    for x, y in pts:
        s = dc.oval_head(kit, (x, y, sign * KIT_TOP), (0, 0, sign), slot, rng, d=0.0055, half_segs=5, dome_k=0.16, name=name)
        tag(s, "0")
        out.append(s)
    return out


def anchor(kit, name, x, y, z):
    kit.anchor(name, U(x, y, z))


def bounds(objs):
    return dc.bounds_unity(objs)


def level_parts(kit, level):
    return [o for o in kit.parts if str(level) in str(o.get("fr_lods", "0"))]


def tris_of(objs):
    t = 0
    for o in objs:
        for p in o.data.polygons:
            t += len(p.vertices) - 2
    return t
