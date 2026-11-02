"""The C16 alarm door (Relay v2 V7b) look: shared numbers and slots for its four kits (游戏视觉, 2026-11-01).

An armed alarm door is a double-acting map door (A12 / A19) dressed with a battery exit-alarm push bar on BOTH faces, a
fire-red local horn over the head and the placard. Research (period patents 1975-1992, no brands in game):
Documentation/research/door_hardware/v7b_alarm_door/10_research.md. Runtime: Assets/Scripts/Rendering/Doors/
FrontRoomsAlarmDoorLook.cs (its constants mirror the numbers below; change both together).

Kits (Assets/Resources/Props/Models, hand-built LOD0/1/2, kitlib P-1b):
  Kit_AlarmExit_Body     the static device on one face: the alarm housing at the latch end (cover on a base plate, the
                         arming key cylinder, the pilot LED, the mode push button, four cover screws), the C-channel rail
                         and the hinge-end cap. Slots Alarm_ExitAlu, Prop_ChromeSatin, Prop_PlasticBlack, Alarm_LED.
  Kit_AlarmExit_Pad      the touch pad (moves 12 mm along part -Z; 0.4 s of travel under Red's 10A hold). Slots
                         Alarm_ExitAlu, Prop_PlasticBlack.
  Kit_AlarmExit_Horn     the local horn over the head (wall mounted). Slots Alarm_BandRed, Alarm_Grille, Prop_ChromeSatin.
  Kit_AlarmExit_Placard  the placard slot: a 15 x 3 in plate whose face is one decal (STAND-IN art until 信息设计 lands
                         the face). Slots Alarm_BandRed, Alarm_DoorPlacard.

PART FRAME of Body and Pad (Unity metres): origin on the LEAF FACE, X = 0 at the housing's latch-side edge, +X toward the
hinge, Y = 0 on the pad's centreline, +Z out of the face. The runtime puts the origin at door (+-0.022, datum Y, 0.975)
with the face rotations of the lock parts (S: Euler(0, 90, 0); P: Euler(0, -90, 0) and scale (-1, 1, 1)), so the housing
is always at the latch end. Horn and Placard: origin = the back-face centre on the wall / leaf face, front +Z, never
mirrored (the placard's decal must read the right way round on both faces).
All dimensions are metres unless named _MM.
"""

import kitlib

ALU = "Alarm_ExitAlu"            # clear anodised aluminium, US28 / 628 satin (NEW surface, tiling grain maps)
CHROME = "Prop_ChromeSatin"      # US26D satin chrome: cylinder, LED bezel, screws
DARK = "Prop_PlasticBlack"       # gaps, keyway, push button, end caps, the channel's floor
LED = "Alarm_LED"                # the pilot LED dome (FrontRoomsAlarmTellLed drives it at run time)
RED = "Alarm_BandRed"            # fire-red enamel #B01E18: horn, placard edges
GRILLE = "Alarm_Grille"          # the horn's dark grille recess
FACE = "Alarm_DoorPlacard"       # the placard's printed face (decal UV 0..1, u to the viewer's right)


def register_slots():
    """Preview colours only; the Unity look comes from Resources/Surfaces/<slot>.mat."""
    kitlib.register_slot(ALU, (0.80, 0.80, 0.78), 0.40, 1.0)
    kitlib.register_slot(CHROME, (0.78, 0.78, 0.78), 0.30, 1.0)
    kitlib.register_slot(RED, (0.43, 0.012, 0.009), 0.45, 0.0)
    kitlib.register_slot(GRILLE, (0.012, 0.011, 0.010), 0.7, 0.0)
    kitlib.register_slot(LED, (0.6, 0.02, 0.015), 0.2, 0.0)
    kitlib.register_slot(FACE, (0.43, 0.012, 0.009), 0.5, 0.0)


def lods(obj, levels):
    """Hand-built LOD part sets (kitlib P-1b): levels is a string such as "0", "12" or "012"."""
    obj["fr_lods"] = levels
    return obj


def lod_meta(kit, distances, budget, note):
    kit.meta["lodDistances"] = [(-1.0 if d is None else float(d)) for d in distances]
    kit.meta["lodBudget"] = list(budget)
    kit.meta["lodHandBuilt"] = True
    kit.meta["lodNote"] = note


# ---------------------------------------------------------------- the device (Body + Pad), part frame
DEVICE_LEN = 0.857           # latch-side housing edge to the hinge-end cap's nose (door Z 0.975 -> 0.118)
HOUSING_W = 0.130            # along X
HOUSING_Y0, HOUSING_Y1 = -0.024, 0.126      # 150 mm tall, its bottom flush with the rail's bottom
HOUSING_D = 0.070            # depth off the leaf face
BASE_T = 0.004               # the base plate (back box flange) under the cover
BASE_LIP = 0.002             # the base plate stands 2 mm out round the cover
COVER_R = 0.009              # the cover's rounded edges
RAIL_X0, RAIL_X1 = 0.128, 0.829
RAIL_H = 0.048               # Y +-0.024
RAIL_D = 0.036               # the channel's flanges reach Z 0.036
RAIL_T = 0.002               # wall
CAP_X0, CAP_X1 = 0.829, 0.857
CAP_H, CAP_D = 0.051, 0.040
PAD_X0, PAD_X1 = 0.134, 0.823                 # 4 mm clear of the housing flank and the cap
PAD_H = 0.040                # Y +-0.020 inside the channel's +-0.022
PAD_Z0, PAD_Z1 = 0.030, 0.066                 # at rest; the back 6 mm sits inside the flanges
PAD_TRAVEL = 0.012           # pressed: Z 0.018 .. 0.054
PAD_FRONT_R = 0.008
PAD_CAP_T = 0.003            # black end caps on the pad's ends
CYL_X, CYL_Y = 0.065, 0.068  # the arming key cylinder on the cover front
LED_X, LED_Y = 0.040, 0.106  # the pilot LED (T-1 3/4, 5 mm) in a chrome bezel
BTN_X, BTN_Y = 0.090, 0.106  # the mode / test push button
SCREWS = ((0.016, -0.009), (0.114, -0.009), (0.016, 0.111), (0.114, 0.111))

BODY_LOD_DISTANCES = (2.0, 8.0, 40.0)       # d01, d12, dcull (m, lodBias 1, FOV 76); the lock kits switch at 1.5 / 4
PAD_LOD_DISTANCES = (2.0, 8.0, 40.0)
BODY_BUDGET = (5200, 420, 60)
PAD_BUDGET = (900, 120, 12)

# ---------------------------------------------------------------- horn (over the head, wall face frame)
HORN_PLATE = 0.124           # square back plate
HORN_PLATE_T = 0.004
HORN_BODY = 0.112            # square body
HORN_D = 0.050               # body front off the wall
HORN_R = 0.010               # body front edge round
BEZEL_R0, BEZEL_R1 = 0.040, 0.047
SLAT_PITCH, SLAT_H = 0.011, 0.0058
HORN_LOD_DISTANCES = (3.0, 10.0, 45.0)
HORN_BUDGET = (3600, 300, 40)

# ---------------------------------------------------------------- placard (leaf face frame)
PLACARD_W, PLACARD_H = 0.381, 0.076          # 15 x 3 in
PLACARD_T = 0.0016           # 1/16 in screen-printed aluminium, adhesive mounted
PLACARD_R = 0.004
PLACARD_LOD_DISTANCES = (3.0, 12.0, None)     # never culled: the placard is the read
PLACARD_BUDGET = (260, 14, 2)
