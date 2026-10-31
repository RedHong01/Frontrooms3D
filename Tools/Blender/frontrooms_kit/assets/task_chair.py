"""1990 open-plan task chair, c. 1985-93: armless, grey fabric (W2, 2026-10-21).

Red, 2026-10-21: "the task chair to armless grey/coloured fabric (keep the
black armed one as a variant)". Spec: Documentation/research/office_and_film/
w2_props/10_spec.md sec. 4. The old black armed chair is now
Kit_TaskChair_Armed (task_chair_armed.py).

After the Sears 1993 Home Catalog p.359 chairs M "desk chair" and N "task
chair" (pneumatic seat height 17-22 in, contoured swivel seat, polyester
cover; N adds manual back and angle adjustment), both armless in grey fabric
on a black 5-star base with twin casters; p.358 B "gray nylon upholstery".

Construction, bottom to top:
* 5-star base, black nylon: tapered legs sloping down from a round hub
  (24 sides) to the tips, 0.64 m across the casters.
* 5 twin-wheel casters (16-sided wheels) parked at mixed swivel angles.
* Gas lift: a smooth black tube housing dia. 0.07 (no bellows: bellows go
  with the armed chair) and the exposed chrome piston dia. 0.05, ~0.02 m of
  it showing under the seat (24 sides each). Fix 2 (2026-10-22): was 0.06 m;
  a long chrome rod mirrors the dim brown room and read rusty at 0.3 m.
  Fix 3: in the seat's shadow the short piston reads dark at 0.3 m (it
  mirrors the seat's underside and the floor under it); a limit, not chrome.
* Mechanism: black housing and column socket under the seat shell; a long
  height lever out to the user's right (-X), angled down and forward, chrome
  rod (S359 photo) with a black paddle grip; a back-angle knob under the rear
  at the bar root. No tilt lever, no front tension knob (neither on M/N).
* Seat: plain contoured pad (no welt) 0.48 W x 0.46 D, top at 0.45 m, with a
  dish, a waterfall front and a slightly narrower back, on a black shell.
* Back: shield/oval pad 0.44 W x 0.42 H, top at 0.93 m, curved in plan with
  a lumbar swell, in a black plastic shell; carried on a black flat-steel bar
  that runs under the seat to the mechanism and shows in the ~0.06 m gap
  between seat and back; back-height knob and bracket on its rear.
* No arms.

Front (seat edge) faces -Y. The user's right hand is on -X.

Slots (3, all existing): Prop_FabricCubicle (both pads: slate-grey poly-wool,
the only mid-grey fabric; FabricCharcoal reads near black), Prop_PlasticBlack
(base, casters, shells, housing, bar, paddle, knobs: painted steel is
dielectric and shares the plastic's slot), Prop_Chrome (piston, lever rod,
spine screws). VARIANTS: Kit_TaskChair_Navy (Prop_FabricNavy, Sears "775
Royal navy"). Red and hot pink need new materials: deferred.

Budget (spec sec. 4.4): LOD0 <= 5,200, LOD1 <= 1,800 (no more than the old
chair's 1,792: chairs are the Office's biggest triangle item; fix 3 keeps it
at about fix 2's 1,620 by lowering LODS[0] to 0.31), LOD2 <= 450;
lodDistances [6, 12, 30]. LOD1 is LOD0 collapse-decimated to LODS[0]
(kitlib); LOD2 is hand-built (fix 2, 2026-10-22): a decimated 10 % chair
broke into spikes and loose caster specks at the 12 m switch, so LOD2 is a
proxy of whole low-poly parts (star legs and hub, a box per caster, the
column, the mechanism, seat and back pads and shells, the back bar) marked
fr_lods "2"; levers, knobs and screws are left out (< 1 px at 12 m). Hero refinements for ~1 px of facet at 0.3 m:
8 cushion corner steps, 24-sided hub / column / piston, 16-sided wheels.
Fix 3 (2026-10-22): verify round 3 saw facets on the back shell's bottom
corners at 0.3 m, so the corners in view get more steps: the back shell and
back pad 15 at the bottom corners (6 deg a step, 0.23 mm sagitta at
r 0.167), the seat pad 12 and the seat shell 10 at the front corners; the
top / rear corners keep 8 (seat shell 5). _rrect takes one step count per
corner, so every ring of a pad keeps the same point count.
The upholstery pads are bmesh lofts (kitlib has no primitive for them) handed
to the kit with kit._new_object / kit._place / kit._bevel.
"""

import math

import bmesh
from mathutils import Vector

NAME = "Kit_TaskChair"
LODS = (0.31, 0.09)       # fix 3: 0.31 of the 5.17k LOD0 = fix 2's ~1,620 (0.35 of 4,634)
SMOOTH_ANGLE = 50.0
VARIANTS = {"Kit_TaskChair_Navy": {"Prop_FabricCubicle": "Prop_FabricNavy"}}

FABRIC = "Prop_FabricCubicle"
BLACK = "Prop_PlasticBlack"
STEEL = BLACK          # black-painted pressed steel (dielectric, shares the plastic slot)
CHROME = "Prop_Chrome"


# --------------------------------------------------------------------- helpers
def _rrect(hw, hd, r, nc, nx, ny):
    """Rounded rectangle outline (CCW, fixed point count for any size).
    ``nc`` is the corner step count, or one per corner in the order
    (+x +y, -x +y, -x -y, +x -y) (fix 3)."""
    r = max(min(r, hw * 0.98, hd * 0.98), 0.0015)
    corners = [(hw - r, hd - r, 0.0), (-hw + r, hd - r, 90.0), (-hw + r, -hd + r, 180.0), (hw - r, -hd + r, 270.0)]
    steps = list(nc) if isinstance(nc, (list, tuple)) else [nc] * 4
    pts = []
    for k, (cx, cy, a0) in enumerate(corners):
        nck = steps[k]
        for i in range(nck + 1):
            a = math.radians(a0 + 90.0 * i / nck)
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
        a_end = math.radians(a0 + 90.0)
        p0 = (cx + r * math.cos(a_end), cy + r * math.sin(a_end))
        nxt = corners[(k + 1) % 4]
        a_nxt = math.radians(nxt[2])
        p1 = (nxt[0] + r * math.cos(a_nxt), nxt[1] + r * math.sin(a_nxt))
        n = nx if k % 2 == 0 else ny
        for i in range(1, n):
            t = i / n
            pts.append((p0[0] + (p1[0] - p0[0]) * t, p0[1] + (p1[1] - p0[1]) * t))
    return pts


def _soft_block(kit, name, slot, hw, hd, r, profile, loc, rot=(0, 0, 0), warp=None, nc=3, nx=3, ny=3,
                cap_bottom=True, cap_top=True):
    """Upholstery pad: rounded-rect rings lofted through ``profile`` [(inset,
    z), ...] (bottom to top), capped with fans so a warp can dish or bend the
    faces. ``warp(x, y, z)`` reshapes every vertex in local space. A cap
    buried in a shell is skipped."""
    bm = bmesh.new()

    def vert(x, y, z):
        return bm.verts.new(warp(x, y, z) if warp else (x, y, z))

    rings = []
    for inset, z in profile:
        rr = max(r - inset, r * 0.35)
        rings.append([vert(x, y, z) for x, y in _rrect(hw - inset, hd - inset, rr, nc, nx, ny)])
    n = len(rings[0])
    for a, b in zip(rings, rings[1:]):
        for i in range(n):
            j = (i + 1) % n
            bm.faces.new((a[i], a[j], b[j], b[i]))
    caps = ([(rings[0], profile[0][1])] if cap_bottom else []) + ([(rings[-1], profile[-1][1])] if cap_top else [])
    for ring, z in caps:
        c = vert(0.0, 0.0, z)
        for i in range(n):
            bm.faces.new((ring[i], ring[(i + 1) % n], c))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = kit._new_object(name, bm, slot, "metres", "xz")
    kit._place(obj, loc, rot)
    return obj


def _strip_outline(points, half):
    """Closed 2D outline of a flat bar of half-thickness ``half`` along a
    polyline (mitred joints)."""
    pts = [Vector(p) for p in points]
    left, right = [], []
    for k, p in enumerate(pts):
        if k == 0:
            d = (pts[1] - pts[0]).normalized()
        elif k == len(pts) - 1:
            d = (pts[-1] - pts[-2]).normalized()
        else:
            d = ((pts[k] - pts[k - 1]).normalized() + (pts[k + 1] - pts[k]).normalized()).normalized()
        nrm = Vector((-d.y, d.x))
        left.append(tuple(p + nrm * half))
        right.append(tuple(p - nrm * half))
    return left + list(reversed(right))


def _fillet_path(a, corner, b, radius, steps=8):
    """Points from a to b passing round ``corner`` with an arc of ``radius``."""
    a, c, b = Vector(a), Vector(corner), Vector(b)
    d1 = (a - c).normalized()
    d2 = (b - c).normalized()
    ang = math.acos(max(-1.0, min(1.0, d1.dot(d2))))
    dist = radius / math.tan(ang / 2)
    centre = c + (d1 + d2).normalized() * (radius / math.sin(ang / 2))
    t1, t2 = c + d1 * dist, c + d2 * dist
    a1 = math.atan2(t1.y - centre.y, t1.x - centre.x)
    a2 = math.atan2(t2.y - centre.y, t2.x - centre.x)
    da = a2 - a1
    while da > math.pi:
        da -= 2 * math.pi
    while da < -math.pi:
        da += 2 * math.pi
    pts = [tuple(a)]
    for k in range(steps + 1):
        ak = a1 + da * k / steps
        pts.append((centre.x + radius * math.cos(ak), centre.y + radius * math.sin(ak)))
    pts.append(tuple(b))
    return pts


def _lod2(obj):
    """A part of the hand-built LOD2 only (kitlib P-1b)."""
    obj["fr_lods"] = "2"
    return obj


def _lod1_by_decimation(kit, ratio):
    """kitlib decimates only when no level is hand-built: with the LOD2 proxy
    in place, make LOD1 here (LOD0 collapse-decimated to ``ratio``, parts
    marked fr_lod1_drop removed first), then let make_lods name and count."""
    make_lods = kit.make_lods

    def make_lods_with_lod1(ratios=None):
        if 1 not in kit.lods:
            kit.lods[1] = kit._decimated(ratio, 1)
        make_lods(ratios)
    kit.make_lods = make_lods_with_lod1


def _drop(obj, lod1=False, lod2=True):
    if lod1:
        obj["fr_lod1_drop"] = True
    if lod2:
        obj["fr_lod2_drop"] = True
    return obj


# ----------------------------------------------------------------------- build
def build(kit):
    # ---------------------------------------------------------------- base
    # Hub (24 sides): top buried in the column housing.
    kit.lathe([(0.040, 0.086), (0.049, 0.097), (0.049, 0.148), (0.042, 0.159), (0.036, 0.163)], (0, 0, 0), BLACK,
              verts=24, close_top=False, name="base hub")
    tip_r = 0.318
    leg_in = 0.028
    length = tip_r - leg_in
    rc = (tip_r + leg_in) / 2
    tip_cz, hub_cz = 0.088, 0.126
    caster_yaw = [25.0, 150.0, 80.0, 300.0, 205.0]
    body = [(-0.012, 0.066), (0.006, 0.068), (0.026, 0.054), (0.036, 0.032), (0.030, 0.014),
            (0.016, 0.010), (-0.006, 0.030), (-0.014, 0.054)]
    for k in range(5):
        th = 36.0 + 72.0 * k
        s, c = math.sin(math.radians(th)), math.cos(math.radians(th))
        kit.loft_box((0.040, 0.030), (0.054, 0.050), length, (s * rc, -c * rc, tip_cz), BLACK,
                     back_offset=(0, hub_cz - tip_cz), bevel=0.012, segments=1, rot=(0, 0, th), name="base leg")
        # Caster: one stem into the toe, the hood, twin 16-sided wheels.
        sx, sy = s * 0.303, -c * 0.303
        _drop(kit.cylinder(0.012, 0.018, (sx, sy, 0.066), BLACK, verts=8, bevel=0.0, name="caster stem"))
        yaw = caster_yaw[k]
        ty, tx = math.cos(math.radians(yaw)), -math.sin(math.radians(yaw))
        ax, ay = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
        hood = kit.extrude(body, 0.018, (sx, sy, 0), BLACK, plane="yz", rot=(0, 0, yaw), bevel=0.0, name="caster hood")
        kit._bevel(hood, 0.004, 1, angle=50.0)     # round the side faces' rims, not the outline's 45-deg steps
        axle = (sx + tx * 0.022, sy + ty * 0.022)
        h = 0.0085
        for side in (-1, 1):
            wx, wy = axle[0] + ax * 0.0185 * side, axle[1] + ay * 0.0185 * side
            # Rounded tyre edge on the outer side only (the inner face meets the hood).
            prof = ([(0.025, -h), (0.025, h - 0.0035), (0.0215, h)] if side > 0 else
                    [(0.0215, -h), (0.025, -h + 0.0035), (0.025, h)])
            kit.lathe(prof, (wx, wy, 0.0252), BLACK, verts=16, rot=(0, 90, yaw), name="caster wheel")

    # ------------------------------------------------------- gas lift column
    # Smooth black tube housing (rolled top lip) and the exposed chrome piston.
    # Fix 2: ~0.02 m of piston shows (was 0.06 m).
    kit.lathe([(0.0350, 0.150), (0.0350, 0.2855), (0.0338, 0.2905), (0.0262, 0.2925)], (0, 0, 0), BLACK, verts=24,
              close_bottom=False, close_top=False, name="column housing")
    kit.lathe([(0.0250, 0.286), (0.0250, 0.318)], (0, 0, 0), CHROME, verts=24, close_bottom=False,
              close_top=False, name="gas piston")

    # ------------------------------------------------------- seat mechanism
    seat_y = -0.03
    kit.box((0.15, 0.22, 0.040), (0, 0.0, 0.342), STEEL, bevel=0.008, segments=1, name="mechanism housing")
    kit.cylinder(0.030, 0.012, (0, 0.0, 0.318), STEEL, verts=24, bevel=0.0, name="column socket")
    # Height lever: chrome rod out of the housing's right side (-X), down and
    # forward past the seat edge, black paddle grip on its end.
    _drop(kit.tube([(-0.068, -0.020, 0.334), (-0.150, -0.072, 0.313), (-0.232, -0.135, 0.288)], 0.0045, CHROME,
                   verts=8, name="height lever rod"))
    _drop(kit.tube([(-0.222, -0.127, 0.291), (-0.246, -0.146, 0.2835), (-0.270, -0.164, 0.277)], 0.0105, BLACK,
                   verts=10, name="height paddle"))
    # Back-angle knob under the rear of the housing, at the bar root.
    _drop(kit.cylinder(0.011, 0.026, (0, 0.092, 0.312), STEEL, verts=8, bevel=0.0, name="knob boss"))
    lobes = [((0.030 if i % 2 == 0 else 0.024) * math.cos(math.radians(i * 36)),
              (0.030 if i % 2 == 0 else 0.024) * math.sin(math.radians(i * 36))) for i in range(10)]
    knob = kit.extrude(lobes, 0.022, (0, 0.092, 0.290), BLACK, plane="xy", bevel=0.0, name="back angle knob")
    kit._bevel(knob, 0.0035, 1, angle=50.0)
    _drop(knob)

    # ---------------------------------------------------------------- seat
    shell_z = 0.362
    cush_z = 0.374
    cush_h = 0.076
    seat_hw, seat_hd = 0.240, 0.230
    seat_r = 0.110

    def taper(x, y, z):
        return (x * (1.0 - 0.05 * max(0.0, y) / seat_hd), y, z)

    # Shell: both caps (the top one closes the gap round the cushion's foot).
    _soft_block(kit, "seat shell", BLACK, 0.236, 0.226, seat_r - 0.006,
                [(0.012, 0.0), (0.003, 0.004), (0.0, 0.011), (0.0, 0.017)],
                (0, seat_y, shell_z), warp=taper, nc=(5, 5, 10, 10), nx=2, ny=2)

    def seat_warp(x, y, z):
        f = max(0.0, min(1.0, z / cush_h))
        # back of the seat a touch narrower than the front
        x *= 1.0 - 0.05 * max(0.0, y) / seat_hd
        # dish where you sit, waterfall roll at the front edge
        dish = 0.007 * math.exp(-(x * x) / 0.018 - ((y - 0.03) ** 2) / 0.02)
        roll = 0.020 * max(0.0, (-y - 0.13) / 0.10) ** 2
        return (x, y, z - (dish + roll) * f)

    # Plain pad (M/N show no welt): boxing (its foot starts inside the shell
    # rim), a soft crown roll and the crowned top.
    seat_prof = [(0.006, 0.003), (0.0, 0.030), (0.0025, 0.050), (0.009, 0.0625), (0.021, 0.0700), (0.045, 0.0745),
                 (0.095, cush_h)]
    _soft_block(kit, "seat cushion", FABRIC, seat_hw, seat_hd, seat_r, seat_prof, (0, seat_y, cush_z),
                warp=seat_warp, nc=(8, 8, 12, 12), nx=3, ny=3, cap_bottom=False)

    # ------------------------------------------------------------- backrest
    alpha = 10.0
    sa, ca = math.sin(math.radians(alpha)), math.cos(math.radians(alpha))
    back_hw, back_hd = 0.220, 0.210
    back_r = 0.160            # shield / oval outline
    back_t = 0.058
    top_z = 0.93
    by = 0.292
    bz = top_z - back_hd * ca - 0.5 * back_t * sa
    bend = 0.62

    def to_world(u, v, w):
        """Back-local (x, up, out-front) to world."""
        return (u, by + v * sa - w * ca, bz + v * ca + w * sa)

    def widen(x, y):
        """Shield: a little wider at the top than at the bottom."""
        return x * (1.0 + 0.06 * y / back_hd)

    def cushion_warp(x, y, z):
        f = max(0.0, min(1.0, z / back_t))
        lumbar = 0.014 * math.exp(-((y + 0.08) / 0.09) ** 2)
        return (widen(x, y), y, z + bend * x * x + lumbar * f)

    def shell_warp(x, y, z):
        return (widen(x, y), y, z + bend * x * x)

    rot_back = (90.0 - alpha, 0, 0)
    back_prof = [(0.004, 0.002), (0.0, 0.022), (0.0025, 0.0355), (0.009, 0.0455), (0.021, 0.0520), (0.048, 0.0562),
                 (0.095, 0.0577), (0.145, back_t)]
    _soft_block(kit, "back cushion", FABRIC, back_hw, back_hd, back_r, back_prof, (0, by, bz), rot=rot_back,
                warp=cushion_warp, nc=(8, 8, 15, 15), nx=3, ny=3, cap_bottom=False)
    shell_prof = [(0.000, 0.004), (0.000, -0.009), (0.008, -0.024), (0.030, -0.033), (0.110, -0.037)]
    _soft_block(kit, "back shell", BLACK, back_hw + 0.007, back_hd + 0.007, back_r + 0.007, shell_prof, (0, by, bz),
                rot=rot_back, warp=shell_warp, nc=(8, 8, 15, 15), nx=3, ny=3)

    # Bracket on the back shell, the bar, the back-height knob.
    spine_w = -0.037 - 0.008
    _drop(kit.box((0.10, 0.20, 0.012), to_world(0, -0.11, -0.040), BLACK, bevel=0.005, segments=1, rot=rot_back,
                  name="back bracket"))
    top = to_world(0, -0.03, spine_w)
    low = to_world(0, -0.21, spine_w)
    # Bar: horizontal under the seat, filleted up into the back line.
    d = Vector((top[1] - low[1], top[2] - low[2])).normalized()
    k = (low[2] - 0.340) / d.y        # corner where the back line meets z = 0.340
    corner = (low[1] - d.x * k, 0.340)
    path = _fillet_path((0.03, 0.340), corner, (top[1], top[2]), 0.11, steps=4)
    kit.extrude(_strip_outline(path, 0.006), 0.05, (0, 0, 0), STEEL, plane="yz", bevel=0.0035, segments=1,
                name="back bar")
    _drop(kit.cylinder(0.024, 0.020, to_world(0, -0.11, spine_w - 0.016), BLACK, verts=12, rot=rot_back,
                       bevel=0.005, segments=1, name="back height knob"))
    # Four domed chrome screws holding the bar to the bracket.
    for v in (-0.17, -0.05):
        for u in (-0.016, 0.016):
            _drop(kit.lathe([(0.0052, 0.0), (0.0044, 0.0021), (0.0, 0.0030)], to_world(u, v, spine_w - 0.006), CHROME,
                            verts=6, rot=(rot_back[0] + 180.0, 0, 0), close_bottom=False, name="spine screw"), lod1=True)

    # ------------------------------------------------------------ metadata
    seat_top = cush_z + cush_h
    kit.support("seat", (0, seat_y, seat_top), (0.40, 0.38))
    kit.anchor("sit", (0, seat_y, seat_top))
    kit.anchor("back_top", (0, by + back_hd * sa, top_z))
    # One box = the real bounds (kitlib's default when no box is given):
    # chairs are static, not pushable (§5.3), and the sidecar footprint keeps
    # the backrest out of cubicle panels and desks.
    kit.meta["lodDistances"] = [6.0, 12.0, 30.0]
    kit.tag("office", "seat")
    kit.pile("Seat", mass=1, palette="office90s", states=["Upright", "Inverted", "Side", "Back"])

    # ------------------------------------------------- LOD2 proxy (fix 2)
    # Whole low-poly parts, no bevels, the same outlines as LOD0 (~420 tris; the
    # shield back keeps 3 corner steps so it stays an oval, not an octagon).
    _lod2(kit.cylinder(0.049, 0.077, (0, 0, 0.1245), BLACK, verts=6, bevel=0.0, name="base hub lod2"))
    for k in range(5):
        th = 36.0 + 72.0 * k
        s, c = math.sin(math.radians(th)), math.cos(math.radians(th))
        _lod2(kit.loft_box((0.040, 0.030), (0.054, 0.050), length, (s * rc, -c * rc, tip_cz), BLACK,
                           back_offset=(0, hub_cz - tip_cz), bevel=0.0, rot=(0, 0, th), name="base leg lod2"))
        sx, sy = s * 0.303, -c * 0.303
        yaw = caster_yaw[k]
        ty, tx = math.cos(math.radians(yaw)), -math.sin(math.radians(yaw))
        _lod2(kit.box((0.052, 0.058, 0.058), (sx + tx * 0.012, sy + ty * 0.012, 0.029), BLACK, bevel=0.0,
                      rot=(0, 0, yaw), name="caster lod2"))
    _lod2(kit.lathe([(0.0350, 0.150), (0.0350, 0.2925)], (0, 0, 0), BLACK, verts=6, close_bottom=False,
                    close_top=False, name="column housing lod2"))
    _lod2(kit.lathe([(0.0250, 0.2905), (0.0250, 0.318)], (0, 0, 0), CHROME, verts=6, close_bottom=False,
                    close_top=False, name="gas piston lod2"))
    _lod2(kit.box((0.15, 0.22, 0.040), (0, 0.0, 0.342), STEEL, bevel=0.0, name="mechanism housing lod2"))
    _lod2(_soft_block(kit, "seat shell lod2", BLACK, 0.236, 0.226, seat_r - 0.006, [(0.012, 0.0), (0.0, 0.017)],
                      (0, seat_y, shell_z), warp=taper, nc=1, nx=1, ny=1, cap_top=False))
    _lod2(_soft_block(kit, "seat cushion lod2", FABRIC, seat_hw, seat_hd, seat_r,
                      [(0.006, 0.003), (0.0, 0.045), (0.060, cush_h)], (0, seat_y, cush_z), warp=seat_warp,
                      nc=1, nx=1, ny=1, cap_bottom=False))
    _lod2(_soft_block(kit, "back cushion lod2", FABRIC, back_hw, back_hd, back_r,
                      [(0.004, 0.002), (0.0, 0.030), (0.060, back_t)], (0, by, bz), rot=rot_back,
                      warp=cushion_warp, nc=3, nx=1, ny=1, cap_bottom=False))
    _lod2(_soft_block(kit, "back shell lod2", BLACK, back_hw + 0.007, back_hd + 0.007, back_r + 0.007,
                      [(0.0, 0.004), (0.020, -0.030), (0.110, -0.037)], (0, by, bz), rot=rot_back,
                      warp=shell_warp, nc=3, nx=1, ny=1, cap_bottom=False))
    _lod2(kit.extrude(_strip_outline([(0.03, 0.340), corner, (top[1], top[2])], 0.006), 0.05, (0, 0, 0), STEEL,
                      plane="yz", bevel=0.0, name="back bar lod2"))
    _lod1_by_decimation(kit, LODS[0])
