"""Ivory thermoset duplex receptacle plate with a 5-15R device, c. 1955-1990:
the default US wall outlet of every Level 0 and Office wall.

Real-world reference: the standard 1-gang urea/phenolic duplex plate,
2.75 x 4.50 in (69.8 x 114.3 mm), 0.22 in (5.6 mm) deep, one #6-32 oval-head
screw at the centre (Leviton Q-1289, Arrow Hart J-3; outlets/01 sections
3.1-3.3), over a NEMA 5-15R duplex receptacle with the ground hole DOWN (UH
and TxState standards; the "surprised face" of 01 section 2.3). The plate
seen low on the chevron paper in the 2002 Level 0 photo DSC00161
(outlets/images/01_canon_dsc00161_outlet.jpg) is this plate.

Size: 69.8 x 114.3 mm, 6.6 mm proud (field crown 5.6 + device face 1.0).
ORIGIN = the wall-face point at the plate centre: the plate back is the
plane y = 0, the plate centre is x = z = 0 and everything is at y < 0. The
front faces -Y (Unity +Z), so placement needs only the face point and the
normal. Render-only: no collider, shadows off, drawn instanced by R3.

Budget (spec 1.3): 2,600 / 1,000 / 40 tris (LOD0 / LOD1 / LOD2), LOD
switches 1.5 / 4 / 12 m. LOD1 and LOD2 are hand-built part sets, built only
once kitlib.Kit.make_lods exists (P-1/P-1b); until then the FBX is LOD0.
Slots: Prop_ThermosetIvory (plate, submesh 0), Prop_NylonIvory (device),
Prop_PlasticBlack (slots, box void, bore), Prop_Brass (contacts).
VARIANT Kit_OutletDuplex_Brown: both ivory slots -> Prop_Ceramic (the brown
phenolic old stock, 10 % of Level 0 lines).

Era fit (lock 1990): a design unchanged since the 1950s; no TR shutters,
no USB, no screwless snap plate, no text, logo, UL mark or date.
"""

import outlet_common as oc

NAME = "Kit_OutletDuplex"
LOD1 = None
LOD1_RATIO = 0.4
LOD2_RATIO = 0.015
LOD_DISTANCES = (1.5, 4.0, 12.0)
BUDGET = (2600, 1000, 40)
SMOOTH_ANGLE = oc.SMOOTH_ANGLE
VARIANTS = {"Kit_OutletDuplex_Brown": {oc.TI: oc.CE, oc.NI: oc.CE}}


def build(kit):
    oc.build_duplex(kit, __import__(__name__), kind="thermoset")
