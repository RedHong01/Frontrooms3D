"""The break BASES of the map members' new leaves (zone doors, the Map stage, 2026-11-06; research/double_doors/30_build.md Part M;
the method of door_break_lite_bases.py, research/door_break/52_production.md): a struck door swaps its intact leaf for its family's
base + a cassette; the cassettes, debris and throws sit in the lock end (Z >= 0.60) and never reach a louvre, a lite or the
porthole (each ends at Z <= 0.5495), so the steel family's land as they are and only the bases are new: each is the INTACT leaf
with the cassette window cut out, so the louvre's cut-out, the F1 lite and the porthole's pane stay through every stage on the same
vertices, UVs and wear. The face slot is renamed Door_Enamel, so the break's slot swap gives the base the row's face and its stage
materials (FrontRoomsDoorBreakVisual.ApplySurfaces), as on every steel base.

  Kit_DoorLeafBreak_SteelLouvre_DA  Kit_DoorLeaf_SteelLouvre_DA minus Z 0.60-0.995 x Y 0.50-1.22 (hangs on the BH-F and BH-K
                                    leaves); the steel family's cassettes, debris, throws and palettes, plus a face palette
                                    Door_BackOfHouse_Map (B-2a's warm grey over the grey shop primer)
  Kit_DoorLeafBreak_FRP_DA          Kit_DoorLeaf_FRP_DA minus the same window (hangs on the PL-F and PL-K leaves); the steel
                                    family's cassettes (a dished and crazed gel-coat skin, the parted edge) with the FRP door's
                                    palettes (the gel coat over chopped-strand glass, the closed-cell PU foam core in the kraft
                                    slot, the polyester resin in the glue slot, the aqua frame) and a slot swap Door_CoreSteel ->
                                    Door_CoreFoam (the core shows foam, not kraft honeycomb: Pitt / Warminster's foam core,
                                    research Part P); face palettes for the white and the aqua gel coat

Run (one Blender, headless; after the leaf kits):
  /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup \
      --python Tools/Blender/frontrooms_kit/door_break_map_bases.py -- [--out-root <unity project>] [--src-root <unity project>]
--src-root names the project whose landed Kit_DoorLeafBreak_Steel_DA.json the families copy (default: --out-root).
"""

import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(HERE, "assets"))

import kitlib  # noqa: E402,F401
import interact_door_break_kitcut as kc  # noqa: E402
import door_leaf_steel_louvre_da as louvre  # noqa: E402
import door_leaf_frp_da as frp  # noqa: E402
import door_leaf_map_common as mc  # noqa: E402

WIN_Z0 = 0.600
LITE_CLEAR_Z = 0.5495
WIN_STEEL = (0.50, 1.22)
STEEL_BASE = "Kit_DoorLeafBreak_Steel_DA"
KEEP = ("material", "anchors", "burst", "window", "motifs", "throws", "palettes", "tags", "lodDistances", "frontAxis", "noCollider", "colliders")


def shades(mean, lo=0.90, hi=1.06, n=7):
    """n linear colours from lo x to hi x the mean (the palettes' rule: a sampled spread of the surface, darkest first)."""
    out = []
    for i in range(n):
        k = lo + (hi - lo) * i / (n - 1)
        out += [round(min(1.0, c * k), 4) for c in mean]
    return out


# B-2a's warm grey (Door_BackOfHouse: the PaintedMetal maps' mean linear (0.650, 0.610, 0.512) x the stored tint's linear
# (0.404, 0.397, 0.394)) over the grey shop primer (Part A's leaf-wear chip, linear 0.150, 0.150, 0.145)
BOH_GREY = (0.263, 0.242, 0.202)
GREY_PRIMER = [0.150, 0.150, 0.145]
# the gel coats (Door_PoolGelcoat: sRGB (230, 230, 224) -> linear 0.791, 0.791, 0.745 x the gel-coat map's mean 0.985; the aqua:
# Door_PoolGelcoat_Frame's sRGB (66, 158, 163) -> linear 0.054, 0.342, 0.366) over chopped-strand glass in polyester (pale grey-green)
GEL_WHITE = (0.779, 0.779, 0.734)
GEL_AQUA = (0.054, 0.342, 0.366)
GLASS_MAT = [0.520, 0.540, 0.500]
PU_FOAM = (0.700, 0.580, 0.300)            # closed-cell polyurethane foam, cream-yellow (sRGB about 219, 200, 149)
RESIN = (0.420, 0.260, 0.080)              # cured polyester resin, amber
AQUA_FRAME = (0.050, 0.330, 0.352)         # the moulded frame (Door_PoolGelcoat_Frame)

FAMILIES = [
    ("Kit_DoorLeafBreak_SteelLouvre_DA", louvre, mc.BOH, ["Kit_DoorLeaf_SteelLouvre_DA", "Kit_DoorLeaf_SteelLouvreLocked_DA"],
     {"Door_BackOfHouse_Map": shades(BOH_GREY) + GREY_PRIMER}, None),
    ("Kit_DoorLeafBreak_FRP_DA", frp, mc.GELCOAT, ["Kit_DoorLeaf_FRP_DA", "Kit_DoorLeaf_FRPLocked_DA"],
     {"steel": shades(GEL_WHITE, 0.94, 1.02) + GLASS_MAT,
      "Door_PoolGelcoat_Map": shades(GEL_WHITE, 0.94, 1.02) + GLASS_MAT,
      "Door_PoolGelcoat_MapAqua": shades(GEL_AQUA, 0.88, 1.08) + GLASS_MAT,
      "kraft": shades(PU_FOAM, 0.80, 1.08, 8),
      "glue": shades(RESIN, 0.95, 1.0, 8),
      "steelframe": shades(AQUA_FRAME, 0.85, 1.10, 8)},
     [{"kit": "*", "from": "Door_CoreSteel", "to": "Door_CoreFoam"}]),
]


def reaches_window(f):
    return any(kc.to_unity(v.co)[2] > LITE_CLEAR_Z for v in f.verts)


def body_of(module, win_y, face):
    intact, _kit = kc.build_intact(module)
    tris = {lv: pc.tris() for lv, pc in intact.items()}
    y0, y1 = win_y
    bodies = {}
    for lv, pc in intact.items():
        for y in (y0, y1):
            kc.cut(pc, "Y", y, [f for f in pc.bm.faces if reaches_window(f)])
        kc.cut(pc, "Z", WIN_Z0, [f for f in pc.bm.faces if reaches_window(f)])
        body, _w = kc.split(pc, lambda c, _f, a=y0, b=y1: c[2] > WIN_Z0 and a < c[1] < b)
        body.slots = ["Door_Enamel" if s == face else s for s in body.slots]
        bodies[lv] = body
    return bodies, tris


def main(argv):
    out_root = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
    src_root = None
    i = 0
    while i < len(argv):
        if argv[i] == "--out-root":
            out_root = argv[i + 1]; i += 2; continue
        if argv[i] == "--src-root":
            src_root = argv[i + 1]; i += 2; continue
        i += 1
    models = os.path.join(out_root, "Assets", "Resources", "Props", "Models")
    src_models = os.path.join(src_root or out_root, "Assets", "Resources", "Props", "Models")
    with open(os.path.join(src_models, STEEL_BASE + ".json")) as f:
        side = json.load(f)
    for name, module, face, hangs, palettes, swaps in FAMILIES:
        bodies, intact_tris = body_of(module, WIN_STEEL, face)
        meta = {k: json.loads(json.dumps(side[k])) for k in KEEP if k in side}
        pals = meta.get("palettes", [])
        for pname, lin in palettes.items():
            assert len(lin) == 24, "%s palette %s: %d values" % (name, pname, len(lin))
            hit = [p for p in pals if p.get("name") == pname]
            if hit:
                hit[0]["linear"] = lin
            else:
                pals.append({"name": pname, "linear": lin})
        meta["palettes"] = pals
        meta.update(name=name, intact=module.NAME, hangsOn=hangs, liteBody=True,
                    triangles=bodies[0].tris(), trianglesLod1=bodies[1].tris(),
                    intactTris=[intact_tris.get(lv, 0) for lv in (0, 1, 2)])
        if swaps:
            meta["slotSwaps"] = swaps
        meta["note"] = ("base only: its cassettes, debris and throws are %s's (the window stays clear of the louvre, the lite and "
                        "the porthole); the face slot is Door_Enamel so the row's face and stage materials take it; face palettes by "
                        "the intact face's material name (FrontRoomsDoorBreakVisual.PaletteArray)" % STEEL_BASE)
        tags = [t for t in meta.pop("tags", []) if t not in ("door", "door_break")] + ["door_vision", "map_member"]
        tris = kc.export(models, name, {"body_L%d" % lv: p for lv, p in bodies.items()}, None, meta=meta, tags=tags)
        print("[map bases] %s body tris %s (intact %s), slots %s" % (name, [tris.get("body_L%d" % lv) for lv in (0, 1, 2)], meta["intactTris"],
                                                                  sorted({s for b in bodies.values() for s in b.slots})))
    print("[map bases] OK")


if __name__ == "__main__":
    main(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
