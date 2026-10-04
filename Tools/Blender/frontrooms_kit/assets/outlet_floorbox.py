"""Above-floor "tombstone" service fitting with one duplex receptacle: the
cast-aluminium hump on the office carpet where a desk used to be.

Real-world reference: Thomas & Betts SFH-40 type above-floor fitting for a
poke-through (01_period_research §6.1, S29): 2-5/8 in high x 4-3/8 in wide
x 3 in deep (67 x 111 x 76 mm). Poke-through fittings date from the 1970s
(patents 1974-79); open-plan offices of the 1980s used them under every
workstation, so the form is period-correct for 1990.

Construction (mm): a 2.0 mm aluminium flange (111 x 76, corner R 8, top
edge rounded 0.8) on the carpet; a cast housing inset 4 mm (103 x 68) whose
front view is a tombstone (flat ends, top corners R 16) and whose front and
back edges are rounded R 5; the front face carries ONE duplex receptacle
mounted sideways (the housing is too low for an upright duplex: the faces
need 66.7 mm side by side), its openings cast in the face with 0.5 mm
rounded mouths, a #6-32 oval-head screw in an 82 deg countersink between
them, and the nylon faces of §1.2 (7 mm black slots, brass contacts). The
back face is plain.

ORIGIN = THE FLOOR CONTACT CENTRE (z = 0 is the carpet; nothing below it).
FRONT = the receptacle face, kit -Y (Unity +Z): the planner's ``normal``.
Budget (10_spec §1.3): 3,200 / 1,200 / 80 tris, LOD_DISTANCES 2 / 6 / 15 m.
Slots: Prop_Aluminium (housing, flange, screw; submesh 0), Prop_NylonIvory
(device), Prop_PlasticBlack (slots), Prop_Brass (contacts). Render-only: no
collider (67 mm tall, under the Relay's 0.4 m probe and the E ray), no
shadows (R3). Anchors: cord_in (duplex centre on the face plane), face_a /
face_b (face centres, for P3 plug props). Era: no text, logos or dates; a
plain cast finish.
"""

import outlet_box_common as B

NAME = "Kit_FloorBoxTombstone"
LOD1 = None                 # under 1 m: no kitlib LODGroup until P-1 (§1.1)
LOD1_RATIO = 1200 / 3200.0
LOD2_RATIO = 80 / 3200.0
LOD_DISTANCES = (2.0, 6.0, 15.0)
BUDGET = (3200, 1200, 80)
SMOOTH_ANGLE = 35.0

W, H, D = 111.0, 67.0, 76.0  # spec envelope (mm)
INSET = 4.0                  # housing inset from the flange edge (ESTIMATE)
TOP_R = 16.0                 # tombstone top-corner radius, front view (ESTIMATE)
EDGE_R = 5.0                 # front/back edge round (ESTIMATE)
ZC = 31.0                    # duplex centre height (ESTIMATE)
FLANGE_T = 2.0


def build(kit):
    import sys
    module = sys.modules[__name__]
    B.build_tombstone(kit, module, W, D, H, INSET, TOP_R, EDGE_R, ZC, ("front",), flange_t=FLANGE_T)
    _, lo, hi = B.box_meta(kit, module, "outlet_floor", W, H, wall=False,
                           outlet_extra={"footprint": [W * B.MM, D * B.MM], "duplexCentreHeight": ZC * B.MM})
    B.assert_tombstone(kit, W, D, H, FLANGE_T, lo, hi)
