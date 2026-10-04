"""Key board: a dark painted board with two rows of brass cup hooks and a
typed paper number under each hook (spec 10 §4.2; 03 §2.3 A; 02 §9).

Real-world reference: the hook board in a store or office back room,
timeless 1950-2000: a 3/4" board, painted or stained dark, screwed to the
wall, 1" brass screw-in cup hooks, typed or label-maker numbers under them.
Board 0.30 x 0.40 x 0.018 with 3 mm round-overs, top at 1.65 m; hooks at
X +/-0.035 and +/-0.105 in rows at Y 1.54 and 1.42 (Ø 3 mm wire, shoulder
disc, J cup tip up, 0.026 proud of the board); typed number strips 01-08;
two slotted brass mounting screws.

Origin = THE WALL FACE PLANE AT FLOOR LEVEL, CENTRED (§4.2 shared host
convention); front +Z faces the room; the board's back is flush with the
wall face (Z 0). Proud 0.044 (<= 0.10). Heights are baked in.
Anchors hook_0 .. hook_7 = where a ring rests in each cup (top of the wire,
1.2 mm behind the bend, Z 0.0355): hook_0..3 the top row and hook_4..7 the bottom
row, each read left to right as seen from the room (Unity +X to -X);
key_hook = hook_6 (bottom row, third from the left; the map may pick any).

Budget §9.3: 4,000 / 1,400 / 150 tris, LOD 3 / 8 / 40 m; 0.40 m tall, so no
LOD1 until P-1. Slots: Prop_WoodDark (board, lum 0.014), Prop_Brass (hooks,
screws), Prop_KeyTagNo (strips, cells 01-08 via uv_rect; NEW slot, fallback
Prop_Paper). Tags interactable, key_host, wall_decor. Render-only.

NAME: the spec calls this asset Kit_KeyBoard (§4.2, §9.3). That name differs
from the existing desk keyboard Kit_Keyboard (assets/keyboard.py) only in
case: on the default case-insensitive macOS volume Kit_KeyBoard.fbx/.json
OVERWRITE Kit_Keyboard.fbx/.json (it happened in the private clone on the
first G3 build), and Unity's Resources.Load is not case-safe either. So the
asset ships as Kit_KeyHookBoard; the map's KeySpot ``host`` value and the
facade must use that name (sidecar meta specName records the spec's name).
"""

import math

import interact_key_common as kc
from interact_key_common import U

NAME = "Kit_KeyHookBoard"          # spec name Kit_KeyBoard collides with Kit_Keyboard (see docstring)
LOD1_RATIO, LOD2_RATIO = 1400 / 4000, 150 / 4000
LOD_DISTANCES = (3.0, 8.0, 40.0)
BUDGET = (4000, 1400, 150)

WOOD, BRASS = "Prop_WoodDark", "Prop_Brass"
BW, BH, BT = 0.30, 0.40, 0.018
TOP = 1.65
ROWS = (1.54, 1.42)
COLS = (0.105, 0.035, -0.035, -0.105)        # left to right as seen from the room
WIRE_R = 0.0015
LEG = 0.0172                                  # ring rest point at Z 0.0355 (§4.2: about 0.036)
STRIP = (0.024, 0.012)
STRIP_DROP = 0.021                            # strip centre below the row height
STRIP_CROP = (0.62, 0.31)


def build(kit):
    kc.register_slots()
    cy = TOP - BH / 2
    path = kc.rounded_rect(BW, BH, 0.004, 3, 0.0, cy)
    ro = 0.003
    prof = [(0.0, 0.0), (0.0, BT - ro)]
    for i in range(1, 5):
        a = math.radians(90 * i / 4)
        prof.append((ro * (1 - math.cos(a)), BT - ro + ro * math.sin(a)))
    prof.append((ro + 0.0002, BT))
    board = kc.profile_sweep(kit, path, prof, WOOD, lambda u, v, w: U(u, v, w), name="board",
                             cap_first=True, cap_last=True)
    board["fr_grain"] = "z"
    hooks = []
    parts = []
    for r, y in enumerate(ROWS):
        for c, x in enumerate(COLS):
            objs, hp = kc.cup_hook(kit, (x, y - WIRE_R, BT), BRASS, wire_r=WIRE_R, leg=LEG, bend_r=0.0055,
                                   rise=0.0045, shoulder_d=0.0068, shoulder_t=0.0015, verts=10,
                                   name="cup hook %d" % (r * 4 + c))
            parts += objs
            hooks.append(hp)
    # Typed number strips 01-08 under the hooks.
    for k, (hx, hy, hz) in enumerate(hooks):
        sw, sh = STRIP
        u0, v0, u1, v1 = kc.number_cell_rect(k + 1, STRIP_CROP[0], STRIP_CROP[1])
        sy = hy - STRIP_DROP
        z = BT + 0.0002
        q = kc.quad_uv(kit, [(hx + sw / 2, sy - sh / 2, z), (hx - sw / 2, sy - sh / 2, z),
                             (hx - sw / 2, sy + sh / 2, z), (hx + sw / 2, sy + sh / 2, z)],
                       [(u0, v0), (u1, v0), (u1, v1), (u0, v1)], kc.KEYTAGNO, name="number strip")
        kc.paint_wear(q)
    for y in (TOP - 0.022, TOP - BH + 0.022):
        s = kc.oval_screw(kit, (0.0, y, BT), BRASS, head_d=0.0078, dome_h=0.0016, slot_w=0.0008, slot_d=0.0009,
                          slot_deg=20 if y > 1.5 else -55, segs=14, name="mount screw")
        kc.drop2(s)
    for o in parts:
        if "shoulder" in o.name:
            kc.drop2(o)

    def grime(p):
        X, Y = -p.x, p.z
        d = min(math.hypot(X - hx, Y - hy) for hx, hy, _ in hooks)
        return (0.78 + 0.22 * min(1.0, d / 0.05), 1.0, 1.0, 1.0)
    kc.paint_wear(board, grime)
    kc.wear_all(kit)

    for k, hp in enumerate(hooks):
        kit.anchor("hook_%d" % k, U(*hp))
    kit.anchor("key_hook", U(*hooks[6]))
    # Hung pose recipe (solved by the G3 self-check with BVH overlap tests on
    # the real meshes; the facade applies it, spec 10 §4.1 "hung"): the ring's
    # hook_contact goes ringLiftM above key_hook, the ring turned
    # Euler(0, -60, 0) from the host (its tangent 60 deg toward the room); the
    # key hangs tip down from key_contact with its flat normal (+X) square to
    # the room (30 deg of twist in its Ø 4.8 hole) and the tag hangs from
    # tag_contact face to the room (30 deg in its Ø 5 hole on the 2.4 mm tab).
    kit.meta["hungPose"] = {"ringYawDeg": -60.0, "ringLiftM": 0.0012, "keyTwistDeg": -30.0, "tagTwistDeg": -30.0,
                            "key": "LookRotation(down, Cross(down, hostForward)): tip down, flats to the room",
                            "tag": "LookRotation(hostForward, up): face to the room, hanging along -Y"}
    kit.no_collider()
    kit.tag("interactable", "key_host", "wall_decor")
    kc.lod_meta(kit, LOD_DISTANCES, BUDGET)
    kit.meta["keyHostDefault"] = "hook_6"
    kit.meta["specName"] = "Kit_KeyBoard"
    kit.meta["numberStrips"] = {"slot": kc.KEYTAGNO, "fallback": kc.KEYTAGNO_FALLBACK, "cells": list(range(1, 9))}

    lo, hi = kc.eval_bounds_unity(kit)
    assert abs(hi[1] - TOP) < 1e-5 and abs(lo[1] - (TOP - BH)) < 1e-5, ("board heights", lo, hi)
    assert abs((hi[0] - lo[0]) - BW) < 1e-5, ("width", lo, hi)
    assert lo[2] > -1e-6 and hi[2] <= 0.10, ("proud", lo, hi)
    for hx, hy, hz in hooks:
        assert 1.40 <= hy <= 1.55 and hz <= 0.10, (hx, hy, hz)
    kc.check_budget(kit, BUDGET[0])
