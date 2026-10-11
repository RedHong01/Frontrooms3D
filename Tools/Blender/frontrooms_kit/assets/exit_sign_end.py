"""Kit_ExitSign_End: the double-face EXIT sign on an end (flag) mount (P2).

Exit-sign workflow, spec Documentation/research/exit_sign/10_spec.md §4.1
(mount E: housing 2.294-2.506 under 2.90 when its origin is set at 2.294),
Run! direction B (room_visuals/10_run_directions.md). Construction in
exit_sign_common.py, numbers in exit_sign_geom.py.

The double-face housing projects from the wall on its end: a 150 x 70 x 3
wall plate with two screws, a 127 x 50 x 12 stand-off canopy, then the
346 mm housing (Z 0.015-0.361). The faces look down +X and -X, so a corridor
reads the sign from both directions.

Origin: the WALL plane at the housing bottom (Y 0 = housing bottom, Z 0 =
the wall face, +Z = the room). Tags exit_sign + wall_decor -> Wall.
VARIANT Kit_ExitSign_End_Dead. Budget §4.3: 6,000 +-15 % / LOD1 1,900;
lodDistances [5, 0, 0].
"""

import exit_sign_common as es
import exit_sign_geom as g
import interact_key_common as kc
from interact_key_common import U

NAME = "Kit_ExitSign_End"
VARIANTS = {"Kit_ExitSign_End_Dead": {es.DIFFUSER: es.DIFFUSER_DEAD}}
BUDGET = (6000, 1900, 300)
LOD1 = BUDGET[1] / BUDGET[0]
SMOOTH_ANGLE = 40.0
MM = es.MM
PLATE = (70.0, 150.0, 3.0)        # wall plate X, Y, Z
STANDOFF = (50.0, 127.0, 12.0)    # stand-off canopy X, Y, Z


def build(kit):
    es.register_slots()
    yc = g.HOUSING_H / 2
    z0 = PLATE[2] + STANDOFF[2]                   # housing end on the wall side
    zc = z0 + g.HOUSING_W / 2                     # housing centre along Z
    xf = g.DEPTH_DOUBLE / 2
    # +X face: the reader looks along -X, so the reader's right is +Z.
    fp = es.Face((xf * MM, yc * MM, zc * MM), (1, 0, 0), (0, 0, 1), screw_axis="+X")
    fn = es.Face((-xf * MM, yc * MM, zc * MM), (-1, 0, 0), (0, 0, -1), mirror_lamps=True, screw_axis="-X")
    es.build_ring_double(kit, yc, xf, to_unity=lambda a, b, z: (z, yc + b, zc + a))
    es.build_face(kit, fp)
    es.build_face(kit, fn)
    # Lamp chassis (build round 3): between the two diffusers, inset 8 mm from the pan.
    xc_front = xf + es.chassis_front_c()
    es.build_chassis(kit, (0.0, yc * MM, zc * MM),
                     (2 * xc_front * MM, (g.HOUSING_H - 2 * es.CHASSIS_INSET) * MM, (g.HOUSING_W - 2 * es.CHASSIS_INSET) * MM))
    # Wall plate and the stand-off canopy (open toward the plate).
    kit.box(kc.U_size(PLATE[0] * MM, PLATE[1] * MM, PLATE[2] * MM), U(0.0, yc * MM, PLATE[2] / 2 * MM), es.HOUSING,
            bevel=0.0006, segments=2, name="mount plate")
    es.build_canopy(kit, STANDOFF[1], STANDOFF[0], STANDOFF[2], 4.0, name="stand-off",
                    to_unity=lambda a, b, d: (b, yc + a, z0 - d), out_U=(0, 0, -1))
    for y, deg in ((yc + 60.0, 30.0), (yc - 60.0, -50.0)):
        s = kc.oval_screw(kit, (0.0, y * MM, PLATE[2] * MM), es.CHROME, axis="+Z", head_d=0.0062, dome_h=0.0012,
                          slot_w=0.0007, slot_d=0.0008, slot_deg=deg, segs=12, name="plate screw")
        kit.lod1_drop(s)
        kc.drop2(s)

    kit.anchor("wall", U(0.0, 0.0, 0.0))
    kit.anchor("centre", U(0.0, yc * MM, zc * MM))
    z_plate = (xf - g.LIP_DEPTH) * MM
    kit.anchor("face_pos_x", U(z_plate, yc * MM, zc * MM))
    kit.anchor("face_neg_x", U(-z_plate, yc * MM, zc * MM))
    for nm, lamp in (("lamp_a", g.LAMP_A), ("lamp_b", g.LAMP_B)):
        a, b = g.lamp_reader_mm(lamp)
        kit.anchor(nm, U(0.0, (yc + b) * MM, (zc + a) * MM))
    kit.anchor("glow", U(0.0, -0.02, zc * MM))
    kit.no_collider()
    kit.tag("exit_sign", "wall_decor")
    es.exit_meta(kit, 2)
    kit.meta["lodBudget"] = list(BUDGET)
    es.check_budget(kit, BUDGET[0])
