"""FrontRooms wallpaper print tool: the motion layer of the "sandwich" wallpaper.

The print is ink only. It never carries paper texture or light; those live in the
paper layer of FrontRooms/Surface (visual chat). Every slice covers one wallpaper
roll tile, 0.75 m x 1.125 m, and must tile seamlessly in both directions.

Slice encoding (linear, not sRGB), same as the static _PrintTex frame 0:
  R = ink density 0..1 (continuous: 0 = bare ground, 0.5 = mid, 1 = deep)
  G = cream / accent 0..1
  B = phosphor glow ink 0..1 (hidden print, only visible where a cell's lamp is off)
  A = unused (1)

Commands (run with a Python that has numpy + Pillow, e.g. /usr/bin/python3 on this Mac):
  gen-test <frames_dir>              8 test keyframes derived from the CC0 chevron
  gen-keys <K00.png> <frames_dir>    the same 8 keyframes derived from a pattern's frame 0
  validate <frames_dir> [--art]      seam check every frame (left/right, top/bottom)
  pack <frames_dir> <out.png> [--art] [--cols 4]
                                     resample to 1024x1536, pack a flipbook sheet and
                                     write <out>.print.json for the Unity importer
  preview <frames_dir> <out.png> [--art]
                                     2x2-tiled contact sheet in the Lobby palette

  build-print <pattern_key> [out_dir] [--size 2048x2048]   (default out: patterns/out/<key>)
                                     THE GAME PATH (Red, 2026-10-03): render the vector
                                     pattern (patterns/<key>.py) at the slice size, make
                                     the 8 keyframes on RAW values, encode each through
                                     the visual chat's print_encode LUT, and write one
                                     linear PNG per slice + a sheet, with no mips (the
                                     visual chat's FrontRoomsPrintMips builds the array)

Frames are PNGs sorted by name (K00.png, K01.png, ...). By default a frame is read
as the encoding above. With --art a frame is authored ink art (black motif on
white, e.g. a Figma 750x1125 frame at 1 px = 1 mm), and density = 1 - luminance.
"""
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
CC0 = os.path.join(HERE, "..", "lookdev", "ref", "Backrooms_Chevron_CC0.png")
SLICE_W, SLICE_H = 1024, 1536          # one 0.75 x 1.125 m roll tile
TILE_METRES = (0.75, 1.125)
LOBBY = ("#D2C27C", "#AC9A52", "#766A34", "#E3D594")  # ground, mid, deep, cream (L0_Wallpaper)


# ------------------------------------------------------------------ io helpers
def hexc(h):
    return np.array([int(h[i:i + 2], 16) for i in (1, 3, 5)], np.float32) / 255


def frame_files(d):
    fs = sorted(f for f in os.listdir(d) if f.lower().endswith(".png"))
    if not fs:
        sys.exit(f"no PNG frames in {d}")
    return [os.path.join(d, f) for f in fs]


def load_frame(path, art):
    """Returns float32 HxWx3 (density, cream, glow)."""
    im = Image.open(path)
    if art:
        lum = np.asarray(im.convert("L"), np.float32) / 255
        z = np.zeros_like(lum)
        return np.stack([1 - lum, z, z], -1)
    rgb = np.asarray(im.convert("RGB"), np.float32) / 255
    return rgb


def save_frame(path, f):
    a = np.concatenate([f, np.ones_like(f[..., :1])], -1)
    Image.fromarray((np.clip(a, 0, 1) * 255 + .5).astype(np.uint8), "RGBA").save(path)


def resample_periodic(f, w, h):
    """Resize a seamless tile without breaking its seam: resample a 3x3 tiling, keep the centre."""
    if f.shape[1] == w and f.shape[0] == h:
        return f
    big = np.tile(f, (3, 3, 1))
    out = []
    for c in range(f.shape[2]):
        ch = Image.fromarray(big[..., c], "F").resize((w * 3, h * 3), Image.BICUBIC)
        out.append(np.asarray(ch, np.float32)[h:2 * h, w:2 * w])
    return np.clip(np.stack(out, -1), 0, 1)


# ------------------------------------------------------------------ seam check
def seam_report(f):
    """Edge mismatch vs the image's own neighbour-pixel variation, per axis.
    A seamless tile has wrap differences about as large as interior ones (ratio ~1)."""
    d = f[..., 0] + 0.5 * f[..., 1] + 0.5 * f[..., 2]
    col = np.abs(np.diff(d, axis=1)).mean(0)          # per column pair
    row = np.abs(np.diff(d, axis=0)).mean(1)
    lr = np.abs(d[:, 0] - d[:, -1]).mean()
    tb = np.abs(d[0, :] - d[-1, :]).mean()
    base_x = np.percentile(col, 90) + 1 / 255
    base_y = np.percentile(row, 90) + 1 / 255
    return lr / base_x, tb / base_y


def validate(frames, art, quiet=False):
    bad = []
    for p in frames:
        rx, ry = seam_report(load_frame(p, art))
        ok = rx <= 1.5 and ry <= 1.5
        if not ok:
            bad.append(p)
        if not quiet or not ok:
            print(f"{'ok ' if ok else 'BAD'} {os.path.basename(p)}  seam x {rx:.2f}  y {ry:.2f}  (<= 1.5 passes)")
    return bad


# ------------------------------------------------------------------ test content
def chevron_base():
    """Frame 0: today's chevron ink, the same formula as gen_surfaces.py wallpaper()."""
    src = np.asarray(Image.open(CC0).convert("RGB"), np.float32) / 255
    src = src[:-1, :-1]  # the CC0 file repeats its first row/column at the far edge
    src = resample_periodic(src, SLICE_W, SLICE_H)
    lum = src @ np.array([.2126, .7152, .0722], np.float32)
    ink = np.clip((0.92 - lum) / 0.35, 0, 1)
    cream = np.clip((lum - .90) / .06, 0, 1)
    return np.stack([ink, cream, np.zeros_like(ink)], -1)


def gen_test(out):
    """The eight keyframes built from the CC0 chevron (the original test set)."""
    make_keys(chevron_base(), out)


def gen_keys(base, out):
    """The eight keyframes built from any encoded frame 0, e.g. a pattern's K00.png."""
    make_keys(resample_periodic(load_frame(base, False), SLICE_W, SLICE_H), out)


def make_keys(k0, out):
    """Eight keyframes in a deliberate order: each step is one more thing wrong with
    the paper, and K07 blends back to K00, so the sequence loops. Every operation keeps
    the tile periodic (whole-tile mirrors, half-drops, integer repeats, tone curves)."""
    os.makedirs(out, exist_ok=True)
    H = k0.shape[0]

    def tone(f, g):
        f = f.copy()
        f[..., 0] = f[..., 0] ** g
        return f

    frames = {
        "K00_original": k0,
        "K01_mirrored": k0[:, ::-1],                                 # chevrons point the other way
        "K02_half_drop": np.roll(k0, H // 2, axis=0),                # the roll hung half a repeat off
        "K03_double_repeat": resample_periodic(np.tile(k0, (2, 1, 1)), SLICE_W, SLICE_H),  # twice as dense
        "K04_starved_ink": tone(k0, 2.2),                            # print running dry
        "K05_flooded_ink": tone(k0, 0.45),                           # ink bleeding, darker
        "K06_negative": np.stack([1 - k0[..., 0], k0[..., 1] * 0, k0[..., 2]], -1),  # negative print
        "K07_turned": np.roll(k0[::-1, ::-1], H // 2, axis=0),      # upside down, half-dropped
    }
    for name, f in frames.items():
        save_frame(os.path.join(out, name + ".png"), f)
    print(f"wrote {len(frames)} frames to {out}")


# ------------------------------------------------------------------ pack + preview
def pack(frames, out, art, cols):
    bad = validate(frames, art, quiet=True)
    if bad:
        sys.exit(f"seam check failed for {len(bad)} frame(s); fix them before packing")
    n = len(frames)
    cols = min(cols, n)
    rows = (n + cols - 1) // cols
    sheet = np.zeros((rows * SLICE_H, cols * SLICE_W, 3), np.float32)
    for i, p in enumerate(frames):
        f = resample_periodic(load_frame(p, art), SLICE_W, SLICE_H)
        # Unity slices a flipbook left to right, top to bottom.
        r, c = divmod(i, cols)
        sheet[r * SLICE_H:(r + 1) * SLICE_H, c * SLICE_W:(c + 1) * SLICE_W] = f
    os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
    save_frame(out, sheet)
    meta = dict(columns=cols, rows=rows, slices=n, sliceWidth=SLICE_W, sliceHeight=SLICE_H,
                tileMetres=list(TILE_METRES), encoding="R density, G cream, B glow, linear",
                frames=[os.path.basename(p) for p in frames])
    with open(os.path.splitext(out)[0] + ".print.json", "w") as fh:
        json.dump(meta, fh, indent=2)
    print(f"packed {n} slices as {cols}x{rows} -> {out} ({sheet.shape[1]}x{sheet.shape[0]})")


def shade(f, pal=LOBBY):
    g, m, dp, cr = (hexc(h) for h in pal)
    t = f[..., :1]
    col = np.where(t < .5, g + (m - g) * (t / .5), m + (dp - m) * ((t - .5) / .5))
    col = col + (cr - col) * f[..., 1:2] * .55
    glow = f[..., 2:3]
    return col * (1 - glow) + np.array([.55, 1, .45], np.float32) * glow


def preview(frames, out, art, cell=256):
    n = len(frames)
    cols = min(4, n)
    rows = (n + cols - 1) // cols
    ch = int(cell * 1.5)
    W, H = cols * cell * 2, rows * (ch * 2 + 22)
    img = Image.new("RGB", (W, H), (24, 24, 24))
    dr = ImageDraw.Draw(img)
    for i, p in enumerate(frames):
        f = resample_periodic(load_frame(p, art), cell, ch)
        tile = np.tile(shade(f), (2, 2, 1))
        r, c = divmod(i, cols)
        x, y = c * cell * 2, r * (ch * 2 + 22)
        img.paste(Image.fromarray((tile * 255).astype(np.uint8)), (x, y + 22))
        dr.text((x + 6, y + 5), os.path.splitext(os.path.basename(p))[0], fill=(235, 235, 235))
    img.save(out)
    print(f"preview -> {out}")


# ------------------------------------------------------------------ game path
LUT_PATH = os.path.join(HERE, "..", "lookdev", "print_encode_lut.json")


def load_lut(path=LUT_PATH):
    lut = json.load(open(path))
    return (np.array(lut["density"], np.float64), np.array(lut["cream"], np.float64),
            len(lut["density_raw"]), len(lut["cream_raw"]))


def print_encode(raw_d, raw_c, lut):
    """Bilinear lookup of the visual chat's print_encode table (Tools/lookdev/
    print_encode_lut.json): x = raw density over the 257 density nodes, y = raw cream
    over the 65 cream nodes, table[i][j] indexed [density][cream]. Applied per texel at
    the frame's own resolution, before any resampling or mips."""
    D, C, nd, nc = lut
    x = np.clip(raw_d, 0, 1) * (nd - 1)
    y = np.clip(raw_c, 0, 1) * (nc - 1)
    i0 = np.minimum(np.floor(x).astype(int), nd - 2)
    j0 = np.minimum(np.floor(y).astype(int), nc - 2)
    fx, fy = x - i0, y - j0

    def bil(T):
        return ((T[i0, j0] * (1 - fx) + T[i0 + 1, j0] * fx) * (1 - fy)
                + (T[i0, j0 + 1] * (1 - fx) + T[i0 + 1, j0 + 1] * fx) * fy)
    return bil(D), bil(C)


def raw_keyframes(k0):
    """The 8 motion keyframes on RAW (density, cream) frames (H x W x 2). Same story
    as gen-test: each step is one more thing wrong with the paper; K07 blends to K00."""
    H = k0.shape[0]
    d, c = k0[..., 0], k0[..., 1]

    def two(dd, cc):
        return np.stack([dd, cc], -1)
    dbl = np.concatenate([k0, k0], 0)                            # x2 repeat, exact row pairs
    dbl = dbl.reshape(H, 2, *k0.shape[1:]).mean(1)
    return {
        "K00_original": k0,
        "K01_mirrored": k0[:, ::-1],
        "K02_half_drop": np.roll(k0, H // 2, axis=0),
        "K03_double_repeat": dbl,
        "K04_starved_ink": two(d ** 2.2, c),
        "K05_flooded_ink": two(d ** 0.45, c),
        "K06_negative": two(1 - d, c * 0),
        "K07_turned": np.roll(k0[::-1, ::-1], H // 2, axis=0),
    }


def build_print(key, out, w, h):
    import hashlib
    import importlib.util
    spec = importlib.util.spec_from_file_location(key, os.path.join(HERE, "patterns", key + ".py"))
    pat = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(pat)
    SS = pat.SUPER
    lab = pat.rasterise(pat.tile_pieces(pat.build_regions()), w * SS, h * SS)
    enc = pat.encode_table()                                     # raw (density, cream) per ink
    raw = np.stack([pat.box_down(enc[lab, 0].astype(np.float32), SS),
                    pat.box_down(enc[lab, 1].astype(np.float32), SS)], -1)
    lut = load_lut()
    sha = hashlib.sha1(open(LUT_PATH, "rb").read()).hexdigest()
    os.makedirs(os.path.join(out, "encoded"), exist_ok=True)
    os.makedirs(os.path.join(out, "raw"), exist_ok=True)
    report = {"pattern": key, "slice": [w, h], "tileMetres": list(TILE_METRES), "lut": "Tools/lookdev/print_encode_lut.json",
              "lut_sha1": sha, "slices": []}
    keys = raw_keyframes(raw)
    encoded = []
    for idx, (name, fr) in enumerate(keys.items()):
        ed, ec = print_encode(fr[..., 0], fr[..., 1], lut)
        rgba = np.stack([ed, ec, np.zeros_like(ed), np.ones_like(ed)], -1)
        encoded.append(rgba)
        Image.fromarray((np.clip(rgba, 0, 1) * 255 + .5).astype(np.uint8), "RGBA").save(
            os.path.join(out, "encoded", f"K{idx:02d}.png"))     # the visual chat's builder reads K00..K07
        save_frame(os.path.join(out, "raw", name + ".png"), np.concatenate([fr, np.zeros_like(fr[..., :1])], -1))
        rx, ry = seam_report(np.concatenate([fr, np.zeros_like(fr[..., :1])], -1))
        report["slices"].append({"file": f"encoded/K{idx:02d}.png", "name": name, "raw_mean": [round(float(fr[..., 0].mean()), 4), round(float(fr[..., 1].mean()), 4)],
                                 "encoded_mean": [round(float(ed.mean()), 4), round(float(ec.mean()), 4)],
                                 "seam_xy": [round(float(rx), 2), round(float(ry), 2)]})
    cols, rows = 4, 2
    sheet = np.zeros((rows * h, cols * w, 4), np.float32)
    for i, a in enumerate(encoded):
        r, c = divmod(i, cols)
        sheet[r * h:(r + 1) * h, c * w:(c + 1) * w] = a
    name = "FR_Print_" + "".join(p.capitalize() for p in key.split("_"))
    Image.fromarray((np.clip(sheet, 0, 1) * 255 + .5).astype(np.uint8), "RGBA").save(os.path.join(out, name + ".png"))
    json.dump({"columns": cols, "rows": rows, "slices": len(encoded), "sliceWidth": w, "sliceHeight": h,
               "tileMetres": list(TILE_METRES), "encoded": True, "lut_sha1": sha, "mips": "none (FrontRoomsPrintMips)",
               "encoding": "R/G = print_encode(raw density, raw cream); B = 0 (phosphor reserved); A = 1; linear"},
              open(os.path.join(out, name + ".print.json"), "w"), indent=2)
    # LUT self-test: at grid nodes the bilinear lookup must return the table exactly
    D, C, nd, nc = lut
    ii, jj = np.meshgrid(np.arange(nd), np.arange(nc), indexing="ij")
    td, tc = print_encode(ii / (nd - 1), jj / (nc - 1), lut)
    report["lut_node_max_err"] = float(max(np.abs(td - D).max(), np.abs(tc - C).max()))
    json.dump(report, open(os.path.join(out, "print_report.json"), "w"), indent=2)
    preview(frame_files(os.path.join(out, "raw")), os.path.join(out, "preview_raw_lobby.png"), False)
    print(json.dumps(report, indent=1))


def main(argv):
    if len(argv) < 2:
        sys.exit(__doc__)
    cmd, args = argv[1], argv[2:]
    art = "--art" in args
    cols = 4
    if "--cols" in args:
        cols = int(args[args.index("--cols") + 1])
    pos = [a for i, a in enumerate(args) if not a.startswith("--") and (i == 0 or args[i - 1] not in ("--cols", "--size"))]
    if cmd == "gen-test":
        gen_test(pos[0])
    elif cmd == "gen-keys":
        gen_keys(pos[0], pos[1])
    elif cmd == "build-print":
        size = args[args.index("--size") + 1] if "--size" in args else "2048x2048"
        w, h = (int(v) for v in size.lower().split("x"))
        build_print(pos[0], pos[1] if len(pos) > 1 else os.path.join(HERE, "patterns", "out", pos[0]), w, h)
    elif cmd == "validate":
        sys.exit(1 if validate(frame_files(pos[0]), art) else 0)
    elif cmd == "pack":
        pack(frame_files(pos[0]), pos[1], art, cols)
    elif cmd == "preview":
        preview(frame_files(pos[0]), pos[1], art)
    else:
        sys.exit(__doc__)


if __name__ == "__main__":
    main(sys.argv)
