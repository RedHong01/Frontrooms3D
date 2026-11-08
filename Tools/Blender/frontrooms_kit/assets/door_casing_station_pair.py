"""Station D's face (D-3, Red 2026-11-05 21:15:15 "按默认做"; research/double_doors/20_design.md Part A, sections A3.5 and
A4.2): the start door seen from the station is the station's own door-frame language (E1 / E2: pressed-steel cased frames in
dark bronze, stainless corner guards; walls 2 · Post Room, Red 10-22 "这个版本可以") on the +Z face only. D's stream face,
leaves, lite, saddle and -Z casing stay today's walnut pair byte for byte.

Section, per jamb (the +x jamb; the -x jamb mirrors it, the head runs it along x, mitred), 16 ga, 3 mm convex bends: the rise
at x 1.212 from the walnut lining's +Z end (z 0.02225) to the face, a 38 mm band at z 0.0331 to x 1.250 (today's casing
edge: the crash rail and the base stop 50 mm out and the BUSINESS HOURS plate 75 mm out stay where they are), the return to
z 0.0195 (0.5 mm into the wall face). A bronze cap covers the walnut lining's +Z end from the soffit plane to the rise
(x 1.200-1.212 on the jambs, y 2.660-2.672 on the head), so the station face reads as one steel frame while the lining's
soffit stays walnut for the stream side. Head band top 2.710 (no header panel, C1-5).
Fix A (2026-11-06, Review A finding 2; measured in the game, journal 14:2x): at the back turn the stream's terminal pair stands
in the map's station wall, whose south liner (Station_Wall, z 0.020-0.024, its opening edge on the soffit plane) hid Build A's
0.5 mm cap (z 0.02225-0.02275): a pale 12 mm strip of the wall's paint ran up both jambs. The cap now fills the lining's end
to z 0.025 (1 mm in front of the liner; after ClearStartArea D's data wall face is at 0.020, so it shows there too), its inner
face 0.5 mm into the opening (x 1.1995: no plane shared with the liner's edge at 1.200), and steps down to the 0.5 mm cap only
in a 5.5 mm notch round each hinge barrel (the barrels reach z 0.02325 within x <= 1.2045; asserted clear shut and turned).
Built as a grid of closed boxes on shared breakpoints (no T-junction): LOD0 notched, LOD1 / LOD2 five boxes.

Origin: the pair's door space (as Kit_DoorFrame_SteelPair_DA); spawned at identity under "double door frame" of both builders
(the stream's terminal pair, Faces.Both, and StationDress's static entrance, Faces.Plus): the same parts in the same place,
so the swap at ClearStartArea changes nothing. One slot: Prop_SteelBrown.
LODs (fr_lods): LOD0 3-step bends; LOD1 1-step; LOD2 sharp. Switch 4 / 12 m, never culled.
"""

import sys

import interact_door_common as dc
import door_pair_common as pc

NAME = "Kit_DoorCasing_StationPair"
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0
STEEL = "Prop_SteelBrown"


def casing_section(level):
    u_out = pc.STEP + pc.STATION_FACE
    # walking from the rise's foot up, out and down to the foot, the visible side is on the LEFT (as the frame's +Z half)
    outer = [(pc.STEP, pc.LEAF_HALF_T), (pc.STEP, pc.FACE_Z), (u_out, pc.FACE_Z), (u_out, pc.FOOT_Z)]
    cv = pc.CONVEX
    radii = [0, cv, cv, 0] if level < 2 else [0] * 4
    return pc.sheet_section(outer, radii, 3 if level == 0 else 1)


def _box(kit, lo, hi, level, name):
    o = dc.obox(kit, STEEL, lo=lo, hi=hi, name=name)
    pc.tag(o, level)
    return o


def cap(kit):
    """The cap over the lining's +Z end, as closed boxes on shared breakpoints (Fix A): LOD0 with a notch round each hinge
    barrel, LOD1 / LOD2 the plain fill (two jambs and the head, the head split at the jambs' edges)."""
    z0, z1, zt = pc.LEAF_HALF_T, pc.CAP_FRONT, pc.LEAF_HALF_T + pc.CAP_T
    xin, xn, xo = pc.HALF - pc.CAP_IN, pc.HALF + pc.NOTCH_U, pc.RISE_X
    head0, head1 = pc.HEAD, pc.HEAD + pc.STEP
    # LOD0: the jambs' rows, notched round the barrels (the barrel 203 mm + 1 mm a side)
    ys = [0.0]
    for c in pc.hinge_centres():
        ys += [c - pc.BARREL_L / 2.0 - 0.001, c + pc.BARREL_L / 2.0 + 0.001]
    ys.append(pc.HEAD)
    assert all(b > a for a, b in zip(ys, ys[1:])), ys
    n = 0
    for s in (-1, 1):
        for j in range(len(ys) - 1):
            notch = j % 2 == 1
            for (u0, u1, zz) in ((xin, xn, zt if notch else z1), (xn, xo, z1)):
                xa, xb = (u0, u1) if s > 0 else (-u1, -u0)
                _box(kit, (xa, ys[j], z0), (xb, ys[j + 1], zz), "0", "lining cap %d" % n)
                n += 1
    xs = [-xo, -xn, -xin, xin, xn, xo]
    for i in range(len(xs) - 1):
        _box(kit, (xs[i], head0, z0), (xs[i + 1], head1, z1), "0", "lining cap head %d" % i)
    # LOD1 / LOD2: the plain fill (at 4 m and beyond the 1.2 mm a barrel sinks into it is far below a pixel)
    for s in (-1, 1):
        xa, xb = (xin, xo) if s > 0 else (-xo, -xin)
        _box(kit, (xa, 0.0, z0), (xb, pc.HEAD, z1), "12", "lining cap lod12 %s" % ("p" if s > 0 else "n"))
    for i, (xa, xb) in enumerate(((-xo, -xin), (-xin, xin), (xin, xo))):
        _box(kit, (xa, head0, z0), (xb, head1, z1), "12", "lining cap head lod12 %d" % i)


def build(kit):
    parts = {}
    for level in (0, 1, 2):
        o = pc.opening_sweep(kit, casing_section(level), STEEL, "casing" if level == 0 else "casing lod%d" % level)
        pc.tag(o, str(level))
        parts[level] = o
    cap(kit)
    dc.finalize(kit, SMOOTH_ANGLE)
    lo, hi = pc.bounds(pc.level_parts(kit, 0))
    assert abs(hi[0] - pc.STATION_OUTER) < 1e-4 and abs(lo[0] + pc.STATION_OUTER) < 1e-4, "outer x %s %s" % (lo[0], hi[0])
    assert abs(hi[1] - pc.STATION_TOP) < 1e-4, "top %s" % hi[1]
    assert lo[2] >= pc.FOOT_Z - 1e-5 and abs(hi[2] - pc.FACE_Z) < 1e-4, "z %s %s" % (lo[2], hi[2])
    pc.anchor(kit, "opening_centre", 0.0, 0.0, 0.0)
    pc.anchor(kit, "casing_outer_r", pc.STATION_OUTER, 1.0, pc.FACE_Z)
    kit.meta["casingOuter"] = pc.STATION_OUTER
    kit.meta["casingTop"] = pc.STATION_TOP
    kit.meta["faceZ"] = pc.FACE_Z
    kit.meta["capFront"] = pc.CAP_FRONT
    kit.meta["capNote"] = "the cap fills the lining's +Z end to z 0.025 (1 mm over the map's south liner at 0.024), 0.5 mm into the opening, notched round the barrels at LOD0"
    kit.meta["doorSpace"] = "as Kit_DoorFrame_SteelPair_DA; the +Z face only"
    kit.no_collider()
    kit.tag("door", "door_frame", "frame_steel", "station", "zone_pair")
    dc.lod_meta(kit, sys.modules[__name__])
    kit.meta["lodNote"] = "hand-built LOD0/1/2 (kitlib fr_lods); lodDistances (d01, d12, dcull) m at lodBias 1, FOV 76; -1 = never cull"
