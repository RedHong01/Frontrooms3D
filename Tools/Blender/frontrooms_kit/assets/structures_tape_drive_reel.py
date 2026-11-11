"""Kit_TapeDrive_Reel (structures_assets G3-a1 2026-11-10, P16 the SP3 computer room; geometry in _struct_l0_computer).
The 9-track tape unit (S1, IBM 3420-class) and its 10.5 in reels (S2). The unit's sidecar anchors reelFile / reelMachine are
the reel kits' origins (the back flange's back face on the axis): the builder places Kit_TapeReel_File / _Machine there and
turns them about their local Z for midTask; Kit_TapeDrive_Reel_Static has both reels baked (one renderer)."""

import _run_b_common as B
import _struct_common as S
import _struct_l0_computer as C

NAME = "Kit_TapeDrive_Reel"
SMOOTH_ANGLE = 40.0
BUDGET = (5000, 2000, 450)
VARIANTS = {"Kit_TapeDrive_Reel_Off": {"Prop_CompIndicator": "Prop_CompIndicator_Off"}} if "Reel_" not in NAME and "TapeReel" not in NAME else {}
LEVEL = C.tape_drive


def build(kit):
    B.levels(kit, LEVEL)
    C.collider_unity(kit, (0.0, C.TD_H / 2, 0.0), (C.TD_W, C.TD_H, C.TD_D))
    kit.min_ceiling = 0.0
    S.finish(kit, (5.0, 15.0, 60.0), BUDGET, tags=("structures", "p16", "computer_room", "wall_unit"))
    C.meta(kit, service=1.2)
