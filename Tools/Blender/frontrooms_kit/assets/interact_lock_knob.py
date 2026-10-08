"""Ball knob with a knurled band: the LOCKED door's passage knob, one per
face on the mortise escutcheon (10_spec §2.1, §3.1, §3.4). Satin chrome.
The knurled band is the UFAS 1984 §4.29.3 tactile mark ("a textured, e.g.
knurled, handle or knob" on doors to hazardous / service areas, 02 §3.1):
it marks the locked back-of-house door from the free door's lever, and it
is period-true for 1984-93 (knob trim was standard before the ADA).

Real-world reference: Schlage "Orbit"-type ball knob, Ø 2-1/8" (0.054),
2-5/8" projection (02 §4.1). Shank Ø 0.019 rising out of the escutcheon's
boss; ball Ø 0.054 centred 0.047 out; a 72-flute straight knurl (0.6 mm
deep) on a turned band Z 0.040-0.054; a rounded crown onto a domed face
(sphere R 0.070, fix pass 2026-10-08: the old near-flat face read as a radio
dial) peaking at Z 0.063 = 0.065 proud of the leaf face (map limit ~0.07).

Origin (PART frame, 10_spec §1.2): the spindle axis on the escutcheon's front
face (= the escutcheon's knob_pivot anchor; door X +-0.024, Y 0.9365,
Z 0.920). Front = Unity +Z (kit -Y), outward. The shank starts 0.2 mm off
the origin and stands in the boss bore (r 0.0100) with 0.5 mm clearance.

Motion: rotate about part Z, -40 .. +40 deg (it retracts the mortise latch;
§3.4 knob beats and the rattle). Symmetric, so the §1.2 mirror on _p is
harmless; negate angles on mirrored placements to keep the visual sense.
Anchors: spindle (0, 0, 0); face (0, 0, 0.063); axis_out (0, 0, 0.10).
Budget (§9.2): LOD0 4,500 (asserted +-15 %), LOD1 1,800, LOD2 300; LOD
distances 1.5 / 4 / 12 m; LOD1 exported since the 2026-10-08 fix pass (critic H4). 96-segment lathe;
the knurl band is 288 around (4 vertices per flute).
Slots: Prop_Chrome. VARIANT _Brass (US3 bright brass).
"""

import math

import interact_lock_common as lc

NAME = "Kit_Lock_Knob"
LOD1_RATIO = 0.40
LOD1 = LOD1_RATIO  # fix pass 2026-10-08 (critic H4): FrontRoomsKitImporter honours the sidecar distances since 663e858, so LOD1 = LOD0->LOD1 at d01 and a cull at dcull
LOD2_RATIO = 0.067
LOD_DISTANCES = (1.5, 4.0, 12.0)
BUDGET = 4500
VARIANTS = {"Kit_Lock_Knob_Brass": {lc.CHROME: lc.BRASS}}
SMOOTH_ANGLE = 40.0

SEGS = 96
SHANK_R = 0.019 / 2
BALL_R, BALL_Z = 0.054 / 2, 0.047
BAND_Z0, BAND_Z1 = 0.040, 0.054
FLUTES, FLUTE_DEPTH = 72, 0.0006
CREST_R = BALL_R
FACE_Z = 0.063
FACE_DOME_R = 0.070      # fix pass 2026-10-08 (L3): crowned face, sphere R 0.07 through FACE_Z on the axis
CROWN_ROUND_DEG = 7.0    # how far down the ball the crown round starts (deg of ball latitude)
CROWN_ROUND_R = 0.0012   # how far in on the dome it ends (m, radial)


def _dome_at(r):
    return (r, FACE_Z - FACE_DOME_R + math.sqrt(FACE_DOME_R ** 2 - r * r))


def crown():
    """(z, r) of the circle where the face dome meets the ball sphere."""
    zd = FACE_Z - FACE_DOME_R                       # dome centre on the axis
    # r^2 = BALL_R^2 - (z - BALL_Z)^2 = FACE_DOME_R^2 - (z - zd)^2  ->  linear in z
    z = (FACE_DOME_R ** 2 - BALL_R ** 2 - zd * zd + BALL_Z * BALL_Z) / (2 * (BALL_Z - zd))
    return z, math.sqrt(BALL_R ** 2 - (z - BALL_Z) ** 2)


CROWN_Z = crown()[0] - 0.0004                       # wear: the crown band starts just below the crease


def _sphere(phi_deg):
    p = math.radians(phi_deg)
    return (BALL_R * math.cos(p), BALL_Z + BALL_R * math.sin(p))


def _knurl_r(i, a):
    k = i % 4
    return CREST_R if k in (0, 1) else CREST_R - FLUTE_DEPTH


def profile():
    fil = 0.003
    fc_r = SHANK_R + fil
    fc_z = BALL_Z - math.sqrt((BALL_R + fil) ** 2 - fc_r ** 2)
    d = math.hypot(fc_r, fc_z - BALL_Z)
    tp = (fc_r * BALL_R / d, BALL_Z + (fc_z - BALL_Z) * BALL_R / d)    # fillet / sphere tangent point
    a_end = math.atan2(tp[1] - fc_z, tp[0] - fc_r)
    a_mid = (math.pi + a_end) / 2
    phi_p = math.degrees(math.asin((tp[1] - BALL_Z) / BALL_R))
    phi_b0 = math.degrees(math.asin((BAND_Z0 - BALL_Z) / BALL_R))
    phi_b1 = math.degrees(math.asin((BAND_Z1 - BALL_Z) / BALL_R))
    ledge = CREST_R - FLUTE_DEPTH
    prof = [(0.0, 0.0002), (SHANK_R - 0.0003, 0.0002), (SHANK_R, 0.0005), (SHANK_R, fc_z),
            (fc_r + fil * math.cos(a_mid), fc_z + fil * math.sin(a_mid))]
    prof += [_sphere(phi_p + (phi_b0 - phi_p) * t / 4) for t in range(5)]
    prof += [(ledge, BAND_Z0),
             (CREST_R, BAND_Z0 + 0.0004, _knurl_r, FLUTES * 4),
             (CREST_R, BAND_Z1 - 0.0004, _knurl_r, FLUTES * 4),
             (ledge, BAND_Z1)]
    # Fix pass 2026-10-08 (critic L3): the face was a near-flat disc (0.9 mm sagitta over Ø 43 mm) behind a
    # crown chamfer, which read as a radio dial. It is now a true crowned ball-knob face: a sphere of radius
    # FACE_DOME_R through FACE_Z on the axis, meeting the ball at the crown circle, with a 3-step round there
    # (the light-catching crown), still 0.065 proud.
    zc, rc = crown()
    a_ball = math.degrees(math.asin((zc - BALL_Z) / BALL_R))
    prof += [_sphere(phi_b1 + (a_ball - CROWN_ROUND_DEG - phi_b1) * t / 2) for t in range(3)]
    prof += [_sphere(a_ball - CROWN_ROUND_DEG * 0.4)]
    prof += [_dome_at(rc - CROWN_ROUND_R * 0.45), _dome_at(rc - CROWN_ROUND_R)]
    for r in (0.019, 0.0125, 0.006):        # chord error <= 0.08 mm on the R 0.07 dome (0.13 px at 0.3 m)
        prof.append(_dome_at(r))
    prof.append((0.0, FACE_Z))
    return prof


def _wear(p, n, slot):
    x, y, z = p
    rad = math.hypot(x, y)
    r, g, b = 1.0, 1.0, 1.0
    if z > BAND_Z0 - 0.002:
        r = 0.78                                     # the gripped half: hand grime
    if BAND_Z0 < z < BAND_Z1:
        if rad < CREST_R - FLUTE_DEPTH * 0.5:
            b = 0.70                                 # knurl grooves hold dirt
        else:
            g = 0.72                                 # crests polished by hands
    if z > CROWN_Z - 0.0002 and rad > crown()[1] - CROWN_ROUND_R - 0.0004:
        g = 0.70                                     # the crown round: light edge wear
    if z > FACE_Z - 0.0005:
        r = 0.85
    return (r, g, b)


def build(kit):
    lc.assert_keyway()
    m = lc.Mesh()
    m.lathe(profile(), SEGS)
    knob = m.to_object(kit, "knob", lc.CHROME)
    lc.finish_part(knob, _wear)

    kit.anchor("spindle", lc.U(0.0, 0.0, 0.0))
    kit.anchor("face", lc.U(0.0, 0.0, FACE_Z))
    kit.anchor("axis_out", lc.U(0.0, 0.0, 0.10))
    lc.common_meta(kit, {"type": "rotate", "axis": [0, 0, 1], "min": -40, "max": 40, "rest": 0,
                         "note": "retracts the mortise latch (0.019) at 40 deg; spring return"},
                   LOD_DISTANCES, LOD1_RATIO, LOD2_RATIO, (BUDGET, 1800, 300))
    kit.tag("knob", "lock_moving", "knurled")
    # Key numbers (10_spec §3.1): Ø 0.054 ball, face 0.063 (0.065 proud with
    # the 0.002 escutcheon), 72 flutes on Z 0.040-0.054, shank Ø 0.019.
    assert abs(2 * CREST_R - 0.054) < 1e-9 and abs(FACE_Z + 0.002 - 0.065) < 1e-9
    assert FLUTES == 72 and abs(FLUTE_DEPTH - 0.0006) < 1e-9 and SEGS >= 96
    lc.check(kit, BUDGET, (-BALL_R, -BALL_R, 0.0002), (BALL_R, BALL_R, FACE_Z), tol=0.00005)
