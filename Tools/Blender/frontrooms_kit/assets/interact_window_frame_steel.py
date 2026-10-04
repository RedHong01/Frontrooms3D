"""Office borrowed-light frame, W-OF (interactables 10_spec.md §5.1-§5.5,
06_period_windows.md §3.2): a 4-sided pressed-steel hollow-metal frame for
a fixed interior light, the office fit-out's steel, c. 1975-90 and still
standard in 1990. Dark-bronze paint, the same slot as the locked door's
frame (Prop_SteelBrown), so a player reads "steel = the office's rooms".

Real-world reference: SDI 111A standard profiles (16 ga, 5/8" stop, square
returns, no moulding); University of Houston master spec 08 11 13: fixed
stop on the secure side, loose stop on the other, "countersunk flat or oval
head machine screws spaced uniformly not more than 9 inches o.c. and not
more than 2 inches o.c. from each corner", mitred hairline corners (06 §2.2
[read]). Generic: no maker's label, no fire label (it is a non-rated light).

Construction (window root W, Unity metres; see interact_window_common):
* ONE pressed-steel section swept round all four sides, sleeve mode: face
  band X 0.6995 -> 0.775 at Z +/-0.105 on both faces, square return back to
  the wall, soffit at |X| 0.6995 / Y 1.9995 / Y 0.3505. 1.5 mm inside bend
  radii: convex arrises show R3.0 (1.5 + 1.5 mm sheet), concave R1.5. Four
  pieces meeting on welded-and-ground mitres, each shown as a closed 0.3 x
  0.3 mm V hairline (a through-gap would expose the map trims' corners).
* Face B (hall): the integral formed stop, 16 x 16, Z -0.022 -> -0.006.
* Face A (room): the loose channel stop, 16 x 16, Z 0.006 -> 0.022, with a
  formed 8.8 mm x 1.6 mm screw channel on its sight-line face and 30 slotted
  oval-head screws in it (Ø 7 mm, 1.5 mm dome, 1.1 mm slot; 8 per jamb at
  Y 0.4175 ... 1.9325, 7 on head and sill at X -0.6325 ... 0.6325; 216 /
  211 mm centres). The channel is what
  keeps the 1.5 mm domes behind the stop line (self-check a) while the
  screws stay on the face a real loose stop is screwed through.
* Black glazing tape (Prop_Rubber), Z +/-(0.0035 -> 0.006), flush with the
  stop line: the dark line at the sight line, and the part that keeps the
  glass edge hidden at 60 degrees. Two neoprene setting blocks at X +/-0.35.
* No glass, no pane, no teeth: the glass-destruction track owns them; this
  asset is the pocket they fit (meta glassSlab, glassInterface, anchors).

Origin = window root (opening centre, wall centre line, floor). Front = face
A = kit -Y = Unity +Z (the map turns it toward the non-tall cell). Size
1.55 x 1.80 (Y 0.275-2.075) x 0.21 m. The sill band is the same 0.0755 face
as the head, so its bottom is 0.3505 - 0.0755 = 0.275 (the §5.2 table's
0.2745 is 0.5 mm deeper than the swept section; not a glass number).
The face-B stop's root bends (1.5 mm inside radius where the stop meets the
soffit) reach |Z| 0.0224 next to the soffit, 0.4 mm past the 0.022 stop
face: a pressed stop has that radius; it is clear of the glass and the clear
zone and is not an interface number.

Budget (§9.4): 3,600 / 1,300 / 220 tris. Built: 4,054 / 1,458 (+12.6 % /
+12.2 %). Pass 4 gave the jambs wear stations (Y ~0.42 / 0.88 / 1.42: shin
scuffs, the grab band) and paid for them by drawing the R3.0 arrises with 3
segments instead of 4 (§1.8 asks for 2-3). LOD1 0.36 (the §9.4 value): the
screws and setting blocks drop, then the collapse takes only the wear-station
loops, which lie on the straight sweep and cost zero error (LOD1 surface
within 0.001 mm of LOD0, measured); the tape stays, so the pocket never reads
as a slot. LOD2 drops the screws, tape and blocks (fr_lod2_drop).
LOD distances 4 / 12 / none. Sidecar placement "Wall" (wall_placement()).
Slots: Prop_SteelBrown (first =
submesh 0 for the RT bridge, §7.3), Prop_Rubber. VARIANT
Kit_WindowFrame_Steel_Enamel (W-RN, Run): Prop_SteelBrown -> Door_Enamel
(fallback Painted_Metal in Unity). Render-only: no collider.

Era: hollow-metal borrowed lights were ordinary office construction long
before 1990 (SDI profiles, NPS ITS 31's corridor glazing); slotted oval-head
screws are the period fastener (no Torx / tamper heads).
"""

import math

from mathutils import Matrix, Vector

import kitlib
import interact_window_common as wc

NAME = "Kit_WindowFrame_Steel"
LOD1_RATIO = 0.36                # §9.4 value; the collapse takes the zero-cost wear-station loops (pass 4)
LOD2_RATIO = 0.061
LOD1 = LOD1_RATIO
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0

STEEL = "Prop_SteelBrown"
RUBBER = "Prop_Rubber"

kitlib.register_slot("Door_Enamel", (0.61, 0.56, 0.43), 0.45, 0.0)   # almond enamel (05 tested sRGB 205/197/176)
VARIANTS = {"Kit_WindowFrame_Steel_Enamel": {STEEL: "Door_Enamel"}}

R_OUT, R_IN = 0.003, 0.0015      # 16 ga: 1.5 mm inside radius -> 3.0 mm outer
SEG_OUT = 3                      # segments on the R3.0 arrises (§1.8: 2-3); was 4, the saving pays for the jamb wear stations
GROOVE_D, GROOVE_N0, GROOVE_N1 = 0.0016, 0.0096, 0.0184
SCREW_N = 0.014                  # screw centre on the stop face (Z)
SLOT_W = 0.0011                  # #6 oval head (head ~6.6-7 mm): slot 1.0-1.2 mm wide (ASME B18.6.3); was 0.8
SETBACK = 0.0002                 # loose stop: 0.4 mm mitre joint, end walls show the soffit behind
VGROOVE = 0.0003                 # frame: welded-and-ground mitre, a closed 0.3 x 0.3 mm V hairline


def shell_profile():
    """Face A wall -> face band A -> soffit -> integral stop (B) -> soffit ->
    face band B -> face B wall (CCW)."""
    return wc.fillet([
        (wc.D_FACE, wc.SHELL_Z0),
        (wc.D_FACE, wc.FACE_Z, R_OUT, SEG_OUT),
        (wc.D_LINING, wc.FACE_Z, R_OUT, SEG_OUT),
        (wc.D_LINING, -wc.STOP_Z0, R_IN, 2),
        (0.0, -wc.STOP_Z0, R_OUT, SEG_OUT),
        (0.0, -wc.STOP_Z1, R_OUT, SEG_OUT),
        (wc.D_LINING, -wc.STOP_Z1, R_IN, 2),
        (wc.D_LINING, -wc.FACE_Z, R_OUT, SEG_OUT),
        (wc.D_FACE, -wc.FACE_Z, R_OUT, SEG_OUT),
        (wc.D_FACE, -wc.SHELL_Z0),
    ])


def loose_stop_profile():
    """Face A channel stop (CCW round the stop): room face -> sight-line face
    with the screw channel -> glass-side face."""
    return wc.fillet([
        (wc.D_LINING, wc.STOP_Z1),
        (0.0, wc.STOP_Z1, 0.002, 3),
        (0.0, GROOVE_N1, 0.0004, 1),
        (GROOVE_D, GROOVE_N1, 0.0004, 1),
        (GROOVE_D, GROOVE_N0, 0.0004, 1),
        (0.0, GROOVE_N0, 0.0004, 1),
        (0.0, wc.STOP_Z0, 0.002, 3),
        (wc.D_LINING, wc.STOP_Z0),
    ])


def screw_points():
    """30 screw seats on the face-A stop: (centre on the rim plane, axis)."""
    seat = GROOVE_D + 0.00002            # rim sunk 0.02 mm into the channel floor
    out = []
    jamb = [0.4175 + k * (1.9325 - 0.4175) / 7 for k in range(8)]
    rail = [-0.6325 + k * 1.265 / 6 for k in range(7)]
    for y in jamb:
        out.append(((wc.SIGHT_X + seat, y, SCREW_N), (-1, 0, 0), "R"))
        out.append(((-(wc.SIGHT_X + seat), y, SCREW_N), (1, 0, 0), "L"))
    for x in rail:
        out.append(((x, wc.SIGHT_Y1 + seat, SCREW_N), (0, -1, 0), "T"))
        out.append(((x, wc.SIGHT_Y0 - seat, SCREW_N), (0, 1, 0), "B"))
    return out, jamb, rail


# Wear stations (length fractions between the mitres). Pass 4: the jambs had
# none, so their only vertices sat at the two ends: the sill's primer value
# smeared up the whole jamb and the grab-height grime had no vertex to land
# on. Jambs: ~Y 0.42 (end of the shin scuffs), ~0.88 and ~1.42 (the grab band).
# Sill: ~X -0.62 / 0 / +0.62, so the climb-plant wear peaks at the centre.
STATIONS = {"B": (0.0, 0.06, 0.5, 0.94, 1.0),
            "R": (0.0, 0.06, 0.33, 0.64, 1.0),
            "L": (0.0, 0.06, 0.33, 0.64, 1.0)}


def shell_wear(X, Y, Z):
    """R hand grime, G paint worn through to grey primer, B cavity grime (§1.8)."""
    r = g = b = 1.0
    if Y <= wc.SIGHT_Y0 + 1e-6:                   # sill piece (and the jamb mitre ends below it)
        k = math.exp(-(X / 0.32) ** 2)            # the climb plant: palms, knees, boots at the centre
        g = 0.85 - 0.4 * k
        r = 1.0 - 0.3 * k
    elif Y < 0.42:
        g = 0.85                                  # shin scuffs low on the jambs
    elif Y > 1.95:
        g = 0.9                                   # head: light knocks only
    if abs(X) > wc.SIGHT_X and 0.85 < Y < 1.45 and abs(Z) > 0.09:
        r = 0.8                                   # hand grime on the jamb face bands (grab height)
    if abs(abs(Z) - wc.STOP_Z0) < 0.003 or abs(abs(Z) - wc.STOP_Z1) < 0.003:
        b = 0.65                                  # dirt in the stop / soffit corners
    return r, g, b


def stop_wear(X, Y, Z):
    """The loose stop has vertices only at its two mitred ends, so anything
    keyed to a corner smears along the whole piece (pass 3 painted every stop
    G 0.6 that way). Per-piece values only: light handling wear from
    reglazing, grime in the screw channel."""
    return (1.0, 0.9, 0.7 if abs(Z - SCREW_N) < 0.005 else 1.0)


def build(kit):
    # Dominant slot first (RT bridge reads submesh 0, spec §7.3 item 2).
    shell = shell_profile()
    for part in wc.frame_ring(kit, shell, STEEL, end=("vmitre", VGROOVE, VGROOVE), stations=STATIONS, name="frame"):
        wc.paint_wear(part, shell_wear)
    groove = loose_stop_profile()
    for part in wc.frame_ring(kit, groove, STEEL, end=("mitre", SETBACK), caps=True, name="loose stop"):
        wc.paint_wear(part, stop_wear)
    rnd = wc.rng(4120)
    seats, jamb, rail = screw_points()
    for i, (c, axis, side) in enumerate(seats):
        base = (0, 1, 0) if side in ("R", "L") else (1, 0, 0)
        ang = rnd.uniform(0.0, 3.14159)
        sd = Matrix.Rotation(ang, 3, Vector(axis)) @ Vector(base)   # random slot angle, as fitted
        floor = 0.0009 if i in (5, 17, 26) else -0.0001       # three paint-filled slots
        s = wc.slotted_oval_head(kit, c, axis, sd, STEEL, w=SLOT_W, floor=floor, rows=3, cols=5, name="screw")
        wc.paint_wear(s, lambda X, Y, Z: (1.0, 0.7, 0.8))
        kit.lod1_drop(s)
        wc.lod2_drop(s)
    for face in ("a", "b"):
        for part in wc.frame_ring(kit, wc.tape_dn(face), RUBBER, name="glazing tape " + face):
            wc.paint_wear(part)
            wc.lod2_drop(part)           # kept in LOD1: without it the empty pocket shows as a slot

    for x in (-0.35, 0.35):                                # neoprene setting blocks under the glass (hidden)
        blk = wc.box_u(kit, (0.100, 0.0033, 0.0056), (x, wc.LINING_Y0 + 0.00165, 0.0), RUBBER, bevel=0.0, name="setting block")
        wc.paint_wear(blk)
        kit.lod1_drop(blk)
        wc.lod2_drop(blk)

    # ---- self-checks that need no ray casting (the rest: scratchpad g4_check.py)
    assert len(seats) == 30
    assert max(b - a for a, b in zip(jamb, jamb[1:])) <= 0.2286 and max(b - a for a, b in zip(rail, rail[1:])) <= 0.2286
    wc.assert_clear_zone(kit)
    wc.assert_envelope(kit, (-wc.FACE_X1, wc.SIGHT_Y0 - wc.D_FACE, -wc.FACE_Z),
                       (wc.FACE_X1, wc.SIGHT_Y1 + wc.D_FACE, wc.FACE_Z))
    wc.interface_anchors(kit)
    wc.lod_meta(kit, LOD1_RATIO, LOD2_RATIO, LOD_DISTANCES)
    kit.no_collider()
    kit.tag("interactable", "window", "frame_steel")
    wc.wall_placement(kit)
