"""Single-pole toggle switch on a 1-gang ivory thermoset plate: the light
switch beside a room arch (Office 50 %, Level 0 rooms 15 %) and the "lone
switch" of a Level 0 corridor (25 % of chunks), centre at 1.22 m (10_spec
§1.6, §2.6).

Real reference: spec-grade AC quiet toggle, ivory nylon, in a standard
1-gang toggle plate: 2.75 x 4.50 in (69.8 x 114.3 mm), 0.22 in deep, toggle
opening 0.40 x 0.93 in (10.3 x 23.8 mm), two #6-32 oval-head screws 2.38 in
(60.3 mm) apart (Leviton Q-1289, Arrow Hart J-2, Pass & Seymour 510;
outlets 01 §3-§4). Toggle, not rocker: Decora existed from 1972-73 but was
the commercial minority in 1990 (01 §4).

Size 69.8 x 114.3 x 18.0 mm (the bat tip; limit 20). ORIGIN = the
wall-face point at the plate centre; plate back on y = 0; front -Y (Unity
+Z). The bat points UP (ON, the US convention); R3 rolls the instance 180
degrees for OFF, which the bat clears by the same margin (self-check (b)).
Anchors screw_0 (top), screw_1 (bottom) at the screw seats (level with the
field) for Kit_OutletScrew; plate_top.

Budget (10_spec §1.3) LOD0 2,100 / LOD1 800 / LOD2 40, switch 1.5 / 4 / 12 m;
LOD1/LOD2 hand-built, exported once make_lods exists (P-1/P-1b).
Slots: Prop_ThermosetIvory (plate), Prop_NylonIvory (switch face and bat),
Prop_PlasticBlack (slot, gaps, screw bores). Render-only (no collider).

Era fit: toggle switches and ivory thermoset plates are current in 1990;
no maker marks, no "ON/OFF" printing, no text, no dates, no screwless
plate (22_era_lock.md, outlets 01 §10).
"""

import sys

import outlet_plate_common as pc

NAME = "Kit_OutletToggle1"
LOD1 = None
BUDGET = (2100, 800, 40)
LOD1_RATIO = BUDGET[1] / BUDGET[0]
LOD2_RATIO = BUDGET[2] / BUDGET[0]
LOD_DISTANCES = pc.LOD_DISTANCES
SMOOTH_ANGLE = pc.SMOOTH_ANGLE


def toggle_plate(kit, module, gangs):
    """Shared by Toggle1 (gangs = (0,)) and Toggle2 (gangs = (-23, +23))."""
    pc.begin(kit)
    W, H = pc.PLATE_1G if len(gangs) == 1 else pc.PLATE_2G
    ow, oh, orr = pc.TOGGLE_OPENING
    openings = [pc.Opening("rrect", gx, 0.0, ow, oh, orr) for gx in gangs]
    screws = []
    for gx in gangs:
        screws += [(gx, pc.TOGGLE_SCREW_Z), (gx, -pc.TOGGLE_SCREW_Z)]
    wear = pc.plate_wear(W, H, [op.box() for op in openings], margin=14.0)
    info = pc.thermoset_plate(kit, W, H, screws, openings, wear=wear)
    clear = []
    for gx in gangs:
        dev = pc.toggle_device(kit, gx, 0.0, info["hf"])
        c = pc.bat_clearance(dev["bat_mesh"], gx, 0.0, dev["face_h"], dev["field"], dev["slot_floor"])
        clear.append(c)
        assert c["opening"] >= 0.5 and c["slot"] >= 0.5, "bat clearance %r" % c
    if pc.HAS_LODS:
        pc.plate_lod1(kit, W, H, screws, openings)
        pc.plate_lod2(kit, W, H, crown_fan=True, screws=screws)
        for gx in gangs:
            pc.toggle_lod1(kit, gx, 0.0, info["hf"])
            pc.toggle_lod2(kit, gx, 0.0, info["hf"])
    worst = {k: round(min(c[k] for c in clear), 3) for k in ("opening", "slot")}
    print("[o2] %s bat clearance at +-%.0f deg: opening %.3f mm, slot %.3f mm (need >= 0.5)" % (
        kit.name, pc.BAT_ANGLE, worst["opening"], worst["slot"]))
    seats = [s for s in info["seats"]]
    # anchor order: per gang top, bottom (screw_0 top-left ...)
    pc.end(kit, module, "outlet_switch", W, H, pc.PROUD_TOGGLE, seats,
           extra={"gangs": len(gangs), "batAngle": pc.BAT_ANGLE, "batClearanceMm": worst})


def build(kit):
    toggle_plate(kit, sys.modules[__name__], (0.0,))
