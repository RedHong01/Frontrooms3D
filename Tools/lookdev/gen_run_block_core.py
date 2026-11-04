#!/usr/bin/env python3
"""The Run! T2 re-stack's surfaces (71_run_room.md s.5.3; card 2, the T2 body, 2026-11-04). numpy + PIL, deterministic.

The end wall comes apart into its concrete masonry units (Kit_RunRestack_Blocks): their faces keep the corridor's paint, their
beds, ends, webs and hollow cores show the raw block, and torn mortar clings to the beds. A 1990 US CMU (ASTM C90, normal
weight, sand and gravel aggregate) is a mid grey, open-textured concrete: sand speckle, a few small stones, pores where
the mix did not close, the cement paste mottled; type N mortar is lighter and finer.

  python3 Tools/lookdev/gen_run_block_core.py [--root <project>]

Writes (byte-stable: fixed seeds and GUIDs):
  Assets/Resources/Surfaces/Textures/Run_BlockCore_A.png  1024 x 1024 sRGB albedo, tile 0.40 m (0.39 mm a texel)
  Assets/Resources/Surfaces/Textures/Run_BlockCore_N.png  1024 x 1024 tangent-space normal (+Y, Unity) from the height
  Assets/Resources/Surfaces/Textures/Run_BlockCore_S.png  1024 x 1024 linear mask: R smoothness, G cavity (FrontRoomsSurface)
  Assets/Resources/Surfaces/RunB_BlockCore.mat     the raw block (mesh UVs in metres, box-projected per piece)
  Assets/Resources/Surfaces/RunB_BlockMortar.mat   the torn mortar (the same maps at a 0.20 m tile, lighter and warmer)
  Assets/Resources/Surfaces/RunB_BlockUpper_Piece.mat, RunB_BlockLower_Piece.mat
      the corridor's two paints with mesh UVs (_FR_MESH_UV): the end's own bond, locked to it, on the at-rest skins
      (Kit_RunRecede_Wall) and on the pieces, so the swap at a step's tell is exact. Everything else is RunB_BlockUpper /
      RunB_BlockLower as they are (the same textures, the same world-space wear), read from those files.
Each with its .meta (the textures' import settings are Run_Block_{A,N,S}'s).
"""
import argparse
import os
import re

import numpy as np
from PIL import Image

N = 1024
TILE_M = 0.40
MM = TILE_M * 1000.0 / N          # mm per texel

GUIDS = {
    "Run_BlockCore_A.png": "e63d069b11654f679bc06238338e338a",
    "Run_BlockCore_N.png": "57bf29b916ec4f52b172aa61a8d1d8a9",
    "Run_BlockCore_S.png": "50d0fea81bd4437cb67e342af1a737fc",
    "RunB_BlockCore.mat": "7b4946f6c58742dba5f415826c4eccb8",
    "RunB_BlockMortar.mat": "f7edbbc1889a4142b01cbdaf88179316",
    "RunB_BlockUpper_Piece.mat": "42a7cf6cf277448abe099e074b264a7e",
    "RunB_BlockLower_Piece.mat": "e43a096c8c9b425bb5ab079f1a2ae156",
}

MAT_META = """fileFormatVersion: 2
guid: %s
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 2100000
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def srgb_to_lin(c):
    c = np.asarray(c, dtype=np.float64) / 255.0
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def lin_to_srgb8(c):
    c = np.clip(c, 0, 1)
    s = np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)
    return np.clip(np.round(s * 255), 0, 255).astype(np.uint8)


def tile_noise(rng, cells):
    """Tileable value noise: a periodic random grid upsampled with wrap (cosine interpolation)."""
    g = rng.random((cells, cells))
    x = np.arange(N) * cells / N
    i0 = np.floor(x).astype(int) % cells
    i1 = (i0 + 1) % cells
    f = x - np.floor(x)
    f = 0.5 - 0.5 * np.cos(np.pi * f)
    row0 = g[:, i0] * (1 - f) + g[:, i1] * f            # (cells, N)
    out = row0[i0, :] * (1 - f)[:, None] + row0[i1, :] * f[:, None]
    return out


def fbm(rng, octaves):
    acc = np.zeros((N, N))
    amp, tot = 1.0, 0.0
    for cells in octaves:
        acc += amp * tile_noise(rng, cells)
        tot += amp
        amp *= 0.55
    return acc / tot


def stamp_grains(rng, count, r_mm, height_mm, tone, H, A, CAV, kind):
    """Round-ish grains (irregular: a perturbed ellipse) stamped with wrap into height H, albedo A (linear) and cavity."""
    for _ in range(count):
        cx, cy = rng.random() * N, rng.random() * N
        r = (r_mm[0] + (r_mm[1] - r_mm[0]) * rng.random() ** 2) / MM
        ax, ay = r * (0.7 + 0.6 * rng.random()), r * (0.7 + 0.6 * rng.random())
        ang = rng.random() * np.pi
        R = int(np.ceil(max(ax, ay))) + 2
        ys = np.arange(-R, R + 1)
        xs = np.arange(-R, R + 1)
        X, Y = np.meshgrid(xs, ys)
        ca, sa = np.cos(ang), np.sin(ang)
        u = (X * ca + Y * sa) / ax
        v = (-X * sa + Y * ca) / ay
        wob = 1.0 + 0.07 * np.sin(np.arctan2(v, u) * (3 + int(rng.random() * 3)) + rng.random() * 6.28)
        d = np.sqrt(u * u + v * v) / wob
        m = np.clip(1.0 - d, 0, 1)
        m = np.clip(m * 3.0, 0, 1)
        if not m.any():
            continue
        iy = (np.arange(-R, R + 1) + int(cy)) % N
        ix = (np.arange(-R, R + 1) + int(cx)) % N
        sub = np.ix_(iy, ix)
        if kind == "pore":
            depth = height_mm * (0.6 + 0.4 * rng.random())
            H[sub] = np.minimum(H[sub], H[sub] * (1 - m) - depth * np.sqrt(m))
            A[sub] = A[sub] * (1 - 0.26 * m)[..., None]
            CAV[sub] = np.minimum(CAV[sub], 1.0 - 0.45 * m)
        else:
            t = tone[int(rng.random() * len(tone))]
            jitter = 0.88 + 0.24 * rng.random()
            col = np.array(t) * jitter
            dome = height_mm * (0.5 + 0.5 * rng.random()) * np.sqrt(m)
            H[sub] = np.maximum(H[sub], H[sub] * (1 - m) + dome)
            A[sub] = A[sub] * (1 - m)[..., None] + col[None, None, :] * m[..., None]


def normal_from_height(h_mm, strength):
    dx = (np.roll(h_mm, -1, axis=1) - np.roll(h_mm, 1, axis=1)) / (2 * MM)
    dy = (np.roll(h_mm, -1, axis=0) - np.roll(h_mm, 1, axis=0)) / (2 * MM)
    n = np.dstack([-dx * strength, dy * strength, np.ones_like(h_mm)])
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return np.clip(np.round((n * 0.5 + 0.5) * 255), 0, 255).astype(np.uint8)


def textures(out):
    rng = np.random.default_rng(19900131)
    # the cement paste: mid grey, a faint warm cast, mottled at 2-5 cm and finer
    paste = srgb_to_lin([150, 148, 141])
    mott = fbm(rng, (8, 16, 32, 64))
    fine = fbm(rng, (128, 256))
    A = np.ones((N, N, 3)) * paste[None, None, :]
    grit = rng.random((N, N))
    A *= (0.90 + 0.20 * mott)[..., None] * (0.95 + 0.10 * fine)[..., None] * (0.965 + 0.07 * grit)[..., None]
    H = (mott - 0.5) * 0.25 + (fine - 0.5) * 0.12 + (grit - 0.5) * 0.05     # mm
    CAV = np.ones((N, N))
    # sand: 0.3-1.0 mm, light and dark, a quarter of the face
    sand = [srgb_to_lin(c) for c in ([160, 156, 146], [138, 136, 130], [156, 150, 138], [132, 128, 122], [166, 162, 154])]
    stamp_grains(rng, 42000, (0.10, 0.40), 0.16, sand, H, A, CAV, "grain")
    # small stones: 1.5-4 mm, greys, buff, a few dark
    stones = [srgb_to_lin(c) for c in ([140, 138, 132], [154, 146, 132], [124, 122, 118], [162, 158, 148], [132, 126, 118])]
    stamp_grains(rng, 420, (0.8, 2.0), 0.40, stones, H, A, CAV, "grain")
    # pores: 0.3-2.5 mm, where the mix did not close
    stamp_grains(rng, 5200, (0.08, 0.6), 0.6, None, H, A, CAV, "pore")
    img = lin_to_srgb8(A)
    Image.fromarray(img, "RGB").save(os.path.join(out, "Run_BlockCore_A.png"), optimize=True)
    Image.fromarray(normal_from_height(H, 1.0), "RGB").save(os.path.join(out, "Run_BlockCore_N.png"), optimize=True)
    smooth = np.clip(0.17 + 0.05 * (fine - 0.5) - 0.10 * (1.0 - CAV), 0.04, 0.3)
    S = np.dstack([np.round(smooth * 255), np.round(np.clip(CAV, 0, 1) * 255), np.zeros((N, N)), np.full((N, N), 255)])
    Image.fromarray(S.astype(np.uint8), "RGBA").save(os.path.join(out, "Run_BlockCore_S.png"), optimize=True)
    mean = img.reshape(-1, 3).mean(axis=0)
    print("[core] albedo mean sRGB %.1f %.1f %.1f" % tuple(mean))


def tex_meta(src_meta, guid):
    text = open(src_meta, encoding="utf-8").read()
    return re.sub(r"^guid: [0-9a-f]+$", "guid: " + guid, text, count=1, flags=re.M)


def set_float(text, prop, value):
    return re.sub(r"(    - %s: )([-0-9.eE]+)\n" % re.escape(prop), lambda m: m.group(1) + fmt(value) + "\n", text, count=1)


def set_color(text, prop, rgb):
    return re.sub(r"(    - %s: \{r: )[-0-9.eE]+(, g: )[-0-9.eE]+(, b: )[-0-9.eE]+(, a: [-0-9.eE]+\})" % re.escape(prop),
                  lambda m: m.group(1) + fmt(rgb[0]) + m.group(2) + fmt(rgb[1]) + m.group(3) + fmt(rgb[2]) + m.group(4), text, count=1)


def set_vec(text, prop, xy):
    return re.sub(r"(    - %s: \{r: )[-0-9.eE]+(, g: )[-0-9.eE]+(, b: [-0-9.eE]+, a: [-0-9.eE]+\})" % re.escape(prop),
                  lambda m: m.group(1) + fmt(xy[0]) + m.group(2) + fmt(xy[1]) + m.group(3), text, count=1)


def set_tex(text, slot, guid):
    return re.sub(r"(    - %s:\n        m_Texture: )\{[^}]*\}" % re.escape(slot),
                  lambda m: m.group(1) + "{fileID: 2800000, guid: %s, type: 3}" % guid, text, count=1)


def fmt(v):
    s = ("%.6f" % v).rstrip("0").rstrip(".")
    return s if s else "0"


def mesh_uv(text):
    text = text.replace("  m_ValidKeywords: []\n", "  m_ValidKeywords:\n  - _FR_MESH_UV\n", 1)
    return set_float(text, "_MeshUV", 1)


def materials(root):
    surf = os.path.join(root, "Assets", "Resources", "Surfaces")
    upper = open(os.path.join(surf, "RunB_BlockUpper.mat"), encoding="utf-8").read()
    lower = open(os.path.join(surf, "RunB_BlockLower.mat"), encoding="utf-8").read()
    out = {}
    out["RunB_BlockUpper_Piece"] = mesh_uv(upper.replace("m_Name: RunB_BlockUpper\n", "m_Name: RunB_BlockUpper_Piece\n", 1))
    out["RunB_BlockLower_Piece"] = mesh_uv(lower.replace("m_Name: RunB_BlockLower\n", "m_Name: RunB_BlockLower_Piece\n", 1))
    # the raw block: the corridor's paint material as the template (same shader and properties), the core maps, no paint
    core = mesh_uv(upper.replace("m_Name: RunB_BlockUpper\n", "m_Name: RunB_BlockCore\n", 1))
    core = set_tex(core, "_BaseMap", GUIDS["Run_BlockCore_A.png"])
    core = set_tex(core, "_BumpMap", GUIDS["Run_BlockCore_N.png"])
    core = set_tex(core, "_MaskMap", GUIDS["Run_BlockCore_S.png"])
    core = set_color(core, "_BaseColor", (0.86, 0.86, 0.84))
    core = set_vec(core, "_TileSize", (0.4, 0.4))
    core = set_float(core, "_Smoothness", 1)
    core = set_float(core, "_CeilingGrime", 0)
    core = set_float(core, "_FloorGrime", 0)
    core = set_float(core, "_MacroDirt", 0.12)
    core = set_float(core, "_MacroTone", 0.25)
    core = set_float(core, "_BumpScale", 1)
    out["RunB_BlockCore"] = core
    mortar = core.replace("m_Name: RunB_BlockCore\n", "m_Name: RunB_BlockMortar\n", 1)
    mortar = set_color(mortar, "_BaseColor", (1.0, 0.975, 0.93))
    mortar = set_vec(mortar, "_TileSize", (0.2, 0.2))
    mortar = set_float(mortar, "_BumpScale", 0.7)
    out["RunB_BlockMortar"] = mortar
    for name, text in out.items():
        with open(os.path.join(surf, name + ".mat"), "w", encoding="utf-8", newline="\n") as f:
            f.write(text)
        with open(os.path.join(surf, name + ".mat.meta"), "w", encoding="utf-8", newline="\n") as f:
            f.write(MAT_META % GUIDS[name + ".mat"])
        print("[core] wrote", name + ".mat")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..")))
    a = ap.parse_args()
    tex = os.path.join(a.root, "Assets", "Resources", "Surfaces", "Textures")
    textures(tex)
    for name, src in (("Run_BlockCore_A.png", "Run_Block_A.png"), ("Run_BlockCore_N.png", "Run_Block_N.png"), ("Run_BlockCore_S.png", "Run_Block_S.png")):
        with open(os.path.join(tex, name + ".meta"), "w", encoding="utf-8", newline="\n") as f:
            f.write(tex_meta(os.path.join(tex, src + ".meta"), GUIDS[name]))
    materials(a.root)


if __name__ == "__main__":
    main()
