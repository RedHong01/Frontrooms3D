"""Two toggle switches under one 2-gang ivory thermoset plate: the Office
arch switch (30 % of Office arch switches, 10_spec §2.6). Two or more
switches in one place share "a single coverplate" (UH spec, outlets 01 §4).

Real reference: standard 2-gang toggle plate 4.56 x 4.50 in (115.9 x 114.3
mm), gang centres 1.81 in (46.0 mm) apart, two toggle openings 0.40 x 0.93
in (10.3 x 23.8 mm), four #6-32 oval-head screws (two per gang, 60.3 mm
apart); spec-grade ivory nylon toggles (Arrow Hart J-3, Leviton Q-1289,
outlets 01 §3-§4).

Size 115.9 x 114.3 x 18.0 mm. ORIGIN = the wall-face point at the plate
centre; plate back on y = 0; front -Y (Unity +Z). Both bats point UP (ON);
R3 may roll the whole plate, not one bat (one mesh). Anchors screw_0..3
(left gang top/bottom, right gang top/bottom) at the screw seats; plate_top.
Missing kit fallback: Toggle2 -> Toggle1 (10_spec §1.4).

Budget (10_spec §1.3) LOD0 3,500 / LOD1 1,300 / LOD2 50, 1.5 / 4 / 12 m;
LOD1/LOD2 hand-built, exported once make_lods exists (P-1/P-1b).
Slots: Prop_ThermosetIvory, Prop_NylonIvory, Prop_PlasticBlack. Render-only.

Era fit: as Kit_OutletToggle1 (current in 1990; no marks, text or dates).
"""

import sys

import outlet_plate_common as pc
import outlet_toggle1

NAME = "Kit_OutletToggle2"
LOD1 = None
BUDGET = (3500, 1300, 50)
LOD1_RATIO = BUDGET[1] / BUDGET[0]
LOD2_RATIO = BUDGET[2] / BUDGET[0]
LOD_DISTANCES = pc.LOD_DISTANCES
SMOOTH_ANGLE = pc.SMOOTH_ANGLE


def build(kit):
    outlet_toggle1.toggle_plate(kit, sys.modules[__name__], (-pc.GANG_X_2G, pc.GANG_X_2G))
