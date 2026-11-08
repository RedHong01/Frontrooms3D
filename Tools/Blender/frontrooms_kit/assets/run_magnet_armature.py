"""Kit_RunMagnetArmature: the armature plate on a held-open compartment leaf (Run! B FD1 / FD2), the half of the period
electromagnetic hold-open that sits on the door (zone doors run, Run! polish, 2026-11-06; Documentation/research/double_doors/
30_build.md Part R; research 10_research.md R.5, sources Z2 US 3,204,154 and Z3 US 4,852,919: "an armature plate attached to
the pull side of the door", swivel-mounted). Today the leaves rest against the bare magnet face of Kit_RunMagnetHolder.

A plated steel disc (64 mm, 6 mm, its contact face flat and ground, a counterbored rivet in the centre) on a swivel ball and a
10 mm stud with a hex boss, on a round door plate (44 mm, two slotted oval heads); a neoprene grommet behind the disc takes the
blow when the door swings onto the magnet. The swivel lets the disc lie flat on the wall magnet while the leaf stands 25 deg
off the wall (the compartment leaves are held at 115 deg): the disc is modelled at that tilt (TILT), its normal leaning toward
the leaf's latch edge.

PART FRAME (Unity metres): origin on the leaf's pull face at the armature's centre; +X out of the face (the stud); +Y up; +Z
along the leaf toward its latch edge. FrontRoomsRunDress hangs it under the leaf's pull-side closer root (scaled (pull, 1, 1)),
so the left leaf's armature is the mirror image. The disc's contact face stands 32.7 mm off the leaf face at the centre (the
dress sets the holder 0.3 mm clear of it).

LODs hand-built: LOD0 every part (plate, screws, boss, stud, ball, grommet, disc with its edge chamfers and rivet counterbore),
LOD1 plate, stud, ball and a plain disc, LOD2 a plate, a stud and an 8-sided disc. Budget 900 / 220 / 40; lodDistances
2 / 7 / 20 (the holder is 3 / 9 / 25). Slots Prop_Chrome (the plated parts; the dress turns it to satin chrome as all Run!
hardware) and Prop_Rubber (the grommet). No collider.
"""

import math
import random

import kitlib

import _run_b_common as B
import interact_door_common as dc

NAME = "Kit_RunMagnetArmature"
SMOOTH_ANGLE = 40.0
BUDGET = (900, 220, 40)

PLATED = "Prop_Chrome"
RUBBER = "Prop_Rubber"
kitlib.register_slot(RUBBER, (0.02, 0.02, 0.02), 0.85, 0.0)

TILT = 25.0                                   # the 115 deg hold-open: the leaf stands 25 deg off the wall
PLATE_R, PLATE_T = 0.022, 0.0028
BOSS_AF, BOSS_X1 = 0.017, 0.0078              # hex boss across flats, its front
STUD_R, STUD_X1 = 0.005, 0.0195
BALL_R, BALL_X = 0.0075, 0.0215
DISC_R, DISC_T, DISC_X = 0.032, 0.006, 0.030  # the disc's centre on the stud axis
RIVET_R, RIVET_D = 0.0068, 0.0005
GROMMET = (0.0080, 0.0150, 0.0030)            # inner r, outer r, thickness

N_DISC = (math.cos(math.radians(TILT)), 0.0, math.sin(math.radians(TILT)))


def _v(a, k):
    return (a[0] * k, a[1] * k, a[2] * k)


def _add(a, b):
    return (a[0] + b[0], a[1] + b[1], a[2] + b[2])


def _level(kit, lv):
    segs = B.segs(DISC_R, lv)
    small = max(8, B.segs(BOSS_AF, lv) // 2)
    # the door plate (0.2 mm into the leaf face: no slit), its edge eased at LOD0 / LOD1
    if lv < 2:
        dc.revolve(kit, [(0.0, -0.0002), (PLATE_R, -0.0002), (PLATE_R, PLATE_T * 0.45), (PLATE_R - 0.0016, PLATE_T), (0.0, PLATE_T)],
                   (0.0, 0.0, 0.0), (1, 0, 0), segs, PLATED, "door plate")
    else:
        dc.revolve(kit, [(0.0, -0.0002), (PLATE_R, -0.0002), (PLATE_R, PLATE_T), (0.0, PLATE_T)], (0.0, 0.0, 0.0), (1, 0, 0), 8,
                   PLATED, "door plate lod2")
    if lv == 0:
        rng = random.Random(61120)
        for y in (-0.0145, 0.0145):
            dc.oval_head(kit, (PLATE_T - 0.0002, y, 0.0), (1, 0, 0), PLATED, rng, d=0.0060, name="plate screw")
        # the hex boss the stud is swaged into (six sides, flats up and down)
        r_hex = BOSS_AF / 2.0 / math.cos(math.radians(30.0))
        dc.revolve(kit, [(0.0, PLATE_T - 0.0003), (r_hex, PLATE_T - 0.0003), (r_hex, BOSS_X1 - 0.0005), (r_hex - 0.0005, BOSS_X1), (0.0, BOSS_X1)],
                   (0.0, 0.0, 0.0), (1, 0, 0), 6, PLATED, "hex boss", phase=math.radians(30.0))
    # the stud (into the boss / plate, into the ball)
    x0 = BOSS_X1 - 0.0005 if lv == 0 else PLATE_T - 0.0003
    if lv < 2:
        dc.revolve(kit, [(STUD_R, x0), (STUD_R, STUD_X1 + 0.002)], (0.0, 0.0, 0.0), (1, 0, 0), small, PLATED, "stud",
                   close_start=True, close_end=True)
        # the swivel ball
        n = 6 if lv == 0 else 4
        prof = [(BALL_R * math.sin(math.pi * k / n), -BALL_R * math.cos(math.pi * k / n)) for k in range(n + 1)]
        prof[0] = (0.0, prof[0][1])
        prof[-1] = (0.0, prof[-1][1])
        dc.revolve(kit, prof, (BALL_X, 0.0, 0.0), (1, 0, 0), small, PLATED, "swivel ball")
    else:
        dc.obox(kit, PLATED, lo=(PLATE_T - 0.0003, -STUD_R, -STUD_R), hi=(DISC_X, STUD_R, STUD_R), bevel=0.0, segs=1, name="stud lod2")
    # the disc, tilted TILT about Y (its normal leans toward +Z, the latch edge): back face, chamfers, front face, counterbore
    centre = (DISC_X, 0.0, 0.0)
    back = _add(centre, _v(N_DISC, -DISC_T / 2.0))
    if lv == 0:
        prof = [(0.0, 0.0), (DISC_R - 0.0010, 0.0), (DISC_R, 0.0008), (DISC_R, DISC_T - 0.0008), (DISC_R - 0.0010, DISC_T),
                (RIVET_R, DISC_T), (RIVET_R, DISC_T - RIVET_D), (0.0, DISC_T - RIVET_D)]
        dc.revolve(kit, prof, back, N_DISC, segs, PLATED, "armature disc")
        # the neoprene grommet behind the disc, round the swivel
        gi, go, gt = GROMMET
        gb = _add(back, _v(N_DISC, -gt))
        dc.revolve(kit, [(gi, 0.0), (go, 0.0), (go, gt), (gi, gt), (gi, 0.0)], gb, N_DISC, small, RUBBER, "grommet",
                   close_start=False, close_end=False)
    elif lv == 1:
        dc.revolve(kit, [(0.0, 0.0), (DISC_R, 0.0), (DISC_R, DISC_T), (0.0, DISC_T)], back, N_DISC, segs, PLATED, "armature disc lod1")
    else:
        dc.revolve(kit, [(0.0, 0.0), (DISC_R, 0.0), (DISC_R, DISC_T), (0.0, DISC_T)], back, N_DISC, 8, PLATED, "armature disc lod2")


def build(kit):
    B.levels(kit, _level)
    # Envelope: the plate on the face, the disc's contact face 32.7 mm out at the centre, nothing behind the face but 0.2 mm.
    lo, hi = dc.bounds_unity([p for p in kit.parts if p.get("fr_lods") == "0"])
    assert lo[0] >= -0.00025, "behind the leaf face: %.5f" % lo[0]
    front = DISC_X + DISC_T / 2.0 * N_DISC[0]
    assert abs(front - 0.0327) < 0.0002, front
    kit.no_collider()
    dc.anchor(kit, "contact", front, 0.0, DISC_T / 2.0 * N_DISC[2])
    dc.anchor(kit, "mount", 0.0, 0.0, 0.0)
    kit.meta["tiltDeg"] = TILT
    kit.meta["contactCentre"] = [round(front, 5), 0.0, round(DISC_T / 2.0 * N_DISC[2], 5)]
    kit.meta["partFrame"] = ("origin on the leaf's pull face at the armature's centre; +X out of the face; +Y up; +Z along the leaf "
                             "toward its latch edge; the disc normal (cos 25, 0, sin 25)")
    kit.tag("run", "run_compartment", "door_hardware", "hold_open")
    B.lod_meta(kit, (2.0, 7.0, 20.0), BUDGET)
    kit.meta["runPolish"] = "zone doors Run! polish, 2026-11-06 (30_build.md Part R); rule 5b: never in today's game"
