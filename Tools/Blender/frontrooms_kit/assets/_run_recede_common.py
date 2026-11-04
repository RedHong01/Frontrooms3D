"""Shared geometry for the Run! receding end (71_run_room.md s.5.1, card 2 'the recede, head first').

The receding end ("the head") is the corridor's end wall made a movable assembly: a block-faced wall on a steel frame,
hung from two trolleys in surface-mounted tracks in the soffit corners, guided at the floor in two 20 mm slots, held
at each seat by wall-mounted hold-open magnets, sealed by rubber sweeps (two poses) and brush strips where the
corridor's services pass through its header. Every part is 1977-1990 hardware or the corridor's own kit; only the
speed and the fact that a wall does it are impossible (motion.txt M7).

Authoring frame: everything below is given in UNITY rig space relative to the head's line (x across the corridor,
y up from the floor, z along the corridor, +z away from the player; the head's wall faces -z), converted to
Blender by U() / S() (Unity imports Blender (x, y, z) as (-x, z, -y), kitlib.export).

The envelope (why the numbers): the head travels through the hidden length past everything mounted there, so it
stays inside the corridor's clear section minus what is fixed: the bumper rail (50 mm proud, 0.825-0.975 m), the EMT
and handy boxes on the left wall (to 32 mm / 52 mm proud), the hold-open magnets (70 mm proud at 1.95 m). Its sides
are 85 mm off the wall faces (x +-1.335); the sweeps close the gap when seated. Over 2.53 m the corridor's services
pass through it: brush-lined slots for the strip lamp (x +-0.11 from 2.74), the sprinkler main and its couplings
(0.46..0.64 from 2.545), the ladder tray (-0.83..-0.37 from 2.655) and the two tracks (x +-1.18..1.32 from 2.80).
The load path: trolleys in the tracks (x +-1.25) -> corner hangers -> a girder inside the wall's top.
"""

import math

import bmesh
import bpy

import _run_b_common as B

# ------------------------------------------------------------------ the corridor (FrontRoomsRunRig: Corridor)
HW = 1.42           # wall faces at x +-1.42
SOFFIT = 2.90
WALL_HALF = 0.08    # the map's wall: 0.16 thick, centred on the cell line

# ------------------------------------------------------------------ the head
HALF = 1.335        # the steel edge channels' outer faces
SKIN_IN = 1.295     # the skins end where the edge channel's flange begins
FRONT = -0.08       # the front face (toward the player)
BACK = 0.08
SILL_TOP = 0.06
BOTTOM = 0.02       # 20 mm over the floor: the bottom sweep closes it
TONE = 1.20         # the two-tone line, as the corridor's walls (lowerToneTop)
GIRDER = 2.53       # the top of the girder inside the wall: the header panel above it carries the slots
TOP = 2.86          # the header's top edge; the top brush closes the last 40 mm to the soffit
DOOR_HALF = 0.50    # the goal door's clear opening (RunRigLayout goalHalf / goalTop)
DOOR_TOP = 2.10
# slots (x0, x1, bottom): the services the header lets through
SLOTS = (
    ("tray", -0.83, -0.37, 2.655),
    ("strip", -0.11, 0.11, 2.74),
    ("main", 0.46, 0.64, 2.545),
)
NOTCH = (1.18, 1.32, 2.80)  # the track notches at both top corners (|x| range, bottom)
TRACK_X = 1.25      # track centre lines (both soffit corners)
TRACK_BOTTOM = 2.835
SLOT_X = 0.95       # the two floor guide slots (x +-0.95)
MAGNET_Y = 1.95     # the hold-open magnets' centre height
MAGNET_Z = -0.125   # their centre ahead of the line (in front of the head's front face)
HOLDER_FACE = 1.35  # the holder's magnet face (70 mm proud of the wall)
ARM_Z = -1.68       # the carried lamp unit's mount plate face (the rig's carried unit back), 1.6 m before the wall face
ARM_Y = (2.60, 2.66)
PLATE_Y = (2.33, 2.68)
C42 = (0.0, 2.20, 0.53)  # the goal window light in its box behind the head (FrontRoomsRunRig.BuildGoalLight)

# Head budgets (LOD0 / LOD1 / LOD2) and switch distances (m at vFOV 76): the head is the run's goal, seen whole from 27 m.
LOD_DISTANCES = (10.0, 28.0, 0.0)


# Slots the recede kits add to kitlib (preview colours only; the look is Resources/Surfaces/<slot>.mat, the corridor's own).
for _slot, (_c, _r) in {
    "RunB_BlockUpper": ((0.70, 0.69, 0.62), 0.8),    # #DAD7CB, world-projected 0.4 m block
    "RunB_BlockLower": ((0.27, 0.30, 0.26), 0.8),    # #8E958B
    "RunB_Cove": ((0.03, 0.03, 0.03), 0.6),
}.items():
    B.kitlib.register_slot(_slot, _c, _r)


def U(x, y, z):
    """Unity rig space -> Blender."""
    return (-x, -z, y)


def S(sx, sy, sz):
    """A Unity-axis box size -> Blender (x, y, z) size."""
    return (sx, sz, sy)


def rbox(kit, x0, x1, y0, y1, z0, z1, slot, bevel=0.0, segs=1, name="box"):
    """An axis box from Unity ranges, with an explicit bevel (0: none)."""
    x0, x1 = sorted((x0, x1))
    y0, y1 = sorted((y0, y1))
    z0, z1 = sorted((z0, z1))
    return kit.box(S(x1 - x0, y1 - y0, z1 - z0), U((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2), slot,
                   bevel=bevel, segments=segs, name=name)


_ROT = {"x": (0, 90, 0), "y": (0, 0, 0), "z": (90, 0, 0)}


def rcyl(kit, axis, r, length, centre, slot, verts, bevel=0.0, segs=1, name="cyl", radius_top=None):
    """A cylinder along a Unity axis ("x", "y" vertical, "z" along the corridor) with explicit sides and bevel."""
    return kit.cylinder(r, length, U(*centre), slot, verts=verts, rot=_ROT[axis], bevel=bevel, segments=segs,
                        name=name, radius_top=radius_top)


def hero(lv, w=0.0012):
    """(bevel, segments) for a visible edge: hero LOD0 >= 0.8 mm x 2, LOD1 the same x 1, LOD2 none."""
    if lv == 0:
        return max(w, 0.0008), 2
    if lv == 1:
        return max(w, 0.0008), 1
    return 0.0, 1


def header_spans():
    """The header panel over the girder as (x0, x1, top) spans: full height except in the slots and the notches."""
    cuts = [(-SKIN_IN, -NOTCH[0], NOTCH[2])]
    for _n, a, b, bottom in SLOTS:
        cuts.append((a, b, bottom))
    cuts.append((NOTCH[0], SKIN_IN, NOTCH[2]))
    cuts.sort()
    out = []
    at = -SKIN_IN
    for a, b, top in cuts:
        if a > at + 1e-6:
            out.append((at, a, TOP))
        out.append((max(a, at), b, top))
        at = b
    if at < SKIN_IN - 1e-6:
        out.append((at, SKIN_IN, TOP))
    return out


def tufts(kit, name, roots, direction, lengths, slot, width=0.0036, thick=0.0012, taper=0.45, lean=None):
    """A strip brush's filament tufts as ONE mesh: a tapered blade per root (Unity points), each pointing along the
    Unity unit vector `direction` for its length; `lean` (optional, per tuft, metres) bends the tip across the blade
    (the brush's lay). 4 faces, 8 tris a tuft, no caps (the root sits in the holder, the tip is 45 % of the root)."""
    verts, faces, uvs = [], [], []
    dx, dy, dz = direction
    # the blade's width runs across the corridor plane perpendicular to the direction, its thickness along z
    if abs(dz) > 0.9:
        wv = (1.0, 0.0, 0.0)
    elif abs(dx) > 0.9:
        wv = (0.0, 1.0, 0.0)
    else:
        wv = (1.0, 0.0, 0.0)
    tv = (dy * wv[2] - dz * wv[1], dz * wv[0] - dx * wv[2], dx * wv[1] - dy * wv[0])
    for i, (p, L) in enumerate(zip(roots, lengths)):
        if L <= 0.002:
            continue
        lw = lean[i] if lean else 0.0
        tip = (p[0] + dx * L + wv[0] * lw, p[1] + dy * L + wv[1] * lw, p[2] + dz * L + wv[2] * lw)
        hw, ht = width / 2.0, thick / 2.0
        tw, tt = hw * taper, ht * taper
        quad = []
        for (c, a, b) in ((p, hw, ht), (tip, tw, tt)):
            for (sa, sb) in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
                quad.append((c[0] + wv[0] * a * sa + tv[0] * b * sb, c[1] + wv[1] * a * sa + tv[1] * b * sb,
                             c[2] + wv[2] * a * sa + tv[2] * b * sb))
        base = len(verts)
        verts += [U(*q) for q in quad]
        for k in range(4):
            a0, a1 = base + k, base + (k + 1) % 4
            b0, b1 = base + 4 + k, base + 4 + (k + 1) % 4
            faces.append((a0, a1, b1, b0))
            uvs += [(0, 0), (1, 0), (1, 1), (0, 1)]
    if not faces:
        return None
    return B.mesh_part(kit, name, verts, faces, uvs, slot)


def jitter(i, seed=0):
    """A stable pseudo-random value in [-1, 1] (no RNG state: every rebuild is byte-stable)."""
    h = (i * 2654435761 + seed * 40503) & 0xFFFFFFFF
    h ^= h >> 15
    h = (h * 2246822519) & 0xFFFFFFFF
    h ^= h >> 13
    return (h & 0xFFFF) / 32767.5 - 1.0
