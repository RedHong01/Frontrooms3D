"""Mortise-lock STRIKE: the LOCKED door's strike on the frame's latch lining
(10_spec §3.1; audit §3.7: on the Relay's final blow it detaches as a rigid,
render-only part). A 0.032 x 0.200 satin-chrome plate, 1.6 mm thick with a
0.3 mm front chamfer, two openings (latchbolt and deadbolt) with dark dust
boxes behind them, a curved lip toward the swing side that the latch's
bevel rides on every close (curl <= 1 mm), and two countersunk slotted
screws. The plate face stands 0.2 mm proud of the lining so it is never
coplanar with it (a strike mortise pocket in the lining is welcome but not
required: Kit_DoorFrame_Steel's 0.032 x 0.200 prep).

Real-world reference: SDI frame mortise strike with a box (02 §5.2),
unbranded. Opening sizes leave 1.25 mm (X) and 1.5 mm (Y) round each bolt.

Origin (PART frame, 10_spec §1.2): the plate centre on the latch lining,
door (0, 0.968, 0.998), placed with Euler(0, 180, 0) as a child of the
FRAME: front (+Z) faces the leaf edge (door -Z); part X = door -X, so the
lip (part -X) points to door +X (the swing side S); part Y = door Y.
Openings: latch at part Y -0.0315 (door 0.9365), deadbolt at +0.032 (door
1.000). The auxiliary plunger (door X +0.0105) lands on the plate between
them, outside the latch opening (as a real deadlatch does).

DUST BOX DEPTHS: latch 0.015 (spec); deadbolt 0.023 (spec 0.015 would let
the 25 mm-thrown bolt poke 4 mm through its box: the leaf edge is at door Z
0.9939, the bolt end at 1.0189, the plate back at 0.9996).
Anchors: plate (0, 0, 0); latch_opening (0, -0.0315, 0.0002);
deadbolt_opening (0, 0.032, 0.0002); lip_tip (-0.040, -0.0315, 0.001);
detach_dir (0, 0, 0.10) (the strike flies toward the leaf edge / room).
Motion: static; detaches on the Relay break ({"detach": true}).
Budget (§9.2): LOD0 900 (asserted +-15 %), LOD1 350, LOD2 60; LOD distances
1.5 / 4 / 12 m; no LOD1 export. Screws fr_lod2_drop.
Slots: Prop_Chrome (plate, first), Prop_PlasticBlack (dust boxes, slots).
"""

import interact_lock_common as lc

NAME = "Kit_Lock_StrikeMortise"
LOD1 = None
LOD1_RATIO = 0.39
LOD2_RATIO = 0.067
LOD_DISTANCES = (1.5, 4.0, 12.0)
BUDGET = 900

HALF_LEN = 0.100
LATCH_Y, BOLT_Y = -0.0315, 0.032
OPENINGS = [(LATCH_Y, 0.0075, 0.0165, 0.015), (BOLT_Y, 0.0075, 0.0165, 0.023)]
LIP_Y = (LATCH_Y - 0.016, LATCH_Y + 0.016)
SCREW_Y = (0.085, -0.085)


def build(kit):
    plate, lip, boxes, screws = lc.build_strike(kit, HALF_LEN, OPENINGS, LIP_Y, SCREW_Y, screw_segs=48,
                                                lip_steps=9, corner_k=4)
    wear = lc.strike_wear(OPENINGS)
    lc.finish_part(plate, wear)
    lc.finish_part(lip, wear)
    lc.finish_part(boxes)
    for s in screws:
        lc.finish_part(s, lambda p, n, slot: (1.0, 0.8, 1.0), lod2=True, delete_below=lc.STRIKE_PROUD)

    kit.anchor("plate", lc.U(0.0, 0.0, 0.0))
    kit.anchor("latch_opening", lc.U(0.0, LATCH_Y, lc.STRIKE_PROUD))
    kit.anchor("deadbolt_opening", lc.U(0.0, BOLT_Y, lc.STRIKE_PROUD))
    kit.anchor("lip_tip", lc.U(lc.LIP_X1, LATCH_Y, lc.STRIKE_PROUD + lc.LIP_CURL))
    kit.anchor("detach_dir", lc.U(0.0, 0.0, 0.10))
    lc.common_meta(kit, {"type": "static", "detach": True,
                         "note": "frame child; detaches as a rigid render-only part on the Relay's last blow"},
                   LOD_DISTANCES, LOD1_RATIO, LOD2_RATIO, (BUDGET, 350, 60))
    kit.meta["openings"] = [{"y": y, "halfX": hx, "halfY": hy, "boxDepth": d} for y, hx, hy, d in OPENINGS]
    kit.tag("strike", "lock_static", "frame_part")
    # Key numbers (10_spec §3.1): 0.032 x 0.200; openings at -0.0315 / +0.032;
    # curl <= 1 mm above the lining; the deadbolt's 0.025 throw stays in its box.
    assert abs(2 * HALF_LEN - 0.200) < 1e-9 and lc.STRIKE_PROUD + lc.LIP_CURL <= 0.001 + 1e-9
    door_back = 0.998 - (lc.STRIKE_PROUD - lc.STRIKE_T)          # plate back in door Z
    assert 0.9939 + 0.025 + 0.002 < door_back + OPENINGS[1][3]   # thrown deadbolt end, >= 2 mm to the box floor
    assert 0.9939 + 0.019 + 0.001 < door_back + OPENINGS[0][3]   # extended latch tip
    lc.check(kit, BUDGET, (lc.LIP_X1, -HALF_LEN, lc.STRIKE_PROUD - lc.STRIKE_T - OPENINGS[1][3]),
             (lc.STRIKE_W / 2, HALF_LEN, lc.STRIKE_PROUD + lc.LIP_CURL), tol=0.0001)
