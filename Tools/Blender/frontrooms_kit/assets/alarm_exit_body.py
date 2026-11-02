"""Kit_AlarmExit_Body: the static part of the C16 alarm door's exit-alarm push bar, one per face (游戏视觉, 2026-11-01).

A c. 1985-90 battery exit alarm on a touch-bar device, unbranded (research: Documentation/research/door_hardware/
v7b_alarm_door/10_research.md): the ALARM HOUSING at the latch end (a cover on a base plate held by four oval-head screws;
on its front the arming KEY CYLINDER, US4006471 / US4631528 / US4785286; the pilot LED in a chrome bezel, US4631528's
"light-emitting diode ... indication of alarm state"; the mode / test push button, US4006471's push button 49), the
C-channel RAIL the touch pad rides in, and the hinge-end CAP. The pad itself is Kit_AlarmExit_Pad (it moves).

Part frame (alarm_exit_common): origin on the leaf face, X = 0 at the housing's latch-side edge, +X toward the hinge,
Y = 0 on the pad's centreline, +Z out of the face. Front = Unity +Z (kit -Y). LOD0 hero (bevelled cover and base plate,
96-segment cylinder, modelled keyway, LED dome, push button, oval-head screws, rounded C-channel); LOD1 plain boxes and
discs; LOD2 three boxes. No collider (render-only, the map's leaf collider is the door).
"""

import math
import random

import bmesh

import kitlib
import interact_door_common as dc
import alarm_exit_common as ax

NAME = "Kit_AlarmExit_Body"
LOD1 = None
LODS = None
SMOOTH_ANGLE = 35.0

L = ax.lods


def lathe_z(kit, profile, centre, slot, verts, name, close_top=True, close_bottom=True):
    """Revolve (r, h) about the Unity +Z axis through ``centre`` (Unity); h along +Z. kitlib.lathe merges doubles,
    so a profile that ends where it starts makes a closed ring."""
    obj = kit.lathe([(r, h) for r, h in profile], dc.U(*centre), slot, verts=verts, rot=(90, 0, 0), name=name,
                    close_top=close_top, close_bottom=close_bottom)
    return obj


def c_channel():
    """The rail's section (Y, Z): a C opening to +Z, 2 mm walls, rounded flange ends."""
    h, d, t = ax.RAIL_H / 2, ax.RAIL_D, ax.RAIL_T
    pts = [(-h, 0.0), (h, 0.0), (h, d), (h - t, d), (h - t, t), (-(h - t), t), (-(h - t), d), (-h, d)]
    radii = [0.0, 0.0, 0.0009, 0.0009, 0.0006, 0.0006, 0.0009, 0.0009]
    return dc.fillet_polygon(pts, radii, 4, closed=True)


def build(kit):
    ax.register_slots()
    rng = random.Random(7307)
    d = ax.HOUSING_D
    y0, y1 = ax.HOUSING_Y0, ax.HOUSING_Y1
    w = ax.HOUSING_W
    lip = ax.BASE_LIP

    # ------------------------------------------------------------ LOD0: the alarm housing
    # Base plate (the back box's flange on the leaf), then the cover on it: the seam between them reads at 0.3 m.
    L(dc.obox(kit, ax.ALU, lo=(0.0, y0, 0.0), hi=(w, y1, ax.BASE_T), bevel=0.0010, segs=2, name="housing base"), "0")
    L(dc.obox(kit, ax.ALU, lo=(lip, y0 + lip, ax.BASE_T), hi=(w - lip, y1 - lip, d), bevel=ax.COVER_R, segs=5,
              name="housing cover"), "0")

    # The arming key cylinder: a mortise cylinder face (1-5/32 in) in a collar, its plug with a modelled keyway.
    cx, cy = ax.CYL_X, ax.CYL_Y
    collar = [(0.0, -0.0006), (0.0178, -0.0006), (0.0178, 0.0008), (0.0172, 0.0015), (0.0158, 0.0018), (0.0150, 0.0018),
              (0.0150, 0.0010), (0.0, 0.0010)]
    L(lathe_z(kit, collar, (cx, cy, d), ax.CHROME, 96, "cylinder collar"), "0")
    face = [(0.0, 0.0008), (0.0147, 0.0008), (0.0147, 0.0024), (0.0141, 0.0031), (0.0068, 0.0033), (0.0068, 0.0028),
            (0.0, 0.0028)]
    L(lathe_z(kit, face, (cx, cy, d), ax.CHROME, 96, "cylinder face"), "0")
    groove = [(0.0, 0.0026), (0.0068, 0.0026), (0.0068, 0.0029), (0.0, 0.0029)]
    L(lathe_z(kit, groove, (cx, cy, d), ax.DARK, 48, "cylinder plug groove"), "0")
    plug = [(0.0, 0.0027), (0.0062, 0.0027), (0.0062, 0.0031), (0.0058, 0.0033), (0.0, 0.0033)]
    L(lathe_z(kit, plug, (cx, cy, d), ax.CHROME, 64, "cylinder plug"), "0")
    keyway = [(-0.00045, -0.0041), (0.00045, -0.0041), (0.00045, 0.0004), (0.00095, 0.0011), (0.00045, 0.0018),
              (0.00045, 0.0039), (-0.00045, 0.0039), (-0.00045, 0.0021), (-0.00090, 0.0014), (-0.00045, 0.0007)]
    z_kw = d + 0.00332
    L(dc.loft_z(kit, [(z_kw - 0.0003, [(cx + u, cy + v) for u, v in keyway]),
                      (z_kw, [(cx + u, cy + v) for u, v in keyway])], ax.DARK, "cylinder keyway"), "0")

    # The pilot LED (T-1 3/4, 5 mm) in a chrome bezel, and the mode / test push button in its own bezel.
    bezel = [(0.0, -0.0005), (0.0044, -0.0005), (0.0044, 0.0010), (0.0040, 0.0016), (0.0028, 0.0016), (0.0028, 0.0010),
             (0.0, 0.0010)]
    L(lathe_z(kit, bezel, (ax.LED_X, ax.LED_Y, d), ax.CHROME, 48, "led bezel"), "0")
    dome = [(0.0, 0.0006), (0.00250, 0.0006), (0.00250, 0.0024)]
    for k in range(1, 9):
        a = math.radians(90.0 * k / 8.0)
        dome.append((0.00250 * math.cos(a), 0.0024 + 0.00250 * math.sin(a)))
    L(lathe_z(kit, dome, (ax.LED_X, ax.LED_Y, d), ax.LED, 32, "led dome"), "0")
    L(lathe_z(kit, [(0.0, -0.0005), (0.0060, -0.0005), (0.0060, 0.0008), (0.0055, 0.0013), (0.0042, 0.0013),
                    (0.0042, 0.0008), (0.0, 0.0008)], (ax.BTN_X, ax.BTN_Y, d), ax.CHROME, 48, "button bezel"), "0")
    cap = [(0.0, 0.0004), (0.0039, 0.0004), (0.0039, 0.0021), (0.0035, 0.0027), (0.0020, 0.0030), (0.0, 0.0031)]
    L(lathe_z(kit, cap, (ax.BTN_X, ax.BTN_Y, d), ax.DARK, 40, "button cap"), "0")

    # Four tamper-resistant oval-head cover screws.
    for sx, sy in ax.SCREWS:
        L(dc.oval_head(kit, (sx, sy, d), (0.0, 0.0, 1.0), ax.CHROME, rng, d=0.0062, name="cover screw"), "0")

    # ------------------------------------------------------------ LOD0: rail, its dark floor, the hinge-end cap
    sec = c_channel()
    L(dc.loft_x(kit, [(ax.RAIL_X0 - 0.002, sec), (ax.RAIL_X1, sec)], ax.ALU, "rail"), "0")
    L(dc.obox(kit, ax.DARK, lo=(ax.RAIL_X0, -(ax.RAIL_H / 2 - ax.RAIL_T - 0.0002), ax.RAIL_T),
              hi=(ax.RAIL_X1 - 0.001, ax.RAIL_H / 2 - ax.RAIL_T - 0.0002, ax.RAIL_T + 0.003), name="rail floor"), "01")
    ch = ax.CAP_H / 2
    L(dc.obox(kit, ax.ALU, lo=(ax.CAP_X0, -ch, 0.0), hi=(ax.CAP_X1, ch, ax.CAP_D), bevel=0.0045, segs=4,
              name="end cap"), "0")
    L(dc.oval_head(kit, ((ax.CAP_X0 + ax.CAP_X1) / 2, 0.0, ax.CAP_D), (0.0, 0.0, 1.0), ax.CHROME, rng, d=0.0055,
                   name="cap screw"), "0")

    # ------------------------------------------------------------ LOD1: boxes and discs (the LED keeps its dome)
    L(dc.obox(kit, ax.ALU, lo=(0.0, y0, 0.0), hi=(w, y1, d), bevel=0.003, segs=1, name="housing_1"), "1")
    L(lathe_z(kit, [(0.0, -0.0006), (0.0178, -0.0006), (0.0178, 0.0018), (0.0, 0.0031)], (cx, cy, d), ax.CHROME, 16,
              "cylinder_1"), "1")
    L(lathe_z(kit, [(0.0, -0.0005), (0.0044, -0.0005), (0.0044, 0.0016), (0.0, 0.0016)], (ax.LED_X, ax.LED_Y, d),
              ax.CHROME, 8, "led bezel_1"), "1")
    L(lathe_z(kit, [(0.0, 0.0006), (0.0025, 0.0006), (0.0025, 0.0030), (0.0, 0.0049)], (ax.LED_X, ax.LED_Y, d),
              ax.LED, 8, "led dome_1"), "1")
    L(lathe_z(kit, [(0.0, -0.0005), (0.0060, -0.0005), (0.0060, 0.0013), (0.0039, 0.0031), (0.0, 0.0031)],
              (ax.BTN_X, ax.BTN_Y, d), ax.DARK, 8, "button_1"), "1")
    hh = ax.RAIL_H / 2
    L(dc.obox(kit, ax.ALU, lo=(ax.RAIL_X0 - 0.002, -hh, 0.0), hi=(ax.RAIL_X1, -hh + ax.RAIL_T, ax.RAIL_D), name="rail lower_1"), "1")
    L(dc.obox(kit, ax.ALU, lo=(ax.RAIL_X0 - 0.002, hh - ax.RAIL_T, 0.0), hi=(ax.RAIL_X1, hh, ax.RAIL_D), name="rail upper_1"), "1")
    L(dc.obox(kit, ax.ALU, lo=(ax.CAP_X0, -ch, 0.0), hi=(ax.CAP_X1, ch, ax.CAP_D), bevel=0.003, segs=1, name="end cap_1"), "1")

    # ------------------------------------------------------------ LOD2: three boxes
    L(dc.obox(kit, ax.ALU, lo=(0.0, y0, 0.0), hi=(w, y1, d), name="housing_2"), "2")
    L(dc.obox(kit, ax.ALU, lo=(ax.RAIL_X0 - 0.002, -hh, 0.0), hi=(ax.RAIL_X1, hh, ax.RAIL_D - 0.004), name="rail_2"), "2")
    L(dc.obox(kit, ax.ALU, lo=(ax.CAP_X0, -ch, 0.0), hi=(ax.CAP_X1, ch, ax.CAP_D), name="end cap_2"), "2")

    kit.no_collider()
    kit.tag("interactable", "alarm_exit", "alarm_door")
    ax.lod_meta(kit, ax.BODY_LOD_DISTANCES, ax.BODY_BUDGET,
                "hand-built LOD0/1/2 (kitlib fr_lods, P-1b); lodDistances (d01, d12, dcull) m")
    kit.anchor("origin", dc.U(0.0, 0.0, 0.0))
    kit.anchor("cylinder", dc.U(cx, cy, d + 0.0033))
    kit.anchor("led", dc.U(ax.LED_X, ax.LED_Y, d + 0.0049))
    kit.anchor("button", dc.U(ax.BTN_X, ax.BTN_Y, d + 0.0031))
    kit.anchor("hinge_end", dc.U(ax.DEVICE_LEN, 0.0, 0.0))
    kit.anchor("housing_top", dc.U(w / 2, y1, d))
    kit.meta["partFrame"] = ("origin = leaf face at the housing's latch-side edge on the pad centreline; +X toward the hinge; "
                             "front = Unity +Z (kit -Y); +Y up. Face S: Euler(0, 90, 0); face P: Euler(0, -90, 0), scale (-1, 1, 1)")
    kit.meta["motion"] = {"type": "static", "pad": "Kit_AlarmExit_Pad (same frame) moves along part -Z"}
    kit.meta["alarmExit"] = {
        "deviceLength": ax.DEVICE_LEN, "housing": [w, ax.HOUSING_Y1 - ax.HOUSING_Y0, d], "housingY": [y0, y1],
        "rail": [ax.RAIL_X0, ax.RAIL_X1, ax.RAIL_H, ax.RAIL_D], "cap": [ax.CAP_X0, ax.CAP_X1],
        "cylinder": [cx, cy], "led": [ax.LED_X, ax.LED_Y], "button": [ax.BTN_X, ax.BTN_Y],
        "slotLed": ax.LED, "source": "research/door_hardware/v7b_alarm_door/10_research.md",
    }
