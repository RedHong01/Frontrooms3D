"""Cylindrical (bored) lockset LATCHBOLT with its deadlatch plunger: the
FREE door's latch that the lever retracts (10_spec §2.1 D7, §3.1, §3.4
"Open"). A 0.010 x 0.020 latch with a 1/2" (0.013) throw (02 §4.1, Schlage
D-series), flat side toward the swing side S (part +X), a rounded 30 deg
bevel facing the push side P (part -X), and a 0.004 x 0.012 deadlatch
plunger beside it on the S side, 2 mm proud, landing on the strike plate.

Real-world reference: ANSI Grade 1 cylindrical lock latch, 2-3/4"
backset, 1-1/8" x 2-1/4" faceplate (the faceplate itself is on
Kit_DoorLeaf_Veneer, G1). Modelled length 0.030 runs back into the leaf.

Origin (PART frame, 10_spec §1.2): on the faceplate surface, on the latch
axis: door (0, 1.000, 0.9939). Front = Unity +Z (kit -Y) = the throw
direction (door +Z). Placed with identity rotation under the Leaf rig.

AUTHORED EXTENDED. Motion: slide along part -Z by 0.013 with the lever's
35 deg (0.08 s), spring back at 0.12-0.22 s. The plunger rides along.
INTERFACE NOTE for G1 (the bored latch faceplate): its opening must clear
X -0.0057 .. +0.0115 (latch plus plunger), Y 0.9895 .. 1.0105.
Anchors: bolt_axis (0, 0, 0); tip (0.0043, 0, 0.013); plunger
(0.009, 0, 0.002); throw_dir (0, 0, 0.10).
Budget (§9.2): LOD0 500 (asserted +-15 %), LOD1 200, LOD2 40; LOD
distances 1.0 / 3 / 8 m; LOD1 exported since the 2026-10-08 fix pass (critic H4). The plunger is fr_lod2_drop.
Slots: Prop_Chrome. VARIANT _Brass (Lobby free doors, US3/US4).
"""

import interact_lock_common as lc

NAME = "Kit_Lock_Latchbolt_Bored"
LOD1_RATIO = 0.40
LOD1 = LOD1_RATIO  # fix pass 2026-10-08 (critic H4): FrontRoomsKitImporter honours the sidecar distances since 663e858, so LOD1 = LOD0->LOD1 at d01 and a cull at dcull
LOD2_RATIO = 0.08
LOD_DISTANCES = (1.0, 3.0, 8.0)
BUDGET = 500
VARIANTS = {"Kit_Lock_Latchbolt_Bored_Brass": {lc.CHROME: lc.BRASS}}

T, H = 0.010, 0.020
THROW, BACK = 0.013, -0.017
PX0, PX1, PH, PBACK, PPROUD = 0.0070, 0.0110, 0.012, -0.008, 0.0020


def _wear(p, n, slot):
    x, y, z = p
    g = 0.70 if z > 0.003 and n[0] < 0.3 else 1.0
    return (1.0, g, 1.0)


def build(kit):
    outline, zb = lc.latch_outline(T, THROW, BACK, land=0.0004, tip_r=0.0004, bevel_deg=30.0,
                                   sagitta=0.0005, arc_n=12)
    m = lc.Mesh()
    lc.extrude_rounded_y(m, outline, -H / 2, H / 2, 0.0004, 3)
    latch = m.to_object(kit, "latchbolt", lc.CHROME)
    lc.finish_part(latch, _wear)
    pm = lc.Mesh()
    lc.extrude_rounded_y(pm, lc.plunger_outline(PX0, PX1, PBACK, PPROUD, 12), -PH / 2, PH / 2, 0.0003, 2)
    plunger = pm.to_object(kit, "deadlatch plunger", lc.CHROME)
    lc.finish_part(plunger, lambda p, n, s: (1.0, 0.75 if p[2] > 0.0004 else 1.0, 1.0), lod2=True)

    kit.anchor("bolt_axis", lc.U(0.0, 0.0, 0.0))
    kit.anchor("tip", lc.U(T / 2 - 0.0007, 0.0, THROW))
    kit.anchor("plunger", lc.U((PX0 + PX1) / 2, 0.0, PPROUD))
    kit.anchor("throw_dir", lc.U(0.0, 0.0, 0.10))
    lc.common_meta(kit, {"type": "slide", "axis": [0, 0, -1], "travel": THROW, "rest": "extended",
                         "note": "spring latch; the lever's 35 deg retracts it 0.013; plunger rides along"},
                   LOD_DISTANCES, LOD1_RATIO, LOD2_RATIO, (BUDGET, 200, 40))
    kit.meta["frontOpening"] = {"x": [-0.0057, 0.0115], "yHalf": 0.0105}
    kit.tag("latchbolt", "lock_moving", "bolt", "bored")
    assert zb < 0.0
    assert abs(THROW - 0.013) < 1e-9 and abs(T - 0.010) < 1e-9 and abs(H - 0.020) < 1e-9
    lc.check(kit, BUDGET, (-T / 2, -H / 2, BACK), (PX1, H / 2, THROW), tol=0.0001)
