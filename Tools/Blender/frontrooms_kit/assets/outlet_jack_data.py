"""IBM Data Connector (IBM Cabling System, Token Ring, 1984) on a 1-gang
ivory thermoset plate: the Office data outlet. 1/3 of Office phone plates
get a data plate 0.30 m further on; the zone hash picks IBM or BNC once per
zone so a zone reads as one network (10_spec §2.11, P2).

Real reference: the IBM hermaphroditic data connector, square, black with
a beige contact carrier, "at least 3 cm x 3 cm" of plate space, standing
well proud of a 1-gang plate (outlets 01 §5.2, S27). Face geometry here is
a reading, not a measurement: a 32 x 32 black housing on a 35 x 35 flange,
a 1.2 mm rim round a sunk face; on the face, a beige contact block in the
upper half (standing 2.8 mm past the rim, four brass leaf contacts on its
lower side) and a matching dark recess in the lower half, so two
connectors mate face to face turned 180 degrees; a shell seam and grip
ribs on the latch sides. UNVERIFIED against a photo: media_candidates.md
om08 (IBM_hermaphroditic_connector.JPG) is the check.

Size 69.8 x 114.3 x 24.3 mm (limit 25). ORIGIN = the wall-face point at the
plate centre; plate back on y = 0; front -Y (Unity +Z). Anchors screw_0
(top), screw_1 (bottom) on the blank-plate spacing (83.3 mm), plate_top.

Budget (10_spec §1.3) LOD0 2,800 / LOD1 1,000 / LOD2 60, 1.5 / 4 / 12 m;
LOD1/LOD2 hand-built, exported once make_lods exists (P-1/P-1b).
Slots: Prop_ThermosetIvory (plate), Prop_PlasticBlack (housing),
Prop_NylonIvory (contact block, beige), Prop_Brass (contacts). Render-only.

Era fit: IBM Cabling System 1984, current in a 1990 office. No RJ45, no
"CAT"/TIA marks, no IBM logo or any text, no dates (22_era_lock.md).
"""

import sys

import outlet_plate_common as pc

NAME = "Kit_JackData"
LOD1 = None
BUDGET = (2800, 1000, 60)
LOD1_RATIO = BUDGET[1] / BUDGET[0]
LOD2_RATIO = BUDGET[2] / BUDGET[0]
LOD_DISTANCES = pc.LOD_DISTANCES
SMOOTH_ANGLE = pc.SMOOTH_ANGLE

# ------------------------------------------------ O2 design numbers (mm)
HOUSING = 32.0            # spec §1.3: 32 x 32
HOUSING_R = 2.0
HSEGS = 6                 # per housing corner (r 2: sagitta 0.017)
FLANGE = 35.0
FLANGE_R = 2.5
FLANGE_H = 1.2            # above the field
FLANGE_ROUND = 0.4
OPENING = 33.0            # plate opening under the flange (hidden)
FRONT = 21.5              # housing rim
RIM_ROUND = 0.8
RIM_INSET = 2.0           # rim round + 1.2 flat
FLOOR_DEPTH = 1.5
SEAM = (12.2, 13.0, 0.4)  # shell seam: h from, h to, depth
BLOCK = (0.0, 6.0, 11.0, 5.0, 0.8)    # cx, cz, hw, hh, r: z +1.0 .. +11.0
BLOCK_FRONT = 24.3
BLOCK_ROUND = 0.4
RECESS = (0.0, -6.0, 11.4, 5.4, 0.8)  # z -11.4 .. -0.6
RECESS_BOTTOM = 12.0
BSEGS = 3
CONTACT_X = (-6.0, -2.0, 2.0, 6.0)
CONTACT_W, CONTACT_T = 1.6, 0.35
RIB_X, RIB_PROUD = 16.0, 0.6
RIB_H = (15.0, 19.0)
RIB_Z = (-4.0, 0.0, 4.0)
RIB_W = 0.8


def housing(kit, f0):
    """Flange, housing walls with the shell seam, front rim, sunk face with
    the recess (all Prop_PlasticBlack)."""
    m = pc.Mesh()
    fl = pc.rr_outline(0.0, 0.0, FLANGE / 2, FLANGE / 2, FLANGE_R, HSEGS)
    ho = pc.rr_outline(0.0, 0.0, HOUSING / 2, HOUSING / 2, HOUSING_R, HSEGS, rmin=0.5)
    ftop = f0 + 0.15 + FLANGE_H
    frings = m.body(fl, pc.convex_walk(f0, ftop, 0.0, FLANGE_ROUND, 2))
    walk = [(0.0, ftop), (0.0, SEAM[0]), (SEAM[2], SEAM[0] + 0.15), (SEAM[2], SEAM[1] - 0.15), (0.0, SEAM[1])]
    walk += pc.convex_walk(SEAM[1], FRONT, 0.0, RIM_ROUND, 3)[1:]
    walk += [(RIM_INSET, FRONT), (RIM_INSET, FRONT - FLOOR_DEPTH)]
    hrings = m.body(ho, walk)
    m.bridge(frings[-1], hrings[0])                  # flange top to the housing wall
    floor_ring = hrings[-1]
    bx, bz, bw, bh, br = BLOCK
    rx, rz, rw, rh, rr = RECESS
    floor_h = FRONT - FLOOR_DEPTH
    block_hole = m.ring(pc.rrect(bx, bz, bw, bh, br, BSEGS), floor_h)
    rec_top = m.ring(pc.rrect(rx, rz, rw, rh, rr, BSEGS), floor_h)
    m.fill(floor_ring, [block_hole, rec_top])
    rec_bot = m.ring(pc.rrect(rx, rz, rw, rh, rr, BSEGS), RECESS_BOTTOM)
    m.bridge(rec_top, rec_bot)
    m.cap(rec_bot)
    # grip ribs on the two latch sides
    for sx in (1.0, -1.0):
        for z in RIB_Z:
            x0, x1 = sorted((sx * RIB_X, sx * (RIB_X + RIB_PROUD)))
            ring0 = [m.add(x1, RIB_H[0], z - RIB_W / 2), m.add(x1, RIB_H[0], z + RIB_W / 2),
                     m.add(x0, RIB_H[0], z + RIB_W / 2), m.add(x0, RIB_H[0], z - RIB_W / 2)]
            ring1 = [m.add(x1, RIB_H[1], z - RIB_W / 2), m.add(x1, RIB_H[1], z + RIB_W / 2),
                     m.add(x0, RIB_H[1], z + RIB_W / 2), m.add(x0, RIB_H[1], z - RIB_W / 2)]
            m.bridge(ring0, ring1)
            m.cap(ring1)
            m.face_out([ring0[0], ring0[1], ring0[2], ring0[3]], (0.0, -1.0, 0.0))
    wear = lambda p, n, s: (0.85 if p[1] > 17.0 else 1.0, 0.9 if p[1] > FRONT - 0.5 and abs(p[0]) > 14.0 else 1.0, 1.0)
    return m.to_object(kit, "data housing", pc.PB, wear=wear)


def contact_block(kit):
    bx, bz, bw, bh, br = BLOCK
    m = pc.Mesh()
    ol = pc.rr_outline(bx, bz, bw, bh, br, BSEGS, rmin=0.2)
    rings = m.body(ol, pc.convex_walk(FRONT - FLOOR_DEPTH, BLOCK_FRONT, 0.0, BLOCK_ROUND, 3))
    support = m.ring(ol(BLOCK_ROUND + 0.6), BLOCK_FRONT)       # support ring: keeps the cap flat-shaded
    m.bridge(rings[-1], support)
    m.cap(support)
    return m.to_object(kit, "data contact block", pc.NI, wear=pc.uniform_wear(0.9, 1.0, 1.0))


def contacts(kit):
    """Four leaf contacts lying on the block's lower face (z = +1.0) and
    curling round its front edge; four pads on the recess ceiling."""
    zb = BLOCK[1] - BLOCK[3]
    m = pc.Mesh()
    t = CONTACT_T / 2
    for x in CONTACT_X:
        m.strip([(x, FRONT - FLOOR_DEPTH + 0.6, zb - t), (x, BLOCK_FRONT - 0.55, zb - t),
                 (x, BLOCK_FRONT - 0.12, zb + 0.25), (x, BLOCK_FRONT + 0.0, zb + 0.9)], CONTACT_W, CONTACT_T)
    zc = RECESS[1] + RECESS[3] - 0.02
    for x in CONTACT_X:
        q = [m.add(x - CONTACT_W / 2, 14.0, zc), m.add(x + CONTACT_W / 2, 14.0, zc),
             m.add(x + CONTACT_W / 2, 19.5, zc), m.add(x - CONTACT_W / 2, 19.5, zc)]
        m.face_out(q, (0.0, 0.0, -1.0))
    return m.to_object(kit, "data contacts", pc.BR)


def lod1(kit, f0):
    """LOD1 (1.5-4 m): housing as a 2-segment-corner box with the flange,
    the rim and face as flat insets; block as a box; no contacts or ribs."""
    m = pc.Mesh()
    fl = pc.rr_outline(0.0, 0.0, FLANGE / 2, FLANGE / 2, FLANGE_R, 6)        # §1.1 LOD1: 24-segment faces
    ho = pc.rr_outline(0.0, 0.0, HOUSING / 2, HOUSING / 2, HOUSING_R, 6, rmin=0.5)
    ftop = f0 + 0.15 + FLANGE_H
    fr = m.body(fl, [(0.0, f0), (0.0, ftop)])
    hr = m.body(ho, [(0.0, ftop), (0.0, SEAM[0]), (SEAM[2], 12.6), (0.0, SEAM[1]), (0.0, FRONT - 0.5), (0.5, FRONT), (RIM_INSET, FRONT), (RIM_INSET, FRONT - FLOOR_DEPTH)])
    m.bridge(fr[-1], hr[0])
    bx, bz, bw, bh, br = BLOCK
    rx, rz, rw, rh, rr = RECESS
    bh_ring = m.ring(pc.rrect(bx, bz, bw, bh, br, 0), FRONT - FLOOR_DEPTH)
    rec = m.ring(pc.rrect(rx, rz, rw, rh, rr, 0), FRONT - FLOOR_DEPTH)
    m.fill(hr[-1], [bh_ring, rec])
    rb = m.ring(pc.rrect(rx, rz, rw, rh, rr, 0), FRONT - FLOOR_DEPTH - 2.0)
    m.bridge(rec, rb)
    m.cap(rb)
    out = [m.to_object(kit, "data housing lod1", pc.PB, lods="1")]
    b = pc.Mesh()
    r0 = b.ring(pc.rrect(bx, bz, bw, bh, br, 1), FRONT - FLOOR_DEPTH)
    r1 = b.ring(pc.rrect(bx, bz, bw, bh, br, 1), BLOCK_FRONT)
    b.bridge(r0, r1)
    b.cap(r1)
    out.append(b.to_object(kit, "data block lod1", pc.NI, lods="1"))
    return out


def lod2(kit, f0):
    """LOD2 (4-12 m): the housing as a box with a chamfered front ring,
    the face as one dark quad and the block as one beige quad."""
    m = pc.Mesh()
    oc = lambda hw, c: pc.oct_outline(hw, hw, c)
    r0 = m.ring(oc(FLANGE / 2, 1.0), f0)
    r2 = m.ring(oc(HOUSING / 2, 1.0), FRONT - 0.6)
    r3 = m.ring(oc(HOUSING / 2 - 1.0, 1.0), FRONT)
    m.chain([r0, r2, r3])
    m.cap(r3)
    out = [m.to_object(kit, "data housing lod2", pc.PB, lods="2")]
    bx, bz, bw, bh, br = BLOCK
    out.append(pc.dark_rect(kit, bx, bz, bw, bh, FRONT + 0.05, "data block lod2", "2", slot=pc.NI))
    return out


def build(kit):
    pc.begin(kit)
    W, H = pc.PLATE_1G
    op = pc.Opening("rrect", 0.0, 0.0, OPENING, OPENING, 2.0, round_=False)
    screws = [(0.0, pc.BLANK_SCREW_Z), (0.0, -pc.BLANK_SCREW_Z)]
    info = pc.thermoset_plate(kit, W, H, screws, [op], wear=pc.plate_wear(W, H, [(0.0, 0.0, FLANGE / 2, FLANGE / 2)], margin=8.0))
    f0 = info["hf"](0.0, 0.0) - 0.15        # the flange sinks 0.15 into the crown: no gap at its edge
    assert f0 + 0.15 <= pc.FIELD_CROWN_H + 1e-9 and FLANGE / 2 > OPENING / 2
    housing(kit, f0)
    contact_block(kit)
    contacts(kit)
    if pc.HAS_LODS:
        pc.plate_lod1(kit, W, H, screws, [])
        pc.plate_lod2(kit, W, H, crown_fan=True)
        lod1(kit, f0)
        lod2(kit, f0)
    pc.end(kit, sys.modules[__name__], "outlet_jack", W, H, pc.PROUD_DATA, info["seats"],
           extra={"jack": "IBM data connector", "housing": [HOUSING, HOUSING]})
