"""Manual fire-alarm pull station, single action, surface-mounted (US, 1975-1992).
PRODUCTION kit for Run! B (Kit_PullStation), hand-built LOD0/1/2.

130 x 180 x 60 mm red die-cast body (#B01E18) on a slightly larger back plate, the face
printed "FIRE" over the handle recess and "PULL DOWN" under it (white on red,
Run_PlatePull_A, TeX Gyre Heros Bold; through 平面视觉 before shipping), a T-handle
(#C42A20) in a dark recess, a chrome reset-key cylinder and two chrome face screws.
ORIGIN = the wall point under the body's bottom centre (back on y = 0, bottom z = 0).
FRONT = kit -Y (Unity +Z). Budget 2,400 / 900 / 150; lodDistances 2 / 6 / 25. No collider.
Slots: RunB_FireRed, RunB_FireHandle, Run_PlatePull, Prop_Chrome.
"""

import _run_b_common as B

NAME = "Kit_PullStation"
SMOOTH_ANGLE = 40.0
BUDGET = (2400, 900, 150)

RED = "RunB_FireRed"
HANDLE = "RunB_FireHandle"
FACE = "Run_PlatePull"
CHROME = "Prop_Chrome"

W, H, D = 0.130, 0.180, 0.060
PLATE_T = 0.006


def _level(kit, lv):
    # Back plate (the box cover on the wall) and the body.
    B.box(kit, (W + 0.008, PLATE_T, H + 0.008), (0, -PLATE_T / 2, H / 2), RED, w=0.0015, lv=lv, name="back plate")
    bw, bs = B.bev(0.004, lv)
    body_d = D - PLATE_T
    kit.box((W, body_d, H), (0, -PLATE_T - body_d / 2, H / 2), RED, bevel=bw, segments=bs, name="body")
    front = -D
    # Printed face: the whole front field, decal UVs (FIRE top, PULL DOWN bottom).
    kit.quad(W - 0.012, H - 0.012, (0, front - 0.0003, H / 2), FACE, facing="-y", uv="decal", name="face print")
    # Handle bezel: a raised cast frame round the handle slot (the print shows the dark slot).
    rz = 0.104
    if lv < 2:
        bw2, bs2 = B.bev(0.0008, lv)
        kit.frame((0.092, 0.036), (0.080, 0.024), 0.004, (0, front - 0.0020, rz), RED, bevel=bw2, segments=bs2, name="handle bezel")
    # T-handle: a round bar on a flat stem.
    if lv == 2:
        kit.box((0.072, 0.016, 0.016), (0, front - 0.012, rz + 0.004), HANDLE, bevel=0.0, segments=0, name="t handle")
    else:
        kit.lathe([(0.0, -0.036), (0.0055, -0.0358), (0.0075, -0.034), (0.0080, -0.030), (0.0080, 0.030), (0.0075, 0.034), (0.0055, 0.0358), (0.0, 0.036)],
                  (0, front - 0.016, rz + 0.006), HANDLE, verts=B.segs(0.008, lv, 16), rot=(0, 90, 0), name="t handle bar")
        B.box(kit, (0.016, 0.014, 0.020), (0, front - 0.008, rz - 0.004), HANDLE, w=0.002, lv=lv, name="t handle stem")
    # Reset key cylinder (low) and the two face screws.
    if lv < 2:
        B.cyl(kit, 0.0065, 0.004, (0, front - 0.0020, 0.030), CHROME, lv=lv, rot=(90, 0, 0), w=0.0008, name="key cylinder", lo=16)
        if lv == 0:
            for z in (0.012, H - 0.012):
                B.lathe(kit, [(0.0034, 0.0), (0.0033, 0.0008), (0.0024, 0.0016), (0.0, 0.0018)], (0, front + 0.0002, z), CHROME, lv=lv, rot=(90, 0, 0), name="face screw", lo=12)


def build(kit):
    B.levels(kit, _level)
    kit.no_collider()
    kit.anchor("centre", (0, -D, H / 2))
    kit.tag("wall_unit", "run", "run_b", "fire_alarm")
    B.lod_meta(kit, (2.0, 6.0, 25.0), BUDGET)
