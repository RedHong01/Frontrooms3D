"""Kit_DoorLeaf_Stair: the C22 EXIT door's leaf (stairwell doors, zone doors run, 2026-11-06; Documentation/research/
double_doors/30_build.md Part S): a 1990 hollow-metal stair door in the stairwell's putty enamel, render-only over the map's
cube leaf ("Door leaf", 0.05 x 2.08 x 0.98 m, its collider kept), hung by FrontRoomsStairwellDresser on the EXIT's "Leaf rig".

What it is: the map's 50 mm envelope (every A13 and stair anchor stays seated; door_stair_common), 4 mm gaps at the jambs, 3.2 mm
at the head, an 8 mm undercut; the formed 1/16 in arrises; a visible edge seam on both stiles; the lock edge bevelled 3 deg (the
push face P is the short side); the F13 cut-out for Kit_DoorLite_Exit (the dresser hangs the lite kit as today); the leaf halves
of the three butts mortised in the hinge edge at the dresser's knuckles; a fire label on the hinge edge, painted over.

Faces: P (-X, N's side, the maze) Stair_DoorSteel (every light), S (+X, the stair) Stair_DoorSteelInside (the stair's own lights,
the safe zone), edges Stair_DoorSteel; butts Stair_Hinge. Both faces read the wear mask DoorWear_Stair (u 0 at the hinge edge).
LODs hand-built: LOD0 2-segment arrises, seams, butts, label; LOD1 1-segment arrises; LOD2 sharp; all keep the cut-out and the
envelope (no pop at the switch). Switch 5 / 15 m, never culled.
"""

import sys

import kitlib

import door_stair_common as sc

NAME = "Kit_DoorLeaf_Stair"
LOD_DISTANCES = (5.0, 15.0, None)
SMOOTH_ANGLE = 35.0

sc.register_slots(kitlib)


def build(kit):
    sc.build_leaf(kit, sys.modules[__name__], sc.EXIT_W, True, False, sc.STEEL, sc.STEEL_IN, sc.STEEL, 61107)
    kit.meta["stairDoor"] = "EXIT (map edge N-X): P = N's side, S = the stairwell"
