"""HW-DA1 door plates: shared geometry for the push plate and the kick plate (helper module, no NAME, no build).

Research: Documentation/research/door_hardware/10_research.md (2026-10-30), set HW-DA1 (section 9): a c. 1990
double-acting pair gets a push plate and a kick plate on every face, US32D / BHMA 630 satin stainless, 0.050 in
(1.27 mm) thick, bevelled on four sides, fixed with #6 oval-head Phillips screws (San Diego CWP Section 08710, Feb
1992: "Phillips head screws shall be used at exposed conditions"; Rockwood No. 70 plates: #6 x 5/8 OH).
Build notes: research/door_hardware/20_build.md.

PART FRAME (as interact_lock_common, 10_spec section 1.2): geometry in UNITY metres, converted once with lc.U().
Origin = the centre of the plate's BACK face (on the leaf face); front = Unity +Z = the leaf face's outward normal;
+Y up. The back face is not modelled (it lies on the leaf).

UVs (the plate slots): planar, in metres from the plate's bottom-left corner as seen from the front
(u = W/2 - x, v = y + H/2), so a material with _TileSize = the plate's size maps its unique wear map 0..1 unmirrored.
The kick plate's maps hold two plates side by side (A = u 0..0.5, B = u 0.5..1 by the B material's offset).

LODs are hand-built (kitlib P-1b, fr_lods): LOD0 = the bevelled plate with countersunk holes and modelled screws
(each head a domed oval head with a Phillips cross); LOD1 = a 45-degree bevelled slab, 18 tris, the screws baked in
the plate's maps; LOD2 = a slab, 10 tris.
"""

import math

import bmesh

import kitlib
import interact_lock_common as lc

T = 0.00127                 # 0.050 in
BEVEL = 0.0010              # 45-degree chamfer, 1.0 x 1.0 mm (ESTIMATE from the Rockwood drawing)
LAND = T - BEVEL            # 0.27 mm vertical land at the edge
SOFT = 0.00008              # the chamfer's two edges eased in 22.5-degree steps (smooth-shaded highlight, no aliasing)
SUPPORT = 0.0004            # flat support ring inside the chamfer: the face keeps a flat normal
CORNER = 0.0005             # sheared corner radius

SCREW_D = 0.0068            # #6 oval head
SCREW_DOME = 0.0012
SCREW_RIM = 0.00012
SEAT_GAP = 0.00015          # dark ring between the head and the countersink lip
PH_LEN, PH_W, PH_W_TIP, PH_W_FLOOR = 0.0036, 0.0008, 0.0006, 0.00045
PH_DEPTH, PH_DEPTH_TIP = 0.0009, 0.0004

PLATE = "Prop_PushPlate630"     # push plate: unique 630 map
KICK = "Prop_KickPlate630"      # kick plate A (left leaf)
KICK_B = "Prop_KickPlate630B"   # kick plate B (right leaf): the same maps, the other half
STEEL = "Prop_Stainless630"     # screws: plain satin 630
DARK = lc.DARK

# Blender preview colours only (the look is the Unity material).
kitlib.register_slot(PLATE, (0.64, 0.63, 0.60), 0.38, 1.0)
kitlib.register_slot(KICK, (0.64, 0.63, 0.60), 0.42, 1.0)
kitlib.register_slot(KICK_B, (0.64, 0.63, 0.60), 0.42, 1.0)
kitlib.register_slot(STEEL, (0.64, 0.63, 0.60), 0.40, 1.0)


def profile():
    """(inset, z) rings from the back edge to the flat face: land, eased 45-degree chamfer, support ring."""
    e = SOFT
    s = math.sqrt(0.5)
    return [(0.0, 0.0), (0.0, LAND - e), (s * e, LAND + s * e), (BEVEL - s * e, T - s * e), (BEVEL + e, T),
            (BEVEL + e + SUPPORT, T)]


def plate_lod0(kit, W, H, slot, k=3):
    m = lc.Mesh()
    lc.plate_sweep(m, 0.0, 0.0, W / 2, H / 2, CORNER, k, profile())
    return m.to_object(kit, "plate", slot)


def _rect(a, b):
    return [(a, -b), (a, b), (-a, b), (-a, -b)]


def plate_lod1(kit, W, H, slot):
    """45-degree bevelled slab, square corners: 2 bands x 4 quads + the face = 18 tris (back deleted)."""
    m = lc.Mesh()
    rings = []
    for d, z in ((0.0, 0.0), (0.0, LAND), (BEVEL, T)):
        rings.append([m.add((x, y, z)) for x, y in _rect(W / 2 - d, H / 2 - d)])
    for A, B in zip(rings, rings[1:]):
        m.bridge(A, B)
    m.face(rings[-1])
    return m.to_object(kit, "plate LOD1", slot)


def plate_lod2(kit, W, H, slot):
    """A slab: the face and four sides = 10 tris (back deleted)."""
    m = lc.Mesh()
    lo = [m.add((x, y, 0.0)) for x, y in _rect(W / 2, H / 2)]
    hi = [m.add((x, y, T)) for x, y in _rect(W / 2, H / 2)]
    m.bridge(lo, hi)
    m.face(hi)
    return m.to_object(kit, "plate LOD2", slot)


def planar_uv(obj, W, H):
    """Plate UVs in metres from the bottom-left corner seen from the front (u = W/2 - x, v = y + H/2)."""
    me = obj.data
    bm = bmesh.new()
    bm.from_mesh(me)
    layer = bm.loops.layers.uv.verify()
    for f in bm.faces:
        for loop in f.loops:
            x, y, _z = lc.to_unity(loop.vert.co)
            loop[layer].uv = (W / 2 - x, y + H / 2)
    bm.to_mesh(me)
    bm.free()
    obj["fr_uv"] = "keep"
    return obj


def seat(kit, plate, cx, cy, segs):
    """The countersunk hole for one oval head: a 0.3 mm dark lip SEAT_GAP outside the head's rim."""
    lc.boolean(kit, plate, lc.countersink(cx, cy, T, SCREW_D / 2 + SEAT_GAP, cone=0.0009, segs=segs), slot=DARK)


def _wing(cx, cy, top, ang):
    """One arm pair of the Phillips cross along the screw-local u axis turned by ang: a trapezoid groove whose floor
    falls from PH_DEPTH_TIP at the tips to PH_DEPTH at the centre, its walls sloped (top PH_W, floor PH_W_FLOOR)."""
    ca, sa = math.cos(math.radians(ang)), math.sin(math.radians(ang))
    m = lc.Mesh()
    secs = []
    half = PH_LEN / 2
    for u in (-half - 0.0002, 0.0, half + 0.0002):
        t = min(1.0, abs(u) / half)
        depth = PH_DEPTH + (PH_DEPTH_TIP - PH_DEPTH) * t
        w = PH_W + (PH_W_TIP - PH_W) * t
        wf = PH_W_FLOOR * (1.0 - 0.35 * t)
        zf, zt = top - depth, top + 0.0015
        sec = []
        for v, z in ((-wf / 2, zf), (wf / 2, zf), (w / 2 + 0.0002, zt), (-w / 2 - 0.0002, zt)):
            sec.append((cx + u * ca - v * sa, cy + u * sa + v * ca, z))
        secs.append(sec)
    m.loft(secs)
    return m


def _cone(cx, cy, top):
    m = lc.Mesh()
    m.lathe([(0.0, top - 0.00095), (0.00092, top + 0.0006), (0.0, top + 0.0006)], 8, (cx, cy))
    return m


def phillips_screw(kit, cx, cy, zs, ang, segs, name="plate screw"):
    """#6 oval-head Phillips screw: the head's rim on the plate face z = zs, a spherical dome SCREW_DOME high, the
    cross recess turned by ang (installs are random). The underside sits in the countersink and is deleted."""
    r = SCREW_D / 2
    dome = SCREW_DOME
    R = (r * r + dome * dome) / (2 * dome)
    zc = zs + SCREW_RIM + dome - R
    prof = [(0.0, zs - 0.0006), (r - 0.0004, zs - 0.0006), (r, zs), (r, zs + SCREW_RIM)]
    for t in (0.5,):
        a = math.asin(r / R) * (1 - t)
        prof.append((R * math.sin(a), zc + R * math.cos(a)))
    top = zs + SCREW_RIM + dome
    prof.append((0.0, top))
    m = lc.Mesh()
    m.lathe(prof, segs, (cx, cy), a0=math.radians(ang))
    obj = m.to_object(kit, name, STEEL)
    lc.boolean(kit, obj, _wing(cx, cy, top, ang), slot=DARK)
    lc.boolean(kit, obj, _wing(cx, cy, top, ang + 90.0), slot=DARK)
    lc.boolean(kit, obj, _cone(cx, cy, top), slot=DARK)
    return obj


def head_wear(zs):
    return lambda p, n, slot: (1.0, 0.85 if p[2] > zs + SCREW_DOME * 0.75 else 1.0, 1.0)


def lods(obj, levels):
    obj["fr_lods"] = levels
    return obj


def meta(kit, kind, W, H, screws, lod_distances, budget, ratios):
    kit.no_collider()
    kit.tag("door_hardware", kind, "hw_da1")
    kit.meta["motion"] = {"type": "static"}
    kit.meta["lodDistances"] = list(lod_distances)
    kit.meta["lodRatios"] = list(ratios)
    kit.meta["lodBudget"] = list(budget)
    kit.meta["partFrame"] = "origin = the plate's back centre on the leaf face; front = Unity +Z (kit -Y); +Y up"
    kit.meta["plate"] = {"w": W, "h": H, "t": T, "bevel": BEVEL, "land": LAND, "corner": CORNER, "finish": "US32D / BHMA 630",
                         "screw": "#6 oval head Phillips, d %.4f dome %.4f" % (SCREW_D, SCREW_DOME),
                         "uv": "metres from the bottom-left corner seen from the front (u = W/2 - x, v = y + H/2)"}
    kit.anchor("back_centre", lc.U(0.0, 0.0, 0.0))
    kit.anchor("face_centre", lc.U(0.0, 0.0, T))
    for i, (sx, sy) in enumerate(screws):
        kit.anchor("screw_%d" % i, lc.U(sx, sy, T))


def count(kit, label):
    n = {}
    for o in kit.parts:
        lv = str(o.get("fr_lods", "0"))
        n[lv] = n.get(lv, 0) + lc.tris(o)
        print("[hw_da1]    %-22s LOD %s %5d tris  slots %s" % (o.name, lv, lc.tris(o), [mm.name for mm in o.data.materials]))
    print("[hw_da1] %s tris by LOD: %s" % (label, n))
    return n
