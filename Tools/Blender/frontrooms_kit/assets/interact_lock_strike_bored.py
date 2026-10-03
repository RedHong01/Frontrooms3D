"""ANSI curved-lip STRIKE for the cylindrical (bored) latch: the FREE door's
strike on the frame's latch lining (10_spec §3.1; 02 §4.1 / §5.2: 1-1/4" x
4-7/8", lip 13/16" to centre). A 0.032 x 0.124 plate, 1.6 mm thick with a
0.3 mm front chamfer, one opening with a dark dust box, the curved lip
toward the swing side (curl <= 1 mm) and two countersunk slotted screws.
Face 0.2 mm proud of the lining (never coplanar with it). Satin chrome;
bright brass VARIANT for the Lobby's free doors.

Origin (PART frame, 10_spec §1.2): the plate centre on the latch lining,
door (0, 1.000, 0.998), placed with Euler(0, 180, 0) as a child of the
FRAME: front (+Z) faces the leaf edge; part X = door -X, so the lip (part
-X) points to door +X (the swing side S). Opening X +-0.0065, Y +-0.0115
round the 0.010 x 0.020 latch; the bored plunger (door X +0.009) lands on
the plate beside the opening.
Anchors: plate (0, 0, 0); latch_opening (0, 0, 0.0002); lip_tip
(-0.040, 0, 0.001); detach_dir (0, 0, 0.10).
Motion: static; detaches on the Relay break ({"detach": true}).
Budget (§9.2): LOD0 600 (asserted +-15 %), LOD1 250, LOD2 40; LOD distances
1.5 / 4 / 12 m; no LOD1 export. Screws are 32-segment (Ø 8.5 mm = 25 px at
0.3 m) to hold the 600 budget; fr_lod2_drop.
Slots: Prop_Chrome (plate, first), Prop_PlasticBlack (dust box, slots).
VARIANT _Brass.
"""

import interact_lock_common as lc

NAME = "Kit_Lock_StrikeBored"
LOD1 = None
LOD1_RATIO = 0.417
LOD2_RATIO = 0.067
LOD_DISTANCES = (1.5, 4.0, 12.0)
BUDGET = 600
VARIANTS = {"Kit_Lock_StrikeBored_Brass": {lc.CHROME: lc.BRASS}}

HALF_LEN = 0.062
OPENINGS = [(0.0, 0.0065, 0.0115, 0.015)]
LIP_Y = (-0.016, 0.016)
SCREW_Y = (0.047, -0.047)


def build(kit):
    plate, lip, boxes, screws = lc.build_strike(kit, HALF_LEN, OPENINGS, LIP_Y, SCREW_Y, screw_segs=24,
                                                corner_k=2, box_k=1,
                                                lip_xs=(-0.0157, -0.024, -0.030, -0.034, -0.037, -0.0392, -0.040))
    wear = lc.strike_wear(OPENINGS)
    lc.finish_part(plate, wear)
    lc.finish_part(lip, wear)
    lc.finish_part(boxes)
    for s in screws:
        lc.finish_part(s, lambda p, n, slot: (1.0, 0.8, 1.0), lod2=True, delete_below=lc.STRIKE_PROUD - 0.00028)

    kit.anchor("plate", lc.U(0.0, 0.0, 0.0))
    kit.anchor("latch_opening", lc.U(0.0, 0.0, lc.STRIKE_PROUD))
    kit.anchor("lip_tip", lc.U(lc.LIP_X1, 0.0, lc.STRIKE_PROUD + lc.LIP_CURL))
    kit.anchor("detach_dir", lc.U(0.0, 0.0, 0.10))
    lc.common_meta(kit, {"type": "static", "detach": True,
                         "note": "frame child; detaches as a rigid render-only part on the Relay's last blow"},
                   LOD_DISTANCES, LOD1_RATIO, LOD2_RATIO, (BUDGET, 250, 40))
    kit.meta["openings"] = [{"y": y, "halfX": hx, "halfY": hy, "boxDepth": d} for y, hx, hy, d in OPENINGS]
    kit.tag("strike", "lock_static", "frame_part")
    door_back = 0.998 - (lc.STRIKE_PROUD - lc.STRIKE_T)
    assert abs(2 * HALF_LEN - 0.124) < 1e-9 and lc.STRIKE_PROUD + lc.LIP_CURL <= 0.001 + 1e-9
    assert 0.9939 + 0.013 + 0.002 < door_back + OPENINGS[0][3]
    lc.check(kit, BUDGET, (lc.LIP_X1, -HALF_LEN, lc.STRIKE_PROUD - lc.STRIKE_T - OPENINGS[0][3]),
             (lc.STRIKE_W / 2, HALF_LEN, lc.STRIKE_PROUD + lc.LIP_CURL), tol=0.0001)
