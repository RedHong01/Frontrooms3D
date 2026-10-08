"""Mortise-lock LATCHBOLT with its auxiliary deadlatch plunger: the bolt the
LOCKED door's knob retracts (10_spec §3.1, §3.4 0.92-1.00 s knob beat).
Satin chrome. A 0.0125 x 0.030 latch with a 0.019 throw, the flat side
toward the swing side S (part +X) and a rounded 30 deg bevel facing the
push side P (part -X), so the closing leaf's strike lip rides it in. Beside
it on the S side, a 0.005 x 0.018 deadlatch plunger stands 2.5 mm out of the
front: at the shut door it lands on the strike plate between the openings
(as a real auxiliary latch does) and never enters an opening.

Real-world reference: Sargent / Russwin-type mortise latch with an
auxiliary latch. Sections are ESTIMATES (§13). Modelled length 0.040 runs
back into the leaf.

Origin (PART frame, 10_spec §1.2): on the lock front, on the latch axis:
door (0, 0.9365, 0.9939). Front = Unity +Z (kit -Y) = the throw direction
(door +Z). Placed with identity rotation under the Leaf rig.

AUTHORED EXTENDED (spring rest). Motion: slide along part -Z by 0.019 (the
knob's 40 deg retracts it). The plunger is part of this one rigid mesh and
moves with the latch ("fixed" = it has no motion of its own).
INTERFACE NOTE for G1 (Kit_DoorLeaf_Steel's armor front): the latch opening
must clear X -0.0070 .. +0.0140 (latch plus plunger), Y 0.9200 .. 0.9530
(door root; self-check (d) passes with that opening).
Anchors: bolt_axis (0, 0, 0); tip (0.0055, 0, 0.019); plunger
(0.0105, 0, 0.0025); throw_dir (0, 0, 0.10).
Budget (§9.2): LOD0 600 (asserted +-15 %), LOD1 220, LOD2 40; LOD
distances 1.0 / 3 / 8 m; LOD1 exported since the 2026-10-08 fix pass (critic H4). The plunger is fr_lod2_drop.
Slots: Prop_Chrome.
"""

import interact_door_common as dc
import interact_lock_common as lc

NAME = "Kit_Lock_Latchbolt_Mortise"
LOD1_RATIO = 0.367
LOD1 = LOD1_RATIO  # fix pass 2026-10-08 (critic H4): FrontRoomsKitImporter honours the sidecar distances since 663e858, so LOD1 = LOD0->LOD1 at d01 and a cull at dcull
LOD2_RATIO = 0.067
LOD_DISTANCES = (1.0, 3.0, 8.0)
BUDGET = 600

T, H = 0.0125, 0.030
THROW, BACK = 0.019, -0.021
PX0, PX1, PH, PBACK, PPROUD = 0.0080, 0.0130, 0.018, -0.010, 0.0025


def _wear(p, n, slot):
    x, y, z = p
    g = 0.70 if z > 0.004 and n[0] < 0.3 else 1.0     # the bevel, rubbed by the strike lip
    return (1.0, g, 1.0)


def build(kit):
    dc.lod1_keep_all(kit)   # fix pass 2026-10-08: LOD1 keeps the LOD0 shape (collapse broke it; see dc.lod1_keep_all)
    outline, zb = lc.latch_outline(T, THROW, BACK, land=0.0005, tip_r=0.0005, bevel_deg=30.0,
                                   sagitta=0.0006, arc_n=14)
    m = lc.Mesh()
    lc.extrude_rounded_y(m, outline, -H / 2, H / 2, 0.0005, 3)
    latch = m.to_object(kit, "latchbolt", lc.CHROME)
    lc.finish_part(latch, _wear)
    pm = lc.Mesh()
    lc.extrude_rounded_y(pm, lc.plunger_outline(PX0, PX1, PBACK, PPROUD, 14), -PH / 2, PH / 2, 0.0004, 3)
    plunger = pm.to_object(kit, "deadlatch plunger", lc.CHROME)
    lc.finish_part(plunger, lambda p, n, s: (1.0, 0.75 if p[2] > 0.0005 else 1.0, 1.0), lod2=True)

    kit.anchor("bolt_axis", lc.U(0.0, 0.0, 0.0))
    kit.anchor("tip", lc.U(T / 2 - 0.0007, 0.0, THROW))
    kit.anchor("plunger", lc.U((PX0 + PX1) / 2, 0.0, PPROUD))
    kit.anchor("throw_dir", lc.U(0.0, 0.0, 0.10))
    lc.common_meta(kit, {"type": "slide", "axis": [0, 0, -1], "travel": THROW, "rest": "extended",
                         "note": "spring latch; the knob's 40 deg retracts it 0.019; plunger rides along"},
                   LOD_DISTANCES, LOD1_RATIO, LOD2_RATIO, (BUDGET, 220, 40))
    kit.meta["frontOpening"] = {"x": [-0.0070, 0.0140], "yHalf": 0.0165}
    kit.tag("latchbolt", "lock_moving", "bolt", "mortise")
    assert zb < 0.0, "the bevel must reach the -X face inside the front"
    assert abs(THROW - 0.019) < 1e-9 and abs(T - 0.0125) < 1e-9 and abs(H - 0.030) < 1e-9
    lc.check(kit, BUDGET, (-T / 2, -H / 2, BACK), (PX1, H / 2, THROW), tol=0.0001)
