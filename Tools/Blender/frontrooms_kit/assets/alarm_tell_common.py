"""The ALARM ZONE tell (RELAY_PURSUIT_REDESIGN.md §6, row 10): shared numbers and slots.

Every trigger room carries one signature at every entrance: a ceiling detector on a red-banded base
(Kit_AlarmDetector) and an emissive ALARM ZONE card (Kit_AlarmZoneCard). Research and design:
Documentation/research/room_visuals/50_alarm_zone_tell.md; the card's art ask to 平面视觉:
51_alarm_zone_for_graphic.md. Runtime API: Assets/Scripts/Rendering/AlarmTell/FrontRoomsAlarmTell.cs.

Slots (Resources/Surfaces/<slot>.mat, FrontRooms/Surface; register_slot only sets preview colours,
kitlib.SLOTS is never edited):
  Alarm_Shell     ivory ABS head (period heads were off-white / buff, aged)       #DCD6C6
  Alarm_BandRed   fire-red enamel: the trim ring, the base, the card housing       #B01E18 (B's RunB_FireRed value)
  Alarm_Grille    the dark smoke-entry band behind the vent fins                     #1E1C1A
  Alarm_LED       the standby LED dome (emissive; FrontRoomsAlarmTellLed drives it)
  Alarm_CardFace  the card's translucent face (decal UV 0..1; STAND-IN art in Resources/Print/AlarmTell)
  Prop_Chrome     (existing) the card's four face-frame screws
All dimensions in millimetres unless named _M.
"""

import kitlib

SHELL = "Alarm_Shell"
RED = "Alarm_BandRed"
GRILLE = "Alarm_Grille"
LED = "Alarm_LED"
FACE = "Alarm_CardFace"
CHROME = "Prop_Chrome"

MM = 0.001

# ---------------------------------------------------------------- detector (System Sensor 1400 class: Ø140 x 80 mm with
# base; the Ingenium Simplex head 15.9 x 7.5 cm). Origin on the CEILING plane, the device hangs into -Z (Unity -Y).
RING_R = 100.0          # fire-red trim ring over the 4 in octagon box: Ø200 (red annulus seen from below 100 -> 78)
RING_T = 7.0            # ring drop under the ceiling
BASE_R = 78.0           # twist-lock base Ø156, red
BASE_BOTTOM = 22.0      # base underside, mm under the ceiling
HEAD_R = 64.5           # ivory head Ø129 at its widest
CORE_R = 58.0           # dark smoke-entry band radius
VENT_TOP, VENT_BOTTOM = 26.0, 44.0
FINS = 36
HEAD_BOTTOM = 71.5      # lowest point (the chamber cap), mm under the ceiling
LED_Z = 49.5            # LED centre, mm under the ceiling, on the front (-Y) side
LED_R = 2.4

DET_BUDGET = (8600, 1250, 320)           # as built 10-31: 8,590 / 1,210 / 314 (hero LOD0 within 3 m only)
DET_LOD_DISTANCES = (3.0, 9.0, 30.0)       # d01, d12, dcull (m, lodBias 1, FOV 76)

# ---------------------------------------------------------------- card (a 1990 lighted warning-sign box, the
# "IN USE" / X-ray-room construction: steel box, translucent acrylic face lit from inside). Origin = wall-face point at
# the housing's centre; the face looks down -Y (Unity +Z); Z up.
BOX_W, BOX_H, BOX_D = 286.0, 184.0, 51.0   # 11.25 x 7.25 x 2 in
FACE_W, FACE_H = 254.0, 152.0              # 10 x 6 in visible acrylic
FRAME_T = 4.0                              # face frame flange depth
FACE_INSET = 2.0                           # acrylic sits 2 mm behind the frame's front
SCREW_X, SCREW_Z = 135.0, 83.0

CARD_BUDGET = (750, 60, 14)               # as built 10-31: 734 / 46 / 14
CARD_LOD_DISTANCES = (5.0, 15.0, -1.0)     # never culled: the card is the read from down a corridor
CARD_CENTRE_M = 1.524                      # 60 in to the centreline, latch side (ADAAG 1991 4.30.6 room-sign rule)


def register_slots():
    """Preview colours only; the Unity look comes from Resources/Surfaces."""
    kitlib.register_slot(SHELL, (0.72, 0.68, 0.58), 0.55, 0.0)
    kitlib.register_slot(RED, (0.43, 0.012, 0.009), 0.45, 0.0)
    kitlib.register_slot(GRILLE, (0.012, 0.011, 0.010), 0.7, 0.0)
    kitlib.register_slot(LED, (0.6, 0.02, 0.015), 0.2, 0.0)
    kitlib.register_slot(FACE, (0.45, 0.03, 0.02), 0.3, 0.0)


def lods(obj, levels):
    """Hand-built LOD part sets (kitlib P-1b): levels is a string such as "0", "12" or "012"."""
    obj["fr_lods"] = levels
    return obj


def lod_meta(kit, distances, budget, note):
    kit.meta["lodDistances"] = [(-1.0 if d is None else float(d)) for d in distances]
    kit.meta["lodBudget"] = list(budget)
    kit.meta["lodHandBuilt"] = True
    kit.meta["lodNote"] = note


def tris_of(objs):
    return sum(len(p.vertices) - 2 for o in objs for p in o.data.polygons)
