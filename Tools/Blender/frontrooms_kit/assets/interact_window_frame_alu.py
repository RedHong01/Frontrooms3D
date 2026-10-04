"""Aluminium office front, W-EX (interactables 10_spec.md §5.1, P2;
06_period_windows.md §3.4): the Exit level's interior light. A clear-
anodised extruded aluminium wrap casing round all four sides, snap-in
bevelled glazing beads and black vinyl gaskets; under the Exit's cool light
it reads silver-cyan, unlike every other member.

Real-world reference (06 §2.1, §2.3 [read]): US4443984 (filed 1981):
aluminium casing extrusions that wrap drywall partitions at doors and
windows with a flat web "masking the margin" of the opening; US4463535 (USG,
filed 1982): demountable glass stops for borrowed lights with an elastomeric
spline; Kawneer "1/4" beveled glass stops" [search]; integral-colour and
clear anodising ordinary since the 1960s-70s (SAF). Generic: no maker's
extrusion marks.

Construction (window root W, Unity metres; see interact_window_common):
* One extrusion section swept round all four sides, sleeve mode: face band
  X 0.6995 -> 0.775 at Z +/-0.105 with a 6 mm x 2.5 mm shadow groove 40 mm
  from the opening edge (X 0.740-0.746), square return to the wall, soffit at
  |X| 0.6995 / Y 1.9995 / Y 0.3505 across the full depth. Crisp 0.5 mm
  arrises (extruded, not pressed). Mitred with a closed 0.3 mm V hairline.
  Wear stations on the sill (the climb plant: palms, rubbed arrises, boot
  scuffs) and at grab height on the jambs.
* Snap-in beads 16 x 16 on both faces, Z +/-(0.006 -> 0.022): a 4 mm sight-
  line face, then a 45-degree bevel up to a 4 mm foot on the soffit; mitred
  with 0.3 mm joints.
* Black vinyl gasket wedges, Z +/-(0.0035 -> 0.006), flush with the stop line,
  0.6 mm rounded lip; two setting blocks under the glass.
* No glass, no pane, no teeth (glass-destruction track).

Origin = window root (opening centre, wall centre line, floor). Front = face
A = kit -Y = Unity +Z. Size 1.55 x 1.80 (Y 0.275-2.075) x 0.21 m (the sill
band is the head's 0.0755 face, so 0.3505 - 0.0755 = 0.275).

Budget (§9.4): 2,800 / 1,000 / 200 tris. Built: 2,190 / 1,050 (LOD0 -22 %,
outside the §10.0 +/-15 % band; FLAGGED in the G4 build note; LOD1 +5 %).
A clip-on extrusion has no fasteners or mouldings to spend more on; the wear
stations (sill: the climb plant; jambs: the grab band, closed at ~Y 1.62 in
pass 4) are the only additions since pass 2 (1,088), and nothing is padded.
LOD1 (0.48, pass 4; §9.4 says "yes"): pass 3 had dropped it because the old
LOD1 was 98 % of LOD0 (nothing left to collapse). The wear stations changed
that: their loops lie on the straight sweep, so collapsing them costs zero
error; the collapse takes them and the soffit dirt-line points and moves
the surface by at most 0.06 mm (measured): LOD1 is the plain section again.
Checks (a)-(c) pass on LOD1.
LOD2 drops the gaskets and blocks (fr_lod2_drop). Slots: Prop_Aluminium
(first), Prop_Rubber. Sidecar placement "Wall" (wall_placement()).
Render-only: no collider.
"""

import math

import interact_window_common as wc

NAME = "Kit_WindowFrame_Alu"
LOD1_RATIO = 0.48                 # pass 4: the collapse takes the zero-cost wear-station loops (see the docstring)
LOD2_RATIO = 0.07
LOD1 = LOD1_RATIO
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 30.0
PREVIEW_WALL = (0.40, 0.55, 0.55)         # Exit: blue-green paper

ALU = "Prop_Aluminium"
RUBBER = "Prop_Rubber"

R_EDGE = 0.0005
G_IN, G_OUT, G_DEPTH = 0.740 - wc.SIGHT_X, 0.746 - wc.SIGHT_X, 0.0025      # shadow groove (d), 40 mm from the opening edge
VGROOVE = 0.0003
BEAD_JOINT = 0.00015


def face_band(_unused=1):
    """Face A part of the section (outer edge -> opening edge), CCW."""
    z, zg = wc.FACE_Z, wc.FACE_Z - G_DEPTH
    pts = [(wc.D_FACE, wc.SHELL_Z0), (wc.D_FACE, z, R_EDGE, 2), (G_OUT, z, 0.0003, 1), (G_OUT, zg, 0.0003, 1),
           (G_IN, zg, 0.0003, 1), (G_IN, z, 0.0003, 1), (wc.D_LINING, z, R_EDGE, 2)]
    return pts


SOFFIT_LINE = 0.0225                # soffit vertices just outside the beads (Z 0.022): the dirt line at the bead foot


def shell_profile():
    """Face A band -> soffit -> face B band (CCW); face B mirrors face A with
    each corner keeping its radius. Two plain soffit points at Z +/-0.0225
    give the wear its bead-foot dirt line."""
    a = face_band(1)
    b = [(p[0], -p[1]) + tuple(p[2:]) for p in reversed(a)]
    return wc.fillet(a + [(wc.D_LINING, SOFFIT_LINE), (wc.D_LINING, -SOFFIT_LINE)] + b)


def bead_profile():
    return wc.fillet([(wc.D_LINING, wc.STOP_Z1), (0.012, wc.STOP_Z1, 0.0004, 2), (0.0, 0.010, 0.0004, 2),
                      (0.0, wc.STOP_Z0, 0.0004, 2), (wc.D_LINING, wc.STOP_Z0)])


# Wear stations (length fractions between the mitres). The sill is the climb
# plant (palms and boots go there every break); the jambs are where a climber
# grabs. With no LOD1 there is no collapse to turn them into slivers.
STATIONS = {"B": (0.0, 0.06, 0.19, 0.36, 0.5, 0.64, 0.81, 0.94, 1.0),     # X ~ -0.45 / -0.2 / 0 / 0.2 / 0.45
            "R": (0.0, 0.08, 0.31, 0.485, 0.66, 0.76, 1.0),                # Y ~ 0.45 / 0.85 / 1.15 / 1.45 / 1.62
            "L": (0.0, 0.08, 0.31, 0.485, 0.66, 0.76, 1.0)}                # (1.62 ends the grab band; pass 3 faded it to the head)


def shell_wear(X, Y, Z):
    """R hand grime, G anodise scuffed through, B cavity grime (§1.8)."""
    ax, az = abs(X), abs(Z)
    r = g = b = 1.0
    if Y < wc.SIGHT_Y0:                                       # sill: the climb plant
        k = math.exp(-(X / 0.32) ** 2)
        if Y > 0.335:
            r = 1.0 - 0.35 * k                                # palms on the sill top and its arrises
            if az > 0.095:
                g = 1.0 - 0.45 * k                            # anodise rubbed off the arrises
        elif Y < 0.30:
            g = 0.85                                          # boot scuffs low on the sill band
    elif ax > wc.SIGHT_X and 0.80 < Y < 1.50 and az > 0.09 and ax < 0.745:
        r = 0.8                                               # hand grime on the jamb face bands
    if 0.1015 < az < 0.1035 or az < SOFFIT_LINE + 0.001:
        b = 0.72                                              # shadow-groove floor; dirt at the bead foot
    return r, g, b


def build(kit):
    for part in wc.frame_ring(kit, shell_profile(), ALU, end=("vmitre", VGROOVE, VGROOVE), stations=STATIONS,
                              name="casing"):
        wc.paint_wear(part, shell_wear)
    bead = bead_profile()
    for face in ("a", "b"):
        prof = bead if face == "a" else wc.mirror_b(bead)
        for part in wc.frame_ring(kit, prof, ALU, end=("mitre", BEAD_JOINT), caps=True, name="bead " + face):
            wc.paint_wear(part)
    for face in ("a", "b"):
        for part in wc.frame_ring(kit, wc.tape_dn(face, lip=0.0006), RUBBER, name="gasket " + face):
            wc.paint_wear(part)
            wc.lod2_drop(part)
    for x in (-0.35, 0.35):
        blk = wc.box_u(kit, (0.100, 0.0033, 0.0056), (x, wc.LINING_Y0 + 0.00165, 0.0), RUBBER, bevel=0.0, name="setting block")
        wc.paint_wear(blk)
        wc.lod2_drop(blk)

    wc.assert_clear_zone(kit)
    wc.assert_envelope(kit, (-wc.FACE_X1, wc.SIGHT_Y0 - wc.D_FACE, -wc.FACE_Z), (wc.FACE_X1, wc.SIGHT_Y1 + wc.D_FACE, wc.FACE_Z))
    assert wc.FACE_Z - G_DEPTH > wc.TRIM_Z, "shadow groove would cut the map trim face"
    wc.interface_anchors(kit)
    wc.lod_meta(kit, LOD1_RATIO, LOD2_RATIO, LOD_DISTANCES)
    kit.no_collider()
    kit.tag("interactable", "window", "frame_alu")
    wc.wall_placement(kit)
