"""The Run! T2 re-stack's two clips, baked offline (71_run_room.md s.5.3, decision 5; card 2, the T2 body, 2026-11-04).
The door break's method (research/door_break/50_rethink.md 3.7, 52_production.md P1): pre-built pieces, motion baked to
30 Hz keys with a contact list for sound, one change per beat, the at-rest wall a single static mesh again at the seat.

  /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup \
      --python Tools/Blender/frontrooms_kit/run_restack_bake.py -- [--out-root <unity project>]
  (plain python3 runs it too: it needs no bpy; Blender is the kit pipeline's interpreter)

Writes Assets/Resources/Props/Models/Kit_RunRestack_{Blocks,Strips}_{flinch,step}.json (LF):
  hz 30, frames, duration, phases [tell, travel, seat] (s), distance (m), pieces[] {name, index, course, side, rest[3],
  lift, land, keys[x y z qx qy qz qw ...]} (head-local: the end's own frame, x across, y up, z along the corridor, +z away
  from the player; Unity axes and quaternions), contacts[] {t, kind, index, course, speed, x, y, z} for 声音设计's cues and
  the grit, checks{} (the gates below).
Clips: flinch = the two-cell first step (tell 0.25, travel 1.1, seat 0.2 s, 6 m), step = a one-cell step (0.15 / 0.8 /
0.2 s, 3 m), the schedule's T2 timings (FrontRoomsRunRecede.cs, RunRecedeSchedule). The final step (0.25 / 1.0 / 0.35) and
the flinch alone (k = 2: seat 0.35) play these clips time-warped per phase; the seat is each clip's end pose (rest).

The choreography (71 s.5.3: "top out, bottom in, left left, right right, every block landing on a beat"):
  * The tell: the magnets let go, the mortar cracks; the courses come out from the top, the outer piece of a course first.
    Each piece lifts out of its bed (22 mm + 5 mm a course; the top course, under the girder, pulls straight out first),
    turns a few degrees, and joins its side's stream: the left half left, the right half right, never across the centre.
  * The streams hug the walls: every piece slides out to the wall lane (its outer edge on the bay's 1.295 m line) and the
    pieces of a course queue along the corridor 0.23 m apart (outer piece in front), each course on its own row; the two
    courses over the lintel rise into the soffit corners (inner edge past 0.895 m, clear of the main, the tray and its rods).
  * They hang back while the end rolls away: a detached piece's place along the corridor is the end's own path a little
    late (S(t) = H(t - delta(t)), delta a smooth bump, delta' < 1), so it only ever moves away from the player and never
    in front of the line it left; the door, its frame, the lintel and the steel roll back alone, upright, untouched.
  * They land from the bottom up, the inner piece of a course first, each dropping the last 20 mm into its bed with a
    4 mm settle; the last course lands into the seat (the heavier knock on the final step is the head's own seat).
Gates (checks{}; the bake fails loudly): first and last frame exactly at rest; no two pieces interpenetrate (oriented boxes,
SAT, 1 mm tolerance for the touching rest faces); clear of the door zone (|x| < 0.551 under 2.20 m: the door, its lite
and the walk line), the sprinkler main and its heads, the ladder tray and its rods, the strip lamps, the carried lamp's arm
and cabinet, the tracks and trolleys, the walls' rail, holders and the end's armature brackets; along the corridor a piece
never moves toward the player by more than 2 cm after its peel and never goes more than 1.0 m in front of the line it left.
"""

import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "assets"))

import _run_restack_common as RS  # noqa: E402

HZ = 30
ACCEL, DECEL = 0.20, 0.15                  # RunRecedeSchedule.Accel / Decel
CLIPS = {
    "flinch": {"tell": 0.25, "travel": 1.10, "seat": 0.20, "distance": 6.0},
    "step": {"tell": 0.15, "travel": 0.80, "seat": 0.20, "distance": 3.0},
}
LANE_OUT = 1.287        # a piece's outer edge in the stream (the bay's outer line; the armature brackets begin at 1.30)
TRAIN = 0.23            # queue spacing along the corridor (piece depth 0.164 + 66 mm)
SERVICE_LANE = 0.895    # inner edge of a piece above 2.53 m (tray and its rods end at 0.865, the main at 0.64)
POP = 0.045             # out of the wall plane at the peel (head-local -z)
SETTLE = 0.004


# ---------------------------------------------------------------------------------------------------- small math
def smooth(u):
    u = max(0.0, min(1.0, u))
    return u * u * (3.0 - 2.0 * u)


def smoother(u):
    u = max(0.0, min(1.0, u))
    return u * u * u * (u * (u * 6.0 - 15.0) + 10.0)


def travel_at(u, total, length):
    """RunRecedeSchedule.TravelAt: accelerate 0.20 s, cruise, decelerate 0.15 s."""
    if length <= 0.0 or total <= 0.0:
        return 0.0
    u = max(0.0, min(total, u))
    v = length / max(0.01, total - (ACCEL + DECEL) * 0.5)
    if u <= ACCEL:
        return v * u * u / (2.0 * ACCEL)
    if u <= total - DECEL:
        return v * ACCEL * 0.5 + v * (u - ACCEL)
    r = total - u
    return length - v * r * r / (2.0 * DECEL)


def qmul(a, b):
    ax, ay, az, aw = a
    bx, by, bz, bw = b
    return (aw * bx + ax * bw + ay * bz - az * by,
            aw * by - ax * bz + ay * bw + az * bx,
            aw * bz + ax * by - ay * bx + az * bw,
            aw * bw - ax * bx - ay * by - az * bz)


def qaxis(axis, deg):
    h = math.radians(deg) * 0.5
    s = math.sin(h)
    return (axis[0] * s, axis[1] * s, axis[2] * s, math.cos(h))


def qeuler(x, y, z):
    """Unity's Quaternion.Euler(x, y, z): z, then x, then y."""
    return qmul(qaxis((0, 1, 0), y), qmul(qaxis((1, 0, 0), x), qaxis((0, 0, 1), z)))


def qrot(q, v):
    """q * v (Unity's Quaternion * Vector3)."""
    x, y, z, w = q
    vx, vy, vz = v
    tx = 2.0 * (y * vz - z * vy)
    ty = 2.0 * (z * vx - x * vz)
    tz = 2.0 * (x * vy - y * vx)
    return (vx + w * tx + (y * tz - z * ty), vy + w * ty + (z * tx - x * tz), vz + w * tz + (x * ty - y * tx))


def jit(i, seed):
    h = (i * 2654435761 + seed * 40503 + 977) & 0xFFFFFFFF
    h ^= h >> 15
    h = (h * 2246822519) & 0xFFFFFFFF
    h ^= h >> 13
    return (h & 0xFFFF) / 32767.5 - 1.0


# ---------------------------------------------------------------------------------------------------- the choreography
LIFT = 0.020            # out of the bed
ROW = 0.006             # each course 6 mm more than the one below (room for a few degrees of roll)
TOP_RISE = 0.072        # the top course, in the wall lane, rises into the soffit corner
POP_TOP = 0.25          # the top course, under the girder, pulls straight out this far before it may rise
SEP = 0.20              # depth between two pieces of a row before either slides sideways (depth 0.164 + 36 mm)
HOLD_RANK = 0.25        # each rank further from the door holds this much longer (m of the end's travel)


class Clip(object):
    def __init__(self, name, spec, pieces, phone):
        self.name = name
        self.tell, self.trav, self.seat, self.dist = spec["tell"], spec["travel"], spec["seat"], spec["distance"]
        self.total = self.tell + self.trav + self.seat
        self.frames = int(round(self.total * HZ)) + 1
        self.pieces = pieces
        self.phone = phone
        flinch = name == "flinch"
        # lifts: top course first, the outer piece of a course first
        self.lift0 = 0.04 if flinch else 0.03
        self.dlc = 0.036 if flinch else 0.021
        self.dlk = 0.042 if flinch else 0.026
        # how far the end rolls away from a held piece before it starts back (the door alone in the hole it left)
        self.hold = 1.30 if flinch else 0.95
        # landings: c0..c11 ripple from the bottom (inner piece first), then the top course last, into the seat
        self.top_land = self.total - (0.07 if flinch else 0.06)
        ripple_end = self.top_land - (0.36 if flinch else 0.30)
        self.land0 = ripple_end - (0.36 if flinch else 0.26)
        self.dac = (ripple_end - self.land0) / 11.0
        self.dak = 0.016
        for p in pieces:
            self.plan(p)
        self.by_course = {}
        for p in pieces:
            self.by_course.setdefault(p["course"], []).append(p)

    def head(self, t):
        """The end's displacement along the corridor from the step's line (the bones ride the end, so this is only used to
        keep a detached piece's WORLD path behind it)."""
        if t <= self.tell:
            return 0.0
        return travel_at(t - self.tell, self.trav, self.dist)

    def head_time(self, d):
        """The first time the end has travelled d (m)."""
        if d <= 0.0:
            return self.tell
        lo, hi = self.tell, self.tell + self.trav
        for _ in range(40):
            mid = (lo + hi) * 0.5
            if self.head(mid) < d:
                lo = mid
            else:
                hi = mid
        return hi

    def plan(self, p):
        c, k, kn, s = p["course"], p["k"], p["kn"], p["side"]
        L = p["x1"] - p["x0"]
        p["L"] = L
        p["cx"], p["cy"] = (p["x0"] + p["x1"]) / 2.0, (p["y0"] + p["y1"]) / 2.0
        rank = kn - 1 - k                       # 0: next to the door ... the outermost at the wall
        p["rank"] = rank
        p["t_lift"] = self.lift0 + (12 - c) * self.dlc + k * self.dlk
        if c == 12:
            p["t_land"] = self.top_land - (kn - 1 - rank) * 0.016
        else:
            p["t_land"] = self.land0 + c * self.dac + rank * self.dak
        flight = p["t_land"] - p["t_lift"]
        assert flight > 0.15, (p["name"], flight)
        p["t_arrive"] = p["t_land"] - 0.09
        # hold where it lifted while the end rolls away, then come back: the inner rank first (each rank further out holds
        # HOLD_RANK more of the end's travel), never later than 45 % of the way to its arrival, never with under 0.22 s left
        span = p["t_arrive"] - p["t_lift"]
        alpha = max(0.10, min(0.45, 0.10 + 0.40 * (flight - 0.15) / 0.90))
        t_cap = self.head_time(self.head(p["t_lift"]) + self.hold + HOLD_RANK * rank)
        p["t_go"] = max(p["t_lift"], min(p["t_lift"] + alpha * span, t_cap, p["t_arrive"] - 0.22))
        over = c >= 11
        # over the door: the carried lamp's flood (1.6 m out at about 2.5 m) runs down past these courses to the leaf, so
        # a piece over the door must never hang in it (it would shadow the lit door, which never dips, 71 s.5.7): it rides
        # just before the end, above the flood's top ray (the middle one rides with the door; the next goes out to its lane)
        centre = over and p["x0"] < 0.55 and p["x1"] > -0.55
        p["centre"] = centre
        p["pop"] = POP_TOP if c == 12 else POP
        if centre and rank == 0:
            p["cap"] = 0.12 if c == 11 else 0.30      # the piece over the door's middle rides with the door, never sideways
        elif centre:
            p["cap"] = 0.20 if c == 11 else 0.58      # the next one out: above the flood's top ray, then out to its lane
        else:
            p["cap"] = 99.0
        stream = (not self.phone) and (over or flight >= 0.35) and not (centre and rank == 0)
        xt = s * (LANE_OUT - L / 2.0)
        p["dx"] = (xt - p["cx"]) if stream else 0.0
        p["dy_target"] = (LIFT + ROW * c) if c <= 10 else 0.12
        if self.phone:
            p["dy_target"] = LIFT if c <= 10 else (0.08 if c == 11 else LIFT)
        i = p["index"]
        amp = 0.5 if self.phone else 1.0
        no_yaw = self.phone and over
        p["rot"] = (amp * 2.5 * jit(i, 21), 0.0 if no_yaw else amp * 8.0 * jit(i, 22), amp * jit(i, 23))

    def S(self, p, t):
        """The piece's place along the corridor (m from the step's line, world, its pop aside): with the end until its lift,
        held where it lifted, then back to the end (g: 0 -> 1, smootherstep), with it after its arrival."""
        tl = p["t_lift"]
        ht = self.head(t)
        if t <= tl or t >= p["t_arrive"]:
            return ht
        hl = self.head(tl)
        g = smoother((t - p["t_go"]) / max(1e-4, p["t_arrive"] - p["t_go"]))
        return max(hl + (ht - hl) * g, ht - p["cap"])

    def z_local(self, p, t):
        """Head-local z of the piece's centre (its pop included)."""
        tl, td = p["t_lift"], p["t_land"]
        if t <= tl or t >= td:
            return 0.0
        a, b = t - tl, td - t
        pop = p["pop"] * min(smooth(a / 0.10), smooth((b - 0.02) / 0.10))
        return self.S(p, t) - self.head(t) - pop

    def all_offsets(self, t):
        """{index: (dx, dy, dz, q)} for every piece at t: depth (hold and catch-up), the lanes and turns where the row's
        depths allow them, the rows' cascade (a course never higher than the one above it) and their roll."""
        z = {}
        for p in self.pieces:
            z[p["index"]] = self.z_local(p, t)
        # lanes: a piece slides sideways only while every other piece of its course on its side is SEP away in depth
        lane = {}
        turn = {}
        for c, row in self.by_course.items():
            for p in row:
                tl, td = p["t_lift"], p["t_land"]
                if t <= tl or t >= td:
                    lane[p["index"]] = 0.0
                    turn[p["index"]] = 0.0
                    continue
                sep_side, sep_near = 9.0, 9.0
                for q in row:
                    if q is p:
                        continue
                    dz = abs(z[p["index"]] - z[q["index"]])
                    inward = abs(p["cx"] + p["dx"]) < abs(p["cx"])
                    if q["side"] == p["side"] and (q["rank"] > p["rank"] or inward):
                        sep_side = min(sep_side, dz)          # the pieces between it and its wall (all of them, moving in)
                    if abs(q["cx"] - p["cx"]) < (q["L"] + p["L"]) / 2.0 + 0.06:
                        sep_near = min(sep_near, dz)
                a, b = t - tl, td - t
                guard = min(smooth((a - 0.10) / 0.10), smooth((b - 0.16) / 0.10))
                lane[p["index"]] = (smooth((sep_side - SEP) / 0.06) * guard) if p["dx"] != 0.0 else 0.0
                clear = smooth((-z[p["index"]] - 0.17) / 0.08)
                turn[p["index"]] = smooth((sep_near - 0.18) / 0.06) * clear * min(smooth((a - 0.08) / 0.12), smooth((b - 0.12) / 0.10))
        # the rows' cascade, top down: a piece never higher than any piece of the course above that overlaps it across and
        # in depth (a course leaves first and lands last, so its pieces are always out of the way first)
        dy = {}
        row_dy = {}
        xs = {}
        for p in self.pieces:
            xs[p["index"]] = p["cx"] + p["dx"] * lane[p["index"]]
        for c in range(RS.COURSES - 1, -1, -1):
            for p in self.by_course.get(c, []):
                v = self.lift_target(p, t, lane[p["index"]], z[p["index"]])
                for q in self.by_course.get(c + 1, []):
                    if abs(xs[q["index"]] - xs[p["index"]]) < (q["L"] + p["L"]) / 2.0 + 0.02 and abs(z[q["index"]] - z[p["index"]]) < 0.20:
                        v = min(v, dy[q["index"]])
                dy[p["index"]] = v
            vals = [dy[p["index"]] for p in self.by_course.get(c, []) if p["t_lift"] < t < p["t_land"]]
            row_dy[c] = min(vals) if vals else 0.0
        out = {}
        for p in self.pieces:
            out[p["index"]] = self.offset(p, t, z[p["index"]], dy[p["index"]], lane[p["index"]], turn[p["index"]], row_dy)
        return out

    def lift_target(self, p, t, env_lane, zl):
        tl, td = p["t_lift"], p["t_land"]
        if t <= tl or t >= td:
            return 0.0
        a, b = t - tl, td - t
        c = p["course"]
        hover = smoother(b / 0.07)
        if c == 12:
            # under the girder until its back face clears the header; the rise into the corner only in the lane; down to its
            # bare lift before the lane returns, and to its bed before it slides back in under the girder
            clear = smooth((-zl - 0.17) / 0.04)
            base = 0.012 * min(clear, smoother((b - 0.14) / 0.05))
            # the rise waits for the piece's inner edge to pass the main (0.64) and the tray (0.84): full at 0.88 m
            inner = abs(p["cx"] + p["dx"] * env_lane) - p["L"] / 2.0
            rise = (TOP_RISE + ROW * 12 - 0.012) * smooth((inner - 0.70) / 0.18) * min(clear, smoother((b - 0.22) / 0.08)) if not self.phone else 0.0
            return base + rise
        base = LIFT * min(smooth(a / 0.10), hover)
        row = (p["dy_target"] - LIFT) * min(smooth((a - 0.06) / 0.16), hover)
        return base + row

    def offset(self, p, t, zl, dy, env_lane, env_turn, row_dy):
        tl, td = p["t_lift"], p["t_land"]
        if t <= tl or t >= td + 0.10:
            return 0.0, 0.0, 0.0, (0.0, 0.0, 0.0, 1.0)
        c = p["course"]
        dz = zl
        dx = p["dx"] * env_lane
        if t > td:
            e = (t - td) / 0.10
            if c == 12:
                dz += 0.002 * math.sin(math.pi * e) * (1.0 - e)     # under the girder: it knocks home along the corridor
            else:
                dy += SETTLE * math.sin(math.pi * e) * (1.0 - e)
        above = row_dy.get(c + 1, 0.5) - dy if c < 12 else 0.5
        below = dy - row_dy.get(c - 1, 0.0) if c > 0 else dy
        gap = max(0.0, min(above, below) - 0.0008)
        half = max(0.05, p["L"] / 2.0)
        roll_ok = math.degrees(math.asin(min(0.9, gap * 0.45 / half)))
        rx, ry, rz = p["rot"]
        rz = max(-roll_ok, min(roll_ok, rz * 3.0))
        pitch_ok = math.degrees(math.asin(min(0.9, gap * 0.45 / 0.082)))
        rx = max(-pitch_ok, min(pitch_ok, rx))
        cx_now = p["cx"] + dx
        room = min(1.299 - (abs(cx_now) + half), (abs(cx_now) - half) - 0.533)
        yaw_ok = math.degrees(math.asin(max(0.0, min(0.9, room / 0.09))))
        ry = max(-yaw_ok, min(yaw_ok, ry))
        env_rot = env_turn if t <= td else 0.0
        if c == 12 and not self.phone:
            env_rot *= env_lane ** 2
        q = qeuler(rx * env_rot, ry * env_rot, rz * env_rot)
        return dx, dy, dz, q


# ---------------------------------------------------------------------------------------------------- checks
def box_axes(q):
    return [qrot(q, (1, 0, 0)), qrot(q, (0, 1, 0)), qrot(q, (0, 0, 1))]


def obb(p, off, shrink):
    dx, dy, dz, q = off
    c = (p["cx"] + dx, p["cy"] + dy, dz)
    h = (p["L"] / 2.0 - shrink, (p["y1"] - p["y0"]) / 2.0 - shrink, p["depth"] - shrink)
    return c, box_axes(q), h


def dot(a, b):
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def cross(a, b):
    return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])


def obb_overlap(A, B):
    """Penetration depth along the separating-axis set (<= 0: apart)."""
    ca, ua, ha = A
    cb, ub, hb = B
    d = (cb[0] - ca[0], cb[1] - ca[1], cb[2] - ca[2])
    axes = list(ua) + list(ub)
    for i in range(3):
        for j in range(3):
            ax = cross(ua[i], ub[j])
            n = math.sqrt(dot(ax, ax))
            if n > 1e-6:
                axes.append((ax[0] / n, ax[1] / n, ax[2] / n))
    pen = 1e9
    for ax in axes:
        ra = sum(ha[i] * abs(dot(ua[i], ax)) for i in range(3))
        rb = sum(hb[i] * abs(dot(ub[i], ax)) for i in range(3))
        sep = abs(dot(d, ax)) - (ra + rb)
        if sep > 0:
            return -sep
        pen = min(pen, -sep)
    return pen


def corners(box):
    c, u, h = box
    out = []
    for sx in (-1, 1):
        for sy in (-1, 1):
            for sz in (-1, 1):
                out.append((c[0] + u[0][0] * h[0] * sx + u[1][0] * h[1] * sy + u[2][0] * h[2] * sz,
                            c[1] + u[0][1] * h[0] * sx + u[1][1] * h[1] * sy + u[2][1] * h[2] * sz,
                            c[2] + u[0][2] * h[0] * sx + u[1][2] * h[1] * sy + u[2][2] * h[2] * sz))
    return out


# Forbidden regions: (name, x0, x1, y0, y1, z0, z1) in the end's frame (z: head-local, None = the whole length).
OBSTACLES = [
    ("door path", -0.530, 0.530, 0.0, 2.20, None, None),
    ("door frame and lintel", -0.5495, 0.5495, 0.0, 2.2, -0.10, 0.10),
    ("sprinkler main", 0.48, 0.62, 2.555, 3.0, None, None),
    ("ladder tray, strut and rods", -0.84, -0.36, 2.66, 3.0, None, None),
    ("strip lamps", -0.13, 0.13, 2.73, 3.0, None, None),
    ("track L", -1.32, -1.18, 2.82, 3.0, None, None),
    ("track R", 1.18, 1.32, 2.82, 3.0, None, None),
    ("wall L (rail, holders, EMT)", -3.0, -1.300, 0.0, 3.0, None, None),
    ("wall R (rail, holders)", 1.300, 3.0, 0.0, 3.0, None, None),
    ("floor", -3.0, 3.0, -1.0, 0.004, None, None),
    ("header and girder", -1.335, 1.335, 2.531, 3.0, -0.0855, 0.0855),
    ("sill L", -1.335, -0.50, 0.0, 0.0595, -0.0855, 0.0855),
    ("sill R", 0.50, 1.335, 0.0, 0.0595, -0.0855, 0.0855),
    ("edge channel L", -1.335, -1.2955, 0.02, 2.86, -0.0855, 0.0855),
    ("edge channel R", 1.2955, 1.335, 0.02, 2.86, -0.0855, 0.0855),
    ("arm plate and knee", -0.085, 0.085, 2.548, 2.71, -0.26, -0.07),
    ("arm tube", -0.025, 0.025, 2.595, 2.665, -1.70, -0.07),
    ("lamp cabinet and plate", -0.21, 0.21, 2.32, 2.72, -1.71, -1.50),
    ("trolleys L", -1.35, -1.12, 2.74, 3.0, -0.20, 0.20),
    ("trolleys R", 1.12, 1.35, 2.74, 3.0, -0.20, 0.20),
]
# The carried lamp's flood onto the leaf (head 0: from about (0, 2.55, -1.62) to the leaf |x| <= 0.5, y 0..2.1 at z 0),
# as slices along the beam: no piece may stand in it (the lit door never dips, 71 s.5.7).
for _k in range(24):
    _z0 = -1.56 + _k * 0.065
    _z1 = _z0 + 0.065
    _f = (-_z0) / 1.62                     # 1 at the lamp, 0 at the door (the slice's lamp-side end: the wider)
    _half = 0.50 * (1.0 - (-_z1) / 1.62) + 0.04 * _f + 0.01
    _top = 2.10 + 0.50 * _f + 0.01         # the top ray from the head (about 2.6 m) to the leaf's top (2.1 m)
    OBSTACLES.append(("lamp flood to the leaf", -_half, _half, 0.0, _top, _z0, _z1))


def inside(pt, ob, margin):
    _n, x0, x1, y0, y1, z0, z1 = ob
    if not (x0 - margin < pt[0] < x1 + margin and y0 - margin < pt[1] < y1 + margin):
        return False
    if z0 is not None and not (z0 - margin < pt[2] < z1 + margin):
        return False
    return True


def run_checks(clip, keys):
    """keys[p][f] = (dx, dy, dz, q). Returns the checks dict."""
    ps = clip.pieces
    n = len(ps)
    worst_pen, worst_pair = -1.0, None
    hits = {}
    min_clear_door = 9.0
    back_worst, front_worst = 0.0, 0.0
    back_who = [None]
    for f in range(clip.frames):
        boxes = [obb(ps[i], keys[i][f], 0.0008) for i in range(n)]
        moving = [i for i in range(n) if keys[i][f] != (0.0, 0.0, 0.0, (0.0, 0.0, 0.0, 1.0))]
        for a in range(n):
            for b in range(a + 1, n):
                if a not in moving and b not in moving:
                    continue
                ca, cb = boxes[a][0], boxes[b][0]
                if abs(ca[0] - cb[0]) > 0.5 or abs(ca[1] - cb[1]) > 0.4 or abs(ca[2] - cb[2]) > 0.5:
                    continue
                pen = obb_overlap(boxes[a], boxes[b])
                if pen > worst_pen:
                    worst_pen, worst_pair = pen, (ps[a]["name"], ps[b]["name"], f)
        for i in moving:
            for pt in corners(boxes[i]):
                for ob in OBSTACLES:
                    if inside(pt, ob, 0.0):
                        key = ob[0]
                        hits.setdefault(key, []).append((ps[i]["name"], f))
    # world path along the corridor
    for i, p in enumerate(ps):
        tl = p["t_lift"]
        prev = None
        lo = 0.0
        for f in range(clip.frames):
            t = f / float(HZ)
            w = clip.head(t) + keys[i][f][2]
            lo = min(lo, w)
            if t > tl + 0.12:
                if prev is not None and w < prev:
                    if prev - w > back_worst:
                        back_who[0] = (p["name"], f)
                    back_worst = max(back_worst, prev - w)
                prev = w if prev is None else max(prev, w)
        front_worst = min(front_worst, lo)
    return {
        "pieceOverlapWorstM": round(worst_pen, 5), "pieceOverlapPair": list(worst_pair) if worst_pair else [],
        "obstacleHits": {k: len(v) for k, v in hits.items()},
        "obstacleFirst": {k: [v[0][0], v[0][1]] for k, v in hits.items()},
        "worldBackWorstM": round(back_worst, 5), "worldBackWho": list(back_who[0]) if back_who[0] else [], "inFrontOfLineWorstM": round(front_worst, 5),
    }


# ---------------------------------------------------------------------------------------------------- bake
def bake(kit, pieces, phone, out_dir):
    results = {}
    for name, spec in CLIPS.items():
        clip = Clip(name, spec, [dict(p) for p in pieces], phone)
        per_frame = [clip.all_offsets(f / float(HZ)) for f in range(clip.frames)]
        keys = [[per_frame[f][p["index"]] for f in range(clip.frames)] for p in clip.pieces]
        # exact rest at both ends
        for i in range(len(clip.pieces)):
            keys[i][0] = (0.0, 0.0, 0.0, (0.0, 0.0, 0.0, 1.0))
            keys[i][-1] = (0.0, 0.0, 0.0, (0.0, 0.0, 0.0, 1.0))
        checks = run_checks(clip, keys)
        contacts = []
        contacts.append({"t": 0.02, "kind": "crack", "index": -1, "course": -1, "speed": 0.0, "x": 0.0, "y": 1.3, "z": -0.08})
        contacts.append({"t": round(clip.tell, 4), "kind": "subframe", "index": -1, "course": -1, "speed": 0.0, "x": 0.0, "y": 0.04, "z": 0.0})
        stream_t = None
        for i, p in enumerate(clip.pieces):
            contacts.append({"t": round(p["t_lift"], 4), "kind": "lift", "index": p["index"], "course": p["course"],
                             "speed": 0.0, "x": round(p["cx"], 3), "y": round(p["cy"], 3), "z": 0.0})
            # the landing speed: the last 20 mm in about 0.07 s, plus what is left of the queue's return
            contacts.append({"t": round(p["t_land"], 4), "kind": "land", "index": p["index"], "course": p["course"],
                             "speed": round(LIFT / 0.07, 3), "x": round(p["cx"], 3),
                             "y": round(p["y0"], 3), "z": 0.0})
            for f in range(clip.frames):
                if keys[i][f][2] < -0.20:
                    stream_t = f / float(HZ) if stream_t is None else min(stream_t, f / float(HZ))
                    break
        if stream_t is not None:
            contacts.append({"t": round(stream_t, 4), "kind": "stream", "index": -1, "course": -1, "speed": 0.0, "x": 0.0, "y": 1.3, "z": -0.5})
        for c in range(RS.COURSES):
            row = [p for p in clip.pieces if p["course"] == c]
            if not row:
                continue
            t_last = max(p["t_land"] for p in row)
            contacts.append({"t": round(t_last, 4), "kind": "course", "index": c, "course": c, "speed": 0.0, "x": 0.0,
                             "y": round(min(p["y0"] for p in row), 3), "z": 0.0})
        contacts.sort(key=lambda d: (d["t"], d["kind"], d["index"]))
        out_pieces = []
        for i, p in enumerate(clip.pieces):
            flat = []
            for (dx, dy, dz, q) in keys[i]:
                flat += [round(p["cx"] + dx, 5), round(p["cy"] + dy, 5), round(dz, 5),
                         round(q[0], 5), round(q[1], 5), round(q[2], 5), round(q[3], 5)]
            out_pieces.append({"name": p["name"], "index": p["index"], "course": p["course"], "side": p["side"],
                               "rest": [round(p["cx"], 6), round(p["cy"], 6), 0.0], "lift": round(p["t_lift"], 4),
                               "land": round(p["t_land"], 4), "keys": flat})
        data = {
            "name": "%s_%s" % (kit, name), "method": "choreographed", "hz": HZ, "frames": clip.frames,
            "duration": round(clip.total, 4), "phases": [clip.tell, clip.trav, clip.seat], "distance": clip.dist,
            "relative": False, "phone": phone,
            "frame": "head-local (the end's frame): x across, y up, z along the corridor (+z away from the player)",
            "pieces": out_pieces, "contacts": contacts, "checks": checks,
        }
        path = os.path.join(out_dir, "%s_%s.json" % (kit, name))
        with open(path, "w", newline="\n") as fh:
            json.dump(data, fh, separators=(",", ":"))
            fh.write("\n")
        results[name] = (path, checks, clip)
        print("[bake] %s_%s: %d pieces, %d frames, %.2f s, %d contacts, %d bytes; checks %s" % (
            kit, name, len(out_pieces), clip.frames, clip.total, len(contacts), os.path.getsize(path), json.dumps(checks)))
    return results


def main(argv):
    out_root = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
    i = 0
    while i < len(argv):
        if argv[i] == "--out-root":
            out_root = argv[i + 1]; i += 2; continue
        i += 1
    out_dir = os.path.join(out_root, "Assets", "Resources", "Props", "Models")
    os.makedirs(out_dir, exist_ok=True)
    failed = []
    for kit, pieces, phone in ((RS.BLOCKS, RS.pieces(), False), (RS.STRIPS, RS.strips(), True)):
        res = bake(kit, pieces, phone, out_dir)
        for name, (_p, ch, _c) in res.items():
            bad = ch["pieceOverlapWorstM"] > 0.001 or ch["obstacleHits"] or ch["worldBackWorstM"] > 0.02 or ch["inFrontOfLineWorstM"] < -1.0
            if bad:
                failed.append("%s_%s" % (kit, name))
    if failed:
        print("[bake] GATES FAILED: " + ", ".join(failed))
        sys.exit(2)
    print("[bake] GATES OK")


if __name__ == "__main__":
    main(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
