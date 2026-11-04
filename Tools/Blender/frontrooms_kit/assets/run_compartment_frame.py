"""A smoke-compartment frame across the Run! corridor (10 RN-2 "fire-door frames at 2.1 m every 3 cells (smoke
compartments), held open by magnets"; 71_run_room.md s.4.3: FD1 at 9 m is the receding variant's trigger plane, s.10:
"0.16 block on the cell line, a pair of 1.0 m leaves in a 2.0 x 2.1 frame, held open on magnets"). PRODUCTION kit
Kit_RunCompartmentFrame, hand-built LOD0/1/2: the cross wall and its steel frame. The leaves (Kit_DoorLeaf_SteelRun_DA,
almond, swung 115 deg until their edges meet the side walls) and their holders (Kit_RunMagnetHolder) are placed by
FrontRoomsRunDress, so the frame shares the corridor's door kits.

The cross wall: two block stubs (x +-1.0 .. +-1.42) and a header over the 2.0 x 2.1 m opening, 0.16 m thick on the cell
line, the corridor's two-tone finish and cove on both faces; a hollow-metal pair frame (brown, as the goal door's),
50 mm casings 11 mm proud on both faces, 0.18 deep, three butt hinges a jamb on the +Z face (the leaves swing toward the
goal, the way out). A new structure (rule 5b): only in a corridor asked for frames, never in today's game.
ORIGIN = the cell line on the floor at the axis. Budget 2,400 / 800 / 120; lodDistances 6 / 20 / 0.
Slots: RunB_BlockUpper, RunB_BlockLower, RunB_Cove, Prop_SteelBrown. No collider (the map owns collision).
"""

import _run_b_common as B
import _run_recede_common as R

NAME = "Kit_RunCompartmentFrame"
SMOOTH_ANGLE = 35.0
BUDGET = (2400, 800, 120)

UPPER = "RunB_BlockUpper"
LOWER = "RunB_BlockLower"
COVE = "RunB_Cove"
STEEL = "Prop_SteelBrown"

OPEN_HALF = 1.00
OPEN_TOP = 2.10
CASE = 0.050
PROUD = 0.011
DEPTH = 0.091


def _lod2(kit):
    """20 m and on: each stub and the header one block, the two-tone line, the casings as one box a jamb."""
    for s in (-1, 1):
        R.rbox(kit, s * OPEN_HALF, s * R.HW, 0.0, R.SOFFIT, -R.WALL_HALF, R.WALL_HALF, UPPER, name="stub")
        R.rbox(kit, s * (OPEN_HALF + CASE), s * R.HW, 0.0, R.TONE, -R.WALL_HALF - 0.002, R.WALL_HALF + 0.002, LOWER, name="stub lower tone")
        R.rbox(kit, s * (OPEN_HALF - 0.004), s * (OPEN_HALF + CASE), 0.0, OPEN_TOP + CASE, -DEPTH, DEPTH, STEEL, name="jamb")
    R.rbox(kit, -OPEN_HALF, OPEN_HALF, OPEN_TOP + CASE, R.SOFFIT, -R.WALL_HALF, R.WALL_HALF, UPPER, name="header")
    R.rbox(kit, -OPEN_HALF - CASE, OPEN_HALF + CASE, OPEN_TOP, OPEN_TOP + CASE, -DEPTH, DEPTH, STEEL, name="head")


def _level(kit, lv):
    if lv == 2:
        _lod2(kit)
        return
    t = 0.010
    for side in (-1, 1):
        z = side * R.WALL_HALF
        zs = (z - t, z) if side > 0 else (z, z + t)
        # stubs (full height) and the header, upper tone; the lower tone and the cove on the stubs (no bevels: the casings,
        # the walls and the soffit cover every skin edge)
        for s in (-1, 1):
            R.rbox(kit, s * OPEN_HALF, s * R.HW, 0.0, R.SOFFIT, zs[0], zs[1], UPPER, name="stub skin")
            lz = (z, z + 0.002) if side > 0 else (z - 0.002, z)
            R.rbox(kit, s * (OPEN_HALF + CASE), s * R.HW, 0.0, R.TONE, lz[0], lz[1], LOWER, name="stub lower tone")
            if lv < 2:
                cz = (z + 0.0015, z + 0.0045) if side > 0 else (z - 0.0045, z - 0.0015)
                R.rbox(kit, s * (OPEN_HALF + CASE), s * R.HW, 0.0, 0.102, cz[0], cz[1], COVE, name="stub cove")
        R.rbox(kit, -OPEN_HALF, OPEN_HALF, OPEN_TOP, R.SOFFIT, zs[0], zs[1], UPPER, name="header skin")
    bw, bs = R.hero(lv, 0.0012)
    # the pair frame: reveal plates, then the casings on both faces
    for s in (-1, 1):
        R.rbox(kit, s * (OPEN_HALF - 0.004), s * OPEN_HALF, 0.0, OPEN_TOP, -DEPTH, DEPTH, STEEL, bw, bs, name="jamb reveal")
    R.rbox(kit, -OPEN_HALF, OPEN_HALF, OPEN_TOP, OPEN_TOP + 0.004, -DEPTH, DEPTH, STEEL, bw, bs, name="head reveal")
    for side in (-1, 1):
        z0, z1 = side * R.WALL_HALF, side * (R.WALL_HALF + PROUD)
        for s in (-1, 1):
            R.rbox(kit, s * OPEN_HALF, s * (OPEN_HALF + CASE), 0.0, OPEN_TOP + CASE, z0, z1, STEEL, bw, bs, name="jamb casing")
        R.rbox(kit, -OPEN_HALF - CASE, OPEN_HALF + CASE, OPEN_TOP, OPEN_TOP + CASE, z0, z1, STEEL, bw, bs, name="head casing")
    if lv == 0:
        # three butt hinges a jamb on the +Z face, knuckles proud of the casing
        for s in (-1, 1):
            for y in (0.28, 1.05, 1.82):
                R.rcyl(kit, "y", 0.0075, 0.114, (s * (OPEN_HALF - 0.012), y, R.WALL_HALF + PROUD + 0.004), STEEL, 12, 0.0006, 1, name="hinge knuckle")
                R.rcyl(kit, "y", 0.0080, 0.006, (s * (OPEN_HALF - 0.012), y + 0.060, R.WALL_HALF + PROUD + 0.004), STEEL, 12, name="hinge tip")
        # the frame's anchor screws in the casings
        for side in (-1, 1):
            zf = side * (R.WALL_HALF + PROUD)
            for s in (-1, 1):
                for y in (0.4, 1.2, 2.0):
                    R.rcyl(kit, "z", 0.0042, 0.0012, (s * (OPEN_HALF + CASE * 0.5), y, zf + side * 0.0004), STEEL, 8, name="casing screw")


def build(kit):
    B.levels(kit, _level)
    kit.no_collider()
    for s, tag in ((-1, "l"), (1, "r")):
        kit.anchor("hinge_" + tag, R.U(s * (OPEN_HALF - 0.012), 0.0, R.WALL_HALF + PROUD + 0.004))
    kit.tag("run", "run_recede", "compartment_frame")
    B.lod_meta(kit, (6.0, 20.0, 0.0), BUDGET)
    kit.meta["runRecede"] = "card 2, 2026-11-04 (10 RN-2, 71_run_room.md s.4.3, s.10 FD1 / FD2); rule 5b: never in today's game"
