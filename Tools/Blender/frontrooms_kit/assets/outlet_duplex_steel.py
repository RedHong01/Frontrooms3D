"""Stainless-steel duplex receptacle plate with a 5-15R device: the public
lobby, corridor and restroom plate of a 1980s-90s commercial building.

Real-world reference: Type 302/430 satin stainless, 0.030 in (0.76 mm)
sheet, standard 1-gang 2.75 x 4.50 in (69.8 x 114.3 mm), 0.19 in (4.7 mm)
deep over the formed edge (Leviton Q-1289, Arrow Hart J-3; UH spec "for
elevator lobbies, entrance lobbies, restrooms, and public areas"; outlets/01
sections 3.1 and 3.4). A flat field, a quarter-round formed return
(r 1.5) and a straight leg to the wall; a pressed countersink for the
#6-32 oval-head screw. The same 5-15R device as Kit_OutletDuplex sits 0.9
lower because the plate is thinner (spec 1.3).

Size: 69.8 x 114.3 mm, 5.7 mm proud (field 4.7 + device face 1.0).
ORIGIN = the wall-face point at the plate centre: plate back on y = 0,
centre at x = z = 0, everything at y < 0; front -Y (Unity +Z).
Render-only (no collider, shadows off, drawn instanced by R3).

Budget (spec 1.3): 2,300 / 950 / 40 tris; LOD switches 1.5 / 4 / 12 m;
LOD1/LOD2 hand-built, exported once P-1/P-1b land. Slots: Prop_Aluminium
(plate, submesh 0; brushed metal), Prop_NylonIvory (device),
Prop_PlasticBlack, Prop_Brass. Self-check (e): the formed edge reads as one
crisp bright line under 45 deg light.

Era fit: timeless commercial stainless; no text, logo or date stamped in
the sheet; no screwless snap plate.
"""

import outlet_common as oc

NAME = "Kit_OutletDuplex_Steel"
LOD1 = None
LOD1_RATIO = 0.4
LOD2_RATIO = 0.017
LOD_DISTANCES = (1.5, 4.0, 12.0)
BUDGET = (2300, 950, 40)
SMOOTH_ANGLE = oc.SMOOTH_ANGLE


def build(kit):
    oc.build_duplex(kit, __import__(__name__), kind="steel")
