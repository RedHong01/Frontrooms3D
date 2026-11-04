#!/usr/bin/env python3
# FrontRooms pool ripple generator. Game visual line, the pool family's water (workflow W/pool_look, stage "Ripples").
# Doc: Frontrooms3D/Documentation/research/bathroom_rooms/70_look_build.md, section "5. Ripples"; physics: 17_water.md 3.
# ASCII only. Python 3 + numpy + Pillow (W/venv). Deterministic: fixed seeds, same files every run.
"""
Bakes the still pool's constant ripple as seamlessly looping slope sequences with the ocean-style FFT method
(Tessendorf): capillary-gravity dispersion omega^2 = g k + (sigma/rho) k^3, isotropic (no wind, no current), every
frequency quantised to its loop period so the loop is exact. FrontRoomsWaterRipples.hlsl samples them; the editor
importer Assets/Editor/Rendering/FrontRoomsRippleArray.cs imports each sheet as a Texture2DArray (flipbook).

Usage (from the project root):
  python Tools/lookdev/pool_water/gen_pool_ripples.py [--out Assets/Resources/Rendering/Water] [--qa <dir>]
                                                      [--only Fine,FinePhone,Long]

Two cascades, because one 64-frame loop cannot hold both ends of the band: a 5 mm wave has a 16 ms period, a 30 cm
wave 0.44 s, and a loop of T seconds can only give each wave a frequency that is a multiple of 1/T.
  FINE  5 mm to ~7 cm, 93 % of the slope variance (the capillary-gravity band where the sparkle lives).
        Desktop  FR_Ripple_Fine.png       64 slices of 512 x 512 (8 x 8 sheet), patch 1.2 m, loop 0.50 s
        Phone    FR_Ripple_FinePhone.png  32 slices of 256 x 256 (8 x 4 sheet), patch 1.2 m, loop 0.32 s
                 (handheld and WebGL; the same random field as the desktop one, cut at its own resolution)
  LONG  ~7 cm to 30 cm, 7 % of the slope variance (the slow wobble of far reflections).
        Every tier FR_Ripple_Long.png     128 slices of 128 x 128 (16 x 8 sheet), patch 2.4 m, loop 2.56 s
Every patch divides 192 m (the room stream's origin rebase), so a rebase never moves the pattern.

What does not fit a sequence becomes roughness, so the game (MSAA, no TAA) shows it as a wider highlight, not noise:
  - waves too short for the sheet's texels (phone: under ~1 cm);
  - waves too fast for its frame step (a wave that turns more than 90-135 degrees of phase per frame is faded out:
    linear frame blending could not show it, and a 60 Hz display shows it only as blur anyway);
  - the part of the field a pixel's footprint averages away (the footprint table, anisotropic: long along the view);
  - the part frame blending loses between two slices (interp_loss: 4 f (1 - f) D of the resolved variance).
Each of these is written to FR_Ripple_Stats.json, which FrontRoomsWaterRipples.cs loads into shader constants.

Channels: R = dh/dx, G = dh/dz (world x along U, world z along V), linear 8 bit, slope = (2 v - 1) * slope_enc_rad.
B is 0. Gain 1 = the reference calm: rms slope 2.0 degrees over both cascades (17: calm slopes 1-3 degrees).
Each PNG gets a .meta (an existing .meta keeps its GUID). <out>/FR_Ripple_Stats.json also lists sizes, sha1, GUIDs.
"""
import argparse
import hashlib
import json
import os
import re
import uuid

import numpy as np
from PIL import Image

# ------------------------------------------------------------------------------------------------ physics
G = 9.81                          # m/s^2
SIGMA_RHO = 0.0728 / 998.0        # surface tension / density, m^3/s^2 (clean water at 20 C)
SIGMA_REF_DEG = 2.0               # rms slope magnitude of the whole constant ripple at gain 1 (17 s3.1: 1-3 degrees)

# Slope variance per ln k (17 s3.1: 0.05-0.3 mm high at 1-5 cm, slopes 1-3 degrees; s3.5: the constant ripple is
# capillary-scale sparkle, anything larger is caused by someone): a log-normal bump at 2 cm, a faint shelf at 12 cm
# (the slow wobble of far reflections), soft edges a quarter octave wide at 5 mm and 30 cm.
SPEC = dict(lam_p=0.020, w=0.55, eps_lo=0.08, lam_lo=0.12, w_lo=0.6, lam_min=0.005, lam_max=0.30, edge_oct=0.25)
XOVER = dict(lam_x=0.07, oct_w=0.35)          # fine / long crossover, smooth in ln k

CASCADES = [
    dict(name="Fine", band="fine", L=1.2, n=512, frames=64, T=0.50, cols=8, phi=(90.0, 135.0), seed=1201, lattice=512,
         tiers="desktop (Standalone: Mac, Windows, the editor)"),
    dict(name="FinePhone", band="fine", L=1.2, n=256, frames=32, T=0.32, cols=8, phi=(90.0, 135.0), seed=1201, lattice=512,
         tiers="phone tier (iOS, WebGL)"),
    dict(name="Long", band="long", L=2.4, n=128, frames=128, T=2.56, cols=16, phi=(45.0, 90.0), seed=2401, lattice=128,
         tiers="every tier"),
]

LUT_LOG2 = [0.5 * i for i in range(19)]       # footprint sizes 1 .. 512 texels, half-octave steps


def omega(k):
    return np.sqrt(G * k + SIGMA_RHO * k ** 3)


def _sm(x):
    x = np.clip(x, 0.0, 1.0)
    return x * x * (3.0 - 2.0 * x)


def spectrum_B(k):
    """Slope variance per ln k (both components), unnormalised."""
    s = SPEC
    k = np.maximum(k, 1e-9)
    kp, klo = 2 * np.pi / s["lam_p"], 2 * np.pi / s["lam_lo"]
    b = np.exp(-0.5 * (np.log(k / kp) / s["w"]) ** 2) + s["eps_lo"] * np.exp(-0.5 * (np.log(k / klo) / s["w_lo"]) ** 2)
    kmax, kmin = 2 * np.pi / s["lam_min"], 2 * np.pi / s["lam_max"]
    e = s["edge_oct"] * np.log(2.0)
    return b * _sm((np.log(kmax) - np.log(k)) / e + 0.5) * _sm((np.log(k) - np.log(kmin)) / e + 0.5)


def xover_fine(k):
    """Share of a wave number that belongs to the fine cascade (1 short, 0 long)."""
    kx = 2 * np.pi / XOVER["lam_x"]
    return _sm((np.log(np.maximum(k, 1e-9)) - np.log(kx)) / (XOVER["oct_w"] * np.log(2.0)) * 0.5 + 0.5)


def spectrum_total():
    """Integral of spectrum_B over ln k (both cascades)."""
    lk = np.linspace(np.log(2 * np.pi / 2.0), np.log(2 * np.pi / 0.001), 20001)
    tz = getattr(np, "trapezoid", None) or np.trapz
    return float(tz(spectrum_B(np.exp(lk)), lk))


# ------------------------------------------------------------------------------------------------ the bake
def lattice(n, L):
    m = (np.fft.fftfreq(n) * n).astype(np.float64)
    mx, mz = np.meshgrid(m, m, indexing="xy")            # arrays are [z, x]
    kx, kz = 2 * np.pi * mx / L, 2 * np.pi * mz / L
    return m, kx, kz, np.hypot(kx, kz)


def bake(c, sigma_ref2, btot):
    n, L, N, T = c["n"], c["L"], c["frames"], c["T"]
    m, kx, kz, k = lattice(n, L)
    k1 = np.where(k > 0, k, 1.0)
    cell = (2 * np.pi / L) ** 2
    band = xover_fine(k1) if c["band"] == "fine" else 1.0 - xover_fine(k1)
    w = sigma_ref2 * spectrum_B(k1) / btot / (2 * np.pi * k1 * k1) * cell * band   # slope variance per lattice cell
    w[0, 0] = 0.0
    # random field on the cascade's lattice; FinePhone takes the desktop lattice's components (same pattern)
    rng = np.random.default_rng(c["seed"])
    nl = c["lattice"]
    xi = (rng.standard_normal((nl, nl)) + 1j * rng.standard_normal((nl, nl))) / np.sqrt(2.0)
    if nl != n:
        idx = (m.astype(np.int64) % nl)
        xi = xi[np.ix_(idx, idx)]
    a = np.sqrt(w) / k1 * xi * np.sqrt(2.0)               # height amplitude: E|a|^2 = 2 w / k^2, slope var = w
    # loop: each frequency quantised to a multiple of 1/T (never 0: nothing stands still)
    om = omega(k1)
    w0 = 2 * np.pi / T
    mt = np.maximum(1.0, np.round(om / w0))
    omq = mt * w0
    phi = omq * T / N                                     # phase turned per frame
    p1, p2 = np.radians(c["phi"][0]), np.radians(c["phi"][1])
    gt = np.where(phi <= p1, 1.0, np.where(phi >= p2, 0.0, np.cos(0.5 * np.pi * (phi - p1) / (p2 - p1)) ** 2))
    knyq = np.pi * n / L
    gs = (k < 0.9 * knyq).astype(np.float64)              # the sheet's resolution (Nyquist margin 10 %)
    keep = gt * gs
    keep[0, 0] = 0.0
    ac = a * keep
    Ax, Az = 1j * kx * ac, 1j * kz * ac
    sx = np.empty((N, n, n), np.float32)
    sz = np.empty((N, n, n), np.float32)
    for j in range(N):
        ph = np.exp(-1j * omq * (j * T / N))
        sx[j] = (np.fft.ifft2(Ax * ph) * (n * n)).real
        sz[j] = (np.fft.ifft2(Az * ph) * (n * n)).real
    # what the sequence holds, what it leaves to roughness, what frame blending loses
    wc = w * keep * keep
    stats = dict(
        var_band_axis=float(w.sum() / 2.0),
        var_expected_axis=float(wc.sum() / 2.0),
        var_sub_axis=float((w - wc).sum() / 2.0),
        interp_loss=float((wc * np.sin(phi / 2.0) ** 2).sum() / max(wc.sum(), 1e-30)),
        freq_err_rms=float(np.sqrt((wc * ((omq - om) / om) ** 2).sum() / max(wc.sum(), 1e-30))),
        resolved=float(wc.sum() / max(w.sum(), 1e-30)),
    )
    stats["lut_major"], stats["lut_minor"], stats["mip_var_frac"] = footprint_lut(kx, kz, wc, L / n)
    return sx, sz, stats, (kx, kz, wc, omq, phi)


def footprint_lut(kx, kz, wc, texel):
    """Unresolved share of the per-axis slope variance for a footprint of 2^a texels along x and 2^b along z
    (box mip x bilinear tent ~ sinc^2 per axis), from the sequence's own spectrum. major = the x component (the
    footprint's long axis when a >= b), minor = the z component. Isotropic field: valid in any orientation."""
    vx = (wc * kx * kx / np.maximum(kx * kx + kz * kz, 1e-30)).ravel()
    vz = (wc * kz * kz / np.maximum(kx * kx + kz * kz, 1e-30)).ravel()
    vx[0] = vz[0] = 0.0
    tx, tz = vx.sum(), vz.sum()
    ux, uz = kx.ravel() * texel * 0.5, kz.ravel() * texel * 0.5
    size = len(LUT_LOG2)
    major = np.zeros((size, size))
    minor = np.zeros((size, size))
    for ia, la in enumerate(LUT_LOG2):
        hx = np.sinc(ux * (2 ** la) / np.pi) ** 2
        for ib, lb in enumerate(LUT_LOG2):
            hz = np.sinc(uz * (2 ** lb) / np.pi) ** 2
            h2 = (hx * hz) ** 2
            major[ia, ib] = float((vx * (1.0 - h2)).sum() / tx)
            minor[ia, ib] = float((vz * (1.0 - h2)).sum() / tz)
    mip = [float(0.5 * (major[2 * i, 2 * i] + minor[2 * i, 2 * i])) for i in range(10)]
    return np.round(major, 5).tolist(), np.round(minor, 5).tolist(), [round(v, 5) for v in mip]


# ------------------------------------------------------------------------------------------------ output
META_TEX = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: 0
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 1
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 16384
  textureSettings:
    serializedVersion: 2
    filterMode: 2
    aniso: 16
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 100
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 0
  alphaIsTransparency: 0
  spriteTessellationMethod: 0
  spriteTessellationDetail: -1
  spriteGeometrySubdivision: -1
  textureType: 0
  textureShape: 4
  singleChannelComponent: 0
  flipbookRows: {rows}
  flipbookColumns: {cols}
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
{platforms}  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

PLATFORM = """  - serializedVersion: 4
    buildTarget: {target}
    maxTextureSize: {size}
    resizeAlgorithm: 0
    textureFormat: {fmt}
    textureCompression: 2
    compressionQuality: 100
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: {ovr}
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
"""

# TextureImporterFormat values (UnityEditor): BC5 27 (Mac/Win: two channels, 1 byte a texel), EAC_RG 43 (Metal on
# iOS and Android: two 11-bit channels, 1 byte a texel), RG16 62 (WebGL: R8G8 is core in WebGL2, BC5 needs an
# extension mobile browsers lack; the precedent is FrontRoomsPrintArray). A stub (32 px per slice) keeps a tier's
# sheet out of another tier's memory: the shader takes the tier's own sheet, so the stub is never sampled.
STUB = 32
FORMATS = {"Standalone": 27, "iOS": 43, "Android": 43, "WebGL": 62}


def slice_sizes(name, n):
    """Per-platform slice size: the tier's own sheet at full size, every other sheet as a stub."""
    full = {"Fine": ("Standalone",), "FinePhone": ("iOS", "Android", "WebGL"),
            "Long": ("Standalone", "iOS", "Android", "WebGL")}[name]
    out = {}
    for t in ("Standalone", "iOS", "Android", "WebGL"):
        out[t] = n if t in full else STUB
    if name == "Long":
        out["WebGL"] = 64          # WebGL keeps its whole data file in memory: the long cascade at 64 px (3.75 cm)
    return out


def existing_guid(meta_path):
    if os.path.exists(meta_path):
        with open(meta_path, "r") as f:
            mm = re.search(r"^guid: ([0-9a-f]{32})", f.read(), re.M)
            if mm:
                return mm.group(1)
    return uuid.uuid4().hex


def write_text(path, text):
    with open(path, "w", newline="\n") as f:
        f.write(text)


def sha1(path):
    h = hashlib.sha1()
    with open(path, "rb") as f:
        h.update(f.read())
    return h.hexdigest()


def write_meta(png, c):
    meta = png + ".meta"
    guid = existing_guid(meta)
    sizes = slice_sizes(c["name"], c["n"])
    plats = PLATFORM.format(target="DefaultTexturePlatform", size=16384, fmt=-1, ovr=0)
    for t in ("Standalone", "Android", "WebGL", "iOS"):
        plats += PLATFORM.format(target=t, size=sizes[t], fmt=FORMATS[t], ovr=1)
    plats += PLATFORM.format(target="VisionOS", size=16384, fmt=-1, ovr=0)      # as Unity writes it back
    rows = c["frames"] // c["cols"]
    write_text(meta, META_TEX.format(guid=guid, rows=rows, cols=c["cols"], platforms=plats))
    return guid, sizes


def encode(sx, sz, enc):
    q = lambda s: np.clip(np.rint(127.5 + 127.5 * s / enc), 0, 255).astype(np.uint8)
    return q(sx), q(sz)


def write_sheet(path, qx, qz, cols):
    N, n, _ = qx.shape
    rows = N // cols
    sheet = np.zeros((rows * n, cols * n, 3), np.uint8)
    for j in range(N):
        r, cc = j // cols, j % cols
        # PNG rows run top-down; a slice's v (world z) runs bottom-up, as in any texture
        sheet[r * n:(r + 1) * n, cc * n:(cc + 1) * n, 0] = np.flipud(qx[j])
        sheet[r * n:(r + 1) * n, cc * n:(cc + 1) * n, 1] = np.flipud(qz[j])
    Image.fromarray(sheet, "RGB").save(path, optimize=True)


def folder_meta(path):
    meta = path.rstrip("/") + ".meta"
    if not os.path.exists(meta):
        write_text(meta, "fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n"
                   "  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % uuid.uuid4().hex)


def text_meta(path):
    meta = path + ".meta"
    guid = existing_guid(meta)
    write_text(meta, "fileFormatVersion: 2\nguid: %s\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n"
               "  assetBundleName: \n  assetBundleVariant: \n" % guid)
    return guid


# ------------------------------------------------------------------------------------------------ QA (outside the project)
def corr_peak(a, b):
    """Normalised circular cross-correlation of two fields: peak value and its shift (texels)."""
    fa, fb = np.fft.fft2(a - a.mean()), np.fft.fft2(b - b.mean())
    cc = np.fft.ifft2(fa * np.conj(fb)).real / (a.std() * b.std() * a.size)
    i = np.unravel_index(np.argmax(cc), cc.shape)
    n = a.shape[0]
    dz, dx = [(v + n // 2) % n - n // 2 for v in i]
    return float(cc[i]), int(dx), int(dz), float(cc[0, 0])


def qa(c, sx, sz, stats, spec, qa_dir, enc):
    kx, kz, wc, omq, phi = spec
    n, N, T, L = c["n"], c["frames"], c["T"], c["L"]
    out = dict()
    # no scrolling: frame 0 against frame j. A scrolled texture keeps a peak near 1 that walks; this field's peak
    # stays at zero shift and decays (each wavelength moves at its own speed, in every direction)
    lags = [1, 2, 4, 8, N // 4, N // 2]
    rows = []
    for lag in lags:
        v, dx, dz, v0 = corr_peak(sx[0], sx[lag % N])
        rows.append(dict(lag_frames=lag, lag_ms=round(1000 * lag * T / N, 1), peak=round(v, 3), shift_texels=[dx, dz],
                         at_zero=round(v0, 3)))
    out["no_scroll"] = rows
    # the control: frame 0 scrolled at 0.25 m/s (the energetic band's phase speed) along +x
    ctl = []
    for lag in lags:
        shift = 0.25 * lag * T / N / (L / n)
        f = np.fft.fft2(sx[0])
        kxx = np.fft.fftfreq(n)[None, :] * 2 * np.pi
        moved = np.fft.ifft2(f * np.exp(-1j * kxx * shift)).real
        v, dx, dz, v0 = corr_peak(sx[0], moved)
        ctl.append(dict(lag_frames=lag, peak=round(v, 3), shift_texels=[dx, dz]))
    out["scrolled_control"] = ctl
    # isotropy: axis variances, cross term, energy per direction (8 sectors of the half plane)
    out["var_x"], out["var_z"] = float((sx.astype(np.float64) ** 2).mean()), float((sz.astype(np.float64) ** 2).mean())
    out["corr_xz"] = float((sx.astype(np.float64) * sz).mean() / np.sqrt(out["var_x"] * out["var_z"]))
    ang = np.mod(np.arctan2(kz, kx), np.pi)
    e = [float(wc[(ang >= i * np.pi / 8) & (ang < (i + 1) * np.pi / 8)].sum()) for i in range(8)]
    out["sector_energy"] = [round(v / (sum(e) / 8), 3) for v in e]
    # the patch's mean slope variance per frame: constant (no large-area pulsing from the loop itself)
    pv = (sx.astype(np.float64) ** 2 + sz.astype(np.float64) ** 2).mean(axis=(1, 2))
    out["frame_var_spread"] = round(float(pv.std() / pv.mean()), 4)
    out["clipped_share"] = float(((np.abs(sx) > enc) | (np.abs(sz) > enc)).mean())
    # preview: raking light (azimuth 30 deg, elevation 12 deg) on frame 0 and a strip of 6 frames
    def relief(x, z):
        sc = 1.0 / (np.sqrt(stats["var_axis_measured"]) * 4.0)
        nx, nz = -x * sc, -z * sc
        ln = np.array([np.cos(np.radians(12)) * np.cos(np.radians(30)), np.sin(np.radians(12)),
                       np.cos(np.radians(12)) * np.sin(np.radians(30))])
        d = (nx * ln[0] + ln[1] + nz * ln[2]) / np.sqrt(nx * nx + 1 + nz * nz)
        return np.clip(255 * (0.5 + 2.2 * (d - ln[1])), 0, 255).astype(np.uint8)
    os.makedirs(qa_dir, exist_ok=True)
    Image.fromarray(np.flipud(relief(sx[0], sz[0]))).save(os.path.join(qa_dir, "%s_f0_relief.png" % c["name"]))
    step = max(1, N // 16)
    strip = np.concatenate([np.flipud(relief(sx[j * step][:n // 2, :n // 2], sz[j * step][:n // 2, :n // 2]))
                            for j in range(6)], axis=1)
    Image.fromarray(strip).save(os.path.join(qa_dir, "%s_strip.png" % c["name"]))
    return out


# ------------------------------------------------------------------------------------------------ main
def main():
    ap = argparse.ArgumentParser(description="FrontRooms pool ripple loops (FFT, capillary-gravity, isotropic)")
    ap.add_argument("--out", default="Assets/Resources/Rendering/Water")
    ap.add_argument("--qa", default=None, help="QA folder outside the project (numbers + previews)")
    ap.add_argument("--only", default=None)
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)
    folder_meta(a.out)
    btot = spectrum_total()
    sigma_ref2 = np.radians(SIGMA_REF_DEG) ** 2
    stats_path = os.path.join(a.out, "FR_Ripple_Stats.json")
    doc = dict(generator="Tools/lookdev/pool_water/gen_pool_ripples.py", sigma_ref_deg=SIGMA_REF_DEG,
               g=G, sigma_over_rho=SIGMA_RHO, spectrum=SPEC, crossover=XOVER, lut_log2=LUT_LOG2,
               channels="R dh/dx, G dh/dz (world x along U, world z along V), slope = (2 v - 1) * slope_enc_rad",
               lut_index="lut_*_flat[a * 19 + b]: footprint 2^lut_log2[a] texels along its long axis, 2^lut_log2[b] across",
               cascades=[])
    cas = dict()
    if os.path.exists(stats_path):
        try:
            with open(stats_path) as f:
                old = json.load(f)
            for r in old.get("cascades", []):
                if isinstance(r, dict) and "name" in r:
                    cas[r["name"]] = r
        except ValueError:
            pass
    only = set(a.only.split(",")) if a.only else None
    qa_all = dict()
    for c in CASCADES:
        if only and c["name"] not in only:
            continue
        sx, sz, st, spec = bake(c, sigma_ref2, btot)
        vx, vz = float((sx.astype(np.float64) ** 2).mean()), float((sz.astype(np.float64) ** 2).mean())
        st["var_axis_measured"] = 0.5 * (vx + vz)
        enc = 5.0 * np.sqrt(st["var_axis_measured"])
        qx, qz = encode(sx, sz, enc)
        png = os.path.join(a.out, "FR_Ripple_%s.png" % c["name"])
        write_sheet(png, qx, qz, c["cols"])
        guid, sizes = write_meta(png, c)
        # what the shader decodes (8-bit), against the float field
        dq = (qx.astype(np.float64) / 255.0 * 2.0 - 1.0) * enc - sx
        n = c["n"]
        rec = dict(name=c["name"], file="FR_Ripple_%s.png" % c["name"], guid=guid, sha1=sha1(png), bytes=os.path.getsize(png),
                   resources="Rendering/Water/FR_Ripple_%s" % c["name"], tiers=c["tiers"], band=c["band"],
                   slices=c["frames"], size=n, cols=c["cols"], rows=c["frames"] // c["cols"], patch_m=c["L"],
                   loop_s=c["T"], frame_dt_s=c["T"] / c["frames"], phase_cut_deg=list(c["phi"]), seed=c["seed"],
                   texel_mm=round(1000 * c["L"] / n, 3), slope_enc_rad=float(enc),
                   var_axis_rad2=st["var_axis_measured"], var_axis_expected_rad2=st["var_expected_axis"],
                   var_sub_axis_rad2=st["var_sub_axis"], var_band_axis_rad2=st["var_band_axis"],
                   resolved=st["resolved"], interp_loss=st["interp_loss"], freq_err_rms=st["freq_err_rms"],
                   quantise_rms_rad=float(np.sqrt((dq ** 2).mean())), slice_size_by_platform=sizes,
                   lut_major_flat=[v for row in st["lut_major"] for v in row],
                   lut_minor_flat=[v for row in st["lut_minor"] for v in row], mip_var_frac=st["mip_var_frac"])
        cas[c["name"]] = rec
        print("%-9s %4d^2 x %3d  loop %.2f s  enc %.4f rad  rms/axis %.3f deg  sub %.3f deg  resolved %.3f  D %.3f  "
              "ferr %.3f  png %.1f MiB" % (c["name"], n, c["frames"], c["T"], enc, np.degrees(np.sqrt(rec["var_axis_rad2"])),
                                           np.degrees(np.sqrt(rec["var_sub_axis_rad2"])), rec["resolved"],
                                           rec["interp_loss"], rec["freq_err_rms"], rec["bytes"] / 2 ** 20))
        if a.qa:
            qa_all[c["name"]] = qa(c, sx, sz, st, spec, a.qa, enc)
        del sx, sz, qx, qz
    doc["cascades"] = [cas[c["name"]] for c in CASCADES if c["name"] in cas]
    write_text(stats_path, json.dumps(doc, indent=1, sort_keys=True) + "\n")
    tguid = text_meta(stats_path)
    print("stats %s (guid %s, sha1 %s)" % (stats_path, tguid, sha1(stats_path)))
    if a.qa:
        write_text(os.path.join(a.qa, "qa.json"), json.dumps(qa_all, indent=1, sort_keys=True) + "\n")
        print("qa %s" % os.path.join(a.qa, "qa.json"))


if __name__ == "__main__":
    main()
