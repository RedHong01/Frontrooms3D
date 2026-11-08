"""Kit_DoorFrame_StairSouth: the stairwell shell's south doorway frame (stairwell doors, 2026-11-06; research/double_doors/
30_build.md Part S), round the sealed leaf and, until the seal, the EXIT door seen from the stair. Render-only over
FrontRoomsStairwell's three "Frame south ..." boxes (70 x 80 mm, 20 mm proud), which the shell's wall stops at: the kit fills
the same 70 mm hole. The stair face is the F4 2 in face 7/16 in proud (as the EXIT frame's), the soffit runs through the 60 mm
shell, the remaining 19.2 mm of the hole is block infill flush with the stair wall (Stair_Block, world-planar, so the block
coursing runs on; the dresser carries the band over it); the outer face is flush with the shell's outer face. Toward the EXIT
door the soffit runs on 18.9 mm to the plane of the EXIT frame's stair face and a 9.2 mm flange on each jamb covers the strip
of map wall paper the shell's wider opening (LeafRoom) left between them: from the stair the reveal reads as one deep steel
throat down to the EXIT door. The EXIT leaf's 95 deg swing stays 17 mm off the flanges (FrontRoomsStairwell.LeafRoom).

Slots: Stair_DoorFrameInside (the stair's own lights), Stair_Block. LODs: the sheet LOD0 3-step / LOD1 1-step / LOD2 sharp
bends; strips in every level. Switch 4 / 12 m, never culled.
"""

import sys

import kitlib

import interact_door_common as dc
import door_stair_common as sc

NAME = "Kit_DoorFrame_StairSouth"
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0

sc.register_slots(kitlib)


def build(kit):
    W = sc.SOUTH_W
    sheets = {}
    for level in (0, 1, 2):
        o = sc.opening_sweep(kit, sc.shell_frame_section(1, level), W, sc.FRAME_IN, "frame" if level == 0 else "frame lod%d" % level)
        sheets[level] = sc.tag(o, str(level))
    outer = sc.tag(sc.strip_u(kit, W, 0.0, sc.SHELL_HOLE, -sc.SHELL_HALF, -1, sc.FRAME_IN, "outer face"), "012")
    infill = sc.tag(sc.strip_u(kit, W, sc.FACE, sc.SHELL_HOLE, sc.SHELL_HALF, 1, sc.BLOCK, "block infill"), "012")
    soff = sc.tag(sc.soffit_strip(kit, W, -sc.BRIDGE_X, -sc.SHELL_HALF, sc.FRAME_IN, "soffit to the EXIT frame"), "012")
    flange = sc.tag(sc.jamb_flanges(kit, W, -sc.BRIDGE_X, sc.BRIDGE_U, 1, sc.FRAME_IN, "flange over the paper strip"), "012")

    dc.finalize(kit, SMOOTH_ANGLE)

    for level in (0, 1, 2):
        lo, hi = dc.bounds_unity([sheets[level]])
        assert lo[0] >= -sc.SHELL_HALF - 1e-4 and hi[0] <= sc.SHELL_FACE_X + 1e-4, "x %s %s" % (lo[0], hi[0])
        assert lo[2] >= -sc.FACE - 1e-4 and hi[2] <= W + sc.FACE + 1e-4, "z %s %s" % (lo[2], hi[2])
    sc.assert_frame_clear_of_leaf([sheets[0], sheets[1], sheets[2], outer, infill, soff, flange], W)
    kit.meta["frameEnvelope"] = {"stairFaceX": sc.SHELL_FACE_X, "outerX": -sc.SHELL_HALF, "hole": sc.SHELL_HOLE, "face": sc.FACE,
                                 "bridgeX": -sc.BRIDGE_X, "flangeU": sc.BRIDGE_U}
    sc.frame_meta(kit, sys.modules[__name__], W, ("shell",))
