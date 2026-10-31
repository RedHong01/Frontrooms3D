"""Fire-extinguisher cabinet, semi-recessed, with a clear glass door and a vertical red
FIRE EXTINGUISHER legend (US, 1975-1992). PRODUCTION kit for Run! B
(Kit_ExtinguisherCabinet), hand-built LOD0/1/2.

0.24 x 0.61 m trim, 70 mm proud, white baked enamel (#E4E2DA), a narrow door frame
holding clear glass, a black pull, and a 2-1/2 lb red ABC extinguisher inside (valve,
lever, carry handle, gauge, hose and nozzle, instruction label).
The stream walls are solid slabs, so the tub cannot recess into them: it is 68 mm deep
in front of the wall plane and the extinguisher (90 mm across) sits against its back,
its rear 5 mm hidden behind the back panel. From the room it reads as a recessed tub.

All white parts and the printed parts share one atlas (Run_PlateExtinguisher_A,
512 x 2048: left half the vertical legend, right half a plain white cell, the
extinguisher's instruction label and the gauge face), so the kit keeps 4 slots.
ORIGIN = the wall point under the trim's bottom centre (z = 0 = trim bottom).
FRONT = kit -Y (Unity +Z). Budget 5,500 / 2,200 / 400; lodDistances 2.5 / 8 / 30. No collider.
Slots: Run_PlateExtinguisher, Prop_Glass, RunB_FireRed, Prop_PlasticBlack.
"""

import _run_b_common as B

NAME = "Kit_ExtinguisherCabinet"
SMOOTH_ANGLE = 40.0
BUDGET = (5500, 2200, 400)

ATLAS = "Run_PlateExtinguisher"
GLASS = "Prop_Glass"
RED = "RunB_FireRed"
BLACK = "Prop_PlasticBlack"

TW, TH = 0.24, 0.61
PROUD = 0.070
WHITE_RECT = (0.56, 0.90, 0.94, 0.98)     # inside the plain white cell (right half, top)
LEGEND_RECT = (0.0, 0.0, 0.5, 1.0)
LABEL_RECT = (0.5, 0.625, 1.0, 0.875)
GAUGE_RECT = (0.5, 0.5, 1.0, 0.625)

EX_X, EX_Y, EX_R = 0.012, -0.040, 0.045
EX_Z0 = 0.060


def _white(obj):
    obj["fr_uv"] = "decal"
    obj["fr_decal_axes"] = "xz"
    obj["fr_uv_rect"] = list(WHITE_RECT)
    return obj


def _level(kit, lv):
    bw, bs = B.bev(0.0015, lv)
    # Tub: back panel just in front of the wall, the four walls, all white.
    _white(kit.box((TW - 0.016, 0.002, TH - 0.016), (0, -0.003, TH / 2), ATLAS, bevel=0.0, segments=0, name="tub back"))
    for sx in (-1, 1):
        _white(kit.box((0.0015, 0.064, TH - 0.016), (sx * (TW / 2 - 0.009), -0.034, TH / 2), ATLAS, bevel=0.0, segments=0, name="tub side"))
    for sz in (0, 1):
        _white(kit.box((TW - 0.016, 0.064, 0.0015), (0, -0.034, 0.008 + sz * (TH - 0.016)), ATLAS, bevel=0.0, segments=0, name="tub end"))
    # Trim: the rolled 8 mm face frame.
    _white(kit.frame((TW, TH), (TW - 0.016, TH - 0.016), 0.006, (0, -0.064, TH / 2), ATLAS, bevel=bw, segments=bs, name="trim"))
    # Door: a narrow frame round the glass, hinged on the left.
    door_w, door_h = TW - 0.020, TH - 0.020
    _white(kit.frame((door_w, door_h), (door_w - 0.024, door_h - 0.024), 0.008, (0, -PROUD + 0.004, TH / 2), ATLAS, bevel=bw, segments=bs, name="door frame"))
    kit.box((door_w - 0.020, 0.003, door_h - 0.020), (0, -PROUD + 0.006, TH / 2), GLASS, bevel=0.0, segments=0, name="door glass")
    # The vertical legend strip behind the glass on the left (red letters on white).
    leg = kit.quad(0.040, door_h - 0.040, (-door_w / 2 + 0.036, -PROUD + 0.0080, TH / 2), ATLAS, facing="-y", uv="decal", uv_rect=LEGEND_RECT, name="legend strip")
    # Pull on the right stile.
    B.box(kit, (0.010, 0.012, 0.070), (door_w / 2 - 0.006, -PROUD - 0.006, TH / 2), BLACK, w=0.002, lv=lv, name="door pull")
    if lv == 0:
        for z in (0.08, TH - 0.08):
            B.cyl(kit, 0.0035, 0.010, (-door_w / 2 + 0.002, -PROUD + 0.004, z), ATLAS, lv=lv, w=0.0006, name="hinge knuckle", lo=12)
            _white(kit.parts[-1])

    # The extinguisher.
    body_h = 0.300
    body = [(0.0, 0.0), (EX_R - 0.004, 0.0), (EX_R, 0.004), (EX_R, body_h - 0.020), (EX_R - 0.006, body_h - 0.006),
            (EX_R - 0.018, body_h + 0.004), (0.016, body_h + 0.010), (0.0, body_h + 0.010)]
    rings = None if lv == 0 else ([0, 1, 2, 3, 4, 5, 6, 7] if lv == 1 else [0, 2, 3, 5, 7])
    B.lathe(kit, body, (EX_X, EX_Y, EX_Z0), RED, lv=lv, name="extinguisher body", lo=36, rings=rings)
    # Instruction label wrapped on the front of the body (a slightly larger cylinder patch).
    if lv < 2:
        lab = kit.cylinder(EX_R + 0.0006, 0.150, (EX_X, EX_Y, EX_Z0 + 0.150), ATLAS, verts=B.segs(EX_R, lv, 36), bevel=0.0, segments=0, name="label band")
        lab["fr_uv"] = "decal"
        lab["fr_decal_axes"] = "xz"
        lab["fr_uv_rect"] = list(LABEL_RECT)
    # Valve: neck, head, discharge lever over the carry handle, pin.
    vz = EX_Z0 + body_h + 0.010
    B.cyl(kit, 0.012, 0.016, (EX_X, EX_Y, vz + 0.008), BLACK, lv=lv, w=0.0012, name="valve neck", lo=16)
    B.box(kit, (0.026, 0.030, 0.026), (EX_X, EX_Y, vz + 0.028), BLACK, w=0.003, lv=lv, name="valve head")
    if lv < 2:
        B.box(kit, (0.012, 0.090, 0.006), (EX_X, EX_Y + 0.020, vz + 0.046), BLACK, w=0.0015, lv=lv, rot=(-8, 0, 0), name="lever")
        B.box(kit, (0.012, 0.080, 0.006), (EX_X, EX_Y + 0.018, vz + 0.032), BLACK, w=0.0015, lv=lv, rot=(4, 0, 0), name="carry handle")
        # Gauge on the valve's front, its face printed.
        B.cyl(kit, 0.0120, 0.010, (EX_X - 0.004, EX_Y - 0.020, vz + 0.030), BLACK, lv=lv, rot=(90, 0, 0), w=0.001, name="gauge case", lo=16)
        kit.quad(0.020, 0.020, (EX_X - 0.004, EX_Y - 0.0253, vz + 0.030), ATLAS, facing="-y", uv="decal", uv_rect=GAUGE_RECT, name="gauge face")
        # Hose: from the valve's left down the side to the nozzle clipped on the body.
        pts = [(EX_X + 0.020, EX_Y - 0.010, vz + 0.024), (EX_X + 0.046, EX_Y - 0.020, vz + 0.000), (EX_X + 0.052, EX_Y - 0.022, vz - 0.080),
               (EX_X + 0.050, EX_Y - 0.020, vz - 0.170)]
        B.tube(kit, pts, 0.0065, BLACK, lv=lv, name="hose", lo=12)
        B.cyl(kit, 0.0080, 0.040, (EX_X + 0.050, EX_Y - 0.020, vz - 0.190), BLACK, lv=lv, w=0.001, name="nozzle", lo=12, radius_top=0.0055)
        B.box(kit, (0.012, 0.010, 0.012), (EX_X + 0.046, EX_Y - 0.014, vz - 0.150), BLACK, w=0.0012, lv=lv, name="hose clip")


def build(kit):
    B.levels(kit, _level)
    kit.no_collider()
    kit.anchor("centre", (0, -PROUD, TH / 2))
    kit.tag("wall_unit", "run", "run_b", "fire_gear")
    B.lod_meta(kit, (2.5, 8.0, 30.0), BUDGET)
