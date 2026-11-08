"""The zone pairs' frame (zone doors, Build A, 2026-11-06; research/double_doors/20_design.md Part A, section A4): the F4
pressed-steel "cased open" frame of a c. 1990 commercial double-acting pair, for the title / station pair's 2.4 x 2.66 m
opening, in dark bronze (Prop_SteelBrown), both faces alike. Every whole-door zone of Part A wears it (Office O-1, the night
post S-2, back-of-house B-2, Run! C-1); Level 0 keeps its walnut casing (Red 2026-11-05 19:35).

Section, per jamb (the +x jamb; the -x jamb mirrors it, the head runs it along x, mitred, welded and ground: the Feb 1991
spec "trim faces mitered, welded, and finished smooth", door_frame F2, so no joint line), 16 ga with 3 mm convex and 1.5 mm
concave bends: soffit at x 1.200 (the leaf's 3 mm gap and the hinge flanges sit on it, as on today's linings) -> the hinge
step, a 12 mm land at |z| 0.02225 (the DA barrels stand 1 mm in front of it and 2.5 mm from the rise) -> the rise at x 1.212
-> the 50.8 mm face band at |z| 0.0331 (7/16 in proud of the paper plane) to x 1.2628 -> the return to z 0.0195 (0.5 mm
into the wall face). No stop, silencer, strike or label: a non-rated DA frame (door_hardware 10 section 10.5). The saddle
is the Steel_DA saddle in the frame's finish (rule M1 of Kit_DoorFrame_Steel_DA): today's 2.4 x 0.152 x 0.012 m envelope,
12 flutes of 0.8 mm on the flat top, a 12 mm screw land on z 0 and 7 countersunk flat heads.

Origin: the pair's door space (the opening's centre on the floor; x across, y up, z through, +Z the stream's swing face);
spawned at identity under "double door frame". Sidecar casingOuter 1.2628, casingTop 2.7228: TitleVignettes' end trims stop
there (FrontRoomsDoubleDoorKit.TrimStopHalf / Top).

LODs (hand-built, fr_lods): LOD0 3-step bends, fluted saddle and screws; LOD1 1-step bends, the saddle as 3 facets, no
screws; LOD2 sharp bends, the saddle as 3 quads. Switch 4 / 12 m, never culled (a large part). One slot: Prop_SteelBrown.
"""

import random
import sys

import interact_door_common as dc
import door_pair_common as pc

NAME = "Kit_DoorFrame_SteelPair_DA"
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0
STEEL = "Prop_SteelBrown"
SCREW_X = (-1.08, -0.72, -0.36, 0.0, 0.36, 0.72, 1.08)


def frame_outer(face, top_pad=0.0):
    """The visible outer path of one sleeve section in (u, z): u outward from the soffit plane, from the -Z return foot to
    the +Z return foot (the visible side on the left of the walk)."""
    u_out = pc.STEP + face
    zf, zl, zs = pc.FACE_Z, pc.LEAF_HALF_T, pc.FOOT_Z
    return [(u_out, -zs), (u_out, -zf), (pc.STEP, -zf), (pc.STEP, -zl), (0.0, -zl),
            (0.0, zl), (pc.STEP, zl), (pc.STEP, zf), (u_out, zf), (u_out, zs)]


def frame_section(level):
    outer = frame_outer(pc.STEEL_FACE)
    cv, cc = pc.CONVEX, pc.BEND_RI
    radii = [0, cv, cv, cc, cv, cv, cc, cv, cv, 0] if level < 2 else [0] * 10
    return pc.sheet_section(outer, radii, 3 if level == 0 else 1)


def saddle_plain():
    h, f, w = dc.SADDLE_H, dc.SADDLE_FLAT_HALF, dc.SADDLE_HALF
    return [(-w, 0.0), (-f, h), (f, h), (w, 0.0)]


def saddle(kit, flutes, name, levels):
    sec = dc.saddle_section(flutes=True) if flutes else saddle_plain()
    # saddle_section is (across, up): across = the pair's z, up = y; loft_x takes (Y, Z) rings along x
    ring = [(y, z) for z, y in sec]
    obj = dc.loft_x(kit, [(-pc.HALF, ring), (pc.HALF, ring)], STEEL, name)
    pc.tag(obj, levels)
    return obj


def build(kit):
    rng = random.Random(51106)
    sheets = {}
    for level in (0, 1, 2):
        sec = frame_section(level)
        o = pc.opening_sweep(kit, sec, STEEL, "sleeve" if level == 0 else "sleeve lod%d" % level)
        pc.tag(o, str(level))
        sheets[level] = (o, sec)
    sad = saddle(kit, True, "saddle", "0")
    for x in SCREW_X:
        s = dc.flat_head(kit, (x, pc.SADDLE_H, 0.0), (0, 1, 0), STEEL, rng, d=0.0080, host=sad, name="saddle screw")
        pc.tag(s, "0")
    saddle(kit, False, "saddle plain", "12")

    dc.finalize(kit, SMOOTH_ANGLE)

    # the envelope: the outer edge and the head top, the soffit, the faces, the feet
    lo, hi = pc.bounds([sheets[0][0]])
    assert abs(hi[0] - pc.FRAME_OUTER) < 1e-4 and abs(lo[0] + pc.FRAME_OUTER) < 1e-4, "outer x %s %s" % (lo[0], hi[0])
    assert abs(hi[1] - pc.FRAME_TOP) < 1e-4 and abs(lo[1]) < 1e-6, "y %s %s" % (lo[1], hi[1])
    assert abs(hi[2] - pc.FACE_Z) < 1e-4 and abs(lo[2] + pc.FACE_Z) < 1e-4, "z %s %s" % (lo[2], hi[2])
    # nothing of the sleeve inside the opening (the soffit plane x 1.200, y 2.660) or where a shut barrel stands
    for level in (0, 1, 2):
        o = sheets[level][0]
        mw = o.matrix_world
        for v in o.data.vertices:
            X, Y, Z = dc.to_unity(mw @ v.co)
            assert abs(X) >= pc.HALF - 1e-5 or Y >= pc.HEAD - 1e-5, "sleeve vertex in the opening %s" % ((X, Y, Z),)
            if abs(Z) > pc.LEAF_HALF_T + 1e-5 and Y < pc.HEAD - 1e-5:
                assert abs(X) >= pc.BARREL_X_MAX + 0.0025 - 1e-4 or abs(Z) <= pc.LEAF_HALF_T + 1e-4 + 0.003, \
                    "sleeve vertex where a barrel swings %s" % ((X, Y, Z),)

    pc.anchor(kit, "opening_centre", 0.0, 0.0, 0.0)
    pc.anchor(kit, "head_soffit", 0.0, pc.HEAD, 0.0)
    pc.anchor(kit, "casing_outer_r", pc.FRAME_OUTER, 1.0, pc.FACE_Z)
    pc.anchor(kit, "casing_top", 0.0, pc.FRAME_TOP, pc.FACE_Z)
    kit.meta["casingOuter"] = pc.FRAME_OUTER
    kit.meta["casingTop"] = pc.FRAME_TOP
    kit.meta["faceZ"] = pc.FACE_Z
    kit.meta["doorSpace"] = ("the title / station pair's door space (FrontRoomsDoubleDoorKit): origin the opening's centre on the floor, "
                             "x across, y up, z through, +Z the stream's swing face; spawn at identity under 'double door frame'")
    kit.no_collider()
    kit.tag("door", "door_frame", "frame_steel", "double_acting", "zone_pair")
    dc.lod_meta(kit, sys.modules[__name__])
    kit.meta["lodNote"] = "hand-built LOD0/1/2 (kitlib fr_lods); lodDistances (d01, d12, dcull) m at lodBias 1, FOV 76; -1 = never cull"
