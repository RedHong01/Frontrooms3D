"""Kit_SecurityLetters: the counter's SECURITY letters, extruded from 信息设计's security_letters.svg as delivered
(SIGNS README §2; DEC 15:3x "use them as delivered; do not re-letter"; 61 §8; plan 70 P12).

TeX Gyre Heros Bold, 4 in caps (ink 758.4 x 104.9 mm), tracking +0.06 em, from the SVG's paths (one per letter, mm,
y down, viewBox = the word's ink box; the baseline 1.67 mm above the ink bottom). Each letter is a 6.35 mm (1/4 in)
aluminium plate with square cut edges, pin-mounted 12.7 mm (1/2 in) off the fascia on two Ø 3 mm studs.
ORIGIN = the fascia face at the baseline, centred on the word; front +Z (toward D); reads left to right from the
front. The SVG path is read from W/info_type/stair_signs/out/first_room/security_letters.svg (or the
FR_SECURITY_SVG environment variable): re-extrude only when 信息设计 re-bakes; never re-letter.
Slots Prop_Aluminium (satin brushed, horizontal grain from the metre UVs). No collider.
"""

import os
import re

import bpy

import _station_common as sc

NAME = "Kit_SecurityLetters"
LODS = (0.45, 0.12)
SMOOTH_ANGLE = 30.0

SVG = os.environ.get("FR_SECURITY_SVG", "/Users/redwang/FrontRoomsVisualWork/info_type/stair_signs/out/first_room/security_letters.svg")
THICK, STANDOFF = 0.00635, 0.0127
BASELINE_FROM_TOP_MM = None  # computed: viewBox height - 1.6724


def parse(d):
    """Absolute M/L/C/Z path data -> list of subpaths; each a list of segments ('L', p) or ('C', c1, c2, p)."""
    toks = re.findall(r"[MLCZ]|-?\d+(?:\.\d+)?", d)
    subs, cur, i, cmd, start = [], None, 0, None, None
    while i < len(toks):
        t = toks[i]
        if t in "MLCZ":
            cmd = t
            i += 1
            if cmd == "Z":
                if cur:
                    subs.append(cur)
                cur = None
            continue
        if cmd == "M":
            p = (float(toks[i]), float(toks[i + 1])); i += 2
            cur = [("M", p)]
            cmd = "L"
        elif cmd == "L":
            p = (float(toks[i]), float(toks[i + 1])); i += 2
            cur.append(("L", p))
        elif cmd == "C":
            c1 = (float(toks[i]), float(toks[i + 1])); c2 = (float(toks[i + 2]), float(toks[i + 3])); p = (float(toks[i + 4]), float(toks[i + 5])); i += 6
            cur.append(("C", c1, c2, p))
    return subs


def build(kit):
    sc.register()
    s = open(SVG).read()
    vb = [float(v) for v in re.search(r'viewBox="([^"]+)"', s).group(1).split()]
    width_mm, height_mm = vb[2], vb[3]
    base_mm = height_mm - 1.6724279835390945
    paths = re.findall(r'<path id="(\w+)" d="([^"]+)"', s)
    curve = bpy.data.curves.new("security", "CURVE")
    curve.dimensions = "2D"
    curve.fill_mode = "BOTH"
    curve.resolution_u = 10
    curve.extrude = THICK / 2
    curve.bevel_depth = 0.0

    def P(p):
        # SVG mm (x right, y down) -> curve space metres (Blender x = (x - W/2), y = height above the baseline):
        # the curve is turned onto the Blender XZ plane below, so it reads left to right from the front (-Y Blender).
        return ((p[0] - width_mm / 2) / 1000.0, (base_mm - p[1]) / 1000.0, 0.0)

    letters = []
    for pid, d in paths:
        for sub in parse(d):
            pts = []  # (co, handle_left, handle_right)
            prev = None
            for seg in sub:
                if seg[0] == "M":
                    pts.append([P(seg[1]), None, None])
                elif seg[0] == "L":
                    pts.append([P(seg[1]), None, None])
                else:
                    pts[-1][2] = P(seg[1])
                    pts.append([P(seg[3]), P(seg[2]), None])
            # Closing: the last point repeats the first in these SVGs (C to the start) or not (L paths).
            if len(pts) > 1 and abs(pts[-1][0][0] - pts[0][0][0]) < 1e-6 and abs(pts[-1][0][1] - pts[0][0][1]) < 1e-6:
                pts[0][1] = pts[-1][1]
                pts.pop()
            sp = curve.splines.new("BEZIER")
            sp.bezier_points.add(len(pts) - 1)
            for bp, (co, hl, hr) in zip(sp.bezier_points, pts):
                bp.co = co
                bp.handle_left_type = bp.handle_right_type = "FREE"
                bp.handle_left = hl if hl is not None else co
                bp.handle_right = hr if hr is not None else co
            sp.use_cyclic_u = True
        letters.append(pid)
    obj = bpy.data.objects.new("security letters", curve)
    bpy.context.scene.collection.objects.link(obj)
    dg = bpy.context.evaluated_depsgraph_get()
    mesh = bpy.data.meshes.new_from_object(obj.evaluated_get(dg))
    bpy.data.objects.remove(obj, do_unlink=True)
    mo = bpy.data.objects.new("letters", mesh)
    bpy.context.scene.collection.objects.link(mo)
    mesh.materials.append(kit._material("Prop_Aluminium"))
    # Curve +Z (the letter face) -> Blender -Y (Unity +Z, toward the room); curve +Y -> Blender +Z (up).
    import math
    mo.rotation_mode = "XYZ"
    mo.rotation_euler = (math.radians(90), 0, 0)
    mo.location = sc.B(0.0, 0.0, STANDOFF + THICK / 2)
    kit.adopt(mo, ["Prop_Aluminium"], uv="metres")
    # Two Ø 3 mm studs per letter (centred on its ink box), hidden behind the plate.
    for m in re.finditer(r'"letter": "(\w)"', open(os.path.join(os.path.dirname(SVG), "layout.json")).read()):
        pass
    xs = [(-width_mm / 2 + 42) / 1000.0, (-width_mm / 2 + 146) / 1000.0, (-width_mm / 2 + 249) / 1000.0, (-width_mm / 2 + 358) / 1000.0,
          (-width_mm / 2 + 462) / 1000.0, (-width_mm / 2 + 548) / 1000.0, (-width_mm / 2 + 625) / 1000.0, (-width_mm / 2 + 715) / 1000.0]
    for bx in xs:
        for hy in (0.025, 0.075):
            kit.cylinder(0.0015, STANDOFF, (bx, -STANDOFF / 2, hy), "Prop_Aluminium", verts=8, rot=(90, 0, 0), bevel=0.0, name="stud")
    kit.no_collider()
    kit.tag("station", "sign")
    kit.meta["letters"] = {"svg": os.path.basename(SVG), "paths": letters, "thicknessM": THICK, "standoffM": STANDOFF,
                           "source": "信息设计 first_room/security_letters.svg, as delivered (never re-lettered)"}
    sc.auto_lod_drops(kit)
    sc.lods(kit, 6.0, 14.0, 60.0, (1500, 500, 0))
    sc.station_meta(kit, "fascia face at the baseline, centred on the word", "")
