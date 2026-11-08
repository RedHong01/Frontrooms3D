"""Kit_DoorFrame_StairNorth: the stairwell shell's north doorway frame, round the next floor's start door (stairwell doors,
2026-11-06; research/double_doors/30_build.md Part S). Render-only over FrontRoomsStairwell's three "Frame north ..." boxes,
filling the same 70 mm hole as Kit_DoorFrame_StairSouth, mirrored: the north door is pushed out of the stair, so its stair face
is the push side P (-X). No stop: in body-door mode the leaf may also swing back into the stair (FrontRoomsStairwell
.TickBodyDoor), so the soffit is flat, as a double-acting frame's, and the leaf's 4 mm gaps show its dark line. The corridor
side is flush with the shell's outer face (Stair_DoorFrame, every light: the next floor's corridor sees 60 mm of it inside the
end wall's opening); the frame halves of the butts on the hinge soffit (the knuckles are on the corridor side); the lever's
ANSI strike (satin chrome, its opening dark) on the latch soffit at 0.968 m; the fire label on the hinge soffit under the leaf.

Slots: Stair_DoorFrameInside, Stair_DoorFrame, Stair_Block, Stair_Hinge, Prop_ChromeSatin, Prop_PlasticBlack. LODs as the south
frame; the small parts LOD0. Switch 4 / 12 m, never culled.
"""

import sys

import kitlib

import interact_door_common as dc
import door_stair_common as sc

NAME = "Kit_DoorFrame_StairNorth"
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0

sc.register_slots(kitlib)


def build(kit):
    W = sc.NORTH_W
    sheets = {}
    for level in (0, 1, 2):
        o = sc.opening_sweep(kit, sc.shell_frame_section(-1, level), W, sc.FRAME_IN, "frame" if level == 0 else "frame lod%d" % level)
        sheets[level] = sc.tag(o, str(level))
    outer = sc.tag(sc.strip_u(kit, W, 0.0, sc.SHELL_HOLE, sc.SHELL_HALF, 1, sc.FRAME, "outer face (corridor)"), "012")
    infill = sc.tag(sc.strip_u(kit, W, sc.FACE, sc.SHELL_HOLE, -sc.SHELL_HALF, -1, sc.BLOCK, "block infill"), "012")
    halves = sc.frame_hinge_halves(kit, "0")
    label = sc.fire_label(kit, sc.FRAME_IN, 0.0, 1, -0.016, 0.016, sc.LABEL_YC, "0")
    # the lever's strike: a 32 x 124 mm ANSI strike mortised in the latch soffit (0.3 mm proud), its opening dark
    sw, sh = sc.STRIKE_W, sc.STRIKE_H
    strike = sc.tag(dc.obox(kit, sc.CHROME, lo=(-sw / 2, sc.LATCH_YC - sh / 2, W - 0.0003), hi=(sw / 2, sc.LATCH_YC + sh / 2, W + 0.0013),
                            bevel=0.0003, segs=1, name="strike"), "0")
    hole = sc.tag(dc.obox(kit, "Prop_PlasticBlack", lo=(-0.0063, sc.LATCH_YC - 0.0127, W - 0.0004), hi=(0.0063, sc.LATCH_YC + 0.0127, W + 0.0005),
                          name="strike opening"), "0")

    dc.finalize(kit, SMOOTH_ANGLE)

    for level in (0, 1, 2):
        lo, hi = dc.bounds_unity([sheets[level]])
        assert lo[0] >= -sc.SHELL_FACE_X - 1e-4 and hi[0] <= sc.SHELL_HALF + 1e-4, "x %s %s" % (lo[0], hi[0])
        assert lo[2] >= -sc.FACE - 1e-4 and hi[2] <= W + sc.FACE + 1e-4, "z %s %s" % (lo[2], hi[2])
    sc.assert_frame_clear_of_leaf([sheets[0], sheets[1], sheets[2], outer, infill] + halves + label + [strike, hole], W)
    kit.meta["frameEnvelope"] = {"stairFaceX": -sc.SHELL_FACE_X, "outerX": sc.SHELL_HALF, "hole": sc.SHELL_HOLE, "face": sc.FACE}
    dc.anchor(kit, "strike", 0.0, sc.LATCH_YC, W)
    sc.frame_meta(kit, sys.modules[__name__], W, ("shell", "north"))
