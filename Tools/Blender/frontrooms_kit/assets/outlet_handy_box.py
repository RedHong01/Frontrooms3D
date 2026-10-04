"""Surface "handy box" with a galvanized raised cover and a duplex
receptacle, fed from above by 1/2 in EMT: the retrofit outlet of tall halls,
warehouse walls and column faces where wires could not be fished.

Real-world reference: drawn-steel utility ("handy") box 4 x 2-1/8 x 1-7/8 in
(101.6 x 54.0 x 47.6 mm), galvanized duplex-receptacle cover, 1/2 in EMT
set-screw connector in the top knockout (01_period_research §6.4; handy
boxes and EMT are unchanged since the 1950s, so the kit fits 1990 and older
second-hand work alike).

Construction (mm, Blender: x right, y out of the wall = negative, z up):
* Box: rounded-corner drawn shell 54.0 x 101.6 (corner R 6), 47.6 deep,
  back on the wall plane y = 0 (nothing behind it). One unused 1/2 in
  knockout slug (Ø 22.2, 0.3 proud) centred on each long side.
* Cover: 0.8 mm sheet, 55.0 x 102.6 (0.5 mm lip over the box; real covers
  run ~58.7 x 104.8, trimmed to keep the spec envelope), corner R 6.5,
  a 2.2 mm flat rim, then a pressed ramp to a field 2.8 mm above the box
  front ("raised cover"). Two punched duplex openings (§1.2: 33.3 round with
  flats 14.3, 0.4 mm clearance, 0.4 mm rounded mouth) at z = +-19.45.
* Device: the §1.2 5-15R faces, ground down, 1.0 proud of the field, 7 mm
  black slots with brass contacts, device colour 1.5 mm under the field.
* Screws: three slotted pan heads (two cover screws into the box ears at
  z = +-41.65, one device screw at the centre); baked into the mesh.
* EMT offset connector on the top knockout: die-cast hex collar
  (25.4 across flats) on the box top at the knockout axis (y = -23.8, the
  box mid-depth), an S-neck that steps the axis to the wall line
  (y = -11.5 = E_AXIS) over 24 mm, and an 18 mm socket (OD 22.6) with a
  slotted set screw on its front boss. Offset connectors (and box offsets
  bent in the EMT) are how surface conduit reaches a box that stands proud
  of the wall.

ORIGIN = THE WALL POINT AT THE BOX CENTRE (box back on y = 0). FRONT = kit
-Y (Unity +Z). Anchor ``conduit_base`` = (0, 0, 0.0926) on the wall plane:
place a Kit_ConduitEMT tile there (same frame) and its tube starts 4 mm
inside the socket mouth, on the socket axis; R3 stacks tiles from there to
the ceiling. Anchors face_top / face_bottom (face centres) and plate_top.

Budget (10_spec §1.3): 3,400 / 1,300 / 80 tris, LOD_DISTANCES 2 / 6 / 20 m.
Slots: Prop_Aluminium (galvanized box, cover, connector, screws; submesh
0), Prop_NylonIvory, Prop_PlasticBlack, Prop_Brass. Render-only. The box is
52 mm proud: the planner must keep furniture off HandyBox faces (spec §2.9
assumes plates <= 7.5 mm). Era: no text, logos, dates or UL marks.
"""

import math

import outlet_box_common as B

NAME = "Kit_OutletHandyBox"
LOD1 = None
LOD1_RATIO = 1300 / 3400.0
LOD2_RATIO = 80 / 3400.0
LOD_DISTANCES = (2.0, 6.0, 20.0)
BUDGET = (3400, 1300, 80)
SMOOTH_ANGLE = 35.0

AL, NI = B.AL, B.NI
MM = B.MM

BOX_W, BOX_H, BOX_D, BOX_R = 54.0, 101.6, 47.6, 6.0       # BOX_R ESTIMATE
COVER_W, COVER_H, COVER_R = 55.0, 102.6, 6.5               # ESTIMATE (see docstring)
COVER_PROFILE = [(0.0, 0.0), (0.0, 0.45), (0.35, 0.8), (2.2, 0.8), (3.6, 2.0), (5.0, 2.8)]   # ESTIMATE
FIELD = COVER_PROFILE[-1][1]                               # 2.8 above the box front
EAR_Z = 41.65                                              # cover screws (3-9/32 in centres)
KO_Y = -BOX_D / 2                                          # knockout axis, box mid-depth
HEX_AF, HEX_H = 25.4, 4.8
NECK_R, NECK_L = 9.5, 24.0                                 # ESTIMATE
SOCKET_R, SOCKET_L = 11.3, 18.0                            # ESTIMATE
TUBE_IN = 4.0                                              # tile starts this far inside the socket
INNER_R = B.EMT_OD / 2 - 0.1                               # socket lip inner radius (under the tube skin)
KO_R, KO_PROUD, KO_SEGS = 11.1, 0.3, 20                    # 1/2 in knockout slug (7/8 in hole); proud ESTIMATE


def knockout(mb, fr, segs=KO_SEGS):
    """Unused 1/2 in knockout on a box side: the punched slug (Ø 22.2) stands
    KO_PROUD out of the side, so its sheared rim catches the light. fr origin
    = slug centre on the side plane, n out of the box."""
    r0 = mb.ring(fr, B.circle(KO_R, segs), 0.0)
    r1 = mb.ring(fr, B.circle(KO_R, segs), KO_PROUD)
    mb.band(r0, r1, AL)
    mb.cap(r1, AL, front=True)


def _connector_axis(t):
    """Neck axis y (mm) at t in [0, 1]: a cosine S from the knockout axis to
    the wall line."""
    return KO_Y + (-B.E_AXIS - KO_Y) * (1 - math.cos(math.pi * t)) / 2


def build(kit):
    top = BOX_H / 2
    zh0 = top                                    # hex collar on the box top
    zn0 = zh0 + HEX_H                            # neck starts
    zs0 = zn0 + NECK_L - 1.0                     # socket bottom (laps the neck end)
    zs1 = zs0 + SOCKET_L                         # socket mouth
    base_z = zs1 - TUBE_IN                       # conduit_base
    cover = B.front_frame(0, -BOX_D * MM, 0)

    def wear(p, n, slot):
        x, y, z = p                              # Unity mm
        r, g = 1.0, 1.0
        if n[2] > 0.9 and z > BOX_D + 1.0 and abs(x) < 16.65 + 9 and abs(y) < 33.75 + 9:
            r = 0.85                             # fingers round the faces
        if slot == AL and BOX_D - 0.1 < z < BOX_D + FIELD and n[2] < 0.85:
            g = 0.8                              # cover lip and pressed ramp
        if slot == AL and zh0 - 0.1 < y < zn0 + 0.1 and abs(n[1]) < 0.9:
            g = 0.85                             # hex collar flats
        return (r, g, 1.0)

    # LOD0 --------------------------------------------------------------------
    mb = B.MB()                                  # galvanized box (dominant slot first)
    B.profile_stack(mb, B.front_frame(0, 0, 0), lambda d: B.rrect(BOX_W, BOX_H, BOX_R, 8),
                    [(0.0, 0.0), (0.0, BOX_D)], AL)
    mb.to_part(kit, "box", "0")

    mb = B.MB()                                  # raised cover with the two openings
    rings = B.profile_stack(mb, cover, lambda d: B.rrect(COVER_W - 2 * d, COVER_H - 2 * d, COVER_R - d, 6),
                            COVER_PROFILE, AL)
    holes = [B.hole_stack(mb, cover, fn, B.mouth_profile(0.4, 2, FIELD, FIELD - 0.8), AL)[0]
             for fn in B.duplex_openings(sideways=False)]
    mb.fill(rings[-1], holes, AL, cover.n)
    mb.to_part(kit, "cover", "0")

    mb = B.MB()                                  # receptacle
    B.duplex(mb, cover, FIELD, "hero", sideways=False)
    su, sv = B.duplex_span(False)
    B.backing(mb, cover, 2 * su + 3.0, 2 * sv + 3.0, 3.0, FIELD - B.BACK_PLANE)
    mb.to_part(kit, "device", "0")

    for k, (v, deg) in enumerate(((EAR_Z, 12.0), (0.0, 64.0), (-EAR_Z, 101.0))):
        B.screw_pan(kit, cover.at(0.0, v, FIELD), deg, segs=24, head_d=6.6, head_h=1.9, name="screw_%d" % k)

    mb = B.MB()                                  # EMT offset connector
    hexf = B.up_frame(0, KO_Y * MM, zh0 * MM)
    r0 = mb.ring(hexf, B.hexagon(HEX_AF), 0.0)
    r1 = mb.ring(hexf, B.hexagon(HEX_AF), HEX_H - 0.6)
    r2 = mb.ring(hexf, B.hexagon(HEX_AF - 1.2), HEX_H)
    mb.band(r0, r1, AL)
    mb.band(r1, r2, AL)
    mb.cap(r2, AL, front=True)
    path = [(0.0, KO_Y * MM, (zn0 - 0.8) * MM)]
    for k in range(6):
        t = k / 5.0
        path.append((0.0, _connector_axis(t) * MM, (zn0 + NECK_L * t) * MM))
    B.sweep(mb, path, [NECK_R * MM] * len(path), 20, AL)
    sock = B.up_frame(0, -B.E_AXIS * MM, 0)
    rs = [mb.ring(sock, B.circle(SOCKET_R, 24), zs0), mb.ring(sock, B.circle(SOCKET_R, 24), zs1 - 0.6),
          mb.ring(sock, B.circle(SOCKET_R - 0.6, 24), zs1), mb.ring(sock, B.circle(INNER_R, 24), zs1)]
    for a, b in zip(rs, rs[1:]):
        mb.band(a, b, AL)
    mb.cap(rs[0], AL, front=False)
    zb = (zs0 + zs1) / 2                         # set-screw boss on the socket front
    boss = B.front_frame(0, -(B.E_AXIS + SOCKET_R - 1.3) * MM, zb * MM)
    b0 = mb.ring(boss, B.circle(4.0, 12), 0.0)
    b1 = mb.ring(boss, B.circle(4.0, 12), 3.6)
    mb.band(b0, b1, AL)
    mb.cap(b1, AL, front=True)
    mb.to_part(kit, "connector", "0")
    B.screw_pan(kit, boss.at(0.0, 0.0, 3.6), 47.0, segs=16, head_d=5.6, head_h=1.5, slot_w=0.8, slot_depth=0.6,
                name="set_screw")

    # Unused knockouts, one centred on each long side (x = +-27, mid-depth).
    mb = B.MB()
    knockout(mb, B.Frame((BOX_W / 2 * MM, KO_Y * MM, 0.0), (0, 1, 0), (0, 0, 1), (1, 0, 0)))
    knockout(mb, B.Frame((-BOX_W / 2 * MM, KO_Y * MM, 0.0), (0, -1, 0), (0, 0, 1), (-1, 0, 0)))
    mb.to_part(kit, "knockouts", "0")

    # LOD1 / LOD2 (hand-built; only when kitlib can export them) ----------------
    if B.lods_enabled():
        mb = B.MB()
        B.profile_stack(mb, B.front_frame(0, 0, 0), lambda d: B.rrect(BOX_W, BOX_H, BOX_R, 4),
                        [(0.0, 0.0), (0.0, BOX_D)], AL)
        rings = B.profile_stack(mb, cover, lambda d: B.rrect(COVER_W - 2 * d, COVER_H - 2 * d, COVER_R - d, 4),
                                [(0.0, 0.0), (0.0, 0.8), (2.2, 0.8), (5.0, FIELD)], AL)
        holes = [B.hole_stack(mb, cover, fn, [(0.3, FIELD), (0.0, FIELD - 0.3), (0.0, FIELD - 0.8)], AL)[0]
                 for fn in B.duplex_openings(sideways=False, pc=32)]
        mb.fill(rings[-1], holes, AL, cover.n)
        B.duplex(mb, cover, FIELD, "mid", sideways=False)
        B.backing(mb, cover, 2 * su + 3.0, 2 * sv + 3.0, 3.0, FIELD - B.BACK_PLANE, per_corner=1)
        for v in (EAR_Z, 0.0, -EAR_Z):
            B.dome(mb, cover.at(0.0, v, FIELD), 3.3, 1.6, 16)
        r0 = mb.ring(hexf, B.hexagon(HEX_AF), 0.0)
        r1 = mb.ring(hexf, B.hexagon(HEX_AF), HEX_H)
        mb.band(r0, r1, AL)
        mb.cap(r1, AL)
        B.sweep(mb, path[1:], [NECK_R * MM] * (len(path) - 1), 12, AL)
        rs = [mb.ring(sock, B.circle(SOCKET_R, 16), zs0), mb.ring(sock, B.circle(SOCKET_R, 16), zs1),
              mb.ring(sock, B.circle(SOCKET_R - 0.6, 16), zs1 + 0.0), mb.ring(sock, B.circle(INNER_R, 16), zs1)]
        for a_, b_ in zip(rs, rs[1:]):
            mb.band(a_, b_, AL)
        mb.cap(rs[0], AL, front=False)
        b0 = mb.ring(boss, B.circle(4.0, 8), 0.0)
        b1 = mb.ring(boss, B.circle(4.0, 8), 3.6)
        mb.band(b0, b1, AL)
        mb.cap(b1, AL, front=True)
        mb.to_part(kit, "lod1", "1")
        mb = B.MB()
        rings = B.profile_stack(mb, B.front_frame(0, 0, 0), lambda d: B.rrect(BOX_W, BOX_H, BOX_R, 2),
                                [(0.0, 0.0), (0.0, BOX_D + FIELD)], AL)
        mb.cap(rings[-1], AL)
        B.duplex_lod2(mb, cover, FIELD, sideways=False)
        B.sweep(mb, [path[0], path[3], (0.0, -B.E_AXIS * MM, zs1 * MM)], [SOCKET_R * MM] * 3, 8, AL)
        mb.to_part(kit, "lod2", "2")

    B.finalize(kit, SMOOTH_ANGLE, wear)

    faces = B.face_centres(cover, FIELD, False)
    kit.anchor("conduit_base", (0.0, 0.0, base_z * MM))
    kit.anchor("face_top", tuple(faces[0]))
    kit.anchor("face_bottom", tuple(faces[1]))
    kit.anchor("plate_top", tuple(cover.p(0.0, COVER_H / 2, 0.8)))
    import sys
    module = sys.modules[__name__]
    _, lo, hi = B.box_meta(kit, module, "outlet_surface", COVER_W, COVER_H, wall=True,
                           outlet_extra={"box": [BOX_W * MM, BOX_H * MM, BOX_D * MM],
                                         "conduitBase": base_z * MM, "conduitAxis": B.E_AXIS * MM})
    # Self-checks: the box shell envelope (spec 54 x 102 x 48, +-1 mm), the
    # connector socket on the conduit axis, nothing behind the wall.
    box = [o for o in kit.parts if o.name == "box"][0]
    blo, bhi = B.bounds_mm([box])
    assert abs((bhi[0] - blo[0]) - 54.0) <= 1.0 and abs((bhi[2] - blo[2]) - 102.0) <= 1.0 and abs(-blo[1] - 48.0) <= 1.0, \
        "handy box shell %.2f x %.2f x %.2f" % (bhi[0] - blo[0], bhi[2] - blo[2], -blo[1])
    assert abs(SOCKET_R - B.E_AXIS) < 0.25, "socket must clear the wall (back at %.2f mm)" % (SOCKET_R - B.E_AXIS)
    # The lip polygon's corners (INNER_R) must lie inside the tube polygon's
    # flats (EMT radius x cos(pi / 48)), so no crack can open at the mouth.
    assert INNER_R < B.EMT_OD / 2 * math.cos(math.pi / 48), "socket lip must sit under the tube skin"
    print("[o3] %s box %.2f x %.2f x %.2f mm; overall x %.2f..%.2f, proud %.2f, z %.2f..%.2f; conduit_base z %.2f" % (
        kit.name, bhi[0] - blo[0], bhi[2] - blo[2], -blo[1], lo[0], hi[0], -lo[1], lo[2], hi[2], base_z))
