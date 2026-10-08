"""Lever ROSE: the FREE door's round trim plate, one per face under the
passage lever (10_spec §2.1 D7, §3.1). A stepped turning: a Ø 0.070 x 0.003
flange with a 1 mm rounded edge, a flat ring carrying two exposed oval-head
slotted through-bolts, a cove rising to a Ø 0.030 boss whose face (Z 0.010)
carries the lever. Satin chrome (Office) or bright brass (Lobby, VARIANT).

Real-world reference: commercial cylindrical lever rose of the 1980s (Arrow
/ Schlage-type, rose Ø 2-9/16"-3-11/32", 02 §4.1), through-bolted with
exposed screws, unbranded.

Origin (PART frame, 10_spec §1.2): the centre of the BACK face, on the leaf
face, at rose_s / rose_p = (+-0.022, 1.000, 0.920). Front = Unity +Z (kit
-Y). Symmetric (the §1.2 mirror on _p only flips the screw-slot angles).
Anchors: spindle (0, 0, 0.010) = Kit_Lock_Lever's origin (door X +-0.032).
Motion: static.
Budget (§9.2): LOD0 2,400 (asserted +-15 %), LOD1 900, LOD2 120; LOD
distances 1.5 / 4 / 12 m; LOD1 exported since the 2026-10-08 fix pass (critic H4). Screws fr_lod2_drop.
Slots: Prop_Chrome (first), Prop_PlasticBlack (screw slots, countersink
shadow rings). VARIANT _Brass. 96-segment turning, 48-segment screws.
"""

import math

import interact_lock_common as lc

NAME = "Kit_Lock_Rose"
LOD1_RATIO = 0.375
LOD1 = LOD1_RATIO  # fix pass 2026-10-08 (critic H4): FrontRoomsKitImporter honours the sidecar distances since 663e858, so LOD1 = LOD0->LOD1 at d01 and a cull at dcull
LOD2_RATIO = 0.05
LOD_DISTANCES = (1.5, 4.0, 12.0)
BUDGET = 2400
VARIANTS = {"Kit_Lock_Rose_Brass": {lc.CHROME: lc.BRASS}}

FLANGE_R, FLANGE_T = 0.035, 0.003
BOSS_R, TOP = 0.015, 0.010
COVE_R = 0.0045
SCREW_Y = 0.025


def profile():
    rr = 0.001
    c45 = math.radians(45)
    cove_c = (BOSS_R + COVE_R, FLANGE_T + COVE_R)
    prof = [(0.0, 0.0), (FLANGE_R, 0.0), (FLANGE_R, FLANGE_T - rr),
            (FLANGE_R - rr + rr * math.cos(c45), FLANGE_T - rr + rr * math.sin(c45)),
            (FLANGE_R - rr, FLANGE_T),
            (cove_c[0] + 0.0004, FLANGE_T),                 # support ring before the cove
            (cove_c[0], FLANGE_T)]
    for a in (-135.0,):
        t = math.radians(a)
        prof.append((cove_c[0] + COVE_R * math.cos(t), cove_c[1] + COVE_R * math.sin(t)))
    prof += [(BOSS_R, cove_c[1]), (BOSS_R, TOP - 0.0005), (BOSS_R - 0.0005, TOP), (0.0, TOP)]
    return prof


def _wear(p, n, slot):
    x, y, z = p
    rad = math.hypot(x, y)
    r = 0.86 + 0.14 * min(1.0, rad / FLANGE_R)       # hands round the lever
    g = 1.0
    if rad > FLANGE_R - 0.0012 and z > 0.0015:
        g = 0.80                                     # the flange's rounded edge
    if BOSS_R - 0.0006 < rad < BOSS_R + 0.0001 and z > TOP - 0.0006:
        g = 0.75                                     # the boss chamfer
    return (r, g, 1.0)


def build(kit):
    m = lc.Mesh()
    m.lathe(profile(), 96)
    rose = m.to_object(kit, "rose", lc.CHROME)
    for sy in (SCREW_Y, -SCREW_Y):
        lc.boolean(kit, rose, lc.countersink(0.0, sy, FLANGE_T, 0.0035 + 0.00015, cone=0.0012), slot=lc.DARK)
    lc.finish_part(rose, _wear, delete_below=0.00001)          # the back face sits on the leaf
    for sy, ang in ((SCREW_Y, 90.0 - 17.0), (-SCREW_Y, 90.0 + 24.0)):
        s = lc.oval_screw(kit, 0.0, sy, FLANGE_T, D=0.0070, dome=0.0011, slot_w=0.0008, slot_d=0.0007,
                          ang=ang, name="rose screw", dome_rings=(0.5,), rim=0.0)
        lc.finish_part(s, lambda p, n, slot: (1.0, 0.85 if p[2] > FLANGE_T + 0.0008 else 1.0, 1.0),
                       lod2=True, delete_below=FLANGE_T + 0.00001)

    kit.anchor("spindle", lc.U(0.0, 0.0, TOP))
    lc.common_meta(kit, {"type": "static"}, LOD_DISTANCES, LOD1_RATIO, LOD2_RATIO, (BUDGET, 900, 120))
    kit.tag("rose", "lock_static")
    assert abs(2 * FLANGE_R - 0.070) < 1e-9 and abs(TOP - 0.010) < 1e-9 and abs(2 * BOSS_R - 0.030) < 1e-9
    lc.check(kit, BUDGET, (-FLANGE_R, -FLANGE_R, 0.0), (FLANGE_R, FLANGE_R, TOP), tol=0.0001)
