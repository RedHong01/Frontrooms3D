"""Pressed-steel door frame (hollow-metal "knock-down" frame in sleeve mode)
for every family member except the Lobby free door: L0-K, OF-F, OF-K, and
the P2 Run / Exit members. Dark-bronze paint, a formed 5/8" push-side stop
with three rubber silencers, a fluted aluminium saddle, and the frame halves
of three satin-chrome 4-1/2" five-knuckle butts (10_spec §1.4, §2.3).

Real-world reference: an SDI 111-type 16 ga single-rabbet hollow-metal
frame, the US commercial standard 1955-1990: 2" face (here widened to a
77 mm sleeve so it swallows the map's 0.07 x 0.20 trims), 5/8" stop, square
returns, 1.5 mm inside bend radii, hairline mitres, rubber silencers on the
latch stop, a mortise strike prep, and a 1/2" fluted aluminium transition
saddle with 1:2 bevels (ADAAG 1991 4.13.8). The face is plain: no moulding,
which is the close-range difference from the wood casing (05 §6.2). No
maker marks. VARIANT Kit_DoorFrame_Steel_Alu (Exit members) swaps the paint
for the clear-anodised look.

Origin: DOOR ROOT D (10_spec §1.2): hinge-jamb edge of the opening on the
wall centre line at floor level; Unity +Z along the opening to the latch
jamb, +Y up, +X the swing / pull face S, -X the push face P (stop side).
Spawn under the chunk root at the hinge position, rotation door.closed,
localScale (S_sign, 1, 1), label "Door frame (kit)".

Section (Unity m): one closed 1.5 mm sheet swept round three sides: P
return (Z -0.075) -> P face band (X -0.105) -> P soffit (Z 0.002) -> formed
stop (X -0.0605..-0.025, Z 0.002..0.018; head 2.082..2.098) -> rabbet soffit
(Z 0.002) -> S face band (X +0.105) -> S return; outer radii 3.0 mm convex /
1.5 mm concave. Head soffit Y 2.098, face top 2.175. Hinge preps are cut
through the soffit and filled flush by the chrome frame leaves; the strike
prep is a 0.032 x 0.200 hole centred Y 0.968 (§2.3) with a dark-bronze
strike box 1.6 mm behind the soffit face, so G2's mortise strike sits flush.
Silencers: Ø 8 x 2.5 mm at Y 0.35 / 1.30 / 1.90, Z 0.988 on the latch stop
(0.5 mm off the closed leaf: by design, like a real door).

Budget (§9.1): 3,600 / 1,300 / 280 tris; slots SteelBrown (submesh 0),
Rubber, Chrome, Aluminium. Render-only (kit.no_collider()). LOD1 0.40
keeps the sheet + saddle envelope uncollapsed (fr_lod_keep), so LOD1 stays
gap-free; screws, pin tips and silencers drop at LOD1; hinges flagged for
LOD2.
"""

import random
import sys

import interact_door_common as dc

NAME = "Kit_DoorFrame_Steel"
LOD1 = 0.37
LOD1_RATIO = 0.37
LOD2_RATIO = 0.08
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0
VARIANTS = {"Kit_DoorFrame_Steel_Alu": {"Prop_SteelBrown": "Prop_Aluminium"}}

STEEL = "Prop_SteelBrown"
RUBBER = "Prop_Rubber"
CHROME = "Prop_Chrome"
ALU = "Prop_Aluminium"

SILENCER_Y = (0.35, 1.30, 1.90)
SILENCER_Z = 0.988
SADDLE_SCREW_Z = (0.15, 0.50, 0.85)
CLOSER_SHOE = (0.125, 2.130, 0.100)


def wear(P, N, edge, obj):
    X, Y, Z = P
    r = g = b = 1.0
    nm = obj.name
    if "jamb" in nm:
        # hands on the latch-side face band and soffit at lever height
        if Z > 0.95:
            r -= 0.20 * dc.smoothstep(0.75, 0.95, Y) * (1 - dc.smoothstep(1.35, 1.55, Y))
        if edge:
            g -= 0.15 + 0.40 * (1 - dc.smoothstep(0.05, 0.45, Y))   # scuffed to primer low down
    if "saddle" in nm:
        r -= 0.25 * (1 - min(1.0, abs(X) / 0.05))
        if edge:
            g -= 0.35
    if obj.get("fr_screw"):
        b -= 0.35
    return r, g, b


def build(kit):
    rng = random.Random(41020)
    envelope = []

    # 1. The sheet: one continuous mitred sweep (dark bronze = submesh 0).
    sec = dc.steel_jamb_section()
    jamb = dc.u_sweep(kit, sec, STEEL, "jamb", z_h=dc.STEEL_Z_REF, z_l=1.0 - dc.STEEL_Z_REF,
                      y_t=dc.HEAD_STOP_Y0, continuous=True)[0]
    envelope.append(jamb)
    cuts = [dc.cutter((dc.PLATE_X0, y0, -0.001), (0.030, y1, 0.0025)) for y0, y1 in dc.HINGES]
    sw, sh, sy = dc.STRIKE_MORTISE
    cuts.append(dc.cutter((-sw / 2, sy - sh / 2, dc.LINING_Z_L - 0.0005), (sw / 2, sy + sh / 2, dc.LINING_Z_L + 0.0025)))
    dc.cut(jamb, cuts)
    floor = dc.obox(kit, STEEL, lo=(-sw / 2 - 0.0015, sy - sh / 2 - 0.002, dc.LINING_Z_L + dc.STRIKE_DEPTH),
                    hi=(sw / 2 + 0.0015, sy + sh / 2 + 0.002, dc.LINING_Z_L + dc.STRIKE_DEPTH + 0.003), name="strike box")
    envelope.append(floor)

    # 2. Rubber silencers on the latch stop face (X -0.025 -> -0.0225).
    for y in SILENCER_Y:
        prof = [(0.004, 0.0), (0.004, 0.0008), (0.0030, 0.0021), (0.0, 0.0025)]
        s = dc.revolve(kit, prof, (dc.STOP_X1, y, SILENCER_Z), (1, 0, 0), 16, RUBBER, "silencer", close_start=False)
        kit.lod1_drop(s)
        dc.lod2_drop(s)

    # 3. Frame halves of the three butts (satin chrome).
    for y0, _ in dc.HINGES:
        for p in dc.hinge_half(kit, "frame", y0, CHROME, rng):
            dc.lod2_drop(p)
            if p.get("fr_screw") or "tip" in p.name or "web" in p.name:
                kit.lod1_drop(p)

    # 4. Fluted aluminium saddle, three countersunk screws on the centre land.
    ssec = dc.saddle_section(flutes=True)
    saddle = dc.loft_z(kit, [(dc.LINING_Z_H, ssec), (dc.LINING_Z_L, ssec)], ALU, "saddle")
    envelope.append(saddle)
    for z in SADDLE_SCREW_Z:
        s = dc.flat_head(kit, (0.0, dc.SADDLE_H, z), (0, 1, 0), ALU, rng, d=0.0080, host=saddle, name="saddle screw")
        kit.lod1_drop(s)
        dc.lod2_drop(s)

    dc.finalize(kit, SMOOTH_ANGLE, wear, protect=envelope)

    # ---- checks ---------------------------------------------------------
    dc.assert_box(kit.parts, (-dc.CASING_X1, 0.0, -0.075), (dc.CASING_X1, dc.CASING_TOP, 1.075), what=NAME)
    dc.assert_outside_leaf(kit.parts, NAME)
    xs = [x for d, x in sec]
    ds = [d for d, x in sec]
    assert abs(min(xs) + dc.CASING_X1) < 1e-6 and abs(max(xs) - dc.CASING_X1) < 1e-6
    assert abs(min(ds)) < 1e-6 and abs(max(ds) - (dc.STEEL_Z_REF + 0.075)) < 1e-6
    # the sheet's back must stay outside the trims (X +-0.10 face, Z <= 0)
    inner_face = min(abs(x) for d, x in sec if 0.020 < d < 0.090 and abs(x) > 0.09)
    assert inner_face >= dc.TRIM_X + 0.003, "steel face band back %.4f must clear the trim face" % inner_face
    for y in SILENCER_Y:
        assert dc.STOP_X1 + 0.0025 < -dc.LEAF_X, "silencer must stay off the closed leaf"

    # ---- metadata -------------------------------------------------------
    dc.anchor(kit, "strike", 0.0, dc.STRIKE_MORTISE[2], dc.LINING_Z_L)
    dc.anchor(kit, "strike_bored", 0.0, dc.STRIKE_BORED[2], dc.LINING_Z_L)
    dc.anchor(kit, "hinge_axis", dc.AXIS_X, 0.0, dc.AXIS_Z)
    dc.anchor(kit, "hinge_axis_dir", dc.AXIS_X, 0.10, dc.AXIS_Z)
    dc.anchor(kit, "head_dust_a", 0.0, dc.LINING_Y, 0.05)
    dc.anchor(kit, "head_dust_b", 0.0, dc.LINING_Y, 0.95)
    dc.anchor(kit, "threshold", 0.0, dc.SADDLE_H, 0.50)
    dc.anchor(kit, "closer_shoe", *CLOSER_SHOE)
    dc.anchor(kit, "exit_sign_p", -0.080, 2.280, 0.500)
    kit.no_collider()
    kit.tag("interactable", "door", "door_frame", "office", "lobby", "frame_steel")
    kit.meta["frameType"] = "steel"
    kit.meta["doorRoot"] = "D: origin hinge-jamb edge, wall centre line, floor; +Z along opening; +X swing (pull) face"
    dc.lod_meta(kit, sys.modules[__name__])
