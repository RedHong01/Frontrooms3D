"""The Run! receding end's block field at rest (71_run_room.md s.5.1 / s.5.3; card 2, the T2 body). PRODUCTION kit
Kit_RunRecede_Wall, one level (its slabs are 12 tris each at any distance).

The corridor's own two-tone block on both faces of the end, below the girder: the two bays beside the RN-F frame
(|x| 0.50-1.295 at rest: on under the RN-F frame's trim; 0.06 m on the sill to the lintel's top, 2.20 m) and the band over the lintel to the girder (2.20-2.53 m),
upper tone over the whole face, the lower tone a 2 mm overlay to 1.2 m (as the corridor's walls). Exactly the union of the
pieces' faces (Kit_RunRestack_Blocks), in the same object-space bond (_run_restack_common: front u = -x + 0.1, back
u = x + 0.3, v = y; the *_Piece twins, mesh UVs), so the swap at a step's tell, when the end comes apart, and back at its
seat, is exact. T1 (WebGL) shows only this. Split out of Kit_RunRecede_Head (head stage), which keeps the steel.

ORIGIN and frame as Kit_RunRecede_Head. Slots: RunB_BlockUpper_Piece, RunB_BlockLower_Piece. No collider.
"""

import _run_b_common as B
import _run_recede_common as R
import _run_restack_common as RS
import run_recede_head as H

NAME = "Kit_RunRecede_Wall"
SMOOTH_ANGLE = 35.0


def build(kit):
    t = 0.010
    for side, z in ((-1, -RS.FACE), (1, RS.FACE)):
        zs = (z, z + t) if side < 0 else (z - t, z)
        for (x0, x1, y0, y1) in RS.wall_skin_rects():
            H.bond_box(kit, x0, x1, y0, y1, zs[0], zs[1], RS.SLOT_UPPER, "upper skin")
        lz = (z - RS.LOWER_PROUD, z) if side < 0 else (z, z + RS.LOWER_PROUD)
        for (x0, x1, y0, y1) in RS.wall_skin_rects()[:2]:
            H.bond_box(kit, x0, x1, y0, RS.TONE, lz[0], lz[1], RS.SLOT_LOWER, "lower tone")
    kit.no_collider()
    kit.tag("run", "run_recede", "receding_end")
    kit.meta["lodDistances"] = [0.0]
    kit.meta["runRecede"] = "card 2 T2 body, 2026-11-04 (71_run_room.md s.5.3): the end's block field at rest; inert until rule 5b"
