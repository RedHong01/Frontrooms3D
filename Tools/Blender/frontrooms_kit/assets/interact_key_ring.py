"""Split key ring: the nickel-plated flat-wire double coil every facility key
hangs on (spec 10 §4.1; 02 §8.2).

Real-world reference: a 1" (25 mm) flat split ring, flat steel wire about
1.6 mm wide and 0.9 mm thick, two turns pressed together; one end crosses
over to the other coil at the split, and both ends are cut on a 30 deg
slant so the split shows (that is where a thumbnail opens it).

Origin = THE TOP INNER POINT of the ring (where it sits on a hook: anchor
hook_contact). The ring lies in the Unity XY plane, front +Z = the ring
normal (kit front, Blender -Y). Outside Ø 0.025, inside Ø 0.0218, the two
coils stack 1.8 mm along Z (Z -0.0009 .. +0.0009). The split sits at the
upper right (35 deg), away from the hook and from the two hang points.
Anchors (§4.1): hook_contact (0, 0, 0); key_contact (-0.004, -0.0215, 0)
and tag_contact (+0.004, -0.0213, 0) - the points where the key's ring_hole
and the tag's hole go (each wire passes through its hole with clearance:
hole Ø 4.8 / 5.0 vs a 1.6 x 1.8 mm wire section); centre (0, -0.0109, 0).

Budget §9.3: 2,000 / 600 / 96 tris, LOD distances 1.0 / 3 / 12 m; LOD1 exported since the 2026-10-08 fix pass (critic H4).
128 segments per turn with a 4-sided (flat bar) section = 2 x 128 x 8 tris:
the §4.1 "8-sided section" would be 4,096 tris, twice the budget, and its
0.1 mm corner rounds are sub-pixel at 0.3 m (deviation, reported).
Slot Prop_Chrome. Era: timeless (split rings since the 19th century).
"""

import math

import bmesh
from mathutils import Vector

import interact_key_common as kc
from interact_key_common import U

NAME = "Kit_KeyRing"
LOD1_RATIO, LOD2_RATIO = 600 / 2000, 96 / 2000
LOD1 = LOD1_RATIO  # fix pass 2026-10-08 (critic H4): FrontRoomsKitImporter honours the sidecar distances since 663e858, so LOD1 = LOD0->LOD1 at d01 and a cull at dcull
LOD_DISTANCES = (1.0, 3.0, 12.0)
BUDGET = (2000, 600, 96)

CHROME = "Prop_Chrome"
R_OUT = 0.0125
WIDTH = 0.0016          # radial
THICK = 0.0009          # axial pitch per coil
SEAM = 0.00005          # the hairline between the two coils (reads as the coil seam)
R_IN = R_OUT - WIDTH
R_C = R_OUT - WIDTH / 2
SEG_PER_TURN = 128
SPLIT_DEG = 35.0
CROSS_DEG = 11.25       # half-width of the crossover (4 segments)
GAP_DEG = 2.8125        # each cut end stops one segment short of the crossover
END_SLANT = THICK / math.tan(math.radians(30.0))


def build(kit):
    C = (0.0, -R_IN)
    step = 360.0 / SEG_PER_TURN
    a_start = SPLIT_DEG + CROSS_DEG + GAP_DEG
    a_end = SPLIT_DEG + 720.0 - CROSS_DEG - GAP_DEG
    n = int(round((a_end - a_start) / step))
    a_tr0, a_tr1 = SPLIT_DEG + 360.0 - CROSS_DEG, SPLIT_DEG + 360.0 + CROSS_DEG

    def zc(a):
        if a <= a_tr0:
            return -THICK / 2
        if a >= a_tr1:
            return THICK / 2
        t = (a - a_tr0) / (a_tr1 - a_tr0)
        t = t * t * (3 - 2 * t)
        return -THICK / 2 + THICK * t

    bm = bmesh.new()
    rings = []
    for i in range(n + 1):
        a = a_start + (a_end - a_start) * i / n
        r = math.radians(a)
        radial = Vector((math.cos(r), math.sin(r), 0.0))
        tang = Vector((-math.sin(r), math.cos(r), 0.0))
        z = zc(a)
        corners = []
        h = (THICK - SEAM) / 2
        zz = z + (SEAM / 2 if z > 0 else -SEAM / 2) * min(1.0, abs(z) / (THICK / 2))
        for (dr, dz) in ((-WIDTH / 2, -h), (WIDTH / 2, -h), (WIDTH / 2, h), (-WIDTH / 2, h)):
            p = Vector((C[0], C[1], 0.0)) + radial * (R_C + dr) + Vector((0, 0, zz + dz))
            # 30 deg slant on both cut ends: the face leans along the wire
            if i == 0:
                p += tang * (END_SLANT * (0.5 - (dz + h) / (2 * h)))
            elif i == n:
                p -= tang * (END_SLANT * ((dz + h) / (2 * h) - 0.5))
            corners.append(bm.verts.new(U(p.x, p.y, p.z)))
        rings.append(corners)
    for a, b in zip(rings, rings[1:]):
        for k in range(4):
            j = (k + 1) % 4
            bm.faces.new((a[k], a[j], b[j], b[k]))
    bm.faces.new(list(reversed(rings[0])))
    bm.faces.new(rings[-1])
    ring = kc.new_obj(kit, bm, CHROME, "split ring")
    kc.paint_wear(ring)

    kit.anchor("hook_contact", U(0, 0, 0))
    kit.anchor("key_contact", U(-0.004, -0.0215, 0))
    kit.anchor("tag_contact", U(0.004, -0.0213, 0))
    kit.anchor("centre", U(0, -R_IN, 0))
    kit.anchor("normal_dir", U(0, -R_IN, 0.10))
    kit.no_collider()
    kit.tag("interactable", "key", "key_ring")
    kc.lod_meta(kit, LOD_DISTANCES, BUDGET)
    kit.meta["ring"] = {"outsideD": 2 * R_OUT, "insideD": 2 * R_IN, "wire": [WIDTH, THICK], "turns": 2,
                        "segmentsPerTurn": SEG_PER_TURN, "splitDeg": SPLIT_DEG}

    lo, hi = kc.eval_bounds_unity(kit)
    assert abs(hi[1] - WIDTH) < 1e-4 and abs(lo[1] + R_IN + R_OUT) < 1e-4, ("ring top/bottom", lo, hi)
    assert abs((hi[0] - lo[0]) - 2 * R_OUT) < 2e-4, ("diameter", lo, hi)
    assert abs((hi[2] - lo[2]) - 2 * THICK) < 1e-5, ("stack", lo, hi)
    # the hang points lie inside the wire's bottom section
    for x, y in ((-0.004, -0.0215), (0.004, -0.0213)):
        rr = math.hypot(x - C[0], y - C[1])
        assert R_IN - 1e-4 <= rr <= R_OUT, (x, y, rr)
    kc.check_budget(kit, BUDGET[0])
