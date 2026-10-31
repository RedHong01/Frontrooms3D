"""Strike-zone inserts of the double-acting door FRAMES (production P4, 2026-10-30; research/door_break/50_rethink.md 3.6 "Frames",
20_dir_A.md (direction A's jamb strip), 52_production.md). The intact frame kits are not touched: like the P3 steel leaf, the
intact frame is built here with its own recipe (interact_door_break_kitcut), the strike zone (the latch jamb from Y 0.70 to
1.30, everything past Z 0.985: lining, both casings or the sleeve, the latch fin, the strike pocket) is cut out by planes,
and the zone is shipped per stage:
  Kit_DoorFrameBreak_Wood_DA   (hangs on Kit_DoorFrame_Wood_DA, the L0-F frame)
      body_L0..L2              the intact frame minus the zone (same vertices, UVs, wear colours, normals)
      ins_INTACT_L0..L2        the zone as it is (shown from the swap until the frame changes)
      ins_SPLIT_L0..L2         FINAL: the jamb splits beside the strike along the grain: the burst-side strip of the lining
                               (from X 0.03, just past the strike pocket) and its casing, 420 mm long (Y 0.79..1.21), is
                               pried 10-16 mm toward the burst side and 7 mm into the opening at the strike, still held at its
                               ends; the casing lifts off the wall. ins_SPLIT_teeth_L0/_L1: the crack's torn walls
                               (Door_CoreStile, the light inside of the wood) and splinter teeth on both lips (L0)
  Kit_DoorFrameBreak_Steel_DA  (hangs on Kit_DoorFrame_Steel_DA and its _Alu_DA variant)
      body_L0..L2, ins_INTACT_L0..L2
      ins_D2_L0..L2            D2: the latch drives the strike: the soffit dents 3 mm into the wall round the strike prep,
                               the burst-side face band flares 2 mm, the brush latch fin bends 35 deg toward the burst side
      ins_TORN_L0..L2          FINAL: dent 7 mm, flare 5 mm, the fin crushed flat (70 deg): the DA frame's "silencer"
                               (the design's word for a stop frame's rubber bumpers; a DA frame has the brush fin instead)
The strike itself stays the rig's part: the visual tilts it at D1/D2 (its lips bend toward the burst side) and the map's
DetachStrike drops it at the break, as today. Canonical burst toward +X; the run time mirrors the INSERT only (the frames are
symmetric in X, and the body stays unmirrored so its texture never flips).

Run: /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup \
         --python Tools/Blender/frontrooms_kit/assets/interact_door_frame_da_break.py -- [--out-root <unity project>]
"""

import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))

import interact_door_break_common as bc  # noqa: E402

ZONE_Y = (0.70, 1.30)
ZONE_Z = 0.985
LINING_Z = 0.998
STILE = "Door_CoreStile"

WOOD = dict(name="Kit_DoorFrameBreak_Wood_DA", module="interact_door_frame_wood_da", material="wood",
            hangsOn=["Kit_DoorFrame_Wood_DA"], stages=[("SPLIT", 4)],
            slotSwaps=[{"kit": "*", "from": "Prop_WoodWalnut", "to": "Door_Frame"}, {"kit": "*", "from": "Prop_Chrome", "to": "Prop_ChromeSatin"}])
STEEL = dict(name="Kit_DoorFrameBreak_Steel_DA", module="interact_door_frame_steel_da", material="steel",
             hangsOn=["Kit_DoorFrame_Steel_DA", "Kit_DoorFrame_Steel_Alu_DA"], stages=[("D2", 3), ("TORN", 4)],
             slotSwaps=[{"kit": "Kit_DoorFrame_Steel_Alu_DA", "from": "Prop_SteelBrown", "to": "Prop_Aluminium"},
                        {"kit": "*", "from": "Prop_Chrome", "to": "Prop_ChromeSatin"}])

# wood split
CRACK_X = 0.030
STRIP_Y = (0.79, 1.21, 1.00)
# steel strike zone
STEEL_W = (0.80, 1.14, 0.968)
STEEL_AMP = {"D2": dict(dent=0.003, flare=0.002, fin=35.0), "TORN": dict(dent=0.007, flare=0.005, fin=70.0)}


def s3(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3.0 - 2.0 * t)


def bump(y, a, b, pk):
    if y <= a or y >= b:
        return 0.0
    if y <= pk:
        return s3((y - a) / max(1e-9, pk - a))
    return s3((b - y) / max(1e-9, b - pk))


def zone_inside(c, _f):
    return c[2] > ZONE_Z and ZONE_Y[0] < c[1] < ZONE_Y[1]


def ygrid(lod, a, b, steps=(0.02, 0.05, 0.10)):
    """Planes over [a, b] only (outside it the fields are zero), ends included."""
    step = steps[lod]
    n = max(1, int(round((b - a) / step)))
    return sorted(set([round(a + (b - a) * k / n, 5) for k in range(0, n + 1)]))


# ===================================================================== wood: the jamb split
def wood_split_field(p):
    X, Y, Z = p
    w = bump(Y, *STRIP_Y)
    if w <= 0.0 or X < CRACK_X - 1e-7:
        return p
    t = s3((X - CRACK_X) / 0.075)
    return (X + w * (0.010 + 0.006 * t), Y, Z - w * 0.007 * (1.0 - t))


def wood_split(zone, lod):
    import interact_door_break_kitcut as kc
    pc = zone.copy()
    lining = lambda: [f for f in pc.bm.faces if 0.9965 < kc.centre(f)[2] < 1.0015 and abs(kc.centre(f)[0]) < 0.0796]
    kc.cut(pc, "X", CRACK_X, lining())
    for x in ((0.05, 0.065) if lod == 0 else (0.055,) if lod == 1 else ()):
        kc.cut(pc, "X", x, lining())
    burst_side = lambda: [f for f in pc.bm.faces if kc.centre(f)[0] > CRACK_X - 0.002]
    kc.grid(pc, "Y", ygrid(lod, STRIP_Y[0], STRIP_Y[1]), burst_side)
    # rip the crack: the lining's edges on X = 0.03 strictly inside the strip's span
    pc.bm.verts.index_update()
    rip = []
    for e in pc.bm.edges:
        a, b = kc.to_unity(e.verts[0].co), kc.to_unity(e.verts[1].co)
        if abs(a[0] - CRACK_X) < 1e-6 and abs(b[0] - CRACK_X) < 1e-6 and min(a[1], b[1]) >= STRIP_Y[0] - 1e-6 and max(a[1], b[1]) <= STRIP_Y[1] + 1e-6:
            rip.append(e)
    kc.weld_rip(pc, rip)
    # which side each vertex now belongs to: the mean of its faces' centres
    side = {}
    for v in pc.bm.verts:
        fs = list(v.link_faces)
        side[v] = (sum(kc.centre(f)[0] for f in fs) / len(fs)) if fs else kc.to_unity(v.co)[0]

    def fld(p, v):
        if abs(p[0] - CRACK_X) < 1e-6 and side[v] < CRACK_X:
            return p                          # the hinge lip stays
        return wood_split_field(p)
    kc.deform(pc, fld)
    return pc, len(rip)


def wood_teeth(lod, seed=4310):
    """The crack's torn walls (the 2 mm lining board's inside, both lips) and splinter teeth (LOD0)."""
    rng = random.Random(seed)
    g = bc.Geo()
    step = 0.02 if lod == 0 else 0.05
    ys = [STRIP_Y[0] + step * k for k in range(int(round((STRIP_Y[1] - STRIP_Y[0]) / step)) + 1)]
    for a, b in zip(ys, ys[1:]):
        # hinge lip: a wall at X 0.03 facing +X (Z 0.998..1.000)
        q = [g.vert((CRACK_X, a, LINING_Z)), g.vert((CRACK_X, b, LINING_Z)), g.vert((CRACK_X, b, 1.000)), g.vert((CRACK_X, a, 1.000))]
        g.face(q, STILE, ("proj", (0, 0, 1), (0, 1, 0)), hint=(1, 0, 0))
        # strip lip: the same wall moved with the strip, facing -X
        pa = [wood_split_field((CRACK_X + 1e-6, a, LINING_Z)), wood_split_field((CRACK_X + 1e-6, b, LINING_Z)),
              wood_split_field((CRACK_X + 1e-6, b, 1.000)), wood_split_field((CRACK_X + 1e-6, a, 1.000))]
        q = [g.vert(p) for p in pa]
        g.face(q, STILE, ("proj", (0, 0, 1), (0, 1, 0)), hint=(-1, 0, 0))
    if lod == 0:
        for k in range(14):
            y = STRIP_Y[0] + 0.04 + (STRIP_Y[1] - STRIP_Y[0] - 0.08) * (k + rng.random()) / 14
            if k % 2 == 0:
                root = wood_split_field((CRACK_X + 1e-6, y, LINING_Z + 0.0004))
                g.extend(bc.splinter(root, (-1.0, rng.uniform(-0.4, 0.4), -0.15), (0.2, 0, -1.0), 0.012 + 0.025 * rng.random(), 0.0015 + 0.002 * rng.random(), rng, slot=STILE))
            else:
                root = (CRACK_X - 0.0003, y, LINING_Z + 0.0004)
                g.extend(bc.splinter(root, (1.0, rng.uniform(-0.4, 0.4), -0.2), (0.0, 0, -1.0), 0.010 + 0.018 * rng.random(), 0.0015 + 0.0015 * rng.random(), rng, slot=STILE))
    return g


# ===================================================================== steel: dent, flare, the fin
def steel_field(st):
    a = STEEL_AMP[st]
    import math
    th = math.radians(a["fin"])

    def fld(p, _v):
        X, Y, Z = p
        w = bump(Y, *STEEL_W)
        if w <= 0.0:
            return p
        nx, nz = X, Z
        ax = abs(X)
        if ax <= 0.0042 and Z < LINING_Z - 1e-5:
            # the brush fin: rotate about its root line (Z 0.998) toward the burst side
            d = LINING_Z - Z
            t = th * w
            nx = X * math.cos(t) + d * math.sin(t)
            nz = LINING_Z - (-X * math.sin(t) + d * math.cos(t))
        if Z < 1.0035 and ax < 0.075:
            nz += a["dent"] * w * (1.0 - s3((ax - 0.025) / 0.045))
        if X > 0.070:
            nx += a["flare"] * w * (1.0 - s3((Z - 1.0) / 0.07))
        return (nx, Y, nz)
    return fld


def steel_stage(zone, st, lod):
    import interact_door_break_kitcut as kc
    pc = zone.copy()
    moving = lambda: [f for f in pc.bm.faces if kc.centre(f)[0] > -0.075]
    kc.grid(pc, "Y", ygrid(lod, STEEL_W[0], STEEL_W[1], (0.025, 0.057, 0.114)), moving)
    soffit = lambda: [f for f in pc.bm.faces if kc.centre(f)[2] < 1.0035 and abs(kc.centre(f)[0]) < 0.075]
    for x in ((0.02, 0.035, 0.05, -0.02, -0.035, -0.05) if lod == 0 else (0.03, -0.03) if lod == 1 else ()):
        kc.cut(pc, "X", x, soffit())
    band = lambda: [f for f in pc.bm.faces if kc.centre(f)[0] > 0.07]
    for z in ((1.01, 1.025, 1.045) if lod == 0 else (1.02,) if lod == 1 else ()):
        kc.cut(pc, "Z", z, band())
    kc.deform(pc, steel_field(st))
    return pc


# ===================================================================== main
def build_family(spec, models):
    import importlib
    import interact_door_break_kitcut as kc
    mod = importlib.import_module(spec["module"])
    intact, _kit = kc.build_intact(mod)
    bodies, zones = {}, {}
    for lv, pc in intact.items():
        for v in ZONE_Y:
            kc.cut(pc, "Y", v)
        kc.cut(pc, "Z", ZONE_Z)
        bodies[lv], zones[lv] = kc.split(pc, zone_inside)
    pieces = {}
    geos = {}
    notes = []
    for lv in sorted(intact):
        pieces["body_L%d" % lv] = bodies[lv]
        pieces["ins_INTACT_L%d" % lv] = zones[lv].copy()
        for st, _frm in spec["stages"]:
            if spec["material"] == "wood":
                p, nrip = wood_split(zones[lv], lv)
                notes.append("L%d crack edges ripped %d" % (lv, nrip))
                pieces["ins_%s_L%d" % (st, lv)] = p
                if lv < 2:
                    geos["ins_%s_teeth_L%d" % (st, lv)] = (wood_teeth(lv), None)
            else:
                pieces["ins_%s_L%d" % (st, lv)] = steel_stage(zones[lv], st, lv)
    tris_pre = {n: p.tris() for n, p in pieces.items()}
    meta = {
        "material": spec["material"],
        "hangsOn": spec["hangsOn"],
        "zone": {"y": list(ZONE_Y), "z": ZONE_Z},
        "stages": [{"name": st, "from": frm} for st, frm in spec["stages"]],
        "slotSwaps": spec["slotSwaps"],
        "intactTris": [intact[lv].tris() if lv in intact else 0 for lv in (0, 1, 2)],
        "notes": notes,
    }
    tris = kc.export(models, spec["name"], pieces, geos, meta=meta, tags=("door_frame", "production_p4"))
    print("[frame] %s: intact %s | %s" % (spec["name"], meta["intactTris"], ", ".join("%s %d" % (k, v) for k, v in sorted(tris.items()))))
    print("[frame] %s notes: %s" % (spec["name"], "; ".join(notes) or "-"))
    return tris_pre


def main(argv):
    out_root = os.path.abspath(os.path.join(HERE, "..", "..", "..", ".."))
    i = 0
    while i < len(argv):
        if argv[i] == "--out-root":
            out_root = argv[i + 1]; i += 2; continue
        i += 1
    models = os.path.join(out_root, "Assets", "Resources", "Props", "Models")
    for spec in (WOOD, STEEL):
        build_family(spec, models)


if __name__ == "__main__":
    main(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
