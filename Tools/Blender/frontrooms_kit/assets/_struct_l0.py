"""P16 structures_l0_props, part 2: the SP3 training room and file mezzanine of 1988-91 (structures_assets G3-a2, line
visual (game visual), 2026-11-10). Geometry for every kit of the two rooms; the thin structures_*.py modules name them.

Plan: Documentation/research/structures_assets/30_plan.md P16 / S3-09 / S3-12; specs 62_furniture_l0_office.md 2.4, 2.9
and 3.1 (LS57, LS59, LS91, LS93, LS94, LS97-LS99); real sizes 60_build.md G3-a2 R1-R6: Spacesaver US 4,984,737 (filed
1989): carriages on floor rail sets, a hand wheel driving a chain to the long shaft, side panels at the ends (R1); a
Micron 775 standard-design microfiche reader, 12 w x 20.5 h x 18 l in, 11.5 x 9.75 in blue-tinted screen, the fiche tray
below it (R2); a 70 x 70 in pull-down in a 22 ga octagonal case 73.5 in long (R3); a four-drawer lateral file 36 x 18 x
52.5 in with full-width recessed aluminium pulls and label holders (R4); AV carts of 24 x 18 in shelves, 26 and 42 in
high, 4 in casters, a three-outlet power assembly (R5); open-format end-tab files 36 in wide on seven tiers, letter
end-tab folders 9.5 in tall (R6); an overhead projector 14.2 x 16.75 x 28 in with a 10.5 in stage 8.5-9 in up and a
fold-down post (LS91); the NARA cubic-foot records carton (LS94).

Frames and rules as _struct_common and _struct_l0_computer: every kit is specified in UNITY space (front +Z = the side a
person uses, origin the floor centre unless the kit says otherwise) and converted with ub(); levels built by hand
(B.levels: LOD0 hero, LOD1 about 40 %, LOD2 boxes). New slots: Prop_PaperAtlas (decal atlas of paper and print, uv_rect
from CELLS: kept in step with Tools/lookdev/structures/gen_structures_g3a2.py, and folder_layout() kept in step with it),
Prop_ProjectionLit / Prop_ProjectionLit_Off (the lit projection faces: the screen's square of light, the overhead's stage,
the fiche page; the ONE slot a kit's light uses, swapped for _Off), Prop_LaminateFawn / Prop_LaminateDove (one matte
laminate map set, tinted). No text, label, brand or logo. ASCII only.
"""

import math

import bmesh
import bpy
from mathutils import Matrix, Vector

import kitlib
import _run_b_common as B
import _struct_common as S
import _struct_l0_computer as C

PAPER, PLIT, PLIT_OFF = "Prop_PaperAtlas", "Prop_ProjectionLit", "Prop_ProjectionLit_Off"
FAWN, DOVE = "Prop_LaminateFawn", "Prop_LaminateDove"
for _slot, _c, _r in ((PAPER, (0.75, 0.70, 0.60), 0.6), (PLIT, (0.9, 0.88, 0.8), 0.3), (PLIT_OFF, (0.8, 0.8, 0.78), 0.4),
                      (FAWN, (0.74, 0.66, 0.53), 0.5), (DOVE, (0.60, 0.60, 0.57), 0.5), ("Prop_FrostedGlass", (0.9, 0.9, 0.87), 0.3)):
    kitlib.register_slot(_slot, _c, _r)

STEEL = "Prop_SteelPutty"
ALMOND = "Prop_SteelAlmond"
SBLACK = "Prop_SteelBlack"
CHROME = "Prop_Chrome"
ALU = "Prop_Aluminium"
SATIN = "Trans_AluminiumSatin"
BLACK = "Prop_PlasticBlack"
PGREY = "Prop_PlasticGrey"
PBEIGE = "Prop_PlasticBeige"
PPUTTY = "Prop_PlasticPutty"
PWHITE = "Prop_PlasticWhite"
RUBBER = "Prop_Rubber"
GLASS = "Prop_Glass"
FROST = "Prop_FrostedGlass"
BULB = "Prop_BulbLit"
WALNUT = "Prop_WoodWalnut"
OAK = "Prop_WoodOak"
BRASS = "Prop_Brass"
NAVY = "Prop_FabricNavy"
PVC = "Prop_PVCEdge"
BACKER = "Prop_Backer"
HARDBOARD = "Prop_Hardboard"
PAPERW = "Prop_Paper"
CARD = "Prop_Cardboard"
PAINT = "Painted_Metal"

SIZE = 2048.0
CELLS = {
    "FILES_A": (0, 0, 2048, 620), "FILES_B": (0, 620, 2048, 620),
    "FILES_TOP_A": (0, 1240, 1024, 48), "FILES_TOP_B": (1024, 1240, 1024, 48),
    "BOARD_WRITTEN": (0, 1288, 1024, 512), "FLIP_WRITTEN": (1024, 1288, 384, 480),
    "CARTON_END": (1408, 1288, 320, 272), "CARTON_SIDE": (1728, 1288, 320, 220),
    "VCR_FACE": (1728, 1508, 320, 72), "COLOURS": (1408, 1580, 512, 128), "STRIP_FACE": (1920, 1580, 128, 40),
    "READER_PLATE": (1920, 1620, 128, 64),
    "FICHE_ENV": (1408, 1708, 136, 92), "HANG_TABS": (1544, 1708, 256, 56), "LABEL_CARD": (1544, 1764, 128, 36),
    "TENT_CARD": (1672, 1764, 128, 36),
    "BOARD_CHALK": (0, 1800, 512, 248), "BOARD_CLEAN": (512, 1800, 256, 128), "BOARD_TRAY": (512, 1928, 256, 32),
    "HANDOUT": (768, 1800, 192, 248), "BINDER_SPINE": (960, 1800, 64, 248), "FILE_SIDE": (1024, 1800, 160, 248),
    "OUTGUIDE_RED": (1184, 1800, 96, 248), "OUTGUIDE_BLUE": (1280, 1800, 96, 248), "TRANSPARENCY": (1376, 1800, 248, 248),
    "MANILA": (1624, 1800, 128, 248), "KRAFT": (1752, 1800, 128, 248), "PRESS_RED": (1880, 1800, 84, 248),
    "PRESS_GREEN": (1964, 1800, 84, 248),
}
COL = {n: i for i, n in enumerate(("manila", "kraft", "hang_green", "press_red", "press_blue", "vinyl_navy", "vinyl_teal",
                                   "paper", "marker_blue", "marker_black", "marker_red", "felt_grey", "chalk", "vcr_black",
                                   "vcr_silver", "pencil_yellow"))}
PCELLS = {"SCREEN": (0, 0, 768, 768), "STAGE": (768, 0, 256, 256), "FICHE": (768, 256, 256, 224)}
PSIZE = 1024.0


# ============================================================================ atlas rects
def rect(name, inset=1.5):
    x, y, w, h = CELLS[name]
    return ((x + inset) / SIZE, 1 - (y + h - inset) / SIZE, (x + w - inset) / SIZE, 1 - (y + inset) / SIZE)


def sub(name, fu0, fv0, fu1, fv1, inset=0.0):
    """A sub-rect of a cell in its own fractions (u from the left, v from the bottom)."""
    u0, v0, u1, v1 = rect(name, inset)
    return (u0 + (u1 - u0) * fu0, v0 + (v1 - v0) * fv0, u0 + (u1 - u0) * fu1, v0 + (v1 - v0) * fv1)


def prect(name, inset=1.5):
    x, y, w, h = PCELLS[name]
    return ((x + inset) / PSIZE, 1 - (y + h - inset) / PSIZE, (x + w - inset) / PSIZE, 1 - (y + inset) / PSIZE)


def colour(name):
    i = COL[name]
    x0, y0, w, h = CELLS["COLOURS"]
    x, y = x0 + (i % 8) * 64, y0 + (i // 8) * 64
    return ((x + 18) / SIZE, 1 - (y + 46) / SIZE, (x + 46) / SIZE, 1 - (y + 18) / SIZE)


def dbox(kit, size, centre, rc, lv, slot=PAPER, w=0.0005, facing="+z", name="plate", segs=None):
    return C.dbox(kit, size, centre, rc, lv, slot=slot, w=w, facing=facing, name=name, segs=segs)


def cbox(kit, size, centre, cname, lv, w=0.0006, name="part"):
    return C.dbox(kit, size, centre, colour(cname), lv, slot=PAPER, w=w, name=name)


box = C.box
flat = C.flat
uvoff = C.uvoff
face_quad = C.face_quad
quad_part = C.quad_part


def unit_quad(m, pts, uvs):
    """Add a quad (4 Unity points, CCW seen from outside) with explicit UVs."""
    idx = [m.vert(tuple(p)) for p in pts]
    m.face(tuple(idx), list(uvs))


# ============================================================================ paths
def rounded_path(pts, r, nseg):
    """A polyline through Unity points with each inner corner replaced by an arc of radius r (nseg segments)."""
    P = [Vector(p) for p in pts]
    out = [P[0]]
    for i in range(1, len(P) - 1):
        a, b, c = P[i - 1], P[i], P[i + 1]
        d0, d1 = (b - a).normalized(), (c - b).normalized()
        ang = d0.angle(d1) if d0.length > 0 and d1.length > 0 else 0.0
        if ang < 1e-3 or nseg <= 0:
            out.append(b)
            continue
        t = r * math.tan(ang / 2)
        t = min(t, (b - a).length * 0.48, (c - b).length * 0.48)
        p0, p1 = b - d0 * t, b + d1 * t
        for k in range(nseg + 1):
            s = k / nseg
            # quadratic Bezier through the corner: close to the arc for these angles
            q = p0 * (1 - s) ** 2 + b * 2 * s * (1 - s) + p1 * s * s
            out.append(q)
    out.append(P[-1])
    return [tuple(p) for p in out]


def tube_path(kit, name, pts, r, slot, lv, bend_r=0.03, segs=None, cap0=True, cap1=True):
    n = segs if segs is not None else (12, 8, 6)[lv]
    nb = (4, 2, 1)[lv]
    path = rounded_path(pts, bend_r, nb) if len(pts) > 2 else pts
    m = S.Mesh()
    S.sweep_section(m, path, S.circle(r, n), cap0=cap0, cap1=cap1)
    return m.part(kit, name, slot)


def rect_tube_path(kit, name, pts, wa, wb, slot, lv, bend_r=0.0, cap0=True, cap1=True, chamfer=0.0015):
    """A rectangular tube (section wa across the frame's normal, wb across its binormal) swept along a Unity path; the
    corners of the section chamfered at LOD0 so the edge catches a highlight."""
    path = rounded_path(pts, bend_r, (3, 1, 0)[lv]) if (bend_r > 0 and len(pts) > 2) else pts
    a, b = wa / 2, wb / 2
    c = chamfer if lv == 0 else 0.0
    if c > 0:
        sec = [(a, b - c), (a - c, b), (-a + c, b), (-a, b - c), (-a, -b + c), (-a + c, -b), (a - c, -b), (a, -b + c)]
    else:
        sec = [(a, b), (-a, b), (-a, -b), (a, -b)]
    m = S.Mesh()
    S.sweep_section(m, path, sec, cap0=cap0, cap1=cap1)
    return m.part(kit, name, slot, merge=False)


def oriented_box(kit, name, corners_fn, slot, uv_fn=None):
    """A box from 8 Unity corners (corners_fn returns them in the order x-y-z bits: 000, 100, 010, 110, 001, 101, 011, 111)
    with metre UVs per face (or uv_fn(face_key, quad) -> 4 UVs)."""
    v = corners_fn()
    m = S.Mesh()
    faces = {"-x": (0, 4, 6, 2), "+x": (1, 3, 7, 5), "-y": (0, 1, 5, 4), "+y": (2, 6, 7, 3), "-z": (0, 2, 3, 1), "+z": (4, 5, 7, 6)}
    for key, (a, b, c, d) in faces.items():
        q = [v[a], v[b], v[c], v[d]]
        if uv_fn is not None:
            uvs = uv_fn(key, q)
            if uvs is None:
                continue
        else:
            e1, e2 = (Vector(q[1]) - Vector(q[0])).length, (Vector(q[3]) - Vector(q[0])).length
            uvs = [(0, 0), (e1, 0), (e1, e2), (0, e2)]
        C._oriented(m, q, uvs, _face_normal(q, key))
    return m.part(kit, name, slot, merge=False)


def _face_normal(q, key):
    n = (Vector(q[1]) - Vector(q[0])).cross(Vector(q[3]) - Vector(q[0]))
    want = {"-x": (-1, 0, 0), "+x": (1, 0, 0), "-y": (0, -1, 0), "+y": (0, 1, 0), "-z": (0, 0, -1), "+z": (0, 0, 1)}[key]
    if n.length < 1e-12:
        return want
    return tuple(n.normalized()) if n.dot(Vector(want)) >= 0 else tuple(-n.normalized())


def rcyl(kit, axis, r, h, centre, slot, lv, n0=16, w=0.0008, name="cyl", r_top=None, bevel_lv1=False):
    """A cylinder on a Unity axis ('x', 'y', 'z') with explicit side counts per level (n0, n0 / 2, n0 / 4, never under 6):
    small round parts of the non-hero kits (casters, knobs, feet) where the hero chord rule would spend hundreds of
    triangles on what reads at 1 m. Bevelled (one segment) at LOD0 when w > 0."""
    n = (n0, max(6, (n0 // 2 + 1) // 2 * 2), max(6, (n0 // 4 + 1) // 2 * 2))[lv]
    if w > 0 and (lv == 0 or (lv == 1 and bevel_lv1)):
        bw, bs = w, 1
    else:
        bw, bs = 0.0, 0
    rot = {"y": (0, 0, 0), "x": (0, 90, 0), "z": (90, 0, 0)}[axis]
    return kit.cylinder(r, h, S.ub(centre), slot, verts=n, rot=rot, bevel=bw, segments=bs, name=name, radius_top=r_top)


def caster(kit, lv, x, z, wheel_d=0.102, brake=False, slot_fork=CHROME, name="caster"):
    """A swivel plate caster (top plate, swivel, fork, rubber wheel with its hub) standing on the floor at (x, z); returns
    its top height. Side counts are explicit (rcyl): 16-sided tyre at LOD0, a box at LOD2."""
    r = wheel_d / 2
    wt = wheel_d * 0.30
    h_top = wheel_d + 0.024
    zw = z - wheel_d * 0.18
    if lv == 2:
        flat_box(kit, (wt, wheel_d, wheel_d * 0.9), (x, r, zw), RUBBER, name=name + " wheel")
        flat_box(kit, (0.064, h_top - wheel_d, 0.064), (x, (wheel_d + h_top) / 2, z), slot_fork, name=name + " plate")
        return h_top + 0.002
    rcyl(kit, "x", r, wt, (x, r, zw), RUBBER, lv, n0=16, w=0.0025, name=name + " wheel")
    rcyl(kit, "x", r * 0.45, wt + 0.004, (x, r, zw), slot_fork, lv, n0=10, w=0.0, name=name + " hub")
    for s_ in (-1, 1):
        flat_box(kit, (0.003, r + 0.022, wheel_d * 0.42), (x + s_ * (wt / 2 + 0.003), r + (r + 0.022) / 2, z - wheel_d * 0.12),
                 slot_fork, name=name + " fork")
    flat_box(kit, (wt + 0.012, 0.004, wheel_d * 0.50), (x, h_top - 0.012, z - wheel_d * 0.10), slot_fork, name=name + " crown")
    rcyl(kit, "y", 0.016, 0.010, (x, h_top - 0.005, z), slot_fork, lv, n0=8, w=0.0, name=name + " swivel")
    if lv == 1:
        flat_box(kit, (0.064, 0.004, 0.064), (x, h_top, z), slot_fork, name=name + " plate")
    else:
        box(kit, (0.064, 0.004, 0.064), (x, h_top - 0.002 + 0.002, z), slot_fork, lv, w=0.0008, name=name + " plate", segs=1)
    if brake and lv == 0:
        box(kit, (0.026, 0.006, 0.040), (x, r + 0.018, z + wheel_d * 0.38), BLACK, lv, w=0.0015, name=name + " brake", segs=1)
    return h_top + 0.002


# ============================================================================ folder runs (kept in step with gen_structures_g3a2)
def lcg(seed):
    s = [int(seed) % 2147483648]

    def rnd():
        s[0] = (s[0] * 1103515245 + 12345) % 2147483648
        return s[0] / 2147483648.0
    return rnd


def folder_layout(seed, length_mm=800.0):
    """A deterministic run of letter end-tab folders along a strip: (x0, x1, kind) in mm from the strip's left edge as
    seen from the aisle. Kept in step with Tools/lookdev/structures/gen_structures_g3a2.folder_layout()."""
    rnd = lcg(seed)
    x, out = 0.0, []
    while x < length_mm - 0.5:
        r = rnd()
        if r < 0.10:
            t = 2.5 + 2.0 * rnd()
        elif r < 0.82:
            t = 6.0 + 9.0 * rnd()
        else:
            t = 15.0 + 13.0 * rnd()
        q = rnd()
        k = 2 if q < 0.05 else 3 if q < 0.08 else (1 if (seed % 2 == 0 and q < 0.30) else 0)
        x1 = min(length_mm, x + t)
        out.append((x, x1, k))
        x = x1
    return out


SEEDS = {"A": 4021, "B": 7716}
STRIP_MM = 800.0
STRIP_H_MM = 620.0 * 800.0 / 2048.0
FOLDER_H = 0.241
_LAYOUT = {}


def layout(strip):
    if strip not in _LAYOUT:
        _LAYOUT[strip] = folder_layout(SEEDS[strip], STRIP_MM)
    return _LAYOUT[strip]


def _strip_uv(strip, s_mm, top=False):
    name = ("FILES_TOP_" if top else "FILES_") + strip
    u0, v0, u1, v1 = rect(name, 0.0)
    return u0 + (u1 - u0) * (s_mm / STRIP_MM), v0, v1


def file_run(m, lv, xa, xb, y0, z_face, facing, depth, strip, seed, gappy=False, height=FOLDER_H, steel=None,
             dividers=True, pitch_div=0.22, jitter=1.0, group=(2, 8), sides=True):
    """One shelf's run of end-tab folders, x from xa to xb (Unity, xa < xb), folders standing on y0, their tab edges in
    the plane z_face facing +Z (facing 1) or -Z (facing -1), `depth` deep. Folders are grouped (a few to a group) into
    boxes cut at folder edges; each group's front maps its slice of the strip (so the drawn folders meet the geometry),
    its top the matching top strip, and an exposed side the folder side cell. Groups stand back from the face by a few mm
    at random (pushed in, pulled out); `gappy` leaves gaps the neighbours lean into and stands red / blue out-guides in.
    Dividers (sliding file supports) stand between groups at about pitch_div. LOD1: big groups, no sides or dividers;
    LOD2: one quad. Returns the steel divider parts' boxes to build (list of (x, y0, z)), built by the caller in steel."""
    rnd = lcg(seed * 7919 + 17)
    lay = layout(strip)
    nf = len(lay)
    run = (xb - xa) * 1000.0
    hmm = height * 1000.0
    vtop = height * 1000.0 / STRIP_H_MM

    def X(s):
        return xb - s / 1000.0 if facing > 0 else xa + s / 1000.0

    def Z(dz):
        return z_face - facing * dz / 1000.0

    zb = z_face - facing * depth
    divs = []
    if lv == 2:
        k = int(rnd() * nf)
        s0 = lay[k][0]
        s0 = min(s0, STRIP_MM - min(run, STRIP_MM))
        uL = _strip_uv(strip, s0)[0]
        uR, fv0, fv1 = _strip_uv(strip, s0 + min(run, STRIP_MM))
        v1 = fv0 + (fv1 - fv0) * vtop
        p = [(X(0), y0, Z(4)), (X(run), y0, Z(4)), (X(run), y0 + height, Z(4)), (X(0), y0 + height, Z(4))]
        C._oriented(m, p, [(uL, fv0), (uR, fv0), (uR, v1), (uL, v1)], (0, 0, facing))
        return divs
    k = int(rnd() * nf)
    pos = 0.0
    prev_dz = None
    next_div = pitch_div * 1000.0 * (0.4 + 0.6 * rnd())
    gmin, gmax = group if lv == 0 else (8, 24)
    pending_lean = 0.0
    while pos < run - 0.8:
        # a divider plate, then on
        if dividers and lv == 0 and pos >= next_div:
            divs.append((X(pos + 1.0), y0, z_face))
            pos += 2.2
            next_div = pos + pitch_div * 1000.0 * (0.75 + 0.5 * rnd())
            prev_dz = None
            continue
        if gappy and rnd() < (0.07 if lv == 0 else 0.05) and pos > 30 and run - pos > 90:
            # an out-guide (vinyl, standing taller and proud) or a gap the next group leans into
            if rnd() < 0.55:
                red = rnd() < 0.6
                cell = "OUTGUIDE_RED" if red else "OUTGUIDE_BLUE"
                ccol = colour("press_red" if red else "press_blue")
                x0, x1 = X(pos), X(pos + 3.0)
                ytop = y0 + height + 0.026
                zf, zbk = Z(-10.0), zb + facing * 0.012
                us0, vs0, us1, vs1 = rect(cell)
                for xs, out in ((x0, 1), (x1, -1)):
                    p = [(xs, y0, zf), (xs, y0, zbk), (xs, ytop, zbk), (xs, ytop, zf)]
                    C._oriented(m, p, [(us0, vs0), (us1, vs0), (us1, vs1), (us0, vs1)], (out * facing, 0, 0))
                cu0, cv0, cu1, cv1 = ccol
                C._oriented(m, [(x0, y0, zf), (x1, y0, zf), (x1, ytop, zf), (x0, ytop, zf)],
                            [(cu0, cv0), (cu1, cv0), (cu1, cv1), (cu0, cv1)], (0, 0, facing))
                C._oriented(m, [(x0, ytop, zf), (x1, ytop, zf), (x1, ytop, zbk), (x0, ytop, zbk)],
                            [(cu0, cv0), (cu1, cv0), (cu1, cv1), (cu0, cv1)], (0, 1, 0))
                pos += 3.0
                prev_dz = None
                continue
            gap = 45.0 + 35.0 * rnd()
            pos += gap
            pending_lean = math.radians(7.0 + 7.0 * rnd())
            prev_dz = None
            continue
        n = gmin + int(rnd() * (gmax - gmin + 1))
        if k >= nf:
            k = 0
        s0 = lay[k][0]
        k1 = min(nf, k + n)
        s1 = lay[k1 - 1][1]
        w = s1 - s0
        if pos + w > run:
            w = run - pos
            s1 = s0 + w
        if dividers and lv == 0 and pos + w > next_div and w > 6.0:
            # cut the group at the divider (keep folder edges where possible)
            cut = next_div - pos
            kk = k
            while kk < k1 and lay[kk][1] - s0 < cut:
                kk += 1
            if kk > k:
                k1 = kk
                s1 = lay[k1 - 1][1]
                w = s1 - s0
        k = k1
        r = rnd()
        dz = (r * 9.0 if r < 0.82 else 12.0 + 20.0 * rnd()) * jitter
        lean = pending_lean
        pending_lean = 0.0
        x0, x1 = X(pos), X(pos + w)
        y1 = y0 + height
        zf = Z(dz)
        uL = _strip_uv(strip, s0)[0]
        uR, fv0, fv1 = _strip_uv(strip, s1)
        v1 = fv0 + (fv1 - fv0) * vtop
        tu0 = _strip_uv(strip, s0, top=True)[0]
        tu1, tv0, tv1 = _strip_uv(strip, s1, top=True)
        corners = {"bl": (x0, y0), "br": (x1, y0), "tr": (x1, y1), "tl": (x0, y1)}
        if lean > 0:
            # lean toward the gap on the viewer's left (the group's left edge falls into it): pivot at its bottom-left
            px = x0
            ca, sa = math.cos(lean), math.sin(lean)
            sgn = 1.0 if facing > 0 else -1.0     # viewer's left is +x for facing +Z

            def rot(p):
                dx, dy = p[0] - px, p[1] - y0
                return (px + (dx * ca + sgn * dy * sa), y0 + (-sgn * dx * sa + dy * ca))
            corners = {kk2: rot(v) for kk2, v in corners.items()}
        bl, br, tr, tl = corners["bl"], corners["br"], corners["tr"], corners["tl"]
        C._oriented(m, [(bl[0], bl[1], zf), (br[0], br[1], zf), (tr[0], tr[1], zf), (tl[0], tl[1], zf)],
                    [(uL, fv0), (uR, fv0), (uR, v1), (uL, v1)], (0, 0, facing))
        C._oriented(m, [(tl[0], tl[1], zf), (tr[0], tr[1], zf), (tr[0], tr[1], zb), (tl[0], tl[1], zb)],
                    [(tu0, tv0), (tu1, tv0), (tu1, tv1), (tu0, tv1)], (0, 1, 0))
        if lv == 0 and sides:
            su0, sv0, su1, sv1 = rect("FILE_SIDE")
            sv1x = sv0 + (sv1 - sv0) * (hmm / 241.0)
            # the left side when it is exposed (a gap, a lean, or the neighbour stands further back)
            if prev_dz is None or prev_dz > dz + 2.5 or lean > 0:
                zback = Z(prev_dz) if (prev_dz is not None and lean == 0) else zb
                du = abs(zback - zf) / 0.311
                C._oriented(m, [(bl[0], bl[1], zf), (bl[0], bl[1], zback), (tl[0], tl[1], zback), (tl[0], tl[1], zf)],
                            [(su0, sv0), (su0 + (su1 - su0) * min(1, du), sv0), (su0 + (su1 - su0) * min(1, du), sv1x), (su0, sv1x)],
                            (facing, 0, 0))
            prev_right = dz
            # the right side: drawn by the next group's test in reverse, so emit it when this group stands forward of
            # what follows; we do not know that yet, so emit when we stand forward of the mean (cheap, mostly right)
            if dz < 4.0:
                du = 0.025 / 0.311
                C._oriented(m, [(br[0], br[1], zf), (br[0], br[1], zf - facing * 0.025), (tr[0], tr[1], zf - facing * 0.025), (tr[0], tr[1], zf)],
                            [(su0, sv0), (su0 + (su1 - su0) * du, sv0), (su0 + (su1 - su0) * du, sv1x), (su0, sv1x)],
                            (-facing, 0, 0))
        prev_dz = dz
        pos += w
    return divs


def divider(kit, lv, x, y0, z_face, facing, depth, slot=STEEL):
    """A sliding file support: a 1.6 mm steel plate standing on the shelf, set back from the folders, its front top corner
    rounded (one bevelled box at LOD0; dropped at LOD1)."""
    if lv > 0:
        return
    h = 0.205
    zc = z_face - facing * (0.016 + (depth - 0.03) / 2)
    o = box(kit, (0.0016, h, depth - 0.03), (x, y0 + h / 2, zc), slot, lv, w=0.0006, name="file support", segs=1)
    return o


# ============================================================================ placement helpers (local kit frames)
def col(kit, lv, centre, size):
    """A Unity box collider, recorded once (on the LOD0 pass)."""
    if lv == 0:
        C.collider_unity(kit, centre, size)


def meta(kit, service=None):
    kit.meta["structures"] = "structures_assets G3-a2 2026-11-10 (30_plan.md P16 structures_l0_props, the SP3 training room and file mezzanine)"
    if service is not None:
        kit.meta["service"] = service


def yawp(p, base=(0.0, 0.0, 0.0), yaw=0.0):
    """A Unity point p (local to a sub-assembly) turned by `yaw` degrees about Unity Y (+Z toward +X) and moved to base."""
    a = math.radians(yaw)
    ca, sa = math.cos(a), math.sin(a)
    return (base[0] + p[0] * ca + p[2] * sa, base[1] + p[1], base[2] - p[0] * sa + p[2] * ca)


def ybox(kit, size, centre, slot, lv, base=(0.0, 0.0, 0.0), yaw=0.0, w=0.0012, name="box", segs=None):
    """A box of a sub-assembly placed at base and turned by yaw (degrees, about Unity Y)."""
    c = yawp(centre, base, yaw)
    if abs(yaw) < 1e-6:
        return box(kit, size, c, slot, lv, w=w, name=name, segs=segs)
    bw, bs = S.bev(w, lv)
    if segs is not None and lv == 0:
        bs = segs
    return kit.box((size[0], size[2], size[1]), S.ub(c), slot, bevel=bw, segments=bs, rot=(0, 0, -yaw), name=name)


def flat_box(kit, size, centre, slot, name="flat"):
    return C.flat(kit, size, centre, slot, name=name)


def colour_quads(m, quads, cname):
    """Quads (each 4 Unity points, CCW from outside, with an outward normal) filled with one colour cell of the atlas."""
    u0, v0, u1, v1 = colour(cname)
    for pts, out in quads:
        C._oriented(m, pts, [(u0, v0), (u1, v0), (u1, v1), (u0, v1)], out)


def rect_quad(m, pts, rc, out):
    """A quad (bl, br, tr, tl as seen from outside) mapped onto atlas rect rc."""
    u0, v0, u1, v1 = rc
    C._oriented(m, pts, [(u0, v0), (u1, v0), (u1, v1), (u0, v1)], out)


# ============================================================================ FILE MEZZANINE (S3-12; 62 2.9, LS59, LS93, LS94, LS98)
def lipped_shelf(kit, lv, x0, x1, y_top, z0, z1, slot=STEEL, lip=0.025, front=True, back=False, name="shelf", cheap=False):
    """A formed sheet-steel shelf: the deck (top at y_top) and its edges turned down `lip` front (and back). The deck stops
    behind each lip (no coplanar faces); the lip's bevel reads the fold at hero range."""
    w, xc = x1 - x0, (x0 + x1) / 2
    za = z0 + (0.0024 if back else 0.0)
    zb = z1 - (0.0024 if front else 0.0)
    box(kit, (w, 0.0024, zb - za), (xc, y_top - 0.0012, (za + zb) / 2), slot, lv, w=0.0008, name=name, segs=1)
    for on, z in ((front, z1 - 0.0012), (back, z0 + 0.0012)):
        if not on or (cheap and lv == 2):
            continue
        if cheap and lv == 1:
            flat_box(kit, (w, lip, 0.0024), (xc, y_top - lip / 2, z), slot, name=name + " lip")
        else:
            box(kit, (w, lip, 0.0024), (xc, y_top - lip / 2, z), slot, lv, w=0.0010, name=name + " lip",
                segs=(1 if cheap else 2) if lv == 0 else None)


def osf_tiers(H, base, cap, n=7):
    pitch = (H - base - cap) / n
    return [base + k * pitch for k in range(n)], pitch


def _folders(kit, lv, runs, divs_cheap=False):
    """Build every folder run in one part (Prop_PaperAtlas) and their sliding file supports (steel)."""
    m = S.Mesh()
    divs = []
    for (xa, xb, y0, z_face, facing, depth, strip, seed, gappy, kw) in runs:
        d = file_run(m, lv, xa, xb, y0, z_face, facing, depth, strip, seed, gappy=gappy, **kw)
        divs += [(dv, facing, depth) for dv in d]
    if m.f:
        m.part(kit, "folders", PAPER)
    for (x, y0, zf), facing, depth in divs:
        if divs_cheap:
            if lv == 0:
                h = 0.205
                flat_box(kit, (0.0016, h, depth - 0.03), (x, y0 + h / 2, zf - facing * (0.016 + (depth - 0.03) / 2)), STEEL, name="file support")
        else:
            divider(kit, lv, x, y0, zf, facing, depth)


OSF_W, OSF_D, OSF_H, OSF_BASE, OSF_CAP = 0.914, 0.305, 2.134, 0.076, 0.025


def open_shelf_file(kit, lv, double=False, gappy=False, seed=0):
    """Kit_OpenShelfFile (HERO at 0.35 m): a 36 x 12 x 84 in open-format end-tab file of seven tiers (R6; 62 2.9), closed
    steel ends with their front flanges, lipped shelves, back panel (or the centre panel of the double-faced unit), top cap
    and a recessed 3 in base; letter end-tab folders (9.5 in) stand on every tier, their colour-coded tab edges mapped from
    the 2.56 px/mm strips, grouped and staggered at folder edges, with sliding file supports between the groups."""
    W, H = OSF_W, OSF_H
    D = OSF_D * (2 if double else 1)
    z0, z1 = -D / 2, D / 2
    te = 0.025
    xi0, xi1 = -W / 2 + te, W / 2 - te
    for sgn in (-1, 1):
        box(kit, (te, H - OSF_CAP, D), (sgn * (W / 2 - te / 2), (H - OSF_CAP) / 2, 0.0), STEEL, lv, w=0.0016, name="closed end")
    box(kit, (W, OSF_CAP, D), (0.0, H - OSF_CAP / 2, 0.0), STEEL, lv, w=0.0018, name="top cap")
    kp = OSF_BASE - 0.003
    box(kit, (xi1 - xi0, kp, 0.0024), (0.0, kp / 2, z1 - 0.010), STEEL, lv, w=0.0008, name="kick plate", segs=1)
    if double:
        box(kit, (xi1 - xi0, kp, 0.0024), (0.0, kp / 2, z0 + 0.010), STEEL, lv, w=0.0008, name="kick plate", segs=1)
        box(kit, (xi1 - xi0, H - OSF_CAP - OSF_BASE, 0.0024), (0.0, (H - OSF_CAP + OSF_BASE) / 2, 0.0), STEEL, lv, w=0.0008,
            name="centre panel", segs=1)
        zs0 = z0
    else:
        box(kit, (xi1 - xi0, H - OSF_CAP, 0.0024), (0.0, (H - OSF_CAP) / 2, z0 + 0.0012), STEEL, lv, w=0.0008, name="back panel", segs=1)
        zs0 = z0 + 0.0024
    ys, pitch = osf_tiers(H, OSF_BASE, OSF_CAP + 0.004)
    runs = []
    for k, y in enumerate(ys):
        lipped_shelf(kit, lv, xi0, xi1, y, zs0, z1, front=True, back=double)
        faces = [(1, z1)] + ([(-1, z0)] if double else [])
        for fi, (facing, zedge) in enumerate(faces):
            strip = "A" if (k + fi + seed) % 2 == 0 else "B"
            sd = 101 + k * 37 + fi * 211 + seed * 997
            depth = OSF_D - 0.024
            runs.append((xi0 + 0.002, xi1 - 0.002, y, zedge - facing * 0.006, facing, depth, strip, sd, gappy, {}))
    _folders(kit, lv, runs)
    col(kit, lv, (0.0, H / 2, 0.0), (W, H, D))
    lv == 0 and kit.support("top", S.ub((0.0, H, 0.0)), (W, D))


MS_BAY, MS_D, MS_H = 0.914, 0.610, 2.134
MS_CAR0, MS_CAR1 = 0.035, 0.105


def hand_wheel(kit, lv, centre, r=0.165, axis=1):
    """The mechanical-assist hand wheel on the aisle end (R1: a wheel in a housing drives the chain and sprocket to the
    long shaft): a cast hub in its housing, three spokes and a round rim of 7/8 in tube, a spinner knob on the rim."""
    cx, cy, cz = centre
    rcyl(kit, "x", 0.070, 0.022, (cx + axis * 0.011, cy, cz), SBLACK, lv, n0=24, w=0.0020, name="wheel housing")
    if lv == 2:
        return
    rcyl(kit, "x", 0.040, 0.050, (cx + axis * 0.040, cy, cz), CHROME, lv, n0=16, w=0.0020, name="wheel hub")
    xr = cx + axis * 0.060
    n = (24, 12)[lv]
    ring = [(xr, cy + r * math.cos(2 * math.pi * k / n), cz + r * math.sin(2 * math.pi * k / n)) for k in range(n + 1)]
    m = S.Mesh()
    S.sweep_section(m, ring, S.circle(0.0111, (10, 6)[lv]))
    m.part(kit, "wheel rim", CHROME)
    for k in range(3):
        a = math.radians(90 + 120 * k)
        p0 = (cx + axis * 0.055, cy + 0.030 * math.cos(a), cz + 0.030 * math.sin(a))
        p1 = (xr, cy + (r - 0.004) * math.cos(a), cz + (r - 0.004) * math.sin(a))
        tube_path(kit, "wheel spoke", [p0, p1], 0.0095, CHROME, lv, segs=(10, 6)[lv])
    if lv < 2:
        a = math.radians(30)
        kc = (xr + axis * 0.040, cy + r * math.cos(a), cz + r * math.sin(a))
        rcyl(kit, "x", 0.015, 0.075, kc, BLACK, lv, n0=12, w=0.003, name="spinner knob")


def mobile_shelving(kit, lv, bays=5, cartons=False, seed=0):
    """Kit_MobileShelving_<n> (LS59, R1 US 4,984,737): a double-faced carriage of n 36 in bays of end-tab file shelving on a
    steel carriage (rides the floor rails; the 35 mm gap under it shows them), uprights between the bays, a plain steel end
    and the aisle end: a wood-grain laminate end panel in satin aluminium trim with the three-spoke hand wheel. The kit's
    +X end is the aisle end; both long faces (+Z, -Z) are file faces."""
    D, H = MS_D, MS_H
    te, tl = 0.022, 0.034
    L = bays * MS_BAY
    xs0 = -(L + te + tl) / 2 + te          # shelving from xs0 to xs1, plain end left of xs0, laminate end right of xs1
    xs1 = xs0 + L
    z0, z1 = -D / 2, D / 2
    box(kit, (L + te + tl - 0.02, MS_CAR1 - MS_CAR0, D - 0.040), ((xs0 - te + xs1 + tl) / 2, (MS_CAR0 + MS_CAR1) / 2, 0.0), STEEL,
        lv, w=0.0015, name="carriage", segs=1)
    base_top = 0.130
    for zz in (z1 - 0.012, z0 + 0.012):
        box(kit, (L, base_top - MS_CAR1 - 0.002, 0.0024), ((xs0 + xs1) / 2, (MS_CAR1 + base_top - 0.002) / 2, zz), STEEL, lv,
            w=0.0008, name="toe kick", segs=1)
    box(kit, (te, H - MS_CAR1 - OSF_CAP, D), (xs0 - te / 2, (H - OSF_CAP + MS_CAR1) / 2, 0.0), STEEL, lv, w=0.0016, name="plain end")
    box(kit, (L + te + tl, OSF_CAP, D), ((xs0 - te + xs1 + tl) / 2, H - OSF_CAP / 2, 0.0), STEEL, lv, w=0.0018, name="top cap")
    # the aisle end: steel end, laminate panel, aluminium trim on its edges, the hand wheel
    box(kit, (0.016, H - MS_CAR1 - OSF_CAP, D), (xs1 + 0.008, (H - OSF_CAP + MS_CAR1) / 2, 0.0), STEEL, lv, w=0.0010, name="aisle end", segs=1)
    lam_h = H - MS_CAR0 - 0.012
    box(kit, (0.013, lam_h, D - 0.020), (xs1 + 0.016 + 0.0065, MS_CAR0 + 0.006 + lam_h / 2, 0.0), "Prop_WoodLaminate", lv, w=0.0012,
        name="end panel laminate")
    for s in (-1, 1):
        box(kit, (0.018, lam_h + 0.004, 0.012), (xs1 + 0.016 + 0.009, MS_CAR0 + 0.006 + lam_h / 2, s * (D / 2 - 0.006)), ALU, lv,
            w=0.0012, name="end trim")
    box(kit, (0.018, 0.012, D), (xs1 + 0.016 + 0.009, MS_CAR0 + 0.006 + lam_h + 0.004, 0.0), ALU, lv, w=0.0012, name="end trim top")
    hand_wheel(kit, lv, (xs1 + 0.029, 1.05, 0.0), axis=1)
    ys, pitch = osf_tiers(H, base_top, OSF_CAP + 0.004)
    for b in range(1, bays):
        xb = xs0 + b * MS_BAY
        box(kit, (0.020, H - base_top - OSF_CAP, D - 0.006), (xb, (H - OSF_CAP + base_top) / 2, 0.0), STEEL, lv, w=0.0010,
            name="upright", segs=1)
    runs, crt = [], []
    for k, y in enumerate(ys):
        lipped_shelf(kit, lv, xs0, xs1, y, z0, z1, front=True, back=True, cheap=True)
        for b in range(bays):
            xa, xb = xs0 + b * MS_BAY + (0.012 if b > 0 else 0.002), xs0 + (b + 1) * MS_BAY - (0.012 if b < bays - 1 else 0.002)
            for fi, (facing, zedge) in enumerate(((1, z1), (-1, z0))):
                if cartons and k < 3 and (b + fi + k) % 2 == 0:
                    crt.append((xa, xb, y, zedge, facing))
                    continue
                strip = "A" if (k + b + fi + seed) % 2 == 0 else "B"
                sd = 301 + k * 41 + b * 113 + fi * 577 + seed * 991
                runs.append((xa, xb, y, zedge - facing * 0.006, facing, D / 2 - 0.022, strip, sd, False,
                             {"group": (8, 20), "dividers": False, "sides": False}))
    _folders(kit, lv, runs, divs_cheap=True)
    if crt:
        mc = S.Mesh()
        for (xa, xb, y, zedge, facing) in crt:
            n = 2
            cw = (xb - xa - 0.02) / n
            for j in range(n):
                rnd = lcg(int(abs(xa) * 1000) + j * 7 + int(y * 100))
                xc = xa + 0.01 + cw * (j + 0.5) + (rnd() - 0.5) * 0.01
                zc = zedge - facing * (0.010 + 0.150 + rnd() * 0.012)
                bl = min(0.381, cw - 0.012)
                fz = "Z" if facing > 0 else "z"
                if lv < 2:
                    box_quads(mc, (xc, y + CT_H / 2, zc), (bl, CT_H, 0.300), {"x": rect("CARTON_END"), "y": rect("KRAFT"),
                              "z": rect("CARTON_SIDE")}, faces=fz + "xX")
                    box_quads(mc, (xc, y + CT_H + CT_T - CT_LID / 2, zc), (bl + 0.008, CT_LID, 0.308), {"x": rect("KRAFT"),
                              "y": rect("KRAFT"), "z": rect("KRAFT")}, faces=fz + "xXY")
                else:
                    box_quads(mc, (xc, y + (CT_H + CT_T) / 2, zc), (bl, CT_H + CT_T, 0.300), {"x": rect("CARTON_END"),
                              "y": rect("KRAFT"), "z": rect("CARTON_SIDE")}, faces="Z" if facing > 0 else "z")
        mc.part(kit, "cartons", PAPER)
    col(kit, lv, ((xs0 - te + xs1 + tl) / 2, H / 2, 0.0), (L + te + tl, H, D))
    lv == 0 and kit.anchor("aisleEnd", S.ub((xs1 + tl, 0.0, 0.0)))


def mobile_rail(kit, lv, length=3.0):
    """Kit_MobileShelving_Rail: one 3 m surface-mounted floor rail of the mobile system (R1: carriages ride floor rail sets):
    an aluminium ramp strip 100 mm wide and 22 mm high carrying the 20 mm steel rail head (30 mm overall), ramped down at
    both ends. Runs along Unity Z, origin the floor centre; no collider (walkable)."""
    ramp = 0.060
    body = length - 2 * ramp
    hw, tw, hh, rh = 0.050, 0.016, 0.022, 0.030
    o = kit.extrude([(-hw, 0.0), (hw, 0.0), (tw, hh), (-tw, hh)], body, S.ub((0, 0, 0)), ALU, plane="xz",
                    bevel=0.0010 if lv < 2 else 0.0, segments=1 if lv < 2 else 0, name="rail ramp strip")
    box(kit, (0.020, rh - hh + 0.002, body - 0.004), (0.0, hh + (rh - hh) / 2 - 0.001, 0.0), "Prop_StainlessSatin", lv,
        w=0.0012, name="rail head", segs=1)
    for s in (-1, 1):
        zc = s * (body / 2)
        ze = s * (body / 2 + ramp)

        def corners(zc=zc, ze=ze):
            pts = []
            for bits in range(8):
                xb, yb, zb = bits & 1, (bits >> 1) & 1, (bits >> 2) & 1
                x = (-hw if xb == 0 else hw)
                z = zc if zb == (0 if s > 0 else 1) else ze
                atend = (z == ze)
                if yb == 1:
                    y = 0.002 if atend else hh
                    x = x * (0.6 if atend else tw / hw)
                else:
                    y = 0.0
                pts.append((x, y, z))
            return pts
        oriented_box(kit, "rail ramp end", corners, ALU)


def hang_row(m, lv, a0, a1, b0, b1, y_top, n, seed, stack="z", face_sign=1, tabs=True, manila=True, h=0.236):
    """A row of n green hanging folders hung by their top bars at y_top, stacked along `stack` ('z' or 'x') from a0 to a1,
    each folder's face spanning b0..b1 across: the top bar (2 mm), the face toward face_sign along the stack, a clear tab on
    every second folder at one of five staggered places (both sides), a manila folder standing proud in every third."""
    rnd = lcg(seed)
    step = (a1 - a0) / n
    tu0, tv0, tu1, tv1 = rect("HANG_TABS")

    def P(a, y, b):
        return (b, y, a) if stack == "z" else (a, y, b)
    out_face = (0, 0, face_sign) if stack == "z" else (face_sign, 0, 0)
    up = (0, 1, 0)
    for i in range(n):
        a = a0 + step * (i + 0.5) + (rnd() - 0.5) * step * 0.4
        lean = (rnd() - 0.5) * 0.006
        yt = y_top + (rnd() - 0.5) * 0.003
        yb = yt - h
        bb0, bb1 = b0 + 0.004, b1 - 0.004
        colour_quads(m, [([P(a - face_sign * lean * 0.2, yb, bb0), P(a - face_sign * lean * 0.2, yb, bb1), P(a + lean, yt, bb1),
                            P(a + lean, yt, bb0)], out_face)], "hang_green")
        if lv < 2:
            colour_quads(m, [([P(a + lean - 0.001, yt, bb0), P(a + lean - 0.001, yt, bb1), P(a + lean + 0.001, yt, bb1),
                               P(a + lean + 0.001, yt, bb0)], up)], "hang_green")
        if lv == 0 and tabs and i % 2 == 0:
            slot = (i // 2) % 5
            tw = 0.052
            bc = bb0 + 0.03 + (bb1 - bb0 - 0.06 - tw) * slot / 4.0
            k = (i // 2) % 4
            ru0 = tu0 + (tu1 - tu0) * (0.02 + 0.245 * k)
            ru1 = ru0 + (tu1 - tu0) * 0.22
            for sgn in (1, -1):
                aa = a + lean + sgn * 0.0004
                pts = [P(aa, yt, bc), P(aa, yt, bc + tw), P(aa, yt + 0.024, bc + tw), P(aa, yt + 0.024, bc)]
                if sgn < 0:
                    pts = [pts[1], pts[0], pts[3], pts[2]]
                    uv = [(ru1, tv0), (ru0, tv0), (ru0, tv1), (ru1, tv1)]
                else:
                    uv = [(ru0, tv0), (ru1, tv0), (ru1, tv1), (ru0, tv1)]
                o = (0, 0, sgn * face_sign) if stack == "z" else (sgn * face_sign, 0, 0)
                C._oriented(m, pts, uv, o)
        if lv == 0 and manila and i % 3 == 1:
            yy = yt + 0.008 + 0.012 * rnd()
            ma = a + lean - face_sign * 0.003
            colour_quads(m, [([P(ma, yt - 0.03, bb0 + 0.02), P(ma, yt - 0.03, bb1 - 0.05), P(ma, yy, bb1 - 0.05), P(ma, yy, bb0 + 0.02)],
                              out_face)], "manila")
            colour_quads(m, [([P(ma - 0.0008, yy, bb0 + 0.02), P(ma - 0.0008, yy, bb1 - 0.05), P(ma + 0.0008, yy, bb1 - 0.05),
                               P(ma + 0.0008, yy, bb0 + 0.02)], up)], "manila")


LF_W, LF_D = 0.914, 0.457
LF_H = {2: 0.724, 4: 1.334, 5: 1.670}


def _drawer_front(kit, lv, wd, y0, y1, zf, lock=False):
    """One lateral drawer front (R4): the 20 mm steel front, the full-width recessed pull at its top (the satin aluminium
    lip over a dark channel, steel end caps), the chrome label holder with a blank card, the lock on the top drawer."""
    yp = y1 - 0.034
    box(kit, (wd, yp - y0, 0.020), (0.0, (y0 + yp) / 2, zf - 0.010), STEEL, lv, w=0.0018, name="drawer front")
    if lv == 2:
        flat_box(kit, (wd, y1 - yp, 0.014), (0.0, (yp + y1) / 2, zf - 0.013), SATIN, name="pull lip")
        return
    if lv == 1:
        flat_box(kit, (wd, 0.026, 0.012), (0.0, yp + 0.013, zf - 0.016), SBLACK, name="pull channel")
        flat_box(kit, (wd, 0.008, 0.020), (0.0, y1 - 0.004, zf - 0.010), SATIN, name="pull lip")
        return
    box(kit, (wd - 0.030, 0.026, 0.012), (0.0, yp + 0.013, zf - 0.016), SBLACK, lv, w=0.0008, name="pull channel", segs=1)
    for s in (-1, 1):
        flat_box(kit, (0.015, 0.026, 0.020), (s * (wd / 2 - 0.0075), yp + 0.013, zf - 0.010), STEEL, name="pull cap")
    box(kit, (wd, 0.008, 0.020), (0.0, y1 - 0.004, zf - 0.010), SATIN, lv, w=0.0014, name="pull lip", segs=1)
    yl = yp - 0.034
    kit.frame((0.082, 0.036), (0.072, 0.026), 0.003, S.ub((0.0, yl, zf + 0.0015)), CHROME, bevel=0.0, segments=0, name="label holder")
    m = S.Mesh()
    C.face_quad(m, (0.0, yl, zf + 0.0008), "+z", 0.072, 0.026, rect("LABEL_CARD"))
    m.part(kit, "label card", PAPER)
    if lock:
        rcyl(kit, "z", 0.0105, 0.008, (-(wd / 2 - 0.070), yp - 0.030, zf + 0.004), CHROME, lv, n0=16, w=0.0012, name="lock")
        flat_box(kit, (0.0018, 0.008, 0.002), (-(wd / 2 - 0.070), yp - 0.030, zf + 0.0085), SBLACK, name="keyway")


def lateral_file(kit, lv, drawers=4, open_k=None):
    """Kit_LateralFile_<n> (R4: a 1988 four-drawer lateral, 36 x 18 x 52.5 in, putty): the case (sides, back, a dark liner
    behind the drawer gaps), an overhanging top, the recessed plinth, n drawer fronts with full-width recessed aluminium
    pulls and label holders, the lock. open_k: that drawer stands out 0.38 m on its suspension (drawer box, rails, two
    rows of green hanging folders front to back with clear tabs, manila folders in them)."""
    W, D, H = LF_W, LF_D, LF_H[drawers]
    plinth, top_t, side_t = 0.064, 0.022, 0.016
    zf = D / 2 - 0.004
    body_h = H - top_t - plinth
    for s in (-1, 1):
        box(kit, (side_t, body_h, D - 0.032), (s * (W / 2 - side_t / 2), plinth + body_h / 2, -0.016), STEEL, lv, w=0.0012,
            name="case side", segs=1)
    box(kit, (W - 2 * side_t, body_h, 0.0024), (0.0, plinth + body_h / 2, -D / 2 + 0.0012), STEEL, lv, w=0.0008, name="case back", segs=1)
    flat_box(kit, (W - 2 * side_t, body_h, 0.002), (0.0, plinth + body_h / 2, zf - 0.023), SBLACK, name="gap liner")
    box(kit, (W + 0.004, top_t, D + 0.004), (0.0, H - top_t / 2, 0.002), STEEL, lv, w=0.0030, name="top")
    box(kit, (W - 0.060, plinth, D - 0.070), (0.0, plinth / 2, -0.006), STEEL, lv, w=0.0012, name="plinth", segs=1)
    dh = body_h / drawers
    for k in range(drawers):
        y0, y1 = plinth + k * dh + 0.0015, plinth + (k + 1) * dh - 0.0015
        dz = 0.38 if k == open_k else 0.0
        _drawer_front(kit, lv, W - 0.008, y0, y1, zf + dz, lock=(k == drawers - 1))
    col(kit, lv, (0.0, H / 2, 0.0), (W, H, D))
    if open_k is not None:
        k = open_k
        y0 = plinth + k * dh
        zfront = zf + 0.38 - 0.020
        dd = 0.400
        iw = W - 2 * side_t - 0.016
        hb = dh - 0.030
        yb = y0 + 0.012
        for s in (-1, 1):
            box(kit, (0.0024, hb, dd), (s * (iw / 2 - 0.0012), yb + hb / 2, zfront - dd / 2), STEEL, lv, w=0.0008, name="drawer side", segs=1)
            if lv < 2:
                box(kit, (0.010, 0.024, dd + 0.10), (s * (iw / 2 + 0.006), yb + hb * 0.55, zfront - dd / 2 - 0.05), "Prop_StainlessSatin",
                    lv, w=0.0008, name="suspension", segs=1)
        box(kit, (iw, 0.0024, dd), (0.0, yb + 0.0012, zfront - dd / 2), STEEL, lv, w=0.0008, name="drawer bottom", segs=1)
        box(kit, (iw, hb, 0.0024), (0.0, yb + hb / 2, zfront - dd + 0.0012), STEEL, lv, w=0.0008, name="drawer back", segs=1)
        y_rail = yb + hb - 0.010
        for xr in (-(iw / 2 - 0.006), -0.007, 0.007, iw / 2 - 0.006):
            box(kit, (0.006, 0.012, dd - 0.006), (xr, y_rail - 0.006, zfront - dd / 2), SATIN, lv, w=0.0006, name="file rail", segs=1)
        m = S.Mesh()
        for r, (b0, b1) in enumerate(((-(iw / 2 - 0.010), -0.011), (0.011, iw / 2 - 0.010))):
            hang_row(m, lv, zfront - dd + 0.012, zfront - 0.020, b0, b1, y_rail + 0.002, (24, 14, 6)[lv], 77 + r * 31, stack="z",
                     face_sign=1)
        m.part(kit, "hanging folders", PAPER)
        col(kit, lv, (0.0, y0 + dh / 2, D / 2 + 0.19), (W - 0.03, dh, 0.38))


def file_cart(kit, lv, full=True):
    """Kit_FileCart (LS98): a 1980s putty steel hanging-file cart, 32 x 16 x 36 in: the lipped bottom tray on four 3 in
    casters (two with brakes), four corner posts, a sheet-steel basket whose rolled top edges are the hanging rails, the
    push handle with its black grip at the -X end; full: green hanging folders across the rails with tabs and manila."""
    L, Wd = 0.810, 0.410
    y_top, y_bb, y_handle = 0.800, 0.540, 0.910
    xc, zc = L / 2 - 0.055, Wd / 2 - 0.050
    h_top = 0.0
    for (x, z, br) in ((xc, zc, True), (xc, -zc, False), (-xc, zc, True), (-xc, -zc, False)):
        h_top = caster(kit, lv, x, z, wheel_d=0.076, brake=br, slot_fork=CHROME)
    y_tray = h_top + 0.025
    lipped_shelf(kit, lv, -L / 2, L / 2, y_tray, -Wd / 2, Wd / 2, front=True, back=True, name="bottom tray")
    for s in (-1, 1):
        box(kit, (0.0024, 0.025, Wd - 0.005), (s * (L / 2 - 0.0012), y_tray - 0.0125, 0.0), STEEL, lv, w=0.0009, name="tray end lip", segs=1)
    px, pz = L / 2 - 0.0115, Wd / 2 - 0.0115
    for sx in (-1, 1):
        for sz in (-1, 1):
            box(kit, (0.019, y_top - y_tray, 0.019), (sx * px, (y_top + y_tray) / 2, sz * pz), STEEL, lv, w=0.0018, name="corner post", segs=1)
    for s in (-1, 1):
        box(kit, (L - 0.040, y_top - y_bb, 0.0024), (0.0, (y_top + y_bb) / 2, s * (Wd / 2 - 0.012)), STEEL, lv, w=0.0008, name="basket side", segs=1)
        box(kit, (0.0024, y_top - y_bb, Wd - 0.040), (s * (L / 2 - 0.012), (y_top + y_bb) / 2, 0.0), STEEL, lv, w=0.0008, name="basket end", segs=1)
        box(kit, (L - 0.020, 0.011, 0.011), (0.0, y_top - 0.0055, s * (Wd / 2 - 0.012)), STEEL, lv, w=0.0035, name="hanging rail",
            segs=1 if lv == 0 else None)
    box(kit, (L - 0.040, 0.0024, Wd - 0.040), (0.0, y_bb + 0.0012, 0.0), STEEL, lv, w=0.0008, name="basket bottom", segs=1)
    hx = -px - 0.030
    tube_path(kit, "push handle", [(-px, y_top - 0.03, pz), (hx, y_handle, pz), (hx, y_handle, -pz), (-px, y_top - 0.03, -pz)],
              0.0111, STEEL, lv, bend_r=0.035, segs=(12, 8, 6)[lv])
    if lv < 2:
        rcyl(kit, "z", 0.0150, 0.250, (hx, y_handle, 0.0), BLACK, lv, n0=12, w=0.0030, name="grip")
    if full:
        m = S.Mesh()
        hang_row(m, lv, -L / 2 + 0.035, L / 2 - 0.035, -(Wd / 2 - 0.016), Wd / 2 - 0.016, y_top + 0.004, (26, 14, 6)[lv], 2024,
                 stack="x", face_sign=-1)
        m.part(kit, "hanging folders", PAPER)
    col(kit, lv, (0.0, y_handle / 2, 0.0), (L + 0.06, y_handle, Wd))


# ---------------------------------------------------------------- records cartons (LS94: the NARA cubic-foot records box)
CT_W, CT_H, CT_L = 0.305, 0.254, 0.381     # 12 x 10 x 15 in: X the end's width, Z the length (front +Z = the printed end)
CT_LID, CT_T = 0.057, 0.004


def uvm(rc, u, v):
    u0, v0, u1, v1 = rc
    return (u0 + (u1 - u0) * u, v0 + (v1 - v0) * v)


def wall_grid(m, P, nu, nv, rc, out):
    """A wall as nu x nv quads; P(s, t) -> Unity point at fractions s (to the viewer's right) and t (up) of the wall."""
    for i in range(nu):
        for j in range(nv):
            s0, s1, t0, t1 = i / nu, (i + 1) / nu, j / nv, (j + 1) / nv
            C._oriented(m, [P(s0, t0), P(s1, t0), P(s1, t1), P(s0, t1)],
                        [uvm(rc, s0, t0), uvm(rc, s1, t0), uvm(rc, s1, t1), uvm(rc, s0, t1)], out)


def wall_handhold(m, lv, P, w, h, rc, out, hole, inward, up):
    """A carton end w x h (P(u, v) -> Unity point, u to the viewer's right from the left edge, v up) mapped onto rc, with
    the die-cut stadium handhold hole = (cu, cv, hw, hh): the board round it, the cut edge (4 mm board), the flap pushed in
    25 degrees about its top edge (printed face and kraft back), and the files seen in the box behind it."""
    cu, cv, hw, hh = hole
    a0, a1, b0, b1 = cu - hw, cu + hw, cv - hh, cv + hh
    r = hh

    def Q(u, v):
        return P(u, v), uvm(rc, u / w, v / h)

    def rq(ua, va, ub, vb):
        (p0, t0), (p1, t1), (p2, t2), (p3, t3) = Q(ua, va), Q(ub, va), Q(ub, vb), Q(ua, vb)
        C._oriented(m, [p0, p1, p2, p3], [t0, t1, t2, t3], out)
    rq(0, 0, a0, h)
    rq(a1, 0, w, h)
    rq(a0, 0, a1, b0)
    rq(a0, b1, a1, h)
    n = (8, 4, 2)[lv]
    outline = []      # the stadium, counter-clockwise from the top-left of the straight edge
    for k in range(n + 1):                                   # left end 90 -> 270
        a = math.radians(90 + 180 * k / n)
        outline.append((a0 + r + r * math.cos(a), cv + r * math.sin(a)))
    for k in range(n + 1):                                   # right end -90 -> 90
        a = math.radians(-90 + 180 * k / n)
        outline.append((a1 - r + r * math.cos(a), cv + r * math.sin(a)))
    for k in range(n):                                       # corner fans
        for (ua, va), (ub, vb), corner in ((outline[k], outline[k + 1], (a0, b1) if k < n / 2 else (a0, b0)),
                                           (outline[n + 1 + k], outline[n + 2 + k], (a1, b0) if k < n / 2 else (a1, b1))):
            (p0, t0), (p1, t1), (p2, t2) = Q(*corner), Q(ua, va), Q(ub, vb)
            C._oriented(m, [p0, p1, p2], [t0, t1, t2], out)
    iv, uvv = Vector(inward), Vector(up)
    ring = outline + [outline[0]]
    ku0, kv0, ku1, kv1 = colour("kraft")
    for k in range(len(ring) - 1):
        pa, pb = Vector(P(*ring[k])), Vector(P(*ring[k + 1]))
        mid = ((ring[k][0] + ring[k + 1][0]) / 2 - cu, (ring[k][1] + ring[k + 1][1]) / 2 - cv)
        C._oriented(m, [tuple(pa), tuple(pb), tuple(pb + iv * CT_T), tuple(pa + iv * CT_T)],
                    [(ku0, kv0), (ku1, kv0), (ku1, kv1), (ku0, kv1)], tuple(-(Vector(P(cu + mid[0], cv + mid[1])) - Vector(P(cu, cv)))))
    if lv < 2:
        ang = math.radians(25.0)

        def F(u, v, push=0.0):
            base = Vector(P(u, b1))
            return tuple(base - uvv * (b1 - v) * math.cos(ang) + iv * ((b1 - v) * math.sin(ang) + push))
        cen = (cu, cv)
        for k in range(len(ring) - 1):
            (ua, va), (ub, vb) = ring[k], ring[k + 1]
            for push, o_sign, uvs in ((0.0005, 1, [uvm(rc, cu / w, cv / h), uvm(rc, ua / w, va / h), uvm(rc, ub / w, vb / h)]),
                                      (CT_T, -1, [(ku0, kv0), (ku1, kv0), (ku1, kv1)])):
                p0, p1, p2 = F(*cen, push), F(ua, va, push), F(ub, vb, push)
                nrm = (Vector(p1) - Vector(p0)).cross(Vector(p2) - Vector(p0))
                want = (Vector(out) * math.cos(ang) - uvv * math.sin(ang)) * o_sign
                C._oriented(m, [p0, p1, p2], uvs, tuple(want))
        mu0, mv0, mu1, mv1 = rect("MANILA")
        d = 0.035
        pts = [tuple(Vector(P(a0 - 0.01, b0 - 0.02)) + iv * d), tuple(Vector(P(a1 + 0.01, b0 - 0.02)) + iv * d),
               tuple(Vector(P(a1 + 0.01, b1 + 0.01)) + iv * d), tuple(Vector(P(a0 - 0.01, b1 + 0.01)) + iv * d)]
        C._oriented(m, pts, [(mu0, mv0), (mu1, mv0), (mu1, mv1), (mu0, mv1)], out)


def carton(m, lv, base=(0.0, 0.0, 0.0), yaw=0.0, lid=True, bulge=0.0, sag=0.0, inside=False, size=(CT_W, CT_H, CT_L),
           lid_pose=None):
    """One records carton into Mesh m (Prop_PaperAtlas): the body (printed ends with die-cut handholds, the long sides,
    the bottom), the lid with its 57 mm skirt (top sagging by `sag` when crushed), `bulge` bowing the walls of a loaded
    box under a stack; inside = lid off: the inner faces, the board's top edges and the folders filed in it."""
    W, H, L = size

    def D(p):
        x, y, z = p
        if bulge:
            fy = math.sin(math.pi * min(1.0, max(0.0, y / H)))
            if abs(abs(x) - W / 2) < 0.002:
                x += math.copysign(bulge * fy * max(0.0, 1 - (2 * z / L) ** 2), x)
            if abs(abs(z) - L / 2) < 0.002:
                z += math.copysign(bulge * 0.7 * fy * max(0.0, 1 - (2 * x / W) ** 2), z)
        return yawp((x, y, z), base, yaw)

    def nrm(v):
        return tuple(Vector(yawp(v, (0, 0, 0), yaw)))
    grid = (4, 3) if (bulge and lv == 0) else (1, 1)
    if lv == 2:
        top = H + (CT_T if lid else 0.0)
        for s in (-1, 1):
            wall_grid(m, lambda u, v, s=s: D((s * (W / 2 - u * W), v * top, s * L / 2)), 1, 1, rect("CARTON_END"), nrm((0, 0, s)))
            wall_grid(m, lambda u, v, s=s: D((s * W / 2, v * top, s * (u - 0.5) * L)), 1, 1, rect("CARTON_SIDE"), nrm((s, 0, 0)))
        wall_grid(m, lambda u, v: D(((0.5 - u) * W, top, (0.5 - v) * L)), 1, 1, rect("KRAFT"), nrm((0, 1, 0)))
        return
    for s in (-1, 1):
        # ends: +Z seen from +Z (right = -X), -Z seen from -Z (right = +X)
        P = (lambda u, v, s=s: D((s * (W / 2 - u), v, s * L / 2)))
        if lv == 0:
            wall_handhold(m, lv, P, W, H, rect("CARTON_END"), nrm((0, 0, s)), (W / 2, H - 0.085, 0.052, 0.016),
                          nrm((0, 0, -s)), (0, 1, 0))
        else:
            wall_grid(m, lambda u, v, s=s: D((s * (W / 2 - u * W), v * H, s * L / 2)), 1, 1, rect("CARTON_END"), nrm((0, 0, s)))
        wall_grid(m, lambda u, v, s=s: D((s * W / 2, v * H, s * (u - 0.5) * L)), grid[0], grid[1], rect("CARTON_SIDE"), nrm((s, 0, 0)))
    wall_grid(m, lambda u, v: D(((u - 0.5) * W, 0.0, (0.5 - v) * L)), 1, 1, rect("KRAFT"), nrm((0, -1, 0)))
    if inside:
        kr = rect("KRAFT")
        t = CT_T
        for s in (-1, 1):
            wall_grid(m, lambda u, v, s=s: D((s * (W / 2 - t), v * H, -s * (u - 0.5) * (L - 2 * t))), 1, 1, kr, nrm((-s, 0, 0)))
            wall_grid(m, lambda u, v, s=s: D((s * (u - 0.5) * (W - 2 * t), v * H, s * (L / 2 - t))), 1, 1, kr, nrm((0, 0, -s)))
            wall_grid(m, lambda u, v, s=s: D((s * (W / 2 - t * v), H, s * (u - 0.5) * L)), 1, 1, kr, nrm((0, 1, 0)))
            wall_grid(m, lambda u, v, s=s: D((s * (u - 0.5) * (W - 2 * t), H, s * (L / 2 - t * v))), 1, 1, kr, nrm((0, 1, 0)))
        tu0, tv0, tu1, tv1 = rect("FILES_TOP_A")
        yf = H - 0.010
        pts = [D((W / 2 - t - 0.002, yf, -L / 2 + t + 0.004)), D((W / 2 - t - 0.002, yf, L / 2 - t - 0.030)),
               D((-W / 2 + t + 0.002, yf, L / 2 - t - 0.030)), D((-W / 2 + t + 0.002, yf, -L / 2 + t + 0.004))]
        su = 0.37 * (L - 0.04) / 0.8
        C._oriented(m, pts, [(tu0, tv1), (tu0 + (tu1 - tu0) * su, tv1), (tu0 + (tu1 - tu0) * su, tv0), (tu0, tv0)], nrm((0, 1, 0)))
        fu0, fv0, fu1, fv1 = rect("MANILA")
        zf = L / 2 - t - 0.030
        C._oriented(m, [D((W / 2 - t - 0.002, 0.02, zf)), D((-W / 2 + t + 0.002, 0.02, zf)), D((-W / 2 + t + 0.002, yf, zf)),
                        D((W / 2 - t - 0.002, yf, zf))], [(fu0, fv0), (fu1, fv0), (fu1, fv1), (fu0, fv1)], nrm((0, 0, 1)))
    if not lid:
        return
    lw, ll, top = W + 0.008, L + 0.008, H + CT_T
    lb = top - CT_LID
    if lid_pose is not None:
        lbase, lyaw = lid_pose

        def LD(p):
            # the lid set down upside down beside the box: its top on the floor, the skirt's open side up
            return yawp((-p[0], top - p[1], p[2]), lbase, lyaw)
    else:
        def LD(p):
            x, y, z = p
            if sag and abs(y - top) < 1e-6:
                y -= sag * max(0.0, 1 - (2 * x / lw) ** 2) * max(0.0, 1 - (2 * z / ll) ** 2)
            return yawp((x, y, z), base, yaw)
    def LN(v):
        if lid_pose is not None:
            return tuple(Vector(yawp((-v[0], -v[1], v[2]), (0, 0, 0), lid_pose[1])))
        return nrm(v)
    kr = rect("KRAFT")
    tg = (3, 3) if (sag and lv == 0) else (1, 1)
    wall_grid(m, lambda u, v: LD(((0.5 - u) * lw, top, (0.5 - v) * ll)), tg[0], tg[1], kr, LN((0, 1, 0)))
    for s in (-1, 1):
        wall_grid(m, lambda u, v, s=s: LD((s * (lw / 2 - u * lw), lb + v * CT_LID, s * ll / 2)), 1, 1, kr, LN((0, 0, s)))
        wall_grid(m, lambda u, v, s=s: LD((s * lw / 2, lb + v * CT_LID, s * (u - 0.5) * ll)), 1, 1, kr, LN((s, 0, 0)))
        if lv == 0 or lid_pose is not None:
            wall_grid(m, lambda u, v, s=s: LD((s * (lw / 2 - u * lw), lb, s * (ll / 2 - CT_T * v))), 1, 1, kr, LN((0, -1, 0)))
            wall_grid(m, lambda u, v, s=s: LD((s * (lw / 2 - CT_T * v), lb, s * (u - 0.5) * ll)), 1, 1, kr, LN((0, -1, 0)))
        if lid_pose is not None:
            it = CT_T
            wall_grid(m, lambda u, v, s=s: LD((s * (lw / 2 - it), lb + v * (CT_LID - it), -s * (u - 0.5) * (ll - 2 * it))), 1, 1, kr,
                      LN((-s, 0, 0)))
            wall_grid(m, lambda u, v, s=s: LD((s * (u - 0.5) * (lw - 2 * it), lb + v * (CT_LID - it), s * (ll / 2 - it))), 1, 1, kr,
                      LN((0, 0, -s)))
    if lid_pose is not None:
        wall_grid(m, lambda u, v: LD(((u - 0.5) * (lw - 2 * CT_T), top - CT_T, (0.5 - v) * (ll - 2 * CT_T))), 1, 1, kr, LN((0, -1, 0)))


def box_quads(m, centre, size, rcs, yaw=0.0, base=(0.0, 0.0, 0.0), faces="xyzXYZ"):
    """An axis box (Unity centre / size, turned by yaw about base) as six quads, each mapped onto an atlas rect:
    rcs = {"x": rc (both X faces), "y": rc (top and bottom), "z": rc (both Z faces)}; faces lists which to emit
    (lower case = negative side, upper case = positive)."""
    cx, cy, cz = centre
    hx, hy, hz = size[0] / 2, size[1] / 2, size[2] / 2

    def P(x, y, z):
        return yawp((cx + x, cy + y, cz + z), base, yaw)

    def N(v):
        return tuple(Vector(yawp(v, (0, 0, 0), yaw)))
    spec = {
        "Z": ([P(hx, -hy, hz), P(-hx, -hy, hz), P(-hx, hy, hz), P(hx, hy, hz)], (0, 0, 1), "z"),
        "z": ([P(-hx, -hy, -hz), P(hx, -hy, -hz), P(hx, hy, -hz), P(-hx, hy, -hz)], (0, 0, -1), "z"),
        "X": ([P(hx, -hy, -hz), P(hx, -hy, hz), P(hx, hy, hz), P(hx, hy, -hz)], (1, 0, 0), "x"),
        "x": ([P(-hx, -hy, hz), P(-hx, -hy, -hz), P(-hx, hy, -hz), P(-hx, hy, hz)], (-1, 0, 0), "x"),
        "Y": ([P(hx, hy, hz), P(-hx, hy, hz), P(-hx, hy, -hz), P(hx, hy, -hz)], (0, 1, 0), "y"),
        "y": ([P(hx, -hy, -hz), P(-hx, -hy, -hz), P(-hx, -hy, hz), P(hx, -hy, hz)], (0, -1, 0), "y"),
    }
    for key in faces:
        pts, o, ax = spec[key]
        rect_quad(m, pts, rcs[ax], N(o))


def plain_box(m, lv, base=(0.0, 0.0, 0.0), yaw=0.0, s=0.457):
    """A plain regular slotted carton (18 in cube moving box): kraft walls, the top's two outer flaps meeting on a 3 mm seam
    with the dark box inside, the seam taped with tan packing tape running down both ends."""
    kr = rect("KRAFT")
    h = s / 2
    box_quads(m, (0.0, h, 0.0), (s, s, s), {"x": kr, "y": kr, "z": kr}, yaw, base, faces="xXzZy")
    g = 0.0015 if lv < 2 else 0.0
    for sg in (-1, 1):
        box_quads(m, (sg * (s / 4 + g / 2), s - 0.0005, 0.0), (s / 2 - g, 0.001, s), {"x": kr, "y": kr, "z": kr}, yaw, base,
                  faces="Y")
    if lv < 2:
        colour_box = colour("vcr_black")
        box_quads(m, (0.0, s - 0.006, 0.0), (0.004, 0.001, s - 0.004), {"x": colour_box, "y": colour_box, "z": colour_box},
                  yaw, base, faces="Y")
        tape = colour("manila")
        tw = 0.048
        box_quads(m, (0.0, s + 0.0004, 0.0), (tw, 0.0008, s + 0.0016), {"x": tape, "y": tape, "z": tape}, yaw, base, faces="Y")
        for sg in (-1, 1):
            box_quads(m, (0.0, s - 0.035, sg * (s / 2 + 0.0004)), (tw, 0.07, 0.0008), {"x": tape, "y": tape, "z": tape}, yaw, base,
                      faces="Z" if sg > 0 else "z")


def records_carton(kit, lv, mode="single"):
    """The carton kits (LS94): single / stack3 / stack5 / lidoff / sagging / plain / bundle (one Prop_PaperAtlas part)."""
    m = S.Mesh()
    hb = CT_H + CT_T
    cols = []
    if mode == "single":
        carton(m, lv)
    elif mode == "stack3":
        for k, (dx, dz, yw) in enumerate(((0.0, 0.0, 0.0), (0.006, -0.004, 1.6), (-0.003, 0.007, -2.2))):
            carton(m, lv, base=(dx, k * hb, dz), yaw=yw, lid=True)
        cols.append(((0.0, 1.5 * hb, 0.0), (CT_W + 0.02, 3 * hb, CT_L + 0.02)))
    elif mode == "stack5":
        for ci, (x0, n) in enumerate(((-0.162, 3), (0.162, 2))):
            for k in range(n):
                rnd = lcg(31 + ci * 7 + k)
                carton(m, lv, base=(x0 + (rnd() - 0.5) * 0.012, k * hb, (rnd() - 0.5) * 0.014), yaw=(rnd() - 0.5) * 4.0, lid=True)
            cols.append(((x0, n * hb / 2, 0.0), (CT_W + 0.02, n * hb, CT_L + 0.02)))
    elif mode == "lidoff":
        lw = CT_W + 0.008
        carton(m, lv, inside=True, lid=True, lid_pose=((CT_W / 2 + 0.045 + lw / 2, 0.0, 0.035), 7.0))
    elif mode == "sagging":
        lean = 0.0
        for k, (bul, sg, yw) in enumerate(((0.012, 0.0, 0.0), (0.008, 0.006, 1.2), (0.004, 0.004, -0.8), (0.0, 0.003, 2.0))):
            lean += (0.000 if k == 0 else 0.006)
            carton(m, lv, base=(lean, k * hb - 0.004 * k, 0.002 * k), yaw=yw, lid=True, bulge=bul, sag=sg)
        cols.append(((0.012, 2 * hb, 0.004), (CT_W + 0.05, 4 * hb, CT_L + 0.03)))
    elif mode == "plain":
        plain_box(m, lv)
    elif mode == "bundle":
        kr = rect("KRAFT")
        for k in range(8):
            rnd = lcg(900 + k)
            box_quads(m, ((rnd() - 0.5) * 0.016, 0.005 + 0.0095 * k, (rnd() - 0.5) * 0.020), (0.60, 0.0090, 0.90),
                      {"x": rect("CARTON_SIDE"), "y": kr, "z": kr}, (rnd() - 0.5) * 1.5, (0.0, 0.0, 0.0),
                      faces="xXzZY" if k < 7 else "xXzZYy")
        strap = colour("vcr_black")
        for zz in (-0.26, 0.26):
            box_quads(m, (0.0, 0.0425, zz), (0.618, 0.087, 0.013), {"x": strap, "y": strap, "z": strap}, faces="xXY")
    m.part(kit, "cartons", PAPER)
    if mode in ("single", "lidoff", "plain", "bundle"):
        lv == 0 and kit.no_collider()
    else:
        for c, s in cols:
            col(kit, lv, c, s)


# ---------------------------------------------------------------- the microfiche reader (R2: Micron 775 class)
FR_W, FR_D, FR_H = 0.305, 0.457, 0.521


def fiche_reader(kit, lv, lit=False, carousel=False):
    """Kit_MicroficheReader: a standard-design rear-projection reader (R2: 12 w x 20.5 h x 18 l in, 11.5 x 9.75 in blue-tinted
    screen, the fiche tray below the screen, a low / high / off switch and a knob on the front): the beige base housing on
    rubber feet, the upper housing whose front leans back 10 degrees with the screen in its black bezel under a hood, the
    fiche carrier (two glass flats on a plate on chrome runners) standing out at the front, the front plate, the focus knob,
    side vents, the cord. lit: the page on the screen (Prop_ProjectionLit; dark = Prop_ProjectionLit_Off)."""
    W, D, H = FR_W, FR_D, FR_H
    yb = 0.112
    zback, zbf = -D / 2 + 0.010, 0.140
    box(kit, (W, yb - 0.008, zbf - zback), (0.0, 0.008 + (yb - 0.008) / 2, (zbf + zback) / 2), PBEIGE, lv, w=0.0060, name="base housing")
    for x in (-W / 2 + 0.035, W / 2 - 0.035):
        for z in (zback + 0.035, zbf - 0.035):
            if lv < 2:
                rcyl(kit, "y", 0.012, 0.009, (x, 0.0045, z), RUBBER, lv, n0=8, w=0.0, name="foot")
    zfb, zft, ytop = 0.120, 0.048, H

    def corners():
        pts = []
        for bits in range(8):
            xb, ybit, zbit = bits & 1, (bits >> 1) & 1, (bits >> 2) & 1
            x = -W / 2 if xb == 0 else W / 2
            y = yb - 0.002 if ybit == 0 else ytop
            z = zback + 0.008 if zbit == 0 else (zfb if ybit == 0 else zft)
            pts.append((x, y, z))
        return pts
    up_body = oriented_box(kit, "upper housing", corners, PBEIGE)
    if lv < 2:
        kit._bevel(up_body, 0.006 if lv == 0 else 0.004, 2 if lv == 0 else 1, angle=30.0)
    fy, fz = ytop - yb, zft - zfb
    flen = math.hypot(fy, fz)
    uvec = Vector((0.0, fy / flen, fz / flen))
    nvec = Vector((0.0, -fz / flen, fy / flen))
    tilt = math.degrees(math.atan2(-fz, fy))

    def F(s, t, off=0.0):
        """A point on the leaning front face: s across (Unity x), t up the face from its bottom edge, off out of it."""
        return tuple(Vector((s, yb, zfb)) + uvec * t + nvec * off)
    sw, sh = 0.286, 0.244
    tc = flen - 0.030 - 0.020 - sh / 2
    bez = 0.008
    for (bw_, bh_, dx, dt) in ((sw + 2 * bez, bez, 0.0, sh / 2 + bez / 2), (sw + 2 * bez, bez, 0.0, -sh / 2 - bez / 2),
                               (bez, sh, sw / 2 + bez / 2, 0.0), (bez, sh, -sw / 2 - bez / 2, 0.0)):
        bwb, bsb = S.bev(0.0010, lv)
        kit.box((bw_, 0.010, bh_), S.ub(F(dx, tc + dt, 0.003)), BLACK, bevel=bwb, segments=min(bsb, 1), rot=(-tilt, 0, 0), name="screen bezel")
    m = S.Mesh()
    rc = prect("FICHE")
    C._oriented(m, [F(sw / 2, tc - sh / 2, 0.001), F(-sw / 2, tc - sh / 2, 0.001), F(-sw / 2, tc + sh / 2, 0.001),
                    F(sw / 2, tc + sh / 2, 0.001)], [(rc[0], rc[1]), (rc[2], rc[1]), (rc[2], rc[3]), (rc[0], rc[3])], tuple(nvec))
    m.part(kit, "screen", PLIT if lit else PLIT_OFF)
    th = flen - 0.004
    S.box(kit, (W + 0.006, 0.008, 0.070), F(0.0, th, 0.035), PBEIGE, lv, w=0.0025, name="hood top", rot=(-tilt, 0, 0))
    if lv < 2:
        for sx in (-1, 1):
            def cheek(sx=sx):
                pts = []
                for bits in range(8):
                    xb, ybit, zbit = bits & 1, (bits >> 1) & 1, (bits >> 2) & 1
                    x = sx * (W / 2 + 0.003) + (0.006 if xb else 0.0) * -sx
                    t = th - 0.004 if ybit else tc - sh / 2 - 0.004
                    o = (0.066 if ybit else 0.004) if zbit else 0.0
                    pts.append(F(x, t, o))
                return pts
            ob = oriented_box(kit, "hood cheek", cheek, PBEIGE)
            kit._bevel(ob, 0.0012, 1, angle=30.0)
    # the fiche carrier on its runners
    yc = yb + 0.004
    box(kit, (0.250, 0.010, 0.170), (0.0, yc + 0.005, zbf + 0.050), PGREY, lv, w=0.0015, name="carrier plate")
    for x in (-0.085, 0.085):
        if lv < 2:
            rcyl(kit, "z", 0.0045, 0.20, (x, yc - 0.002, zbf + 0.020), CHROME, lv, n0=8, w=0.0, name="runner")
    box(kit, (0.200, 0.004, 0.130), (0.0, yc + 0.012, zbf + 0.048), GLASS, lv, w=0.0006, name="glass flat", segs=1)
    box(kit, (0.200, 0.004, 0.130), (0.0, yc + 0.0165, zbf + 0.048), GLASS, lv, w=0.0006, name="glass flat", segs=1)
    if lv < 2:
        box(kit, (0.205, 0.006, 0.010), (0.0, yc + 0.018, zbf + 0.112), SBLACK, lv, w=0.0010, name="flat frame", segs=1)
        rcyl(kit, "z", 0.008, 0.026, (0.0, yc + 0.006, zbf + 0.148), BLACK, lv, n0=12, w=0.0020, name="carrier knob")
        dbox(kit, (0.120, 0.060, 0.002), (0.075, 0.060, zbf + 0.001), rect("READER_PLATE"), lv, w=0.0003, name="front plate", segs=1)
        box(kit, (0.026, 0.020, 0.008), (0.104, 0.060, zbf + 0.006), BLACK, lv, w=0.0010, name="rocker", segs=1)
        rcyl(kit, "z", 0.016, 0.020, (-0.090, 0.062, zbf + 0.010), BLACK, lv, n0=16, w=0.0025, name="focus knob")
        rcyl(kit, "z", 0.021, 0.004, (-0.090, 0.062, zbf + 0.002), BLACK, lv, n0=16, w=0.0, name="knob skirt")
        for sx in (-1, 1):
            for k in range(6):
                flat_box(kit, (0.002, 0.008, 0.120), (sx * (W / 2 + 0.0005), 0.30 + 0.022 * k, -0.07), SBLACK, name="vent slot")
        tube_path(kit, "cord", [(0.090, 0.045, zback + 0.010), (0.090, 0.030, zback - 0.040), (0.120, 0.005, zback - 0.200),
                                (0.160, 0.004, zback - 0.330)], 0.0035, BLACK, lv, bend_r=0.04, segs=(6, 6, 6)[lv])
    if carousel:
        fiche_carousel(kit, lv, (-0.350, 0.0, 0.010))
    lv == 0 and kit.no_collider()


def fiche_carousel(kit, lv, at):
    """A rotary fiche file beside the reader: a black turntable on a base, a hub, and fiche envelopes standing in a ring."""
    cx, cy, cz = at
    S.lathe_y(kit, [(0.172, 0.0), (0.176, 0.008), (0.172, 0.018), (0.040, 0.022), (0.0, 0.022)], (cx, cy, cz), BLACK, lv, name="turntable")
    S.cyl_y(kit, 0.028, 0.120, (cx, cy + 0.080, cz), BLACK, lv, w=0.0020, name="hub", lo=16)
    m = S.Mesh()
    n = (48, 24, 10)[lv]
    ru0, rv0, ru1, rv1 = rect("FICHE_ENV")
    for k in range(n):
        a = 2 * math.pi * k / n
        ca, sa = math.cos(a), math.sin(a)
        r0, r1 = 0.030, 0.168
        y0, y1 = cy + 0.024, cy + 0.024 + 0.100

        def P(r, y, dt=0.0):
            return (cx + ca * r - sa * dt, y, cz + sa * r + ca * dt)
        for side in (1, -1):
            dt = side * 0.0006
            pts = [P(r0, y0, dt), P(r1, y0, dt), P(r1, y1, dt), P(r0, y1, dt)]
            C._oriented(m, pts, [(ru0, rv0), (ru1, rv0), (ru1, rv1), (ru0, rv1)], (-sa * side, 0.0, ca * side))
    m.part(kit, "fiche envelopes", PAPER)


# ============================================================================ TRAINING ROOM (S3-09; 62 2.4, LS57, LS91, LS97-LS99)
TT_L, TT_D, TT_H, TT_T = 1.524, 0.457, 0.737, 0.0286      # 60 x 18 x 29 in, a 1-1/8 in top (LS97 / LS98)
NAVYF = "Prop_FabricNavy"


def rr_outline(w, d, r, n):
    """A rounded rectangle (w along X, d along Z) as (x, z) points counter-clockwise from above, starting mid +Z edge."""
    pts = []
    pts.append((0.0, d / 2))
    for cx, cz, a0 in ((w / 2 - r, d / 2 - r, 90.0), (w / 2 - r, -d / 2 + r, 0.0), (-w / 2 + r, -d / 2 + r, 270.0), (-w / 2 + r, d / 2 - r, 180.0)):
        for k in range(n + 1):
            a = math.radians(a0 - 90.0 * k / n)
            pts.append((cx + r * math.cos(a), cz + r * math.sin(a)))
    pts.append((0.0, d / 2))
    return pts


def training_table(kit, lv, x0=0.0, z0=0.0, modesty=False, top=FAWN):
    """One 60 x 18 in training table of 1988 (LS97 / LS98): the 1-1/8 in top in matte plastic laminate with rounded corners,
    the vinyl T-mould edge round it, the kraft backer under it, two black steel T-legs (a 2 x 1 in post on an 18 in foot
    runner with end caps and levelling glides, a mounting rail under the top) and the stretcher between the legs near the
    back; modesty: a black steel modesty panel hung on the presenter's side (-Z)."""
    L, D, H, T = TT_L, TT_D, TT_H, TT_T
    r = 0.025
    n = (4, 2, 1)[lv]
    out = rr_outline(L, D, r, n)
    ol = [(-(x0 + x), -(z0 + z)) for (x, z) in out[1:-1]]          # Blender (x, y) = (-Unity x, -Unity z)
    kit.extrude(ol, T - 0.0006, S.ub((0.0, H - T / 2, 0.0)), top, plane="xy", bevel=0.0, segments=0, name="table top")
    if lv < 2:
        po = rr_outline(L + 0.0030, D + 0.0030, r + 0.0015, n)
        path = [(x0 + x, H - T / 2, z0 + z) for (x, z) in po]
        a, b = T / 2, 0.0015
        c = 0.0018 if lv == 0 else 0.0
        sec = ([(-a, -b + 0.0004), (-a + c, -b), (a - c, -b), (a, -b + 0.0004), (a, b - 0.0004), (a - c, b), (-a + c, b), (-a, b - 0.0004)]
               if lv == 0 else [(-a, -b), (a, -b), (a, b), (-a, b)])
        m = S.Mesh()
        S.sweep_section(m, path, sec)
        m.part(kit, "t-mould", PVC)
        flat_box(kit, (L - 0.012, 0.0008, D - 0.012), (x0, H - T - 0.0004, z0), BACKER, name="backer")
    lx = TT_L / 2 - 0.110
    for s in (-1, 1):
        x = x0 + s * lx
        yb = H - T - 0.025
        box(kit, (0.0254, yb - 0.040, 0.0508), (x, 0.040 + (yb - 0.040) / 2, z0), SBLACK, lv, w=0.0016, name="leg post", segs=1)
        box(kit, (0.0254, 0.032, 0.420), (x, 0.008 + 0.016, z0), SBLACK, lv, w=0.0018, name="foot runner", segs=1)
        box(kit, (0.0254, 0.025, 0.380), (x, H - T - 0.0125, z0), SBLACK, lv, w=0.0012, name="top rail", segs=1)
        if lv < 2:
            for zz in (-1, 1):
                flat_box(kit, (0.0262, 0.0330, 0.004), (x, 0.024, z0 + zz * 0.212), BLACK, name="foot cap")
                rcyl(kit, "y", 0.011, 0.008, (x, 0.004, z0 + zz * 0.180), BLACK, lv, n0=12, w=0.0, name="glide")
    box(kit, (2 * lx - 0.0254, 0.0508, 0.0254), (x0, H - T - 0.050, z0 - 0.120), SBLACK, lv, w=0.0014, name="stretcher", segs=1)
    if modesty:
        box(kit, (2 * lx - 0.040, 0.300, 0.0016), (x0, H - T - 0.020 - 0.150, z0 - D / 2 + 0.030), SBLACK, lv, w=0.0007,
            name="modesty panel", segs=1)
        if lv < 2:
            for s in (-1, 0, 1):
                flat_box(kit, (0.030, 0.020, 0.025), (x0 + s * (lx - 0.12), H - T - 0.010, z0 - D / 2 + 0.042), SBLACK, name="panel bracket")


def table_row(kit, lv, modesty=False, top=FAWN):
    for s in (-1, 1):
        training_table(kit, lv, x0=s * TT_L / 2, modesty=modesty, top=top)
    if lv < 2:
        flat_box(kit, (0.12, 0.003, 0.05), (0.0, TT_H - TT_T - 0.0015, 0.12), SBLACK, name="ganging plate")
    col(kit, lv, (0.0, TT_H / 2, 0.0), (2 * TT_L, TT_H, TT_D))
    lv == 0 and kit.support("top", S.ub((0.0, TT_H, 0.0)), (2 * TT_L, TT_D))


# ---------------------------------------------------------------- the upholstered stack chair (LS57, LS98)
SC_TUBE = 0.0111          # 7/8 in OD chrome tube
SC_X = 0.236              # frame tube centres at +-X
SC_SEAT = 0.418           # seat rail centre height


def stack_chair(kit, lv, base=(0.0, 0.0, 0.0), fabric=NAVYF, glides=True):
    """A 1980s upholstered stack chair (LS57 / LS98), seat 18 in: two bends of 7/8 in chrome tube a side (rear leg and back
    post in one; front leg and seat rail in one), cross rails under the seat, a black moulded seat pan with its cushion and
    a black back shell with its thin pad (both in the fabric slot: navy; _Charcoal / _Teal by slot swap), nylon glides,
    stacking bumpers. Frame wider than the seat so chairs nest; base = a local offset (stacks)."""
    bx, by, bz = base
    seg = (14, 8, 6)[lv]
    nb = (4, 2, 1)[lv]

    def P(x, y, z):
        return (bx + x, by + y, bz + z)
    for s in (-1, 1):
        x = s * SC_X
        a = [P(x, 0.010, -0.250), P(x, SC_SEAT + 0.012, -0.202), P(x, 0.800, -0.252)]
        b = [P(x, 0.010, 0.236), P(x, SC_SEAT, 0.206), P(x, SC_SEAT, -0.190)]
        for nm, pts, br in (("rear leg and post", a, 0.090), ("front leg and rail", b, 0.045)):
            m = S.Mesh()
            S.sweep_section(m, rounded_path(pts, br, nb), S.circle(SC_TUBE, seg), cap0=True, cap1=True)
            m.part(kit, nm, CHROME)
        if glides and lv < 2:
            for (gx, gz) in ((x, -0.250), (x, 0.236)):
                rcyl(kit, "y", 0.0125, 0.012, P(gx, 0.006, gz), BLACK, lv, n0=10, w=0.0, name="glide")
        if lv == 0:
            flat_box(kit, (0.012, 0.008, 0.034), P(x, SC_SEAT + SC_TUBE + 0.004, 0.120), BLACK, name="stack bumper")
    for zc in (0.168, -0.178):
        tube_path(kit, "cross rail", [P(-SC_X, SC_SEAT - 0.004, zc), P(SC_X, SC_SEAT - 0.004, zc)], 0.0095, CHROME, lv, segs=(12, 8, 6)[lv])
    ys = SC_SEAT + SC_TUBE
    box(kit, (0.400, 0.010, 0.380), P(0.0, ys + 0.005, 0.010), BLACK, lv, w=0.0035, name="seat pan", segs=2 if lv == 0 else None)
    cs = (0.440, 0.048, 0.420)
    if lv < 2:
        kit.soft_box((cs[0], cs[2], cs[1]), S.ub(P(0.0, ys + 0.008 + cs[1] / 2, 0.012)), fabric, radius=0.026,
                     segments=(24, 12)[lv], rings=(12, 6)[lv], sag=0.005, puff=0.003, name="seat cushion")
    else:
        flat_box(kit, (cs[0], cs[1], cs[2]), P(0.0, ys + 0.010 + cs[1] / 2, 0.012), fabric, name="seat cushion")
    tilt = 11.0
    zb, yb = -0.236, 0.655
    ta = math.radians(tilt)
    zshell = zb + 0.012 + 0.003
    bwb, bsb = S.bev(0.0025, lv)
    kit.box((0.415, 0.006, 0.235), S.ub(P(0.0, yb, zshell)), BLACK, bevel=bwb, segments=min(bsb, 1), rot=(-tilt, 0, 0), name="back shell")
    pc = P(0.0, yb + 0.013 * math.sin(ta), zshell + 0.013 * math.cos(ta))
    if lv < 2:
        kit.soft_box((0.410, 0.022, 0.230), S.ub(pc), fabric, radius=0.010, segments=(22, 10)[lv], rings=(11, 5)[lv], puff=0.003,
                     rot=(-tilt, 0, 0), name="back pad")
    else:
        kit.box((0.405, 0.020, 0.225), S.ub(pc), fabric, bevel=0.0, segments=0, rot=(-tilt, 0, 0), name="back pad")


def chair_proxy(kit, base=(0.0, 0.0, 0.0), fabric=NAVYF):
    """LOD2 stand-in of a stack chair inside a group kit: seat, back and four legs as boxes."""
    bx, by, bz = base
    flat_box(kit, (0.43, 0.06, 0.41), (bx, by + SC_SEAT + 0.04, bz + 0.01), fabric, name="seat")
    flat_box(kit, (0.41, 0.24, 0.03), (bx, by + 0.655, bz - 0.225), fabric, name="back")
    for sx in (-1, 1):
        for (z0, z1) in ((0.236, 0.206), (-0.250, -0.202)):
            flat_box(kit, (0.022, SC_SEAT, 0.022), (bx + sx * SC_X, by + SC_SEAT / 2, bz + (z0 + z1) / 2), CHROME, name="leg")


def stack_chair_kit(kit, lv):
    stack_chair(kit, lv)
    col(kit, lv, (0.0, 0.41, 0.0), (0.53, 0.82, 0.55))


def chair_stack(kit, lv, n=6):
    """Kit_StackChair_Stack6: six chairs nested, each 85 mm up and 30 mm back on the one below (the backs nest behind)."""
    for k in range(n):
        if lv == 2 and k < n - 1:
            chair_proxy(kit, base=(0.0, 0.085 * k, -0.030 * k)) if k == 0 else flat_box(
                kit, (0.43, 0.06, 0.41), (0.0, 0.085 * k + SC_SEAT + 0.04, -0.030 * k + 0.01), NAVYF, name="seat")
            continue
        stack_chair(kit, lv if k == n - 1 else min(2, lv + 1), base=(0.0, 0.085 * k, -0.030 * k), glides=(k == 0))
    col(kit, lv, (0.0, (0.81 + 0.085 * (n - 1)) / 2, -0.015 * (n - 1)), (0.53, 0.81 + 0.085 * (n - 1), 0.55 + 0.030 * (n - 1)))


def table_row_tucked(kit, lv, top=FAWN):
    """Kit_TrainingTable_Row2_Tucked: the two-table row with four stack chairs pushed in (seats under the top, 'cleared')."""
    table_row(kit, lv, top=top)
    for x in (-1.14, -0.38, 0.38, 1.14):
        if lv == 2:
            chair_proxy(kit, base=(x, 0.0, 0.440))
        else:
            stack_chair(kit, min(2, lv + 1), base=(x, 0.0, 0.440))
    col(kit, lv, (0.0, 0.41, 0.455), (2 * TT_L, 0.82, 0.46))


def handout_set(kit, lv, m, x, z, seed, binder=True, pencil=True, tent=True):
    """At one seat (a point on the table top, the seat toward +Z): a stapled handout, the course binder (a 1-1/2 in vinyl
    three-ring binder lying flat), a name tent and a pencil."""
    rnd = lcg(seed)
    y = TT_H + 0.0004
    a = math.radians((rnd() - 0.5) * 12.0)
    hw, hl = 0.216, 0.279
    ca, sa = math.cos(a), math.sin(a)

    def Pp(u, v, yy):
        return (x + u * ca + v * sa, yy, z - u * sa + v * ca)
    hu0, hv0, hu1, hv1 = rect("HANDOUT")
    for k in range(3 if lv == 0 else 1):
        yy = y + 0.0003 * k
        # the reader sits at +Z: the page's foot toward +Z, its left edge at +X (the reader's left)
        C._oriented(m, [Pp(hw / 2, hl / 2, yy), Pp(-hw / 2, hl / 2, yy), Pp(-hw / 2, -hl / 2, yy), Pp(hw / 2, -hl / 2, yy)],
                    [(hu0, hv0), (hu1, hv0), (hu1, hv1), (hu0, hv1)], (0, 1, 0))
    if binder:
        bx, bz = x + 0.21 * (1 if rnd() < 0.5 else -1), z - 0.06
        by = 0.038
        bc = colour("vinyl_navy" if rnd() < 0.6 else "vinyl_teal")
        box_quads(m, (bx, TT_H + by / 2, bz), (0.290, by, 0.300), {"x": rect("BINDER_SPINE"), "y": bc, "z": bc},
                  yaw=(rnd() - 0.5) * 10.0, base=(0.0, 0.0, 0.0), faces="xXzZY")
    if tent and lv < 2:
        tx, tz = x, z - 0.17
        tw, th = 0.200, 0.075
        ru0, rv0, ru1, rv1 = rect("TENT_CARD")
        for sgn in (1, -1):
            C._oriented(m, [(tx + sgn * tw / 2, TT_H, tz + sgn * 0.022), (tx - sgn * tw / 2, TT_H, tz + sgn * 0.022),
                            (tx - sgn * tw / 2, TT_H + th, tz), (tx + sgn * tw / 2, TT_H + th, tz)],
                        [(ru0, rv0), (ru1, rv0), (ru1, rv1), (ru0, rv1)], (0, 0.3, sgn))
    if pencil and lv == 0:
        pa = math.radians(20 + rnd() * 40)
        px, pz = x + 0.13, z + 0.05
        d = Vector((math.cos(pa), 0.0, -math.sin(pa))) * 0.095
        p0 = (px - d.x, TT_H + 0.0036, pz - d.z)
        p1 = (px + d.x, TT_H + 0.0036, pz + d.z)
        mp = S.Mesh()
        S.sweep_section(mp, [p0, p1], S.circle(0.0036, 6), cap0=True, cap1=True)
        o = mp.part(kit, "pencil", PAPER)
        uu0, vv0, uu1, vv1 = colour("pencil_yellow")
        for loop_uv in o.data.uv_layers.active.data:
            loop_uv.uv = ((uu0 + uu1) / 2, (vv0 + vv1) / 2)


def table_row_handouts(kit, lv, top=FAWN):
    """Kit_TrainingTable_Row2_Handouts: the row set for a session ('midTask'): handouts, binders, name tents and pencils at
    four seats, a paper cup by one of them (chairs are separate Kit_StackChair kits, pulled out)."""
    table_row(kit, lv, top=top)
    m = S.Mesh()
    for k, x in enumerate((-1.14, -0.38, 0.38, 1.14)):
        handout_set(kit, lv, m, x, 0.06, 41 + k * 13, binder=(k != 2))
    m.part(kit, "handouts", PAPER)
    if lv < 2:
        S.lathe_y(kit, [(0.027, 0.0), (0.0275, 0.002), (0.036, 0.090), (0.0375, 0.092), (0.0375, 0.094), (0.0355, 0.094), (0.026, 0.004)],
                  (0.62, TT_H, -0.09), PWHITE, lv, name="paper cup", close_top=True)


# ---------------------------------------------------------------- projection screens (R3: 70 x 70 in, 22 ga octagonal case 73.5 in)
SCR = 1.778
CASE_L, CASE_R = 1.867, 0.046


def octa_case(kit, lv, centre, length=CASE_L, r=CASE_R, slot=PAINT, caps=BLACK):
    """The screen's octagonal roller case along X (flats top and bottom), its end caps, the fabric slot under the front."""
    cx, cy, cz = centre
    sec = [(r * math.cos(math.radians(22.5 + 45 * k)), r * math.sin(math.radians(22.5 + 45 * k))) for k in range(8)]
    m = S.Mesh()
    S.sweep_section(m, [(cx - length / 2, cy, cz), (cx + length / 2, cy, cz)], sec, cap0=False, cap1=False)
    o = m.part(kit, "screen case", slot)
    if lv < 2:
        kit._bevel(o, 0.0012, 1, angle=30.0)
    for s in (-1, 1):
        rcyl(kit, "x", r * 1.07, 0.014, (cx + s * (length / 2 + 0.006), cy, cz), caps, lv, n0=8, w=0.0015, name="end cap")
    if lv < 2:
        flat_box(kit, (length - 0.04, 0.004, 0.010), (cx, cy - r * 0.93, cz + 0.010), SBLACK, name="fabric slot")


def screen_face(kit, lv, x0, x1, y0, y1, z, lit, back_slot=PWHITE, tilt_top_dz=0.0):
    """The matte white surface (Prop_ProjectionLit_Off, SCREEN cell; lit: Prop_ProjectionLit with the overhead's keystoned
    square in its emission) facing +Z, and its back. tilt_top_dz moves the top edge forward (the keystone arm)."""
    rc = prect("SCREEN")
    m = S.Mesh()
    C._oriented(m, [(x1, y0, z), (x0, y0, z), (x0, y1, z + tilt_top_dz), (x1, y1, z + tilt_top_dz)],
                [(rc[0], rc[1]), (rc[2], rc[1]), (rc[2], rc[3]), (rc[0], rc[3])], (0, 0, 1))
    m.part(kit, "screen surface", PLIT if lit else PLIT_OFF)
    m2 = S.Mesh()
    C._oriented(m2, [(x0, y0, z - 0.0008), (x1, y0, z - 0.0008), (x1, y1, z + tilt_top_dz - 0.0008), (x0, y1, z + tilt_top_dz - 0.0008)],
                [(0, 0), (1, 0), (1, 1), (0, 1)], (0, 0, -1))
    m2.part(kit, "screen back", back_slot)


def projection_screen_wall(kit, lv, mode="rolled"):
    """Kit_ProjectionScreen_Wall (R3): a 70 x 70 in wall screen in its octagonal case on two hook brackets; origin the wall
    point at the brackets' top (the case hangs below and 65 mm out). rolled: the slat and pull ring under the case;
    pulled: the black drop and the matte white surface down to the slat (dark); lit: the overhead's square on it."""
    zc, yc = 0.065, -0.055
    octa_case(kit, lv, (0.0, yc, zc))
    for s in (-1, 1):
        x = s * 0.78
        box(kit, (0.032, 0.105, 0.004), (x, -0.050, 0.002), SBLACK, lv, w=0.0008, name="bracket plate", segs=1)
        box(kit, (0.032, 0.004, zc + 0.006), (x, -0.002, (zc + 0.006) / 2), SBLACK, lv, w=0.0008, name="bracket arm", segs=1)
        if lv < 2:
            box(kit, (0.032, 0.022, 0.004), (x, -0.013, zc + 0.004), SBLACK, lv, w=0.0008, name="bracket hook", segs=1)
    y_slot = yc - CASE_R
    if mode == "rolled":
        y_slat = y_slot - 0.012
    else:
        drop = 0.150
        y_top = y_slot - 0.004
        y_img = y_top - drop
        y_slat = y_img - SCR - 0.010
        m = S.Mesh()
        cu0, cv0, cu1, cv1 = colour("vcr_black")
        for zz, o in ((zc + 0.0102, (0, 0, 1)), (zc + 0.0094, (0, 0, -1))):
            C._oriented(m, [(-SCR / 2, y_img, zz), (SCR / 2, y_img, zz), (SCR / 2, y_top, zz), (-SCR / 2, y_top, zz)],
                        [(cu0, cv0), (cu1, cv0), (cu1, cv1), (cu0, cv1)], o)
        m.part(kit, "black drop", PAPER)
        screen_face(kit, lv, -SCR / 2, SCR / 2, y_slat + 0.010, y_img, zc + 0.0100, mode == "lit")
    box(kit, (SCR + 0.024, 0.022, 0.012), (0.0, y_slat, zc + 0.010), PWHITE, lv, w=0.0030, name="slat")
    if lv < 2:
        tube_path(kit, "pull cord", [(0.0, y_slat - 0.010, zc + 0.016), (0.0, y_slat - 0.110, zc + 0.018)], 0.0016, PWHITE, lv, segs=6)
        n = (14, 8)[lv]
        ring = [(0.018 * math.sin(2 * math.pi * k / n), y_slat - 0.128 + 0.018 * math.cos(2 * math.pi * k / n), zc + 0.018) for k in range(n + 1)]
        mm = S.Mesh()
        S.sweep_section(mm, ring, S.circle(0.0025, 6))
        mm.part(kit, "pull ring", BLACK)
    lv == 0 and kit.no_collider()


def projection_screen_tripod(kit, lv):
    """Kit_ProjectionScreen_Tripod: a 70 x 70 in portable tripod screen: three folding legs from a collar on the square
    centre tube, braces to a lower collar, the case clamped across the tube at 0.48 m, the surface pulled up from it to the
    slat hooked on the keystone arm at the top of the extension tube (the top 0.12 m forward)."""
    tz = -0.030
    yb, y_case = 0.30, 0.48
    box(kit, (0.025, 2.32 - 0.12, 0.025), (0.0, 0.12 + (2.32 - 0.12) / 2, tz), SBLACK, lv, w=0.0015, name="centre tube", segs=1)
    box(kit, (0.019, 0.60, 0.019), (0.0, 2.32 - 0.30, tz), ALU, lv, w=0.0012, name="extension tube", segs=1)
    feet = [(0.0, -0.40), (0.36, 0.20), (-0.36, 0.20)]
    collar = (0.0, 0.58, tz)
    box(kit, (0.045, 0.060, 0.045), collar, SBLACK, lv, w=0.0030, name="leg collar", segs=1)
    box(kit, (0.040, 0.040, 0.040), (0.0, yb, tz), SBLACK, lv, w=0.0030, name="brace collar", segs=1)
    for (fx, fz) in feet:
        top = (fx * 0.06, 0.57, tz + fz * 0.06)
        foot = (fx, 0.012, tz + fz)
        tube_path(kit, "leg", [top, foot], 0.0095, SBLACK, lv, segs=(10, 6, 6)[lv])
        mid = tuple(top[i] + (foot[i] - top[i]) * 0.55 for i in range(3))
        if lv < 2:
            tube_path(kit, "brace", [(fx * 0.02, yb, tz + fz * 0.02), mid], 0.0055, SBLACK, lv, segs=(8, 6)[lv])
            rcyl(kit, "y", 0.013, 0.020, (fx, 0.010, tz + fz), RUBBER, lv, n0=10, w=0.0, name="leg tip")
    octa_case(kit, lv, (0.0, y_case, tz + 0.055), length=1.90, r=0.040, slot=SBLACK, caps=BLACK)
    box(kit, (0.06, 0.07, 0.06), (0.0, y_case, tz + 0.020), SBLACK, lv, w=0.0020, name="case clamp", segs=1)
    y0, y1 = y_case + 0.042, y_case + 0.042 + SCR
    dz = 0.12
    screen_face(kit, lv, -SCR / 2, SCR / 2, y0, y1, tz + 0.058, False, back_slot=PWHITE, tilt_top_dz=dz)
    box(kit, (SCR + 0.024, 0.022, 0.012), (0.0, y1 + 0.008, tz + 0.058 + dz), SBLACK, lv, w=0.0030, name="top slat")
    tube_path(kit, "keystone arm", [(0.0, 2.32, tz), (0.0, 2.33, tz + 0.06), (0.0, y1 + 0.03, tz + 0.058 + dz)], 0.0070, SBLACK, lv,
              bend_r=0.03, segs=(8, 6, 6)[lv])
    col(kit, lv, (0.0, 0.60, tz - 0.05), (0.80, 1.20, 0.70))


# ---------------------------------------------------------------- the overhead projector (LS91: 14.2 x 16.75 x 28 in, 10.5 in stage)
def overhead_projector(kit, lv, lit=False):
    """Kit_OverheadProjector (LS91): a 1988 transmissive overhead: the beige base housing (lamp, fan and Fresnel inside;
    side vents, the switch and cord at the operator's side) with the 10.5 in glass stage 8.5 in up in its grey deck, the
    square post standing at the -Z edge (the audience side), the focus knob on its rack, the arm and the head over the
    stage: the lens barrel looking down and the mirror throwing the image toward +Z, over the operator who stands at +Z.
    lit: the stage glows with the transparency on it (Prop_ProjectionLit, STAGE cell); dark: Prop_ProjectionLit_Off."""
    W, D = 0.360, 0.425
    yt = 0.217
    box(kit, (W, yt - 0.014, D), (0.0, 0.014 + (yt - 0.014) / 2, 0.0), PBEIGE, lv, w=0.0080, name="base housing")
    if lv < 2:
        for x in (-W / 2 + 0.04, W / 2 - 0.04):
            for z in (-D / 2 + 0.04, D / 2 - 0.04):
                rcyl(kit, "y", 0.014, 0.014, (x, 0.007, z), RUBBER, lv, n0=8, w=0.0, name="foot")
    box(kit, (W - 0.020, 0.012, D - 0.020), (0.0, yt + 0.004, 0.0), PGREY, lv, w=0.0030, name="stage deck")
    st = 0.267
    m = S.Mesh()
    rc = prect("STAGE")
    yg = yt + 0.0105
    C._oriented(m, [(st / 2, yg, st / 2), (-st / 2, yg, st / 2), (-st / 2, yg, -st / 2), (st / 2, yg, -st / 2)],
                [(rc[0], rc[1]), (rc[2], rc[1]), (rc[2], rc[3]), (rc[0], rc[3])], (0, 1, 0))
    m.part(kit, "stage", PLIT if lit else PLIT_OFF)
    box(kit, (st + 0.004, 0.003, st + 0.004), (0.0, yg + 0.0016, 0.0), GLASS, lv, w=0.0006, name="stage glass", segs=1)
    if lv < 2:
        for s in (-1, 1):
            for k in range(7):
                flat_box(kit, (0.002, 0.008, 0.200), (s * (W / 2 + 0.0008), 0.060 + 0.016 * k, -0.04), SBLACK, name="vent slot")
        box(kit, (0.030, 0.016, 0.010), (0.120, 0.120, D / 2 + 0.004), BLACK, lv, w=0.0015, name="switch", segs=1)
        tube_path(kit, "cord", [(-0.12, 0.05, -D / 2 - 0.002), (-0.12, 0.02, -D / 2 - 0.06), (-0.16, 0.004, -D / 2 - 0.30)], 0.0035,
                  BLACK, lv, bend_r=0.04, segs=6)
    zp = -0.170
    yp1 = 0.620
    box(kit, (0.040, yp1 - yt, 0.036), (0.0, yt + (yp1 - yt) / 2, zp), PBEIGE, lv, w=0.0030, name="post")
    box(kit, (0.052, 0.050, 0.052), (0.0, yt + 0.032, zp), PGREY, lv, w=0.0030, name="post foot", segs=1)
    rcyl(kit, "x", 0.020, 0.026, (0.034, 0.470, zp), BLACK, lv, n0=16, w=0.0020, name="focus knob")
    box(kit, (0.060, 0.050, 0.060), (0.0, 0.470, zp), PGREY, lv, w=0.0030, name="focus carriage", segs=1)
    box(kit, (0.050, 0.038, 0.130), (0.0, 0.590, zp + 0.065), PBEIGE, lv, w=0.0030, name="arm")
    yh = 0.600
    box(kit, (0.140, 0.095, 0.120), (0.0, yh, 0.010), PBEIGE, lv, w=0.0060, name="head")
    rcyl(kit, "y", 0.046, 0.050, (0.0, yh - 0.068, 0.010), BLACK, lv, n0=24, w=0.0020, name="lens barrel")
    if lv < 2:
        m2 = S.Mesh()
        C.face_quad(m2, (0.0, yh - 0.0935, 0.010), "-y", 0.074, 0.074, (0.4, 0.4, 0.6, 0.6))
        m2.part(kit, "lens glass", GLASS)
    bwb, bsb = S.bev(0.0015, lv)
    kit.box((0.128, 0.004, 0.110), S.ub((0.0, yh + 0.060, 0.064)), SBLACK, bevel=bwb, segments=min(1, bsb), rot=(-45, 0, 0), name="mirror back")
    kit.box((0.120, 0.002, 0.102), S.ub((0.0, yh + 0.058, 0.067)), GLASS, bevel=0.0, segments=0, rot=(-45, 0, 0), name="mirror")
    lv == 0 and kit.no_collider()
    lv == 0 and kit.anchor("beam", S.ub((0.0, yh + 0.04, 0.10)))


# ---------------------------------------------------------------- the floor lectern (LS57 / LS91)
def lectern(kit, lv, lit=False, wood=WALNUT):
    """Kit_Lectern: a walnut-veneer floor lectern of the late 1980s: recessed plinth, the carcass with raised panels on the
    audience face (-Z) and the sides, open at the speaker's side (+Z) with two shelves, the slanted reading top falling 15
    degrees toward the speaker with its book ledge, a brass gooseneck lamp with a hooded shade at the high rear corner.
    lit: the lamp on (Prop_BulbLit under the hood) and notes on the top."""
    W, D = 0.610, 0.450
    y0, yb = 0.070, 1.050
    box(kit, (W - 0.030, y0, D - 0.030), (0.0, y0 / 2, -0.005), SBLACK, lv, w=0.0030, name="plinth", segs=1)
    t = 0.019
    slope = math.radians(15.0)
    zf, zb = -D / 2, D / 2
    yhi = yb + 0.120        # the top's rear (audience) edge
    ylo = yhi - D * math.tan(slope)

    def ytop(z):
        return yhi - (z - zf) * math.tan(slope)
    for s in (-1, 1):
        def side(s=s):
            pts = []
            for bits in range(8):
                xb, ybit, zbit = bits & 1, (bits >> 1) & 1, (bits >> 2) & 1
                x = s * W / 2 - s * t * (1 - (xb if s > 0 else 1 - xb))      # the panel's outer face at s * W / 2, t thick
                z = zf if zbit == 0 else zb
                y = y0 if ybit == 0 else ytop(z) - 0.022
                pts.append((x, y, z))
            return pts
        o = oriented_box(kit, "carcass side", side, wood)
        if lv < 2:
            kit._bevel(o, 0.0020 if lv == 0 else 0.0015, 1, angle=30.0)
    box(kit, (W - 2 * t, yhi - 0.022 - y0, t), (0.0, y0 + (yhi - 0.022 - y0) / 2, zf + t / 2), wood, lv, w=0.0015, name="front panel", segs=1)
    for yy in (0.30, 0.80):
        box(kit, (W - 2 * t, t, D - t - 0.010), (0.0, yy, zf + t + (D - t - 0.010) / 2), wood, lv, w=0.0012, name="shelf", segs=1)
    box(kit, (W - 2 * t, 0.060, t), (0.0, ylo - 0.022 - 0.030, zb - t / 2), wood, lv, w=0.0015, name="rear apron", segs=1)
    # raised panels: mouldings on the audience face and the sides
    if lv < 2:
        mw, mt = 0.030, 0.008
        fy0, fy1 = y0 + 0.080, yhi - 0.110
        fx = W / 2 - t - 0.045
        for (sx, sy, cx, cy) in ((2 * fx, mw, 0.0, fy1), (2 * fx, mw, 0.0, fy0), (mw, fy1 - fy0 + mw, fx, (fy0 + fy1) / 2),
                                 (mw, fy1 - fy0 + mw, -fx, (fy0 + fy1) / 2)):
            box(kit, (sx, sy, mt), (cx, cy, zf - mt / 2), wood, lv, w=0.0025, name="moulding", segs=2 if lv == 0 else None)
        box(kit, (2 * fx - mw - 0.040, fy1 - fy0 - mw - 0.040, 0.006), (0.0, (fy0 + fy1) / 2, zf - 0.003), wood, lv, w=0.0035,
            name="raised panel", segs=2 if lv == 0 else None)
        for s in ((-1, 1) if lv == 0 else ()):
            zs0, zs1 = zf + 0.07, zb - 0.07
            sy1 = ytop(zb) - 0.13
            for (sz, syy, cz, cy) in ((zs1 - zs0, mw, (zs0 + zs1) / 2, sy1), (zs1 - zs0, mw, (zs0 + zs1) / 2, fy0),
                                      (mw, sy1 - fy0 + mw, zs0, (fy0 + sy1) / 2), (mw, sy1 - fy0 + mw, zs1, (fy0 + sy1) / 2)):
                box(kit, (mt, syy, sz), (s * (W / 2 + mt / 2), cy, cz), wood, lv, w=0.0025, name="side moulding", segs=1)
    # the slanted top and the book ledge
    zc = 0.0
    yc = ytop(zc) + 0.011
    tw, td = W + 0.050, D / math.cos(slope) + 0.060
    bwb, bsb = S.bev(0.0030, lv)
    kit.box((tw, td, 0.022), S.ub((0.0, yc, zc + 0.010)), wood, bevel=bwb, segments=bsb, rot=(15, 0, 0), name="reading top")

    def ysurf(z):
        """The slanted top's upper surface at Unity z (the box is centred at (yc, zc + 0.010), 22 mm thick, turned 15 deg)."""
        return yc + (0.011 - (z - (zc + 0.010)) * math.sin(slope)) / math.cos(slope)
    nz = math.sin(slope)
    ny = math.cos(slope)
    zl = zb + 0.030 - 0.012
    yl = ytop(zl) + 0.022 + 0.012
    kit.box((tw - 0.020, 0.016, 0.028), S.ub((0.0, yl, zl)), wood, bevel=bwb, segments=min(bsb, 1), rot=(15, 0, 0), name="book ledge")
    # the lamp
    if lv < 2:
        bx, bz = 0.215, zf + 0.060
        by = ysurf(bz)
        rcyl(kit, "y", 0.034, 0.014, (bx, by + 0.007, bz), BRASS, lv, n0=20, w=0.0020, name="lamp base")
        neck = [(bx, by + 0.014, bz), (bx, by + 0.150, bz + 0.010), (bx - 0.030, by + 0.250, bz + 0.080), (bx - 0.070, by + 0.250, bz + 0.180)]
        tube_path(kit, "gooseneck", neck, 0.0058, BRASS, lv, bend_r=0.06, segs=(10, 6)[lv])
        hx, hy, hz = bx - 0.090, by + 0.232, bz + 0.230
        S.lathe_axis(kit, [(0.000, 0.000), (0.020, 0.002), (0.030, 0.030), (0.034, 0.075), (0.031, 0.076), (0.027, 0.032), (0.0, 0.010)],
                     (hx, hy + 0.030, hz), "-y", BRASS, lv, name="lamp hood", close_top=False, close_bottom=False)
        rcyl(kit, "y", 0.014, 0.020, (hx, hy - 0.002, hz), BULB if lit else PWHITE, lv, n0=12, w=0.0, name="bulb")
    if lit:
        m = S.Mesh()
        hu0, hv0, hu1, hv1 = rect("HANDOUT")
        for k, (dx, a) in enumerate(((-0.05, -4.0), (0.06, 6.0))):
            ca, sa = math.cos(math.radians(a)), math.sin(math.radians(a))
            pts = []
            for (u, v) in ((0.108, 0.140), (-0.108, 0.140), (-0.108, -0.140), (0.108, -0.140)):
                x = dx + u * ca + v * sa
                zz = zc + 0.02 - u * sa + v * ca
                pts.append((x, ysurf(zz) + 0.0008 + 0.0005 * k, zz))
            C._oriented(m, pts, [(hu0, hv0), (hu1, hv0), (hu1, hv1), (hu0, hv1)], (0, ny, nz))
        m.part(kit, "notes", PAPER)
    col(kit, lv, (0.0, 0.59, 0.0), (W, 1.18, D + 0.05))


# ---------------------------------------------------------------- AV carts (R5: 24 x 18 in shelves, 26 / 42 in, 4 in casters)
def rolled_tray(kit, lv, y_top, w=0.610, d=0.460, slot=SBLACK, name="shelf"):
    """A 24 x 18 in steel cart shelf: the deck and its four edges rolled down 25 mm (a box each, the roll a generous bevel)."""
    box(kit, (w - 0.010, 0.0024, d - 0.010), (0.0, y_top - 0.0012, 0.0), slot, lv, w=0.0008, name=name, segs=1)
    for s in (-1, 1):
        if lv == 1:
            flat_box(kit, (w, 0.025, 0.006), (0.0, y_top - 0.0125, s * (d / 2 - 0.003)), slot, name=name + " roll")
            flat_box(kit, (0.006, 0.025, d - 0.012), (s * (w / 2 - 0.003), y_top - 0.0125, 0.0), slot, name=name + " roll")
            continue
        box(kit, (w, 0.025, 0.006), (0.0, y_top - 0.0125, s * (d / 2 - 0.003)), slot, lv, w=0.0028, name=name + " roll", segs=2)
        box(kit, (0.006, 0.025, d - 0.012), (s * (w / 2 - 0.003), y_top - 0.0125, 0.0), slot, lv, w=0.0028, name=name + " roll", segs=1)


def av_cart(kit, lv, tall=False, cord_floor=False):
    """Kit_AVCart_Low / _Tall (R5): black steel AV cart, 24 x 18 in rolled-edge shelves on 1 in square posts, four 4 in
    casters (the two at +Z with brakes), the three-outlet power assembly under the top shelf's front edge, its cord coiled
    on two hooks on the back posts (cord_floor: run down to the floor instead); tall: a VHS deck on the middle shelf.
    Anchors: 'top' (the top shelf, a projector) / 'tv' (Kit_CRTTV on the tall cart's top)."""
    W, D = 0.610, 0.460
    y_top = 1.067 if tall else 0.660
    xc, zc = W / 2 - 0.050, D / 2 - 0.050
    h_top = 0.0
    for (x, z, br) in ((xc, zc, True), (-xc, zc, True), (xc, -zc, False), (-xc, -zc, False)):
        h_top = caster(kit, lv, x, z, wheel_d=0.102, brake=br, slot_fork=CHROME)
    px, pz = W / 2 - 0.022, D / 2 - 0.022
    for sx in (-1, 1):
        for sz in (-1, 1):
            if lv == 1:
                flat_box(kit, (0.0254, y_top - h_top, 0.0254), (sx * px, h_top + (y_top - h_top) / 2, sz * pz), SBLACK, name="post")
            else:
                box(kit, (0.0254, y_top - h_top, 0.0254), (sx * px, h_top + (y_top - h_top) / 2, sz * pz), SBLACK, lv, w=0.0018,
                    name="post", segs=1)
    shelves = [h_top + 0.050, y_top] + ([0.620] if tall else [])
    for y in shelves:
        rolled_tray(kit, lv, y + 0.0012 if y == y_top else y)
    if lv < 2:
        sy = y_top - 0.045
        box(kit, (0.190, 0.040, 0.046), (0.0, sy, D / 2 - 0.030), SBLACK, lv, w=0.0030, name="power strip", segs=1)
        dbox(kit, (0.180, 0.030, 0.002), (0.0, sy, D / 2 - 0.0062), rect("STRIP_FACE"), lv, w=0.0002, name="strip face", segs=1)
        for sx in (-1, 1):
            tube_path(kit, "cord hook", [(sx * px, 0.42 if not tall else 0.80, -pz - 0.013), (sx * px, 0.42 if not tall else 0.80, -pz - 0.045),
                                         (sx * px, (0.42 if not tall else 0.80) + 0.03, -pz - 0.050)], 0.003, SBLACK, lv, bend_r=0.01, segs=5)
        hy = 0.42 if not tall else 0.80
        if not cord_floor:
            n = (14, 8)[lv]
            for k in range(2 if lv == 0 else 1):
                loop = []
                for j in range(n + 1):
                    a = 2 * math.pi * j / n
                    loop.append((0.11 * math.cos(a) * (1 + 0.04 * k), hy - 0.13 - 0.12 * math.sin(a) - 0.006 * k, -pz - 0.040 - 0.006 * k))
                mm = S.Mesh()
                S.sweep_section(mm, loop, S.circle(0.0035, 5))
                mm.part(kit, "cord coil", BLACK)
        else:
            tube_path(kit, "cord", [(0.07, sy - 0.02, D / 2 - 0.030), (0.10, sy - 0.08, D / 2 - 0.02), (0.20, 0.30, D / 2 + 0.02),
                                    (0.26, 0.004, D / 2 + 0.10), (0.30, 0.004, D / 2 + 0.70)], 0.0035, BLACK, lv, bend_r=0.08, segs=6)
    if tall:
        vy = 0.620 + 0.0012
        box(kit, (0.430, 0.094, 0.330), (0.0, vy + 0.047, -0.020), SBLACK, lv, w=0.0030, name="vcr case", segs=1)
        dbox(kit, (0.426, 0.090, 0.002), (0.0, vy + 0.047, -0.020 + 0.166), rect("VCR_FACE"), lv, w=0.0002, name="vcr face", segs=1)
        if lv < 2:
            for x in (-0.18, 0.18):
                flat_box(kit, (0.030, 0.006, 0.030), (x, vy + 0.003, 0.08), RUBBER, name="vcr foot")
        lv == 0 and kit.anchor("tv", S.ub((0.0, y_top + 0.0012, 0.0)))
    lv == 0 and kit.anchor("top", S.ub((0.0, y_top + 0.0012, 0.0)))
    col(kit, lv, (0.0, y_top / 2, 0.0), (W, y_top, D))


# ---------------------------------------------------------------- boards (LS99: 8 x 4 ft porcelain marker board; slate)
def marker_board(kit, lv, face="clean"):
    """Kit_MarkerBoard (LS99): an 8 x 4 ft porcelain-steel marker board on the wall, satin anodised aluminium frame with grey
    corner caps, the full-length aluminium tray (sill 0.90 m) with four markers and a felt eraser. face: clean (ghosting),
    written (marker diagrams, no letters), chalk (a slate green chalkboard, chalk sticks and dust in the tray).
    Origin the wall point at floor level, front +Z; no collider."""
    W, H = 2.440, 1.220
    sill = 0.900
    y0 = sill + 0.006
    yc = y0 + H / 2
    fw, fd = 0.022, 0.024
    cell = {"clean": "BOARD_CLEAN", "written": "BOARD_WRITTEN", "chalk": "BOARD_CHALK"}[face]
    box(kit, (W - 0.02, H - 0.02, 0.012), (0.0, yc, 0.006), HARDBOARD, lv, w=0.0010, name="board core", segs=1)
    m = S.Mesh()
    C.face_quad(m, (0.0, yc, 0.0125), "+z", W - 2 * fw + 0.004, H - 2 * fw + 0.004, rect(cell, inset=1.0))
    m.part(kit, "board face", PAPER)
    for (sx, sy, cx, cy) in ((W, fw, 0.0, y0 + fw / 2), (W, fw, 0.0, y0 + H - fw / 2), (fw, H - 2 * fw, W / 2 - fw / 2, yc),
                             (fw, H - 2 * fw, -W / 2 + fw / 2, yc)):
        box(kit, (sx, sy, fd), (cx, cy, fd / 2), SATIN, lv, w=0.0035, name="frame", segs=2 if lv == 0 else None)
    if lv < 2:
        for sx in (-1, 1):
            for sy in (-1, 1):
                if lv == 1:
                    flat_box(kit, (0.038, 0.038, fd + 0.004), (sx * (W / 2 - 0.016), yc + sy * (H / 2 - 0.016), (fd + 0.004) / 2), PGREY,
                             name="corner cap")
                else:
                    box(kit, (0.038, 0.038, fd + 0.004), (sx * (W / 2 - 0.016), yc + sy * (H / 2 - 0.016), (fd + 0.004) / 2), PGREY, lv,
                        w=0.0040, name="corner cap", segs=1)
    # the tray: bottom, front lip, back plate, grey end caps
    td = 0.075
    box(kit, (W - 0.010, 0.004, td), (0.0, sill - 0.002, td / 2), SATIN, lv, w=0.0012, name="tray", segs=1)
    box(kit, (W - 0.010, 0.026, 0.004), (0.0, sill + 0.009, td - 0.002), SATIN, lv, w=0.0016, name="tray lip", segs=1)
    box(kit, (W - 0.010, 0.040, 0.004), (0.0, sill + 0.016, 0.002), SATIN, lv, w=0.0010, name="tray back", segs=1)
    for s in (-1, 1):
        box(kit, (0.012, 0.032, td + 0.004), (s * (W / 2 - 0.002), sill + 0.012, td / 2), PGREY, lv, w=0.0020, name="tray end", segs=1)
    if lv == 2:
        lv == 0 and kit.no_collider()
        return
    if face == "chalk":
        cu0, cv0, cu1, cv1 = colour("chalk")
        mc = S.Mesh()
        for k, (x, a) in enumerate(((-0.62, 4.0), (-0.48, -8.0), (0.35, 2.0), (0.82, 12.0))):
            L = 0.080 if k != 2 else 0.040
            d = Vector((math.cos(math.radians(a)), 0.0, math.sin(math.radians(a)))) * (L / 2)
            S.sweep_section(mc, [(x - d.x, sill + 0.0052, 0.040 - d.z), (x + d.x, sill + 0.0052, 0.040 + d.z)], S.circle(0.0050, 8),
                            cap0=True, cap1=True)
        oc = mc.part(kit, "chalk", PAPER)
        for luv in oc.data.uv_layers.active.data:
            luv.uv = ((cu0 + cu1) / 2, (cv0 + cv1) / 2)
        md = S.Mesh()
        du0, dv0, du1, dv1 = colour("chalk")
        C._oriented(md, [(-0.9, sill + 0.0002, 0.012), (0.9, sill + 0.0002, 0.012), (0.9, sill + 0.0002, 0.050), (-0.9, sill + 0.0002, 0.050)],
                    [(du0, dv0), (du1, dv0), (du1, dv1), (du0, dv1)], (0, 1, 0))
        md.part(kit, "chalk dust", PAPER)
    else:
        caps = (BLACK, "Prop_PlasticBlue", "Prop_PlasticRed", PGREY)
        for k, x in enumerate((-0.55, -0.40, 0.30, 0.46)):
            rcyl(kit, "x", 0.0090, 0.112, (x, sill + 0.0110, 0.038), PWHITE if k != 3 else PGREY, lv, n0=10, w=0.0, name="marker")
            rcyl(kit, "x", 0.0098, 0.040, (x + 0.072, sill + 0.0110, 0.038), caps[k], lv, n0=10, w=0.0, name="marker cap")
    ex = 0.75
    box(kit, (0.125, 0.026, 0.052), (ex, sill + 0.014 + 0.013, 0.040), PGREY, lv, w=0.0040, name="eraser back", segs=1)
    C.dbox(kit, (0.125, 0.012, 0.052), (ex, sill + 0.006 + 0.0002, 0.040), colour("felt_grey"), lv, slot=PAPER, w=0.0010, name="eraser felt", segs=1)
    lv == 0 and kit.no_collider()


# ---------------------------------------------------------------- the flip chart easel (LS57: 27 x 34 in pad)
def flip_easel(kit, lv, face="blank", pages_over=0):
    """Kit_FlipChartEasel (LS57): a telescoping aluminium tripod easel: two front legs carrying the hardboard back with its
    marker tray, the clamp bar and its two knobs holding a 27 x 34 in pad, the rear leg on its top hinge; face: blank or
    written (marker work, no letters); pages_over: that many sheets thrown over the top to the back."""
    lw = 0.330
    yt = 1.850
    for s in (-1, 1):
        tube_path(kit, "front leg", [(s * lw, 0.010, 0.020), (s * 0.300, yt, -0.010)], 0.0110, ALU, lv, segs=(12, 8, 6)[lv])
        if lv < 2:
            rcyl(kit, "y", 0.0125, 0.020, (s * lw, 0.010, 0.020), RUBBER, lv, n0=10, w=0.0, name="leg tip")
    tube_path(kit, "rear leg", [(0.0, yt - 0.06, -0.035), (0.0, 0.010, -0.600)], 0.0110, ALU, lv, segs=(12, 8, 6)[lv])
    tube_path(kit, "rear leg sleeve", [(0.0, 0.85, -0.315), (0.0, 0.010, -0.600)], 0.0135, ALU, lv, segs=(12, 8, 6)[lv])
    if lv < 2:
        box(kit, (0.050, 0.040, 0.040), (0.0, yt - 0.06, -0.030), SBLACK, lv, w=0.0030, name="top hinge", segs=1)
    bw_, bh = 0.700, 0.900
    yb0 = yt - bh - 0.020
    box(kit, (bw_, bh, 0.006), (0.0, yb0 + bh / 2, 0.010), HARDBOARD, lv, w=0.0010, name="back board", segs=1)
    box(kit, (bw_ + 0.010, 0.012, 0.070), (0.0, yb0 - 0.006, 0.040), ALU, lv, w=0.0015, name="tray", segs=1)
    box(kit, (bw_ + 0.010, 0.022, 0.004), (0.0, yb0 + 0.005, 0.073), ALU, lv, w=0.0012, name="tray lip", segs=1)
    pw, ph = 0.686, 0.864
    ycl = yt - 0.030
    pad_t = 0.010
    zp = 0.013 + pad_t / 2
    py1 = ycl
    py0 = py1 - ph
    pu0, pv0, pu1, pv1 = (rect("FLIP_WRITTEN") if face == "written" else colour("paper"))
    m = S.Mesh()
    box_quads(m, (0.0, (py0 + py1) / 2, zp), (pw, ph, pad_t), {"x": colour("paper"), "y": colour("paper"), "z": colour("paper")},
              faces="xXy")
    C._oriented(m, [(pw / 2, py0, zp + pad_t / 2), (-pw / 2, py0, zp + pad_t / 2), (-pw / 2, py1, zp + pad_t / 2), (pw / 2, py1, zp + pad_t / 2)],
                [(pu0, pv0), (pu1, pv0), (pu1, pv1), (pu0, pv1)], (0, 0, 1))
    if pages_over and lv < 2:
        for k in range(pages_over):
            r = 0.020 + 0.003 * k
            zc = zp + pad_t / 2 - r + 0.001 * k
            drop = 0.30 + 0.07 * k
            n = (8, 4)[lv]
            path = []
            for j in range(n + 1):
                a = math.pi * j / n
                path.append((zc + r * math.cos(a), ycl + 0.012 + r * math.sin(a) + 0.002 * k))
            path.append((zc - r - 0.004 * k, ycl + 0.012 - drop))
            for side, off in ((1, 0.0006), (-1, -0.0006)):
                for j in range(len(path) - 1):
                    (za, ya), (zb_, yb_) = path[j], path[j + 1]
                    nz_, ny_ = (yb_ - ya), -(zb_ - za)
                    ln = math.hypot(nz_, ny_) or 1.0
                    nz_, ny_ = nz_ / ln * side, ny_ / ln * side
                    cu0, cv0, cu1, cv1 = colour("paper")
                    C._oriented(m, [(pw / 2, ya + ny_ * 0.0006, za + nz_ * 0.0006), (-pw / 2, ya + ny_ * 0.0006, za + nz_ * 0.0006),
                                    (-pw / 2, yb_ + ny_ * 0.0006, zb_ + nz_ * 0.0006), (pw / 2, yb_ + ny_ * 0.0006, zb_ + nz_ * 0.0006)],
                                [(cu0, cv0), (cu1, cv0), (cu1, cv1), (cu0, cv1)], (0, ny_, nz_))
    m.part(kit, "pad", PAPER)
    box(kit, (bw_ + 0.020, 0.030, 0.022), (0.0, ycl, zp + pad_t / 2 + 0.011), SATIN, lv, w=0.0030, name="clamp bar",
        segs=2 if lv == 0 else None)
    if lv < 2:
        for s in (-1, 1):
            rcyl(kit, "z", 0.016, 0.022, (s * 0.280, ycl, zp + pad_t / 2 + 0.033), BLACK, lv, n0=14, w=0.0025, name="clamp knob")
    col(kit, lv, (0.0, 0.60, -0.25), (0.70, 1.20, 0.65))


def table_kit(kit, lv, top=FAWN):
    """Kit_TrainingTable: one table, its collider and its top as a support."""
    training_table(kit, lv, top=top)
    col(kit, lv, (0.0, TT_H / 2, 0.0), (TT_L, TT_H, TT_D))
    lv == 0 and kit.support("top", S.ub((0.0, TT_H, 0.0)), (TT_L, TT_D))
