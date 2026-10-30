"""Pressed-steel DOUBLE-ACTING door frame (D1.6, A12 / A19): the frame of
every Office door, the Lobby LOCKED door and (P2) the Run doors; VARIANT
Kit_DoorFrame_Steel_Alu_DA (clear aluminium, the EXIT / stairwell doors).
A dark-bronze 16 ga sleeve with no formed stop (a cased opening): P return
-> P face band -> soffit -> S face band -> S return, mitred round the
opening, enclosing the map's trims; a seal slot through the hinge soffit for
the leaf's radius stile; black brush seals on the wall centre plane (hinge
seal, latch fin, head fin); the mortise strike prep (0.032 x 0.200 at Y
0.968) with a strike box; a fluted saddle in the frame's own finish (merge
M1 rule) with three countersunk screws; satin-chrome pivot caps.

Real-world reference: SDI hollow-metal frame for a centre-hung double-acting
door, 1955-1990 (hospital, kitchen and service corridors): no stop, pivot
caps only, brush seals on both stiles (02 §2.2, §5; 04 §3.2). No maker marks.

Origin: DOOR ROOT D = the pivot axis (see Kit_DoorFrame_Wood_DA). Spawn under
the chunk root at the hinge position, rotation door.closed, scale 1 (no
handing mirror), label "Door frame (kit)". +X = face S.

LODs (hand-built, fr_lods): LOD0 3-step bends, strike prep and box, fluted
saddle and screws, 96-segment caps; LOD1 1-step bends, plain saddle, 32-segment
caps; LOD2 the LOD1 sheet, seals and saddle, no caps. Slots: Prop_SteelBrown
(submesh 0), Prop_PlasticBlack (seals), Prop_Chrome (caps, screws).
"""

import random
import sys

import interact_door_common as dc
import interact_door_da_common as da

NAME = "Kit_DoorFrame_Steel_DA"
LOD1_RATIO = 0.37
LOD2_RATIO = 0.08
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0
VARIANTS = {"Kit_DoorFrame_Steel_Alu_DA": {"Prop_SteelBrown": "Prop_Aluminium"}}

STEEL = "Prop_SteelBrown"
SEAL = "Prop_PlasticBlack"
CHROME = "Prop_Chrome"
SADDLE_SCREW_Z = (0.15, 0.50, 0.85)


def sheet(kit, level):
    sec = da.steel_sleeve_section(level)
    jamb = dc.u_sweep(kit, sec, STEEL, "jamb", z_h=da.LINING_Z_H, z_l=da.LINING_Z_L, y_t=da.LINING_Y, continuous=True)[0]
    cuts = []   # FIX 1: no hinge slot (the stile turns about the visual pivot, 3 mm off the flat hinge lining)
    if level == 0:
        sw, sh, sy = da.STRIKE_MORTISE
        cuts.append(dc.cutter((-sw / 2, sy - sh / 2, da.LINING_Z_L - 0.0005), (sw / 2, sy + sh / 2, da.LINING_Z_L + 0.0025)))
    if cuts:
        dc.cut(jamb, cuts)
    return jamb, sec


def build(kit):
    rng = random.Random(41220)
    envelope = []
    secs = {}
    for level in (0, 1, 2):
        jamb, sec = sheet(kit, level)
        secs[level] = sec
        da.tag_lods(jamb, str(level))
        envelope.append(jamb)
    # FIX 1: the hinge lining is whole; the hinge fin (da.seals) sits on it, on the centre plane.
    sw, sh, sy = da.STRIKE_MORTISE
    box = dc.obox(kit, STEEL, lo=(-sw / 2 - 0.0015, sy - sh / 2 - 0.002, da.LINING_Z_L + da.STRIKE_DEPTH),
                  hi=(sw / 2 + 0.0015, sy + sh / 2 + 0.002, da.LINING_Z_L + da.STRIKE_DEPTH + 0.003), name="strike box")
    da.tag_lods(box, "0")
    envelope.append(box)

    ssec = dc.saddle_section(flutes=True)
    saddle = dc.loft_z(kit, [(da.LINING_Z_H, ssec), (da.LINING_Z_L, ssec)], STEEL, "saddle")
    da.tag_lods(saddle, "0")
    envelope.append(saddle)
    for z in SADDLE_SCREW_Z:
        s = dc.flat_head(kit, (0.0, da.SADDLE_H, z), (0, 1, 0), STEEL, rng, d=0.0080, host=saddle, name="saddle screw")
        da.tag_lods(s, "0")
    psec = dc.saddle_section(flutes=False)
    plain = dc.loft_z(kit, [(da.LINING_Z_H, psec), (da.LINING_Z_L, psec)], STEEL, "saddle plain")
    da.tag_lods(plain, "12")
    envelope.append(plain)

    envelope += da.seals(kit, SEAL, "012")
    for level in (0, 1):
        for c in da.pivot_caps(kit, CHROME, level):
            da.tag_lods(c, str(level))

    dc.finalize(kit, SMOOTH_ANGLE, da.wear_frame, protect=envelope)

    da.assert_frame(kit, NAME)
    sec = secs[0]
    xs = [x for d, x in sec]
    assert abs(min(xs) + da.CASING_X1) < 1e-6 and abs(max(xs) - da.CASING_X1) < 1e-6
    # d is measured from the soffit (Z 0.002): the face band over the trim (Z 0 .. -0.07) is d 0.002 .. 0.072
    inner_face = min(abs(x) for d, x in sec if 0.002 < d < 0.072 and abs(x) > 0.09)
    assert inner_face >= dc.TRIM_X + 0.003, "steel face band back %.4f must clear the trim face" % inner_face

    da.frame_anchors(kit)
    kit.no_collider()
    kit.tag("interactable", "door", "door_frame", "office", "lobby", "frame_steel", "double_acting")
    kit.meta["frameType"] = "steel"
    da.lod_meta(kit, sys.modules[__name__])
