#!/usr/bin/env python3
"""The Run! receding end's ghost bands (71_run_room.md s.4.6 and s.5.2; card 2, head first): the corridor's own finishes as
'_Ghost' twins, the 200 mm bands of cleaner paint and unworn wax the end leaves at every line it stood on (built as slabs by
FrontRoomsRunRecede.BuildGhostBands; no decals, door break rule D2).

Why each change: the end's sweeps sealed the band for as long as it stood there, so the paint missed the corridor's dirt and
the floor missed its traffic.
  walls (RunB_BlockLower / Upper): macro dirt x 0.25, the grime at the floor and under the soffit x 0.3, the macro tone x 0.6,
    the base colour x 1.04 (less yellowed);
  soffit (RunB_Soffit): macro dirt x 0.25, macro tone x 0.5, base x 1.06;
  floor (RunB_Floor, _FloorLane, _FloorAccent, _FloorWax): the wax unworn, smoothness + 0.12, macro dirt x 0.3, base x 1.03.
Same shader, same textures, same world projection: the block and the tiles run straight through the band; only its wear differs.

  python3 Tools/lookdev/gen_run_ghost_surfaces.py [--root <project>]     (writes Assets/Resources/Surfaces/<name>_Ghost.mat + .meta)
Byte-stable: the GUIDs are fixed below, the values derive from the source materials as they are.
"""
import argparse
import os
import re

GUIDS = {
    "RunB_BlockLower": "bf4d64c50888482ab0be7160d5345d27",
    "RunB_BlockUpper": "a3facf427a0b482f91c608d7e4a381d4",
    "RunB_Soffit": "fb584bcb01504fef8ffc14fbd469114b",
    "RunB_Floor": "92016329a3054c6e98f0f4b7ff19426a",
    "RunB_FloorLane": "97de9678f1954758a4ca50a64b959bd4",
    "RunB_FloorAccent": "355299f475b54a77a1fa357bcc2f7e9d",
    "RunB_FloorWax": "30b0aa55ab5e4a01966cc78c07c7fe39",
}

WALL = {"_MacroDirt": ("mul", 0.25), "_FloorGrime": ("mul", 0.3), "_CeilingGrime": ("mul", 0.3), "_MacroTone": ("mul", 0.6), "base": 1.04}
SOFFIT = {"_MacroDirt": ("mul", 0.25), "_MacroTone": ("mul", 0.5), "base": 1.06}
FLOOR = {"_Smoothness": ("add", 0.12), "_MacroDirt": ("mul", 0.3), "base": 1.03}
RULES = {"RunB_BlockLower": WALL, "RunB_BlockUpper": WALL, "RunB_Soffit": SOFFIT,
         "RunB_Floor": FLOOR, "RunB_FloorLane": FLOOR, "RunB_FloorAccent": FLOOR, "RunB_FloorWax": FLOOR}

META = """fileFormatVersion: 2
guid: %s
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 2100000
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def fmt(v):
    s = ("%.6f" % v).rstrip("0").rstrip(".")
    return s if s else "0"


def ghost(text, name, rule):
    text = text.replace("m_Name: %s\n" % name, "m_Name: %s_Ghost\n" % name, 1)
    for prop, (op, k) in ((p, r) for p, r in rule.items() if p != "base"):
        m = re.search(r"(    - %s: )([-0-9.eE]+)\n" % re.escape(prop), text)
        if not m:
            continue
        v = float(m.group(2))
        v = v * k if op == "mul" else v + k
        text = text[:m.start()] + m.group(1) + fmt(v) + "\n" + text[m.end():]
    m = re.search(r"(    - _BaseColor: \{r: )([-0-9.eE]+)(, g: )([-0-9.eE]+)(, b: )([-0-9.eE]+)(, a: [-0-9.eE]+\})", text)
    if m:
        k = rule["base"]
        r, g, b = (min(1.0, float(m.group(i)) * k) for i in (2, 4, 6))
        text = text[:m.start()] + m.group(1) + fmt(r) + m.group(3) + fmt(g) + m.group(5) + fmt(b) + m.group(7) + text[m.end():]
    return text


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..")))
    a = ap.parse_args()
    folder = os.path.join(a.root, "Assets", "Resources", "Surfaces")
    for name, rule in RULES.items():
        src = os.path.join(folder, name + ".mat")
        text = open(src, encoding="utf-8").read()
        out = ghost(text, name, rule)
        with open(os.path.join(folder, name + "_Ghost.mat"), "w", encoding="utf-8", newline="\n") as f:
            f.write(out)
        with open(os.path.join(folder, name + "_Ghost.mat.meta"), "w", encoding="utf-8", newline="\n") as f:
            f.write(META % GUIDS[name])
        print("wrote", name + "_Ghost.mat")


if __name__ == "__main__":
    main()
