"""Cylindrical (bored) lockset DOUBLE-BEVEL LATCHBOLT for the double-acting
FREE door (D1.6, A12 / A19; 10_design §2.1, D6): a 0.010 x 0.020 latch with a
1/2" (0.013) throw and a rounded 30 deg bevel on BOTH faces, so the leaf
latches when it is shut from either face (a single-bevel latch only rides
the strike from one side). No deadlatch plunger (a plunger holds a
single-acting latch against a stop; a DA leaf has none).

Real-world reference: the ANSI cylindrical latch with its bevel reversed per
handing; a both-ways latch is a game liberty (A12: handles kept), modelled
on the double-bevel "roller/bevel" latches of 1980s double-acting service
doors (ESTIMATE). No maker marks.

Origin (PART frame, 10_spec §1.2): on the faceplate surface, on the latch
axis: door (0, 1.000, 0.995). Front = Unity +Z = the throw (door +Z).
Placed with identity rotation under the Leaf rig. AUTHORED EXTENDED.
Motion: slide along part -Z by 0.013 with the lever's 35 deg (0.08 s),
spring back. Anchors: bolt_axis (0, 0, 0); tip (0, 0, 0.013); throw_dir
(0, 0, 0.10). LODs (hand-built): 520 / ~210 / ~40 tris; distances 1 / 3 / 8 m.
Slot Prop_Chrome; VARIANT _Brass (Lobby).
"""

import interact_door_da_common as da
import interact_lock_common as lc

NAME = "Kit_Lock_Latchbolt_Bored_DA"
LOD1_RATIO = 0.40
LOD2_RATIO = 0.08
LOD_DISTANCES = (1.0, 3.0, 8.0)
BUDGET = 520
VARIANTS = {"Kit_Lock_Latchbolt_Bored_Brass_DA": {lc.CHROME: lc.BRASS}}

T, H = 0.010, 0.020
THROW, BACK = 0.013, -0.017


def _wear(p, n, slot):
    x, y, z = p
    g = 0.70 if z > 0.004 and abs(n[0]) > 0.2 else 1.0       # both bevels rubbed by the strike lips
    return (1.0, g, 1.0)


def build(kit):
    zb = None
    for level in (0, 1, 2):
        obj, zb_l = da.build_latch_da(kit, T, H, THROW, BACK, lc.CHROME, level, arc_n=12, round_r=0.0004, round_k=3)
        lc.finish_part(obj, _wear)
        if level == 0:
            zb = zb_l
    kit.anchor("bolt_axis", lc.U(0.0, 0.0, 0.0))
    kit.anchor("tip", lc.U(0.0, 0.0, THROW))
    kit.anchor("throw_dir", lc.U(0.0, 0.0, 0.10))
    lc.common_meta(kit, {"type": "slide", "axis": [0, 0, -1], "travel": THROW, "rest": "extended",
                         "note": "double-bevel spring latch (DA); the lever's 35 deg retracts it 0.013"},
                   LOD_DISTANCES, LOD1_RATIO, LOD2_RATIO, (BUDGET, 210, 40))
    kit.meta["frontOpening"] = {"x": [-0.0055, 0.0055], "yHalf": 0.0105}
    kit.meta["acting"] = "double"
    kit.tag("latchbolt", "lock_moving", "bolt", "bored", "double_acting")
    assert zb > 0.0, "each bevel spans half the bolt: it starts outside the front"
    n0 = da.lock_tris(kit, 0)
    print("[da] %s LOD0 %d tris (budget %d), LOD1 %d, LOD2 %d" % (NAME, n0, BUDGET, da.lock_tris(kit, 1), da.lock_tris(kit, 2)))
