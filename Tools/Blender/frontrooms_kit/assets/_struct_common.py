"""Shared helpers for the structures kits of scene 4 (line visual (game visual), structures_assets step 2, group G1-b, 2026-11-06):
P4 structures_rail_kits, P5 structures_stair_kits, P6 structures_laylight, P9 structures_shared_kits.

Plan: Documentation/research/structures_assets/30_plan.md section 2 (packages) and 20_research.md F6, F8, F12 (Part B).
Every module builds its three levels BY HAND through _run_b_common.levels (kitlib P-1b): the same parts at three
qualities, so a LOD keeps its silhouette and slots. Hero rule as Run! B (VL338): curves at a chord error <= 0.2 mm at
LOD0, every exposed edge bevelled >= 0.8 mm at LOD0 (kitlib bevels 3-6 mm on the large parts), checked at 0.35 m.

Frames. kitlib works in Blender space (metres, Z up, the FRONT toward -Y); Unity reads (x, y, z) as (-x, z, -y). These
kits are specified in UNITY space (the builder's frame, as the journal names them), so every helper here takes Unity
coordinates and converts with ub() / ubd(). Slots are registered here (kitlib.SLOTS is not edited); their Unity materials
are Resources/Surfaces/<slot>.mat (FrontRoomsKitImporter remaps by name). No text, label, brand or logo. ASCII only.
"""

import math

import bmesh
import bpy
from mathutils import Matrix, Vector

import kitlib
import _run_b_common as B

S_SLOTS = {
    "Prop_StainlessSatin": ((0.62, 0.62, 0.61), 0.45, 0.45),
    "Trans_AluminiumSatin": ((0.60, 0.60, 0.58), 0.35, 1.0),
    "Prop_SteelBeige": ((0.62, 0.58, 0.48), 0.55, 0.0),
    "Prop_LaylightDiffuser": ((0.86, 0.86, 0.84), 0.3, 0.0),
    "Prop_AccessFloor": ((0.62, 0.62, 0.60), 0.6, 0.0),
    "Prop_FRPGrating": ((0.40, 0.40, 0.38), 0.7, 0.0),
    "Prop_FlexConduit": ((0.55, 0.56, 0.56), 0.45, 0.6),
    "L0_Carpet": ((0.42, 0.36, 0.20), 0.9, 0.0),
    "Plenum_Dark": ((0.05, 0.05, 0.05), 0.9, 0.0),
    "Prop_RailEnamel": ((0.66, 0.64, 0.58), 0.38, 0.0),     # P3: Painted_Metal's maps at the rail's scale (G1 fix)
}
for _slot, (_c, _r, _m) in S_SLOTS.items():
    kitlib.register_slot(_slot, _c, _r, _m)

# The rail tube of P1's handrail profile: 1-1/4 in NPS, 1.660 in OD.
TUBE_R = 0.0211
WALL_CLEAR = 0.0381         # 1-1/2 in clear between the wall and the rail (ADA 1991 4.26.2)
WALL_Z = -(WALL_CLEAR + TUBE_R)   # the wall plane behind the rail axis, Unity z


def ub(p):
    """Unity point (x, y, z) -> Blender (x, y, z)."""
    return (-p[0], -p[2], p[1])


def ubd(d):
    """Unity direction -> Blender direction (same map as points)."""
    return Vector((-d[0], -d[2], d[1]))


def segs(r, lv, lo=8):
    return B.segs(r, lv, lo)


def bev(w, lv):
    return B.bev(w, lv)


# ----------------------------------------------------------------------------- mesh building in Unity space
class Mesh:
    """A part assembled from Unity-space vertices, faces and per-corner UVs (metres), then adopted by the kit with its
    UVs kept (uv="keep"). Faces are wound so their normal points OUT in Unity space; the Blender copy is mirrored by
    ub() (a handedness change), so faces are reversed on the way in."""

    def __init__(self):
        self.v = []
        self.f = []
        self.uv = []

    def vert(self, p):
        self.v.append(ub(p))
        return len(self.v) - 1

    def face(self, idx, uvs):
        # ub() maps (x, y, z) -> (-x, -z, y): a reflection (det -1), so reverse the winding to keep normals out.
        self.f.append(tuple(reversed(idx)))
        self.uv.extend(reversed(list(uvs)))

    def quad(self, a, b, c, d, uva, uvb, uvc, uvd):
        ia, ib, ic, idd = self.vert(a), self.vert(b), self.vert(c), self.vert(d)
        self.face((ia, ib, ic, idd), (uva, uvb, uvc, uvd))

    def tri(self, a, b, c, uva, uvb, uvc):
        ia, ib, ic = self.vert(a), self.vert(b), self.vert(c)
        self.face((ia, ib, ic), (uva, uvb, uvc))

    def part(self, kit, name, slot, merge=True):
        obj = B.mesh_part(kit, name, self.v, self.f, self.uv, slot)
        if merge:
            bm = bmesh.new()
            bm.from_mesh(obj.data)
            bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
            bm.normal_update()
            bm.to_mesh(obj.data)
            bm.free()
        return obj


def frames_along(path):
    """Rotation-minimising frames (tangent, normal, binormal) along a Unity-space polyline (double reflection)."""
    pts = [Vector(p) for p in path]
    n = len(pts)
    tans = []
    for i in range(n):
        if i == 0:
            t = pts[1] - pts[0]
        elif i == n - 1:
            t = pts[-1] - pts[-2]
        else:
            t = (pts[i + 1] - pts[i]).normalized() + (pts[i] - pts[i - 1]).normalized()
        tans.append(t.normalized())
    ref = Vector((0, 1, 0)) if abs(tans[0].y) < 0.9 else Vector((1, 0, 0))
    nrm = (ref - tans[0] * ref.dot(tans[0])).normalized()
    out = [(tans[0], nrm, tans[0].cross(nrm))]
    for i in range(n - 1):
        v1 = pts[i + 1] - pts[i]
        c1 = v1.dot(v1)
        if c1 < 1e-14:
            out.append(out[-1])
            continue
        rL = out[-1][1] - (2.0 / c1) * v1.dot(out[-1][1]) * v1
        tL = out[-1][0] - (2.0 / c1) * v1.dot(out[-1][0]) * v1
        v2 = tans[i + 1] - tL
        c2 = v2.dot(v2)
        r = rL - (2.0 / c2) * v2.dot(rL) * v2 if c2 > 1e-14 else rL
        r = (r - tans[i + 1] * r.dot(tans[i + 1])).normalized()
        out.append((tans[i + 1], r, tans[i + 1].cross(r)))
    return out


def sweep_section(m, path, section, closed_section=True, frames=None, cap0=False, cap1=False, uv_scale=1.0, flip=False):
    """Sweep a 2D section (list of (a, b) in the frame's normal / binormal) along a Unity polyline into Mesh m.
    UVs: u = arc length along the path (m), v = arc length round the section (m): FrontRooms/Surface mesh-UV metres."""
    fr = frames or frames_along(path)
    pts = [Vector(p) for p in path]
    ns = len(section)
    rings = []
    for i, p in enumerate(pts):
        t, nn, bb = fr[i]
        rings.append([p + nn * a + bb * b for (a, b) in section])
    along = [0.0]
    for i in range(1, len(pts)):
        along.append(along[-1] + (pts[i] - pts[i - 1]).length)
    around = [0.0]
    for j in range(1, ns + (1 if closed_section else 0)):
        a0, a1 = section[(j - 1) % ns], section[j % ns]
        around.append(around[-1] + math.hypot(a1[0] - a0[0], a1[1] - a0[1]))
    jm = ns if closed_section else ns - 1
    for i in range(len(pts) - 1):
        for j in range(jm):
            j1 = (j + 1) % ns
            a, b = rings[i][j], rings[i][j1]
            c, d = rings[i + 1][j1], rings[i + 1][j]
            ua, ub_ = along[i] * uv_scale, along[i + 1] * uv_scale
            va, vb = around[j] * uv_scale, around[j + 1] * uv_scale
            if flip:
                m.quad(a, d, c, b, (ua, va), (ub_, va), (ub_, vb), (ua, vb))
            else:
                m.quad(a, b, c, d, (ua, va), (ua, vb), (ub_, vb), (ub_, va))
    if closed_section:
        for (cap, ring, sgn) in ((cap0, rings[0], -1), (cap1, rings[-1], 1)):
            if not cap:
                continue
            idx = [m.vert(tuple(q)) for q in ring]
            uvs = [(s[0] * uv_scale, s[1] * uv_scale) for s in section]
            if (sgn < 0) != flip:
                m.face(tuple(reversed(idx)), list(reversed(uvs)))
            else:
                m.face(tuple(idx), uvs)
    return m


def circle(r, n, phase=0.0):
    """A closed circular section, counter-clockwise in (a, b)."""
    return [(r * math.cos(2 * math.pi * k / n + phase), r * math.sin(2 * math.pi * k / n + phase)) for k in range(n)]


def tube(kit, name, path, r, slot, lv, cap0=True, cap1=True, lo=8, uv_scale=1.0):
    m = Mesh()
    sweep_section(m, path, circle(r, segs(r, lv, lo)), cap0=cap0, cap1=cap1, uv_scale=uv_scale)
    return m.part(kit, name, slot)


def arc_points(centre, r, a0, a1, axis_u, axis_v, n):
    """Points on an arc in the plane spanned by unit Unity vectors axis_u, axis_v round centre, from angle a0 to a1 (rad)."""
    c, u, v = Vector(centre), Vector(axis_u), Vector(axis_v)
    return [tuple(c + u * (r * math.cos(a0 + (a1 - a0) * k / n)) + v * (r * math.sin(a0 + (a1 - a0) * k / n))) for k in range(n + 1)]


def arc_steps(r, angle, lv, lo=4):
    """Segments on an arc of radius r over angle (rad) at the chord rule of this level."""
    full = segs(max(r, 1e-4), lv, 8)
    return max(lo if lv == 0 else 2, int(math.ceil(full * angle / (2 * math.pi))))


# ----------------------------------------------------------------------------- kitlib primitives in Unity space
def box(kit, size, centre, slot, lv, w=0.0012, name="box", rot=(0, 0, 0)):
    """An axis box of Unity size (x, y, z) at a Unity centre; rot = Unity Euler degrees (applied as Blender's)."""
    bw, bs = bev(w, lv)
    s = (size[0], size[2], size[1])
    return kit.box(s, ub(centre), slot, bevel=bw, segments=bs, rot=(rot[0], rot[2], -rot[1]), name=name)


def cyl_y(kit, r, h, centre, slot, lv, w=0.0008, name="cyl", lo=8, r_top=None):
    """A vertical (Unity Y) cylinder of radius r and height h centred at a Unity point."""
    bw, bs = bev(w, lv)
    return kit.cylinder(r, h, ub(centre), slot, verts=segs(max(r, r_top or 0), lv, lo), bevel=bw, segments=bs, name=name, radius_top=r_top)


def cyl_x(kit, r, h, centre, slot, lv, w=0.0008, name="cyl", lo=8):
    bw, bs = bev(w, lv)
    return kit.cylinder(r, h, ub(centre), slot, verts=segs(r, lv, lo), bevel=bw, segments=bs, rot=(0, 90, 0), name=name)


def cyl_z(kit, r, h, centre, slot, lv, w=0.0008, name="cyl", lo=8):
    bw, bs = bev(w, lv)
    return kit.cylinder(r, h, ub(centre), slot, verts=segs(r, lv, lo), bevel=bw, segments=bs, rot=(90, 0, 0), name=name)


def lathe_y(kit, profile, base, slot, lv, name="lathe", lo=8, close_top=True, close_bottom=True, rings=None):
    """Revolve a (radius, height) profile (bottom to top) about the Unity Y axis through a Unity base point."""
    return B.lathe(kit, profile, ub(base), slot, lv=lv, name=name, lo=lo, close_top=close_top, close_bottom=close_bottom, rings=rings)


def lathe_axis(kit, profile, base, axis, slot, lv, name="lathe", lo=8, close_top=True, close_bottom=True):
    """Revolve a (radius, height) profile about a Unity axis 'x', '-x', 'z', '-z', 'y' or '-y' through a Unity base point."""
    rot = {"y": (0, 0, 0), "-y": (180, 0, 0), "x": (0, -90, 0), "-x": (0, 90, 0), "z": (90, 0, 0), "-z": (-90, 0, 0)}[axis]
    r = max(p[0] for p in profile)
    return kit.lathe(profile, ub(base), slot, verts=segs(r, lv, lo), rot=rot, name=name, close_top=close_top, close_bottom=close_bottom)


def hex_nut(kit, af, h, centre, axis, slot, lv, name="nut"):
    """A hex nut / bolt head (across flats af, height h) on a Unity axis ('y' or 'z' or 'x'), chamfered at LOD0."""
    r = af / math.sqrt(3.0)
    rot = {"y": (0, 0, 0), "z": (90, 0, 0), "x": (0, 90, 0)}[axis]
    bw, bs = (0.0006, 1) if lv == 0 else (0.0, 0)
    return kit.cylinder(r, h, ub(centre), slot, verts=6, rot=rot, bevel=bw, segments=bs, name=name)


def screw_head(kit, r, centre, axis, slot, lv, name="screw", dome=0.35):
    """A pan / oval screw head of radius r facing along a Unity axis ('+z', '-z', '+y', '-y', '+x', '-x')."""
    h = r * dome
    prof = [(r, 0.0), (r * 0.98, h * 0.45), (r * 0.78, h * 0.85), (r * 0.40, h), (0.0, h * 1.02)]
    if lv >= 2:
        prof = [(r, 0.0), (r * 0.6, h), (0.0, h)]
    # Blender Z (the lathe axis) to the Unity axis: Unity +Z = Blender -Y (R_x +90), Unity +X = Blender -X (R_y -90).
    rot = {"+y": (0, 0, 0), "-y": (180, 0, 0), "+z": (90, 0, 0), "-z": (-90, 0, 0), "+x": (0, -90, 0), "-x": (0, 90, 0)}[axis]
    return kit.lathe(prof, ub(centre), slot, verts=segs(r, lv, 8), rot=rot, name=name, close_bottom=True)


def finish(kit, distances, budget, tags=(), placement_tag=None):
    B.lod_meta(kit, distances, budget)
    kit.meta["structures"] = "structures_assets G1-b 2026-11-06 (30_plan.md section 2)"
    if tags:
        kit.tag(*tags)
    if placement_tag:
        kit.tag(placement_tag)
