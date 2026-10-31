"""Band (clevis-type) pipe hanger for the 4 in sprinkler main, on 3/8 in threaded rod.
PRODUCTION kit for Run! B (Kit_PipeHanger_Band), hand-built LOD0/1 (no LOD2: culls at 20 m).

A 25 mm galvanized strap band round the pipe, its ends pinched into a swivel nut at
the top, the rod rising 0.30 m to the soffit (the rod's top dies in the deck).
ORIGIN = the pipe axis; the band lies in the YZ plane (the pipe runs along X, as the
main tile). Budget 300 / 100 / 0; lodDistances 2 / 6 / 20. The band is 24-sided at LOD0
(0.45 mm chord): the 300-tri budget rules here; it hangs at 2.6 m, never at 0.3 m.
Slots: RunB_GalvSteel.
"""

import _run_b_common as B

NAME = "Kit_PipeHanger_Band"
SMOOTH_ANGLE = 40.0
BUDGET = (300, 100, 0)

GALV = "RunB_GalvSteel"
RI, RO, HW = 0.0505, 0.0525, 0.0125


def _level(kit, lv):
    n = 24 if lv == 0 else 10
    prof = [(RI, -HW), (RO, -HW), (RO, HW), (RI, HW), (RI, -HW)]
    if lv == 1:
        prof = [(RO, -HW), (RO, HW)]
    kit.lathe(prof, (0, 0, 0), GALV, verts=n, rot=(0, 90, 0), name="band", close_top=False, close_bottom=False)
    # Pinch and swivel nut above the pipe, then the rod.
    kit.box((0.025, 0.016, 0.022), (0, 0, RO + 0.011), GALV, bevel=0.0012 if lv == 0 else 0.0, segments=1 if lv == 0 else 0, name="pinch")
    kit.cylinder(0.0095, 0.012, (0, 0, RO + 0.028), GALV, verts=6, bevel=0.0, segments=0, name="swivel nut")
    kit.cylinder(0.0048, 0.30, (0, 0, RO + 0.034 + 0.15), GALV, verts=8 if lv == 0 else 6, bevel=0.0, segments=0, name="rod")


def build(kit):
    B.levels(kit, _level, which=(0, 1))
    kit.no_collider()
    kit.anchor("rod_top", (0, 0, RO + 0.034 + 0.30))
    kit.tag("ceiling", "run", "run_b", "sprinkler")
    B.lod_meta(kit, (2.0, 6.0, 20.0), BUDGET)
