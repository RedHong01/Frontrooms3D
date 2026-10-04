"""Above-floor "tombstone" service fitting, back to back: one duplex
receptacle on each face (two desks once shared it).

Real-world reference: Thomas & Betts SFH-50 type above-floor fitting for a
poke-through (01_period_research §6.1, S29): 3 in high x 5 in wide x
3-3/8 in deep (76 x 127 x 86 mm). Same casting family and period as
Kit_FloorBoxTombstone (poke-through fittings 1970s-80s; period-correct for
a 1990 open-plan floor).

Construction (mm): 2.0 mm aluminium flange 127 x 86 (corner R 8, top edge
rounded 0.8); cast housing inset 4 mm (119 x 78), front view a tombstone
(flat ends, top corners R 18), front and back edges rounded R 5; a sideways
duplex on BOTH faces (openings cast in the face, 0.5 mm rounded mouths, a
#6-32 oval-head screw in an 82 deg countersink between the faces; §1.2
nylon faces with 7 mm black slots and brass contacts). The back face is the
front turned 180 deg about Z, so the fitting reads the same from either
desk.

DETAIL: the 4,600-tri budget cannot hold four 64-segment faces and four
64-segment openings (that build measured 6,4xx). This module therefore uses
48 segments per circle for faces, openings and countersinks and 24 per half
circle for the ground U (spec §1.1 asks 64): the chord error at 0.3 m is
0.035 mm = 0.08 px on a face, 0.036 mm on an opening (1080p, FOV 76). The
3-segment edge rounds, slot chamfers, 7 mm slots and contacts are kept.

ORIGIN = THE FLOOR CONTACT CENTRE (z = 0). FRONT = kit -Y (Unity +Z).
Budget (10_spec §1.3): 4,600 / 1,700 / 100 tris, LOD_DISTANCES 2 / 6 / 15 m.
Slots: Prop_Aluminium (submesh 0), Prop_NylonIvory, Prop_PlasticBlack,
Prop_Brass. Render-only (no collider, no shadows). Anchors: cord_in /
face_a / face_b and cord_in_back / face_a_back / face_b_back. Era: no
text, logos or dates.
"""

import outlet_box_common as B

NAME = "Kit_FloorBoxTombstone2"
LOD1 = None
LOD1_RATIO = 1700 / 4600.0
LOD2_RATIO = 100 / 4600.0
LOD_DISTANCES = (2.0, 6.0, 15.0)
BUDGET = (4600, 1700, 100)
SMOOTH_ANGLE = 35.0

W, H, D = 127.0, 76.0, 86.0  # spec envelope (mm)
INSET = 4.0                  # ESTIMATE
TOP_R = 18.0                 # ESTIMATE
EDGE_R = 5.0                 # ESTIMATE
ZC = 34.0                    # duplex centre height (ESTIMATE)
FLANGE_T = 2.0


def build(kit):
    import sys
    module = sys.modules[__name__]
    B.build_tombstone(kit, module, W, D, H, INSET, TOP_R, EDGE_R, ZC, ("front", "back"), flange_t=FLANGE_T,
                      level="hero48", pc_open=48, csk_segs=48, screw_segs=40, arc_segs=8, edge_segs=3)
    _, lo, hi = B.box_meta(kit, module, "outlet_floor", W, H, wall=False,
                           outlet_extra={"footprint": [W * B.MM, D * B.MM], "duplexCentreHeight": ZC * B.MM,
                                         "faces": 2})
    B.assert_tombstone(kit, W, D, H, FLANGE_T, lo, hi)
