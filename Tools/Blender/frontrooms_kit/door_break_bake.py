"""The FINAL's debris throws, baked offline (door-break prototype DB2, 2026-10-30; production P1 the same day: FOUR throw
variants per family, Red's "4 预算" = baked throws, 4 variants; research/door_break/50_rethink.md 3.7 "Debris bake (RE7
style)", 4.3 item 3; 52_production.md).

Run (after interact_door_leaf_veneer_da_break.py; it rebuilds the same debris from the same seed):
  /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup \
      --python Tools/Blender/frontrooms_kit/door_break_bake.py -- [--out-root <unity project>] [--seed 1] [--method auto|rb|ballistic]

P1: --variants N (default 4) bakes Kit_DoorLeafBreak_Veneer_DA_throw0..N-1.json, each from its own launch seed, on motif
0's debris (the splinters' and crumbs' shapes are the family's; only their roots follow a motif). The keys are applied
RELATIVE to the bake's rests ("relative": the run time plays rest_motif + (key - rest0), and the key's rotation), so one
throw fits every motif. --method defaults to ballistic: the Blender rigid-body bake failed its own gates in the
prototype (keys below the floor, no settle; 51_prototype.md 3).
Writes Assets/Resources/Props/Models/Kit_DoorLeafBreak_Veneer_DA_throw<k>.json (LF):
  hz 30, t = 0 at the FINAL blow, breakAt 0.1 (the map's BreakDoor), pieces[] {name, rest, keys[x y z qx qy qz qw ...]},
  settle (s after FINAL), contacts[] {t, piece, speed, surface} (the sound line's DebrisImpact ask S3), checks{}.
Frame: door root D, canonical +X burst (the burst side is +X; the run time mirrors X for a -X burst).

Method:
  rb         Blender rigid bodies (Bullet): the leaf is a kinematic notched slab keyed on the map's throw (0 -> 105 deg
             in 0.12 s from 0.1 s after FINAL, then 0.18 s back to 80 deg, about the D1.6 visual pivot); floor, saddle
             and the wall slab around the opening are passive. Launch velocities are given by two animated frames
             before the bodies go dynamic.
  ballistic  the fallback: scripted ballistics with analytic floor, wall slab and the swinging leaf (its notch lets
             the strip through), carpet friction, rest flat on the largest face.
  auto       rb, then the gates (no key below the floor or inside the wall slab, settle <= 1.6 s, every piece moved);
             ballistic if rb fails any of them.
"""

import json
import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(HERE, "assets"))

import interact_door_break_common as bc  # noqa: E402
import interact_door_leaf_veneer_da_break as vb  # noqa: E402

# P3 (2026-10-30): --family steel bakes Kit_DoorLeafBreak_Steel_DA_throw<k> from interact_door_leaf_steel_da_break's debris
# (enamel chips, gypsum crumb clusters): the leaf has no notch (it stays whole), so nothing passes through it
FAMILY = {"mod": vb, "notch": True}

HZ_OUT = 30
SIM_HZ = 240
DURATION = 2.0
BREAK_AT = 0.10                     # FinalBlowLead
THROW_S, BOUNCE_S = 0.12, 0.18      # DoorBreak.ThrowSeconds, MapWorld.DoorBreakBounceSeconds
THROW_DEG, REST_DEG = 105.0, 80.0
PIVOT_Z = 0.036
WALL_HALF = 0.08
OPEN_Z0, OPEN_Z1 = 0.0, 0.998       # the opening between the linings
G = 9.81


def leaf_angle(t):
    """The map's throw (degrees toward +X) at t seconds after FINAL."""
    if t <= BREAK_AT:
        return 0.0
    u = t - BREAK_AT
    if u <= THROW_S:
        return THROW_DEG * u / THROW_S
    v = min(1.0, (u - THROW_S) / BOUNCE_S)
    return THROW_DEG + (REST_DEG - THROW_DEG) * (1 - (1 - v) * (1 - v))


def launch(name, rest, rng):
    """Launch velocity (m/s) and spin (rad/s) per piece kind (50_rethink 3.7; steel 3.5 / 54 4.3)."""
    if name.startswith("deb_chip"):
        # enamel flakes knocked off the crushed lock edge: light, fast spin, mostly out and down
        s = 0.5 + 1.2 * rng.random()
        v = (s * 0.8, s * rng.uniform(-0.1, 0.5), s * rng.uniform(-0.3, 0.25))
        w = tuple(rng.uniform(-30, 30) for _ in range(3))
        return v, w
    if name.startswith("deb_gyp"):
        # gypsum from the frame anchors behind the strike: pops out of the jamb into the opening and falls
        s = 0.3 + 0.9 * rng.random()
        v = (s * 0.6, s * rng.uniform(-0.4, 0.2), s * rng.uniform(-0.6, 0.0))
        w = tuple(rng.uniform(-12, 12) for _ in range(3))
        return v, w
    if name == "deb_strip":
        v = (0.5 + 1.0 * rng.random(), 0.25 + 0.3 * rng.random(), rng.uniform(-0.15, 0.05))
        w = (rng.uniform(-2, 2), rng.uniform(-3, 3), rng.uniform(-6, -2))
    elif name.startswith("deb_spl"):
        s = 1.2 + 1.8 * rng.random()
        v = (s * 0.85, s * rng.uniform(0.05, 0.4), s * rng.uniform(-0.15, 0.3))
        w = tuple(rng.uniform(-25, 25) for _ in range(3))
    else:
        s = 0.6 + 1.2 * rng.random()
        v = (s * 0.6, s * rng.uniform(-0.3, 0.2), s * rng.uniform(-0.1, 0.35))
        w = tuple(rng.uniform(-15, 15) for _ in range(3))
    return v, w


# ------------------------------------------------------------------------------------------- quaternion math
def qmul(a, b):
    ax, ay, az, aw = a
    bx, by, bz, bw = b
    return (aw * bx + ax * bw + ay * bz - az * by, aw * by - ax * bz + ay * bw + az * bx,
            aw * bz + ax * by - ay * bx + az * bw, aw * bw - ax * bx - ay * by - az * bz)


def qaxis(axis, ang):
    s = math.sin(ang * .5)
    return (axis[0] * s, axis[1] * s, axis[2] * s, math.cos(ang * .5))


def qrot(q, v):
    x, y, z, w = q
    qv = (x, y, z)
    t = bc.mul(bc.cross(qv, v), 2.0)
    return bc.add(bc.add(v, bc.mul(t, w)), bc.cross(qv, t))


def qnorm(q):
    l = math.sqrt(sum(c * c for c in q)) or 1.0
    return tuple(c / l for c in q)


def qslerp(a, b, t):
    d = sum(x * y for x, y in zip(a, b))
    if d < 0:
        b = tuple(-c for c in b)
        d = -d
    if d > 0.9995:
        return qnorm(tuple(x + (y - x) * t for x, y in zip(a, b)))
    th = math.acos(d)
    s = math.sin(th)
    return tuple((math.sin((1 - t) * th) * x + math.sin(t * th) * y) / s for x, y in zip(a, b))


def mat_to_q(m):
    """Rotation matrix (rows) -> (x, y, z, w)."""
    tr = m[0][0] + m[1][1] + m[2][2]
    if tr > 0:
        s = math.sqrt(tr + 1.0) * 2
        return qnorm(((m[2][1] - m[1][2]) / s, (m[0][2] - m[2][0]) / s, (m[1][0] - m[0][1]) / s, 0.25 * s))
    if m[0][0] > m[1][1] and m[0][0] > m[2][2]:
        s = math.sqrt(1.0 + m[0][0] - m[1][1] - m[2][2]) * 2
        return qnorm((0.25 * s, (m[0][1] + m[1][0]) / s, (m[0][2] + m[2][0]) / s, (m[2][1] - m[1][2]) / s))
    if m[1][1] > m[2][2]:
        s = math.sqrt(1.0 + m[1][1] - m[0][0] - m[2][2]) * 2
        return qnorm(((m[0][1] + m[1][0]) / s, 0.25 * s, (m[1][2] + m[2][1]) / s, (m[0][2] - m[2][0]) / s))
    s = math.sqrt(1.0 + m[2][2] - m[0][0] - m[1][1]) * 2
    return qnorm(((m[0][2] + m[2][0]) / s, (m[1][2] + m[2][1]) / s, 0.25 * s, (m[1][0] - m[0][1]) / s))


# ------------------------------------------------------------------------------------------- shared helpers
def local_verts(geo, origin):
    return [bc.sub(p, origin) for p in geo.v]


def lowest(verts, q, pos):
    return min(qrot(q, v)[1] for v in verts) + pos[1]


def in_wall(p):
    """Inside the wall slab around the opening (the jambs), below the head."""
    return abs(p[0]) < WALL_HALF - 0.002 and (p[2] < OPEN_Z0 - 0.002 or p[2] > OPEN_Z1 + 0.002) and p[1] < 2.2


def leaf_local(p, ang_deg):
    """A point in the leaf's own frame (Z along the leaf from the visual pivot, X its normal) at a throw angle."""
    a = math.radians(ang_deg)
    dx, dz = p[0], p[2] - PIVOT_Z
    # the leaf direction from the pivot is (sin a, cos a) in (x, z); its normal toward +X burst is (cos a, -sin a)
    along = dx * math.sin(a) + dz * math.cos(a)
    across = dx * math.cos(a) - dz * math.sin(a)
    return along + PIVOT_Z, across


# ------------------------------------------------------------------------------------------- ballistic fallback
def bake_ballistic(pieces, seed):
    rng = random.Random(4400 + seed)
    dt = 1.0 / SIM_HZ
    steps = int(DURATION * SIM_HZ)
    state = []
    for name, geo, origin in pieces:
        v, w = launch(name, origin, rng)
        verts = local_verts(geo, origin)
        ext = [max(abs(p[k]) for p in verts) for k in range(3)]
        state.append(dict(name=name, p=origin, q=(0.0, 0.0, 0.0, 1.0), v=v, w=w, verts=verts, ext=ext, rest=False,
                          flat=None, keys=[], contacts=0, moved=0.0, rad=max(ext)))
    contacts = []
    settle = None
    prev_ang = 0.0
    for s in range(steps + 1):
        t = s * dt
        if s % (SIM_HZ // HZ_OUT) == 0:
            for st in state:
                st["keys"].append(list(st["p"]) + list(st["q"]))
        if s == steps:
            break
        ang = leaf_angle(t + dt)
        omega = math.radians(ang - prev_ang) / dt
        prev_ang = ang
        all_slow = True
        for st in state:
            if st["rest"]:
                continue
            p, v, w, q = st["p"], st["v"], st["w"], st["q"]
            v = (v[0] * (1 - 0.15 * dt), v[1] - G * dt, v[2] * (1 - 0.15 * dt))
            np_ = bc.add(p, bc.mul(v, dt))
            wl = math.sqrt(sum(c * c for c in w))
            if wl > 1e-6:
                q = qnorm(qmul(qaxis(bc.mul(w, 1.0 / wl), wl * dt), q))
            # the swinging leaf: a slab 22 mm either side of its plane, notched at the latch block
            if abs(omega) > 1e-3 or ang > 0.5:
                along, across = leaf_local(np_, ang)
                notch = FAMILY["notch"] and along > 0.905 and bc.NOTCH[0] - 0.02 < np_[1] < bc.NOTCH[1] + 0.02
                if 0.005 < along < 0.995 and -0.03 < across < 0.022 + st["rad"] * 0.5 and not notch and 0.0 < np_[1] < 2.1:
                    r = along - PIVOT_Z
                    a = math.radians(ang)
                    lv = (omega * r * math.cos(a), 0.0, -omega * r * math.sin(a))
                    nrm = (math.cos(a), 0.0, -math.sin(a))
                    if bc.dot(bc.sub(lv, v), nrm) > 0 and abs(omega) > 1e-3:
                        # a glancing knock from the leaf: soft wood and a notched leaf pass on a quarter of its speed, capped
                        push = bc.add(bc.mul(v, 0.5), bc.mul(lv, 0.25))
                        sp = math.sqrt(bc.dot(push, push))
                        v = bc.mul(push, min(1.0, 2.5 / max(sp, 1e-6)))
                        # one leaf contact per piece, on its own count, so a leaf's run of glancing knocks never uses up the carpet's
                        if not st.get("leafed"):
                            contacts.append({"t": round(t, 4), "piece": st["name"], "speed": round(math.sqrt(bc.dot(lv, lv)), 3), "surface": "leaf"})
                            st["leafed"] = True
                    # never inside the leaf, moving or at rest
                    np_ = bc.add(np_, bc.mul(nrm, 0.022 + st["rad"] * 0.5 - across))
            # the wall slab beside the opening
            if in_wall(np_):
                v = (-v[0] * 0.3, v[1], v[2] * 0.6)
                np_ = (math.copysign(WALL_HALF, np_[0] if abs(np_[0]) > 1e-6 else 1.0), np_[1], np_[2])
                if st["contacts"] < 4:
                    contacts.append({"t": round(t, 4), "piece": st["name"], "speed": round(math.sqrt(bc.dot(v, v)), 3), "surface": "wall"})
                    st["contacts"] += 1
            # the carpet
            low = lowest(st["verts"], q, np_)
            if low < 0.0:
                np_ = (np_[0], np_[1] - low, np_[2])
                if v[1] < 0:
                    speed = math.sqrt(bc.dot(v, v))
                    if speed > 0.25 and st["contacts"] < 4:
                        contacts.append({"t": round(t, 4), "piece": st["name"], "speed": round(speed, 3), "surface": "carpet"})
                        st["contacts"] += 1
                    v = (v[0] * 0.45, -v[1] * 0.22, v[2] * 0.45)
                    w = bc.mul(w, 0.5)
                # carpet friction while touching
                hv = math.sqrt(v[0] * v[0] + v[2] * v[2])
                if hv > 1e-6:
                    k = max(0.0, hv - 0.85 * G * dt) / hv
                    v = (v[0] * k, v[1], v[2] * k)
                w = bc.mul(w, 0.9)
                # settle: slow and low -> ease to lying on its largest face
                if math.sqrt(bc.dot(v, v)) < 0.05 and abs(v[1]) < 0.05:
                    if st["flat"] is None:
                        st["flat"] = flat_of(q, st["ext"])
                    q = qslerp(q, st["flat"], 0.25)
                    low = lowest(st["verts"], q, np_)
                    np_ = (np_[0], np_[1] - low, np_[2])
                    w = (0.0, 0.0, 0.0)
                    if sum(abs(a - b) for a, b in zip(q, st["flat"])) < 1e-3 or -sum(a * b for a, b in zip(q, st["flat"])) > 0.9999:
                        st["rest"] = True
                        v = (0.0, 0.0, 0.0)
            st["moved"] += math.sqrt(bc.dot(bc.sub(np_, p), bc.sub(np_, p)))
            st.update(p=np_, v=v, w=w, q=q)
            if not st["rest"]:
                all_slow = False
        if all_slow and settle is None and t > BREAK_AT:
            settle = t
    return state, contacts, settle


def flat_of(q, ext):
    """The rotation nearest q that puts the piece's thinnest local axis vertical (lying on its largest face)."""
    thin = min(range(3), key=lambda k: ext[k])
    axis = [0.0, 0.0, 0.0]
    axis[thin] = 1.0
    world = qrot(q, tuple(axis))
    up = (0.0, 1.0 if world[1] >= 0 else -1.0, 0.0)
    c = bc.cross(world, up)
    s = math.sqrt(bc.dot(c, c))
    d = max(-1.0, min(1.0, bc.dot(world, up)))
    if s < 1e-6:
        return q
    corr = qaxis(bc.mul(c, 1.0 / s), math.atan2(s, d))
    return qnorm(qmul(corr, q))


# ------------------------------------------------------------------------------------------- Blender rigid bodies
def bake_rb(pieces, seed):
    import bpy
    from mathutils import Matrix
    rng = random.Random(4400 + seed)
    bc.reset_scene()
    scene = bpy.context.scene
    fps = 120
    scene.render.fps = fps
    frames = int(DURATION * fps) + 4
    scene.frame_start, scene.frame_end = 1, frames
    bpy.ops.rigidbody.world_add()
    rw = scene.rigidbody_world
    rw.substeps_per_frame = 4
    rw.solver_iterations = 20
    rw.point_cache.frame_start, rw.point_cache.frame_end = 1, frames

    def passive_box(name, lo, hi):
        g = bc.box(lo, hi, bc.CORE)
        o = bc.to_blender(g, name)
        scene.frame_set(1)
        bpy.context.view_layer.objects.active = o
        bpy.ops.rigidbody.object_add(type="PASSIVE")
        o.rigid_body.collision_shape = "BOX"
        o.rigid_body.friction = 0.9
        o.rigid_body.restitution = 0.1
        return o
    passive_box("floor", (-4, -0.2, -4), (4, 0.0, 5))
    passive_box("jamb_hinge", (-WALL_HALF, 0, -2.0), (WALL_HALF, 2.4, OPEN_Z0))
    passive_box("jamb_latch", (-WALL_HALF, 0, OPEN_Z1), (WALL_HALF, 2.4, 3.0))
    # the kinematic notched leaf: three boxes keyed on the throw about the visual pivot
    leaf_parts = [((-0.022, 0.015, 0.005), (0.022, 2.095, 0.902)),
                  ((-0.022, 0.015, 0.902), (0.022, bc.NOTCH[0] - 0.012, 0.995)),
                  ((-0.022, bc.NOTCH[1] + 0.012, 0.902), (0.022, 2.095, 0.995))]
    leaf_objs = []
    for k, (lo, hi) in enumerate(leaf_parts):
        o = bc.to_blender(bc.box(lo, hi, bc.VENEER), "leaf_%d" % k)
        bpy.context.view_layer.objects.active = o
        bpy.ops.rigidbody.object_add(type="PASSIVE")
        o.rigid_body.kinematic = True
        o.rigid_body.collision_shape = "BOX"
        leaf_objs.append(o)
    pivot = Matrix.Translation((0.0, -PIVOT_Z, 0.0))
    for f in range(1, frames + 1):
        t = (f - 3) / fps
        ang = leaf_angle(max(0.0, t))
        m = pivot @ Matrix.Rotation(math.radians(-ang), 4, "Z") @ pivot.inverted()
        for o in leaf_objs:
            o.matrix_world = m
            o.keyframe_insert("location", frame=f)
            o.keyframe_insert("rotation_euler", frame=f)
    objs = []
    for name, geo, origin in pieces:
        o = bc.to_blender(geo, name, origin=origin)
        bpy.context.view_layer.objects.active = o
        bpy.ops.rigidbody.object_add(type="ACTIVE")
        rb = o.rigid_body
        rb.collision_shape = "CONVEX_HULL"
        rb.friction, rb.restitution = 0.9, 0.15
        rb.linear_damping, rb.angular_damping = 0.25, 0.5
        rb.collision_margin = 0.0005
        lo, hi = geo.bounds()
        vol = max(1e-7, (hi[0] - lo[0]) * (hi[1] - lo[1]) * (hi[2] - lo[2]) * 0.6)
        rb.mass = max(0.002, 650.0 * vol)
        v, w = launch(name, origin, rng)
        # launch: animated on frames 1-2 moving at v (Blender), dynamic from frame 3
        vb_ = bc.dc.U(*v)
        base = o.location.copy()
        for f, k in ((1, -2), (2, -1), (3, 0)):
            o.location = (base[0] + vb_[0] * k / fps, base[1] + vb_[1] * k / fps, base[2] + vb_[2] * k / fps)
            o.keyframe_insert("location", frame=f)
            rb.kinematic = f < 3
            rb.keyframe_insert("kinematic", frame=f)
        o.location = base
        objs.append((name, o, origin))
    M = [[-1, 0, 0], [0, 0, 1], [0, -1, 0]]        # Blender -> Unity (to_unity)
    keys = {name: [] for name, _o, _origin in objs}
    vel = {name: (0.0, 0.0, 0.0) for name, _o, _origin in objs}
    prevp = {name: origin for name, _o, origin in objs}
    contacts = []
    settle = None
    for f in range(1, frames + 1):
        scene.frame_set(f)
        t = (f - 3) / fps
        if t < 0:
            continue
        slow = True
        for name, o, origin in objs:
            mw = o.matrix_world
            p = bc.dc.to_unity(mw.translation)
            rb3 = mw.to_3x3()
            R = [[rb3[i][j] for j in range(3)] for i in range(3)]
            # R_u = M R_b M^T (M is its own inverse transpose here)
            Ru = [[sum(M[i][a] * R[a][b] * M[j][b] for a in range(3) for b in range(3)) for j in range(3)] for i in range(3)]
            q = mat_to_q(Ru)
            nv = bc.mul(bc.sub(p, prevp[name]), fps)
            if bc.dot(nv, nv) > 0.0004:
                slow = False
            if vel[name][1] < -0.4 and nv[1] > -0.05 and p[1] < 0.06 and len([c for c in contacts if c["piece"] == name]) < 4:
                contacts.append({"t": round(t, 4), "piece": name, "speed": round(math.sqrt(bc.dot(vel[name], vel[name])), 3), "surface": "carpet"})
            vel[name] = nv
            prevp[name] = p
            if (f - 3) % (fps // HZ_OUT) == 0:
                keys[name].append(list(p) + list(q))
        if slow and settle is None and t > BREAK_AT + 0.2:
            settle = t
    state = [dict(name=name, keys=keys[name], moved=1.0) for name, _o, _origin in objs]
    return state, contacts, settle


# ------------------------------------------------------------------------------------------- gates + output
def gates(state, pieces, settle):
    by = {n: (g, o) for n, g, o in pieces}
    below = inwall = 0
    unmoved = []
    for st in state:
        g, origin = by[st["name"]]
        verts = local_verts(g, origin)
        first = st["keys"][0]
        last = st["keys"][-1]
        if math.sqrt(sum((a - b) ** 2 for a, b in zip(first[:3], last[:3]))) < 0.05:
            unmoved.append(st["name"])
        for k in st["keys"]:
            p, q = tuple(k[:3]), tuple(k[3:])
            if lowest(verts, q, p) < -0.004:
                below += 1
            if in_wall(p):
                inwall += 1
    ok = below == 0 and inwall == 0 and settle is not None and settle <= 1.6 and not unmoved
    return ok, {"keysBelowFloor": below, "keysInWall": inwall, "settle": settle, "unmoved": unmoved}


def main(argv):
    out_root = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
    seed, method, variants = 1, "ballistic", 4
    i = 0
    while i < len(argv):
        if argv[i] == "--out-root":
            out_root = argv[i + 1]; i += 2; continue
        if argv[i] == "--seed":
            seed = int(argv[i + 1]); i += 2; continue
        if argv[i] == "--method":
            method = argv[i + 1]; i += 2; continue
        if argv[i] == "--variants":
            variants = int(argv[i + 1]); i += 2; continue
        if argv[i] == "--family":
            if argv[i + 1] == "steel":
                import interact_door_leaf_steel_da_break as sb
                FAMILY.update(mod=sb, notch=False)
            i += 2; continue
        i += 1
    fm = FAMILY["mod"]
    geos, _info, _motif = fm.build_all(seed)
    pieces = [(n, g, o) for n, (g, o) in geos.items() if o is not None]
    for var in range(variants):
        vseed = seed + 97 * var
        log = []
        used = None
        if method in ("auto", "rb"):
            try:
                state, contacts, settle = bake_rb(pieces, vseed)
                ok, chk = gates(state, pieces, settle)
                log.append("rb: %s %s" % ("PASS" if ok else "FAIL", chk))
                if ok or method == "rb":
                    used = "rigidbody"
            except Exception as e:  # noqa: BLE001
                log.append("rb: EXCEPTION %r" % (e,))
                if method == "rb":
                    raise
        if used is None:
            state, contacts, settle = bake_ballistic(pieces, vseed)
            ok, chk = gates(state, pieces, settle)
            log.append("ballistic: %s %s" % ("PASS" if ok else "FAIL", chk))
            used = "ballistic"
        rests = dict((n, o) for n, _g, o in pieces)
        out = {
            "name": "%s_throw%d" % (fm.NAME, var),
            "method": used,
            "seed": vseed,
            "variant": var,
            "relative": True,
            "hz": HZ_OUT,
            "t0": "FINAL blow",
            "breakAt": BREAK_AT,
            "throw": {"deg": THROW_DEG, "seconds": THROW_S, "bounceSeconds": BOUNCE_S, "restDeg": REST_DEG, "pivotZ": PIVOT_Z},
            "frames": len(state[0]["keys"]),
            "settle": settle if settle is not None else -1.0,
            "burst": "+X",
            "pieces": [{"name": st["name"], "rest": [round(c, 6) for c in rests[st["name"]]],
                        "keys": [round(c, 5) for k in st["keys"] for c in k]} for st in state],
            "contacts": sorted(contacts, key=lambda c: c["t"]),
            "checks": chk,
            "log": log,
        }
        path = os.path.join(out_root, "Assets", "Resources", "Props", "Models", "%s_throw%d.json" % (fm.NAME, var))
        with open(path, "w", newline="\n") as f:
            json.dump(out, f, separators=(",", ":"))
        for line in log:
            print("[bake] v%d %s" % (var, line))
        print("[bake] wrote %s: method %s, %d pieces x %d keys, settle %s s, %d contacts, %d bytes" % (
            os.path.basename(path), used, len(state), out["frames"], settle, len(contacts), os.path.getsize(path)))


if __name__ == "__main__":
    main(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
