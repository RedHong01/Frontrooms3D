"""G4 windows: the shared section, profiles and builders (interactables spec
10_spec.md §5, §2.3, §9.4, §10.0, §10.4; detail from 06_period_windows.md
§3-§4). Helper module: no NAME, no build(). Imported by
interact_window_frame_wood/steel/alu.py and interact_mini_blind_*.py.

Frames
* Window root W (Unity metres): origin at the opening centre, on the wall
  centre line, at floor level. +Z = face A (the room, non-tall side, kit
  front), -Z = face B (the tall hall). Opening X +/-0.700, Y 0.350-2.000;
  wall faces Z +/-0.080; the map's jamb and head trims (render-only boxes,
  0.07 face x 0.20 deep, MapWorld:999-1009) reach X +/-0.77, Y 2.07 and
  Z +/-0.100.
* Blender (x, y, z) = (-X, -Z, Y) (spec §1.2), written U(X, Y, Z).
  kitlib.export() maps Blender (x, y, z) to Unity (-x, z, -y), so every
  number below lands in the sidecar exactly as written.

Section (§5.2, S1 stop band, sleeve mode, every member)
* Lining / soffit visible face at |X| 0.6995, head Y 1.9995, sill Y 0.3505.
* STOP LINE = SIGHT LINE (the exposed glass edge): X +/-0.6835, Y 0.3665 /
  1.9835. Nothing but glass may sit inside it (assert_clear_zone).
* Stops 16 x 16 mm on BOTH faces, all four sides, Z +/-(0.006 -> 0.022).
* Glass pocket: the 6 mm slab's edge at X +/-0.6955, Y 0.354 / 1.996, glass
  Z +/-0.003. Bite 12.0 mm at the jambs, 12.5 mm at head and sill.
* Tape / compound / gasket: Z +/-(0.0035 -> 0.006) over the whole pocket
  band, front edge flush with the stop line. It is what hides the glass edge
  at 60 degrees (a straight ray that slips under the stop needs
  tan(theta) >= 12 / (3.5 + 3) mm, i.e. theta >= 61.6 deg); without it the
  edge's back 2 mm shows through the glass at 60 deg.
* Face band / casing: X 0.6995 -> 0.775 (the wood casing uses the shared
  door profile, 77 mm), Z 0.080 -> 0.105.

Profiles are (d, n): d = distance outward from the sight-line rectangle
(lining at d = 0.016, face band edge at d = 0.0915), n = Unity Z. Every
profile is listed counter-clockwise round its material in the (d right, n up)
plane, so a step (dd, dn) has the face normal (dn, -dd); sweep_side() orients
every face that way, so nothing depends on bmesh winding. Face B profiles are
mirror_b(face A profile).

Wear (spec §1.8 W1/W2): every part gets the point colour attribute
``fr_wear``, white by default; a channel below 1 means more of it: R = hand
grime, G = edge wear (paint or finish worn through), B = cavity grime.

LOD1 (kitlib.make_lod1 collapses the joined mesh): an open seam between two
pieces can open under the collapse and show the map trims, so (1) members
that collapse weld what can be welded into one sweep (wood: casing A + liner
+ casing B per side); (2) the wood stops are protected with lod_keep();
(3) sweep_side(pin=True) can pin a piece's end rings and wall edges (no
member uses it now: pinning starved the collapse); (4) steel and alu (pass 4)
set a LOD1 ratio the collapse can reach by removing only the wear-station
loops, which lie on the straight sweep and cost zero error (LOD1 within
0.001 mm / 0.06 mm of LOD0); the raised blind needs no collapse at all. The
scratch check runs (a)-(c) on LOD1 too.
"""

import math
import random

import bmesh
import bpy
from mathutils import Vector

# ------------------------------------------------------------------ frames


def U(X, Y, Z):
    """Unity window-root point -> Blender point."""
    return (-X, -Z, Y)


def UD(v):
    """Unity direction -> Blender direction (same linear map)."""
    return Vector((-v[0], -v[2], v[1]))


def to_unity(p):
    """Blender point -> Unity point (what export() writes)."""
    return (-p[0], p[2], -p[1])


SQ2 = math.sqrt(2.0)

# ------------------------------------------------------------- §5.2 section
OPEN_X, OPEN_Y0, OPEN_Y1 = 0.700, 0.350, 2.000
WALL_Z = 0.080
TRIM_Z, TRIM_X1, TRIM_HEAD_Y1 = 0.100, 0.770, 2.070
LINING_X, LINING_Y0, LINING_Y1 = 0.6995, 0.3505, 1.9995
SIGHT_X, SIGHT_Y0, SIGHT_Y1 = 0.6835, 0.3665, 1.9835
GLASS_X, GLASS_Y0, GLASS_Y1 = 0.6955, 0.354, 1.996
GLASS_Z = 0.003
STOP_Z0, STOP_Z1 = 0.006, 0.022
TAPE_Z0 = 0.0035
FACE_X1, FACE_Z = 0.775, 0.105
SHELL_Z0 = 0.0795          # shells start 0.5 mm inside the wall face (as the door's X 0.0795)

HW = SIGHT_X                                   # sight-line rectangle half width
HH = (SIGHT_Y1 - SIGHT_Y0) / 2.0               # 0.8085
CY = (SIGHT_Y1 + SIGHT_Y0) / 2.0               # 1.175
D_LINING = round(LINING_X - SIGHT_X, 6)         # 0.016
D_FACE = round(FACE_X1 - SIGHT_X, 6)            # 0.0915

SLAB = (1.391, 1.642, 0.006)                   # meta["glassSlab"]
SLAB_C = (0.0, 1.175, 0.0)
TOOTH_X, TOOTH_Y1, TOOTH_Y0 = 0.600, 1.900, 0.390
BLIND_RAIL = (0.0, 2.195, 0.1275)

# The interface numbers must agree with each other (the glass track reads them).
assert abs(SLAB[0] / 2 - GLASS_X) < 1e-9 and abs(SLAB_C[1] - SLAB[1] / 2 - GLASS_Y0) < 1e-9
assert abs(SLAB_C[1] + SLAB[1] / 2 - GLASS_Y1) < 1e-9 and abs(SLAB[2] / 2 - GLASS_Z) < 1e-9
assert abs(CY - SLAB_C[1]) < 1e-9
assert abs((GLASS_X - SIGHT_X) - 0.012) < 1e-9                          # jamb bite 12.0 mm
assert abs((GLASS_Y1 - SIGHT_Y1) - 0.0125) < 1e-9 and abs((SIGHT_Y0 - GLASS_Y0) - 0.0125) < 1e-9
assert abs(D_LINING - 0.016) < 1e-9 and abs(LINING_Y1 - SIGHT_Y1 - 0.016) < 1e-9 and abs(SIGHT_Y0 - LINING_Y0 - 0.016) < 1e-9


def interface_anchors(kit, blind=True):
    """§5.4/§5.5 anchors (Unity, window root) plus meta["glassSlab"]."""
    a = {
        "glass_slab": SLAB_C,
        "stop_l": (-SIGHT_X, CY, 0.0), "stop_r": (SIGHT_X, CY, 0.0),
        "stop_t": (0.0, SIGHT_Y1, 0.0), "stop_b": (0.0, SIGHT_Y0, 0.0),
        "pocket_l": (-GLASS_X, CY, 0.0), "pocket_r": (GLASS_X, CY, 0.0),
        "pocket_t": (0.0, GLASS_Y1, 0.0), "pocket_b": (0.0, GLASS_Y0, 0.0),
        "tooth_band_l": (-TOOTH_X, CY, 0.0), "tooth_band_r": (TOOTH_X, CY, 0.0),
        "tooth_band_t": (0.0, TOOTH_Y1, 0.0), "tooth_band_b": (0.0, TOOTH_Y0, 0.0),
        "face_a": (0.0, CY, FACE_Z),
        "sill_plant_a": (0.0, LINING_Y0, 0.05), "sill_plant_b": (0.0, LINING_Y0, -0.05),
        "floor_a": (0.0, 0.0, 0.45), "floor_b": (0.0, 0.0, -0.45),
    }
    if blind:
        a["blind_rail"] = BLIND_RAIL
    for name, p in a.items():
        kit.anchor(name, U(*p))
    kit.meta["glassSlab"] = list(SLAB)
    kit.meta["glassInterface"] = {
        "sightLine": {"x": SIGHT_X, "yBottom": SIGHT_Y0, "yTop": SIGHT_Y1},
        "glassEdge": {"x": GLASS_X, "yBottom": GLASS_Y0, "yTop": GLASS_Y1, "z": GLASS_Z},
        "bite": {"jamb": 0.012, "head": 0.0125, "sill": 0.0125},
        "liningFace": {"x": LINING_X, "yBottom": LINING_Y0, "yTop": LINING_Y1},
        "stopZ": [STOP_Z0, STOP_Z1],
        "tapeZ": [TAPE_Z0, STOP_Z0],
        "toothBand": {"x": TOOTH_X, "yBottom": TOOTH_Y0, "yTop": TOOTH_Y1},
        "paneUV": "u = X + 0.6955, v = Y - 0.354 (pane-space metres)",
    }
    return a


def lod_meta(kit, lod1_ratio, lod2_ratio, distances):
    """(d01, d12, dcull) in metres (spec §1.8). "None" (no cull) is written as
    0.0, the sentinel FrontRoomsKitLibrary.Info.lodDistances documents ("0
    means never cull") and FrontRoomsKitImporter honours (distance <= 0 on
    the last LOD -> no cull); a JSON null in a float[] is not a value
    JsonUtility is documented to accept (G1's door sidecars use -1.0, which
    the importer treats the same way)."""
    kit.meta["lodDistances"] = [0.0 if d is None else float(d) for d in distances]
    kit.meta["lodRatios"] = [lod1_ratio, lod2_ratio]


def wall_placement(kit):
    """Sidecar ``placement: "Wall"`` (service 0), the main project's
    convention for these kits. kitlib.export() derives placement only from
    tags ("wall_decor" -> Wall, kitlib.py:820), so the tag is the one way to
    get it without editing kitlib. Without it every G4 sidecar says "Floor",
    the Level Designer palette offers window frames and blinds as floor
    furniture (window_landing/02_tests_frames.md §6 item 3), and the main
    project had to hand-patch the six JSONs at promotion (2026-10-03 19:xx),
    which any rebuild would silently revert. Runtime code never reads
    placement or this tag (only the editor palette and the map tests do)."""
    kit.tag("wall_decor")


# ---------------------------------------------------------------- profiles


def fillet(corners, closed=False, segs=4):
    """Round the corners of a 2D polyline. corners: (a, b) or (a, b, r) or
    (a, b, r, n) with r the fillet radius at that corner (0 = sharp) and n its
    segment count. Radii are clamped so neighbouring fillets never overlap."""
    pts = []
    for c in corners:
        r = float(c[2]) if len(c) > 2 else 0.0
        n = int(c[3]) if len(c) > 3 else segs
        pts.append((float(c[0]), float(c[1]), r, n))
    m = len(pts)
    out = []
    for i, (a, b, r, n) in enumerate(pts):
        end = not closed and (i == 0 or i == m - 1)
        if r <= 0 or end:
            out.append((a, b))
            continue
        P = Vector((a, b))
        A = Vector(pts[i - 1][:2])
        B = Vector(pts[(i + 1) % m][:2])
        e1, e2 = A - P, B - P
        l1, l2 = e1.length, e2.length
        u1, u2 = e1 / l1, e2 / l2
        ang = math.acos(max(-1.0, min(1.0, u1.dot(u2))))
        if ang < 1e-4 or ang > math.pi - 1e-4:
            out.append((a, b))
            continue
        half = ang / 2.0
        t = r / math.tan(half)
        prev_round = pts[i - 1][2] > 0 and (closed or i - 1 > 0)
        next_round = pts[(i + 1) % m][2] > 0 and (closed or i + 1 < m - 1)
        lim = min(l1 * (0.5 if prev_round else 1.0), l2 * (0.5 if next_round else 1.0)) * 0.999
        if t > lim:
            t = lim
            r = t * math.tan(half)
        T1, T2 = P + u1 * t, P + u2 * t
        C = P + (u1 + u2).normalized() * (r / math.sin(half))
        a1 = math.atan2(T1.y - C.y, T1.x - C.x)
        a2 = math.atan2(T2.y - C.y, T2.x - C.x)
        da = a2 - a1
        while da > math.pi:
            da -= 2 * math.pi
        while da < -math.pi:
            da += 2 * math.pi
        for k in range(n + 1):
            q = a1 + da * k / n
            out.append((C.x + r * math.cos(q), C.y + r * math.sin(q)))
    clean = []
    for p in out:
        if not clean or abs(p[0] - clean[-1][0]) > 1e-7 or abs(p[1] - clean[-1][1]) > 1e-7:
            clean.append(p)
    if closed and len(clean) > 2 and abs(clean[0][0] - clean[-1][0]) < 1e-7 and abs(clean[0][1] - clean[-1][1]) < 1e-7:
        clean.pop()
    return clean


def mirror_b(profile):
    """Face A profile (CCW) -> the same part on face B (CCW)."""
    return [(d, -n) for (d, n) in reversed(profile)]


def ranch_casing_uw(segs=6):
    """§2.3 shared ranch casing with back band, in (u, w): u outward from the
    lining face, w above the wall face (window Z 0.0795). Inner edge first.
    (0,0) -> (0,0.0195) -> R3 round-over -> (0.003,0.0225) -> (0.058,0.0245)
    -> 1.5 mm quirk -> (0.0605,0.0255) -> (0.0735,0.0255) -> R3.5 ->
    (0.077,0.022) -> (0.077,0)."""
    return fillet([
        (0.0, 0.0),
        (0.0, 0.02239, 0.003, segs),          # R3 round-over (tangents ~(0,0.0195) / (0.0029,0.0225))
        (0.058, 0.0245, 0.0003, 1),           # quirk: 1.5 mm wide, 1.3 mm deep
        (0.0583, 0.0232, 0.0003, 1),
        (0.0598, 0.0232, 0.0003, 1),
        (0.0602, 0.0255, 0.0004, 1),          # back band
        (0.077, 0.0255, 0.0035, segs),        # R3.5 (tangents (0.0735,0.0255) / (0.077,0.022))
        (0.077, 0.0),
    ])


def assert_casing_rule(uw):
    """§2.3 hard rule: w >= 0.0215 for u in [0.0015, 0.0735] (stays clear of
    the map trim face at Z 0.100)."""
    worst = 1.0
    for (u0, w0), (u1, w1) in zip(uw, uw[1:]):
        lo, hi = min(u0, u1), max(u0, u1)
        if hi < 0.0015 or lo > 0.0735:
            continue
        for u in (max(lo, 0.0015), min(hi, 0.0735)):
            w = min(w0, w1) if abs(u1 - u0) < 1e-12 else w0 + (w1 - w0) * (u - u0) / (u1 - u0)
            worst = min(worst, w)
    assert worst >= 0.0215 - 1e-9, "casing rule broken: w %.5f < 0.0215" % worst
    return worst


def casing_dn(face="a", segs=6):
    uw = ranch_casing_uw(segs)
    assert_casing_rule(uw)
    prof = [(D_LINING + u, SHELL_Z0 + w) for (u, w) in reversed(uw)]   # face A, CCW: outer edge first
    return prof if face == "a" else mirror_b(prof)


def tape_dn(face="a", lip=0.0004):
    """Glazing tape / compound / gasket: fills Z (0.0035 -> 0.006) over the
    pocket band, front edge on the stop line. Only the two faces that can be
    seen (the stop line edge and the face toward the glass)."""
    prof = fillet([(0.0, STOP_Z0), (0.0, TAPE_Z0, lip, 2), (D_LINING, TAPE_Z0)])
    return prof if face == "a" else mirror_b(prof)


# ------------------------------------------------------------------ sweeps
SIDES = {                       # (d direction, along direction), Unity
    "R": ((1.0, 0.0, 0.0), (0.0, 1.0, 0.0)),
    "L": ((-1.0, 0.0, 0.0), (0.0, 1.0, 0.0)),
    "T": ((0.0, 1.0, 0.0), (1.0, 0.0, 0.0)),
    "B": ((0.0, -1.0, 0.0), (1.0, 0.0, 0.0)),
}


def side_point(side, d, n, a):
    if side == "R":
        return (HW + d, a, n)
    if side == "L":
        return (-(HW + d), a, n)
    if side == "T":
        return (a, CY + HH + d, n)
    return (a, CY - HH - d, n)


def mitre_range(side, d):
    if side in ("R", "L"):
        return (CY - HH - d, CY + HH + d)
    return (-(HW + d), HW + d)


def _vertex_normals(profile, closed):
    """Unit (d, n) normals at each profile point (average of its segments)."""
    m = len(profile)
    seg = []
    for k in range(m if closed else m - 1):
        k2 = (k + 1) % m
        dd = profile[k2][0] - profile[k][0]
        dn = profile[k2][1] - profile[k][1]
        L = math.hypot(dd, dn) or 1.0
        seg.append((dn / L, -dd / L))
    out = []
    for k in range(m):
        ns = []
        if closed or k > 0:
            ns.append(seg[(k - 1) % len(seg)])
        if closed or k < m - 1:
            ns.append(seg[k % len(seg)])
        x = sum(n[0] for n in ns)
        y = sum(n[1] for n in ns)
        L = math.hypot(x, y) or 1.0
        out.append((x / L, y / L))
    return out


def tess_2d(points2d):
    """Triangles (index triples) filling a simple, possibly concave 2D polygon."""
    from mathutils.geometry import tessellate_polygon
    return tessellate_polygon([[Vector((p[0], p[1], 0.0)) for p in points2d]])


def sweep_side(kit, side, profile, slot, start=("mitre", 0.0), end=("mitre", 0.0), stations=(0.0, 1.0),
               caps=(False, False), closed=False, name="sweep", pin=False):
    """One side of a frame: ``profile`` (d, n) swept along ``side`` of the
    sight-line rectangle. End kinds:
      ("mitre", setback)   cut on the 45-degree corner plane, pulled back
                           ``setback`` m (perpendicular): with caps, a hairline
                           joint that shows whatever is behind the piece;
      ("vmitre", w, depth) meet the neighbour exactly on the mitre plane, the
                           last ``w`` m of the surface sinking ``depth`` m into
                           the material: a closed V-groove hairline (no gap,
                           nothing behind it can show);
      ("at", along)        cut square at that along-coordinate (Y on jambs, X
                           on head/sill).
    ``stations`` are length fractions between the ends (wear resolution).
    ``caps`` close "mitre"/"at" ends with the profile polygon. ``pin`` puts
    the end rings, groove shoulders and open edges in kitlib's fr_lod_keep
    group (off by default: pinning a whole frame starves the LOD1 collapse
    and it crushes the unpinned parts instead)."""
    bm = bmesh.new()
    ddir, adir = SIDES[side]
    vn = _vertex_normals(profile, closed)

    def along(d, spec, which):
        lo, hi = mitre_range(side, d)
        if spec[0] == "at":
            return spec[1]
        if spec[0] == "mitre":
            return lo + spec[1] * SQ2 if which == 0 else hi - spec[1] * SQ2
        return (lo + spec[1]) if which == 0 else (hi - spec[1])        # vmitre: groove shoulder

    rows_dna = []
    for k, (d, n) in enumerate(profile):
        A0, A1 = along(d, start, 0), along(d, end, 1)
        row = [(d, n, A0 + (A1 - A0) * f) for f in stations]
        for which, spec in ((0, start), (1, end)):
            if spec[0] != "vmitre":
                continue
            d2 = d - spec[2] * vn[k][0]
            n2 = n - spec[2] * vn[k][1]
            lo, hi = mitre_range(side, d2)
            if which == 0:
                row.insert(0, (d2, n2, lo))
            else:
                row.append((d2, n2, hi))
        rows_dna.append(row)
    grid = [[bm.verts.new(U(*side_point(side, d, n, a))) for (d, n, a) in row] for row in rows_dna]
    m = len(profile)
    ns = len(grid[0])
    for k in range(m if closed else m - 1):
        k2 = (k + 1) % m
        dd = profile[k2][0] - profile[k][0]
        dn = profile[k2][1] - profile[k][1]
        want = UD((ddir[0] * dn, ddir[1] * dn, -dd))
        for s in range(ns - 1):
            f = bm.faces.new((grid[k][s], grid[k][s + 1], grid[k2][s + 1], grid[k2][s]))
            f.normal_update()
            if f.normal.dot(want) < 0:
                f.normal_flip()
    for idx, flag, spec in ((0, caps[0], start), (-1, caps[1], end)):
        if not flag or spec[0] == "vmitre":
            continue
        want = UD(adir) * (-1.0 if idx == 0 else 1.0)
        ring = [grid[k][idx] for k in range(m)]
        for tri in tess_2d([(rows_dna[k][idx][0], rows_dna[k][idx][1]) for k in range(m)]):
            f = bm.faces.new([ring[i] for i in tri])
            f.normal_update()
            if f.normal.dot(want) < 0:
                f.normal_flip()
    pinned = set()
    if pin:
        for k in range(m):
            pinned.update((grid[k][0], grid[k][-1]))
            if start[0] == "vmitre":
                pinned.add(grid[k][1])
            if end[0] == "vmitre":
                pinned.add(grid[k][-2])
        if not closed:
            pinned.update(grid[0])
            pinned.update(grid[-1])
    bm.verts.index_update()
    ids = sorted(v.index for v in pinned)
    obj = kit._new_object(name, bm, slot, "metres", "xz")
    if ids:
        obj.vertex_groups.new(name="fr_lod_keep").add(ids, 1.0, "REPLACE")
    return obj


def frame_ring(kit, profile, slot, end=("mitre", 0.0), caps=False, stations=None, name="ring", sides="RLTB", closed=False,
               pin=False):
    """The same profile on several sides, mitred at the corners (see
    sweep_side for the end kinds)."""
    stations = stations or {}
    return [sweep_side(kit, s, profile, slot, end, end, stations.get(s, (0.0, 1.0)), (caps, caps), closed,
                       name="%s %s" % (name, s), pin=pin) for s in sides]


def extrude_x(kit, section, x0, x1, slot, stations=(0.0, 1.0), caps=(True, True), name="extrude", mapf=None):
    """A closed (Z, Y) section (Unity, any winding) extruded along Unity X
    from x0 to x1; caps ear-clipped; outward normals."""
    mapf = mapf or U
    bm = bmesh.new()
    rings = [[bm.verts.new(mapf(x0 + (x1 - x0) * f, y, z)) for (z, y) in section] for f in stations]
    m = len(section)
    for s in range(len(stations) - 1):
        for k in range(m):
            k2 = (k + 1) % m
            bm.faces.new((rings[s][k], rings[s][k2], rings[s + 1][k2], rings[s + 1][k]))
    tris = tess_2d(section)
    for idx, flag in ((0, caps[0]), (-1, caps[1])):
        if flag:
            for tri in tris:
                bm.faces.new([rings[idx][i] for i in tri])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return kit._new_object(name, bm, slot, "metres", "xz")


def rounded_slab(kit, cx, cz, hx, hz, profile, xs, zs, slot, name="slab", mapf=None):
    """A board lying in Unity X/Z with an edge profile swept round its plan
    (mitred corners): core rectangle (cx +/- hx, cz +/- hz); profile [(e, Y)]
    runs from the top (e = 0) round the edge to the bottom (e = 0), e being
    the outward offset. xs / zs: interior station fractions shared by the
    edge rings and the gridded top and bottom (wear resolution)."""
    mapf = mapf or U
    fx = [0.0] + list(xs) + [1.0]
    fz = [0.0] + list(zs) + [1.0]
    X = [cx - hx + 2 * hx * f for f in fx]
    Z = [cz - hz + 2 * hz * f for f in fz]

    def ring_xy(e):
        pts = []
        for i in range(len(X) - 1):                    # south, Z min, X ascending
            pts.append((X[i] - (e if i == 0 else 0), Z[0] - e))
        for j in range(len(Z) - 1):                    # east, X max, Z ascending
            pts.append((X[-1] + e, Z[j] - (e if j == 0 else 0)))
        for i in range(len(X) - 1, 0, -1):             # north, Z max, X descending
            pts.append((X[i] + (e if i == len(X) - 1 else 0), Z[-1] + e))
        for j in range(len(Z) - 1, 0, -1):             # west, X min, Z descending
            pts.append((X[0] - e, Z[j] + (e if j == len(Z) - 1 else 0)))
        return pts

    bm = bmesh.new()

    def grid(y):
        g = [[bm.verts.new(mapf(X[i], y, Z[j])) for j in range(len(Z))] for i in range(len(X))]
        for i in range(len(X) - 1):
            for j in range(len(Z) - 1):
                bm.faces.new((g[i][j], g[i + 1][j], g[i + 1][j + 1], g[i][j + 1]))
        ring = []
        for i in range(len(X) - 1):
            ring.append(g[i][0])
        for j in range(len(Z) - 1):
            ring.append(g[-1][j])
        for i in range(len(X) - 1, 0, -1):
            ring.append(g[i][-1])
        for j in range(len(Z) - 1, 0, -1):
            ring.append(g[0][j])
        return ring

    rings = [grid(profile[0][1])]
    for (e, y) in profile[1:-1]:
        rings.append([bm.verts.new(mapf(px, y, pz)) for (px, pz) in ring_xy(e)])
    rings.append(grid(profile[-1][1]))
    m = len(rings[0])
    for a, b in zip(rings, rings[1:]):
        for i in range(m):
            j = (i + 1) % m
            bm.faces.new((a[i], a[j], b[j], b[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return kit._new_object(name, bm, slot, "metres", "xz")


# ----------------------------------------------------------------- screws


def slotted_oval_head(kit, origin, axis, slot_dir, slot, R=0.0035, H=0.0015, w=0.0008, floor=-0.0001,
                      rows=3, cols=4, name="screw", mapf=None):
    """Countersunk oval-head slotted screw, head only (the countersink is
    buried). origin = centre of the rim plane (Unity), axis = outward normal
    of the seat, slot_dir = slot direction (projected onto the rim plane).
    The dome is a spherical cap of height H; the slot is a saw cut w wide
    whose floor sits at ``floor`` above the rim plane (negative = through the
    rim, as a real slot; > 0 reads as a paint-filled slot). 64 tris at rows 3, cols 4."""
    mapf = mapf or U
    O = Vector(origin)
    Z = Vector(axis).normalized()
    Xd = Vector(slot_dir)
    Xd = (Xd - Z * Xd.dot(Z)).normalized()
    Yd = Z.cross(Xd)
    rho = (R * R + H * H) / (2 * H)

    def h(x, y):
        r2 = min(x * x + y * y, R * R)
        return math.sqrt(rho * rho - r2) - (rho - H)

    bm = bmesh.new()

    def V(x, y, z):
        p = O + Xd * x + Yd * y + Z * z
        return bm.verts.new(mapf(p.x, p.y, p.z))

    phi0 = math.asin((w / 2) / R)
    phis = [phi0 + (math.pi / 2 - phi0) * j / rows for j in range(rows + 1)]
    walls = {}
    faces = []                       # (face, local want)
    for sgn in (1, -1):
        rows_v = []
        for phi in phis[:-1]:
            y = R * math.sin(phi)
            L = R * math.cos(phi)
            xs = [-L + 2 * L * i / cols for i in range(cols + 1)]
            rows_v.append([(V(x, sgn * y, h(x, y)), x) for x in xs])
        apex = V(0.0, sgn * R, 0.0)
        for j in range(len(rows_v) - 1):
            a, b = rows_v[j], rows_v[j + 1]
            for i in range(cols):
                faces.append((bm.faces.new((a[i][0], a[i + 1][0], b[i + 1][0], b[i][0])), (0, 0, 1)))
        last = rows_v[-1]
        for i in range(cols):
            faces.append((bm.faces.new((last[i][0], last[i + 1][0], apex)), (0, 0, 1)))
        top = rows_v[0]
        bot = [V(x, sgn * w / 2, min(floor, h(x, w / 2) - 0.00005)) for (_, x) in top]
        for i in range(cols):
            faces.append((bm.faces.new((top[i][0], top[i + 1][0], bot[i + 1], bot[i])), (0, -sgn, 0)))
        walls[sgn] = bot
    for i in range(cols):
        faces.append((bm.faces.new((walls[1][i], walls[1][i + 1], walls[-1][i + 1], walls[-1][i])), (0, 0, 1)))
    for f, (lx, ly, lz) in faces:
        wv = Xd * lx + Yd * ly + Z * lz
        f.normal_update()
        if f.normal.dot(UD(wv)) < 0:
            f.normal_flip()
    return kit._new_object(name, bm, slot, "metres", "xz")


# -------------------------------------------------------------- parts/meta


def box_u(kit, size, centre, slot, bevel=0.0005, segments=2, name="box"):
    """Axis box of Unity size (SX, SY, SZ) at Unity centre (spec §1.2)."""
    return kit.box((size[0], size[2], size[1]), U(*centre), slot, bevel=bevel, segments=segments, name=name)


def paint_wear(obj, fn=None):
    """Point colour attribute fr_wear (white = clean). fn(X, Y, Z) in Unity
    window-root metres -> (r, g, b)."""
    bpy.context.view_layer.update()          # matrix_world of freshly placed parts
    mesh = obj.data
    attr = mesh.color_attributes.get("fr_wear") or mesh.color_attributes.new("fr_wear", "BYTE_COLOR", "POINT")
    mw = obj.matrix_world
    for i, v in enumerate(mesh.vertices):
        if fn is None:
            c = (1.0, 1.0, 1.0)
        else:
            p = mw @ v.co
            c = fn(-p.x, p.z, -p.y)
        attr.data[i].color = (c[0], c[1], c[2], 1.0)
    mesh.color_attributes.active_color = attr
    return obj


def lod_keep(obj):
    """Protect a part from kitlib's LOD1 collapse (its own fr_lod_keep vertex
    group, which finish() merges and make_lod1() honours): stops and liners
    keep their edges instead of jagging into see-through specks."""
    vg = obj.vertex_groups.get("fr_lod_keep") or obj.vertex_groups.new(name="fr_lod_keep")
    vg.add([v.index for v in obj.data.vertices], 1.0, "REPLACE")
    return obj


def lod2_drop(obj):
    obj["fr_lod2_drop"] = True
    return obj


def grain(obj, axis):
    """Wood grain along the part's Unity axis ("X", "Y", "Z")."""
    obj["fr_grain"] = {"X": "x", "Y": "z", "Z": "y"}[axis]
    return obj


def unity_verts(kit):
    bpy.context.view_layer.update()
    for obj in kit.parts:
        mw = obj.matrix_world
        for v in obj.data.vertices:
            yield obj.name, to_unity(mw @ v.co)


def assert_clear_zone(kit, eps=1e-6):
    """Self-check (a): no vertex inside X +/-0.6835 x Y 0.3665-1.9835, any Z."""
    bad = [(n, p) for n, p in unity_verts(kit)
           if abs(p[0]) < SIGHT_X - eps and SIGHT_Y0 + eps < p[1] < SIGHT_Y1 - eps]
    assert not bad, "clear zone violated by %d vertices, e.g. %r" % (len(bad), bad[:4])


def unity_bounds(kit):
    lo = [1e9] * 3
    hi = [-1e9] * 3
    for _, p in unity_verts(kit):
        for i in range(3):
            lo[i] = min(lo[i], p[i])
            hi[i] = max(hi[i], p[i])
    return lo, hi


def assert_envelope(kit, lo_want, hi_want, tol=0.0006):
    lo, hi = unity_bounds(kit)
    for i, axis in enumerate("XYZ"):
        assert abs(lo[i] - lo_want[i]) <= tol and abs(hi[i] - hi_want[i]) <= tol, \
            "envelope %s: got %.4f..%.4f, want %.4f..%.4f" % (axis, lo[i], hi[i], lo_want[i], hi_want[i])
    return lo, hi


def rng(seed):
    return random.Random(seed)


def lift_preview(kit, lift):
    """kitlib's turntable (Kit.preview) stands every asset on a floor at
    Blender z = 0. A blind's origin is its rail top (spec §9.4: origin P =
    `blind_rail`), so the whole blind hangs below that floor and the stills
    showed nothing but floor. This wraps preview() on THIS kit instance only
    (kitlib is not edited): for the two stills it lifts the finished mesh by
    ``lift`` m and shifts the bounds the camera frames, then restores both.
    build_asset.py calls preview() after the FBX, the JSON and every variant
    are written, so the exported asset and its sidecar never see the lift."""
    original = kit.preview

    def preview(png_base, samples=48):
        obj = kit.object
        lo, hi = list(kit.meta["boundsMin"]), list(kit.meta["boundsMax"])
        obj.location.z += lift
        kit.meta["boundsMin"] = [lo[0], lo[1], lo[2] + lift]
        kit.meta["boundsMax"] = [hi[0], hi[1], hi[2] + lift]
        try:
            original(png_base, samples)
        finally:
            obj.location.z -= lift
            kit.meta["boundsMin"], kit.meta["boundsMax"] = lo, hi

    kit.preview = preview
    return kit
