"""Kit_DoorFrame_FRP_DA: the pool map member's frame, rows PL-F / PL-K (zone doors, the Map stage, 2026-11-06;
research/double_doors/20_design.md section B10, 30_build.md Part M): the single-leaf version of the pool pair's moulded FRP frame
(Kit_DoorFrame_FRPPair_DA, PD-A) on the D1.6 map door, in the pool's course aqua one step deeper (Door_PoolGelcoat_Frame).

Section: Kit_DoorFrame_Steel_DA's F4 envelope (the cased-open DA sleeve, 2 in x 7/16 in: the 50.8 mm face band 11.1 mm proud of
the wall face, the soffit, the returns 0.5 mm into the wall, enclosing the map's door-only trims), in a 4 mm moulded wall (solid
FRP frames "moulded with no jointed corners", research Part P section P3.3): the visible arrises r 4 mm, the cavity side sharp. In
elevation the face band's outer head corners are rounded r 12.7 mm (one moulding, no mitre and no joint line); its inner corners
stay square (the leaf's). No stop, silencer or fire label (a non-rated DA frame, door_hardware 10 section 10.5). The latch jamb
keeps the steel kit's mortise strike prep (0.032 x 0.200 at Y 0.968) so both DA strikes seat as on the Steel_DA frame.
The saddle is the steel kit's fluted saddle as a stainless tread in a moulded threshold (the pool pair's Fix B rule): the fluted
top and its three countersunk screws in 630 (Prop_Stainless630), the 1:2 bevels, eased arrises and lips (every face more than
46 mm off the wall's centre plane) in the frame's gel coat. Black brush seals on the centre plane; the pivot caps in 630.

Origin: DOOR ROOT D = the pivot axis (as Kit_DoorFrame_Steel_DA): spawn under the chunk root at the hinge position, rotation
door.closed, scale 1, label "Door frame (kit)". +X = face S.

LODs (hand-built, fr_lods): LOD0 4-segment arrises, 8-segment head corners, the strike prep and box, the fluted saddle and screws,
96-segment caps; LOD1 2-segment arrises, 4-segment corners, the plain saddle, 32-segment caps; LOD2 sharp, 2-segment corners, the
seals and the plain saddle. Switch 4 / 12 m, never culled. Slots: Door_PoolGelcoat_Frame (submesh 0: the frame, the strike box and
the saddle's bevels), Prop_PlasticBlack (seals), Prop_Stainless630 (the tread, screws, caps).
"""

import math
import random
import sys

import bmesh
from mathutils import Vector

import interact_door_common as dc
import interact_door_da_common as da
import interact_door_frame_steel_da as steel
import kitlib

NAME = "Kit_DoorFrame_FRP_DA"
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0
GELCOAT = "Door_PoolGelcoat_Frame"
SEAL = "Prop_PlasticBlack"
STAINLESS = "Prop_Stainless630"
kitlib.register_slot(GELCOAT, (0.054, 0.342, 0.366), 0.22, 0.0)   # preview only: the course aqua one step deeper
kitlib.register_slot(STAINLESS, (0.64, 0.63, 0.60), 0.35, 0.9)

WALL = 0.004                              # the moulded wall
R_CONVEX = 0.004                          # the moulded arrises
R_HEAD = 0.0127                           # the face band's outer head corners, in elevation (1/2 in)
U_OUT = da.STEEL_FACE                     # the band's outer edge, from the soffit line
U_ROUND = U_OUT - R_HEAD                  # section points beyond this follow the rounded head corner
SEGS = {0: 4, 1: 2, 2: 0}
CORNER = {0: 8, 1: 4, 2: 2}
BEVEL_X = 0.046                           # saddle faces whose centre lies further than this off the wall's centre plane: gel coat


def frp_section(level):
    """The closed (d, X) section: Kit_DoorFrame_Steel_DA's outer path (P return -> P face band -> soffit -> S face band -> S
    return) with r 4 mm arrises, the cavity side the sharp outer path offset 4 mm in; d = LINING_Z_H - Z on the hinge jamb."""
    zr = da.LINING_Z_H
    zo, x1 = zr - da.STEEL_FACE, da.STEEL_X1
    outer_xz = [(-da.WALL_X, zo), (-x1, zo), (-x1, zr), (x1, zr), (x1, zo), (da.WALL_X, zo)]
    r = [0.0, R_CONVEX, R_CONVEX, R_CONVEX, R_CONVEX, 0.0] if level < 2 else [0.0] * 6
    outer = dc.fillet_polygon(outer_xz, r, max(SEGS[level], 1), closed=False)
    inner = dc.offset_polyline(outer_xz, WALL, normals_left=False, closed=False)
    poly = outer + list(reversed(inner))
    return [(zr - z, x) for x, z in poly]


def _signed_area(poly):
    a = 0.0
    for i in range(len(poly)):
        x0, y0 = poly[i]
        x1, y1 = poly[(i + 1) % len(poly)]
        a += x0 * y1 - x1 * y0
    return a / 2.0


def _edge_out(poly, i):
    x0, y0 = poly[i]
    x1, y1 = poly[(i + 1) % len(poly)]
    dx, dy = x1 - x0, y1 - y0
    ln = math.hypot(dx, dy) or 1.0
    n = (dy / ln, -dx / ln)
    return n if _signed_area(poly) > 0 else (-n[0], -n[1])


def _face(bm, verts, want_unity):
    f = bm.faces.new(verts)
    f.normal_update()
    w = Vector(dc.U(*want_unity))
    if f.normal.dot(w) < 0:
        f.normal_flip()
    return f


def _corner(d, x, theta, latch):
    """A section point (d, x) on a head-corner ring at angle theta (0: the jamb's run, 90 deg: the head's), Unity (X, Y, Z)."""
    z_h, z_l, y_t = da.LINING_Z_H, da.LINING_Z_L, da.LINING_Y
    if d > U_ROUND + 1e-9:
        r = d - U_ROUND
        along, up = U_ROUND + r * math.cos(theta), U_ROUND + r * math.sin(theta)
    else:
        along, up = d, d                  # the square inner corner: the mitre point, the same for every theta
    return (x, y_t + up, (z_l + along) if latch else (z_h - along))


def sweep_round(kit, section, slot, name, corner_segs):
    """The closed (d, X) section swept round the opening: the hinge jamb from the floor, the head corners turned through
    corner_segs rings (points of the face band's outer 12.7 mm follow an r 12.7 mm round, the rest keep the square mitre: shared
    vertices, so their quads become triangles there or vanish), the latch jamb to the floor. No caps (the feet stand on the floor)."""
    z_h, z_l = da.LINING_Z_H, da.LINING_Z_L
    rings = [([(x, 0.0, z_h - d) for d, x in section], (-1.0, 0.0))]          # (points, the ring's outward (Z, Y) direction)
    for k in range(corner_segs + 1):
        th = 0.5 * math.pi * k / corner_segs
        rings.append(([_corner(d, x, th, False) for d, x in section], (-math.cos(th), math.sin(th))))
    for k in range(corner_segs + 1):
        th = 0.5 * math.pi * (1.0 - k / corner_segs)
        rings.append(([_corner(d, x, th, True) for d, x in section], (math.cos(th), math.sin(th))))
    rings.append(([(x, 0.0, z_l + d) for d, x in section], (1.0, 0.0)))
    bm = bmesh.new()
    verts = {}

    def vert(p):
        key = (round(p[0], 9), round(p[1], 9), round(p[2], 9))
        v = verts.get(key)
        if v is None:
            v = verts[key] = bm.verts.new(dc.U(*p))
        return v

    vr = [[vert(p) for p in ring] for ring, _ in rings]
    n = len(section)
    made = set()
    for s in range(len(rings) - 1):
        a, b = vr[s], vr[s + 1]
        du, dv = rings[s][1], rings[s + 1][1]
        mz, my = du[0] + dv[0], du[1] + dv[1]
        ml = math.hypot(mz, my) or 1.0
        mz, my = mz / ml, my / ml
        for i in range(n):
            j = (i + 1) % n
            uniq = []
            for v in (a[i], a[j], b[j], b[i]):
                if v not in uniq:
                    uniq.append(v)
            if len(uniq) < 3:
                continue
            key = frozenset(uniq)
            if key in made:
                continue
            made.add(key)
            nd, nx = _edge_out(section, i)
            _face(bm, uniq, (nx, nd * my, nd * mz))
    return dc._part(kit, bm, slot, name)


def saddle(kit, flutes, name, levels):
    """The steel kit's saddle lofted across the opening: the fluted (or plain) top in 630, the bevels in the frame's gel coat."""
    ssec = dc.saddle_section(flutes=flutes)
    obj = dc.loft_z(kit, [(da.LINING_Z_H, ssec), (da.LINING_Z_L, ssec)], STAINLESS, name)
    obj.data.materials.append(kit._material(GELCOAT))
    mw = obj.matrix_world
    n_bevel = 0
    for poly in obj.data.polygons:
        X, Y, Z = dc.to_unity(mw @ poly.center)
        if abs(X) > BEVEL_X:
            poly.material_index = 1
            n_bevel += 1
    assert n_bevel >= 2, "saddle %s: %d bevel faces" % (name, n_bevel)
    da.tag_lods(obj, levels)
    return obj


def build(kit):
    rng = random.Random(61124)
    sleeves = {}
    for level in (0, 1, 2):
        sec = frp_section(level)
        o = sweep_round(kit, sec, GELCOAT, "frame frp" if level == 0 else "frame frp lod%d" % level, CORNER[level])
        if level == 0:
            sw, sh, sy = da.STRIKE_MORTISE
            dc.cut(o, [dc.cutter((-sw / 2, sy - sh / 2, da.LINING_Z_L - 0.0005), (sw / 2, sy + sh / 2, da.LINING_Z_L + WALL + 0.0005))])
        da.tag_lods(o, str(level))
        sleeves[level] = o
    sw, sh, sy = da.STRIKE_MORTISE
    box = dc.obox(kit, GELCOAT, lo=(-sw / 2 - 0.0015, sy - sh / 2 - 0.002, da.LINING_Z_L + da.STRIKE_DEPTH),
                  hi=(sw / 2 + 0.0015, sy + sh / 2 + 0.002, da.LINING_Z_L + da.STRIKE_DEPTH + 0.003), name="strike box")
    da.tag_lods(box, "0")
    fl = saddle(kit, True, "saddle", "0")
    for z in steel.SADDLE_SCREW_Z:
        s = dc.flat_head(kit, (0.0, da.SADDLE_H, z), (0, 1, 0), STAINLESS, rng, d=0.0080, host=fl, name="saddle screw")
        da.tag_lods(s, "0")
    plain = saddle(kit, False, "saddle plain", "12")
    seals = da.seals(kit, SEAL, "012")
    for level in (0, 1):
        for c in da.pivot_caps(kit, STAINLESS, level):
            da.tag_lods(c, str(level))

    dc.finalize(kit, SMOOTH_ANGLE, da.wear_frame, protect=list(sleeves.values()) + [fl, plain] + seals)

    da.assert_frame(kit, NAME, da.STEEL_FACE, da.STEEL_X1)
    for level in (0, 1, 2):
        o = sleeves[level]
        lo, hi = dc.bounds_unity([o])
        assert abs(hi[0] - da.STEEL_X1) < 1e-4 and abs(lo[0] + da.STEEL_X1) < 1e-4, "frame x %s %s" % (lo[0], hi[0])
        assert abs(hi[1] - (da.LINING_Y + U_OUT)) < 1e-4 and abs(lo[1]) < 1e-6, "frame y %s %s" % (lo[1], hi[1])
        assert abs(lo[2] - (da.LINING_Z_H - U_OUT)) < 1e-4 and abs(hi[2] - (da.LINING_Z_L + U_OUT)) < 1e-4, "frame z %s %s" % (lo[2], hi[2])
        mw = o.matrix_world
        reach = 0.0
        for v in o.data.vertices:
            X, Y, Z = dc.to_unity(mw @ v.co)
            # nothing in the opening (the soffit lines)
            assert Z <= da.LINING_Z_H + 1e-5 or Z >= da.LINING_Z_L - 1e-5 or Y >= da.LINING_Y - 1e-5, "frame vertex in the opening %s" % ((X, Y, Z),)
            reach = max(reach, (da.LINING_Z_H - Z) + Y)
        # the outer head corner is rounded: the frame never reaches the square corner
        square = U_OUT + da.LINING_Y + U_OUT
        lost = (2.0 - math.sqrt(2.0)) * R_HEAD
        assert reach <= square - lost + 0.0006, "head corner not rounded (%.5f vs %.5f)" % (reach, square - lost)
    # the map's door-only trims (45 mm face, 8 mm proud) stay hidden: the steel kit's 1.5 mm band clears their face from behind;
    # the 4 mm moulded band encloses it instead (its back at 0.0871, the trim face at 0.088, the visible face at 0.0911: the trim
    # face lies inside the solid wall, 3.1 mm behind the visible face, and the trim's sides inside the cavity under the band)
    sec = frp_section(0)
    outer_face = max(abs(x) for d, x in sec if 0.002 < d < da.DOOR_TRIM_FACE + 0.002)
    inner_face = min(abs(x) for d, x in sec if 0.002 < d < da.DOOR_TRIM_FACE + 0.002 and abs(x) > da.WALL_X + 0.004)
    assert outer_face >= da.DOOR_TRIM_X + 0.003, "the FRP band's face %.4f must stand in front of the trim face" % outer_face
    assert inner_face <= da.DOOR_TRIM_X and outer_face - da.DOOR_TRIM_X >= 0.003, "the trim face %.4f inside the band's wall %.4f-%.4f" % (da.DOOR_TRIM_X, inner_face, outer_face)
    assert da.STEEL_FACE >= da.DOOR_TRIM_FACE + da.LINING_Z_H + 0.002, "the band covers the trim's width"

    da.frame_anchors(kit)
    kit.no_collider()
    kit.tag("interactable", "door", "door_frame", "pool", "frame_frp", "double_acting", "map_member")
    kit.meta["frameType"] = "frp"
    kit.meta["wall"] = WALL
    kit.meta["headCornerRadius"] = R_HEAD
    da.lod_meta(kit, sys.modules[__name__])
