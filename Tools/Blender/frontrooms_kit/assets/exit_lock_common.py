"""Shared numbers of the EXIT door's chain lock, R1 (游戏视觉, 2026-10-31; research/unlock/10_research.md §5,
20_design.md §2). NOT AN ASSET: no NAME and no build().

The four kits it feeds are the hardware of the c. 1990 after-hours chain on the stair EXIT's crossbar, built to
real part sizes so that every part fits the next one as on a real door:

* Kit_Padlock_Body     a 2 in laminated-steel padlock body: seven plates (the HUD padlock family's stylised count,
                       3113:6999; a real 1990 lock has more, thinner plates), four rivets top and bottom, the two
                       shackle bores and the cylinder bore. Keyway on the BOTTOM, as on every laminated padlock.
* Kit_Padlock_Shackle  the J shackle: 9.6 mm wire, legs 34 mm apart centre to centre (24 mm inside), 25 mm clear
                       height when shut; heel (long) leg with its locking notch, toe (short) leg with its notch.
* Kit_Padlock_Plug     the brass plug face with its keyway (it turns with the key).
* Kit_ChainStaple      the wall anchor: a 76 x 127 x 6 mm steel plate, four 3/8 in masonry anchors (hex nut and
                       washer), a 1/2 in round-bar staple welded to it, standing 38 mm out.

The chain itself (3/8 in grade 30 proof coil) is NOT a kit: its links follow a path that moves through the micro
cut scene, so FrontRoomsExitChainKit (C#) builds it from the same numbers (ChainWire, ChainLength, ChainWidth).

PART frames (Unity metres, dc.U converts to Blender): the padlock's three kits share one frame, origin = the centre
of the body's TOP face, +X across the 52 mm width (heel leg at -0.017, toe leg at +0.017), +Y up, +Z out of the
lock's face (the 26 mm depth runs Z -0.013..+0.013). The shackle kit's own origin is its HEEL leg axis on that top
face plane (it swivels about it); the plug kit's origin is the plug face's centre on the bottom face (it turns about
+Y, the key's axis). The staple kit: origin = the plate's back centre on the wall face, +Z out of the wall, +Y up.
"""

# ---------------------------------------------------------------- padlock (2 in laminated class; 10_research §2, §5)
BODY_W, BODY_H, BODY_D = 0.052, 0.046, 0.026     # main's (the HUD padlock family's) 52 x 46 x 26 mm
PLATES = 7                                       # the HUD family's 7 (stylised); a real lock: more, thinner plates
PLATE_GROOVE = 0.00035                           # each plate's edge chamfer: two of them make the groove between plates
BODY_CORNER_R = 0.003                            # plate outline corner radius
SHACKLE_WIRE_R = 0.0048                          # 9.6 mm shackle
SHACKLE_HALF = 0.017                             # legs 34 mm apart centre to centre
SHACKLE_CLEAR = 0.025                            # 25 mm clear height when shut (top face to the bow's inside)
SHACKLE_BOW_C = SHACKLE_CLEAR + SHACKLE_WIRE_R - SHACKLE_HALF   # the bow's centre height over the top face (0.0128)
HEEL_X, TOE_X = -SHACKLE_HALF, SHACKLE_HALF
HEEL_DEPTH = 0.030                               # the heel leg's end inside the body (hidden until the pop)
TOE_DEPTH = 0.009                                # the toe's engagement: 9 mm (the pop clears it by 2 mm)
POP_TRAVEL = 0.011                               # the shackle's travel at the release (10_research §6: 10-12 mm)
BORE_R = 0.0056                                  # the shackle bores (dark ring round each leg)
PLUG_R = 0.0065                                  # the brass plug face, 13 mm
PLUG_BORE_R = 0.0071                             # the cylinder bore round it (dark ring)
RIVET_X, RIVET_Z, RIVET_R, RIVET_H = 0.0215, 0.0075, 0.00275, 0.0013
KEYWAY_W, KEYWAY_L = 0.0020, 0.0080              # the keyway slot: blade 2 mm, along the lock's depth (Z)

# ---------------------------------------------------------------- chain (3/8 in grade 30 proof coil; 10_research §3)
CHAIN_WIRE = 0.0095
CHAIN_LENGTH = 0.054                             # link outside length
CHAIN_WIDTH = 0.034                              # link outside width

# ---------------------------------------------------------------- the wall anchor (10_research §5 table)
PLATE_W, PLATE_HT, PLATE_T = 0.076, 0.127, 0.006
ANCHOR_X, ANCHOR_Y = 0.024, 0.045                # the four masonry anchors, inset from the plate's edges
NUT_AF, NUT_H, WASHER_R, WASHER_T, STUD_R = 0.0143, 0.0083, 0.011, 0.002, 0.00475
STAPLE_R = 0.00635                               # 1/2 in round bar
STAPLE_OUT = 0.038                               # the staple's outer face 38 mm from the plate's face
STAPLE_HALF = 0.01885                            # legs 37.7 mm apart centre to centre: 25 mm clear between them
STAPLE_APEX_Z = PLATE_T + STAPLE_OUT - STAPLE_R  # the bow's centre line at its apex (0.03765), where the chain's link threads

# The fit rules a real lock obeys (asserted at import, so a number cannot drift into a part that would not close).
assert SHACKLE_HALF * 2 - SHACKLE_WIRE_R * 2 >= 2 * CHAIN_WIRE + 0.004, "two chain wires must pass the shackle's legs"
assert SHACKLE_CLEAR >= CHAIN_WIRE * 2, "the bow must take two links"
assert TOE_DEPTH + 0.002 <= POP_TRAVEL + 1e-9, "the pop must clear the toe"
assert STAPLE_HALF * 2 - STAPLE_R * 2 >= CHAIN_WIRE + 0.010, "a link must thread the staple"
assert STAPLE_APEX_Z - STAPLE_R - PLATE_T >= CHAIN_WIRE + 0.010, "a link must pass behind the staple's bow"
