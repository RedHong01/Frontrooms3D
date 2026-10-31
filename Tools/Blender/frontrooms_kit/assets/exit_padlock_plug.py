"""Kit_Padlock_Plug: the brass plug face of the EXIT chain's 2 in laminated padlock, with its keyway (游戏视觉,
unlock micro cut scene, 2026-10-31; research/unlock/10_research.md §2 + §6, 20_design.md §2.3).

How it works (10_research §6, U11 / U16): the key's blade lifts each pin stack to the shear line; the plug (and the
cam behind it) can then turn and draw the shackle's latches. The plug face is what the player sees turn with the key:
a 13 mm brass disc in the bottom plate's cylinder bore, its keyway slot along the lock's depth (front to back), so the
key goes in with its bow edge-on to the lock's face and turns it face-on.

PART frame: origin = the plug face's centre on the body's bottom face (Kit_Padlock_Body's (0, -0.046, 0)); +Y is the
plug's axis INTO the lock (the key goes in along +Y), +Z the lock's face direction, the keyway along Z.
FrontRoomsExitPadlock turns it about +Y with the key.

Geometry: the brass face (a 0.3 mm chamfer at its rim), 0.1 mm proud of the bottom plate, standing 2 mm into the bore;
the keyway is a dark warded slot (a straight 2 mm slot with two 0.7 mm wards, the paracentric read) 0.05 mm proud of
the brass. LOD0 24 sides, LOD1 12, LOD2 8 (no keyway). Slots: Prop_BrassSatin, Prop_SteelBlack. Render-only.
"""

import sys

import exit_lock_common as L
import interact_door_common as dc
import kitlib

NAME = "Kit_Padlock_Plug"
SMOOTH_ANGLE = 40.0
LOD_DISTANCES = (1.0, 3.0, None)
LOD1_RATIO = 0.5
LOD2_RATIO = 0.2

BRASS = "Prop_BrassSatin"
DARK = "Prop_SteelBlack"
kitlib.register_slot(BRASS, (0.66, 0.50, 0.27), 0.4, 1.0)


def face(kit, segs, lods):
    r = L.PLUG_R
    # (radius, height along +Y = into the lock); the face is at -0.0001 (0.1 mm proud of the bottom plate)
    prof = [(0.0, -0.0001), (r - 0.0003, -0.0001), (r, 0.0002), (r, 0.002)]
    obj = dc.revolve(kit, prof, (0.0, 0.0, 0.0), (0, 1, 0), segs, BRASS, "plug face", close_end=True)
    obj["fr_lods"] = lods
    return obj


def keyway(kit, lods):
    y0, y1 = -0.00016, -0.00010
    w, ln = L.KEYWAY_W, L.KEYWAY_L
    parts = [
        dc.obox(kit, DARK, lo=(-w / 2, y0, -ln / 2), hi=(w / 2, y1, ln / 2), name="keyway"),
        dc.obox(kit, DARK, lo=(w / 2 - 0.0002, y0, -ln / 2 + 0.0012), hi=(w / 2 + 0.0007, y1, -ln / 2 + 0.0032), name="keyway ward"),
        dc.obox(kit, DARK, lo=(-w / 2 - 0.0007, y0, ln / 2 - 0.0034), hi=(-w / 2 + 0.0002, y1, ln / 2 - 0.0014), name="keyway ward"),
    ]
    for p in parts:
        p["fr_lods"] = lods
    return parts


def build(kit):
    face(kit, 24, "0")
    face(kit, 12, "1")
    face(kit, 8, "2")
    keyway(kit, "01")
    dc.finalize(kit, SMOOTH_ANGLE, None)
    lo, hi = dc.bounds_unity(kit.parts)
    assert hi[0] - lo[0] < 2 * L.PLUG_BORE_R, "the plug fits its bore"
    dc.anchor(kit, "key_entry", 0.0, 0.0, 0.0)
    kit.meta["motion"] = {"type": "rig", "owner": "FrontRoomsExitPadlock", "turnAxis": "+Y", "turnDeg": 90,
                          "note": "the EXIT chain's padlock plug: turns with the key"}
    kit.no_collider()
    kit.tag("interactable", "exit", "padlock")
    dc.lod_meta(kit, sys.modules[__name__])
