"""Kit_Padlock_Body: the body of the EXIT chain's 2 in laminated-steel padlock (游戏视觉, unlock micro cut scene,
2026-10-31; research/unlock/10_research.md §2 + §5, 20_design.md §2.3).

Real-world reference: the US laminated padlock (the stacked, riveted steel plates of US 1,490,987, 1924; US 4,098,100,
1978), the No. 5 size class: 2 in wide, about 1 3/4 in tall, 1 in deep, a 3/8 in shackle and a pin-tumbler plug
whose keyway is on the BOTTOM face. No maker's mark, no stamp (no brands in the game).

PART frame (exit_lock_common): origin = the centre of the TOP face; +X across the width (heel bore at -0.017, toe bore
at +0.017), +Y up (the body runs Y -0.046..0), +Z out of the lock's face. Unity spawns it under the padlock's pivot.

Geometry: seven plates (rounded-rectangle outline 52 x 26 mm, r 3 mm), each with a 0.35 mm edge chamfer top and
bottom, so a groove 0.7 mm wide shows between plates as on the real lock; four domed rivet heads on the top and on
the bottom face (they run through every plate); the two shackle bores on top and the cylinder bore under it are
thin dark rings (the shackle and the plug kits fill them); the bores read as holes when the shackle pops out.

LODs (hand-built, P-1b): LOD0 seven plates (corner 4 segs) + rivets + bores; LOD1 the plates with 2-seg corners, the
bores, no rivets; LOD2 one rounded block (no grooves). Switch (d01, d12) 1.5 / 5 m, never culled (the lock must read
at 30 m, A13 §4). Slots: Prop_SteelLaminated (submesh 0), Prop_SteelBlack (the bores). Render-only.
"""

import sys

import exit_lock_common as L
import interact_door_common as dc
import kitlib

NAME = "Kit_Padlock_Body"
SMOOTH_ANGLE = 40.0
LOD_DISTANCES = (1.5, 5.0, None)
LOD1_RATIO = 0.45
LOD2_RATIO = 0.04

STEEL = "Prop_SteelLaminated"
DARK = "Prop_SteelBlack"
kitlib.register_slot(STEEL, (0.604, 0.612, 0.592), 0.45, 1.0)   # #9A9C97, A13's laminated steel


def ring(inset, segs):
    """The plate outline pulled in by ``inset``, as (X, Z)."""
    return dc.rounded_rect(L.BODY_W - 2 * inset, L.BODY_D - 2 * inset, max(L.BODY_CORNER_R - inset, 0.0005), segs)


def plate(kit, i, segs, groove, lods):
    h = L.BODY_H / L.PLATES
    y0 = -L.BODY_H + i * h
    y1 = y0 + h
    g = L.PLATE_GROOVE if groove else 0.0
    if g > 0:
        rings = [(y0, ring(g, segs)), (y0 + g, ring(0.0, segs)), (y1 - g, ring(0.0, segs)), (y1, ring(g, segs))]
    else:
        rings = [(y0, ring(0.0, segs)), (y1, ring(0.0, segs))]
    obj = dc.loft_y(kit, rings, STEEL, "plate %d" % i)
    obj["fr_lods"] = lods
    return obj


def disc(kit, centre, normal, r_out, r_in, slot, segs, name, lods):
    """A flat annulus (r_in > 0) or disc 0.05 mm proud of a face, facing ``normal``."""
    prof = [(r_out, 0.0), (r_out, 0.00005)] + ([(r_in, 0.00005), (r_in, 0.0)] if r_in > 0 else [(0.0, 0.00005)])
    obj = dc.revolve(kit, prof, centre, normal, segs, slot, name, close_start=r_in <= 0, close_end=False)
    obj["fr_lods"] = lods
    return obj


def rivet(kit, x, z, top, lods):
    y = 0.0 if top else -L.BODY_H
    axis = (0, 1, 0) if top else (0, -1, 0)
    r, hh = L.RIVET_R, L.RIVET_H
    prof = [(r, 0.0), (r * 0.97, hh * 0.35), (r * 0.82, hh * 0.72), (r * 0.5, hh * 0.95), (0.0, hh)]
    obj = dc.revolve(kit, prof, (x, y, z), axis, 16, STEEL, "rivet", close_start=False)
    obj["fr_lods"] = lods
    return obj


def build(kit):
    # LOD0: seven grooved plates (steel first -> submesh 0)
    for i in range(L.PLATES):
        plate(kit, i, 4, True, "0")
    for top in (True, False):
        for sx in (-1, 1):
            for sz in (-1, 1):
                rivet(kit, sx * L.RIVET_X, sz * L.RIVET_Z, top, "0")
    # LOD1: the same plates with coarser corners, no rivets
    for i in range(L.PLATES):
        plate(kit, i, 2, True, "1")
    # LOD2: one block
    blk = dc.loft_y(kit, [(-L.BODY_H, ring(0.0, 1)), (0.0, ring(0.0, 1))], STEEL, "block")
    blk["fr_lods"] = "2"
    # the bores (dark rings): the shackle's two on top, the cylinder's underneath (the plug kit's face covers its middle)
    for x in (L.HEEL_X, L.TOE_X):
        disc(kit, (x, 0.0, 0.0), (0, 1, 0), L.BORE_R, 0.0, DARK, 20, "shackle bore", "01")
    disc(kit, (0.0, -L.BODY_H, 0.0), (0, -1, 0), L.PLUG_BORE_R, 0.0, DARK, 24, "cylinder bore", "012")

    dc.finalize(kit, SMOOTH_ANGLE, None)
    lo, hi = dc.bounds_unity(kit.parts)
    assert abs(hi[0] - lo[0] - L.BODY_W) < 1e-3 and abs(hi[2] - lo[2] - L.BODY_D) < 1e-3, "body 52 x 26 mm (%s %s)" % (lo, hi)
    dc.anchor(kit, "shackle_heel", L.HEEL_X, 0.0, 0.0)
    dc.anchor(kit, "shackle_toe", L.TOE_X, 0.0, 0.0)
    dc.anchor(kit, "keyway", 0.0, -L.BODY_H, 0.0)
    kit.meta["motion"] = {"type": "rig", "owner": "FrontRoomsExitPadlock", "parts": ["Kit_Padlock_Body", "Kit_Padlock_Shackle", "Kit_Padlock_Plug"],
                          "note": "the EXIT chain's padlock (unlock micro cut scene): the body drops on the shackle at the release"}
    kit.no_collider()
    kit.tag("interactable", "exit", "padlock")
    dc.lod_meta(kit, sys.modules[__name__])
