"""Stainless casing guards for the title / station pair (zone doors, Build A, 2026-11-06; research/double_doors/20_design.md
Part A, sections A4.2 and A4.3). NOT AN ASSET: door_guard_casing38 (Kit_DoorGuard_Casing38, station D's +Z face, D-3) and
door_guard_casing51 (Kit_DoorGuard_Casing51, the night post's family face, S-2) import it.

The station's door-frame language (E1 / E2, FrontRoomsStationTrim: stainless corner guards 0.10-1.32 m) on a DA pair: both
of the pair's jambs are hinge jambs, so E1's 50 x 50 x 3 mm angle cannot wrap them; the guard is a 1.5 mm #4 satin stainless
sheet formed over the casing's inner arris (its inside radius = the casing's 3 mm outer bend: it sits on the steel, no slit),
the face wing over the casing's band, the side wing down the rise, 1.0 mm clear of the DA barrels in every swing (the shut
barrels reach x 1.2095; in a swing both move toward -x). Ends eased.

Origin: the casing's inner arris (x 1.212, z 0.0331 in the pair's door space) at the guard's mid-height (y 0.71); the +x jamb,
+Z face. The other three placements turn it 180 deg about y and / or z (its ends are symmetric), never a negative scale.
LODs (fr_lods): LOD0 8-step inner bend (Fix A), eased edges (0.6 mm, 2 segments); LOD1 1-step bend, square ends; LOD2 one quad per wing. Switch 2 / 8 m,
culled at 30 m. One slot: Station_StainlessGuard.
"""

import door_pair_common as pc
import interact_door_common as dc
import kitlib

SLOT = "Station_StainlessGuard"
kitlib.register_slot(SLOT, (0.66, 0.66, 0.64), 0.32, 1.0)
MID_Y = (pc.GUARD_Y0 + pc.GUARD_Y1) / 2.0     # 0.71
HALF_L = (pc.GUARD_Y1 - pc.GUARD_Y0) / 2.0    # 0.61


def section(face, side_end, level):
    """Closed (x, z) section in the guard's frame: the inner (contact) path from the side wing's end up the rise, round the
    arris and along the band to its flat end, offset 1.5 mm outward (the visible side)."""
    w_end = face - pc.CONVEX                  # the band's flat ends where the casing's outer bend begins
    path = [(0.0, side_end), (0.0, 0.0), (w_end, 0.0)]
    radii = [0, pc.CONVEX, 0] if level < 2 else [0, 0, 0]
    # Fix A (Review A finding 3: a dotted bright line along the night post guard's bend at 0.35 m): LOD0 bends in 8 steps
    # (11 deg each: the smoothed normals sweep evenly round the arris, one highlight line, not a row of facet glints)
    inner = dc.fillet_polygon(path, radii, 8 if level == 0 else 1, closed=False)
    outer = dc.offset_polyline(inner, pc.GUARD_T, normals_left=True, closed=False)
    return outer + list(reversed(inner))


def grain_uv(obj, stretch=0.25):
    """Fix A (Review A finding 3: a dotted bright line along the guard's bend at 0.35 m): the guard's own UVs, the #4 grain run
    along its length: u = x + z in the guard's frame (the section unfolded across the bend, metres), v = y x stretch (the
    stainless map's fine noise drawn out 4 x along the guard). Box-projected metre UVs broke the lamp's highlight on the bend
    into dots; drawn out along the length it is a brushed streak, as a #4 finish's grain runs on a vertical guard."""
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    layer = bm.loops.layers.uv.verify()
    for f in bm.faces:
        for loop in f.loops:
            x, y, z = dc.to_unity(loop.vert.co)
            loop[layer].uv = (x + z, y * stretch)
    bm.to_mesh(obj.data)
    bm.free()
    obj["fr_uv"] = "keep"


def build_guard(kit, face, side_end, name):
    for level in (0, 1):
        sec = section(face, side_end, level)
        o = dc.loft_y(kit, [(-HALF_L, sec), (HALF_L, sec)], SLOT, name if level == 0 else name + " lod1")
        grain_uv(o)
        if level == 0:
            # the eased ends and wing edges: 0.6 mm in 2 segments (Build A: 0.4 mm in one, a sub-pixel facet that glinted)
            kit._bevel(o, 0.0006, 2, angle=40.0)
        pc.tag(o, str(level))
    # LOD2: one quad per wing, on the visible faces
    import bmesh
    bm = bmesh.new()
    t = pc.GUARD_T
    w_end = face - pc.CONVEX
    face_q = [(-t, -HALF_L, t), (w_end, -HALF_L, t), (w_end, HALF_L, t), (-t, HALF_L, t)]
    side_q = [(-t, -HALF_L, side_end), (-t, -HALF_L, t), (-t, HALF_L, t), (-t, HALF_L, side_end)]
    pc._face(bm, [bm.verts.new(pc.U(*p)) for p in face_q], (0.0, 0.0, 1.0))
    pc._face(bm, [bm.verts.new(pc.U(*p)) for p in side_q], (-1.0, 0.0, 0.0))
    q = dc._part(kit, bm, SLOT, name + " lod2")
    pc.tag(q, "2")
    dc.finalize(kit, 35.0)
    lo, hi = pc.bounds(pc.level_parts(kit, 0))
    # the side wing's outer face 1.0 mm clear of the barrels (x 1.2095 shut): in the guard's frame x >= -(GUARD_T) + ...
    clear = (pc.RISE_X + lo[0]) - pc.BARREL_X_MAX
    assert clear >= 0.001 - 1e-4, "guard %.4f m from the barrels" % clear
    assert abs(hi[1] - HALF_L) < 1e-4 and abs(lo[1] + HALF_L) < 1e-4
    pc.anchor(kit, "arris", 0.0, 0.0, 0.0)
    kit.meta["guardFace"] = face
    kit.meta["guardY"] = [pc.GUARD_Y0, pc.GUARD_Y1]
    kit.meta["barrelClearance"] = round(clear, 5)
    kit.meta["origin"] = "the casing's inner arris (x 1.212, z 0.0331) at mid-height y 0.71; +x jamb, +Z face"
    kit.no_collider()
    kit.tag("door", "guard", "station", "zone_pair")
