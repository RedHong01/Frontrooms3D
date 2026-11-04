"""The Run! T2 re-stack (71_run_room.md s.5.3, decision 5; card 2, the T2 body): the end wall's block layout.

Pure Python (no bpy): the kit builder (run_restack_blocks.py), the bake (run_restack_bake.py) and the clone's checks read
the same table, so a piece's slot, its paint and its clip key can never disagree.

The bond is the corridor's own: Run_Block_A is a 0.4 x 0.2 m running bond (texture tile 0.4 x 0.4 m: bed joints at
v = 0 and 0.2 m, head joints at u = 0 in the lower row of the tile and at u = 0.2 m in the upper row). The end wall's two
faces carry it in OBJECT space, locked to the end (the *_Piece twins of RunB_BlockUpper / RunB_BlockLower, mesh UVs in
metres): front (toward the player, normal -Z) u = -x + U_FRONT, back u = x + U_BACK, v = y on both. So the at-rest skins
(Kit_RunRecede_Wall) and the pieces (Kit_RunRestack_Blocks) show the same joints on the same lines and the swap at the
tell is exact, wherever the map places the room; the bed joints sit on y = 0.2 k as the corridor's world-projected walls
do (a room's floor at y 0). Head joints: lower rows (y mod 0.4 in [0, 0.2)) at x = 0.1 + 0.4 k, upper rows at
x = 0.3 + 0.4 k: each bay is two blocks wide, a full pair in one row and half + full + half in the next.

Frame (Unity, the end's own space): origin on the floor at the axis on the end's line; x across, y up, z along the
corridor, the wall's front face toward -z (the player). The courses: c0 0.06-0.20 (cut, on the steel sill), c1-c11 0.2 m,
c12 2.40-2.53 (cut, under the girder's header). Courses 0-10 stand in the two bays beside the RN-F frame (|x| 0.551 to
1.295: the frame's face trim ends at 0.549, the edge channels' flanges begin at 1.295); courses 11 and 12 run the full
width over a 51 mm steel lintel (2.149-2.200) on the frame's head. 69 pieces; phones get 28 half-course strips.
"""

SKIN_IN = 1.295         # the edge channels' flanges begin here
JAMB = 0.551            # the bays begin 2 mm clear of the RN-F frame's face trim (0.549)
DOOR_HALF = 0.50        # the goal door's clear opening (the frame's jambs and trim stand in 0.50-0.549)
SILL_TOP = 0.06
GIRDER = 2.53           # the header (steel, with the services' slots) begins here
LINTEL = (2.149, 2.200)
TONE = 1.20             # the two-tone line (RunRigLayout.lowerToneTop), a bed joint
FACE = 0.080            # the upper tone's faces at z -0.08 / +0.08 (the head's skins)
LOWER_PROUD = 0.002     # the lower tone 2 mm proud, as the corridor's walls and the head's skins
COURSE = 0.20
TILE = 0.40
U_FRONT = 0.1           # front face u = -x + U_FRONT
U_BACK = 0.3            # back face u = x + U_BACK
LOWER_JOINT = 0.1       # lower rows: head joints at x = LOWER_JOINT + 0.4 k
UPPER_JOINT = 0.3       # upper rows: x = UPPER_JOINT + 0.4 k
COURSES = 13
HALF_LANE = (0.86, 1.29)   # |x| of the streams (the tray's strut ends at -0.81, the main at 0.60; the armature brackets begin at 1.30)

# Kit names (frozen once landed).
BLOCKS = "Kit_RunRestack_Blocks"
STRIPS = "Kit_RunRestack_Strips"
WALL = "Kit_RunRecede_Wall"
SLOT_UPPER = "RunB_BlockUpper_Piece"
SLOT_LOWER = "RunB_BlockLower_Piece"
SLOT_CORE = "RunB_BlockCore"
SLOT_MORTAR = "RunB_BlockMortar"
SLOTS = (SLOT_UPPER, SLOT_LOWER, SLOT_CORE, SLOT_MORTAR)


def course_y(c):
    """(y0, y1) of course c."""
    if c == 0:
        return SILL_TOP, COURSE
    if c == COURSES - 1:
        return COURSE * 12, GIRDER
    return COURSE * c, COURSE * (c + 1)


def lower_row(c):
    """True for the tile's lower row (y mod 0.4 in [0, 0.2)): even courses."""
    return c % 2 == 0


def joints(c):
    """The head joints' x positions of course c across the whole end."""
    j0 = LOWER_JOINT if lower_row(c) else UPPER_JOINT
    out = []
    x = j0 - TILE * 4
    while x < SKIN_IN + 1e-6:
        if x > -SKIN_IN + 1e-6:
            out.append(round(x, 6))
        x += TILE
    return out


def segments(c):
    """The spans of course c: the two bays beside the door (c0-c10), the full width over the lintel (c11, c12)."""
    if c <= 10:
        return [(-SKIN_IN, -JAMB), (JAMB, SKIN_IN)]
    return [(-SKIN_IN, SKIN_IN)]


def pieces():
    """The 69 pieces: dicts with name, index, course, side (-1 left, +1 right by the centre), k (0 at the wall, inward),
    x0, x1, y0, y1, lower (the lower tone: below 1.2 m), depth (the half depth: 0.082 lower tone, 0.080 upper)."""
    out = []
    for c in range(COURSES):
        y0, y1 = course_y(c)
        js = joints(c)
        for (a, b) in segments(c):
            cuts = [a] + [j for j in js if a + 1e-6 < j < b - 1e-6] + [b]
            for i in range(len(cuts) - 1):
                x0, x1 = cuts[i], cuts[i + 1]
                out.append({"course": c, "x0": round(x0, 6), "x1": round(x1, 6), "y0": round(y0, 6), "y1": round(y1, 6)})
    for p in out:
        cx = (p["x0"] + p["x1"]) / 2.0
        p["side"] = -1 if cx < 0 else 1
        p["lower"] = p["y1"] <= TONE + 1e-6
        p["depth"] = FACE + (LOWER_PROUD if p["lower"] else 0.0)
    # k: order from the wall inward on each side of each course
    for c in range(COURSES):
        for s in (-1, 1):
            row = [p for p in out if p["course"] == c and p["side"] == s]
            row.sort(key=lambda p: -abs((p["x0"] + p["x1"]) / 2.0))
            for k, p in enumerate(row):
                p["k"] = k
                p["kn"] = len(row)
    out.sort(key=lambda p: (p["course"], p["x0"]))
    for i, p in enumerate(out):
        p["index"] = i
        p["name"] = "p%03d" % i
    return out


def strips():
    """Phones (71 s.5.3: half-course strips, no grit): the bays' courses as one strip a side; over the lintel the two half
    courses with the centre block on its own (it must step aside before it can rise: 28 strips."""
    out = []
    ps = pieces()
    for c in range(COURSES):
        row = [p for p in ps if p["course"] == c]
        y0, y1 = course_y(c)
        groups = []
        if c <= 10:
            for s in (-1, 1):
                groups.append(([p for p in row if p["side"] == s], s, 0))
        else:
            # the block spanning the centre line (|centre| = 0.1) stands alone; the rest of each half is one strip
            centre = [p for p in row if abs((p["x0"] + p["x1"]) / 2.0) < 0.15]
            for s in (-1, 1):
                rest = [p for p in row if p["side"] == s and p not in centre]
                groups.append((rest, s, 0))
            for p in centre:
                groups.append(([p], p["side"], 1))
        for members, s, k in groups:
            if not members:
                continue
            x0 = min(p["x0"] for p in members)
            x1 = max(p["x1"] for p in members)
            out.append({"course": c, "x0": x0, "x1": x1, "y0": y0, "y1": y1, "side": s, "k": k,
                        "lower": y1 <= TONE + 1e-6, "depth": FACE + (LOWER_PROUD if y1 <= TONE + 1e-6 else 0.0),
                        "members": [p["index"] for p in members]})
    for c in range(COURSES):
        for s in (-1, 1):
            row = [p for p in out if p["course"] == c and p["side"] == s]
            for p in row:
                p["kn"] = len(row)
    out.sort(key=lambda p: (p["course"], p["x0"]))
    for i, p in enumerate(out):
        p["index"] = i
        p["name"] = "s%03d" % i
    return out


def face_uv(x, y, front):
    """The metre UV of a face point (end space): the corridor's bond on the end, locked to it."""
    return (-x + U_FRONT, y) if front else (x + U_BACK, y)


def wall_rects():
    """The at-rest skins' face rectangles = the union of the pieces' faces: the two bays to the lintel's top, the band
    over it to the girder."""
    return [(-SKIN_IN, -JAMB, SILL_TOP, LINTEL[1]), (JAMB, SKIN_IN, SILL_TOP, LINTEL[1]), (-SKIN_IN, SKIN_IN, LINTEL[1], GIRDER)]


def wall_skin_rects():
    """The at-rest skins (Kit_RunRecede_Wall): the pieces' faces, the bays run on under the RN-F frame's face trim to the
    opening (|x| 0.50; the trim covers 0.50-0.549), so no 2 mm slot shows between the trim and the block at rest."""
    return [(-SKIN_IN, -DOOR_HALF, SILL_TOP, LINTEL[1]), (DOOR_HALF, SKIN_IN, SILL_TOP, LINTEL[1]), (-SKIN_IN, SKIN_IN, LINTEL[1], GIRDER)]


if __name__ == "__main__":
    ps = pieces()
    print("pieces", len(ps), "strips", len(strips()))
    for c in range(COURSES):
        row = [p for p in ps if p["course"] == c]
        print(c, course_y(c), ["%s%+d k%d [%.3f %.3f]" % (p["name"], p["side"], p["k"], p["x0"], p["x1"]) for p in row])
