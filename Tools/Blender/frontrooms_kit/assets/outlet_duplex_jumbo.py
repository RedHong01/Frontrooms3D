"""Jumbo (oversize) ivory thermoset duplex plate over a bad cut-out (wear
mesh variant, 5 % of Level 0 and 3 % of Office plates).

Real-world reference: the 1-gang "oversize" plate, 3.50 x 5.25 in
(88.9 x 133.4 mm), 0.26 in (6.5 mm) deep; Leviton: "used to conceal greater
wall irregularities"; the UH spec bans them ("Jumbo plates are not
acceptable"), which shows they were used (outlets/01 sections 3.1, 8). The
section 1.2 thermoset profile is scaled by 6.5 / 5.6 in inset and height
(O1 ESTIMATE), corner radius 2.0 kept, same openings, screw and device.

Size: 88.9 x 133.4 mm, 7.5 mm proud (field crown 6.5 + device face 1.0;
the screw crown also reaches 7.5 = the plate proud limit). ORIGIN = the
wall-face point at the plate centre (plate back on y = 0, centre
x = z = 0, all at y < 0); front -Y (Unity +Z). Render-only.

Budget (spec 1.3): 2,700 / 1,000 / 40 tris; LOD 1.5 / 4 / 12 m. Slots:
Prop_ThermosetIvory, Prop_NylonIvory, Prop_PlasticBlack, Prop_Brass.

Era fit: oversize plates were sold for decades before 1990; no text,
logo or date.
"""

import outlet_common as oc

NAME = "Kit_OutletDuplex_Jumbo"
LOD1 = None
LOD1_RATIO = 0.37
LOD2_RATIO = 0.015
LOD_DISTANCES = (1.5, 4.0, 12.0)
BUDGET = (2700, 1000, 40)
SMOOTH_ANGLE = oc.SMOOTH_ANGLE


def build(kit):
    oc.build_duplex(kit, __import__(__name__), kind="jumbo")
