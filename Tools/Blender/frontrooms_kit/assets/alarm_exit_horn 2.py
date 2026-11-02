"""Kit_AlarmExit_Horn: the C16 alarm door's local horn over the head (游戏视觉, 2026-11-01).

A c. 1985-90 surface horn on a flush back box, in the alarm family's fire-red enamel (Alarm_BandRed #B01E18, the ALARM ZONE
tell's red): a square back plate, a square body with rounded front edges, a round projector with a rolled lip and seven
horizontal slats over the dark grille, four oval-head screws. Unbranded, no lettering, no strobe (battery exit alarms of
the period carry none: research/door_hardware/v7b_alarm_door/10_research.md section 4). The sound is the sound line's
(DeviceTripped, id "alarmdoor:(x, y)-(x, y)"); the horn never lights.

Origin: the WALL-FACE point at the back plate's centre; front = Unity +Z (kit -Y); +Y up; never mirrored. The runtime
hangs it centred over the opening on the owner zone's face, its bottom 2.180 m over the floor (clear of the F4 frame's head
at 2.149 and the wood frame's casing at 2.160), so its top (2.304) stays under the Low ceiling (2.4).
"""

import kitlib
import interact_door_common as dc
import alarm_exit_common as ax

NAME = "Kit_AlarmExit_Horn"
LOD1 = None
LODS = None
SMOOTH_ANGLE = 35.0

L = ax.lods


def lathe_z(kit, profile, centre, slot, verts, name, close_top=True, close_bottom=True):
    return kit.lathe(list(profile), dc.U(*centre), slot, verts=verts, rot=(90, 0, 0), name=name,
                     close_top=close_top, close_bottom=close_bottom)


def build(kit):
    import random
    ax.register_slots()
    rng = random.Random(4417)
    p, pt = ax.HORN_PLATE / 2, ax.HORN_PLATE_T
    b, d = ax.HORN_BODY / 2, ax.HORN_D
    body_front = 0.040
    r0, r1 = ax.BEZEL_R0, ax.BEZEL_R1

    # ------------------------------------------------------------ LOD0
    L(dc.obox(kit, ax.RED, lo=(-p, -p, 0.0), hi=(p, p, pt), bevel=0.0012, segs=2, name="back plate"), "0")
    L(dc.obox(kit, ax.RED, lo=(-b, -b, pt), hi=(b, b, body_front), bevel=ax.HORN_R, segs=5, name="horn body"), "0")
    # The projector: a drum with a rolled lip, its floor recessed 8 mm behind the lip.
    lip = [(0.0, body_front - 0.002), (r1, body_front - 0.002), (r1, d + 0.004), (r1 - 0.0007, d + 0.0068),
           (r1 - 0.0025, d + 0.0080), (r0 + 0.0020, d + 0.0080), (r0 + 0.0003, d + 0.0068), (r0, d + 0.0050),
           (r0, d), (0.0, d)]
    L(lathe_z(kit, lip, (0.0, 0.0, 0.0), ax.RED, 96, "projector"), "0")
    L(lathe_z(kit, [(0.0, d), (r0 + 0.0002, d), (r0 + 0.0002, d + 0.0003), (0.0, d + 0.0003)], (0.0, 0.0, 0.0),
              ax.GRILLE, 64, "grille"), "0")
    # Seven horizontal slats across the grille, rounded fronts, kept inside the lip.
    for k in range(-3, 4):
        y = k * ax.SLAT_PITCH
        edge = abs(y) + ax.SLAT_H / 2
        c = (r0 * r0 - edge * edge) ** 0.5 - 0.0006
        L(dc.obox(kit, ax.RED, lo=(-c, y - ax.SLAT_H / 2, d + 0.0003), hi=(c, y + ax.SLAT_H / 2, d + 0.0060),
                  bevel=0.0014, segs=2, name="slat"), "0")
    for sx in (-1, 1):
        for sy in (-1, 1):
            L(dc.oval_head(kit, (sx * 0.0445, sy * 0.0445, body_front), (0.0, 0.0, 1.0), ax.CHROME, rng, d=0.0062,
                           name="horn screw"), "0")

    # ------------------------------------------------------------ LOD1
    L(dc.obox(kit, ax.RED, lo=(-p, -p, 0.0), hi=(p, p, pt), name="back plate_1"), "1")
    L(dc.obox(kit, ax.RED, lo=(-b, -b, pt), hi=(b, b, body_front), bevel=0.004, segs=1, name="horn body_1"), "1")
    L(lathe_z(kit, [(0.0, body_front - 0.002), (r1, body_front - 0.002), (r1, d + 0.008), (r0, d + 0.008), (r0, d),
                    (0.0, d)], (0.0, 0.0, 0.0), ax.RED, 16, "projector_1"), "1")
    L(lathe_z(kit, [(0.0, d), (r0 + 0.0002, d), (r0 + 0.0002, d + 0.0003), (0.0, d + 0.0003)], (0.0, 0.0, 0.0),
              ax.GRILLE, 16, "grille_1"), "1")
    for k in (-2, 0, 2):
        y = k * ax.SLAT_PITCH
        edge = abs(y) + ax.SLAT_H / 2
        c = (r0 * r0 - edge * edge) ** 0.5 - 0.0006
        L(dc.obox(kit, ax.RED, lo=(-c, y - ax.SLAT_H / 2, d + 0.0003), hi=(c, y + ax.SLAT_H / 2, d + 0.0060),
                  name="slat_1"), "1")

    # ------------------------------------------------------------ LOD2
    L(dc.obox(kit, ax.RED, lo=(-p, -p, 0.0), hi=(p, p, body_front), name="horn_2"), "2")
    L(lathe_z(kit, [(0.0, body_front - 0.002), (r1, body_front - 0.002), (r1, d + 0.008), (0.0, d + 0.008)],
              (0.0, 0.0, 0.0), ax.RED, 8, "projector_2"), "2")

    kit.no_collider()
    kit.tag("wall_decor", "alarm_exit", "alarm_door")
    ax.lod_meta(kit, ax.HORN_LOD_DISTANCES, ax.HORN_BUDGET,
                "hand-built LOD0/1/2 (kitlib fr_lods, P-1b); lodDistances (d01, d12, dcull) m")
    kit.anchor("wall", dc.U(0.0, 0.0, 0.0))
    kit.anchor("grille_centre", dc.U(0.0, 0.0, d))
    kit.anchor("sound_point", dc.U(0.0, 0.0, d + 0.008))
    kit.meta["partFrame"] = "origin = wall-face point at the back plate centre; front = Unity +Z; +Y up; never mirrored"
    kit.meta["alarmExit"] = {"plate": ax.HORN_PLATE, "body": ax.HORN_BODY, "depth": d + 0.008, "bottomOverFloor": 2.180,
                             "source": "research/door_hardware/v7b_alarm_door/10_research.md"}
