# Blender-side: write the wrapped loads' film layout (the same numbers the kits are built from) for gen_dock_maps.py film().
import json, os, sys
sys.path.insert(0, "/Users/redwang/FrontRoomsVisualWork/structures_assets/kits/frontrooms_kit")
sys.path.insert(0, "/Users/redwang/FrontRoomsVisualWork/structures_assets/kits/frontrooms_kit/assets")
import _struct_dock as D
out = dict(tex=list(D.FILM_TEX), regions={k: list(v) for k, v in D.film_regions().items()}, tail_x=list(D.FILM_TAIL_X), variants={})
for variant in ("low", "high"):
    lay = D.load_layout(variant)
    y_top = lay[-1]["y1"]
    faces = {}
    for (name, c, d, L, n) in D._faces():
        faces[name] = dict(len=L, start=list(c), dir=list(d), normal=list(n))
    out["variants"][variant] = dict(layers=lay, span=D.film_span(y_top), y0=D.FILM_Y0, y_top=y_top, pallet_top=D.PAL_TOP, faces=faces)
json.dump(out, open(sys.argv[sys.argv.index("--") + 1], "w"), indent=1)
print("FILM LAYOUT WRITTEN")
