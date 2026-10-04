"""Back-office light, W-L0 (interactables 10_spec.md §5.1-§5.5,
06_period_windows.md §3.1): a fixed interior window in the store's older
back rooms, built in the second-hand era (1955-85) and still in use in 1990.
Stained walnut trim, the same casing as the free veneer door
(Kit_DoorFrame_Wood, §2.3), so a player reads "wood = the building's own
rooms".

Real-world reference: standard interior window carpentry (This Old House,
"How to trim an interior window" [read via 06]): jamb liner, casing on both
faces, a through-stool with horns ("add ... 2 inches total for horn
projection"), an apron under the stool nose with returned ends, applied wood
glazing stops; NPS ITS 31's corridors "distinguished by mahogany trim around
openings" with "interior sidelight windows" [read via 06]. Generic, no
maker's marks.

Construction (window root W, Unity metres; see interact_window_common):
* Ranch casing with back band on both faces, the shared §2.3 profile (77 mm
  x 25.5 mm; w >= 0.0215 over the trim zone, asserted), mitred at the head
  with a closed 0.2 mm V hairline (the round-over passes 0.4 mm from the map
  trim's corner, so a deeper V or an open joint would expose it), the jamb
  casings landing on the stool.
* Jamb and head liners at |X| 0.6995 / Y 1.9995 over the bare reveal, Z
  +/-0.0795 between the casings, swept as ONE welded profile with both
  casings per side (no casing/liner seam for the LOD1 collapse to open).
* Through-stool with horns: X +/-0.800, Z +/-0.121, Y 0.3255-0.3505,
  half-round nosings returned round the horn ends. Its top IS the sill (Y
  0.3505, 0.5 mm over the map's bare wallpaper sill top). Climb plant here.
* Apron on both faces: the casing profile, X +/-0.7765, Y 0.2485-0.3255,
  returned ends.
* Glazing stops 16 x 16 on both faces, all four sides, Z +/-(0.006 ->
  0.022): square bead with a 3 mm quirked ovolo on the exposed arris, 0.5 mm
  ease at the sight line, mitred with 0.3 mm open joints (end grain shows;
  behind them is the liner or stool, never the map trims).
* Glazing compound line (Prop_Rubber), Z +/-(0.0035 -> 0.006), flush with the
  stop line; two neoprene setting blocks under the glass at X +/-0.35. Period
  wood-stop glazing was bedded in compound or putty, not a black gasket: the
  main project's facade (FrontRoomsInteractableKit.Window.cs, WindowParts.
  Frame) swaps this submesh to Resources/Surfaces/Prop_Putty for W-L0 once
  that surface exists; the mesh keeps Prop_Rubber as §5.1 specifies.
* No glass, no pane, no teeth (glass-destruction track).

Origin = window root (opening centre, wall centre line, floor). Front = face
A = kit -Y = Unity +Z (the map turns it toward the non-tall cell). Size
1.60 x 1.828 (Y 0.2485-2.0765) x 0.242 m.

Budget (§9.4): 2,600 / 1,000 / 200 tris; built 2,322 / 1,044. LOD1 at 0.45
(setting blocks drop; the stops are protected from the collapse (lod_keep),
so the pocket edge stays clean; casing, stool and apron lose their wear
stations and some arc segments, moving the surface by at most 0.25 mm,
measured in pass 4; checks a-c pass on LOD1 too); LOD2 0.077 (fr_lod2_drop on the
compound line and the setting blocks; the ovolo is part of the stop sweep
and collapses with it).
LOD distances 4 / 12 / none. Sidecar placement "Wall" (wall_placement()).
Slots: Prop_WoodWalnut (first = submesh 0, the
RT bridge reads only that, §7.3), Prop_Rubber. Render-only: no collider.

Era: stool-and-apron trim with ranch casing is 1955-85 back-office stock; a
1990 fit-out would leave it in place (22_era_lock §2: second-hand back to
~1955). Nothing printed, nothing dated.
"""

import math

import kitlib  # noqa: F401  (slots)
import interact_window_common as wc

NAME = "Kit_WindowFrame_Wood"
LOD1_RATIO = 0.45
LOD2_RATIO = 0.077
LOD1 = LOD1_RATIO
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0
PREVIEW_WALL = (0.60, 0.52, 0.30)        # Level 0 wallpaper value for the g4 renders

WALNUT = "Prop_WoodWalnut"
RUBBER = "Prop_Rubber"

STOOL_Y0, STOOL_Y1 = 0.3255, 0.3505
STOOL_X, STOOL_Z = 0.800, 0.121
NOSE_R = (STOOL_Y1 - STOOL_Y0) / 2
VGROOVE = 0.0002                          # casing mitre V: the §2.3 round-over passes 0.4 mm from the trim corner
STOP_JOINT = 0.00015                      # 0.3 mm mitre joint on the stops


def stop_profile():
    """Face A wood stop, CCW: room face -> quirked 3 mm ovolo -> sight-line
    face -> glass side. Open at the back (it sits on the liner / stool)."""
    pts = [(wc.D_LINING, wc.STOP_Z1), (0.0035, wc.STOP_Z1), (0.0035, 0.0215)]
    for k in range(1, 7):                  # quarter round R3 about (0.0035, 0.0185), 90 -> 180 deg
        a = math.radians(90 + 90 * k / 6)
        pts.append((0.0035 + 0.003 * math.cos(a), 0.0185 + 0.003 * math.sin(a)))
    pts.append((0.0, 0.0185))
    pts += wc.fillet([(0.0, 0.0185), (0.0, wc.STOP_Z0, 0.0005, 2), (wc.D_LINING, wc.STOP_Z0)])[1:]
    return pts


def stool_profile(segs=10):
    """(e, Y) from the top (e = 0) round the half-round nosing to the bottom."""
    yc = (STOOL_Y0 + STOOL_Y1) / 2
    return [(NOSE_R * math.sin(math.pi * k / segs), yc + NOSE_R * math.cos(math.pi * k / segs)) for k in range(segs + 1)]


def apron_section(face):
    """Closed (Z, Y) section of the apron: the casing profile hung under the
    stool, thin edge up; the back (against the wall) closes it."""
    s = 1.0 if face == "a" else -1.0
    return [(s * (wc.SHELL_Z0 + w), STOOL_Y0 - u) for (u, w) in wc.ranch_casing_uw(5)]


def casing_wear(X, Y, Z):
    return (1.0, 0.8 if Y < 0.55 else 1.0, 1.0)


def stool_wear(X, Y, Z):
    r = 1.0 - (0.45 * math.exp(-(X / 0.32) ** 2) if abs(Z) > 0.06 else 0.0)   # palms at the climb plant
    g = 0.6 if abs(Z) > 0.112 and Y > STOOL_Y0 + 0.003 else 1.0              # finish worn off the nosing
    b = 0.7 if abs(Z) < 0.03 else 1.0                                       # grit along the stops
    return (r, g, b)


def stop_wear(X, Y, Z):
    """Stops have vertices only at their mitred ends, so a corner test smears
    along the whole piece (pass 3 painted every stop B 0.65 that way).
    Per-piece values: dust along the glazing line on every stop, finish
    rubbed on the sill stops (Y < 0.37)."""
    return (1.0, 0.75 if Y < 0.37 else 1.0, 0.8)


def build(kit):
    # ---- walnut first (submesh 0)
    # Casing A + liner + casing B as ONE welded profile per side: no seam
    # between casing and liner for the LOD1 collapse to open onto the trims.
    trim = wc.casing_dn("a", segs=5) + wc.casing_dn("b", segs=5)
    jamb_st = (0.0, 0.08, 0.35, 1.0)
    for side in ("R", "L"):
        p = wc.sweep_side(kit, side, trim, WALNUT, ("at", STOOL_Y1), ("vmitre", VGROOVE, VGROOVE), jamb_st,
                          name="casing+liner " + side)
        wc.paint_wear(wc.grain(p, "Y"), casing_wear)
    p = wc.sweep_side(kit, "T", trim, WALNUT, ("vmitre", VGROOVE, VGROOVE), ("vmitre", VGROOVE, VGROOVE),
                      name="casing+liner T")
    wc.paint_wear(wc.grain(p, "X"), casing_wear)

    xs = (0.04, 0.12, 0.25, 0.375, 0.5, 0.625, 0.75, 0.88, 0.96)
    zs = (0.1, 0.5, 0.9)
    stool = wc.rounded_slab(kit, 0.0, 0.0, STOOL_X - NOSE_R, STOOL_Z - NOSE_R, stool_profile(10), xs, zs, WALNUT, name="stool")
    wc.paint_wear(wc.grain(stool, "X"), stool_wear)

    for face in ("a", "b"):
        ap = wc.extrude_x(kit, apron_section(face), -(wc.LINING_X + 0.077), wc.LINING_X + 0.077, WALNUT, name="apron " + face)
        wc.paint_wear(wc.grain(ap, "X"))

    sp = stop_profile()
    for face in ("a", "b"):
        prof = sp if face == "a" else wc.mirror_b(sp)
        for part in wc.frame_ring(kit, prof, WALNUT, end=("mitre", STOP_JOINT), caps=True, name="stop " + face):
            wc.paint_wear(wc.grain(part, "Y" if part.name.endswith(("R", "L")) else "X"), stop_wear)
            wc.lod_keep(part)

    # ---- black glazing compound and setting blocks
    for face in ("a", "b"):
        for part in wc.frame_ring(kit, wc.tape_dn(face), RUBBER, name="compound " + face):
            wc.paint_wear(part)
            wc.lod2_drop(part)           # kept in LOD1 (see the steel frame)
    for x in (-0.35, 0.35):
        blk = wc.box_u(kit, (0.100, 0.0033, 0.0056), (x, wc.LINING_Y0 + 0.00165, 0.0), RUBBER, bevel=0.0, name="setting block")
        wc.paint_wear(blk)
        kit.lod1_drop(blk)
        wc.lod2_drop(blk)

    # ---- self-checks (ray checks: scratchpad g4_check.py)
    assert abs(wc.assert_casing_rule(wc.ranch_casing_uw(5)) - 0.0215) < 0.002
    wc.assert_clear_zone(kit)
    wc.assert_envelope(kit, (-STOOL_X, STOOL_Y0 - 0.077, -STOOL_Z), (STOOL_X, wc.SIGHT_Y1 + wc.D_LINING + 0.077, STOOL_Z))
    wc.interface_anchors(kit)
    wc.lod_meta(kit, LOD1_RATIO, LOD2_RATIO, LOD_DISTANCES)
    kit.no_collider()
    kit.tag("interactable", "window", "frame_wood")
    wc.wall_placement(kit)
