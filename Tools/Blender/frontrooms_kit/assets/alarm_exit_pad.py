"""Kit_AlarmExit_Pad: the moving touch pad of the C16 alarm door's push bar (游戏视觉, 2026-11-01).

An extruded anodised-aluminium pad with a lightly crowned face, generous front rounds and black end caps, riding in
Kit_AlarmExit_Body's C-channel. Same part frame as the body (alarm_exit_common), so both spawn at one transform; the
runtime moves this one along part -Z by up to PAD_TRAVEL (12 mm): Red's 10A hold ("the bar visibly travels" for 0.4 s,
research/interaction/doors/20_design.md 4.6), a short strain on a refused push, a press at the trip, and the spring back.
"""

import kitlib
import interact_door_common as dc
import alarm_exit_common as ax

NAME = "Kit_AlarmExit_Pad"
LOD1 = None
LODS = None
SMOOTH_ANGLE = 35.0

L = ax.lods


def section(grow=0.0, front_segs=6, crown=0.0006):
    """The pad's (Y, Z) section, counter-clockwise; grow widens it (the end caps), crown bows the face out."""
    h = ax.PAD_H / 2 + grow
    z0, z1 = ax.PAD_Z0 - grow, ax.PAD_Z1 + grow
    r = ax.PAD_FRONT_R
    flat = h - r
    pts = [(-h, z0), (h, z0), (h, z1)]
    radii = [0.0015, 0.0015, r]
    if crown > 0.0:
        for k in range(4, -5, -1):
            y = flat * k / 5.0
            pts.append((y, z1 + crown * (1.0 - (y / flat) ** 2)))
            radii.append(0.0)
    pts.append((-h, z1))
    radii.append(r)
    segs = [2, 2, front_segs] + [1] * (len(pts) - 4) + [front_segs]
    return dc.fillet_polygon(pts, radii, segs, closed=True)


def scaled(sec, k):
    cy, cz = 0.0, (ax.PAD_Z0 + ax.PAD_Z1) / 2
    return [(cy + (y - cy) * k, cz + (z - cz) * k) for y, z in sec]


def build(kit):
    ax.register_slots()
    x0, x1, t = ax.PAD_X0, ax.PAD_X1, ax.PAD_CAP_T
    body = section()
    L(dc.loft_x(kit, [(x0 + t - 0.0002, body), (x1 - t + 0.0002, body)], ax.ALU, "pad"), "0")
    capsec = section(grow=0.0007, crown=0.0)
    inner = scaled(capsec, 0.93)
    # End caps: a 1 mm chamfer onto the outer end, flush onto the pad.
    L(dc.loft_x(kit, [(x0, inner), (x0 + 0.001, capsec), (x0 + t, capsec)], ax.DARK, "pad cap latch"), "0")
    L(dc.loft_x(kit, [(x1 - t, capsec), (x1 - 0.001, capsec), (x1, inner)], ax.DARK, "pad cap hinge"), "0")

    simple = section(front_segs=2, crown=0.0)
    L(dc.loft_x(kit, [(x0 + t, simple), (x1 - t, simple)], ax.ALU, "pad_1"), "1")
    L(dc.loft_x(kit, [(x0, simple), (x0 + t, simple)], ax.DARK, "pad cap latch_1"), "1")
    L(dc.loft_x(kit, [(x1 - t, simple), (x1, simple)], ax.DARK, "pad cap hinge_1"), "1")
    h = ax.PAD_H / 2
    L(dc.obox(kit, ax.ALU, lo=(x0, -h, ax.PAD_Z0), hi=(x1, h, ax.PAD_Z1), name="pad_2"), "2")

    kit.no_collider()
    kit.tag("interactable", "alarm_exit", "alarm_door")
    ax.lod_meta(kit, ax.PAD_LOD_DISTANCES, ax.PAD_BUDGET,
                "hand-built LOD0/1/2 (kitlib fr_lods, P-1b); lodDistances (d01, d12, dcull) m")
    kit.anchor("origin", dc.U(0.0, 0.0, 0.0))
    kit.anchor("push_point", dc.U((x0 + x1) / 2, 0.0, ax.PAD_Z1))
    kit.meta["partFrame"] = "the same frame as Kit_AlarmExit_Body; the runtime moves this part along part -Z"
    kit.meta["motion"] = {"type": "translate", "axis": "-Z", "travel": ax.PAD_TRAVEL,
                          "hold": "0.4 s facing push (Red 10A), map hook AlarmBarPress (contract)"}
