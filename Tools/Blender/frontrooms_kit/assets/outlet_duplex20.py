"""Ivory thermoset duplex plate with a NEMA 5-20R (20 A, T-slot) device:
the spec-grade commercial receptacle of a 1990 office on 20 A circuits.

Real-world reference: as Kit_OutletDuplex (69.8 x 114.3 mm plate, one
#6-32 oval-head screw), with the 5-20R face: "The 5-20R receptacle has a
T-shaped neutral hole, to accept both 5-15P and 5-20P plugs" (outlets/01
section 2.1, S09). The neutral slot gets a horizontal arm 6.4 x 2.0 mm at
its centre, pointing away from the hot slot (spec 1.2; arm size ESTIMATE,
to check against a period photo). Ground hole down.

Size: 69.8 x 114.3 mm, 6.6 mm proud. ORIGIN = the wall-face point at the
plate centre (plate back on y = 0, centre x = z = 0, all at y < 0); front
-Y (Unity +Z). Render-only.

Budget (spec 1.3): 2,800 / 1,050 / 40 tris; LOD 1.5 / 4 / 12 m.
Slots: Prop_ThermosetIvory, Prop_NylonIvory, Prop_PlasticBlack, Prop_Brass.
VARIANT Kit_OutletDuplex20_IG: the device in Prop_PlasticOrange, the
all-orange isolated-ground receptacle of 1990 office desks (01 section 2.4:
before the 1996 NEC the orange body was the identification; all-orange,
never an ivory device with only a triangle).

Era fit: 5-20R T-slots were current in 1990 commercial work; no TR
shutters, USB, text, logo or date.
"""

import outlet_common as oc

NAME = "Kit_OutletDuplex20"
LOD1 = None
LOD1_RATIO = 0.38
LOD2_RATIO = 0.014
LOD_DISTANCES = (1.5, 4.0, 12.0)
BUDGET = (2800, 1050, 40)
SMOOTH_ANGLE = oc.SMOOTH_ANGLE
VARIANTS = {"Kit_OutletDuplex20_IG": {oc.NI: oc.OR}}


def build(kit):
    oc.build_duplex(kit, __import__(__name__), kind="thermoset", t20=True)
