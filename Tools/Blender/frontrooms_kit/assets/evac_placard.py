"""Q16 evacuation-plan placard: the snap frame and the printed sheet
(Kit_EvacPlacard). Spec: Documentation/research/placard/10_spec.md §2.

Real-world reference: a 1990 US office's 17 x 11 in evacuation plan (a
copied ANSI B sheet) in a 1 in clear-anodised aluminium snap frame with
tamper-proof bullnose clip rails (MDI US 4,145,828, 1977; US 4,519,152,
1983), four rails mitred 45 deg, a thin non-glare lens, screwed flat to the
wall through the base under the closed rails. No brand, no visible screws.

Outside 466.8 x 313.8 x 13.5 mm; sight opening 416.0 x 263.0; the sheet
(432 x 279, the 平面视觉 artwork) lies at 4.55 mm and the rails' lip noses
rest on the 1.0 mm lens at 5.55. Origin = the wall-face point at the frame
centre; Unity +Z out of the wall (kit front -Y), +Y up; nothing behind z = 0.
The lens is its own asset (evac_placard_lens.py) so it can have its own
renderer, sort order and shadow mode, and WebGL can leave it out.

LOD0 (hero, 0-2 m; holds at 0.3 m): four mitred rail solids with the §2.3
section (48 points: bullnose 16, face 20, nose 8 segments), 0.25 mm mitre
chamfers (a 0.55 mm V seam), the hinge seam as a real 0.40 x 0.35 mm V, the
base strip swept round the rectangle, the sheet quad. LOD1 (2-6 m): one
14-point sweep. LOD2 (6-30 m, culled beyond): one 4-point sweep. LOD1/LOD2
are hand-built and exported once kitlib has make_lods (P-1/P-1b); until then
the FBX holds LOD0 only (interactables convention).

Slots: Prop_Aluminium (existing), Prop_EvacPlan (NEW, the artwork; its glow
mask is the emission map that FrontRoomsPlacardGlow drives). Render-only:
no collider; the spawner turns shadow casting off.
"""

import evac_placard_common as pc
import interact_key_common as kc
from interact_key_common import U

NAME = "Kit_EvacPlacard"
SMOOTH_ANGLE = pc.SMOOTH_ANGLE
LOD1 = None                                   # hand-built LODs (fr_lods), not the collapse decimator
LOD1_RATIO = pc.BUDGET[1] / pc.BUDGET[0]
LOD2_RATIO = pc.BUDGET[2] / pc.BUDGET[0]
LOD_DISTANCES = pc.LOD_DISTANCES
BUDGET = pc.BUDGET

# §2.9 wear (fr_wear: R hand grime / dirt, G edge wear, B cavity; 1 = clean).
DUST_TOP = 0.85            # the top rail's bullnose (outer 20 % of the face): dust settles there
THUMB_BOTTOM = 0.90        # the bottom rail's face, centre +-60 mm: thumbs, from opening it
THUMB_HALF = 60.0


def _wear_fn(side):
    def fn(co):
        X, Y, Z = pc.to_unity_mm(co)
        if side == "top":
            u = pc.B_HALF - Y
            if u <= 0.2 * pc.FACE_W + 1e-6 and Z >= pc.BULL_C[1] - 1e-6:
                return (DUST_TOP, 1.0, 1.0, 1.0)
        if side == "bottom":
            if abs(X) <= THUMB_HALF + 1e-6 and Z >= pc.LENS_TOP + 0.5:
                return (THUMB_BOTTOM, 1.0, 1.0, 1.0)
        return (1.0, 1.0, 1.0, 1.0)
    return fn


def build(kit):
    pc.register_slots()
    builders = []
    # ---- LOD0
    rails = {}
    for side in ("top", "bottom", "left", "right"):
        extra = (-THUMB_HALF, THUMB_HALF) if side == "bottom" else ()
        obj, b = pc.rail_lod0(kit, side, extra_s=extra)
        kc.paint_wear(obj, _wear_fn(side))
        rails[side] = obj
        builders.append((obj.name, b))
    base, b = pc.base_strip(kit, pc.BASE_PROFILE, True, "base strip", "0")
    builders.append((base.name, b))
    sheet = pc.sheet_quad(kit, "012")
    # ---- LOD1 / LOD2 (hand-built; only once kitlib can export them)
    if pc.lods_on():
        l1, b = pc.base_strip(kit, pc.profile_lod1(), False, "frame lod1", "1")
        builders.append((l1.name, b))
        l2, b = pc.base_strip(kit, pc.profile_lod2(), False, "frame lod2", "2")
        builders.append((l2.name, b))
    kc.wear_all(kit)
    pc.finalize(kit)

    # ---- sidecar (§2.6)
    kit.no_collider()
    kit.tag("placard", "sign", "wall_flush")
    M = pc.MM
    kit.anchor("back", U(0.0, 0.0, 0.0))
    kit.anchor("face", U(0.0, 0.0, pc.SHEET_W * M))
    kit.anchor("face_dir", U(0.0, 0.0, pc.SHEET_W * M + 0.10))
    kit.anchor("lens_top", U(0.0, 0.0, pc.LENS_TOP * M))
    x0, x1, y0, y1 = pc.LEGEND_BOX
    lx = pc.SHEET[0] / 2 - (x0 + x1) / 2          # sheet x from the left (viewer) -> Unity X (viewer's right is -X)
    ly = pc.SHEET[1] / 2 - (y0 + y1) / 2
    kit.anchor("legend_centre", U(lx * M, ly * M, pc.SHEET_W * M))
    for i, (sx, sy) in enumerate(((1, 1), (-1, 1), (-1, -1), (1, -1))):
        kit.anchor("screw_%d" % i, U(sx * pc.SCREW_X * M, sy * pc.SCREW_Y * M, pc.SCREW_W * M))
    R, c, ang = pc.face_arc()
    kit.meta["placard"] = {
        "sheet": [pc.SHEET[0] * M, pc.SHEET[1] * M], "sight": [round(pc.SIGHT[0] * M, 5), round(pc.SIGHT[1] * M, 5)],
        "frame": [round(pc.FRAME[0] * M, 5), round(pc.FRAME[1] * M, 5)], "depth": pc.DEPTH * M,
        "sheetZ": pc.SHEET_W * M, "lensTop": pc.LENS_TOP * M,
        "slotPrint": pc.PRINT, "slotFrame": pc.ALU, "lensKit": "Kit_EvacPlacardLens",
        "uvV": [pc.UV_V0, pc.UV_V1], "textureRows": pc.TEX_ROWS,
        "faceArc": {"radiusMm": round(R, 3), "centreMm": [round(c[0], 3), round(c[1], 3)], "noseTangentDeg": round(ang, 2)},
    }
    kc.lod_meta(kit, LOD_DISTANCES, BUDGET)
    kit.meta["lodHandBuilt"] = True
    kit.meta["lodNote"] = ("LOD1/LOD2 are hand-built part sets (fr_lods); exported only once kitlib "
                           "Kit.make_lods exists (P-1/P-1b). Until then this FBX holds LOD0 only.")
    kit.meta["partFrame"] = "origin = wall-face point at the frame centre; front = Unity +Z (kit -Y); +Y up (placard 10_spec §2.1)"
    kit.meta["wear"] = {"attribute": "fr_wear", "topRailDust": DUST_TOP, "bottomRailThumbs": THUMB_BOTTOM, "channel": "R"}

    # ---- asserts (§2.8)
    lod0 = pc.lod_parts(kit, 0)
    lo, hi = pc.bounds_unity_mm(lod0)
    assert abs((hi[0] - lo[0]) - pc.FRAME[0]) < 0.1 and abs((hi[1] - lo[1]) - pc.FRAME[1]) < 0.1, ("frame", lo, hi)
    assert abs(lo[0] + hi[0]) < 0.01 and abs(lo[1] + hi[1]) < 0.01, ("centred", lo, hi)
    assert lo[2] >= -1e-3 and hi[2] <= pc.DEPTH + 0.1 and abs(hi[2] - pc.DEPTH) < 0.01, ("depth", lo, hi)
    # Sight opening = the inner edge of the lip noses; the nose's lowest point rests on the lens.
    sx = sy = 0.0
    nose_low = 1e9
    for side, obj in rails.items():
        for v in obj.data.vertices:
            X, Y, Z = pc.to_unity_mm(obj.matrix_world @ v.co)
            if 5.0 < Z < 8.0:
                if side == "top":
                    sy = max(sy, -(Y - pc.B_HALF))
                elif side == "left":
                    sx = max(sx, X + pc.A_HALF)
            nose_low = min(nose_low, Z) if Z > 5.0 else nose_low
    assert abs(sy - pc.FACE_W) < 0.1 and abs(sx - pc.FACE_W) < 0.1, ("sight inset", sx, sy)
    assert abs(nose_low - pc.LENS_TOP) < 0.02, ("nose low", nose_low)
    assert sheet.data.materials[0].name == pc.PRINT
    for name, b in builders:
        assert b.worst > 0.2, ("face orientation", name, b.worst)
    n0 = pc.tris_of(lod0)
    kit.meta["trianglesCheck"] = n0
    assert abs(n0 - BUDGET[0]) <= 0.15 * BUDGET[0], "%s: LOD0 %d tris vs budget %d" % (kit.name, n0, BUDGET[0])
    counts = [n0]
    for lv in (1, 2):
        parts = [o for o in pc.lod_parts(kit, lv)]
        if parts and pc.lods_on():
            t = pc.tris_of(parts)
            counts.append(t)
            assert abs(t - BUDGET[lv]) <= 0.15 * BUDGET[lv], "%s: LOD%d %d tris vs budget %d" % (kit.name, lv, t, BUDGET[lv])
            l, h = pc.bounds_unity_mm(parts)
            assert abs((h[0] - l[0]) - pc.FRAME[0]) < 0.1 and abs((h[1] - l[1]) - pc.FRAME[1]) < 0.1 and h[2] <= pc.DEPTH + 0.01, ("lod bounds", lv, l, h)
        else:
            counts.append(None)
    kit.meta["placardCheck"] = {"tris": counts, "boundsMm": [lo, hi], "sightInsetMm": [round(sx, 3), round(sy, 3)],
                                "noseLowMm": round(nose_low, 3), "faceFlips": {n: b.flips for n, b in builders}}
    print("[placard] %s LOD0 %d tris (budget %d, %+.1f %%), LODs %s, bounds mm %s..%s, sight inset %.3f/%.3f, nose %.3f"
          % (kit.name, n0, BUDGET[0], 100.0 * (n0 - BUDGET[0]) / BUDGET[0], counts, [round(x, 3) for x in lo],
             [round(x, 3) for x in hi], sx, sy, nose_low))
    for o in kit.parts:
        print("[placard]    %-14s %5d tris  %-18s lods %s" % (o.name, pc.tris_of([o]), o.data.materials[0].name, o.get("fr_lods")))
