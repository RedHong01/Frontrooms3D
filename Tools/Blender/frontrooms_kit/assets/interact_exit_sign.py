"""Kit_ExitSign: the 1990 single-face wall EXIT sign (exit-sign workflow, P1).

Rewritten 2026-10-03 by the exit-sign workflow, which took over this module,
Kit_ExitSign and Kit_ExitSign_Dead from G3 (interactables/build_g3 §0.4,
item 15). Spec: Documentation/research/exit_sign/10_spec.md §4.1-§4.5;
construction in exit_sign_common.py, numbers in exit_sign_geom.py.

What it is: a white-enamel drawn steel pan 346 x 212 x 64 mm on the wall, a
pressed face frame (bead, 4 slotted oval-head screws, the 0.6 x 0.4 seam), a
0.9 mm stencil plate with EXIT cut through it (6 in letters, 3/4 in strokes,
both chevron knockouts closed), a white spacer and a red diffuser 6 mm
behind the plate. A 1/2 in knockout on the top, two keyholes on the back on
3.5 in centres (the outlet-box screws). No pilot light, no test switch, no
brand, no date (era lock, T9).

Origin: the bottom-centre of the back plane (Y 0 = the housing bottom, Z 0 =
the wall face); front +Z = the room. The Level Designer's Wall placement
with the "hang" anchor puts the bottom at min(2.10, ceiling - 0.262).
Seen from the front, +X is the reader's LEFT (Unity is left-handed).

VARIANT Kit_ExitSign_Dead: ExitSign_Diffuser -> ExitSign_Diffuser_Dead (no
emission keyword at all). Main's GUIDs for both names stay (spec §1).
Budget §4.3: 3,500 +-15 % / LOD1 1,100; lodDistances [5, 0, 0] (2 LODs:
LOD1 never culls; never null).
"""

import exit_sign_common as es
import exit_sign_geom as g
from interact_key_common import U

NAME = "Kit_ExitSign"
VARIANTS = {"Kit_ExitSign_Dead": {es.DIFFUSER: es.DIFFUSER_DEAD}}
BUDGET = (3500, 1100, 200)
LOD1 = BUDGET[1] / BUDGET[0]
SMOOTH_ANGLE = 40.0
ZF = 62.5            # the frame's front flat, mm from the wall (bead 63.5, screw domes ~64.0)
MM = es.MM


def build(kit):
    es.register_slots()
    yc = g.HOUSING_H / 2
    face = es.Face((0.0, yc * MM, ZF * MM), (0, 0, 1), (-1, 0, 0), screw_axis="+Z")
    es.build_pan_wall(kit, yc, ZF)
    es.build_face(kit, face)
    es.build_knockout(kit, (0.0, g.HOUSING_H * MM, 0.024))
    # Lamp chassis (build round 3): Z 1.5 mm (clear of the 1 mm keyhole pockets) to
    # 2 mm behind the diffuser, inset 8 mm from the pan sides.
    z_back, z_front = 1.5, ZF + es.chassis_front_c()
    es.build_chassis(kit, (0.0, yc * MM, (z_back + z_front) / 2 * MM),
                     ((g.HOUSING_W - 2 * es.CHASSIS_INSET) * MM, (g.HOUSING_H - 2 * es.CHASSIS_INSET) * MM, (z_front - z_back) * MM))

    z_plate = (ZF - g.LIP_DEPTH) * MM
    z_diff = (ZF - g.LIP_DEPTH - g.SHEET - g.DIFFUSER_GAP) * MM
    kit.anchor("hang", U(0.0, 0.0, 0.0))
    kit.anchor("back_centre", U(0.0, yc * MM, 0.0))
    kit.anchor("face", U(0.0, yc * MM, z_plate))
    kit.anchor("face_dir", U(0.0, yc * MM, z_plate + 0.10))
    kit.anchor("diffuser", U(0.0, yc * MM, z_diff))
    for nm, lamp in (("lamp_a", g.LAMP_A), ("lamp_b", g.LAMP_B)):
        a, b = g.lamp_reader_mm(lamp)
        kit.anchor(nm, U(-a * MM, (yc + b) * MM, 0.030))
    kit.anchor("glow", U(0.0, -0.02, 0.06))
    kit.anchor("mount_l", U(g.KEYHOLE_PITCH / 2 * MM, 0.140, 0.0))
    kit.anchor("mount_r", U(-g.KEYHOLE_PITCH / 2 * MM, 0.140, 0.0))
    kit.no_collider()
    kit.tag("exit_sign", "wall_decor")
    es.exit_meta(kit, 1)
    kit.meta["lodBudget"] = list(BUDGET)
    es.check_budget(kit, BUDGET[0])
