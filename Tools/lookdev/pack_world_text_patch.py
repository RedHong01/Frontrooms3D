#!/usr/bin/env python3
"""World text patch copies (FrontRoomsWorldText "patch"; 游戏视觉, 2026-11-03).

A big sheet whose words cover only part of it gets, per language, only the grid cells that differ from English instead of a
full copy: the build carries the cells, and the game copies their compressed blocks into the English sheet on the GPU
(FrontRoomsWorldText.Patch).

  python3 Tools/lookdev/pack_world_text_patch.py <code> <language image> <base> [--root DIR] [--full]

  code            the language: a folder of Assets/Resources/WorldText/ (FrontRoomsLanguage's code: zh-Hans, de, ...)
  language image  the language's sheet, packed exactly as its base was (same size; Tools/lookdev/pack_evac_plan.py for the
                  evacuation plans)
  base            the English sheet in this project, e.g. Assets/Resources/Surfaces/Textures/Prop_EvacPlan_A.png
  --root DIR      where Assets/... is written (default: this project); the base and its .meta are read from this project
  --full          write a full copy (the swap kind) instead
  --patch         keep the patch form whatever share of the sheet differs (game visual, 2026-11-04, the German EXIT in
                  its wider housing: every cell of its face and diffuser maps differs, and its copies replace a landed
                  patch in place, where a full copy beside the patch would lose to it: FrontRoomsWorldTextImport uses
                  the patch). Any other reason for a full copy stops the tool with nothing written. Without it,
                  nothing changes.

Writes Assets/Resources/WorldText/<code>/<the base's path under Resources>~patch.json, ~patch0.png, ~patch1.png ... and
~patchtail.png, with a .meta for each new file (the base's settings and a new GUID, as FrontRoomsWorldTextImport gives them; json and folders
plain). Writes a full copy (<path>.png, the swap kind) instead when the base is readable, when its import is not one this
tool knows to stay block-local (box mips only, no border or fade mips, no crunch, the print frames' own mips), when it would
import smaller than its source on desktop or iOS, when a differing cell is cut by the sheet's top or right edge, or when the
atlases would come to more than 3/4 of the sheet.

The grid: cells of 32 x the largest block side among the base's formats on desktop and iOS (128 for BC1/BC7/ASTC 4x4, 192
when iOS uses ASTC 6x6), laid from the texture's origin (bottom left, where its blocks start). A cell's own box mips equal
the sheet's while it is whole, and its blocks copy while it stays on the block grid: levels 0-5 at 128 texels with 4 x 4
blocks. The smaller levels come whole from the tail: the language's sheet box-filtered to the first of them (64 x 32 for an
evacuation plan) as Unity filters, in linear light for an sRGB sheet (measured 2026-11-03 in Unity 6000.6: an sRGB 0/255 pair
gives 188, a linear one 128), so Unity's own mips of the tail continue the chain. Atlases are powers of two (Unity 6 keeps a
non-power-of-two texture with mips uncompressed, so it could not take a block copy), at most 4096 a side, cells on multiples
of the cell; neighbouring cells of a row stay one copy.

Checks (nothing written on a failure): the base with every cell pasted from the atlases equals the language image, every
channel and texel. Prints the cells, atlases and texels against a full copy, and how far that level would be from the
language's own had it kept English (8-bit mean and max): what the tail spares a far sheet.
"""
import argparse
import hashlib
import json
import os
import re
import sys
import uuid

import numpy as np
from PIL import Image

PROJECT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
RES = "Assets/Resources/"
WT = RES + "WorldText/"
SUFFIX = "~patch"
MAX_ATLAS = 4096
TEXT_META = "fileFormatVersion: 2\nguid: %s\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
FOLDER_META = "fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
# Block side by TextureImporterFormat, for a platform tab that names its format (uncompressed formats are 1).
FORMAT_BLOCK = {
    1: 1, 2: 1, 3: 1, 4: 1, 5: 1, 7: 1, 9: 1, 13: 1, 17: 1, 62: 1, 63: 1,
    10: 4, 12: 4, 24: 4, 25: 4, 26: 4, 27: 4, 34: 4, 45: 4, 46: 4, 47: 4,
    48: 4, 49: 5, 50: 6, 51: 8, 52: 10, 53: 12, 54: 4, 55: 5, 56: 6, 57: 8, 58: 10, 59: 12,
    66: 4, 67: 5, 68: 6, 69: 8, 70: 10, 71: 12,
}
# Block side of the automatic format by compression (0 none, 1 normal, 2 high, 3 low quality).
AUTO_BLOCK = {"Standalone": {0: 1, 1: 4, 2: 4, 3: 4}, "iOS": {0: 1, 1: 6, 2: 4, 3: 8}}


def sha1(path):
    with open(path, "rb") as f:
        return hashlib.sha1(f.read()).hexdigest()


def write_text(path, text):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)


def meta_field(meta, key):
    m = re.search(r"\n\s*" + key + r": ([^\n]*)", meta)
    return m.group(1).strip() if m else None


def platform_tabs(meta):
    """buildTarget -> {maxTextureSize, textureFormat, textureCompression, crunchedCompression, overridden}."""
    tabs = {}
    for block in re.split(r"\n  - serializedVersion: \d+\n", meta.split("platformSettings:", 1)[-1])[1:]:
        fields = dict(re.findall(r"^\s*(\w+): (-?\w+)\s*$", block.split("spriteSheet:")[0], re.M))
        if "buildTarget" in fields:
            tabs[fields["buildTarget"]] = fields
    return tabs


def import_plan(meta, size):
    """(cell, {platform: block}) for the base's import, or (None, reason) when a patch would not equal its sheet."""
    if meta_field(meta, "isReadable") == "1":
        return None, "the base is readable (its code reads its pixels)"
    for key, want in (("mipMapMode", "0"), ("fadeOut", "0"), ("borderMipMap", "0"), ("mipMapsPreserveCoverage", "0")):
        if meta_field(meta, key) not in (want, None):
            return None, "%s %s (mips that are not plain box mips)" % (key, meta_field(meta, key))
    tabs = platform_tabs(meta)
    default = tabs.get("DefaultTexturePlatform")
    if default is None:
        return None, "no default platform tab in the .meta"
    blocks = {}
    for platform in ("Standalone", "iOS"):
        tab = tabs.get(platform)
        use = tab if tab is not None and tab.get("overridden") == "1" else default
        if use.get("crunchedCompression") == "1":
            return None, platform + " crunches it"
        if int(use.get("maxTextureSize", "0")) < max(size):
            return None, "%s imports it at %s, smaller than its %dx%d source" % (platform, use.get("maxTextureSize"), size[0], size[1])
        fmt = int(use.get("textureFormat", "-1"))
        block = AUTO_BLOCK[platform].get(int(use.get("textureCompression", "1"))) if fmt == -1 else FORMAT_BLOCK.get(fmt)
        if block is None:
            return None, "%s format %d is not in this tool's table" % (platform, fmt)
        blocks[platform] = block
    side = max(blocks.values())
    cell = 32 * side
    if any(cell % b for b in blocks.values()):
        cell = 32 * int(np.lcm.reduce(list(blocks.values())))
    return cell, blocks


def to_linear(a):
    a = a / 255.0
    return np.where(a <= 0.04045, a / 12.92, ((a + 0.055) / 1.055) ** 2.4)


def to_srgb8(a):
    a = np.clip(a, 0, 1)
    return np.where(a <= 0.0031308, a * 12.92, 1.055 * a ** (1 / 2.4) - 0.055) * 255.0


def box_level(img, level, srgb):
    """Box mip `level` of an HxWxC uint8 image as Unity's importer makes it (8-bit floats): 2 x 2 averages level by level,
    the colour in linear light for an sRGB sheet, alpha and linear sheets as stored."""
    colour = 1 if img.shape[2] <= 2 else 3
    a = img.astype(np.float64) / 255.0
    if srgb:
        a[..., :colour] = to_linear(a[..., :colour] * 255.0)
    for _ in range(level):
        h, w = a.shape[0] // 2, a.shape[1] // 2
        a = a[:2 * h, :2 * w].reshape(h, 2, w, 2, -1).mean(axis=(1, 3))
    out = a * 255.0
    if srgb:
        out[..., :colour] = to_srgb8(a[..., :colour])
    return out


def atlas_sizes(n, cell):
    """Power-of-two atlases (wide side first, at most MAX_ATLAS) for n cells: the smallest that holds the rest when it wastes
    at most an eighth of itself (or is the fourth), else the largest that the rest fills, and on."""
    sides = [1 << k for k in range(13) if cell <= (1 << k) <= MAX_ATLAS]
    cands = sorted((w * h, w, h, (w // cell) * (h // cell)) for w in sides for h in sides if h <= w)
    out, rest = [], n
    while rest > 0:
        fit = [c for c in cands if c[3] >= rest]
        part = [c for c in cands if 0 < c[3] < rest]
        if fit and (fit[0][0] - rest * cell * cell <= fit[0][0] // 8 or not part or len(out) == 3):
            out.append(fit[0][1:])
            break
        best = max(part, key=lambda c: (c[3], -c[0]))
        out.append(best[1:])
        rest -= best[3]
    return out


def main():
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("code")
    ap.add_argument("language")
    ap.add_argument("base")
    ap.add_argument("--root", default=PROJECT)
    ap.add_argument("--full", action="store_true")
    ap.add_argument("--patch", action="store_true")
    a = ap.parse_args()
    if a.full and a.patch:
        sys.exit("--full and --patch exclude each other")
    base_rel = a.base.replace("\\", "/")
    if not base_rel.startswith(RES):
        sys.exit("the base must be under " + RES)
    base_path = os.path.join(PROJECT, base_rel)
    meta = open(base_path + ".meta", encoding="utf-8").read()
    stem_rel = os.path.splitext(base_rel[len(RES):])[0]
    copy_dir = os.path.join(a.root, WT + a.code, os.path.dirname(stem_rel))
    name = os.path.basename(stem_rel)
    base_img = Image.open(base_path)
    mode = base_img.mode if base_img.mode in ("RGB", "RGBA", "L", "LA") else "RGBA"
    base = np.asarray(base_img.convert(mode))
    lang_img = Image.open(a.language)
    if "A" in lang_img.mode and "A" not in mode and np.asarray(lang_img.convert("RGBA"))[..., 3].min() < 255:
        print("note: the language image has transparency; its base has no alpha, so it is dropped")
    lang = np.asarray(lang_img.convert(mode))
    if base.ndim == 2:
        base, lang = base[..., None], lang[..., None]
    if base.shape != lang.shape:
        sys.exit("the language image is %s, its base %s" % (lang.shape[1::-1], base.shape[1::-1]))
    H, W = base.shape[:2]
    srgb = meta_field(meta, "sRGBTexture") == "1"
    cell, blocks = (None, "--full") if a.full else import_plan(meta, (W, H))
    if os.path.basename(stem_rel).endswith("_P") and cell:
        cell, blocks = None, "a print frame (its mips are FrontRoomsPrintMips', not box mips)"
    cells = []
    if cell:
        diff = np.any(base != lang, axis=2)
        if not diff.any():
            sys.exit("the language image equals its base: no copy needed")
        rows, cols = -(-H // cell), -(-W // cell)
        for cy in range(rows):                       # texture rows, from the bottom
            y1 = H - cy * cell
            y0 = max(0, y1 - cell)
            for cx in range(cols):
                x0, x1 = cx * cell, min(W, (cx + 1) * cell)
                if diff[y0:y1, x0:x1].any():
                    if y1 - y0 < cell or x1 - x0 < cell:
                        cell, blocks = None, "a differing cell (%d, %d) is cut by the sheet's edge" % (x0, H - y1)
                        break
                    cells.append((cx, cy))
            if not cell:
                break
    sizes = atlas_sizes(len(cells), cell) if cell else []
    if cell and sum(w * h for w, h, _ in sizes) > 0.75 * W * H and not a.patch:
        cell, blocks = None, "the cells would come to %.0f%% of the sheet" % (100.0 * sum(w * h for w, h, _ in sizes) / (W * H))
    if a.patch and not cell:
        sys.exit("--patch: no patch for %s (%s); nothing written" % (stem_rel, blocks))
    written = []
    if not cell:
        out = os.path.join(copy_dir, name + ".png")
        print("%s %s: full copy (%s)" % (a.code, stem_rel, blocks))
        os.makedirs(copy_dir, exist_ok=True)
        Image.fromarray(np.ascontiguousarray(lang[..., 0] if lang.shape[2] == 1 else lang), mode).save(out, optimize=True)
        written.append((out, "image"))
    else:
        # Place the cells: atlas by atlas, slot rows from the bottom, left to right; a run that stays side by side in both
        # textures is one copy.
        atlases = [np.zeros((h, w, base.shape[2]), np.uint8) for w, h, _ in sizes]
        rects = []
        i = 0
        for k, (w, h, slots) in enumerate(sizes):
            per_row = w // cell
            for s in range(slots):
                if i == len(cells):
                    break
                cx, cy = cells[i]
                sx, sy = (s % per_row) * cell, (s // per_row) * cell
                atlases[k][h - sy - cell:h - sy, sx:sx + cell] = lang[H - (cy + 1) * cell:H - cy * cell, cx * cell:(cx + 1) * cell]
                bx, by = cx * cell, cy * cell
                last = rects[-1] if rects else None
                if last and last[0] == k and last[2] == sy and last[1] + last[3] == sx and last[6] == by and last[5] + last[3] == bx:
                    last[3] += cell
                else:
                    rects.append([k, sx, sy, cell, cell, bx, by])
                i += 1
        # The check: English with every copy pasted is the language's sheet.
        recon = base.copy()
        for k, ax, ay, w, h, bx, by in rects:
            ah = atlases[k].shape[0]
            recon[H - by - h:H - by, bx:bx + w] = atlases[k][ah - ay - h:ah - ay, ax:ax + w]
        bad = int(np.any(recon != lang, axis=2).sum())
        if bad:
            sys.exit("CHECK FAILED: %d texels of the pasted base differ from the language image; nothing written" % bad)
        levels = {p: sum(1 for m in range(13) if (cell >> m) % b == 0 and (cell >> m) > 0) for p, b in blocks.items()}
        first = min(levels.values())
        lang_far = box_level(lang, first, srgb)
        far = np.abs(lang_far - box_level(base, first, srgb))
        tail = np.clip(np.rint(lang_far), 0, 255).astype(np.uint8)
        atlas_texels = sum(w * h for w, h, _ in sizes)
        print("%s %s: %d cells of %d, %d copies, atlases %s = %.1f%% of the sheet's texels; blocks %s, levels %s; tail %dx%d "
              "from level %d (kept English there: mean %.2f, max %.0f off, 8-bit)"
              % (a.code, stem_rel, len(cells), cell, len(rects), " + ".join("%dx%d" % (w, h) for w, h, _ in sizes),
                 100.0 * atlas_texels / (W * H), blocks, levels, W >> first, H >> first, first, far.mean(), far.max()))
        os.makedirs(copy_dir, exist_ok=True)
        for k, img in enumerate(atlases):
            out = os.path.join(copy_dir, name + SUFFIX + str(k) + ".png")
            Image.fromarray(np.ascontiguousarray(img[..., 0] if img.shape[2] == 1 else img), mode).save(out, optimize=True)
            written.append((out, "image"))
        out = os.path.join(copy_dir, name + SUFFIX + "tail.png")
        Image.fromarray(np.ascontiguousarray(tail[..., 0] if tail.shape[2] == 1 else tail), mode).save(out, optimize=True)
        written.append((out, "image"))
        plan = os.path.join(copy_dir, name + SUFFIX + ".json")
        lines = ["{", '    "width": %d,' % W, '    "height": %d,' % H, '    "cell": %d,' % cell, '    "atlases": %d,' % len(sizes),
                 '    "tail": %d,' % first,
                 '    "base_sha1": "%s",' % sha1(base_path), '    "source_sha1": "%s",' % sha1(a.language), '    "rects": [']
        lines += ["        " + ", ".join(str(v) for v in r) + ("," if j < len(rects) - 1 else "") for j, r in enumerate(rects)]
        lines += ["    ]", "}"]
        write_text(plan, "\n".join(lines) + "\n")
        written.append((plan, "text"))
        # A full copy of the same path, here or in this project, only adds to the build now.
        for top in {a.root, PROJECT}:
            full = os.path.join(top, WT + a.code, stem_rel + ".png")
            if os.path.exists(full):
                print("  superseded: %s (the patch is used; delete the full copy)" % os.path.relpath(full, top))
        for top in {a.root, PROJECT}:
            for f in sorted(os.listdir(os.path.join(top, WT + a.code, os.path.dirname(stem_rel)))) if os.path.isdir(os.path.join(top, WT + a.code, os.path.dirname(stem_rel))) else []:
                m = re.match(re.escape(name + SUFFIX) + r"(\d+)\.\w+$", f)
                if m and int(m.group(1)) >= len(sizes):
                    print("  stale atlas: %s (no longer in the plan; delete it)" % os.path.join(os.path.dirname(stem_rel), f))
    # .meta files for what is new (a file that already has one keeps its GUID), and the new folders'.
    base_lines = meta.split("\n")
    for path, kind in written:
        if os.path.exists(path + ".meta") or os.path.exists(os.path.join(PROJECT, os.path.relpath(path, a.root)) + ".meta"):
            continue
        if kind == "image":
            lines = list(base_lines)
            lines[1] = "guid: " + uuid.uuid4().hex
            write_text(path + ".meta", "\n".join(lines))
        else:
            write_text(path + ".meta", TEXT_META % uuid.uuid4().hex)
    d = os.path.relpath(copy_dir, a.root)
    while d.startswith(WT.rstrip("/")) and d != RES.rstrip("/"):
        if not os.path.exists(os.path.join(a.root, d + ".meta")) and not os.path.exists(os.path.join(PROJECT, d + ".meta")):
            write_text(os.path.join(a.root, d + ".meta"), FOLDER_META % uuid.uuid4().hex)
        d = os.path.dirname(d)
    for path, _ in written:
        print("  wrote", os.path.relpath(path, a.root))



if __name__ == "__main__":
    main()
