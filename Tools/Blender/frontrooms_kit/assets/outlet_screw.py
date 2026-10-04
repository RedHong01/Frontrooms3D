"""#6-32 slotted oval-head plate screw (the one screw of a duplex plate).

Real-world reference: Pass & Seymour 510 / Leviton plate screws, #6-32
oval head, 1/2 in long, "countersunk for oval head #6-32 screws" (Arrow Hart
J-2; outlets/01 section 3.3). Head about 0.26 in (6.6 mm) across with a low
oval crown about 1 mm proud and one straight slot. The colour matches the
plate: R3 re-colours this kit to the plate's finish (ivory, almond, white,
brown, grey, stainless).

Size: 6.6 mm diameter, 1.0 mm crown (spec 1.2). Slot 0.8 wide x 0.6 deep
from the crown top, flat-bottomed, so it runs out through the dome where
the dome is lower than the slot floor (r > 2.6 mm), as a saw cut does.
ORIGIN = the seat centre (the head rim on the countersink, level with the
plate field); the crown faces -Y (Unity +Z). Place it at a plate's screw_N
anchor with identity rotation, then roll it about the plate normal by the
plate's slot angle (uniform 0-180 deg). At roll 0 the slot is horizontal.
The underside is not modelled: it sits in the plate's countersink.

Budget 260 tris, LOD0 only, drawn only while the plate draws LOD0 (spec
D6; until P-1 that is to 12 m, and the screw must be drawn with it or the
plate's dark bore cap reads as an empty hole). Slot: Prop_ThermosetIvory.

Era fit: a slotted screw (no Phillips on plates in 1990 practice), no
markings. Missing screw = no instance (spec 1.4).
"""

import outlet_common as oc

NAME = "Kit_OutletScrew"
LOD1 = None
LOD1_RATIO = 0.0
LOD2_RATIO = 0.0
LOD_DISTANCES = (1.5, 1.5, 1.5)
BUDGET = (260,)
SMOOTH_ANGLE = oc.SMOOTH_ANGLE

CROWN = oc.SCREW_CROWN


def build(kit):
    oc.register_slots()
    oc.assert_shared_numbers()
    m, dome = oc.screw_head()
    hz = oc.SCREW_SLOT[0] / 2

    def wear(p, n):
        x, h, z = p
        in_slot = abs(z) <= hz + 1e-3 and h < dome(x, z) - 0.02
        return (1.0, 1.0, 0.4 if in_slot else 1.0)

    obj = m.to_object(kit, "screw head", oc.TI, wear=wear, lods="0")
    tris = oc.part_tris(obj)
    oc.tri_check(NAME, tris, BUDGET[0])
    # the slot removes the apex: the highest point is the slot lip
    assert abs(m.hmax() - dome(0.0, hz)) < 0.01 and abs(dome(0.0, 0.0) - CROWN) < 1e-9, "crown %.3f" % m.hmax()
    assert m.hmin() >= -1e-9
    kit.no_collider()
    kit.tag("outlet", "outlet_screw", "wall_flush")
    kit.anchor("seat", (0.0, 0.0, 0.0))
    kit.meta["lodDistances"] = list(LOD_DISTANCES)
    kit.meta["lodNote"] = "LOD0 only; draw while the plate draws LOD0 (spec D6), at the plate's screw_N anchors."
    kit.meta["outlet"] = {"headD": oc.SCREW_D * oc.MM, "crown": CROWN * oc.MM, "slot": [oc.SCREW_SLOT[0] * oc.MM, oc.SCREW_SLOT[1] * oc.MM],
                          "slotAxisAtRoll0": "x (horizontal)", "recolour": "plate finish"}
    kit.meta["trianglesByLod"] = [tris]
