"""Glow option A on the A + M4 + V1 geometry: which ink glows in a power failure.

The v3 legend (legend_swatches.py -> legend/) was drawn on the old single-row cue
geometry (states.py), where FLOW turned only the grey band; under §8 option A (cream ink
glows, nothing else) the cue disappeared in the dark (research/placard/
NOTE_s8_option_A_hides_flow_cue.md). The game's cue set is now A + M4 + V1
(../cue_states/cue_flipbook.py): every arrow turns whole and is fitted to its field, so
its cream stroke turns with it.

This samples the shipped flipbook slices themselves (cue_states/flipbook/encoded, the
same bytes the game would load) through the legend's window and scale, so nothing is
re-drawn by hand:
  window x 75-1200 mm, z 915-1665 mm (the v3 legend's), 1/25, 6 px per placard mm;
  the print starts at the floor in 1125 mm blocks, tile x repeats every 750 mm.
Writes to legend_A/:
  baseline_cream.png, flow_cream.png   white = cream ink (what option A makes glow)
  baseline.png, flow.png               the print in the Lobby palette (print_tool.shade on the
                                       encoded slices; a preview, like legend/<state>.png)
  flow_cream_change.png                white = cream that differs from baseline (the cue
                                       a viewer sees in the dark)
  report.json                          areas, and a check that baseline_cream matches
                                       the v3 legend's (same K00, so it must)
Usage: /usr/bin/python3 legend_cream_A.py [flow_slice, default 3 = C03 turned +u]
"""
import json
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.abspath(os.path.join(HERE, "..", "..", "..")))
import print_tool as pt  # noqa: E402
FLIP = os.path.join(HERE, "..", "cue_states", "flipbook", "encoded")
OUT = os.path.join(HERE, "legend_A")
TILE_W, TILE_H = 750.0, 1125.0          # mm
X0, X1, Z0, Z1 = 75.0, 1200.0, 915.0, 1665.0
K = 6.0 / 25.0                           # placard px per wall mm
SS = 6                                   # samples per output pixel, per axis
CREAM_ON = 0.5                           # encoded G above this = cream ink


def slice_rg(i):
    return np.asarray(Image.open(os.path.join(FLIP, "C%02d.png" % i)))[..., :2].astype(np.float32) / 255.0


def window(field):
    """Area-averaged view of a tile-periodic field (2048^2, y down from the block top)."""
    n, m = field.shape
    w, h = int(round((X1 - X0) * K)), int(round((Z1 - Z0) * K))
    px = (np.arange(w * SS) + 0.5) / (K * SS)
    pz = (np.arange(h * SS) + 0.5) / (K * SS)
    x = X0 + px
    z = Z1 - pz
    u = np.floor((x % TILE_W) / TILE_W * m).astype(int) % m
    v = np.floor((TILE_H - (z % TILE_H)) / TILE_H * n).astype(int) % n
    s = field[v[:, None], u[None, :]]
    return s.reshape(h, SS, w, SS).mean((1, 3))


def save(a, name):
    Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8), "L").save(os.path.join(OUT, name))


def main():
    flow = int(sys.argv[1]) if len(sys.argv) > 1 else 3
    os.makedirs(OUT, exist_ok=True)
    base, turned = slice_rg(0), slice_rg(flow)
    bc, fc = window((base[..., 1] > CREAM_ON).astype(np.float32)), window((turned[..., 1] > CREAM_ON).astype(np.float32))
    save(bc, "baseline_cream.png")
    save(fc, "flow_cream.png")
    for name, rg in (("baseline.png", base), ("flow.png", turned)):
        f = np.stack([window(rg[..., 0]), window(rg[..., 1]), np.zeros(bc.shape, np.float32)], -1)
        Image.fromarray((np.clip(pt.shade(f), 0, 1) * 255 + 0.5).astype(np.uint8), "RGB").save(os.path.join(OUT, name))
    change = np.abs(fc - bc)
    save(change, "flow_cream_change.png")
    v3 = os.path.join(HERE, "legend", "baseline_cream.png")
    rep = {"flow_slice": "C%02d" % flow, "window_mm": [X0, X1, Z0, Z1], "px_per_wall_mm": K,
           "cream_area": {"baseline": round(float(bc.mean()), 4), "flow": round(float(fc.mean()), 4)},
           "cream_changed_area": round(float((change > 0.5).mean()), 4)}
    if os.path.exists(v3):
        old = np.asarray(Image.open(v3).convert("L")).astype(np.float32) / 255.0
        if old.shape == bc.shape:
            rep["baseline_vs_v3_legend"] = {"mean_abs": round(float(np.abs(old - bc).mean()), 4),
                                            "texels_over_half": round(float((np.abs(old - bc) > 0.5).mean()), 5)}
    json.dump(rep, open(os.path.join(OUT, "report.json"), "w"), indent=1)
    print(json.dumps(rep, indent=1))


if __name__ == "__main__":
    main()
