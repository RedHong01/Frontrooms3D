"""The pool pair's frame (zone doors, Build B, 2026-11-06; research/double_doors/20_design.md Part B, sections B3.2 and B8): PD-A,
the one-piece moulded FRP "cased open" frame of a c. 1990 commercial double-acting pair, for the title / station pair's 2.4 x 2.66 m
opening, in the pool's course aqua gel coat (Door_PoolGelcoat_Frame), both faces alike, with a fluted stainless saddle. Worn by the
title Pool and Dark pool doors (FrontRoomsDoubleDoorKit Zone.Pool, PoolFinish.Frp).

Section: Part A's F4 envelope (door_frame_steel_pair_da.frame_outer, the same outer path), in a 4 mm moulded wall (the period's
solid FRP frames, moulded "with no jointed corners", research Part P), per jamb (+x jamb; the -x jamb mirrors it, the head runs
it along x): soffit at x 1.200 (the leaf's 3 mm gap and the hinge flanges sit on it) -> the hinge step, a 12 mm land at |z| 0.02225
(the DA barrels stand 1 mm in front of it and 2.5 mm from the rise) -> the rise at x 1.212 -> the 50.8 mm face band at |z| 0.0331
to x 1.2628 -> the return to z 0.0195 (0.5 mm into the wall face, as Build A's steel kit: no slit on the tile or the paper). Moulded
arrises r 4 mm, the land's concave corner r 1.5 mm (the steel kit's: the barrels' 2.5 mm clearance is unchanged). In elevation the
face band's two outer head corners are rounded r 12.7 mm (one moulding, no mitre and no joint line); its inner corners stay square
(the leaves' corners need them). No stop, silencer, strike or label: a non-rated DA frame (door_hardware 10 section 10.5). The
saddle is the steel kit's (today's 2.4 x 0.152 x 0.012 m envelope, 12 flutes of 0.8 mm, a 12 mm screw land on z 0, 7 countersunk
flat heads) in 630 stainless (Prop_Stainless630, its own slot): the threshold where the Level 0 carpet meets the deck tile, fluted
against a wet sole.

Fix B (2026-11-06, Review B finding 4): the saddle is a stainless fluted tread in a moulded FRP threshold: its two 1:2 bevels, their
1 mm eased arrises and the 0.6 mm lips (every face more than 46 mm off the centre line, i.e. outside the flutes and the screw land)
are the frame's own gel coat (slot Door_PoolGelcoat_Frame), so the frame's aqua runs round the opening into the threshold. Why: the
title's unshadowed fill light ("Soft ambient direction", directional, 70 deg down) made the stainless arris a 1-2 px specular line
along the saddle's front edge seen from the dark pool (Descent door 6: 141/255 against a floor of 37; isolated in the title: off
with that light, unchanged with every lamp of both pools off, the reflection probes off or anchored, the water light off; gone with
a non-metal there). The gel coat (a dielectric) takes the same light as the deck; the fluted top stays 630. No new slot, no new draw.

Origin: the pair's door space (the opening's centre on the floor; x across, y up, z through, +Z the stream's swing face);
spawned at identity under "double door frame". Sidecar casingOuter 1.2628, casingTop 2.7228 (Part A's envelope): TitleVignettes'
pool band ends stop there (FrontRoomsDoubleDoorKit.TrimStopHalfFor(Zone.Pool)), so the aqua band runs into the aqua frame.

LODs (hand-built, fr_lods): LOD0 4-segment arrises, 8-segment head corners, the fluted saddle and its screws; LOD1 2-segment
arrises, 4-segment head corners, the saddle as 3 facets, no screws; LOD2 sharp, 2-segment head corners, the saddle as 3 quads.
Switch 4 / 12 m, never culled (a large part). Slots: Door_PoolGelcoat_Frame (the frame and, since Fix B, the saddle's bevels),
Prop_Stainless630 (the fluted tread and its screws).
"""

import math
import random
import sys

import bmesh

import interact_door_common as dc
import door_pair_common as pc
import door_frame_steel_pair_da as steel
import kitlib

NAME = "Kit_DoorFrame_FRPPair_DA"
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0
GELCOAT = "Door_PoolGelcoat_Frame"
STAINLESS = "Prop_Stainless630"
# preview colours only (Unity maps the slots to Resources/Surfaces by name): the course aqua (sRGB 102 194 196), 630 satin
kitlib.register_slot(GELCOAT, (0.133, 0.539, 0.552), 0.22, 0.0)
kitlib.register_slot(STAINLESS, (0.64, 0.63, 0.60), 0.35, 0.9)

WALL = 0.004                              # the moulded wall
R_CONVEX = 0.004                          # the moulded arrises
R_CONCAVE = 0.0015                        # the land's inside corner (the steel kit's)
R_HEAD = 0.0127                           # the face band's outer head corners, in elevation (1/2 in)
SCREW_X = steel.SCREW_X
U_OUT = pc.STEP + pc.STEEL_FACE           # 0.0628: the face band's outer edge off the soffit plane
U_ROUND = U_OUT - R_HEAD                  # section points beyond this follow the rounded head corner

SEGS = {0: 4, 1: 2, 2: 0}                 # arris segments per level
CORNER = {0: 8, 1: 4, 2: 2}               # head-corner segments per level


def frp_section(level):
    """The closed (u, z) section: the visible outer path (Part A's F4 envelope) with moulded arrises, and the cavity side offset
    4 mm into the frame (its convex corners sharp, its concave corner r 1.5 + 4 mm), the visible side on the left of the walk."""
    outer = steel.frame_outer(pc.STEEL_FACE)
    n = len(outer)
    conc = (3, 6)                          # the land's inside corners (indices into frame_outer's 10 points)
    if level < 2:
        r_out = [0.0] + [R_CONCAVE if i in conc else R_CONVEX for i in range(1, n - 1)] + [0.0]
        r_in = [0.0] + [R_CONCAVE + WALL if i in conc else 0.0 for i in range(1, n - 1)] + [0.0]
    else:
        r_out = [0.0] * n
        r_in = [0.0] * n
    segs = max(SEGS[level], 1)
    o = dc.fillet_polygon(outer, r_out, segs, closed=False)
    inner_sharp = dc.offset_polyline(outer, WALL, normals_left=False, closed=False)
    i = dc.fillet_polygon(inner_sharp, r_in, segs, closed=False)
    return o + list(reversed(i))


def _corner_point(u, z, theta, plus):
    """A section point (u, z) on the head-corner ring at angle theta (0: the jamb's run, 90 deg: the head's), +x or -x corner."""
    if u > U_ROUND + 1e-9:
        r = u - U_ROUND
        x = pc.HALF + U_ROUND + r * math.cos(theta)
        y = pc.HEAD + U_ROUND + r * math.sin(theta)
    else:
        x = pc.HALF + u                    # the square inner corner: on the 45 deg mitre, the same point for every theta
        y = pc.HEAD + u
    return (x if plus else -x, y, z)


def opening_sweep_round(kit, section, slot, name, corner_segs):
    """The closed (u, z) section swept round the opening, the jambs from the floor, the head corners turned through corner_segs
    rings: points of the face band's outer edge (u > U_ROUND) follow an r 12.7 mm round, the rest keep the square mitre (shared
    vertices: their quads become triangles there, or vanish). No caps (the jamb feet stand on the floor)."""
    rings = []                             # (list of (x, y, z), u-direction (dx, dy) for the faces' orientation)
    rings.append(([(pc.HALF + u, 0.0, z) for u, z in section], (1.0, 0.0)))
    for k in range(corner_segs + 1):
        th = 0.5 * math.pi * k / corner_segs
        rings.append(([_corner_point(u, z, th, True) for u, z in section], (math.cos(th), math.sin(th))))
    for k in range(corner_segs + 1):
        th = 0.5 * math.pi * (1.0 - k / corner_segs)
        rings.append(([_corner_point(u, z, th, False) for u, z in section], (-math.cos(th), math.sin(th))))
    rings.append(([(-(pc.HALF + u), 0.0, z) for u, z in section], (-1.0, 0.0)))
    bm = bmesh.new()
    verts = {}

    def vert(p):
        key = (round(p[0], 9), round(p[1], 9), round(p[2], 9))
        v = verts.get(key)
        if v is None:
            v = verts[key] = bm.verts.new(pc.U(*p))
        return v

    vr = [[vert(p) for p in ring] for ring, _ in rings]
    n = len(section)
    made = set()
    for s in range(len(rings) - 1):
        a, b = vr[s], vr[s + 1]
        du = rings[s][1]
        dv = rings[s + 1][1]
        mx, my = du[0] + dv[0], du[1] + dv[1]
        ml = math.hypot(mx, my) or 1.0
        mx, my = mx / ml, my / ml
        for i in range(n):
            j = (i + 1) % n
            quad = [a[i], a[j], b[j], b[i]]
            uniq = []
            for v in quad:
                if v not in uniq:
                    uniq.append(v)
            if len(uniq) < 3:
                continue
            key = frozenset(uniq)
            if key in made:
                continue
            made.add(key)
            nu, nz = pc._edge_out(section, i)
            pc._face(bm, uniq, (nu * mx, nu * my, nz))
    return dc._part(kit, bm, slot, name)


BEVEL_Z = 0.046                           # Fix B: saddle faces whose centre lies further than this off z 0 (the bevels, the arrises,
                                          # the lips; the flutes end at 0.0405, the flat top at 0.052) take the frame's gel coat


def saddle(kit, flutes, name, levels):
    sec = dc.saddle_section(flutes=True) if flutes else steel.saddle_plain()
    ring = [(y, z) for z, y in sec]
    obj = dc.loft_x(kit, [(-pc.HALF, ring), (pc.HALF, ring)], STAINLESS, name)
    # Fix B: the moulded FRP threshold's bevels in the frame's gel coat; the fluted tread stays 630
    obj.data.materials.append(kit._material(GELCOAT))
    mw = obj.matrix_world
    n_bevel = 0
    for poly in obj.data.polygons:
        X, Y, Z = dc.to_unity(mw @ poly.center)
        if abs(Z) > BEVEL_Z:
            poly.material_index = 1
            n_bevel += 1
    assert n_bevel >= 2, "saddle %s: %d bevel faces" % (name, n_bevel)
    pc.tag(obj, levels)
    return obj


def build(kit):
    rng = random.Random(51116)
    sleeves = {}
    for level in (0, 1, 2):
        sec = frp_section(level)
        o = opening_sweep_round(kit, sec, GELCOAT, "frame frp" if level == 0 else "frame frp lod%d" % level, CORNER[level])
        pc.tag(o, str(level))
        sleeves[level] = o
    sad = saddle(kit, True, "saddle", "0")
    for x in SCREW_X:
        s = dc.flat_head(kit, (x, pc.SADDLE_H, 0.0), (0, 1, 0), STAINLESS, rng, d=0.0080, host=sad, name="saddle screw")
        pc.tag(s, "0")
    saddle(kit, False, "saddle plain", "12")

    dc.finalize(kit, SMOOTH_ANGLE)

    # the envelope: Part A's outer edge and head top, the faces, the feet
    for level in (0, 1, 2):
        lo, hi = pc.bounds([sleeves[level]])
        assert abs(hi[0] - pc.FRAME_OUTER) < 1e-4 and abs(lo[0] + pc.FRAME_OUTER) < 1e-4, "outer x %s %s" % (lo[0], hi[0])
        assert abs(hi[1] - pc.FRAME_TOP) < 1e-4 and abs(lo[1]) < 1e-6, "y %s %s" % (lo[1], hi[1])
        assert abs(hi[2] - pc.FACE_Z) < 1e-4 and abs(lo[2] + pc.FACE_Z) < 1e-4, "z %s %s" % (lo[2], hi[2])
    for level in (0, 1, 2):
        o = sleeves[level]
        mw = o.matrix_world
        corner_reach = 0.0
        for v in o.data.vertices:
            X, Y, Z = dc.to_unity(mw @ v.co)
            # nothing in the opening (the soffit plane x 1.200, y 2.660)
            assert abs(X) >= pc.HALF - 1e-5 or Y >= pc.HEAD - 1e-5, "frame vertex in the opening %s" % ((X, Y, Z),)
            # nothing where a shut or swinging barrel stands
            if abs(Z) > pc.LEAF_HALF_T + 1e-5 and Y < pc.HEAD - 1e-5:
                assert abs(X) >= pc.BARREL_X_MAX + 0.0025 - 1e-4 or abs(Z) <= pc.LEAF_HALF_T + 1e-4 + 0.003, \
                    "frame vertex where a barrel swings %s" % ((X, Y, Z),)
            # past the rise (the face band and its return) nothing goes deeper than the feet, 0.5 mm into the wall face
            if abs(X) > pc.RISE_X + WALL + 0.001 and Y < pc.HEAD:
                assert abs(Z) >= pc.FOOT_Z - 1e-6, "a vertex behind the feet %s" % ((X, Y, Z),)
            corner_reach = max(corner_reach, abs(X) + Y)
        # the outer head corners are rounded: the frame never reaches the square corner (x 1.2628, y 2.7228)
        square = pc.FRAME_OUTER + pc.FRAME_TOP
        lost = (2.0 - math.sqrt(2.0)) * R_HEAD
        assert corner_reach <= square - lost + 0.0006, "head corner not rounded (%.5f vs %.5f)" % (corner_reach, square - lost)

    pc.anchor(kit, "opening_centre", 0.0, 0.0, 0.0)
    pc.anchor(kit, "head_soffit", 0.0, pc.HEAD, 0.0)
    pc.anchor(kit, "casing_outer_r", pc.FRAME_OUTER, 1.0, pc.FACE_Z)
    pc.anchor(kit, "casing_top", 0.0, pc.FRAME_TOP, pc.FACE_Z)
    kit.meta["casingOuter"] = pc.FRAME_OUTER
    kit.meta["casingTop"] = pc.FRAME_TOP
    kit.meta["faceZ"] = pc.FACE_Z
    kit.meta["wall"] = WALL
    kit.meta["headCornerRadius"] = R_HEAD
    kit.meta["doorSpace"] = ("the title / station pair's door space (FrontRoomsDoubleDoorKit): origin the opening's centre on the floor, "
                             "x across, y up, z through, +Z the stream's swing face; spawn at identity under 'double door frame'")
    kit.no_collider()
    kit.tag("door", "door_frame", "frame_frp", "double_acting", "zone_pair", "pool")
    dc.lod_meta(kit, sys.modules[__name__])
    kit.meta["lodNote"] = "hand-built LOD0/1/2 (kitlib fr_lods); lodDistances (d01, d12, dcull) m at lodBias 1, FOV 76; -1 = never cull"
