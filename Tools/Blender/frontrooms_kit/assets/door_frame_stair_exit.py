"""Kit_DoorFrame_StairExit: the C22 EXIT door's frame (stairwell doors, 2026-11-06; Documentation/research/double_doors/
30_build.md Part S): a 1990 welded 16 ga hollow-metal frame (SDI 111A, door_frame 10 section 1.2) on the map's own "EXIT frame
N-X" trim envelope (50.8 mm face, 11.1 mm proud of each wall face, 182.2 mm through), render-only over the trim (its renderer
hidden, its object kept: A13 and this dress hang on it). The trim was the period frame's face and return already; the kit adds
the profile: the 3 mm convex / 1.5 mm concave bends, the 1-9/16 in second rabbet on N's side, the 5/8 in stop on N's side (the
leaf swings into the stair: a stop must stand on its push side), the door rabbet behind it (118 mm deep, because the map hangs
the leaf on the wall's centre line, where a real frame's is 49 mm: stated), the faces mitred, welded and ground (no joint
line), three grey rubber silencers on the latch stop, the frame halves of the three butts at the dresser's knuckles, and the
fire label on the hinge soffit past the leaf, painted over. No strike: the crossbar's rim latch is drawn retracted
(Kit_ExitDevice_Crossbar), and a rim strike would stand where A13's chain runs to its staple.

Clearances (asserted): every vertex inside the trim's envelope (so the chain fall's casing box, x <= .105 and z .995-1.075 in
the shut frame, still holds the chain off it); nothing in the shut leaf's box; the stop 14 mm clear of the crossbar's latch
head; the EXIT sign 29 mm over the head (unchanged).

Slots: Stair_DoorFrame on N's side (every light, as the map's trim; #5F5A50 on PaintedMetal), Stair_DoorFrameInside on the stair
side (the deep rabbet, the stair-side face: the safe zone's own lights), silencers Prop_Rubber, butts Stair_Hinge.
The dresser turns its shadows off (C23: the EXIT frame and sign cast no shadow). LODs hand-built: LOD0 3-step bends + parts,
LOD1 1-step bends, LOD2 sharp. Switch 4 / 12 m, never culled.
"""

import sys

import kitlib

import interact_door_common as dc
import door_stair_common as sc

NAME = "Kit_DoorFrame_StairExit"
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0

sc.register_slots(kitlib)


def build(kit):
    W = sc.EXIT_W
    sheets = {}
    for level in (0, 1, 2):
        o = sc.opening_sweep(kit, sc.exit_frame_section(level), W, sc.FRAME, "frame" if level == 0 else "frame lod%d" % level)
        # the stair side (S, +X: the deep door rabbet, the stair-side face and return) takes the stair's own lights, as the leaf's
        # stair face and the shell's frames do (the safe zone: the map's lamps light through X's walls and dip with W1); N's side
        # keeps the every-light finish the map's trim had
        o.data.materials.append(kit._material(sc.FRAME_IN))
        for poly in o.data.polygons:
            if poly.center.x < 0.0:          # Blender -x = Unity +X
                poly.material_index = 1
        sheets[level] = sc.tag(o, str(level))
    sil = []
    for y in sc.SILENCER_Y:
        s = dc.revolve(kit, [(sc.SILENCER_R, 0.0), (sc.SILENCER_R, 0.0011), (sc.SILENCER_R * 0.72, sc.SILENCER_H - 0.0003),
                             (0.0011, sc.SILENCER_H - 0.0001), (0.0, sc.SILENCER_H - 0.0001)],
                       (sc.STOP_S_X - 0.0003, y, W - sc.STOP_H / 2 - sc.SOFFIT_IN), (1, 0, 0), 12, sc.RUBBER, "silencer")
        sil.append(sc.tag(s, "0"))
    halves = sc.frame_hinge_halves(kit, "0", sc.SOFFIT_IN)
    label = sc.fire_label(kit, sc.FRAME_IN, sc.SOFFIT_IN, 1, 0.045, 0.077, sc.LABEL_YC, "0")

    dc.finalize(kit, SMOOTH_ANGLE)

    fx = sc.EXIT_FACE_X
    for level in (0, 1, 2):
        lo, hi = dc.bounds_unity([sheets[level]])
        assert lo[0] >= -fx - 1e-4 and hi[0] <= fx + 1e-4, "x %s %s" % (lo[0], hi[0])
        assert lo[1] >= -1e-6 and hi[1] <= sc.HEAD + sc.FACE + 1e-4, "y %s %s" % (lo[1], hi[1])
        assert lo[2] >= -sc.FACE - 1e-4 and hi[2] <= W + sc.FACE + 1e-4, "z %s %s" % (lo[2], hi[2])
        # the stop stands no nearer the latch case than z 0.980 (the crossbar's latch head ends at 0.9702)
        mw = sheets[level].matrix_world
        for v in sheets[level].data.vertices:
            X, Y, Z = dc.to_unity(mw @ v.co)
            if X < -sc.HT and 0.05 < Z < W and Y < sc.HEAD:
                assert Z >= 0.980, "stop vertex at z %.4f near the latch case" % Z
    sc.assert_frame_clear_of_leaf([sheets[0], sheets[1], sheets[2]] + halves + label, W)
    lo, hi = dc.bounds_unity(sil)
    assert hi[0] <= -sc.HT - 0.00005, "silencers stop 0.05 mm off the leaf's P face (%.5f)" % hi[0]

    dc.anchor(kit, "stop_latch", sc.STOP_S_X, 1.0, W - sc.STOP_H / 2 - sc.SOFFIT_IN)
    for k, y in enumerate(sc.SILENCER_Y):
        dc.anchor(kit, "silencer_%d" % (k + 1), sc.STOP_S_X + sc.SILENCER_H, y, W - sc.STOP_H / 2 - sc.SOFFIT_IN)
    kit.meta["frameEnvelope"] = {"faceX": fx, "face": sc.FACE, "stopH": sc.STOP_H, "stopX": [sc.STOP_P_X, sc.STOP_S_X]}
    sc.frame_meta(kit, sys.modules[__name__], W, ("exit",))
