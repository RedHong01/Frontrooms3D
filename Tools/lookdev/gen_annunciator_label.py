#!/usr/bin/env python3
"""Prop_AnnunciatorLabel_A: the fire annunciator's label atlas (游戏视觉, station_labels 2026-10-31).

Kit_FireAnnunciator (Tools/Blender/frontrooms_kit/assets/fire_annunciator.py) maps its nine label strips (150 x 16 mm: 8
zones + TROUBLE), its POWER strip (60 x 16 mm) and its nine frosted lenses onto this one atlas; the cells come from the
kit's sidecar (Assets/Resources/Props/Models/Kit_FireAnnunciator.json, "labelAtlas"), so the kit and the texture cannot
drift. Layout for 平面视觉: Documentation/research/station/annunciator_label_layout.md.

Until 平面视觉 draws the engraved words (叙事 signs them), the atlas is the plain label ground: #C9C3B2 (the colour the
material carried before, 30L §10.2 S12) everywhere, so the panel looks exactly as it did; the material's _BaseColor is
white, the texture carries the colour. 平面视觉's finished art replaces this PNG as is (same size, same cells; .meta kept).

Usage: python3 gen_annunciator_label.py [--root <Frontrooms3D>] [--out <png>] [--template <png>]
  --out defaults to <root>/Assets/Resources/Surfaces/Textures/Prop_AnnunciatorLabel_A.png.
  --template also writes a drawing template (NOT for the game): the ground with each cell outlined and named, at 1 px.
"""
import json
import os
import sys

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))


def arg(name, default=None):
    return sys.argv[sys.argv.index(name) + 1] if name in sys.argv else default


ROOT = arg("--root", os.path.dirname(os.path.dirname(HERE)))
OUT = arg("--out", os.path.join(ROOT, "Assets", "Resources", "Surfaces", "Textures", "Prop_AnnunciatorLabel_A.png"))
TEMPLATE = arg("--template")
SIDECAR = os.path.join(ROOT, "Assets", "Resources", "Props", "Models", "Kit_FireAnnunciator.json")
GROUND = (0xC9, 0xC3, 0xB2)


def main():
    with open(SIDECAR, encoding="utf-8") as f:
        atlas = json.load(f)["labelAtlas"]
    w, h = atlas["size"]
    img = Image.new("RGB", (w, h), GROUND)
    img.save(OUT)
    print("wrote %s: %d x %d, %d px/mm, ground #%02X%02X%02X, %d cells" % (OUT, w, h, atlas["pxPerMm"], *GROUND, len(atlas["cells"])))
    if TEMPLATE:
        t = img.copy()
        d = ImageDraw.Draw(t)
        for name, (x, y, cw, ch) in atlas["cells"].items():
            d.rectangle((x, y, x + cw - 1, y + ch - 1), outline=(200, 30, 40))
            d.text((x + 6, y + 4), "%s  %d x %d px = %g x %g mm" % (name, cw, ch, cw / atlas["pxPerMm"], ch / atlas["pxPerMm"]), fill=(200, 30, 40))
        t.save(TEMPLATE)
        print("template %s" % TEMPLATE)


if __name__ == "__main__":
    main()
