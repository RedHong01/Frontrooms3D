"""DOUBLE-ACTING mortise-lock strike: the LOCKED DA door's strike on the
frame's latch lining (D1.6, A12 / A19; 10_design §2.1). A 0.032 x 0.200
satin-chrome plate, 1.6 mm thick, two openings (latch at part Y -0.0315,
deadbolt at +0.032) with dark dust boxes (latch 0.017, deadbolt 0.023 deep,
G2 §4.4), two countersunk screws, and an eased lip on BOTH long edges round
the latch opening (to |X| 0.030, curl 0.8 mm).

Origin (PART frame): plate centre on the latch lining, door (0, 0.968,
0.998), placed with Euler(0, 180, 0) as a child of the FRAME; symmetric in
X. Anchors: plate; latch_opening (0, -0.0315, 0.0002); deadbolt_opening
(0, 0.032, 0.0002); lip_tip_a/b (+-0.030, -0.0315, 0.001); detach_dir.
Static; detaches on the Relay break. LODs (hand-built): 700 / ~280 / ~50;
distances 1.5 / 4 / 12 m. Slots Prop_Chrome, Prop_PlasticBlack.
"""

import interact_door_da_common as da
import interact_lock_common as lc

NAME = "Kit_Lock_StrikeMortise_DA"
LOD1_RATIO = 0.40
LOD2_RATIO = 0.07
LOD_DISTANCES = (1.5, 4.0, 12.0)
BUDGET = 700

HALF_LEN = 0.100
LATCH_Y, BOLT_Y = -0.0315, 0.032
OPENINGS = [(LATCH_Y, 0.0075, 0.0165, 0.017), (BOLT_Y, 0.0075, 0.0165, 0.023)]   # latch box 0.017: the DA latch front is 1.1 mm further out (Z 0.995)
LIP_Y = (LATCH_Y - 0.016, LATCH_Y + 0.016)
SCREW_Y = (0.085, -0.085)


def build(kit):
    wear = da.strike_wear_da(OPENINGS)
    for level in (0, 1, 2):
        parts, screws = da.build_strike_da(kit, HALF_LEN, OPENINGS, LIP_Y, SCREW_Y, level, screw_segs=24, corner_k=2, box_k=1)
        for p in parts:
            lc.finish_part(p, wear if "box" not in p.name else None)
        for s in screws:
            lc.finish_part(s, lambda p, n, slot: (1.0, 0.8, 1.0), delete_below=lc.STRIKE_PROUD - 0.00028)
    kit.anchor("plate", lc.U(0.0, 0.0, 0.0))
    kit.anchor("latch_opening", lc.U(0.0, LATCH_Y, lc.STRIKE_PROUD))
    kit.anchor("deadbolt_opening", lc.U(0.0, BOLT_Y, lc.STRIKE_PROUD))
    kit.anchor("lip_tip_a", lc.U(da.STRIKE_LIP_OUT, LATCH_Y, lc.STRIKE_PROUD + da.STRIKE_LIP_CURL))
    kit.anchor("lip_tip_b", lc.U(-da.STRIKE_LIP_OUT, LATCH_Y, lc.STRIKE_PROUD + da.STRIKE_LIP_CURL))
    kit.anchor("detach_dir", lc.U(0.0, 0.0, 0.10))
    lc.common_meta(kit, {"type": "static", "detach": True,
                         "note": "frame child; detaches as a rigid render-only part on the Relay's last blow"},
                   LOD_DISTANCES, LOD1_RATIO, LOD2_RATIO, (BUDGET, 280, 50))
    kit.meta["openings"] = [{"y": y, "halfX": hx, "halfY": hy, "boxDepth": d} for y, hx, hy, d in OPENINGS]
    kit.meta["acting"] = "double"
    kit.tag("strike", "lock_static", "frame_part", "double_acting")
    door_back = 0.998 - (lc.STRIKE_PROUD - lc.STRIKE_T)
    assert 0.995 + 0.025 + 0.002 < door_back + OPENINGS[1][3], "thrown deadbolt end, >= 2 mm to the box floor"
    assert 0.995 + 0.019 + 0.0005 < door_back + OPENINGS[0][3], "extended DA latch tip"
    n0 = da.lock_tris(kit, 0)
    print("[da] %s LOD0 %d tris (budget %d), LOD1 %d, LOD2 %d" % (NAME, n0, BUDGET, da.lock_tris(kit, 1), da.lock_tris(kit, 2)))
