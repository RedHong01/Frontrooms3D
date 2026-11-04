"""A wall-mounted electromagnetic hold-open holder (period fire-door hardware: a die-cast housing on a back plate, a round
steel face, a manual release button), for the Run! receding end's seat lines and the compartment frames' leaves
(71_run_room.md s.5.1 "hold-open magnets keep it seated; their release is every step's tell"; 10 RN-2 "held open by
magnets"). PRODUCTION kit Kit_RunMagnetHolder, hand-built LOD0/1/2. No maker marks.

70 mm deep, 90 mm square plate, almond enamel housing with a draft, bright steel face Dia 64 mm with its pole ring and
coil cap, a black release button on top. ORIGIN = the wall point behind the plate's centre; the face looks along +Z
(out of the wall). Budget 1,000 / 260 / 40; lodDistances 3 / 9 / 25.
Slots: Prop_SteelAlmond, Prop_Aluminium, Prop_PlasticBlack. No collider.
"""

import _run_b_common as B
import _run_recede_common as R

NAME = "Kit_RunMagnetHolder"
SMOOTH_ANGLE = 40.0
BUDGET = (1000, 260, 40)
ALMOND = "Prop_SteelAlmond"
FACE = "Prop_Aluminium"
DARK = "Prop_PlasticBlack"


def _level(kit, lv):
    bw, bs = R.hero(lv, 0.0012)
    R.rbox(kit, -0.045, 0.045, -0.045, 0.045, 0.0, 0.003, ALMOND, bw, 1, name="back plate")
    if lv == 2:
        R.rbox(kit, -0.040, 0.040, -0.040, 0.040, 0.003, 0.068, ALMOND, name="holder")
        return
    # the die-cast housing: drafted, a generous radius (loft_box: the front rectangle, Blender -Y = Unity +Z, is the smaller)
    kit.loft_box((0.072, 0.072), (0.080, 0.080), 0.055, R.U(0.0, 0.0, 0.0305), ALMOND,
                 bevel=0.006 if lv == 0 else 0.003, segments=3 if lv == 0 else 1, name="housing")
    R.rcyl(kit, "z", 0.032, 0.010, (0.0, 0.0, 0.063), FACE, 32 if lv == 0 else 16, 0.0010 if lv == 0 else 0.0, 1, name="magnet face")
    if lv == 0:
        R.rcyl(kit, "z", 0.024, 0.0010, (0.0, 0.0, 0.0681), DARK, 32, name="pole ring")
        R.rcyl(kit, "z", 0.013, 0.0012, (0.0, 0.0, 0.0682), FACE, 24, name="coil cap")
    R.rcyl(kit, "y", 0.006, 0.006, (0.0, 0.040, 0.034), DARK, 16 if lv == 0 else 8, 0.0008 if lv == 0 else 0.0, 1, name="release button")
    if lv == 0:
        for y in (-0.0425, 0.0425):
            R.rcyl(kit, "z", 0.0030, 0.0012, (0.0, y, 0.0035), ALMOND, 10, name="plate screw")


def build(kit):
    B.levels(kit, _level)
    kit.no_collider()
    kit.anchor("face", R.U(0.0, 0.0, 0.068))
    kit.tag("run", "run_recede", "wall_unit")
    kit.meta["service"] = 0.0
    B.lod_meta(kit, (3.0, 9.0, 25.0), BUDGET)
