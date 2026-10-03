"""Mortise-lock escutcheon (tall trim plate), c. 1960-93 US commercial
storeroom door: the LOCKED door family's lockset plate (10_spec §2.1, §3.1).
A 2-1/4" x 8" satin-chrome plate with eased edges, a turned knob boss below
and a seat for the IC mortise cylinder's collar above (0.3 mm recess ring,
a Ø 13 mm clearance hole for the plug under it), held by two exposed
oval-head slotted screws. No maker marks (era lock: generic, unbranded).

Real-world reference: Sargent / Russwin style "full escutcheon" trim for a
mortise lock (02 §4.4: 2-1/4" x 8-10"), cylinder above the knob. Size
0.0572 (X) x 0.2032 (Y) x 0.0020 plate, 0.008 deep over the knob boss.

Origin (PART frame, 10_spec §1.2): the centre of the plate's BACK face, on
the leaf face. Front = Unity +Z (kit -Y) = the leaf face's outward normal.
Placed on the leaf at escutcheon_s / escutcheon_p = (+-0.022, 0.968, 0.920)
(door root) with the face rule of §1.2 (s: Euler(0, 90, 0); p: Euler(0, -90,
0) with scale (-1, 1, 1)).

Anchors (Unity, part frame):
  knob_pivot (0, -0.0315, 0.002)  the knob's spindle origin (boss bore floor)
  cylinder   (0, +0.032, 0.002)   the collar seat (0.3 mm recess ring)
  keyhole    (0, +0.032, 0.0095)  the cylinder shell and plug origins
  keyhole_in (0, +0.032, -0.0905) 0.10 m into the lock (insertion axis)
  keyhole_up (0, +0.132, 0.0095)  cuts-up direction
Motion: static.

Budget (§9.2): LOD0 3,000 tris (asserted +-15 %), LOD1 1,200, LOD2 200;
LOD distances 1.5 / 4 / 12 m. No LOD1 export (part < 1 m: today's importer
would cull it near 1.4 m, §1.8). Screws are flagged fr_lod2_drop.
Slots: Prop_Chrome (dominant, first), Prop_PlasticBlack (screw slots,
countersink and collar-recess shadow lines). VARIANT _Brass.
Detail: 96-segment boss, 1 mm rounded plate edge (3 segments), real slotted
oval heads (48 segments, 0.8 mm slots).
"""

import math

import interact_lock_common as lc

NAME = "Kit_Lock_Escutcheon"
LOD1 = None
LOD1_RATIO = 0.40
LOD2_RATIO = 0.067
LOD_DISTANCES = (1.5, 4.0, 12.0)
BUDGET = 3000
VARIANTS = {"Kit_Lock_Escutcheon_Brass": {lc.CHROME: lc.BRASS}}

W, H, T = 0.0572, 0.2032, 0.0020
CORNER = 0.0032
EDGE_R = 0.0010
KNOB_Y = -0.0315
CYL_Y = 0.032
KEYHOLE_Z = 0.0095
BOSS_R, BOSS_TOP, BORE_R = 0.0150, 0.0080, 0.0100
SCREW_Y = 0.088


def _plate(kit):
    m = lc.Mesh()
    a, b = W / 2, H / 2
    prof = [(0.0, 0.0), (0.0, T - EDGE_R)]
    for i in (1, 2, 3):
        t = math.radians(30.0 * i)
        prof.append((EDGE_R * (1 - math.cos(t)), T - EDGE_R + EDGE_R * math.sin(t)))
    # Support ring 0.4 mm inside the round: the flat face keeps a flat normal
    # (no smoothing fan across the long face triangles).
    prof.append((EDGE_R + 0.0004, T))
    rings = []
    for d, z in prof:
        ring = [m.add((x, y, z)) for x, y in lc.rounded_rect(0, 0, a - d, b - d, CORNER - d, 8)]
        rings.append(ring)
    for A, B in zip(rings, rings[1:]):
        m.bridge(A, B)
    m.face(list(reversed(rings[0])))
    m.face(rings[-1])
    return m.to_object(kit, "escutcheon plate", lc.CHROME)


def _boss():
    """Knob boss: a 96-segment ring Z 0.002 -> 0.008 with a 0.6 mm fillet
    into the plate and an eased top edge; the 20 mm bore for the knob shank
    (Ø 0.019, so only a 0.5 mm ring of it ever shows) steps down to 48
    segments. Its lower half sits inside the plate (union)."""
    m = lc.Mesh()
    fil = 0.0006
    outer = [(BOSS_R + fil, 0.0012), (BOSS_R + fil, T), (BOSS_R, T + fil),
             (BOSS_R, BOSS_TOP - 0.0005), (BOSS_R - 0.0005, BOSS_TOP)]
    inner = [(BORE_R + 0.0003, BOSS_TOP), (BORE_R, BOSS_TOP - 0.0003), (BORE_R, 0.0012)]
    rings = [m.ring(r, z, 96, (0.0, KNOB_Y)) for r, z in outer]
    rings += [m.ring(r, z, 48, (0.0, KNOB_Y)) for r, z in inner]
    for A, B in zip(rings, rings[1:]):
        m.bridge(A, B)
    m.bridge(rings[-1], rings[0])
    return m


def _cylinder_cut(r, z0, z1, cy, segs=96):
    m = lc.Mesh()
    m.lathe([(0.0, z0), (r, z0), (r, z1), (0.0, z1)], segs, (0.0, cy))
    return m


def _wear(p, n, slot):
    x, y, z = p
    r = g = b = 1.0
    dk = math.hypot(x, y - KNOB_Y)
    if dk < 0.045:
        r = 0.80 + 0.20 * (dk / 0.045) ** 2          # hands round the knob
    dc = math.hypot(x, y - CYL_Y)
    if dc < 0.032:
        r = min(r, 0.88 + 0.12 * (dc / 0.032))      # key fumbling round the cylinder
    if z > 0.0011 and abs(n[2]) < 0.97 and dk > BOSS_R + 0.001:
        g = 0.82                                     # plate's rounded front edge
    if dk < BOSS_R + 0.0002 and z > BOSS_TOP - 0.0008 and abs(n[2]) < 0.97:
        g = 0.75                                     # boss top round
    return (r, g, b)


def build(kit):
    lc.assert_keyway()
    plate = _plate(kit)
    lc.boolean(kit, plate, _boss(), op="UNION")
    # Collar seat: a 0.3 mm recess ring (dark shadow line round the collar)
    # and a 29 mm cylinder hole under it (hidden by the collar).
    lc.boolean(kit, plate, _cylinder_cut(0.0225, T - 0.0003, T + 0.002, CYL_Y), slot=lc.DARK)
    lc.boolean(kit, plate, _cylinder_cut(0.0065, -0.001, T + 0.002, CYL_Y, segs=24), slot=lc.DARK)
    for sy in (SCREW_Y, -SCREW_Y):
        lc.boolean(kit, plate, lc.countersink(0.0, sy, T, 0.0035 + 0.00015, cone=0.0012, r_bottom=0.0), slot=lc.DARK)
    lc.finish_part(plate, _wear)
    for sy, ang in ((SCREW_Y, 12.0), (-SCREW_Y, -31.0)):
        s = lc.oval_screw(kit, 0.0, sy, T, D=0.0070, dome=0.0012, slot_w=0.0008, slot_d=0.0008, ang=ang,
                          name="escutcheon screw", dome_rings=(0.5,))
        lc.finish_part(s, lambda p, n, slot: (1.0, 0.85 if p[2] > T + 0.0009 else 1.0, 1.0), lod2=True,
                       delete_below=T + 0.00001)

    kit.anchor("knob_pivot", lc.U(0.0, KNOB_Y, T))
    kit.anchor("cylinder", lc.U(0.0, CYL_Y, T))
    kit.anchor("keyhole", lc.U(0.0, CYL_Y, KEYHOLE_Z))
    kit.anchor("keyhole_in", lc.U(0.0, CYL_Y, KEYHOLE_Z - 0.10))
    kit.anchor("keyhole_up", lc.U(0.0, CYL_Y + 0.10, KEYHOLE_Z))
    lc.common_meta(kit, {"type": "static"}, LOD_DISTANCES, LOD1_RATIO, LOD2_RATIO, (BUDGET, 1200, 200))
    kit.tag("escutcheon", "lock_static")
    # Key numbers (10_spec §3.1).
    assert abs(KNOB_Y - CYL_Y + 0.0635) < 1e-9          # cylinder-to-hub 0.0635 (ESTIMATE in §13)
    assert abs(0.968 + CYL_Y - 1.000) < 1e-9 and abs(0.968 + KNOB_Y - 0.9365) < 1e-9
    lc.check(kit, BUDGET, (-W / 2, -H / 2, 0.0), (W / 2, H / 2, BOSS_TOP), tol=0.00015)
