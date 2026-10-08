"""Wood door frame for the Lobby FREE door (family member L0-F): walnut
ranch casing with a back band on both faces, walnut linings over the bare
reveal, an applied walnut stop on the push side, an oak saddle, and the
frame halves of three brass 4-1/2" five-knuckle butts. Gap-free single-swing
construction derived from RE8's deep, dark door surround (10_spec §1, §2.3).

Real-world reference: a 1960s-80s US office interior door opening in a
cased wood jamb: 2-1/4"-3" ranch casing with a back band (here 77 mm, swept
round three sides, mitred at the head), an applied 5/8" stop, full-mortise
4-1/2" x 4-1/2" standard-weight butts at "5-10-equal" (0.254 / 1.0565 /
1.859 m), a 1/2" oak transition saddle with 1:2 bevels (ADAAG 1991 4.13.8).
No plinth blocks (none in 1990 commercial fit-out). Era fit: everything
here was ordinary in 1955-1990; no maker marks.

Origin: DOOR ROOT D (10_spec §1.2): the opening's hinge-jamb edge on the
wall centre line at floor level. Unity +Z runs along the opening to the
latch jamb (1.0), +Y up, +X is the swing / pull face S, -X the push face P
where the stop is. Spawn under the chunk root at the hinge position with
rotation door.closed and localScale (S_sign, 1, 1); label
"Door frame (kit)". Front: the kit's usual -Y front is meaningless here;
the sidecar's frontAxis +Z is the opening direction.

Section (Unity metres, §1.4): linings X +-0.0795, faces Z 0.002 / 0.998,
head 2.098; casing X +-(0.0795..0.105), Z -0.075..0.002 and 0.998..1.075,
head 2.098..2.175 (encloses the map's 0.07 x 0.20 trims, w >= 0.0215 over
them, asserted in interact_door_common); stop X -0.0605..-0.025 (35.5 x
16 mm, 3 mm round-over on the leaf-side arris, 1.5 mm chamfer on the room
side), Z 0.002..0.018 / 0.982..0.998, head 2.082..2.098, scribed onto the
saddle; saddle 0.152 x 0.012. Hinge-lining mortises take the brass frame
leaves flush; a 1.6 mm ANSI strike pocket (0.032 x 0.124 at Y 1.000) takes
G2's Kit_Lock_StrikeBored flush (added here: the spec places the strike
"mortised flush" but lists no pocket for this frame).

Budget (§9.1): 2,800 / 1,100 / 250 tris, slots WoodWalnut (submesh 0),
Brass, WoodOak. Render-only: kit.no_collider(). Shadows: the importer casts
them (size > 0.3 m). LOD1 (0.40) keeps the casing/lining/stop/saddle
envelope uncollapsed (kitlib's fr_lod_keep group), so LOD1 stays gap-free;
screws and pin tips drop at LOD1, the hinge halves at LOD2 (flagged).
"""

import math
import random
import sys

import interact_door_common as dc

NAME = "Kit_DoorFrame_Wood"
LOD1 = 0.40
LOD1_RATIO = 0.40
LOD2_RATIO = 0.09
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0

WOOD = "Prop_WoodWalnut"
BRASS = "Prop_Brass"
OAK = "Prop_WoodOak"


def stop_profile():
    """(d, X): d = 0 at the stop's opening face (Z 0.018), 0.016 at the lining."""
    pts = [(0.016, dc.STOP_X0), (0.0015, dc.STOP_X0), (0.0, dc.STOP_X0 + 0.0015), (0.0, dc.STOP_X1 - 0.003)]
    # R 3 round-over on the leaf-side arris: centre (d 0.003, X -0.028)
    for k in range(1, 4):
        a = math.radians(-90 + 90 * k / 3)
        pts.append((0.003 + 0.003 * math.sin(a), dc.STOP_X1 - 0.003 + 0.003 * math.cos(a)))
    pts.append((0.016, dc.STOP_X1))
    return pts


def wear(P, N, edge, obj):
    X, Y, Z = P
    r = g = b = 1.0
    name = obj.name
    # hands on the latch-side casing and lining around the lever height
    if Z > 0.95:
        r -= 0.18 * dc.smoothstep(0.75, 0.95, Y) * (1 - dc.smoothstep(1.35, 1.55, Y))
    # mops, shoes and carts: low arrises
    if edge:
        g -= 0.35 * (1 - dc.smoothstep(0.08, 0.40, Y))
        g -= 0.15                                   # every arris a little
    if "saddle" in name:
        r -= 0.20 * (1 - min(1.0, abs(X) / 0.05))   # foot traffic grime in the middle
        if edge:
            g -= 0.30
    if obj.get("fr_screw"):
        b -= 0.35
    return r, g, b


def build(kit):
    dc.u_grain(kit)
    rng = random.Random(41010)
    envelope = []

    # 1. Casing, both faces (walnut: the dominant slot goes first -> submesh 0).
    for side, tag in ((1, "s"), (-1, "p")):
        prof = [(u, side * (dc.WALL_X + w)) for u, w in dc.RANCH_CASING]
        legs = dc.u_sweep(kit, prof, WOOD, "casing " + tag, z_h=dc.LINING_Z_H, z_l=dc.LINING_Z_L, y_t=dc.LINING_Y)
        for leg in legs:
            leg["fr_uv_offset"] = (round(rng.uniform(0, 0.6), 3), round(rng.uniform(0, 0.6), 3))
        envelope += legs

    # 2. Linings: 2 mm skins over the wall ends and the head soffit.
    lin_h = dc.obox(kit, WOOD, lo=(-dc.WALL_X, 0.0, dc.LINING_BACK_H), hi=(dc.WALL_X, dc.LINING_Y, dc.LINING_Z_H),
                    name="lining hinge", grain="z")
    lin_l = dc.obox(kit, WOOD, lo=(-dc.WALL_X, 0.0, dc.LINING_Z_L), hi=(dc.WALL_X, dc.LINING_Y, dc.LINING_BACK_L),
                    name="lining latch", grain="z")
    lin_t = dc.obox(kit, WOOD, lo=(-dc.WALL_X, dc.LINING_Y, dc.LINING_BACK_H), hi=(dc.WALL_X, dc.LINING_BACK_Y, dc.LINING_BACK_L),
                    name="lining head", grain="y")
    envelope += [lin_h, lin_l, lin_t]
    # hinge mortises (the brass frame leaves fill them) and the strike pocket
    dc.cut(lin_h, [dc.cutter((dc.PLATE_X0, y0, -0.001), (0.030, y1, 0.0025)) for y0, y1 in dc.HINGES])
    sw, sh, sy = dc.STRIKE_BORED
    dc.cut(lin_l, [dc.cutter((-sw / 2, sy - sh / 2, dc.LINING_Z_L - 0.0001), (sw / 2, sy + sh / 2, dc.LINING_Z_L + dc.STRIKE_DEPTH))])

    # 3. Applied stop, push side, scribed onto the saddle's bevel.
    stops = dc.u_sweep(kit, stop_profile(), WOOD, "stop", z_h=dc.STOP_Z_H, z_l=dc.STOP_Z_L, y_t=dc.HEAD_STOP_Y0,
                       bot_fn=lambda x, d: dc.saddle_top_at(x))
    for leg in stops:
        leg["fr_uv_offset"] = (round(rng.uniform(0, 0.6), 3), round(rng.uniform(0, 0.6), 3))
    envelope += stops

    # 4. Oak saddle between the linings.
    sec = dc.saddle_section(flutes=False)
    saddle = dc.loft_z(kit, [(dc.LINING_Z_H, sec), (dc.LINING_Z_L, sec)], OAK, "saddle", grain="y")
    # Fix pass 2026-10-08 (critic H3): Prop_WoodOak_A is U-grain; turn the saddle's UVs so the grain runs
    # along the threshold (interact_door_common.u_grain), not across it.
    saddle["fr_ugrain"] = True
    envelope.append(saddle)

    # 5. Frame halves of the three butts (brass), screws and button tips.
    for y0, _ in dc.HINGES:
        for p in dc.hinge_half(kit, "frame", y0, BRASS, rng):
            dc.lod2_drop(p)
            if p.get("fr_screw") or "tip" in p.name or "web" in p.name:
                kit.lod1_drop(p)

    dc.finalize(kit, SMOOTH_ANGLE, wear, protect=envelope)

    # ---- checks (Unity metres) ------------------------------------------
    dc.assert_box(kit.parts, (-dc.CASING_X1, 0.0, -0.075), (dc.CASING_X1, dc.CASING_TOP, 1.075), what=NAME)
    dc.assert_box(stops, (dc.STOP_X0, 0.0, dc.LINING_Z_H), (dc.STOP_X1, dc.LINING_Y, dc.LINING_Z_L), what="stop")
    dc.assert_box([saddle], (-dc.SADDLE_HALF, 0.0, dc.LINING_Z_H), (dc.SADDLE_HALF, dc.SADDLE_H, dc.LINING_Z_L), what="saddle")
    dc.assert_outside_leaf(kit.parts, NAME)

    # ---- metadata -------------------------------------------------------
    dc.anchor(kit, "strike", 0.0, 1.000, dc.LINING_Z_L)
    dc.anchor(kit, "hinge_axis", dc.AXIS_X, 0.0, dc.AXIS_Z)
    dc.anchor(kit, "hinge_axis_dir", dc.AXIS_X, 0.10, dc.AXIS_Z)
    dc.anchor(kit, "head_dust_a", 0.0, dc.LINING_Y, 0.05)
    dc.anchor(kit, "head_dust_b", 0.0, dc.LINING_Y, 0.95)
    dc.anchor(kit, "threshold", 0.0, dc.SADDLE_H, 0.50)
    kit.no_collider()
    kit.tag("interactable", "door", "door_frame", "lobby", "frame_wood")
    kit.meta["frameType"] = "wood"
    kit.meta["doorRoot"] = "D: origin hinge-jamb edge, wall centre line, floor; +Z along opening; +X swing (pull) face"
    dc.lod_meta(kit, sys.modules[__name__])
