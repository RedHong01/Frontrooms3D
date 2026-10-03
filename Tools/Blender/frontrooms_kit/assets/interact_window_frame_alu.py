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
* Snap-in beads 16 x 16 on both faces, Z +/-(0.006 -> 0.022): a 4 mm sight-
  line face, then a 45-degree bevel up to a 4 mm foot on the soffit; mitred
  with 0.3 mm joints.
* Black vinyl gasket wedges, Z +/-(0.0035 -> 0.006), flush with the stop line,
  1 mm rounded lip; two setting blocks under the glass.
* No glass, no pane, no teeth (glass-destruction track).

Origin = window root (opening centre, wall centre line, floor). Front = face
A = kit -Y = Unity +Z. Size 1.55 x 1.80 (Y 0.2745-2.075) x 0.21 m.

Budget (§9.4): 2,800 / 1,000 / 200 tris (ESTIMATE). A clip-on extrusion has
no fasteners or mouldings to spend that on, so it lands near 1,500; nothing is
padded (see the G4 build note). LOD1 now at 0.36; LOD2 drops the gaskets and
blocks. LOD distances 4 / 12 / none. Slots: Prop_Aluminium (first),
Prop_Rubber. Render-only: no collider.
"""

import interact_window_common as wc

NAME = "Kit_WindowFrame_Alu"
LOD1_RATIO = 0.36
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


def shell_profile():
    """Face A band -> soffit -> face B band (CCW); face B mirrors face A with
    each corner keeping its radius."""
    a = face_band(1)
    b = [(p[0], -p[1]) + tuple(p[2:]) for p in reversed(a)]
    return wc.fillet(a + b)


def bead_profile():
    return wc.fillet([(wc.D_LINING, wc.STOP_Z1), (0.012, wc.STOP_Z1, 0.0004, 2), (0.0, 0.010, 0.0004, 2),
                      (0.0, wc.STOP_Z0, 0.0004, 2), (wc.D_LINING, wc.STOP_Z0)])


def shell_wear(X, Y, Z):
    return (0.85 if (Y < 0.40 and abs(Z) > 0.09) else 1.0, 0.7 if Y < 0.36 else 1.0, 1.0)


def build(kit):
    for part in wc.frame_ring(kit, shell_profile(), ALU, end=("vmitre", VGROOVE, VGROOVE),
                              stations={"B": (0.0, 0.08, 0.5, 0.92, 1.0)}, name="casing"):
        wc.paint_wear(part, shell_wear)
    bead = bead_profile()
    for face in ("a", "b"):
        prof = bead if face == "a" else wc.mirror_b(bead)
        for part in wc.frame_ring(kit, prof, ALU, end=("mitre", BEAD_JOINT), caps=True, name="bead " + face):
            wc.paint_wear(part)
    for face in ("a", "b"):
        for part in wc.frame_ring(kit, wc.tape_dn(face, lip=0.001), RUBBER, name="gasket " + face):
            wc.paint_wear(part)
            kit.lod1_drop(part)
            wc.lod2_drop(part)
    for x in (-0.35, 0.35):
        blk = wc.box_u(kit, (0.100, 0.0033, 0.0056), (x, wc.LINING_Y0 + 0.00165, 0.0), RUBBER, bevel=0.0, name="setting block")
        wc.paint_wear(blk)
        kit.lod1_drop(blk)
        wc.lod2_drop(blk)

    wc.assert_clear_zone(kit)
    wc.assert_envelope(kit, (-wc.FACE_X1, wc.SIGHT_Y0 - wc.D_FACE, -wc.FACE_Z), (wc.FACE_X1, wc.SIGHT_Y1 + wc.D_FACE, wc.FACE_Z))
    assert wc.FACE_Z - G_DEPTH > wc.TRIM_Z, "shadow groove would cut the map trim face"
    wc.interface_anchors(kit)
    wc.lod_meta(kit, LOD1_RATIO, LOD2_RATIO, LOD_DISTANCES)
    kit.no_collider()
    kit.tag("interactable", "window", "frame_alu")
