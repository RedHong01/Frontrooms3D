"""Blank (box-mount) wall plate, ivory thermoset: the plate over an unused
outlet box. A 1990 building has them where a receptacle was moved or never
fitted; FrontRooms draws one in place of 3 % (Level 0) / 4 % (Office) of
receptacles (10_spec §1.4).

Real reference: standard 1-gang thermoset blank, 2.75 x 4.50 in (69.8 x
114.3 mm), 0.22 in (5.6 mm) deep, two #6-32 oval-head screws 3.28 in (83.3
mm) apart into the box ears (Leviton Q-1289, Arrow Hart J-2/J-3; outlets
01 §3.1-§3.3). Glossy urea thermoset, ivory #E6DDC2.

Size 69.8 x 114.3 x 5.6 mm. ORIGIN = the wall-face point at the plate
centre; the plate back is the plane y = 0 and the face looks -Y (Unity +Z);
nothing lies behind y = 0. Anchors screw_0 (top) and screw_1 (bottom) are
the screw seats, level with the field, where R3 draws Kit_OutletScrew;
plate_top is the top edge on the wall.

Budget (10_spec §1.3) LOD0 1,300 / LOD1 450 / LOD2 30, switch 1.5 / 4 / 12 m.
LOD1 and LOD2 are hand-built part sets, exported once kitlib has make_lods
(P-1/P-1b); until then the FBX holds LOD0 only and R3 culls it at 12 m.
Slot: Prop_ThermosetIvory (new tint-only slot, P-4b) + Prop_PlasticBlack for
the two screw bores. Render-only: no collider, no shadows (R3).

Era fit: a plain blank plate is timeless (1950s-today); no maker marks, no
text, no dates, no screwless snap cover (era lock 22_era_lock.md, outlets
01 §10).
"""

import outlet_plate_common as pc

NAME = "Kit_OutletBlank"
LOD1 = None                      # no kitlib LOD1 (decimation); hand-built parts below
BUDGET = (1300, 450, 30)
LOD1_RATIO = BUDGET[1] / BUDGET[0]
LOD2_RATIO = BUDGET[2] / BUDGET[0]
LOD_DISTANCES = pc.LOD_DISTANCES
SMOOTH_ANGLE = pc.SMOOTH_ANGLE


def build(kit):
    pc.begin(kit)
    W, H = pc.PLATE_1G
    screws = [(0.0, pc.BLANK_SCREW_Z), (0.0, -pc.BLANK_SCREW_Z)]
    info = pc.thermoset_plate(kit, W, H, screws, [])
    if pc.HAS_LODS:
        pc.plate_lod1(kit, W, H, screws, [])
        pc.plate_lod2(kit, W, H, crown_fan=True)
    pc.end(kit, __import__(__name__), "outlet_receptacle", W, H, pc.PROUD_PLATE, info["seats"])
