"""Kit_DoorLeaf_StairNorth: the stairwell's north door, the next floor's start door (stairwell doors, 2026-11-06; research/
double_doors/30_build.md Part S; owed since 2026-10-22, LANDINGS: "the north door keeps the wooden latch until 游戏视觉's steel
look"): Kit_DoorLeaf_Stair's build at the map's 1.0 m (render-only over FrontRoomsStairwell's "Door leaf" cube), no lite (the
far door never had one: no sightline onto a floor not built yet), and the lever's bored latch: a satin-chrome faceplate and the
retracted bolt in the bevelled lock edge at the lever's 0.968 m.

Faces: P (-X) is the stair face (the door is pushed open out of X), so P takes Stair_DoorSteelInside (the safe zone) and S (+X,
the next floor's corridor, NOT AN EXIT) Stair_DoorSteel; edges Stair_DoorSteel. The faces' u is flipped, so the stair face reads
DoorWear_Stair as the EXIT's stair face does. LODs as Kit_DoorLeaf_Stair. Switch 5 / 15 m, never culled.
"""

import sys

import kitlib

import door_stair_common as sc

NAME = "Kit_DoorLeaf_StairNorth"
LOD_DISTANCES = (5.0, 15.0, None)
SMOOTH_ANGLE = 35.0

sc.register_slots(kitlib)


def build(kit):
    sc.build_leaf(kit, sys.modules[__name__], sc.NORTH_W, False, True, sc.STEEL_IN, sc.STEEL, sc.STEEL, 61109)
    kit.meta["stairDoor"] = "north door (the next floor's start door): P = the stairwell (pushed), S = the next floor's corridor"
