"""The walkable stair's kits (line visual (game visual), stage STAIR author, 2026-11-10; package stairwell_walkable;
research/stairwell/60_walkable_stair_for_map.md v1.1 sections 3 and 6, 10_directions.md 1.1 and 2 (look A), 01_research.md 1.1-1.2;
journal W/structures_assets/journal.txt 'STAIR BUILD LIST').

Period construction, 1988-91 US office egress stair (01_research 1.1-1.2; sources there: UBC 1991 S1 / S42-S45, NAAMM pan detail S11,
HABS NJ-979-A-22 S50, ADAAG 1991):
  - steel pan stair: C10 x 15.3 channel stringers (254 deep, 66 flange, 6.1 web, 11.1 flange), toes turned away from the treads,
    12 ga formed pans filled with 38 mm of concrete, the pan's turned front edge as the nosing, closed plate risers (trade practice S11);
    10 risers x 175 / 9 treads x 280 (UBC 1991 3306(c): rise <= 7 in, run >= 11 in), flights 1.12 m (44 in, 3306(b));
  - pan landings: 50 mm fill on a 3 mm pan, a C10 header on the flight edge (the landing's steel nosing), C6 joists under;
  - guards: 42 in (1,067 mm) at landings, at handrail height (34-38 in) along flights (UBC 1991 1712); 1/2 in square pickets
    closing under a 4 in sphere (1712(a)); the bottom rail 2 in over the nosing line so the riser / tread / rail triangle stays under
    6 in; 1-1/4 in NPS rails (42.2 mm OD, inside the 1-1/4 to 2 in grip, 3306(i));
  - wall rails 38 mm clear of the wall (1-1/2 in, 3306(i); ADAAG 4.26.2), 0.91 m over the nosings, 12 in level at the top, one tread
    plus 12 in at the bottom (ADAAG 1991 4.9.4(2); UBC 12 in at both ends), returned to the wall (3306(i); ADAAG 4.26.4) on brackets
    built by G1's _struct_rails.bracket_wall (P4);
  - the Class I standpipe: a 4 in riser (NFPA 14-1990) with a 2-1/2 in angle hose valve at every floor-level landing (UBC 1991 3805(c)),
    the outlet 3-5 ft over the floor (NFPA 14);
  - the headroom barrier: ADAAG 1991 4.4.2, a cane-detectable rail where the clear height under the stair drops below 80 in.

All in UNITY space (x, y, z; metres), converted by _struct_common.ub(); hand-built LOD0 / LOD1 / LOD2 (B.levels). Hero rule as G1-b / G2:
curves at a 0.2 mm chord at LOD0, exposed edges bevelled at LOD0, read at 0.35 m. Generic: no text, label, brand or logo. ASCII only.

Frames (each kit's own; the shell, FrontRoomsStairShell.cs, places them):
  Kit_StairFlight_SteelPan     origin = the upper landing's edge (nosing 0) at the flight's centre line, y 0 = the upper landing; the flight
                               descends along +Z (10 risers, 9 treads, nosing k at z 0.28 k, y -0.175 k), width x -0.56 .. +0.56; the wall
                               side is +X, the well side -X. Riser 1 is the upper landing's header (not in this kit).
  Kit_StairLanding_SteelPan    origin = the landing's flight edge at its centre, y 0 = its surface; it runs to -Z by 1.57 m, x -1.33 .. +1.33.
  Kit_StairGuard_Flight        in the flight's frame: the well-side guard on the -X stringer (axis x -0.56), newel posts on the two landings.
  Kit_StairGuard_Landing_1540  origin = the guard's middle on the floor, along X (-0.77 .. +0.77): the -X end on a wall flange, the +X end on
                               the flight guard's top post (not in this kit); one middle post.
  Kit_StairGuard_Landing_420   origin = the guard's middle on the floor, along X (-0.21 .. +0.21) between the two flight guards' posts.
  Kit_StairRail_Wall           in the flight's frame: the wall rail on the +X wall (wall face x +0.56).
  Kit_StandpipeHoseValve       origin = the riser axis on the landing (y 0); the wall at -Z; the hose outlet to +Z; riser stub y 0.60 .. 1.80.
  Kit_StairBarrier_2660        origin = the barrier's middle on the floor, along X (-1.33 .. +1.33, wall to wall), front +Z.
"""

import math

import bmesh
import bpy
from mathutils import Matrix, Vector

import kitlib
import _run_b_common as B
import _struct_common as S
import _struct_rails as R

# ----------------------------------------------------------------------------- slots (Unity: Resources/Surfaces/<slot>.mat, all own-lights)
STEEL = "Stair_SteelEnamel"     # NEW: the stair's dark brown-grey enamel (#47443E, L* 29; 10_directions 2.2)
FILL = "Stair_Fill"             # main (stairwell_a): grey porch-enamel concrete fill (#BDBEB8)
CHIP = "Stair_PrimerChip"       # NEW: red-oxide primer where the enamel chipped (#6A3323)
RAIL = "Stair_RailEnamel"       # NEW: rail enamel (#3E3B36, L* 25)
WORN = "Stair_RailWorn"         # NEW: the rail's hand-worn bare steel top (#A8A9A4, metallic)
RED = "Stair_RedEnamel"         # NEW: fire-protection red enamel (#8E1E17)
BRASS = "Stair_ValveBrass"      # NEW: cast brass of the hose valve (rough, dulled)

for _slot, (_c, _r, _m) in {
    STEEL: ((0.060, 0.056, 0.048), 0.55, 0.0),
    FILL: ((0.50, 0.51, 0.47), 0.75, 0.0),
    CHIP: ((0.15, 0.037, 0.018), 0.8, 0.0),   # r3: #6A3323, a duller red oxide (r2 read orange)
    RAIL: ((0.048, 0.044, 0.038), 0.5, 0.0),
    WORN: ((0.39, 0.40, 0.38), 0.32, 0.85),
    RED: ((0.27, 0.012, 0.008), 0.45, 0.0),
    BRASS: ((0.42, 0.29, 0.11), 0.42, 1.0),
}.items():
    kitlib.register_slot(_slot, _c, _r, _m)

# ----------------------------------------------------------------------------- the stair's numbers (60 s3 / s6.1)
RISE, GOING, TREADS = 0.175, 0.28, 9
PITCH = RISE / GOING
THETA = math.atan(PITCH)                  # 32.005 deg
HALF_W = 0.56                             # flight half width (1.12 m)
WEB_IN = 0.527                            # the stringers' tread-side web faces (|x|)
WEB_T, FL_W, FL_T, CH_D = 0.0061, 0.066, 0.0111, 0.254   # C10 x 15.3
FILL_T, PAN_T, LIP_H = 0.038, 0.0027, 0.045
STR_UP = 0.060                            # stringer top over the nosing line
STR_VD = CH_D / math.cos(THETA)           # the stringer's vertical depth
LAND_D, LAND_HALF = 1.57, 1.33            # landing depth and half width
RAIL_H, GUARD_H = 0.91, 1.067
TUBE_R = S.TUBE_R                         # 42.2 mm OD
WALL_X = HALF_W                           # the wall face in the flight frame
RAIL_X = WALL_X - S.WALL_CLEAR - TUBE_R   # the wall rail's axis (38.1 mm clear)
GUARD_X = -0.56                           # the guard's axis over the well-side stringer flange
PICKET, PICKET_PITCH = 0.0127, 0.105      # 1/2 in square, 92 mm clear (< 4 in)
BOTTOM_UP = 0.050                         # bottom rail over the nosing line (6 in triangle)
POST = 0.0381                             # 1-1/2 in square newel posts


def nose_y(z):
    """The nosing line at flight z (y = 0 at the top edge)."""
    return -PITCH * z


# ----------------------------------------------------------------------------- generic Unity-space solids
def obox(kit, name, centre, ax, ay, az, hx, hy, hz, slot, lv, bevel=0.0):
    """An oriented box: centre, three orthonormal Unity axes and half sizes; faces wound outward (S.Mesh mirrors for Blender)."""
    c, X, Y, Z = Vector(centre), Vector(ax), Vector(ay), Vector(az)
    def P(i, j, k):
        return tuple(c + X * (hx * i) + Y * (hy * j) + Z * (hz * k))
    m = S.Mesh()
    sx, sy, sz = 2 * hx, 2 * hy, 2 * hz
    # +X, -X, +Y, -Y, +Z, -Z
    m.quad(P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1), (0, 0), (sy, 0), (sy, sz), (0, sz))
    m.quad(P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), P(-1, -1, -1), (0, 0), (sy, 0), (sy, sz), (0, sz))
    m.quad(P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1), (0, 0), (0, sz), (sx, sz), (sx, 0))
    m.quad(P(-1, -1, 1), P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), (0, sz), (0, 0), (sx, 0), (sx, sz))
    m.quad(P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), (0, 0), (sx, 0), (sx, sy), (0, sy))
    m.quad(P(1, -1, -1), P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), (0, 0), (sx, 0), (sx, sy), (0, sy))
    obj = m.part(kit, name, slot)
    if bevel > 0.0 and lv == 0:
        kit._bevel(obj, bevel, 2)
    elif bevel > 0.0 and lv == 1:
        kit._bevel(obj, bevel, 1)
    return obj


def abox(kit, name, lo, hi, slot, lv, bevel=0.0):
    """An axis box from Unity corner lo to corner hi."""
    c = tuple((a + b) * 0.5 for a, b in zip(lo, hi))
    h = tuple(abs(b - a) * 0.5 for a, b in zip(lo, hi))
    return obox(kit, name, c, (1, 0, 0), (0, 1, 0), (0, 0, 1), h[0], h[1], h[2], slot, lv, bevel)


def bar(kit, name, a, b, w, h, up, slot, lv, bevel=0.0):
    """A square / rectangular bar from Unity point a to b: w across (perpendicular to up), h along up's projection."""
    A, Bv = Vector(a), Vector(b)
    t = (Bv - A)
    L = t.length
    t.normalize()
    u = Vector(up)
    u = (u - t * u.dot(t)).normalized()
    s = u.cross(t).normalized()          # (s, u, t) right-handed: obox's faces stay outward
    return obox(kit, name, tuple((A + Bv) * 0.5), tuple(s), tuple(u), tuple(t), w * 0.5, h * 0.5, L * 0.5, slot, lv, bevel)


def prism_x(kit, name, outline, x0, x1, slot, lv, bevel=0.0):
    """A convex (z, y) outline extruded across x from x0 to x1 (x0 < x1); either winding (made counter-clockwise in (y, z))."""
    m = S.Mesh()
    n = len(outline)
    area = sum(outline[i][1] * outline[(i + 1) % n][0] - outline[(i + 1) % n][1] * outline[i][0] for i in range(n))
    if area < 0.0:
        outline = list(reversed(outline))
    # +X face (seen from +X: (z, y) counter-clockwise -> outward +X)
    m.face([m.vert((x1, y, z)) for (z, y) in outline], [(z, y) for (z, y) in outline])
    m.face([m.vert((x0, y, z)) for (z, y) in reversed(outline)], [(z, y) for (z, y) in reversed(outline)])
    for i in range(n):
        za, ya = outline[i]
        zb, yb = outline[(i + 1) % n]
        L = math.hypot(zb - za, yb - ya)
        m.quad((x0, ya, za), (x0, yb, zb), (x1, yb, zb), (x1, ya, za), (0, 0), (L, 0), (L, x1 - x0), (0, x1 - x0))
    obj = m.part(kit, name, slot)
    if bevel > 0.0 and lv <= 1:
        kit._bevel(obj, bevel, 2 if lv == 0 else 1)
    return obj


def arc_tube_path(points, radius, lv):
    """A polyline with its inner corners rounded at a bend radius (pipe bends): Unity points."""
    pts = [Vector(p) for p in points]
    out = [tuple(pts[0])]
    for i in range(1, len(pts) - 1):
        a, b, c = pts[i - 1], pts[i], pts[i + 1]
        d0, d1 = (b - a).normalized(), (c - b).normalized()
        ang = math.acos(max(-1.0, min(1.0, d0.dot(d1))))
        if ang < 1e-3:
            out.append(tuple(b))
            continue
        cut = radius * math.tan(ang * 0.5)
        cut = min(cut, (b - a).length * 0.45, (c - b).length * 0.45)
        p0, p1 = b - d0 * cut, b + d1 * cut
        n = max(2, S.arc_steps(radius, ang, lv, 3))
        for k in range(n + 1):
            t = k / n
            # quadratic Bezier through the corner: close to the arc for the bends used here (<= 90 deg)
            q = p0 * ((1 - t) ** 2) + b * (2 * (1 - t) * t) + p1 * (t ** 2)
            out.append(tuple(q))
    out.append(tuple(pts[-1]))
    return out


def rail_tube(kit, name, path, lv, worn=True, slot=RAIL, r=TUBE_R):
    """A rail tube along a Unity path; worn=True splits the top 70 deg into the hand-worn bare steel (frames' normal = up)."""
    n = S.segs(r, lv, 12)
    if not worn or lv >= 2:
        return S.tube(kit, name, path, r, slot, lv, cap0=False, cap1=False, lo=12)
    half = math.radians(35.0)
    k_w = max(2, int(round(n * (2 * half) / (2 * math.pi))))
    k_e = max(4, n - k_w)
    worn_sec = [(r * math.cos(-half + 2 * half * j / k_w), r * math.sin(-half + 2 * half * j / k_w)) for j in range(k_w + 1)]
    enam_sec = [(r * math.cos(half + (2 * math.pi - 2 * half) * j / k_e), r * math.sin(half + (2 * math.pi - 2 * half) * j / k_e)) for j in range(k_e + 1)]
    fr = S.frames_along(path)
    m1 = S.Mesh()
    S.sweep_section(m1, path, worn_sec, closed_section=False, frames=fr)
    m1.part(kit, name + " worn top", WORN)
    m2 = S.Mesh()
    S.sweep_section(m2, path, enam_sec, closed_section=False, frames=fr)
    return m2.part(kit, name, slot)


def disc_x(kit, name, centre, r, t, facing, slot, lv):
    """A round wall flange on a wall normal to X: its back on the wall at centre, facing +X (facing=1) or -X (-1)."""
    prof = [(r, 0.0), (r, t * 0.6), (r - 0.002, t), (r * 0.5, t * 1.3), (0.0, t * 1.3)]
    if lv >= 1:
        prof = [(r, 0.0), (r, t), (0.0, t * 1.2)]
    return S.lathe_axis(kit, prof, centre, "x" if facing > 0 else "-x", slot, lv, name=name, close_bottom=True)


def disc_y(kit, name, centre, r, t, slot, lv):
    prof = [(r, 0.0), (r, t * 0.6), (r - 0.002, t), (r * 0.45, t * 1.2), (0.0, t * 1.2)]
    if lv >= 1:
        prof = [(r, 0.0), (r, t), (0.0, t)]
    return S.lathe_axis(kit, prof, centre, "y", slot, lv, name=name, close_bottom=True)


def hash01(a, b, c):
    h = ((a * 73856093) ^ (b * 19349663) ^ (c * 83492791)) & 0xFFFFFFFF
    h ^= (h << 13) & 0xFFFFFFFF
    h ^= h >> 17
    h ^= (h << 5) & 0xFFFFFFFF
    return (h & 0xFFFFFF) / 16777216.0


# ----------------------------------------------------------------------------- Kit_StairFlight_SteelPan
def _stringer(kit, side, lv):
    """One C10 channel stringer along the pitch: web (tread side at |x| = WEB_IN), flanges turned away from the treads.
    Plumb cut at the top (against the upper landing's header, z 0, clipped under the landing surface), its foot cut level on the
    lower landing (y -1.75): the top edge runs out onto the landing as on a real pan stair."""
    y_low = -RISE * (TREADS + 1)
    top = lambda z: nose_y(z) + STR_UP
    z_clip = (STR_UP + 0.003) / PITCH                  # where the top edge drops under the landing surface
    z_toe = (STR_UP - y_low) / PITCH                   # the top edge meets the lower landing
    z_heel = (STR_UP - STR_VD - y_low) / PITCH         # the bottom edge meets the lower landing
    web = [(0.0, -0.003), (z_clip, -0.003), (z_toe, y_low), (z_heel, y_low), (0.0, top(0.0) - STR_VD)]
    xin = side * WEB_IN
    xw = side * (WEB_IN + WEB_T)
    prism_x(kit, "stringer web", web, min(xin, xw), max(xin, xw), STEEL, lv, bevel=0.0008)
    if lv >= 2:
        return
    # flanges: top flange along the top edge (sloped part only, the level clip keeps the web's top), bottom flange along the bottom
    xo = side * (WEB_IN + FL_W)
    x0, x1 = min(xin, xo), max(xin, xo)
    n_up = Vector((0.0, math.cos(THETA), math.sin(THETA)))      # perpendicular to the pitch, up (Unity: +z is downhill)
    t_dn = Vector((0.0, -math.sin(THETA), math.cos(THETA)))     # along the pitch, downhill
    def flange(name, a_z, b_z, edge, inward):
        a = Vector((0.0, edge(a_z), a_z))
        b = Vector((0.0, edge(b_z), b_z))
        off = n_up * (-FL_T * 0.5 if inward else FL_T * 0.5)
        mid = (a + b) * 0.5 - off
        L = (b - a).length
        obox(kit, name, ((x0 + x1) * 0.5, mid.y, mid.z), (1, 0, 0), tuple(n_up), tuple(t_dn), (x1 - x0) * 0.5, FL_T * 0.5, L * 0.5, STEEL, lv,
             bevel=0.001)
    # the top flange from the clip to the toe (its outer face on the top edge), the bottom flange from the top end to the heel
    flange("stringer top flange", z_clip + 0.01, z_toe - 0.012, top, False)
    bot = lambda z: top(z) - STR_VD
    flange("stringer bottom flange", 0.012, z_heel - 0.012, bot, True)
    if lv == 0:
        # a bent base plate under the foot and two anchor bolts; a clip angle at the top end against the header
        zf = (z_toe + z_heel) * 0.5
        abox(kit, "stringer base plate", (x0 - 0.004 * side, y_low, z_heel + 0.005), (x1 + 0.004 * side, y_low + 0.008, z_toe - 0.005), STEEL, lv, bevel=0.001)
        for zz in (zf - 0.09, zf + 0.09):
            S.hex_nut(kit, 0.019, 0.011, ((x0 + x1) * 0.5 + side * 0.012, y_low + 0.0135, zz), "y", STEEL, lv, name="anchor nut")
        abox(kit, "stringer clip angle", (min(xin, xin - side * 0.008), -0.21, 0.0), (max(xin, xin - side * 0.008), -0.06, 0.07), STEEL, lv, bevel=0.0008)


def flight(kit, lv):
    """Kit_StairFlight_SteelPan: nine filled pans, risers 2-10, two stringers, nosing chips on the walk lines (LOD0)."""
    xi = WEB_IN
    for k in range(1, TREADS + 1):
        y = -RISE * k
        za, zb = GOING * (k - 1), GOING * k
        # the concrete fill in its pan, its front edge eased against the lip
        abox(kit, "tread fill", (-xi, y - FILL_T, za), (xi, y, zb - PAN_T), FILL, lv, bevel=0.004 if lv == 0 else 0.0)
        if lv <= 1:
            abox(kit, "tread pan", (-xi, y - FILL_T - PAN_T, za - PAN_T), (xi, y - FILL_T, zb), STEEL, lv)
        # the pan's turned front edge: the nosing lip (a 45 mm band, 1.5 mm proud of the riser below), rounded on top
        abox(kit, "nosing lip", (-xi, y - LIP_H, zb - PAN_T), (xi, y + 0.0004, zb + 0.0015), STEEL, lv, bevel=0.0018 if lv == 0 else 0.0)
        if lv == 0:
            # the lip's return under the nosing and two clip angles at the stringers
            abox(kit, "nosing return", (-xi, y - LIP_H, zb - 0.016), (xi, y - LIP_H + PAN_T, zb - PAN_T), STEEL, lv)
            for sx in (-1, 1):
                abox(kit, "pan clip", (sx * xi - (0.0 if sx > 0 else -0.0), y - FILL_T - PAN_T - 0.05, za + 0.06),
                     (sx * (xi - 0.006), y - FILL_T - PAN_T, zb - 0.06), STEEL, lv)
    # risers 2..10 (riser 1 is the upper landing's header): plates flush with the nosing above, down to the pan below / the landing
    for j in range(2, TREADS + 2):
        z = GOING * (j - 1)
        y_top = -RISE * (j - 1) - LIP_H
        y_bot = -RISE * j - (FILL_T + PAN_T if j <= TREADS else 0.0)
        abox(kit, "riser plate", (-xi, y_bot, z - PAN_T), (xi, y_top, z), STEEL, lv)
    for side in (-1, 1):
        _stringer(kit, side, lv)
    if lv == 0:
        # wear on the pan nosings (r3; r1 flat rectangles, r2 2-5 cm flakes read as stickers): where boots strike, along the two walk
        # lines (0.30 m in from each side), the lip's face just under its rounded top edge is worn through the enamel in thin bright
        # streaks of bare steel (20-80 mm long, 1.5-2.5 mm tall), with red-oxide primer flecks (2-5 mm) at their ends; a few tiny chips
        # lower on the face. Spaced along x so nothing overlaps (no z-fight); all on the +z face, 0.15-0.2 mm proud.
        for k in range(1, TREADS + 1):
            y, zb = -RISE * k, GOING * k
            zf = zb + 0.0015
            for si, side in enumerate((-1.0, 1.0)):
                xc = side * (HALF_W - 0.30)
                x = xc - 0.15 + hash01(k, si, 3) * 0.05
                for j in range(4):
                    ln = 0.020 + hash01(k, j, 11 + si) ** 1.3 * 0.060
                    if x + ln > xc + 0.16:
                        break
                    if hash01(k, j, 17 + si) < 0.72:
                        h = 0.0015 + hash01(k, j, 19 + si) * 0.0010
                        _streak(kit, x, x + ln, y - 0.0011, h, zf + 0.00015, hash01(k, j, 23 + si), WORN)
                        for e, xe in enumerate((x - 0.004, x + ln + 0.004)):
                            if hash01(k, j * 2 + e, 29 + si) < 0.6:
                                _flake(kit, (xe, y - 0.0028, zf + 0.0002), 0.0012 + hash01(j, k, 31 + e) * 0.0016, hash01(k, j, 37 + e), CHIP, "+z")
                    x += ln + 0.018 + hash01(k, j, 41 + si) * 0.040
                # tiny chips lower on the lip's face
                for j in range(2):
                    if hash01(k, j, 43 + si) < 0.45:
                        continue
                    xq = xc + (hash01(k, j, 47 + si) - 0.5) * 0.24
                    _flake(kit, (xq, y - 0.012 - hash01(k, j, 53 + si) * 0.022, zf + 0.0002), 0.0012 + hash01(j, k, 59 + si) * 0.0018,
                           hash01(k, j, 61 + si), CHIP, "+z")


def _streak(kit, x0, x1, ytop, h, z, seed, slot):
    """A worn streak on a +z face: a long thin polygon from x0 to x1 under ytop, ragged top and bottom edges, tapered ends."""
    m = S.Mesh()
    n = 6
    top, bot = [], []
    for j in range(n + 1):
        u = j / n
        x = x0 + (x1 - x0) * u
        taper = math.sin(math.pi * u) ** 0.5
        top.append((x, ytop - h * 0.15 * hash01(int(seed * 7919), j, 3), z))
        bot.append((x, ytop - h * (0.25 + 0.75 * taper) * (0.75 + 0.25 * hash01(int(seed * 7919), j, 5)), z))
    pts = bot + list(reversed(top))
    idx = [m.vert(p) for p in pts]
    m.face(tuple(idx), [(p[0], p[1]) for p in pts])
    m.part(kit, "nosing wear streak", slot)


def _flake(kit, centre, size, seed, slot, facing):
    """An irregular paint flake (a 7-9 sided star-ish polygon with jittered radii) lying on a face (+z or +y), 0.3 mm thick visually."""
    m = S.Mesh()
    n = 7 + int(seed * 3)
    pts = []
    for j in range(n):
        a = 2 * math.pi * j / n + seed * 1.7
        r = size * (0.55 + 0.45 * hash01(int(seed * 9973), j, 7)) * (1.0 + 0.6 * math.cos(a * 2 + seed * 5.0) * 0.3)
        if facing == "+z":
            pts.append((centre[0] + r * math.cos(a) * 1.6, centre[1] + r * math.sin(a) * 0.7, centre[2]))
        else:
            pts.append((centre[0] + r * math.cos(a) * 1.6, centre[1], centre[2] + r * math.sin(a) * 0.7))
    idx = [m.vert(p) for p in pts]
    uvs = [(p[0], p[1] if facing == "+z" else p[2]) for p in pts]
    # counter-clockwise seen from the face's outside (right-handed): +z faces as listed, +y faces reversed
    if facing == "+z":
        m.face(tuple(idx), uvs)
    else:
        m.face(tuple(reversed(idx)), list(reversed(uvs)))
    m.part(kit, "nosing flake", slot)


def flight_meta(kit):
    kit.no_collider()
    kit.min_ceiling = 0.0
    kit.anchor("top_nosing", S.ub((0.0, 0.0, 0.0)))
    kit.anchor("bottom_landing_edge", S.ub((0.0, -RISE * (TREADS + 1), GOING * TREADS)))


# ----------------------------------------------------------------------------- Kit_StairLanding_SteelPan
def landing(kit, lv):
    """Kit_StairLanding_SteelPan: 50 mm fill on a 3 mm pan, the C10 header on the flight edge (its top flange is the landing's
    steel nosing), three C6 joists to a wall angle at the back."""
    h = LAND_HALF
    abox(kit, "landing fill", (-h, -0.050, -LAND_D), (h, 0.0, -FL_W), FILL, lv, bevel=0.003 if lv == 0 else 0.0)
    if lv <= 1:
        abox(kit, "landing pan", (-h, -0.053, -LAND_D), (h, -0.050, -WEB_T), STEEL, lv)
    # the header channel: web on the edge, flanges back under the landing
    abox(kit, "header web", (-h, -CH_D, -WEB_T), (h, 0.0, 0.0), STEEL, lv, bevel=0.0008 if lv == 0 else 0.0)
    abox(kit, "header top flange", (-h, -FL_T, -FL_W), (h, 0.0, -WEB_T), STEEL, lv, bevel=0.0015 if lv == 0 else 0.0)
    if lv <= 1:
        abox(kit, "header bottom flange", (-h, -CH_D, -FL_W), (h, -CH_D + FL_T, -WEB_T), STEEL, lv)
        # C6 x 8.2 joists (152 deep, 49 flange) from the header to the wall angle
        for x in (-0.95, 0.0, 0.95):
            abox(kit, "joist web", (x - 0.0025, -0.053 - 0.152, -LAND_D + 0.08), (x + 0.0025, -0.053, -FL_W), STEEL, lv)
            if lv == 0:
                abox(kit, "joist flange", (x - 0.0025, -0.053 - 0.152, -LAND_D + 0.08), (x + 0.046, -0.053 - 0.152 + 0.0087, -FL_W), STEEL, lv)
        # the wall angle (L3 x 3 x 1/4) the back of the landing bears on
        abox(kit, "wall angle", (-h, -0.053 - 0.076, -LAND_D), (h, -0.053, -LAND_D + 0.0064), STEEL, lv)
        abox(kit, "wall angle leg", (-h, -0.053 - 0.0064, -LAND_D), (h, -0.053, -LAND_D + 0.076), STEEL, lv)
    if lv == 0:
        # chips on the header nosing where the walk lines cross it (both flights' lines, 0.30 m in from each flight's sides)
        for i, x in enumerate((-1.03, -0.51, 0.51, 1.03)):
            for j in range(2):
                ln = 0.015 + hash01(i, j, 31) * 0.035
                xc = x + (hash01(i, j, 37) - 0.5) * 0.2
                abox(kit, "header chip", (xc - ln * 0.5, -0.020 - hash01(j, i, 41) * 0.03, 0.0), (xc + ln * 0.5, -0.010, 0.0004), CHIP, lv)


def landing_meta(kit):
    kit.no_collider()
    kit.min_ceiling = 0.0
    kit.anchor("edge", S.ub((0.0, 0.0, 0.0)))


# ----------------------------------------------------------------------------- guards
def _post(kit, name, x, z, y0, y1, lv, cap=True):
    """A 1-1/2 in square newel post on a 100 mm base plate with four bolts, a welded cap plate."""
    abox(kit, name, (x - POST * 0.5, y0 + 0.008, z - POST * 0.5), (x + POST * 0.5, y1, z + POST * 0.5), STEEL, lv, bevel=0.0015)
    if lv <= 1:
        abox(kit, name + " base plate", (x - 0.05, y0, z - 0.05), (x + 0.05, y0 + 0.008, z + 0.05), STEEL, lv, bevel=0.0012)
    if lv == 0:
        for dx in (-0.034, 0.034):
            for dz in (-0.034, 0.034):
                S.hex_nut(kit, 0.0143, 0.008, (x + dx, y0 + 0.012, z + dz), "y", STEEL, lv, name=name + " bolt")
    if cap:
        abox(kit, name + " cap", (x - POST * 0.5 - 0.002, y1, z - POST * 0.5 - 0.002), (x + POST * 0.5 + 0.002, y1 + 0.006, z + POST * 0.5 + 0.002),
             STEEL, lv, bevel=0.0015 if lv == 0 else 0.0)


def _pickets_between(kit, a_top, b_top, a_bot, b_bot, lv, name="picket", skip_ends=0.04):
    """Vertical 1/2 in pickets between a rail line (a_top -> b_top) and a bottom rail line (a_bot -> b_bot) at PICKET_PITCH."""
    A, Bt = Vector(a_top), Vector(b_top)
    horiz = Vector((Bt.x - A.x, 0.0, Bt.z - A.z))
    L = horiz.length
    n = max(1, int(math.ceil((L - 2 * skip_ends) / PICKET_PITCH)))
    step = (L - 2 * skip_ends) / n
    for i in range(n + 1):
        t = (skip_ends + step * i) / L
        top = Vector(a_top).lerp(Vector(b_top), t)
        bot = Vector(a_bot).lerp(Vector(b_bot), t)
        y0, y1 = bot.y + 0.0127, top.y - TUBE_R * 0.8
        abox(kit, name, (top.x - PICKET * 0.5, y0, top.z - PICKET * 0.5), (top.x + PICKET * 0.5, y1, top.z + PICKET * 0.5), STEEL, lv,
             bevel=0.0007 if lv == 0 else 0.0)


def guard_flight(kit, lv):
    """Kit_StairGuard_Flight: the well-side guard of one flight on its -X stringer: the top rail at handrail height (0.91 m over the
    nosings, worn bare on top), a 1 in square bottom rail 2 in over the nosing line, 1/2 in pickets at 105 mm, newel posts on the upper
    landing (to the 42 in landing guard) and on the lower landing."""
    x = GUARD_X
    z_top_post, z_bot_post = -0.035, GOING * TREADS + 0.035
    y_low = -RISE * (TREADS + 1)
    rail = lambda z: nose_y(z) + RAIL_H
    # posts: the top one carries the 42 in landing guard; the bottom one meets the next landing guard at its 42 in
    _post(kit, "newel post top", x, z_top_post, 0.0, GUARD_H, lv)
    _post(kit, "newel post bottom", x, z_bot_post, y_low, y_low + GUARD_H, lv)
    za, zb = z_top_post + POST * 0.5, z_bot_post - POST * 0.5
    a, b = (x, rail(za), za), (x, rail(zb), zb)
    rail_tube(kit, "guard rail", [a, b], lv, worn=True)
    # the bottom rail: 25 mm square tube, 2 in over the nosing line, between the posts
    ba, bb = (x, nose_y(za) + BOTTOM_UP, za), (x, nose_y(zb) + BOTTOM_UP, zb)
    bar(kit, "guard bottom rail", ba, bb, 0.0254, 0.0254, (0, 1, 0), STEEL, lv, bevel=0.001 if lv == 0 else 0.0)
    if lv <= 1:
        _pickets_between(kit, a, b, ba, bb, lv)
    if lv == 0:
        # the bottom rail's tabs welded to the stringer's top flange, one every fourth picket
        for k in range(1, TREADS + 1, 2):
            z = GOING * (k - 0.5)
            abox(kit, "guard tab", (x - 0.003, nose_y(z) + STR_UP - 0.004, z - 0.02), (x + 0.003, nose_y(z) + BOTTOM_UP, z + 0.02), STEEL, lv)


def guard_flight_meta(kit):
    kit.no_collider()
    kit.min_ceiling = 0.0
    kit.anchor("top_post", S.ub((GUARD_X, GUARD_H, -0.035)))
    kit.anchor("bottom_post", S.ub((GUARD_X, -RISE * (TREADS + 1) + GUARD_H, GOING * TREADS + 0.035)))


def guard_landing(kit, lv, length, wall_end, mid_post):
    """Kit_StairGuard_Landing_*: a 42 in guard along X over a landing edge: top rail (worn), bottom rail 2 in over the floor, pickets;
    ends on the adjoining posts (not in the kit) or, wall_end, on a wall flange at -X; mid_post adds a newel at the middle."""
    h = length * 0.5
    xa, xb = -h + (0.0 if wall_end else POST * 0.5), h - POST * 0.5
    if wall_end:
        disc_x(kit, "rail wall flange", (-h, GUARD_H, 0.0), 0.036, 0.006, 1, STEEL, lv)
        disc_x(kit, "bottom rail wall flange", (-h, BOTTOM_UP + 0.0127, 0.0), 0.028, 0.005, 1, STEEL, lv)
    rail_tube(kit, "landing guard rail", [(xa, GUARD_H, 0.0), (xb, GUARD_H, 0.0)], lv, worn=True)
    bar(kit, "landing bottom rail", (xa, BOTTOM_UP + 0.0127, 0.0), (xb, BOTTOM_UP + 0.0127, 0.0), 0.0254, 0.0254, (0, 1, 0), STEEL, lv,
        bevel=0.001 if lv == 0 else 0.0)
    if mid_post:
        _post(kit, "landing guard post", 0.0, 0.0, 0.0, GUARD_H, lv, cap=False)
    if lv <= 1:
        spans = [(xa, -POST * 0.5), (POST * 0.5, xb)] if mid_post else [(xa, xb)]
        for (s0, s1) in spans:
            _pickets_between(kit, (s0, GUARD_H, 0.0), (s1, GUARD_H, 0.0), (s0, BOTTOM_UP, 0.0), (s1, BOTTOM_UP, 0.0), lv, skip_ends=0.05)


# ----------------------------------------------------------------------------- Kit_StairRail_Wall
def _parts_since(kit, n0):
    return list(kit.parts[n0:])


def _transform_parts(objs, M_unity, slot=None, kit=None):
    """Apply a Unity-space rigid transform (4x4, det +1) to parts built in Unity space at the origin, re-slotting them."""
    U = Matrix(((-1, 0, 0, 0), (0, 0, -1, 0), (0, 1, 0, 0), (0, 0, 0, 1)))   # ub(): Unity -> Blender
    Mb = U @ M_unity @ U.inverted()
    for o in objs:
        o.matrix_basis = Mb @ o.matrix_basis
        if slot is not None and kit is not None:
            for i in range(len(o.data.materials)):
                o.data.materials[i] = kit._material(slot)


def _bracket(kit, lv, at, along, slot):
    """G1's cast swan-neck wall bracket (_struct_rails.bracket_wall: rail axis at the origin along X, wall at -Z) carried onto a wall
    at +X under a rail running along +Z (sloped rails tilt only the saddle, as a swivel saddle does)."""
    n0 = len(kit.parts)
    R.bracket_wall(kit, lv)
    objs = _parts_since(kit, n0)
    tilt = math.atan2(-along[1], math.hypot(along[0], along[2]))
    saddle = [o for o in objs if o.name.startswith("saddle")]
    if abs(tilt) > 1e-4 and saddle:
        _transform_parts(saddle, Matrix.Rotation(-tilt, 4, "Z"))
    # G1 frame -> ours: G1 +X (rail) -> +Z, G1 +Y -> +Y, G1 +Z (out of the wall) -> -X (the wall is at +X)
    Rm = Matrix(((0, 0, -1, 0), (0, 1, 0, 0), (1, 0, 0, 0), (0, 0, 0, 1)))
    _transform_parts(objs, Matrix.Translation(Vector(at)) @ Rm, slot, kit)


def rail_wall(kit, lv):
    """Kit_StairRail_Wall: the 1-1/4 in NPS wall rail on the +X wall, 0.91 m over the nosings, 12 in level at the top, one tread
    sloping then 12 in level at the bottom, bent (R 0.10) and returned to the wall on cast flanges, on four G1 brackets, the hand-worn
    bare steel along its top."""
    x = RAIL_X
    ext = 0.305
    z0, z9 = 0.0, GOING * TREADS
    y_at = lambda z: nose_y(z) + RAIL_H
    zs = z9 + GOING                                 # one tread past the bottom riser
    y_low = -RISE * (TREADS + 1)
    pts = [(x, RAIL_H, z0 - ext), (x, RAIL_H, z0), (x, y_at(zs), zs), (x, y_low + RAIL_H, zs + ext)]
    # the level ends: (x, y, z0 - ext) .. bend at z0 .. slope .. bend at zs .. level to zs + ext
    path = arc_tube_path([pts[0], pts[1], pts[2], pts[3]], 0.10, lv)
    rail_tube(kit, "wall rail", path, lv, worn=True)
    # returns to the wall: a cast short-radius elbow (R 0.040) and a stub into a cast flange
    for (zend, sgn) in ((z0 - ext, -1.0), (zs + ext, 1.0)):
        yr = RAIL_H if sgn < 0 else y_low + RAIL_H
        rr = 0.040                                 # a cast short-radius return (the axis is 59 mm off the wall)
        n = max(3, S.arc_steps(rr, math.pi / 2, lv))
        arc = []
        for k in range(n + 1):
            a = (math.pi / 2) * k / n
            arc.append((x + rr - rr * math.cos(a), yr, zend + sgn * rr * math.sin(a)))
        ret = [(x, yr, zend)] + arc[1:] + [(WALL_X - 0.006, yr, zend + sgn * rr)]
        S.tube(kit, "wall rail return", ret, TUBE_R, RAIL, lv, cap0=False, cap1=True, lo=12)
        disc_x(kit, "wall rail flange", (WALL_X, yr, zend + sgn * rr), 0.036, 0.007, -1, RAIL, lv)
        if lv == 0:
            for k in range(3):
                a = math.radians(90.0 + 120.0 * k)
                S.screw_head(kit, 0.0042, (WALL_X - 0.0085, yr + 0.024 * math.sin(a), zend + sgn * rr + 0.024 * math.cos(a)), "-x", RAIL, lv,
                             name="flange screw", dome=0.22)
    # four brackets: three under the slope, one under the bottom extension
    slope = Vector((0.0, -RISE, GOING)).normalized()
    for t in (0.17, 0.5, 0.83):
        z = z0 + (z9 - z0) * t
        _bracket(kit, lv, (x, y_at(z), z), tuple(slope), RAIL)
    _bracket(kit, lv, (x, y_low + RAIL_H, zs + ext * 0.45), (0.0, 0.0, 1.0), RAIL)


def rail_wall_meta(kit):
    kit.no_collider()
    kit.min_ceiling = 0.0
    kit.anchor("wall", S.ub((WALL_X, 0.0, 0.0)))


# ----------------------------------------------------------------------------- Kit_StandpipeHoseValve
RISER_R = 0.05715          # 4 in schedule 40, 114.3 mm OD
VALVE_Y = 1.22             # the tee's branch: the outlet 3-5 ft over the landing (NFPA 14-1990)
WALL_GAP = 0.090           # riser axis to the wall


def _torus_y(kit, name, centre, R_, r, slot, lv, n_major=24, axis="y"):
    """A ring (torus) of major radius R_ and tube radius r round a Unity axis through centre."""
    c = Vector(centre)
    n = max(10, S.segs(R_, lv, 12)) if lv < 2 else 10
    pts = []
    for k in range(n + 1):
        a = 2 * math.pi * k / n
        if axis == "y":
            pts.append(tuple(c + Vector((R_ * math.cos(a), 0.0, R_ * math.sin(a)))))
        elif axis == "z":
            pts.append(tuple(c + Vector((R_ * math.cos(a), R_ * math.sin(a), 0.0))))
        else:
            pts.append(tuple(c + Vector((0.0, R_ * math.cos(a), R_ * math.sin(a)))))
    return S.tube(kit, name, pts, r, slot, lv, cap0=False, cap1=False, lo=6)


def _grooved_coupling(kit, y, lv):
    """A 4 in grooved coupling: two cast housings round the pipe ends, two bolt pads with track bolts and nuts at +-X."""
    prof = [(RISER_R + 0.001, -0.026), (RISER_R + 0.012, -0.024), (RISER_R + 0.018, -0.016), (RISER_R + 0.019, 0.0),
            (RISER_R + 0.018, 0.016), (RISER_R + 0.012, 0.024), (RISER_R + 0.001, 0.026)]
    if lv >= 1:
        prof = [(RISER_R + 0.001, -0.026), (RISER_R + 0.019, -0.016), (RISER_R + 0.019, 0.016), (RISER_R + 0.001, 0.026)]
    S.lathe_y(kit, [(r, y + dy) for (r, dy) in prof], (0.0, 0.0, 0.0), RED, lv, name="coupling housing", close_top=False, close_bottom=False)
    for sx in (-1, 1):
        abox(kit, "coupling bolt pad", (sx * (RISER_R + 0.010) - 0.012, y - 0.022, -0.016), (sx * (RISER_R + 0.034) + 0.0, y + 0.022, 0.016), RED, lv,
             bevel=0.003 if lv == 0 else 0.0)
        if lv <= 1:
            S.cyl_y(kit, 0.0055, 0.075, (sx * (RISER_R + 0.024), y, 0.0), STEEL, lv, name="track bolt")
            S.hex_nut(kit, 0.019, 0.012, (sx * (RISER_R + 0.024), y + 0.030, 0.0), "y", STEEL, lv, name="coupling nut")
            if lv == 0:
                S.hex_nut(kit, 0.019, 0.010, (sx * (RISER_R + 0.024), y - 0.029, 0.0), "y", STEEL, lv, name="track bolt head")


def standpipe_valve(kit, lv):
    """Kit_StandpipeHoseValve: the 4 in red riser stub with grooved couplings, a 4 x 2-1/2 in tee, the nipple, a 2-1/2 in cast brass angle
    hose valve (bonnet up, red handwheel), its outlet turned 45 deg down with the NST male thread and a capped, chained cap, and a riser
    clamp to the wall."""
    y0, y1 = 0.60, 1.80
    S.cyl_y(kit, RISER_R, y1 - y0, (0.0, (y0 + y1) * 0.5, 0.0), RED, lv, name="riser", lo=16)
    _grooved_coupling(kit, y0 + 0.03, lv)
    _grooved_coupling(kit, y1 - 0.03, lv)
    # the tee: a cast run with a bead at each end and the branch boss to +Z
    tprof = [(RISER_R + 0.002, -0.13), (RISER_R + 0.010, -0.125), (RISER_R + 0.010, -0.105), (RISER_R + 0.006, -0.100),
             (RISER_R + 0.006, 0.100), (RISER_R + 0.010, 0.105), (RISER_R + 0.010, 0.125), (RISER_R + 0.002, 0.13)]
    if lv >= 1:
        tprof = [(RISER_R + 0.002, -0.13), (RISER_R + 0.010, -0.12), (RISER_R + 0.010, 0.12), (RISER_R + 0.002, 0.13)]
    S.lathe_y(kit, [(r, VALVE_Y + dy) for (r, dy) in tprof], (0.0, 0.0, 0.0), RED, lv, name="tee run", close_top=False, close_bottom=False)
    bprof = [(0.046, 0.0), (0.046, RISER_R + 0.03), (0.050, RISER_R + 0.034), (0.050, RISER_R + 0.052), (0.044, RISER_R + 0.056)]
    S.lathe_axis(kit, bprof, (0.0, VALVE_Y, 0.0), "z", RED, lv, name="tee branch", close_top=True, close_bottom=False)
    # the 2-1/2 in nipple
    z_n0 = RISER_R + 0.056
    S.cyl_z(kit, 0.0365, 0.05, (0.0, VALVE_Y, z_n0 + 0.025), BRASS if lv == 0 else BRASS, lv, name="nipple", lo=16)
    # the angle valve: a hex inlet, a cast globe body, the bonnet and stem up, the outlet 45 deg down to +Z
    z_b = z_n0 + 0.05 + 0.055
    S.hex_nut(kit, 0.082, 0.030, (0.0, VALVE_Y, z_n0 + 0.05 + 0.012), "z", BRASS, lv, name="valve inlet hex")
    body = [(0.0, -0.058), (0.030, -0.055), (0.050, -0.035), (0.056, -0.005), (0.054, 0.020), (0.044, 0.042), (0.030, 0.056), (0.0, 0.060)]
    if lv >= 1:
        body = [(0.0, -0.058), (0.048, -0.035), (0.054, 0.010), (0.034, 0.052), (0.0, 0.060)]
    S.lathe_y(kit, [(r, VALVE_Y + dy) for (r, dy) in body], (0.0, 0.0, z_b), BRASS, lv, name="valve body")
    # bonnet, packing nut, stem
    S.hex_nut(kit, 0.070, 0.034, (0.0, VALVE_Y + 0.075, z_b), "y", BRASS, lv, name="valve bonnet")
    S.hex_nut(kit, 0.036, 0.022, (0.0, VALVE_Y + 0.104, z_b), "y", BRASS, lv, name="packing nut")
    S.cyl_y(kit, 0.0075, 0.06, (0.0, VALVE_Y + 0.135, z_b), BRASS, lv, name="stem")
    # the red handwheel: rim, four spokes, hub
    yw = VALVE_Y + 0.152
    _torus_y(kit, "handwheel rim", (0.0, yw, z_b), 0.060, 0.0065, RED, lv)
    S.cyl_y(kit, 0.016, 0.022, (0.0, yw, z_b), RED, lv, name="handwheel hub")
    for k in range(4 if lv <= 1 else 2):
        a = math.radians(45.0 + 90.0 * k) if lv <= 1 else math.radians(45.0 + 180.0 * k)
        d = Vector((math.cos(a), 0.0, math.sin(a)))
        p0 = Vector((0.0, yw, z_b)) + d * 0.014
        p1 = Vector((0.0, yw, z_b)) + d * 0.056
        S.tube(kit, "handwheel spoke", [tuple(p0), tuple(p1)], 0.0045, RED, lv, cap0=False, cap1=False, lo=6)
    if lv == 0:
        S.screw_head(kit, 0.007, (0.0, yw + 0.011, z_b), "+y", BRASS, lv, name="handwheel nut", dome=0.5)
    # the outlet: 45 deg down to +Z, a neck, the NST male thread (rings), the cap with rocker lugs
    d_out = Vector((0.0, -math.sqrt(0.5), math.sqrt(0.5)))
    c0 = Vector((0.0, VALVE_Y, z_b)) + d_out * 0.040
    def along(t):
        return tuple(c0 + d_out * t)
    S.tube(kit, "valve outlet neck", [along(0.0), along(0.050)], 0.040, BRASS, lv, cap0=False, cap1=False, lo=16)
    if lv == 0:
        for i in range(5):
            S.tube(kit, "outlet thread", [along(0.052 + i * 0.0065), along(0.0555 + i * 0.0065)], 0.0405, BRASS, lv, cap0=True, cap1=True, lo=16)
    S.tube(kit, "outlet thread core", [along(0.050), along(0.086)], 0.0385, BRASS, lv, cap0=False, cap1=False, lo=16)
    S.tube(kit, "hose cap", [along(0.078), along(0.112)], 0.0465, BRASS, lv, cap0=True, cap1=True, lo=16)
    if lv <= 1:
        # rocker lugs on the cap
        side = Vector((1.0, 0.0, 0.0))
        for sgn in (-1, 1):
            q0 = Vector(along(0.090)) + side * (sgn * 0.044)
            q1 = Vector(along(0.106)) + side * (sgn * 0.044)
            S.tube(kit, "cap lug", [tuple(q0), tuple(q1)], 0.0075, BRASS, lv, cap0=True, cap1=True, lo=8)
    if lv == 0:
        # the cap's chain: four links hanging from the cap's lug back to an eye on the valve body
        eye = Vector((0.050, VALVE_Y - 0.02, z_b + 0.01))
        start = Vector(along(0.098)) + Vector((0.050, 0.0, 0.0))
        for i in range(4):
            t = (i + 0.5) / 4.0
            p = start.lerp(eye, t) + Vector((0.0, -0.035 * math.sin(math.pi * t), 0.0))
            _torus_y(kit, "cap chain link", tuple(p), 0.0055, 0.0013, BRASS, lv, axis="z" if i % 2 == 0 else "x")
    # the riser clamp: a U-bolt band round the riser and a strut stub to the wall (wall at -Z)
    yc = 1.70
    if lv <= 1:
        abox(kit, "strut channel", (-0.0205, yc - 0.0205, -WALL_GAP), (0.0205, yc + 0.0205, -RISER_R - 0.004), STEEL, lv,
             bevel=0.0015 if lv == 0 else 0.0)
        rr = RISER_R + 0.005
        n = max(6, S.arc_steps(rr, math.pi, lv))
        half_ring = [(rr, yc, -RISER_R - 0.012)]
        for k in range(n + 1):
            a = math.pi * k / n
            half_ring.append((rr * math.cos(a), yc, rr * math.sin(a)))
        half_ring.append((-rr, yc, -RISER_R - 0.012))
        S.tube(kit, "u-bolt", half_ring, 0.0048, STEEL, lv, cap0=True, cap1=True, lo=6)
    if lv == 0:
        for sx in (-1, 1):
            S.hex_nut(kit, 0.0143, 0.008, (sx * (RISER_R + 0.005), yc, -RISER_R - 0.012), "z", STEEL, lv, name="u-bolt nut")


def standpipe_meta(kit):
    kit.no_collider()
    kit.min_ceiling = 0.0
    kit.anchor("riser_bottom", S.ub((0.0, 0.60, 0.0)))
    kit.anchor("riser_top", S.ub((0.0, 1.80, 0.0)))
    kit.anchor("outlet", S.ub((0.0, VALVE_Y - 0.09, RISER_R + 0.17)))


# ----------------------------------------------------------------------------- Kit_StairBarrier_2660
def barrier(kit, lv):
    """Kit_StairBarrier_2660: ADAAG 1991 4.4.2's barrier where the clear height under the stair drops below 80 in: two newel posts,
    a top rail at 0.91 m and a cane-detectable bottom rail at 0.15 m, wall to wall on cast flanges."""
    h = LAND_HALF
    for x in (-0.62, 0.62):
        _post(kit, "barrier post", x, 0.0, 0.0, RAIL_H + 0.02, lv)
    for (y, worn) in ((RAIL_H, False), (0.15, False)):
        rail_tube(kit, "barrier rail", [(-h + 0.006, y, 0.0), (h - 0.006, y, 0.0)], lv, worn=worn)
        disc_x(kit, "barrier flange", (-h, y, 0.0), 0.036, 0.006, 1, RAIL, lv)
        disc_x(kit, "barrier flange", (h, y, 0.0), 0.036, 0.006, -1, RAIL, lv)


def small_meta(kit):
    kit.no_collider()
    kit.min_ceiling = 0.0
