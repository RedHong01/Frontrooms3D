"""Mortise-lock DOUBLE-BEVEL LATCHBOLT: the bolt the LOCKED double-acting
door's knob retracts (D1.6, A12 / A19; 10_design §2.1, D6). Satin chrome, a
0.0125 x 0.030 latch with a 0.019 throw and a rounded 30 deg bevel on BOTH
faces, no auxiliary plunger (see Kit_Lock_Latchbolt_Bored_DA). Sections are
ESTIMATES. Modelled length 0.040 runs back into the leaf.

Origin (PART frame): on the lock front, on the latch axis: door
(0, 0.9365, 0.995). Front = Unity +Z = the throw. Identity rotation under
the Leaf rig. AUTHORED EXTENDED. Motion: slide along part -Z by 0.019 (the
knob's 40 deg). Anchors: bolt_axis; tip (0, 0, 0.019); throw_dir.
LODs (hand-built): 520 / ~210 / ~40 tris; distances 1 / 3 / 8 m. Slot Prop_Chrome.
"""

import interact_door_da_common as da
import interact_lock_common as lc

NAME = "Kit_Lock_Latchbolt_Mortise_DA"
LOD1_RATIO = 0.40
LOD2_RATIO = 0.08
LOD_DISTANCES = (1.0, 3.0, 8.0)
BUDGET = 520

T, H = 0.0125, 0.030
THROW, BACK = 0.019, -0.021


def _wear(p, n, slot):
    x, y, z = p
    g = 0.70 if z > 0.006 and abs(n[0]) > 0.2 else 1.0
    return (1.0, g, 1.0)


def build(kit):
    zb = None
    for level in (0, 1, 2):
        obj, zb_l = da.build_latch_da(kit, T, H, THROW, BACK, lc.CHROME, level, arc_n=12, round_r=0.0005, round_k=3)
        lc.finish_part(obj, _wear)
        if level == 0:
            zb = zb_l
    kit.anchor("bolt_axis", lc.U(0.0, 0.0, 0.0))
    kit.anchor("tip", lc.U(0.0, 0.0, THROW))
    kit.anchor("throw_dir", lc.U(0.0, 0.0, 0.10))
    lc.common_meta(kit, {"type": "slide", "axis": [0, 0, -1], "travel": THROW, "rest": "extended",
                         "note": "double-bevel spring latch (DA); the knob's 40 deg retracts it 0.019"},
                   LOD_DISTANCES, LOD1_RATIO, LOD2_RATIO, (BUDGET, 210, 40))
    kit.meta["frontOpening"] = {"x": [-0.00625, 0.00625], "yHalf": 0.0165}
    kit.meta["acting"] = "double"
    kit.tag("latchbolt", "lock_moving", "bolt", "mortise", "double_acting")
    assert zb > 0.0
    n0 = da.lock_tris(kit, 0)
    print("[da] %s LOD0 %d tris (budget %d), LOD1 %d, LOD2 %d" % (NAME, n0, BUDGET, da.lock_tris(kit, 1), da.lock_tris(kit, 2)))
