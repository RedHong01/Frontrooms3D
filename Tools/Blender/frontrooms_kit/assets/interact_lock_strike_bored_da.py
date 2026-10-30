"""DOUBLE-ACTING strike for the bored DA latch: the FREE door's strike on the
frame's latch lining (D1.6, A12 / A19; 10_design §2.1). A flat 0.032 x 0.124
plate, 1.6 mm thick with a 0.3 mm front chamfer, 0.2 mm proud of the lining,
one opening (X +-0.0065, Y +-0.0115) with a dark dust box, two countersunk
slotted screws, and a short eased lip on BOTH long edges round the opening
(to |X| 0.030, curl 0.8 mm), so the double-bevel latch rides in from either
face. Satin chrome; VARIANT _Brass (Lobby).

Origin (PART frame): the plate centre on the latch lining, door
(0, 1.000, 0.998), placed with Euler(0, 180, 0) as a child of the FRAME;
symmetric in X. Anchors: plate (0, 0, 0); latch_opening (0, 0, 0.0002);
lip_tip_a/b (+-0.030, 0, 0.001); detach_dir (0, 0, 0.10). Motion: static;
detaches on the Relay break. LODs (hand-built): 700 / ~280 / ~50;
distances 1.5 / 4 / 12 m. Slots Prop_Chrome, Prop_PlasticBlack.
"""

import interact_door_da_common as da
import interact_lock_common as lc

NAME = "Kit_Lock_StrikeBored_DA"
LOD1_RATIO = 0.40
LOD2_RATIO = 0.07
LOD_DISTANCES = (1.5, 4.0, 12.0)
BUDGET = 700
VARIANTS = {"Kit_Lock_StrikeBored_Brass_DA": {lc.CHROME: lc.BRASS}}

HALF_LEN = 0.062
OPENINGS = [(0.0, 0.0065, 0.0115, 0.015)]
LIP_Y = (-0.016, 0.016)
SCREW_Y = (0.047, -0.047)


def build(kit):
    wear = da.strike_wear_da(OPENINGS)
    for level in (0, 1, 2):
        parts, screws = da.build_strike_da(kit, HALF_LEN, OPENINGS, LIP_Y, SCREW_Y, level, screw_segs=24, corner_k=2, box_k=1)
        for p in parts:
            lc.finish_part(p, wear if "box" not in p.name else None)
        for s in screws:
            lc.finish_part(s, lambda p, n, slot: (1.0, 0.8, 1.0), delete_below=lc.STRIKE_PROUD - 0.00028)
    kit.anchor("plate", lc.U(0.0, 0.0, 0.0))
    kit.anchor("latch_opening", lc.U(0.0, 0.0, lc.STRIKE_PROUD))
    kit.anchor("lip_tip_a", lc.U(da.STRIKE_LIP_OUT, 0.0, lc.STRIKE_PROUD + da.STRIKE_LIP_CURL))
    kit.anchor("lip_tip_b", lc.U(-da.STRIKE_LIP_OUT, 0.0, lc.STRIKE_PROUD + da.STRIKE_LIP_CURL))
    kit.anchor("detach_dir", lc.U(0.0, 0.0, 0.10))
    lc.common_meta(kit, {"type": "static", "detach": True,
                         "note": "frame child; detaches as a rigid render-only part on the Relay's last blow"},
                   LOD_DISTANCES, LOD1_RATIO, LOD2_RATIO, (BUDGET, 280, 50))
    kit.meta["openings"] = [{"y": y, "halfX": hx, "halfY": hy, "boxDepth": d} for y, hx, hy, d in OPENINGS]
    kit.meta["acting"] = "double"
    kit.tag("strike", "lock_static", "frame_part", "double_acting")
    door_back = 0.998 - (lc.STRIKE_PROUD - lc.STRIKE_T)
    assert 0.995 + 0.013 + 0.0015 < door_back + OPENINGS[0][3], "the thrown DA latch stays in its box"
    assert lc.STRIKE_PROUD + da.STRIKE_LIP_CURL <= 0.001 + 1e-9
    n0 = da.lock_tris(kit, 0)
    print("[da] %s LOD0 %d tris (budget %d), LOD1 %d, LOD2 %d" % (NAME, n0, BUDGET, da.lock_tris(kit, 1), da.lock_tris(kit, 2)))
