"""The Run! receding end, its steel (71_run_room.md s.5.1; card 2, head first; T2 body 2026-11-04). PRODUCTION kit
Kit_RunRecede_Head, hand-built LOD0/1/2 (the Run! B convention, _run_b_common.levels).

The end is a block wall on a steel frame: brown hollow-metal edge channels (the goal door frame's paint, Prop_SteelBrown),
a steel sill over a 20 mm floor gap, a header over the internal girder with steel-lined slots for the corridor's services
(their brushes are Kit_RunRecede_HeadGear's), notched at both top corners for the tracks; the goal door's 1.0 x 2.1 m
opening (the map's RN-F door and its Steel DA frame sleeve it, as in any Run! end wall); the two armature brackets of the
hold-open magnets on the front corners; the carried lamp unit's arm (1.6 m) ending in the mount plate the rig hangs the
T-A twin-head on.

T2 body (card 2's T2 stage): the block field below the girder (0.06-2.53 m) is no longer in this kit. At rest it is
Kit_RunRecede_Wall (the skins), in a step Kit_RunRestack_Blocks (71 s.5.3: the wall comes apart and re-stacks round the
door). What stays here is the steel that rolls back whole: the edge channels, the sill, the header over the girder (its
block-faced fascia in the end's own bond: the *_Piece twin, mesh UVs, _run_restack_common), the slots, a 51 mm steel
lintel over the RN-F frame's head (2.149-2.200; the over-door courses bear on it) and the carried unit's arm, now a
gusseted cantilever from the header plate. The head stage's under-brace landed on the courses that come apart; a
60 x 40 x 3 tube carries the 12 kg unit 1.6 m out at about 26 MPa and 4 mm of sag, so the brace was never load-bearing.

ORIGIN = the head's line on the floor at the corridor's axis (the wall's centre plane). The wall faces Unity -Z (toward
the entrance); Blender authoring through _run_recede_common.U(). Budget 4,600 / 1,900 / 360; lodDistances 10 / 28 / 0.
Slots: RunB_BlockUpper_Piece, Prop_SteelBrown. No collider (the map's goal collider moves with the head).
"""

import _run_b_common as B
import _run_recede_common as R
import _run_restack_common as RS

NAME = "Kit_RunRecede_Head"
SMOOTH_ANGLE = 35.0
BUDGET = (4600, 1900, 360)

STEEL = "Prop_SteelBrown"
D = 0.085           # the edge channels' half depth (wraps the 0.16 wall)
F = 0.004           # steel thickness

B.kitlib.register_slot(RS.SLOT_UPPER, (0.70, 0.69, 0.62), 0.8)
B.kitlib.register_slot(RS.SLOT_LOWER, (0.27, 0.30, 0.26), 0.8)


def bond_box(kit, x0, x1, y0, y1, z0, z1, slot, name):
    """A skin box whose two big faces carry the end's own bond (metre UVs, _run_restack_common.face_uv: front u = -x + 0.1,
    back u = x + 0.3, v = y); its thin edges (under steel) take the nearer face's projection. 12 tris, no bevel. Faces are
    listed in Unity space and turned outward by flip_check (U() changes handedness)."""
    v = [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0), (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)]
    front = z0 + z1 < 0.0
    quads = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (3, 7, 6, 2), (0, 4, 7, 3), (1, 2, 6, 5)]
    faces, uvs = [], []
    for qi, q in enumerate(quads):
        faces.append(q)
        f = True if qi == 0 else (False if qi == 1 else front)
        for i in q:
            uvs.append(RS.face_uv(v[i][0], v[i][1], f))
    obj = B.mesh_part(kit, name, [R.U(*p) for p in v], faces, uvs, slot)
    return B.flip_check(obj)


def _skins(kit, lv):
    """The header's fascia over the girder (2.53-2.86) on both faces, in the end's bond; the block field below the girder
    is Kit_RunRecede_Wall's. No bevels: every skin edge is under a cap, a lining or a channel."""
    t = 0.010
    for side, z in ((-1, R.FRONT), (1, R.BACK)):
        zs = (z, z + t) if side < 0 else (z - t, z)
        rects = [(a, b, R.GIRDER, top) for (a, b, top) in R.header_spans()] if lv < 2 else [(-R.SKIN_IN, R.SKIN_IN, R.GIRDER, R.TOP)]
        for (x0, x1, y0, y1) in rects:
            bond_box(kit, x0, x1, y0, y1, zs[0], zs[1], RS.SLOT_UPPER, "header skin")


def _edges(kit, lv):
    """The hollow-metal edge channels, the sill, the lintel, the top caps, the slot and notch linings."""
    bw, bs = R.hero(lv, 0.0012)
    top = R.NOTCH[2]
    for s in (-1, 1):
        x_out, x_in = s * R.HALF, s * R.SKIN_IN
        R.rbox(kit, x_out, x_out - s * F, R.BOTTOM, top, -D, D, STEEL, bw, bs, name="edge web")
        if lv < 2:
            for zz in ((-D, -D + F), (D - F, D)):
                R.rbox(kit, x_in, x_out, R.BOTTOM, top, zz[0], zz[1], STEEL, bw, 1, name="edge flange")
        # The notch's floor and inner cheek (the track runs over it; the trolley's hanger stands in it).
        R.rbox(kit, s * R.NOTCH[0], x_out, top - F, top, -D, D, STEEL, bw, bs, name="notch floor")
        if lv < 2:
            R.rbox(kit, s * R.NOTCH[0], s * (R.NOTCH[0] - F), top, R.TOP, -D, D, STEEL, bw, 1, name="notch cheek")
        if lv == 0:
            # the girder's bearing plate where the corner hanger lands, under the trolley
            R.rbox(kit, s * R.TRACK_X - 0.05, s * R.TRACK_X + 0.05, top, top + 0.010, -0.06, 0.06, STEEL, 0.0015, 2, name="hanger bearing plate")
    # The sill (both sides of the door), over the 20 mm gap the bottom sweep closes.
    for s in (-1, 1):
        R.rbox(kit, s * R.DOOR_HALF, s * R.HALF, R.BOTTOM, R.SILL_TOP, -D, D, STEEL, bw, bs, name="sill")
    # The lintel over the RN-F frame's head: a 51 mm steel channel the over-door courses bear on (T2: they come apart and
    # land on it again), 4 mm proud of the block faces like the frame's own trim, bolted through.
    R.rbox(kit, -RS.JAMB, RS.JAMB, RS.LINTEL[0], RS.LINTEL[1], -0.084, 0.084, STEEL, bw, bs, name="lintel")
    if lv == 0:
        ym = (RS.LINTEL[0] + RS.LINTEL[1]) / 2.0
        for s in (-1, 1):
            for x in (-0.36, 0.0, 0.36):
                R.rcyl(kit, "z", 0.0055, 0.004, (x, ym, s * 0.086), STEEL, 10, 0.0006, 1, name="lintel bolt")
    # Header: caps over the full-height spans, linings round each slot (sides and bottom).
    for (x0, x1, top_y) in R.header_spans():
        if top_y >= R.TOP - 1e-6:
            R.rbox(kit, x0, x1, R.TOP - F, R.TOP + 0.002, -D, D, STEEL, bw, 1, name="header cap")
    for (_n, a, b, bottom) in R.SLOTS:
        R.rbox(kit, a, b, bottom - F, bottom, -D, D, STEEL, bw, 1, name="slot sill")
        if lv < 2:
            for x in (a, b):
                R.rbox(kit, x - F / 2, x + F / 2, bottom, R.TOP + 0.002, -D, D, STEEL, bw, 1, name="slot cheek")
    # The girder's soffit under the header (seen from below when the courses are away): a steel plate over the full width.
    R.rbox(kit, -R.SKIN_IN, R.SKIN_IN, R.GIRDER, R.GIRDER + F, -0.070, 0.070, STEEL, bw, 1, name="girder soffit")


def _armatures(kit, lv):
    """The hold-open magnets' armature brackets on both front corners (the holders are on the walls, Kit_RunMagnetHolder):
    an angle off the edge channel's front flange carrying a swivel armature disc that faces the wall."""
    bw, bs = R.hero(lv, 0.0012)
    for s in (-1, 1):
        x_out = s * R.HALF
        R.rbox(kit, x_out - s * 0.035, x_out, R.MAGNET_Y - 0.030, R.MAGNET_Y + 0.030, R.MAGNET_Z - 0.040, -D, STEEL, bw, bs, name="armature angle")
        if lv < 2:
            R.rcyl(kit, "x", 0.032, 0.006, (x_out + s * 0.004, R.MAGNET_Y, R.MAGNET_Z), STEEL, 28 if lv == 0 else 14,
                   0.0010 if lv == 0 else 0.0, 1, name="armature disc")
        if lv == 0:
            R.rcyl(kit, "x", 0.0065, 0.010, (x_out + s * 0.001, R.MAGNET_Y, R.MAGNET_Z), STEEL, 12, 0.0005, 1, name="armature stud")
            for dz in (-0.020, 0.020):
                R.rcyl(kit, "x", 0.0045, 0.003, (x_out - s * 0.0362, R.MAGNET_Y, R.MAGNET_Z + dz), STEEL, 10, 0.0, 1, name="angle bolt")


def _arm(kit, lv):
    """The carried unit's arm: a 60 x 40 mm tube cantilevered 1.6 m from a plate bolted through the header to the girder,
    a knee gusset under it at the plate (all above the girder's 2.53 m top: the courses below come apart in T2), and the
    vertical mount plate the twin-head cabinet is bolted to (its +Z face at ARM_Z), with its own gusset."""
    bw, bs = R.hero(lv, 0.0015)
    y0, y1 = R.ARM_Y
    zp = R.ARM_Z
    R.rbox(kit, -0.020, 0.020, y0, y1, zp - 0.008, R.FRONT - 0.008, STEEL, bw, bs, name="arm tube")
    R.rbox(kit, -0.080, 0.080, y0 - 0.050, y1 + 0.040, R.FRONT - 0.008, R.FRONT, STEEL, bw, bs, name="arm wall plate")
    p0, p1 = R.PLATE_Y
    R.rbox(kit, -0.200, 0.200, p0, p1, zp - 0.008, zp, STEEL, bw, bs, name="mount plate")
    if lv < 2:
        # knee gusset: a 6 mm plate under the tube, 0.16 m along it, its foot on the wall plate above the girder
        zw = R.FRONT - 0.008
        knee = [(zw, y0), (zw - 0.160, y0), (zw, y0 - 0.046)]
        verts = []
        for x in (-0.003, 0.003):
            verts += [R.U(x, yy, zz) for (zz, yy) in knee]
        faces = [(0, 1, 2), (5, 4, 3), (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0)]
        uvs = []
        for f in faces:
            uvs += [(0.0, 0.0)] * len(f)
        B.flip_check(B.mesh_part(kit, "arm knee gusset", verts, faces, uvs, STEEL))
        R.rbox(kit, -0.004, 0.004, y0 - 0.040, y0, zp, zp + 0.060, STEEL, bw, 1, name="plate gusset")
    if lv == 0:
        for (x, y) in ((-0.055, y0 - 0.030), (0.055, y0 - 0.030), (-0.055, y1 + 0.020), (0.055, y1 + 0.020)):
            kit.cylinder(0.0075, 0.006, R.U(x, y, R.FRONT - 0.011), STEEL, verts=6, rot=(90, 0, 0), bevel=0.0, segments=1, name="wall plate bolt")
        for (x, y) in ((-0.17, p0 + 0.03), (0.17, p0 + 0.03), (-0.17, p1 - 0.03), (0.17, p1 - 0.03)):
            kit.cylinder(0.0065, 0.005, R.U(x, y, zp - 0.0105), STEEL, verts=6, rot=(90, 0, 0), bevel=0.0, segments=1, name="plate bolt")


def _level(kit, lv):
    _skins(kit, lv)
    _edges(kit, lv)
    _armatures(kit, lv)
    _arm(kit, lv)


def build(kit):
    B.levels(kit, _level)
    kit.no_collider()
    # Anchors (Unity rig space relative to the head's line): where the rig and the dress hang things on the head.
    kit.anchor("door_hinge", R.U(-R.DOOR_HALF, 0.0, 0.0))
    kit.anchor("lamp_mount", R.U(0.0, R.PLATE_Y[0], R.ARM_Z))
    kit.anchor("goal_window_light", R.U(*R.C42))
    for s, tag in ((-1, "l"), (1, "r")):
        kit.anchor("armature_" + tag, R.U(s * (R.HALF + 0.007), R.MAGNET_Y, R.MAGNET_Z))
        kit.anchor("trolley_" + tag, R.U(s * R.TRACK_X, R.NOTCH[2], 0.0))
    kit.tag("run", "run_recede", "receding_end")
    B.lod_meta(kit, R.LOD_DISTANCES, BUDGET)
    kit.meta["runRecede"] = "card 2 head first, 2026-11-04 (71_run_room.md s.5.1); T2 body: block field split out, lintel, cantilever arm; inert until rule 5b"
