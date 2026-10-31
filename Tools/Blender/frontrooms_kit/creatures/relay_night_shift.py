"""Relay "Night Shift": the production body (production_b/10_plan.md, H1-H3).

One skinned mesh in a neutral bind pose, a mirrored 36-bone armature and the pose keys the
runtime rig blends from CeilingHeight / DoorSqueeze (FrontRoomsRelayBody):
  low, std, tall, door_in, door_mid, door_out, listen_std   x   neck long / face +0.3 (faceLift)
Every key is the giant module's own pose (creatures/giant_b_night_shift.py POSE) re-targeted onto the
fixed-length skeleton, so the silhouettes the critics passed stay; door_mid / door_out are new.

Body geometry is built by the giant module itself (its build() in a "bind" parameter set, hood exponent
2.1, neck lengthened 0.20 m into the collar for the telescope). The low poly keeps the silhouette shells;
the seams, pleats, creases, placket and pocket flaps are baked into the normal and cavity maps (high -> low,
Cycles). The one part this file replaces in that build is the hood's front: hood_deep() draws an egg-shaped
snorkel opening and sets a ribbed glass deep behind it (Red 2026-10-30, 20_build.md "Lens rework").

  Blender -b --factory-startup --python Tools/Blender/frontrooms_kit/creatures/relay_night_shift.py -- \
      --out-root <unity project> --work <dir> [--quick] [--no-bake] [--previews]

Outputs (out-root/Assets/Resources): Creatures/Relay_NightShift.fbx + .json (kit sidecar),
Creatures/Relay_NightShift_Poses.json, Surfaces/Textures/Creature_{TwillSpruce,LensOpal,TapeSilver}_{A,N,S[,E]}.png.
Diagnostics (T5) are printed as "[relay_b] ..." lines and written to <work>/t5_envelope.json.
"""

import math
import os
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
KIT = os.path.dirname(HERE)
for p in (KIT, HERE):
    if p not in sys.path:
        sys.path.insert(0, p)

import bmesh  # noqa: E402
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Euler, Matrix, Vector  # noqa: E402

import creature_lib as cl  # noqa: E402
import creature_rig as cr  # noqa: E402
import giant_b_night_shift as gb  # noqa: E402
import kitlib  # noqa: E402

NAME = "Relay_NightShift"
TWILL, LENS, TAPE = "Creature_TwillSpruce", "Creature_LensOpal", "Creature_TapeSilver"
V = cr.V
LOG = []
PART_NAMES = []          # fr_part id -> part name (the low poly's int attribute indexes this)
PATCH_FRAME = {}         # the name patch's own frame before its transform is applied


def log(*a):
    s = " ".join(str(x) for x in a)
    LOG.append(s)
    print("[relay_b]", s)


KH, KG, KF, KC = gb.KH, gb.KG, gb.KF, gb.KC
THIGH, SHIN, UPPER, FORE = gb.THIGH, gb.SHIN, gb.UPPER, gb.FORE
HIP_HALF, SOCKET_HALF = gb.HIP_HALF, gb.SOCKET_HALF
HOOD_M = 2.1                     # superellipse exponent of the hood shell (module 2.4): no flat top, no box corner
# The lens rework (Red 2026-10-30, "keep B, change the lens"): it read as a phone. So no portrait card on the
# hood's face: a drawn snorkel opening, a dark sleeve of cloth, and a small ribbed glass deep inside it.
OPEN_A, OPEN_B = 0.120, 0.150    # opening half sizes, an egg 1.25 : 1 (was a 0.106 x 0.200 rounded rectangle)
LENS_APEX, LENS_RIM = 0.085, 0.115    # the glass's apex and rim behind the opening plane (was a flat panel 15 mm in)
DOME_A, DOME_B = 0.098, 0.118    # glass half sizes
DOME_UV = 0.62                   # the lens map is a disc: the glass inside this radius, the lit sleeve round it
HOT_X, HOT_Z = -0.012, -0.020    # the lamp behind the glass: off centre, and low, under the brim
NECK_TRIM_Y = 0.255              # the neck sleeve stops this far behind the opening plane, clear of the glass
LENS_UV = []                     # (u, v) per vertex of the lens part, in build order
HOOD_FRAME = {}                  # bind head frame (H, R), for the mouth mask in compose_twill
NECK_HIDDEN = 0.10               # neck sleeve carried on this far inside the collar (bind)
NECK_BIND_VISIBLE = 0.76         # modelled visible neck: std (0.95) is a 1.25 x stretch, short necks slide in
N1 = NECK_HIDDEN + NECK_BIND_VISIBLE / 3.0
N2 = N3 = NECK_BIND_VISIBLE / 3.0
STRETCH_MAX = 1.25
FACE_LIFT = 0.30

# ------------------------------------------------------------------ pose parameter sets (giant format)
# door-key wrists (world, door plane y = 0): every arm stays inside the 1.0 m opening (|x| <= 0.45)
# (T5 sweep, 10-22 runs e1-e6: e5 kept; door_in clean, door_mid <= 0.025 m, door_out <= 0.067 m at the start of its range)
DOOR_IN_L, DOOR_IN_R = (0.20, 0.25, 1.25), (0.20, -0.25, 1.45)
DOOR_MID_L, DOOR_MID_R = (0.25, 0.45, 1.25), (0.25, -0.75, 1.50)
DOOR_OUT_L, DOOR_OUT_R = (0.22, 0.22, 1.32), (0.22, -1.00, 1.35)
# the s range each door key is blended over (Unity FrontRoomsRelayBody), for the T5 sweep
DOOR_SWEEP = {"door_in": (-1.10, -0.20), "door_mid": (-0.55, 0.45), "door_out": (0.10, 0.95)}

STD_HUNT_R = dict(wrist=(-0.47, -0.56, 1.58), elbow=(-1.0, 0.25, 0.1),
                  hand=((-0.05, -0.25, -1.0), 0.55, (1.0, 0.3, 0.0), (0.0, -1.0, 0.0)))


def pose_params():
    import copy
    P = {}
    std = copy.deepcopy(gb.POSE["std"])
    listen = copy.deepcopy(gb.POSE["std"])          # the electrician feeling the ceiling: right glove on a tile
    std["wrist"]["r"] = STD_HUNT_R["wrist"]          # Hunt: it drops
    std["elbow"]["r"] = STD_HUNT_R["elbow"]
    std["hand"]["r"] = STD_HUNT_R["hand"]
    P["std"] = std
    P["listen_std"] = listen
    P["low"] = copy.deepcopy(gb.POSE["low"])
    P["tall"] = copy.deepcopy(gb.POSE["tall"])
    din = copy.deepcopy(gb.POSE["door"])
    din["hood"] = (14.0, -8.0, 15.0)                 # pitch >= 14 down, roll 5-9 (plan §1.2 rule 5)
    # arms inside the 1.0 m opening at every s the key is used (the module's glove round the far casing,
    # x 0.84, cut the jamb while the body was still behind it): the right hand leads through, the left trails
    din["wrist"] = dict(l=DOOR_IN_L, r=DOOR_IN_R)
    din["elbow"] = dict(l=(0.0, 1.0, -0.6), r=(0.0, -0.5, -0.8))     # elbows tucked down, inside the opening
    din["knee"] = dict(l=(0.3, 1.0, 0.2), r=(0.3, -1.0, 0.3))         # knees along the crossing, not across it
    din["hand"] = dict(l=din["hand"]["l"], r=((0.0, -1.0, 0.0), 0.8, (1.0, 0.0, 0.0), (0.0, 0.0, 1.0), 0.8))
    P["door_in"] = din
    P["door_mid"] = dict(
        world=True, pelvis=(0.0, 0.0, 0.80),
        spine=((12, -14), (16, -30), (20, -44), (46, -70)),
        feet=dict(l=((-0.04, 0.40), 92.0, 34.0), r=((0.06, -0.80), 50.0, 0.0)),
        knee=dict(l=(0.0, 1.0, -0.2), r=(0.0, -1.0, 0.3)),
        lens=(0.06, -0.98, 1.48), hood=(14.0, -8.0, 18.0),
        wrist=dict(l=DOOR_MID_L, r=DOOR_MID_R),    # both inside |x| 0.45: they pass the opening at any s (T5 sweep)
        elbow=dict(l=(0.0, 1.0, -0.5), r=(0.2, -0.4, 1.0)),
        hand=din["hand"], ceiling=None, xform=(80.0, -0.12, 0.0))
    P["door_out"] = dict(
        world=True, pelvis=(0.0, 0.0, 0.95),
        spine=((10, -10), (14, -22), (18, -32), (42, -48)),
        feet=dict(l=((-0.02, 0.10), 90.0, 46.0), r=((0.05, -1.02), 40.0, 0.0)),
        knee=dict(l=(0.0, 1.0, -0.6), r=(0.0, -1.0, 0.3)),
        lens=(0.02, -1.30, 1.74), hood=(16.0, -6.0, 10.0),
        wrist=dict(l=DOOR_OUT_L, r=DOOR_OUT_R),
        elbow=dict(l=(0.0, 0.6, -0.6), r=(0.0, -0.4, -0.6)),
        hand=din["hand"], ceiling=None, xform=(80.0, -0.15, -0.60))
    # face +0.3 (faceLift = 1): the lens 0.3 m higher in the pinned rooms; door and tall are lintel / reveal bound.
    for k in ("std", "listen_std", "low"):
        f = copy.deepcopy(P[k])
        f["lens"] = (f["lens"][0], f["lens"][1] + 0.06, f["lens"][2] + FACE_LIFT)
        P[k + "_f"] = f
    return P


def to_body(P):
    """giant build(): world-space door targets into the body frame (door plane y = 0)."""
    P = dict(P)
    if not P.get("world"):
        return P, 0.0, (0.0, 0.0)
    yaw0, tx, ty = P["xform"]
    Ri = Euler((0, 0, math.radians(-yaw0))).to_matrix()

    def Lp(p):
        return tuple(Ri @ (V(*p) - V(tx, ty, 0.0)))

    def Ld(v):
        return tuple(Ri @ V(*v))
    P["lens"] = Lp(P["lens"])
    P["hood"] = (P["hood"][0], P["hood"][1], P["hood"][2] - yaw0)
    P["wrist"] = {s: Lp(w) for s, w in P["wrist"].items()}
    P["elbow"] = {s: Ld(w) for s, w in P["elbow"].items()}
    P["knee"] = {s: Ld(w) for s, w in P["knee"].items()}
    P["hand"] = {s: (Ld(h[0]), h[1], Ld(h[2]), Ld(h[3])) + tuple(h[4:]) for s, h in P["hand"].items()}
    P["feet"] = {s: (Lp((f[0][0], f[0][1], 0.0))[:2], f[1] - yaw0, f[2]) for s, f in P["feet"].items()}
    return P, yaw0, (tx, ty)


# ------------------------------------------------------------------ bind pose
def make_bind():
    P = dict(spine=((4, 0), (11, 0), (19, 0), (36, 0)),
             feet=dict(l=((0.24, -0.12), 6.0, 0.0), r=((-0.24, -0.12), -6.0, 0.0)),
             knee=dict(l=(0.15, -1.0, 0.0), r=(-0.15, -1.0, 0.0)),
             hood=(14.0, 0.0, 0.0),
             elbow=dict(l=(0.25, 1.0, 0.0), r=(-0.25, 1.0, 0.0)),
             hand=dict(l=((0.0, -0.1, -1.0), 0.3, (-1.0, 0.0, 0.0), (0.0, -1.0, 0.0), 0.55, 1.0),
                       r=((0.0, -0.1, -1.0), 0.3, (1.0, 0.0, 0.0), (0.0, -1.0, 0.0), 0.55, 1.0)),
             ceiling=None, xform=None)
    # knees bent 15 deg: pelvis height from the hip-ankle distance
    want = math.sqrt(THIGH ** 2 + SHIN ** 2 + 2 * THIGH * SHIN * math.cos(math.radians(15)))
    fp = gb.foot_points(V(0.24, -0.12, 0.0), 6.0, 0.0)
    lo, hi = 1.0, 2.4
    for _ in range(60):
        z = (lo + hi) / 2
        pts = gb.spine_points(V(0, 0.08, z), P["spine"])
        F = gb.seg_frames(pts)
        hip = pts[0] + F[0][2] * HIP_HALF - F[0][4] * 0.07 + F[0][3] * 0.01
        if (hip - fp["ankle"]).length > want:
            hi = z
        else:
            lo = z
    P["pelvis"] = (0.0, 0.08, round((lo + hi) / 2, 4))
    pts = gb.spine_points(V(*P["pelvis"]), P["spine"])
    F = gb.seg_frames(pts)
    shoulders, shrug = pts[3], pts[4]
    fx_s, fy_s, fz_s = F[2][2], F[2][3], F[2][4]
    reach = math.sqrt(UPPER ** 2 + FORE ** 2 + 2 * UPPER * FORE * math.cos(math.radians(14)))
    P["wrist"] = {}
    for s, sx in (("l", 1), ("r", -1)):
        sock = shoulders + fx_s * (sx * SOCKET_HALF) + fy_s * 0.03 + fz_s * 0.02
        d = V(sx * math.sin(math.radians(35)), -0.08, -math.cos(math.radians(35))).normalized()
        P["wrist"][s] = tuple(sock + d * reach)
    # lens: neck visible arc NECK_BIND_VISIBLE, the neck going forward out of the shrug
    HR = gb.rot(*P["hood"])
    sdir = F[3][4]
    root = shrug - sdir * 0.06
    u = V(0.0, -0.86, 0.12).normalized()
    a, b = 0.2, 1.2
    for _ in range(60):
        L = (a + b) / 2
        end = root + u * L
        if neck_arc(root, sdir, end) > NECK_BIND_VISIBLE:
            b = L
        else:
            a = L
    end = root + u * ((a + b) / 2)
    H = end - HR @ V(0, 0.062 * KH + 0.08, 0.01 * KH + 0.05)
    P["lens"] = tuple(H)
    return P


def neck_arc(root, sdir, end, n=64):
    reach = (end - root).length
    ctrl = root + sdir * reach * 0.42
    pts = [gb.bezier(root, ctrl, end, i / n) for i in range(n + 1)]
    return sum((pts[i + 1] - pts[i]).length for i in range(n))


def neck_path(root, sdir, end, n=96):
    """Points and cumulative arc along the neck bezier (arc 0 at root)."""
    reach = (end - root).length
    ctrl = root + sdir * reach * 0.42
    pts = [gb.bezier(root, ctrl, end, i / n) for i in range(n + 1)]
    arc = [0.0]
    for i in range(n):
        arc.append(arc[-1] + (pts[i + 1] - pts[i]).length)
    return pts, arc, ctrl


def path_at(pts, arc, sdir, root, a):
    """Point at arc length a along the neck (a < 0: back along -sdir into the collar)."""
    if a <= 0:
        return root + sdir * a
    if a >= arc[-1]:
        t = (pts[-1] - pts[-2]).normalized()
        return pts[-1] + t * (a - arc[-1])
    for i in range(len(arc) - 1):
        if arc[i + 1] >= a:
            f = (a - arc[i]) / max(arc[i + 1] - arc[i], 1e-9)
            return pts[i].lerp(pts[i + 1], f)
    return pts[-1]


# ------------------------------------------------------------------ skeleton (body frame)
def hand_frame(P, s, elbow, wrist):
    hd, mix, palm, thumb_hint = P["hand"][s][:4]
    d = (wrist - elbow).normalized().lerp(V(*hd).normalized(), mix).normalized()
    pv = V(*palm)
    n = (pv - d * pv.dot(d)).normalized()
    sv = n.cross(d).normalized()
    thumb = sv if sv.dot(V(*thumb_hint)) > 0 else -sv
    return d, n, thumb


def skeleton(P0):
    """Bone (head, tail, ref) in the BODY frame for one pose, plus extras (lens, neck, hood)."""
    P, yaw, txy = to_body(P0)
    pts = gb.spine_points(V(*P["pelvis"]), P["spine"])
    pelvis, belly, chest, shoulders, shrug = pts
    F = gb.seg_frames(pts)
    fx_p, fy_p, fz_p = F[0][2], F[0][3], F[0][4]
    fx_s, fy_s, fz_s = F[2][2], F[2][3], F[2][4]
    J, X = {}, {"yaw": yaw, "txy": txy, "F": F}
    J["root"] = (V(0, 0, 0), V(0, 0, 0.25), V(1, 0, 0))
    J["pelvis"] = (pelvis, belly, F[0][2])
    J["spine_01"] = (belly, chest, F[1][2])
    J["spine_02"] = (chest, shoulders, F[2][2])
    J["chest"] = (shoulders, shrug, F[3][2])
    sdir = F[3][4]
    J["shrug"] = (shrug, shrug + sdir * 0.12, F[3][2])
    # hood and neck
    H = V(*P["lens"])
    HR = gb.rot(*P["hood"])
    hood_c = H + HR @ V(0, 0.062 * KH, 0.01 * KH)
    root = shrug - sdir * 0.06
    end = hood_c + HR @ V(0, 0.08, 0.05)
    npts, narc, ctrl = neck_path(root, sdir, end)
    Lv = narc[-1]
    third = NECK_BIND_VISIBLE / 3.0
    if Lv <= NECK_BIND_VISIBLE:                        # shorter: the rigid neck slides into the collar
        slide, stretch = NECK_BIND_VISIBLE - Lv, 1.0
        a3 = Lv
        a2 = a3 - third
        a1 = a2 - third
        a0 = a1 - N1
    else:                                              # longer: the whole visible sleeve stretches evenly
        slide = 0.0
        stretch = Lv / NECK_BIND_VISIBLE
        a0 = -NECK_HIDDEN
        a1 = third * stretch
        a2 = 2 * third * stretch
        a3 = Lv
    P_ = lambda a: path_at(npts, narc, sdir, root, a)  # noqa: E731
    J0, J1, J2, J3 = P_(a0), P_(a1), P_(a2), P_(a3)
    hx = HR @ V(1, 0, 0)
    r02 = (F[3][2] + hx).normalized() if (F[3][2] + hx).length > 1e-3 else hx
    J["neck_01"] = (J0, J1, F[3][2])
    J["neck_02"] = (J1, J2, r02)
    J["neck_03"] = (J2, J3, hx)
    J["head"] = (J3, J3 + HR @ V(0, 0, 0.20), hx)
    X.update(lens=H, HR=HR, hood_c=hood_c, neck_root=root, neck_end=end, neck_ctrl=ctrl, neck_len=Lv,
             slide=slide, stretch=stretch, sdir=sdir)
    # drawcords from the hem's lower corners
    for s, sx in (("L", 1), ("R", -1)):
        corner = H + HR @ V(sx * 0.07 * KH, -0.003 * KH, -0.135 * KH)
        start = corner + HR @ V(-sx * 0.002, -0.014, 0)
        p1 = start + HR @ V(sx * 0.008, -0.016, -0.05)
        p2 = p1 + V(sx * 0.006, -0.004, -0.16)
        p3 = p2 + V(sx * 0.004, 0.006, -0.15)
        J["cord_" + s] = (p1, p3, hx)
    # arms
    misses = {}
    for s, S, sx in (("l", "L", 1), ("r", "R", -1)):
        clav = shoulders + fx_s * (sx * 0.08) + fy_s * 0.03 + fz_s * 0.02
        sock = shoulders + fx_s * (sx * SOCKET_HALF) + fy_s * 0.03 + fz_s * 0.02
        wrist_t = V(*P["wrist"][s])
        pole = V(*P["elbow"][s])
        elbow = gb.ik(sock, wrist_t, UPPER, FORE, pole)
        wrist = elbow + (wrist_t - elbow).normalized() * FORE
        misses["wrist_" + s] = (wrist - wrist_t).length
        nrm = cr.bend_normal(sock, elbow, wrist, pole)
        d, n, thumb = hand_frame(P, s, elbow, wrist)
        mid = elbow.lerp(wrist, 0.5)
        J["clavicle_" + S] = (clav, sock, fz_s)
        J["upperarm_" + S] = (sock, elbow, nrm)
        J["forearm_" + S] = (elbow, mid, nrm)
        J["forearm_twist_" + S] = (mid, wrist, n)        # resolved to half the hand roll in pose_delta()
        knuck = wrist + d * 0.106 * KG
        tip = wrist + (d * 0.20 + n * 0.03) * KG
        J["hand_" + S] = (wrist, knuck, n)
        J["fingers_" + S] = (knuck, tip, n)
        t0 = wrist + (d * 0.035 + thumb * 0.032 + n * 0.010) * KG
        t2 = t0 + (d * 0.061 + thumb * 0.022 + n * 0.040) * KG
        J["thumb_" + S] = (t0, t2, n)
        X["hand_" + S] = (d, n, thumb)
    # legs
    for s, S, sx in (("l", "L", 1), ("r", "R", -1)):
        (bx, by), fyaw, lift = P["feet"][s]
        fp = gb.foot_points(V(bx, by, 0.0), fyaw, lift)
        hip = pelvis + fx_p * (sx * HIP_HALF) - fz_p * 0.07 + fy_p * 0.01
        pole = V(*P["knee"][s])
        knee = gb.ik(hip, fp["ankle"], THIGH, SHIN, pole)
        ankle = knee + (fp["ankle"] - knee).normalized() * SHIN
        misses["ankle_" + s] = (ankle - fp["ankle"]).length
        shift = ankle - fp["ankle"]
        nrm = cr.bend_normal(hip, knee, ankle, pole)
        lat = Euler((0, 0, math.radians(fyaw))).to_matrix() @ V(1, 0, 0)
        J["thigh_" + S] = (hip, knee, nrm)
        J["shin_" + S] = (knee, ankle, nrm)
        J["foot_" + S] = (ankle, fp["ball"] + shift, lat)
        J["toe_" + S] = (fp["ball"] + shift, fp["toe"] + shift, lat)
        X["ball_" + S] = fp["ball"] + shift
    X["misses"] = misses
    return J, X


# bone table: (name, parent, deform). Centre + left; the right side is mirrored.
BONES = [
    ("root", None, False),
    ("pelvis", "root", True), ("spine_01", "pelvis", True), ("spine_02", "spine_01", True),
    ("chest", "spine_02", True), ("shrug", "chest", True),
    ("neck_01", "shrug", True), ("neck_02", "neck_01", True), ("neck_03", "neck_02", True),
    ("head", "neck_03", True), ("cord_L", "head", True),
    ("clavicle_L", "spine_02", True), ("upperarm_L", "clavicle_L", True), ("forearm_L", "upperarm_L", True),
    ("forearm_twist_L", "forearm_L", True), ("hand_L", "forearm_twist_L", True),
    ("fingers_L", "hand_L", True), ("thumb_L", "hand_L", True),
    ("thigh_L", "pelvis", True), ("shin_L", "thigh_L", True), ("foot_L", "shin_L", True), ("toe_L", "foot_L", True),
    ("keyring", "pelvis", True), ("pouch", "pelvis", True),
]


def mirror_name(n):
    return n[:-2] + "_R" if n.endswith("_L") else n


def full_bone_list():
    out = []
    for n, p, d in BONES:
        out.append((n, p, d))
    for n, p, d in BONES:
        if n.endswith("_L"):
            out.append((mirror_name(n), mirror_name(p) if p else None, d))
    return out


# ------------------------------------------------------------------ the hood's front and the deep lens
def hood_outline(count, inset=0.0):
    """The drawn opening in the head frame's x-z plane (y = 0), counter-clockwise from +x seen from the front:
    an egg, full under the brim and drawn in at the chin, puckered by the cord. inset pulls it in along its rays."""
    pts = []
    e = 2.0 / 2.3
    for i in range(count):
        t = 2 * math.pi * i / count
        c, s = math.cos(t), math.sin(t)
        x = OPEN_A * math.copysign(abs(c) ** e, c) * (1.0 + 0.10 * s)
        z = OPEN_B * math.copysign(abs(s) ** e, s)
        if s > 0:
            z *= 1.0 - 0.10 * s ** 4                                  # the brim sags over the top
        pucker = 1.0 + 0.030 * math.cos(9 * t) + 0.014 * math.cos(14 * t + 2.1)
        r = math.hypot(x, z)
        q = pucker * (1.0 - inset / r)
        pts.append(V(x * q, 0.0, z * q))
    return pts


def lens_point(f, t, y):
    """A point on the glass: f 0..1 along the ray at angle t, at depth y."""
    c, s = math.cos(t), math.sin(t)
    return V(f * DOME_A * c * (1.0 + 0.08 * s), y, f * DOME_B * s)


def build_deep_lens(kit, W_org, R, org, n=27):
    """One part, slot LENS: the convex ribbed glass and the sleeve of lit cloth from its rim out to just behind the
    hem. Vertices are local to org (the module's lens origin, which the rig's lens locator and the floor shift read).
    UVs: a disc (DOME_UV is the glass's edge, 1 the hem), so glass and sleeve share one map with no seam."""
    bm = bmesh.new()
    del LENS_UV[:]

    def vert(p, rho, t):
        LENS_UV.append((0.5 + 0.5 * rho * math.cos(t), 0.5 + 0.5 * rho * math.sin(t)))
        return bm.verts.new(tuple(p - org))
    rise = LENS_RIM - LENS_APEX
    centre = vert(V(0, LENS_APEX, 0), 0.0, 0.0)
    rows = []
    for f in (0.36, 0.70, 1.0):
        y = LENS_RIM - rise * (1.0 - f * f)
        rows.append([vert(lens_point(f, 2 * math.pi * i / n, y), DOME_UV * f, 2 * math.pi * i / n) for i in range(n)])
    outer = hood_outline(n, 0.016)
    mid = hood_outline(n, 0.020)
    for i in range(n):                                                # the cord's nine pleats run on into the sleeve (3 verts each)
        o = mid[i]
        q = 1.0 + 0.055 * math.cos(9 * 2 * math.pi * i / n)
        mid[i] = V(o.x * q, 0.0, o.z * q)
    for rho, y, ring in ((0.82, 0.058, mid), (1.0, 0.012, outer)):
        rows.append([vert(V(ring[i].x, y, ring[i].z), rho, 2 * math.pi * i / n) for i in range(n)])
    faces = []
    for i in range(n):
        j = (i + 1) % n
        faces.append(bm.faces.new((centre, rows[0][i], rows[0][j])))
        for a, b in zip(rows, rows[1:]):
            faces.append(bm.faces.new((a[i], b[i], b[j], a[j])))
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    for face in faces:                                                # every face looks in, toward the opening's axis
        face.normal_update()
        cen = face.calc_center_median()
        inward = V(-cen.x, -1.0, -cen.z - org.z)
        if face.normal.dot(inward) < 0:
            face.normal_flip()
    obj = kit._new_object("opal lens", bm, LENS, "decal", "xz")
    kit._place(obj, W_org, gb.deg(R))
    return obj


def hood_deep(kit, H, R, half, lens, k, m=HOOD_M, n=48, shell_rings=12, band_rings=5, pleats=9):
    """The module's build_hood with its front replaced: the same shell, band, hem and parka peak, drawn in to
    hood_outline, and a sleeve running 0.14 m back into the hood instead of a floor 35 mm in."""
    cy, zc, y_rim = 0.062 * k, 0.01 * k, 0.015 * k
    puff, brim, pleat_amp = 0.008 * k, 0.014 * k, 0.007 * k
    hem_r, peak = 0.013 * k, 0.03 * k
    hx, hy, hz = half
    O = hood_outline(n)
    th = [math.atan2(o.z, o.x) for o in O]
    rad = [gb.se_radius(t, hx, hz, m) for t in th]
    C = V(0, cy, zc)

    def shell_pt(i, alpha):
        ca, sa = math.cos(alpha), math.sin(alpha)
        s = abs(sa) ** (2.0 / m)
        return V(rad[i] * math.cos(th[i]) * s, cy - hy * math.copysign(abs(ca) ** (2.0 / m), ca),
                 zc + rad[i] * math.sin(th[i]) * s)

    def deform(p):
        q = p - C
        fy, fz = q.y / hy, q.z / hz
        if fy > -0.3 and fz > 0:
            f = gb.smooth((fy + 0.3) * 0.9) * gb.smooth(fz * 1.2) * max(0.0, 1 - abs(q.x / hx)) ** 1.5
            q.y += peak * 0.7 * f
            q.z += peak * f
        if fy > 0 and fz < 0:
            q.z -= 0.04 * k * gb.smooth(fy) * gb.smooth(-fz)
        return C + q

    a_rim = math.acos(((cy - y_rim) / hy) ** (m / 2.0))
    rings = []
    for j in range(1, shell_rings + 1):
        alpha = math.pi - (math.pi - a_rim) * j / shell_rings
        rings.append([deform(shell_pt(i, alpha)) for i in range(n)])
    S = rings[-1]
    for j in range(1, band_rings + 1):
        g = 1 - (1 - j / band_rings) ** 1.6
        w = gb.smooth(g / 0.45) * gb.smooth((1 - g) / 0.25)
        ring = []
        for i in range(n):
            p = S[i].lerp(O[i], g)
            top = max(0.0, math.sin(th[i]))
            bulge = (puff + brim * top * top) * math.sin(math.pi * g) ** 0.8
            p.y = S[i].y * (1 - g) - bulge - pleat_amp * w * math.cos(2 * math.pi * pleats * i / n)
            ring.append(p)
        rings.append(ring)
    back = LENS_RIM + 0.014
    for d, yy in ((0.008, 0.012), (0.012, back)):            # the sleeve's outer wall (the part below lines it)
        rings.append([V(o.x * (1 - d / o.length), yy, o.z * (1 - d / o.length)) for o in O])

    def W(p):
        return H + R @ p
    bm = bmesh.new()
    pole = bm.verts.new(W(deform(V(0, cy + hy, zc))))
    vr = [[bm.verts.new(W(p)) for p in ring] for ring in rings]
    floor = bm.verts.new(W(V(0, back + 0.004, 0)))
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((pole, vr[0][j], vr[0][i]))
        for r0, r1 in zip(vr, vr[1:]):
            bm.faces.new((r0[i], r0[j], r1[j], r1[i]))
        bm.faces.new((vr[-1][i], vr[-1][j], floor))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    shell = kit._new_object("hood shell", bm, gb.TWILL, "metres", "xz")
    hn = 54                                                  # six samples a pleat
    hem_o = hood_outline(hn)
    pts, radii = [], []
    for j, o in enumerate(hem_o):
        wave = math.cos(2 * math.pi * pleats * j / hn)
        bunch = max(math.exp(-((o - V(sx * 0.085, 0, -0.125)).length / (0.03 * k)) ** 2) for sx in (1, -1))
        pts.append(W(V(o.x, -0.003 * k - 0.002 * k * wave, o.z)))
        radii.append(hem_r * (1.0 + 0.2 * wave + 0.3 * bunch))
    gb.loop_tube(kit, pts, radii, gb.TWILL, R @ V(0, -1, 0), verts=6, name="hood hem")
    org = V(0, 0.012 * k, -0.004 * k)
    lens_obj = build_deep_lens(kit, W(org), R, org)
    return {"lens": lens_obj, "shell": shell, "centre": W(C), "R": R, "H": H, "hem": pts}


def set_lens_uv(obj):
    me = obj.data
    if not me.uv_layers:
        me.uv_layers.new(name="UVMap")
    assert len(me.vertices) == len(LENS_UV), (len(me.vertices), len(LENS_UV))
    uv = me.uv_layers.active.data
    for poly in me.polygons:
        for li in poly.loop_indices:
            uv[li].uv = LENS_UV[me.loops[li].vertex_index]


# ------------------------------------------------------------------ body build
def build_bind_body(kit, bindP):
    gb.POSE["bind"] = bindP
    gb.LENS_GLOW = 0
    orig_hood = gb.build_hood
    gb.build_hood = hood_deep
    try:
        gb.build(kit, cl, "bind")
    finally:
        gb.build_hood = orig_hood
    bpy.context.view_layer.update()
    parts = list(kit.parts)
    # floor shift the module applied (lowest vertex to z = 0)
    lens = next(o for o in parts if o.name.startswith("opal lens"))
    J, X = skeleton(bindP)
    shift = (lens.matrix_world @ V(0, 0, 0)).z - X["lens"].z
    log("bind: module floor shift %.4f m (lens origin vs skeleton)" % shift)
    return parts, shift


def finish_part(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    for mod in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)


def tris(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


# part name prefix -> (category, keep in the low poly, LOD0 tri target)
PART_RULES = [
    ("coverall torso", "cloth", True, 2700), ("coverall legs", "cloth", True, 1450),
    ("coverall arms", "cloth", True, 1450), ("hood shell", "cloth", True, 1800),
    ("hood hem", "cloth", True, 648), ("hood neck", "neck", True, 760), ("collar", "cloth", True, 260),
    ("glove", "glove", True, 450), ("boot", "boot", True, 380), ("tape", "tape", True, 110),
    ("tool belt", "belt", True, 260), ("pouch flap", "belt", True, 100), ("pouch", "belt", True, 200),
    ("tool handle", "belt", True, 40), ("key clip", "steel", True, 12), ("key ring", "brass", True, 64),
    ("key ", "brass", True, 14), ("drawcord", "cord", True, 64), ("aglet", "steel", True, 32),
    ("name patch", "patch", True, 64), ("opal lens", "lens", True, 400),
    ("hood seam", "cloth", False, 0), ("neck seam", "neck", False, 0), ("back yoke seam", "cloth", False, 0),
    ("action pleat", "cloth", False, 0), ("strain crease", "cloth", False, 0), ("zip placket", "cloth", False, 0),
    ("pocket flap", "cloth", False, 0),
]
CATS = ["cloth", "neck", "glove", "boot", "belt", "cord", "tape", "patch", "brass", "steel", "lens"]
CAT_SLOT = {"cloth": TWILL, "neck": TWILL, "glove": TWILL, "boot": TWILL, "belt": TWILL, "cord": TWILL,
            "tape": TAPE, "patch": TAPE, "brass": TAPE, "steel": TAPE, "lens": LENS}
UV_DENSITY = {"cloth": 1.0, "neck": 1.35, "glove": 1.35, "boot": 0.8, "belt": 1.0, "cord": 1.0}


def rule(name):
    for pre, cat, keep, tgt in PART_RULES:
        if name.startswith(pre):
            return cat, keep, tgt
    return "cloth", True, 200


NECK_RX = (0.19, 0.115)     # neck sleeve half-width root -> hood: 0.38 -> 0.23 m wide (plan §1.2 rule 6)
NECK_RY = (0.16, 0.100)     # half-depth


def neck_radius(t):
    f = gb.smooth(t * 1.15)
    return (NECK_RX[0] + (NECK_RX[1] - NECK_RX[0]) * f, NECK_RY[0] + (NECK_RY[1] - NECK_RY[0]) * f)


def rebuild_neck(kit, parts, X):
    """Replace the module's neck sleeve (0.76 -> 0.30 m: it read as a dome over the hood once the
    body is skinned) by the plan's bull neck, 0.38 -> 0.23 m wide, carried NECK_HIDDEN into the
    collar, and re-roll the collar round it."""
    old = next(o for o in parts if o.name.startswith("hood neck"))
    oldc = next(o for o in parts if o.name.startswith("collar"))
    root, ctrl, end, sdir = X["neck_root"], X["neck_ctrl"], X["neck_end"], X["sdir"]
    joints = {"nh": (tuple(root - sdir * NECK_HIDDEN), neck_radius(0.0))}
    # the sleeve stops inside the hood, behind the glass (it used to end 7 cm behind the opening plane)
    HT = X["HR"].transposed()
    trim = 1.0
    for j in range(100, 49, -1):
        if (HT @ (gb.bezier(root, ctrl, end, j / 100) - X["lens"])).y >= NECK_TRIM_Y:
            trim = j / 100
            break
    log("neck sleeve trimmed at t %.2f, %.3f m behind the opening" % (trim, (HT @ (gb.bezier(root, ctrl, end, trim) - X["lens"])).y))
    for i in range(5):
        t = trim * i / 4
        joints["n%d" % i] = (tuple(gb.bezier(root, ctrl, end, t)), neck_radius(t))
    bones = [("nh", "n0")] + [("n%d" % i, "n%d" % (i + 1)) for i in range(4)]
    for o in (old, oldc):
        kit.parts.remove(o)
        parts.remove(o)
        bpy.data.objects.remove(o, do_unlink=True)
    new = cl.skin_body(kit, joints, bones, TWILL, subdiv=2, name="hood neck")
    parts.append(new)
    # the coverall collar shoved up round the neck where it leaves the shrug (the module's ring,
    # sized to the new sleeve): a rolled, bunched ring, fattest at the back
    tc = 0.08
    cc = gb.bezier(root, ctrl, end, tc)
    ndir = (gb.bezier(root, ctrl, end, tc + 0.05) - gb.bezier(root, ctrl, end, max(0.0, tc - 0.05))).normalized()
    rx, ry = neck_radius(tc)
    e1 = (V(1, 0, 0) - ndir * ndir.x).normalized()
    e2 = ndir.cross(e1).normalized()
    cpts, crad = [], []
    for j in range(24):
        a = 2 * math.pi * j / 24
        dirv = e1 * math.cos(a) + e2 * math.sin(a)
        back = max(0.0, dirv.z)
        rr = math.hypot(rx * math.cos(a), ry * math.sin(a))
        cpts.append(cc + dirv * (rr * 0.95 + 0.022) + ndir * (0.02 * back))
        crad.append(0.032 * (1.0 + 0.35 * back + 0.18 * math.cos(5 * a)))
    col = gb.loop_tube(kit, cpts, crad, TWILL, ndir, verts=8, name="collar")
    parts.append(col)
    return new


# ------------------------------------------------------------------ UVs
def uv_select_all(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")


def smart_uv(obj, angle=66.0, margin=0.003):
    uv_select_all(obj)
    bpy.ops.uv.smart_project(angle_limit=math.radians(angle), island_margin=margin, area_weight=0.0,
                             correct_aspect=True, scale_to_bounds=False)
    bpy.ops.object.mode_set(mode="OBJECT")


def pack_uv(obj, margin=0.003):
    uv_select_all(obj)
    bpy.ops.uv.select_all(action="SELECT")
    bpy.ops.uv.pack_islands(rotate=True, margin=margin)
    bpy.ops.object.mode_set(mode="OBJECT")


def uv_array(obj):
    uv = obj.data.uv_layers.active.data
    a = np.zeros(len(uv) * 2)
    uv.foreach_get("uv", a)
    return a.reshape(-1, 2)


def set_uv_array(obj, a):
    obj.data.uv_layers.active.data.foreach_set("uv", a.ravel())


def fit_rect(obj, rect, keep_aspect=True):
    """Scale an object's UVs into rect (u0, v0, u1, v1)."""
    a = uv_array(obj)
    lo, hi = a.min(axis=0), a.max(axis=0)
    size = np.maximum(hi - lo, 1e-6)
    rw, rh = rect[2] - rect[0], rect[3] - rect[1]
    if keep_aspect:
        s = min(rw / size[0], rh / size[1])
        sc = np.array([s, s])
    else:
        sc = np.array([rw / size[0], rh / size[1]])
    a = (a - lo) * sc + np.array([rect[0], rect[1]])
    set_uv_array(obj, a)


def planar_uv(obj, origin, ax_u, ax_v, half_u, half_v, rect):
    """Decal UVs: u along ax_u, v along ax_v, the +-half box mapped onto rect."""
    me = obj.data
    if not me.uv_layers:
        me.uv_layers.new(name="UVMap")
    uv = me.uv_layers.active.data
    for poly in me.polygons:
        for li in poly.loop_indices:
            co = me.vertices[me.loops[li].vertex_index].co
            q = co - origin
            u = 0.5 + q.dot(ax_u) / (2 * half_u)
            v = 0.5 + q.dot(ax_v) / (2 * half_v)
            uv[li].uv = (rect[0] + (rect[2] - rect[0]) * u, rect[1] + (rect[3] - rect[1]) * v)


# ------------------------------------------------------------------ helpers
def join(objs, name):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    if len(objs) > 1:
        bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    o.name = name
    o.data.name = name
    return o


def duplicate(obj, name):
    o = obj.copy()
    o.data = obj.data.copy()
    o.name = name
    bpy.context.scene.collection.objects.link(o)
    return o


def set_int_attr(obj, name, value, domain="POINT"):
    me = obj.data
    at = me.attributes.get(name) or me.attributes.new(name, "INT", domain)
    n = len(me.vertices) if domain == "POINT" else len(me.polygons)
    at.data.foreach_set("value", [int(value)] * n)


def get_int_attr(obj, name, domain="POINT"):
    me = obj.data
    at = me.attributes.get(name)
    n = len(me.vertices) if domain == "POINT" else len(me.polygons)
    a = np.zeros(n, dtype=np.int32)
    if at is not None:
        at.data.foreach_get("value", a)
    return a


def material(slot):
    m = bpy.data.materials.get(slot)
    if m is None:
        m = bpy.data.materials.new(slot)
        m.use_nodes = True
    return m


def decimate(obj, target, protect=None):
    t = tris(obj)
    if t <= target:
        return t
    mod = obj.modifiers.new("dec", "DECIMATE")
    mod.ratio = max(0.02, target / t)
    mod.use_collapse_triangulate = True
    if protect is not None:
        mod.vertex_group = protect
        mod.invert_vertex_group = True
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return tris(obj)


def triangulate(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    mod = obj.modifiers.new("tri", "TRIANGULATE")
    mod.quad_method = "BEAUTY"
    bpy.ops.object.modifier_apply(modifier=mod.name)


def shade(obj, angle=60.0):
    obj.data.shade_smooth()
    obj.data.set_sharp_from_angle(angle=math.radians(angle))


# ------------------------------------------------------------------ weights
def bind_segments(J):
    return {n: (np.array(h), np.array(t)) for n, (h, t, r) in J.items()}


def compute_weights(low, J, bone_names, X, bindP):
    """Per-vertex weights from the part each vertex belongs to (distance to the candidate bones,
    Laplacian-smoothed inside the part; rigid parts get one bone; the neck chain is linear along
    its joints so the telescope stretches the sleeve evenly)."""
    P = cr.mesh_positions(low)
    part = get_int_attr(low, "fr_part")
    names = PART_NAMES
    K = len(bone_names)
    bi = {n: i for i, n in enumerate(bone_names)}
    W = np.zeros((len(P), K))
    seg = bind_segments(J)
    edges = cr.adjacency(low.data)
    rigid_all = np.zeros(len(P), dtype=bool)

    def side_of(x, centre=0.0):
        return "L" if x >= centre else "R"

    for pid, pname in enumerate(names):
        idx = np.nonzero(part == pid)[0]
        if len(idx) == 0:
            continue
        Q = P[idx]
        cx = float(Q[:, 0].mean())
        S = side_of(cx)
        cands, rigid = None, None
        if pname.startswith(("hood shell", "hood hem", "opal lens")):
            rigid = "head"
        elif pname.startswith("drawcord") or pname.startswith("aglet"):
            rigid = "cord_" + side_of(cx, float(X["lens"].x))
        elif pname.startswith(("pouch", "tool handle")):
            rigid = "pouch"
        elif pname.startswith("key clip"):
            rigid = "pelvis"
        elif pname.startswith(("key ring", "key ")):
            rigid = "keyring"
        elif pname.startswith("name patch"):
            rigid = "spine_02"
        elif pname.startswith("hood neck"):
            W[idx] = neck_chain_weights(Q, J, bi, K)
            continue
        elif pname.startswith("collar"):
            rigid = "shrug"
        elif pname.startswith("coverall torso"):
            cands = ["pelvis", "spine_01", "spine_02", "chest", "shrug", "clavicle_L", "clavicle_R",
                     "upperarm_L", "upperarm_R", "thigh_L", "thigh_R"]
        elif pname.startswith("tool belt"):
            cands = ["pelvis", "spine_01"]
        elif pname.startswith("coverall legs"):
            cands = None
        elif pname.startswith("coverall arms") or pname.startswith("tape wrist"):
            cands = None
        elif pname.startswith("glove") and "cuff" in pname:
            cands = ["forearm_twist_" + S, "hand_" + S]
        elif pname.startswith("glove") and "palm" in pname:
            rigid = "hand_" + S
        elif pname.startswith("glove"):
            cands = ["hand_" + S, "fingers_" + S, "thumb_" + S]
        elif pname.startswith("boot"):
            cands = ["shin_" + S, "foot_" + S, "toe_" + S]
        elif pname.startswith("tape ankle"):
            cands = ["shin_" + S]
        if rigid is not None:
            W[idx, bi[rigid]] = 1.0
            rigid_all[idx] = True
            continue
        if pname.startswith("coverall legs"):
            # both legs in one part: split by side per vertex
            for S2 in ("L", "R"):
                m = (Q[:, 0] >= 0) if S2 == "L" else (Q[:, 0] < 0)
                sub = idx[m]
                W[sub] = distance_weights(P[sub], ["pelvis", "thigh_" + S2, "shin_" + S2, "foot_" + S2], seg, bi, K,
                                          scale={"pelvis": 0.55, "foot_" + S2: 0.6})
            continue
        if pname.startswith("coverall arms") or pname.startswith("tape wrist"):
            for S2 in ("L", "R"):
                m = (Q[:, 0] >= 0) if S2 == "L" else (Q[:, 0] < 0)
                sub = idx[m]
                if len(sub) == 0:
                    continue
                c2 = ["clavicle_" + S2, "upperarm_" + S2, "forearm_" + S2, "forearm_twist_" + S2]
                if pname.startswith("coverall arms"):
                    c2 = c2 + ["hand_" + S2, "spine_02"]
                W[sub] = distance_weights(P[sub], c2, seg, bi, K,
                                          scale={"spine_02": 0.35, "clavicle_" + S2: 0.7, "hand_" + S2: 0.5})
            continue
        sc = {}
        if pname.startswith("coverall torso"):
            sc = {"upperarm_L": 0.55, "upperarm_R": 0.55, "clavicle_L": 0.8, "clavicle_R": 0.8,
                  "thigh_L": 0.5, "thigh_R": 0.5}
        W[idx] = distance_weights(Q, cands, seg, bi, K, scale=sc)
    # smoothing (not across rigid parts), then <= 4 influences
    W = cr.smooth_weights(W, edges, iterations=4, lam=0.5, keep=rigid_all)
    W = cr.limit_weights(W, 4)
    return W


def distance_weights(Q, cands, seg, bi, K, scale=None, power=4.0):
    scale = scale or {}
    out = np.zeros((len(Q), K))
    D = []
    for n in cands:
        a, b = seg[n]
        d, _ = cr.seg_dist(Q, a, b)
        D.append(d / scale.get(n, 1.0))
    D = np.stack(D, axis=1)
    w = 1.0 / np.power(D + 0.03, power)
    w /= w.sum(axis=1, keepdims=True)
    for k, n in enumerate(cands):
        out[:, bi[n]] = w[:, k]
    return out


def neck_chain_weights(Q, J, bi, K):
    """Linear weights along the joint chain J0 (neck_01) -> J1 (neck_02) -> J2 (neck_03) -> J3 (head)."""
    chain = [("neck_01", "neck_02"), ("neck_02", "neck_03"), ("neck_03", "head")]
    pts = [np.array(J["neck_01"][0]), np.array(J["neck_02"][0]), np.array(J["neck_03"][0]), np.array(J["head"][0])]
    best_d = np.full(len(Q), 1e9)
    out = np.zeros((len(Q), K))
    for k, (b0, b1) in enumerate(chain):
        d, t = cr.seg_dist(Q, pts[k], pts[k + 1])
        better = d < best_d
        best_d = np.where(better, d, best_d)
        w0 = 1.0 - t
        out[better] = 0.0
        out[better, bi[b0]] = w0[better]
        out[better, bi[b1]] = t[better]
    # the sleeve's root inside the collar rides the shrug a little (no tearing at the collar line)
    return out


# ------------------------------------------------------------------ poses
def pose_delta(Jb, Jp, Xb, Xp, yaw):
    """Armature-space delta per bone (3x3), and the posed armature-space heads."""
    Rz = Euler((0, 0, math.radians(yaw))).to_matrix()
    D, heads = {}, {}
    txy = Xp["txy"]
    for n in Jb:
        hb, tb, rb = Jb[n]
        hp, tp, rp = Jp[n]
        Ab = cr.aux_frame(tb - hb, rb)
        Ap = cr.aux_frame(Rz @ (tp - hp), Rz @ rp)
        D[n] = Ap @ Ab.transposed()
        heads[n] = Rz @ hp + V(txy[0], 0.0, 0.0) if n != "root" else V(txy[0], 0.0, 0.0)
    D["root"] = Rz.copy()
    # twist bones: half the hand's roll about the forearm axis
    for S in ("L", "R"):
        f = "forearm_" + S
        h = "hand_" + S
        tw = "forearm_twist_" + S
        axis = Rz @ (Jp[tw][1] - Jp[tw][0])
        carried = D[f] @ Jb[h][2]
        ang = cr.signed_angle(carried, Rz @ Jp[h][2], axis)
        D[tw] = cr.rot_axis(axis, ang * 0.5) @ D[f]
    # cords, keyring, pouch: rigid with their parents in the keys (pendulums at runtime)
    for c in ("cord_L", "cord_R"):
        D[c] = D["head"].copy()
    for c in ("keyring", "pouch"):
        D[c] = D["pelvis"].copy()
    return D, heads


def posed_heads_rigid(heads, D, bind_heads, parents, names):
    """Heads of the rigid children (cords, keyring, pouch) follow their parents."""
    for c in ("cord_L", "cord_R", "keyring", "pouch"):
        p = parents[c]
        heads[c] = heads[p] + D[p] @ (bind_heads[c] - bind_heads[p])
    return heads


def pose_matrices(arm, D, heads):
    mats = {}
    for b in arm.data.bones:
        R = D[b.name] @ b.matrix_local.to_3x3()
        M = R.to_4x4()
        M.translation = heads[b.name]
        mats[b.name] = M
    return mats


# ------------------------------------------------------------------ textures
def bake_material(name, img):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    node = nt.nodes.new("ShaderNodeTexImage")
    node.image = img
    nt.nodes.active = node
    node.select = True
    return m


def emission_material(name, colour):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = (colour[0], colour[1], colour[2], 1.0)
    em.inputs["Strength"].default_value = 1.0
    nt.links.new(em.outputs[0], out.inputs[0])
    return m


def position_material(name):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission")
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    nt.links.new(geo.outputs["Position"], em.inputs["Color"])
    nt.links.new(em.outputs[0], out.inputs[0])
    return m


def set_only_material(obj, mat):
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    for p in obj.data.polygons:
        p.material_index = 0


def add_bake_image_node(obj, img):
    for m in obj.data.materials:
        nt = m.node_tree
        node = nt.nodes.new("ShaderNodeTexImage")
        node.image = img
        nt.nodes.active = node


def run_bake(low, highs, kind, img, samples, extrusion=0.03, max_ray=0.10, margin=16, clear=True):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = samples
    scene.render.bake.margin = margin
    for m in low.data.materials:
        nt = m.node_tree
        for n in nt.nodes:
            if n.bl_idname == "ShaderNodeTexImage":
                n.image = img
                nt.nodes.active = n
    bpy.ops.object.select_all(action="DESELECT")
    for h in highs:
        h.select_set(True)
    low.select_set(True)
    bpy.context.view_layer.objects.active = low
    t0 = time.time()
    if highs:
        bpy.ops.object.bake(type=kind, use_selected_to_active=True, cage_extrusion=extrusion,
                            max_ray_distance=max_ray, normal_space="TANGENT", margin=margin, use_clear=True)
    else:
        bpy.ops.object.bake(type=kind, margin=margin, use_clear=clear)
    log("bake %s %dx%d samples %d: %.1f s" % (kind, img.size[0], img.size[1], samples, time.time() - t0))


def twill_weave(h, w, period=4.0):
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    # 2/1 twill: diagonal wales, a faint cross-pick
    wale = np.sin(2 * np.pi * (xx + yy) / (period * 1.5))
    pick = np.sin(2 * np.pi * yy / period) * 0.35
    return (wale * 0.75 + pick * 0.25)


def noise3(P, seed=7, scale=1.0, octaves=3):
    """Cheap smooth 3D noise from a position map (continuous across UV seams)."""
    rng = np.random.default_rng(seed)
    out = np.zeros(P.shape[:2], dtype=np.float32)
    amp, freq = 1.0, scale
    for _ in range(octaves):
        for _ in range(4):
            d = rng.normal(size=3)
            d /= np.linalg.norm(d)
            ph = rng.random() * 6.283
            out += amp * np.sin((P @ d) * freq * 6.283 + ph).astype(np.float32)
        amp *= 0.5
        freq *= 2.1
    return out / 6.0


def draw_stroke_dist(h, w, segs, rect_px):
    """Distance (px) from every pixel to a polyline set given in 0..1 of rect_px (x0, y0, x1, y1)."""
    x0, y0, x1, y1 = rect_px
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    d = np.full((h, w), 1e9, dtype=np.float32)
    for (ax, ay), (bx, by) in segs:
        A = np.array([x0 + ax * (x1 - x0), y0 + ay * (y1 - y0)])
        B = np.array([x0 + bx * (x1 - x0), y0 + by * (y1 - y0)])
        ab = B - A
        L2 = max(float(ab @ ab), 1e-6)
        t = np.clip(((xx - A[0]) * ab[0] + (yy - A[1]) * ab[1]) / L2, 0, 1)
        qx, qy = A[0] + t * ab[0], A[1] + t * ab[1]
        d = np.minimum(d, np.hypot(xx - qx, yy - qy))
    return d


def glyph_segments(text):
    """Monoline chain-stitch skeletons, provisional (the lettering goes through 平面视觉)."""
    G = {
        "1": [((0.30, 0.78), (0.55, 0.95)), ((0.55, 0.95), (0.55, 0.05)), ((0.30, 0.05), (0.80, 0.05))],
        "4": [((0.70, 0.05), (0.70, 0.95)), ((0.70, 0.95), (0.12, 0.32)), ((0.12, 0.32), (0.90, 0.32))],
    }
    segs = []
    n = len(text)
    for i, ch in enumerate(text):
        for (a, b) in G[ch]:
            segs.append(((((i + a[0] * 0.86 + 0.07) / n), a[1]), (((i + b[0] * 0.86 + 0.07) / n), b[1])))
    return segs


def make_trim_textures(out_dir, rects, size=1024):
    """Creature_TapeSilver atlas: tape, name patch with the stock number, brass keys, steel aglets."""
    H = W = size
    alb = np.zeros((H, W, 3), dtype=np.float32)
    smooth = np.zeros((H, W), dtype=np.float32)
    metal = np.zeros((H, W), dtype=np.float32)
    height = np.zeros((H, W), dtype=np.float32)
    rng = np.random.default_rng(114)
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)

    def region(rect):
        u0, v0, u1, v1 = rect
        return (xx >= u0 * W) & (xx < u1 * W) & (yy >= v0 * H) & (yy < v1 * H)
    # tape: silver cloth tape, crinkles + the fine scrim weave
    m = region(rects["tape"])
    crink = cr.box_blur(rng.normal(size=(H, W)).astype(np.float32), 6) * 6.0
    scrim = np.sin(xx * 2 * np.pi / 3.0) * np.sin(yy * 2 * np.pi / 3.0) * 0.15
    alb[m] = cr.hex_lin("#B8B8B4") * (0.97 + 0.04 * crink[m][:, None])
    smooth[m] = 0.50 + 0.06 * crink[m]
    height[m] = crink[m] * 0.6 + scrim[m]
    # name patch: #DCD8CC twill patch, merrowed border, "114" in chain stitch #1D1D1C
    m = region(rects["patch"])
    u0, v0, u1, v1 = rects["patch"]
    cx, cy = (u0 + u1) / 2 * W, (v0 + v1) / 2 * H
    rx, ry = (u1 - u0) / 2 * W, (v1 - v0) / 2 * H
    e = np.sqrt(((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2)
    alb[m] = cr.hex_lin("#DCD8CC")
    smooth[m] = 0.45
    border = (e > 0.86) & (e < 0.99) & m
    alb[border] = cr.hex_lin("#C8C3B6")
    height[border] = 0.6 + 0.3 * np.sin(np.arctan2(yy - cy, xx - cx)[border] * 140)
    segs = glyph_segments("114")
    tx0, tx1 = cx - rx * 0.62, cx + rx * 0.62
    ty0, ty1 = cy - ry * 0.48, cy + ry * 0.48
    d = draw_stroke_dist(H, W, segs, (tx0, ty0, tx1, ty1))
    sw = max(2.0, ry * 0.075)
    ink = np.clip(1.0 - (d - sw) / 1.2, 0, 1) * m
    # chain stitch: loops every ~1.6 stroke widths along the stroke (a beaded relief)
    bead = 0.5 + 0.5 * np.cos((xx + yy) * 2 * np.pi / (sw * 1.6))
    alb = alb * (1 - ink[..., None]) + cr.hex_lin("#1D1D1C") * ink[..., None]
    height += ink * (0.8 + 0.4 * bead)
    smooth = smooth * (1 - ink) + 0.30 * ink
    # brass key ring and keys
    m = region(rects["brass"])
    alb[m] = cr.hex_lin("#B08A4A") * (0.95 + 0.05 * crink[m][:, None])
    smooth[m] = 0.55
    metal[m] = 1.0
    # steel aglets and key clip
    m = region(rects["steel"])
    alb[m] = cr.hex_lin("#9A9A96")
    smooth[m] = 0.60
    metal[m] = 1.0
    A = cr.srgb_encode(alb)
    N = cr.normal_from_height(cr.box_blur(height, 1), 1.2)
    S = np.stack([smooth, np.ones_like(smooth), metal, np.zeros_like(smooth)], axis=-1)
    cr.save_png(A, os.path.join(out_dir, TAPE + "_A.png"), "sRGB", "trimA")
    cr.save_png(N * 0.5 + 0.5, os.path.join(out_dir, TAPE + "_N.png"), "Non-Color", "trimN")
    cr.save_png(S, os.path.join(out_dir, TAPE + "_S.png"), "Non-Color", "trimS")
    log("trim atlas %d^2 written (stock number '114', provisional chain-stitch lettering)" % size)


def make_lens_textures(out_dir, w=1024, h=1024):
    """The ribbed glass and the sleeve round it, on one disc (build_deep_lens). A lamp behind old prismatic glass, not
    a lit card: one hot core, off centre and low, falling away fast; flutes that run pole to pole across the dome and
    break the lamp's outline; aged amber toward the edge; a dark seal; the tell, a soft shadow beside the core; the
    sleeve lit only near the glass, more on its lower wall, gone before the hem. Rows bottom-up."""
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    X = (xx + 0.5) / w * 2 - 1
    Y = (yy + 0.5) / h * 2 - 1
    rho = np.hypot(X, Y)
    phi = np.arctan2(Y, X)
    f = rho / DOME_UV
    xm, zm = X / DOME_UV * DOME_A, Y / DOME_UV * DOME_B
    px_m = 2.0 / w / DOME_UV * DOME_A

    def ramp(a, b, t):
        q = np.clip((t - a) / (b - a), 0, 1)
        return q * q * (3 - 2 * q)

    flute, fine = 0.0165, 0.0041              # the flutes read to about 3 m, the fine prism at the catch
    merid = xm / np.sqrt(np.clip(1 - (zm / (DOME_B * 1.30)) ** 2, 0.25, 1))
    phase = 2 * np.pi * merid / flute
    rib = 0.5 + 0.5 * np.cos(phase)
    prism = np.abs(((merid % fine) / fine) - 0.5) * 2
    shift = 0.32 * flute * np.sin(phase)      # each flute throws its own image of the lamp
    dx, dz = xm - HOT_X + shift, zm - HOT_Z
    core = np.exp(-((dx / 0.027) ** 2 + (dz / 0.040) ** 2))
    halo = np.exp(-(((dx / 0.066) ** 2 + (dz / 0.082) ** 2) ** 0.78))
    tube = np.exp(-((dx / 0.0085) ** 2 + (dz / 0.046) ** 2))     # the lamp itself, a short upright stroke behind the flutes
    lamp = 0.52 * core + 0.34 * halo + 0.32 * tube
    P = np.stack([xm * 26.0, zm * 26.0, np.zeros_like(xm)], axis=-1)
    age = 0.80 + 0.40 * noise3(P, seed=114, scale=1.0, octaves=3)
    dust = 1.0 - 0.45 * ramp(0.35, 1.0, -Y / DOME_UV) * ramp(0.45, 1.0, f)
    brim = 1.0 - 0.60 * ramp(0.15, 0.90, Y / DOME_UV)
    edge = np.clip(1 - f ** 3.2, 0, 1) ** 0.8
    # the tell: something pressed against the glass from inside, beside the core
    tx, tz = xm - (HOT_X + 0.030), zm - (HOT_Z + 0.012)
    face = np.exp(-((tx / 0.026) ** 2 + (tz / 0.052) ** 2))
    brow = np.exp(-((tx / 0.034) ** 2 + ((tz - 0.030) / 0.008) ** 2)) * 0.5
    shadow = np.clip(face * 0.42 + brow * 0.30, 0, 0.6)
    glass = lamp * (0.60 + 0.40 * rib) * (0.94 + 0.06 * prism) * age * dust * brim * edge * (1 - shadow)
    glass = glass / glass[f < 0.9].max()
    seal = ramp(0.95, 0.985, f) * (1 - ramp(1.02, 1.06, f))                # the dark seal between glass and cloth
    is_glass = 1 - ramp(0.985, 1.0, f)
    t = np.clip((rho - DOME_UV) / (1 - DOME_UV), 0, 1)                       # 0 at the glass, 1 at the hem
    pleat = np.cos(9 * phi)                                                  # crests out, valleys toward the lamp
    fold = (0.5 - 0.5 * pleat) ** 1.5
    wall = 0.20 * np.exp(-((t / 0.40) ** 1.3)) * (1 - ramp(0.62, 0.92, t)) * (1 - 0.50 * np.sin(phi)) * (0.30 + 0.70 * fold)
    wall = wall * (1 - is_glass) * (1 - seal)
    emit = np.clip(glass * is_glass * (1 - seal) + wall, 0, 1)
    emit = cr.box_blur(emit, 1)
    warm = np.clip(emit * 1.6, 0, 1)[..., None] ** 0.55
    amber = np.array([1.0, 0.60, 0.28])[None, None, :]
    white = np.array([1.0, 0.96, 0.86])[None, None, :]
    E = emit[..., None] * (amber + (white - amber) * warm)
    # albedo: dark aged glass in shadow, the twill's own colour (dimmer) on the sleeve
    centre_a, rim_a = cr.hex_lin("#6A6150"), cr.hex_lin("#2C2822")
    g = np.clip(f, 0, 1)[..., None]
    alb = (centre_a + (rim_a - centre_a) * g) * (0.82 + 0.18 * rib[..., None]) * (0.92 + 0.16 * (age[..., None] - 0.8))
    cloth = cr.hex_lin("#2E3B33")[None, None, :] * (0.04 + 0.16 * ramp(0.55, 1.0, t)[..., None])     # lit by the lamp, not the room
    ig = is_glass[..., None]
    alb = alb * ig + cloth * (1 - ig)
    alb = alb * (1 - seal[..., None] * 0.85)
    # height in metres: 1 mm flutes, a fine prism on the glass; cord-drawn pleats on the sleeve
    hg = 0.00075 * np.cos(phase) + 0.00012 * prism
    hc = 0.0030 * pleat * (0.4 + 0.6 * t)
    N = cr.normal_from_height(hg * is_glass + hc * (1 - is_glass), 1.0 / px_m)
    smooth_map = 0.62 * is_glass
    cavity = np.clip(is_glass * (1.0 - 0.45 * f) + (1 - is_glass) * 0.45 * ramp(0.60, 1.0, t), 0, 1)     # no room reflection deep in the sleeve
    # mask B: the sleeve is "metal" with a near-black albedo (no diffuse, F0 = the albedo). On a dielectric the troffer overhead
    # puts URP's grazing specular on the lower wall, a pewter crescent; in the hood only the lamp may light this cloth.
    S = np.stack([smooth_map, cavity, 1 - is_glass, np.zeros_like(rib)], axis=-1)
    cr.save_png(cr.srgb_encode(alb), os.path.join(out_dir, LENS + "_A.png"), "sRGB", "lensA")
    cr.save_png(N * 0.5 + 0.5, os.path.join(out_dir, LENS + "_N.png"), "Non-Color", "lensN")
    cr.save_png(S, os.path.join(out_dir, LENS + "_S.png"), "Non-Color", "lensS")
    cr.save_png(cr.srgb_encode(E), os.path.join(out_dir, LENS + "_E.png"), "sRGB", "lensE")
    inside = f < 0.92
    hot = float(np.percentile(emit[inside], 99.5))
    mid = float(np.median(emit[inside]))
    rim = float(emit[(f > 0.80) & (f < 0.92)].mean())
    log("lens maps %dx%d: glass p99.5 %.2f, median %.3f, near the rim %.3f: hot/median %.1f : 1, hot/rim %.1f : 1 (old card: 4.16 : 1 between bands, no hot centre)"
        % (w, h, hot, mid, rim, hot / max(mid, 1e-4), hot / max(rim, 1e-4)))
    return hot / max(rim, 1e-4)


# ------------------------------------------------------------------ previews
def preview_render(path, objs, cam_loc, target, lens_mm=35, res=(800, 1000), engine="BLENDER_WORKBENCH"):
    scene = bpy.context.scene
    scene.render.engine = engine
    scene.render.resolution_x, scene.render.resolution_y = res
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    if engine == "BLENDER_WORKBENCH":
        sh = scene.display.shading
        sh.light = "STUDIO"
        sh.color_type = "MATERIAL"
        sh.show_cavity = True
    cam = scene.camera
    if cam is None:
        cd = bpy.data.cameras.new("cam")
        cam = bpy.data.objects.new("cam", cd)
        scene.collection.objects.link(cam)
        scene.camera = cam
    cam.data.type = "PERSP"
    cam.data.lens = lens_mm
    cam.location = cam_loc
    cam.rotation_euler = (target - cam_loc).to_track_quat("-Z", "Y").to_euler()
    hidden = []
    for o in scene.objects:
        if o.type == "MESH" and o not in objs:
            if not o.hide_render:
                hidden.append(o)
                o.hide_render = True
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    for o in hidden:
        o.hide_render = False


# ------------------------------------------------------------------ main
def main(argv):
    out_root, work, quick, do_bake, previews = None, None, False, True, False
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == "--out-root":
            out_root = argv[i + 1]; i += 2; continue
        if a == "--work":
            work = argv[i + 1]; i += 2; continue
        if a == "--quick":
            quick = True; i += 1; continue
        if a == "--no-bake":
            do_bake = False; i += 1; continue
        if a == "--previews":
            previews = True; i += 1; continue
        i += 1
    assert out_root and work, "--out-root and --work are required"
    t_start = time.time()
    os.makedirs(work, exist_ok=True)
    creatures = os.path.join(out_root, "Assets", "Resources", "Creatures")
    textures = os.path.join(out_root, "Assets", "Resources", "Surfaces", "Textures")
    os.makedirs(creatures, exist_ok=True)
    os.makedirs(textures, exist_ok=True)

    # ---- 1. bind body (the giant module's own build), high poly
    bindP = make_bind()
    log("bind pose: pelvis %s, lens %s" % (tuple(round(x, 3) for x in bindP["pelvis"]), tuple(round(x, 3) for x in bindP["lens"])))
    kit = kitlib.Kit(NAME)
    parts, fshift = build_bind_body(kit, bindP)
    Jb, Xb = skeleton(bindP)
    HOOD_FRAME.update(H=np.array(Xb["lens"]), R=np.array(Xb["HR"]))
    if abs(fshift) > 1e-4:
        for n in Jb:
            h, t, r = Jb[n]
            Jb[n] = (h + V(0, 0, fshift), t + V(0, 0, fshift), r)
    rebuild_neck(kit, parts, Xb)
    patch = next(o for o in parts if o.name.startswith("name patch"))
    bpy.context.view_layer.update()
    mw = patch.matrix_world
    PATCH_FRAME.update(origin=mw.translation.copy(), u=mw.to_3x3().col[0].normalized(), v=mw.to_3x3().col[1].normalized())
    for o in parts:
        finish_part(o)
    # key ring / pouch bone positions from the parts themselves
    def centre(prefix):
        objs = [o for o in parts if o.name.startswith(prefix)]
        P_ = np.concatenate([cr.mesh_positions(o) for o in objs])
        return V(*P_.mean(axis=0)), V(*P_.max(axis=0)), V(*P_.min(axis=0))
    kc, kmax, kmin = centre("key ring")
    pc, pmax, pmin = centre("pouch")
    Jb["keyring"] = (V(kc.x, kc.y, kmax.z + 0.02), kc, V(1, 0, 0))
    Jb["pouch"] = (V(pc.x, pc.y, pmax.z), pc, V(1, 0, 0))
    total_high = sum(tris(o) for o in parts)
    log("high poly: %d parts, %d tris" % (len(parts), total_high))

    # ---- 2. low poly: keep the silhouette shells, decimate, categorise
    low_parts, high_parts = [], []
    names = []
    for o in parts:
        cat, keep, tgt = rule(o.name)
        h = duplicate(o, "HIGH " + o.name)
        h["fr_cat"] = cat
        high_parts.append(h)
        if not keep:
            bpy.data.objects.remove(o, do_unlink=True)
            continue
        o["fr_cat"] = cat
        o.vertex_groups.clear()
        decimate(o, tgt)
        names.append(o.name)
        PART_NAMES.append(o.name)
        set_int_attr(o, "fr_part", len(names) - 1)
        set_int_attr(o, "fr_cat", CATS.index(cat))
        set_only_material(o, material(CAT_SLOT[cat]))
        low_parts.append(o)
    # UVs: twill atlas (smart project + density + pack), lens decal, trim atlas cells
    twill_objs = [o for o in low_parts if CAT_SLOT[o["fr_cat"]] == TWILL]
    trim_objs = [o for o in low_parts if CAT_SLOT[o["fr_cat"]] == TAPE]
    lens_obj = next(o for o in low_parts if o["fr_cat"] == "lens")
    tw = join([duplicate(o, "tw " + o.name) for o in twill_objs], "LOW_TWILL_TMP")
    for o in twill_objs:
        bpy.data.objects.remove(o, do_unlink=True)
    smart_uv(tw)
    cat_f = get_int_attr(tw, "fr_cat", "POINT")
    a = uv_array(tw)
    loop_v = np.zeros(len(tw.data.loops), dtype=np.int64)
    tw.data.loops.foreach_get("vertex_index", loop_v)
    dens = np.array([UV_DENSITY.get(CATS[c], 1.0) for c in cat_f[loop_v]])
    set_uv_array(tw, a * dens[:, None])
    pack_uv(tw, 0.003)
    # lens: the disc UVs written with the part (glass + sleeve)
    HRb = Xb["HR"]
    set_lens_uv(lens_obj)
    # trim cells
    rects = {"tape": (0.01, 0.01, 0.60, 0.99), "patch": (0.62, 0.785, 0.99, 0.99),
             "brass": (0.62, 0.40, 0.99, 0.76), "steel": (0.62, 0.01, 0.99, 0.37)}
    groups = {}
    for o in trim_objs:
        groups.setdefault(o["fr_cat"], []).append(o)
    trims = []
    for cat, objs in groups.items():
        g = join(objs, "trim " + cat)
        if cat == "patch":
            # decal across the patch's own frame: u along the figure's left (+X), v up
            planar_uv(g, PATCH_FRAME["origin"], PATCH_FRAME["u"], PATCH_FRAME["v"], 0.044 * KC * 1.02,
                      0.044 * KC * 0.58 * 1.04, rects["patch"])
        else:
            smart_uv(g, 66.0, 0.01)
            pack_uv(g, 0.01)
            fit_rect(g, rects[cat])
        trims.append(g)
    low = join([tw, lens_obj] + trims, NAME + "_LOD0")
    # material slots in a fixed order: twill, lens, trim
    mats = [material(TWILL), material(LENS), material(TAPE)]
    old = [m.name for m in low.data.materials]
    idx = np.zeros(len(low.data.polygons), dtype=np.int64)
    low.data.polygons.foreach_get("material_index", idx)
    remap = np.array([[TWILL, LENS, TAPE].index(n.split(".")[0]) for n in old])
    low.data.materials.clear()
    for m in mats:
        low.data.materials.append(m)
    low.data.polygons.foreach_set("material_index", remap[idx])
    # part names survive the join as the int attribute; keep the table on the object
    triangulate(low)
    shade(low, 60.0)
    lod0_tris = tris(low)
    log("LOD0: %d tris (budget <= 12000, cap 15000)" % lod0_tris)

    # ---- 3. armature (left + centre built, mirrored) and weights
    bone_rows = []
    for n, p, d in BONES:
        h, t, r = Jb[n]
        bone_rows.append(dict(name=n, head=h, tail=t, parent=p, zref=r.cross((t - h).normalized()) if False else r, deform=d))
    # align_roll wants the Z axis: our ref is the frame's X, so Z = X x Y
    for row in bone_rows:
        y = (row["tail"] - row["head"]).normalized()
        x = row["zref"] - y * row["zref"].dot(y)
        x = x.normalized() if x.length > 1e-6 else V(1, 0, 0)
        row["zref"] = x.cross(y)
    arm = cr.build_armature(NAME + "_Armature", bone_rows)
    all_names = [b.name for b in cr.bone_order(arm)]
    # mirror check: the symmetrized right side against the skeleton's own right side
    worst = 0.0
    for b in arm.data.bones:
        if b.name.endswith("_R"):
            worst = max(worst, (b.head_local - Jb[b.name][0]).length, (b.tail_local - Jb[b.name][1]).length)
    log("armature: %d bones (%d deform); mirror vs skeleton right side: max %.4f m" % (
        len(arm.data.bones), sum(1 for b in arm.data.bones if b.use_deform), worst))
    deform_names = [n for n in all_names]
    W = compute_weights(low, Jb, deform_names, Xb, bindP)
    root_col = deform_names.index("root")
    assert W[:, root_col].max() == 0.0, "root must carry no weights"
    cr.set_all_weights(low, deform_names, W)
    low.parent = arm
    mod = low.modifiers.new("Armature", "ARMATURE")
    mod.object = arm
    infl = (W > 0).sum(axis=1)
    log("weights: max influences %d, mean %.2f, verts %d" % (infl.max(), infl.mean(), len(infl)))

    # ---- 4. pose keys + T5 envelope diagnostics
    PP = pose_params()
    bind_heads = {n: Jb[n][0] for n in Jb}
    parents = {b.name: (b.parent.name if b.parent else None) for b in arm.data.bones}
    pose_out, t5 = [], {}
    order = [b.name for b in cr.bone_order(arm)]
    s_of = {"door_in": -0.62, "door_mid": 0.0, "door_out": 0.60}
    cats_v = get_int_attr(low, "fr_cat", "POINT")
    hand_v = np.isin(cats_v, [CATS.index("glove")])
    neck_v = cats_v == CATS.index("neck")
    torso_names = PART_NAMES
    parts_v = get_int_attr(low, "fr_part", "POINT")
    torso_pid = [k for k, n in enumerate(torso_names) if n.startswith("coverall torso")]
    torso_v = np.isin(parts_v, torso_pid)
    bindP_body = to_body(bindP)[0]
    for key in ["std", "listen_std", "low", "tall", "door_in", "door_mid", "door_out", "std_f", "listen_std_f", "low_f"]:
        P = PP[key]
        Jp, Xp = skeleton(P)
        if Xp is None:
            continue
        Jp["keyring"] = Jb["keyring"]
        Jp["pouch"] = Jb["pouch"]
        D, heads = pose_delta(Jb, Jp, Xb, Xp, Xp["yaw"])
        heads = posed_heads_rigid(heads, D, bind_heads, parents, order)
        mats_p = pose_matrices(arm, D, heads)
        cr.apply_pose(arm, mats_p)
        co = cr.evaluated_positions(low)
        lo = float(co[:, 2].min())
        body = co[~hand_v] if key != "listen_std" and key != "listen_std_f" else co[~hand_v]
        top = float(body[:, 2].max())
        hw = float(np.abs(co[:, 0]).max())
        lens_c = heads["head"]  # head joint; the lens centre itself below
        lc = Xp["lens"]
        if Xp["yaw"]:
            Rz = Euler((0, 0, math.radians(Xp["yaw"]))).to_matrix()
            lc = Rz @ lc + V(Xp["txy"][0], 0, 0)
        env = {"top": round(top, 3), "lowest": round(lo, 3), "halfWidth": round(hw, 3), "lensZ": round(lc.z, 3),
               "neck": round(Xp["neck_len"], 3), "slide": round(Xp["slide"], 3), "stretch": round(Xp["stretch"], 3),
               "misses": {k: round(v, 3) for k, v in Xp["misses"].items()}}
        if key in s_of:
            plane = s_of[key]                    # nav axis at s along the heading: the door plane is at y = s
            sl = np.abs(co[:, 1] - plane) < 0.15
            slab = co[sl]
            if sl.any():
                bad = sl & ((co[:, 2] > 2.08) | (np.abs(co[:, 0]) > 0.48))
                bp = sorted({PART_NAMES[k] for k in parts_v[bad]})
                env["doorViolators"] = bp
                if bad.any():
                    bc_ = co[bad]
                    log("    violating verts %d: x %.2f..%.2f y %.2f..%.2f z %.2f..%.2f" % (
                        len(bc_), bc_[:, 0].min(), bc_[:, 0].max(), bc_[:, 1].min(), bc_[:, 1].max(), bc_[:, 2].min(), bc_[:, 2].max()))
            env["doorTop"] = round(float(slab[:, 2].max()) if len(slab) else 0.0, 3)
            env["doorHalfWidth"] = round(float(np.abs(slab[:, 0]).max()) if len(slab) else 0.0, 3)
            env["doorOk"] = env["doorTop"] <= 2.08 and env["doorHalfWidth"] <= 0.48
        if key in DOOR_SWEEP:
            # the key is shown over a range of s: the door plane sits at y = s0 + (s - s0) in the key's own frame
            lo_s, hi_s = DOOR_SWEEP[key]
            worst, worst_s, parts_bad = 0.0, None, set()
            s_ = lo_s
            while s_ <= hi_s + 1e-6:
                plane = s_                      # the door plane sits at y = s in the key's own frame
                sl = np.abs(co[:, 1] - plane) < 0.08
                bad = sl & ((co[:, 2] > 2.10) | (np.abs(co[:, 0]) > 0.50))
                if bad.any():
                    over = max(float((co[bad, 2] - 2.10).max()), float((np.abs(co[bad, 0]) - 0.50).max()))
                    parts_bad |= {PART_NAMES[k] for k in parts_v[bad]}
                    if over > worst:
                        worst, worst_s = over, s_
                s_ += 0.05
            env["sweep"] = {"range": [lo_s, hi_s], "worst": round(worst, 3), "worstS": worst_s, "parts": sorted(parts_bad)}
            log("    sweep s %.2f..%.2f: worst overshoot %.3f m at s %s; parts %s" % (lo_s, hi_s, worst, ("%.2f" % worst_s) if worst_s is not None else "-", sorted(parts_bad)))
        # neck vertices outside the torso hull (where they should be inside the collar): a rough check
        # by distance from the posed neck root along -sdir
        t5[key] = env
        log("pose %-13s top %.3f lens %.3f neck %.3f (slide %.2f, stretch %.2f) half-width %.2f%s misses %s" % (
            key, top, lc.z, Xp["neck_len"], Xp["slide"], Xp["stretch"], hw,
            ("  door slab top %.3f |x| %.3f %s" % (env["doorTop"], env["doorHalfWidth"], "ok" if env["doorOk"] else "FAIL")) if "doorTop" in env else "",
            env["misses"]))
        # top markers: the top-most body vertex of this pose
        top_i = int(np.nonzero(~hand_v)[0][np.argmax(co[~hand_v][:, 2])])
        env["topPart"] = PART_NAMES[parts_v[top_i]]
        log("    top vertex on %s; violators %s" % (env["topPart"], env.get("doorViolators", "-")))
        pose_out.append(dict(name=key, D=D, heads=heads, top_vertex=top_i, env=env, lens=lc))
        if previews:
            preview_render(os.path.join(work, "pose_%s_front.png" % key), [low], V(0.0, -7.5, 1.6), V(0, 0, 1.5), 50)
            preview_render(os.path.join(work, "pose_%s_side.png" % key), [low], V(7.5, -0.3, 1.6), V(0, -0.3, 1.5), 50)
            preview_render(os.path.join(work, "pose_%s_34.png" % key), [low], V(4.5, -5.5, 2.0), V(0, -0.3, 1.5), 50)
    cr.rest_pose(arm)

    # ---- 5. JSON: bind heads/rotations + every key's deltas and offsets (Blender armature space)
    bones_json = []
    for n in order:
        b = arm.data.bones[n]
        bones_json.append(dict(name=n, parent=parents[n] or "", bind=cr.vlist(b.head_local),
                               bindRot=cr.qlist(b.matrix_local.to_3x3()), length=round(b.length, 5)))
    poses_json = []
    for po in pose_out:
        rot, off = [], []
        for n in order:
            rot += cr.qlist(po["D"][n])
            p = parents[n]
            if p is None:
                o = po["heads"][n]
            else:
                o = po["D"][p].transposed() @ (po["heads"][n] - po["heads"][p])
            off += cr.vlist(o)
        name = po["name"]
        face = 1 if name.endswith("_f") else 0
        poses_json.append(dict(name=name.replace("_f", ""), faceLift=face, rot=rot, off=off,
                               lens=cr.vlist(po["lens"]), top=po["env"]["top"]))
    # locators (bind, armature space): lens centre + normal, hood front probe points, ball of each foot, top markers
    tops = sorted({po["top_vertex"] for po in pose_out})
    bind_co = cr.mesh_positions(low)
    vg_names = deform_names
    markers = []
    for vi in tops:
        k = int(np.argmax(W[vi]))
        markers.append(dict(name="top_%d" % vi, bone=vg_names[k], pos=cr.vlist(V(*bind_co[vi]))))
    lens_n = HRb @ V(0, -1, 0)
    lens_c = Xb["lens"]
    locs = [dict(name="lens", bone="head", pos=cr.vlist(lens_c), dir=cr.vlist(lens_n)),
            dict(name="hood_front_top", bone="head", pos=cr.vlist(Xb["lens"] + HRb @ V(0, -0.02, 0.30)), dir=cr.vlist(lens_n)),
            dict(name="hood_front_low", bone="head", pos=cr.vlist(Xb["lens"] + HRb @ V(0, -0.02, -0.22)), dir=cr.vlist(lens_n)),
            dict(name="hood_centre", bone="head", pos=cr.vlist(Xb["hood_c"]), dir=cr.vlist(lens_n)),
            dict(name="ball_L", bone="foot_L", pos=cr.vlist(Xb["ball_L"]), dir=[0, 0, 1]),
            dict(name="ball_R", bone="foot_R", pos=cr.vlist(Xb["ball_R"]), dir=[0, 0, 1]),
            dict(name="shoulder_L", bone="clavicle_L", pos=cr.vlist(Jb["upperarm_L"][0]), dir=[1, 0, 0]),
            dict(name="shoulder_R", bone="clavicle_R", pos=cr.vlist(Jb["upperarm_R"][0]), dir=[-1, 0, 0]),
            dict(name="hips", bone="pelvis", pos=cr.vlist(Jb["pelvis"][0]), dir=[0, 0, 1])] + \
        [dict(name=m["name"], bone=m["bone"], pos=m["pos"], dir=[0, 0, 1]) for m in markers]
    data = dict(version=1, name=NAME, space="blender_armature", toUnity="measured from bind heads at load",
                faceLiftKeys=["std", "listen_std", "low"], bones=bones_json, poses=poses_json, locators=locs,
                neck=dict(n1=N1, n2=N2, n3=N3, hidden=NECK_HIDDEN, stretchMax=STRETCH_MAX),
                gait=dict(stepStd=1.14, stepStdRun=1.45, stepLow=0.90, stepLowRun=1.10, stepTall=1.30, stepTallRun=1.60,
                          swingLift=0.14),
                door=dict(sIn=-0.62, sMid=0.0, sOut=0.60))
    cr.write_json(os.path.join(creatures, NAME + "_Poses.json"), data)
    cr.write_json(os.path.join(work, "t5_envelope.json"), t5)

    # ---- 6. bakes and textures
    if do_bake:
        res_n = 2048 if quick else 4096
        res_s = 1024 if quick else 2048
        # bake copy of the twill faces only
        tw_low = duplicate(low, "BAKE_TWILL")
        tw_low.modifiers.clear()
        tw_low.parent = None
        bm = bmesh.new()
        bm.from_mesh(tw_low.data)
        bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.material_index != 0], context="FACES")
        bm.to_mesh(tw_low.data)
        bm.free()
        imgN = cr.new_image("bakeN", res_n, res_n, float_buffer=True, colorspace="Non-Color")
        set_only_material(tw_low, bake_material("bake_tw", imgN))
        twill_high = [h for h in high_parts if CAT_SLOT[h["fr_cat"]] == TWILL]
        run_bake(tw_low, twill_high, "NORMAL", imgN, 1, margin=16 if not quick else 8)
        Nb = cr.image_array(imgN)[..., :3] * 2 - 1
        imgAO = cr.new_image("bakeAO", res_s, res_s, float_buffer=True, colorspace="Non-Color")
        run_bake(tw_low, twill_high, "AO", imgAO, 8 if quick else 24, margin=8)
        AO = cr.image_array(imgAO)[..., 0]
        # category ID (from the high parts: strips carry their garment's colour)
        id_col = {}
        for k, c in enumerate(CATS):
            id_col[c] = ((k + 1) / 16.0, 0.0, 0.0)
        saved_mats = {}
        for h in twill_high:
            saved_mats[h.name] = list(h.data.materials)
            set_only_material(h, emission_material("id_" + h.name, id_col[h["fr_cat"]]))
        imgID = cr.new_image("bakeID", res_s, res_s, float_buffer=True, colorspace="Non-Color")
        run_bake(tw_low, twill_high, "EMIT", imgID, 1, margin=8)
        ID = np.rint(cr.image_array(imgID)[..., 0] * 16.0 - 1).astype(np.int32)
        imgP = cr.new_image("bakeP", res_s, res_s, float_buffer=True, colorspace="Non-Color")
        sentinel = np.zeros((res_s, res_s, 4), dtype=np.float32)
        sentinel[..., :3] = -100.0
        imgP.pixels.foreach_set(sentinel.ravel())
        set_only_material(tw_low, position_material("pos"))
        add_bake_image_node(tw_low, imgP)
        run_bake(tw_low, [], "EMIT", imgP, 1, margin=8, clear=False)
        PM = cr.image_array(imgP)
        cover = PM[..., 0] > -50.0
        compose_twill(textures, Nb, AO, ID, PM[..., :3], cover, Jb, res_n, res_s)
        make_lens_textures(textures)
        make_trim_textures(textures, rects)
        bpy.data.objects.remove(tw_low, do_unlink=True)

    # ---- 7. LODs: collapse copies of LOD0 (the lens protected), <= 2 influences
    lods = [low]
    keep = low.vertex_groups.new(name="fr_lod_keep")
    lens_faces = [p for p in low.data.polygons if p.material_index == 1]
    keep.add(sorted({v for p in lens_faces for v in p.vertices}), 1.0, "REPLACE")
    shoulder_hip = np.zeros(len(low.data.vertices), dtype=bool)
    bc = cr.mesh_positions(low)
    for j in (Jb["upperarm_L"][0], Jb["upperarm_R"][0], Jb["thigh_L"][0], Jb["thigh_R"][0]):
        shoulder_hip |= np.linalg.norm(bc - np.array(j), axis=1) < 0.30
    for lv, target in ((1, 5500), (2, 2200)):
        o = duplicate(low, "%s_LOD%d" % (NAME, lv))
        o.modifiers.clear()
        o.parent = arm
        decimate(o, target, protect="fr_lod_keep")
        triangulate(o)
        shade(o, 60.0)
        Wl = cr.read_groups(o, deform_names)
        pos_l = cr.mesh_positions(o)
        near = np.zeros(len(pos_l), dtype=bool)
        for j in (Jb["upperarm_L"][0], Jb["upperarm_R"][0], Jb["thigh_L"][0], Jb["thigh_R"][0]):
            near |= np.linalg.norm(pos_l - np.array(j), axis=1) < 0.30
        mi = np.where(near, 4, 2) if lv == 1 else np.full(len(pos_l), 2)
        Wl = cr.limit_weights(Wl, mi)
        cr.set_all_weights(o, deform_names, Wl)
        m2 = o.modifiers.new("Armature", "ARMATURE")
        m2.object = arm
        lods.append(o)
        log("LOD%d: %d tris" % (lv, tris(o)))
    low.vertex_groups.remove(low.vertex_groups["fr_lod_keep"])
    for o in lods[1:]:
        g = o.vertex_groups.get("fr_lod_keep")
        if g is not None:
            o.vertex_groups.remove(g)
    for o in lods:
        for at in ("fr_part", "fr_cat"):
            if o.data.attributes.get(at) is not None:
                o.data.attributes.remove(o.data.attributes[at])

    # ---- 8. export
    fbx = os.path.join(creatures, NAME + ".fbx")
    cr.export_skinned_fbx(fbx, arm, lods)
    xs = cr.mesh_positions(low)
    lo_u = [-xs[:, 0].max(), xs[:, 2].min(), -xs[:, 1].max()]
    hi_u = [-xs[:, 0].min(), xs[:, 2].max(), -xs[:, 1].min()]
    sidecar = dict(name=NAME, slots=[TWILL, LENS, TAPE], lodDistances=[8.0, 20.0, 0.0],
                   triangles=tris(low), trianglesLod1=tris(lods[1]), trianglesLod2=tris(lods[2]),
                   bones=len(arm.data.bones), boundsMin=lo_u, boundsMax=hi_u, frontAxis="+Z", noCollider=True,
                   envelope=t5, concept=dict(title="Night Shift (production)", pose="bind",
                                             pitch=gb.PITCH))
    cr.write_json(os.path.join(creatures, NAME + ".json"), sidecar)
    with open(os.path.join(work, "build_log.txt"), "w") as fh:
        fh.write("\n".join(LOG) + "\n")
    log("exported %s (%.1f s)" % (fbx, time.time() - t_start))


def compose_twill(textures, Nb, AO, ID, PM, cover, Jb, res_n, res_s):
    """Creature_TwillSpruce A / N / S (4096^2): category colours, worn knees and seat, sun-faded tops,
    the baked normal + a twill weave, cavity from the baked AO. Rows bottom-up."""
    f = res_n // res_s
    IDu = cr.upsample(ID, f)
    AOu = cr.box_blur(cr.upsample(AO, f), 1)
    PMu = cr.upsample(PM, f)
    cov = cr.upsample(cover, f)
    H, W_ = IDu.shape
    cat = np.clip(IDu, 0, len(CATS) - 1)
    base = {"cloth": "#2E3B33", "neck": "#2E3B33", "glove": "#1C1C1E", "boot": "#151413", "belt": "#151413",
            "cord": "#191817"}
    sm = {"cloth": 0.15, "neck": 0.15, "glove": 0.15, "boot": 0.40, "belt": 0.40, "cord": 0.30}
    sheen = {"cloth": 1.0, "neck": 1.0, "glove": 0.6, "boot": 0.0, "belt": 0.15, "cord": 0.4}
    alb = np.zeros((H, W_, 3), dtype=np.float32)
    smooth = np.zeros((H, W_), dtype=np.float32)
    sh = np.zeros((H, W_), dtype=np.float32)
    for k, c in enumerate(CATS):
        m = cat == k
        if not m.any():
            continue
        alb[m] = cr.hex_lin(base.get(c, "#2E3B33"))
        smooth[m] = sm.get(c, 0.15)
        sh[m] = sheen.get(c, 1.0)
    clothy = np.isin(cat, [CATS.index("cloth"), CATS.index("neck")])
    # fabric mottling (position-driven, continuous across seams), sun-faded tops, worn knees / seat
    nz = noise3(PMu, 3, 2.0, 3)
    alb *= (0.955 + 0.045 * np.clip(nz, -1, 1))[..., None]
    z = PMu[..., 2]
    # sun-faded tops: the shoulders and hood stay at the base value, the rest a touch deeper
    fade = np.clip((z - 2.55) / 0.6, 0, 1) * clothy
    alb = alb * (0.94 + 0.06 * fade[..., None])
    wear = np.zeros((H, W_), dtype=np.float32)
    for S in ("L", "R"):
        kn = np.array(Jb["shin_" + S][0])
        d = np.linalg.norm(PMu - kn, axis=-1)
        front = (PMu[..., 1] < kn[1] + 0.02)
        wear = np.maximum(wear, np.clip(1 - d / 0.17, 0, 1) * front)
    pel = np.array(Jb["pelvis"][0])
    dseat = np.linalg.norm((PMu - pel) * np.array([0.8, 1.0, 1.4]), axis=-1)
    seat = np.clip(1 - dseat / 0.32, 0, 1) * (PMu[..., 1] > pel[1] + 0.05)
    wear = np.maximum(wear, seat) * clothy
    smooth = smooth + (0.35 - smooth) * np.clip(wear * 1.6, 0, 1)
    # cuffs and hems pick up grime (darker, a touch warmer)
    grime = np.clip((0.35 - z) / 0.25, 0, 1) * clothy
    alb = alb * (1 - 0.12 * grime[..., None])
    alb = alb * (0.88 + 0.12 * AOu[..., None])
    # the hood's mouth: the band and hem round the opening sit in the hood's own shadow, no ring brighter than the hood.
    # (Those faces hold few texels: the weave drawn in texture space came out as wide hatching, and the sheen lit it.)
    q = (PMu - HOOD_FRAME["H"]) @ HOOD_FRAME["R"]
    rr = np.sqrt((q[..., 0] / 0.31) ** 2 + (q[..., 2] / 0.37) ** 2)
    t_r = np.clip((rr - 0.62) / 0.38, 0, 1)
    t_y = np.clip((q[..., 1] - 0.03) / 0.10, 0, 1)
    mouth = (1 - t_r * t_r * (3 - 2 * t_r)) * (1 - t_y * t_y * (3 - 2 * t_y)) * (cat == CATS.index("cloth"))
    alb = alb * (1 - 0.68 * mouth[..., None])
    sh = sh * (1 - 0.92 * mouth)
    # value band check: twill stays <= #3B (sRGB 0.231)
    A = cr.srgb_encode(alb)
    cloth_vals = A[clothy & cov].max(axis=-1) if (clothy & cov).any() else np.zeros(1)
    log("twill albedo: cloth p99 sRGB %.3f (band <= 0.231 = #3B), mean %.3f" % (float(np.percentile(cloth_vals, 99)), float(cloth_vals.mean())))
    weave = twill_weave(H, W_, 4.0)
    Nd = cr.normal_from_height(weave * clothy * (1 - mouth) * 0.35, 1.0)
    Nf = cr.blend_normals(Nb, Nd)
    Nf[~cov] = np.array([0, 0, 1.0])
    S = np.stack([np.clip(smooth, 0, 1), np.clip(AOu, 0, 1), np.zeros_like(smooth), sh], axis=-1)
    A = cr.dilate(A, cov, 8)
    S = cr.dilate(S, cov, 8)
    cr.save_png(A, os.path.join(textures, TWILL + "_A.png"), "sRGB", "twA")
    cr.save_png(Nf * 0.5 + 0.5, os.path.join(textures, TWILL + "_N.png"), "Non-Color", "twN")
    cr.save_png(S, os.path.join(textures, TWILL + "_S.png"), "Non-Color", "twS")
    log("twill maps %d^2 written (coverage %.1f %%)" % (H, 100.0 * cov.mean()))


if __name__ == "__main__":
    main(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
