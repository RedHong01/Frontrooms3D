"""Kit_AlarmDetector: the ALARM ZONE tell's ceiling detector on its red-banded base (RELAY §6, row 10).

A 1985-93 commercial spot smoke detector of the plug-in head + twist-lock base kind (System Sensor 1400 / 2400 class,
Simplex 2098 class): an ivory head Ø129 with a slotted smoke-entry band, a domed cover with two shallow rings and a
raised chamber cap, a red standby LED on the side; the head twists into a base Ø156 that sits on a Ø200 trim ring over
the 4 in octagon box. Base and ring are fire-red enamel (#B01E18): the game's red family (red = ALARM ZONE) on the
period habit of painting fire-alarm boxes and covers red (NEC 760 identification, AHJ practice). Research:
Documentation/research/room_visuals/50_alarm_zone_tell.md.

Origin: the device's centre on the CEILING plane (Unity Y 0); it hangs into -Y; the LED faces Unity +Z (kit -Y), so
FrontRoomsAlarmTell turns that side toward the entrance. Tags "ceiling" -> placement Ceiling. No collider (it sits
2.83 m up). Hand-built LODs (kitlib P-1b, fr_lods):
  LOD0 hero (holds at 0.3 m): 64-sided lathes, 36 vent fins, LED with bezel, base lock tab;
  LOD1 24-sided, no fins (the band reads dark past 3 m: the fins are 2.4 mm), LED kept;
  LOD2 12-sided, three lathes and the LED.
"""

import math

import kitlib
import alarm_tell_common as at

NAME = "Kit_AlarmDetector"
LOD1 = None
LODS = None
SMOOTH_ANGLE = 40.0
BUDGET = at.DET_BUDGET
M = at.MM


def P(profile_mm):
    """(radius mm, depth under the ceiling mm) -> lathe profile (r m, z m), z negative."""
    return [(r * M, -d * M) for r, d in profile_mm]


def ring_profile(level):
    if level == 0:
        # bottom to top: inner edge under the base -> flat underside -> rolled outer edge -> ceiling
        return P([(74, 7.0), (92, 7.0), (96, 6.8), (98.6, 6.1), (99.6, 4.9), (100, 3.2), (100, 0.0)])
    if level == 1:
        return P([(74, 7.0), (96, 7.0), (99.4, 5.6), (100, 3.0), (100, 0.0)])
    return P([(74, 7.0), (99, 7.0), (100, 0.0)])


def base_profile(level):
    if level == 0:
        return P([(60, 22.0), (74.5, 22.0), (76.6, 21.6), (77.7, 20.5), (78.0, 18.8), (78.0, 10.0), (77.6, 8.2), (76.5, 7.0)])
    if level == 1:
        return P([(60, 22.0), (76.8, 21.8), (78.0, 19.5), (78.0, 9.0), (76.5, 7.0)])
    return P([(60, 22.0), (78.0, 21.0), (78.0, 7.5), (76.5, 7.0)])


def cover_profile(level):
    if level == 0:
        # centre of the chamber cap -> cap edge -> cover dome with two shallow rings -> rolled skirt -> under the vent band
        return P([(0, 71.5), (12, 71.3), (18.5, 70.8), (19.6, 70.2), (20.2, 69.2), (21.0, 68.4), (22.5, 68.1),
                  (33.4, 67.75), (33.9, 68.15), (35.1, 68.15), (35.6, 67.7), (41.4, 67.4), (41.9, 67.8), (43.1, 67.8),
                  (43.6, 67.3), (49.0, 66.7), (54.0, 65.6), (58.0, 63.9), (61.0, 61.6), (63.0, 58.6), (64.1, 55.0),
                  (64.5, 51.0), (64.5, 47.0), (64.2, 45.2), (63.4, 44.2), (61.0, 44.0), (57.0, 44.0)])
    if level == 1:
        return P([(0, 71.5), (20.0, 70.0), (22.0, 68.1), (49.0, 66.7), (58.0, 63.9), (63.0, 58.6), (64.5, 51.0),
                  (64.4, 45.0), (61.0, 44.0), (57.5, 44.0)])
    return P([(0, 71.0), (40.0, 67.5), (60.0, 63.0), (64.5, 52.0), (64.5, 44.0), (63.5, 22.0), (59.0, 22.0)])


def collar_profile(level):
    if level == 0:
        return P([(57.0, 26.0), (62.4, 26.0), (63.3, 25.5), (63.6, 24.4), (63.3, 22.6), (62.5, 22.0), (59.0, 22.0)])
    return P([(57.5, 26.0), (63.4, 25.6), (63.4, 22.4), (59.0, 22.0)])


def build(kit):
    at.register_slots()
    segs = {0: 64, 1: 24, 2: 12}

    for lv in (0, 1, 2):
        s = segs[lv]
        at.lods(kit.lathe(ring_profile(lv), (0, 0, 0), at.RED, verts=s, name="trim_ring_%d" % lv), str(lv))
        at.lods(kit.lathe(base_profile(lv), (0, 0, 0), at.RED, verts=s, name="base_%d" % lv), str(lv))
        at.lods(kit.lathe(cover_profile(lv), (0, 0, 0), at.SHELL, verts=s, name="cover_%d" % lv), str(lv))
        if lv < 2:
            at.lods(kit.lathe(collar_profile(lv), (0, 0, 0), at.SHELL, verts=s, name="collar_%d" % lv), str(lv))
            # the dark smoke-entry band behind the fins
            core = kit.cylinder(at.CORE_R * M, (at.VENT_BOTTOM - at.VENT_TOP + 1.0) * M,
                                (0, 0, -(at.VENT_TOP + at.VENT_BOTTOM) * 0.5 * M), at.GRILLE, verts=s, bevel=0.0,
                                name="vent_core_%d" % lv)
            at.lods(core, str(lv))

    # LOD0: 36 vent fins round the smoke-entry band (2.4 mm thick, radial 6.5 mm, 18 mm tall); the LED sits below the band
    zc = -(at.VENT_TOP + at.VENT_BOTTOM) * 0.5 * M
    hz = (at.VENT_BOTTOM - at.VENT_TOP) * M
    for i in range(at.FINS):
        a = 360.0 * (i + 0.5) / at.FINS
        r = (at.CORE_R + 3.0) * M
        x, y = r * math.cos(math.radians(a)), r * math.sin(math.radians(a))
        fin = kit.box((0.0065, 0.0024, hz), (x, y, zc), at.SHELL, bevel=0.0005, segments=1, rot=(0, 0, a), name="fin_%02d" % i)
        at.lods(fin, "0")

    # LED: a red dome in a small ivory bezel on the cover's skirt, kit -Y (Unity +Z)
    yl = -(at.HEAD_R + 0.2) * M
    zl = -at.LED_Z * M
    bezel = kit.cylinder(4.3 * M, 1.6 * M, (0, yl - 0.4 * M, zl), at.SHELL, verts=24, rot=(90, 0, 0), bevel=0.0006,
                         segments=2, name="led_bezel")
    at.lods(bezel, "0")
    dome = kit.cylinder(at.LED_R * M, 2.4 * M, (0, yl - 1.3 * M, zl), at.LED, verts=16, rot=(90, 0, 0), bevel=0.0011,
                        segments=3, name="led_dome")
    at.lods(dome, "0")
    dome1 = kit.cylinder(at.LED_R * M, 2.0 * M, (0, yl - 1.0 * M, zl), at.LED, verts=8, rot=(90, 0, 0), bevel=0.0,
                         name="led_dome_1")
    at.lods(dome1, "1")
    dome2 = kit.box((2 * at.LED_R * M, 1.6 * M, 2 * at.LED_R * M), (0, yl - 0.8 * M, zl), at.LED, bevel=0.0, name="led_dome_2")
    at.lods(dome2, "2")

    # LOD0 hero details: the base's twist-lock tab and the head's alignment rib (period heads carry both)
    tab = kit.box((0.010, 0.0035, 0.0055), (0.0, (at.BASE_R + 1.2) * M, -0.0155), at.RED, bevel=0.0007, segments=1,
                  rot=(0, 0, 0), name="base_tab")
    at.lods(tab, "0")
    rib = kit.box((0.0022, 0.0028, 0.0035), (0.0, -(63.6 + 0.9) * M, -0.0242), at.SHELL, bevel=0.0004, segments=1,
                  name="head_rib")
    at.lods(rib, "0")

    kit.no_collider()
    kit.tag("ceiling", "alarm_tell", "alarm_detector")
    kit.min_ceiling = 2.4
    at.lod_meta(kit, at.DET_LOD_DISTANCES, BUDGET,
                "hand-built LOD0/1/2 (kitlib fr_lods, P-1b); lodDistances (d01, d12, dcull) m at lodBias 1, FOV 76")
    kit.anchor("led", (0.0, yl - 2.5 * M, zl))
    kit.anchor("ceiling", (0.0, 0.0, 0.0))
    kit.meta["partFrame"] = "origin = device centre on the ceiling plane; hangs into Unity -Y; LED faces Unity +Z"
    kit.meta["alarmTell"] = {
        "ringDiameter": 2 * at.RING_R * M, "baseDiameter": 2 * at.BASE_R * M, "headDiameter": 2 * at.HEAD_R * M,
        "drop": at.HEAD_BOTTOM * M, "redAnnulusFromBelow": [2 * at.HEAD_R * M, 2 * at.RING_R * M],
        "ledDepthUnderCeiling": at.LED_Z * M, "slotLed": at.LED, "slotRed": at.RED,
    }
