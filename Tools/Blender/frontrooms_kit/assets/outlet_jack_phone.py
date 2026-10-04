"""Flush telephone jack: one 6-position modular jack (RJ11/RJ14) on a 1-gang
ivory thermoset plate. Beside 1/2 of Office-room receptacles (0.30 m off,
same height), 25 % of title Lobby rooms, 4 % of Level 0 room receptacles
(10_spec §1.6, §2.11).

Real reference: the 1976-on FCC modular jack in a flush 1-gang plate, the
plate 2.75 x 4.50 in (69.8 x 114.3 mm), two screws on the blank-plate
spacing 3.28 in (83.3 mm); 6P plug width 9.85 mm (outlets 01 §5.1, S25).
The 1993 Sears catalogue (S26) shows "fully modular" phones, so the office
desk phone wants a modular jack, not a 4-prong.

Size 69.8 x 114.3 x 5.6 mm. ORIGIN = the wall-face point at the plate
centre; plate back on y = 0; front -Y (Unity +Z). The jack sits at the
plate centre, latch notch DOWN (the plug's latch faces the floor, as fitted).
Anchors screw_0 (top), screw_1 (bottom), plate_top.

Budget (10_spec §1.3) LOD0 2,200 / LOD1 800 / LOD2 40, 1.5 / 4 / 12 m;
LOD1/LOD2 hand-built, exported once make_lods exists (P-1/P-1b).
Slots: Prop_ThermosetIvory (plate), Prop_NylonIvory (jack insert),
Prop_PlasticBlack (cavity, bores), Prop_Brass (contact wires; real ones are
gold-flashed phosphor bronze). Render-only.

Era fit: current in 1990. No RJ45 (8P8C 10BASE-T is weeks old in 1990,
01 §5.2), no "CAT"/TIA-568 marks, no text, logos or dates.
"""

import sys

import outlet_plate_common as pc

NAME = "Kit_JackPhone"
LOD1 = None
BUDGET = (2200, 800, 40)
LOD1_RATIO = BUDGET[1] / BUDGET[0]
LOD2_RATIO = BUDGET[2] / BUDGET[0]
LOD_DISTANCES = pc.LOD_DISTANCES
SMOOTH_ANGLE = pc.SMOOTH_ANGLE


def build(kit):
    pc.begin(kit)
    W, H = pc.PLATE_1G
    ow, oh, orr = pc.PHONE_OPENING
    op = pc.Opening("rrect", 0.0, 0.0, ow, oh, orr)
    screws = [(0.0, pc.BLANK_SCREW_Z), (0.0, -pc.BLANK_SCREW_Z)]
    info = pc.thermoset_plate(kit, W, H, screws, [op], wear=pc.plate_wear(W, H, [op.box()], margin=10.0))
    jack = pc.jack_6p(kit, 0.0, 0.0, info["hf"], op)
    side = pc.check_jack(jack)
    if pc.HAS_LODS:
        pc.plate_lod1(kit, W, H, screws, [op])
        pc.plate_lod2(kit, W, H, crown_fan=True)
        pc.jack_lod1(kit, 0.0, 0.0, info["hf"], op)
        fh = info["hf"](0.0, 0.0)
        pc.dark_rect(kit, 0.0, 0.0, ow / 2 - 1.2, oh / 2 - 1.5, fh + 0.3, "jack lod2", "2")
    print("[o2] %s 6P cavity %.2f x %.2f, plug 9.85 clearance %.3f a side, floor %.2f mm in front of the wall, depth %.2f (spec 12)" % (
        kit.name, pc.JACK_CAVITY[0], pc.JACK_CAVITY[1], side, jack["floor_h"], jack["face_h"] - jack["floor_h"]))
    pc.end(kit, sys.modules[__name__], "outlet_jack", W, H, pc.PROUD_PLATE, info["seats"],
           extra={"jack": "6P", "cavityDepthMm": round(jack["face_h"] - jack["floor_h"], 2)})
