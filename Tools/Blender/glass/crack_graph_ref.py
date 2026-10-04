#!/usr/bin/env python3
"""crack_graph_ref.py -- FrontRooms glass crack-graph REFERENCE generator (GD3, plan 10 sec 2.6, step 2).

What it is
    The one source of truth for the fracture pattern of a struck window pane, in plain Python 3.9+
    (no bpy, no numpy), deterministic by seed. It is:
      * the pattern Red signs off on (glass_lookdev.py renders it in Cycles), and
      * the test oracle for the C# port FrontRoomsGlassFracturePattern.Generate(profile, seed, impactUV,
        slabSize, rotation) (step 3: golden match on 20 seeds, vertices within 0.1 mm).

The pattern (annealed glass after a blunt strike, [SWGMAT04], plan 10 sec 2.1, 2.6)
    * Radial tracks start on a crushed core (r about 1.2 cm, the crater), 8-12 of them.
    * Ring cracks are straight CHORDS between neighbouring radials. Each chord picks its OWN end radius on
      each radial, so the two chords on either side of a radial meet it at different points: T-junctions,
      never "+" crossings (the 04 prototype made "+" junctions everywhere).
    * Radials are straight between junctions and KINK only at a T-junction, always toward the side that has
      no chord there. That keeps every piece convex by construction (a kink is a reflex corner only on the
      chord side, where the chord splits it). Kink size is about 2 deg (U(0.5, 1.5) x 2 deg).
    * Forks are "Y" branches: a child radial leaves an existing radial at a point between two ring bands and
      the parent bends away a little. Wedges wider than 0.3 rad fork with p = 0.3 from ring 2.
    * Chord probability falls from 0.97 near the impact to 0.45 at the frame (log-radius), so long daggers
      form outside. Daggers longer than 0.90 m get one secondary crack (prototype fix 2).
    * Every piece is clipped to the visible slab (1.391 x 1.642). Pieces that touch the glass edge may keep a
      TOOTH, cut by a tilted crack that stays inside the tooth band (plan sec 2.2): jambs 95.5 mm, head 96 mm,
      sill 36 mm from the glass edge, of which 12-12.5 mm is hidden in the stop pocket (prototype fix 3).
    * Validation runs on every build (prototype fix 4 and plan sec 2.6): area sum +-0.05 %, no crossing
      cracks, no overlaps, every piece convex, no piece under 1 cm2 outside the crush core, teeth only in the
      band, deterministic bytes. Tracks that would cross are repaired (prototype fix 1).

Coordinates
    Slab-local metres: x = window-root +X, y up, origin = slab centre = window root (0, 1.175, 0).
    Window root = "Window {a}-{b}": opening centre on the wall line at floor level, +Z into cell b
    (06w sec 1.1; harness FrontRoomsGlassContract).
    The inputs follow the map's GlassBreakRecord exactly (in main since 2026-10-03, build/00_map_contract.md item 6):
      impact     root-local metres (x across, y up) -- preferred input (impact_root);
      impactUV   over the EXPOSED glass 1.367 x 1.617: u = (x + 0.6835) / 1.367, v = (y_root - 0.3665) / 1.617;
      rotation   DEGREES;
      side       +1 = struck from cell a, so the glass flies toward +Z (cell b); -1 = struck from cell b.
    The crack pattern itself never depends on side (a pane cracked from a can be finished from b; the record's side
    follows the LATEST strike). side only sets far_z_sign, which pose_point() uses to push and tilt toward the far
    side.

Randomness (portable to C#)
    PCG32 (XSH RR) seeded through splitmix64 from (seed, stream, entity keys). Every random draw is KEYED by
    the entity it belongs to (track, chord, fork, piece), never by call order, so a local repair (a dropped
    chord) does not reshuffle the rest of the pattern. Floats are (u32 >> 8) / 2^24 (exact in float and
    double). Output floats are rounded to 1e-7 m.

CLI
    python3 crack_graph_ref.py gen --profile Annealed6 --seed 4242 --impact-root 0 1.62 [--rotation 64.2]
                                   [--side 1] [--out x.json]
    python3 crack_graph_ref.py golden --out-dir golden          # the 20 golden seeds (+ index.json)
    python3 crack_graph_ref.py profiles                          # print the profile table
"""
import hashlib
import json
import struct
import math
import os
import sys
import time

VERSION = "frontrooms-glass-crackgraph-ref 1.3"

# ---------------------------------------------------------------------------------------------------------
# Window geometry (window-root metres): 06w sec 4.2-4.4, plan 10 sec 2.2
# ---------------------------------------------------------------------------------------------------------
SLAB_W, SLAB_H, SLAB_T = 1.391, 1.642, 0.006
SLAB_CY = 1.175                                   # slab centre height above the floor (root y)
STOP_X, STOP_TOP, STOP_BOT = 0.6835, 1.9835, 0.3665   # exposed-glass edge (stop line), root frame
CLEAR_X, CLEAR_TOP, CLEAR_BOT = 0.600, 1.900, 0.390   # nothing may remain inside this after the break
IMPACT_MIN_FROM_STOP = 0.2                        # FrontRoomsShotTimings.GlassBreak.ImpactMinFromFrame


def frame_for_slab(w=SLAB_W, h=SLAB_H, band_mode="band"):
    """Stop line, clear zone and tooth band in SLAB-LOCAL coordinates for a slab of w x h.

    Depths are measured inward from the glass edge (which sits in the hidden pocket behind the stop).
    For the map window (1.391 x 1.642) this is exactly 06w sec 4.4: pocket 12 / 12.5 mm, band (pocket + visible
    tooth) 95.5 mm at the jambs, 96 mm at the head, 36 mm at the sill. Other slab sizes keep those depths.
    band_mode "band" = the tooth band the map chat is asked to approve (plan sec 2.2, the default);
    band_mode "stop" = the fallback if the map chat refuses it: teeth end at the stop line (band = pocket), so no
    tooth is visible and the break reads more like tempered glass."""
    hx, hy = w / 2.0, h / 2.0
    exact_w, exact_h = abs(w - SLAB_W) < 1e-9, abs(h - SLAB_H) < 1e-9
    pocket = {"L": hx - STOP_X if exact_w else 0.012,
              "R": hx - STOP_X if exact_w else 0.012,
              "T": (SLAB_CY + SLAB_H / 2) - STOP_TOP if exact_h else 0.0125,
              "B": STOP_BOT - (SLAB_CY - SLAB_H / 2) if exact_h else 0.0125}
    band = {"L": hx - CLEAR_X if exact_w else 0.0955,
            "R": hx - CLEAR_X if exact_w else 0.0955,
            "T": (SLAB_CY + SLAB_H / 2) - CLEAR_TOP if exact_h else 0.096,
            "B": CLEAR_BOT - (SLAB_CY - SLAB_H / 2) if exact_h else 0.036}
    if band_mode == "stop":
        band = dict(pocket)
    elif band_mode != "band":
        raise ValueError("band_mode must be 'band' or 'stop'")
    return {"hx": hx, "hy": hy, "pocket": pocket, "band": band, "band_mode": band_mode,
            "stop": {"x0": -hx + pocket["L"], "x1": hx - pocket["R"], "y0": -hy + pocket["B"], "y1": hy - pocket["T"]},
            "clear": {"x0": -hx + band["L"], "x1": hx - band["R"], "y0": -hy + band["B"], "y1": hy - band["T"]}}


# ---------------------------------------------------------------------------------------------------------
# Profiles: one per glass type and tier (FrontRoomsGlassTypeProfile). Annealed6 = BUILD (plan sec 2.1).
# ---------------------------------------------------------------------------------------------------------
_ANNEALED = dict(
    glass="annealed", tier="High", thickness=0.006,
    bevel=0.0003,                                       # 0.3 mm (plan said 0.6; the Cycles A/B in build/02_pattern.md sec 4
                                                        # shows 0.6 mm reads as a drawn dark outline, 0.3 mm as a crack)
    radials=(8, 12), radial_angle_jitter=0.30,          # fraction of the even spacing
    core_radius=0.010, core_jitter=0.30, core_max=0.0145,  # crush core: powder + crater, not bodies (irregular)
    ring1=0.030, ring_growth=1.55, ring_growth_jitter=0.10, ring_node_jitter=0.12,
    kink_deg=2.0, lane_frac=0.40,                       # wander per junction; how far a radial may drift into its wedge
    kink_strong_p=0.15, kink_strong_deg=(4.0, 7.0),     # occasional visible deflection at a T
    fork_from_ring=2, fork_min_gap=0.30, fork_p=0.30, fork_child_frac=(0.35, 0.60), fork_parent_kink_frac=(0.04, 0.12),
    fork_max_frac_to_stop=0.80,                         # no forks in the outer 20 % (tiny dagger tips at the frame)
    fork_wide=0.16, fork_wide_p=0.85,                   # wedges wider than 16 cm fork more (up to p 0.85 at 32 cm)
    chord_p_inner=0.97, chord_p_outer=0.45,
    t_sep=0.004, t_sep_frac=0.05,                       # min gap between the two T-ends on one radial (V2: T, not +)
    chord_min_wedge=0.012,                              # no ring crack across a wedge narrower than this
    stage1_count=(5, 8), stage1_reach=(0.25, 0.35),     # Crack1: 5-8 radials to 25-35 % of the way to the stop
    dagger_max=0.90, dagger_split=(0.40, 0.60), dagger_split_tilt_deg=20.0,
    tooth_keep={"L": 0.80, "R": 0.80, "T": 0.75, "B": 0.65}, tooth_tri_p=0.65, tooth_max_len=0.30,
    tooth_vis_min=0.006, tooth_split_tilt_deg=25.0,
    corner_tooth_p=0.85, corner_leg=(0.9, 1.9),         # corner teeth: legs as a multiple of the band depth
    min_piece_area=1.0e-4, level2_area=60.0e-4, level2_max_depth=2,
    tilt_deg=(0.2, 0.8), push_m=(0.0005, 0.003), pose_falloff=0.35, tilt_axis_jitter_deg=35.0,
    release_inner_r=0.15, release_mid_ms=(30.0, 120.0), release_outer_ms=(120.0, 300.0), release_speed=(2.0, 4.0),
    release_speed_falloff=0.25,
    max_pieces=150, max_bodies=150, cap_merge_area=400.0e-4, chips_s1=(6, 12), chips_s2=(10, 20),
    budget_s2_tris=10000, budget_teeth_tris=2000, budget_fin_tris=400, budget_crater_tris=200,
    crater_tris_per_vertex=8,                           # crater: rim ring + pit (desktop); WebGL uses a 6-tri fan
    wire_grid=0.0, hangs_on_wire=False, mode="graph",
)


def _profile(base, **over):
    p = dict(base)
    p.update(over)
    return p


PROFILES = {
    # Desktop High (default): <= 150 pieces, 0.6 mm bevel (plan sec 4.6)
    "Annealed6": _profile(_ANNEALED, name="Annealed6"),
    # Desktop Cinematic: same pattern, a little denser, <= 170 pieces
    "Annealed6_Cinematic": _profile(_ANNEALED, name="Annealed6_Cinematic", tier="Cinematic", fork_p=0.34,
                                    chord_p_outer=0.50, max_pieces=170, max_bodies=170),
    # WebGL tier: about 60 pieces, no bevel, no level-2 re-break (plan sec 4.8)
    "Annealed6_WebGL": _profile(_ANNEALED, name="Annealed6_WebGL", tier="WebGL", bevel=0.0,
                                radials=(6, 8), ring1=0.035, ring_growth=1.85, fork_p=0.22, fork_min_gap=0.40,
                                chord_p_inner=0.92, chord_p_outer=0.30, dagger_max=0.95,
                                tooth_keep={"L": 0.60, "R": 0.60, "T": 0.55, "B": 0.45}, tooth_max_len=0.40,
                                corner_tooth_p=0.60,
                                level2_area=0.0, max_pieces=64, max_bodies=40, chips_s1=(4, 6), chips_s2=(6, 10),
                                budget_s2_tris=1500, budget_teeth_tris=400, budget_fin_tris=150, budget_crater_tris=60,
                                crater_tris_per_vertex=6),
    # PARKED, data only (plan sec 2.1): wired glass cracks like annealed and hangs on its 12.5 mm wire
    "Wired6": _profile(_ANNEALED, name="Wired6", glass="wired", wire_grid=0.0125, hangs_on_wire=True,
                       tooth_keep={"L": 1.0, "R": 1.0, "T": 1.0, "B": 1.0},
                       budget_teeth_tris=2600),     # every frame piece keeps a tooth (the wire holds it)
    # PARKED, data only: tempered glass has no crack stages; the whole pane dices at 1.0 s
    "Tempered6": _profile(_ANNEALED, name="Tempered6", glass="tempered", mode="dice", granule=0.009,
                          clump=0.16, clump_jitter=0.30, tooth_keep={"L": 0.0, "R": 0.0, "T": 0.0, "B": 0.0},
                          corner_tooth_p=0.0),
}

# ---------------------------------------------------------------------------------------------------------
# Portable RNG: splitmix64 + PCG32 (XSH RR), keyed
# ---------------------------------------------------------------------------------------------------------
MASK64 = (1 << 64) - 1
MASK32 = (1 << 32) - 1
PCG_MUL = 6364136223846793005

# stream ids (part of every key)
S_TRACKS, S_CORE, S_RINGS, S_FORK, S_CHORD, S_KINK, S_STAGE1, S_TEETH, S_SPLIT, S_LEVEL2, S_POSE, S_CHIPS, S_ROT, S_DICE = \
    1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14


def splitmix64(x):
    x = (x + 0x9E3779B97F4A7C15) & MASK64
    z = x
    z = ((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9) & MASK64
    z = ((z ^ (z >> 27)) * 0x94D049BB133111EB) & MASK64
    return z ^ (z >> 31)


def mix_key(*vals):
    h = 0x6A09E667F3BCC908
    for v in vals:
        h = splitmix64(h ^ (int(v) & MASK64))
    return h


class Rng:
    """PCG32 seeded by mix_key(seed, *keys). C#: ulong arithmetic, identical."""
    __slots__ = ("state", "inc")

    def __init__(self, seed, *keys):
        s = mix_key(seed & MASK32, *keys)
        self.inc = ((splitmix64(s ^ 0xDA3E39CB94B95BDB) << 1) | 1) & MASK64
        self.state = 0
        self.next_u32()
        self.state = (self.state + s) & MASK64
        self.next_u32()

    def next_u32(self):
        old = self.state
        self.state = (old * PCG_MUL + self.inc) & MASK64
        xs = (((old >> 18) ^ old) >> 27) & MASK32
        rot = old >> 59
        return ((xs >> rot) | (xs << ((-rot) & 31))) & MASK32

    def u01(self):
        return (self.next_u32() >> 8) * (1.0 / 16777216.0)

    def uniform(self, a, b):
        return a + (b - a) * self.u01()

    def randint(self, lo, hi):
        """Inclusive. Modulo bias < 1e-8 for our ranges (documented, C# does the same)."""
        return lo + self.next_u32() % (hi - lo + 1)


# ---------------------------------------------------------------------------------------------------------
# 2D polygon helpers (CCW polygons as lists of (x, y))
# ---------------------------------------------------------------------------------------------------------
def area(poly):
    s = 0.0
    n = len(poly)
    for i in range(n):
        x0, y0 = poly[i]
        x1, y1 = poly[(i + 1) % n]
        s += x0 * y1 - x1 * y0
    return 0.5 * s


def centroid(poly):
    a = area(poly)
    if abs(a) < 1e-18:
        n = len(poly)
        return (sum(p[0] for p in poly) / n, sum(p[1] for p in poly) / n)
    cx = cy = 0.0
    n = len(poly)
    for i in range(n):
        x0, y0 = poly[i]
        x1, y1 = poly[(i + 1) % n]
        f = x0 * y1 - x1 * y0
        cx += (x0 + x1) * f
        cy += (y0 + y1) * f
    return (cx / (6.0 * a), cy / (6.0 * a))


def clip_half(poly, a, b, c):
    """Keep a*x + b*y <= c (Sutherland-Hodgman, one plane)."""
    out = []
    n = len(poly)
    if n == 0:
        return out
    for i in range(n):
        p = poly[i]
        q = poly[(i + 1) % n]
        dp = a * p[0] + b * p[1] - c
        dq = a * q[0] + b * q[1] - c
        if dp <= 0.0:
            out.append(p)
        if (dp < 0.0 < dq) or (dq < 0.0 < dp):
            t = dp / (dp - dq)
            out.append((p[0] + (q[0] - p[0]) * t, p[1] + (q[1] - p[1]) * t))
    return out


def clip_rect(poly, x0, x1, y0, y1):
    for a, b, c in ((1.0, 0.0, x1), (-1.0, 0.0, -x0), (0.0, 1.0, y1), (0.0, -1.0, -y0)):
        poly = clip_half(poly, a, b, c)
        if len(poly) < 3:
            return []
    return poly


def clean_poly(poly, eps_len=1e-9, eps_cross=1e-13):
    """Drop repeated and exactly collinear vertices (T-junction points of a straight radial)."""
    pts = []
    for p in poly:
        if not pts or abs(p[0] - pts[-1][0]) > eps_len or abs(p[1] - pts[-1][1]) > eps_len:
            pts.append(p)
    while len(pts) > 1 and abs(pts[0][0] - pts[-1][0]) <= eps_len and abs(pts[0][1] - pts[-1][1]) <= eps_len:
        pts.pop()
    changed = True
    while changed and len(pts) > 3:
        changed = False
        n = len(pts)
        for i in range(n):
            a = pts[i - 1]
            b = pts[i]
            c = pts[(i + 1) % n]
            cr = (b[0] - a[0]) * (c[1] - b[1]) - (b[1] - a[1]) * (c[0] - b[0])
            dot = (b[0] - a[0]) * (c[0] - b[0]) + (b[1] - a[1]) * (c[1] - b[1])
            if abs(cr) <= eps_cross and dot > 0.0:
                del pts[i]
                changed = True
                break
    return pts


def is_convex(poly, eps=1e-12):
    n = len(poly)
    if n < 3:
        return False
    for i in range(n):
        a = poly[i - 1]
        b = poly[i]
        c = poly[(i + 1) % n]
        cr = (b[0] - a[0]) * (c[1] - b[1]) - (b[1] - a[1]) * (c[0] - b[0])
        if cr < -eps:
            return False
    return area(poly) > 0.0


def convex_hull(points):
    pts = sorted(set(points))
    if len(pts) < 3:
        return pts

    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    lower, upper = [], []
    for p in pts:
        while len(lower) >= 2 and cross(lower[-2], lower[-1], p) <= 0:
            lower.pop()
        lower.append(p)
    for p in reversed(pts):
        while len(upper) >= 2 and cross(upper[-2], upper[-1], p) <= 0:
            upper.pop()
        upper.append(p)
    return lower[:-1] + upper[:-1]


def split_by_line(poly, px, py, dx, dy):
    """Split a convex polygon by the infinite line through (px, py) with direction (dx, dy).
    Returns (left, right); either may be empty."""
    nx, ny = -dy, dx
    c = nx * px + ny * py
    left = clip_half(poly, -nx, -ny, -c)     # n.p >= c
    right = clip_half(poly, nx, ny, c)       # n.p <= c
    return left, right


def diameter(poly):
    best = (0.0, 0, 0)
    n = len(poly)
    for i in range(n):
        for j in range(i + 1, n):
            d = math.hypot(poly[i][0] - poly[j][0], poly[i][1] - poly[j][1])
            if d > best[0]:
                best = (d, i, j)
    return best


def orient(a, b, c):
    return (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])


def segments_cross(p1, p2, p3, p4, eps=1e-14):
    """True when two segments that share no endpoint touch or cross anywhere."""
    d1 = orient(p3, p4, p1)
    d2 = orient(p3, p4, p2)
    d3 = orient(p1, p2, p3)
    d4 = orient(p1, p2, p4)
    if ((d1 > eps and d2 < -eps) or (d1 < -eps and d2 > eps)) and ((d3 > eps and d4 < -eps) or (d3 < -eps and d4 > eps)):
        return True

    def on_seg(a, b, c):   # c on segment ab (collinear assumed)
        return min(a[0], b[0]) - 1e-12 <= c[0] <= max(a[0], b[0]) + 1e-12 and \
            min(a[1], b[1]) - 1e-12 <= c[1] <= max(a[1], b[1]) + 1e-12
    if abs(d1) <= eps and on_seg(p3, p4, p1):
        return True
    if abs(d2) <= eps and on_seg(p3, p4, p2):
        return True
    if abs(d3) <= eps and on_seg(p1, p2, p3):
        return True
    if abs(d4) <= eps and on_seg(p1, p2, p4):
        return True
    return False


def convex_overlap_area(a, b):
    """Intersection area of two convex CCW polygons."""
    out = a
    n = len(b)
    for i in range(n):
        p = b[i]
        q = b[(i + 1) % n]
        # keep the left side of edge p->q:  cross(q-p, x-p) >= 0  ->  -(qy-py) x + (qx-px) y ... <= form
        ex, ey = q[0] - p[0], q[1] - p[1]
        out = clip_half(out, ey, -ex, ey * p[0] - ex * p[1])
        if len(out) < 3:
            return 0.0
    return max(0.0, area(out))


# ---------------------------------------------------------------------------------------------------------
# Generator
# ---------------------------------------------------------------------------------------------------------
TWO_PI = 2.0 * math.pi


class _Track(object):
    __slots__ = ("tid", "parent", "pts", "x", "y", "phi", "lo", "hi", "initial", "order_index")

    def __init__(self, tid, parent, vid, x, y, phi, lo, hi, initial):
        self.tid = tid
        self.parent = parent
        self.pts = [vid]
        self.x = x
        self.y = y
        self.phi = phi
        self.lo = lo
        self.hi = hi
        self.initial = initial
        self.order_index = 0


def _ray_rect_exit(ox, oy, dx, dy, x0, x1, y0, y1):
    t = float("inf")
    if dx > 1e-15:
        t = min(t, (x1 - ox) / dx)
    elif dx < -1e-15:
        t = min(t, (x0 - ox) / dx)
    if dy > 1e-15:
        t = min(t, (y1 - oy) / dy)
    elif dy < -1e-15:
        t = min(t, (y0 - oy) / dy)
    return max(t, 0.0)


class GenerationError(Exception):
    pass


def map_rotation_deg(seed):
    """The map's record rotation for a seed: MapHash.Unit((uint)seed) * 360f (FrontRoomsMapWorld.GlassBreakOf),
    with C#'s float32 rounding. Unit(h) = (h >> 8) / 2^24 is exact in float32; the product is rounded to float32."""
    u = ((int(seed) & MASK32) >> 8) * (1.0 / 16777216.0)
    return struct.unpack("<f", struct.pack("<f", u * 360.0))[0]


def _exposed_uv(ix, iy, fr):
    """Slab-local impact -> GlassBreakRecord.impactUV (over the exposed glass, the stop-line rectangle)."""
    st = fr["stop"]
    return [(ix - st["x0"]) / (st["x1"] - st["x0"]), (iy - st["y0"]) / (st["y1"] - st["y0"])]


def _clamp_impact(x, y, fr):
    st = fr["stop"]
    m = IMPACT_MIN_FROM_STOP
    return (min(max(x, st["x0"] + m), st["x1"] - m), min(max(y, st["y0"] + m), st["y1"] - m))


def _build_graph(P, seed, ix, iy, rot, fr, supp):
    """One attempt: tracks, chords, forks -> vertices + edges (impact-relative coords) + bookkeeping."""
    hx, hy = fr["hx"], fr["hy"]
    st = fr["stop"]
    corners = [(-hx, -hy), (hx, -hy), (hx, hy), (-hx, hy)]
    r_far = max(math.hypot(cx - ix, cy - iy) for cx, cy in corners)
    r_out = 2.6 * r_far + 0.1
    rj = P["ring_node_jitter"]

    def dist_to_stop(phi):
        return _ray_rect_exit(ix, iy, math.cos(phi), math.sin(phi), st["x0"], st["x1"], st["y0"], st["y1"])

    V = []                     # vertices, impact-relative
    vkind = []                 # 'core','junction','fork','end'

    def add_v(x, y, kind):
        V.append((x, y))
        vkind.append(kind)
        return len(V) - 1

    # ---- rings
    rings = [P["core_radius"], P["ring1"]]
    k = 1
    while rings[-1] < r_far:
        g = Rng(seed, S_RINGS, k).uniform(1.0 - P["ring_growth_jitter"], 1.0 + P["ring_growth_jitter"])
        rings.append(rings[-1] * P["ring_growth"] * g)
        k += 1
    K = len(rings) - 1          # rings[K] >= r_far: no chords there

    def t_sep_at(k):
        return max(P["t_sep"], P["t_sep_frac"] * rings[k])

    def chord_r_max(k):         # highest radius a ring-k chord end can take (node jitter + T separation)
        return rings[k] * (1.0 + rj) + 0.5 * t_sep_at(k)

    def chord_r_min(k):
        return rings[k] * (1.0 - rj) - 0.5 * t_sep_at(k)

    # ---- initial radials + crush core
    r0 = Rng(seed, S_TRACKS)
    n0 = r0.randint(P["radials"][0], P["radials"][1])
    th = []
    for i in range(n0):
        j = Rng(seed, S_TRACKS, 1, i).uniform(-P["radial_angle_jitter"], P["radial_angle_jitter"])
        th.append(rot + (i + j) * TWO_PI / n0)
    cr = [P["core_radius"] * (1.0 + Rng(seed, S_CORE, i).uniform(-P["core_jitter"], P["core_jitter"])) for i in range(n0)]
    # make the core polygon strictly convex (raise any vertex that sits inside its neighbours' chord)
    for _ in range(4 * n0):
        fixed = True
        for i in range(n0):
            a = (cr[i - 1] * math.cos(th[i - 1]), cr[i - 1] * math.sin(th[i - 1]))
            b = (cr[(i + 1) % n0] * math.cos(th[(i + 1) % n0]), cr[(i + 1) % n0] * math.sin(th[(i + 1) % n0]))
            ux, uy = math.cos(th[i]), math.sin(th[i])
            # radius where the ray at th[i] meets line a-b
            ex, ey = b[0] - a[0], b[1] - a[1]
            den = ux * ey - uy * ex
            if abs(den) < 1e-15:
                continue
            t = (a[0] * ey - a[1] * ex) / den
            if cr[i] < t * 1.02:
                cr[i] = t * 1.02
                fixed = False
        if fixed:
            break
    # the crater must stay <= 15 mm (V5): scale the whole core down if the jitter + convexity raise grew it
    m = max(cr)
    if m > P["core_max"]:
        cr = [r * P["core_max"] / m for r in cr]
    tracks = []
    for i in range(n0):
        gl = (th[i] - th[i - 1]) if i > 0 else (th[0] - (th[-1] - TWO_PI))
        gr = (th[i + 1] - th[i]) if i + 1 < n0 else ((th[0] + TWO_PI) - th[i])
        x, y = cr[i] * math.cos(th[i]), cr[i] * math.sin(th[i])
        vid = add_v(x, y, "core")
        tracks.append(_Track(i, -1, vid, x, y, th[i], th[i] - P["lane_frac"] * gl, th[i] + P["lane_frac"] * gr, True))
    core_vids = [t.pts[0] for t in tracks]
    active = list(tracks)       # angular (CCW) order; the pair (active[-1], active[0]) wraps by 2 pi
    all_tracks = list(tracks)
    by_tid = {t.tid: t for t in tracks}
    kink_scale = supp.get("kink_scale", {})

    def advance(t, rho):
        px, py = t.x, t.y
        dx, dy = math.cos(t.phi), math.sin(t.phi)
        pd = px * dx + py * dy
        disc = pd * pd - (px * px + py * py - rho * rho)
        if disc < 0.0:
            raise GenerationError("track %s cannot reach radius %.4f" % (t.tid, rho))
        s = -pd + math.sqrt(disc)
        if s <= 1e-9:
            raise GenerationError("track %s does not move outward at %.4f" % (t.tid, rho))
        t.x, t.y = px + dx * s, py + dy * s
        return t.x, t.y

    def peek(t, rho):
        """Where track t would meet radius rho on its current heading (no state change)."""
        px, py = t.x, t.y
        dx, dy = math.cos(t.phi), math.sin(t.phi)
        pd = px * dx + py * dy
        disc = pd * pd - (px * px + py * py - rho * rho)
        if disc < 0.0:
            return None
        s_ = -pd + math.sqrt(disc)
        return (px + dx * s_, py + dy * s_)

    def kink(t, kappa):
        # clamp into the lane without flipping the sign required by the convexity rule
        new = t.phi + kappa
        if kappa > 0.0:
            new = min(new, max(t.phi, t.hi))
        elif kappa < 0.0:
            new = max(new, min(t.phi, t.lo))
        t.phi = new

    def pair_gap(a, b, wrap):
        return (b.phi + (TWO_PI if wrap else 0.0)) - a.phi

    chords = []        # dict(ring, a, b, va, vb, key)
    forks = []         # dict(ring, parent, child, vid, key)
    events_log = {"kinks": 0, "kinks_zeroed": 0}

    for k in range(1, K):
        # -------- forks in the annulus below ring k
        if k >= P["fork_from_ring"] and P["fork_p"] > 0.0:
            lo_r = chord_r_max(k - 1) * 1.02
            hi_r = chord_r_min(k) * 0.98
            if hi_r > lo_r:
                used = set()
                n = len(active)
                plan = []
                for idx in range(n):
                    a = active[idx]
                    b = active[(idx + 1) % n]
                    wrap = idx + 1 == n
                    key = (k, a.tid, b.tid)
                    rng = Rng(seed, S_FORK, *key)
                    c, side, fr_u, cf, pk = rng.u01(), rng.u01(), rng.u01(), rng.u01(), rng.u01()
                    gap = pair_gap(a, b, wrap)
                    # branching: wedges wider than fork_min_gap fork with fork_p; wedges that are physically wide
                    # (far from the impact) fork more, so the outer glass breaks into long daggers, not slabs
                    width = rings[k] * gap
                    p_f = P["fork_p"]
                    if width > P["fork_wide"]:
                        p_f += (P["fork_wide_p"] - p_f) * min(1.0, (width - P["fork_wide"]) / P["fork_wide"])
                    gate = gap > P["fork_min_gap"] or (width > P["fork_wide"] and gap > 0.10)
                    if not gate or c >= p_f or key in supp["forks"]:
                        continue
                    if a.tid in used or b.tid in used:
                        continue
                    rho = lo_r + fr_u * (hi_r - lo_r)
                    mid = a.phi + 0.5 * gap
                    if rho > P["fork_max_frac_to_stop"] * dist_to_stop(mid):
                        continue
                    used.add(a.tid)
                    used.add(b.tid)
                    plan.append((idx, a, b, wrap, key, side, rho, cf, pk, gap))
                inserted = 0
                for (idx, a, b, wrap, key, side, rho, cf, pk, gap) in plan:
                    parent = a if side < 0.5 else b
                    s = 1.0 if parent is a else -1.0
                    sx, sy = advance(parent, rho)
                    vid = add_v(sx, sy, "fork")
                    parent.pts.append(vid)
                    child_frac = P["fork_child_frac"][0] + cf * (P["fork_child_frac"][1] - P["fork_child_frac"][0])
                    pk_frac = P["fork_parent_kink_frac"][0] + pk * (P["fork_parent_kink_frac"][1] - P["fork_parent_kink_frac"][0])
                    # unwrapped child direction (between parent and the other track)
                    phic = parent.phi + s * gap * child_frac
                    margin = 0.08 * gap
                    kink(parent, -s * gap * pk_frac)
                    events_log["kinks"] += 1
                    tid = 1000 + (mix_key(*key) & 0x7FFFFFF)
                    if s > 0:   # child between a (parent) and b
                        midpc = 0.5 * (parent.phi + phic)
                        midcb = 0.5 * (phic + b.phi + (TWO_PI if wrap else 0.0))
                        parent.hi = min(parent.hi, midpc - margin)
                        child = _Track(tid, parent.tid, vid, sx, sy, phic, midpc + margin, midcb - margin, False)
                        if wrap:
                            b.lo = max(b.lo, midcb + margin - TWO_PI)
                        else:
                            b.lo = max(b.lo, midcb + margin)
                    else:       # child between a and b (parent)
                        a_phi = a.phi - (TWO_PI if wrap else 0.0)    # a's direction in b's unwrapped frame
                        midcp = 0.5 * (phic + parent.phi)
                        midac = 0.5 * (a_phi + phic)
                        parent.lo = max(parent.lo, midcp + margin)
                        child = _Track(tid, parent.tid, vid, sx, sy, phic, midac + margin, midcp - margin, False)
                        if wrap:
                            a.hi = min(a.hi, midac - margin + TWO_PI)
                        else:
                            a.hi = min(a.hi, midac - margin)
                        if wrap:
                            # keep the child's angles in a's frame so the active list stays monotonic
                            child.phi += TWO_PI
                            child.lo += TWO_PI
                            child.hi += TWO_PI
                    forks.append({"ring": k, "parent": parent.tid, "child": tid, "vid": vid, "key": key})
                    all_tracks.append(child)
                    by_tid[tid] = child
                    # insert into the active order
                    pos = active.index(parent)
                    if s > 0:
                        active.insert(pos + 1, child)
                    else:
                        if wrap:
                            active.append(child)   # between the last (a) and first (b)
                        else:
                            active.insert(pos, child)
                    inserted += 1
                # renormalise unwrapped angles so active[0] is the reference and the list is monotonic
                _renormalise(active)

        # -------- chords at ring k
        n = len(active)
        ev = {}
        ring_chords = []
        for idx in range(n):
            a = active[idx]
            b = active[(idx + 1) % n]
            wrap = idx + 1 == n
            key = (k, a.tid, b.tid)
            rng = Rng(seed, S_CHORD, *key)
            u, ja, jb = rng.u01(), rng.u01(), rng.u01()
            mid = a.phi + 0.5 * pair_gap(a, b, wrap)
            dstop = max(dist_to_stop(mid), P["ring1"] * 1.0001)
            t = 0.0 if dstop <= P["ring1"] else min(1.0, max(0.0, math.log(rings[k] / P["ring1"]) / math.log(dstop / P["ring1"])))
            p = P["chord_p_inner"] + (P["chord_p_outer"] - P["chord_p_inner"]) * t
            if u >= p or key in supp["chords"]:
                continue
            # chords whose wedge lies wholly outside the slab are useless
            if rings[k] * (1.0 - rj) > r_far:
                continue
            ra = rings[k] * (1.0 - rj + 2.0 * rj * ja)
            rb = rings[k] * (1.0 - rj + 2.0 * rj * jb)
            # a ring crack runs ACROSS its wedge: in a thin wedge (next to a fork) skip it, and keep the chord
            # within ~48 deg of tangential, so both faces stay convex at its T-ends
            pa, pb = peek(a, rings[k]), peek(b, rings[k])
            if pa is None or pb is None:
                continue
            s_w = math.hypot(pa[0] - pb[0], pa[1] - pb[1])
            if s_w < max(P["chord_min_wedge"], 2.5 * t_sep_at(k)):
                continue
            lim = 0.5 * s_w
            if abs(ra - rb) > lim:
                m = 0.5 * (ra + rb)
                ra, rb = (m - 0.5 * lim, m + 0.5 * lim) if ra < rb else (m + 0.5 * lim, m - 0.5 * lim)
            ch = {"ring": k, "a": a.tid, "b": b.tid, "ra": ra, "rb": rb, "key": key, "va": -1, "vb": -1}
            ring_chords.append(ch)
            ev.setdefault(a.tid, []).append((ra, -1.0, ch, "va"))   # chord on a's higher side -> bend lower
            ev.setdefault(b.tid, []).append((rb, 1.0, ch, "vb"))    # chord on b's lower side -> bend higher
        for t in active:
            lst = ev.get(t.tid)
            if not lst:
                continue
            lst.sort(key=lambda e: e[0])
            if len(lst) == 2:
                # V2: the ring cracks on the two sides of a radial end at visibly different points (two T's, not a +)
                sep = t_sep_at(k)
                gap = lst[1][0] - lst[0][0]
                if gap < sep:
                    d = 0.5 * (sep - gap)
                    lst = [(lst[0][0] - d,) + lst[0][1:], (lst[1][0] + d,) + lst[1][1:]]
            last_r = -1.0
            for (rho, sgn, ch, slot) in lst:
                if rho <= last_r + 1e-6:
                    rho = last_r + 1e-6
                last_r = rho
                x, y = advance(t, rho)
                vid = add_v(x, y, "junction")
                t.pts.append(vid)
                ch[slot] = vid
                rk = Rng(seed, S_KINK, ch["key"][0], ch["key"][1], ch["key"][2], 0 if slot == "va" else 1)
                mag = math.radians(P["kink_deg"]) * rk.uniform(0.5, 1.5)
                if rk.u01() < P["kink_strong_p"]:
                    # now and then a radial deflects visibly where a ring crack meets it (V2: "kinked")
                    mag = math.radians(rk.uniform(P["kink_strong_deg"][0], P["kink_strong_deg"][1]))
                mag *= kink_scale.get(t.tid, 1.0) * supp.get("global_kink", 1.0)
                before = t.phi
                kink(t, sgn * mag)
                events_log["kinks"] += 1
                if abs(t.phi - before) < 1e-12:
                    events_log["kinks_zeroed"] += 1
        chords.extend(ring_chords)

    # ---- run every track out to r_out and close the outer boundary
    for t in active:
        x, y = advance(t, r_out)
        t.pts.append(add_v(x, y, "end"))
    edges = []          # (u, v, kind, owner)
    for t in all_tracks:
        for i in range(len(t.pts) - 1):
            edges.append((t.pts[i], t.pts[i + 1], "track", t.tid))
    for ch in chords:
        edges.append((ch["va"], ch["vb"], "chord", ch["key"]))
    for i in range(len(core_vids)):
        edges.append((core_vids[i], core_vids[(i + 1) % len(core_vids)], "core", i))
    ends = [t.pts[-1] for t in active]
    for i in range(len(ends)):
        edges.append((ends[i], ends[(i + 1) % len(ends)], "outer", i))
    return {"V": V, "vkind": vkind, "edges": edges, "tracks": all_tracks, "by_tid": by_tid, "active": active, "chords": chords,
            "forks": forks, "rings": rings, "K": K, "r_far": r_far, "r_out": r_out, "core_vids": core_vids,
            "n0": n0, "theta0": th, "log": events_log}


def _renormalise(active):
    """Keep the active list's unwrapped angles monotonic, starting near active[0]."""
    base = active[0].phi
    prev = None
    for t in active:
        while t.phi < base - 1e-9:
            t.phi += TWO_PI
            t.lo += TWO_PI
            t.hi += TWO_PI
        while t.phi >= base + TWO_PI - 1e-9:
            t.phi -= TWO_PI
            t.lo -= TWO_PI
            t.hi -= TWO_PI
        prev = t


def _faces(V, edges):
    """Bounded faces of the planar straight-line graph (CCW), as vertex-id lists."""
    nbr = {}
    for (u, v, _, _) in edges:
        if u == v:
            continue
        nbr.setdefault(u, set()).add(v)
        nbr.setdefault(v, set()).add(u)
    order = {}
    pos = {}
    for v, ns in nbr.items():
        vx, vy = V[v]
        lst = sorted(ns, key=lambda w: math.atan2(V[w][1] - vy, V[w][0] - vx))
        order[v] = lst
        pos[v] = {w: i for i, w in enumerate(lst)}
    seen = set()
    faces = []
    for u, ns in nbr.items():
        for v in ns:
            if (u, v) in seen:
                continue
            face = []
            a, b = u, v
            guard = 0
            while (a, b) not in seen:
                seen.add((a, b))
                face.append(a)
                lst = order[b]
                i = pos[b][a]
                c = lst[(i - 1) % len(lst)]
                a, b = b, c
                guard += 1
                if guard > 100000:
                    raise GenerationError("face walk did not close")
            faces.append(face)
    out = []
    for f in faces:
        poly = [V[i] for i in f]
        if area(poly) > 0.0:
            out.append(f)
    return out


def _clip_segment(p, q, box):
    """Liang-Barsky: the part of segment p-q inside box (x0, x1, y0, y1), or None."""
    x0, x1, y0, y1 = box
    t0, t1 = 0.0, 1.0
    dx, dy = q[0] - p[0], q[1] - p[1]
    for pp, qq in ((-dx, p[0] - x0), (dx, x1 - p[0]), (-dy, p[1] - y0), (dy, y1 - p[1])):
        if pp == 0.0:
            if qq < 0.0:
                return None
            continue
        r = qq / pp
        if pp < 0.0:
            if r > t1:
                return None
            if r > t0:
                t0 = r
        else:
            if r < t0:
                return None
            if r < t1:
                t1 = r
    return ((p[0] + t0 * dx, p[1] + t0 * dy), (p[0] + t1 * dx, p[1] + t1 * dy))


def _graph_crossings(V, edges, limit_box=None):
    """Pairs (i, j), i < j, of graph edges that touch or cross without sharing a vertex.

    Only edges with a part inside limit_box take part (crossings far outside the slab do not matter).
    Broad phase: sweep over the x-extent of each edge's part inside the box. The result is sorted, so the
    repair order does not depend on container iteration order (the C# port must match it)."""
    boxes = []
    for idx, (u, v, kind, owner) in enumerate(edges):
        p, q = V[u], V[v]
        if limit_box is not None:
            c = _clip_segment(p, q, limit_box)
            if c is None:
                continue
            p, q = c
        boxes.append((min(p[0], q[0]), max(p[0], q[0]), min(p[1], q[1]), max(p[1], q[1]), idx))
    boxes.sort()
    found = []
    n = len(boxes)
    for i in range(n):
        ax0, ax1, ay0, ay1, a = boxes[i]
        u1, v1 = edges[a][0], edges[a][1]
        for j in range(i + 1, n):
            bx0, bx1, by0, by1, b = boxes[j]
            if bx0 > ax1 + 1e-12:
                break
            if by0 > ay1 + 1e-12 or by1 < ay0 - 1e-12:
                continue
            u2, v2 = edges[b][0], edges[b][1]
            if u1 in (u2, v2) or v1 in (u2, v2):
                continue
            if segments_cross(V[u1], V[v1], V[u2], V[v2]):
                found.append((a, b) if a < b else (b, a))
    found.sort()
    return found


def _piece_key(poly):
    c = centroid(poly)
    return (int(round((c[0] + 10.0) * 10000.0)), int(round((c[1] + 10.0) * 10000.0)))


def _side_frames(fr):
    hx, hy = fr["hx"], fr["hy"]
    # d = n.p + off (depth from the glass edge, inward), u = t.p (along the side)
    return {"L": ((1.0, 0.0), hx, (0.0, 1.0)), "R": ((-1.0, 0.0), hx, (0.0, 1.0)),
            "T": ((0.0, -1.0), hy, (1.0, 0.0)), "B": ((0.0, 1.0), hy, (1.0, 0.0))}


def _contacts(poly, fr):
    hx, hy = fr["hx"], fr["hy"]
    c = {"L": 0.0, "R": 0.0, "T": 0.0, "B": 0.0}
    n = len(poly)
    for i in range(n):
        p = poly[i]
        q = poly[(i + 1) % n]
        L = math.hypot(q[0] - p[0], q[1] - p[1])
        if abs(p[0] + hx) < 1e-9 and abs(q[0] + hx) < 1e-9:
            c["L"] += L
        if abs(p[0] - hx) < 1e-9 and abs(q[0] - hx) < 1e-9:
            c["R"] += L
        if abs(p[1] - hy) < 1e-9 and abs(q[1] - hy) < 1e-9:
            c["T"] += L
        if abs(p[1] + hy) < 1e-9 and abs(q[1] + hy) < 1e-9:
            c["B"] += L
    return c


def _in_clear_area(poly, fr):
    cz = fr["clear"]
    return area(clip_rect(poly, cz["x0"], cz["x1"], cz["y0"], cz["y1"])) if poly else 0.0


def _tooth_cut(poly, side, fr, P, rng, attempt):
    """Return (tooth, rest) or None. The cut is a tilted crack that stays within the side's band."""
    (nx, ny), off, (tx, ty) = _side_frames(fr)[side]
    D = fr["band"][side]
    pk = fr["pocket"][side]
    # part of the piece inside the band depth
    band_part = clip_half(poly, nx, ny, D - off)
    if len(band_part) < 3 or area(band_part) < 1e-10:
        return None
    us = [tx * p[0] + ty * p[1] for p in band_part]
    u0, u1 = min(us), max(us)
    if u1 - u0 < 0.01:
        return None
    vis = P["tooth_vis_min"]
    lo_d = pk + vis
    if lo_d >= D:
        return None
    tri = rng.u01() < P["tooth_tri_p"]
    a1, a2, flip = rng.u01(), rng.u01(), rng.u01()
    span = D - pk
    if tri:
        # a pointed tooth: the cut runs from (nearly) the stop line at one end to (nearly) the band at the other
        d0 = pk + 0.25 * span * a1
        d1 = D - 0.10 * span * a2
        if flip < 0.5:
            d0, d1 = d1, d0
    else:
        # a blunt stub: both ends 30-100 % of the way into the band
        d0 = pk + (0.30 + 0.70 * a1) * span
        d1 = pk + (0.30 + 0.70 * a2) * span
    if max(d0, d1) < lo_d:
        return None
    s = (d1 - d0) / (u1 - u0)
    # tooth: n.p + off - d0 - s (t.p - u0) <= 0
    a = nx - s * tx
    b = ny - s * ty
    c = d0 - off - s * u0
    # tooth + rest == poly exactly (two half-planes of one line); the caller rejects a tooth that reaches the
    # clear zone, so no second clip is needed (a second clip would leave a hole)
    tooth = clip_half(poly, a, b, c)
    rest = clip_half(poly, -a, -b, -c)
    tooth = clean_poly(tooth)
    rest = clean_poly(rest)
    return tooth, rest


def _split_long_for_teeth(poly, side, fr, P, rng):
    """Cracks into the frame so no tooth runs longer than tooth_max_len along its side."""
    (nx, ny), off, (tx, ty) = _side_frames(fr)[side]
    D = fr["band"][side]
    band_part = clip_half(poly, nx, ny, D - off)
    if len(band_part) < 3:
        return [poly]
    us = [tx * p[0] + ty * p[1] for p in band_part]
    u0, u1 = min(us), max(us)
    if u1 - u0 <= P["tooth_max_len"]:
        return [poly]
    f = rng.uniform(0.35, 0.65)
    us_ = u0 + f * (u1 - u0)
    ang = math.radians(rng.uniform(-P["tooth_split_tilt_deg"], P["tooth_split_tilt_deg"]))
    # point on the glass edge at u = us_, direction = inward normal rotated by ang
    px = -nx * off + tx * us_
    py = -ny * off + ty * us_
    dx = nx * math.cos(ang) - ny * math.sin(ang)
    dy = nx * math.sin(ang) + ny * math.cos(ang)
    left, right = split_by_line(poly, px, py, dx, dy)
    left, right = clean_poly(left), clean_poly(right)
    if len(left) < 3 or len(right) < 3 or area(left) < P["min_piece_area"] or area(right) < P["min_piece_area"]:
        return [poly]
    return _split_long_for_teeth(left, side, fr, P, Rng(rng.next_u32(), S_SPLIT, 1)) + \
        _split_long_for_teeth(right, side, fr, P, Rng(rng.next_u32(), S_SPLIT, 2))


def _split_dagger(poly, P, seed):
    d, i, j = diameter(poly)
    if d <= P["dagger_max"]:
        return [poly]
    rng = Rng(seed, S_SPLIT, *_piece_key(poly))
    f = rng.uniform(P["dagger_split"][0], P["dagger_split"][1])
    ax, ay = poly[j][0] - poly[i][0], poly[j][1] - poly[i][1]
    L = math.hypot(ax, ay)
    ax, ay = ax / L, ay / L
    px, py = poly[i][0] + ax * f * L, poly[i][1] + ay * f * L
    ang = math.radians(90.0 + rng.uniform(-P["dagger_split_tilt_deg"], P["dagger_split_tilt_deg"]))
    dx = ax * math.cos(ang) - ay * math.sin(ang)
    dy = ax * math.sin(ang) + ay * math.cos(ang)
    left, right = split_by_line(poly, px, py, dx, dy)
    left, right = clean_poly(left), clean_poly(right)
    if len(left) < 3 or len(right) < 3:
        return [poly]
    return _split_dagger(left, P, seed) + _split_dagger(right, P, seed)


def _level2(poly, P, seed, depth=0):
    if P["level2_area"] <= 0.0 or area(poly) <= P["level2_area"] or depth >= P["level2_max_depth"]:
        return [poly]
    rng = Rng(seed, S_LEVEL2, depth, *_piece_key(poly))
    d, i, j = diameter(poly)
    ax, ay = poly[j][0] - poly[i][0], poly[j][1] - poly[i][1]
    L = math.hypot(ax, ay)
    ax, ay = ax / L, ay / L
    f = rng.uniform(0.35, 0.65)
    px, py = poly[i][0] + ax * f * L, poly[i][1] + ay * f * L
    ang = math.radians(90.0 + rng.uniform(-30.0, 30.0))
    dx = ax * math.cos(ang) - ay * math.sin(ang)
    dy = ax * math.sin(ang) + ay * math.cos(ang)
    left, right = split_by_line(poly, px, py, dx, dy)
    left, right = clean_poly(left), clean_poly(right)
    if len(left) < 3 or len(right) < 3 or area(left) < P["min_piece_area"] or area(right) < P["min_piece_area"]:
        return [poly]
    return _level2(left, P, seed, depth + 1) + _level2(right, P, seed, depth + 1)


def _merge_convex(a, b):
    h = convex_hull(a + b)
    if len(h) >= 3 and abs(area(h) - (area(a) + area(b))) < 1e-10:
        return clean_poly(h)
    return None


def _shares_edge(a, b, tol=1e-9):
    """Two pieces share a boundary segment of positive length."""
    for i in range(len(a)):
        p, q = a[i], a[(i + 1) % len(a)]
        ex, ey = q[0] - p[0], q[1] - p[1]
        L2 = ex * ex + ey * ey
        if L2 < 1e-18:
            continue
        for j in range(len(b)):
            r, s = b[j], b[(j + 1) % len(b)]
            if abs(orient(p, q, r)) > tol * math.sqrt(L2) or abs(orient(p, q, s)) > tol * math.sqrt(L2):
                continue
            t0 = ((r[0] - p[0]) * ex + (r[1] - p[1]) * ey) / L2
            t1 = ((s[0] - p[0]) * ex + (s[1] - p[1]) * ey) / L2
            lo, hi = max(0.0, min(t0, t1)), min(1.0, max(t0, t1))
            if (hi - lo) * math.sqrt(L2) > 1e-6:
                return True
    return False


def generate(profile="Annealed6", seed=4242, impact_uv=None, impact_root=None, slab_size=(SLAB_W, SLAB_H),
             rotation_deg=None, side=1, band_mode="band", max_attempts=96, timing=None):
    """Generate one fracture pattern. Returns a plain dict (JSON-ready, floats rounded to 1e-7 m).

    impact_root: (x, y) in window-root metres = GlassBreakRecord.impact (wins over impact_uv);
    impact_uv: (u, v) over the EXPOSED glass = GlassBreakRecord.impactUV (u from x -0.6835 over 1.367, v from
               y 0.3665 over 1.617 for the map window; for another slab size, over its stop-line rectangle).
    rotation_deg: GlassBreakRecord.rotation (degrees); None = the map's own formula for the seed,
                  MapHash.Unit((uint)seed) * 360 (map_rotation_deg). Small test seeds give ~0 deg that way; the map's
                  seeds are full 32-bit hashes.
    side: GlassBreakRecord.side (+1 = struck from cell a, glass flies toward +Z; -1 = struck from cell b). It does
          not change the pattern, only far_z_sign in the output.
    band_mode: "band" (tooth band, plan sec 2.2) or "stop" (fallback: teeth clipped at the stop line).

    The graph is rebuilt after each local repair (a dropped chord or fork, or damped kinks). Every random draw is
    keyed by the element it belongs to, so a repair changes only that element. The repair order is canonical."""
    P = PROFILES[profile] if isinstance(profile, str) else profile
    seed = int(seed) & MASK32
    side = 1 if int(side) >= 0 else -1
    w, h = float(slab_size[0]), float(slab_size[1])
    fr = frame_for_slab(w, h, band_mode)
    if impact_root is not None:
        ix, iy = float(impact_root[0]), float(impact_root[1]) - SLAB_CY
    elif impact_uv is not None:
        st0 = fr["stop"]
        ix = st0["x0"] + float(impact_uv[0]) * (st0["x1"] - st0["x0"])
        iy = st0["y0"] + float(impact_uv[1]) * (st0["y1"] - st0["y0"])
    else:
        ix, iy = 0.0, 1.62 - SLAB_CY
    ix, iy = _clamp_impact(ix, iy, fr)
    if rotation_deg is None:
        rotation_deg = map_rotation_deg(seed)
    rotation_deg = float(rotation_deg)
    rot = math.radians(rotation_deg)
    t_start = time.perf_counter()
    if P.get("mode") == "dice":
        res = _generate_dice(P, seed, ix, iy, rotation_deg, side, fr, w, h)
        if timing is not None:
            timing["gen_ms"] = (time.perf_counter() - t_start) * 1000.0
            timing["attempts"] = 1
        return res

    hx, hy = fr["hx"], fr["hy"]
    box = (-hx - 0.05, hx + 0.05, -hy - 0.05, hy + 0.05)      # crossings matter inside the slab (+5 cm)
    supp = {"chords": set(), "forks": set(), "kink_scale": {}}
    repairs = []
    last_err = None
    for attempt in range(max_attempts):
        try:
            g = _build_graph(P, seed, ix, iy, rot, fr, supp)
        except GenerationError as e:
            last_err = str(e)
            supp["global_kink"] = supp.get("global_kink", 1.0) * 0.5      # deterministic fallback
            repairs.append(("build_error_damp_kinks", last_err))
            continue
        V = g["V"]
        edges = g["edges"]
        edge_owner = {}
        for (u, v, kind, owner) in edges:
            edge_owner[(u, v)] = (kind, owner)
            edge_owner[(v, u)] = (kind, owner)

        # ---- crossings (prototype fix 1): drop the younger element and rebuild
        crossing = _graph_crossings(V, edges, limit_box=(box[0] - ix, box[1] - ix, box[2] - iy, box[3] - iy))
        if crossing:
            ea, eb = crossing[0]
            A, B = edges[ea], edges[eb]
            if _repair_crossing(A, B, g, supp, repairs):
                continue
            last_err = "unrepairable crossing"
            supp["global_kink"] = supp.get("global_kink", 1.0) * 0.5
            continue

        # ---- faces -> pieces in slab coordinates
        faces = _faces(V, edges)
        core_set = set(g["core_vids"])
        raw = []
        bad_face = False
        for f in faces:
            poly = [(V[i][0] + ix, V[i][1] + iy) for i in f]
            cp = clean_poly(poly)
            cl = clean_poly(clip_rect(cp, -hx, hx, -hy, hy))
            if len(cl) < 3 or area(cl) <= 1e-14:
                continue                                    # wholly outside the slab
            if not is_convex(cp, 1e-12):
                bad_face = True
                break
            is_core = len(f) == len(core_set) and set(f) == core_set
            raw.append({"poly": cl, "kind": "crush" if is_core else "shard", "vids": f, "full": cp})
        if bad_face:
            supp["global_kink"] = supp.get("global_kink", 1.0) * 0.5
            repairs.append(("nonconvex_face_damp_kinks", None))
            continue

        # ---- small faces (< min_piece_area inside the slab): drop a bounding chord, else a fork, else merge
        # batch: every small face drops its own outermost chord in the same rebuild (keyed RNG keeps the rest)
        small = sorted([r for r in raw if r["kind"] != "crush" and area(r["poly"]) < P["min_piece_area"]],
                       key=lambda q: (area(q["poly"]), _piece_key(q["poly"])))
        batch = []
        for r in small:
            ch = _outermost_chord(r, V, edge_owner, supp, batch)
            if ch is not None:
                batch.append(ch)
        rebuild = False
        if batch:
            for ch in batch:
                supp["chords"].add(ch)
                repairs.append(("small_drop_chord", list(ch)))
            rebuild = True
        else:
            unfixed = set()
            while True:
                small = [r for r in raw if r["kind"] != "crush" and area(r["poly"]) < P["min_piece_area"]
                         and _piece_key(r["poly"]) not in unfixed]
                if not small:
                    break
                r = min(small, key=lambda q: (area(q["poly"]), _piece_key(q["poly"])))
                n_before = len(raw)
                if _repair_small(r, raw, g, V, edge_owner, supp, repairs):
                    rebuild = True
                    break
                if len(raw) == n_before:
                    unfixed.add(_piece_key(r["poly"]))
        if rebuild:
            continue
        # ---- post-process: daggers, teeth, level-2 cells
        pieces = _post_process(raw, P, seed, ix, iy, fr)
        n_bodies = sum(1 for q_ in pieces if q_["kind"] == "shard")
        max_bodies = P.get("max_bodies", P["max_pieces"])
        excess = max(len(pieces) - P["max_pieces"], n_bodies - max_bodies)
        if excess > 0:
            # count cap (pieces, and moving bodies on WebGL): drop OUTER ring chords whose union stays shorter than
            # dagger_max, so each drop merges two pieces. Preference: tier 0 = both faces free shards and small,
            # tier 1 = free shards, tier 2 = a face touches the frame. The dense web near the impact, the part
            # that reads as a strike, goes last. If no chord qualifies, the outermost fork goes.
            by_owner = {}
            for rr in raw:
                if rr["kind"] == "crush":
                    continue
                f = rr["vids"]
                for i in range(len(f)):
                    kind, owner = edge_owner[(f[i], f[(i + 1) % len(f)])]
                    if kind == "chord" and owner not in supp["chords"]:
                        by_owner.setdefault(owner, []).append(rr)
            cands = []
            for owner, fs in by_owner.items():
                if len(fs) != 2:
                    continue
                p0, p1 = fs[0]["poly"], fs[1]["poly"]
                if diameter(convex_hull(p0 + p1))[0] > P["dagger_max"]:
                    continue
                a_u = area(p0) + area(p1)
                framed = max(_contacts(p0, fr).values()) > 0.0 or max(_contacts(p1, fr).values()) > 0.0
                tier = 2 if framed else (0 if a_u <= P["cap_merge_area"] else 1)
                cands.append((tier, -owner[0], a_u, owner))
            if cands:
                cands.sort()
                for cand in cands[:excess]:
                    supp["chords"].add(cand[3])
                    repairs.append(("cap_drop_outer_chord", list(cand[3])))
                continue
            fks = sorted([f_ for f_ in g["forks"] if f_["key"] not in supp["forks"]], key=lambda f_: (-f_["ring"], f_["key"]))
            if fks:
                supp["forks"].add(fks[0]["key"])
                repairs.append(("cap_drop_fork", list(fks[0]["key"])))
                continue
            repairs.append(("cap_unmet", len(pieces)))
        res = _assemble(P, seed, ix, iy, rotation_deg, side, fr, w, h, g, pieces, repairs, attempt)
        if timing is not None:          # timing stays OUT of the JSON, so the output is byte-deterministic
            timing["gen_ms"] = (time.perf_counter() - t_start) * 1000.0
            timing["attempts"] = attempt + 1
        return res
    raise GenerationError("no valid pattern after %d attempts (%s)" % (max_attempts, last_err))


def _outermost_chord(r, V, edge_owner, supp, taken):
    """The outermost ring chord bounding face r that is not suppressed or already taken, or None."""
    f = r["vids"]
    cand = []
    for i in range(len(f)):
        kind, owner = edge_owner[(f[i], f[(i + 1) % len(f)])]
        if kind == "chord" and owner not in supp["chords"] and owner not in taken:
            a = V[f[i]]
            b = V[f[(i + 1) % len(f)]]
            cand.append((math.hypot(0.5 * (a[0] + b[0]), 0.5 * (a[1] + b[1])), owner))
    if not cand:
        return None
    cand.sort(reverse=True)
    return cand[0][1]


def _repair_crossing(A, B, g, supp, repairs):
    """Drop a chord of the crossing pair, else the younger fork, else damp both tracks' kinks."""
    for E in (A, B):
        if E[2] == "chord" and E[3] not in supp["chords"]:
            supp["chords"].add(E[3])
            repairs.append(("crossing_drop_chord", list(E[3])))
            return True
    for E in (A, B):
        if E[2] == "track":
            t = g["by_tid"][E[3]]
            if not t.initial:
                fk = next(f for f in g["forks"] if f["child"] == t.tid)
                if fk["key"] not in supp["forks"]:
                    supp["forks"].add(fk["key"])
                    repairs.append(("crossing_drop_fork", list(fk["key"])))
                    return True
    done = False
    for E in (A, B):
        if E[2] == "track":
            cur = supp["kink_scale"].get(E[3], 1.0)
            if cur > 0.0:
                supp["kink_scale"][E[3]] = 0.0 if cur <= 0.5 else 0.5
                done = True
    if done:
        repairs.append(("crossing_damp_kinks", [A[3], B[3]]))
    return done


def _repair_small(r, raw, g, V, edge_owner, supp, repairs):
    """A face smaller than min_piece_area inside the slab: drop its outermost chord, else its fork, else merge it
    into a neighbour when the union is convex (slivers clipped by the slab edge). Returns True to rebuild."""
    f = r["vids"]
    cand = []
    for i in range(len(f)):
        kind, owner = edge_owner[(f[i], f[(i + 1) % len(f)])]
        if kind == "chord" and owner not in supp["chords"]:
            a = V[f[i]]
            b = V[f[(i + 1) % len(f)]]
            cand.append((math.hypot(0.5 * (a[0] + b[0]), 0.5 * (a[1] + b[1])), owner))
    if cand:
        cand.sort(reverse=True)
        supp["chords"].add(cand[0][1])
        repairs.append(("small_drop_chord", list(cand[0][1])))
        return True
    for i in range(len(f)):
        kind, owner = edge_owner[(f[i], f[(i + 1) % len(f)])]
        if kind == "track":
            t = g["by_tid"][owner]
            if not t.initial:
                fk = next(ff for ff in g["forks"] if ff["child"] == t.tid)
                if fk["key"] not in supp["forks"]:
                    supp["forks"].add(fk["key"])
                    repairs.append(("small_drop_fork", list(fk["key"])))
                    return True
    others = sorted([o for o in raw if o is not r and o["kind"] != "crush"], key=lambda o: _piece_key(o["poly"]))
    for other in others:
        if not _shares_edge(r["poly"], other["poly"]):
            continue
        m = _merge_convex(r["poly"], other["poly"])
        if m is not None:
            other["poly"] = m
            raw.remove(r)
            repairs.append(("small_merge_neighbour", round(area(r["poly"]) * 1e4, 4)))
            return False
    repairs.append(("small_unfixed", round(area(r["poly"]) * 1e4, 4)))
    return False


def _post_process(raw, P, seed, ix, iy, fr):
    """Daggers -> teeth -> level-2 sub-cells. Returns the final list of piece dicts (slab coords)."""
    out = []
    shards = []
    for r in raw:
        if r["kind"] == "crush":
            out.append({"poly": r["poly"], "kind": "crush"})
        else:
            for part in _split_dagger(r["poly"], P, seed):
                shards.append(part)
    for poly in shards:
        c = _contacts(poly, fr)
        if max(c.values()) < 0.005:
            out.append({"poly": poly, "kind": "shard"})
            continue
        rng = Rng(seed, S_TEETH, *_piece_key(poly))
        order = sorted(c.keys(), key=lambda s_: (-c[s_], s_))
        # long contact: cracks into the frame first (each part keeps its own tooth)
        parts = _split_long_for_teeth(poly, order[0], fr, P, Rng(seed, S_SPLIT, 7, *_piece_key(poly)))
        for part in parts:
            out.extend(_teeth_for(part, P, seed, fr))
    # level-2 sub-cells for the floor re-break (desktop only)
    for p in out:
        if p["kind"] == "shard" and P["level2_area"] > 0.0 and area(p["poly"]) > P["level2_area"]:
            subs = _level2(p["poly"], P, seed)
            p["sub"] = subs if len(subs) > 1 else []
        else:
            p["sub"] = []
    return out


def _corner_of(poly, fr, tol=1e-9):
    """The slab corner this piece contains ('TL', 'TR', 'BL', 'BR'), or None."""
    hx, hy = fr["hx"], fr["hy"]
    for name, (cx, cy) in (("TL", (-hx, hy)), ("TR", (hx, hy)), ("BL", (-hx, -hy)), ("BR", (hx, -hy))):
        for (x, y) in poly:
            if abs(x - cx) <= tol and abs(y - cy) <= tol:
                return name
    return None


def _corner_cut(poly, corner, fr, P, rng):
    """A tooth across a slab corner: the triangle cut by a crack from a point a along the horizontal edge to a
    point b along the vertical edge. Legs are a multiple of the band depth, chosen so the clear zone's corner
    stays outside the triangle (band_x / a + band_y / b > 1). Returns (tooth, rest) or None."""
    hx, hy = fr["hx"], fr["hy"]
    sx = 1.0 if corner[1] == "R" else -1.0
    sy = 1.0 if corner[0] == "T" else -1.0
    side_x = "R" if sx > 0 else "L"          # the jamb (vertical edge) of this corner
    side_y = "T" if sy > 0 else "B"          # the head or sill (horizontal edge)
    bx, by = fr["band"][side_x], fr["band"][side_y]
    lo, hi = P["corner_leg"]
    la, lb = rng.uniform(lo, hi), rng.uniform(lo, hi)
    if 1.0 / la + 1.0 / lb <= 1.02:
        lb = 1.0 / (1.02 - 1.0 / la) if 1.02 - 1.0 / la > 1e-6 else hi
    a = la * bx                               # along the horizontal edge, from the corner
    b = lb * by                               # along the vertical edge, from the corner
    cx, cy = sx * hx, sy * hy
    p1 = (cx - sx * a, cy)                    # on the horizontal edge
    p2 = (cx, cy - sy * b)                    # on the vertical edge
    # half-plane containing the corner: n.p >= n.p1 with n pointing to the corner
    ex, ey = p2[0] - p1[0], p2[1] - p1[1]
    nx, ny = -ey, ex
    if nx * (cx - p1[0]) + ny * (cy - p1[1]) < 0.0:
        nx, ny = -nx, -ny
    c = nx * p1[0] + ny * p1[1]
    tooth = clean_poly(clip_half(poly, -nx, -ny, -c))
    rest = clean_poly(clip_half(poly, nx, ny, c))
    return tooth, rest


def _teeth_for(poly, P, seed, fr):
    """Teeth that stay in the stop (plan sec 2.2): a subset of the frame pieces, each clipped to the tooth band.
    Corner pieces may keep a corner tooth (the largest teeth, where radial cracks run out); other frame pieces
    keep a tooth per side with probability tooth_keep[side], cut by a tilted crack inside the band."""
    c = _contacts(poly, fr)
    if max(c.values()) < 0.005:
        return [{"poly": poly, "kind": "shard"}]
    key = _piece_key(poly)
    rng = Rng(seed, S_TEETH, 1, *key)
    corner = _corner_of(poly, fr)
    if corner is not None and P.get("corner_tooth_p", 0.0) > 0.0 and fr["band_mode"] == "band":
        rc = Rng(seed, S_TEETH, 3, *key)
        if rc.u01() < P["corner_tooth_p"]:
            for attempt in range(3):
                cut = _corner_cut(poly, corner, fr, P, Rng(seed, S_TEETH, 4, attempt, *key))
                if cut is None:
                    break
                tooth, rest = cut
                if len(tooth) < 3 or area(tooth) < P["min_piece_area"] or _in_clear_area(tooth, fr) > 1e-12:
                    continue
                if len(rest) >= 3 and area(rest) >= P["min_piece_area"]:
                    return [{"poly": tooth, "kind": "tooth", "side": corner[1], "corner": corner},
                            {"poly": rest, "kind": "shard"}]
                if len(rest) < 3 or area(rest) < 1e-10:
                    return [{"poly": tooth, "kind": "tooth", "side": corner[1], "corner": corner}]
    sides = sorted([s_ for s_ in c if c[s_] >= 0.005], key=lambda s_: (-c[s_], s_))
    result = []
    rest = poly
    for side in sides:
        if rng.u01() >= P["tooth_keep"][side]:
            continue
        cc = _contacts(rest, fr)
        if cc[side] < 0.005:
            continue
        # the whole remaining piece fits in the band: it stays as one tooth
        if _in_clear_area(rest, fr) < 1e-12 and area(rest) >= P["min_piece_area"] and \
                fr["band"][side] > fr["pocket"][side] + 1e-9:
            result.append({"poly": rest, "kind": "tooth", "side": side})
            rest = None
            break
        for attempt in range(4):
            cut = _tooth_cut(rest, side, fr, P, Rng(seed, S_TEETH, 2, attempt, *key), attempt)
            if cut is None:
                break
            tooth, rest2 = cut
            if len(tooth) < 3 or len(rest2) < 3:
                continue
            if area(tooth) < P["min_piece_area"] or area(rest2) < P["min_piece_area"]:
                continue
            if _in_clear_area(tooth, fr) > 1e-12:
                continue
            result.append({"poly": tooth, "kind": "tooth", "side": side})
            rest = rest2
            break
    if rest is not None:
        result.append({"poly": rest, "kind": "shard"})
    return result


def _pose(poly, kind, P, seed, ix, iy, side):
    if kind != "shard":
        return {"pivot": [0.0, 0.0], "axis": [1.0, 0.0], "deg": 0.0, "push": 0.0}
    rng = Rng(seed, S_POSE, *_piece_key(poly))
    c = centroid(poly)
    r = math.hypot(c[0] - ix, c[1] - iy)
    w = math.exp(-r / P["pose_falloff"])
    tilt = rng.uniform(P["tilt_deg"][0], P["tilt_deg"][1]) * w
    push = rng.uniform(P["push_m"][0], P["push_m"][1]) * w
    jit = math.radians(rng.uniform(-P["tilt_axis_jitter_deg"], P["tilt_axis_jitter_deg"]))
    # pivot: the vertex farthest from the impact (the inner end swings to the far side)
    far = max(poly, key=lambda p: (p[0] - ix) ** 2 + (p[1] - iy) ** 2)
    inx, iny = ix - far[0], iy - far[1]
    L = math.hypot(inx, iny) or 1.0
    inx, iny = inx / L, iny / L
    ax, ay = iny, -inx                      # rotation about +a moves the inward side toward +far
    ca, sa = math.cos(jit), math.sin(jit)
    ax, ay = ax * ca - ay * sa, ax * sa + ay * ca
    return {"pivot": [far[0], far[1]], "axis": [ax, ay], "deg": tilt, "push": push}


def _release(poly, kind, P, seed, ix, iy, fr):
    if kind != "shard":
        return None
    rng = Rng(seed, S_POSE, 99, *_piece_key(poly))
    c = centroid(poly)
    r = math.hypot(c[0] - ix, c[1] - iy)
    st = fr["stop"]
    dstop = _ray_rect_exit(ix, iy, (c[0] - ix) / (r or 1.0), (c[1] - iy) / (r or 1.0), st["x0"], st["x1"], st["y0"], st["y1"])
    edge = max(_contacts(poly, fr).values()) >= 0.005
    if r < P["release_inner_r"]:
        ms, wave = 0.0, 0
    elif edge or r > 0.75 * dstop:
        ms, wave = rng.uniform(*P["release_outer_ms"]), 2
    else:
        f = min(1.0, max(0.0, (r - P["release_inner_r"]) / max(1e-6, 0.75 * dstop - P["release_inner_r"])))
        ms = P["release_mid_ms"][0] + f * (P["release_mid_ms"][1] - P["release_mid_ms"][0]) + rng.uniform(-10.0, 10.0)
        ms, wave = max(P["release_mid_ms"][0], min(P["release_mid_ms"][1], ms)), 1
    speed = rng.uniform(*P["release_speed"]) * math.exp(-r / P["release_speed_falloff"])
    return {"wave": wave, "ms": ms, "speed": speed}


def pose_point(p, pose, far_sign):
    """Stage-2 pose of one slab point p = (x, y, z): slab-local metres, z across the glass (+z = toward cell b).

    Reference for every consumer (Blender look-dev, the C# meshes, the ray tracer): rotate by pose["deg"] about
    the in-plane axis pose["axis"] through pose["pivot"], in the sense that moves the piece's IMPACT side toward
    the FAR side, then push by pose["push"] toward the far side. far_sign = the output's far_z_sign: +1 when the
    far side is +z (struck from cell a, side = +1), else -1. Computed in a right-handed (x, y, f) frame with
    f = z * far_sign; with axis a = (ax, ay, 0) Rodrigues' formula gives the impact side +f (see _pose)."""
    th = math.radians(pose["deg"])
    ax, ay = pose["axis"]
    px, py = pose["pivot"]
    vx, vy, vf = p[0] - px, p[1] - py, p[2] * far_sign
    c, s = math.cos(th), math.sin(th)
    kdotv = ax * vx + ay * vy
    # k x v with k = (ax, ay, 0): (ay*vf, -ax*vf, ax*vy - ay*vx)
    cx, cy, cf = ay * vf, -ax * vf, ax * vy - ay * vx
    rx = vx * c + cx * s + ax * kdotv * (1.0 - c)
    ry = vy * c + cy * s + ay * kdotv * (1.0 - c)
    rf = vf * c + cf * s
    return (px + rx, py + ry, (rf + pose["push"]) * far_sign)


def _tris_bevel(n):
    return 12 * n - 4     # 6 mm prism, every edge bevelled with 1 segment (matches the 04 prototype counts)


def _tris_flat(n):
    return 4 * n - 4


def _assemble(P, seed, ix, iy, rotation_deg, side, fr, w, h, g, pieces, repairs, attempts):
    V = g["V"]
    # ---- stage 1: 5-8 of the initial radials, to 25-35 % of the way to the stop line
    rng = Rng(seed, S_STAGE1)
    n_s1 = min(g["n0"], rng.randint(P["stage1_count"][0], P["stage1_count"][1]))
    initial = [t for t in g["tracks"] if t.initial]
    # pick by keyed score (stable): the n_s1 lowest scores
    scored = sorted(initial, key=lambda t: (Rng(seed, S_STAGE1, 1, t.tid).u01(), t.tid))
    chosen = sorted(scored[:n_s1], key=lambda t: t.tid)
    st = fr["stop"]
    s1 = []
    for t in chosen:
        reach_f = Rng(seed, S_STAGE1, 2, t.tid).uniform(P["stage1_reach"][0], P["stage1_reach"][1])
        th = g["theta0"][t.tid]
        dstop = _ray_rect_exit(ix, iy, math.cos(th), math.sin(th), st["x0"], st["x1"], st["y0"], st["y1"])
        reach = reach_f * dstop
        pts = [V[t.pts[0]]]
        for i in range(1, len(t.pts)):
            p = V[t.pts[i]]
            rp = math.hypot(p[0], p[1])
            if rp >= reach:
                q = pts[-1]
                rq = math.hypot(q[0], q[1])
                # interpolate on the segment to radius 'reach' (radius is monotonic along a track)
                lo, hi = 0.0, 1.0
                for _ in range(60):
                    m = 0.5 * (lo + hi)
                    x = q[0] + (p[0] - q[0]) * m
                    y = q[1] + (p[1] - q[1]) * m
                    if math.hypot(x, y) < reach:
                        lo = m
                    else:
                        hi = m
                pts.append((q[0] + (p[0] - q[0]) * hi, q[1] + (p[1] - q[1]) * hi))
                break
            pts.append(p)
        s1.append({"track": t.tid, "reach": reach, "reach_frac": reach_f,
                   "points": [(x + ix, y + iy) for x, y in pts]})
    # ---- chips (particle spawn points on the new cracks of each stage)
    rngc = Rng(seed, S_CHIPS, 1)
    chips1 = []
    n1 = rngc.randint(P["chips_s1"][0], P["chips_s1"][1])
    for i in range(n1):
        cr_ = s1[rngc.next_u32() % len(s1)]["points"]
        seg = rngc.next_u32() % (len(cr_) - 1)
        f = rngc.u01()
        a, b = cr_[seg], cr_[seg + 1]
        chips1.append((a[0] + (b[0] - a[0]) * f, a[1] + (b[1] - a[1]) * f))
    rngc2 = Rng(seed, S_CHIPS, 2)
    n2 = rngc2.randint(P["chips_s2"][0], P["chips_s2"][1])
    shard_list = [p for p in pieces if p["kind"] != "crush"]
    chips2 = []
    cz = fr["clear"]
    for i in range(n2 * 8):
        if len(chips2) >= n2:
            break
        pc = shard_list[rngc2.next_u32() % len(shard_list)]["poly"]
        e = rngc2.next_u32() % len(pc)
        f = rngc2.u01()
        a, b = pc[e], pc[(e + 1) % len(pc)]
        q = (a[0] + (b[0] - a[0]) * f, a[1] + (b[1] - a[1]) * f)
        if abs(q[0]) >= fr["hx"] - 1e-6 or abs(q[1]) >= fr["hy"] - 1e-6:
            continue        # on the glass edge: not a crack
        chips2.append(q)

    # ---- final piece records (sorted for stable ids: by kind, then distance from the impact, then angle)
    kind_rank = {"crush": 0, "shard": 1, "tooth": 2}
    recs = []
    for p in pieces:
        poly = p["poly"]
        c = centroid(poly)
        r = math.hypot(c[0] - ix, c[1] - iy)
        ang = math.atan2(c[1] - iy, c[0] - ix)
        recs.append((kind_rank[p["kind"]], round(r, 6), round(ang, 6), p))
    recs.sort(key=lambda q: (q[0], q[1], q[2]))
    rings = g["rings"]
    sectors = sorted(g["theta0"])
    out_pieces = []
    for idx, (_, _, _, p) in enumerate(recs):
        poly = p["poly"]
        c = centroid(poly)
        r = math.hypot(c[0] - ix, c[1] - iy)
        band = 0
        while band + 1 < len(rings) and rings[band + 1] <= r:
            band += 1
        ang = math.atan2(c[1] - iy, c[0] - ix)
        rel = (ang - g["theta0"][0]) % TWO_PI
        sector = 0
        for si in range(len(g["theta0"])):
            if ((g["theta0"][si] - g["theta0"][0]) % TWO_PI) <= rel + 1e-12:
                sector = si
        rec = {"id": idx, "kind": p["kind"], "poly": poly, "area": area(poly), "centroid": c, "r": r,
               "band": band, "sector": sector, "cluster": band * 64 + sector}
        if p["kind"] == "tooth":
            rec["side"] = p["side"]
            rec["edge"] = {"L": "left", "R": "right", "T": "top", "B": "bottom"}[p["side"]]
            if p.get("corner"):
                rec["corner"] = p["corner"]
        rec["pose"] = _pose(poly, p["kind"], P, seed, ix, iy, side)
        rel_ = _release(poly, p["kind"], P, seed, ix, iy, fr)
        if rel_ is not None:
            rec["release"] = rel_
        rec["sub"] = p.get("sub", [])
        out_pieces.append(rec)

    # ---- graph export (slab coords), clipped edges are kept whole for debugging
    tracks_out = []
    for t in g["tracks"]:
        tracks_out.append({"id": t.tid, "parent": t.parent, "initial": t.initial,
                           "points": [(V[i][0] + ix, V[i][1] + iy) for i in t.pts]})
    chords_out = [{"ring": c["ring"], "a": c["a"], "b": c["b"],
                   "p": [(V[c["va"]][0] + ix, V[c["va"]][1] + iy), (V[c["vb"]][0] + ix, V[c["vb"]][1] + iy)]}
                  for c in g["chords"]]
    crush = next((p for p in out_pieces if p["kind"] == "crush"), None)
    crater_r = max(math.hypot(x - ix, y - iy) for x, y in crush["poly"]) if crush else 0.0
    res = {
        "version": VERSION,
        "profile": P["name"], "glass": P["glass"], "tier": P["tier"],
        "seed": seed, "rotation_deg": rotation_deg, "side": side, "far_z_sign": 1 if side > 0 else -1,
        "impact": [ix, iy], "impact_root": [ix, iy + SLAB_CY],
        "impact_uv": _exposed_uv(ix, iy, fr),
        "slab": {"w": w, "h": h, "t": P["thickness"], "bevel": P["bevel"], "centre_root_y": SLAB_CY},
        "frame": {"pocket": fr["pocket"], "band": fr["band"], "band_mode": fr["band_mode"], "stop": fr["stop"],
                  "clear": fr["clear"]},
        "rings": rings,
        "graph": {"tracks": tracks_out, "chords": chords_out,
                  "forks": [{"ring": f["ring"], "parent": f["parent"], "child": f["child"],
                             "at": (V[f["vid"]][0] + ix, V[f["vid"]][1] + iy)} for f in g["forks"]]},
        "pieces": out_pieces,
        "stage1": {"cracks": s1, "crater": {"poly": crush["poly"] if crush else [], "radius": crater_r},
                   "chips": chips1},
        "stage2": {"chips": chips2, "removed": ["crush"]},
        "wire": {"grid": P["wire_grid"], "hangs": P["hangs_on_wire"]},
        "repairs": [[r[0], r[1]] for r in repairs],
        "attempts": attempts + 1,
    }
    res["stats"] = _stats(res, P, g)
    return _round_tree(res)


def _stats(res, P, g):
    pcs = res["pieces"]
    kinds = {k: sum(1 for p in pcs if p["kind"] == k) for k in ("shard", "tooth", "crush")}
    shard_areas = sorted(p["area"] for p in pcs if p["kind"] == "shard")

    def q(a, f):
        return a[min(len(a) - 1, int(len(a) * f))] if a else 0.0
    bevel = P["bevel"] > 0.0
    tri = _tris_bevel if bevel else _tris_flat
    tris_s2 = sum(tri(len(p["poly"])) for p in pcs if p["kind"] != "crush")
    tris_teeth = sum(tri(len(p["poly"])) for p in pcs if p["kind"] == "tooth")
    fin_segs = sum(len(c["points"]) - 1 for c in res["stage1"]["cracks"])
    crater_n = len(res["stage1"]["crater"]["poly"])
    plus = _plus_junctions(g)
    return {"pieces": kinds, "pieces_total": len(pcs), "bodies": kinds["shard"],
            "tracks_initial": g["n0"], "tracks_total": len(g["tracks"]), "forks": len(g["forks"]),
            "chords": len(g["chords"]), "rings": len(g["rings"]),
            "t_junctions": 2 * len(g["chords"]) + len(g["forks"]), "plus_junctions": plus,
            "kinks": g["log"]["kinks"], "kinks_zeroed": g["log"]["kinks_zeroed"],
            "shard_area_cm2_p10_p50_p90": [q(shard_areas, 0.1) * 1e4, q(shard_areas, 0.5) * 1e4, q(shard_areas, 0.9) * 1e4],
            "max_piece_len": max(diameter(p["poly"])[0] for p in pcs),
            "avg_verts": sum(len(p["poly"]) for p in pcs) / len(pcs),
            "tris_stage2": tris_s2, "tris_teeth": tris_teeth, "tris_fins": 2 * fin_segs, "tris_crater": P["crater_tris_per_vertex"] * crater_n,
            "level2_cells": sum(len(p["sub"]) for p in pcs),
            "stage1_cracks": len(res["stage1"]["cracks"])}


def _plus_junctions(g):
    """Vertices where two chords meet a radial at the same point (a '+'); must be 0."""
    cnt = {}
    for c in g["chords"]:
        for v in (c["va"], c["vb"]):
            cnt[v] = cnt.get(v, 0) + 1
    return sum(1 for v in cnt.values() if v > 1)


def _round_tree(x):
    if isinstance(x, float):
        r = round(x, 7)
        return 0.0 if r == 0.0 else r
    if isinstance(x, dict):
        return {k: _round_tree(v) for k, v in x.items()}
    if isinstance(x, (list, tuple)):
        return [_round_tree(v) for v in x]
    return x


# ---------------------------------------------------------------------------------------------------------
# Tempered (PARKED, data only): the whole pane dices at 1.0 s; clumps of ~1 cm granules, no teeth
# ---------------------------------------------------------------------------------------------------------
def _generate_dice(P, seed, ix, iy, rotation_deg, side, fr, w, h):
    """Tempered6 (PARKED, data only; plan sec 2.1): no crack stages. At the shatter the whole pane dices into
    ~9 mm granules (EN 12150: >= 40 particles in 50 x 50 mm). For physics the pane is cut into ~16 cm CLUMPS
    (a jittered grid) that leave the frame together and fall apart into granules; no teeth stay in the stop."""
    hx, hy = fr["hx"], fr["hy"]
    nx = max(2, int(round(w / P["clump"])))
    ny = max(2, int(round(h / P["clump"])))
    dx, dy = w / nx, h / ny
    J = P["clump_jitter"] * 0.5
    nodes = {}
    for i in range(nx + 1):
        for j in range(ny + 1):
            x = -hx + i * dx
            y = -hy + j * dy
            r = Rng(seed, S_DICE, i, j)
            jx, jy = r.uniform(-J, J) * dx, r.uniform(-J, J) * dy
            if 0 < i < nx:
                x += jx
            if 0 < j < ny:
                y += jy
            nodes[(i, j)] = (x, y)
    pieces = []
    for i in range(nx):
        for j in range(ny):
            poly = [nodes[(i, j)], nodes[(i + 1, j)], nodes[(i + 1, j + 1)], nodes[(i, j + 1)]]
            c = centroid(poly)
            rr = math.hypot(c[0] - ix, c[1] - iy)
            rng = Rng(seed, S_DICE, 1000 + i, j)
            pieces.append({"id": len(pieces), "kind": "clump", "poly": poly, "area": area(poly), "centroid": c,
                           "r": rr, "granules": int(area(poly) / P["granule"] ** 2),
                           # the crazed sheet holds for the 50 ms hit-stop, then sags out of the frame from the
                           # impact outward; clumps near the impact are thrown, the rest drop
                           "release": {"wave": 0, "ms": min(80.0, 60.0 * rr) + rng.uniform(0.0, 15.0),
                                       "speed": rng.uniform(1.0, 2.0) * math.exp(-rr / 0.35)}})
    res = {"version": VERSION, "profile": P["name"], "glass": P["glass"], "tier": P["tier"], "seed": seed,
           "rotation_deg": rotation_deg, "side": side, "far_z_sign": 1 if side > 0 else -1,
           "impact": [ix, iy], "impact_root": [ix, iy + SLAB_CY], "impact_uv": _exposed_uv(ix, iy, fr),
           "slab": {"w": w, "h": h, "t": P["thickness"], "bevel": 0.0, "centre_root_y": SLAB_CY},
           "frame": {"pocket": fr["pocket"], "band": fr["band"], "band_mode": fr["band_mode"], "stop": fr["stop"],
                     "clear": fr["clear"]},
           "mode": "dice", "granule": P["granule"], "pieces": pieces,
           "stage1": {"cracks": [], "crater": {"poly": [], "radius": 0.0}, "chips": []},
           "stage2": {"chips": [], "removed": []},
           "stats": {"pieces": {"clump": len(pieces)}, "pieces_total": len(pieces),
                     "granules_estimate": int(w * h / P["granule"] ** 2)}}
    return _round_tree(res)


# ---------------------------------------------------------------------------------------------------------
# Validation (plan sec 2.6; also the C# edit-mode test list)
# ---------------------------------------------------------------------------------------------------------
def validate(res, P=None):
    """Returns {check: {"ok": bool, ...}}. Works on the ROUNDED output (what consumers read)."""
    P = P or PROFILES[res["profile"]]
    out = {}
    w, h = res["slab"]["w"], res["slab"]["h"]
    fr = frame_for_slab(w, h, res["frame"].get("band_mode", "band"))
    pcs = res["pieces"]
    polys = [[tuple(p) for p in pc["poly"]] for pc in pcs]
    total = sum(area(p) for p in polys)
    rel = (total - w * h) / (w * h)
    out["area_sum"] = {"ok": abs(rel) <= 0.0005, "rel_error_pct": rel * 100.0}
    # overlaps (bbox broad phase)
    boxes = [(min(x for x, _ in p), max(x for x, _ in p), min(y for _, y in p), max(y for _, y in p)) for p in polys]
    worst = 0.0
    for i in range(len(polys)):
        for j in range(i + 1, len(polys)):
            a, b = boxes[i], boxes[j]
            if a[1] < b[0] or b[1] < a[0] or a[3] < b[2] or b[3] < a[2]:
                continue
            worst = max(worst, convex_overlap_area(polys[i], polys[j]))
    # outputs are rounded to 1e-7 m, so two neighbours can overlap by a sliver of (edge length x 1e-7 m);
    # 0.1 mm2 is far below any real defect (the 04 prototype's hole was 39 cm2)
    out["no_overlap"] = {"ok": worst < 1e-7, "worst_overlap_mm2": worst * 1e6}
    # convex
    bad = [pc["id"] for pc, p in zip(pcs, polys) if not is_convex(p, 1e-11)]
    out["convex"] = {"ok": not bad, "non_convex_ids": bad}
    # min area (outside the crush core), level-2 cells too
    small = [(pc["id"], round(pc["area"] * 1e4, 4)) for pc in pcs if pc["kind"] != "crush" and pc["area"] < P["min_piece_area"] - 1e-12]
    subsmall = []
    for pc in pcs:
        for s in pc.get("sub", []):
            if area([tuple(q) for q in s]) < P["min_piece_area"] - 1e-12:
                subsmall.append(pc["id"])
    mn = min((pc["area"] for pc in pcs if pc["kind"] != "crush"), default=0.0)
    out["min_area"] = {"ok": not small and not subsmall, "min_cm2": mn * 1e4, "small": small, "small_sub_of": subsmall}
    # crossings of the crack graph (tracks + chords) inside the slab
    if res.get("graph"):
        V = []
        E = []
        index = {}

        def vid(p):
            k = (round(p[0], 9), round(p[1], 9))
            if k not in index:
                index[k] = len(V)
                V.append((p[0], p[1]))
            return index[k]
        for t in res["graph"]["tracks"]:
            ids = [vid(p) for p in t["points"]]
            for i in range(len(ids) - 1):
                E.append((ids[i], ids[i + 1], "track", t["id"]))
        for c in res["graph"]["chords"]:
            E.append((vid(c["p"][0]), vid(c["p"][1]), "chord", c["ring"]))
        cr = _graph_crossings(V, E, limit_box=(-w / 2, w / 2, -h / 2, h / 2))
        out["no_crossings"] = {"ok": not cr, "crossings": len(cr)}
    # teeth: inside the band, held by the edge, nothing in the clear zone
    tbad = []
    for pc, p in zip(pcs, polys):
        if pc["kind"] != "tooth":
            continue
        if _in_clear_area(p, fr) > 1e-10 or max(_contacts(p, fr).values()) < 0.004:
            tbad.append(pc["id"])
        if any(abs(x) > fr["hx"] + 1e-9 or abs(y) > fr["hy"] + 1e-9 for x, y in p):
            tbad.append(pc["id"])
    out["teeth_in_band"] = {"ok": not tbad, "bad": tbad, "teeth": sum(1 for pc in pcs if pc["kind"] == "tooth")}
    # impact clamp
    ix, iy = res["impact"]
    st = fr["stop"]
    m = IMPACT_MIN_FROM_STOP - 1e-9
    out["impact_clamp"] = {"ok": st["x0"] + m <= ix <= st["x1"] - m and st["y0"] + m <= iy <= st["y1"] - m}
    if res.get("mode") != "dice":
        s = res["stats"]
        out["budget"] = {"ok": s["pieces_total"] <= P["max_pieces"] and s["bodies"] <= P.get("max_bodies", P["max_pieces"])
                         and s["tris_stage2"] <= P["budget_s2_tris"]
                         and s["tris_teeth"] <= P["budget_teeth_tris"] and s["tris_fins"] <= P["budget_fin_tris"]
                         and s["tris_crater"] <= P["budget_crater_tris"],
                         "pieces": s["pieces_total"], "bodies": s["bodies"], "tris_stage2": s["tris_stage2"], "tris_teeth": s["tris_teeth"],
                         "tris_fins": s["tris_fins"], "tris_crater": s["tris_crater"]}
        # V2 from the geometry: every ring-crack end is a T on a radial; two ring cracks never meet a radial at
        # the same point ('+'), and the two T's on a radial are at least t_sep apart
        ends = {}
        for c in res["graph"]["chords"]:
            for p in c["p"]:
                key = (round(p[0], 6), round(p[1], 6))
                ends[key] = ends.get(key, 0) + 1
        plus = sum(1 for n in ends.values() if n > 1)
        out["v2_t_not_plus"] = {"ok": plus == 0, "plus_junctions": plus, "t_junctions": len(ends) + s["forks"],
                                "forks": s["forks"], "chords": len(res["graph"]["chords"])}
        out["no_unfixed_repairs"] = {"ok": not any(r[0] == "small_unfixed" for r in res["repairs"]),
                                     "repairs": len(res["repairs"])}
        out["crater"] = {"ok": res["stage1"]["crater"]["radius"] <= 0.015 + 1e-9,
                         "radius_mm": res["stage1"]["crater"]["radius"] * 1000.0}
    # stored areas match the polygons (consumers read "area" directly)
    worst_a = max((abs(pc["area"] - area(p)) for pc, p in zip(pcs, polys)), default=0.0)
    out["area_field"] = {"ok": worst_a < 2e-7, "worst_mm2": worst_a * 1e6}     # floats are rounded to 1e-7
    if res.get("mode") != "dice" and res.get("graph"):
        # stage 1 is a subset of stage 2: every Crack1 fin lies on a radial track of the stage-2 graph
        worst = 0.0
        tracks = {t["id"]: t["points"] for t in res["graph"]["tracks"]}
        for c in res["stage1"]["cracks"]:
            pts = tracks.get(c["track"], [])
            for q in c["points"]:
                d = min((_dist_point_seg(q, pts[i], pts[i + 1]) for i in range(len(pts) - 1)), default=1.0)
                worst = max(worst, d)
        out["stage1_on_stage2"] = {"ok": worst < 1e-6, "worst_mm": worst * 1000.0}
    out["all_ok"] = all(v["ok"] for v in out.values() if isinstance(v, dict))
    return out


def _dist_point_seg(q, a, b):
    ex, ey = b[0] - a[0], b[1] - a[1]
    L2 = ex * ex + ey * ey
    t = 0.0 if L2 <= 0.0 else max(0.0, min(1.0, ((q[0] - a[0]) * ex + (q[1] - a[1]) * ey) / L2))
    return math.hypot(q[0] - a[0] - t * ex, q[1] - a[1] - t * ey)


def to_json(res):
    return json.dumps(res, sort_keys=True, separators=(",", ":"))


# ---------------------------------------------------------------------------------------------------------
# Golden seeds (the step-3 oracle) and look-dev sample set
# ---------------------------------------------------------------------------------------------------------
LAB_HITS = {"H1": (0.0, 1.62), "H2": (0.40, 0.90), "H3": (-0.4835, 1.30)}   # window root (build/01_setup.md sec 2.3)
GRID_X = (-0.40, 0.0, 0.40)
GRID_Y = (0.90, 1.30, 1.65)                                                 # 06w sec 4.4 bake centres
LOOKDEV_SEEDS = (4242, 1990)                                                # plan sec 4.1: 3 impact spots x 2 seeds
SIDE_A, SIDE_B = 1, -1      # GlassBreakRecord.side: struck from cell a (the lab's player room, root z < 0) / from b


def lab_rotation(seed):
    """The rotation of the look-dev sign-off set and the lab cases (rendered 2026-10-03): (seed % 3600) * 0.1, so
    seed 4242 -> 64.2 deg and seed 1990 -> 199.0 deg. Pinned, so the signed-off renders stay reproducible."""
    return (int(seed) % 3600) * 0.1


def golden_cases():
    """The 20 golden cases (the step-3 oracle for the C# port):
      g00-g08  the 9 bake centres (06w sec 4.4), full 32-bit seeds and the map's default rotation (map_rotation_deg);
      g09-g11  the 3 lab hits, seed 4242 at 64.2 deg (the look-dev set);
      g12      a strike from cell b (side -1), seed 1990 at 199.0 deg;
      g13-g14  the two clamp corners (impacts outside the 0.2 m clamp);
      g15-g17  the WebGL and Cinematic tiers;
      g18      the stop-line fallback for the tooth band;
      g19      the parked tempered profile."""
    cases = []
    i = 0
    for y in GRID_Y:
        for x in GRID_X:
            seed = mix_key(20261003, i) & MASK32
            cases.append({"profile": "Annealed6", "seed": seed, "impact_root": (x, y), "side": SIDE_A, "band_mode": "band",
                          "rotation_deg": None})
            i += 1
    for name in ("H1", "H2", "H3"):
        cases.append({"profile": "Annealed6", "seed": 4242, "impact_root": LAB_HITS[name], "side": SIDE_A, "band_mode": "band",
                      "rotation_deg": lab_rotation(4242)})
    cases.append({"profile": "Annealed6", "seed": 1990, "impact_root": LAB_HITS["H1"], "side": SIDE_B, "band_mode": "band",
                  "rotation_deg": lab_rotation(1990)})
    cases.append({"profile": "Annealed6", "seed": 7, "impact_root": (0.60, 0.40), "side": SIDE_A, "band_mode": "band",
                  "rotation_deg": 123.4})   # clamped
    cases.append({"profile": "Annealed6", "seed": 8, "impact_root": (-0.60, 1.95), "side": SIDE_A, "band_mode": "band",
                  "rotation_deg": 301.7})   # clamped
    for prof, hit in (("Annealed6_WebGL", "H1"), ("Annealed6_WebGL", "H2"), ("Annealed6_Cinematic", "H1")):
        cases.append({"profile": prof, "seed": 4242, "impact_root": LAB_HITS[hit], "side": SIDE_A, "band_mode": "band",
                      "rotation_deg": lab_rotation(4242)})
    cases.append({"profile": "Annealed6", "seed": 4242, "impact_root": LAB_HITS["H2"], "side": SIDE_A, "band_mode": "stop",
                  "rotation_deg": lab_rotation(4242)})
    cases.append({"profile": "Tempered6", "seed": 4242, "impact_root": LAB_HITS["H1"], "side": SIDE_A, "band_mode": "band",
                  "rotation_deg": lab_rotation(4242)})
    assert len(cases) == 20
    return cases


def lookdev_cases(profile="Annealed6"):
    """Plan sec 4.1 sign-off set: 3 impact spots (the lab's H1, H2, H3) x 2 seeds."""
    out = []
    for name in ("H1", "H2", "H3"):
        for seed in LOOKDEV_SEEDS:
            out.append({"name": "%s_%s_s%d" % (profile, name, seed), "hit": name, "profile": profile, "seed": seed,
                        "impact_root": LAB_HITS[name], "side": SIDE_A, "band_mode": "band", "rotation_deg": lab_rotation(seed)})
    return out


def case_name(i, c):
    return "g%02d_%s_s%d_x%+.4f_y%.4f_side%+d_%s.json" % (i, c["profile"], c["seed"], c["impact_root"][0],
                                                          c["impact_root"][1], c["side"], c["band_mode"])


def write_golden(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    index = []
    ok_all = True
    for i, c in enumerate(golden_cases()):
        res = generate(c["profile"], c["seed"], impact_root=c["impact_root"], side=c["side"], band_mode=c["band_mode"],
                       rotation_deg=c["rotation_deg"])
        v = validate(res)
        ok_all &= v["all_ok"]
        name = case_name(i, c)
        txt = to_json(res)
        with open(os.path.join(out_dir, name), "w") as f:
            f.write(txt)
        index.append({"file": name, "profile": c["profile"], "seed": c["seed"], "impact_root": list(c["impact_root"]),
                      "side": c["side"], "band_mode": c["band_mode"], "rotation_deg": res["rotation_deg"],
                      "pieces": res["stats"]["pieces_total"], "valid": v["all_ok"],
                      "sha256": hashlib.sha256(txt.encode("utf-8")).hexdigest()})
    with open(os.path.join(out_dir, "index.json"), "w") as f:
        json.dump({"version": VERSION, "note": "Golden output of crack_graph_ref.py. The C# port must match "
                   "every vertex within 0.1 mm (plan sec 5.2 step 3). sha256 is of the exact file bytes.",
                   "cases": index}, f, indent=1, sort_keys=True)
    return index, ok_all


def _cli(argv):
    import argparse
    ap = argparse.ArgumentParser(description="FrontRooms glass crack-graph reference generator")
    sub = ap.add_subparsers(dest="cmd")
    g = sub.add_parser("gen", help="one pattern -> JSON (+ validation summary)")
    g.add_argument("--profile", default="Annealed6", choices=sorted(PROFILES))
    g.add_argument("--seed", type=int, default=4242)
    g.add_argument("--impact-root", type=float, nargs=2, default=None, metavar=("X", "Y"))
    g.add_argument("--impact-uv", type=float, nargs=2, default=None, metavar=("U", "V"))
    g.add_argument("--rotation", type=float, default=None, help="degrees; default the map's MapHash.Unit(seed) * 360")
    g.add_argument("--side", type=int, default=1, choices=(-1, 1), help="GlassBreakRecord.side: +1 struck from cell a")
    g.add_argument("--band-mode", default="band", choices=("band", "stop"))
    g.add_argument("--out", default=None)
    gd = sub.add_parser("golden", help="write the 20 golden cases + index.json")
    gd.add_argument("--out-dir", default=os.path.join(os.path.dirname(os.path.abspath(__file__)), "golden"))
    ld = sub.add_parser("lookdev", help="write the 6 sign-off samples of a profile")
    ld.add_argument("--profile", default="Annealed6", choices=sorted(PROFILES))
    ld.add_argument("--out-dir", required=True)
    sub.add_parser("profiles", help="print the profile table")
    a = ap.parse_args(argv)
    if a.cmd == "gen":
        res = generate(a.profile, a.seed, impact_uv=a.impact_uv, impact_root=a.impact_root, rotation_deg=a.rotation,
                       side=a.side, band_mode=a.band_mode)
        txt = to_json(res)
        if a.out:
            with open(a.out, "w") as f:
                f.write(txt)
        v = validate(res)
        print(json.dumps({"stats": res["stats"], "validation": v}, indent=1, sort_keys=True))
        return 0 if v["all_ok"] else 1
    if a.cmd == "golden":
        index, ok_all = write_golden(a.out_dir)
        print("golden:", len(index), "cases,", "all valid" if ok_all else "SOME INVALID")
        return 0 if ok_all else 1
    if a.cmd == "lookdev":
        os.makedirs(a.out_dir, exist_ok=True)
        ok_all = True
        for c in lookdev_cases(a.profile):
            res = generate(c["profile"], c["seed"], impact_root=c["impact_root"], side=c["side"], band_mode=c["band_mode"],
                           rotation_deg=c["rotation_deg"])
            v = validate(res)
            ok_all &= v["all_ok"]
            with open(os.path.join(a.out_dir, c["name"] + ".json"), "w") as f:
                f.write(to_json(res))
            print(c["name"], res["stats"]["pieces"], "valid" if v["all_ok"] else "INVALID")
        return 0 if ok_all else 1
    if a.cmd == "profiles":
        print(json.dumps(PROFILES, indent=1, sort_keys=True, default=str))
        return 0
    ap.print_help()
    return 2


if __name__ == "__main__":
    sys.exit(_cli(sys.argv[1:]))
