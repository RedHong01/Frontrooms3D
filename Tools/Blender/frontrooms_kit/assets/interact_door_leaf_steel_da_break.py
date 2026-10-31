"""Pre-damaged break set of the double-acting HOLLOW-METAL family (production P3, 2026-10-30; research/door_break/50_rethink.md
3.5 steel blow by blow, 3.6 SteelLite, 3.7, 3.8; 54_dust_grit.md 4.3 steel; 52_production.md). Red approved the design
2026-10-30 ("1a，2 要，3 要，4 预算，5 要"): pose (a), kept blows, steel takes 0.5 s longer (6/5/5/4/4 blows, so a dent always
shows before it gives), baked throws (4 per family), the leak card.

A steel leaf never splinters: it dents, bows and opens its edge seam, and stays whole (50 3.5). So unlike the veneer family
(lofted, split, torn) this family is the INTACT kit cut and deformed (interact_door_break_kitcut): the intact
Kit_DoorLeaf_Steel_DA is built here with its own recipe, a window Z 0.60..0.995 x Y 0.50..1.22 is cut out by planes (the top
stays under SteelLite's lite kit at Y 1.229, so both leaves share the cassettes), and the window is refined and deformed per
stage:
  M     the push face's contact dish, 3 mm (the Relay's forearm at Y ~1.08, Z ~0.745), enamel crazing in the onset mask
  D1    the leaf bows 8 mm toward the burst side round the lock; dish 6 mm; the lock edge crushed 1.5 mm (latch gap 4.5 mm)
  D2    bow 15 mm; dish 10 mm; the lock edge crushed 6 mm (latch gap 9 mm, a lit line: the leak card); the EDGE SEAM opens
        below the armor front (Y ~0.60..0.855): the burst skin's lip peels 20 mm and the seam shows the brown kraft
        honeycomb (Door_CoreSteel on the stretched seam walls)
  TORN  bow 18 mm; dish 11 mm; crush 9 mm; the seam open 30 mm, plus a second peel (14 mm) above the armor front
The body outside the window is the intact mesh itself (vertices, UVs, fr_wear colours, kick plates, pivot plates, custom
normals), so the swap frame changes only the window. Every field is zero with zero slope on the window's boundary.

Run (one Blender, headless):
  /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup \
      --python Tools/Blender/frontrooms_kit/assets/interact_door_leaf_steel_da_break.py -- [--out-root <unity project>]

Writes into Assets/Resources/Props/Models/:
  Kit_DoorLeafBreak_Steel_DA          base + family sidecar: body_L0..L2 (Steel_DA minus the window)
  Kit_DoorLeafBreak_Steel_DA_M0..M2   per motif: cas_<M|D1|D2|TORN>_L0..L2, leak_D1, leak_D2
  Kit_DoorLeafBreak_Steel_DA_Deb0..2  per motif: deb_chip_00..09 (enamel flakes off the crushed lock edge), deb_gyp_00..05
                                      (gypsum from the frame anchors behind the strike); readable (the floor merge)
  Kit_DoorLeafBreak_SteelLite_DA      base only: body_L0..L2 (SteelLite_DA minus the same window); its sidecar points at the
                                      Steel family's motifs, debris and throws
Canonical burst toward +X (the push face is -X); the run time mirrors for a -X burst.
"""

import json
import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))

import interact_door_break_common as bc  # noqa: E402

NAME = "Kit_DoorLeafBreak_Steel_DA"
LITE_NAME = "Kit_DoorLeafBreak_SteelLite_DA"
INTACT = "Kit_DoorLeaf_Steel_DA"
HANGS_ON = ["Kit_DoorLeaf_Steel_DA", "Kit_DoorLeaf_Steel_PaintedMetal_DA"]
LITE_HANGS_ON = ["Kit_DoorLeaf_SteelLite_DA"]
MOTIF_SEEDS = (1, 2, 3)
THROWS = 4
STAGES = ("M", "D1", "D2", "TORN")

ENAMEL, CORE_STEEL, GYPSUM, CHROME, ALU, LEAK = "Door_Enamel", "Door_CoreSteel", "Door_CoreGypsum", "Prop_Chrome", "Prop_Aluminium", "Door_LeakCard"
bc.SLOT_COLOURS.update({ENAMEL: (0.61, 0.56, 0.43), CORE_STEEL: (0.45, 0.33, 0.20), GYPSUM: (0.85, 0.84, 0.80), CHROME: (0.7, 0.7, 0.7)})

LEAF_X = bc.LEAF_X
LATCH_Z = bc.LATCH_Z
WIN_Z0, WIN_Y0, WIN_Y1 = 0.600, 0.500, 1.220
LINING_Z = 0.998
ARMOR_Y, ARMOR_H = 0.968, 0.2032
BUDGET = {
    0: {"body": 5524, "cas_M": 6000, "cas_D1": 6000, "cas_D2": 6000, "cas_TORN": 6000, "debris": 1500},
    1: {"body": 1892, "cas_M": 1500, "cas_D1": 1500, "cas_D2": 1500, "cas_TORN": 1500},
    2: {"body": 292, "cas_M": 40, "cas_D1": 40, "cas_D2": 200, "cas_TORN": 200},
}
# per stage: bow (m, toward the burst side), the push-face dish (depth, radius), the lock edge's crush, the seam peel (below,
# above the armor front)
AMP = {
    "M": dict(bow=0.0, dish=0.003, dishR=0.100, crush=0.0, peel=0.0, peel2=0.0),
    "D1": dict(bow=0.008, dish=0.006, dishR=0.105, crush=0.0015, peel=0.0, peel2=0.0),
    "D2": dict(bow=0.015, dish=0.010, dishR=0.110, crush=0.006, peel=0.020, peel2=0.0),
    "TORN": dict(bow=0.018, dish=0.011, dishR=0.110, crush=0.009, peel=0.030, peel2=0.014),
}
CRUSH_Y = (0.72, 1.20)
PALETTES = os.path.join(HERE, "..", "..", "..", "door_break", "palettes.json")


def s3(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3.0 - 2.0 * t)


def bump(y, a, b, pk):
    if y <= a or y >= b:
        return 0.0
    if y <= pk:
        return s3((y - a) / max(1e-9, pk - a))
    return s3((b - y) / max(1e-9, b - pk))


class Motif:
    """One seeded damage layout: where the bow peaks, where the Relay's dish sits, where the lock edge crushes and the seam
    peels. The runtime reads the same numbers from the sidecar (FrontRoomsDoorBreakVisual.SteelField) to move the face
    hardware with the skin."""

    def __init__(self, seed):
        rng = random.Random(500 + seed)
        self.seed = seed
        self.bow_y = 0.97 + rng.uniform(-0.03, 0.03)
        self.bow_z = 0.85 + rng.uniform(-0.03, 0.03)
        self.dish_y = 1.08 + rng.uniform(-0.02, 0.02)
        self.dish_z = 0.745 + rng.uniform(-0.02, 0.02)
        self.crush_pk = 0.97 + rng.uniform(-0.02, 0.02)
        self.peel = (0.60 + rng.uniform(-0.02, 0.03), 0.855, 0.70 + rng.uniform(-0.03, 0.05))
        self.peel2 = (1.085 + rng.uniform(0.0, 0.01), 1.200, 1.14 + rng.uniform(-0.02, 0.02))
        self.k = 1.0 + rng.uniform(-0.10, 0.10)
        # the latch gap's centre line (the slot shaft / dust's "motif"), every 10 mm over the window, at D2
        self.ys = [WIN_Y0 + 0.01 * i for i in range(int(round((WIN_Y1 - WIN_Y0) / 0.01)) + 1)]
        self.zs = [round(0.5 * (LATCH_Z - self.crush(y, "D2") + LINING_Z), 5) for y in self.ys]

    def a(self, st, key):
        return AMP[st][key] * (self.k if key in ("bow", "crush", "peel", "peel2") else 1.0)

    def bow(self, y, z, st):
        A = self.a(st, "bow")
        if A <= 0 or z < WIN_Z0:
            return 0.0
        hz = s3((z - WIN_Z0) / (self.bow_z - WIN_Z0)) if z < self.bow_z else 1.0 - 0.45 * s3((z - self.bow_z) / (LATCH_Z - self.bow_z))
        return A * bump(y, WIN_Y0, WIN_Y1, self.bow_y) * hz

    def dish(self, y, z, st):
        d, R = self.a(st, "dish"), self.a(st, "dishR")
        r2 = ((z - self.dish_z) / R) ** 2 + ((y - self.dish_y) / (1.15 * R)) ** 2
        return d * (1.0 - r2) ** 2 if r2 < 1.0 else 0.0

    def crush(self, y, st):
        return self.a(st, "crush") * bump(y, CRUSH_Y[0], CRUSH_Y[1], self.crush_pk)

    def peel_at(self, y, st):
        return self.a(st, "peel") * bump(y, *self.peel) + self.a(st, "peel2") * bump(y, *self.peel2)

    def displace(self, p, st):
        """The stage's deformed position of an intact window point p (Unity, canonical +X burst)."""
        X, Y, Z = p
        if Y < WIN_Y0 - 1e-6 or Y > WIN_Y1 + 1e-6 or Z < WIN_Z0 - 1e-6:
            return p
        dx = self.bow(Y, Z, st)
        if X < -0.010:
            dx += self.dish(Y, Z, st)
        dz = -self.crush(Y, st) * s3((Z - 0.95) / 0.045)
        pl = self.peel_at(Y, st)
        if X > 0.00005:
            dx += pl * s3((Z - 0.93) / 0.065)
        elif abs(X) <= 0.00005 and Z > 0.990:
            dz -= 0.45 * pl                       # the seam's bottom: the honeycomb torn back between the parted skins
        return (X + dx, Y, Z + dz)


# ===================================================================== the window (Blender: intact kit -> cut -> deform)
def window_inside(c, _f):
    return c[2] > WIN_Z0 and WIN_Y0 < c[1] < WIN_Y1


def grid_values(lod):
    if lod == 0:
        ys = [WIN_Y0 + 0.024 * k for k in range(1, 30)]
        zs = [0.625 + 0.025 * k for k in range(0, 13)] + [0.94, 0.955, 0.967, 0.978, 0.987, 0.991]
    elif lod == 1:
        ys = [WIN_Y0 + 0.045 * k for k in range(1, 16)]
        zs = [0.66, 0.72, 0.78, 0.84, 0.90, 0.94, 0.97, 0.985]
    else:
        ys = [WIN_Y0 + 0.12 * k for k in range(1, 6)]
        zs = [0.72, 0.86, 0.95]
    return ys, zs


def latch_faces(pc):
    """The flat latch end (Unity normal +Z) of the slab in the window."""
    import interact_door_break_kitcut as kc
    out = []
    pc.bm.normal_update()
    for f in pc.faces_of((ENAMEL,)):
        n = f.normal
        if -n.y > 0.9 and kc.centre(f)[2] > 0.990:     # Blender -y = Unity +Z
            out.append(f)
    return out


def components(pc):
    """Connected face groups (by shared vertices): [[faces]]."""
    parent = {}

    def find(a):
        root = a
        while parent.get(root, root) != root:
            root = parent[root]
        while parent.get(a, a) != root:
            parent[a], a = root, parent[a]
        return root
    pc.bm.verts.index_update()
    for f in pc.bm.faces:
        vs = [v.index for v in f.verts]
        for v in vs[1:]:
            ra, rb = find(vs[0]), find(v)
            if ra != rb:
                parent[ra] = rb
    groups = {}
    for f in pc.bm.faces:
        groups.setdefault(find(f.verts[0].index), []).append(f)
    return list(groups.values())


def cassette(win, motif, st, lod):
    """One stage of the window at one LOD from the intact window piece."""
    import interact_door_break_kitcut as kc
    pc = win.copy()
    if lod == 2 and st in ("M", "D1"):
        return pc                                # beyond 12 m a 1 m leaf is under 50 px: the plain window
    ys, zs = grid_values(lod)
    kc.grid(pc, "Y", ys, lambda: pc.faces_of((ENAMEL, CHROME)))
    kc.grid(pc, "Z", zs, lambda: pc.faces_of((ENAMEL,)))
    if lod >= 1:
        # LOD1/2 have no seam groove on the latch end: add its centre line (LOD1 also its 0.3 mm walls), flat at rest
        kc.cut(pc, "X", 0.0, latch_faces(pc))
        if lod == 1:
            kc.cut(pc, "X", 0.00015, latch_faces(pc))
            kc.cut(pc, "X", -0.00015, latch_faces(pc))
    pc.bm.verts.ensure_lookup_table()
    pc.bm.verts.index_update()
    orig = {v.index: kc.to_unity(v.co) for v in pc.bm.verts}
    kc.deform(pc, lambda p, v: motif.displace(p, st))
    # the parted seam's walls show the kraft honeycomb (Door_CoreSteel, U = depth into the leaf, V = along the seam)
    core = pc.slot(CORE_STEEL)
    uv = pc.bm.loops.layers.uv.active
    for f in pc.faces_of((ENAMEL,)):
        o = [orig[v.index] for v in f.verts]
        if max(abs(q[0]) for q in o) > 0.00016 or min(q[2] for q in o) < 0.990:
            continue
        yc = sum(q[1] for q in o) / len(o)
        if motif.peel_at(yc, st) < 0.0004:
            continue
        f.material_index = core
        for loop in f.loops:
            q0 = orig[loop.vert.index]
            pn = kc.to_unity(loop.vert.co)
            edge_x = motif.bow(q0[1], LATCH_Z, st)
            edge_z = LATCH_Z - motif.crush(q0[1], st)
            loop[uv].uv = (0.022 + abs(pn[0] - edge_x) + max(0.0, edge_z - pn[2]), q0[1])
    return pc


def leak_card(motif, st):
    """A thin emissive card in the latch gap on the burst side of the brush fin, where the crushed edge opened it."""
    if st not in ("D1", "D2"):
        return None
    ys = [y for y in motif.ys if motif.crush(y, st) > 0.0006]
    if len(ys) < 2:
        return None
    lo = [(LATCH_Z - motif.crush(y, st) + 0.0006, y) for y in ys]
    hi = [(LINING_Z - 0.0005, y) for y in ys]
    poly = lo + list(reversed(hi))
    return bc.prism(poly, 0.0046, 0.0054, cap_lo=LEAK, cap_hi=LEAK, wall=lambda *a: LEAK, xoff=lambda y, z: motif.bow(y, LATCH_Z, st))


# ===================================================================== debris (FINAL), pure Python (the bake rebuilds it)
def debris(motif, seed):
    """The pieces the FINAL frees, at their TORN rest (canonical +X burst): enamel chips flaking off the crushed lock edge,
    gypsum crumb clusters from the frame anchors behind the strike. [(name, Geo)]."""
    rng = random.Random(9201)                      # the family's shapes (one rng): a baked throw fits every motif
    st = "TORN"
    out = []
    for k in range(10):
        y = 0.86 + 0.24 * rng.random()
        z = 0.955 + 0.035 * rng.random()
        r = 0.005 + 0.008 * rng.random() ** 1.4
        n = 5 + int(rng.random() * 3)
        ang0 = rng.uniform(0, 2 * math.pi)
        poly = []
        for j in range(n):
            a = ang0 + 2 * math.pi * j / n
            rr = r * (0.6 + 0.5 * rng.random())
            poly.append((z + rr * math.cos(a) * 0.8, y + rr * math.sin(a)))
        x0 = LEAF_X + 0.0002
        g = bc.prism(poly, x0, x0 + 0.0005, cap_lo=CORE_STEEL, cap_hi=ENAMEL, wall=lambda *a: ENAMEL)

        def onface(p):
            q = motif.displace((LEAF_X, p[1], p[2]), st)
            return (p[0] + q[0] - LEAF_X, p[1], q[2])
        g.map(onface)
        out.append(("deb_chip_%02d" % k, g))
    for k in range(6):
        c = (0.030 + 0.025 * rng.random(), 0.88 + 0.18 * rng.random(), 0.986 + 0.006 * rng.random())
        g = bc.Geo()
        for j in range(3 + int(rng.random() * 3)):
            g.extend(bc.crumb(bc.add(c, (rng.uniform(-.005, .005), rng.uniform(-.008, .008), rng.uniform(-.003, .003))), 0.003 + 0.004 * rng.random(), rng, slot=GYPSUM))
        out.append(("deb_gyp_%02d" % k, g))
    return out


def build_all(seed=1):
    """door_break_bake.py's entry: motif `seed`'s debris (Geo, origin) without Blender."""
    motif = Motif(seed)
    geos = {name: (g, g.centroid()) for name, g in debris(motif, seed)}
    return geos, {}, motif


# ===================================================================== main
def stage_info(motif):
    out = []
    for st in STAGES:
        ys = [y for y in motif.ys if motif.crush(y, st) > 0.0005]
        a = AMP[st]
        out.append({
            "stage": st,
            "gapAtBore": round(0.003 + motif.crush(motif.crush_pk, st), 5),
            "stripDxLatch": 0.0,
            "splitY": [round(ys[0], 4), round(ys[-1], 4)] if ys else [round(motif.crush_pk - 0.05, 4), round(motif.crush_pk + 0.05, 4)],
            "bow": round(motif.a(st, "bow"), 5), "dish": round(a["dish"], 5), "dishR": round(a["dishR"], 5),
            "crush": round(motif.a(st, "crush"), 5), "peel": round(motif.a(st, "peel"), 5), "peel2": round(motif.a(st, "peel2"), 5),
        })
    return out


def main(argv):
    import interact_door_break_kitcut as kc
    import interact_door_leaf_steel_da as sd
    import interact_door_leaf_steel_lite_da as sl
    out_root = os.path.abspath(os.path.join(HERE, "..", "..", "..", ".."))
    i = 0
    while i < len(argv):
        if argv[i] == "--out-root":
            out_root = argv[i + 1]; i += 2; continue
        i += 1
    models = os.path.join(out_root, "Assets", "Resources", "Props", "Models")
    os.makedirs(models, exist_ok=True)
    pal = {}
    if os.path.exists(PALETTES):
        with open(PALETTES) as f:
            pal = json.load(f)
    palettes = [{"name": k, "linear": [c for col in v for c in col]} for k, v in pal.items()]

    # 1. the intact leaves, cut
    intact, _kit = kc.build_intact(sd)
    bodies, wins = {}, {}
    for lv, pc in intact.items():
        for v in (WIN_Y0, WIN_Y1):
            kc.cut(pc, "Y", v)
        kc.cut(pc, "Z", WIN_Z0)
        bodies[lv], wins[lv] = kc.split(pc, window_inside)
    lite, _kit2 = kc.build_intact(sl)
    lite_bodies = {}
    for lv, pc in lite.items():
        for v in (WIN_Y0, WIN_Y1):
            kc.cut(pc, "Y", v)
        kc.cut(pc, "Z", WIN_Z0)
        lite_bodies[lv], _w = kc.split(pc, window_inside)
    intact_tris = {lv: pc.tris() for lv, pc in intact.items()}

    budget = []
    motifs_meta = []
    base_tris = {"body_L%d" % lv: p.tris() for lv, p in bodies.items()}
    for k, seed in enumerate(MOTIF_SEEDS):
        motif = Motif(seed)
        pieces = {}
        for lv in (0, 1, 2):
            for st in STAGES:
                pieces["cas_%s_L%d" % (st, lv)] = cassette(wins[lv], motif, st, lv)
        geos = {}
        for st in ("D1", "D2"):
            g = leak_card(motif, st)
            if g is not None:
                geos["leak_" + st] = (g, None)
        ct = kc.export(models, "%s_M%d" % (NAME, k), pieces, geos, meta={"family": NAME, "motif": k, "seed": seed}, tags=("production_p3",))
        deb = {name: (g, g.centroid()) for name, g in debris(motif, seed)}
        dt = kc.export(models, "%s_Deb%d" % (NAME, k), {}, deb, meta={"family": NAME, "motif": k, "seed": seed, "readable": True}, tags=("production_p3",))
        for lv in (0, 1, 2):
            for key, lim in BUDGET[lv].items():
                if key == "debris":
                    n = sum(dt.values())
                elif key == "body":
                    n = base_tris.get("body_L%d" % lv, 0)
                else:
                    n = ct.get("%s_L%d" % (key, lv), 0)
                budget.append({"motif": k, "part": key, "lod": lv, "tris": n, "budget": lim, "ok": n <= lim})
        rests = []
        for name, (g, origin) in deb.items():
            lo, hi = g.bounds()
            rests.append({"name": name, "pos": [round(c, 6) for c in origin], "size": [round(hi[j] - lo[j], 5) for j in range(3)], "tris": dt.get(name, 0)})
        motifs_meta.append({
            "index": k, "seed": seed, "model": "%s_M%d" % (NAME, k), "debris": "%s_Deb%d" % (NAME, k),
            "ys": [round(y, 4) for y in motif.ys], "zs": motif.zs,
            "bowY": round(motif.bow_y, 5), "bowZ": round(motif.bow_z, 5), "dishY": round(motif.dish_y, 5), "dishZ": round(motif.dish_z, 5),
            "crushPk": round(motif.crush_pk, 5), "peel": [round(c, 5) for c in motif.peel], "peel2": [round(c, 5) for c in motif.peel2],
            "stageInfo": stage_info(motif),
            "debrisRest": rests,
            "casTris": [{"name": n, "tris": v} for n, v in sorted(ct.items())],
        })
    fam_anchors = [
        {"name": "damage_latch", "pos": [0.0, 1.10, LATCH_Z]},
        {"name": "head_dust_a", "pos": [0.0, 2.098, 0.05]},
        {"name": "head_dust_b", "pos": [0.0, 2.098, 0.95]},
        {"name": "contact_push", "pos": [-LEAF_X, 1.08, 0.745]},
        {"name": "pivot_bottom", "pos": [0.0, 0.015, 0.036]},
        {"name": "pivot_top", "pos": [0.0, 2.095, 0.036]},
    ]
    common = {
        "material": "steel",
        "anchors": fam_anchors,
        "burst": "+X (canonical; mirror X for a -X burst)",
        "window": {"z": [WIN_Z0, LATCH_Z], "y": [WIN_Y0, WIN_Y1]},
        "motifs": motifs_meta,
        "throws": ["%s_throw%d" % (NAME, t) for t in range(THROWS)],
        "palettes": palettes,
        "intactTris": [intact_tris.get(lv, 0) for lv in (0, 1, 2)],
    }
    meta = dict(common, intact=INTACT, hangsOn=HANGS_ON, budget=budget, triangles=base_tris.get("body_L0", 0), trianglesLod1=base_tris.get("body_L1", 0))
    kc.export(models, NAME, {"body_L%d" % lv: p for lv, p in bodies.items()}, meta=meta, tags=("production_p3",))
    lite_tris = {"body_L%d" % lv: p.tris() for lv, p in lite_bodies.items()}
    meta_l = dict(common, intact="Kit_DoorLeaf_SteelLite_DA", hangsOn=LITE_HANGS_ON, triangles=lite_tris.get("body_L0", 0), trianglesLod1=lite_tris.get("body_L1", 0),
                  note="SteelLite: the window stays under the lite kit (Y 1.229), so it shares the Steel family's cassettes, debris and throws")
    kc.export(models, LITE_NAME, {"body_L%d" % lv: p for lv, p in lite_bodies.items()}, meta=meta_l, tags=("production_p3",))
    over = [b for b in budget if not b["ok"]]
    print("[steel] intact tris LOD0/1/2 %s" % [intact_tris.get(lv, 0) for lv in (0, 1, 2)])
    print("[steel] body tris %s | lite body %s" % ([base_tris.get("body_L%d" % lv, 0) for lv in (0, 1, 2)], [lite_tris.get("body_L%d" % lv, 0) for lv in (0, 1, 2)]))
    for b in budget:
        if b["motif"] == 0 or not b["ok"]:
            print("[steel] m%d %-9s LOD%d %5d / %5d %s" % (b["motif"], b["part"], b["lod"], b["tris"], b["budget"], "OK" if b["ok"] else "OVER"))
    print("[steel] budgets: %d checks, %d over" % (len(budget), len(over)))
    print("[steel] palettes: %s" % (", ".join(sorted(pal.keys())) or "MISSING"))


if __name__ == "__main__":
    main(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
