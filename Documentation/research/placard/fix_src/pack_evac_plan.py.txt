"""Pack the Q16 evacuation-plan artwork for Unity (research/placard/10_spec.md §3.3, 35_fix.md §6).

  python3 Tools/lookdev/pack_evac_plan.py [art_dir] [out_dir]
    art_dir  default Tools/print/ink/art_from_graphic/placard   (平面视觉's artwork; never edited)
    out_dir  default Assets/Resources/Surfaces/Textures

Inputs: placard_print_lit.png and placard_glow_mask.png, 2592 x 1674 (432 x 279 mm at 6 px/mm).
Step 1: one edge row is copied at the top and at the bottom (2592 x 1676), so the kit's UVs
(v 1/1676 .. 1675/1676) land on the artwork's first and last rows.
Step 2 (35_fix.md §6): the padded sheet is resampled to 4096 x 2048, a power of two. Unity 6
keeps a non-power-of-two texture with mips UNCOMPRESSED on every platform (measured 2026-10-08:
2592 x 1676 imported as RGBA32 / RGB24 on Mac, iOS and WebGL, 40 MB for the pair with mips),
because its mip chain leaves the 4-pixel block grid. At 4096 x 2048 it compresses (BC7 / BC1 on
desktop, ASTC on iOS, DXT1 on WebGL). The resample is a stretch of the whole padded sheet, so the
UVs do not change; it is done in linear light (the GPU filters sRGB textures in linear too), with
Lanczos, 1.580 x across and 1.222 x down: an upsample, so no artwork detail is lost.
Outputs:
  Prop_EvacPlan_A.png  RGB of the print (its alpha is 255 everywhere and is dropped)   -> sRGB, BC7
  Prop_EvacPlan_E.png  the mask's R as R = G = B greyscale (white = phosphor ink)       -> linear, BC1
An R8/BC4 mask would not do: FrontRooms/Surface reads _EmissionMap.rgb, so one channel
would glow red only.

Checks: the padded rows 1..1674 equal the source byte for byte before the resample; the
4096 x 2048 result, resampled back to 2592 x 1676 (Lanczos), stays within a mean of 2/255 of the padded
sheet (printed). The input md5s are printed for the build report. A redrawn legend (same size)
re-runs this script; no mesh, UV or code change.
"""
import hashlib
import os
import sys

import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
ART = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "Tools", "print", "ink", "art_from_graphic", "placard")
OUT = sys.argv[2] if len(sys.argv) > 2 else os.path.join(ROOT, "Assets", "Resources", "Surfaces", "Textures")
W, H = 2592, 1674
OW, OH = 4096, 2048                     # the power-of-two texture (35_fix.md §6)
# The artwork this pack was last verified with: v2 of 2026-10-07 (WP03 swatch legend, A.12 footer, the
# mask = heading + labels + each swatch's turned or flattened band). v1 (2026-10-03, InkShape glyphs):
# print 7b96ea089792d4ba7ceedb713932f98d, mask 5ee79d132fadd4209eda045c14d12371 (art_from_graphic/placard/v1/).
EXPECTED_MD5 = {"placard_print_lit.png": "e39befc64c1509599ba51d450256d467",   # v2, 2026-10-07
                "placard_glow_mask.png": "c7e22e2f192fe1315d4af110328e55a0"}


def md5(path):
    with open(path, "rb") as f:
        return hashlib.md5(f.read()).hexdigest()


def pad_rows(a):
    """One edge row copied above and below (edge replicate)."""
    return np.concatenate([a[:1], a, a[-1:]], axis=0)


def srgb_to_linear(c):
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def linear_to_srgb(c):
    c = np.clip(c, 0.0, 1.0)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)


def resample(f, w, h, filt):
    """Per-channel float resample (PIL mode F) of an H x W x C float array."""
    return np.stack([np.asarray(Image.fromarray(np.ascontiguousarray(f[..., c], np.float32), "F").resize((w, h), filt))
                     for c in range(f.shape[2])], -1)


def to8(f):
    return (np.clip(f, 0.0, 1.0) * 255.0 + 0.5).astype(np.uint8)


def main():
    os.makedirs(OUT, exist_ok=True)
    src_print = os.path.join(ART, "placard_print_lit.png")
    src_mask = os.path.join(ART, "placard_glow_mask.png")
    for p in (src_print, src_mask):
        h = md5(p)
        note = "" if EXPECTED_MD5.get(os.path.basename(p)) == h else "  (NEW artwork: differs from the verified v2 md5)"
        print("[pack_evac_plan] input %s md5 %s%s" % (os.path.basename(p), h, note))
    pr = np.asarray(Image.open(src_print).convert("RGBA"))
    mk = np.asarray(Image.open(src_mask).convert("RGBA"))
    assert pr.shape == (H, W, 4) and mk.shape == (H, W, 4), (pr.shape, mk.shape)
    if pr[..., 3].min() != 255:
        print("[pack_evac_plan] WARNING: the print has transparent pixels; alpha is dropped anyway")
    rgb = pad_rows(pr[..., :3])
    grey = mk[..., 0]
    if not (np.array_equal(grey, mk[..., 1]) and np.array_equal(grey, mk[..., 2])):
        print("[pack_evac_plan] WARNING: the mask is not grey; its R channel is used")
    e = pad_rows(np.repeat(grey[..., None], 3, axis=2))
    assert rgb.shape == (H + 2, W, 3) and e.shape == (H + 2, W, 3)
    assert np.array_equal(rgb[1:H + 1], pr[..., :3]), "print pixels changed"
    assert np.array_equal(e[1:H + 1, :, 0], grey), "mask pixels changed"
    # Step 2: the power-of-two resample (print in linear light; the mask is linear coverage already).
    lin = srgb_to_linear(rgb.astype(np.float64) / 255.0)
    big_a = to8(linear_to_srgb(resample(lin, OW, OH, Image.LANCZOS)))
    big_e1 = to8(resample(e[..., :1].astype(np.float64) / 255.0, OW, OH, Image.LANCZOS))
    big_e = np.repeat(big_e1, 3, axis=2)
    assert big_a.shape == (OH, OW, 3) and big_e.shape == (OH, OW, 3)
    # Round trip: Lanczos back to the padded size and compare (a mean error, not an identity).
    back_a = to8(linear_to_srgb(resample(srgb_to_linear(big_a.astype(np.float64) / 255.0), W, H + 2, Image.LANCZOS)))
    back_e = to8(resample(big_e1.astype(np.float64) / 255.0, W, H + 2, Image.LANCZOS))
    err_a = float(np.abs(back_a.astype(np.int16) - rgb.astype(np.int16)).mean())
    err_e = float(np.abs(back_e[..., 0].astype(np.int16) - e[..., 0].astype(np.int16)).mean())
    print("[pack_evac_plan] round trip 4096 x 2048 -> 2592 x 1676: mean |error| print %.3f, mask %.3f (8-bit levels)" % (err_a, err_e))
    assert err_a < 2.0 and err_e < 2.0, "the resample moved the artwork"   # v2: 0.88 and 0.04 (hard 1 px rules round-trip loosely)
    out_a = os.path.join(OUT, "Prop_EvacPlan_A.png")
    out_e = os.path.join(OUT, "Prop_EvacPlan_E.png")
    Image.fromarray(np.ascontiguousarray(big_a), "RGB").save(out_a, optimize=True)
    Image.fromarray(np.ascontiguousarray(big_e), "RGB").save(out_e, optimize=True)
    # Read back: the written files hold exactly the arrays.
    assert np.array_equal(np.asarray(Image.open(out_a).convert("RGB")), big_a)
    assert np.array_equal(np.asarray(Image.open(out_e).convert("RGB")), big_e)
    cover = float((grey > 127).mean())
    print("[pack_evac_plan] wrote %s and %s: %d x %d (from %d x %d), mask cover %.2f %%" % (out_a, out_e, OW, OH, W, H + 2, 100 * cover))


if __name__ == "__main__":
    main()
