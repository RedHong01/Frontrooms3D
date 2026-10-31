"""The break BASES of the lited double-acting leaves (door vision set A, 2026-10-30; research/door_vision/60_landing.md,
research/door_break/52_production.md). A struck door swaps its intact leaf for the family's base + a cassette; the cassettes,
debris and throws sit in the lock end (Z >= 0.60) and never reach a lite (frame Z <= 0.5495), so they stay as landed, and only
the bases change: each is now the INTACT lited leaf with the cassette window cut out (interact_door_break_kitcut, the steel
family's method), so the lite (cut-out, bead or kit, glass) stays through every stage on the same vertices, UVs and wear.

  Kit_DoorLeafBreak_Veneer_DA    Veneer_DA F1 minus Z 0.60-0.995 x Y 0.50-1.70 (hangs on Veneer_DA, Veneer_Oak_DA)
  Kit_DoorLeafBreak_Ward_DA      new, Ward_DA F5 minus the same window, its armor plates in the body (+ armor_win_L2); shares
                                 the veneer family's cassettes, debris and throws
  Kit_DoorLeafBreak_Steel_DA     Steel_DA F1 minus Z 0.60-0.995 x Y 0.50-1.22 (hangs on Steel_DA, Steel_PaintedMetal_DA)
  Kit_DoorLeafBreak_SteelRun_DA  new, SteelRun_DA F5 minus the same window; shares the steel family's

Each sidecar keeps the landed family's motifs, throws, palettes and anchors (read from the landed base's .json); parts body_L0..L2.

Run (one Blender, headless; after the leaf kits):
  /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup \
      --python Tools/Blender/frontrooms_kit/door_break_lite_bases.py -- [--out-root <unity project>]
"""

import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(HERE, "assets"))

import kitlib  # noqa: E402,F401
import interact_door_break_common as bc  # noqa: E402
import interact_door_break_kitcut as kc  # noqa: E402
import interact_door_leaf_steel_da as steel  # noqa: E402
import interact_door_leaf_steel_run_da as steel_run  # noqa: E402
import interact_door_leaf_veneer_da as veneer  # noqa: E402
import interact_door_leaf_ward_da as ward  # noqa: E402

WIN_Z0 = 0.600
LITE_CLEAR_Z = 0.5495               # no lite frame reaches past this (asserted by the leaf generators)
WIN_VENEER = (0.50, 1.70)
WIN_STEEL = (0.50, 1.22)
VENEER_BASE, STEEL_BASE = "Kit_DoorLeafBreak_Veneer_DA", "Kit_DoorLeafBreak_Steel_DA"
# family, source sidecar, module, window Y, hangsOn, armor_win_L2
FAMILIES = [
    (VENEER_BASE, VENEER_BASE, veneer, WIN_VENEER, ["Kit_DoorLeaf_Veneer_DA", "Kit_DoorLeaf_Veneer_Oak_DA"], False),
    ("Kit_DoorLeafBreak_Ward_DA", VENEER_BASE, ward, WIN_VENEER, ["Kit_DoorLeaf_Ward_DA"], True),
    (STEEL_BASE, STEEL_BASE, steel, WIN_STEEL, ["Kit_DoorLeaf_Steel_DA", "Kit_DoorLeaf_Steel_PaintedMetal_DA"], False),
    ("Kit_DoorLeafBreak_SteelRun_DA", STEEL_BASE, steel_run, WIN_STEEL, ["Kit_DoorLeaf_SteelRun_DA"], False),
]
KEEP = ("material", "anchors", "burst", "window", "motifs", "throws", "palettes", "tags", "lodDistances", "frontAxis", "noCollider", "colliders")


def reaches_window(f):
    """Only the faces that cross into the lock end are cut, so the planes never slice a lite."""
    return any(kc.to_unity(v.co)[2] > LITE_CLEAR_Z for v in f.verts)


def body_of(module, win_y):
    intact, _kit = kc.build_intact(module)
    tris = {lv: pc.tris() for lv, pc in intact.items()}
    y0, y1 = win_y
    bodies = {}
    for lv, pc in intact.items():
        for y in (y0, y1):
            kc.cut(pc, "Y", y, [f for f in pc.bm.faces if reaches_window(f)])
        kc.cut(pc, "Z", WIN_Z0, [f for f in pc.bm.faces if reaches_window(f)])
        bodies[lv], _w = kc.split(pc, lambda c, _f, a=y0, b=y1: c[2] > WIN_Z0 and a < c[1] < b)
    return bodies, tris


def main(argv):
    out_root = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
    i = 0
    while i < len(argv):
        if argv[i] == "--out-root":
            out_root = argv[i + 1]; i += 2; continue
        i += 1
    models = os.path.join(out_root, "Assets", "Resources", "Props", "Models")
    sources = {}
    for n in (VENEER_BASE, STEEL_BASE):
        with open(os.path.join(models, n + ".json")) as f:
            sources[n] = json.load(f)
    for name, src, module, win_y, hangs, armor in FAMILIES:
        side = sources[src]
        bodies, intact_tris = body_of(module, win_y)
        geos = {"armor_win_L2": (bc.armor_window(None, "M", 2), None)} if armor else None
        meta = {k: side[k] for k in KEEP if k in side}
        meta.update(name=name, intact=module.NAME, hangsOn=hangs, liteBody=True,
                    triangles=bodies[0].tris(), trianglesLod1=bodies[1].tris(),
                    intactTris=[intact_tris.get(lv, 0) for lv in (0, 1, 2)])
        if src != name:
            meta["note"] = "base only: its cassettes, debris and throws are %s's (the window stays clear of the lite)" % src
        tags = [t for t in meta.pop("tags", []) if t not in ("door", "door_break")] + ["door_vision"]
        tris = kc.export(models, name, {"body_L%d" % lv: p for lv, p in bodies.items()}, geos, meta=meta, tags=tags)
        print("[lite bases] %s body tris %s (intact %s)" % (name, [tris.get("body_L%d" % lv) for lv in (0, 1, 2)], meta["intactTris"]))
    print("[lite bases] OK")


if __name__ == "__main__":
    main(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
