#!/usr/bin/env python3
# FrontRooms pool water: the caustic loops (research/bathroom_rooms/70_look_build.md, "Submerged"; 17_water.md 4.4, 7
# item 8). Game visual line. Deterministic: the same bytes every run.
#
# The caustics are the projection of the SAME surface the ripple loops hold (gen_pool_ripples.bake: the exact float
# slope fields of every frame, before 8-bit encoding), not a separate texture: a ray crossing the surface at x leaves it
# bent by the slope s(x) and meets a receiver at
#     x + D s(x)        D = d (1 - 1/n) for light refracted down onto a floor d under the water,
#                       D = h (n - 1)   for light leaving the water up to a ceiling h above it (n = 1.333);
# the brightness there is the density of arriving rays (brightness from the area change, GPU Gems 1 ch. 2, Evan
# Wallace's WebGL Water), folds included: every source texel's energy is splatted where its ray lands, on a field
# upsampled 8x (FFT, exact for the band-limited bake), then a 0.5-texel blur. Four lens distances D per loop go into
# R, G, B, A; the shader interpolates in log D, holds the last channel past it (the nets of random waves keep their
# contrast past the focus; a lamp's own size blurs them, by mip). Each slice's mean is exactly 1 (energy is only moved).
#
# Loops (the ripple's slice clock: the shader uses _FR_RippleA / _FR_RippleL as for the slopes, so Reduce Motion
# freezes both together):
#   Fine       desktop fine cascade at its desktop weight (layers A and B each carry 1/sqrt 2 of it): 256^2 x 64,
#              D = 0.01 0.03 0.1 0.32 m (floors 4 cm - 1.3 m under the water: ankle floors, stair treads, basins)
#   FinePhone  phone tier (handheld, and WebGL's two scrolled layers use its slice 0): layer A at full weight,
#              128^2 x 32, same D
#   Long       every tier, the 7-30 cm cascade: 128^2 x 128, D = 0.06 0.2 0.6 1.8 m (basin floors 0.24-7 m down,
#              thrown light on ceilings 0.2-5.4 m up)
# Encoding: linear, I / 6 in 8 bits (box mips stay energy-true); the share of energy above 6 is reported (clipped).
# Formats: Standalone BC7, iOS / Android ASTC 4x4, WebGL RGBA32 (FinePhone, Long at 64 px); other tiers' sheets
# are 32 px stubs, as the ripple loops.
#
#   python3 Tools/lookdev/pool_water/gen_pool_caustics.py [--out Assets/Resources/Rendering/Water] [--qa <dir>]
import argparse
import hashlib
import json
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gen_pool_ripples as R  # noqa: E402

N_WATER = 1.333
ENC_MAX = 6.0
UPSAMPLE = 8
CAUSTICS = [
    dict(name="Fine", src="Fine", weight=0.70710678, out=256, D=(0.01, 0.03, 0.1, 0.32), cols=8,
         sizes={"Standalone": 256, "iOS": R.STUB, "Android": R.STUB, "WebGL": R.STUB},
         tiers="desktop (Standalone: Mac, Windows, the editor)"),
    dict(name="FinePhone", src="FinePhone", weight=1.0, out=128, D=(0.01, 0.03, 0.1, 0.32), cols=8,
         sizes={"Standalone": R.STUB, "iOS": 128, "Android": 128, "WebGL": 128},
         tiers="phone tier (iOS, Android) and WebGL's scrolled layers (slice 0)"),
    dict(name="Long", src="Long", weight=1.0, out=128, D=(0.06, 0.2, 0.6, 1.8), cols=16,
         sizes={"Standalone": 128, "iOS": 128, "Android": 128, "WebGL": 64},
         tiers="every tier"),
]
FORMATS = {"Standalone": 25, "iOS": 48, "Android": 48, "WebGL": 4}   # BC7, ASTC 4x4, ASTC 4x4, RGBA32


def upsample(f, n_up):
    """Exact band-limited upsampling of a periodic field (zero-padding its spectrum)."""
    n = f.shape[0]
    F = np.fft.fft2(f.astype(np.float64))
    G = np.zeros((n_up, n_up), np.complex128)
    h = n // 2
    G[:h, :h] = F[:h, :h]
    G[:h, -h:] = F[:h, -h:]
    G[-h:, :h] = F[-h:, :h]
    G[-h:, -h:] = F[-h:, -h:]
    return (np.fft.ifft2(G) * (n_up * n_up) / (n * n)).real


def splat(px, pz, m):
    """Bilinear splat of unit-energy samples at texel coordinates (px, pz) on a periodic m x m grid; returns the
    density relative to the mean (mean 1)."""
    x0 = np.floor(px)
    z0 = np.floor(pz)
    fx = px - x0
    fz = pz - z0
    x0 = x0.astype(np.int64) % m
    z0 = z0.astype(np.int64) % m
    x1 = (x0 + 1) % m
    z1 = (z0 + 1) % m
    acc = np.bincount((z0 * m + x0).ravel(), ((1 - fx) * (1 - fz)).ravel(), m * m)
    acc += np.bincount((z0 * m + x1).ravel(), (fx * (1 - fz)).ravel(), m * m)
    acc += np.bincount((z1 * m + x0).ravel(), ((1 - fx) * fz).ravel(), m * m)
    acc += np.bincount((z1 * m + x1).ravel(), (fx * fz).ravel(), m * m)
    acc = acc.reshape(m, m)
    return acc / acc.mean()


def blur_half(a):
    """Periodic Gaussian blur, sigma 0.5 texel (removes the splat's sub-texel aliasing)."""
    m = a.shape[0]
    k = np.fft.fftfreq(m) * 2 * np.pi
    g = np.exp(-0.5 * 0.25 * (k[None, :] ** 2 + k[:, None] ** 2))
    return np.fft.ifft2(np.fft.fft2(a) * g).real


def bake_caustics(c, sx, sz, L):
    """sx, sz: (N, n, n) float slopes [z, x] of one cascade; returns (N, m, m, 4) float intensities."""
    N, n, _ = sx.shape
    m = c["out"]
    n_up = int(max(n, min(m * UPSAMPLE, 2048)))                    # 8 samples per output texel and axis (64 a texel)
    texel_up = L / n_up
    zz, xx = np.meshgrid(np.arange(n_up, dtype=np.float64), np.arange(n_up, dtype=np.float64), indexing="ij")
    out = np.empty((N, m, m, 4), np.float32)
    scale = m / L                                                  # output texels per metre
    for j in range(N):
        ux = upsample(sx[j] * c["weight"], n_up)
        uz = upsample(sz[j] * c["weight"], n_up)
        bx = (xx + 0.5) * texel_up                                 # sample centres in metres
        bz = (zz + 0.5) * texel_up
        for di, D in enumerate(c["D"]):
            px = (bx + D * ux) * scale - 0.5
            pz = (bz + D * uz) * scale - 0.5
            out[j, :, :, di] = blur_half(splat(px, pz, m))
    return out, n_up


def stats_of(I):
    """Per D channel: std, the 99.9th percentile, max, the share of energy above ENC_MAX."""
    o = []
    for di in range(4):
        a = I[..., di].astype(np.float64)
        over = np.clip(a - ENC_MAX, 0.0, None).sum() / a.sum()
        o.append(dict(std=round(float(a.std()), 4), p999=round(float(np.percentile(a, 99.9)), 3),
                      max=round(float(a.max()), 3), min=round(float(a.min()), 4), clipped_energy=round(float(over), 6)))
    return o


def encode(I):
    return np.clip(np.rint(I / ENC_MAX * 255.0), 0, 255).astype(np.uint8)


def write_sheet(path, q, cols):
    N, m, _, _ = q.shape
    rows = N // cols
    sheet = np.zeros((rows * m, cols * m, 4), np.uint8)
    for j in range(N):
        r, cc = j // cols, j % cols
        sheet[r * m:(r + 1) * m, cc * m:(cc + 1) * m, :] = np.flipud(q[j])
    Image.fromarray(sheet, "RGBA").save(path, optimize=True)


def write_meta(png, c, N):
    meta = png + ".meta"
    guid = R.existing_guid(meta)
    plats = R.PLATFORM.format(target="DefaultTexturePlatform", size=16384, fmt=-1, ovr=0)
    for t in ("Standalone", "Android", "WebGL", "iOS"):
        plats += R.PLATFORM.format(target=t, size=c["sizes"][t], fmt=FORMATS[t], ovr=1)
    plats += R.PLATFORM.format(target="VisionOS", size=16384, fmt=-1, ovr=0)
    # the fourth lens distance lives in alpha: alpha source "from input" (the ripple sheets' template drops alpha)
    text = R.META_TEX.format(guid=guid, rows=N // c["cols"], cols=c["cols"], platforms=plats).replace("  alphaUsage: 0\n", "  alphaUsage: 1\n")
    assert "  alphaUsage: 1\n" in text
    R.write_text(meta, text)
    return guid


def sha1(path):
    h = hashlib.sha1()
    with open(path, "rb") as f:
        h.update(f.read())
    return h.hexdigest()


def main():
    ap = argparse.ArgumentParser(description="FrontRooms pool caustic loops (projected from the ripple loops)")
    ap.add_argument("--out", default="Assets/Resources/Rendering/Water")
    ap.add_argument("--qa", default=None)
    ap.add_argument("--only", default=None)
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)
    btot = R.spectrum_total()
    sigma_ref2 = np.radians(R.SIGMA_REF_DEG) ** 2
    src = {c["name"]: c for c in R.CASCADES}
    only = set(a.only.split(",")) if a.only else None
    stats_path = os.path.join(a.out, "FR_Caustic_Stats.json")
    doc = dict(generator="Tools/lookdev/pool_water/gen_pool_caustics.py", n_water=N_WATER, enc_max=ENC_MAX,
               upsample=UPSAMPLE, lens="receiver at x + D s(x): D = d (1 - 1/n) under the water, h (n - 1) above it",
               channels="R G B A = the four D of the loop; value / 255 * enc_max = intensity (mean 1)", loops=[])
    old = {}
    if os.path.exists(stats_path):
        try:
            with open(stats_path) as f:
                for r in json.load(f).get("loops", []):
                    old[r["name"]] = r
        except ValueError:
            pass
    for c in CAUSTICS:
        if only and c["name"] not in only:
            continue
        rc = src[c["src"]]
        sx, sz, st, spec = R.bake(rc, sigma_ref2, btot)
        I, n_up = bake_caustics(c, sx, sz, rc["L"])
        st_c = stats_of(I)
        q = encode(I)
        N = I.shape[0]
        png = os.path.join(a.out, "FR_Caustic_%s.png" % c["name"])
        write_sheet(png, q, c["cols"])
        guid = write_meta(png, c, N)
        rec = dict(name=c["name"], file=os.path.basename(png), guid=guid, sha1=sha1(png), bytes=os.path.getsize(png),
                   resources="Rendering/Water/FR_Caustic_%s" % c["name"], source_loop="FR_Ripple_%s" % c["src"],
                   tiers=c["tiers"], slices=N, size=c["out"], cols=c["cols"], rows=N // c["cols"], patch_m=rc["L"],
                   loop_s=rc["T"], slope_weight=c["weight"], lens_D_m=list(c["D"]), upsampled_to=n_up,
                   size_by_platform=c["sizes"], per_D=st_c)
        old[c["name"]] = rec
        print("%-9s %3d^2 x %3d  D %s  std %s  clipped %s  png %.2f MiB" % (
            c["name"], c["out"], N, c["D"], [s["std"] for s in st_c], [s["clipped_energy"] for s in st_c],
            rec["bytes"] / 2 ** 20))
        if a.qa:
            os.makedirs(a.qa, exist_ok=True)
            strip = np.concatenate([np.flipud(np.clip(I[0, :, :, di] / 3.0, 0, 1)) for di in range(4)], axis=1)
            Image.fromarray((strip * 255).astype(np.uint8)).save(os.path.join(a.qa, "caustic_%s_f0_D.png" % c["name"]))
        del sx, sz, I, q
    doc["loops"] = [old[c["name"]] for c in CAUSTICS if c["name"] in old]
    R.write_text(stats_path, json.dumps(doc, indent=1, sort_keys=True) + "\n")
    tg = R.text_meta(stats_path)
    print("stats %s (guid %s, sha1 %s)" % (stats_path, tg, sha1(stats_path)))


if __name__ == "__main__":
    main()
