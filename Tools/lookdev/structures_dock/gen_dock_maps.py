#!/usr/bin/env python3
"""FRONTROOMSSS structures G4-a (line visual (game visual), 2026-11-10; package structures_dock, 30_plan.md P19): the dock's new surface maps
for Resources/Surfaces/Textures (FrontRooms/Surface, mesh-UV metres; FrontRoomsSurfaceTextureImporter sets every map by its name).

  Prop_DoorSlatGalv_{A,N,S}       512, 0.30 m tile: hot-dip galvanised slat steel (G90): a spangle of zinc crystals 3-12 mm, each its own
                                  grey, gloss and facet tilt; roll streaks along the slat; a little dull white zinc oxide
  Prop_TreadPlate_{A,N,S}        1024, 0.60 m tile: 1/4 in floor plate, the lentil (two-way bar) lugs 25 x 6 mm, 1.5 mm high, painted
                                  near-black and worn to bright steel on the lugs' crowns, dirt and rust specks in the valleys
                                  (S.b = per-texel metal for _FR_SHEEN)
  Prop_TreadPlateYellow_A         512, the same tile: the yellow edge paint on the same lugs (its N / S are the tread plate's)
  Prop_PaintChip_{N,S}            512, 0.50 m tile: enamel over steel with orange peel, chips (primer or rust at the core) and fine
                                  scratches, shared by the three enamel colours below
  Prop_SafetyYellow_A, Prop_PalletJackRed_A, Prop_DumpsterGreen_A
                                  512: each colour with the shared chips painted in primer grey and rust
  Prop_PalletFilm_{A,N,S}        1024 x 512 decal atlas: the wrapped loads' stretch film with the cartons seen through it (layout = the
                                  kit builder's _struct_dock.load_layout / film_regions, written to film_layout.json by the build)

House style of the zone lamps' generators (numpy + Pillow): _A sRGB albedo, _N OpenGL +Y normal, _S linear mask (R smoothness, G cavity,
B metal (with _FR_SHEEN), A 1). Seeds fixed: the maps rebuild byte for byte. No text, label, brand or logo. ASCII only.
Run: W/venv/bin/python gen_dock_maps.py <out dir> [film_layout.json]
"""
import json
import math
import os
import sys

import numpy as np
from PIL import Image

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.abspath(__file__))
LAYOUT = sys.argv[2] if len(sys.argv) > 2 else os.path.join(OUT, "film_layout.json")
os.makedirs(OUT, exist_ok=True)


# ----------------------------------------------------------------------------- helpers
def srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= .0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - .055)


def lin(c):
    c = np.asarray(c, np.float32)
    return np.where(c <= .04045, c / 12.92, ((c + .055) / 1.055) ** 2.4)


def hexl(s):
    return lin(np.array([int(s[i:i + 2], 16) / 255 for i in (1, 3, 5)], np.float32))


def save(name, arr, mode):
    a = np.clip(arr, 0, 1)
    Image.fromarray((a * 255 + .5).astype(np.uint8), mode).save(os.path.join(OUT, name), optimize=True)


def save_albedo(name, lin_rgb):
    save(name, srgb(lin_rgb), "RGB")


def save_normal(name, nrm):
    save(name, nrm * .5 + .5, "RGB")


def save_mask(name, sm, cav, met):
    h, w = sm.shape
    save(name, np.stack([sm, cav, met, np.ones((h, w), np.float32)], -1), "RGBA")


def noise(h, w, lo, hi, seed):
    """Band-limited tiling noise (FFT), 0..1; lo / hi in cycles per tile."""
    r = np.random.default_rng(seed)
    F = np.fft.fft2(r.standard_normal((h, w)))
    fy = np.fft.fftfreq(h)[:, None] * h
    fx = np.fft.fftfreq(w)[None, :] * w
    f = np.sqrt(fx * fx + fy * fy)
    m = np.exp(-((np.log(np.maximum(f, 1e-3)) - np.log((lo * hi) ** .5)) ** 2) / (2 * (np.log(hi / lo) / 2.5) ** 2))
    m[0, 0] = 0
    n = np.real(np.fft.ifft2(F * m))
    n -= n.min()
    n /= max(1e-8, n.max())
    return n.astype(np.float32)


def normal_from_height(hgt, scale_mm_per_px, depth_mm):
    """Height 0..1 (x depth_mm) over a tiling grid of scale mm per px -> OpenGL normals (+Y up the texture)."""
    hz = hgt * depth_mm
    gx = (np.roll(hz, -1, 1) - np.roll(hz, 1, 1)) / (2 * scale_mm_per_px)
    gy = -(np.roll(hz, -1, 0) - np.roll(hz, 1, 0)) / (2 * scale_mm_per_px)
    n = np.stack([-gx, -gy, np.ones_like(gx)], -1)
    return n / np.linalg.norm(n, axis=-1, keepdims=True)


def blur(a, r):
    if r <= 0:
        return a
    k = int(r)
    out = a.copy()
    for ax in (0, 1):
        acc = np.zeros_like(out)
        for d in range(-k, k + 1):
            acc += np.roll(out, d, ax)
        out = acc / (2 * k + 1)
    return out


def voronoi(h, w, n_cells, seed):
    """Tiling Voronoi: per pixel the cell id and the distance to the nearest edge (px)."""
    rng = np.random.default_rng(seed)
    pts = rng.uniform(0, 1, (n_cells, 2)) * [w, h]
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    best = np.full((h, w), 1e9, np.float32)
    second = np.full((h, w), 1e9, np.float32)
    ids = np.zeros((h, w), np.int32)
    for i, (px, py) in enumerate(pts):
        dx = np.abs(xx - px)
        dx = np.minimum(dx, w - dx)
        dy = np.abs(yy - py)
        dy = np.minimum(dy, h - dy)
        d = np.sqrt(dx * dx + dy * dy)
        closer = d < best
        second = np.where(closer, best, np.minimum(second, d))
        ids = np.where(closer, i, ids)
        best = np.where(closer, d, best)
    return ids, (second - best) * 0.5


# ----------------------------------------------------------------------------- galvanised slat
def galv():
    N, tile_mm = 512, 300.0
    px = tile_mm / N
    ids, edge = voronoi(N, N, 2600, 401)             # ~6 mm crystals over 0.3 m (a regular commercial spangle)
    rng = np.random.default_rng(402)
    tone = rng.normal(0, 1, ids.max() + 1)[ids]
    gloss = rng.uniform(0, 1, ids.max() + 1)[ids]
    tx = rng.normal(0, 1, ids.max() + 1)[ids]
    ty = rng.normal(0, 1, ids.max() + 1)[ids]
    # dendritic grain inside each crystal: fine noise, stretched along its own direction
    fine = noise(N, N, 40, 160, 403)
    streak = noise(N, N * 8, 2, 30, 404).reshape(N, 8, N).mean(1)    # roll streaks run along u (the slat's length)
    streak = np.repeat(streak.mean(1, keepdims=True), N, 1) * 0.5 + noise(N, N, 1, 6, 405) * 0.5
    oxide = np.clip((noise(N, N, 2, 12, 406) - 0.72) * 6.0, 0, 1) * np.clip((noise(N, N, 10, 60, 407) - 0.3) * 2, 0, 1)
    base = hexl("#9C9E9B")
    alb = base[None, None, :] * (1.0 + 0.025 * tone[..., None] + 0.03 * (fine[..., None] - 0.5) + 0.05 * (streak[..., None] - 0.5))
    alb = alb * (1 - 0.6 * oxide[..., None]) + hexl("#C9CAC4")[None, None, :] * (0.6 * oxide[..., None])
    edge_line = np.exp(-(edge / 0.7) ** 2)
    alb *= (1 - 0.035 * edge_line[..., None])
    sm = 0.55 + 0.12 * gloss + 0.06 * (streak - 0.5) - 0.30 * oxide - 0.04 * edge_line
    hgt = 0.5 + 0.25 * tx * 0.0 + 0.3 * fine * 0.2
    # each crystal a small facet tilt (the spangle catching the light one crystal at a time)
    ang = 0.014
    n = np.stack([ang * tx, ang * ty, np.ones_like(tx)], -1)
    nfine = normal_from_height(fine, px, 0.004)
    n = n + (nfine - [0, 0, 1]) * 1.0
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    save_albedo("Prop_DoorSlatGalv_A.png", alb)
    save_normal("Prop_DoorSlatGalv_N.png", n)
    save_mask("Prop_DoorSlatGalv_S.png", np.clip(sm, 0.05, 0.95), np.clip(1.0 - 0.25 * edge_line, 0, 1), np.ones_like(sm))
    print("galv done")


# ----------------------------------------------------------------------------- tread plate
def tread_height(N, tile_mm):
    """Lentil lugs: an elongated lens 25 x 6 mm, crowned 1.5 mm, on a 30 mm grid, alternate cells turned 90 deg (two-way bar)."""
    px = tile_mm / N
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32) * px
    pitch = 30.0
    cx = np.floor(xx / pitch)
    cy = np.floor(yy / pitch)
    lx = xx - (cx + 0.5) * pitch
    ly = yy - (cy + 0.5) * pitch
    turn = ((cx + cy) % 2) == 1
    a = np.where(turn, ly, lx)
    b = np.where(turn, lx, ly)
    L, Wd = 12.5, 3.0
    # lens shape: |b| < Wd * sqrt(1 - (a / L)^2)
    lim = Wd * np.sqrt(np.clip(1 - (a / L) ** 2, 0, 1))
    inside = np.clip(1.0 - (np.abs(b) / np.maximum(lim, 1e-3)) ** 2, 0, 1) * (np.abs(a) < L)
    hgt = np.sqrt(inside) * np.clip(1 - (a / L) ** 4, 0, 1)
    hgt = blur(hgt, 1)
    return hgt, turn


def tread():
    N, tile_mm = 1024, 600.0
    px = tile_mm / N
    hgt, turn = tread_height(N, tile_mm)
    crown = np.clip((hgt - 0.55) / 0.45, 0, 1)
    var = noise(N, N, 3, 24, 501)
    wear = np.clip(crown * (0.55 + 0.9 * var) - 0.15, 0, 1)          # paint worn off the crowns, more in places
    valley = 1.0 - np.clip(hgt * 3.0, 0, 1)
    dirt = valley * np.clip(noise(N, N, 6, 60, 502) * 1.4 - 0.2, 0, 1)
    rust = valley * np.clip((noise(N, N, 20, 120, 503) - 0.70) * 5.0, 0, 1)
    scuff = np.clip((noise(N, N, 1, 5, 504) - 0.45) * 2.0, 0, 1)
    paint = hexl("#22231F") * (1.0 + 0.10 * (noise(N, N, 2, 16, 505)[..., None] - 0.5))
    steel = hexl("#8E8D88")
    alb = paint * (1 - wear[..., None]) + steel[None, None, :] * wear[..., None]
    alb = alb * (1 - 0.55 * dirt[..., None]) + hexl("#4A4234")[None, None, :] * (0.55 * dirt[..., None])
    alb = alb * (1 - 0.6 * rust[..., None]) + hexl("#5C3418")[None, None, :] * (0.6 * rust[..., None])
    alb *= (1.0 + 0.08 * scuff[..., None])
    sm = 0.32 * (1 - wear) + 0.62 * wear - 0.18 * dirt - 0.15 * rust + 0.05 * scuff
    cav = 1.0 - 0.35 * valley * (1 - wear)
    met = np.clip(wear * 1.1, 0, 1)
    n = normal_from_height(hgt, px, 1.5)
    save_albedo("Prop_TreadPlate_A.png", alb)
    save_normal("Prop_TreadPlate_N.png", n)
    save_mask("Prop_TreadPlate_S.png", np.clip(sm, 0.05, 0.95), np.clip(cav, 0, 1), met)
    # the yellow edge paint on the same plate (half resolution albedo)
    paint_y = hexl("#D2A614") * (1.0 + 0.08 * (noise(N, N, 2, 16, 506)[..., None] - 0.5))
    wear_y = np.clip(wear * 1.15, 0, 1)
    aly = paint_y * (1 - wear_y[..., None]) + steel[None, None, :] * wear_y[..., None]
    aly = aly * (1 - 0.6 * dirt[..., None]) + hexl("#4A4234")[None, None, :] * (0.6 * dirt[..., None])
    aly = aly * (1 - 0.6 * rust[..., None]) + hexl("#5C3418")[None, None, :] * (0.6 * rust[..., None])
    aly = aly.reshape(N // 2, 2, N // 2, 2, 3).mean((1, 3))
    save_albedo("Prop_TreadPlateYellow_A.png", aly)
    print("tread done")


# ----------------------------------------------------------------------------- chipped enamel (shared N / S, three albedos)
def chips():
    N, tile_mm = 512, 500.0
    px = tile_mm / N
    rng = np.random.default_rng(601)
    chip = np.zeros((N, N), np.float32)
    core = np.zeros((N, N), np.float32)
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    edge_noise = noise(N, N, 20, 90, 602)
    for k in range(130):
        cx, cy = rng.uniform(0, N, 2)
        r = rng.choice([1.5, 2.5, 3.5, 5.0, 7.0, 10.0], p=[.32, .26, .2, .12, .07, .03]) / px
        ex = rng.uniform(0.5, 1.6)
        ang = rng.uniform(0, math.pi)
        dx = (xx - cx + N / 2) % N - N / 2
        dy = (yy - cy + N / 2) % N - N / 2
        u = (dx * math.cos(ang) + dy * math.sin(ang)) / (r * ex)
        v = (-dx * math.sin(ang) + dy * math.cos(ang)) / (r / ex)
        d = np.sqrt(u * u + v * v) + (edge_noise - 0.5) * 0.9
        chip = np.maximum(chip, (d < 1.0).astype(np.float32))
        core = np.maximum(core, (d < 0.40).astype(np.float32) * (1.0 if r * px > 4.5 else 0.0))
    chip = blur(chip, 1)
    core = blur(core, 1)
    # fine scratches: thin lines at random angles
    scratch = np.zeros((N, N), np.float32)
    for k in range(70):
        x0, y0 = rng.uniform(0, N, 2)
        ang = rng.uniform(0, math.pi)
        L = rng.uniform(15, 80) / px
        t = np.linspace(0, 1, int(L * 2))
        xs = ((x0 + np.cos(ang) * L * t) % N).astype(int)
        ys = ((y0 + np.sin(ang) * L * t) % N).astype(int)
        scratch[ys, xs] = np.maximum(scratch[ys, xs], rng.uniform(0.4, 1.0))
    scratch = blur(scratch, 1) * 2.2
    peel = noise(N, N, 25, 110, 603)
    hgt = 0.6 * (1 - chip) + 0.12 * peel * (1 - chip) - 0.15 * scratch
    n = normal_from_height(hgt, px, 0.12)
    sm = (0.48 + 0.06 * (peel - 0.5)) * (1 - chip) + (0.28 * (1 - core) + 0.16 * core) * chip - 0.12 * np.clip(scratch, 0, 1)
    cav = 1.0 - 0.15 * chip
    save_normal("Prop_PaintChip_N.png", n)
    save_mask("Prop_PaintChip_S.png", np.clip(sm, 0.05, 0.95), np.clip(cav, 0, 1), np.zeros_like(sm))
    mott = noise(N, N, 2, 14, 604)
    dirt = np.clip(noise(N, N, 4, 40, 605) - 0.55, 0, 1) * 1.2
    primer, rust = hexl("#77746C"), hexl("#5E3518")
    for name, col, rust_share in (("Prop_SafetyYellow_A.png", "#D9AE16", 0.22), ("Prop_PalletJackRed_A.png", "#B01E18", 0.25), ("Prop_DumpsterGreen_A.png", "#2E513A", 0.6)):
        base = hexl(col)[None, None, :] * (1.0 + 0.06 * (mott[..., None] - 0.5))
        under = primer * (1 - rust_share) + rust * rust_share
        chipcol = under[None, None, :] * (1 - core[..., None]) + rust[None, None, :] * core[..., None]
        alb = base * (1 - chip[..., None]) + chipcol * chip[..., None]
        alb *= (1.0 - 0.10 * np.clip(scratch, 0, 1)[..., None])
        alb = alb * (1 - 0.25 * dirt[..., None]) + hexl("#4E473C")[None, None, :] * 0.25 * dirt[..., None]
        save_albedo(name, alb)
    print("chips done")


# ----------------------------------------------------------------------------- pallet film atlas
def film():
    lay = json.load(open(LAYOUT))
    TW, TH = lay["tex"]
    A = np.zeros((TH, TW, 3), np.float32)
    A[:] = hexl("#DCDCD6") * 0.8
    Nn = np.zeros((TH, TW, 3), np.float32)
    Nn[..., 2] = 1
    Sm = np.full((TH, TW), 0.86, np.float32)
    rng = np.random.default_rng(701)
    kraft = hexl("#B08C62")
    kraft2 = hexl("#A07E56")
    tape = hexl("#C9A46A")
    haze = hexl("#DCDCD6")
    for key, (x0, w, y0, h) in lay["regions"].items():
        variant, face = key.split("_", 1)
        V = lay["variants"][variant]
        layers = V["layers"]
        span = V["span"]
        face_len = V["faces"][face]["len"]
        start = V["faces"][face]["start"]
        dirn = V["faces"][face]["dir"]
        horiz = abs(dirn[0]) > 0.5
        X0, X1 = int(round(x0)), int(round(x0 + w))
        Y0, Y1 = int(round(y0)), int(round(y0 + h))
        ww, hh = X1 - X0, Y1 - Y0
        s = (np.arange(ww, dtype=np.float32) + 0.5) / ww * face_len                  # metres along the face
        hgt = (np.arange(hh, dtype=np.float32) + 0.5) / hh * span                    # metres up from the film's bottom
        S_, H_ = np.meshgrid(s, hgt)
        Yw = H_ + V["y0"]                                                             # world height
        top_edge = V["y_top"] - 0.012
        over = Yw > top_edge
        # world coordinate along the face (x or z) for each s
        coord = start[0 if horiz else 1] + (dirn[0] if horiz else dirn[1]) * S_
        col = np.zeros((hh, ww, 3), np.float32)
        nrm_h = np.zeros((hh, ww), np.float32)
        seam = np.zeros((hh, ww), np.float32)
        on_carton = np.zeros((hh, ww), bool)
        pallet = Yw < V["pallet_top"]
        for L in layers:
            cuts = L["xs"] if horiz else L["zs"]
            inl = (Yw >= L["y0"]) & (Yw < L["y1"])
            for i in range(len(cuts) - 1):
                a, b = min(cuts[i], cuts[i + 1]), max(cuts[i], cuts[i + 1])
                inc = inl & (coord >= a) & (coord < b)
                if not inc.any():
                    continue
                on_carton |= inc
                tone = 1.0 + rng.uniform(-0.06, 0.06)
                c = kraft * tone if rng.uniform() < 0.7 else kraft2 * tone
                col[inc] = c
                # joints: dark gaps at the carton edges (3 mm), the top flap's crease 25 mm under the carton's top
                dj = np.minimum(np.abs(coord - a), np.abs(coord - b))
                dy = np.minimum(np.abs(Yw - L["y0"]), np.abs(Yw - L["y1"]))
                seam = np.maximum(seam, inc * np.exp(-(dj / 0.003) ** 2))
                seam = np.maximum(seam, inc * np.exp(-(dy / 0.003) ** 2))
                crease = inc * np.exp(-((Yw - (L["y1"] - 0.025)) / 0.0015) ** 2) * 0.35
                seam = np.maximum(seam, crease)
                # the tape's end down the face at the carton's centre
                mid = (a + b) / 2.0
                tp = inc & (np.abs(coord - mid) < 0.024) & (Yw > L["y1"] - 0.06)
                col[tp] = tape * (1.0 + rng.uniform(-0.04, 0.04))
        col[~on_carton] = hexl("#6E5E46")                      # the pallet's top boards and gaps under the film
        col[pallet] = hexl("#9C8160") * 0.85
        flute = 0.03 * np.sin(coord * 2 * math.pi / 0.0085)    # the liner over the corrugation, faint
        col *= (1.0 + flute[..., None] * on_carton[..., None])
        col *= (1.0 - 0.85 * seam[..., None])
        # the film: hazy layers, more at the bottom wraps; the spiral band edges at a slight incline; wrinkles; the over-wrap brighter
        band_w = 0.50
        incl = 0.06
        phase = (Yw - incl * S_) / (band_w * 0.5)
        layers_n = 2.0 + (np.floor(phase) % 2) + np.clip(2.5 - (Yw - V["y0"]) / 0.12, 0, 2.5)
        layers_n = layers_n + np.where(over, 2.0, 0.0)
        cover = 1.0 - np.exp(-0.085 * layers_n)
        band_edge = np.exp(-(((phase % 1.0) - 0.5) * band_w * 0.5 / 0.004) ** 2) * 0.0 + np.exp(-((phase - np.round(phase)) * band_w * 0.5 / 0.003) ** 2)
        wr = noise(hh, ww, 2, 40, 702 + Y0)
        wrinkle_h = np.sin((Yw * 140.0 + 9.0 * wr)) * 0.5 + 0.5
        wrinkle_h = wrinkle_h * (0.35 + 0.65 * noise(hh, ww, 3, 30, 703 + X0))
        ec = np.minimum(S_, face_len - S_)
        corner = np.exp(-(ec / 0.05) ** 2)
        crease_v = np.abs(np.sin(S_ * 90.0 + 7.0 * wr)) * corner
        col = col * (1 - cover[..., None]) + haze[None, None, :] * cover[..., None]
        col *= (1.0 + 0.10 * band_edge[..., None] + 0.05 * (wrinkle_h[..., None] - 0.5))
        col = np.where(over[..., None], col * 1.06, col)
        A[TH - Y1:TH - Y0, X0:X1] = col[::-1]
        nh = 0.25 * wrinkle_h + 0.35 * crease_v - 0.4 * seam * (1 - cover) + 0.3 * band_edge
        n = normal_from_height(nh, 4.2, 0.6)
        Nn[TH - Y1:TH - Y0, X0:X1] = n[::-1]
        sm = 0.88 - 0.10 * corner - 0.05 * wrinkle_h + 0.03 * band_edge
        Sm[TH - Y1:TH - Y0, X0:X1] = sm[::-1]
    # the tail / rope strip: film over nothing
    tx0, tx1 = lay["tail_x"]
    strip = hexl("#9A9A94")
    A[:, tx0:tx1 + 1] = strip
    Sm[:, tx0:tx1 + 1] = 0.82
    save_albedo("Prop_PalletFilm_A.png", A)
    save_normal("Prop_PalletFilm_N.png", Nn)
    save_mask("Prop_PalletFilm_S.png", np.clip(Sm, 0.05, 0.97), np.ones_like(Sm), np.zeros_like(Sm))
    print("film done")


if __name__ == "__main__":
    which = set(sys.argv[3:])
    for name, fn in (("galv", galv), ("tread", tread), ("chips", chips), ("film", film)):
        if not which or name in which:
            fn()
