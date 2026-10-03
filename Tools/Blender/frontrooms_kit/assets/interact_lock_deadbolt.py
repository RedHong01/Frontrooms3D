"""Mortise-lock DEADBOLT: the bolt the key throws and retracts (10_spec §3.1,
§3.4 0.86 s "BoltJolt"; Red's "a deadbolt that slides"). A satin-chrome
square-ended bolt 0.0125 x 0.030 with a 1 mm chamfer round the end face,
1 mm radii on its long edges and two hardened-steel anti-saw insert dots on
the end face. The modelled length (0.045) runs back into the leaf, so the
bolt never shows its back end at any point of its 0.025 travel.

Real-world reference: 1" (25 mm) throw commercial mortise deadbolt with
hardened inserts (02 §4.2: 1" throw). Section dimensions are ESTIMATES
(§13).

Origin (PART frame, 10_spec §1.2): on the lock front surface (the leaf's
mortise armor front) on the bolt axis: door (0, 1.000, 0.9939). Front =
Unity +Z (kit -Y) = the THROW direction (door +Z, into the strike). Placed
with identity rotation as a child of the Leaf rig (§6.2), so it follows the
rig's handing mirror.

AUTHORED THROWN (the locked door's rest state): the end face is at Z +0.025.
Motion: slide along part -Z by 0.025 to retract (end face flush with the
front at 0); it stays retracted once DoorUnlocked fires.
INTERFACE NOTE for G1 (the armor front): the deadbolt opening must clear
X -0.0070 .. +0.0070, Y 0.9835 .. 1.0165 (door root).
Anchors: bolt_axis (0, 0, 0); end_face (0, 0, 0.025); throw_dir (0, 0, 0.10).
Budget (§9.2): LOD0 400 (asserted +-15 %), LOD1 150, LOD2 30; LOD distances
1.0 / 3 / 8 m; no LOD1 export (part < 1 m). The insert dots are
fr_lod2_drop. Slots: Prop_Chrome (bolt, first), Prop_Aluminium (the duller
hardened-steel inserts).
"""

import interact_lock_common as lc

NAME = "Kit_Lock_Deadbolt"
LOD1 = None
LOD1_RATIO = 0.375
LOD2_RATIO = 0.075
LOD_DISTANCES = (1.0, 3.0, 8.0)
BUDGET = 400

BX, BY = 0.0125, 0.030
THROW = 0.025
BACK = -0.020
CHAMFER = 0.0010
EDGE_R = 0.0011
DOT_R, DOT_Y = 0.0015, 0.0075


def _wear(p, n, slot):
    x, y, z = p
    g = 0.72 if z > THROW - CHAMFER - 0.0001 and abs(n[2]) < 0.99 else 1.0   # the end chamfer
    r = 0.9 if z > 0.0 else 1.0
    return (r, g, 1.0)


def build(kit):
    m = lc.Mesh()
    lc.plate_sweep(m, 0.0, 0.0, BX / 2, BY / 2, EDGE_R, 3,
                   [(0.0, BACK), (0.0, THROW - CHAMFER), (CHAMFER, THROW)])
    bolt = m.to_object(kit, "deadbolt", lc.CHROME)
    lc.finish_part(bolt, _wear)
    d = lc.Mesh()
    for sy in (DOT_Y, -DOT_Y):
        d.lathe([(0.0, THROW - 0.0004), (DOT_R, THROW - 0.0004), (DOT_R, THROW + 0.00004),
                 (DOT_R - 0.00012, THROW + 0.0001), (0.0, THROW + 0.0001)], 24, (0.0, sy))
    dots = d.to_object(kit, "hardened inserts", lc.ALU)
    lc.finish_part(dots, lambda p, n, s: (1.0, 0.8, 1.0), lod2=True)

    kit.anchor("bolt_axis", lc.U(0.0, 0.0, 0.0))
    kit.anchor("end_face", lc.U(0.0, 0.0, THROW))
    kit.anchor("throw_dir", lc.U(0.0, 0.0, 0.10))
    lc.common_meta(kit, {"type": "slide", "axis": [0, 0, -1], "travel": THROW, "rest": "thrown",
                         "note": "authored thrown; slide -Z 0.025 to retract (0.08 s at the 0.86 commit)"},
                   LOD_DISTANCES, LOD1_RATIO, LOD2_RATIO, (BUDGET, 150, 30))
    kit.meta["frontOpening"] = {"x": [-0.0070, 0.0070], "yHalf": 0.0165}
    kit.tag("deadbolt", "lock_moving", "bolt")
    assert abs(THROW - 0.025) < 1e-9 and abs(THROW - BACK - 0.045) < 1e-9
    lc.check(kit, BUDGET, (-BX / 2, -BY / 2, BACK), (BX / 2, BY / 2, THROW + 0.0001), tol=0.00005)
