"""Pack the CC0 glass grime scans into the two textures FrontRooms/Glass samples.

  python Tools/lookdev/pack_glass_grime.py <cc0_src dir> <out textures dir>

Inputs (ambientCG, CC0 1.0, approved by Red 2026-10-02, listed in
Tools/lookdev/cc0_src/SOURCES.txt; the zips are git-ignored source):
  Smear007                 https://ambientcg.com/a/Smear007
  Fingerprints002          https://ambientcg.com/a/Fingerprints002
  SurfaceImperfections001  https://ambientcg.com/a/SurfaceImperfections001
  SurfaceImperfections007  https://ambientcg.com/a/SurfaceImperfections007
  SurfaceImperfections013  https://ambientcg.com/a/SurfaceImperfections013
  SurfaceImperfections015  https://ambientcg.com/a/SurfaceImperfections015

Outputs (linear data, import with sRGB off; FrontRoomsGlassSetup sets the importers):
  GlassGrime_M.png  1024 RGBA
      R  smear / wipe streaks    Smear007 opacity
      G  fingerprints            Fingerprints002 opacity
      B  dust specks + debris    max(SurfaceImperfections007, 0.7 x SurfaceImperfections013)
      A  broad mottling          0.6 x SurfaceImperfections015 + 0.4 x SurfaceImperfections001
     Each channel is stretched between its 2nd and 99.5th percentile so the shader's
     thresholds mean the same thing on every channel.
  GlassSmear_N.png  512 RGB  OpenGL normal of Smear007 (Unity's convention), for the
     0.05 smudge normal.
All six scans are seamless 1K tiles, so the packed maps tile too.
"""
import io
import os
import sys
import zipfile

import numpy as np
from PIL import Image

SRC = sys.argv[1]
OUT = sys.argv[2]
os.makedirs(OUT, exist_ok=True)


def scan(asset, mapname):
    path = os.path.join(SRC, "ambientcg", asset, asset + "_1K-JPG.zip")
    with zipfile.ZipFile(path) as z:
        for n in z.namelist():
            if n.endswith("_" + mapname + ".jpg"):
                return Image.open(io.BytesIO(z.read(n))).copy()
    raise FileNotFoundError(asset + " " + mapname)


def grey(asset, mapname="Opacity", size=1024):
    im = scan(asset, mapname).convert("L")
    if im.size != (size, size):
        im = im.resize((size, size), Image.LANCZOS)
    return np.asarray(im, np.float32) / 255.0


def stretch(a, lo=2.0, hi=99.5):
    p0, p1 = np.percentile(a, [lo, hi])
    return np.clip((a - p0) / max(p1 - p0, 1e-4), 0.0, 1.0)


r = stretch(grey("Smear007"))
g = stretch(grey("Fingerprints002"))
b = stretch(np.maximum(grey("SurfaceImperfections007"), 0.7 * grey("SurfaceImperfections013")))
a = stretch(0.6 * grey("SurfaceImperfections015") + 0.4 * grey("SurfaceImperfections001"))

rgba = np.stack([r, g, b, a], -1)
Image.fromarray((rgba * 255.0 + 0.5).astype(np.uint8), "RGBA").save(os.path.join(OUT, "GlassGrime_M.png"), optimize=True)

n = scan("Smear007", "NormalGL").convert("RGB").resize((512, 512), Image.LANCZOS)
n.save(os.path.join(OUT, "GlassSmear_N.png"), optimize=True)

for name, ch in (("R smear", r), ("G prints", g), ("B specks", b), ("A mottle", a)):
    print("%-9s mean %.3f  p50 %.3f  p90 %.3f  p99 %.3f" % ((name, ch.mean()) + tuple(np.percentile(ch, [50, 90, 99]))))
print("wrote", os.path.join(OUT, "GlassGrime_M.png"), "and GlassSmear_N.png")
