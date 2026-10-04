"""Cracked ivory thermoset duplex plate (wear mesh variant, 5 % of Level 0
and 3 % of Office plates, thermoset only).

Real-world reference: Kit_OutletDuplex with the two classic thermoset
failures: thermoset is "quite brittle" (Pass & Seymour patent, outlets/01
section 8, S22), so an over-tightened centre screw splits the plate from
the screw hole through the bridge between the openings to the side edge,
and a knock chips a corner.
* The split: a 0.25 mm V groove from the countersink to the right edge,
  the lower half stepped 0.2 mm down at the lip (blended out over 6 mm and
  tapered in over the first 5 mm, where the screw still clamps it). The
  groove floor stays inside the plate, so no gap opens to the wall
  (self-check d). It enters the countersink lip and vanishes under the
  screw head.
* The chip: the lower-left corner, about 5.4 x 4.2 mm (deeper than 0.25 mm,
  measured from the square corner), three bisected fracture
  facets plus a flat floor facet (so the hollow back never shows through).

Size: 69.8 x 114.3 mm, 6.6 mm proud. ORIGIN = the wall-face point at the
plate centre (plate back on y = 0, centre x = z = 0, all at y < 0); front
-Y (Unity +Z). Render-only.

Budget (spec 1.3): 3,100 / 1,100 / 40 tris; LOD 1.5 / 4 / 12 m. Slots:
Prop_ThermosetIvory, Prop_NylonIvory, Prop_PlasticBlack, Prop_Brass.
fr_wear: B 0.4 in the split (dirt), G 0.6 on the fracture faces.

Era fit: as Kit_OutletDuplex.
"""

import outlet_common as oc

NAME = "Kit_OutletDuplex_Cracked"
LOD1 = None
LOD1_RATIO = 0.35
LOD2_RATIO = 0.013
LOD_DISTANCES = (1.5, 4.0, 12.0)
BUDGET = (3100, 1100, 40)
SMOOTH_ANGLE = oc.SMOOTH_ANGLE

# The split's centre line (plate frame, mm): from the Ø7.4 countersink ring
# to the field edge (x = 30.9), then straight through the edge profile at
# z = 1.4. A loose zig-zag, as thermoset fractures run.
CRACK_PATH = ((3.7, 0.0), (6.8, 0.55), (9.9, -0.2), (13.4, 0.75), (16.9, 0.35),
              (20.6, 1.25), (24.2, 0.8), (27.8, 1.4), (30.9, 1.4))
CRACK_Z_EXIT = 1.4


def build(kit):
    crack = oc.Crack(CRACK_PATH, CRACK_Z_EXIT)
    chip = oc.Chip((-1, -1), oc.CHIP_PLANES)
    oc.build_duplex(kit, __import__(__name__), kind="thermoset", crack=crack, chip=chip)
    kit.meta["outlet"]["wear"] = {"crack": {"width": oc.Crack.WIDTH * oc.MM, "step": oc.Crack.STEP * oc.MM,
                                            "from": "countersink", "to": "right edge"},
                                  "chip": {"corner": "lower left", "extentMm": list(chip.extent),
                                           "clampedVerts": chip.clamped}}
    u, w, d = chip.extent
    assert 4.5 <= u <= 6.0 and 3.5 <= w <= 4.8, "chip extent %.2f x %.2f (want about 5 x 4)" % (u, w)
