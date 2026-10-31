"""Shared helpers for the DOUBLE-ACTING (DA) door kit of D1.6 (A12 / A19, Red
2026-10-22: every map door swings both ways; research/interactables/d16/10_design.md
§2, 04_door_re8_gap.md §5 Option B).

NOT AN ASSET: no NAME and no build(). The _DA asset modules import it
(``import interact_door_da_common as da``). It imports interact_door_common
(the single-acting helpers) and never changes it, so the single-acting kits
(the dormant DoorActing.Single mode) rebuild byte-identically.

Frame (door root D, Unity metres; 10_design §2.2). Origin = the pivot axis:
the opening's hinge-jamb edge on the wall centre plane, at floor level (the
map's "Door hinge"). +Z along the opening (latch jamb at 1.0), +Y up, +X is
face S (the door root's +X; no handing mirror in DA), -X is face P.

What the DA door is (a commercial double-acting door, 04 §3.2 / §5.1):
* no stops, no butts, no closers: top and bottom PIVOTS that show only as
  caps (head soffit, floor plate) and as plates in the leaf's top and foot;
* FIX 1 (2026-10-22, VL319 junction): the leaf's VISUAL pivot axis sits on the
  wall centre plane PIVOT_Z = 36 mm into the opening from the map's hinge point,
  as real centre-hung double-acting pivots are set in from the jamb. The hinge
  stile is a circular arc of radius R_ARC 0.031 about that axis (the "radius-cut
  pivot edge": the same shape at every angle), 3 mm off the flat hinge lining
  (no slot), so the leaf clears the lining, the casing and the map's trims at
  every angle to 95 deg both ways. FrontRoomsDoorRig turns the kit leaf about
  this axis (the map's "Door hinge" still turns about its own point; the visual
  leaf and the map's collider differ by at most 2 x 0.036 x sin(47.5) = 53 mm
  at 95 deg, render-only). The frame and the leaf are still authored in the
  door root (origin = the map's hinge point), so nothing moves when shut;
* the latch end is flat at Z 0.995 (an arc r 0.995 about the axis is within
  0.25 mm of it), 3 mm short of the latch lining (0.998);
* black brush seals on the wall centre plane close the straight sightlines:
  a hinge fin on the hinge lining, a latch fin and a head fin overlap the shut
  leaf by 1, 1 and 2 mm; the floor is the 12 mm saddle, 3 mm under the leaf;
* casing on both faces in sleeve mode at period sizes (F4, Red 2026-10-30): steel 50.8 x 11.1 mm, wood 57.2 x
  17.5 mm + a 4.8 mm reveal, enclosing the map's door-only 0.045 x 0.008 trims.

LODs (P-1 hand-built levels, kitlib ``fr_lods``): every builder here takes a
``level`` (0, 1, 2) and the asset modules build each level from the same
numbers: LOD0 hero (96-segment radius stile and caps, 1.5 mm arrises, wear
rings), LOD1 the same envelope with fewer segments and no screws, LOD2 the
envelope only. The seals and linings are in every level (a far door must stay
gap-free), so their parts carry fr_lods "012".
"""

import math
import random

import bmesh
import bpy
from mathutils import Matrix, Vector

import interact_door_common as dc

U = dc.U
to_unity = dc.to_unity

# ------------------------------------------------------------- DA section
LEAF_X = dc.LEAF_X                      # 0.022
PIVOT_Z = 0.036                         # FIX 1: the visual pivot axis, on the wall centre plane, 36 mm into the opening
R_ARC = 0.031                           # the hinge stile: an arc of radius 31 mm about the visual pivot axis
STILE_Z_FACE = PIVOT_Z - (R_ARC ** 2 - LEAF_X ** 2) ** 0.5   # where the arc meets the leaf faces (Z 0.0142)
STILE_APEX_Z = PIVOT_Z - R_ARC          # the arc's apex on the centre plane (Z 0.005, 3 mm off the lining)
R_STILE = R_ARC                         # (kept name) the stile's radius
LEAF_Y0, LEAF_Y1 = dc.LEAF_Y0, dc.LEAF_Y1   # 0.015 .. 2.095 (3 mm gaps to the saddle and the head)
LEAF_Z_LATCH = 0.995                    # flat latch end (3 mm to the latch lining)
ARRIS = dc.ARRIS                        # 1.5 mm
LINING_Z_H, LINING_Z_L, LINING_Y = dc.LINING_Z_H, dc.LINING_Z_L, dc.LINING_Y
LINING_BACK_H, LINING_BACK_L, LINING_BACK_Y = dc.LINING_BACK_H, dc.LINING_BACK_L, dc.LINING_BACK_Y
WALL_X = dc.WALL_X
SADDLE_HALF, SADDLE_FLAT_HALF, SADDLE_H = dc.SADDLE_HALF, dc.SADDLE_FLAT_HALF, dc.SADDLE_H
FIN_X = 0.004                           # hinge, latch and head fins: 8 mm brush on the centre plane
HINGE_SEAL = ((-FIN_X, 0.012, LINING_Z_H), (FIN_X, LINING_Y, STILE_APEX_Z + 0.001))   # FIX 1: on the flat hinge lining, 1 mm into the arc
LATCH_FIN = ((-FIN_X, 0.011, 0.994), (FIN_X, LINING_Y, LINING_Z_L))
HEAD_FIN = ((-FIN_X, 2.093, 0.0), (FIN_X, LINING_Y, LINING_Z_L))
PIVOT_CAP_HEAD = (0.016, 0.0015)        # head soffit cap: radius, thickness (flush under the soffit)
PIVOT_CAP_FLOOR = (0.019, 0.0015)       # floor cover plate on the saddle's hinge end
PIVOT_PLATE = (0.022, 0.060, 0.002)     # leaf top / foot plates: X width, Z length, depth (ESTIMATE)
PIVOT_PLATE_Z = (PIVOT_Z - 0.015, PIVOT_Z + 0.045)
STILE_SEGS = (96, 48, 16)               # arc segments per level over the stile's 90 deg (LOD0: 384 per revolution; 10_spec's hero rule >= 96)
CAP_SEGS = (96, 32, 12)               # LOD2 builds no caps
STRIKE_BORED = dc.STRIKE_BORED          # (0.032, 0.124, 1.000)
STRIKE_MORTISE = dc.STRIKE_MORTISE      # (0.032, 0.200, 0.968)
STRIKE_DEPTH = dc.STRIKE_DEPTH

LATCH_Y_BORED, LATCH_Y_MORTISE, DEADBOLT_Y = 1.000, 0.9365, 1.000

# ------------------------------------------------------- the frame section (F4, Red 2026-10-30: research/door_frame/20_for_red.md)
# Period sizes, sourced in research/door_frame/10_period_frames.md. Face = from the lining face (Z 0.002 / 0.998, Y 2.098) outward;
# proud = above the wall face (X 0.08). Steel: 2 in x 7/16 in, the SDI cased-open profile. Wood: a 2-1/4 x 11/16 in casing that
# steps back 3/16 in from the opening, so the band is 62 mm; a flat crown and a rounded outer edge, no back band (a sloped crown
# mirrors the ceiling lamp on the head as a white bar).
# The sleeve encloses the map's door-only trims (DOOR_TRIM_*: FrontRoomsMapWorld.DoorTrimFace / DoorTrimProud, W/apply/door_frame_f4_contract).
STEEL_FACE, STEEL_PROUD = 0.0508, 0.0111
WOOD_FACE, WOOD_PROUD, WOOD_REVEAL = 0.0572, 0.0175, 0.0048
WOOD_BAND = WOOD_FACE + WOOD_REVEAL
WALL_FACE_X = 0.08
STEEL_X1, WOOD_X1 = WALL_FACE_X + STEEL_PROUD, WALL_FACE_X + WOOD_PROUD
DOOR_TRIM_FACE, DOOR_TRIM_PROUD = 0.045, 0.008
DOOR_TRIM_X = WALL_FACE_X + DOOR_TRIM_PROUD
assert STEEL_FACE > DOOR_TRIM_FACE + LINING_Z_H and STEEL_X1 > DOOR_TRIM_X + 0.002, "the steel sleeve encloses the map's door trims"
assert WOOD_BAND > DOOR_TRIM_FACE + LINING_Z_H and WOOD_X1 > DOOR_TRIM_X + 0.002, "the wood casing encloses the map's door trims"

assert R_ARC > LEAF_X, "the stile arc spans the leaf's thickness"
assert abs(STILE_APEX_Z - LINING_Z_H - 0.003) < 1e-6, "3 mm hinge gap at the apex"
assert LEAF_Z_LATCH < LINING_Z_L - 0.0029, "3 mm latch gap"
assert math.hypot(LEAF_X, LEAF_Z_LATCH - PIVOT_Z) < LINING_Z_L - PIVOT_Z - 0.002, "the latch corner's arc clears the latch lining"
# 95 deg: the face that turns toward the hinge jamb stays clear of the casing's reveal arris (Z 0.002 at the widest |X|, the wood casing's)
_t = math.radians(95.0)
assert PIVOT_Z - LEAF_X * math.sin(_t) + ((WOOD_X1 - LEAF_X * math.cos(_t)) / math.sin(_t)) * math.cos(_t) > LINING_Z_H + 0.002, "95 deg clears the casing"
assert HEAD_FIN[0][1] < LEAF_Y1 and LATCH_FIN[0][2] < LEAF_Z_LATCH, "fins overlap the shut leaf"


def tag_lods(obj, levels):
    """kitlib P-1b: the LOD levels this part belongs to ("0", "01", "012", ...)."""
    obj["fr_lods"] = levels
    return obj


# --------------------------------------------------------------- the leaf
def leaf_section(inset=0.0, level=0, r_l=ARRIS, seams=(), seam_w=0.0003, seam_d=0.0003):
    """Plan section (X, Z) of the DA leaf: flat faces at X +-(LEAF_X - inset),
    a flat latch end at LEAF_Z_LATCH with filleted arrises r_l, V seams on the
    latch end at X in ``seams``, and the hinge stile: an arc of radius
    R_ARC - inset about the visual pivot (0, PIVOT_Z) between the faces (FIX 1).
    Level 2 has square latch corners."""
    x = LEAF_X - inset
    R = R_ARC - inset
    zl = LEAF_Z_LATCH - inset
    rl = max(r_l - inset, 5e-5) if level < 2 else 0.0
    fseg = 2 if level == 0 else 1
    pts, radii = [(x, zl)], [rl]
    for sx in sorted(seams, reverse=True):
        if level == 0:
            pts += [(sx + seam_w / 2, zl), (sx, zl - seam_d), (sx - seam_w / 2, zl)]
            radii += [0, 0, 0]
    pts.append((-x, zl))
    radii.append(rl)
    n = STILE_SEGS[level]
    alpha = math.acos(x / R)                  # the faces meet the arc at pi + alpha (-X) and 2 pi - alpha (+X)
    a0, a1 = math.pi + alpha, 2.0 * math.pi - alpha
    for i in range(n + 1):
        a = a0 + (a1 - a0) * i / n             # -X face corner -> apex (0, PIVOT_Z - R) -> +X face corner
        pts.append((R * math.cos(a), PIVOT_Z + R * math.sin(a)))
        radii.append(0)
    # the last arc point is (x, PIVOT_Z - sqrt(R^2 - x^2)); the polygon closes back to (x, zl)
    return dc.fillet_polygon(pts, radii, fseg, closed=True)


def chamfer_rings(y_edge, sign, fn, level):
    """Top / bottom arris rounds: 3 rings (LOD0 and LOD1, fix 1: LOD1 keeps LOD0's ring structure so the
    vertex-colour wear and the shading do not pop at the 4 m switch), 2 rings = a 45 deg chamfer (LOD2)."""
    A = ARRIS
    out = []
    phis = (0.0, 45.0, 90.0) if level < 2 else (0.0, 90.0)
    for phi in phis:
        o = A - A * math.sin(math.radians(phi))
        h = A - A * math.cos(math.radians(phi))
        out.append((y_edge + sign * h, fn(o)))
    return out


def slab(kit, slot, level, name="slab", seams=(), wear_y=None, extra_rings=None, grain="z"):
    """The DA leaf slab for one LOD level: loft_y over the arris rings, the
    optional hand-wear rings (wear_y = 4 heights where the latch arris goes
    1.5 -> 3 mm and back, LOD0 only) and extra rings (channel lines)."""
    def sec(o, wear=False):
        return leaf_section(o, level, r_l=(0.003 if wear else ARRIS), seams=seams)
    rings = chamfer_rings(LEAF_Y0, 1, lambda o: sec(o), level)
    mids = []
    # fix 1: LOD1 keeps the wear rings and the extra rings (channel lines) of LOD0: same vertex-colour wear, no pop
    if level < 2 and wear_y:
        mids += [(wear_y[0], sec(0.0)), (wear_y[1], sec(0.0, True)), (wear_y[2], sec(0.0, True)), (wear_y[3], sec(0.0))]
    if level < 2 and extra_rings:
        mids += extra_rings(sec)
    mids.sort(key=lambda r: r[0])
    rings += mids
    rings += list(reversed(chamfer_rings(LEAF_Y1, -1, lambda o: sec(o), level)))
    obj = dc.loft_y(kit, rings, slot, name, grain=grain)
    return obj


def edge_frame():
    """The flat latch end: local x = X, y = Y, z = +Z (the outward normal)."""
    return Matrix.Identity(3), Vector((0.0, 0.0, 1.0))


def edge_plate(kit, slot, yc, w, h, t, name, bevel=0.0003):
    """A plate mortised flush into the flat latch end, centred X 0 at height
    yc; returns (plate, mortise cutter for the slab)."""
    R, n = edge_frame()
    face = Vector((0.0, yc, LEAF_Z_LATCH))
    plate = dc.obox(kit, slot, centre=face - n * (t / 2), half=(w / 2, h / 2, t / 2), R=R, bevel=bevel, segs=1, name=name)
    mort = dc.cutter(None, None, R=R, centre=face - n * (t / 2 - 0.0002), half=(w / 2, h / 2, t / 2 + 0.0002))
    return plate, mort


def edge_opening(yc, w, h, depth=0.006):
    R, n = edge_frame()
    face = Vector((0.0, yc, LEAF_Z_LATCH))
    return dc.cutter(None, None, R=R, centre=face - n * (depth / 2 - 0.0003), half=(w / 2, h / 2, depth / 2))


def pivot_plates(kit, slot, level, rng=None):
    """The top-pivot plate in the leaf top and the bottom-arm plate in its
    foot, flush (LOD0 only: they show only from below or above the gaps)."""
    if level != 0:
        return []
    w, ln, d = PIVOT_PLATE
    z0, z1 = PIVOT_PLATE_Z
    out = []
    for y_face, sgn, nm in ((LEAF_Y1, -1, "top pivot plate"), (LEAF_Y0, 1, "bottom arm plate")):
        lo = (-w / 2, min(y_face, y_face + sgn * d), z0)
        hi = (w / 2, max(y_face, y_face + sgn * d), z1)
        p = dc.obox(kit, slot, lo=lo, hi=hi, bevel=0.0003, segs=1, name=nm)
        tag_lods(p, "0")
        out.append(p)
        if rng is not None:
            for z in (z0 + 0.008, z1 - 0.008):
                s = dc.flat_head(kit, (0.0, y_face, z), (0, -sgn, 0), slot, rng, d=0.0060, host=p, notch=0.0006, name="pivot screw")
                tag_lods(s, "0")
    return out


def pivot_plate_cutters():
    w, ln, d = PIVOT_PLATE
    z0, z1 = PIVOT_PLATE_Z
    return [dc.cutter((-w / 2, LEAF_Y1 - d, z0), (w / 2, LEAF_Y1 + 0.001, z1)),
            dc.cutter((-w / 2, LEAF_Y0 - 0.001, z0), (w / 2, LEAF_Y0 + d, z1))]


def leaf_anchors(kit, locked, sign_yz=(1.524, 0.500)):
    """Sidecar anchors of a DA leaf (10_design §2.2), door root, S = +X. sign_yz: the locked-door sign's centre (Y, Z);
    a leaf with the DA-L1 vision lite hangs it on the top rail (2.010, 0.400; door_vision 30_design 1.2)."""
    dc.anchor(kit, "pivot_axis", 0.0, 0.0, PIVOT_Z)
    dc.anchor(kit, "pivot_axis_dir", 0.0, 0.10, PIVOT_Z)
    dc.anchor(kit, "map_hinge", 0.0, 0.0, 0.0)
    kit.meta["visualPivotZ"] = PIVOT_Z
    kit.meta["stileArcRadius"] = R_ARC
    for tag, sx in (("s", 1), ("p", -1)):
        if locked:
            dc.anchor(kit, "escutcheon_" + tag, sx * LEAF_X, 0.968, 0.920)
            dc.anchor(kit, "sign_" + tag, sx * LEAF_X, sign_yz[0], sign_yz[1])
            dc.anchor(kit, "tagplate_" + tag, sx * LEAF_X, 1.000, 0.790)
        else:
            dc.anchor(kit, "rose_" + tag, sx * LEAF_X, 1.000, 0.920)
        dc.anchor(kit, "kick_" + tag, sx * 0.0226, 0.145, 0.500)
    latch_y = LATCH_Y_MORTISE if locked else LATCH_Y_BORED
    dc.anchor(kit, "latchbolt", 0.0, latch_y, LEAF_Z_LATCH)
    dc.anchor(kit, "latchbolt_dir", 0.0, latch_y, LEAF_Z_LATCH + 0.10)
    if locked:
        dc.anchor(kit, "deadbolt", 0.0, DEADBOLT_Y, LEAF_Z_LATCH)
        dc.anchor(kit, "deadbolt_dir", 0.0, DEADBOLT_Y, LEAF_Z_LATCH + 0.10)
    dc.anchor(kit, "latch_edge_bottom", 0.0, LEAF_Y0, LEAF_Z_LATCH)
    dc.anchor(kit, "latch_edge_top", 0.0, LEAF_Y1, LEAF_Z_LATCH)
    dc.anchor(kit, "damage_latch", 0.0, 1.10, LEAF_Z_LATCH)
    kit.meta["acting"] = "double"
    kit.meta["doorRoot"] = "D (closed pose): origin = pivot axis (hinge-jamb edge, wall centre plane, floor); +Z along opening; +X = face S; no handing mirror (DA)"


def assert_leaf(kit, slab_obj, what):
    lo, hi = dc.bounds_unity([slab_obj])
    assert abs(hi[0] - LEAF_X) < 1e-4 and abs(lo[0] + LEAF_X) < 1e-4, "%s faces %s %s" % (what, lo, hi)
    assert abs(lo[1] - LEAF_Y0) < 1e-4 and abs(hi[1] - LEAF_Y1) < 1e-4
    assert abs(hi[2] - LEAF_Z_LATCH) < 2e-4 and abs(lo[2] - STILE_APEX_Z) < 2e-4, "%s Z %s %s" % (what, lo[2], hi[2])
    # every vertex inside the swing radius of the latch lining about the visual pivot, and outside the hinge gap
    rmax = LINING_Z_L - PIVOT_Z - 0.0015
    for o in kit.parts:
        mw = o.matrix_world
        for v in o.data.vertices:
            X, Y, Z = to_unity(mw @ v.co)
            assert math.hypot(X, Z - PIVOT_Z) <= rmax, "%s vertex %s outside r %.4f" % (o.name, (X, Y, Z), rmax)
            assert math.hypot(X, Z - PIVOT_Z) <= R_ARC + 1e-4 or Z >= STILE_Z_FACE - 1e-4, "%s vertex %s in the hinge gap" % (o.name, (X, Y, Z))


# --------------------------------------------------------------- the frame
def linings(kit, slot, level, strike=None, grain=True):
    """The 2 mm skins over the wall ends and the head soffit; the hinge lining
    is flat and whole (FIX 1: the stile turns about the visual pivot 3 mm off
    it, so no slot); the latch lining takes the strike pocket (strike = (w, h,
    centre Y), LOD0)."""
    g = (lambda a: a) if grain else (lambda a: None)
    parts = []
    parts.append(dc.obox(kit, slot, lo=(-WALL_X, 0.0, LINING_BACK_H), hi=(WALL_X, LINING_Y, LINING_Z_H), name="lining hinge", grain=g("z")))
    lin_l = dc.obox(kit, slot, lo=(-WALL_X, 0.0, LINING_Z_L), hi=(WALL_X, LINING_Y, LINING_BACK_L), name="lining latch", grain=g("z"))
    lin_t = dc.obox(kit, slot, lo=(-WALL_X, LINING_Y, LINING_BACK_H), hi=(WALL_X, LINING_BACK_Y, LINING_BACK_L), name="lining head", grain=g("y"))
    parts += [lin_l, lin_t]
    if strike is not None and level == 0:
        sw, sh, sy = strike
        dc.cut(lin_l, [dc.cutter((-sw / 2, sy - sh / 2, LINING_Z_L - 0.0001), (sw / 2, sy + sh / 2, LINING_Z_L + STRIKE_DEPTH))])
    return parts


def seals(kit, slot, levels="012"):
    """The black brush seals on the wall centre plane (render-only; the
    "brush strips on both stiles" of 04 §3.2): hinge fin on the hinge lining,
    latch fin, head fin. In every LOD level."""
    out = []
    for (lo, hi), nm in ((HINGE_SEAL, "seal hinge"), (LATCH_FIN, "seal latch fin"), (HEAD_FIN, "seal head fin")):
        o = dc.obox(kit, slot, lo=lo, hi=hi, bevel=0.0003, segs=1, name=nm)
        o["fr_seal"] = True
        tag_lods(o, levels)
        out.append(o)
    return out


def pivot_caps(kit, slot, level):
    """Nothing visible of a pivot but its caps (02 §5): a disc flush under the
    head soffit and a cover plate on the saddle's hinge end, on the axis."""
    segs = CAP_SEGS[level]
    r, t = PIVOT_CAP_HEAD
    head = dc.revolve(kit, [(0.0, 0.0), (r, 0.0), (r, t - 0.0006), (r - 0.0006, t), (0.0, t)],
                      (0.0, LINING_Y, PIVOT_Z), (0, -1, 0), segs, slot, "pivot cap head", close_start=False, close_end=False)
    r, t = PIVOT_CAP_FLOOR
    floor = dc.revolve(kit, [(0.0, 0.0), (r, 0.0), (r, t - 0.0008), (r - 0.0008, t), (0.0, t)],
                       (0.0, SADDLE_H, PIVOT_Z), (0, 1, 0), segs, slot, "pivot cap floor", close_start=False, close_end=False)
    return [head, floor]


def wood_casing_profile():
    """(u, w) of the wood casing: u outward from the lining face, w above X 0.0795. A plain period casing, thick at the jamb and thin
    on the wall. The 4.8 mm reveal is a step (1.5 mm over the map's door trim), one sweep with the casing."""
    r, u1 = WOOD_REVEAL, WOOD_BAND
    wmax = WOOD_X1 - WALL_X
    edge, ri, thin, fall = DOOR_TRIM_X - WALL_X + 0.0015, 0.003, 0.0085, 0.016
    p = [(0.0, 0.0), (0.0, edge), (r, edge), (r, wmax - ri)]
    p += dc._arc(r + ri, wmax - ri, ri, 180, 90, 3)[1:]
    top = wmax - 0.0008
    p += [(u1 - fall, top)]
    p += [(u1 - fall + fall * math.sin(math.radians(a)), thin + (top - thin) * math.cos(math.radians(a))) for a in (22.5, 45.0, 67.5, 90.0)]
    p += [(u1, 0.0)]
    return p


def casing(kit, slot, level, rng, profile=None):
    """Sleeve casing on both faces (wood_casing_profile at LOD0/1, a plain box section
    of the same envelope at LOD2)."""
    prof = profile or wood_casing_profile()
    if level == 2:
        prof = [(0.0, 0.0), (0.0, WOOD_X1 - WALL_X), (WOOD_BAND, WOOD_X1 - WALL_X), (WOOD_BAND, 0.0)]
    out = []
    for side, tag in ((1, "s"), (-1, "p")):
        p = [(u, side * (WALL_X + w)) for u, w in prof]
        legs = dc.u_sweep(kit, p, slot, "casing " + tag, z_h=LINING_Z_H, z_l=LINING_Z_L, y_t=LINING_Y)
        for leg in legs:
            leg["fr_uv_offset"] = (round(rng.uniform(0, 0.6), 3), round(rng.uniform(0, 0.6), 3))
        out += legs
    return out


def steel_sleeve_section(level=0):
    """Closed sheet section of the DA pressed-steel sleeve, (d, X): no formed
    stop (a cased opening, 04 §3.2), d = LINING_Z_H - Z on the hinge jamb.
    Outer path: P return -> P face band -> soffit -> S face band -> S return;
    inner path offset 1.5 mm (16 ga), 3 mm convex bends."""
    zr = LINING_Z_H
    zo, x1 = LINING_Z_H - STEEL_FACE, STEEL_X1
    outer_xz = [(-WALL_X, zo), (-x1, zo), (-x1, LINING_Z_H), (x1, LINING_Z_H), (x1, zo), (WALL_X, zo)]
    convex = dc.BEND_RI + dc.SHEET_T
    radii = [0, convex, convex, convex, convex, 0] if level < 2 else [0] * 6   # fix 1: LOD2 sharp bends (its budget)
    outer = dc.fillet_polygon(outer_xz, radii, (3 if level == 0 else 1), closed=False)
    inner = dc.offset_polyline(outer, dc.SHEET_T, normals_left=False, closed=False)
    poly = outer + list(reversed(inner))
    return [(zr - z, x) for x, z in poly]


# ------------------------------------------------------------------- checks
def assert_frame(kit, what, band, x1):
    """band = the frame's reach past the lining, x1 = its face off the wall centre."""
    dc.assert_box([o for o in kit.parts if "0" in str(o.get("fr_lods", "0"))],
                  (-x1, 0.0, LINING_Z_H - band), (x1, LINING_Y + band, LINING_Z_L + band), what=what)
    # nothing static inside the shut leaf except the seals (1-2 mm overlap by design) and the caps
    for o in kit.parts:
        if o.get("fr_seal"):
            continue
        mw = o.matrix_world
        for v in o.data.vertices:
            X, Y, Z = to_unity(mw @ v.co)
            inside = (-LEAF_X + 1e-4 < X < LEAF_X - 1e-4) and (LEAF_Y0 + 1e-4 < Y < LEAF_Y1 - 1e-4) and \
                     (STILE_APEX_Z + 1e-4 < Z < LEAF_Z_LATCH - 1e-4)
            if inside and Z < STILE_Z_FACE:
                inside = math.hypot(X, Z - PIVOT_Z) < R_ARC - 1e-4
            assert not inside, "%s: %s vertex %s inside the shut leaf" % (what, o.name, (round(X, 4), round(Y, 4), round(Z, 4)))
    # the fins sit on the centre plane
    for o in kit.parts:
        if o.get("fr_seal"):
            lo, hi = dc.bounds_unity([o])
            assert abs(lo[0] + hi[0]) < 1e-5, "%s off the centre plane" % o.name


def frame_anchors(kit, alu_exit_sign=False):
    dc.anchor(kit, "pivot_axis", 0.0, 0.0, PIVOT_Z)
    dc.anchor(kit, "pivot_axis_dir", 0.0, 0.10, PIVOT_Z)
    dc.anchor(kit, "map_hinge", 0.0, 0.0, 0.0)
    kit.meta["visualPivotZ"] = PIVOT_Z
    dc.anchor(kit, "strike", 0.0, STRIKE_MORTISE[2], LINING_Z_L)
    dc.anchor(kit, "strike_bored", 0.0, STRIKE_BORED[2], LINING_Z_L)
    dc.anchor(kit, "head_dust_a", 0.0, LINING_Y, 0.05)
    dc.anchor(kit, "head_dust_b", 0.0, LINING_Y, 0.95)
    dc.anchor(kit, "threshold", 0.0, SADDLE_H, 0.50)
    dc.anchor(kit, "exit_sign_p", -0.080, 2.180, 0.500)
    kit.meta["acting"] = "double"
    kit.meta["doorRoot"] = "D: origin = pivot axis (hinge-jamb edge, wall centre plane, floor); +Z along opening; +X = face S; no handing mirror (DA)"
    kit.meta["seals"] = {"hingeSeal": [list(HINGE_SEAL[0]), list(HINGE_SEAL[1])], "latchFin": [list(LATCH_FIN[0]), list(LATCH_FIN[1])],
                         "headFin": [list(HEAD_FIN[0]), list(HEAD_FIN[1])], "slot": "Prop_PlasticBlack",
                         "note": "brush seals on the centre plane; the A12 gap sheet hides this slot for the no-brush frames"}


def lod_meta(kit, module):
    dc.lod_meta(kit, module)
    kit.meta["lodNote"] = "hand-built LOD0/1/2 (kitlib fr_lods, P-1b); lodDistances (d01, d12, dcull) m at lodBias 1, FOV 76; -1 = never cull"


def wear_frame(P, N, edge, obj):
    X, Y, Z = P
    r = g = b = 1.0
    nm = obj.name
    if Z > 0.95 and ("casing" in nm or "lining" in nm or "jamb" in nm):
        r -= 0.18 * dc.smoothstep(0.75, 0.95, Y) * (1 - dc.smoothstep(1.35, 1.55, Y))
    if edge:
        g -= 0.15 + 0.35 * (1 - dc.smoothstep(0.08, 0.40, Y))
    if "saddle" in nm:
        r -= 0.22 * (1 - min(1.0, abs(X) / 0.05))
        if edge:
            g -= 0.30
    if obj.get("fr_seal"):
        b -= 0.45
    if obj.get("fr_screw"):
        b -= 0.35
    return r, g, b


def wear_leaf(P, N, edge, obj, slab_names=("slab",)):
    X, Y, Z = P
    r = g = b = 1.0
    nm = obj.name
    if any(s in nm for s in slab_names):
        # DA: hands on BOTH faces near the latch end at handle height, and on the radius stile edge (pushed open from either side)
        hand = dc.smoothstep(0.62, 0.92, Z) * dc.smoothstep(0.80, 1.00, Y) * (1 - dc.smoothstep(1.30, 1.50, Y))
        r -= 0.40 * hand
        if edge:
            g -= 0.10 + 0.45 * hand + 0.35 * (1 - dc.smoothstep(0.05, 0.30, Y))
        if Z < STILE_Z_FACE:
            g -= 0.10 * (1 - dc.smoothstep(0.30, 0.60, Y))       # the stile scuffs where shoes kick it
    if obj.get("fr_screw"):
        b -= 0.35
    return r, g, b


# ============================================================ DA lock parts
# The double-acting latch and strikes (10_design §2.1, D6): a single-bevel
# latch can only be shut from its bevel's side, so the DA latchbolt has a
# rounded 30 deg bevel on BOTH faces and no deadlatch plunger, and its strike
# has a short eased lip on BOTH edges. Part frame as interact_lock_common
# (origin on the lock front on the bolt axis, front +Z = the throw).

def latch_outline_da(t, throw, back, land=0.0008, tip_r=0.0004, bevel_deg=30.0, sagitta=0.0004, arc_n=10):
    """Top view (X, Z) of a double-bevel latchbolt, CCW: the tip land
    (|X| <= land / 2, rounded tip_r) at Z = throw, a rounded bevel from each
    end of the land to its face at ``bevel_deg`` off the bolt axis, flat
    faces back to Z = back. Returns (outline, z where the bevels meet the faces)."""
    import interact_lock_common as lc
    hx = t / 2
    x0 = land / 2 + tip_r
    zb = throw - (hx - x0) / math.tan(math.radians(bevel_deg))
    pts = [(-hx, back), (hx, back), (hx, zb)]

    def bevel(p0, p1, outward):
        mid = (p0 + p1) / 2
        nrm = Vector((-(p1 - p0).y, (p1 - p0).x)).normalized()
        if nrm.dot(outward) < 0:
            nrm = -nrm
        half = (p1 - p0).length / 2
        R = (half * half + sagitta * sagitta) / (2 * sagitta)
        ctr = mid - nrm * (R - sagitta)
        a0 = math.atan2(p0.y - ctr.y, p0.x - ctr.x)
        a1 = math.atan2(p1.y - ctr.y, p1.x - ctr.x)
        delta = (a1 - a0 + math.pi) % (2 * math.pi) - math.pi
        return [(ctr.x + R * math.cos(a0 + delta * i / arc_n), ctr.y + R * math.sin(a0 + delta * i / arc_n)) for i in range(1, arc_n + 1)]

    # +X bevel: from the +X face at zb up to the land's +X end
    pts += bevel(Vector((hx, zb)), Vector((x0, throw - tip_r)), Vector((1.0, 1.0)))
    if tip_r > 0:
        pts += lc.arc_pts(x0 - tip_r, throw - tip_r, tip_r, 0, 90, 2, include_start=False)
        pts += lc.arc_pts(-x0 + tip_r, throw - tip_r, tip_r, 90, 180, 2, include_start=True)
    else:
        pts += [(land / 2, throw), (-land / 2, throw)]
    pts += bevel(Vector((-x0, throw - tip_r)), Vector((-hx, zb)), Vector((-1.0, 1.0)))
    return pts, zb


def build_latch_da(kit, T, H, THROW, BACK, slot, level, arc_n=12, round_r=0.0004, round_k=3, name="latchbolt"):
    """One level of a DA latchbolt (LOD0 rounded ends; LOD1 1-step rounds and
    4-point bevels; LOD2 a plain prism with flat bevels)."""
    import interact_lock_common as lc
    m = lc.Mesh()
    if level == 0:
        outline, zb = latch_outline_da(T, THROW, BACK, arc_n=arc_n)
        lc.extrude_rounded_y(m, outline, -H / 2, H / 2, round_r, round_k)
    elif level == 1:
        # fix 1: LOD1 keeps the rounded tip and 6-point bevels with 2-step end rounds (its 210-tri budget)
        outline, zb = latch_outline_da(T, THROW, BACK, tip_r=0.0004, sagitta=0.0003, arc_n=6)
        lc.extrude_rounded_y(m, outline, -H / 2, H / 2, round_r, 2)
    else:
        outline, zb = latch_outline_da(T, THROW, BACK, tip_r=0.0, sagitta=0.0002, arc_n=3)
        m.transform(lambda p: p)
        sections = [[(x, y, z) for x, z in outline] for y in (-H / 2, H / 2)]
        m.loft(sections)
    obj = m.to_object(kit, name + ("" if level == 0 else " lod%d" % level), slot)
    tag_lods(obj, str(level))
    return obj, zb


STRIKE_LIP_OUT = 0.030          # each lip runs from the plate edge (0.016) out to |X| 0.030
STRIKE_LIP_CURL = 0.0008        # <= 1 mm (10_spec §3.1)


def build_strike_da(kit, half_len, openings, lip_y, screw_y, level, screw_segs=24, corner_k=2, box_k=1):
    """One level of a DA strike (part frame of 10_spec §3.1: plate centre on the
    latch lining, front +Z faces the leaf edge; symmetric in X, so the frame's
    Euler(0, 180, 0) placement needs no handing): the plate, its openings with
    dark dust boxes, two countersunk screws, and an eased lip on BOTH edges
    round the latch opening, each curling up STRIKE_LIP_CURL toward the leaf.
    LOD1: no screws, 1-step corners; LOD2: the plate and a dark quad per opening."""
    import interact_lock_common as lc
    zf, zb = lc.STRIKE_PROUD, lc.STRIKE_PROUD - lc.STRIKE_T
    lv = str(level)
    parts = []
    m = lc.Mesh()
    k = corner_k if level == 0 else 1
    if level < 2:
        lc.plate_sweep(m, 0.0, 0.0, lc.STRIKE_W / 2, half_len, 0.002, k, [(0.0, zb), (0.0, zf - 0.0003), (0.0003, zf)])
    else:
        lc.plate_sweep(m, 0.0, 0.0, lc.STRIKE_W / 2, half_len, 0.002, 1, [(0.0, zb), (0.0, zf)])
    plate = m.to_object(kit, "strike plate" + ("" if level == 0 else " lod%d" % level), lc.CHROME)
    tag_lods(plate, lv)
    parts.append(plate)
    if level < 2:
        for y, hx, hy, depth in openings:
            c = lc.Mesh()
            c.prism(lc.rounded_rect(0.0, y, hx, hy, 0.001, box_k), zb - 0.002, zf + 0.002)
            lc.boolean(kit, plate, c)
        if level < 2:
            # fix 1: LOD1 keeps the screw countersinks at 12 segments (its 280-tri budget)
            for sy in screw_y:
                lc.boolean(kit, plate, lc.countersink(0.0, sy, zf, 0.0085 / 2 + 0.00015, cone=0.0010, segs=(screw_segs if level == 0 else 12)), slot=lc.DARK)
        # lips, both edges: a tongue off each long edge round the latch opening
        y0, y1 = lip_y
        rc = 0.006
        for sgn in (1, -1):
            xs = [0.0157, 0.020, 0.024, 0.027, STRIKE_LIP_OUT - rc * 0.3, STRIKE_LIP_OUT] if level == 0 else [0.0157, 0.024, STRIKE_LIP_OUT]
            sections = []
            for x in xs:
                dy = 0.0
                if x > STRIKE_LIP_OUT - rc:
                    dy = rc - math.sqrt(max(0.0, rc * rc - (x - (STRIKE_LIP_OUT - rc)) ** 2))
                u = max(0.0, (x - 0.020) / (STRIKE_LIP_OUT - 0.020))
                zt = zf + STRIKE_LIP_CURL * u * u
                a, b = y0 + dy, y1 - dy
                e = 0.0002
                sec = [(sgn * x, a, zt - lc.STRIKE_T), (sgn * x, b, zt - lc.STRIKE_T), (sgn * x, b, zt - e), (sgn * x, b - e, zt),
                       (sgn * x, a + e, zt), (sgn * x, a, zt - e)]
                sections.append(sec if sgn > 0 else list(reversed(sec)))
            lm = lc.Mesh()
            lm.loft(sections)
            lip = lm.to_object(kit, "strike lip %s" % ("x+" if sgn > 0 else "x-") + ("" if level == 0 else " lod%d" % level), lc.CHROME)
            tag_lods(lip, lv)
            parts.append(lip)
    bm_ = lc.Mesh()
    for y, hx, hy, depth in openings:
        if level < 2:
            top = [bm_.add((x, yy, zb)) for x, yy in lc.rounded_rect(0.0, y, hx, hy, 0.001, box_k)]
            bot = [bm_.add((x, yy, zb - depth)) for x, yy in lc.rounded_rect(0.0, y, hx, hy, 0.001, box_k)]
            n = len(top)
            for i in range(n):
                j = (i + 1) % n
                bm_.face((top[i], top[j], bot[j], bot[i]))
            bm_.face(bot)
        else:
            q = [bm_.add((x, yy, zf + 0.0001)) for x, yy in ((-hx, y - hy), (hx, y - hy), (hx, y + hy), (-hx, y + hy))]
            bm_.face(q)
    boxes = bm_.to_object(kit, "dust boxes" + ("" if level == 0 else " lod%d" % level), lc.DARK, recalc=False)
    tag_lods(boxes, lv)
    parts.append(boxes)
    screws = []
    if level == 0:
        for sy, ang in zip(screw_y, (8.0, -23.0)):
            s = lc.flat_screw(kit, 0.0, sy, zf, D=0.0085, proud=-0.00005, slot_w=0.0009, slot_d=0.0006, ang=ang,
                              segs=screw_segs, name="strike screw")
            tag_lods(s, "0")
            screws.append(s)
    return parts, screws


def strike_wear_da(openings):
    def fn(p, n, slot):
        x, y, z = p
        g = 1.0
        if abs(x) > 0.0157:
            g = 0.62                                 # both lips: the double-bevel latch rides one or the other every close
        for oy, hx, hy, d in openings:
            if abs(y - oy) < hy + 0.0015 and abs(x) < hx + 0.0015:
                g = min(g, 0.72)
        return (1.0, g, 1.0)
    return fn


def lock_tris(kit, level):
    import interact_lock_common as lc
    return sum(lc.tris(o) for o in kit.parts if str(level) in str(o.get("fr_lods", "0")))
