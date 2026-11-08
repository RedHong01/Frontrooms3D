"""P9 structures_shared_kits and P6 structures_laylight geometry (structures_assets G1-b, 2026-11-06).

P9: Kit_SteelShelving (S3-12 file mezzanine, S5-08 dock), the access floor (Kit_AccessFloorPanel_600, Kit_AccessFloor_Cell_3m,
Kit_AccessFloorPedestal, Kit_AccessFloor_EdgeTrim_3m, Kit_AccessFloor_RampNosing_1200; S3-07, S4-05), Kit_AccessGrate_600
(S6-12, moulded FRP grating over the balance tank), Kit_CableWhips (S3-02, a missing ceiling tile's plenum).
P6: Kit_Laylight_Module (+ _B, _C, _DeadRow; S4-08, S2-02) and Kit_LightWell_Round (S6-13).
Period basis: 20_research.md F12 (luminous ceilings: T-bar grid, prismatic diffusers, lamp rows faintly through, edges
darker, panels differing), C.5 props (62 / 63 specs: 7 ft open steel shelving under a 2.5 m gallery; 24 in access floor
panels with a laminate top and a dark edge band on pedestals; 1-1/2 in moulded FRP grating; flexible-conduit lighting
whips over a lay-in grid). UNITY frame for every kit (origins as the journal names them). ASCII only, no brands.
"""

import math
import random

import _run_b_common as B
import _struct_common as S

PUTTY, BLACK, GALV, PAINT = "Prop_SteelPutty", "Prop_PlasticBlack", "RunB_GalvSteel", "Painted_Metal"
FLOOR_TOP, GRATE, FLEX, GREY, DARK = "Prop_AccessFloor", "Prop_FRPGrating", "Prop_FlexConduit", "Prop_PlasticGrey", "Plenum_Dark"
ALU, DIFF = "Trans_AluminiumSatin", "Prop_LaylightDiffuser"


def _uvoff(obj, seed):
    rnd = random.Random(seed)
    obj["fr_uv_offset"] = (rnd.uniform(0.0, 7.0), rnd.uniform(0.0, 7.0))
    return obj


# ============================================================================ Kit_SteelShelving
SH_W, SH_D, SH_H = 0.914, 0.457, 2.134
SH_SHELVES = (0.076, 0.483, 0.889, 1.295, 1.702, 2.108)
POST_LEG, POST_T = 0.038, 0.0019
HOLE_PITCH = 0.0381


def _post(kit, x, z, sx, sz, lv):
    """An angle post at corner (x, z): one leg on the face whose outward normal is (0, sz) (front / back), one on (sx, 0)."""
    h = SH_H
    # leg on the front / back face: spans x inward from the corner, thickness along z
    S.box(kit, (POST_LEG, h, POST_T), (x - sx * POST_LEG / 2, h / 2, z - sz * POST_T / 2), PUTTY, lv, w=0.0006, name="post leg")
    S.box(kit, (POST_T, h, POST_LEG), (x - sx * POST_T / 2, h / 2, z - sz * POST_LEG / 2), PUTTY, lv, w=0.0006, name="post leg")
    if lv > 0:
        return
    # the punched holes up both legs (1-1/2 in pitch), drawn as dark slots 0.3 mm proud of the leg's outer face
    m = S.Mesh()
    y = 0.040
    while y < h - 0.030:
        # front / back leg: hole centred 19 mm in from the corner
        cx = x - sx * 0.022
        zf = z + sz * 0.0003
        a, b = (cx - 0.0035, y - 0.0055), (cx + 0.0035, y + 0.0055)
        if sz > 0:
            m.quad((a[0], a[1], zf), (b[0], a[1], zf), (b[0], b[1], zf), (a[0], b[1], zf), (0, 0), (1, 0), (1, 1), (0, 1))
        else:
            m.quad((a[0], a[1], zf), (a[0], b[1], zf), (b[0], b[1], zf), (b[0], a[1], zf), (0, 0), (0, 1), (1, 1), (1, 0))
        cz = z - sz * 0.022
        xf = x + sx * 0.0003
        a, b = (cz - 0.0035, y - 0.0055), (cz + 0.0035, y + 0.0055)
        if sx > 0:
            m.quad((xf, a[1], a[0]), (xf, b[1], a[0]), (xf, b[1], b[0]), (xf, a[1], b[0]), (0, 0), (0, 1), (1, 1), (1, 0))
        else:
            m.quad((xf, a[1], a[0]), (xf, a[1], b[0]), (xf, b[1], b[0]), (xf, b[1], a[0]), (0, 0), (1, 0), (1, 1), (0, 1))
        y += HOLE_PITCH
    m.part(kit, "post holes", DARK)


def _shelf(kit, y, lv, seed):
    """A 22 ga pan shelf: the deck and its four 32 mm flanges (open underneath), resting inside the posts."""
    w, d = SH_W - 0.004, SH_D - 0.004
    t = 0.0012
    fl = 0.032
    o = S.box(kit, (w, t, d), (0.0, y, 0.0), PUTTY, lv, w=0.0004, name="shelf deck")
    _uvoff(o, seed)
    if lv < 2:
        for sz in (-1, 1):
            S.box(kit, (w, fl, t), (0.0, y - fl / 2, sz * (d / 2 - t / 2)), PUTTY, lv, w=0.0004, name="shelf flange")
            if lv == 0:
                # the front and back hems (a 6 mm return under the flange)
                S.box(kit, (w - 0.01, t, 0.006), (0.0, y - fl + t / 2, sz * (d / 2 - 0.003 - t)), PUTTY, lv, w=0.0, name="shelf hem")
        for sx in (-1, 1):
            S.box(kit, (t, fl, d - 0.004), (sx * (w / 2 - t / 2), y - fl / 2, 0.0), PUTTY, lv, w=0.0004, name="shelf flange")
    else:
        S.box(kit, (w, fl, d), (0.0, y - fl / 2, 0.0), PUTTY, lv, w=0.0, name="shelf block")
    if lv == 0:
        # two nut-and-bolt heads at each corner on the posts' outer faces
        for sx in (-1, 1):
            for sz in (-1, 1):
                for dy in (-0.010, -0.024):
                    S.screw_head(kit, 0.0042, (sx * (SH_W / 2 - 0.022), y + dy, sz * (SH_D / 2 + 0.0002)), "+z" if sz > 0 else "-z", PUTTY, 1, name="shelf bolt", dome=0.4)


def steel_shelving(kit, lv):
    hx, hz = SH_W / 2, SH_D / 2
    for sx in (-1, 1):
        for sz in (-1, 1):
            _post(kit, sx * hx, sz * hz, sx, sz, lv)
            if lv < 2:
                # a black plastic foot under each post
                S.box(kit, (0.040, 0.006, 0.040), (sx * (hx - 0.019), 0.003, sz * (hz - 0.019)), BLACK, lv, w=0.0008, name="foot")
    for i, y in enumerate(SH_SHELVES):
        _shelf(kit, y, lv, 11 + i)
    if lv < 2:
        # sway braces: flat straps crossed on the back and both ends (bolted at the posts)
        strap_w, strap_t = 0.025, 0.0016
        y0, y1 = 0.10, SH_H - 0.10
        for k, (p0, p1) in enumerate((((-hx + 0.02, y0, -hz - 0.001), (hx - 0.02, y1, -hz - 0.001)), ((hx - 0.02, y0, -hz - 0.0028), (-hx + 0.02, y1, -hz - 0.0028)))):
            _strap(kit, p0, p1, strap_w, strap_t, (0.0, 0.0, -1.0), lv)
        for sx in (-1, 1):
            for k, (za, zb) in enumerate(((-hz + 0.02, hz - 0.02), (hz - 0.02, -hz + 0.02))):
                xo = sx * (hx + 0.001 + k * 0.0018)
                _strap(kit, (xo, y0, za), (xo, y1, zb), strap_w, strap_t, (float(sx), 0.0, 0.0), lv)


def _strap(kit, p0, p1, w, t, outward, lv):
    """A flat strap between two Unity points, lying in the plane whose normal is the given outward Unity vector."""
    import mathutils
    a, b = mathutils.Vector(p0), mathutils.Vector(p1)
    d = b - a
    L = d.length
    n = mathutils.Vector(outward).normalized()
    side = d.normalized().cross(n).normalized() * (w / 2)
    off = n * (t / 2)
    m = S.Mesh()
    c = [tuple(v) for v in (a - side - off, a + side - off, b + side - off, b - side - off, a - side + off, a + side + off, b + side + off, b - side + off)]
    m.quad(c[4], c[5], c[6], c[7], (0, 0), (w, 0), (w, L), (0, L))
    m.quad(c[0], c[3], c[2], c[1], (0, 0), (0, L), (w, L), (w, 0))
    m.quad(c[1], c[2], c[6], c[5], (0, 0), (L, 0), (L, t), (0, t))
    m.quad(c[0], c[4], c[7], c[3], (0, 0), (0, t), (L, t), (L, 0))
    m.part(kit, "sway strap", PUTTY)
    if lv == 0:
        ax = ("+" if (n.x + n.y + n.z) > 0 else "-") + ("x" if abs(n.x) > 0.5 else "z")
        for q in (a + d.normalized() * 0.012, b - d.normalized() * 0.012):
            S.screw_head(kit, 0.0045, tuple(q + n * (t / 2 + 0.0001)), ax, PUTTY, lv, name="strap bolt", dome=0.4)


# ============================================================================ access floor
PANEL, PANEL_T, GAP = 0.600, 0.032, 0.003


def _panel_uv(obj, cx, cz, cell):
    """P8's Prop_AccessFloor (G1-a) draws 2 x 2 panels of 0.6 m, each with its PVC edge, in one 1.2 m tile (UV metres,
    _TileSize 1.2). A panel top takes one of the four cells (cell = (k, m), 0 or 1 each), centred on it, so the drawn edge
    meets the panel's own edge. kitlib's top projection is u = -x, v = -z of Unity space: centre (cx, cz) lands on
    (0.3 + 0.6 k, 0.3 + 0.6 m)."""
    obj["fr_uv_offset"] = (cx + 0.3 + 0.6 * cell[0], cz + 0.3 + 0.6 * cell[1])
    return obj


def _panel(kit, cx, cz, lv, cell, lift=0.0, underside=False):
    """One lay-in panel: the laminate top over a steel-encased core, its dark edge band, the top at y 0 (+ lift)."""
    s = PANEL - GAP
    o = S.box(kit, (s, 0.0016, s), (cx, -0.0008 + lift, cz), FLOOR_TOP, lv, w=0.0006, name="panel top")
    _panel_uv(o, cx, cz, cell)
    if lv < 2:
        # edge band: 2 mm black PVC round the core, its top 1.6 mm under the laminate face
        S.box(kit, (s, PANEL_T - 0.0016, s), (cx, -0.0016 - (PANEL_T - 0.0016) / 2 + lift, cz), BLACK, lv, w=0.0004, name="panel band")
    if underside:
        S.box(kit, (s - 0.006, 0.002, s - 0.006), (cx, -PANEL_T + 0.001 + lift, cz), GALV, lv, w=0.0004, name="panel pan")


def access_panel(kit, lv):
    _panel(kit, 0.0, 0.0, lv, (1, 0), underside=True)
    if lv == 0:
        # the pan's four corner seats where the pedestal heads bear
        for sx in (-1, 1):
            for sz in (-1, 1):
                S.box(kit, (0.050, 0.0012, 0.050), (sx * 0.272, -PANEL_T - 0.0005, sz * 0.272), GALV, lv, w=0.0003, name="corner seat")


def access_cell(kit, lv):
    """A 3 x 3 m cell of 5 x 5 panels, each on one of P8's four panel cells (never the same as its left or near neighbour),
    two a hair proud (lippage)."""
    rnd = random.Random(1990)
    proud = {(1, 3), (3, 0)}
    cells = [(0, 0), (1, 0), (0, 1), (1, 1)]
    chosen = {}
    for i in range(5):
        for j in range(5):
            cx, cz = -1.2 + 0.6 * i, -1.2 + 0.6 * j
            lift = 0.0006 if (i, j) in proud else 0.0
            s = PANEL - GAP
            o = S.box(kit, (s, 0.0016, s), (cx, -0.0008 + lift, cz), FLOOR_TOP, lv, w=0.0006 if lv == 0 else 0.0, name="panel top")
            near = (chosen.get((i - 1, j)), chosen.get((i, j - 1)))
            pick = [c for c in cells if c not in near]
            chosen[(i, j)] = pick[rnd.randrange(len(pick))]
            _panel_uv(o, cx, cz, chosen[(i, j)])
    # the dark joint plane under the laminate faces (the edge bands seen through the 3 mm joints)
    S.box(kit, (3.0, 0.004, 3.0), (0.0, -0.0036, 0.0), BLACK, lv, w=0.0, name="joint shadow")
    if lv == 0:
        rnd.random()


def pedestal(kit, lv):
    """A pedestal for a 0.30 m floor: base plate on the slab (y -0.30), tube, threaded stud and lock nut, head plate (y -0.032)."""
    base_y, head_y = -0.300, -PANEL_T
    S.box(kit, (0.100, 0.003, 0.100), (0.0, base_y + 0.0015, 0.0), GALV, lv, w=0.0006, name="base plate")
    S.cyl_y(kit, 0.011, 0.170, (0.0, base_y + 0.003 + 0.085, 0.0), GALV, lv, w=0.0006, name="pedestal tube")
    S.cyl_y(kit, 0.008, 0.080, (0.0, base_y + 0.173 + 0.040, 0.0), GALV, lv, w=0.0004, name="threaded stud")
    if lv < 2:
        S.hex_nut(kit, 0.024, 0.010, (0.0, base_y + 0.178, 0.0), "y", GALV, lv, name="lock nut")
    S.box(kit, (0.075, 0.004, 0.075), (0.0, head_y - 0.002, 0.0), GALV, lv, w=0.0006, name="head plate")
    if lv == 0:
        S.box(kit, (0.070, 0.002, 0.070), (0.0, head_y - 0.0005, 0.0), BLACK, lv, w=0.0, name="head gasket")


def edge_trim(kit, lv):
    """The exposed edge of a 0.30 m raised floor, 3 m along X, face +Z at z 0: an aluminium nosing angle over the panel edge
    and a painted steel fascia below it with joints every 1.2 m and screws."""
    L = 3.0
    # the nosing's top flange lies ON the edge panels (3 mm proud, as the angle is fitted): flush with them it would share
    # their face and flicker (job 5 / 6 LOD2 frames)
    S.box(kit, (L, 0.003, 0.045), (0.0, 0.0015, -0.0225 + 0.003), ALU, lv, w=0.0008, name="nosing top")
    S.box(kit, (L, 0.043, 0.003), (0.0, -0.0185, 0.0015), ALU, lv, w=0.0008, name="nosing leg")
    if lv == 0:
        # the abrasive insert in the nosing's top
        S.box(kit, (L - 0.004, 0.0006, 0.014), (0.0, 0.0033, -0.012), BLACK, lv, w=0.0, name="nosing insert")
    for k in range(3):
        x0 = -L / 2 + 1.0 * k
        S.box(kit, (0.998, 0.258, 0.0016), (x0 + 0.5, -0.040 - 0.129, -0.0010), PAINT, lv, w=0.0004, name="fascia plate")
        if lv == 0:
            for xs in (0.03, 0.5, 0.97):
                for ys in (-0.060, -0.280):
                    S.screw_head(kit, 0.0035, (x0 + xs, ys, -0.0002 + 0.0001), "+z", PAINT, lv, name="fascia screw", dome=0.35)


def ramp_nosing(kit, lv):
    """The ramp's top: a 1.2 m aluminium transition nosing, a tapered extrusion with an abrasive strip, ramp toward +Z."""
    L = 1.2
    prof = [(-0.020, 0.0), (0.0, 0.0012), (0.045, 0.0), (0.045, -0.006), (-0.020, -0.006)]
    ol = [(-z, y) for (z, y) in [(p[0], p[1]) for p in prof]]
    bw, bs = S.bev(0.0008, lv)
    kit.extrude(ol, L, S.ub((0.0, 0.0, 0.0)), ALU, plane="yz", bevel=bw, segments=bs, name="ramp nosing")
    if lv < 2:
        S.box(kit, (L - 0.006, 0.0006, 0.016), (0.0, 0.0011, 0.010), BLACK, lv, w=0.0, name="abrasive strip")
    if lv == 0:
        for xs in (-0.55, -0.18, 0.18, 0.55):
            S.screw_head(kit, 0.0030, (xs, 0.0006, 0.032), "+y", ALU, lv, name="nosing screw", dome=0.1)


# ============================================================================ Kit_AccessGrate_600
def access_grate(kit, lv):
    """Moulded FRP grating, 600 x 600 x 38, 16 x 16 openings: bars 7 mm at the top, 5 mm at the bottom (the mould's draft);
    a gritted top in the material. Origin the top centre."""
    n = 16
    s = 0.600
    p = s / n
    d = 0.038
    m = S.Mesh()
    bars = {0: n + 1, 1: n + 1, 2: n // 2 + 1}[lv]
    wt, wb = 0.007, 0.005
    for axis in (0, 1):
        for k in range(bars):
            c = -s / 2 + (s * k / (bars - 1))
            w_top = wt * (1.6 if k in (0, bars - 1) else 1.0)
            w_bot = wb * (1.6 if k in (0, bars - 1) else 1.0)
            a0, a1 = -s / 2, s / 2
            def P(along, across, y):
                return (along, y, across) if axis == 0 else (across, y, along)
            top_l, top_r = c - w_top / 2, c + w_top / 2
            bot_l, bot_r = c - w_bot / 2, c + w_bot / 2
            # top (at LOD0 a flat with a 1 mm chamfer each side: the mould's radiused bar top)
            ch = 0.001 if lv == 0 else 0.0
            tl, tr = top_l + ch, top_r - ch
            if axis == 0:
                m.quad(P(a0, tl, 0.0), P(a0, tr, 0.0), P(a1, tr, 0.0), P(a1, tl, 0.0), (a0, tl), (a0, tr), (a1, tr), (a1, tl))
            else:
                m.quad(P(a0, tl, 0.0), P(a1, tl, 0.0), P(a1, tr, 0.0), P(a0, tr, 0.0), (a0, tl), (a1, tl), (a1, tr), (a0, tr))
            if ch > 0.0:
                for (e0, e1) in ((top_l, tl), (tr, top_r)):
                    y0e, y1e = (-ch, 0.0) if e0 == top_l else (0.0, -ch)
                    if axis == 0:
                        m.quad(P(a0, e0, y0e), P(a0, e1, y1e), P(a1, e1, y1e), P(a1, e0, y0e), (a0, e0), (a0, e1), (a1, e1), (a1, e0))
                    else:
                        m.quad(P(a0, e0, y0e), P(a1, e0, y0e), P(a1, e1, y1e), P(a0, e1, y1e), (a0, e0), (a1, e0), (a1, e1), (a0, e1))
            top_y0 = -ch
            if lv == 2:
                continue
            # the two drafted sides
            for (tt, bb, sgn) in ((top_l, bot_l, -1), (top_r, bot_r, 1)):
                q = [P(a0, tt, top_y0), P(a1, tt, top_y0), P(a1, bb, -d), P(a0, bb, -d)]
                if (sgn > 0) != (axis == 0):
                    m.quad(q[0], q[1], q[2], q[3], (a0, 0), (a1, 0), (a1, d), (a0, d))
                else:
                    m.quad(q[0], q[3], q[2], q[1], (a0, 0), (a0, d), (a1, d), (a1, 0))
            if lv == 0:
                # bottom
                if axis == 0:
                    m.quad(P(a0, bot_l, -d), P(a1, bot_l, -d), P(a1, bot_r, -d), P(a0, bot_r, -d), (a0, bot_l), (a1, bot_l), (a1, bot_r), (a0, bot_r))
                else:
                    m.quad(P(a0, bot_l, -d), P(a0, bot_r, -d), P(a1, bot_r, -d), P(a1, bot_l, -d), (a0, bot_l), (a0, bot_r), (a1, bot_r), (a1, bot_l))
    m.part(kit, "grating", GRATE, merge=False)


# ============================================================================ Kit_CableWhips
def _catenary(p0, p1, sag, n, side=(0.0, 0.0, 0.0)):
    out = []
    for k in range(n + 1):
        t = k / n
        x = p0[0] + (p1[0] - p0[0]) * t + side[0] * math.sin(math.pi * t)
        y = p0[1] + (p1[1] - p0[1]) * t - sag * math.sin(math.pi * t) ** 1.3
        z = p0[2] + (p1[2] - p0[2]) * t + side[2] * math.sin(math.pi * t)
        out.append((x, y, z))
    return out


def cable_whips(kit, lv):
    """A missing tile's plenum: a junction box on the deck, two 3/8 in flexible conduit whips looping down through the
    opening and back up to the troffers beside it, two grey low-voltage cables, one cut loose. Origin the ceiling plane at
    the opening's centre."""
    # junction box on the deck 0.55 m up
    S.box(kit, (0.102, 0.040, 0.102), (0.05, 0.55, -0.10), GALV, lv, w=0.0008, name="junction box")
    if lv < 2:
        S.box(kit, (0.108, 0.003, 0.108), (0.05, 0.5285, -0.10), GALV, lv, w=0.0004, name="box cover")
    rflex = 0.0085
    n = {0: 40, 1: 16, 2: 8}[lv]
    sides = {0: 12, 1: 8, 2: 5}[lv]
    # each whip leaves the box, sags through the opening and climbs back into the plenum inside the opening's edge (it never
    # crosses a neighbouring tile), on to its troffer over the grid (hidden there)
    whips = [
        ((0.03, 0.53, -0.08), (-0.25, 0.16, 0.06), 0.72, (0.0, 0.0, 0.10)),
        ((0.07, 0.53, -0.12), (0.10, 0.16, 0.26), 0.58, (0.14, 0.0, 0.0)),
    ]
    for (p0, p1, sag, side) in whips:
        path = _catenary(p0, p1, sag, n, side)
        m = S.Mesh()
        S.sweep_section(m, path, S.circle(rflex, sides), cap0=True, cap1=True)
        m.part(kit, "flex whip", FLEX)
        if lv < 2:
            # the connectors at each end (a die-cast squeeze connector)
            for (q, nb) in ((path[0], path[1]), (path[-1], path[-2])):
                d = (q[0] - nb[0], q[1] - nb[1], q[2] - nb[2])
                L = math.sqrt(d[0] ** 2 + d[1] ** 2 + d[2] ** 2) or 1.0
                d = (d[0] / L, d[1] / L, d[2] / L)
                a = (q[0] - d[0] * 0.02, q[1] - d[1] * 0.02, q[2] - d[2] * 0.02)
                S.tube(kit, "whip connector", [a, q], rflex * 1.45, GALV, lv, lo=8)
    cables = [
        ((-0.10, 0.50, 0.10), (0.20, -0.05, -0.18), 0.62, (0.0, 0.0, 0.08), True),
        ((-0.05, 0.52, -0.25), (0.26, 0.14, -0.20), 0.46, (0.0, 0.0, -0.06), False),
    ]
    rc = 0.003
    for (p0, p1, sag, side, loose) in cables:
        path = _catenary(p0, p1, sag, n, side)
        if loose:
            # the cut end hangs: drop the last third straight down
            k0 = int(len(path) * 0.7)
            last = path[k0]
            path = path[:k0 + 1] + [(last[0] + 0.01, last[1] - 0.06 * t, last[2]) for t in (1, 2, 3)]
        m = S.Mesh()
        S.sweep_section(m, path, S.circle(rc, {0: 8, 1: 6, 2: 4}[lv]), cap0=True, cap1=True)
        m.part(kit, "lv cable", GREY)


# ============================================================================ P6 laylight
CELLS_A = [[0, 3, 7, 1, 9], [5, 11, 2, 6, 4], [8, 1, 12, 3, 10], [2, 6, 4, 0, 7], [9, 5, 1, 8, 3]]
CELLS_B = [[4, 8, 1, 10, 2], [7, 0, 9, 3, 6], [1, 5, 2, 11, 8], [10, 3, 6, 4, 0], [2, 9, 0, 7, 12]]
CELLS_C = [[6, 1, 10, 4, 3], [2, 9, 5, 0, 11], [7, 3, 8, 12, 1], [0, 11, 2, 6, 9], [5, 4, 7, 1, 8]]
ATLAS_TILE = 2.4          # Prop_LaylightDiffuser: 4 x 4 panels of 0.6 m in one 2.4 m tile (_TileSize 2.4)
TBAR_W, TBAR_H = 0.024, 0.038


def _cell_uv(cell, u, v):
    cx, cy = cell % 4, cell // 4
    return (cx * 0.6 + u, cy * 0.6 + v)


def laylight(kit, lv, cells, dead_row=None):
    """One 3 x 3 m bay: 25 prismatic panels (the lens: slot Prop_LaylightDiffuser) on a 15/16 in T-bar grid at 0.6 m; the bay's
    own edges carry half a T-bar so neighbouring bays meet as one; origin the bay centre, the T-bar face at y 0."""
    half = 1.5
    # ---- the panels (one mesh): their faces 1 mm over the T-bar face, 3 mm thick edges
    m = S.Mesh()
    pin = 0.012 - 0.001       # the panel's visible face ends at the T-bar flange (12 mm each side of the line)
    for i in range(5):
        for j in range(5):
            cell = cells[j][i]
            if dead_row is not None and j == dead_row[0]:
                cell = dead_row[1]
            x0, x1 = -half + 0.6 * i + pin, -half + 0.6 * (i + 1) - pin
            z0, z1 = -half + 0.6 * j + pin, -half + 0.6 * (j + 1) - pin
            y = 0.001
            ua, va = _cell_uv(cell, pin, pin)
            ub_, vb = _cell_uv(cell, 0.6 - pin, 0.6 - pin)
            # face down (-Y): seen from below
            m.quad((x0, y, z0), (x1, y, z0), (x1, y, z1), (x0, y, z1), (ua, va), (ub_, va), (ub_, vb), (ua, vb))
    m.part(kit, "laylight panels", DIFF, merge=False)
    # ---- the T-bar grid: face strips at y 0 (24 mm, half at the bay edges), webs up to 38 mm and a bulb
    g = S.Mesh()
    lines = [-half + 0.6 * k for k in range(6)]
    for axis in (0, 1):
        for k, c in enumerate(lines):
            edge = k in (0, 5)
            # the bay's own edges: half a flange INSIDE the bay, so two bays meet as one 24 mm T-bar and the laylight's outer
            # edge ends on a half flange like a wall angle (the panels' edges always sit on a flange)
            w0 = 0.0 if (edge and k == 0) else -TBAR_W / 2
            w1 = 0.0 if (edge and k == 5) else TBAR_W / 2
            a0, a1 = -half, half
            def P(along, across, y):
                return (along, y, c + across) if axis == 0 else (c + across, y, along)
            # face (down)
            if axis == 0:
                g.quad(P(a0, w0, 0.0), P(a1, w0, 0.0), P(a1, w1, 0.0), P(a0, w1, 0.0), (a0, w0), (a1, w0), (a1, w1), (a0, w1))
            else:
                g.quad(P(a0, w0, 0.0), P(a0, w1, 0.0), P(a1, w1, 0.0), P(a1, w0, 0.0), (a0, w0), (a0, w1), (a1, w1), (a1, w0))
            if lv == 2:
                continue
            # the flange edges (1 mm) and the web (both faces, 1 mm apart) to the bulb
            for (wx, sgn) in ((w0, -1), (w1, 1)):
                if abs(wx) < 1e-6:
                    continue
                q = [P(a0, wx, 0.0), P(a1, wx, 0.0), P(a1, wx, 0.0012), P(a0, wx, 0.0012)]
                if (sgn > 0) != (axis == 0):
                    g.quad(q[0], q[3], q[2], q[1], (a0, 0), (a0, 0.0012), (a1, 0.0012), (a1, 0))
                else:
                    g.quad(q[0], q[1], q[2], q[3], (a0, 0), (a1, 0), (a1, 0.0012), (a0, 0.0012))
            for (wx, sgn) in ((-0.0005, -1), (0.0005, 1)):
                # an edge keeps only the web face that looks into its own bay (the neighbour's web stands 1 mm away)
                if (edge and k == 0 and sgn < 0) or (edge and k == 5 and sgn > 0):
                    continue
                q = [P(a0, wx, 0.0012), P(a1, wx, 0.0012), P(a1, wx, TBAR_H), P(a0, wx, TBAR_H)]
                if (sgn > 0) != (axis == 0):
                    g.quad(q[0], q[3], q[2], q[1], (a0, 0), (a0, TBAR_H), (a1, TBAR_H), (a1, 0))
                else:
                    g.quad(q[0], q[1], q[2], q[3], (a0, 0), (a1, 0), (a1, TBAR_H), (a0, TBAR_H))
    g.part(kit, "t-bar grid", PAINT, merge=False)
    if lv == 0:
        # the bulbs at the webs' tops and a few hold-down clips on the panels
        for axis in (0, 1):
            for k, c in enumerate(lines):
                if k in (0, 5):
                    continue
                if axis == 0:
                    S.box(kit, (3.0, 0.006, 0.006), (0.0, TBAR_H, c), PAINT, lv, w=0.001, name="t-bar bulb")
                else:
                    S.box(kit, (0.006, 0.006, 3.0), (c, TBAR_H, 0.0), PAINT, lv, w=0.001, name="t-bar bulb")


def light_well(kit, lv):
    """Kit_LightWell_Round: the oculus diffuser (D 1.20, a prismatic disc, the lens) in a white enamel frame ring with eight
    spring clips, the lamp's cavity can over it; origin the diffuser centre at the well's top, the disc facing -Y."""
    rd, rf = 0.600, 0.650
    n = {0: 96, 1: 48, 2: 24}[lv]
    m = S.Mesh()
    cell = 12
    for k in range(n):
        a0, a1 = 2 * math.pi * k / n, 2 * math.pi * (k + 1) / n
        p = lambda a, r: (r * math.cos(a), 0.002, r * math.sin(a))
        uv = lambda a, r: _cell_uv(cell, 0.3 + 0.3 * r / rd * math.cos(a), 0.3 + 0.3 * r / rd * math.sin(a))
        m.tri((0.0, 0.002, 0.0), p(a0, rd), p(a1, rd), uv(0, 0), uv(a0, rd), uv(a1, rd))
    m.part(kit, "well diffuser", DIFF, merge=False)
    prof = [(rd - 0.008, 0.002), (rd - 0.008, -0.004), (rd + 0.004, -0.012), (rf - 0.004, -0.012), (rf, -0.008), (rf, 0.002)]
    if lv >= 1:
        prof = [(rd - 0.008, 0.002), (rd - 0.008, -0.012), (rf, -0.012), (rf, 0.002)]
    S.lathe_y(kit, prof, (0.0, 0.0, 0.0), PAINT, lv, name="well frame", close_top=False, close_bottom=False)
    if lv < 2:
        S.lathe_y(kit, [(rd + 0.002, 0.004), (rd + 0.002, 0.25), (0.0, 0.25)], (0.0, 0.0, 0.0), PAINT, lv, name="cavity can", close_bottom=False)
    if lv == 0:
        for k in range(8):
            a = 2 * math.pi * (k + 0.5) / 8
            S.box(kit, (0.030, 0.004, 0.012), ((rd - 0.006) * math.cos(a), -0.012, (rd - 0.006) * math.sin(a)), PAINT, lv, w=0.0008, name="spring clip")
