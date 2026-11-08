"""FRONTROOMSSS lamp options: texture maps for L01-L10 (lamp_options workflow, build stage, 2026-10-31).

Run: venv/bin/python gen_lamp_textures.py <layout dir (build_lamps.py out)> <tex out dir> [IDs]
House style follows Tools/lookdev/gen_common.py (numpy + Pillow, periodic noise, OpenGL +Y normals):
  <name>_A  sRGB albedo        <name>_N  linear normal (OpenGL +Y)
  <name>_S  linear mask: R smoothness, G cavity, B metallic (read only with _FR_SHEEN), A 1
  <name>_E  emission, stored sRGB-encoded so the GPU decodes the LINEAR value painted here
  <name>_F  L05 only: _FR_LAMP_FIELD map, linear: R outer-pair light, G inner-pair light, B 0, A transmission
Every region is painted in physical millimetres from the UV layout that build_lamps.py wrote.
Emission is relative: the lens band peak is 1.0. The lamp's level comes from _EmissionColor (the
existing driver), so off / stutter / dead stay renderable. No text, label, brand or logo is drawn.
ASCII only.
"""
import json
import math
import os
import sys
import numpy as np
from PIL import Image

LAY, OUT = sys.argv[1], sys.argv[2]
ONLY = set(sys.argv[3:])
os.makedirs(OUT, exist_ok=True)
MAIN_LOOKDEV = None


# ----------------------------------------------------------------------- house helpers (gen_common)
def band(h, w, lo, hi, seed):
    r = np.random.default_rng(seed)
    F = np.fft.fft2(r.standard_normal((h, w)))
    fy = np.fft.fftfreq(h)[:, None] * h
    fx = np.fft.fftfreq(w)[None, :] * w
    f = np.sqrt(fx * fx + fy * fy)
    mask = np.exp(-((np.log(np.maximum(f, 1e-3)) - np.log((lo * hi) ** .5)) ** 2) / (2 * (np.log(hi / lo) / 2.5) ** 2))
    mask[0, 0] = 0
    n = np.real(np.fft.ifft2(F * mask))
    n -= n.min(); n /= max(1e-8, n.max())
    return n.astype(np.float32)


def spectral(h, w, beta, seed):
    r = np.random.default_rng(seed)
    F = np.fft.fft2(r.standard_normal((h, w)))
    fy = np.fft.fftfreq(h)[:, None] * h
    fx = np.fft.fftfreq(w)[None, :] * w
    f = np.sqrt(fx * fx + fy * fy); f[0, 0] = 1
    amp = 1.0 / np.power(np.maximum(f, 1.0), beta); amp[0, 0] = 0
    n = np.real(np.fft.ifft2(F * amp))
    n -= n.min(); n /= max(1e-8, n.max())
    return n.astype(np.float32)


def lin_to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= .0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - .055)


def hexc(s):
    return np.array([int(s[i:i + 2], 16) / 255 for i in (1, 3, 5)], np.float32)


def g(d, s):
    return np.exp(-(d / s) ** 2)


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def save(path, arr, mode):
    a = np.clip(arr, 0, 1)
    Image.fromarray((a * 255 + .5).astype(np.uint8), mode).save(path, optimize=True)


def down2(arr):
    h, w = arr.shape[:2]
    return arr.reshape(h // 2, 2, w // 2, 2, *arr.shape[2:]).mean(axis=(1, 3))


# ----------------------------------------------------------------------- canvas
class Canvas:
    """All channels for one atlas. Regions are painted in place; gutters are dilated after."""

    def __init__(self, tw, th):
        self.tw, self.th = tw, th
        self.alb = np.zeros((th, tw, 3), np.float32)
        self.nrm = np.zeros((th, tw, 3), np.float32); self.nrm[..., 2] = 1
        self.msk = np.zeros((th, tw, 4), np.float32); self.msk[..., 1] = 1; self.msk[..., 3] = 1
        self.emi = np.zeros((th, tw, 3), np.float32)
        self.fld = np.zeros((th, tw, 4), np.float32)
        self.cov = np.zeros((th, tw), bool)

    def grid(self, r):
        """(rows, cols) slices and a (0..1 across width), b (0..1 up the height) for a region."""
        x, y, w, h = r["x"], r["y"], r["w"], r["h"]
        rows = slice(self.th - y - h, self.th - y)
        cols = slice(x, x + w)
        jj, ii = np.meshgrid(np.arange(w, dtype=np.float32), np.arange(h, dtype=np.float32))
        a = jj / max(w - 1, 1)
        b = (h - 1 - ii) / max(h - 1, 1)
        return rows, cols, a, b

    def put(self, r, alb=None, nrm=None, sm=None, cav=None, met=None, emi=None, fld=None):
        rows, cols, a, b = self.grid(r)
        shp = a.shape
        if alb is not None: self.alb[rows, cols] = np.broadcast_to(alb, shp + (3,))
        if nrm is not None: self.nrm[rows, cols] = nrm
        if sm is not None: self.msk[rows, cols, 0] = sm
        if cav is not None: self.msk[rows, cols, 1] = cav
        if met is not None: self.msk[rows, cols, 2] = met
        if emi is not None: self.emi[rows, cols] = emi if np.ndim(emi) == 3 else np.repeat(np.asarray(emi, np.float32)[..., None] * np.ones(shp + (1,), np.float32), 3, -1)
        if fld is not None: self.fld[rows, cols] = fld
        self.cov[rows, cols] = True

    def dilate(self, n=10):
        cov = self.cov.copy()
        chans = [self.alb, self.nrm, self.msk, self.emi, self.fld]
        for _ in range(n):
            new = cov.copy()
            for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
                src = np.roll(cov, (dy, dx), (0, 1))
                take = src & ~new
                if not take.any():
                    continue
                for c in chans:
                    c[take] = np.roll(c, (dy, dx), (0, 1))[take]
                new |= take
            cov = new

    def write(self, name, half_as=True, emission=True, field=False):
        alb, msk = self.alb, self.msk
        if half_as:
            alb, msk = down2(alb), down2(msk)
        save(f"{OUT}/{name}_A.png", alb, "RGB")
        save(f"{OUT}/{name}_N.png", self.nrm * .5 + .5, "RGB")
        save(f"{OUT}/{name}_S.png", msk, "RGBA")
        if emission:
            save(f"{OUT}/{name}_E.png", lin_to_srgb(self.emi), "RGB")
        if field:
            save(f"{OUT}/{name}_F.png", self.fld, "RGBA")


def normal_from_grad(gx, gy):
    """gx, gy: height slope along the region's tangent (u) and bitangent (v), mm per mm."""
    n = np.stack([-gx, -gy, np.ones_like(gx)], -1)
    return n / np.linalg.norm(n, axis=-1, keepdims=True)


def peel(shape, seed, amp=0.035):
    """Orange-peel paint: band noise turned into small slopes."""
    h, w = shape
    n = band(h, w, max(4, w / 18), max(8, w / 5), seed)
    gx = (np.roll(n, -1, 1) - np.roll(n, 1, 1)) * amp * 8
    gy = (np.roll(n, 1, 0) - np.roll(n, -1, 0)) * amp * 8
    return normal_from_grad(gx, gy), n


# ----------------------------------------------------------------------- finishes (sRGB albedo, smoothness, metal)
FIN = {
    "tbar": (hexc("#E3DFD3"), .42, 0.0),     # matches the printed grid's T-bar tone (gen_surfaces ceiling)
    "enamel": (hexc("#DDD7C9"), .50, 0.0),   # baked white enamel, about 15 years old (PaintedMetal_A avg 211/205/189)
    "door": None,
    "alu": (hexc("#C6C8CA"), .58, 1.0),      # natural aluminium, brushed
    "black": (hexc("#1D1C1A"), .20, 0.0),    # matte black paint
    "zinc": (hexc("#B4B6B3"), .50, 1.0),     # zinc-plated steel hardware
    "holder": (hexc("#E8E3D6"), .48, 0.0),   # white urea lampholder
    "rim_alu": (hexc("#C2C5C8"), .70, 1.0),  # semi-specular louver aluminium
    "tile": (hexc("#D6D2C6"), .07, 0.0),     # mineral-fibre tile edge
    "trim": (hexc("#E2DDD0"), .52, 0.0),     # can trim ring, white steel
}


def paint_swatch(cv, r, finish, door_finish):
    f = door_finish if finish == "door" else finish
    col, sm, met = FIN[f]
    cv.put(r, alb=col, sm=sm, cav=1.0, met=met, emi=0.0)


# ----------------------------------------------------------------------- lensed family
def ret_profile(spec):
    kind, h, inset, lip = spec["ret"]
    if kind == "slope":
        return [(-(inset + lip), h + 0.5), (-(inset + lip), h), (-inset, h), (0.0, 1.0)]
    if kind == "regress":
        dr = h * math.tan(math.radians(inset))
        return [(-(dr + lip), h + 0.5), (-(dr + lip), h), (-dr, h), (0.0, 0.8)]
    return [(0.0, h + 0.5), (0.0, 1.0)]


def u_path_distance(x, y, xa, xb, y_sock, y_far):
    """Distance to a U-lamp centreline and the arc length from the socket (both legs start there)."""
    rb = abs(xb - xa) / 2
    xc = (xa + xb) / 2
    yb = y_far - rb
    best = np.full(x.shape, 1e9, np.float32)
    sarr = np.zeros(x.shape, np.float32)
    leg = yb - y_sock
    for xl, sgn in ((xa, 0), (xb, 1)):
        yy = np.clip(y, y_sock, yb)
        d = np.hypot(x - xl, y - yy)
        s = (yy - y_sock) if sgn == 0 else (yy - y_sock)
        take = d < best
        best = np.where(take, d, best)
        sarr = np.where(take, s, sarr)
    ang = np.arctan2(y - yb, x - xc)
    on = (y >= yb)
    d = np.abs(np.hypot(x - xc, y - yb) - rb)
    s = leg + rb * (np.pi - np.clip(ang, 0, np.pi))
    take = on & (d < best)
    best = np.where(take, d, best)
    sarr = np.where(take, np.minimum(s, 2 * leg + np.pi * rb - s), sarr)
    return best, sarr


class LensModel:
    """Emission, albedo and film of one lensed troffer, as functions of lens mm (x, y)."""

    def __init__(self, oid, spec, W, L):
        self.oid, self.spec, self.W, self.L = oid, spec, W, L
        kind, pos, depth = spec["tubes"]
        self.kind, self.pos = kind, pos
        self.empty = spec.get("empty", [])
        n = len(pos) if kind == "straight" else 4
        if oid == "L04":
            self.b0, self.bb, self.sb = 0.36, 0.14, 155.0
        elif oid == "L09":
            self.b0, self.bb, self.sb = 0.24, 0.14, 90.0
        else:
            self.b0, self.bb, self.sb = 0.20, 0.16, 120.0
        xs = np.linspace(-W / 2, W / 2, 1025, dtype=np.float32)
        self.norm = 1.0
        e = self.raw(xs, np.zeros_like(xs))
        self.norm = float(e.max())

    def bands(self, x, y, which=None):
        """Tube images through the prisms. which: None all, 'outer' / 'inner' pair (4-lamp)."""
        out = np.zeros(x.shape, np.float32)
        dist_end = self.L / 2 - np.abs(y)
        cath = 0.62 + 0.38 * smooth(18, 85, dist_end)
        if self.kind == "straight":
            for t in self.pos:
                if which == "outer" and abs(t) < 100: continue
                if which == "inner" and abs(t) >= 100: continue
                out += (0.30 * g(x - t, 21) + 0.12 * g(x - t, 48)) * cath
        else:
            xs = self.pos
            ysock, yfar = -self.L / 2 - 25.0, self.L / 2 + 23.0
            for k, (xa, xb) in enumerate(((xs[0], xs[1]), (xs[2], xs[3]))):
                if which == "outer" and k == 1: continue
                if which == "inner" and k == 0: continue
                d, s = u_path_distance(x, y, xa, xb, ysock, yfar)
                c = 0.60 + 0.40 * smooth(20, 90, s)
                bend = 1.0 + 0.08 * (y > (yfar - abs(xb - xa) / 2))
                out += (0.30 * g(d, 21) + 0.12 * g(d, 48)) * c * bend
        return out

    def broad(self, x, y, which=None):
        out = np.full(x.shape, self.b0 if which is None else self.b0 / 2, np.float32)
        centres = self.pos if self.kind == "straight" else [(self.pos[0] + self.pos[1]) / 2, (self.pos[2] + self.pos[3]) / 2]
        for t in centres:
            if which == "outer" and abs(t) < 100 and self.kind == "straight": continue
            if which == "inner" and abs(t) >= 100 and self.kind == "straight": continue
            if self.kind != "straight" and which == "outer" and t > 0: continue
            if self.kind != "straight" and which == "inner" and t < 0: continue
            out += self.bb * g(x - t, self.sb)
        dist_end = self.L / 2 - np.abs(y)
        out *= 0.80 + 0.20 * smooth(0, 150, dist_end)
        if self.kind == "straight" and len(self.pos) == 4:
            out *= 1.0 - 0.06 * g(x, 38)            # ballast cover strip between the inner pair
        return out

    def raw(self, x, y, which=None):
        return self.broad(x, y, which) + self.bands(x, y, which)

    def holders(self, x, y):
        """Lampholder shadows at the tube ends (all positions, filled or empty)."""
        out = np.zeros(x.shape, np.float32)
        if self.kind != "straight":
            for xl in self.pos:
                out = np.maximum(out, g(x - xl, 13) * smooth(-self.L / 2 + 22, -self.L / 2 + 4, y))
            return out
        for xl in list(self.pos) + list(self.empty):
            out = np.maximum(out, g(x - xl, 13) * (smooth(self.L / 2 - 22, self.L / 2 - 4, np.abs(y))))
        return out

    def tube_shadow(self, x, y):
        out = np.zeros(x.shape, np.float32)
        if self.kind == "straight":
            for t in self.pos:
                out += g(x - t, 19)
        else:
            xs = self.pos
            for xa, xb in ((xs[0], xs[1]), (xs[2], xs[3])):
                d, s = u_path_distance(x, y, xa, xb, -self.L / 2 - 25.0, self.L / 2 + 23.0)
                out += g(d, 19)
        return np.clip(out, 0, 1)

    def black_ends(self, x, y, length=65.0):
        out = np.zeros(x.shape, np.float32)
        dist_end = self.L / 2 - np.abs(y)
        if self.kind == "straight":
            for t in self.pos:
                out = np.maximum(out, g(x - t, 17) * smooth(length, length * 0.45, dist_end))
        return out



def prisms(x, y, pitch=4.8, height=2.0):
    """Pattern 12 cone prisms on a square grid (S11): returns height 0..1 and slopes (mm/mm)."""
    fx = (x / pitch) % 1.0 - 0.5
    fy = (y / pitch) % 1.0 - 0.5
    r = np.hypot(fx, fy) * pitch
    R = pitch / 2
    h = np.clip(1 - r / R, 0, 1)
    k = (height / R) * (r < R) * 0.6                 # 0.6: the refracted look, not a full 40 deg facet
    rr = np.maximum(r, 1e-4)
    gx = -k * (fx * pitch) / rr
    gy = -k * (fy * pitch) / rr
    return h, gx, gy


def paint_lensed(lay, variant=None):
    oid, spec = lay["id"], lay["spec"]
    A = lay["atlas"]
    cv = Canvas(*A["size"])
    regs = A["regions"]
    lr = regs["lens"]
    W, L = lr["W"], lr["L"]
    M = LensModel(oid, spec, W, L)
    seed = int(oid[1:]) * 101 + (7 if variant else 0)
    door_finish = spec["door"][0]
    aged = oid == "L05"

    # --- lens plate
    rows, cols, a, b = cv.grid(lr)
    x = (a - 0.5) * W
    y = (b - 0.5) * L
    ph, gx, gy = prisms(x, y)
    h, w = a.shape
    lowf = spectral(h, w, 2.2, seed)
    E = M.raw(x, y) / M.norm
    E *= 1.0 - 0.10 * M.holders(x, y)
    E *= 0.965 + 0.07 * lowf                           # hot and dim regions (phosphor / age)
    E *= 0.95 + 0.05 * (1 - ph)                         # prism lattice, visible up close only
    corner = np.exp(-((W / 2 - np.abs(x)) / 55)) * np.exp(-((L / 2 - np.abs(y)) / 55))
    # ageing only where the option's section calls for it: L01 (dead: dust in the corners),
    # L03 (film, soil), L05 (old mixed stock)
    if oid not in ("L01", "L03", "L05"):
        corner = corner * 0.0
    E *= 1 - 0.10 * corner
    alb = hexc("#CBC9C1")[None, None, :] * np.ones((h, w, 3), np.float32)
    alb *= (1 - 0.075 * M.tube_shadow(x, y))[..., None]
    alb *= (1 - 0.05 * M.holders(x, y))[..., None]
    if spec["tubes"][0] == "straight" and len(spec["tubes"][1]) >= 4:
        alb *= (1 - 0.035 * g(x, 40))[..., None]
    alb *= (0.975 + 0.03 * (1 - ph))[..., None]
    dust = hexc("#8E8270")
    cd = np.clip(corner * 1.2, 0, 1) * (0.55 + 0.45 * band(h, w, 6, 30, seed + 3))
    alb = alb * (1 - 0.30 * cd[..., None]) + dust * (0.30 * cd[..., None])
    sm = 0.80 - 0.25 * cd
    T = np.ones_like(E)                                 # transmission (film)
    if oid == "L03":
        # air-handling: film gathers at the lens edge nearest the channel (long sides)
        film = np.exp(-((W / 2 - np.abs(x)) / 45)) * (0.6 + 0.4 * band(h, w, 4, 20, seed + 5))
        film = np.clip(film + 0.18, 0, 1)
        alb = alb * (1 - 0.22 * film[..., None]) + hexc("#7A6142") * (0.22 * film[..., None])
        T *= 1 - 0.15 * film
        sm = sm - 0.2 * film
    if aged:
        # L05: tar and dust film, black tube ends 50-80 mm, dust in the corners stronger
        film = 0.35 + 0.65 * spectral(h, w, 2.0, seed + 9)
        alb = alb * (1 - 0.12 * film[..., None]) + hexc("#806A48") * (0.12 * film[..., None])
        be = M.black_ends(x, y, 72.0)
        alb *= (1 - 0.35 * be)[..., None]
        T *= (0.90 - 0.06 * film) * (1 - 0.45 * be)
        sm = sm - 0.15 * film
    E_out = E * T
    nrm = normal_from_grad(gx, gy)
    if variant == "delamped":
        # L05 delamped (outboard pair kept): albedo of L04, aged
        M4 = LensModel("L04", dict(spec, tubes=["straight", [-195, 195], 70], empty=[-65, 65]), W, L)
        alb2 = hexc("#CBC9C1")[None, None, :] * np.ones((h, w, 3), np.float32)
        alb2 *= (1 - 0.075 * M4.tube_shadow(x, y))[..., None] * (1 - 0.05 * M4.holders(x, y))[..., None]
        alb2 *= (0.975 + 0.03 * (1 - ph))[..., None]
        alb2 = alb2 * (1 - 0.30 * cd[..., None]) + dust * (0.30 * cd[..., None])
        film = 0.35 + 0.65 * spectral(h, w, 2.0, seed + 9)
        alb2 = alb2 * (1 - 0.12 * film[..., None]) + hexc("#806A48") * (0.12 * film[..., None])
        alb = alb2
    fld = None
    emi = E_out
    if aged:
        Ro = M.raw(x, y, "outer") / M.norm
        Gi = M.raw(x, y, "inner") / M.norm
        mod = (1.0 - 0.10 * M.holders(x, y)) * (0.965 + 0.07 * lowf) * (0.95 + 0.05 * (1 - ph)) * (1 - 0.10 * corner)
        fld = np.stack([Ro * mod, Gi * mod, np.zeros_like(Ro), T], -1)
        if variant == "mixed":
            warm = np.array([1.0, 0.86, 0.66], np.float32)
            cool = np.array([0.93, 0.98, 1.0], np.float32)
            emi = (Ro * mod * T)[..., None] * cool + (Gi * mod * T)[..., None] * warm
    cv.put(lr, alb=alb, nrm=nrm, sm=sm, cav=1.0, met=0.0, emi=emi, fld=fld)

    # --- returns / lit walls (on the lens renderer: they die with the lamp)
    prof = ret_profile(spec)
    seglen = [math.hypot(prof[i + 1][0] - prof[i][0], prof[i + 1][1] - prof[i][1]) for i in range(len(prof) - 1)]
    tt = sum(seglen)
    kind = spec["ret"][0]
    wall_fin = "alu" if kind == "regress" else "enamel"
    hx_l, hy_l = W / 2, L / 2
    for s in range(4):
        r = regs["ret%d" % s]
        rows, cols, a, b = cv.grid(r)
        t = a * tt
        Ls = r["length"]
        along = (b - 0.5) * Ls
        # the lens edge point next to this return strip
        if s == 0: ex, ey = np.full_like(along, hx_l - 2), np.clip(along, -hy_l, hy_l)
        elif s == 2: ex, ey = np.full_like(along, -hx_l + 2), np.clip(-along, -hy_l, hy_l)
        elif s == 1: ex, ey = np.clip(-along, -hx_l, hx_l), np.full_like(along, hy_l - 2)
        else: ex, ey = np.clip(along, -hx_l, hx_l), np.full_like(along, -hy_l + 2)
        edge = M.raw(ex, ey) / M.norm
        edge = np.clip(edge / 0.75, 0.3, 1.25)
        if len(prof) == 4:
            t_wall0 = seglen[0] + seglen[1]
            on_wall = t >= t_wall0
            sw = np.clip((t - t_wall0) / max(tt - t_wall0, 1e-3), 0, 1)
        else:
            on_wall = np.ones_like(t, bool)
            sw = np.clip(t / tt, 0, 1)
        hgt = spec["ret"][1]
        top, low = (0.36, 0.08) if kind != "regress" else (0.40, 0.07)
        Ew = (low + (top - low) * (1 - sw) ** 1.4) * edge
        Elip = 0.24 * edge
        Er = np.where(on_wall, Ew, Elip)
        col, smv, met = FIN[wall_fin]
        hh, ww = a.shape
        nrm, pn = peel((hh, ww), seed + 20 + s, 0.02 if wall_fin == "alu" else 0.03)
        if wall_fin == "alu":
            streak = band(hh, ww * 4, 2, 60, seed + 30 + s)[:, ::4]
            albr = col * (0.94 + 0.08 * streak[..., None])
        else:
            albr = col * (0.97 + 0.04 * pn[..., None])
        cav = np.where(on_wall, 0.85 + 0.15 * sw, 0.75)
        if kind == "vertical" and oid == "L03":
            Er *= 1.0
        if aged:
            albr = albr * 0.93 + hexc("#806A48") * 0.05
        fl = None
        if aged:
            eo = M.raw(ex, ey, "outer") / M.norm
            ei = M.raw(ex, ey, "inner") / M.norm
            tot = np.maximum(eo + ei, 1e-4)
            fl = np.stack([Er * eo / tot, Er * ei / tot, np.zeros_like(Er), np.full_like(Er, 0.92)], -1)
        Eout = Er * (0.92 if aged else 1.0)
        if aged and variant == "mixed":
            warm = np.array([1.0, 0.86, 0.66], np.float32)
            cool = np.array([0.93, 0.98, 1.0], np.float32)
            Eout = (fl[..., 0] * 0.92)[..., None] * cool + (fl[..., 1] * 0.92)[..., None] * warm
        cv.put(r, alb=albr, nrm=nrm, sm=np.where(on_wall, smv, 0.45), cav=cav, met=np.where(on_wall, met, met), emi=Eout, fld=fl)

    for n, r in regs.items():
        if r.get("kind") == "swatch":
            paint_swatch(cv, r, r["finish"], door_finish)
            if aged:
                rows, cols, a, b = cv.grid(r)
                cv.fld[rows, cols] = (0, 0, 0, 1)
    cv.dilate(12)
    return cv


# ----------------------------------------------------------------------- L05 door open
def paint_open(lay):
    A = lay["open_atlas"]
    cv = Canvas(*A["size"])
    regs = A["regions"]
    spec = lay["spec"]
    xs = spec["tubes"][1]
    hyo = 587.0
    seed = 505
    cav_col = hexc("#DCD5C4")
    for n, r in regs.items():
        rows, cols, a, b = cv.grid(r)
        h, w = a.shape
        nz = spectral(h, w, 1.8, seed + len(n))
        k = r["kind"]
        if k == "swatch":
            paint_swatch(cv, r, r["finish"], "enamel"); continue
        if k == "tube":
            s = b * 1146.0
            de = np.minimum(s, 1146.0 - s)
            be = smooth(80, 35, de)
            E = (0.62 + 0.38 * smooth(20, 70, de)) * (1 - 0.7 * be)
            alb = hexc("#ECEBE5") * (1 - 0.7 * be[..., None])
            cv.put(r, alb=alb, sm=0.85, cav=1.0, met=0.0, emi=E); continue
        if k in ("cav_long", "cav_end", "cav_top", "ballast"):
            if k == "cav_top":
                x = (a - 0.5) * 464; y = (b - 0.5) * 2 * hyo
                E = 0.55 + sum(0.25 * g(x - t, 70) - 0.18 * g(x - t, 18) for t in xs)
            elif k == "cav_long":
                E = 0.40 + 0.25 * a
                y = (b - 0.5) * 2 * hyo
            elif k == "ballast":
                E = 0.45 + 0.1 * a
                y = (b - 0.5) * 2 * hyo
            else:
                x = (a - 0.5) * 2 * 287
                E = 0.30 + sum(0.22 * g(x - t, 25) for t in xs) * b
                y = np.full_like(a, hyo)
            if k != "cav_end":
                E = E * (0.75 + 0.25 * smooth(0, 160, hyo - np.abs(y)))
            dust = smooth(0.55, 0.9, nz)
            alb = cav_col * (1 - 0.12 * dust[..., None])
            cv.put(r, alb=alb, sm=0.42, cav=0.9, met=0.0, emi=E * (0.95 + 0.1 * nz)); continue
        if k in ("lens_hang", "lens_back", "ret_hang"):
            E = 0.10 + 0.06 * b
            alb = hexc("#C4C0B4") * (0.92 + 0.08 * nz[..., None])
            if k == "lens_back":
                bugs = (band(h, w, 30, 90, seed + 77) > 0.86).astype(np.float32)
                alb = alb * (1 - 0.6 * bugs[..., None])
            cv.put(r, alb=alb, sm=0.6, cav=1.0, met=0.0, emi=E); continue
    cv.dilate(10)
    return cv


# ----------------------------------------------------------------------- louver family
def blade_profile_z(depth, top, segs, t_frac):
    ws = []
    for k in range(segs, -1, -1):
        z = depth * k / segs
        ws.append((-(1.5 + (top / 2 - 1.5) * (z / depth) ** 2), z))
    pts = ws + [(-w, z) for (w, z) in reversed(ws)]
    t = [0.0]
    for i in range(1, len(pts)):
        t.append(t[-1] + math.hypot(pts[i][0] - pts[i - 1][0], pts[i][1] - pts[i - 1][1]))
    T = t[-1]
    zs = np.interp(t_frac * T, t, [p[1] for p in pts])
    return zs


def paint_louver(lay):
    oid, spec = lay["id"], lay["spec"]
    A = lay["atlas"]
    cv = Canvas(*A["size"])
    regs = A["regions"]
    fx, fy = lay["field"]
    D, top = spec["depth"], spec["blade_top"]
    kind, xs_t, zt = spec["tubes"]
    seed = int(oid[1:]) * 131
    cols = lay["columns"]
    alu = FIN["rim_alu"][0]
    for n, r in regs.items():
        rows, cols_, a, b = cv.grid(r)
        h, w = a.shape
        k = r["kind"]
        if k == "swatch":
            paint_swatch(cv, r, r["finish"], "enamel"); continue
        if k == "refl":
            x = (a - 0.5) * 2 * fx; y = (b - 0.5) * 2 * fy
            E = np.full_like(a, 0.14)
            alb = np.ones((h, w, 3), np.float32) * alu
            sm = np.full_like(a, 0.72); met = np.ones_like(a)
            for xc, hw in cols:
                if kind == "straight":
                    E += 0.30 * g(x - xc, 55) - 0.14 * g(x - xc, 15)
                else:
                    E += 0.20 * g(x - xc, 120)
                    # diffuser patch over each U-lamp (S25): matte white, glows evenly
                    patch = (np.abs(x - xc) < 110) & (np.abs(y) < fy - 60)
                    E = np.where(patch, 0.42, E)
                    alb = np.where(patch[..., None], hexc("#E6E2D8"), alb)
                    sm = np.where(patch, 0.35, sm); met = np.where(patch, 0.0, met)
            E *= 0.78 + 0.22 * smooth(0, 120, fy - np.abs(y))
            nrm, pn = peel((h, w), seed + 1, 0.015)
            cv.put(r, alb=alb * (0.96 + 0.05 * pn[..., None]), nrm=nrm, sm=sm, cav=1.0, met=met, emi=E); continue
        if k == "tube":
            Ltot = (2 * fy - 40) if kind == "straight" else 1000.0
            s = b * Ltot
            de = np.minimum(s, Ltot - s)
            E = 0.64 + 0.36 * smooth(15, 60, de)
            alb = hexc("#EDECE6") * (1 - 0.15 * smooth(45, 20, de)[..., None])
            cv.put(r, alb=alb, sm=0.85, cav=1.0, met=0.0, emi=E); continue
        if k == "blade":
            which = r["which"]
            if which.startswith("perim"):
                z = D * (1 - b)                      # profile from top (b = 0 at t = 0) to the rim edge
                z = D * (1 - b)
            else:
                z = blade_profile_z(D, top, 5, b)
            zz = np.clip(z / D, 0, 1)
            # semi-specular faces mirror the dark room below the cut-off; only the top of the cell
            # carries the lamp (S04: 230 against 1432 cd/m2 for a lensed fixture at 75 deg)
            E = 0.012 + 0.11 * zz ** 2.6
            if which in ("blade_long", "perim_long"):
                E = E + 0.13 * g(z - 0.82 * D, 0.08 * D)        # image of the tube high in the cell
            else:
                span = 2 * fx
                xpos = (a - 0.5) * span
                for xc in (xs_t if kind == "straight" else [(xs_t[0] + xs_t[1]) / 2, (xs_t[2] + xs_t[3]) / 2]):
                    E = E + 0.16 * g(xpos - xc, 22) * zz ** 2
            lip = z < 0.6
            streak = band(h, w * 4, 2, 50, seed + 7)[:, ::4]
            alb = alu * (0.93 + 0.08 * streak[..., None])
            nrm = normal_from_grad((streak - 0.5) * 0.04, np.zeros_like(streak))
            E = np.where(lip, 0.0, E)
            cv.put(r, alb=alb, nrm=nrm, sm=np.where(lip, 0.5, 0.68), cav=np.where(lip, 1.0, 0.85 + 0.15 * zz), met=1.0, emi=E); continue
        if k == "cav_long":
            E = 0.26 - 0.10 * a
            cv.put(r, alb=alu, sm=0.65, cav=0.9, met=1.0, emi=E); continue
        if k == "cav_end":
            x = (a - 0.5) * 2 * fx
            E = 0.12 + 0.16 * b
            for t in xs_t:
                E = E + 0.10 * g(x - t, 30) * b
            cv.put(r, alb=alu, sm=0.6, cav=0.85, met=1.0, emi=E); continue
        if k == "lip":
            cv.put(r, alb=alu, sm=0.5, cav=1.0, met=1.0, emi=0.0); continue
        if k == "flatblade":
            E = 0.012 + 0.14 * b ** 2.6
            cv.put(r, alb=alu, sm=0.66, cav=1.0, met=1.0, emi=E); continue
    cv.dilate(10)
    return cv


# ----------------------------------------------------------------------- can (L10)
def paint_can(lay):
    spec = lay["spec"]
    A = lay["atlas"]
    cv = Canvas(*A["size"])
    regs = A["regions"]
    warm = np.array([1.0, 0.80, 0.56], np.float32)           # filament colour against the cool driver
    H = spec["baffle_h"]
    for n, r in regs.items():
        rows, cols, a, b = cv.grid(r)
        h, w = a.shape
        k = r.get("kind")
        if k == "swatch":
            paint_swatch(cv, r, r["finish"], "enamel"); continue
        if k == "baffle":
            z = b * H
            ph = (z / 6.0) % 1.0                                   # 6 mm rib pitch (B20, B65)
            # sawtooth step: a down-facing flat (seen from below) then a short up-facing riser
            gy = np.where(ph < 0.78, -0.35, 1.6)
            tip = smooth(0.70, 0.78, ph) * smooth(0.9, 0.78, ph)
            nrm = normal_from_grad(np.zeros_like(gy), gy)
            alb = hexc("#1A1918") * (1 + 0.6 * tip[..., None])
            E = (0.006 + 0.10 * (z / H) ** 3 * (0.25 + 0.75 * tip))[..., None] * warm
            cv.put(r, alb=alb, nrm=nrm, sm=0.25 + 0.2 * tip, cav=np.where(ph < 0.78, 1.0, 0.6), met=0.0, emi=E); continue
        if k == "cup":
            E = (0.20 + 0.30 * b)[..., None] * warm
            cv.put(r, alb=hexc("#BDBDB8"), sm=0.75, cav=1.0, met=1.0, emi=E); continue
        if k == "gap":
            cv.put(r, alb=hexc("#2A2826"), sm=0.3, cav=0.6, met=0.0, emi=np.repeat((0.04 * np.ones_like(a))[..., None], 3, -1)); continue
        if k == "face":
            rf = spec["face_d"] / 2
            x = (a - 0.5) * 2 * rf; y = (b - 0.5) * 2 * rf
            rr = np.hypot(x, y) / rf
            # frosted reflector-lamp face with a hexagonal stipple, hot centre
            q = 3.2
            hx = (x / q + 0.5 * np.floor(y / (q * 0.866)) % 1.0) % 1.0 - 0.5
            hy = (y / (q * 0.866)) % 1.0 - 0.5
            st = np.clip(1 - np.hypot(hx, hy) * 2.2, 0, 1)
            E = (0.78 + 0.22 * np.clip(1 - rr, 0, 1) ** 0.6) * (0.96 + 0.04 * st)
            E = np.where(rr > 1.0, 0.1, E)
            nrm = normal_from_grad(-(hx) * 0.2 * st, -(hy) * 0.2 * st)
            cv.put(r, alb=hexc("#F1EEE6"), nrm=nrm, sm=0.55, cav=1.0, met=0.0, emi=E[..., None] * warm); continue
    cv.dilate(10)
    return cv


# ----------------------------------------------------------------------- trim sheet (housing)
STRIPS = {"tbar": (0, 120), "enamel": (120, 280), "alu": (280, 400), "black": (400, 520),
          "zinc": (520, 620), "holder": (620, 720), "rim_alu": (720, 850), "tile": (850, 1024)}


def paint_trim():
    N = 1024
    cv = Canvas(N, N)
    for name, (lo, hi) in STRIPS.items():
        r = dict(x=0, y=lo, w=N, h=hi - lo)
        h, w = hi - lo, N
        seed = 900 + lo
        col, sm, met = FIN[name]
        nb = band(h * 4, w, 40, 300, seed)[: h]                 # periodic in U (the strip repeats along U)
        grime = spectral(h * 4, w, 1.6, seed + 1)[: h]
        if name in ("alu", "rim_alu"):
            streak = band(h * 8, w, 1, 200, seed + 2)[: h]       # brushing along U
            streak = np.repeat(streak.mean(0, keepdims=True), h, 0) * 0.6 + streak * 0.4
            alb = col * (0.93 + 0.09 * streak[..., None])
            nrm = normal_from_grad(np.zeros_like(streak), (np.roll(streak, 1, 0) - np.roll(streak, -1, 0)) * 0.6)
            smv = sm - 0.06 * grime
        elif name == "tile":
            fis = band(h * 4, w, 80, 400, seed + 3)[: h]
            alb = col * (0.9 + 0.12 * fis[..., None])
            nrm = normal_from_grad((np.roll(fis, -1, 1) - np.roll(fis, 1, 1)) * 2, (np.roll(fis, 1, 0) - np.roll(fis, -1, 0)) * 2)
            smv = np.full_like(fis, sm)
        else:
            amp = 0.06 if name in ("enamel", "tbar", "holder") else 0.03
            gx = (np.roll(nb, -1, 1) - np.roll(nb, 1, 1)) * amp * 4
            gy = (np.roll(nb, 1, 0) - np.roll(nb, -1, 0)) * amp * 4
            nrm = normal_from_grad(gx, gy)
            alb = col * (0.965 + 0.05 * nb[..., None]) * (0.97 + 0.04 * grime[..., None])
            if name == "enamel":
                alb = alb * (1 - 0.05 * smooth(0.6, 0.95, grime)[..., None]) + hexc("#8A7A60") * 0.05 * smooth(0.6, 0.95, grime)[..., None]
            if name == "zinc":
                spk = (band(h * 4, w, 200, 500, seed + 5)[: h] > 0.72).astype(np.float32)
                alb = alb * (1 + 0.08 * spk[..., None])
            smv = sm - 0.05 * grime
        cv.put(r, alb=alb, nrm=nrm, sm=smv, cav=1.0, met=met, emi=0.0)
    return cv


# ----------------------------------------------------------------------- L03 soil (multiply)
def paint_soil(lay):
    sx, sy = lay["soil_mm"]
    w, h = 256, 512
    jj, ii = np.meshgrid(np.arange(w, dtype=np.float32), np.arange(h, dtype=np.float32))
    x = ((jj + 0.5) / w - 0.5) * sx
    y = ((h - 1 - ii + 0.5) / h - 0.5) * sy
    hx_i, hy_i = 312.0, 612.0
    dx = np.abs(x) - hx_i
    dy = np.abs(y) - hy_i
    streaks = band(h, w * 3, 2, 40, 303)[:, ::3]             # dust follows the drawn air: across the slot
    patchy = spectral(h, w, 2.0, 304)
    long_side = np.exp(-np.maximum(dx, 0) / 55.0) * (dx > -2) * smooth(hy_i + 40, hy_i - 120, np.abs(y))
    ends = 0.35 * np.exp(-np.maximum(dy, 0) / 30.0) * (dy > -2) * smooth(hx_i + 30, hx_i - 80, np.abs(x))
    d = np.clip(long_side * (0.55 + 0.45 * streaks) * (0.7 + 0.3 * patchy) + ends * (0.6 + 0.4 * patchy), 0, 1)
    soil = np.array([0.74, 0.70, 0.62], np.float32)
    m = 1 - d[..., None] * (1 - soil) * 0.95
    save(f"{OUT}/LampOpt_L03_Soil_A.png", m, "RGB")


# ----------------------------------------------------------------------- main
def main():
    done = []
    if not ONLY or "TRIM" in ONLY:
        paint_trim().write("LampOpt_Trim", half_as=False, emission=False)
        # the trim mask at half size (smoothness / metal are low frequency)
        done.append("Trim")
    for oid in ["L01", "L02", "L03", "L04", "L05", "L06", "L07", "L08", "L09", "L10"]:
        if ONLY and oid not in ONLY:
            continue
        lay = json.load(open(os.path.join(LAY, "LampOpt_%s_layout.json" % oid)))
        fam = lay["family"]
        name = "LampOpt_%s_Lens" % oid
        if fam == "lensed":
            cv = paint_lensed(lay)
            if oid == "L05":
                cv.write(name, field=True)
                v = paint_lensed(lay, "delamped")
                save(f"{OUT}/LampOpt_L05_LensDelamped_A.png", down2(v.alb), "RGB")
                v = paint_lensed(lay, "mixed")
                save(f"{OUT}/LampOpt_L05_LensMixed_E.png", lin_to_srgb(v.emi), "RGB")
                paint_open(lay).write("LampOpt_L05_Open", half_as=True)
            else:
                cv.write(name)
            if oid == "L03":
                paint_soil(lay)
        elif fam == "louver":
            paint_louver(lay).write(name)
        else:
            paint_can(lay).write(name)
        done.append(oid)
    print("TEXTURES", " ".join(done))


main()
