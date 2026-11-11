"""SP5's services, the riser shaft, the E8 cage, the closet, the hose cabinet and the time clock, and SP7's end kits (structures G4-b,
line visual (game visual), 2026-11-10; package structures_run_service, 30_plan.md P20; research/level_space/63_furniture_run_pool.md 3.1
RN-SPINE, 3.2 RN-JAN, 6.1; 62_furniture_l0_office.md (Kit_WireCagePartition, Kit_TempLightString, Kit_TIScrap); 20_research.md C.4.4,
C.4.5, C.5.1; sources LS99, LS103, LS117, LS119 and the period standards cited at each kit).

All in UNITY space (x, y, z; metres), converted by _struct_common.ub(); hand-built LOD0 / LOD1 / LOD2 (B.levels). Hero rule as G1-b / G2 /
G4-a: curves at a 0.2 mm chord at LOD0, exposed edges bevelled at LOD0, read at 0.35 m. Period 1988-91, generic: no text, label, brand or
logo (the cards, plates and cabinet lettering are the graphic line (pingmian shijue)'s and wait on Red's 11-10 hold). Origins: floor kits stand on y 0 centred on
x = z = 0, front +Z; wall kits sit on the wall plane z 0 (+Z out of the wall, +Y up, y 0 at the floor unless said); runs (duct, pipe)
start at their origin on the axis and run along +Z (horizontal) or +Y (risers). ASCII only.

  Kit_Duct_3m / _1500 / _0500   24 x 12 in galvanised supply duct, S-slip and drive joints every 1.5 m, cross-broken panels
  Kit_Duct_Slope8               the 1:8 pitch change (7.125 deg) as a gored fitting
  Kit_DuctHanger                strap hangers (1 in x 18 ga) each side to the soffit 0.15 m over the duct
  Kit_DuctWallCollar            the fire damper sleeve's retaining angles where the duct meets a rated wall
  Kit_SprinklerMain_Slope8      the 4 in main's pitch change in a flexible grooved coupling
  Kit_CableTray_Slope8          the ladder tray's hinged vertical splice plates
  Kit_RiserPipe_3m (+ _Galv2)   4 in sch 10 roll-grooved riser with its coupling; 2 in galvanised threaded pipe
  Kit_RiserPipe_Tee             the grooved tee the riser rises from
  Kit_RiserClamp                two-bolt riser clamp bearing on a beam
  Kit_PipeClampWall             strut channel on the wall with two 2 in pipe clamps
  Kit_CagedLadder               fixed steel ladder with its cage (OSHA 1910.27), 4.50 m
  (the cage partition, hose cabinet, time clock, closet kits and SP7's kits follow below)
"""

import math
import random

import bmesh
import bpy
from mathutils import Vector

import kitlib
import _run_b_common as B
import _struct_common as S
import _struct_dock as D

# ----------------------------------------------------------------------------- slots (Unity materials: Resources/Surfaces/<slot>.mat)
GALV = "Prop_DoorSlatGalv"        # G4-a (structures_dock): galvanised sheet, spangle and roll streaks: duct, strut channel
HDG = "RunB_GalvSteel"            # main: hot-dip galvanised (dull): ladder, straps, tray splice plates
RED = "RunB_SprinklerRed"         # main: the sprinkler red
PAINT = "Painted_Metal"           # main: field-painted enamel steel
WIRE = "Prop_WireMesh"            # main: 10 ga woven wire, 1-1/2 in diamond, alpha cut
STEEL = "Prop_SteelBlack"         # main: black steel hardware
ZINC = "Prop_StainlessSatin"      # main: bright satin metal for zinc-plated hardware and stainless trim
CHROME = "Prop_Chrome"
BRASS = "Prop_Brass"
CANVAS = "Prop_Canvas"
GLASS = "Prop_Glass"
WHITE = "RunB_CabinetWhite"
FIRE = "RunB_FireRed"
BEIGE = "Prop_SteelBeige"
PWHITE = "Prop_PlasticWhite"
PBLACK = "Prop_PlasticBlack"
PRED = "Prop_PlasticRed"
PAPER = "Prop_Paper"                # G4 render 11-11: blank card stock (Prop_StationPaper printed digits on the cards; Red 11-10 11:11 no lettering)
PLY = "Prop_Plywood"
STONE = "Prop_MoldedStone"         # NEW (this package): light grey molded stone, marble-chip speckle, satin (main's Prop_Ceramic is a brown glaze)
RUBBER = "Prop_Rubber"
OAK = "Prop_WoodOak"
PUTTY = "Prop_SteelPutty"
YELLOW = "RunB_BucketYellow"
LIT = "Prop_BulbLit"
WOOL = "Prop_Fireproofing"
PRIMER = "Wall_Primer"
CARD = "Prop_Cardboard"
TAPE = "Prop_TapeBeige"
ALU = "Prop_Aluminium"

for _slot, _c, _r, _m in ((GALV, (0.60, 0.61, 0.60), 0.45, 0.85), (HDG, (0.60, 0.61, 0.60), 0.42, 0.75), (RED, (0.36, 0.02, 0.012), 0.52, 0.0),
                          (PAINT, (0.55, 0.55, 0.52), 0.5, 0.0), (WIRE, (0.55, 0.56, 0.56), 0.42, 0.55), (STEEL, (0.10, 0.10, 0.10), 0.5, 0.3),
                          (ZINC, (0.62, 0.62, 0.61), 0.45, 0.45), (CHROME, (0.85, 0.85, 0.85), 0.85, 1.0), (BRASS, (0.62, 0.47, 0.22), 0.55, 1.0),
                          (CANVAS, (0.74, 0.70, 0.60), 0.85, 0.0), (GLASS, (0.15, 0.17, 0.16), 0.94, 0.0), (WHITE, (0.78, 0.76, 0.70), 0.45, 0.0),
                          (FIRE, (0.43, 0.012, 0.009), 0.5, 0.0), (BEIGE, (0.62, 0.58, 0.48), 0.55, 0.0), (PWHITE, (0.85, 0.85, 0.82), 0.4, 0.0),
                          (PBLACK, (0.03, 0.03, 0.03), 0.4, 0.0), (PRED, (0.55, 0.04, 0.03), 0.4, 0.0), (PAPER, (0.86, 0.85, 0.80), 0.7, 0.0),
                          (PLY, (0.62, 0.50, 0.33), 0.75, 0.0), (STONE, (0.70, 0.70, 0.68), 0.55, 0.0), (RUBBER, (0.02, 0.02, 0.02), 0.15, 0.0),
                          (OAK, (0.55, 0.40, 0.25), 0.45, 0.0), (PUTTY, (0.62, 0.60, 0.55), 0.45, 0.0), (YELLOW, (0.69, 0.42, 0.007), 0.45, 0.0),
                          (LIT, (1.0, 0.85, 0.6), 0.2, 0.0), (WOOL, (0.55, 0.53, 0.48), 0.9, 0.0), (PRIMER, (0.80, 0.79, 0.76), 0.8, 0.0),
                          (CARD, (0.55, 0.42, 0.27), 0.85, 0.0), (TAPE, (0.62, 0.50, 0.33), 0.5, 0.0), (ALU, (0.66, 0.66, 0.66), 0.45, 1.0)):
    kitlib.register_slot(_slot, _c, _r, _m)

STAMP = "structures_assets G4-b 2026-11-10 (30_plan.md P20; 63 sections 3.1, 3.2, 6.1; 62)"
A8 = math.atan(1.0 / 8.0)          # the ramp's 1:8 = 7.125 deg


# ----------------------------------------------------------------------------- helpers in Unity space
def finish(kit, distances, budget, tags=()):
    B.lod_meta(kit, distances, budget)
    kit.meta["structures"] = STAMP
    if tags:
        kit.tag(*tags)


def V(p):
    return Vector(p)


def rx(p, a):
    """Rotate a Unity point about +X by a (rad): (0, 0, 1) -> (0, -sin a, cos a) for a > 0 (the duct falls)."""
    c, s = math.cos(a), math.sin(a)
    return (p[0], p[1] * c - p[2] * s, p[1] * s + p[2] * c)


def ry(p, a):
    """Rotate about +Y by a (rad): (0, 0, 1) -> (sin a, 0, cos a)."""
    c, s = math.cos(a), math.sin(a)
    return (p[0] * c + p[2] * s, p[1], -p[0] * s + p[2] * c)


def arcs(sec, closed=True):
    out = [0.0]
    n = len(sec)
    for i in range(1, n + (1 if closed else 0)):
        p, q = sec[i - 1], sec[i % n]
        out.append(out[-1] + math.hypot(q[0] - p[0], q[1] - p[1]))
    return out


def prism(m, sec, o, ex, ey, ez, z0, z1, caps=(True, True), closed=True, u0=0.0):
    """A 2-D section (CCW in the (ex, ey) plane, right-handed with ez) extruded along ez from z0 to z1 into Mesh m; metre UVs (u along
    ez, v round the section); quads wound out (D.qd), caps as n-gons."""
    O, X, Y, Z = V(o), V(ex), V(ey), V(ez)
    n = len(sec)
    ar = arcs(sec, closed)
    P0 = [O + X * a + Y * b + Z * z0 for (a, b) in sec]
    P1 = [O + X * a + Y * b + Z * z1 for (a, b) in sec]
    seg = n if closed else n - 1
    for i in range(seg):
        j = (i + 1) % n
        e = P0[j] - P0[i]
        want = e.cross(Z)
        D.qd(m, tuple(P0[i]), tuple(P0[j]), tuple(P1[j]), tuple(P1[i]), (u0 + z0, ar[i]), (u0 + z0, ar[i + 1]), (u0 + z1, ar[i + 1]),
             (u0 + z1, ar[i]), tuple(want))
    if closed and caps[1]:
        m.face(tuple(m.vert(tuple(p)) for p in P1), [(a, b) for (a, b) in sec])
    if closed and caps[0]:
        m.face(tuple(reversed([m.vert(tuple(p)) for p in P0])), [(a, b) for (a, b) in reversed(sec)])


def rrect(w, h, r, per):
    """Rounded rectangle, CCW, centred; per = points per 90 deg corner (0: sharp)."""
    if per <= 0 or r <= 0:
        hw, hh = w / 2.0, h / 2.0
        return [(hw, -hh), (hw, hh), (-hw, hh), (-hw, -hh)]
    return B.rrect(w, h, r, per)


def bar(m, a, b, w, t, up, slot_free=None):
    """A flat bar from a to b (Unity), w across (along 'up' x dir), t thick: a closed box prism into Mesh m."""
    A, Bv = V(a), V(b)
    d = Bv - A
    L = d.length
    z = d / L
    u = V(up)
    x = u.cross(z).normalized()
    y = z.cross(x).normalized()
    prism(m, [(t / 2, -w / 2), (t / 2, w / 2), (-t / 2, w / 2), (-t / 2, -w / 2)], tuple(A), tuple(x), tuple(y), tuple(z), 0.0, L)


def flat_quad(m, a, b, c, d, want, uv=None):
    uv = uv or ((0, 0), (1, 0), (1, 1), (0, 1))
    D.qd(m, a, b, c, d, uv[0], uv[1], uv[2], uv[3], want)


def hexnut(kit, centre, axis, af, h, slot, lv, name="nut"):
    if lv >= 2:
        return None
    return S.hex_nut(kit, af, h, centre, axis, slot, lv, name=name)


def screw(kit, centre, axis, r, slot, lv, name="screw"):
    if lv >= 2:
        return None
    return S.screw_head(kit, r, centre, axis, slot, lv, name=name)


def box(kit, size, centre, slot, lv, w=0.0012, name="box", rot=(0, 0, 0)):
    """S.box, except w <= 0 builds a sharp box at every level (B.bev puts 0.8 mm x 2 on every LOD0 box: ~90 tris for a sheet-metal band)."""
    if w > 0.0:
        return S.box(kit, size, centre, slot, lv, w=w, name=name, rot=rot)
    sz = (size[0], size[2], size[1])
    return kit.box(sz, S.ub(centre), slot, bevel=0.0, segments=0, rot=(rot[0], rot[2], -rot[1]), name=name)


def rcyl(kit, r, h, centre, axis, slot, verts, name="cyl"):
    """A sharp cylinder of a fixed segment count on a Unity axis ('x', 'y', 'z')."""
    rot = {"y": (0, 0, 0), "z": (90, 0, 0), "x": (0, 90, 0)}[axis]
    return kit.cylinder(r, h, S.ub(centre), slot, verts=verts, rot=rot, bevel=0.0, segments=0, name=name)


def stud(kit, centre, axis, r, h, slot, lv, name="screw"):
    """A cheap fastener head: an 8-sided unbevelled cylinder on a Unity axis ('x', 'y' or 'z'), for screws and pins seen from metres away
    (the hero rule's heads stay S.screw_head / S.hex_nut)."""
    if lv >= 1:
        return None
    rot = {"y": (0, 0, 0), "z": (90, 0, 0), "x": (0, 90, 0)}[axis]
    return kit.cylinder(r, h, S.ub(centre), slot, verts=8, rot=rot, bevel=0.0, segments=0, name=name)


def tube(kit, name, pts, r, slot, lv, lo=8, caps=True):
    return S.tube(kit, name, pts, r, slot, lv, cap0=caps, cap1=caps, lo=lo)


def ptube(kit, name, pts, r, slot, lv, n0=24, caps=True):
    """A tube of a fixed side count (n0 at LOD0, half at LOD1, a third at LOD2, never under 6): the view-only services in the shaft and on
    the soffit, seen from 2.5 m and more, where the hero chord (0.2 mm) would spend 60 sides on a 4 in pipe."""
    n = max(6, n0 if lv == 0 else (n0 // 2 if lv == 1 else n0 // 3))
    m = S.Mesh()
    S.sweep_section(m, pts, S.circle(r, n), cap0=caps, cap1=caps)
    return m.part(kit, name, slot)


# ============================================================================= Kit_Duct_* (24 x 12 in low-pressure supply duct)
DW, DH = 0.6096, 0.3048            # 24 x 12 in outside
DR = 0.0025                        # the brake's corner radius on 26 ga
XBREAK = 0.008                     # the cross-break rise at the panel centre (24 in faces: 1/4-3/8 in)
SEC_L = 1.5                        # a section (S-slip and drive at its +Z end)
JW = 0.025                         # joint band width


def _duct_outline(lv):
    per = 2 if lv == 0 else (1 if lv == 1 else 0)
    return rrect(DW, DH, DR, per)


def duct_skin(m, z0, z1, lv, xbreak=True):
    """One section's skin (open tube) from z0 to z1: the 12 in sides flat, the 24 in faces cross-broken (four facets to a centre raised
    XBREAK), the corners on the brake's radius; metre UVs (u along z, v round the section)."""
    hw, hh = DW / 2.0, DH / 2.0
    if lv >= 2:
        sec = [(hw, -hh), (hw, hh), (-hw, hh), (-hw, -hh)]
        prism(m, sec, (0, 0, 0), (1, 0, 0), (0, 1, 0), (0, 0, 1), z0, z1, caps=(False, False))
        return
    sec = _duct_outline(lv)
    ar = arcs(sec)
    n = len(sec)
    zc = (z0 + z1) / 2.0
    for i in range(n):
        j = (i + 1) % n
        (xa, ya), (xb, yb) = sec[i], sec[j]
        top = abs(ya - hh) < 1e-6 and abs(yb - hh) < 1e-6
        bot = abs(ya + hh) < 1e-6 and abs(yb + hh) < 1e-6
        a0, b0, a1, b1 = (xa, ya, z0), (xb, yb, z0), (xa, ya, z1), (xb, yb, z1)
        e = V(b0) - V(a0)
        want = e.cross(V((0, 0, 1)))
        if (top or bot) and xbreak:
            sgn = 1.0 if top else -1.0
            apex = (0.0, sgn * (hh + XBREAK), zc)
            va, vb = ar[i], ar[i + 1]
            vm = (va + vb) / 2.0
            D.tri(m, a0, b0, apex, (z0, va), (z0, vb), (zc, vm), tuple(want))
            D.tri(m, b0, b1, apex, (z0, vb), (z1, vb), (zc, vm), tuple(want))
            D.tri(m, b1, a1, apex, (z1, vb), (z1, va), (zc, vm), tuple(want))
            D.tri(m, a1, a0, apex, (z1, va), (z0, va), (zc, vm), tuple(want))
        else:
            D.qd(m, a0, b0, b1, a1, (z0, ar[i]), (z0, ar[i + 1]), (z1, ar[i + 1]), (z1, ar[i]), tuple(want))


def duct_joint(kit, z1, lv):
    """The transverse joint at a section's +Z end: S-slips on the 24 in faces, drive cleats on the 12 in sides with their tabs folded
    over the slips (SMACNA's S and drive for low-pressure duct of this size)."""
    hw, hh = DW / 2.0, DH / 2.0
    zc = z1 - JW / 2.0
    w = 0.0
    for s in (1.0, -1.0):
        box(kit, (DW - 0.006, 0.0042, JW), (0.0, s * (hh + XBREAK * 0.0 + 0.0021), zc), GALV, lv, w=w, name="S-slip")
        if lv == 0:
            box(kit, (DW - 0.010, 0.0016, 0.006), (0.0, s * (hh + 0.0050), zc - JW / 2.0 + 0.004), GALV, lv, w=0.0, name="slip hem")
        box(kit, (0.0032, DH + 0.0060, JW), (s * (hw + 0.0016), 0.0, zc), GALV, lv, w=w, name="drive cleat")
        if lv == 0:
            for t in (1.0, -1.0):
                box(kit, (0.016, 0.0016, JW - 0.002), (s * (hw - 0.0065), t * (hh + 0.0050), zc), GALV, lv, w=0.0, name="drive tab")


def duct_seams(kit, z0, z1, lv):
    """Two Pittsburgh lock seams on diagonally opposite corners (the folded seam proud of the corner)."""
    if lv >= 2:
        return
    hw, hh = DW / 2.0, DH / 2.0
    L = z1 - z0
    for (sx, sy) in ((1.0, 1.0), (-1.0, -1.0)):
        box(kit, (0.0048, 0.0048, L - 0.002), (sx * (hw - 0.0004), sy * (hh - 0.0004), (z0 + z1) / 2.0), GALV, lv, w=0.0,
              rot=(0, 0, 45), name="pittsburgh seam")


def duct(kit, lv, length, sections):
    m = S.Mesh()
    seg = length / sections
    for k in range(sections):
        duct_skin(m, k * seg, (k + 1) * seg, lv)
    m.part(kit, "duct skin", GALV)
    if lv <= 1:
        for k in range(sections):
            duct_joint(kit, (k + 1) * seg, lv)
        duct_seams(kit, 0.0, length, lv)


def duct_meta(kit, length):
    kit.no_collider()
    D.anchor_u(kit, "start", (0.0, 0.0, 0.0))
    D.anchor_u(kit, "end", (0.0, 0.0, length))
    kit.meta["ductSection"] = [DW, DH]
    kit.meta["sectionLength"] = SEC_L
    kit.min_ceiling = 0.0


def duct_slope(kit, lv):
    """The 1:8 pitch change: two gores mitred on the bisecting plane (z = y tan(a/2)), 0.15 m each side of the kink, joints at both ends,
    a standing gore seam round the mitre. Origin on the axis at the kink; inlet along +Z level, outlet turned A8 about +X (falling)."""
    hw, hh = DW / 2.0, DH / 2.0
    t = math.tan(A8 / 2.0)
    sec = _duct_outline(min(lv, 1)) if lv < 2 else [(hw, -hh), (hw, hh), (-hw, hh), (-hw, -hh)]
    ar = arcs(sec)
    n = len(sec)
    m = S.Mesh()
    L = 0.15
    for i in range(n):
        j = (i + 1) % n
        (xa, ya), (xb, yb) = sec[i], sec[j]
        # inlet gore
        a0, b0 = (xa, ya, -L), (xb, yb, -L)
        a1, b1 = (xa, ya, ya * t), (xb, yb, yb * t)
        want = (V(b0) - V(a0)).cross(V((0, 0, 1)))
        D.qd(m, a0, b0, b1, a1, (-L, ar[i]), (-L, ar[i + 1]), (yb * t, ar[i + 1]), (ya * t, ar[i]), tuple(want))
        # outlet gore (local, then turned)
        c0, d0 = rx((xa, ya, -ya * t), A8), rx((xb, yb, -yb * t), A8)
        c1, d1 = rx((xa, ya, L), A8), rx((xb, yb, L), A8)
        want2 = rx(tuple(want), A8)
        D.qd(m, c0, d0, d1, c1, (-ya * t, ar[i]), (-yb * t, ar[i + 1]), (L, ar[i + 1]), (L, ar[i]), want2)
    m.part(kit, "gores", GALV)
    if lv <= 1:
        w = 0.0
        # joint bands at both outer ends (inlet: the female end shows the slip's edge; outlet: the male end)
        for s in (1.0, -1.0):
            box(kit, (DW - 0.006, 0.0042, JW), (0.0, s * (hh + 0.0021), -L + JW / 2.0), GALV, lv, w=w, name="S-slip in")
            box(kit, (0.0032, DH + 0.006, JW), (s * (hw + 0.0016), 0.0, -L + JW / 2.0), GALV, lv, w=w, name="drive in")
        mo = S.Mesh()
        for s in (1.0, -1.0):
            for (sz, cz) in (((DW - 0.006, 0.0042, JW), (0.0, s * (hh + 0.0021), L - JW / 2.0)), ((0.0032, DH + 0.006, JW), (s * (hw + 0.0016), 0.0, L - JW / 2.0))):
                hx, hy, hz = sz[0] / 2.0, sz[1] / 2.0, sz[2] / 2.0
                cx, cy, cz2 = cz
                corners = [rx((cx + ix * hx, cy + iy * hy, cz2 + iz * hz), A8) for ix in (-1, 1) for iy in (-1, 1) for iz in (-1, 1)]
                _box_into(mo, corners)
        mo.part(kit, "joint out", GALV)
        # the gore seam: a standing seam 6 mm high round the mitre (four bars on the mitre lines)
        ms = S.Mesh()
        for s in (1.0, -1.0):
            y = s * (hh + 0.003)
            bar(ms, (-hw - 0.003, y, y * t), (hw + 0.003, y, y * t), 0.0016, 0.006, (0, 0, 1))
            x = s * (hw + 0.003)
            bar(ms, (x, -hh - 0.003, (-hh - 0.003) * t), (x, hh + 0.003, (hh + 0.003) * t), 0.006, 0.0016, (0, 0, 1))
        ms.part(kit, "gore seam", GALV)


def _box_into(m, c):
    """A box from its 8 corners (index ix*4 + iy*2 + iz over (-1, 1)^3) into Mesh m, quads wound out."""
    cen = sum((V(p) for p in c), V((0, 0, 0))) / 8.0
    faces = ((0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3))
    for f in faces:
        a, b, cc, d = (c[k] for k in f)
        mid = (V(a) + V(b) + V(cc) + V(d)) / 4.0
        D.qd(m, a, b, cc, d, (0, 0), (0.02, 0), (0.02, 0.02), (0, 0.02), tuple(mid - cen))


DUCT_HANG = 0.15                   # duct top to the soffit (today's 2.75 -> 2.90)


def duct_hanger(kit, lv):
    """Strap hangers (SMACNA: 1 in x 18 ga for this size): each side a galvanised strap from the soffit down the duct's side, turned
    50 mm under its bottom, two screws in the side and one under; the strap's head turned 50 mm out on the soffit with a pin."""
    hw, hh = DW / 2.0, DH / 2.0
    top = hh + DUCT_HANG
    t, w = 0.0012, 0.0254
    bw = 0.0
    for s in (-1.0, 1.0):
        x = s * (hw + t / 2.0 + 0.0002)
        box(kit, (t, top + hh + t, w), (x, (top - hh) / 2.0, 0.0), HDG, lv, w=bw, name="strap")
        box(kit, (0.05, t, w), (s * (hw - 0.025 + t), -hh - t / 2.0 - 0.0002, 0.0), HDG, lv, w=bw, name="strap foot")
        box(kit, (0.05, t, w), (s * (hw + 0.025 + t), top - t / 2.0, 0.0), HDG, lv, w=bw, name="strap head")
        if lv == 0:
            for y in (0.07, -0.09):
                stud(kit, (x + s * (t / 2.0 + 0.001), y, 0.0), "x", 0.0045, 0.002, HDG, lv)
            stud(kit, (s * (hw - 0.03), -hh - t - 0.0012, 0.0), "y", 0.0045, 0.002, HDG, lv)
            stud(kit, (s * (hw + 0.035), top - t - 0.0013, 0.0), "y", 0.0045, 0.0025, STEEL, lv, name="pin")


def duct_hanger_meta(kit):
    kit.no_collider()
    D.anchor_u(kit, "axis", (0.0, 0.0, 0.0))
    D.anchor_u(kit, "soffit", (0.0, DH / 2.0 + DUCT_HANG, 0.0))
    kit.min_ceiling = 0.0


def duct_collar(kit, lv):
    """The fire damper's retaining angles (UL 555 practice: 1-1/2 x 1-1/2 in, 16 ga) round the duct on the wall face: one leg on the wall,
    one on the duct, screwed to the sleeve every 150 mm; origin on the axis at the wall face, the duct leaving along +Z."""
    hw, hh = DW / 2.0, DH / 2.0
    g, t = 0.038, 0.0016
    bw = 0.0
    for s in (1.0, -1.0):
        box(kit, (DW + 2 * g, g, t), (0.0, s * (hh + g / 2.0), t / 2.0), HDG, lv, w=bw, name="angle wall leg")
        box(kit, (DW + 2 * t, t, g), (0.0, s * (hh + t / 2.0), g / 2.0), HDG, lv, w=bw, name="angle duct leg")
        box(kit, (g, DH, t), (s * (hw + g / 2.0), 0.0, t / 2.0), HDG, lv, w=bw, name="angle wall leg side")
        box(kit, (t, DH, g), (s * (hw + t / 2.0), 0.0, g / 2.0), HDG, lv, w=bw, name="angle duct leg side")
        if lv == 0:
            for k in range(4):
                x = -0.225 + 0.15 * k
                stud(kit, (x, s * (hh + t + 0.001), g * 0.55), "y", 0.004, 0.002, HDG, lv)
            stud(kit, (s * (hw + t + 0.001), 0.0, g * 0.55), "x", 0.004, 0.002, HDG, lv)


def duct_collar_meta(kit):
    kit.no_collider()
    kit.tag("wall_unit")
    kit.min_ceiling = 0.0


# ============================================================================= the services' slope pieces
MAIN_R = 0.0572                    # 4 in sch 10 (Kit_SprinklerMain_3m)
CPL_R = 0.0700                     # grooved coupling housing


def _coupling(kit, centre, axis, lv, r_pipe, name="coupling", slot=RED):
    """A two-bolt grooved coupling round a pipe (axis a unit Unity vector): the housing ring in two halves with their bolt pads, track
    bolts and nuts (ductile iron, field painted with the pipe)."""
    C, A = V(centre), V(axis).normalized()
    r = r_pipe + 0.0125
    w = 0.046
    ptube(kit, name + " housing", [tuple(C - A * (w / 2.0)), tuple(C + A * (w / 2.0))], r, slot, lv, n0=28)
    if lv >= 2:
        return
    if lv == 0:
        ptube(kit, name + " key", [tuple(C - A * (w / 2.0 + 0.004)), tuple(C - A * (w / 2.0))], r - 0.006, slot, lv, n0=24, caps=False)
        ptube(kit, name + " key b", [tuple(C + A * (w / 2.0)), tuple(C + A * (w / 2.0 + 0.004))], r - 0.006, slot, lv, n0=24, caps=False)
    ref = V((0, 1, 0)) if abs(A.y) < 0.9 else V((1, 0, 0))
    side = ref.cross(A).normalized()
    for s in (1.0, -1.0):
        p = C + side * (s * (r + 0.012))
        m = S.Mesh()
        bar(m, tuple(p - A * 0.022), tuple(p + A * 0.022), 0.026, 0.022, tuple(ref))
        m.part(kit, name + " pad", slot)
        if lv == 0:
            ptube(kit, "track bolt", [tuple(p - ref * 0.03), tuple(p + ref * 0.03)], 0.0055, STEEL, lv, n0=6)
            for t in (1.0, -1.0):
                hexnut(kit, tuple(p + ref * (t * 0.017)), "y" if abs(ref.y) > 0.9 else "x", 0.017, 0.009, STEEL, lv)


def main_slope(kit, lv):
    """The 4 in main's 1:8 pitch change: two stubs meeting in a flexible grooved coupling at the kink (two couplings share the 7.1 deg);
    origin on the axis at the kink, inlet along +Z level, outlet turned A8 about +X. The adjacent Kit_SprinklerMain_3m kits butt on."""
    L = 0.14
    ptube(kit, "stub in", [(0.0, 0.0, -L), (0.0, 0.0, 0.01)], MAIN_R, RED, lv, n0=24)
    ptube(kit, "stub out", [rx((0.0, 0.0, -0.01), A8), rx((0.0, 0.0, L), A8)], MAIN_R, RED, lv, n0=24)
    bis = V(rx((0.0, 0.0, 1.0), A8 / 2.0))
    _coupling(kit, (0.0, 0.0, 0.0), tuple(bis), lv, MAIN_R, "kink coupling")
    if lv == 0:
        for z in (-L + 0.016,):
            ptube(kit, "groove", [(0.0, 0.0, z - 0.004), (0.0, 0.0, z + 0.004)], MAIN_R - 0.0021, RED, lv, n0=24, caps=False)
        g = rx((0.0, 0.0, L - 0.016), A8)
        ptube(kit, "groove b", [tuple(V(g) - V(rx((0, 0, 0.004), A8))), tuple(V(g) + V(rx((0, 0, 0.004), A8)))], MAIN_R - 0.0021, RED, lv, n0=24, caps=False)


def main_slope_meta(kit):
    kit.no_collider()
    kit.tag("ceiling")
    kit.min_ceiling = 0.0


TRAY_W, TRAY_H = 0.30, 0.08


def tray_slope(kit, lv):
    """Kit_CableTray_3m's hinged vertical splice plates for the 1:8 change: on each side rail two 3 mm plates, one on each tray end,
    pivoting on a hinge bolt at the kink, two bolts each into the rails. Origin at the kink on the tray's bottom centre line."""
    t = 0.003
    for s in (1.0, -1.0):
        x = s * (TRAY_W / 2.0 + t / 2.0 + 0.001)
        m = S.Mesh()
        prism(m, [(-t / 2.0, 0.006), (t / 2.0, 0.006), (t / 2.0, TRAY_H - 0.006), (-t / 2.0, TRAY_H - 0.006)], (x, 0.0, 0.0), (1, 0, 0), (0, 1, 0), (0, 0, 1),
              -0.11, 0.012)
        m.part(kit, "splice plate in", HDG)
        m2 = S.Mesh()
        sec = [(-t / 2.0, 0.006), (t / 2.0, 0.006), (t / 2.0, TRAY_H - 0.006), (-t / 2.0, TRAY_H - 0.006)]
        o = (x + s * t, 0.0, 0.0)
        prism(m2, sec, o, (1, 0, 0), rx((0, 1, 0), A8), rx((0, 0, 1), A8), -0.012, 0.11)
        m2.part(kit, "splice plate out", HDG)
        if lv <= 1:
            hexnut(kit, (x + s * (t + 0.004), TRAY_H / 2.0, 0.0), "x", 0.014, 0.007, STEEL, lv, name="hinge nut")
            if lv == 0:
                for z in (-0.085, -0.04):
                    hexnut(kit, (x + s * 0.0035, TRAY_H / 2.0, z), "x", 0.011, 0.005, HDG, lv)
                for z in (0.04, 0.085):
                    p = rx((0.0, TRAY_H / 2.0, z), A8)
                    hexnut(kit, (x + s * (t + 0.0035), p[1], p[2]), "x", 0.011, 0.005, HDG, lv)


def tray_slope_meta(kit):
    kit.no_collider()
    kit.tag("ceiling")
    kit.min_ceiling = 0.0


# ============================================================================= the riser shaft: Kit_RiserPipe_*, Kit_RiserClamp, Kit_PipeClampWall
RISER_R = 0.05715                  # 4 in sch 10, 4.500 in OD
GALV2_R = 0.03015                  # 2 in sch 40, 2.375 in OD


def _grooved_pipe(kit, a, b, r, slot, lv, name="pipe"):
    """A roll-grooved pipe from a to b (Unity): the body, and at LOD0 the two roll grooves (8 mm wide, 2.1 mm deep, 16 mm from each end)
    as real recesses."""
    A, Bv = V(a), V(b)
    d = (Bv - A)
    L = d.length
    t = d / L
    if lv > 0:
        ptube(kit, name, [tuple(A), tuple(Bv)], r, slot, lv, n0=24)
        return
    ptube(kit, name + " end a", [tuple(A), tuple(A + t * 0.012)], r, slot, lv, n0=24)
    ptube(kit, name + " groove a", [tuple(A + t * 0.012), tuple(A + t * 0.020)], r - 0.0021, slot, lv, n0=24, caps=False)
    ptube(kit, name, [tuple(A + t * 0.020), tuple(Bv - t * 0.020)], r, slot, lv, n0=24)
    ptube(kit, name + " groove b", [tuple(Bv - t * 0.020), tuple(Bv - t * 0.012)], r - 0.0021, slot, lv, n0=24, caps=False)
    ptube(kit, name + " end b", [tuple(Bv - t * 0.012), tuple(Bv)], r, slot, lv, n0=24)


def riser(kit, lv, length=3.0):
    """Kit_RiserPipe_3m: a 4 in sch 10 sprinkler riser, roll-grooved both ends, its coupling at the top joint (the next length's bottom
    seats in it). Origin at the bottom on the axis; +Y up."""
    _grooved_pipe(kit, (0.0, 0.0, 0.0), (0.0, length, 0.0), RISER_R, RED, lv, "riser")
    _coupling(kit, (0.0, length, 0.0), (0.0, 1.0, 0.0), lv, RISER_R, "riser coupling")


def riser_galv(kit, lv, length=3.0):
    """Kit_RiserPipe_3m_Galv2: 2 in galvanised threaded pipe, its coupling at the top with the thread run showing below it."""
    ptube(kit, "pipe", [(0.0, 0.0, 0.0), (0.0, length - 0.035, 0.0)], GALV2_R, HDG, lv, n0=16)
    ptube(kit, "thread run", [(0.0, length - 0.035, 0.0), (0.0, length, 0.0)], GALV2_R - (0.0008 if lv == 0 else 0.0), HDG, lv, n0=16)
    ptube(kit, "coupling", [(0.0, length - 0.022, 0.0), (0.0, length + 0.022, 0.0)], 0.0365, HDG, lv, n0=16)
    if lv == 0:
        for y in (length - 0.022, length + 0.020):
            ptube(kit, "coupling chamfer", [(0.0, y, 0.0), (0.0, y + 0.002, 0.0)], 0.0352, HDG, lv, n0=16)


def riser_meta(kit, length, r):
    kit.no_collider()
    D.anchor_u(kit, "bottom", (0.0, 0.0, 0.0))
    D.anchor_u(kit, "top", (0.0, length, 0.0))
    kit.meta["pipeRadius"] = r
    kit.min_ceiling = 0.0


TEE_RUN, TEE_UP = 0.13, 0.155


def riser_tee(kit, lv):
    """Kit_RiserPipe_Tee: a grooved tee on the 4 in main (run along Z through it), its branch up to the riser's first coupling (the riser's
    bottom at y TEE_UP). Origin on the run's axis at the branch's centre."""
    r = RISER_R + 0.0083
    ptube(kit, "tee run", [(0.0, 0.0, -TEE_RUN), (0.0, 0.0, TEE_RUN)], r, RED, lv, n0=28)
    ptube(kit, "tee branch", [(0.0, 0.0, 0.0), (0.0, TEE_UP - 0.02, 0.0)], r, RED, lv, n0=28)
    if lv <= 1:
        # the cast body's crotch fillet as a short cone, and the parting line
        kit.cylinder(r + 0.012, 0.03, S.ub((0.0, r * 0.75, 0.0)), RED, verts=28 if lv == 0 else 14, bevel=0.0, segments=0, name="crotch", radius_top=r)
    _coupling(kit, (0.0, TEE_UP, 0.0), (0.0, 1.0, 0.0), lv, RISER_R, "branch coupling")
    for s in (1.0, -1.0):
        _coupling(kit, (0.0, 0.0, s * TEE_RUN), (0.0, 0.0, 1.0), lv, RISER_R, "run coupling")


def riser_tee_meta(kit):
    kit.no_collider()
    D.anchor_u(kit, "riser", (0.0, TEE_UP, 0.0))
    kit.min_ceiling = 0.0


def riser_clamp(kit, lv):
    """Kit_RiserClamp: a two-bolt riser clamp for 4 in pipe (3/8 x 1-1/4 in carbon steel halves, ears to 0.18 m each side) bearing on a
    beam's top flange. Origin on the pipe axis at the clamp's underside."""
    t, h = 0.0095, 0.032
    ear = 0.18
    rr = RISER_R + t / 2.0 + 0.0005
    gap = 0.004
    n = S.arc_steps(rr, math.pi, lv, lo=6)
    sec = [(-h / 2.0, -t / 2.0), (h / 2.0, -t / 2.0), (h / 2.0, t / 2.0), (-h / 2.0, t / 2.0)]
    for s in (1.0, -1.0):
        zo = s * (gap + t / 2.0)
        path = [(-ear, h / 2.0, zo)]
        for k in range(n + 1):
            a = math.pi - math.pi * k / n
            path.append((rr * math.cos(a), h / 2.0, s * (rr * math.sin(a)) + (zo if abs(math.sin(a)) < 1e-6 else 0.0)))
        path.append((ear, h / 2.0, zo))
        # the straight ears sit off the axis by the gap; the arc joins them
        pts = [path[0], (-rr, h / 2.0, zo)] + [(rr * math.cos(math.pi - math.pi * k / n), h / 2.0, zo + s * rr * math.sin(math.pi - math.pi * k / n))
                                                for k in range(1, n)] + [(rr, h / 2.0, zo), path[-1]]
        m = S.Mesh()
        S.sweep_section(m, pts, sec, closed_section=True, cap0=True, cap1=True)
        m.part(kit, "clamp half", STEEL)
    if lv <= 1:
        for x in (-(RISER_R + 0.024), RISER_R + 0.024):
            tube(kit, "clamp bolt", [(x, h / 2.0, -0.03), (x, h / 2.0, 0.03)], 0.0048, STEEL, lv, lo=8)
            hexnut(kit, (x, h / 2.0, gap + t + 0.004), "z", 0.0143, 0.008, STEEL, lv)
            hexnut(kit, (x, h / 2.0, -(gap + t + 0.004)), "z", 0.0143, 0.008, STEEL, lv)


def riser_clamp_meta(kit):
    kit.no_collider()
    kit.min_ceiling = 0.0


STRUT = 0.0413                     # 1-5/8 in strut channel


def pipe_clamp_wall(kit, lv):
    """Kit_PipeClampWall: a 1-5/8 in 12 ga strut channel 0.80 m long on the wall (open face out), two expansion anchors inside it, and two
    strut pipe clamps holding 2 in pipes 0.60 m apart (axes 0.071 m off the wall). Origin on the wall plane at the channel's centre."""
    T = 0.0026
    prof = [(0.0, -STRUT / 2), (STRUT, -STRUT / 2), (STRUT, -0.0105), (STRUT - T, -0.0105), (STRUT - T, -STRUT / 2 + T), (T, -STRUT / 2 + T),
            (T, STRUT / 2 - T), (STRUT - T, STRUT / 2 - T), (STRUT - T, 0.0105), (STRUT, 0.0105), (STRUT, STRUT / 2), (0.0, STRUT / 2)]
    if lv >= 2:
        prof = [(0.0, -STRUT / 2), (STRUT, -STRUT / 2), (STRUT, STRUT / 2), (0.0, STRUT / 2)]
    D.extrude_profile(kit, "strut", prof, -0.40, 0.40, GALV)
    if lv <= 1:
        for x in (-0.33, 0.33):
            hexnut(kit, (x, 0.0, T + 0.004), "z", 0.0143, 0.008, ZINC, lv, name="anchor head")
            rcyl(kit, 0.012, 0.002, (x, 0.0, T + 0.001), "z", ZINC, 12, name="washer")
    pz = STRUT + GALV2_R
    for x in (-0.30, 0.30):
        m = S.Mesh()
        n = S.arc_steps(GALV2_R + 0.002, 2 * math.pi, lv, lo=8)
        ring = [(x + (GALV2_R + 0.0013) * math.cos(2 * math.pi * k / n), 0.0, pz + (GALV2_R + 0.0013) * math.sin(2 * math.pi * k / n)) for k in range(n)]
        sec = [(-0.0125, -0.0013), (0.0125, -0.0013), (0.0125, 0.0013), (-0.0125, 0.0013)]
        S.sweep_section(m, ring + [ring[0]], sec, closed_section=True)
        m.part(kit, "clamp strap", ZINC)
        for s in (1.0, -1.0):
            box(kit, (0.012, 0.025, 0.012), (x + s * (GALV2_R + 0.009), 0.0, STRUT + 0.006), ZINC, lv, w=0.0, name="clamp ear")
            if lv <= 1:
                hexnut(kit, (x + s * (GALV2_R + 0.009), 0.0, STRUT + 0.016), "z", 0.0111, 0.006, ZINC, lv)


def pipe_clamp_wall_meta(kit):
    kit.no_collider()
    kit.tag("wall_unit")
    D.anchor_u(kit, "pipe_a", (-0.30, 0.0, STRUT + GALV2_R))
    D.anchor_u(kit, "pipe_b", (0.30, 0.0, STRUT + GALV2_R))
    kit.min_ceiling = 0.0


# ============================================================================= Kit_CagedLadder (OSHA 1910.27, 1971 text)
LAD_TOP = 4.33                     # the rails' tops over the bottom rung (the shaft's lid 4.49 over it)
RUNG_Z = 0.178                     # 7 in from the wall to the rung's centre
RAIL_X = 0.2080                    # 16 in clear between the rails' inner faces + half the 3/8 in bar
RUNG_R = 0.0095                    # 3/4 in round
HOOP_R = 0.343                     # 27 in wide cage
CAGE_IN = 0.711                    # 28 in from the rung centre line to the cage's inside
HOOPS = (1.00, 2.07, 3.14, 4.21)   # the first 1.0 m over the bottom rung (the shaft's rule: out of reach anyway), <= 4 ft apart


def _hoop_path(r, lv, flare=0.0):
    cz = RUNG_Z + CAGE_IN - HOOP_R
    R = r + flare
    n = S.arc_steps(R, math.pi, lv, lo=8)
    pts = [(-(RAIL_X + 0.0095), 0.0, RUNG_Z - 0.010), (-R, 0.0, RUNG_Z - 0.010 + 0.02)]
    pts += [(R * math.cos(math.pi - math.pi * k / n), 0.0, cz + R * math.sin(math.pi - math.pi * k / n) + flare * 0.0) for k in range(n + 1)]
    pts += [(R, 0.0, RUNG_Z - 0.010 + 0.02), (RAIL_X + 0.0095, 0.0, RUNG_Z - 0.010)]
    return pts, cz


def _bar_spots():
    """Where the cage's vertical bars stand on the hoop (x, z) and their outward normal: two on each straight side, six on the round."""
    cz = RUNG_Z + CAGE_IN - HOOP_R
    out = []
    for s in (-1.0, 1.0):
        out.append(((s * HOOP_R, RUNG_Z + 0.13), (s, 0.0)))
    for k in range(6):
        a = math.pi - math.pi * k / 5.0
        out.append(((HOOP_R * math.cos(a), cz + HOOP_R * math.sin(a)), (math.cos(a), math.sin(a))))
    return out


def caged_ladder(kit, lv):
    """Kit_CagedLadder, 4.50 m: hot-dip galvanised 2-1/2 x 3/8 in rails 16 in apart, 3/4 in rungs at 12 in plug-welded through the rails,
    wall brackets on 7 in standoffs with expansion anchors, the cage of 2 x 1/4 in hoops with eight 1-1/2 x 1/4 in bars bolted inside,
    the bottom hoop flared 100 mm. Origin on the wall plane at the bottom rung's height, x centred; +Z out of the wall."""
    bw = 0.0008 if lv == 0 else 0.0
    for s in (-1.0, 1.0):
        box(kit, (0.0095, LAD_TOP + 0.15, 0.0635), (s * RAIL_X, (LAD_TOP - 0.15) / 2.0, RUNG_Z), HDG, lv, w=bw, name="rail")
    n_r = int(LAD_TOP / 0.3048 + 1e-6) + 1
    for k in range(n_r):
        y = k * 0.3048
        if y > LAD_TOP - 0.05:
            break
        tube(kit, "rung", [(-RAIL_X - 0.00475, y, RUNG_Z), (RAIL_X + 0.00475, y, RUNG_Z)], RUNG_R, HDG, lv, lo=8)
    # wall brackets
    for y in (0.20, 2.15, 4.10):
        for s in (-1.0, 1.0):
            x = s * (RAIL_X + 0.0095)
            box(kit, (0.0095, 0.0635, RUNG_Z + 0.03), (x, y, (RUNG_Z + 0.03) / 2.0), HDG, lv, w=bw, name="bracket arm")
            box(kit, (0.075, 0.110, 0.0095), (x + s * 0.028, y, 0.00475), HDG, lv, w=bw, name="bracket flange")
            if lv == 0:
                for dy in (-0.032, 0.032):
                    stud(kit, (x + s * 0.045, y + dy, 0.0095 + 0.004), "z", 0.0082, 0.008, HDG, lv, name="anchor nut")
                for dz in (RUNG_Z - 0.016, RUNG_Z + 0.016):
                    stud(kit, (x + s * 0.0095, y, dz), "x", 0.0082, 0.008, HDG, lv, name="rail bolt")
    # the cage
    sec = [(-0.0254, -0.0032), (0.0254, -0.0032), (0.0254, 0.0032), (-0.0254, 0.0032)]
    for i, y in enumerate(HOOPS):
        pts, cz = _hoop_path(HOOP_R + 0.0032, lv, flare=0.10 if i == 0 else 0.0)
        m = S.Mesh()
        S.sweep_section(m, [(p[0], y, p[2]) for p in pts], sec, closed_section=True, cap0=True, cap1=True)
        m.part(kit, "hoop", HDG)
    bars = _bar_spots()
    for (bx, bz), (nx, nz) in bars:
        inset = 0.0064 + 0.0032
        x0, z0 = bx - nx * inset, bz - nz * inset
        # flared foot: from the flared hoop out at the bottom to the regular line at 1.45 m
        fx, fz = x0 + nx * 0.10, z0 + nz * 0.10
        tx, tz = nz, -nx       # tangent: the bar's wide face is tangent to the hoop
        m = S.Mesh()
        up = (tx, 0.0, tz)
        bar(m, (fx, HOOPS[0], fz), (x0, HOOPS[0] + 0.45, z0), 0.038, 0.0064, up)
        bar(m, (x0, HOOPS[0] + 0.45, z0), (x0, HOOPS[-1] + 0.02, z0), 0.038, 0.0064, up)
        m.part(kit, "cage bar", HDG)
        if lv == 0:
            for y in HOOPS[1:]:
                stud(kit, (bx + nx * 0.0072, y, bz + nz * 0.0072), "x" if abs(nx) > 0.7 else "z", 0.0064, 0.006, HDG, lv, name="cage nut")


def caged_ladder_meta(kit):
    kit.no_collider()
    kit.tag("wall_unit")
    D.anchor_u(kit, "bottom_rung", (0.0, 0.0, RUNG_Z))
    D.anchor_u(kit, "top", (0.0, LAD_TOP, 0.0))
    kit.min_ceiling = 0.0


# ============================================================================= Kit_WireCagePartition_* (welded-wire tenant storage partition)
CAGE_TOP = 2.88                    # SP5's deck over the cage (Clear - 0.02)
POST = 0.0508                      # 2 in square tube posts, 11 ga
CH_W, CH_D, CH_T = 0.0318, 0.0127, 0.0027    # 1-1/4 x 1/2 in, 12 ga channel frames
PANEL_Y0, PANEL_Y1 = 0.050, 2.830  # frames 50 mm off the floor, 50 mm under the deck
WIRE_Z = 0.0017                    # the two wire faces, 3.4 mm apart (10 ga): both sides front-faced (FR-1)


def _channel(m, a, b, n_in, z_ax, lv):
    """A 1-1/4 x 1/2 in channel member from a to b (Unity), its web on the outside and its open side toward n_in (in the panel's plane),
    z_ax the panel's thickness axis; built as a closed C prism (a box at LOD2)."""
    A, Bv = V(a), V(b)
    t = (Bv - A).normalized()
    n, z = V(n_in), V(z_ax)
    # right-handed frame (n, z, t)
    if n.cross(z).dot(t) < 0:
        z = -z
    h = CH_D / 2.0
    if lv >= 2:
        sec = [(0.0, -h), (CH_W, -h), (CH_W, h), (0.0, h)]
    else:
        sec = [(0.0, -h), (CH_W, -h), (CH_W, -h + CH_T), (CH_T, -h + CH_T), (CH_T, h - CH_T), (CH_W, h - CH_T), (CH_W, h), (0.0, h)]
    prism(m, sec, tuple(A), tuple(n), tuple(z), tuple(t), 0.0, (Bv - A).length)


def _wire(m, x0, x1, y0, y1, z0=0.0):
    """The woven wire as two faces 3.4 mm apart, each facing its own side, both mapped by the same metre UVs so the alpha-cut diamonds
    coincide: from either side the near face wins and is lit as a front face (the K1 review's FR-1: one double-sided face read black from
    behind)."""
    for s in (1.0, -1.0):
        z = z0 + s * WIRE_Z
        flat_quad(m, (x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z), (0.0, 0.0, s), ((x0, y0), (x1, y0), (x1, y1), (x0, y1)))


def _frame(kit, x0, x1, y0, y1, lv, name="frame", slot=PAINT, z0=0.0):
    m = S.Mesh()
    _channel(m, (x0, y0, z0), (x0, y1, z0), (1, 0, 0), (0, 0, 1), lv)
    _channel(m, (x1, y1, z0), (x1, y0, z0), (-1, 0, 0), (0, 0, 1), lv)
    _channel(m, (x0 + CH_W, y0, z0), (x1 - CH_W, y0, z0), (0, 1, 0), (0, 0, 1), lv)
    _channel(m, (x1 - CH_W, y1, z0), (x0 + CH_W, y1, z0), (0, -1, 0), (0, 0, 1), lv)
    m.part(kit, name, slot)


def cage_panel(kit, lv, width):
    """A partition panel width wide (between the posts' faces, less 4 mm), frame 50 mm off the floor to 50 mm under SP5's deck: 12 ga
    channel frame, 10 ga woven wire in a 1-1/2 in diamond (Prop_WireMesh, two faces), a 1-1/4 in flat-bar stiffener across at mid height,
    clamp plates with their bolts where it bolts to the posts. Origin on the floor at the panel's centre; the panel in the XY plane."""
    hw = width / 2.0
    _frame(kit, -hw, hw, PANEL_Y0, PANEL_Y1, lv)
    m = S.Mesh()
    _wire(m, -hw + CH_W - 0.004, hw - CH_W + 0.004, PANEL_Y0 + CH_W - 0.004, PANEL_Y1 - CH_W + 0.004)
    m.part(kit, "woven wire", WIRE)
    ym = (PANEL_Y0 + PANEL_Y1) / 2.0
    box(kit, (width - 2 * CH_W + 0.004, 0.0318, 0.0032), (0.0, ym, WIRE_Z + 0.0024), PAINT, lv, w=0.0008 if lv == 0 else 0.0, name="stiffener")
    if lv <= 1:
        for s in (-1.0, 1.0):
            for y in (0.32, ym, PANEL_Y1 - 0.30):
                box(kit, (0.006, 0.045, 0.028), (s * (hw + 0.003), y, 0.0), STEEL, lv, w=0.0, name="clamp plate")
                if lv == 0:
                    for dy in (-0.014, 0.014):
                        stud(kit, (s * (hw - 0.012), y + dy, CH_D / 2.0 + 0.003), "z", 0.0064, 0.005, STEEL, lv, name="clamp nut")


def cage_panel_meta(kit, width):
    kit.no_collider()
    kit.meta["panelWidth"] = width
    kit.min_ceiling = CAGE_TOP


def cage_post(kit, lv):
    """A 2 in square tube post (11 ga, the brake's 3 mm corners) from a bolted floor shoe to an angle clip on SP5's deck. Origin on the
    floor at the post's axis."""
    per = 2 if lv == 0 else (1 if lv == 1 else 0)
    sec = rrect(POST, POST, 0.003, per)
    m = S.Mesh()
    prism(m, sec, (0.0, 0.0, 0.0), (1, 0, 0), (0, 0, -1), (0, 1, 0), 0.0064, CAGE_TOP - 0.004)
    m.part(kit, "post", PAINT)
    box(kit, (0.10, 0.0064, 0.10), (0.0, 0.0032, 0.0), PAINT, lv, w=0.0008 if lv == 0 else 0.0, name="floor shoe")
    box(kit, (0.05, 0.004, 0.05), (0.0, CAGE_TOP - 0.002, 0.0), PAINT, lv, w=0.0, name="deck clip")
    box(kit, (0.004, 0.05, 0.05), (POST / 2.0 + 0.002, CAGE_TOP - 0.027, 0.0), PAINT, lv, w=0.0, name="clip leg")
    if lv <= 1:
        for (x, z) in ((-0.035, -0.035), (0.035, 0.035)):
            hexnut(kit, (x, 0.0064 + 0.004, z), "y", 0.0143, 0.008, STEEL, lv, name="anchor nut")
        hexnut(kit, (POST / 2.0 + 0.008, CAGE_TOP - 0.027, 0.0), "x", 0.0111, 0.006, STEEL, lv, name="clip bolt")


def cage_post_meta(kit):
    kit.no_collider()
    kit.min_ceiling = CAGE_TOP


OPEN_HW = 0.6096                   # a 4 ft door opening (1.219 m between the posts' faces): with one 5 ft panel module (62's Panel1520)
DOOR_W, DOOR_Y1 = 2 * OPEN_HW - 0.040, 2.08   # it closes SP5's 2.84 m cage front exactly; the leaf 20 mm clear each side


def _padlock(kit, c, lv):
    """A laminated steel padlock (1-3/4 in): eight plates with their seams, four rivets, a hardened 9 mm shackle, the keyway. It hangs from
    a horizontal staple, so its shackle's legs part along Z (one leg down through the staple's hole) and its broad faces look along +-X.
    c = the shackle leg through the staple, at the crown's spring (x, y, z)."""
    cx, cy, cz = c
    bw, bh, bd = 0.044, 0.038, 0.022           # width (along z), height, depth (along x)
    zc = cz + 0.013                            # the body's centre line: legs at cz and cz + 0.026
    top = cy - 0.034                           # the body's top, 34 mm under the crown's spring
    n = 8 if lv == 0 else (3 if lv == 1 else 1)
    gap = 0.0004 if lv == 0 else 0.0
    ph = (bh - gap * (n - 1)) / n
    for k in range(n):
        y = top - ph / 2.0 - k * (ph + gap)
        box(kit, (bd, ph, bw), (cx, y, zc), ZINC, lv, w=0.0006 if (lv == 0 and k in (0, n - 1)) else 0.0, name="lamination")
    if gap > 0:
        box(kit, (bd - 0.002, bh - 0.002, bw - 0.002), (cx, top - bh / 2.0, zc), PBLACK, lv, w=0.0, name="core")
    if lv == 0:
        for sx in (1.0, -1.0):
            for (dz, dy) in ((-0.015, -0.006), (0.015, -0.006), (-0.015, -0.032), (0.015, -0.032)):
                S.screw_head(kit, 0.0028, (cx + sx * bd / 2.0, top + dy, zc + dz), "+x" if sx > 0 else "-x", CHROME, lv, name="rivet")
        box(kit, (0.008, 0.002, 0.0028), (cx, top - bh - 0.0008, zc), PBLACK, lv, w=0.0, name="keyway")
    r = 0.0045
    k = S.arc_steps(0.013, math.pi, lv, lo=6)
    pts = [(cx, top + 0.001, cz)] + [(cx, cy + 0.013 * math.sin(math.pi * j / k), zc - 0.013 * math.cos(math.pi * j / k)) for j in range(k + 1)] + [(cx, top + 0.001, cz + 0.026)]
    tube(kit, "shackle", pts, r, CHROME, lv, lo=8)


PF = POST / 2.0                    # a post's front face (z), the posts centred on the panels' plane
LATCH_X = OPEN_HW + POST / 2.0     # the latch post's axis


def cage_door(kit, lv):
    """The cage's door: a 1.18 x 2.03 leaf (the panel's channel frame and two-faced wire, a mid stiffener and a diagonal brace against
    sag) on three weld-on barrel hinges, a barrel bolt on a spacer shooting into a keeper on the latch post, an offset padlock hasp folded
    over a staple welded to the latch post's face and the padlock locked through the staple; the transom panel and its head channel to
    the deck. Origin on the floor at the opening's centre (the opening 1.219 between the posts' faces, the hinge post at -X)."""
    hw = DOOR_W / 2.0
    _frame(kit, -hw, hw, PANEL_Y0, DOOR_Y1, lv, name="leaf frame")
    m = S.Mesh()
    _wire(m, -hw + CH_W - 0.004, hw - CH_W + 0.004, PANEL_Y0 + CH_W - 0.004, DOOR_Y1 - CH_W + 0.004)
    m.part(kit, "leaf wire", WIRE)
    box(kit, (DOOR_W - 2 * CH_W + 0.004, 0.0318, 0.0032), (0.0, 1.06, WIRE_Z + 0.0024), PAINT, lv, w=0.0008 if lv == 0 else 0.0, name="leaf stiffener")
    if lv <= 1:
        # the sag brace: a 1 x 1/8 in flat bar from the bottom hinge corner up to the latch side's stiffener (in tension)
        mb = S.Mesh()
        ba, bb = (-hw + CH_W, PANEL_Y0 + CH_W, -(WIRE_Z + 0.0024)), (hw - CH_W, 1.06 - 0.016, -(WIRE_Z + 0.0024))
        dx, dy = bb[0] - ba[0], bb[1] - ba[1]
        dl = math.hypot(dx, dy)
        bar(mb, ba, bb, 0.0254, 0.0032, (-dy / dl, dx / dl, 0.0))
        mb.part(kit, "leaf brace", PAINT)
    box(kit, (2 * OPEN_HW, 0.0318, 0.0127), (0.0, DOOR_Y1 + 0.022, 0.0), PAINT, lv, w=0.0008 if lv == 0 else 0.0, name="head")
    _frame(kit, -OPEN_HW, OPEN_HW, DOOR_Y1 + 0.038, PANEL_Y1, lv, name="transom frame")
    m2 = S.Mesh()
    _wire(m2, -OPEN_HW + CH_W - 0.004, OPEN_HW - CH_W + 0.004, DOOR_Y1 + 0.038 + CH_W - 0.004, PANEL_Y1 - CH_W + 0.004)
    m2.part(kit, "transom wire", WIRE)
    # hinges: the barrels in the gap at the hinge post, a leaf welded to the post's face and one to the door frame
    for y in ((0.30, 1.06, 1.84) if lv <= 1 else (0.30, 1.84)):
        rcyl(kit, 0.0095, 0.10, (-OPEN_HW + 0.010, y, PF - 0.006), "y", PAINT, 16 if lv == 0 else 8, name="hinge barrel")
        box(kit, (0.004, 0.10, 0.030), (-OPEN_HW + 0.002, y, PF - 0.016), PAINT, lv, w=0.0, name="hinge leaf (post)")
        box(kit, (0.030, 0.10, 0.004), (-hw + 0.012, y, CH_D / 2.0 + 0.002), PAINT, lv, w=0.0, name="hinge leaf (door)")
        if lv == 0:
            rcyl(kit, 0.0050, 0.112, (-OPEN_HW + 0.010, y, PF - 0.006), "y", STEEL, 10, name="hinge pin")
    # the barrel bolt on a spacer (its axis 35 mm proud of the panels' plane, in front of the post's face), the keeper on the latch post
    yb, zb = 0.98, 0.035
    box(kit, (0.085, 0.030, zb - 0.006 - CH_D / 2.0), (hw - 0.050, yb, (zb - 0.006 + CH_D / 2.0) / 2.0), PAINT, lv, w=0.0, name="bolt spacer")
    box(kit, (0.085, 0.024, 0.012), (hw - 0.050, yb, zb), ZINC, lv, w=0.0008 if lv == 0 else 0.0, name="bolt housing")
    tube(kit, "slide bolt", [(hw - 0.090, yb, zb), (LATCH_X - 0.004, yb, zb)], 0.0055, ZINC, lv, lo=8)
    if lv <= 1:
        tube(kit, "bolt knob", [(hw - 0.060, yb, zb), (hw - 0.060, yb, zb + 0.026)], 0.0045, ZINC, lv, lo=8)
        box(kit, (0.026, 0.030, zb + 0.008 - PF), (LATCH_X, yb, (zb + 0.008 + PF) / 2.0), ZINC, lv, w=0.0006 if lv == 0 else 0.0, name="bolt keeper")
    # the offset hasp: its plate on the leaf's stile, the knuckle, the strap stepping out over the gap and lying on the post's face
    yh = 1.16
    if lv <= 1:
        box(kit, (0.040, 0.034, 0.003), (hw - 0.030, yh, CH_D / 2.0 + 0.0016), ZINC, lv, w=0.0, name="hasp plate")
        S.cyl_y(kit, 0.0045, 0.034, (hw - 0.008, yh, CH_D / 2.0 + 0.0055), ZINC, lv, w=0.0006, name="hasp knuckle")
        ms = S.Mesh()
        bar(ms, (hw - 0.006, yh, CH_D / 2.0 + 0.008), (OPEN_HW + 0.002, yh, PF + 0.0015), 0.034, 0.003, (0, 1, 0))
        bar(ms, (OPEN_HW + 0.002, yh, PF + 0.0015), (LATCH_X + 0.032, yh, PF + 0.0015), 0.034, 0.003, (0, 1, 0))
        ms.part(kit, "hasp strap", ZINC)
        # the staple: a 7 mm loop welded to the post's face, legs side by side, its hole upright through the strap's slot
        k = S.arc_steps(0.010, math.pi, lv, lo=6)
        pts = [(LATCH_X - 0.010, yh, PF)] + [(LATCH_X - 0.010 * math.cos(math.pi * j / k), yh, PF + 0.010 + 0.010 * math.sin(math.pi * j / k)) for j in range(k + 1)] + [(LATCH_X + 0.010, yh, PF)]
        tube(kit, "staple", pts, 0.0035, ZINC, lv, lo=8)
        _padlock(kit, (LATCH_X, yh + 0.020, PF + 0.0115), lv)


def cage_door_meta(kit):
    kit.no_collider()
    D.anchor_u(kit, "hinge", (-OPEN_HW, 0.0, 0.0))
    D.anchor_u(kit, "hasp", (LATCH_X, 1.16, PF + 0.012))
    D.anchor_u(kit, "latch_post", (LATCH_X, 0.0, 0.0))
    kit.min_ceiling = CAGE_TOP


# ============================================================================= Kit_HoseCabinet (Class II occupant hose station, semi-recessed)
CAB_W, CAB_H, CAB_Y0 = 0.90, 1.05, 0.98     # the tub's face; its bottom over the bumper rail (0.975)
CAB_BACK, CAB_FRONT = -0.12, 0.05           # the tub 0.12 into the wall (K4 opens the wall face round it), 0.05 out
DOOR_T = 0.025


def hose_cabinet(kit, lv):
    """Kit_HoseCabinet: an 18 ga tub 0.90 x 1.05 x 0.17 semi-recessed (0.08 proud with its door) with a 1-1/2 in flat trim on the wall, a
    full-glass door (DSA glass in a steel frame) on a continuous hinge with a lever and cam latch; inside, a red swing rack with eight folds
    of 1-1/2 in unlined linen hose hanging from its pins, the 1-1/2 in brass angle valve (inlet from the back, outlet down, red handwheel
    toward the door) at 1.40 m, the hose's coupling on it and the brass nozzle hanging from the rack. No lettering (the graphic line (pingmian shijue) hold).
    Origin on the wall plane at the floor, x centred; +Z out of the wall."""
    hw = CAB_W / 2.0
    y0, y1 = CAB_Y0, CAB_Y0 + CAB_H
    yc = (y0 + y1) / 2.0
    sh = 0.0015
    w0 = 0.0008 if lv == 0 else 0.0
    # tub: back, sides, top, bottom (their inner faces are what shows through the glass)
    box(kit, (CAB_W, CAB_H, sh), (0.0, yc, CAB_BACK), WHITE, lv, w=0.0, name="tub back")
    for s in (-1.0, 1.0):
        box(kit, (sh, CAB_H, CAB_FRONT - CAB_BACK), (s * (hw - sh / 2.0), yc, (CAB_BACK + CAB_FRONT) / 2.0), WHITE, lv, w=0.0, name="tub side")
    for (y, nm) in ((y0 + sh / 2.0, "tub bottom"), (y1 - sh / 2.0, "tub top")):
        box(kit, (CAB_W, sh, CAB_FRONT - CAB_BACK), (0.0, y, (CAB_BACK + CAB_FRONT) / 2.0), WHITE, lv, w=0.0, name=nm)
    # the 1-1/2 in flat trim on the wall face round the tub
    tw = 0.038
    for s in (-1.0, 1.0):
        box(kit, (tw, CAB_H + 2 * tw, 0.0016), (s * (hw + tw / 2.0), yc, 0.0008), WHITE, lv, w=0.0, name="trim side")
        box(kit, (CAB_W, tw, 0.0016), (0.0, (y1 + tw / 2.0) if s > 0 else (y0 - tw / 2.0), 0.0008), WHITE, lv, w=0.0, name="trim rail")
    # the door: a 50 mm steel frame, the glass in its stops, the continuous hinge, the lever and cam
    dz = CAB_FRONT + DOOR_T / 2.0
    fw = 0.05
    for s in (-1.0, 1.0):
        box(kit, (fw, CAB_H - 0.004, DOOR_T), (s * (hw - fw / 2.0 - 0.002), yc, dz), WHITE, lv, w=0.0015 if lv == 0 else 0.0, name="door stile")
        box(kit, (CAB_W - 2 * fw - 0.004, fw, DOOR_T), (0.0, (y1 - fw / 2.0 - 0.002) if s > 0 else (y0 + fw / 2.0 + 0.002), dz), WHITE, lv,
            w=0.0015 if lv == 0 else 0.0, name="door rail")
    gw, gh = CAB_W - 2 * fw - 0.004, CAB_H - 2 * fw - 0.004
    box(kit, (gw + 0.012, gh + 0.012, 0.003), (0.0, yc, dz + 0.002), GLASS, lv, w=0.0, name="glass")
    if lv <= 1:
        for s in (-1.0, 1.0):
            box(kit, (0.010, gh, 0.006), (s * (gw / 2.0 - 0.005), yc, dz + 0.0065), WHITE, lv, w=0.0, name="glazing stop")
            box(kit, (gw, 0.010, 0.006), (0.0, yc + s * (gh / 2.0 - 0.005), dz + 0.0065), WHITE, lv, w=0.0, name="glazing stop")
    n_k = 12 if lv == 0 else (4 if lv == 1 else 1)
    kl = (CAB_H - 0.01) / n_k
    for k in range(n_k):
        rcyl(kit, 0.0045, kl - (0.0015 if lv == 0 else 0.0), (-hw - 0.001, y0 + 0.005 + kl * (k + 0.5), dz), "y", WHITE, 8, name="hinge knuckle")
    if lv <= 1:
        hx, hy = hw - 0.026, yc + 0.05
        S.cyl_z(kit, 0.016, 0.006, (hx, hy, dz + DOOR_T / 2.0 + 0.003), CHROME, lv, w=0.001, name="lever rose")
        box(kit, (0.016, 0.075, 0.010), (hx, hy - 0.030, dz + DOOR_T / 2.0 + 0.012), CHROME, lv, w=0.003 if lv == 0 else 0.0, name="lever")
        S.cyl_z(kit, 0.007, 0.004, (hx, hy - 0.10, dz + DOOR_T / 2.0 + 0.002), CHROME, lv, w=0.0006, name="cam lock")
    # inside: the valve (rough brass), its inlet from the back, outlet down to the hose coupling, the red handwheel toward the door
    vx, vy, vz = 0.29, 1.40, -0.055
    if lv >= 2:
        box(kit, (0.40, 0.66, 0.050), (-0.13, 1.51, -0.060), CANVAS, lv, w=0.0, name="hose pack")
        box(kit, (0.40, 0.032, 0.010), (-0.13, 1.84, -0.085), FIRE, lv, w=0.0, name="rack bar")
        rcyl(kit, 0.036, 0.07, (vx, vy, vz), "z", BRASS, 8, name="valve")
        rcyl(kit, 0.048, 0.012, (vx, vy, vz + 0.088), "z", FIRE, 8, name="handwheel")
        return
    tube(kit, "supply nipple", [(vx, vy, CAB_BACK - 0.01), (vx, vy, vz - 0.01)], 0.0241, BRASS, lv, lo=10)
    S.lathe_axis(kit, [(0.0, -0.040), (0.026, -0.036), (0.036, -0.016), (0.038, 0.0), (0.036, 0.016), (0.026, 0.034), (0.0, 0.038)], (vx, vy, vz), "z", BRASS, lv,
                 name="valve body")
    tube(kit, "bonnet", [(vx, vy, vz + 0.030), (vx, vy, vz + 0.062)], 0.022, BRASS, lv, lo=10)
    hexnut(kit, (vx, vy, vz + 0.060), "z", 0.040, 0.012, BRASS, lv, name="packing nut")
    tube(kit, "stem", [(vx, vy, vz + 0.066), (vx, vy, vz + 0.092)], 0.0055, BRASS, lv, lo=8)
    wr = 0.048
    S.lathe_axis(kit, [(0.0, 0.0), (0.012, 0.0), (0.012, 0.016), (0.0, 0.016)], (vx, vy, vz + 0.080), "z", FIRE, lv, name="handwheel hub")
    nwr = S.arc_steps(wr, 2 * math.pi, lv, lo=12)
    rim = [(vx + wr * math.cos(2 * math.pi * k / nwr), vy + wr * math.sin(2 * math.pi * k / nwr), vz + 0.088) for k in range(nwr)]
    mrim = S.Mesh()
    S.sweep_section(mrim, rim + [rim[0]], S.circle(0.0055, 6 if lv == 0 else 4), closed_section=True)
    mrim.part(kit, "handwheel rim", FIRE)
    for k in range(4):
        a = math.pi / 4.0 + math.pi / 2.0 * k
        tube(kit, "spoke", [(vx, vy, vz + 0.088), (vx + (wr - 0.004) * math.cos(a), vy + (wr - 0.004) * math.sin(a), vz + 0.088)], 0.0045, FIRE, lv, lo=6)
    tube(kit, "outlet", [(vx, vy - 0.03, vz), (vx, vy - 0.075, vz)], 0.0241, BRASS, lv, lo=10)
    # the hose coupling (rocker lugs) and the hose leaving it down and left to the rack
    S.lathe_axis(kit, [(0.0, 0.0), (0.033, 0.0), (0.034, 0.012), (0.031, 0.040), (0.028, 0.055), (0.0, 0.055)], (vx, vy - 0.130, vz), "y", BRASS, lv,
                 name="hose coupling")
    if lv <= 1:
        for s in (-1.0, 1.0):
            box(kit, (0.012, 0.018, 0.010), (vx + s * 0.038, vy - 0.095, vz), BRASS, lv, w=0.0008 if lv == 0 else 0.0, name="rocker lug")
    hs = [(-0.030, -0.003), (0.030, -0.003), (0.030, 0.003), (-0.030, 0.003)]      # flattened 1-1/2 in hose, 60 x 6 mm
    # the rack: a red flat-bar bracket on the left side wall swinging out, a bar across with eight pins pointing out
    rx0, rx1, ry = -hw + 0.03, 0.17, 1.84
    box(kit, (0.006, 0.50, 0.040), (-hw + 0.005, ry - 0.20, -0.08), FIRE, lv, w=w0, name="rack bracket")
    box(kit, (rx1 - rx0, 0.032, 0.010), ((rx0 + rx1) / 2.0, ry, -0.085), FIRE, lv, w=w0, name="rack bar")
    folds = 8
    rnd = random.Random(1990)
    xs = [rx0 + 0.055 + (rx1 - rx0 - 0.09) * k / (folds - 1) for k in range(folds)]
    for k, x in enumerate(xs):
        if lv <= 1:
            tube(kit, "rack pin", [(x, ry + 0.010, -0.085), (x, ry + 0.010, -0.012)], 0.0045, FIRE, lv, lo=6)
        bottom = 1.16 + 0.06 * rnd.random() - 0.004 * k
        zb, zf = -0.072, -0.034 + 0.003 * rnd.random()
        r = (zf - zb) / 2.0
        nseg = S.arc_steps(r, math.pi, lv, lo=4)
        path = [(x, ry + 0.012, zb), (x, bottom + r, zb)]
        path += [(x, bottom + r - r * math.sin(math.pi * j / nseg), zb + r - r * math.cos(math.pi * j / nseg)) for j in range(1, nseg)]
        path += [(x, bottom + r, zf), (x, ry + 0.010, zf)]
        mh = S.Mesh()
        S.sweep_section(mh, path, hs, closed_section=True, cap0=True, cap1=True)
        mh.part(kit, "hose fold", CANVAS)
    # the hose's run from the last fold over to the coupling, and the nozzle on the first fold's end
    mr = S.Mesh()
    run = [(xs[-1], ry + 0.006, -0.034), (xs[-1] + 0.05, ry - 0.06, -0.040), (vx - 0.04, vy - 0.06, -0.052), (vx, vy - 0.165, vz)]
    pts = []
    for i in range(len(run) - 1):
        for j in range(6):
            pts.append(tuple(V(run[i]).lerp(V(run[i + 1]), j / 6.0)))
    pts.append(run[-1])
    S.sweep_section(mr, pts, S.circle(0.017, 10 if lv == 0 else 6), closed_section=True, cap0=True, cap1=True)
    mr.part(kit, "hose run", CANVAS)
    nx = xs[0] - 0.035
    S.lathe_axis(kit, [(0.0, 0.0), (0.024, 0.0), (0.025, 0.030), (0.020, 0.060), (0.014, 0.150), (0.010, 0.190), (0.0, 0.190)], (nx, ry - 0.245, -0.050), "y",
                 BRASS, lv, name="nozzle")


def hose_cabinet_meta(kit):
    kit.no_collider()
    kit.tag("wall_unit")
    D.anchor_u(kit, "valve", (0.29, 1.40, -0.055))
    kit.meta["wallOpening"] = {"x0": -CAB_W / 2.0, "x1": CAB_W / 2.0, "y0": CAB_Y0, "y1": CAB_Y0 + CAB_H, "depth": -CAB_BACK}
    kit.min_ceiling = 0.0


# ============================================================================= Kit_TimeClockStation (+ _OneIn)
TC_Y0 = 1.00                       # the station's bottom over the bumper rail (0.975)
BOARD_T = 0.019                    # 3/4 in plywood backboard
RACK_X = 0.285
POCKETS = 13
POCKET_P = 0.0445


def _rack(kit, x, lv, cards):
    """One 13-pocket steel card rack: back plate, side flanges, pockets with their angled lips; cards = pocket indices holding a card."""
    z0 = BOARD_T
    rw, rh, rd = 0.110, POCKETS * POCKET_P + 0.04, 0.045
    yc = TC_Y0 + rh / 2.0
    box(kit, (rw, rh, 0.0012), (x, yc, z0 + 0.0006), BEIGE, lv, w=0.0, name="rack back")
    for s in (-1.0, 1.0):
        box(kit, (0.0012, rh, rd), (x + s * (rw / 2.0 - 0.0006), yc, z0 + rd / 2.0), BEIGE, lv, w=0.0, name="rack flange")
    lips = POCKETS if lv <= 1 else 0
    for k in range(lips):
        yb = TC_Y0 + 0.012 + k * POCKET_P
        box(kit, (rw - 0.003, 0.030, 0.0012), (x, yb + 0.015, z0 + rd - 0.003), BEIGE, lv, w=0.0, rot=(-12, 0, 0), name="pocket lip")
        if lv == 0:
            box(kit, (rw - 0.003, 0.0012, rd - 0.004), (x, yb, z0 + rd / 2.0), BEIGE, lv, w=0.0, name="pocket floor")
    if lv >= 2:
        box(kit, (rw, rh, 0.002), (x, yc, z0 + rd - 0.002), BEIGE, lv, w=0.0, name="rack face")
    for k in (cards if lv <= 1 else ()):
        yb = TC_Y0 + 0.013 + k * POCKET_P
        lean = 6.0 + (k * 37 % 5) * 0.6
        cz = z0 + rd - 0.012 - 0.004 * ((k * 13) % 3)
        box(kit, (0.0858, 0.184, 0.0005), (x + 0.002 * ((k * 7) % 3 - 1), yb + 0.092, cz), PAPER, lv, w=0.0, rot=(-lean, 0, 0), name="time card")


CLOCK_Y0 = TC_Y0 + 0.170                # the recorder centred on the 0.62 m racks


def _clock(kit, lv, hh=4, mm=42, ss=10):
    """The electromechanical time recorder: a steel case with rounded front edges, the round dial window in a bright bezel, a white dial
    with bar indices (no numerals) and the hour, minute and red sweep hands, the card throat in a bright bezel on top with its dark slot,
    the side lock cylinder, the ribbon door's seam."""
    cw, ch, cd = 0.216, 0.280, 0.175
    z0 = BOARD_T
    yc = CLOCK_Y0 + ch / 2.0
    box(kit, (cw, ch, cd), (0.0, yc, z0 + cd / 2.0), BEIGE, lv, w=0.008 if lv == 0 else (0.004 if lv == 1 else 0.0), name="case")
    box(kit, (cw + 0.010, 0.010, 0.016), (0.0, CLOCK_Y0 + 0.005, z0 + 0.008), BEIGE, lv, w=0.0, name="wall bracket")
    fz = z0 + cd
    dcy = CLOCK_Y0 + 0.150
    R = 0.074
    # the dial stands on the case front inside its bezel ring (the case is solid: nothing may sit behind its front face)
    S.lathe_axis(kit, [(R + 0.007, 0.0), (R + 0.007, 0.004), (R + 0.004, 0.0085), (R, 0.009), (R, 0.0)], (0.0, dcy, fz), "z", CHROME, lv, name="dial bezel")
    S.cyl_z(kit, R, 0.002, (0.0, dcy, fz + 0.001), PWHITE, lv, w=0.0, name="dial")
    if lv <= 1:
        for k in range(60):
            a = math.pi / 2.0 - 2 * math.pi * k / 60.0
            big = k % 5 == 0
            if lv == 1 and not big:
                continue
            L = 0.011 if big else 0.005
            rr = R - 0.008 - L / 2.0
            w = 0.0032 if big else 0.0011
            box(kit, (w, L, 0.0006), (rr * math.cos(a), dcy + rr * math.sin(a), fz + 0.0023), PBLACK, lv, w=0.0, rot=(0, 0, math.degrees(a) - 90.0), name="index")
        for (val, length, wid, slot, dzz) in (((hh % 12) / 12.0 + mm / 720.0, 0.040, 0.0045, PBLACK, -0.0030), (mm / 60.0 + ss / 3600.0, 0.060, 0.0032, PBLACK, -0.0038),
                                              (ss / 60.0, 0.064, 0.0012, PRED, -0.0046)):
            a = math.pi / 2.0 - 2 * math.pi * val
            box(kit, (wid, length, 0.0008), ((length / 2.0 - 0.008) * math.cos(a), dcy + (length / 2.0 - 0.008) * math.sin(a), fz - dzz), slot, lv, w=0.0,
                rot=(0, 0, math.degrees(a) - 90.0), name="hand")
        S.cyl_z(kit, 0.004, 0.003, (0.0, dcy, fz + 0.0052), PBLACK, lv, w=0.0, name="hand boss")
    # the domed glass over the dial
    S.lathe_axis(kit, [(R, 0.0), (R * 0.85, 0.005), (R * 0.5, 0.0085), (0.0, 0.0095)], (0.0, dcy, fz + 0.0085), "z", GLASS, lv, name="dial glass", close_bottom=False)
    # the card throat on top (cards drop in from above), a bright bezel with the dark slot
    ty = CLOCK_Y0 + ch
    box(kit, (0.140, 0.014, 0.060), (0.0, ty + 0.007, z0 + cd - 0.045), CHROME, lv, w=0.002 if lv == 0 else 0.0, name="throat bezel")
    box(kit, (0.100, 0.003, 0.010), (0.0, ty + 0.0128, z0 + cd - 0.045), PBLACK, lv, w=0.0, name="card slot")
    if lv <= 1:
        S.cyl_x(kit, 0.009, 0.006, (cw / 2.0 + 0.002, CLOCK_Y0 + 0.215, z0 + cd - 0.040), CHROME, lv, w=0.0008, name="lock cylinder")
        box(kit, (0.0012, 0.004, 0.012), (cw / 2.0 + 0.0052, CLOCK_Y0 + 0.215, z0 + cd - 0.040), PBLACK, lv, w=0.0, name="keyway")
        box(kit, (0.0008, 0.200, 0.0012), (cw / 2.0 + 0.0002, yc - 0.01, z0 + cd - 0.075), PBLACK, lv, w=0.0, name="ribbon door seam")
        box(kit, (0.0008, 0.0012, 0.110), (cw / 2.0 + 0.0002, yc - 0.11, z0 + cd - 0.075 - 0.055 + 0.055), PBLACK, lv, w=0.0, name="ribbon door seam b")


def time_clock(kit, lv, one_in=False):
    """Kit_TimeClockStation: a 3/4 in plywood backboard 0.80 x 0.70 on the block wall, the time recorder centred (its card throat at about
    1.29 m), an IN rack left and an OUT rack right; allOut: ten blank cards in OUT, IN empty; _OneIn: one card stands in IN (63's event;
    card faces the graphic line (pingmian shijue), strings the narrative line (xushi sheji), held). Origin on the wall plane at the floor, x centred."""
    box(kit, (0.80, 0.70, BOARD_T), (0.0, TC_Y0 - 0.04 + 0.35, BOARD_T / 2.0), PLY, lv, w=0.0015 if lv == 0 else 0.0, name="backboard")
    if lv <= 1:
        for (x, y) in ((-0.36, TC_Y0 - 0.005), (0.36, TC_Y0 - 0.005), (-0.36, TC_Y0 + 0.615), (0.36, TC_Y0 + 0.615)):
            stud(kit, (x, y, BOARD_T + 0.001), "z", 0.006, 0.002, ZINC, lv, name="board screw") if lv == 0 else None
    _clock(kit, lv)
    _rack(kit, -RACK_X, lv, [6] if one_in else [])
    _rack(kit, RACK_X, lv, [0, 1, 2, 4, 5, 6, 8, 9, 10, 12])


def time_clock_meta(kit):
    kit.no_collider()
    kit.tag("wall_unit")
    D.anchor_u(kit, "card_slot", (0.0, CLOCK_Y0 + 0.293, BOARD_T + 0.130))
    kit.min_ceiling = 0.0


# ============================================================================= the janitor's closet (63 3.2 RN-JAN; LS103)
SINK_W, SINK_H = 0.610, 0.254      # 24 x 24 x 10 in floor basin (LS103: 24 x 24 x 10-12 in, molded stone or terrazzo, by the door)
SINK_FY = 1.168                    # the faucet 36 in over the curb (LS103)


def _rr_normals(pts):
    """Outward normals (x, z) of a closed CCW-or-CW outline round its centroid."""
    n = len(pts)
    cx = sum(p[0] for p in pts) / n
    cz = sum(p[1] for p in pts) / n
    out = []
    for j in range(n):
        a, b = pts[j - 1], pts[(j + 1) % n]
        tx, tz = b[0] - a[0], b[1] - a[1]
        nx, nz = tz, -tx
        if nx * (pts[j][0] - cx) + nz * (pts[j][1] - cz) < 0:
            nx, nz = -nx, -nz
        L = math.hypot(nx, nz) or 1.0
        out.append((nx / L, nz / L))
    return out


def _loft(m, rings, zc):
    """Quads between consecutive rounded-square rings (lists of (half-size, corner r, y)), equal point counts, wound by the profile's
    normal (outer wall out, curb up, bowl in, floor up); metre UVs (u round the ring, v down the profile). Rings centred on (0, zc)."""
    per = rings[0][3]
    outl = [rrect(2 * h, 2 * h, r, per) for (h, r, y, _p) in rings]
    nrm = _rr_normals(outl[0])
    vacc = [0.0]
    for i in range(1, len(rings)):
        vacc.append(vacc[-1] + math.hypot(rings[i][0] - rings[i - 1][0], rings[i][2] - rings[i - 1][2]))
    for i in range(len(rings) - 1):
        (h0, r0, y0, _a), (h1, r1, y1, _b) = rings[i], rings[i + 1]
        ds, dy = h1 - h0, y1 - y0
        nr, ny = dy, -ds
        P0 = [(p[0], y0, zc + p[1]) for p in outl[i]]
        P1 = [(p[0], y1, zc + p[1]) for p in outl[i + 1]]
        ar = arcs([(p[0], p[1]) for p in outl[i]])
        n = len(P0)
        for j in range(n):
            j1 = (j + 1) % n
            nx, nz = nrm[j]
            want = (nx * nr, ny, nz * nr)
            D.qd(m, P0[j], P0[j1], P1[j1], P1[j], (ar[j], vacc[i]), (ar[j + 1], vacc[i]), (ar[j + 1], vacc[i + 1]), (ar[j], vacc[i + 1]), want)


def mop_sink(kit, lv):
    """Kit_MopSink: a 24 x 24 x 10 in molded-stone floor basin with a 50 mm rounded curb, a floor falling to a 3 in dome strainer, stainless
    caps on the three exposed curbs, and the wall-mounted service faucet 36 in over the curb (two valves on 8 in centres with four-arm
    handles, the vacuum breaker on the riser, a spout with its pail hook and wall brace, 3/4 in hose thread) with a black rubber hose
    hanging into the basin. Origin on the wall plane at the floor, x centred on the basin; the basin's back on the wall (+Z out)."""
    per = 3 if lv == 0 else (2 if lv == 1 else 1)
    hw = SINK_W / 2.0
    rings = [(hw, 0.040, 0.0, per), (hw, 0.040, SINK_H - 0.014, per), (hw - 0.004, 0.036, SINK_H - 0.003, per), (hw - 0.012, 0.030, SINK_H, per),
             (hw - 0.036, 0.024, SINK_H, per), (hw - 0.046, 0.022, SINK_H - 0.006, per), (hw - 0.050, 0.020, SINK_H - 0.020, per),
             (hw - 0.058, 0.020, 0.072, per), (hw - 0.074, 0.034, 0.050, per), (0.070, 0.050, 0.040, per), (0.047, 0.040, 0.036, per)]
    if lv == 2:
        rings = [rings[0], rings[1], rings[3], rings[4], rings[6], rings[7], rings[9], rings[10]]
    m = S.Mesh()
    _loft(m, rings, hw)
    m.part(kit, "basin", STONE)
    # the dome strainer
    S.lathe_axis(kit, [(0.0, 0.0), (0.050, 0.0), (0.050, 0.002), (0.040, 0.0045), (0.0, 0.006)], (0.0, 0.035, hw), "y", ZINC, lv, name="strainer")
    if lv == 0:
        for k in range(8):
            a = math.pi * k / 4.0
            box(kit, (0.004, 0.0008, 0.020), (0.026 * math.cos(a), 0.0402, hw + 0.026 * math.sin(a)), PBLACK, lv, w=0.0, rot=(0, -math.degrees(a) + 90.0, 0), name="slot")
    # stainless caps (18 ga, an inverted U) on the front and both side curbs
    if lv <= 1:
        sec = [(-0.0290, -0.030), (-0.0278, -0.030), (-0.0278, 0.0), (0.0278, 0.0), (0.0278, -0.030), (0.0290, -0.030), (0.0290, 0.0012), (-0.0290, 0.0012)]
        if lv == 1:
            sec = [(-0.0290, -0.030), (0.0290, -0.030), (0.0290, 0.0012), (-0.0290, 0.0012)]
        mc = S.Mesh()
        cl = hw - 0.025
        prism(mc, sec, (0.0, SINK_H, hw + cl), (0, 0, -1), (0, 1, 0), (1, 0, 0), -hw + 0.040, hw - 0.040, caps=(False, False))
        for s in (-1.0, 1.0):
            prism(mc, sec, (s * cl, SINK_H, 0.0), (s, 0, 0), (0, 1, 0), (0, 0, s), 0.030 if s > 0 else -(SINK_W - 0.040), (SINK_W - 0.040) if s > 0 else -0.030,
                  caps=(False, False))
        mc.part(kit, "curb caps", ZINC)
    # the faucet
    fy = SINK_FY
    for s in (-1.0, 1.0):
        x = s * 0.1016
        S.lathe_axis(kit, [(0.0, 0.0), (0.032, 0.0), (0.031, 0.004), (0.022, 0.010), (0.0, 0.011)], (x, fy, 0.0), "z", CHROME, lv, name="flange")
        tube(kit, "supply arm", [(x, fy, 0.005), (x, fy, 0.060)], 0.011, CHROME, lv, lo=10)
        S.lathe_axis(kit, [(0.0, -0.030), (0.017, -0.030), (0.018, -0.010), (0.017, 0.020), (0.013, 0.030), (0.0, 0.031)], (x, fy, 0.066), "y", CHROME, lv, name="valve body")
        hexnut(kit, (x, fy + 0.036, 0.066), "y", 0.022, 0.010, CHROME, lv, name="bonnet nut")
        tube(kit, "stem", [(x, fy + 0.040, 0.066), (x, fy + 0.058, 0.066)], 0.004, CHROME, lv, lo=8)
        rcyl(kit, 0.010, 0.014, (x, fy + 0.062, 0.066), "y", CHROME, 16 if lv == 0 else 8, name="handle hub")
        if lv <= 1:
            for k in range(4):
                a = math.pi / 4.0 + math.pi / 2.0 * k
                e = (x + 0.036 * math.cos(a), fy + 0.064, 0.066 + 0.036 * math.sin(a))
                tube(kit, "handle arm", [(x, fy + 0.064, 0.066), e], 0.0045, CHROME, lv, lo=8)
    tube(kit, "cross body", [(-0.090, fy - 0.022, 0.066), (0.090, fy - 0.022, 0.066)], 0.013, CHROME, lv, lo=10)
    tube(kit, "riser", [(0.0, fy - 0.022, 0.066), (0.0, fy + 0.048, 0.066)], 0.012, CHROME, lv, lo=10)
    S.lathe_axis(kit, [(0.0, 0.0), (0.024, 0.0), (0.025, 0.006), (0.025, 0.048), (0.021, 0.058), (0.010, 0.064), (0.0, 0.065)], (0.0, fy + 0.046, 0.066), "y", CHROME, lv,
                 name="vacuum breaker")
    nb = S.arc_steps(0.05, math.pi / 2.0, lv, lo=4)
    sp = [(0.0, fy + 0.074, 0.066)] + [(0.0, fy + 0.074 - 0.05 + 0.05 * math.cos(math.pi / 2.0 * k / nb), 0.110 + 0.05 * math.sin(math.pi / 2.0 * k / nb)) for k in range(nb + 1)]
    sp += [(0.0, fy - 0.110, 0.160)]
    tube(kit, "spout", sp, 0.011, CHROME, lv, lo=10)
    S.lathe_axis(kit, [(0.0, 0.0), (0.0140, 0.0), (0.0145, 0.004), (0.0145, 0.024), (0.0135, 0.028), (0.0, 0.028)], (0.0, fy - 0.138, 0.160), "y", CHROME, lv, name="hose thread")
    if lv <= 1:
        tube(kit, "pail hook", [(0.0, fy - 0.060, 0.170), (0.0, fy - 0.068, 0.200), (0.0, fy - 0.050, 0.212)], 0.0035, CHROME, lv, lo=6)
        tube(kit, "wall brace", [(0.0, fy - 0.090, 0.150), (0.0, fy - 0.205, 0.006)], 0.005, CHROME, lv, lo=8)
        rcyl(kit, 0.016, 0.004, (0.0, fy - 0.208, 0.002), "z", CHROME, 16, name="brace flange")
    # the hose: from the thread, down into the basin, a loose turn on its floor
    path = [(0.0, fy - 0.140, 0.160), (0.004, fy - 0.300, 0.165), (0.020, 0.62, 0.190), (0.050, 0.34, 0.235), (0.085, 0.14, 0.300), (0.110, 0.072, 0.360),
            (0.150, 0.066, 0.420), (0.190, 0.066, 0.380), (0.180, 0.066, 0.300), (0.120, 0.066, 0.260), (0.040, 0.068, 0.290)]
    if lv == 2:
        path = path[:5] + [path[6], path[8]]
    pts = []
    for i in range(len(path) - 1):
        steps = 3 if lv == 0 else 1
        for k in range(steps):
            pts.append(tuple(V(path[i]).lerp(V(path[i + 1]), k / float(steps))))
    pts.append(path[-1])
    ptube(kit, "hose", pts, 0.011, RUBBER, lv, n0=10)


def mop_sink_meta(kit):
    D.collider_u(kit, (0.0, SINK_H / 2.0, SINK_W / 2.0), (SINK_W, SINK_H, SINK_W))
    kit.tag("wall_unit")
    D.anchor_u(kit, "basin", (0.0, 0.040, SINK_W / 2.0))
    kit.min_ceiling = 0.0


RACK_Y = 1.55                      # the shelf (63: shelf at 1.55; LS103: wet mops hung at 5 ft to drip into the sink)
RACK_D = 0.203                     # 8 in deep, 36 in long
HOLD_Z = 0.232                     # the handles' axis off the wall
HOLDERS = (-0.33, -0.11, 0.11, 0.33)


def _mop_head(kit, x, y_top, lv, seed):
    """A cut-end cotton wet mop (24 oz) in its jaw clamp: the yarn bundle as an irregular loft, loose strands round it (Prop_Canvas)."""
    rnd = random.Random(seed)
    z = HOLD_Z
    box(kit, (0.120, 0.040, 0.022), (x, y_top - 0.020, z), ZINC, lv, w=0.002 if lv == 0 else 0.0, name="mop clamp")
    if lv <= 1:
        tube(kit, "clamp lever", [(x + 0.060, y_top - 0.010, z + 0.012), (x + 0.065, y_top + 0.030, z + 0.016)], 0.004, ZINC, lv, lo=6)
    n = 14 if lv == 0 else (8 if lv == 1 else 6)
    prof = [(0.062, 0.016, y_top - 0.040), (0.066, 0.044, y_top - 0.085), (0.060, 0.054, y_top - 0.165), (0.052, 0.050, y_top - 0.250), (0.040, 0.038, y_top - 0.320),
            (0.014, 0.012, y_top - 0.358)]
    if lv == 2:
        prof = [prof[0], prof[2], prof[4], prof[5]]
    rings = []
    for (a, b, y) in prof:
        ring = []
        for k in range(n):
            t = 2 * math.pi * k / n
            j = 1.0 + 0.10 * (rnd.random() - 0.5) * (1.0 if lv == 0 else 0.0)
            ring.append((x + a * j * math.cos(t), y + 0.006 * (rnd.random() - 0.5), z + b * j * math.sin(t)))
        rings.append(ring)
    m = S.Mesh()
    vacc = 0.0
    for i in range(len(rings) - 1):
        for k in range(n):
            k1 = (k + 1) % n
            a, b, c, d = rings[i][k], rings[i][k1], rings[i + 1][k1], rings[i + 1][k]
            mid = (V(a) + V(b) + V(c) + V(d)) / 4.0
            want = tuple(mid - V((x, mid.y, z)))
            D.qd(m, a, b, c, d, (k * 0.02, vacc), ((k + 1) * 0.02, vacc), ((k + 1) * 0.02, vacc + 0.08), (k * 0.02, vacc + 0.08), want)
        vacc += 0.08
    m.part(kit, "mop yarn", CANVAS)
    if lv == 0:
        for k in range(10):
            t = 2 * math.pi * (k + rnd.random() * 0.5) / 10.0
            r0 = 0.058
            y0 = y_top - 0.06 - 0.10 * rnd.random()
            L = 0.22 + 0.14 * rnd.random()
            p0 = (x + r0 * math.cos(t), y0, z + 0.048 * math.sin(t))
            p1 = (x + (r0 + 0.010) * math.cos(t), y0 - L * 0.5, z + 0.054 * math.sin(t))
            p2 = (x + (r0 + 0.002) * math.cos(t) + 0.01 * (rnd.random() - 0.5), y0 - L, z + 0.050 * math.sin(t))
            ms = S.Mesh()
            S.sweep_section(ms, [p0, p1, p2], [(-0.004, -0.0015), (0.004, -0.0015), (0.004, 0.0015), (-0.004, 0.0015)], cap1=True)
            ms.part(kit, "mop strand", CANVAS)


def _holder(kit, x, lv):
    box(kit, (0.050, 0.035, 0.0015), (x, RACK_Y - 0.045, RACK_D + 0.0018), ZINC, lv, w=0.0, name="holder plate")
    if lv <= 1:
        for s in (-1.0, 1.0):
            box(kit, (0.0015, 0.035, 0.036), (x + s * 0.024, RACK_Y - 0.045, RACK_D + 0.019), ZINC, lv, w=0.0, name="holder cheek")
            rcyl(kit, 0.0105, 0.030, (x + s * 0.0225, RACK_Y - 0.045, HOLD_Z), "y", RUBBER, 12 if lv == 0 else 8, name="holder cam")


def mop_rack(kit, lv, one_out=False):
    """Kit_MopRack: a 36 in mop and broom holder shelf at 1.55 m (18 ga stainless, 8 in deep, hemmed front lip, back upstand, two end
    gussets), four spring cam holders on the lip and three rag hooks under it (LS103), holding two wet mops (54 in oak handles, cut-end
    cotton heads hanging to 0.30 m over the sink), a corn broom and a floor squeegee, a rag on the middle hook; _OneOut: the second mop
    out (its holder empty). Origin on the wall plane at the floor, x centred."""
    t = 0.0012
    box(kit, (0.914, t, RACK_D), (0.0, RACK_Y - t / 2.0, RACK_D / 2.0), ZINC, lv, w=0.0, name="shelf")
    box(kit, (0.914, 0.050, t), (0.0, RACK_Y - 0.025, RACK_D + t / 2.0), ZINC, lv, w=0.0, name="lip")
    box(kit, (0.914, 0.050, t), (0.0, RACK_Y + 0.025, t / 2.0), ZINC, lv, w=0.0, name="upstand")
    if lv <= 1:
        box(kit, (0.914, 0.004, 0.006), (0.0, RACK_Y - 0.050, RACK_D - 0.002), ZINC, lv, w=0.0, name="hem")
        mg = S.Mesh()
        for s in (-1.0, 1.0):
            prism(mg, [(0.0, RACK_Y - t), (0.0, RACK_Y - 0.150), (0.150, RACK_Y - t)], (s * 0.452, 0.0, 0.0), (0, 0, 1), (0, 1, 0), (-1, 0, 0), -t / 2.0, t / 2.0)
        mg.part(kit, "gussets", ZINC)
    for x in HOLDERS:
        _holder(kit, x, lv)
    if lv <= 1:
        for x in (-0.22, 0.0, 0.22):
            tube(kit, "rag hook", [(x, RACK_Y - 0.040, RACK_D + 0.002), (x, RACK_Y - 0.080, RACK_D + 0.030), (x, RACK_Y - 0.100, RACK_D + 0.034),
                                   (x, RACK_Y - 0.090, RACK_D + 0.055)], 0.0028, ZINC, lv, lo=6)
    # two wet mops (the second out on _OneOut)
    for (x, seed) in ((HOLDERS[0], 11), (HOLDERS[2], 23)):
        if one_out and x == HOLDERS[2]:
            continue
        tube(kit, "mop handle", [(x, 0.700, HOLD_Z), (x, 2.040, HOLD_Z)], 0.0120, OAK, lv, lo=10)
        if lv <= 1:
            S.lathe_axis(kit, [(0.0, 0.0), (0.012, 0.0), (0.011, 0.012), (0.006, 0.020), (0.0, 0.022)], (x, 2.040, HOLD_Z), "y", OAK, lv, name="handle end")
        _mop_head(kit, x, 0.700, lv, seed)
    # the corn broom (head down)
    xb = HOLDERS[3]
    tube(kit, "broom handle", [(xb, 0.700, HOLD_Z), (xb, 1.640, HOLD_Z)], 0.0120, OAK, lv, lo=10)
    rcyl(kit, 0.016, 0.040, (xb, 0.690, HOLD_Z), "y", ZINC, 16 if lv == 0 else 8, name="broom ferrule")
    nb = 6 if lv == 0 else 4
    bw = [(0.040, 0.016, 0.670), (0.090, 0.028, 0.600), (0.130, 0.030, 0.480), (0.150, 0.026, 0.300), (0.152, 0.018, 0.282)]
    if lv == 2:
        bw = [bw[0], bw[2], bw[4]]
    mb = S.Mesh()
    for i in range(len(bw) - 1):
        (w0, d0, y0), (w1, d1, y1) = bw[i], bw[i + 1]
        q0 = [(xb + w0, y0, HOLD_Z + d0), (xb - w0, y0, HOLD_Z + d0), (xb - w0, y0, HOLD_Z - d0), (xb + w0, y0, HOLD_Z - d0)]
        q1 = [(xb + w1, y1, HOLD_Z + d1), (xb - w1, y1, HOLD_Z + d1), (xb - w1, y1, HOLD_Z - d1), (xb + w1, y1, HOLD_Z - d1)]
        for k in range(4):
            k1 = (k + 1) % 4
            mid = (V(q0[k]) + V(q0[k1]) + V(q1[k1]) + V(q1[k])) / 4.0
            D.qd(mb, q0[k], q0[k1], q1[k1], q1[k], (k * 0.1, y0), (k * 0.1 + 0.1, y0), (k * 0.1 + 0.1, y1), (k * 0.1, y1), tuple(mid - V((xb, mid.y, HOLD_Z))))
    D.qd(mb, (xb + bw[-1][0], bw[-1][2], HOLD_Z + bw[-1][1]), (xb - bw[-1][0], bw[-1][2], HOLD_Z + bw[-1][1]), (xb - bw[-1][0], bw[-1][2], HOLD_Z - bw[-1][1]),
         (xb + bw[-1][0], bw[-1][2], HOLD_Z - bw[-1][1]), (0, 0), (0.3, 0), (0.3, 0.04), (0, 0.04), (0.0, -1.0, 0.0))
    mb.part(kit, "broom straw", CANVAS)
    if lv <= 1:
        for (y, w) in ((0.610, 0.086), (0.560, 0.104), (0.510, 0.120)):
            box(kit, (2 * w, 0.004, 0.062), (xb, y, HOLD_Z), PRED, lv, w=0.0, name="broom stitching")
    # the floor squeegee (18 in): handle, steel frame, rubber blade
    xs = HOLDERS[1]
    tube(kit, "squeegee handle", [(xs, 0.420, HOLD_Z), (xs, 1.780, HOLD_Z)], 0.0120, OAK, lv, lo=10)
    box(kit, (0.457, 0.030, 0.026), (xs, 0.400, HOLD_Z), PAINT, lv, w=0.002 if lv == 0 else 0.0, name="squeegee frame")
    box(kit, (0.457, 0.050, 0.008), (xs, 0.360, HOLD_Z), RUBBER, lv, w=0.0, name="squeegee blade")
    if lv <= 1:
        tube(kit, "squeegee brace", [(xs, 0.415, HOLD_Z), (xs, 0.560, HOLD_Z + 0.004)], 0.008, PAINT, lv, lo=6)
    # a rag over the middle hook
    if lv <= 1:
        mr = S.Mesh()
        rag = [(0.16, RACK_Y - 0.098, RACK_D + 0.040), (0.28, RACK_Y - 0.098, RACK_D + 0.040)]
        for (dz, y) in ((0.010, RACK_Y - 0.30), (0.055, RACK_Y - 0.26)):
            a, b = rag
            flat_quad(mr, (a[0], y, a[2] + dz - 0.02), (b[0] + 0.01, y - 0.01, b[2] + dz - 0.02), (b[0], b[1], b[2]), (a[0], a[1], a[2]), (0.0, 0.0, 1.0 if dz > 0.03 else -1.0))
            flat_quad(mr, (a[0], a[1], a[2]), (b[0], b[1], b[2]), (b[0] + 0.01, y - 0.01, b[2] + dz - 0.02), (a[0], y, a[2] + dz - 0.02), (0.0, 0.0, -1.0 if dz > 0.03 else 1.0))
        mr.part(kit, "rag", CANVAS)


def mop_rack_meta(kit):
    kit.no_collider()
    kit.tag("wall_unit")
    kit.min_ceiling = 0.0


PGREY = "Prop_PlasticGrey"


def floor_machine(kit, lv):
    """Kit_FloorMachine: a 17 in rotary floor machine (175 rpm, 1-1/2 hp class) stored upright on its brush: the black brush block and its
    bristle ring, a grey non-marking bumper round the steel deck, the gear case and the ribbed motor with its vented cap, two 5 in transport
    wheels on a bracket at the back, the yoke and a chrome 1-1/4 in handle tube to a T-bar with black grips, the switch box and its bail
    levers, and 50 ft of black cord coiled over the handle's two hooks with the plug hanging (63 Kit_FloorMachine; LS103). Origin on the
    floor at the brush's centre; the handle rises at -Z."""
    S.lathe_axis(kit, [(0.211, 0.0), (0.211, 0.014), (0.0, 0.0145)], (0.0, 0.0, 0.0), "y", RUBBER, lv, name="bristles", close_top=False)
    S.lathe_axis(kit, [(0.213, 0.0), (0.215, 0.004), (0.215, 0.030), (0.211, 0.034), (0.0, 0.034)], (0.0, 0.014, 0.0), "y", PBLACK, lv, name="brush block", close_bottom=False)
    S.lathe_axis(kit, [(0.236, 0.0), (0.252, 0.008), (0.256, 0.026), (0.252, 0.046), (0.236, 0.054)], (0.0, 0.052, 0.0), "y", PGREY, lv, name="bumper",
                 close_top=False, close_bottom=False)
    S.lathe_axis(kit, [(0.238, 0.0), (0.238, 0.060), (0.228, 0.074), (0.176, 0.108), (0.150, 0.114), (0.0, 0.116)], (0.0, 0.052, 0.0), "y", PUTTY, lv, name="deck")
    S.lathe_axis(kit, [(0.142, 0.0), (0.142, 0.048), (0.138, 0.052), (0.0, 0.052)], (0.0, 0.166, 0.0), "y", PUTTY, lv, name="gear case", close_bottom=False)
    S.lathe_axis(kit, [(0.115, 0.0), (0.115, 0.205), (0.106, 0.232), (0.072, 0.248), (0.0, 0.252)], (0.0, 0.218, 0.0), "y", PUTTY, lv, name="motor")
    if lv <= 1:
        for y in (0.262, 0.330):
            rcyl(kit, 0.1195, 0.007, (0.0, y, 0.0), "y", PUTTY, 48 if lv == 0 else 24, name="cooling rib")
    if lv == 0:
        for k in range(10):
            a = 2 * math.pi * k / 10.0
            box(kit, (0.030, 0.002, 0.006), (0.050 * math.cos(a), 0.4655, 0.050 * math.sin(a)), PBLACK, lv, w=0.0, rot=(0, -math.degrees(a), 0), name="vent")
    # transport wheels on the bracket at the back
    box(kit, (0.34, 0.012, 0.060), (0.0, 0.112, -0.205), PUTTY, lv, w=0.0015 if lv == 0 else 0.0, name="wheel bracket")
    for s in (-1.0, 1.0):
        box(kit, (0.006, 0.060, 0.050), (s * 0.128, 0.084, -0.215), PUTTY, lv, w=0.0, name="wheel cheek")
        kit.lathe([(0.0545, 0.0), (0.064, 0.006), (0.064, 0.028), (0.0545, 0.034)], S.ub((s * 0.150 - 0.017, 0.068, -0.222)), RUBBER, verts=28 if lv == 0 else 14,
                  rot=(0, -90, 0), name="wheel", close_top=False, close_bottom=False)
        rcyl(kit, 0.0545, 0.030, (s * 0.150, 0.068, -0.222), "x", PGREY, 28 if lv == 0 else 14, name="wheel hub")
    # the yoke and the handle
    hz0, hz1 = -0.150, -0.185
    for s in (-1.0, 1.0):
        mb = S.Mesh()
        bar(mb, (s * 0.146, 0.192, -0.040), (s * 0.040, 0.360, hz0 + 0.004), 0.030, 0.008, (0, 0, 1))
        mb.part(kit, "yoke arm", PUTTY)
        rcyl(kit, 0.016, 0.012, (s * 0.150, 0.192, -0.040), "x", PUTTY, 12, name="yoke pivot")
    rcyl(kit, 0.024, 0.060, (0.0, 0.370, hz0), "y", PUTTY, 16 if lv == 0 else 8, name="handle socket")
    tube(kit, "handle tube", [(0.0, 0.370, hz0), (0.0, 1.080, hz1)], 0.0159, CHROME, lv, lo=10)
    tube(kit, "handle bar", [(-0.250, 1.120, hz1 - 0.020), (-0.180, 1.100, hz1), (0.180, 1.100, hz1), (0.250, 1.120, hz1 - 0.020)], 0.0127, CHROME, lv, lo=10)
    for s in (-1.0, 1.0):
        tube(kit, "grip", [(s * 0.150, 1.102, hz1), (s * 0.252, 1.121, hz1 - 0.021)], 0.0170, PBLACK, lv, lo=10)
        if lv <= 1:
            tube(kit, "bail lever", [(s * 0.060, 1.072, hz1 + 0.010), (s * 0.110, 1.062, hz1 + 0.030), (s * 0.230, 1.080, hz1 + 0.016)], 0.0050, CHROME, lv, lo=6)
    box(kit, (0.110, 0.080, 0.064), (0.0, 1.080, hz1), PBLACK, lv, w=0.004 if lv == 0 else 0.0, name="switch box")
    if lv <= 1:
        tube(kit, "strain relief", [(0.0, 1.040, hz1 - 0.020), (0.0, 1.010, hz1 - 0.040)], 0.008, PBLACK, lv, lo=8)
    # the cord: from the switch box down behind the tube, coiled four times over the two hooks, the plug hanging
    yt, yb = 0.960, 0.560
    for y in (yt, yb):
        f = (0.0, y, hz0 + (hz1 - hz0) * (y - 0.37) / 0.71)
        tube(kit, "cord hook", [f, (0.0, y - 0.010, f[2] - 0.040), (0.0, y + 0.012, f[2] - 0.058)], 0.0040, CHROME, lv, lo=6)
    zt = hz0 + (hz1 - hz0) * (yt - 0.37) / 0.71 - 0.040
    zb = hz0 + (hz1 - hz0) * (yb - 0.37) / 0.71 - 0.040
    loops = 4 if lv <= 1 else 1
    rc = 0.022
    nseg = 6 if lv == 0 else 3
    for k in range(loops):
        x = -0.018 + 0.012 * k
        pts = []
        for j in range(nseg + 1):
            a = math.pi * j / nseg
            pts.append((x, yt + rc * math.sin(a) * 0.6 + 0.012, zt + rc * math.cos(a)))
        for j in range(nseg + 1):
            a = math.pi * j / nseg
            pts.append((x + 0.004, yb - rc * math.sin(a) * 0.6 - 0.012 - 0.01 * (k % 2), zb - rc * math.cos(a)))
        pts.append(pts[0])
        mc = S.Mesh()
        S.sweep_section(mc, pts, S.circle(0.0045, 6 if lv == 0 else 4))
        mc.part(kit, "cord loop", PBLACK)
    tube(kit, "cord to box", [(0.0, 1.010, hz1 - 0.040), (0.004, 0.990, zt - 0.012)], 0.0045, PBLACK, lv, lo=6)
    tail = [(0.022, yb - 0.030, zb - 0.020), (0.028, 0.420, zb - 0.030), (0.034, 0.330, zb - 0.020)]
    tube(kit, "cord tail", tail, 0.0045, PBLACK, lv, lo=6)
    box(kit, (0.030, 0.050, 0.022), (0.034, 0.300, zb - 0.020), PBLACK, lv, w=0.002 if lv == 0 else 0.0, name="plug")
    if lv == 0:
        for s in (-1.0, 1.0):
            box(kit, (0.0015, 0.016, 0.006), (0.034 + s * 0.006, 0.268, zb - 0.020), ZINC, lv, w=0.0, name="plug blade")


def floor_machine_meta(kit):
    D.collider_u(kit, (0.0, 0.24, 0.0), (0.52, 0.48, 0.52))
    kit.min_ceiling = 0.0


def door_wedge(kit, lv):
    """Kit_DoorWedge: a hardwood (oak) door wedge 150 x 50 x 40 mm, its edges eased, the tip worn round (63 Kit_DoorWedge; LS119).
    Origin on the floor at its centre; the tip toward +Z (under the leaf)."""
    if lv == 0:
        sec = [(-0.075, 0.0), (0.075, 0.0), (0.075, 0.0025), (0.071, 0.0048), (-0.071, 0.0400), (-0.075, 0.0370)]
    else:
        sec = [(-0.075, 0.0), (0.075, 0.0), (0.075, 0.003), (-0.075, 0.040)]
    m = S.Mesh()
    prism(m, sec, (0.0, 0.0, 0.0), (0, 0, 1), (0, 1, 0), (-1, 0, 0), -0.025, 0.025)
    m.part(kit, "wedge", OAK)


def door_wedge_meta(kit):
    kit.no_collider()
    kit.min_ceiling = 0.0


# ============================================================================= SP7's end kits, built NOT wired (Red's #3 open; 30_plan S7-11, S7-14)
def _work_lamp(kit, z, yc, lv, lit=True):
    """One caged work lamp on the string: a yellow pigtail, the moulded rubber lampholder, a 150 W A21 lamp, the steel wire guard
    clamped to the holder (six bows, a girth ring, a bottom ring); the lamp's centre 0.265 m under the tie line (FarShore's numbers)."""
    tube(kit, "pigtail", [(0.0, yc, z), (0.0, yc - 0.072, z)], 0.0045, YELLOW, lv, lo=6)
    nl = 16 if lv == 0 else (10 if lv == 1 else 6)
    kit.lathe([(0.030, 0.0), (0.030, 0.010), (0.021, 0.018), (0.019, 0.050), (0.012, 0.066), (0.006, 0.070), (0.0, 0.071)], S.ub((0.0, yc - 0.142, z)), RUBBER,
              verts=nl, name="lampholder")
    kit.lathe([(0.0132, 0.0), (0.0132, 0.025), (0.0, 0.026)], S.ub((0.0, yc - 0.168, z)), BRASS, verts=nl, name="lamp base", close_bottom=False)
    kit.lathe([(0.0, 0.0), (0.019, 0.004), (0.029, 0.016), (0.0335, 0.036), (0.032, 0.056), (0.025, 0.082), (0.016, 0.104), (0.0133, 0.120)],
              S.ub((0.0, yc - 0.288, z)), LIT if lit else GLASS, verts=nl + 4 if lv == 0 else nl, name="lamp glass", close_top=False)
    if not lit and lv == 0:
        tube(kit, "filament", [(-0.006, yc - 0.236, z), (0.0, yc - 0.226, z), (0.006, yc - 0.236, z)], 0.0006, STEEL, lv, lo=4)
    if lv >= 2:
        return
    nb = 6 if lv == 0 else 4
    wr = 0.0016
    for k in range(nb):
        a = 2 * math.pi * k / nb
        c, s = math.cos(a), math.sin(a)
        prof = [(0.028, yc - 0.146), (0.046, yc - 0.190), (0.056, yc - 0.250), (0.050, yc - 0.300), (0.024, yc - 0.338)]
        mbw = S.Mesh()
        S.sweep_section(mbw, [(r * c, y, z + r * s) for (r, y) in prof], S.circle(wr, 3))
        mbw.part(kit, "guard bow", STEEL)
    for (r, y) in (((0.056, yc - 0.250), (0.024, yc - 0.338)) if lv == 0 else ((0.056, yc - 0.250),)):
        n = 10 if lv == 0 else 8
        ring = [(r * math.cos(2 * math.pi * j / n), y, z + r * math.sin(2 * math.pi * j / n)) for j in range(n)]
        mr = S.Mesh()
        S.sweep_section(mr, ring + [ring[0]], S.circle(wr * 1.2, 3))
        mr.part(kit, "guard ring", STEEL)
    rcyl(kit, 0.031, 0.008, (0.0, yc - 0.146, z), "y", STEEL, nl, name="guard clamp")


def temp_light_string(kit, lv, lit=(True, True, True)):
    """Kit_TempLightString: 3 m of yellow 12/3 SJ cord tied up at 1 m centres (nylon ties round a tie wire) and sagging 40 mm between, a
    caged work lamp hanging from each low point at z 0.5 / 1.5 / 2.5 (62 Kit_TempLightString, LS99; FarShore 72 3.2: caged work lights on
    a yellow cord already burning). Origin at the first tie on the support line; the string runs along +Z, the lamps hang to -Y. lit: per
    lamp (Lit, _Dead all out, _OneOut the middle one out)."""
    sag = 0.040
    pts = []
    for span in range(3):
        for k in range(8 if lv == 0 else 4):
            u = k / float(8 if lv == 0 else 4)
            pts.append((0.0, -sag * 4.0 * u * (1.0 - u), span + u))
    pts.append((0.0, 0.0, 3.0))
    ptube(kit, "cord", pts, 0.0066, YELLOW, lv, n0=6)
    for k in range(4):
        z = float(k)
        tube(kit, "tie wire", [(0.0, 0.0, z), (0.0, 0.060, z)], 0.0010, HDG, lv, lo=4) if lv <= 1 else None
        if lv <= 1:
            rcyl(kit, 0.0085, 0.006, (0.0, 0.0, z), "z", PBLACK, 8, name="cable tie")
    for i, z in enumerate((0.5, 1.5, 2.5)):
        _work_lamp(kit, z, -sag, lv, lit=lit[i])


def temp_light_string_meta(kit, lit):
    kit.no_collider()
    D.anchor_u(kit, "start", (0.0, 0.0, 0.0))
    D.anchor_u(kit, "end", (0.0, 0.0, 3.0))
    kit.meta["lampsLit"] = [bool(v) for v in lit]
    kit.meta["lampCentres"] = [[0.0, -0.040 - 0.225, z] for z in (0.5, 1.5, 2.5)]
    kit.meta["wiredInSP7"] = False
    kit.min_ceiling = 0.0


COL_HW, COL_H = 1.42, 2.90         # the corridor's lining: inner faces x +-1.42, soffit 2.90 (FarShore: Inner, Soffit)


def expansion_collar(kit, lv):
    """Kit_ExpansionCollar: FAR SHORE's steel expansion-joint collar round the corridor's section (72 3.2: "a steel expansion-joint collar
    runs round walls, soffit and floor (a steel threshold plate about 200 mm wide across the floor)"; FarShore CollarZ0 / Z1, proud 6 mm):
    a 200 mm band of 6 mm painted steel on the near lining and a 100 mm band on the far, a 25 mm joint between them packed with mineral
    fire-stop wool (Prop_Fireproofing) bulging a little and tufted, button-head bolts at 450 mm, bent corner angles at the soffit, and a
    galvanised 6 mm threshold plate 250 mm wide with bevelled edges and countersunk screws across the floor. Origin on the floor at the
    joint's centre line; the near shore at -Z."""
    t = 0.006
    bands = ((-0.2125, -0.0125, "near band"), (0.0125, 0.1125, "far band"))
    for (z0, z1, nm) in bands:
        zc, L = (z0 + z1) / 2.0, z1 - z0
        bw = 0.0015 if lv == 0 else 0.0
        for s in (-1.0, 1.0):
            box(kit, (t, COL_H - t, L), (s * (COL_HW - t / 2.0), (COL_H - t) / 2.0, zc), HDG, lv, w=bw, name=nm + " wall")
        box(kit, (2 * COL_HW, t, L), (0.0, COL_H - t / 2.0, zc), HDG, lv, w=bw, name=nm + " soffit")
        if lv <= 1:
            for s in (-1.0, 1.0):
                mc = S.Mesh()
                cx = s * (COL_HW - t - 0.020)
                prism(mc, [(-0.020, -0.0015), (0.020, -0.0015), (0.020, 0.0015), (-0.020, 0.0015)], (s * (COL_HW - t - 0.014), COL_H - t - 0.014, 0.0),
                      (s * 0.7071, -0.7071, 0.0) if s > 0 else (0.7071, 0.7071, 0.0), (0.7071, 0.7071, 0.0) if s > 0 else (0.7071, -0.7071, 0.0), (0, 0, 1) if s > 0 else (0, 0, -1),
                      z0 if s > 0 else -z1, z1 if s > 0 else -z0)
                mc.part(kit, "corner angle", HDG)
        if lv == 0:
            zb = zc
            for s in (-1.0, 1.0):
                y = 0.20
                while y < COL_H - 0.10:
                    stud(kit, (s * (COL_HW - t - 0.0015), y, zb), "x", 0.0085, 0.004, STEEL, lv, name="bolt")
                    y += 0.45
            x = -COL_HW + 0.25
            while x < COL_HW - 0.2:
                stud(kit, (x, COL_H - t - 0.0015, zb), "y", 0.0085, 0.004, STEEL, lv, name="bolt")
                x += 0.45
    # the threshold plate
    sec = [(-0.125, 0.0), (0.125, 0.0), (0.119, t), (-0.119, t)] if lv <= 1 else [(-0.125, 0.0), (0.125, 0.0), (0.125, t), (-0.125, t)]
    mt = S.Mesh()
    prism(mt, sec, (0.0, 0.0, 0.0), (0, 0, 1), (0, 1, 0), (-1, 0, 0), -COL_HW, COL_HW)
    mt.part(kit, "threshold plate", HDG)
    if lv == 0:
        x = -COL_HW + 0.12
        while x < COL_HW - 0.05:
            for zz in (-0.085, 0.085):
                rcyl(kit, 0.0075, 0.0008, (x, t + 0.0004, zz), "y", ZINC, 10, name="countersunk screw")
            x += 0.30
    # the fire-stop wool in the joint: up the west wall, across the soffit, down the east wall (an irregular bulging rope)
    rnd = random.Random(1989)
    path = []
    step = 0.10 if lv == 0 else (0.25 if lv == 1 else 0.70)
    y = 0.0
    while y < COL_H - 0.03:
        path.append((-COL_HW + 0.004, y, 0.0))
        y += step
    x = -COL_HW + 0.03
    while x < COL_HW - 0.03:
        path.append((x, COL_H - 0.004, 0.0))
        x += step
    y = COL_H - 0.03
    while y > 0.0:
        path.append((COL_HW - 0.004, y, 0.0))
        y -= step
    path.append((COL_HW - 0.004, 0.0, 0.0))
    nsec = 6 if lv == 0 else 4
    m = S.Mesh()
    secs = []
    for i, p in enumerate(path):
        j = 1.0 + (0.35 * (rnd.random() - 0.5) if lv == 0 else 0.0)
        secs.append(j)
    base = [(0.010 * math.cos(2 * math.pi * k / nsec), 0.0115 * math.sin(2 * math.pi * k / nsec)) for k in range(nsec)]
    fr = S.frames_along(path)
    for i in range(len(path) - 1):
        for k in range(nsec):
            k1 = (k + 1) % nsec
            def P(ii, kk):
                tt, nn, bb = fr[ii]
                a, b = base[kk]
                return tuple(V(path[ii]) + nn * (a * secs[ii]) + bb * (b * secs[ii]))
            a0, b0, c0, d0 = P(i, k), P(i, k1), P(i + 1, k1), P(i + 1, k)
            mid = (V(a0) + V(b0) + V(c0) + V(d0)) / 4.0
            ctr = (V(path[i]) + V(path[i + 1])) / 2.0
            D.qd(m, a0, b0, c0, d0, (i * step, k * 0.01), (i * step, (k + 1) * 0.01), ((i + 1) * step, (k + 1) * 0.01), ((i + 1) * step, k * 0.01), tuple(mid - ctr))
    m.part(kit, "fire-stop wool", WOOL)
    if lv == 0:
        for (px, py) in ((-COL_HW + 0.012, 1.10), (-0.40, COL_H - 0.012), (0.85, COL_H - 0.012), (COL_HW - 0.012, 2.10)):
            for q in range(3):
                d = (0.0, -1.0, 0.0) if py > 2.5 else ((1.0, 0.0, 0.0) if px < 0 else (-1.0, 0.0, 0.0))
                p0 = (px, py - 0.02 * q, 0.004 * (q - 1))
                p1 = (px + d[0] * 0.025 + 0.01 * (rnd.random() - 0.5), py + d[1] * 0.025 - 0.02 * q - 0.03, 0.006 * (q - 1))
                p2 = (px + d[0] * 0.035, py + d[1] * 0.045 - 0.02 * q - 0.07, 0.008 * (q - 1))
                ms = S.Mesh()
                S.sweep_section(ms, [p0, p1, p2], [(-0.003, -0.001), (0.003, -0.001), (0.003, 0.001), (-0.003, 0.001)], cap1=True)
                ms.part(kit, "wool tuft", WOOL)


def expansion_collar_meta(kit):
    kit.no_collider()
    kit.meta["section"] = [2 * COL_HW, COL_H]
    kit.meta["farShore"] = {"CollarZ0": 17.60, "JointZ": 17.80, "CollarZ1": 17.92, "proud": 0.006}
    kit.meta["wiredInSP7"] = False
    kit.min_ceiling = COL_H


PLATE_HW, PLATE_D, PLATE_T = 1.40, 0.40, 0.020     # DreamRun: PlateHalf, NearPlate0..NearHinge, CombTop
TEETH_N, TEETH_P = 112, 0.025


def comb_plate(kit, lv):
    """Kit_CombPlate: DREAM RUN's landing plate at the moving floor (RunEndDreamRun's numbers): a galvanised plate 2.80 x 0.40 standing
    20 mm proud with a 30 mm nosing from 3 mm, a 60 mm safety-yellow band before the comb, the comb in eight cast segments 80 mm deep
    (yellow, three countersunk screws each) hinged on the plate's edge, and its 112 teeth 100 mm long on a 25 mm pitch, 13 mm at the root
    and 7 mm at the tip, their tips 2 mm down. Origin on the floor at the hinge line; the plate at -Z, the teeth at +Z (level: the build's
    rest state; DreamRun rotates its own comb)."""
    galv = [(-PLATE_D, 0.0), (-0.060, 0.0), (-0.060, PLATE_T), (-PLATE_D + 0.030, PLATE_T), (-PLATE_D, 0.003)]
    m = S.Mesh()
    prism(m, galv, (0.0, 0.0, 0.0), (0, 0, 1), (0, 1, 0), (-1, 0, 0), -PLATE_HW, PLATE_HW)
    m.part(kit, "landing plate", HDG)
    m2 = S.Mesh()
    prism(m2, [(-0.060, 0.0), (0.0, 0.0), (0.0, PLATE_T), (-0.060, PLATE_T)], (0.0, 0.0, 0.0), (0, 0, 1), (0, 1, 0), (-1, 0, 0), -PLATE_HW, PLATE_HW)
    m2.part(kit, "yellow band", YELLOW)
    if lv == 0:
        x = -PLATE_HW + 0.15
        while x < PLATE_HW - 0.1:
            rcyl(kit, 0.0075, 0.0008, (x, PLATE_T + 0.0004, -0.25), "y", ZINC, 10, name="plate screw")
            x += 0.30
    nseg = 8
    sw = 2 * PLATE_HW / nseg
    for k in range(nseg):
        x0 = -PLATE_HW + sw * k + 0.001
        box(kit, (sw - 0.002, PLATE_T, 0.080), (x0 + (sw - 0.002) / 2.0, PLATE_T / 2.0, 0.040), YELLOW, lv, w=0.0012 if lv == 0 else 0.0, name="comb segment")
        if lv == 0:
            for dx in (0.06, sw / 2.0, sw - 0.06):
                rcyl(kit, 0.0060, 0.0007, (x0 + dx, PLATE_T + 0.00035, 0.035), "y", ZINC, 10, name="segment screw")
    if lv >= 2:
        box(kit, (2 * PLATE_HW, 0.005, 0.100), (0.0, 0.0165, 0.130), YELLOW, lv, w=0.0, name="teeth")
        return
    mt = S.Mesh()
    for i in range(TEETH_N):
        x = -PLATE_HW + TEETH_P * (i + 0.5)
        r, tp = 0.0065, 0.0035
        z0, z1 = 0.080, 0.180
        yb, yr, yt = 0.014, PLATE_T, 0.018
        A0, B0 = (x - r, yb, z0), (x + r, yb, z0)
        C0, D0 = (x + r, yr, z0), (x - r, yr, z0)
        A1, B1 = (x - tp, yb, z1), (x + tp, yb, z1)
        C1, D1 = (x + tp, yt, z1), (x - tp, yt, z1)
        D.qd(mt, D0, C0, C1, D1, (x - r, z0), (x + r, z0), (x + tp, z1), (x - tp, z1), (0.0, 1.0, 0.0))
        D.qd(mt, A1, B1, C1, D1, (x - tp, 0.0), (x + tp, 0.0), (x + tp, 0.004), (x - tp, 0.004), (0.0, 0.0, 1.0))
        if lv == 0:
            D.qd(mt, B0, B1, C1, C0, (z0, yb), (z1, yb), (z1, yt), (z0, yr), (1.0, 0.0, 0.0))
            D.qd(mt, A0, D0, D1, A1, (z0, yb), (z0, yr), (z1, yt), (z1, yb), (-1.0, 0.0, 0.0))
            D.qd(mt, A0, A1, B1, B0, (x - r, z0), (x - tp, z1), (x + tp, z1), (x + r, z0), (0.0, -1.0, 0.0))
    mt.part(kit, "comb teeth", YELLOW)


def comb_plate_meta(kit):
    kit.no_collider()
    kit.meta["dreamRun"] = {"PlateHalf": PLATE_HW, "CombTop": PLATE_T, "teeth": TEETH_N, "pitch": TEETH_P}
    kit.meta["wiredInSP7"] = False
    kit.min_ceiling = 0.0


GYP = "Door_CoreGypsum"            # main: the gypsum core (a board's cut edge)
kitlib.register_slot(GYP, (0.80, 0.79, 0.76), 0.9, 0.0)


def _frame_yaw(yaw_deg, roll=False):
    """(ex, ey, ez) for a profile lying on the floor along ez = yaw (Unity: +Z turned yaw about +Y); ey up (or, rolled, on its side)."""
    a = math.radians(yaw_deg)
    ez = (math.sin(a), 0.0, math.cos(a))
    ey = (0.0, 1.0, 0.0)
    ex = (ez[2], 0.0, -ez[0])
    if roll:
        ex, ey = (0.0, -1.0, 0.0), ex
    return ex, ey, ez


def _c_section(web, flange, lip, t):
    """A C (lip > 0) or U (lip 0) light-gauge section, web on the floor, flanges up; an open outline of thickness t (CCW)."""
    h = web / 2.0
    out = [(-h, 0.0), (h, 0.0), (h, flange)]
    if lip > 0:
        out += [(h - lip, flange), (h - lip, flange - t), (h - t, flange - t)]
    else:
        out += [(h - t, flange)]
    out += [(h - t, t), (-h + t, t)]
    if lip > 0:
        out += [(-h + t, flange - t), (-h + lip, flange - t), (-h + lip, flange)]
    else:
        out += [(-h + t, flange)]
    out += [(-h, flange)]
    return out


def _stud_piece(kit, centre, yaw, length, lv, track=False, roll=False, holes=True):
    t = 0.0006
    sec = _c_section(0.0921, 0.0318 if track else 0.0318, 0.0 if track else 0.0064, t) if lv <= 1 else [(-0.046, 0.0), (0.046, 0.0), (0.046, 0.032), (-0.046, 0.032)]
    ex, ey, ez = _frame_yaw(yaw, roll)
    o = (centre[0] - ez[0] * length / 2.0, centre[1], centre[2] - ez[2] * length / 2.0)
    m = S.Mesh()
    prism(m, sec, o, ex, ey, ez, 0.0, length, caps=(False, False), closed=True)
    m.part(kit, "track offcut" if track else "stud offcut", HDG)
    if holes and not track and lv == 0 and not roll:
        for u in (0.25, 0.70):
            if u * length < 0.06 or u * length > length - 0.06:
                continue
            c = V(o) + V(ez) * (u * length) + V(ey) * (t + 0.0003)
            n = 10
            pts = []
            for k in range(n):
                a = 2 * math.pi * k / n
                pts.append(tuple(c + V(ex) * (0.019 * math.cos(a)) + V(ez) * (0.050 * math.sin(a))))
            mh = S.Mesh()
            idx = [mh.vert(p) for p in pts]
            mh.face(tuple(idx), [(0.019 * math.cos(2 * math.pi * k / n), 0.050 * math.sin(2 * math.pi * k / n)) for k in range(n)])
            mh.part(kit, "punch-out", PBLACK)


def _board_piece(kit, outline, y0, lv, flip=False):
    """A broken gypsum-board offcut: an outline (x, z) on the floor (convex, CCW seen from above), 12.7 mm of core with the paper face."""
    th = 0.0127
    m = S.Mesh()
    sec = [(p[0], -p[1]) for p in outline]
    prism(m, sec, (0.0, y0, 0.0), (1, 0, 0), (0, 0, -1), (0, 1, 0), 0.0, th)
    m.part(kit, "board core", GYP)
    mf = S.Mesh()
    idx = [mf.vert((p[0], y0 + th + 0.0003, p[1])) for p in outline]
    mf.face(tuple(reversed(idx)), [(p[0], p[1]) for p in reversed(outline)])
    mf.part(kit, "board face", PRIMER)


def _screw_box(kit, c, yaw, lv):
    """A carton of 1-1/4 in drywall screws, its lid flap open, a few screws spilled."""
    a = math.radians(yaw)
    box(kit, (0.150, 0.070, 0.090), (c[0], 0.035, c[2]), CARD, lv, w=0.002 if lv == 0 else 0.0, rot=(0, yaw, 0), name="screw carton")
    if lv <= 1:
        box(kit, (0.150, 0.0015, 0.060), (c[0] - 0.075 * math.sin(a) * 0.0, 0.074, c[2] + 0.0), CARD, lv, w=0.0, rot=(-25, yaw, 0), name="carton flap")
    if lv == 0:
        rnd = random.Random(int(c[0] * 1000) + 7)
        for k in range(5):
            px = c[0] + 0.10 + 0.08 * rnd.random()
            pz = c[2] - 0.05 + 0.10 * rnd.random()
            ang = rnd.random() * 2 * math.pi
            tube(kit, "screw", [(px, 0.003, pz), (px + 0.032 * math.cos(ang), 0.003, pz + 0.032 * math.sin(ang))], 0.0018, STEEL, lv, lo=4)


def _corner_bead(kit, centre, yaw, length, lv):
    t = 0.0005
    leg = 0.032
    sec = [(-leg, 0.0), (0.0, 0.0), (0.0, leg), (-t, leg), (-t, t), (-leg, t)] if lv <= 1 else [(-leg, 0.0), (0.0, 0.0), (0.0, leg), (-leg, t)]
    ex, ey, ez = _frame_yaw(yaw)
    o = (centre[0] - ez[0] * length / 2.0, centre[1], centre[2] - ez[2] * length / 2.0)
    m = S.Mesh()
    prism(m, sec, o, ex, ey, ez, 0.0, length, caps=(False, False))
    m.part(kit, "corner bead", HDG)
    if lv == 0:
        tube(kit, "bead nose", [tuple(V(o) + V(ey) * 0.0016), tuple(V(o) + V(ey) * 0.0016 + V(ez) * length)], 0.0016, HDG, lv, lo=6)


def ti_scrap(kit, lv, variant="A"):
    """Kit_TIScrap: the tenant fit-out's floor scatter (62 Kit_TIScrap, LS99): 3-5/8 in 25 ga steel stud and track offcuts with their
    punch-outs, broken 1/2 in gypsum board pieces (paper face, grey core), a corner bead, an open carton of drywall screws with a few
    spilled. Variant A / B: two arrangements over about 1.5 x 1.0 m. Origin on the floor at the scatter's centre."""
    if variant == "A":
        _stud_piece(kit, (-0.42, 0.0, 0.10), 74.0, 0.52, lv)
        _stud_piece(kit, (0.18, 0.0, -0.28), 12.0, 0.36, lv)
        _stud_piece(kit, (0.30, 0.046, 0.05), 103.0, 0.24, lv, roll=True)
        _stud_piece(kit, (-0.05, 0.0, 0.34), 168.0, 0.61, lv, track=True)
        _board_piece(kit, [(0.30, 0.18), (0.66, 0.24), (0.62, 0.46), (0.40, 0.52), (0.26, 0.40)], 0.0, lv)
        _board_piece(kit, [(-0.70, -0.40), (-0.36, -0.44), (-0.32, -0.20), (-0.62, -0.12)], 0.0, lv)
        _corner_bead(kit, (-0.20, 0.0, -0.06), 52.0, 0.86, lv)
        _screw_box(kit, (0.42, 0.0, -0.08), 18.0, lv)
    else:
        _stud_piece(kit, (0.35, 0.0, 0.20), -38.0, 0.44, lv)
        _stud_piece(kit, (-0.30, 0.0, -0.30), 96.0, 0.30, lv)
        _stud_piece(kit, (-0.45, 0.0, 0.22), 20.0, 0.58, lv, track=True)
        _board_piece(kit, [(-0.10, -0.20), (0.32, -0.32), (0.40, -0.06), (0.06, 0.04)], 0.0, lv)
        _board_piece(kit, [(-0.66, -0.10), (-0.48, -0.16), (-0.40, 0.02), (-0.58, 0.08)], 0.0, lv)
        _board_piece(kit, [(0.02, 0.10), (0.24, 0.12), (0.20, 0.30), (0.00, 0.26)], 0.0127, lv)
        _corner_bead(kit, (0.10, 0.0, 0.40), -8.0, 0.70, lv)
        _screw_box(kit, (-0.12, 0.0, -0.44), -64.0, lv)


def ti_scrap_meta(kit):
    kit.no_collider()
    kit.meta["wiredInSP7"] = False
    kit.min_ceiling = 0.0
